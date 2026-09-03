using System.Collections.Generic;
using UnityEngine;

public class GameLogicManager : MonoBehaviour
{
    public static GameLogicManager Instance { get; private set; }

    [Header("References")]
    [SerializeField]
    private Player player;

    [SerializeField]
    private PoolManager poolManager;

    private VfxSpawner vfxSpawner;

    private IUnitCreator unitCreator;

    private Border[] borders;

    private Difficulty difficulty;

    // Step 节拍计时：Running 中按 Difficulty 当前阶段的 StepInterval 触发 GameEvents.OnStep。
    private float stepTimer;

    // 金币怪冷却计时与就绪标记：冷却满后置标记，下一次普通波把 1~2 只替换成金币怪。
    private float goldBountyTimer;
    private bool goldBountyPending;

    // 宝箱怪待刷计数：经验每跨一个里程碑 +1，下一次普通波替换一只宝箱怪并 -1。
    private int chestBountyPending;

    // Roguelike 升级体系：StartGame 时 Reset，PinBallBase / Player 通过这些对象读取当前生效值。
    private BallStats ballStats;
    private UpgradeService upgradeService;

    // 单位定义表：Unit.Init 按 (unitId, 难度等级) 查询数值。
    private UnitTable unitTable;

    // 全局金币：击杀 Unit 累加；暂未接入显示/消费（后续经济系统使用）。
    private int gold;

    public Difficulty Difficulty => difficulty;

    public UnitTable UnitTable => unitTable;

    /// <summary>当前全局金币存量（击杀 Unit 累加；每局 StartGame 清零）。</summary>
    public int Gold => gold;

    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        gold += amount;
    }

    public Border[] Borders => borders;

    public IReadOnlyList<PinBallBase> ActivePinBalls => poolManager != null ? poolManager.ActivePinBalls : null;

    public IReadOnlyList<UnitBase> ActiveUnits => poolManager != null ? poolManager.ActiveUnits : null;

    public Player Player => player;

    public VfxSpawner VfxSpawner => vfxSpawner;

    public BallStats BallStats => ballStats;

    public UpgradeService UpgradeService => upgradeService;

    [Header("Game State")]
    [SerializeField]
    private GameState gameState = GameState.Preparing;

    public GameState CurrentState => gameState;

    public void SetGameState(GameState state)
    {
        gameState = state;
    }

    private void Awake()
    {
        Instance = this;
        vfxSpawner = new VfxSpawner(poolManager);

        // 难度表：通过 Addressables 短地址加载。
        DifficultyTable difficultyTable = AssetLoader.Load<DifficultyTable>("DifficultyTable");
        difficulty = new Difficulty(difficultyTable);

        // 单位定义表：通过 Addressables 短地址加载（Units.csv + Units_Level.csv 导入生成）。
        unitTable = AssetLoader.Load<UnitTable>("UnitTable");

        // Roguelike 升级体系初始化（BallStats 为通用属性容器，stat 定义待重新设计后回填；池数据通过 Addressables 加载）。
        ballStats = new BallStats();

        KillMilestoneTable milestoneTable = AssetLoader.Load<KillMilestoneTable>("KillMilestoneTable");
        UpgradeCatalog catalog = AssetLoader.Load<UpgradeCatalog>("UpgradeCatalog");
        upgradeService = new UpgradeService(milestoneTable, catalog, ballStats, player);
        upgradeService.RegisterEvents();

        unitCreator = new UnitCreator();

        // 击杀结算：累计金币、宝箱怪掉落升级机会（经验由 UpgradeService 通过同一事件自行累计）。
        GameEvents.OnUnitKilled += HandleUnitKilled;

        // 经验跨里程碑：记一次"待刷宝箱怪"，下一次普通波替换生成。
        GameEvents.OnKillMilestoneReached += HandleKillMilestoneReached;
    }

    private void OnDestroy()
    {
        GameEvents.OnUnitKilled -= HandleUnitKilled;
        GameEvents.OnKillMilestoneReached -= HandleKillMilestoneReached;

        if (upgradeService != null)
        {
            upgradeService.UnregisterEvents();
            upgradeService = null;
        }

        if (unitCreator is System.IDisposable disposable)
            disposable.Dispose();
        unitCreator = null;

        if (Instance == this)
            Instance = null;
    }

    /// <summary>Unit 被击杀时回调：累计该 Unit 掉落的金币；宝箱怪额外给一次升级机会。</summary>
    private void HandleUnitKilled(UnitBase unit)
    {
        if (unit == null) return;

        AddGold(unit.Gold);

        if (unit.UnitId == Defines.UnitChestId && upgradeService != null)
            upgradeService.GrantUpgradePoint();
    }

    /// <summary>经验里程碑达成：记一只待刷宝箱怪（在后续普通波中替换生成）。</summary>
    private void HandleKillMilestoneReached(int _)
    {
        chestBountyPending++;
    }

    private void Update()
    {
        if (gameState != GameState.Running)
            return;
        UpdateGame();
    }

    public void StartGame()
    {
        gameState = GameState.Preparing;

        borders = FindObjectsByType<Border>(FindObjectsSortMode.None);

        // 重置 Roguelike 体系：清空所有 modifier 与击杀计数、候选。
        // 在 player.Init 之前完成，确保新一局所有数值都从基础值开始。
        if (ballStats != null) ballStats.Reset();
        if (upgradeService != null) upgradeService.Reset();
        gold = 0;

        if (player != null)
            player.Init();

        if (poolManager != null)
        {
            poolManager.ClearActivePinBalls();
            poolManager.ClearActiveUnits();
            poolManager.ClearActiveVfx();

            UnitBase[] existingUnits = FindObjectsByType<UnitBase>(FindObjectsSortMode.None);
            for (int i = 0; i < existingUnits.Length; i++)
            {
                existingUnits[i].Init();
                poolManager.RegisterExistingUnit(existingUnits[i]);
            }
        }

        stepTimer = 0f;
        goldBountyTimer = 0f;
        goldBountyPending = false;
        chestBountyPending = 0;
        difficulty?.Reset();

        gameState = GameState.Running;
        GameEvents.RaiseGameStart();
    }

    /// <summary>暂停游戏：主循环停推，并通知所有订阅方。</summary>
    public void PauseGame()
    {
        if (gameState != GameState.Running) return;
        gameState = GameState.Paused;
        GameEvents.RaiseGamePause();
    }

    /// <summary>从暂停恢复游戏。</summary>
    public void ResumeGame()
    {
        if (gameState != GameState.Paused) return;
        gameState = GameState.Running;
        GameEvents.RaiseGameResume();
    }

    /// <summary>升级抽卡触发：进入 SelectingUpgrade 状态，UI 监听 OnUpgradeOffered 显面板。</summary>
    public void PauseForUpgradeSelection()
    {
        if (gameState != GameState.Running) return;
        gameState = GameState.SelectingUpgrade;
    }

    /// <summary>
    /// HUD 宝箱按钮入口：消耗一次升级次数并打开三选一面板。
    /// 仅在 Running 且有剩余升级次数时生效。
    /// </summary>
    public void OpenUpgradeSelection()
    {
        if (gameState != GameState.Running) return;
        if (upgradeService == null || !upgradeService.TryBeginOffer()) return;

        // 无 UI 订阅方时 UpgradeService 会自动应用并结束，此时无需暂停。
        if (upgradeService.IsOffering)
            PauseForUpgradeSelection();
    }

    /// <summary>UpgradeService.ApplySelected 完成后回到 Running。</summary>
    public void ResumeFromUpgradeSelection()
    {
        if (gameState != GameState.SelectingUpgrade) return;
        gameState = GameState.Running;
    }

    public void UpdateGame()
    {
        if (borders == null) return;

        IReadOnlyList<UnitBase> activeUnits = poolManager != null ? poolManager.ActiveUnits : null;

        for (int i = 0; i < borders.Length; i++)
        {
            if (borders[i] != null)
                borders[i].RefreshRect();
        }

        if (activeUnits != null)
        {
            for (int i = 0; i < activeUnits.Count; i++)
            {
                if (activeUnits[i] != null)
                    activeUnits[i].RefreshRect();
            }
        }

        if (player != null)
            player.Tick();

        // 金币怪冷却：满 GoldSpawnInterval 秒后就绪，等下一次普通波消费（把 1~2 只替换成金币怪）。
        goldBountyTimer += Time.deltaTime;
        if (goldBountyTimer >= Defines.GoldSpawnInterval)
        {
            goldBountyTimer = 0f;
            goldBountyPending = true;
        }

        // 推进难度时间轴与 Step 心跳：Running 下每 Difficulty.GetStepInterval() 秒触发一次，
        // 供 UnitCreator 生成新一批、所有 Unit 开启本轮移动动画。
        difficulty?.Tick(Time.deltaTime);
        stepTimer += Time.deltaTime;
        float interval = difficulty != null ? difficulty.GetStepInterval() : Defines.StepInterval;
        bool chestReady = chestBountyPending > 0;
        while (stepTimer >= interval)
        {
            stepTimer -= interval;
            GameEvents.RaiseStep();
            unitCreator?.SpawnStep(goldBountyPending, chestReady);
            goldBountyPending = false;
            if (chestReady)
            {
                chestBountyPending = Mathf.Max(0, chestBountyPending - 1);
                chestReady = false;
            }
            interval = difficulty != null ? difficulty.GetStepInterval() : Defines.StepInterval;
        }

        if (poolManager != null)
        {
            IReadOnlyList<PinBallBase> activePinBalls = poolManager.ActivePinBalls;
            for (int i = activePinBalls.Count - 1; i >= 0; i--)
            {
                if (i >= activePinBalls.Count) continue;
                PinBallBase pb = activePinBalls[i];
                if (pb == null || !pb.gameObject.activeSelf) continue;
                pb.Tick(borders, activeUnits);
            }

            for (int i = activeUnits.Count - 1; i >= 0; i--)
            {
                if (i >= activeUnits.Count) continue;
                UnitBase unit = activeUnits[i];
                if (unit == null || !unit.gameObject.activeSelf) continue;
                unit.Tick();
            }
        }
    }

    public PinBallBase SpawnPinBall(string address, Vector2 position, Vector2 direction, float speed)
    {
        if (poolManager == null) return null;
        return poolManager.SpawnPinBall(address, position, direction, speed);
    }

    public void RecyclePinBall(PinBallBase pb)
    {
        // 无限发射模式下球回收仅归还对象池，不再操作玩家库存。
        if (poolManager != null)
            poolManager.RecyclePinBall(pb);
    }

    public UnitBase SpawnUnit(string address, Vector2 position, int level = 1)
    {
        if (poolManager == null) return null;
        return poolManager.SpawnUnit(address, position, level);
    }

    public void RecycleUnit(UnitBase unit)
    {
        if (poolManager != null)
            poolManager.RecycleUnit(unit);
    }

    /// <summary>
    /// Unit 触碰到底部 Border 时回调：对 Player 造成伤害并回收 Unit；
    /// 如果 Player 死亡，则进入游戏结束流程。
    /// </summary>
    public void OnUnitReachBottom(UnitBase unit)
    {
        if (unit == null) return;

        if (player != null && !player.IsDead)
        {
            unit.PlayReachBottomAnimation();
            bool dead = player.TakeDamage(unit.Attack);
            RecycleUnit(unit);

            if (dead)
                EndGame();
        }
        else
        {
            RecycleUnit(unit);
        }
    }

    /// <summary>结束当前游戏：停止主循环，清空场上对象，通知订阅方。</summary>
    public void EndGame()
    {
        gameState = GameState.Ended;

        if (poolManager != null)
        {
            poolManager.ClearActivePinBalls();
            poolManager.ClearActiveUnits();
            poolManager.ClearActiveVfx();
        }

        GameEvents.RaiseGameEnd();
    }

    /// <summary>由游戏结束界面「重新开始」按钮调用。</summary>
    public void RestartGame()
    {
        StartGame();
    }

    /// <summary>由游戏结束界面「回到主页」按钮调用：回到准备状态，通知订阅方。</summary>
    public void BackToHome()
    {
        gameState = GameState.Preparing;

        if (poolManager != null)
        {
            poolManager.ClearActivePinBalls();
            poolManager.ClearActiveUnits();
            poolManager.ClearActiveVfx();
        }

        GameEvents.RaiseReturnToHome();
    }
}

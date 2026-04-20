using System.Collections.Generic;
using UnityEngine;

public class GameLogicManager : MonoBehaviour
{
    public static GameLogicManager Instance { get; private set; }

    [Header("References")]
    [SerializeField]
    private Player player;

    [SerializeField]
    private PlayerRender playerRender;

    [SerializeField]
    private PoolManager poolManager;

    private IUnitCreator unitCreator;

    private Border[] borders;

    private Difficulty difficulty;

    // Step 节拍计时：Running 中按 Difficulty 当前阶段的 StepInterval 触发 GameEvents.OnStep。
    private float stepTimer;

    public Difficulty Difficulty => difficulty;

    public Border[] Borders => borders;

    public IReadOnlyList<PinBallBase> ActivePinBalls => poolManager != null ? poolManager.ActivePinBalls : null;

    public IReadOnlyList<UnitBase> ActiveUnits => poolManager != null ? poolManager.ActiveUnits : null;

    public Player Player => player;

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

        // 难度表：运行时走 AssetLoader（目前 Editor 下 AssetDatabase；将来接 Addressables）。
        DifficultyTable table = AssetLoader.Load<DifficultyTable>("8_Data/DifficultyTable.asset");
        difficulty = new Difficulty(table);

        unitCreator = new UnitCreator();
    }

    private void OnDestroy()
    {
        if (unitCreator is System.IDisposable disposable)
            disposable.Dispose();
        unitCreator = null;

        if (Instance == this)
            Instance = null;
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

        if (player != null)
            player.Init();

        if (poolManager != null)
        {
            poolManager.ClearActivePinBalls();
            poolManager.ClearActiveUnits();

            UnitBase[] existingUnits = FindObjectsByType<UnitBase>(FindObjectsSortMode.None);
            for (int i = 0; i < existingUnits.Length; i++)
            {
                existingUnits[i].Init();
                poolManager.RegisterExistingUnit(existingUnits[i]);
            }
        }

        stepTimer = 0f;
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

        // 推进难度时间轴与 Step 心跳：Running 下每 Difficulty.GetStepInterval() 秒触发一次，
        // 供 UnitCreator 生成新一批、所有 Unit 开启本轮移动动画。
        difficulty?.Tick(Time.deltaTime);
        stepTimer += Time.deltaTime;
        float interval = difficulty != null ? difficulty.GetStepInterval() : Defines.StepInterval;
        while (stepTimer >= interval)
        {
            stepTimer -= interval;
            GameEvents.RaiseStep();
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

        if (playerRender != null)
            playerRender.Tick();
    }

    public PinBallBase SpawnPinBall(Vector2 position, Vector2 direction, float speed)
    {
        if (poolManager == null) return null;
        return poolManager.SpawnPinBall(position, direction, speed);
    }

    public void RecyclePinBall(PinBallBase pb)
    {
        if (poolManager != null)
            poolManager.RecyclePinBall(pb);

        if (player != null)
            player.AddPinBall();
    }

    public UnitBase SpawnUnit(Vector2 position)
    {
        if (poolManager == null) return null;
        return poolManager.SpawnUnit(position);
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
        }

        GameEvents.RaiseReturnToHome();
    }
}

using UnityEngine;

/// <summary>
/// 难度运行时驱动：基于 <see cref="DifficultyTable"/> + 当前 gameTime 提供参数查询。
/// 由 GameLogicManager 在 Awake 时加载并持有；StartGame 时 Reset，UpdateGame 时 Tick。
/// </summary>
public class Difficulty
{
    private readonly DifficultyTable table;
    private float gameTime;

    public float GameTime => gameTime;

    public bool HasTable => table != null && table.StageCount > 0;

    public Difficulty(DifficultyTable table)
    {
        this.table = table;
    }

    public void Reset()
    {
        gameTime = 0f;
    }

    public void Tick(float deltaTime)
    {
        if (deltaTime > 0f)
            gameTime += deltaTime;
    }

    private DifficultyStageData CurrentStage => table != null ? table.GetStageAt(gameTime) : null;

    /// <summary>当前阶段的生成区间 [min, max]，若无表返回 (1,1) 兜底。</summary>
    public (int min, int max) GetSpawnRange()
    {
        DifficultyStageData s = CurrentStage;
        if (s == null) return (1, 1);

        int min = Mathf.Max(1, s.spawnMin);
        int max = Mathf.Max(min, s.spawnMax);
        return (min, max);
    }

    /// <summary>当前阶段 Unit 的 maxHp；无表时返回 1。</summary>
    public int GetUnitHp()
    {
        DifficultyStageData s = CurrentStage;
        return s != null && s.unitHp > 0 ? s.unitHp : 3;
    }

    /// <summary>当前阶段 Unit 的 attack；无表时返回 1。</summary>
    public int GetUnitAttack()
    {
        DifficultyStageData s = CurrentStage;
        return s != null && s.unitAttack > 0 ? s.unitAttack : 1;
    }

    /// <summary>当前阶段的 Step 间隔；未配置（&lt;=0）时退回 Defines.StepInterval。</summary>
    public float GetStepInterval()
    {
        DifficultyStageData s = CurrentStage;
        if (s == null || s.stepInterval <= 0f) return Defines.StepInterval;
        return s.stepInterval;
    }

    /// <summary>
    /// 当前阶段单个 Unit 击杀给玩家累加的经验值。Roguelike 升级里程碑依赖此值,
    /// 若 Difficulty 表缺失或当前阶段未配置(<=0)会输出 LogError 并回退为 1,避免静默错误。
    /// </summary>
    public int GetUnitExperience()
    {
        DifficultyStageData s = CurrentStage;
        if (s == null)
        {
            Debug.LogError("[Difficulty] DifficultyTable missing or empty when querying unit experience; falling back to 1.");
            return 1;
        }
        if (s.unitExperience <= 0)
        {
            Debug.LogError($"[Difficulty] Stage at startTime={s.startTime} has unitExperience<=0; falling back to 1. Check Difficulty.csv.");
            return 1;
        }
        return s.unitExperience;
    }
}

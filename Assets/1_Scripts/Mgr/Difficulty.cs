using UnityEngine;

/// <summary>
/// 难度运行时驱动：基于 <see cref="DifficultyTable"/> + 当前 gameTime 提供参数查询。
/// 难度表只管「节奏 + 生成数量区间 + 等级分布权重」；怪的数值由
/// <see cref="UnitTable"/> 按等级提供。由 GameLogicManager 在 Awake 时加载并持有；
/// StartGame 时 Reset，UpdateGame 时 Tick。
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

    /// <summary>当前阶段的生成数量区间 [min, max]，若无表返回 (1, 1) 兜底。</summary>
    public (int min, int max) GetSpawnRange()
    {
        DifficultyStageData s = CurrentStage;
        if (s == null) return (1, 1);

        int min = Mathf.Max(1, s.spawnMin);
        int max = Mathf.Max(min, s.spawnMax);
        return (min, max);
    }

    /// <summary>
    /// 按当前阶段的等级权重随机出一个刷怪等级（每 spawn 一只调用一次）。
    /// 权重为空或全 0 时返回 1。
    /// </summary>
    public int RollSpawnLevel()
    {
        DifficultyStageData s = CurrentStage;
        if (s == null || s.spawnLevels == null || s.spawnLevels.Count == 0) return 1;

        int total = 0;
        for (int i = 0; i < s.spawnLevels.Count; i++)
            total += Mathf.Max(0, s.spawnLevels[i].weight);
        if (total <= 0) return 1;

        int roll = Random.Range(0, total);
        int acc = 0;
        for (int i = 0; i < s.spawnLevels.Count; i++)
        {
            acc += Mathf.Max(0, s.spawnLevels[i].weight);
            if (roll < acc)
                return Mathf.Max(1, s.spawnLevels[i].level);
        }
        return 1;
    }

    /// <summary>
    /// 当前阶段 spawnLevels 中配置的最高等级（供金币怪等「跟随难度」的独立刷怪使用）。
    /// 无表或无配置返回 1。
    /// </summary>
    public int GetStageMaxLevel()
    {
        DifficultyStageData s = CurrentStage;
        if (s == null || s.spawnLevels == null || s.spawnLevels.Count == 0) return 1;

        int max = 1;
        for (int i = 0; i < s.spawnLevels.Count; i++)
            max = Mathf.Max(max, s.spawnLevels[i].level);
        return max;
    }

    /// <summary>当前阶段的 Step 间隔；未配置（&lt;=0）时退回 Defines.StepInterval。</summary>
    public float GetStepInterval()
    {
        DifficultyStageData s = CurrentStage;
        if (s == null || s.stepInterval <= 0f) return Defines.StepInterval;
        return s.stepInterval;
    }
}

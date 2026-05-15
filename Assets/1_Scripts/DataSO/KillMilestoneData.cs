using System;

/// <summary>
/// 一次升级里程碑：玩家累计经验值达到 <see cref="experienceThreshold"/> 时触发一次三选一升级。
/// 各品质权重决定该次升级抽到不同品质的概率（按权重比例归一化）。
/// </summary>
[Serializable]
public class KillMilestoneData
{
    /// <summary>累计经验阈值（必须严格升序）。每个 Unit 击杀给予的经验由 Difficulty 阶段配置。</summary>
    public int experienceThreshold;

    public int weightCommon;
    public int weightUncommon;
    public int weightRare;
    public int weightLegendary;
}

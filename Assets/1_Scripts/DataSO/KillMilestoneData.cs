using System;

/// <summary>
/// 一次击杀里程碑：累计击杀达到 <see cref="killThreshold"/> 时触发一次三选一升级。
/// 各品质权重决定该次升级抽到不同品质的概率（按权重比例归一化）。
/// </summary>
[Serializable]
public class KillMilestoneData
{
    /// <summary>累计击杀阈值（必须严格升序）。</summary>
    public int killThreshold;

    public int weightCommon;
    public int weightUncommon;
    public int weightRare;
    public int weightLegendary;
}

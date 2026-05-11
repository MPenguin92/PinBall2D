using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 击杀里程碑表 ScriptableObject：由 DataImporter 从 9_Excel/KillMilestones.csv 生成，
/// 运行时由 UpgradeService 用 killCount 索引下一个里程碑。
/// 表末之后使用最后一行的权重并以前两行差值无限循环（避免后期玩家无升级）。
/// </summary>
[CreateAssetMenu(fileName = "KillMilestoneTable", menuName = "PinBall2D/Data/KillMilestoneTable", order = 1)]
public class KillMilestoneTable : ScriptableObject
{
    [SerializeField]
    [Tooltip("按 killThreshold 升序排列；运行时 killCount 达到任一阈值则触发一次升级。")]
    private List<KillMilestoneData> milestones = new List<KillMilestoneData>();

    public IReadOnlyList<KillMilestoneData> Milestones => milestones;

    public int Count => milestones != null ? milestones.Count : 0;

    /// <summary>由 Editor 导入工具写入；运行时无需调用。</summary>
    public void SetMilestones(List<KillMilestoneData> list)
    {
        milestones = list ?? new List<KillMilestoneData>();
    }

    /// <summary>
    /// 根据 0-based 里程碑索引返回对应的阈值。
    /// 当 idx 超出表末时按"上一行差值"线性外推。
    /// </summary>
    public int GetThresholdAt(int idx)
    {
        if (milestones == null || milestones.Count == 0) return int.MaxValue;
        if (idx < milestones.Count) return milestones[idx].killThreshold;

        int last = milestones[milestones.Count - 1].killThreshold;
        int delta = milestones.Count >= 2
            ? milestones[milestones.Count - 1].killThreshold - milestones[milestones.Count - 2].killThreshold
            : last;
        delta = Mathf.Max(1, delta);
        int extra = idx - (milestones.Count - 1);
        return last + delta * extra;
    }

    /// <summary>取对应索引的权重；超出表末时使用最后一行权重。</summary>
    public KillMilestoneData GetWeightsAt(int idx)
    {
        if (milestones == null || milestones.Count == 0) return null;
        if (idx < milestones.Count) return milestones[idx];
        return milestones[milestones.Count - 1];
    }
}

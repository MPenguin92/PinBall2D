using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 难度表 ScriptableObject：由 DataImporter 从 9_Excel/Difficulty.csv 生成，
/// 运行时由 Difficulty 根据 gameTime 查询对应阶段。
/// </summary>
[CreateAssetMenu(fileName = "DifficultyTable", menuName = "PinBall2D/Data/DifficultyTable", order = 0)]
public class DifficultyTable : ScriptableObject
{
    [SerializeField]
    [Tooltip("按 startTime 升序排列；运行时按顺序匹配首个满足 gameTime >= startTime 的阶段。")]
    private List<DifficultyStageData> stages = new List<DifficultyStageData>();

    public IReadOnlyList<DifficultyStageData> Stages => stages;

    public int StageCount => stages != null ? stages.Count : 0;

    /// <summary>供 Editor 导入工具写入；运行时无需调用。</summary>
    public void SetStages(List<DifficultyStageData> newStages)
    {
        stages = newStages ?? new List<DifficultyStageData>();
    }

    /// <summary>
    /// 根据 gameTime 返回匹配的阶段：取 startTime &lt;= gameTime 的**最后一个**。
    /// 若表为空或时间早于第一个阶段，返回第一个（兜底，避免 null）。
    /// </summary>
    public DifficultyStageData GetStageAt(float gameTime)
    {
        if (stages == null || stages.Count == 0) return null;

        DifficultyStageData matched = stages[0];
        for (int i = 0; i < stages.Count; i++)
        {
            if (gameTime >= stages[i].startTime)
                matched = stages[i];
            else
                break;
        }
        return matched;
    }
}

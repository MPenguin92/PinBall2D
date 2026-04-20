using System;

/// <summary>
/// 一个难度阶段：从 <see cref="startTime"/> 秒开始生效，直到下一阶段接替。
/// 全部字段与 Excel/CSV 的列一一对应，修改字段请同步 9_Excel 的表头。
/// </summary>
[Serializable]
public class DifficultyStageData
{
    /// <summary>阶段生效的起始时间（秒，相对 Running 开始）。</summary>
    public float startTime;

    /// <summary>单次 Step 生成 Unit 数的下限（与屏幕可容纳数的较小者取 min）。</summary>
    public int spawnMin;

    /// <summary>单次 Step 生成 Unit 数的上限。</summary>
    public int spawnMax;

    /// <summary>本阶段生成的 Unit 最大生命值。</summary>
    public int unitHp;

    /// <summary>本阶段生成的 Unit 触底伤害。</summary>
    public int unitAttack;

    /// <summary>本阶段的 Step 间隔（秒），<= 0 表示沿用 <see cref="Defines.StepInterval"/>。</summary>
    public float stepInterval;
}

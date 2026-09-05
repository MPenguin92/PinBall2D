using System;
using System.Collections.Generic;

/// <summary>
/// 单个难度阶段内的一种「刷怪等级 + 权重」：SpawnLevels 数组的一项。
/// </summary>
[Serializable]
public class SpawnLevelEntry
{
    /// <summary>刷出的 Unit 等级（Units_Level.csv 的 level 列）。</summary>
    public int level;

    /// <summary>该等级的抽取权重（越大越常出）。</summary>
    public int weight;
}

/// <summary>
/// 一个难度阶段：从 <see cref="startTime"/> 秒开始生效，直到下一阶段接替。
/// 只描述「节奏 + 刷怪密度 + 等级分布权重」，不携带怪自身数值——
/// 怪的 hp/attack/experience 由 <see cref="UnitTable"/> 按 unit 等级查询。
/// 全部字段与 Excel/CSV 的列一一对应，修改字段请同步 9_Excel 的表头。
/// </summary>
[Serializable]
public class DifficultyStageData
{
    /// <summary>阶段生效的起始时间（秒，相对 Running 开始）。</summary>
    public float startTime;

    /// <summary>
    /// 单波刷怪密度下限（百分比 0~100）：按屏幕可容纳列数换算，
    /// 如 50 = 刷半行。随难度的数量增长用百分比驱动（绝对数受屏幕宽度限制意义不大）。
    /// </summary>
    public int spawnFillMin;

    /// <summary>单波刷怪密度上限（百分比 0~100）。</summary>
    public int spawnFillMax;

    /// <summary>本阶段的 Step 间隔（秒），<= 0 表示沿用 <see cref="Defines.StepInterval"/>。</summary>
    public float stepInterval;

    /// <summary>
    /// 本阶段刷怪的等级分布（权重式）：先按 spawnFillMin~spawnFillMax 随机本波密度，
    /// 再按这里的权重为每只 roll 一个等级（如 1x60;2x30;3x10）。
    /// </summary>
    public List<SpawnLevelEntry> spawnLevels = new List<SpawnLevelEntry>();
}

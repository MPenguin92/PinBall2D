using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全局弹珠属性容器：管理每个 <see cref="BallStatType"/> 的「基础值 + Flat 修饰器 + Percent 修饰器」
/// 三段式结构。读取时 Get(t) = base * (1 + sumPct) + sumFlat。
///
/// ⚠️ 升级体系已清空（2026-09-01）：<see cref="BallStatType"/> 暂无取值，
/// 具体 stat 与按类型的钳制规则待重新设计后回填。容器与通用 API
/// （SetBase / AddFlat / AddPercent / Get）保留，供数值类升级词条（BallStatUpgradeData）写入。
/// 由 GameLogicManager 创建并持有；StartGame 时 Reset 清空到初始状态。
/// </summary>
public class BallStats
{
    private readonly Dictionary<BallStatType, float> baseValues = new Dictionary<BallStatType, float>();
    private readonly Dictionary<BallStatType, float> flatModifiers = new Dictionary<BallStatType, float>();
    private readonly Dictionary<BallStatType, float> percentModifiers = new Dictionary<BallStatType, float>();

    /// <summary>清空所有基础值与修饰器。GameLogicManager.StartGame 调用。</summary>
    public void Reset()
    {
        baseValues.Clear();
        flatModifiers.Clear();
        percentModifiers.Clear();
    }

    public void SetBase(BallStatType t, float value)
    {
        baseValues[t] = value;
    }

    public void AddFlat(BallStatType t, float value)
    {
        flatModifiers.TryGetValue(t, out float current);
        flatModifiers[t] = current + value;
    }

    public void AddPercent(BallStatType t, float value)
    {
        percentModifiers.TryGetValue(t, out float current);
        percentModifiers[t] = current + value;
    }

    /// <summary>读取最终值：base * (1 + sumPct) + sumFlat。重新设计后如需按类型钳制，在此补充。</summary>
    public float Get(BallStatType t)
    {
        baseValues.TryGetValue(t, out float baseValue);
        flatModifiers.TryGetValue(t, out float flat);
        percentModifiers.TryGetValue(t, out float pct);

        return baseValue * (1f + pct) + flat;
    }

    public int GetInt(BallStatType t)
    {
        return Mathf.Max(0, Mathf.RoundToInt(Get(t)));
    }
}

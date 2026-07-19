using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全局弹珠属性容器：管理每个 <see cref="BallStatType"/> 的「基础值 + Flat 修饰器 + Percent 修饰器」
/// 三段式结构。读取时 Get(t) = base * (1 + sumPct) + sumFlat，并按 t 自身约束钳制。
/// 由 GameLogicManager 创建并持有，PinBallBase / Player 在各自 Tick 中通过 Get 读取。
/// 升级词条通过 AddFlat / AddPercent 写入；StartGame 时 Reset 回到默认基础值。
/// </summary>
public class BallStats
{
    private readonly Dictionary<BallStatType, float> baseValues = new Dictionary<BallStatType, float>();
    private readonly Dictionary<BallStatType, float> flatModifiers = new Dictionary<BallStatType, float>();
    private readonly Dictionary<BallStatType, float> percentModifiers = new Dictionary<BallStatType, float>();

    private readonly BallStatDefaultsTable defaultsTable;

    public BallStats() : this(null) { }

    public BallStats(BallStatDefaultsTable defaultsTable)
    {
        this.defaultsTable = defaultsTable;
        Reset();
    }

    /// <summary>清空所有修饰器并把基础值恢复为默认值。GameLogicManager.StartGame 调用。</summary>
    public void Reset()
    {
        baseValues.Clear();
        flatModifiers.Clear();
        percentModifiers.Clear();

        // 优先使用 SO 表里的值,缺项回退到代码兜底,保证 SO 缺失/损坏时游戏仍可玩。
        SetBaseDefault(BallStatType.BaseDamage, 1f);
        SetBaseDefault(BallStatType.FrontHitMul, 1f);
        SetBaseDefault(BallStatType.SideHitMul, 1f);
        SetBaseDefault(BallStatType.BackHitMul, 1f);
        SetBaseDefault(BallStatType.InitialSpeed, 24f);
        SetBaseDefault(BallStatType.MinSpeed, 3f);
        SetBaseDefault(BallStatType.MaxSpeed, 0f);
        SetBaseDefault(BallStatType.BounceAccel, 0f);
        SetBaseDefault(BallStatType.BounceSpeedMul, 1f);
        SetBaseDefault(BallStatType.HitSlowdown, 0f);
        SetBaseDefault(BallStatType.PiercingChance, 0f);
        SetBaseDefault(BallStatType.PiercingKeepSpeed, 0.7f);
        SetBaseDefault(BallStatType.MaxBounces, 0f);
        SetBaseDefault(BallStatType.FireInterval, 0.3f);
    }

    private void SetBaseDefault(BallStatType t, float fallback)
    {
        if (defaultsTable != null && defaultsTable.TryGet(t, out float v))
            baseValues[t] = v;
        else
            baseValues[t] = fallback;
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

    /// <summary>
    /// 读取最终值：base * (1 + sumPct) + sumFlat，并按类型规则钳制。
    /// </summary>
    public float Get(BallStatType t)
    {
        baseValues.TryGetValue(t, out float baseValue);
        flatModifiers.TryGetValue(t, out float flat);
        percentModifiers.TryGetValue(t, out float pct);

        float raw = baseValue * (1f + pct) + flat;
        return Clamp(t, raw);
    }

    public int GetInt(BallStatType t)
    {
        return Mathf.Max(0, Mathf.RoundToInt(Get(t)));
    }

    private static float Clamp(BallStatType t, float v)
    {
        switch (t)
        {
            case BallStatType.BaseDamage:
                return Mathf.Max(1f, v);
            case BallStatType.FrontHitMul:
            case BallStatType.SideHitMul:
            case BallStatType.BackHitMul:
                return Mathf.Max(0.1f, v);
            case BallStatType.InitialSpeed:
                return Mathf.Max(1f, v);
            case BallStatType.MinSpeed:
                return Mathf.Max(0.5f, v);
            case BallStatType.MaxSpeed:
                return Mathf.Max(0f, v);
            case BallStatType.BounceAccel:
                return v;
            case BallStatType.BounceSpeedMul:
                return Mathf.Clamp(v, 0.1f, 2f);
            case BallStatType.HitSlowdown:
                return Mathf.Clamp01(v);
            case BallStatType.PiercingChance:
                return Mathf.Clamp01(v);
            case BallStatType.PiercingKeepSpeed:
                return Mathf.Clamp(v, 0.1f, 1.5f);
            case BallStatType.MaxBounces:
                return Mathf.Max(0f, v);
            case BallStatType.FireInterval:
                return Mathf.Max(0.05f, v);
            default:
                return v;
        }
    }
}

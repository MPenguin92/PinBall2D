/// <summary>
/// 弹珠机制强化参数枚举。所有数值类升级词条通过修改这些参数对全部 PinBall 生效。
/// 取值使用 BallStats.Get(StatType) = base * (1 + sumPercent) + sumFlat，再按各自规则钳制。
/// </summary>
public enum BallStatType
{
    /// <summary>球的基础伤害（实际伤害 = round(BaseDamage * dirHitMul)）。</summary>
    BaseDamage,

    /// <summary>正面命中倍率：球与 Unit 移动方向迎面对撞。默认 1。</summary>
    FrontHitMul,

    /// <summary>侧面命中倍率：球从 Unit 移动方向的侧向命中。默认 1。</summary>
    SideHitMul,

    /// <summary>背面命中倍率：球从 Unit 移动方向背后追击。默认 1。</summary>
    BackHitMul,

    /// <summary>初始发射速度（覆盖 Player.firePinBallSpeed）。</summary>
    InitialSpeed,

    /// <summary>反弹/命中后允许的最低速度（既保持手感）。</summary>
    MinSpeed,

    /// <summary>速度上限（0 视为不限）。</summary>
    MaxSpeed,

    /// <summary>每次反弹后额外加速量（flat，单位/次）。</summary>
    BounceAccel,

    /// <summary>每次反弹保留比例（默认 1.0：完全弹性）。</summary>
    BounceSpeedMul,

    /// <summary>命中 Unit 后速度衰减比例（0 表示不衰减；0.3 表示 -30%）。</summary>
    HitSlowdown,

    /// <summary>击杀 Unit 后触发穿透（跳过反弹继续直行）的概率（0~1）。</summary>
    PiercingChance,

    /// <summary>穿透成功时保留速度比例（0~1）。</summary>
    PiercingKeepSpeed,

    /// <summary>反弹次数上限：0 = 无限；&gt;0 表示反弹达到该次数后自动回收。</summary>
    MaxBounces,

    /// <summary>普通球库存上限（取代 Player.maxPinBallCount）。</summary>
    BasePinBallSlots,

    /// <summary>发射间隔（秒，下限 0.05）。</summary>
    FireInterval,
}

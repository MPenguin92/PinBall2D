using UnityEngine;

/// <summary>
/// 弹珠（Ball）领域事件总线：广播球在生命周期中的关键时机。
/// 这是升级 / 词条效果的底层支持——效果只订阅事件，不改动核心循环。
///
/// 触发顺序约定（单次命中单元时）：
///   命中（OnHitUnit）→ 若摧毁目标则（OnKilledUnit）→ 反弹（OnBounced）
/// 返回（OnReturned）在球触底被回收前触发，回收后球实例不再有效。
///
/// 事件使用只读 struct 上下文，Raise 时零 GC 分配（场上球多、反弹高频）。
/// 订阅/退订由持有方负责（GameStart 订阅、游戏结束/回到主页退订）。
///
/// ⚠️ 当前仅提供时机，尚无任何订阅方；后续升级效果系统在此挂载。
/// </summary>
public static class BallEvents
{
    // ---- 事件声明 ----

    /// <summary>发射时：Player 成功生成一颗弹珠后触发。</summary>
    public static event System.Action<BallFiredContext> OnFired;

    /// <summary>命中时：球击中任一 Unit 后触发（无论是否摧毁；Killed 表示是否击杀）。</summary>
    public static event System.Action<BallHitContext> OnHitUnit;

    /// <summary>恰好击杀时：命中且该 Unit 被摧毁后触发（在回收 unit 前，引用仍有效）。</summary>
    public static event System.Action<BallHitContext> OnKilledUnit;

    /// <summary>反弹时：球发生反射后触发（边框反弹与命中 Unit 后的反弹都算）。</summary>
    public static event System.Action<BallBouncedContext> OnBounced;

    /// <summary>返回时：球触底消失、被回收前触发。</summary>
    public static event System.Action<BallReturnedContext> OnReturned;

    // ---- Raise（触发点只允许调用这些）----

    public static void RaiseFired(PinBallBase ball, Vector2 position, Vector2 direction, float speed)
    {
        if (OnFired == null) return;
        OnFired(new BallFiredContext(ball, position, direction, speed));
    }

    public static void RaiseHitUnit(PinBallBase ball, UnitBase target, Vector2 position, Vector2 normal,
        HitDirection direction, bool killed)
    {
        if (OnHitUnit == null) return;
        OnHitUnit(new BallHitContext(ball, target, position, normal, direction, killed));
    }

    public static void RaiseKilledUnit(PinBallBase ball, UnitBase target, Vector2 position, Vector2 normal,
        HitDirection direction)
    {
        if (OnKilledUnit == null) return;
        OnKilledUnit(new BallHitContext(ball, target, position, normal, direction, true));
    }

    public static void RaiseBounced(PinBallBase ball, Vector2 position, Vector2 normal)
    {
        if (OnBounced == null) return;
        OnBounced(new BallBouncedContext(ball, position, normal));
    }

    public static void RaiseReturned(PinBallBase ball, Vector2 position)
    {
        if (OnReturned == null) return;
        OnReturned(new BallReturnedContext(ball, position));
    }
}

// ---- 事件上下文（只读 struct，Raise 时零分配）----

/// <summary>发射时机上下文。</summary>
public readonly struct BallFiredContext
{
    /// <summary>被发射的球实例（刚 Spawn，Init 已完成）。</summary>
    public readonly PinBallBase Ball;

    /// <summary>发射位置（炮口世界坐标）。</summary>
    public readonly Vector2 Position;

    /// <summary>发射方向（单位向量）。</summary>
    public readonly Vector2 Direction;

    /// <summary>发射初速。</summary>
    public readonly float Speed;

    public BallFiredContext(PinBallBase ball, Vector2 position, Vector2 direction, float speed)
    {
        Ball = ball;
        Position = position;
        Direction = direction;
        Speed = speed;
    }
}

/// <summary>命中 / 击杀时机共享上下文。</summary>
public readonly struct BallHitContext
{
    /// <summary>发起命中的球实例。</summary>
    public readonly PinBallBase Ball;

    /// <summary>被命中的目标（回收前引用有效）。</summary>
    public readonly UnitBase Target;

    /// <summary>命中位置（球圆心）。</summary>
    public readonly Vector2 Position;

    /// <summary>命中边法线。</summary>
    public readonly Vector2 Normal;

    /// <summary>命中方向桶（Front/Side/Back）。</summary>
    public readonly HitDirection Direction;

    /// <summary>该次命中是否恰好摧毁目标（OnKilledUnit 恒为 true）。</summary>
    public readonly bool Killed;

    public BallHitContext(PinBallBase ball, UnitBase target, Vector2 position, Vector2 normal,
        HitDirection direction, bool killed)
    {
        Ball = ball;
        Target = target;
        Position = position;
        Normal = normal;
        Direction = direction;
        Killed = killed;
    }
}

/// <summary>反弹时机上下文。</summary>
public readonly struct BallBouncedContext
{
    /// <summary>反弹的球实例。</summary>
    public readonly PinBallBase Ball;

    /// <summary>反弹发生位置。</summary>
    public readonly Vector2 Position;

    /// <summary>反射法线（决定新速度方向）。</summary>
    public readonly Vector2 Normal;

    public BallBouncedContext(PinBallBase ball, Vector2 position, Vector2 normal)
    {
        Ball = ball;
        Position = position;
        Normal = normal;
    }
}

/// <summary>返回（触底回收）时机上下文。</summary>
public readonly struct BallReturnedContext
{
    /// <summary>即将被回收的球实例。</summary>
    public readonly PinBallBase Ball;

    /// <summary>触底位置。</summary>
    public readonly Vector2 Position;

    public BallReturnedContext(PinBallBase ball, Vector2 position)
    {
        Ball = ball;
        Position = position;
    }
}

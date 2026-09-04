using UnityEngine;

/// <summary>
/// 发射执行器：Player 提供给 <see cref="FireStrategy"/> 的发射能力接口。
/// 策略只描述「怎么发」（颗数 / 时序 / 角度），不关心球地址、位置、速度等细节。
/// </summary>
public interface IFireExecutor
{
    /// <summary>本次射击的基准方向（Player 当前瞄准方向快照；扇形等策略在其上做角度偏转）。</summary>
    Vector2 BaseDirection { get; }

    /// <summary>沿 <paramref name="direction"/> 生成一颗球并广播 BallEvents.OnFired（生成成功时）。</summary>
    void SpawnBall(Vector2 direction);

    /// <summary>延迟 <paramref name="seconds"/> 秒后执行 <paramref name="action"/>（供连发策略控制先后节奏）。</summary>
    void Delay(float seconds, System.Action action);
}

/// <summary>
/// 发射策略基类：决定「一次射击输入」产出弹幕的形态。
/// 由 Player 持有当前策略并执行；升级词条可通过 Player.SetFireStrategy 替换，
/// 以实现单发 / 双发 / 三连发 / 扇形散射等不同射击模式。
///
/// 约定：策略产出的每一颗球都由 executor.SpawnBall 生成并广播 OnFired——
/// OnFired 表示「本次射击产出的球」，派生弹不广播的规则由 executor 实现保证。
/// </summary>
public abstract class FireStrategy
{
    /// <summary>执行一次射击（由 Player 在冷却结束、方向已锁定时调用）。</summary>
    public abstract void Fire(IFireExecutor executor);
}

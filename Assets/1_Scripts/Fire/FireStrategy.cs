using UnityEngine;

/// <summary>
/// 一次射击中的「一颗弹」：指定球型（Balls.csv 的 id）与该球等级
/// （Balls_Level.csv 决定该级伤害等数值）。
/// </summary>
public readonly struct FireShot
{
    /// <summary>球类型 id（Balls.csv 第一列）。</summary>
    public readonly string BallId;

    /// <summary>该球使用的等级（&gt;=1；查 Balls_Level.csv）。</summary>
    public readonly int Level;

    public FireShot(string ballId, int level)
    {
        BallId = ballId;
        Level = level;
    }

    /// <summary>基础普通弹 Lv1（Player 默认射击、单发/扇形用）。</summary>
    public static FireShot Base => new FireShot(Defines.BallBaseId, 1);
}

/// <summary>
/// 发射执行器：Player 提供给 <see cref="FireStrategy"/> 的发射能力接口。
/// 策略只描述「怎么发」（哪颗球、时序、角度），不关心 prefab 地址/位置/速度等细节。
/// </summary>
public interface IFireExecutor
{
    /// <summary>本次射击的基准方向（Player 当前瞄准方向快照；扇形等策略在其上做角度偏转）。</summary>
    Vector2 BaseDirection { get; }

    /// <summary>沿 <paramref name="direction"/> 生成一颗 <paramref name="shot"/> 指定的球并广播 BallEvents.OnFired。</summary>
    void SpawnBall(Vector2 direction, FireShot shot);

    /// <summary>延迟 <paramref name="seconds"/> 秒后执行 <paramref name="action"/>（供连发策略控制先后节奏）。</summary>
    void Delay(float seconds, System.Action action);
}

/// <summary>
/// 发射策略基类：决定「一次射击输入」产出哪些弹（发射序列）。
/// 由 Player 持有当前策略并执行；升级词条可通过 Player.SetFireStrategy 替换，
/// 例如连发 = 主弹 + 若干副弹的序列。
///
/// 约定：策略产出的每一颗球都由 executor.SpawnBall 生成并广播 OnFired——
/// OnFired 表示「本次射击产出的球」，派生弹不广播的规则由 executor 实现保证。
/// </summary>
public abstract class FireStrategy
{
    /// <summary>执行一次射击（由 Player 在冷却结束、方向已锁定时调用）。</summary>
    public abstract void Fire(IFireExecutor executor);
}

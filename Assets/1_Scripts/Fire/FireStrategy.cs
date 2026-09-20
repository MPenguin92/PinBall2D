using System.Collections.Generic;
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
/// 一次射击的预览（供 HUD 弹舱队列）：弹舱一格只可能是「一颗普通 base 球」或
/// 「一个能力」。<see cref="IsAbility"/> 区分二者；能力格显示 <see cref="AbilityIcon"/>
/// （Upgrades.csv icon 列配置的图标 key，可能为空则占位）。
/// 能力实际发射多少颗由策略 Fire 时决定，预览不关心、无多颗概念。
/// </summary>
public readonly struct FirePreview
{
    /// <summary>是否能力射击（true=连发等能力接管；false=普通单发 base 球）。</summary>
    public readonly bool IsAbility;

    /// <summary>能力词条的展示图标 key（Upgrades.csv icon 列；普通单发为空）。</summary>
    public readonly string AbilityIcon;

    public FirePreview(bool isAbility, string abilityIcon = null)
    {
        IsAbility = isAbility;
        AbilityIcon = abilityIcon;
    }

    /// <summary>普通单发预览（一颗 base 球）。</summary>
    public static FirePreview Single => new FirePreview(false);
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
/// 由 <see cref="FireAbilityManager"/> 持有的能力引用并执行；升级词条通过
/// Player.AddFireAbility 注册为射击能力（如连发 = 主弹 + 若干副弹的序列）。
///
/// 约定：策略产出的每一颗球都由 executor.SpawnBall 生成并广播 OnFired——
/// OnFired 表示「本次射击产出的球」，派生弹不广播的规则由 executor 实现保证。
/// </summary>
public abstract class FireStrategy
{
    /// <summary>执行一次射击（由 Player 在冷却结束、方向已锁定时调用）。</summary>
    public abstract void Fire(IFireExecutor executor);

    /// <summary>
    /// 预览一次射击（不实际发射）：单发策略返回普通单发；能力策略返回
    /// IsAbility=true 及其弹序。供 HUD 弹舱队列（一格 = 一次射击）使用。
    /// </summary>
    public abstract FirePreview PreviewShot();
}

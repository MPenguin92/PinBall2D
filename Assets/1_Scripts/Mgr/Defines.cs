/// <summary>
/// 项目级常量集中定义。任何需要在多个模块共享的"魔法数字"都放在这里。
/// </summary>
public static class Defines
{
    /// <summary>Unit 的标准尺寸（正方形边长，单位：米）。</summary>
    public const float UnitSize = 1f;

    /// <summary>单次 Step 的移动距离（米），与 <see cref="UnitSize"/> 保持一致。</summary>
    public const float StepDistance = UnitSize;

    /// <summary>相邻两次 Step 事件之间的时间间隔（秒）。</summary>
    public const float StepInterval = 1f;

    /// <summary>单次 Step 的移动时长：从起点平滑插值到目标位置所用秒数。</summary>
    public const float StepMoveDuration = 0.2f;

    /// <summary>金币怪（unit_gold）独立刷新的间隔（秒）。</summary>
    public const float GoldSpawnInterval = 8f;

    // 单位类型 id（Units.csv 第一列）。
    /// <summary>普通怪（吃伤害型）。</summary>
    public const string UnitDamageId = "unit_damage";

    /// <summary>金币怪（击杀掉落金币）。</summary>
    public const string UnitGoldId = "unit_gold";

    /// <summary>宝箱怪（经验里程碑达成时刷出，击杀获得一次升级机会）。</summary>
    public const string UnitChestId = "unit_chest";

    // 球类型 id（Balls.csv 第一列）。
    /// <summary>基础普通弹（Player 默认射击、连发主弹）。</summary>
    public const string BallBaseId = "base";

    /// <summary>连发副弹（伤害随 Balls_Level 等级提升）。</summary>
    public const string BallSubId = "ball_sub";
}

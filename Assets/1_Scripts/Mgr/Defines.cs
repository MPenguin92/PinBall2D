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
}

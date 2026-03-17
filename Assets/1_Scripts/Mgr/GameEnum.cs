// 通用游戏枚举，集中定义项目中使用到的所有枚举。

/// <summary>边框反弹法线方向，供 Border 与反射计算使用。</summary>
public enum BounceDirection
{
    Up,
    Down,
    Left,
    Right
}

/// <summary>游戏状态，用于控制主逻辑 Update 是否运行。</summary>
public enum GameState
{
    /// <summary>准备中（未开始或初始化阶段）</summary>
    Preparing,

    /// <summary>运行中（正常游戏循环）</summary>
    Running,

    /// <summary>暂停</summary>
    Paused,

    /// <summary>结束</summary>
    Ended
}

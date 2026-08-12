/// <summary>
/// 弹珠类型。Player 的弹珠库存是一个全局 FIFO 队列，每个元素都带类型标签；
/// Base 在 StartGame 时入队 N 个（Player.initialBallCount）。
/// 特殊球种体系正在重新设计，后续扩展在此追加枚举值。
/// </summary>
public enum BallType
{
    Base = 0,
}
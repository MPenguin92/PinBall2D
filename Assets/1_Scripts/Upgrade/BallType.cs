/// <summary>
/// 弹珠类型。Player 的弹珠库存是一个全局 FIFO 队列，每个元素都带类型标签；
/// Base 在 StartGame 时入队 N 个（Player.initialBallCount），其他类型默认 0，由升级入队解锁。
/// </summary>
public enum BallType
{
    Base = 0,
    Fire = 1,
    Ice = 2,
    Lightning = 3,
    Poison = 4,
    Heavy = 5,
    Boomerang = 6,
}

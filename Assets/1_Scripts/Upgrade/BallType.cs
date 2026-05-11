/// <summary>
/// 弹珠类型（库存槽位的 key）。
/// 普通球库存上限由 BallStats.BasePinBallSlots 决定；其他类型默认 0/0，由升级解锁。
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

/// <summary>
/// 默认 Unit 类型:全部行为(向下移动 + 减速 + 队列堵塞 + 触底)继承自 <see cref="UnitBase"/>。
/// 保留这个空壳是因为 SimpleUnit.prefab / Addressables 地址 "SimpleUnit" 仍指向此类;
/// 后续如有不同节奏/移动方向的 Unit,在此处或新派生类里 override <c>HandleStep</c> / <c>MoveDirection</c>。
/// </summary>
public class SimpleUnit : UnitBase
{
}

using System;

/// <summary>
/// 单位生成器接口。实现类通过订阅 <see cref="GameEvents"/> 自驱：
/// - <see cref="GameEvents.OnStep"/> 触发生成；
/// - <see cref="GameEvents.OnGameStart"/> / <see cref="GameEvents.OnGameEnd"/> 等控制启停。
/// GameLogicManager 不再调用任何驱动方法，只在销毁时 Dispose。
/// </summary>
public interface IUnitCreator : IDisposable
{
}

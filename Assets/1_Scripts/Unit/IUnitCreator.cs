using System;

/// <summary>
/// 单位生成器接口。实现类通过订阅 <see cref="GameEvents"/> 自驱：
/// - <see cref="GameEvents.OnStep"/> 之后由 GameLogicManager 调用 <see cref="SpawnStep"/> 生成新一行；
/// - <see cref="GameEvents.OnGameStart"/> / <see cref="GameEvents.OnGameEnd"/> 等控制启停。
/// GameLogicManager 在销毁时 Dispose。
/// </summary>
public interface IUnitCreator : IDisposable
{
    /// <summary>在一个 Step 节拍内、所有 Unit 完成移动决策后调用，生成顶部新一行。</summary>
    void SpawnStep();
}

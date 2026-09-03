using System;

/// <summary>
/// 单位生成器接口。实现类通过订阅 <see cref="GameEvents"/> 自驱：
/// - <see cref="GameEvents.OnStep"/> 之后由 GameLogicManager 调用 <see cref="SpawnStep"/> 生成新一行；
/// - <see cref="GameEvents.OnGameStart"/> / <see cref="GameEvents.OnGameEnd"/> 等控制启停。
/// GameLogicManager 在销毁时 Dispose。
/// </summary>
public interface IUnitCreator : IDisposable
{
    /// <summary>
    /// 在一个 Step 节拍内、所有 Unit 完成移动决策后调用，生成顶部新一行。
    /// <paramref name="allowGoldReplace"/> 表示金币冷却就绪（本波可替换少量普通怪为金币怪）；
    /// <paramref name="allowChestReplace"/> 表示经验里程碑达成（本波可替换一只普通怪为宝箱怪）。
    /// 具体替换数量与等级由实现决定。
    /// </summary>
    void SpawnStep(bool allowGoldReplace, bool allowChestReplace);
}

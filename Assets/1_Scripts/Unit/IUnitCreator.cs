/// <summary>
/// 单位生成器接口。生命周期相关响应由实现类自行订阅 <see cref="GameEvents"/>，
/// GameLogicManager 只负责每帧驱动其 <see cref="Tick"/>。
/// </summary>
public interface IUnitCreator
{
    /// <summary>游戏运行中每帧调用：推进计时与生成。</summary>
    void Tick();
}

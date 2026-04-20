using System;

/// <summary>
/// 游戏生命周期事件总线。任何系统都可以订阅感兴趣的事件。
/// 只有 GameLogicManager 负责在适当时机触发（Raise）这些事件，
/// 其他模块（UIManager、UnitCreator 等）通过订阅来响应，避免直接耦合。
/// </summary>
public static class GameEvents
{
    /// <summary>游戏开始（进入 Running 状态）。</summary>
    public static event Action OnGameStart;

    /// <summary>游戏暂停（从 Running 进入 Paused）。</summary>
    public static event Action OnGamePause;

    /// <summary>游戏从暂停恢复（从 Paused 进入 Running）。</summary>
    public static event Action OnGameResume;

    /// <summary>游戏结束（Player 死亡或主动结束）。</summary>
    public static event Action OnGameEnd;

    /// <summary>回到主页（从 Ended 回到 Preparing，等待重新开始）。</summary>
    public static event Action OnReturnToHome;

    public static void RaiseGameStart() => OnGameStart?.Invoke();
    public static void RaiseGamePause() => OnGamePause?.Invoke();
    public static void RaiseGameResume() => OnGameResume?.Invoke();
    public static void RaiseGameEnd() => OnGameEnd?.Invoke();
    public static void RaiseReturnToHome() => OnReturnToHome?.Invoke();
}

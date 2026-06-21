using System;
using System.Collections.Generic;

/// <summary>
/// 游戏生命周期事件总线。任何系统都可以订阅感兴趣的事件。
/// 只有 GameLogicManager / Service 类负责在适当时机触发（Raise）这些事件，
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

    /// <summary>节奏心跳：Running 状态下 GameLogicManager 每 <see cref="Defines.StepInterval"/> 秒触发一次。
    /// Unit 用它推进移动、UnitCreator 用它生成新一批。</summary>
    public static event Action OnStep;

    /// <summary>Unit 被弹珠击杀时触发（PinBallBase.Tick 中销毁 Unit 之前 Raise）。</summary>
    public static event Action<UnitBase> OnUnitKilled;

    /// <summary>累计击杀达到一个里程碑（参数为里程碑表中的索引）。</summary>
    public static event Action<int> OnKillMilestoneReached;

    /// <summary>三选一抽卡完成，向 UI 推送候选；UI 显示面板等待选择。</summary>
    public static event Action<IList<UpgradeBase>> OnUpgradeOffered;

    /// <summary>玩家选中并应用了某个升级；UI 关闭面板，逻辑恢复 Running。</summary>
    public static event Action<UpgradeBase> OnUpgradeApplied;

    public static void RaiseGameStart() => OnGameStart?.Invoke();
    public static void RaiseGamePause() => OnGamePause?.Invoke();
    public static void RaiseGameResume() => OnGameResume?.Invoke();
    public static void RaiseGameEnd() => OnGameEnd?.Invoke();
    public static void RaiseReturnToHome() => OnReturnToHome?.Invoke();
    public static void RaiseStep() => OnStep?.Invoke();
    public static void RaiseUnitKilled(UnitBase unit) => OnUnitKilled?.Invoke(unit);
    public static void RaiseKillMilestoneReached(int milestoneIdx) => OnKillMilestoneReached?.Invoke(milestoneIdx);
    public static void RaiseUpgradeOffered(IList<UpgradeBase> options) => OnUpgradeOffered?.Invoke(options);
    public static void RaiseUpgradeApplied(UpgradeBase upgrade) => OnUpgradeApplied?.Invoke(upgrade);

    /// <summary>是否有 UI 订阅了升级三选一（用于无面板时的兜底逻辑）。</summary>
    public static bool HasUpgradeOfferedListeners =>
        OnUpgradeOffered != null && OnUpgradeOffered.GetInvocationList().Length > 0;
}

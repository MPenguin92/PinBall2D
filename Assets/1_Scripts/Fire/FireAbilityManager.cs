using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一次射击能力的 CD 状态：<see cref="CdRemaining"/> 每次发射普通弹时 -1，
/// 归 0 表示「下一次普通弹触发」。
/// </summary>
public class FireAbilityState
{
    /// <summary>能力触发后执行的射击策略（如连发 = 主弹 + 副弹序列）。</summary>
    public FireStrategy Strategy { get; }

    /// <summary>CD 上限（每次触发后重置为它）。</summary>
    public int MaxCd { get; }

    /// <summary>剩余 CD：每发射一颗普通弹 -1；0 表示就绪（等待触发）。</summary>
    public int CdRemaining { get; private set; }

    public FireAbilityState(FireStrategy strategy, int maxCd)
    {
        Strategy = strategy;
        MaxCd = Mathf.Max(1, maxCd);
        CdRemaining = MaxCd;
    }

    /// <summary>发射普通球（未被任何能力触发的那一次）：CD 递减，0 保持 0（等待下次触发）。</summary>
    public void OnNormalShot()
    {
        if (CdRemaining <= 0) return;
        CdRemaining--;
    }

    /// <summary>触发本次能力：执行策略并重置 CD。</summary>
    public void Trigger(IFireExecutor executor)
    {
        if (Strategy != null)
            Strategy.Fire(executor);
        CdRemaining = MaxCd;
    }
}

/// <summary>
/// 射击能力 CD 统一管理：持有所有已解锁的射击能力，每次发射普通弹时统一推进 CD，
/// 并在「就绪」的能力中挑选触发（保证每颗普通弹最多只触发一个能力）。
///
/// 规则：
///   - 每次射击先检查就绪能力（CdRemaining == 0），有则触发第一个（本次被能力接管，
///     所有能力 CD 都不推进）；
///   - 只有**没有**能力触发、正常发射普通球的那一次，才推进所有能力 CD（OnNormalShot）；
///   - 多个能力同时就绪时本次只触发第一个，其余保持 0，下次普通球再触发下一个。
/// </summary>
public class FireAbilityManager
{
    private readonly List<FireAbilityState> abilities = new List<FireAbilityState>();

    public int Count => abilities.Count;

    /// <summary>添加或更新一个能力（同策略实例视为同能力，更新 CD 上限并重置）。</summary>
    public void AddAbility(FireStrategy strategy, int maxCd)
    {
        if (strategy == null) return;

        for (int i = 0; i < abilities.Count; i++)
        {
            if (abilities[i].Strategy == strategy)
            {
                abilities[i] = new FireAbilityState(strategy, maxCd);
                return;
            }
        }
        abilities.Add(new FireAbilityState(strategy, maxCd));
    }

    /// <summary>清空所有能力（每局 Init 时调用）。</summary>
    public void Reset()
    {
        abilities.Clear();
    }

    /// <summary>
    /// 发射一颗普通球的完整流程：
    ///   先检查是否有就绪（CdRemaining == 0）能力 → 有则触发第一个（其余能力 CD 保持不变）；
    ///   没有触发（本次发射普通球）→ 才推进所有能力 CD（-1）。
    /// 返回 true 表示本次被某个能力接管（不发射普通球）；false 表示正常发射普通球。
    /// </summary>
    public bool OnNormalShot(IFireExecutor executor)
    {
        // 先找就绪能力：命中即触发，CD 重置；本次不推进任何其他能力。
        for (int i = 0; i < abilities.Count; i++)
        {
            if (abilities[i].CdRemaining <= 0)
            {
                abilities[i].Trigger(executor);
                return true;
            }
        }

        // 无能力触发：本次为普通球，推进所有能力 CD。
        for (int i = 0; i < abilities.Count; i++)
            abilities[i].OnNormalShot();

        return false;
    }
}

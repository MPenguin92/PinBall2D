using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Roguelike 升级服务：击杀累积经验，达到里程碑时**只记账不弹窗**（获得一次可升级次数），
/// 玩家点 HUD 宝箱按钮后经 <see cref="TryBeginOffer"/> 才进入抽卡。
/// 流程：监听 <see cref="GameEvents.OnUnitKilled"/> -> 把 unit.Experience 加进累计 -> 跨阈值 -> 
///       pendingMilestones 入队 + <see cref="GameEvents.OnKillMilestoneReached"/>（HUD 刷新宝箱角标） ->
///       玩家点宝箱 <see cref="TryBeginOffer"/> -> 按权重抽品质 -> 在该品质池中无放回抽 3 张 ->
///       <see cref="GameEvents.RaiseUpgradeOffered"/>（UI 显示） -> 玩家点选 <see cref="ApplySelected"/>。
/// 选完一张后若仍有剩余次数，不关闭面板，重新抽 3 张替换继续选；全部用完才收尾恢复 Running。
/// 由 <see cref="GameLogicManager"/> 在 Awake 创建并持有，StartGame 时 Reset。
/// </summary>
public class UpgradeService
{
    private readonly KillMilestoneTable milestoneTable;
    private readonly UpgradeCatalog catalog;
    private readonly UpgradeContext context;

    private readonly List<UpgradeBase> currentOffer = new List<UpgradeBase>(3);
    private readonly Queue<int> pendingMilestones = new Queue<int>();
    private int experienceAccumulated;
    private int nextMilestoneIdx;
    private bool isOffering;

    public int ExperienceAccumulated => experienceAccumulated;

    public int NextMilestoneIdx => nextMilestoneIdx;

    public bool IsOffering => isOffering;

    /// <summary>当前累积、尚未消费的升级次数（HUD 宝箱角标读取）。</summary>
    public int PendingUpgradeCount => pendingMilestones.Count;

    public IReadOnlyList<UpgradeBase> CurrentOffer => currentOffer;

    public UpgradeService(
        KillMilestoneTable milestoneTable,
        UpgradeCatalog catalog,
        BallStats stats,
        Player player)
    {
        this.milestoneTable = milestoneTable;
        this.catalog = catalog;
        this.context = new UpgradeContext
        {
            Stats = stats,
            Player = player,
        };
    }

    public void RegisterEvents()
    {
        GameEvents.OnUnitKilled += HandleUnitKilled;
    }

    public void UnregisterEvents()
    {
        GameEvents.OnUnitKilled -= HandleUnitKilled;
    }

    /// <summary>StartGame 时调用：清零累计经验、里程碑索引、待消费次数、堆叠状态与候选。</summary>
    public void Reset()
    {
        experienceAccumulated = 0;
        nextMilestoneIdx = 0;
        isOffering = false;
        currentOffer.Clear();
        pendingMilestones.Clear();

        if (catalog != null)
        {
            for (int i = 0; i < catalog.Entries.Count; i++)
            {
                if (catalog.Entries[i] != null)
                    catalog.Entries[i].ResetRuntimeState();
            }
        }
    }

    /// <summary>
    /// 玩家点击 HUD 宝箱：进入抽卡（当前这一组的次数暂不消费，选完才扣）。
    /// 成功抽到候选后返回 true；抽卡失败（池空等）返回 false，次数仍保留。
    /// </summary>
    public bool TryBeginOffer()
    {
        if (isOffering) return false;
        if (pendingMilestones.Count == 0) return false;

        return RollAndOffer(pendingMilestones.Peek());
    }

    /// <summary>玩家点击三选一面板上的某张卡：消费本次次数并应用升级；若还有剩余次数则重抽下一组继续选，否则收尾恢复 Running。</summary>
    public void ApplySelected(UpgradeBase chosen)
    {
        if (!isOffering || chosen == null) return;
        if (!currentOffer.Contains(chosen)) return;

        chosen.Apply(context);
        chosen.IncrementLevel();

        // 消费当前这一组的升级次数。
        pendingMilestones.Dequeue();

        currentOffer.Clear();
        isOffering = false;

        // 还有剩余升级次数：不关闭面板，直接抽下一组候选替换内容。
        if (pendingMilestones.Count > 0)
        {
            if (RollAndOffer(pendingMilestones.Peek()))
                return;
        }

        GameEvents.RaiseUpgradeApplied(chosen);

        // 应用完之后从 SelectingUpgrade 回到 Running。
        if (GameLogicManager.Instance != null)
            GameLogicManager.Instance.ResumeFromUpgradeSelection();
    }

    private void HandleUnitKilled(UnitBase unit)
    {
        // 单个 Unit 的经验值由其所在 Difficulty 阶段决定;Init 已写入 unit.Experience。
        // 兜底:不传 Unit 或经验<=0 时按 1 计,避免完全不累积。
        int gain = (unit != null && unit.Experience > 0) ? unit.Experience : 1;
        experienceAccumulated += gain;

        if (milestoneTable == null || milestoneTable.Count == 0) return;

        // 一次击杀可能跨多个里程碑：全部入队记账，弹窗时机交给玩家（宝箱按钮）。
        while (nextMilestoneIdx < milestoneTable.Count)
        {
            int threshold = milestoneTable.GetThresholdAt(nextMilestoneIdx);
            if (threshold <= 0)
            {
                Debug.LogError($"[UpgradeService] Milestone {nextMilestoneIdx} has invalid experienceThreshold={threshold}. " +
                               "Re-import KillMilestones.csv via Tools/Data/Import All.");
                nextMilestoneIdx++;
                continue;
            }
            if (experienceAccumulated < threshold) break;

            int reachedIdx = nextMilestoneIdx;
            nextMilestoneIdx++;
            pendingMilestones.Enqueue(reachedIdx);
            GameEvents.RaiseKillMilestoneReached(reachedIdx);
        }
    }

    /// <summary>按指定里程碑权重抽卡并推送 UI；无候选返回 false。</summary>
    private bool RollAndOffer(int milestoneIdx)
    {
        if (catalog == null || catalog.Count == 0) return false;

        KillMilestoneData weights = milestoneTable.GetWeightsAt(milestoneIdx);
        if (weights == null) return false;

        UpgradeRarity rolledRarity = RollRarity(weights);

        // 按品质从高到低依次降级保底，直到凑够 3 张或全部品质为空。
        List<UpgradeBase> picked = PickThree(rolledRarity);
        if (picked.Count == 0) return false;

        currentOffer.Clear();
        currentOffer.AddRange(picked);
        isOffering = true;

        GameEvents.RaiseUpgradeOffered(currentOffer);

        // 无订阅方时自动选第一项，避免卡死在 SelectingUpgrade。
        if (isOffering && !GameEvents.HasUpgradeOfferedListeners)
        {
            Debug.LogWarning("[UpgradeService] OnUpgradeOffered has no listeners (UpgradeSelectionUI missing in scene). " +
                             "Auto-selecting first upgrade.");
            ApplySelected(currentOffer[0]);
        }

        return true;
    }

    private static UpgradeRarity RollRarity(KillMilestoneData w)
    {
        int total = Mathf.Max(0, w.weightCommon)
                  + Mathf.Max(0, w.weightUncommon)
                  + Mathf.Max(0, w.weightRare)
                  + Mathf.Max(0, w.weightLegendary);
        if (total <= 0) return UpgradeRarity.Common;

        int roll = Random.Range(0, total);
        int acc = 0;
        acc += Mathf.Max(0, w.weightCommon);
        if (roll < acc) return UpgradeRarity.Common;
        acc += Mathf.Max(0, w.weightUncommon);
        if (roll < acc) return UpgradeRarity.Uncommon;
        acc += Mathf.Max(0, w.weightRare);
        if (roll < acc) return UpgradeRarity.Rare;
        return UpgradeRarity.Legendary;
    }

    private List<UpgradeBase> PickThree(UpgradeRarity preferred)
    {
        List<UpgradeBase> result = new List<UpgradeBase>(3);
        // 按品质降级保底顺序：先从抽到的品质往下找；若它已耗尽再往上一级（向 Common 方向）凑。
        // 这里采用 [preferred, preferred-1, ..., Common, preferred+1, ...] 的顺序。
        UpgradeRarity[] order = BuildFallbackOrder(preferred);
        for (int i = 0; i < order.Length && result.Count < 3; i++)
        {
            DrawFromPool(order[i], result);
        }

        // 全空兜底：若仍不足 3 张，仅返回当前已有的（即使为空）。
        return result;
    }

    private UpgradeRarity[] BuildFallbackOrder(UpgradeRarity preferred)
    {
        // 例如 preferred=Rare(2): [Rare, Uncommon, Common, Legendary]
        List<UpgradeRarity> order = new List<UpgradeRarity>(4);
        for (int r = (int)preferred; r >= 0; r--) order.Add((UpgradeRarity)r);
        for (int r = (int)preferred + 1; r <= (int)UpgradeRarity.Legendary; r++) order.Add((UpgradeRarity)r);
        return order.ToArray();
    }

    private void DrawFromPool(UpgradeRarity rarity, List<UpgradeBase> result)
    {
        List<UpgradeBase> pool = new List<UpgradeBase>();
        for (int i = 0; i < catalog.Entries.Count; i++)
        {
            UpgradeBase u = catalog.Entries[i];
            if (u == null) continue;
            if (u.Rarity != rarity) continue;
            if (u.IsFull) continue;
            if (result.Contains(u)) continue;
            pool.Add(u);
        }

        // 无放回 Fisher–Yates：从 pool 中抽 (3 - result.Count) 张。
        int need = 3 - result.Count;
        for (int i = 0; i < need && pool.Count > 0; i++)
        {
            int idx = Random.Range(0, pool.Count);
            result.Add(pool[idx]);
            pool[idx] = pool[pool.Count - 1];
            pool.RemoveAt(pool.Count - 1);
        }
    }
}

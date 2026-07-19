using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 闪电球：命中 Unit 后对最近的 N 个 Unit 链式跳跃，每跳衰减伤害。
///
/// 参数（来自 SpecialBallParams[BallType.Lightning]）：
/// - chainCount       : 跳跃目标数（不含主命中）
/// - chainCountAdd    : 累加目标数
/// - chainDecay       : 每跳衰减比例（0~1；0 = 不衰减；0.3 = 每跳 -30%）
/// - chainRange       : 单跳搜索半径（米）
/// </summary>
public class LightningPinBall : PinBallBase
{
    private readonly List<UnitBase> visited = new List<UnitBase>();

    protected override void OnHitUnit(UnitBase unit, Vector2 hitPos, Vector2 hitNormal, HitDirection dir, bool destroyed)
    {
        SpecialBallParams sp = GameLogicManager.Instance != null ? GameLogicManager.Instance.SpecialBallParams : null;
        if (sp == null) return;

        int chainCount = Mathf.Max(0, Mathf.RoundToInt(sp.Get(BallType.Lightning, "chainCount")));
        if (chainCount <= 0) return;

        float decay = Mathf.Clamp01(sp.Get(BallType.Lightning, "chainDecay"));
        float range = sp.Get(BallType.Lightning, "chainRange");
        if (range <= 0f) return;

        var activeUnits = GameLogicManager.Instance.ActiveUnits;
        if (activeUnits == null) return;

        BallStats stats = GameLogicManager.Instance.BallStats;
        float baseDmg = stats != null ? stats.Get(BallStatType.BaseDamage) : 1f;

        visited.Clear();
        if (unit != null) visited.Add(unit);

        Vector2 from = hitPos;
        float currentDmg = baseDmg;

        for (int hop = 0; hop < chainCount; hop++)
        {
            currentDmg *= (1f - decay);
            int dmg = Mathf.Max(1, Mathf.RoundToInt(currentDmg));

            UnitBase next = FindNearest(activeUnits, from, range, visited);
            if (next == null) break;

            visited.Add(next);
            bool destroyedByChain = next.TakeDamage(dmg, BallType.Lightning);
            if (destroyedByChain)
            {
                GameEvents.RaiseUnitKilled(next);
                GameLogicManager.Instance.RecycleUnit(next);
            }

            from = next.transform.position;
        }
    }

    private static UnitBase FindNearest(IReadOnlyList<UnitBase> units, Vector2 from, float range, List<UnitBase> exclude)
    {
        UnitBase best = null;
        float bestSqr = range * range;
        for (int i = 0; i < units.Count; i++)
        {
            UnitBase u = units[i];
            if (u == null || !u.gameObject.activeSelf) continue;
            if (exclude.Contains(u)) continue;

            float sqr = ((Vector2)u.transform.position - from).sqrMagnitude;
            if (sqr > bestSqr) continue;

            best = u;
            bestSqr = sqr;
        }
        return best;
    }
}

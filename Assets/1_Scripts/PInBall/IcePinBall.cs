using UnityEngine;

/// <summary>
/// 冰球：命中 Unit 时以命中点为中心，对范围内所有 Unit 添加减速 buff。
///
/// 参数（来自 SpecialBallParams[BallType.Ice]）：
/// - slowPct          : 减速比例（slowFactor = 1 - slowPct，默认 0）
/// - slowDuration     : 减速持续秒数（默认 0）
/// - slowDurationAdd  : 持续时间累加
/// - slowRadius       : 范围半径（&lt;=0 则只对直接命中的 Unit 生效；默认 0 = 单体）
/// </summary>
public class IcePinBall : PinBallBase
{
    protected override void OnHitUnit(UnitBase unit, Vector2 hitPos, Vector2 hitNormal, HitDirection dir, bool destroyed)
    {
        SpecialBallParams sp = GameLogicManager.Instance != null ? GameLogicManager.Instance.SpecialBallParams : null;
        if (sp == null) return;

        float pct = Mathf.Clamp01(sp.Get(BallType.Ice, "slowPct"));
        float duration = sp.Get(BallType.Ice, "slowDuration");
        float radius = sp.Get(BallType.Ice, "slowRadius");
        if (pct <= 0f || duration <= 0f) return;

        float factor = 1f - pct;

        if (radius <= 0f)
        {
            if (!destroyed && unit != null)
                unit.ApplySlow(factor, duration);
            return;
        }

        var activeUnits = GameLogicManager.Instance.ActiveUnits;
        if (activeUnits == null) return;
        float r2 = radius * radius;
        for (int i = 0; i < activeUnits.Count; i++)
        {
            UnitBase other = activeUnits[i];
            if (other == null || !other.gameObject.activeSelf) continue;
            if (other == unit && destroyed) continue;

            Vector2 d = (Vector2)other.transform.position - hitPos;
            if (d.sqrMagnitude > r2) continue;

            other.ApplySlow(factor, duration);
        }
    }
}

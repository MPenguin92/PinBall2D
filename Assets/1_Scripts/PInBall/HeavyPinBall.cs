using UnityEngine;

/// <summary>
/// 重力球：不受 BallStats.MinSpeed 钳制（可走得很慢），命中 Unit 时把 Unit
/// 沿命中方向击退一段距离。基础伤害较高。
///
/// 参数（来自 SpecialBallParams[BallType.Heavy]）：
/// - knockbackDistance    : 击退距离（米）
/// - knockbackDistanceAdd : 击退距离累加
/// - heavyBonusDamage     : 额外基础伤害
/// </summary>
public class HeavyPinBall : PinBallBase
{
    protected override void OnHitUnit(UnitBase unit, Vector2 hitPos, Vector2 hitNormal, HitDirection dir, bool destroyed)
    {
        SpecialBallParams sp = GameLogicManager.Instance != null ? GameLogicManager.Instance.SpecialBallParams : null;
        if (sp == null) return;

        // 1. 额外伤害
        int bonus = Mathf.Max(0, Mathf.RoundToInt(sp.Get(BallType.Heavy, "heavyBonusDamage")));
        if (bonus > 0 && unit != null && !destroyed)
        {
            bool killedByBonus = unit.TakeDamage(bonus);
            if (killedByBonus)
            {
                GameEvents.RaiseUnitKilled(unit);
                GameLogicManager.Instance.RecycleUnit(unit);
                return;
            }
        }

        // 2. 击退（使用法线反向：把 Unit 推离弹珠方向）
        float dist = sp.Get(BallType.Heavy, "knockbackDistance");
        if (dist <= 0f || unit == null || destroyed || !unit.gameObject.activeSelf) return;

        Vector2 push = -hitNormal.normalized * dist;
        unit.transform.position += new Vector3(push.x, push.y, 0f);
        unit.RefreshRect();
    }
}

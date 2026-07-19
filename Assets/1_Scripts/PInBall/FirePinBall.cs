using UnityEngine;

/// <summary>
/// 火球：命中 Unit 时（无论是否击杀）以命中点为中心做一次圆形 AOE，
/// 对范围内所有 Unit 造成 ExplosionDamage（独立于普通命中伤害）。
///
/// 参数（来自 SpecialBallParams[BallType.Fire]）：
/// - explosionRadius     : 爆炸半径（米；默认 0）
/// - explosionRadiusAdd  : 半径累加增量（多次升级叠加）
/// - explosionDamage     : 爆炸伤害（默认 0）
/// - explosionDamageAdd  : 伤害累加增量
/// </summary>
public class FirePinBall : PinBallBase
{
    protected override void OnHitUnit(UnitBase unit, Vector2 hitPos, Vector2 hitNormal, HitDirection dir, bool destroyed)
    {
        SpecialBallParams sp = GameLogicManager.Instance != null ? GameLogicManager.Instance.SpecialBallParams : null;
        if (sp == null) return;

        float radius = sp.Get(BallType.Fire, "explosionRadius");
        int dmg = Mathf.Max(0, Mathf.RoundToInt(sp.Get(BallType.Fire, "explosionDamage")));
        if (radius <= 0f || dmg <= 0) return;

        var activeUnits = GameLogicManager.Instance.ActiveUnits;
        if (activeUnits == null) return;

        float r2 = radius * radius;
        // 反向遍历：被命中后 RecycleUnit 会从列表中移除，反向避免索引漂移。
        for (int i = activeUnits.Count - 1; i >= 0; i--)
        {
            UnitBase other = activeUnits[i];
            if (other == null || !other.gameObject.activeSelf) continue;
            if (other == unit && destroyed) continue; // 已被主命中击杀

            Vector2 d = (Vector2)other.transform.position - hitPos;
            if (d.sqrMagnitude > r2) continue;

            bool destroyedByAoe = other.TakeDamage(dmg, BallType.Fire);
            if (destroyedByAoe)
            {
                GameEvents.RaiseUnitKilled(other);
                GameLogicManager.Instance.RecycleUnit(other);
            }
        }
    }
}

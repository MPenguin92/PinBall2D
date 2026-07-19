using UnityEngine;

/// <summary>
/// 毒球：命中 Unit 时给该 Unit 添加持续伤害 buff（每秒结算一次），
/// 简化实现：用减速 buff 字段同槽，但持续期内每秒扣 1 点 HP。
///
/// 当前为最小实现：仅对直接命中的 Unit 添加 DoT，且通过协程外扣血。
/// 完整实现建议引入 UnitBuff 列表与 Tick 集中处理；此处为占位。
///
/// 参数（来自 SpecialBallParams[BallType.Poison]）：
/// - poisonDuration       : 持续秒数
/// - poisonDurationAdd    : 持续秒数累加
/// - poisonDamagePerSec   : 每秒伤害
/// - poisonDamagePerSecAdd: 每秒伤害累加
/// </summary>
public class PoisonPinBall : PinBallBase
{
    protected override void OnHitUnit(UnitBase unit, Vector2 hitPos, Vector2 hitNormal, HitDirection dir, bool destroyed)
    {
        if (destroyed || unit == null) return;

        SpecialBallParams sp = GameLogicManager.Instance != null ? GameLogicManager.Instance.SpecialBallParams : null;
        if (sp == null) return;

        float duration = sp.Get(BallType.Poison, "poisonDuration");
        int dps = Mathf.Max(0, Mathf.RoundToInt(sp.Get(BallType.Poison, "poisonDamagePerSec")));
        if (duration <= 0f || dps <= 0) return;

        // 通过 MonoBehaviour 协程对该 Unit 持续掉血。
        unit.StartCoroutine(PoisonRoutine(unit, dps, duration));
    }

    private static System.Collections.IEnumerator PoisonRoutine(UnitBase unit, int dps, float duration)
    {
        float remaining = duration;
        while (remaining > 0f && unit != null && unit.gameObject.activeSelf && unit.CurrentHp > 0)
        {
            yield return new WaitForSeconds(1f);
            remaining -= 1f;

            if (unit == null || !unit.gameObject.activeSelf) yield break;

            bool destroyed = unit.TakeDamage(dps, BallType.Poison);
            if (destroyed)
            {
                GameEvents.RaiseUnitKilled(unit);
                if (GameLogicManager.Instance != null)
                    GameLogicManager.Instance.RecycleUnit(unit);
                yield break;
            }
        }
    }
}

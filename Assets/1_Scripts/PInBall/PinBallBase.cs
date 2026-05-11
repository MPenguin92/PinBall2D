using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 弹珠基类：每帧自行推进位置，与 Border / UnitBase 做圆-AABB 相交检测。
/// 数值（速度、伤害、命中方向倍率、穿透、反弹次数等）统一从全局
/// <see cref="BallStats"/> 读取，所以同一份 modifier 同时影响所有出场球。
/// 子类（FirePinBall / IcePinBall ...）通过 override <see cref="OnHitUnit"/>
/// 注入命中时的额外效果（爆炸、减速、链跳）。
/// </summary>
public class PinBallBase : MonoBehaviour
{
    [SerializeField]
    [Tooltip("仅作为 Inspector 调试展示；运行时初速来自 Init 入参或 BallStats.InitialSpeed。")]
    private float initialSpeedHint = 10f;

    [SerializeField]
    [Tooltip("此球种类型，用于回收时归还到对应库存槽位。")]
    private BallType ballType = BallType.Base;

    private Vector2 velocity;
    private int bounceCount;

    public Vector2 Velocity => velocity;

    public BallType BallType => ballType;

    public Vector2 Position => transform.position;

    public float Radius => transform.localScale.x * 0.5f;

    public void Init(Vector2 direction, float speed)
    {
        BallStats stats = GetStats();
        float minSpeed = stats != null ? stats.Get(BallStatType.MinSpeed) : 3f;
        velocity = direction.normalized * Mathf.Max(speed, minSpeed);
        bounceCount = 0;
    }

    public virtual void Tick(Border[] borders, IReadOnlyList<UnitBase> activeUnits)
    {
        BallStats stats = GetStats();
        float dt = Time.deltaTime;
        Vector2 currentPos = transform.position;
        float radius = Radius;
        bool bounced = false;

        Vector2 nextPos = currentPos + velocity * dt;

        // 1. 边框检测
        for (int i = 0; i < borders.Length; i++)
        {
            Border border = borders[i];
            if (border == null) continue;

            if (!IsCircleOverlappingRect(nextPos, radius, border.BorderRect))
                continue;

            if (border.IsBottomBorder)
            {
                GameLogicManager.Instance.RecyclePinBall(this);
                return;
            }

            Vector2 normal = border.GetNormal();
            ApplyBounce(stats, normal);
            bounced = true;

            if (HitMaxBounces(stats))
            {
                GameLogicManager.Instance.RecyclePinBall(this);
                return;
            }
            break;
        }

        // 2. Unit 检测
        if (!bounced && activeUnits != null)
        {
            for (int i = 0; i < activeUnits.Count; i++)
            {
                UnitBase unit = activeUnits[i];
                if (unit == null || !unit.gameObject.activeSelf) continue;

                if (!IsCircleOverlappingRect(nextPos, radius, unit.UnitRect))
                    continue;

                Vector2 hitNormal = unit.GetCollisionNormal(nextPos);
                HitDirection dir = ResolveHitDirection(hitNormal, unit.MoveDirection);

                int dmg = ComputeDamage(stats, dir);
                bool destroyed = unit.TakeDamage(dmg);

                // 子类钩子：可在击杀/未击杀分支前注入额外效果。
                OnHitUnit(unit, nextPos, hitNormal, dir, destroyed);

                if (destroyed)
                {
                    // OnUnitKilled 必须在回收前 Raise，UpgradeService 才能拿到有效引用。
                    GameEvents.RaiseUnitKilled(unit);
                    GameLogicManager.Instance.RecycleUnit(unit);

                    if (TryPiercing(stats))
                    {
                        // 穿透：跳过反弹，按比例保留速度继续直行。
                        float keep = stats != null ? stats.Get(BallStatType.PiercingKeepSpeed) : 0.7f;
                        float minSpeed = stats != null ? stats.Get(BallStatType.MinSpeed) : 3f;
                        float curMag = velocity.magnitude;
                        float newMag = Mathf.Max(minSpeed, curMag * keep);
                        velocity = velocity.normalized * newMag;
                    }
                    else
                    {
                        ApplyBounce(stats, hitNormal);
                        if (HitMaxBounces(stats))
                        {
                            GameLogicManager.Instance.RecyclePinBall(this);
                            return;
                        }
                    }
                }
                else
                {
                    // 未击杀：先反弹，再按 HitSlowdown 整体减速。
                    ApplyBounce(stats, hitNormal);
                    ApplyHitSlowdown(stats);

                    if (HitMaxBounces(stats))
                    {
                        GameLogicManager.Instance.RecyclePinBall(this);
                        return;
                    }
                }

                break;
            }
        }

        Vector2 finalPos = currentPos + velocity * dt;
        transform.position = new Vector3(finalPos.x, finalPos.y, transform.position.z);
    }

    /// <summary>
    /// 命中 Unit 时的子类扩展点。基类不做事；FirePinBall 在此实现 AOE，
    /// IcePinBall 在此调用 ApplySlow，LightningPinBall 在此触发链跳。
    /// </summary>
    /// <param name="unit">直接命中的 Unit。</param>
    /// <param name="hitPos">本帧位置（圆心）。</param>
    /// <param name="hitNormal">命中边的法线。</param>
    /// <param name="dir">命中方向（Front/Side/Back）。</param>
    /// <param name="destroyed">unit.TakeDamage 返回值。</param>
    protected virtual void OnHitUnit(UnitBase unit, Vector2 hitPos, Vector2 hitNormal, HitDirection dir, bool destroyed)
    {
    }

    private bool HitMaxBounces(BallStats stats)
    {
        bounceCount++;
        if (stats == null) return false;
        int maxB = stats.GetInt(BallStatType.MaxBounces);
        return maxB > 0 && bounceCount >= maxB;
    }

    private static bool TryPiercing(BallStats stats)
    {
        if (stats == null) return false;
        float p = stats.Get(BallStatType.PiercingChance);
        if (p <= 0f) return false;
        return Random.value < p;
    }

    private void ApplyHitSlowdown(BallStats stats)
    {
        if (stats == null) return;
        float slow = stats.Get(BallStatType.HitSlowdown);
        if (slow <= 0f) return;

        float minSpeed = stats.Get(BallStatType.MinSpeed);
        float newMag = Mathf.Max(minSpeed, velocity.magnitude * (1f - slow));
        if (velocity.sqrMagnitude > 0f)
            velocity = velocity.normalized * newMag;
    }

    private void ApplyBounce(BallStats stats, Vector2 normal)
    {
        Vector2 reflected = Vector2.Reflect(velocity, normal);
        if (reflected.sqrMagnitude <= Mathf.Epsilon) return;

        float bounceMul = stats != null ? stats.Get(BallStatType.BounceSpeedMul) : 1f;
        float bounceAccel = stats != null ? stats.Get(BallStatType.BounceAccel) : 0f;
        float minSpeed = stats != null ? stats.Get(BallStatType.MinSpeed) : 3f;
        float maxSpeed = stats != null ? stats.Get(BallStatType.MaxSpeed) : 0f;

        float newMagnitude = reflected.magnitude * bounceMul + bounceAccel;
        newMagnitude = Mathf.Max(newMagnitude, minSpeed);
        if (maxSpeed > 0f)
            newMagnitude = Mathf.Min(newMagnitude, maxSpeed);

        velocity = reflected.normalized * newMagnitude;
    }

    private static int ComputeDamage(BallStats stats, HitDirection dir)
    {
        float baseDmg = stats != null ? stats.Get(BallStatType.BaseDamage) : 1f;
        float dirMul = 1f;
        if (stats != null)
        {
            switch (dir)
            {
                case HitDirection.Front: dirMul = stats.Get(BallStatType.FrontHitMul); break;
                case HitDirection.Side: dirMul = stats.Get(BallStatType.SideHitMul); break;
                case HitDirection.Back: dirMul = stats.Get(BallStatType.BackHitMul); break;
            }
        }
        return Mathf.Max(1, Mathf.RoundToInt(baseDmg * dirMul));
    }

    /// <summary>
    /// 根据 AABB 法线与 Unit 移动方向解算命中方向。
    /// 约定：以 Unit 移动方向为基准，迎面对撞=正面（normal ≈ -moveDir），
    ///       从背后追击=背面（normal ≈ moveDir），其他=侧面。
    /// </summary>
    private static HitDirection ResolveHitDirection(Vector2 hitNormal, Vector2 moveDir)
    {
        if (moveDir.sqrMagnitude <= Mathf.Epsilon)
            return HitDirection.Side;

        Vector2 mDir = moveDir.normalized;
        Vector2 nDir = hitNormal.normalized;

        // dot(normal, -moveDir): 1=正面对撞, -1=背面追击, 0=侧面。
        float dot = -Vector2.Dot(nDir, mDir);

        if (dot > 0.5f) return HitDirection.Front;
        if (dot < -0.5f) return HitDirection.Back;
        return HitDirection.Side;
    }

    private static BallStats GetStats()
    {
        GameLogicManager mgr = GameLogicManager.Instance;
        return mgr != null ? mgr.BallStats : null;
    }

    private static bool IsCircleOverlappingRect(Vector2 circleCenter, float radius, Rect rect)
    {
        float closestX = Mathf.Clamp(circleCenter.x, rect.xMin, rect.xMax);
        float closestY = Mathf.Clamp(circleCenter.y, rect.yMin, rect.yMax);
        float dx = circleCenter.x - closestX;
        float dy = circleCenter.y - closestY;
        return dx * dx + dy * dy <= radius * radius;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, transform.localScale.x * 0.5f);
    }
}

/// <summary>命中方向桶：以 Unit 移动方向为基准。</summary>
public enum HitDirection
{
    Front,
    Side,
    Back,
}

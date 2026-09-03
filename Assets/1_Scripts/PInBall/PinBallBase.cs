using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 弹珠基类：每帧自行推进位置，与 Border / UnitBase 做圆-AABB 相交检测。
/// ⚠️ 升级体系已清空（2026-09-01）：伤害、速度、反弹等均回到基础行为
/// （伤害固定 1、完全弹性反弹），后续由重新设计的属性系统接管。
/// 子类通过 override <see cref="OnHitUnit"/> 注入命中时的额外效果。
/// </summary>
public class PinBallBase : MonoBehaviour
{

    [SerializeField]
    [Tooltip("此球种类型，用于回收时归还到对应库存槽位。")]
    private BallType ballType = BallType.Base;

    private Vector2 velocity;

    public Vector2 Velocity => velocity;

    public BallType BallType => ballType;

    public Vector2 Position => transform.position;

    public float Radius => transform.localScale.x * 0.5f;

    public void Init(Vector2 direction, float speed)
    {
        velocity = direction.normalized * Mathf.Max(speed, 0.01f);
    }

    public virtual void Tick(Border[] borders, IReadOnlyList<UnitBase> activeUnits)
    {
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
                // 返回时（触底回收前）：广播后回收，回收后球实例不再有效。
                BallEvents.RaiseReturned(this, currentPos);
                GameLogicManager.Instance.RecyclePinBall(this);
                return;
            }

            Vector2 normal = border.GetNormal();
            ApplyBounce(normal, nextPos);
            bounced = true;
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

                bool destroyed = unit.TakeDamage(1, BallType);

                // 命中时（无论是否击杀）：在子类钩子前广播，让效果与钩子都能拿到存活/摧毁信息。
                BallEvents.RaiseHitUnit(this, unit, nextPos, hitNormal, dir, destroyed);

                // 子类钩子：可在击杀/未击杀分支前注入额外效果。
                OnHitUnit(unit, nextPos, hitNormal, dir, destroyed);

                if (destroyed)
                {
                    // 恰好击杀时（unit 回收前，引用仍有效）。
                    BallEvents.RaiseKilledUnit(this, unit, nextPos, hitNormal, dir);

                    // OnUnitKilled 必须在回收前 Raise，UpgradeService 才能拿到有效引用。
                    GameEvents.RaiseUnitKilled(unit);
                    GameLogicManager.Instance.RecycleUnit(unit);
                }

                ApplyBounce(hitNormal, nextPos);
                break;
            }
        }

        Vector2 finalPos = currentPos + velocity * dt;
        transform.position = new Vector3(finalPos.x, finalPos.y, transform.position.z);
    }

    /// <summary>
    /// 命中 Unit 时的子类扩展点。基类不做事；子类可在此注入命中时的额外效果。
    /// </summary>
    /// <param name="unit">直接命中的 Unit。</param>
    /// <param name="hitPos">本帧位置（圆心）。</param>
    /// <param name="hitNormal">命中边的法线。</param>
    /// <param name="dir">命中方向（Front/Side/Back）。</param>
    /// <param name="destroyed">unit.TakeDamage 返回值。</param>
    protected virtual void OnHitUnit(UnitBase unit, Vector2 hitPos, Vector2 hitNormal, HitDirection dir, bool destroyed)
    {
    }

    /// <summary>完全弹性反弹：仅反射速度方向，大小保持不变；反射生效后广播反弹时机。</summary>
    private void ApplyBounce(Vector2 normal, Vector2 bouncePos)
    {
        Vector2 reflected = Vector2.Reflect(velocity, normal);
        if (reflected.sqrMagnitude <= Mathf.Epsilon) return;
        velocity = reflected;

        BallEvents.RaiseBounced(this, bouncePos, normal);
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

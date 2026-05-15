using UnityEngine;

/// <summary>
/// 回旋球：触底前会先往回反弹 N 次（给玩家额外救场机会），N 由升级等级决定。
/// 参数（来自 SpecialBallParams[BallType.Boomerang]）：
///   - extraReturns：触底允许反弹的次数，Lv1=1（即至少反弹 1 次再回收）。
/// </summary>
public class BoomerangPinBall : PinBallBase
{
    private int returnsUsed;

    private void OnEnable()
    {
        // 池化复用：每次激活都重置已用回旋次数，避免上一次发射的状态残留。
        returnsUsed = 0;
    }

    public override void Tick(Border[] borders, System.Collections.Generic.IReadOnlyList<UnitBase> activeUnits)
    {
        // 简化策略：自定义底边检测；非底边沿用基类。
        BallStats stats = GameLogicManager.Instance != null ? GameLogicManager.Instance.BallStats : null;
        float dt = Time.deltaTime;
        float radius = Radius;
        Vector2 currentPos = transform.position;
        Vector2 nextPos = currentPos + Velocity * dt;

        for (int i = 0; i < borders.Length; i++)
        {
            Border b = borders[i];
            if (b == null) continue;
            if (!b.IsBottomBorder) continue;
            if (!IsCircleOverlappingRect(nextPos, radius, b.BorderRect)) continue;

            int allowed = ResolveExtraReturns();
            if (returnsUsed < allowed)
            {
                returnsUsed++;
                // 反弹回上方：只翻 Y 速度。
                Vector2 v = Velocity;
                v.y = Mathf.Abs(v.y);
                if (stats != null)
                {
                    float minSpeed = stats.Get(BallStatType.MinSpeed);
                    if (v.magnitude < minSpeed) v = v.normalized * minSpeed;
                }
                SetVelocity(v);
                Vector2 finalPos = currentPos + v * dt;
                transform.position = new Vector3(finalPos.x, finalPos.y, transform.position.z);
                return;
            }

            // 用完次数后正常回收：returnsUsed 由 OnEnable 在下次激活时重置。
            GameLogicManager.Instance.RecyclePinBall(this);
            return;
        }

        // 否则走基类完整逻辑（其他边框 / Unit 检测 / 移动）。
        base.Tick(borders, activeUnits);
    }

    private static bool IsCircleOverlappingRect(Vector2 circleCenter, float radius, Rect rect)
    {
        float closestX = Mathf.Clamp(circleCenter.x, rect.xMin, rect.xMax);
        float closestY = Mathf.Clamp(circleCenter.y, rect.yMin, rect.yMax);
        float dx = circleCenter.x - closestX;
        float dy = circleCenter.y - closestY;
        return dx * dx + dy * dy <= radius * radius;
    }

    private void SetVelocity(Vector2 v)
    {
        // 通过 Init 重写 velocity（保持 minSpeed 钳制）。
        Init(v.normalized, v.magnitude);
        // Init 会重置 bounceCount，这里不再额外重置，因为该球种不依赖 MaxBounces 限制。
    }

    private static int ResolveExtraReturns()
    {
        SpecialBallParams sp = GameLogicManager.Instance != null ? GameLogicManager.Instance.SpecialBallParams : null;
        if (sp == null) return 1;
        // 默认 1 次：Lv1 时玩家至少能享受一次回旋；未配置任何参数时也给 1 次保底。
        return Mathf.Max(0, Mathf.RoundToInt(sp.Get(BallType.Boomerang, "extraReturns", 1f)));
    }
}

using UnityEngine;

/// <summary>
/// 回旋球：触底前会先往回反弹一次（给玩家额外救场机会）。
/// 通过私有标记 returnedOnce 实现：第一次命中底边时不回收，转向反弹一次；
/// 第二次再触底则正常回收。
///
/// 该球种没有特殊参数，挂上 prefab 即可。
/// </summary>
public class BoomerangPinBall : PinBallBase
{
    private bool hasReturned;

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

            if (!hasReturned)
            {
                hasReturned = true;
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

            // 第二次触底正常回收。
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
}

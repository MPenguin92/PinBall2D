using System.Collections.Generic;
using UnityEngine;

public class PinBallBase : MonoBehaviour
{
    [SerializeField]
    private float initialSpeed = 10f;

    [SerializeField]
    private float minSpeed = 3f;

    [SerializeField]
    private float bounceSpeedMultiplier = 0.95f;

    private Vector2 velocity;

    public Vector2 Velocity => velocity;

    public float Radius => transform.localScale.x * 0.5f;

    public void Init(Vector2 direction, float speed)
    {
        velocity = direction.normalized * Mathf.Max(speed, minSpeed);
    }

    public virtual void Tick(Border[] borders, IReadOnlyList<UnitBase> activeUnits)
    {
        float dt = Time.deltaTime;
        Vector2 currentPos = transform.position;
        float radius = Radius;
        bool bounced = false;

        Vector2 nextPos = currentPos + velocity * dt;

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
            ApplyBounce(normal);
            bounced = true;
            break;
        }

        if (!bounced && activeUnits != null)
        {
            for (int i = 0; i < activeUnits.Count; i++)
            {
                UnitBase unit = activeUnits[i];
                if (unit == null || !unit.gameObject.activeSelf) continue;

                if (!IsCircleOverlappingRect(nextPos, radius, unit.UnitRect))
                    continue;

                Vector2 normal = unit.GetCollisionNormal(nextPos);
                ApplyBounce(normal);

                bool destroyed = unit.TakeDamage(1);
                if (destroyed)
                {
                    GameLogicManager.Instance.RecycleUnit(unit);
                }

                break;
            }
        }

        Vector2 finalPos = currentPos + velocity * dt;
        transform.position = new Vector3(finalPos.x, finalPos.y, transform.position.z);
    }

    private void ApplyBounce(Vector2 normal)
    {
        Vector2 reflected = Vector2.Reflect(velocity, normal);
        if (reflected.sqrMagnitude <= Mathf.Epsilon) return;

        float newMagnitude = reflected.magnitude * bounceSpeedMultiplier;
        newMagnitude = Mathf.Max(newMagnitude, minSpeed);
        velocity = reflected.normalized * newMagnitude;
    }

    private bool IsCircleOverlappingRect(Vector2 circleCenter, float radius, Rect rect)
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

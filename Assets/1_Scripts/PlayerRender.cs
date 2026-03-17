using System.Collections.Generic;
using UnityEngine;

public class PlayerRender : MonoBehaviour
{
    [SerializeField]
    private Player player;

    [SerializeField]
    private LineRenderer lineRenderer;

    [SerializeField]
    private float maxLineLength = 20f;

    [SerializeField]
    private int maxBounces = 10;

    private readonly List<Vector3> linePoints = new List<Vector3>();

    public void Tick()
    {
        UpdateDirectionLine();
    }

    private void UpdateDirectionLine()
    {
        if (player == null || lineRenderer == null) return;

        linePoints.Clear();

        Vector2 origin = player.transform.position;
        Vector2 direction = player.Direction;
        float remainingLength = maxLineLength;

        linePoints.Add(origin);

        GameLogicManager manager = GameLogicManager.Instance;
        Border[] borders = manager != null ? manager.Borders : null;
        IReadOnlyList<UnitBase> activeUnits = manager != null ? manager.ActiveUnits : null;

        int bounceCount = 0;

        while (remainingLength > 0f && bounceCount < maxBounces)
        {
            float nearestDist = remainingLength;
            Vector2 nearestNormal = Vector2.zero;
            bool hitSomething = false;
            bool shouldStop = false;

            if (borders != null)
            {
                for (int i = 0; i < borders.Length; i++)
                {
                    Border border = borders[i];
                    if (border == null) continue;

                    if (RaycastRect(origin, direction, border.BorderRect, out float dist, out Vector2 normal))
                    {
                        if (dist > 0.001f && dist < nearestDist)
                        {
                            nearestDist = dist;
                            nearestNormal = normal;
                            hitSomething = true;
                            shouldStop = border.IsBottomBorder;
                        }
                    }
                }
            }

            if (activeUnits != null)
            {
                for (int i = 0; i < activeUnits.Count; i++)
                {
                    UnitBase unit = activeUnits[i];
                    if (unit == null || !unit.gameObject.activeSelf) continue;

                    if (RaycastRect(origin, direction, unit.UnitRect, out float dist, out Vector2 normal))
                    {
                        if (dist > 0.001f && dist < nearestDist)
                        {
                            nearestDist = dist;
                            nearestNormal = normal;
                            hitSomething = true;
                            shouldStop = true;
                        }
                    }
                }
            }

            Vector2 hitPoint = origin + direction * nearestDist;
            linePoints.Add(hitPoint);
            remainingLength -= nearestDist;

            if (!hitSomething || shouldStop)
                break;

            direction = Vector2.Reflect(direction, nearestNormal);
            origin = hitPoint;
            bounceCount++;
        }

        lineRenderer.positionCount = linePoints.Count;
        for (int i = 0; i < linePoints.Count; i++)
        {
            lineRenderer.SetPosition(i, linePoints[i]);
        }
    }

    /// <summary>
    /// Ray-AABB intersection using the slab method.
    /// Returns the entry distance and outward-facing normal of the hit face.
    /// Returns false if the ray origin is inside the rect or no intersection found.
    /// </summary>
    private bool RaycastRect(Vector2 origin, Vector2 dir, Rect rect, out float distance, out Vector2 normal)
    {
        distance = 0f;
        normal = Vector2.zero;

        float tMin = 0f;
        float tMax = float.MaxValue;
        Vector2 hitNormal = Vector2.zero;

        if (Mathf.Abs(dir.x) < Mathf.Epsilon)
        {
            if (origin.x < rect.xMin || origin.x > rect.xMax)
                return false;
        }
        else
        {
            float invD = 1f / dir.x;
            float t1 = (rect.xMin - origin.x) * invD;
            float t2 = (rect.xMax - origin.x) * invD;
            Vector2 n1 = Vector2.left;
            Vector2 n2 = Vector2.right;

            if (t1 > t2)
            {
                (t1, t2) = (t2, t1);
                (n1, n2) = (n2, n1);
            }

            if (t1 > tMin) { tMin = t1; hitNormal = n1; }
            if (t2 < tMax) tMax = t2;

            if (tMin > tMax) return false;
        }

        if (Mathf.Abs(dir.y) < Mathf.Epsilon)
        {
            if (origin.y < rect.yMin || origin.y > rect.yMax)
                return false;
        }
        else
        {
            float invD = 1f / dir.y;
            float t1 = (rect.yMin - origin.y) * invD;
            float t2 = (rect.yMax - origin.y) * invD;
            Vector2 n1 = Vector2.down;
            Vector2 n2 = Vector2.up;

            if (t1 > t2)
            {
                (t1, t2) = (t2, t1);
                (n1, n2) = (n2, n1);
            }

            if (t1 > tMin) { tMin = t1; hitNormal = n1; }
            if (t2 < tMax) tMax = t2;

            if (tMin > tMax) return false;
        }

        if (tMin < Mathf.Epsilon)
            return false;

        distance = tMin;
        normal = hitNormal;
        return true;
    }
}

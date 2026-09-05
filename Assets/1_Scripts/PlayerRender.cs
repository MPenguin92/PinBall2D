using System.Collections.Generic;
using UnityEngine;

public class PlayerRender : MonoBehaviour, ICombatAnimation
{
    private const string DashedLineMaterialPath = "7_Res/dashed_line.mat";

    [SerializeField]
    private Player player;

    [SerializeField]
    private LineRenderer lineRenderer;

    [SerializeField]
    [Tooltip("瞄准线起点相对发射点沿射击方向的前移；0 = 与出球点重合。")]
    private float lineForwardOffset = 0f;

    [SerializeField]
    private float maxLineLength = 20f;

    [SerializeField, HideInInspector]
    private Material dashedLineMaterial;

    public void Tick()
    {
        UpdateDirectionLine();
    }

    public virtual void PlayAttackAnimation()
    {
    }

    public virtual void PlayHitAnimation()
    {
    }

    public virtual void PlayDeathAnimation()
    {
    }

    private void UpdateDirectionLine()
    {
        if (player == null || lineRenderer == null) return;

        Vector2 direction = player.Direction;
        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            lineRenderer.positionCount = 0;
            return;
        }

        direction.Normalize();

        Vector2 origin = player.FirePosition + direction * lineForwardOffset;
        float stopDist = maxLineLength;

        GameLogicManager manager = GameLogicManager.Instance;
        Border[] borders = manager != null ? manager.Borders : null;
        IReadOnlyList<UnitBase> activeUnits = manager != null ? manager.ActiveUnits : null;

        if (borders != null)
        {
            for (int i = 0; i < borders.Length; i++)
            {
                Border border = borders[i];
                if (border == null) continue;

                if (RaycastRect(origin, direction, border.BorderRect, out float dist, out _)
                    && dist > 0.001f && dist < stopDist)
                {
                    stopDist = dist;
                }
            }
        }

        if (activeUnits != null)
        {
            for (int i = 0; i < activeUnits.Count; i++)
            {
                UnitBase unit = activeUnits[i];
                if (unit == null || !unit.gameObject.activeSelf) continue;

                if (RaycastRect(origin, direction, unit.UnitRect, out float dist, out _)
                    && dist > 0.001f && dist < stopDist)
                {
                    stopDist = dist;
                }
            }
        }

        Vector2 end = origin + direction * stopDist;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, origin);
        lineRenderer.SetPosition(1, end);
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

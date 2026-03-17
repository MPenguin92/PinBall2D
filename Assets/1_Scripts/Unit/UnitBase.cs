using UnityEngine;

public class UnitBase : MonoBehaviour
{
    [SerializeField]
    private int maxHp = 3;

    private int currentHp;

    public int CurrentHp => currentHp;

    public int MaxHp => maxHp;

    public float Width => transform.localScale.x;

    public float Height => transform.localScale.y;

    public Rect UnitRect { get; private set; }

    public void Init()
    {
        currentHp = maxHp;
        RefreshRect();
    }

    public void RefreshRect()
    {
        Vector3 pos = transform.position;
        Vector3 scale = transform.localScale;
        UnitRect = new Rect(
            pos.x - scale.x * 0.5f,
            pos.y - scale.y * 0.5f,
            scale.x,
            scale.y
        );
    }

    public virtual void Tick()
    {
    }

    public bool TakeDamage(int damage)
    {
        currentHp = Mathf.Max(0, currentHp - damage);
        return currentHp <= 0;
    }

    public Vector2 GetCollisionNormal(Vector2 circleCenter)
    {
        Rect rect = UnitRect;
        float closestX = Mathf.Clamp(circleCenter.x, rect.xMin, rect.xMax);
        float closestY = Mathf.Clamp(circleCenter.y, rect.yMin, rect.yMax);
        float dx = circleCenter.x - closestX;
        float dy = circleCenter.y - closestY;

        if (Mathf.Abs(dx) > Mathf.Abs(dy))
            return dx >= 0f ? Vector2.right : Vector2.left;

        return dy >= 0f ? Vector2.up : Vector2.down;
    }

    private void OnDrawGizmos()
    {
        RefreshRect();
        Gizmos.color = Color.green;
        Vector3 center = new Vector3(UnitRect.center.x, UnitRect.center.y, transform.position.z);
        Vector3 size = new Vector3(UnitRect.width, UnitRect.height, 0f);
        Gizmos.DrawWireCube(center, size);
    }
}

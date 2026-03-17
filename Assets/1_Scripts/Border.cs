using UnityEngine;

public class Border : MonoBehaviour
{
    [SerializeField]
    private BounceDirection bounceDirection = BounceDirection.Up;

    [SerializeField]
    private bool isBottomBorder;

    public BounceDirection BounceDirection => bounceDirection;

    public bool IsBottomBorder => isBottomBorder;

    public Rect BorderRect { get; private set; }

    public float Width => transform.localScale.x;

    public float Height => transform.localScale.y;

    private void Awake()
    {
        RefreshRect();
    }

    private void OnValidate()
    {
        RefreshRect();
    }

    public void RefreshRect()
    {
        Vector3 position = transform.position;
        Vector3 scale = transform.localScale;
        BorderRect = new Rect(
            position.x - scale.x * 0.5f,
            position.y - scale.y * 0.5f,
            scale.x,
            scale.y
        );
    }

    public Vector2 GetNormal()
    {
        switch (bounceDirection)
        {
            case BounceDirection.Up: return Vector2.up;
            case BounceDirection.Down: return Vector2.down;
            case BounceDirection.Left: return Vector2.left;
            case BounceDirection.Right: return Vector2.right;
            default: return Vector2.up;
        }
    }

    private void OnDrawGizmos()
    {
        RefreshRect();
        Gizmos.color = isBottomBorder ? Color.red : Color.yellow;
        Vector3 center = new Vector3(BorderRect.center.x, BorderRect.center.y, transform.position.z);
        Vector3 size = new Vector3(BorderRect.width, BorderRect.height, 0f);
        Gizmos.DrawWireCube(center, size);
    }
}

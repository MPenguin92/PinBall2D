using UnityEngine;

public class Border : MonoBehaviour
{
    [SerializeField]
    private BounceDirection bounceDirection = BounceDirection.Up;

    [SerializeField]
    private bool isBottomBorder;

    [Header("Auto Align To Camera Edge")]
    [SerializeField]
    private bool autoAlignToCameraEdge = true;

    [SerializeField]
    private float thickness = 1f;

    [SerializeField]
    private Camera targetCamera;

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
        if (autoAlignToCameraEdge)
            AlignToCameraEdge();

        Vector3 position = transform.position;
        Vector3 scale = transform.localScale;
        BorderRect = new Rect(
            position.x - scale.x * 0.5f,
            position.y - scale.y * 0.5f,
            scale.x,
            scale.y
        );
    }

    /// <summary>
    /// 根据正交相机的视口，将自身贴到对应屏幕边缘：
    /// Border 中心位于屏幕可视区域之外，内侧面刚好与屏幕边缘对齐。
    /// bounceDirection 指示反弹法线方向，对应贴边关系：
    ///   Up    -> 贴下边（法线朝上）
    ///   Down  -> 贴上边
    ///   Right -> 贴左边
    ///   Left  -> 贴右边
    /// </summary>
    private void AlignToCameraEdge()
    {
        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null || !cam.orthographic) return;

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        Vector3 camPos = cam.transform.position;

        Vector3 pos = transform.position;
        Vector3 scale = transform.localScale;
        float t = Mathf.Max(thickness, 0.0001f);

        switch (bounceDirection)
        {
            case BounceDirection.Up:
                pos.x = camPos.x;
                pos.y = camPos.y - halfHeight - t * 0.5f;
                scale.x = halfWidth * 2f + t * 2f;
                scale.y = t;
                break;
            case BounceDirection.Down:
                pos.x = camPos.x;
                pos.y = camPos.y + halfHeight + t * 0.5f;
                scale.x = halfWidth * 2f + t * 2f;
                scale.y = t;
                break;
            case BounceDirection.Right:
                pos.x = camPos.x - halfWidth - t * 0.5f;
                pos.y = camPos.y;
                scale.x = t;
                scale.y = halfHeight * 2f;
                break;
            case BounceDirection.Left:
                pos.x = camPos.x + halfWidth + t * 0.5f;
                pos.y = camPos.y;
                scale.x = t;
                scale.y = halfHeight * 2f;
                break;
        }

        transform.position = pos;
        transform.localScale = scale;
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

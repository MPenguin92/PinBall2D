using UnityEngine;

public class PinBallBase : MonoBehaviour
{
    [SerializeField]
    private Vector2 speed = new Vector2(0f, -10f);

    [SerializeField]
    private float borderBounceSpeedMultiplier = 0.9f;

    private Vector2 initialSpeed;
    private float initialSpeedMagnitude;

    public Vector2 Speed => speed;

    public float Radius => transform.localScale.x * 0.5f;


    private void Awake()
    {
        initialSpeed = speed;
        initialSpeedMagnitude = initialSpeed.magnitude;
    }

    private void OnEnable()
    {
        RegisterToGameLogicManager();
    }

    private void OnDisable()
    {
        if (GameLogicManager.Instance != null)
        {
            GameLogicManager.Instance.UnregisterPinBall(this);
        }

        speed = initialSpeed;
    }

    public virtual void Tick(Border[] borders)
    {
        Vector2 currentPosition = transform.position;
        Vector2 nextPosition = currentPosition + speed * Time.deltaTime;
        float radius = Radius;

        for (int i = 0; i < borders.Length; i++)
        {
            Border border = borders[i];
            border.RefreshRect();

            if (!IsCircleOverlappingRect(nextPosition, radius, border.BorderRect))
            {
                continue;
            }

            Vector2 normal = GetBorderNormal(border.BounceDirection);
            ApplyBorderBounce(normal);
            break;
        }

        Vector2 finalPosition = currentPosition + speed * Time.deltaTime;
        transform.position = finalPosition;
    }

    public void PushBall(Vector2 newSpeed)
    {
        Vector2 direction = newSpeed.normalized;
        
        float magnitude = Mathf.Max(initialSpeedMagnitude, newSpeed.magnitude);
        speed = direction * magnitude;
    }

    private void ApplyBorderBounce(Vector2 normal)
    {
        Vector2 reflected = Vector2.Reflect(speed, normal);
        if (reflected.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        float currentSpeedMagnitude = reflected.magnitude;
        float targetMagnitude = currentSpeedMagnitude;
        if (currentSpeedMagnitude > initialSpeedMagnitude)
        {
            targetMagnitude = currentSpeedMagnitude * borderBounceSpeedMultiplier;
        }

        speed = reflected.normalized * targetMagnitude;
    }

    private void RegisterToGameLogicManager()
    {
        GameLogicManager manager = GameLogicManager.Instance;
        if (manager == null)
        {
            manager = Object.FindFirstObjectByType<GameLogicManager>();
        }

        if (manager != null)
        {
            manager.RegisterPinBall(this);
        }
    }

    private bool IsCircleOverlappingRect(Vector2 circleCenter, float radius, Rect rect)
    {
        float closestX = Mathf.Clamp(circleCenter.x, rect.xMin, rect.xMax);
        float closestY = Mathf.Clamp(circleCenter.y, rect.yMin, rect.yMax);
        float distanceX = circleCenter.x - closestX;
        float distanceY = circleCenter.y - closestY;
        return distanceX * distanceX + distanceY * distanceY <= radius * radius;
    }

    private Vector2 GetBorderNormal(BounceDirection direction)
    {
        switch (direction)
        {
            case BounceDirection.Up:
                return Vector2.up;
            case BounceDirection.Down:
                return Vector2.down;
            case BounceDirection.Left:
                return Vector2.left;
            case BounceDirection.Right:
                return Vector2.right;
            default:
                return Vector2.up;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Vector3 center = transform.position;
        Gizmos.DrawWireSphere(center, transform.localScale.x * 0.5f);
    }
}

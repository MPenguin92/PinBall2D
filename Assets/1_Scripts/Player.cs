using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField]
    private float rotateSpeed = 120f;

    [SerializeField]
    private float maxAngle = 80f;

    [SerializeField]
    private int maxPinBallCount = 5;

    [SerializeField]
    private float fireInterval = 0.3f;

    [SerializeField]
    private float firePinBallSpeed = 10f;

    private int currentPinBallCount;
    private float fireTimer;

    public int CurrentPinBallCount => currentPinBallCount;

    public int MaxPinBallCount => maxPinBallCount;

    public Vector2 Direction
    {
        get
        {
            float angleRad = transform.eulerAngles.z * Mathf.Deg2Rad;
            return new Vector2(-Mathf.Sin(angleRad), Mathf.Cos(angleRad));
        }
    }

    public void Init()
    {
        currentPinBallCount = maxPinBallCount;
        fireTimer = 0f;
        transform.rotation = Quaternion.identity;
    }

    public void Tick()
    {
        HandleRotation();
        HandleFire();

        if (fireTimer > 0f)
            fireTimer -= Time.deltaTime;
    }

    public void AddPinBall(int count = 1)
    {
        currentPinBallCount = Mathf.Min(currentPinBallCount + count, maxPinBallCount);
    }

    private void HandleRotation()
    {
        float input = 0f;
        if (Input.GetKey(KeyCode.A)) input -= 1f;
        if (Input.GetKey(KeyCode.D)) input += 1f;

        if (Mathf.Approximately(input, 0f)) return;

        float currentZ = transform.eulerAngles.z;
        if (currentZ > 180f) currentZ -= 360f;

        float delta = -input * rotateSpeed * Time.deltaTime;
        float newAngle = Mathf.Clamp(currentZ + delta, -maxAngle, maxAngle);

        transform.rotation = Quaternion.Euler(0f, 0f, newAngle);
    }

    private void HandleFire()
    {
        if (!Input.GetKeyDown(KeyCode.F)) return;
        if (currentPinBallCount <= 0) return;
        if (fireTimer > 0f) return;

        GameLogicManager.Instance.SpawnPinBall(transform.position, Direction, firePinBallSpeed);
        currentPinBallCount--;
        fireTimer = fireInterval;
    }

    /*private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, transform.localScale.x * 0.5f);

        Gizmos.color = Color.yellow;
        Vector3 dir = (Vector3)(Vector2)Direction;
        Gizmos.DrawLine(transform.position, transform.position + dir * 2f);
    }*/
}

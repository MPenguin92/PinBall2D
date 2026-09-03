using UnityEngine;

/// <summary>
/// 玩家：发射弹珠 + 鼠标瞄准 + 生命值。
/// 弹珠为无限发射模式（2026-09-03 起不再维护库存队列）：
/// 发射不扣库存、回收不还库存，每次松开鼠标直接发射一发普通球。
///
/// 操作方式：按住鼠标左键时炮口持续跟随鼠标方向，松开鼠标发射一发。
/// </summary>
public class Player : MonoBehaviour
{
    /// <summary>普通球在 Addressables 中的地址（BaseBall.prefab，注册于 Unit 组）。</summary>
    private const string BaseBallAddress = "BaseBall";

    [SerializeField]
    [Tooltip("Player 最大生命值")]
    private int maxHp = 5;

    [SerializeField]
    private PlayerRender playerRender;

    [SerializeField]
    [Tooltip("炮口 Transform，旋转与发射均以此为准；Player 本体不旋转。")]
    private Transform muzzle;

    /// <summary>屏幕→世界坐标转换用的主相机；无相机时禁用鼠标操作。</summary>
    private Camera mainCamera;

    [SerializeField]
    [Tooltip("发射间隔（秒）：重新设计升级体系前暂为固定值。")]
    private float fireInterval = 0.3f;

    [SerializeField]
    [Tooltip("发射初速：重新设计升级体系前暂为固定值。")]
    private float fireSpeed = 24f;

    private float fireTimer;
    private int currentHp;

    public int CurrentHp => currentHp;

    public int MaxHp => maxHp;

    public bool IsDead => currentHp <= 0;

    /// <summary>当前发射冷却间隔（升级体系清空期间为固定值，重新设计后可改由属性系统驱动）。</summary>
    public float FireInterval => fireInterval;

    public Vector2 Direction
    {
        get
        {
            if (muzzle != null)
                return muzzle.up;

            float angleRad = transform.eulerAngles.z * Mathf.Deg2Rad;
            return new Vector2(-Mathf.Sin(angleRad), Mathf.Cos(angleRad));
        }
    }

    /// <summary>当前发射位置（炮口世界坐标）。</summary>
    public Vector2 FirePosition => muzzle != null ? muzzle.position : (Vector2)transform.position;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    public void Init()
    {
        fireTimer = 0f;
        currentHp = maxHp;
        if (muzzle != null)
            muzzle.localRotation = Quaternion.identity;
    }

    public bool TakeDamage(int damage)
    {
        if (damage <= 0 || IsDead) return IsDead;

        currentHp = Mathf.Max(0, currentHp - damage);
        if (playerRender != null)
            playerRender.PlayHitAnimation();

        if (IsDead)
        {
            if (playerRender != null)
                playerRender.PlayDeathAnimation();
        }

        return IsDead;
    }

    /// <summary>回复生命（不超过上限）。</summary>
    public void Heal(int amount)
    {
        if (amount <= 0 || IsDead) return;
        currentHp = Mathf.Min(maxHp, currentHp + amount);
    }

    public void Tick()
    {
        HandlePointerInput();

        if (playerRender != null)
            playerRender.Tick();

        if (fireTimer > 0f)
            fireTimer -= Time.deltaTime;
    }

    /// <summary>
    /// 指针操作（兼容移动端）：按住时炮口持续跟随指针方向，松开时发射一发。
    /// Android/iOS 真机用触摸，其余平台（Editor/桌面）用鼠标；松开的那一帧仍能读到按压状态，因此发射方向已对准指针。
    /// </summary>
    private void HandlePointerInput()
    {
        if (muzzle == null || mainCamera == null) return;

#if UNITY_ANDROID || UNITY_IOS
        if (Input.touchCount <= 0) return;

        Touch touch = Input.GetTouch(0);
        if (touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled)
            RotateMuzzleTowardScreen(touch.position);

        if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            TryFire();
#else
        if (Input.GetMouseButton(0))
            RotateMuzzleTowardScreen(Input.mousePosition);

        if (Input.GetMouseButtonUp(0))
            TryFire();
#endif
    }

    private void RotateMuzzleTowardScreen(Vector2 screenPos)
    {
        Vector3 pointerWorld = mainCamera.ScreenToWorldPoint(screenPos);
        pointerWorld.z = 0f;

        Vector2 worldDir = (Vector2)(pointerWorld - muzzle.position);
        if (worldDir.sqrMagnitude <= Mathf.Epsilon) return;

        // 炮口朝向以 muzzle.up 为准：把世界方向换算到父级局部空间后求偏转。
        Transform parent = muzzle.parent;
        Vector2 localDir = parent != null
            ? (Vector2)(Quaternion.Inverse(parent.rotation) * worldDir)
            : worldDir;

        float angle = Mathf.Atan2(localDir.y, localDir.x) * Mathf.Rad2Deg - 90f;
        muzzle.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    /// <summary>无限发射：冷却结束后直接发射一发普通球，不依赖任何库存。</summary>
    private void TryFire()
    {
        if (fireTimer > 0f) return;

        GameLogicManager.Instance.SpawnPinBall(BaseBallAddress, FirePosition, Direction, fireSpeed);

        fireTimer = fireInterval;

        if (playerRender != null)
            playerRender.PlayAttackAnimation();
    }
}

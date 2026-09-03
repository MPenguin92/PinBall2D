using System.Collections;
using UnityEngine;

/// <summary>
/// 玩家：发射弹珠 + 鼠标瞄准 + 生命值。
/// 弹珠为无限发射模式（2026-09-03 起不再维护库存队列）：
/// 发射不扣库存、回收不还库存，每次松开鼠标执行一次射击。
///
/// 「一次射击产出什么」由 <see cref="FireStrategy"/> 决定（单发 / 连发 / 扇形…），
/// 玩家自身只提供发射能力（生成球、延迟调度、当前瞄准方向），
/// 升级词条可通过 <see cref="SetFireStrategy"/> 替换射击模式。
///
/// 操作方式：按住鼠标左键时炮口持续跟随鼠标方向，松开鼠标发射。
/// </summary>
public class Player : MonoBehaviour, IFireExecutor
{
    /// <summary>普通球在 Addressables 中的地址（BaseBall.prefab，注册于 Unit 组）。</summary>
    private const string BaseBallAddress = "BaseBall";

    /// <summary>当前射击模式；默认单发，升级系统可替换。</summary>
    private FireStrategy fireStrategy = new SingleFireStrategy();

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

    /// <summary>当前射击策略（只读查看；切换用 <see cref="SetFireStrategy"/>）。</summary>
    public FireStrategy FireStrategy => fireStrategy;

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
        // 清掉上一局可能残留的连发延迟（Burst 策略的跨局安全）。
        StopAllCoroutines();

        // 每局从单发射击开始；射击模式的成长由升级词条在本局内叠加。
        fireStrategy = new SingleFireStrategy();

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
    /// 真机 Android/iOS 用触摸；Editor / 桌面用鼠标（切到移动目标时 Editor 也会定义 UNITY_ANDROID，故排除 UNITY_EDITOR）。
    /// </summary>
    private void HandlePointerInput()
    {
        if (muzzle == null || mainCamera == null) return;

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
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

    /// <summary>替换射击模式；传入 null 回退为单发。升级词条应用时调用。</summary>
    public void SetFireStrategy(FireStrategy strategy)
    {
        fireStrategy = strategy ?? new SingleFireStrategy();
    }

    // ---- IFireExecutor（Player 提供的发射能力）----

    public Vector2 BaseDirection => Direction;

    public void SpawnBall(Vector2 direction)
    {
        PinBallBase ball = GameLogicManager.Instance.SpawnPinBall(BaseBallAddress, FirePosition, direction, fireSpeed);

        // 发射时：生成成功才广播（供升级效果在球出生瞬间附加影响）。
        if (ball != null)
            BallEvents.RaiseFired(ball, FirePosition, direction, fireSpeed);
    }

    public void Delay(float seconds, System.Action action)
    {
        if (action == null) return;
        if (seconds <= 0f)
        {
            action();
            return;
        }
        StartCoroutine(DelayRoutine(seconds, action));
    }

    private IEnumerator DelayRoutine(float seconds, System.Action action)
    {
        yield return new WaitForSeconds(seconds);
        action();
    }

    /// <summary>无限发射入口：冷却结束后交给当前 FireStrategy 决定产出，随后进入冷却。</summary>
    private void TryFire()
    {
        if (fireTimer > 0f) return;

        if (fireStrategy != null)
            fireStrategy.Fire(this);

        fireTimer = fireInterval;

        if (playerRender != null)
            playerRender.PlayAttackAnimation();
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 玩家：发射弹珠 + 鼠标瞄准 + 生命值。
/// 弹珠为无限发射模式（2026-09-03 起不再维护库存队列）：
/// 发射不扣库存、回收不还库存，每次松开鼠标执行一次射击。
///
/// 「一次射击产出哪些球」由 <see cref="FireStrategy"/> 决定（发射序列：单发 / 连发主弹+副弹…），
/// 每颗弹按 Balls 表（id → prefab 地址）+ Balls_Level 表（等级 → 伤害）出池；
/// 玩家自身只提供发射能力（查表出池、延迟调度、当前瞄准方向），
/// 升级词条可通过 <see cref="SetFireStrategy"/> 替换射击模式。
///
/// 操作方式：按住鼠标左键时炮口持续跟随鼠标方向，松开鼠标发射。
/// </summary>
public class Player : MonoBehaviour, IFireExecutor
{
    /// <summary>当前射击模式；默认单发，升级系统可替换。</summary>
    private FireStrategy fireStrategy = new SingleFireStrategy();

    [SerializeField]
    [Tooltip("Player 最大生命值")]
    private int maxHp = 5;

    [SerializeField]
    private PlayerRender playerRender;

    [SerializeField]
    [Tooltip("炮口 Transform：局部 +Y 为发射点偏移；旋转与外观一起跟随瞄准。")]
    private Transform muzzle;

    [SerializeField]
    [Tooltip("随瞄准旋转的外观（一般为 render 子节点）；为空则只转炮口。")]
    private Transform aimVisual;

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

    /// <summary>本局内已按下瞄准（按下发生在 Running 且非 UI）；松开时才开火，避免点「开始」的抬起误射。</summary>
    private bool aimPressActive;

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

    /// <summary>当前发射位置（炮口世界坐标；炮口绕玩家中心旋转，始终落在瞄准轴上）。</summary>
    public Vector2 FirePosition => muzzle != null ? (Vector2)muzzle.position : (Vector2)transform.position;

    private void Awake()
    {
        mainCamera = Camera.main;
        if (aimVisual == null && playerRender != null)
            aimVisual = playerRender.transform;
    }

    public void Init()
    {
        // 清掉上一局可能残留的连发延迟（Burst 策略的跨局安全）。
        StopAllCoroutines();

        // 每局从单发射击开始；射击模式的成长由升级词条在本局内叠加。
        fireStrategy = new SingleFireStrategy();

        fireTimer = 0f;
        currentHp = maxHp;
        aimPressActive = false;
        SetAimRotation(Quaternion.identity);
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
    /// 必须在 Running 内完成「按下」才允许松开开火，避免点开始/UI 的抬起误射。
    /// </summary>
    private void HandlePointerInput()
    {
        if (mainCamera == null) return;

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        if (Input.touchCount <= 0) return;

        Touch touch = Input.GetTouch(0);
        if (touch.phase == TouchPhase.Began && !IsPointerOverUI(touch.fingerId))
            aimPressActive = true;

        if (aimPressActive && touch.phase != TouchPhase.Canceled)
            AimTowardScreen(touch.position);

        if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
        {
            if (aimPressActive && touch.phase == TouchPhase.Ended)
                TryFire();
            aimPressActive = false;
        }
#else
        if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
            aimPressActive = true;

        if (aimPressActive && (Input.GetMouseButton(0) || Input.GetMouseButtonUp(0)))
            AimTowardScreen(Input.mousePosition);

        if (Input.GetMouseButtonUp(0))
        {
            if (aimPressActive)
                TryFire();
            aimPressActive = false;
        }
#endif
    }

    private static bool IsPointerOverUI(int pointerId = -1)
    {
        EventSystem es = EventSystem.current;
        if (es == null) return false;
        return pointerId >= 0 ? es.IsPointerOverGameObject(pointerId) : es.IsPointerOverGameObject();
    }

    /// <summary>
    /// 以玩家中心为瞄准原点指向指针，再旋转炮口/外观。
    /// 避免「从带偏移的 muzzle 位置算方向再旋转」导致炮口绕飞、线与球不同轴。
    /// </summary>
    private void AimTowardScreen(Vector2 screenPos)
    {
        if (mainCamera == null) return;

        Vector2 origin = transform.position;
        Vector2 pointerWorld = ScreenToWorld2D(screenPos);
        Vector2 worldDir = pointerWorld - origin;
        if (worldDir.sqrMagnitude <= Mathf.Epsilon) return;

        worldDir.Normalize();
        float angle = Mathf.Atan2(worldDir.y, worldDir.x) * Mathf.Rad2Deg - 90f;
        SetAimRotation(Quaternion.Euler(0f, 0f, angle));
    }

    private Vector2 ScreenToWorld2D(Vector2 screenPos)
    {
        float zDist = Mathf.Abs(mainCamera.transform.position.z);
        Vector3 world = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, zDist));
        return new Vector2(world.x, world.y);
    }

    private void SetAimRotation(Quaternion localRotation)
    {
        if (muzzle != null)
            muzzle.localRotation = localRotation;
        if (aimVisual != null)
            aimVisual.localRotation = localRotation;
    }

    /// <summary>替换射击模式；传入 null 回退为单发。升级词条应用时调用。</summary>
    public void SetFireStrategy(FireStrategy strategy)
    {
        fireStrategy = strategy ?? new SingleFireStrategy();
    }

    // ---- IFireExecutor（Player 提供的发射能力）----

    public Vector2 BaseDirection => Direction;

    public void SpawnBall(Vector2 direction, FireShot shot)
    {
        GameLogicManager mgr = GameLogicManager.Instance;
        if (mgr == null) return;

        // prefab 地址按球型查 Balls 表；未登记的球型直接跳过（防御，避免空地址出池）。
        BallDefinition def = mgr.BallTable != null ? mgr.BallTable.Get(shot.BallId) : null;
        if (def == null) return;

        PinBallBase ball = mgr.SpawnPinBall(def.prefabAddress, FirePosition, direction, fireSpeed, shot.BallId, shot.Level);

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

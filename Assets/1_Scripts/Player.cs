using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家:发射弹珠 + 鼠标瞄准 + 生命值。
///
/// 弹珠库存为一个**全局 FIFO 队列**:发射 = 队首出队,球碰底回收 = 入队尾。
/// 默认 <see cref="initialBallCount"/> 个普通球(Base)入队;获得特殊球升级时,
/// 立即把对应数量的该 BallType 入队尾,容量随之增长。
/// 谁在队首就发谁——后发先回的球会"插队"到下一次发射位置。
///
/// 操作方式:按住鼠标左键时炮口持续跟随鼠标方向,松开鼠标发射一发。
/// </summary>
public class Player : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Player 最大生命值")]
    private int maxHp = 5;

    [SerializeField]
    [Tooltip("StartGame 时入队的初始普通球数量。")]
    private int initialBallCount = 5;

    [SerializeField]
    private PlayerRender playerRender;

    [SerializeField]
    [Tooltip("炮口 Transform，旋转与发射均以此为准；Player 本体不旋转。")]
    private Transform muzzle;

    /// <summary>屏幕→世界坐标转换用的主相机；无相机时禁用鼠标操作。</summary>
    private Camera mainCamera;

    /// <summary>各 BallType 的 Addressables 地址;默认填普通球的 BaseBall。</summary>
    private readonly Dictionary<BallType, string> ballAddress = new Dictionary<BallType, string>
    {
        { BallType.Base, "BaseBall" },
        { BallType.Fire, "FireBall" },
        { BallType.Ice, "IceBall" },
        { BallType.Lightning, "LightningBall" },
        { BallType.Poison, "PoisonBall" },
        { BallType.Heavy, "HeavyBall" },
        { BallType.Boomerang, "BoomerangBall" },
    };

    // FIFO 队列:队首=下一发,队尾=最新入队。Enqueue/Dequeue 是 O(1)。
    private readonly Queue<BallType> ballQueue = new Queue<BallType>();

    // 历史累计入队总数(含已发射在外的球)。等于"容量",用于 HUD 显示与 BallsInFlight 推导。
    private int totalBalls;

    // 已解锁过的特殊 BallType 集合(仅记录非 Base):用于"全部已解锁特殊球各 +N"类升级。
    private readonly HashSet<BallType> unlockedSpecials = new HashSet<BallType>();

    private float fireTimer;
    private int currentHp;

    public int CurrentHp => currentHp;

    public int MaxHp => maxHp;

    public bool IsDead => currentHp <= 0;

    /// <summary>当前队列内可发射的球数(不含飞行中的)。</summary>
    public int QueueCount => ballQueue.Count;

    /// <summary>历史入队总数,等价于"容量":队列内 + 飞行中 = TotalBalls。</summary>
    public int TotalBalls => totalBalls;

    /// <summary>当前飞行在外的球数。</summary>
    public int BallsInFlight => totalBalls - ballQueue.Count;

    /// <summary>HUD 用:按队列顺序(队首→队尾)只读暴露当前队列内容。</summary>
    public IReadOnlyCollection<BallType> BallQueue => ballQueue;

    /// <summary>当前发射冷却间隔,来自 <see cref="BallStats"/>。</summary>
    public float FireInterval
    {
        get
        {
            BallStats stats = GetStats();
            return stats != null ? stats.Get(BallStatType.FireInterval) : 0.3f;
        }
    }

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
        ballQueue.Clear();
        unlockedSpecials.Clear();
        totalBalls = 0;

        int initial = Mathf.Max(0, initialBallCount);
        for (int i = 0; i < initial; i++)
            ballQueue.Enqueue(BallType.Base);
        totalBalls = initial;

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

    public void Tick()
    {
        HandlePointerInput();

        if (playerRender != null)
            playerRender.Tick();

        if (fireTimer > 0f)
            fireTimer -= Time.deltaTime;
    }

    /// <summary>
    /// 球回收时调用(GameLogicManager.RecyclePinBall):按其类型入队尾,**不改变 totalBalls**。
    /// </summary>
    public void AddPinBall(BallType type)
    {
        ballQueue.Enqueue(type);
    }

    /// <summary>
    /// 升级解锁/扩容:在队尾追加 <paramref name="count"/> 个 <paramref name="type"/>,
    /// 同步增加 totalBalls。第一次添加非 Base 类型时会被记入"已解锁特殊球"集合。
    /// </summary>
    public void AddBalls(BallType type, int count)
    {
        if (count <= 0) return;
        for (int i = 0; i < count; i++)
            ballQueue.Enqueue(type);
        totalBalls += count;

        if (type != BallType.Base)
            unlockedSpecials.Add(type);
    }

    /// <summary>查询某种特殊球是否已被解锁(历史上有过入队)。Base 永远视为已解锁。</summary>
    public bool IsUnlocked(BallType type)
    {
        if (type == BallType.Base) return true;
        return unlockedSpecials.Contains(type);
    }

    /// <summary>已解锁的特殊球集合(只读),供升级"全部已解锁特殊球 +N"等场景使用。</summary>
    public IReadOnlyCollection<BallType> UnlockedSpecials => unlockedSpecials;

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

    private void TryFire()
    {
        if (fireTimer > 0f) return;
        if (ballQueue.Count == 0) return;

        BallType chosen = ballQueue.Dequeue();

        BallStats stats = GetStats();
        float speed = stats != null ? stats.Get(BallStatType.InitialSpeed) : 24f;
        string address = ResolveAddress(chosen);

        GameLogicManager.Instance.SpawnPinBall(address, FirePosition, Direction, speed);

        float fireInterval = stats != null ? stats.Get(BallStatType.FireInterval) : 0.3f;
        fireTimer = fireInterval;

        if (playerRender != null)
            playerRender.PlayAttackAnimation();
    }

    private string ResolveAddress(BallType type)
    {
        return ballAddress.TryGetValue(type, out string addr) ? addr : ballAddress[BallType.Base];
    }

    private BallStats GetStats()
    {
        GameLogicManager mgr = GameLogicManager.Instance;
        return mgr != null ? mgr.BallStats : null;
    }
}

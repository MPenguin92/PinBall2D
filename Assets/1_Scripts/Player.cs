using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家：发射弹珠 + 旋转控制 + 生命值。
///
/// 弹珠数值（最大库存 / 发射间隔 / 初速）统一从 <see cref="BallStats"/> 读取，
/// 不再保留独立 SerializeField；Inspector 中只剩输入控制与渲染相关字段。
///
/// 弹珠库存按 <see cref="BallType"/> 分槽：普通球初始库存上限 = BallStats.BasePinBallSlots，
/// 其余特殊球（火/冰/雷/...）默认 0/0，由升级解锁与扩容。
/// HandleFire 按 BallType 顺序找到第一个 current>0 的发射，特殊球优先。
/// </summary>
public class Player : MonoBehaviour
{
    [SerializeField]
    private float rotateSpeed = 120f;

    [SerializeField]
    private float maxAngle = 80f;

    [SerializeField]
    [Tooltip("Player 最大生命值")]
    private int maxHp = 5;

    [SerializeField]
    private PlayerRender playerRender;

    /// <summary>各 BallType 的 Addressables 地址；默认填普通球的 BaseBall。</summary>
    private static readonly Dictionary<BallType, string> BallAddress = new Dictionary<BallType, string>
    {
        { BallType.Base, "BaseBall" },
        { BallType.Fire, "FireBall" },
        { BallType.Ice, "IceBall" },
        { BallType.Lightning, "LightningBall" },
        { BallType.Poison, "PoisonBall" },
        { BallType.Heavy, "HeavyBall" },
        { BallType.Boomerang, "BoomerangBall" },
    };

    /// <summary>发射优先级（数组前面的优先消耗）：特殊球优先，最后才是普通球。</summary>
    private static readonly BallType[] FirePriority =
    {
        BallType.Fire,
        BallType.Ice,
        BallType.Lightning,
        BallType.Poison,
        BallType.Heavy,
        BallType.Boomerang,
        BallType.Base,
    };

    private readonly Dictionary<BallType, int> currentCounts = new Dictionary<BallType, int>();
    private readonly Dictionary<BallType, int> maxCounts = new Dictionary<BallType, int>();

    private float fireTimer;
    private int currentHp;

    public int CurrentHp => currentHp;

    public int MaxHp => maxHp;

    public bool IsDead => currentHp <= 0;

    /// <summary>
    /// 普通球当前数（兼容老 HUD）。
    /// </summary>
    public int CurrentPinBallCount => GetCurrentCount(BallType.Base);

    /// <summary>普通球库存上限（兼容老 HUD）。</summary>
    public int MaxPinBallCount => GetMaxCount(BallType.Base);

    /// <summary>所有 BallType 的当前/最大库存（HUD 多球种显示用）。</summary>
    public IReadOnlyDictionary<BallType, int> CurrentCounts => currentCounts;

    public IReadOnlyDictionary<BallType, int> MaxCounts => maxCounts;

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
        currentCounts.Clear();
        maxCounts.Clear();

        // 普通球初始上限来自 BallStats（StartGame 已 Reset 为基础值）。
        int baseSlots = ResolveBaseSlots();
        maxCounts[BallType.Base] = baseSlots;
        currentCounts[BallType.Base] = baseSlots;

        fireTimer = 0f;
        currentHp = maxHp;
        transform.rotation = Quaternion.identity;
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
        // 普通球库存上限可能因升级动态变化，这里每帧同步上限到 BallStats。
        SyncBaseSlotsCap();

        HandleRotation();
        HandleFire();

        if (playerRender != null)
            playerRender.Tick();

        if (fireTimer > 0f)
            fireTimer -= Time.deltaTime;
    }

    /// <summary>由 PoolManager 在弹珠回收时调用，把球归还到对应类型的库存。</summary>
    public void AddPinBall(BallType type, int count = 1)
    {
        int max = GetMaxCount(type);
        if (max <= 0) return;
        int cur = GetCurrentCount(type);
        currentCounts[type] = Mathf.Clamp(cur + count, 0, max);
    }

    /// <summary>给某 BallType 增加 N 个槽位（同步增加上限与当前库存）。</summary>
    public void AddBallSlot(BallType type, int slots)
    {
        if (slots <= 0) return;
        int max = GetMaxCount(type) + slots;
        int cur = GetCurrentCount(type) + slots;
        maxCounts[type] = max;
        currentCounts[type] = Mathf.Clamp(cur, 0, max);
    }

    public int GetCurrentCount(BallType type)
    {
        currentCounts.TryGetValue(type, out int v);
        return v;
    }

    public int GetMaxCount(BallType type)
    {
        maxCounts.TryGetValue(type, out int v);
        return v;
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
        if (fireTimer > 0f) return;

        BallType chosen = PickFireCandidate();
        if (chosen == BallType.Base && GetCurrentCount(BallType.Base) <= 0)
        {
            // 无任何弹珠可用。
            return;
        }
        if (GetCurrentCount(chosen) <= 0) return;

        BallStats stats = GetStats();
        float speed = stats != null ? stats.Get(BallStatType.InitialSpeed) : 10f;
        string address = ResolveAddress(chosen);

        GameLogicManager.Instance.SpawnPinBall(address, transform.position, Direction, speed);

        currentCounts[chosen] = GetCurrentCount(chosen) - 1;

        float fireInterval = stats != null ? stats.Get(BallStatType.FireInterval) : 0.3f;
        fireTimer = fireInterval;

        if (playerRender != null)
            playerRender.PlayAttackAnimation();
    }

    private BallType PickFireCandidate()
    {
        for (int i = 0; i < FirePriority.Length; i++)
        {
            BallType bt = FirePriority[i];
            if (GetMaxCount(bt) <= 0) continue;
            if (GetCurrentCount(bt) > 0) return bt;
        }
        return BallType.Base;
    }

    private string ResolveAddress(BallType type)
    {
        return BallAddress.TryGetValue(type, out string addr) ? addr : BallAddress[BallType.Base];
    }

    private static BallStats GetStats()
    {
        GameLogicManager mgr = GameLogicManager.Instance;
        return mgr != null ? mgr.BallStats : null;
    }

    private int ResolveBaseSlots()
    {
        BallStats stats = GetStats();
        return stats != null ? Mathf.Max(1, stats.GetInt(BallStatType.BasePinBallSlots)) : 5;
    }

    private void SyncBaseSlotsCap()
    {
        int newMax = ResolveBaseSlots();
        int oldMax = GetMaxCount(BallType.Base);
        if (newMax == oldMax) return;

        maxCounts[BallType.Base] = newMax;
        if (newMax > oldMax)
        {
            // 上限提升时，未发射的部分（差值）自动补到当前库存（手感更好）。
            int cur = GetCurrentCount(BallType.Base);
            currentCounts[BallType.Base] = Mathf.Min(newMax, cur + (newMax - oldMax));
        }
        else
        {
            int cur = GetCurrentCount(BallType.Base);
            currentCounts[BallType.Base] = Mathf.Min(newMax, cur);
        }
    }
}

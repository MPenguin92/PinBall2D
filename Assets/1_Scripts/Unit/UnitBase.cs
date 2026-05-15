using UnityEngine;

public class UnitBase : MonoBehaviour
{
    [SerializeField]
    private int maxHp = 3;

    [SerializeField]
    [Tooltip("Unit 触碰到下边框时对 Player 造成的伤害")]
    private int attack = 1;

    [SerializeField]
    [Tooltip("被击杀时给玩家累加的经验值（运行时由 Difficulty 当前阶段覆盖）")]
    private int experience = 1;

    [SerializeField]
    private UnitRender unitRender;

    private int currentHp;

    // 减速 buff（由 IcePinBall 等通过 ApplySlow 写入）：每个 Unit 私有的 step 节奏缩放因子。
    // 取值 (0, 1]。1 = 不减速；0.5 = 行为减半（Step 心跳每 2 次才执行一次个体移动）。
    private float slowFactor = 1f;
    private float slowRemaining;
    // 用浮点累计避免硬编码 step 计数；个体每收到一次 Step 累加 slowFactor，
    // 累计到 >= 1 才执行一次实际行为，并保留小数部分。
    private float slowStepAcc;

    public int CurrentHp => currentHp;

    public int MaxHp => maxHp;

    public int Attack => attack;

    /// <summary>本 Unit 被击杀时给玩家累加的经验值。由 Difficulty 当前阶段在 Init 时写入。</summary>
    public int Experience => experience;

    /// <summary>标准 Unit 为 1x1 正方形，尺寸统一来自 <see cref="Defines.UnitSize"/>。</summary>
    public float Width => Defines.UnitSize;

    public float Height => Defines.UnitSize;

    public Rect UnitRect { get; private set; }

    /// <summary>Unit 的移动方向（命中方向解算用）。基类默认向下；移动行为子类可重写。</summary>
    public virtual Vector2 MoveDirection => Vector2.down;

    public bool IsSlowed => slowFactor < 1f && slowRemaining > 0f;

    public float SlowFactor => slowFactor;

    public void Init()
    {
        ApplyDifficulty();
        currentHp = maxHp;
        slowFactor = 1f;
        slowRemaining = 0f;
        slowStepAcc = 0f;
        // 强制统一尺寸，避免预制体 scale 不为 1 导致视觉/碰撞与逻辑不一致。
        transform.localScale = Vector3.one * Defines.UnitSize;
        RefreshRect();
    }

    /// <summary>
    /// 从 <see cref="Difficulty"/> 读取当前阶段参数并覆盖字段；无表时保留 Inspector 默认值。
    /// 子类可重写以插入其他属性。
    /// </summary>
    protected virtual void ApplyDifficulty()
    {
        GameLogicManager mgr = GameLogicManager.Instance;
        if (mgr == null || mgr.Difficulty == null || !mgr.Difficulty.HasTable) return;

        maxHp = mgr.Difficulty.GetUnitHp();
        attack = mgr.Difficulty.GetUnitAttack();
        experience = mgr.Difficulty.GetUnitExperience();
    }

    public void RefreshRect()
    {
        Vector3 pos = transform.position;
        float size = Defines.UnitSize;
        UnitRect = new Rect(
            pos.x - size * 0.5f,
            pos.y - size * 0.5f,
            size,
            size
        );
    }

    /// <summary>由 GameLogicManager 每帧统一调用。基类默认推进减速倒计时，子类按需重写并 base.Tick。</summary>
    public virtual void Tick()
    {
        if (slowRemaining > 0f)
        {
            slowRemaining -= Time.deltaTime;
            if (slowRemaining <= 0f)
            {
                slowFactor = 1f;
                slowRemaining = 0f;
                slowStepAcc = 0f;
            }
        }
    }

    public bool TakeDamage(int damage)
    {
        if (damage <= 0 || currentHp <= 0)
            return currentHp <= 0;

        currentHp = Mathf.Max(0, currentHp - damage);
        if (unitRender != null)
            unitRender.PlayHitAnimation();

        if (currentHp <= 0)
        {
            if (unitRender != null)
                unitRender.PlayDeathAnimation();
        }

        return currentHp <= 0;
    }

    public void PlayReachBottomAnimation()
    {
        if (unitRender != null)
            unitRender.PlayReachBottomAnimation();
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

    /// <summary>
    /// 应用减速 buff：factor 为相对原速度的缩放（0~1，越小越慢），duration 秒。
    /// 多次叠加取「更慢的 factor + 更长的剩余时间」。
    /// </summary>
    public void ApplySlow(float factor, float duration)
    {
        if (duration <= 0f) return;
        float clamped = Mathf.Clamp(factor, 0.05f, 1f);
        if (clamped >= 1f) return;

        if (clamped < slowFactor) slowFactor = clamped;
        if (duration > slowRemaining) slowRemaining = duration;
    }

    /// <summary>
    /// 子类（SimpleUnit）在每次收到 Step 心跳时调用，决定本次是否真的执行移动行为。
    /// 当未减速时永远返回 true；减速时按 slowFactor 概率推进。
    /// </summary>
    protected bool ConsumeStepWithSlow()
    {
        if (slowFactor >= 1f) return true;

        slowStepAcc += slowFactor;
        if (slowStepAcc >= 1f)
        {
            slowStepAcc -= 1f;
            return true;
        }
        return false;
    }

    // 订阅 Step 事件：出池（SetActive(true)）时自动订阅，入池（SetActive(false)）时自动取消。
    // 这样依赖对象池生命周期，避免重复订阅或泄露。
    protected virtual void OnEnable()
    {
        GameEvents.OnStep += HandleStep;
    }

    protected virtual void OnDisable()
    {
        GameEvents.OnStep -= HandleStep;
    }

    /// <summary>收到 Step 心跳时调用。基类空实现，子类（如 SimpleUnit）重写以响应节奏。</summary>
    protected virtual void HandleStep()
    {
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawCube(transform.position, new Vector3(Width, Height, 0f));
    }
}

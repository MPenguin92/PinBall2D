using UnityEngine;

public class UnitBase : MonoBehaviour
{
    [SerializeField]
    private int maxHp = 3;

    [SerializeField]
    [Tooltip("Unit 触碰到下边框时对 Player 造成的伤害")]
    private int attack = 1;

    [SerializeField]
    private UnitRender unitRender;

    private int currentHp;

    public int CurrentHp => currentHp;

    public int MaxHp => maxHp;

    public int Attack => attack;

    /// <summary>标准 Unit 为 1x1 正方形，尺寸统一来自 <see cref="Defines.UnitSize"/>。</summary>
    public float Width => Defines.UnitSize;

    public float Height => Defines.UnitSize;

    public Rect UnitRect { get; private set; }

    public void Init()
    {
        ApplyDifficulty();
        currentHp = maxHp;
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

    /// <summary>由 GameLogicManager 每帧统一调用。基类默认无行为，子类按需重写。</summary>
    public virtual void Tick()
    {
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

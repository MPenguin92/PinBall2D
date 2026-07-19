using DG.Tweening;
using UnityEngine;

public class UnitRender : MonoBehaviour, ICombatAnimation
{
    [SerializeField]
    private UnitBase unit;

    [SerializeField]
    private SpriteRenderer spriteRenderer;

    [Header("Hit")]
    [SerializeField]
    private Color hitFlashColor = Color.white;

    [SerializeField]
    private float hitFlashDuration = 0.12f;

    [SerializeField]
    private float hitPunchScale = 0.16f;

    [Header("Slow")]
    [SerializeField]
    [Tooltip("被减速 buff 命中时整体染色到这个色;buff 结束后恢复原色。")]
    private Color slowTintColor = new Color(0.55f, 0.85f, 1f, 1f);

    private Color originalColor;
    private Vector3 originalScale;
    private Sequence hitSequence;
    private bool isPlayingHitAnimation;
    private bool isSlowedVisual;

    private void Awake()
    {
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        isSlowedVisual = false;
        ResetRenderState();
    }

    private void OnDisable()
    {
        hitSequence?.Kill();
        isPlayingHitAnimation = false;
    }

    public void Tick()
    {
        if (unit == null || spriteRenderer == null) return;
        if (isPlayingHitAnimation) return;

        //spriteRenderer.color = GetHpColor();
    }

    /// <summary>
    /// 由 UnitBase.Tick 每帧调用,把 IsSlowed 状态映射到外观染色上。
    /// 命中闪白动画期间不覆盖颜色,等动画结束后下一帧自然恢复。
    /// </summary>
    public void SetSlowVisual(bool on)
    {
        isSlowedVisual = on;
        if (spriteRenderer == null || isPlayingHitAnimation) return;
        spriteRenderer.color = on ? slowTintColor : originalColor;
    }

    public virtual void PlayAttackAnimation()
    {
    }

    /// <summary>
    /// ICombatAnimation 入口；无球种信息时按 Base 播受击。
    /// </summary>
    public virtual void PlayHitAnimation()
    {
        PlayHitAnimation(BallType.Base);
    }

    /// <summary>
    /// 受击反馈：本体闪白 + PunchScale，并按球种播 Hit VFX。
    /// 由 <see cref="UnitBase.TakeDamage"/> 在未击杀时调用。
    /// </summary>
    public virtual void PlayHitAnimation(BallType sourceType)
    {
        PlayHitFlash();
        PlayHitVfx(sourceType);
    }

    /// <summary>
    /// ICombatAnimation 入口；无球种信息时按 Base 播死亡。
    /// </summary>
    public virtual void PlayDeathAnimation()
    {
        PlayDeathAnimation(BallType.Base);
    }

    /// <summary>
    /// 死亡反馈：闪白 + 按球种播 Kill VFX（无 Kill 地址时回退 Hit）。
    /// 由 <see cref="UnitBase.TakeDamage"/> 在击杀时调用。
    /// </summary>
    public virtual void PlayDeathAnimation(BallType sourceType)
    {
        PlayHitFlash();
        PlayKillVfx(sourceType);
    }

    public virtual void PlayReachBottomAnimation()
    {
    }

    private void PlayHitFlash()
    {
        if (spriteRenderer == null) return;

        hitSequence?.Kill();
        transform.localScale = originalScale;
        isPlayingHitAnimation = true;

        Color restoreColor = isSlowedVisual ? slowTintColor : originalColor;
        hitSequence = DOTween.Sequence()
            .AppendCallback(() => spriteRenderer.color = hitFlashColor)
            .Join(transform.DOPunchScale(Vector3.one * hitPunchScale, hitFlashDuration, 8, 0.6f))
            .Append(spriteRenderer.DOColor(restoreColor, hitFlashDuration))
            .OnComplete(() => isPlayingHitAnimation = false);
    }

    private void PlayHitVfx(BallType sourceType)
    {
        GameLogicManager mgr = GameLogicManager.Instance;
        if (mgr == null || mgr.VfxSpawner == null) return;
        mgr.VfxSpawner.PlayHit(sourceType, transform.position);
    }

    private void PlayKillVfx(BallType sourceType)
    {
        GameLogicManager mgr = GameLogicManager.Instance;
        if (mgr == null || mgr.VfxSpawner == null) return;
        mgr.VfxSpawner.PlayKill(sourceType, transform.position);
    }

    private Color GetHpColor()
    {
        float hpRatio = unit != null && unit.MaxHp > 0 ? (float)unit.CurrentHp / unit.MaxHp : 0f;
        return Color.Lerp(Color.gray, originalColor, hpRatio);
    }

    private void ResetRenderState()
    {
        hitSequence?.Kill();
        isPlayingHitAnimation = false;
        transform.localScale = originalScale;

        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
    }
}

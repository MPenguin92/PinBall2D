using DG.Tweening;
using UnityEngine;

public class UnitRender : MonoBehaviour, ICombatAnimation
{
    private static readonly int FillAmountId = Shader.PropertyToID("_FillAmount");
    private static readonly int ColorFullId = Shader.PropertyToID("_ColorFull");
    private static readonly int ColorEmptyId = Shader.PropertyToID("_ColorEmpty");

    [SerializeField]
    private UnitBase unit;

    [SerializeField]
    private SpriteRenderer spriteRenderer;

    [Header("HP Fill")]
    [SerializeField]
    [Tooltip("相对 prefab Color 提亮，作为满血亮色。")]
    private float fullBrightness = 1.15f;

    [SerializeField]
    [Tooltip("相对 prefab Color 压暗，作为空血底色。")]
    private float emptyBrightness = 0.55f;

    [Header("Hit")]
    [SerializeField]
    private Color hitFlashColor = new Color(1f, 1f, 1f, 1f);

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("受击时往闪白色混合的强度；1=整块纯闪白色，0=无闪白。")]
    private float hitFlashStrength = 0.35f;

    [SerializeField]
    private float hitFlashDuration = 0.12f;

    [SerializeField]
    [Tooltip("受击缩放弹一下的幅度（相对原 scale）。")]
    private float hitPunchScale = 0.12f;

    [SerializeField]
    [Tooltip("受击缩放到恢复的总时长（秒）。")]
    private float hitScaleDuration = 0.2f;

    private Color baseColor = Color.white;
    private Color colorFull = Color.white;
    private Color colorEmpty = Color.white;
    private Vector3 originalScale;
    private Sequence hitSequence;
    private bool isPlayingHitAnimation;
    private MaterialPropertyBlock propertyBlock;

    /// <summary>单位底色（prefab 上 SpriteRenderer.color）。</summary>
    public Color BaseColor => baseColor;

    private void Awake()
    {
        originalScale = transform.localScale;
        propertyBlock = new MaterialPropertyBlock();

        // prefab 上 SpriteRenderer.color 作为该单位色相；渲染时用白色乘遮罩，颜色走 shader。
        if (spriteRenderer != null)
        {
            baseColor = spriteRenderer.color;
            colorFull = ScaleRgb(baseColor, fullBrightness);
            colorEmpty = ScaleRgb(baseColor, emptyBrightness);
            spriteRenderer.color = Color.white;
        }
    }

    private void OnEnable()
    {
        ApplyHpFill(1f);
    }

    private void OnDisable()
    {
        hitSequence?.Kill();
        isPlayingHitAnimation = false;
    }

    /// <summary>由 <see cref="UnitBase.Init"/> 在数值就绪后调用，按当前血量刷新 fill。</summary>
    public void SyncColorFromHp()
    {
        if (isPlayingHitAnimation) return;
        ApplyHpFill();
    }

    public void Tick()
    {
        if (unit == null || spriteRenderer == null) return;
        if (isPlayingHitAnimation) return;
        ApplyHpFill();
    }

    public virtual void PlayAttackAnimation()
    {
    }

    public virtual void PlayHitAnimation()
    {
        PlayHitAnimation(BallType.Base);
    }

    public virtual void PlayHitAnimation(BallType sourceType)
    {
        PlayHitFeedback();
    }

    public virtual void PlayDeathAnimation()
    {
        PlayDeathAnimation(BallType.Base, Color.white);
    }

    public virtual void PlayDeathAnimation(BallType sourceType)
    {
        PlayDeathAnimation(sourceType, Color.white);
    }

    public virtual void PlayDeathAnimation(BallType sourceType, Color sourceBallColor)
    {
        PlayShatterDebris(sourceBallColor);
    }

    public virtual void PlayReachBottomAnimation()
    {
    }

    private void PlayShatterDebris(Color ballColor)
    {
        GameLogicManager mgr = GameLogicManager.Instance;
        if (mgr == null || mgr.VfxSpawner == null) return;
        mgr.VfxSpawner.PlayShatter(transform.position, baseColor, ballColor);
    }

    /// <summary>未击杀受击：轻闪 + 快速缩放回弹。</summary>
    private void PlayHitFeedback()
    {
        if (spriteRenderer == null) return;

        hitSequence?.Kill();
        transform.localScale = originalScale;
        isPlayingHitAnimation = true;

        float punch = Mathf.Max(0f, hitPunchScale);
        float duration = Mathf.Max(0.01f, hitScaleDuration);

        hitSequence = DOTween.Sequence()
            .AppendCallback(() => ApplyHpFill(flash: true))
            .Append(transform.DOScale(originalScale * (1f + punch), duration * 0.4f).SetEase(Ease.OutQuad))
            .Append(transform.DOScale(originalScale, duration * 0.6f).SetEase(Ease.InOutQuad))
            .InsertCallback(Mathf.Min(hitFlashDuration, duration), () => ApplyHpFill(flash: false))
            .OnComplete(() =>
            {
                transform.localScale = originalScale;
                isPlayingHitAnimation = false;
                ApplyHpFill(flash: false);
            });
    }

    private void ApplyHpFill(float? fillOverride = null, bool flash = false)
    {
        if (spriteRenderer == null) return;

        float fill = fillOverride ?? GetHpRatio();
        Color full = flash ? Color.Lerp(colorFull, hitFlashColor, hitFlashStrength) : colorFull;
        Color empty = flash ? Color.Lerp(colorEmpty, hitFlashColor, hitFlashStrength) : colorEmpty;

        spriteRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(FillAmountId, fill);
        propertyBlock.SetColor(ColorFullId, full);
        propertyBlock.SetColor(ColorEmptyId, empty);
        spriteRenderer.SetPropertyBlock(propertyBlock);
    }

    private float GetHpRatio()
    {
        if (unit == null || unit.MaxHp <= 0) return 1f;
        return Mathf.Clamp01((float)unit.CurrentHp / unit.MaxHp);
    }

    private static Color ScaleRgb(Color c, float scale)
    {
        return new Color(
            Mathf.Clamp01(c.r * scale),
            Mathf.Clamp01(c.g * scale),
            Mathf.Clamp01(c.b * scale),
            c.a);
    }
}

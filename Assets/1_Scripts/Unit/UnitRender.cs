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
    private float hitPunchScale = 0.16f;

    private Color baseColor = Color.white;
    private Color colorFull = Color.white;
    private Color colorEmpty = Color.white;
    private Vector3 originalScale;
    private Sequence hitSequence;
    private bool isPlayingHitAnimation;
    private MaterialPropertyBlock propertyBlock;

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
        PlayHitFlash();
        PlayHitVfx(sourceType);
    }

    public virtual void PlayDeathAnimation()
    {
        PlayDeathAnimation(BallType.Base);
    }

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

        hitSequence = DOTween.Sequence()
            .AppendCallback(() => ApplyHpFill(flash: true))
            .Join(transform.DOPunchScale(Vector3.one * hitPunchScale, hitFlashDuration, 8, 0.6f))
            .AppendInterval(hitFlashDuration)
            .AppendCallback(() =>
            {
                isPlayingHitAnimation = false;
                ApplyHpFill(flash: false);
            });
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

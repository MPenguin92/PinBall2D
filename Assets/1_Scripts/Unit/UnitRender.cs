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

    [Header("Death")]
    [SerializeField]
    private float deathEffectDuration = 0.35f;

    [SerializeField]
    private int deathShardCount = 8;

    [SerializeField]
    private float deathShardDistance = 0.8f;

    [SerializeField]
    private float deathShardScale = 0.18f;

    private Color originalColor;
    private Vector3 originalScale;
    private Sequence hitSequence;
    private bool isPlayingHitAnimation;

    private void Awake()
    {
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        originalScale = transform.localScale;
    }

    private void OnEnable()
    {
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

    public virtual void PlayAttackAnimation()
    {
    }

    public virtual void PlayHitAnimation()
    {
        if (spriteRenderer == null) return;

        hitSequence?.Kill();
        transform.localScale = originalScale;
        isPlayingHitAnimation = true;

        hitSequence = DOTween.Sequence()
            .AppendCallback(() => spriteRenderer.color = hitFlashColor)
            .Join(transform.DOPunchScale(Vector3.one * hitPunchScale, hitFlashDuration, 8, 0.6f))
            .Append(spriteRenderer.DOColor(spriteRenderer.color, hitFlashDuration))
            .OnComplete(() => isPlayingHitAnimation = false);
    }

    public virtual void PlayDeathAnimation()
    {
        SpawnDeathEffect();
    }

    public virtual void PlayReachBottomAnimation()
    {
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

    private void SpawnDeathEffect()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null)
            return;

        GameObject root = new GameObject("UnitDeathEffect");
        root.transform.position = transform.position;

        SpawnDeathShockwave(root.transform);
        SpawnDeathShards(root.transform);

        Destroy(root, deathEffectDuration + 0.1f);
    }

    private void SpawnDeathShockwave(Transform root)
    {
        SpriteRenderer shockwave = CreateEffectSprite("Shockwave", root);
        shockwave.transform.localScale = transform.lossyScale;
        shockwave.sortingLayerID = spriteRenderer.sortingLayerID;
        shockwave.sortingOrder = spriteRenderer.sortingOrder + 1;
        shockwave.color = originalColor;

        shockwave.transform
            .DOScale(transform.lossyScale * 1.6f, deathEffectDuration)
            .SetEase(Ease.OutQuad);
        shockwave
            .DOFade(0f, deathEffectDuration)
            .SetEase(Ease.OutQuad);
    }

    private void SpawnDeathShards(Transform root)
    {
        int count = Mathf.Max(0, deathShardCount);
        for (int i = 0; i < count; i++)
        {
            float angle = Mathf.PI * 2f * i / count;
            Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);

            SpriteRenderer shard = CreateEffectSprite($"Shard_{i + 1}", root);
            shard.transform.localScale = transform.lossyScale * deathShardScale;
            shard.sortingLayerID = spriteRenderer.sortingLayerID;
            shard.sortingOrder = spriteRenderer.sortingOrder + 2;
            shard.color = originalColor;

            Vector3 targetPosition = shard.transform.position + direction * deathShardDistance;
            shard.transform
                .DOMove(targetPosition, deathEffectDuration)
                .SetEase(Ease.OutCubic);
            shard.transform
                .DORotate(new Vector3(0f, 0f, Random.Range(-240f, 240f)), deathEffectDuration, RotateMode.FastBeyond360)
                .SetEase(Ease.OutCubic);
            shard
                .DOFade(0f, deathEffectDuration)
                .SetEase(Ease.InQuad);
        }
    }

    private SpriteRenderer CreateEffectSprite(string objectName, Transform root)
    {
        GameObject effectObject = new GameObject(objectName);
        effectObject.transform.SetParent(root, false);
        effectObject.transform.position = transform.position;

        SpriteRenderer effectRenderer = effectObject.AddComponent<SpriteRenderer>();
        effectRenderer.sprite = spriteRenderer.sprite;
        effectRenderer.material = spriteRenderer.sharedMaterial;
        return effectRenderer;
    }
}

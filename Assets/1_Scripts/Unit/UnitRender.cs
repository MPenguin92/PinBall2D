using UnityEngine;

public class UnitRender : MonoBehaviour, ICombatAnimation
{
    [SerializeField]
    private UnitBase unit;

    [SerializeField]
    private SpriteRenderer spriteRenderer;

    private Color originalColor;

    private void Awake()
    {
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    public void Tick()
    {
        if (unit == null || spriteRenderer == null) return;

        float hpRatio = unit.MaxHp > 0 ? (float)unit.CurrentHp / unit.MaxHp : 0f;
        spriteRenderer.color = Color.Lerp(Color.gray, originalColor, hpRatio);
    }

    public virtual void PlayAttackAnimation()
    {
    }

    public virtual void PlayHitAnimation()
    {
    }

    public virtual void PlayDeathAnimation()
    {
    }

    public virtual void PlayReachBottomAnimation()
    {
    }
}

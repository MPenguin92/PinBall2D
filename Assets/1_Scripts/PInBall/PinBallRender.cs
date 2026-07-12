using UnityEngine;

public class PinBallRender : MonoBehaviour
{
    private static BallSpriteSet spriteSet;

    [SerializeField]
    private PinBallBase pinBall;

    [SerializeField]
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        ApplySprite();
    }

    private void OnEnable()
    {
        ApplySprite();
    }

    public void Tick()
    {
    }

    private void ApplySprite()
    {
        if (spriteRenderer == null || pinBall == null)
            return;

        if (spriteSet == null)
            spriteSet = AssetLoader.Load<BallSpriteSet>("BallSpriteSet");

        Sprite sprite = spriteSet != null ? spriteSet.Get(pinBall.BallType) : null;
        if (sprite != null)
            spriteRenderer.sprite = sprite;
    }
}

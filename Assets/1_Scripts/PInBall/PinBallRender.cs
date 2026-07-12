using UnityEngine;
using UnityEngine.Rendering;

public class PinBallRender : MonoBehaviour
{
    [SerializeField]
    private PinBallBase pinBall;

    [SerializeField]
    private SpriteRenderer spriteRenderer;

    private BallSpriteSet spriteSet;
    private BallTrailSet trailSet;
    private Material trailMaterial;
    private TrailRenderer trailRenderer;

    private void Awake()
    {
        EnsureTrailRenderer();
        ApplySprite();
        ApplyTrail();
    }

    private void OnEnable()
    {
        ApplySprite();
        ApplyTrail();
        StopTrail();
    }

    private void OnDisable()
    {
        StopTrail();
    }

    public void Tick()
    {
    }

    private void EnsureTrailRenderer()
    {
        if (trailRenderer != null)
            return;

        trailRenderer = GetComponent<TrailRenderer>();
        if (trailRenderer == null)
            trailRenderer = gameObject.AddComponent<TrailRenderer>();

        trailRenderer.autodestruct = false;
        trailRenderer.minVertexDistance = 0.05f;
        trailRenderer.alignment = LineAlignment.TransformZ;
        trailRenderer.textureMode = LineTextureMode.Stretch;
        trailRenderer.numCornerVertices = 2;
        trailRenderer.numCapVertices = 2;
        trailRenderer.shadowCastingMode = ShadowCastingMode.Off;
        trailRenderer.receiveShadows = false;
        trailRenderer.generateLightingData = false;

        if (trailMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                trailMaterial = new Material(shader);
        }

        if (trailMaterial != null)
            trailRenderer.sharedMaterial = trailMaterial;
    }

    private void ApplyTrail()
    {
        if (trailRenderer == null || pinBall == null)
            return;

        BallTrailSet set = ResolveTrailSet();
        if (set != null)
            set.ApplyTo(trailRenderer, pinBall.BallType);

        if (spriteRenderer != null)
        {
            trailRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
            trailRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;
        }
    }

    /// <summary>
    /// 出池并完成位置设置后调用，清除旧轨迹并开始发射。
    /// </summary>
    public void ResetTrailAfterSpawn()
    {
        EnsureTrailRenderer();
        if (trailRenderer == null)
            return;

        trailRenderer.emitting = false;
        trailRenderer.Clear();
        trailRenderer.emitting = true;
    }

    private void StopTrail()
    {
        if (trailRenderer == null)
            return;

        trailRenderer.emitting = false;
        trailRenderer.Clear();
    }

    private void ApplySprite()
    {
        if (spriteRenderer == null || pinBall == null)
            return;

        BallSpriteSet set = ResolveSpriteSet();
        Sprite sprite = set != null ? set.Get(pinBall.BallType) : null;
        if (sprite != null)
            spriteRenderer.sprite = sprite;
    }

    private BallSpriteSet ResolveSpriteSet()
    {
        if (spriteSet != null)
            return spriteSet;

        spriteSet = AssetLoader.Load<BallSpriteSet>("BallSpriteSet");
        return spriteSet;
    }

    private BallTrailSet ResolveTrailSet()
    {
        if (trailSet != null)
            return trailSet;

        trailSet = AssetLoader.Load<BallTrailSet>("BallTrailSet");
        return trailSet;
    }
}
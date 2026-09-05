using UnityEngine;
using UnityEngine.Rendering;

public class PinBallRender : MonoBehaviour
{
    [SerializeField]
    private PinBallBase pinBall;

    [SerializeField]
    private SpriteRenderer spriteRenderer;

    private Material trailMaterial;
    private TrailRenderer trailRenderer;
    private string loadedSpriteAddress;

    /// <summary>弹珠当前底色（SpriteRenderer.color），供击碎等特效取色。</summary>
    public Color DisplayColor => spriteRenderer != null ? spriteRenderer.color : Color.white;

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

        BallDefinition def = ResolveBallDefinition();
        if (def != null)
            def.ApplyTrail(trailRenderer);

        if (spriteRenderer != null)
        {
            trailRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
            trailRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;
        }
    }

    /// <summary>
    /// 出池并完成球型注入后调用：套用外观/拖尾，清除旧轨迹并开始发射。
    /// </summary>
    public void ResetTrailAfterSpawn()
    {
        EnsureTrailRenderer();
        ApplySprite();
        ApplyTrail();
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

        BallDefinition def = ResolveBallDefinition();
        if (def == null || string.IsNullOrEmpty(def.spriteAddress))
            return;

        if (loadedSpriteAddress == def.spriteAddress && spriteRenderer.sprite != null)
            return;

        Sprite sprite = AssetLoader.Load<Sprite>(def.spriteAddress);
        if (sprite == null)
            return;

        spriteRenderer.sprite = sprite;
        loadedSpriteAddress = def.spriteAddress;
    }

    private BallDefinition ResolveBallDefinition()
    {
        GameLogicManager mgr = GameLogicManager.Instance;
        if (mgr == null || mgr.BallTable == null || pinBall == null)
            return null;
        return mgr.BallTable.Get(pinBall.BallId);
    }
}

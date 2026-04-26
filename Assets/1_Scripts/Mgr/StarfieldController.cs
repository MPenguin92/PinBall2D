using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 在黑色背景上生成一层随机星空，并给每颗星独立的明暗与缩放闪烁。
/// 挂到场景空物体即可使用；未指定 Sprite 时会运行时创建一个备用星星贴图。
/// </summary>
public class StarfieldController : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Camera targetCamera;

    [SerializeField]
    private Sprite starSprite;

    [Header("Spawn")]
    [SerializeField]
    private int starCount = 120;

    [SerializeField]
    private bool useCameraBounds = true;

    [SerializeField]
    private Rect spawnArea = new Rect(-5f, -9f, 10f, 18f);

    [SerializeField]
    private float cameraPadding = 0.4f;

    [SerializeField]
    private float zPosition = 8f;

    [SerializeField]
    private Vector2 sizeRange = new Vector2(0.035f, 0.16f);

    [SerializeField]
    private Vector2 initialAlphaRange = new Vector2(0.35f, 0.9f);

    [SerializeField]
    private int randomSeed;

    [Header("Twinkle")]
    [SerializeField]
    private Vector2 twinkleSpeedRange = new Vector2(0.8f, 2.6f);

    [SerializeField]
    [Range(0f, 1f)]
    private float alphaTwinkleStrength = 0.45f;

    [SerializeField]
    [Range(0f, 1f)]
    private float scaleTwinkleStrength = 0.18f;

    [Header("Render")]
    [SerializeField]
    private string sortingLayerName = "Default";

    [SerializeField]
    private int sortingOrder = -100;

    private readonly List<Star> stars = new List<Star>();
    private Sprite generatedSprite;
    private Texture2D generatedTexture;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void OnEnable()
    {
        Rebuild();
    }

    private void OnDisable()
    {
        ClearStars();
    }

    private void OnDestroy()
    {
        if (generatedSprite != null)
            Destroy(generatedSprite);

        if (generatedTexture != null)
            Destroy(generatedTexture);
    }

    private void Update()
    {
        float time = Time.time;
        for (int i = 0; i < stars.Count; i++)
        {
            Star star = stars[i];
            if (star.Renderer == null) continue;

            float wave = (Mathf.Sin(time * star.TwinkleSpeed + star.Phase) + 1f) * 0.5f;
            float alpha = Mathf.Clamp01(star.BaseAlpha * (1f - alphaTwinkleStrength + wave * alphaTwinkleStrength));
            float scale = star.BaseScale * (1f - scaleTwinkleStrength + wave * scaleTwinkleStrength);

            Color color = star.Renderer.color;
            color.a = alpha;
            star.Renderer.color = color;
            star.Renderer.transform.localScale = Vector3.one * scale;
        }
    }

    [ContextMenu("Rebuild Stars")]
    public void Rebuild()
    {
        ClearStars();

        if (starCount <= 0)
            return;

        Sprite sprite = starSprite != null ? starSprite : GetGeneratedSprite();
        Rect area = GetSpawnArea();
        System.Random random = randomSeed == 0 ? new System.Random() : new System.Random(randomSeed);

        for (int i = 0; i < starCount; i++)
        {
            GameObject starObject = new GameObject($"Star_{i:000}");
            starObject.transform.SetParent(transform, false);

            float x = Mathf.Lerp(area.xMin, area.xMax, NextFloat(random));
            float y = Mathf.Lerp(area.yMin, area.yMax, NextFloat(random));
            starObject.transform.position = new Vector3(x, y, zPosition);

            SpriteRenderer renderer = starObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerName = sortingLayerName;
            renderer.sortingOrder = sortingOrder;

            float alpha = Mathf.Lerp(initialAlphaRange.x, initialAlphaRange.y, NextFloat(random));
            renderer.color = new Color(1f, 1f, 1f, alpha);

            float scale = Mathf.Lerp(sizeRange.x, sizeRange.y, NextFloat(random));
            starObject.transform.localScale = Vector3.one * scale;

            stars.Add(new Star
            {
                Renderer = renderer,
                BaseScale = scale,
                BaseAlpha = alpha,
                TwinkleSpeed = Mathf.Lerp(twinkleSpeedRange.x, twinkleSpeedRange.y, NextFloat(random)),
                Phase = NextFloat(random) * Mathf.PI * 2f
            });
        }
    }

    [ContextMenu("Clear Stars")]
    public void ClearStars()
    {
        for (int i = stars.Count - 1; i >= 0; i--)
        {
            if (stars[i].Renderer == null) continue;

            GameObject starObject = stars[i].Renderer.gameObject;
            if (Application.isPlaying)
                Destroy(starObject);
            else
                DestroyImmediate(starObject);
        }

        stars.Clear();
    }

    private Rect GetSpawnArea()
    {
        if (!useCameraBounds || targetCamera == null)
            return spawnArea;

        float height = targetCamera.orthographicSize * 2f;
        float width = height * targetCamera.aspect;
        Vector3 cameraPosition = targetCamera.transform.position;

        return new Rect(
            cameraPosition.x - width * 0.5f - cameraPadding,
            cameraPosition.y - height * 0.5f - cameraPadding,
            width + cameraPadding * 2f,
            height + cameraPadding * 2f
        );
    }

    private Sprite GetGeneratedSprite()
    {
        if (generatedSprite != null)
            return generatedSprite;

        const int size = 64;
        generatedTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        generatedTexture.name = "GeneratedStar";
        generatedTexture.wrapMode = TextureWrapMode.Clamp;
        generatedTexture.filterMode = FilterMode.Bilinear;

        float center = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float radius = Mathf.Sqrt(dx * dx + dy * dy);
                float core = Mathf.Exp(-radius * radius * 90f);
                float glow = Mathf.Exp(-radius * radius * 8f) * 0.35f;
                float cross = Mathf.Max(0f, 1f - Mathf.Min(Mathf.Abs(dx), Mathf.Abs(dy)) * 12f) * Mathf.Max(0f, 1f - radius);
                float alpha = Mathf.Clamp01(core + glow + cross * 0.75f);

                generatedTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        generatedTexture.Apply();
        generatedSprite = Sprite.Create(generatedTexture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        generatedSprite.name = "GeneratedStarSprite";
        return generatedSprite;
    }

    private static float NextFloat(System.Random random)
    {
        return (float)random.NextDouble();
    }

    [Serializable]
    private class Star
    {
        public SpriteRenderer Renderer;
        public float BaseScale;
        public float BaseAlpha;
        public float TwinkleSpeed;
        public float Phase;
    }
}

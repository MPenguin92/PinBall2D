using System;
using UnityEngine;

/// <summary>
/// 单个 BallType 的 TrailRenderer 样式参数。
/// </summary>
[Serializable]
public struct BallTrailStyle
{
    public Color startColor;
    public Color endColor;
    public float time;
    public float startWidth;
    public float endWidth;
}

/// <summary>
/// 各 BallType 对应的拖尾样式映射表，由 Addressables 加载。
/// 数组下标与 <see cref="BallType"/> 枚举一致；数值以 <c>BallTrailSet.asset</c> 为准。
/// </summary>
[CreateAssetMenu(fileName = "BallTrailSet", menuName = "PinBall2D/Data/BallTrailSet", order = 5)]
public class BallTrailSet : ScriptableObject
{
    [SerializeField]
    private BallTrailStyle[] styles = new BallTrailStyle[]
    {
        // Base
        new BallTrailStyle
        {
            startColor = new Color(1f, 1f, 1f, 0.55f),
            endColor = new Color(1f, 1f, 1f, 0f),
            time = 0.12f,
            startWidth = 0.18f,
            endWidth = 0.04f,
        },
        // Fire
        new BallTrailStyle
        {
            startColor = new Color(1f, 0.55f, 0.1f, 0.85f),
            endColor = new Color(1f, 0.15f, 0f, 0f),
            time = 0.25f,
            startWidth = 0.24f,
            endWidth = 0.04f,
        },
        // Ice
        new BallTrailStyle
        {
            startColor = new Color(0.55f, 0.9f, 1f, 0.75f),
            endColor = new Color(1f, 1f, 1f, 0f),
            time = 0.2f,
            startWidth = 0.2f,
            endWidth = 0.04f,
        },
        // Lightning
        new BallTrailStyle
        {
            startColor = new Color(0.7f, 0.85f, 1f, 0.9f),
            endColor = new Color(0.4f, 0.6f, 1f, 0f),
            time = 0.08f,
            startWidth = 0.18f,
            endWidth = 0.02f,
        },
        // Poison
        new BallTrailStyle
        {
            startColor = new Color(0.35f, 0.95f, 0.25f, 0.8f),
            endColor = new Color(0.1f, 0.5f, 0.1f, 0f),
            time = 0.2f,
            startWidth = 0.22f,
            endWidth = 0.05f,
        },
        // Heavy
        new BallTrailStyle
        {
            startColor = new Color(0.75f, 0.75f, 0.8f, 0.7f),
            endColor = new Color(0.45f, 0.45f, 0.5f, 0f),
            time = 0.15f,
            startWidth = 0.3f,
            endWidth = 0.08f,
        },
        // Boomerang
        new BallTrailStyle
        {
            startColor = new Color(1f, 0.9f, 0.25f, 0.8f),
            endColor = new Color(1f, 0.65f, 0.1f, 0f),
            time = 0.18f,
            startWidth = 0.22f,
            endWidth = 0.05f,
        },
    };

    public BallTrailStyle Get(BallType type)
    {
        int index = (int)type;
        if (styles == null || index < 0 || index >= styles.Length)
            return default;

        return styles[index];
    }

    public void ApplyTo(TrailRenderer trail, BallType type)
    {
        if (trail == null)
            return;

        BallTrailStyle style = Get(type);
        trail.time = style.time;
        trail.startWidth = style.startWidth;
        trail.endWidth = style.endWidth;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(style.startColor, 0f),
                new GradientColorKey(style.endColor, 1f),
            },
            new[]
            {
                new GradientAlphaKey(style.startColor.a, 0f),
                new GradientAlphaKey(style.endColor.a, 1f),
            }
        );
        trail.colorGradient = gradient;
    }
}
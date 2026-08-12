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
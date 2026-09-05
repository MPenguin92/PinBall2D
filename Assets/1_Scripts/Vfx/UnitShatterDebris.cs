using UnityEngine;

/// <summary>
/// Unit 击碎碎块：运行时只注入双色；粒子参数全部在 Prefab 上配。
/// Prefab 地址：VFX/UnitShatter
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class UnitShatterDebris : MonoBehaviour
{
    [SerializeField]
    private ParticleSystem particleSystem;

    private void Awake()
    {
        if (particleSystem == null)
            particleSystem = GetComponent<ParticleSystem>();
    }

    /// <summary>按双色随机混合播一次爆发。</summary>
    public void Play(Color unitColor, Color ballColor)
    {
        if (particleSystem == null)
            particleSystem = GetComponent<ParticleSystem>();
        if (particleSystem == null) return;

        unitColor.a = 1f;
        ballColor.a = 1f;

        particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = particleSystem.main;
        main.startColor = new ParticleSystem.MinMaxGradient(unitColor, ballColor);

        particleSystem.Play(true);
    }
}

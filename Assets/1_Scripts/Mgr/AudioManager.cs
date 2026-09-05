using UnityEngine;

/// <summary>
/// 音频播放：Addressables 加载 clip，订阅 BallEvents / GameEvents。
/// 由 <see cref="GameLogicManager"/> 创建并持有，OnDestroy 时 Dispose。
/// </summary>
public class AudioManager : System.IDisposable
{
    private readonly AudioCatalog catalog;
    private readonly AudioSource sfxSource;
    private readonly AudioSource bgmSource;

    private AudioClip fireClip;
    private AudioClip hitClip;
    private AudioClip bgmClip;
    private bool eventsRegistered;

    public AudioManager(Transform host)
    {
        catalog = AssetLoader.Load<AudioCatalog>("AudioCatalog");

        GameObject root = new GameObject("AudioSources");
        root.transform.SetParent(host, false);

        sfxSource = root.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;
        sfxSource.volume = 1f;

        bgmSource = root.AddComponent<AudioSource>();
        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
        bgmSource.spatialBlend = 0f;
        bgmSource.volume = 0.85f;

        LoadClips();
        RegisterEvents();
    }

    public void Dispose()
    {
        UnregisterEvents();
        StopBgm();
    }

    private void LoadClips()
    {
        if (catalog == null) return;

        if (!string.IsNullOrEmpty(catalog.FireAddress))
            fireClip = AssetLoader.Load<AudioClip>(catalog.FireAddress);
        if (!string.IsNullOrEmpty(catalog.HitAddress))
            hitClip = AssetLoader.Load<AudioClip>(catalog.HitAddress);
        if (!string.IsNullOrEmpty(catalog.BgmAddress))
            bgmClip = AssetLoader.Load<AudioClip>(catalog.BgmAddress);
    }

    private void RegisterEvents()
    {
        if (eventsRegistered) return;
        BallEvents.OnFired += HandleFired;
        BallEvents.OnHitUnit += HandleHitUnit;
        GameEvents.OnGameStart += HandleGameStart;
        GameEvents.OnGameEnd += HandleGameEnd;
        GameEvents.OnReturnToHome += HandleGameEnd;
        eventsRegistered = true;
    }

    private void UnregisterEvents()
    {
        if (!eventsRegistered) return;
        BallEvents.OnFired -= HandleFired;
        BallEvents.OnHitUnit -= HandleHitUnit;
        GameEvents.OnGameStart -= HandleGameStart;
        GameEvents.OnGameEnd -= HandleGameEnd;
        GameEvents.OnReturnToHome -= HandleGameEnd;
        eventsRegistered = false;
    }

    private void HandleFired(BallFiredContext _)
    {
        // TODO: 发射 SFX，待自行选曲后再开。
        // PlaySfx(fireClip, 0.55f);
    }

    private void HandleHitUnit(BallHitContext _)
    {
        // TODO: 受击 SFX，待自行选曲后再开。
        // PlaySfx(hitClip, 0.75f);
    }

    private void HandleGameStart()
    {
        PlayBgm();
    }

    private void HandleGameEnd()
    {
        StopBgm();
    }

    private void PlaySfx(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    private void PlayBgm()
    {
        if (bgmClip == null || bgmSource == null) return;
        if (bgmSource.isPlaying && bgmSource.clip == bgmClip) return;
        bgmSource.clip = bgmClip;
        bgmSource.Play();
    }

    private void StopBgm()
    {
        if (bgmSource == null) return;
        bgmSource.Stop();
        bgmSource.clip = null;
    }
}

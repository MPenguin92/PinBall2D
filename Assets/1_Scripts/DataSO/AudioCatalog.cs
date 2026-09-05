using UnityEngine;

/// <summary>
/// 音效 / BGM 的 Addressables 短地址目录（Audio 组，如 Audio/sfx_fire）。
/// </summary>
[CreateAssetMenu(fileName = "AudioCatalog", menuName = "PinBall2D/Data/AudioCatalog", order = 7)]
public class AudioCatalog : ScriptableObject
{
    [SerializeField]
    [Tooltip("发射弹珠 SFX 地址")]
    private string fireAddress = "Audio/sfx_fire";

    [SerializeField]
    [Tooltip("Unit 受击 SFX 地址")]
    private string hitAddress = "Audio/sfx_hit";

    [SerializeField]
    [Tooltip("局内 BGM 地址")]
    private string bgmAddress = "Audio/bgm_main";

    public string FireAddress => fireAddress;
    public string HitAddress => hitAddress;
    public string BgmAddress => bgmAddress;
}

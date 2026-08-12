using UnityEngine;

/// <summary>
/// 各 BallType 对应的 VFX Addressables 地址（VFX 组内短地址，如 VFX/HitFire）。
/// 数组下标与 <see cref="BallType"/> 枚举一致；数值以 <c>VfxCatalog.asset</c> 为准。
/// </summary>
[CreateAssetMenu(fileName = "VfxCatalog", menuName = "PinBall2D/Data/VfxCatalog", order = 6)]
public class VfxCatalog : ScriptableObject
{
    [SerializeField]
    private string[] hitAddresses = new string[]
    {
        "VFX/HitBase",
    };

    [SerializeField]
    private string[] killAddresses = new string[]
    {
        "VFX/KillBase",
    };

    public string GetHitAddress(BallType type)
    {
        return GetAddress(hitAddresses, type);
    }

    public string GetKillAddress(BallType type)
    {
        return GetAddress(killAddresses, type);
    }

    private static string GetAddress(string[] addresses, BallType type)
    {
        int index = (int)type;
        if (addresses == null || index < 0 || index >= addresses.Length)
            return null;

        string address = addresses[index];
        return string.IsNullOrEmpty(address) ? null : address;
    }
}
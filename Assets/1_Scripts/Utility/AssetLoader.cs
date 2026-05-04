using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// 项目统一的资源加载入口。
/// 通过 Addressables 同步加载资源，调用方不需要直接依赖 Addressables API。
/// 调用方统一传入 Addressables 短地址，例如："DifficultyTable"。
/// </summary>
public static class AssetLoader
{

    /// <summary>
    /// 同步加载一个资源。
    /// </summary>
    /// <typeparam name="T">Unity 资源类型（ScriptableObject / Prefab / Texture 等）。</typeparam>
    /// <param name="address">Addressables 地址。</param>
    public static T Load<T>(string address) where T : Object
    {
        if (string.IsNullOrEmpty(address))
        {
            Debug.LogError("[AssetLoader] Load address is null or empty.");
            return null;
        }

        T asset = Addressables.LoadAssetAsync<T>(address).WaitForCompletion();
        if (asset == null)
            Debug.LogError($"[AssetLoader] Failed to load asset: {address} (type={typeof(T).Name})");
        return asset;
    }
}

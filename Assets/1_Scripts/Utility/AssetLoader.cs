using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 项目统一的资源加载入口。
/// 当前仅支持 Editor 下通过 AssetDatabase 同步加载，将来可在同一入口内接入 Addressables
/// （AsyncOperationHandle / LoadAssetAsync）而不需要修改业务侧调用。
/// 调用方统一传入相对 "Assets/" 的路径，例如："8_Data/DifficultyTable.asset"。
/// </summary>
public static class AssetLoader
{
    private const string AssetRoot = "Assets/";

    /// <summary>
    /// 同步加载一个资源。Editor 下走 AssetDatabase；运行时尚未接入 Addressables，
    /// 先行返回 null 并打印错误，提示集成方替换实现。
    /// </summary>
    /// <typeparam name="T">Unity 资源类型（ScriptableObject / Prefab / Texture 等）。</typeparam>
    /// <param name="relativePath">相对 Assets/ 的路径，含扩展名。</param>
    public static T Load<T>(string relativePath) where T : Object
    {
        if (string.IsNullOrEmpty(relativePath))
        {
            Debug.LogError("[AssetLoader] Load path is null or empty.");
            return null;
        }

#if UNITY_EDITOR
        string fullPath = relativePath.StartsWith(AssetRoot) ? relativePath : AssetRoot + relativePath;
        T asset = AssetDatabase.LoadAssetAtPath<T>(fullPath);
        if (asset == null)
            Debug.LogError($"[AssetLoader] Failed to load asset: {fullPath} (type={typeof(T).Name})");
        return asset;
#else
        Debug.LogError($"[AssetLoader] Runtime loading not integrated. Path={relativePath}. " +
                       "Please integrate Addressables here.");
        return null;
#endif
    }
}

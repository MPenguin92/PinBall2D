#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

/// <summary>
/// 按文件夹规则把资源同步进 Addressables 分组，并使用简短地址。
/// 菜单：Tools/Addressables/Sync Groups From Folders
///
/// 规则（与当前工程约定一致）：
/// - Assets/2_Prefab/*.prefab（不含子目录）→ Unit，地址 = 文件名
/// - Assets/2_Prefab/UI/**/*.prefab → UI，地址 = 文件名
/// - Assets/2_Prefab/VFX/**/*.prefab → VFX，地址 = VFX/文件名（兼容 VfxCatalog）
/// - Assets/4_Audio/** → Audio，地址 = Audio/文件名（无扩展名）
/// - Assets/7_Res/balls/** → Balls，地址 = Balls/文件名（无扩展名，Sprite）
/// - Assets/8_Data/**/*.asset → Data，地址 = 文件名
/// </summary>
public static class AddressableFolderSync
{
    private readonly struct FolderRule
    {
        public readonly string Folder;
        public readonly string GroupName;
        public readonly string SearchFilter;
        public readonly bool Recursive;
        /// <summary>地址前缀，如 "VFX/"；空则仅用文件名。</summary>
        public readonly string AddressPrefix;

        public FolderRule(string folder, string groupName, string searchFilter, bool recursive, string addressPrefix = "")
        {
            Folder = folder;
            GroupName = groupName;
            SearchFilter = searchFilter;
            Recursive = recursive;
            AddressPrefix = addressPrefix ?? "";
        }
    }

    private static readonly FolderRule[] Rules =
    {
        new FolderRule("Assets/2_Prefab", "Unit", "t:Prefab", recursive: false),
        new FolderRule("Assets/2_Prefab/UI", "UI", "t:Prefab", recursive: true),
        new FolderRule("Assets/2_Prefab/VFX", "VFX", "t:Prefab", recursive: true, addressPrefix: "VFX/"),
        new FolderRule("Assets/4_Audio", "Audio", "t:AudioClip", recursive: true, addressPrefix: "Audio/"),
        new FolderRule("Assets/7_Res/balls", "Balls", "t:Texture2D", recursive: true, addressPrefix: "Balls/"),
        new FolderRule("Assets/8_Data", "Data", "t:ScriptableObject", recursive: true),
    };

    [MenuItem("Tools/Addressables/Sync Groups From Folders")]
    public static void Sync()
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("[AddressableFolderSync] AddressableAssetSettings not found. Open Window/Asset Management/Addressables/Groups once to create them.");
            return;
        }

        int added = 0;
        int updated = 0;
        int skipped = 0;
        var log = new StringBuilder();

        try
        {
            AssetDatabase.StartAssetEditing();

            for (int i = 0; i < Rules.Length; i++)
            {
                FolderRule rule = Rules[i];
                if (!AssetDatabase.IsValidFolder(rule.Folder))
                {
                    log.AppendLine($"- skip missing folder: {rule.Folder} → {rule.GroupName}");
                    continue;
                }

                AddressableAssetGroup group = settings.FindGroup(rule.GroupName);
                if (group == null)
                {
                    group = settings.CreateGroup(
                        rule.GroupName,
                        setAsDefaultGroup: false,
                        readOnly: false,
                        postEvent: true,
                        schemasToCopy: null,
                        typeof(UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema),
                        typeof(UnityEditor.AddressableAssets.Settings.GroupSchemas.ContentUpdateGroupSchema));
                    log.AppendLine($"- created group: {rule.GroupName}");
                }

                if (group == null)
                {
                    Debug.LogError($"[AddressableFolderSync] Failed to create group: '{rule.GroupName}'.");
                    continue;
                }

                string[] guids = AssetDatabase.FindAssets(rule.SearchFilter, new[] { rule.Folder });
                var seen = new HashSet<string>();

                for (int g = 0; g < guids.Length; g++)
                {
                    string guid = guids[g];
                    if (!seen.Add(guid))
                        continue;

                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (string.IsNullOrEmpty(path) || path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                        continue;

                    // 非递归规则：只收该文件夹直属资源（排除子目录，如 2_Prefab/UI）。
                    if (!rule.Recursive && !IsDirectChildOfFolder(path, rule.Folder))
                        continue;

                    string address = BuildAddress(path, rule.AddressPrefix);
                    AddressableAssetEntry entry = settings.FindAssetEntry(guid);
                    bool isNew = entry == null;
                    bool moved = entry != null && entry.parentGroup != group;
                    bool renamed = entry != null && entry.address != address;

                    if (isNew || moved)
                    {
                        entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);
                        added++;
                    }

                    if (entry == null)
                    {
                        skipped++;
                        continue;
                    }

                    if (entry.address != address)
                    {
                        entry.SetAddress(address, postEvent: false);
                        if (!isNew)
                            updated++;
                    }
                    else if (!isNew && !moved && !renamed)
                    {
                        skipped++;
                    }

                    log.AppendLine($"- {rule.GroupName}: {path} → '{address}'");
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true, true);
        AssetDatabase.SaveAssets();

        Debug.Log(
            $"[AddressableFolderSync] Done. added/moved={added}, addressUpdated={updated}, unchanged={skipped}\n{log}");
    }

    private static string BuildAddress(string assetPath, string prefix)
    {
        string fileName = Path.GetFileNameWithoutExtension(assetPath);
        return string.IsNullOrEmpty(prefix) ? fileName : prefix + fileName;
    }

    private static bool IsDirectChildOfFolder(string assetPath, string folder)
    {
        string normalizedFolder = folder.Replace('\\', '/').TrimEnd('/');
        string normalizedPath = assetPath.Replace('\\', '/');
        if (!normalizedPath.StartsWith(normalizedFolder + "/", StringComparison.OrdinalIgnoreCase))
            return false;

        string relative = normalizedPath.Substring(normalizedFolder.Length + 1);
        return relative.IndexOf('/') < 0;
    }
}
#endif

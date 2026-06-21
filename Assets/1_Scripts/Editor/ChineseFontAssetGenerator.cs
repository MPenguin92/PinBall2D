#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// 从 <c>Assets/7_Res/Fonts/NotoSansSC-Regular.ttf</c> 生成 TMP 动态 SDF 字体资源，
/// 并设为 TMP 默认字体、LiberationSans 作为西文回退。
/// </summary>
public static class ChineseFontAssetGenerator
{
    private const string SourceFontPath = "Assets/7_Res/Fonts/NotoSansSC-Regular.ttf";
    private const string OutputFontPath = "Assets/7_Res/Fonts/NotoSansSC-Regular SDF.asset";
    private const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
    private const string LiberationSansPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    private const int SamplingPointSize = 48;
    private const int AtlasPadding = 5;
    private const int AtlasSize = 2048;

    [MenuItem("Tools/Text/Generate Chinese Dynamic SDF Font")]
    public static void Generate()
    {
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        if (sourceFont == null)
        {
            Debug.LogError($"[ChineseFont] Source font not found: {SourceFontPath}");
            return;
        }

        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutputFontPath);
        if (existing != null)
            AssetDatabase.DeleteAsset(OutputFontPath);

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            SamplingPointSize,
            AtlasPadding,
            GlyphRenderMode.SDFAA,
            AtlasSize,
            AtlasSize,
            AtlasPopulationMode.Dynamic,
            true);

        if (fontAsset == null)
        {
            Debug.LogError("[ChineseFont] TMP_FontAsset.CreateFontAsset returned null.");
            return;
        }

        fontAsset.name = "NotoSansSC-Regular SDF";

        if (!fontAsset.TryAddCharacters(BuildInitialCharacterSet(), true))
            Debug.LogWarning("[ChineseFont] Some initial characters could not be added. Check source TTF coverage.");

        BindAtlasToMaterial(fontAsset);

        TMP_FontAsset liberation = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LiberationSansPath);
        if (liberation != null)
        {
            if (fontAsset.fallbackFontAssetTable == null)
                fontAsset.fallbackFontAssetTable = new List<TMP_FontAsset>();

            if (!fontAsset.fallbackFontAssetTable.Contains(liberation))
                fontAsset.fallbackFontAssetTable.Add(liberation);
        }

        AssetDatabase.CreateAsset(fontAsset, OutputFontPath);
        SaveFontSubAssets(fontAsset);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        ApplyAsDefaultFont(fontAsset);
        ApplyFontToUiPrefabs(fontAsset);
        Debug.Log($"[ChineseFont] Dynamic SDF font created: {OutputFontPath}");
    }

    [MenuItem("Tools/Text/Apply Chinese Font To UI Prefabs")]
    public static void ApplyToUiPrefabsOnly()
    {
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutputFontPath);
        if (fontAsset == null)
        {
            Debug.LogError($"[ChineseFont] Font asset not found. Run Tools/Text/Generate Chinese Dynamic SDF Font first.");
            return;
        }

        ApplyFontToUiPrefabs(fontAsset);
    }

    private static string BuildInitialCharacterSet()
    {
        // ASCII 可打印字符 + 当前 UI 会出现的汉字/符号
        const string ascii = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";
        const string uiChinese = "选择升级按或点击卡片经验连射间隔弹珠伤害速度穿透反弹侧击前后普通火焰冰霜雷电毒素重型回旋";
        return ascii + uiChinese;
    }

    private static void BindAtlasToMaterial(TMP_FontAsset fontAsset)
    {
        if (fontAsset == null || fontAsset.material == null) return;

        Texture2D atlas = fontAsset.atlasTexture;
        if (atlas != null)
            fontAsset.material.SetTexture(ShaderUtilities.ID_MainTex, atlas);
    }

    private static void SaveFontSubAssets(TMP_FontAsset fontAsset)
    {
        Material material = fontAsset.material;
        if (material != null)
        {
            material.name = fontAsset.name + " Material";
            if (AssetDatabase.GetAssetPath(material) != AssetDatabase.GetAssetPath(fontAsset))
                AssetDatabase.AddObjectToAsset(material, fontAsset);
        }

        Texture2D[] atlases = fontAsset.atlasTextures;
        if (atlases == null) return;

        for (int i = 0; i < atlases.Length; i++)
        {
            Texture2D atlas = atlases[i];
            if (atlas == null) continue;

            atlas.name = atlases.Length == 1 ? fontAsset.name + " Atlas" : $"{fontAsset.name} Atlas {i}";
            if (AssetDatabase.GetAssetPath(atlas) != AssetDatabase.GetAssetPath(fontAsset))
                AssetDatabase.AddObjectToAsset(atlas, fontAsset);
        }

        EditorUtility.SetDirty(fontAsset);
    }

    private static readonly string[] UiPrefabPaths =
    {
        "Assets/2_Prefab/UI/GameHUD.prefab",
        "Assets/2_Prefab/UI/GameOverScreen.prefab",
        "Assets/2_Prefab/UI/StartScreen.prefab",
        "Assets/2_Prefab/UI/UpgradeSelectionUI.prefab",
    };

    private static void ApplyFontToUiPrefabs(TMP_FontAsset fontAsset)
    {
        foreach (string path in UiPrefabPaths)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null)
            {
                Debug.LogWarning($"[ChineseFont] Prefab not found: {path}");
                continue;
            }

            bool changed = false;
            TextMeshProUGUI[] texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (TextMeshProUGUI text in texts)
            {
                text.font = fontAsset;
                // 使用字体资源自带材质（已绑定 Atlas），避免引用到空 _MainTex 的材质变体。
                if (fontAsset.material != null)
                    text.fontSharedMaterial = fontAsset.material;
                changed = true;
            }

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log($"[ChineseFont] Updated TMP font in {path}");
            }

            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ApplyAsDefaultFont(TMP_FontAsset chineseFont)
    {
        TMP_Settings settingsAsset = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
        if (settingsAsset == null)
        {
            Debug.LogWarning($"[ChineseFont] TMP Settings not found: {TmpSettingsPath}");
            return;
        }

        TMP_FontAsset liberation = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LiberationSansPath);

        SerializedObject serializedSettings = new SerializedObject(settingsAsset);
        serializedSettings.FindProperty("m_defaultFontAsset").objectReferenceValue = chineseFont;

        if (liberation != null && liberation != chineseFont)
        {
            SerializedProperty fallbacks = serializedSettings.FindProperty("m_fallbackFontAssets");
            bool alreadyAdded = false;
            for (int i = 0; i < fallbacks.arraySize; i++)
            {
                if (fallbacks.GetArrayElementAtIndex(i).objectReferenceValue == liberation)
                {
                    alreadyAdded = true;
                    break;
                }
            }

            if (!alreadyAdded)
            {
                fallbacks.InsertArrayElementAtIndex(0);
                fallbacks.GetArrayElementAtIndex(0).objectReferenceValue = liberation;
            }
        }

        serializedSettings.ApplyModifiedProperties();
        EditorUtility.SetDirty(settingsAsset);
        AssetDatabase.SaveAssets();
        Debug.Log("[ChineseFont] TMP Settings default font updated to NotoSansSC-Regular SDF.");
    }
}
#endif

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 将 9_Excel 下的 CSV 配置表导入为 8_Data 下的 ScriptableObject Asset。
/// 菜单：Tools/Data/Import All | Tools/Data/Import Difficulty | Tools/Data/Import Upgrades。
/// 当前仅支持 CSV（UTF-8，英文逗号分隔，首行为表头）；将来接入 EPPlus/NPOI 读 xlsx 时在此处替换。
/// </summary>
public static class DataImporter
{
    private const string ExcelFolder = "Assets/9_Excel";
    private const string DataFolder = "Assets/8_Data";
    private const string UpgradesSubFolder = "Upgrades";

    [MenuItem("Tools/Data/Import All")]
    public static void ImportAll()
    {
        EnsureDataFolder();
        ImportDifficulty();
        ImportKillMilestones();
        ImportBallStatDefaults();
        ImportUpgrades();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[DataImporter] Import All finished.");
    }

    [MenuItem("Tools/Data/Import Difficulty")]
    public static void ImportDifficulty()
    {
        string csvPath = ExcelFolder + "/Difficulty.csv";
        if (!File.Exists(csvPath))
        {
            Debug.LogError($"[DataImporter] CSV not found: {csvPath}");
            return;
        }

        List<DifficultyStageData> stages = new List<DifficultyStageData>();
        string[] lines = File.ReadAllLines(csvPath);
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] tokens = line.Split(',');
            if (tokens.Length < 7)
            {
                Debug.LogError($"[DataImporter] Difficulty line {i + 1} has too few columns (expected 7: startTime,spawnMin,spawnMax,unitHp,unitAttack,stepInterval,unitExperience), skipped: {line}");
                continue;
            }

            stages.Add(new DifficultyStageData
            {
                startTime = ParseFloat(tokens[0]),
                spawnMin = ParseInt(tokens[1]),
                spawnMax = ParseInt(tokens[2]),
                unitHp = ParseInt(tokens[3]),
                unitAttack = ParseInt(tokens[4]),
                stepInterval = ParseFloat(tokens[5]),
                unitExperience = ParseInt(tokens[6]),
            });
        }

        EnsureDataFolder();
        string assetPath = DataFolder + "/DifficultyTable.asset";
        DifficultyTable table = AssetDatabase.LoadAssetAtPath<DifficultyTable>(assetPath);
        if (table == null)
        {
            table = ScriptableObject.CreateInstance<DifficultyTable>();
            AssetDatabase.CreateAsset(table, assetPath);
        }

        table.SetStages(stages);
        EditorUtility.SetDirty(table);
        AssetDatabase.SaveAssets();

        Debug.Log($"[DataImporter] Difficulty imported: {stages.Count} stages -> {assetPath}");
    }

    [MenuItem("Tools/Data/Import Kill Milestones")]
    public static void ImportKillMilestones()
    {
        string csvPath = ExcelFolder + "/KillMilestones.csv";
        if (!File.Exists(csvPath))
        {
            Debug.LogError($"[DataImporter] CSV not found: {csvPath}");
            return;
        }

        List<KillMilestoneData> milestones = new List<KillMilestoneData>();
        string[] lines = File.ReadAllLines(csvPath);
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] tokens = line.Split(',');
            if (tokens.Length < 5)
            {
                Debug.LogWarning($"[DataImporter] Milestone line {i + 1} has too few columns, skipped: {line}");
                continue;
            }

            milestones.Add(new KillMilestoneData
            {
                experienceThreshold = ParseInt(tokens[0]),
                weightCommon = ParseInt(tokens[1]),
                weightUncommon = ParseInt(tokens[2]),
                weightRare = ParseInt(tokens[3]),
                weightLegendary = ParseInt(tokens[4]),
            });
        }

        EnsureDataFolder();
        string assetPath = DataFolder + "/KillMilestoneTable.asset";
        KillMilestoneTable table = AssetDatabase.LoadAssetAtPath<KillMilestoneTable>(assetPath);
        if (table == null)
        {
            table = ScriptableObject.CreateInstance<KillMilestoneTable>();
            AssetDatabase.CreateAsset(table, assetPath);
        }

        table.SetMilestones(milestones);
        EditorUtility.SetDirty(table);
        AssetDatabase.SaveAssets();

        Debug.Log($"[DataImporter] KillMilestones imported: {milestones.Count} rows -> {assetPath}");
    }

    [MenuItem("Tools/Data/Import Ball Stat Defaults")]
    public static void ImportBallStatDefaults()
    {
        string csvPath = ExcelFolder + "/BallStatDefaults.csv";
        if (!File.Exists(csvPath))
        {
            Debug.LogError($"[DataImporter] CSV not found: {csvPath}");
            return;
        }

        List<BallStatDefault> defaults = new List<BallStatDefault>();
        string[] lines = File.ReadAllLines(csvPath);
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] tokens = line.Split(',');
            if (tokens.Length < 2)
            {
                Debug.LogWarning($"[DataImporter] BallStatDefaults line {i + 1} has too few columns, skipped: {line}");
                continue;
            }

            string statRaw = tokens[0].Trim();
            if (string.IsNullOrEmpty(statRaw)) continue;

            if (!Enum.TryParse<BallStatType>(statRaw, true, out BallStatType type))
            {
                Debug.LogWarning($"[DataImporter] BallStatDefaults line {i + 1}: unknown BallStatType '{statRaw}', skipped.");
                continue;
            }

            defaults.Add(new BallStatDefault
            {
                statType = type,
                baseValue = ParseFloat(tokens[1]),
            });
        }

        EnsureDataFolder();
        string assetPath = DataFolder + "/BallStatDefaultsTable.asset";
        BallStatDefaultsTable table = AssetDatabase.LoadAssetAtPath<BallStatDefaultsTable>(assetPath);
        if (table == null)
        {
            table = ScriptableObject.CreateInstance<BallStatDefaultsTable>();
            AssetDatabase.CreateAsset(table, assetPath);
        }

        table.SetDefaults(defaults);
        EditorUtility.SetDirty(table);
        AssetDatabase.SaveAssets();

        Debug.Log($"[DataImporter] BallStatDefaults imported: {defaults.Count} entries -> {assetPath}");
    }

    [MenuItem("Tools/Data/Import Upgrades")]
    public static void ImportUpgrades()
    {
        EnsureDataFolder();
        EnsureUpgradeFolder();

        List<UpgradeBase> entries = new List<UpgradeBase>();

        ImportBallStatUpgrades(entries);

        // 写入 / 更新 UpgradeCatalog
        string catalogPath = DataFolder + "/UpgradeCatalog.asset";
        UpgradeCatalog catalog = AssetDatabase.LoadAssetAtPath<UpgradeCatalog>(catalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<UpgradeCatalog>();
            AssetDatabase.CreateAsset(catalog, catalogPath);
        }
        catalog.SetEntries(entries);
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();

        Debug.Log($"[DataImporter] Upgrades imported: {entries.Count} entries -> {catalogPath}");
    }

    private static void ImportBallStatUpgrades(List<UpgradeBase> entries)
    {
        string csvPath = ExcelFolder + "/Upgrades_Stat.csv";
        if (!File.Exists(csvPath))
        {
            Debug.LogWarning($"[DataImporter] Upgrades_Stat.csv not found: {csvPath}");
            return;
        }

        string[] lines = File.ReadAllLines(csvPath);
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] t = line.Split(',');
            if (t.Length < 14)
            {
                Debug.LogWarning($"[DataImporter] Stat upgrade line {i + 1} has too few columns, skipped: {line}");
                continue;
            }

            string id = t[0].Trim();
            if (string.IsNullOrEmpty(id)) continue;

            string assetPath = $"{DataFolder}/{UpgradesSubFolder}/Stat_{id}.asset";
            BallStatUpgradeData asset = AssetDatabase.LoadAssetAtPath<BallStatUpgradeData>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<BallStatUpgradeData>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            asset.SetMeta(
                id,
                t[1].Trim(),
                t[2].Trim(),
                ParseRarity(t[3]),
                ParseInt(t[4])
            );

            List<BallStatModifier> mods = new List<BallStatModifier>();
            TryAddModifier(mods, t[5], t[6], t[7]);
            TryAddModifier(mods, t[8], t[9], t[10]);
            TryAddModifier(mods, t[11], t[12], t[13]);
            asset.SetModifiers(mods);

            EditorUtility.SetDirty(asset);
            entries.Add(asset);
        }
    }

    private static void TryAddModifier(List<BallStatModifier> mods, string statRaw, string flatRaw, string pctRaw)
    {
        string s = statRaw == null ? string.Empty : statRaw.Trim();
        if (string.IsNullOrEmpty(s)) return;

        if (!Enum.TryParse<BallStatType>(s, true, out BallStatType type))
        {
            Debug.LogWarning($"[DataImporter] Unknown BallStatType '{s}', skipped.");
            return;
        }

        mods.Add(new BallStatModifier
        {
            statType = type,
            flat = ParseFloat(flatRaw),
            percent = ParseFloat(pctRaw),
        });
    }

    private static UpgradeRarity ParseRarity(string s)
    {
        if (Enum.TryParse<UpgradeRarity>(s.Trim(), true, out UpgradeRarity r))
            return r;
        return UpgradeRarity.Common;
    }

    private static void EnsureDataFolder()
    {
        if (!AssetDatabase.IsValidFolder(DataFolder))
        {
            AssetDatabase.CreateFolder("Assets", "8_Data");
        }
    }

    private static void EnsureUpgradeFolder()
    {
        string folder = DataFolder + "/" + UpgradesSubFolder;
        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder(DataFolder, UpgradesSubFolder);
        }
    }

    private static int ParseInt(string s)
    {
        int.TryParse((s ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v);
        return v;
    }

    private static float ParseFloat(string s)
    {
        float.TryParse((s ?? string.Empty).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float v);
        return v;
    }
}
#endif

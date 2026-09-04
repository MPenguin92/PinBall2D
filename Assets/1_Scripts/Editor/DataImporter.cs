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

        // 词条通用元信息表：Upgrades.csv = id,name,desc,rarity,maxLevel
        // （展示/通用字段集中于此，后续加 icon 等也在这一张表加列）。
        Dictionary<string, UpgradeMeta> meta = ReadUpgradeMeta();

        // 类型专有参数按表分发：每个专有表一行 = 某个词条的某一级。
        ImportFireUpgrades(meta, entries);

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

    private struct UpgradeMeta
    {
        public string name;
        public string desc;
        public UpgradeRarity rarity;
        public int maxLevel;
    }

    /// <summary>读取通用元信息表；返回 id → 元信息（不含 id 无行）。</summary>
    private static Dictionary<string, UpgradeMeta> ReadUpgradeMeta()
    {
        Dictionary<string, UpgradeMeta> result = new Dictionary<string, UpgradeMeta>();
        string csvPath = ExcelFolder + "/Upgrades.csv";
        if (!File.Exists(csvPath))
        {
            Debug.LogWarning($"[DataImporter] Upgrades.csv not found: {csvPath}");
            return result;
        }

        string[] lines = File.ReadAllLines(csvPath);
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] t = line.Split(',');
            if (t.Length < 5)
            {
                Debug.LogWarning($"[DataImporter] Upgrades.csv line {i + 1} has too few columns, skipped: {line}");
                continue;
            }

            string id = t[0].Trim();
            if (string.IsNullOrEmpty(id)) continue;

            result[id] = new UpgradeMeta
            {
                name = t[1].Trim(),
                desc = t[2].Trim(),
                rarity = ParseRarity(t[3]),
                maxLevel = ParseInt(t[4]),
            };
        }
        return result;
    }

    /// <summary>
    /// 导入射击类行为词条（当前：连发 Burst）：
    /// Upgrades_Fire.csv = id, level, desc, shots, interval —— 一行 = 一个词条的某一级。
    /// desc 为等级化描述（选卡卡面展示）；shots 为该级每次发射球数（直接取值）。
    /// 生成 asset 前缀 Fire_；满级取自通用表的 maxLevel，某级未配置时沿用上一级。
    /// </summary>
    private static void ImportFireUpgrades(Dictionary<string, UpgradeMeta> meta, List<UpgradeBase> entries)
    {
        string csvPath = ExcelFolder + "/Upgrades_Fire.csv";
        if (!File.Exists(csvPath))
        {
            Debug.LogWarning($"[DataImporter] Upgrades_Fire.csv not found: {csvPath}");
            return;
        }

        // 第一步：按词条 id 收集 (level -> 等级数据)。
        Dictionary<string, Dictionary<int, FireLevelData>> raw = new Dictionary<string, Dictionary<int, FireLevelData>>();
        string[] lines = File.ReadAllLines(csvPath);
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] t = line.Split(',');
            if (t.Length < 5)
            {
                Debug.LogWarning($"[DataImporter] Upgrades_Fire.csv line {i + 1} has too few columns, skipped: {line}");
                continue;
            }

            string id = t[0].Trim();
            int level = ParseInt(t[1]);
            if (string.IsNullOrEmpty(id) || level < 1) continue;

            if (!raw.TryGetValue(id, out Dictionary<int, FireLevelData> levels))
            {
                levels = new Dictionary<int, FireLevelData>();
                raw[id] = levels;
            }
            levels[level] = new FireLevelData
            {
                desc = t[2].Trim(),
                shots = Mathf.Max(1, ParseInt(t[3])),
                interval = Mathf.Max(0f, ParseFloat(t[4])),
            };
        }

        // 第二步：逐词条生成 / 更新 asset（通用元信息 + 逐级专有数据）。
        foreach (KeyValuePair<string, Dictionary<int, FireLevelData>> pair in raw)
        {
            string id = pair.Key;
            if (!meta.TryGetValue(id, out UpgradeMeta m))
            {
                Debug.LogWarning($"[DataImporter] Upgrades_Fire.csv id '{id}' missing in Upgrades.csv, skipped.");
                continue;
            }

            // 补全到 maxLevel 长度；缺失等级沿用上一级（逐级拷贝，避免列表内共享同一实例）。
            List<FireLevelData> levels = new List<FireLevelData>();
            FireLevelData last = null;
            for (int lv = 1; lv <= Mathf.Max(1, m.maxLevel); lv++)
            {
                if (pair.Value.TryGetValue(lv, out FireLevelData data))
                    last = data;

                levels.Add(new FireLevelData
                {
                    desc = last != null ? last.desc : string.Empty,
                    shots = last != null ? last.shots : 1,
                    interval = last != null ? last.interval : 0.08f,
                });
            }

            string assetPath = $"{DataFolder}/{UpgradesSubFolder}/Fire_{id}.asset";
            FireBurstUpgradeData asset = AssetDatabase.LoadAssetAtPath<FireBurstUpgradeData>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<FireBurstUpgradeData>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            asset.SetMeta(id, m.name, m.desc, m.rarity, m.maxLevel);
            asset.SetLevels(levels);

            EditorUtility.SetDirty(asset);
            entries.Add(asset);
        }
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

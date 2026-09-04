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
        ImportUnits();
        ImportBalls();
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
            if (tokens.Length < 5)
            {
                Debug.LogError($"[DataImporter] Difficulty line {i + 1} has too few columns (expected 5: startTime,spawnFillMin,spawnFillMax,stepInterval,spawnLevels), skipped: {line}");
                continue;
            }

            stages.Add(new DifficultyStageData
            {
                startTime = ParseFloat(tokens[0]),
                spawnFillMin = ParseInt(tokens[1]),
                spawnFillMax = ParseInt(tokens[2]),
                stepInterval = ParseFloat(tokens[3]),
                spawnLevels = ParseSpawnLevels(tokens[4]),
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

    [MenuItem("Tools/Data/Import Units")]
    public static void ImportUnits()
    {
        // Units.csv：id, name, prefab（每类一行）—— 定义与 prefab 地址。
        // Units_Level.csv：id, level, hp, attack, experience（每级一行）—— 逐级数值。
        string metaCsv = ExcelFolder + "/Units.csv";
        string levelCsv = ExcelFolder + "/Units_Level.csv";
        if (!File.Exists(metaCsv) || !File.Exists(levelCsv))
        {
            Debug.LogError($"[DataImporter] Units.csv / Units_Level.csv not found (need both).");
            return;
        }

        // 第一步：读定义。
        Dictionary<string, UnitDefinition> defs = new Dictionary<string, UnitDefinition>();
        string[] metaLines = File.ReadAllLines(metaCsv);
        for (int i = 1; i < metaLines.Length; i++)
        {
            string line = metaLines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] t = line.Split(',');
            if (t.Length < 3) continue;

            string id = t[0].Trim();
            if (string.IsNullOrEmpty(id)) continue;

            defs[id] = new UnitDefinition
            {
                id = id,
                name = t[1].Trim(),
                prefabAddress = t[2].Trim(),
            };
        }

        // 第二步：按 id 聚合逐级数值（level 升序填充，缺失沿用上一级）。
        string[] levelLines = File.ReadAllLines(levelCsv);
        for (int i = 1; i < levelLines.Length; i++)
        {
            string line = levelLines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] t = line.Split(',');
            if (t.Length < 6)
            {
                Debug.LogWarning($"[DataImporter] Units_Level.csv line {i + 1} has too few columns, skipped: {line}");
                continue;
            }

            string id = t[0].Trim();
            int level = ParseInt(t[1]);
            if (!defs.TryGetValue(id, out UnitDefinition def) || level < 1) continue;

            // 填充到目标等级；缺口沿用上一级。
            while (def.levels.Count < level)
                def.levels.Add(LastOrNew(def));

            def.levels[level - 1] = new UnitLevelData
            {
                hp = Mathf.Max(1, ParseInt(t[2])),
                attack = Mathf.Max(0, ParseInt(t[3])),
                experience = Mathf.Max(1, ParseInt(t[4])),
                gold = Mathf.Max(0, ParseInt(t[5])),
            };
        }

        // 第三步：写入 / 更新 UnitTable.asset。
        EnsureDataFolder();
        string assetPath = DataFolder + "/UnitTable.asset";
        UnitTable table = AssetDatabase.LoadAssetAtPath<UnitTable>(assetPath);
        if (table == null)
        {
            table = ScriptableObject.CreateInstance<UnitTable>();
            AssetDatabase.CreateAsset(table, assetPath);
        }

        List<UnitDefinition> list = new List<UnitDefinition>(defs.Values);
        table.SetUnits(list);
        EditorUtility.SetDirty(table);
        AssetDatabase.SaveAssets();

        Debug.Log($"[DataImporter] Units imported: {list.Count} definitions -> {assetPath}");
    }

    [MenuItem("Tools/Data/Import Balls")]
    public static void ImportBalls()
    {
        // Balls.csv：id, name, prefab（每类一行）—— 定义与出池 prefab 地址。
        // Balls_Level.csv：id, level, damage（每级一行）—— 逐级数值（伤害）。
        string metaCsv = ExcelFolder + "/Balls.csv";
        string levelCsv = ExcelFolder + "/Balls_Level.csv";
        if (!File.Exists(metaCsv) || !File.Exists(levelCsv))
        {
            Debug.LogError($"[DataImporter] Balls.csv / Balls_Level.csv not found (need both).");
            return;
        }

        // 第一步：读定义。
        Dictionary<string, BallDefinition> defs = new Dictionary<string, BallDefinition>();
        string[] metaLines = File.ReadAllLines(metaCsv);
        for (int i = 1; i < metaLines.Length; i++)
        {
            string line = metaLines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] t = line.Split(',');
            if (t.Length < 3) continue;

            string id = t[0].Trim();
            if (string.IsNullOrEmpty(id)) continue;

            defs[id] = new BallDefinition
            {
                id = id,
                name = t[1].Trim(),
                prefabAddress = t[2].Trim(),
            };
        }

        // 第二步：按 id 聚合逐级数值（level 升序填充，缺口沿用上一级）。
        string[] levelLines = File.ReadAllLines(levelCsv);
        for (int i = 1; i < levelLines.Length; i++)
        {
            string line = levelLines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] t = line.Split(',');
            if (t.Length < 3)
            {
                Debug.LogWarning($"[DataImporter] Balls_Level.csv line {i + 1} has too few columns, skipped: {line}");
                continue;
            }

            string id = t[0].Trim();
            int level = ParseInt(t[1]);
            if (!defs.TryGetValue(id, out BallDefinition def) || level < 1) continue;

            while (def.levels.Count < level)
                def.levels.Add(LastBallLevelOrNew(def));

            def.levels[level - 1] = new BallLevelData
            {
                damage = Mathf.Max(0f, ParseFloat(t[2])),
            };
        }

        // 第三步：写入 / 更新 BallTable.asset。
        EnsureDataFolder();
        string assetPath = DataFolder + "/BallTable.asset";
        BallTable table = AssetDatabase.LoadAssetAtPath<BallTable>(assetPath);
        if (table == null)
        {
            table = ScriptableObject.CreateInstance<BallTable>();
            AssetDatabase.CreateAsset(table, assetPath);
        }

        List<BallDefinition> list = new List<BallDefinition>(defs.Values);
        table.SetBalls(list);
        EditorUtility.SetDirty(table);
        AssetDatabase.SaveAssets();

        Debug.Log($"[DataImporter] Balls imported: {list.Count} definitions -> {assetPath}");
    }

    /// <summary>取 Ball 定义现有末级数值，用于补齐缺口；无末级时给默认伤害 1。</summary>
    private static BallLevelData LastBallLevelOrNew(BallDefinition def)
    {
        if (def.levels != null && def.levels.Count > 0)
        {
            BallLevelData last = def.levels[def.levels.Count - 1];
            return new BallLevelData { damage = last.damage };
        }
        return new BallLevelData { damage = 1f };
    }

    /// <summary>取定义现有末级数值，用于补齐缺口；无末级时给默认（1/1/1/1）。</summary>
    private static UnitLevelData LastOrNew(UnitDefinition def)
    {
        if (def.levels != null && def.levels.Count > 0)
        {
            UnitLevelData last = def.levels[def.levels.Count - 1];
            return new UnitLevelData
            {
                hp = last.hp,
                attack = last.attack,
                experience = last.experience,
                gold = last.gold,
            };
        }
        return new UnitLevelData { hp = 1, attack = 1, experience = 1, gold = 1 };
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
    /// Upgrades_Fire.csv = id, level, desc, subCount, interval —— 一行 = 一个词条的某一级；
    /// desc 为等级化描述（选卡卡面展示）；subCount 为该级副弹颗数（主弹 base 固定 1 颗）。
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
                subCount = Mathf.Max(0, ParseInt(t[3])),
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
                    subCount = last != null ? last.subCount : 0,
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

    /// <summary>
    /// 解析 spawnLevels 列：";" 分隔多个「levelxweight」段（如 1x60;2x30;3x10）。
    /// 段格式不合法（缺 x 或权重 &lt;=0）时跳过该段。
    /// </summary>
    private static List<SpawnLevelEntry> ParseSpawnLevels(string raw)
    {
        List<SpawnLevelEntry> list = new List<SpawnLevelEntry>();
        if (string.IsNullOrWhiteSpace(raw)) return list;

        string[] segments = raw.Split(';');
        for (int i = 0; i < segments.Length; i++)
        {
            string seg = segments[i];
            if (string.IsNullOrWhiteSpace(seg)) continue;

            string[] parts = seg.Split('x');
            if (parts.Length < 2) continue;

            int level = ParseInt(parts[0]);
            int weight = ParseInt(parts[1]);
            if (level < 1 || weight <= 0) continue;

            list.Add(new SpawnLevelEntry { level = level, weight = weight });
        }
        return list;
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

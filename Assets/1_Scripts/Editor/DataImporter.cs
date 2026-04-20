#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 将 9_Excel 下的 CSV 配置表导入为 8_Data 下的 ScriptableObject Asset。
/// 菜单：Tools/Data/Import All | Tools/Data/Import Difficulty。
/// 当前仅支持 CSV（UTF-8，英文逗号分隔，首行为表头）；将来接入 EPPlus/NPOI 读 xlsx 时在此处替换。
/// </summary>
public static class DataImporter
{
    private const string ExcelFolder = "Assets/9_Excel";
    private const string DataFolder = "Assets/8_Data";

    [MenuItem("Tools/Data/Import All")]
    public static void ImportAll()
    {
        EnsureDataFolder();
        ImportDifficulty();
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
            if (tokens.Length < 6)
            {
                Debug.LogWarning($"[DataImporter] Difficulty line {i + 1} has too few columns, skipped: {line}");
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

    private static void EnsureDataFolder()
    {
        if (!AssetDatabase.IsValidFolder(DataFolder))
        {
            AssetDatabase.CreateFolder("Assets", "8_Data");
        }
    }

    private static int ParseInt(string s)
    {
        int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v);
        return v;
    }

    private static float ParseFloat(string s)
    {
        float.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float v);
        return v;
    }
}
#endif

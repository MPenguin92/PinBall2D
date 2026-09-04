using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单个 Unit 某一级（level）的数值：Units_Level.csv 一行。
/// </summary>
[System.Serializable]
public class UnitLevelData
{
    public int hp;
    public int attack;
    public int experience;

    /// <summary>击杀该等级 Unit 时给玩家累加的金币（全局经济）。</summary>
    public int gold;
}

/// <summary>
/// 一种 Unit 的定义：Units.csv 一行 + 该 id 的逐级数值（Units_Level.csv）。
/// </summary>
[System.Serializable]
public class UnitDefinition
{
    /// <summary>单位类型 key（Units.csv 第一列；prefab 上 UnitBase.unitId 与此对应）。</summary>
    public string id;

    /// <summary>显示名。</summary>
    public string name;

    /// <summary>生成用的 prefab Addressables 短地址（如 "SimpleUnit"）。</summary>
    public string prefabAddress;

    /// <summary>逐级数值：下标 0 = Lv1；未配置更高等级时取末级。</summary>
    public List<UnitLevelData> levels = new List<UnitLevelData>();
}

/// <summary>
/// 单位定义表 ScriptableObject：由 DataImporter 从 9_Excel/Units.csv + Units_Level.csv 生成。
/// 运行时由 Unit 的 Init 按 (unitId, 当前难度等级) 查询该级的 hp/attack/experience。
/// </summary>
[CreateAssetMenu(fileName = "UnitTable", menuName = "PinBall2D/Data/UnitTable", order = 4)]
public class UnitTable : ScriptableObject
{
    [SerializeField]
    private List<UnitDefinition> units = new List<UnitDefinition>();

    public IReadOnlyList<UnitDefinition> Units => units;

    public int Count => units != null ? units.Count : 0;

    /// <summary>由 Editor 导入工具写入；运行时无需调用。</summary>
    public void SetUnits(List<UnitDefinition> list)
    {
        units = list ?? new List<UnitDefinition>();
    }

    /// <summary>按 id 取定义；不存在返回 null。</summary>
    public UnitDefinition Get(string id)
    {
        if (units == null || string.IsNullOrEmpty(id)) return null;
        for (int i = 0; i < units.Count; i++)
        {
            if (units[i] != null && units[i].id == id)
                return units[i];
        }
        return null;
    }

    /// <summary>
    /// 按 (id, level) 查该级数值；level 超出配表范围时沿用末级；
    /// 命中返回 true，未命中（id 不存在或没有等级数据）返回 false。
    /// </summary>
    public bool TryGetLevel(string id, int level, out UnitLevelData data)
    {
        data = null;
        UnitDefinition def = Get(id);
        if (def == null || def.levels == null || def.levels.Count == 0) return false;

        int index = Mathf.Clamp(level - 1, 0, def.levels.Count - 1);
        data = def.levels[index];
        return data != null;
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单条弹珠属性默认值条目:与 CSV 一行对应。
/// </summary>
[Serializable]
public class BallStatDefault
{
    public BallStatType statType;
    public float baseValue;
}

/// <summary>
/// 弹珠属性默认值表 ScriptableObject:由 DataImporter 从 9_Excel/BallStatDefaults.csv 生成,
/// 由 BallStats.Reset() 在每局开始时读取作为基础值。
/// 缺项或表为空时,BallStats 自动回退到代码内的硬编码兜底值。
/// </summary>
[CreateAssetMenu(fileName = "BallStatDefaultsTable", menuName = "PinBall2D/Data/BallStatDefaultsTable", order = 3)]
public class BallStatDefaultsTable : ScriptableObject
{
    [SerializeField]
    [Tooltip("每个 BallStatType 的默认基础值;Reset 时优先读取此处,缺项回退到代码兜底。")]
    private List<BallStatDefault> defaults = new List<BallStatDefault>();

    public IReadOnlyList<BallStatDefault> Defaults => defaults;

    public int Count => defaults != null ? defaults.Count : 0;

    public void SetDefaults(List<BallStatDefault> list)
    {
        defaults = list ?? new List<BallStatDefault>();
    }

    /// <summary>
    /// 查询某个 stat 的默认基础值;命中返回 true。
    /// 同一 statType 重复时取第一条。
    /// </summary>
    public bool TryGet(BallStatType type, out float value)
    {
        if (defaults != null)
        {
            for (int i = 0; i < defaults.Count; i++)
            {
                BallStatDefault d = defaults[i];
                if (d != null && d.statType == type)
                {
                    value = d.baseValue;
                    return true;
                }
            }
        }
        value = 0f;
        return false;
    }
}

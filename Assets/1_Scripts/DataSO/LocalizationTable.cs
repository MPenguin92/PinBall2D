using System.Collections.Generic;
using UnityEngine;

/// <summary>游戏语言。</summary>
public enum GameLanguage
{
    Chinese,
    English,
}

/// <summary>单条本地化文本：Localization.csv 一行。</summary>
[System.Serializable]
public class LocalizedText
{
    /// <summary>文本 key（数据表里以此引用）。</summary>
    public string key;

    /// <summary>简体中文。</summary>
    public string zh;

    /// <summary>英文。</summary>
    public string en;
}

/// <summary>
/// 本地化文本表 ScriptableObject：由 DataImporter 从 9_Excel/Localization.csv 生成。
/// 数据表（Upgrades/Units/Balls 等）里不再直接写展示文本，而是存 key，
/// 展示时经 <see cref="Get"/> 按当前语言取词。
/// </summary>
[CreateAssetMenu(fileName = "LocalizationTable", menuName = "PinBall2D/Data/LocalizationTable", order = 6)]
public class LocalizationTable : ScriptableObject
{
    [SerializeField]
    private List<LocalizedText> entries = new List<LocalizedText>();

    private Dictionary<string, LocalizedText> index;

    public int Count => entries != null ? entries.Count : 0;

    /// <summary>由 Editor 导入工具写入；运行时无需调用。</summary>
    public void SetEntries(List<LocalizedText> list)
    {
        entries = list ?? new List<LocalizedText>();
        index = null;
    }

    /// <summary>
    /// 按 key + 语言取词；key 不存在时原样返回 key（便于缺失时可见）。
    /// </summary>
    public string Get(string key, GameLanguage language)
    {
        if (string.IsNullOrEmpty(key)) return key;

        EnsureIndex();
        if (index == null || !index.TryGetValue(key, out LocalizedText text) || text == null)
            return key;

        return language == GameLanguage.English ? text.en : text.zh;
    }

    private void EnsureIndex()
    {
        if (index != null) return;

        index = new Dictionary<string, LocalizedText>();
        if (entries == null) return;

        for (int i = 0; i < entries.Count; i++)
        {
            LocalizedText t = entries[i];
            if (t == null || string.IsNullOrEmpty(t.key)) continue;
            index[t.key] = t;
        }
    }
}

using UnityEngine;

/// <summary>
/// 全局本地化服务（静态）：保存当前语言，懒加载 <see cref="LocalizationTable"/>，
/// 数据表里的展示文本 key 经 <see cref="Get"/> 按当前语言取词。
/// 非 MonoBehaviour、无状态持久化需求，故用 static；语言设置/存档后续在此接入。
/// </summary>
public static class Localization
{
    private static GameLanguage currentLanguage = GameLanguage.Chinese;
    private static LocalizationTable table;

    /// <summary>当前语言（默认中文）。</summary>
    public static GameLanguage CurrentLanguage => currentLanguage;

    public static void SetLanguage(GameLanguage value)
    {
        currentLanguage = value;
    }

    /// <summary>
    /// 按当前语言取词；key 为空 / 表缺失 / 词条缺失时回退 key 本身（便于排查）。
    /// 表首次使用时经 AssetLoader 加载（"LocalizationTable"）。
    /// </summary>
    public static string Get(string key)
    {
        if (string.IsNullOrEmpty(key)) return key;

        EnsureTable();
        if (table == null) return key;
        return table.Get(key, currentLanguage);
    }

    /// <summary>预加载文本表（可在 GameLogicManager.Awake 调用避免首帧卡顿）；失败无妨，Get 会再试。</summary>
    public static void Preload()
    {
        EnsureTable();
    }

    private static void EnsureTable()
    {
        if (table == null)
            table = AssetLoader.Load<LocalizationTable>("LocalizationTable");
    }
}

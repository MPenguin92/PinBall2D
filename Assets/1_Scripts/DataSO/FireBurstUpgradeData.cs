using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 连发词条某一级的数据（Upgrades_Fire.csv 一行）。
/// </summary>
[System.Serializable]
public class FireLevelData
{
    /// <summary>等级化描述：选卡时展示（如「每次发射 3 颗弹珠」）。</summary>
    public string desc;

    /// <summary>该级每次射击发射的球数（直接取值，不做推导）。</summary>
    public int shots;

    /// <summary>该级相邻两发的时间间隔（秒）。</summary>
    public float interval;
}

/// <summary>
/// 行为类升级词条：连发（Burst）。
/// 每次抽到升 1 级（UpgradeService 在 Apply 成功后才 IncrementLevel，
/// 因此 Apply 内 CurrentLevel = 旧等级，本次升级后到达 level = CurrentLevel + 1），
/// 逐级数据（发射球数 / 间隔 / 等级化描述）全部取自 <see cref="levels"/>。
/// 应用方式：把 Player 的 FireStrategy 替换为参数化的 <see cref="BurstFireStrategy"/>。
///
/// 数据来源（通用元信息 + 类型专有逐级表，由 DataImporter 写入）：
///   Upgrades.csv       通用列：id, name, desc（抽象概括）, rarity, maxLevel
///   Upgrades_Fire.csv  专有列：id, level, desc（等级化描述）, shots, interval
/// </summary>
public class FireBurstUpgradeData : UpgradeBase
{
    [SerializeField]
    [Tooltip("逐级数据：下标 0 = 第 1 级；未配置更高等级时沿用末级。")]
    private List<FireLevelData> levels = new List<FireLevelData>();

    public IReadOnlyList<FireLevelData> Levels => levels;

    /// <summary>由 DataImporter 在导入时写入（按等级 1..N 顺序传入，已补齐到满级）。</summary>
    public void SetLevels(List<FireLevelData> list)
    {
        levels = list ?? new List<FireLevelData>();
    }

    /// <summary>卡面展示：返回「升到下一级后」的等级化描述；无等级数据时回退通用描述。</summary>
    public override string OfferDescription
    {
        get
        {
            FireLevelData data = GetLevelData(CurrentLevel + 1);
            if (data == null || string.IsNullOrEmpty(data.desc))
                return Description;
            return data.desc;
        }
    }

    public override void Apply(UpgradeContext ctx)
    {
        if (ctx == null || ctx.Player == null) return;

        FireLevelData data = GetLevelData(CurrentLevel + 1);
        if (data == null) return;

        int shots = Mathf.Max(1, data.shots);
        float interval = Mathf.Max(0f, data.interval);
        ctx.Player.SetFireStrategy(new BurstFireStrategy(shots, interval));
    }

    private FireLevelData GetLevelData(int level)
    {
        if (levels == null || levels.Count == 0) return null;
        int index = Mathf.Clamp(level - 1, 0, levels.Count - 1);
        return levels[index];
    }
}

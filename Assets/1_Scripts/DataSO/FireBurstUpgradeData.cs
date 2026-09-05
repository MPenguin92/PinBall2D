using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 连发词条某一级的数据（Upgrades_Fire.csv 一行）。
/// 发哪颗球/每级发几颗由本表 + 推导规则给出：主弹 base 固定 1 颗，
/// 副弹 ball_sub 发 <see cref="subCount"/> 颗（等级 = 连发本级，伤害查 Balls_Level.csv）。
/// </summary>
[System.Serializable]
public class FireLevelData
{
    /// <summary>等级化描述文本 key：选卡时展示（如「发射 2 颗：副弹伤害 20%」）。</summary>
    public string desc;

    /// <summary>该级副弹（ball_sub）发射颗数；主弹 1 颗固定。</summary>
    public int subCount;

    /// <summary>该级相邻两发的时间间隔（秒）。</summary>
    public float interval;
}

/// <summary>
/// 行为类升级词条：连发（Burst）。
/// 每次抽到升 1 级（UpgradeService 在 Apply 成功后才 IncrementLevel，
/// 因此 Apply 内 CurrentLevel = 旧等级，本次升级后到达 level = CurrentLevel + 1）。
///
/// 升级效果（发射序列）：主弹 base Lv1 固定 1 颗；副弹 ball_sub 颗数读本表 subCount，
/// 等级 = 本次连发等级（伤害随 Balls_Level 的 ball_sub 曲线提升）。
/// 例：Lv1 [base, ball_sub@1]（副弹 20%）；Lv5 [base, ball_sub@5 ×2]（表配 subCount=2）。
///
/// 应用方式：把 Player 的 FireStrategy 替换为参数化的 <see cref="BurstFireStrategy"/>。
///
/// 数据来源（通用元信息 + 类型专有逐级表，由 DataImporter 写入）：
///   Upgrades.csv       通用列：id, name, desc（抽象概括）, rarity, maxLevel
///   Upgrades_Fire.csv  专有列：id, level, desc（等级化描述）, interval
///   Balls.csv / Balls_Level.csv：球型定义与各等级伤害
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

    /// <summary>卡面展示：返回「升到下一级后」的等级化描述（按 key 本地化）；无等级数据时回退通用描述。</summary>
    public override string OfferDescription
    {
        get
        {
            FireLevelData data = GetLevelData(CurrentLevel + 1);
            if (data == null || string.IsNullOrEmpty(data.desc))
                return Description;
            return GetText(data.desc);
        }
    }

    public override void Apply(UpgradeContext ctx)
    {
        if (ctx == null || ctx.Player == null) return;

        FireLevelData data = GetLevelData(CurrentLevel + 1);
        if (data == null) return;

        int level = CurrentLevel + 1;

        // 主弹 1 颗基础弹；副弹颗数/等级均读表（subCount + ball_sub@本级）。
        List<FireShot> shots = new List<FireShot> { FireShot.Base };
        int subCount = Mathf.Max(0, data.subCount);
        for (int i = 0; i < subCount; i++)
            shots.Add(new FireShot(Defines.BallSubId, level));

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

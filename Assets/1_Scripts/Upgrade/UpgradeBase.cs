using UnityEngine;

/// <summary>
/// 升级词条基类：所有数值/新球升级数据 SO 继承此类。
/// 同时保存运行时堆叠状态，便于服务层判断"已堆满"与生成显示文案。
///
/// 注意：派生类一般为 ScriptableObject，因此本类同样继承 ScriptableObject。
/// </summary>
public abstract class UpgradeBase : ScriptableObject
{
    [SerializeField]
    [Tooltip("唯一 id（与 CSV 第一列对应），用于堆叠去重")]
    private string id;

    [SerializeField]
    [Tooltip("展示名")]
    private string displayName;

    [SerializeField]
    [TextArea]
    [Tooltip("展示描述（支持简单 rich text）")]
    private string description;

    [SerializeField]
    private UpgradeRarity rarity = UpgradeRarity.Common;

    [SerializeField]
    [Tooltip("满级等级：抽到一次升 1 级，达到满级后从抽卡池剔除")]
    private int maxLevel = 1;

    [System.NonSerialized]
    private int currentLevel;

    public string Id => id;

    public string DisplayName => displayName;

    public string Description => description;

    public UpgradeRarity Rarity => rarity;

    public int MaxLevel => Mathf.Max(1, maxLevel);

    /// <summary>当前已升到的等级（0 = 尚未抽中；抽中一次 +1）。</summary>
    public int CurrentLevel => currentLevel;

    public bool IsFull => currentLevel >= MaxLevel;

    /// <summary>设置由 CSV 写入的字段（仅 Editor 导入时使用）。</summary>
    public void SetMeta(string id, string name, string desc, UpgradeRarity rarity, int maxLevel)
    {
        this.id = id;
        this.displayName = name;
        this.description = desc;
        this.rarity = rarity;
        this.maxLevel = Mathf.Max(1, maxLevel);
    }

    /// <summary>
    /// 抽卡卡面展示描述：默认返回通用描述（Upgrades.csv 的 desc）。
    /// 子类可覆盖为「升级到下一级的等级化描述」（如专有表里每级更具体的文案），
    /// 展示时升级尚未应用，因此按 CurrentLevel + 1 取目标等级。
    /// </summary>
    public virtual string OfferDescription => description;

    /// <summary>由 UpgradeService 在 GameStart 时调用，重置等级计数。</summary>
    public void ResetRuntimeState()
    {
        currentLevel = 0;
    }

    /// <summary>应用一层升级（升 1 级）；由派生类实现具体逻辑。
    /// 调用方在调用本方法后应自行 <see cref="IncrementLevel"/>，
    /// 因此 Apply 内 CurrentLevel 为尚未 +1 的旧等级（即升到新级前的状态）。</summary>
    public abstract void Apply(UpgradeContext ctx);

    /// <summary>抽中一次升 1 级（由 UpgradeService 在 Apply 成功后调用）。</summary>
    public void IncrementLevel()
    {
        currentLevel++;
    }
}

/// <summary>
/// 升级应用时所需的运行时上下文（由 UpgradeService 提供给词条）。
/// </summary>
public class UpgradeContext
{
    public BallStats Stats;
    public Player Player;
}

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
    [Tooltip("最大堆叠层数；达到后从抽卡池剔除")]
    private int maxStack = 1;

    [System.NonSerialized]
    private int currentStack;

    public string Id => id;

    public string DisplayName => displayName;

    public string Description => description;

    public UpgradeRarity Rarity => rarity;

    public int MaxStack => Mathf.Max(1, maxStack);

    public int CurrentStack => currentStack;

    public bool IsFull => currentStack >= MaxStack;

    /// <summary>设置由 CSV 写入的字段（仅 Editor 导入时使用）。</summary>
    public void SetMeta(string id, string name, string desc, UpgradeRarity rarity, int maxStack)
    {
        this.id = id;
        this.displayName = name;
        this.description = desc;
        this.rarity = rarity;
        this.maxStack = Mathf.Max(1, maxStack);
    }

    /// <summary>由 UpgradeService 在 GameStart 时调用，重置堆叠计数。</summary>
    public void ResetRuntimeState()
    {
        currentStack = 0;
    }

    /// <summary>
    /// 应用一层升级；由派生类实现具体逻辑（修改 BallStats / SpecialBallParams 等）。
    /// 调用方在调用本方法后应自行 ++currentStack。
    /// </summary>
    public abstract void Apply(UpgradeContext ctx);

    public void IncrementStack()
    {
        currentStack++;
    }
}

/// <summary>
/// 升级应用时所需的运行时上下文（由 UpgradeService 提供给词条）。
/// </summary>
public class UpgradeContext
{
    public BallStats Stats;
    public SpecialBallParams SpecialParams;
    public Player Player;
}

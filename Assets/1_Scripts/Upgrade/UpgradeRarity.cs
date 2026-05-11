/// <summary>
/// Roguelike 升级稀有度。每个里程碑按权重抽取一个稀有度，
/// 然后在该稀有度池内 3 选 1。
/// </summary>
public enum UpgradeRarity
{
    Common = 0,
    Uncommon = 1,
    Rare = 2,
    Legendary = 3,
}

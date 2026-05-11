using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全局升级池。持有所有可抽到的 <see cref="UpgradeBase"/> 资源（数值类 + 新球类）。
/// 由 DataImporter 写入；UpgradeService 在 GameStart 时拷贝引用并清零堆叠状态。
/// </summary>
[CreateAssetMenu(fileName = "UpgradeCatalog", menuName = "PinBall2D/Data/UpgradeCatalog", order = 2)]
public class UpgradeCatalog : ScriptableObject
{
    [SerializeField]
    [Tooltip("所有可抽到的升级词条（数值 + 新球）。")]
    private List<UpgradeBase> entries = new List<UpgradeBase>();

    public IReadOnlyList<UpgradeBase> Entries => entries;

    public int Count => entries != null ? entries.Count : 0;

    public void SetEntries(List<UpgradeBase> list)
    {
        entries = list ?? new List<UpgradeBase>();
    }
}

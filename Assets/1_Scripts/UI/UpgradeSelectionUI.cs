using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Roguelike 三选一升级面板。监听 <see cref="GameEvents.OnUpgradeOffered"/> 显示，
/// 监听 <see cref="GameEvents.OnUpgradeApplied"/> 隐藏。
///
/// 自身不做布局：在 Inspector 拖三张「卡片」根节点（按顺序：左中右），
/// 每张卡片必须包含 nameText / descText / rarityText（可选）以及 Button。
/// </summary>
public class UpgradeSelectionUI : MonoBehaviour
{
    [System.Serializable]
    public class CardView
    {
        public Button button;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI descText;
        public TextMeshProUGUI rarityText;
        public Image background;
    }

    [SerializeField]
    private GameObject panelRoot;

    [SerializeField]
    private List<CardView> cards = new List<CardView>(3);

    [SerializeField]
    private Color colorCommon = new Color(0.85f, 0.85f, 0.85f, 1f);

    [SerializeField]
    private Color colorUncommon = new Color(0.4f, 0.7f, 1f, 1f);

    [SerializeField]
    private Color colorRare = new Color(0.7f, 0.4f, 1f, 1f);

    [SerializeField]
    private Color colorLegendary = new Color(1f, 0.75f, 0.2f, 1f);

    private readonly List<UpgradeBase> currentOptions = new List<UpgradeBase>();

    private void Awake()
    {
        for (int i = 0; i < cards.Count; i++)
        {
            int idx = i;
            CardView card = cards[i];
            if (card != null && card.button != null)
                card.button.onClick.AddListener(() => OnCardClicked(idx));
        }

        if (panelRoot != null) panelRoot.SetActive(false);

        GameEvents.OnUpgradeOffered += HandleOffered;
        GameEvents.OnUpgradeApplied += HandleApplied;
        GameEvents.OnReturnToHome += HandleReturnToHome;
        GameEvents.OnGameEnd += HandleGameEnd;
    }

    private void OnDestroy()
    {
        GameEvents.OnUpgradeOffered -= HandleOffered;
        GameEvents.OnUpgradeApplied -= HandleApplied;
        GameEvents.OnReturnToHome -= HandleReturnToHome;
        GameEvents.OnGameEnd -= HandleGameEnd;
    }

    private void HandleOffered(IList<UpgradeBase> options)
    {
        currentOptions.Clear();
        if (options != null) currentOptions.AddRange(options);

        if (panelRoot != null) panelRoot.SetActive(true);

        for (int i = 0; i < cards.Count; i++)
        {
            CardView card = cards[i];
            if (card == null) continue;

            bool has = i < currentOptions.Count && currentOptions[i] != null;
            if (card.button != null) card.button.interactable = has;

            if (!has)
            {
                if (card.nameText != null) card.nameText.text = string.Empty;
                if (card.descText != null) card.descText.text = string.Empty;
                if (card.rarityText != null) card.rarityText.text = string.Empty;
                continue;
            }

            UpgradeBase u = currentOptions[i];
            if (card.nameText != null) card.nameText.text = u.DisplayName;
            if (card.descText != null) card.descText.text = u.Description;
            if (card.rarityText != null) card.rarityText.text = u.Rarity.ToString();

            Color tint = GetRarityColor(u.Rarity);
            if (card.background != null) card.background.color = tint;
            if (card.rarityText != null) card.rarityText.color = tint;
        }
    }

    private void HandleApplied(UpgradeBase _)
    {
        currentOptions.Clear();
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void HandleReturnToHome()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void HandleGameEnd()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void OnCardClicked(int idx)
    {
        if (idx < 0 || idx >= currentOptions.Count) return;
        UpgradeBase chosen = currentOptions[idx];
        if (chosen == null) return;

        UpgradeService svc = GameLogicManager.Instance != null ? GameLogicManager.Instance.UpgradeService : null;
        if (svc == null) return;

        svc.ApplySelected(chosen);
    }

    private Color GetRarityColor(UpgradeRarity r)
    {
        switch (r)
        {
            case UpgradeRarity.Common: return colorCommon;
            case UpgradeRarity.Uncommon: return colorUncommon;
            case UpgradeRarity.Rare: return colorRare;
            case UpgradeRarity.Legendary: return colorLegendary;
            default: return colorCommon;
        }
    }
}

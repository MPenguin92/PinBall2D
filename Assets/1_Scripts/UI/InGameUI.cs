using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 游戏中 HUD：左下纵向生命值、右侧升级宝箱入口。
/// （右下角弹珠队列已于 2026-09-03 移除；顶部经验文本已于 2026-09-04 移除——
/// 升级机会改由击杀宝箱怪获得，经验进度改为数据接口供后续手动查看 UI 接入。）
/// </summary>
public class InGameUI : MonoBehaviour
{
    [SerializeField]
    private Player player;

    [Header("Health — bottom-left, vertical")]
    [SerializeField]
    private RectTransform heartContainer;

    [SerializeField]
    private Sprite heartSprite;

    [SerializeField]
    private Vector2 heartSize = new Vector2(42f, 42f);

    [SerializeField]
    private float heartSpacing = 8f;

    [SerializeField]
    private Color heartColor = Color.red;

    [SerializeField]
    [Range(0f, 1f)]
    private float emptyHeartAlpha = 0.25f;

    [Header("Upgrade chest button — right side")]
    [SerializeField]
    [Tooltip("升级宝箱按钮：有剩余升级次数时显示，点击打开三选一面板。图标为 Image，Sprite 留空由美术资源导入。")]
    private Button chestButton;

    [SerializeField]
    [Tooltip("宝箱按钮右下角的剩余升级次数文本")]
    private TextMeshProUGUI chestCountText;

    private readonly List<Image> heartImages = new List<Image>();

    private int lastHp = -1;
    private int lastMaxHp = -1;

    private void Awake()
    {
        if (chestButton != null)
            chestButton.onClick.AddListener(OnChestClicked);
    }

    private void OnEnable()
    {
        Refresh(true);
    }

    private void Update()
    {
        Refresh(false);
    }

    private void Refresh(bool force)
    {
        Player target = ResolvePlayer();
        if (target == null)
            return;

        if (force || target.MaxHp != lastMaxHp)
            RebuildHearts(target.MaxHp);

        if (force || target.CurrentHp != lastHp || target.MaxHp != lastMaxHp)
            RefreshHearts(target.CurrentHp, target.MaxHp);

        RefreshChestButton();

        lastHp = target.CurrentHp;
        lastMaxHp = target.MaxHp;
    }

    /// <summary>
    /// 宝箱按钮显隐与角标数字：有剩余升级次数且不在升级选择期间才显示；
    /// 点击后由 <see cref="GameLogicManager.OpenUpgradeSelection"/> 打开三选一面板。
    /// </summary>
    private void RefreshChestButton()
    {
        if (chestButton == null) return;

        GameLogicManager mgr = GameLogicManager.Instance;
        UpgradeService svc = mgr != null ? mgr.UpgradeService : null;
        bool selecting = mgr != null && mgr.CurrentState == GameState.SelectingUpgrade;
        int count = svc != null ? svc.PendingUpgradeCount : 0;

        bool show = !selecting && count > 0;
        if (chestButton.gameObject.activeSelf != show)
            chestButton.gameObject.SetActive(show);

        if (chestCountText != null)
            chestCountText.text = count.ToString();
    }

    private void OnChestClicked()
    {
        GameLogicManager mgr = GameLogicManager.Instance;
        if (mgr != null)
            mgr.OpenUpgradeSelection();
    }

    private Player ResolvePlayer()
    {
        if (player != null)
            return player;

        GameLogicManager manager = GameLogicManager.Instance;
        player = manager != null ? manager.Player : null;
        return player;
    }

    private void RebuildHearts(int maxHp)
    {
        if (heartContainer == null || heartSprite == null)
            return;

        ClearImages(heartImages);

        for (int i = 0; i < maxHp; i++)
        {
            Image image = CreateSlotIcon(heartContainer, heartSprite, heartSize, i, heartSpacing, anchorRight: false);
            heartImages.Add(image);
        }

        heartContainer.sizeDelta = new Vector2(
            heartSize.x,
            VerticalStackHeight(maxHp, heartSize.y, heartSpacing)
        );
    }

    private void RefreshHearts(int currentHp, int maxHp)
    {
        int hp = Mathf.Clamp(currentHp, 0, maxHp);
        for (int i = 0; i < heartImages.Count; i++)
        {
            if (heartImages[i] == null) continue;

            Color color = heartColor;
            color.a = i < hp ? 1f : emptyHeartAlpha;
            heartImages[i].color = color;
        }
    }

    private static Image CreateSlotIcon(
        RectTransform container,
        Sprite sprite,
        Vector2 size,
        int index,
        float spacing,
        bool anchorRight)
    {
        GameObject slotObject = new GameObject($"Slot_{index + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        slotObject.transform.SetParent(container, false);

        RectTransform rectTransform = (RectTransform)slotObject.transform;
        if (anchorRight)
        {
            rectTransform.anchorMin = new Vector2(1f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 0f);
            rectTransform.pivot = new Vector2(1f, 0f);
        }
        else
        {
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(0f, 0f);
            rectTransform.pivot = new Vector2(0f, 0f);
        }

        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = new Vector2(0f, index * (size.y + spacing));

        Image image = slotObject.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private static float VerticalStackHeight(int count, float itemHeight, float spacing)
    {
        if (count <= 0) return 0f;
        return count * itemHeight + (count - 1) * spacing;
    }

    private static void ClearImages(List<Image> images)
    {
        for (int i = images.Count - 1; i >= 0; i--)
        {
            if (images[i] != null)
                Destroy(images[i].gameObject);
        }
        images.Clear();
    }
}

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 游戏中 HUD：左下纵向生命值、右下纵向弹珠队列图标、顶部居中经验值、右侧升级宝箱入口。
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

    [Header("Ball queue — bottom-right, vertical icons")]
    [SerializeField]
    private RectTransform ballQueueContainer;

    [SerializeField]
    private Vector2 ballIconSize = new Vector2(36f, 36f);

    [SerializeField]
    private float ballIconSpacing = 6f;

    [Header("Experience — top center")]
    [SerializeField]
    private TextMeshProUGUI killCountText;

    [Header("Upgrade chest button — right side")]
    [SerializeField]
    [Tooltip("升级宝箱按钮：有剩余升级次数时显示，点击打开三选一面板。图标为 Image，Sprite 留空由美术资源导入。")]
    private Button chestButton;

    [SerializeField]
    [Tooltip("宝箱按钮右下角的剩余升级次数文本")]
    private TextMeshProUGUI chestCountText;

    private readonly List<Image> heartImages = new List<Image>();
    private readonly List<Image> ballIcons = new List<Image>();
    private readonly List<BallType> lastBallQueue = new List<BallType>();
    private BallSpriteSet ballSpriteSet;

    private int lastHp = -1;
    private int lastMaxHp = -1;
    private KillMilestoneTable cachedMilestoneTable;
    private bool milestoneTableLoaded;

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

        if (force || BallQueueChanged(target.BallQueue))
        {
            RebuildBallIcons(target.BallQueue);
            SyncBallQueueCache(target.BallQueue);
        }

        RefreshKillCount();
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

    private void RebuildBallIcons(IReadOnlyCollection<BallType> queue)
    {
        if (ballQueueContainer == null)
            return;

        ClearImages(ballIcons);

        BallSpriteSet spriteSet = ResolveBallSpriteSet();
        if (spriteSet == null || queue == null || queue.Count == 0)
        {
            ballQueueContainer.sizeDelta = new Vector2(ballIconSize.x, 0f);
            return;
        }

        int index = 0;
        foreach (BallType ballType in queue)
        {
            Sprite sprite = spriteSet.Get(ballType);
            if (sprite == null)
                continue;

            Image image = CreateSlotIcon(ballQueueContainer, sprite, ballIconSize, index, ballIconSpacing, anchorRight: true);
            image.color = Color.white;
            ballIcons.Add(image);
            index++;
        }

        ballQueueContainer.sizeDelta = new Vector2(
            ballIconSize.x,
            VerticalStackHeight(index, ballIconSize.y, ballIconSpacing)
        );
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

    private bool BallQueueChanged(IReadOnlyCollection<BallType> queue)
    {
        if (queue == null)
            return lastBallQueue.Count > 0;

        if (queue.Count != lastBallQueue.Count)
            return true;

        int index = 0;
        foreach (BallType ballType in queue)
        {
            if (lastBallQueue[index] != ballType)
                return true;
            index++;
        }

        return false;
    }

    private void SyncBallQueueCache(IReadOnlyCollection<BallType> queue)
    {
        lastBallQueue.Clear();
        if (queue == null) return;
        foreach (BallType ballType in queue)
            lastBallQueue.Add(ballType);
    }

    private BallSpriteSet ResolveBallSpriteSet()
    {
        if (ballSpriteSet != null)
            return ballSpriteSet;

        ballSpriteSet = AssetLoader.Load<BallSpriteSet>("BallSpriteSet");
        return ballSpriteSet;
    }

    private void RefreshKillCount()
    {
        if (killCountText == null) return;

        UpgradeService svc = GameLogicManager.Instance != null ? GameLogicManager.Instance.UpgradeService : null;
        if (svc == null)
        {
            killCountText.text = string.Empty;
            return;
        }

        if (!milestoneTableLoaded)
        {
            cachedMilestoneTable = AssetLoader.Load<KillMilestoneTable>("KillMilestoneTable");
            milestoneTableLoaded = true;
        }

        int cur = svc.ExperienceAccumulated;
        int next = 0;
        if (cachedMilestoneTable != null && cachedMilestoneTable.Count > 0)
            next = cachedMilestoneTable.GetThresholdAt(svc.NextMilestoneIdx);

        killCountText.text = next > 0 ? $"EXP {cur}/{next}" : $"EXP {cur}";
    }
}
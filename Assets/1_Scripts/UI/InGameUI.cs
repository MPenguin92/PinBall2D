using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 游戏中 HUD：显示 Player 生命值、各 BallType 库存（已解锁的特殊球与普通球）以及当前击杀计数。
/// </summary>
public class InGameUI : MonoBehaviour
{
    [SerializeField]
    private Player player;

    [SerializeField]
    private RectTransform heartContainer;

    [SerializeField]
    private Sprite heartSprite;

    [SerializeField]
    private TextMeshProUGUI pinBallCountText;

    [SerializeField]
    [Tooltip("可选：显示累计经验值与下一里程碑阈值。")]
    private TextMeshProUGUI killCountText;

    [SerializeField]
    private Vector2 heartSize = new Vector2(42f, 42f);

    [SerializeField]
    private float heartSpacing = 8f;

    [SerializeField]
    private Color heartColor = Color.red;

    [SerializeField]
    [Range(0f, 1f)]
    private float emptyHeartAlpha = 0.25f;

    /// <summary>HUD 队列中每种球的单字符标签（按队首到队尾顺序串接）。</summary>
    private static readonly Dictionary<BallType, string> QueueLabels = new Dictionary<BallType, string>
    {
        { BallType.Base, "B" },
        { BallType.Fire, "F" },
        { BallType.Ice, "I" },
        { BallType.Lightning, "L" },
        { BallType.Poison, "P" },
        { BallType.Heavy, "H" },
        { BallType.Boomerang, "R" },
    };

    /// <summary>HUD 各 BallType 的 TMP 颜色 hex（不含 #）。</summary>
    private static readonly Dictionary<BallType, string> QueueColors = new Dictionary<BallType, string>
    {
        { BallType.Base, "FFFFFF" },
        { BallType.Fire, "FF6633" },
        { BallType.Ice, "66CCFF" },
        { BallType.Lightning, "FFE066" },
        { BallType.Poison, "99CC33" },
        { BallType.Heavy, "AAAAAA" },
        { BallType.Boomerang, "FF99CC" },
    };

    private readonly List<Image> heartImages = new List<Image>();
    private readonly StringBuilder ballText = new StringBuilder(64);
    private int lastHp = -1;
    private int lastMaxHp = -1;
    private KillMilestoneTable cachedMilestoneTable;
    private bool milestoneTableLoaded;

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

        RefreshBallCounts(target);
        RefreshKillCount();

        lastHp = target.CurrentHp;
        lastMaxHp = target.MaxHp;
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

        for (int i = heartImages.Count - 1; i >= 0; i--)
        {
            if (heartImages[i] != null)
                Destroy(heartImages[i].gameObject);
        }

        heartImages.Clear();

        for (int i = 0; i < maxHp; i++)
        {
            GameObject heartObject = new GameObject($"Heart_{i + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            heartObject.transform.SetParent(heartContainer, false);

            RectTransform rectTransform = (RectTransform)heartObject.transform;
            rectTransform.anchorMin = new Vector2(0f, 0.5f);
            rectTransform.anchorMax = new Vector2(0f, 0.5f);
            rectTransform.pivot = new Vector2(0f, 0.5f);
            rectTransform.sizeDelta = heartSize;
            rectTransform.anchoredPosition = new Vector2(i * (heartSize.x + heartSpacing), 0f);

            Image image = heartObject.GetComponent<Image>();
            image.sprite = heartSprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            heartImages.Add(image);
        }

        heartContainer.sizeDelta = new Vector2(
            Mathf.Max(0f, maxHp * heartSize.x + Mathf.Max(0, maxHp - 1) * heartSpacing),
            heartSize.y
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

    private void RefreshBallCounts(Player target)
    {
        if (pinBallCountText == null) return;

        ballText.Length = 0;

        // 队首 → 队尾，每颗球渲染为带颜色的单字符。空队列也明确显示 "(empty)"。
        bool first = true;
        foreach (BallType bt in target.BallQueue)
        {
            if (!first) ballText.Append(' ');
            first = false;

            string label = QueueLabels.TryGetValue(bt, out string l) ? l : "?";
            string color = QueueColors.TryGetValue(bt, out string c) ? c : "FFFFFF";
            ballText.Append("<color=#").Append(color).Append('>').Append(label).Append("</color>");
        }
        if (first) ballText.Append("(empty)");

        // 末尾追加一行汇总：飞行中 / 容量。
        ballText.Append("  <size=70%>(").Append(target.BallsInFlight).Append('/').Append(target.TotalBalls).Append(")</size>");

        pinBallCountText.text = ballText.ToString();
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

        if (next > 0)
            killCountText.text = $"EXP {cur}/{next}";
        else
            killCountText.text = $"EXP {cur}";
    }
}

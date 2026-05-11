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
    [Tooltip("可选：显示累计击杀数与下一里程碑阈值。")]
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

    /// <summary>HUD 显示的 BallType 顺序：普通球放最前，特殊球按 enum 顺序。</summary>
    private static readonly BallType[] DisplayOrder =
    {
        BallType.Base,
        BallType.Fire,
        BallType.Ice,
        BallType.Lightning,
        BallType.Poison,
        BallType.Heavy,
        BallType.Boomerang,
    };

    private static readonly Dictionary<BallType, string> DisplayLabels = new Dictionary<BallType, string>
    {
        { BallType.Base, "Ball" },
        { BallType.Fire, "Fire" },
        { BallType.Ice, "Ice" },
        { BallType.Lightning, "Lt" },
        { BallType.Poison, "Poi" },
        { BallType.Heavy, "Hvy" },
        { BallType.Boomerang, "Bmr" },
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
        bool first = true;
        for (int i = 0; i < DisplayOrder.Length; i++)
        {
            BallType bt = DisplayOrder[i];
            int max = target.GetMaxCount(bt);
            if (max <= 0 && bt != BallType.Base) continue;

            int cur = target.GetCurrentCount(bt);
            if (!first) ballText.Append("  ");
            first = false;
            ballText.Append(DisplayLabels[bt]).Append(":").Append(cur).Append("/").Append(max);
        }
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

        int cur = svc.KillCount;
        int next = 0;
        if (cachedMilestoneTable != null && cachedMilestoneTable.Count > 0)
            next = cachedMilestoneTable.GetThresholdAt(svc.NextMilestoneIdx);

        if (next > 0)
            killCountText.text = $"Kills {cur}/{next}";
        else
            killCountText.text = $"Kills {cur}";
    }
}

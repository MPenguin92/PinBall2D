using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 游戏中 HUD：显示 Player 生命值和实时弹珠数量。
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
    private Vector2 heartSize = new Vector2(42f, 42f);

    [SerializeField]
    private float heartSpacing = 8f;

    [SerializeField]
    private Color heartColor = Color.red;

    [SerializeField]
    [Range(0f, 1f)]
    private float emptyHeartAlpha = 0.25f;

    private readonly List<Image> heartImages = new List<Image>();
    private int lastHp = -1;
    private int lastMaxHp = -1;
    private int lastPinBallCount = -1;
    private int lastMaxPinBallCount = -1;

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

        if (force || target.CurrentPinBallCount != lastPinBallCount || target.MaxPinBallCount != lastMaxPinBallCount)
            RefreshPinBallCount(target.CurrentPinBallCount, target.MaxPinBallCount);

        lastHp = target.CurrentHp;
        lastMaxHp = target.MaxHp;
        lastPinBallCount = target.CurrentPinBallCount;
        lastMaxPinBallCount = target.MaxPinBallCount;
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

    private void RefreshPinBallCount(int currentCount, int maxCount)
    {
        if (pinBallCountText == null)
            return;

        pinBallCountText.text = $"{currentCount}/{maxCount}";
    }
}

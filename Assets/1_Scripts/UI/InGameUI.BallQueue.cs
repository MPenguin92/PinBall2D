using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// InGameUI 的弹舱队列 partial：右下角显示「接下来 queueCapacity 次射击」的预测队列，
/// 每次射击后整体上移滚动、队尾补新球，形成「弹舱出舱、后面补上」的效果。
///
/// 一格 = 一次射击：普通单发白色；能力射击（连发/扇形整组）占 1 格金色。
/// 图标暂用纯色块占位，等球型图标数据结构确定后替换。
/// </summary>
public partial class InGameUI
{
    [Header("Ball queue — bottom-right, next shots")]
    [SerializeField]
    private RectTransform ballQueueContainer;

    [SerializeField]
    private Vector2 ballIconSize = new Vector2(36f, 36f);

    [SerializeField]
    private float ballIconSpacing = 6f;

    /// <summary>普通弹（base）占位色——普通单发格固定白色；能力格图标暂存 key、渲染留空。</summary>
    [SerializeField]
    private Color baseBallColor = Color.white;

    /// <summary>弹舱滚动动画时长（秒）。</summary>
    [SerializeField]
    private float queueScrollDuration = 0.12f;

    /// <summary>弹舱容量（显示的格数 = 预测射击次数）。</summary>
    [SerializeField]
    private int queueCapacity = 5;

    /// <summary>弹舱当前显示的格位（按下标顺序 = 队首→队尾）。</summary>
    private readonly List<Image> queueSlots = new List<Image>();

    /// <summary>当前展示的弹序（缓存，避免每帧重算）。</summary>
    private readonly List<FirePreview> displayedShots = new List<FirePreview>();

    /// <summary>弹舱滚动动画协程是否进行中（进行中则跳过新的滚动，避免重叠/回弹错乱）。</summary>
    private bool queueAnimating;

    /// <summary>订阅升级应用事件（能力变化时刷新预测）。</summary>
    private void OnBallQueueAwake()
    {
        GameEvents.OnUpgradeApplied += HandleUpgradeApplied;
    }

    private void OnBallQueueDestroy()
    {
        GameEvents.OnUpgradeApplied -= HandleUpgradeApplied;
    }

    /// <summary>升级应用后能力可能变化（新增/变强连发），弹舱预测随之刷新。</summary>
    private void HandleUpgradeApplied(UpgradeBase _)
    {
        displayedShots.Clear();
        RefreshBallQueue(true);
    }

    /// <summary>
    /// 刷新弹舱：预测接下来 queueCapacity 次射击；若与当前展示不同则触发
    /// 「队首出舱 → 整体上移滚动 → 队尾补新球」动画。
    /// </summary>
    private void RefreshBallQueue(bool force)
    {
        if (ballQueueContainer == null) return;

        Player target = ResolvePlayer();
        if (target == null)
        {
            if (queueSlots.Count > 0)
            {
                ClearImages(queueSlots);
                displayedShots.Clear();
            }
            return;
        }

        List<FirePreview> next = target.PreviewNextShots(queueCapacity);

        // 内容未变则不处理；动画进行中只更新内容不再播滚动（避免重叠）。
        if (!force && SameShots(next)) return;

        displayedShots.Clear();
        displayedShots.AddRange(next);

        EnsureQueueSlots(next.Count);
        UpdateQueueSlotColors(next);

        // 有变化且无动画进行中，才播滚动动画（队首出舱，其余上移，队尾补位）。
        if (!force && !queueAnimating && queueSlots.Count > 0)
            StartCoroutine(QueueScrollRoutine());
    }

    /// <summary>当前展示的弹序是否与目标一致（逐位比较 IsAbility + Icon）。</summary>
    private bool SameShots(List<FirePreview> next)
    {
        if (next.Count != displayedShots.Count) return false;
        for (int i = 0; i < next.Count; i++)
        {
            FirePreview a = next[i];
            FirePreview b = displayedShots[i];
            if (a.IsAbility != b.IsAbility) return false;
            if (a.AbilityIcon != b.AbilityIcon) return false;
        }
        return true;
    }

    /// <summary>确保弹舱有 N 个格位（不足则新建 Image 槽位，多余的销毁）。</summary>
    private void EnsureQueueSlots(int count)
    {
        if (count == queueSlots.Count) return;

        while (queueSlots.Count > count)
        {
            Image img = queueSlots[queueSlots.Count - 1];
            queueSlots.RemoveAt(queueSlots.Count - 1);
            if (img != null) Destroy(img.gameObject);
        }

        while (queueSlots.Count < count)
        {
            int index = queueSlots.Count;
            Image image = CreateSlotIcon(
                ballQueueContainer,
                null,
                ballIconSize,
                index,
                ballIconSpacing,
                anchorRight: true);
            // 无 sprite 时用纯色块占位，后续接美术图标。
            image.color = baseBallColor;
            queueSlots.Add(image);
        }

        ballQueueContainer.sizeDelta = new Vector2(
            ballIconSize.x,
            VerticalStackHeight(count, ballIconSize.y, ballIconSpacing)
        );
    }

    /// <summary>
    /// 按一次射击占一格给槽位上色：普通单发白色；能力格按词条配置的图标 key 取图，
    /// 图标资源未就绪（key 为空）时留灰占位。
    /// </summary>
    private void UpdateQueueSlotColors(List<FirePreview> previews)
    {
        for (int i = 0; i < queueSlots.Count; i++)
        {
            if (queueSlots[i] == null) continue;

            if (i < previews.Count && previews[i].IsAbility)
            {
                // 能力格：目前仅保存图标 key（previews[i].AbilityIcon），
                // 待球型/能力图标映射表就绪后替换为实际 Sprite。
                queueSlots[i].sprite = null;
                queueSlots[i].color = new Color(0.65f, 0.65f, 0.65f, 1f); // 灰占位
            }
            else
            {
                queueSlots[i].sprite = null;
                queueSlots[i].color = baseBallColor;
            }
        }
    }

    /// <summary>
    /// 滚动补位动画协程：把所有格位整体向上（队首方向）移动一格的距离，再移回原位；
    /// 视觉上表现为「队首出舱、其余上移、队尾新球补上」。
    /// </summary>
    private System.Collections.IEnumerator QueueScrollRoutine()
    {
        queueAnimating = true;
        float step = ballIconSize.y + ballIconSpacing;
        Vector2[] origins = new Vector2[queueSlots.Count];
        for (int i = 0; i < queueSlots.Count; i++)
            origins[i] = queueSlots[i].rectTransform.anchoredPosition;

        float t = 0f;
        float duration = Mathf.Max(0.01f, queueScrollDuration);
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            // 先上移一格（出舱方向），再回弹。
            float offset = Mathf.Lerp(step, 0f, p);
            for (int i = 0; i < queueSlots.Count; i++)
            {
                if (queueSlots[i] == null) continue;
                queueSlots[i].rectTransform.anchoredPosition = origins[i] + new Vector2(0f, offset);
            }
            yield return null;
        }

        for (int i = 0; i < queueSlots.Count; i++)
        {
            if (queueSlots[i] == null) continue;
            queueSlots[i].rectTransform.anchoredPosition = origins[i];
        }
        queueAnimating = false;
    }
}

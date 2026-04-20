using UnityEngine;

/// <summary>
/// 默认单位生成器实现：订阅 <see cref="GameEvents.OnStep"/>，每个 Step 从屏幕上方
/// 批量生成 1..N 个 Unit。N 的上限由可用宽度 / <see cref="Defines.UnitSize"/> 决定，
/// 保证相邻 Unit 不重叠、不越过屏幕边界。
/// 纯逻辑类，由 GameLogicManager 在 Awake 时 new 一次并持有，OnDestroy 时 Dispose。
/// </summary>
public class UnitCreator : IUnitCreator
{
    private const float HorizontalPadding = 0.5f;
    private const float TopOffset = 0f;

    private bool isRunning;
    private bool isPaused;

    public UnitCreator()
    {
        GameEvents.OnGameStart += HandleGameStart;
        GameEvents.OnGamePause += HandleGamePause;
        GameEvents.OnGameResume += HandleGameResume;
        GameEvents.OnGameEnd += HandleGameEnd;
        GameEvents.OnReturnToHome += HandleGameEnd;
        GameEvents.OnStep += HandleStep;
    }

    public void Dispose()
    {
        GameEvents.OnGameStart -= HandleGameStart;
        GameEvents.OnGamePause -= HandleGamePause;
        GameEvents.OnGameResume -= HandleGameResume;
        GameEvents.OnGameEnd -= HandleGameEnd;
        GameEvents.OnReturnToHome -= HandleGameEnd;
        GameEvents.OnStep -= HandleStep;
    }

    private void HandleGameStart()
    {
        isRunning = true;
        isPaused = false;
    }

    private void HandleGamePause()
    {
        if (!isRunning) return;
        isPaused = true;
    }

    private void HandleGameResume()
    {
        if (!isRunning) return;
        isPaused = false;
    }

    private void HandleGameEnd()
    {
        isRunning = false;
        isPaused = false;
    }

    private void HandleStep()
    {
        if (!isRunning || isPaused) return;
        SpawnBatch();
    }

    /// <summary>
    /// 一次性在屏幕顶部生成 1..N 个 Unit。
    /// 把可用宽度均分为 count 个槽，每个槽内随机 X，相邻不重叠。
    /// </summary>
    private void SpawnBatch()
    {
        GameLogicManager mgr = GameLogicManager.Instance;
        if (mgr == null) return;

        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic) return;

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        Vector3 camPos = cam.transform.position;

        float minX = camPos.x - halfWidth + HorizontalPadding;
        float maxX = camPos.x + halfWidth - HorizontalPadding;
        float y = camPos.y + halfHeight - TopOffset;

        if (maxX <= minX) return;
        float availWidth = maxX - minX;

        float unitW = Defines.UnitSize;
        if (unitW <= 0f) return;

        int maxCount = Mathf.Max(1, Mathf.FloorToInt(availWidth / unitW));

        // 从难度表取当前阶段的生成区间，并用屏幕可容纳数夹紧，保证不越界也不重叠。
        int rangeMin = 1;
        int rangeMax = maxCount;
        if (mgr.Difficulty != null && mgr.Difficulty.HasTable)
        {
            (int dMin, int dMax) = mgr.Difficulty.GetSpawnRange();
            rangeMin = Mathf.Clamp(dMin, 1, maxCount);
            rangeMax = Mathf.Clamp(dMax, rangeMin, maxCount);
        }

        int count = Random.Range(rangeMin, rangeMax + 1);

        float slotW = availWidth / count;
        float halfUnit = unitW * 0.5f;
        for (int i = 0; i < count; i++)
        {
            float slotMin = minX + i * slotW + halfUnit;
            float slotMax = minX + (i + 1) * slotW - halfUnit;
            if (slotMax < slotMin)
            {
                float mid = (slotMin + slotMax) * 0.5f;
                slotMin = slotMax = mid;
            }

            float x = Random.Range(slotMin, slotMax);
            mgr.SpawnUnit(new Vector2(x, y));
        }
    }
}

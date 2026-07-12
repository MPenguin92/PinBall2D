using UnityEngine;

/// <summary>
/// 默认单位生成器实现：每个 Step 在屏幕外随机生成一行 Unit（填充率 10%~50%）。
/// 新行在 SpawnStep 时生成于屏外，下一 Step 随全体下移进入画面。
/// 纯逻辑类，由 GameLogicManager 在 Awake 时 new 一次并持有，OnDestroy 时 Dispose。
/// </summary>
public class UnitCreator : IUnitCreator
{
    private const string UnitAddress = "SimpleUnit";
    private const float HorizontalPadding = 0.5f;
    private const float MinFillRatio = 0.1f;
    private const float MaxFillRatio = 0.5f;
    /// <summary>出生点相对可见顶行向上偏移的格数（1 = 完全在屏幕外）。</summary>
    private const float SpawnRowsAboveVisible = 1f;

    private bool isRunning;
    private bool isPaused;

    public UnitCreator()
    {
        GameEvents.OnGameStart += HandleGameStart;
        GameEvents.OnGamePause += HandleGamePause;
        GameEvents.OnGameResume += HandleGameResume;
        GameEvents.OnGameEnd += HandleGameEnd;
        GameEvents.OnReturnToHome += HandleGameEnd;
    }

    public void Dispose()
    {
        GameEvents.OnGameStart -= HandleGameStart;
        GameEvents.OnGamePause -= HandleGamePause;
        GameEvents.OnGameResume -= HandleGameResume;
        GameEvents.OnGameEnd -= HandleGameEnd;
        GameEvents.OnReturnToHome -= HandleGameEnd;
    }

    public void SpawnStep()
    {
        if (!isRunning || isPaused) return;
        SpawnBatch();
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

    /// <summary>
    /// 在屏幕外按固定网格随机生成一行 Unit：每行填充 10%~50% 的列，列位置随机。
    /// 出生点位于可见顶行正上方 1 格；当列被已有 Unit 占用时跳过该列。
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
        float visibleTopRowCenterY = camPos.y + halfHeight - Defines.UnitSize * 0.5f;
        float y = visibleTopRowCenterY + Defines.UnitSize * SpawnRowsAboveVisible;

        if (maxX <= minX) return;
        float availWidth = maxX - minX;

        float unitW = Defines.UnitSize;
        if (unitW <= 0f) return;

        int columnCount = Mathf.Max(1, Mathf.FloorToInt(availWidth / unitW));
        int minSpawn = Mathf.Max(1, Mathf.CeilToInt(columnCount * MinFillRatio));
        int maxSpawn = Mathf.Max(minSpawn, Mathf.FloorToInt(columnCount * MaxFillRatio));
        int spawnCount = Random.Range(minSpawn, maxSpawn + 1);

        float gridWidth = columnCount * unitW;
        float gridStartX = minX + (availWidth - gridWidth) * 0.5f;

        int[] columns = new int[columnCount];
        for (int i = 0; i < columnCount; i++)
            columns[i] = i;

        for (int i = 0; i < spawnCount; i++)
        {
            int pick = Random.Range(i, columnCount);
            (columns[i], columns[pick]) = (columns[pick], columns[i]);
        }

        for (int i = 0; i < spawnCount; i++)
        {
            int col = columns[i];
            float x = gridStartX + (col + 0.5f) * unitW;
            Vector2 spawnPos = new Vector2(x, y);

            if (IsSpawnOccupied(mgr, spawnPos, unitW)) continue;

            mgr.SpawnUnit(UnitAddress, spawnPos);
        }
    }

    private static bool IsSpawnOccupied(GameLogicManager mgr, Vector2 center, float unitSize)
    {
        var actives = mgr.ActiveUnits;
        if (actives == null) return false;

        float half = unitSize * 0.5f;
        Rect spawnRect = new Rect(center.x - half, center.y - half, unitSize, unitSize);

        for (int i = 0; i < actives.Count; i++)
        {
            UnitBase other = actives[i];
            if (other == null || !other.gameObject.activeSelf) continue;
            if (spawnRect.Overlaps(other.UnitRect)) return true;
        }
        return false;
    }
}

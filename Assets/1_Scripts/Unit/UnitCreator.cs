using UnityEngine;

/// <summary>
/// 默认单位生成器实现：
/// - 普通怪（unit_damage，吃伤害型）：每个 Step 在屏幕外生成一批，
///   总数与等级均由难度表驱动（spawnMin~spawnMax + 等级权重）；
/// - 金币怪（unit_gold）**混入普通波**：金币冷却就绪（GameLogicManager 计时），
///   本波会把随机 1~2 只原本的普通怪替换成金币怪（等级跟随当前难度最高等级）。
/// 怪的类型（unitId → prefab 地址）都查 <see cref="UnitTable"/>，不硬编码资源地址。
/// 纯逻辑类，由 GameLogicManager 在 Awake 时 new 一次并持有，OnDestroy 时 Dispose。
/// </summary>
public class UnitCreator : IUnitCreator
{
    /// <summary>普通怪（吃伤害型）在 Units.csv 中的 id。</summary>
    private const string DamageUnitId = "unit_damage";

    /// <summary>金币怪在 Units.csv 中的 id。</summary>
    private const string GoldUnitId = "unit_gold";

    /// <summary>金币波替换普通怪的最大只数（随机 1~max）。</summary>
    private const int MaxGoldPerWave = 2;

    /// <summary>出生点与屏幕左右边缘的间距。</summary>
    private const float HorizontalPadding = 0.5f;

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

    public void SpawnStep(bool allowGoldReplace)
    {
        if (!isRunning || isPaused) return;
        SpawnBatch(allowGoldReplace);
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
    /// 普通怪一批：总数取当前难度阶段 spawnMin~spawnMax 随机区间（不超屏幕列数），
    /// 随机不重复列放置；每只的等级由难度阶段等级权重独立 roll。
    /// 金币冷却就绪（allowGoldReplace）时，先把本波随机 1~2 个位置标为金币怪。
    /// </summary>
    private void SpawnBatch(bool allowGoldReplace)
    {
        GameLogicManager mgr = GameLogicManager.Instance;
        if (mgr == null || mgr.UnitTable == null || mgr.Difficulty == null) return;

        UnitDefinition damage = mgr.UnitTable.Get(DamageUnitId);
        if (damage == null) return;

        if (!TryResolveSpawnGrid(out int columnCount, out float gridStartX, out float y)) return;

        (int min, int max) range = mgr.Difficulty.GetSpawnRange();
        int spawnCount = Mathf.Clamp(Random.Range(range.min, range.max + 1), 1, columnCount);

        int[] columns = ShuffledColumns(columnCount);

        // 金币替换位：columns[0, goldCount) 对应的本波位置改为金币怪。
        UnitDefinition gold = allowGoldReplace ? mgr.UnitTable.Get(GoldUnitId) : null;
        int goldCount = 0;
        int goldLevel = 1;
        if (gold != null)
        {
            goldCount = Mathf.Min(Random.Range(1, MaxGoldPerWave + 1), spawnCount);
            // 在已选中的本波列里再洗一次，取前 goldCount 个作为替换位。
            for (int i = 0; i < goldCount; i++)
            {
                int pick = Random.Range(i, spawnCount);
                (columns[i], columns[pick]) = (columns[pick], columns[i]);
            }
            goldLevel = Mathf.Clamp(mgr.Difficulty.GetStageMaxLevel(), 1, Mathf.Max(1, gold.levels.Count));
        }

        for (int i = 0; i < spawnCount; i++)
        {
            float x = gridStartX + (columns[i] + 0.5f) * Defines.UnitSize;
            Vector2 spawnPos = new Vector2(x, y);
            if (IsSpawnOccupied(spawnPos)) continue;

            if (i < goldCount)
            {
                mgr.SpawnUnit(gold.prefabAddress, spawnPos, goldLevel);
            }
            else
            {
                // 普通怪：每只独立 roll 等级（难度阶段等级权重）。
                int level = mgr.Difficulty.RollSpawnLevel();
                mgr.SpawnUnit(damage.prefabAddress, spawnPos, level);
            }
        }
    }

    /// <summary>
    /// 解析出生行网格：可见顶行上方 1 格处，铺满可用宽度的列。
    /// 成功返回 true 并给出列数、首列中心 x 与出生 y。
    /// </summary>
    private bool TryResolveSpawnGrid(out int columnCount, out float gridStartX, out float y)
    {
        columnCount = 0;
        gridStartX = 0f;
        y = 0f;

        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic) return false;

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        Vector3 camPos = cam.transform.position;

        float minX = camPos.x - halfWidth + HorizontalPadding;
        float maxX = camPos.x + halfWidth - HorizontalPadding;
        float visibleTopRowCenterY = camPos.y + halfHeight - Defines.UnitSize * 0.5f;
        y = visibleTopRowCenterY + Defines.UnitSize * SpawnRowsAboveVisible;

        if (maxX <= minX) return false;

        float availWidth = maxX - minX;
        float unitW = Defines.UnitSize;
        if (unitW <= 0f) return false;

        columnCount = Mathf.Max(1, Mathf.FloorToInt(availWidth / unitW));
        float gridWidth = columnCount * unitW;
        gridStartX = minX + (availWidth - gridWidth) * 0.5f;
        return true;
    }

    /// <summary>返回 [0, columnCount) 洗牌后的列号数组（取前 N 个即 N 个不重复列）。</summary>
    private int[] ShuffledColumns(int columnCount)
    {
        int[] columns = new int[columnCount];
        for (int i = 0; i < columnCount; i++)
            columns[i] = i;

        for (int i = 0; i < columnCount - 1; i++)
        {
            int pick = Random.Range(i, columnCount);
            (columns[i], columns[pick]) = (columns[pick], columns[i]);
        }
        return columns;
    }

    /// <summary>目标格是否已被场上其他 Unit 占用。</summary>
    private bool IsSpawnOccupied(Vector2 center)
    {
        GameLogicManager mgr = GameLogicManager.Instance;
        if (mgr == null) return false;

        var actives = mgr.ActiveUnits;
        if (actives == null) return false;

        float half = Defines.UnitSize * 0.5f;
        Rect spawnRect = new Rect(center.x - half, center.y - half, Defines.UnitSize, Defines.UnitSize);

        for (int i = 0; i < actives.Count; i++)
        {
            UnitBase other = actives[i];
            if (other == null || !other.gameObject.activeSelf) continue;
            if (spawnRect.Overlaps(other.UnitRect)) return true;
        }
        return false;
    }
}

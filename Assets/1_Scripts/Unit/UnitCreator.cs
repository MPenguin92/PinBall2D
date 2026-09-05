using UnityEngine;

/// <summary>
/// 默认单位生成器实现：
/// - 普通怪（unit_damage，吃伤害型）：每个 Step 在屏幕外生成一批，
///   总数与等级均由难度表驱动（刷怪密度百分比 + 等级权重）；
/// - 金币怪（unit_gold）**混入普通波**：金币冷却就绪（GameLogicManager 计时），
///   本波会把随机 1~2 只原本的普通怪替换成金币怪（等级跟随当前难度最高等级）；
/// - 宝箱怪（unit_chest）**混入普通波**：经验里程碑达成（GameLogicManager 置标记），
///   本波替换一只（顺序靠后：随机到金币位会顶掉金币怪），击杀获得一次升级机会。
/// 怪的类型（unitId → prefab 地址）都查 <see cref="UnitTable"/>，id 常量见 <see cref="Defines"/>。
/// 纯逻辑类，由 GameLogicManager 在 Awake 时 new 一次并持有，OnDestroy 时 Dispose。
/// </summary>
public class UnitCreator : IUnitCreator
{
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

    public void SpawnStep(bool allowGoldReplace, bool allowChestReplace)
    {
        if (!isRunning || isPaused) return;
        SpawnBatch(allowGoldReplace, allowChestReplace);
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
    /// 普通怪一批：本波数量 = 难度阶段密度百分比（spawnFillMin~Max）换算到屏幕列数，
    /// 随机不重复列放置；每只的等级由难度阶段等级权重独立 roll。
    /// 金币就绪（allowGoldReplace）：随机 1~2 个位置标为金币怪（等级取难度最高级）；
    /// 宝箱就绪（allowChestReplace）：再随机挑 1 个位置标为宝箱怪（顺序靠后，
    /// 可能随机到金币位并顶掉它）。
    /// </summary>
    private void SpawnBatch(bool allowGoldReplace, bool allowChestReplace)
    {
        GameLogicManager mgr = GameLogicManager.Instance;
        if (mgr == null || mgr.UnitTable == null || mgr.Difficulty == null) return;

        UnitDefinition damage = mgr.UnitTable.Get(Defines.UnitDamageId);
        if (damage == null) return;

        if (!TryResolveSpawnGrid(out int columnCount, out float gridStartX, out float y)) return;

        // 本波密度：难度阶段给百分比区间，换算成「屏幕可容纳列数」的比例。
        (int minPct, int maxPct) pct = mgr.Difficulty.GetSpawnFillRange();
        int fillPct = Random.Range(pct.minPct, pct.maxPct + 1);
        int spawnCount = Mathf.Clamp(
            Mathf.Max(1, Mathf.RoundToInt(columnCount * fillPct / 100f)),
            1, columnCount);

        int[] columns = ShuffledColumns(columnCount);

        // 金币替换位：columns[0, goldCount) 对应的本波位置改为金币怪。
        UnitDefinition gold = allowGoldReplace ? mgr.UnitTable.Get(Defines.UnitGoldId) : null;
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

        // 宝箱替换位：整段随机挑 1 个下标（可能落在金币区 → 顶掉该金币）。
        // 宝箱替换位：整段随机挑 1 个下标（可能落在金币区 → 顶掉该金币）。
        UnitDefinition chest = allowChestReplace ? mgr.UnitTable.Get(Defines.UnitChestId) : null;
        int chestIndex = -1;
        int chestLevel = 1;
        if (chest != null && spawnCount >= 1)
        {
            chestIndex = Random.Range(0, spawnCount);
            chestLevel = Mathf.Clamp(mgr.Difficulty.GetStageMaxLevel(), 1, Mathf.Max(1, chest.levels.Count));
        }
        else if (allowChestReplace)
        {
            // 里程碑已触发但表里查不到 unit_chest：通常是 Units.csv 改后未重新导入 UnitTable。
            Debug.LogError($"[UnitCreator] 本波应刷宝箱怪，但 UnitTable 查不到 '{Defines.UnitChestId}'。" +
                           "请通过 Tools/Data/Import Units 重新导入 Units.csv。");
        }

        for (int i = 0; i < spawnCount; i++)
        {
            float x = gridStartX + (columns[i] + 0.5f) * Defines.UnitSize;
            Vector2 spawnPos = new Vector2(x, y);
            if (IsSpawnOccupied(spawnPos)) continue;

            if (i == chestIndex)
            {
                mgr.SpawnUnit(chest.prefabAddress, spawnPos, chestLevel);
            }
            else if (i < goldCount)
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

using System;
using UnityEngine;

/// <summary>
/// 默认单位生成器实现：在游戏运行中，以固定间隔从屏幕上方（左右 X 随机）生成 Unit。
/// 纯逻辑类，由 GameLogicManager 直接 new 并按帧驱动 Tick。
/// 自身订阅 <see cref="GameEvents"/> 响应游戏生命周期。
/// </summary>
public class UnitCreator : IUnitCreator, IDisposable
{
    private const float SpawnInterval = 2f;
    private const float InitialDelay = 0f;
    private const float HorizontalPadding = 0.5f;
    private const float TopOffset = 0f;

    private float spawnTimer;
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

    public void Tick()
    {
        if (!isRunning || isPaused) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f) return;

        SpawnOne();
        spawnTimer = SpawnInterval;
    }

    private void HandleGameStart()
    {
        spawnTimer = InitialDelay;
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

    private void SpawnOne()
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
        if (maxX < minX)
        {
            float mid = (minX + maxX) * 0.5f;
            minX = mid;
            maxX = mid;
        }

        float x = UnityEngine.Random.Range(minX, maxX);
        float y = camPos.y + halfHeight - TopOffset;

        mgr.SpawnUnit(new Vector2(x, y));
    }
}

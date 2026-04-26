using UnityEngine;

/// <summary>
/// UI 管理器：持有场景中各 UI 根节点引用，监听游戏生命周期事件驱动 UI 显隐。
/// 单例，挂到场景的一个独立 GameObject 上即可。
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Roots")]
    [SerializeField]
    [Tooltip("开始界面 UI 根节点（准备/回到主页时显示）")]
    private GameObject startScreenUI;

    [SerializeField]
    [Tooltip("游戏结束界面 UI 根节点（Player 死亡时显示）")]
    private GameObject gameOverUI;

    [SerializeField]
    [Tooltip("游戏中 HUD 根节点（显示生命值与弹珠数量）")]
    private GameObject inGameUI;

    private void Awake()
    {
        Instance = this;

        GameEvents.OnGameStart += HandleGameStart;
        GameEvents.OnGameEnd += HandleGameEnd;
        GameEvents.OnReturnToHome += HandleReturnToHome;
    }

    private void OnDestroy()
    {
        GameEvents.OnGameStart -= HandleGameStart;
        GameEvents.OnGameEnd -= HandleGameEnd;
        GameEvents.OnReturnToHome -= HandleReturnToHome;

        if (Instance == this)
            Instance = null;
    }

    private void HandleGameStart()
    {
        if (startScreenUI != null) startScreenUI.SetActive(false);
        if (gameOverUI != null) gameOverUI.SetActive(false);
        if (inGameUI != null) inGameUI.SetActive(true);
    }

    private void HandleGameEnd()
    {
        if (inGameUI != null) inGameUI.SetActive(false);
        if (gameOverUI != null) gameOverUI.SetActive(true);
    }

    private void HandleReturnToHome()
    {
        if (inGameUI != null) inGameUI.SetActive(false);
        if (gameOverUI != null) gameOverUI.SetActive(false);
        if (startScreenUI != null) startScreenUI.SetActive(true);
    }
}

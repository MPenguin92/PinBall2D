using UnityEngine;

/// <summary>
/// UI 管理器：持有场景中各 UI 根节点引用，监听游戏生命周期事件驱动 UI 显隐。
/// 单例，挂到场景的一个独立 GameObject 上即可。
///
/// Roguelike 升级面板：UpgradeSelectionUI 自己监听 OnUpgradeOffered/OnUpgradeApplied 显隐，
/// 这里只负责在 GameStart 时把可能残留的升级面板关掉，避免上一局的 UI 漏到新一局。
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

    [SerializeField]
    [Tooltip("Roguelike 三选一升级面板根节点（默认隐藏，由 OnUpgradeOffered 显示）")]
    private GameObject upgradeSelectionUI;

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
        HideUpgradePanel();
    }

    private void HandleGameEnd()
    {
        if (inGameUI != null) inGameUI.SetActive(false);
        if (gameOverUI != null) gameOverUI.SetActive(true);
        HideUpgradePanel();
    }

    private void HandleReturnToHome()
    {
        if (inGameUI != null) inGameUI.SetActive(false);
        if (gameOverUI != null) gameOverUI.SetActive(false);
        if (startScreenUI != null) startScreenUI.SetActive(true);
        HideUpgradePanel();
    }

    private void HideUpgradePanel()
    {
        if (upgradeSelectionUI == null) return;
        UpgradeSelectionUI ui = upgradeSelectionUI.GetComponent<UpgradeSelectionUI>();
        if (ui != null)
            ui.HidePanel();
    }
}

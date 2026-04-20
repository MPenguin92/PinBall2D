using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 游戏结束界面 UI，挂载在 GameOverScreen.prefab 根节点上。
/// 提供「重新开始」与「回到主页」两个按钮。
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [SerializeField]
    private Button restartButton;

    [SerializeField]
    private Button homeButton;

    private void Awake()
    {
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);
        if (homeButton != null)
            homeButton.onClick.AddListener(OnHomeClicked);
    }

    private void OnRestartClicked()
    {
        gameObject.SetActive(false);

        if (GameLogicManager.Instance != null)
            GameLogicManager.Instance.RestartGame();
    }

    private void OnHomeClicked()
    {
        gameObject.SetActive(false);

        if (GameLogicManager.Instance != null)
            GameLogicManager.Instance.BackToHome();
    }
}

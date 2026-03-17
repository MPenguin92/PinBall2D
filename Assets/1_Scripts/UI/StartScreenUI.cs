using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 开始界面 UI，挂载在 StartScreen.prefab 根节点上。
/// 点击「开始」按钮后隐藏本界面并调用 GameLogicManager.StartGame() 开始游戏。
/// </summary>
public class StartScreenUI : MonoBehaviour
{
    [SerializeField]
    private Button startButton;

    private void Awake()
    {
        if (startButton != null)
            startButton.onClick.AddListener(OnStartClicked);
    }

    private void OnStartClicked()
    {
        gameObject.SetActive(false);

        if (GameLogicManager.Instance != null)
            GameLogicManager.Instance.StartGame();
    }
}

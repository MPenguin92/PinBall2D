using System.Collections.Generic;
using UnityEngine;

public class GameLogicManager : MonoBehaviour
{
    public static GameLogicManager Instance { get; private set; }

    private Border[] borders;

    private readonly List<PinBallBase> pinBalls = new List<PinBallBase>();

    public Border[] Borders => borders;

    public IReadOnlyList<PinBallBase> PinBalls => pinBalls;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        StartGame();
    }

    private void Update()
    {
        UpdateGame();
    }

    public void StartGame()
    {
        borders = Object.FindObjectsByType<Border>(FindObjectsSortMode.None);

        PinBallBase[] existingPinBalls = Object.FindObjectsByType<PinBallBase>(FindObjectsSortMode.None);
        pinBalls.Clear();
        for (int i = 0; i < existingPinBalls.Length; i++)
        {
            pinBalls.Add(existingPinBalls[i]);
        }
    }

    public void UpdateGame()
    {
        if (borders == null)
        {
            return;
        }

        for (int i = 0; i < pinBalls.Count; i++)
        {
            PinBallBase pinBall = pinBalls[i];
            if (pinBall == null)
            {
                continue;
            }

            pinBall.Tick(borders);
        }
    }

    public void RegisterPinBall(PinBallBase pinBall)
    {
        if (pinBall == null || pinBalls.Contains(pinBall))
        {
            return;
        }

        pinBalls.Add(pinBall);
    }

    public void UnregisterPinBall(PinBallBase pinBall)
    {
        if (pinBall == null)
        {
            return;
        }

        pinBalls.Remove(pinBall);
    }
}

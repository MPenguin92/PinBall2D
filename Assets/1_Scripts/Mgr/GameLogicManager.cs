using System.Collections.Generic;
using UnityEngine;

public class GameLogicManager : MonoBehaviour
{
    public static GameLogicManager Instance { get; private set; }

    [Header("References")]
    [SerializeField]
    private Player player;

    [SerializeField]
    private PlayerRender playerRender;

    [SerializeField]
    private PoolManager poolManager;

    private Border[] borders;

    public Border[] Borders => borders;

    public IReadOnlyList<PinBallBase> ActivePinBalls => poolManager != null ? poolManager.ActivePinBalls : null;

    public IReadOnlyList<UnitBase> ActiveUnits => poolManager != null ? poolManager.ActiveUnits : null;

    public Player Player => player;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
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
        borders = FindObjectsByType<Border>(FindObjectsSortMode.None);

        if (player != null)
            player.Init();

        if (poolManager != null)
        {
            poolManager.ClearActivePinBalls();
            poolManager.ClearActiveUnits();

            UnitBase[] existingUnits = FindObjectsByType<UnitBase>(FindObjectsSortMode.None);
            for (int i = 0; i < existingUnits.Length; i++)
            {
                existingUnits[i].Init();
                poolManager.RegisterExistingUnit(existingUnits[i]);
            }
        }
    }

    public void UpdateGame()
    {
        if (borders == null) return;

        IReadOnlyList<UnitBase> activeUnits = poolManager != null ? poolManager.ActiveUnits : null;

        for (int i = 0; i < borders.Length; i++)
        {
            if (borders[i] != null)
                borders[i].RefreshRect();
        }

        if (activeUnits != null)
        {
            for (int i = 0; i < activeUnits.Count; i++)
            {
                if (activeUnits[i] != null)
                    activeUnits[i].RefreshRect();
            }
        }

        if (player != null)
            player.Tick();

        if (poolManager != null)
        {
            IReadOnlyList<PinBallBase> activePinBalls = poolManager.ActivePinBalls;
            for (int i = activePinBalls.Count - 1; i >= 0; i--)
            {
                if (i >= activePinBalls.Count) continue;
                PinBallBase pb = activePinBalls[i];
                if (pb == null || !pb.gameObject.activeSelf) continue;
                pb.Tick(borders, activeUnits);
            }

            for (int i = activeUnits.Count - 1; i >= 0; i--)
            {
                if (i >= activeUnits.Count) continue;
                UnitBase unit = activeUnits[i];
                if (unit == null || !unit.gameObject.activeSelf) continue;
                unit.Tick();
            }
        }

        if (playerRender != null)
            playerRender.Tick();
    }

    public PinBallBase SpawnPinBall(Vector2 position, Vector2 direction, float speed)
    {
        if (poolManager == null) return null;
        return poolManager.SpawnPinBall(position, direction, speed);
    }

    public void RecyclePinBall(PinBallBase pb)
    {
        if (poolManager != null)
            poolManager.RecyclePinBall(pb);

        if (player != null)
            player.AddPinBall();
    }

    public UnitBase SpawnUnit(Vector2 position)
    {
        if (poolManager == null) return null;
        return poolManager.SpawnUnit(position);
    }

    public void RecycleUnit(UnitBase unit)
    {
        if (poolManager != null)
            poolManager.RecycleUnit(unit);
    }
}

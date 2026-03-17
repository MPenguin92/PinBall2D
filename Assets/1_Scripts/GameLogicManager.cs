using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class GameLogicManager : MonoBehaviour
{
    public static GameLogicManager Instance { get; private set; }

    [Header("References")]
    [SerializeField]
    private Player player;

    [SerializeField]
    private PlayerRender playerRender;

    [Header("Prefabs")]
    [SerializeField]
    private PinBallBase pinBallPrefab;

    [SerializeField]
    private UnitBase unitPrefab;

    [Header("Pool Settings")]
    [SerializeField]
    private Transform pinBallPoolRoot;

    [SerializeField]
    private Transform unitPoolRoot;

    [SerializeField]
    private int pinBallPoolDefaultCapacity = 20;

    [SerializeField]
    private int pinBallPoolMaxSize = 50;

    [SerializeField]
    private int unitPoolDefaultCapacity = 20;

    [SerializeField]
    private int unitPoolMaxSize = 100;

    private Border[] borders;
    private readonly List<PinBallBase> activePinBalls = new List<PinBallBase>();
    private readonly List<UnitBase> activeUnits = new List<UnitBase>();

    private ObjectPool<PinBallBase> pinBallPool;
    private ObjectPool<UnitBase> unitPool;

    public Border[] Borders => borders;

    public IReadOnlyList<PinBallBase> ActivePinBalls => activePinBalls;

    public IReadOnlyList<UnitBase> ActiveUnits => activeUnits;

    public Player Player => player;

    private void Awake()
    {
        Instance = this;
        InitPools();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        pinBallPool?.Dispose();
        unitPool?.Dispose();
    }

    private void Start()
    {
        StartGame();
    }

    private void Update()
    {
        UpdateGame();
    }

    private void InitPools()
    {
        if (pinBallPoolRoot == null)
        {
            pinBallPoolRoot = new GameObject("PinBallPool").transform;
            pinBallPoolRoot.SetParent(transform);
        }

        if (unitPoolRoot == null)
        {
            unitPoolRoot = new GameObject("UnitPool").transform;
            unitPoolRoot.SetParent(transform);
        }

        pinBallPool = new ObjectPool<PinBallBase>(
            createFunc: () =>
            {
                PinBallBase pb = Instantiate(pinBallPrefab, pinBallPoolRoot);
                pb.gameObject.SetActive(false);
                return pb;
            },
            actionOnGet: pb =>
            {
                pb.transform.SetParent(null);
                pb.gameObject.SetActive(true);
            },
            actionOnRelease: pb =>
            {
                pb.gameObject.SetActive(false);
                pb.transform.SetParent(pinBallPoolRoot);
            },
            actionOnDestroy: pb => Destroy(pb.gameObject),
            defaultCapacity: pinBallPoolDefaultCapacity,
            maxSize: pinBallPoolMaxSize
        );

        unitPool = new ObjectPool<UnitBase>(
            createFunc: () =>
            {
                UnitBase u = Instantiate(unitPrefab, unitPoolRoot);
                u.gameObject.SetActive(false);
                return u;
            },
            actionOnGet: u =>
            {
                u.transform.SetParent(null);
                u.gameObject.SetActive(true);
            },
            actionOnRelease: u =>
            {
                u.gameObject.SetActive(false);
                u.transform.SetParent(unitPoolRoot);
            },
            actionOnDestroy: u => Destroy(u.gameObject),
            defaultCapacity: unitPoolDefaultCapacity,
            maxSize: unitPoolMaxSize
        );
    }

    public void StartGame()
    {
        borders = FindObjectsByType<Border>(FindObjectsSortMode.None);

        if (player != null)
            player.Init();

        activePinBalls.Clear();

        activeUnits.Clear();
        UnitBase[] existingUnits = FindObjectsByType<UnitBase>(FindObjectsSortMode.None);
        for (int i = 0; i < existingUnits.Length; i++)
        {
            existingUnits[i].Init();
            activeUnits.Add(existingUnits[i]);
        }
    }

    public void UpdateGame()
    {
        if (borders == null) return;

        for (int i = 0; i < borders.Length; i++)
        {
            if (borders[i] != null)
                borders[i].RefreshRect();
        }

        for (int i = 0; i < activeUnits.Count; i++)
        {
            if (activeUnits[i] != null)
                activeUnits[i].RefreshRect();
        }

        if (player != null)
            player.Tick();

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

        if (playerRender != null)
            playerRender.Tick();
    }

    public PinBallBase SpawnPinBall(Vector2 position, Vector2 direction, float speed)
    {
        PinBallBase pb = pinBallPool.Get();
        pb.transform.position = new Vector3(position.x, position.y, 0f);
        pb.Init(direction, speed);
        activePinBalls.Add(pb);
        return pb;
    }

    public void RecyclePinBall(PinBallBase pb)
    {
        activePinBalls.Remove(pb);
        pinBallPool.Release(pb);

        if (player != null)
            player.AddPinBall();
    }

    public UnitBase SpawnUnit(Vector2 position)
    {
        UnitBase unit = unitPool.Get();
        unit.transform.position = new Vector3(position.x, position.y, 0f);
        unit.Init();
        activeUnits.Add(unit);
        return unit;
    }

    public void RecycleUnit(UnitBase unit)
    {
        activeUnits.Remove(unit);
        unitPool.Release(unit);
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class PoolManager : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField]
    private PinBallBase pinBallPrefab;

    [SerializeField]
    private UnitBase unitPrefab;

    [Header("Pool Roots")]
    [SerializeField]
    private Transform pinBallPoolRoot;

    [SerializeField]
    private Transform unitPoolRoot;

    [Header("Active Roots")]
    [SerializeField]
    private Transform pinBallSpawnRoot;

    [SerializeField]
    private Transform unitSpawnRoot;

    [Header("PinBall Pool")]
    [SerializeField]
    private int pinBallPoolDefaultCapacity = 20;

    [SerializeField]
    private int pinBallPoolMaxSize = 50;

    [Header("Unit Pool")]
    [SerializeField]
    private int unitPoolDefaultCapacity = 20;

    [SerializeField]
    private int unitPoolMaxSize = 100;

    private readonly List<PinBallBase> activePinBalls = new List<PinBallBase>();
    private readonly List<UnitBase> activeUnits = new List<UnitBase>();

    private ObjectPool<PinBallBase> pinBallPool;
    private ObjectPool<UnitBase> unitPool;

    public IReadOnlyList<PinBallBase> ActivePinBalls => activePinBalls;

    public IReadOnlyList<UnitBase> ActiveUnits => activeUnits;

    private void Awake()
    {
        InitPools();
    }

    private void OnDestroy()
    {
        pinBallPool?.Dispose();
        unitPool?.Dispose();
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

        if (pinBallSpawnRoot == null)
        {
            pinBallSpawnRoot = new GameObject("PinBallSpawnRoot").transform;
            pinBallSpawnRoot.SetParent(transform);
        }

        if (unitSpawnRoot == null)
        {
            unitSpawnRoot = new GameObject("UnitSpawnRoot").transform;
            unitSpawnRoot.SetParent(transform);
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
                pb.transform.SetParent(pinBallSpawnRoot);
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
                u.transform.SetParent(unitSpawnRoot);
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

    public void ClearActivePinBalls()
    {
        for (int i = activePinBalls.Count - 1; i >= 0; i--)
        {
            PinBallBase pinBall = activePinBalls[i];
            activePinBalls.RemoveAt(i);

            if (pinBall != null)
                pinBallPool.Release(pinBall);
        }
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
    }

    public void ClearActiveUnits()
    {
        for (int i = activeUnits.Count - 1; i >= 0; i--)
        {
            UnitBase unit = activeUnits[i];
            activeUnits.RemoveAt(i);

            if (unit != null)
                unitPool.Release(unit);
        }
    }

    public void RegisterExistingUnit(UnitBase unit)
    {
        if (unit != null && !activeUnits.Contains(unit))
            activeUnits.Add(unit);
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

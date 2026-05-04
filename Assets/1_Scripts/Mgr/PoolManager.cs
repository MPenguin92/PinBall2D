using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class PoolManager : MonoBehaviour
{
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

    private readonly Dictionary<string, ObjectPool<PinBallBase>> pinBallPools = new Dictionary<string, ObjectPool<PinBallBase>>();
    private readonly Dictionary<string, ObjectPool<UnitBase>> unitPools = new Dictionary<string, ObjectPool<UnitBase>>();
    private readonly Dictionary<PinBallBase, ObjectPool<PinBallBase>> pinBallPoolByInstance = new Dictionary<PinBallBase, ObjectPool<PinBallBase>>();
    private readonly Dictionary<UnitBase, ObjectPool<UnitBase>> unitPoolByInstance = new Dictionary<UnitBase, ObjectPool<UnitBase>>();

    public IReadOnlyList<PinBallBase> ActivePinBalls => activePinBalls;

    public IReadOnlyList<UnitBase> ActiveUnits => activeUnits;

    private void Awake()
    {
        InitPools();
    }

    private void OnDestroy()
    {
        foreach (ObjectPool<PinBallBase> pool in pinBallPools.Values)
            pool.Dispose();
        foreach (ObjectPool<UnitBase> pool in unitPools.Values)
            pool.Dispose();
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

    }

    private ObjectPool<PinBallBase> GetOrCreatePinBallPool(string address)
    {
        if (string.IsNullOrEmpty(address))
        {
            Debug.LogError("[PoolManager] PinBall address is null or empty.");
            return null;
        }

        if (pinBallPools.TryGetValue(address, out ObjectPool<PinBallBase> pool))
            return pool;

        PinBallBase prefab = LoadPrefabComponent<PinBallBase>(address);
        if (prefab == null)
            return null;

        pool = new ObjectPool<PinBallBase>(
            createFunc: () =>
            {
                PinBallBase pb = Instantiate(prefab, pinBallPoolRoot);
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

        pinBallPools.Add(address, pool);
        return pool;
    }

    private ObjectPool<UnitBase> GetOrCreateUnitPool(string address)
    {
        if (string.IsNullOrEmpty(address))
        {
            Debug.LogError("[PoolManager] Unit address is null or empty.");
            return null;
        }

        if (unitPools.TryGetValue(address, out ObjectPool<UnitBase> pool))
            return pool;

        UnitBase prefab = LoadPrefabComponent<UnitBase>(address);
        if (prefab == null)
            return null;

        pool = new ObjectPool<UnitBase>(
            createFunc: () =>
            {
                UnitBase u = Instantiate(prefab, unitPoolRoot);
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

        unitPools.Add(address, pool);
        return pool;
    }

    private static T LoadPrefabComponent<T>(string address) where T : Component
    {
        GameObject prefab = AssetLoader.Load<GameObject>(address);
        if (prefab == null)
            return null;

        T component = prefab.GetComponent<T>();
        if (component == null)
            Debug.LogError($"[PoolManager] Addressable prefab '{address}' does not have component {typeof(T).Name}.");

        return component;
    }

    public void ClearActivePinBalls()
    {
        for (int i = activePinBalls.Count - 1; i >= 0; i--)
        {
            PinBallBase pinBall = activePinBalls[i];
            activePinBalls.RemoveAt(i);

            if (pinBall != null)
                ReleasePinBall(pinBall);
        }
    }

    public PinBallBase SpawnPinBall(string address, Vector2 position, Vector2 direction, float speed)
    {
        ObjectPool<PinBallBase> pool = GetOrCreatePinBallPool(address);
        if (pool == null)
            return null;

        PinBallBase pb = pool.Get();
        pb.transform.position = new Vector3(position.x, position.y, 0f);
        pb.Init(direction, speed);
        activePinBalls.Add(pb);
        pinBallPoolByInstance[pb] = pool;
        return pb;
    }

    public void RecyclePinBall(PinBallBase pb)
    {
        activePinBalls.Remove(pb);
        ReleasePinBall(pb);
    }

    public void ClearActiveUnits()
    {
        for (int i = activeUnits.Count - 1; i >= 0; i--)
        {
            UnitBase unit = activeUnits[i];
            activeUnits.RemoveAt(i);

            if (unit != null)
                ReleaseUnit(unit);
        }
    }

    public void RegisterExistingUnit(UnitBase unit)
    {
        if (unit != null && !activeUnits.Contains(unit))
            activeUnits.Add(unit);
    }

    public UnitBase SpawnUnit(string address, Vector2 position)
    {
        ObjectPool<UnitBase> pool = GetOrCreateUnitPool(address);
        if (pool == null)
            return null;

        UnitBase unit = pool.Get();
        unit.transform.position = new Vector3(position.x, position.y, 0f);
        unit.Init();
        activeUnits.Add(unit);
        unitPoolByInstance[unit] = pool;
        return unit;
    }

    public void RecycleUnit(UnitBase unit)
    {
        activeUnits.Remove(unit);
        ReleaseUnit(unit);
    }

    private void ReleasePinBall(PinBallBase pinBall)
    {
        if (pinBallPoolByInstance.TryGetValue(pinBall, out ObjectPool<PinBallBase> pool))
        {
            pinBallPoolByInstance.Remove(pinBall);
            pool.Release(pinBall);
            return;
        }

        Debug.LogWarning($"[PoolManager] PinBall '{pinBall.name}' was not spawned from an addressable pool.");
        pinBall.gameObject.SetActive(false);
        pinBall.transform.SetParent(pinBallPoolRoot);
    }

    private void ReleaseUnit(UnitBase unit)
    {
        if (unitPoolByInstance.TryGetValue(unit, out ObjectPool<UnitBase> pool))
        {
            unitPoolByInstance.Remove(unit);
            pool.Release(unit);
            return;
        }

        Debug.LogWarning($"[PoolManager] Unit '{unit.name}' was not spawned from an addressable pool.");
        unit.gameObject.SetActive(false);
        unit.transform.SetParent(unitPoolRoot);
    }
}

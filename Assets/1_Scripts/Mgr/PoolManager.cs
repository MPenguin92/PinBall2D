using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class PoolManager : MonoBehaviour
{
    // —— 休眠节点：对象回池后挂在这里（失活）——
    [Header("休眠根节点")]
    [SerializeField]
    private Transform pinBallPoolRoot;   // 弹球休眠父节点

    [SerializeField]
    private Transform unitPoolRoot;      // 单位休眠父节点

    [SerializeField]
    private Transform vfxPoolRoot;       // 特效休眠父节点

    // —— 出场节点：从池取出后挂在这里（激活）——
    [Header("出场根节点")]
    [SerializeField]
    private Transform pinBallSpawnRoot;  // 弹球出场父节点

    [SerializeField]
    private Transform unitSpawnRoot;     // 单位出场父节点

    [SerializeField]
    private Transform vfxSpawnRoot;      // 特效出场父节点

    // —— 各池容量：DefaultCapacity=预热数，MaxSize=池上限（超出则销毁）——
    [Header("弹球池容量")]
    [SerializeField]
    private int pinBallPoolDefaultCapacity = 20;

    [SerializeField]
    private int pinBallPoolMaxSize = 50;

    [Header("单位池容量")]
    [SerializeField]
    private int unitPoolDefaultCapacity = 20;

    [SerializeField]
    private int unitPoolMaxSize = 100;

    [Header("特效池容量")]
    [SerializeField]
    private int vfxPoolDefaultCapacity = 16;

    [SerializeField]
    private int vfxPoolMaxSize = 64;

    [SerializeField]
    [Tooltip("无法从 ParticleSystem 推断时长时的兜底回收时间（秒）。")]
    private float vfxFallbackLifetime = 2f;

    private readonly List<PinBallBase> activePinBalls = new List<PinBallBase>();
    private readonly List<UnitBase> activeUnits = new List<UnitBase>();
    private readonly List<GameObject> activeVfx = new List<GameObject>();

    private readonly Dictionary<string, ObjectPool<PinBallBase>> pinBallPools = new Dictionary<string, ObjectPool<PinBallBase>>();
    private readonly Dictionary<string, ObjectPool<UnitBase>> unitPools = new Dictionary<string, ObjectPool<UnitBase>>();
    private readonly Dictionary<string, ObjectPool<GameObject>> vfxPools = new Dictionary<string, ObjectPool<GameObject>>();
    private readonly Dictionary<PinBallBase, ObjectPool<PinBallBase>> pinBallPoolByInstance = new Dictionary<PinBallBase, ObjectPool<PinBallBase>>();
    private readonly Dictionary<UnitBase, ObjectPool<UnitBase>> unitPoolByInstance = new Dictionary<UnitBase, ObjectPool<UnitBase>>();
    private readonly Dictionary<GameObject, ObjectPool<GameObject>> vfxPoolByInstance = new Dictionary<GameObject, ObjectPool<GameObject>>();

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
        foreach (ObjectPool<GameObject> pool in vfxPools.Values)
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

        if (vfxPoolRoot == null)
        {
            vfxPoolRoot = new GameObject("VfxPool").transform;
            vfxPoolRoot.SetParent(transform);
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

        if (vfxSpawnRoot == null)
        {
            vfxSpawnRoot = new GameObject("VfxSpawnRoot").transform;
            vfxSpawnRoot.SetParent(transform);
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

    private ObjectPool<GameObject> GetOrCreateVfxPool(string address)
    {
        if (string.IsNullOrEmpty(address))
        {
            Debug.LogError("[PoolManager] VFX address is null or empty.");
            return null;
        }

        if (vfxPools.TryGetValue(address, out ObjectPool<GameObject> pool))
            return pool;

        GameObject prefab = AssetLoader.Load<GameObject>(address);
        if (prefab == null)
            return null;

        pool = new ObjectPool<GameObject>(
            createFunc: () =>
            {
                GameObject go = Instantiate(prefab, vfxPoolRoot);
                go.SetActive(false);
                return go;
            },
            actionOnGet: go =>
            {
                go.transform.SetParent(vfxSpawnRoot);
                go.SetActive(true);
                RestartParticleSystems(go);
            },
            actionOnRelease: go =>
            {
                StopAndClearParticleSystems(go);
                go.SetActive(false);
                go.transform.SetParent(vfxPoolRoot);
            },
            actionOnDestroy: go => Destroy(go),
            defaultCapacity: vfxPoolDefaultCapacity,
            maxSize: vfxPoolMaxSize
        );

        vfxPools.Add(address, pool);
        return pool;
    }

    /// <summary>
    /// 从对象池取出 VFX，放到目标位置；播完后自动回收。
    /// 也可手动调用 <see cref="RecycleVfx"/>。
    /// </summary>
    public GameObject SpawnVfx(string address, Vector2 position, float lifetime = -1f)
    {
        if (vfxPoolRoot == null || vfxSpawnRoot == null)
            InitPools();

        ObjectPool<GameObject> pool = GetOrCreateVfxPool(address);
        if (pool == null)
            return null;

        GameObject go = pool.Get();
        go.transform.SetPositionAndRotation(
            new Vector3(position.x, position.y, 0f),
            Quaternion.identity);

        activeVfx.Add(go);
        vfxPoolByInstance[go] = pool;

        float recycleAfter = lifetime > 0f ? lifetime : EstimateVfxLifetime(go);
        StartCoroutine(RecycleVfxAfter(go, recycleAfter));
        return go;
    }

    public void RecycleVfx(GameObject vfx)
    {
        if (vfx == null)
            return;

        activeVfx.Remove(vfx);
        ReleaseVfx(vfx);
    }

    public void ClearActiveVfx()
    {
        StopAllCoroutines();

        for (int i = activeVfx.Count - 1; i >= 0; i--)
        {
            GameObject vfx = activeVfx[i];
            activeVfx.RemoveAt(i);

            if (vfx != null)
                ReleaseVfx(vfx);
        }
    }

    private void ReleaseVfx(GameObject vfx)
    {
        if (vfxPoolByInstance.TryGetValue(vfx, out ObjectPool<GameObject> pool))
        {
            vfxPoolByInstance.Remove(vfx);
            pool.Release(vfx);
            return;
        }

        Debug.LogWarning($"[PoolManager] VFX '{vfx.name}' was not spawned from an addressable pool.");
        vfx.SetActive(false);
        vfx.transform.SetParent(vfxPoolRoot);
    }

    private IEnumerator RecycleVfxAfter(GameObject vfx, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (vfx == null)
            yield break;

        // 已被 Clear / 手动回收则跳过。
        if (!vfxPoolByInstance.ContainsKey(vfx))
            yield break;

        RecycleVfx(vfx);
    }

    private float EstimateVfxLifetime(GameObject go)
    {
        float max = 0f;
        ParticleSystem[] systems = go.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            ParticleSystem.MainModule main = systems[i].main;
            float life = main.startLifetime.constantMax;
            if (main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants)
                life = Mathf.Max(main.startLifetime.constantMin, main.startLifetime.constantMax);

            float duration = main.loop ? vfxFallbackLifetime : main.duration + life;
            if (duration > max)
                max = duration;
        }

        return max > 0f ? max + 0.15f : vfxFallbackLifetime;
    }

    private static void RestartParticleSystems(GameObject go)
    {
        ParticleSystem[] systems = go.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            systems[i].Clear(true);
            systems[i].Play(true);
        }
    }

    private static void StopAndClearParticleSystems(GameObject go)
    {
        ParticleSystem[] systems = go.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
            systems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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

        PinBallRender render = pb.GetComponent<PinBallRender>();
        if (render != null)
            render.ResetTrailAfterSpawn();

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

using UnityEngine;

/// <summary>
/// 运行时通过 Addressables 动态加载 VFX Prefab 并实例化。
/// </summary>
public class VfxSpawner : MonoBehaviour
{
    [SerializeField]
    private Transform vfxRoot;

    private VfxCatalog catalog;

    private void Awake()
    {
        if (vfxRoot == null)
            vfxRoot = transform;

        catalog = AssetLoader.Load<VfxCatalog>("VfxCatalog");
    }

    public void PlayBallHit(BallType type, Vector2 position, bool killed)
    {
        if (catalog == null)
            return;

        string address = killed ? catalog.GetKillAddress(type) : null;
        if (string.IsNullOrEmpty(address))
            address = catalog.GetHitAddress(type);

        if (string.IsNullOrEmpty(address))
            return;

        Spawn(address, position);
    }

    private void Spawn(string address, Vector2 position)
    {
        GameObject prefab = AssetLoader.Load<GameObject>(address);
        if (prefab == null)
            return;

        Instantiate(
            prefab,
            new Vector3(position.x, position.y, 0f),
            Quaternion.identity,
            vfxRoot);
    }
}

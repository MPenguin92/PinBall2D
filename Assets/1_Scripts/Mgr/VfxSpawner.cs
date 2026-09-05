using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

/// <summary>
/// 按 BallType 查 <see cref="VfxCatalog"/> 地址，再通过 <see cref="PoolManager"/> 出池播放。
/// 击碎碎块走独立地址 VFX/UnitShatter，双色由调用方注入。
/// </summary>
public class VfxSpawner
{
    private const string ShatterAddress = "VFX/UnitShatter";

    private readonly PoolManager poolManager;
    private readonly VfxCatalog catalog;
    private bool? shatterAddressReady;

    public VfxSpawner(PoolManager poolManager)
    {
        this.poolManager = poolManager;
        catalog = AssetLoader.Load<VfxCatalog>("VfxCatalog");
    }

    public void PlayHit(BallType type, Vector2 position)
    {
        Spawn(catalog != null ? catalog.GetHitAddress(type) : null, position);
    }

    public void PlayKill(BallType type, Vector2 position)
    {
        string address = catalog != null ? catalog.GetKillAddress(type) : null;
        if (string.IsNullOrEmpty(address) && catalog != null)
            address = catalog.GetHitAddress(type);
        Spawn(address, position);
    }

    /// <summary>
    /// Unit 击碎：碎块颜色在 unitColor / ballColor 间随机混合。
    /// 需要 Addressable Prefab：VFX/UnitShatter。
    /// </summary>
    public void PlayShatter(Vector2 position, Color unitColor, Color ballColor)
    {
        if (poolManager == null || !IsShatterAddressReady())
            return;

        GameObject go = poolManager.SpawnVfx(ShatterAddress, position, lifetime: 1.2f);
        if (go == null) return;

        UnitShatterDebris debris = go.GetComponent<UnitShatterDebris>();
        if (debris != null)
            debris.Play(unitColor, ballColor);
    }

    private bool IsShatterAddressReady()
    {
        if (shatterAddressReady.HasValue)
            return shatterAddressReady.Value;

        AsyncOperationHandle<IList<IResourceLocation>> handle =
            Addressables.LoadResourceLocationsAsync(ShatterAddress);
        IList<IResourceLocation> locations = handle.WaitForCompletion();
        shatterAddressReady = locations != null && locations.Count > 0;
        Addressables.Release(handle);
        return shatterAddressReady.Value;
    }

    private void Spawn(string address, Vector2 position)
    {
        if (string.IsNullOrEmpty(address) || poolManager == null)
            return;

        poolManager.SpawnVfx(address, position);
    }
}

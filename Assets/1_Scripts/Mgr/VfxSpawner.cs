using UnityEngine;

/// <summary>
/// 按 BallType 查 <see cref="VfxCatalog"/> 地址，再通过 <see cref="PoolManager"/> 出池播放。
/// 由 UnitRender 在受击/死亡时调用，不由 PinBall 直接调。
/// </summary>
public class VfxSpawner
{
    private readonly PoolManager poolManager;
    private readonly VfxCatalog catalog;

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

    private void Spawn(string address, Vector2 position)
    {
        if (string.IsNullOrEmpty(address) || poolManager == null)
            return;

        poolManager.SpawnVfx(address, position);
    }
}

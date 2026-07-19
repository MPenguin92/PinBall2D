using UnityEngine;

/// <summary>
/// 按 BallType 查 <see cref="VfxCatalog"/> 地址，再通过 <see cref="PoolManager"/> 出池播放。
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

    public void PlayBallHit(BallType type, Vector2 position, bool killed)
    {
        if (catalog == null || poolManager == null)
            return;

        string address = killed ? catalog.GetKillAddress(type) : null;
        if (string.IsNullOrEmpty(address))
            address = catalog.GetHitAddress(type);

        if (string.IsNullOrEmpty(address))
            return;

        poolManager.SpawnVfx(address, position);
    }
}

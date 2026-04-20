using UnityEngine;

/// <summary>
/// 最简单的 Unit：创建后以固定速度向屏幕下方移动，
/// 触碰到底边 Border 时自动回收入池。
/// </summary>
public class SimpleUnit : UnitBase
{
    [SerializeField]
    private float moveSpeed = 2f;

    public override void Tick()
    {
        Vector3 pos = transform.position;
        pos.y -= moveSpeed * Time.deltaTime;
        transform.position = pos;
        RefreshRect();

        GameLogicManager mgr = GameLogicManager.Instance;
        if (mgr == null) return;

        Border[] borders = mgr.Borders;
        if (borders == null) return;

        for (int i = 0; i < borders.Length; i++)
        {
            Border b = borders[i];
            if (b == null || !b.IsBottomBorder) continue;

            if (UnitRect.Overlaps(b.BorderRect))
            {
                mgr.OnUnitReachBottom(this);
                return;
            }
        }
    }
}

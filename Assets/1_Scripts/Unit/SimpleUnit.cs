using UnityEngine;

/// <summary>
/// 最简单的 Unit：
/// - 收到 <see cref="GameEvents.OnStep"/> 时开启一次向下移动（距离 <see cref="Defines.StepDistance"/>，
///   时长 <see cref="Defines.StepMoveDuration"/> 秒）；
/// - 移动到达目标位置后，如果与底边 Border 重叠，则调用
///   <see cref="GameLogicManager.OnUnitReachBottom"/> 扣 Player 血并回收入池。
/// </summary>
public class SimpleUnit : UnitBase
{
    private Vector2 moveStart;
    private Vector2 moveTarget;
    private float moveTimer;
    private bool isMoving;

    protected override void OnEnable()
    {
        base.OnEnable();
        isMoving = false;
        moveTimer = 0f;
    }

    protected override void HandleStep()
    {
        // 即使上一轮还没走完也会被新的一轮覆盖（正常配置下 StepInterval 远大于 MoveDuration，
        // 不会发生这种情况）。
        moveStart = transform.position;
        moveTarget = moveStart + Vector2.down * Defines.StepDistance;
        moveTimer = 0f;
        isMoving = true;
    }

    public override void Tick()
    {
        if (!isMoving) return;

        moveTimer += Time.deltaTime;
        float t = Mathf.Clamp01(moveTimer / Defines.StepMoveDuration);
        Vector2 pos = Vector2.Lerp(moveStart, moveTarget, t);
        transform.position = new Vector3(pos.x, pos.y, transform.position.z);
        RefreshRect();

        if (t >= 1f)
        {
            isMoving = false;
            CheckBottomCollision();
        }
    }

    private void CheckBottomCollision()
    {
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

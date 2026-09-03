using UnityEngine;

/// <summary>
/// 扇形策略：一次射击以基准方向为中心，同时向两侧均匀散射
/// <see cref="Count"/> 颗球，总张角 <see cref="SpreadDegrees"/> 度。
/// </summary>
public class FanFireStrategy : FireStrategy
{
    /// <summary>单次射出的球数（至少 1）。</summary>
    public int Count { get; }

    /// <summary>扇形总张角（度，&gt;0；Count==1 时不散射）。</summary>
    public float SpreadDegrees { get; }

    public FanFireStrategy(int count = 3, float spreadDegrees = 30f)
    {
        Count = System.Math.Max(1, count);
        SpreadDegrees = System.Math.Max(0f, spreadDegrees);
    }

    public override void Fire(IFireExecutor executor)
    {
        Vector2 baseDir = executor.BaseDirection;
        int n = Count;

        if (n == 1)
        {
            executor.SpawnBall(baseDir);
            return;
        }

        float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x);
        float spread = SpreadDegrees * Mathf.Deg2Rad;
        float step = spread / (n - 1);
        float startAngle = baseAngle - spread * 0.5f;

        for (int i = 0; i < n; i++)
        {
            float angle = startAngle + step * i;
            executor.SpawnBall(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)));
        }
    }
}

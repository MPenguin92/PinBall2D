using System.Collections.Generic;

/// <summary>
/// 连发策略：一次射击沿基准方向「先后」按顺序发射 <see cref="Shots"/> 中指定的每一颗球，
/// 相邻两颗间隔 <see cref="Interval"/> 秒。Shots 可以是不同球型/等级的混合（如主弹 + 副弹）。
/// </summary>
public class BurstFireStrategy : FireStrategy
{
    /// <summary>本次射击依次发射的弹（每颗可指定不同球型与等级）。</summary>
    public IReadOnlyList<FireShot> Shots { get; }

    /// <summary>相邻两发的时间间隔（秒，&gt;0 即形成先后手感）。</summary>
    public float Interval { get; }

    public BurstFireStrategy(IReadOnlyList<FireShot> shots, float interval = 0.08f)
    {
        Shots = shots ?? new List<FireShot>();
        Interval = System.Math.Max(0f, interval);
    }

    public override void Fire(IFireExecutor executor)
    {
        FireAt(executor, 0);
    }

    /// <summary>发第 index 颗；未发完则延迟 Interval 再发下一颗（利用 executor 的延迟能力）。</summary>
    private void FireAt(IFireExecutor executor, int index)
    {
        if (index >= Shots.Count) return;

        executor.SpawnBall(executor.BaseDirection, Shots[index]);

        int next = index + 1;
        if (next >= Shots.Count) return;
        executor.Delay(Interval, () => FireAt(executor, next));
    }
}

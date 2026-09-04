/// <summary>
/// 连发策略：一次射击沿基准方向「先后」飞出 <see cref="Count"/> 颗球，
/// 相邻两颗间隔 <see cref="Interval"/> 秒（双发 / 三连发均为此类的实例）。
/// </summary>
public class BurstFireStrategy : FireStrategy
{
    /// <summary>本次射击产出的球数（至少 1）。</summary>
    public int Count { get; }

    /// <summary>相邻两发的时间间隔（秒，&gt;0 即形成先后手感）。</summary>
    public float Interval { get; }

    public BurstFireStrategy(int count = 2, float interval = 0.08f)
    {
        Count = System.Math.Max(1, count);
        Interval = System.Math.Max(0f, interval);
    }

    public override void Fire(IFireExecutor executor)
    {
        FireAt(executor, 0);
    }

    /// <summary>发第 index 颗；未发完则延迟 Interval 再发下一颗（利用 executor 的延迟能力）。</summary>
    private void FireAt(IFireExecutor executor, int index)
    {
        executor.SpawnBall(executor.BaseDirection);

        int next = index + 1;
        if (next >= Count) return;
        executor.Delay(Interval, () => FireAt(executor, next));
    }
}

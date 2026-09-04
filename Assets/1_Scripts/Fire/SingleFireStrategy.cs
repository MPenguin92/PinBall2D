/// <summary>
/// 单发策略：一次射击沿基准方向发射 1 颗基础普通弹（默认行为）。
/// </summary>
public class SingleFireStrategy : FireStrategy
{
    public override void Fire(IFireExecutor executor)
    {
        executor.SpawnBall(executor.BaseDirection, FireShot.Base);
    }
}

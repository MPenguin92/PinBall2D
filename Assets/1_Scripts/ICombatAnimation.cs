/// <summary>
/// 战斗表现动画入口，由 Render 层实现。
/// </summary>
public interface ICombatAnimation
{
    void PlayAttackAnimation();

    void PlayHitAnimation();

    void PlayDeathAnimation();
}

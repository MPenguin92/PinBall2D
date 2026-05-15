using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单个等级的参数取值集合：与 <see cref="NewBallUpgradeData.ParamKeys"/> 一一对应。
/// 例如 paramKeys = ["explosionRadius", "explosionDamage"] 时，
/// values = [1.5, 1] 表示该等级 explosionRadius=1.5、explosionDamage=1。
/// </summary>
[Serializable]
public class BallLevelValues
{
    public List<float> values = new List<float>();
}

/// <summary>
/// 新球类升级词条（单实例 + 多级）。
///
/// 设计：每种特殊球（Fire/Ice/...）在玩家库存中**至多一颗**——首次抽到 = 解锁 + 入队 1 颗 + 应用 Lv1 参数；
/// 之后每次再抽到同一条 = 升级一级，**不再入队**，仅把对应等级的参数 Set 到 <see cref="SpecialBallParams"/>
/// 覆盖上一级数值。<see cref="UpgradeBase.MaxStack"/> 即满级（=levelValues.Count，建议 5）。
///
/// 特例：<see cref="ballType"/> = <see cref="BallType.Base"/> 时退化为「+1 普通球入队尾」，
/// 每次 Apply 都入队 1 颗，可堆叠到 maxStack 次；不涉及 paramKeys / levelValues。
/// </summary>
[CreateAssetMenu(fileName = "NewBallUpgrade", menuName = "PinBall2D/Upgrade/NewBallUpgrade", order = 11)]
public class NewBallUpgradeData : UpgradeBase
{
    [SerializeField]
    private BallType ballType = BallType.Fire;

    [SerializeField]
    [Tooltip("写入 SpecialBallParams 的 key 列表（与每一级的 values 一一对应）。")]
    private List<string> paramKeys = new List<string>();

    [SerializeField]
    [Tooltip("逐级参数表：第 N 项对应 Lv(N+1) 的参数取值；表项数量等同 maxStack。")]
    private List<BallLevelValues> levelValues = new List<BallLevelValues>();

    public BallType BallType => ballType;

    public IReadOnlyList<string> ParamKeys => paramKeys;

    public IReadOnlyList<BallLevelValues> LevelValues => levelValues;

    public void SetData(BallType type, List<string> keys, List<BallLevelValues> levels)
    {
        ballType = type;
        paramKeys = keys ?? new List<string>();
        levelValues = levels ?? new List<BallLevelValues>();
    }

    public override void Apply(UpgradeContext ctx)
    {
        if (ctx == null) return;

        // Base：每次抽到 = 入队 1 颗，不涉及 params/等级。
        if (ballType == BallType.Base)
        {
            if (ctx.Player != null) ctx.Player.AddBalls(BallType.Base, 1);
            return;
        }

        // 特殊球：UpgradeBase.CurrentStack 在 Apply 调用前还未 +1，
        // 所以这里它表示「已升过的次数」，即即将应用的等级 (1-based) = CurrentStack + 1。
        // 用 CurrentStack 直接索引 levelValues（0-based）。
        if (paramKeys != null && levelValues != null
            && CurrentStack >= 0 && CurrentStack < levelValues.Count
            && ctx.SpecialParams != null)
        {
            BallLevelValues lv = levelValues[CurrentStack];
            if (lv != null && lv.values != null)
            {
                int n = Mathf.Min(paramKeys.Count, lv.values.Count);
                for (int i = 0; i < n; i++)
                {
                    string k = paramKeys[i];
                    if (string.IsNullOrEmpty(k)) continue;
                    // Set 是覆盖式：升级时把上一级的值替换为新等级的绝对值。
                    ctx.SpecialParams.Set(ballType, k, lv.values[i]);
                }
            }
        }

        // 首次解锁（CurrentStack == 0 表示尚未升过任何级）：在队尾入队 1 颗。
        // 后续升级时不再入队，确保该 BallType 全程至多 1 颗（队列内 + 飞行中）。
        if (CurrentStack == 0 && ctx.Player != null)
        {
            ctx.Player.AddBalls(ballType, 1);
        }
    }

}

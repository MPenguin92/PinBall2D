using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单个 BallStat 修饰器：对某个 <see cref="BallStatType"/> 同时支持 Flat 与 Percent 增量。
/// </summary>
[Serializable]
public class BallStatModifier
{
    public BallStatType statType;
    public float flat;
    public float percent;
}

/// <summary>
/// Ball 数值强化词条：每次应用时把 <see cref="modifiers"/> 中的 modifier
/// 全部叠加到 <see cref="BallStats"/> 上；多个 modifier 即"两个例子也可同存"。
/// </summary>
[CreateAssetMenu(fileName = "BallStatUpgrade", menuName = "PinBall2D/Upgrade/BallStatUpgrade", order = 10)]
public class BallStatUpgradeData : UpgradeBase
{
    [SerializeField]
    [Tooltip("一次应用要叠加的若干修饰器（建议 1~3 个）。")]
    private List<BallStatModifier> modifiers = new List<BallStatModifier>();

    public IReadOnlyList<BallStatModifier> Modifiers => modifiers;

    public void SetModifiers(List<BallStatModifier> list)
    {
        modifiers = list ?? new List<BallStatModifier>();
    }

    public override void Apply(UpgradeContext ctx)
    {
        if (ctx == null || ctx.Stats == null || modifiers == null) return;

        for (int i = 0; i < modifiers.Count; i++)
        {
            BallStatModifier m = modifiers[i];
            if (m == null) continue;

            if (!Mathf.Approximately(m.flat, 0f))
                ctx.Stats.AddFlat(m.statType, m.flat);

            if (!Mathf.Approximately(m.percent, 0f))
                ctx.Stats.AddPercent(m.statType, m.percent);
        }
    }
}

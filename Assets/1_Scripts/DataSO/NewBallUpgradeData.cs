using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 新球类升级词条：解锁/扩容某种特殊球（Fire / Ice / Lightning ...）槽位，
/// 同时通过 <see cref="paramKeys"/>/<see cref="paramValues"/> 注入该球的运行时参数到 <see cref="SpecialBallParams"/>。
///
/// 约定：
/// - 同 id 第一次抽到 = 解锁；后续 = 扩容/参数升级。
/// - paramKeys 末尾以 "Add" 结尾的会触发 SpecialBallParams.Add 累加；
///   其他视为基础参数 Set 写入（首次解锁时使用）。
/// - <see cref="slotsAdd"/> 直接调用 Player.AddBallSlot(ballType, slotsAdd)。
/// </summary>
[CreateAssetMenu(fileName = "NewBallUpgrade", menuName = "PinBall2D/Upgrade/NewBallUpgrade", order = 11)]
public class NewBallUpgradeData : UpgradeBase
{
    [SerializeField]
    private BallType ballType = BallType.Fire;

    [SerializeField]
    [Tooltip("每次应用增加的特殊球槽位数（影响最大库存与当前库存）。")]
    private int slotsAdd = 1;

    [SerializeField]
    [Tooltip("写入 SpecialBallParams 的 key 列表。")]
    private List<string> paramKeys = new List<string>();

    [SerializeField]
    [Tooltip("与 paramKeys 一一对应的浮点值。")]
    private List<float> paramValues = new List<float>();

    public BallType BallType => ballType;

    public int SlotsAdd => slotsAdd;

    public IReadOnlyList<string> ParamKeys => paramKeys;

    public IReadOnlyList<float> ParamValues => paramValues;

    public void SetData(BallType type, int slots, List<string> keys, List<float> values)
    {
        ballType = type;
        slotsAdd = slots;
        paramKeys = keys ?? new List<string>();
        paramValues = values ?? new List<float>();
    }

    public override void Apply(UpgradeContext ctx)
    {
        if (ctx == null) return;

        // 1. 处理特殊全局 key：allSpecialSlotsAdd 给所有已解锁的特殊球各 +N 槽位。
        if (paramKeys != null && paramValues != null)
        {
            int n = Mathf.Min(paramKeys.Count, paramValues.Count);
            for (int i = 0; i < n; i++)
            {
                string k = paramKeys[i];
                float v = paramValues[i];
                if (string.IsNullOrEmpty(k)) continue;

                if (k == "allSpecialSlotsAdd")
                {
                    if (ctx.Player != null)
                    {
                        int add = Mathf.Max(0, Mathf.RoundToInt(v));
                        if (add > 0)
                        {
                            // 已解锁 = Player 已被授予过该 BallType 的槽位（MaxCount > 0）。
                            foreach (BallType bt in System.Enum.GetValues(typeof(BallType)))
                            {
                                if (bt == BallType.Base) continue;
                                if (ctx.Player.GetMaxCount(bt) > 0)
                                    ctx.Player.AddBallSlot(bt, add);
                            }
                        }
                    }
                    continue;
                }

                // 普通参数：以 "Add" 结尾的 key 累加；其他 key 直接 Set。
                if (ctx.SpecialParams != null)
                {
                    if (k.EndsWith("Add"))
                        ctx.SpecialParams.Add(ballType, k, v);
                    else
                        ctx.SpecialParams.Set(ballType, k, v);
                }
            }
        }

        // 2. 扩槽位（解锁等同于 +N 槽位，其中 N 至少为 1）。
        if (ctx.Player != null && slotsAdd > 0)
        {
            ctx.Player.AddBallSlot(ballType, slotsAdd);
        }
    }
}

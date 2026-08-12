# 升级效果总表（当前生效）

> 本表是**所有已有升级效果**的权威清单，后续新增 / 修改 / 删除升级时同步更新此表。
> 配表数据源：`Assets/9_Excel/Upgrades_Stat.csv`（数值）与 `Assets/9_Excel/Upgrades_NewBall.csv`（新球 / 扩容）。
> 修改配表后需在 Unity 执行 `Tools/Data/Import All` 重新导入。
> 最后更新：2026-08-10

## 总览

- 数值词条 9 个（`BallStatUpgradeData`）
- 扩容词条 2 个（`NewBallUpgradeData`，BallType.Base）
- 特殊球体系（Fire/Ice/Lightning/Poison/Heavy/Boomerang）已整体撤下，重新设计中

## Common（6）

| # | 名称（id） | 类型 | 效果 | 堆叠 |
|---|-----------|------|------|------|
| 1 | 锋利 `ball_dmg_basic` | 数值 | 基础伤害 +1 | 5 |
| 2 | 连射 `ball_fire_faster` | 数值 | 发射间隔 -15% | 4 |
| 3 | 弹匣扩容 `new_base_more` | 扩容 | 普通球 +1 入队尾 | 5 |
| 4 | 弹射加速 `ball_speed_charge` | 数值 | 每次反弹 +1 速度 | 5 |
| 5 | 巧击 `ball_side_swift` | 数值 | 侧面命中伤害 +30% | 4 |
| 6 | 暴击 `ball_crit_basic` | 数值 | 命中 10% 概率双倍伤害（每层 +10%） | 5 |

## Uncommon（3）

| # | 名称（id） | 类型 | 效果 | 堆叠 |
|---|-----------|------|------|------|
| 7 | 处决 `ball_execution` | 数值 | 对低血量敌人伤害翻倍（斩杀线 10%/层） | 3 |
| 8 | 急速 `ball_speed_boost` | 数值 | 初始速度 +20% | 3 |
| 9 | 振奋 `ball_heal_on_kill` | 数值 | 击杀 8/6/4 个敌人回复 1 点生命 | 3 |

## Rare（1）

| # | 名称（id） | 类型 | 效果 | 堆叠 |
|---|-----------|------|------|------|
| 10 | 背刺 `ball_back_assassin` | 数值 | 背面命中伤害 +80% | 3 |

## Legendary（1）

| # | 名称（id） | 类型 | 效果 | 堆叠 |
|---|-----------|------|------|------|
| 11 | 弹匣扩充 `new_base_flood` | 扩容 | 普通球 +3 入队尾 | 2 |

## 说明

- **编号**：按品质分组连续编号，仅用于本表沟通，与代码无关。
- **堆叠**：抽到同一条词条可重复生效的次数；堆满后从抽卡池剔除。
- **扩容词条**（3 / 11）复用 `NewBallUpgradeData` 的 `BallType.Base` 形态：每次抽到直接入队对应数量的普通球。
- **特殊球参数**：`SpecialBallParams` 容器与 `NewBallUpgradeData` 的分级结构保留，待新球体系设计完成后回填。
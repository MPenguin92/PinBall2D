# 难度数值设计说明

本文档用于记录当前难度系统的字段含义、运行时规则与当前曲线设计意图。下次继续设计难度时，优先阅读本文档，再查看 `Assets/9_Excel/Difficulty.csv`。

## 当前设计目标

当前目标是做一个长期无尽体验，但前期节奏清晰分层：

- `0-30s`：非常简单，主要让玩家进入节奏。
- `30-60s`：稍有难度，但正常人类玩家应能稳定通过。
- `60-180s`：每分钟增加一次难度，逐步给玩家压力。
- `180s` 左右：若无升级成长，正常情况下应基本不可能继续稳定通过。
- `180-1200s`：提供到 20 分钟的完整增长曲线；配合 Roguelike 升级（经验里程碑）后，强玩家可继续推进，需结合成长强度再评估。

当前有一个明确限制：所有阶段的 `unitAttack` 都固定为 `1`。因此难度不能依赖“单个 Unit 触底秒杀玩家”，而是依赖生成密度、Unit 血量和 Step 节奏共同制造压力。

局内成长已接入：击杀 Unit 按当前阶段 `unitExperience` 累加经验，达到里程碑弹出三选一升级（见 `doc/Function/Upgrade.md`）。调难度时必须同时考虑升级带来的清怪能力提升。

## 数据文件

原始配置表：

`Assets/9_Excel/Difficulty.csv`

运行时资源：

`Assets/8_Data/DifficultyTable.asset`（Addressables 短地址：`DifficultyTable`）

字段定义：

`Assets/1_Scripts/DataSO/DifficultyStageData.cs`

导入工具：

`Assets/1_Scripts/Editor/DataImporter.cs`

修改难度时，优先改 `Assets/9_Excel/Difficulty.csv`，再在 Unity 菜单执行 `Tools/Data/Import Difficulty` 生成 `DifficultyTable.asset`。如果手动直接改 asset，也要同步 CSV，否则下次导入会被 CSV 覆盖。

## DifficultyStageData 字段解释

`DifficultyStageData` 表示一个难度阶段。阶段从 `startTime` 秒开始生效，一直持续到下一条阶段数据接替。

字段说明：

- `startTime`：阶段开始时间，单位秒，相对游戏进入 Running 状态后的时间。运行时会取“当前时间之前最后一条阶段”。
- `spawnMin`：每次 Step 生成 Unit 数量下限。实际生成时会被屏幕可容纳数量夹紧。
- `spawnMax`：每次 Step 生成 Unit 数量上限。实际生成时同样会被屏幕可容纳数量夹紧。
- `unitHp`：本阶段新生成 Unit 的最大生命值。已有 Unit 不会因阶段变化自动改血量。
- `unitAttack`：Unit 触底时对 Player 造成的伤害。当前数值设计约束为全阶段固定 `1`。
- `stepInterval`：Step 间隔，单位秒。每次 Step 会让已有 Unit 下移一步，并生成新一批 Unit。数值越小，刷怪和推进越快。`<= 0` 时回退到 `Defines.StepInterval`。
- `unitExperience`：本阶段每个 Unit 被击杀时给玩家累加的经验值（Roguelike 里程碑用）。`<= 0` 时运行时报错并回退为 `1`。

运行时相关逻辑：

- `DifficultyTable.GetStageAt(gameTime)`：按 `startTime` 升序查找，返回 `startTime <= gameTime` 的最后一条。
- `Difficulty.GetSpawnRange()`：返回当前阶段的 `spawnMin/spawnMax`。
- `Difficulty.GetUnitHp()`：返回当前阶段的 `unitHp`。
- `Difficulty.GetUnitAttack()`：返回当前阶段的 `unitAttack`。
- `Difficulty.GetStepInterval()`：返回当前阶段的 `stepInterval`。
- `Difficulty.GetUnitExperience()`：返回当前阶段的 `unitExperience`（缺表/缺值时 LogError 并回退 1）。
- `UnitCreator.SpawnBatch()`：每个 Step 从难度表取生成区间，再用屏幕宽度计算出的最大可容纳数量进行夹紧。
- `UnitBase.Init()`：Unit 出池初始化时读取当前难度，把 `unitHp/unitAttack/unitExperience` 覆盖到 Unit 上。

## 当前曲线

当前 CSV 内容如下：

```csv
startTime,spawnMin,spawnMax,unitHp,unitAttack,stepInterval,unitExperience
0,1,1,3,1,1.0,1
30,1,2,3,1,0.917,1
60,1,3,6,1,0.833,2
120,2,4,9,1,0.708,2
180,7,10,24,1,0.35,4
240,8,11,33,1,0.3,5
300,9,12,45,1,0.267,6
360,10,13,60,1,0.233,8
420,11,14,78,1,0.208,10
480,12,15,99,1,0.183,12
540,13,16,123,1,0.167,14
600,14,17,150,1,0.15,16
660,15,18,180,1,0.133,18
720,16,19,216,1,0.121,20
780,17,20,255,1,0.108,23
840,18,21,300,1,0.096,26
900,19,22,348,1,0.083,30
960,20,23,402,1,0.075,34
1020,21,24,462,1,0.067,38
1080,22,25,528,1,0.058,42
1140,23,26,600,1,0.05,46
1200,24,28,675,1,0.042,50
```

设计解读：

- 前 30 秒使用 `spawn=1`、`hp=3`、`stepInterval=1.0`、`exp=1`，给玩家最低压力并开始积累升级经验。
- 30-60 秒只增加生成上限到 2，并略微加快 Step。
- 60-120 秒开始出现 6 血 Unit，经验升至 2，整体仍保持可通过。
- 120-180 秒进入过渡期，生成数量和血量都增加，但还没有完全压死玩家。
- 180 秒处是第一段明显断崖：`spawnMin=7`、`spawnMax=10`、`unitHp=24`、`stepInterval=0.35`、`exp=4`。由于 `unitAttack=1` 固定，这里通过高密度和快速推进让漏怪数量迅速累积；同时经验提高，促使玩家更频繁拿到升级。
- 180 秒后每分钟继续递增，主要提高 `unitHp`、缩短 `stepInterval`，并逐步提高 `unitExperience`。后期 `spawnMax` 可能超过屏幕可容纳数量，此时实际生成数量会被 `UnitCreator` 夹紧，但血量、节奏与经验产出仍会继续提升。

相对旧版曲线：`unitHp` 整体 ×3（开局 `1→3`，180s `8→24`，末期 `225→675`），配合球 `InitialSpeed` `12→24`（×2）拉长单怪交战节奏、加快弹道；`stepInterval` / 生成量曲线不变。

## 调参原则

如果要让游戏更简单：

- 优先增大 `stepInterval`，这会直接降低推进和刷怪频率。
- 其次降低 `spawnMin/spawnMax`，减少同屏压力。
- 再降低 `unitHp`，让玩家更容易清怪。
- 不建议提高或降低 `unitAttack`，当前设计要求它始终为 `1`。
- 也可提高前期 `unitExperience`，让玩家更快拿到升级（会改变成长节奏，需单独评估）。

如果要让游戏更难：

- 优先降低关键阶段的 `stepInterval`。
- 提高 `spawnMin` 比只提高 `spawnMax` 更稳定，因为它会提高每轮保底压力。
- 提高 `unitHp` 会显著增加清怪负担，尤其是 180 秒之后。
- 注意 `spawnMax` 受屏幕宽度上限影响，过高的值不一定能真实生效。
- 降低 `unitExperience` 会推迟升级，间接提高难度。

如果要调整“三分钟正常不可通过”的位置：

- 想提前到来：提高 `120s` 阶段，或把 `180s` 的断崖提前到 `150s`。
- 想延后到来：降低 `180s` 的 `spawnMin/spawnMax`，或把 `stepInterval=0.35` 放慢到 `0.45-0.55`。
- 想保留 3 分钟节点但更自然：在 `150s` 增加一个中间阶段，避免从 `120s` 到 `180s` 变化过猛。
- 评估时要带上升级：无升级基准 vs 正常抽卡成长，两条曲线可能差很多。

## 下次继续设计时的注意事项

- 先确认 `unitAttack` 是否仍要求全程固定为 `1`。
- 先确认目标死亡点：当前目标是约 3 分钟进入「无升级时」常规不可通过区；有升级后的存活时间需实测。
- 如果改 CSV，记得同步导入或更新 `DifficultyTable.asset`，并确认 Addressables 仍指向该资源。
- 如果需要更精确评估，应在 Unity 里实际跑 30s、60s、120s、180s 四个节点，观察同屏 Unit 数量、漏怪频率、经验/升级节奏和玩家可清怪能力。
- 经验里程碑阈值见 `Assets/9_Excel/KillMilestones.csv`；弹珠成长见 `Upgrades_Stat.csv` / `Upgrades_NewBall.csv`。改难度曲线时建议对照这两张表一起看。

# 游戏主逻辑（GamePlay）

整局由 **GameLogicManager** 主导，配合 **PoolManager**、**UIManager**、**UnitCreator**、**UpgradeService**、**VfxSpawner** 与 **GameEvents** 协同。Manager 负责切状态、清场与发事件；其它模块订阅响应。

---

## 职责划分

| 组件 | 职责 |
|------|------|
| **GameLogicManager** | 状态机；Running 下统一 Tick；Step 心跳；触底伤害；持有 Difficulty / BallStats / UpgradeService / VfxSpawner |
| **PoolManager** | PinBall / Unit 对象池（PoolRoot + SpawnRoot）与活跃列表 |
| **UIManager** | Start / GameOver / InGame / 升级面板根节点显隐 |
| **InGameUI** | 左下心形血条、右下纵向弹珠图标队列、顶部经验 `cur/next` |
| **UpgradeSelectionUI** | 三选一面板（自订 `OnUpgradeOffered/Applied`） |
| **VfxSpawner** | 按 `VfxCatalog` Addressables 地址播放命中/击杀特效 |
| **UnitCreator** | `OnStep` 批量生成 |
| **GameEvents** | 生命周期 + `OnStep` + 升级相关事件 |
| **Difficulty** | gameTime + 阶段查询（含 `unitExperience`） |
| **AssetLoader** | Addressables 同步加载（短地址，如 `"DifficultyTable"`） |
| **DataImporter** | `Tools/Data/*` CSV → `8_Data/*.asset` |

---

## 节奏系统（Step）

- Running 下按 `Difficulty.GetStepInterval()`（无表则 `Defines.StepInterval`）广播 `OnStep`。
- Unit：启动一步下移（可被减速/堵塞跳过）；UnitCreator：同帧 `SpawnBatch`。
- `Paused` / `SelectingUpgrade` / `Ended` 不推进。

---

## 生命值与攻击力

- Player 有 HP；Unit 触底扣 `unit.Attack`（设计上全程为 1）。
- PinBall 触底仍是回收补弹，与 Unit 触底独立。

---

## 游戏生命周期与事件

| 事件 | 触发 | 典型订阅方 |
|------|------|-----------|
| `OnGameStart` | `StartGame` 完成 | UIManager、UnitCreator |
| `OnGamePause` / `OnGameResume` | 暂停/恢复 | UnitCreator |
| `OnGameEnd` | 死亡或结束 | UIManager、UnitCreator |
| `OnReturnToHome` | `BackToHome` | UIManager、UnitCreator |
| `OnStep` | Step 心跳 | UnitBase、UnitCreator |
| `OnUnitKilled` | 击杀 Unit（回收前） | UpgradeService |
| `OnKillMilestoneReached` | 经验达里程碑 | （可扩展） |
| `OnUpgradeOffered` / `OnUpgradeApplied` | 抽卡 / 选卡 | UpgradeSelectionUI |

### 典型流程

1. Preparing → 点开始 → `StartGame`（Reset BallStats / UpgradeService / Difficulty，`player.Init`）→ Running。
2. Running：Tick + Step；击杀累加经验，达阈值 → `SelectingUpgrade` → 选卡 → 回 Running。
3. 触底扣血；HP 归零 → Ended → GameOver；Restart / Home。

---

## 游戏状态（GameState）

`Preparing` / `Running` / `Paused` / `Ended` / `SelectingUpgrade`（等同暂停，不跑 `UpdateGame`）。

---

## 更新驱动（统一 Tick）

仅 Running：刷新 Rect → `player.Tick` → `difficulty.Tick` + Step → PinBalls → Units。缓存池内对象不 Tick。

---

## 缓存池（PoolManager）

双根分离；`SpawnPinBall(address, …)` 按 Addressables 地址取球种 prefab；回收时按 `BallType` 补弹。

---

## 数值与难度

- CSV：`9_Excel/Difficulty.csv`（含 `unitExperience`）→ Import → `DifficultyTable`。
- 运行时：`AssetLoader.Load<DifficultyTable>("DifficultyTable")`。
- 作用点：生成区间、Unit HP/Attack/Experience、Step 间隔。
- 曲线说明见 **doc/Data/DifficultyBalance.md**。

---

## 与项目文档的对应

- Mgr：`GameLogicManager` / `PoolManager` / `UIManager` / `GameEvents` / `Difficulty` / `VfxSpawner` / `StarfieldController`
- 升级：`Upgrade/` + `doc/Function/Upgrade.md`
- 详细流程见 **PROJECT.md** 第 3~5 节。

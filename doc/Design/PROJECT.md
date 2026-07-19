# PinBall2D 项目文档

## 1. 项目概述

**PinBall2D** 是一款基于 Unity 的 2D 弹球游戏。玩家控制一个固定位置的发射器（Player），通过旋转瞄准方向并发射弹球（PinBall），弹球在由边框（Border）围成的区域内运动、反弹，撞击消灭从屏幕上方持续生成的方块单位（Unit）。若 Unit 落到底边，会对玩家造成伤害；玩家生命值归零则游戏结束。

- **引擎**：Unity 2022.3（2D 项目）
- **产品名**：PinBall2D（见 `ProjectSettings/ProjectSettings.asset`）
- **主场景**：`Assets/Scenes/MainScene.unity`
- **第三方插件**：DOTween（位于 `Assets/Plugins/DOTween/`，用于战斗动画补间）

---

## 2. 目录结构

```
PinBall2D/
├── Assets/
│   ├── 1_Scripts/                      # 游戏逻辑脚本
│   │   ├── Border.cs                  # 边框（自动对齐屏幕 / 反弹 / 底边回收）
│   │   ├── Player.cs                  # 玩家发射器（旋转、发射、生命值）
│   │   ├── PlayerRender.cs            # 玩家渲染（LineRenderer 方向预览线 + 战斗动画）
│   │   ├── ICombatAnimation.cs        # 战斗动画接口（攻击/受击/死亡）
│   │   ├── Mgr/
│   │   │   ├── Defines.cs             # 项目级常量（UnitSize / Step*）
│   │   │   ├── Difficulty.cs          # 难度运行时查询（gameTime + Stage，含 unitExperience）
│   │   │   ├── GameEnum.cs            # 通用枚举（BounceDirection、GameState）
│   │   │   ├── GameEvents.cs          # 静态事件总线（生命周期 + OnStep + 升级）
│   │   │   ├── GameLogicManager.cs    # 单例，统一调度 Tick，受 GameState 控制
│   │   │   ├── PoolManager.cs         # PinBall/Unit 对象池与活跃列表（PoolRoot/SpawnRoot 分离）
│   │   │   ├── UIManager.cs           # 单例，订阅事件控制 UI 显隐
│   │   │   ├── VfxSpawner.cs          # 命中/击杀 VFX（Addressables + VfxCatalog）
│   │   │   └── StarfieldController.cs # 程序化星空背景（闪烁/缩放）
│   │   ├── DataSO/                    # 数据 ScriptableObject 定义
│   │   │   ├── DifficultyStageData.cs # 单个阶段数据（与 Excel 列一一对应）
│   │   │   ├── DifficultyTable.cs     # 难度阶段表 SO
│   │   │   ├── KillMilestoneData.cs   # Roguelike：单个经验里程碑（阈值 + 4 品质权重）
│   │   │   ├── KillMilestoneTable.cs  # Roguelike：里程碑表 SO（表末按差值外推）
│   │   │   ├── UpgradeCatalog.cs      # Roguelike：全局升级池（所有可抽词条）
│   │   │   ├── BallStatUpgradeData.cs # Roguelike：数值类升级 SO（多 modifier）
│   │   │   ├── NewBallUpgradeData.cs  # Roguelike：新球类升级 SO（解锁 + 多级参数）
│   │   │   ├── BallStatDefaultsTable.cs # Roguelike：弹珠属性默认基础值表
│   │   │   ├── BallSpriteSet.cs       # 各 BallType 的 Sprite 映射
│   │   │   ├── BallTrailSet.cs        # 各 BallType 的拖尾样式
│   │   │   └── VfxCatalog.cs          # 各 BallType 命中/击杀 VFX 地址
│   │   ├── Upgrade/                   # Roguelike 升级运行时
│   │   │   ├── BallStatType.cs        # 弹珠机制参数枚举
│   │   │   ├── BallStats.cs           # 全局弹珠属性容器（base + flat + percent）
│   │   │   ├── BallType.cs            # 弹珠类型枚举（Base/Fire/Ice/Lightning/...）
│   │   │   ├── SpecialBallParams.cs   # 各球种全局参数字典
│   │   │   ├── UpgradeRarity.cs       # 品质枚举
│   │   │   ├── UpgradeBase.cs         # 升级 SO 抽象基类 + UpgradeContext
│   │   │   └── UpgradeService.cs      # 监听 OnUnitKilled、经验阈值、抽池、Apply
│   │   ├── Utility/
│   │   │   └── AssetLoader.cs         # 统一资源加载（Addressables 同步，短地址）
│   │   ├── Editor/
│   │   │   ├── DataImporter.cs        # CSV → SO 导入菜单（Tools/Data/*）
│   │   │   └── ChineseFontAssetGenerator.cs # TMP 中文字体 Asset 生成
│   │   ├── PInBall/
│   │   │   ├── PinBallBase.cs         # 弹球基类（运动、碰撞、BallStats、穿透/反弹）
│   │   │   ├── PinBallRender.cs       # Sprite + TrailRenderer（BallSpriteSet / BallTrailSet）
│   │   │   ├── FirePinBall.cs         # 火球：命中 AOE 爆炸
│   │   │   ├── IcePinBall.cs          # 冰球：ApplySlow + 冰墙堵塞
│   │   │   ├── LightningPinBall.cs    # 闪电球：链式跳跃
│   │   │   ├── PoisonPinBall.cs       # 毒球：DoT 持续伤害
│   │   │   ├── HeavyPinBall.cs        # 重力球：击退 + 额外伤害
│   │   │   └── BoomerangPinBall.cs    # 回旋球：首次触底自动反弹一次
│   │   ├── Unit/
│   │   │   ├── UnitBase.cs            # 单位基类（HP/Attack/Experience、Step、减速/堵塞）
│   │   │   ├── UnitRender.cs          # 单位渲染（HP 颜色 + 战斗/触底动画）
│   │   │   ├── IUnitCreator.cs        # 生成器接口（空标记，继承 IDisposable）
│   │   │   ├── UnitCreator.cs         # 默认生成器（订阅 OnStep 批量生成）
│   │   │   └── SimpleUnit.cs          # 空壳占位（逻辑在 UnitBase）
│   │   └── UI/
│   │       ├── StartScreenUI.cs       # 开始界面按钮脚本
│   │       ├── GameOverUI.cs          # 游戏结束界面按钮脚本
│   │       ├── InGameUI.cs            # HUD：心形血条 + 弹珠图标队列 + 经验进度
│   │       └── UpgradeSelectionUI.cs  # Roguelike 三选一升级面板
│   ├── 2_Prefab/
│   │   ├── BaseBall.prefab / FireBall / IceBall / LightningBall / PoisonBall / HeavyBall / BoomerangBall
│   │   ├── SimpleUnit.prefab          # 默认单位预制体
│   │   └── UI/
│   │       ├── StartScreen.prefab / GameOverScreen.prefab / GameHUD.prefab / UpgradeSelectionUI.prefab
│   ├── 3_Shader/
│   │   ├── HsApp_CyberpunkPolygon.shader # 赛博朋克霓虹呼吸 Shader
│   │   └── DashedLine.shader          # 滚动虚线 Shader（瞄准预览线）
│   ├── 7_Res/
│   │   ├── common.mat / dashed_line.mat
│   │   └── GeneratedShapes/           # 程序生成的形状贴图
│   ├── 8_Data/                         # 运行时数据 Asset（Import 生成 + 手配 SO）
│   │   ├── DifficultyTable.asset / KillMilestoneTable.asset / UpgradeCatalog.asset
│   │   ├── BallStatDefaultsTable.asset / BallSpriteSet.asset / BallTrailSet.asset / VfxCatalog.asset
│   │   └── Upgrades/                   # Stat_*.asset / NewBall_*.asset
│   ├── 9_Excel/                        # 原始配置表 CSV
│   │   ├── Difficulty.csv（含 unitExperience）
│   │   ├── KillMilestones.csv / Upgrades_Stat.csv / Upgrades_NewBall.csv / BallStatDefaults.csv
│   ├── Plugins/DOTween/
│   ├── Resources/DOTweenSettings.asset
│   └── Scenes/MainScene.unity
├── doc/
│   ├── Design/   # Design.md 索引、PROJECT.md 本文档
│   ├── Function/ # GamePlay / Player / Border / Unit / PinBall / Upgrade
│   └── Data/     # DifficultyBalance.md 难度曲线说明
├── ProjectSettings/
├── UserSettings/
├── Library/
├── Logs/
└── .gitignore
```

### 2.1 脚本职责总览

| 脚本 | 路径 | 职责 |
|------|------|------|
| `Border.cs` | 1_Scripts/ | 矩形边框，自动对齐屏幕边；反弹法线；底边标识 |
| `Player.cs` | 1_Scripts/ | 旋转瞄准、F 发射、FIFO 弹珠队列、生命值、Addressables 球种地址 |
| `PlayerRender.cs` | 1_Scripts/ | LineRenderer 方向预览线（反射/阻挡）；实现 `ICombatAnimation`，攻击=DOTween 360° 旋转 |
| `ICombatAnimation.cs` | 1_Scripts/ | 战斗动画接口：`PlayAttackAnimation / PlayHitAnimation / PlayDeathAnimation` |
| `Defines.cs` | 1_Scripts/Mgr/ | 项目级常量：UnitSize / StepDistance / StepInterval / StepMoveDuration |
| `Difficulty.cs` | 1_Scripts/Mgr/ | 难度运行时：gameTime + 阶段参数（含 unitExperience） |
| `GameEnum.cs` | 1_Scripts/Mgr/ | 通用枚举：BounceDirection、GameState |
| `GameEvents.cs` | 1_Scripts/Mgr/ | 静态事件总线：生命周期事件 + `OnStep` 节奏心跳 |
| `DifficultyStageData.cs` | 1_Scripts/DataSO/ | 单阶段数据结构（字段对齐 CSV 列） |
| `DifficultyTable.cs` | 1_Scripts/DataSO/ | 难度阶段列表 ScriptableObject |
| `AssetLoader.cs` | 1_Scripts/Utility/ | Addressables 同步加载入口（短地址） |
| `DataImporter.cs` | 1_Scripts/Editor/ | CSV→SO 导入菜单工具（Editor-only） |
| `GameLogicManager.cs` | 1_Scripts/Mgr/ | 单例，统一调度 Tick，切状态 + 清场 + 发事件；持有 BallStats / UpgradeService / VfxSpawner |
| `PoolManager.cs` | 1_Scripts/Mgr/ | PinBall/Unit 对象池：PoolRoot + SpawnRoot 双根分离 |
| `UIManager.cs` | 1_Scripts/Mgr/ | 单例，驱动 Start / GameOver / InGame / 升级面板显隐 |
| `VfxSpawner.cs` | 1_Scripts/Mgr/ | 命中/击杀特效实例化（VfxCatalog + Addressables） |
| `StarfieldController.cs` | 1_Scripts/Mgr/ | 程序化星空背景 |
| `PinBallBase.cs` | 1_Scripts/PInBall/ | 弹球运动、BallStats 伤害/穿透/反弹、底边回收、RaiseUnitKilled |
| `PinBallRender.cs` | 1_Scripts/PInBall/ | BallSpriteSet + TrailRenderer（BallTrailSet） |
| `UnitBase.cs` | 1_Scripts/Unit/ | HP/Attack/Experience、Step 位移、减速/堵塞、触底 |
| `UnitRender.cs` | 1_Scripts/Unit/ | HP 变色、战斗/触底动画钩子、减速染色 |
| `IUnitCreator.cs` | 1_Scripts/Unit/ | 生成器接口（空标记，继承 `IDisposable`） |
| `UnitCreator.cs` | 1_Scripts/Unit/ | 默认生成器（订阅 `OnStep` 批量生成） |
| `SimpleUnit.cs` | 1_Scripts/Unit/ | 空壳占位（逻辑在 UnitBase） |
| `StartScreenUI.cs` | 1_Scripts/UI/ | 开始界面 → `StartGame()` |
| `GameOverUI.cs` | 1_Scripts/UI/ | Restart / Home |
| `InGameUI.cs` | 1_Scripts/UI/ | 心形血条 + 弹珠图标队列 + 经验进度 |

---

## 3. 核心架构原则

### 3.1 统一 Tick 驱动

所有需要逐帧更新的游戏对象（Player、PinBall、Unit）**不持有独立的 `Update`**，由 **GameLogicManager.UpdateGame()** 统一按固定顺序调用 `Tick`。UnitCreator 完全由事件驱动，不参与 Tick。处于缓存池内（已隐藏）的物体不参与 Tick。

### 3.2 游戏状态（GameState）

- **GameState** 枚举：`Preparing`（准备中）、`Running`（运行中）、`Paused`（暂停）、`Ended`（结束）、`SelectingUpgrade`（Roguelike 选卡，等同 Paused）。
- **主逻辑 Update**：仅当 `GameLogicManager.CurrentState == GameState.Running` 时执行 `UpdateGame()`；其他状态不驱动 Tick。
- **切换入口**：`StartGame / PauseGame / ResumeGame / EndGame / BackToHome`，每个入口都会向 `GameEvents` 发送对应事件。

### 3.3 事件总线（GameEvents）

模块间解耦的核心：

| 事件 | 触发时机 |
|------|---------|
| `OnGameStart` | `StartGame()` 初始化完成后 |
| `OnGamePause` | `PauseGame()` |
| `OnGameResume` | `ResumeGame()` |
| `OnGameEnd` | `EndGame()`（玩家死亡或主动结束） |
| `OnReturnToHome` | `BackToHome()` |
| `OnStep` | Running 下每 `Defines.StepInterval` 秒一次（节奏心跳） |
| `OnUnitKilled(unit)` | `PinBallBase` 击杀 Unit 时（在 `RecycleUnit` 之前） |
| `OnKillMilestoneReached(idx)` | `UpgradeService` 累计经验达到一个里程碑时(事件名保留历史命名) |
| `OnUpgradeOffered(options)` | 抽出三张升级候选，UI 显面板 |
| `OnUpgradeApplied(upgrade)` | 玩家点选并应用了某个升级，UI 关面板 |

**发送方**：只有 `GameLogicManager`。
**典型订阅方**：`UIManager`（UI 显隐）、`UnitCreator`（`OnStep` 批量生成）、所有活跃 `UnitBase`（`OnStep` 启动一步位移）。未来音效、关卡、分数等系统都可以直接订阅事件，无需改动 `GameLogicManager`。

### 3.3.1 节奏系统（Step）

- **心跳源**：`GameLogicManager.UpdateGame()` 在 Running 状态下累加 `stepTimer`，每满 `Defines.StepInterval` 秒调用一次 `GameEvents.RaiseStep()`。`StartGame()` 会重置该计时器，`Paused/Ended` 下不推进。
- **Unit 响应**：`UnitBase.OnEnable` 订阅 `OnStep` 并在 `OnDisable` 取消订阅，刚好随对象池出入池自动管理订阅。`SimpleUnit` 收到事件后启动一次向下的 Lerp 位移（距离 `Defines.StepDistance`，时长 `Defines.StepMoveDuration`）。
- **生成响应**：`UnitCreator` 在 `OnStep` 里调用 `SpawnBatch`。新一批生成与存量单位的下移**同帧发生**，形成稳定的「每秒 1 步、顶部刷一批」节奏。
- **调参**：直接改 `Defines.cs` 中的常量即可整体联动。

### 3.4 对象池（PoolManager）

PinBall 与 Unit 的缓存池由独立组件 **PoolManager** 管理，使用 `UnityEngine.Pool.ObjectPool<T>`：

- **入池**：`SetActive(false)` + `SetParent(poolRoot)`。
- **出池**：`SetParent(null)` + `SetActive(true)`，加入活跃列表参与 Tick。
- 池根节点若未在 Inspector 指定，`Awake` 时自动创建。
- `GameLogicManager` 通过引用调用 `SpawnPinBall / RecyclePinBall / SpawnUnit / RecycleUnit`；弹球回收时额外调用 `player.AddPinBall()` 补充弹药。

### 3.4.1 难度系统（Difficulty）与数据流水线

- **数据源**：`Assets/9_Excel/Difficulty.csv`（UTF-8、逗号分隔、首行为表头）。策划直接用 Excel 编辑后另存为 CSV 即可。
- **数据结构**：`DifficultyStageData`（纯 `[Serializable]` 类）与列一一对应；`DifficultyTable : ScriptableObject` 持有 `List<DifficultyStageData>`。
- **导入工具**：`Tools/Data/Import Difficulty` 菜单（Editor-only，位于 `Assets/1_Scripts/Editor/DataImporter.cs`），读 CSV → 生成/更新 `Assets/8_Data/DifficultyTable.asset`。
- **运行时查询**：`Difficulty` 纯 C# 类：
  - 由 `GameLogicManager.Awake()` 通过 `AssetLoader.Load<DifficultyTable>("DifficultyTable")` 加载 SO 并 `new Difficulty(table)`。
  - `StartGame()` 调用 `Reset()` 归零 `gameTime`；`UpdateGame()` 每帧 `Tick(Time.deltaTime)`。
  - 对外暴露：`GetSpawnRange() / GetUnitHp() / GetUnitAttack() / GetStepInterval() / GetUnitExperience()`。查询均按 `gameTime` 匹配阶段；`GetUnitExperience()` 缺表/缺值时 `LogError` 并回退 1。
- **调用点**：
  - `UnitCreator.SpawnBatch`：`GetSpawnRange()` + 屏幕可容纳数夹紧。
  - `UnitBase.Init()` → `ApplyDifficulty()`：覆盖 hp / attack / experience。
  - `GameLogicManager.UpdateGame`：`GetStepInterval()` 驱动 Step 心跳。
- **容错**：表缺失或为空时 `HasTable == false`，返回保守兜底（spawn=(1,1)、hp=1、attack=1、interval=`Defines.StepInterval`）。
- **曲线说明**：见 `doc/Data/DifficultyBalance.md`。
- **扩展**：新增表 = 一对 SO + `DataImporter.ImportXxx` + CSV。

### 3.4.2 资源加载（AssetLoader）

- 统一入口：`AssetLoader.Load<T>(address)`，入参为 Addressables **短地址**（如 `"DifficultyTable"`、`"BaseBall"`、`"VFX/HitFire"`）。
- 实现：`Addressables.LoadAssetAsync<T>(address).WaitForCompletion()`；失败打错误并返回 null。业务层不直接依赖 Addressables API。

### 3.5 UI 与事件

- **UIManager** 持有 `startScreenUI` / `gameOverUI` / `inGameUI` / `upgradeSelectionUI`。
- `OnGameStart` → 隐藏 Start/GameOver、显示 InGame、关升级面板；`OnGameEnd` → 隐藏 InGame、显示 GameOver；`OnReturnToHome` → 显示 StartScreen。
- **UpgradeSelectionUI** 自行订阅 `OnUpgradeOffered/Applied` 显隐。
- **StartScreenUI / GameOverUI**：按钮回调 `StartGame / RestartGame / BackToHome`。

---

## 4. 核心脚本详解

### 4.1 GameEnum.cs — 通用枚举

- 路径：`Assets/1_Scripts/Mgr/GameEnum.cs`
- `BounceDirection`：Up / Down / Left / Right，供 Border 指定反弹法线。
- `GameState`：Preparing / Running / Paused / Ended / SelectingUpgrade。

### 4.1.1 Defines.cs — 项目级常量

- 路径：`Assets/1_Scripts/Mgr/Defines.cs`
- `UnitSize`：Unit 的标准边长（= 1 米），作为逻辑矩形、视觉 `localScale` 统一来源。
- `StepDistance`：单次 Step 的位移距离，默认与 `UnitSize` 一致（= 1 米）。
- `StepInterval`：两次 Step 事件之间的时间间隔（秒），默认 1（运行时可被 Difficulty 覆盖）。
- `StepMoveDuration`：单步位移的过渡时长（秒），默认 0.2。
- 调参影响：节奏系统在无难度表时从这里派生。

### 4.1.2 Difficulty.cs — 难度运行时

- 路径：`Assets/1_Scripts/Mgr/Difficulty.cs`
- 非 MonoBehaviour，由 `GameLogicManager` 持有。
- 字段：`table`（SO 引用）、`gameTime`（Running 累积秒）。
- 方法：`Reset() / Tick(dt) / GetSpawnRange() / GetUnitHp() / GetUnitAttack() / GetStepInterval() / GetUnitExperience() / HasTable`。
- 查询逻辑：基于 `DifficultyTable.GetStageAt(gameTime)`；表为空时 `HasTable == false`。

### 4.1.3 DataSO — 数据 ScriptableObject

- 路径：`Assets/1_Scripts/DataSO/`
- `DifficultyStageData`：字段 `startTime / spawnMin / spawnMax / unitHp / unitAttack / stepInterval / unitExperience`。
- `DifficultyTable`：`List<DifficultyStageData>` + `SetStages / GetStageAt / StageCount`。
- 另有：`KillMilestoneTable`、`UpgradeCatalog`、`BallStatDefaultsTable`、`BallSpriteSet`、`BallTrailSet`、`VfxCatalog` 等（后三者多为手配 SO，不经 CSV 导入）。

### 4.1.4 AssetLoader.cs — 资源加载入口

- 路径：`Assets/1_Scripts/Utility/AssetLoader.cs`
- `T Load<T>(string address)`：Addressables 短地址同步加载。

### 4.1.5 DataImporter.cs — Excel 导入工具（Editor-only）

- 路径：`Assets/1_Scripts/Editor/DataImporter.cs`
- 菜单：`Tools/Data/Import All`（Difficulty / KillMilestones / BallStatDefaults / Upgrades）、以及各分项 Import。
- Difficulty CSV 需 7 列（含 `unitExperience`），写入 `8_Data/DifficultyTable.asset`。

### 4.2 GameEvents.cs — 事件总线

- 路径：`Assets/1_Scripts/Mgr/GameEvents.cs`
- 静态事件：`OnGameStart / OnGamePause / OnGameResume / OnGameEnd / OnReturnToHome / OnStep`，
  以及 Roguelike 升级体系新增的 `OnUnitKilled(UnitBase) / OnKillMilestoneReached(int) / OnUpgradeOffered(IList<UpgradeBase>) / OnUpgradeApplied(UpgradeBase)`。
- 对应 `Raise*` 静态方法供 `GameLogicManager` 与 `UpgradeService` 调用。
- `OnStep` 仅在 Running 下由 `UpdateGame` 按 `Defines.StepInterval` 推进广播；其它事件在对应状态切换时广播。

### 4.3 Border.cs — 边框

- 矩形障碍，弹球碰触后镜面反射；底边（`isBottomBorder`）时弹球回收并补弹；Unit 触底另由 `SimpleUnit` 检测并回调 `OnUnitReachBottom`。
- **自动对齐**：`autoAlignToCameraEdge` 为 true 时根据正交相机的 `orthographicSize / aspect` 与 `bounceDirection` 自动设置位置和 scale，适应任意分辨率。

### 4.4 GameLogicManager.cs — 游戏逻辑管理器（单例）

- **职责**：整局状态机入口；统一驱动 `UpdateGame()`；负责清场；通过 `GameEvents` 广播生命周期；不直接持有 UI 与生成器的"业务控制权"。

#### Inspector 配置

| 分组 | 字段 | 说明 |
|------|------|------|
| References | `player` | 场景中的 Player |
| References | `poolManager` | 场景中的 PoolManager |
| References | `vfxSpawner` | 命中特效（可空，Awake 时自动补） |
| Game State | `gameState` | Preparing / Running / Paused / Ended / SelectingUpgrade |

UI 引用在 `UIManager`；生成逻辑在 `UnitCreator`；`PlayerRender` 由 `Player.Tick()` 驱动。

#### 核心字段

- `borders`：`StartGame()` 时 `FindObjectsByType<Border>`。
- `unitCreator`：`Awake` 时 `new UnitCreator()`，`OnDestroy` 时 `Dispose`。
- `difficulty` / `ballStats` / `specialBallParams` / `upgradeService`：Awake 加载并构造；`StartGame` 时 Reset。
- `stepTimer`：按 `difficulty.GetStepInterval()` 触发 `RaiseStep`。
- `ActivePinBalls / ActiveUnits`、`Player`、`BallStats`、`UpgradeService`、`VfxSpawner`：只读暴露。

#### 主要方法

| 方法 | 说明 |
|------|------|
| `StartGame()` | Reset 升级数值 → 收集 Border、`player.Init()`、清池 → Running + `RaiseGameStart` |
| `PauseGame() / ResumeGame()` | Running ↔ Paused |
| `PauseForUpgradeSelection() / ResumeFromUpgradeSelection()` | Running ↔ SelectingUpgrade |
| `UpdateGame()` | 仅 Running；含 Step 心跳 |
| `EndGame()` | Ended → 清场 → `RaiseGameEnd` |
| `RestartGame()` / `BackToHome()` | 重开 / 回 Preparing |
| `OnUnitReachBottom(unit)` | 触底动画 → 扣血 → 回收 → 死亡则 EndGame |
| `SpawnPinBall / RecyclePinBall / SpawnUnit / RecycleUnit` | 转发 PoolManager（回收球时 `AddPinBall`） |

**生命周期**：`Awake`（Instance、Difficulty、BallStats、UpgradeService、UnitCreator、VfxSpawner）→ 点开始 → Running Tick → `OnDestroy` 反注册。

### 4.5 UIManager.cs — UI 管理器（单例）

- Inspector：`startScreenUI` / `gameOverUI` / `inGameUI` / `upgradeSelectionUI`。
- `OnGameStart` → 显 InGame、隐 Start/GameOver、关升级面板；`OnGameEnd` → 显 GameOver；`OnReturnToHome` → 显 StartScreen。

### 4.6 PoolManager.cs — 缓存池管理器

- **职责**：PinBall / Unit 对象池 + 活跃列表；不处理游戏规则。

#### Inspector 配置

| 分组 | 字段 | 说明 |
|------|------|------|
| Prefabs | `pinBallPrefab` / `unitPrefab` | 对应预制体 |
| Pool Roots | `pinBallPoolRoot` / `unitPoolRoot` | **缓存根**（隐藏对象的归宿，可选，不设则自动创建） |
| Active Roots | `pinBallSpawnRoot` / `unitSpawnRoot` | **运行时根**（出池后挂载位置，便于在 Hierarchy 整理活跃对象，可选） |
| PinBall Pool | `pinBallPoolDefaultCapacity / pinBallPoolMaxSize` | 默认 20 / 50 |
| Unit Pool | `unitPoolDefaultCapacity / unitPoolMaxSize` | 默认 20 / 100 |

> 出池流程：`SetParent(spawnRoot)` + `SetActive(true)`；入池流程：`SetActive(false)` + `SetParent(poolRoot)`。两根分离让活跃对象与缓存对象在 Hierarchy 中互不干扰。

#### 主要方法

| 方法 | 说明 |
|------|------|
| `ClearActivePinBalls / ClearActiveUnits` | 清空并回收所有活跃对象 |
| `RegisterExistingUnit(unit)` | 把场景里已摆好的 Unit 注入活跃列表 |
| `SpawnPinBall / SpawnUnit` | 从池取出、设置位置并 Init、加入活跃列表 |
| `RecyclePinBall / RecycleUnit` | 从活跃列表移除并 Release |

**生命周期**：`Awake` → `InitPools()`；`OnDestroy` → 两个池 `Dispose()`。

### 4.7 PinBallBase.cs — 弹球基类

- **职责**：运动、与 Border / Unit 碰撞与镜面反弹、底边回收。所有数值（速度、伤害、命中方向倍率、穿透、最大反弹）从全局 `BallStats` 实时读取。
- 可配置：`ballType`（决定回收时归还到哪种库存槽）、`initialSpeedHint`（仅 Inspector 调试展示，不参与逻辑）。
- 核心：`Init(direction, speed)`、`Tick(borders, activeUnits)`、`Velocity`、`Radius`、`BallType`。
- 命中方向：以 Unit.MoveDirection 为基准。撞顶边 = `FrontHit`，撞底边 = `BackHit`，撞左右 = `SideHit`，伤害倍率分别从 `BallStats` 取 `FrontHitMul / BackHitMul / SideHitMul`。
- 击杀流程：`unit.TakeDamage` → 子类钩子 `OnHitUnit` → `destroyed=true` 时 `RaiseUnitKilled` 再 `RecycleUnit`，然后按 `PiercingChance` 决定穿透或反弹。
- 派生球扩展点：override `OnHitUnit` 即可（FirePinBall/IcePinBall/LightningPinBall/...）。

### 4.8 PinBallRender.cs — 弹球渲染

- 按 `BallType` 从 Addressables 加载 `BallSpriteSet` / `BallTrailSet`，套用 Sprite 与 `TrailRenderer`。
- `ResetTrailAfterSpawn()`：出池后清轨迹再开始发射；入池/Disable 时停止拖尾。

### 4.9 Player.cs — 玩家发射器

- **职责**：固定位置，A/D 旋转（±`maxAngle`），F 发射，FIFO `Queue<BallType>` + 生命值；可选 `muzzle` 作为旋转/发射原点。
- 可配置：`rotateSpeed`、`maxAngle`、`maxHp`、`initialBallCount`、`playerRender`、`muzzle`。冷却与初速来自 `BallStats`。
- 核心：
  - `Init()`：清空队列，入队 N 个 `Base`，`totalBalls = N`，重置 HP。
  - `AddPinBall(type)`：回收入队尾，不改 totalBalls；`AddBalls(type, n)`：升级入队 + 扩容。
  - `HandleFire`：出队 → `SpawnPinBall(ballAddress[type], …)`；队首即下一发。

### 4.10 PlayerRender.cs — 玩家渲染

- 预览虚线：Border 反射、Unit/底边停止；`DashedLine` 材质。
- `PlayAttackAnimation`：DOTween 360° 旋转；受击/死亡预留。

### 4.11 UnitBase.cs — 单位基类

- HP / Attack / Experience；`ApplyDifficulty` 覆盖三者；Step 内减速累计 + 堵塞检测 + 位移；触底回调。
- `SimpleUnit` 为空壳，逻辑全在基类。

### 4.12 UnitRender.cs — 单位渲染

- HP 变色；`ICombatAnimation` + `PlayReachBottomAnimation`；减速染色钩子。

### 4.13~4.15 UnitCreator / SimpleUnit

- `IUnitCreator` 空标记；`UnitCreator` 事件驱动 `SpawnBatch`（难度区间 + 出生点占用检测）。
- `SimpleUnit`：Addressables `"SimpleUnit"` 占位脚本。

### 4.16~4.17 StartScreenUI / GameOverUI

- 开始 → `StartGame`；Restart / Home → `RestartGame` / `BackToHome`。

### 4.18 InGameUI.cs — 游戏内 HUD

- 左下纵向心形血条；右下纵向弹珠图标（`BallSpriteSet`）；顶部经验 `ExperienceAccumulated / nextThreshold`。
- 由 `UIManager` 在 GameStart 显示、End/Home 隐藏。

### 4.19 ICombatAnimation.cs — 战斗动画接口

- 路径：`Assets/1_Scripts/ICombatAnimation.cs`。
- 定义三个钩子：`PlayAttackAnimation / PlayHitAnimation / PlayDeathAnimation`。
- 当前实现者：`PlayerRender`（攻击：DOTween 360° 旋转）、`UnitRender`（默认空实现，`PlayReachBottomAnimation` 为额外扩展方法）。
- 设计目的：把战斗反馈（补间/粒子/震屏）从逻辑层完全剥离，逻辑层只负责事件触发，效果层可独立替换。

### 4.20 StarfieldController.cs — 程序化星空背景

- 路径：`Assets/1_Scripts/Mgr/StarfieldController.cs`。
- 在指定相机视口范围内（或 `spawnArea` 矩形内）批量生成 `starCount` 颗星星 `SpriteRenderer`，每颗有独立的初始 alpha、size、闪烁速度与相位。
- `Update` 中按 `sin` 波同时驱动每颗星的透明度（`alphaTwinkleStrength`）与缩放（`scaleTwinkleStrength`）。
- 未指定 `starSprite` 时会运行时生成一张 64×64 的高斯辉光 + 十字星形 RGBA 贴图作为兜底。
- `OnEnable` 自动 `Rebuild`，`OnDisable` 清场；提供 `[ContextMenu("Rebuild Stars" / "Clear Stars")]` 方便编辑期调试。
- 渲染：通过 `sortingLayerName / sortingOrder`（默认 -100）置于背景层。

---

## 5. 游戏流程与数据流

### 5.1 初始化

1. `GameLogicManager.Awake()`：设置 `Instance`，`new UnitCreator()`（构造器内部订阅事件）。
2. `PoolManager.Awake()`：`InitPools()` 创建两个 ObjectPool 及池根节点（若未指定）。
3. `UIManager.Awake()`：订阅事件；保持场景里默认显示的 StartScreen 可见。
4. 场景启动后 `GameState = Preparing`，等待玩家点击 Start。

### 5.2 游戏开始

1. 玩家点击 StartScreen 上的开始按钮 → `StartScreenUI.OnStartClicked`。
2. 隐藏 StartScreen GameObject，调用 `GameLogicManager.StartGame()`。
3. `StartGame()` 收集 borders、`player.Init()`（重置弹药 + HP）、清池并注册场景 Unit → `GameState = Running` → `RaiseGameStart`。
4. `UnitCreator` 订阅回调重置计时并进入运行；`UIManager` 订阅回调确保两个 UI 都隐藏。

### 5.3 每帧更新顺序（Running）

`GameLogicManager.Update()` → `UpdateGame()`：

| 步骤 | 操作 |
|------|------|
| 1 | 刷新所有 Border / Unit Rect |
| 2 | `player.Tick()`（处理旋转、发射、冷却 + 内部调用 `playerRender.Tick()` 绘制预览线） |
| 3 | 推进难度时间轴 `difficulty.Tick(dt)` 与 `stepTimer`，每满 `difficulty.GetStepInterval()` 触发一次 `GameEvents.RaiseStep()`（同帧驱动 UnitCreator 生成 + 所有 Unit 启动位移；支持单帧补齐多步） |
| 4 | 逆向遍历 `ActivePinBalls`，每项 `Tick(borders, activeUnits)` |
| 5 | 逆向遍历 `ActiveUnits`，每项 `Tick()`（`SimpleUnit` 内部推进 Step 位移插值 + 到达目标时触底回调） |

### 5.4 碰撞与交互

- **PinBall ↔ Border**：反射；底边则 `RecyclePinBall` 并 `player.AddPinBall()`。
- **PinBall ↔ Unit**：反射 + 扣血（`UnitBase.TakeDamage` → `UnitRender.PlayHitAnimation`）；HP 归零 → `PlayDeathAnimation` → `RecycleUnit`。
- **Unit ↔ 底边 Border**：`SimpleUnit` 在每次 Step 位移到达目标时检测 → `OnUnitReachBottom` → `unit.PlayReachBottomAnimation()` → `player.TakeDamage(unit.Attack)`（触发 `PlayerRender.PlayHitAnimation` / `PlayDeathAnimation`）+ `RecycleUnit`；Player 死亡则 `EndGame()`。
- **F 发射**：`player.HandleFire` → `SpawnPinBall`，弹药 -1，触发 `PlayerRender.PlayAttackAnimation()`（DOTween 360° 旋转）。
- **预览线**：Ray-AABB Slab 求交，反射循环直至最大长度/次数，遇 Unit 或底边停止；线段使用 `DashedLine.shader` 滚动虚线材质。

### 5.5 游戏结束与重开

1. `EndGame()`：`GameState = Ended` → 清池 → `RaiseGameEnd` → UIManager 弹出 GameOver UI → UnitCreator 停止生成。
2. 玩家点击 **Restart**：`GameOverUI.OnRestartClicked` → `gameObject.SetActive(false)` → `RestartGame()` → 等同 `StartGame()`。
3. 玩家点击 **Home**：`GameOverUI.OnHomeClicked` → `gameObject.SetActive(false)` → `BackToHome()` → `GameState = Preparing` → 清池 → `RaiseReturnToHome` → UIManager 显示 StartScreen，等待玩家再次开始。

### 5.6 依赖关系简图

```
GameLogicManager  ──►  GameEvents  ◄──  UIManager（Start / GameOver / InGame / Upgrade 面板）
      │ (RaiseStep)         ▲
      │                     ├── UnitCreator  (OnStep → SpawnBatch)
      │                     ├── UnitBase*N   (OnStep → 一步位移)
      │                     └── UpgradeService (OnUnitKilled → 经验/抽卡)
      ├─► PoolManager / VfxSpawner / Difficulty / BallStats / SpecialBallParams
      ├─► Player ──► PlayerRender
      └─► Borders

PinBallBase ──► RaiseUnitKilled + VfxSpawner.PlayBallHit
InGameUI    ──► Player（HP / BallQueue）+ UpgradeService（经验）
UpgradeSelectionUI ──► OnUpgradeOffered / Applied
```

---

## 6. 场景与配置建议

### 6.1 场景中需存在

- **GameLogicManager**：绑定 `player / poolManager / vfxSpawner`（可选自动补）。
- **PoolManager**：各球种 / Unit 池与 PoolRoot / SpawnRoot。
- **UIManager**：`startScreenUI / gameOverUI / inGameUI / upgradeSelectionUI`。
- **Canvas**：StartScreen / GameOverScreen / GameHUD / UpgradeSelectionUI；默认仅 Start 显示。
- **四面 Border**（底边 `isBottomBorder`）；**Player** + **PlayerRender**；可选 **StarfieldController**。

### 6.2 预制体

- 各 `*Ball.prefab`：`PinBallBase` 派生 + `PinBallRender` + SpriteRenderer（Addressables 短地址与 `Player.ballAddress` 一致）。
- `SimpleUnit.prefab`：`SimpleUnit + UnitRender`。
- UI：`StartScreen` / `GameOverScreen` / `GameHUD` / `UpgradeSelectionUI`。

### 6.3 调参点一览

| 模块 | 参数 | 位置 |
|------|------|------|
| 玩家 | `maxHp / maxAngle / rotateSpeed / initialBallCount` | Player Inspector |
| 单位默认值 | `maxHp / attack / experience`（运行时被难度表覆盖） | SimpleUnit Prefab |
| 节奏缺省 | `UnitSize / StepDistance / StepInterval / StepMoveDuration` | `Defines.cs` |
| **难度曲线** | `startTime…stepInterval,unitExperience` | `9_Excel/Difficulty.csv` → Import；说明见 `doc/Data/DifficultyBalance.md` |
| **经验里程碑** | `experienceThreshold` + 品质权重 | `KillMilestones.csv` |
| **数值/新球词条** | Stat / NewBall CSV | `Upgrades_*.csv` → Import |
| **弹珠默认基础值** | `statType, baseValue` | `BallStatDefaults.csv` |
| 外观 | Sprite / 拖尾 / VFX 地址 | `BallSpriteSet` / `BallTrailSet` / `VfxCatalog` asset |
| 瞄准线 / HUD / 星空 | 各 Inspector 字段 | PlayerRender / InGameUI / StarfieldController |

---

## 7. 扩展与维护

- **新的 Unit 类型**：继承 `UnitBase`（或 `SimpleUnit`），重写 `HandleStep` 定义节奏响应、重写 `Tick` 推进动画；如果需要额外属性，重写 `ApplyDifficulty` 从 `Difficulty` 读自定义字段；复用同一池或新开池。
- **新的生成策略**：实现 `IUnitCreator`（例如按波次、按活跃数量阈值等），在 `GameLogicManager.Awake` 里替换 `new ...` 的实现即可；订阅所需 `GameEvents`（含 `OnStep`）自管生命周期。
- **调整节奏**：直接改 `9_Excel/Difficulty.csv` 并重新导入（`Tools/Data/Import Difficulty`）；若未配表，则改 `Defines.cs` 作为全局缺省。
- **新增数据表**：新增 `XxxData + XxxTable` 两个脚本放到 `DataSO/`、在 `DataImporter` 里加一段 `ImportXxx`、在 `9_Excel/` 放 CSV，即可接入一套新的数值管线。
- **Addressables**：已接入；新增资源需登记短地址，业务层继续只调 `AssetLoader.Load<T>`。
- **新 UI / 系统**：订阅 `GameEvents`，或挂到 `UIManager`。
- **新战斗动画 / VFX**：Render 钩子或往 `VfxCatalog` 加地址。
- **新视觉资源**：贴图 `7_Res/`、Shader `3_Shader/`；球外观改 `BallSpriteSet` / `BallTrailSet`。

---

## 8. 文档与版本

- 总览：`doc/Design/PROJECT.md`（本文档）；索引：`doc/Design/Design.md`。
- 功能：`doc/Function/*.md`；难度数值：`doc/Data/DifficultyBalance.md`。
- 后续增删脚本或改 CSV 时请同步对应文档。

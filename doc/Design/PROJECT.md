# PinBall2D 项目文档

## 1. 项目概述

**PinBall2D** 是一款基于 Unity 的 2D 弹球游戏。玩家控制一个固定位置的发射器（Player），通过旋转瞄准方向并发射弹球（PinBall），弹球在由边框（Border）围成的区域内运动、反弹，撞击消灭从屏幕上方持续生成的方块单位（Unit）。若 Unit 落到底边，会对玩家造成伤害；玩家生命值归零则游戏结束。

- **引擎**：Unity 2022.3（2D 项目）
- **产品名**：PinBall2D（见 `ProjectSettings/ProjectSettings.asset`）
- **主场景**：`Assets/Scenes/MainScene.unity`

---

## 2. 目录结构

```
PinBall2D/
├── Assets/
│   ├── 1_Scripts/                      # 游戏逻辑脚本
│   │   ├── Border.cs                  # 边框（自动对齐屏幕 / 反弹 / 底边回收）
│   │   ├── Player.cs                  # 玩家发射器（旋转、发射、生命值）
│   │   ├── PlayerRender.cs            # 玩家渲染（LineRenderer 方向预览线）
│   │   ├── Mgr/
│   │   │   ├── Defines.cs             # 项目级常量（UnitSize / Step*）
│   │   │   ├── Difficulty.cs          # 难度运行时查询（gameTime + Stage）
│   │   │   ├── GameEnum.cs            # 通用枚举（BounceDirection、GameState）
│   │   │   ├── GameEvents.cs          # 静态事件总线（生命周期事件 + OnStep）
│   │   │   ├── GameLogicManager.cs    # 单例，统一调度 Tick，受 GameState 控制
│   │   │   ├── PoolManager.cs         # PinBall/Unit 对象池与活跃列表
│   │   │   └── UIManager.cs           # 单例，订阅事件控制 UI 显隐
│   │   ├── DataSO/                    # 数据 ScriptableObject 定义
│   │   │   ├── DifficultyStageData.cs # 单个阶段数据（与 Excel 列一一对应）
│   │   │   └── DifficultyTable.cs     # 难度阶段表 SO
│   │   ├── Utility/
│   │   │   └── AssetLoader.cs         # 统一资源加载入口（Editor→AssetDatabase）
│   │   ├── Editor/
│   │   │   └── DataImporter.cs        # CSV → SO 导入菜单（Tools/Data/*）
│   │   ├── PInBall/
│   │   │   ├── PinBallBase.cs         # 弹球基类（运动、碰撞、反弹）
│   │   │   └── PinBallRender.cs       # 弹球渲染（SpriteRenderer）
│   │   ├── Unit/
│   │   │   ├── UnitBase.cs            # 单位基类（HP、Attack、订阅 OnStep、统一尺寸）
│   │   │   ├── UnitRender.cs          # 单位渲染（HP 颜色）
│   │   │   ├── IUnitCreator.cs        # 生成器接口（空标记，继承 IDisposable）
│   │   │   ├── UnitCreator.cs         # 默认生成器（订阅 OnStep 批量生成）
│   │   │   └── SimpleUnit.cs          # 简单单位：按 Step 下移 1 米 + 触底扣血
│   │   └── UI/
│   │       ├── StartScreenUI.cs       # 开始界面按钮脚本
│   │       └── GameOverUI.cs          # 游戏结束界面按钮脚本
│   ├── 2_Prefab/
│   │   └── UI/
│   │       ├── StartScreen.prefab     # 开始界面占位 prefab
│   │       └── GameOverScreen.prefab  # 结束界面占位 prefab
│   ├── 8_Data/                         # 运行时数据 Asset（由 DataImporter 生成）
│   │   └── DifficultyTable.asset
│   ├── 9_Excel/                        # 原始配置表（CSV/XLSX）
│   │   └── Difficulty.csv
│   └── Scenes/
│       └── MainScene.unity            # 主场景
├── doc/
│   ├── Design/
│   │   ├── Design.md                  # 设计概述与文档索引
│   │   └── PROJECT.md                 # 本文档
│   └── Function/                      # 功能说明（按模块）
│       ├── GamePlay.md                # 主逻辑、生命周期、事件、池
│       ├── Player.md, Border.md, Unit.md, PinBall.md
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
| `Player.cs` | 1_Scripts/ | 旋转瞄准、F 键发射、弹药容量、**生命值** |
| `PlayerRender.cs` | 1_Scripts/ | LineRenderer 方向预览线（反射/阻挡） |
| `Defines.cs` | 1_Scripts/Mgr/ | 项目级常量：UnitSize / StepDistance / StepInterval / StepMoveDuration |
| `Difficulty.cs` | 1_Scripts/Mgr/ | 难度运行时查询：gameTime 推进 + 阶段参数接口 |
| `GameEnum.cs` | 1_Scripts/Mgr/ | 通用枚举：BounceDirection、GameState |
| `GameEvents.cs` | 1_Scripts/Mgr/ | 静态事件总线：生命周期事件 + `OnStep` 节奏心跳 |
| `DifficultyStageData.cs` | 1_Scripts/DataSO/ | 单阶段数据结构（字段对齐 CSV 列） |
| `DifficultyTable.cs` | 1_Scripts/DataSO/ | 难度阶段列表 ScriptableObject |
| `AssetLoader.cs` | 1_Scripts/Utility/ | 统一资源加载入口（Editor→AssetDatabase；将来 Addressables） |
| `DataImporter.cs` | 1_Scripts/Editor/ | CSV→SO 导入菜单工具（Editor-only） |
| `GameLogicManager.cs` | 1_Scripts/Mgr/ | 单例，统一调度 Tick，切状态 + 清场 + 发事件 |
| `PoolManager.cs` | 1_Scripts/Mgr/ | PinBall/Unit 对象池与活跃列表 |
| `UIManager.cs` | 1_Scripts/Mgr/ | 单例，订阅事件驱动 UI 显隐 |
| `PinBallBase.cs` | 1_Scripts/PInBall/ | 弹球运动、碰撞与反弹、底边回收 |
| `PinBallRender.cs` | 1_Scripts/PInBall/ | 弹球外观渲染 |
| `UnitBase.cs` | 1_Scripts/Unit/ | 单位基类，HP、**Attack**、统一 1x1 尺寸、订阅 `OnStep` |
| `UnitRender.cs` | 1_Scripts/Unit/ | 单位外观，按 HP 比例变色 |
| `IUnitCreator.cs` | 1_Scripts/Unit/ | 生成器接口（空标记，继承 `IDisposable`） |
| `UnitCreator.cs` | 1_Scripts/Unit/ | 默认生成器实现（订阅 `OnStep` 批量生成） |
| `SimpleUnit.cs` | 1_Scripts/Unit/ | 最简单 Unit：按 Step 0.2s 平滑下移 1 米 + 触底回调 |
| `StartScreenUI.cs` | 1_Scripts/UI/ | 开始界面点击回调 → `StartGame()` |
| `GameOverUI.cs` | 1_Scripts/UI/ | 结束界面 Restart / Home 按钮回调 |

---

## 3. 核心架构原则

### 3.1 统一 Tick 驱动

所有需要逐帧更新的游戏对象（Player、PinBall、Unit）**不持有独立的 `Update`**，由 **GameLogicManager.UpdateGame()** 统一按固定顺序调用 `Tick`。UnitCreator 完全由事件驱动，不参与 Tick。处于缓存池内（已隐藏）的物体不参与 Tick。

### 3.2 游戏状态（GameState）

- **GameState** 枚举：`Preparing`（准备中）、`Running`（运行中）、`Paused`（暂停）、`Ended`（结束）。
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
  - 由 `GameLogicManager.Awake()` 通过 `AssetLoader.Load<DifficultyTable>("8_Data/DifficultyTable.asset")` 加载 SO 并 `new Difficulty(table)`。
  - `StartGame()` 调用 `Reset()` 归零 `gameTime`；`UpdateGame()` 每帧 `Tick(Time.deltaTime)`。
  - 对外暴露：`GetSpawnRange() / GetUnitHp() / GetUnitAttack() / GetStepInterval()`。所有查询都根据 `gameTime` 匹配 `DifficultyTable.GetStageAt(gameTime)` 返回的阶段。
- **调用点**：
  - `UnitCreator.SpawnBatch`：用 `GetSpawnRange()` 取当前阶段生成区间（再以屏幕可容纳数夹紧，避免越界）。
  - `UnitBase.Init()` → `ApplyDifficulty()`：用 `GetUnitHp() / GetUnitAttack()` 覆盖 Inspector 默认值。
  - `GameLogicManager.UpdateGame`：用 `GetStepInterval()` 动态决定 Step 心跳周期。
- **容错**：若 `DifficultyTable.asset` 不存在（未运行导入）或为空，`Difficulty.HasTable == false`，各查询返回保守兜底值（spawn=(1,1)、hp=1、attack=1、interval=`Defines.StepInterval`），保持游戏可运行但无难度曲线。
- **扩展**：新增一张表只需新增一对 SO（`xxxData` + `xxxTable`）、一段 `DataImporter.ImportXxx` 方法、一个 CSV 文件，其它层无需修改。

### 3.4.2 资源加载（AssetLoader）

- 所有动态资源加载统一走 `AssetLoader.Load<T>(relativePath)`（相对 `Assets/`）。
- 当前实现：`#if UNITY_EDITOR` 下走 `AssetDatabase.LoadAssetAtPath`；非 Editor 打印错误并返回 null，等待后续接入 **Addressables**（集成时仅在此类内部替换实现，业务调用点不变）。

### 3.5 UI 与事件

- **UIManager** 单例持有 `startScreenUI` / `gameOverUI` 根节点。
- 监听 `OnGameStart` → 隐藏两个 UI；`OnGameEnd` → 显示 GameOver UI；`OnReturnToHome` → 显示 StartScreen、隐藏 GameOver。
- **StartScreenUI / GameOverUI**：按钮回调分别调用 `GameLogicManager.StartGame / RestartGame / BackToHome`；点击后各自隐藏自身 GameObject，剩下的由事件订阅方接力完成。

---

## 4. 核心脚本详解

### 4.1 GameEnum.cs — 通用枚举

- 路径：`Assets/1_Scripts/Mgr/GameEnum.cs`
- `BounceDirection`：Up / Down / Left / Right，供 Border 指定反弹法线。
- `GameState`：Preparing / Running / Paused / Ended。

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
- 方法：`Reset() / Tick(dt) / GetSpawnRange() / GetUnitHp() / GetUnitAttack() / GetStepInterval() / HasTable`。
- 查询逻辑：基于 `DifficultyTable.GetStageAt(gameTime)`，遍历已排序 stages 返回 `startTime <= gameTime` 的最后一个阶段；表为空时 `HasTable == false`，调用方保留默认值。

### 4.1.3 DataSO — 数据 ScriptableObject

- 路径：`Assets/1_Scripts/DataSO/`
- `DifficultyStageData`：一个 `[Serializable]` 类，字段：`startTime / spawnMin / spawnMax / unitHp / unitAttack / stepInterval`。
- `DifficultyTable`：ScriptableObject，内部 `List<DifficultyStageData> stages` + `SetStages / GetStageAt / StageCount`。`CreateAssetMenu` 名字为 `PinBall2D/Data/DifficultyTable`。

### 4.1.4 AssetLoader.cs — 资源加载入口

- 路径：`Assets/1_Scripts/Utility/AssetLoader.cs`
- 单方法：`T Load<T>(string relativePath)`，路径相对 `Assets/`。
- 当前 Editor 下走 `AssetDatabase.LoadAssetAtPath<T>`；非 Editor 返回 null 并报错（等待接入 Addressables）。业务层调用不变。

### 4.1.5 DataImporter.cs — Excel 导入工具（Editor-only）

- 路径：`Assets/1_Scripts/Editor/DataImporter.cs`
- 菜单：`Tools/Data/Import All`、`Tools/Data/Import Difficulty`。
- 读取 `9_Excel/Difficulty.csv`（首行表头、跳过），解析为 `DifficultyStageData` 列表写入 `8_Data/DifficultyTable.asset`（不存在时自动 `CreateAsset`；`Import All` 会 `SaveAssets + Refresh`）。
- 将来替换为 xlsx 读取时（EPPlus / NPOI），只改此处即可。

### 4.2 GameEvents.cs — 事件总线

- 路径：`Assets/1_Scripts/Mgr/GameEvents.cs`
- 6 个静态事件：`OnGameStart / OnGamePause / OnGameResume / OnGameEnd / OnReturnToHome / OnStep`。
- 对应 `Raise*` 静态方法供 `GameLogicManager` 调用。
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
| References | `playerRender` | 场景中的 PlayerRender |
| References | `poolManager` | 场景中的 PoolManager |
| Game State | `gameState` | 当前游戏状态（Preparing/Running/Paused/Ended） |

UI 引用已移至 `UIManager`；单位生成配置已移至 `UnitCreator` 内部常量。

#### 核心字段

- `borders`：`Border[]`，在 `StartGame()` 时通过 `FindObjectsByType<Border>` 收集。
- `unitCreator`：`IUnitCreator` 接口引用；`Awake` 时 `new UnitCreator()`，`OnDestroy` 时 `Dispose`。Manager 不再调用其任何方法，只负责生命周期。
- `difficulty`：`Difficulty` 实例；`Awake` 时通过 `AssetLoader` 加载 `DifficultyTable` 并 `new Difficulty(table)`；`StartGame` 调用 `Reset()`；`UpdateGame` 每帧 `Tick(dt)`。对外通过 `Difficulty` 属性暴露。
- `stepTimer`：Step 心跳累加器；`StartGame()` 重置，`UpdateGame` 每帧累加并按 `difficulty.GetStepInterval()`（无表则退回 `Defines.StepInterval`）触发 `RaiseStep`，支持单帧内连续补齐。
- `ActivePinBalls / ActiveUnits`：转发自 `poolManager`。
- `Player`：只读属性。

#### 主要方法

| 方法 | 说明 |
|------|------|
| `StartGame()` | 设 Preparing → 收集 Border、`player.Init()`、清池并注册场景 Unit → 设 Running，`RaiseGameStart` |
| `PauseGame() / ResumeGame()` | Running ↔ Paused 切换，并广播 `OnGamePause / OnGameResume` |
| `UpdateGame()` | 仅 Running 时每帧调用；内含 Step 心跳推进 |
| `EndGame()` | 设 Ended → 清场 → `RaiseGameEnd` |
| `RestartGame()` | 直接调用 `StartGame()` |
| `BackToHome()` | 设 Preparing → 清场 → `RaiseReturnToHome` |
| `OnUnitReachBottom(unit)` | 对 Player 扣 `unit.Attack` 血 → 回收 Unit → Player 死亡则 `EndGame()` |
| `SpawnPinBall / RecyclePinBall / SpawnUnit / RecycleUnit` | 转发到 PoolManager（`RecyclePinBall` 额外调用 `player.AddPinBall`） |

**生命周期**：`Awake`（设 Instance、`new UnitCreator()`） → 等待 `StartScreenUI` 点击开始 → `StartGame()` → 每帧 `Update()` 仅在 **Running** 时 `UpdateGame()` → `OnDestroy`（Dispose UnitCreator）。

### 4.5 UIManager.cs — UI 管理器（单例）

- 挂在场景中的一个独立 GameObject 上，Inspector 绑定 `startScreenUI` / `gameOverUI` 根节点。
- `Awake` 订阅、`OnDestroy` 取消订阅 `OnGameStart / OnGameEnd / OnReturnToHome`。
- 对应回调负责 `SetActive(true/false)`。

### 4.6 PoolManager.cs — 缓存池管理器

- **职责**：PinBall / Unit 对象池 + 活跃列表；不处理游戏规则。

#### Inspector 配置

| 分组 | 字段 | 说明 |
|------|------|------|
| Prefabs | `pinBallPrefab` / `unitPrefab` | 对应预制体 |
| Pool Roots | `pinBallPoolRoot` / `unitPoolRoot` | 缓存根（可选，不设则自动创建） |
| PinBall Pool | `pinBallPoolDefaultCapacity / pinBallPoolMaxSize` | 默认 20 / 50 |
| Unit Pool | `unitPoolDefaultCapacity / unitPoolMaxSize` | 默认 20 / 100 |

#### 主要方法

| 方法 | 说明 |
|------|------|
| `ClearActivePinBalls / ClearActiveUnits` | 清空并回收所有活跃对象 |
| `RegisterExistingUnit(unit)` | 把场景里已摆好的 Unit 注入活跃列表 |
| `SpawnPinBall / SpawnUnit` | 从池取出、设置位置并 Init、加入活跃列表 |
| `RecyclePinBall / RecycleUnit` | 从活跃列表移除并 Release |

**生命周期**：`Awake` → `InitPools()`；`OnDestroy` → 两个池 `Dispose()`。

### 4.7 PinBallBase.cs — 弹球基类

- **职责**：运动、与 Border / Unit 碰撞与镜面反弹、底边回收。
- 可配置：`initialSpeed`、`minSpeed`、`bounceSpeedMultiplier`。
- 核心：`Init(direction, speed)`、`Tick(borders, activeUnits)`、`Velocity`、`Radius`。

### 4.8 PinBallRender.cs — 弹球渲染

- 弹球外观，预留 `Tick()` 扩展（轨迹、变色等）。

### 4.9 Player.cs — 玩家发射器

- **职责**：固定位置，A/D 旋转（±80°），F 发射弹球，管理弹药容量与**生命值**。
- 可配置：`rotateSpeed`、`maxAngle`、`maxPinBallCount`、`fireInterval`、`firePinBallSpeed`、`maxHp`。
- 核心：`Init()` 重置角度/弹药/HP；`Tick()`（旋转 + 发射 + 冷却）；`Direction`；`AddPinBall(count)`；`TakeDamage(damage)`；`IsDead`。

### 4.10 PlayerRender.cs — 玩家渲染（方向预览线）

- LineRenderer 从 Player 沿 `Direction` 绘制预览线，遇 Border 反射、遇 Unit 或底边停止。
- 可配置：`player`、`lineRenderer`、`maxLineLength`、`maxBounces`。

### 4.11 UnitBase.cs — 单位基类

- **职责**：单位通用属性与行为。HP、**Attack**（触底对 Player 造成的伤害）、统一 1x1 正方形尺寸（`Defines.UnitSize`）、碰撞矩形 `UnitRect`、碰撞法线、订阅 `OnStep`。
- 可配置：`maxHp`、`attack`（运行时会被 `Difficulty` 覆盖；尺寸固定由 `Defines.UnitSize` 决定）。
- 核心：
  - `Init()`：先 `ApplyDifficulty()` 读当前阶段的 hp/attack → 重置 HP → 强制 `transform.localScale = Vector3.one * Defines.UnitSize` → 刷新 `UnitRect`。
  - `ApplyDifficulty()`（virtual）：从 `GameLogicManager.Instance.Difficulty` 读 `GetUnitHp/GetUnitAttack` 覆盖字段；`HasTable == false` 时保留 Inspector 默认值。
  - `RefreshRect()`：基于位置 + `Defines.UnitSize` 计算矩形。
  - `Width / Height`：始终返回 `Defines.UnitSize`。
  - `OnEnable / OnDisable`：订阅 / 取消订阅 `GameEvents.OnStep`，调用虚方法 `HandleStep()`。
  - 虚方法 `Tick()`、`HandleStep()` / `TakeDamage(damage)` / `GetCollisionNormal(circleCenter)`。

### 4.12 UnitRender.cs — 单位渲染

- 按当前 HP 比例在灰色与原始颜色间 Lerp。

### 4.13 IUnitCreator.cs — 单位生成器接口

- **空标记接口**，继承 `IDisposable`，不定义任何方法。
- 存在意义：`GameLogicManager` 通过接口持有引用，便于在扩展时整体替换实现。

### 4.14 UnitCreator.cs — 默认单位生成器

- 纯 C# 类，实现 `IUnitCreator`（继承自 `IDisposable`）。
- 内置常量：`HorizontalPadding / TopOffset`（节奏相关常量来自 `Defines`）。
- 构造函数订阅 `OnGameStart / OnGamePause / OnGameResume / OnGameEnd / OnReturnToHome / OnStep`，`Dispose` 时全部取消订阅。
- **无 `Tick`**：完全由事件驱动。`OnStep` 回调在运行中且未暂停时调用 `SpawnBatch`：
  - 以相机视口计算可用宽度（左右各留 `HorizontalPadding`）；
  - `maxCount = floor(availWidth / Defines.UnitSize)`；
  - 若 `Difficulty.HasTable`，从 `GetSpawnRange()` 取 `[spawnMin, spawnMax]` 并用 `maxCount` 夹紧；否则退回 `[1, maxCount]`；
  - 在该区间随机 count，把宽度均分为 count 个槽；
  - 每个槽内再随机 X，保证相邻 Unit 既不重叠也不越出屏幕。

### 4.15 SimpleUnit.cs — 简单下落单位

- 继承 `UnitBase`。
- **节奏响应**：`HandleStep()` 记录起点与 `target = start + Vector2.down * Defines.StepDistance`，开启一次位移动画。
- **动画推进**：`Tick()` 累加 `moveTimer`，`t = moveTimer / Defines.StepMoveDuration` 截断到 [0,1]，用 `Vector2.Lerp` 更新位置并 `RefreshRect`。
- **触底**：到达目标（t == 1）那一帧检查是否与底边 Border 重叠，若是则调用 `GameLogicManager.OnUnitReachBottom(this)`（扣血 + 回收 + 死亡判定）。

### 4.16 StartScreenUI.cs — 开始界面

- 挂在 `StartScreen.prefab` 根节点上，持有 `startButton`。
- 点击按钮：`gameObject.SetActive(false)` 隐藏自身 + 调用 `GameLogicManager.StartGame()`。

### 4.17 GameOverUI.cs — 结束界面

- 挂在 `GameOverScreen.prefab` 根节点上，持有 `restartButton` / `homeButton`。
- Restart：隐藏自身 + `RestartGame()`。
- Home：隐藏自身 + `BackToHome()`。

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
| 2 | `player.Tick()`（处理旋转、发射、冷却） |
| 3 | 推进 `stepTimer`，每满 `Defines.StepInterval` 触发一次 `GameEvents.RaiseStep()`（同帧驱动 UnitCreator 生成 + 所有 Unit 启动位移） |
| 4 | 逆向遍历 `ActivePinBalls`，每项 `Tick(borders, activeUnits)` |
| 5 | 逆向遍历 `ActiveUnits`，每项 `Tick()`（`SimpleUnit` 内部推进 Step 位移插值 + 到达目标时触底回调） |
| 6 | `playerRender.Tick()`（绘制预览线） |

### 5.4 碰撞与交互

- **PinBall ↔ Border**：反射；底边则 `RecyclePinBall` 并 `player.AddPinBall()`。
- **PinBall ↔ Unit**：反射 + 扣血（`TakeDamage`）；HP 归零 → `RecycleUnit`。
- **Unit ↔ 底边 Border**：`SimpleUnit` 在每次 Step 位移到达目标时检测 → `OnUnitReachBottom` → Player `TakeDamage(unit.Attack)` + `RecycleUnit`；Player 死亡则 `EndGame()`。
- **F 发射**：`player.HandleFire` → `SpawnPinBall`，弹药 -1。
- **预览线**：Ray-AABB Slab 求交，反射循环直至最大长度/次数，遇 Unit 或底边停止。

### 5.5 游戏结束与重开

1. `EndGame()`：`GameState = Ended` → 清池 → `RaiseGameEnd` → UIManager 弹出 GameOver UI → UnitCreator 停止生成。
2. 玩家点击 **Restart**：`GameOverUI.OnRestartClicked` → `gameObject.SetActive(false)` → `RestartGame()` → 等同 `StartGame()`。
3. 玩家点击 **Home**：`GameOverUI.OnHomeClicked` → `gameObject.SetActive(false)` → `BackToHome()` → `GameState = Preparing` → 清池 → `RaiseReturnToHome` → UIManager 显示 StartScreen，等待玩家再次开始。

### 5.6 依赖关系简图

```
GameLogicManager  ──►  GameEvents  ◄──  UIManager
      │ (RaiseStep)         ▲              (显隐 UI)
      │                     ├── UnitCreator  (OnStep → SpawnBatch)
      │                     └── UnitBase*N   (OnStep → 启动一步位移)
      ├─► PoolManager（池 + 活跃列表）
      ├─► Player / PlayerRender
      ├─► Borders
      └─► IUnitCreator (UnitCreator，仅持有用于 Dispose)

StartScreenUI ──► GameLogicManager.StartGame
GameOverUI    ──► GameLogicManager.RestartGame / BackToHome
SimpleUnit    ──► GameLogicManager.OnUnitReachBottom
```

---

## 6. 场景与配置建议

### 6.1 场景中需存在

- **GameLogicManager**：GameObject，Inspector 绑定 `player / playerRender / poolManager / gameState`。
- **PoolManager**：同上或单独 GameObject，配置 PinBall / Unit 预制体与池参数。
- **UIManager**：单独 GameObject，Inspector 绑定 `startScreenUI / gameOverUI` 两个 UI 根节点。
- **Canvas + StartScreen + GameOverScreen**：两个 UI prefab 放到 Canvas 下。默认 `StartScreen` 显示，`GameOverScreen` 隐藏。
- **四面 Border**：开启 `autoAlignToCameraEdge` 自动贴屏幕；底边勾选 `isBottomBorder`。
- **Player / PlayerRender**：各一。
- 场景中可不摆 Unit，运行时由 UnitCreator 持续生成。

### 6.2 预制体

- **PinBall**：`PinBallBase + PinBallRender + SpriteRenderer`。
- **Unit**（如 `SimpleUnit`）：`SimpleUnit + UnitRender + SpriteRenderer`，Inspector 设置 `maxHp / attack`（尺寸会被 `Init()` 强制统一为 `Defines.UnitSize`）。
- **StartScreen / GameOverScreen**：UI prefab，各自挂 `StartScreenUI` / `GameOverUI`。

### 6.3 调参点一览

| 模块 | 参数 | 位置 |
|------|------|------|
| 玩家 | `maxHp / maxPinBallCount / fireInterval / firePinBallSpeed / maxAngle / rotateSpeed` | Player Inspector |
| 单位（默认值，运行时被难度表覆盖） | `maxHp / attack` | SimpleUnit Prefab Inspector |
| 节奏 / 尺寸（缺省） | `UnitSize / StepDistance / StepInterval / StepMoveDuration` | `Mgr/Defines.cs` 常量 |
| **难度曲线**（主调参入口） | 每阶段 `startTime / spawnMin / spawnMax / unitHp / unitAttack / stepInterval` | `Assets/9_Excel/Difficulty.csv` → `Tools/Data/Import Difficulty` 导入 |
| 生成范围 | `HorizontalPadding / TopOffset` | `UnitCreator.cs` 常量 |
| 弹球 | `initialSpeed / minSpeed / bounceSpeedMultiplier` | PinBall Prefab Inspector |
| 边框 | `bounceDirection / isBottomBorder / autoAlignToCameraEdge / thickness` | 各 Border Inspector |

---

## 7. 扩展与维护

- **新的 Unit 类型**：继承 `UnitBase`（或 `SimpleUnit`），重写 `HandleStep` 定义节奏响应、重写 `Tick` 推进动画；如果需要额外属性，重写 `ApplyDifficulty` 从 `Difficulty` 读自定义字段；复用同一池或新开池。
- **新的生成策略**：实现 `IUnitCreator`（例如按波次、按活跃数量阈值等），在 `GameLogicManager.Awake` 里替换 `new ...` 的实现即可；订阅所需 `GameEvents`（含 `OnStep`）自管生命周期。
- **调整节奏**：直接改 `9_Excel/Difficulty.csv` 并重新导入（`Tools/Data/Import Difficulty`）；若未配表，则改 `Defines.cs` 作为全局缺省。
- **新增数据表**：新增 `XxxData + XxxTable` 两个脚本放到 `DataSO/`、在 `DataImporter` 里加一段 `ImportXxx`、在 `9_Excel/` 放 CSV，即可接入一套新的数值管线。
- **接入 Addressables**：只需替换 `AssetLoader.Load<T>` 内部实现（改成 `Addressables.LoadAssetAsync<T>().WaitForCompletion()` 或异步版），业务层零改动。
- **新的 UI（例如暂停面板、结算分数）**：新增 MonoBehaviour，订阅 `GameEvents` 自管显隐；或把引用加进 `UIManager` 并在对应回调里 `SetActive`。
- **新的系统（音效 / 特效 / 关卡 / 分数）**：直接订阅 `GameEvents`，无需改动 `GameLogicManager`。

---

## 8. 文档与版本

- 本文档：`doc/Design/PROJECT.md`。
- 基于当前脚本目录与逻辑整理，后续若增删脚本或目录请同步更新本文档。

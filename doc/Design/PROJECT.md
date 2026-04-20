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
│   │   │   ├── GameEnum.cs            # 通用枚举（BounceDirection、GameState）
│   │   │   ├── GameEvents.cs          # 静态事件总线（游戏生命周期事件）
│   │   │   ├── GameLogicManager.cs    # 单例，统一调度 Tick，受 GameState 控制
│   │   │   ├── PoolManager.cs         # PinBall/Unit 对象池与活跃列表
│   │   │   └── UIManager.cs           # 单例，订阅事件控制 UI 显隐
│   │   ├── PInBall/
│   │   │   ├── PinBallBase.cs         # 弹球基类（运动、碰撞、反弹）
│   │   │   └── PinBallRender.cs       # 弹球渲染（SpriteRenderer）
│   │   ├── Unit/
│   │   │   ├── UnitBase.cs            # 单位基类（HP、Attack、碰撞法线）
│   │   │   ├── UnitRender.cs          # 单位渲染（HP 颜色）
│   │   │   ├── IUnitCreator.cs        # 生成器接口（只定义 Tick）
│   │   │   ├── UnitCreator.cs         # 默认生成器（订阅事件、按间隔生成）
│   │   │   └── SimpleUnit.cs          # 简单单位：下落 + 触底扣血
│   │   └── UI/
│   │       ├── StartScreenUI.cs       # 开始界面按钮脚本
│   │       └── GameOverUI.cs          # 游戏结束界面按钮脚本
│   ├── 2_Prefab/
│   │   └── UI/
│   │       ├── StartScreen.prefab     # 开始界面占位 prefab
│   │       └── GameOverScreen.prefab  # 结束界面占位 prefab
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
| `GameEnum.cs` | 1_Scripts/Mgr/ | 通用枚举：BounceDirection、GameState |
| `GameEvents.cs` | 1_Scripts/Mgr/ | 静态事件总线：游戏生命周期事件 |
| `GameLogicManager.cs` | 1_Scripts/Mgr/ | 单例，统一调度 Tick，切状态 + 清场 + 发事件 |
| `PoolManager.cs` | 1_Scripts/Mgr/ | PinBall/Unit 对象池与活跃列表 |
| `UIManager.cs` | 1_Scripts/Mgr/ | 单例，订阅事件驱动 UI 显隐 |
| `PinBallBase.cs` | 1_Scripts/PInBall/ | 弹球运动、碰撞与反弹、底边回收 |
| `PinBallRender.cs` | 1_Scripts/PInBall/ | 弹球外观渲染 |
| `UnitBase.cs` | 1_Scripts/Unit/ | 单位基类，HP、**Attack**、碰撞法线 |
| `UnitRender.cs` | 1_Scripts/Unit/ | 单位外观，按 HP 比例变色 |
| `IUnitCreator.cs` | 1_Scripts/Unit/ | 生成器接口（仅 Tick） |
| `UnitCreator.cs` | 1_Scripts/Unit/ | 默认生成器实现（订阅事件、按间隔生成） |
| `SimpleUnit.cs` | 1_Scripts/Unit/ | 最简单 Unit：下落、触底回调 |
| `StartScreenUI.cs` | 1_Scripts/UI/ | 开始界面点击回调 → `StartGame()` |
| `GameOverUI.cs` | 1_Scripts/UI/ | 结束界面 Restart / Home 按钮回调 |

---

## 3. 核心架构原则

### 3.1 统一 Tick 驱动

所有游戏对象（Player、PinBall、Unit、UnitCreator）**不持有独立的 `Update`**，由 **GameLogicManager.UpdateGame()** 统一按固定顺序调用 `Tick`。处于缓存池内（已隐藏）的物体不参与 Tick。

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

**发送方**：只有 `GameLogicManager`。
**典型订阅方**：`UIManager`（UI 显隐）、`UnitCreator`（生成节奏）。未来可以加入音效管理、关卡管理等都无需改动 `GameLogicManager`。

### 3.4 对象池（PoolManager）

PinBall 与 Unit 的缓存池由独立组件 **PoolManager** 管理，使用 `UnityEngine.Pool.ObjectPool<T>`：

- **入池**：`SetActive(false)` + `SetParent(poolRoot)`。
- **出池**：`SetParent(null)` + `SetActive(true)`，加入活跃列表参与 Tick。
- 池根节点若未在 Inspector 指定，`Awake` 时自动创建。
- `GameLogicManager` 通过引用调用 `SpawnPinBall / RecyclePinBall / SpawnUnit / RecycleUnit`；弹球回收时额外调用 `player.AddPinBall()` 补充弹药。

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

### 4.2 GameEvents.cs — 事件总线

- 路径：`Assets/1_Scripts/Mgr/GameEvents.cs`
- 5 个静态事件：`OnGameStart / OnGamePause / OnGameResume / OnGameEnd / OnReturnToHome`。
- 对应 `Raise*` 静态方法供 `GameLogicManager` 调用。

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
- `unitCreator`：`IUnitCreator` 接口引用；`Awake` 时 `new UnitCreator()`，`OnDestroy` 时 `Dispose`。
- `ActivePinBalls / ActiveUnits`：转发自 `poolManager`。
- `Player`：只读属性。

#### 主要方法

| 方法 | 说明 |
|------|------|
| `StartGame()` | 设 Preparing → 收集 Border、`player.Init()`、清池并注册场景 Unit → 设 Running，`RaiseGameStart` |
| `PauseGame() / ResumeGame()` | Running ↔ Paused 切换，并广播 `OnGamePause / OnGameResume` |
| `UpdateGame()` | 仅 Running 时每帧调用 |
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

- **职责**：单位通用属性与行为。HP、**Attack**（触底对 Player 造成的伤害）、碰撞矩形 `UnitRect`、碰撞法线。
- 可配置：`maxHp`、`attack`。
- 核心：`Init()` / `RefreshRect()` / 虚方法 `Tick()` / `TakeDamage(damage)` / `GetCollisionNormal(circleCenter)`。

### 4.12 UnitRender.cs — 单位渲染

- 按当前 HP 比例在灰色与原始颜色间 Lerp。

### 4.13 IUnitCreator.cs — 单位生成器接口

- 只定义 `void Tick()`。生命周期响应由实现类自行订阅 `GameEvents`。

### 4.14 UnitCreator.cs — 默认单位生成器

- 纯 C# 类，实现 `IUnitCreator + IDisposable`。
- 内置常量：`SpawnInterval / InitialDelay / HorizontalPadding / TopOffset`。
- 构造函数订阅 `OnGameStart / OnGamePause / OnGameResume / OnGameEnd / OnReturnToHome`，`Dispose` 时取消订阅。
- `Tick()`：运行中且未暂停时推进计时，到期从屏幕顶部随机 X 位置调用 `GameLogicManager.SpawnUnit`。

### 4.15 SimpleUnit.cs — 简单下落单位

- 继承 `UnitBase`。`Tick()` 中以 `moveSpeed` 向下移动并 `RefreshRect`。
- 与底边 Border 相交时调用 `GameLogicManager.OnUnitReachBottom(this)`（扣血 + 回收 + 死亡判定）。

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
| 3 | `unitCreator.Tick()`（到期时生成新 Unit） |
| 4 | 逆向遍历 `ActivePinBalls`，每项 `Tick(borders, activeUnits)` |
| 5 | 逆向遍历 `ActiveUnits`，每项 `Tick()`（`SimpleUnit` 下落 + 触底回调） |
| 6 | `playerRender.Tick()`（绘制预览线） |

### 5.4 碰撞与交互

- **PinBall ↔ Border**：反射；底边则 `RecyclePinBall` 并 `player.AddPinBall()`。
- **PinBall ↔ Unit**：反射 + 扣血（`TakeDamage`）；HP 归零 → `RecycleUnit`。
- **Unit ↔ 底边 Border**：`SimpleUnit` 检测 → `OnUnitReachBottom` → Player `TakeDamage(unit.Attack)` + `RecycleUnit`；Player 死亡则 `EndGame()`。
- **F 发射**：`player.HandleFire` → `SpawnPinBall`，弹药 -1。
- **预览线**：Ray-AABB Slab 求交，反射循环直至最大长度/次数，遇 Unit 或底边停止。

### 5.5 游戏结束与重开

1. `EndGame()`：`GameState = Ended` → 清池 → `RaiseGameEnd` → UIManager 弹出 GameOver UI → UnitCreator 停止生成。
2. 玩家点击 **Restart**：`GameOverUI.OnRestartClicked` → `gameObject.SetActive(false)` → `RestartGame()` → 等同 `StartGame()`。
3. 玩家点击 **Home**：`GameOverUI.OnHomeClicked` → `gameObject.SetActive(false)` → `BackToHome()` → `GameState = Preparing` → 清池 → `RaiseReturnToHome` → UIManager 显示 StartScreen，等待玩家再次开始。

### 5.6 依赖关系简图

```
GameLogicManager  ──►  GameEvents  ◄──  UIManager
      │                    ▲              (显隐 UI)
      │                    │
      ├─► PoolManager（池 + 活跃列表）
      ├─► Player / PlayerRender
      ├─► Borders
      └─► IUnitCreator (UnitCreator 订阅 GameEvents)

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
- **Unit**（如 `SimpleUnit`）：`SimpleUnit + UnitRender + SpriteRenderer`，Inspector 设置 `maxHp / attack / moveSpeed`。
- **StartScreen / GameOverScreen**：UI prefab，各自挂 `StartScreenUI` / `GameOverUI`。

### 6.3 调参点一览

| 模块 | 参数 | 位置 |
|------|------|------|
| 玩家 | `maxHp / maxPinBallCount / fireInterval / firePinBallSpeed / maxAngle / rotateSpeed` | Player Inspector |
| 单位 | `maxHp / attack / moveSpeed` | SimpleUnit Prefab Inspector |
| 生成节奏 | `SpawnInterval / InitialDelay / HorizontalPadding / TopOffset` | `UnitCreator.cs` 常量 |
| 弹球 | `initialSpeed / minSpeed / bounceSpeedMultiplier` | PinBall Prefab Inspector |
| 边框 | `bounceDirection / isBottomBorder / autoAlignToCameraEdge / thickness` | 各 Border Inspector |

---

## 7. 扩展与维护

- **新的 Unit 类型**：继承 `UnitBase`（或 `SimpleUnit`），重写 `Tick`；复用同一池或新开池。
- **新的生成策略**：实现 `IUnitCreator`，在 `GameLogicManager.Awake` 里替换 `new ...` 的实现即可；订阅所需生命周期事件。
- **新的 UI（例如暂停面板、结算分数）**：新增 MonoBehaviour，订阅 `GameEvents` 自管显隐；或把引用加进 `UIManager` 并在对应回调里 `SetActive`。
- **新的系统（音效 / 特效 / 关卡 / 分数）**：直接订阅 `GameEvents`，无需改动 `GameLogicManager`。

---

## 8. 文档与版本

- 本文档：`doc/Design/PROJECT.md`。
- 基于当前脚本目录与逻辑整理，后续若增删脚本或目录请同步更新本文档。

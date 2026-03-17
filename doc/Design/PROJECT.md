# PinBall2D 项目文档

## 1. 项目概述

**PinBall2D** 是一款基于 Unity 的 2D 弹球游戏。玩家控制一个固定位置的发射器（Player），通过旋转瞄准方向并发射弹球（PinBall），弹球在由边框（Border）围成的区域内运动、反弹，撞击消灭场上的方块单位（Unit）。弹球触底后回收并自动补充弹药。

- **引擎**：Unity 2022.3（2D 项目）
- **产品名**：PinBall2D（见 `ProjectSettings/ProjectSettings.asset`）
- **主场景**：`Assets/Scenes/MainScene.unity`

---

## 2. 目录结构

```
PinBall2D/
├── Assets/
│   ├── 1_Scripts/                  # 游戏逻辑脚本
│   │   ├── Border.cs              # 边框（矩形障碍，反弹/底边回收）
│   │   ├── Player.cs              # 玩家发射器（旋转、发射弹球）
│   │   ├── PlayerRender.cs        # 玩家渲染（LineRenderer 方向预览线）
│   │   ├── Mgr/
│   │   │   ├── GameEnum.cs        # 通用枚举（BounceDirection、GameState）
│   │   │   ├── GameLogicManager.cs # 单例，统一调度 Tick，受游戏状态控制
│   │   │   └── PoolManager.cs     # PinBall/Unit 缓存池与活跃列表
│   │   ├── PInBall/
│   │   │   ├── PinBallBase.cs     # 弹球基类（运动、碰撞、反弹）
│   │   │   └── PinBallRender.cs   # 弹球渲染（SpriteRenderer）
│   │   └── Unit/
│   │       ├── UnitBase.cs        # 游戏单位基类（HP、碰撞法线）
│   │       └── UnitRender.cs      # 单位渲染（SpriteRenderer + HP 颜色）
│   └── Scenes/
│       └── MainScene.unity        # 主场景
├── doc/
│   ├── Design/
│   │   ├── Design.md              # 设计概述与各模块文档索引
│   │   └── PROJECT.md             # 本文档
│   └── Function/                  # 功能说明（按模块）
│       ├── GamePlay.md            # 主逻辑、游戏状态、池
│       ├── Player.md, Border.md, Unit.md, PinBall.md
├── ProjectSettings/
├── UserSettings/
├── Library/
├── Logs/
└── .gitignore
```

说明：所有通用枚举集中在 `Mgr/GameEnum.cs`（如 `BounceDirection`、`GameState`）。

### 2.1 脚本职责总览

| 脚本 | 路径 | 职责 |
|------|------|------|
| `Border.cs` | 1_Scripts/ | 矩形边框，反弹法线与底边标识 |
| `Player.cs` | 1_Scripts/ | 旋转瞄准、F 键发射弹球、弹药容量 |
| `PlayerRender.cs` | 1_Scripts/ | LineRenderer 方向预览线（反射/阻挡） |
| `GameEnum.cs` | 1_Scripts/Mgr/ | 通用枚举：BounceDirection、GameState |
| `GameLogicManager.cs` | 1_Scripts/Mgr/ | 单例，统一调度 Tick，受游戏状态控制，委托 PoolManager 管理池 |
| `PoolManager.cs` | 1_Scripts/Mgr/ | PinBall/Unit 对象池与活跃列表 |
| `PinBallBase.cs` | 1_Scripts/PInBall/ | 弹球运动、碰撞与反弹、底边回收 |
| `PinBallRender.cs` | 1_Scripts/PInBall/ | 弹球外观渲染（预留扩展） |
| `UnitBase.cs` | 1_Scripts/Unit/ | 方块单位基类，HP、碰撞法线 |
| `UnitRender.cs` | 1_Scripts/Unit/ | 单位外观，按 HP 比例变色 |

---

## 3. 核心架构原则

### 3.1 统一 Tick 驱动

所有游戏对象（Player、PinBall、Unit）**不持有独立的 `Update`**，由 **GameLogicManager.UpdateGame()** 统一调用各自的 `Tick`。处于缓存池内（已隐藏）的物体不参与 Tick。

### 3.2 游戏状态（GameState）

- **GameState** 枚举：`Preparing`（准备中）、`Running`（运行中）、`Paused`（暂停）、`Ended`（结束）。
- **主逻辑 Update**：仅当 `GameLogicManager.CurrentState == GameState.Running` 时执行 `UpdateGame()`；准备中/暂停/结束状态下不驱动 Tick。
- 通过 `SetGameState(GameState)` 或 Inspector 修改状态；`StartGame()` 会先设为 `Preparing`，初始化完成后设为 `Running`。

### 3.3 对象池（PoolManager）

PinBall 与 Unit 的缓存池由独立组件 **PoolManager** 管理，使用 `UnityEngine.Pool.ObjectPool<T>`：

- **入池**：`SetActive(false)` + `SetParent(poolRoot)`，移至专用缓存根节点下隐藏。
- **出池**：`SetParent(null)` + `SetActive(true)`，离开缓存节点并加入活跃列表，参与 Tick。
- 池根节点若未在 Inspector 指定，PoolManager 在 `Awake` 时自动创建为自身子节点。
- GameLogicManager 通过引用 PoolManager 调用 `SpawnPinBall` / `RecyclePinBall` / `SpawnUnit` / `RecycleUnit`；弹球回收后由 GameLogicManager 调用 `player.AddPinBall()` 补充弹药。

---

## 4. 核心脚本详解

### 4.1 GameEnum.cs — 通用枚举

- **路径**：`Assets/1_Scripts/Mgr/GameEnum.cs`
- **BounceDirection**：Up / Down / Left / Right，供 Border 指定反弹法线。
- **GameState**：Preparing（准备中）、Running（运行中）、Paused（暂停）、Ended（结束），供 GameLogicManager 控制是否执行每帧 Update。
- 后续新增枚举可在此文件中统一添加。

### 4.2 Border.cs — 边框

- 矩形障碍，弹球碰触后镜面反射；底边（`isBottomBorder`）时弹球不反射，回收并补充 Player 弹药。
- **可配置**：`bounceDirection`、`isBottomBorder`。**核心**：`BorderRect`、`GetNormal()`、`RefreshRect()`；Gizmos 底边红色、其余黄色。

---

### 4.3 GameLogicManager.cs — 游戏逻辑管理器（单例）

- **职责**：整局调度入口，统一驱动所有 Tick；**不直接持有对象池**，池相关操作委托给 PoolManager；**游戏状态**控制下仅在 Running 时执行 UpdateGame。
- **单例**：`Instance` 在 `Awake` 赋值，`OnDestroy` 时置空。

#### Inspector 配置

| 分组 | 字段 | 说明 |
|------|------|------|
| References | `player` | 场景中的 Player |
| References | `playerRender` | 场景中的 PlayerRender |
| References | `poolManager` | 场景中的 PoolManager（负责池与活跃列表） |
| Game State | `gameState` | 当前游戏状态（Preparing/Running/Paused/Ended），仅 Running 时执行 UpdateGame |

#### 核心数据

- `borders`：`Border[]`，在 `StartGame()` 时通过 `FindObjectsByType<Border>` 收集。
- `ActivePinBalls` / `ActiveUnits`：转发自 `poolManager.ActivePinBalls` / `poolManager.ActiveUnits`。
- `Player`：只读属性。

#### 主要方法

| 方法 | 说明 |
|------|------|
| `StartGame()` | 设状态为 Preparing；收集 Border、初始化 Player、清空池并注册场景 Unit；最后设为 Running |
| `UpdateGame()` | 每帧（**仅当 gameState == Running 时**）：刷新所有 Rect → Player.Tick → 逆向遍历活跃 PinBall/Unit 调用 Tick → PlayerRender.Tick |
| `SetGameState(state)` | 设置当前游戏状态（如 Paused、Ended） |
| `SpawnPinBall(pos, dir, speed)` | 委托 `poolManager.SpawnPinBall` |
| `RecyclePinBall(pb)` | 委托 `poolManager.RecyclePinBall`，并调用 `player.AddPinBall()` |
| `SpawnUnit(pos)` / `RecycleUnit(unit)` | 委托 PoolManager |

**生命周期**：`Awake`（单例）→ `Start` → `StartGame()`（Preparing → … → Running）→ 每帧 `Update` 仅在 **Running** 时调用 `UpdateGame()`。

---

### 4.4 PoolManager.cs — 缓存池管理器

- **职责**：PinBall 与 Unit 的对象池创建、出池/入池、活跃列表维护；不处理游戏规则（如补弹由 GameLogicManager 负责）。

#### Inspector 配置

| 分组 | 字段 | 说明 |
|------|------|------|
| Prefabs | `pinBallPrefab` | PinBall 预制体 |
| Prefabs | `unitPrefab` | Unit 预制体 |
| Pool Roots | `pinBallPoolRoot` | PinBall 缓存根（可选，不设则自动创建） |
| Pool Roots | `unitPoolRoot` | Unit 缓存根（可选，不设则自动创建） |
| PinBall Pool | `pinBallPoolDefaultCapacity` / `pinBallPoolMaxSize` | 默认 20 / 50 |
| Unit Pool | `unitPoolDefaultCapacity` / `unitPoolMaxSize` | 默认 20 / 100 |

#### 核心数据

- `activePinBalls` / `activeUnits`：当前活跃实例列表。
- 对外只读：`ActivePinBalls`、`ActiveUnits`（`IReadOnlyList`）。

#### 主要方法

| 方法 | 说明 |
|------|------|
| `ClearActivePinBalls()` | 清空活跃弹球列表（StartGame 时由 GameLogicManager 调用） |
| `ClearActiveUnits()` | 清空活跃单位列表 |
| `RegisterExistingUnit(unit)` | 将场景中已有的 Unit 加入活跃列表 |
| `SpawnPinBall(pos, dir, speed)` | 从池取出、设置位置并 Init、加入 activePinBalls |
| `RecyclePinBall(pb)` | 从 activePinBalls 移除并 Release 回池 |
| `SpawnUnit(pos)` | 从池取出、设置位置并 Init、加入 activeUnits |
| `RecycleUnit(unit)` | 从 activeUnits 移除并 Release 回池 |

**生命周期**：`Awake` → `InitPools()`；`OnDestroy` → 两个池 `Dispose()`。

---

### 4.5 PinBallBase.cs — 弹球基类

- **职责**：运动、与 Border/Unit 碰撞与镜面反弹、底边回收。
- **可配置**：`initialSpeed`、`minSpeed`、`bounceSpeedMultiplier`。
- **核心**：`Init(direction, speed)`、`Tick(borders, activeUnits)`（virtual）、`Velocity`、`Radius`。
- **Tick**：先检测 Border（底边则回收并 return），再检测 Unit（反射 + 扣血，HP 归零则 RecycleUnit），最后更新位置。

---

### 4.6 PinBallRender.cs — 弹球渲染

- **职责**：弹球外观，预留 `Tick()` 扩展（轨迹、变色等）。与 PinBallBase 同 Prefab，可绑定 `pinBall`、`spriteRenderer`。

---

### 4.7 Player.cs — 玩家发射器

- **职责**：固定位置，A/D 旋转（±80°），F 发射弹球，管理弹药容量。
- **可配置**：`rotateSpeed`、`maxAngle`、`maxPinBallCount`、`fireInterval`、`firePinBallSpeed`。
- **核心**：`Init()`、`Tick()`（旋转 + 发射 + 冷却）、`Direction`、`AddPinBall(count)`。

---

### 4.8 PlayerRender.cs — 玩家渲染（方向预览线）

- **职责**：LineRenderer 从 Player 位置沿 `Direction` 绘制预览线，遇 Border 反射、遇 Unit 或底边停止。
- **可配置**：`player`、`lineRenderer`、`maxLineLength`、`maxBounces`。
- **实现**：Ray-AABB Slab 求交，循环反射直至长度或次数用尽。

---

### 4.9 UnitBase.cs — 游戏单位基类

- **职责**：方块单位，HP，被弹球撞击扣血，HP 归零回收入池；基类可继承扩展。
- **可配置**：`maxHp`。
- **核心**：`Init()`、`RefreshRect()`、`Tick()`（virtual）、`TakeDamage(damage)`、`GetCollisionNormal(circleCenter)`、`UnitRect`。

---

### 4.10 UnitRender.cs — 单位渲染

- **职责**：按当前 HP 比例在灰色与原始颜色间 Lerp。与 UnitBase 同 Prefab，绑定 `unit`、`spriteRenderer`。

---

## 5. 游戏流程与数据流

### 5.1 初始化

1. **GameLogicManager.Awake**：设置单例。
2. **PoolManager.Awake**：`InitPools()`，创建两个 ObjectPool 及池根节点（若未指定）。
3. **GameLogicManager.Start** → **StartGame()**：设 `gameState = Preparing`；收集 `borders`、`player.Init()`、清空池并注册场景 Unit；最后设 `gameState = Running`。

### 5.2 每帧更新顺序

`GameLogicManager.Update()` → `UpdateGame()`：

| 步骤 | 操作 |
|------|------|
| 1 | 刷新所有 Border / Unit Rect |
| 2 | `player.Tick()` |
| 3 | 逆向遍历 `poolManager.ActivePinBalls`，每项 `Tick(borders, activeUnits)` |
| 4 | 逆向遍历 `poolManager.ActiveUnits`，每项 `Tick()` |
| 5 | `playerRender.Tick()` |

### 5.3 碰撞与交互

（同前：弹球↔Border 反射/底边回收、弹球↔Unit 反射+扣血、F 发射、预览线 Border 反射/Unit 与底边停止。）

### 5.4 弹药循环

Player 发射 → PinBall 出池、弹药 -1 → 弹球运动与反弹 → 触底回收入池、弹药 +1。

### 5.5 依赖关系简图

```
BounceDirection ← Border
                      ↑
GameLogicManager（borders，统一 Tick）──→ PoolManager（池 + 活跃列表）
    │                    │                        ↑
    │                    ├── ActivePinBalls → PinBallBase / PinBallRender
    │                    ├── ActiveUnits     → UnitBase / UnitRender
    │                    ├── Player（Tick，发射）
    │                    └── PlayerRender（预览线，依赖 Borders + ActiveUnits）
    └── RecyclePinBall 时调用 player.AddPinBall()
```

---

## 6. 场景与配置建议

### 6.1 场景中需存在

- **GameLogicManager**：挂在一个 GameObject 上，Inspector 中绑定 `player`、`playerRender`、`poolManager`。
- **PoolManager**：可挂在同一 GameObject 或单独节点，配置 PinBall/Unit 预制体与池参数。
- **四面 Border**：底边勾选 `isBottomBorder`。
- **一个 Player**、**一个 PlayerRender**（含 LineRenderer）。
- **若干 UnitBase**：场景中预先摆放，StartGame 时自动收集并注册到 PoolManager。

### 6.2 预制体

- PinBall：`PinBallBase` + `PinBallRender` + SpriteRenderer。
- Unit：`UnitBase` + `UnitRender` + SpriteRenderer。

### 6.3 LineRenderer

World Space，线宽与材质按需设置；`maxLineLength`、`maxBounces` 依场地调整。

---

## 7. 扩展与维护

- 新弹球/单位类型：继承 PinBallBase/UnitBase，重写 Tick；可复用同一池或新池。
- 新边框：复用 Border，调整 `BounceDirection`、`isBottomBorder`。
- 关卡：不同场景或运行时 `SpawnUnit`；UI 可读 `player.CurrentPinBallCount` 等。

---

## 8. 文档与版本

- 本文档：`doc/Design/PROJECT.md`。
- 基于当前脚本目录（`Assets/1_Scripts/` 及子目录 Mgr、PInBall、Unit）与逻辑整理，后续若增删脚本或目录请同步更新本文档。

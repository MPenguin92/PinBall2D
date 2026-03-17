# 游戏主逻辑（GamePlay）

整局游戏的调度与更新由 **GameLogicManager** 与 **PoolManager** 配合完成：前者统一驱动 Tick 并受**游戏状态**控制，后者专管 PinBall/Unit 的缓存池与活跃列表。

---

## 职责划分

| 组件 | 职责 |
|------|------|
| **GameLogicManager**（单例） | 整局入口、游戏状态、每帧仅在 **Running** 时执行 `UpdateGame()`，统一调用 Player / PinBall / Unit / PlayerRender 的 `Tick`；对外提供 `SpawnPinBall` / `RecyclePinBall` / `SpawnUnit` / `RecycleUnit`，内部委托 PoolManager，弹球回收时调用 `player.AddPinBall()` |
| **PoolManager** | PinBall 与 Unit 的**对象池**（创建、出池/入池）及**活跃列表**维护；不处理“补弹”等游戏规则 |

---

## 游戏状态（GameState）

- **通用枚举**：项目内枚举集中在 `Mgr/GameEnum.cs`，当前包含：
  - **BounceDirection**：边框反弹法线（Up / Down / Left / Right）。
  - **GameState**：游戏状态（见下）。
- **GameState 取值**：
  - **Preparing**（准备中）：初始化阶段，不执行游戏循环。
  - **Running**（运行中）：正常游戏，每帧执行 `UpdateGame()`。
  - **Paused**（暂停）：不执行 `UpdateGame()`。
  - **Ended**（结束）：不执行 `UpdateGame()`。
- **主逻辑 Update**：仅当 `GameLogicManager.CurrentState == GameState.Running` 时调用 `UpdateGame()`；其他状态下不驱动 Tick。
- **设置方式**：Inspector 中可改 `gameState`；代码中可调用 `GameLogicManager.Instance.SetGameState(GameState.Paused)` 等。`StartGame()` 会先设为 Preparing，初始化完成后设为 Running。

---

## 更新驱动（统一 Tick）

- **无独立 Update**：PinBall、Player、Unit 都不写自己的 `Update`，只实现 `Tick()` 方法。
- **统一调度**：在 **Running** 状态下，由 GameLogicManager 每帧 `UpdateGame()` 按固定顺序调用：
  1. 刷新所有 Border / Unit 的 Rect  
  2. `player.Tick()`  
  3. 逆向遍历活跃 PinBall，逐个 `Tick(borders, activeUnits)`  
  4. 逆向遍历活跃 Unit，逐个 `Tick()`  
  5. `playerRender.Tick()`  
- **隐藏不参与**：处于**缓存池内**（已隐藏、挂在池根节点下）的物体不加入活跃列表，不执行 Tick。

---

## 缓存池（PoolManager）

- **实现**：使用 Unity 自带的 `UnityEngine.Pool.ObjectPool<T>` 管理 PinBall 与 Unit。
- **入池**：物体 `SetActive(false)`，并 `SetParent(poolRoot)` 移到独立缓存根节点下。
- **出池**：物体 `SetParent(null)`、`SetActive(true)`，离开缓存节点，加入活跃列表并参与 Tick。
- **与 GameLogicManager 的关系**：GameLogicManager 持有 PoolManager 引用，所有生成/回收通过 PoolManager 的 `SpawnPinBall` / `RecyclePinBall` / `SpawnUnit` / `RecycleUnit` 完成；弹球回收时由 GameLogicManager 负责调用 `player.AddPinBall()` 补充弹药。

---

## 与项目文档的对应

- 脚本路径：`Assets/1_Scripts/Mgr/GameLogicManager.cs`、`Assets/1_Scripts/Mgr/PoolManager.cs`、`Assets/1_Scripts/Mgr/GameEnum.cs`
- 详细流程、状态与依赖见 **doc/Design/PROJECT.md** 第 3、4、5 节。

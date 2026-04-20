# 游戏主逻辑（GamePlay）

整局游戏的调度由 **GameLogicManager** 主导，配合 **PoolManager**（缓存池）、**UIManager**（界面）、**UnitCreator**（单位生成）与 **GameEvents**（事件总线）协同完成。
各模块通过事件解耦：`GameLogicManager` 只负责切状态、清场与**发事件**，其他模块订阅感兴趣的事件做对应响应。

---

## 职责划分

| 组件 | 职责 |
|------|------|
| **GameLogicManager**（单例） | 整局入口；维护 `GameState`；`UpdateGame()` 仅在 **Running** 时执行并统一驱动 Player / PinBall / Unit / UnitCreator / PlayerRender 的 `Tick`；处理 Unit 触底伤害、玩家死亡判定；发送 `GameEvents` 生命周期事件 |
| **PoolManager** | PinBall / Unit 的**对象池**（创建、出池/入池）与**活跃列表**维护 |
| **UIManager**（单例） | 场景 UI 根节点持有者；订阅 `GameEvents` 控制开始界面、游戏结束界面的显隐 |
| **UnitCreator**（纯 C# 类，实现 `IUnitCreator`） | 单位生成节奏；订阅 `GameEvents` 维护内部运行/暂停标志，由 GameLogicManager 每帧 `Tick` |
| **GameEvents**（静态） | 事件总线：定义 `OnGameStart / OnGamePause / OnGameResume / OnGameEnd / OnReturnToHome` |
| **StartScreenUI / GameOverUI** | 界面挂载脚本；按钮回调调用 `GameLogicManager.StartGame / RestartGame / BackToHome` |

---

## 生命值与攻击力

- **Player.maxHp / currentHp**：玩家有生命值，`Init()` 时重置为 `maxHp`；`TakeDamage(int)` 扣血，`IsDead` 表示是否死亡。
- **UnitBase.attack**：每个 Unit 都有攻击力字段（默认 1），在 Inspector 中可调。
- **触底伤害**：Unit 移动到底边 Border 时，不再直接回收，而是调用 `GameLogicManager.OnUnitReachBottom(unit)`：
  1. 对 Player 扣 `unit.Attack` 点血
  2. 回收该 Unit 进池
  3. 若 Player HP 归零 → 调用 `EndGame()`
- **不影响 PinBall 规则**：PinBall 触底仍然走原逻辑（回收 + 补弹），与 Unit 触底独立。

---

## 游戏生命周期与事件

整局游戏围绕 5 个事件展开，由 `GameLogicManager` 统一发送：

| 事件 | 触发时机 | 典型订阅方 |
|------|---------|-----------|
| `OnGameStart` | `StartGame()` 初始化完成后 | UIManager（隐藏所有 UI）、UnitCreator（重置计时、进入运行） |
| `OnGamePause` | `PauseGame()` 从 Running 进入 Paused | UnitCreator（暂停生成） |
| `OnGameResume` | `ResumeGame()` 从 Paused 恢复 Running | UnitCreator（恢复生成） |
| `OnGameEnd` | `EndGame()`（玩家死亡或主动结束） | UIManager（弹出 Game Over UI）、UnitCreator（停止生成） |
| `OnReturnToHome` | `BackToHome()` 从 Ended 回到 Preparing | UIManager（显示开始界面）、UnitCreator（停止生成） |

### 典型流程

1. **进入场景**：`GameState = Preparing`，`UIManager` 默认保持 StartScreen 可见。
2. **点击「开始」按钮**：`StartScreenUI.OnStartClicked` → 隐藏自身并调用 `GameLogicManager.StartGame()`。
   - `StartGame()` 初始化 borders / player / pool，`GameState = Running`，`RaiseGameStart`。
3. **Running 阶段**：`UpdateGame()` 每帧调用；`UnitCreator.Tick` 按间隔从屏幕顶部随机 X 生成 Unit。
4. **Unit 触底扣血**：`GameLogicManager.OnUnitReachBottom` 扣 Player HP。
5. **玩家死亡**：`EndGame()` → `GameState = Ended`，清空场上对象，`RaiseGameEnd` → UIManager 显示 GameOverUI。
6. **GameOver UI 操作**：
   - **Restart** → `GameLogicManager.RestartGame()`（即重新 `StartGame`）。
   - **Home** → `GameLogicManager.BackToHome()` → `GameState = Preparing`，`RaiseReturnToHome` → UIManager 显示 StartScreen。

---

## 游戏状态（GameState）

- **通用枚举**集中在 `Mgr/GameEnum.cs`，当前包含：
  - `BounceDirection`：边框反弹法线（Up / Down / Left / Right）。
  - `GameState`：游戏状态（见下）。
- **取值**：
  - **Preparing**：初始化 / 等待开始，不执行游戏循环。
  - **Running**：主逻辑运行，每帧执行 `UpdateGame()`。
  - **Paused**：主循环停推（`UnitCreator` 会响应 `OnGamePause` 停止生成）。
  - **Ended**：游戏结束，不执行 `UpdateGame()`（UI 层显示结算）。
- **切换入口**（都在 `GameLogicManager`）：
  - `StartGame()` → Preparing → Running（发 `OnGameStart`）
  - `PauseGame()` → Running → Paused（发 `OnGamePause`）
  - `ResumeGame()` → Paused → Running（发 `OnGameResume`）
  - `EndGame()` → * → Ended（发 `OnGameEnd`）
  - `BackToHome()` → * → Preparing（发 `OnReturnToHome`）

---

## 更新驱动（统一 Tick）

- **无独立 Update**：PinBall、Player、Unit、UnitCreator 都不写自己的 `Update`，只实现 `Tick()`。
- **统一调度**：在 **Running** 状态下，`GameLogicManager.UpdateGame()` 每帧按固定顺序调用：
  1. 刷新所有 Border / Unit 的 Rect
  2. `player.Tick()`
  3. `unitCreator.Tick()`（推进生成计时，必要时调用 `SpawnUnit`）
  4. 逆向遍历活跃 PinBall，逐个 `Tick(borders, activeUnits)`
  5. 逆向遍历活跃 Unit，逐个 `Tick()`（`SimpleUnit` 内部负责下落 + 触底回调）
  6. `playerRender.Tick()`
- **隐藏不参与**：处于**缓存池内**（已隐藏、挂在池根节点下）的物体不加入活跃列表，不执行 Tick。

---

## UnitCreator：单位生成

- **接口 `IUnitCreator`**：只定义 `Tick()`，生命周期响应交由实现类自行订阅 `GameEvents`。
- **默认实现 `UnitCreator`**：
  - 纯 C# 类（非 MonoBehaviour）；由 `GameLogicManager.Awake()` `new UnitCreator()` 创建，`OnDestroy()` 时 `Dispose` 取消订阅。
  - 常量内置生成参数（间隔、初始延迟、左右 padding、顶部偏移）。
  - 订阅 5 个事件：`OnGameStart` 重置计时并进入运行；`OnGamePause/Resume` 切换暂停标志；`OnGameEnd / OnReturnToHome` 停止生成。
  - `Tick()`：运行中且未暂停时推进计时，到期调用 `GameLogicManager.SpawnUnit(pos)` 从屏幕顶部随机 X 位置生成一个 Unit。
- **SimpleUnit**：默认 Unit 类型，被生成后以固定速度向下移动；触碰底边 Border 时调用 `GameLogicManager.OnUnitReachBottom(this)` 完成扣血 + 回收。

---

## 缓存池（PoolManager）

- **实现**：使用 Unity 自带的 `UnityEngine.Pool.ObjectPool<T>` 管理 PinBall 与 Unit。
- **入池**：`SetActive(false)`，并 `SetParent(poolRoot)` 移到独立缓存根节点下。
- **出池**：`SetParent(null)`、`SetActive(true)`，加入活跃列表并参与 Tick。
- **与 GameLogicManager 的关系**：`GameLogicManager` 对外的 `SpawnPinBall / RecyclePinBall / SpawnUnit / RecycleUnit` 都转发到 `PoolManager`；PinBall 回收时由 `GameLogicManager` 额外调用 `player.AddPinBall()` 补充弹药。
- **清场**：`EndGame` / `BackToHome` / 每次 `StartGame` 都会调用 `ClearActivePinBalls` / `ClearActiveUnits` 将场上对象全部回收。

---

## 与项目文档的对应

- 管理脚本：`Assets/1_Scripts/Mgr/GameLogicManager.cs`、`Mgr/PoolManager.cs`、`Mgr/UIManager.cs`、`Mgr/GameEvents.cs`、`Mgr/GameEnum.cs`
- 单位生成：`Assets/1_Scripts/Unit/IUnitCreator.cs`、`Unit/UnitCreator.cs`、`Unit/SimpleUnit.cs`、`Unit/UnitBase.cs`
- UI：`Assets/1_Scripts/UI/StartScreenUI.cs`、`UI/GameOverUI.cs`；对应 prefab：`Assets/2_Prefab/UI/StartScreen.prefab`、`GameOverScreen.prefab`
- 详细流程、状态与依赖见 **doc/Design/PROJECT.md** 第 3、4、5 节。

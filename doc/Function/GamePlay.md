# 游戏主逻辑（GamePlay）

整局游戏的调度由 **GameLogicManager** 主导，配合 **PoolManager**（缓存池）、**UIManager**（界面）、**UnitCreator**（单位生成）与 **GameEvents**（事件总线）协同完成。
各模块通过事件解耦：`GameLogicManager` 只负责切状态、清场与**发事件**，其他模块订阅感兴趣的事件做对应响应。

---

## 职责划分

| 组件 | 职责 |
|------|------|
| **GameLogicManager**（单例） | 整局入口；维护 `GameState`；`UpdateGame()` 仅在 **Running** 时执行并统一驱动 Player / PinBall / Unit 的 `Tick`（PlayerRender 由 Player 内部驱动）；处理 Unit 触底伤害、玩家死亡判定；**按 Difficulty 当前阶段 stepInterval 节拍广播 `OnStep`**；发送其它 `GameEvents` 生命周期事件 |
| **PoolManager** | PinBall / Unit 的**对象池**：缓存根 `PoolRoot`（隐藏存放）与运行时根 `SpawnRoot`（活跃挂载）双根分离 + 活跃列表维护 |
| **UIManager**（单例） | 场景 UI 根节点持有者；订阅 `GameEvents` 控制开始界面 / 游戏内 HUD / 游戏结束界面三组 UI 的显隐 |
| **InGameUI** | 游戏内 HUD：根据 `Player.MaxHp` 动态生成心形 Image，实时刷新血量与 `currentPinBall/maxPinBall` 文本 |
| **StarfieldController** | 程序化星空背景：批量 `SpriteRenderer`，按 `sin` 波同时驱动 alpha 与缩放闪烁 |
| **UnitCreator**（纯 C# 类，实现 `IUnitCreator`） | 订阅 `OnStep` 批量生成 1..N 个 Unit；订阅生命周期事件维护运行/暂停标志 |
| **GameEvents**（静态） | 事件总线：`OnGameStart / OnGamePause / OnGameResume / OnGameEnd / OnReturnToHome / OnStep` |
| **ICombatAnimation** | 战斗动画接口：`PlayAttackAnimation / PlayHitAnimation / PlayDeathAnimation`，由 `PlayerRender` / `UnitRender` 实现，DOTween 提供补间能力 |
| **Defines**（静态常量） | 项目级常量：`UnitSize`、`StepDistance`、`StepInterval`、`StepMoveDuration` |
| **Difficulty**（运行时类） | 基于 `DifficultyTable` 与 `gameTime` 提供 spawn 区间 / unit hp / attack / stepInterval 查询 |
| **DifficultyTable / DifficultyStageData** | `DataSO/` 下的 ScriptableObject，运行时 Asset 位于 `8_Data/` |
| **AssetLoader** | 统一资源加载入口（Editor→AssetDatabase，将来 Addressables） |
| **DataImporter**（Editor-only） | `Tools/Data/*` 菜单，把 `9_Excel/*.csv` 导入为 `8_Data/*.asset` |
| **StartScreenUI / GameOverUI** | 界面挂载脚本；按钮回调调用 `GameLogicManager.StartGame / RestartGame / BackToHome` |

---

## 节奏系统（Step）

- **心跳**：`GameLogicManager.UpdateGame()` 在 Running 状态下按 `Difficulty.GetStepInterval()`（未配表时退回 `Defines.StepInterval` = 1 秒）向 `GameEvents.RaiseStep()` 广播。
- **Unit 响应**：`UnitBase` 在 `OnEnable` 订阅 `OnStep`（入池会 `OnDisable` 取消订阅）；`SimpleUnit` 在收到 `OnStep` 时开启一次向下平滑移动：
  - 距离：`Defines.StepDistance`（= `Defines.UnitSize` = 1 米）
  - 时长：`Defines.StepMoveDuration`（默认 0.2 秒）
  - 到达目标位置后检查是否与底边 Border 重叠，若重叠则触发 `OnUnitReachBottom`。
- **UnitCreator 响应**：`OnStep` 时调用 `SpawnBatch` 生成新一批 Unit，与上一批的一次整体下移**同时**发生，形成"每秒走一步 + 每秒刷一批"的节奏。
- **暂停**：`Paused` 状态下 `UpdateGame` 不会执行，自然也不会发 `OnStep`；`UnitCreator` 还额外用内部 `isPaused` 标志双重兜底。

---

## 单位标准化（Defines）

- **Unit 尺寸统一**：`UnitBase.Width = UnitBase.Height = Defines.UnitSize`（1x1 正方形）。`Init()` 强制 `transform.localScale = Vector3.one * Defines.UnitSize`，避免 prefab scale 与逻辑不一致。
- **UnitRect 同步**：`RefreshRect` 根据位置 + `Defines.UnitSize` 计算；无需再读 `transform.localScale`。
- **其他常量**：`StepDistance / StepInterval / StepMoveDuration`，在 `UnitCreator` / `SimpleUnit` / `GameLogicManager` 中复用。

---

## 生命值与攻击力

- **Player.maxHp / currentHp**：玩家有生命值，`Init()` 时重置为 `maxHp`；`TakeDamage(int)` 扣血，`IsDead` 表示是否死亡。
- **UnitBase.attack**：每个 Unit 都有攻击力字段（默认 1），Inspector 可调。
- **触底伤害**：Unit 移动到底边 Border 时，不再直接回收，而是调用 `GameLogicManager.OnUnitReachBottom(unit)`：
  1. 调用 `unit.PlayReachBottomAnimation()`（转发到 `UnitRender`）
  2. 对 Player 扣 `unit.Attack` 点血（触发 `PlayerRender.PlayHitAnimation` / `PlayDeathAnimation`）
  3. 回收该 Unit 进池
  4. 若 Player HP 归零 → 调用 `EndGame()`
- **不影响 PinBall 规则**：PinBall 触底仍然走原逻辑（回收 + 补弹），与 Unit 触底独立。

---

## 游戏生命周期与事件

整局游戏围绕 6 个事件展开，由 `GameLogicManager` 统一发送：

| 事件 | 触发时机 | 典型订阅方 |
|------|---------|-----------|
| `OnGameStart` | `StartGame()` 初始化完成后 | UIManager（隐藏 Start/GameOver、显示 InGameUI）、UnitCreator（进入运行） |
| `OnGamePause` | `PauseGame()` 从 Running 进入 Paused | UnitCreator（暂停生成） |
| `OnGameResume` | `ResumeGame()` 从 Paused 恢复 Running | UnitCreator（恢复生成） |
| `OnGameEnd` | `EndGame()`（玩家死亡或主动结束） | UIManager（隐藏 InGameUI、弹出 GameOverUI）、UnitCreator（停止生成） |
| `OnReturnToHome` | `BackToHome()` 从 Ended 回到 Preparing | UIManager（隐藏 InGameUI/GameOverUI、显示 StartScreen）、UnitCreator（停止生成） |
| `OnStep` | Running 下每 `Difficulty.GetStepInterval()` 秒一次（无表退回 `Defines.StepInterval`） | 所有活跃 `UnitBase`（启动移动）、UnitCreator（生成新一批） |

### 典型流程

1. **进入场景**：`GameState = Preparing`，UIManager 默认保持 StartScreen 可见。
2. **点击「开始」按钮**：`StartScreenUI.OnStartClicked` → 隐藏自身并调用 `GameLogicManager.StartGame()`。
   - `StartGame()` 初始化 borders / player / pool、重置 `stepTimer`、`GameState = Running`、`RaiseGameStart`。
3. **Running 阶段**：每帧 `UpdateGame()`；每满 `StepInterval` 秒广播一次 `OnStep`，同一时刻触发：
   - `UnitCreator.SpawnBatch` 从屏幕顶部批量生成 1..N 个 Unit（不重叠、不越界）；
   - 所有活跃 Unit 启动本轮 0.2s 下移 1 米的动画。
4. **Unit 触底扣血**：移动结束时 `OnUnitReachBottom` 扣 Player HP。
5. **玩家死亡**：`EndGame()` → `GameState = Ended`，清空场上对象，`RaiseGameEnd` → UIManager 显示 GameOverUI。
6. **GameOver UI 操作**：
   - **Restart** → `GameLogicManager.RestartGame()`（= 重新 `StartGame`）。
   - **Home** → `GameLogicManager.BackToHome()` → `GameState = Preparing`，`RaiseReturnToHome` → UIManager 显示 StartScreen。

---

## 游戏状态（GameState）

- **通用枚举**集中在 `Mgr/GameEnum.cs`，当前包含：
  - `BounceDirection`：边框反弹法线（Up / Down / Left / Right）。
  - `GameState`：游戏状态（见下）。
- **取值**：
  - **Preparing**：初始化 / 等待开始，不执行游戏循环。
  - **Running**：主逻辑运行，每帧执行 `UpdateGame()` 并推进 Step 心跳。
  - **Paused**：主循环停推（不发 `OnStep`，Unit 也不会继续移动）。
  - **Ended**：游戏结束，不执行 `UpdateGame()`（UI 层显示结算）。
- **切换入口**（都在 `GameLogicManager`）：
  - `StartGame()` → Preparing → Running（发 `OnGameStart`，重置 `stepTimer`）
  - `PauseGame()` → Running → Paused（发 `OnGamePause`）
  - `ResumeGame()` → Paused → Running（发 `OnGameResume`）
  - `EndGame()` → * → Ended（发 `OnGameEnd`）
  - `BackToHome()` → * → Preparing（发 `OnReturnToHome`）

---

## 更新驱动（统一 Tick）

- **无独立 Update**：PinBall、Player、Unit 都不写自己的 `Update`，只实现 `Tick()`。
- **UnitCreator 无 Tick**：完全由事件驱动（`OnStep` / `OnGame*`），Manager 不再调用其 `Tick`。
- **统一调度**：Running 状态下 `GameLogicManager.UpdateGame()` 每帧按固定顺序执行：
  1. 刷新所有 Border / Unit 的 Rect
  2. `player.Tick()`（内部驱动 `playerRender.Tick()` 绘制预览虚线）
  3. 推进 `difficulty.Tick(dt)` 与 `stepTimer`，到期 `RaiseStep()`（支持单帧补齐多步）
  4. 逆向遍历活跃 PinBall，逐个 `Tick(borders, activeUnits)`
  5. 逆向遍历活跃 Unit，逐个 `Tick()`（`SimpleUnit` 在这里推进位移插值）
- **隐藏不参与**：处于**缓存池内**（已隐藏、挂在池根节点下）的物体不加入活跃列表，不执行 Tick，也因 `OnDisable` 取消了 `OnStep` 订阅。

---

## UnitCreator：单位生成

- **接口 `IUnitCreator`**：纯标记接口（继承 `IDisposable`），不再包含任何方法。
- **默认实现 `UnitCreator`**：
  - 纯 C# 类（非 MonoBehaviour）；`GameLogicManager.Awake()` `new UnitCreator()` 创建一次，`OnDestroy` 时 `Dispose` 取消订阅。
  - 构造时订阅 6 个事件：`OnGameStart / OnGamePause / OnGameResume / OnGameEnd / OnReturnToHome / OnStep`；维护 `isRunning / isPaused` 两个标志。
  - `OnStep` 回调：`isRunning && !isPaused` 时调用 `SpawnBatch`，使生成节奏严格对齐心跳。
- **SpawnBatch 规则**：
  - 以正交相机视口为准计算可用宽度 `availWidth`（左右各留 `HorizontalPadding`）。
  - `maxCount = floor(availWidth / Defines.UnitSize)`，随机 `count ∈ [1, maxCount]`。
  - 均分 count 个槽，每个槽内再随机 X，保证相邻 Unit 不重叠且不越界。
- **SimpleUnit**：
  - 收到 `OnStep` 时记录起点与目标（`position + (0, -StepDistance)`），启动 `StepMoveDuration` 秒的 Lerp 位移；
  - `Tick` 推进插值，到达目标时再检测是否与底边 Border 重叠，若重叠则 `OnUnitReachBottom`。

---

## 缓存池（PoolManager）

- **实现**：使用 Unity 自带的 `UnityEngine.Pool.ObjectPool<T>` 管理 PinBall 与 Unit。
- **双根分离**：每类对象都有 `PoolRoot`（缓存挂载）与 `SpawnRoot`（运行时挂载）两个 Transform；Inspector 未指定时 `Awake` 自动创建。
- **入池**：`SetActive(false)`，并 `SetParent(poolRoot)`；`UnitBase.OnDisable` 自动取消 `OnStep` 订阅。
- **出池**：`SetParent(spawnRoot)`、`SetActive(true)`；`UnitBase.OnEnable` 自动订阅 `OnStep`，并加入活跃列表参与 Tick。
- **与 GameLogicManager 的关系**：`GameLogicManager` 对外的 `SpawnPinBall / RecyclePinBall / SpawnUnit / RecycleUnit` 都转发到 `PoolManager`；PinBall 回收时由 `GameLogicManager` 额外调用 `player.AddPinBall()` 补充弹药。
- **清场**：`EndGame` / `BackToHome` / 每次 `StartGame` 都会调用 `ClearActivePinBalls` / `ClearActiveUnits` 将场上对象全部回收。

---

## 战斗动画（ICombatAnimation）

- **接口位置**：`Assets/1_Scripts/ICombatAnimation.cs`，定义 `PlayAttackAnimation / PlayHitAnimation / PlayDeathAnimation` 三个钩子。
- **PlayerRender** 实现：
  - `PlayAttackAnimation`：DOTween 在 `player.FireInterval` 时长内做一次 360° 旋转（`Ease.Linear`），起手时 `Kill()` 上一段补间避免叠加，结束自动归零。
  - 在 `Player.HandleFire` 成功发射时被调用。
  - 受击 / 死亡动画当前空实现，预留扩展位。
- **UnitRender** 实现：当前钩子均为空，`UnitBase.TakeDamage` 与 `OnUnitReachBottom` 已经完成事件转发，需要表现时直接在 Render 层填充补间/粒子即可。
- **依赖**：`Assets/Plugins/DOTween/`（DLL + Modules + 设置 SO 在 `Assets/Resources/DOTweenSettings.asset`），无需在场景中额外初始化。

---

## 游戏内 HUD（InGameUI）

- 由 `UIManager` 在 `OnGameStart` 时显示，`OnGameEnd / OnReturnToHome` 时隐藏。
- 数据来源：`Player.MaxHp / CurrentHp / MaxPinBallCount / CurrentPinBallCount`，每帧 `Update` 比对差值，仅在变化时刷新。
- 心形血条：根据 `Player.MaxHp` 动态生成 `Image` 心形（贴图来自 `7_Res/GeneratedShapes/heart_red_64.png`），按当前血量切换不透明 / 半透明。
- 弹珠数量：通过 TextMeshPro 文本以 `cur/max` 形式显示。

---

## 背景星空（StarfieldController）

- 程序化生成的星空，独立于核心战斗逻辑，挂在场景任意 GameObject 上即可。
- `OnEnable` 自动 `Rebuild`：在相机视口（或自定义矩形）内创建 `starCount` 颗 `SpriteRenderer`，每颗独立 alpha / scale / 闪烁速度 / 相位；`Update` 中按 `sin` 波同时驱动透明度与缩放闪烁。
- 未指定 `starSprite` 时运行时生成一张 64×64 的高斯辉光 + 十字星形 RGBA 贴图。
- 提供 `[ContextMenu("Rebuild Stars" / "Clear Stars")]` 方便编辑期调参。

---

## 数值与难度

- **数据流水线**：策划编辑 `Assets/9_Excel/Difficulty.csv`（Excel 可直接另存为 CSV）→ 在 Unity 菜单点 `Tools/Data/Import Difficulty` → 生成/更新 `Assets/8_Data/DifficultyTable.asset`。
- **运行时驱动**：`GameLogicManager.Awake` 用 `AssetLoader.Load<DifficultyTable>("8_Data/DifficultyTable.asset")` 加载表，构造 `Difficulty`。Running 中每帧 `difficulty.Tick(dt)` 推进 `gameTime`。
- **作用点**：
  - `UnitCreator.SpawnBatch`：每拍的生成数量区间从 `Difficulty.GetSpawnRange()` 取（再与屏幕可容纳数取交集）；
  - `UnitBase.Init → ApplyDifficulty`：每个 Unit 出池时 hp/attack 被覆盖为当前阶段值；
  - `GameLogicManager.UpdateGame`：Step 间隔每帧从 `Difficulty.GetStepInterval()` 动态取，阶段切换自然产生节奏变化。
- **兜底**：表缺失或为空时走 `Defines` 缺省，Unit 走 Inspector 默认，保证玩法可运行但难度平缓。

---

## 与项目文档的对应

- 管理脚本：`Assets/1_Scripts/Mgr/GameLogicManager.cs`、`Mgr/PoolManager.cs`、`Mgr/UIManager.cs`、`Mgr/GameEvents.cs`、`Mgr/GameEnum.cs`、`Mgr/Defines.cs`、`Mgr/Difficulty.cs`、`Mgr/StarfieldController.cs`
- 数据与资源：`Assets/1_Scripts/DataSO/DifficultyStageData.cs`、`DataSO/DifficultyTable.cs`、`Utility/AssetLoader.cs`、`Editor/DataImporter.cs`；配表 `Assets/9_Excel/Difficulty.csv`；Asset `Assets/8_Data/DifficultyTable.asset`
- 单位生成：`Assets/1_Scripts/Unit/IUnitCreator.cs`、`Unit/UnitCreator.cs`、`Unit/SimpleUnit.cs`、`Unit/UnitBase.cs`、`Unit/UnitRender.cs`
- UI：`Assets/1_Scripts/UI/StartScreenUI.cs`、`UI/GameOverUI.cs`、`UI/InGameUI.cs`；对应 prefab：`Assets/2_Prefab/UI/StartScreen.prefab`、`GameOverScreen.prefab`、`GameHUD.prefab`
- 战斗动画：`Assets/1_Scripts/ICombatAnimation.cs`，由 `PlayerRender.cs`、`UnitRender.cs` 实现；DOTween 插件位于 `Assets/Plugins/DOTween/`。
- 详细流程、状态与依赖见 **doc/Design/PROJECT.md** 第 3、4、5 节。

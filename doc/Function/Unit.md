# 单位（Unit）

游戏中的方块单位：会被弹球撞击扣血，也会对玩家造成伤害。基类为 `UnitBase.cs`，渲染为 `UnitRender.cs`；现有实现类 `SimpleUnit.cs`；生成由 `UnitCreator.cs`（实现 `IUnitCreator`）负责。单位的**尺寸与移动距离**由 `Mgr/Defines.cs` 统一定义。

---

## 职责划分

| 脚本 | 职责 |
|------|------|
| **UnitBase.cs** | 基类：生命值（`maxHp`）、攻击力（`attack`）、统一尺寸（`Defines.UnitSize`）、碰撞矩形、受击扣血、碰撞法线、订阅 `OnStep` |
| **UnitRender.cs** | 渲染单位形象（Image / SpriteRenderer），按 HP 比例变色 |
| **SimpleUnit.cs** | 默认单位实现：`OnStep` 触发 0.2s 下移 1 米 + 到达后触底回调 |
| **IUnitCreator.cs** | 生成器接口（空标记，继承 `IDisposable`） |
| **UnitCreator.cs** | 默认生成器：订阅 `OnStep` 批量生成 1..N 个 Unit（不重叠且不越界） |

---

## 形态与属性

- **形状**：**1x1 正方形**。`UnitBase.Width = Height = Defines.UnitSize`；`Init()` 会把 `transform.localScale` 强制设为 `Vector3.one * Defines.UnitSize`，保证视觉、碰撞与逻辑一致。
- **UnitRect**：由位置 + `Defines.UnitSize` 计算，不再依赖 `transform.localScale`。
- **生命值（HP）**：受 PinBall 撞击扣血；归零后 `RecycleUnit` 回池。
- **攻击力（Attack）**：Unit **触底**时对 Player 造成的伤害，Inspector 可配，默认 1。

---

## 节奏与移动（SimpleUnit）

Unit 不再连续平移，而是完全跟着 Step 心跳节奏走：

- **订阅 `OnStep`**：`UnitBase` 在 `OnEnable` 订阅、`OnDisable` 取消订阅（对应出池 / 入池）。子类通过重写 `HandleStep()` 响应。
- **一步移动**：
  - 距离：`Defines.StepDistance`（= `Defines.UnitSize` = 1 米，方向固定为 `Vector2.down`）
  - 时长：`Defines.StepMoveDuration`（默认 0.2s）
  - 过渡：`Vector2.Lerp(start, target, t)`，`t = moveTimer / StepMoveDuration`
- **推进**：由 `UnitBase.Tick()`（`SimpleUnit.Tick` 重写）每帧推进位移插值并 `RefreshRect`。
- **触底**：到达目标位置那一帧再检查与底边 Border 是否重叠，若重叠则调用 `GameLogicManager.OnUnitReachBottom(this)`：
  1. `player.TakeDamage(Attack)`
  2. `RecycleUnit(this)` 回收入池
  3. 如果 Player 死亡 → `EndGame()`
- **被击毁**：被 PinBall 撞击 `TakeDamage(1)`；若 HP 归零，`PinBallBase` 直接 `RecycleUnit`。

---

## 碰撞与反射

- **PinBall ↔ Unit**：PinBall 根据**当前位置与 Unit 矩形**计算碰撞面法线，做**镜面反射**，并对 Unit 扣血。
- 反射逻辑与 Border 一致（方向反射 + 可选速度变化），仅在法线来源上不同（Border 用配置方向，Unit 用几何最近面）。

---

## 生成（UnitCreator）

- **接口**：`IUnitCreator` 是空标记接口（`IDisposable`），Manager 只负责持有引用、`Dispose()`。
- **默认实现 `UnitCreator`**：
  - 纯 C# 类（非 MonoBehaviour），`GameLogicManager.Awake()` `new` 一次并持有，`OnDestroy` 时 `Dispose` 取消所有订阅。
  - 构造函数订阅：`OnGameStart / OnGamePause / OnGameResume / OnGameEnd / OnReturnToHome / OnStep`。
  - `OnGameStart` → 运行；`OnGamePause / Resume` → 切换暂停标志；`OnGameEnd / OnReturnToHome` → 停止运行。
  - **`OnStep` 回调**：`isRunning && !isPaused` 时调用内部 `SpawnBatch()`：
    - 以正交相机视口为准计算可用宽度 `availWidth`（左右各留 `HorizontalPadding`）；
    - `maxCount = floor(availWidth / Defines.UnitSize)`，随机 `count ∈ [1, maxCount]`；
    - 均分 count 个槽，每个槽内再随机 X，保证相邻 Unit 不重叠、不越界；
    - 直接使用 `Defines.UnitSize` 作为宽度，不需要再运行时测量 prefab。
- **常量**：`HorizontalPadding / TopOffset` 写在 `UnitCreator.cs` 内；节奏相关常量（间隔、移动时长）统一来自 `Defines`。

---

## 扩展

- **新的 Unit 行为**：继承 `UnitBase` 或 `SimpleUnit`，重写 `HandleStep`（处理节奏）与 `Tick`（每帧推进）；PoolManager 支持替换 `unitPrefab` 或新开池。
- **新的生成策略**：实现 `IUnitCreator`（例如按波次、仅在活跃 Unit < N 时生成、或监听自定义事件），在 `GameLogicManager.Awake` 里替换 `new UnitCreator()` 即可；订阅 `GameEvents` 自管生命周期。
- **修改节奏/尺寸**：只改 `Mgr/Defines.cs` 中的常量，整个节奏系统（Step 间隔、移动距离、移动时长、Unit 尺寸）会整体联动。

---

## 与项目文档的对应

- 脚本路径：
  - `Assets/1_Scripts/Unit/UnitBase.cs` / `UnitRender.cs`
  - `Assets/1_Scripts/Unit/IUnitCreator.cs` / `UnitCreator.cs`
  - `Assets/1_Scripts/Unit/SimpleUnit.cs`
  - `Assets/1_Scripts/Mgr/Defines.cs`
- 详细接口与池化见 **doc/Design/PROJECT.md** 中「4.11 UnitBase」「4.12 UnitRender」「4.13 IUnitCreator」「4.14 UnitCreator」「4.15 SimpleUnit」。

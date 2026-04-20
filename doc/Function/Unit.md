# 单位（Unit）

游戏中的方块单位：会被弹球撞击扣血，也会对玩家造成伤害。基类为 `UnitBase.cs`，渲染为 `UnitRender.cs`；现有实现类 `SimpleUnit.cs`；生成由 `UnitCreator.cs`（实现 `IUnitCreator`）负责。

---

## 职责划分

| 脚本 | 职责 |
|------|------|
| **UnitBase.cs** | 基类：生命值（`maxHp`）、攻击力（`attack`）、碰撞矩形、受击扣血、碰撞法线 |
| **UnitRender.cs** | 渲染单位形象（Image / SpriteRenderer），按 HP 比例变色 |
| **SimpleUnit.cs** | 默认单位实现：下落 + 触底回调 |
| **IUnitCreator.cs** | 生成器接口（只定义 `Tick`） |
| **UnitCreator.cs** | 默认生成器：订阅 `GameEvents`，按间隔从屏幕顶部随机 X 生成单位 |

---

## 形态与属性

- **形状**：正方形（由 `transform.localScale` 与位置构造 `UnitRect`）。
- **生命值（HP）**：受 PinBall 撞击扣血；归零后 `RecycleUnit` 回池。
- **攻击力（Attack）**：Unit **触底**时对 Player 造成的伤害，Inspector 可配，默认 1。

---

## 行为（SimpleUnit）

- **下落**：`Tick()` 中以 `moveSpeed` 向下移动并 `RefreshRect`。
- **触底**：与底边 Border 矩形相交时，调用 `GameLogicManager.OnUnitReachBottom(this)`：
  1. `player.TakeDamage(Attack)`
  2. `RecycleUnit(this)` 回收入池
  3. 如果 Player 死亡 → `EndGame()`
- **被击毁**：被 PinBall 撞击 `TakeDamage(1)` 若 HP 归零，由 `PinBallBase` 直接调用 `RecycleUnit`。

---

## 碰撞与反射

- **PinBall ↔ Unit**：PinBall 根据**当前位置与 Unit 矩形**计算碰撞面法线，做**镜面反射**，并对 Unit 扣血。
- 反射逻辑与 Border 一致（方向反射 + 可选速度变化），仅在法线来源上不同（Border 用配置方向，Unit 用几何最近面）。

---

## 生成（UnitCreator）

- **接口**：`IUnitCreator` 仅定义 `Tick()`；生命周期响应由实现类通过 `GameEvents` 事件处理。
- **默认实现 `UnitCreator`**：
  - 纯 C# 类（非 MonoBehaviour），由 `GameLogicManager.Awake()` `new` 一次并持有引用，`OnDestroy` 时 `Dispose` 取消订阅。
  - 构造函数订阅：`OnGameStart / OnGamePause / OnGameResume / OnGameEnd / OnReturnToHome`。
  - `OnGameStart` → 重置计时并标记运行；`OnGamePause / Resume` → 切换暂停标志；`OnGameEnd / OnReturnToHome` → 停止运行。
  - `Tick()`：运行中且未暂停时推进计时，到期从屏幕顶部（正交相机范围内随机 X）调用 `GameLogicManager.SpawnUnit`。
- **生成节奏常量**：`SpawnInterval / InitialDelay / HorizontalPadding / TopOffset` 直接写在 `UnitCreator.cs` 内。

---

## 扩展

- **新的 Unit 行为**：继承 `UnitBase` 或 `SimpleUnit`，重写 `Tick`；PoolManager 支持替换 `unitPrefab` 或新开池。
- **新的生成策略**：实现 `IUnitCreator`（例如按波次、仅在活跃 Unit < N 时生成），在 `GameLogicManager.Awake` 里替换 `new UnitCreator()` 即可；订阅所需的 `GameEvents` 事件自管生命周期。

---

## 与项目文档的对应

- 脚本路径：
  - `Assets/1_Scripts/Unit/UnitBase.cs` / `UnitRender.cs`
  - `Assets/1_Scripts/Unit/IUnitCreator.cs` / `UnitCreator.cs`
  - `Assets/1_Scripts/Unit/SimpleUnit.cs`
- 详细接口与池化见 **doc/Design/PROJECT.md** 中「4.11 UnitBase」「4.12 UnitRender」「4.13 IUnitCreator」「4.14 UnitCreator」「4.15 SimpleUnit」。

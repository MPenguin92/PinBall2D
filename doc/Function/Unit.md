# 单位（Unit）

游戏中的方块单位：会被弹球撞击扣血，也会对玩家造成伤害。基类为 `UnitBase.cs`，渲染为 `UnitRender.cs`；`SimpleUnit.cs` 为空壳占位；生成由 `UnitCreator.cs`（实现 `IUnitCreator`）负责。尺寸与节奏常量来自 `Mgr/Defines.cs`。

---

## 职责划分

| 脚本 | 职责 |
|------|------|
| **UnitBase.cs** | HP、Attack、Experience；统一尺寸；Step 位移 + 减速 + 堵塞；碰撞法线；动画转发 |
| **UnitRender.cs** | HP 变色；`ICombatAnimation` + `PlayReachBottomAnimation`；减速染色 |
| **SimpleUnit.cs** | 空壳：prefab / Addressables 地址 `"SimpleUnit"` 的脚本绑定占位 |
| **IUnitCreator.cs** | 生成器接口（空标记，继承 `IDisposable`） |
| **UnitCreator.cs** | 订阅 `OnStep` 批量生成；出生点占用检测 |

---

## 形态与属性

- **形状**：1x1 正方形，`Defines.UnitSize`；`Init()` 强制 `localScale`。
- **HP / Attack**：运行时由 `Difficulty` 当前阶段覆盖；触底对 Player 造成 `Attack` 伤害。
- **Experience**：击杀时累加到升级系统；由 `Difficulty.GetUnitExperience()` 在 `Init` 写入。

---

## 节奏与移动

- **订阅 `OnStep`**：`OnEnable` 订阅 / `OnDisable` 取消（随对象池自动管理）。
- **HandleStep（基类）**：先处理减速累计（`slowFactor`），再检查目标格是否被其他 Unit 占用；占用则本拍跳过（堵塞/冰墙）；否则启动一次向 `MoveDirection`（默认向下）的 Lerp 位移。
- **一步**：距离 `Defines.StepDistance`，时长 `Defines.StepMoveDuration`；`Tick` 推进插值。
- **触底**：到达目标后检测底边 → `OnUnitReachBottom` → 触底动画 → `player.TakeDamage(Attack)` → 回收。
- **被击毁**：`TakeDamage` → 受击/死亡动画；由 PinBall 在击杀后 `RaiseUnitKilled` 再 `RecycleUnit`。

---

## 碰撞与反射

- PinBall 用 `GetCollisionNormal` 取最近面法线做镜面反射（或穿透），并结算方向倍率伤害。

---

## 生成（UnitCreator）

- 构造时订阅生命周期事件 + `OnStep`；`SpawnBatch` 按相机宽度与 `Difficulty.GetSpawnRange()` 决定数量。
- 槽位随机 X，保证不重叠不越界；出生点已被占则放弃该颗（避免顶部压死）。

---

## 扩展

- 新 Unit：继承 `UnitBase`，override `MoveDirection` / `HandleStep` / `ApplyDifficulty`。
- 新生成策略：实现 `IUnitCreator`，在 `GameLogicManager.Awake` 替换实例。
- 调节奏/尺寸：改 `Defines` 或缺省表；主曲线改 `Difficulty.csv`。

---

## 与项目文档的对应

- 脚本：`Assets/1_Scripts/Unit/`、`Mgr/Defines.cs`
- 预制体：`Assets/2_Prefab/SimpleUnit.prefab`
- 详细见 **PROJECT.md**「4.11~4.15」；难度字段见 **DifficultyBalance.md**。

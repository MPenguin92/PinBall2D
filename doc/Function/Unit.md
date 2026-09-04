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
- **HP / Attack / Experience / Gold**：不存难度表。`Init → ApplyDifficulty` 按 prefab 的 `unitId` +
  出池时注入的等级查 `UnitTable`（来源 `Units.csv` 定义 + `Units_Level.csv` 逐级数值）覆盖。
  触底对 Player 造成 `Attack` 伤害；击杀累加 `Experience`（升级经验）并把 `Gold` 计入全局金币
  （`GameLogicManager.Gold`，每局清零，尚未接入显示/消费）。
- **难度表（Difficulty.csv）**：只描述时间轴节奏（stepInterval）、每波数量区间（spawnMin/Max）与
  等级分布权重（spawnLevels，如 `1x60;2x30;3x10`），不携带怪数值。

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

- 订阅生命周期事件 + `OnStep`；普通怪（`unit_damage`）每 Step 生成一批：总数 `spawnMin~spawnMax`，
  每只按 `spawnLevels` 等级权重 roll；列随机不重叠，出生点被占则放弃该只。
- **金币怪（`unit_gold`）混入普通波**：`GameLogicManager` 每 `Defines.GoldSpawnInterval` 秒
  置一次就绪标记；下一次 `SpawnStep` 时本波随机 1~2 只原本的普通怪会被替换成金币怪
  （等级 = 当前难度阶段最高等级，夹在该怪满级内）。金币怪 hp 低、击杀产出高额 `gold`
  （普通怪 gold 恒 0）。
- 怪的类型都查 `UnitTable`（id → prefab 地址），不硬编码资源字符串；加新怪 = `Units.csv`/`Units_Level.csv`
  加行 + 提供对应 prefab。

---

## 扩展

- 新 Unit：`Units.csv` 加一行（id/name/prefab 地址）+ `Units_Level.csv` 加逐级数值 → 派生 `UnitBase`
  覆盖行为（override `MoveDirection` / `HandleStep` / `ApplyDifficulty`）。
- 新生成策略：实现 `IUnitCreator`，在 `GameLogicManager.Awake` 替换实例。
- 调节奏/等级曲线：难度节奏改 `Difficulty.csv`；怪自身数值改 `Units_Level.csv`。

---

## 与项目文档的对应

- 脚本：`Assets/1_Scripts/Unit/`、`Mgr/Defines.cs`
- 预制体：`Assets/2_Prefab/SimpleUnit.prefab`
- 详细见 **PROJECT.md**「4.11~4.15」；难度字段见 **DifficultyBalance.md**。

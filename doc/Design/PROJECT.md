# PinBall2D 项目文档

## 1. 项目概述

**PinBall2D** 是一款基于 Unity 的 2D 弹珠台（Pinball）游戏。玩家控制一个可左右移动的挡板（Player），通过蓄力击打弹珠（PinBall），弹珠在由边框（Border）围成的区域内运动并发生反弹。

- **引擎**：Unity（2D 项目）
- **产品名**：PinBall2D（见 `ProjectSettings/ProjectSettings.asset`）
- **主场景**：`Assets/Scenes/MainScene.unity`

---

## 2. 目录结构

```
PinBall2D/
├── Assets/
│   ├── 1_Scripts/           # 游戏逻辑脚本
│   │   ├── Border.cs        # 边框/墙壁，定义反弹区域与反弹方向
│   │   ├── GameEnum.cs      # 游戏枚举（如 BounceDirection）
│   │   ├── GameLogicManager.cs  # 单例游戏逻辑管理器
│   │   ├── PinBallBase.cs   # 弹珠基类（运动、碰撞、反弹）
│   │   ├── Player.cs        # 玩家挡板（移动、蓄力、击球）
│   │   └── PlayerRender.cs  # 玩家蓄力条 UI 显示
│   └── Scenes/
│       └── MainScene.unity  # 主场景
├── doc/
│   └── PROJECT.md           # 本文档
├── ProjectSettings/         # Unity 项目配置
├── UserSettings/            # 编辑器布局等
├── Library/                 # Unity 缓存与编译产物（不纳入版本控制）
├── Logs/                    # 编辑器/编译日志
└── .gitignore
```

### 2.1 资源与脚本对应关系

| 路径 | 说明 |
|------|------|
| `Assets/1_Scripts/` | 所有 C# 游戏逻辑脚本及 `.meta` |
| `Assets/Scenes/` | 场景文件，当前仅 `MainScene.unity` |

---

## 3. 核心脚本与功能逻辑

### 3.1 GameEnum.cs — 游戏枚举

定义边框反弹方向，供 `Border` 与 `PinBallBase` 使用：

```csharp
public enum BounceDirection
{
    Up, Down, Left, Right
}
```

---

### 3.2 Border.cs — 边框

- **职责**：表示一块矩形边框区域，用于限制弹珠运动范围并指定反弹方向。
- **主要成员**：
  - `BounceDirection`：反弹法线方向（上/下/左/右）。
  - `BorderRect`：世界坐标系下的矩形（由 `RefreshRect()` 根据 `transform.position` 与 `transform.localScale` 计算）。
  - `Width` / `Height`：取自 `transform.localScale`。
- **逻辑要点**：
  - `RefreshRect()` 在 `Awake`、`OnValidate` 以及每帧由 `PinBallBase.Tick` 调用，保证矩形与 Transform 同步。
  - Gizmos 中绘制黄色线框矩形，便于在编辑器中查看边界。

---

### 3.3 GameLogicManager.cs — 游戏逻辑管理器（单例）

- **职责**：集中管理边框引用与弹珠列表，驱动每帧游戏更新。
- **单例**：`Instance` 在 `Awake` 中赋值，`OnDestroy` 时若为自身则置空。
- **核心数据**：
  - `borders`：场景中所有 `Border` 的缓存数组（在 `StartGame()` 时通过 `FindObjectsByType<Border>` 获取）。
  - `pinBalls`：当前参与逻辑的 `PinBallBase` 列表。
- **主要方法**：
  - `StartGame()`：重新收集所有 `Border` 和现有 `PinBallBase`，清空并填充 `pinBalls`。
  - `UpdateGame()`：每帧对 `pinBalls` 中每个弹珠调用 `Tick(borders)`，实现移动与边框碰撞。
  - `RegisterPinBall` / `UnregisterPinBall`：供弹珠在 `OnEnable` / `OnDisable` 时注册/注销。

**流程**：`Start()` → `StartGame()`；`Update()` → `UpdateGame()`。

---

### 3.4 PinBallBase.cs — 弹珠基类

- **职责**：弹珠的物理运动、与边框的碰撞检测与反弹、被玩家击打后的速度变化。
- **可配置**：
  - `speed`：当前速度向量（默认 `(0, -10)`）。
  - `borderBounceSpeedMultiplier`：碰到边框后若速度大于初始速度，则按该系数衰减（默认 0.9）。
- **生命周期**：
  - `Awake`：记录 `initialSpeed`、`initialSpeedMagnitude`。
  - `OnEnable`：向 `GameLogicManager` 注册。
  - `OnDisable`：从 `GameLogicManager` 注销，并恢复 `speed = initialSpeed`。
- **核心方法**：
  - **Tick(borders)**：  
    - 根据当前 `speed` 计算下一帧位置。  
    - 遍历 `borders`，用 `IsCircleOverlappingRect` 做圆与矩形重叠检测；若重叠则取该边框的 `BounceDirection` 得到法线，调用 `ApplyBorderBounce(normal)` 反射速度并应用衰减，然后只处理第一个重叠的边框并 `break`。  
    - 最后用更新后的 `speed` 移动 `transform.position`。
  - **PushBall(newSpeed)**：被玩家击打时调用。将速度设为 `newSpeed` 的方向，大小取 `max(initialSpeedMagnitude, newSpeed.magnitude)`，保证击打后至少保持初始速度大小。
- **辅助**：
  - `IsCircleOverlappingRect`：圆心到矩形最近点的距离与半径比较。
  - `GetBorderNormal`：将 `BounceDirection` 转为 `Vector2` 法线。
- Gizmos：白色线框球体，半径 `transform.localScale.x * 0.5f`（即 `Radius`）。

---

### 3.5 Player.cs — 玩家挡板

- **职责**：左右移动、蓄力、与弹珠的碰撞检测与击打。
- **可配置**：
  - `moveSpeed`：横向移动速度（默认 8）。
  - `baseHitPower`：未蓄力时的基础击打力度（默认 0.5）。
  - `maxHitPower`：最大蓄力（默认 2）。
  - `powerChargeSpeed`：蓄力增长速度（默认 1）。
- **属性**：
  - `Radius`：`transform.localScale.x * 0.5f`，用于与边框、弹珠的距离判断。
  - `CurrentHitPower` / `MaxHitPower` / `BaseHitPower`：供 `PlayerRender` 与击球逻辑使用。
- **每帧逻辑（Update）**：
  1. **HandleMove()**：A/D 键左右移动，`ClampXWithBorders` 将 x 限制在左右边框内侧（通过 `TryGetHorizontalLimits` 识别“竖长”的左右边框，得到 `minX`/`maxX`），避免挡板穿出边界。
  2. **HandleCharge()**：未按 A/D 时蓄力，`currentHitPower` 随时间增加至 `maxHitPower`；若正在移动则 `ResetHitPower()` 回到 `baseHitPower`。
  3. **HandleHit()**：遍历 `GameLogicManager` 提供的弹珠（或 `FindObjectsByType<PinBallBase>`），若与某弹珠距离 ≤ `Radius + ball.Radius`，则对该弹珠调用 `PushBall(-ball.Speed * currentHitPower)`，然后 `ResetHitPower()` 并只处理一颗弹珠（`break`）。
- **辅助**：
  - `GetBorders()` / `GetPinBalls()`：优先从 `GameLogicManager.Instance` 取，否则用 `FindObjectsByType`。
  - `TryGetHorizontalLimits`：在所有 Border 中找“竖长”（height ≥ width）的左右两块，用其 `BorderRect` 的 x 边界加上 `Radius` 得到可移动 x 范围。
- Gizmos：白色线框球体表示挡板碰撞体。

---

### 3.6 PlayerRender.cs — 蓄力条显示

- **职责**：根据玩家当前蓄力值更新 UI 蓄力条（Image 的 fillAmount）。
- **依赖**：需在 Inspector 中指定 `power`（Image，建议使用 Image 的 Filled 类型）和 `player`（Player）。
- **逻辑**：每帧将 `(CurrentHitPower - BaseHitPower) / (MaxHitPower - BaseHitPower)` 钳制到 [0,1] 并赋给 `power.fillAmount`，即蓄力条从“基础力度”到“满蓄力”的比例。

---

## 4. 游戏流程与数据流

### 4.1 初始化

1. 场景加载后，`GameLogicManager.Awake` 设置单例，`Border.Awake` / `PinBallBase.Awake` 等执行。
2. `GameLogicManager.Start` 调用 `StartGame()`，收集所有 `Border` 与 `PinBallBase`，填充 `borders` 与 `pinBalls`。
3. 之后动态生成的弹珠在 `OnEnable` 时通过 `RegisterPinBall` 加入 `pinBalls`。

### 4.2 每帧更新顺序（概念上）

1. **GameLogicManager.Update**  
   - 调用 `UpdateGame()`，对每个已注册的 `PinBallBase` 执行 `Tick(borders)`，完成弹珠移动与边框反弹。

2. **Player.Update**  
   - `HandleMove()`：根据输入和边框限制更新挡板 x。  
   - `HandleCharge()`：不移动时增加 `currentHitPower`。  
   - `HandleHit()`：若与某弹珠相交，则 `PushBall(-ball.Speed * currentHitPower)` 并重置蓄力。

3. **PlayerRender.Update**  
   - 根据 `player.CurrentHitPower` 等更新蓄力条 `power.fillAmount`。

（实际执行顺序由 Unity 的脚本执行顺序和物体顺序决定，上述为逻辑顺序。）

### 4.3 碰撞与击打

- **弹珠–边框**：在 `PinBallBase.Tick` 中用圆与矩形重叠检测，重叠则按 `Border.BounceDirection` 反射速度，并可能应用 `borderBounceSpeedMultiplier`。
- **挡板–弹珠**：在 `Player.HandleHit` 中用圆心距离与 `Radius + ball.Radius` 比较，满足则调用 `ball.PushBall(...)`，弹珠速度由 `PinBallBase.PushBall` 按方向和最小速度处理。

### 4.4 依赖关系简图

```
GameEnum (BounceDirection)
    ↑
Border ←—— GameLogicManager（持有 borders，每帧传参）
    ↑              ↑
    |              +—— PinBallBase（注册/注销，每帧 Tick）
    |                        ↑
    +———————————— Player（获取 borders 做 x 限制；获取 pinBalls 做击球）
                        ↑
                  PlayerRender（仅依赖 Player 的蓄力数值）
```

---

## 5. 场景与配置建议

- **MainScene** 中需存在：
  - 至少一个 `GameLogicManager`（建议单例）。
  - 若干 `Border`：围成弹珠活动区域，左右两侧建议为“竖长”矩形以便 `Player` 正确计算水平范围。
  - 至少一个 `PinBallBase`（弹珠）。
  - 一个 `Player`（挡板）。
  - 若使用蓄力条：一个挂有 `PlayerRender` 的 GameObject，并绑定 `power`（Filled Image）和 `player`。

- **Player** 的移动范围由“竖长”的左右 `Border` 自动计算，无需在 Player 上再写死边界。

---

## 6. 扩展与维护说明

- **新增弹珠类型**：继承 `PinBallBase`，可重写 `Tick` 或增加行为，在 `OnEnable` 中仍会通过基类注册到 `GameLogicManager`。
- **新增边框类型**：可继承 `Border` 或复用现有 `Border`，保证 `RefreshRect` 与 `BounceDirection` 正确即可。
- **关卡/多场景**：每个场景若有独立玩法，需保证该场景内有 `GameLogicManager` 且会调用 `StartGame()`；跨场景单例若需保留，需在场景切换时自行处理 `Instance`。

---

## 7. 文档与版本

- 本文档路径：`doc/PROJECT.md`。
- 基于当前仓库脚本与目录结构整理，若后续增加场景、预制体或脚本，建议同步更新本文档的目录结构与依赖说明。

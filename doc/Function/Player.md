# 角色（Player）

玩家主控单位：固定位置的发射器，负责旋转瞄准、发射弹球，并承受来自 Unit 的伤害。逻辑在 `Player.cs`，渲染在 `PlayerRender.cs`。

---

## 职责划分

| 脚本 | 职责 |
|------|------|
| **Player.cs** | 旋转、发射、弹药容量、冷却间隔、**生命值与受伤**；调用 `PlayerRender` 实现的 `ICombatAnimation` 触发攻击/受击/死亡动画 |
| **PlayerRender.cs** | 形象渲染 + 方向预览虚线（LineRenderer + `DashedLine.shader`） + 战斗动画（DOTween 攻击旋转） |

---

## 功能需求

### 移动与旋转

- **不移动**：Player 位置固定，不能平移。
- **旋转**：通过 **A / D** 键左右旋转。
- **正方向**：正 Y 轴为朝向正前方（0° 时朝上）。
- **旋转限制**：左右旋转均不超过 **80°**（即总范围 ±80°）。

### 发射弹球

- **按键**：按 **F** 从 Player 位置、沿当前朝向发射 PinBall。
- **容量**：Player 拥有的 PinBall 数量有上限（默认 5），发射会消耗数量。
- **间隔**：两次发射之间有冷却时间，避免连发。
- **补充**：弹球触底回收后，由 `GameLogicManager.RecyclePinBall` 调用 `player.AddPinBall()` 补充数量；数量为 0 时无法发射，直到被补充。

### 生命值与受伤

- **属性**：`maxHp`（Inspector 可配，默认 5）、`currentHp`、`IsDead`；同时对外暴露 `MaxHp / CurrentHp`、`MaxPinBallCount / CurrentPinBallCount`、`FireInterval` 供 `InGameUI` 取值刷新 HUD。
- **初始化**：`Init()` 时 `currentHp = maxHp`（开始/重开游戏时触发）。
- **受伤来源**：Unit 触底 → `GameLogicManager.OnUnitReachBottom(unit)` → `player.TakeDamage(unit.Attack)`。
- **动画联动**：`TakeDamage` 扣血时调用 `playerRender.PlayHitAnimation()`；归零时再调用 `PlayDeathAnimation()`（默认空实现，预留补间/特效）。
- **死亡判定**：`TakeDamage` 返回 `IsDead`；死亡时 `GameLogicManager.EndGame()` 会被触发，游戏进入 `Ended`、弹出 GameOver UI。

---

## 渲染（PlayerRender）

- **形象**：渲染 Player 外观（当前可用 Image 或 SpriteRenderer）。
- **方向预览线**：使用 **LineRenderer** 从 Player 位置沿当前方向画一条直线：
  - **起点偏移**：`lineForwardOffset` 让起点沿发射方向前移一段，避免线段插到 Player 内侧。
  - **长度**：由 `maxLineLength` 控制最大长度，`maxBounces` 控制最大反射次数。
  - **碰到 Unit**：线在该处停止，不再向前延伸。
  - **碰到 Border**：按入射角做**镜面反射**，沿新方向继续画线。
  - **碰到底边 Border**：与 Unit 一致，停止。
  - **材质**：使用 `7_Res/dashed_line.mat`（Shader：`3_Shader/DashedLine.shader`）实现滚动虚线效果。
- **统一调度**：`PlayerRender.Tick()` 由 `Player.Tick()` 内部调用（`GameLogicManager` 不再持有 PlayerRender 引用）。

### 战斗动画（实现 `ICombatAnimation`）

- **PlayAttackAnimation**：发射成功时触发，使用 DOTween 在 `player.FireInterval` 秒内做绕 Z 轴的 360° 旋转（`RotateMode.FastBeyond360 + Ease.Linear`），起手时 `Kill()` 上一段补间，结束自动归零。
- **PlayHitAnimation / PlayDeathAnimation**：默认空实现，预留扩展（震屏 / 闪红 / 粒子等）。
- DOTween 插件位于 `Assets/Plugins/DOTween/`，无需场景额外初始化。

---

## 与项目文档的对应

- 脚本路径：`Assets/1_Scripts/Player.cs`、`Assets/1_Scripts/PlayerRender.cs`、`Assets/1_Scripts/ICombatAnimation.cs`
- 资源：瞄准虚线 `Assets/3_Shader/DashedLine.shader` + `Assets/7_Res/dashed_line.mat`；DOTween 插件 `Assets/Plugins/DOTween/`
- 详细接口与配置见 **doc/Design/PROJECT.md** 中「4.9 Player」「4.10 PlayerRender」「4.19 ICombatAnimation」。

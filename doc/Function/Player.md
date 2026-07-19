# 角色（Player）

玩家主控单位：固定位置的发射器，负责旋转瞄准、发射弹球，并承受来自 Unit 的伤害。逻辑在 `Player.cs`，渲染在 `PlayerRender.cs`。

---

## 职责划分

| 脚本 | 职责 |
|------|------|
| **Player.cs** | 旋转、FIFO 弹珠队列、发射冷却、生命值；按 `BallType` Addressables 地址生成球 |
| **PlayerRender.cs** | 方向预览虚线（LineRenderer + DashedLine）+ `ICombatAnimation`（DOTween 攻击旋转） |

---

## 功能需求

### 移动与旋转

- **不移动**：Player 本体位置固定。
- **旋转**：A / D 旋转瞄准；正 Y 为前方；限制 ±`maxAngle`（默认 80°）。
- **炮口**：可选 `muzzle` Transform，旋转与发射以此为准，本体可不转。

### 发射弹球

- **按键**：F 发射；冷却读 `BallStats.FireInterval`。
- **队列**：全局 FIFO `Queue<BallType>`；开局入队 `initialBallCount` 个 `Base`；发射 = 队首出队。
- **补充**：球触底回收 → `AddPinBall(type)` 入队尾（不改 `totalBalls`）。
- **升级扩容**：`AddBalls(type, count)` 入队尾并增加容量；非 Base 记入 `UnlockedSpecials`。
- **地址表**：实例字典 `ballAddress`（BaseBall / FireBall / …），经 PoolManager + Addressables 出池。

### 生命值与受伤

- `maxHp` / `currentHp` / `IsDead`；`Init` 重置。
- Unit 触底 → `TakeDamage(unit.Attack)` → 受击/死亡动画；死亡则 `EndGame()`。
- HUD：`InGameUI` 读 HP 与 `BallQueue`（纵向心形 + 纵向球图标）。

---

## 渲染（PlayerRender）

- 预览线：遇 Border 反射、遇 Unit 或底边停止；材质 `dashed_line.mat`。
- `PlayAttackAnimation`：DOTween 在 `FireInterval` 内 360° 旋转；受击/死亡预留空实现。

---

## 与项目文档的对应

- 脚本：`Assets/1_Scripts/Player.cs`、`PlayerRender.cs`、`ICombatAnimation.cs`
- 资源：`3_Shader/DashedLine.shader`、`7_Res/dashed_line.mat`、DOTween
- 详细见 **PROJECT.md**「4.9 / 4.10」；队列与升级见 **Upgrade.md**。

# 角色（Player）

玩家主控单位：固定位置的发射器，负责旋转瞄准与发射弹球。逻辑在 `Player.cs`，渲染在 `PlayerRender.cs`。

---

## 职责划分

| 脚本 | 职责 |
|------|------|
| **Player.cs** | 旋转、发射、弹药容量、冷却间隔等全部逻辑 |
| **PlayerRender.cs** | 形象渲染 + 方向预览线（LineRenderer） |

---

## 功能需求

### 移动与旋转

- **不移动**：Player 位置固定，不能平移。
- **旋转**：通过 **A / D** 键左右旋转。
- **正方向**：正 Y 轴为朝向正前方（0° 时朝上）。
- **旋转限制**：左右旋转均不超过 **80°**（即总范围 ±80°）。

### 发射弹球

- **按键**：按 **F** 从 Player 位置、沿当前朝向发射 PinBall。
- **容量**：Player 拥有的 PinBall 数量有上限（如 5），发射会消耗数量。
- **间隔**：两次发射之间有冷却时间，避免连发。
- **补充**：弹球触底回收后，由游戏逻辑自动给 Player 补充数量；数量为 0 时无法发射，直到被补充。

---

## 渲染（PlayerRender）

- **形象**：渲染 Player 外观（当前可用 Image 或 SpriteRenderer）。
- **方向预览线**：使用 **LineRenderer** 从 Player 位置沿当前方向画一条直线：
  - **长度**：由参数控制最大长度。
  - **碰到 Unit**：线在该处停止，不再向前延伸。
  - **碰到 Border**：按入射角做**镜面反射**，沿新方向继续画线。
  - 重复反射与碰撞检测，直到达到最大长度或碰到 Unit / 底边。

---

## 与项目文档的对应

- 脚本路径：`Assets/1_Scripts/Player.cs`、`Assets/1_Scripts/PlayerRender.cs`
- 详细接口与配置见 **doc/Design/PROJECT.md** 中「4.7 Player」「4.8 PlayerRender」。

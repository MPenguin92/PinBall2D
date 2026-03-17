# 边界（Border）

场景中的矩形障碍物，由 `Border.cs` 实现。PinBall 碰到 Border 时按运动方向与 Border 方向做镜面反射；底边为特例，不反射而是回收弹球并补充 Player 弹药。

---

## 职责

- 定义一块**矩形区域**（由 `transform` 位置与缩放计算 `BorderRect`）。
- 指定**反弹法线方向**（上/下/左/右），用于镜面反射计算。
- **底边**可单独勾选（`isBottomBorder`），触发“回收弹球 + 补弹”而非反射。

---

## 碰撞与反射

| 情况 | 行为 |
|------|------|
| **普通边（上/左/右）** | PinBall 按当前速度方向与 Border 法线做**镜面反射**，修改方向后继续运动。 |
| **底边** | PinBall **不反射**，立即隐藏并回收入池，同时为 Player **补充 PinBall 容量**。 |

反射与底边判断均在 `PinBallBase.Tick` 中根据 `Border.BounceDirection` 与 `IsBottomBorder` 完成。

---

## 与项目文档的对应

- 脚本路径：`Assets/1_Scripts/Border.cs`
- 详细接口与 Gizmos 见 **doc/Design/PROJECT.md** 中「4.4 Border」。

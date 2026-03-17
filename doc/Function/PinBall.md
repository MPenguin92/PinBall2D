# 弹球（PinBall）

圆形弹球，由 Player 发射，在 Border 围成的区域内运动、反弹，撞击 Unit 扣血。逻辑在 `PinBallBase.cs`，渲染在 `PinBallRender.cs`；Base 表示基类，便于将来继承实现各种特殊弹球。

---

## 职责划分

| 脚本 | 职责 |
|------|------|
| **PinBallBase.cs** | 运动、与 Border/Unit 碰撞、镜面反射、底边回收、速度与最小速度 |
| **PinBallRender.cs** | 渲染弹球外观（当前可用 Image / SpriteRenderer），预留扩展） |

---

## 产生与回收

- **发射**：由 Player 按 **F** 从 Player 位置、沿当前方向发射；初始**方向**来自 Player，**速度**为 PinBall 自身属性（可配置）。
- **缓存池**：PinBall 由 **PoolManager** 管理，数量较多；**创建/出池**即显示并参与逻辑，**隐藏/入池**即回收到底边或逻辑移除时。

---

## 运动与碰撞

- **圆形**：碰撞检测按圆形（半径取自 `transform`）与矩形（Border 的 `BorderRect`、Unit 的 `UnitRect`）做重叠判断。
- **上/左/右 Border**：按当前运动方向与 Border 方向做**镜面反射**；反射后速度大小由自身**速度变化属性**决定（可加速或减速），并有**最小速度**限制，防止静止。
- **底边 Border**：不反射，PinBall 立即隐藏并回收入池，同时为 Player 补充弹药。
- **Unit**：碰撞时做镜面反射并对 Unit 扣血；Unit 血量为 0 时由 PoolManager 回收 Unit。

---

## 与项目文档的对应

- 脚本路径：`Assets/1_Scripts/PInBall/PinBallBase.cs`、`Assets/1_Scripts/PInBall/PinBallRender.cs`
- 详细接口、Tick 流程与池化见 **doc/Design/PROJECT.md** 中「4.5 PinBallBase」「4.6 PinBallRender」。

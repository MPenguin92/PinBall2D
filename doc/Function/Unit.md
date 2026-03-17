# 单位（Unit）

游戏中的方块单位，可被弹球撞击扣血，血量为 0 时隐藏并回收入池。逻辑基类为 `UnitBase.cs`，渲染为 `UnitRender.cs`，便于将来继承扩展特殊单位。

---

## 职责划分

| 脚本 | 职责 |
|------|------|
| **UnitBase.cs** | 基类：生命值、碰撞矩形、受击扣血、碰撞法线计算、回收入池条件 |
| **UnitRender.cs** | 渲染单位形象（当前可用 Image / SpriteRenderer），按 HP 比例变色 |

---

## 形态与属性

- **形状**：正方形游戏单位（由 `transform.localScale` 与位置得到矩形 `UnitRect`）。
- **生命值**：拥有 HP 属性；PinBall **接触** Unit 时扣除生命值。
- **消灭**：当生命值降为 **0** 时，Unit **隐藏并回归缓存池**（由 PoolManager 回收）。

---

## 碰撞与反射

- 与 Border 类似：PinBall 碰到 Unit 时，根据**当前位置与 Unit 矩形**计算碰撞面法线，做**镜面反射**，然后继续运动。
- 反射逻辑与 Border 一致（方向反射 + 可选速度变化），仅在法线来源上不同（Border 用配置方向，Unit 用几何最近面）。

---

## 扩展

- **UnitBase** 为基类，可继承实现特殊 Unit（不同血量、行为、奖励等）。
- 渲染暂定 Image / SpriteRenderer，由 `UnitRender` 统一管理外观与 HP 反馈。

---

## 与项目文档的对应

- 脚本路径：`Assets/1_Scripts/Unit/UnitBase.cs`、`Assets/1_Scripts/Unit/UnitRender.cs`
- 详细接口与池化见 **doc/Design/PROJECT.md** 中「4.9 UnitBase」「4.10 UnitRender」。

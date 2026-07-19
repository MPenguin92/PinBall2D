# 弹球（PinBall）

圆形弹球，由 Player 发射，在 Border 围成的区域内运动、反弹，撞击 Unit 扣血。逻辑在 `PinBallBase.cs`，渲染在 `PinBallRender.cs`；派生类实现各特殊球效果。

---

## 职责划分

| 脚本 | 职责 |
|------|------|
| **PinBallBase.cs** | 运动、Border/Unit 碰撞、镜面反射/穿透/最大反弹、命中方向伤害、底边回收；数值统一读 `BallStats` |
| **PinBallRender.cs** | 按 `BallType` 套用 `BallSpriteSet` / `BallTrailSet`（Addressables）；出池后重置拖尾 |
| **FirePinBall** 等 | 命中钩子 `OnHitUnit`：AOE / 减速 / 链电 / DoT / 击退 / 回旋 |

---

## 产生与回收

- **发射**：Player 按 **F** 从炮口沿朝向发射；类型来自 FIFO 队列出队的 `BallType`，初速来自 `BallStats.InitialSpeed`。
- **Addressables**：`Player` 内 `ballAddress` 映射 `BallType →` 短地址（`BaseBall` / `FireBall` / …），`PoolManager` 按地址取 prefab。
- **缓存池**：由 **PoolManager** 管理；底边触碰或超过 `MaxBounces` 时回收，并按 `BallType` 入队尾补弹。

---

## 运动与碰撞

- **圆形**：半径取自 `transform.localScale`，与 Border / Unit 的 AABB 做重叠判断。
- **上/左/右 Border**：镜面反射；速度经 `BounceSpeedMul` / `BounceAccel` 等修饰，受 `MinSpeed` / `MaxSpeed` 钳制。
- **底边 Border**：不反射，立即回收并 `player.AddPinBall(type)`。
- **Unit**：按命中法线相对 `Unit.MoveDirection` 判定 Front / Side / Back，伤害 = `BaseDamage * 对应倍率`；击杀时 `RaiseUnitKilled` 再回收。
- **穿透**：击杀后按 `PiercingChance` 决定直行（`PiercingKeepSpeed`）或反弹；未击杀则反弹并可能施加 `HitSlowdown`。
- **VFX**：命中后 `VfxSpawner.PlayBallHit(type, pos, killed)`，地址来自 `VfxCatalog`。

---

## 派生球（OnHitUnit）

| 类型 | 效果概要 |
|------|----------|
| Fire | 命中点 AOE 爆炸 |
| Ice | `ApplySlow`；可形成冰墙堵塞 |
| Lightning | 链式跳跃，伤害衰减 |
| Poison | DoT 持续伤害 |
| Heavy | 击退 + 额外伤害 |
| Boomerang | 首次触底自动回弹一次 |

参数由 `SpecialBallParams` 按球种全局持有，升级时覆盖写入。详情见 **Upgrade.md**。

---

## 与项目文档的对应

- 脚本：`Assets/1_Scripts/PInBall/`
- 预制体：`Assets/2_Prefab/*Ball.prefab`
- 数据：`BallSpriteSet` / `BallTrailSet` / `VfxCatalog`（`8_Data/`，Addressables 短地址同名）
- 详细接口见 **PROJECT.md**「4.7 PinBallBase」「4.8 PinBallRender」；升级与 BallStats 见 **Upgrade.md**。

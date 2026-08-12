# 设计概述

PinBall2D 各模块的设计说明分散在以下文档中，本文档作为索引入口。

---

## 文档索引

| 文档 | 内容 |
|------|------|
| **doc/Function/Player.md** | 角色：玩家发射器（旋转、发射、FIFO 弹珠队列、生命值、方向预览虚线、攻击动画） |
| **doc/Function/Border.md** | 边界：矩形障碍、自动对齐屏幕、镜面反射、底边回收与补弹 |
| **doc/Function/Unit.md** | 单位：1x1 标准尺寸、HP/Attack/Experience、Step 节奏下移、减速与堵塞、UnitCreator 批量生成 |
| **doc/Function/PinBall.md** | 弹球：BallStats 驱动数值、碰撞反射/穿透、派生球种、Sprite/拖尾、命中 VFX |
| **doc/Function/GamePlay.md** | 主逻辑：GameState、GameEvents、统一 Tick、PoolManager、UIManager、Difficulty、VfxSpawner |
| **doc/Function/Upgrade.md** | Roguelike 升级：经验里程碑、品质抽卡、三选一、BallStats / 新球种解锁 |
| **doc/Design/UpgradeList.md** | 升级效果总表：当前所有生效升级的编号、效果、品质与堆叠（持续维护） |
| **doc/Data/DifficultyBalance.md** | 难度曲线：字段含义、当前 CSV、调参原则、与升级经验的关系 |
| **doc/Design/PROJECT.md** | 项目总览：目录结构、脚本说明、架构原则、流程与配置建议 |

---

## 快速跳转

- 想了解**某个脚本放在哪、做什么** → **PROJECT.md**
- 想了解**玩法与模块职责** → 上表对应模块的 `.md`
- 想了解**整局如何驱动、状态与事件、谁管池、谁管 UI** → **doc/Function/GamePlay.md**
- 想了解**难度与经验数值怎么配** → **doc/Data/DifficultyBalance.md**
- 想了解**局内升级与特殊球** → **doc/Function/Upgrade.md**
- 想扩展**新的 Unit / 球种 / UI / 系统** → 参考 **PROJECT.md 第 7 节「扩展与维护」**

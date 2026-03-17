# 设计概述

PinBall2D 各模块的设计说明分散在以下文档中，本文档作为索引入口。

---

## 文档索引

| 文档                               | 内容                                                                 |
| ---------------------------------- | -------------------------------------------------------------------- |
| **doc/Function/Player.md**   | 角色：玩家发射器（旋转、发射、弹药、PlayerRender 方向预览线）        |
| **doc/Function/Border.md**   | 边界：矩形障碍、镜面反射、底边回收与补弹                             |
| **doc/Function/Unit.md**     | 单位：方块单位、HP、受击扣血、镜面反射、回收入池                     |
| **doc/Function/PinBall.md**  | 弹球：发射、运动、碰撞反射、速度与最小速度、池化                     |
| **doc/Function/GamePlay.md** | 主逻辑：游戏状态、GameLogicManager 与 PoolManager、统一 Tick、缓存池 |
| **doc/Design/PROJECT.md**    | 项目总览：目录结构、脚本说明、流程与配置建议                         |

---

## 快速跳转

- 想了解**某个脚本放在哪、做什么** → **PROJECT.md**
- 想了解**玩法与模块职责** → 上表对应模块的 `.md`
- 想了解**整局如何驱动、游戏状态、谁管池** → **doc/Function/GamePlay.md**

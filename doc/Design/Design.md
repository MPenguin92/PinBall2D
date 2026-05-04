# 设计概述

PinBall2D 各模块的设计说明分散在以下文档中，本文档作为索引入口。

---

## 文档索引

| 文档                         | 内容                                                                                                 |
| ---------------------------- | ---------------------------------------------------------------------------------------------------- |
| **doc/Function/Player.md**   | 角色：玩家发射器（旋转、发射、弹药、生命值、方向预览虚线、攻击/受击/死亡动画）                       |
| **doc/Function/Border.md**   | 边界：矩形障碍、自动对齐屏幕、镜面反射、底边回收与补弹                                               |
| **doc/Function/Unit.md**     | 单位：1x1 标准尺寸、HP、攻击力、Step 节奏下移、SimpleUnit 触底扣血、UnitCreator 批量生成、战斗动画接口 |
| **doc/Function/PinBall.md**  | 弹球：发射、运动、碰撞反射、速度与最小速度、池化                                                     |
| **doc/Function/GamePlay.md** | 主逻辑：游戏状态、生命周期事件 + Step 节奏心跳（GameEvents）、GameLogicManager / PoolManager / UIManager（含 InGameUI HUD）、统一 Tick、ICombatAnimation 战斗动画 |
| **doc/Design/PROJECT.md**    | 项目总览：目录结构、脚本说明、架构原则（含难度系统/数据流水线/资源加载/DOTween/星空背景）、流程与配置建议 |

---

## 快速跳转

- 想了解**某个脚本放在哪、做什么** → **PROJECT.md**
- 想了解**玩法与模块职责** → 上表对应模块的 `.md`
- 想了解**整局如何驱动、状态与事件、谁管池、谁管 UI** → **doc/Function/GamePlay.md**
- 想扩展**新的 Unit 类型 / 新生成策略 / 新 UI / 新系统** → 参考 **PROJECT.md 第 7 节「扩展与维护」**

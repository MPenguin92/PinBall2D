# 升级效果总表（已清空，重新设计中）

> 升级体系已大幅精简（2026-09-03）：弹珠改为**无限发射**（无库存、无队列），
> **newball（新球/扩容）大类已整体删除**，仅保留 stat 大类与抽卡机制框架。
> 重新设计完成后需同步更新本表与 `doc/Function/Upgrade.md`。

## 当前保留（架构 / 机制）

- **stat（数值类）**：`BallStatUpgradeData`，通过修改 `BallStats` 的 stat 生效；
  `BallStatType` 为空枚举，待重新定义
- **抽卡机制框架**（保留可用）：经验累积 → `KillMilestones.csv` 里程碑 → HUD 宝箱 → 三选一面板
  （`UpgradeService` / `UpgradeSelectionUI` / `GameEvents` 相关事件）
- **空池子**：`UpgradeCatalog.asset` 已置空（entries = []）

## 已删除 / 已清空

| 项目 | 状态 |
|------|------|
| `NewBallUpgradeData` / `SpecialBallParams` / `Upgrades_NewBall.csv` | 已删除 |
| `UpgradeContext.SpecialParams`、`GameLogicManager.SpecialBallParams` | 已删除 |
| `Upgrades_Stat.csv`（原 9 条）、`BallStatDefaults.csv` | 只留表头 |
| `8_Data/Upgrades/*.asset`、`BallStatDefaultsTable.asset` | 已删除 / 空表 |
| 弹珠库存队列（`Player.ballQueue` / `totalBalls` / 出入队逻辑） | 已删除，改为无限发射 |
| `InGameUI` 右下角弹珠队列 + `GameHUD.prefab` 的 BallQueueRoot | 已移除 / 已隐藏 |
| `BallStatType` 枚举值、词条效果逻辑（暴击/处决/回血/倍率等） | 已清空 |

## 重建提示

- 修改配表后在 Unity 执行 `Tools/Data/Import All` 重新导入（已不再读取 `Upgrades_NewBall.csv`）。
- 重新设计 stat 时：`BallStatType` 回填枚举 → `BallStats.Reset()` 设默认值 → 按类型补充钳制规则 → 使用方（Player / PinBallBase）接回读取。

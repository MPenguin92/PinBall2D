# 升级效果总表（已清空，重新设计中）

> 升级词条体系已于 2026-09-01 全量清空，仅保留架构与机制框架，具体内容待重新设计。
> 重新设计完成后需同步更新本表与 `doc/Function/Upgrade.md`。

## 当前保留（架构 / 机制）

- **两个大类区分**：
  - `stat`（数值类）→ `BallStatUpgradeData`，通过修改 `BallStats` 的 stat 生效；`BallStatType` 已清空为空枚举，待重新定义
  - `newball`（新球 / 扩容类）→ `NewBallUpgradeData`，通过 `SpecialBallParams` 参数 + `Player.AddBalls` 生效；`BallType` 仅剩 `Base`
- **抽卡机制框架**（保留可用）：经验累积 → `KillMilestones.csv` 里程碑 → HUD 宝箱 → 三选一面板（`UpgradeService` / `UpgradeSelectionUI` / `GameEvents` 相关事件）
- **空池子**：`UpgradeCatalog.asset` 已置空（entries = []）

## 已清空

| 项目 | 状态 |
|------|------|
| `Upgrades_Stat.csv`（原 9 条） | 只留表头 |
| `Upgrades_NewBall.csv`（原 2 条） | 只留表头 |
| `BallStatDefaults.csv` / `BallStatDefaultsTable.asset` | 只留表头 / 空表 |
| `8_Data/Upgrades/*.asset`（原 6 个词条） | 已删除 |
| `BallStatType` 枚举值（伤害/速度/暴击/处决/回血/倍率等） | 已清空 |
| 词条效果逻辑（暴击、处决、振奋回血、反弹加速、命中倍率等） | 已移除，球回到基础行为 |

## 重建提示

- 修改配表后在 Unity 执行 `Tools/Data/Import All` 重新导入。
- 重新设计 stat 时：`BallStatType` 回填枚举 → `BallStats.Reset()` 设默认值 → 按类型补充钳制规则 → 使用方（Player / PinBallBase）接回读取。

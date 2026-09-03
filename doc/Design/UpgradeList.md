# 升级效果总表（重新设计中）

> 升级体系已推倒重来（2026-09-03 起）：清空旧数值词条，先搭底层支持再填内容。
> 当前第一个测试词条：**连发（Burst）**。完成后同步更新本表与 `doc/Function/Upgrade.md`。

## 底层支持（已完成）

- **弹珠时机事件 `BallEvents`**：发射 / 命中 / 恰好击杀 / 反弹 / 返回（触底），
  只读 struct 上下文、零分配，供效果订阅（`Assets/1_Scripts/PInBall/BallEvents.cs`）
- **射击策略 `FireStrategy`**：Single / Burst（连发）/ Fan（扇形）互斥策略，
  `Player` 只提供发射能力（`IFireExecutor`），策略决定"一次射击产出什么"
- **抽卡机制框架**（保留可用）：经验 → 里程碑 → HUD 宝箱 → 三选一

## 配表结构（2026-09-04 起）

升级数据拆成两层，加字段 = 加列，扩展不再散落：

1. **`Upgrades.csv`（通用元信息，展示用）**
   列：`id, name, desc, rarity, maxLevel`
   - `desc` 为**抽象概括**（描述效果类别，如「增加每次射击的弹珠数量」），不做数值
   - `maxLevel` = 满级等级，抽到一次升 1 级，满级后从池剔除
   - 后续 icon 等通用展示列加这里

2. **类型专有表（每类词条一张，逐级）**
   一行 = 一个词条的某一级（`id, level, ...`），`level` 从 1 起；
   某级未配置的行会沿用上一级数据。
   - `Upgrades_Fire.csv`（射击类）：`id, level, desc, shots, interval`
     - `desc` = **等级化描述**（选卡时展示「升到该级后」的文案，如「每次发射 3 颗弹珠」）
     - `shots` = 该级每次发射球数（**直接取值**，不做推导）
   - 运行时展示走 `UpgradeBase.OfferDescription`：子类词条返回目标等级的专有描述，基类回退通用 `desc`

## 词条（测试中）

| id | 名称 | 类型 | 机制 | 满级 |
|----|------|------|------|------|
| `burst` | 连发 | Fire（行为） | 逐级配表：发射 2→6 颗，间隔 0.08→0.04 | 5 |

- 实现：`FireBurstUpgradeData`（每级 `FireLevelData{desc, shots, interval}`，Apply 直接取该级 shots/interval 替换 FireStrategy）
- 导入：`DataImporter` 读 `Upgrades.csv` 元信息 + `Upgrades_Fire.csv` 逐级数据 → 生成 `Fire_burst.asset` 写入 `UpgradeCatalog`

## 重建提示

- 修改配表后在 Unity 执行 `Tools/Data/Import All` 重新导入。
- 新增行为词条类时：在 `Upgrades.csv` 加元信息行 → 新建类型专有表/行 → `DataImporter` 加对应导入方法。

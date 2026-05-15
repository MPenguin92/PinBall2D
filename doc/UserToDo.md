# 待 Unity 编辑器手动处理的事项

每条改完代码后无法用脚本完成、需要在 Unity 编辑器里点一下的事项,记录在此。完成一条勾一条。

## 弹珠库存改 FIFO 队列(2026-05-11)

背景:Player 的弹珠库存从「按 BallType 分槽 + 优先级」重构为全局 FIFO 队列,涉及 [Player.cs](../Assets/1_Scripts/Player.cs)、[BallStats](../Assets/1_Scripts/Upgrade/BallStats.cs)、[Upgrades_NewBall.csv](../Assets/9_Excel/Upgrades_NewBall.csv) 等。代码层全部完成,以下需在编辑器内手动跑一次。

- [ ] **重新跑 DataImporter**:菜单 `Tools/Data/Import All`(或单跑 `Import Upgrades`),根据新 CSV 重建 SO:
  - 新生成 `Assets/8_Data/Upgrades/NewBall_new_base_more.asset`
  - `UpgradeCatalog.asset` 自动重新引用,旧的 `Stat_ball_slot_more` 不再被列入抽卡池
- [ ] **删除残留 SO 文件**(可选,只是清洁):删掉 `Assets/8_Data/Upgrades/Stat_ball_slot_more.asset`(及 `.meta`),它不会再被 Catalog 引用,留着也无害但会污染目录
- [ ] **Player Inspector 检查 Initial Ball Count**:打开场景里的 Player GameObject,新增了 `Initial Ball Count` 字段,默认 5。如想改初始普通球数,在这里调

## 特殊球改「单实例 + 5 级」(2026-05-11)

背景:每种特殊球(Fire/Ice/Lightning/Poison/Heavy/Boomerang)在玩家库存中**至多 1 颗**;再次抽到同一条 = 升级。CSV 列结构变更,旧的 SO 资源需要清理,且老的多条 `new_xxx_basic`/`new_xxx_more` 词条已合并为 `new_xxx` 单行 × 5 级。

- [ ] **重新跑 DataImporter**:菜单 `Tools/Data/Import All`(或 `Import Upgrades`),会按新 CSV 重新生成:
  - `NewBall_new_fire.asset` / `NewBall_new_ice.asset` / `NewBall_new_lightning.asset` / `NewBall_new_poison.asset` / `NewBall_new_heavy.asset` / `NewBall_new_boomerang.asset`
  - `UpgradeCatalog.asset` 自动重建,只引用上面这些 + `NewBall_new_base_more.asset`
- [ ] **删除旧的 SO 文件**(必须做,否则磁盘上多出残留资源):
  - 旧名清单:`NewBall_new_fire_basic` / `NewBall_new_fire_bigger` / `NewBall_new_ice_basic` / `NewBall_new_ice_more` / `NewBall_new_ice_aoe` / `NewBall_new_lightning_basic` / `NewBall_new_lightning_more` / `NewBall_new_poison_basic` / `NewBall_new_heavy_basic` / `NewBall_new_boomerang_basic` / `NewBall_all_specials_plus`
  - 路径:`Assets/8_Data/Upgrades/`,连同对应的 `.meta` 一起删
  - 这些不在新 CSV 里,Catalog 不会再引用,但留在磁盘会污染目录
- [ ] **`all_specials_plus` 万象升级已删除**:原本的 Legendary 在新模型下没有合理语义。如想恢复一个 Legendary(例如"所有已解锁特殊球各 +1 级"),需要在代码里另写,或后续我们再讨论
- [ ] **Boomerang 行为变化**:回旋球之前固定回旋 1 次,现在按 `extraReturns` 参数(Lv1=1, Lv5=5)允许 N 次。Lv1 行为与原版一致,所以不会破坏既有手感

## 弹珠属性默认值改配表(2026-05-15)

背景:[BallStats.cs](../Assets/1_Scripts/Upgrade/BallStats.cs) 原本在 `Reset()` 里硬编码 14 个 stat 的默认基础值,改为读取新 SO `BallStatDefaultsTable`(由 [BallStatDefaults.csv](../Assets/9_Excel/BallStatDefaults.csv) 生成)。SO 缺失或缺项时仍然回退到代码里的硬编码兜底,所以游戏不会因为没导入而崩。

- [ ] **跑 DataImporter**:菜单 `Tools/Data/Import All`(或单跑 `Tools/Data/Import Ball Stat Defaults`),会生成 `Assets/8_Data/BallStatDefaultsTable.asset`
- [ ] **加入 Addressables**:把 `Assets/8_Data/BallStatDefaultsTable.asset` 加入 `Data` 组,**地址必须为 `BallStatDefaults`**(与 [GameLogicManager.cs](../Assets/1_Scripts/Mgr/GameLogicManager.cs) `AssetLoader.Load<BallStatDefaultsTable>("BallStatDefaults")` 对应)
- [ ] **(可选)调整数值**:之后改基础值只需编辑 `BallStatDefaults.csv` → 重跑 Import → 更新 Addressables bundle 即可,无需改代码

## 升级触发改"经验值"(2026-05-15)

背景:升级里程碑从「累计**击杀数**」改为「累计**经验值**」。每个 Unit 给的经验由 [Difficulty.csv](../Assets/9_Excel/Difficulty.csv) 当前阶段 `unitExperience` 列决定(早期 1xp,晚期 50xp);[KillMilestones.csv](../Assets/9_Excel/KillMilestones.csv) 第一列从 `killThreshold` 改名为 `experienceThreshold`,数值已按经验口径重排(5/25/80/200/450/900/1600/2800),策划可继续微调。HUD 文案 `Kills cur/next` 改成了 `EXP cur/next`。事件名 `OnKillMilestoneReached` 与 SO 类名 `KillMilestoneTable / KillMilestoneData` 保留(改名涉及大量引用与 Addressables 地址)。

- [ ] **重新跑 DataImporter**:菜单 `Tools/Data/Import All` 重新导入 Difficulty(列数从 6→7)与 KillMilestones(首列改名),否则旧 SO 字段对不上会报错或读到 0
- [ ] **(代码已强制)运行时如果 Difficulty 表缺失或某阶段 `unitExperience<=0` 会 `Debug.LogError` 并回退 1**;游戏不会崩,但 Console 出现红字时请立即修表
- [ ] **(可选)按手感再调阈值**:打几局看看升级节奏,直接改 `KillMilestones.csv` 与 `Difficulty.csv` 的 `unitExperience` 列重导即可

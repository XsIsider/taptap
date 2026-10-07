# 古堡调查原型 · 第一版

打开 `Scenes/CastlePrototype.unity` 后 Play，或运行本地 `Builds/CastleV2/Castle.exe`。这是最新《程序说明文档》的独立示例案件，正式剧情待替换。运行时内容来自 `ContentV2.asset`，配置来自 `GameSettings.asset`；旧版 `CastleDatabase` 已移除。

## 体验路线

1. 开始调查，等待自动对白结束，点击继续。管家对白中点击“地图”；漏领可在场景“对白回看 / 补领”取回。
2. 前往女主房间，按引导打开放音机，选择剧情磁带并播放到结尾，返回对白点击继续。
3. 去书房，和访客交谈并检查维护簿。末句完成收录现场文件，继续才结算事件。
4. 去中控室，中央档案自动导入个人文件。三个固定文件各有可标记声响，游标拖到 0 秒点击标记。频道 C 是第二次声响干扰。
5. “分析与重构”中匹配 A/B，同环境证据定位 B，再选 A 作参照，将 B 移动 +180 秒。正确后仍需点击奖励对白的继续。
6. 离开中控室，到大厅检查登记台；若错过时窗，自动使用备用留言。路径 A=大厅、B=书房、C=大厅，可切换楼层。
7. 排列三张已验证事件卡，选择最终推论，观看波形重构。已完成事件/结局可在对白回看中重播。

录音刻录：在中控室文件页选择磁带，空白直接写入，已有内容先确认旧/新文件。只有选中的实例改变。回房间放音机可访问持有磁带。磁带覆盖不清除证据和草稿。

地图/调查册快捷键：场景中 M / J；Esc 返回或关闭最上层弹窗。世界时间不会随真实等待流逝，进入其他页面也不会额外消耗中控室时间；离开中控室只计一次 15 分钟。房间可休息到次日 08:00。

## 开发入口

- 六表导入：`Tools > Castle > Content > Import All Six Tables`；`Reimport Plot Only` 只替换 Plot 并全量校验。
- 自动验证：`Tools > Castle > Validate Planning V1` 或 Test Runner 的 Castle.Editor.Tests。
- 构建：`Tools > Castle > Build Windows Prototype`。
- 开发播放器参数 `-castleV2Smoke` 执行隔离存档自动通关与离屏截图，输出构建目录 Screenshots。仅测试进程改变渲染方式；普通启动沿用 URP。
- 规则/界面分离，代码在 Scripts/V2；六表和配置说明见 Content/README.md。

保存位于 Unity persistentDataPath 的 `castle-save-v2.json`，独立于旧 v1。不要把 Library、Temp、Logs、Builds 或玩家存档提交到仓库。

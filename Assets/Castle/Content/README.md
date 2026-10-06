# 第一版内容导入与联调

六表目录：`Assets/Castle/Content/CSV/`。文件编码 UTF-8，支持 CSV 标准双引号、逗号和单元格内换行。不要修改稳定 ID 来“改名”，显示名称可单独修改。正式 Plot 尚未提供，当前内容为示例，不是正式案件结论。

1. 修改 Room、Event、Plot、Record、Entry、Puzzle.csv。
2. Unity 菜单 `Tools > Castle > Content > Import All Six Tables`。仅替换正文时可用 `Reimport Plot Only`。
3. 查看 Console 和 `Logs/castle-content-import.txt`。诊断包含表、源文件行、字段、原因；失败不会发布半成品数据库。
4. 运行 `Tools > Castle > Validate Planning V1`，然后进入 CastlePrototype 场景试玩。
5. 按需执行 `Tools > Castle > Build Windows Prototype`，输出 `Builds/CastleV2/`。

`ContentVersion.txt` 是内容版本。仅文字校对可以保持版本，改变稳定 ID/案件结构时升级版本；不兼容的已有 v2 存档会保留且拒绝覆盖，先备份移走后新开调查。v1 文件从不读取、迁移或删除。

配置资产位于 `Resources/Castle/`：GameSettings、Devices、Characters、Map、Hotspots。Setup 只创建缺失示例配置，不重置已有 Inspector 设置。Room 不增加 scene 必填列；Map.Rooms 负责背景绑定。音效在 GameSettings.Sounds 按 Plot 行 ID 绑定；未配素材的锚点使用合成占位提示音。

列表用分号，奖励 `ID*数量`，路径/排序 `标签=ID`，完整字段以最新程序说明为准。Plot 链接正文必须包含 Entry.name；含锚点的录音必须用独立文本行 ID。每日窗口为 [start,end)，结束可以 24:00。offset 正为设备快，负为设备慢，移动量与 offset 相反。

校验是静态引用/格式/明显依赖死锁检查，不是任意消耗、多日时间路线的模型检查器。正式案件仍需跑完整可达性试玩。添加新场景/节点时，应同时配置 Map 楼层和房间背景、节点和 Hotspots。

开发辅助脚本 `Tools/generate_castle_example.py` 会重建示例 CSV，正式填表后不要再次运行它覆盖策划内容。

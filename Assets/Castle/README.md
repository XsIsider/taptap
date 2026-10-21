# 古堡 · 声音与时间的谜题

Unity 2022.3.62f3 原生 uGUI 可玩原型。根据提供的《古堡中控室_可点击原型 (3).html》重建交互；HTML 作为产品参考，不执行其中脚本。

## 启动

打开 `Assets/Castle/Scenes/CastleEditable.unity`，点击 Play。
也可选择菜单 **Tools → Castle → Open Editable Scene**。
该场景放在 Build Settings 首位，旧 CastlePrototype 和 SampleScene 保留。

Canvas、按钮、滑块、文字、地图和固定页面已保存为场景对象，在编辑模式即可修改。
调查册的可变卡片通过 `Prefabs/JournalCard.prefab` 创建并复用。界面以 1600×900 为基准等比适配。

直接操作步骤、对象树和修改对照表见 [场景编辑指南](../Docs/CastleEditable_场景编辑指南.md)；每个页面实际的 Id、组件类型和对象路径见 [实际绑定清单](../Docs/CastleEditable_绑定清单.md)。旧的 `prototype_test_UI*` 文档是迁移前设计草案，以这两份已实施文档为准。

## 已实现

- 标题、新游戏覆盖确认、继续、设置和退出。
- 邀请函展开与收纳、进入古堡、六段女爵对白、随身对话记录。
- 两层地图、未探索遮罩、房间许可和开放时间、主动等待、移动消耗两分钟。
- 中控室录音浏览、两设备声音锚点、证词摘录、文字回放与进度定位。
- 整条设备轨道拖动、滑块和方向键调整校正量、候选组与对齐检查。
- 声源选择、历史门状态和依据、传播草稿、撤销、联合验证。
- 录音身份关联、三节点人物路线、位置证据、独立保存已确认结论。
- 最后物证关系、三段动态真相重建、两种结局与返回结局前。
- 分类调查册、本地 JSON 自动存档、上一个存档备份、字号/音量/动态设置。

## 工作台与调查册布局（2026-10-09）

`prototype_test` 保留第一版案件规则，中控工作台按草图改为绿板上的“录音 / 锚点 / 房间”三个标签页。录音左侧选文件、右侧滚动阅读，声音行直接标记/取消锚点，证词行可摘录；底部保留播放与定位。锚点页保留候选组、两条轨道、拖动/滑条/方向键校时及文本对照。房间页保留声源放置、两层地图、历史门状态、证据关联、撤销、保存草稿和联合验证；只展示本案实际存在的两层楼。

调查册仅有“人物 / 道具 / 事件”三类，采用三列可滚动卡片。点击卡片打开独立详情页，返回保留分类和滚动位置；Esc 在详情页返回列表，再次 Esc 关闭。女爵详情保存完整对话；证词摘录归入事件列表并标注尚非确认结论。人物详情仍可进入身份/路线推理，事件详情仍可查看已确认事件，道具详情支持展开邀请函。原 v1 存档字段与判定保持兼容。

J：调查册；M：场景导航地图；Esc：关闭覆盖层/设置；空格：继续对白；左右方向键：对齐页面微调一秒。
阅读、工作台分析不推进时钟。原型音频为**文字回放**；只生成了轻量按钮提示音，没有对白配音或实录音效。

## 完整体验路线（含谜题答案）

1. 新游戏 → 邀请函 → 收纳 → 进入古堡 → 读完女爵对白。
2. 地图点中控室 → 等待到 19:00 → 进入 → 查看工作台。
3. 在 REC-01、REC-02 各自点击 05:00 声音片段并设为锚点。
4. 锚点对齐：REC-02 校正 **+03:00**，检查对齐。
5. 房间页：声源选**餐厅**，书房门选**关闭**，关联门状态证据，保存草稿，联合验证。
6. 身份：选**林女士**，关联白花证词，确认身份。
7. 人物路线：依次选编号并放置**书房 → 餐厅 → 休息室**，关联位置证据，核对。
8. 事件还原：关联**柜门金属搭扣**，提交，阅读重建，选择结局。

和 HTML 一样，案件阶段所需的旧证词、位置资料及金属搭扣作为现有案件档案提供。其他房间目前为探索占位场景，不包含独立的搜证关卡。原型中的相机推进/翻页演出、真实声学模拟、语音与三维角色移动不在本版范围内。

## 代码结构

- `Scripts/CastleState.cs`：可序列化状态、证据快照、纯规则层；错误判断返回具体反馈。
- `Scripts/CastleDatabase.cs`：ScriptableObject 剧情数据库，包含对白、房间、开放时间、地图坐标和真相文字。
- `Resources/Castle/Database.asset`：可在 Inspector 编辑的数据实例。
- `Scripts/CastleSave.cs`：JSON 持久化；临时文件替换、`.bak` 回退；路径为 `Application.persistentDataPath/castle-save-v1.json`。
- `Scripts/CastleGame.cs`：生命周期、页面切换、场景组件绑定与全局快捷键。
- `Scripts/CastleGame.Exploration.cs`：开场、场景、导航、设置、调查册。
- `Scripts/CastleGame.Workbench.cs`：录音、锚点、校时、地图与传播草稿。
- `Scripts/CastleGame.SimpleUi.cs`：调查册数据、Prefab 卡片复用、详情页与滚动位置。
- `Scripts/CastleGame.Resolution.cs`：事件、身份、路线、还原和结局。
- `Scripts/CastleTimelineDrag.cs`：设备轨道拖动输入。
- `Editor/CastleProjectTools.cs`：打开场景、规则验证和 Windows 构建菜单。
- `Editor/CastleSceneUiBuilder.cs`：仅在编辑器中、场景缺失时创建初始 UI 场景和卡片 Prefab。
- `Scripts/CastleSceneUi.cs` / `CastleUiView.cs`：总引用和页面控件绑定。
- `Scripts/CastleMapView.cs`：房间热点、走廊路点与预置线条。
- `Scripts/CastleJournalCardView.cs`：调查册卡片。
- `Scripts/CastleGame.Smoke.cs`：使用 `-castleSmoke` 参数的自动 UI 通关与截图检查；不读写正式存档。

扩展时先改数据库添加房间与对白；新增地图房间还需要在相应 Map 下创建热点，并加入 CastleMapView.Rooms。现有热点的位置以场景 RectTransform 为准，不会在 Play 时按数据库坐标重新排版。新的案件规则放入独立规则/定义类。当前是单案件框架，谜题答案和证据匹配仍集中在 CastleRules 中；批量生产案件前可进一步抽成案件 ScriptableObject。界面使用原生 Button/Slider，可直接在场景中调整，替换组件时在 CastleUiView.Controls 中重新拖入引用。

## 验证和构建

- **Tools → Castle → Validate Game Flow**：验证开放时间、错误推理、证据门槛、快照隔离与序列化，结果输出 `Logs/castle-validation.txt`。
- **Tools → Castle → Build Windows Prototype**：构建 `Builds/Castle/Castle.exe`。
- 开发版传入 `-batchmode -castleSmoke`：自动点击通关两个结局，截图与运行结果输出工作目录下的 `Logs/CastleScreenshots/`。布局截图在测试进程内使用内置管线的离屏相机，避免隐藏窗口的启动画面干扰；正常游戏仍使用项目原有 URP 管线。

本次场景 UI 迁移在隔离副本中完成 Unity Windows 构建、31 项规则验证和双结局自动 UI 通关，无运行时错误。额外检查了新游戏确认/取消/重置、继续游戏、固定页面不重建、手动按钮位置保留、调查册三类详情及返回滚动位置；截图已核对录音、锚点、地图标记、路线、调查册与设置。运行截图与结果保存在 Logs/CastleEditable/（生成文件不纳入 Git）。

## 资源来源

背景图、地图和中文字体从用户提供的 HTML 内嵌资源提取。`Tools/extract_prototype.py` 可重新提取，需要 Python 与 Pillow。WebP 转为 Unity 可导入的 JPEG，没有下载其他美术素材。字体为原型内嵌 Noto Sans CJK SC，遵循 SIL Open Font License；保留原型及字体授权信息，正式发行前确认原始美术的使用授权。

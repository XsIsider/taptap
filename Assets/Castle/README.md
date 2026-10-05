# 古堡 · 声音与时间的谜题

Unity 2022.3.62f3 原生 uGUI 可玩原型。根据提供的《古堡中控室_可点击原型 (3).html》重建交互；HTML 作为产品参考，不执行其中脚本。

## 启动

打开 `Assets/Castle/Scenes/CastlePrototype.unity`，点击 Play。
也可选择菜单 **Tools → Castle → Open Prototype Scene**。
该场景已放在 Build Settings 首位，原有 SampleScene 保留。

所有 Canvas、按钮和文字由 Castle Game 对象在运行时构建，编辑模式下场景不显示完整 UI。
界面以 1600×900 为基准，在不同窗口比例下等比适配并留黑边。

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

J：调查册；M：场景导航地图；Esc：关闭覆盖层/设置；空格：继续对白；左右方向键：对齐页面微调一秒。
阅读、工作台分析不推进时钟。原型音频为**文字回放**；只生成了轻量按钮提示音，没有对白配音或实录音效。

## 完整体验路线（含谜题答案）

1. 新游戏 → 邀请函 → 收纳 → 进入古堡 → 读完女爵对白。
2. 地图点中控室 → 等待到 19:00 → 进入 → 查看工作台。
3. 在 REC-01、REC-02 各自点击 05:00 声音片段并设为锚点。
4. 锚点对齐：REC-02 校正 **+03:00**，检查对齐。
5. 声音传播：声源选**餐厅**，书房门选**关闭**，关联门状态证据，保存草稿，联合验证。
6. 身份：选**林女士**，关联白花证词，确认身份。
7. 人物路线：依次选编号并放置**书房 → 餐厅 → 休息室**，关联位置证据，核对。
8. 事件还原：关联**柜门金属搭扣**，提交，阅读重建，选择结局。

和 HTML 一样，案件阶段所需的旧证词、位置资料及金属搭扣作为现有案件档案提供。其他房间目前为探索占位场景，不包含独立的搜证关卡。原型中的相机推进/翻页演出、真实声学模拟、语音与三维角色移动不在本版范围内。

## 代码结构

- `Scripts/CastleState.cs`：可序列化状态、证据快照、纯规则层；错误判断返回具体反馈。
- `Scripts/CastleDatabase.cs`：ScriptableObject 剧情数据库，包含对白、房间、开放时间、地图坐标和真相文字。
- `Resources/Castle/Database.asset`：可在 Inspector 编辑的数据实例。
- `Scripts/CastleSave.cs`：JSON 持久化；临时文件替换、`.bak` 回退；路径为 `Application.persistentDataPath/castle-save-v1.json`。
- `Scripts/CastleGame.cs`：生命周期、页面切换、原生 UI 组件构建与全局快捷键。
- `Scripts/CastleGame.Exploration.cs`：开场、场景、导航、设置、调查册。
- `Scripts/CastleGame.Workbench.cs`：录音、锚点、校时、地图与传播草稿。
- `Scripts/CastleGame.Resolution.cs`：事件、身份、路线、还原和结局。
- `Scripts/CastleTimelineDrag.cs`：设备轨道拖动输入。
- `Editor/CastleProjectTools.cs`：场景生成、规则验证和 Windows 构建菜单。
- `Scripts/CastleGame.Smoke.cs`：使用 `-castleSmoke` 参数的自动 UI 通关与截图检查；不读写正式存档。

扩展时先改数据库添加房间与对白；新的案件规则放入独立规则/定义类。当前是单案件框架，谜题答案和证据匹配仍集中在 CastleRules 中；批量生产案件前可进一步抽成案件 ScriptableObject。界面使用原生 Button/Slider，可逐步替换为设计好的 Prefab。

## 验证和构建

- **Tools → Castle → Validate Game Flow**：验证开放时间、错误推理、证据门槛、快照隔离与序列化，结果输出 `Logs/castle-validation.txt`。
- **Tools → Castle → Build Windows Prototype**：构建 `Builds/Castle/Castle.exe`。
- 开发版传入 `-batchmode -castleSmoke`：自动点击通关两个结局，截图与运行结果输出工作目录下的 `Logs/CastleScreenshots/`。布局截图在测试进程内使用内置管线的离屏相机，避免隐藏窗口的启动画面干扰；正常游戏仍使用项目原有 URP 管线。

## 资源来源

背景图、地图和中文字体从用户提供的 HTML 内嵌资源提取。`Tools/extract_prototype.py` 可重新提取，需要 Python 与 Pillow。WebP 转为 Unity 可导入的 JPEG，没有下载其他美术素材。字体为原型内嵌 Noto Sans CJK SC，遵循 SIL Open Font License；保留原型及字体授权信息，正式发行前确认原始美术的使用授权。

# 手工 UI 迁移修改对照表

核对基线：`根据策划案修订的版本` / `910d350`，2026-10-07。依据用户提供的《程序说明文档.pdf》，重点为 5.1～5.10、6、7.2～7.5；结合当前 V2 源码逐项整理。

**本文是待实施清单，不表示这些代码已经修改。** 对象层级、具体组件和 Inspector 字段见 [手工UI对象树与绑定清单.md](手工UI对象树与绑定清单.md)。本方案保留现有 Session/业务服务，将 UI 的创建方式改为场景引用。

## 1. 必改文件与方法

下列源码路径均相对项目根目录 `D:/game_test/taptap`。优先级：P0=手工 UI 能接管的前置；P1=对应页面完整可用；P2=可选增强或后续优化。

| 优先级 | 文件/位置 | 当前实现 | 需要修改的内容 | Inspector/场景动作 | 验证结果 |
|---|---|---|---|---|---|
| P0 | **新增** `Assets/Castle/Scripts/V2/UI/SceneUiBindings.cs` | 无显式场景引用组件 | 新增 MonoBehaviour，按对象树文档定义 Common、13 页、Modal、Templates 引用组；提供必填项检查，报错包含字段/物体路径 | 挂 Canvas；拖入每个引用，页根即使 inactive 也要绑定 | 缺字段启动明确报错；不静默生成另一套 UI |
| P0 | `Assets/Castle/Scripts/CastleGame.cs` / Awake | `new PrototypeUi(this, Session)` | 新增序列化 UiBindings；校验后把引用传入控制器；保留 Settings/Content 加载、Session 创建与初始化顺序 | Castle Game.UiBindings 拖 Canvas 的组件；Settings/Content 沿用现有资源 | 启动只有一个 Session；首屏标题正常 |
| P0 | 同上 / Update、暂停/退出 | 宿主调用 Tick 和每两秒保存 | 保留；新增控制器 Dispose 清理监听与运行时音频资源的生命周期出口；初始化失败时不 Tick | 不再另挂一个重复 Tick 的 UI 驱动器 | 播放不加速，存档调用正常 |
| P0 | `Assets/Castle/Scripts/V2/UI/PrototypeUi.cs` / 构造函数 | new Canvas/Stage/EventSystem，AddComponent AudioSource | 改构造参数接收 SceneUiBindings；引用已有 Canvas/Stage/AudioSource；删除对应创建代码；保留音量、速度偏好和音效策略 | 手建 Canvas、EventSystem、AudioSource，AudioSource 关闭 Play On Awake | 运行时不重复生成基础组件 |
| P0 | 同上 / **新增 BindUi、Dispose** | 每次创建按钮时 AddListener | 静态控件只绑定一次；切页只刷新；保存注册的回调并在 Dispose 移除；动态项 Bind 时解绑旧回调 | Button.OnClick/Slider.OnValueChanged 留空；不要 Inspector 和代码重复接线 | 页面开关十次，点击仍只执行一次 |
| P0 | 同上 / Page | Destroy 旧页、Resources.Load+Instantiate 新页、追加背景和页头 | 用 screen→绑定页根的映射，SetActive 切换13页；引用固定 Header/Status；保留 screen/back、播放停止、缓存重置等语义；不 Destroy 固定页面 | 13 页根都在 PagesRoot 中，公共 Chrome 单独放 | 页数固定；返回按钮、地图/调查册快捷入口正确 |
| P0 | 同上 / CloseModal、Modal、Confirm | 创建/销毁遮罩和弹窗控件 | 固定 ModalRoot 显隐；Body 子组互斥；清理旧确认动作；CloseModal 保留偏好保存；保持单弹窗替换语义 | Shield 开射线，ModalRoot 最后绘制；绑定 Close/Accept | Esc 先关弹窗；覆盖刻录取消不改数据；不点穿 |
| P0 | 同上 / Tick | 以 `_modal` 对象是否存在判断是否暂停底层 | `_modal` 现在常驻，改为独立 `_modalOpen` 或 ModalRoot.activeSelf；禁止仅判断引用非空；对应键盘拦截一起改 | 可加 CanvasGroup 控制底层交互，但不能代替 Tick 门控 | 弹窗关闭后对白/录音恢复；打开时快捷键不穿透 |
| P1 | 同上 / ShowTitle、BeginIntroduction、ShowInvitation | 工厂创建标题和三个过场页 | 给固定图片/文本赋值，切换对应页；绑定 Begin、Continue 等；保留已开始时 Begin 禁用、Continue 按状态开启 | 绑定 Title/Outside/Invitation/Collected 全部控件 | 新档/旧档按钮状态正确；Start 只在进入古堡动作执行 |
| P1 | 同上 / ShowSettings、ShowCredits、RequestExit、RequestReset | 每次创建设置滑条、制作名单、确认框 | 使用固定弹窗子组；音量0～1，速度0.5～2；初始化用 SetValueWithoutNotify；调试重置保留 Editor/Debug 限制；退出/重置仍走确认 | 配置各 Body 字段；SaveTitle 按 Started 显示 | 调整即时生效，重开保留；取消重置不丢档 |
| P1 | 同上 / ShowRoom、Hotspot | 创建背景、底部热点按钮和功能入口 | 固定背景赋 MapConfig.Rooms 图；热点从模板复用或 ManualHotspots 按 ID 绑定；选择全手工位置就删除位置覆盖/自动排布；保留八种模式分发 | 当前6热点按清单填 ID；房间不匹配隐藏 | 点正确热点执行正确入口，多事件仍弹选择框 |
| P1 | 同上 / ShowDialogue、RenderDialogue | 创建对白滚动区、TMP、LinkHandler；逐句绘制 | 使用绑定正文/说话人/Content，保留 preferredHeight 计算或统一换布局；LinkHandler.Click 代码赋值，点击时取当前有效行；保留末句继续门控 | 正文打开 TMP Rich Text 和 Raycast Target，挂 LinkHandler | 自动播字/句、点击收录、末句完成与点击继续分开 |
| P1 | 同上 / 结局相关绘制、BuildWave、AnimateWave | 运行时创建90条波形、游标和摘要 | 改为绑定两个独立的波形数组；进页选当前数组；结局组按状态显示；使用固定游标/摘要；保留 SkipEnding 流程 | 播放页、结局各90条，时间线槽位另20条静态波形 | 切离页面后动画不改错对象，结局跳过仍结算 |
| P1 | 同上 / ShowTravel | Map 页面被用作房间列表 | MapPage 增加 TravelListRoot/MapVisualRoot 两种模式；按入口切换，避免两个布局同时显示 | 两种模式根分别绑定 | 未获得地图时普通旅行入口仍可用 |
| P1 | 同上 / ShowHistory | Journal 页面另建一套历史列表 | JournalPage 增加历史模式；复用 EntryScroll 并刷新/清空；隐藏不适用分类控件，保留回 Room 的返回动作 | 复用历史项模板，绑定对应入口 | 历史重放不额外发奖励 |
| P1 | `Assets/Castle/Scripts/V2/UI/PrototypeUi.Panels.cs` / ShowRecords、ShowAnalysis | 创建工作台标签、设备状态、录音/推理按钮 | 改成绑定固定标签和列表 host；ShowAnalysis 复用 PuzzlesPage 的过滤模式；仍调用 Recordings.Import 和 CanAccess | 4 个标签与各列表父节点绑定 | 新录音导入正常；未知设备位置/偏差不泄露 |
| P1 | 同上 / ShowTapes | 为每盘磁带动态建按钮 | 用 ActionItem 填充；回调捕获磁带**实例 ID**；空白磁带继续只提示 | TapeScroll.Content+模板 | 两盘同类型磁带可各自保存不同录音 |
| P1 | 同上 / ShowPlayback、UpdatePlayback | 创建文字区、进度条和操作按钮 | 换固定引用；Seek 回调保留 tapeId 上下文和 Persist；程序刷新游标用 SetValueWithoutNotify；每次播放页面重新关联当前录音/磁带 | 绑定正文、Clock、WaveBars、Seek、五个动作按钮 | 拖进度不递归回调；暂停恢复、标记提取正确 |
| P1 | 同上 / ShowBurn | 动态创建磁带选择和覆盖确认 | 固定 BurnBody+实例列表；已有内容先显示旧/新文件确认；接受时才 Burn(...,true)；切确认前保存目标实例/record ID | BurnBody 与 ConfirmBody 绑定 | 点击取消不覆盖；确认只改选中一盘 |
| P1 | 同上 / ShowJournal | 每次创建5标签及各类型条目 | 改固定分类按钮+复用条目；按类型显示 Text/Character/JournalEntry 项；刷新先清理旧项；来源、人物、邀请函各用对应弹窗 | 绑定5分类、InvitationCard、列表 | 只展示已获内容；切分类无上次残留 |
| P1 | 同上 / ShowCharacter、ShowSources | 动态创建人物图、信息与来源按钮 | 使用固定弹窗控件+重复项；保留 owner 过滤、路径开放检查；来源按中央/磁带权限，Plot 来源回放 Seek 到行 | Portrait 是 Image/Sprite，Illustration 是 RawImage/Texture2D，不能混拖 | 未知信息不显示；无权限只提示地点，不绕过访问 |
| P1 | 同上 / ShowMap | 根据配置位置新建楼层与节点按钮 | 固定地图与图例，复用/手绑楼层和节点；按 RoomStatus 更新状态；若手工摆坐标，移除每次位置写入 | 导航节点绑定 NodeId；每层独立分组或筛选 | 时段关闭/未解锁/可进入区分；Active 期间不移动 |
| P1 | 同上 / ShowPuzzles、ShowPuzzle | 每次重建推理页、候选项和按钮 | 固定页+类型子组：Choice、Calibration、Slots、Path、Timeline；每次先关所有类型组再开需要组；读取 Draft/Solved/Available 更新状态 | 绑定全部子组、Submit、Journal、Feedback | 六种 type 都有视图；切类型不留下上种控件 |
| P1 | 同上 / BuildCalibration | 新建参照/候选按钮、滑条、两条轨道 | 固定 Track/Marker/ShiftSlider；参照/候选列表复用；保持 Edit 保存 Shift/Reference；describe 逻辑拆成刷新方法；刷新滑条不触发编辑 | 绑定滑条 -600～600、两锚点、位移说明 | 正负号正确：校正时间=报告时间+移动量；设备偏差=-移动量 |
| P1 | 同上 / BuildSlots、PlaceSlot | 新建槽位、候选卡、地图节点与连线，并 AddComponent 拖放组件 | 组件预先放模板；填 Value/Slot，按谜题类型配置 Drop；保留时间线交换、路径同点可复用、跨楼层草稿；选槽/放置后刷新而非重建页面 | 槽位/卡片预挂 CanvasGroup、DragToken；落点挂 DropZone | 点击赋值和拖放同结果；锁定后组件不能继续修改 |
| P1 | **新增** `Assets/Castle/Scripts/V2/UI/UiItemView.cs` | 没有数据项引用组件 | 实现模板的显式字段、Bind/Unbind；保留稳定数据 ID，刷新清空不适用图片/按钮；避免 GameObject.Find/中文名称定位 | 挂各模板根，填模板内部引用 | 复用后无旧文字、旧头像、旧点击目标 |
| P1 | `Assets/Castle/Scripts/V2/UI/DragToken.cs` | BeginDrag 时可能 AddComponent CanvasGroup；EndDrag 查 DropZone | 手工模板必须预挂 CanvasGroup；若要求完全无组件动态创建，改为校验必备组件；必要时补锁定/禁用保护 | CanvasGroup 初始 alpha=1、blocksRaycasts=true | 拖完和取消后恢复，背景不会永久穿透 |
| P1 | `Assets/Castle/Scripts/V2/UI/DropZone.cs`、`LinkHandler.cs` | 用 Action 委托接控制器 | 通常不用改类本身；在 Bind/Unbind 设置/清除委托；不要额外加一套 IDropHandler 造成重复提交 | 对白/节点/槽位挂对位置；文本和目标图形射线正确 | 一次链接点击/一次拖放只执行一次 |
| P1 | `Assets/Castle/Scripts/V2/UI/UiFactory.cs` | 创建全部视觉组件 | 把固定 UI 的调用全部移出；列表也改为作者制作的 prefab；不再使用后可保留为原型工具或删除，但先确认无引用 | 样式转移到场景/prefab | 无残留 _ui.Button/Text/Rect 调用重建固定画面 |
| P1 | `Assets/Castle/Scripts/V2/UI/PrototypePanel.cs` | 空壳 Content 属性 | 可保留，但不作为新绑定接口；如要删，先移除场景/prefab 上组件再删除脚本，避免 Missing Script | 核对原10个空壳 prefab | 无 Missing Script |
| P1 | `Assets/Castle/Scenes/CastlePrototype.unity` 与新模板 | 场景只含启动宿主/相机；UI 运行时出现 | 编辑并保存完整层级和引用；模板目录保留 .meta；不再把13页作为 Resources 动态页面读取 | 完成对象树全部绑定 | 编辑模式即可看到并调整 UI |
| P1 | `Assets/Castle/Editor/CastleProjectTools.cs` | Setup 为缺失页面生成10个空壳；Build/Validate 调 Setup | 改为检查手工场景/引用和模板，停用空壳补建路径；保留构建场景配置；把示例内容导入与日常 UI 校验区分，避免每次验证重导示例数据 | Build Settings 保留 CastlePrototype；新增替代场景时同步入口 | 验证/构建不覆盖设计内容，不重新产生空壳 |
| P1 | `Assets/Castle/Scripts/V2/Diagnostics/PrototypeSmoke.cs` | 通过 Button.name 包含中文寻找按钮，拖放查 S1/S3 名称 | 改成显式绑定/稳定测试 ID；场景 Canvas 保持 Castle Game 子级，或同时修改 host.GetComponentInChildren<Canvas>()；保持公开 ShowPlayback/ShowPuzzle/Resume 测试接口 | 按钮物体重命名不应使测试失效 | 完整冒烟流程可继续跑，而非仅通过编译 |
| P1 | `Assets/Castle/Editor/PrototypeValidation.cs`、现有测试 | 现有内容/流程验证为主 | 增加场景引用、唯一 EventSystem、必要组件、模板字段校验；保留业务测试；补关键 UI 流程验证 | 使用真实编辑器场景验证，不只查资源文件存在 | 所有 UI 引用在发布前可发现缺失 |
| P2 | **可选新增** CalibrationTrackDrag | 当前无直接轨道拖动脚本 | 若要求直接拖轨道，实现拖动事件→局部坐标→秒数→ShiftSlider.value；只走一个 Edit 入口，应用现有步长和边界；不要复制校验公式 | 挂 TargetTrack，添加可射线命中 Image；拖对应 Slider 引用 | 轨道拖动与滑条改出同一草稿；缩放分辨率后无误差 |
| P2 | **可选新增** 拖拽影子 | DragToken.OnDrag 为空 | 在独立视觉层显示拖拽副本，不移动受布局控制的源卡；结束/禁用清理；副本关闭射线 | 可选 DragLayer 位于 PagesRoot 上方、ModalRoot 下方 | 跟随鼠标且不遮挡真正落点 |

## 2. 六种谜题需要显示哪些组

| Puzzle.type | 显示组 | 输入/规则应沿用 | 不应该做的改动 |
|---|---|---|---|
| anchor | ChoiceRoot | 多选已标记声响；Draft.Values 保存选择 | 不把未标记声响变成可选答案 |
| location | ChoiceRoot | 单选候选位置，交给 PuzzleService 判定 | 不直接把 DeviceConfig.room 显示成答案 |
| calibrate | CalibrationRoot | 参照设备、两条声响、位移；保留草稿 | 不把初始 Slider 值当作已确认校正 |
| person_path | SlotsRoot + PathRoot | 槽序→NodeId；切楼层保留；同点可重复 | 不套用时间线“一卡只占一个槽”的约束 |
| timeline | SlotsRoot + TimelineRoot | 事件卡按槽排序/交换；槽内静态波形 | 不直接修改 Solved，不因切页丢排序 |
| conclusion | ChoiceRoot | 单选结论；正确后通过 success 事件进结局 | 不恢复旧原型的双结局选择或身份绑定玩法 |

PDF 明确暂不实现声音传播，不需要新增声音传播页面。本次迁移也不把文档尚待正式内容补充的部分当成 UI 已完成的功能。

## 3. 可以保留的业务层

| 文件/配置 | 是否需要因手工 UI 而修改 | 保留职责 |
|---|---|---|
| `SessionService.cs`、`SessionSave.cs`、`SaveService.cs` | 原则上不改 | 世界时间、房间、总状态、自动保存/恢复；UI 引用不能进入存档 |
| `EventService.cs` | 原则上不改 | 调度、时窗、Plan B、对白推进、末句完成和最终事件结算 |
| `RecordingService.cs` | 原则上不改 | 导入/访问权限、游标、标记、时间显示、刻录 |
| `InventoryService.cs` | 原则上不改 | 链接领取、线索提取、道具和磁带实例 |
| `PuzzleService.cs` | 原则上不改 | 草稿、六种判定、PendingPuzzle、成功事件和锁定结果 |
| `ContentDatabase/CsvContent/ContentValidator` | 原则上不改 | 内容加载与校验，ID 关联 |
| `GameSettings/DeviceConfig/CharacterConfig/MapConfig/HotspotBinding` 资产 | 按美术或点位需要改配置，不重写业务 | 公共参数、设备真值、人物图、楼层底图、房间背景、热点模式 |

“原则上不改”仅指切换 UI 创建方式不要求改它们，不代表以后业务需求变化时不能改。

必须保留的流程边界：

- 普通房间移动不额外耗时；离开中控室统一按配置结算（当前15分钟）；UI 翻页不等于离开中控室。
- 对白和录音播放使用各自计时，不用读字速度推进世界时间。模态弹窗暂停底层输入及当前原型的 UI 播放 Tick。
- 末句完成可触发现场录音收录；点击继续才进行事件结算，不能合并成一次按钮操作。
- 推理提交正确先进入 PendingPuzzle 和成功事件；奖励展示完成后才写 Solved。恢复存档优先恢复 Active/待结算事件。
- 已知房间、设备偏差和人物信息来自玩家认知状态；配置里的真值不能直接填进面板。
- 不因隐藏场景 UI 清空 Session 草稿、磁带、已获得信息或中控室访问状态。

## 4. 最小接入代码示意

以下代码仅展示接线方式，**不是可直接替换整个项目的完整补丁**。完整字段需按对象树文档补齐；所有新增类名与实际脚本文件名保持一致。

```csharp
// 新增 SceneUiBindings.cs；其他页面组按同样方式添加。
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Castle.V2
{
    [Serializable]
    public sealed class TitleUiRefs
    {
        public GameObject Root;
        public Button BeginButton;
        public Button ContinueButton;
    }

    public sealed class SceneUiBindings : MonoBehaviour
    {
        public Canvas Canvas;
        public AudioSource Audio;
        public TitleUiRefs Title;
        // 补齐 Common、其他12页、Modal、Templates等引用。
    }
}
```

```csharp
// CastleGame 内新增字段；保留现有 Settings、Content、Session 和存档逻辑。
[SerializeField] private SceneUiBindings _uiBindings;

// Awake 中，校验引用并完成现有 Session 创建后：
_ui = new PrototypeUi(this, Session, _uiBindings);
_ui.ShowTitle();
```

```csharp
// PrototypeUi 内部；继续保持普通 C# 类。
readonly SceneUiBindings _bindings;

public PrototypeUi(CastleGame host, SessionService session, SceneUiBindings bindings)
{
    _session = session;
    _bindings = bindings;
    _audio = bindings.Audio;
    // 使用绑定的 Stage、音量/速度偏好等，删除原 Canvas 等创建代码。
    BindUi();
}

void BindUi()
{
    _bindings.Title.BeginButton.onClick.AddListener(BeginIntroduction);
    _bindings.Title.ContinueButton.onClick.AddListener(Resume);
    // 其他静态监听只在这里注册一次，不在 ShowTitle 中反复注册。
}

public void Dispose()
{
    _bindings.Title.BeginButton.onClick.RemoveListener(BeginIntroduction);
    _bindings.Title.ContinueButton.onClick.RemoveListener(Resume);
    // 清理其他本控制器注册的监听、Action 委托和运行时占位音频资源。
}
```

`Page()` 的改写要点：

1. 保留原 screen/back 赋值以及播放状态、当前正文、波形集合、结局缓存重置。
2. 关闭弹窗；关闭全部绑定页面，再激活目标页；不销毁页面物体。
3. 更新公共标题和返回按钮；Title/Outside/Invitation/Collected 隐藏公共地图、调查册、设置入口（标题页有自己的设置）。
4. `ShowXxx()` 给目标页填数据，重新选定当前正文、滑条、波形引用；刷新解锁状态和状态栏。
5. 静态按钮回调读取“当前 ID”或明确的当前上下文；不要一直捕获首次打开页面时的旧 recordId/puzzleId。重复项回调在每次 Bind 时替换。

**特别注意**：当前各函数大量使用顶层 Stage 坐标。改为嵌套布局后，地图节点、校时 Marker、路径连接线必须统一换算到各自父容器的局部坐标；不能把原来的 x=765 等值原样套到宽度不同的新子面板。

## 5. 实施阶段与检查表

| 阶段 | 要完成的工作 | 最小检查 |
|---|---|---|
| A：固定外壳接管 | 引用脚本、CastleGame 注入、Canvas/页根、Page/Modal 切换 | Play 后无第二套 Canvas；固定页不增殖；漏绑有明确日志 |
| B：主要流程 | 标题、过场、对白、房间、公共导航 | 新档能进入房间；对白链接一次领取；继续按钮时机正确 |
| C：调查系统 | 地图、档案、磁带、播放器、调查册、来源弹窗 | 无权限不能回听；两盘磁带内容独立；覆盖取消不改变内容 |
| D：推理结局 | 六种谜题、拖放、奖励、结局 | 草稿跨页保存；时间线交换和路径重复点正确；奖励只结算一次 |
| E：回归与交付 | 引用校验、修改冒烟定位、真实场景/构建验证、更新交接文档 | 模态输入、返回链、保存恢复、不同分辨率、中文字体均检查 |

建议保留已有业务测试，并针对这次迁移实际新增的风险检查：inactive 页引用可加载、重复开页不重复监听、关闭弹窗后 Tick 恢复、拖放锁定有效、手工对象不被销毁、列表超容量不静默丢失。纯美术尺寸调整不需要为每个坐标写单元测试。

实施后更新 `Assets/Docs/项目交接.md` 和 `Assets/Castle/README.md` 的真实运行方式；本次尚未实施，不应把交接文档写成“已完成手工 UI”。

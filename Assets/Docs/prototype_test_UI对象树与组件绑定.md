> 历史设计草案：场景迁移现已实施。实际使用请参阅 [CastleEditable 场景编辑指南](CastleEditable_场景编辑指南.md) 和 [实际绑定清单](CastleEditable_绑定清单.md)。

# prototype_test：手工 UI 对象树与组件绑定

核对日期：2026-10-09。适用：`prototype_test` 分支 `03b0850` 加当前工作区的简化工作台、调查册格子与详情页改动。入口场景：`Assets/Castle/Scenes/CastlePrototype.unity`。

**这是待实施的搭建方案，本次只生成文档，没有把游戏改成手工 UI。** 对象名、序列化字段和标注“新增”的脚本是拟定接口，当前 Inspector 中尚不存在。配套：[prototype_test_UI迁移修改表.md](prototype_test_UI迁移修改表.md)。

旧文档《手工UI对象树与绑定清单》《手工UI迁移修改对照表》针对另一个 V2 分支，不适用于本版。这里没有 PrototypeUi、SessionService、LinkHandler、DragToken、DropZone；也不使用 V2 的六种 Puzzle 页面。

## 1. 先明确挂载关系

当前程序每次 `Render()` 都会销毁 Stage 下所有子物体，然后重新创建 UI。**只在场景里放 Canvas/Button 并拖脚本，仍然不能工作，必须同时改 Render、Awake 和各绘制方法。**

建议职责关系：

```text
Castle Game [CastleGame] ── ui 字段 ──> Canvas [新增 CastleSceneUi]
         │                                  │
         │                                  └─ 引用所有页面、Button、Text、Slider
         ├─ CastleState：状态                  （通过 Inspector 拖入）
         ├─ CastleRules：规则
         └─ CastleSave：存档
```

| 脚本/类型 | 是否已有 | 挂在哪里 | 需要做什么 |
|---|---|---|---|
| `CastleGame.cs` / CastleGame | 已有 MonoBehaviour | 现有 `Castle Game`，只挂一次 | 保留 database；新增 ui 字段拖入 CastleSceneUi；保留状态、规则调用、存档和 Update |
| `CastleGame.Exploration.cs`、`.Workbench.cs`、`.Resolution.cs`、`.SimpleUi.cs`、`.Smoke.cs` | 已有 partial 类文件 | **不要分别挂载** | 它们与 CastleGame.cs 编译为同一个 CastleGame 类，不是六个管理器 |
| `CastleSceneUi.cs` / CastleSceneUi | **需要新增** MonoBehaviour | Canvas | 保存场景控件引用和页面分组，进行缺失引用校验；不创建第二份游戏状态 |
| `CastleJournalCardView.cs` / CastleJournalCardView | **需要新增** MonoBehaviour | 调查册卡片 prefab 或每个预置卡片 | 保存卡片的 Button、分类编号、标题、摘要 Text 引用；供列表填充/复用 |
| `CastleTimelineDrag.cs` / CastleTimelineDrag | 已有 MonoBehaviour | 锚点页的 `TargetTrack` | 拖动目标轨道；width 对应本地坐标中的有效轨道宽；onDrag 委托由代码绑定 |
| `CastleDatabase` | 已有 ScriptableObject | **不挂组件** | 把现有 Database.asset 拖到 CastleGame.database |
| `CastleState`、`CastleRules`、`CastleSave` | 已有普通/静态 C# 类型 | **不挂组件** | 继续负责状态、判定和存档 |
| `CastleProjectTools`、`CastleTextureImporter` | 已有编辑器代码 | **不挂到游戏对象** | 维护场景检查、构建与资源导入 |

固定页面和普通按钮不需要各自编写脚本。主方案中静态 Button.OnClick、Slider.OnValueChanged 留空，由 CastleGame 新增 `BindUi()` 接线一次。CastleGame 虽然是 MonoBehaviour，但现有多数方法为 private，不能直接选进 Inspector 的事件下拉框。若想全 Inspector 接线，需新增 public 包装方法，见修改表。

## 2. 控件缩写与组件配置

所有 UI 对象都需要 RectTransform；CanvasRenderer 由 Unity 自动添加，下面不重复列出。

| 树中标记 | 创建方式/组件 | Inspector 重点 |
|---|---|---|
| `[G]` | 空 UI 容器，RectTransform | 不需要额外脚本或 Image |
| `[T]` | UI → Legacy → Text，组件 UnityEngine.UI.Text | Font 用 `Assets/Castle/Resources/Castle/Chinese.otf`；关闭 Raycast Target |
| `[B]` | Image + Button，子物体 Label `[T]` | Target Graphic 指向本体 Image；可选 Outline 做边框；OnClick 留空 |
| `[I]` | Image | 背板、线条、遮罩；纯装饰关闭 Raycast Target |
| `[P]` | RawImage | 使用现有 Texture2D/JPG；关闭 Raycast Target。不要把 Texture2D 直接拖进 Image.Source Image |
| `[S]` | Slider + Background(Image) + HandleSlideArea/Handle(Image) | 绑定 Handle Rect 与 Target Graphic；Left To Right；OnValueChanged 留空 |
| `[V]` | ScrollRect + Viewport(Image、RectMask2D) + Viewport/Content + VerticalScrollbar | 显式绑定 viewport/content/verticalScrollbar；Vertical 开、Horizontal 关；Clamped、Auto Hide |

ScrollView 推荐结构如下，后续所有 `[V]` 都展开成这棵树：

```text
ExampleScroll [ScrollRect]
├─ Viewport [Image，Raycast Target 开 + RectMask2D]
│  └─ Content [RectTransform，顶部锚定]
└─ VerticalScrollbar [Image + Scrollbar，Bottom To Top]
   └─ SlidingArea [RectTransform]
      └─ Handle [Image]
```

Scrollbar.Handle Rect 指向 Handle；Handle 的 sizeDelta 为零，由 Scrollbar 控制锚点/尺寸，避免固定高度叠加后超出轨道。正文详情 Text 若由代码按 preferredHeight 设置高度，不要再让 ContentSizeFitter 同时控制同一个 RectTransform。

**本版优先继续使用 Legacy Text。** 若要用 TMP，需要另行把 Text 字段、Label、字体加载、文本测量及 GetComponentInChildren<Text>() 统一迁移，不能只换场景文字组件。

## 3. 场景基础与完整 UI 对象树

保留现有 Camera 和 Castle Game，在下面创建 UI。英文名字是建议，不要求按名字查找；依赖 Inspector 引用。

```text
CastlePrototype
├─ Main Camera                         [沿用 Camera、AudioListener、渲染配置]
├─ EventSystem                         [EventSystem + StandaloneInputModule，只保留一个]
└─ Castle Game                         [CastleGame + AudioSource]
   └─ Canvas                           [Canvas + CanvasScaler + GraphicRaycaster + 新增 CastleSceneUi]
      ├─ Letterbox                     [I，全屏底色]
      └─ Stage                         [G，1600×900，居中]
         ├─ PageRoot                   [G]
         │  ├─ TitlePage               [G]
         │  │  ├─ Background           [P：Title]
         │  │  ├─ MenuBacking          [I]
         │  │  ├─ StartButton          [B：开始游戏]
         │  │  ├─ ContinueButton       [B：继续游戏]
         │  │  ├─ SettingsButton       [B]
         │  │  ├─ CreditsButton        [B]
         │  │  ├─ ExitButton           [B]
         │  │  └─ CaptionText          [T]
         │  ├─ OutsidePage             [G]
         │  │  ├─ Background           [P：按状态使用 Held / Outside]
         │  │  ├─ OpenInvitationButton [B]
         │  │  ├─ EnterCastleButton    [B]
         │  │  └─ CollectedHintText    [T]
         │  ├─ InvitationPage          [G]
         │  │  ├─ Background           [P：Invitation]
         │  │  └─ CollectButton        [B：收起并保存邀请函]
         │  ├─ CollectedPage           [G]
         │  │  ├─ Background           [P：Collected]
         │  │  └─ ConfirmButton        [B：确认收纳]
         │  ├─ LoungePage              [G]
         │  │  ├─ Background           [P：Lounge]
         │  │  └─ DialoguePanel        [I]
         │  │     ├─ SpeakerText       [T]
         │  │     ├─ Line0Text         [T]
         │  │     ├─ Line1Text         [T]
         │  │     ├─ Line2Text         [T；最多显示最近三句]
         │  │     ├─ ProgressText      [T]
         │  │     ├─ ContinueButton    [B：对白结束后改为打开地图]
         │  │     └─ MapButton         [B]
         │  ├─ RoomPage                [G]
         │  │  ├─ Background           [P：ControlRoom / EmptyRoom]
         │  │  ├─ ControlRoot          [G：仅中控室]
         │  │  │  ├─ WorkbenchButton   [B]
         │  │  │  └─ CaptionText       [T]
         │  │  └─ OtherRoomRoot        [I：其他房间]
         │  │     ├─ RoomNameText      [T]
         │  │     ├─ DescriptionText   [T]
         │  │     └─ MapButton         [B]
         │  ├─ WorkbenchShell          [G：Browse / Align / Sound 共用]
         │  │  ├─ Background           [P：ControlRoom]
         │  │  ├─ WoodTint             [I]
         │  │  ├─ BackButton           [B：回中控室]
         │  │  ├─ JournalButton        [B]
         │  │  ├─ SettingsButton       [B]
         │  │  ├─ BoardShadow          [I]
         │  │  └─ Board                [I：绿板]
         │  │     ├─ Tabs              [G]
         │  │     │  ├─ BrowseButton   [B：录音]
         │  │     │  ├─ AlignButton    [B：锚点]
         │  │     │  └─ SoundButton    [B：房间]
         │  │     ├─ BorderAndDividers [G，子 Image 线框，关闭射线]
         │  │     ├─ BrowsePage        [G；内部详见第4节]
         │  │     ├─ AlignPage         [G；内部详见第4节]
         │  │     └─ SoundPage         [G；内部详见第4节]
         │  ├─ AnalysisShell           [G：Event / Identity / Route / Final / Truth 共用]
         │  │  ├─ Header               [I]
         │  │  │  ├─ BackButton        [B：回中控室]
         │  │  │  ├─ BrowseButton      [B]
         │  │  │  ├─ AlignButton       [B]
         │  │  │  ├─ SoundButton       [B]
         │  │  │  ├─ JournalButton     [B]
         │  │  │  ├─ SettingsButton    [B]
         │  │  │  ├─ BrandText         [T：声音档案工作台]
         │  │  │  ├─ SectionText       [T]
         │  │  │  ├─ TitleText         [T]
         │  │  │  └─ TimeHintText      [T]
         │  │  ├─ Footer              [I + 子 HintText(T)]
         │  │  ├─ EventPage           [G：已确认声音事件]
         │  │  │  ├─ EventTitleText   [T]
         │  │  │  ├─ EvidenceText     [T]
         │  │  │  ├─ RelationText     [T]
         │  │  │  ├─ IdentityButton   [B]
         │  │  │  └─ BrowseButton     [B]
         │  │  ├─ IdentityPage        [G]
         │  │  │  ├─ QuoteText        [T]
         │  │  │  ├─ LinButton        [B]
         │  │  │  ├─ ChenButton       [B]
         │  │  │  ├─ EvidenceButton   [B]
         │  │  │  ├─ ResultText       [T]
         │  │  │  └─ SubmitButton     [B：确认身份 / 重建人物路线]
         │  │  ├─ RoutePage           [G]
         │  │  │  ├─ Node0Button      [B]
         │  │  │  ├─ Node1Button      [B]
         │  │  │  ├─ Node2Button      [B]
         │  │  │  ├─ UndoButton       [B]
         │  │  │  ├─ RouteMap         [G：第6节地图结构]
         │  │  │  ├─ SelectedNodeText [T]
         │  │  │  ├─ EvidenceButton   [B]
         │  │  │  ├─ ResultText       [T]
         │  │  │  ├─ VerifyButton     [B]
         │  │  │  └─ FinalButton      [B]
         │  │  ├─ FinalPage           [G]
         │  │  │  ├─ TimelineCards    [G：三张固定 Image + Text 卡]
         │  │  │  ├─ EvidenceSummary  [T]
         │  │  │  ├─ RelationText     [T]
         │  │  │  ├─ ClaspButton      [B：选择支持证据]
         │  │  │  └─ SubmitButton     [B]
         │  │  └─ TruthPage           [G]
         │  │     ├─ WaveRoot         [G，78 个 Image 波形条]
         │  │     ├─ SourcesText      [T]
         │  │     ├─ ProgressText     [T]
         │  │     ├─ NarrativeText    [T]
         │  │     └─ ContinueButton   [B：继续重建 / 作出决定]
         │  ├─ ChoicePage              [G]
         │  │  ├─ Background           [P：ControlRoom]
         │  │  ├─ TitleText / BodyText [T]
         │  │  ├─ PublishButton        [B：公开完整记录]
         │  │  ├─ ConfrontButton       [B：先与当事人对质]
         │  │  ├─ JournalButton        [B]
         │  │  └─ FinalButton          [B：返回事件还原]
         │  └─ EndingPage              [G]
         │     ├─ Background           [P：ControlRoom]
         │     ├─ TitleText / BodyText / HintText [T]
         │     ├─ JournalButton        [B]
         │     ├─ BackToChoiceButton   [B：返回结局前]
         │     └─ TitleButton          [B]
         ├─ SceneHud                   [G，只在 Outside / Lounge / Room 显示]
         │  ├─ LocationText / ClockText [T]
         │  ├─ MapButton / JournalButton / SettingsButton [B]
         ├─ OverlayRoot                [G，默认关闭]
         │  ├─ Shield                  [I，全屏半透明，Raycast Target 开]
         │  ├─ NavigationPanel         [G：第6节导航地图]
         │  ├─ SettingsPanel           [I]
         │  │  ├─ TitleText / SizeValueText / HelpText [T]
         │  │  ├─ ReadingSizeSlider    [S：20～28，整数]
         │  │  ├─ VolumeSlider         [S：0～100，整数，回调除100]
         │  │  ├─ MotionButton         [B：切换 reduceMotion]
         │  │  ├─ TitleButton          [B]
         │  │  └─ ContinueButton       [B]
         │  ├─ JournalGridPanel        [G：第5节]
         │  ├─ JournalDetailPanel      [G：第5节]
         │  ├─ ArchivedInvitationPanel [G]
         │  │  ├─ InvitationImage      [P]
         │  │  └─ BackToDetailButton   [B]
         │  └─ MessagePanel            [I：确认框/提示/制作名单共用]
         │     ├─ TitleText / BodyText [T]
         │     ├─ ConfirmButton        [B：确认 / 继续调查]
         │     └─ CancelButton         [B：有确认动作时显示]
         └─ ToastRoot                  [I，默认关闭，所有图形关闭射线]
            └─ MessageText             [T]
```

Canvas：Screen Space Overlay；CanvasScaler：Scale With Screen Size，1600×900，Expand；Stage 居中固定参考尺寸。AudioSource 关闭 Play On Awake、Spatial Blend=0。默认只显示 TitlePage；其他页、两个 Shell、SceneHud、OverlayRoot、ToastRoot 关闭。Shell 打开时只打开自己的一个内容页。

注意：PageRoot 下分组后，原代码中的屏幕绝对坐标不能直接复制成子节点局部坐标。请用锚点布局，程序刷新时不要覆盖你摆好的位置。

## 4. 三个工作台页的详细树与接线

```text
BrowsePage [G]
├─ FileList [G]
│  ├─ HeadingText [T]
│  ├─ Rec01Button / Rec02Button / Rec03Button / PersonalButton [B]
├─ FileInfoText [T]
├─ TranscriptRoot [G，仅设备录音]
│  └─ TranscriptScroll [V]
│     └─ Viewport/Content
│        ├─ Row0 [G] → SelectButton(B)、ExcerptButton(B)、Rule(I)
│        ├─ Row1 [G] → SelectButton(B)、AnchorButton(B)、Rule(I)
│        ├─ Row2 [G] → SelectButton(B)、ExcerptButton(B)、Rule(I)
│        └─ Row3 [G] → SelectButton(B)、ExcerptButton(B)、Rule(I)
├─ ConversationRoot [G，仅随身记录]
│  └─ ConversationScroll [V] → Viewport/Content/BodyText(T)
├─ PlaybackRoot [G，仅设备录音]
│  ├─ PlayPauseButton [B]
│  ├─ ProgressSlider [S]
│  ├─ PlaybackText [T]
│  └─ MarkCompleteButton [B]
├─ HintText [T]
└─ IdentityButton [B，仅确认声音事件后显示]

AlignPage [G]
├─ CandidateList [G]
│  ├─ HeadingText [T]
│  ├─ AnchorAButton / AnchorBButton / BackToBrowseButton [B]
├─ AlignmentScroll [V]
│  └─ Viewport/Content
│     ├─ ReferenceTrack [I] → TitleText(T)、Rail(I)、ReferenceMarker(I)
│     ├─ TargetTrack [I + 已有 CastleTimelineDrag]
│     │  ├─ TitleText [T]
│     │  ├─ Rail [I]
│     │  └─ TargetMarker [I]
│     ├─ OffsetSlider [S，-180～360，整数秒]
│     └─ ComparisonPanel [I] → TitleText(T)、BodyText(T)
├─ OffsetText / StatusText [T]
└─ CheckButton / SoundButton [B]

SoundPage [G]
├─ MarkerList [G]
│  ├─ HeadingText [T]
│  ├─ SourceButton / Rec01MarkerButton / Rec02MarkerButton [B]
│  ├─ HintText [T]
│  └─ UndoButton [B]
├─ SoundMap [G：第6节地图结构]
├─ InspectorPanel [G]
│  ├─ HeadingText / SourceInfoText / DoorTitleText [T]
│  ├─ UnknownButton / OpenButton / ClosedButton [B]
│  ├─ DoorEvidenceButton [B]
│  └─ HintText [T]
├─ DraftStatusText [T]
└─ SaveDraftButton / VerifyButton [B]
```

| 控件 | CastleGame 需要绑定的行为 | 必须保留的条件 |
|---|---|---|
| Rec01 / Rec02 / Personal | ChooseFile(0 / 1 / 2) | Personal 仅 state.recorded 时显示；Rec03 目前只提示待定位，不是第三个可播放文件 |
| Row0～3.SelectButton | 设置 selectedLine、playing=false、playPosition=291/300/312/324，然后刷新 | 内容随当前 file 和身份确认状态变化；按钮文字不是程序 ID |
| Row1.AnchorButton | ToggleAnchor() | 既可标记也可移除；同时更新 pairA/pairB，使 aligned/pathSaved 失效 |
| Row0/2/3.ExcerptButton | 保留原来的摘录去重、state.excerpts、Save 和 Toast | 回调读取当前 file；不要一直捕获第一次打开的录音 |
| PlayPause / ProgressSlider | 切换 playing；游标改变后刷新选中行和 PlaybackText | REC01 0～480，REC02 0～720；程序回写用 SetValueWithoutNotify |
| MarkComplete / Identity | Go(Align) / Go(Identity) | Identity 按钮仅 confirmedEvent!=null 时显示 |
| AnchorA / AnchorB | 切换 pairA/pairB，清除 aligned/pathSaved，Save 后刷新 | 对应 anchorA/anchorB 为真才显示 |
| OffsetSlider | SetOffsetLive((int)value) | 与方向键和轨道拖动共用同一修改入口 |
| TargetTrack.CastleTimelineDrag | onDrag = delta => SetOffsetLive(RoundToInt(state.offset + delta)) | onDrag 是 Action，不是 UnityEvent，不能在 Inspector 接 OnDrag；只给目标轨挂脚本 |
| Check / Sound | Check(CastleRules.Align(state), 刷新和提示) / Go(Sound) | 不绕过两个锚点、候选组及时间差检查 |
| 三个空间标记按钮 | 设置 spatialMarker=0/1/2 后刷新 | 只有选中声源 S 才能在地图放置假设；设备位置是原始档案 |
| 三个门状态按钮 | Remember；设置 DoorState；pathSaved=false；Save、刷新 | “未知”也需保留，不默认替玩家选择答案 |
| DoorEvidence / Undo | 切换证据并使草稿失效 / UndoDraft() | 不修改 confirmedEvent 快照 |
| SaveDraft / Verify | 检查声源后保存草稿 / Check(CastleRules.VerifyEvent(state), Go(Event)) | 保存与验证是两个动作；不能省略证据关联 |

当前轨道有效宽为 936，范围共 540 秒。手工改宽度后，应读取 Rail 的实际本地宽度设置 drag.width，同时按同一宽度刷新 Marker；不能一处改宽、一处仍写死 936。

## 5. 调查册格子、二级详情及卡片 prefab

```text
JournalGridPanel [G]
├─ BookShadow / BookBackground [I]
├─ TitleText / CountText / FooterText [T]
├─ CloseButton [B]
├─ PeopleTab / ItemsTab / EventsTab [B，仅这三类]
├─ GridScroll [V]
│  └─ Viewport/Content [GridLayoutGroup + ContentSizeFitter]
│     └─ JournalCard… [手工 prefab 的实例，或预置卡片池]
└─ EmptyText [T]

JournalCard prefab [Image + Button + 新增 CastleJournalCardView]
├─ CategoryNumberText [T]
├─ TitleText [T]
└─ SummaryText [T]

JournalDetailPanel [G，人物/道具/事件复用同一详情页]
├─ Background [I]
├─ BackButton / CloseButton [B]
├─ CategoryText / TitleText [T]
├─ DetailScroll [V]
│  └─ Viewport/Content/BodyText [T]
├─ ActionButton [B：有后续操作才显示]
└─ FooterText [T]
```

GridLayoutGroup：Fixed Column Count=3，Cell Size 建议 410×185，Spacing=30×25，Start Corner=Upper Left。ContentSizeFitter：Horizontal=Unconstrained，Vertical=Preferred Size；Content 顶部锚定。使用布局组件后，移除原代码逐张卡片设置 x/y 及手算 Content 高度的逻辑。

CastleJournalCardView 的拟定字段：`Button OpenButton`、`Text CategoryNumberText`、`Text TitleText`、`Text SummaryText`。全部拖本卡片的对应组件。卡片点击由控制器绑定 JournalDetail(entry)，不在 Inspector 填中文标题来识别条目。现有 `JournalEntry` 是 CastleGame 的内部类；卡片视图只接显示字符串、稳定 ID 和点击回调，不要求把该内部类暴露给 Inspector。

| 类别 | 当前内容来源 | 详情页动作 |
|---|---|---|
| 人物 | 拉帕莉亚；交谈后女爵；工作台开放后林女士、陈先生 | 女爵详情包含完整对话；林女士按钮按状态进入 Identity/Route；其余不显示动作按钮 |
| 道具 | 邀请函、地图、金属搭扣、位置资料，按状态出现 | 邀请函可打开 ArchivedInvitationPanel，再返回同一详情 |
| 事件 | 中控室线索、声音事件、人物路线、最终还原、录音摘录 | 已确认条目可跳 Event/Route/Final；摘录只展示原文与来源 |

详情页返回保留 journalTab 和 journalScroll；Esc：邀请函→道具详情→当前分类格子→关闭调查册。关闭调查册回到原游戏页面。不能把第四个“对话”分类加回来，也不能删除已存在的对话/摘录内容。

## 6. 三套地图如何创建和绑定

导航地图、声音空间地图、人物路线地图需要**三套物体实例**。可做同一个手工地图 prefab 后分别实例化进场景，但三处引用不能指向同一个 GameObject。

```text
MapView [G]
├─ TitleText / HintText [T]
├─ Floor1Button / Floor2Button [B]
├─ MapArea [G]
│  ├─ Background [P：Floor1 / Floor2]
│  ├─ Floor1Rooms [G] → Room101Button、Room102Button… [B]
│  ├─ Floor2Rooms [G] → Room201Button、Room203Button… [B]
│  └─ MarkersAndConnections [G，纯显示，关闭射线]
└─ 该地图特有控件
```

| 地图 | 额外控件 | 点击房间的用途 |
|---|---|---|
| NavigationPanel | LocationText、SelectionText、EnterButton、WaitButton、CloseButton | 先设置 selectedRoom，再显示权限/时段；确认后 CastleRules.Travel；楼梯切层 |
| SoundMap | SourceMarker Text、两条传播连线容器 | 设置 state.source（通过 ID 查询 r.title）；显示传播条件，不执行 Travel |
| RouteMap | 三个编号 Text、两段路线连线容器 | 为 selectedNode 设置 state.route[node]（通过 ID 查询 r.title），不执行 Travel |

CastleSceneUi 每套地图保存自己的 `RoomButtonRefs[]`（新增 `[Serializable]` 数据类，不是组件），每项填写 `RoomId : string`、`Button`、`Text Label`、`RectTransform MarkerAnchor`。MarkerAnchor 可放在手工房间按钮中心。代码用 RoomId 查数据库；为了兼容第一版规则，source/route 最后仍保存现有规则需要的房间 title，本次不要顺带迁移存档字段。

实际 Database.asset ID：一楼 `101,102,103,104,105,106,107,108,109,110,stairs1,east`；二楼 `201,203,204,205,206,207,208,209,210,stairs2`。导航全部创建；两张分析地图排除 stairs1/stairs2/east。现有项目只有两层，不需要创建第三层页签。

手工摆地图位置后停止按数据库 x/y/width/height 强制重排按钮。连线需同步改为使用本地图下的房间 MarkerAnchor 和手工走廊路点，避免按钮已移动、线仍按原 1672×941 坐标绘制。图形可复用手工制作的线段 prefab；完全禁止 Instantiate 时需预置足够线段并规定容量。

## 7. CastleSceneUi 应提供哪些 Inspector 字段

推荐按 `[Serializable]` 字段组组织。每个页面/面板组都有 `GameObject Root`；下表字段是待开发的引用契约，不是当前已存在的 API。

| 引用组 | 拖入的组件/物体 |
|---|---|
| Common | Canvas、Stage、PageRoot、AudioSource、SceneHud、WorkbenchShell、AnalysisShell、OverlayRoot、Shield、ToastRoot/MessageText |
| Title / Outside / Invitation / Collected / Lounge / Room | 第3节对应所有背景、文字、Button；Lounge.LineTexts[3]；Room.ControlRoot/OtherRoomRoot |
| SceneHud | LocationText、ClockText、Map/Journal/Settings Button |
| Workbench | 背板/背景、Back/Journal/Settings、Browse/Align/Sound Tab 按钮及其 Label |
| Browse | Root、四个文件按钮、FileInfoText、TranscriptRoot、TranscriptScroll/Content、四个 RowRefs、ConversationRoot/Scroll/Text、PlaybackRoot、PlayPause/MarkComplete/Identity Button、ProgressSlider、PlaybackText、HintText |
| Align | Root、候选两个按钮、BackToBrowse、AlignmentScroll、两轨道及 Rail/Marker/Text、TargetTrack 上的 CastleTimelineDrag、OffsetSlider、比较文字、OffsetText/StatusText、Check/Sound Button |
| Sound | Root、三个标记按钮、Undo、SoundMapRefs、信息文字、三个门状态按钮、证据按钮、状态文字、SaveDraft/Verify |
| AnalysisHeader | Header/Footer、品牌/分区/标题/时间提示/HintText、导航按钮及 Label |
| Event / Identity / Route / Final / Truth / Choice / Ending | 第3节对应所有文字、按钮与根；RouteMapRefs、三个路线节点按钮；Truth.WaveBars[78] |
| Navigation | Root、NavigationMapRefs、当前位置与所选房间文字、Enter/Wait/Close |
| Settings | Root、ReadingSizeSlider/SizeValueText、VolumeSlider、Motion/Title/Continue、帮助文字 |
| JournalGrid | Root、三个分类按钮、GridScroll/Content、EmptyText/CountText、Close、CastleJournalCardView prefab（或预置 Cards[]） |
| JournalDetail | Root、CategoryText/TitleText、DetailScroll/Content/BodyText、Back/Close/ActionButton 及文字 |
| ArchivedInvitation | Root、RawImage、BackToDetailButton |
| Message | Root、TitleText/BodyText、Confirm/Cancel Button 及文字；当前确认动作只存在运行时 |

最后在 CastleGame 的新增 `ui` 字段中拖入 Canvas 上的 CastleSceneUi。不要用 Find 按物体名字找所有控件，也不要同时保留 Inspector 监听和代码监听造成双重提交。

## 8. 是否允许列表运行时实例化，以及搭建顺序

主方案：固定页面、录音四行、地图按钮、两轨道、78 条波形都手工建好；调查册卡片和连线用你做好的 prefab 复用/实例化。**代码不再创建 Canvas、Text、Button 组件，只创建作者已制作的重复项实例。**

若要求运行时一个物体都不创建：把调查册卡片与连线预先放进场景并绑定数组。内容超出卡片池时需分页/复用，不能静默丢条目；只填数据和 SetActive。此时 CastleJournalCardView 仍挂在每张预置卡上。

建议顺序：先实现引用脚本和移除根节点销毁 → 基础 Canvas/标题/开场 → 场景与覆盖层 → 工作台三页 → 调查册格子/详情 → 后续推理/结局 → 回归。完成后检查一个主 Canvas、一个 EventSystem，重复切页不增殖组件，重复打开按钮只执行一次，保存/继续仍恢复第一版进度。


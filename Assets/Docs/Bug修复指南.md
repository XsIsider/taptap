# Bug 修复指南

当前 UI 版迁移范围：BUG-001、002、004～008；BUG-003 暂不实施，保留原推进方式。UI 版已删除旧 `DialoguePanel/TextBackdrop`，后续处理 BUG-003 时必须重新确认点击层，不能直接套用旧场景步骤。

本文中的 Bug 和调整方案基于记录时的项目版本。修复前，请先在你们当前版本中逐项确认所描述的问题或修改点是否仍然存在；若已修复或已有等效处理，则无需重复修改。仍存在的问题按当前代码结构定向修复，保留后续开发内容。

## BUG-001 获得窗口左侧出现“人物灵感”

**问题：** 获得人物内容时，会打开与道具共用的奖励弹窗。窗口左侧显示文件夹图，右侧显示奖励名称和正文，但文件夹旁还叠着“人物灵感”四个字，位置偏左，影响界面呈现。需要隐藏的是左侧这四个字，不是右侧“佩克里斯灵感1”等奖励名称，也不是整个弹窗。

**出现范围：** 人物初始资料、灵感和人物路径奖励都使用 `Type == "info"`，打开该弹窗时均受影响。例如开场管家说完“米娅，带拉帕莉亚小姐去一楼东边靠里的那间客房，不要怠慢了。”后继续推进，首次获得“佩克里斯灵感1”（条目 `320310`）即可观察。已经领取的奖励通常不会重复弹窗。

**位置：** `Assets/Castle/Scripts/V24/CastleV24Controller.cs` → `Reward()` → `Acquire/Icon`（Text）。

**原因：** 当前按“无道具图片”启用类型文字，人物类奖励没有道具图片，因此也被启用。

**修改：** 找到 `FitItemArt(...)` 后、确认按钮绑定前的代码：

```csharp
v.Get<Text>("Icon").enabled = art.texture == null;
```

替换为：

```csharp
bool showTypeLabel = entry.Type != "info" && art.texture == null;
v.Get<Text>("Icon").enabled = showTypeLabel;
```

每次打开弹窗都执行此判断。保留文件夹图、名称、正文及其他类型奖励的显示；无需修改场景或配置表。

## BUG-002 对白姓名与正文改为同行显示（排版调整）

**问题：** 交谈界面右侧的每条对白先单独显示姓名，正文从下一行开始。例如“拉帕莉亚”占一行，下方才是“这就是芳里耶古堡？……”。需要改为“拉帕莉亚：这就是芳里耶古堡？……”，姓名后加中文冒号，正文紧接其后，超过宽度再自然换行。旁白、未表明身份的“？？”及玩家选择后的回复记录使用相同排版；未知人物继续显示原占位名，不提前公开身份。

**位置：** `Assets/Castle/Scripts/V24/CastleV24Controller.Explore.cs` → `Line()`、`Explore()` 中 `S.Transcript` 的回复分支，以及新增的 `SetInlineDialogue()`。行模板为 `Assets/Castle/Prefabs/Flow/DialogueLine.prefab`，其中 `Speaker` 是 Text，`Body` 是 TMP。

**原因：** 模板中的姓名和正文是上下分开的两个控件，`Body` 顶部位置为 -42。仅在正文前拼接姓名会保留原姓名行和空白；玩家回复还绕过了 `Line()`，必须一并处理。

**修改：**

1. 新增 `SetInlineDialogue(view, speakerName, body)`，仅对现场对白实例禁用 `Speaker` 的 Text 显示，将 `Body` 上移到 `Speaker` 的顶部位置。使用 TMP 富文本将原姓名颜色和字号包在“姓名：”外，随后直接连接正文，不插入换行。
2. `Line()` 先完成原有正文关键词链接替换，再在 `recording == false` 时调用该方法。姓名继续使用 `SpeakerName()`；保留链接 ID、解锁条件和点击回调，避免把姓名前缀也替换成链接。
3. `Explore()` 遍历 `S.Transcript` 时，`Plot == 0` 的玩家回复也调用该方法，姓名仍取 `Engine.CharacterName(200001)`，正文取 `item.Reply`。
4. 按父列表宽度扣除布局组左右留白、正文横向偏移，计算合并文本的首选高度，同时设置 `Body` 高度和根 `LayoutElement.preferredHeight`，去掉原来为独立姓名行预留的高度，避免长句重叠或裁切。
5. 同一条对白换行后上下行较挤，在 `SetInlineDialogue()` 内、计算首选高度前设置 `text.lineSpacing = 8f`（TMP Line Spacing 原为 0），稍微增大内部行距。沿用整句高度计算，不增加不同对白之间的列表间距。

录音／磁带共用模板时保留原时间与姓名排版，`recording == true` 不走此方法；无需修改共用 Prefab、场景绑定、配置表或存档。

## BUG-003 对话框普通文字和空白处也可点击推进

**问题：** 现场交谈时，点击正文中的普通文字或对话框大部分空白没有反应，只能点击底部窄条 `Continue` 或开启自动播放。需要让对话框内的普通文字和空白也能继续下一句，同时保留关键词链接、选项、自动按钮与滚动条各自的操作。

**位置：** `Assets/Castle/Scripts/V24/CastleV24Controller.Explore.cs` 的 `Explore()`、`SetInlineDialogue()`、新增 `AdvanceDialogueFromPanel()`；`Assets/Castle/Scripts/V24/V24LineHit.cs`；`Assets/Castle/Scenes/CastleEditable.unity` 中 `Explore/DialoguePanel` 及其 `TextBackdrop`。

**原因：** `Continue` 是底部透明 Button；正文由 TMP 上的 `FlowTextLink` 检测 `<link>` 字符范围，普通文字点击没有推进回调，背景也未接收射线。直接把底部 Button 放大并盖在正文之上，会拦截链接及滚动输入。

**修改：**

1. `V24LineHit.OnPointerClick()` 忽略非左键和拖动；有 TMP 时先调用 `TMP_TextUtilities.FindIntersectingLink()`，命中链接立即返回，由同对象的 `FlowTextLink` 单独处理。没有 TMP 时允许触发 `Clicked`，便于背景复用。不要关闭正文的 Raycast Target。
2. 在场景的 `DialoguePanel` 上挂已有 `V24LineHit`，开启其底层子物体 `TextBackdrop/Image` 的 Raycast Target；保持背景在正文与按钮下方，不移动或放大 `Continue`，不遮挡 ScrollRect。空白处点击由父级处理；普通正文点击由正文自身处理，二者不会自动重复冒泡。
3. `Explore()` 将面板 `V24LineHit.Clicked` 绑定到 `AdvanceDialogueFromPanel()`；`SetInlineDialogue()` 将正文 `V24LineHit.Clicked` 也绑定到该方法，覆盖普通对白和玩家选项回复。
4. `AdvanceDialogueFromPanel()` 仅在 Explore、无弹窗、有活动对白且无待开奖励时执行；还必须确认原 `Continue` 按钮有效、可交互，且没有 `Engine.RequiredAction()`。通过后调用 `Continue.onClick.Invoke()`，复用现有推进逻辑。选项期间、已结束对白和必点动作不能靠背景点击跳过。

正文拖动仍交给原 ScrollRect；选项、自动按钮、滚动条保持独立接收事件。录音／磁带的正文标记回调保留，不绑定现场推进。

## BUG-004 磁带播放页隐藏左上角时间和地点

**问题：** 进入磁带播放页 `Tape` 后，设备画面左上角仍叠着“当前位置”和“当前时间”及其背景、图标。该页不需要显示这组 UI；返回探索页时应恢复。

**位置：** `Assets/Castle/Scripts/V24/CastleV24Controller.cs` → `Hud()`；控件 `LocationGroup` 对应 `UIFlow/Canvas/Stage/Hud/bg_location`。

**修改：** 在 `Show(Ui.Hud,"LocationGroup",...)` 原有页面排除条件末尾追加 `&& _page != "Tape"`。由页面切换时调用的 `Hud()` 统一控制整组显隐，不只隐藏两个文字，也不要隐藏整个 Hud。离开 Tape 返回 Explore 时按原条件自动恢复，保留返回、地图、调查册和设置按钮；无需修改场景或绑定。

## BUG-005 磁带播放新增对白时跟随到底部

**问题：** `Tape` 播放页按时间在列表下方追加录音对白，但滚动窗口停在旧位置；文字超出可视范围后，新对白在下面看不到，需要玩家反复手动下滚。需要默认跟随最新对白；玩家上翻查历史时暂停跟随，回到底部后恢复，避免新句强行把阅读位置拉走。

**位置：** `Assets/Castle/Scripts/V24/CastleV24Controller.Workbench.cs` → `PlaybackRender()`、`PlaybackTick()`、新增 `RestoreTapeScroll()`；`CastleV24Controller.cs` → `Page()`。复用 `Tape/LinesScroll` 的 ScrollRect 和 `Lines` 内容绑定。

**修改：**

1. `PlaybackRender(bool revealLatest = false)` 清空旧行前，记录内容的像素位置，并按旧内容判断是否距底部不超过 16 个 UI 单位。首次进入、切换录音或 `revealLatest` 为真时跟随；否则仅原来贴近底部才跟随。
2. 重建后启动 `RestoreTapeScroll()`，等待一帧并刷新 Canvas 布局，再停止滚动惯性、将 `verticalNormalizedPosition` 设为 0；查看历史时恢复原像素位置，不能用旧归一化比例恢复，以免内容变长后跳位。重新重建前取消旧协程，执行前检查仍在 Tape 且录音未切换。
3. 拖动播放进度条、从结束位置重新播放时调用 `PlaybackRender(true)`，显示所选播放位置的最新对白；离开 Tape 的 `Page()` 将 `_tapeScrollRecord` 归零，使再次进入默认跟随。
4. `PlaybackTick()` 对比已显示行数时仅统计激活的子对象；`Clear()` 停用后等待帧末销毁的旧行不计入，避免同帧反复重建覆盖跟随请求。

仅作用于 Tape 的逐句追加列表；中控室 Recordings 全文列表、录音播放／听完判定、标记和链接功能保持原逻辑。无需修改场景、Prefab 或存档。

## BUG-006 锚点拖到轨道时被界面遮挡

**问题：** 在中控的“锚点对齐”页，将左侧已标记锚点拖向右侧轨道时，随鼠标移动的图标和文字被轨道背景覆盖，离开左侧列表后还可能被裁切，看起来像拖到一半消失。需要在拖动全过程中显示于轨道上方。后续“文本对照”页复用同一组候选卡片，也在本次处理范围内。

**位置：** `Assets/Castle/Scripts/Flow/FlowDrag.cs` 的 `OnBeginDrag()`、`OnDrag()`、`OnEndDrag()`、`OnDisable()`；`Assets/Castle/Scripts/V24/CastleV24Controller.Workbench.cs` 的 `Anchors()` 候选卡片绑定。模板为 `Assets/Castle/Prefabs/V24/AnchorCard.prefab`。

**原因：** 原拖动只改变卡片在左侧列表内的位置；卡片始终留在 `ListScroll/Viewport/List` 下，受列表遮罩与整个页面的兄弟绘制顺序限制。仅对卡片执行 `SetAsLastSibling()` 仍只会排到左侧列表内部最后，无法越过右侧轨道。

**修改：**

1. 在 `FlowDrag` 增加可选属性 `RectTransform DragRoot`；仅在 `Anchors()` 创建 `Kind == "evidence"` 的候选卡片时设置 `drag.DragRoot = (RectTransform)v.transform`，目标是当前 Anchors 页面。未设置的其他拖拽沿用原显示方式。
2. 开始拖动时，记录原卡片的位置及 CanvasGroup 的透明度、射线状态；将卡片复制为页面的直接子节点，禁用副本的 `FlowDrag`，保持原世界位置、缩放和实际宽高，并 `SetAsLastSibling()`。副本不再位于列表遮罩内，原卡片只隐藏显示、继续占据列表位置。
3. `OnDrag()` 移动副本；副本透明度沿用原拖动效果（0.65 倍），原卡片与副本的 `blocksRaycasts` 都为 false，让下方轨道继续接收放置事件。`eventData.pointerDrag` 仍是原卡片，保留原 `Kind`、`Id` 和 `OnDrop()` 判断。
4. 统一由 `EndDrag()` 停用并销毁副本、恢复原卡片状态；`OnEndDrag()` 和 `OnDisable()` 都调用它。放入后列表立即重建或中途关闭页面时也要清理，避免遗留图标。

无需修改场景或 Prefab 绑定；不改变加入／替换轨道、参考轨、校时或谜题验证规则。

## BUG-007 空间推演拖动标签时被地图遮挡

**问题：** 在“空间推演”页，将左侧 A／B／C 标签拖向地图时，图标和文字被地图背景盖住，离开左侧列表后可能被裁切；地图上已放置的标签再次拖动时，也可能被其他房间热点遮住。需要拖动中的标签始终显示在地图上方。

**位置：** `Assets/Castle/Scripts/V24/CastleV24Controller.Workbench.cs` → `PathRender()` 中左侧候选卡片及地图已放置标签（`badge`）的 `FlowDrag` 初始化处。

**原因：** 两处 `Kind == "path"` 的拖拽未设置 BUG-006 新增的 `DragRoot`，仍在原列表或房间节点内移动，受遮罩及兄弟节点绘制顺序影响。

**修改：** 先确认 `FlowDrag` 已有 BUG-006 的预览与清理逻辑。在上述两处初始化后各添加 `if (v.Id == "Space") drag.DragRoot = (RectTransform)v.transform;`，将预览放在 Space 页面最上层，脱离列表和缩放地图节点。保留原卡片、标签 ID、`Dropped` 回调及 `PathDragging` 计数，预览不阻挡射线；松手、放置重建或退出页面时沿用统一清理。

仅调整空间推演的拖拽显示；不改变房间放置、门状态、地图缩放／平移或验证规则，也不修改人物路径页、场景及 Prefab 绑定。

## BUG-008 空间推演标记字母未在菱形中心

**问题：** 在“空间推演”地图上放置 A／B／C 标签后，字母偏离白色菱形中央；切换楼层或重新放置后仍会出现，影响字母辨认。需要让三个字母分别居中显示在各自的菱形内。

**位置：** `Assets/Castle/Scripts/V24/CastleV24Controller.Workbench.cs` → `PathRender()` → 创建地图已放置标签 `badge` 后，对 `Title` 的排版设置。

**原因：** 标记根节点 `badgeRect.pivot` 为 `(0,1)`，菱形图片为 31×46，而标记矩形为 40×38。`Image.preserveAspect` 缩窄图片时按根节点 pivot 分配留白，图片因此贴左绘制，中心比矩形中心偏左约 7.2 个 UI 单位；仅把文字框铺满并居中，字母仍会偏在菱形右边。原标题还带 `(4,-2)` 偏移和固定高度，需一并清除。

**修改：** 仅对 `v.Id == "Space"` 执行以下两步，不能只改文字对齐：

1. 将标记根节点 pivot 改为 `(0.5,0.5)`，让等比图片在矩形内居中。改 pivot 前先执行 `badgeRect.anchoredPosition += Vector2.Scale(new Vector2(.5f,.5f) - badgeRect.pivot, badgeRect.rect.size)`，补偿轴心变化，保留原标记矩形及点击区的位置。标记实例局部缩放为 1，地图缩放由父级承担。
2. 将 `Title` 的 `anchorMin` 设为 `(0,0)`、`anchorMax` 设为 `(1,1)`、`pivot` 设为 `(0.5,0.5)`，将 `offsetMin` 和 `offsetMax` 都设为零。设置 `title.alignment = TextAnchor.MiddleCenter`、`title.alignByGeometry = true`，让字形与图片共用中心；保留原字号、深色文字和菱形图片。

无需调整标记坐标、房间热区或共用 Prefab；人物路径页保持原排版。现有拖拽预览复制同一标签，自动沿用该居中设置。

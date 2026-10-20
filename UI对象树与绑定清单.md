## 2026-10-21 地图热区绑定与楼层文字修复

- 校正 Map 的 FloorImage 控件误绑到 Rooms/RawImage：现在固定绑定真正 FloorImage 的 RawImage，Rooms 绑定其 RectTransform。保存的场景中 Rooms 对象存在，已恢复核对绑定。
- 删除 Rooms 上的 RawImage 组件，Rooms 仅保留热区容器职责；地图只由 FloorImage 绘制，切层不再重新启用/赋图到 Rooms，避免重影与误删贴图组件造成缺失引用。
- Floor1Label / Floor2Label 原固定左上锚点配合零高度尺寸造成不显示，改为在各自按钮内拉伸；运行时明确启用并显示1F/2F，保留选中标签在前、非选中标签在纸后。
- 验证：Roslyn 编译、全部场景 View 引用及 RectTransform 父子检查通过；确认 Rooms 无贴图组件、FloorImage 与 Rooms 指向不同对象、楼层文字有有效拉伸区域。未进入 Unity Play，最终显示及点击仍待运行验收；未构建、未改存档、未提交推送。

## 2026-10-21 地图显示与当前位置标记补修

- 修复 Map 的 FloorImage 场景对象关闭导致的空白地图：场景初始激活并设置一层贴图，MapArt 每次渲染明确激活对象和 RawImage，再按楼层更新纹理。Rooms 继续仅作为动态热区容器，不恢复会遮挡二层的固定一层贴图。
- Details 与悬浮 Tooltip 删除“当前位置”文字，当前位置房间不再另画菱形文字；CurrentLocationMarker 绑定改指向作者现有 Overlays/Map/current_location。保留该组件的素材、尺寸与缩放，取消射线阻挡；按当前房间中心换算至 Map 父坐标，图钉底端贴位置，同层显示、异层隐藏。停用 FloorImage 下重复的旧标记，标记不放入会被清理的 Rooms。
- 验证：Roslyn 编译通过；FloorImage 激活/组件启用/贴图引用、现有 current_location 绑定与重复标记关闭静态检查通过；场景父子关系与固定绑定回归检查通过。未进入 Unity Play，实际显示及位置对齐尚待运行验收；未构建、未改存档、未提交推送。

## 2026-10-21 地图双层纸张与标签层级

- Overlays/Map 的 Panel 作为 Paper 绑定，按楼层切换 UI整理/地图/地图一层.png 与 地图2层.png；使用完整1600×900画布比例。两枚楼层标签改用同目录原素材比例和参考位置，文字靠右。
- 层级按未选中标签 → 当前纸张 → FloorImage → Rooms热区 → 选中标签排列，每次切层同步更新。Dismiss 始终在底层，Tooltip 保持前景；未选中标签外露部分仍可点击。
- 禁用 Rooms 上遗留的固定一层 RawImage，仅保留其热区容器，避免它遮挡真正随楼层切换的 FloorImage。保留现有两层地图配置、当前地点标记、房间点击、悬浮详情与空白关闭，不改 Path / Space 的地图展示。
- 验证：Roslyn 编译、固定绑定及 RectTransform 父子检查通过；已查看两张参考图及素材。未进入 Unity Play，动态切层与最终渲染待实际运行验收；未构建、未改存档、未提交推送。

## 2026-10-21 对话头像、调查册封面及 Toast 修复

- 按用户提供《对话头像贴框修复说明.md》仅修改 ConversationPortrait.prefab/Head 的 RectTransform：位置(13.5,-15)、尺寸(86.5,117.5)、Z旋转+2°（四元数同步）。保持左上锚点/轴心、单位缩放；Frame、GUID、HeadRect、DrawHead 及点击行为不变，不套改调查册头像或 StackHead。
- Journal/Cover 保留本次读取到的手调 Transform：位置(50.864994,0)、尺寸(1525.3911,896.667)、缩放(1,1,1)。尺寸跳变来自道具/记录页 PlainRightPage 仍使用旧矩形；场景初始值与运行时分类切换均让覆盖图跟随 Cover 的锚点、位置、尺寸、轴心、旋转和缩放，绝不反向覆盖 Cover。
- 移除 Explore/People 中额外生成的“使用邀请函 / 打开地图，选择客房”操作卡片，左下头像区不再混入此类流程门入口。对白原链接、历史对白链接有效性、调查册道具使用和地图选择流程仍保留。
- 按用户最新要求覆盖上一轮提示规则：所有原 FloatingHint 触发点均改为 Toast，Toast 调用直接显示场景 Toast/Body，停留5秒后隐藏，显示层级置顶且不拦截点击；FloatingHint 保留序列化兼容绑定但关闭，无激活路径。
- 验证：Unity 随附 Roslyn 编译通过；239项真实 V24Engine 隔离断言通过；场景固定绑定/父子关系检查通过；头像参数回读、仅修改 Head、Cover 原始块未改写与覆盖图一致性检查通过。未进入 Unity Play，头像最终贴框、分类切换及提示显示待实际运行验收；未构建、未改玩家存档、未提交推送。

## 2026-10-21 九项 UI 追加修订

- Timeline 的 SortRoot/Submit 及 SubmitLabel 绑定已删除。仅玩家实际改变槽位且全部放齐、证据条件满足后自动提交；错误排序保留可继续交换，每次更改再次校验，打开/重绘不重复提交。
- Explore/CharacterImage 在运行时固定 anchoredPosition=(-468.4181,-331.5084)、sizeDelta=(612.4181,1122.509)，保留现有锚点和等比显示。
- 对白间距入口在场景 UIFlow 的 CastleV24Controller：DialogueLineSpacing=12（条内行距）、DialogueEntrySpacing=14（相邻条目间距），可直接在 Inspector 手调；现场和 Tape 通用对白一起生效。
- V24 全部 Toast 调用统一转到 FloatingHint；旧 Toast 不再显示。切换页面不会主动清掉刚出现的提示，仍按原悬浮组件 5 秒淡出。未改变提示文案和触发条件。
- Journal 新增底层全屏透明 Dismiss 按钮，点击空白合上；标签、道具、翻页与详情滚动仍各自消费点击，怀表关闭入口兼容保留。Overlays/example 保留编辑参考，运行时关闭，避免遮挡交互。
- 道具页四个可见框按 example 的参考图分别匹配上排/下排尺寸（上排195×170.833，下排216.667×187.5，1600画布单位）；补偿原 PNG 透明留边，已输出并查看叠图。道具 RawImage 共享 BookItemCard.prefab 的 Shadow，颜色 #130B0B、alpha 0.34，偏移(0,-6)，全部实例生效。框、胶带和图标落点同步调整。
- Recordings / Anchors / Space 及顶层 Path 使用 UI整理/中控/锚点对齐/鼠标.png；其他页面和地图、调查册等覆盖页恢复普通指针，退出覆盖页后恢复对应页面指针。新指针导入为32像素可读 Cursor，不生成新美术。
- Map 的 Details 绑定及可见 Tooltip 统一补充“当前位置：实际场景名”，包含查看未解锁房间的情况，不将悬停房间误报为当前位置。
- 四个绿色中控页只统一 Backdrop：使用 UI整理/中控/人物推演/图层 1.png，1600×900、同锚点与比例。逐组件对照修改前快照，确认各页内部其他对象、组件和绑定保持不变。
- 实际验证：Unity 随附 Roslyn 编译通过；原有239项真实 V24Engine 隔离断言通过；全部场景 View 引用、父子关系、Dismiss 顺序、Submit 清理、Cursor 导入、Shadow 参数及中控内部保持不变检查通过；道具框源素材叠图已查看。**未进入 Unity Play，最终渲染、鼠标切换、拖放自动提交及点击关闭仍待 Unity 内验收。** 未改玩家存档、未构建、未提交推送。

## 2026-10-21 Path / Timeline / Tape 绑定更新

- Path 仍位于 UIFlow/Canvas/Stage/Overlays/Path；Backdrop 改为 UI整理/中控/人物推演/图层 1.png，新增 MapArea、FloorBar、SpaceUnderline、FloorUnderline。原 Close 改为左上返回箭头，旧调试式说明/缩放按钮/假设框隐藏，绑定保留。新增 Path 内的 JournalShortcut / MapShortcut / SettingsShortcut 三个固定按钮及同名 Button 绑定，避免被全屏弹层遮住下方 HUD。
- Path/MapViewport/MapContent 为 V24MapPan.Content，锚点/轴心居中；FloorImage、Rooms 为其同级子对象，位置零、1140×416，底图用 V24PathMap 材质。FloorImage / Rooms 的原绑定仍指向各自原组件。新增控制器 PathLabelPrefab、PersonPathMarkerIcon。
- Timeline/TimelineArt 为固定共用背景层，内含 ReferenceWave、ClueDivider、Connection1～9、WavePoint0～9、Slots/Slot0～9。原 Slots 绑定不变，新增 Slot0～9 绑定至各自 CastleUiView；槽位均保存到场景，不再运行创建/清空。
- Timeline/SortRoot/CluesScroll 改为 (420,-770)、760×105，水平滚动，内容为固定一行 GridLayoutGroup、HorizontalFit=PreferredSize；原纵向条隐藏并解除引用。控制器新增 TimelineCluePrefab。TimelineSlot / TimelineClue / PersonPathLabel 与 .meta 一起保存于 Prefabs/V24。
- Tape/DialoguePanel 从当前 Explore/DialoguePanel 复用布局保存；LinesScroll / Lines 绑定分别改指新面板的 DialogueScroll / Viewport/Dialogue，新增 DialoguePanel、Backdrop 绑定。旧 Tape 工作台边框、标签、旧列表等隐藏，Play / Progress / Playback / FileInfo 移至对白面板区域；退出继续使用 Hud/Back。
- 所有新固定绑定已静态核对；尚未完成 Unity 内实际渲染、Shader 与点击/拖放验收。

## 2026-10-21 Journal / Map 新绑定

- 控制器 BookmarkNormal / BookmarkSelected 引用 UI整理/调查-角色 的未选中/选中标签 Sprite，均裁切可见区域；Tab0/1/2 与 BookmarkIcon 保留原对象和绑定。
- Journal/PlainRightPage：固定 RawImage，不接收射线，使用 UI整理/调查-角色/bg_diaocha.png 与 V24JournalPaper 材质；同名绑定，位于 Cover 后、内容前，只在道具/记录页显示。原 CharacterDecoration/Note 隐藏并清空旧 Sprite（便笺已在新底图中）。Close 保留绑定，透明热区移到右侧怀表；模板尺寸同步运行布局。
- Map/Dismiss：固定全屏透明 Image + Button，位于子对象首位，同名绑定指向 Button；仅空白点击关闭。Close 隐藏保留旧引用，FloorImage/Panel 不拦射线。
- Map/FloorImage/CurrentLocationMarker：固定 Image + RectTransform，图来自 UI整理/地图/图层 16.png，不拦射线；同名绑定到 RectTransform。与 Rooms 分离，避免清理动态房间按钮时销毁。
- TooltipPanel / Tooltip 继续用原绑定，面板 300×165、正文 264×141；运行定位到对应房间顶部并限制画面内。FloorImage 使用 V24MapPaper 材质与 Scene_update 两层地图，区域与底图共用原 Crop。
- 编辑预览入口仍在 UIFlow/Canvas 的 CastleFlowUi 组件。新材质和场景绑定已保存，C# 编译与引用静态检查通过；Unity 实际渲染/点击未验收。

## 2026-10-20 开场黑屏旁白

- UIFlow/Canvas/Stage/Pages/Opening：固定 RectTransform + Image + Button + CastleUiView（Id=Opening），已登记 CastleFlowUi.Pages；黑底全屏，Surface 绑定根 Button，无按钮变色和导航选择效果。
- Opening/Body：Body 绑定 Text，ReadingFont（I.Ming）30 号、纯白、居中、无富文本、无自动字号，不接收射线；锚点从 (0.15,0.3) 到 (0.85,0.7)，偏移为零，文本区域横向占画面 70%。
- UIFlow/Canvas/Stage/OpeningTransition：固定全屏 Image + CanvasGroup，Opening 的 Transition 绑定指向此组。位于 Stage 最后、弹窗和提示层之上，黑底，初始隐藏且 alpha=0；blocksRaycasts=true、interactable=false，激活时拦截点击，负责单程 0.35 秒淡入淡出，结束后隐藏。
- 运行时只更新文字、显隐和透明度，不创建页面或组件；原 Cutscene／Invitation 页面及绑定保留。

## 2026-10-20 人物路径悬浮引导

- UIFlow/Canvas/Stage/FloatingHint：复用 AnchorDetails 的 RectTransform、Image、CanvasGroup 和 Body，删除来源行；新增 CastleUiView（Id=FloatingHint），Controls 的 Group 指向 CanvasGroup、Body 指向 TextMeshProUGUI，控制器 FloatingHint 字段指向该视图。
- 全局位于 Toast 后、所有弹窗之上；底部横向铺满，位置 (0,8)、高度 128，黑色晕影沿用 UI/刻录/弹窗背景.png。Body 宽度为画面 75%、y=56、高 32，V24Workbench 24 号、居中单行 Ellipsis；沿用锚点详情的浅米白及材质。
- 初始隐藏、alpha=0、Body 为空；CanvasGroup.interactable／blocksRaycasts=false，Image／Body 不接收射线。运行时仅更新文案、透明度和显隐，5 秒平滑淡出，不新增固定组件。
- 原解锁 Confirm 结构与绑定保留；成功引导前由 JournalRender 更新人物详情及 CharacterPath 显隐。

## 2026-10-20 调查册收齐人物立绘

- UIFlow/Canvas/Stage/Overlays/Journal/CharacterPortrait：编辑期新增 RectTransform + CanvasRenderer + Image，同名绑定指向 Image，初始隐藏、sprite=null。
- anchorMin／anchorMax／pivot=(1,0)，anchoredPosition=(0,0)，默认尺寸 446×740、缩放 1；运行时保持高度并按 Sprite 比例更新宽度。preserveAspect=true、Raycast Target=false，右边缘与下边缘贴齐游戏画面。
- 位于 Details 后、FlipSheet 及操作按钮前；不增加遮罩或背景。JournalCharacterPortrait 只更新图片、宽度和显隐；人物收齐判定由 Engine.CharacterInspirationComplete 提供，切换人物／分类时清理旧图。

## 2026-10-20 录音文件目录滚动条

- Recordings/ListScroll：原位置 (155,-340)，尺寸 230×385；Viewport 与 List 宽 195，内容不被新滚动条占用。verticalScrollbar 重新指向现有子 Scrollbar，verticalScrollbarVisibility=Permanent。
- ListScroll/Scrollbar：启用，位置 (200,-5)、尺寸 25×351；轨道和 Handle 的 Image 复用 LinesScroll 滚动条美术与颜色，保留原 Scrollbar、Handle 引用和 BottomToTop 方向。
- Recordings/LinesScroll：位置 (395,-340)、尺寸 1077×383；Viewport 与 Lines 宽 1032，原正文 850 宽及信息标记保留。其 Scrollbar 栏内 x 改为 1025，画面中的位置不变。两栏间保留 10 个画布单位，左栏左边缘与右栏右边缘不变。
- 仅修改场景内尺寸、显隐和 ScrollRect 的滚动条引用，原 ListScroll／LinesScroll 等视图绑定键不变，无新增对象或组件。

## 2026-10-20 收录窗口确认按钮

- Acquire/Confirm 的 RectTransform 在原值基础上等比放大 15%，文字随父级同步放大，保持原中心；最终 anchoredPosition 约 (981.963,-590.286)、localScale 约 (0.570,0.832,1)，sizeDelta 仍为 270×50。
- 场景已保存，原 Button、Confirm／ConfirmLabel 绑定、字体、美术和点击逻辑不变；不影响通用 Confirm 双按钮弹窗。

## 2026-10-20 调查册人物页布局（BUG-018）

- BookPersonCard.prefab：Head 位置 (23,-16)、尺寸 179×200；Title 位置 (16,-255)、尺寸 198×35，字号／自动字号上限 24。原头像、姓名及按钮绑定不变。
- Journal/Details：人物 DetailTitle 位置 (1068,-184)、尺寸 300×85、Z 旋转 -4°；DetailScroll 位置 (950,-409)、尺寸 420×330；DetailBody 宽 405、统一 lineSpacing=1.4。按实拍反馈将正文从 -405 回落至 -409（不低于此前 -412），姓名继续左上移动；位置均为左上锚定，沿用游戏字体。
- 场景保存人物页默认样式，JournalAppearance 切换分类时同步应用；记录页恢复原 DetailTitle (1084.4,-200.7)、Z 旋转 3.53°、DetailScroll (890,-340)、450×390、正文宽 445 和 1 倍行距。无新增对象或绑定。

## 2026-10-20 对白收录提示绑定

- Explore/DialoguePanel/DialogueCollectionHint：固定 RectTransform + TextMeshProUGUI，绑定键 DialogueCollectionHint 指向 TMP。左上锚定，位置 (38,-751)、尺寸 390×44，位于自动播放键左侧，保持原按钮位置与对白视口尺寸。
- 使用对白 V24Reading 字体及材质，18 号、不自动缩字号，米白色沿用正文且 alpha=0.65；左对齐、允许换行、最多两行、超长 Ellipsis；Rich Text／Raycast Target 关闭，初始空文本且隐藏。
- 控制器订阅 V24Engine.DialogueStarted／EntryGranted，只更新文本与显隐；同段对白保留最新收录结果，新对白清空，不在运行时创建固定 UI。

## 2026-10-20 锚点底部详情绑定

- Anchors/AnchorDetails：固定 RectTransform + Image + CanvasGroup，绑定键 AnchorDetails 仍指向 RectTransform，底部横向铺满，位置 (0,8)、高度 128；Image 复用 UI/刻录/弹窗背景.png，拉伸显示，默认隐藏。CanvasGroup 初始 alpha=0，interactable／blocksRaycasts=false，控制文字与底图统一淡出。
- Body：绑定 AnchorDetailsText（TextMeshProUGUI），宽度为页面 75%，底部相对中心 y=56、高 32、24 号字；Source：绑定 AnchorDetailsSource（TextMeshProUGUI），同宽、y=24、高 26、20 号字。
- 两行均使用 V24Workbench，居中、关闭自动字号与换行、Ellipsis 溢出，字体颜色为浅米白。背景和两行文字不接收射线，不遮挡原按钮与拖拽。
- 左侧锚点按钮点击调用 ShowAnchorDetails(true)；第一行显示该锚点正文，第二行为“地点(设备) 录音文件xx”（取配置录音名称），未知位置不揭晓。出现后在 5 秒内平滑淡出，重复点击重新计时，列表重绘保留计时且不唤回提示；运行时只更新内容、显隐和透明度，不新增组件或生成固定 UI。

## 2026-10-20 场景缩放配置

- UIFlow/CastleV24Controller.RoomArt 新增 AllowZoom：房间 100003（中控室）为 false，其他 15 项已有 2.5D 房间背景为 true；CorridorDay／CorridorNight 覆盖视角强制禁用缩放，未配置背景也不缩放。
- Explore/Backdrop 仍由原 RawImage 显示背景，仅背景 Transform 缩放：2.5D 默认 1.4 倍、范围 1～1.5 倍，固定视角 1 倍；ZoomHint 原对象与绑定保留，ApplySceneZoom 始终隐藏其文字提示，空格复位逻辑不变。

## 2026-10-20 姓名链接反馈参数

- DialogueLine.prefab/Body 保留 V24DialogueLinkFeedback，移除 _hoverBackground／_pressedBackground，改为 _hoverAlpha=0.85、_hoverScale=1.05。悬停与按下使用同一文字反馈，不再绘制底色。
- CastleV24Controller.Line() 继续通过 Configure 登记现场姓名链接及可交互条件；FlowTextLink 点击、V24LineHit 与滚动组件绑定不变，无新增场景对象。

## 2026-10-20 对白滚动绑定

- Explore/DialoguePanel/DialogueScroll：新增 V24DialogueScrollInput，Scroll 指向自身 ScrollRect，IsScrollbar=false；运行时 UserScrolled 绑定 OnDialogueUserScroll。Viewport 高度 610（普通对白向下延长 40），选项时 480，既有正文与链接引用保留。
- Scrollbar：新增 V24DialogueScrollbarInput，Scroll 指向同一 ScrollRect，IsScrollbar=true；仅滑动条接收 PointerDown，正文组件不截获链接点击。轨道位置 (445,-80)，尺寸 41×502、缩放 1；选项时高度 372。
- Scrollbar/SlidingArea 为新 RectTransform，上下各内缩 26；原 Handle 连同 Art 移入其中，Scrollbar.handleRect 保持原 Handle，滑块图居中。背景、滑块 Image 保持射线接收，Art 不接收射线。

## 2026-10-20 道具链接提示绑定

- CastleEditable：Explore/ItemLinkHint 为固定 CanvasGroup + Image，绑定键 ItemLinkHint 指向 CanvasGroup；子 Body 的 TextMeshProUGUI 绑定 ItemLinkHintText。原交谈选项边框不用于提示。
- 相对 Explore 底部居中，位置 (0,28)、尺寸 1120×70；Image 复用 UI/刻录/弹窗背景.png 黑色晕影。字体和材质与 LinePrefab/Body 一致，V24Reading、24 号。Body 左右各内缩 14、底部 6、顶部 30，文字相对原位置下移 12，位于晕影实色区。
- 初始隐藏且 alpha=0；CanvasGroup 不交互、不阻挡射线，Image／Body 的 Raycast Target 关闭。控制器只控制显隐、透明度和文案，不在运行时新增固定 UI。

## 2026-10-20 对话框点击推进

- Explore/DialoguePanel 新挂 V24LineHit，运行时绑定 AdvanceDialogueFromPanel；现有 Back/Image 开启 Raycast Target，作为底层空白点击区域。无新增 UI 对象，不改变尺寸和绘制顺序。
- 普通现场对白及玩家回复的 Body/V24LineHit 同样绑定推进；FlowTextLink 优先处理关键词链接，录音正文保持标记回调。Continue、Auto、选项及滚动条原绑定保留。

## 2026-10-20 对话选项绑定

- CastleV24Controller.DialogueChoicePrefab → Assets/Castle/Prefabs/V24/DialogueChoice.prefab；绑定 Title、Body（隐藏）、Select、Background。模板挂 V24DialogueChoice，Border 指向根 Image，Glow 指向 HighlightMask，FirstBorder／RegularBorder 分别绑定 button_talking_01／02。
- HighlightMask 使用 02 的透明轮廓裁切子级 GoldTexture（03），仅悬停／选中／按下显示，不拦截鼠标。
- Explore/DialoguePanel/Choices 改为 VerticalLayoutGroup，位置 (50,-586)、尺寸 425×162、间距 8；第一项高 54，其余高 46。选项期间 DialogueScroll／Viewport 高度 480，结束后恢复。

## 2026-10-20 新增绑定与字体

- UIFlow / CastleV24Controller：WorkbenchFont 为点点像素体-方形，ReadingFont 为 I.Ming-7.00，ButtonFont 为万事全兴体。TMP 字库 V24Reading／V24Workbench；中控、阅读与主界面字体按用途分配。
- Acquire/IconBack 保留原区域 RectTransform，仅在获得道具时停用静态 Image 并显示子级 ItemArt（RawImage）；线索、人物灵感等非道具恢复原固定 Image，隐藏 ItemArt。绑定键 ItemArt 保留。
- Burn/FilesScroll 的内容使用 RecordingFolder／RecordingFile 预制件；List、Burn、Close 原绑定保持不变。
- 新增 Overlays/Credits（Image 黑底 + CastleUiView），登记 Ui.Modals；Credits/Close 绑定关闭按钮，标题页 Credits 按钮运行时打开此弹层。Heading、Role0～10、Name0～10 为可编辑的固定白色文本。
- Space 的 DoorOpen／DoorClosed 按当前标签独立设置；Details 显示各标签状态。已放置房间点击选择、拖放移动。

# V2.4 UI 对象树与绑定清单

固定组件已保存到 CastleEditable.unity；运行时仅复制列表 Prefab。UIFlow 挂 CastleV24Controller（旧 CastleFlowController 已禁用）。Canvas 挂 CastleFlowUi。

## Title

| 控件 ID | 组件 | 对象路径 |
|---|---|---|
| Title | Text | UIFlow/Canvas/Stage/Pages/Title/Title |
| Subtitle | Text | UIFlow/Canvas/Stage/Pages/Title/Subtitle |
| Start | Button | UIFlow/Canvas/Stage/Pages/Title/Start |
| StartLabel | Text | UIFlow/Canvas/Stage/Pages/Title/Start/StartLabel |
| Continue | Button | UIFlow/Canvas/Stage/Pages/Title/Continue |
| ContinueLabel | Text | UIFlow/Canvas/Stage/Pages/Title/Continue/ContinueLabel |
| Settings | Button | UIFlow/Canvas/Stage/Pages/Title/Settings |
| SettingsLabel | Text | UIFlow/Canvas/Stage/Pages/Title/Settings/SettingsLabel |
| Credits | Button | UIFlow/Canvas/Stage/Pages/Title/Credits |
| CreditsLabel | Text | UIFlow/Canvas/Stage/Pages/Title/Credits/CreditsLabel |
| Quit | Button | UIFlow/Canvas/Stage/Pages/Title/Quit |
| QuitLabel | Text | UIFlow/Canvas/Stage/Pages/Title/Quit/QuitLabel |

## Invitation

| 控件 ID | 组件 | 对象路径 |
|---|---|---|
| Surface | Button | UIFlow/Canvas/Stage/Pages/Invitation/Surface |
| SurfaceLabel | Text | UIFlow/Canvas/Stage/Pages/Invitation/Surface/SurfaceLabel |
| Heading | Text | UIFlow/Canvas/Stage/Pages/Invitation/Heading |
| Body | Text | UIFlow/Canvas/Stage/Pages/Invitation/Body |
| Artwork | Image | UIFlow/Canvas/Stage/Pages/Invitation/Letter |

## Explore

| 控件 ID | 组件 | 对象路径 |
|---|---|---|
| RoomName | Text | UIFlow/Canvas/Stage/Pages/Explore/RoomName |
| Portrait | Text | UIFlow/Canvas/Stage/Pages/Explore/Portrait |
| People | RectTransform | UIFlow/Canvas/Stage/Pages/Explore/PeopleScroll/Viewport/People |
| PeopleScroll | ScrollRect | UIFlow/Canvas/Stage/Pages/Explore/PeopleScroll |
| FreePanel | RectTransform | UIFlow/Canvas/Stage/Pages/Explore/FreePanel |
| RoomHint | Text | UIFlow/Canvas/Stage/Pages/Explore/FreePanel/RoomHint |
| Talk | Button | UIFlow/Canvas/Stage/Pages/Explore/FreePanel/Talk |
| TalkLabel | Text | UIFlow/Canvas/Stage/Pages/Explore/FreePanel/Talk/TalkLabel |
| Map | Button | UIFlow/Canvas/Stage/Pages/Explore/FreePanel/Map |
| MapLabel | Text | UIFlow/Canvas/Stage/Pages/Explore/FreePanel/Map/MapLabel |
| ExploreRoom | Button | UIFlow/Canvas/Stage/Pages/Explore/FreePanel/ExploreRoom |
| ExploreRoomLabel | Text | UIFlow/Canvas/Stage/Pages/Explore/FreePanel/ExploreRoom/ExploreRoomLabel |
| Workbench | Button | UIFlow/Canvas/Stage/Pages/Explore/FreePanel/Workbench |
| WorkbenchLabel | Text | UIFlow/Canvas/Stage/Pages/Explore/FreePanel/Workbench/WorkbenchLabel |
| DialoguePanel | RectTransform | UIFlow/Canvas/Stage/Pages/Explore/DialoguePanel |
| Dialogue | RectTransform | UIFlow/Canvas/Stage/Pages/Explore/DialoguePanel/DialogueScroll/Viewport/Dialogue |
| DialogueScroll | ScrollRect | UIFlow/Canvas/Stage/Pages/Explore/DialoguePanel/DialogueScroll |
| Choices | RectTransform | UIFlow/Canvas/Stage/Pages/Explore/DialoguePanel/Choices |
| Auto | Button | UIFlow/Canvas/Stage/Pages/Explore/DialoguePanel/Auto |
| AutoLabel | Text | UIFlow/Canvas/Stage/Pages/Explore/DialoguePanel/Auto/AutoLabel |
| Continue | Button | UIFlow/Canvas/Stage/Pages/Explore/DialoguePanel/Continue |
| ContinueLabel | Text | UIFlow/Canvas/Stage/Pages/Explore/DialoguePanel/Continue/ContinueLabel |
| ExitTape | Button | UIFlow/Canvas/Stage/Pages/Explore/DialoguePanel/ExitTape |
| ExitTapeLabel | Text | UIFlow/Canvas/Stage/Pages/Explore/DialoguePanel/ExitTape/ExitTapeLabel |
| CharacterImage | Image | UIFlow/Canvas/Stage/Pages/Explore/CharacterImage |
| Protagonist | Text | UIFlow/Canvas/Stage/Pages/Explore/Protagonist |
| HeroineImage | Image | UIFlow/Canvas/Stage/Pages/Explore/HeroineImage |
| ZoomHint | Text | UIFlow/Canvas/Stage/Pages/Explore/ZoomHint |
| AvatarStack | Button | UIFlow/Canvas/Stage/Pages/Explore/AvatarStack |
| StackHead | RawImage | UIFlow/Canvas/Stage/Pages/Explore/AvatarStack/Head |

## Recordings

| 控件 ID | 组件 | 对象路径 |
|---|---|---|
| Recordings | Button | UIFlow/Canvas/Stage/Pages/Recordings/Recordings |
| RecordingsLabel | Text | UIFlow/Canvas/Stage/Pages/Recordings/Recordings/RecordingsLabel |
| Anchors | Button | UIFlow/Canvas/Stage/Pages/Recordings/Anchors |
| AnchorsLabel | Text | UIFlow/Canvas/Stage/Pages/Recordings/Anchors/AnchorsLabel |
| Space | Button | UIFlow/Canvas/Stage/Pages/Recordings/Space |
| SpaceLabel | Text | UIFlow/Canvas/Stage/Pages/Recordings/Space/SpaceLabel |
| PreviousPuzzle | Button | UIFlow/Canvas/Stage/Pages/Recordings/PreviousPuzzle |
| PreviousPuzzleLabel | Text | UIFlow/Canvas/Stage/Pages/Recordings/PreviousPuzzle/PreviousPuzzleLabel |
| PuzzleName | Text | UIFlow/Canvas/Stage/Pages/Recordings/PuzzleName |
| NextPuzzle | Button | UIFlow/Canvas/Stage/Pages/Recordings/NextPuzzle |
| NextPuzzleLabel | Text | UIFlow/Canvas/Stage/Pages/Recordings/NextPuzzle/NextPuzzleLabel |
| List | RectTransform | UIFlow/Canvas/Stage/Pages/Recordings/ListScroll/Viewport/List |
| ListScroll | ScrollRect | UIFlow/Canvas/Stage/Pages/Recordings/ListScroll |
| FileInfo | Text | UIFlow/Canvas/Stage/Pages/Recordings/FileInfo |
| Lines | RectTransform | UIFlow/Canvas/Stage/Pages/Recordings/LinesScroll/Viewport/Lines |
| LinesScroll | ScrollRect | UIFlow/Canvas/Stage/Pages/Recordings/LinesScroll |
| Playback | Text | UIFlow/Canvas/Stage/Pages/Recordings/Playback |
| Play | Button | UIFlow/Canvas/Stage/Pages/Recordings/Play |
| PlayLabel | Text | UIFlow/Canvas/Stage/Pages/Recordings/Play/PlayLabel |
| Progress | Slider | UIFlow/Canvas/Stage/Pages/Recordings/Progress |
| Done | Button | UIFlow/Canvas/Stage/Pages/Recordings/Done |
| DoneLabel | Text | UIFlow/Canvas/Stage/Pages/Recordings/Done/DoneLabel |
| Extract | Button | UIFlow/Canvas/Stage/Pages/Recordings/Extract |
| ExtractLabel | Text | UIFlow/Canvas/Stage/Pages/Recordings/Extract/Label |
| Duration | Text | UIFlow/Canvas/Stage/Pages/Recordings/Duration |

## Anchors

| 控件 ID | 组件 | 对象路径 |
|---|---|---|
| Recordings | Button | UIFlow/Canvas/Stage/Pages/Anchors/Recordings |
| RecordingsLabel | Text | UIFlow/Canvas/Stage/Pages/Anchors/Recordings/RecordingsLabel |
| Anchors | Button | UIFlow/Canvas/Stage/Pages/Anchors/Anchors |
| AnchorsLabel | Text | UIFlow/Canvas/Stage/Pages/Anchors/Anchors/AnchorsLabel |
| Space | Button | UIFlow/Canvas/Stage/Pages/Anchors/Space |
| SpaceLabel | Text | UIFlow/Canvas/Stage/Pages/Anchors/Space/SpaceLabel |
| PreviousPuzzle | Button | UIFlow/Canvas/Stage/Pages/Anchors/PreviousPuzzle |
| PreviousPuzzleLabel | Text | UIFlow/Canvas/Stage/Pages/Anchors/PreviousPuzzle/PreviousPuzzleLabel |
| PuzzleName | Text | UIFlow/Canvas/Stage/Pages/Anchors/PuzzleName |
| NextPuzzle | Button | UIFlow/Canvas/Stage/Pages/Anchors/NextPuzzle |
| NextPuzzleLabel | Text | UIFlow/Canvas/Stage/Pages/Anchors/NextPuzzle/NextPuzzleLabel |
| List | RectTransform | UIFlow/Canvas/Stage/Pages/Anchors/ListScroll/Viewport/List |
| ListScroll | ScrollRect | UIFlow/Canvas/Stage/Pages/Anchors/ListScroll |
| DropZone | FlowDrag | UIFlow/Canvas/Stage/Pages/Anchors/TrackDropArea |
| Tracks | RectTransform | UIFlow/Canvas/Stage/Pages/Anchors/TrackDropArea/TracksScroll/Viewport/Tracks |
| TracksScroll | ScrollRect | UIFlow/Canvas/Stage/Pages/Anchors/TrackDropArea/TracksScroll |
| Status | Text | UIFlow/Canvas/Stage/Pages/Anchors/Status |
| Check | Button | UIFlow/Canvas/Stage/Pages/Anchors/Check |
| CheckLabel | Text | UIFlow/Canvas/Stage/Pages/Anchors/Check/CheckLabel |

## Space

| 控件 ID | 组件 | 对象路径 |
|---|---|---|
| Recordings | Button | UIFlow/Canvas/Stage/Pages/Space/Recordings |
| RecordingsLabel | Text | UIFlow/Canvas/Stage/Pages/Space/Recordings/RecordingsLabel |
| Anchors | Button | UIFlow/Canvas/Stage/Pages/Space/Anchors |
| AnchorsLabel | Text | UIFlow/Canvas/Stage/Pages/Space/Anchors/AnchorsLabel |
| Space | Button | UIFlow/Canvas/Stage/Pages/Space/Space |
| SpaceLabel | Text | UIFlow/Canvas/Stage/Pages/Space/Space/SpaceLabel |
| PreviousPuzzle | Button | UIFlow/Canvas/Stage/Pages/Space/PreviousPuzzle |
| PreviousPuzzleLabel | Text | UIFlow/Canvas/Stage/Pages/Space/PreviousPuzzle/PreviousPuzzleLabel |
| PuzzleName | Text | UIFlow/Canvas/Stage/Pages/Space/PuzzleName |
| NextPuzzle | Button | UIFlow/Canvas/Stage/Pages/Space/NextPuzzle |
| NextPuzzleLabel | Text | UIFlow/Canvas/Stage/Pages/Space/NextPuzzle/NextPuzzleLabel |
| List | RectTransform | UIFlow/Canvas/Stage/Pages/Space/ListScroll/Viewport/List |
| ListScroll | ScrollRect | UIFlow/Canvas/Stage/Pages/Space/ListScroll |
| FloorImage | RawImage | UIFlow/Canvas/Stage/Pages/Space/MapViewport/Rooms |
| Rooms | RectTransform | UIFlow/Canvas/Stage/Pages/Space/MapViewport/Rooms |
| Tooltip | Text | UIFlow/Canvas/Stage/Pages/Space/TooltipPanel/Tooltip |
| Details | Text | UIFlow/Canvas/Stage/Pages/Space/Details |
| Floor1 | Button | UIFlow/Canvas/Stage/Pages/Space/Floor1 |
| Floor1Label | Text | UIFlow/Canvas/Stage/Pages/Space/Floor1/Floor1Label |
| Floor2 | Button | UIFlow/Canvas/Stage/Pages/Space/Floor2 |
| Floor2Label | Text | UIFlow/Canvas/Stage/Pages/Space/Floor2/Floor2Label |
| Verify | Button | UIFlow/Canvas/Stage/Pages/Space/Verify |
| VerifyLabel | Text | UIFlow/Canvas/Stage/Pages/Space/Verify/VerifyLabel |
| TooltipPanel | RectTransform | UIFlow/Canvas/Stage/Pages/Space/TooltipPanel |
| ResetView | Button | UIFlow/Canvas/Stage/Pages/Space/ResetView |
| ResetViewLabel | Text | UIFlow/Canvas/Stage/Pages/Space/ResetView/Label |
| MapPan | V24MapPan | UIFlow/Canvas/Stage/Pages/Space/MapViewport |
| ZoomIn | Button | UIFlow/Canvas/Stage/Pages/Space/ZoomIn |
| ZoomOut | Button | UIFlow/Canvas/Stage/Pages/Space/ZoomOut |
| Hypothesis | InputField | UIFlow/Canvas/Stage/Pages/Space/Hypothesis |
| DoorOpen | Button | UIFlow/Canvas/Stage/Pages/Space/DoorOpen |
| DoorClosed | Button | UIFlow/Canvas/Stage/Pages/Space/DoorClosed |

## Timeline

| 控件 ID | 组件 | 对象路径 |
|---|---|---|
| Heading | Text | UIFlow/Canvas/Stage/Pages/Timeline/Heading |
| SortRoot | RectTransform | UIFlow/Canvas/Stage/Pages/Timeline/SortRoot |
| Clues | RectTransform | UIFlow/Canvas/Stage/Pages/Timeline/SortRoot/CluesScroll/Viewport/Clues |
| CluesScroll | ScrollRect | UIFlow/Canvas/Stage/Pages/Timeline/SortRoot/CluesScroll |
| Slots | RectTransform | UIFlow/Canvas/Stage/Pages/Timeline/SortRoot/Slots |
| Hint | Text | UIFlow/Canvas/Stage/Pages/Timeline/SortRoot/Hint |
| Submit | Button | UIFlow/Canvas/Stage/Pages/Timeline/SortRoot/Submit |
| SubmitLabel | Text | UIFlow/Canvas/Stage/Pages/Timeline/SortRoot/Submit/SubmitLabel |
| RevealRoot | RectTransform | UIFlow/Canvas/Stage/Pages/Timeline/RevealRoot |
| Wave | RectTransform | UIFlow/Canvas/Stage/Pages/Timeline/RevealRoot/Wave |
| EndingStatus | Text | UIFlow/Canvas/Stage/Pages/Timeline/RevealRoot/EndingStatus |
| EndingText | Text | UIFlow/Canvas/Stage/Pages/Timeline/RevealRoot/EndingText |
| Progress | Slider | UIFlow/Canvas/Stage/Pages/Timeline/RevealRoot/Progress |
| Play | Button | UIFlow/Canvas/Stage/Pages/Timeline/RevealRoot/Play |
| PlayLabel | Text | UIFlow/Canvas/Stage/Pages/Timeline/RevealRoot/Play/PlayLabel |
| Replay | Button | UIFlow/Canvas/Stage/Pages/Timeline/RevealRoot/Replay |
| ReplayLabel | Text | UIFlow/Canvas/Stage/Pages/Timeline/RevealRoot/Replay/ReplayLabel |
| Requirements | Text | UIFlow/Canvas/Stage/Pages/Timeline/Requirements |
| Heroine | Image | UIFlow/Canvas/Stage/Pages/Timeline/RevealRoot/Heroine |
| Partner | Image | UIFlow/Canvas/Stage/Pages/Timeline/RevealRoot/Partner |
| SoundWave | V24EndingWave | UIFlow/Canvas/Stage/Pages/Timeline/RevealRoot/SoundWave |

## Tape

| 控件 ID | 组件 | 对象路径 |
|---|---|---|
| Recordings | Button | UIFlow/Canvas/Stage/Pages/Tape/Recordings |
| RecordingsLabel | Text | UIFlow/Canvas/Stage/Pages/Tape/Recordings/RecordingsLabel |
| Anchors | Button | UIFlow/Canvas/Stage/Pages/Tape/Anchors |
| AnchorsLabel | Text | UIFlow/Canvas/Stage/Pages/Tape/Anchors/AnchorsLabel |
| Space | Button | UIFlow/Canvas/Stage/Pages/Tape/Space |
| SpaceLabel | Text | UIFlow/Canvas/Stage/Pages/Tape/Space/SpaceLabel |
| PreviousPuzzle | Button | UIFlow/Canvas/Stage/Pages/Tape/PreviousPuzzle |
| PreviousPuzzleLabel | Text | UIFlow/Canvas/Stage/Pages/Tape/PreviousPuzzle/PreviousPuzzleLabel |
| PuzzleName | Text | UIFlow/Canvas/Stage/Pages/Tape/PuzzleName |
| NextPuzzle | Button | UIFlow/Canvas/Stage/Pages/Tape/NextPuzzle |
| NextPuzzleLabel | Text | UIFlow/Canvas/Stage/Pages/Tape/NextPuzzle/NextPuzzleLabel |
| List | RectTransform | UIFlow/Canvas/Stage/Pages/Tape/ListScroll/Viewport/List |
| ListScroll | ScrollRect | UIFlow/Canvas/Stage/Pages/Tape/ListScroll |
| FileInfo | Text | UIFlow/Canvas/Stage/Pages/Tape/FileInfo |
| Lines | RectTransform | UIFlow/Canvas/Stage/Pages/Tape/LinesScroll/Viewport/Lines |
| LinesScroll | ScrollRect | UIFlow/Canvas/Stage/Pages/Tape/LinesScroll |
| Playback | Text | UIFlow/Canvas/Stage/Pages/Tape/Playback |
| Play | Button | UIFlow/Canvas/Stage/Pages/Tape/Play |
| PlayLabel | Text | UIFlow/Canvas/Stage/Pages/Tape/Play/PlayLabel |
| Progress | Slider | UIFlow/Canvas/Stage/Pages/Tape/Progress |
| Done | Button | UIFlow/Canvas/Stage/Pages/Tape/Done |
| DoneLabel | Text | UIFlow/Canvas/Stage/Pages/Tape/Done/DoneLabel |
| Extract | Button | UIFlow/Canvas/Stage/Pages/Tape/Extract |
| ExtractLabel | Text | UIFlow/Canvas/Stage/Pages/Tape/Extract/Label |

## Cutscene

| 控件 ID | 组件 | 对象路径 |
|---|---|---|
| Surface | Button | UIFlow/Canvas/Stage/Pages/Cutscene |
| Body | Text | UIFlow/Canvas/Stage/Pages/Cutscene/Body |

## EndCard

| 控件 ID | 组件 | 对象路径 |
|---|---|---|
| Surface | Button | UIFlow/Canvas/Stage/Pages/EndCard |
| Body | Text | UIFlow/Canvas/Stage/Pages/EndCard/Body |

## Map

| 控件 ID | 组件 | 对象路径 |
|---|---|---|
| Heading | Text | UIFlow/Canvas/Stage/Overlays/Map/Heading |
| Close | Button | UIFlow/Canvas/Stage/Overlays/Map/Close |
| CloseLabel | Text | UIFlow/Canvas/Stage/Overlays/Map/Close/CloseLabel |
| FloorImage | RawImage | UIFlow/Canvas/Stage/Overlays/Map/Rooms |
| Rooms | RectTransform | UIFlow/Canvas/Stage/Overlays/Map/Rooms |
| Tooltip | Text | UIFlow/Canvas/Stage/Overlays/Map/TooltipPanel/Tooltip |
| Floor1 | Button | UIFlow/Canvas/Stage/Overlays/Map/Floor1 |
| Floor1Label | Text | UIFlow/Canvas/Stage/Overlays/Map/Floor1/Floor1Label |
| Floor2 | Button | UIFlow/Canvas/Stage/Overlays/Map/Floor2 |
| Floor2Label | Text | UIFlow/Canvas/Stage/Overlays/Map/Floor2/Floor2Label |
| Details | Text | UIFlow/Canvas/Stage/Overlays/Map/Details |
| Hint | Text | UIFlow/Canvas/Stage/Overlays/Map/Hint |
| Enter | Button | UIFlow/Canvas/Stage/Overlays/Map/Enter |
| EnterLabel | Text | UIFlow/Canvas/Stage/Overlays/Map/Enter/EnterLabel |
| TooltipPanel | RectTransform | UIFlow/Canvas/Stage/Overlays/Map/TooltipPanel |

## Journal

| 控件 ID | 组件 | 对象路径 |
|---|---|---|
| Close | Button | UIFlow/Canvas/Stage/Overlays/Journal/Close |
| CloseLabel | Text | UIFlow/Canvas/Stage/Overlays/Journal/Close/CloseLabel |
| Tab0 | Button | UIFlow/Canvas/Stage/Overlays/Journal/Tab0 |
| Tab0Label | Text | UIFlow/Canvas/Stage/Overlays/Journal/Tab0/Tab0Label |
| Tab1 | Button | UIFlow/Canvas/Stage/Overlays/Journal/Tab1 |
| Tab1Label | Text | UIFlow/Canvas/Stage/Overlays/Journal/Tab1/Tab1Label |
| Tab2 | Button | UIFlow/Canvas/Stage/Overlays/Journal/Tab2 |
| Tab2Label | Text | UIFlow/Canvas/Stage/Overlays/Journal/Tab2/Tab2Label |
| Category | Text | UIFlow/Canvas/Stage/Overlays/Journal/Category |
| PageNumber | Text | UIFlow/Canvas/Stage/Overlays/Journal/PageNumber |
| Empty | Text | UIFlow/Canvas/Stage/Overlays/Journal/Empty |
| LeftCards | RectTransform | UIFlow/Canvas/Stage/Overlays/Journal/LeftCards |
| RightCards | RectTransform | UIFlow/Canvas/Stage/Overlays/Journal/RightCards |
| Details | RectTransform | UIFlow/Canvas/Stage/Overlays/Journal/Details |
| DetailTitle | Text | UIFlow/Canvas/Stage/Overlays/Journal/Details/DetailTitle |
| DetailBody | Text | UIFlow/Canvas/Stage/Overlays/Journal/Details/DetailScroll/DetailBody |
| Previous | Button | UIFlow/Canvas/Stage/Overlays/Journal/Previous |
| PreviousLabel | Text | UIFlow/Canvas/Stage/Overlays/Journal/Previous/PreviousLabel |
| Next | Button | UIFlow/Canvas/Stage/Overlays/Journal/Next |
| NextLabel | Text | UIFlow/Canvas/Stage/Overlays/Journal/Next/NextLabel |
| Timeline | Button | UIFlow/Canvas/Stage/Overlays/Journal/Timeline |
| TimelineLabel | Text | UIFlow/Canvas/Stage/Overlays/Journal/Timeline/TimelineLabel |
| FlipSheet | RectTransform | UIFlow/Canvas/Stage/Overlays/Journal/FlipSheet |
| CharacterPath | Button | UIFlow/Canvas/Stage/Overlays/Journal/CharacterPath |
| CharacterPathLabel | Text | UIFlow/Canvas/Stage/Overlays/Journal/CharacterPath/Label |
| CharacterDecoration | RectTransform | UIFlow/Canvas/Stage/Overlays/Journal/Details/CharacterDecoration |
| DetailHead | RawImage | UIFlow/Canvas/Stage/Overlays/Journal/Details/CharacterDecoration/Head |
| RecordDecoration | RectTransform | UIFlow/Canvas/Stage/Overlays/Journal/Details/RecordDecoration |
| DetailScroll | ScrollRect | UIFlow/Canvas/Stage/Overlays/Journal/Details/DetailScroll |
| CharacterPortrait | Image | UIFlow/Canvas/Stage/Overlays/Journal/CharacterPortrait |

## Acquire

| 控件 ID | 组件 | 对象路径 |
|---|---|---|
| Heading | Text | UIFlow/Canvas/Stage/Overlays/Acquire/Heading |
| Icon | Text | UIFlow/Canvas/Stage/Overlays/Acquire/Icon |
| Name | Text | UIFlow/Canvas/Stage/Overlays/Acquire/Name |
| Body | Text | UIFlow/Canvas/Stage/Overlays/Acquire/Body |
| Filed | Text | UIFlow/Canvas/Stage/Overlays/Acquire/Filed |
| Confirm | Button | UIFlow/Canvas/Stage/Overlays/Acquire/Confirm |
| ConfirmLabel | Text | UIFlow/Canvas/Stage/Overlays/Acquire/Confirm/ConfirmLabel |
| ItemArt | RawImage | UIFlow/Canvas/Stage/Overlays/Acquire/IconBack/ItemArt |

## Letter

| 控件 ID | 组件 | 对象路径 |
|---|---|---|
| Body | Text | UIFlow/Canvas/Stage/Overlays/Letter/Body |
| Close | Button | UIFlow/Canvas/Stage/Overlays/Letter/Close |
| CloseLabel | Text | UIFlow/Canvas/Stage/Overlays/Letter/Close/CloseLabel |
| ItemArt | RawImage | UIFlow/Canvas/Stage/Overlays/Letter/ItemArt |

## Settings

| 控件 ID | 组件 | 对象路径 |
|---|---|---|
| Heading | Text | UIFlow/Canvas/Stage/Overlays/Settings/Heading |
| SpeedLabel | Text | UIFlow/Canvas/Stage/Overlays/Settings/SpeedLabel |
| Speed | Slider | UIFlow/Canvas/Stage/Overlays/Settings/Speed |
| SpeedValue | Text | UIFlow/Canvas/Stage/Overlays/Settings/SpeedValue |
| VolumeLabel | Text | UIFlow/Canvas/Stage/Overlays/Settings/VolumeLabel |
| Volume | Slider | UIFlow/Canvas/Stage/Overlays/Settings/Volume |
| Motion | Button | UIFlow/Canvas/Stage/Overlays/Settings/Motion |
| MotionLabel | Text | UIFlow/Canvas/Stage/Overlays/Settings/Motion/MotionLabel |
| Title | Button | UIFlow/Canvas/Stage/Overlays/Settings/Title |
| TitleLabel | Text | UIFlow/Canvas/Stage/Overlays/Settings/Title/TitleLabel |
| Close | Button | UIFlow/Canvas/Stage/Overlays/Settings/Close |
| CloseLabel | Text | UIFlow/Canvas/Stage/Overlays/Settings/Close/CloseLabel |
| SaveGame | Button | UIFlow/Canvas/Stage/Overlays/Settings/SaveGame |
| SaveGameLabel | Text | UIFlow/Canvas/Stage/Overlays/Settings/SaveGame/Label |
| LoadGame | Button | UIFlow/Canvas/Stage/Overlays/Settings/LoadGame |
| LoadGameLabel | Text | UIFlow/Canvas/Stage/Overlays/Settings/LoadGame/Label |
| EffectsVolume | Slider | UIFlow/Canvas/Stage/Overlays/Settings/SoundEffect |
| ExitGame | Button | UIFlow/Canvas/Stage/Overlays/Settings/ExitGame |

## Confirm

| 控件 ID | 组件 | 对象路径 |
|---|---|---|
| Body | Text | UIFlow/Canvas/Stage/Overlays/Confirm/Body |
| Cancel | Button | UIFlow/Canvas/Stage/Overlays/Confirm/Cancel |
| CancelLabel | Text | UIFlow/Canvas/Stage/Overlays/Confirm/Cancel/CancelLabel |
| Confirm | Button | UIFlow/Canvas/Stage/Overlays/Confirm/Confirm |
| ConfirmLabel | Text | UIFlow/Canvas/Stage/Overlays/Confirm/Confirm/ConfirmLabel |

## Path

| 控件 ID | 组件 | 对象路径 |
|---|---|---|
| Recordings | Button | UIFlow/Canvas/Stage/Overlays/Path/Recordings |
| RecordingsLabel | Text | UIFlow/Canvas/Stage/Overlays/Path/Recordings/RecordingsLabel |
| Anchors | Button | UIFlow/Canvas/Stage/Overlays/Path/Anchors |
| AnchorsLabel | Text | UIFlow/Canvas/Stage/Overlays/Path/Anchors/AnchorsLabel |
| Space | Button | UIFlow/Canvas/Stage/Overlays/Path/Space |
| SpaceLabel | Text | UIFlow/Canvas/Stage/Overlays/Path/Space/SpaceLabel |
| PreviousPuzzle | Button | UIFlow/Canvas/Stage/Overlays/Path/PreviousPuzzle |
| PreviousPuzzleLabel | Text | UIFlow/Canvas/Stage/Overlays/Path/PreviousPuzzle/PreviousPuzzleLabel |
| PuzzleName | Text | UIFlow/Canvas/Stage/Overlays/Path/PuzzleName |
| NextPuzzle | Button | UIFlow/Canvas/Stage/Overlays/Path/NextPuzzle |
| NextPuzzleLabel | Text | UIFlow/Canvas/Stage/Overlays/Path/NextPuzzle/NextPuzzleLabel |
| List | RectTransform | UIFlow/Canvas/Stage/Overlays/Path/ListScroll/Viewport/List |
| ListScroll | ScrollRect | UIFlow/Canvas/Stage/Overlays/Path/ListScroll |
| FloorImage | RawImage | UIFlow/Canvas/Stage/Overlays/Path/MapViewport/Rooms |
| Rooms | RectTransform | UIFlow/Canvas/Stage/Overlays/Path/MapViewport/Rooms |
| Tooltip | Text | UIFlow/Canvas/Stage/Overlays/Path/TooltipPanel/Tooltip |
| Details | Text | UIFlow/Canvas/Stage/Overlays/Path/Details |
| Floor1 | Button | UIFlow/Canvas/Stage/Overlays/Path/Floor1 |
| Floor1Label | Text | UIFlow/Canvas/Stage/Overlays/Path/Floor1/Floor1Label |
| Floor2 | Button | UIFlow/Canvas/Stage/Overlays/Path/Floor2 |
| Floor2Label | Text | UIFlow/Canvas/Stage/Overlays/Path/Floor2/Floor2Label |
| Verify | Button | UIFlow/Canvas/Stage/Overlays/Path/Verify |
| VerifyLabel | Text | UIFlow/Canvas/Stage/Overlays/Path/Verify/VerifyLabel |
| TooltipPanel | RectTransform | UIFlow/Canvas/Stage/Overlays/Path/TooltipPanel |
| Close | Button | UIFlow/Canvas/Stage/Overlays/Path/Close |
| CloseLabel | Text | UIFlow/Canvas/Stage/Overlays/Path/Close/Label |
| ResetView | Button | UIFlow/Canvas/Stage/Overlays/Path/ResetView |
| ResetViewLabel | Text | UIFlow/Canvas/Stage/Overlays/Path/ResetView/Label |
| MapPan | V24MapPan | UIFlow/Canvas/Stage/Overlays/Path/MapViewport |
| ZoomIn | Button | UIFlow/Canvas/Stage/Overlays/Path/ZoomIn |
| ZoomOut | Button | UIFlow/Canvas/Stage/Overlays/Path/ZoomOut |
| Hypothesis | InputField | UIFlow/Canvas/Stage/Overlays/Path/Hypothesis |

## Burn

| 控件 ID | 组件 | 对象路径 |
|---|---|---|
| Heading | Text | UIFlow/Canvas/Stage/Overlays/Burn/Heading |
| Close | Button | UIFlow/Canvas/Stage/Overlays/Burn/Close |
| CloseLabel | Text | UIFlow/Canvas/Stage/Overlays/Burn/Close/CloseLabel |
| FloorImage | RawImage | UIFlow/Canvas/Stage/Overlays/Burn/FloorImage |
| Rooms | RectTransform | UIFlow/Canvas/Stage/Overlays/Burn/Rooms |
| Tooltip | Text | UIFlow/Canvas/Stage/Overlays/Burn/TooltipPanel/Tooltip |
| Floor1 | Button | UIFlow/Canvas/Stage/Overlays/Burn/Floor1 |
| Floor1Label | Text | UIFlow/Canvas/Stage/Overlays/Burn/Floor1/Floor1Label |
| Floor2 | Button | UIFlow/Canvas/Stage/Overlays/Burn/Floor2 |
| Floor2Label | Text | UIFlow/Canvas/Stage/Overlays/Burn/Floor2/Floor2Label |
| Details | Text | UIFlow/Canvas/Stage/Overlays/Burn/Details |
| Hint | Text | UIFlow/Canvas/Stage/Overlays/Burn/Hint |
| Enter | Button | UIFlow/Canvas/Stage/Overlays/Burn/Enter |
| EnterLabel | Text | UIFlow/Canvas/Stage/Overlays/Burn/Enter/EnterLabel |
| TooltipPanel | RectTransform | UIFlow/Canvas/Stage/Overlays/Burn/TooltipPanel |
| Floor3 | Button | UIFlow/Canvas/Stage/Overlays/Burn/Floor3 |
| Floor3Label | Text | UIFlow/Canvas/Stage/Overlays/Burn/Floor3/Label |
| List | RectTransform | UIFlow/Canvas/Stage/Overlays/Burn/FilesScroll/Viewport/List |
| Burn | Button | UIFlow/Canvas/Stage/Overlays/Burn/Burn |
| BurnLabel | Text | UIFlow/Canvas/Stage/Overlays/Burn/Burn/Label |

## TapeSelect

| 控件 ID | 组件 | 对象路径 |
|---|---|---|
| Heading | Text | UIFlow/Canvas/Stage/Overlays/TapeSelect/Heading |
| Close | Button | UIFlow/Canvas/Stage/Overlays/TapeSelect/Close |
| CloseLabel | Text | UIFlow/Canvas/Stage/Overlays/TapeSelect/Close/CloseLabel |
| FloorImage | RawImage | UIFlow/Canvas/Stage/Overlays/TapeSelect/FloorImage |
| Rooms | RectTransform | UIFlow/Canvas/Stage/Overlays/TapeSelect/Rooms |
| Tooltip | Text | UIFlow/Canvas/Stage/Overlays/TapeSelect/TooltipPanel/Tooltip |
| Floor1 | Button | UIFlow/Canvas/Stage/Overlays/TapeSelect/Floor1 |
| Floor1Label | Text | UIFlow/Canvas/Stage/Overlays/TapeSelect/Floor1/Floor1Label |
| Floor2 | Button | UIFlow/Canvas/Stage/Overlays/TapeSelect/Floor2 |
| Floor2Label | Text | UIFlow/Canvas/Stage/Overlays/TapeSelect/Floor2/Floor2Label |
| Details | Text | UIFlow/Canvas/Stage/Overlays/TapeSelect/Details |
| Hint | Text | UIFlow/Canvas/Stage/Overlays/TapeSelect/Hint |
| Enter | Button | UIFlow/Canvas/Stage/Overlays/TapeSelect/Enter |
| EnterLabel | Text | UIFlow/Canvas/Stage/Overlays/TapeSelect/Enter/EnterLabel |
| TooltipPanel | RectTransform | UIFlow/Canvas/Stage/Overlays/TapeSelect/TooltipPanel |
| Floor3 | Button | UIFlow/Canvas/Stage/Overlays/TapeSelect/Floor3 |
| Floor3Label | Text | UIFlow/Canvas/Stage/Overlays/TapeSelect/Floor3/Label |
| List | RectTransform | UIFlow/Canvas/Stage/Overlays/TapeSelect/FilesScroll/Viewport/List |
| Burn | Button | UIFlow/Canvas/Stage/Overlays/TapeSelect/Burn |
| BurnLabel | Text | UIFlow/Canvas/Stage/Overlays/TapeSelect/Burn/Label |

## Credits

| 控件 ID | 组件 | 对象路径 |
|---|---|---|
| Close | Button | UIFlow/Canvas/Stage/Overlays/Credits/Close |

## Hud

| 控件 ID | 组件 | 对象路径 |
|---|---|---|
| Location | Text | UIFlow/Canvas/Stage/Hud/bg_location/Location |
| Back | Button | UIFlow/Canvas/Stage/Hud/Back |
| BackLabel | Text | UIFlow/Canvas/Stage/Hud/Back/BackLabel |
| Map | Button | UIFlow/Canvas/Stage/Hud/Map |
| MapLabel | Text | UIFlow/Canvas/Stage/Hud/Map/MapLabel |
| Journal | Button | UIFlow/Canvas/Stage/Hud/Journal |
| JournalLabel | Text | UIFlow/Canvas/Stage/Hud/Journal/JournalLabel |
| Settings | Button | UIFlow/Canvas/Stage/Hud/Settings |
| SettingsLabel | Text | UIFlow/Canvas/Stage/Hud/Settings/SettingsLabel |
| CurrentTime | Text | UIFlow/Canvas/Stage/Hud/bg_location/CurrentTime |
| LocationGroup | RectTransform | UIFlow/Canvas/Stage/Hud/bg_location |

## Toast

| 控件 ID | 组件 | 对象路径 |
|---|---|---|
| Body | Text | UIFlow/Canvas/Stage/Toast/Body |

## 列表模板

FlowCard：Button、Image、CastleUiView、FlowDrag、FlowPointer、CanvasGroup。FlowTrack：参考／移除按钮及偏移 Slider。DialogueLine：TMP、FlowTextLink、V24LineHit。PathLine：Image。RecordingFolder：可展开设备文件夹。RecordingFile：缩进文件与状态图标。RecordingLine：整条 Button、时间、说话人、正文、标记菱形与分隔线。模板资源均可直接编辑。

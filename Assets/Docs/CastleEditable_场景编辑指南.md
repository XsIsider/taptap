# CastleEditable 场景编辑指南

本次已经完成场景 UI 迁移。打开 `Assets/Castle/Scenes/CastleEditable.unity`，等待 Unity 编译完成后点击 Play。所有固定界面已保存为场景对象；调查册根据记录数量实例化 `Assets/Castle/Prefabs/JournalCard.prefab`。

## 场景对象树

```text
CastleEditable
├─ Main Camera                         Camera、AudioListener
├─ EventSystem                         EventSystem、StandaloneInputModule
├─ CastleGame                          CastleGame、AudioSource
└─ Canvas                              Canvas、CanvasScaler、GraphicRaycaster、CastleSceneUi
   └─ Stage                            RectTransform，1600 × 900
      ├─ Pages
      │  ├─ Title                      标题 / 新游戏 / 继续
      │  ├─ Outside                    古堡外 / 邀请函入口
      │  ├─ Invitation                 邀请函
      │  ├─ Collected                  收纳确认
      │  ├─ Lounge                     女爵对话
      │  ├─ Room                       中控室 / 其他房间
      │  ├─ Browse                     录音 / 摘录 / 标记 / 文字回放
      │  ├─ Align                      两条轨道 / 校时 / 文本对照
      │  ├─ Sound                      空间标记 / 地图 / 历史门状态
      │  ├─ Event                      已确认声音事件
      │  ├─ Identity                   身份与证据关联
      │  ├─ Route                      三节点人物路线
      │  ├─ Final                      最后一条物证关系
      │  ├─ Truth                      波形 / 三段重建叙述
      │  ├─ Choice                     结局选择
      │  └─ Ending                     结局
      ├─ SceneHud                      地点 / 时间 / 地图 / 调查册 / 设置
      ├─ Overlays                      Image，拦截底层点击
      │  ├─ Navigation                 两层导航地图
      │  ├─ Settings                   字号 / 音量 / 动态效果
      │  ├─ Journal                    人物 / 道具 / 事件格子
      │  ├─ JournalDetail              条目详情 / 返回
      │  ├─ InvitationArchive          调查册内邀请函
      │  └─ Message                    确认 / 取消对话框
      └─ Toast                         提示文字
```

每个页面、每个覆盖层和 SceneHud 均挂 `CastleUiView`。展开它的 **Controls**，每项都有 **Id** 和 **Target**。Id 是程序约定，Target 是场景组件引用；已全部绑定，不需要再手动接一次。

详细的对象路径、Id、目标组件见同目录 `CastleEditable_绑定清单.md`。

## 怎么修改界面

快捷方法：选中 **Canvas**，在 CastleSceneUi 的 Inspector 顶部选择 **编辑时预览页面**，点击 **显示所选页面（编辑模式）**。它只切换现有对象的显隐，然后你即可展开对应页面编辑。下面也列出手动切换方法。

1. 退出 Play，在 Hierarchy 展开 `Canvas/Stage/Pages`。
2. 默认只有 Title 激活。要编辑工作台，取消 Title 的激活，勾选 Browse、Align 或 Sound 中的一个。使用 Scene 的 2D 模式，选中页面按 F 聚焦；Game 视图可看整体。
3. 直接调整子对象的 RectTransform、Image/RawImage、Text 字体与颜色、Button 过渡色。Ctrl+S 保存场景。运行后页面控制器只更新动态数据、显隐和交互状态，保留静态布局。
4. 编辑调查册时，勾选 Overlays 和其中一个面板。编辑完可关闭覆盖层；运行初始化也会关闭它们。
5. 编辑卡片外观，双击 `JournalCard.prefab` 进入 Prefab 模式。根对象挂 Image、Button、CastleJournalCardView，三个 Text 子对象分别是类别、标题、摘要。格子间距和列数在 Journal 的滚动 Content 上修改。

文字使用 **UI → Legacy → Text**，按钮是 **uGUI Button**，不是 UI Toolkit，也不是 TMP。替换成 TMP 需要同步改脚本类型。普通 Text 建议关闭 Raycast Target，Button 的 Image 保持开启。

## 自己创建/替换按钮如何重新绑定

以录音页面的“标记完成”为例：

1. 在 Browse 下创建 **UI → Button (Legacy)**，调整样式和位置。
2. Button 根对象需要 RectTransform、Image、Button；子对象需要 RectTransform、CanvasRenderer、Text。Unity 会自动添加 CanvasRenderer。
3. 选中 Browse 的 CastleUiView，在 Controls 中找到 `MarkCompleteButton`，把新按钮的 **Button 组件**拖到 Target；找到 `MarkCompleteButtonLabel`，把文字子对象的 **Text 组件**拖到 Target。
4. 删除旧按钮或停用它。Id 保持原样。运行后脚本自动接上“进入锚点页”。

不要再给 Button 的 Inspector → On Click 添加同一动作，避免执行两次。当前 On Click 空白是正常的，逻辑在运行时绑定到已保存的组件引用。

如果要新增一种原来没有的操作，需要同时新增 Controls 项及相应 `B(view, "新Id", 回调)` 逻辑。只改对象名称不会破坏绑定；删除或替换组件后必须修复 Target。

## 哪些组件挂在哪

| 对象 | 需要挂载/绑定 | 作用 |
|---|---|---|
| CastleGame | CastleGame，database → Database.asset，ui → Canvas 的 CastleSceneUi；AudioSource | 玩法、存档、页面状态及点击音效 |
| Canvas | Canvas、CanvasScaler、GraphicRaycaster、CastleSceneUi | UI 渲染和各页面/模板的总引用 |
| 每个页面或覆盖层 | CastleUiView，Id 和 Controls | 将语义 Id 绑定到你创建的具体组件 |
| 普通按钮 | Image、Button；子对象 Text | 接收点击及显示标题；不必每个按钮另挂业务脚本 |
| 普通文字 | Text、CanvasRenderer | 显示说明或动态数据 |
| 滑块 | Slider、Rail/Image、Handle/Image；Slider.handleRect 和 targetGraphic | 字号、音量、播放进度、时间校正 |
| 滚动区 | ScrollRect；Viewport/Image/RectMask2D；Content；Scrollbar | 录音、对照文字、调查册列表和详情 |
| Align/AlignmentScroll/Viewport/Content/TargetTrack | Image、CastleTimelineDrag | 拖动目标轨道调整校正量 |
| Sound/Map、Route/Map、Navigation/Map | CastleMapView | 地图底图、房间按钮、标记位置、走廊路点、预置连线 |
| 地图每个 Room 对象 | Image、Button；RoomName/Text；MarkerAnchor/RectTransform | 可直接在场景移动热点；MarkerAnchor 跟随房间 |
| Journal 的 Content | GridLayoutGroup、ContentSizeFitter | 卡片自动排列和纵向扩展 |
| JournalCard.prefab | Image、Button、CastleJournalCardView + 三个 Text | 可复用的调查册卡片模板 |
| EventSystem | EventSystem、StandaloneInputModule | 鼠标、键盘和 UI 事件 |

所有 CastleGame.*.cs 是**同一个 partial 类**，只在 CastleGame 根对象挂一次 CastleGame，不能把 Exploration、Workbench 等当成不同组件分别挂载。

## 本次修改对照

| 修改位置 | 修改内容 | 你后续在哪里改 |
|---|---|---|
| CastleEditable.unity（新增） | 16 个固定页面、6 个覆盖层、HUD、所有按钮/滑块/地图预存进场景 | Hierarchy / Inspector |
| CastleGame.cs | 去掉运行时创建/销毁固定 UI，改为显示切换、读取引用、重复刷新时只更新回调与数据 | 只有玩法/交互变化才改 C# |
| Exploration / Workbench / Resolution | 控件创建改为使用 CastleUiView 引用，保留流程与判断 | 静态排版在场景；动态文案仍由脚本更新 |
| SimpleUi | 格子卡片复用 Prefab，详情页复用；保存滚动位置 | 卡片改 Prefab，列表改 GridLayoutGroup |
| CastleSceneUi / CastleUiView（新增） | 可序列化总引用、稳定 Id、缺失绑定报错 | Inspector 中重新拖引用 |
| CastleMapView（新增） | 地图热点和路点可编辑，复用预置标记和连线 | 场景 Map 下的 Room、MarkerAnchor、Corridor |
| CastleJournalCardView（新增） | 卡片数据与点击行为 | Prefab 的组件引用 |
| CastleSceneUiBuilder（新增，Editor） | 仅在场景缺失时生成初始布局，正常 Play 不运行 | 已有场景不会被这个工具覆盖 |
| CastleProjectTools / Build Settings | 默认打开、构建新场景 | Tools → Castle → Open Editable Scene |
| CastlePrototype.unity | 旧入口保留；旧场景运行时跳转到 CastleEditable | 建议直接打开新场景编辑 |
| CastleState / CastleSave / Database.asset | 继续使用原规则、原存档格式和数据库 | 仍沿用现有数据编辑方式 |

创建工具是一次性搭建辅助。你保存后的场景才是实际界面来源。不要删除场景后运行创建工具，否则会从初始模板重建，丢失你后来做的场景排版（Git 中保存的版本仍可找回）。


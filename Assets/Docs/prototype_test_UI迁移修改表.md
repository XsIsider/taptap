> 历史设计草案：场景迁移现已实施。实际使用请参阅 [CastleEditable 场景编辑指南](CastleEditable_场景编辑指南.md) 和 [实际绑定清单](CastleEditable_绑定清单.md)。

# prototype_test：手工 UI 迁移修改表

适用：`prototype_test` / `03b0850` + 2026-10-09 工作区的简化工作台和调查册二级页。以下是待实施修改，不是已完成记录；本次不修改 `.cs` 或场景。对应对象、组件与引用：[prototype_test_UI对象树与组件绑定.md](prototype_test_UI对象树与组件绑定.md)。

目标：让你在 Unity 场景/prefab 中创建、排版所有视觉组件，CastleGame 只连接输入、刷新显示和调用原规则。保留第一版玩法、两种结局和 v1 存档，不套用 V2 架构。

## 1. 前置改动：先让手工物体不被删掉

源码位于 `Assets/Castle/Scripts/`，编辑器文件位于 `Assets/Castle/Editor/`。优先级 P0 为场景接管前置，P1 为功能页迁移必需。

| 优先级 | 文件/方法 | 当前行为 | 应修改为 | 验收点 |
|---|---|---|---|---|
| P0 | **新增 CastleSceneUi.cs** | 没有场景 UI 引用表 | MonoBehaviour，按对象树文档提供公共区、16 个 ScreenId 页面、6 类覆盖面板及控件字段组；验证必填引用和数组长度 | 所有 inactive 页也可通过序列化引用访问；漏绑明确报错，不悄悄生成备用 UI |
| P0 | CastleGame.cs / 字段、Awake | new Canvas/Letterbox/Stage/EventSystem，AddComponent AudioSource | 新增 `[SerializeField] CastleSceneUi ui`；从 ui 获取 root/AudioSource；移除这些创建语句；保留数据库、字体、存档初始化及点击音效 | Play 后不出现第二套 Canvas/EventSystem/AudioSource |
| P0 | CastleGame.cs / **新增 BindUi、UnbindUi** | Button/Slider 工厂在创建时接线 | 静态监听集中绑定一次；场景销毁时移除自己注册的监听，清空拖动委托；复用卡片 Bind 前解绑旧监听 | 打开/关闭十次仍只执行一次；保留点击提示音，不再依赖旧 Button 工厂的声音封装 |
| P0 | CastleGame.cs / Render | 循环 Destroy(root.GetChild(i))，然后重建整个页面 | 删除遍历销毁；将“页面可见性切换”与“当前页刷新”拆开；只修改 Text/RawImage/interactable/颜色/显隐 | 手工摆放的物体不丢失，普通刷新不重置滚动或关闭弹窗 |
| P0 | CastleGame.cs / Go | 设置 page、playing=false、overlay=null，保存再 Render | 保留 page/resume/Save 规则；显式关闭当前覆盖层，再切换固定页与 Shell；不能只把 overlay 变量置空 | 退出设置去标题后遮罩确实消失，不挡标题按钮 |
| P0 | CastleGame.cs / Overlay、CloseOverlay | 每次新建遮罩、销毁旧覆盖层 | 固定 OverlayRoot 和六个互斥面板；设 `overlayOpen` 与 `overlayBack`；关闭时隐藏、清掉临时确认动作，不 Destroy | 调查册/设置/确认框不叠出第二套；返回行为与现版一致 |
| P0 | CastleGame.cs / Update | `if (overlay) return`，Esc 判断 overlay 是否存在 | 常驻引用永远存在，改为 overlayOpen/activeSelf 判断；Esc 先处理 overlayBack，再 CloseOverlay；保留 J/M/空格/方向键 | 遮罩关闭后播放恢复可操作；打开时底层快捷键不穿透 |
| P0 | CastleGame.cs / Toast、Update 超时 | 创建提示条，超时 Destroy | 引用固定 ToastRoot/Text；更新文案和截止时间，超时 SetActive(false)；全提示条关闭射线 | 通知消失后仍可再次显示；不会删除手工组件或挡按钮 |
| P0 | CastleGame.cs / Rect、Box、Label、Button、Picture、Slider、Card | 创建物体、AddComponent、布局、接线混在工厂 | 所有固定 UI 调用移除；改成对绑定控件赋值；保留资源加载缓存可选；装饰线框在编辑器搭 | 没有残留工厂路径重新创建固定界面 |
| P0 | CastleGame.cs / Render 的临时字段清理 | waveBars.Clear，销毁对象后自然失效 | 根据当前页切换引用，Truth 显示时装载预置78条；playLabel/progressSlider/offsetLabel 等只指向有效页；不要依赖销毁引用变 null | 切页后动画不更新错误页；返回后仍能播放/调节 |

## 2. 探索、导航、设置与消息

| 文件/方法 | 手工 UI 引用 | 修改内容 | 保留行为 |
|---|---|---|---|
| CastleGame.Exploration.cs / Title | TitlePage 全部按钮/图文 | 改成刷新背景、继续按钮状态；将原 lambda 抽成命名回调便于只绑定一次 | 新游戏有进度时确认后新建 CastleState，清 undo，进入 Outside；取消不变；继续进入 state.resume |
| Outside / Invitation / Collected | 三页背景和按钮 | 按 invitation 显示展开/入堡按钮；沿用资源 Held/Outside/Invitation/Collected | 收起邀请函先更新状态，确认收纳回 Outside；入堡设置房间101与时间1130 |
| Hud | SceneHud | 只在 Outside/Lounge/Room 显示；刷新房间、时钟、地图解锁 | 地图仅获得后开放；J 调查册、设置入口保留 |
| Lounge / AdvanceDialogue | 三个 LineText、进度文字、Continue/Map | 循环填最多最近三句，隐藏空行；根据状态替换 Continue 行为 | CastleRules.AdvanceDialogue、Save；对白结束后按钮打开地图；空格继续仍有效 |
| Room | ControlRoot / OtherRoomRoot | 用 state.room 决定背景与子组；保留其他房间说明文字 | 工作台入口设置 workbench，Discover(103,104,108)，Go(Browse) |
| Navigation / DrawNavigation | NavigationPanel、RoomButtonRefs[]、楼层与 Enter/Wait/Close | 通过房间ID绑定手工按钮，刷新遮罩/已探索/当前房间；换楼层只显隐分组；Enter/Wait 读取当前 selectedRoom | CanVisit、Travel、等待开放、移动耗时2分钟、楼梯切层；分析地图点击不能误调用此回调 |
| Settings | 字号/音量滑条、动态/标题/继续按钮 | 初始化 Slider 使用 SetValueWithoutNotify；字号同步现有阅读文本；音量仍转0～1；按钮 Label 随状态刷新 | readingSize 20～28、volume、reduceMotion 都在 state；Changed/Save 不删 |
| CastleGame.cs / Message、Check | MessagePanel | 固定 Title/Body/Confirm/Cancel；运行时保存一个当前 Action；每次打开替换，不累积 AddListener | confirm==null 时只有继续调查；错误提示后草稿仍保留；制作名单共用此面板 |
| 所有背景与字体 | ui 中的 RawImage/Text | Texture2D 使用 RawImage.texture；Legacy Text 使用 Chinese.otf | 不把 V2 的 ChineseTMP.asset、TMP_Text 类型带进来 |

## 3. 中控工作台迁移

| 文件/方法 | 对应物体 | 需要修改 | 重点检查 |
|---|---|---|---|
| CastleGame.SimpleUi.cs / WorkbenchFrame | WorkbenchShell | 停止创建背景、木色罩、绿板、边框、导航；切页只更新三个 Tab 的颜色 | Browse/Align/Sound 共用一个 Shell；其余页面不显示 |
| BrowseSimple / ChooseFile | BrowsePage | 四个文件按钮固定；保留 file=0/1/2 的选择语义和 transcriptScroll 重置；分开显示设备正文和随身完整对话 | Rec03 仍是待定位提示；Personal 受 recorded 控制 |
| BrowseSimple 的四行循环 | 4个 RowRefs | 预建四行；每行保存 SelectButton/Text、ActionButton/Text 引用；刷新当前 file 对应文案和身份名 | 0/2/3 摘录，1 标记；不得把摘录操作错误绑定到声音标记 |
| ToggleAnchor | Row1.AnchorButton | 保留规则操作，仅 Render 改局部刷新，包括文件按钮/候选锚点状态 | 移除锚点同步移除候选并令 aligned/pathSaved=false；confirmedEvent 不变 |
| BrowseSimple 的播放/进度回调、CastleGame.Update | PlaybackRoot | 绑定固定 progressSlider/playLabel/transcriptButtons；游标推进和高亮局部刷新，禁止每换一句重建全页 | 暂停、结束和定位正常；滑条回写不触发循环；滚动位置保留 |
| AlignSimple | AlignPage | 候选按钮、轨道、比较文字、状态、检查/房间按钮全部引用预建控件 | 已标记的锚点才可见；候选组选中状态来自 pairA/pairB |
| CastleGame.Workbench.cs / Track | ReferenceTrack / TargetTrack | 不再生成轨道、Marker 或 AddComponent；从绑定中获取 TargetTrack 上的 CastleTimelineDrag | 参考轨无拖动组件；目标轨 Image 可射线命中，装饰图不抢拖动 |
| SetOffsetLive、CastleTimelineDrag.onDrag | OffsetSlider、TargetMarker、OffsetText | 保留 CastleRules.ChangeTime；把固定936/20/-62坐标改为绑定 Rail 的实际范围和转换；绑定 drag.width 为相同本地宽度 | -180～360 秒一致；方向键每次1秒；拖动和Slider得到相同校正量 |
| SoundSimple | SoundPage | 绑定3个空间标记、信息框、门状态、证据、撤销、保存/验证；回调只改状态并刷新 | 选择设备标记只查看原档案；选声源S后才可放置 |
| AnalysisMap(bool route) | SoundMap / RouteMap | 拆成接受具体 MapRefs 和用途参数的刷新方法；不要让两个页面引用同一个地图实例；按RoomId连接按钮，房间位置以场景为准 | 楼层切换、两种地图行为、正确的 source/route 赋值 |
| DrawConnection / PathFor / Line | 两套地图各自的 MarkerAnchor、走廊路点与连线 | 按手工地图局部坐标画线；保留沿走廊路径、实线/断续线；线段使用手工模板池；更新前隐藏上一批 | 房间位置改动后连接正确；不穿墙直连；不重复堆线；线条关闭射线 |
| Remember / UndoDraft | UndoButton | 保留 JSON 草稿栈，只把 Render 改刷新地图和信息框 | source/door/doorEvidence/route 恢复，pathSaved=false，不覆盖已确认快照 |
| ScrollArea / Border | 所有预建 ScrollView/边框 | 停止创建组件；ScrollRect 绑定 Viewport/Content/Scrollbar，手柄sizeDelta零；固定线框在编辑器做 | 手柄不会超过轨道；无需滚动时 AutoHide；文字不超出裁剪区 |

## 4. 调查册格子与二级页迁移

| 文件/方法 | 需要修改的内容 | 对象/组件绑定 | 验收点 |
|---|---|---|---|
| **新增 CastleJournalCardView.cs** | 保存卡片 Button 和3个 Text；提供 Bind/Unbind，复用时覆盖旧字段并移除自己注册的回调 | 挂手工卡片 prefab 根，或每个预置卡片 | 点击的是当前条目，不是上次复用的条目 |
| SimpleUi.cs / JournalEntries | 基本保留条目筛选、稳定 Id、正文和后续动作 | 不挂组件；仍由 CastleGame 生成显示数据 | 只有人物/道具/事件；女爵完整对白和事件摘录没有丢失 |
| JournalGrid | 引用固定面板/三个Tab；生成数据后填卡片池；显示数量/空态；程序不再手工计算卡片x/y | GridLayoutGroup=3列；ContentSizeFitter 管高度；GridScroll、Content、CardPrefab/Cards[] | 无第四类；新增条目不互相遮挡，长列表可滚动 |
| JournalGrid 的滚动回调 | BindUi 只注册一次；打开列表时布局完成后恢复 journalScroll | 固定 ScrollRect | 从详情返回保留位置；换类别重置顶部；旧 inactive 列表不再写回滚动值 |
| JournalDetail | 三类复用一个详情面板；设置标题、分类、正文、当前动作；没有动作时清空并隐藏按钮 | 固定 Back/Close/Action、DetailScroll/BodyText | 打开不同类型不会残留上个条目的按钮行为 |
| JournalDetail 文本高度 | Legacy Text preferredHeight 计算后设置Body/Content，或统一用布局方案；保留阅读字号 | BodyText 使用 Wrap，正文按需要扩高 | 长对话全部可滚动阅读，不强制缩成很小字体 |
| OpenJournalInvitation | 固定 ArchivedInvitationPanel 切换；存当前详情上下文 | InvitationImage、BackToDetailButton | 返回同一条道具详情，Esc 行为一致 |
| Overlay / overlayBack | 用显式类型或当前活动面板管理返回层级；关闭/Go清掉旧回调 | 6面板互斥与全屏 Shield | 邀请函→详情→格子→关闭；确认弹窗接管输入 |
| confirmedRoute / 旧档保护 | 保留现有长度==3保护，不把空数组当已确认路线 | 不需要新组件 | 旧档无路线时打开事件列表不越界 |

## 5. 后续推理和结局也必须迁移

仅重做三张工作台页还不够：联合验证成功后会进入后续页面；这些函数若仍调用旧工厂，就又会创建 UI。

| CastleGame.Resolution.cs 方法 | 手工引用 | 行为保留 |
|---|---|---|
| EventView | EventPage、IdentityButton、BrowseButton | 展示 confirmedEvent 快照；有效记录才允许进入身份页 |
| Identity | 两个候选按钮、证据按钮、结果文字、SubmitButton | candidate、identityEvidence；VerifyIdentity；确认后按钮转向 Route |
| Route | 三个节点按钮、RouteMap、证据、撤销、核对/Final按钮 | selectedNode、routeEvidence、VerifyRoute、confirmedRoute 独立保存；不引入V2拖放 |
| FinalView | 三个固定时间线卡、摘要、ClaspButton、Submit | finalEvidence、CastleRules.Submit；成功进入 Truth |
| Truth | 78条预建 Image、叙述文字、进度和继续按钮 | 保留 truthIndex 0～2、reduceMotion 和原波形更新；每次重播前重置显示比例 |
| Choice | 两个结局按钮、调查册、返回还原 | state.ending 仍为现有两个值；Go(Ending) |
| Ending | 结局标题/正文、调查册/回Choice/回Title | 保留两种结局内容及返回结局前功能 |
| CastleGame.cs / Header、Hint | AnalysisShell 固定头尾 | 改为更新文字、导航和Hint，不生成；本次不要将16种ScreenId合并成错误的通用状态 |

## 6. 哪些层不用因手工 UI 重写

| 文件/资源 | 处理 |
|---|---|
| CastleState.cs / CastleState、EventRecord | 保留存档字段、ScreenId、枚举值；不要把 GameObject/Button 引用保存进去 |
| CastleState.cs / CastleRules | 保留 Align/VerifyEvent/VerifyIdentity/VerifyRoute/Submit/Travel 等判定，不在按钮回调复制答案逻辑 |
| CastleSave.cs | 保留 v1 JSON、.bak 和读写逻辑，本次不借UI迁移清除玩家进度 |
| CastleDatabase.cs、Resources/Castle/Database.asset | 保留剧情/房间配置和字段；手工房间按钮用稳定RoomId查配置 |
| 图片/字体/.meta | 继续引用原资源，保留GUID；只新增需要的手工prefab及其meta |

## 7. 最小接入示意：新增组件后 Inspector 才有字段

下列示意只展示标题页。完整实现需补全对象树中的字段、校验和所有回调，不能把片段当成完整可替换代码。

```csharp
// 新增 CastleSceneUi.cs（本次未创建），命名空间与当前工程一致。
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Castle
{
    [Serializable]
    public sealed class TitleUiRefs
    {
        public GameObject Root;
        public Button StartButton;
        public Button ContinueButton;
    }

    public sealed class CastleSceneUi : MonoBehaviour
    {
        public RectTransform Stage;
        public AudioSource Audio;
        public TitleUiRefs Title;
        // 补齐16页、公共Shell、覆盖层及控件字段组。
    }
}
```

```csharp
// 以下成员加到现有 CastleGame partial 类内部。
[SerializeField] private CastleSceneUi ui;

void BindUi()
{
    ui.Title.StartButton.onClick.AddListener(OnStartClicked);
    ui.Title.ContinueButton.onClick.AddListener(OnContinueClicked);
}

void OnContinueClicked()
{
    audioSource.PlayOneShot(clickSound, state.volume);
    Go(state.resume);
}

void OnStartClicked()
{
    audioSource.PlayOneShot(clickSound, state.volume);
    Action start = () => {
        state = new CastleState { started = true };
        undo.Clear(); file = 0; selectedLine = -1;
        Go(ScreenId.Outside);
    };
    if (state.started) Message("开始新的调查", "当前进度将被新游戏替换。", start);
    else start();
}
```

Awake 保留原状态加载后，先校验 ui，再 `root=ui.Stage; audioSource=ui.Audio;`，完成原点击音初始化后调用 BindUi 和新版 Render。OnDestroy 在销毁 clickSound 之前移除对应监听。不要在每次 Render 里 BindUi。

如果要求 Button.OnClick 在 Inspector 手动选方法：可把包装方法声明为 public，如 `public void OpenJournalFromUi(){ Journal(); }`，把 **Castle Game 上的 CastleGame 组件**拖进 OnClick。带枚举/上下文的动作最好包装成无参方法，例如 OpenRecordingsFromUi→Go(Browse)。此模式与 BindUi 对同一个按钮二选一；包装中也要保留声音和必要状态检查。不能把 `.SimpleUi.cs` 文件当独立组件拖入。

## 8. 场景、工具、回归需要同步修改

| 文件/工作项 | 要改什么 | 为什么 |
|---|---|---|
| Assets/Castle/Scenes/CastlePrototype.unity | 保存完整层级与 CastleGame.ui 引用 | Awake 不再兜底生成 UI 后，场景缺引用就无法启动 |
| 手工卡片/地图/线段 prefab | 创建并保留 .meta；全部内部引用拖好 | 重复项从作者制作的模板加载，避免恢复 AddComponent 工厂 |
| CastleProjectTools.Setup | 当前仅在场景不存在时生成 Camera+CastleGame；改为手工场景检查，缺失时明确报错，或从完整已维护模板创建 | 否则工具会产生一个缺少ui引用的不可运行场景 |
| CastleProjectTools.Validate / Build | 在原31项规则检查外，增加场景绑定、唯一EventSystem、缺组件、数组长度与prefab引用检查 | “规则通过”不代表手工UI已绑齐 |
| CastleGame.Smoke.cs / Click | 改为控件引用或稳定测试ID；保留卡片JournalCard:ID定位策略，避免按中文名字强耦合 | 场景物体可重命名；预置卡复用后测试必须指向当前数据 |
| CastleGame.Smoke.cs / Capture | Canvas仍放Castle Game子级或显式取ui.Canvas；波形、Scrollbar查找适配常驻inactive页面 | 隔离截图不应捕获隐藏页面或误报重复控件 |
| README/手工搭建说明 | 实施后才更新为“编辑模式可见UI” | 当前仍是运行时创建，不能提前声称完成 |

推荐验收顺序：

1. **结构**：Play 前能编辑 UI；Play 后固定物体数稳定；一个主 Canvas/一个EventSystem；漏绑清晰报错。
2. **输入**：反复开关页面不重复执行；弹窗屏蔽底层；关闭后恢复；按Esc符合返回链。
3. **工作台**：两个录音切换、播放/定位/摘录、锚点增删、拖动/Slider/方向键一致、门状态/撤销/草稿/联合验证。
4. **调查册**：三类格子、空态、长列表、详情回退位置、完整对白、邀请函及摘录；字数变多也能滚动。
5. **主流程与存档**：新游戏覆盖取消、继续旧档、身份/路线/物证、两种结局、已确认快照不被草稿覆盖。
6. **显示**：1600×900及窗口缩放，字号20～28；滚动条不越界；手工地图改坐标后连线依然正确。

完全不允许运行时 Instantiate 的项目，将调查册/线段实例化替换成预置池，额外验证容量不足时的分页或明确提示；不要静默丢失证据条目。


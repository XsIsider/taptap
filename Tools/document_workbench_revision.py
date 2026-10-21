"""Record the confirmed workbench, link and playback changes."""
from revise_ui_20261021 import ROOT

summary='''## 2026-10-21 人物路径、时间线与磁带页更新

- UI 美术继续只取 `Assets/Castle/Art/V24/UI整理`。人物路径实际在 `Overlays/Path`，保留原调查册入口及规则；按《中控-人物空间》调整边框、三页签、左侧人物时间标签、楼层栏、地图区域与提交按钮。新增 PersonPathLabel 模板和白色标记；底图与 Rooms 同属 MapContent，居中缩放/平移，清理动态房间时不销毁底图。PathMap 材质去除地图白底并支持视口裁切，悬停可查看房间说明。
- Timeline 按《结局界面1》保存十个固定 Slot0～Slot9、红色连线、声纹和白点；纸卡及金色角标均来自整理目录。线索改为底部横向 ScrollRect，新增 TimelineClue / TimelineSlot 模板。每次打开页面洗牌，拖放重绘保持当前顺序；不修改候选 ID、答案和草稿，拒绝把其他页面遗留选择写进排序槽。结局播放仍保留排序槽背景，禁用槽位交互，立绘/对白按《结局界面2》布置。
- 人物初始资料已解锁后，现场及回放正文中该人物的 entry/character 姓名链接恢复普通文字；灵感、道具和地址链接保留。配置原始链接不删除，新游戏仍可首次点击解锁。
- 正文 room/进入中控室地址只打开对应楼层地图，不预选目的地、不移动，玩家自行选房。邀请函和首日客房流程门继续沿用上一轮规则；末尾独立“进入中控室”操作保留。
- Tape 以女主当前房间和昼夜状态选择 Scene_update 背景，无立绘；复用现场 DialoguePanel 的固定布局、滚动区和通用 DialogueLine.prefab，正文改为姓名与对白同行，保留已播内容跟随及历史滚动。左上 HUD 返回箭头退出播放，停止音效/语音并回当前地点；普通磁带在女主房间退出即回女主房间。首次中控室的自动播放则回中控室并接续原剧情。
- 初次中控室 920016 / 620015 录音不再强制听完：导入器明确 AllowEarlyExit=true，规则只对真正 MustFinish 且未允许提前退出的关卡拦截；退出结算及后续 920017 去重保留，旧档等待状态直接兼容。
- 验证：Unity 随附 Roslyn 定向编译通过；实际 V24Engine 共 239 项断言通过（含此前流程门、姓名链接、提前退出和 100 组洗牌各两项校验）；固定绑定、全部 RectTransform 父子关系、底图清理安全性、Tape 通用面板及配置一致性静态检查通过，已检查源素材布局/叠图。**未进入 Unity Play，Shader 导入、最终渲染、实际拖放与播放仍需 Unity 验收；叠图不是 Unity 截图。** 未改玩家存档、未构建、未提交推送。

'''
for file in ['功能与UI实现说明.md','Assets/Docs/项目交接.md']:
    path=ROOT/file
    text=path.read_text(encoding='utf-8')
    if not text.startswith(summary.splitlines()[0]):
        path.write_text(summary+text,encoding='utf-8',newline='\n')

bindings='''## 2026-10-21 Path / Timeline / Tape 绑定更新

- Path 仍位于 UIFlow/Canvas/Stage/Overlays/Path；Backdrop 改为 UI整理/中控/人物推演/图层 1.png，新增 MapArea、FloorBar、SpaceUnderline、FloorUnderline。原 Close 改为左上返回箭头，旧调试式说明/缩放按钮/假设框隐藏，绑定保留。
- Path/MapViewport/MapContent 为 V24MapPan.Content，锚点/轴心居中；FloorImage、Rooms 为其同级子对象，位置零、1140×416，底图用 V24PathMap 材质。FloorImage / Rooms 的原绑定仍指向各自原组件。新增控制器 PathLabelPrefab、PersonPathMarkerIcon。
- Timeline/TimelineArt 为固定共用背景层，内含 ReferenceWave、ClueDivider、Connection1～9、WavePoint0～9、Slots/Slot0～9。原 Slots 绑定不变，新增 Slot0～9 绑定至各自 CastleUiView；槽位均保存到场景，不再运行创建/清空。
- Timeline/SortRoot/CluesScroll 改为 (420,-770)、760×105，水平滚动，内容为固定一行 GridLayoutGroup、HorizontalFit=PreferredSize；原纵向条隐藏并解除引用。控制器新增 TimelineCluePrefab。TimelineSlot / TimelineClue / PersonPathLabel 与 .meta 一起保存于 Prefabs/V24。
- Tape/DialoguePanel 从当前 Explore/DialoguePanel 复用布局保存；LinesScroll / Lines 绑定分别改指新面板的 DialogueScroll / Viewport/Dialogue，新增 DialoguePanel、Backdrop 绑定。旧 Tape 工作台边框、标签、旧列表等隐藏，Play / Progress / Playback / FileInfo 移至对白面板区域；退出继续使用 Hud/Back。
- 所有新固定绑定已静态核对；尚未完成 Unity 内实际渲染、Shader 与点击/拖放验收。

'''
path=ROOT/'UI对象树与绑定清单.md'
if not path.read_text(encoding='utf-8').startswith(bindings.splitlines()[0]):
    path.write_text(bindings+path.read_text(encoding='utf-8'),encoding='utf-8',newline='\n')
path=ROOT/'新会话必读.md'
text=path.read_text(encoding='utf-8')
addition='''## 2026-10-21 最新追加：Path / Timeline / Tape

- 人物路径按《中控-人物空间》改版；时间线按两张结局图改为十个固定槽位、红线/声纹及底部横向线索条。每次打开随机打乱，拖放重绘不重洗牌；UI 美术仍只取 UI整理。人物路径实际对象仍是 Overlays/Path，不是 Pages。
- 已解锁姓名取消正文超链接；地址只开地图、不预选或自动转场。磁带页改当前地点昼夜背景、无立绘、复用现场对白面板与 DialogueLine.prefab；左上箭头退出，停止音频。初次中控室 620015 可以提前退出并继续原 920017。
- C# 定向编译、239 项规则断言（包含 100 组洗牌）、固定绑定及层级静态检查通过，已检查源素材叠图；未 Unity Play，Shader/最终渲染/拖放/播放仍待验收。未改存档、构建或提交推送。详见最新根说明、项目交接和绑定清单。

'''
if addition.splitlines()[0] not in text:
    path.write_text(text.replace('## 2026-10-21 本机最新状态',addition+'## 2026-10-21 本机最新状态',1),encoding='utf-8',newline='\n')
path=ROOT/'Assets/Docs/Bug修复指南.md'
brief='''2026-10-21 追加：人物路径/时间线/磁带页按参考图改版，时间线底部线索打开时洗牌；已解锁姓名取消链接，地址只打开地图；首次中控室录音可提前退出。编译、姓名/退出/洗牌等规则及固定绑定检查通过，Unity 渲染与操作未验收。

'''
if not path.read_text(encoding='utf-8').startswith(brief.splitlines()[0]):
    path.write_text(brief+path.read_text(encoding='utf-8'),encoding='utf-8',newline='\n')
print('Root behavior notes, handoff, binding inventory and concise bug summary synchronized.')

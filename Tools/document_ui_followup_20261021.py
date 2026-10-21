"""Record the confirmed follow-up behavior and actual validation boundaries."""
from revise_ui_20261021 import ROOT

heading = '## 2026-10-21 九项 UI 追加修订'
body = '''

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

'''
for name in ['功能与UI实现说明.md', 'Assets/Docs/项目交接.md', 'UI对象树与绑定清单.md', 'Assets/Docs/Bug修复指南.md']:
    path = ROOT / name
    text = path.read_text(encoding='utf-8')
    if heading not in text:
        path.write_text(heading + body + text, encoding='utf-8', newline='\n')
path = ROOT / '新会话必读.md'
text = path.read_text(encoding='utf-8')
if heading not in text:
    text = text.replace('# 新会话必读\n', '# 新会话必读\n\n' + heading + body, 1)
    path.write_text(text, encoding='utf-8', newline='\n')
print('Updated implementation notes, handoff, binding inventory, bug guide and session entry.')

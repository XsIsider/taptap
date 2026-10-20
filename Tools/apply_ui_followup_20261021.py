"""Apply the nine confirmed follow-up changes without rebuilding the scene."""
import json
import re
from pathlib import Path
from revise_ui_20261021 import ROOT, UnityFile, guid, ref
from apply_workbench_revision_20261021 import transplant


def main():
    scene = UnityFile('Assets/Castle/Scenes/CastleEditable.unity')
    flow = next(k for k, t in scene.docs.items() if t.startswith('--- !u!1 ') and scene.get(k, 'm_Name') == 'UIFlow')
    stage = scene.find(flow, 'Canvas/Stage')
    controller = scene.mono(flow, 'RoomArt')
    snapshot_path = ROOT / 'Logs/Revision20261021/followup-before.json'
    if not snapshot_path.exists():
        snapshot_path.write_text(json.dumps(scene.docs), encoding='utf-8')

    timeline = scene.find(stage, 'Pages/Timeline')
    try:
        submit = scene.find(timeline, 'SortRoot/Submit')
    except StopIteration:
        submit = None
    if submit:
        def remove(go):
            for child in list(scene.children(go)):
                remove(child)
            rect = scene.component(go, 224)
            parent = int(re.search(r'\d+', scene.get(rect, 'm_Father'))[0])
            scene.docs[parent] = scene.docs[parent].replace(f'  - {{fileID: {rect}}}\n', '')
            for key in [int(x) for x in re.findall(r'component: \{fileID: (\d+)\}', scene.docs[go])] + [go]:
                del scene.docs[key]
        remove(submit)
        view = scene.mono(timeline, 'Controls')
        scene.docs[view] = re.sub(r'  - Id: Submit(?:Label)?\n    Target: .*\n', '', scene.docs[view])
    view = scene.mono(timeline, 'Controls')
    scene.docs[view] = re.sub(r'  - Id: Submit(?:Label)?\n    Target: .*\n', '', scene.docs[view])

    for field, value in [('DialogueLineSpacing', '12'), ('DialogueEntrySpacing', '14'),
                         ('WorkbenchCursor', ref('Assets/Castle/Art/V24/UI整理/中控/锚点对齐/鼠标.png'))]:
        if re.search(r'^  ' + field + ':', scene.docs[controller], re.M):
            scene.set(controller, field, value)
        else:
            scene.docs[controller] += f'  {field}: {value}\n'
    character = scene.find(stage, 'Pages/Explore/CharacterImage')
    rect = scene.component(character, 224)
    scene.set(rect, 'm_AnchoredPosition', '{x: -468.4181, y: -331.5084}')
    scene.set(rect, 'm_SizeDelta', '{x: 612.4181, y: 1122.509}')

    journal = scene.find(stage, 'Overlays/Journal')
    try:
        dismiss = scene.find(journal, 'Dismiss')
    except StopIteration:
        dismiss = transplant(scene, scene.find(stage, 'Overlays/Map/Dismiss'), scene, journal, 'Dismiss')
        scene.bind(journal, 'Dismiss', scene.mono(dismiss, 'm_Interactable'))
        root_rect = scene.component(journal, 224)
        dismiss_rect = scene.component(dismiss, 224)
        scene.docs[root_rect] = scene.docs[root_rect].replace(f'  - {{fileID: {dismiss_rect}}}\n', '')
        scene.docs[root_rect] = scene.docs[root_rect].replace('  m_Children:\n', f'  m_Children:\n  - {{fileID: {dismiss_rect}}}\n')
    # 图层只作美术展示，空白点击穿透至最底层 Dismiss；按钮与滚动内容保留交互。
    for name in ['Cover', 'PlainRightPage', 'LeftPage', 'RightPage', 'Spine', 'FlipSheet']:
        node = scene.find(journal, name)
        for key, text in list(scene.docs.items()):
            if f'  m_GameObject: {{fileID: {node}}}\n' in text and '  m_RaycastTarget:' in text:
                scene.set(key, 'm_RaycastTarget', 0)
    # 保留作者的参考对象，仅在编辑状态使用，运行时由控制器关闭。
    common = ref('Assets/Castle/Art/V24/UI整理/中控/人物推演/图层 1.png')
    changed_backgrounds = []
    for page in ['Pages/Recordings', 'Pages/Anchors', 'Pages/Space', 'Overlays/Path']:
        backdrop = scene.find(stage, page + '/Backdrop')
        changed_backgrounds += [scene.component(backdrop, 224), scene.mono(backdrop, 'm_Texture')]
        scene.place(backdrop, 0, 0, 1600, 900)
        scene.set(scene.mono(backdrop, 'm_Texture'), 'm_Texture', common)
    # 本项的其余页面对象/组件保持逐字不变。
    before = {int(k): v for k, v in json.loads(snapshot_path.read_text(encoding='utf-8')).items()}
    for page in ['Pages/Recordings', 'Pages/Anchors', 'Pages/Space', 'Overlays/Path']:
        nodes = []
        def collect(go):
            nodes.append(go)
            for child in scene.children(go):
                collect(child)
        collect(scene.find(stage, page))
        for go in nodes:
            keys = [go] + [int(x) for x in re.findall(r'component: \{fileID: (\d+)\}', scene.docs[go])]
            for key in keys:
                if key not in changed_backgrounds:
                    assert scene.docs[key] == before[key], (page, scene.get(go, 'm_Name'), key)
    scene.save()

    cursor = ROOT / 'Assets/Castle/Art/V24/UI整理/中控/锚点对齐/鼠标.png.meta'
    cursor_guid = guid(cursor.relative_to(ROOT).as_posix()[:-5])
    meta = (ROOT / 'Assets/Castle/Art/V24/UI整理/交谈界面/iocon_talking_mouse.png.meta').read_text(encoding='utf-8')
    meta = re.sub(r'^guid: .*$', 'guid: ' + cursor_guid, meta, flags=re.M)
    cursor.write_text(meta, encoding='utf-8', newline='\n')

    prefab = UnityFile('Assets/Castle/Prefabs/V24/BookItemCard.prefab')
    art = next(k for k, t in prefab.docs.items() if t.startswith('--- !u!1 ') and prefab.get(k, 'm_Name') == 'ItemArt')
    names = {prefab.get(k, 'm_Name'): k for k, t in prefab.docs.items() if t.startswith('--- !u!1 ')}
    prefab.place(names['ArtFrame'], -10.5, 31.8145, 231.75, 202.722)
    prefab.place(art, 15, 48.333, 165, 135.833)
    prefab.place(names['Tape'], 257.833, 22.5, 236.667, 81.667)
    prefab.place(names['Title'], 262.833, 37.5, 230, 45)
    prefab.place(names['Body'], 262.833, 107.5, 230, 165)
    if not any('guid: cfabb0440166ab443bba8876756fdfa9' in t for t in prefab.docs.values()):
        key = max(k for k in prefab.docs if k < 2**63 - 100) + 1
        prefab.docs[art] = prefab.docs[art].replace('  m_Layer:', f'  - component: {{fileID: {key}}}\n  m_Layer:')
        prefab.docs[key] = f'''--- !u!114 &{key}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {art}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: cfabb0440166ab443bba8876756fdfa9, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  m_EffectColor: {{r: 0.07450981, g: 0.043137256, b: 0.043137256, a: 0.34}}
  m_EffectDistance: {{x: 0, y: -6}}
  m_UseGraphicAlpha: 1
'''
    prefab.save()
    print('PASS: scene migration; workbench interiors unchanged; item Shadow and cursor importer saved.')


if __name__ == '__main__':
    main()

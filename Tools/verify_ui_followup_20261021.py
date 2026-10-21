"""Read-only binding checks and item-art overlays; no Unity rendering claims."""
import re
from PIL import Image, ImageDraw
from revise_ui_20261021 import ROOT, UnityFile, guid

scene = UnityFile('Assets/Castle/Scenes/CastleEditable.unity')
flow = next(k for k, t in scene.docs.items() if t.startswith('--- !u!1 ') and scene.get(k, 'm_Name') == 'UIFlow')
stage = scene.find(flow, 'Canvas/Stage')
for key, text in scene.docs.items():
    if not text.startswith('--- !u!114 ') or '  Controls:' not in text:
        continue
    controls = re.findall(r'  - Id: (.*)\n    Target: \{fileID: (\d+)\}', text)
    assert len(controls) == len({name for name, _ in controls}), key
    assert all(int(target) in scene.docs for _, target in controls), key
timeline = scene.find(stage, 'Pages/Timeline')
assert '  - Id: Submit' not in scene.docs[scene.mono(timeline, 'Controls')]
assert all(scene.get(ch, 'm_Name') != 'Submit' for ch in scene.children(scene.find(timeline, 'SortRoot')))
journal = scene.find(stage, 'Overlays/Journal')
dismiss = scene.find(journal, 'Dismiss')
journal_rect = scene.docs[scene.component(journal, 224)]
assert re.search(r'  m_Children:\n  - \{fileID: (\d+)\}', journal_rect)[1] == str(scene.component(dismiss, 224))
common = guid('Assets/Castle/Art/V24/UI整理/中控/人物推演/图层 1.png')
for page in ['Pages/Recordings', 'Pages/Anchors', 'Pages/Space', 'Overlays/Path']:
    backdrop = scene.find(stage, page + '/Backdrop')
    assert common in scene.get(scene.mono(backdrop, 'm_Texture'), 'm_Texture')
    assert scene.get(scene.component(backdrop, 224), 'm_SizeDelta') == '{x: 1600, y: 900}'
prefab = UnityFile('Assets/Castle/Prefabs/V24/BookItemCard.prefab')
shadow = next(t for t in prefab.docs.values() if 'guid: cfabb0440166ab443bba8876756fdfa9' in t)
assert 'a: 0.34' in shadow and 'm_EffectDistance: {x: 0, y: -6}' in shadow
cursor = (ROOT / 'Assets/Castle/Art/V24/UI整理/中控/锚点对齐/鼠标.png.meta').read_text(encoding='utf-8')
assert '  textureType: 7\n' in cursor and '  isReadable: 1\n' in cursor

reference = Image.open(ROOT / 'Assets/Castle/Art/V24/preview/调查册-道具.png').convert('RGBA').resize((1600, 900))
layer = Image.new('RGBA', reference.size)
frame = Image.open(ROOT / 'Assets/Castle/Art/V24/UI整理/调查-道具/新建组.png').convert('RGBA')
frame = frame.crop((14, 2, 274, 227))
draw = ImageDraw.Draw(layer)
for x, y, width, height in [(233, 188.333, 195, 170.833), (531.667, 556.667, 216.667, 187.5),
                             (865, 154.167, 195, 170.833), (1158.333, 531.667, 216.667, 187.5)]:
    box = tuple(round(n) for n in (x, y, width, height))
    layer.alpha_composite(frame.resize(box[2:], Image.Resampling.LANCZOS), box[:2])
    draw.rectangle((box[0], box[1], box[0]+box[2], box[1]+box[3]), outline='#00d9ff', width=1)
out = ROOT / 'Logs/Revision20261021'
layer.save(out / 'journal-item-frames.png')
Image.alpha_composite(reference, layer).save(out / 'journal-item-frame-overlay.png')
print('PASS: all saved view bindings, removed Submit, blank-dismiss order, common backgrounds, Shadow and cursor import.')
print('Item frame overlay exported against supplied reference (source-art comparison, not a Unity screenshot).')

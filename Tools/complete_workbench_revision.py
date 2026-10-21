"""Finish Path overlay shortcuts and serialized typography without reapplying migration."""
import re
import subprocess
from pathlib import Path
from revise_ui_20261021 import ROOT, UnityFile, ref
from apply_workbench_revision_20261021 import transplant, UI, root_go

s=UnityFile('Assets/Castle/Scenes/CastleEditable.unity')
flow=next(k for k,t in s.docs.items() if t.startswith('--- !u!1 ') and s.get(k,'m_Name')=='UIFlow')
stage=s.find(flow,'Canvas/Stage')
path=s.find(stage,'Overlays/Path')
for name,source,picture,box in [
    ('JournalShortcut','Journal','图层 23.png',(1325,8,62.5,65)),
    ('MapShortcut','Map','图层 21.png',(1402,12,66.667,64.167)),
    ('SettingsShortcut','Settings','图层 22.png',(1479,12,62.5,64.167))]:
    try:
        node=s.find(path,name)
    except StopIteration:
        node=transplant(s,s.find(stage,'Hud/'+source),s,path,name)
        s.bind(path,name,s.mono(node,'m_OnClick'))
    s.place(node,*box)
    s.set(s.mono(node,'m_Sprite'),'m_Sprite',ref(UI/'中控/人物推演'/picture,True))
    for child in s.children(node):
        s.active(child,False)
controller=s.mono(flow,'RoomArt')
workbench=s.get(controller,'WorkbenchFont')
reading=s.get(controller,'ReadingFont')

def update_fonts(asset,node,font):
    for key,text in list(asset.docs.items()):
        if f'  m_GameObject: {{fileID: {node}}}\n' in text and '\n  m_Text:' in text:
            asset.docs[key]=re.sub(r'(?m)^    m_Font: .*$', '    m_Font: '+font,text)
    for child in asset.children(node):
        update_fonts(asset,child,font)

update_fonts(s,path,workbench)
update_fonts(s,s.find(stage,'Pages/Timeline/TimelineArt/Slots'),reading)
s.save()
for name in ['PersonPathLabel','TimelineClue','TimelineSlot']:
    asset=UnityFile('Assets/Castle/Prefabs/V24/'+name+'.prefab')
    update_fonts(asset,root_go(asset),workbench if name=='PersonPathLabel' else reading)
    asset.save()

# 只规范本次差异中的尾随空格，保留已有场景内容。
result=subprocess.run(['git','-c','safe.directory=D:/game_test/taptap','diff','--check'],cwd=ROOT,capture_output=True,text=True,encoding='utf-8')
lines=set()
for line in result.stdout.splitlines():
    match=re.match(r'Assets/Castle/Scenes/CastleEditable.unity:(\d+): trailing whitespace',line)
    if match:
        lines.add(int(match[1]))
scene=ROOT/'Assets/Castle/Scenes/CastleEditable.unity'
scene.write_text(''.join(line.rstrip()+'\n' if index in lines else line for index,line in enumerate(scene.read_text(encoding='utf-8').splitlines(keepends=True),1)),encoding='utf-8',newline='\n')
print('Saved overlay-local shortcuts, matching fonts and normalized changed scene lines.')

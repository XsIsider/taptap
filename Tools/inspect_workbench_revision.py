"""Inspect workbench nodes and supplied artwork."""
import json
from pathlib import Path
from PIL import Image, ImageDraw
from revise_ui_20261021 import ROOT, UnityFile

s=UnityFile('Assets/Castle/Scenes/CastleEditable.unity')
flow=next(k for k,t in s.docs.items() if t.startswith('--- !u!1 ') and s.get(k,'m_Name')=='UIFlow')
for page in ('Path','Timeline','Tape'):
    try:
        root=s.find(flow,'Canvas/Stage/Pages/'+page)
    except StopIteration:
        root=s.find(flow,'Canvas/Stage/Overlays/'+page)
    def walk(go,depth):
        tr=s.component(go,224)
        print(' '*depth,s.get(go,'m_Name'),go,s.get(tr,'m_AnchoredPosition') if tr else '',s.get(tr,'m_SizeDelta') if tr else '')
        if depth<3:
            for child in s.children(go):
                walk(child,depth+1)
    walk(root,0)
art=ROOT/'Assets/Castle/Art/V24/UI整理'
files=[]
for name in ['中控/人物推演','结局']:
    files.extend((art/name).glob('*.png'))
sheet=Image.new('RGB',(1200,((len(files)+4)//5)*180),'#999999')
draw=ImageDraw.Draw(sheet)
for i,path in enumerate(files):
    pic=Image.open(path).convert('RGBA')
    print(i,path.relative_to(art),pic.size,pic.getbbox())
    pic.thumbnail((230,140))
    x,y=i%5*240,i//5*180
    sheet.paste(pic,(x,y),pic)
    draw.text((x,y+145),str(i)+' '+path.name,fill='black',font_size=13)
out=ROOT/'Logs/WorkbenchRevision20261021'
out.mkdir(parents=True,exist_ok=True)
sheet.save(out/'art.png')
print('GATES',json.loads((ROOT/'SourceData/Formal20261021/GeneratedContent.json').read_text(encoding='utf-8'))['PlaybackGates'])

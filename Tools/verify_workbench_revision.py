"""Check saved workbench bindings and render source-art layout comparisons."""
import json
import math
import re
from PIL import Image, ImageChops, ImageDraw, ImageFont
import numpy as np
from revise_ui_20261021 import ROOT, UnityFile

s=UnityFile('Assets/Castle/Scenes/CastleEditable.unity')
flow=next(k for k,t in s.docs.items() if t.startswith('--- !u!1 ') and s.get(k,'m_Name')=='UIFlow')
stage=s.find(flow,'Canvas/Stage')
pages={name:s.find(stage,path) for name,path in [('Path','Overlays/Path'),('Timeline','Pages/Timeline'),('Tape','Pages/Tape')]}
for name,go in pages.items():
    data=s.docs[s.mono(go,'Controls')]
    controls=re.findall(r'  - Id: (.+)\n    Target: \{fileID: (\d+)\}',data)
    assert len({key for key,_ in controls})==len(controls),name
    assert all(int(target) in s.docs for _,target in controls),name
    if name=='Timeline':
        assert sum(key.startswith('Slot') and key[4:].isdigit() for key,_ in controls)==10
    if name=='Tape':
        bindings=dict(controls)
        for key in ['LinesScroll','Lines','DialoguePanel','Backdrop']:
            assert key in bindings
        lines=int(bindings['Lines'])
        ancestors=[]
        while lines:
            ancestors.append(lines)
            lines=int(re.search(r'\d+',s.get(lines,'m_Father'))[0])
        assert int(bindings['DialoguePanel']) in ancestors,'Tape lines must use common dialogue panel'

for key,text in s.docs.items():
    if not text.startswith('--- !u!224 '):
        continue
    parent=int(re.search(r'\d+',s.get(key,'m_Father'))[0])
    if parent:
        assert f'  - {{fileID: {key}}}\n' in s.docs[parent],('parent-child mismatch',key,parent)
path=pages['Path']
rooms=s.find(path,'MapViewport/MapContent/Rooms')
floor=s.find(path,'MapViewport/MapContent/FloorImage')
assert s.get(s.component(rooms,224),'m_Father')==s.get(s.component(floor,224),'m_Father')
assert floor not in list(s.children(rooms)),'fixed floor image must survive dynamic region clearing'
assert s.get(s.component(s.find(path,'MapViewport/MapContent'),224),'m_Pivot')=='{x: 0.5, y: 0.5}'
data=json.loads((ROOT/'Assets/Castle/Resources/Castle/V24/Content.json').read_text(encoding='utf-8'))
assert next(x for x in data['PlaybackGates'] if x['Event']==920016)['AllowEarlyExit']
assert (ROOT/'SourceData/Formal20261021/GeneratedContent.json').read_bytes()==(ROOT/'Assets/Castle/Resources/Castle/V24/Content.json').read_bytes()
print('PASS: Path/Timeline/Tape fixed bindings, all RectTransform parents, safe map clearing, common Tape panel and playback policy.')

out=ROOT/'Logs/WorkbenchRevision20261021';out.mkdir(parents=True,exist_ok=True)
art=ROOT/'Assets/Castle/Art/V24'
font_path='C:/Windows/Fonts/msyh.ttc'
def font(size):
    return ImageFont.truetype(font_path,size)

def paste(canvas,name,box,relative='UI整理',alpha=1):
    image=Image.open(art/relative/name).convert('RGBA')
    x,y,w,h=box
    image=image.resize((round(w),round(h)),Image.Resampling.LANCZOS)
    if alpha!=1:
        image.putalpha(image.getchannel('A').point(lambda x:round(x*alpha)))
    canvas.alpha_composite(image,(round(x),round(y)))

canvas=Image.new('RGBA',(1600,900))
paste(canvas,'中控/人物推演/图层 1.png',(0,0,1600,900))
draw=ImageDraw.Draw(canvas);draw.rectangle((324,312,1464,728),fill=(56,79,56))
map_image=Image.open(art/'Scene_update/古堡地图1层.png').convert('RGB')
w,h=map_image.size
map_image=map_image.crop((w*.14,h*.18,w*.88,h*.82)).resize((1140,416))
pixels=np.asarray(map_image).astype(float)/255
white=np.min(pixels,axis=2)
t=np.clip((white-.92)/.07,0,1)
alpha=1-t*t*(3-2*t)
map_image=map_image.convert('RGBA')
map_image.putalpha(Image.fromarray((alpha*255).astype('uint8')))
canvas.alpha_composite(map_image,(324,312))
paste(canvas,'中控/人物推演/大底框.png',(324,260,1142,51))
paste(canvas,'中控/人物推演/图层 26.png',(522,232,123,8))
paste(canvas,'中控/人物推演/楼层选择.png',(1312,305,65,5))
paste(canvas,'中控/人物推演/退出.png',(38,15,52,56))
paste(canvas,'中控/人物推演/按钮底图.png',(1330,742,137,46))
draw=ImageDraw.Draw(canvas)
for i,text in enumerate(['录音文本','锚点对齐','空间推演']):
    draw.text((192+i*166,200),text,font=font(26),fill='white' if i==2 else '#9faa96')
for i in range(2):
    draw.text((1312+i*88,272),str(i+1)+'F',font=font(28),fill='white')
for i,text in enumerate(['15:50','16:00','16:10','16:20','16:30']):
    paste(canvas,'中控/人物推演/图层 28.png',(175,288+i*83,65,65))
    ImageDraw.Draw(canvas).text((245,302+i*83),text,font=font(19),fill='white')
ImageDraw.Draw(canvas).text((1342,753),'提交路径',font=font(23),fill='white')
canvas.save(out/'path-layout.png')
reference=Image.open(art/'preview/中控-人物空间.png').convert('RGBA').resize((1600,900))
Image.blend(canvas,reference,.5).save(out/'path-overlay.png')

canvas=Image.open(art/'Scene_update/走廊 白天.png').convert('RGBA').resize((1600,900))
canvas=ImageChops.multiply(canvas,Image.new('RGBA',canvas.size,(217,217,217,255)))
paste(canvas,'结局/封面-ZITI.png',(20,178,1560,411))
paste(canvas,'结局/图层 31.png',(315,777,970,100))
slots=s.find(pages['Timeline'],'TimelineArt/Slots')
positions=[]
for i in range(10):
    node=s.find(slots,'Slot'+str(i))
    value=s.get(s.component(node,224),'m_AnchoredPosition')
    x,y=map(float,re.findall(r'-?\d+(?:\.\d+)?',value))
    positions.append((x,-y))
centers=[(x+44,y+50) for x,y in positions]
draw=ImageDraw.Draw(canvas)
for a,b in zip(centers,centers[1:]):
    same=abs(a[1]-b[1])<1
    start=(a[0],a[1]+(50 if same or b[1]>a[1] else -50))
    end=(b[0],b[1]+(50 if same or a[1]>b[1] else -50))
    draw.line((start,end),fill='#b20805',width=2)
star=Image.open(art/'UI整理/结局/图层 31.png').convert('RGBA').crop((0,0,161,238)).resize((8,12))
for x,y in positions:
    paste(canvas,'结局/11.png',(x,y,88,100),alpha=.55)
    for dx,dy in [(-4,-6),(84,-6),(-4,94),(84,94)]:
        canvas.alpha_composite(star,(round(x+dx),round(y+dy)))
for i in range(10):
    paste(canvas,'结局/图层 28 拷贝 6.png',(78+i*155.5,382,10,10))
for i in range(7):
    paste(canvas,'结局/11.png',(420+i*108,770,86,100))
    paste(canvas,'调查-记录/素材.png',(438+i*108,802,53,60))
    ImageDraw.Draw(canvas).text((427+i*108,774),'记录'+str((i*3)%10+1),font=font(16),fill='#302719')
canvas.save(out/'timeline-layout.png')
reference=Image.open(art/'preview/结局界面1.png').convert('RGBA').resize((1600,900))
Image.blend(canvas,reference,.5).save(out/'timeline-overlay.png')
print('Source-art layouts and reference overlays exported (not Unity screenshots).')

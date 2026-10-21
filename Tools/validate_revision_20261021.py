"""Verify serialized references and export art-layout comparison previews."""
import hashlib
import json
import re
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageChops
import numpy as np
from revise_ui_20261021 import ROOT, UnityFile, guid

art=ROOT/'Assets/Castle/Art/V24'
new=list((art/'UI整理').rglob('*.png'))
scene=UnityFile('Assets/Castle/Scenes/CastleEditable.unity')
flow=next(k for k,t in scene.docs.items() if t.startswith('--- !u!1 ') and scene.get(k,'m_Name')=='UIFlow')
map_go=scene.find(flow,'Canvas/Stage/Overlays/Map')
# 检查新增对象的双向父子引用和全部固定绑定。
for go_name in ['Dismiss','FloorImage/CurrentLocationMarker']:
    go=scene.find(map_go,go_name)
    rect=scene.component(go,224)
    parent=int(re.search(r'\d+',scene.get(rect,'m_Father'))[0])
    assert f'  - {{fileID: {rect}}}\n' in scene.docs[parent],go_name
data=json.loads((ROOT/'Assets/Castle/Resources/Castle/V24/Content.json').read_text(encoding='utf-8'))
assert (ROOT/'SourceData/Formal20261021/GeneratedContent.json').read_bytes()==(ROOT/'Assets/Castle/Resources/Castle/V24/Content.json').read_bytes()
for table in ['Character','Room','Plot','PlotChoice','Event','Record','Puzzle','Entry']:
    assert len({r['Id'] for r in data[table]})==len(data[table]),table
for row in data['Plot']:
    assert len(row['LinkText'])==len(row['LinkTarget']),row['Id']
assert next(x for x in data['Plot'] if x['Id']==520156)['Text']=='（开门声）'
assert next(x for x in data['Room'] if x['Id']==100006)['Name']=='收藏室'

# 按画布坐标组合美术，供叠图查看；不替代 Unity 的实际渲染验收。
out=ROOT/'Logs/Revision20261021'
def paste(canvas,path,box,crop=None):
    image=Image.open(art/'UI整理'/path).convert('RGBA')
    if crop:
        image=image.crop(crop)
    x,y,w,h=box
    image=image.resize((round(w),round(h)),Image.Resampling.LANCZOS)
    canvas.alpha_composite(image,(round(x),round(y)))

for category,name in enumerate(['角色','道具','记录']):
    canvas=Image.new('RGBA',(1600,900))
    paste(canvas,'调查-角色/bg_diaocha.png',(65.833,0,1529.167,896.667))
    if category!=0:
        paper=Image.open(art/'UI整理/调查-角色/bg_diaocha.png').convert('RGBA')
        w,h=paper.size
        a=np.array(paper).astype(float)
        yy,xx=np.mgrid[0:h,0:w];u=xx/(w-1);v=1-yy/(h-1)
        def smooth(low,high,x):
            t=np.clip((x-low)/(high-low),0,1)
            return t*t*(3-2*t)
        mask=smooth(.526,.546,u)*(1-smooth(.874,.888,u))*smooth(.145,.165,v)*(1-smooth(.623,.643,v))
        sample=a[:,np.clip(np.round(np.arange(w)-w*.435).astype(int),0,w-1)]
        clean=Image.fromarray(np.clip(a*(1-mask[:,:,None])+sample*mask[:,:,None],0,255).astype('uint8'))
        canvas=Image.new('RGBA',(1600,900))
        canvas.alpha_composite(clean.resize((1529,897),Image.Resampling.LANCZOS),(66,0))
    for i in range(3):
        active=category==i
        paste(canvas,'调查-角色/button_diaocha_biaoqian_'+('2' if active else '1')+'.png',(201.667+i*62.5,20.833 if active else 32.5,59.167,79.167 if active else 67.5), (242,19,313,114) if active else (317,29,388,110))
        icon=['people','search','prop'][i]
        image=Image.open(art/f'UI整理/调查-角色/icon_diaocha_{icon}.png').convert('RGBA')
        bbox=image.getbbox();image=image.crop(bbox)
        image=image.resize((round(image.width/1.2),round(image.height/1.2)),Image.Resampling.LANCZOS)
        canvas.alpha_composite(image,(round(201.667+i*62.5+(59.167-image.width)/2),round((20.833 if active else 32.5)+(19 if active else 12))))
    if category==0:
        for row in range(2):
            for col in range(2):
                paste(canvas,'调查-角色/bg_diaocha_character_1.png',(226.667+col*287.5,111.667+row*355,235,315))
        paste(canvas,'调查-角色/bg_diaocha_jiaodai.png',(950,151,241,82))
        paste(canvas,'调查-角色/bg_diaocha_character_2.png',(865,180,190,215))
        paste(canvas,'调查-角色/bg_diaocha_fenge.png',(910,85,309,17.5))
    elif category==1:
        for col in range(2):
            for row in range(2):
                x=233 if col==0 else 865;y=155 if col==0 else 130
                paste(canvas,'调查-道具/新建组.png',(x+(300 if row else 0),y+row*370+25,195,170))
                paste(canvas,'调查-角色/bg_diaocha_jiaodai.png',(x+(30 if row else 260),y+row*370+25,241,82))
    else:
        for row in range(3):
            paste(canvas,'调查-记录/从选区.png',(215,158+row*210+96,241,82))
            paste(canvas,'调查-记录/新建组.png',(225,158+row*210,168,169))
        paste(canvas,'调查-记录/素材.png',(841,36,239,260))
    canvas.save(out/f'journal-{category}-layout.png')
    reference=Image.open(art/f'preview/调查册-{name}.png').convert('RGBA').resize((1600,900))
    Image.blend(canvas,reference,.5).save(out/f'journal-{category}-overlay.png')
print('PASS: config tables, link arrays, synchronized JSON, new hierarchy bindings; layout/overlay previews saved.')

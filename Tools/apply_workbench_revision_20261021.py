"""Apply targeted Path / Timeline / Tape UI changes, preserving current scene edits."""
import math
import re
import uuid
from pathlib import Path
from revise_ui_20261021 import ROOT, UnityFile, guid, ref

SCENE='Assets/Castle/Scenes/CastleEditable.unity'
UI=Path('Assets/Castle/Art/V24/UI整理')


def root_go(asset):
    for k,text in asset.docs.items():
        if text.startswith('--- !u!224 ') and asset.get(k,'m_Father')=='{fileID: 0}':
            return int(re.search(r'\d+',asset.get(k,'m_GameObject'))[0])
    raise RuntimeError('root missing')


def move(asset,go,parent):
    tr=asset.component(go,224)
    old=int(re.search(r'\d+',asset.get(tr,'m_Father'))[0])
    if old:
        asset.docs[old]=asset.docs[old].replace(f'  - {{fileID: {tr}}}\n','')
    new=asset.component(parent,224)
    asset.set(tr,'m_Father',f'{{fileID: {new}}}')
    asset.docs[new]=asset.docs[new].replace('  m_Children: []','  m_Children:')
    asset.docs[new]=asset.docs[new].replace('  m_Father:',f'  - {{fileID: {tr}}}\n  m_Father:')


def transplant(source,go,target,parent,name):
    nodes=[]
    def collect(node):
        nodes.append(node)
        for child in source.children(node):
            collect(child)
    collect(go)
    keys=[]
    for node in nodes:
        keys.append(node)
        keys.extend(int(x) for x in re.findall(r'component: \{fileID: (\d+)\}',source.docs[node]))
    keys=list(dict.fromkeys(keys))
    base=max((k for k in target.docs if k<2**31),default=1000000000)+10
    mapping=dict(zip(keys,range(base,base+len(keys))))
    for old,new in mapping.items():
        text=source.docs[old]
        text=re.sub(r'(?m)^--- (!u!\d+) &\d+',rf'--- \1 &{new}',text)
        target.docs[new]=re.sub(r'fileID: (-?\d+)',lambda m:'fileID: '+str(mapping.get(int(m[1]),int(m[1]))),text)
    node=mapping[go]
    target.set(node,'m_Name',name)
    tr=target.component(node,224)
    target.set(tr,'m_Father','{fileID: 0}')
    move(target,node,parent)
    return node


def prefab(name):
    path=ROOT/'Assets/Castle/Prefabs/V24'/f'{name}.prefab'
    path.write_bytes((ROOT/'Assets/Castle/Prefabs/Flow/FlowCard.prefab').read_bytes())
    if not Path(str(path)+'.meta').exists():
        Path(str(path)+'.meta').write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nPrefabImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n',encoding='utf-8')
    asset=UnityFile(path.relative_to(ROOT))
    node=root_go(asset)
    asset.set(node,'m_Name',name)
    asset.set(asset.mono(node,'Controls'),'Id',name)
    return asset,node


def text_style(asset,go,size,color='{r: 1, g: 1, b: 1, a: 1}',align=4):
    key=asset.mono(go,'m_Text')
    asset.set(key,'m_Color',color)
    data=asset.docs[key]
    data=re.sub(r'(?m)^    m_FontSize: .*$',f'    m_FontSize: {size}',data)
    data=re.sub(r'(?m)^    m_MaxSize: .*$',f'    m_MaxSize: {size}',data)
    data=re.sub(r'(?m)^    m_MinSize: .*$',f'    m_MinSize: {min(12,size)}',data)
    data=re.sub(r'(?m)^    m_Alignment: .*$',f'    m_Alignment: {align}',data)
    asset.docs[key]=data


def slot_corners(source,raw_template,target,slot):
    for i,(x,y) in enumerate([(-4,-6),(84,-6),(-4,94),(84,94)]):
        node=transplant(source,raw_template,target,slot,'Corner'+str(i))
        target.place(node,x,y,8,12)
        raw=target.mono(node,'m_Texture')
        target.set(raw,'m_Texture',ref(UI/'结局/图层 31.png'))
        target.set(raw,'m_Material','{fileID: 0}')
        target.set(raw,'m_Color','{r: 1, g: 1, b: 1, a: 1}')
        target.set(raw,'m_RaycastTarget',0)
        target.docs[raw]=re.sub(r'(?ms)^  m_UVRect:.*?(?=^\S|\Z)', '  m_UVRect:\n    serializedVersion: 2\n    x: 0\n    y: 0\n    width: 0.07\n    height: 1\n',target.docs[raw])
        target.active(node,True)


def main():
    s=UnityFile(SCENE)
    if '  - Id: Slot0\n' in ''.join(s.docs.values()):
        raise RuntimeError('Workbench revision already applied')
    flow=next(k for k,t in s.docs.items() if t.startswith('--- !u!1 ') and s.get(k,'m_Name')=='UIFlow')
    controller=s.mono(flow,'RoomArt')
    stage=s.find(flow,'Canvas/Stage')
    path=s.find(stage,'Overlays/Path')
    timeline=s.find(stage,'Pages/Timeline')
    tape=s.find(stage,'Pages/Tape')
    image_template=s.find(path,'Board')
    def image(asset,parent,name,picture,box,color='{r: 1, g: 1, b: 1, a: 1}'):
        node=transplant(s,image_template,asset,parent,name)
        asset.place(node,*box)
        key=asset.mono(node,'m_Sprite')
        asset.set(key,'m_Sprite',ref(UI/picture,True) if picture else '{fileID: 0}')
        asset.set(key,'m_Color',color)
        asset.set(key,'m_RaycastTarget',0)
        asset.set(key,'m_Material','{fileID: 0}')
        asset.active(node,True)
        return node

    # 只创建动态列表模板；固定排序槽在场景中保存。
    path_prefab,card=prefab('PersonPathLabel')
    path_prefab.place(card,0,0,160,78)
    path_prefab.set(path_prefab.mono(card,'m_PreferredHeight'),'m_PreferredHeight',78)
    path_prefab.set(path_prefab.mono(card,'m_Sprite'),'m_Color','{r: 0, g: 0, b: 0, a: 0}')
    image(path_prefab,card,'PersonIcon','中控/人物推演/图层 28.png',(0,3,65,65))
    path_prefab.place(path_prefab.find(card,'Title'),70,15,78,35)
    text_style(path_prefab,path_prefab.find(card,'Title'),19)
    path_prefab.active(path_prefab.find(card,'Body'),False)
    path_prefab.save()
    s.docs[controller]+=f'  PathLabelPrefab: {{fileID: {path_prefab.mono(card,"Controls")}, guid: {guid(path_prefab.path.relative_to(ROOT))}, type: 3}}\n'
    s.docs[controller]+=f'  PersonPathMarkerIcon: {ref(UI/"中控/人物推演/地图标记点.png",True)}\n'

    clue_prefab,card=prefab('TimelineClue')
    clue_prefab.place(card,0,0,86,100)
    clue_prefab.set(clue_prefab.mono(card,'m_PreferredHeight'),'m_PreferredHeight',100)
    bg=clue_prefab.mono(card,'m_Sprite')
    clue_prefab.set(bg,'m_Sprite',ref(UI/'结局/11.png',True))
    clue_prefab.set(bg,'m_Color','{r: 1, g: 1, b: 1, a: 1}')
    clue_prefab.place(clue_prefab.find(card,'Title'),3,3,80,30)
    text_style(clue_prefab,clue_prefab.find(card,'Title'),16,'{r: 0.18, g: 0.15, b: 0.1, a: 1}')
    clue_prefab.active(clue_prefab.find(card,'Body'),False)
    image(clue_prefab,card,'RecordIcon','调查-记录/素材.png',(18,32,53,60))
    clue_prefab.save()
    s.docs[controller]+=f'  TimelineCluePrefab: {{fileID: {clue_prefab.mono(card,"Controls")}, guid: {guid(clue_prefab.path.relative_to(ROOT))}, type: 3}}\n'

    slot_prefab,card=prefab('TimelineSlot')
    slot_prefab.place(card,0,0,88,100)
    slot_prefab.set(slot_prefab.mono(card,'m_PreferredHeight'),'m_PreferredHeight',100)
    bg=slot_prefab.mono(card,'m_Sprite')
    slot_prefab.set(bg,'m_Sprite',ref(UI/'结局/11.png',True))
    slot_prefab.set(bg,'m_Color','{r: 1, g: 1, b: 1, a: 0.55}')
    slot_prefab.place(slot_prefab.find(card,'Title'),4,4,80,92)
    text_style(slot_prefab,slot_prefab.find(card,'Title'),16,'{r: 0.16, g: 0.12, b: 0.06, a: 1}')
    slot_prefab.active(slot_prefab.find(card,'Body'),False)
    slot_corners(s,s.find(path,'FloorImage'),slot_prefab,card)
    slot_prefab.save()

    p=lambda name:s.find(path,name)
    for name in ['Tint','Board','Divider','SideDivider','Details','Hypothesis','ZoomIn','ZoomOut','ResetView','PuzzleName','PreviousPuzzle','NextPuzzle','TooltipPanel']:
        s.active(p(name),False)
    raw=s.mono(p('Backdrop'),'m_Texture')
    s.set(raw,'m_Texture',ref(UI/'中控/人物推演/图层 1.png'))
    s.set(raw,'m_Color','{r: 1, g: 1, b: 1, a: 1}')
    s.set(raw,'m_RaycastTarget',0)
    s.place(p('Backdrop'),0,0,1600,900)
    for name,source,picture,box in [
        ('JournalShortcut','Journal','图层 23.png',(1325,8,62.5,65)),
        ('MapShortcut','Map','图层 21.png',(1402,12,66.667,64.167)),
        ('SettingsShortcut','Settings','图层 22.png',(1479,12,62.5,64.167))]:
        shortcut=transplant(s,s.find(stage,'Hud/'+source),s,path,name)
        s.place(shortcut,*box)
        s.set(s.mono(shortcut,'m_Sprite'),'m_Sprite',ref(UI/'中控/人物推演'/picture,True))
        for child in s.children(shortcut):
            s.active(child,False)
        s.bind(path,name,s.mono(shortcut,'m_OnClick'))
    area=image(s,path,'MapArea',None,(324,312,1140,416),'{r: 0.22, g: 0.31, b: 0.22, a: 1}')
    image(s,path,'FloorBar','中控/人物推演/大底框.png',(324,260,1142,51))
    image(s,path,'SpaceUnderline','中控/人物推演/图层 26.png',(522,232,123,8))
    underline=image(s,path,'FloorUnderline','中控/人物推演/楼层选择.png',(1312,305,65,5))
    s.bind(path,'FloorUnderline',s.component(underline,224))
    for i,name in enumerate(['Recordings','Anchors','Space']):
        s.active(p(name),True)
        s.place(p(name),192+i*166,199,136,40)
        s.set(s.mono(p(name),'m_Color'),'m_Color','{r: 1, g: 1, b: 1, a: 0}')
        label=next(c for c in s.children(p(name)) if s.get(c,'m_Name').endswith('Label'))
        s.place(label,0,0,136,40)
        text_style(s,label,26)
    s.place(p('Close'),38,15,52,56)
    s.set(s.mono(p('Close'),'m_Sprite'),'m_Sprite',ref(UI/'中控/人物推演/退出.png',True))
    s.set(s.mono(p('Close'),'m_Color'),'m_Color','{r: 1, g: 1, b: 1, a: 1}')
    for child in s.children(p('Close')):
        s.active(child,False)
    for i,name in enumerate(['Floor1','Floor2']):
        s.place(p(name),1312+i*88,271,65,35)
        s.set(s.mono(p(name),'m_Color'),'m_Color','{r: 1, g: 1, b: 1, a: 0}')
        label=s.find(p(name),name+'Label')
        s.place(label,0,0,65,35)
        text_style(s,label,28)
        s.set(s.mono(label,'m_Text'),'m_Text',str(i+1)+'F')
    s.place(p('ListScroll'),175,285,150,420)
    s.place(p('ListScroll/Viewport'),0,0,150,420)
    s.place(p('ListScroll/Viewport/List'),0,0,150,0)
    s.set(s.mono(p('ListScroll/Viewport/List'),'m_Spacing'),'m_Spacing',5)
    s.active(p('ListScroll/Scrollbar'),False)
    scroll=s.mono(p('ListScroll'),'m_Content')
    s.set(scroll,'m_VerticalScrollbar','{fileID: 0}')
    s.place(p('FloorImage'),324,312,1140,416)
    s.set(s.mono(p('FloorImage'),'m_Texture'),'m_Material',f'{{fileID: 2100000, guid: {guid("Assets/Castle/Resources/Castle/V24PathMap.mat")}, type: 2}}')
    s.place(p('MapViewport'),324,312,1140,416)
    s.place(p('MapViewport/Rooms'),0,0,1140,416)
    # 底图与交互区域一起缩放/平移，且固定底图不被 Rooms 的动态列表清理销毁。
    rooms=p('MapViewport/Rooms')
    floor_image=p('FloorImage')
    content=s.clone(rooms,p('MapViewport'),'MapContent')
    s.place(content,0,0,1140,416)
    for key in ('m_AnchorMin','m_AnchorMax','m_Pivot'):
        s.set(s.component(content,224),key,'{x: 0.5, y: 0.5}')
    move(s,floor_image,content);move(s,rooms,content)
    s.place(floor_image,0,0,1140,416)
    raw=s.mono(floor_image,'m_Texture')
    s.docs[raw]=re.sub(r'(?ms)^  m_UVRect:.*?(?=^\S|\Z)', '  m_UVRect:\n    serializedVersion: 2\n    x: 0.14\n    y: 0.18\n    width: 0.74\n    height: 0.64\n',s.docs[raw])
    s.set(s.mono(p('MapViewport'),'Content'),'Content',f'{{fileID: {s.component(content,224)}}}')
    s.place(p('Verify'),1330,742,137,46)
    s.set(s.mono(p('Verify'),'m_Sprite'),'m_Sprite',ref(UI/'中控/人物推演/按钮底图.png',True))
    s.set(s.mono(p('Verify'),'m_Color'),'m_Color','{r: 1, g: 1, b: 1, a: 1}')
    verify_label=s.find(p('Verify'),'VerifyLabel')
    s.place(verify_label,0,0,137,46);text_style(s,verify_label,23)
    # 背景、地图区域排到所有交互对象之后。
    parent=s.component(path,224)
    order=[s.component(p('Backdrop'),224),s.component(area,224)]
    for tr in order:
        s.docs[parent]=s.docs[parent].replace(f'  - {{fileID: {tr}}}\n','')
    s.docs[parent]=s.docs[parent].replace('  m_Children:\n','  m_Children:\n'+''.join(f'  - {{fileID: {tr}}}\n' for tr in order))

    t=lambda name:s.find(timeline,name)
    s.set(s.mono(t('Backdrop'),'m_Texture'),'m_Texture',ref('Assets/Castle/Art/V24/Scene_update/走廊 白天.png'))
    s.set(s.mono(t('Backdrop'),'m_Color'),'m_Color','{r: 0.85, g: 0.85, b: 0.85, a: 1}')
    for name in ['Tint','Heading','Requirements','SortRoot/Hint','RevealRoot/SceneBackdrop','RevealRoot/SoundWave','RevealRoot/Wave','RevealRoot/EndingText_frame']:
        s.active(t(name),False)
    shared=s.clone(t('SortRoot/Slots'),timeline,'TimelineArt')
    s.place(shared,0,0,1600,900)
    s.set(s.mono(shared,'m_CellSize'),'m_Enabled',0)
    s.active(shared,True)
    wave=image(s,shared,'ReferenceWave','结局/封面-ZITI.png',(20,178,1560,411))
    image(s,shared,'ClueDivider','结局/图层 31.png',(315,777,970,100))
    slots=t('SortRoot/Slots')
    move(s,slots,shared)
    s.place(slots,0,0,1600,900)
    grid=s.mono(slots,'m_CellSize');s.set(grid,'m_Enabled',0)
    positions=[(44,545),(176,140),(302,442),(390,216),(607,216),(804,484),(1019,210),(1144,540),(1258,140),(1434,435)]
    centers=[(x+44,y+50) for x,y in positions]
    for i,(x,y) in enumerate(positions):
        node=transplant(slot_prefab,card,s,slots,'Slot'+str(i))
        s.place(node,x,y,88,100)
        s.bind(timeline,'Slot'+str(i),s.mono(node,'Controls'))
        if i:
            a,b=centers[i-1],centers[i]
            same=abs(a[1]-b[1])<1
            start=(a[0],a[1]+(50 if same or b[1]>a[1] else -50))
            end=(b[0],b[1]+(50 if same or a[1]>b[1] else -50))
            dx,dy=end[0]-start[0],end[1]-start[1]
            node=image(s,shared,'Connection'+str(i),None,(start[0],start[1],math.hypot(dx,dy),1.5),'{r: 0.7, g: 0.03, b: 0.02, a: 1}')
            tr=s.component(node,224)
            s.set(tr,'m_Pivot','{x: 0, y: 0.5}')
            angle=math.atan2(-dy,dx)/2
            s.set(tr,'m_LocalRotation',f'{{x: 0, y: 0, z: {math.sin(angle)}, w: {math.cos(angle)}}}')
    for i in range(10):
        image(s,shared,'WavePoint'+str(i),'结局/图层 28 拷贝 6.png',(78+i*155.5,382,10,10))
    # 共享时间线始终位于人物与对白前景后方。
    parent=s.component(timeline,224);tr=s.component(shared,224)
    s.docs[parent]=s.docs[parent].replace(f'  - {{fileID: {tr}}}\n','')
    backdrop=s.component(t('Backdrop'),224)
    s.docs[parent]=s.docs[parent].replace(f'  - {{fileID: {backdrop}}}\n',f'  - {{fileID: {backdrop}}}\n  - {{fileID: {tr}}}\n')
    clue_scroll=t('SortRoot/CluesScroll')
    s.place(clue_scroll,420,770,760,105)
    s.place(t('SortRoot/CluesScroll/Viewport'),0,0,760,105)
    content=t('SortRoot/CluesScroll/Viewport/Clues')
    s.place(content,0,0,760,100)
    layout=s.mono(content,'m_Padding')
    template=s.docs[grid]
    template=template.replace(f'&{grid}\n',f'&{layout}\n').replace(f'm_GameObject: {{fileID: {slots}}}',f'm_GameObject: {{fileID: {content}}}')
    s.docs[layout]=template
    for field,value in [('m_Enabled',1),('m_CellSize','{x: 86, y: 100}'),('m_Spacing','{x: 22, y: 0}'),('m_Constraint',2),('m_ConstraintCount',1)]:
        s.set(layout,field,value)
    fitter=s.mono(content,'m_HorizontalFit');s.set(fitter,'m_HorizontalFit',2);s.set(fitter,'m_VerticalFit',0)
    scroll=s.mono(clue_scroll,'m_Content');s.set(scroll,'m_Horizontal',1);s.set(scroll,'m_Vertical',0);s.set(scroll,'m_VerticalScrollbar','{fileID: 0}')
    s.active(t('SortRoot/CluesScroll/Scrollbar'),False)
    s.place(t('SortRoot/Submit'),1365,824,190,45)
    for name,box in {'DialogueArt':(315,555,1040,322),'NameArt':(375,574,270,64),'EndingStatus':(445,590,220,40),'EndingText':(490,664,690,155),'Heroine':(0,150,510,750),'Partner':(1100,120,500,780)}.items():
        s.place(t('RevealRoot/'+name),*box)

    # 磁带页直接复用现场对白面板和其滚动设置；内容继续共用 DialogueLine.prefab。
    explore_panel=s.find(stage,'Pages/Explore/DialoguePanel')
    panel=transplant(s,explore_panel,s,tape,'DialoguePanel')
    s.active(panel,True)
    for child in s.children(panel):
        if s.get(child,'m_Name') not in ('DialogueScroll','Back'):
            s.active(child,False)
    # 克隆对象包含组件但不包含现场回调；磁带只使用播放/进度控制。
    v=s.mono(tape,'Controls')
    for key,target in [('LinesScroll',s.mono(s.find(panel,'DialogueScroll'),'m_Content')),('Lines',s.component(s.find(panel,'DialogueScroll/Viewport/Dialogue'),224))]:
        s.docs[v]=re.sub(r'(?m)(  - Id: '+key+r'\n    Target: )\{fileID: \d+\}',rf'\g<1>{{fileID: {target}}}',s.docs[v])
    s.bind(tape,'DialoguePanel',s.component(panel,224))
    s.bind(tape,'Backdrop',s.mono(s.find(tape,'Backdrop'),'m_Texture'))
    for child in list(s.children(tape)):
        name=s.get(child,'m_Name')
        if name not in ('DialoguePanel','Backdrop','Play','Progress','Playback','FileInfo'):
            s.active(child,False)
    for name,box in {'Play':(1052,788,40,40),'Progress':(1105,798,385,22),'Playback':(1052,833,440,28),'FileInfo':(1052,110,440,28)}.items():
        go=s.find(tape,name);s.place(go,*box);s.active(go,True)
    s.place(s.find(tape,'Progress/Rail'),0,9,385,4)
    s.place(s.find(tape,'Progress/HandleArea'),12,0,361,22)
    text_style(s,s.find(tape,'FileInfo'),18)
    text_style(s,s.find(tape,'Playback'),17)
    s.set(s.mono(s.find(tape,'Backdrop'),'m_Texture'),'m_Color','{r: 1, g: 1, b: 1, a: 1}')
    s.set(s.mono(s.find(tape,'Backdrop'),'m_Texture'),'m_Texture',ref('Assets/Castle/Art/V24/Scene_update/女主客房1 白天.png'))
    # 背景在底层；播放按钮位于对话框前。
    parent=s.component(tape,224)
    tr=s.component(s.find(tape,'Backdrop'),224)
    s.docs[parent]=s.docs[parent].replace(f'  - {{fileID: {tr}}}\n','')
    s.docs[parent]=s.docs[parent].replace('  m_Children:\n',f'  m_Children:\n  - {{fileID: {tr}}}\n')
    for name in ('Play','Progress','Playback','FileInfo'):
        tr=s.component(s.find(tape,name),224)
        s.docs[parent]=s.docs[parent].replace(f'  - {{fileID: {tr}}}\n','')
        s.docs[parent]=s.docs[parent].replace('  m_Father:',f'  - {{fileID: {tr}}}\n  m_Father:')
    s.save()
    for script in (ROOT/'Assets/Castle/Scripts/V24').glob('*.cs'):
        meta=Path(str(script)+'.meta')
        if not meta.exists():
            meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nMonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n',encoding='utf-8')
    print('Saved Path artwork, fixed Timeline slots, bottom clue scroll and common Tape dialogue panel.')


if __name__=='__main__':
    main()

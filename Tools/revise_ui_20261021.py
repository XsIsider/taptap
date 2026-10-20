"""Targeted serialized UI migration; preserve all unrelated scene data and GUIDs."""
import re
import json
import uuid
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


class UnityFile:
    def __init__(self, path):
        self.path = ROOT / path
        text = self.path.read_text(encoding='utf-8')
        parts = re.split(r'(?m)(^--- !u!\d+ &\d+.*\n)', text)
        self.header = parts[0]
        self.docs = {}
        for i in range(1, len(parts), 2):
            key = int(re.search(r'&(\d+)', parts[i]).group(1))
            self.docs[key] = parts[i] + parts[i+1]

    def get(self, key, field):
        return re.search(r'(?m)^  '+re.escape(field)+r': (.*)$', self.docs[key]).group(1)

    def set(self, key, field, value):
        self.docs[key], count = re.subn(r'(?m)^  '+re.escape(field)+r': .*$', '  '+field+': '+str(value), self.docs[key])
        assert count == 1, (key, field, count)

    def component(self, go, kind):
        for key, text in self.docs.items():
            if text.startswith('--- !u!'+str(kind)+' ') and f'  m_GameObject: {{fileID: {go}}}\n' in text:
                return key

    def children(self, go):
        transform = self.component(go, 224) or self.component(go, 4)
        for key, text in self.docs.items():
            if f'  m_Father: {{fileID: {transform}}}\n' in text:
                yield int(self.get(key, 'm_GameObject').split(': ')[1].strip('}'))

    def find(self, go, path):
        for name in path.split('/'):
            go = next(child for child in self.children(go) if self.get(child, 'm_Name') == name)
        return go

    def place(self, go, x, y, width, height):
        key = self.component(go, 224)
        for field, value in {'m_AnchorMin':'{x: 0, y: 1}', 'm_AnchorMax':'{x: 0, y: 1}',
                             'm_Pivot':'{x: 0, y: 1}', 'm_AnchoredPosition':f'{{x: {x}, y: {-y}}}',
                             'm_SizeDelta':f'{{x: {width}, y: {height}}}', 'm_LocalScale':'{x: 1, y: 1, z: 1}'}.items():
            self.set(key, field, value)

    def save(self):
        self.path.write_text(self.header + ''.join(self.docs.values()), encoding='utf-8', newline='\n')

    def mono(self, go, field):
        return next(k for k,t in self.docs.items() if t.startswith('--- !u!114 ') and f'  m_GameObject: {{fileID: {go}}}\n' in t and re.search(r'(?m)^  '+re.escape(field)+':',t))

    def active(self, go, value):
        self.set(go,'m_IsActive',int(value))

    def clone(self, source, parent, name, first=False):
        rect = self.component(source,224)
        ids = [source,rect] + [int(x) for x in re.findall(r'component: \{fileID: (\d+)\}',self.docs[source]) if int(x)!=rect]
        base = max(k for k in self.docs if k < 2**31) + 10
        mapping = dict(zip(ids,range(base,base+len(ids))))
        for old,new in mapping.items():
            text = self.docs[old]
            text = re.sub(r'(?m)^--- (!u!\d+) &\d+',rf'--- \1 &{new}',text)
            text = re.sub(r'fileID: (\d+)',lambda m:'fileID: '+str(mapping.get(int(m[1]),int(m[1]))),text)
            self.docs[new] = text
        go = mapping[source]
        self.set(go,'m_Name',name)
        tr = mapping[rect]
        self.docs[tr] = re.sub(r'(?ms)^  m_Children:.*?(?=^  m_Father:)', '  m_Children: []\n',self.docs[tr])
        par = self.component(parent,224)
        self.set(tr,'m_Father',f'{{fileID: {par}}}')
        if first:
            self.docs[par] = self.docs[par].replace('  m_Children:\n',f'  m_Children:\n  - {{fileID: {tr}}}\n')
        else:
            self.docs[par] = self.docs[par].replace('  m_Father:',f'  - {{fileID: {tr}}}\n  m_Father:')
        return go

    def bind(self, go, name, component):
        key = self.mono(go,'Controls')
        self.docs[key] += f'  - Id: {name}\n    Target: {{fileID: {component}}}\n'


def guid(path):
    return re.search(r'(?m)^guid: (\w+)',(ROOT / (str(path)+'.meta')).read_text(encoding='utf-8'))[1]


def ref(path, sprite=False, file_id=None):
    return f'{{fileID: {file_id or (21300000 if sprite else 2800000)}, guid: {guid(path)}, type: 3}}'


def migrate():
    if 'm_Name: PlainRightPage\n' in (ROOT/'Assets/Castle/Scenes/CastleEditable.unity').read_text(encoding='utf-8'):
        raise RuntimeError('本次一次性迁移已经应用；请勿重复执行 --apply。默认运行只检查布局。')
    art = Path('Assets/Castle/Art/V24')
    ui = art / 'UI整理'
    # 复用旧导入器已裁切的可见 Sprite；新文件保留自己的 GUID。
    for name in ['button_diaocha_biaoqian_1.png','button_diaocha_biaoqian_2.png','icon_diaocha_people.png','icon_diaocha_search.png','icon_diaocha_prop.png']:
        old = art / 'UI/调查-角色' / name
        new = ui / '调查-角色' / name
        new_guid = guid(new)
        meta = (ROOT / (str(old)+'.meta')).read_text(encoding='utf-8')
        meta = re.sub(r'(?m)^guid: \w+', 'guid: '+new_guid, meta)
        (ROOT / (str(new)+'.meta')).write_text(meta,encoding='utf-8',newline='\n')
    remap = {}
    candidates = list((ROOT/ui).rglob('*.png'))
    aliases = {'background.png':'bg_diaocha.png','dividing.png':'bg_diaocha_fenge.png','iocon_talking_mouse.png':'iocon_talking_mouse.png'}
    for old in (ROOT/art/'UI').rglob('*.png'):
        rel = old.relative_to(ROOT/art/'UI')
        new = ROOT/ui/rel
        if not new.exists():
            matches = [p for p in candidates if p.name == aliases.get(old.name,old.name)]
            if len(matches)==1:
                new = matches[0]
        if new.exists():
            remap[guid(old.relative_to(ROOT))] = guid(new.relative_to(ROOT))
    scene = UnityFile('Assets/Castle/Scenes/CastleEditable.unity')
    flow = next(k for k,t in scene.docs.items() if t.startswith('--- !u!1 ') and scene.get(k,'m_Name')=='UIFlow')
    controller = scene.mono(flow,'RoomArt')
    # 替换所有现有共享房间贴图引用，保留 Room ID 及白天/黑夜规则。
    room_files = {100001:('女主客房1 白天.png','女主客房1 黑夜.png'),100002:('宴会大厅 白天.png','宴会大厅 夜晚.png'),100003:('中控台白天.png','中控台 黑夜.png'),100005:('厨房 白天.png','厨房 黑夜.png'),100006:('收藏厅 白天.png','收藏厅 黑夜.png'),100023:('议会厅 白天.png','议会厅 黑夜.png')}
    text = scene.docs[controller]
    for room, names in room_files.items():
        match = re.search(r'  - Room: '+str(room)+r'\n    Day: .*?guid: (\w+).*?\n    Night: .*?guid: (\w+)',text)
        assert match, room
        for old,name in zip(match.groups(),names):
            remap[old] = guid(art/'Scene_update'/name)
    for field,name in [('CorridorDay','走廊 白天.png'),('CorridorNight','走廊 黑夜.png'),('Floor1Background','古堡地图1层.png'),('Floor2Background','古堡地图2层.png')]:
        old = re.search(r'guid: (\w+)',scene.get(controller,field))[1]
        remap[old] = guid(art/'Scene_update'/name)
    files = [scene] + [UnityFile(p.relative_to(ROOT)) for p in (ROOT/'Assets/Castle/Prefabs').rglob('*.prefab')]
    for asset in files:
        for k,t in asset.docs.items():
            asset.docs[k] = re.sub(r'guid: (\w+)',lambda m:'guid: '+remap.get(m[1],m[1]),t)
    journal = scene.find(flow,'Canvas/Stage/Overlays/Journal')
    j = lambda path: scene.find(journal,path)
    # 1920×1080 参考图转为当前 1600×900 画布，逐节点保存可编辑位置。
    scene.place(j('Cover'),65.833,0,1529.167,896.667)
    scene.place(j('LeftCards'),226.667,111.667,520,680)
    scene.place(j('RightCards'),865,145,520,640)
    scene.place(j('Details/CharacterDecoration/PhotoFrame'),865,180,190,215)
    scene.place(j('Details/CharacterDecoration/Head'),887,202,142,162)
    scene.place(j('Details/CharacterDecoration/Tape'),950,151,241,82)
    scene.place(j('Details/CharacterDecoration/Heading'),1231,63,170,50)
    scene.place(j('Details/CharacterDecoration/Divider'),910,85,309,17.5)
    scene.active(j('Details/CharacterDecoration/Note'),False)
    scene.place(j('Details/RecordDecoration/SourcePaper'),841,36,239,260)
    for i in range(3):
        tab = j('Tab'+str(i))
        scene.place(tab,201.667+i*62.5,28.333,59.167,67.5)
        image = scene.mono(tab,'m_Sprite')
        scene.set(image,'m_Sprite',ref(ui/'调查-角色/button_diaocha_biaoqian_1.png',True,-8720334283676733600))
        scene.set(image,'m_Color','{r: 1, g: 1, b: 1, a: 1}')
        scene.set(scene.mono(tab,'m_Transition'),'m_Transition',0)
        for child in scene.children(tab):
            scene.active(child,scene.get(child,'m_Name')=='BookmarkIcon')
        icon = scene.find(tab,'BookmarkIcon')
        sizes = [(31.667,47.5),(40.833,35.833),(26.667,47.5)]
        w,h=sizes[i]
        scene.place(icon,(59.167-w)/2,16,w,h)
    # 新增空白纸张覆盖层：复用同一书页上方的纸纹 UV，不生成美术文件。
    map_go = scene.find(flow,'Canvas/Stage/Overlays/Map')
    raw_template = scene.find(map_go,'FloorImage')
    clean = scene.clone(raw_template,journal,'PlainRightPage')
    scene.place(clean,875,340,510,415)
    raw = scene.mono(clean,'m_Texture')
    scene.set(raw,'m_Texture',ref(ui/'调查-角色/bg_diaocha.png'))
    scene.docs[raw]=re.sub(r'(?ms)^  m_UVRect:.*?(?=^\S|\Z)', '  m_UVRect:\n    serializedVersion: 2\n    x: 0.55\n    y: 0.70\n    width: 0.31\n    height: 0.20\n', scene.docs[raw])
    scene.set(raw,'m_Color','{r: 1, g: 1, b: 1, a: 1}')
    scene.set(raw,'m_RaycastTarget',0)
    scene.active(clean,False)
    scene.bind(journal,'PlainRightPage',raw)
    # 覆盖层位于书封之后、其余交互与内容之前。
    parent = scene.component(journal,224)
    tr = scene.component(clean,224)
    scene.docs[parent]=scene.docs[parent].replace(f'  - {{fileID: {tr}}}\n','')
    cover_tr=scene.component(j('Cover'),224)
    scene.docs[parent]=scene.docs[parent].replace(f'  - {{fileID: {cover_tr}}}\n',f'  - {{fileID: {cover_tr}}}\n  - {{fileID: {tr}}}\n')
    m = lambda path:scene.find(map_go,path)
    scene.place(m('Panel'),0,0,1600,900)
    panel_image=scene.mono(m('Panel'),'m_Sprite')
    scene.set(panel_image,'m_Sprite',ref(ui/'地图/2.png',True))
    scene.set(panel_image,'m_Color','{r: 1, g: 1, b: 1, a: 1}')
    scene.set(panel_image,'m_RaycastTarget',0)
    scene.place(m('FloorImage'),222,93,1100,685)
    scene.place(m('Rooms'),222,93,1100,685)
    scene.place(m('Floor1'),1437,105,132,67)
    scene.place(m('Floor2'),1415,205,155,80)
    for n,sprite in [('Floor1','从选区-1.png'),('Floor2','从选区.png')]:
        scene.set(scene.mono(m(n),'m_Sprite'),'m_Sprite',ref(ui/'地图'/sprite,True))
    scene.place(m('Floor1/Floor1Label'),34,8,80,45)
    scene.place(m('Floor2/Floor2Label'),52,13,80,45)
    scene.place(m('TooltipPanel'),0,0,300,165)
    scene.place(m('TooltipPanel/Tooltip'),18,12,264,141)
    scene.set(scene.mono(m('TooltipPanel'),'m_RaycastTarget'),'m_RaycastTarget',0)
    scene.set(scene.mono(m('TooltipPanel/Tooltip'),'m_RaycastTarget'),'m_RaycastTarget',0)
    for name in ('Close','Details','Hint','Heading','Enter'):
        scene.active(m(name),False)
    dismiss=scene.clone(m('Close'),map_go,'Dismiss',first=True)
    scene.place(dismiss,0,0,1600,900)
    scene.active(dismiss,True)
    dismiss_image=scene.mono(dismiss,'m_Sprite')
    scene.set(dismiss_image,'m_Sprite','{fileID: 0}')
    scene.set(dismiss_image,'m_Color','{r: 0, g: 0, b: 0, a: 0}')
    scene.set(dismiss_image,'m_RaycastTarget',1)
    scene.bind(map_go,'Dismiss',scene.mono(dismiss,'m_OnClick'))
    # 导航空白处可关闭；地图底图不拦射线，房间与楼层按钮优先。
    scene.set(scene.mono(m('FloorImage'),'m_RaycastTarget'),'m_RaycastTarget',0)
    for field,value in [('Location','你的房间'),('CurrentTime','Day 1 · 17:57')]:
        go=scene.find(flow,'Canvas/Stage/Hud/bg_location/'+field)
        scene.set(scene.mono(go,'m_Text'),'m_Text',value)
    for asset in files:
        asset.save()
    # 模板各自保留 GUID，只微调已绑定节点。
    for name in ('BookPersonCard','BookItemCard','BookRecordCard'):
        prefab=UnityFile('Assets/Castle/Prefabs/V24/'+name+'.prefab')
        root=next(k for k,t in prefab.docs.items() if t.startswith('--- !u!1 ') and prefab.get(k,'m_Name')==name)
        if name=='BookPersonCard':
            for path,box in {'Frame':(0,0,235,315),'Head':(24,43,185,210),'Title':(18,273,200,34)}.items():
                prefab.place(prefab.find(root,path),*box)
        if name=='BookRecordCard':
            for path,box in {'Folder':(0,0,168,169),'Tape':(-10,96,241,82),'Title':(170,128,155,44),'Body':(330,75,190,100)}.items():
                prefab.place(prefab.find(root,path),*box)
        prefab.save()
    # 新指针使用整理目录，缩小约 20%，不改动原 PNG。
    cursor=ROOT/ui/'交谈界面/iocon_talking_mouse.png.meta'
    meta=cursor.read_text(encoding='utf-8')
    for field,value in [('textureType',7),('isReadable',1),('maxTextureSize',26),('textureCompression',0),('enableMipMap',0)]:
        meta=re.sub(r'(?m)^(\s*'+field+r':) .*$',rf'\1 {value}',meta)
    cursor.write_text(meta,encoding='utf-8',newline='\n')
    print('Migrated scene, prefabs, cursor; remapped',len(remap),'asset GUID references.')


def inspect():
    scene = UnityFile('Assets/Castle/Scenes/CastleEditable.unity')
    flow = next(key for key, text in scene.docs.items() if text.startswith('--- !u!1 ') and scene.get(key, 'm_Name') == 'UIFlow')
    for page in ('Journal','Map'):
        go = scene.find(flow, 'Canvas/Stage/Overlays/'+page)
        print(page, go)
        def walk(node, depth):
            name = scene.get(node,'m_Name')
            rect = scene.component(node,224)
            monos = [k for k,t in scene.docs.items() if t.startswith('--- !u!114 ') and f'  m_GameObject: {{fileID: {node}}}\n' in t]
            print(' '*depth, node, name, scene.get(rect,'m_AnchoredPosition') if rect else '', scene.get(rect,'m_SizeDelta') if rect else '', monos)
            if depth < 3:
                for child in scene.children(node):
                    walk(child,depth+1)
        walk(go,0)


if __name__ == '__main__':
    import sys
    migrate() if '--apply' in sys.argv else inspect()

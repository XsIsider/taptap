using System;
using System.IO;
using System.Linq;
using Castle.Flow;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Castle.Editor
{
    public static class CastleFlowSceneBuilder
    {
        public const string ScenePath = "Assets/Castle/Scenes/CastleEditable.unity";
        public const string ContentPath = "Assets/Castle/Resources/Castle/FlowContent.asset";
        private static Font _font;
        private static TMP_FontAsset _linkFont;
        private static CastleFlowUi _ui;
        private static readonly Color Ink = new Color32(24, 35, 29, 255), Board = new Color32(39, 62, 47, 255), Paper = new Color32(220, 204, 166, 255), Light = new Color32(236, 231, 210, 255), Gold = new Color32(218, 184, 119, 255);

        [MenuItem("Tools/Castle/Install Confirmed UI Flow")]
        public static void Install()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath);
            if (UnityEngine.Object.FindObjectOfType<CastleFlowUi>(true)) { Debug.Log("UI flow already installed; preserving designer edits."); return; }
            if (!Directory.Exists("Assets/TextMesh Pro"))
                AssetDatabase.ImportPackage("Library/PackageCache/com.unity.textmeshpro@3.0.7/Package Resources/TMP Essential Resources.unitypackage", false);
            _font = Resources.Load<Font>("Castle/Chinese");
            _linkFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Castle/Resources/Castle/FlowChinese.asset");
            if (!_linkFont || !_linkFont.material || !_linkFont.atlasTexture)
            {
                var generated = TMP_FontAsset.CreateFontAsset(_font, 36, 5, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 2048, 2048);
                if (_linkFont) { EditorUtility.CopySerialized(generated, _linkFont); UnityEngine.Object.DestroyImmediate(generated); }
                else { _linkFont = generated; AssetDatabase.CreateAsset(_linkFont, "Assets/Castle/Resources/Castle/FlowChinese.asset"); }
                _linkFont.name = "FlowChinese";
                AssetDatabase.AddObjectToAsset(_linkFont.material, _linkFont);
                foreach (var texture in _linkFont.atlasTextures) AssetDatabase.AddObjectToAsset(texture, _linkFont);
                EditorUtility.SetDirty(_linkFont); AssetDatabase.SaveAssets();
            }
            var content = AssetDatabase.LoadAssetAtPath<CastleFlowContent>(ContentPath);
            if (!content) { content = CastleFlowSample.Create(); AssetDatabase.CreateAsset(content, ContentPath); }
            var old = new GameObject("LegacyPrototype_Disabled");
            foreach (var go in scene.GetRootGameObjects().Where(go => go.GetComponent<CastleGame>() || go.GetComponent<CastleSceneUi>())) go.transform.SetParent(old.transform);
            old.SetActive(false);
            var root = new GameObject("UIFlow", typeof(CastleFlowController), typeof(AudioSource));
            var canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CastleFlowUi));
            canvas.transform.SetParent(root.transform, false);
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1600,900); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            _ui = canvas.GetComponent<CastleFlowUi>();
            var stage = Rect(canvas.transform, "Stage", 0,0,1600,900);
            stage.anchorMin = stage.anchorMax = stage.pivot = new Vector2(.5f,.5f); stage.anchoredPosition = Vector2.zero;
            Templates();
            var pages = Rect(stage,"Pages",0,0,1600,900);
            _ui.Pages = new[] { "Title", "Invitation", "Explore", "Recordings", "Anchors", "Space", "Timeline" }.Select(id => View(pages,id)).ToArray();
            Opening(); Workbench(); Ending();
            _ui.Hud = View(stage,"Hud");
            Label(_ui.Hud,"Location","走廊  /  18:58",32,26,670,52,24);
            Btn(_ui.Hud,"Back","返回场景",720,26,180,52);
            Btn(_ui.Hud,"Map","地图",934,26,170,52); Btn(_ui.Hud,"Journal","调查册",1120,26,200,52); Btn(_ui.Hud,"Settings","设置",1336,26,210,52);
            var overlays = Box(stage,"Overlays",0,0,1600,900,new Color(0,0,0,.68f),true); _ui.ModalRoot = overlays.gameObject;
            _ui.Modals = new[] { "Map", "Journal", "Acquire", "Letter", "Settings", "Confirm" }.Select(id=>View(overlays,id)).ToArray();
            Modals();
            _ui.Toast = View(stage,"Toast"); Box(_ui.Toast.transform,"Back",250,818,1100, height:60, color:Ink);
            Label(_ui.Toast,"Body","提示",270,824,1060,48,21);
            var game = root.GetComponent<CastleFlowController>(); game.Content = content; game.Ui = _ui; game.Audio = root.GetComponent<AudioSource>(); game.Audio.playOnAwake = false;
            foreach (var view in _ui.Pages) view.gameObject.SetActive(view.Id == "Title");
            foreach (var view in _ui.Modals) view.gameObject.SetActive(false);
            _ui.ModalRoot.SetActive(false); _ui.Hud.gameObject.SetActive(false); _ui.Toast.gameObject.SetActive(false);
            _ui.ValidateBindings(); AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene);
            Debug.Log("CASTLE FLOW SCENE SAVED: " + ScenePath);
        }

        private static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            var rect = new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent,false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0,1); rect.anchoredPosition = new Vector2(x,-y); rect.sizeDelta = new Vector2(width,height); return rect;
        }
        private static RectTransform Box(Transform parent, string name, float x, float y, float width, float height, Color color, bool raycast = false)
        {
            var rect = Rect(parent,name,x,y,width,height); var image = rect.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = raycast; return rect;
        }
        private static void Bind(CastleUiView view, string id, Component component) => view.Controls.Add(new CastleUiBinding { Id = id, Target = component });
        private static CastleUiView View(Transform parent,string id)
        {
            var view = Rect(parent,id,0,0,1600,900).gameObject.AddComponent<CastleUiView>(); view.Id=id; return view;
        }
        private static Text Label(CastleUiView view,string id,string text,float x,float y,float width,float height,int size=23,Transform parent=null,Color? color=null)
        {
            var label = Rect(parent ? parent : view.transform,id,x,y,width,height).gameObject.AddComponent<Text>(); label.font=_font; label.text=text; label.fontSize=size; label.color=color??Light; label.raycastTarget=false; label.horizontalOverflow=HorizontalWrapMode.Wrap; label.verticalOverflow=VerticalWrapMode.Truncate;
            Bind(view,id,label); return label;
        }
        private static Button Btn(CastleUiView view,string id,string text,float x,float y,float width,float height,Transform parent=null)
        {
            var rect=Box(parent?parent:view.transform,id,x,y,width,height,Board,true); var button=rect.gameObject.AddComponent<Button>(); button.targetGraphic=rect.GetComponent<Image>(); Bind(view,id,button);
            var label=Label(view,id+"Label",text,8,5,width-16,height-10,Mathf.Min(22,Mathf.FloorToInt((height-10)/1.5f)),rect);label.alignment=TextAnchor.MiddleCenter; return button;
        }
        private static RectTransform Group(CastleUiView view,string id,float x,float y,float width,float height,Transform parent=null)
        { var rect=Rect(parent?parent:view.transform,id,x,y,width,height);Bind(view,id,rect);return rect; }
        private static void Background(CastleUiView view,string resource,bool tint=true)
        {
            var image=Rect(view.transform,"Backdrop",0,0,1600,900).gameObject.AddComponent<RawImage>();image.texture=Resources.Load<Texture2D>("Castle/"+resource);image.raycastTarget=false;
            if(tint)Box(view.transform,"Tint",0,0,1600,900,new Color(.05f,.08f,.06f,.78f));
        }
        private static RectTransform List(CastleUiView view,string id,float x,float y,float width,float height,float rowHeight,Transform parent=null)
        {
            var scrollRoot=Rect(parent?parent:view.transform,id+"Scroll",x,y,width,height); var scroll=scrollRoot.gameObject.AddComponent<ScrollRect>();
            var viewport=Box(scrollRoot,"Viewport",0,0,width-16,height,new Color(1,1,1,.015f),true);viewport.gameObject.AddComponent<RectMask2D>();
            var content=Rect(viewport,id,0,0,width-22,height);Bind(view,id,content);
            var layout=content.gameObject.AddComponent<VerticalLayoutGroup>();layout.spacing=12;layout.childControlWidth=true;layout.childControlHeight=true;layout.childForceExpandHeight=false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=35;Bind(view,id+"Scroll",scroll);
            var rail=Box(scrollRoot,"Scrollbar",width-9,0,9,height,new Color(1,1,1,.12f),true);
            var handle=Box(rail,"Handle",0,0,9,height,Gold,true);handle.anchorMin=Vector2.zero;handle.anchorMax=Vector2.one;handle.sizeDelta=Vector2.zero;handle.anchoredPosition=Vector2.zero;
            var bar=rail.gameObject.AddComponent<Scrollbar>();bar.direction=Scrollbar.Direction.BottomToTop;bar.handleRect=handle;bar.targetGraphic=handle.GetComponent<Image>();scroll.verticalScrollbar=bar;scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
            return content;
        }
        private static Slider Slide(CastleUiView view,string id,float x,float y,float width,Transform parent=null)
        {
            var rect=Rect(parent?parent:view.transform,id,x,y,width,34);var slider=rect.gameObject.AddComponent<Slider>();
            Box(rect,"Rail",0,15,width,4,Paper,true);
            var area=Rect(rect,"HandleArea",12,0,width-24,34);
            var handle=Box(area,"Diamond",0,0,22,34,Gold,true);handle.pivot=new Vector2(.5f,.5f);handle.sizeDelta=new Vector2(22,0);
            slider.handleRect=handle;slider.targetGraphic=handle.GetComponent<Image>();Bind(view,id,slider);return slider;
        }

        private static void Templates()
        {
            const string folder="Assets/Castle/Prefabs/Flow";Directory.CreateDirectory(folder);
            var view=View(null,"Card");((RectTransform)view.transform).sizeDelta=new Vector2(460,132);
            var image=view.gameObject.AddComponent<Image>();image.color=Board;image.raycastTarget=true;Bind(view,"Background",image);
            var button=view.gameObject.AddComponent<Button>();button.targetGraphic=image;Bind(view,"Select",button);
            var layout=view.gameObject.AddComponent<LayoutElement>();layout.preferredHeight=132;
            Bind(view,"Pointer",view.gameObject.AddComponent<FlowPointer>());Bind(view,"Drag",view.gameObject.AddComponent<FlowDrag>());
            var title=Label(view,"Title","条目名称",16,12,420,36,24,color:Gold);StretchWidth(title.rectTransform,16,12,36);
            var body=Label(view,"Body","条目说明",16,56,420,68,20);StretchWidth(body.rectTransform,16,56,68);
            _ui.CardPrefab=SavePrefab(view,folder+"/FlowCard.prefab");
            view=View(null,"Track");((RectTransform)view.transform).sizeDelta=new Vector2(940,136);view.gameObject.AddComponent<LayoutElement>().preferredHeight=136;
            Box(view.transform,"Back",0,0,940,136,Ink);
            Label(view,"Name","文件 01",18,10,240,38,23);Label(view,"Offset","+0 秒",285,12,250,36,22);
            Btn(view,"Reference","设为参考",570,8,190,40);Btn(view,"Remove","移除",775,8,135,40);Slide(view,"OffsetSlider",30,76,870);
            _ui.TrackPrefab=SavePrefab(view,folder+"/FlowTrack.prefab");
            view=View(null,"DialogueLine");((RectTransform)view.transform).sizeDelta=new Vector2(760,144);view.gameObject.AddComponent<LayoutElement>().preferredHeight=144;
            Label(view,"Speaker","管家",4,4,730,30,21,color:Gold);
            var text=Rect(view.transform,"Body",4,42,730,94).gameObject.AddComponent<TextMeshProUGUI>();text.font=_linkFont;text.fontSize=24;text.color=Light;text.raycastTarget=true;text.text="欢迎来到古堡，点击关键词可以查看详情。";text.overflowMode=TextOverflowModes.Overflow;
            Bind(view,"Body",text);text.gameObject.AddComponent<FlowTextLink>();
            _ui.LinePrefab=SavePrefab(view,folder+"/DialogueLine.prefab");
        }
        private static void StretchWidth(RectTransform rect,float margin,float y,float height)
        { rect.anchorMin=new Vector2(0,1);rect.anchorMax=new Vector2(1,1);rect.sizeDelta=new Vector2(-margin*2,height);rect.anchoredPosition=new Vector2(margin,-y); }
        private static CastleUiView SavePrefab(CastleUiView view,string path)
        { PrefabUtility.SaveAsPrefabAsset(view.gameObject,path);UnityEngine.Object.DestroyImmediate(view.gameObject);return AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<CastleUiView>(); }

        private static void Opening()
        {
            var view=_ui.View("Title");Background(view,"Castle");Label(view,"Title","古堡",100,145,1200,100,80,color:Gold);Label(view,"Subtitle","声音与时间的谜题",108,255,1050,50,28);
            string[] ids={"Start","Continue","Settings","Credits","Quit"};string[] labels={"开始游戏","继续游戏","设置","制作名单","退出"};for(int i=0;i<ids.Length;i++)Btn(view,ids[i],labels[i],110,360+i*85,370, height:62);
            view=_ui.View("Invitation");Background(view,"Castle");
            var surface=Btn(view,"Surface","",0,0,1600,900);surface.targetGraphic.color=new Color(0,0,0,.01f);
            Box(view.transform,"Letter",390,185,820,530,Paper);Label(view,"Heading","一封来自古堡的邀请函",455,255,690,90,38,color:Ink);Label(view,"Body","点击邀请函展开",455,383,690,250,27,color:Ink);
            view=_ui.View("Explore");Background(view,"EmptyRoom");Label(view,"RoomName","走廊",85,116,640,60,36,color:Gold);
            Label(view,"Portrait","拉帕莉亚  ·  管家",85,385,500,100,36,color:Gold);
            List(view,"People",60,535,520,280,132);
            var free=Group(view,"FreePanel",660,190,850,600);Box(free,"Back",0,0,850,600,Ink);Label(view,"RoomHint","在走廊探索或与人物交谈。",32,38,786,170,27,free);
            Btn(view,"Talk","与管家交谈",32,258,350, height:60,parent:free);Btn(view,"Map","查看地图",416,258,400,60,free);Btn(view,"ExploreRoom","探索房间",32,358,350,60,free);Btn(view,"Workbench","录音工作台",416,358,400,60,free);
            var dialogue=Group(view,"DialoguePanel",660,135,850,670);Box(dialogue,"Back",0,0,850,670,Ink);
            var content=List(view,"Dialogue",25,26,800,390,144,dialogue);
            var preview=UnityEngine.Object.Instantiate(_ui.LinePrefab,content);preview.name="PreviewLine";
            var choices=Group(view,"Choices",25,435,800,95,dialogue);var horizontal=choices.gameObject.AddComponent<HorizontalLayoutGroup>();horizontal.spacing=12;horizontal.childControlHeight=true;horizontal.childControlWidth=true;horizontal.childForceExpandWidth=true;
            Btn(view,"Auto","自动：关闭",25,565,225, height:55,parent:dialogue);Btn(view,"Continue","继续 →",520,565,280,55,dialogue);Btn(view,"ExitTape","退出磁带",265,565,240,55,dialogue);
        }

        private static void BoardPage(CastleUiView view)
        {
            Background(view,"ControlRoom");Box(view.transform,"Board",88,132,1424,670,Board);
            Btn(view,"Recordings","录音",130,150,170,52);Btn(view,"Anchors","锚点",314,150,170,52);Btn(view,"Space","空间",498,150,170,52);
            Btn(view,"PreviousPuzzle","‹",745,156,50,42);Label(view,"PuzzleName","事件 1 / 3",820,160,520,40,24);Btn(view,"NextPuzzle","›",1392,156,50,42);
            Box(view.transform,"Divider",115,218,1360,2,Paper);Box(view.transform,"SideDivider",355,220,2,493,Paper);
            List(view,"List",128,239,210,470,132);
        }
        private static void Workbench()
        {
            var view=_ui.View("Recordings");BoardPage(view);Label(view,"FileInfo","文件 01 · 时间与地点",385,233,1070,50,24);
            List(view,"Lines",385,300,1060,350,132);Label(view,"Playback","文字回放",540,718,660,64,20);
            Btn(view,"Play","播放",390,667,140,50);Slide(view,"Progress",552,674,610);Btn(view,"Done","标记完成",1225,723,220,50);
            view=_ui.View("Anchors");BoardPage(view);
            var drop=Box(view.transform,"TrackDropArea",380,235,1080,365,new Color(1,1,1,.03f),true);Bind(view,"DropZone",drop.gameObject.AddComponent<FlowDrag>());
            List(view,"Tracks",10,7,1050,349,136,drop);
            Label(view,"Details","文本对照 · 选择左侧锚点查看",390,618,1050,83,22);Btn(view,"Add","加入轨道",125,723,220,50);
            Label(view,"Status","先匹配，再校准",390,728,560,45,22);Btn(view,"Check","检查匹配",976,723,218,50);Btn(view,"Next","空间匹配 →",1210,723,230,50);
            view=_ui.View("Space");BoardPage(view);MapArea(view,385,291,748,421);Label(view,"Details","设备信息",1162,290,280,335,22);
            Btn(view,"Floor1","1F",905,238,100,40);Btn(view,"Floor2","2F",1021,238,100,40);Btn(view,"Verify","验证并收录线索",1140,728,305,50);
        }
        private static void MapArea(CastleUiView view,float x,float y,float width,float height)
        {
            var back=Rect(view.transform,"FloorImage",x,y,width,height).gameObject.AddComponent<RawImage>();back.texture=Resources.Load<Texture2D>("Castle/Floor1");back.uvRect=new Rect(210f/1672,181f/941,1280f/1672,600f/941);back.raycastTarget=false;Bind(view,"FloorImage",back);
            Group(view,"Rooms",x,y,width,height);
            var panel=Box(view.transform,"TooltipPanel",x+25,y+height-185,width-50,180,Ink);Bind(view,"TooltipPanel",panel);
            Label(view,"Tooltip","名称 / 开放时间 / 未知设备",14,12,width-78,156,21,panel,Gold);panel.gameObject.SetActive(false);
        }
        private static void Ending()
        {
            var view=_ui.View("Timeline");Background(view,"ControlRoom");Label(view,"Heading","线索时间线",85,125,1400,70,40,color:Gold);
            var sort=Group(view,"SortRoot",0,0,1600,900);List(view,"Clues",95,255,480,450,132,sort);
            var slots=Group(view,"Slots",635,350,850,240,sort);var layout=slots.gameObject.AddComponent<HorizontalLayoutGroup>();layout.spacing=20;layout.childControlWidth=true;layout.childControlHeight=true;
            Label(view,"Hint","拖动线索进行排序",640,620,845,95,25,sort);Btn(view,"Submit","提交时间线",1175,760,310,65,sort);
            var reveal=Group(view,"RevealRoot",0,0,1600,900);var wave=Group(view,"Wave",95,270,1390,250,reveal);
            for(int i=0;i<80;i++){float height=30+Mathf.Abs(Mathf.Sin(i*.51f)*Mathf.Cos(i*.11f))*160;var bar=Box(wave,"Bar"+i,16+i*17,125,6,height,Gold);bar.pivot=new Vector2(.5f,.5f);}
            Label(view,"EndingStatus","事件还原",95,220,1390,45,25,reveal);Label(view,"EndingText","对应的对白文字将在波形下呈现。",95,550,1390,155,31,reveal);
            Slide(view,"Progress",275,742,890,reveal);Btn(view,"Play","暂停",95,731,150,55,reveal);Btn(view,"Replay","重播",1220,731,265,55,reveal);
        }

        private static void Modals()
        {
            var view=_ui.View("Map");Box(view.transform,"Panel",60,60,1480,775,Ink);Label(view,"Heading","古堡地图",95,95,1000, height:55,size:35);Btn(view,"Close","关闭",1310,90,180,55);
            MapArea(view,100,218,1000,563);Btn(view,"Floor1","1F",885,162,95,42);Btn(view,"Floor2","2F",1000,162,95,42);Label(view,"Details","地点详情",1140,220,340,320,24);Label(view,"Hint","选择地点后进入",1140,575,340,115,22);Btn(view,"Enter","进入",1140,710,340, height:55);
            view=_ui.View("Journal");Box(view.transform,"Cover",95,82,1410,740,new Color(.24f,.13f,.08f));Box(view.transform,"LeftPage",123,115,665,650,Paper);Box(view.transform,"RightPage",793,115,685,650,Paper);Box(view.transform,"Spine",787,115,6,650,new Color(.43f,.33f,.19f));
            Btn(view,"Close","合上调查册",1225,35,265, height:50);
            for(int i=0;i<3;i++)Btn(view,"Tab"+i,new[]{"人物","道具","记录"}[i],160+i*205,120,180,50);
            Label(view,"Category","人物",165,190,450,50,30,color:Ink);Label(view,"PageNumber","1 / 1",1170,712,200,40,22,color:Ink);Label(view,"Empty","",165,285,500,140,24,color:Ink);
            var left=Group(view,"LeftCards",165,265,510,450);var grid=left.gameObject.AddComponent<GridLayoutGroup>();grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=2;grid.cellSize=new Vector2(230,205);grid.spacing=new Vector2(16,14);
            var right=Group(view,"RightCards",865,265,510,450);grid=right.gameObject.AddComponent<GridLayoutGroup>();grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=1;grid.cellSize=new Vector2(486,205);grid.spacing=new Vector2(16,14);
            var detail=Group(view,"Details",850,194,555,440);Label(view,"DetailTitle","人物 / 线索详情",0,0,555,70,32,detail,Ink);Label(view,"DetailBody","左侧选择条目查看详情。",0,100,555,320,26,detail,Ink);
            Btn(view,"Previous","‹ 上一页",160,772,220,40);Btn(view,"Next","下一页 ›",1210,772,220,40);Btn(view,"Timeline","收录完成 · 还原时间线",865,652,505,50);
            var sheet=Box(view.transform,"FlipSheet",793,115,685,650,new Color(.90f,.83f,.68f,.93f));Bind(view,"FlipSheet",sheet);sheet.gameObject.SetActive(false);
            view=_ui.View("Acquire");Box(view.transform,"Panel",300,230,1000,430,Ink);Label(view,"Heading","获得道具 / 线索",340,261,900,50,30,color:Gold);Box(view.transform,"IconBack",345,345,210,225,Board);Label(view,"Icon","邀请函",365,420,170,70,35);
            Label(view,"Name","邀请函",605,340,630,50,32,color:Gold);Label(view,"Body","道具介绍",605,406,630,138,22);Label(view,"Filed","已收入调查册 · 道具",605,555,620,38,21);Btn(view,"Confirm","确认",960,596,270, height:50);
            view=_ui.View("Letter");Box(view.transform,"Paper",355,125,890,620,Paper);Label(view,"Body","邀请函",420,195,760,430,30,color:Ink);Btn(view,"Close","返回调查册",830,670,340,50);
            view=_ui.View("Settings");Box(view.transform,"Panel",360,150,880,630,Ink);Label(view,"Heading","设置",410,180,780,60,38,color:Gold);Label(view,"SpeedLabel","自动对白间隔",410,280,300,45,25);Slide(view,"Speed",725,283,430);Label(view,"SpeedValue","3 秒 / 句",725,332,430, height:40,size:21);
            Label(view,"VolumeLabel","音量",410,405,300,45,25);Slide(view,"Volume",725,407,430);Btn(view,"Motion","翻页动效：开启",410,515,740, height:60);Btn(view,"Title","返回标题",410,660,350,60);Btn(view,"Close","继续",800,660,350,60);
            view=_ui.View("Confirm");Box(view.transform,"Panel",375,240,850,380,Ink);Label(view,"Body","确认操作",415,285,770,160,28);Btn(view,"Cancel","取消",415,520,350,60);Btn(view,"Confirm","确认",810,520,350,60);
        }
    }
}

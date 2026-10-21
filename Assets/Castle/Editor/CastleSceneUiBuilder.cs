using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace Castle.Editor
{
    // Runs only in the editor. The saved scene is the runtime source of UI objects.
    public static class CastleSceneUiBuilder
    {
        public const string ScenePath = "Assets/Castle/Scenes/CastleEditable.unity";
        const string PrefabPath = "Assets/Castle/Prefabs/JournalCard.prefab";
        static Font font;
        static CastleSceneUi ui;
        static CastleDatabase db;
        static readonly Color Ink = new Color32(20,32,25,255), Gold = new Color32(210,181,127,255), Light = new Color32(229,230,213,255), Board = new Color32(38,62,47,255);
        [MenuItem("Tools/Castle/Create Editable Scene If Missing")]
        public static void CreateIfMissing()
        {
            if (File.Exists(ScenePath)) return; // Never overwrite a designer's scene.
            font = Resources.Load<Font>("Castle/Chinese"); db = Resources.Load<CastleDatabase>("Castle/Database");
            if (!font || !db) throw new InvalidOperationException("Castle font/database missing.");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)); camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0,0,-10); camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor; camera.GetComponent<Camera>().backgroundColor = Ink;
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            var game = new GameObject("CastleGame", typeof(CastleGame), typeof(AudioSource));
            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CastleSceneUi));
            ui = canvasGo.GetComponent<CastleSceneUi>(); ui.Canvas = canvasGo.GetComponent<Canvas>(); ui.Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1600,900); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            ui.Stage = R(canvasGo.transform,"Stage",0,0,1600,900); ui.Stage.anchorMin = ui.Stage.anchorMax = ui.Stage.pivot = new Vector2(.5f,.5f); ui.Stage.anchoredPosition = Vector2.zero;
            ui.Audio = game.GetComponent<AudioSource>(); ui.Audio.playOnAwake = false;
            game.GetComponent<CastleGame>().database = db; game.GetComponent<CastleGame>().ui = ui;
            var pages = R(ui.Stage,"Pages",0,0,1600,900);
            ui.Pages = Enum.GetValues(typeof(ScreenId)).Cast<ScreenId>().Select(id=>View(pages,id.ToString())).ToArray();
            Exploration(); Workbench(); Resolution();
            ui.SceneHud = View(ui.Stage,"SceneHud"); Hud();
            var overlays = Box(ui.Stage,"Overlays",0,0,1600,900,new Color(0,0,0,.65f)); overlays.GetComponent<Image>().raycastTarget = true; ui.OverlayRoot = overlays.gameObject;
            ui.Overlays = new[]{"Navigation","Settings","Journal","JournalDetail","InvitationArchive","Message"}.Select(id=>View(overlays,id)).ToArray();
            Overlays();
            var toast = Box(ui.Stage,"Toast",360,18,880,65,Ink); ui.ToastRoot = toast.gameObject; ui.ToastText = Text(toast,"ToastText","提示",20,10,840,45,23,Gold); toast.gameObject.SetActive(false);
            foreach (var view in ui.Pages) view.gameObject.SetActive(view.Id == "Title");
            foreach (var view in ui.Overlays) view.gameObject.SetActive(false);
            ui.SceneHud.gameObject.SetActive(false); ui.OverlayRoot.SetActive(false);
            ui.ValidateBindings(); Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene,ScenePath); AssetDatabase.SaveAssets(); WriteBindingGuide();
            Debug.Log("CASTLE EDITABLE SCENE CREATED: " + ScenePath);
        }
        static void WriteBindingGuide()
        {
            var text = new System.Text.StringBuilder("# CastleEditable 实际场景绑定清单\n\n由创建场景工具导出，对应已保存的原生 uGUI 场景。修改布局不需要修改 Id，也不需要修改脚本。\n\n");
            text.Append("`CastleGame` 挂 CastleGame、AudioSource；`Canvas` 挂 Canvas、CanvasScaler、GraphicRaycaster、CastleSceneUi；`EventSystem` 挂 EventSystem、StandaloneInputModule。\n\n");
            foreach(var view in ui.Pages.Concat(ui.Overlays).Concat(new[]{ui.SceneHud}))
            {
                text.Append("## ").Append(view.Id).Append("\n\n面板脚本：`CastleUiView`。其 Controls 列表使用以下 Id 与组件引用。\n\n| Id | 绑定组件 | 相对面板的对象路径 |\n|---|---|---|\n");
                foreach(var b in view.Controls)
                {
                    var t=b.Target.transform;var path=t.name;while(t.parent && t.parent!=view.transform){t=t.parent;path=t.name+"/"+path;}
                    text.Append('|').Append(b.Id).Append('|').Append(b.Target.GetType().Name).Append('|').Append(path).Append("|\n");
                }
                text.Append('\n');
            }
            text.Append("## 地图和卡片\n\n每个 Map 对象另挂 CastleMapView，Rooms 列表保存房间 ID、Button、Text、MarkerAnchor 和 Corridor 路点；Lines 与 Markers 已预置。移动按钮和路点即可修改地图布局。\n\nJournalCard.prefab 根对象挂 Image、Button、CastleJournalCardView，三个子对象为 CategoryText、TitleText、SummaryText，均挂 Text。Journal/GridScroll 的 Content 挂 GridLayoutGroup 与 ContentSizeFitter。卡片数量根据当前记录实例化。\n");
            Directory.CreateDirectory("Assets/Docs");File.WriteAllText("Assets/Docs/CastleEditable_绑定清单.md",text.ToString());
        }
        static CastleUiView V(ScreenId id) => ui.Page(id);
        static RectTransform R(Transform parent,string name,float x,float y,float w,float h)
        {
            var r = new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent,false);
            r.anchorMin = r.anchorMax = new Vector2(0,1); r.pivot = new Vector2(0,1); r.anchoredPosition = new Vector2(x,-y); r.sizeDelta = new Vector2(w,h); return r;
        }
        static RectTransform Box(Transform p,string name,float x,float y,float w,float h,Color color)
        { var r=R(p,name,x,y,w,h); var image=r.gameObject.AddComponent<Image>(); image.color=color; image.raycastTarget=false; return r; }
        static Text Text(Transform p,string name,string value,float x,float y,float w,float h,int size=24,Color? color=null,TextAnchor align=TextAnchor.UpperLeft)
        {
            float height=Mathf.Max(h,Mathf.Ceil(size*1.6f));
            var r=R(p,name,x,y-(height-h)*.5f,w,height); var t=r.gameObject.AddComponent<Text>(); t.font=font; t.text=value; t.fontSize=size; t.color=color??Light;
            t.alignment=align; t.horizontalOverflow=HorizontalWrapMode.Wrap; t.verticalOverflow=VerticalWrapMode.Truncate; t.raycastTarget=false; return t;
        }
        static void Bind(CastleUiView v,string id,Component target) { v.Controls.Add(new CastleUiBinding {Id=id,Target=target}); }
        static CastleUiView View(Transform parent,string id) { var v=R(parent,id,0,0,1600,900).gameObject.AddComponent<CastleUiView>(); v.Id=id; return v; }
        static Text L(CastleUiView v,string id,string value,float x,float y,float w,float h,int size=24,Transform parent=null,Color? color=null)
        { var t=Text(parent?parent:v.transform,id,value,x,y,w,h,size,color); Bind(v,id,t); return t; }
        static Button Btn(CastleUiView v,string id,string label,float x,float y,float w,float h,int size=24,Transform parent=null)
        {
            var r=Box(parent?parent:v.transform,id,x,y,w,h,new Color32(47,64,48,255)); var image=r.GetComponent<Image>(); image.raycastTarget=true;
            var b=r.gameObject.AddComponent<Button>(); b.targetGraphic=image; var colors=b.colors; colors.highlightedColor=new Color(1.2f,1.2f,1.1f); colors.pressedColor=new Color(.75f,.75f,.75f); colors.disabledColor=new Color(.5f,.5f,.5f,.6f); b.colors=colors;
            var t=Text(r,id+"Label",label,8,6,w-16,h-12,size,Light,TextAnchor.MiddleCenter);
            Bind(v,id,b); Bind(v,id+"Label",t); return b;
        }
        static RectTransform Group(CastleUiView v,string id,Transform p=null) { var r=R(p?p:v.transform,id,0,0,1600,900); Bind(v,id,r); return r; }
        static RawImage Pic(CastleUiView v,string id,string resource,float x=0,float y=0,float w=1600,float h=900,Transform p=null)
        { var r=R(p?p:v.transform,id,x,y,w,h); var image=r.gameObject.AddComponent<RawImage>(); image.texture=Resources.Load<Texture2D>("Castle/"+resource); image.raycastTarget=false; Bind(v,id,image); return image; }
        static RectTransform Panel(CastleUiView v,float x,float y,float w,float h) => Box(v.transform,"Panel",x,y,w,h,Ink);
        static Slider Slider(CastleUiView v,string id,float x,float y,float w,Transform p=null)
        {
            var r=R(p?p:v.transform,id,x,y,w,34); var s=r.gameObject.AddComponent<Slider>();
            var rail=Box(r,"Rail",0,14,w,5,new Color32(117,139,110,255)); rail.GetComponent<Image>().raycastTarget=true;
            var handle=Box(r,"Handle",0,0,22,34,Gold); handle.pivot=new Vector2(.5f,.5f); handle.anchoredPosition=Vector2.zero; handle.sizeDelta=new Vector2(22,0); handle.GetComponent<Image>().raycastTarget=true;
            s.handleRect=handle; s.targetGraphic=handle.GetComponent<Image>(); s.wholeNumbers=true; Bind(v,id,s); return s;
        }
        static ScrollRect Scroll(CastleUiView v,string id,float x,float y,float w,float h,float contentHeight,Transform parent=null)
        {
            var r=R(parent?parent:v.transform,id,x,y,w,h); var s=r.gameObject.AddComponent<ScrollRect>();
            var viewport=Box(r,"Viewport",0,0,w-22,h,new Color(1,1,1,.005f)); viewport.GetComponent<Image>().raycastTarget=true; viewport.gameObject.AddComponent<RectMask2D>();
            var content=R(viewport,"Content",0,0,w-22,contentHeight); s.viewport=viewport; s.content=content; s.horizontal=false; s.movementType=ScrollRect.MovementType.Clamped; s.scrollSensitivity=30;
            var rail=Box(r,"Scrollbar",w-13,0,9,h,new Color(1,1,1,.12f)); rail.GetComponent<Image>().raycastTarget=true;
            var handle=Box(rail,"Handle",0,0,9,0,Gold); handle.anchorMin=Vector2.zero; handle.anchorMax=Vector2.one; handle.sizeDelta=Vector2.zero; handle.anchoredPosition=Vector2.zero; handle.GetComponent<Image>().raycastTarget=true;
            var bar=rail.gameObject.AddComponent<Scrollbar>(); bar.direction=Scrollbar.Direction.BottomToTop; bar.handleRect=handle; bar.targetGraphic=handle.GetComponent<Image>();
            s.verticalScrollbar=bar; s.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide; Bind(v,id,s); return s;
        }
        static void Header(CastleUiView v,string title,bool workbench=false)
        {
            Pic(v,"Background","ControlRoom"); Box(v.transform,"Tint",0,0,1600,900,new Color(.12f,.08f,.04f,.88f));
            Btn(v,"BackButton","‹ 中控室",58,35,150,56); Btn(v,"JournalButton","调查册 J",1170,35,175,56); Btn(v,"SettingsButton","设置",1370,35,150,56);
            if(workbench) { Box(v.transform,"Workbench board",130,135,1340,695,Board); Border(v.transform,162,230,1276,515); Box(v.transform,"List divider",340,230,2,515,Light); }
            else Text(v.transform,"Title",title,32,104,1480,60,36,Gold);
            float y=workbench?164:170;
            Btn(v,"BrowseButton","录音",172,y,132,workbench?56:32,workbench?26:20); Btn(v,"AlignButton","锚点",320,y,132,workbench?56:32,workbench?26:20); Btn(v,"SoundButton","房间",468,y,132,workbench?56:32,workbench?26:20);
        }
        static void Border(Transform p,float x,float y,float w,float h)
        { Box(p,"TopRule",x,y,w,2,Light);Box(p,"BottomRule",x,y+h,w,2,Light);Box(p,"LeftRule",x,y,2,h,Light);Box(p,"RightRule",x+w,y,2,h,Light); }
        static void Exploration()
        {
            var v=V(ScreenId.Title); Pic(v,"Background","Title"); Panel(v,642,302,316,420);
            string[] ids={"StartButton","ContinueButton","SettingsButton","CreditsButton","ExitButton"}, labels={"开始游戏","继续游戏","设置","制作名单","退出游戏"};
            for(int i=0;i<5;i++)Btn(v,ids[i],labels[i],661,317+i*75,278,61);
            Text(v.transform,"Caption","古堡 · 声音与时间的谜题",32,851,900,32,18,Gold);
            v=V(ScreenId.Outside); Pic(v,"Background","Held");Btn(v,"OpenInvitationButton","展开手中的邀请函",560,744,480,68);Btn(v,"EnterButton","进入古堡",661,720,278,65);L(v,"HintText","邀请函已收纳 · 可从调查册再次查看",40,824,800,45,22);
            v=V(ScreenId.Invitation);Pic(v,"Background","Invitation");Btn(v,"CollectButton","收起并保存邀请函",1130,800,420,60);
            v=V(ScreenId.Collected);Pic(v,"Background","Collected");Btn(v,"ConfirmButton","确认收纳",943,623,310,78);
            v=V(ScreenId.Lounge);Pic(v,"Background","Lounge");var p=Panel(v,962,133,595,708);
            Text(p,"Heading","会客厅 · 女爵",28,25,535,42,28,Gold);
            for(int i=0;i<3;i++)L(v,"Line"+i,db.greeting[i],28,100+i*132,535,122,24,p);
            L(v,"ProgressText","空格或点击按钮继续阅读",28,534,535,57,20,p);Btn(v,"ContinueButton","继续 →",270,615,295,58,24,p);Btn(v,"MapButton","查看地图",28,615,216,58,24,p);
            v=V(ScreenId.Room);Pic(v,"Background","ControlRoom");var c=Group(v,"ControlRoot"); Btn(v,"WorkbenchButton","查看录音工作台",565,477,400,78,24,c);
            Box(c,"Caption",34,708,760,130,Ink);Text(c,"Hint","这里保存着古堡各处的录音记录。\n从片段、时间与空间中，还原前一晚的经过。",58,736,710,86,25);
            var other=Group(v,"OtherRoomRoot");p=Box(other,"Panel",966,263,570,460,Ink);
            L(v,"RoomNameText","古堡",32,30,506,55,36,p); L(v,"DescriptionText","房间记录",32,112,506,227,24,p);Btn(v,"MapButton","打开地图",32,366,506,57,24,p);
        }
        static void Hud()
        {
            var v=ui.SceneHud;Panel(v,28,26,320,94);L(v,"LocationText","古堡外",48,38,290,40,25,color:Gold);L(v,"ClockText","当晚 18:50",48,81,290,32,20);
            Btn(v,"MapButton","地图 M",1110,28,145,55);Btn(v,"JournalButton","调查册 J",1272,28,165,55);Btn(v,"SettingsButton","菜单",1454,28,118,55);
        }
        static void Workbench()
        {
            var v=V(ScreenId.Browse);Header(v,"录音",true);
            Btn(v,"Rec01Button","REC-01\n餐厅",178,285,140,80,20);Btn(v,"Rec02Button","REC-02\n书房",178,384,140,80,20);Btn(v,"Rec03Button","REC-03",178,483,140,70,20);Btn(v,"PersonalButton","随身记录",178,582,140,70,20);
            Btn(v,"IdentityButton","身份关联 →",178,678,140,44,17);L(v,"FileInfoText","REC-01 · 餐厅",365,247,1045,48,24);
            var transcript=Group(v,"TranscriptRoot");var scroll=Scroll(v,"TranscriptScroll",365,310,1054,422,628,transcript);
            for(int i=0;i<4;i++)
            {
                var b=Btn(v,"Row"+i,"录音片段 "+(i+1),0,i*154,825,148,24,scroll.content);
                var t=b.GetComponentInChildren<Text>();t.alignment=TextAnchor.UpperLeft;t.rectTransform.anchoredPosition=new Vector2(16,-8);t.rectTransform.sizeDelta=new Vector2(790,132);
                if(i!=1)Btn(v,"Excerpt"+i,"摘录",845,i*154+45,170,52,20,scroll.content);
                else Btn(v,"AnchorButton","◇ 标记锚点",845,i*154+29,170,84,20,scroll.content);
            }
            var conversation=Group(v,"ConversationRoot");scroll=Scroll(v,"ConversationScroll",365,310,1054,422,900,conversation);L(v,"ConversationText","会客厅的完整对话",8,0,995,900,24,scroll.content);
            var playback=Group(v,"PlaybackRoot");Btn(v,"PlayButton","播放文字",365,756,140,52,21,playback);Slider(v,"ProgressSlider",535,757,585,playback);L(v,"PlaybackText","04:51 / 08:00 · 文字回放",535,795,585,25,18,playback);Btn(v,"MarkCompleteButton","标记完成 →",1220,766,220,48,23,playback);
            v=V(ScreenId.Align);Header(v,"锚点",true);L(v,"CandidateTitle","候选锚点",178,246,146,35,21);
            Btn(v,"AnchorAButton","锚点 1\nREC-01\n加入对照",178,310,140,104,19);Btn(v,"AnchorBButton","锚点 2\nREC-02\n加入对照",178,435,140,104,19);Btn(v,"BackToBrowseButton","返回录音标记",178,668,140,52,18);
            scroll=Scroll(v,"AlignmentScroll",365,248,1054,482,510);var content=scroll.content;
            Track(v,content,"Reference",8,"锚点 1 · REC-01 / 餐厅 · 21:30:00 · 参考轨锁定",false);
            Track(v,content,"Target",150,"锚点 2 · REC-02 / 书房 · 21:27:00 · 拖动轨道对齐",true);
            Slider(v,"OffsetSlider",25,289,976,content);Text(content,"TextCompareTitle","文本对照",25,347,350,32,23);Border(content,25,389,976,103);
            Text(content,"TextCompare","REC-01：三声清晰金属碰撞，紧接着玻璃碎裂。\nREC-02：隔门三声闷响，随后轻微碎裂。\n基准：REC-01 校时记录；时间重合仍需空间证据。",43,399,940,87,20);
            L(v,"OffsetText","+00:00",365,766,235,45,28,color:Gold);L(v,"StatusText","REC-02 校正量 · ← → 微调 1 秒",610,772,390,36,19);
            Btn(v,"CheckButton","检查对齐",1025,766,190,48);Btn(v,"NextButton","房间 →",1230,766,210,48);
            v=V(ScreenId.Sound);Header(v,"房间",true);L(v,"ListTitle","空间标记",178,246,146,35,21);
            string[] markers={"声源 S","REC-01","REC-02"};for(int i=0;i<3;i++)Btn(v,"Marker"+i,markers[i],178,310+i*92,140,70,21);
            Text(v.transform,"MarkerHelp","选择声源 S 后\n点击地图放置。",178,595,142,65,19);Btn(v,"UndoButton","撤销一步",178,668,140,52,19);
            Map(v,365,304,720,406,false);Box(v.transform,"InspectorDivider",1108,230,2,515,Light);L(v,"InspectorTitle","信息预览",1130,252,282,42,25);
            L(v,"SourceInfoText","声源：尚未放置",1130,309,280,84,22);Text(v.transform,"DoorLabel","书房门 · 历史状态",1130,411,280,35,21);
            string[] doors={"未知","开启","关闭"};for(int i=0;i<3;i++)Btn(v,"Door"+i,doors[i],1130+i*96,463,86,46,19);
            Btn(v,"EvidenceButton","关联门状态证据\nREC-02 / 04:51\n“门是关着的……”",1130,540,280,108,20);Text(v.transform,"DoorHint","核对录音发生时的门，\n不改变当前房间状态。",1130,677,280,50,18);
            L(v,"DraftText","先选择声源、门状态，并关联证据。",365,775,620,34,20);Btn(v,"SaveDraftButton","保存传播草稿",1000,766,216,48,22);Btn(v,"VerifyButton","联合验证",1230,766,210,48);
        }
        static void Track(CastleUiView v,Transform parent,string id,float y,string title,bool drag)
        {
            Text(parent,id+"Title",title,25,y,976,32,21);var p=Box(parent,id+"Track",25,y+42,976,70,new Color(1,1,1,.035f));Border(p,0,0,976,70);
            var rail=Box(p,id+"Rail",20,34,936,3,Light);Bind(v,id+"Rail",rail);
            var marker=Text(p,id+"Marker","◆",350,12,36,44,32,Gold,TextAnchor.MiddleCenter);marker.rectTransform.pivot=new Vector2(.5f,.5f);Bind(v,id+"Marker",marker.rectTransform);
            if(drag){p.GetComponent<Image>().raycastTarget=true;Bind(v,"TrackDrag",p.gameObject.AddComponent<CastleTimelineDrag>());}
        }
        static Vector2[] Corridor(RoomDefinition r)
        {
            if(r.title=="书房")return new[]{new Vector2(554,281),new Vector2(554,532),new Vector2(836,532)};
            if(r.title=="餐厅")return new[]{new Vector2(833,501),new Vector2(836,532)};
            if(r.title=="休息室")return new[]{new Vector2(1137,575),new Vector2(1110,532),new Vector2(836,532)};
            return new[]{new Vector2(r.x,532),new Vector2(836,532)};
        }
        static void Map(CastleUiView v,float x,float y,float w,float h,bool navigation)
        {
            Btn(v,"Floor1Button","1F",x+w-160,y-51,70,38,21);Btn(v,"Floor2Button","2F",x+w-80,y-51,70,38,21);
            var area=R(v.transform,"Map",x,y,w,h);var map=area.gameObject.AddComponent<CastleMapView>();Bind(v,"Map",map);map.MapArea=area;
            var back=R(area,"FloorImage",0,0,w,h).gameObject.AddComponent<RawImage>();back.texture=Resources.Load<Texture2D>("Castle/Floor1");back.raycastTarget=false;map.Background=back;map.Floor1=(Texture2D)back.texture;map.Floor2=Resources.Load<Texture2D>("Castle/Floor2");
            var roomBindings=new List<CastleRoomButton>();float sx=w/1672f,sy=h/941f;
            foreach(var r in db.rooms)
            {
                if(!navigation && (r.id.StartsWith("stairs") || r.id=="east"))continue;
                var box=Box(area,"Room_"+r.id+"_"+r.title,(r.x-r.width/2)*sx,(r.y-r.height/2)*sy,r.width*sx,r.height*sy,new Color(.05f,.1f,.07f,.66f));box.GetComponent<Image>().raycastTarget=true;
                var button=box.gameObject.AddComponent<Button>();button.targetGraphic=box.GetComponent<Image>();
                var label=Text(box,"RoomName",r.title,1,1,r.width*sx-2,r.height*sy-2,navigation?22:15,Light,TextAnchor.MiddleCenter);
                var anchor=R(box,"MarkerAnchor",r.width*sx/2,r.height*sy/2+18,0,0);
                var waypoints=new List<RectTransform>();foreach(var point in Corridor(r))waypoints.Add(R(area,"Corridor_"+r.id+"_"+waypoints.Count,point.x*sx,point.y*sy,0,0));
                roomBindings.Add(new CastleRoomButton{RoomId=r.id,Button=button,Label=label,MarkerAnchor=anchor,Corridor=waypoints.ToArray()});box.gameObject.SetActive(r.floor==1);
            }
            map.Rooms=roomBindings.ToArray();map.Lines=new RectTransform[128];
            for(int i=0;i<map.Lines.Length;i++){var line=Box(area,"Connection_"+i,0,0,0,4,Gold);line.pivot=new Vector2(0,.5f);line.gameObject.SetActive(false);map.Lines[i]=line;}
            map.Markers=new Text[3];for(int i=0;i<3;i++){var marker=Text(area,"Marker_"+i,(i+1).ToString(),0,0,34,34,28,v.Id=="Sound"?Ink:Gold,TextAnchor.MiddleCenter);marker.rectTransform.pivot=new Vector2(.5f,.5f);marker.gameObject.SetActive(false);map.Markers[i]=marker;}
        }
        static void Resolution()
        {
            var v=V(ScreenId.Event);Header(v,"联合验证 · 餐厅的碰撞与玻璃碎裂");var p=Panel(v,32,207,1058,580);Text(p,"Heading","声音事件 / 前一晚 21:30",35,32,980,61,33,Gold);L(v,"RecordText","已确认记录",35,126,984,398,25,p);
            p=Panel(v,1114,207,452,580);Text(p,"Heading","已确认关系",30,32,392,50,29,Gold);Text(p,"Explanation","时间与传播条件互相支持。\n\n已确认记录保留原始来源；继续修改推理草稿，不会改变此记录。\n\n下一步：确认匿名说话者 A 的身份。",30,118,392,287,24);Btn(v,"IdentityButton","确认录音身份 →",30,474,392,65,24,p);Btn(v,"RecordingButton","回看原始录音",1320,829,246,53);
            v=V(ScreenId.Identity);Header(v,"人物调查 · 说话者 A 是谁？");p=Panel(v,32,205,427,581);Text(p,"Heading","原始声音片段",28,25,371,48,27,Gold);Text(p,"Quote","“西翼那株白花，是我亲手栽下的。”\n\nREC-02 · 文件02\n05:12 · 说话者 A\n\n名字被提到，或录音出现在某个房间，都不能单独证明身份。",28,122,371,375,25);
            p=Panel(v,481,205,569,581);Text(p,"Heading","选择候选人物",28,25,513,50,27,Gold);Btn(v,"LinButton","林女士\n西翼花园的照料者",28,127,513,144,27,p);Btn(v,"ChenButton","陈先生\n古堡的宾客",28,312,513,144,27,p);
            p=Panel(v,1072,205,496,581);Text(p,"Heading","支持证据",28,25,440,50,27,Gold);Btn(v,"EvidenceButton","关联休息室交谈摘录",28,126,440,233,24,p);L(v,"StatusText","选择人物与独立证词，再提交身份判断。",28,414,440,108,23,p);Btn(v,"ConfirmButton","确认身份",1270,829,296,53);
            v=V(ScreenId.Route);Header(v,"空间推理 · 林女士 / 前一晚 21:20—21:40");p=Panel(v,30,205,292,581);Text(p,"Heading","位置节点",20,24,252,45,26,Gold);for(int i=0;i<3;i++)Btn(v,"Node"+i,"节点 "+(i+1),20,100+i*123,252,105,20,p);Btn(v,"UndoButton","撤销一步",20,503,252,54,20,p);
            Panel(v,344,205,824,581);Map(v,344,289,824,464,false);p=Panel(v,1190,205,380,581);L(v,"NodeText","节点 1 / 关联位置证据",24,24,332,71,25,p);Btn(v,"EvidenceButton","关联位置证据",24,127,332,229,21,p);L(v,"StatusText","路线沿通道连接。",24,399,332,110,22,p);Btn(v,"VerifyButton","核对人物路线",1050,829,249,53);Btn(v,"NextButton","事件还原 →",1320,829,246,53);
            v=V(ScreenId.Final);Header(v,"案件还原 · 接通最后一条证据关系");p=Panel(v,32,205,1058,581);Text(p,"Heading","前一晚 · 校正后事件时间轴",28,28,998,45,26,Gold);
            string[] events={"21:20\n进入书房","21:30 之前\n柜门被打开","21:30\n碰撞与玻璃碎裂"};for(int i=0;i<3;i++){var e=Box(p,"TimelineEvent"+i,28+i*339,131,320,160,Board);Text(e,"Caption",events[i],23,25,274,110,27,Light,TextAnchor.MiddleCenter);}
            L(v,"RelationsText","证据关系",30,342,998,218,25,p);p=Panel(v,1114,205,452,581);Text(p,"Heading","最后的关系",28,28,396,43,27,Gold);Text(p,"Explanation","开柜动作与器物坠落有关\n\n仅凭时间先后不足以证明因果，需要独立物证。",28,113,396,169,24);Btn(v,"EvidenceButton","选择支持证据",28,318,396,196,24,p);Btn(v,"SubmitButton","提交事件还原",1270,829,296,53);
            v=V(ScreenId.Truth);Header(v,"事件重建 · 记录终于彼此吻合");p=Panel(v,32,205,964,581);ui.WaveBars=new RectTransform[78];for(int i=0;i<78;i++){float h=20+Mathf.Abs(Mathf.Sin(i*.47f)*Mathf.Cos(i*.13f))*270;var bar=Box(p,"Wave"+i,35+i*11.5f,273,4,h,Gold);bar.pivot=new Vector2(.5f,.5f);ui.WaveBars[i]=bar;}
            Text(p,"Caption","REC-01 + REC-02 / 共同事件时间轴\n传播条件 · 人物路线 · 现场物证",40,462,884,82,23,Light,TextAnchor.MiddleCenter);p=Panel(v,1020,205,546,581);L(v,"IndexText","拉帕莉亚的重建叙述 1 / 3",30,35,486,59,25,p,color:Gold);L(v,"StoryText",db.truth[0],30,160,486,290,30,p);Btn(v,"NextButton","继续重建",1270,829,296,53);
            v=V(ScreenId.Choice);Pic(v,"Background","ControlRoom");p=Panel(v,125,95,1350,710);Text(p,"SavedHint","真相已查明 · 结局前已保存",50,40,1240,43,22);Text(p,"Heading","如何处理这些证据？",50,114,1240,90,48,Gold);Text(p,"Explanation","事实判断已由调查完成。现在选择的是拉帕莉亚的行动。",50,226,1240,62,27);Btn(v,"PublicButton","公开完整记录\n\n让相关人物面对已核实的事实。",50,338,605,180,28,p);Btn(v,"ConfrontButton","先与当事人对质\n\n携带证据，进行一次当面对质。",695,338,605,180,28,p);Btn(v,"JournalButton","核对调查册",50,598,605,60,24,p);Btn(v,"BackButton","返回事件还原",695,598,605,60,24,p);
            v=V(ScreenId.Ending);Pic(v,"Background","ControlRoom");p=Panel(v,125,95,1350,710);Text(p,"Heading","调查落幕",55,45,1240,45,24);L(v,"EndingText","公开记录",55,138,1240,110,66,p,Gold);L(v,"StoryText","调查已完成",55,303,1240,140,31,p);Text(p,"Hint","声音事件、设备校正、人物位置与现场证据已收入调查册。",55,484,1240,51,23);Btn(v,"JournalButton","查看调查册",55,597,370,62,24,p);Btn(v,"BackButton","返回结局前",490,597,370,62,24,p);Btn(v,"TitleButton","返回标题",925,597,370,62,24,p);
        }
        static void Overlays()
        {
            var v=ui.Overlay("Navigation");Panel(v,55,30,1490,840);L(v,"Heading","古堡地图",90,48,600,60,34,color:Gold);L(v,"LocationText","当前位置：会客厅",90,108,1150,42,23);Map(v,296,206,1008,568,true);Btn(v,"CloseButton","关闭地图",1290,48,220,55);L(v,"SelectionText","点击房间查看开放条件",95,790,910,50,22);Btn(v,"WaitButton","等待至 19:00",1060,790,210,50,22);Btn(v,"EnterButton","进入房间",1290,790,220,50,22);
            v=ui.Overlay("Settings");var p=Panel(v,360,145,880,620);Text(p,"Heading","设置",50,32,780,70,42,Gold);Text(p,"SizeLabel","阅读字号",50,136,250,45,26);L(v,"SizeText","24",680,136,130,45,26,p);Slider(v,"SizeSlider",290,143,360,p);Text(p,"VolumeLabel","界面音量",50,223,250,45,26);Slider(v,"VolumeSlider",290,230,480,p);Btn(v,"MotionButton","动态效果：开启",50,330,780,70,26,p);Btn(v,"TitleButton","返回标题",50,500,370,65,26,p);Btn(v,"ContinueButton","继续游戏",460,500,370,65,26,p);
            v=ui.Overlay("Message");p=Panel(v,360,190,880,520);L(v,"TitleText","提示",50,35,780,65,34,p,Gold);L(v,"BodyText","消息内容",50,128,780,232,26,p);Btn(v,"CancelButton","取消",50,413,350,65,24,p);Btn(v,"ConfirmButton","确认",430,413,400,65,24,p);
            v=ui.Overlay("Journal");Panel(v,60,35,1480,815);Text(v.transform,"Heading","调查册",115,74,950,66,42,Gold);Btn(v,"CloseButton","关闭调查册",1240,77,215,55);Btn(v,"PeopleButton","人物",115,164,180,54);Btn(v,"ItemsButton","道具",315,164,180,54);Btn(v,"EventsButton","事件",515,164,180,54);L(v,"CountText","人物 · 0 条记录",940,171,480,42,23);
            var scroll=Scroll(v,"GridScroll",115,250,1340,515,515);scroll.name="Journal grid";var grid=scroll.content.gameObject.AddComponent<GridLayoutGroup>();grid.cellSize=new Vector2(410,185);grid.spacing=new Vector2(30,25);grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=3;grid.childAlignment=TextAnchor.UpperLeft;
            var fitter=scroll.content.gameObject.AddComponent<ContentSizeFitter>();fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            L(v,"EmptyText","当前分类暂无记录，继续调查后会自动收录。",150,350,1200,70,27);Text(v.transform,"Hint","点击卡片查看详情与原始记录。",115,797,1250,30,21);
            CreateJournalPrefab();
            v=ui.Overlay("JournalDetail");Panel(v,110,55,1380,790);Btn(v,"BackButton","‹ 返回人物",160,95,230,55);Btn(v,"CloseButton","关闭调查册",1220,95,215,55);L(v,"CategoryText","人物",190,194,1150,40,23,color:Gold);L(v,"TitleText","详情标题",190,250,1200,75,42,color:Gold);scroll=Scroll(v,"DetailScroll",190,340,1230,355,355);L(v,"BodyText","详情内容",0,0,1178,355,24,scroll.content);Btn(v,"ActionButton","查看记录",1020,751,400,58);Text(v.transform,"DetailHint","原始材料与确认结论分别保存。",190,762,770,40,21);
            v=ui.Overlay("InvitationArchive");Pic(v,"Background","Invitation");Btn(v,"BackButton","返回道具详情",1130,800,420,60);
        }
        static void CreateJournalPrefab()
        {
            if(File.Exists(PrefabPath)) { ui.JournalCardPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).GetComponent<CastleJournalCardView>(); return; }
            Directory.CreateDirectory("Assets/Castle/Prefabs");AssetDatabase.Refresh();
            var r=Box(null,"JournalCard",0,0,410,185,Board);r.GetComponent<Image>().raycastTarget=true;
            var card=r.gameObject.AddComponent<CastleJournalCardView>();card.OpenButton=r.gameObject.AddComponent<Button>();card.OpenButton.targetGraphic=r.GetComponent<Image>();
            card.CategoryText=Text(r,"CategoryText","人物",22,18,366,28,18,Gold);card.TitleText=Text(r,"TitleText","条目名称",22,55,366,55,28,Light);card.SummaryText=Text(r,"SummaryText","条目摘要 · 点击查看详情",22,119,366,48,20,Light);
            PrefabUtility.SaveAsPrefabAsset(r.gameObject,PrefabPath);UnityEngine.Object.DestroyImmediate(r.gameObject);ui.JournalCardPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).GetComponent<CastleJournalCardView>();
        }
    }
}

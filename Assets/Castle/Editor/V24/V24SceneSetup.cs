using System;
using System.IO;
using System.Linq;
using Castle.Flow;
using Castle.V24;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace Castle.Editor
{
    /// <summary>仅编辑期迁移已有场景；运行时不创建页面或组件。重复调用保留设计师修改。</summary>
    public static class V24SceneSetup
    {
        private const string Art="Assets/Castle/Art/V24/";
        // 仅定向更新本轮确认的页面与模板，不执行旧版全场景布局工具。
        public static void ApplyInteractionReviewAndBuild()
        {
            var scene=EditorSceneManager.OpenScene(CastleFlowSceneBuilder.ScenePath);
            var game=UnityEngine.Object.FindObjectOfType<CastleV24Controller>(true);
            _ui=game.Ui;_font=Resources.Load<Font>("Castle/Chinese");
            // 素材名描述的是当前播放状态：暂停态素材为三角形，播放态素材为双竖线。
            game.PlayIcon=Sprite("2中控-录音文件/拆分/录音文件暂停.png");
            game.PauseIcon=Sprite("2中控-录音文件/拆分/录音文件播放.png");
            var settings=_ui.View("Settings");
            if(!settings.Controls.Any(x=>x.Id=="SaveGame"))Button(settings,"SaveGame","保存进度",430,610,280,48);
            if(!settings.Controls.Any(x=>x.Id=="LoadGame"))Button(settings,"LoadGame","读取进度",750,610,280,48);
            var map=_ui.View("Map");
            var panel=map.transform.Find("Panel");if(panel)panel.gameObject.SetActive(false);
            // 普通地图去除边框与覆盖地图原字的色块，保留透明点击区及导航控件。
            var v=_ui.View("Recordings");
            Disable(v,"Divider","SideDivider","Extract","PuzzleName");
            Place(v.Get<Button>("Recordings").transform,188,193,125,47);
            Place(v.Get<Button>("Anchors").transform,354,193,130,47);
            Place(v.Get<Button>("Space").transform,523,193,130,47);
            foreach(var id in new[]{"Recordings","Anchors","Space"})
            {
                var button=v.Get<Button>(id);button.GetComponent<Image>().color=Color.clear;
                var label=button.GetComponentInChildren<Text>(true);label.fontSize=28;label.color=id=="Recordings"?Color.white:new Color(.62f,.67f,.59f);
                label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;label.rectTransform.offsetMin=label.rectTransform.offsetMax=Vector2.zero;
            }
            if(!v.transform.Find("ActiveTabLine"))
            {var line=Rect(v.transform,"ActiveTabLine",188,232,125,7).gameObject.AddComponent<Image>();line.color=Color.white;line.raycastTarget=false;}
            Place(v.Get<Text>("FileInfo").transform,166,280,1290,35);v.Get<Text>("FileInfo").fontSize=24;v.Get<Text>("FileInfo").color=Color.white;
            if(!v.transform.Find("FileHeader"))
            {var bg=Rect(v.transform,"FileHeader",155,266,1317,55).gameObject.AddComponent<Image>();bg.sprite=Sprite("2中控-录音文件/拆分/文件描述底框.png");bg.color=Color.white;bg.raycastTarget=false;bg.transform.SetSiblingIndex(1);}
            var header=v.transform.Find("FileHeader").GetComponent<Image>();header.sprite=null;header.color=new Color(1,1,1,.025f);
            ResizeList(v,"List",155,340,195,385,false);
            ResizeList(v,"Lines",355,340,1117,383,true);
            Place(v.Get<Button>("Play").transform,281,752,28,28);
            Place(v.Get<Slider>("Progress").transform,324,749,980,34);
            var slider=v.Get<Slider>("Progress");
            Place(slider.transform.Find("Rail"),0,16,980,2);Place(slider.transform.Find("HandleArea"),12,0,956,34);
            slider.handleRect.GetComponent<Image>().sprite=Sprite("2中控-录音文件/拆分/标记点.png");slider.handleRect.GetComponent<Image>().preserveAspect=true;slider.handleRect.sizeDelta=new Vector2(25,0);
            foreach(var image in slider.GetComponentsInChildren<Image>(true))image.color=Color.white;
            Place(v.Get<Text>("Playback").transform,324,775,500,30);v.Get<Text>("Playback").fontSize=20;v.Get<Text>("Playback").color=Color.white;
            if(!v.Controls.Any(x=>x.Id=="Duration"))
            {var text=Rect(v.transform,"Duration",1190,775,114,30).gameObject.AddComponent<Text>();text.font=_font;text.fontSize=20;text.color=Color.white;text.alignment=TextAnchor.UpperRight;Bind(v,"Duration",text);}
            Place(v.Get<Button>("Done").transform,1352,746,120,34);var done=v.Get<Button>("Done");done.GetComponent<Image>().color=Color.white;
            var doneText=done.GetComponentInChildren<Text>(true);doneText.color=new Color(.07f,.14f,.07f);doneText.fontSize=24;doneText.rectTransform.anchorMin=Vector2.zero;doneText.rectTransform.anchorMax=Vector2.one;doneText.rectTransform.offsetMin=doneText.rectTransform.offsetMax=Vector2.zero;
            doneText.resizeTextForBestFit=true;doneText.resizeTextMinSize=14;doneText.resizeTextMaxSize=24;doneText.verticalOverflow=VerticalWrapMode.Overflow;
            game.RecordingFolderPrefab=RecordingCard(true);game.RecordingFilePrefab=RecordingCard(false);game.RecordingLinePrefab=RecordingTextRow();
            // 顶部图标按参考稿保持原比例，文字说明留在普通探索页。
            Place(_ui.Hud.Get<Button>("Back").transform,26,23,50,38);
            Place(_ui.Hud.Get<Button>("Map").transform,1332,7,64,64);
            Place(_ui.Hud.Get<Button>("Journal").transform,1408,7,64,64);
            Place(_ui.Hud.Get<Button>("Settings").transform,1486,7,64,64);
            foreach(var id in new[]{"Back","Map","Journal","Settings"})_ui.Hud.Get<Button>(id).GetComponent<Image>().preserveAspect=true;
            EditorUtility.SetDirty(game);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();ExportBindings();
            InstallValidateBuild();
        }
        public static void ApplyVisualReviewAndBuild()
        {
            var scene = EditorSceneManager.OpenScene(CastleFlowSceneBuilder.ScenePath);
            var game = UnityEngine.Object.FindObjectOfType<CastleV24Controller>(true);
            _ui = game.Ui;
            Place(_ui.Hud.Get<Text>("Location").transform, 100, 26, 1000, 42);
            var explore = _ui.View("Explore");
            Disable(explore, "RoomName", "Portrait", "Protagonist");
            // 以1920×1080参考稿按5/6映射到1600×900，边框保持653:991原始比例。
            Place(explore.transform.Find("DialoguePanel"), 1006, 61, 544, 826);
            var frame = explore.transform.Find("DialoguePanel/Back").GetComponent<Image>();
            Place(frame.transform, 0, 0, 544, 826);
            frame.sprite = Sprite("5交谈界面/拆分/对话框边框.png");
            frame.type = Image.Type.Simple; frame.preserveAspect = true; frame.color = Color.white; frame.raycastTarget = false;
            var backdrop = explore.transform.Find("DialoguePanel/TextBackdrop");
            if (backdrop) backdrop.GetComponent<Image>().color = Color.clear;
            var scroll = explore.Get<ScrollRect>("DialogueScroll");
            Place(scroll.transform, 38, 90, 475, 570);
            Place(scroll.viewport, 0, 0, 420, 570);
            scroll.viewport.GetComponent<Image>().color = Color.clear;
            scroll.content.sizeDelta = new Vector2(420, 570);
            var layout = scroll.content.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 10, 0, 0);
            var bar = scroll.verticalScrollbar;
            Place(bar.transform, 445, 80, 41, 462);
            bar.GetComponent<Image>().sprite = Sprite("5交谈界面/拆分/滑动条底框.png");
            bar.GetComponent<Image>().color = Color.white;
            bar.GetComponent<Image>().preserveAspect = true;
            var handle = bar.handleRect.GetComponent<Image>();
            handle.sprite = null;handle.color = Color.clear;
            var thumbTransform = bar.handleRect.Find("Art");
            if(!thumbTransform)thumbTransform=new GameObject("Art",typeof(RectTransform),typeof(Image)).transform;
            thumbTransform.SetParent(bar.handleRect,false);
            var thumb=(RectTransform)thumbTransform;thumb.anchorMin=thumb.anchorMax=thumb.pivot=new Vector2(.5f,.5f);thumb.anchoredPosition=Vector2.zero;thumb.sizeDelta=new Vector2(13.3f,49.2f);
            var thumbImage=thumb.GetComponent<Image>();thumbImage.sprite=Sprite("5交谈界面/拆分/滑动块.png");thumbImage.color=Color.white;thumbImage.preserveAspect=true;thumbImage.raycastTarget=false;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            Place(explore.Get<RectTransform>("Choices"), 34, 670, 475, 82);
            var choices = explore.Get<RectTransform>("Choices").GetComponent<HorizontalLayoutGroup>();
            choices.spacing = 5;
            Place(explore.Get<Button>("Auto").transform, 34, 760, 190, 40);
            Place(explore.Get<Button>("Continue").transform, 300, 760, 195, 40);
            Place(explore.Get<Image>("CharacterImage").transform, 35, 115, 610, 760);
            var dialogueLine = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(_ui.LinePrefab));
            var lineView = dialogueLine.GetComponent<CastleUiView>();
            foreach(var id in new[]{"Speaker","Body"})
            {
                var rect=(RectTransform)lineView.Controls.First(x=>x.Id==id).Target.transform;
                rect.anchorMin=new Vector2(0,1);rect.anchorMax=new Vector2(1,1);rect.pivot=new Vector2(0,1);
                rect.anchoredPosition=new Vector2(4,id=="Body"?-42:-4);rect.sizeDelta=new Vector2(-8,id=="Body"?94:30);
            }
            PrefabUtility.SaveAsPrefabAsset(dialogueLine,AssetDatabase.GetAssetPath(_ui.LinePrefab));
            PrefabUtility.UnloadPrefabContents(dialogueLine);

            // 头像模板在编辑期保存；运行时仅复制这个已绑定的模板。
            var card = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(game.PersonCardPrefab));
            var view = card.GetComponent<CastleUiView>();
            view.Get<Image>("Background").color = Color.clear;
            var avatarFrame = card.transform.Find("Frame").GetComponent<Image>();
            avatarFrame.sprite = Sprite("5交谈界面/拆分/选择角色.png");avatarFrame.preserveAspect = true;
            view.Get<Text>("Body").gameObject.SetActive(false);
            var title = view.Get<Text>("Title");Place(title.transform, 8, 114, 96, 25);title.fontSize=15;title.color=Color.white;
            view.Get<Image>("Portrait").preserveAspect = true;
            game.ConversationPortraitPrefab = PrefabUtility.SaveAsPrefabAsset(card,"Assets/Castle/Prefabs/V24/ConversationPortrait.prefab").GetComponent<CastleUiView>();
            PrefabUtility.UnloadPrefabContents(card);
            var people = explore.Get<RectTransform>("People");
            var peopleScroll = people.GetComponentInParent<ScrollRect>(true);
            Place(peopleScroll.transform, 36, 744, 640, 145);Place(peopleScroll.viewport, 0, 0, 640, 145);
            peopleScroll.viewport.GetComponent<Image>().color = Color.clear;
            peopleScroll.horizontal = true;peopleScroll.vertical = false;
            if(peopleScroll.verticalScrollbar){peopleScroll.verticalScrollbar.gameObject.SetActive(false);peopleScroll.verticalScrollbar=null;}
            UnityEngine.Object.DestroyImmediate(people.GetComponent<VerticalLayoutGroup>());
            var grid = people.GetComponent<GridLayoutGroup>();if(!grid)grid=people.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize=new Vector2(112,143);grid.spacing=new Vector2(15,0);grid.constraint=GridLayoutGroup.Constraint.FixedRowCount;grid.constraintCount=1;
            people.sizeDelta=new Vector2(640,145);
            var fitter=people.GetComponent<ContentSizeFitter>();fitter.verticalFit=ContentSizeFitter.FitMode.Unconstrained;fitter.horizontalFit=ContentSizeFitter.FitMode.PreferredSize;

            var map = _ui.View("Map");Disable(map,"Panel","Heading","Details","Hint","Enter");
            var mapBackground=map.transform.Find("Panel");mapBackground.gameObject.SetActive(true);
            Place(mapBackground,0,0,1600,900);mapBackground.SetAsFirstSibling();
            var mapBackgroundImage=mapBackground.GetComponent<Image>();mapBackgroundImage.sprite=null;mapBackgroundImage.color=new Color(175f/255,165f/255,148f/255);mapBackgroundImage.raycastTarget=false;
            foreach(var graphic in map.Get<RectTransform>("TooltipPanel").GetComponentsInChildren<Graphic>(true))graphic.raycastTarget=false;
            // 按现行裁切宽高比显示地图；透明热点仍共用同一坐标根。
            Place(map.Get<RectTransform>("Rooms"), 160, 45, 1280, 830);
            Place(map.Get<Button>("Floor1").transform, 1450, 135, 100, 48);
            Place(map.Get<Button>("Floor2").transform, 1450, 190, 100, 48);
            Place(map.Get<Button>("Close").transform, 1450, 60, 100, 48);
            foreach(var id in new[]{"Floor1","Floor2","Close"})
            {
                var button=map.Get<Button>(id);button.GetComponent<Image>().color=Color.clear;
                button.GetComponentInChildren<Text>(true).color=new Color(.15f,.12f,.08f);
            }
            foreach(var image in map.GetComponentsInChildren<Image>(true))
                if(image.name=="MapBack" || image.name=="MapFrame")image.color=Color.clear;
            EditorUtility.SetDirty(game);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();ExportBindings();
            InstallValidateBuild();
        }
        public static void ApplyWorkbenchReferenceAndBuild()
        {
            var scene=EditorSceneManager.OpenScene(CastleFlowSceneBuilder.ScenePath);
            var game=UnityEngine.Object.FindObjectOfType<CastleV24Controller>(true);_ui=game.Ui;_font=Resources.Load<Font>("Castle/Chinese");
            var dialogue=_ui.View("Explore").transform.Find("DialoguePanel/TextBackdrop");
            Place(dialogue,16,70,512,739);dialogue.GetComponent<Image>().color=new Color(43f/255,37f/255,32f/255,.4f);
            foreach(var id in new[]{"Anchors","Space"})
            {
                var v=_ui.View(id);Backdrop(v,id=="Anchors"?"3中控-锚点对齐/拆分/背景.png":"4中控-空间推演/拆分/图层 1.png");
                Disable(v,"Board","Divider","SideDivider");
                int i=0;
                foreach(var tab in new[]{"Recordings","Anchors","Space"})
                {
                    var button=v.Get<Button>(tab);Place(button.transform,188+i++*166,193,125,47);button.GetComponent<Image>().color=Color.clear;
                    var label=button.GetComponentInChildren<Text>(true);label.fontSize=28;label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;label.rectTransform.offsetMin=label.rectTransform.offsetMax=Vector2.zero;
                }
                var active=v.transform.Find("ActiveTabLine");if(!active)active=Rect(v.transform,"ActiveTabLine",0,0,1,1).gameObject.AddComponent<Image>().transform;
                Place(active,id=="Anchors"?356:523,233,122,8);active.GetComponent<Image>().color=Color.white;active.GetComponent<Image>().raycastTarget=false;
                ResizeList(v,"List",170,266,150,470,false);
                Place(v.Get<Text>("PuzzleName").transform,790,208,530,32);v.Get<Text>("PuzzleName").fontSize=18;
                Place(v.Get<Button>("PreviousPuzzle").transform,750,203,35,35);Place(v.Get<Button>("NextPuzzle").transform,1330,203,35,35);
            }
            var anchors=_ui.View("Anchors");
            var drop=anchors.Get<FlowDrag>("DropZone");Place(drop.transform,352,266,1084,470);
            var dropImage=drop.GetComponent<Image>();if(dropImage)dropImage.color=Color.clear;
            ResizeList(anchors,"Tracks",0,0,1084,470,true);
            var tracks=anchors.Get<RectTransform>("Tracks");tracks.GetComponent<VerticalLayoutGroup>().spacing=29;
            tracks.GetComponent<VerticalLayoutGroup>().padding=new RectOffset();
            var scroll=tracks.GetComponentInParent<ScrollRect>(true);Place(scroll.viewport,0,0,1031,470);tracks.sizeDelta=new Vector2(1031,470);
            var bar=scroll.verticalScrollbar;Place(bar.transform,1059,0,23,427);bar.GetComponent<Image>().sprite=null;bar.GetComponent<Image>().color=new Color(1,1,1,.2f);bar.handleRect.GetComponent<Image>().sprite=null;bar.handleRect.GetComponent<Image>().color=Color.white;scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.Permanent;
            // Details已移除；片段详情由点击后的提示显示。
            Place(anchors.Get<Text>("Status").transform,352,775,550,28);anchors.Get<Text>("Status").fontSize=18;
            if(anchors.TryGet<Button>("Add",out var add))Place(add.transform,170,748,150,34);
            Place(anchors.Get<Button>("Check").transform,1343,744,119,34);WhiteAction(anchors.Get<Button>("Check"));
            if(anchors.TryGet<Button>("Next",out var next))Place(next.transform,1120,790,230,34);
            var track=PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(_ui.TrackPrefab));var tv=track.GetComponent<CastleUiView>();
            track.GetComponent<LayoutElement>().preferredHeight=139;
            Place(track.transform.Find("Back"),0,0,1031,139);var back=track.transform.Find("Back").GetComponent<Image>();back.sprite=Sprite("3中控-锚点对齐/拆分/轨道槽位背景.png");back.color=new Color(.6f,.8f,.6f,.65f);
            Place(tv.Get<Text>("Name").transform,25,18,480,30);tv.Get<Text>("Name").fontSize=20;
            Place(tv.Get<Text>("Offset").transform,25,55,650,30);tv.Get<Text>("Offset").fontSize=18;
            Place(tv.Get<Button>("Reference").transform,720,14,165,32);Place(tv.Get<Button>("Remove").transform,903,14,105,32);
            foreach(var key in new[]{"Reference","Remove"}){tv.Get<Button>(key).GetComponent<Image>().color=Color.clear;tv.Get<Button>(key).GetComponentInChildren<Text>(true).fontSize=18;}
            var slider=tv.Get<Slider>("OffsetSlider");Place(slider.transform,25,96,980,28);Place(slider.transform.Find("Rail"),0,13,980,2);Place(slider.transform.Find("HandleArea"),12,0,956,28);slider.handleRect.GetComponent<Image>().sprite=Sprite("4中控-空间推演/拆分/地图标记点.png");slider.handleRect.GetComponent<Image>().color=Color.white;slider.handleRect.GetComponent<Image>().preserveAspect=true;
            PrefabUtility.SaveAsPrefabAsset(track,AssetDatabase.GetAssetPath(_ui.TrackPrefab));PrefabUtility.UnloadPrefabContents(track);
            game.AnchorCardPrefab=WorkbenchCard("AnchorCard","3中控-锚点对齐/拆分/锚点图标.png",true);
            game.PathMarkerIcon=Sprite("4中控-空间推演/拆分/地图标记点.png");
            game.SpaceCardPrefab=WorkbenchCard("SpaceCard","4中控-空间推演/拆分/标记图标.png",false);
            var space=_ui.View("Space");Disable(space,"PuzzleName");
            var pan=space.Get<V24MapPan>("MapPan");Place(pan.transform,324,308,888,417);pan.GetComponent<Image>().color=new Color(1,1,1,.07f);
            pan.Content.sizeDelta=new Vector2(644,417);pan.Content.anchoredPosition=Vector2.zero;
            Place(space.Get<Button>("Floor1").transform,1042,267,65,36);Place(space.Get<Button>("Floor2").transform,1130,267,65,36);
            foreach(var id in new[]{"Floor1","Floor2"}){space.Get<Button>(id).GetComponent<Image>().color=Color.clear;var label=space.Get<Button>(id).GetComponentInChildren<Text>(true);label.fontSize=30;label.verticalOverflow=VerticalWrapMode.Overflow;label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;label.rectTransform.offsetMin=label.rectTransform.offsetMax=Vector2.zero;}
            var underline=space.transform.Find("FloorUnderline");if(!underline)underline=Rect(space.transform,"FloorUnderline",1042,300,65,5).gameObject.AddComponent<Image>().transform;underline.GetComponent<Image>().color=Color.white;underline.GetComponent<Image>().raycastTarget=false;
            var panel=space.transform.Find("HypothesisPanel");if(!panel)panel=Rect(space.transform,"HypothesisPanel",1216,308,250,417).gameObject.AddComponent<Image>().transform;
            panel.GetComponent<Image>().sprite=Sprite("4中控-空间推演/拆分/信息假设底框.png");panel.GetComponent<Image>().color=Color.white;panel.GetComponent<Image>().raycastTarget=false;panel.SetSiblingIndex(1);
            if(!space.transform.Find("HypothesisTitle")){var text=Rect(space.transform,"HypothesisTitle",1226,270,240,38).gameObject.AddComponent<Text>();text.font=_font;text.text="信息假设";text.fontSize=27;text.color=Color.white;text.raycastTarget=false;}
            space.transform.Find("HypothesisTitle").GetComponent<Text>().verticalOverflow=VerticalWrapMode.Overflow;
            Place(space.Get<Text>("Details").transform,1235,336,210,140);space.Get<Text>("Details").fontSize=19;
            Place(space.Get<InputField>("Hypothesis").transform,1235,480,210,150);space.Get<InputField>("Hypothesis").GetComponent<Image>().color=new Color(0,0,0,.08f);
            Place(space.Get<Button>("Verify").transform,1343,740,119,34);WhiteAction(space.Get<Button>("Verify"));
            Place(space.Get<Button>("ZoomIn").transform,324,744,36,30);Place(space.Get<Button>("ZoomOut").transform,368,744,36,30);Place(space.Get<Button>("ResetView").transform,418,744,130,30);
            EditorUtility.SetDirty(game);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();ExportBindings();InstallValidateBuild();
        }
        public static void ApplyJournalAndRoomArtAndBuild()
        {
            var scene=EditorSceneManager.OpenScene(CastleFlowSceneBuilder.ScenePath);
            var game=UnityEngine.Object.FindObjectOfType<CastleV24Controller>(true);_ui=game.Ui;_font=Resources.Load<Font>("Castle/Chinese");
            game.InvitationClosed=Sprite("道具/邀请函1.png");game.InvitationOpen=Sprite("道具/邀请函2.png");
            game.CorridorBackground=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"场景/走廊.png");
            game.GuestBackground=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"场景/客房.jpg");
            game.ControlBackground=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"场景/中控室.jpg");
            if(!game.InvitationClosed||!game.InvitationOpen||!game.CorridorBackground||!game.GuestBackground||!game.ControlBackground)throw new InvalidOperationException("邀请函／房间素材未找到");
            var invitation=_ui.View("Invitation");var letter=invitation.transform.Find("Letter").GetComponent<Image>();
            Place(letter.transform,350,-60,900,900);letter.sprite=game.InvitationClosed;letter.preserveAspect=true;letter.color=Color.white;letter.raycastTarget=false;
            if(!invitation.Controls.Any(x=>x.Id=="Artwork"))Bind(invitation,"Artwork",letter);
            Disable(invitation,"Heading","Body");
            var explore=_ui.View("Explore");Place(explore.transform.Find("FreePanel"),45,660,600,75);
            Place(explore.Get<Button>("ExploreRoom").transform,0,0,220,55);Place(explore.Get<Button>("Workbench").transform,0,0,260,55);
            var journal=_ui.View("Journal");
            foreach(var id in new[]{"RecordFilter","Source"})
            {
                if(journal.TryGet<Button>(id,out var button))UnityEngine.Object.DestroyImmediate(button.gameObject);
                journal.Controls.RemoveAll(x=>x.Id==id||x.Id==id+"Label");
            }
            var timeline=_ui.View("Timeline");
            if(!timeline.Controls.Any(x=>x.Id=="Requirements"))
            {
                var text=Rect(timeline.transform,"Requirements",95,180,1380,65).gameObject.AddComponent<Text>();text.font=_font;text.fontSize=18;text.color=Color.white;text.verticalOverflow=VerticalWrapMode.Overflow;text.raycastTarget=false;Bind(timeline,"Requirements",text);
            }
            EditorUtility.SetDirty(game);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();ExportBindings();InstallValidateBuild();
        }
        private static void WhiteAction(Button button)
        {
            button.GetComponent<Image>().sprite=Sprite("3中控-锚点对齐/拆分/完成键位背景.png");button.GetComponent<Image>().color=Color.white;
            var label=button.GetComponentInChildren<Text>(true);label.fontSize=23;label.color=new Color(.08f,.14f,.08f);label.resizeTextForBestFit=true;label.resizeTextMinSize=16;label.resizeTextMaxSize=23;
            label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;label.rectTransform.offsetMin=label.rectTransform.offsetMax=Vector2.zero;
        }
        private static CastleUiView WorkbenchCard(string name,string iconPath,bool anchor)
        {
            var go=PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(_ui.CardPrefab));go.name=name;var v=go.GetComponent<CastleUiView>();
            v.Get<Image>("Background").color=Color.clear;v.Get<Button>("Select").transition=Selectable.Transition.None;go.GetComponent<LayoutElement>().preferredHeight=anchor?94:102;
            var icon=Rect(go.transform,"Icon",0,0,76,anchor?40:76).gameObject.AddComponent<Image>();icon.sprite=Sprite(iconPath);icon.preserveAspect=true;icon.raycastTarget=false;
            var title=v.Get<Text>("Title");Place(title.transform,84,anchor?8:24,65,32);title.fontSize=20;title.color=Color.white;title.resizeTextForBestFit=true;title.resizeTextMinSize=12;title.resizeTextMaxSize=20;
            var body=v.Get<Text>("Body");Place(body.transform,anchor?26:0,anchor?48:78,anchor?124:150,anchor?42:25);body.fontSize=15;body.color=new Color(.75f,.8f,.73f);body.verticalOverflow=VerticalWrapMode.Truncate;
            var prefab=PrefabUtility.SaveAsPrefabAsset(go,"Assets/Castle/Prefabs/V24/"+name+".prefab");PrefabUtility.UnloadPrefabContents(go);return prefab.GetComponent<CastleUiView>();
        }
        private static void Place(Transform t,float x,float y,float width,float height)
        {
            var r=(RectTransform)t;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(width,height);
        }
        private static void ResizeList(CastleUiView v,string id,float x,float y,float width,float height,bool visibleBar)
        {
            var content=v.Get<RectTransform>(id);var scroll=content.GetComponentInParent<ScrollRect>(true);
            Place(scroll.transform,x,y,width,height);Place(scroll.viewport,0,0,width-(visibleBar?45:0),height);
            content.sizeDelta=new Vector2(width-(visibleBar?45:0),height);
            var layout=content.GetComponent<VerticalLayoutGroup>();layout.spacing=0;layout.padding=new RectOffset(0,visibleBar?25:0,0,0);
            scroll.viewport.GetComponent<Image>().color=visibleBar?new Color(1,1,1,.018f):Color.clear;
            if(scroll.verticalScrollbar)
            {
                if(!visibleBar){scroll.verticalScrollbar.gameObject.SetActive(false);scroll.verticalScrollbar=null;return;}
                scroll.verticalScrollbar.gameObject.SetActive(visibleBar);
                Place(scroll.verticalScrollbar.transform,width-52,5,25,height-34);
                var bg=scroll.verticalScrollbar.GetComponent<Image>();bg.sprite=Sprite("2中控-录音文件/拆分/滑动条滑动框.png");bg.color=Color.white;
                var handle=scroll.verticalScrollbar.handleRect.GetComponent<Image>();handle.sprite=Sprite("2中控-录音文件/拆分/滑动条滑动块.png");handle.color=Color.white;
            }
        }
        private static CastleUiView RecordingCard(bool folder)
        {
            var go=new GameObject(folder?"RecordingFolder":"RecordingFile",typeof(RectTransform),typeof(Image),typeof(Button),typeof(LayoutElement),typeof(CastleUiView));
            var v=go.GetComponent<CastleUiView>();v.Id=go.name;go.GetComponent<LayoutElement>().preferredHeight=folder?94:64;
            var bg=go.GetComponent<Image>();bg.color=Color.clear;go.GetComponent<Button>().targetGraphic=bg;Bind(v,"Background",bg);Bind(v,"Select",go.GetComponent<Button>());
            var icon=Rect(go.transform,"Icon",folder?0:78,folder?5:19,folder?88:26,folder?72:26).gameObject.AddComponent<Image>();icon.preserveAspect=true;icon.raycastTarget=false;Bind(v,"Icon",icon);
            var title=Rect(go.transform,"Title",folder?104:112,folder?25:7,folder?90:83,folder?65:54).gameObject.AddComponent<Text>();title.font=_font;title.fontSize=folder?24:17;title.color=Color.white;title.raycastTarget=false;Bind(v,"Title",title);
            title.resizeTextForBestFit=true;title.resizeTextMinSize=folder?18:12;title.resizeTextMaxSize=folder?24:17;
            var body=Rect(go.transform,"Body",0,0,1,1).gameObject.AddComponent<Text>();body.font=_font;body.raycastTarget=false;Bind(v,"Body",body);
            if(!folder){var branch=Rect(go.transform,"Branch",53,0,3,64).gameObject.AddComponent<Image>();branch.color=new Color(1,1,1,.35f);branch.raycastTarget=false;var mark=Rect(go.transform,"Node",44,19,21,34).gameObject.AddComponent<Image>();mark.sprite=Sprite("2中控-录音文件/拆分/录音文件标点1.png");mark.raycastTarget=false;}
            var prefab=PrefabUtility.SaveAsPrefabAsset(go,"Assets/Castle/Prefabs/V24/"+go.name+".prefab");UnityEngine.Object.DestroyImmediate(go);return prefab.GetComponent<CastleUiView>();
        }
        private static CastleUiView RecordingTextRow()
        {
            var go=new GameObject("RecordingLine",typeof(RectTransform),typeof(Image),typeof(Button),typeof(LayoutElement),typeof(CastleUiView));var v=go.GetComponent<CastleUiView>();v.Id="RecordingLine";
            var bg=go.GetComponent<Image>();bg.color=Color.clear;go.GetComponent<Button>().targetGraphic=bg;go.GetComponent<Button>().transition=Selectable.Transition.None;
            Bind(v,"Background",bg);Bind(v,"Select",go.GetComponent<Button>());Bind(v,"Layout",go.GetComponent<LayoutElement>());
            foreach(var id in new[]{"Time","Speaker"}){var label=Rect(go.transform,id,id=="Time"?30:115,7,id=="Time"?85:700,32).gameObject.AddComponent<Text>();label.font=_font;label.fontSize=22;label.color=Color.white;label.raycastTarget=false;Bind(v,id,label);}
            var body=UnityEngine.Object.Instantiate(_ui.LinePrefab.Get<TextMeshProUGUI>("Body"),go.transform);body.name="Body";Place(body.transform,122,50,850,80);body.fontSize=18;body.color=Color.white;body.raycastTarget=false;Bind(v,"Body",body);
            var marker=Rect(go.transform,"Marker",997,45,25,40).gameObject.AddComponent<Image>();marker.sprite=Sprite("2中控-录音文件/拆分/文本标记点.png");marker.raycastTarget=false;Bind(v,"Marker",marker);
            var line=Rect(go.transform,"Divider",30,136,990,2).gameObject.AddComponent<Image>();line.sprite=Sprite("2中控-录音文件/拆分/文本分隔线.png");line.color=new Color(1,1,1,.45f);line.raycastTarget=false;
            var r=line.rectTransform;r.anchorMin=new Vector2(0,0);r.anchorMax=new Vector2(1,0);r.anchoredPosition=new Vector2(30,0);r.sizeDelta=new Vector2(-60,2);
            var prefab=PrefabUtility.SaveAsPrefabAsset(go,"Assets/Castle/Prefabs/V24/RecordingLine.prefab");UnityEngine.Object.DestroyImmediate(go);return prefab.GetComponent<CastleUiView>();
        }
        private static CastleFlowUi _ui;
        private static Font _font;
        [MenuItem("Tools/Castle/V2.4/Install Scene And Assets")]
        public static void Install()
        {
            AssetDatabase.Refresh();
            var scene=EditorSceneManager.OpenScene(CastleFlowSceneBuilder.ScenePath);
            if(UnityEngine.Object.FindObjectOfType<CastleV24Controller>(true)){Debug.Log("V24 already installed; keeping scene edits.");return;}
            Directory.CreateDirectory("Temp/v24_review");File.Copy(CastleFlowSceneBuilder.ScenePath,"Temp/v24_review/CastleEditable-before-v24.unity",true);
            _ui=UnityEngine.Object.FindObjectOfType<CastleFlowUi>(true);_font=Resources.Load<Font>("Castle/Chinese");
            var old=UnityEngine.Object.FindObjectOfType<CastleFlowController>(true);old.enabled=false;
            var game=old.gameObject.AddComponent<CastleV24Controller>();game.Ui=_ui;game.Effects=old.Audio;
            game.Music=old.gameObject.AddComponent<AudioSource>();game.Music.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Art+"audio/主界面bgm.mp3");game.Music.loop=true;game.Music.playOnAwake=true;game.Music.volume=.5f;
            game.ButlerPortrait=Sprite("角色/管家.png");game.MerchantPortrait=Sprite("角色/酒商.png");
            // 列表仍只复制已保存 Prefab，组件结构在编辑器中完成。
            var line=PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(_ui.LinePrefab));
            if(!line.GetComponentInChildren<V24LineHit>())line.GetComponentInChildren<TextMeshProUGUI>().gameObject.AddComponent<V24LineHit>();
            PrefabUtility.SaveAsPrefabAsset(line,AssetDatabase.GetAssetPath(_ui.LinePrefab));PrefabUtility.UnloadPrefabContents(line);
            foreach(var id in new[]{"Recordings","Anchors","Space"})
            {
                var v=_ui.View(id);string file=id=="Recordings"?"2中控-录音文件/拆分/背景图.png":id=="Anchors"?"3中控-锚点对齐/拆分/背景.png":"4中控-空间推演/拆分/图层 1.png";
                Backdrop(v,file);Disable(v,"Tint","Board");
                foreach(var b in v.GetComponentsInChildren<Button>(true))Skin(b,"2中控-录音文件/拆分/完成按钮底图.png");
            }
            var explore=_ui.View("Explore");Backdrop(explore,"5交谈界面/交谈界面.png");Disable(explore,"Tint");
            var portrait=Rect(explore.transform,"CharacterImage",130,225,430,540).gameObject.AddComponent<Image>();portrait.preserveAspect=true;portrait.raycastTarget=false;Bind(explore,"CharacterImage",portrait);
            portrait.transform.SetSiblingIndex(2);
            var dialogueBack=explore.transform.Find("DialoguePanel/Back").GetComponent<Image>();dialogueBack.sprite=Sprite("5交谈界面/拆分/对话框边框.png");dialogueBack.color=new Color(1,1,1,.96f);
            // 保留可读的独立文本面板，不将整张合成稿当成交互界面。
            var map=_ui.View("Map");Button(map,"Floor3","3F",1110,162,95,42);
            Button(_ui.View("Recordings"),"Extract","提取信息",1195,667,250,50);
            var journal=_ui.View("Journal");var cover=journal.transform.Find("Cover").GetComponent<Image>();cover.sprite=Sprite("6调查册-角色界面/拆分/背景.png");cover.color=Color.white;Disable(journal,"LeftPage","RightPage","Spine");
            Button(journal,"RecordFilter","切换：已验证事件",850,180,350,40);
            Button(journal,"CharacterPath","人物路径",865,652,505,50);
            Button(journal,"Source","回看来源",865,708,240,40);
            // 新弹窗与磁带播放页来自已存在页面模板，在编辑期保存全部固定控件。
            var tape=CloneView(_ui.View("Recordings"),"Tape",false);
            Disable(tape,"ListScroll","Recordings","Anchors","Space","PreviousPuzzle","NextPuzzle","PuzzleName","Done","SideDivider");
            var path=CloneView(_ui.View("Space"),"Path",true);Disable(path,"Recordings","Anchors","Space","PreviousPuzzle","NextPuzzle","PuzzleName");Button(path,"Close","返回人物详情",1180,145,280,50);
            SetupPath(_ui.View("Space"));SetupPath(path);
            var burn=CloneView(_ui.View("Map"),"Burn",true);Disable(burn,"FloorImage","Rooms","TooltipPanel","Floor1","Floor2","Floor3","Enter","Hint");
            var list=UnityEngine.Object.Instantiate(_ui.View("Recordings").transform.Find("ListScroll").gameObject,burn.transform);list.name="FilesScroll";
            var listRect=list.GetComponent<RectTransform>();listRect.anchoredPosition=new Vector2(110,-210);listRect.sizeDelta=new Vector2(950,540);
            var scroll=list.GetComponent<ScrollRect>();scroll.viewport.sizeDelta=new Vector2(920,540);scroll.content.sizeDelta=new Vector2(900,540);Bind(burn,"List",scroll.content);Button(burn,"Burn","刻录",1140,710,340,55);
            var select=CloneView(burn,"TapeSelect",true);Disable(select,"Burn");
            // 地图底图尚未提供，保留浅色占位，不冒用原型房间坐标。
            foreach(var v in new[]{map,_ui.View("Space"),path})
            {var raw=v.Get<RawImage>("FloorImage");raw.texture=null;raw.uvRect=new Rect(0,0,1,1);raw.color=new Color(.68f,.63f,.46f);}
            foreach(var id in new[]{"Map","Journal","Settings","Back"})Skin(_ui.Hud.Get<Button>(id),"2中控-录音文件/拆分/"+(id=="Map"?"地图键":id=="Journal"?"调查册":id=="Settings"?"设置":"返回键")+".png");
            // 纯连接线也是预制资源，运行时只实例化。
            Directory.CreateDirectory("Assets/Castle/Prefabs/V24");var lineGo=new GameObject("PathLine",typeof(RectTransform),typeof(Image));var image=lineGo.GetComponent<Image>();image.color=new Color(1,.8f,.3f);image.raycastTarget=false;
            var prefab=PrefabUtility.SaveAsPrefabAsset(lineGo,"Assets/Castle/Prefabs/V24/PathLine.prefab");game.PathLinePrefab=prefab.GetComponent<Image>();UnityEngine.Object.DestroyImmediate(lineGo);
            foreach(var view in _ui.Pages)view.gameObject.SetActive(view.Id=="Title");foreach(var view in _ui.Modals)view.gameObject.SetActive(false);_ui.ModalRoot.SetActive(false);_ui.Hud.gameObject.SetActive(false);_ui.Toast.gameObject.SetActive(false);
            _ui.ValidateBindings();EditorUtility.SetDirty(game);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);ExportBindings();
            Debug.Log("V24 SCENE INSTALLED");
        }
        private static void SetupPath(CastleUiView v)
        {
            Button(v,"Floor3","3F",790,238,100,40);Button(v,"ResetView","复位视图",1140,660,305,48);
            var rooms=v.Get<RectTransform>("Rooms");var viewport=Rect(v.transform,"MapViewport",385,291,748,421);viewport.gameObject.AddComponent<Image>().color=new Color(0,0,0,.03f);viewport.gameObject.AddComponent<RectMask2D>();
            rooms.SetParent(viewport,false);rooms.anchorMin=rooms.anchorMax=rooms.pivot=new Vector2(.5f,.5f);rooms.anchoredPosition=Vector2.zero;
            var pan=viewport.gameObject.AddComponent<V24MapPan>();pan.Content=rooms;Bind(v,"MapPan",pan);
        }
        private static CastleUiView CloneView(CastleUiView source,string id,bool modal)
        {
            var go=UnityEngine.Object.Instantiate(source.gameObject,modal?_ui.ModalRoot.transform:source.transform.parent);go.name=id;var v=go.GetComponent<CastleUiView>();v.Id=id;
            if(modal)_ui.Modals=_ui.Modals.Concat(new[]{v}).ToArray();else _ui.Pages=_ui.Pages.Concat(new[]{v}).ToArray();return v;
        }
        private static void Backdrop(CastleUiView view,string path){view.transform.Find("Backdrop").GetComponent<RawImage>().texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+path);}
        private static void Disable(CastleUiView view,params string[] names){foreach(var name in names){var obj=view.transform.Find(name);if(obj)obj.gameObject.SetActive(false);}}
        private static Sprite Sprite(string relative)
        {
            string path=Art+relative;var importer=(TextureImporter)AssetImporter.GetAtPath(path);if(importer.textureType!=TextureImporterType.Sprite){importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.maxTextureSize=4096;importer.SaveAndReimport();}return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        private static void Skin(Button button,string path){var image=button.GetComponent<Image>();image.sprite=Sprite(path);image.color=Color.white;}
        private static void Bind(CastleUiView v,string id,Component c){v.Controls.Add(new CastleUiBinding{Id=id,Target=c});}
        private static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
        }
        private static void Button(CastleUiView v,string id,string text,float x,float y,float w,float h)
        {
            var r=Rect(v.transform,id,x,y,w,h);var image=r.gameObject.AddComponent<Image>();image.color=new Color(.15f,.25f,.18f);var button=r.gameObject.AddComponent<Button>();button.targetGraphic=image;Bind(v,id,button);
            var label=Rect(r,"Label",8,4,w-16,h-8).gameObject.AddComponent<Text>();label.font=_font;label.text=text;label.fontSize=20;label.alignment=TextAnchor.MiddleCenter;label.raycastTarget=false;Bind(v,id+"Label",label);
        }
        [MenuItem("Tools/Castle/V2.4/Export Bindings")]
        public static void ExportBindings()
        {
            var ui=UnityEngine.Object.FindObjectOfType<CastleFlowUi>(true);if(!ui)return;
            var b=new System.Text.StringBuilder("# V2.4 UI 对象树与绑定清单\n\n固定组件已保存到 CastleEditable.unity；运行时仅复制列表 Prefab。UIFlow 挂 CastleV24Controller（旧 CastleFlowController 已禁用）。Canvas 挂 CastleFlowUi。\n\n");
            foreach(var v in ui.Pages.Concat(ui.Modals).Concat(new[]{ui.Hud,ui.Toast}))
            {
                b.Append("## "+v.Id+"\n\n| 控件 ID | 组件 | 对象路径 |\n|---|---|---|\n");
                foreach(var c in v.Controls)b.Append("| "+c.Id+" | "+c.Target.GetType().Name+" | "+FullPath(c.Target.transform)+" |\n");
                b.Append('\n');
            }
            b.Append("## 列表模板\n\nFlowCard：Button、Image、CastleUiView、FlowDrag、FlowPointer、CanvasGroup。FlowTrack：参考／移除按钮及偏移 Slider。DialogueLine：TMP、FlowTextLink、V24LineHit。PathLine：Image。RecordingFolder：可展开设备文件夹。RecordingFile：缩进文件与状态图标。RecordingLine：整条 Button、时间、说话人、正文、标记菱形与分隔线。模板资源均可直接编辑。\n");
            File.WriteAllText("UI对象树与绑定清单.md",b.ToString());
        }
        private static string FullPath(Transform t)=>t.parent?FullPath(t.parent)+"/"+t.name:t.name;
        public static void InstallValidateBuild()
        {
            Install();V24Validation.Run();
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{CastleFlowSceneBuilder.ScenePath},locationPathName="Builds/V24/CastleV24.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("V24 build failed");
        }
    }
}

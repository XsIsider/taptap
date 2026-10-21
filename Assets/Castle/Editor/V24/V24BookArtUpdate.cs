using System;
using System.Linq;
using Castle.V24;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Castle.Editor
{
    // 一次性编辑期迁移：所有固定对象和绑定保存进场景，运行时不建 UI。
    public static class V24BookArtUpdate
    {
        private const string Art = "Assets/Castle/Art/V24/";
        private static Font _font;
        private static readonly Color Ink = new Color(.14f,.12f,.09f);

        public static void ApplyAndBuild()
        {
            var scene=EditorSceneManager.OpenScene(CastleFlowSceneBuilder.ScenePath);
            var game=UnityEngine.Object.FindObjectOfType<CastleV24Controller>(true);
            _font=Resources.Load<Font>("Castle/Chinese");
            var settings=game.Ui.View("Settings");
            Bind(settings,"EffectsVolume",settings.transform.Find("SoundEffect").GetComponent<Slider>());
            Bind(settings,"ExitGame",settings.transform.Find("ExitGame").GetComponent<Button>());
            if (!game.Voice) { game.Voice=game.gameObject.AddComponent<AudioSource>();game.Voice.playOnAwake=false; }
            game.Floor1Background=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"Scenes/古堡地图1层.png");
            game.Floor2Background=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"Scenes/古堡地图2层.png");
            if(!game.Floor1Background||!game.Floor2Background)throw new Exception("新地图资源缺失");
            string cursorPath=Art+"UI/其他/iocon_talking_mouse.png";
            var cursor=(TextureImporter)AssetImporter.GetAtPath(cursorPath);
            cursor.textureType=TextureImporterType.Cursor;cursor.isReadable=true;cursor.maxTextureSize=32;cursor.mipmapEnabled=false;cursor.textureCompression=TextureImporterCompression.Uncompressed;cursor.SaveAndReimport();
            game.TalkingCursor=AssetDatabase.LoadAssetAtPath<Texture2D>(cursorPath);
            var title=game.Ui.View("Title");
            string[] buttons={"Start","Continue","Settings","Credits","Quit"};
            string[] names={"newgame","continues","options","credits","quit"};
            for(int i=0;i<buttons.Length;i++)
            {
                var button=title.Get<Button>(buttons[i]);var image=button.GetComponent<Image>();
                image.sprite=Sprite("UI/封面/word_"+names[i]+"_1.png");image.color=Color.white;
                button.targetGraphic=image;button.transition=Selectable.Transition.SpriteSwap;
                var state=button.spriteState;state.highlightedSprite=Sprite("UI/封面/word_"+names[i]+"_2.png");state.pressedSprite=state.highlightedSprite;state.selectedSprite=image.sprite;button.spriteState=state;
            }
            foreach(var art in game.CharacterArt)
            {
                string name=art.Character==200001?"heroine":art.Character==200002||art.Character==200011||art.Character==200015?"housemaid":art.Character==200003?"butler":art.Character==200004?"child":art.Character==200005?"docter":art.Character==200006?"painter":art.Character==200007?"vintner":art.Character==200008?"duchess":art.Character==200014?"tailor":"duke";
                art.Portrait=Sprite("character/character_"+name+(name=="duke"||name=="tailor"?".jpg":".png"));
                art.Height=art.Character==200004?820:art.Character==200001||name=="housemaid"?1080:1180;
                art.HeadRect=HeadRect(name);
            }
            game.ButlerPortrait=game.CharacterArt.First(x=>x.Character==200003).Portrait;
            game.MerchantPortrait=game.CharacterArt.First(x=>x.Character==200007).Portrait;
            var explore=game.Ui.View("Explore");
            var portrait=explore.Get<Image>("CharacterImage").rectTransform;
            portrait.anchorMin=portrait.anchorMax=portrait.pivot=new Vector2(.5f,0);portrait.anchoredPosition=new Vector2(-460,-400);
            var avatarPath=AssetDatabase.GetAssetPath(game.ConversationPortraitPrefab);
            var avatar=PrefabUtility.LoadPrefabContents(avatarPath);var avatarView=avatar.GetComponent<CastleUiView>();
            avatarView.Get<Image>("Portrait").gameObject.SetActive(false);
            var head=Raw(avatar.transform,"Head",15,17,80,105);Bind(avatarView,"Head",head);
            PrefabUtility.SaveAsPrefabAsset(avatar,avatarPath);PrefabUtility.UnloadPrefabContents(avatar);
            var stackHead=Raw(explore.Get<Button>("AvatarStack").transform,"Head",25,24,69,91);Bind(explore,"StackHead",stackHead);
            Book(game);
            EditorUtility.SetDirty(game);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            V24SceneSetup.ExportBindings();V24SceneSetup.InstallValidateBuild();
        }

        private static Rect HeadRect(string name)
        {
            // PNG 上缘为头部，单独校正不同人物头部的横向位置；不修改原图。
            switch(name)
            {
                case "butler":return new Rect(.42f,.76f,.40f,.24f);
                case "heroine":return new Rect(.30f,.76f,.48f,.24f);
                case "housemaid":return new Rect(.25f,.77f,.52f,.23f);
                case "child":return new Rect(.30f,.68f,.60f,.32f);
                case "duchess":return new Rect(.30f,.73f,.45f,.27f);
                default:return new Rect(.20f,.75f,.60f,.25f);
            }
        }

        private static void Book(CastleV24Controller game)
        {
            var view=game.Ui.View("Journal");
            var cover=view.transform.Find("Cover").GetComponent<Image>();cover.sprite=Sprite("UI/调查-角色/background.png");cover.color=Color.white;cover.raycastTarget=false;
            Place(cover.transform,0,0,1600,900);cover.preserveAspect=false;
            // 旧纸张底图隐藏，所有书页内容由下列可编辑拆分节点组成。
            foreach(var image in view.GetComponentsInChildren<Image>(true))
                if(image!=cover && !image.GetComponent<Button>() && image.name!="FlipSheet" && image.transform.parent==view.transform)image.gameObject.SetActive(false);
            for(int i=0;i<3;i++)
            {
                var tab=view.Get<Button>("Tab"+i);Place(tab.transform,145+i*62,27,57,78);
                tab.GetComponent<Image>().sprite=Sprite("UI/调查-角色/button_diaocha_biaoqian_1.png");tab.GetComponent<Image>().color=Color.white;
                tab.transition=Selectable.Transition.None;
                foreach(Transform child in tab.transform)child.gameObject.SetActive(false);
                Pic(tab.transform,"BookmarkIcon","UI/调查-角色/"+new[]{"icon_diaocha_people.png","icon_diaocha_search.png","icon_diaocha_prop.png"}[i],10,22,36,40);
            }
            var left=view.Get<RectTransform>("LeftCards");var right=view.Get<RectTransform>("RightCards");
            Place(left,175,155,530,610);Place(right,850,155,530,610);
            foreach(var area in new[]{left,right})
            {
                var grid=area.GetComponent<GridLayoutGroup>();grid.spacing=new Vector2(35,25);grid.padding=new RectOffset();grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;
                grid.cellSize=new Vector2(530,285);grid.constraintCount=1;
            }
            var details=view.Get<RectTransform>("Details");Place(details,0,0,1600,900);
            var character=Node(details,"CharacterDecoration",0,0,1600,900);Bind(view,"CharacterDecoration",character);
            Pic(character,"Note","UI/调查-角色/note.png",860,385,505,355);
            Pic(character,"PhotoFrame","UI/调查-角色/bg_diaocha_character_2.png",840,170,195,210);
            Pic(character,"Tape","UI/调查-角色/bg_diaocha_jiaodai.png",1035,185,295,65);
            Label(character,"Heading","人物档案",1190,85,200,55,34);
            Pic(character,"Divider","UI/调查-角色/dividing.png",870,98,280,15);
            Bind(view,"DetailHead",Raw(character,"Head",865,195,143,150));
            var record=Node(details,"RecordDecoration",0,0,1600,900);Bind(view,"RecordDecoration",record);
            Pic(record,"SourcePaper","UI/调查-记录/素材.png",850,120,190,205);
            var detailTitle=view.Get<Text>("DetailTitle");detailTitle.transform.SetParent(details,false);Style(detailTitle,28);Place(detailTitle.transform,1060,190,300,85);
            var detailBody=view.Get<Text>("DetailBody");
            var detailScroll=details.Find("DetailScroll").GetComponent<ScrollRect>();
            Place(detailScroll.transform,890,405,460,330);Bind(view,"DetailScroll",detailScroll);
            detailBody.transform.SetParent(detailScroll.transform,false);Style(detailBody,24);Place(detailBody.transform,0,0,445,330);
            detailBody.resizeTextForBestFit=false;detailBody.verticalOverflow=VerticalWrapMode.Overflow;
            detailScroll.content=detailBody.rectTransform;detailScroll.verticalNormalizedPosition=1;
            detailScroll.transform.SetAsLastSibling();
            detailTitle.transform.SetAsLastSibling();detailBody.transform.SetAsLastSibling();
            foreach(string id in new[]{"CharacterPath","Timeline"})
            {
                var action=view.Get<Button>(id);action.transform.SetParent(view.transform,false);Place(action.transform,1030,756,320,40);
                action.GetComponent<Image>().sprite=Sprite("UI/调查-角色/bg_diaocha_jiaodai.png");action.GetComponent<Image>().color=Color.white;Style(action.GetComponentInChildren<Text>(true),22);
                action.transform.SetAsLastSibling();
            }
            Place(view.Get<Button>("Previous").transform,190,780,150,40);Place(view.Get<Button>("Next").transform,1230,780,150,40);
            Place(view.Get<Text>("PageNumber").transform,740,780,120,35);
            Place(view.Get<Button>("Close").transform,1360,110,95,40);
            foreach(string id in new[]{"Previous","Next","Close"}) {var button=view.Get<Button>(id);button.transform.SetAsLastSibling();button.GetComponent<Image>().color=Color.white;button.GetComponent<Image>().sprite=Sprite("UI/调查-角色/bg_diaocha_jiaodai.png");Style(button.GetComponentInChildren<Text>(true),24);}
            view.Get<Button>("Close").GetComponentInChildren<Text>(true).text="合上";
            view.Get<Text>("PageNumber").transform.SetAsLastSibling();Style(view.Get<Text>("PageNumber"),20);
            game.PersonCardPrefab=Card("BookPersonCard",0);
            game.ItemCardPrefab=Card("BookItemCard",1);
            game.JournalRecordPrefab=Card("BookRecordCard",2);
        }

        private static CastleUiView Card(string name,int category)
        {
            var root=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button),typeof(CastleUiView));
            var view=root.GetComponent<CastleUiView>();view.Id=name;var image=root.GetComponent<Image>();image.color=Color.clear;
            root.GetComponent<Button>().transition=Selectable.Transition.None;Bind(view,"Select",root.GetComponent<Button>());Bind(view,"Background",image);
            if(category==0)
            {
                Pic(root.transform,"Frame","UI/调查-角色/bg_diaocha_character_1.png",0,0,230,285);
                Bind(view,"Head",Raw(root.transform,"Head",23,30,179,200));
                Bind(view,"Title",Label(root.transform,"Title","",16,239,198,35,21));
                Bind(view,"Body",Label(root.transform,"Body","",0,0,0,0,18));
            }
            else if(category==1)
            {
                var frame=Pic(root.transform,"ArtFrame","UI/调查-道具/新建组.png",0,25,190,190);Bind(view,"ArtFrame",frame.rectTransform);
                Bind(view,"ItemArt",Raw(root.transform,"ItemArt",15,40,160,160));
                Bind(view,"Tape",Pic(root.transform,"Tape","UI/调查-角色/bg_diaocha_jiaodai.png",215,25,295,55));
                Bind(view,"Title",Label(root.transform,"Title","",220,30,280,45,26));
                Bind(view,"Body",Label(root.transform,"Body","",220,95,280,170,22));
            }
            else
            {
                Pic(root.transform,"Tape","UI/调查-记录/从选区.png",70,115,230,42);
                Pic(root.transform,"Folder","UI/调查-记录/新建组.png",0,0,150,145);
                Bind(view,"Title",Label(root.transform,"Title","",135,118,190,50,22));
                Bind(view,"Body",Label(root.transform,"Body","",310,40,195,120,21));
            }
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,"Assets/Castle/Prefabs/V24/"+name+".prefab");UnityEngine.Object.DestroyImmediate(root);return prefab.GetComponent<CastleUiView>();
        }

        private static void Bind(CastleUiView view,string id,Component target) {view.Controls.RemoveAll(x=>x.Id==id);view.Controls.Add(new CastleUiBinding{Id=id,Target=target});}
        private static RectTransform Node(Transform parent,string name,float x,float y,float w,float h)
        {
            var rect=parent.Find(name) as RectTransform;
            if(!rect){rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);}
            rect.gameObject.SetActive(true);Place(rect,x,y,w,h);return rect;
        }
        private static void Place(Transform transform,float x,float y,float w,float h) {var rect=(RectTransform)transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);rect.localScale=Vector3.one;}
        private static Image Pic(Transform parent,string name,string path,float x,float y,float w,float h)
        {var node=Node(parent,name,x,y,w,h);var image=node.GetComponent<Image>()??node.gameObject.AddComponent<Image>();image.sprite=Sprite(path);image.color=Color.white;image.raycastTarget=false;return image;}
        private static RawImage Raw(Transform parent,string name,float x,float y,float w,float h)
        {var node=Node(parent,name,x,y,w,h);var image=node.GetComponent<RawImage>()??node.gameObject.AddComponent<RawImage>();image.raycastTarget=false;image.color=Color.white;return image;}
        private static Text Label(Transform parent,string name,string value,float x,float y,float w,float h,int size)
        {var node=Node(parent,name,x,y,w,h);var text=node.GetComponent<Text>()??node.gameObject.AddComponent<Text>();text.text=value;text.font=_font;Style(text,size);return text;}
        private static void Style(Text text,int size) {text.fontSize=size;text.color=Ink;text.raycastTarget=false;text.alignment=TextAnchor.UpperLeft;text.resizeTextForBestFit=true;text.resizeTextMinSize=16;text.resizeTextMaxSize=size;text.verticalOverflow=VerticalWrapMode.Truncate;}
        private static Sprite Sprite(string path)
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(Art+path);
            if(!importer)throw new Exception("Missing art: "+path);
            if(importer.textureType!=TextureImporterType.Sprite){importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.SaveAndReimport();}
            Rect? crop = path.EndsWith("button_diaocha_biaoqian_1.png") ? new Rect(317,966,71,81)
                : path.EndsWith("icon_diaocha_people.png") ? new Rect(258,966,38,57)
                : path.EndsWith("icon_diaocha_prop.png") ? new Rect(407,973,32,57)
                : path.EndsWith("icon_diaocha_search.png") ? new Rect(327,978,49,43)
                : path.EndsWith("/note.png") ? new Rect(943,97,612,490) : (Rect?)null;
            if(crop.HasValue)
            {
                importer.spriteImportMode=SpriteImportMode.Multiple;
                importer.spritesheet=new[]{new SpriteMetaData{name="VisibleArtwork",rect=crop.Value,pivot=new Vector2(.5f,.5f),alignment=0}};
                importer.SaveAndReimport();
                return AssetDatabase.LoadAllAssetsAtPath(Art+path).OfType<Sprite>().First();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(Art+path);
        }
    }
}

using System;
using System.IO;
using System.Linq;
using Castle.V24;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Castle.Editor
{
    public static class V24FormalUpdate
    {
        private const string Art = "Assets/Castle/Art/V24/";
        private static Font _font;
        public static void Validate()
        {
            var data = JsonUtility.FromJson<V24Data>(File.ReadAllText("SourceData/Formal20261020/GeneratedContent.json"));
            V24FormalValidation.Run(data);
        }
        public static void ApplyAndBuild()
        {
            Validate();
            File.Copy("SourceData/Formal20261020/GeneratedContent.json", "Assets/Castle/Resources/Castle/V24/Content.json", true);
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.OpenScene(CastleFlowSceneBuilder.ScenePath);
            var game = UnityEngine.Object.FindObjectOfType<CastleV24Controller>(true);
            _font = Resources.Load<Font>("Castle/Chinese");
            game.DefaultBgm = game.DefaultBgm ? game.DefaultBgm : game.Music.clip;
            game.BanquetBgm = AssetDatabase.LoadAssetAtPath<AudioClip>(Art + "audio/宴会厅.wav");
            game.PlotAudio = AssetDatabase.FindAssets("t:AudioClip", new[] { Art + "audio/CV", Art + "audio/SFX" })
                .Select(AssetDatabase.GUIDToAssetPath).Select(path => new V24AudioBinding { Key=Path.GetFileNameWithoutExtension(path), Clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path), Voice=path.Contains("/CV/") }).ToArray();
            var existing = game.CharacterArt.ToDictionary(x=>x.Character);
            int[] characters = {200001,200002,200003,200004,200005,200006,200007,200008,200009,200010,200011,200015,200018};
            game.CharacterArt = characters.Select(id=>
            {
                string name=id==200018?"tailor_jianying":id==200010||id==200011||id==200015||id==200002?"housemaid":id==200001?"heroine":id==200003?"butler":id==200004?"child":id==200005?"docter":id==200006?"painter":id==200007?"vintner":id==200008?"duchess":"duke";
                var portrait=AssetDatabase.FindAssets("character_"+name,new[]{Art+"character"}).Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(p=>Path.GetFileNameWithoutExtension(p)=="character_"+name);
                var row=existing.TryGetValue(id,out var old)?old:new V24CharacterArt {Character=id,Height=id==200004?820:1180};
                row.Portrait=portrait==null?null:Sprite(portrait);return row;
            }).ToArray();
            var space=game.Ui.View("Space");
            Button(space,"DoorOpen","打开",1265,570,95,34);Button(space,"DoorClosed","关闭",1365,570,95,34);
            Timeline(game.Ui.View("Timeline"),game);
            foreach(var view in game.Ui.GetComponentsInChildren<CastleUiView>(true))view.ValidateBindings();
            EditorUtility.SetDirty(game);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            V24SceneSetup.ExportBindings();
            Directory.CreateDirectory("Builds/V24");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=new[]{CastleFlowSceneBuilder.ScenePath},locationPathName="Builds/V24/CastleV24.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("正式版构建失败");
            Debug.Log("FORMAL_BUILD_OK");
        }
        private static void Timeline(CastleUiView view,CastleV24Controller game)
        {
            var reveal=view.Get<RectTransform>("RevealRoot");
            foreach(var image in view.GetComponentsInChildren<RawImage>(true)) if(image.transform.parent==view.transform)image.texture=game.CorridorDay;
            var back=Image(reveal,"SceneBackdrop",0,0,1600,900);back.sprite=Sprite(Art+"Scenes/走廊.png");
            if(!back.sprite){back.color=Color.clear;var rect=Rect(reveal,"ScenePhoto",0,0,1600,900);var raw=rect.GetComponent<RawImage>()??rect.gameObject.AddComponent<RawImage>();raw.texture=game.CorridorDay;raw.raycastTarget=false;rect.SetAsFirstSibling();}
            back.transform.SetAsFirstSibling();
            var heroine=Image(reveal,"Heroine",0,115,520,785);Bind(view,"Heroine",heroine);
            var partner=Image(reveal,"Partner",1150,110,450,790);Bind(view,"Partner",partner);
            var panel=Image(reveal,"DialogueArt",270,560,1050,320);panel.sprite=Sprite(Art+"UI/结局/组 3.png");panel.color=new Color(1,1,1,.65f);
            var name=Image(reveal,"NameArt",350,570,280,65);name.sprite=Sprite(Art+"UI/结局/图层 35.png");
            foreach(string id in new[]{"EndingStatus","EndingText"})
            {
                var text=view.Get<Text>(id);var oldParent=text.transform.parent;text.transform.SetParent(reveal,false);
                if(oldParent!=reveal)oldParent.gameObject.SetActive(false);
                text.transform.localScale=Vector3.one;text.color=Color.white;text.verticalOverflow=VerticalWrapMode.Overflow;text.gameObject.SetActive(true);
            }
            Place(view.Get<Text>("EndingStatus").rectTransform,380,585,220,40);view.Get<Text>("EndingStatus").fontSize=27;view.Get<Text>("EndingStatus").transform.SetAsLastSibling();
            Place(view.Get<Text>("EndingText").rectTransform,365,655,850,170);view.Get<Text>("EndingText").fontSize=28;view.Get<Text>("EndingText").transform.SetAsLastSibling();
            view.Get<RectTransform>("Wave").gameObject.SetActive(false);
            var waveRect=Rect(reveal,"SoundWave",60,200,1480,330);if(!waveRect.GetComponent<CanvasRenderer>())waveRect.gameObject.AddComponent<CanvasRenderer>();var wave=waveRect.GetComponent<V24EndingWave>()??waveRect.gameObject.AddComponent<V24EndingWave>();wave.color=Color.white;wave.raycastTarget=false;Bind(view,"SoundWave",wave);
            Place(view.Get<Button>("Play").transform as RectTransform,720,830,150,45);view.Get<Button>("Play").transform.SetAsLastSibling();
            var slots=view.Get<RectTransform>("Slots");
            var oldLayout=slots.GetComponent<HorizontalLayoutGroup>();if(oldLayout)UnityEngine.Object.DestroyImmediate(oldLayout);
            Place(slots,630,290,850,420);var grid=slots.GetComponent<GridLayoutGroup>()??slots.gameObject.AddComponent<GridLayoutGroup>();grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=5;grid.cellSize=new Vector2(154,185);grid.spacing=new Vector2(12,20);
            Place(view.Get<Text>("Hint").rectTransform,640,710,850,40);
        }
        private static Sprite Sprite(string path)
        {
            if(!File.Exists(path))return null;
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            if(importer.textureType!=TextureImporterType.Sprite){importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.SaveAndReimport();}
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        private static RectTransform Rect(Transform parent,string name,float x,float y,float width,float height)
        {
            var child=parent.Find(name);var rect=child?child as RectTransform:new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent,false);Place(rect,x,y,width,height);return rect;
        }
        private static void Place(RectTransform rect,float x,float y,float width,float height)
        {rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);}
        private static Image Image(Transform parent,string name,float x,float y,float width,float height)
        {var rect=Rect(parent,name,x,y,width,height);var image=rect.GetComponent<Image>()??rect.gameObject.AddComponent<Image>();image.raycastTarget=false;return image;}
        private static void Bind(CastleUiView view,string id,Component component)
        {view.Controls.RemoveAll(x=>x.Id==id);view.Controls.Add(new CastleUiBinding {Id=id,Target=component});}
        private static void Button(CastleUiView view,string id,string text,float x,float y,float width,float height)
        {
            var image=Image(view.transform,id,x,y,width,height);image.raycastTarget=true;image.color=new Color(.8f,.83f,.78f);
            var button=image.GetComponent<Button>()??image.gameObject.AddComponent<Button>();button.targetGraphic=image;Bind(view,id,button);
            var rect=Rect(button.transform,"Label",0,0,width,height);var label=rect.GetComponent<Text>()??rect.gameObject.AddComponent<Text>();label.font=_font;label.text=text;label.fontSize=22;label.verticalOverflow=VerticalWrapMode.Overflow;label.color=Color.black;label.alignment=TextAnchor.MiddleCenter;label.raycastTarget=false;
        }
    }
}

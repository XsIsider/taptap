using System.Linq;
using Castle.Flow;
using Castle.V24;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace Castle.Editor
{
    public static class V24ScenePolish
    {
        public static void ApplyAndBuild()
        {
            var scene=EditorSceneManager.OpenScene(CastleFlowSceneBuilder.ScenePath);var ui=Object.FindObjectOfType<CastleFlowUi>(true);var game=Object.FindObjectOfType<CastleV24Controller>(true);
            var legacy=game.GetComponent<CastleFlowController>();
            if(legacy)
            {
                var inactive=new GameObject("LegacyFlowController_Disabled");inactive.transform.SetParent(game.transform,false);inactive.SetActive(false);
                UnityEditorInternal.ComponentUtility.CopyComponent(legacy);UnityEditorInternal.ComponentUtility.PasteComponentAsNew(inactive);Object.DestroyImmediate(legacy);
            }
            var explore=ui.View("Explore");explore.transform.Find("Backdrop").GetComponent<RawImage>().texture=Resources.Load<Texture2D>("Castle/EmptyRoom");
            // 合成效果图含固定人物和对话框，作为参考保留，实际页面用独立边框与立绘。
            explore.transform.Find("FreePanel/Back").gameObject.SetActive(false);
            if(!explore.transform.Find("Protagonist"))
            {
                var go=new GameObject("Protagonist",typeof(RectTransform),typeof(Text));var r=go.GetComponent<RectTransform>();r.SetParent(explore.transform,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(55,-240);r.sizeDelta=new Vector2(225,100);var text=go.GetComponent<Text>();text.font=Resources.Load<Font>("Castle/Chinese");text.fontSize=23;text.raycastTarget=false;text.color=new Color(1,.9f,.7f);explore.Controls.Add(new CastleUiBinding{Id="Protagonist",Target=text});
            }
            var back=explore.transform.Find("DialoguePanel/Back");
            if(!explore.transform.Find("DialoguePanel/TextBackdrop"))
            {
                var panel=new GameObject("TextBackdrop",typeof(RectTransform),typeof(Image));var r=panel.GetComponent<RectTransform>();r.SetParent(back.parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(24,-50);r.sizeDelta=new Vector2(800,555);panel.GetComponent<Image>().color=new Color(.03f,.05f,.04f,.9f);panel.GetComponent<Image>().raycastTarget=false;panel.transform.SetSiblingIndex(0);
            }
            var dialogueScroll=explore.Get<ScrollRect>("DialogueScroll");((RectTransform)dialogueScroll.transform).anchoredPosition=new Vector2(25,-75);((RectTransform)dialogueScroll.transform).sizeDelta=new Vector2(800,340);dialogueScroll.viewport.sizeDelta=new Vector2(784,340);
            // 单独的地点模板，避免常规长卡片在地图上发生文字裁切。
            if(!game.MapCardPrefab)
            {
                var card=PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(ui.CardPrefab));var view=card.GetComponent<CastleUiView>();
                var title=view.Get<Text>("Title");title.fontSize=18;title.rectTransform.anchoredPosition=new Vector2(8,-6);title.rectTransform.sizeDelta=new Vector2(-16,44);title.alignment=TextAnchor.MiddleCenter;
                var body=view.Get<Text>("Body");body.fontSize=15;body.rectTransform.anchoredPosition=new Vector2(8,-52);body.rectTransform.sizeDelta=new Vector2(-16,30);body.alignment=TextAnchor.MiddleCenter;
                var prefab=PrefabUtility.SaveAsPrefabAsset(card,"Assets/Castle/Prefabs/V24/MapCard.prefab");game.MapCardPrefab=prefab.GetComponent<CastleUiView>();PrefabUtility.UnloadPrefabContents(card);
            }
            var journal=ui.View("Journal");
            if(!journal.transform.Find("Details/DetailScroll"))
            {
                var parent=journal.Get<RectTransform>("Details");var go=new GameObject("DetailScroll",typeof(RectTransform),typeof(Image),typeof(RectMask2D),typeof(ScrollRect));var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(0,-85);r.sizeDelta=new Vector2(555,345);go.GetComponent<Image>().color=new Color(0,0,0,.001f);
                var body=journal.Get<Text>("DetailBody");body.transform.SetParent(r,false);body.rectTransform.anchorMin=new Vector2(0,1);body.rectTransform.anchorMax=new Vector2(1,1);body.rectTransform.pivot=new Vector2(0,1);body.rectTransform.anchoredPosition=Vector2.zero;body.rectTransform.sizeDelta=new Vector2(-12,345);body.verticalOverflow=VerticalWrapMode.Overflow;body.fontSize=22;
                body.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
                var scroll=go.GetComponent<ScrollRect>();scroll.viewport=r;scroll.content=body.rectTransform;scroll.horizontal=false;scroll.scrollSensitivity=30;scroll.movementType=ScrollRect.MovementType.Clamped;
            }
            foreach(var v in ui.Pages.Concat(ui.Modals))
            foreach(var button in v.GetComponentsInChildren<Button>(true))
            {
                var image=button.GetComponent<Image>();
                if(image && image.sprite && image.sprite.name=="完成按钮底图")image.color=new Color(.18f,.29f,.21f);
            }
            foreach(var id in new[]{"Burn","TapeSelect"})
            {
                var scroll=ui.View(id).transform.Find("FilesScroll").GetComponent<ScrollRect>();
                var bar=scroll.verticalScrollbar.GetComponent<RectTransform>();bar.anchoredPosition=new Vector2(936,0);bar.sizeDelta=new Vector2(9,540);
            }
            game.FolderClosed=LoadSprite("2中控-录音文件/拆分/文件夹1.png");game.FolderOpen=LoadSprite("2中控-录音文件/拆分/打开文件夹.png");
            game.PlayIcon=LoadSprite("2中控-录音文件/拆分/录音文件播放.png");game.PauseIcon=LoadSprite("2中控-录音文件/拆分/录音文件暂停.png");
            foreach(var id in new[]{"Recordings","Tape"})
            {
                var button=ui.View(id).Get<Button>("Play");button.GetComponent<Image>().sprite=game.PlayIcon;button.GetComponent<Image>().color=Color.white;((RectTransform)button.transform).sizeDelta=new Vector2(50,50);
            }
            if(!game.PersonCardPrefab)
            {
                var card=PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(ui.CardPrefab));var view=card.GetComponent<CastleUiView>();
                view.Get<Image>("Background").sprite=LoadSprite("6调查册-角色界面/拆分/左上角色框底色.png");view.Get<Image>("Background").color=Color.white;
                var picture=new GameObject("Portrait",typeof(RectTransform),typeof(Image));var r=picture.GetComponent<RectTransform>();r.SetParent(card.transform,false);r.anchorMin=new Vector2(.15f,.17f);r.anchorMax=new Vector2(.85f,.92f);r.offsetMin=r.offsetMax=Vector2.zero;picture.GetComponent<Image>().preserveAspect=true;picture.GetComponent<Image>().raycastTarget=false;
                view.Controls.Add(new CastleUiBinding{Id="Portrait",Target=picture.GetComponent<Image>()});
                var frame=new GameObject("Frame",typeof(RectTransform),typeof(Image));var f=frame.GetComponent<RectTransform>();f.SetParent(card.transform,false);f.anchorMin=Vector2.zero;f.anchorMax=Vector2.one;f.offsetMin=f.offsetMax=Vector2.zero;frame.GetComponent<Image>().sprite=LoadSprite("6调查册-角色界面/拆分/左上角色框.png");frame.GetComponent<Image>().raycastTarget=false;
                var title=view.Get<Text>("Title");title.rectTransform.anchoredPosition=new Vector2(16,-168);title.rectTransform.sizeDelta=new Vector2(-32,32);title.fontSize=20;title.color=new Color(.12f,.1f,.07f);title.alignment=TextAnchor.MiddleCenter;title.transform.SetAsLastSibling();
                var body=view.Get<Text>("Body");body.rectTransform.anchoredPosition=new Vector2(16,-80);body.rectTransform.sizeDelta=new Vector2(-32,60);body.fontSize=18;body.color=new Color(.25f,.22f,.18f);body.alignment=TextAnchor.MiddleCenter;
                var prefab=PrefabUtility.SaveAsPrefabAsset(card,"Assets/Castle/Prefabs/V24/PersonCard.prefab");game.PersonCardPrefab=prefab.GetComponent<CastleUiView>();PrefabUtility.UnloadPrefabContents(card);
            }
            if(!game.ItemCardPrefab)
            {
                var card=PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(ui.CardPrefab));var view=card.GetComponent<CastleUiView>();view.Get<Image>("Background").sprite=LoadSprite("6调查册-角色界面/拆分/照片底色.png");view.Get<Image>("Background").color=Color.white;view.Get<Text>("Title").color=new Color(.18f,.12f,.07f);view.Get<Text>("Body").color=new Color(.18f,.16f,.12f);view.Get<Text>("Body").rectTransform.sizeDelta=new Vector2(-32,132);
                var prefab=PrefabUtility.SaveAsPrefabAsset(card,"Assets/Castle/Prefabs/V24/ItemCard.prefab");game.ItemCardPrefab=prefab.GetComponent<CastleUiView>();PrefabUtility.UnloadPrefabContents(card);
            }
            var track=PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(ui.TrackPrefab));track.transform.Find("Back").GetComponent<Image>().sprite=LoadSprite("3中控-锚点对齐/拆分/轨道槽位背景.png");track.transform.Find("Back").GetComponent<Image>().color=new Color(.18f,.27f,.2f);
            if(!track.GetComponent<FlowDrag>())track.AddComponent<FlowDrag>();
            PrefabUtility.SaveAsPrefabAsset(track,AssetDatabase.GetAssetPath(ui.TrackPrefab));PrefabUtility.UnloadPrefabContents(track);
            foreach(var v in new[]{ui.View("Recordings"),ui.View("Anchors"),ui.View("Space")})
            foreach(var slider in v.GetComponentsInChildren<Slider>(true))slider.targetGraphic.GetComponent<Image>().sprite=LoadSprite("3中控-锚点对齐/拆分/锚点图标.png");
            foreach(var id in new[]{"Space","Path"})
            {
                var view=ui.View(id);
                foreach(var name in new[]{"ZoomIn","ZoomOut"})
                {
                    if(view.Controls.Any(x=>x.Id==name))continue;
                    var go=Object.Instantiate(view.Get<Button>("ResetView").gameObject,view.transform);go.name=name;var r=go.GetComponent<RectTransform>();r.anchoredPosition=new Vector2(name=="ZoomIn"?1140:1210,-610);r.sizeDelta=new Vector2(60,40);go.GetComponentInChildren<Text>().text=name=="ZoomIn"?"＋":"－";view.Controls.Add(new CastleUiBinding{Id=name,Target=go.GetComponent<Button>()});
                }
                if(!view.Controls.Any(x=>x.Id=="Hypothesis"))
                {
                    var go=new GameObject("Hypothesis",typeof(RectTransform),typeof(Image),typeof(InputField));var r=go.GetComponent<RectTransform>();r.SetParent(view.transform,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(1140,-485);r.sizeDelta=new Vector2(305,115);var image=go.GetComponent<Image>();image.sprite=LoadSprite("4中控-空间推演/拆分/信息假设底框.png");image.color=new Color(.2f,.3f,.22f);
                    var textGo=new GameObject("Text",typeof(RectTransform),typeof(Text));var tr=textGo.GetComponent<RectTransform>();tr.SetParent(r,false);tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=new Vector2(10,6);tr.offsetMax=new Vector2(-10,-6);var text=textGo.GetComponent<Text>();text.font=Resources.Load<Font>("Castle/Chinese");text.fontSize=18;text.supportRichText=false;
                    var placeholder=Object.Instantiate(textGo,go.transform).GetComponent<Text>();placeholder.name="Placeholder";placeholder.text="记录信息假设（可选）";placeholder.color=new Color(.9f,.9f,.8f,.5f);placeholder.raycastTarget=false;
                    var input=go.GetComponent<InputField>();input.textComponent=text;input.placeholder=placeholder;input.targetGraphic=image;input.lineType=InputField.LineType.MultiLineNewline;view.Controls.Add(new CastleUiBinding{Id="Hypothesis",Target=input});
                }
            }
            EditorUtility.SetDirty(game);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();V24SceneSetup.ExportBindings();V24SceneSetup.InstallValidateBuild();
        }
        private static Sprite LoadSprite(string relative)
        {
            string path="Assets/Castle/Art/V24/"+relative;var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            if(importer.textureType!=TextureImporterType.Sprite){importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.SaveAndReimport();}
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}

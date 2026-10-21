using Castle.Flow;
using Castle.V24;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace Castle.Editor
{
    /// <summary>定向绑定用户补充的场景素材，不重建已有场景布局。</summary>
    public static class V24SceneArtUpdate
    {
        public static void ApplyAndBuild()
        {
            var scene = EditorSceneManager.OpenScene(CastleFlowSceneBuilder.ScenePath);
            var game = Object.FindObjectOfType<CastleV24Controller>(true);
            const string root = "Assets/Castle/Art/V24/Scenes/";
            game.BanquetBackground = AssetDatabase.LoadAssetAtPath<Texture2D>(root + "宴会大厅.png");
            game.CorridorBackground = AssetDatabase.LoadAssetAtPath<Texture2D>(root + "走廊.png");
            game.Floor1Background = AssetDatabase.LoadAssetAtPath<Texture2D>(root + "古堡地图一层.png");
            game.Floor2Background = AssetDatabase.LoadAssetAtPath<Texture2D>(root + "古堡地图二层.png");
            if (!game.BanquetBackground || !game.CorridorBackground)
                throw new System.InvalidOperationException("缺少用户提供的场景素材");
            foreach(var id in new[]{"Map","Space","Path"})
            {
                var view=game.Ui.View(id);
                var old=view.Controls.FirstOrDefault(x=>x.Id=="Floor3");
                if(old!=null){Object.DestroyImmediate(old.Target.gameObject);view.Controls.RemoveAll(x=>x.Id=="Floor3"||x.Id=="Floor3Label");}
                view.Get<RawImage>("FloorImage").gameObject.SetActive(false);
                var rooms=view.Get<RectTransform>("Rooms");
                var image=rooms.GetComponent<RawImage>();if(!image)image=rooms.gameObject.AddComponent<RawImage>();
                image.texture=game.Floor1Background;image.uvRect=new Rect(.14f,.18f,.74f,.64f);image.color=Color.white;image.raycastTarget=false;
                view.Controls.First(x=>x.Id=="FloorImage").Target=image;
                rooms.gameObject.SetActive(true);
                if(id!="Map")foreach(var name in new[]{"ZoomIn","ZoomOut"})
                {
                    var label=view.Get<Button>(name).GetComponentInChildren<Text>(true);var r=label.rectTransform;
                    r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;label.alignment=TextAnchor.MiddleCenter;
                }
            }
            foreach(var id in new[]{"Cutscene","EndCard"})
            {
                if(game.Ui.Pages.Any(x=>x.Id==id))continue;
                var go=new GameObject(id,typeof(RectTransform),typeof(Image),typeof(Button),typeof(CastleUiView));
                var rect=go.GetComponent<RectTransform>();rect.SetParent(game.Ui.View("Explore").transform.parent,false);rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
                var image=go.GetComponent<Image>();image.color=Color.black;go.GetComponent<Button>().targetGraphic=image;
                var view=go.GetComponent<CastleUiView>();view.Id=id;view.Controls.Add(new CastleUiBinding{Id="Surface",Target=go.GetComponent<Button>()});
                var body=new GameObject("Body",typeof(RectTransform),typeof(Text));var r=body.GetComponent<RectTransform>();r.SetParent(rect,false);r.anchorMin=new Vector2(.15f,.2f);r.anchorMax=new Vector2(.85f,.8f);r.offsetMin=r.offsetMax=Vector2.zero;
                var text=body.GetComponent<Text>();text.font=Resources.Load<Font>("Castle/Chinese");text.fontSize=30;text.alignment=TextAnchor.MiddleCenter;text.color=Color.white;text.raycastTarget=false;
                view.Controls.Add(new CastleUiBinding{Id="Body",Target=text});game.Ui.Pages=game.Ui.Pages.Concat(new[]{view}).ToArray();go.SetActive(false);
            }
            EditorUtility.SetDirty(game);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            V24SceneSetup.ExportBindings();
            V24SceneSetup.InstallValidateBuild();
        }
    }
}

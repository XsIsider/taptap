using System;
using System.Linq;
using Castle.Flow;
using Castle.V24;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Castle.Editor
{
    public static class V24PresentationUpdate
    {
        private const string Art = "Assets/Castle/Art/V24/";
        public static void ApplyAndBuild()
        {
            var scene = EditorSceneManager.OpenScene(CastleFlowSceneBuilder.ScenePath);
            var game = UnityEngine.Object.FindObjectOfType<CastleV24Controller>(true);
            var hud = game.Ui.Hud;
            var group = hud.GetComponentsInChildren<Transform>(true).First(x => x.name == "bg_location");
            Bind(hud, "LocationGroup", group);
            Place(group, 95, 18, 440, 90); group.localScale = Vector3.one;
            var location = hud.Get<Text>("Location");
            location.transform.SetParent(group, false); Place(location.transform, 48, 3, 380, 40); location.transform.localScale = Vector3.one; location.fontSize = 20; location.verticalOverflow = VerticalWrapMode.Overflow; location.alignment = TextAnchor.MiddleLeft;
            var time = Text(hud, "CurrentTime", group, location); Place(time.transform, 48, 45, 380, 40); time.fontSize = 20; time.verticalOverflow = VerticalWrapMode.Overflow; time.alignment = TextAnchor.MiddleLeft;
            int iconIndex=0; foreach (var icon in group.GetComponentsInChildren<Image>(true).Where(x=>x.transform!=group)) { Place(icon.transform,12,iconIndex++==0?48:5,28,32); icon.transform.localScale=Vector3.one; icon.preserveAspect=true; }
            var explore = game.Ui.View("Explore");
            var heroine = Image(explore, "HeroineImage");
            var right = explore.Get<Image>("CharacterImage");
            foreach (var image in new[] {heroine, right})
            {
                var r = image.rectTransform; r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f, 0);
                r.anchoredPosition = new Vector2(image == heroine ? -530 : -50, 15);
                r.sizeDelta = new Vector2(410, 700); image.raycastTarget = false; image.preserveAspect = true;
                image.transform.SetSiblingIndex(1);
            }
            var backdrop = explore.transform.Find("Backdrop").GetComponent<RectTransform>();
            backdrop.pivot = new Vector2(.5f, .5f); backdrop.anchorMin = Vector2.zero; backdrop.anchorMax = Vector2.one;
            backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
            var hint = Text(explore, "ZoomHint", explore.transform, location);
            Place(hint.transform, 650, 850, 300, 35); hint.text = "空格键还原"; hint.alignment = TextAnchor.MiddleCenter; hint.raycastTarget = false;
            var people = explore.Get<RectTransform>("People");
            var scroll = people.GetComponentInParent<ScrollRect>(true);
            scroll.viewport.GetComponent<Image>().raycastTarget = false;
            Place(scroll.transform, 36, 744, 920, 145); Place(scroll.viewport, 0, 0, 920, 145);
            var stack = explore.transform.Find("AvatarStack");
            if (!stack)
            {
                stack = new GameObject("AvatarStack", typeof(RectTransform), typeof(Image), typeof(Button)).transform;
                stack.SetParent(explore.transform, false);
                var label = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
                label.transform.SetParent(stack, false); label.font = location.font; label.text = "";
            }
            Place(stack, 36, 744, 112, 143);
            stack.GetComponent<Image>().sprite = Sprite("UI/5交谈界面/拆分/bg_talking_avatarframe.png");
            stack.GetComponent<Image>().preserveAspect = true;
            Bind(explore, "AvatarStack", stack.GetComponent<Button>());
            // 展开后列表覆盖堆叠入口，避免入口挡住第一张头像。
            stack.SetSiblingIndex(scroll.transform.GetSiblingIndex());

            var prefabPath = AssetDatabase.GetAssetPath(game.ConversationPortraitPrefab);
            var prefab = PrefabUtility.LoadPrefabContents(prefabPath);
            var frame = prefab.transform.Find("Frame").GetComponent<Image>();
            frame.sprite = Sprite("UI/5交谈界面/拆分/选择角色.png"); frame.preserveAspect = true;
            frame.transform.SetAsFirstSibling(); // 素材中心不透明，肖像需要画在框的内侧上层。
            prefab.GetComponent<CastleUiView>().Get<Image>("Portrait").transform.SetAsLastSibling();
            prefab.GetComponent<CastleUiView>().Get<Text>("Title").transform.SetAsLastSibling();
            PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath); PrefabUtility.UnloadPrefabContents(prefab);

            game.RoomArt = new[] {
                Room(100001,"客房"), Room(100002,"宴会大厅","夜晚"), Room(100003,"中控台",allowZoom:false), Room(100005,"厨房"),
                Room(100006,"收藏厅"), Room(100023,"议会厅"), Room(100012,"客房"), Room(100013,"客房"),
                Room(100015,"客房"), Room(100024,"客房"), Room(100025,"客房"), Room(100026,"客房"),
                Room(100027,"客房"), Room(100028,"客房"), Room(100029,"客房"), Room(100030,"客房")
            };
            game.CorridorDay = Texture("走廊 白天.png"); game.CorridorNight = Texture("走廊 黑夜.png");
            game.CharacterArt = new[] {
                Character(200001,"heroine",650), Character(200002,"housemaid",630), Character(200003,"butler",740),
                Character(200004,"boy",570), Character(200005,"docter",700), Character(200006,"youngster",720,"jpg"),
                Character(200007,"vintner",720), Character(200008,"duchess",740), Character(200009,"duke",740,"jpg"),
                Character(200010,"duke",740,"jpg"), Character(200011,"housemaid",630), Character(200014,"tailor",700,"jpg"),
                Character(200015,"housemaid",630)
            };
            game.ButlerPortrait = game.CharacterArt.First(x => x.Character == 200003).Portrait;
            game.MerchantPortrait = game.CharacterArt.First(x => x.Character == 200007).Portrait;
            game.ConversationChoices = new[] { Choice(900086,200008),Choice(900087,200006),Choice(900088,200005),Choice(900093,200007),Choice(900094,200014),Choice(900095,200004) };
            EditorUtility.SetDirty(game); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            V24SceneSetup.ExportBindings(); V24SceneSetup.InstallValidateBuild();
        }
        private static V24ConversationChoice Choice(int ev, int character) => new V24ConversationChoice { Event = ev, Character = character };
        private static V24RoomArt Room(int id, string name, string night = "黑夜", bool allowZoom = true) => new V24RoomArt { Room = id, Day = Texture(name == "中控台" ? "中控台白天.png" : name + " 白天.png"), Night = Texture(name + " " + night + ".png"), AllowZoom = allowZoom };
        private static Texture2D Texture(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "Scenes/" + name) ?? throw new Exception("Missing scene art: " + name);
        private static V24CharacterArt Character(int id, string name, float height, string extension = "png") => new V24CharacterArt { Character = id, Portrait = Sprite("character/character_" + name + "." + extension), Height = height };
        private static Sprite Sprite(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(Art + path);
            if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single; importer.alphaIsTransparency = true; importer.SaveAndReimport(); }
            return AssetDatabase.LoadAssetAtPath<Sprite>(Art + path);
        }
        private static void Bind(CastleUiView view, string id, Component component)
        {
            view.Controls.RemoveAll(x => x.Id == id); view.Controls.Add(new CastleUiBinding { Id = id, Target = component });
        }
        private static Image Image(CastleUiView view, string id)
        {
            if (view.TryGet<Image>(id, out var image)) return image;
            image = new GameObject(id, typeof(RectTransform), typeof(Image)).GetComponent<Image>(); image.transform.SetParent(view.transform, false); Bind(view,id,image); return image;
        }
        private static Text Text(CastleUiView view, string id, Transform parent, Text style)
        {
            if (view.TryGet<Text>(id, out var text)) return text;
            text = new GameObject(id, typeof(RectTransform), typeof(Text)).GetComponent<Text>(); text.transform.SetParent(parent,false);
            text.font = style.font; text.fontSize = style.fontSize; text.color = style.color; text.raycastTarget = false;
            Bind(view,id,text); return text;
        }
        private static void Place(Transform transform, float x,float y,float width,float height)
        {
            var r=(RectTransform)transform; r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1); r.anchoredPosition=new Vector2(x,-y); r.sizeDelta=new Vector2(width,height);
        }
    }
}

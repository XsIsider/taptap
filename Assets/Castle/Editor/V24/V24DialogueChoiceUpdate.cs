using System;
using System.Linq;
using Castle.V24;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Castle.Editor
{
    /// <summary>按交谈参考图创建独立选项模板；素材不齐时不修改场景。</summary>
    public static class V24DialogueChoiceUpdate
    {
        [MenuItem("Tools/Castle/Update Dialogue Choices And Build")]
        public static void ApplyAndBuild()
        {
            var normal = FindSprite("button_talking_01");
            var highlight = FindSprite("button_talking_02");
            var pressed = FindSprite("button_talking_03");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.isDirty) throw new Exception("请先保存当前场景");
            if (scene.path != CastleFlowSceneBuilder.ScenePath) scene = EditorSceneManager.OpenScene(CastleFlowSceneBuilder.ScenePath);
            var game = UnityEngine.Object.FindObjectOfType<CastleV24Controller>(true);
            var root = new GameObject("DialogueChoice", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(CastleUiView), typeof(V24DialogueChoice));
            var view = root.GetComponent<CastleUiView>();view.Id = "DialogueChoice";
            var image = root.GetComponent<Image>();image.sprite = normal;image.color = new Color(1,1,1,.28f);image.type = Image.Type.Simple;
            var button = root.GetComponent<Button>();button.targetGraphic = image;button.transition = Selectable.Transition.None;
            var layout = root.GetComponent<LayoutElement>();layout.preferredHeight = 54;layout.minHeight = 46;
            var glow = new GameObject("HighlightMask", typeof(RectTransform), typeof(Image), typeof(Mask));glow.transform.SetParent(root.transform, false);
            var glowRect = (RectTransform)glow.transform;glowRect.anchorMin = Vector2.zero;glowRect.anchorMax = Vector2.one;glowRect.offsetMin = new Vector2(2,2);glowRect.offsetMax = new Vector2(-2,-2);
            glow.GetComponent<Image>().sprite = highlight;glow.GetComponent<Image>().raycastTarget = false;glow.GetComponent<Mask>().showMaskGraphic = false;
            var texture = new GameObject("GoldTexture", typeof(RectTransform), typeof(Image));texture.transform.SetParent(glow.transform, false);
            var textureRect = (RectTransform)texture.transform;textureRect.anchorMin = Vector2.zero;textureRect.anchorMax = Vector2.one;textureRect.offsetMin = textureRect.offsetMax = Vector2.zero;
            texture.GetComponent<Image>().sprite = pressed;texture.GetComponent<Image>().color = new Color(1,1,1,.12f);texture.GetComponent<Image>().raycastTarget = false;
            var appearance = root.GetComponent<V24DialogueChoice>();appearance.Border = image;appearance.Glow = glow;appearance.FirstBorder = normal;appearance.RegularBorder = highlight;glow.SetActive(false);
            var title = TextChild(root.transform, "Title", game.ReadingFont);
            title.fontSize = 21;title.alignment = TextAnchor.MiddleCenter;title.color = Color.white;
            title.resizeTextForBestFit = true;title.resizeTextMinSize = 15;title.resizeTextMaxSize = 21;
            var body = TextChild(root.transform, "Body", game.ReadingFont);body.gameObject.SetActive(false);
            view.Controls.Add(new CastleUiBinding { Id = "Title", Target = title });
            view.Controls.Add(new CastleUiBinding { Id = "Body", Target = body });
            view.Controls.Add(new CastleUiBinding { Id = "Select", Target = button });
            view.Controls.Add(new CastleUiBinding { Id = "Background", Target = image });
            game.DialogueChoicePrefab = PrefabUtility.SaveAsPrefabAsset(root, "Assets/Castle/Prefabs/V24/DialogueChoice.prefab").GetComponent<CastleUiView>();
            UnityEngine.Object.DestroyImmediate(root);
            var choices = game.Ui.View("Explore").Get<RectTransform>("Choices");
            var horizontal = choices.GetComponent<HorizontalLayoutGroup>();
            if (horizontal) UnityEngine.Object.DestroyImmediate(horizontal);
            var vertical = choices.GetComponent<VerticalLayoutGroup>();
            if (!vertical) vertical = choices.gameObject.AddComponent<VerticalLayoutGroup>();
            vertical.spacing = 8;vertical.childAlignment = TextAnchor.LowerCenter;
            vertical.childControlHeight = vertical.childControlWidth = true;
            vertical.childForceExpandHeight = false;vertical.childForceExpandWidth = true;
            choices.anchorMin = choices.anchorMax = choices.pivot = new Vector2(0, 1);
            choices.anchoredPosition = new Vector2(50, -586);choices.sizeDelta = new Vector2(425, 162);
            EditorUtility.SetDirty(game);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            V24OverlayUpdate.BuildCurrent();
        }

        private static Sprite FindSprite(string name)
        {
            string path = "Assets/Castle/Art/V24/UI/5交谈界面/拆分/" + name + ".png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;importer.spriteImportMode = SpriteImportMode.Single;importer.alphaIsTransparency = true;importer.SaveAndReimport();
            }
            var sprites = AssetDatabase.FindAssets(name, new[] { "Assets/Castle/Art/V24/UI" })
                .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Sprite>).Where(x => x && x.name == name).ToArray();
            if (sprites.Length != 1) throw new Exception("需要唯一的按钮 Sprite 素材：" + name);
            return sprites[0];
        }

        private static Text TextChild(Transform parent, string name, Font font)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;rect.anchorMin = Vector2.zero;rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(22, 3);rect.offsetMax = new Vector2(-22, -3);
            var text = go.GetComponent<Text>();text.font = font;text.raycastTarget = false;return text;
        }
    }
}

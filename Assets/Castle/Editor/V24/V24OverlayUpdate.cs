using System;
using System.Linq;
using Castle.V24;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Castle.Editor
{
    /// <summary>定向更新获得图片、刻录列表和制作名单，不重排其他页面。</summary>
    public static class V24OverlayUpdate
    {
        [MenuItem("Tools/Castle/Update Overlays And Build")]
        public static void ApplyAndBuild()
        {
            V24FormalUpdate.Validate();
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.isDirty) throw new Exception("请先保存当前场景");
            if (scene.path != CastleFlowSceneBuilder.ScenePath) scene = EditorSceneManager.OpenScene(CastleFlowSceneBuilder.ScenePath);
            var game = UnityEngine.Object.FindObjectOfType<CastleV24Controller>(true);
            var acquire = game.Ui.View("Acquire");
            var iconBack = acquire.transform.Find("IconBack");
            iconBack.GetComponent<Image>().enabled = false;
            var art = acquire.Get<RawImage>("ItemArt");
            art.transform.SetParent(iconBack, false);
            art.color = Color.white;
            art.raycastTarget = false;
            art.uvRect = new Rect(0, 0, 1, 1);
            Stretch(art.rectTransform);
            // 文字只在没有对应美术时作为类型提示，不能与旧静态图叠加。
            var icon = acquire.Get<Text>("Icon");
            icon.gameObject.SetActive(true);
            var burn = game.Ui.View("Burn");
            var source = game.Ui.View("Recordings").transform.Find("ListScroll").GetComponent<ScrollRect>();
            var scroll = burn.transform.Find("FilesScroll").GetComponent<ScrollRect>();
            scroll.movementType = source.movementType;
            scroll.scrollSensitivity = source.scrollSensitivity;
            scroll.horizontal = false;
            scroll.vertical = true;
            var layout = scroll.content.GetComponent<VerticalLayoutGroup>();
            var sourceLayout = source.content.GetComponent<VerticalLayoutGroup>();
            EditorUtility.CopySerialized(sourceLayout, layout);
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            var fitter = scroll.content.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var content = scroll.content;
            content.anchorMin = new Vector2(0, 1);content.anchorMax = Vector2.one;content.pivot = new Vector2(0, 1);
            content.anchoredPosition = Vector2.zero;content.sizeDelta = Vector2.zero;
            CreateCredits(game);
            foreach (var view in game.Ui.Modals) view.ValidateBindings();
            EditorUtility.SetDirty(game.Ui);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            V24SceneSetup.ExportBindings();
            BuildCurrent();
        }

        public static void BuildCurrent()
        {
            V24FormalUpdate.Validate();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { CastleFlowSceneBuilder.ScenePath }, locationPathName = "Builds/V24/CastleV24.exe", target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("弹窗更新构建失败");
            Debug.Log("OVERLAY_UPDATE_OK");
        }

        private static void CreateCredits(CastleV24Controller game)
        {
            if (game.Ui.Modals.Any(x => x.Id == "Credits")) return;
            var root = new GameObject("Credits", typeof(RectTransform), typeof(Image), typeof(CastleUiView));
            root.transform.SetParent(game.Ui.ModalRoot.transform, false);
            Stretch((RectTransform)root.transform);
            root.GetComponent<Image>().color = Color.black;
            var view = root.GetComponent<CastleUiView>();view.Id = "Credits";
            Label(root.transform, "Heading", "制作名单", game.ReadingFont, 40, 110, 66, 1200, 65);
            string[] roles = { "主策", "程序", "主美", "角色美术", "场景美术", "UI", "道具美术", "文案策划", "剧情编剧", "音效", "CV鸣谢" };
            string[] names = { "角角", "匡匡、Xs", "柚子", "大海参、第十名、枫沅、浔木司", "味精、柚子", "一头驴", "Zoe", "云外秋", "季雨橙、孟清秋", "苏泠冰", "菜得扣饺、橙帝、川声、kira、希夜、XXS（栖）、执棋" };
            for (int i = 0; i < roles.Length; i++)
            {
                Label(root.transform, "Role" + i, "【" + roles[i] + "】", game.ReadingFont, 26, 205, 175 + i * 53, 235, 48);
                Label(root.transform, "Name" + i, names[i], game.ReadingFont, 26, 455, 175 + i * 53, 1030, 48);
            }
            var close = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
            close.transform.SetParent(root.transform, false);
            Place((RectTransform)close.transform, 1400, 55, 130, 55);
            close.GetComponent<Image>().color = new Color(.14f, .14f, .14f);
            close.GetComponent<Button>().targetGraphic = close.GetComponent<Image>();
            var text = Label(close.transform, "Label", "关闭 ×", game.ReadingFont, 25, 0, 0, 130, 55);
            text.alignment = TextAnchor.MiddleCenter;
            view.Controls.Add(new CastleUiBinding { Id = "Close", Target = close.GetComponent<Button>() });
            game.Ui.Modals = game.Ui.Modals.Concat(new[] { view }).ToArray();
            root.SetActive(false);
        }

        private static Text Label(Transform parent, string name, string value, Font font, int size, float x, float y, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));go.transform.SetParent(parent, false);
            Place((RectTransform)go.transform, x, y, width, height);
            var text = go.GetComponent<Text>();text.text = value;text.font = font;text.fontSize = size;text.color = Color.white;
            text.raycastTarget = false;text.alignment = TextAnchor.MiddleLeft;
            return text;
        }

        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);rect.sizeDelta = new Vector2(width, height);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;rect.anchorMax = Vector2.one;rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}

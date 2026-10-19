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
    /// <summary>只更新配置、道具引用和新增图像控件，保留现有手工场景布局。</summary>
    public static class V24ConfigRefresh
    {
        [MenuItem("Tools/Castle/Refresh Updated Config And Build %#u")]
        public static void ApplyAndBuild()
        {
            try
            {
                V24FormalUpdate.Validate();
                File.Copy("SourceData/Formal20261020/GeneratedContent.json", "Assets/Castle/Resources/Castle/V24/Content.json", true);
                AssetDatabase.Refresh();
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                if (scene.path != CastleFlowSceneBuilder.ScenePath)
                {
                    if (scene.isDirty) throw new Exception("请先保存当前场景，再执行配置更新");
                    scene = EditorSceneManager.OpenScene(CastleFlowSceneBuilder.ScenePath);
                }
                var game = UnityEngine.Object.FindObjectOfType<CastleV24Controller>(true);
                var data = JsonUtility.FromJson<V24Data>(File.ReadAllText("SourceData/Formal20261020/GeneratedContent.json"));
                game.ItemArt = data.Entry.SelectMany(x => new[] { x.Icon, x.DetailImage }).Where(x => !string.IsNullOrEmpty(x) && x.StartsWith("icon_")).Distinct().OrderBy(x => x)
                    .Select(key => new V24ItemArt { Key = key, Texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Castle/Art/V24/道具/" + key + ".png") }).ToArray();
                if (game.ItemArt.Length != 7 || game.ItemArt.Any(x => !x.Texture)) throw new Exception("道具图片引用不完整");
                var acquire = game.Ui.View("Acquire");
                var icon = acquire.Get<Text>("Icon");
                var acquireArt = Art(acquire, icon.transform, "ItemArt");
                acquireArt.rectTransform.anchorMin = acquireArt.rectTransform.anchorMax = acquireArt.rectTransform.pivot = new Vector2(.5f, .5f);
                acquireArt.rectTransform.anchoredPosition = Vector2.zero;
                acquireArt.rectTransform.sizeDelta = new Vector2(170, 170);
                var letter = game.Ui.View("Letter");
                var body = letter.Get<Text>("Body");
                var letterArt = Art(letter, body.transform.parent, "ItemArt");
                Place(letterArt.rectTransform, 385, 195, 390, 390);
                Place(body.rectTransform, 800, 185, 410, 460);
                body.fontSize = 22;
                body.resizeTextForBestFit = true;
                body.resizeTextMinSize = 16;
                body.resizeTextMaxSize = 22;
                foreach (var view in game.Ui.GetComponentsInChildren<CastleUiView>(true)) view.ValidateBindings();
                EditorUtility.SetDirty(game);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                V24SceneSetup.ExportBindings();
                Directory.CreateDirectory("Builds/V24");
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { CastleFlowSceneBuilder.ScenePath }, locationPathName = "Builds/V24/CastleV24.exe", target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
                if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("配置更新构建失败");
                File.WriteAllText("Logs/config-refresh-result.txt", "PASS config validation; seven item textures; scene bindings; Windows build\n");
                Debug.Log("CONFIG_REFRESH_OK");
            }
            catch (Exception exception)
            {
                File.WriteAllText("Logs/config-refresh-result.txt", exception.ToString());
                Debug.LogException(exception);
                throw;
            }
        }

        private static RawImage Art(CastleUiView view, Transform parent, string id)
        {
            if (view.TryGet<RawImage>(id, out var existing)) return existing;
            var go = new GameObject(id, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<RawImage>();
            image.raycastTarget = false;
            view.Controls.Add(new CastleUiBinding { Id = id, Target = image });
            return image;
        }

        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}

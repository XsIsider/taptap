using System;
using System.IO;
using System.Linq;
using Castle.V24;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Castle.Editor
{
    /// <summary>定向替换字体，保留场景布局、图片按钮和已有绑定。</summary>
    public static class V24FontUpdate
    {
        [MenuItem("Tools/Castle/Apply Fonts And Build")]
        public static void ApplyAndBuild()
        {
            V24FormalUpdate.Validate();
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.isDirty) throw new Exception("请先保存当前场景");
            if (scene.path != CastleFlowSceneBuilder.ScenePath) scene = EditorSceneManager.OpenScene(CastleFlowSceneBuilder.ScenePath);
            var game = UnityEngine.Object.FindObjectOfType<CastleV24Controller>(true);
            game.WorkbenchFont = FindFont("点点像素体-方形.ttf");
            game.ReadingFont = FindFont("I.Ming-7.00.ttf");
            game.ButtonFont = FindFont("万事全兴体.ttf");
            var reading = CreateTmp(game.ReadingFont, "V24Reading");
            var pixel = CreateTmp(game.WorkbenchFont, "V24Workbench");
            Apply(game.Ui.gameObject, game.ButtonFont, reading);
            foreach (var view in game.Ui.Pages.Concat(game.Ui.Modals))
            {
                bool workbench = new[] { "Recordings", "Anchors", "Space" }.Contains(view.Id);
                bool book = new[] { "Journal", "Acquire", "Letter", "Invitation", "Path" }.Contains(view.Id);
                Apply(view.gameObject, workbench ? game.WorkbenchFont : book ? game.ReadingFont : game.ButtonFont, workbench ? pixel : reading);
            }
            Apply(game.Ui.View("Explore").Get<RectTransform>("DialoguePanel").gameObject, game.ReadingFont, reading);
            foreach (var id in new[] { "EndingText", "EndingStatus" }) game.Ui.View("Timeline").Get<Text>(id).font = game.ReadingFont;
            var paths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Castle/Prefabs/V24" }).Select(AssetDatabase.GUIDToAssetPath)
                .Concat(new[] { AssetDatabase.GetAssetPath(game.Ui.CardPrefab), AssetDatabase.GetAssetPath(game.Ui.TrackPrefab), AssetDatabase.GetAssetPath(game.Ui.LinePrefab) }).Distinct();
            foreach (var path in paths)
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    string name = Path.GetFileNameWithoutExtension(path);
                    bool workbench = name.StartsWith("Recording") || name == "AnchorCard" || name == "SpaceCard" || name == game.Ui.TrackPrefab.name;
                    Apply(root, workbench ? game.WorkbenchFont : game.ReadingFont, workbench ? pixel : reading);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            EditorUtility.SetDirty(game);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            V24SceneSetup.ExportBindings();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { scene.path }, locationPathName = "Builds/V24/CastleV24.exe", target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("字体更新构建失败");
            Debug.Log("FONT_UPDATE_OK");
        }

        private static Font FindFont(string name)
        {
            string path = Directory.GetFiles("Assets/Castle/Art/V24/Font", name, SearchOption.AllDirectories).Single();
            return AssetDatabase.LoadAssetAtPath<Font>(path.Replace('\\', '/')) ?? throw new Exception("字体未导入：" + name);
        }

        private static TMP_FontAsset CreateTmp(Font font, string name)
        {
            string path = "Assets/Castle/Resources/Castle/" + name + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (asset) return asset;
            asset = TMP_FontAsset.CreateFontAsset(font, 36, 5, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 2048, 2048);
            asset.name = name;
            asset.isMultiAtlasTexturesEnabled = true;
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            foreach (var texture in asset.atlasTextures) AssetDatabase.AddObjectToAsset(texture, asset);
            return asset;
        }

        private static void Apply(GameObject root, Font font, TMP_FontAsset tmp)
        {
            foreach (var text in root.GetComponentsInChildren<Text>(true)) { text.font = font; EditorUtility.SetDirty(text); }
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true)) { text.font = tmp; EditorUtility.SetDirty(text); }
        }
    }
}

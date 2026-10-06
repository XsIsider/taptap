using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Castle.Editor
{
    public static class CastleProjectTools
    {
        public const string ScenePath = "Assets/Castle/Scenes/CastlePrototype.unity";
        [MenuItem("Tools/Castle/Open Prototype Scene")]
        public static void OpenScene() { if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath); }
        public static void Setup()
        {
            if (!File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset"))
                AssetDatabase.ImportPackage(Path.GetFullPath("Library/PackageCache/com.unity.textmeshpro@3.0.7/Package Resources/TMP Essential Resources.unitypackage"), false);
            const string fontPath = "Assets/Castle/Resources/Castle/ChineseTMP.asset";
            if (!File.Exists(fontPath))
            {
                var font = TMPro.TMP_FontAsset.CreateFontAsset(Resources.Load<Font>("Castle/Chinese"));
                font.name = "ChineseTMP"; font.atlasPopulationMode = TMPro.AtlasPopulationMode.Dynamic; font.isMultiAtlasTexturesEnabled = true;
                AssetDatabase.CreateAsset(font, fontPath);
                AssetDatabase.AddObjectToAsset(font.material, font);
                foreach (var texture in font.atlasTextures) AssetDatabase.AddObjectToAsset(texture, font);
                EditorUtility.SetDirty(font); AssetDatabase.SaveAssets();
            }
            ContentImporter.Setup(); ContentImporter.ImportExample();
            Directory.CreateDirectory("Assets/Castle/Resources/Castle/Prefabs");
            foreach (var name in new[] { "Title", "Room", "Dialogue", "Map", "Records", "Tapes", "Playback", "Journal", "Puzzle", "Puzzles" })
            {
                string path = "Assets/Castle/Resources/Castle/Prefabs/" + name + ".prefab";
                if (File.Exists(path)) continue;
                var obj = new GameObject(name, typeof(RectTransform), typeof(Castle.V2.PrototypePanel)); PrefabUtility.SaveAsPrefabAsset(obj, path); UnityEngine.Object.DestroyImmediate(obj);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) }; AssetDatabase.SaveAssets();
        }
        [MenuItem("Tools/Castle/Validate Planning V1")]
        public static void Validate() { Setup(); PrototypeValidation.Run(); }
        [MenuItem("Tools/Castle/Build Windows Prototype")]
        public static void Build()
        {
            Validate(); Directory.CreateDirectory("Builds/CastleV2");
            var result = BuildPipeline.BuildPlayer(new[] { ScenePath }, "Builds/CastleV2/Castle.exe", BuildTarget.StandaloneWindows64, BuildOptions.Development);
            if (result.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("Build failed: " + result.summary.result);
        }
    }
    public sealed class CastleTextureImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Castle/Resources/Castle/")) return;
            var importer = (TextureImporter)assetImporter; importer.textureType = TextureImporterType.Default; importer.mipmapEnabled = false; importer.maxTextureSize = 2048; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.npotScale = TextureImporterNPOTScale.None;
        }
    }
}

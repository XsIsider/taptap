using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Castle.V2;
using UnityEditor;
using UnityEngine;

namespace Castle.Editor
{
    public static class ContentImporter
    {
        public const string DatabasePath = "Assets/Castle/Resources/Castle/ContentV2.asset";
        public const string SettingsPath = "Assets/Castle/Resources/Castle/GameSettings.asset";
        [MenuItem("Tools/Castle/Content/Import All Six Tables")]
        public static void ImportExample() { Import("Assets/Castle/Content/CSV", false); }
        [MenuItem("Tools/Castle/Content/Reimport Plot Only")]
        public static void ImportPlot() { Import("Assets/Castle/Content/CSV", true); }
        public static void Import(string folder, bool plotOnly)
        {
            var previous = AssetDatabase.LoadAssetAtPath<ContentDatabase>(DatabasePath);
            var candidate = ScriptableObject.CreateInstance<ContentDatabase>();
            candidate.Version = "example-v1";
            if (plotOnly && previous != null) candidate.Rows.AddRange(previous.Rows.Where(r => r.Table != "Plot"));
            var errors = new List<string>();
            foreach (var schema in CsvContent.Schema.Where(s => !plotOnly || s.Key == "Plot"))
            {
                string path = Path.Combine(folder, schema.Key + ".csv");
                try { candidate.Rows.AddRange(CsvContent.Parse(schema.Key, File.ReadAllText(path))); }
                catch (Exception ex) { errors.Add(schema.Key + ":1 " + ex.Message); }
            }
            var settings = AssetDatabase.LoadAssetAtPath<GameSettings>(SettingsPath);
            if (errors.Count == 0) errors.AddRange(ContentValidator.Validate(candidate, settings));
            Directory.CreateDirectory("Logs"); File.WriteAllLines("Logs/castle-content-import.txt", errors.Count == 0 ? new[] { "PASS " + candidate.Rows.Count + " rows" } : errors.ToArray());
            if (errors.Count > 0) { UnityEngine.Object.DestroyImmediate(candidate); throw new InvalidOperationException(string.Join("\n", errors)); }
            // 所有校验完成后才修改正式资产；失败完全保留旧数据库。
            if (previous == null) AssetDatabase.CreateAsset(candidate, DatabasePath);
            else { previous.Version = candidate.Version; previous.Rows = candidate.Rows; EditorUtility.SetDirty(previous); UnityEngine.Object.DestroyImmediate(candidate); }
            AssetDatabase.SaveAssets();
        }
        static T Asset<T>(string name) where T : ScriptableObject
        {
            string path = "Assets/Castle/Resources/Castle/" + name + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) { asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); }
            return asset;
        }
        [MenuItem("Tools/Castle/Content/Create Missing Example Config")]
        public static void Setup()
        {
            if (AssetDatabase.LoadAssetAtPath<GameSettings>(SettingsPath) != null) return;
            var settings = Asset<GameSettings>("GameSettings");
            settings.Devices = Asset<DeviceConfig>("Devices");
            settings.Devices.Devices = new[]
            {
                new DeviceDefinition { Id = "D_A", Label = "频道 A", Channel = "A", Room = "R_HALL", Proof = new[] { "C_PROOF" } },
                new DeviceDefinition { Id = "D_B", Label = "频道 B", Channel = "B", Room = "R_STUDY", Offset = -180 },
                new DeviceDefinition { Id = "D_C", Label = "频道 C", Channel = "C", Room = "R_HALL" },
                new DeviceDefinition { Id = "D_PERSONAL", Label = "女主录音笔", Channel = "个人", Room = "R_HOME", Personal = true }
            };
            settings.Characters = Asset<CharacterConfig>("Characters");
            settings.Characters.Characters = new[] { new CharacterDefinition { Id = "CHAR_VISITOR", Name = "访客" } };
            settings.Map = Asset<MapConfig>("Map");
            settings.Map.Nodes = new[]
            {
                new MapNode { Id = "N_HALL", Room = "R_HALL", Floor = 1, Position = new Vector2(220, 240) },
                new MapNode { Id = "N_CONTROL", Room = "R_CONTROL", Floor = 1, Position = new Vector2(620, 230) },
                new MapNode { Id = "N_STUDY", Room = "R_STUDY", Floor = 2, Position = new Vector2(250, 220) },
                new MapNode { Id = "N_HOME", Room = "R_HOME", Floor = 2, Position = new Vector2(620, 230) }
            };
            settings.Map.Floors = new[] { new FloorBinding { Floor = 1, Background = Resources.Load<Texture2D>("Castle/Floor1") }, new FloorBinding { Floor = 2, Background = Resources.Load<Texture2D>("Castle/Floor2") } };
            settings.Map.Rooms = new[] { new RoomBinding { Room = "R_HALL", Background = Resources.Load<Texture2D>("Castle/Lounge") }, new RoomBinding { Room = "R_CONTROL", Background = Resources.Load<Texture2D>("Castle/ControlRoom") }, new RoomBinding { Room = "R_STUDY", Background = Resources.Load<Texture2D>("Castle/Castle") }, new RoomBinding { Room = "R_HOME", Background = Resources.Load<Texture2D>("Castle/EmptyRoom") } };
            settings.Hotspots = Asset<HotspotBinding>("Hotspots");
            settings.Hotspots.Hotspots = new[]
            {
                new HotspotDefinition { Id = "HS_VISITOR", Room = "R_STUDY", Mode = "event", Label = "访客 · 交谈" },
                new HotspotDefinition { Id = "HS_BOOK", Room = "R_STUDY", Mode = "event", Label = "维护簿 · 查看" },
                new HotspotDefinition { Id = "HS_DESK", Room = "R_HALL", Mode = "event", Label = "登记台 · 查看" },
                new HotspotDefinition { Id = "HS_PLAYER", Room = "R_HOME", Mode = "tape_player", Label = "放音机" },
                new HotspotDefinition { Id = "HS_REST", Room = "R_HOME", Mode = "rest", Label = "休息至次日" },
                new HotspotDefinition { Id = "HS_CONTROL", Room = "R_CONTROL", Mode = "control", Label = "中央档案" }
            };
            settings.TitleScene = Resources.Load<Texture2D>("Castle/Title"); settings.ExteriorScene = Resources.Load<Texture2D>("Castle/Outside");
            foreach (var asset in new ScriptableObject[] { settings, settings.Devices, settings.Characters, settings.Map, settings.Hotspots }) EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }
    }
}

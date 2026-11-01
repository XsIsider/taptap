using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Castle.V24
{
    [Serializable]
    public sealed class V24TestCheckpoint
    {
        public string Id, Label, Description, Resource, Page;
        public int Day, Room, ActiveEvent, Line;
        public long Now;
    }

    [Serializable]
    public sealed class V24TestCheckpointCatalog
    {
        public int Version;
        public string ContentHash;
        public List<V24TestCheckpoint> Checkpoints = new List<V24TestCheckpoint>();
    }

    /// <summary>开发测试档：校验配置版本，保留独立备份后再替换本机自动档。</summary>
    public static class V24TestCheckpoints
    {
        public const string ResourceRoot = "Castle/V24/TestCheckpoints/";

        public static string Fingerprint(string text)
        {
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "");
        }

        public static V24TestCheckpointCatalog LoadCatalog()
        {
            var asset = Resources.Load<TextAsset>(ResourceRoot + "Catalog");
            if (!asset) throw new InvalidDataException("内置测试存档目录缺失。");
            var catalog = JsonUtility.FromJson<V24TestCheckpointCatalog>(asset.text);
            var content = Resources.Load<TextAsset>("Castle/V24/Content");
            if (catalog == null || catalog.Version != 26 || catalog.Checkpoints == null || catalog.Checkpoints.Count == 0
                || catalog.Checkpoints.Select(x => x.Id).Distinct().Count() != catalog.Checkpoints.Count)
                throw new InvalidDataException("内置测试存档目录无效。");
            if (!content || !string.Equals(catalog.ContentHash, Fingerprint(content.text), StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("剧情配置已经变化，请重新生成并验证测试存档。");
            return catalog;
        }

        public static V24State Read(V24Data data, V24TestCheckpoint checkpoint)
        {
            if (checkpoint == null || checkpoint.Resource != ResourceRoot + checkpoint.Id)
                throw new InvalidDataException("测试存档路径无效。");
            var asset = Resources.Load<TextAsset>(checkpoint.Resource);
            if (!asset) throw new InvalidDataException("测试存档缺失：" + checkpoint.Label);
            var state = JsonUtility.FromJson<V24State>(asset.text);
            Validate(data, state, checkpoint);
            return state;
        }

        /// <summary>核对节点元数据和运行中会直接引用的 ID；完整可达性由生成工具验证。</summary>
        public static void Validate(V24Data data, V24State state, V24TestCheckpoint checkpoint)
        {
            if (state == null || state.Version != 26 || !state.Started || state.Day != checkpoint.Day
                || state.Room != checkpoint.Room || state.Now != checkpoint.Now || state.Page != checkpoint.Page
                || state.ActiveEvent != checkpoint.ActiveEvent || state.Line != checkpoint.Line)
                throw new InvalidDataException("测试存档节点不一致：" + checkpoint.Label);
            if (data.Room.All(x => x.Id != state.Room) || state.Entries.Any(x => data.EntryById(x) == null)
                || state.CompletedEvents.Any(x => data.EventById(x) == null)
                || state.Puzzles.Any(x => data.PuzzleById(x) == null)
                || state.Recordings.Any(x => data.RecordById(x.Id) == null)
                || state.Tapes.Any(x => data.EntryById(x.Entry) == null || x.Record != 0 && data.RecordById(x.Record) == null)
                || state.Transcript.Any(x => x.Plot != 0 && data.Plot.All(p => p.Id != x.Plot)))
                throw new InvalidDataException("测试存档包含失效的配置 ID。");
            if (state.ActiveEvent != 0)
            {
                var ev = data.EventById(state.ActiveEvent);
                if (ev == null || state.Line < 0 || state.Line >= data.Lines(ev.Plot).Length
                    || state.CompletedEvents.Contains(ev.Id))
                    throw new InvalidDataException("测试存档的当前对白无效。");
            }
            if (state.RewardPopups.Count != 0 || state.ActionPending != 0 || state.DeferredEvent != 0
                || state.PlaybackGate != 0 || state.TutorialEvent != 0 || state.QueuedItem != 0)
                throw new InvalidDataException("测试存档仍有未处理的结算。");
        }

        /// <summary>先保存原档和原 .bak 的独立副本；验证写入成功后才由调用者切换内存状态。</summary>
        public static string Install(V24State state, string destination = null)
        {
            destination = destination ?? V24Save.Path;
            string directory = System.IO.Path.GetDirectoryName(destination);
            Directory.CreateDirectory(directory);
            string backup = System.IO.Path.Combine(directory, "TestCheckpointBackups",
                DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            bool existed = File.Exists(destination);
            bool backupExisted = File.Exists(destination + ".bak");
            if (existed || backupExisted)
            {
                Directory.CreateDirectory(backup);
                if (existed) File.Copy(destination, System.IO.Path.Combine(backup, "original.json"));
                if (backupExisted) File.Copy(destination + ".bak", System.IO.Path.Combine(backup, "original.json.bak"));
            }
            try
            {
                V24Save.Write(state, destination);
                if (JsonUtility.ToJson(V24Save.Read(destination)) != JsonUtility.ToJson(state))
                    throw new IOException("测试存档写入后的回读不一致。");
            }
            catch
            {
                if (existed) File.Copy(System.IO.Path.Combine(backup, "original.json"), destination, true);
                else if (File.Exists(destination)) File.Delete(destination);
                if (backupExisted) File.Copy(System.IO.Path.Combine(backup, "original.json.bak"), destination + ".bak", true);
                else if (File.Exists(destination + ".bak")) File.Delete(destination + ".bak");
                throw;
            }
            return existed || backupExisted ? backup : "";
        }
    }
}

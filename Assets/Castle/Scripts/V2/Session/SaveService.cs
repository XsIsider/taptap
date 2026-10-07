using System;
using System.IO;
using UnityEngine;

namespace Castle.V2
{
    public sealed class SaveService
    {
        readonly string _path;
        public string LastError { get; private set; }
        bool _protectExisting;
        public SaveService(string path) { _path = path; }
        public static string DefaultPath => Path.Combine(Application.persistentDataPath, "castle-save-v2.json");
        public bool Write(SessionSave state)
        {
            if (_path == null) return true;
            if (_protectExisting) { LastError = "现有 v2 存档不兼容，已保护原文件；请先备份后移走文件再新建调查。"; return false; }
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path));
                string temp = _path + ".tmp";
                using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write))
                using (var writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false))) { writer.Write(JsonUtility.ToJson(state, true)); writer.Flush(); stream.Flush(true); }
                if (File.Exists(_path)) File.Replace(temp, _path, _path + ".bak"); else File.Move(temp, _path);
                LastError = null; return true;
            }
            catch (Exception ex) { LastError = "存档未写入：" + ex.Message; return false; }
        }
        public SessionSave Load(string version)
        {
            if (_path == null) return null;
            foreach (string path in new[] { _path, _path + ".bak" })
            {
                if (!File.Exists(path)) continue;
                try
                {
                    var state = JsonUtility.FromJson<SessionSave>(File.ReadAllText(path));
                    if (state == null || state.Version != 2 || state.ContentVersion != version || string.IsNullOrEmpty(state.WorldTime)) throw new FormatException("内容版本不兼容或存档不完整");
                    ContentFormat.Date(state.WorldTime);
                    if (path.EndsWith(".bak")) LastError = "已从备份恢复存档。";
                    return state;
                }
                catch (Exception ex) { LastError = "无法读取 " + Path.GetFileName(path) + "：" + ex.Message; }
            }
            _protectExisting = Exists;
            return null;
        }
        public bool Exists => _path != null && (File.Exists(_path) || File.Exists(_path + ".bak"));
    }

}

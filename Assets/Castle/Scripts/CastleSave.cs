using System;
using System.IO;
using UnityEngine;

namespace Castle
{
    public static class CastleSave
    {
        public static string PathName { get { return Path.Combine(Application.persistentDataPath, "castle-save-v1.json"); } }
        public static string LastError { get; private set; }

        public static CastleState Load()
        {
            LastError = null;
            foreach (var path in new[] { PathName, PathName + ".bak" })
            {
                if (!File.Exists(path)) continue;
                try
                {
                    var s = JsonUtility.FromJson<CastleState>(File.ReadAllText(path));
                    if (s == null || s.version != 1 || s.route == null || s.route.Length != 3 || s.discovered == null || s.excerpts == null)
                        throw new InvalidDataException("不兼容的存档结构");
                    s.dialogue = Mathf.Clamp(s.dialogue, 0, 5);
                    s.truthIndex = Mathf.Clamp(s.truthIndex, 0, 2);
                    s.readingSize = Mathf.Clamp(s.readingSize, 20, 28);
                    s.volume = Mathf.Clamp01(s.volume);
                    return s;
                }
                catch (Exception e) { LastError = "存档读取失败：" + e.Message; }
            }
            return new CastleState();
        }

        public static bool Write(CastleState state)
        {
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                File.WriteAllText(PathName + ".tmp", JsonUtility.ToJson(state, true));
                if (File.Exists(PathName)) File.Replace(PathName + ".tmp", PathName, PathName + ".bak");
                else File.Move(PathName + ".tmp", PathName);
                LastError = null;
                return true;
            }
            catch (Exception e) { LastError = "无法保存进度：" + e.Message; Debug.LogWarning(LastError); return false; }
        }
    }
}

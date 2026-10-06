using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Castle.V2
{
    [Serializable] public sealed class ItemCount { public string Id; public int Count; }
    [Serializable] public sealed class TapeInstance { public string Id, EntryId, RecordId; }
    [Serializable] public sealed class PersonalRecord { public string Id, Started, Room, Imported; }
    [Serializable] public sealed class PuzzleDraft { public string Id, Reference; public string[] Values = Array.Empty<string>(); public float Shift; }
    [Serializable] public sealed class DeviceKnowledge { public string Id, Room; public bool Calibrated; public float Offset; }
    [Serializable] public sealed class EventProgress { public string Id, Started, Room; public int Line; public float Cursor; public bool LastLineDone, Replay; }
    [Serializable] public sealed class RecordProgress { public string Id; public float Cursor; }
    [Serializable] public sealed class SessionSave
    {
        public int Version = 2;
        public string ContentVersion, WorldTime, Room, PendingPuzzle, EndingStage = "none";
        public bool Started, ControlVisit;
        public EventProgress Active;
        public List<string> Goals = new List<string>(), Entries = new List<string>(), Solved = new List<string>(), Links = new List<string>(), CompletedEvents = new List<string>(), BackupRoutes = new List<string>(), MarkedLines = new List<string>(), ReadLines = new List<string>(), Played = new List<string>();
        public List<ItemCount> Items = new List<ItemCount>();
        public List<TapeInstance> Tapes = new List<TapeInstance>();
        public List<PersonalRecord> Personal = new List<PersonalRecord>();
        public List<PuzzleDraft> Drafts = new List<PuzzleDraft>();
        public List<DeviceKnowledge> Devices = new List<DeviceKnowledge>();
        public List<RecordProgress> Playback = new List<RecordProgress>();
    }

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

    public sealed class SessionService
    {
        public SessionSave State { get; private set; }
        public ContentDatabase Content { get; }
        public GameSettings Settings { get; }
        public SaveService Storage { get; }
        public InventoryService Inventory { get; }
        public EventService Events { get; }
        public RecordingService Recordings { get; }
        public PuzzleService Puzzles { get; }
        public DateTime Now => ContentFormat.Date(State.WorldTime);
        public string Notice { get; set; }
        public SessionService(ContentDatabase content, GameSettings settings, SaveService storage, SessionSave state = null)
        {
            Content = content; Settings = settings; Storage = storage;
            State = state ?? storage.Load(content.Version) ?? new SessionSave { ContentVersion = content.Version, WorldTime = settings.InitialTime, Room = settings.ArrivalRoom };
            if (State.Active != null && string.IsNullOrEmpty(State.Active.Id)) State.Active = null;
            Inventory = new InventoryService(this); Events = new EventService(this); Recordings = new RecordingService(this); Puzzles = new PuzzleService(this);
        }
        public void Persist() { if (!Storage.Write(State)) Notice = Storage.LastError; }
        public void Start()
        {
            if (State.Started) return;
            State.Started = true;
            foreach (var grant in Settings.InitialItems) Inventory.Grant(grant);
            Events.Begin(Settings.OpeningEvent, true); Persist();
        }
        public bool Has(string id) => State.Goals.Contains(id) || State.Solved.Contains(id) || State.Entries.Contains(id) || Inventory.Count(id) > 0;
        public bool Meets(IEnumerable<string> needs) => needs.All(Has);
        public DeviceKnowledge Knowledge(string id)
        {
            var knowledge = State.Devices.FirstOrDefault(d => d.Id == id);
            if (knowledge == null) { knowledge = new DeviceKnowledge { Id = id }; State.Devices.Add(knowledge); }
            return knowledge;
        }
        public string RoomStatus(string id)
        {
            var room = Content.Require(id);
            if (!Meets(room.List("need"))) return "尚未解锁";
            return EventService.InWindow(room, Now) ? "可进入" : "时段关闭";
        }
        public bool Travel(string id)
        {
            if (State.Active != null || RoomStatus(id) != "可进入") return false;
            if (State.Room == id) return true;
            if (State.ControlVisit) { State.ControlVisit = false; Advance(Settings.ControlMinutes); }
            // 中控室离开消耗时间后，重新检查目的地，避免进入刚关闭的房间。
            if (RoomStatus(id) != "可进入") { EnsureSafeRoom(); Persist(); return false; }
            State.Room = id;
            if (id == Settings.ControlRoom) { State.ControlVisit = true; Recordings.Import(); }
            Persist(); Events.Schedule(); return true;
        }
        public void Advance(int minutes)
        {
            var before = Now; State.WorldTime = ContentFormat.Stamp(before.AddMinutes(minutes)); Events.CrossWindows(before, Now);
        }
        public void EnsureSafeRoom()
        {
            if (State.Active != null || RoomStatus(State.Room) == "可进入") return;
            if (State.ControlVisit) { State.ControlVisit = false; Advance(Settings.ControlMinutes); }
            State.Room = Settings.HomeRoom; Notice = "当前场所已关闭，已返回房间。";
        }
        public bool Rest()
        {
            if (State.Active != null || State.Room != Settings.HomeRoom) return false;
            var after = Now.Date.AddDays(1).AddMinutes(ContentFormat.Minute(Settings.WakeTime));
            var before = Now; State.WorldTime = ContentFormat.Stamp(after); Events.CrossWindows(before, after); Persist(); Events.Schedule(); return true;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace Castle.V2
{
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
        public bool ResetProgress()
        {
            var fresh = new SessionSave { ContentVersion = Content.Version, WorldTime = Settings.InitialTime, Room = Settings.ArrivalRoom };
            if (!Storage.Reset(fresh)) { Notice = Storage.LastError; return false; }
            State = fresh;
            Notice = "调查进度已重置；原 v2 存档已备份（如存在）。";
            return true;
        }
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

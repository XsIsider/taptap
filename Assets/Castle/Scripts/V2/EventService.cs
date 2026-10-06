using System;
using System.Linq;
namespace Castle.V2
{
    public sealed class EventService
    {
        readonly SessionService _session;
        public EventService(SessionService session) { _session = session; }
        public static bool InWindow(ContentRow row, DateTime time) => time.TimeOfDay.TotalMinutes >= ContentFormat.Minute(row.Get("start")) && time.TimeOfDay.TotalMinutes < ContentFormat.Minute(row.Get("end"));
        bool RouteEnabled(ContentRow row)
        {
            if (_session.State.BackupRoutes.Contains(row.Get("plan_b"))) return false;
            if (_session.Content.Table("Event").Any(e => e.Get("plan_b") == row.Id)) return _session.State.BackupRoutes.Contains(row.Id);
            return true;
        }
        public bool Available(ContentRow row)
        {
            if (_session.State.Goals.Contains(row.Get("goal")) || !_session.Meets(row.List("need")) || !RouteEnabled(row)) return false;
            if (row.List("use").Any(v => { var g = ContentFormat.Grant(v); return _session.Inventory.Count(g.Key) < g.Value; })) return false;
            if (row.Get("trigger") == "interact" || row.Get("trigger") == "auto") return row.Get("room") == _session.State.Room && InWindow(row, _session.Now) && _session.RoomStatus(row.Get("room")) == "可进入";
            return true;
        }
        public ContentRow[] Choices(string target) => _session.Content.Table("Event").Where(e => e.Get("trigger") == "interact" && e.Get("target") == target && Available(e)).ToArray();
        public bool Begin(string id, bool flow = false, bool replay = false)
        {
            var state = _session.State; var row = _session.Content.Require(id);
            if (state.Active != null) return false;
            if (replay) { if (!state.CompletedEvents.Contains(id)) return false; }
            else if (!Available(row) || row.Get("trigger") == "reward" && state.PendingPuzzle == null || row.Get("trigger") == "flow" && !flow) return false;
            state.Active = new EventProgress { Id = id, Started = state.WorldTime, Room = state.Room, Replay = replay };
            if (row.Get("action") == "ending" && !replay) state.EndingStage = "playing";
            ReadCurrent(); _session.Persist(); return true;
        }
        public void ReadCurrent()
        {
            var active = _session.State.Active; if (active == null) return;
            var lines = _session.Content.Plot(_session.Content.Require(active.Id).Get("plot"));
            if (lines.Length == 0) { active.LastLineDone = true; return; }
            active.Line = Math.Min(active.Line, lines.Length - 1);
            if (!_session.State.ReadLines.Contains(lines[active.Line].Id)) _session.State.ReadLines.Add(lines[active.Line].Id);
        }
        public float Hold(ContentRow line) => Math.Max(_session.Settings.DialogueHold, line.Get("text").Length * _session.Settings.SecondsPerCharacter);
        public void Tick(float seconds)
        {
            var active = _session.State.Active; if (active == null || active.LastLineDone) return;
            var row = _session.Content.Require(active.Id); var lines = _session.Content.Plot(row.Get("plot"));
            if (lines.Length == 0) { CompleteLastLine(); return; }
            active.Cursor += seconds;
            bool ending = row.Get("action") == "ending";
            if (active.Line < lines.Length - 1)
            {
                float threshold = ending ? lines[active.Line + 1].Number("at") : Hold(lines[active.Line]);
                if (active.Cursor >= threshold) { active.Line++; if (!ending) active.Cursor = 0; ReadCurrent(); _session.Persist(); }
            }
            else if (active.Cursor >= (ending ? lines.Last().Number("at") + _session.Settings.EndingHold : Hold(lines.Last()))) CompleteLastLine();
        }
        public void CompleteLastLine()
        {
            var active = _session.State.Active; if (active == null) return;
            active.LastLineDone = true;
            var row = _session.Content.Require(active.Id);
            if (!active.Replay && row.Get("record") != "") _session.Recordings.Capture(row.Get("record"), active.Started, active.Room);
            _session.Persist();
        }
        public bool Continue()
        {
            var state = _session.State; var active = state.Active;
            if (active == null || !active.LastLineDone) return false;
            var row = _session.Content.Require(active.Id);
            if (row.Id == _session.Settings.TapeTutorialEvent && !active.Replay && !state.Played.Contains(_session.Settings.TutorialRecord)) { _session.Notice = "请在房间放音机播放指定剧情磁带至结束。"; return false; }
            if (!active.Replay && !state.Goals.Contains(row.Get("goal")))
            {
                foreach (var use in row.List("use")) _session.Inventory.Consume(use);
                foreach (var grant in row.List("reward")) _session.Inventory.Grant(grant);
                state.Goals.Add(row.Get("goal")); state.CompletedEvents.Add(row.Id);
                _session.Advance((int)row.Number("time"));
                if (!string.IsNullOrEmpty(state.PendingPuzzle)) { _session.Puzzles.Commit(state.PendingPuzzle); state.PendingPuzzle = null; }
                if (row.Get("action") == "ending") state.EndingStage = "complete";
            }
            bool replay = active.Replay; state.Active = null; _session.EnsureSafeRoom(); _session.Persist();
            if (!replay) Schedule(); return true;
        }
        public void SkipEnding()
        {
            if (_session.State.Active == null || _session.Content.Require(_session.State.Active.Id).Get("action") != "ending") return;
            CompleteLastLine(); Continue();
        }
        public void Schedule()
        {
            if (_session.State.Active != null || !_session.State.Started) return;
            if (!string.IsNullOrEmpty(_session.State.PendingPuzzle)) { Begin(_session.Content.Require(_session.State.PendingPuzzle).Get("success"), true); return; }
            var arrival = _session.Content.Require(_session.Settings.ArrivalEvent);
            if (_session.Has(_session.Content.Require(_session.Settings.OpeningEvent).Get("goal")) && Available(arrival)) { Begin(arrival.Id, true); return; }
            var tutorial = _session.Content.Require(_session.Settings.TapeTutorialEvent);
            if (_session.State.Room == _session.Settings.HomeRoom && Available(tutorial)) { Begin(tutorial.Id, true); return; }
            var next = _session.Content.Table("Event").FirstOrDefault(e => e.Get("trigger") == "auto" && Available(e));
            if (next != null) Begin(next.Id);
        }
        public void CrossWindows(DateTime before, DateTime after)
        {
            foreach (var row in _session.Content.Table("Event").Where(e => e.Get("plan_b") != "" && !_session.Has(e.Get("goal")) && _session.Meets(e.List("need"))))
            {
                var room = _session.Content.Require(row.Get("room"));
                if (!_session.Meets(room.List("need"))) continue;
                int end = Math.Min(ContentFormat.Minute(row.Get("end")), ContentFormat.Minute(room.Get("end")));
                var boundary = before.Date.AddMinutes(end);
                if (boundary <= before) boundary = boundary.AddDays(1);
                if (boundary <= after && !_session.State.BackupRoutes.Contains(row.Get("plan_b"))) _session.State.BackupRoutes.Add(row.Get("plan_b"));
            }
        }
    }
}

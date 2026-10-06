using System;
using System.Linq;
namespace Castle.V2
{
    public sealed class RecordingService
    {
        readonly SessionService _session;
        public RecordingService(SessionService session) { _session = session; }
        public void Capture(string id, string started, string room)
        {
            if (_session.State.Personal.Any(p => p.Id == id)) return;
            _session.State.Personal.Add(new PersonalRecord { Id = id, Started = started, Room = room }); _session.Notice = "现场录音已收录；进入中控室后导入。";
        }
        public void Import()
        {
            int count = 0;
            foreach (var file in _session.State.Personal.Where(p => string.IsNullOrEmpty(p.Imported))) { file.Imported = _session.State.WorldTime; count++; }
            if (count > 0) _session.Notice = count + " 份个人录音已导入。原录制时间保持不变。";
        }
        public bool IsPersonal(ContentRow record) => _session.Settings.Devices.Devices.Any(d => d.Id == record.Get("device") && d.Personal);
        public bool CanAccess(string id, string tapeId = null)
        {
            var record = _session.Content.Require(id);
            if (tapeId != null) return _session.State.Room == _session.Settings.HomeRoom && _session.State.Tapes.Any(t => t.Id == tapeId && t.RecordId == id);
            if (_session.State.Room != _session.Settings.ControlRoom || !_session.State.ControlVisit || record.Get("need") == "TAPE_ONLY") return false;
            if (IsPersonal(record)) return _session.State.Personal.Any(p => p.Id == id && !string.IsNullOrEmpty(p.Imported)) && _session.Meets(record.List("need"));
            if (!_session.Settings.KeepRecords && ContentFormat.Date(record.Get("start")).Date < _session.Now.Date) return false;
            return _session.Meets(record.List("need"));
        }
        public DateTime Started(string id)
        {
            var personal = _session.State.Personal.FirstOrDefault(p => p.Id == id);
            string start = personal?.Started ?? _session.Content.Require(id).Get("start");
            return string.IsNullOrEmpty(start) ? ContentFormat.Date(_session.Settings.InitialTime) : ContentFormat.Date(start);
        }
        public DateTime TimeAt(string id, float at, bool corrected)
        {
            var record = _session.Content.Require(id); var knowledge = _session.Knowledge(record.Get("device"));
            return Started(id).AddSeconds(at - (corrected && knowledge.Calibrated ? knowledge.Offset : 0));
        }
        public ContentRow RecordForLine(string lineId) => _session.Content.Table("Record").FirstOrDefault(r => r.Get("plot") == _session.Content.Require(lineId).Get("group"));
        public string Label(string id)
        {
            var record = _session.Content.Require(id); var device = _session.Settings.Devices.Devices.FirstOrDefault(d => d.Id == record.Get("device"));
            string room = _session.State.Devices.FirstOrDefault(d => d.Id == device?.Id)?.Room;
            if (IsPersonal(record)) room = _session.State.Personal.FirstOrDefault(p => p.Id == id)?.Room;
            return record.Name + " · " + (device?.Label ?? "磁带") + (string.IsNullOrEmpty(room) ? "" : " · " + _session.Content.Require(room).Name);
        }
        public RecordProgress Progress(string id)
        {
            var progress = _session.State.Playback.FirstOrDefault(p => p.Id == id);
            if (progress == null) { progress = new RecordProgress { Id = id }; _session.State.Playback.Add(progress); }
            return progress;
        }
        public bool Seek(string id, float cursor, string tapeId = null)
        {
            if (!CanAccess(id, tapeId)) return false;
            var record = _session.Content.Require(id); cursor = Math.Max(0, Math.Min(cursor, record.Number("duration"))); Progress(id).Cursor = cursor;
            foreach (var line in _session.Content.Plot(record.Get("plot")).Where(l => l.Number("at") <= cursor)) if (!_session.State.ReadLines.Contains(line.Id)) _session.State.ReadLines.Add(line.Id);
            if (cursor >= record.Number("duration"))
            {
                if (!_session.State.Played.Contains(id)) _session.State.Played.Add(id);
                if (id == _session.Settings.TutorialRecord && tapeId != null) _session.Notice = "指定磁带播放完毕，可以完成房间引导。";
            }
            return true;
        }
        public bool Mark(string lineId)
        {
            if (!_session.State.ReadLines.Contains(lineId) || _session.Content.Require(lineId).Get("anchor") != "true") return false;
            if (!_session.State.MarkedLines.Contains(lineId)) _session.State.MarkedLines.Add(lineId); _session.Persist(); return true;
        }
        public bool Burn(string tapeId, string recordId, bool overwriteConfirmed)
        {
            if (!CanAccess(recordId)) return false;
            var tape = _session.State.Tapes.FirstOrDefault(t => t.Id == tapeId);
            if (tape == null || !string.IsNullOrEmpty(tape.RecordId) && !overwriteConfirmed) return false;
            tape.RecordId = recordId; _session.Persist(); return true;
        }
    }
}

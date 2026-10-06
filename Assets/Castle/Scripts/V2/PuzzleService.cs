using System;
using System.Linq;
namespace Castle.V2
{
    public sealed class PuzzleService
    {
        readonly SessionService _session;
        public PuzzleService(SessionService session) { _session = session; }
        public PuzzleDraft Draft(string id)
        {
            var draft = _session.State.Drafts.FirstOrDefault(d => d.Id == id);
            if (draft == null) { draft = new PuzzleDraft { Id = id }; _session.State.Drafts.Add(draft); } return draft;
        }
        public void Edit(string id, string[] values, float shift = 0, string reference = null)
        {
            if (_session.State.Solved.Contains(id) || _session.State.PendingPuzzle == id) return;
            var draft = Draft(id); draft.Values = values.ToArray(); draft.Shift = (float)Math.Round(shift / _session.Settings.CalibrationStep) * _session.Settings.CalibrationStep; draft.Reference = reference; _session.Persist();
        }
        public bool Available(ContentRow puzzle) => _session.Meets(puzzle.List("need")) && (puzzle.Get("type") != "conclusion" || _session.State.Solved.Contains(puzzle.Get("target")));
        public string Submit(string id)
        {
            var puzzle = _session.Content.Require(id); var state = _session.State; var draft = Draft(id);
            if (state.Solved.Contains(id)) return null;
            if (state.Active != null || !string.IsNullOrEmpty(state.PendingPuzzle)) return "请先完成当前对白或奖励。";
            if (!Available(puzzle)) return "指定的前置证据尚未收齐。";
            var values = draft.Values; var candidates = puzzle.List("candidates"); string type = puzzle.Get("type");
            bool correct = values.All(candidates.Contains);
            var expected = ContentFormat.Answers(puzzle);
            if (type == "anchor") correct &= values.Distinct().Count() == values.Length && values.Length == expected.Length && values.All(expected.Contains) && values.All(state.MarkedLines.Contains);
            else if (type == "location") correct &= values.Length == 1 && values[0] == _session.Settings.Devices.Devices.First(d => d.Id == puzzle.Get("target")).Room;
            else if (type == "calibrate")
            {
                var target = _session.Settings.Devices.Devices.First(d => d.Id == puzzle.Get("target"));
                var reference = _session.Settings.Devices.Devices.FirstOrDefault(d => d.Id == draft.Reference);
                bool reliable = reference != null && puzzle.List("reference").Contains(reference.Id) && (_session.Knowledge(reference.Id).Calibrated || reference.Proof.Length > 0 && _session.Meets(reference.Proof));
                bool matched = state.Solved.Select(_session.Content.Require).Where(p => p.Get("type") == "anchor").Any(p => values.Length == 2 && values.All(ContentFormat.Answers(p).Contains));
                correct &= reliable && matched && values.All(state.MarkedLines.Contains) && Math.Abs(draft.Shift + target.Offset) <= _session.Settings.CalibrationTolerance;
                if (correct)
                {
                    var records = values.Select(_session.Recordings.RecordForLine).ToArray();
                    int targetIndex = Array.FindIndex(records, r => r.Get("device") == target.Id), refIndex = Array.FindIndex(records, r => r.Get("device") == reference.Id);
                    correct &= targetIndex >= 0 && refIndex >= 0;
                    if (correct)
                    {
                        var a = _session.Recordings.TimeAt(records[targetIndex].Id, _session.Content.Require(values[targetIndex]).Number("at"), false).AddSeconds(draft.Shift);
                        var b = _session.Recordings.TimeAt(records[refIndex].Id, _session.Content.Require(values[refIndex]).Number("at"), true);
                        correct &= Math.Abs((a - b).TotalSeconds) <= _session.Settings.CalibrationTolerance;
                    }
                }
            }
            else correct &= values.SequenceEqual(expected);
            if (type == "timeline") correct &= values.All(_session.Has) && values.Distinct().Count() == values.Length;
            if (!correct) { _session.Persist(); return puzzle.Get("fail_text"); }
            state.PendingPuzzle = id; _session.Persist(); _session.Events.Schedule(); return null;
        }
        public void Commit(string id)
        {
            var puzzle = _session.Content.Require(id);
            if (!_session.State.Solved.Contains(id)) _session.State.Solved.Add(id);
            if (puzzle.Get("type") == "location") _session.Knowledge(puzzle.Get("target")).Room = Draft(id).Values[0];
            if (puzzle.Get("type") == "calibrate") { var knowledge = _session.Knowledge(puzzle.Get("target")); knowledge.Calibrated = true; knowledge.Offset = -Draft(id).Shift; }
        }
    }
}

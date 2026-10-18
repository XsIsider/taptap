using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
namespace Castle.V24
{
    /// <summary>纯规则层。界面只调用操作，所有结算与待处理状态在一次存档中提交。</summary>
    public sealed partial class V24Engine
    {
        public readonly V24Data Data;
        public V24State State;
        public Action Changed;
        public Func<V24Event, bool> WaitForSelection;
        public V24Engine(V24Data data, V24State state) { Data = data; State = state; }
        public DateTime Now => new DateTime(State.Now);
        public DateTime BaseDate => new DateTime(State.DayBase);
        public DateTime Cutoff => BaseDate.AddDays(1).Add(TimeSpan.Parse(Data.GameSettings.ForceSleepTime));
        public bool Busy => State.ActiveEvent != 0 || State.PendingPuzzle != 0 || State.RewardPopups.Count > 0 || State.TutorialEvent != 0;
        public bool IntroDone => State.Puzzles.Contains(Data.GameSettings.IntroSoundPath);
        public void Commit() { RefreshUnlocks(); Changed?.Invoke(); }
        public void NewGame()
        {
            var time = DateTime.Parse(Data.GameSettings.InitialTime, CultureInfo.InvariantCulture);
            State = new V24State { Started = true, Room = Data.GameSettings.ArrivalRoom, Now = time.Ticks, DayBase = (time.TimeOfDay < TimeSpan.FromHours(10) ? time.Date.AddDays(-1) : time.Date).Ticks, Page = "Invitation" };
            foreach (var id in Data.GameSettings.InitialItems) Grant(id, "initial:" + id, false);
            RegisterRecordings(); Commit();
        }
        public bool Has(int id) => State.Entries.Contains(id);
        public bool Need(IEnumerable<string> needs)
        {
            if (needs == null) return true;
            foreach (var n in needs.Where(x => !string.IsNullOrEmpty(x)))
            {
                var split = n.Split(':');
                if (split.Length != 2 || !int.TryParse(split[1], out int id)) return false;
                bool yes = split[0] == "entry" ? Has(id) : split[0] == "goal" ? State.Goals.Contains(id) : split[0] == "puzzle" ? State.Puzzles.Contains(id) : split[0] == "talk" && State.ActiveEvent == id;
                if (!yes) return false;
            }
            return true;
        }
        public bool Window(string start, string end, DateTime when)
        {
            if (string.IsNullOrEmpty(start) && string.IsNullOrEmpty(end)) return true;
            if (start == "00:00" && end == "24:00") return true;
            double a = TimeSpan.Parse(start).TotalMinutes, b = end == "24:00" ? 1440 : TimeSpan.Parse(end).TotalMinutes;
            if (a == b) return false;
            for (int day = -1; day <= 1; day++)
            {
                var from = BaseDate.AddDays(day).AddMinutes(a);
                var to = BaseDate.AddDays(day + (b < a ? 1 : 0)).AddMinutes(b);
                if (when >= from && when < to && when >= BaseDate.AddHours(10) && when < Cutoff) return true;
            }
            return false;
        }
        public bool Done(V24Event ev) => ev.Goal != 0 ? State.Goals.Contains(ev.Goal) : State.CompletedEvents.Contains(ev.Id);
        public bool Eligible(V24Event ev, bool direct = false)
        {
            if (ev == null || Done(ev)) return false;
            if (ev.Trigger == "reward") return direct;
            if (!Need(ev.Need)) return false;
            if (ev.Trigger == "flow") return direct;
            if (State.SleepPending) return false;
            if (ev.Room != 0 && ev.Room != State.Room) return false;
            if (ev.Day != 0 && ev.Day != State.Day) return false;
            if (!Window(ev.Start, ev.End, Now)) return false;
            if (Data.Event.Any(x => x.PlanB == ev.Id) && !State.PlanB.Contains(ev.Id)) return false;
            if (ev.PlanB != 0 && State.PlanB.Contains(ev.PlanB)) return false;
            return true;
        }
        public void RefreshUnlocks()
        {
            foreach (var room in Data.Room) if (!State.Unlocked.Contains(room.Id) && Need(room.UnlockNeed)) State.Unlocked.Add(room.Id);
            foreach (var device in Data.DeviceConfig) if (State.PendingPuzzle != Data.GameSettings.IntroSoundPath && !State.KnownDevices.Contains(device.Id) && Need(device.LocationNeed)) State.KnownDevices.Add(device.Id);
        }
        public string CharacterName(int id)
        {
            var c = Data.Character.FirstOrDefault(x => x.Id == id);
            return c == null ? "旁白" : Need(c.NameNeed) ? c.Name : c.UnknownName;
        }
        public string DeviceName(int id)
        {
            var d = Data.DeviceConfig.FirstOrDefault(x => x.Id == id);
            if (d == null) return "外来磁带";
            return d.Label + " · " + (State.KnownDevices.Contains(id) ? RoomName(d.Room) : "位置未知");
        }
        public bool CanEnter(int id)
        {
            var room = Data.Room.FirstOrDefault(x => x.Id == id);
            if (room == null || (id != State.Room && !CanExitControl())) return false;
            if (State.Room == Data.GameSettings.ControlRoom && State.SessionAction != 0 && id != State.Room && id != State.ControlReturn) return false;
            if (id == Data.GameSettings.ControlRoom && State.Room != id && !ControlMapOpen()) return false;
            return !Busy && !State.SleepPending && Now < Cutoff && State.Unlocked.Contains(id) && Window(room.Start, room.End, Now);
        }
        public bool Move(int id)
        {
            if (!CanEnter(id)) return false;
            if (State.Room == Data.GameSettings.ControlRoom && id != State.Room && !string.IsNullOrEmpty(State.ControlSession))
            {
                long target = new DateTime(State.ControlEntered).AddMinutes(State.ControlCost).Ticks;
                State.ControlSession = ""; State.SessionAction = 0; AdvanceTime(target);
                if (State.SleepPending) { Sleep(); return true; }
            }
            int old = State.Room; State.Room = id; State.Page = "Explore";
            if (old != id) State.Transcript.Clear();
            if (!CanEnter(id)) State.Room = Data.GameSettings.HomeRoom;
            if (State.Room == Data.GameSettings.ControlRoom && string.IsNullOrEmpty(State.ControlSession))
            {
                State.ControlSession = Guid.NewGuid().ToString("N"); State.ControlEntered = State.Now;
                State.ControlCost = Data.GameSettings.ControlMinutes; State.ControlDay = State.Day; State.ControlReturn = old; State.ControlMode = "normal";
            }
            Scan(); Commit(); return true;
        }
        public void Scan()
        {
            if (Busy || State.SleepPending || State.Finished || State.ActionPending != 0 || State.SleepStage != "") return;
            var ev = Data.Event.FirstOrDefault(x => x.Trigger == "auto" && Eligible(x))
                ?? Data.Event.FirstOrDefault(x => x.Trigger == "interact" && Eligible(x));
            if (ev != null && !(WaitForSelection?.Invoke(ev) ?? false)) Start(ev.Id);
        }
        public bool Start(int id, bool direct = false, bool keepText = false, bool validated = false)
        {
            var ev = Data.EventById(id);
            if (ev == null || (!validated && !Eligible(ev, direct))) return false;
            if (!keepText) { State.Transcript.Clear(); State.DialogueScroll = 1; }
            State.ActiveEvent = id; State.Line = -1;
            if (ev.Record != 0 && State.Recordings.All(x => x.Id != ev.Record))
            {
                var record = Data.RecordById(ev.Record); int offset = Offset(record.Device);
                State.Recordings.Add(new V24RecordingState { Id = record.Id, TrueStart = State.Now, RawStart = Now.AddSeconds(offset).Ticks, CapturedOffset = offset, Day = State.Day });
            }
            if (Data.Lines(ev.Plot).Length == 0 && ev.ChoiceGroup == 0) Finish(); else Reveal();
            Commit(); return true;
        }
        public void Reveal()
        {
            var ev = Data.EventById(State.ActiveEvent); if (ev == null) return;
            var lines = Data.Lines(ev.Plot);
            if (State.Line + 1 >= lines.Length) return;
            State.Line++;
            var line = lines[State.Line];
            // 分支首句若与刚选的女主回复相同，保留录音行但现场不重复显示。
            bool repeat = State.Line == 0 && State.Transcript.LastOrDefault()?.Reply == line.Text;
            if (!repeat) State.Transcript.Add(new V24Transcript { Plot = line.Id });
            if (!State.Shown.Contains(line.Id)) State.Shown.Add(line.Id);
            if (State.Line == lines.Length - 1 && ev.Record != 0) State.Recordings.First(x => x.Id == ev.Record).Complete = true;
            Commit();
        }
        public void Advance(int choice = 0)
        {
            var ev = Data.EventById(State.ActiveEvent); if (ev == null) return;
            if (RequiredAction() != null) return;
            if (State.Line + 1 < Data.Lines(ev.Plot).Length) { Reveal(); return; }
            if (ev.ChoiceGroup != 0 && State.QueuedItem == 0 && choice == 0) return;
            if (choice != 0)
            {
                var selected = Data.PlotChoice.First(x => x.Id == choice && x.Group == ev.ChoiceGroup);
                State.Choices.Add(choice); State.Transcript.Add(new V24Transcript { Choice = choice, Reply = selected.Text });
            }
            Finish(choice);
        }
        public void Finish(int choice = 0)
        {
            var ev = Data.EventById(State.ActiveEvent); if (ev == null) return;
            if (ev.Id == Data.GameSettings.TapeTutorialEvent && State.TutorialEvent == 0)
            {
                State.TutorialEvent = ev.Id; State.ActiveEvent = 0; State.Auto = false; Commit(); return;
            }
            if (!Done(ev))
            {
                foreach (int id in ev.Reward)
                {
                    bool linked = Data.Lines(ev.Plot).Any(line => State.Claims.Contains(line.Id + ":" + id));
                    if (!linked) Grant(id, "event:" + ev.Id + ":" + id);
                }
                if (ev.Goal != 0) State.Goals.Add(ev.Goal);
                State.CompletedEvents.Add(ev.Id);
                if (ev.Time > 0) AdvanceTime(Now.AddMinutes(ev.Time).Ticks);
            }
            int next = State.QueuedItem != 0 ? State.QueuedItem : choice != 0 ? Data.PlotChoice.First(x => x.Id == choice).NextEvent : ev.NextEvent;
            bool queued = State.QueuedItem != 0; State.QueuedItem = 0; State.ActiveEvent = 0;
            if (next != 0 && !Start(next, true, true, queued)) throw new InvalidOperationException("接续事件前置未满足：" + next);
            if (next == 0) EndChain();
            Commit();
        }
        public void EndChain()
        {
            if (State.RewardPopups.Count > 0) return;
            if (State.ActionPending != 0) { ResumeAction(); return; }
            if (State.PendingPuzzle != 0)
            {
                int id = State.PendingPuzzle;
                if (!State.Puzzles.Contains(id)) State.Puzzles.Add(id);
                State.PendingPuzzle = 0;
                State.Page = id == Data.GameSettings.IntroSoundPath ? "Recordings" : State.ReturnPage;
                if (State.Ending) { State.Ending = false; State.Page = "Recordings"; }
            }
            State.Auto = false;
            if (State.Goals.Contains(Data.FinaleConfig.EndCard.AfterGoal))
            { State.Finished = true; State.Page = "EndCard"; State.SleepPending = false; return; }
            if (State.SleepStage != "") { ResumeSleep(); return; }
            if (State.SleepPending && !State.Ending) Sleep();
            RefreshUnlocks();
        }
        public void CloseReward()
        {
            if (State.RewardPopups.Count > 0) State.RewardPopups.RemoveAt(0);
            if (State.ActiveEvent == 0 && State.RewardPopups.Count == 0) { EndChain(); Scan(); }
            Commit();
        }
        public void Grant(int id, string key, bool popup = true)
        {
            if (State.Grants.Contains(key)) return;
            var entry = Data.EntryById(id); if (entry == null) throw new InvalidOperationException("未知奖励 " + id);
            State.Grants.Add(key);
            bool owned = Has(id);
            if (!owned) State.Entries.Add(id);
            if (entry.Type == "item" || entry.Type == "tape")
            {
                var inventory = State.Inventory.FirstOrDefault(x => x.Entry == id);
                if (inventory == null) { inventory = new V24Inventory { Entry = id }; State.Inventory.Add(inventory); }
                inventory.Count++;
            }
            if (entry.Type == "tape") State.Tapes.Add(new V24Tape { Instance = State.NextTape++, Entry = id, Record = entry.Record });
            if (popup && (entry.Type == "item" || entry.Type == "tape") && (!owned || entry.Type == "tape")) State.RewardPopups.Add(id);
            RefreshUnlocks();
        }
        public void Claim(int lineId, int entryId)
        {
            if (!State.Shown.Contains(lineId)) return;
            string key = lineId + ":" + entryId;
            if (!State.Claims.Contains(key))
            {
                State.Claims.Add(key);
                if (!Has(entryId)) Grant(entryId, "link:" + key);
            }
            Commit();
        }
        public string UseItem(int id)
        {
            if (!Has(id)) return "尚未持有该道具";
            if (State.TutorialEvent != 0) return "请打开教学磁带，查看后关闭";
            if (State.SleepPending) return "即将休息，请先听完这段对话";
            if (State.QueuedItem != 0) return "先听完这段对话";
            var ev = Data.Event.FirstOrDefault(x => x.Trigger == "item_use" && x.UseEntry == id && Eligible(x) && TargetPresent(x.Target));
            if (ev == null) return "似乎没什么用";
            if (State.ActiveEvent != 0) State.QueuedItem = ev.Id;
            else Start(ev.Id, true);
            Commit(); return "使用成功";
        }
        public bool TargetPresent(string target)
        {
            if (string.IsNullOrEmpty(target)) return true;
            if (!target.StartsWith("character:") || State.ActiveEvent == 0) return false;
            int id = int.Parse(target.Substring(10));
            return Data.Lines(Data.EventById(State.ActiveEvent).Plot).Any(x => x.SpeakerId == id);
        }
        public void AdvanceTime(long target)
        {
            long old = State.Now;
            State.Now = Math.Min(target, Cutoff.Ticks);
            State.SleepPending |= target >= Cutoff.Ticks;
            RegisterRecordings(); Expire(old, State.Now);
        }
        private void Expire(long old, long next)
        {
            foreach (var ev in Data.Event.Where(x => x.PlanB != 0 && !Done(x) && (x.Day == 0 || x.Day == State.Day) && Need(x.Need)))
            {
                for (int day = -1; day <= 1; day++)
                {
                    if (string.IsNullOrEmpty(ev.End)) continue;
                    double a = TimeSpan.Parse(ev.Start).TotalMinutes, b = ev.End == "24:00" ? 1440 : TimeSpan.Parse(ev.End).TotalMinutes;
                    var end = BaseDate.AddDays(day + (b < a ? 1 : 0)).AddMinutes(b);
                    if (end.Ticks > old && end.Ticks <= next && !State.PlanB.Contains(ev.PlanB)) State.PlanB.Add(ev.PlanB);
                }
            }
        }
        public void Sleep()
        {
            if (SleepReason() != "") return;
            State.SleepPending = true; State.SleepStage = "blackout"; State.Page = "Cutscene"; Commit();
        }
        public int Offset(int device) => State.Repaired.Contains(device) ? 0 : Data.DeviceConfig.FirstOrDefault(x => x.Id == device)?.Offset ?? 0;
        public void RegisterRecordings()
        {
            foreach (var record in Data.Record.Where(x => x.AccessType == "central" && !string.IsNullOrEmpty(x.Start)))
            {
                var device = Data.DeviceConfig.First(x => x.Id == record.Device);
                DateTime real = DateTime.Parse(record.Start, CultureInfo.InvariantCulture).AddSeconds(-device.Offset);
                if (real.Ticks > State.Now) continue;
                var snapshot = State.Recordings.FirstOrDefault(x => x.Id == record.Id);
                if (snapshot == null)
                {
                    int offset = Offset(record.Device);
                    var date = real.TimeOfDay < TimeSpan.FromHours(10) ? real.Date.AddDays(-1) : real.Date;
                    snapshot = new V24RecordingState { Id = record.Id, TrueStart = real.Ticks, RawStart = real.AddSeconds(offset).Ticks, CapturedOffset = offset, Day = State.Day + (date - BaseDate).Days };
                    State.Recordings.Add(snapshot);
                }
                snapshot.Complete |= real.AddSeconds(record.Duration).Ticks <= State.Now;
            }
        }
        public bool Accessible(V24Record record)
        {
            var snapshot = State.Recordings.FirstOrDefault(x => x.Id == record.Id);
            return record.AccessType == "central" && snapshot != null && snapshot.Complete && Need(record.Need) && (Data.GameSettings.KeepRecords || snapshot.Day == State.Day);
        }
        public long DisplayStart(V24RecordingState snapshot, int device) => snapshot.RawStart - (State.Repaired.Contains(device) ? TimeSpan.FromSeconds(snapshot.CapturedOffset).Ticks : 0);
        public bool Mark(int line)
        {
            if (!State.Shown.Contains(line) || !Data.Plot.First(x => x.Id == line).Anchor) return false;
            if (!State.Marked.Contains(line)) State.Marked.Add(line);
            Commit(); return true;
        }
        /// <summary>点击录音条切换标记；首次标记同时收录该句的人物信息和线索，取消不撤销已获知识。</summary>
        public bool ToggleMark(int line)
        {
            if (!State.Shown.Contains(line)) return false;
            var plot = Data.Plot.First(x => x.Id == line);
            var entries = Data.Entry.Where(x => (x.Type == "clue" || x.Type == "info") && x.Source.Contains("plot:" + line)).ToArray();
            if (!plot.Anchor && entries.Length == 0) return false;
            if (State.Marked.Contains(line))
            {
                State.Marked.Remove(line);
                foreach (var draft in State.Drafts.Where(x => !State.Puzzles.Contains(x.Id)))
                {
                    var type = Data.PuzzleById(draft.Id).Type;
                    if (type != "anchor_align" && type != "logic_match") continue;
                    draft.Values.Remove(line);
                    if (draft.Reference == line) draft.Reference = 0;
                }
            }
            else
            {
                State.Marked.Add(line);
                foreach (var entry in entries) if (!Has(entry.Id)) Grant(entry.Id, "extract:" + entry.Id, false);
            }
            Commit(); return true;
        }
        public V24Draft Draft(int id)
        {
            var draft = State.Drafts.FirstOrDefault(x => x.Id == id);
            if (draft == null) { draft = new V24Draft { Id = id }; State.Drafts.Add(draft); }
            return draft;
        }
        public string ValidatePuzzle(int id)
        {
            var puzzle = Data.PuzzleById(id); var draft = Draft(id);
            if (State.Puzzles.Contains(id)) return "已完成";
            if (!Need(puzzle.Need)) return "请先收集所需证据";
            if (puzzle.Type == "sound_path" && Data.DeviceConfig.Any(x => x.Offset != 0 && !State.Repaired.Contains(x.Id))) return "请先完成全部故障设备校时";
            if (Busy) return "请先完成当前对白与奖励";
            int[] answer = puzzle.Answer.Split('|').Select(x => int.Parse(x.Contains("=") ? x.Split('=')[1] : x)).ToArray();
            bool set = puzzle.Type == "anchor_align" || puzzle.Type == "logic_match";
            if (draft.Values.Count != answer.Length || draft.Values.Any(x => x == 0)) return "请放齐所有槽位";
            if (set && draft.Values.Any(x => !State.Marked.Contains(x))) return "请先在录音中标记证据";
            if (puzzle.Type == "timeline" && draft.Values.Any(x => !Has(x))) return "尚未获得所需事件卡";
            if (set && (draft.Values.Distinct().Count() != draft.Values.Count || !draft.Values.OrderBy(x => x).SequenceEqual(answer.OrderBy(x => x)))) return "这些片段不属于本次关联，请重新选择";
            if (!set && !draft.Values.SequenceEqual(answer)) return "顺序或位置不正确，请调整";
            if (puzzle.Type == "anchor_align")
            {
                if (!draft.Values.Contains(draft.Reference)) return "请重新选择参考轨道";
                var reference = Data.Record.First(x => x.Plot == Data.Plot.First(l => l.Id == draft.Reference).Group);
                if (!puzzle.Reference.Contains(reference.Device) || Offset(reference.Device) != 0) return "这条录音暂时不能作为时间参考，请更换";
                var target = Data.Record.First(x => x.Plot == Data.Plot.First(l => l.Id == draft.Values.First(v => v != draft.Reference)).Group);
                if (target.Device == reference.Device) return "请选择两个不同设备";
                var r = State.Recordings.First(x => x.Id == reference.Id); var t = State.Recordings.First(x => x.Id == target.Id);
                double refTime = new TimeSpan(DisplayStart(r, reference.Device)).TotalSeconds + Data.Plot.First(x => x.Id == draft.Reference).At;
                double targetTime = new TimeSpan(DisplayStart(t, target.Device)).TotalSeconds + Data.Plot.First(x => x.Id == draft.Values.First(v => v != draft.Reference)).At;
                if (Math.Abs(targetTime + draft.Shift - refTime) > 2 || Math.Abs(draft.Shift + (State.Repaired.Contains(target.Device) ? 0 : t.CapturedOffset)) > 2) return "时间还未对齐，请调整音轨";
                RegisterRecordings();
                if (!State.Repaired.Contains(target.Device)) State.Repaired.Add(target.Device);
            }
            State.PendingPuzzle = id;
            State.Ending |= puzzle.Type == "timeline";
            Commit();
            return "成功";
        }
        public void ContinuePuzzleReward()
        {
            if (State.PendingPuzzle == 0 || State.ActiveEvent != 0) return;
            var puzzle = Data.PuzzleById(State.PendingPuzzle);
            if (puzzle.Success != 0 && !Done(Data.EventById(puzzle.Success))) Start(puzzle.Success, true);
            else EndChain();
            Commit();
        }
        public void Burn(int instance, int recordId)
        {
            var record = Data.RecordById(recordId);
            if (State.Room != Data.GameSettings.ControlRoom || !Accessible(record)) throw new InvalidOperationException("文件不可刻录");
            var tape = State.Tapes.First(x => x.Instance == instance); var snapshot = State.Recordings.First(x => x.Id == recordId);
            tape.Record = recordId; tape.RawStart = snapshot.RawStart; tape.CapturedOffset = snapshot.CapturedOffset; Commit();
        }
        public void TapeCompleted(int instance)
        {
            var tape = State.Tapes.First(x => x.Instance == instance);
            if (State.TutorialEvent != 0 && tape.Entry == Data.TutorialTape)
            { State.ActiveEvent = State.TutorialEvent; Finish(); State.TutorialEvent = 0; Commit(); }
        }
    }
}

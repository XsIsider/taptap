using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Castle.V24;
using UnityEngine;

namespace Castle.Tools
{
    /// <summary>从新游戏沿正常规则生成内置测试档；不读取或写入玩家自动档。</summary>
    public static class GenerateTestCheckpoints
    {
        private const string Output = "Assets/Castle/Resources/Castle/V24/TestCheckpoints";
        private static V24Data _data;
        private static readonly V24TestCheckpointCatalog Catalog = new V24TestCheckpointCatalog { Version = 26 };
        private static int _checks;

        private static void Check(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
            _checks++;
        }

        private static V24State Clone(V24State state) => JsonUtility.FromJson<V24State>(JsonUtility.ToJson(state));
        private static V24Engine Engine(V24State state) => new V24Engine(_data, Clone(state));
        private static bool AtEvent(V24State state, int id) => state.ActiveEvent == id && state.Line == 0 && state.RewardPopups.Count == 0;
        private static bool AtChoice(V24State state, int id) => state.ActiveEvent == id && state.RewardPopups.Count == 0
            && state.Line == _data.Lines(_data.EventById(id).Plot).Length - 1;

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Castle/V2.4/Regenerate Test Checkpoints")]
        public static void Regenerate()
        {
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("请退出 Play 后再生成测试存档。");
            Main(Array.Empty<string>());
            UnityEditor.AssetDatabase.Refresh();
        }
#endif

        public static void Main(string[] args)
        {
            Catalog.Checkpoints.Clear();
            _checks = 0;
            string raw = File.ReadAllText("Assets/Castle/Resources/Castle/V24/Content.json");
            _data = JsonUtility.FromJson<V24Data>(raw);
            Catalog.ContentHash = V24TestCheckpoints.Fingerprint(raw);
            Directory.CreateDirectory(Output);
            var engine = new V24Engine(_data, new V24State());
            engine.NewGame();
            // 对应黑屏旁白两次点击、邀请函展开/收起，以及正式抵达事件的 UI 操作。
            foreach (var line in engine.OpeningLines) engine.State.Shown.Add(line.Id);
            engine.State.InvitationStage = 2; engine.State.Page = "Explore";
            engine.Grant(_data.InvitationEntry, "invitation");
            while (engine.State.RewardPopups.Count > 0) engine.CloseReward();
            Check(engine.Start(_data.GameSettings.ArrivalEvent, true), "Arrival did not start");

            Reach(engine, s => AtEvent(s, 920014));
            Save(engine, "day2-wake", "第二天 · 起床", "11:30｜礼物、信件流程开头");
            Reach(engine, s => s.Day == 2 && s.Room == _data.GameSettings.ControlRoom
                && s.ActiveEvent == 0 && !engine.Busy && s.CompletedEvents.Contains(920017) && s.Puzzles.Count == 0);
            Save(engine, "day2-calibration", "第二天 · 首次中控调查", "13:00｜校时与空间题尚未完成", "Recordings");
            Reach(engine, s => s.Puzzles.Contains(720001) && s.Puzzles.Contains(720002)
                && !s.Puzzles.Contains(720003) && !engine.Busy);
            Save(engine, "day2-space", "第二天 · 空间推演前", "两台设备已校时，空间题未做", "Space");
            Reach(engine, s => s.Activity == "day2_afternoon" && !engine.Busy);
            Save(engine, "day2-afternoon", "第二天 · 下午调查", "16:00｜三项地图调查均未开始");
            Reach(engine, s => AtEvent(s, 920033));
            Save(engine, "day2-banquet", "第二天 · 晚宴开场", "18:30｜多人交谈尚未开始");
            Reach(engine, s => AtChoice(s, 920050));
            Save(engine, "day2-body-choice", "第二天 · 尸体选择前", "00:00｜息事宁人／告知众人均可选");
            var bodyChoice = Clone(engine.State);
            Reach(engine, s => AtEvent(s, 920057));
            Save(engine, "day3-wake-hide", "第三天 · 起床（隐瞒）", "10:00｜前夜选择息事宁人");
            var report = Engine(bodyChoice);
            Check(report.AdvanceChoice(440007), "Report branch could not be selected");
            Reach(report, s => AtEvent(s, 920058), true, true);
            Save(report, "day3-wake-report", "第三天 · 起床（告知）", "11:00｜前夜已告知众人");
            var reportDecline = Engine(report.State);
            Reach(engine, s => AtChoice(s, 920067));
            Save(engine, "day3-muffin-choice", "第三天 · 松饼选择前", "13:30｜答应／不答应均可选");
            var muffinChoice = Clone(engine.State);
            Reach(engine, s => s.Activity == "day3_afternoon" && !engine.Busy);
            Save(engine, "day3-afternoon-accept", "第三天 · 下午（答应）", "14:00｜科林、维里诺均待拜访");
            var decline = Engine(muffinChoice);
            Reach(decline, s => s.Activity == "day3_afternoon" && !decline.Busy, false);
            Save(decline, "day3-afternoon-decline", "第三天 · 下午（不答应）", "14:00｜没有松饼，仅科林拜访可用");
            Reach(engine, s => s.Day == 3 && s.Activity == "" && !engine.Busy
                && engine.Now.TimeOfDay == TimeSpan.FromHours(22));
            Check(engine.Move(_data.GameSettings.HomeRoom), "Return home before night failed");
            Save(engine, "day3-night", "第三天 · 夜间自由调查", "22:00｜可休息，或去中控室调查");
            var night = Clone(engine.State);
            var ghost = Engine(night);
            Check(ghost.Move(_data.GameSettings.ControlRoom), "Night control entry failed");
            ghost.ImportRecords();
            Check(ghost.Move(_data.GameSettings.HomeRoom), "Night control exit failed");
            Check(ghost.Now.TimeOfDay == TimeSpan.FromHours(23), "Night visit did not naturally cost one hour");
            Check(ghost.SleepReason() == "", "Ghost-night rest unavailable");
            ghost.Sleep(); ghost.ResumeSleep();
            Check(AtEvent(ghost.State, 920076), "Ghost event not reached");
            Save(ghost, "day3-ghost", "第三天 · 遇鬼开头", "23:00｜正常夜访中控室后回房休息", "Cutscene");
            Reach(engine, s => AtEvent(s, 920077));
            Save(engine, "day4-wake-hide-accept", "第四天 · 起床（隐瞒／答应）", "10:00｜两次下午拜访均已完成");
            Reach(report, s => AtEvent(s, 920077), true, true);
            Save(report, "day4-wake-report-accept", "第四天 · 起床（告知／答应）", "10:00｜保留告知分支的历史记录");
            Reach(decline, s => AtEvent(s, 920077), false);
            Save(decline, "day4-wake-hide-decline", "第四天 · 起床（隐瞒／不答应）", "10:00｜未做松饼开放的维里诺拜访");
            Reach(reportDecline, s => AtEvent(s, 920077), false, true);
            Save(reportDecline, "day4-wake-report-decline", "第四天 · 起床（告知／不答应）", "10:00｜保留告知与不答应两项选择");
            Reach(engine, s => s.Day == 4 && s.Room == _data.GameSettings.ControlRoom && !engine.Busy
                && s.SessionAction == 1500002 && s.CompletedEvents.Contains(920088));
            engine.ImportRecords();
            Save(engine, "day4-control", "第四天 · 中控调查开始", "11:00｜教学已做，六道对照与时间线未做", "Recordings");
            var control = Clone(engine.State);
            // 打开可访问录音全文，相当于逐个选择录音文件，不直接发放线索。
            ReadAndMark(engine, false);
            while (engine.State.RewardPopups.Count > 0) engine.CloseReward();
            Check(engine.Accessible(_data.RecordById(620024)), "Identity tape not normally accessible");
            engine.CompleteRecord(620024); Drain(engine);
            foreach (var puzzle in _data.Puzzle.Where(x => x.Type == "logic_match"))
            {
                Check(engine.Need(puzzle.Need), "Comparison prerequisite missing: " + puzzle.Id);
                Solve(engine, puzzle); Drain(engine);
            }
            Check(engine.Need(_data.PuzzleById(720030).Need), "Timeline clues incomplete after normal collection");
            Save(engine, "day4-timeline-ready", "第四天 · 时间线重构前", "六题与十线索已齐，人物路径未做", "Timeline");
            Solve(engine, _data.PuzzleById(720030));
            // 对应原 RevealEnding 协程完成；真相第一句尚未点击。
            engine.State.EndingTransitionDone = true; engine.State.Page = "Timeline";
            engine.State.RecordId = 620025; engine.State.RecordPosition = (float)_data.Lines(_data.RecordById(620025).Plot)[0].At;
            engine.State.PlaybackHeard = engine.State.RecordPosition;
            Save(engine, "day4-truth", "第四天 · 真相对白开头", "已正确重构，点击对白逐句推进", "Timeline");
            engine.CompleteRecord(620025); Drain(engine);
            Check(engine.State.Puzzles.Contains(720030), "Timeline completion did not settle");
            Check(engine.Need(_data.PuzzleById(720020).Need), "Person path prerequisites unavailable after truth");
            Save(engine, "day4-path-ready", "第四天 · 人物路径待推理", "真相对白已结算，人物路径尚未完成", "Recordings");
            Check(engine.Move(engine.State.ControlReturn), "Final control exit failed");
            Reach(engine, s => AtEvent(s, 920091));
            Save(engine, "day4-final-known", "第四天 · 最后谈话（已知身份）", "16:05｜录音调查已获知母亲身份");
            var unknown = Engine(control);
            ReadAndMark(unknown, true);
            while (unknown.State.RewardPopups.Count > 0) unknown.CloseReward();
            foreach (var puzzle in _data.Puzzle.Where(x => x.Type == "logic_match")) { Solve(unknown, puzzle); Drain(unknown); }
            Check(unknown.Need(_data.PuzzleById(720030).Need), "Selective route lacks timeline clues");
            Check(!unknown.Has(320672), "Selective route accidentally learned identity");
            Solve(unknown, _data.PuzzleById(720030)); unknown.State.EndingTransitionDone = true;
            unknown.CompleteRecord(620025); Drain(unknown);
            Check(unknown.Move(unknown.State.ControlReturn), "Unknown route exit failed");
            Reach(unknown, s => AtEvent(s, 920091));
            Save(unknown, "day4-final-unknown", "第四天 · 最后谈话（未知身份）", "16:05｜保留尚未知晓母亲的对白分支");
            Reach(engine, s => s.Finished);
            Reach(unknown, s => s.Finished);
            Check(engine.State.CompletedEvents.Contains(920095) && unknown.State.CompletedEvents.Contains(920096), "Identity branches failed to diverge");
            File.WriteAllText(Output + "/Catalog.json", JsonUtility.ToJson(Catalog, true), new System.Text.UTF8Encoding(false));
            VerifyExported();
            Console.WriteLine("PASS checkpoints=" + Catalog.Checkpoints.Count + " checks=" + _checks + "; generated from NewGame; player save untouched");
        }

        private static void Reach(V24Engine engine, Func<V24State, bool> stop, bool accept = true, bool report = false)
        {
            for (int step = 0; step < 12000; step++)
            {
                if (stop(engine.State)) return;
                Pump(engine, accept, report);
                // 对应 Refresh 首次发现教学完成时显示维修提示并记录去重。
                if (engine.IntroDone && !engine.State.RepairNoticeShown) engine.State.RepairNoticeShown = true;
            }
            throw new InvalidOperationException("Progress stalled: " + JsonUtility.ToJson(engine.State));
        }

        private static void Pump(V24Engine engine, bool accept, bool report)
        {
            var state = engine.State;
            if (state.RewardPopups.Count > 0) { engine.CloseReward(); return; }
            var gate = engine.PendingInteraction;
            if (gate != null && engine.CanOpenInteraction(gate.Id))
            {
                if (gate.Id == 1500004 && !state.InteractionSignals.Contains("item_used:300001@plot:520006"))
                { Check(engine.UseInteractionItem(300001), "Invitation use failed"); return; }
                if (gate.Id == 1500003 && !state.InteractionSignals.Contains("player_selected_room:100001"))
                { Check(engine.SelectInteractionRoom(100001), "Initial room choice failed"); return; }
            }
            if (state.ActiveEvent != 0)
            {
                var ev = _data.EventById(state.ActiveEvent);
                var line = _data.Lines(ev.Plot)[state.Line];
                foreach (string target in line.LinkTarget.Where(x => x.StartsWith("entry:"))) engine.Claim(line.Id, int.Parse(target.Substring(6)));
                if (state.RewardPopups.Count > 0) return;
                var action = engine.RequiredAction();
                if (action != null) { Check(engine.ExecuteAction(action.Id) == "成功", "Control entry failed"); return; }
                int choice = 0;
                if (AtChoice(state, ev.Id) && ev.ChoiceGroup != 0)
                    choice = ev.Id == 920050 ? (report ? 440007 : 440006)
                        : ev.Id == 920067 ? (accept ? 440008 : 440009) : _data.PlotChoice.First(x => x.Group == ev.ChoiceGroup).Id;
                engine.Advance(choice); return;
            }
            if (state.SleepStage != "") { engine.ResumeSleep(); return; }
            if (state.PlaybackGate != 0) { Check(engine.ClosePlaybackGate(), "Playback exit failed"); return; }
            if (state.PendingPuzzle != 0) { engine.ContinuePuzzleReward(); return; }
            if (state.ActionPending != 0) { engine.ResumeAction(); return; }
            if (state.Room == _data.GameSettings.ControlRoom && state.Day == 2)
            {
                ReadAndMark(engine, true);
                if (state.RewardPopups.Count > 0) return;
                var puzzle = _data.Puzzle.FirstOrDefault(x => x.Id <= 720003 && !state.Puzzles.Contains(x.Id) && engine.Need(x.Need));
                if (puzzle != null) { ReadAndMark(engine, true); Solve(engine, puzzle); return; }
                Check(engine.IntroDone && engine.Move(state.ControlReturn), "Tutorial exit failed"); return;
            }
            var group = engine.CurrentConversationGroup();
            if (group != null)
            {
                int next = group.Ordered.FirstOrDefault(id => !engine.Done(_data.EventById(id)));
                if (next == 0) next = group.Events.First(id => !engine.Done(_data.EventById(id)));
                Check(engine.SelectFormalEvent(next), "Conversation selection failed"); return;
            }
            if (state.Activity != "")
            {
                var activity = _data.DayActivities.First(x => x.Id == state.Activity);
                int next = activity.Events.FirstOrDefault(id => !engine.Done(_data.EventById(id)) && (id != 920072 || engine.Has(300004)));
                if (next != 0)
                {
                    Check(engine.IsRoomBasedActivity ? engine.Move(_data.EventById(next).Room) : engine.SelectFormalEvent(next), "Activity selection failed"); return;
                }
                if (state.Room != _data.GameSettings.HomeRoom) Check(engine.Move(_data.GameSettings.HomeRoom), "Return home failed");
                Check(engine.WaitForActivity(), "Wait for banquet failed"); return;
            }
            if (state.Day == 3 && engine.Now.TimeOfDay >= TimeSpan.FromHours(22))
            {
                if (state.Room != _data.GameSettings.HomeRoom) Check(engine.Move(_data.GameSettings.HomeRoom), "Night return failed");
                Check(engine.SleepReason() == "", "Rest unavailable"); engine.Sleep(); return;
            }
            engine.Scan();
            Check(engine.Busy || state.SleepStage != "" || state.Activity != "" || state.Finished, "Unrecognized idle state: day=" + state.Day + " last=" + state.LastCompletedEvent);
        }

        private static void ReadAndMark(V24Engine engine, bool selective)
        {
            engine.ImportRecords();
            var needed = new HashSet<int>(_data.Puzzle.Where(x => x.Type == "anchor_align" || x.Type == "logic_match")
                .SelectMany(x => x.Answer.Split('|').Select(v => int.Parse(v.Split('=').Last()))));
            foreach (var effect in _data.PlotEffectConfig.Where(x => x.Trigger == "anchor_mark" && (x.Entry == 330010 || x.Entry >= 340001 && x.Entry <= 340010))) needed.Add(effect.Plot);
            foreach (var record in _data.Record.Where(engine.Accessible))
                foreach (var line in _data.Lines(record.Plot))
                {
                    if (!engine.State.Shown.Contains(line.Id)) engine.State.Shown.Add(line.Id);
                    if ((!selective || needed.Contains(line.Id)) && !engine.State.Marked.Contains(line.Id)) engine.ToggleMark(line.Id);
                }
        }

        private static void Solve(V24Engine engine, V24Puzzle puzzle)
        {
            while (engine.State.RewardPopups.Count > 0) engine.CloseReward();
            var draft = engine.Draft(puzzle.Id);
            draft.Values = puzzle.Answer.Split('|').Select(x => int.Parse(x.Split('=').Last())).ToList();
            if (puzzle.Type == "timeline") draft.Values = draft.Values.Select(engine.BestEntry).ToList();
            if (puzzle.Type == "anchor_align")
            {
                draft.Reference = draft.Values[0];
                var record = _data.Record.First(x => x.Plot == _data.Plot.First(p => p.Id == draft.Values[1]).Group);
                draft.Shift = -_data.DeviceConfig.First(x => x.Id == record.Device).Offset;
            }
            if (puzzle.Type == "sound_path") draft.Doors = new List<string> { "open", "closed", "closed" };
            Check(engine.ValidatePuzzle(puzzle.Id) == "成功", "Puzzle validation failed: " + puzzle.Id);
            if (puzzle.Type != "timeline") engine.ContinuePuzzleReward();
        }

        private static void Drain(V24Engine engine) => Reach(engine, s => !engine.Busy);

        private static void Save(V24Engine engine, string id, string label, string description, string page = "Explore")
        {
            var state = Clone(engine.State);
            state.Page = page; state.ReturnPage = "Explore"; state.Auto = false; state.PlayingTape = 0;
            if (!state.Ending) { state.RecordId = 0; state.RecordPosition = 0; state.PlaybackHeard = 0; }
            var checkpoint = new V24TestCheckpoint { Id = id, Label = label, Description = description, Resource = V24TestCheckpoints.ResourceRoot + id,
                Day = state.Day, Room = state.Room, ActiveEvent = state.ActiveEvent, Line = state.Line, Now = state.Now, Page = state.Page };
            V24TestCheckpoints.Validate(_data, state, checkpoint);
            Check(state.CompletedEvents.All(x => _data.EventById(x).Goal == 0 || state.Goals.Contains(_data.EventById(x).Goal)), "Completed goal missing");
            Check(state.Tapes.Select(x => x.Instance).Distinct().Count() == state.Tapes.Count, "Duplicate tape instance");
            File.WriteAllText(Output + "/" + id + ".json", JsonUtility.ToJson(state, true), new System.Text.UTF8Encoding(false));
            Catalog.Checkpoints.Add(checkpoint);
            Console.WriteLine(id + " | day=" + state.Day + " time=" + engine.Now.ToString("HH:mm") + " event=" + state.ActiveEvent + " line=" + state.Line);
        }

        private static void VerifyExported()
        {
            foreach (var checkpoint in Catalog.Checkpoints)
            {
                var state = V24Save.Read(Output + "/" + checkpoint.Id + ".json");
                V24TestCheckpoints.Validate(_data, state, checkpoint);
                var engine = new V24Engine(_data, state);
                string before = JsonUtility.ToJson(state);
                engine.ResumeAction();
                if (state.PendingPuzzle != 0 && state.ActiveEvent == 0 && state.RewardPopups.Count == 0) engine.ContinuePuzzleReward();
                engine.Scan();
                Check(JsonUtility.ToJson(state) == before, "Continue unexpectedly changes checkpoint: " + checkpoint.Id);
                if (state.ActiveEvent != 0)
                {
                    int ev = state.ActiveEvent, line = state.Line;
                    if (_data.EventById(ev).ChoiceGroup != 0 && AtChoice(state, ev))
                    {
                        foreach (var choice in _data.PlotChoice.Where(x => x.Group == _data.EventById(ev).ChoiceGroup))
                        {
                            var branch = Engine(state); branch.Advance(choice.Id);
                            Check(branch.State.Choices.Contains(choice.Id), "Checkpoint choice not accepted");
                        }
                    }
                    else
                    {
                        Pump(engine, true, false);
                        Check(state.ActiveEvent != ev || state.Line != line || state.RewardPopups.Count > 0, "Checkpoint cannot advance: " + checkpoint.Id);
                    }
                }
            }
            // 隔离目录验证首次无档安装及已有档备份，不接触 persistentDataPath。
            string target = "Logs/SaveCheckpoints/DebugInstallValidation/castle-formal-20261019.json";
            var sample = V24Save.Read(Output + "/day3-wake-hide.json");
            V24TestCheckpoints.Install(sample, target);
            string prior = File.ReadAllText(target);
            string backup = V24TestCheckpoints.Install(V24Save.Read(Output + "/day4-wake-hide-accept.json"), target);
            Check(File.ReadAllText(backup + "/original.json") == prior, "Independent original backup differs");
            Check(V24Save.Read(target).Day == 4, "Installed checkpoint did not persist");
        }

        private static bool AdvanceChoice(this V24Engine engine, int choice)
        {
            engine.Advance(choice);
            return engine.State.Choices.Contains(choice);
        }
    }
}

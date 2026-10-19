using System;
using System.Linq;
namespace Castle.V24
{
    public sealed partial class V24Engine
    {
        public string RoomName(int id)
        {
            var display = Data.RoomDisplayConfig.FirstOrDefault(x => x.Room == id);
            return display != null ? Need(display.RevealNeed) ? display.AfterName : display.BeforeName
                : Data.Room.FirstOrDefault(x => x.Id == id)?.Name ?? "古堡";
        }
        public bool ControlMapOpen()
        {
            var day = Data.ControlRoomPolicy.Days.FirstOrDefault(x => x.Day == State.Day);
            return day != null && day.MapEntry && Window(day.Start, day.End, Now);
        }
        public bool CanExitControl()
        {
            if (State.Room != Data.GameSettings.ControlRoom || State.SessionAction == 0) return true;
            return Need(Data.DialogueActionConfig.First(x => x.Id == State.SessionAction).ExitNeed);
        }
        public string ControlReason()
        {
            if (!CanExitControl()) return "请先完成本次调查，暂时不能离开";
            if (State.Day == 1) return "已找到仓库位置，第二天按信件指引进入";
            if (State.Day == 2 || State.Day == 4) return "请通过当天对白中的入口进入";
            return "开放时间 22:00—01:30";
        }
        public V24Action RequiredAction()
        {
            var ev = Data.EventById(State.ActiveEvent);
            if (ev == null) return null;
            var lines = Data.Lines(ev.Plot);
            if (State.Line != lines.Length - 1 || State.Line < 0) return null;
            return Data.DialogueActionConfig.FirstOrDefault(x => x.Required && x.SourceEvent == ev.Id && (Data.Formal || x.SourcePlot == lines[State.Line].Id) && State.ActionPending != x.Id && !State.ConsumedActions.Contains(x.Id));
        }
        public string ExecuteAction(int id)
        {
            if (Data.Formal)
            {
                var requested = Data.DialogueActionConfig.FirstOrDefault(x => x.Id == id && x.SourceEvent == State.ActiveEvent && State.Shown.Contains(x.SourcePlot));
                if (requested == null || requested.Day!=State.Day || State.ConsumedActions.Contains(id)) return "请在对应现场对白中使用入口";
                State.ActionPending = id;
                if (State.Line + 1 >= Data.Lines(Data.EventById(State.ActiveEvent).Plot).Length) Finish();
                Commit(); return "成功";
            }
            var action = RequiredAction();
            if (action == null || action.Id != id) return "只能在当前现场对白末句使用这个入口";
            var ev = Data.EventById(action.SourceEvent);
            if (State.Day != action.Day || !Need(action.Need) || !Window(action.Start, action.End, Now.AddMinutes(ev.Time))) return "当前不满足进入条件";
            if (id == Data.FinaleConfig.Action)
            {
                var missing = Data.FinaleConfig.RequiredEntries.Where(x => !Has(x)).Select(x => Data.EntryById(x).Name + "（" + string.Join("、", Data.EntryById(x).Source) + "）")
                    .Concat(Data.FinaleConfig.RequiredGoals.Where(x => !State.Goals.Contains(x)).Select(x => "未完成：" + Data.Event.First(e => e.Goal == x).Name))
                    .Concat(Data.FinaleConfig.MustExistRecords.Where(x => !Accessible(Data.RecordById(x))).Select(x => "录音尚不可用：" + x)).ToArray();
                if (missing.Length > 0) return "进入前还需调查：\n" + string.Join("\n", missing);
            }
            // pending 与源事件结算一起提交，读取后只续做转场，不重复时间和奖励。
            State.ActionPending = id; Finish(); ResumeAction(); return "成功";
        }
        public void ResumeAction()
        {
            if (State.ActionPending == 0 || State.ActiveEvent != 0 || State.RewardPopups.Count != 0) return;
            var action = Data.DialogueActionConfig.First(x => x.Id == State.ActionPending);
            if (Data.Formal) SetClock(action.EnterClock);
            State.Room = Data.GameSettings.ControlRoom; State.Page = "Recordings";
            State.ControlSession = Guid.NewGuid().ToString("N"); State.ControlEntered = State.Now;
            State.ControlCost = action.ExitMinutes; State.ControlReturn = action.ReturnRoom; State.ControlDay = State.Day;
            State.ControlMode = action.SessionMode; State.SessionAction = action.Id;
            State.ConsumedActions.Add(action.Id); State.ActionPending = 0;
            if (action.OnEnterEvent != 0) Start(action.OnEnterEvent, true);
            Commit();
        }
        public string SleepReason()
        {
            if (State.Day >= Data.SleepPolicy.LastDay || State.Finished) return "第四天需要完成调查，不能进入下一天";
            if (Busy || !CanExitControl()) return "请先完成当前对白或调查";
            if (!State.SleepPending && (State.Room != Data.GameSettings.HomeRoom || !Window(Data.SleepPolicy.Start, Data.SleepPolicy.Force, Now))) return "请在22:00后回房休息";
            var requirement = Data.SleepPolicy.Requirements.FirstOrDefault(x => x.Day == State.Day);
            if (requirement != null && (!Need(requirement.All) || (requirement.Any.Length > 0 && !requirement.Any.Any(x => Need(new[] { x }))))) return "当天必要调查尚未完成，暂时不能换日";
            return "";
        }
        public void ResumeSleep()
        {
            if (Data.Formal) { ResumeFormalSleep(); return; }
            if (State.SleepStage == "" || State.ActiveEvent != 0 || State.RewardPopups.Count > 0) return;
            var ghost = Data.SleepPolicy.Day3Ghost;
            if (State.Day == ghost.Day && !State.Goals.Contains(ghost.OnceGoal))
            {
                State.SleepStage = "ghost"; State.Now = Math.Max(State.Now, BaseDate.Add(TimeSpan.Parse(ghost.Start)).Ticks);
                Start(ghost.Event, true, false, true); Commit(); return;
            }
            long target = BaseDate.AddDays(1).Add(TimeSpan.Parse(Data.SleepPolicy.Wake)).Ticks;
            Expire(State.Now, target); State.Now = target; RegisterRecordings();
            State.Day++; State.DayBase = new DateTime(target).Date.Ticks;
            State.Room = Data.GameSettings.HomeRoom; State.ControlSession = ""; State.SessionAction = 0;
            State.SleepPending = false; State.SleepStage = ""; State.Page = "Explore"; Commit();
        }
    }
}

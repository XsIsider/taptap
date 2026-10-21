using System;
using System.Linq;

namespace Castle.V24
{
    public sealed partial class V24Engine
    {
        public int CanonicalEntry(int id) => Data.EntryVariantConfig.FirstOrDefault(x => x.Entries.Contains(id))?.ConditionAlias ?? id;
        public int BestEntry(int id)
        {
            var variants = Data.EntryVariantConfig.FirstOrDefault(x => x.Entries.Contains(id));
            return variants == null ? id : variants.Entries.LastOrDefault(State.Entries.Contains);
        }
        public bool VisibleEntry(int id) => BestEntry(id) == id;
        // 初始资料的姓名链接必须主动点击；不能通过结束对白或标记录音自动补领。
        public bool IsCharacterProfile(int id)
        {
            var entry = Data.EntryById(id);
            return Data.Formal && entry != null && entry.Type == "info" && entry.Name.EndsWith("初始资料");
        }
        public bool KnowsCharacter(int id) => State.Entries.Select(Data.EntryById).Any(x => x.Type == "info" && x.Owner.Contains(id) && (!Data.Formal || x.Name.EndsWith("初始资料")));

        /// <summary>人物已解锁且每种灵感均已收录；同种灵感任一版本有效，人物路径奖励不计入。</summary>
        public bool CharacterInspirationComplete(int character)
        {
            if (!KnowsCharacter(character)) return false;
            var pathRewards = Data.Puzzle.Where(x => x.Type == "person_path")
                .Select(x => Data.EventById(x.Success)).Where(x => x != null)
                .SelectMany(x => x.Reward).ToArray();
            var inspirations = Data.Entry.Where(x => x.Type == "info" && x.Owner.Contains(character)
                    && !IsCharacterProfile(x.Id) && !pathRewards.Contains(x.Id))
                .Select(x => CanonicalEntry(x.Id)).Distinct().ToArray();
            return inspirations.Length > 0 && inspirations.All(Has);
        }

        public int ImportRecords()
        {
            if (!Data.Formal || State.Room != Data.GameSettings.ControlRoom) return 0;
            foreach (var tape in State.Tapes.Where(x=>x.Record!=0))
            {
                if (State.Recordings.Any(x=>x.Id==tape.Record)) continue;
                State.Recordings.Add(new V24RecordingState {Id=tape.Record,Complete=true,Day=State.Day,TrueStart=State.Now,RawStart=tape.RawStart!=0?tape.RawStart:State.Now,CapturedOffset=tape.CapturedOffset});
            }
            int count=0;
            foreach (var record in Data.Record.Where(x=>Accessible(x)&&!State.ImportedRecords.Contains(x.Id)))
            {State.ImportedRecords.Add(record.Id);count++;}
            return count;
        }

        /// <summary>调查日内的绝对时刻，凌晨仍归属前一调查日；24:00 表示次日零点。</summary>
        public void SetClock(string clock)
        {
            if (string.IsNullOrEmpty(clock)) return;
            var time = clock == "24:00" ? TimeSpan.FromDays(1) : TimeSpan.Parse(clock);
            if (time.TotalHours < 10) time += TimeSpan.FromDays(1);
            State.Now = BaseDate.Add(time).Ticks;
            RegisterRecordings();
        }
        public void ApplyEffects(string trigger, int id)
        {
            foreach (var effect in Data.PlotEffectConfig.Where(x => x.Trigger == trigger &&
                (trigger == "record_complete" ? x.Record == id : trigger == "event_complete" ? x.Event == id : x.Plot == id)))
            {
                if (effect.Entry == 0 || effect.Kind != "grant_entry" && effect.Kind != "grant_item_quantity") continue;
                int count = Math.Max(1, effect.Quantity);
                for (int i = 0; i < count; i++) Grant(effect.Entry, trigger + ":" + id + ":" + effect.Entry + ":" + i);
            }
        }
        public void CompleteRecord(int id)
        {
            if (!State.HeardRecords.Contains(id)) State.HeardRecords.Add(id);
            ApplyEffects("record_complete", id);
            var gate = Data.PlaybackGates.FirstOrDefault(x => x.Event == State.PlaybackGate && x.Record == id);
            if (gate != null)
            {
                State.PlaybackGate = 0; State.DeferredEvent = gate.ThenEvent;
                SetClock(gate.WorldClockAfter);
            }
            if (id == 620025 && State.PendingPuzzle == 720030) ContinuePuzzleReward();
            Commit();
        }
        public bool ClosePlaybackGate()
        {
            var gate = Data.PlaybackGates.FirstOrDefault(x => x.Event == State.PlaybackGate);
            if (gate == null) return true;
            if (gate.MustFinish && !gate.AllowEarlyExit && !State.HeardRecords.Contains(gate.Record)) return false;
            CompleteRecord(gate.Record); EndChain(); return true;
        }
        private void FinishFormal(int choice)
        {
            var ev = Data.EventById(State.ActiveEvent);
            if (ev == null) return;
            if (!Done(ev))
            {
                foreach (var line in Data.Lines(ev.Plot).Where(x => State.Shown.Contains(x.Id)))
                {
                    ApplyEffects("plot_complete", line.Id);
                    foreach (string target in line.LinkTarget.Where(x => x.StartsWith("entry:")))
                    {
                        int entry = int.Parse(target.Substring(6));
                        if (!IsCharacterProfile(entry) && !Has(entry)) Grant(entry, "link:" + line.Id + ":" + entry);
                    }
                }
                foreach (int entry in ev.Reward)
                    if (!Data.PlotEffectConfig.Any(x => x.Trigger == "event_complete" && x.Event == ev.Id && x.Entry == entry))
                        Grant(entry, "event:" + ev.Id + ":" + entry);
                ApplyEffects("event_complete", ev.Id);
                if (ev.Goal != 0 && !State.Goals.Contains(ev.Goal)) State.Goals.Add(ev.Goal);
                State.CompletedEvents.Add(ev.Id);
                if (ev.Record != 0)
                {
                    var record = State.Recordings.First(x => x.Id == ev.Record); record.Complete = true;
                    ApplyEffects("record_complete", ev.Record);
                }
                if (ev.Time > 0) AdvanceTime(Now.AddMinutes(ev.Time).Ticks);
            }
            if (ev.Trigger != "reward") State.LastCompletedEvent = ev.Id;
            State.ActiveEvent = 0;
            var gate = Data.PlaybackGates.FirstOrDefault(x => x.Event == ev.Id);
            if (gate != null) State.PlaybackGate = ev.Id;
            else if (State.ActionPending == 0)
            {
                var route = Data.ConditionalRoutes.FirstOrDefault(x=>x.AfterEvent==ev.Id);
                State.DeferredEvent = choice != 0 ? Data.PlotChoice.First(x => x.Id == choice).NextEvent
                    : route != null && route.AfterEvent == ev.Id ? Has(route.IfEntry) ? route.ThenEvent : route.ElseEvent : ev.NextEvent;
                var group = CurrentConversationGroup();
                if (group != null && group.Events.Contains(State.DeferredEvent)) State.DeferredEvent = 0;
            }
            EndChain(); Commit();
        }
        public V24ConversationGroup CurrentConversationGroup() => !Data.Formal ? null : Data.ConversationGroups.FirstOrDefault(x =>
            x.Day == State.Day && State.CompletedEvents.Contains(x.AfterEvent) &&
            (x.RequiredEvent == 0 || State.CompletedEvents.Contains(x.RequiredEvent)) && x.Events.Any(id => !Done(Data.EventById(id))));

        /// <summary>第二、三天下午通过地图到达对应房间后触发调查，不使用跨房间快捷入口。</summary>
        public bool IsRoomBasedActivity => Data.Formal
            && (State.Activity == "day2_afternoon" || State.Activity == "day3_afternoon");

        public bool CanSelectFormalEvent(int id)
        {
            if (Busy || State.PlaybackGate != 0) return false;
            var group = CurrentConversationGroup();
            if (group != null && group.Events.Contains(id))
            {
                int required = group.Ordered.FirstOrDefault(x => !Done(Data.EventById(x)));
                return !Done(Data.EventById(id)) && (required == 0 || required == id);
            }
            var activity = Data.DayActivities.FirstOrDefault(x => x.Id == State.Activity);
            return activity != null && activity.Events.Contains(id) && !Done(Data.EventById(id)) && ActivityEventAvailable(id)
                && (!IsRoomBasedActivity || Data.EventById(id).Room == State.Room);
        }
        private bool ActivityEventAvailable(int id) => id != 920072 || Has(300004);
        private void TryStartRoomActivity()
        {
            if (!IsRoomBasedActivity || Busy || State.SleepPending) return;
            var activity = Data.DayActivities.First(x => x.Id == State.Activity);
            int next = activity.Events.FirstOrDefault(CanSelectFormalEvent);
            if (next != 0) SelectFormalEvent(next);
        }
        public bool SelectFormalEvent(int id)
        {
            if (!CanSelectFormalEvent(id)) return false;
            var group = CurrentConversationGroup();
            if (group != null)
            {
                int index = Array.IndexOf(group.Ordered, id);
                if (index >= 0 && index < group.Clocks.Length) SetClock(group.Clocks[index]);
            }
            var ev = Data.EventById(id); if (ev.Room != 0) { State.Room = ev.Room; State.SceneContext=""; }
            return Start(id, true);
        }
        public bool WaitForActivity()
        {
            var activity = Data.DayActivities.FirstOrDefault(x => x.Id == State.Activity);
            if (activity == null || !activity.AllowWaitToFinish || Busy
                || IsRoomBasedActivity && State.Room != Data.GameSettings.HomeRoom) return false;
            FinishActivity(activity); EndChain(); Commit(); return true;
        }
        private void FinishActivity(V24DayActivity activity)
        {
            SetClock(activity.FinishClock ?? activity.FinishClockMin);
            State.Activity = ""; State.DeferredEvent = activity.NextEvent;
        }
        private void ResumeFormal()
        {
            if (Busy || State.PlaybackGate != 0 || State.SleepStage != "") return;
            int last = State.LastCompletedEvent;
            if (last == 920013 || last == 920052 || last == 920056)
            {
                State.Room = Data.GameSettings.HomeRoom; State.SleepPending = true;
                State.WakeEvent = last == 920013 ? 920014 : last == 920052 ? 920057 : 920058;
                State.LastCompletedEvent = 0; State.SleepStage = "blackout"; State.Page = "Cutscene"; return;
            }
            var activity = Data.DayActivities.FirstOrDefault(x => x.Id == State.Activity);
            if (activity != null)
            {
                if (activity.Events.Contains(last) && activity.TimePerEvent > 0 && Data.EventById(last).Time == 0)
                { AdvanceTime(Now.AddMinutes(activity.TimePerEvent).Ticks); State.LastCompletedEvent = 0; }
                // 允许回房等待的活动保留探索；其余活动仍按原条件自动结算。
                bool waitForRest = IsRoomBasedActivity && activity.AllowWaitToFinish;
                if (!waitForRest && activity.Events.Where(ActivityEventAvailable).All(x => Done(Data.EventById(x)))) FinishActivity(activity);
            }
            else
            {
                // 0 表示没有待结算事件，不能匹配未配置 AfterEvent 的活动。
                activity = Data.DayActivities.FirstOrDefault(x => last != 0 && (x.AfterEvent == last || x.AfterAnyEvent.Contains(last)));
                if (activity != null) { State.Activity = activity.Id; SetClock(activity.StartClock); State.LastCompletedEvent = 0; }
            }
            if (State.DeferredEvent != 0)
            {
                // 先等待玩家实际使用道具或选房，再执行原 StoryFlow 的地点与时间。
                var interaction = Data.DialogueInteractionGateConfig.FirstOrDefault(x =>
                    x.SourceEvent == last && x.ResumeEvent == State.DeferredEvent);
                if (interaction != null)
                {
                    if (!interaction.CompletionConditions.All(InteractionCondition)) return;
                    if (!State.ConsumedActions.Contains(interaction.Id)) State.ConsumedActions.Add(interaction.Id);
                }
                int next = State.DeferredEvent; State.DeferredEvent = 0;
                var step = Data.StoryFlow.FirstOrDefault(x => x.FromEvent == last && x.ToEvent == next);
                if (step != null)
                {
                    SetClock(step.SetClock); State.SceneContext = step.SceneContext ?? "";
                    if (step.ToRoom != 0) State.Room = step.ToRoom;
                }
                else if (Data.EventById(next).Room != 0) State.Room = Data.EventById(next).Room;
                var reply = State.Transcript.LastOrDefault();
                bool keepText = reply != null && reply.Choice != 0
                    && Data.PlotChoice.Any(x => x.Id == reply.Choice && x.NextEvent == next);
                Start(next, true, keepText);
            }
        }
        private void ResumeFormalSleep()
        {
            if (State.SleepStage == "" || State.ActiveEvent != 0 || State.RewardPopups.Count > 0) return;
            if (State.Day == 3 && State.Now >= BaseDate.AddHours(23).Ticks && !State.CompletedEvents.Contains(920076))
            { State.SleepStage = "ghost"; Start(920076, true, false, true); Commit(); return; }
            int wake = State.WakeEvent != 0 ? State.WakeEvent : 920077;
            State.Day++; State.DayBase = BaseDate.AddDays(1).Ticks;
            State.Now = BaseDate.AddHours(wake == 920014 ? 11.5 : wake == 920058 ? 11 : 10).Ticks;
            State.Room = Data.GameSettings.HomeRoom; State.SleepPending = false; State.SleepStage = "";
            State.ControlSession = ""; State.SessionAction = 0; State.WakeEvent = 0; State.Page = "Explore";
            RegisterRecordings(); Start(wake, true); Commit();
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Castle.V24;
using UnityEngine;

namespace Castle.Editor
{
    public static class V24FormalValidation
    {
        private static readonly List<string> Results = new List<string>();
        private static void Check(bool condition,string label)
        {if(!condition)throw new Exception("正式规则验证失败："+label);Results.Add(label);}
        public static void Run(V24Data data)
        {
            Results.Clear();Check(data.Formal,"正式配置已识别");
            Check(data.Room.All(x=>x.Floor>=1&&x.Floor<=2),"仅两层地图");
            Check(data.Plot.Length==1155,"1155句对白完整导入");
            ValidateProfileLinks(data);
            ValidateDialogueItemLinks(data);
            foreach(var group in data.ConversationGroups)
                Check(group.Events.All(id=>data.EventById(id)!=null),"选人事件存在："+group.Id);
            for(int branch=0;branch<8;branch++)Walk(data,branch);
            Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/formal-validation.txt",Results);
            Debug.Log("FORMAL_VALIDATION_OK "+Results.Count);
        }
        private static void ValidateProfileLinks(V24Data data)
        {
            var engine = new V24Engine(data, new V24State());
            engine.NewGame();
            foreach (var profile in data.Entry.Where(x => engine.IsCharacterProfile(x.Id)))
            {
                engine = new V24Engine(data, new V24State());
                engine.NewGame();
                var links = data.Plot.Where(x => x.LinkTarget.Contains("entry:" + profile.Id)).ToArray();
                Check(links.Length > 0, "初始资料有姓名入口：" + profile.Id);
                Check(!data.Event.Any(x => x.Reward.Contains(profile.Id)), "事件不自动奖励初始资料：" + profile.Id);
                engine.Claim(links[0].Id, profile.Id);
                Check(!engine.Has(profile.Id), "未显示姓名不能领取：" + profile.Id);
                engine.State.Shown.Add(links[0].Id);
                engine.State.Room = data.GameSettings.ControlRoom;
                engine.ToggleMark(links[0].Id);
                Check(!engine.Has(profile.Id), "标记录音不解锁人物：" + profile.Id);
                engine.Claim(links[0].Id, profile.Id);
                Check(engine.Has(profile.Id) && profile.Owner.All(engine.KnowsCharacter), "点击姓名解锁人物：" + profile.Id);
                int count = engine.State.RewardPopups.Count;
                foreach (var link in links) { engine.State.Shown.Add(link.Id); engine.Claim(link.Id, profile.Id); }
                Check(engine.State.RewardPopups.Count == count, "重复姓名不重复奖励：" + profile.Id);
            }
            engine = new V24Engine(data, new V24State());
            engine.NewGame();
            engine.Start(920003, true, validated: true);
            for (int i=0; i<500 && engine.State.ActiveEvent==920003; i++)
            {
                if (engine.State.RewardPopups.Count > 0) engine.CloseReward();
                else if (engine.PendingDialogueItem() != 0)
                {
                    int item = engine.PendingDialogueItem();
                    var line = data.Plot.First(x => engine.State.Transcript.Any(row => row.Plot == x.Id) && x.LinkTarget.Contains("entry:" + item));
                    engine.Claim(line.Id, item);
                }
                else engine.Advance(0);
            }
            Check(engine.State.CompletedEvents.Contains(920003), "管家接待实际完成");
            Check(!engine.KnowsCharacter(200003), "接待结束不自动补领管家档案");
        }
        private static void ValidateDialogueItemLinks(V24Data data)
        {
            foreach (var line in data.Plot)
            {
                foreach (string target in line.LinkTarget.Where(x => x.StartsWith("entry:")))
                {
                    int id = int.Parse(target.Substring(6));
                    var entry = data.EntryById(id);
                    if (entry.Type != "item" && entry.Type != "tape") continue;
                    var ev = data.Event.First(x => x.Plot == line.Group);
                    var engine = new V24Engine(data, new V24State());
                    engine.NewGame(); engine.Start(ev.Id, true, validated: true);
                    var state = engine.State;
                    state.Entries.Remove(id); state.Transcript.Clear(); state.Shown.Clear(); state.RewardPopups.Clear();
                    state.Line = Array.FindIndex(data.Lines(ev.Plot), x => x.Id == line.Id) - 1;
                    state.Auto = true; engine.Reveal();
                    Check(engine.PendingDialogueItem() == id && !state.Auto, "新道具出现暂停自动：" + line.Id);
                    int before = state.Line;
                    engine.Advance(); engine.Finish();
                    Check(state.Line == before && state.ActiveEvent == ev.Id && !engine.Has(id), "未收集道具不推进不补领：" + line.Id);
                    Check(engine.ExecuteAction(0) != "成功" && state.ActionPending == 0, "动作不能跳过道具：" + line.Id);
                    var restored = new V24Engine(data, JsonUtility.FromJson<V24State>(JsonUtility.ToJson(state)));
                    Check(restored.PendingDialogueItem() == id, "读档保留道具阻塞：" + line.Id);
                    engine.Claim(line.Id, id);
                    Check(engine.Has(id) && engine.PendingDialogueItem() == 0, "点击领取后解除：" + line.Id);
                    int rewards = state.RewardPopups.Count;
                    engine.Claim(line.Id, id);
                    Check(state.RewardPopups.Count == rewards, "重复点击不重复奖励：" + line.Id);
                    state.Claims.Clear();
                    Check(engine.PendingDialogueItem() == 0, "已有道具无需再次点击：" + line.Id);
                }
            }
            var profileLine = data.Plot.First(x => x.LinkTarget.Any(target => target.StartsWith("entry:") && data.EntryById(int.Parse(target.Substring(6))).Type == "info"));
            var profileEvent = data.Event.First(x => x.Plot == profileLine.Group);
            var profileState = new V24State { ActiveEvent = profileEvent.Id };
            profileState.Transcript.Add(new V24Transcript { Plot = profileLine.Id });
            // 同一句可能同时含人物和道具链接，只领取道具以单独验证人物不会阻塞。
            foreach (string target in profileLine.LinkTarget.Where(x => x.StartsWith("entry:")))
            {
                int id = int.Parse(target.Substring(6));
                if (data.EntryById(id).Type == "item" || data.EntryById(id).Type == "tape") profileState.Entries.Add(id);
            }
            Check(new V24Engine(data, profileState).PendingDialogueItem() == 0, "人物信息不阻塞对白");
        }
        private static void Walk(V24Data data,int branch)
        {
            bool report=(branch&1)!=0,muffin=(branch&2)!=0,ghost=(branch&4)!=0;
            var engine=new V24Engine(data,new V24State());engine.NewGame();engine.State.InvitationStage=2;
            engine.Start(data.GameSettings.ArrivalEvent,true);
            var history=new List<string>();
            for(int step=0;step<10000;step++)
            {
                if(step%37==0)engine.State=JsonUtility.FromJson<V24State>(JsonUtility.ToJson(engine.State));
                var state=engine.State;
                if(state.Finished){Check(state.CompletedEvents.Contains(920076)==ghost,"遇鬼时段正确");Check(true,"完整通关 report="+report+" muffin="+muffin+" ghost="+ghost);return;}
                if(state.RewardPopups.Count>0){engine.CloseReward();continue;}
                if(state.ActiveEvent!=0)
                {
                    var ev=data.EventById(state.ActiveEvent);
                    if(state.Line==0)history.Add(ev.Id.ToString());
                    foreach (var line in data.Lines(ev.Plot).Where(x => state.Shown.Contains(x.Id)))
                        foreach (string target in line.LinkTarget.Where(x => x.StartsWith("entry:")))
                            engine.Claim(line.Id, int.Parse(target.Substring(6)));
                    if(engine.RequiredAction()!=null){engine.ExecuteAction(engine.RequiredAction().Id);continue;}
                    int choice=0;
                    if(state.Line+1>=data.Lines(ev.Plot).Length && ev.ChoiceGroup!=0)
                    {
                        var options=data.PlotChoice.Where(x=>x.Group==ev.ChoiceGroup).ToArray();
                        int target=ev.Id==920050?(report?920053:920051):ev.Id==920067?(muffin?920068:920070):0;
                        choice=(target==0?options[0]:options.First(x=>x.NextEvent==target)).Id;
                    }
                    engine.Advance(choice);continue;
                }
                if(state.SleepStage!=""){engine.ResumeSleep();continue;}
                if(state.PlaybackGate!=0)
                {
                    var gate=data.PlaybackGates.First(x=>x.Event==state.PlaybackGate);
                    if(gate.AllowEarlyExit)Check(engine.ClosePlaybackGate(),"教学磁带提前退出继续剧情");
                    else {Check(!engine.ClosePlaybackGate(),"维修教学必须完整播放");engine.CompleteRecord(gate.Record);engine.EndChain();}
                    continue;
                }
                if(state.PendingPuzzle!=0)
                {
                    if(state.PendingPuzzle==720030){Check(!state.Puzzles.Contains(720030),"真相播放前不结算时间线");engine.CompleteRecord(620025);}
                    else engine.ContinuePuzzleReward();
                    continue;
                }
                var group=engine.CurrentConversationGroup();
                if(group!=null)
                {
                    int required=group.Ordered.FirstOrDefault(id=>!engine.Done(data.EventById(id)));
                    int wrong=group.Events.Last();
                    if(required!=0)Check(!engine.SelectFormalEvent(wrong),"错误人物阻止："+required);
                    int next=required!=0?required:group.Events.Last(id=>!engine.Done(data.EventById(id)));
                    Check(engine.SelectFormalEvent(next),"选人成功："+next);continue;
                }
                if(state.Activity!="")
                {
                    var activity=data.DayActivities.First(x=>x.Id==state.Activity);
                    if (engine.IsRoomBasedActivity)
                    {
                        int investigation = activity.Events.FirstOrDefault(id => !engine.Done(data.EventById(id)));
                        int destination = investigation != 0 ? data.EventById(investigation).Room : data.GameSettings.HomeRoom;
                        Check(engine.Move(destination), "下午调查通过地图到场：" + destination);
                        if (investigation != 0) Check(state.ActiveEvent == investigation, "到场触发下午对白：" + investigation);
                        else Check(engine.WaitForActivity(), "回房休息后进入晚宴");
                        continue;
                    }
                    int next=activity.Events.FirstOrDefault(engine.CanSelectFormalEvent);
                    if(next!=0){engine.SelectFormalEvent(next);continue;}
                    if(engine.WaitForActivity())continue;
                }
                if(state.Room==data.GameSettings.ControlRoom)
                {
                    // 按玩家可访问文件标记证据，禁止直接赠送通关线索。
                    foreach(var record in data.Record.Where(engine.Accessible).ToArray())
                    {
                        foreach(var line in data.Lines(record.Plot))
                        {if(!state.Shown.Contains(line.Id))state.Shown.Add(line.Id);if(!state.Marked.Contains(line.Id))engine.ToggleMark(line.Id);}
                    }
                    if(state.RewardPopups.Count>0)continue;
                    var puzzle=data.Puzzle.FirstOrDefault(x=>x.Type!="timeline"&&x.Type!="person_path"&&!state.Puzzles.Contains(x.Id)&&engine.Need(x.Need));
                    if(puzzle!=null){Solve(engine,puzzle);continue;}
                    if(state.Day==4&&!state.Puzzles.Contains(720030))
                    {
                        var timeline=data.PuzzleById(720030);
                        Check(engine.Need(timeline.Need),"结局线索可达，松饼="+muffin+" 缺少="+string.Join(",",timeline.Need.Where(n=>!engine.Need(new[]{n}))));
                        Check(!state.Puzzles.Contains(720020),"人物路径不是时间线前置");Solve(engine,timeline);continue;
                    }
                    if(state.Day==4&&engine.Need(data.PuzzleById(720020).Need)&&!state.Puzzles.Contains(720020)){Solve(engine,data.PuzzleById(720020));continue;}
                    Check(engine.Move(state.ControlReturn),"中控室可退出 day="+state.Day);continue;
                }
                if(state.Day==3&&state.Activity=="")
                {state.Room=data.GameSettings.HomeRoom;engine.SetClock(ghost?"23:00":"22:00");engine.Sleep();continue;}
                engine.Scan();
                if(state.ActiveEvent!=0||state.RewardPopups.Count>0||state.SleepStage!="")continue;
                throw new Exception("流程停滞 day="+state.Day+" room="+state.Room+" last="+state.LastCompletedEvent+" activity="+state.Activity+" history="+string.Join(",",history));
            }
            throw new Exception("流程循环超限："+string.Join(",",history));
        }
        private static void Solve(V24Engine engine,V24Puzzle puzzle)
        {
            var draft=engine.Draft(puzzle.Id);draft.Values=puzzle.Answer.Split('|').Select(x=>int.Parse(x.Contains("=")?x.Split('=')[1]:x)).ToList();
            if(puzzle.Type=="timeline")draft.Values=draft.Values.Select(engine.BestEntry).ToList();
            if(puzzle.Type=="anchor_align")
            {
                draft.Reference=draft.Values[0];var plot=engine.Data.Plot.First(x=>x.Id==draft.Values[1]);
                var record=engine.Data.Record.First(x=>x.Plot==plot.Group);draft.Shift=-engine.Data.DeviceConfig.First(x=>x.Id==record.Device).Offset;
            }
            if(puzzle.Type=="sound_path")draft.Doors=new List<string>{"open","closed","closed"};
            string result=engine.ValidatePuzzle(puzzle.Id);Check(result=="成功","验证谜题 "+puzzle.Id+"："+result);
        }
    }
}

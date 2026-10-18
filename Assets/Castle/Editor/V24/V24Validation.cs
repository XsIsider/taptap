using System;
using System.IO;
using System.Linq;
using System.Text;
using Castle.V24;
using UnityEditor;
using UnityEngine;
namespace Castle.Editor
{
    public static class V24Validation
    {
        private static StringBuilder _log;
        private static int _checks, _artChoice = 1;
        private static bool _report = true, _food = true;
        private static void Check(bool pass,string message){_log.AppendLine((pass?"PASS ":"FAIL ")+message);_checks++;if(!pass)throw new Exception(message);}
        [MenuItem("Tools/Castle/V2.4/Validate Rules And Main Route")]
        public static void Run()
        {
            _log=new StringBuilder();_checks=0;Directory.CreateDirectory("Logs");
            try
            {
                var data=V24Data.Load();var engine=new V24Engine(data,new V24State());
                for(int branch=0;branch<12;branch++)
                {
                _artChoice=branch%3+1;_report=(branch/3)%2==0;_food=branch<6;
                engine=new V24Engine(data,new V24State());engine.NewGame();
                Check(data.Plot.Length==749 && data.Event.Length==106 && data.Room.Length==18,"新版八表加载数量");
                Check(data.Room.All(x=>x.Floor==1||x.Floor==2)&&data.Room.Count(x=>x.Floor==1)==9,"双层地图每层九个房间");
                Check(!engine.State.KnownDevices.Contains(1100005),"未知设备不提前揭晓");
                Check(engine.RoomName(100003)=="仓库","发现前不提前显示中控室");
                engine.Grant(data.InvitationEntry,"invitation",false);
                foreach(var row in File.ReadAllLines("SourceData/V24TwoFloors/"+(_report?"主线测试路线.csv":"四日主线测试路线_隐瞒分支.csv")).Skip(1))
                {
                    var cells=row.Split(',');if(cells.Length<6)continue;
                    string table=cells[3],idText=cells[4];
                    if(table=="Event")
                    {
                        int id=int.Parse(idText);
                        if(id==900101)id=900100+_artChoice;
                        if(id==900107&&!_report)id=900106;
                        if(id==900115&&!_report)id=900114;
                        if(id==900126&&!_food)id=900127;
                        if(!_report&&id>=900109&&id<=900113)continue;
                        if(!_food&&(id==900030||id==900032))continue;
                        var ev=data.EventById(id);
                        if(ev.Trigger=="reward"||engine.Done(ev))continue;
                        if(id==900130){if(engine.State.Room!=data.GameSettings.HomeRoom)Check(engine.Move(data.GameSettings.HomeRoom),"返回客卧准备遇鬼过场");engine.Sleep();engine.ResumeSleep();}
                        if(ev.Room!=0&&engine.State.Room!=ev.Room)
                        {Check(engine.Move(ev.Room),"移动到事件房间 "+id);}
                        if(engine.State.ActiveEvent!=id)
                        {
                            Check(engine.State.ActiveEvent==0,"事件入口未被其他对白阻塞 "+id+" active="+engine.State.ActiveEvent);
                            if(ev.Trigger=="item_use")Check(engine.UseItem(ev.UseEntry)=="使用成功","道具触发 "+id);
                            else Check(engine.Start(id,true),"事件可触发 "+id+" time="+engine.Now);
                        }
                        if(id==900030&&_food)Check(engine.UseItem(300028)=="使用成功","对白内道具排队");
                        FinishOne(engine,id);
                        if(id==data.GameSettings.TapeTutorialEvent)
                        {
                            Check(!engine.Done(ev),"引导等待打开磁带后关闭");
                            engine.TapeCompleted(engine.State.Tapes.First(x=>x.Entry==data.TutorialTape).Instance);
                            Drain(engine);
                        }
                        Check(engine.Done(ev),"事件完成 "+id);
                        if(id==900129)Check(engine.RoomName(100003)=="中控室","发现剧情结束后名称揭晓");
                    }
                    else if(table=="Puzzle")
                    {
                        int id=int.Parse(idText);
                        if(branch>=6 && (id==700010 || id==700011))id=id==700010?700011:700010;
                        var puzzle=data.PuzzleById(id);
                        if(puzzle.Type!="timeline"&&engine.State.Room!=data.GameSettings.ControlRoom)
                        {Check(engine.Move(data.GameSettings.ControlRoom),"进入中控室");if(engine.State.ActiveEvent!=0)FinishOne(engine,engine.State.ActiveEvent);}
                        ReadAccessible(engine);
                        var draft=engine.Draft(id);draft.Values=puzzle.Answer.Split('|').Select(x=>int.Parse(x.Contains("=")?x.Split('=')[1]:x)).ToList();
                        if(puzzle.Type=="anchor_align"){draft.Reference=500125;draft.Shift=180;}
                        string result=engine.ValidatePuzzle(id);Check(result=="成功","谜题 "+id+" "+result);
                        if(branch==0 && puzzle.Type=="anchor_align")
                        {
                            var resumed=new V24Engine(data,JsonUtility.FromJson<V24State>(JsonUtility.ToJson(engine.State)));
                            Check(!resumed.State.Puzzles.Contains(id)&&resumed.State.Repaired.Contains(1100011),"校时修复与待结算同存，谜题未提前完成");
                            resumed.ContinuePuzzleReward();while(resumed.State.ActiveEvent!=0)FinishOne(resumed,resumed.State.ActiveEvent);Drain(resumed);
                            Check(resumed.State.Puzzles.Contains(id)&&resumed.State.Repaired.Count==1,"恢复奖励链不重复修复设备");
                        }
                        engine.ContinuePuzzleReward();while(engine.State.ActiveEvent!=0)FinishOne(engine,engine.State.ActiveEvent);Drain(engine);
                        Check(engine.State.Puzzles.Contains(id),"奖励链完成后提交谜题 "+id);
                    }
                    else if(table=="Action")
                    {
                        Check(engine.State.ConsumedActions.Contains(int.Parse(idText)),"对白跳转已提交 "+idText);
                    }
                    else if(table=="ControlRoom")
                    {
                        if(idText=="3"&&engine.State.Room!=data.GameSettings.ControlRoom)Check(engine.Move(data.GameSettings.ControlRoom),"第三夜常规进入");
                        else Check(engine.Move(idText=="3"?data.GameSettings.HomeRoom:engine.State.ControlReturn),"按会话规则退出 "+idText);
                    }
                    else if(table=="SleepPolicy")
                    {
                        int expected=int.Parse(cells[5].Substring(1,1));
                        if(engine.State.Day<expected){if(engine.State.SleepStage==""){if(engine.State.Room!=data.GameSettings.HomeRoom)Check(engine.Move(data.GameSettings.HomeRoom),"返回客卧");engine.Sleep();}engine.ResumeSleep();}
                        Check(engine.State.Day==expected&&engine.Now.Hour==10,"睡眠只换日一次 "+expected);
                    }
                }
                Check(engine.State.Finished && engine.State.Puzzles.Count==12,"分支 "+branch+" 完成12题与未完待续");
                }
                V24Save.Write(engine.State,"Logs/v24-test-save.json");var restored=V24Save.Read("Logs/v24-test-save.json");Check(restored.Finished&&restored.Day==4,"独立存档往返");
                var boundary=new V24Engine(data,new V24State());boundary.NewGame();boundary.State.Now=new DateTime(boundary.State.DayBase).AddDays(1).AddHours(1).Ticks;
                Check(boundary.Window("22:00","01:30",boundary.Now),"跨午夜时窗");Check(boundary.State.Day==1,"午夜不换调查日");
                Check(!boundary.CanEnter(data.GameSettings.ControlRoom),"第一日禁止进入仓库");
                boundary.State.Room=data.GameSettings.HomeRoom;boundary.Sleep();Check(boundary.State.Day==1&&boundary.State.SleepStage=="","缺少日终剧情不得换日");
                boundary.State.Goals.Add(1900073);boundary.Sleep();boundary.ResumeSleep();Check(boundary.State.Day==2&&boundary.Now.Hour==10,"凌晨休息不多跳一天");
                engine.Sleep();Check(engine.State.Day==4,"终章不产生第五日");
                _log.AppendLine("PASS TOTAL "+_checks);Debug.Log("V24 VALIDATION PASS "+_checks);
            }
            finally{File.WriteAllText("Logs/v24-validation.txt",_log.ToString());}
        }
        private static void Drain(V24Engine engine){while(engine.State.RewardPopups.Count>0)engine.CloseReward();}
        private static void FinishOne(V24Engine engine,int id)
        {
            var ev=engine.Data.EventById(id);int guard=0;
            while(engine.State.ActiveEvent==id && guard++<200)
            {
                var lines=engine.Data.Lines(ev.Plot);
                if(engine.State.Line>=0)
                {
                    var line=lines[engine.State.Line];foreach(var link in line.LinkTarget.Where(x=>x.StartsWith("entry:")))engine.Claim(line.Id,int.Parse(link.Substring(6)));
                }
                Drain(engine);
                var action=engine.RequiredAction();
                if(action!=null)
                {
                    if(action.Id==engine.Data.FinaleConfig.Action)
                    {
                        var blocked=new V24Engine(engine.Data,JsonUtility.FromJson<V24State>(JsonUtility.ToJson(engine.State)));
                        blocked.State.Entries.Remove(engine.Data.FinaleConfig.RequiredEntries[0]);long before=blocked.State.Now;
                        Check(blocked.ExecuteAction(action.Id)!="成功"&&blocked.State.ActiveEvent==id&&blocked.State.Now==before&&blocked.State.ActionPending==0,"终章预检缺项不消费动作或时间");
                    }
                    Check(engine.ExecuteAction(action.Id)=="成功","必点末句跳转 "+action.Id);
                    var saved=new V24Engine(engine.Data,JsonUtility.FromJson<V24State>(JsonUtility.ToJson(engine.State)));long settled=saved.State.Now;
                    Drain(saved);saved.ResumeAction();
                    Check(saved.State.Room==engine.Data.GameSettings.ControlRoom&&saved.State.Now==settled&&saved.State.ConsumedActions.Count(x=>x==action.Id)==1,"动作结算后读档接续不重复时间或入口");
                    Drain(engine);
                    Check(!engine.CanExitControl(),"会话完成前禁止退出 "+action.Id);
                    continue;
                }
                int choice=engine.State.Line+1>=lines.Length?engine.Data.PlotChoice.Where(x=>x.Group==ev.ChoiceGroup&&ev.ChoiceGroup!=0).OrderBy(x=>ev.ChoiceGroup==410003 ? (x.Order==_artChoice?0:1) : ev.ChoiceGroup==410004 ? (x.Order==(_report?2:1)?0:1) : ev.ChoiceGroup==410005 ? (x.Order==(_food?1:2)?0:1) : x.Order).FirstOrDefault()?.Id??0:0;
                if(choice!=0 && engine.State.QueuedItem==0)
                {
                    V24Save.Write(engine.State,"Logs/v24-choice-test.json");
                    var restored=new V24Engine(engine.Data,V24Save.Read("Logs/v24-choice-test.json"));
                    restored.Advance();
                    Check(restored.State.ActiveEvent==id && restored.State.Line==engine.State.Line && restored.State.Choices.Count==engine.State.Choices.Count,"选项存档读取后仍等待选择 "+id);
                }
                engine.Advance(choice);
            }
            Drain(engine);
        }
        private static void ReadAccessible(V24Engine engine)
        {
            foreach(var record in engine.Data.Record.Where(engine.Accessible).ToArray())
            foreach(var line in engine.Data.Lines(record.Plot))
            {
                if(!engine.State.Shown.Contains(line.Id))engine.State.Shown.Add(line.Id);
                foreach(var link in line.LinkTarget.Where(x=>x.StartsWith("entry:")))engine.Claim(line.Id,int.Parse(link.Substring(6)));
                engine.Mark(line.Id);
                foreach(var entry in engine.Data.Entry.Where(x=>(x.Type=="info"||x.Type=="clue")&&x.Source.Contains("plot:"+line.Id)))engine.Grant(entry.Id,"extract:"+entry.Id,false);
            }
            Drain(engine);
        }
    }
}

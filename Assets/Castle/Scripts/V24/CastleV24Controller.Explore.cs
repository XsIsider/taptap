using System;
using System.Collections;
using System.Linq;
using Castle.Flow;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Castle.V24
{
    public sealed partial class CastleV24Controller
    {
        private static readonly int[] GuestRooms={100001,100012,100013,100015,100024,100025,100026,100027,100028,100029,100030};
        private void Explore()
        {
            var v=V("Explore"); Show(v,"RoomName",false);
            SetChoiceSpace(v, false);
            // 仅替换有明确对应素材的场景，其他房间恢复原背景，避免沿用上一房间的图。
            bool talk=S.ActiveEvent!=0;
            bool hasText=talk || S.Transcript.Count>0;
            var receptionGroups=_data.Event.Where(x=>x.Id==900059||x.Id==900060||x.Id==900061).Select(x=>x.Plot).ToArray();
            bool reception=S.Room==100002 && (talk?receptionGroups.Contains(_data.EventById(S.ActiveEvent).Plot):S.Transcript.Any(x=>x.Plot!=0&&receptionGroups.Contains(_data.Plot.First(p=>p.Id==x.Plot).Group)));
            if (_data.Formal) reception = S.SceneContext.Contains("走廊");
            SceneBackground(reception);
            bool freshSelection = !talk && ConversationAvailable() && !CurrentChoices().Any(x => Engine.Done(_data.EventById(x.Event)));
            if (freshSelection) hasText = false;
            Show(v,"DialoguePanel",hasText); Show(v,"FreePanel",!talk); Show(v,"ExitTape",false);
            Txt(v,"RoomHint","选择在场人物或房间热点进行调查。没有可用对白时保持自由探索。");
            Btn(v,"Talk",()=>Toast("点击左侧人物或调查入口"),"调查入口见左侧");
            Btn(v,"Map",()=>Map(),enabled:Engine.Has(_data.GameSettings.MapEntry));
            Btn(v,"ExploreRoom",Rest,"休息",S.Room==_data.GameSettings.HomeRoom && !Engine.Busy);
            Btn(v,"Workbench",()=>Page("Recordings"),"录音工作台",S.Room==_data.GameSettings.ControlRoom && !Engine.Busy);
            Show(v,"Talk",false);Show(v,"Map",false);Show(v,"RoomHint",false);
            Show(v,"ExploreRoom",!talk&&S.Room==_data.GameSettings.HomeRoom);Show(v,"Workbench",!talk&&S.Room==_data.GameSettings.ControlRoom);
            var people=v.Get<RectTransform>("People");Clear(people);
            if (_data.Formal && !talk && !ConversationAvailable())
            {
                var activity = _data.DayActivities.FirstOrDefault(x => x.Id == S.Activity);
                if (activity != null)
                {
                    foreach (int id in activity.Events.Where(Engine.CanSelectFormalEvent))
                    {
                        int local = id; var ev = _data.EventById(id);
                        var npc = _data.Lines(ev.Plot).FirstOrDefault(x => x.SpeakerId != 0 && x.SpeakerId != 200001);
                        Card(people, Engine.CharacterName(npc?.SpeakerId ?? 0), Engine.RoomName(ev.Room), () => { Engine.SelectFormalEvent(local); Refresh(); });
                    }
                    if (activity.AllowWaitToFinish) Card(people,"等待晚宴","结束本阶段调查",()=>{Engine.WaitForActivity();Refresh();});
                }
            }
            var hotspots=_data.HotspotBinding.Where(x=>x.Room==S.Room && (x.Day==0||x.Day==S.Day) && Engine.Need(x.Need) && Engine.Window(x.Start,x.End,Engine.Now)).ToList();
            // 以事件有效入口补充测试包中时间条件不同的复用热点，避免已有入口被静态绑定挡住。
            var events=_data.Event.Where(x=>x.Trigger=="interact" && Engine.Eligible(x)).ToArray();
            foreach(var ev in events.Where(x=>!talk && !IsConversationChoice(x)))
            {
                var local=ev;
                Card(people,ev.Name,"点击调查",()=>{if(!Engine.Busy){Engine.Start(local.Id);Refresh();}}).Get<Button>("Select").interactable=!talk;
            }
            int[] participants = talk
                ? _data.Lines(_data.EventById(S.ActiveEvent).Plot).Where(x => x.SpeakerId != 0 && x.SpeakerId != 200001).Select(x => x.SpeakerId).Distinct().ToArray()
                : hotspots.Where(x => x.Mode == "character").Select(x => x.Character).Where(x => x != 0 && x != 200001).Distinct().ToArray();
            Show(v,"Protagonist",false);
            SceneCharacters(talk, participants);
            if (freshSelection) Show(v,"CharacterImage",false);
            Show(v,"Continue",talk);Show(v,"Auto",talk);
            if(!hasText)return;
            var content=v.Get<RectTransform>("Dialogue");Clear(content);
            foreach(var item in S.Transcript)
            {
                if(item.Plot!=0) Line(content,_data.Plot.First(x=>x.Id==item.Plot),false);
                else {var line=Instantiate(Ui.LinePrefab,content);Txt(line,"Speaker",Engine.CharacterName(200001));line.Get<TextMeshProUGUI>("Body").text=item.Reply;}
            }
            Clear(v.Get<RectTransform>("Choices"));
            if(!talk){StartCoroutine(RestoreScroll(v.Get<ScrollRect>("DialogueScroll"),S.DialogueScroll));return;}
            var active=_data.EventById(S.ActiveEvent); bool end=S.Line+1>=_data.Lines(active.Plot).Length;
            bool choices=end && active.ChoiceGroup!=0 && S.QueuedItem==0;
            if(choices)
            {
                S.Auto=false;
                SetChoiceSpace(v, true);
                bool firstChoice = true;
                foreach(var choice in _data.PlotChoice.Where(x=>x.Group==active.ChoiceGroup).OrderBy(x=>x.Order))
                {
                    int id=choice.Id;
                    var button=Card(v.Get<RectTransform>("Choices"),choice.Text,"",()=>{Engine.Advance(id);Refresh();},template:DialogueChoicePrefab);
                    if (DialogueChoicePrefab)
                    {
                        button.Get<Text>("Title").font=ReadingFont;
                        button.GetComponent<V24DialogueChoice>().SetFirst(firstChoice);
                        firstChoice = false;
                    }
                }
            }
            var requiredAction=Engine.RequiredAction();
            Btn(v,"Continue",()=>{if(requiredAction==null)Next();else {string result=Engine.ExecuteAction(requiredAction.Id);if(result!="成功")Toast(result);Refresh();}},requiredAction!=null?"进入中控室":"继续 →",!choices);
            Show(v,"Continue",!end||requiredAction!=null);
            Btn(v,"Auto",()=>{S.Auto=!S.Auto;_nextAuto=Time.unscaledTime+S.AutoSeconds;Txt(v,"AutoLabel",S.Auto?"自动：开启":"自动：关闭");Save();},S.Auto?"自动：开启":"自动：关闭");
            StartCoroutine(RestoreScroll(v.Get<ScrollRect>("DialogueScroll"),S.DialogueScroll));
        }
        private void Portrait(int id)
        {
            DrawCharacter("CharacterImage", id, true);
        }
        private Vector2 _dialogueScrollSize, _dialogueViewportSize;
        private bool _choiceLayoutSaved;
        private void SetChoiceSpace(CastleUiView view, bool choices)
        {
            if (!DialogueChoicePrefab) return;
            var scroll = view.Get<ScrollRect>("DialogueScroll");
            var rect = (RectTransform)scroll.transform;
            if (!_choiceLayoutSaved)
            {
                _dialogueScrollSize = rect.sizeDelta;_dialogueViewportSize = scroll.viewport.sizeDelta;_choiceLayoutSaved = true;
            }
            rect.sizeDelta = new Vector2(_dialogueScrollSize.x, choices ? 480 : _dialogueScrollSize.y);
            scroll.viewport.sizeDelta = new Vector2(_dialogueViewportSize.x, choices ? 480 : _dialogueViewportSize.y);
        }
        private Sprite PortraitSprite(int id) => CharacterArt.FirstOrDefault(x => x.Character == id)?.Portrait;
        private IEnumerator RestoreScroll(ScrollRect scroll,float position) { yield return null; Canvas.ForceUpdateCanvases();scroll.verticalNormalizedPosition=position; }
        private void Next()
        {
            var ev=_data.EventById(S.ActiveEvent);if(ev==null)return;
            S.DialogueScroll=0;Engine.Advance();_nextAuto=Time.unscaledTime+S.AutoSeconds;Refresh();
        }
        private CastleUiView Line(Transform parent,V24Plot plot,bool recording)
        {
            var view=Instantiate(Ui.LinePrefab,parent); view.gameObject.SetActive(true);
            Txt(view,"Speaker",(recording?TimeSpan.FromSeconds(plot.At).ToString(@"mm\:ss")+" · ":"")+SpeakerName(plot,recording)+(S.Marked.Contains(plot.Id)?"  ◆":""));
            string body=plot.Text;
            for(int i=0;i<plot.LinkText.Length;i++)
            {
                string target=plot.LinkTarget[i];
                if(target.StartsWith("action:") && (recording || (_data.Formal ? !_data.DialogueActionConfig.Any(x=>x.SourcePlot==plot.Id&&x.SourceEvent==S.ActiveEvent&&!S.ConsumedActions.Contains(x.Id)) : Engine.RequiredAction()?.SourcePlot!=plot.Id)))continue;
                if(target.StartsWith("character:")&&!CharacterUnlocked(int.Parse(target.Substring(10))))continue;
                body=body.Replace(plot.LinkText[i],"<link=\""+plot.Id+";"+target+"\"><color=#E6BB68><u>"+plot.LinkText[i]+"</u></color></link>");
            }
            var tmp=view.Get<TextMeshProUGUI>("Body");tmp.text=body;
            tmp.GetComponent<FlowTextLink>().Clicked=Link;
            tmp.GetComponent<V24LineHit>().Clicked=()=>{if(recording)ToggleRecordingLine(plot.Id);};
            var layout=view.GetComponent<LayoutElement>();layout.preferredHeight=Mathf.Max(110,65+tmp.GetPreferredValues(body,recording?720:405,0).y);
            return view;
        }
        private bool CharacterUnlocked(int id) => Engine.KnowsCharacter(id);
        private void Link(string key)
        {
            var pair=key.Split(';');int line=int.Parse(pair[0]);var target=pair[1].Split(':');int id=int.Parse(target[1]);
            if(!S.Shown.Contains(line))return;
            if(target[0]=="room") {Map(id);return;}
            if(target[0]=="action") { string result=Engine.ExecuteAction(id); if(result!="成功") { Confirm(result,()=>{}); return; } Refresh();return; }
            if(target[0]=="character") {if(CharacterUnlocked(id))Journal(0,id,true);return;}
            if(target[0]=="entry")
            {
                var entry=_data.EntryById(id);Engine.Claim(line,id);
                if(S.RewardPopups.Count>0)Reward();
                else if(entry.Type=="info")Journal(0,entry.Owner[0],true);
                else Journal(entry.Type=="item"||entry.Type=="tape"?1:2,id,true);
            }
        }
        private void Rest()
        {
            if(Engine.Busy||S.Room!=_data.GameSettings.HomeRoom)return;
            string reason=Engine.SleepReason();if(reason!=""){Toast(reason);return;}
            Confirm("休息至下一调查日 10:00？未完成的当天剧情可能错过；将保留睡前恢复点。",()=>
            {
                if(!_test)V24Save.Write(S,V24Save.Path+".before-sleep");Engine.Sleep();Engine.Scan();Refresh();
            });
        }
        private void Map(int target=0)
        {
            if(!Engine.Has(_data.GameSettings.MapEntry)){Toast("尚未获得地图");return;}
            if(target!=0){_selectedRoom=target;_floor=_data.Room.First(x=>x.Id==target).Floor;}
            Push("Map");MapRender();
        }
        private void MapRender()
        {
            var v=V("Map");Btn(v,"Close",Pop);
            _floor=Mathf.Clamp(_floor,1,2);MapArt(v,_floor);
            Btn(v,"Floor1",()=>{_floor=1;_selectedRoom=0;MapRender();});Btn(v,"Floor2",()=>{_floor=2;_selectedRoom=0;MapRender();});
            Txt(v,"Heading","古堡地图 · "+_floor+"F");Show(v,"TooltipPanel",false);
            var root=v.Get<RectTransform>("Rooms");Clear(root);
            foreach(var node in _data.MapConfig[0].Nodes.Where(x=>x.Floor==_floor&&x.Room!=0))
            {
                var room=_data.Room.First(x=>x.Id==node.Room);bool unlocked=S.Unlocked.Contains(room.Id),open=unlocked&&Engine.Window(room.Start,room.End,Engine.Now);
                if(room.Id==_data.GameSettings.ControlRoom)open=unlocked&&Engine.ControlMapOpen();
                var card=Card(root,(S.Room==room.Id?"◆ ":"")+(unlocked?Engine.RoomName(room.Id):"未解锁"),"",()=>
                {
                    _selectedRoom=room.Id;
                    if(Engine.CanEnter(room.Id) && Engine.Move(room.Id)){Pop();Refresh();}
                    else {Show(v,"TooltipPanel",true);Txt(v,"Tooltip",RoomDetail(room.Id)+(Engine.Busy?"\n对白或奖励期间仅可查看":"\n当前不可进入"));}
                },true);
                PlaceMapRegion(card,root,room.Id);
                Txt(card,"Body",S.Room==room.Id?"◆":open?"开放中":unlocked?room.Start+"—"+room.End:"");
                card.Get<Image>("Background").color=Color.clear;
                card.Get<Button>("Select").transition=Selectable.Transition.None;
                Show(card,"Title",false);
                card.Get<FlowPointer>("Pointer").Enter=()=>{Show(v,"TooltipPanel",true);Txt(v,"Tooltip",RoomDetail(room.Id));};card.Get<FlowPointer>("Pointer").Exit=()=>Show(v,"TooltipPanel",false);
            }
            foreach(var hidden in new[]{"Heading","Details","Hint","Enter"})Show(v,hidden,false);
            Txt(v,"Details",_selectedRoom==0?"请选择地点":RoomDetail(_selectedRoom));
            Txt(v,"Hint",S.ActiveEvent!=0?"对白期间只能查看地图":"选择地点后点击进入；普通移动不加时。");
            Btn(v,"Enter",()=>{if(Engine.Move(_selectedRoom)){Pop();Refresh();}},enabled:_selectedRoom!=0&&Engine.CanEnter(_selectedRoom));
        }
        private string RoomDetail(int id)
        {
            if(!S.Unlocked.Contains(id))return "未解锁区域";
            var r=_data.Room.First(x=>x.Id==id);
            var devices=_data.DeviceConfig.Where(x=>x.Room==id).Select(x=>S.KnownDevices.Contains(x.Id)?x.Label:"未知设备");
            return Engine.RoomName(id)+"\n"+(id==_data.GameSettings.ControlRoom?Engine.ControlReason():string.IsNullOrEmpty(r.Start)?"全天开放":r.Start+"—"+r.End)+"\n"+string.Join("、",devices)+(S.Room==id?"\n◆ 当前位置":"");
        }
    }
}


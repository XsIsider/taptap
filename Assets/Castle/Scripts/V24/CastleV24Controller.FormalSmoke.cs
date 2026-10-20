using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Castle.V24
{
    public sealed partial class CastleV24Controller
    {
        private IEnumerator FormalSmoke()
        {
            Directory.CreateDirectory("Logs/V24Screens");
            Application.logMessageReceived+=(message,stack,type)=>{if(type==LogType.Exception||type==LogType.Error)File.AppendAllText("Logs/formal-smoke-errors.txt",message+"\n"+stack);};
            yield return null;
            V("Title").Get<Button>("Credits").onClick.Invoke();yield return null;
            if (!V("Credits").gameObject.activeInHierarchy) throw new Exception("制作名单未打开");
            Capture("formal-credits");
            V("Credits").Get<Button>("Close").onClick.Invoke();yield return null;
            if (_modal.Count != 0 || !V("Title").gameObject.activeInHierarchy) throw new Exception("制作名单关闭未返回标题");
            StartNew();yield return null;
            while (_openingTransition) yield return null;
            foreach (var line in Engine.OpeningLines)
            {
                if (_page != "Opening" || V("Opening").Get<Text>("Body").text != line.Text || Ui.Hud.gameObject.activeSelf)
                    throw new Exception("黑屏开场旁白显示错误");
                V("Opening").Get<Button>("Surface").onClick.Invoke();
                while (_openingTransition) yield return null;
            }
            if (_page != "Invitation") throw new Exception("开场旁白未衔接邀请函");
            foreach(var view in Ui.GetComponentsInChildren<CastleUiView>(true))view.ValidateBindings();
            if(PlotAudio.Length!=153||PlotAudio.Any(x=>!x.Clip))throw new Exception("正式音频绑定不完整");
            V("Invitation").Get<Button>("Surface").onClick.Invoke();yield return null;
            V("Invitation").Get<Button>("Surface").onClick.Invoke();yield return null;
            while(S.RewardPopups.Count>0){V("Acquire").Get<Button>("Confirm").onClick.Invoke();yield return null;}
            if(S.ActiveEvent!=920001)throw new Exception("正式开场入口错误");
            if (S.Transcript.Count != 1 || S.Transcript[0].Plot != 520003) throw new Exception("开场旁白重复进入普通对白");
            Capture("formal-opening");yield return null;
            var first=JsonUtility.ToJson(S);
            var choiceEvent = _data.Event.First(x => x.ChoiceGroup != 0 && _data.PlotChoice.Count(c => c.Group == x.ChoiceGroup) == 3);
            Engine.Start(choiceEvent.Id, true, false, true);
            S.Line = _data.Lines(choiceEvent.Plot).Length - 1;S.QueuedItem = 0;S.RewardPopups.Clear();
            S.Transcript.Clear();S.Transcript.Add(new V24Transcript { Plot = _data.Lines(choiceEvent.Plot).Last().Id });
            Page("Explore");yield return null;
            var choiceCards = V("Explore").Get<RectTransform>("Choices").GetComponentsInChildren<V24DialogueChoice>();
            if (choiceCards.Length != 3) throw new Exception("对话选项数量错误");
            var choiceImage = choiceCards[0].Border;
            float normalAlpha = choiceImage.color.a;
            choiceCards[0].OnPointerEnter(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current));
            if (choiceImage.color.a <= normalAlpha || !choiceCards[0].Glow.activeSelf) throw new Exception("选项悬停未高亮");
            Capture("formal-dialogue-choices-hover");
            choiceCards[0].OnPointerExit(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current));
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(choiceCards[1].gameObject);yield return null;
            if (!choiceCards[1].Glow.activeSelf || choiceCards[0].Glow.activeSelf) throw new Exception("选项键盘选择高亮错误");
            Capture("formal-dialogue-choices-selected");
            var a = new Vector3[4];var b = new Vector3[4];
            choiceCards[0].GetComponent<RectTransform>().GetWorldCorners(a);choiceCards[1].GetComponent<RectTransform>().GetWorldCorners(b);
            if (a[0].y <= b[1].y) throw new Exception("对话选项重叠");
            choiceCards[1].GetComponent<Button>().onClick.Invoke();yield return null;
            if (S.ActiveEvent == choiceEvent.Id && S.Line == _data.Lines(choiceEvent.Plot).Length - 1) throw new Exception("选项点击没有推进剧情");
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            Engine.State=JsonUtility.FromJson<V24State>(first);CloseAll();Page("Explore");yield return null;
            foreach (var entry in _data.Entry.Where(x => x.Type == "item" && !string.IsNullOrEmpty(x.Icon)))
            {
                S.RewardPopups.Clear();S.RewardPopups.Add(entry.Id);Reward();yield return null;
                var rewardArt = V("Acquire").Get<RawImage>("ItemArt");
                if (!rewardArt.gameObject.activeInHierarchy || rewardArt.texture != ItemTexture(entry) || rewardArt.transform.parent.name != "IconBack") throw new Exception("获得道具图未在 IconBack 更新：" + entry.Id);
                Capture("formal-acquire-" + entry.Id);Pop();
            }
            Engine.State=JsonUtility.FromJson<V24State>(first);
            foreach (var type in new[] { "clue", "info", "item" })
            {
                var entry = _data.Entry.First(x => x.Type == type && (type != "item" || !string.IsNullOrEmpty(x.Icon)));
                S.RewardPopups.Clear();S.RewardPopups.Add(entry.Id);Reward();yield return null;
                var art = V("Acquire").Get<RawImage>("ItemArt");
                bool isItem = type == "item";
                if (art.gameObject.activeSelf != isItem || art.transform.parent.GetComponent<Image>().enabled == isItem) throw new Exception("获得弹窗图片分类错误：" + type);
                Capture("formal-acquire-type-" + type);Pop();
            }
            Engine.State=JsonUtility.FromJson<V24State>(first);
            if (ItemArt.Length != 7 || ItemArt.Any(x => !x.Texture)) throw new Exception("道具图绑定不完整");
            foreach (var entry in _data.Entry.Where(x => !string.IsNullOrEmpty(x.Icon)))
            {
                Engine.Grant(entry.Id, "art-smoke:" + entry.Id, false);
                if (!ItemTexture(entry) || !ItemTexture(entry, true)) throw new Exception("道具图读取失败：" + entry.Id);
            }
            Journal(1);yield return null;Capture("formal-item-journal");CloseAll();
            foreach (var entry in _data.Entry.Where(x => x.Type == "item" && !string.IsNullOrEmpty(x.Icon)))
            {
                UseEntry(entry.Id);yield return null;
                if (_modal.LastOrDefault() == "Letter")
                {
                    if (V("Letter").Get<RawImage>("ItemArt").texture != ItemTexture(entry, true)) throw new Exception("详情图绑定错误");
                    Capture("formal-item-detail-" + entry.Id);Pop();
                }
            }
            Engine.State=JsonUtility.FromJson<V24State>(first);
            S.Shown.Add(520005);Link("520005;entry:320300");yield return null;
            if(!Engine.KnowsCharacter(200003))throw new Exception("点击姓名未解锁人物");
            while(S.RewardPopups.Count>0){V("Acquire").Get<Button>("Confirm").onClick.Invoke();yield return null;}
            Link("520005;entry:320300");yield return null;
            if(_modal.LastOrDefault()!="Journal"||S.JournalSelection!=200003)throw new Exception("重复点击未定位人物");
            Capture("formal-character-profile");CloseAll();Engine.State=JsonUtility.FromJson<V24State>(first);
            S.ActiveEvent=0;S.Transcript.Clear();S.Day=2;Engine.SetClock("18:30");S.Room=100002;S.CompletedEvents.Add(920033);Page("Explore");yield return null;
            if(V("Explore").Get<RectTransform>("DialoguePanel").gameObject.activeSelf)throw new Exception("多人首次进入应隐藏对白");
            V("Explore").Get<Button>("AvatarStack").onClick.Invoke();yield return null;
            if(V("Explore").Get<RectTransform>("People").GetComponentsInChildren<CastleUiView>().Length!=6)throw new Exception("晚宴头像数量错误");
            Capture("formal-banquet-selection");
            ChooseConversation(CurrentChoices().First(x=>x.Event==920035));
            if(S.ActiveEvent!=0)throw new Exception("未拦截错误顺序");
            ChooseConversation(CurrentChoices().First(x=>x.Event==920034));yield return null;
            if(S.ActiveEvent!=920034||Music.clip!=BanquetBgm)throw new Exception("晚宴选择或音乐错误");
            Capture("formal-banquet-dialogue");yield return null;
            Engine.State=JsonUtility.FromJson<V24State>(first);S.ActiveEvent=0;S.Day=2;S.DayBase+=TimeSpan.FromDays(1).Ticks;S.Room=100003;Engine.SetClock("13:00");Engine.RegisterRecordings();
            Page("Recordings");yield return null;Capture("formal-recordings");
            var burnState = JsonUtility.ToJson(S);
            BurnSelect();yield return null;
            var burnGroup = _data.Record.Where(Engine.Accessible).GroupBy(x => x.Device).First();
            int burnDevice = burnGroup.Key;
            S.ExpandedDevices.Remove(burnDevice);BurnRender();yield return null;
            var burnRoot = V("Burn").Get<RectTransform>("List");
            burnRoot.Find("BurnDevice" + burnDevice).GetComponent<CastleUiView>().Get<Button>("Select").onClick.Invoke();yield return null;
            var burnFile = burnRoot.Find("BurnRecord" + burnGroup.First().Id).GetComponent<CastleUiView>();
            burnFile.Get<Button>("Select").onClick.Invoke();yield return null;
            if (_burnRecord != burnGroup.First().Id || !V("Burn").Get<Button>("Burn").interactable) throw new Exception("刻录文件选择失败");
            Capture("formal-burn-expanded");
            burnRoot.Find("BurnDevice" + burnDevice).GetComponent<CastleUiView>().Get<Button>("Select").onClick.Invoke();yield return null;
            if (burnRoot.Find("BurnRecord" + burnGroup.First().Id)) throw new Exception("刻录文件夹未收起");
            Capture("formal-burn-collapsed");V("Burn").Get<Button>("Close").onClick.Invoke();
            if (!S.Tapes.Select(x => x.Record).SequenceEqual(JsonUtility.FromJson<V24State>(burnState).Tapes.Select(x => x.Record))) throw new Exception("关闭选择窗口修改了磁带");
            foreach(var record in _data.Record.Where(Engine.Accessible))foreach(var line in _data.Lines(record.Plot))if(!S.Shown.Contains(line.Id))S.Shown.Add(line.Id);
            Engine.ToggleMark(520036);Engine.ToggleMark(520046);Engine.ToggleMark(520040);S.RewardPopups.Clear();
            _puzzle=720001;Engine.Draft(_puzzle).Values.AddRange(new[]{520036,520046});Page("Anchors");yield return null;Capture("formal-anchors");
            S.Puzzles.AddRange(new[]{720001,720002});Page("Space");yield return null;
            if(!V("Space").Get<Button>("DoorOpen").gameObject.activeSelf)throw new Exception("空间门状态未绑定");Capture("formal-space");
            var space = V("Space");
            var spaceDraft = Engine.Draft(_puzzle);
            spaceDraft.Values = _data.PuzzleById(_puzzle).Answer.Split('|').Select(x => int.Parse(x.Split('=').Last())).ToList();
            PathRender(space);yield return null;
            for (int i = 0; i < spaceDraft.Values.Count; i++)
            {
                spaceDraft.Floor = _data.MapConfig.First(x => x.Id == _data.PuzzleById(_puzzle).Map).Nodes.First(x => x.Id == spaceDraft.Values[i]).Floor;
                PathRender(space);yield return null;
                space.Get<RectTransform>("Rooms").Find("Node" + spaceDraft.Values[i]).GetComponent<CastleUiView>().Get<Button>("Select").onClick.Invoke();
                yield return null;
                if (_slot != i) throw new Exception("点击已放置房间没有选择对应标签");
                space.Get<Button>(i == 0 ? "DoorOpen" : "DoorClosed").onClick.Invoke();
            }
            if (!spaceDraft.Doors.SequenceEqual(new[] { "open", "closed", "closed" })) throw new Exception("三个房间门状态串联");
            spaceDraft.Floor = _data.MapConfig.First(x => x.Id == _data.PuzzleById(_puzzle).Map).Nodes.First(x => x.Id == spaceDraft.Values[0]).Floor;
            PathRender(space);yield return null;
            space.Get<RectTransform>("Rooms").Find("Node" + spaceDraft.Values[0]).GetComponent<CastleUiView>().Get<Button>("Select").onClick.Invoke();yield return null;
            space.Get<Button>("DoorClosed").onClick.Invoke();
            space.Get<Button>("DoorOpen").onClick.Invoke();
            if (!spaceDraft.Doors.SequenceEqual(new[] { "open", "closed", "closed" })) throw new Exception("修改后厨影响其他房间");
            Engine.State = JsonUtility.FromJson<V24State>(JsonUtility.ToJson(S));
            if (!Engine.Draft(_puzzle).Doors.SequenceEqual(new[] { "open", "closed", "closed" })) throw new Exception("门状态存档恢复失败");
            foreach (var device in _data.DeviceConfig) if (!S.Repaired.Contains(device.Id)) S.Repaired.Add(device.Id);
            string spaceResult = Engine.ValidatePuzzle(_puzzle);
            if (spaceResult != "成功") throw new Exception("正确空间答案无法通过：" + spaceResult);
            S.PendingPuzzle = 0;
            Capture("formal-space-independent-doors");
            _puzzle=720020;Push("Path");PathRender(V("Path"));yield return null;Capture("formal-path");Pop();
            Engine.Grant(340001,"smoke",false);Engine.Grant(340009,"smoke9",false);Journal(2);yield return null;Capture("formal-record-journal");CloseAll();
            Page("Timeline");yield return null;Capture("formal-timeline-sort");
            S.Ending=true;S.PendingPuzzle=720030;S.EndingTransitionDone=true;_record=620025;_position=25;_playing=false;Page("Timeline");yield return null;
            if(!V("Timeline").Get<V24EndingWave>("SoundWave").GetComponent<CanvasRenderer>())throw new Exception("声纹缺少渲染组件");
            if(V("Timeline").Get<Text>("EndingText").transform.parent!=V("Timeline").Get<RectTransform>("RevealRoot"))throw new Exception("结局文字层级错误");
            Capture("formal-timeline-reveal");
            yield return new WaitForSecondsRealtime(1);
            File.WriteAllText("Logs/formal-smoke.txt","PASS saved UI bindings; 153 clips; opening; banquet order and music; recordings; anchors; doors; hallway path; journal; timeline sort/reveal\n");
            Application.Quit(0);
        }
    }
}

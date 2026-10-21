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
            if (S.Transcript.Count == 0) ClearDialogueCollectionHint();
            v.Get<RectTransform>("DialoguePanel").GetComponent<V24LineHit>().Clicked = AdvanceDialogueFromPanel;
            var dialogueScroll = v.Get<ScrollRect>("DialogueScroll");
            dialogueScroll.GetComponent<V24DialogueScrollInput>().UserScrolled = OnDialogueUserScroll;
            dialogueScroll.verticalScrollbar.GetComponent<V24DialogueScrollbarInput>().UserScrolled = OnDialogueUserScroll;
            dialogueScroll.onValueChanged.RemoveListener(OnDialogueScrollChanged);
            dialogueScroll.onValueChanged.AddListener(OnDialogueScrollChanged);
            int lastPlot = S.Transcript.LastOrDefault()?.Plot ?? 0;
            int lastChoice = S.Transcript.LastOrDefault()?.Choice ?? 0;
            bool newText = _renderedDialogueCount != S.Transcript.Count || _renderedDialogueLastPlot != lastPlot || _renderedDialogueLastChoice != lastChoice;
            if (!newText && _renderedDialogueCount > 0) S.DialogueScroll = dialogueScroll.verticalNormalizedPosition;
            if (newText) { S.DialogueScroll = 0; _dialogueHistoryPaused = false; }
            _renderedDialogueCount = S.Transcript.Count;
            _renderedDialogueLastPlot = lastPlot;
            _renderedDialogueLastChoice = lastChoice;
            SetChoiceSpace(v, false);
            // 仅替换有明确对应素材的场景，其他房间恢复原背景，避免沿用上一房间的图。
            bool talk=S.ActiveEvent!=0;
            bool itemBlocked = UpdateDialogueItemHint();
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
                if (activity != null && !Engine.IsRoomBasedActivity)
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
                else
                {
                    var line = Instantiate(Ui.LinePrefab, content);
                    line.gameObject.SetActive(true);
                    SetInlineDialogue(line, Engine.CharacterName(200001), item.Reply);
                }
            }
            Clear(v.Get<RectTransform>("Choices"));
            if(!talk){QueueDialogueScroll(S.DialogueScroll);return;}
            var active=_data.EventById(S.ActiveEvent); bool end=S.Line+1>=_data.Lines(active.Plot).Length;
            bool choices=!itemBlocked && end && active.ChoiceGroup!=0 && S.QueuedItem==0;
            if(choices)
            {
                S.Auto=false;
                SetChoiceSpace(v, true);
                bool firstChoice = true;
                foreach(var choice in _data.PlotChoice.Where(x=>x.Group==active.ChoiceGroup).OrderBy(x=>x.Order))
                {
                    int id=choice.Id;
                    var button=Card(v.Get<RectTransform>("Choices"),choice.Text,"",()=>ChooseDialogueOption(id),template:DialogueChoicePrefab);
                    if (DialogueChoicePrefab)
                    {
                        button.Get<Text>("Title").font=ReadingFont;
                        button.GetComponent<V24DialogueChoice>().SetFirst(firstChoice);
                        firstChoice = false;
                    }
                }
            }
            var requiredAction=Engine.RequiredAction();
            Btn(v,"Continue",()=>{if(BlockDialogueAdvance())return;if(requiredAction==null)Next();else {string result=Engine.ExecuteAction(requiredAction.Id);if(result!="成功")Toast(result);Refresh();}},requiredAction!=null?"进入中控室":"继续 →",!choices);
            Show(v,"Continue",itemBlocked||!end||requiredAction!=null||(_dialogueHistoryPaused&&!choices));
            Btn(v,"Auto",()=>
            {
                if(BlockDialogueAdvance())return;
                S.Auto=!S.Auto;
                if (S.Auto) { _dialogueHistoryPaused = false; S.DialogueScroll = 0; QueueDialogueScroll(0); }
                _nextAuto=Time.unscaledTime+S.AutoSeconds;
                Txt(v,"AutoLabel",S.Auto?"自动：开启":"自动：关闭");Save();
            },S.Auto?"自动：开启":"自动：关闭");
            QueueDialogueScroll(S.DialogueScroll);
        }
        private void ChooseDialogueOption(int choiceId)
        {
            if (_page != "Explore" || _modal.Count > 0) return;
            Engine.Advance(choiceId);
            // Refresh 遇到奖励会直接打开弹窗；先刷新底层对白，移除旧选项并显示已选回复。
            if (S.RewardPopups.Count > 0) Explore();
            Refresh();
        }
        /// <summary>当前对白只保留最新一条收录结果，不参与存档或自动消失。</summary>
        private void ShowDialogueCollectionHint(int entryId)
        {
            if (_page != "Explore" || S.Transcript.Count == 0) return;
            var entry = _data.EntryById(entryId);
            if (entry == null || entry.Type != "info" && entry.Type != "item" && entry.Type != "tape") return;
            string name = Engine.IsCharacterProfile(entryId) && entry.Owner.Length > 0
                ? Engine.CharacterName(entry.Owner[0]) : entry.Name;
            string category = entry.Type == "info" ? "人物" : "道具";
            var hint = V("Explore").Get<TextMeshProUGUI>("DialogueCollectionHint");
            hint.text = name + "已收录进调查册·" + category;
            hint.gameObject.SetActive(true);
        }
        private void ClearDialogueCollectionHint()
        {
            var hint = V("Explore").Get<TextMeshProUGUI>("DialogueCollectionHint");
            hint.text = string.Empty;
            hint.gameObject.SetActive(false);
        }
        private Coroutine _dialogueItemHintRoutine;
        /// <summary>道具未收集时暂停推进；底部提示淡入淡出，不遮挡链接和其他按钮。</summary>
        private bool UpdateDialogueItemHint()
        {
            int item = _page == "Explore" && S.ActiveEvent != 0 ? Engine.PendingDialogueItem() : 0;
            bool canShow = item != 0 && _modal.Count == 0 && S.RewardPopups.Count == 0;
            if (item != 0)
            {
                if (S.Auto) { S.Auto = false; Save(); }
                _endingDialogue = 0;
                Txt(V("Explore"), "AutoLabel", "自动：关闭");
            }
            if (canShow && _pendingDialogueItem != item)
            {
                _pendingDialogueItem = item;
                ShowDialogueItemHint();
            }
            else if (!canShow && _pendingDialogueItem != 0)
            {
                _pendingDialogueItem = 0;
                HideDialogueItemHint();
            }
            return item != 0;
        }
        private bool BlockDialogueAdvance()
        {
            if (!UpdateDialogueItemHint()) return false;
            ShowDialogueItemHint();
            return true;
        }
        private void ShowDialogueItemHint()
        {
            if (_dialogueItemHintRoutine != null) StopCoroutine(_dialogueItemHintRoutine);
            HideToast();
            V("Explore").Get<TextMeshProUGUI>("ItemLinkHintText").text = "似乎有什么信息需要注意……请先点击对白中的道具链接";
            _dialogueItemHintRoutine = StartCoroutine(FadeHint(V("Explore").Get<CanvasGroup>("ItemLinkHint"),
                () => _dialogueItemHintRoutine = null));
        }
        private void HideDialogueItemHint()
        {
            if (_dialogueItemHintRoutine != null) StopCoroutine(_dialogueItemHintRoutine);
            _dialogueItemHintRoutine = null;
            var hint = V("Explore").Get<CanvasGroup>("ItemLinkHint");
            hint.alpha = 0;
            hint.gameObject.SetActive(false);
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
            // 出现选项时轨道随正文区域缩短，保留顶部 80、底部 28 个画布单位的边距。
            var barRect = (RectTransform)scroll.verticalScrollbar.transform;
            barRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rect.rect.height - 108f);
        }
        private Sprite PortraitSprite(int id) => CharacterArt.FirstOrDefault(x => x.Character == id)?.Portrait;
        private Coroutine _dialogueScrollRoutine;
        private bool _restoringDialogueScroll, _dialogueHistoryPaused;
        private int _renderedDialogueCount, _renderedDialogueLastPlot, _renderedDialogueLastChoice;
        private void QueueDialogueScroll(float position)
        {
            if (_dialogueScrollRoutine != null) StopCoroutine(_dialogueScrollRoutine);
            _restoringDialogueScroll = true;
            _dialogueScrollRoutine = StartCoroutine(RestoreDialogueScroll(Mathf.Clamp01(position)));
        }
        private IEnumerator RestoreDialogueScroll(float position)
        {
            // 等待旧行销毁及文本布局完成，避免在旧高度上定位后下一帧又跳回去。
            yield return null;
            var scroll = V("Explore").Get<ScrollRect>("DialogueScroll");
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
            scroll.Rebuild(CanvasUpdate.PostLayout);
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = position;
            S.DialogueScroll = position;
            _restoringDialogueScroll = false;
            _dialogueScrollRoutine = null;
        }
        private void OnDialogueScrollChanged(Vector2 position)
        {
            if (_page == "Explore" && !_restoringDialogueScroll && _renderedDialogueCount > 0)
                S.DialogueScroll = Mathf.Clamp01(position.y);
        }
        private void OnDialogueUserScroll()
        {
            if (_page != "Explore" || _modal.Count > 0) return;
            if (_dialogueScrollRoutine != null) StopCoroutine(_dialogueScrollRoutine);
            _dialogueScrollRoutine = null;
            _restoringDialogueScroll = false;
            _dialogueHistoryPaused = true;
            _endingDialogue = 0;
            S.Auto = false;
            var view = V("Explore");
            S.DialogueScroll = Mathf.Clamp01(view.Get<ScrollRect>("DialogueScroll").verticalNormalizedPosition);
            Txt(view, "AutoLabel", "自动：关闭");
            var active = _data.EventById(S.ActiveEvent);
            if (active != null && (active.ChoiceGroup == 0 || S.QueuedItem != 0)) Show(view, "Continue", true);
            Save();
        }
        private void Next()
        {
            var ev=_data.EventById(S.ActiveEvent);if(ev==null)return;
            if (BlockDialogueAdvance()) return;
            _dialogueHistoryPaused = false;
            S.DialogueScroll=0;Engine.Advance();_nextAuto=Time.unscaledTime+S.AutoSeconds;Refresh();
        }
        private void AdvanceDialogueFromPanel()
        {
            if (_page != "Explore" || _modal.Count > 0 || S.ActiveEvent == 0 || S.RewardPopups.Count > 0) return;
            if (BlockDialogueAdvance()) return;
            // 只复用可用的继续按钮；选项、结束状态和专用动作不能被背景点击跳过。
            var button = V("Explore").Get<Button>("Continue");
            if (!button.isActiveAndEnabled || !button.IsInteractable() || Engine.RequiredAction() != null) return;
            button.onClick.Invoke();
        }
        private CastleUiView Line(Transform parent,V24Plot plot,bool recording)
        {
            var view=Instantiate(Ui.LinePrefab,parent); view.gameObject.SetActive(true);
            string speaker = (recording ? TimeSpan.FromSeconds(plot.At).ToString(@"mm\:ss") + " · " : "") + SpeakerName(plot, recording) + (S.Marked.Contains(plot.Id) ? "  ◆" : "");
            Txt(view, "Speaker", speaker);
            string body=plot.Text;
            for(int i=0;i<plot.LinkText.Length;i++)
            {
                string target=plot.LinkTarget[i];
                if (Engine.PersonLinkResolved(target)) continue;
                if(target.StartsWith("action:") && (recording || (_data.Formal ? !_data.DialogueActionConfig.Any(x=>x.SourcePlot==plot.Id&&(x.SourceEvent==S.ActiveEvent||Engine.CanOpenInteraction(x.Id))&&!S.ConsumedActions.Contains(x.Id)) : Engine.RequiredAction()?.SourcePlot!=plot.Id)))continue;
                if(target.StartsWith("character:")&&!CharacterUnlocked(int.Parse(target.Substring(10))))continue;
                bool pendingItem = !recording && target.StartsWith("entry:")
                    && int.TryParse(target.Substring(6), out int entryId)
                    && (_data.EntryById(entryId)?.Type == "item" || _data.EntryById(entryId)?.Type == "tape")
                    && !Engine.Has(entryId);
                string linkText = pendingItem ? "<b>" + plot.LinkText[i] + "</b>" : plot.LinkText[i];
                body=body.Replace(plot.LinkText[i],"<link=\""+plot.Id+";"+target+"\"><color="+(pendingItem?"#FFE09A":"#E6BB68")+"><u>"+linkText+"</u></color></link>");
            }
            var tmp=view.Get<TextMeshProUGUI>("Body");tmp.text=body;
            tmp.GetComponent<FlowTextLink>().Clicked=Link;
            tmp.GetComponent<V24LineHit>().Clicked=()=>{if(recording)ToggleRecordingLine(plot.Id);};
            if (recording && _page != "Tape")
            {
                var layout = view.GetComponent<LayoutElement>();
                layout.preferredHeight = Mathf.Max(110, 65 + tmp.GetPreferredValues(body, 720, 0).y);
            }
            else SetInlineDialogue(view, recording ? SpeakerName(plot,true) : speaker, body);
            if (recording) tmp.GetComponent<V24LineHit>().Clicked=()=>ToggleRecordingLine(plot.Id);
            tmp.GetComponent<V24DialogueLinkFeedback>().Configure(recording ? Array.Empty<string>()
                : plot.LinkTarget.Where(IsPersonNameLink).Select(target => plot.Id + ";" + target),
                () => _page == "Explore" && _modal.Count == 0);
            return view;
        }
        private bool IsPersonNameLink(string target)
        {
            if (target.StartsWith("character:")) return true;
            return target.StartsWith("entry:") && int.TryParse(target.Substring(6), out int id) && Engine.IsCharacterProfile(id);
        }
        private void SetInlineDialogue(CastleUiView view, string speakerName, string body)
        {
            var speaker = view.Get<Text>("Speaker");
            var text = view.Get<TextMeshProUGUI>("Body");
            speaker.text = speakerName;
            speaker.enabled = false;
            // 稍微拉开同一条对白内的行距，后续首选高度计算同时包含新增间距。
            text.lineSpacing = DialogueLineSpacing;
            text.GetComponent<V24LineHit>().Clicked = AdvanceDialogueFromPanel;

            // 先处理正文链接，再拼姓名，避免姓名前缀被关键词替换成可点击链接。
            string color = ColorUtility.ToHtmlStringRGBA(speaker.color);
            text.text = string.IsNullOrEmpty(speakerName) ? body : "<color=#" + color + "><size=" + speaker.fontSize + ">" + speakerName + "：</size></color>" + body;
            var bodyRect = text.rectTransform;
            bodyRect.anchoredPosition = new Vector2(bodyRect.anchoredPosition.x, speaker.rectTransform.anchoredPosition.y);

            // 列表布局尚未刷新时，预制件仍保留原宽度，按父列表可用宽度计算换行和整行高度。
            var parentRect = (RectTransform)view.transform.parent;
            var group = parentRect.GetComponent<VerticalLayoutGroup>();
            if (group != null) group.spacing = DialogueEntrySpacing;
            float rowWidth = parentRect.rect.width - (group != null ? group.padding.horizontal : 0);
            float textWidth = Mathf.Max(1f, rowWidth * (bodyRect.anchorMax.x - bodyRect.anchorMin.x) + bodyRect.sizeDelta.x);
            float textHeight = Mathf.Ceil(text.GetPreferredValues(text.text, textWidth, 0).y);
            bodyRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, textHeight);
            view.GetComponent<LayoutElement>().preferredHeight = Mathf.Ceil(-bodyRect.anchoredPosition.y + textHeight + 4f);
        }
        private bool CharacterUnlocked(int id) => Engine.KnowsCharacter(id);
        private void Link(string key)
        {
            var pair=key.Split(';');int line=int.Parse(pair[0]);var target=pair[1].Split(':');int id=int.Parse(target[1]);
            if(!S.Shown.Contains(line))return;
            if(target[0]=="room") {OpenAddressMap(id);return;}
            // 正文中的中控室地址只打开地图；由末尾的独立按钮确认进入，避免点击地址直接转场。
            if(target[0]=="action")
            {
                var action = _data.DialogueActionConfig.FirstOrDefault(x=>x.Id==id && x.SourcePlot==line);
                if (action == null) return;
                if (action.Action == "enter_control") OpenAddressMap(_data.GameSettings.ControlRoom);
                else if (Engine.CanOpenInteraction(id)) OpenDialogueInteraction(action);
                return;
            }
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
            if (Engine.IsRoomBasedActivity && _data.DayActivities.Any(x => x.Id == S.Activity && x.AllowWaitToFinish))
            {
                Confirm("休息并等待晚宴？", () => { if (Engine.WaitForActivity()) Refresh(); });
                return;
            }
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
        private void OpenAddressMap(int room)
        {
            var address = _data.Room.FirstOrDefault(x=>x.Id==room);
            if (address == null) return;
            _floor = Mathf.Clamp(address.Floor,1,2);
            _selectedRoom = 0;
            Map();
        }
        private void MapRender()
        {
            var v=V("Map");Btn(v,"Close",Pop);Show(v,"Close",false);
            Btn(v,"Dismiss",Pop);
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
                    if (BlockMapRoomEntry(room.Id)) return;
                    _selectedRoom=room.Id;
                    if (Engine.SelectInteractionRoom(room.Id)) {Pop();Refresh();return;}
                    if (Engine.PendingInteraction != null && Engine.PendingInteraction.CompletionConditions.Any(x=>x.StartsWith("player_selected_room:")))
                    { ShowRoomTooltip(v,cardRoom:room.Id,extra:"请根据对白选择对应客房");return; }
                    if(Engine.CanEnter(room.Id) && Engine.Move(room.Id)){Pop();Refresh();}
                    else ShowRoomTooltip(v,room.Id,Engine.Busy?"对白或奖励期间仅可查看":"当前不可进入");
                },true);
                PlaceMapRegion(card,root,room.Id);
                Txt(card,"Body",S.Room==room.Id?"":open?"开放中":unlocked?room.Start+"—"+room.End:"");
                card.Get<Image>("Background").color=Color.clear;
                card.Get<Button>("Select").transition=Selectable.Transition.None;
                Show(card,"Title",false);
                card.Get<FlowPointer>("Pointer").Enter=()=>ShowRoomTooltip(v,room.Id);card.Get<FlowPointer>("Pointer").Exit=()=>Show(v,"TooltipPanel",false);
            }
            foreach(var hidden in new[]{"Heading","Details","Enter"})Show(v,hidden,false);
            Txt(v,"Details",_selectedRoom==0?"请选择地点":RoomDetail(_selectedRoom));
            Show(v,"Hint",true);Txt(v,"Hint","点击空白处关闭——");
            Btn(v,"Enter",()=>{if(BlockMapRoomEntry(_selectedRoom))return;if(Engine.Move(_selectedRoom)){Pop();Refresh();}},enabled:_selectedRoom!=0&&Engine.CanEnter(_selectedRoom));
        }
        /// <summary>地图中的书房只可查看；拦截发生在房间交互、移动和关闭地图之前。</summary>
        private bool BlockMapRoomEntry(int room)
        {
            if (room != 100014) return false;
            Toast("不可进入书房");
            return true;
        }
        private string RoomDetail(int id)
        {
            if(!S.Unlocked.Contains(id))return "未解锁区域";
            var r=_data.Room.First(x=>x.Id==id);
            var devices=_data.DeviceConfig.Where(x=>x.Room==id).Select(x=>S.KnownDevices.Contains(x.Id)?x.Label:"未知设备");
            return Engine.RoomName(id)+"\n"+(id==_data.GameSettings.ControlRoom?Engine.ControlReason():string.IsNullOrEmpty(r.Start)?"全天开放":r.Start+"—"+r.End)+"\n"+string.Join("、",devices);
        }
    }
}


using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Castle.Flow;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Castle.V24
{
    public sealed partial class CastleV24Controller : MonoBehaviour
    {
        public CastleFlowUi Ui;
        public CastleUiView ConversationPortraitPrefab, AnchorCardPrefab, SpaceCardPrefab;
        public CastleUiView MapCardPrefab, PersonCardPrefab, ItemCardPrefab, RecordingFolderPrefab, RecordingFilePrefab, RecordingLinePrefab;
        public Sprite PathMarkerIcon;
        public Sprite FolderClosed, FolderOpen, PlayIcon, PauseIcon;
        public AudioSource Music, Effects;
        public Sprite ButlerPortrait, MerchantPortrait;
        public Texture2D BanquetBackground, CorridorBackground, GuestBackground, ControlBackground;
        public Sprite InvitationClosed, InvitationOpen;
        private Texture _defaultRoomBackground;
        public V24Engine Engine { get; private set; }
        private V24Data _data;
        private V24State S => Engine.State;
        private readonly List<string> _modal = new List<string>();
        private readonly List<Action> _returns = new List<Action>();
        private string _page = "Title";
        private float _toastUntil, _nextAuto;
        private bool _blocked, _test, _playing;
        private int _floor = 1, _selectedRoom, _selectedEntry, _selectedTape, _selectedCharacter, _puzzle, _selectedLine, _slot, _record, _playTape, _burnRecord;
        private float _position;
        private int _endingDialogue;
        private float _dialogueEndDelay;
        private CastleUiView V(string id) => Ui.View(id);
        private void Awake()
        {
            _test = Environment.GetCommandLineArgs().Contains("-castleV24Smoke");
            _data = V24Data.Load(); V24State state;
            _defaultRoomBackground = V("Explore").transform.Find("Backdrop").GetComponent<RawImage>().texture;
            try { state = _test ? new V24State() : V24Save.Read(); }
            catch (Exception ex) { state = new V24State(); _blocked = true; Debug.LogWarning(ex.Message); }
            Engine = new V24Engine(_data, state); Engine.Changed = Save; Engine.WaitForSelection = IsConversationChoice;
            Music.volume=state.Volume;Effects.volume=state.Volume;
            Ui.Toast.gameObject.SetActive(false); Page("Title");
            if (_test) StartCoroutine(Smoke());
        }
        private void Save()
        {
            if (_test || _blocked || !S.Started) return;
            try { V24Save.Write(S); } catch (Exception ex) { Toast("保存失败：" + ex.Message); }
        }
        private void OnApplicationQuit() => Save();
        private void OnApplicationPause(bool paused) { if (paused) Save(); }
        private void Update()
        {
            if (Ui.Toast.gameObject.activeSelf && Time.unscaledTime > _toastUntil) Ui.Toast.gameObject.SetActive(false);
            if (_modal.Count > 0) { if (Input.GetKeyDown(KeyCode.Escape) && _modal.Last() != "Acquire") Pop(); return; }
            if (Input.GetKeyDown(KeyCode.Escape)) { Settings(); return; }
            bool zoomReset = SceneZoomInput();
            if (S.ActiveEvent != 0 && _page == "Explore")
            {
                var active = _data.EventById(S.ActiveEvent);
                bool atEnd = S.Line + 1 >= _data.Lines(active.Plot).Length;
                if (atEnd && (active.ChoiceGroup == 0 || S.QueuedItem != 0) && Engine.RequiredAction() == null)
                {
                    if (_endingDialogue != active.Id) { _endingDialogue = active.Id; _dialogueEndDelay = S.AutoSeconds; }
                    _dialogueEndDelay -= Time.unscaledDeltaTime;
                    if (_dialogueEndDelay <= 0) { _endingDialogue = 0; Engine.Advance(); Refresh(); return; }
                }
                else _endingDialogue = 0;
                if (!zoomReset && Input.GetKeyDown(KeyCode.Space) || S.Auto && Time.unscaledTime >= _nextAuto) Next();
            }
            if (_playing && _page == "Tape")
            {
                if (_playTape != 0)
                {
                    var tape = S.Tapes.First(x => x.Instance == _playTape);
                    if (_position <= tape.ListenedSeconds + .2f) tape.ListenedSeconds = Mathf.Max(tape.ListenedSeconds, _position + Time.unscaledDeltaTime);
                }
                _position += Time.unscaledDeltaTime;
                var record = _data.RecordById(_record);
                if (_position >= record.Duration)
                {
                    _position = record.Duration; _playing = false;

                }
                PlaybackTick();
            }
            if (S.Ending && _page == "Timeline")
                foreach (Transform bar in V("Timeline").Get<RectTransform>("Wave"))
                    bar.localScale = new Vector3(1, .3f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3 + bar.GetSiblingIndex())), 1);
        }
        private void Txt(CastleUiView v, string id, string text) => v.Get<Text>(id).text = text;
        private void Show(CastleUiView v, string id, bool active) => v.Get<Component>(id).gameObject.SetActive(active);
        private void Btn(CastleUiView v, string id, Action action, string label = null, bool enabled = true)
        {
            var b = v.Get<Button>(id); b.onClick.RemoveAllListeners(); if (action != null) b.onClick.AddListener(() => action()); b.interactable = enabled;
            if (label != null) b.GetComponentInChildren<Text>(true).text = label;
        }
        private void Slide(CastleUiView v, string id, float min, float max, float value, Action<float> action)
        {
            var slider = v.Get<Slider>(id); slider.onValueChanged.RemoveAllListeners(); slider.minValue = min; slider.maxValue = max; slider.SetValueWithoutNotify(value); slider.onValueChanged.AddListener(x => action(x));
        }
        private void Clear(Transform root) { foreach (Transform c in root) { c.gameObject.SetActive(false); Destroy(c.gameObject); } }
        private CastleUiView Card(Transform parent, string title, string body, Action action, bool map = false, CastleUiView template = null)
        {
            var card = Instantiate(template ? template : map ? MapCardPrefab : Ui.CardPrefab, parent); card.gameObject.SetActive(true); Txt(card,"Title",title); Txt(card,"Body",body); Btn(card,"Select",action); return card;
        }
        private void Toast(string text) { Txt(Ui.Toast,"Body",text); Ui.Toast.gameObject.SetActive(true); _toastUntil = Time.unscaledTime + 5; }
        private void Push(string id, Action close = null)
        {
            if (_modal.Count == 0 && _page == "Explore") S.DialogueScroll = V("Explore").Get<ScrollRect>("DialogueScroll").verticalNormalizedPosition;
            _modal.Add(id); _returns.Add(close); ModalVisibility();
        }
        private void Pop()
        {
            if (_modal.Count == 0) return;
            int last = _modal.Count - 1; var callback = _returns[last]; _modal.RemoveAt(last); _returns.RemoveAt(last); ModalVisibility(); callback?.Invoke();
            _nextAuto = Time.unscaledTime + S.AutoSeconds;
        }
        private void ModalVisibility()
        {
            Ui.ModalRoot.SetActive(_modal.Count > 0);
            foreach (var v in Ui.Modals) v.gameObject.SetActive(_modal.Count > 0 && v.Id == _modal.Last());
            bool mapOpen=_modal.Contains("Map");
            foreach(var v in Ui.Pages)v.gameObject.SetActive(!mapOpen&&v.Id==_page);
            Ui.Hud.gameObject.SetActive(!mapOpen&&_page!="Title"&&_page!="Invitation"&&_page!="Cutscene"&&_page!="EndCard");
        }
        private void CloseAll() { _modal.Clear(); _returns.Clear(); ModalVisibility(); }
        private void Confirm(string text, Action yes)
        {
            Push("Confirm"); var v = V("Confirm"); Txt(v,"Body",text); Btn(v,"Cancel",Pop); Btn(v,"Confirm",()=>{ Pop(); yes(); });
        }
        private void Page(string id)
        {
            _page = id; if(!Music.isPlaying)Music.Play(); if (id != "Title") S.Page = id;
            foreach (var v in Ui.Pages) v.gameObject.SetActive(v.Id == id);
            Ui.Hud.gameObject.SetActive(id != "Title" && id != "Invitation" && id != "Cutscene" && id != "EndCard");
            Hud();
            switch (id)
            {
                case "Title": Title(); break;
                case "Invitation": Invitation(); break;
                case "Explore": Explore(); break;
                case "Recordings": Recordings(); break;
                case "Anchors": Anchors(); break;
                case "Space": PathPage(); break;
                case "Timeline": Timeline(); break;
                case "Tape": PlaybackRender(); break;
                case "Cutscene": Cutscene(); break;
                case "EndCard": EndCard(); break;
            }
            Save();
        }
        private void Hud()
        {
            Show(Ui.Hud,"LocationGroup",_page!="Recordings"&&_page!="Anchors"&&_page!="Space");
            foreach(var id in new[]{"Back","Map","Journal","Settings"})Ui.Hud.Get<Button>(id).GetComponentInChildren<Text>(true).gameObject.SetActive(false);
            Txt(Ui.Hud,"Location","当前位置：" + Engine.RoomName(S.Room));
            Txt(Ui.Hud,"CurrentTime","当前时间：Day " + S.Day + " · " + Engine.Now.ToString("HH:mm"));
            Btn(Ui.Hud,"Map",()=>Map(),enabled:Engine.Has(_data.GameSettings.MapEntry)); Btn(Ui.Hud,"Journal",()=>Journal()); Btn(Ui.Hud,"Settings",Settings);
            Btn(Ui.Hud,"Back",()=>
            {
                _playing = false;
                if (_page == "Tape") { CloseTape(); return; }
                if (S.Room == _data.GameSettings.ControlRoom && _page != "Explore") { Page("Explore"); return; }
                if (_page == "Timeline" && !S.Ending) { Page("Explore"); Journal(2); return; }
                if (S.Room == _data.GameSettings.ControlRoom && !Engine.Busy) { if (Engine.Move(S.ControlReturn)) Refresh(); }
                else if (!Engine.Busy) Page("Explore");
            }, S.Room==_data.GameSettings.ControlRoom && _page=="Explore" ? "离开 · " + S.ControlCost + "分钟" : "返回", _page=="Tape" || (!Engine.Busy&&(_page!="Explore"||Engine.CanExitControl())));
            Show(Ui.Hud,"Back",_page!="Explore"||S.Room==_data.GameSettings.ControlRoom);
        }
        private void Title()
        {
            var v=V("Title"); Btn(v,"Start",()=>{ if(S.Started||_blocked) Confirm("开始 V2.4 新调查？旧示例存档保留，新版进度将被替换。",StartNew); else StartNew(); });
            Btn(v,"Continue",()=>
            {
                CloseAll();
                Engine.ResumeAction();
                if(S.PendingPuzzle!=0 && S.ActiveEvent==0 && S.RewardPopups.Count==0)
                {
                    if(S.Ending&&!S.EndingTransitionDone){Page("Timeline");StartCoroutine(RevealEnding());return;}
                    Engine.ContinuePuzzleReward();
                }
                _record=S.RecordId; _position=S.RecordPosition; _playTape=S.PlayingTape;_nextAuto=Time.unscaledTime+S.AutoSeconds; Refresh();
            },enabled:S.Started);
            Btn(v,"Settings",Settings); Btn(v,"Credits",()=>Toast("古堡 V2.4 · UI 与角色素材使用提供的素材包；缺失资源为占位。"));
            Btn(v,"Quit",()=>Confirm("退出游戏？",()=>{Save(); Application.Quit();}));
        }
        private void StartNew() { _presentationRoom=0;_sceneZoom=1;_avatarsExpanded=false;_playing=false;_playTape=0;_record=0;_position=0;_blocked=false; CloseAll(); Engine.NewGame(); Page("Invitation"); }
        private void Invitation()
        {
            var v=V("Invitation"); var entry=_data.EntryById(_data.InvitationEntry);
            if(v.TryGet<Image>("Artwork",out var artwork)){artwork.sprite=S.InvitationStage==0?InvitationClosed:InvitationOpen;artwork.preserveAspect=true;}
            Show(v,"Heading",false);Show(v,"Body",false);
            Txt(v,"Heading",S.InvitationStage==0 ? "劳里耶古堡" : entry.Name);
            Txt(v,"Body",S.InvitationStage==0 ? "一封邀请函\n点击展开" : entry.Text+"\n\n点击收起并收录");
            Btn(v,"Surface",()=>
            {
                if(S.InvitationStage==0) { S.InvitationStage=1; Save(); Invitation(); }
                else { S.InvitationStage=2; Engine.Grant(entry.Id,"invitation"); S.Page="Explore"; Engine.Commit(); Refresh(); }
            });
        }
        private void Refresh()
        {
            if(S.RewardPopups.Count>0) { Reward(); return; }
            CloseAll();
            if(S.Finished){Page("EndCard");return;}
            if(S.SleepStage!=""){Page("Cutscene");return;}
            if(S.SleepPending&&!Engine.Busy)Toast(Engine.SleepReason());
            if(S.InvitationStage==2 && !S.CompletedEvents.Contains(_data.GameSettings.ArrivalEvent) && S.ActiveEvent==0) Engine.Start(_data.GameSettings.ArrivalEvent,true);
            if(S.ActiveEvent!=0) Page("Explore");
            else { Engine.Scan(); Page(S.ActiveEvent!=0 ? "Explore" : S.Page=="Invitation" ? "Invitation" : S.Page=="Title" ? "Explore" : S.Page); }
            if(S.RewardPopups.Count>0) Reward();
            else if(S.ReturnPath!=0 && S.ActiveEvent==0 && S.PendingPuzzle==0)
            {
                _puzzle=S.ReturnPath; S.ReturnPath=0;
                Journal(0,_data.PuzzleById(_puzzle).Target,true);Push("Path",JournalRender);PathRender(V("Path"));
            }
            if(Engine.IntroDone&&!S.RepairNoticeShown){S.RepairNoticeShown=true;Save();Toast(_data.ControlRoomPolicy.RepairToast);}
        }
        private void Reward()
        {
            if(_modal.LastOrDefault()=="Acquire") return;
            Push("Acquire"); var entry=_data.EntryById(S.RewardPopups[0]); var v=V("Acquire");
            Txt(v,"Name",entry.Name); Txt(v,"Body",entry.Text); Txt(v,"Icon",entry.Type=="tape"?"磁带":"道具"); Txt(v,"Filed","已收入调查册 · 道具");
            Btn(v,"Confirm",()=>{Pop();Engine.CloseReward();Refresh();});
        }
        private void Settings()
        {
            Push("Settings"); var v=V("Settings");
            Slide(v,"Speed",1,8,S.AutoSeconds,x=>{S.AutoSeconds=x;Txt(v,"SpeedValue",x.ToString("0.0")+" 秒 / 句");Save();}); Txt(v,"SpeedValue",S.AutoSeconds.ToString("0.0")+" 秒 / 句");
            Slide(v,"Volume",0,1,S.Volume,x=>{S.Volume=x;Music.volume=x;Effects.volume=x;Save();});
            Btn(v,"Motion",()=>Toast("翻页使用书页过渡，内容与选择在关闭子页面后保留。"),"调查册 · 书页过渡"); Btn(v,"Close",Pop);
            Btn(v,"Title",()=>{Save();CloseAll();_playing=false;Page("Title");});
            Btn(v,"SaveGame",()=>
            {
                try { V24Save.Write(S,_test?"Logs/v24-manual-test.json":V24Save.Path+".manual"); Toast("已保存当前进度（含待选选项）"); }
                catch(Exception ex) { Toast("保存失败："+ex.Message); }
            },"保存进度",S.Started);
            Btn(v,"LoadGame",()=>Confirm("读取手动存档？当前进度将被替换。",()=>
            {
                try
                {
                    var state=V24Save.Read(_test?"Logs/v24-manual-test.json":V24Save.Path+".manual");
                    if(!state.Started){Toast("尚无手动存档");return;}
                    Engine.State=state;S.Auto=false;_playing=false;_endingDialogue=0;
                    _record=S.RecordId;_position=S.RecordPosition;_playTape=S.PlayingTape;
                    Music.volume=S.Volume;Effects.volume=S.Volume;
                    CloseAll();Engine.ResumeAction();
                    if(S.PendingPuzzle!=0 && S.ActiveEvent==0 && S.RewardPopups.Count==0)
                    {
                        if(S.Ending&&!S.EndingTransitionDone){Page("Timeline");StartCoroutine(RevealEnding());return;}
                        Engine.ContinuePuzzleReward();
                    }
                    Refresh();
                }
                catch(Exception ex){Toast("读取失败："+ex.Message);}
            }),"读取进度");
        }
        private void CloseTape()
        {
            int instance=_playTape;
            _playing=false;_playTape=0;S.PlayingTape=0;
            if(instance!=0)Engine.TapeCompleted(instance);
            Page("Explore");Refresh();
        }
    }
}

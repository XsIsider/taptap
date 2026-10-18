using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Castle.Flow
{
    public sealed partial class CastleFlowController : MonoBehaviour
    {
        public CastleFlowContent Content;
        public CastleFlowUi Ui;
        public AudioSource Audio;
        public CastleFlowSession Session { get; private set; }
        private string _page = "Title";
        private readonly List<string> _modals = new List<string>();
        private Action _confirm;
        private bool _auto, _playing, _smoke, _loadBlocked;
        private float _nextLineAt, _toastUntil, _position;

        private void Awake()
        {
            Content.ValidateContent();
            Ui.ValidateBindings();
            _smoke = Environment.GetCommandLineArgs().Contains("-castleFlowSmoke");
            string error = null;
            Session = _smoke ? new CastleFlowSession() : CastleFlowSave.Load(CastleFlowSave.Path, out error);
            _loadBlocked = error != null;
            Ui.Toast.gameObject.SetActive(false);
            ShowPage("Title", false);
            if (error != null) Toast(error + "；开始新游戏前会确认替换。");
            if (_smoke) StartCoroutine(RunFlowSmoke());
        }

        private void Update()
        {
            if (Ui.Toast.gameObject.activeSelf && Time.unscaledTime > _toastUntil) Ui.Toast.gameObject.SetActive(false);
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_modals.Count > 0) { if (_modals.Last() != "Acquire") PopModal(); }
                else OpenSettings();
            }
            if (_modals.Count != 0) return;
            if (_page != "Title" && _page != "Invitation" && Input.GetKeyDown(KeyCode.J)) OpenJournal();
            if (_page == "Explore" && Input.GetKeyDown(KeyCode.M)) OpenMap();
            if (_page == "Explore" && !string.IsNullOrEmpty(Session.ActiveDialogue))
            {
                if (Input.GetKeyDown(KeyCode.Space)) Advance();
                if (_auto && Time.unscaledTime >= _nextLineAt) Advance(true);
            }
            if (_playing && (_page == "Recordings" || _page == "Timeline"))
            {
                _position += Time.unscaledDeltaTime;
                if (_page == "Recordings") UpdateRecordingPlayback(); else UpdateEndingPlayback();
            }
        }

        private void OnApplicationPause(bool paused) { if (paused) Save(); }
        private void OnApplicationQuit() => Save();
        private void Save()
        {
            if (_smoke || _loadBlocked || Session == null || !Session.Started) return;
            if (!CastleFlowSave.Write(Session, CastleFlowSave.Path, out var error)) Toast(error);
        }

        private CastleUiView V(string id) => Ui.View(id);
        private void Text(CastleUiView view, string id, string text) => view.Get<Text>(id).text = text;
        private void Visible(CastleUiView view, string id, bool active) => view.Get<Component>(id).gameObject.SetActive(active);
        private void Button(CastleUiView view, string id, Action action, string label = null, bool enabled = true)
        {
            var button = view.Get<Button>(id);
            button.onClick.RemoveAllListeners();
            if (action != null) button.onClick.AddListener(() => action());
            button.interactable = enabled;
            if (label != null) button.GetComponentInChildren<Text>().text = label;
        }

        private void Slider(CastleUiView view, string id, float min, float max, float value, Action<float> changed)
        {
            var slider = view.Get<Slider>(id);
            slider.onValueChanged.RemoveAllListeners();
            slider.minValue = min; slider.maxValue = max;
            slider.SetValueWithoutNotify(value);
            slider.onValueChanged.AddListener(number => changed(number));
        }

        private void ShowPage(string id, bool save = true, bool captureDialogue = true)
        {
            PausePlayback(captureDialogue);
            _modals.Clear(); RefreshModals();
            _page = id;
            if (id != "Title") Session.Page = id;
            foreach (var view in Ui.Pages) view.gameObject.SetActive(view.Id == id);
            Ui.Hud.gameObject.SetActive(id != "Title" && id != "Invitation");
            RenderHud();
            switch (id)
            {
                case "Title": RenderTitle(); break;
                case "Invitation": RenderInvitation(); break;
                case "Explore": RenderExplore(); break;
                case "Recordings": RenderRecordings(); break;
                case "Anchors": RenderAnchors(); break;
                case "Space": RenderSpace(); break;
                case "Timeline": RenderTimeline(); break;
            }
            if (save) Save();
        }

        private void RenderHud()
        {
            Text(Ui.Hud, "Location", (Content.Room(Session.RoomId)?.Name ?? "古堡") + "  /  " + CastleRules.Clock(Session.Minutes));
            Button(Ui.Hud, "Journal", () => OpenJournal());
            Button(Ui.Hud, "Map", () => OpenMap(), enabled: Session.Entries.Contains(Content.MapId));
            Button(Ui.Hud, "Settings", OpenSettings);
            Button(Ui.Hud, "Back", () => ShowPage("Explore"));
            Visible(Ui.Hud, "Back", _page != "Explore");
        }

        private void PausePlayback(bool captureDialogue = true)
        {
            _auto = false; _playing = false;
            Audio.Pause();
            if (_page == "Explore" && !string.IsNullOrEmpty(Session?.ActiveDialogue))
            {
                if (captureDialogue) Session.Progress(Session.ActiveDialogue).Scroll = V("Explore").Get<ScrollRect>("DialogueScroll").verticalNormalizedPosition;
                Text(V("Explore"), "AutoLabel", "自动：关闭");
            }
            if (_page == "Recordings" || _page == "Timeline") Text(V(_page), "PlayLabel", "播放");
        }

        private CastleUiView PushModal(string id)
        {
            PausePlayback();
            _modals.Remove(id); _modals.Add(id); RefreshModals();
            return V(id);
        }

        private void PopModal()
        {
            if (_modals.Count == 0) return;
            _modals.RemoveAt(_modals.Count - 1); RefreshModals();
            if (_modals.Count == 0) RenderHud();
        }

        private void RefreshModals()
        {
            Ui.ModalRoot.SetActive(_modals.Count > 0);
            foreach (var view in Ui.Modals) view.gameObject.SetActive(_modals.Count > 0 && view.Id == _modals.Last());
        }

        private void Toast(string message)
        {
            Ui.Toast.gameObject.SetActive(true);
            Text(Ui.Toast, "Body", message); _toastUntil = Time.unscaledTime + 5;
        }

        private void Confirm(string message, Action action)
        {
            var view = PushModal("Confirm"); _confirm = action;
            Text(view, "Body", message);
            Button(view, "Cancel", PopModal);
            Button(view, "Confirm", () => { var callback = _confirm; _confirm = null; PopModal(); callback?.Invoke(); });
        }

        private void RenderTitle()
        {
            var view = V("Title");
            Button(view, "Start", () =>
            {
                if (Session.Started || _loadBlocked) Confirm("开始新的调查将替换本 UI 框架的进度。旧版存档不受影响。", NewGame);
                else NewGame();
            });
            Button(view, "Continue", ResumeGame, enabled: Session.Started);
            Button(view, "Settings", OpenSettings);
            Button(view, "Credits", () => Toast("UI 交互框架 · 原型素材来自项目已有资源；本轮内容为接入示例。"));
            Button(view, "Quit", () => Confirm("退出游戏？", () => {
                Save();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }));
        }

        private void NewGame()
        {
            _loadBlocked = false;
            Session = CastleFlowSession.New(Content);
            _selectedRoom = null; _recordingIndex = 0; _position = 0;
            ShowPage("Invitation");
        }

        private void ResumeGame()
        {
            ShowPage(Session.Page);
            if (Session.Page == "Invitation" && Session.Entries.Contains(Content.InvitationId))
                Acquire(Content.InvitationId, BeginOpening, true);
            else if (!string.IsNullOrEmpty(Session.ActiveDialogue) && Session.Progress(Session.ActiveDialogue).Completed)
                CompleteDialogue();
        }

        private void RenderInvitation()
        {
            var view = V("Invitation");
            Text(view, "Heading", Session.InvitationExpanded ? "致受邀的调查者" : "一封来自古堡的邀请函");
            Text(view, "Body", Session.InvitationExpanded ? Content.Entry(Content.InvitationId).Body + "\n\n点击任意区域收起邀请函" : "封蜡尚未开启\n\n点击邀请函展开");
            Button(view, "Surface", () =>
            {
                if (!Session.InvitationExpanded) { Session.InvitationExpanded = true; Save(); RenderInvitation(); }
                else Acquire(Content.InvitationId, BeginOpening, true);
            });
        }

        private void BeginOpening()
        {
            Session.RoomId = Content.InitialRoom;
            StartDialogue(Content.OpeningDialogue);
        }

        private void Acquire(string id, Action completed = null, bool showOwned = false)
        {
            if (!Session.Collect(id) && !showOwned) { completed?.Invoke(); return; }
            Session.PendingClaims.Remove(id); Save();
            var entry = Content.Entry(id); var view = PushModal("Acquire");
            Text(view, "Name", entry.Name); Text(view, "Body", entry.Body);
            Text(view, "Icon", entry.Kind == EntryKind.Clue ? "线索" : entry.Id == Content.InvitationId ? "邀请函" : entry.Id == Content.MapId ? "地图" : "磁带");
            Text(view, "Filed", "已收入调查册 · " + (entry.Kind == EntryKind.Clue ? "记录" : "道具"));
            Button(view, "Confirm", () => { PopModal(); RefreshVisibleDialogueLinks(); RenderHud(); completed?.Invoke(); });
        }

        private void OpenSettings()
        {
            var view = PushModal("Settings");
            Slider(view, "Speed", 1, 8, Session.AutoSeconds, value => { Session.AutoSeconds = value; Text(view, "SpeedValue", value.ToString("0.0") + " 秒 / 句"); Save(); });
            Text(view, "SpeedValue", Session.AutoSeconds.ToString("0.0") + " 秒 / 句");
            Slider(view, "Volume", 0, 1, Session.Volume, value => { Session.Volume = value; Audio.volume = value; Save(); });
            Button(view, "Motion", () => { Session.ReduceMotion = !Session.ReduceMotion; PopModal(); OpenSettings(); Save(); }, Session.ReduceMotion ? "翻页动效：关闭" : "翻页动效：开启");
            Button(view, "Close", PopModal);
            Button(view, "Title", () => { Save(); ShowPage("Title", false); });
        }

        private static void Clear(Transform parent)
        {
            foreach (Transform child in parent) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        }

        private CastleUiView Card(Transform parent, string title, string body, Action action)
        {
            var view = Instantiate(Ui.CardPrefab, parent); view.gameObject.SetActive(true);
            Text(view, "Title", title); Text(view, "Body", body);
            Button(view, "Select", action); return view;
        }
    }
}

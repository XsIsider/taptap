using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
namespace Castle
{
    public sealed partial class CastleGame : MonoBehaviour
    {
        public CastleDatabase database;
        public CastleSceneUi ui;
        public CastleState State => state;
        CastleState state;
        ScreenId page = ScreenId.Title;
        RectTransform root, overlay;
        AudioSource audioSource;
        AudioClip clickSound;
        readonly Stack<string> undo = new Stack<string>();
        readonly List<RectTransform> waveBars = new List<RectTransform>();
        readonly List<Button> transcriptButtons = new List<Button>();
        readonly Dictionary<Button, Action> buttonActions = new Dictionary<Button, Action>();
        readonly Dictionary<Button, UnityAction> buttonListeners = new Dictionary<Button, UnityAction>();
        readonly Dictionary<Button, Color> buttonColors = new Dictionary<Button, Color>();
        readonly Dictionary<Text, Color> textColors = new Dictionary<Text, Color>();
        readonly Dictionary<Slider, Action<float>> sliderActions = new Dictionary<Slider, Action<float>>();
        readonly Dictionary<Slider, UnityAction<float>> sliderListeners = new Dictionary<Slider, UnityAction<float>>();
        int file, floor = 1, selectedNode, selectedLine = -1;
        bool playing, dirty, smokeTest, initialized;
        float playPosition = 291, saveAt, toastUntil;
        Text playLabel, offsetLabel, toastLabel;
        Slider progressSlider, offsetSlider;
        RectTransform movingAnchor;
        string journalTab = "人物", selectedRoom;
        Action overlayBack, messageAction;
        void Awake()
        {
            if (!ui)
            {
                if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "CastlePrototype")
                    UnityEngine.SceneManagement.SceneManager.LoadScene("CastleEditable");
                else Debug.LogError("CastleGame 未绑定 CastleSceneUi，请打开 CastleEditable 场景。", this);
                enabled = false; return;
            }
            try { ui.ValidateBindings(); }
            catch (Exception ex) { Debug.LogException(ex, this); enabled = false; return; }
            smokeTest = Array.IndexOf(Environment.GetCommandLineArgs(), "-castleSmoke") >= 0;
            state = smokeTest ? new CastleState() : CastleSave.Load();
            if (!database) database = Resources.Load<CastleDatabase>("Castle/Database");
            root = ui.Stage; audioSource = ui.Audio;
            clickSound = AudioClip.Create("Interface click", 2205, 1, 44100, false);
            var samples = new float[2205];
            for (int i = 0; i < samples.Length; i++) samples[i] = Mathf.Sin(i * .075f) * .06f * (1 - i / (float)samples.Length);
            clickSound.SetData(samples, 0); waveBars.AddRange(ui.WaveBars);
            ui.ToastRoot.SetActive(false); CloseOverlay(); BindScrollPositions(); initialized = true; Render();
            if (CastleSave.LastError != null) Toast(CastleSave.LastError);
            if (smokeTest) StartCoroutine(RunSmokeTest());
        }
        void Update()
        {
            if (dirty && Time.unscaledTime >= saveAt) Save();
            if (toastLabel && Time.unscaledTime > toastUntil) { ui.ToastRoot.SetActive(false); toastLabel = null; }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (overlay) { if (overlayBack != null) overlayBack(); else CloseOverlay(); }
                else if (page == ScreenId.Invitation) { state.invitation = true; Go(ScreenId.Collected); }
                else Settings();
            }
            if (overlay) return;
            if (Input.GetKeyDown(KeyCode.J) && page != ScreenId.Title) { Journal(); return; }
            if (Input.GetKeyDown(KeyCode.M) && IsScene()) { Navigation(); return; }
            if (Input.GetKeyDown(KeyCode.Space) && page == ScreenId.Lounge && !state.recorded) AdvanceDialogue();
            if (page == ScreenId.Align && (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow))) SetOffsetLive(state.offset + (Input.GetKeyDown(KeyCode.RightArrow) ? 1 : -1));
            if (playing && page == ScreenId.Browse && file < 2)
            {
                playPosition = Mathf.Min(file == 0 ? 480 : 720, playPosition + Time.unscaledDeltaTime);
                selectedLine = playPosition < 300 ? 0 : playPosition < 312 ? 1 : playPosition < 324 ? 2 : 3;
                if (playPosition >= (file == 0 ? 480 : 720)) playing = false;
                RefreshPlayback();
            }
            if (page == ScreenId.Truth)
                for (int i = 0; i < waveBars.Count; i++) waveBars[i].localScale = new Vector3(1, state.reduceMotion ? 1 : .8f + .2f * Mathf.Sin(Time.unscaledTime * 2 + i * .2f), 1);
        }
        void OnApplicationPause(bool paused) { if (paused && state != null) Save(); }
        void OnApplicationQuit() { if (state != null) Save(); }
        void OnDestroy()
        {
            foreach (var pair in buttonListeners) if (pair.Key) pair.Key.onClick.RemoveListener(pair.Value);
            foreach (var pair in sliderListeners) if (pair.Key) pair.Key.onValueChanged.RemoveListener(pair.Value);
            if (initialized && ui)
            {
                ui.Page(ScreenId.Browse).Get<ScrollRect>("TranscriptScroll").onValueChanged.RemoveListener(OnTranscriptScrolled);
                ui.Overlay("Journal").Get<ScrollRect>("GridScroll").onValueChanged.RemoveListener(OnJournalScrolled);
                ui.Page(ScreenId.Align).Get<CastleTimelineDrag>("TrackDrag").onDrag = null;
            }
            if (clickSound) Destroy(clickSound);
        }
        bool IsScene() => page == ScreenId.Outside || page == ScreenId.Lounge || page == ScreenId.Room;
        void Save() { dirty = false; if (!smokeTest && !CastleSave.Write(state)) Toast(CastleSave.LastError); }
        void Changed() { dirty = true; saveAt = Time.unscaledTime + .4f; }
        void Go(ScreenId target)
        {
            CloseOverlay(); page = target; playing = false;
            if (target != ScreenId.Title && target != ScreenId.Invitation && target != ScreenId.Collected) state.resume = target;
            Save(); Render();
        }
        void Check(string error, Action success)
        {
            if (error != null) { Message("当前判断尚不能成立", error + "\n\n草稿已保留，可以继续调整。", null); return; }
            Save(); success();
        }
        void Render()
        {
            foreach (var view in ui.Pages) view.gameObject.SetActive(view.Id == page.ToString());
            ui.SceneHud.gameObject.SetActive(IsScene());
            switch (page)
            {
                case ScreenId.Title: Title(); break;
                case ScreenId.Outside: Outside(); break;
                case ScreenId.Invitation: Invitation(); break;
                case ScreenId.Collected: Collected(); break;
                case ScreenId.Lounge: Lounge(); break;
                case ScreenId.Room: Room(); break;
                case ScreenId.Browse: Browse(); break;
                case ScreenId.Align: Align(); break;
                case ScreenId.Sound: Sound(); break;
                case ScreenId.Event: EventView(); break;
                case ScreenId.Identity: Identity(); break;
                case ScreenId.Route: Route(); break;
                case ScreenId.Final: FinalView(); break;
                case ScreenId.Truth: Truth(); break;
                case ScreenId.Choice: Choice(); break;
                case ScreenId.Ending: Ending(); break;
            }
            if (IsScene()) Hud();
        }
        CastleUiView Current => ui.Page(page);
        void T(CastleUiView v, string id, string text) { v.Get<Text>(id).text = text; }
        void Visible(CastleUiView v, string id, bool show) { v.Get<Component>(id).gameObject.SetActive(show); }
        void Picture(CastleUiView v, string id, string resource) { v.Get<RawImage>(id).texture = Resources.Load<Texture2D>("Castle/" + resource); }
        void B(CastleUiView v, string id, Action action, string label = null, bool selected = false, bool interactable = true)
        {
            var button = v.Get<Button>(id); var text = v.Get<Text>(id + "Label");
            if (label != null) text.text = label;
            BindButton(button, action); button.interactable = interactable; Select(button, text, selected);
        }
        void BindButton(Button button, Action action)
        {
            buttonActions[button] = action;
            if (buttonListeners.ContainsKey(button)) return;
            UnityAction listener = () => { audioSource.PlayOneShot(clickSound, state.volume); buttonActions[button]?.Invoke(); };
            buttonListeners.Add(button, listener); button.onClick.AddListener(listener);
        }
        void Select(Button button, Text text, bool selected)
        {
            if (!buttonColors.ContainsKey(button)) buttonColors.Add(button, button.targetGraphic.color);
            if (!textColors.ContainsKey(text)) textColors.Add(text, text.color);
            button.targetGraphic.color = selected ? ui.SelectedColor : buttonColors[button];
            text.color = selected ? ui.SelectedTextColor : textColors[text];
        }
        void S(CastleUiView v, string id, float min, float max, float value, Action<float> change)
        {
            var slider = v.Get<Slider>(id); slider.minValue = min; slider.maxValue = max;
            slider.SetValueWithoutNotify(value); sliderActions[slider] = change;
            if (sliderListeners.ContainsKey(slider)) return;
            UnityAction<float> listener = n => sliderActions[slider](n);
            sliderListeners.Add(slider, listener); slider.onValueChanged.AddListener(listener);
        }
        void Header()
        {
            var v = Current;
            B(v, "BackButton", () => Go(ScreenId.Room)); B(v, "BrowseButton", () => Go(ScreenId.Browse), selected: page == ScreenId.Browse);
            B(v, "AlignButton", () => Go(ScreenId.Align), selected: page == ScreenId.Align); B(v, "SoundButton", () => Go(ScreenId.Sound), selected: page == ScreenId.Sound);
            B(v, "JournalButton", Journal); B(v, "SettingsButton", Settings);
        }
        CastleUiView OpenOverlay(string id)
        {
            CloseOverlay(); playing = false; ui.OverlayRoot.SetActive(true);
            var v = ui.Overlay(id); v.gameObject.SetActive(true); overlay = (RectTransform)v.transform; return v;
        }
        void CloseOverlay()
        {
            overlay = null; overlayBack = null; messageAction = null;
            if (!ui) return;
            foreach (var v in ui.Overlays) v.gameObject.SetActive(false);
            ui.OverlayRoot.SetActive(false);
        }
        void Toast(string text)
        {
            if (!ui) return;
            ui.ToastRoot.SetActive(true); toastLabel = ui.ToastText; toastLabel.text = text; toastUntil = Time.unscaledTime + 4;
        }
        void Message(string title, string body, Action confirm)
        {
            var v = OpenOverlay("Message"); messageAction = confirm;
            T(v, "TitleText", title); T(v, "BodyText", body);
            B(v, "ConfirmButton", () => { var action = messageAction; CloseOverlay(); action?.Invoke(); }, confirm == null ? "继续调查" : "确认");
            Visible(v, "CancelButton", confirm != null); B(v, "CancelButton", CloseOverlay);
        }
    }
}

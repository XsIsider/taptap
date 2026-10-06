using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Castle
{
    public sealed partial class CastleGame : MonoBehaviour
    {
        public CastleDatabase database;
        public CastleState State { get { return state; } }
        CastleState state;
        ScreenId page = ScreenId.Title;
        RectTransform root, overlay;
        Font font;
        AudioSource audioSource;
        AudioClip clickSound;
        readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        readonly Stack<string> undo = new Stack<string>();
        readonly List<RectTransform> waveBars = new List<RectTransform>();
        int file, floor = 1, selectedNode, selectedLine = -1;
        bool playing, dirty;
        float playPosition = 291, saveAt;
        Text playLabel, offsetLabel;
        Slider progressSlider;
        Slider offsetSlider;
        readonly List<Button> transcriptButtons = new List<Button>();
        RectTransform movingAnchor;
        Text toastLabel;
        float toastUntil;
        bool smokeTest;
        string journalTab = "事件", selectedRoom;

        static readonly Color Ink = new Color32(22, 34, 27, 255);
        static readonly Color Panel = new Color32(28, 43, 33, 250);
        static readonly Color Gold = new Color32(210, 181, 127, 255);
        static readonly Color Paper = new Color32(229, 220, 199, 255);
        static readonly Color Muted = new Color32(166, 182, 157, 255);
        static readonly Color Light = new Color32(233, 227, 207, 255);

        void Awake()
        {
            smokeTest = Array.IndexOf(Environment.GetCommandLineArgs(), "-castleSmoke") >= 0;
            state = smokeTest ? new CastleState() : CastleSave.Load();
            if (!database) database = Resources.Load<CastleDatabase>("Castle/Database");
            if (!database) database = ScriptableObject.CreateInstance<CastleDatabase>();
            font = Resources.Load<Font>("Castle/Chinese");
            if (!font) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasObject = new GameObject("Castle Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var backdrop = new GameObject("Letterbox", typeof(RectTransform), typeof(Image));
            backdrop.transform.SetParent(canvasObject.transform, false);
            var br = (RectTransform)backdrop.transform;
            br.anchorMin = Vector2.zero; br.anchorMax = Vector2.one; br.offsetMin = br.offsetMax = Vector2.zero;
            backdrop.GetComponent<Image>().color = Color.black;
            var stage = new GameObject("Stage 1600x900", typeof(RectTransform));
            root = (RectTransform)stage.transform;
            root.SetParent(canvasObject.transform, false);
            root.sizeDelta = new Vector2(1600, 900);
            if (!FindObjectOfType<EventSystem>()) new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            clickSound = AudioClip.Create("Interface click", 2205, 1, 44100, false);
            var samples = new float[2205];
            for (int i = 0; i < samples.Length; i++) samples[i] = Mathf.Sin(i * .075f) * .06f * (1 - i / (float)samples.Length);
            clickSound.SetData(samples, 0);
            Render();
            if (CastleSave.LastError != null) Toast(CastleSave.LastError);
            if (smokeTest) StartCoroutine(RunSmokeTest());
        }

        void Update()
        {
            if (dirty && Time.unscaledTime >= saveAt) Save();
            if (toastLabel && Time.unscaledTime > toastUntil) { Destroy(toastLabel.transform.parent.gameObject); toastLabel = null; }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (overlay) CloseOverlay();
                else if (page == ScreenId.Invitation) { state.invitation = true; Go(ScreenId.Collected); }
                else Settings();
            }
            if (overlay) return;
            if (Input.GetKeyDown(KeyCode.J) && page != ScreenId.Title) Journal();
            if (Input.GetKeyDown(KeyCode.M) && IsScene()) Navigation();
            if (Input.GetKeyDown(KeyCode.Space) && page == ScreenId.Lounge && !state.recorded) AdvanceDialogue();
            if (page == ScreenId.Align && (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow)))
            { CastleRules.ChangeTime(state, state.offset + (Input.GetKeyDown(KeyCode.RightArrow) ? 1 : -1)); Save(); Render(); }
            if (playing && page == ScreenId.Browse && file < 2)
            {
                playPosition = Mathf.Min(file == 0 ? 480 : 720, playPosition + Time.unscaledDeltaTime);
                if (progressSlider) progressSlider.SetValueWithoutNotify(playPosition);
                if (playLabel) playLabel.text = PlaybackText();
                int line = playPosition < 300 ? 0 : playPosition < 312 ? 1 : playPosition < 324 ? 2 : 3;
                if (line != selectedLine) { selectedLine = line; Render(); }
                if (playPosition >= (file == 0 ? 480 : 720)) { playing = false; Render(); }
            }
            if (page == ScreenId.Truth && !state.reduceMotion)
                for (int i = 0; i < waveBars.Count; i++) if (waveBars[i]) waveBars[i].localScale = new Vector3(1, .8f + .2f * Mathf.Sin(Time.unscaledTime * 2 + i * .2f), 1);
        }

        void OnApplicationPause(bool paused) { if (paused && state != null) Save(); }
        void OnApplicationQuit() { if (state != null) Save(); }
        void OnDestroy() { if (clickSound) Destroy(clickSound); }
        bool IsScene() { return page == ScreenId.Outside || page == ScreenId.Lounge || page == ScreenId.Room; }
        void Save() { dirty = false; if (!smokeTest && !CastleSave.Write(state)) Toast(CastleSave.LastError); }
        void Changed() { dirty = true; saveAt = Time.unscaledTime + .4f; }
        void Go(ScreenId target)
        {
            page = target; playing = false; overlay = null;
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
            for (int i = root.childCount - 1; i >= 0; i--) { root.GetChild(i).gameObject.SetActive(false); Destroy(root.GetChild(i).gameObject); }
            overlay = null; toastLabel = null; waveBars.Clear();
            Box(root, "Background", 0, 0, 1600, 900, Ink);
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
        }

        RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)obj.transform; rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y); rt.sizeDelta = new Vector2(w, h);
            return rt;
        }
        RectTransform Box(Transform parent, string name, float x, float y, float w, float h, Color color)
        { var r = Rect(parent, name, x, y, w, h); r.gameObject.AddComponent<Image>().color = color; return r; }
        Text Label(Transform parent, string text, float x, float y, float w, float h, int size = 22, Color? color = null, TextAnchor align = TextAnchor.UpperLeft)
        {
            var r = Rect(parent, "Text", x, y, w, h); var t = r.gameObject.AddComponent<Text>();
            t.font = font; t.text = text; t.fontSize = size; t.color = color ?? Light; t.alignment = align;
            t.raycastTarget = false; t.supportRichText = false; t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate;
            t.lineSpacing = 1.05f;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = Mathf.Max(12, size - 6);
            t.resizeTextMaxSize = size;
            return t;
        }
        Button Button(Transform parent, string text, float x, float y, float w, float h, Action action, bool primary = false, bool enabled = true, int size = 22)
        {
            var r = Box(parent, text, x, y, w, h, primary ? Gold : new Color32(40, 60, 44, 255));
            var outline = r.gameObject.AddComponent<Outline>(); outline.effectColor = primary ? Gold : new Color32(94, 113, 78, 255); outline.effectDistance = new Vector2(1, -1);
            var b = r.gameObject.AddComponent<Button>(); b.targetGraphic = r.GetComponent<Image>();
            var colors = b.colors; colors.highlightedColor = new Color(1.2f, 1.2f, 1.12f); colors.pressedColor = new Color(.75f, .8f, .7f); colors.disabledColor = new Color(.5f,.5f,.5f,.6f); b.colors = colors;
            b.interactable = enabled;
            Label(r, text, 12, 4, w - 24, h - 8, size, primary ? Ink : Light, TextAnchor.MiddleCenter);
            b.onClick.AddListener(() => { audioSource.PlayOneShot(clickSound, state.volume); action(); });
            return b;
        }
        void Picture(Transform parent, string name, float x, float y, float w, float h)
        {
            if (!textures.TryGetValue(name, out var texture)) { texture = Resources.Load<Texture2D>("Castle/" + name); textures[name] = texture; }
            if (!texture) return;
            var r = Rect(parent, name, x, y, w, h); var image = r.gameObject.AddComponent<RawImage>(); image.texture = texture; image.raycastTarget = false;
        }
        Slider Slider(Transform parent, float x, float y, float w, float min, float max, float value, Action<float> change)
        {
            var r = Rect(parent, "Slider", x, y, w, 38);
            Box(r,"Track",0,16,w,6,new Color32(95,113,79,255));
            var area = Rect(r,"Handle area",10,0,w-20,38);
            var handle = Box(area,"Handle",0,0,20,38,Gold);
            handle.pivot = new Vector2(.5f,.5f);
            handle.sizeDelta = new Vector2(20,0);
            var slider = r.gameObject.AddComponent<Slider>(); slider.handleRect = handle; slider.targetGraphic = handle.GetComponent<Image>();
            slider.minValue = min; slider.maxValue = max; slider.wholeNumbers = true; slider.SetValueWithoutNotify(value);
            slider.onValueChanged.AddListener(v => change(v)); return slider;
        }
        void Header(string section, string title)
        {
            Box(root,"Header",0,0,1600,84,Panel);
            Button(root,"‹ 中控室",25,18,135,48,()=>Go(ScreenId.Room),size:19);
            Label(root,"声音档案工作台",186,25,260,48,25,Gold);
            Button(root,"录音浏览",470,18,154,48,()=>Go(ScreenId.Browse),page == ScreenId.Browse);
            Button(root,"锚点对齐",642,18,154,48,()=>Go(ScreenId.Align),page == ScreenId.Align);
            Button(root,"空间推理",814,18,154,48,()=>Go(ScreenId.Sound),page == ScreenId.Sound);
            Button(root,"调查册 J",1270,18,150,48,Journal);
            Button(root,"设置",1438,18,132,48,Settings);
            Label(root,section,32,106,1180,28,17,Muted);
            Label(root,title,32,140,1250,48,30,Gold);
            Label(root,"调查进行中 · 时间暂停",1290,120,275,36,18,Muted);
            Box(root,"Footer",0,809,1600,91,Panel);
        }
        void Hint(string text) { Label(root,text,32,831,990,52,19,Muted,TextAnchor.MiddleLeft); }
        RectTransform Card(float x, float y, float w, float h, bool paper = false)
        { var r = Box(root,"Panel",x,y,w,h,paper ? Paper : Panel); return r; }
        void Toast(string text)
        {
            if (!root) return;
            if (toastLabel) Destroy(toastLabel.transform.parent.gameObject);
            var r = Box(root,"Notification",350,745,900,58,new Color32(49,77,53,255));
            toastLabel = Label(r,text,18,8,864,44,20,Light,TextAnchor.MiddleCenter); toastUntil = Time.unscaledTime + 4;
        }
        RectTransform Overlay(string title)
        {
            CloseOverlay(); playing = false;
            overlay = Box(root,title,0,0,1600,900,new Color(0.02f,.04f,.03f,.9f));
            return overlay;
        }
        void CloseOverlay() { if (overlay) { overlay.gameObject.SetActive(false); Destroy(overlay.gameObject); overlay = null; } }
        void Message(string title, string body, Action confirm)
        {
            var p = Overlay(title); Box(p,"Dialog",390,230,820,450,Panel);
            Label(p,title,440,270,720,60,32,Gold); Label(p,body,440,360,720,180,24);
            Button(p,confirm == null ? "继续调查" : "确认",850,586,300,56,()=>{CloseOverlay(); confirm?.Invoke();},true);
            if (confirm != null) Button(p,"取消",440,586,250,56,CloseOverlay);
        }
    }
}

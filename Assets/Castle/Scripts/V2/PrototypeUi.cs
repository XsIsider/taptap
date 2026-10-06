using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Castle.V2
{
    public sealed partial class PrototypeUi
    {
        readonly CastleGame _host;
        readonly SessionService _session;
        readonly UiFactory _ui;
        readonly RectTransform _root;
        RectTransform _page, _modal;
        TMP_Text _status, _dialogue, _speaker, _playText, _playClock;
        Button _continue;
        RectTransform _endingCursor;
        TMP_Text _endingSummary;
        Slider _seek;
        AudioSource _audio;
        AudioClip _placeholder;
        readonly List<RectTransform> _waves = new List<RectTransform>();
        string _screen, _record, _tape, _lastSound, _puzzle;
        int _shownLine = -1, _floor = 1, _slot;
        bool _playing;
        Action _back;
        public PrototypeUi(CastleGame host, SessionService session)
        {
            _host = host; _session = session;
            var font = Resources.Load<TMP_FontAsset>("Castle/ChineseTMP"); _ui = new UiFactory(font);
            var canvas = new GameObject("Castle V2 Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); canvas.transform.SetParent(host.transform, false); canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1600, 900); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var background = _ui.Box(canvas.transform, "Letterbox", 0, 0, 1600, 900, UiFactory.Ink); background.anchorMin = Vector2.zero; background.anchorMax = Vector2.one; background.offsetMin = background.offsetMax = Vector2.zero;
            _root = new GameObject("Stage", typeof(RectTransform)).GetComponent<RectTransform>(); _root.SetParent(canvas.transform, false); _root.sizeDelta = new Vector2(1600, 900);
            if (!UnityEngine.Object.FindObjectOfType<EventSystem>()) new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            _audio = host.gameObject.AddComponent<AudioSource>(); _audio.playOnAwake = false;
            _placeholder = AudioClip.Create("Replaceable sound cue", 6000, 1, 24000, false); var samples = new float[6000]; for (int i = 0; i < samples.Length; i++) samples[i] = Mathf.Sin(i * .07f) * .12f * (1 - i / 6000f); _placeholder.SetData(samples, 0);
        }
        void Page(string screen, string title, Action back = null)
        {
            _screen = screen; _back = back; _playing = false; _waves.Clear(); _dialogue = null; _playText = null; _seek = null; _shownLine = -1; _endingCursor = null; _endingSummary = null;
            CloseModal(); if (_page) { _page.gameObject.SetActive(false); UnityEngine.Object.Destroy(_page.gameObject); }
            var prefab = Resources.Load<GameObject>("Castle/Prefabs/" + screen);
            _page = prefab ? UnityEngine.Object.Instantiate(prefab, _root).GetComponent<RectTransform>() : _ui.Rect(_root, screen, 0, 0, 1600, 900);
            _page.anchorMin = _page.anchorMax = _page.pivot = new Vector2(0, 1); _page.anchoredPosition = Vector2.zero; _page.sizeDelta = new Vector2(1600, 900);
            _ui.Box(_page, "Background", 0, 0, 1600, 900, UiFactory.Ink);
            _ui.Text(_page, "古堡调查 / " + title, 35, 25, 1000, 50, 30, UiFactory.Gold);
            if (back != null) _ui.Button(_page, "返回 · Esc", 1380, 20, 185, back);
            _status = _ui.Text(_page, "", 35, 840, 1530, 55, 18); UpdateStatus();
        }
        void UpdateStatus()
        {
            if (_status) _status.text = _session.State.WorldTime + "  ·  " + _session.Content.Require(_session.State.Room).Name + "    " + (_session.Notice ?? "示例案件 · 正式剧情待策划替换");
        }
        public void ShowTitle()
        {
            Page("Title", "声音留在墙壁之间"); _ui.Picture(_page, _session.Settings.TitleScene, 0, 95, 1600, 720);
            _ui.Box(_page, "Title Card", 120, 260, 690, 380, UiFactory.Panel);
            _ui.Text(_page, "时差档案", 165, 310, 610, 80, 55, UiFactory.Gold);
            _ui.Text(_page, "听见片段，校正时钟，重构古堡中的行程。\n第一版策划案 · 可替换示例内容", 165, 415, 605, 100, 25);
            _ui.Button(_page, _session.State.Started ? "继续调查" : "开始调查", 165, 550, 500, () => { _session.Start(); Resume(); });
            if (_session.Storage.LastError != null) _session.Notice = _session.Storage.LastError; UpdateStatus();
        }
        public void Resume() { _session.Events.Schedule(); if (_session.State.Active != null) ShowDialogue(); else ShowRoom(); }
        public void ShowRoom()
        {
            if (_session.State.Active != null) { ShowDialogue(); return; }
            var room = _session.Content.Require(_session.State.Room); Page("Room", room.Name);
            _ui.Picture(_page, _session.Settings.Map.Rooms.First(r => r.Room == room.Id).Background, 0, 100, 1600, 700);
            _ui.Box(_page, "Actions", 1090, 110, 480, 680, UiFactory.Panel);
            float y = 135;
            foreach (var hotspot in _session.Settings.Hotspots.Hotspots.Where(h => h.Room == room.Id))
            {
                var h = hotspot; _ui.Button(_page, h.Label, 1120, y, 420, () => Hotspot(h)); y += 65;
            }
            _ui.Button(_page, "前往其他房间", 1120, y, 420, ShowTravel); y += 65;
            if (_session.Has(_session.Settings.MapEntry)) { _ui.Button(_page, "地图 M", 1120, y, 420, () => ShowMap(false)); y += 65; }
            _ui.Button(_page, "调查册 J", 1120, y, 420, ShowJournal); y += 65;
            _ui.Button(_page, "分析与重构", 1120, y, 420, ShowPuzzles); y += 65;
            _ui.Button(_page, "对白回看 / 补领", 1120, y, 420, ShowHistory);
            _ui.Button(_page, "保存并返回标题", 35, 735, 270, () => { _session.Persist(); ShowTitle(); });
        }
        void Hotspot(HotspotDefinition hotspot)
        {
            if (hotspot.Mode == "event")
            {
                var choices = _session.Events.Choices(hotspot.Id);
                if (choices.Length == 0) { Notify("此处当前没有可用事件。请查看开放时段或前置线索。"); return; }
                if (choices.Length == 1) { _session.Events.Begin(choices[0].Id); ShowDialogue(); return; }
                var panel = Modal("选择要进行的交互"); float y = 250;
                foreach (var choice in choices) { var e = choice; _ui.Button(panel, e.Name, 450, y, 700, () => { _session.Events.Begin(e.Id); ShowDialogue(); }); y += 65; }
            }
            else if (hotspot.Mode == "control") ShowRecords();
            else if (hotspot.Mode == "tape_player") ShowTapes();
            else if (hotspot.Mode == "rest") Confirm("休息到次日 " + _session.Settings.WakeTime + "？", () => { _session.Rest(); Resume(); });
            else if (hotspot.Mode == "map") ShowMap(false);
            else if (hotspot.Mode == "puzzle") ShowPuzzle(hotspot.PuzzleId);
            else ShowJournal();
        }
        void ShowTravel()
        {
            Page("Map", "房间与开放时间", ShowRoom); float y = 145;
            foreach (var room in _session.Content.Table("Room"))
            {
                var id = room.Id; string status = _session.RoomStatus(id);
                _ui.Button(_page, room.Name + " · " + room.Get("start") + "—" + room.Get("end") + " · " + status, 160, y, 1280, () => { _session.Travel(id); Resume(); }, status == "可进入"); y += 95;
            }
            _ui.Text(_page, "房间移动不额外耗时。离开中控室统一结算 " + _session.Settings.ControlMinutes + " 分钟。", 160, 660, 1270, 80);
        }
        public void ShowDialogue()
        {
            var active = _session.State.Active; if (active == null) { ShowRoom(); return; }
            var row = _session.Content.Require(active.Id); Page("Dialogue", (active.Replay ? "回看 · " : "") + row.Name);
            _ui.Picture(_page, row.Id == _session.Settings.OpeningEvent ? _session.Settings.ExteriorScene : _session.Settings.Map.Rooms.First(r => r.Room == _session.State.Room).Background, 0, 100, 1600, 650);
            _ui.Box(_page, "Dialogue Paper", 120, 395, 1360, 345, UiFactory.Panel);
            _speaker = _ui.Text(_page, "", 160, 420, 1250, 45, 25, UiFactory.Gold);
            var scroll = _ui.Scroll(_page, 160, 480, 1240, 180, 220);
            _dialogue = _ui.Text(scroll, "", 0, 0, 1210, 210, 30); _dialogue.overflowMode = TextOverflowModes.Overflow; _dialogue.raycastTarget = true;
            _dialogue.gameObject.AddComponent<LinkHandler>().Click = id => { var lines = _session.Content.Plot(row.Get("plot")); if (lines.Length > 0) _session.Inventory.Claim(lines[active.Line].Id, id); UpdateStatus(); };
            _continue = _ui.Button(_page, "继续", 1180, 760, 300, () => { _session.Events.Continue(); Resume(); }, active.LastLineDone);
            if (row.Id == _session.Settings.TapeTutorialEvent) _ui.Button(_page, "打开房间放音机", 120, 760, 400, ShowTapes);
            if (row.Get("action") == "ending") { _ui.Button(_page, "跳过演出", 120, 760, 260, () => { _session.Events.SkipEnding(); Resume(); }); BuildWave(160, 230, 1270);
                _ui.Box(_page, "Connected waves", 160, 260, 1270, 2, UiFactory.Gold);
                _endingCursor = _ui.Box(_page, "Reconstruction cursor", 160, 205, 3, 110, UiFactory.Paper);
                var timeline = _session.Content.Table("Puzzle").FirstOrDefault(p => p.Get("type") == "timeline" && _session.Has(p.Id));
                if (timeline != null) _endingSummary = _ui.Text(_page, string.Join("  →  ", _session.Puzzles.Draft(timeline.Id).Values.Select(id => _session.Content.Require(id).Name)), 170, 130, 1250, 65, 28, UiFactory.Gold);
            }
            RenderDialogue();
        }
        void RenderDialogue()
        {
            var active = _session.State.Active; if (active == null || !_dialogue) return;
            var lines = _session.Content.Plot(_session.Content.Require(active.Id).Get("plot"));
            if (lines.Length == 0) { _dialogue.text = "验证结果已准备完成。点击继续结算。"; _session.Events.CompleteLastLine(); return; }
            var line = lines[active.Line]; _speaker.text = line.Get("speaker"); string text = line.Get("text");
            foreach (var id in line.List("link_entry")) { var name = _session.Content.Require(id).Name; text = text.Replace(name, "<link=\"" + id + "\"><color=#D8B87F><u>" + name + "</u></color></link>"); }
            _dialogue.text = text;
            float height = Mathf.Max(180, _dialogue.GetPreferredValues(text, 1210, 0).y + 35);
            _dialogue.rectTransform.sizeDelta = new Vector2(1210, height);
            ((RectTransform)_dialogue.transform.parent).sizeDelta = new Vector2(1228, height);
            _shownLine = active.Line; PlayCue(line.Id);
        }
        void ShowHistory()
        {
            Page("Journal", "对白回看 · 可补领遗漏链接", ShowRoom);
            var content = _ui.Scroll(_page, 100, 120, 1400, 670, _session.State.CompletedEvents.Count * 75);
            int i = 0; foreach (var id in _session.State.CompletedEvents) { var e = _session.Content.Require(id); _ui.Button(content, e.Name, 20, i++ * 75, 1320, () => { _session.Events.Begin(e.Id, true, true); ShowDialogue(); }); }
        }
        RectTransform Modal(string title)
        {
            CloseModal(); _modal = _ui.Box(_root, "Modal Input Shield", 0, 0, 1600, 900, new Color(0, 0, 0, .85f));
            _ui.Box(_modal, "Modal", 350, 150, 900, 620, UiFactory.Panel); _ui.Text(_modal, title, 400, 180, 800, 130, 28, UiFactory.Gold);
            _ui.Button(_modal, "关闭 / 取消", 850, 680, 350, CloseModal); return _modal;
        }
        void CloseModal() { if (_modal) { _modal.gameObject.SetActive(false); UnityEngine.Object.Destroy(_modal.gameObject); _modal = null; } }
        void Confirm(string text, Action accept)
        { var modal = Modal(text); _ui.Button(modal, "确认", 400, 680, 350, () => { CloseModal(); accept(); }); }
        void Notify(string text) { _session.Notice = text; UpdateStatus(); }
        void PlayCue(string lineId)
        {
            var line = _session.Content.Require(lineId); var clip = _session.Settings.Sounds.FirstOrDefault(s => s.LineId == lineId)?.Clip;
            if (clip || line.Get("anchor") == "true") _audio.PlayOneShot(clip ? clip : _placeholder);
        }
        void BuildWave(float x, float y, float width)
        { for (int i = 0; i < 90; i++) _waves.Add(_ui.Box(_page, "Wave", x + i * width / 90, y, 5, 30, UiFactory.Gold)); }
        public void Tick(float delta)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) { if (_modal) CloseModal(); else _back?.Invoke(); }
            if (_modal) return;
            if (_screen == "Room" && Input.GetKeyDown(KeyCode.J)) ShowJournal();
            if (_screen == "Room" && Input.GetKeyDown(KeyCode.M) && _session.Has(_session.Settings.MapEntry)) ShowMap(false);
            if (_screen == "Dialogue")
            {
                _session.Events.Tick(delta); var active = _session.State.Active;
                if (active != null) { if (active.Line != _shownLine) RenderDialogue(); if (_continue) _continue.interactable = active.LastLineDone; AnimateWave(active.Cursor);
                    if (_endingCursor)
                    {
                        var lines = _session.Content.Plot(_session.Content.Require(active.Id).Get("plot"));
                        float duration = lines.Last().Number("at") + _session.Settings.EndingHold;
                        _endingCursor.anchoredPosition = new Vector2(160 + 1270 * Mathf.Clamp01(active.Cursor / duration), -205);
                        if (_endingSummary) _endingSummary.alpha = Mathf.Clamp01(1 - active.Cursor / 3);
                    } }
            }
            if (_screen == "Playback" && _playing)
            {
                var record = _session.Content.Require(_record); var progress = _session.Recordings.Progress(_record);
                _session.Recordings.Seek(_record, progress.Cursor + delta, _tape); if (progress.Cursor >= record.Number("duration")) { _playing = false; _session.Persist(); }
                UpdatePlayback();
            }
            UpdateStatus();
        }
        void AnimateWave(float cursor) { for (int i = 0; i < _waves.Count; i++) _waves[i].sizeDelta = new Vector2(5, 15 + 65 * Mathf.Abs(Mathf.Sin(cursor * 2 + i * .6f))); }
    }
}

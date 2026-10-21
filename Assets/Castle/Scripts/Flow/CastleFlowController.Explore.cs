using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Castle.Flow
{
    public sealed partial class CastleFlowController
    {
        private int _floor = 1;
        private string _selectedRoom;
        private readonly Dictionary<int, TextMeshProUGUI> _dialogueTexts = new Dictionary<int, TextMeshProUGUI>();

        private void StartDialogue(string id, bool tape = false)
        {
            PausePlayback();
            Session.ActiveDialogue = id; Session.TapePlayback = tape;
            var dialogue = Content.Dialogue(id);
            foreach (var person in dialogue.Participants) Session.Collect(person);
            Session.SelectedNpc = dialogue.Participants.FirstOrDefault();
            var progress = Session.Progress(id);
            if (tape) { progress.Shown.Clear(); progress.Completed = false; progress.Scroll = 1; }
            if (progress.Shown.Count == 0) Session.Reveal(dialogue, 0);
            ShowPage("Explore", true, false);
        }

        private void RenderExplore()
        {
            var view = V("Explore");
            var room = Content.Room(Session.RoomId);
            Text(view, "RoomName", room.Name);
            bool active = !string.IsNullOrEmpty(Session.ActiveDialogue);
            Visible(view, "DialoguePanel", active);
            Visible(view, "FreePanel", !active);
            Text(view, "RoomHint", Session.RoomId == Content.ControlRoom ? "录音工作台已接通。从声音、时间和空间中寻找线索。" : "探索房间或与在场的人交谈。地图可以查看各区域的开放情况。");
            Button(view, "Workbench", () => ShowPage("Recordings"));
            Visible(view, "Workbench", Session.RoomId == Content.ControlRoom);
            Button(view, "ExploreRoom", () => { Session.Minutes += room.ExploreMinutes; Save(); RenderHud(); Toast("探索完成，游戏时间推进 " + room.ExploreMinutes + " 分钟。"); });
            Button(view, "Talk", () => StartDialogue(Content.OpeningDialogue));
            Button(view, "Map", () => OpenMap(), enabled: Session.Entries.Contains(Content.MapId));
            var people = view.Get<RectTransform>("People"); Clear(people);
            var dialogue = active ? Content.Dialogue(Session.ActiveDialogue) : null;
            var available = Content.Dialogues.Where(item => !item.Forced && item.Id != Content.Entry(Content.TapeId).UseDialogue).ToArray();
            var ids = active && dialogue.Forced ? dialogue.Participants : available.SelectMany(item => item.Participants).Distinct().ToArray();
            foreach (var personId in ids)
            {
                string capturedId = personId;
                var entry = Content.Entry(personId);
                var card = Card(people, entry.Name, personId == Session.SelectedNpc ? "当前交谈" : "点击交谈", () =>
                {
                    var target = available.FirstOrDefault(item => item.Participants.Contains(capturedId));
                    if (target != null) { StartDialogue(target.Id); Session.SelectedNpc = capturedId; RenderExplore(); Save(); }
                });
                card.Get<Button>("Select").interactable = !(active && dialogue.Forced);
                card.Get<Image>("Background").color = personId == Session.SelectedNpc ? new Color(.40f,.37f,.24f) : new Color(.17f,.23f,.20f);
            }
            Text(view, "Portrait", active ? string.Join("  ·  ", dialogue.Participants.Select(id => Content.Entry(id).Name)) : "拉帕莉亚");
            if (!active) return;
            var progress = Session.Progress(dialogue.Id);
            var scroll = view.Get<ScrollRect>("DialogueScroll");
            scroll.onValueChanged.RemoveAllListeners();
            Clear(scroll.content); _dialogueTexts.Clear();
            foreach (int index in progress.Shown)
            {
                int capturedIndex = index;
                var line = dialogue.Lines[index];
                var row = Instantiate(Ui.LinePrefab, scroll.content);
                row.gameObject.SetActive(true);
                Text(row, "Speaker", line.Speaker);
                var body = row.Get<TextMeshProUGUI>("Body");
                body.text = LinkedText(line); _dialogueTexts.Add(index, body);
                body.GetComponent<FlowTextLink>().Clicked = key => ClickKeyword(dialogue.Lines[capturedIndex].Keywords[int.Parse(key)]);
            }
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
            scroll.verticalNormalizedPosition = progress.Scroll;
            scroll.onValueChanged.AddListener(value => progress.Scroll = value.y);
            Button(view, "Continue", () => Advance(), progress.Completed ? "结束交谈" : "继续 →");
            Button(view, "Auto", () => { _auto = !_auto; _nextLineAt = Time.unscaledTime + Session.AutoSeconds; Text(view, "AutoLabel", _auto ? "自动：开启" : "自动：关闭"); });
            Text(view, "AutoLabel", _auto ? "自动：开启" : "自动：关闭");
            Visible(view, "ExitTape", Session.TapePlayback);
            Button(view, "ExitTape", ExitTape);
            var choices = view.Get<RectTransform>("Choices"); Clear(choices);
            if (!progress.Completed)
            {
                var current = dialogue.Lines[progress.Shown.Last()];
                foreach (var choice in current.Choices)
                {
                    var option = choice;
                    Card(choices, option.Label, "", () => RevealNext(dialogue, option.NextLine));
                }
                view.Get<Button>("Continue").interactable = current.Choices.Length == 0;
            }
        }

        private string LinkedText(FlowLine line)
        {
            // 使用明确配置的关键词，按最长词优先匹配；不从文案猜测奖励。
            var ordered = line.Keywords.Select((keyword, index) => new { Keyword = keyword, Index = index }).OrderByDescending(pair => pair.Keyword.Word.Length).ToArray();
            var text = new System.Text.StringBuilder();
            for (int offset = 0; offset < line.Text.Length;)
            {
                var match = ordered.FirstOrDefault(pair => offset + pair.Keyword.Word.Length <= line.Text.Length && string.CompareOrdinal(line.Text, offset, pair.Keyword.Word, 0, pair.Keyword.Word.Length) == 0);
                if (match == null) { text.Append(line.Text[offset++]); continue; }
                bool clickable = match.Keyword.Kind != KeywordKind.Person || Session.Entries.Contains(match.Keyword.Target);
                if (clickable) text.Append("<link=\"").Append(match.Index).Append("\"><color=#D8B878><u>");
                text.Append(match.Keyword.Word);
                if (clickable) text.Append("</u></color></link>");
                offset += match.Keyword.Word.Length;
            }
            return text.ToString();
        }

        private void RefreshVisibleDialogueLinks()
        {
            if (string.IsNullOrEmpty(Session.ActiveDialogue)) return;
            var dialogue = Content.Dialogue(Session.ActiveDialogue);
            foreach (var pair in _dialogueTexts) if (pair.Value) pair.Value.text = LinkedText(dialogue.Lines[pair.Key]);
        }

        private void ClickKeyword(FlowKeyword keyword)
        {
            switch (keyword.Kind)
            {
                case KeywordKind.Location: OpenMap(keyword.Target); break;
                case KeywordKind.Person:
                    if (Session.Entries.Contains(keyword.Target)) OpenJournal(keyword.Target);
                    break;
                case KeywordKind.Claim:
                    if (!Session.Entries.Contains(keyword.Target)) Acquire(keyword.Target);
                    else OpenJournal(keyword.Target);
                    break;
                case KeywordKind.Item:
                    if (Session.Entries.Contains(keyword.Target)) OpenJournal(keyword.Target);
                    else Toast("这件道具尚未收录。");
                    break;
            }
        }

        private void Advance(bool automatic = false)
        {
            if (_modals.Count > 0 || string.IsNullOrEmpty(Session.ActiveDialogue)) return;
            var dialogue = Content.Dialogue(Session.ActiveDialogue); var progress = Session.Progress(dialogue.Id);
            int currentIndex = progress.Shown.Last();
            if (dialogue.Lines[currentIndex].Choices.Length > 0 && !progress.Completed) { _auto = false; Text(V("Explore"), "AutoLabel", "自动：关闭"); return; }
            if (progress.Completed || currentIndex == dialogue.Lines.Length - 1)
            {
                _auto = false; Text(V("Explore"), "AutoLabel", "自动：关闭");
                if (automatic) { Text(V("Explore"), "ContinueLabel", "继续并结束交谈"); return; }
                progress.Completed = true; Save(); CompleteDialogue(); return;
            }
            RevealNext(dialogue, currentIndex + 1);
        }

        private void RevealNext(FlowDialogue dialogue, int index)
        {
            Session.Reveal(dialogue, index); Session.Progress(dialogue.Id).Scroll = 0;
            _nextLineAt = Time.unscaledTime + Session.AutoSeconds;
            Save(); RenderExplore();
        }

        private void CompleteDialogue()
        {
            var dialogue = Content.Dialogue(Session.ActiveDialogue);
            var claims = Session.Progress(dialogue.Id).Shown.SelectMany(index => dialogue.Lines[index].Keywords)
                .Where(keyword => keyword.Kind == KeywordKind.Claim).Select(keyword => keyword.Target).Distinct().ToArray();
            string pending = claims.FirstOrDefault(id => !Session.Entries.Contains(id));
            if (pending != null) { Acquire(pending, CompleteDialogue); return; }
            // 切换自由交谈对象时，不提前补发另一段尚未结束的对白奖励。
            Session.PendingClaims.RemoveAll(claims.Contains);
            if (Session.TapePlayback) { ExitTape(); return; }
            Session.ActiveDialogue = null; ShowPage("Explore");
        }

        private void ExitTape()
        {
            PausePlayback();
            Session.TapePlayback = false; Session.ActiveDialogue = Session.TapeReturnDialogue; Session.TapeReturnDialogue = null;
            ShowPage("Explore", true, false); OpenJournal(Content.TapeId);
        }

        private void OpenMap(string target = null)
        {
            if (!Session.Entries.Contains(Content.MapId)) { Toast("尚未获得地图；地点解锁已记录。"); return; }
            _selectedRoom = target;
            _floor = Content.Room(target ?? Session.RoomId)?.Floor ?? 1;
            PushModal("Map"); RenderMap();
        }

        private void RenderMap()
        {
            var view = V("Map");
            Button(view, "Close", PopModal);
            Button(view, "Floor1", () => { _floor = 1; RenderMap(); });
            Button(view, "Floor2", () => { _floor = 2; RenderMap(); });
            Text(view, "Heading", "古堡地图  /  " + _floor + "F");
            BuildMap(view, id => { _selectedRoom = id; RenderMap(); }, _selectedRoom);
            Text(view, "Details", RoomDetails(_selectedRoom));
            Text(view, "Hint", string.IsNullOrEmpty(Session.ActiveDialogue) ? "选择地点，再点击进入。◆ 标识当前位置。" : "对白期间仅可查看地图，关闭后返回原句。");
            Button(view, "Enter", () =>
            {
                if (!Session.CanEnter(Content, _selectedRoom)) return;
                Session.RoomId = _selectedRoom; Session.SelectedNpc = null; ShowPage("Explore");
            }, enabled: Session.CanEnter(Content, _selectedRoom));
        }

        private string RoomDetails(string id)
        {
            var room = Content.Room(id);
            if (room == null) return "将鼠标悬停在地点上查看详情。";
            if (!Session.Unlocked.Contains(id)) return "未解锁区域";
            bool open = Session.Minutes >= room.Opens && Session.Minutes < room.Closes;
            string device = Session.Devices.Any(item => item.Id == room.DeviceId && item.Verified) ? Content.Recordings.First(item => item.DeviceId == room.DeviceId).DeviceName : "未知设备";
            return room.Name + "\n开放时间：" + CastleRules.Clock(room.Opens) + "—" + CastleRules.Clock(room.Closes) + "\n" + (open ? "开放中" : "未达开放时间或已关闭") + "\n录音设备：" + device + (room.Navigable ? "" : "\n此地点暂不可进入");
        }

        private void BuildMap(CastleUiView view, Action<string> click, string selected, bool deduction = false)
        {
            var area = view.Get<RectTransform>("Rooms"); Clear(area);
            view.Get<RawImage>("FloorImage").texture = Resources.Load<Texture2D>("Castle/Floor" + _floor);
            // 原型底图含旧导航。使用 UV 裁切只显示地图区域，地点归一化坐标同步变换。
            const float cropX = 210f / 1672, cropY = 160f / 941, cropWidth = 1280f / 1672, cropHeight = 600f / 941;
            view.Get<RawImage>("FloorImage").uvRect = new Rect(cropX, 1 - cropY - cropHeight, cropWidth, cropHeight);
            float width = area.rect.width, height = area.rect.height;
            foreach (var room in Content.Rooms.Where(item => item.Floor == _floor))
            {
                var captured = room;
                bool unlocked = deduction || Session.Unlocked.Contains(room.Id);
                bool open = deduction || Session.Minutes >= room.Opens && Session.Minutes < room.Closes;
                var card = Card(area, !unlocked ? "未解锁" : (Session.RoomId == room.Id ? "◆ " : "") + room.Name, "", () => { if (unlocked) click(captured.Id); });
                Visible(card, "Body", false);
                var rect = (RectTransform)card.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
                rect.anchoredPosition = new Vector2((room.Bounds.x - cropX) / cropWidth * width, -(room.Bounds.y - cropY) / cropHeight * height);
                rect.sizeDelta = new Vector2(room.Bounds.z / cropWidth * width, room.Bounds.w / cropHeight * height);
                var title = card.Get<Text>("Title"); title.fontSize = 18;
                title.rectTransform.anchorMin = Vector2.zero; title.rectTransform.anchorMax = Vector2.one;
                title.rectTransform.offsetMin = new Vector2(2,2); title.rectTransform.offsetMax = new Vector2(-2,-2);
                title.alignment = TextAnchor.MiddleCenter;
                card.Get<Image>("Background").color = !unlocked ? new Color(.12f,.13f,.12f,1) : room.Id == selected ? new Color(.57f,.40f,.17f,.97f) : open ? new Color(.19f,.30f,.22f,.94f) : new Color(.18f,.19f,.18f,.98f);
                var pointer = card.Get<FlowPointer>("Pointer");
                pointer.Enter = () => { Text(view, "Tooltip", deduction ? captured.Name : RoomDetails(captured.Id)); Visible(view, "TooltipPanel", true); };
                pointer.Exit = () => Visible(view, "TooltipPanel", false);
            }
            Visible(view, "TooltipPanel", false);
        }
    }
}

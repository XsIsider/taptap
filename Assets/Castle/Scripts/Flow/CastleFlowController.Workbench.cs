using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Castle.Flow
{
    public sealed partial class CastleFlowController
    {
        private int _recordingIndex;
        private string _selectedAnchor, _selectedDevice, _selectedClue;
        private FlowPuzzle Puzzle => Content.Puzzles[Mathf.Clamp(Session.PuzzleIndex, 0, Content.Puzzles.Length - 1)];
        private FlowRecording Recording => Content.Recordings[_recordingIndex];

        private void WorkbenchTabs(CastleUiView view)
        {
            Button(view, "Recordings", () => ShowPage("Recordings"));
            Button(view, "Anchors", () => ShowPage("Anchors"));
            Button(view, "Space", () => ShowPage("Space"));
            Button(view, "PreviousPuzzle", () => SelectPuzzle(-1), enabled: Session.PuzzleIndex > 0);
            Button(view, "NextPuzzle", () => SelectPuzzle(1), enabled: Session.PuzzleIndex + 1 < Content.Puzzles.Length);
            Text(view, "PuzzleName", "事件 " + (Session.PuzzleIndex + 1) + " / " + Content.Puzzles.Length + "  ·  " + Puzzle.Name);
        }

        private void SelectPuzzle(int direction)
        {
            Session.PuzzleIndex = Mathf.Clamp(Session.PuzzleIndex + direction, 0, Content.Puzzles.Length - 1);
            _selectedAnchor = null; _position = 0;
            ShowPage(_page);
        }

        private void RenderRecordings()
        {
            var view = V("Recordings"); WorkbenchTabs(view);
            var list = view.Get<RectTransform>("List"); Clear(list);
            for (int i = 0; i < Content.Recordings.Length; i++)
            {
                int index = i; var record = Content.Recordings[i];
                Card(list, record.Name, Session.Devices.Any(item => item.Id == record.DeviceId && item.Verified) ? record.DeviceName : "未知设备", () => { PausePlayback(); _recordingIndex = index; _position = 0; RenderRecordings(); });
            }
            Text(view, "FileInfo", Recording.Name + "  /  原始时间 " + TimeText(Recording.StartSeconds) + "  /  " + (Session.Devices.Any(item => item.Id == Recording.DeviceId && item.Verified) ? Content.Room(Recording.RoomId).Name : "地点待匹配"));
            var content = view.Get<RectTransform>("Lines"); Clear(content);
            for (int i = 0; i < Recording.Texts.Length; i++)
            {
                int line = i;
                var puzzle = Content.Puzzles.FirstOrDefault(item => item.LineIndex == line && item.RecordingIds.Contains(Recording.Id));
                var draft = puzzle == null ? null : Session.Draft(puzzle.Id);
                bool marked = draft != null && draft.Marked.Contains(Recording.Id);
                var card = Card(content, TimeText(Recording.StartSeconds + Recording.AtSeconds[i]) + (marked ? "   ◆ 已标记" : "   ◇ 点击标记"), Recording.Texts[i], () =>
                {
                    _position = Recording.AtSeconds[line];
                    if (draft == null) return;
                    if (!draft.Marked.Remove(Recording.Id)) draft.Marked.Add(Recording.Id);
                    else
                    {
                        draft.Tracks.RemoveAll(item => item.RecordingId == Recording.Id);
                        if (draft.ReferenceId == Recording.Id) draft.ReferenceId = null;
                        draft.Matched = draft.Aligned = false;
                    }
                    Save(); RenderRecordings();
                });
                card.Get<Image>("Background").color = i == Puzzle.LineIndex ? new Color(.31f,.39f,.28f) : new Color(.15f,.24f,.19f);
            }
            Button(view, "Play", ToggleRecording, _playing ? "暂停" : "播放");
            Slider(view, "Progress", 0, Recording.DurationSeconds, _position, value => { _position = value; if (Audio.clip) Audio.time = Mathf.Min(value, Audio.clip.length - .01f); UpdateRecordingPlayback(); });
            Button(view, "Done", () => ShowPage("Anchors"));
            UpdateRecordingPlayback();
        }

        private void ToggleRecording()
        {
            _playing = !_playing;
            if (_position >= Recording.DurationSeconds) _position = 0;
            if (_playing && Recording.Audio) PlayAudio(Recording.Audio); else Audio.Pause();
            Text(V("Recordings"), "PlayLabel", _playing ? "暂停" : "播放");
        }

        private void PlayAudio(AudioClip clip)
        {
            Audio.clip = clip; Audio.volume = Session.Volume;
            Audio.time = Mathf.Clamp(_position, 0, Math.Max(0, clip.length - .01f)); Audio.Play();
        }

        private void UpdateRecordingPlayback()
        {
            if (_position >= Recording.DurationSeconds) { _position = Recording.DurationSeconds; _playing = false; Audio.Pause(); }
            var view = V("Recordings");
            view.Get<Slider>("Progress").SetValueWithoutNotify(_position);
            int line = -1;
            for (int i = 0; i < Recording.AtSeconds.Length; i++) if (_position >= Recording.AtSeconds[i]) line = i;
            Text(view, "Playback", TimeText((int)_position) + " / " + TimeText(Recording.DurationSeconds) + (Recording.Audio ? "" : "  ·  文字回放") + "\n" + (line < 0 ? "等待录音片段" : Recording.Texts[line]));
            Text(view, "PlayLabel", _playing ? "暂停" : "播放");
        }

        private void RenderAnchors()
        {
            var view = V("Anchors"); WorkbenchTabs(view);
            var draft = Session.Draft(Puzzle.Id);
            var candidates = view.Get<RectTransform>("List"); Clear(candidates);
            foreach (string recordingId in draft.Marked)
            {
                string id = recordingId; var recording = Content.Recording(id);
                var card = Card(candidates, recording.Name, "点击查看 / 拖入轨道", () => { _selectedAnchor = id; RenderAnchorDetails(); });
                var drag = card.Get<FlowDrag>("Drag"); drag.Kind = "Anchor"; drag.Id = id; drag.CanDrag = true;
            }
            view.Get<FlowDrag>("DropZone").Dropped = source => { if (source.Kind == "Anchor") AddTrack(source.Id); };
            Button(view, "Add", () => AddTrack(_selectedAnchor), enabled: !string.IsNullOrEmpty(_selectedAnchor));
            var tracks = view.Get<RectTransform>("Tracks"); Clear(tracks);
            foreach (var track in draft.Tracks)
            {
                var captured = track;
                var row = Instantiate(Ui.TrackPrefab, tracks); row.gameObject.SetActive(true);
                bool reference = draft.ReferenceId == track.RecordingId;
                Text(row, "Name", Content.Recording(track.RecordingId).Name);
                Text(row, "Offset", ShiftText(track.ShiftSeconds));
                Button(row, "Reference", () => { Session.ChooseReference(draft, captured.RecordingId); Save(); RenderAnchors(); }, reference ? "参考设备 ✓" : "设为参考");
                Button(row, "Remove", () =>
                {
                    draft.Tracks.Remove(captured);
                    if (draft.ReferenceId == captured.RecordingId) { draft.ReferenceId = null; Toast("参考轨道已移除，请重新选择参考设备。"); }
                    draft.Matched = draft.Aligned = false; Save(); RenderAnchors();
                });
                Slider(row, "OffsetSlider", -600, 600, track.ShiftSeconds, value =>
                {
                    captured.ShiftSeconds = value; draft.Aligned = false;
                    Text(row, "Offset", ShiftText(value)); Text(view, "Status", "校准草稿已改变，请重新检查。"); Save();
                });
                row.Get<Slider>("OffsetSlider").interactable = !reference && !string.IsNullOrEmpty(draft.ReferenceId);
            }
            Button(view, "Check", () =>
            {
                if (!draft.Matched) Toast(Session.Match(Puzzle) ? "匹配通过，请选择参考设备并调整各轨道时间。" : "候选组不完整，请加入这个事件的全部录音锚点。");
                else Toast(Session.Align(Content, Puzzle) ? "时间对齐通过。继续匹配设备所在空间。" : "时间尚未对齐，或尚未选择参考设备。草稿已保留。");
                Save(); RenderAnchors();
            }, draft.Matched ? "检查校准" : "检查匹配");
            Text(view, "Status", draft.Aligned ? "对齐已通过" : draft.Matched ? "匹配通过 · 调整相对时间" : "先将左侧锚点拖入右侧轨道");
            Button(view, "Next", () => { _floor = 1; ShowPage("Space"); });
            RenderAnchorDetails();
        }

        private void RenderAnchorDetails()
        {
            var record = Content.Recording(_selectedAnchor);
            Text(V("Anchors"), "Details", record == null ? "选择锚点查看原文，再拖入右侧。参考设备由你指定。" : record.Name + "  ·  " + TimeText(record.StartSeconds + record.AtSeconds[Puzzle.LineIndex]) + "\n" + record.Texts[Puzzle.LineIndex]);
            V("Anchors").Get<Button>("Add").interactable = record != null;
        }

        private void AddTrack(string id)
        {
            var draft = Session.Draft(Puzzle.Id);
            if (string.IsNullOrEmpty(id) || !draft.Marked.Contains(id) || draft.Tracks.Any(track => track.RecordingId == id)) return;
            draft.Tracks.Add(new TrackDraft { RecordingId = id }); draft.Matched = draft.Aligned = false;
            Save(); RenderAnchors();
        }

        private void RenderSpace()
        {
            var view = V("Space"); WorkbenchTabs(view);
            var records = Puzzle.RecordingIds.Select(Content.Recording).ToArray();
            if (!records.Any(record => record.DeviceId == _selectedDevice)) _selectedDevice = records[0].DeviceId;
            var list = view.Get<RectTransform>("List"); Clear(list);
            foreach (var record in records)
            {
                string device = record.DeviceId;
                var location = Session.Devices.FirstOrDefault(item => item.Id == device);
                Card(list, device == _selectedDevice ? "◆ " + record.Name : record.Name, location == null ? "尚未放置" : Content.Room(location.RoomId).Name, () => { _selectedDevice = device; RenderSpace(); });
            }
            var selected = Session.Devices.FirstOrDefault(item => item.Id == _selectedDevice);
            BuildMap(view, roomId =>
            {
                var location = Session.Devices.FirstOrDefault(item => item.Id == _selectedDevice);
                if (location == null) { location = new DeviceLocation { Id = _selectedDevice }; Session.Devices.Add(location); }
                location.RoomId = roomId; Save(); RenderSpace();
            }, selected?.RoomId, true);
            var selectedRecord = records.First(record => record.DeviceId == _selectedDevice);
            string deviceName = selected != null && selected.Verified ? selectedRecord.DeviceName : selectedRecord.Name + "（未知设备）";
            Text(view, "Details", "当前设备：" + deviceName + "\n空间草稿：" + (Content.Room(selected?.RoomId)?.Name ?? "尚未选择") + "\n\n选择设备后，点击地图地点放置。可以对照录音判断设备的位置。验证正确后将收入线索。");
            Button(view, "Floor1", () => { _floor = 1; RenderSpace(); });
            Button(view, "Floor2", () => { _floor = 2; RenderSpace(); });
            Button(view, "Verify", () =>
            {
                if (!Session.VerifySpace(Content, Puzzle)) { Toast("锚点校准或空间匹配不正确，请检查草稿。"); return; }
                Save(); Acquire(Puzzle.ClueId, () => OpenJournal(Puzzle.ClueId), true);
            });
        }

        private void RenderTimeline()
        {
            var view = V("Timeline");
            Visible(view, "SortRoot", !Session.EndingSubmitted);
            Visible(view, "RevealRoot", Session.EndingSubmitted);
            if (Session.EndingSubmitted)
            {
                _position = 0; _playing = true;
                if (Content.EndingAudio) PlayAudio(Content.EndingAudio);
                Button(view, "Play", () => { _playing = !_playing; if (_position >= EndingDuration) _position = 0; if (_playing && Content.EndingAudio) PlayAudio(Content.EndingAudio); else Audio.Pause(); UpdateEndingPlayback(); });
                Button(view, "Replay", () => { _position = 0; _playing = true; if (Content.EndingAudio) PlayAudio(Content.EndingAudio); });
                Slider(view, "Progress", 0, EndingDuration, _position, value => { _position = value; if (Audio.clip) Audio.time = Mathf.Clamp(value,0, Audio.clip.length - .01f); UpdateEndingPlayback(); });
                UpdateEndingPlayback(); return;
            }
            var list = view.Get<RectTransform>("Clues"); Clear(list);
            foreach (var entry in Content.Entries.Where(item => item.Kind == EntryKind.Clue && Session.Entries.Contains(item.Id)))
            {
                string id = entry.Id;
                var card = Card(list, entry.Name, entry.Body, () => { _selectedClue = id; Text(view, "Hint", "已选择「" + entry.Name + "」，点击槽位放置，或直接拖放。"); });
                var drag = card.Get<FlowDrag>("Drag"); drag.Kind = "Clue"; drag.Id = id; drag.CanDrag = true;
            }
            var slots = view.Get<RectTransform>("Slots"); Clear(slots);
            for (int i = 0; i < Content.EndingOrder.Length; i++)
            {
                int index = i; string id = Session.Timeline[i];
                var card = Card(slots, "时间节点 " + (i + 1), Content.Entry(id)?.Name ?? "拖入线索", () => PlaceClue(_selectedClue, index));
                var drag = card.Get<FlowDrag>("Drag"); drag.Kind = "Clue"; drag.Id = id; drag.CanDrag = !string.IsNullOrEmpty(id);
                drag.Dropped = source => { if (source.Kind == "Clue") PlaceClue(source.Id, index); };
            }
            Text(view, "Hint", "将已获得的线索按时间先后排列；拖到已有线索的槽位可以交换。");
            Button(view, "Submit", () =>
            {
                if (!Session.SubmitTimeline(Content)) { Toast("线索尚未齐全，或顺序不正确。当前排列已保留。"); return; }
                Save(); RenderTimeline();
            });
        }

        private void PlaceClue(string id, int slot)
        {
            if (string.IsNullOrEmpty(id) || !Session.Entries.Contains(id) || Content.Entry(id)?.Kind != EntryKind.Clue) return;
            int source = Session.Timeline.IndexOf(id);
            if (source >= 0) Session.Timeline[source] = Session.Timeline[slot];
            Session.Timeline[slot] = id; Save(); RenderTimeline();
        }

        private float EndingDuration => Content.EndingLines.Length * Content.EndingLineSeconds;
        private void UpdateEndingPlayback()
        {
            var view = V("Timeline");
            if (_position >= EndingDuration) { _position = EndingDuration; _playing = false; Audio.Pause(); }
            int line = Mathf.Min(Content.EndingLines.Length - 1, (int)(_position / Content.EndingLineSeconds));
            Text(view, "EndingText", Content.EndingLines[line]);
            Text(view, "EndingStatus", _position >= EndingDuration ? "结局已揭露 · 可重播" : "事件还原  " + (line + 1) + " / " + Content.EndingLines.Length);
            Text(view, "PlayLabel", _playing ? "暂停" : "播放");
            view.Get<Slider>("Progress").SetValueWithoutNotify(_position);
            int index = 0;
            foreach (RectTransform bar in view.Get<RectTransform>("Wave"))
            {
                bar.localScale = new Vector3(1, Session.ReduceMotion ? 1 : .45f + Mathf.Abs(Mathf.Sin(_position * 3 + index * .4f)), 1); index++;
            }
        }

        private static string TimeText(int seconds) => (seconds / 3600).ToString("00") + ":" + (seconds / 60 % 60).ToString("00") + ":" + (seconds % 60).ToString("00");
        private static string ShiftText(float seconds) => (seconds < 0 ? "−" : "+") + Mathf.Abs(seconds).ToString("0") + " 秒";
    }
}

using System;
using System.Linq;
using UnityEngine;
namespace Castle.V2
{
    public sealed partial class PrototypeUi
    {
        void ShowRecords()
        {
            _session.Recordings.Import(); Page("Records", "中控室 · 录音工作台", () => Resume());
            _ui.TabBar(_page, new[] { "录音档案", "锚点与校时", "空间与事件推理", "调查册" }, 0, index =>
            {
                if (index == 0) ShowRecords(); else if (index == 3) ShowJournal(); else ShowAnalysis(index == 1);
            });
            _ui.Box(_page, "Device panel", 80, 180, 430, 545, UiFactory.Panel);
            _ui.Text(_page, "设备状态", 110, 200, 370, 45, 27, UiFactory.Gold);
            float y = 260;
            foreach (var device in _session.Settings.Devices.Devices)
            {
                var knowledge = _session.Knowledge(device.Id);
                _ui.Text(_page, device.Label + "\n" + (!string.IsNullOrEmpty(knowledge.Room) ? "位置已验证" : "位置待调查") + " · " + (knowledge.Calibrated ? "时钟已校正" : "时钟待校正"), 110, y, 365, 100, 22);
                y += 130;
            }
            var rows = _session.Content.Table("Record").Where(row => _session.Recordings.CanAccess(row.Id)).ToArray();
            _ui.Text(_page, "可访问录音  /  " + rows.Length + " 份", 560, 190, 920, 50, 27, UiFactory.Gold);
            var content = _ui.Scroll(_page, 550, 255, 970, 460, rows.Length * 95);
            y = 0;
            foreach (var row in rows)
            {
                var id = row.Id;
                _ui.Button(content, _session.Recordings.Label(id) + " · 打开录音", 10, y, 920, () => ShowPlayback(id), true, 78); y += 95;
            }
            if (rows.Length == 0) _ui.Text(content, "当前没有可访问录音。探索后再来导入。", 20, 20, 900, 100);
            _ui.Text(_page, "个人录音进入时自动导入；关闭工作台仍留在中控室。", 90, 755, 930, 70, 21);
            _ui.Button(_page, "离开中控室（" + _session.Settings.ControlMinutes + " 分钟）", 1080, 760, 430, () => { _session.Travel(_session.Settings.HomeRoom); Resume(); });
        }
        void ShowAnalysis(bool audio)
        {
            Page("Puzzles", audio ? "声响分析" : "空间与事件推理", ShowRecords);
            var rows = _session.Content.Table("Puzzle").Where(p => audio == (p.Get("type") == "anchor" || p.Get("type") == "calibrate")).ToArray();
            var content = _ui.Scroll(_page, 90, 160, 1420, 610, rows.Length * 100);
            for (int i = 0; i < rows.Length; i++)
            {
                var puzzle = rows[i]; bool solved = _session.State.Solved.Contains(puzzle.Id);
                _ui.Button(content, puzzle.Name + (solved ? " · 已验证" : _session.Puzzles.Available(puzzle) ? " · 待分析" : " · 等待线索"), 20, i * 100, 1360, () => ShowPuzzle(puzzle.Id), solved || _session.Puzzles.Available(puzzle), 80);
            }
        }
        void ShowTapes()
        {
            Page("Tapes", "房间放音机 · 磁带实例", () => Resume()); float y = 145;
            foreach (var tape in _session.State.Tapes) { var t = tape; string label = _session.Content.Require(t.EntryId).Name + (string.IsNullOrEmpty(t.RecordId) ? " · 空白" : " · " + _session.Content.Require(t.RecordId).Name); _ui.Button(_page, label, 120, y, 1250, () => { if (string.IsNullOrEmpty(t.RecordId)) Notify("这盘磁带为空，可在中控室刻录。"); else ShowPlayback(t.RecordId, t.Id); }); y += 70; }
            if (_session.State.Tapes.Count == 0) _ui.Text(_page, "尚未获得磁带。", 120, 150, 1200, 80, 24);
        }
        public void ShowPlayback(string recordId, string tapeId = null)
        {
            if (!_session.Recordings.CanAccess(recordId, tapeId)) { Notify("请在中控室访问已开放文件，或回房间使用持有的磁带。"); return; }
            _record = recordId; _tape = tapeId; _lastSound = null;
            var record = _session.Content.Require(recordId);
            Page("Playback", "录音回放 · " + record.Name, tapeId == null ? (Action)ShowRecords : ShowTapes);
            _ui.Text(_page, _session.Recordings.Label(recordId), 90, 110, 1400, 45, 25, UiFactory.Gold);
            _ui.Text(_page, "原始 " + _session.Recordings.Started(recordId).ToString("yyyy-MM-dd HH:mm:ss") + "  /  校正 " + _session.Recordings.TimeAt(recordId, 0, true).ToString("HH:mm:ss"), 90, 160, 1400, 45, 20);
            var transcript = _ui.Scroll(_page, 90, 230, 850, 330, 500);
            _playText = _ui.Text(transcript, "", 15, 10, 810, 480, 25); _playText.overflowMode = TMPro.TextOverflowModes.Overflow;
            _playClock = _ui.Text(_page, "", 90, 590, 850, 45, 20, UiFactory.Gold);
            BuildWave(95, 650, 840);
            _seek = _ui.Slider(_page, 95, 725, 840, 0, record.Number("duration"), _session.Recordings.Progress(recordId).Cursor, value => { _session.Recordings.Seek(recordId, value, tapeId); UpdatePlayback(); _session.Persist(); });
            _ui.Button(_page, "播放 / 暂停", 1000, 215, 450, () => { if (_session.Recordings.Progress(recordId).Cursor >= record.Number("duration")) _session.Recordings.Seek(recordId, 0, tapeId); _playing = !_playing; });
            _ui.Button(_page, "标记当前声响", 1000, 285, 450, () =>
            {
                var line = _session.Content.Plot(record.Get("plot")).LastOrDefault(l => l.Number("at") <= _session.Recordings.Progress(recordId).Cursor);
                Notify(line != null && _session.Recordings.Mark(line.Id) ? "声响已保存，可用于分析。" : "当前句不是可标记声响；可拖动游标定位。");
            });
            _ui.Button(_page, "提取当前句线索", 1000, 355, 450, () =>
            {
                var line = _session.Content.Plot(record.Get("plot")).LastOrDefault(l => l.Number("at") <= _session.Recordings.Progress(recordId).Cursor);
                int count = 0;
                if (line != null) foreach (var entry in _session.Content.Table("Entry").Where(e => e.List("source").Contains(line.Id))) if (_session.Inventory.Extract(entry.Id, line.Id)) count++;
                Notify(count == 0 ? "当前句未配置可提取线索。" : "已提取 " + count + " 项线索/人物信息。");
            });
            if (tapeId == null) _ui.Button(_page, "刻录完整文件", 1000, 435, 450, () => ShowBurn(recordId));
            _ui.Button(_page, "分析已标记声响", 1000, 515, 450, ShowPuzzles);
            _ui.Text(_page, "文字、波形和提示音使用同一游标。\n可暂停并拖动到某句；声响逐次标记。", 1000, 620, 450, 110, 21);
            _session.Recordings.Seek(recordId, _session.Recordings.Progress(recordId).Cursor, tapeId); UpdatePlayback();
        }
        void UpdatePlayback()
        {
            if (!_playText) return;
            var record = _session.Content.Require(_record); float cursor = _session.Recordings.Progress(_record).Cursor;
            var lines = _session.Content.Plot(record.Get("plot")).Where(l => l.Number("at") <= cursor).ToArray();
            _playText.text = string.Join("\n\n", lines.Select(l => (l.Get("anchor") == "true" ? "◇ " : "") + l.Get("speaker") + "：" + l.Get("text")));
            _playClock.text = cursor.ToString("0.0") + " / " + record.Number("duration").ToString("0.0") + " 秒  ·  " + _session.Recordings.TimeAt(_record, cursor, true).ToString("HH:mm:ss");
            if (_seek) _seek.SetValueWithoutNotify(cursor);
            if (lines.Length > 0 && _lastSound != lines.Last().Id) { _lastSound = lines.Last().Id; PlayCue(_lastSound); }
            AnimateWave(cursor);
        }
        void ShowBurn(string recordId)
        {
            var modal = Modal("选择要刻录的磁带"); float y = 280; foreach (var tape in _session.State.Tapes) { var t = tape; _ui.Button(modal, string.IsNullOrEmpty(t.RecordId) ? "空白磁带" : "覆盖：" + _session.Content.Require(t.RecordId).Name, 450, y, 700, () => { if (!string.IsNullOrEmpty(t.RecordId)) Confirm("将把「" + _session.Content.Require(t.RecordId).Name + "」覆盖为「" + _session.Content.Require(recordId).Name + "」。磁带数量不变。", () => { _session.Recordings.Burn(t.Id, recordId, true); CloseModal(); }); else { _session.Recordings.Burn(t.Id, recordId, true); CloseModal(); } }); y += 65; }
        }
        void ShowJournal()
        {
            if (_screen != "Journal") _journalBack = CurrentReturn();
            Page("Journal", "调查册 · 已收录信息", _journalBack);
            var labels = new[] { "道具", "线索", "人物", "已验证事件", "对话记录" };
            var types = new[] { "item", "clue", "info", "event" };
            _ui.TabBar(_page, labels, _journalTab, index => { _journalTab = index; ShowJournal(); });
            _ui.Box(_page, "Journal paper", 80, 175, 1440, 625, UiFactory.Paper);
            var owned = _session.Content.Table("Entry").Where(e => _session.Has(e.Id)).ToArray();
            var content = _ui.Scroll(_page, 100, 195, 1400, 580, 580);
            float y = 10;
            if (_journalTab == 4)
            {
                foreach (var id in _session.State.CompletedEvents)
                {
                    var entry = _session.Content.Require(id);
                    _ui.Button(content, entry.Name + " · 回看 / 补领", 20, y, 1330, () => { _session.Events.Begin(entry.Id, true, true); ShowDialogue(); }, _session.State.Active == null); y += 75;
                }
            }
            else if (_journalTab == 2)
            {
                foreach (var character in _session.Settings.Characters.Characters.Where(c => owned.Any(e => e.Get("type") == "info" && e.List("owner").Contains(c.Id))))
                {
                    var person = character;
                    _ui.Button(content, person.Name + " · 打开人物档案", 20, y, 1330, () => ShowCharacter(person), true, 100); y += 120;
                }
            }
            else foreach (var entry in owned.Where(e => e.Get("type") == types[_journalTab] || _journalTab == 0 && e.Get("type") == "tape"))
            {
                var item = entry;
                var body = _ui.Text(content, item.Name + ((item.Get("type") == "item" || item.Get("type") == "tape") ? " ×" + _session.Inventory.Count(item.Id) : "") + "\n" + item.Get("text"), 30, y, 970, 115, 24, UiFactory.Ink);
                float height = Mathf.Max(115, body.GetPreferredValues(body.text, 970, 0).y + 25);
                body.rectTransform.sizeDelta = new Vector2(970, height);
                _ui.Button(content, "查看来源", 1060, y + 15, 270, () => ShowSources(item)); y += height + 30;
            }
            if (y == 10) _ui.Text(content, "此页还没有记录。调查中获得的内容会收录在这里。", 30, 50, 1300, 100, 26, UiFactory.Ink);
            content.sizeDelta = new Vector2(content.sizeDelta.x, Mathf.Max(580, y));
        }
        void ShowCharacter(CharacterDefinition character)
        {
            var modal = Modal(character.Name + " · 已知信息");
            float top = 290;
            if (character.Portrait)
            {
                var portrait = _ui.Box(modal, "Portrait", 1030, 190, 130, 130, Color.white).GetComponent<UnityEngine.UI.Image>();
                portrait.sprite = character.Portrait; portrait.preserveAspect = true; portrait.raycastTarget = false; top = 335;
            }
            var content = _ui.Scroll(modal, 410, top, 780, 630 - top, 700); float y = 0;
            foreach (var entry in _session.Content.Table("Entry").Where(e => e.Get("type") == "info" && e.List("owner").Contains(character.Id) && _session.Has(e.Id)))
            { _ui.Text(content, entry.Name + "\n" + entry.Get("text"), 10, y, 720, 110, 22); y += 125; }
            var puzzle = _session.Content.Table("Puzzle").FirstOrDefault(p => p.Get("type") == "person_path" && p.Get("target") == character.Id);
            if (puzzle != null) _ui.Button(content, _session.State.Solved.Contains(puzzle.Id) ? "路径已验证 · 查看" : "验证人物路径", 10, y, 710, () => ShowPuzzle(puzzle.Id), _session.Puzzles.Available(puzzle) && _session.State.Active == null);
        }
        void ShowSources(ContentRow entry)
        {
            var modal = Modal(entry.Name + " · 来源"); float y = 280;
            foreach (var sourceId in entry.List("source"))
            {
                var source = _session.Content.Require(sourceId);
                var recording = source.Table == "Record" ? source : source.Table == "Plot" ? _session.Recordings.RecordForLine(sourceId) : null;
                if (recording != null)
                {
                    var tape = _session.State.Tapes.FirstOrDefault(t => t.RecordId == recording.Id);
                    bool central = _session.Recordings.CanAccess(recording.Id);
                    bool portable = tape != null && _session.Recordings.CanAccess(recording.Id, tape.Id);
                    _ui.Button(modal, recording.Name + (central || portable ? " · 回看" : " · 需中控室权限或房间磁带"), 420, y, 760, () =>
                    {
                        ShowPlayback(recording.Id, central ? null : tape.Id);
                        if (source.Table == "Plot") { _session.Recordings.Seek(recording.Id, source.Number("at"), central ? null : tape.Id); UpdatePlayback(); }
                    }, central || portable);
                }
                else _ui.Text(modal, source.Table == "Plot" ? source.Get("text") : source.Name, 420, y, 760, 70, 21);
                y += 85;
            }
        }
        void ShowMap()
        {
            if (_screen != "Map") _mapBack = CurrentReturn();
            Page("Map", "古堡平面图", _mapBack);
            var floors = _session.Settings.Map.Floors.OrderBy(f => f.Floor).ToArray();
            if (!floors.Any(f => f.Floor == _floor)) _floor = floors[0].Floor;
            _ui.TabBar(_page, floors.Select(f => f.Floor + " 楼").ToArray(), Array.FindIndex(floors, f => f.Floor == _floor), index => { _floor = floors[index].Floor; ShowMap(); });
            _ui.Picture(_page, floors.First(f => f.Floor == _floor).Background, 80, 175, 1120, 600);
            _ui.Box(_page, "Map legend", 1220, 175, 300, 610, UiFactory.Panel);
            _ui.Text(_page, "图例与行程", 1240, 200, 265, 45, 26, UiFactory.Gold);
            _ui.StatusBadge(_page, "可进入", 1250, 265, UiFactory.Gold);
            _ui.StatusBadge(_page, "时段关闭", 1250, 325, new Color(.65f, .65f, .6f));
            _ui.StatusBadge(_page, "尚未解锁", 1250, 385, new Color(.45f, .5f, .46f));
            _ui.Text(_page, "当前位置\n" + _session.Content.Require(_session.State.Room).Name + "\n\n" + _session.Now.ToString("HH:mm") + "\n\n选择可进入的房间前往。", 1240, 450, 260, 210, 23);
            _ui.Button(_page, "收起地图", 1240, 705, 260, _mapBack);
            foreach (var node in _session.Settings.Map.Nodes.Where(n => n.Floor == _floor))
            {
                var point = node; string status = _session.RoomStatus(point.Room);
                bool here = point.Room == _session.State.Room;
                _ui.Button(_page, (here ? "◆ " : "") + _session.Content.Require(point.Room).Name + "\n" + (here ? "当前位置" : status), point.Position.x, Mathf.Max(190, point.Position.y), 330, () =>
                {
                    if (_session.State.Active != null) { Notify("请先结束当前交互，再前往其他房间。"); return; }
                    _session.Travel(point.Room); Resume();
                }, status == "可进入", 80);
            }
        }
        void ShowPuzzles()
        {
            Page("Puzzles", "分析与真相重构", ShowRoom); float y = 145; foreach (var p in _session.Content.Table("Puzzle")) { var q = p; bool open = _session.Puzzles.Available(q) || _session.State.Solved.Contains(q.Id); _ui.Button(_page, q.Name + (open ? "" : " · 未开放") + (_session.State.Solved.Contains(q.Id) ? " · 已完成" : ""), 130, y, 1240, () => ShowPuzzle(q.Id), open); y += 72; }
        }
        public void ShowPuzzle(string id)
        {
            _puzzle = id;
            var puzzle = _session.Content.Require(id);
            Page("Puzzle", puzzle.Name, ShowPuzzles);
            var draft = _session.Puzzles.Draft(id);
            string type = puzzle.Get("type");
            bool locked = _session.State.Solved.Contains(id);
            _ui.Text(_page, locked ? "已验证 · 结果已锁定" : "根据已收录证据填写；错误不会扣除道具或推进时间。", 80, 100, 1400, 45, 20, UiFactory.Gold);
            if (type == "calibrate") BuildCalibration(puzzle, draft, locked);
            else if (type == "person_path" || type == "timeline") BuildSlots(puzzle, draft, locked);
            else
            {
                var candidates = puzzle.List("candidates");
                var content = _ui.Scroll(_page, 90, 170, 1420, 485, candidates.Length * 115);
                float y = 0;
                foreach (var candidate in candidates)
                {
                    var value = candidate;
                    bool known = type != "anchor" || _session.State.MarkedLines.Contains(value);
                    string label = known ? CandidateLabel(value) : "尚未标记的声响 · 请先听取并标记录音";
                    _ui.Button(content, (draft.Values.Contains(value) ? "✓ " : "○ ") + label, 10, y, 1370, () =>
                    {
                        var values = draft.Values.ToList();
                        if (type == "anchor") { if (values.Contains(value)) values.Remove(value); else values.Add(value); }
                        else values = new System.Collections.Generic.List<string> { value };
                        _session.Puzzles.Edit(id, values.ToArray()); ShowPuzzle(id);
                    }, !locked && known, 98);
                    y += 115;
                }
            }
            _ui.Button(_page, "提交判断", 110, 755, 300, () =>
            {
                string error = _session.Puzzles.Submit(id);
                if (error != null) Notify(error); else Resume();
            }, !locked && DraftComplete(puzzle, draft));
            _ui.Button(_page, "查看调查册", 450, 755, 300, ShowJournal);
            if (locked && puzzle.Get("feedback") != "")
                _ui.Text(_page, string.Join("\n", _session.Content.Plot(puzzle.Get("feedback")).Select(l => l.Get("text"))), 790, 685, 700, 120, 21, UiFactory.Gold);
        }
        bool DraftComplete(ContentRow puzzle, PuzzleDraft draft)
        {
            int slots = puzzle.List("slots").Length;
            if (slots > 0) return draft.Values.Length == slots && draft.Values.All(v => !string.IsNullOrEmpty(v));
            if (puzzle.Get("type") == "calibrate") return draft.Values.Length == 2 && !string.IsNullOrEmpty(draft.Reference);
            return draft.Values.Length > 0;
        }
        string CandidateLabel(string id)
        {
            var row = _session.Content.Find(id);
            if (row == null)
            {
                var node = _session.Settings.Map.Nodes.FirstOrDefault(n => n.Id == id);
                return node == null ? id : _session.Content.Require(node.Room).Name + " · " + node.Floor + "楼";
            }
            if (row.Table == "Plot")
            {
                var record = _session.Recordings.RecordForLine(id);
                return _session.Recordings.Label(record.Id) + " · " + _session.Recordings.TimeAt(record.Id, row.Number("at"), true).ToString("HH:mm:ss") + "\n" + row.Get("text");
            }
            return row.Name + (row.Table == "Entry" ? "\n" + row.Get("text") : "");
        }
        void BuildCalibration(ContentRow puzzle, PuzzleDraft draft, bool locked)
        {
            var target = _session.Settings.Devices.Devices.First(d => d.Id == puzzle.Get("target"));
            _ui.Text(_page, target.Label + " · 拖动目标轨道，与可信参照的同次声响对齐", 100, 165, 1380, 40, 25);
            var shiftLabel = _ui.Text(_page, "", 100, 535, 1380, 80, 24, UiFactory.Gold);
            _ui.Box(_page, "Reference track", 125, 655, 1280, 3, UiFactory.Gold);
            _ui.Box(_page, "Target track", 125, 705, 1280, 3, UiFactory.Gold);
            var referenceMark = _ui.Box(_page, "Reference anchor", 765, 638, 4, 35, UiFactory.Paper);
            var targetMark = _ui.Box(_page, "Target anchor", 765, 688, 4, 35, UiFactory.Paper);
            var alignedTime = _ui.Text(_page, "选择一条目标声响和一条参照声响", 115, 607, 1320, 38, 19);
            Action<float> describe = value =>
            {
                shiftLabel.text = "移动量 " + value.ToString("+0;-0;0") + " 秒 · 待确认：设备" + (value >= 0 ? "慢" : "快") + Mathf.Abs(value).ToString("0") + "秒\n校正时间 = 报告时间 + 移动量；设备偏差 = −移动量";
                var targetLine = draft.Values.FirstOrDefault(line => _session.Recordings.RecordForLine(line).Get("device") == target.Id);
                var referenceLine = draft.Values.FirstOrDefault(line => _session.Recordings.RecordForLine(line).Get("device") == draft.Reference);
                if (targetLine != null && referenceLine != null)
                {
                    var targetRecord = _session.Recordings.RecordForLine(targetLine); var referenceRecord = _session.Recordings.RecordForLine(referenceLine);
                    var reported = _session.Recordings.TimeAt(targetRecord.Id, _session.Content.Require(targetLine).Number("at"), false).AddSeconds(value);
                    var referenceTime = _session.Recordings.TimeAt(referenceRecord.Id, _session.Content.Require(referenceLine).Number("at"), true);
                    targetMark.anchoredPosition = new Vector2(765 + Mathf.Clamp((float)(reported - referenceTime).TotalSeconds, -600, 600), -688);
                    alignedTime.text = "参照 " + referenceTime.ToString("HH:mm:ss") + "    目标（移动后）" + reported.ToString("HH:mm:ss");
                }
            };
            describe(draft.Shift);
            _ui.Slider(_page, 110, 480, 1320, -600, 600, draft.Shift, value => { if (locked) return; _session.Puzzles.Edit(puzzle.Id, draft.Values, value, draft.Reference); describe(draft.Shift); });
            float y = 225;
            foreach (var deviceId in puzzle.List("reference"))
            {
                var device = _session.Settings.Devices.Devices.First(d => d.Id == deviceId);
                _ui.Button(_page, (draft.Reference == deviceId ? "✓ " : "") + device.Label + " · 设为参照", 100, y, 450, () => { _session.Puzzles.Edit(puzzle.Id, draft.Values, draft.Shift, device.Id); ShowPuzzle(puzzle.Id); }, !locked); y += 65;
            }
            y = 215;
            foreach (var lineId in puzzle.List("candidates"))
            {
                string value = lineId; bool marked = _session.State.MarkedLines.Contains(value);
                _ui.Button(_page, marked ? (draft.Values.Contains(value) ? "✓ " : "") + CandidateLabel(value).Split('\n')[0] : "未标记的声响", 610, y, 870, () =>
                {
                    var values = draft.Values.ToList(); if (!values.Remove(value)) { if (values.Count == 2) values.RemoveAt(0); values.Add(value); }
                    _session.Puzzles.Edit(puzzle.Id, values.ToArray(), draft.Shift, draft.Reference); ShowPuzzle(puzzle.Id);
                }, !locked && marked, 70); y += 80;
            }
        }
        void BuildSlots(ContentRow puzzle, PuzzleDraft draft, bool locked)
        {
            var slots = puzzle.List("slots");
            if (draft.Values.Length != slots.Length && !locked) _session.Puzzles.Edit(puzzle.Id, new string[slots.Length]);
            if (_slot >= slots.Length) _slot = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                int slot = i; string value = draft.Values.ElementAtOrDefault(i);
                var slotButton = _ui.Button(_page, (_slot == i ? "▶ " : "") + slots[i] + "\n" + (string.IsNullOrEmpty(value) ? "未放置" : CandidateLabel(value)), 100 + i * (1400f / slots.Length), 170, 1360f / slots.Length, () => { _slot = slot; ShowPuzzle(puzzle.Id); }, !locked, 125);
                if (!locked)
                {
                    var token = slotButton.gameObject.AddComponent<DragToken>(); token.Slot = slot; token.Value = value;
                    slotButton.gameObject.AddComponent<DropZone>().Drop = drag =>
                    {
                        if (puzzle.Get("type") != "timeline" || string.IsNullOrEmpty(drag.Value)) return;
                        _slot = slot; PlaceSlot(puzzle, drag.Value);
                    };
                }
                if (puzzle.Get("type") == "timeline")
                    for (int bar = 0; bar < 20; bar++) _ui.Box(slotButton.transform, "Static local wave", 20 + bar * 19, 112, 4, 4 + 8 * Mathf.Abs(Mathf.Sin(bar)), UiFactory.Gold).GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            }
            bool path = puzzle.Get("type") == "person_path";
            _ui.Text(_page, path ? "选择或拖动标签到地图点位。标签可重复落在同一点；跨楼层保留。" : "拖动事件卡到槽位，或先选槽位再选卡；已有卡与目标位置交换。", 100, 310, 1360, 50, 20);
            if (path)
            {
                var floor = _session.Settings.Map.Floors.First(f => f.Floor == _floor);
                _ui.Picture(_page, floor.Background, 100, 370, 1000, 320);
                _ui.Button(_page, "楼层 " + _floor + " · 切换", 1170, 390, 280, () => { _floor = _floor == 1 ? 2 : 1; ShowPuzzle(puzzle.Id); });
                foreach (var node in _session.Settings.Map.Nodes.Where(n => n.Floor == _floor && puzzle.List("candidates").Contains(n.Id)))
                {
                    var n = node;
                    var point = _ui.Button(_page, _session.Content.Require(n.Room).Name, 100 + n.Position.x, 355 + n.Position.y / 2, 220, () => PlaceSlot(puzzle, n.Id), !locked);
                    if (!locked) point.gameObject.AddComponent<DropZone>().Drop = drag => { if (drag.Slot < 0) return; _slot = drag.Slot; PlaceSlot(puzzle, n.Id); };
                }
                for (int i = 0; i < draft.Values.Length; i++)
                {
                    var node = _session.Settings.Map.Nodes.FirstOrDefault(n => n.Id == draft.Values[i]);
                    if (node == null || node.Floor != _floor) continue;
                    _ui.Text(_page, slots[i], 100 + node.Position.x + i * 24, 325 + node.Position.y / 2, 80, 35, 23, UiFactory.Gold);
                    if (i == 0) continue;
                    var previous = _session.Settings.Map.Nodes.FirstOrDefault(n => n.Id == draft.Values[i - 1]);
                    if (previous == null || previous.Floor != _floor) continue;
                    var from = new Vector2(210 + previous.Position.x, 378 + previous.Position.y / 2);
                    var to = new Vector2(210 + node.Position.x, 378 + node.Position.y / 2);
                    var delta = to - from;
                    var line = _ui.Box(_page, "Path connection", from.x, from.y, delta.magnitude, 3, UiFactory.Gold);
                    line.localEulerAngles = new Vector3(0, 0, -Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
                }
                _ui.Text(_page, string.Join(" → ", draft.Values.Select(v => string.IsNullOrEmpty(v) ? "待定" : CandidateLabel(v))), 1150, 470, 340, 180, 22, UiFactory.Gold);
            }
            else
            {
                float y = 385;
                foreach (var candidate in puzzle.List("candidates").Where(_session.Has))
                {
                    var value = candidate; var card = _ui.Button(_page, CandidateLabel(value), 110, y, 1300, () => PlaceSlot(puzzle, value), !locked, 85);
                    if (!locked) card.gameObject.AddComponent<DragToken>().Value = value;
                    y += 95;
                }
            }
        }
        void PlaceSlot(ContentRow puzzle, string value)
        {
            var values = _session.Puzzles.Draft(puzzle.Id).Values.ToArray();
            if (puzzle.Get("type") == "timeline") { int old = Array.IndexOf(values, value); if (old >= 0) values[old] = values[_slot]; }
            values[_slot] = value; _session.Puzzles.Edit(puzzle.Id, values); ShowPuzzle(puzzle.Id);
        }
    }
}

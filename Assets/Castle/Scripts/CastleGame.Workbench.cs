using System;
using UnityEngine;
using UnityEngine.UI;
namespace Castle
{
    public sealed partial class CastleGame
    {
        int spatialMarker;
        string PlaybackText() => CastleRules.Clock((int)playPosition) + " / " + (file == 0 ? "08:00" : "12:00") + " · 文字回放";
        void ChooseFile(int index) { file = index; transcriptScroll = 1; selectedLine = -1; playPosition = 291; Go(ScreenId.Browse); }
        string[] Transcript() => new[] {
            "04:51  说话者 A\n" + (file == 0 ? "柜子旁边的东西，好像要掉下来了。" : "门是关着的。我看不见餐厅里发生了什么。"),
            "05:00  声音事件\n" + (file == 0 ? "［三声清晰的金属碰撞，紧接着是玻璃碎裂］" : "［隔门传来三声闷响，随后有轻微的玻璃碎裂声］"),
            "05:12  " + (state.identity ? "林女士" : "说话者 A") + "\n西翼那株白花，是我亲手栽下的。",
            "05:24  说话者 B\n我会去休息室等你。先别动那些碎片。" };
        void Browse()
        {
            Header(); var v = Current;
            B(v, "Rec01Button", () => ChooseFile(0), "REC-01\n餐厅" + (state.anchorA ? "  ◆" : ""), file == 0);
            B(v, "Rec02Button", () => ChooseFile(1), "REC-02\n书房" + (state.anchorB ? "  ◆" : ""), file == 1);
            B(v, "Rec03Button", () => Toast("此设备尚无可定位片段，先分析 REC-01 与 REC-02。"));
            B(v, "PersonalButton", () => ChooseFile(2), selected: file == 2); Visible(v, "PersonalButton", state.recorded);
            T(v, "FileInfoText", file == 2 ? "随身记录 · 女爵 / 会客厅 / 当晚 18:50" : "REC-0" + (file + 1) + " · " + (file == 0 ? "餐厅 / 21:25 开始 / 08:00" : "书房 / 21:22 开始 / 12:00"));
            Visible(v, "TranscriptRoot", file < 2); Visible(v, "ConversationRoot", file == 2); Visible(v, "PlaybackRoot", file < 2);
            Visible(v, "IdentityButton", state.confirmedEvent != null); B(v, "IdentityButton", () => Go(ScreenId.Identity));
            if (file == 2) { FitBody(v, "ConversationText", "ConversationScroll", string.Join("\n\n", database.greeting)); return; }
            var lines = Transcript(); transcriptButtons.Clear();
            for (int i = 0; i < 4; i++)
            {
                int line = i;
                B(v, "Row" + i, () => { selectedLine = line; playing = false; playPosition = new[] { 291, 300, 312, 324 }[line]; RefreshPlayback(); }, lines[i]);
                v.Get<Text>("Row" + i + "Label").fontSize = state.readingSize;
                transcriptButtons.Add(v.Get<Button>("Row" + i));
                if (i != 1) B(v, "Excerpt" + i, () => { string quote = "REC-0" + (file + 1) + " / " + Transcript()[line]; if (!state.excerpts.Contains(quote)) state.excerpts.Add(quote); Save(); Toast("原句和来源已收入调查册的事件摘录。"); });
            }
            B(v, "AnchorButton", ToggleAnchor, (file == 0 ? state.anchorA : state.anchorB) ? "◆ 已标记\n再次点击删除" : "◇ 标记锚点");
            B(v, "PlayButton", () => { playing = !playing; RefreshPlayback(); }); B(v, "MarkCompleteButton", () => Go(ScreenId.Align));
            S(v, "ProgressSlider", 0, file == 0 ? 480 : 720, playPosition, value => { playPosition = value; selectedLine = value < 300 ? 0 : value < 312 ? 1 : value < 324 ? 2 : 3; RefreshPlayback(); });
            progressSlider = v.Get<Slider>("ProgressSlider"); playLabel = v.Get<Text>("PlaybackText");
            RestoreScroll(v.Get<ScrollRect>("TranscriptScroll"), transcriptScroll); RefreshPlayback();
        }
        void RefreshPlayback()
        {
            var v = ui.Page(ScreenId.Browse);
            v.Get<Slider>("ProgressSlider").SetValueWithoutNotify(playPosition); T(v, "PlaybackText", PlaybackText()); T(v, "PlayButtonLabel", playing ? "暂停" : "播放文字");
            for (int i = 0; i < 4; i++) Select(v.Get<Button>("Row" + i), v.Get<Text>("Row" + i + "Label"), selectedLine == i);
        }
        void ToggleAnchor()
        {
            if (file == 0) { state.anchorA = !state.anchorA; state.pairA = state.anchorA; }
            else { state.anchorB = !state.anchorB; state.pairB = state.anchorB; }
            state.aligned = state.pathSaved = false; Save(); Render();
        }
        void Align()
        {
            Header(); var v = Current;
            Visible(v, "AnchorAButton", state.anchorA); Visible(v, "AnchorBButton", state.anchorB);
            B(v, "AnchorAButton", () => { state.pairA = !state.pairA; state.aligned = state.pathSaved = false; Save(); Render(); }, "锚点 1\nREC-01\n" + (state.pairA ? "✓ 已加入" : "加入对照"), state.pairA);
            B(v, "AnchorBButton", () => { state.pairB = !state.pairB; state.aligned = state.pathSaved = false; Save(); Render(); }, "锚点 2\nREC-02\n" + (state.pairB ? "✓ 已加入" : "加入对照"), state.pairB);
            B(v, "BackToBrowseButton", () => Go(ScreenId.Browse));
            B(v, "CheckButton", () => Check(CastleRules.Align(state), () => { Render(); Toast("时间关系初步成立，继续检查房间与门状态。"); }));
            B(v, "NextButton", () => Go(ScreenId.Sound));
            S(v, "OffsetSlider", -180, 360, state.offset, value => SetOffsetLive((int)value));
            offsetSlider = v.Get<Slider>("OffsetSlider"); offsetLabel = v.Get<Text>("OffsetText"); movingAnchor = v.Get<RectTransform>("TargetMarker");
            var drag = v.Get<CastleTimelineDrag>("TrackDrag"); drag.width = v.Get<RectTransform>("TargetRail").rect.width;
            drag.onDrag = delta => SetOffsetLive(Mathf.RoundToInt(state.offset + delta)); RefreshOffset();
        }
        void SetOffsetLive(int value) { CastleRules.ChangeTime(state, value); Changed(); RefreshOffset(); }
        void RefreshOffset()
        {
            var v = ui.Page(ScreenId.Align); T(v, "OffsetText", CastleRules.Offset(state.offset));
            T(v, "StatusText", state.aligned ? "✓ 时间对齐已检查" : "REC-02 校正量 · ← → 微调 1 秒");
            v.Get<Slider>("OffsetSlider").SetValueWithoutNotify(state.offset);
            PlaceAnchor(v.Get<RectTransform>("TargetRail"), v.Get<RectTransform>("TargetMarker"), (state.offset + 180) / 540f);
            PlaceAnchor(v.Get<RectTransform>("ReferenceRail"), v.Get<RectTransform>("ReferenceMarker"), 360 / 540f);
        }
        void PlaceAnchor(RectTransform rail, RectTransform marker, float value)
        {
            Vector3 position = rail.TransformPoint(new Vector3(Mathf.Lerp(rail.rect.xMin, rail.rect.xMax, value), rail.rect.center.y, 0));
            marker.position = position;
        }
        void Sound()
        {
            Header(); var v = Current;
            for (int i = 0; i < 3; i++) { int index = i; B(v, "Marker" + i, () => { spatialMarker = index; Render(); }, selected: spatialMarker == i); }
            T(v, "SourceInfoText", spatialMarker == 0 ? "声源：" + (string.IsNullOrEmpty(state.source) ? "尚未放置" : state.source) : spatialMarker == 1 ? "REC-01 · 餐厅\n清晰的金属碰撞" : "REC-02 · 书房\n隔门闷响与轻微碎裂");
            var doors = new[] { DoorState.Unknown, DoorState.Open, DoorState.Closed };
            for (int i = 0; i < 3; i++) { var door = doors[i]; B(v, "Door" + i, () => { Remember(); state.door = door; state.pathSaved = false; Save(); Render(); }, selected: state.door == door); }
            B(v, "EvidenceButton", () => { Remember(); state.doorEvidence = !state.doorEvidence; state.pathSaved = false; Save(); Render(); }, (state.doorEvidence ? "✓ 已关联门状态证据" : "关联门状态证据") + "\nREC-02 / 04:51\n“门是关着的……”", state.doorEvidence);
            B(v, "UndoButton", UndoDraft);
            B(v, "SaveDraftButton", () => { if (string.IsNullOrEmpty(state.source)) { Toast("先在地图选择声源房间。"); return; } state.pathSaved = true; Save(); Render(); });
            B(v, "VerifyButton", () => Check(CastleRules.VerifyEvent(state), () => Go(ScreenId.Event)));
            T(v, "DraftText", state.pathSaved ? "✓ 草稿已保存 · 可联合验证" : "先选择声源、门状态，并关联证据。"); BindMap(v, false, false);
        }
        void BindMap(CastleUiView v, bool navigation, bool route)
        {
            var map = v.Get<CastleMapView>("Map"); map.ShowFloor(floor, database);
            B(v, "Floor1Button", () => { floor = 1; if (navigation) { selectedRoom = null; DrawNavigation(); } else Render(); }, selected: floor == 1);
            B(v, "Floor2Button", () => { floor = 2; if (navigation) { selectedRoom = null; DrawNavigation(); } else Render(); }, selected: floor == 2);
            foreach (var item in map.Rooms)
            {
                var r = database.Room(item.RoomId);
                item.Label.text = navigation && !state.discovered.Contains(r.id) ? "未探索" : r.title;
                Select(item.Button, item.Label, navigation ? state.room == r.id : !route && state.source == r.title);
                if (navigation && !state.discovered.Contains(r.id)) item.Button.targetGraphic.color = new Color32(20,32,25,255);
                BindButton(item.Button, () => {
                    if (navigation) { if (r.id.StartsWith("stairs")) { floor = floor == 1 ? 2 : 1; selectedRoom = null; } else selectedRoom = r.id; DrawNavigation(); return; }
                    if (floor != 1) { Toast("本案的已知位置证据在一楼。"); return; }
                    if (!route && spatialMarker != 0) { Toast("请先选择声源 S，再在地图放置假设。"); return; }
                    Remember(); if (route) state.route[selectedNode] = r.title; else { state.source = r.title; state.pathSaved = false; } Save(); Render();
                });
            }
            if (navigation) return;
            if (route)
            {
                for (int i = 0; i < 3; i++) { map.Mark(i, state.route[i], (i + 1).ToString(), database); if (i > 0) map.Connect(state.route[i - 1], state.route[i], false, database); }
            }
            else { map.Mark(0, state.source, "S", database); map.Connect(state.source, "书房", state.door == DoorState.Closed, database); map.Connect(state.source, "餐厅", false, database); }
        }
        [Serializable] sealed class Draft { public string source; public DoorState door; public bool doorEvidence; public string[] route; }
        void Remember() { undo.Push(JsonUtility.ToJson(new Draft { source = state.source, door = state.door, doorEvidence = state.doorEvidence, route = (string[])state.route.Clone() })); }
        void UndoDraft()
        {
            if (undo.Count == 0) { Toast("没有可撤销的操作。"); return; }
            var d = JsonUtility.FromJson<Draft>(undo.Pop()); state.source = d.source; state.door = d.door; state.doorEvidence = d.doorEvidence; state.route = d.route; state.pathSaved = false; Save(); Render();
        }
    }
}

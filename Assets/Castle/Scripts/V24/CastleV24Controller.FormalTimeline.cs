using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Castle.V24
{
    public sealed partial class CastleV24Controller
    {
        private void TimelinePlayback()
        {
            var view = V("Timeline");
            if (_record != 620025) { _record = 620025; _position = 0; _heardPosition = 0; }
            SetPlaybackPlaying(false);
            _playTape = 0;
            S.Auto = false;
            S.RecordId = _record;
            S.RecordPosition = _position;
            S.PlaybackHeard = _heardPosition;
            S.PlayingTape = 0;
            var record = _data.RecordById(620025);
            var line = _data.Lines(record.Plot).LastOrDefault(x => x.At <= _position);
            Txt(view,"EndingStatus",line == null ? "真相正在显现" : SpeakerName(line,true));
            Txt(view,"EndingText",line?.Text ?? "");
            if (view.TryGet<Image>("Heroine",out var heroine)) { heroine.sprite=PortraitSprite(200001);heroine.preserveAspect=true; }
            if (view.TryGet<Image>("Partner",out var partner))
            {
                var npc = _data.Lines(record.Plot).LastOrDefault(x=>x.At<=_position&&x.SpeakerId!=0&&x.SpeakerId!=200001);
                partner.sprite=PortraitSprite(npc?.SpeakerId??200006); partner.preserveAspect=true;partner.enabled=partner.sprite!=null;
            }
            if (view.TryGet<V24EndingWave>("SoundWave",out var wave)) wave.Playing=_playing;
            Show(view,"Progress",false);Show(view,"Replay",false);
            view.Get<V24LineHit>("DialogueContinue").Clicked = AdvanceTimelineDialogue;
        }

        /// <summary>真相对白每次点击只前进一句；复用录音时刻保存位置，兼容已有存档。</summary>
        private void AdvanceTimelineDialogue()
        {
            if (!_data.Formal || _page != "Timeline" || !S.Ending || !S.EndingTransitionDone
                || _modal.Count > 0 || S.HeardRecords.Contains(620025)) return;

            var record = _data.RecordById(620025);
            var next = _data.Lines(record.Plot).FirstOrDefault(x => x.At > _position);
            _position = next == null ? record.Duration : (float)next.At;
            _heardPosition = _position;
            S.RecordId = record.Id;
            S.RecordPosition = _position;
            S.PlaybackHeard = _heardPosition;
            if (next == null)
            {
                // 最后一句仍须玩家点击确认，再按原流程结算时间线和奖励。
                Engine.CompleteRecord(record.Id);
                Refresh();
                return;
            }
            Engine.Commit();
            TimelinePlayback();
        }
        private string NodeName(V24Node node) => node.Room == 0 ? "连廊" : Engine.RoomName(node.Room);
        private Vector2 NodePoint(V24Node node) => node.Room == 0 ? new Vector2(.34f,.55f) : MapPoint(node.Room);
        private void BindDoors(CastleUiView view, V24Draft draft)
        {
            if (!view.TryGet<Button>("DoorOpen",out var open) || !view.TryGet<Button>("DoorClosed",out var closed)) return;
            bool visible=_data.Formal && _data.PuzzleById(draft.Id).Type=="sound_path";
            open.gameObject.SetActive(visible);closed.gameObject.SetActive(visible);
            if (!visible) return;
            while(draft.Doors.Count<draft.Values.Count)draft.Doors.Add("");
            if (draft.Values.Count == 0) return;
            _slot=Mathf.Clamp(_slot,0,draft.Values.Count-1);
            // 回调绑定当前标签，避免后续页面改变共享选择索引时写入其他位置。
            int selectedSlot = _slot;
            Btn(view,"DoorOpen",()=>{draft.Doors[selectedSlot]="open";Engine.Commit();BindDoors(view,draft);},draft.Doors[selectedSlot]=="open"?"● 打开":"打开");
            Btn(view,"DoorClosed",()=>{draft.Doors[selectedSlot]="closed";Engine.Commit();BindDoors(view,draft);},draft.Doors[selectedSlot]=="closed"?"● 关闭":"关闭");
            var map = _data.MapConfig.First(x => x.Id == _data.PuzzleById(draft.Id).Map);
            Txt(view, "Details", string.Join("\n", draft.Values.Select((value, index) =>
            {
                var node = map.Nodes.FirstOrDefault(x => x.Id == value);
                string door = draft.Doors[index] == "open" ? "打开" : draft.Doors[index] == "closed" ? "关闭" : "未设置";
                return (index == selectedSlot ? "▶ " : "") + (char)('A' + index) + " " + (node == null ? "待放置" : NodeName(node)) + "：" + door;
            })));
        }
    }
}

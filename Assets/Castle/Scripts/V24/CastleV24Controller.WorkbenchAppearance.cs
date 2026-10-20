using System;
using System.Linq;
using Castle.Flow;
using UnityEngine;
using UnityEngine.UI;

namespace Castle.V24
{
    public sealed partial class CastleV24Controller
    {
        public CastleUiView PathLabelPrefab, TimelineCluePrefab;
        public Sprite PersonPathMarkerIcon;
        private int[] _timelineClueOrder;

        private void PreparePathView(CastleUiView view)
        {
            if (view.Id != "Path") return;
            Btn(view,"Recordings",()=>{CloseAll();Page("Recordings");},"录音文本");
            Btn(view,"Anchors",()=>{CloseAll();Page("Anchors");},"锚点对齐");
            Btn(view,"Space",()=>{},"空间推演");
            Btn(view,"JournalShortcut",()=>{int target=_data.PuzzleById(_puzzle).Target;CloseAll();Journal(0,target,true);});
            Btn(view,"MapShortcut",()=>Map());
            Btn(view,"SettingsShortcut",Settings);
            foreach (string id in new[]{"Recordings","Anchors","Space"})
            {
                var label=view.Get<Button>(id).GetComponentInChildren<Text>(true);
                label.font=WorkbenchFont;
                label.color = id=="Space" ? Color.white : new Color(.62f,.67f,.59f);
            }
            var underline=view.Get<RectTransform>("FloorUnderline");
            underline.anchoredPosition=new Vector2(_floor==1?1312:1400,-305);
        }

        private int[] TimelineClueOrder(int[] candidates)
        {
            if (_timelineClueOrder == null || !_timelineClueOrder.OrderBy(x=>x).SequenceEqual(candidates.OrderBy(x=>x)))
                _timelineClueOrder=V24Engine.ShuffleTimelineClues(candidates,new System.Random());
            return _timelineClueOrder;
        }

        private void RenderTimelineSlots(CastleUiView view, V24Draft draft, int count)
        {
            for(int i=0;i<count;i++)
            {
                int slot=i;
                var entry=_data.EntryById(draft.Values[i]);
                var card=view.Get<CastleUiView>("Slot"+i);
                Txt(card,"Title",entry?.Name??"");
                card.Get<Text>("Title").font=ReadingFont;
                Txt(card,"Body","");
                Btn(card,"Select",()=>PlaceTimeline(slot,_selectedEntry),enabled:!S.Ending);
                var drag=card.Get<FlowDrag>("Drag");
                drag.Dropped=source=>{if(!S.Ending && source.Kind=="event")PlaceTimeline(slot,int.Parse(source.Id));};
                drag.CanDrag=!S.Ending && entry!=null;drag.Kind="event";drag.Id=draft.Values[i].ToString();
                drag.DragRoot=(RectTransform)view.transform;
            }
        }

        private void TapeView()
        {
            var view=V("Tape");
            bool night=Engine.Now.Hour>=18 || Engine.Now.Hour<6;
            var art=RoomArt.FirstOrDefault(x=>x.Room==S.Room);
            var texture=art==null?null:night?art.Night:art.Day;
            view.Get<RawImage>("Backdrop").texture=texture?texture:_defaultRoomBackground;
            Show(view,"DialoguePanel",true);
            PlaybackRender();
        }
    }
}

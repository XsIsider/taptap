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
            // 真相揭示保留白色波形，收起排序用的红线、槽框及槽内卡片。
            Show(view, "Slots", !S.Ending);
            for (int i = 1; i < count; i++) Show(view, "Connection" + i, !S.Ending);
            for(int i=0;i<count;i++)
            {
                int slot=i;
                var entry=_data.EntryById(draft.Values[i]);
                var card=view.Get<CastleUiView>("Slot"+i);
                Txt(card,"Title","");
                Txt(card,"Body","");
                RenderPlacedTimelineClue(card,entry);
                Btn(card,"Select",()=>PlaceTimeline(slot,_selectedEntry),enabled:!S.Ending);
                var drag=card.Get<FlowDrag>("Drag");
                drag.Dropped=source=>{if(!S.Ending && source.Kind=="event")PlaceTimeline(slot,int.Parse(source.Id));};
                drag.CanDrag=!S.Ending && entry!=null;drag.Kind="event";drag.Id=draft.Values[i].ToString();
                drag.DragRoot=(RectTransform)view.transform;
            }
        }

        /// <summary>槽内复用完整线索卡外观；拖放仍由固定槽位接收，避免子卡片重复处理。</summary>
        private void RenderPlacedTimelineClue(CastleUiView slot, V24Entry entry)
        {
            var child = slot.transform.Find("PlacedClue");
            var placed = child ? child.GetComponent<CastleUiView>() : null;
            if (entry == null)
            {
                if (placed) placed.gameObject.SetActive(false);
                return;
            }
            if (!placed)
            {
                placed = Instantiate(TimelineCluePrefab, slot.transform);
                placed.name = "PlacedClue";
                var rect = (RectTransform)placed.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                placed.Get<FlowDrag>("Drag").enabled = false;
                placed.Get<Button>("Select").enabled = false;
                placed.GetComponent<CanvasGroup>().blocksRaycasts = false;
            }
            Txt(placed,"Title",entry.Name);
            placed.Get<Text>("Title").font = ReadingFont;
            placed.gameObject.SetActive(true);
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

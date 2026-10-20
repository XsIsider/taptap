using System;
using System.Collections;
using System.Linq;
using Castle.Flow;
using UnityEngine;
using UnityEngine.UI;
namespace Castle.V24
{
    public sealed partial class CastleV24Controller
    {
        public Image PathLinePrefab;
        // 锚点详情从出现到完全淡出的时长，单位为秒。
        private const float AnchorDetailsFadeSeconds = 5f;
        private float _anchorDetailsUntil;
        private bool SpaceAvailable => !Engine.IntroDone && Engine.Need(_data.PuzzleById(_data.GameSettings.IntroSoundPath).Need);
        private void OpenSpace(){if(SpaceAvailable){_puzzle=_data.GameSettings.IntroSoundPath;Page("Space");}}
        private void Tabs(CastleUiView v)
        {
            Btn(v,"Recordings",()=>{_playTape=0;Page("Recordings");},"录音文本");Btn(v,"Anchors",()=>Page("Anchors"),Engine.IntroDone?"文本对照":"锚点对齐");
            foreach(var tab in new[]{"Recordings","Anchors","Space"})
            {
                var label=v.Get<Button>(tab).GetComponentInChildren<Text>(true);
                label.color=v.Id==tab?Color.white:new Color(.62f,.67f,.59f);
            }
            Show(v,"Space",SpaceAvailable);Btn(v,"Space",OpenSpace,"空间推演",SpaceAvailable);
        }
        private void Recordings()
        {
            if (_data.Formal && Engine.ImportRecords()>0) Toast("文件录入成功");
            var v=V("Recordings");_playing=false;Tabs(v);Show(v,"PreviousPuzzle",false);Show(v,"NextPuzzle",false);Show(v,"PuzzleName",false);
            var root=v.Get<RectTransform>("List");Clear(root);
            var records=_data.Record.Where(Engine.Accessible).OrderBy(x=>x.Device).ThenBy(x=>x.Id).ToArray();
            if(!records.Any(x=>x.Id==_record))
            {
                _record=records.FirstOrDefault()?.Id??0;_position=0;
                if(_record!=0&&!S.ExpandedDevices.Contains(_data.RecordById(_record).Device))S.ExpandedDevices.Add(_data.RecordById(_record).Device);
            }
            foreach(var group in records.GroupBy(x=>x.Device))
            {
                int device=group.Key;bool expanded=S.ExpandedDevices.Contains(device);
                var folder=Card(root,_data.DeviceConfig.First(x=>x.Id==device).Label,"",()=>{if(expanded)S.ExpandedDevices.Remove(device);else S.ExpandedDevices.Add(device);Recordings();},template:RecordingFolderPrefab);
                folder.Get<Image>("Icon").sprite=expanded?FolderOpen:FolderClosed;
                if(!expanded)continue;
                foreach(var record in group)
                {
                    int id=record.Id;
                    var card=Card(root,record.Name,"",()=>{_record=id;_position=0;_heardPosition=0;_audioLine=0;_playing=false;Recordings();},template:RecordingFilePrefab);card.name="Record"+id;
                    card.Get<Image>("Icon").sprite=id==_record?PauseIcon:PlayIcon;
                    card.Get<Text>("Title").color=id==_record?new Color(1,.87f,.5f):Color.white;
                }
            }
            Btn(v,"Done",()=>Page("Anchors"),"标记完成");PlaybackRender();Save();
        }
        private Coroutine _tapeScrollRoutine;
        private int _tapeScrollRecord;
        private int _recordingScrollRecord;
        private void PlaybackRender(bool revealLatest = false)
        {
            var v=V(_page=="Tape"?"Tape":"Recordings");
            var scroll = _page == "Tape" ? v.Get<ScrollRect>("LinesScroll") : null;
            var recordingScroll = _page == "Recordings" ? v.Get<ScrollRect>("LinesScroll") : null;
            float recordingScrollPosition = recordingScroll != null && _recordingScrollRecord == _record
                ? recordingScroll.verticalNormalizedPosition : 1f;
            if (recordingScroll != null) _recordingScrollRecord = _record;
            bool follow = false;
            Vector2 scrollPosition = Vector2.zero;
            if (scroll != null)
            {
                if (_tapeScrollRoutine != null) StopCoroutine(_tapeScrollRoutine);
                _tapeScrollRoutine = null;
                float hiddenHeight = Mathf.Max(0f, scroll.content.rect.height - scroll.viewport.rect.height);
                // 先按旧内容判断是否贴近底部；向上查看历史时保留原来的像素位置。
                follow = revealLatest || _tapeScrollRecord != _record || hiddenHeight <= 16f || scroll.verticalNormalizedPosition * hiddenHeight <= 16f;
                scrollPosition = scroll.content.anchoredPosition;
                _tapeScrollRecord = _record;
            }
            Clear(v.Get<RectTransform>("Lines"));
            if(_record==0)
            {
                Txt(v,"FileInfo","暂无可访问录音");Btn(v,"Play",null,"播放",false);
                if (recordingScroll != null) RestoreRecordingScroll(recordingScroll, 1f);
                return;
            }
            var record=_data.RecordById(_record);_position=Mathf.Clamp(_position,0,record.Duration);
            Txt(v,"FileInfo",record.Name+" · "+Engine.DeviceName(record.Device));
            foreach(var plot in _data.Lines(record.Plot).Where(x=>_page!="Tape" || x.At<=_position))
            {
                if(!S.Shown.Contains(plot.Id))S.Shown.Add(plot.Id);
                if(_page=="Tape")Line(v.Get<RectTransform>("Lines"),plot,true);
                else RecordingLine(v.Get<RectTransform>("Lines"),plot);
            }
            Btn(v,"Play",()=>{if(_page=="Tape"||_data.Formal){if(_position>=record.Duration){_position=0;_heardPosition=0;_audioLine=0;if(_page=="Tape")PlaybackRender(true);}SetPlaybackPlaying(!_playing);}}," ");
            Slide(v,"Progress",0,record.Duration,_position,x=>{_position=x;_audioLine=0;if(Voice)Voice.Stop();if(Effects)Effects.Stop();if(_page=="Tape")PlaybackRender(true);else {HighlightRecording();PlaybackTick(false);Save();}});
            Show(v,"Extract",false);PlaybackTick(false);if(_page!="Tape")HighlightRecording();
            if (recordingScroll != null) RestoreRecordingScroll(recordingScroll, recordingScrollPosition);
            if (scroll != null) _tapeScrollRoutine = StartCoroutine(RestoreTapeScroll(scroll, follow, scrollPosition, _record));
        }
        /// <summary>正文布局完成后恢复位置并重算滑块，避免旧文件的越界位置把滑块压缩为零。</summary>
        private static void RestoreRecordingScroll(ScrollRect scroll, float position)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = Mathf.Clamp01(position);
            // 显式刷新缓存和滑块尺寸，不依赖玩家再次拖动来触发 ScrollRect 更新。
            scroll.Rebuild(CanvasUpdate.PostLayout);
        }
        private IEnumerator RestoreTapeScroll(ScrollRect scroll, bool follow, Vector2 position, int recordId)
        {
            // 等待旧行销毁与新行布局完成，避免立即置底时仍使用上一帧的内容高度。
            yield return null;
            _tapeScrollRoutine = null;
            if (_page != "Tape" || _record != recordId || !scroll || !scroll.isActiveAndEnabled) yield break;
            Canvas.ForceUpdateCanvases();
            if (follow)
            {
                scroll.StopMovement();
                scroll.verticalNormalizedPosition = 0f;
            }
            else scroll.content.anchoredPosition = position;
        }
        private void PlaybackTick(bool redraw=true)
        {
            var v=V(_page=="Tape"?"Tape":"Recordings");var record=_data.RecordById(_record);if(record==null)return;Txt(v,"FileInfo",record.Name+" · "+Engine.DeviceName(record.Device));
            if(redraw && _page=="Tape")
            {
                int count=_data.Lines(record.Plot).Count(x=>x.At<=_position);
                if(v.Get<RectTransform>("Lines").Cast<Transform>().Count(x=>x.gameObject.activeSelf)!=count){PlaybackRender();return;}
            }
            string time="相对进度";var snapshot=S.Recordings.FirstOrDefault(x=>x.Id==_record);
            if(snapshot!=null)time=new DateTime(Engine.DisplayStart(snapshot,record.Device)).AddSeconds(_position).ToString("HH:mm:ss");
            Txt(v,"Playback",_page=="Tape"?time+" · "+TimeSpan.FromSeconds(_position).ToString(@"mm\:ss")+" / "+TimeSpan.FromSeconds(record.Duration).ToString(@"mm\:ss"):TimeSpan.FromSeconds(_position).ToString(@"mm\:ss"));
            if(_page!="Tape")Txt(v,"Duration",TimeSpan.FromSeconds(record.Duration).ToString(@"mm\:ss"));
            v.Get<Slider>("Progress").SetValueWithoutNotify(_position);v.Get<Button>("Play").GetComponent<Image>().sprite=_playing?PauseIcon:PlayIcon;Txt(v,"PlayLabel","");S.RecordId=_record;S.RecordPosition=_position;
        }
        private void RecordingLine(Transform parent,V24Plot plot)
        {
            var row=Instantiate(RecordingLinePrefab,parent);row.gameObject.SetActive(true);row.name="Line"+plot.Id;
            Txt(row,"Time",TimeSpan.FromSeconds(plot.At).ToString(@"mm\:ss"));Txt(row,"Speaker",SpeakerName(plot,true)+"：");
            var body=row.Get<TMPro.TextMeshProUGUI>("Body");body.text=plot.Text;
            float height=body.GetPreferredValues(plot.Text,850,0).y;
            body.rectTransform.sizeDelta=new Vector2(850,Mathf.Max(65,height));
            row.Get<LayoutElement>("Layout").preferredHeight=Mathf.Max(138,70+height);
            Show(row,"Marker",S.Marked.Contains(plot.Id));Btn(row,"Select",()=>ToggleRecordingLine(plot.Id));
        }
        private void HighlightRecording()
        {
            var current=_data.Lines(_data.RecordById(_record).Plot).LastOrDefault(x=>x.At<=_position);
            foreach(var row in V("Recordings").Get<RectTransform>("Lines").GetComponentsInChildren<CastleUiView>())
                row.Get<Image>("Background").color=row.name=="Line"+current?.Id?new Color(1,1,1,.035f):Color.clear;
        }
        private void ToggleRecordingLine(int id)
        {
            bool changed=Engine.ToggleMark(id);
            if (_data.Formal && S.RewardPopups.Count > 0) { Reward(); return; }
            Toast(changed?(S.Marked.Contains(id)?"已标记信息":"已取消标记"):"这句话似乎没什么用");
            if(_page=="Tape"){PlaybackRender();return;}
            foreach(var row in V("Recordings").Get<RectTransform>("Lines").GetComponentsInChildren<CastleUiView>())
                if(row.name=="Line"+id)Show(row,"Marker",S.Marked.Contains(id));
        }
        private V24Puzzle[] AnalysisPuzzles()=>_data.Puzzle.Where(x=>x.Type==(Engine.IntroDone?"logic_match":"anchor_align")).ToArray();
        private void Anchors()
        {
            var v=V("Anchors");Tabs(v);var puzzles=AnalysisPuzzles();if(!puzzles.Any(x=>x.Id==_puzzle))_puzzle=puzzles[0].Id;
            int index=Array.FindIndex(puzzles,x=>x.Id==_puzzle);var puzzle=_data.PuzzleById(_puzzle);var draft=Engine.Draft(_puzzle);
            Txt(v,"PuzzleName",puzzle.Name+" "+(index+1)+"/"+puzzles.Length);
            Btn(v,"PreviousPuzzle",()=>{_puzzle=puzzles[(index+puzzles.Length-1)%puzzles.Length].Id;Anchors();});Btn(v,"NextPuzzle",()=>{_puzzle=puzzles[(index+1)%puzzles.Length].Id;Anchors();});
            Clear(v.Get<RectTransform>("List"));Clear(v.Get<RectTransform>("Tracks"));
            foreach(int id in S.Marked)
            {
                var plot=_data.Plot.First(x=>x.Id==id);
                var card=Card(v.Get<RectTransform>("List"),"锚点 "+(S.Marked.IndexOf(id)+1),plot.Text,()=>{_selectedLine=id;ShowAnchorDetails(true);},template:AnchorCardPrefab);
                var drag=card.Get<FlowDrag>("Drag");drag.CanDrag=true;drag.Kind="evidence";drag.Id=id.ToString();
                drag.DragRoot=(RectTransform)v.transform;
            }
            int capacity=puzzle.Answer.Split('|').Length;
            v.Get<FlowDrag>("DropZone").Dropped=drag=>{if(drag.Kind=="evidence")AddEvidence(int.Parse(drag.Id),capacity);};
            if(v.TryGet<Button>("Add",out _))Btn(v,"Add",()=>AddEvidence(_selectedLine,capacity));
            foreach(int id in draft.Values.ToArray())
            {
                var plot=_data.Plot.First(x=>x.Id==id);var record=_data.Record.First(x=>x.Plot==plot.Group);
                var track=Instantiate(Ui.TrackPrefab,v.Get<RectTransform>("Tracks"));track.gameObject.SetActive(true);
                track.GetComponent<FlowDrag>().Dropped=drag=>
                {
                    if(drag.Kind!="evidence")return;int value=int.Parse(drag.Id);if(draft.Values.Contains(value))return;
                    int slot=draft.Values.IndexOf(id);draft.Values[slot]=value;if(draft.Reference==id)draft.Reference=0;Engine.Commit();Anchors();
                };
                Txt(track,"Name",record.Name);Txt(track,"Offset",Engine.DeviceName(record.Device));
                Btn(track,"Remove",()=>{draft.Values.Remove(id);if(draft.Reference==id)draft.Reference=0;Engine.Commit();Anchors();});
                bool anchor=puzzle.Type=="anchor_align";Show(track,"Reference",anchor);Show(track,"OffsetSlider",anchor);
                if(anchor)
                {
                    Btn(track,"Reference",()=>{draft.Reference=id;draft.Shift=0;Engine.Commit();Anchors();},draft.Reference==id?"参考轨":"设为参考");
                    float delta=draft.Reference==0?0:(float)(AnchorSeconds(id)-AnchorSeconds(draft.Reference));
                    Slide(track,"OffsetSlider",-600,600,draft.Reference==id?0:draft.Shift+delta,x=>{draft.Shift=Mathf.Round(x-delta);Txt(track,"Offset",AnchorClock(id,draft.Reference==id?0:draft.Shift));Engine.Commit();});
                    track.Get<Slider>("OffsetSlider").interactable=draft.Reference!=id;
                    track.Get<Text>("Offset").fontSize=18;Txt(track,"Offset",AnchorClock(id,draft.Reference==id?0:draft.Shift));
                }
                else {Txt(track,"Offset",plot.Text);var body=track.Get<Text>("Offset");body.rectTransform.anchoredPosition=new Vector2(18,-50);body.rectTransform.sizeDelta=new Vector2(850,160);body.fontSize=21;body.verticalOverflow=VerticalWrapMode.Overflow;track.GetComponent<LayoutElement>().preferredHeight=Mathf.Max(180,body.preferredHeight+70);Txt(track,"Name",record.Name+" · "+Engine.DeviceName(record.Device));track.Get<Text>("Name").rectTransform.sizeDelta=new Vector2(740,36);track.Get<Text>("Name").fontSize=18;}
            }
            for(int i=draft.Values.Count;i<3;i++)
            {
                bool usable=i<capacity;var empty=Instantiate(Ui.TrackPrefab,v.Get<RectTransform>("Tracks"));empty.gameObject.SetActive(true);
                Txt(empty,"Name",puzzle.Type=="anchor_align"?(i==0?"（参考轨道）":"（校准轨道）"):"槽位 "+(i+1));Txt(empty,"Offset",usable?"拖入已标记片段":"本题不使用");Show(empty,"Reference",false);Show(empty,"OffsetSlider",false);Show(empty,"Remove",false);
                empty.GetComponent<FlowDrag>().Dropped=drag=>{if(usable&&drag.Kind=="evidence")AddEvidence(int.Parse(drag.Id),capacity);};
            }
            Txt(v,"Status",S.Puzzles.Contains(_puzzle)?"已完成":Engine.Need(puzzle.Need)?"拖入片段后验证":"尚缺前置证据");
            Btn(v,"Check",()=>SubmitPuzzle("Anchors"),puzzle.Type=="anchor_align"?"检查对齐":"验证关联",draft.Values.Count==capacity&&Engine.Need(puzzle.Need));
            if(v.TryGet<Button>("Next",out _)){Show(v,"Next",SpaceAvailable);Btn(v,"Next",OpenSpace,"声音路径 →",SpaceAvailable);}
            ShowAnchorDetails();
        }
        /// <summary>显示锚点正文和来源；只有主动点击才重置淡出，未知位置不提前揭晓。</summary>
        private void ShowAnchorDetails(bool restartFade = false)
        {
            var view = V("Anchors");
            bool selected = S.Marked.Contains(_selectedLine);
            if (selected && restartFade) _anchorDetailsUntil = Time.unscaledTime + AnchorDetailsFadeSeconds;
            bool visible = selected && _anchorDetailsUntil > Time.unscaledTime;
            Show(view, "AnchorDetails", visible);
            if (!visible) { _anchorDetailsUntil = 0f; return; }

            var plot = _data.Plot.First(x => x.Id == _selectedLine);
            var record = _data.Record.FirstOrDefault(x => x.Plot == plot.Group);
            var device = record == null ? null : _data.DeviceConfig.FirstOrDefault(x => x.Id == record.Device);
            string location = device != null && device.Room != 0 && S.KnownDevices.Contains(device.Id)
                ? Engine.RoomName(device.Room) : "位置未知";
            string deviceName = device?.Label ?? "未知设备";
            // 单行宽度与省略规则保存在场景；先移除原文换行，避免挤占下方来源行。
            view.Get<TMPro.TextMeshProUGUI>("AnchorDetailsText").text = plot.Text.Replace("\r", " ").Replace("\n", " ");
            view.Get<TMPro.TextMeshProUGUI>("AnchorDetailsSource").text =
                location + "(" + deviceName + ") " + (record?.Name ?? "录音文件未知");
            UpdateAnchorDetailsFade();
        }
        private void UpdateAnchorDetailsFade()
        {
            if (_anchorDetailsUntil <= 0f) return;
            var group = V("Anchors").Get<RectTransform>("AnchorDetails").GetComponent<CanvasGroup>();
            if (_page != "Anchors" || !S.Marked.Contains(_selectedLine)) _anchorDetailsUntil = 0f;
            float remaining = Mathf.Clamp01((_anchorDetailsUntil - Time.unscaledTime) / AnchorDetailsFadeSeconds);
            group.alpha = Mathf.SmoothStep(0f, 1f, remaining);
            if (remaining <= 0f)
            {
                _anchorDetailsUntil = 0f;
                group.gameObject.SetActive(false);
            }
        }
        private double AnchorSeconds(int id)
        {
            var line=_data.Plot.First(x=>x.Id==id);var record=_data.Record.First(x=>x.Plot==line.Group);var snapshot=S.Recordings.First(x=>x.Id==record.Id);
            return new TimeSpan(Engine.DisplayStart(snapshot,record.Device)).TotalSeconds+line.At;
        }
        private string AnchorClock(int id,float shift)=>new DateTime((long)((AnchorSeconds(id)+shift)*TimeSpan.TicksPerSecond)).ToString("HH:mm:ss")+" ("+shift.ToString("+0;-0;0")+"秒)";
        private void AddEvidence(int id,int capacity)
        {
            if(id==0||!S.Marked.Contains(id))return;var d=Engine.Draft(_puzzle);
            if(d.Values.Contains(id)){Toast("该片段已加入");return;}
            if(d.Values.Count>=capacity){Toast("请先移除一个片段");return;}
            d.Values.Add(id);Engine.Commit();Anchors();
        }
        private void SubmitPuzzle(string source)
        {
            S.ReturnPage=source=="Path"?"Explore":source;if(source=="Path"&&!_data.Formal)S.ReturnPath=_puzzle;string result=Engine.ValidatePuzzle(_puzzle);Toast(result);
            if(result!="成功"){if(source=="Path")S.ReturnPath=0;return;}
            if(S.Ending){CloseAll();Page("Timeline");StartCoroutine(RevealEnding());return;}
            Engine.ContinuePuzzleReward();Refresh();
        }
        private IEnumerator RevealEnding()
        {
            for(float t=0;t<1.6f;){if(_modal.Count==0&&_page=="Timeline")t+=Time.unscaledDeltaTime;yield return null;}
            if (_data.Formal) { S.EndingTransitionDone=true;_record=620025;_position=0;_heardPosition=0;_playing=true;Engine.Commit();Timeline();yield break; }
            S.EndingTransitionDone=true;Engine.Commit();Engine.ContinuePuzzleReward();Refresh();
        }
        private void PathPage(){if(!SpaceAvailable){Page("Recordings");return;}_puzzle=_data.GameSettings.IntroSoundPath;Tabs(V("Space"));PathRender(V("Space"));}
        private void PathRender(CastleUiView v)
        {
            var puzzle=_data.PuzzleById(_puzzle);var draft=Engine.Draft(_puzzle);_floor=draft.Floor;string[] labels=puzzle.Answer.Split('|').Select(x=>x.Split('=')[0]).ToArray();
            if (_data.Formal && puzzle.Type=="person_path") labels=_data.PersonPathPolicy.Nodes.OrderBy(x=>x.Step).Select(x=>x.Time).ToArray();
            while(draft.Values.Count<labels.Length)draft.Values.Add(0);
            _slot=Mathf.Clamp(_slot,0,labels.Length-1);
            if(v.Id=="Path")Btn(v,"Close",Pop);
            else {Txt(v,"PuzzleName",puzzle.Name);Show(v,"PreviousPuzzle",false);Show(v,"NextPuzzle",false);}
            draft.Floor=_floor=Mathf.Clamp(draft.Floor,1,2);MapArt(v,_floor);
            Btn(v,"Floor1",()=>{draft.Floor=1;PathRender(v);});Btn(v,"Floor2",()=>{draft.Floor=2;PathRender(v);});
            var root=v.Get<RectTransform>("Rooms");Clear(root);var list=v.Get<RectTransform>("List");Clear(list);
            var map=_data.MapConfig.First(x=>x.Id==puzzle.Map);
            for(int i=0;i<labels.Length;i++)
            {
                int slot=i;int value=draft.Values[i];var node=map.Nodes.FirstOrDefault(x=>x.Id==value);
                var card=Card(list,labels[i],node==null?"拖到地图地点":NodeName(node)+" "+node.Floor+"F",()=>{_slot=slot;Txt(v,"Details","已选择标签 "+labels[slot]+"，点击或拖放到地点。");BindDoors(v,draft);},template:v.Id=="Space"?SpaceCardPrefab:null);
                if(puzzle.Type=="person_path") {var title=card.Get<Text>("Title");title.resizeTextForBestFit=true;title.resizeTextMinSize=12;title.resizeTextMaxSize=20;title.horizontalOverflow=HorizontalWrapMode.Overflow;}
                var drag=card.Get<FlowDrag>("Drag");drag.CanDrag=true;drag.Kind="path";drag.Id=slot.ToString();
                if(v.Id=="Space")drag.DragRoot=(RectTransform)v.transform;
            }
            foreach(var node in map.Nodes.Where(x=>x.Floor==_floor&&puzzle.Candidates.Contains(x.Id)))
            {
                string placed=string.Join(" ",Enumerable.Range(0,labels.Length).Where(i=>draft.Values[i]==node.Id).Select(i=>labels[i]));
                var card=Card(root,NodeName(node),"",()=>
                {
                    // 空间页点击已放置的房间是选择；移动到已有标签的房间仍可通过拖放完成。
                    int placedSlot = draft.Values.IndexOf(node.Id);
                    if (v.Id == "Space" && placedSlot >= 0) _slot = placedSlot;
                    else draft.Values[_slot] = node.Id;
                    Engine.Commit();PathRender(v);
                },true);card.name="Node"+node.Id;
                if(node.Room!=0)PlaceMapRegion(card,root,node.Room);
                else {var rect=(RectTransform)card.transform;var point=NodePoint(node);rect.anchorMin=rect.anchorMax=new Vector2(point.x,1-point.y);rect.anchoredPosition=Vector2.zero;rect.sizeDelta=new Vector2(95,160);}
                card.Get<Image>("Background").color=v.Id=="Space"?Color.clear:new Color(.15f,.25f,.18f,.5f);
                card.Get<FlowDrag>("Drag").Dropped=drag=>{if(drag.Kind=="path"){draft.Values[int.Parse(drag.Id)]=node.Id;Engine.Commit();PathRender(v);}};
                card.Get<FlowPointer>("Pointer").Enter=()=>{if(v.Id=="Space")Txt(v,"Details",NodeName(node)+"\n"+placed);if(FlowDrag.PathDragging)card.Get<Image>("Background").color=new Color(.5f,.4f,.15f);};
                card.Get<FlowPointer>("Pointer").Exit=()=>card.Get<Image>("Background").color=v.Id=="Space"?Color.clear:new Color(.15f,.25f,.18f);
                if(v.Id=="Space")Show(card,"Title",false);
                int order=0;
                foreach(int slot in Enumerable.Range(0,labels.Length).Where(i=>draft.Values[i]==node.Id))
                {
                    var badge=Card(card.transform,((char)('A'+slot)).ToString(),"",()=>{_slot=slot;BindDoors(v,draft);},true);badge.name="Label"+slot;
                    var badgeRect=(RectTransform)badge.transform;badgeRect.anchorMin=badgeRect.anchorMax=new Vector2(0,0);badgeRect.pivot=new Vector2(0,1);badgeRect.anchoredPosition=new Vector2(order++*44,0);badgeRect.sizeDelta=new Vector2(40,38);
                    if(v.Id=="Space"){var mark=badge.Get<Image>("Background");mark.sprite=PathMarkerIcon;mark.color=Color.white;mark.preserveAspect=true;badge.Get<Text>("Title").color=new Color(.1f,.2f,.1f);}
                    var title=badge.Get<Text>("Title");title.rectTransform.anchoredPosition=new Vector2(4,-2);title.rectTransform.sizeDelta=new Vector2(-8,30);
                    if(v.Id=="Space")
                    {
                        // 等比图片按 pivot 分配留白；改为中心轴心，同时补偿位置以保留原矩形。
                        var markerCenter=new Vector2(.5f,.5f);
                        badgeRect.anchoredPosition+=Vector2.Scale(markerCenter-badgeRect.pivot,badgeRect.rect.size);
                        badgeRect.pivot=markerCenter;
                        // 标记文字与菱形共用中心，并按字形而非字体行框居中。
                        var titleRect=title.rectTransform;
                        titleRect.anchorMin=Vector2.zero;titleRect.anchorMax=Vector2.one;
                        titleRect.pivot=new Vector2(.5f,.5f);
                        titleRect.offsetMin=Vector2.zero;titleRect.offsetMax=Vector2.zero;
                        title.alignment=TextAnchor.MiddleCenter;
                        title.alignByGeometry=true;
                    }
                    var drag=badge.Get<FlowDrag>("Drag");drag.Kind="path";drag.Id=slot.ToString();drag.CanDrag=true;drag.Dropped=card.Get<FlowDrag>("Drag").Dropped;
                    if(v.Id=="Space")drag.DragRoot=(RectTransform)v.transform;
                }
            }
            for(int i=1;i<draft.Values.Count;i++)
            {
                var a=map.Nodes.FirstOrDefault(x=>x.Id==draft.Values[i-1]);var b=map.Nodes.FirstOrDefault(x=>x.Id==draft.Values[i]);
                if(a==null||b==null||a.Floor!=_floor||b.Floor!=_floor)continue;
                var line=Instantiate(PathLinePrefab,root);line.transform.SetAsFirstSibling();var r=line.rectTransform;r.anchorMin=r.anchorMax=new Vector2(0,1);
                var ap=NodePoint(a);var bp=NodePoint(b);
                Vector2 from=new Vector2(ap.x*root.rect.width,-ap.y*root.rect.height),to=new Vector2(bp.x*root.rect.width,-bp.y*root.rect.height);
                r.pivot=new Vector2(0,.5f);r.anchoredPosition=from;r.sizeDelta=new Vector2(Vector2.Distance(from,to),3);r.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(to.y-from.y,to.x-from.x)*Mathf.Rad2Deg);
            }
            var state=S.Views.FirstOrDefault(x=>x.Puzzle==_puzzle&&x.Floor==_floor);
            if(state==null){state=new V24MapView{Puzzle=_puzzle,Floor=_floor};S.Views.Add(state);}
            var pan=v.Get<V24MapPan>("MapPan");pan.Changed=null;pan.Zoom=state.Zoom;pan.Content.anchoredPosition=new Vector2(state.X,state.Y);pan.Apply();
            pan.Changed=(zoom,pos)=>{state.Zoom=zoom;state.X=pos.x;state.Y=pos.y;Save();};
            Btn(v,"ZoomIn",()=>pan.ZoomAtCenter(.25f));Btn(v,"ZoomOut",()=>pan.ZoomAtCenter(-.25f));
            var note=v.Get<InputField>("Hypothesis");note.onValueChanged.RemoveAllListeners();note.SetTextWithoutNotify(draft.Hypothesis);note.onValueChanged.AddListener(text=>{draft.Hypothesis=text;Engine.Commit();});
            Btn(v,"ResetView",()=>{pan.Zoom=1;pan.Content.anchoredPosition=Vector2.zero;pan.Apply();},"复位视图");
            Txt(v,"Details",puzzle.Name+"\n标签按顺序连线；跨层路径见左侧楼层。\n滚轮缩放，右键平移。\n当前："+_floor+"F");
            bool hasRequirements = Engine.Need(puzzle.Need);
            bool canSubmit = draft.Values.All(x => x != 0) && hasRequirements && (v.Id != "Path" || !Engine.Busy);
            Show(v,"Verify",v.Id != "Path" || hasRequirements);
            Btn(v,"Verify",()=>
            {
                // 人物路径与时间线入口一样，点击时再次校验，避免旧按钮绕过前置。
                if (v.Id == "Path" && (draft.Values.Any(x => x == 0) || !Engine.Need(puzzle.Need) || Engine.Busy)) return;
                SubmitPuzzle(v.Id=="Path"?"Path":"Space");
            },v.Id=="Space"?"验证对齐":"提交路径",canSubmit);
            if(v.Id=="Space")
            {
                Txt(v,"Details","声源\n"+string.Join(" → ", draft.Values.Select(x=>map.Nodes.FirstOrDefault(n=>n.Id==x)).Select(x=>x==null?"待放置":Engine.RoomName(x.Room))));
                foreach(var floorId in new[]{"Floor1","Floor2"})v.Get<Button>(floorId).GetComponentInChildren<Text>(true).color=(_floor==1)==(floorId=="Floor1")?Color.white:new Color(.65f,.7f,.65f);
                var underline=v.transform.Find("FloorUnderline");if(underline)((RectTransform)underline).anchoredPosition=new Vector2(_floor==1?1042:1130,-300);
            }
            if(v.Id=="Path")Txt(v,"Details",puzzle.Name+"\n"+PuzzleRequirements(puzzle));
            Show(v,"TooltipPanel",false);
            BindDoors(v,draft);
        }
        private void Timeline()
        {
            var v=V("Timeline");var puzzle=_data.Puzzle.First(x=>x.Type=="timeline");_puzzle=puzzle.Id;var draft=Engine.Draft(puzzle.Id);int count=puzzle.Answer.Split('|').Length;
            if(v.TryGet<Text>("Requirements",out var requirementLabel))requirementLabel.gameObject.SetActive(!S.Ending);
            Show(v,"SortRoot",!S.Ending);Show(v,"RevealRoot",S.Ending);
            if(S.Ending)
            {
                if (_data.Formal) { TimelinePlayback(); return; }
                Txt(v,"EndingStatus",S.Finished?"未完待续":"真相正在显现");Txt(v,"EndingText",S.Finished?"本次调查告一段落。":"事件时间线已还原……");Show(v,"Progress",false);Show(v,"Play",false);Show(v,"Replay",false);return;
            }
            while(draft.Values.Count<count)draft.Values.Add(0);
            Clear(v.Get<RectTransform>("Clues"));Clear(v.Get<RectTransform>("Slots"));
            foreach(int id in puzzle.Candidates.Where(Engine.Has).Select(Engine.BestEntry))
            {
                var entry=_data.EntryById(id);var card=Card(v.Get<RectTransform>("Clues"),entry.Name,entry.Text,()=>_selectedEntry=id);
                var drag=card.Get<FlowDrag>("Drag");drag.CanDrag=true;drag.Kind="event";drag.Id=id.ToString();
            }
            for(int i=0;i<count;i++)
            {
                int slot=i;var entry=_data.EntryById(draft.Values[i]);var card=Card(v.Get<RectTransform>("Slots"),(i+1).ToString(),entry?.Name??"拖入事件",()=>PlaceTimeline(slot,_selectedEntry));
                card.Get<FlowDrag>("Drag").Dropped=drag=>{if(drag.Kind=="event")PlaceTimeline(slot,int.Parse(drag.Id));};
                card.Get<FlowDrag>("Drag").CanDrag=entry!=null;card.Get<FlowDrag>("Drag").Kind="event";card.Get<FlowDrag>("Drag").Id=draft.Values[i].ToString();
            }
            if(v.TryGet<Text>("Requirements",out var requirements))requirements.text=PuzzleRequirements(puzzle);
            Btn(v,"Submit",()=>SubmitPuzzle("Timeline"),"提交时间线",draft.Values.All(x=>x!=0)&&Engine.Need(puzzle.Need));
        }
        private void PlaceTimeline(int slot,int id)
        {
            if(id==0)return;var draft=Engine.Draft(_puzzle);int old=draft.Values.IndexOf(id);if(old>=0)draft.Values[old]=draft.Values[slot];draft.Values[slot]=id;Engine.Commit();Timeline();
        }
    }
}

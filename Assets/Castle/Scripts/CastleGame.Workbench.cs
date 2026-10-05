using UnityEngine;
using UnityEngine.UI;

namespace Castle
{
    public sealed partial class CastleGame
    {
        string PlaybackText() { return CastleRules.Clock((int)playPosition)+" / "+(file==0?"08:00":"12:00")+" · 文字回放"; }
        void Browse()
        {
            transcriptButtons.Clear();
            Header("录音转写 · 原始来源保留",file==2?"随身记录 / 女爵 · 会客厅":file==0?"REC-01 / 餐厅的碰撞声":"REC-02 / 隔门传来的声音");
            var left=Card(30,205,292,581);
            Label(left,"历史设备档案",20,22,252,40,25,Gold);
            Button(left,"REC-01 · 餐厅\n21:25 开始 / 08:00"+(state.anchorA?"\n◆ 已标记":""),20,89,252,113,()=>ChooseFile(0),file==0,size:20);
            Button(left,"REC-02 · 书房\n21:22 开始 / 12:00"+(state.anchorB?"\n◆ 已标记":""),20,221,252,113,()=>ChooseFile(1),file==1,size:20);
            Button(left,"REC-03\n位置待确认",20,352,252,84,()=>Toast("此设备尚无可定位的片段，先分析 REC-01 与 REC-02。"),size:20);
            if(state.recorded) Button(left,"随身记录 · 女爵",20,454,252,70,()=>ChooseFile(2),file==2,size:20);
            var center=Card(342,205,828,581,true);
            var right=Card(1190,205,380,581);
            if(file==2)
            {
                Label(center,string.Join("\n\n",database.greeting),27,25,774,530,23,Ink);
                Label(right,"记录来源",24,24,332,44,26,Gold);
                Label(right,"当晚 18:50 · 会客厅\n\n入堡时与女爵的交谈。\n\n已知人物和地点来自当时的会面；证词可与设备录音交叉核对。",24,100,332,325,23);
                Hint("随身录音为对白转写，原始来源始终保留。"); return;
            }
            Label(center,"设备报告的时间 · "+(file==0?"21:25:00":"21:22:00")+" 开始",25,20,778,39,19,new Color32(104,116,91,255));
            var lines=new[]{
                "04:51  说话者 A\n"+(file==0?"柜子旁边的东西，好像要掉下来了。":"门是关着的。我看不见餐厅里发生了什么。"),
                "05:00  声音事件\n"+(file==0?"［三声清晰的金属碰撞，紧接着是玻璃碎裂］":"［隔门传来三声闷响，随后有轻微的玻璃碎裂声］"),
                "05:12  "+(state.identity?"林女士":"说话者 A")+"\n西翼那株白花，是我亲手栽下的。",
                "05:24  说话者 B\n我会去休息室等你。先别动那些碎片。"
            };
            for(int i=0;i<4;i++)
            {
                int line=i;
                var b=Button(center,lines[i],22,78+i*120,784,108,()=>{selectedLine=line;playing=false;playPosition=new[]{291,300,312,324}[line];Render();},selectedLine==i,size:state.readingSize);
                b.GetComponent<Image>().color=selectedLine==i?new Color32(188,202,168,255):new Color32(219,213,190,255);
                var t=b.GetComponentInChildren<Text>();t.color=Ink;t.alignment=TextAnchor.MiddleLeft;
                transcriptButtons.Add(b);
            }
            Label(right,"片段操作",24,24,332,44,26,Gold);
            if(selectedLine==1)
            {
                Label(right,"◆ "+(file==0?"清晰金属碰撞":"隔门闷响与碎裂")+"\n\n设备事件时间\n"+(file==0?"21:30:00":"21:27:00")+"\n\n文件内 05:00\n候选关联仍需验证。",24,96,332,267,23);
                Button(right,(file==0?state.anchorA:state.anchorB)?"锚点已保存":"设为声音锚点",24,378,332,57,()=>{
                    if(file==0)state.anchorA=state.pairA=true;else state.anchorB=state.pairB=true;Save();Render();Toast("锚点保留了原文、设备与时间，可前往对齐。");
                },true);
                Button(right,"前往锚点对齐",24,454,332,57,()=>Go(ScreenId.Align));
            }
            else if(selectedLine>=0)
            {
                Label(right,"人物证词\n\n可摘录原文；匿名说话者需要与人物档案建立证据关联。",24,97,332,194,23);
                Button(right,"摘录到调查册",24,322,332,57,()=>{
                    var quote="REC-0"+(file+1)+" / "+lines[selectedLine];if(!state.excerpts.Contains(quote))state.excerpts.Add(quote);Save();Toast("原句与设备来源已记入调查册。");
                },true);
                Button(right,"确认说话者 A 身份",24,401,332,65,()=>Go(ScreenId.Identity),enabled:state.confirmedEvent!=null,size:21);
            }
            else Label(right,"点击一段声音描述，查看来源并设为锚点。\n\n先在两个设备中分别标记 05:00 的声音。",24,110,332,330,24);
            Button(root,playing?"暂停":"播放文字",32,831,155,49,()=>{playing=!playing;Render();},size:20);
            progressSlider=Slider(root,217,837,470,0,file==0?480:720,playPosition,v=>{
                playPosition=v;selectedLine=v<300?0:v<312?1:v<324?2:3;
                if(playLabel)playLabel.text=PlaybackText();
                for(int i=0;i<transcriptButtons.Count;i++)
                    transcriptButtons[i].GetComponent<Image>().color=i==selectedLine?new Color32(188,202,168,255):new Color32(219,213,190,255);
            });
            playLabel=Label(root,PlaybackText(),719,841,500,35,20,Muted);
            Button(root,"锚点对齐 →",1320,829,246,53,()=>Go(ScreenId.Align),true);
        }
        void ChooseFile(int index) { file=index;selectedLine=-1;playPosition=291;Go(ScreenId.Browse); }
        void Align()
        {
            Header("跨设备分析 · 候选组", "三声碰撞是否属于同一事件？");
            var left=Card(30,205,292,581);
            Label(left,"全局声音锚点",20,22,252,44,25,Gold);
            if(state.anchorA) Button(left,"REC-01 · 21:30\n清晰金属碰撞\n"+(state.pairA?"✓ 已加入候选组":"加入候选组"),20,94,252,126,()=>{state.pairA=!state.pairA;state.aligned=state.pathSaved=false;Save();Render();},state.pairA,size:20);
            if(state.anchorB) Button(left,"REC-02 · 21:27\n隔门闷响\n"+(state.pairB?"✓ 已加入候选组":"加入候选组"),20,243,252,126,()=>{state.pairB=!state.pairB;state.aligned=state.pathSaved=false;Save();Render();},state.pairB,size:20);
            Label(left,"候选组仅代表假设。\n两个锚点均需来自已标记的原始录音。",20,411,252,95,20,Muted);
            Button(left,"返回录音标记",20,511,252,50,()=>Go(ScreenId.Browse),size:20);
            var center=Card(342,205,828,581);
            Label(center,"前一晚 · 公共时间轴",25,20,770,35,22,Muted);
            var ticks=new[]{"21:24","21:27","21:30","21:33"};
            for(int i=0;i<4;i++)Label(center,ticks[i],45+i*246-37,81,74,35,19,Muted,TextAnchor.MiddleCenter);
            Track(center,130,"REC-01 · 餐厅 / 参考轨锁定",180,false);
            Track(center,275,"REC-02 · 书房 / 整条设备轨道",state.offset,true);
            offsetSlider=Slider(center,28,426,770,-180,360,state.offset,v=>SetOffsetLive((int)v));
            Label(center,"清晰金属碰撞与隔门闷响都发生在文件内 05:00。\n拖动金色轨道或滑块；方向键每次微调 1 秒。",28,486,770,82,22,Muted);
            var right=Card(1190,205,380,581);
            Label(right,"时间校正草稿",24,24,332,48,26,Gold);
            Label(right,"REC-02 校正量",24,102,332,35,22,Muted);
            offsetLabel=Label(right,CastleRules.Offset(state.offset),24,153,332,67,43,Gold);
            Label(right,"基准证据\nREC-01 校时记录\n\n偏差在本时段内视为固定。\n时间重合仍需要空间证据支持。",24,254,332,185,22);
            Button(right,"声音传播 →",24,479,332,63,()=>Go(ScreenId.Sound),true);
            Hint(state.aligned?"✓ 时间关系初步成立。继续检查声音来源与传播条件。":"先标记两个声音锚点，加入候选组，再调整整条设备轨道。");
            Button(root,"检查对齐",1320,829,246,53,()=>Check(CastleRules.Align(state),()=>{Render();Toast("时间关系初步成立，继续检查声音传播。");}),true);
        }
        void Track(Transform parent,float y,string title,int offset,bool draggable)
        {
            var p=Box(parent,title,25,y,778,123,new Color32(33,49,38,255));
            Label(p,title,18,12,740,32,21,Light);
            Box(p,"Timeline",20,75,738,3,Muted);
            var mark=Box(p,"Anchor",20+(offset+180)/540f*738-7,62,14,29,Gold);
            if(draggable)
            {
                movingAnchor=mark;
                var drag=p.gameObject.AddComponent<CastleTimelineDrag>();drag.width=738;
                drag.onDrag=delta=>SetOffsetLive(Mathf.RoundToInt(state.offset+delta));
            }
        }
        void SetOffsetLive(int value)
        {
            CastleRules.ChangeTime(state,value);Changed();
            if(offsetLabel)offsetLabel.text=CastleRules.Offset(state.offset);
            if(offsetSlider)offsetSlider.SetValueWithoutNotify(state.offset);
            if(movingAnchor)movingAnchor.anchoredPosition=new Vector2(20+(state.offset+180)/540f*738-7,-62);
        }
        void Sound()
        {
            Header("空间推理 · 历史声音传播", "三声碰撞的来源与传播条件");
            var left=Card(30,205,292,581);
            Label(left,"S · 假设声源",20,23,252,44,26,Gold);
            Label(left,string.IsNullOrEmpty(state.source)?"点击地图放置声源":state.source,20,85,252,48,26);
            Label(left,"REC-01 · 餐厅\n清晰的金属碰撞\n\nREC-02 · 书房\n隔门闷响与轻微碎裂\n\n地图表示录音发生时的空间关系。",20,171,252,291,22,Muted);
            Button(left,"撤销一步",20,503,252,54,UndoDraft,size:20);
            AnalysisMap(false);
            var right=Card(1190,205,380,581);
            Label(right,"书房门 · 历史状态",24,24,332,45,25,Gold);
            var doors=new[]{DoorState.Unknown,DoorState.Open,DoorState.Closed};var labels=new[]{"未知","开启","关闭"};
            for(int i=0;i<3;i++){var door=doors[i];Button(right,labels[i],24+i*113,95,104,54,()=>{Remember();state.door=door;state.pathSaved=false;Save();Render();},state.door==door,size:20);}
            Button(right,(state.doorEvidence?"✓ 已关联证据":"关联门状态证据")+"\n“门是关着的，我看不见餐厅。”\nREC-02 / 04:51",24,187,332,148,()=>{Remember();state.doorEvidence=!state.doorEvidence;state.pathSaved=false;Save();Render();},state.doorEvidence,size:20);
            Label(right,"关闭的门使传播声音减弱。\n核对录音描述，而不是当前场景里的门。",24,377,332,102,22,Muted);
            Button(right,"保存传播草稿",24,500,332,57,()=>{
                if(string.IsNullOrEmpty(state.source)){Toast("先在地图选择声源房间。");return;}
                state.pathSaved=true;Save();Render();Toast("声音传播草稿已保存，可进行联合验证。");
            },true);
            Hint(state.pathSaved?"传播草稿已保存。联合验证会同时检查时间、声源、门状态与依据。":"点击房间放置 S；细碎线段表示穿过关闭的门后声音减弱。");
            Button(root,"联合验证",1320,829,246,53,()=>Check(CastleRules.VerifyEvent(state),()=>Go(ScreenId.Event)),true);
        }
        void Remember() { undo.Push(JsonUtility.ToJson(new Draft {source=state.source,door=state.door,doorEvidence=state.doorEvidence,route=(string[])state.route.Clone()})); }
        [System.Serializable] sealed class Draft { public string source; public DoorState door; public bool doorEvidence; public string[] route; }
        void UndoDraft()
        {
            if(undo.Count==0){Toast("没有可撤销的操作。");return;}
            var d=JsonUtility.FromJson<Draft>(undo.Pop());state.source=d.source;state.door=d.door;state.doorEvidence=d.doorEvidence;state.route=d.route;state.pathSaved=false;Save();Render();
        }
        void AnalysisMap(bool route)
        {
            var p=Card(342,205,828,581,true);
            Label(p,"古堡平面图 · "+floor+"F",22,18,505,43,25,Ink);
            Button(p,"1F",632,13,80,43,()=>{floor=1;Render();},floor==1,size:19);
            Button(p,"2F",730,13,76,43,()=>{floor=2;Render();},floor==2,size:19);
            Picture(p,floor==1?"Floor1":"Floor2",0,70,828,466);
            foreach(var room in database.rooms)
            {
                if(room.floor!=floor||room.id.StartsWith("stairs")||room.id=="east")continue;
                var r=room;
                float sx=828/1672f,sy=466/941f;
                var b=Button(p,r.title,(r.x-r.width/2)*sx,70+(r.y-r.height/2)*sy,r.width*sx,r.height*sy,()=>{
                    if(floor!=1){Toast("本案的已知位置证据在一楼。");return;}
                    Remember();if(route){state.route[selectedNode]=r.title;}else{state.source=r.title;state.pathSaved=false;}Save();Render();
                },!route&&state.source==r.title,size:17);
                b.GetComponent<Image>().color=!route&&state.source==r.title?Gold:new Color(.12f,.25f,.16f,.64f);
            }
            if(floor==1)
            {
                if(route)
                {
                    for(int i=0;i<3;i++)
                    {
                        if(string.IsNullOrEmpty(state.route[i]))continue;
                        var r=System.Array.Find(database.rooms,x=>x.floor==1&&x.title==state.route[i]);
                        if(r!=null)Label(p,(i+1).ToString(),r.x*828/1672f-14,70+r.y*466/941f+10,30,28,22,Gold);
                        if(i>0&&!string.IsNullOrEmpty(state.route[i-1]))DrawConnection(p,state.route[i-1],state.route[i],false);
                    }
                }
                else if(!string.IsNullOrEmpty(state.source))
                {
                    DrawConnection(p,state.source,"书房",state.door==DoorState.Closed);
                    DrawConnection(p,state.source,"餐厅",false);
                    var source=System.Array.Find(database.rooms,r=>r.floor==1&&r.title==state.source);
                    if(source!=null)Label(p,"S",source.x*828/1672f-12,70+source.y*466/941f+10,28,28,23,Ink);
                }
            }
            Label(p,route?"编号为先后顺序 · 路线沿通道连接":"实线：清晰   断续线：减弱   S：假设声源",22,540,783,32,18,Ink);
        }
        // Waypoints explicitly follow the corridors in the prototype, never a straight line through rooms.
        Vector2[] PathFor(string name)
        {
            var r=System.Array.Find(database.rooms,x=>x.floor==1&&x.title==name);
            if(r==null)return new Vector2[0];
            var origin=new Vector2(r.x,r.y);
            if(name=="书房")return new[]{origin,new Vector2(554,281),new Vector2(554,532),new Vector2(836,532)};
            if(name=="餐厅")return new[]{origin,new Vector2(833,501),new Vector2(836,532)};
            if(name=="休息室")return new[]{origin,new Vector2(1137,575),new Vector2(1110,532),new Vector2(836,532)};
            return new[]{origin,new Vector2(r.x,532),new Vector2(836,532)};
        }
        void DrawConnection(Transform parent,string from,string to,bool dashed)
        {
            var points=new System.Collections.Generic.List<Vector2>(PathFor(from));
            var end=PathFor(to);System.Array.Reverse(end);points.AddRange(end);
            for(int i=1;i<points.Count;i++)
            {
                var a=new Vector2(points[i-1].x*828/1672f,70+points[i-1].y*466/941f);
                var b=new Vector2(points[i].x*828/1672f,70+points[i].y*466/941f);
                if(dashed){for(float t=0;t<1;t+=.17f)Line(parent,Vector2.Lerp(a,b,t),Vector2.Lerp(a,b,Mathf.Min(1,t+.09f)),Gold);}
                else Line(parent,a,b,new Color32(169,198,151,255));
            }
        }
        void Line(Transform parent,Vector2 a,Vector2 b,Color color)
        {
            var r=Box(parent,"Evidence path",a.x,a.y,Vector2.Distance(a,b),4,color);
            r.localRotation=Quaternion.Euler(0,0,-Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg);
            r.GetComponent<Image>().raycastTarget=false;
        }
    }
}

using System;
using UnityEngine;

namespace Castle
{
    public sealed partial class CastleGame
    {
        void Title()
        {
            Picture(root,"Title",0,0,1600,900);
            Box(root,"Menu backing",642,302,316,420,Ink);
            Button(root,"开始游戏",661,317,278,61,()=>{
                Action start=()=>{state=new CastleState {started=true}; undo.Clear(); file=0; selectedLine=-1; Go(ScreenId.Outside);};
                if (state.started) Message("开始新的调查", "当前进度将被新游戏替换。", start); else start();
            },true);
            Button(root,"继续游戏",661,392,278,61,()=>Go(state.resume),enabled:state.started);
            Button(root,"设置",661,467,278,61,Settings);
            Button(root,"制作名单",661,542,278,61,()=>Message("古堡 · 声音与时间的谜题","Unity 交互框架\n场景美术与字体来自提供的 HTML 原型。\n正式制作名单待团队补充。",null));
            Button(root,"退出游戏",661,617,278,61,()=>{Save();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying=false;
#else
                Application.Quit();
#endif
            });
            Label(root,"古堡 · 声音与时间的谜题",32,851,900,32,18,Gold);
        }
        void Hud()
        {
            var room=database.Room(state.room);
            Box(root,"Location",28,26,320,94,new Color(.07f,.12f,.09f,.94f));
            Label(root,room == null ? "古堡外" : room.floor + "F · " + room.title,48,38,290,40,25,Gold);
            Label(root,"当晚  " + CastleRules.Clock(state.minutes),48,81,290,32,20,Muted);
            Button(root,"地图 M",1110,28,145,55,Navigation,enabled:state.mapUnlocked);
            Button(root,"调查册 J",1272,28,165,55,Journal);
            Button(root,"菜单",1454,28,118,55,Settings);
        }
        void Outside()
        {
            Picture(root,state.invitation ? "Outside" : "Held",0,0,1600,900); Hud();
            if (!state.invitation)
            {
                Button(root,"展开手中的邀请函",560,744,480,68,()=>Go(ScreenId.Invitation),true);
            }
            else
            {
                Button(root,"进入古堡",661,720,278,65,()=>{state.room="101";state.minutes=1130;Go(ScreenId.Lounge);},true);
                Label(root,"邀请函已收纳 · 可从调查册再次查看",40,824,800,45,22,Gold);
            }
        }
        void Invitation()
        {
            Picture(root,"Invitation",0,0,1600,900);
            Button(root,"收起并保存邀请函",1130,800,420,60,()=>{state.invitation=true;Go(ScreenId.Collected);},true);
        }
        void Collected()
        {
            Picture(root,"Collected",0,0,1600,900);
            Button(root,"确认收纳",943,623,310,78,()=>Go(ScreenId.Outside),true);
        }
        void AdvanceDialogue() { CastleRules.AdvanceDialogue(state); Save(); Render(); }
        void Lounge()
        {
            Picture(root,"Lounge",0,0,1600,900); Hud();
            var p=Card(962,133,595,708);
            Label(p,"会客厅 · 女爵",28,25,535,42,28,Gold);
            int start=Math.Max(0,state.dialogue-2);
            for(int i=start;i<=state.dialogue;i++) Label(p,database.greeting[i],28,100+(i-start)*132,535,122,24,i%2==0?Light:new Color32(162,197,220,255));
            Label(p,state.recorded?"对话已录音 · 已收入随身档案":state.controlUnlocked?"中控室已解锁 · 19:00开放":state.mapUnlocked?"古堡地图已收录":"空格或点击按钮继续阅读",28,534,535,57,20,Muted);
            Button(p,state.recorded?"打开地图":"继续 →",270,615,295,58,state.recorded?(Action)Navigation:AdvanceDialogue,true);
            if(state.mapUnlocked) Button(p,"查看地图",28,615,216,58,Navigation);
        }
        void Room()
        {
            Picture(root,state.room=="110"?"ControlRoom":"EmptyRoom",0,0,1600,900); Hud();
            if(state.room=="110")
            {
                Button(root,"查看录音工作台",565,477,400,78,()=>{
                    state.workbench=true;state.Discover("103","104","108");Go(ScreenId.Browse);
                },true);
                Box(root,"Scene caption",34,708,760,130,new Color(.06f,.11f,.08f,.9f));
                Label(root,"这里保存着古堡各处的录音记录。\n从片段、时间与空间中，还原前一晚的经过。",58,736,710,86,25);
            }
            else
            {
                var r=database.Room(state.room); var p=Card(966,263,570,460);
                Label(p,r==null?"古堡":r.title,32,30,506,55,36,Gold);
                string text=state.room=="108"?"林女士：西翼那株白花，是我亲手栽下的。\n\n前一晚，我先去书房，之后经过餐厅，再回到休息室。":state.room=="104"?"柜门旁留下碰撞的痕迹。金属搭扣与器物受力方向，可以作为还原事件的物证。":"房间暂时无人。远处传来器物轻轻碰撞的声音。可以打开地图，继续探索古堡。";
                Label(p,text,32,112,506,227,24);
                Button(p,"打开地图",32,366,506,57,Navigation,true);
            }
        }
        void Navigation()
        {
            if(!IsScene() || !state.mapUnlocked) {Toast("在场景中获得地图后，可以进行房间导航。");return;}
            selectedRoom=null; DrawNavigation();
        }
        void DrawNavigation()
        {
            var p=Overlay("Navigation");
            Picture(p,floor==1?"Floor1":"Floor2",0,0,1600,900);
            Box(p,"Map heading",20,18,510,85,Panel);
            Label(p,"古堡地图 · "+floor+"F",40,28,480,38,28,Gold);
            Label(p,"当前位置："+(database.Room(state.room)?.title??"古堡外")+"  /  "+CastleRules.Clock(state.minutes),40,68,480,29,18,Muted);
            Button(p,"一楼",1110,28,130,52,()=>{floor=1;selectedRoom=null;DrawNavigation();},floor==1);
            Button(p,"二楼",1258,28,130,52,()=>{floor=2;selectedRoom=null;DrawNavigation();},floor==2);
            Button(p,"关闭",1406,28,160,52,CloseOverlay);
            foreach(var room in database.rooms)
            {
                if(room.floor!=floor) continue;
                var r=room; bool known=state.discovered.Contains(r.id);
                float x=(r.x-r.width/2)*1600/1672f,y=(r.y-r.height/2)*900/941f,w=r.width*1600/1672f,h=r.height*900/941f;
                var b=Button(p,known?r.title:"未探索",x,y,w,h,()=>{
                    if(r.id.StartsWith("stairs")){floor=floor==1?2:1;DrawNavigation();return;}
                    selectedRoom=r.id; DrawNavigation();
                },state.room==r.id,size:r.id=="east"?17:20);
                b.GetComponent<UnityEngine.UI.Image>().color=state.room==r.id?Gold:known?new Color(.12f,.22f,.14f,.4f):new Color(.76f,.71f,.57f,.96f);
                if(!known)b.GetComponentInChildren<UnityEngine.UI.Text>().color=Ink;
            }
            Box(p,"Map help",28,779,1544,94,Panel);
            var selected=database.Room(selectedRoom);
            if(selected==null) Label(p,"点击房间查看开放条件，再选择进入。阅读不耗时，移动耗时 2 分钟。",52,806,1480,48,22,Light);
            else
            {
                var error=CastleRules.CanVisit(state,selected);
                Label(p,(state.discovered.Contains(selected.id)?selected.title:"未探索区域")+" · "+(error??"现在可以进入"),50,792,1000,72,22);
                if(error==null) Button(p,"进入房间",1170,799,360,55,()=>{
                    Check(CastleRules.Travel(state,selected),()=>Go(selected.id=="101"?ScreenId.Lounge:ScreenId.Room));
                },true);
                else if(state.discovered.Contains(selected.id)&&state.minutes<selected.opens&&(selected.id!="110"||state.controlUnlocked))
                    Button(p,"等待至 "+CastleRules.Clock(selected.opens),1170,799,360,55,()=>{state.minutes=selected.opens;Save();DrawNavigation();},true);
            }
        }
        void Settings()
        {
            var p=Overlay("Settings"); Box(p,"Settings panel",380,115,840,690,Panel);
            Label(p,"设置与操作",425,155,750,60,36,Gold);
            Label(p,"文字字号",425,251,720,35,22);
            var sizeLabel=Label(p,state.readingSize.ToString(),1090,251,80,35,22,Gold);
            Slider(p,425,300,740,20,28,state.readingSize,v=>{state.readingSize=(int)v;sizeLabel.text=v.ToString();Changed();});
            Label(p,"界面音量",425,369,700,35,22);
            Slider(p,425,416,740,0,100,state.volume*100,v=>{state.volume=v/100;Changed();});
            Label(p,"J 调查册    M 导航地图    Esc 关闭 / 设置\n空格继续对白    ← → 微调校正量\n阅读与推理暂停游戏时间；切换页面自动存档。",425,485,740,121,21,Muted);
            Button(p,state.reduceMotion?"动态效果：关闭":"动态效果：开启",425,623,350,56,()=>{state.reduceMotion=!state.reduceMotion;Save();Settings();});
            Button(p,"返回标题",802,623,365,56,()=>Go(ScreenId.Title));
            Button(p,"继续",425,715,742,56,()=>{Save();CloseOverlay();Render();},true);
        }
        void Journal()
        {
            var p=Overlay("Journal"); Box(p,"Book",60,52,1480,786,Panel);
            Label(p,"调查册 / "+journalTab,94,78,900,55,32,Gold);
            Button(p,"关闭",1350,75,150,50,CloseOverlay);
            var tabs=new[]{"事件","人物","道具","对话"};
            for(int i=0;i<tabs.Length;i++){var tab=tabs[i];Button(p,tab,98,168+i*79,230,61,()=>{journalTab=tab;Journal();},journalTab==tab);}
            Box(p,"Page",359,161,1138,620,Paper);
            string text="";
            if(journalTab=="事件")
            {
                text=state.controlUnlocked?"中控室开放\n女爵告知：东廊北侧，19:00 以后开放。\n\n":"尚未确认事件。\n";
                if(state.confirmedEvent!=null) text+="已确认 · 餐厅的碰撞与碎裂\nREC-01 21:30 / REC-02 21:27，校正 "+CastleRules.Offset(state.confirmedEvent.offset)+"。\n声源："+state.confirmedEvent.source+"；书房门关闭，关联了录音原句。\n\n";
                if(state.confirmedRoute!=null) text+="已确认 · 林女士的路线\n21:20 "+state.confirmedRoute[0]+" → 21:30 "+state.confirmedRoute[1]+" → 21:40 "+state.confirmedRoute[2]+"\n\n";
                if(state.finalOK) text+="事件还原已完成 · 金属搭扣支持开柜与器物坠落的关系。";
            }
            if(journalTab=="人物") text="拉帕莉亚 · 调查者\n\n"+(state.dialogue>0?"女爵 · 古堡主人\n会客厅的交谈解锁了古堡地图与中控室。\n\n":"")+(state.workbench?"林女士 · "+(state.identity?"录音身份已确认":"录音身份待确认")+"\n证词：西翼的白花由她亲手栽下。\n前一晚路线自述：书房 → 餐厅 → 休息室。\n\n陈先生 · 候选人物\n尚无证据将他关联到说话者 A。":"");
            if(journalTab=="道具") text=(state.invitation?"邀请函 · 古堡外获得\n以玫瑰蜡封缄封，邀请拉帕莉亚于晚七时赴宴。\n\n":"尚无道具。\n")+(state.mapUnlocked?"古堡地图 · 女爵赠予\n双层平面图，房间按线索与时间开放。\n\n":"")+(state.workbench?"柜门金属搭扣 · 案件档案附带物证\n现场痕迹记录支持开柜动作与器物受力的关系。\n\n位置资料\n21:20 书房记录 / 21:30 声音事件 / 21:40 休息室访客簿。":"");
            if(journalTab=="对话") text=state.recorded?string.Join("\n\n",database.greeting):"交谈结束后，完整记录会保存在这里。";
            Label(p,text,394,191,1058,515,journalTab=="对话"?21:24,Ink);
            if(journalTab=="道具"&&state.invitation) Button(p,"展开邀请函",1090,703,350,53,()=>{
                var invite=Overlay("Archived invitation");Picture(invite,"Invitation",0,0,1600,900);Button(invite,"返回调查册",1180,802,370,56,Journal,true);
            });
            if(journalTab=="事件"&&state.confirmedEvent!=null) Button(p,"查看已确认事件",1060,703,380,53,()=>Go(ScreenId.Event),true);
            if(journalTab=="人物"&&state.workbench) Button(p,state.identity?"重建人物路线":"确认录音身份",1060,703,380,53,()=>Go(state.identity?ScreenId.Route:ScreenId.Identity),true);
            if(journalTab=="对话"&&state.excerpts.Count>0) Button(p,"查看摘录 ("+state.excerpts.Count+")",1080,703,360,53,()=>Message("录音摘录",string.Join("\n\n",state.excerpts),null));
        }
    }
}

using UnityEngine;

namespace Castle
{
    public sealed partial class CastleGame
    {
        void EventView()
        {
            Header("联合验证 · 已确认记录", "餐厅的碰撞与玻璃碎裂");
            var p=Card(32,207,1058,580,true);
            var record=state.confirmedEvent;
            Label(p,"声音事件 / 前一晚 21:30",35,32,980,61,33,Ink);
            Label(p,record==null?"尚未确认事件。":
                "REC-01 · 餐厅\n原始事件时间 21:30:00，清晰的三声金属碰撞。\n\nREC-02 · 书房\n原始事件时间 21:27:00，校正 "+CastleRules.Offset(record.offset)+" → 21:30:00。\n\n声音来源："+record.source+"\n历史条件：书房门关闭；隔门传来的声音较弱。\n依据：REC-02 / 04:51 的门状态原句。",35,126,984,398,25,Ink);
            var right=Card(1114,207,452,580);
            Label(right,"已确认关系",30,32,392,50,29,Gold);
            Label(right,"时间与传播条件互相支持。\n\n已确认记录保留原始来源；继续修改推理草稿，不会改变此记录。\n\n下一步：确认匿名说话者 A 的身份。",30,118,392,287,24);
            Button(right,"确认录音身份 →",30,474,392,65,()=>Go(ScreenId.Identity),true,record!=null);
            Hint("调查册会保留校正量、事件来源、历史门状态与关联证据。");
            Button(root,"回看原始录音",1320,829,246,53,()=>Go(ScreenId.Browse));
        }
        void Identity()
        {
            Header("人物调查 · 身份关联", "说话者 A 是谁？");
            var left=Card(32,205,427,581,true);
            Label(left,"原始声音片段",28,25,371,48,27,Ink);
            Label(left,"“西翼那株白花，是我亲手栽下的。”\n\nREC-02 · 文件02\n05:12 · 说话者 A\n\n名字被提到，或录音出现在某个房间，都不能单独证明身份。",28,122,371,375,25,Ink);
            var middle=Card(481,205,569,581);
            Label(middle,"选择候选人物",28,25,513,50,27,Gold);
            Button(middle,"林女士\n西翼花园的照料者",28,127,513,144,()=>{state.candidate="林女士";Save();Render();},state.candidate=="林女士",size:27);
            Button(middle,"陈先生\n古堡的宾客",28,312,513,144,()=>{state.candidate="陈先生";Save();Render();},state.candidate=="陈先生",size:27);
            var right=Card(1072,205,496,581);
            Label(right,"支持证据",28,25,440,50,27,Gold);
            Button(right,(state.identityEvidence?"✓ 已关联":"关联休息室交谈摘录")+"\n\n林女士曾提到：\n西翼的白花由她亲手栽下。",28,126,440,233,()=>{state.identityEvidence=!state.identityEvidence;Save();Render();},state.identityEvidence,size:24);
            Label(right,state.identity?"身份已确认，录音标签已更新为林女士。":"选择人物与独立证词，再提交身份判断。",28,414,440,108,23,Muted);
            Hint("身份关联确认后，开放人物路线核对。");
            Button(root,state.identity?"重建人物路线 →":"确认身份",1270,829,296,53,()=>{
                if(state.identity)Go(ScreenId.Route);else Check(CastleRules.VerifyIdentity(state),()=>{Render();Toast("说话者 A 已关联林女士。");});
            },true);
        }
        void Route()
        {
            Header("空间推理 · 人物路径", "林女士 / 前一晚 21:20—21:40");
            var left=Card(30,205,292,581);
            Label(left,"位置节点",20,24,252,45,26,Gold);
            var times=new[]{"21:20 · 较早位置","21:30 · 巨响发生时","21:40 · 较晚位置"};
            for(int i=0;i<3;i++){int node=i;Button(left,(i+1)+"  "+times[i]+"\n"+(string.IsNullOrEmpty(state.route[i])?"点击选择，再点房间":state.route[i]),20,100+i*123,252,105,()=>{selectedNode=node;Render();},selectedNode==i,size:20);}
            Button(left,"撤销一步",20,503,252,54,UndoDraft,size:20);
            AnalysisMap(true);
            var right=Card(1190,205,380,581);
            Label(right,"节点 "+(selectedNode+1)+" / 关联位置证据",24,24,332,71,25,Gold);
            Button(right,(state.routeEvidence?"✓ 位置证据已关联":"关联位置证据")+"\n\n21:20 书房记录\n21:30 声音事件\n21:40 休息室访客簿",24,127,332,229,()=>{state.routeEvidence=!state.routeEvidence;Save();Render();},state.routeEvidence,size:21);
            Label(right,state.confirmedRoute!=null?"已确认路线已保存在调查册。当前地图为可编辑草稿。":"路线沿通道连接。某人经过一个房间，不代表她有罪。",24,399,332,110,22,Muted);
            Hint("选择左侧编号，再在一楼地图放置地点。三个节点都需要来源与时间。");
            Button(root,"核对人物路线",1050,829,249,53,()=>Check(CastleRules.VerifyRoute(state),()=>{Render();Toast("人物路线已确认，事件还原入口已开放。");}),true);
            Button(root,"事件还原 →",1320,829,246,53,()=>Go(ScreenId.Final),enabled:state.confirmedEvent!=null&&state.confirmedRoute!=null);
        }
        void FinalView()
        {
            Header("案件还原 · 已确认关系自动带入", "接通最后一条证据关系");
            var p=Card(32,205,1058,581);
            Label(p,"前一晚 · 校正后事件时间轴",28,28,998,45,26,Gold);
            var titles=new[]{"21:20\n进入书房","21:30 之前\n柜门被打开","21:30\n碰撞与玻璃碎裂"};
            for(int i=0;i<3;i++)
            {
                var c=Box(p,"Timeline event",28+i*339,131,320,160,new Color32(42,62,46,255));
                Label(c,titles[i],23,25,274,110,27,Light,TextAnchor.MiddleCenter);
            }
            Label(p,"声音事件  →  "+(state.confirmedEvent!=null?"已确认巨响发生于餐厅":"尚未确认")+"\n\n人物路线  →  "+(state.confirmedRoute!=null?"已核实当时的位置关系":"尚未核实")+"\n\n现场物证  →  "+(state.finalEvidence?"金属搭扣支持最后一条关系":"最后关系仍需独立证据支持"),30,342,998,218,25);
            var right=Card(1114,205,452,581);
            Label(right,"最后的关系",28,28,396,43,27,Gold);
            Label(right,"开柜动作与器物坠落有关\n\n仅凭时间先后不足以证明因果，需要独立物证。",28,113,396,169,24);
            Button(right,(state.finalEvidence?"✓ 已接入关系":"选择支持证据")+"\n\n柜门金属搭扣\n现场痕迹与器物受力关系",28,318,396,196,()=>{state.finalEvidence=!state.finalEvidence;Save();Render();},state.finalEvidence,size:24);
            Hint("成功提交后进入真相重建；此前仍可回看调查册和原始资料。");
            Button(root,"提交事件还原",1270,829,296,53,()=>Check(CastleRules.Submit(state),()=>Go(ScreenId.Truth)),true);
        }
        void Truth()
        {
            Header("事件重建 · 证据关系已成立", "记录终于彼此吻合");
            var p=Card(32,205,964,581);
            for(int i=0;i<78;i++)
            {
                float h=20+Mathf.Abs(Mathf.Sin(i*.47f)*Mathf.Cos(i*.13f))*270;
                var bar=Box(p,"Wave",35+i*11.5f,273-h/2,4,h,Gold);bar.pivot=new Vector2(.5f,.5f);bar.anchoredPosition=new Vector2(35+i*11.5f,-273);waveBars.Add(bar);
            }
            Label(p,"REC-01 + REC-02 / 共同事件时间轴\n传播条件 · 人物路线 · 现场物证",40,462,884,82,23,Muted,TextAnchor.MiddleCenter);
            var right=Card(1020,205,546,581);
            Label(right,"拉帕莉亚的重建叙述  "+(state.truthIndex+1)+" / 3",30,35,486,59,25,Gold);
            Label(right,database.truth[state.truthIndex],30,160,486,290,30);
            Hint("逐段阅读，不会自动跳过叙述。可在设置中减弱动态效果。");
            Button(root,state.truthIndex<2?"继续重建":"作出决定",1270,829,296,53,()=>{if(state.truthIndex<2){state.truthIndex++;Save();Render();}else Go(ScreenId.Choice);},true);
        }
        void Choice()
        {
            Picture(root,"ControlRoom",0,0,1600,900);var p=Card(125,95,1350,710);
            Label(p,"真相已查明 · 结局前已保存",50,40,1240,43,22,Muted);
            Label(p,"如何处理这些证据？",50,114,1240,90,48,Gold);
            Label(p,"事实判断已由调查完成。现在选择的是拉帕莉亚的行动。",50,226,1240,62,27);
            Button(p,"公开完整记录\n\n让相关人物面对已核实的事实。",50,338,605,180,()=>{state.ending="公开记录";Go(ScreenId.Ending);},true,size:28);
            Button(p,"先与当事人对质\n\n携带证据，进行一次当面对质。",695,338,605,180,()=>{state.ending="当面对质";Go(ScreenId.Ending);},size:28);
            Button(p,"核对调查册",50,598,605,60,Journal);
            Button(p,"返回事件还原",695,598,605,60,()=>Go(ScreenId.Final));
        }
        void Ending()
        {
            Picture(root,"ControlRoom",0,0,1600,900);var p=Card(125,95,1350,710);
            Label(p,"调查落幕",55,45,1240,45,24,Muted);
            Label(p,state.ending,55,138,1240,110,66,Gold);
            Label(p,state.ending=="公开记录"?"拉帕莉亚将完整录音和核实过的证据公开。\n这一次，古堡里的每个人都听见了同一段真相。":"拉帕莉亚带着记录来到当事人面前。\n她决定先听取回应，再决定这些证据的去向。",55,303,1240,140,31);
            Label(p,"声音事件、设备校正、人物位置与现场证据已收入调查册。",55,484,1240,51,23,Muted);
            Button(p,"查看调查册",55,597,370,62,Journal);
            Button(p,"返回结局前",490,597,370,62,()=>Go(ScreenId.Choice),true);
            Button(p,"返回标题",925,597,370,62,()=>Go(ScreenId.Title));
        }
    }
}

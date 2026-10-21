using UnityEngine;
namespace Castle
{
    public sealed partial class CastleGame
    {
        void EventView()
        {
            Header(); var v = Current; var record = state.confirmedEvent;
            T(v, "RecordText", record == null ? "尚未确认事件。" : "REC-01 · 餐厅\n原始事件时间 21:30:00，清晰的三声金属碰撞。\n\nREC-02 · 书房\n原始事件时间 21:27:00，校正 " + CastleRules.Offset(record.offset) + " → 21:30:00。\n\n声音来源：" + record.source + "\n历史条件：书房门关闭；隔门传来的声音较弱。\n依据：REC-02 / 04:51 的门状态原句。");
            B(v, "IdentityButton", () => Go(ScreenId.Identity), interactable: record != null);
            B(v, "RecordingButton", () => Go(ScreenId.Browse));
        }
        void Identity()
        {
            Header(); var v = Current;
            B(v, "LinButton", () => { state.candidate = "林女士"; Save(); Render(); }, selected: state.candidate == "林女士");
            B(v, "ChenButton", () => { state.candidate = "陈先生"; Save(); Render(); }, selected: state.candidate == "陈先生");
            B(v, "EvidenceButton", () => { state.identityEvidence = !state.identityEvidence; Save(); Render(); }, (state.identityEvidence ? "✓ 已关联" : "关联休息室交谈摘录") + "\n\n林女士曾提到：\n西翼的白花由她亲手栽下。", state.identityEvidence);
            T(v, "StatusText", state.identity ? "身份已确认，录音标签已更新为林女士。" : "选择人物与独立证词，再提交身份判断。");
            B(v, "ConfirmButton", () => { if (state.identity) Go(ScreenId.Route); else Check(CastleRules.VerifyIdentity(state), () => { Render(); Toast("说话者 A 已关联林女士。"); }); }, state.identity ? "重建人物路线 →" : "确认身份");
        }
        void Route()
        {
            Header(); var v = Current; var times = new[] { "21:20 · 较早位置", "21:30 · 巨响发生时", "21:40 · 较晚位置" };
            for (int i = 0; i < 3; i++) { int node = i; B(v, "Node" + i, () => { selectedNode = node; Render(); }, (i + 1) + "  " + times[i] + "\n" + (string.IsNullOrEmpty(state.route[i]) ? "点击选择，再点房间" : state.route[i]), selectedNode == i); }
            B(v, "UndoButton", UndoDraft); BindMap(v, false, true);
            T(v, "NodeText", "节点 " + (selectedNode + 1) + " / 关联位置证据");
            B(v, "EvidenceButton", () => { state.routeEvidence = !state.routeEvidence; Save(); Render(); }, (state.routeEvidence ? "✓ 位置证据已关联" : "关联位置证据") + "\n\n21:20 书房记录\n21:30 声音事件\n21:40 休息室访客簿", state.routeEvidence);
            T(v, "StatusText", state.confirmedRoute != null && state.confirmedRoute.Length == 3 ? "已确认路线已保存在调查册。当前地图为可编辑草稿。" : "路线沿通道连接。某人经过一个房间，不代表她有罪。");
            B(v, "VerifyButton", () => Check(CastleRules.VerifyRoute(state), () => { Render(); Toast("人物路线已确认，事件还原入口已开放。"); }));
            B(v, "NextButton", () => Go(ScreenId.Final), interactable: state.confirmedEvent != null && state.confirmedRoute != null && state.confirmedRoute.Length == 3);
        }
        void FinalView()
        {
            Header(); var v = Current;
            T(v, "RelationsText", "声音事件  →  " + (state.confirmedEvent != null ? "已确认巨响发生于餐厅" : "尚未确认") + "\n\n人物路线  →  " + (state.confirmedRoute != null && state.confirmedRoute.Length == 3 ? "已核实当时的位置关系" : "尚未核实") + "\n\n现场物证  →  " + (state.finalEvidence ? "金属搭扣支持最后一条关系" : "最后关系仍需独立证据支持"));
            B(v, "EvidenceButton", () => { state.finalEvidence = !state.finalEvidence; Save(); Render(); }, (state.finalEvidence ? "✓ 已接入关系" : "选择支持证据") + "\n\n柜门金属搭扣\n现场痕迹与器物受力关系", state.finalEvidence);
            B(v, "SubmitButton", () => Check(CastleRules.Submit(state), () => Go(ScreenId.Truth)));
        }
        void Truth()
        {
            Header(); var v = Current; T(v, "IndexText", "拉帕莉亚的重建叙述  " + (state.truthIndex + 1) + " / 3");
            T(v, "StoryText", database.truth[state.truthIndex]);
            B(v, "NextButton", () => { if (state.truthIndex < 2) { state.truthIndex++; Save(); Render(); } else Go(ScreenId.Choice); }, state.truthIndex < 2 ? "继续重建" : "作出决定");
        }
        void Choice()
        {
            var v = Current;
            B(v, "PublicButton", () => { state.ending = "公开记录"; Go(ScreenId.Ending); });
            B(v, "ConfrontButton", () => { state.ending = "当面对质"; Go(ScreenId.Ending); });
            B(v, "JournalButton", Journal); B(v, "BackButton", () => Go(ScreenId.Final));
        }
        void Ending()
        {
            var v = Current; T(v, "EndingText", state.ending);
            T(v, "StoryText", state.ending == "公开记录" ? "拉帕莉亚将完整录音和核实过的证据公开。\n这一次，古堡里的每个人都听见了同一段真相。" : "拉帕莉亚带着记录来到当事人面前。\n她决定先听取回应，再决定这些证据的去向。");
            B(v, "JournalButton", Journal); B(v, "BackButton", () => Go(ScreenId.Choice)); B(v, "TitleButton", () => Go(ScreenId.Title));
        }
    }
}

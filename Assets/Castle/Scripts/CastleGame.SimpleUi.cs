using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Castle
{
    public sealed partial class CastleGame
    {
        float transcriptScroll = 1, journalScroll = 1;
        bool restoringScroll;
        JournalEntry currentJournalEntry;
        readonly List<CastleJournalCardView> journalCards = new List<CastleJournalCardView>();
        void BindScrollPositions()
        {
            ui.Page(ScreenId.Browse).Get<ScrollRect>("TranscriptScroll").onValueChanged.AddListener(OnTranscriptScrolled);
            ui.Overlay("Journal").Get<ScrollRect>("GridScroll").onValueChanged.AddListener(OnJournalScrolled);
        }
        void OnTranscriptScrolled(Vector2 value) { if (!restoringScroll && page == ScreenId.Browse) transcriptScroll = value.y; }
        void OnJournalScrolled(Vector2 value) { if (!restoringScroll && overlay == ui.Overlay("Journal").transform) journalScroll = value.y; }
        void RestoreScroll(ScrollRect scroll, float position)
        {
            restoringScroll = true; Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
            scroll.verticalNormalizedPosition = position; restoringScroll = false;
        }
        void FitBody(CastleUiView view, string textId, string scrollId, string body)
        {
            var text = view.Get<Text>(textId); var scroll = view.Get<ScrollRect>(scrollId);
            text.text = body; text.fontSize = state.readingSize;
            Canvas.ForceUpdateCanvases();
            float height = Mathf.Max(scroll.viewport.rect.height, text.preferredHeight + 30);
            text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            RestoreScroll(scroll, 1);
        }
        sealed class JournalEntry
        {
            public string Id, Title, Summary, Detail, ActionLabel;
            public Action Open;
            public JournalEntry(string id, string title, string summary, string detail, string actionLabel = null, Action open = null)
            { Id = id; Title = title; Summary = summary; Detail = detail; ActionLabel = actionLabel; Open = open; }
        }

        List<JournalEntry> JournalEntries()
        {
            var entries = new List<JournalEntry>();
            if (journalTab == "人物")
            {
                entries.Add(new JournalEntry("person-investigator", "拉帕莉亚", "调查者", "受邀来到古堡的调查者。通过录音、人物证词与物证，还原前一晚发生的事情。"));
                if (state.dialogue > 0) entries.Add(new JournalEntry("person-countess", "女爵", "古堡主人 · 会客厅", "会客厅的交谈提供了古堡地图与中控室的线索。\n\n" +
                    (state.recorded ? "完整对话记录\n\n" + string.Join("\n\n", database.greeting) : "完整对话将在交谈结束后收录。")));
                if (state.workbench)
                {
                    entries.Add(new JournalEntry("person-lin", "林女士", state.identity ? "录音身份已确认" : "录音身份待确认", "证词：西翼的白花由她亲手栽下。\n\n前一晚路线自述：书房 → 餐厅 → 休息室。\n\n" +
                        (state.identity ? "已将录音中的说话者 A 与林女士关联。" : "需要将人物证词与匿名说话者的原句关联，才能确认身份。"),
                        state.identity ? "重建人物路线" : "确认录音身份", () => Go(state.identity ? ScreenId.Route : ScreenId.Identity)));
                    entries.Add(new JournalEntry("person-chen", "陈先生", "候选人物", "当前尚无证据将陈先生关联到录音中的说话者 A。\n\n候选人物不代表已确认身份。"));
                }
            }
            else if (journalTab == "道具")
            {
                if (state.invitation) entries.Add(new JournalEntry("item-invitation", "邀请函", "古堡外获得", "以玫瑰蜡封缄封，邀请拉帕莉亚于晚七时赴宴。", "展开邀请函", OpenJournalInvitation));
                if (state.mapUnlocked) entries.Add(new JournalEntry("item-map", "古堡地图", "女爵赠予 · 两层平面图", "房间按线索与开放时间进入。\n\n地图导航可在场景中通过 M 打开；查看地图不推进时间，实际移动耗时两分钟。"));
                if (state.workbench)
                {
                    entries.Add(new JournalEntry("item-clasp", "柜门金属搭扣", "案件档案 · 现场物证", "现场痕迹记录支持开柜动作与器物受力的关系。\n\n需要先确认声音事件和人物路线，再用于最终事件还原。"));
                    entries.Add(new JournalEntry("item-position", "位置资料", "案件档案 · 三处记录", "21:20 书房记录\n\n21:30 声音事件\n\n21:40 休息室访客簿\n\n作为人物路线的时间与位置依据。"));
                }
            }
            else
            {
                if (state.controlUnlocked) entries.Add(new JournalEntry("event-control", "中控室开放", "女爵提供的线索", "东廊北侧的中控室在 19:00 以后开放。获得许可后，可从场景地图前往。"));
                if (state.confirmedEvent != null) entries.Add(new JournalEntry("event-sound", "餐厅的碰撞与碎裂", "已确认 · 声音事件", "REC-01 21:30 / REC-02 21:27，校正 " + CastleRules.Offset(state.confirmedEvent.offset) +
                    "。\n\n声源：" + state.confirmedEvent.source + "\n书房门关闭，已关联录音原句。\n\n这是确认时保存的结论，不受随后编辑草稿影响。", "查看已确认事件", () => Go(ScreenId.Event)));
                if (state.confirmedRoute != null && state.confirmedRoute.Length == 3) entries.Add(new JournalEntry("event-route", "林女士的路线", "已确认 · 人物路线", "21:20 " + state.confirmedRoute[0] + "\n\n21:30 " + state.confirmedRoute[1] + "\n\n21:40 " + state.confirmedRoute[2], "查看人物路线", () => Go(ScreenId.Route)));
                if (state.finalOK) entries.Add(new JournalEntry("event-truth", "事件还原", "已完成 · 物证关系", "柜门金属搭扣支持开柜动作与器物坠落的关系。\n\n声音、时间、人物路线与独立物证共同构成了本次还原。", "查看事件还原", () => Go(ScreenId.Final)));
                for (int i = 0; i < state.excerpts.Count; i++)
                    entries.Add(new JournalEntry("excerpt-" + i, "录音摘录 " + (i + 1), "原文与来源 · 尚非确认结论", state.excerpts[i]));
            }
            return entries;
        }

        void JournalGrid()
        {
            var v = OpenOverlay("Journal");
            string[] ids = { "PeopleButton", "ItemsButton", "EventsButton" };
            string[] tabs = { "人物", "道具", "事件" };
            for (int i = 0; i < tabs.Length; i++) { string tab = tabs[i]; B(v, ids[i], () => { journalTab = tab; journalScroll = 1; Journal(); }, selected: journalTab == tab); }
            B(v, "CloseButton", CloseOverlay);
            var entries = JournalEntries(); var scroll = v.Get<ScrollRect>("GridScroll");
            T(v, "CountText", journalTab + " · " + entries.Count + " 条记录"); Visible(v, "EmptyText", entries.Count == 0);
            while (journalCards.Count < entries.Count) journalCards.Add(Instantiate(ui.JournalCardPrefab, scroll.content));
            for (int i = 0; i < journalCards.Count; i++)
            {
                if (i >= entries.Count) { journalCards[i].Release(); continue; }
                var entry = entries[i]; var card = journalCards[i]; card.name = "JournalCard:" + entry.Id;
                card.Bind(entry.Id, journalTab, entry.Title, entry.Summary, () => { audioSource.PlayOneShot(clickSound, state.volume); JournalDetail(entry); });
            }
            RestoreScroll(scroll, journalScroll);
        }
        void JournalDetail(JournalEntry entry)
        {
            currentJournalEntry = entry; var v = OpenOverlay("JournalDetail"); overlayBack = Journal;
            T(v, "CategoryText", journalTab); T(v, "TitleText", entry.Title);
            FitBody(v, "BodyText", "DetailScroll", entry.Detail);
            B(v, "BackButton", Journal, "‹ 返回" + journalTab); B(v, "CloseButton", CloseOverlay);
            Visible(v, "ActionButton", entry.Open != null); B(v, "ActionButton", entry.Open, entry.ActionLabel ?? "查看记录");
        }
        void OpenJournalInvitation()
        {
            var v = OpenOverlay("InvitationArchive");
            Action back = () => JournalDetail(currentJournalEntry); overlayBack = back; B(v, "BackButton", back);
        }
    }
}

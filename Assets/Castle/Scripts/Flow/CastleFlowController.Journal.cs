using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Castle.Flow
{
    public sealed partial class CastleFlowController
    {
        private Coroutine _flip;

        private void OpenJournal(string target = null)
        {
            if (target != null)
            {
                var entry = Content.Entry(target);
                Session.JournalCategory = (int)entry.Kind;
                var entries = JournalEntries();
                Session.JournalPage = Math.Max(0, Array.FindIndex(entries, item => item.Id == target)) / JournalPageSize();
                Session.JournalSelection = target;
            }
            PushModal("Journal"); RenderJournal();
        }

        private FlowEntry[] JournalEntries() => Content.Entries.Where(entry => (int)entry.Kind == Session.JournalCategory && Session.Entries.Contains(entry.Id)).ToArray();
        private int JournalPageSize() => Session.JournalCategory == (int)EntryKind.Clue ? 6 : 4;

        private void RenderJournal()
        {
            var view = V("Journal"); var entries = JournalEntries(); int size = JournalPageSize();
            int total = Math.Max(1, (entries.Length + size - 1) / size);
            Session.JournalPage = Mathf.Clamp(Session.JournalPage, 0, total - 1);
            var current = entries.Skip(Session.JournalPage * size).Take(size).ToArray();
            bool items = Session.JournalCategory == (int)EntryKind.Item;
            if (!current.Any(entry => entry.Id == Session.JournalSelection)) Session.JournalSelection = current.FirstOrDefault()?.Id;
            Button(view, "Close", PopModal);
            for (int i = 0; i < 3; i++)
            {
                int category = i;
                Button(view, "Tab" + i, () => { Session.JournalCategory = category; Session.JournalPage = 0; Session.JournalSelection = null; RenderJournal(); });
            }
            Button(view, "Previous", () => TurnPage(-1), enabled: Session.JournalPage > 0);
            Button(view, "Next", () => TurnPage(1), enabled: Session.JournalPage + 1 < total);
            Text(view, "PageNumber", (Session.JournalPage + 1) + " / " + total);
            Text(view, "Category", new[] { "人物", "道具", "记录" }[Session.JournalCategory]);
            Visible(view, "Details", !items);
            Text(view, "Empty", entries.Length == 0 ? "尚未收录。继续调查，寻找新的线索。" : "");
            var left = view.Get<RectTransform>("LeftCards"); var right = view.Get<RectTransform>("RightCards"); Clear(left); Clear(right);
            var grid = left.GetComponent<GridLayoutGroup>();
            grid.constraintCount = Session.JournalCategory == (int)EntryKind.Person ? 2 : 1;
            grid.cellSize = Session.JournalCategory == (int)EntryKind.Person ? new Vector2(230,205) : new Vector2(486, items ? 205 : 68);
            grid.spacing = new Vector2(16,14);
            for (int i = 0; i < current.Length; i++)
            {
                var entry = current[i];
                var card = Card(items && i >= 2 ? right : left, entry.Name, items ? entry.Body : entry.Kind == EntryKind.Person ? "点击查看人物详情" : "已验证线索", () =>
                {
                    if (items) UseItem(entry);
                    else { Session.JournalSelection = entry.Id; RenderJournal(); }
                });
                card.Get<Image>("Background").color = entry.Id == Session.JournalSelection ? new Color(.49f,.39f,.23f) : new Color(.24f,.27f,.21f);
                if (items) card.Get<Text>("Body").rectTransform.sizeDelta = new Vector2(-32, 130);
                if (entry.Kind == EntryKind.Clue) Visible(card, "Body", false);
            }
            var selected = Content.Entry(Session.JournalSelection);
            Text(view, "DetailTitle", selected?.Name ?? "");
            Text(view, "DetailBody", selected?.Body ?? "");
            Button(view, "Timeline", () => ShowPage("Timeline"), enabled: Content.EndingOrder.All(Session.Entries.Contains));
            Visible(view, "Timeline", Session.JournalCategory == (int)EntryKind.Clue);
            Save();
        }

        private void TurnPage(int direction)
        {
            if (_flip != null) return;
            Session.JournalPage += direction; Session.JournalSelection = null;
            if (Session.ReduceMotion) RenderJournal();
            else _flip = StartCoroutine(FlipPage(direction));
        }

        private IEnumerator FlipPage(int direction)
        {
            var sheet = V("Journal").Get<RectTransform>("FlipSheet"); sheet.gameObject.SetActive(true);
            sheet.pivot = new Vector2(direction > 0 ? 0 : 1, .5f);
            for (float elapsed = 0; elapsed < .26f; elapsed += Time.unscaledDeltaTime)
            {
                sheet.localScale = new Vector3(Mathf.Abs(Mathf.Cos(elapsed / .26f * Mathf.PI)),1,1);
                yield return null;
            }
            sheet.gameObject.SetActive(false); sheet.localScale = Vector3.one;
            RenderJournal(); _flip = null;
        }

        private void UseItem(FlowEntry entry)
        {
            if (entry.Id == Content.MapId) { OpenMap(); return; }
            if (entry.Id == Content.InvitationId)
            {
                var view = PushModal("Letter"); Text(view, "Body", entry.Body); Button(view, "Close", PopModal); return;
            }
            if (string.IsNullOrEmpty(entry.UseDialogue)) { Toast("该道具暂无可用操作。"); return; }
            if ((!string.IsNullOrEmpty(entry.UseRoom) && Session.RoomId != entry.UseRoom) || (!string.IsNullOrEmpty(entry.UsePerson) && Session.SelectedNpc != entry.UsePerson))
            { Toast("当前地点或交谈对象不符合使用条件。"); return; }
            if (!string.IsNullOrEmpty(Session.ActiveDialogue) && Content.Dialogue(Session.ActiveDialogue).Forced)
            { Toast("请先结束当前剧情对白。"); return; }
            Session.TapeReturnDialogue = Session.ActiveDialogue;
            StartDialogue(entry.UseDialogue, true);
        }
    }
}

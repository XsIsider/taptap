using System;
using System.Linq;
namespace Castle.V2
{
    public sealed class InventoryService
    {
        readonly SessionService _session;
        public InventoryService(SessionService session) { _session = session; }
        public int Count(string id) => _session.State.Tapes.Count(t => t.EntryId == id) + (_session.State.Items.FirstOrDefault(i => i.Id == id)?.Count ?? 0);
        public void Grant(string value)
        {
            var grant = ContentFormat.Grant(value); var entry = _session.Content.Require(grant.Key); var state = _session.State;
            if (entry.Get("type") == "conclusion") throw new InvalidOperationException("不能收录推论选项");
            if (entry.Get("type") == "tape") { for (int i = 0; i < grant.Value; i++) state.Tapes.Add(new TapeInstance { Id = Guid.NewGuid().ToString("N"), EntryId = entry.Id, RecordId = entry.Get("record") }); }
            else if (entry.Get("type") == "item")
            {
                var item = state.Items.FirstOrDefault(i => i.Id == entry.Id);
                if (item == null) { item = new ItemCount { Id = entry.Id }; state.Items.Add(item); } item.Count += grant.Value;
            }
            else if (!state.Entries.Contains(entry.Id)) state.Entries.Add(entry.Id);
        }
        public bool Claim(string lineId, string entryId)
        {
            var line = _session.Content.Require(lineId);
            if (!line.List("link_entry").Contains(entryId) || !_session.State.ReadLines.Contains(lineId)) return false;
            string key = lineId + ":" + entryId;
            if (!_session.State.Links.Contains(key)) { Grant(entryId); _session.State.Links.Add(key); _session.Persist(); }
            _session.Notice = _session.Content.Require(entryId).Get("text"); return true;
        }
        public bool Extract(string entryId, string lineId)
        {
            var entry = _session.Content.Require(entryId);
            if (!new[] { "clue", "info" }.Contains(entry.Get("type")) || !entry.List("source").Contains(lineId) || !_session.State.ReadLines.Contains(lineId)) return false;
            Grant(entryId); _session.Persist(); return true;
        }
        public bool Consume(string value)
        {
            var grant = ContentFormat.Grant(value); var item = _session.State.Items.FirstOrDefault(i => i.Id == grant.Key);
            if (item == null || item.Count < grant.Value) return false; item.Count -= grant.Value; return true;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace Castle.V24
{
    public sealed partial class V24Engine
    {
        /// <summary>初始资料已解锁后，该人物姓名在后续正文中恢复为普通文字。</summary>
        public bool PersonLinkResolved(string target)
        {
            var pair = target.Split(':');
            if (pair.Length != 2 || !int.TryParse(pair[1],out int id)) return false;
            if (pair[0] == "character") return KnowsCharacter(id);
            if (pair[0] != "entry" || !IsCharacterProfile(id)) return false;
            var profile = Data.EntryById(id);
            return Has(id) || profile.Owner.Any(KnowsCharacter);
        }
        public V24DialogueInteractionGate PendingInteraction => Data.DialogueInteractionGateConfig
            .FirstOrDefault(x => !State.ConsumedActions.Contains(x.Id)
                && (State.ActiveEvent == x.SourceEvent
                    || State.LastCompletedEvent == x.SourceEvent && State.DeferredEvent == x.ResumeEvent));

        private bool InteractionCondition(string condition)
        {
            if (condition.StartsWith("event_complete:"))
                return int.TryParse(condition.Substring(15), out int id) && State.CompletedEvents.Contains(id);
            return State.InteractionSignals != null && State.InteractionSignals.Contains(condition);
        }

        public bool CanOpenInteraction(int actionId)
        {
            var gate = PendingInteraction;
            return gate != null && gate.Id == actionId && State.Shown.Contains(gate.SourcePlot);
        }

        /// <summary>只由探索地图的玩家点击调用；打开、关闭地图不产生选房信号。</summary>
        public bool SelectInteractionRoom(int room)
        {
            var gate = PendingInteraction;
            string signal = "player_selected_room:" + room;
            if (gate == null || !CanOpenInteraction(gate.Id) || !gate.CompletionConditions.Contains(signal)) return false;
            RecordInteraction(signal);
            return true;
        }

        public bool UseInteractionItem(int entry)
        {
            var gate = PendingInteraction;
            if (gate == null || !CanOpenInteraction(gate.Id) || !Has(entry)) return false;
            string signal = "item_used:" + entry + "@plot:" + gate.SourcePlot;
            if (!gate.CompletionConditions.Contains(signal)) return false;
            RecordInteraction(signal);
            return true;
        }

        private void RecordInteraction(string signal)
        {
            if (State.InteractionSignals == null) State.InteractionSignals = new List<string>();
            if (!State.InteractionSignals.Contains(signal)) State.InteractionSignals.Add(signal);
            // 若原对白尚未结束，仅记录操作；事件结算后才允许原路线继续。
            if (State.ActiveEvent == 0) EndChain();
            Commit();
        }
    }
}

using System;
using System.Linq;
using UnityEngine;

namespace Castle.Flow
{
    public enum EntryKind { Person, Item, Clue }
    public enum KeywordKind { Person, Item, Claim, Location }

    [Serializable]
    public sealed class FlowRoom
    {
        public string Id, Name, DeviceId;
        public int Floor = 1, Opens, Closes = 1440;
        public bool InitiallyUnlocked, Navigable;
        [Tooltip("示例探索结算分钟数；不是正式策划数值。")]
        public int ExploreMinutes;
        public Vector4 Bounds;
    }

    [Serializable]
    public sealed class FlowEntry
    {
        public string Id, Name;
        public EntryKind Kind;
        [TextArea] public string Body;
        public string IconResource, UseRoom, UsePerson, UseDialogue;
    }

    [Serializable]
    public sealed class FlowKeyword
    {
        public string Word, Target;
        public KeywordKind Kind;
    }

    [Serializable]
    public sealed class FlowChoice
    {
        public string Label;
        public int NextLine;
    }

    [Serializable]
    public sealed class FlowLine
    {
        public string Speaker;
        [TextArea] public string Text;
        public FlowKeyword[] Keywords = Array.Empty<FlowKeyword>();
        public FlowChoice[] Choices = Array.Empty<FlowChoice>();
    }

    [Serializable]
    public sealed class FlowDialogue
    {
        public string Id;
        public string[] Participants;
        public bool Forced;
        public FlowLine[] Lines;
    }

    [Serializable]
    public sealed class FlowRecording
    {
        public string Id, DeviceId, DeviceName, Name, RoomId;
        [Tooltip("原始设备时间，单位为秒。")]
        public int StartSeconds;
        public int DurationSeconds = 240;
        public AudioClip Audio;
        public string[] Texts;
        public int[] AtSeconds;
    }

    [Serializable]
    public sealed class FlowPuzzle
    {
        public string Id, Name, ClueId;
        public string[] RecordingIds;
        public int LineIndex;
        public float ToleranceSeconds = 2;
    }

    /// <summary>UI 的稳定内容接口。新版策划表到位后通过适配器填充，不把旧表字段作为正式约束。</summary>
    [CreateAssetMenu(menuName = "Castle/UI 流程配置")]
    public sealed class CastleFlowContent : ScriptableObject
    {
        public string InvitationId = "item_invitation", MapId = "item_map", TapeId = "item_tape";
        public string OpeningDialogue = "dlg_butler", InitialRoom = "east", ControlRoom = "110";
        public int InitialMinutes = 1138;
        public float AutoSeconds = 3;
        public FlowRoom[] Rooms;
        public FlowEntry[] Entries;
        public FlowDialogue[] Dialogues;
        public FlowRecording[] Recordings;
        public FlowPuzzle[] Puzzles;
        public string[] EndingOrder;
        public string[] EndingLines;
        public float EndingLineSeconds = 4;
        public AudioClip EndingAudio;

        public FlowRoom Room(string id) => Rooms.FirstOrDefault(value => value.Id == id);
        public FlowEntry Entry(string id) => Entries.FirstOrDefault(value => value.Id == id);
        public FlowDialogue Dialogue(string id) => Dialogues.FirstOrDefault(value => value.Id == id);
        public FlowRecording Recording(string id) => Recordings.FirstOrDefault(value => value.Id == id);

        public void ValidateContent()
        {
            Require(Rooms.Select(value => value.Id), "地点");
            Require(Entries.Select(value => value.Id), "条目");
            Require(Dialogues.Select(value => value.Id), "对白");
            Require(Recordings.Select(value => value.Id), "录音");
            Require(Puzzles.Select(value => value.Id), "谜题");
            if (Room(InitialRoom) == null || Room(ControlRoom) == null || Dialogue(OpeningDialogue) == null)
                throw new InvalidOperationException("开场配置引用缺失。");
            foreach (var id in new[] { InvitationId, MapId, TapeId })
                if (Entry(id) == null) throw new InvalidOperationException("初始道具引用缺失：" + id);
            foreach (var entry in Entries)
                if (!string.IsNullOrEmpty(entry.UseDialogue) && Dialogue(entry.UseDialogue) == null)
                    throw new InvalidOperationException("道具对白不存在：" + entry.Id);
            foreach (var dialogue in Dialogues)
            {
                if (dialogue.Lines == null || dialogue.Lines.Length == 0) throw new InvalidOperationException("对白为空。");
                foreach (var line in dialogue.Lines)
                {
                    foreach (var keyword in line.Keywords)
                        if (!line.Text.Contains(keyword.Word) || (keyword.Kind == KeywordKind.Location ? Room(keyword.Target) == null : Entry(keyword.Target) == null))
                            throw new InvalidOperationException("关键词引用或文字不匹配：" + dialogue.Id);
                    foreach (var choice in line.Choices)
                        if (choice.NextLine < 0 || choice.NextLine >= dialogue.Lines.Length) throw new InvalidOperationException("选项跳转越界。");
                }
            }
            foreach (var recording in Recordings)
                if (recording.Texts.Length != recording.AtSeconds.Length || Room(recording.RoomId) == null || recording.AtSeconds.Any(at => at < 0 || at > recording.DurationSeconds))
                    throw new InvalidOperationException("录音时间或地点配置错误：" + recording.Id);
            foreach (var puzzle in Puzzles)
                if (Entry(puzzle.ClueId)?.Kind != EntryKind.Clue || puzzle.RecordingIds.Length < 2 || puzzle.RecordingIds.Any(id => Recording(id) == null || puzzle.LineIndex >= Recording(id).Texts.Length))
                    throw new InvalidOperationException("谜题引用错误：" + puzzle.Id);
            if (EndingOrder.Length == 0 || EndingOrder.Distinct().Count() != EndingOrder.Length || EndingOrder.Any(id => Entry(id)?.Kind != EntryKind.Clue))
                throw new InvalidOperationException("结局必须引用不重复的线索。");
        }

        private static void Require(System.Collections.Generic.IEnumerable<string> ids, string label)
        {
            var values = ids.ToArray();
            if (values.Any(string.IsNullOrEmpty) || values.Distinct().Count() != values.Length)
                throw new InvalidOperationException(label + " ID 缺失或重复。");
        }
    }
}

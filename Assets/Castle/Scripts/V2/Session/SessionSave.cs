using System;
using System.Collections.Generic;

namespace Castle.V2
{
    [Serializable] public sealed class ItemCount { public string Id; public int Count; }
    [Serializable] public sealed class TapeInstance { public string Id, EntryId, RecordId; }
    [Serializable] public sealed class PersonalRecord { public string Id, Started, Room, Imported; }
    [Serializable] public sealed class PuzzleDraft { public string Id, Reference; public string[] Values = Array.Empty<string>(); public float Shift; }
    [Serializable] public sealed class DeviceKnowledge { public string Id, Room; public bool Calibrated; public float Offset; }
    [Serializable] public sealed class EventProgress { public string Id, Started, Room; public int Line; public float Cursor; public bool LastLineDone, Replay; }
    [Serializable] public sealed class RecordProgress { public string Id; public float Cursor; }
    [Serializable] public sealed class SessionSave
    {
        public int Version = 2;
        public string ContentVersion, WorldTime, Room, PendingPuzzle, EndingStage = "none";
        public bool Started, ControlVisit;
        public EventProgress Active;
        public List<string> Goals = new List<string>(), Entries = new List<string>(), Solved = new List<string>(), Links = new List<string>(), CompletedEvents = new List<string>(), BackupRoutes = new List<string>(), MarkedLines = new List<string>(), ReadLines = new List<string>(), Played = new List<string>();
        public List<ItemCount> Items = new List<ItemCount>();
        public List<TapeInstance> Tapes = new List<TapeInstance>();
        public List<PersonalRecord> Personal = new List<PersonalRecord>();
        public List<PuzzleDraft> Drafts = new List<PuzzleDraft>();
        public List<DeviceKnowledge> Devices = new List<DeviceKnowledge>();
        public List<RecordProgress> Playback = new List<RecordProgress>();
    }

}

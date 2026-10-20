using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace Castle.V24
{
    [Serializable] public sealed class V24RecordingState
    {
        public int Id, CapturedOffset, Day; public long TrueStart, RawStart; public bool Complete; public float HeardSeconds;
    }
    [Serializable] public sealed class V24Inventory { public int Entry, Count; }
    [Serializable] public sealed class V24Tape
    {
        public int Instance, Entry, Record; public float ListenedSeconds; public long RawStart; public int CapturedOffset;
    }
    [Serializable] public sealed class V24Draft
    {
        public List<string> Doors = new List<string>();
        public string Hypothesis = "";
        public int Id, Reference, Floor = 1; public List<int> Values = new List<int>(); public float Shift;
    }
    [Serializable] public sealed class V24MapView
    {
        public int Puzzle, Floor; public float Zoom = 1, X, Y;
    }
    [Serializable] public sealed class V24Transcript
    {
        public int Plot, Choice; public string Reply;
    }
    /// <summary>V2.4 独立存档；时间使用 DateTime ticks，避免跨午夜丢失日期。</summary>
    [Serializable] public sealed class V24State
    {
        public int DeferredEvent, PlaybackGate, LastCompletedEvent, WakeEvent;
        // 现场交互去重记录；旧档缺省为空，不更改原存档版本。
        public List<string> InteractionSignals = new List<string>();
        public string Activity = "", SceneContext = "";
        public bool PathReadyNotice;
        public bool PathCompleteNotice;
        public List<int> HeardRecords = new List<int>(), ImportedRecords = new List<int>();
        public int Version = 26, Day = 1, Room, ActiveEvent, Line, QueuedItem, PendingPuzzle, TutorialEvent;
        public int ActionPending, SessionAction;
        public string ControlMode = "";
        public bool RepairNoticeShown;
        public List<int> ConsumedActions = new List<int>();
        public long Now, DayBase, ControlEntered;
        public int ControlCost, ControlDay, ControlReturn, NextTape = 1, InvitationStage;
        public string ControlSession = "", Page = "Title", ReturnPage = "Explore", SleepStage = "";
        public bool Started, SleepPending, Ending, Finished, Auto, EndingTransitionDone;
        public float DialogueScroll = 1, AutoSeconds = 1.8f, Volume = .7f, EffectsVolume = .7f;
        public int JournalCategory, JournalPage, JournalSelection, RecordId, PlayingTape, ReturnPath;
        public int[] JournalPages = new int[3];
        public List<int> ExpandedDevices = new List<int>();
        public float RecordPosition, PlaybackHeard;
        public List<int> Entries = new List<int>(), Goals = new List<int>(), CompletedEvents = new List<int>(), Puzzles = new List<int>(), Repaired = new List<int>(), Unlocked = new List<int>(), KnownDevices = new List<int>(), Marked = new List<int>(), Shown = new List<int>(), PlanB = new List<int>(), Choices = new List<int>();
        public List<string> Claims = new List<string>(), Grants = new List<string>();
        public List<int> RewardPopups = new List<int>();
        public List<V24Inventory> Inventory = new List<V24Inventory>();
        public List<V24Tape> Tapes = new List<V24Tape>();
        public List<V24RecordingState> Recordings = new List<V24RecordingState>();
        public List<V24Draft> Drafts = new List<V24Draft>();
        public List<V24MapView> Views = new List<V24MapView>();
        public List<V24Transcript> Transcript = new List<V24Transcript>();
    }
    public static class V24Save
    {
        public static string Path => System.IO.Path.Combine(Application.persistentDataPath, "castle-formal-20261019.json");
        public static void Write(V24State state, string path = null)
        {
            path = path ?? Path;
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            File.WriteAllText(path + ".tmp", JsonUtility.ToJson(state, true));
            if (File.Exists(path)) File.Replace(path + ".tmp", path, path + ".bak");
            else File.Move(path + ".tmp", path);
        }
        public static V24State Read(string path = null)
        {
            path = path ?? Path;
            if (!File.Exists(path)) return new V24State();
            var state = JsonUtility.FromJson<V24State>(File.ReadAllText(path));
            if (state == null || state.Version != 26) throw new InvalidDataException("存档版本不匹配，请开始正式版调查。旧存档保留。");
            return state;
        }
    }
}

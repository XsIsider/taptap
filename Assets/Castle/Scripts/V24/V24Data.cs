using System;
using System.Linq;
using UnityEngine;
namespace Castle.V24
{
    [Serializable] public sealed class V24Settings
    {
        public string InitialTime, WakeTime, ForceSleepTime;
        public int[] InitialItems;
        public int LastInvestigationDay;
        public string RestStartTime;
        public int ArrivalRoom, ArrivalEvent, TapeTutorialEvent, HomeRoom, ControlRoom, MapEntry, ControlMinutes, ControlIntroEvent, IntroSoundPath;
        public bool KeepRecords;
    }
    [Serializable] public sealed class V24Device
    {
        public int Id, Room, Offset;
        public string Channel, Label;
        public string[] LocationNeed;
    }
    [Serializable] public sealed class V24Node { public int Id, Room, Floor; public float[] Position; }
    [Serializable] public sealed class V24MapFloor { public int Floor; public string Background; }
    [Serializable] public sealed class V24Map { public int Id; public string Background; public V24Node[] Nodes; public V24MapFloor[] Floors; }
    [Serializable] public sealed class V24Hotspot
    {
        public int Id, Room, Character, Day, PuzzleId;
        public string Key, Mode, Start, End;
        public string[] Need;
    }
    [Serializable] public sealed class V24Data
    {
        public V24Character[] Character;
        public V24Room[] Room;
        public V24Entry[] Entry;
        public V24Plot[] Plot;
        public V24PlotChoice[] PlotChoice;
        public V24Event[] Event;
        public V24Record[] Record;
        public V24Puzzle[] Puzzle;
        public V24Settings GameSettings;
        public V24Device[] DeviceConfig;
        public V24Map[] MapConfig;
        public V24Hotspot[] HotspotBinding;
        public int InvitationEntry, TutorialTape;
        public V24ControlPolicy ControlRoomPolicy;
        public V24Action[] DialogueActionConfig;
        public V24SleepPolicy SleepPolicy;
        public V24Finale FinaleConfig;
        public V24RoomDisplay[] RoomDisplayConfig;
        public V24Plot[] Lines(int group) => Plot.Where(x => x.Group == group).OrderBy(x => x.Order).ToArray();
        public V24Event EventById(int id) => Event.FirstOrDefault(x => x.Id == id);
        public V24Entry EntryById(int id) => Entry.FirstOrDefault(x => x.Id == id);
        public V24Record RecordById(int id) => Record.FirstOrDefault(x => x.Id == id);
        public V24Puzzle PuzzleById(int id) => Puzzle.FirstOrDefault(x => x.Id == id);
        public static V24Data Load() => JsonUtility.FromJson<V24Data>(Resources.Load<TextAsset>("Castle/V24/Content").text.Replace("收藏厅/画廊", "收藏厅"));
    }
}

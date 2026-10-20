using System;
namespace Castle.V24
{
    [Serializable] public sealed class V24ControlDay
    { public int Day, Action, SessionsMax; public bool MapEntry; public string Mode, Start, End; }
    [Serializable] public sealed class V24ControlPolicy
    { public int Room; public V24ControlDay[] Days; public string RepairToast; }
    [Serializable] public sealed class V24Action
    {
        public int Id, SourceEvent, SourcePlot, Day, OnEnterEvent, ReturnRoom, ExitMinutes, ExitEvent;
        public string EnterClock, ExitClock;
        public string Action, Hint;
        public int Entry, InitialFloor;
        public bool Required; public string Start, End, SessionMode;
        public string[] Need, ExitNeed;
    }
    [Serializable] public sealed class V24SleepRequirement
    { public int Day; public string[] All, Any; }
    [Serializable] public sealed class V24Ghost
    { public int Event, Day, OnceGoal; public string Start; }
    [Serializable] public sealed class V24SleepPolicy
    { public string Start, Force, Wake, Confirm; public int LastDay; public V24SleepRequirement[] Requirements; public V24Ghost Day3Ghost; }
    [Serializable] public sealed class V24EndCard
    { public int AfterGoal; public string Text; }
    [Serializable] public sealed class V24Finale
    {
        public int Day, Action, Timeline, TruthEvent, ExitRoom, ExitEvent, LastDialogueEvent;
        public int[] RequiredEntries, RequiredGoals, MustExistRecords;
        public V24EndCard EndCard;
    }
    [Serializable] public sealed class V24RoomDisplay
    { public int Room; public string MapLabel, BeforeName, AfterName; public string[] RevealNeed; }
}

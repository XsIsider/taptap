using System;
namespace Castle.V24
{
    [Serializable] public sealed class V24StoryStep
    { public int FromEvent, ToEvent, ToRoom, Day; public string Kind, SceneContext, SetClock; }
    [Serializable] public sealed class V24EntryVariant
    { public int[] Entries = Array.Empty<int>(); public int ConditionAlias, TimelineAlias; }
    [Serializable] public sealed class V24PlotEffect
    { public string Trigger, Context, Kind, Text; public int Plot, Record, Event, Entry, Quantity; }
    [Serializable] public sealed class V24PlotMeta
    { public int Id; public float Duration; public string Unknown, SpeakerDisplayOverride; public bool Sound; }
    [Serializable] public sealed class V24PlaybackGate
    { public int Event, Record, ThenEvent; public bool MustFinish, AllowEarlyExit; public string WorldClockAfter; }
    [Serializable] public sealed class V24ConversationGroup
    {
        public string Id; public int AfterEvent, RequiredEvent, Day;
        public int[] Events = Array.Empty<int>(), Ordered = Array.Empty<int>();
        public string[] Clocks = Array.Empty<string>();
    }
    [Serializable] public sealed class V24DayActivity
    {
        public string Id, StartClock, FinishClock, FinishClockMin, After;
        public int AfterEvent, NextEvent, TimePerEvent, TimeBudget;
        public int[] AfterAnyEvent = Array.Empty<int>(), Events = Array.Empty<int>();
        public bool Mandatory, MandatoryAvailable, AllowWaitToFinish;
    }
    [Serializable] public sealed class V24ConditionalRoute
    { public int AfterEvent, IfEntry, ThenEvent, ElseEvent; }
    [Serializable] public sealed class V24PersonPathStep { public int Step, Node; public string Time; }
    [Serializable] public sealed class V24PersonPathPolicy
    { public int Puzzle, Character; public V24PersonPathStep[] Nodes=Array.Empty<V24PersonPathStep>(); }
    [Serializable] public sealed class V24ItemPolicy
    { public int GiftEntry, OpenAfterEvent, LetterEntry; public string OpenedDescription, LetterFullText; }
}

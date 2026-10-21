// Generated from the supplied workbook schema by Tools/import_v24.py.
using System;
namespace Castle.V24
{
    [Serializable]
    public sealed class V24Character
    {
        public int Id;
        public string Name = "";
        public string UnknownName = "";
        public string Portrait = "";
        public string Avatar = "";
        public string[] NameNeed = Array.Empty<string>();
    }
    [Serializable]
    public sealed class V24Entry
    {
        public int Id;
        public string Name = "";
        public string Type = "";
        public string Text = "";
        public string Icon = "";
        public string DetailImage = "";
        public int[] Owner = Array.Empty<int>();
        public int Record;
        public string[] Source = Array.Empty<string>();
    }
    [Serializable]
    public sealed class V24Event
    {
        public int Id;
        public string Name = "";
        public int Room;
        public string Trigger = "";
        public string Target = "";
        public int UseEntry;
        public int Day;
        public string Start = "";
        public string End = "";
        public string[] Need = Array.Empty<string>();
        public int Plot;
        public int ChoiceGroup;
        public int Time;
        public int[] Reward = Array.Empty<int>();
        public int Goal;
        public int PlanB;
        public int Record;
        public int NextEvent;
    }
    [Serializable]
    public sealed class V24PlotChoice
    {
        public int Id;
        public int Group;
        public int Order;
        public string Text = "";
        public int NextEvent;
    }
    [Serializable]
    public sealed class V24Plot
    {
        public int Id;
        public int Group;
        public int Order;
        public int SpeakerId;
        public string Text = "";
        public string Sfx = "";
        public string[] LinkText = Array.Empty<string>();
        public string[] LinkTarget = Array.Empty<string>();
        public float At;
        public bool Anchor;
    }
    [Serializable]
    public sealed class V24Puzzle
    {
        public int Id;
        public string Name = "";
        public string Type = "";
        public string[] Need = Array.Empty<string>();
        public int Target;
        public int Map;
        public int[] Candidates = Array.Empty<int>();
        public string Answer = "";
        public int[] Reference = Array.Empty<int>();
        public int Success;
    }
    [Serializable]
    public sealed class V24Record
    {
        public int Id;
        public string Name = "";
        public int Device;
        public string Start = "";
        public float Duration;
        public int Plot;
        public string[] Need = Array.Empty<string>();
        public string AccessType = "";
    }
    [Serializable]
    public sealed class V24Room
    {
        public int Id;
        public string Name = "";
        public int Floor;
        public string Scene = "";
        public string Start = "";
        public string End = "";
        public string[] UnlockNeed = Array.Empty<string>();
    }
}

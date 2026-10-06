using System;
using System.Collections.Generic;

namespace Castle
{
    public enum ScreenId { Title, Outside, Invitation, Collected, Lounge, Room, Browse, Align, Sound, Event, Identity, Route, Final, Truth, Choice, Ending }
    public enum DoorState { Unknown, Open, Closed }

    [Serializable]
    public sealed class EventRecord
    {
        public int offset;
        public string source;
        public DoorState door;
        public bool doorEvidence;
    }

    [Serializable]
    public sealed class CastleState
    {
        public int version = 1;
        public bool started, invitation, mapUnlocked, controlUnlocked, recorded, workbench;
        public int minutes = 1125, dialogue;
        public string room = "outside";
        public ScreenId resume = ScreenId.Outside;
        public List<string> discovered = new List<string> { "101", "102", "106", "stairs1", "east" };
        public bool anchorA, anchorB, pairA, pairB, aligned, pathSaved;
        public int offset;
        public string source = "";
        public DoorState door;
        public bool doorEvidence;
        public EventRecord confirmedEvent;
        public bool identityEvidence, identity;
        public string candidate = "";
        public string[] route = new string[3];
        public string[] confirmedRoute;
        public bool routeEvidence, finalEvidence, finalOK;
        public int truthIndex;
        public string ending = "";
        public List<string> excerpts = new List<string>();
        public float volume = .7f;
        public int readingSize = 23;
        public bool reduceMotion;

        public void Discover(params string[] ids)
        {
            foreach (var id in ids) if (!discovered.Contains(id)) discovered.Add(id);
        }
    }

    // Rules never depend on a view. A changed draft cannot mutate a confirmed record.
    public static class CastleRules
    {
        public static void AdvanceDialogue(CastleState s)
        {
            s.dialogue = Math.Min(5, s.dialogue + 1);
            if (s.dialogue >= 2) s.mapUnlocked = true;
            if (s.dialogue >= 4)
            {
                s.controlUnlocked = true;
                s.Discover("110", "stairs2", "205", "206");
            }
            if (s.dialogue == 5 && !s.recorded) { s.recorded = true; s.minutes += 2; }
        }

        public static string CanVisit(CastleState s, RoomDefinition room)
        {
            if (room == null || !s.discovered.Contains(room.id)) return "尚未探索。请继续交谈或查看录音档案。";
            if (room.id == "110" && !s.controlUnlocked) return "需要女爵的进入许可。";
            if (s.minutes < room.opens) return Clock(room.opens) + " 以后开放；阅读与查看地图不会推进时间。";
            if (s.minutes >= 1440) return "今晚的探索时段已经结束。";
            return null;
        }

        public static string Travel(CastleState s, RoomDefinition room)
        {
            var error = CanVisit(s, room);
            if (error != null) return error;
            if (s.room != room.id) s.minutes += 2;
            s.room = room.id;
            return null;
        }

        public static void ChangeTime(CastleState s, int offset)
        {
            s.offset = Math.Max(-180, Math.Min(360, offset));
            s.aligned = s.pathSaved = false;
        }

        public static string Align(CastleState s)
        {
            if (!(s.anchorA && s.anchorB && s.pairA && s.pairB)) return "请先标记两个设备的声音片段，并加入候选组。";
            if (Math.Abs(s.offset - 180) > 2) return "锚点尚未对齐。核对两台设备报告的事件时间，调整 REC-02 的整条轨道。";
            s.aligned = true;
            return null;
        }

        public static string VerifyEvent(CastleState s)
        {
            if (!s.aligned || Math.Abs(s.offset - 180) > 2) return "时间草稿已改变，请重新检查对齐。";
            if (!s.pathSaved) return "请先保存声音传播草稿。";
            if (s.source != "餐厅" || s.door != DoorState.Closed) return "REC-02 的隔门闷响与当前假设冲突。请核对声源与录音发生时的门状态。";
            if (!s.doorEvidence) return "历史门状态缺少证据，请关联录音中的原句。";
            s.confirmedEvent = new EventRecord { offset = s.offset, source = s.source, door = s.door, doorEvidence = true };
            return null;
        }

        public static string VerifyIdentity(CastleState s)
        {
            if (s.confirmedEvent == null) return "先完成声音事件的联合验证。";
            if (!s.identityEvidence || string.IsNullOrEmpty(s.candidate)) return "选择候选人物，并关联面对面交谈的原句。";
            if (s.candidate != "林女士") return "候选人物与关联证词不符。请重新查看白花的证词。";
            s.identity = true;
            return null;
        }

        public static string VerifyRoute(CastleState s)
        {
            if (!s.identity || !s.routeEvidence) return "需要已确认的录音身份和各位置节点的来源证据。";
            if (s.route[0] != "书房" || s.route[1] != "餐厅" || s.route[2] != "休息室") return "至少一个位置与证据不符。请核对书房记录、声音事件与访客簿的时间。";
            s.confirmedRoute = (string[])s.route.Clone();
            return null;
        }

        public static string Submit(CastleState s)
        {
            if (s.confirmedEvent == null || s.confirmedRoute == null || !s.finalEvidence) return "事件还原需要已验证的声音事件、人物路线与独立物证。";
            s.finalOK = true;
            s.truthIndex = 0;
            return null;
        }

        public static string Clock(int minute) { return (minute / 60).ToString("00") + ":" + (minute % 60).ToString("00"); }
        public static string Offset(int seconds) { return (seconds < 0 ? "−" : "+") + Clock(Math.Abs(seconds)); }
    }
}

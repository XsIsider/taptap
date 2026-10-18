using System.Linq;
using Castle.Flow;
using UnityEngine;

namespace Castle.Editor
{
    /// <summary>本轮演示内容，不代表正式剧情或正式表结构。只在配置资源不存在时写入。</summary>
    public static class CastleFlowSample
    {
        public static CastleFlowContent Create()
        {
            var content = ScriptableObject.CreateInstance<CastleFlowContent>();
            var legacy = Resources.Load<CastleDatabase>("Castle/Database");
            content.Rooms = legacy.rooms.Select(room => new FlowRoom
            {
                Id = room.id, Name = room.id == "east" ? "走廊" : room.title, Floor = room.floor,
                Opens = room.id == "110" ? 1140 : room.opens, Closes = 1440,
                InitiallyUnlocked = new[] { "east", "102", "101" }.Contains(room.id),
                Navigable = new[] { "east", "101", "110", "103", "104", "108", "206" }.Contains(room.id),
                ExploreMinutes = 2,
                DeviceId = room.id == "103" ? "device_b" : room.id == "104" ? "device_a" : room.id == "108" ? "device_c" : "",
                Bounds = new Vector4((room.x - room.width / 2) / 1672f, (room.y - room.height / 2) / 941f, room.width / 1672f, room.height / 941f)
            }).ToArray();
            content.Entries = new[]
            {
                Entry("person_butler", "管家", EntryKind.Person, "古堡的管家。负责接待宾客，并向你交付地图。"),
                Entry("person_guest", "访客", EntryKind.Person, "在走廊遇见的访客。此角色用于展示自由交談和对白选项。"),
                Entry("person_lady", "女爵", EntryKind.Person, "古堡的主人。人物内容为界面示例，正式介绍待补充。"),
                Entry("person_guard", "守卫", EntryKind.Person, "在走廊值守。用于演示多人交谈对象切换。"),
                Entry("person_cook", "厨师", EntryKind.Person, "示例角色，用于演示人物调查册翻页。"),
                Entry(content.InvitationId, "邀请函", EntryKind.Item, "致拉帕莉亚：\n诚邀您今晚来到古堡。抵达后，请向走廊的管家出示此函。\n\n此函已妥善收入调查册。"),
                Entry(content.MapId, "古堡地图", EntryKind.Item, "标有古堡各层的地点。打开后可查看开放时间和当前位置。"),
                new FlowEntry { Id = content.TapeId, Name = "演示磁带", Kind = EntryKind.Item, Body = "可在走廊反复播放的演示磁带。点击即可使用。", UseRoom = "east", UseDialogue = "dlg_tape" },
                Entry("clue_steps", "走廊的脚步", EntryKind.Clue, "21:20，三处录音捕捉到一串脚步。锚点与设备空间位置已匹配。"),
                Entry("clue_impact", "碰撞与碎裂", EntryKind.Clue, "21:30，短促的金属碰撞后传来玻璃碎裂。锚点与设备空间位置已匹配。"),
                Entry("clue_door", "关门之后", EntryKind.Clue, "21:40，一声关门后，走廊重新安静。锚点与设备空间位置已匹配。")
            };
            content.Dialogues = new[]
            {
                new FlowDialogue { Id = content.OpeningDialogue, Participants = new[] { "person_butler" }, Forced = true, Lines = new[]
                {
                    Line("管家", "欢迎来到古堡。我是这里的管家。", Key("管家", "person_butler", KeywordKind.Person)),
                    Line("管家", "请收下这份古堡地图。您可以用它查看各处的开放时间。", Key("古堡地图", content.MapId, KeywordKind.Claim)),
                    Line("管家", "中控室十九点开放。书房、餐厅和休息室也在地图上。", Key("中控室", "110", KeywordKind.Location), Key("书房", "103", KeywordKind.Location), Key("餐厅", "104", KeywordKind.Location), Key("休息室", "108", KeywordKind.Location)),
                    Line("管家", "二楼还有画廊。对白时可以查看地图，结束交谈后再前往。", Key("画廊", "206", KeywordKind.Location)),
                    Line("管家", "这盘演示磁带也交给您。在走廊打开调查册，点击磁带即可反复播放。", Key("演示磁带", content.TapeId, KeywordKind.Claim), Key("走廊", "east", KeywordKind.Location)),
                    Line("拉帕莉亚", "谢谢，我先在走廊探索一下，再去中控室。", Key("走廊", "east", KeywordKind.Location), Key("中控室", "110", KeywordKind.Location))
                } },
                new FlowDialogue { Id = "dlg_guest", Participants = new[] { "person_guest", "person_lady" }, Lines = new[]
                {
                    Line("访客", "女爵正在这里。您想先了解哪一件事？", Key("女爵", "person_lady", KeywordKind.Person)),
                    new FlowLine { Speaker = "拉帕莉亚", Text = "我想问……", Choices = new[] { new FlowChoice { Label = "询问古堡地图", NextLine = 2 }, new FlowChoice { Label = "询问邀请函", NextLine = 3 } } },
                    new FlowLine { Speaker = "访客", Text = "古堡地图可以随时查看。", Keywords = new[] { Key("古堡地图", content.MapId, KeywordKind.Item) }, Choices = new[] { new FlowChoice { Label = "结束话题", NextLine = 4 } } },
                    Line("女爵", "邀请函记录着今晚的约定。", Key("邀请函", content.InvitationId, KeywordKind.Item)),
                    Line("拉帕莉亚", "我明白了。")
                } },
                new FlowDialogue { Id = "dlg_guard", Participants = new[] { "person_guard", "person_cook", "person_butler" }, Lines = new[]
                {
                    Line("守卫", "这里可以选择不同的交谈对象；切换后仍保留之前读到的位置。"),
                    Line("厨师", "餐厅在走廊北侧。", Key("餐厅", "104", KeywordKind.Location)),
                    Line("管家", "如果已经收下邀请函，可以在调查册中重新阅读。", Key("邀请函", content.InvitationId, KeywordKind.Item))
                } },
                new FlowDialogue { Id = "dlg_tape", Participants = new[] { "person_butler" }, Forced = true, Lines = new[]
                {
                    Line("磁带中的声音", "这是一段演示录音。播放不会消耗磁带，也不会推进探索时间。"),
                    Line("磁带中的声音", "中控室的三份录音来自餐厅、书房、休息室。设备 B 比 A 慢三分钟，C 比 A 快一分钟。", Key("中控室", "110", KeywordKind.Location)),
                    Line("磁带中的声音", "请按脚步、碰撞、关门的顺序还原示例。播放结束后返回调查册。")
                } }
            };
            content.Recordings = new[]
            {
                Record("record_a", "device_a", "文件 01", "104", 76740),
                Record("record_b", "device_b", "文件 02", "103", 76560),
                Record("record_c", "device_c", "文件 03", "108", 76800)
            };
            string[] clueIds = { "clue_steps", "clue_impact", "clue_door" };
            string[] titles = { "走廊的脚步", "碰撞与碎裂", "关门之后" };
            content.Puzzles = Enumerable.Range(0, 3).Select(i => new FlowPuzzle { Id = "puzzle_" + i, Name = titles[i], ClueId = clueIds[i], LineIndex = i, RecordingIds = new[] { "record_a", "record_b", "record_c" } }).ToArray();
            content.EndingOrder = clueIds;
            content.EndingLines = new[]
            {
                "21:20 · 脚步沿走廊而来。不同设备的记录，终于回到了同一条时间线上。",
                "21:30 · 金属碰撞，接着玻璃碎裂。三份录音共同指向了这一刻。",
                "21:40 · 门合上了。脚步、碰撞与关门的先后关系，揭开了这一段经过。\n\n示例演出结束。正式结局内容待接入。"
            };
            content.ValidateContent(); return content;
        }

        private static FlowEntry Entry(string id, string name, EntryKind kind, string body) => new FlowEntry { Id = id, Name = name, Kind = kind, Body = body };
        private static FlowKeyword Key(string word, string target, KeywordKind kind) => new FlowKeyword { Word = word, Target = target, Kind = kind };
        private static FlowLine Line(string speaker, string text, params FlowKeyword[] keywords) => new FlowLine { Speaker = speaker, Text = text, Keywords = keywords };
        private static FlowRecording Record(string id, string device, string name, string room, int start) => new FlowRecording
        {
            Id = id, DeviceId = device, DeviceName = "录音设备 " + device.Substring(device.Length - 1).ToUpperInvariant(), Name = name, RoomId = room, StartSeconds = start, DurationSeconds = 1320,
            AtSeconds = new[] { 60, 660, 1260 },
            Texts = new[] { "一串脚步由远及近，然后停下。", "三声金属碰撞，随后传来玻璃碎裂。", "门被合上，走廊重归安静。" }
        };
    }
}

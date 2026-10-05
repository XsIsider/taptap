using System;
using UnityEngine;

namespace Castle
{
    [Serializable]
    public sealed class RoomDefinition
    {
        public string id, title;
        public int floor, opens;
        public float x, y, width, height;
        public RoomDefinition(string id, string title, int floor, float x, float y, float width, float height, int opens = 1140)
        { this.id = id; this.title = title; this.floor = floor; this.x = x; this.y = y; this.width = width; this.height = height; this.opens = opens; }
    }

    [CreateAssetMenu(menuName = "Castle/剧情数据库", fileName = "Database")]
    public sealed class CastleDatabase : ScriptableObject
    {
        public string[] greeting = {
            "女爵：欢迎来到古堡，拉帕莉亚。愿您今晚过得愉快。",
            "拉帕莉亚：承蒙邀请，女爵阁下。这座古堡比我想象中更为壮丽。",
            "女爵：初来乍到，难免迷路。请收下这份地图，宴会厅就在东廊尽头。",
            "拉帕莉亚：多谢您的指引，女爵阁下。",
            "女爵：东廊北侧是中控室，十九点以后开放。若对古堡的记录感兴趣，您可以去那里看看。",
            "拉帕莉亚：我会记得。祝您度过一个愉快的夜晚。"
        };
        public string[] truth = {
            "不同设备记录的碰撞与碎裂，来自同一次餐厅事件。隔门闷响解释了两份录音的差异。",
            "校正后的时间，与现场物证和人物位置记录相互支持。打开柜门的动作，与器物坠落产生了联系。",
            "零散的记录已经连接成完整经过。接下来，拉帕莉亚需要决定如何处理这些事实。"
        };
        public RoomDefinition[] rooms = {
            new RoomDefinition("101", "会客厅",1,365,588,274,218,0),
            new RoomDefinition("102", "门厅",1,838,650,203,128,0),
            new RoomDefinition("103", "书房",1,402,281,248,176),
            new RoomDefinition("104", "餐厅",1,833,420,282,152),
            new RoomDefinition("105", "厨房",1,1248,292,248,178),
            new RoomDefinition("106", "宴会厅",1,1300,538,296,304),
            new RoomDefinition("107", "音乐室",1,403,422,253,121),
            new RoomDefinition("108", "休息室",1,1137,641,254,192),
            new RoomDefinition("109", "储藏室",1,610,285,148,160),
            new RoomDefinition("110", "中控室",1,1069,301,150,157),
            new RoomDefinition("201", "主卧",2,369,590,288,224,1260),
            new RoomDefinition("203", "客房",2,414,417,240,137,1260),
            new RoomDefinition("204", "二层书房",2,605,286,141,162,1260),
            new RoomDefinition("205", "女爵卧室",2,1250,283,274,216,1260),
            new RoomDefinition("206", "画廊",2,834,422,276,151),
            new RoomDefinition("207", "收藏室",2,1300,537,294,302,1260),
            new RoomDefinition("208", "衣帽间",2,1075,285,157,160,1260),
            new RoomDefinition("209", "储物室",2,1125,647,212,122,1260),
            new RoomDefinition("210", "塔楼",2,376,277,278,194,1260),
            new RoomDefinition("stairs1", "楼梯",1,832,262,217,150,0),
            new RoomDefinition("stairs2", "楼梯",2,832,262,217,150,0),
            new RoomDefinition("east", "东廊",1,842,533,595,65,0)
        };

        public RoomDefinition Room(string id) { return Array.Find(rooms, r => r.id == id); }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Castle.V2
{
    public static class ContentValidator
    {
        public static List<string> Validate(ContentDatabase db, GameSettings settings)
        {
            var errors = new List<string>();
            Action<ContentRow, string, string> error = (r, field, reason) => errors.Add(r.Table + ":" + r.Line + " [" + field + "] " + reason);
            foreach (var table in CsvContent.Schema)
            {
                var rows = db.Table(table.Key).ToArray();
                if (rows.Length == 0) errors.Add(table.Key + ":1 缺少内容");
                foreach (var row in rows) foreach (var field in table.Value.Split(',')) if (!row.Headers.Contains(field)) error(row, field, "缺少表头");
            }
            foreach (var group in db.Rows.GroupBy(r => r.Id)) if (group.Key == "" || group.Count() != 1) error(group.First(), "id", "编号为空或重复：" + group.Key);
            if (settings == null || settings.Devices == null || settings.Characters == null || settings.Map == null || settings.Hotspots == null) { errors.Add("Config:0 缺少配置资产"); return errors; }
            var devices = settings.Devices.Devices;
            var chars = settings.Characters.Characters;
            var nodes = settings.Map.Nodes;
            var goals = db.Table("Event").Select(r => r.Get("goal")).ToHashSet();
            Func<string, bool> condition = id => goals.Contains(id) || db.Find(id)?.Table == "Puzzle" || db.Find(id)?.Table == "Entry";
            Action<ContentRow, string, string> reference = (r, field, table) => { foreach (var id in r.List(field)) if (db.Find(id)?.Table != table) error(r, field, "不存在的 " + table + " 引用 " + id); };
            Action<ContentRow, string> plot = (r, field) => { if (r.Get(field) != "" && db.Plot(r.Get(field)).Length == 0) error(r, field, "文本组不存在"); };
            foreach (var r in db.Rows)
            {
                foreach (var need in r.List("need")) if (need != "TAPE_ONLY" && !condition(need)) error(r, "need", "无效条件 " + need);
                if (r.Table == "Room" || r.Table == "Event" && new[] { "interact", "auto" }.Contains(r.Get("trigger")))
                {
                    try { if (ContentFormat.Minute(r.Get("start")) >= ContentFormat.Minute(r.Get("end"))) error(r, "start/end", "窗口必须为当日正区间"); }
                    catch (Exception ex) { error(r, "start/end", ex.Message); }
                }
                if (r.Table == "Event")
                {
                    if (!new[] { "interact", "auto", "reward", "flow" }.Contains(r.Get("trigger"))) error(r, "trigger", "未知触发类型");
                    if (!new[] { "none", "ending" }.Contains(r.Get("action"))) error(r, "action", "未知动作");
                    reference(r, "room", "Room"); reference(r, "record", "Record"); reference(r, "plan_b", "Event"); plot(r, "plot");
                    if (!r.Get("goal").StartsWith("G_")) error(r, "goal", "必须为 G_ 编号");
                    if (!int.TryParse(r.Get("time"), out int minutes) || minutes < 0) error(r, "time", "必须为非负整数分钟");
                    if (r.Get("trigger") == "reward" && (r.Get("need") != "" || r.Get("use") != "" || minutes != 0)) error(r, "trigger", "奖励事件必须无条件、无消耗、零耗时");
                    if (new[] { "interact", "auto" }.Contains(r.Get("trigger")) && r.Get("room") == "") error(r, "room", "必填");
                    if (r.Get("trigger") == "interact" && !settings.Hotspots.Hotspots.Any(h => h.Id == r.Get("target") && h.Room == r.Get("room") && h.Mode == "event")) error(r, "target", "缺少匹配房间的热点绑定");
                    foreach (var field in new[] { "reward", "use" }) foreach (var grant in r.List(field))
                    {
                        try
                        {
                            var pair = ContentFormat.Grant(grant); var entry = db.Find(pair.Key);
                            if (pair.Value <= 0 || entry?.Table != "Entry" || entry.Get("type") == "conclusion" || field == "use" && entry.Get("type") != "item") error(r, field, "数量或内容类型无效：" + grant);
                        }
                        catch { error(r, field, "无效奖励格式 " + grant); }
                    }
                    var backup = db.Find(r.Get("plan_b"));
                    if (backup != null && (backup.Get("goal") != r.Get("goal") || backup.Get("trigger") == "reward")) error(r, "plan_b", "备用必须同目标且不是奖励事件");
                    var visited = new HashSet<string> { r.Id }; var current = backup;
                    while (current != null) { if (!visited.Add(current.Id)) { error(r, "plan_b", "备用循环"); break; } current = db.Find(current.Get("plan_b")); }
                    if (r.Get("record") != "")
                    {
                        var recording = db.Find(r.Get("record"));
                        if (recording != null && (!devices.Any(d => d.Id == recording.Get("device") && d.Personal) || recording.Get("plot") != r.Get("plot"))) error(r, "record", "现场录音必须使用个人设备及同一文本组");
                    }
                }
                if (r.Table == "Plot")
                {
                    if (!bool.TryParse(r.Get("anchor"), out _)) error(r, "anchor", "必须为 true/false");
                    reference(r, "link_entry", "Entry");
                    foreach (var id in r.List("link_entry")) { var entry = db.Find(id); if (entry != null && (entry.Name == "" || !r.Get("text").Contains(entry.Name) || entry.Get("type") == "conclusion")) error(r, "link_entry", "正文缺少名称或关联最终推论"); }
                }
                if (r.Table == "Record")
                {
                    if (r.Get("plot") == "") error(r, "plot", "录音文本组必填");
                    plot(r, "plot");
                    if (r.Get("device") != "" && !devices.Any(d => d.Id == r.Get("device"))) error(r, "device", "设备不存在");
                    if (r.Get("start") != "") { try { ContentFormat.Date(r.Get("start")); } catch { error(r, "start", "日期格式错误"); } }
                    else if (!db.Table("Event").Any(e => e.Get("record") == r.Id) && r.Get("need") != "TAPE_ONLY") error(r, "start", "固定设备必须有报告时间");
                    var lines = db.Plot(r.Get("plot"));
                    if (r.Number("duration") <= 0 || lines.Length > 0 && r.Number("duration") <= lines.Last().Number("at")) error(r, "duration", "时长必须大于末句时间");
                    ValidateTimed(lines, error);
                }
                if (r.Table == "Entry")
                {
                    if (r.Get("text") == "") error(r, "text", "内容说明必填");
                    if (!new[] { "item", "tape", "clue", "info", "event", "conclusion" }.Contains(r.Get("type"))) error(r, "type", "未知内容类型");
                    reference(r, "record", "Record");
                    if (r.Get("record") != "" && r.Get("type") != "tape") error(r, "record", "仅磁带可配置");
                    foreach (var id in r.List("owner")) if (!chars.Any(c => c.Id == id)) error(r, "owner", "人物不存在");
                    if (r.Get("type") == "info" && r.List("owner").Length != 1) error(r, "owner", "人物信息恰好关联一人");
                    foreach (var id in r.List("source")) if (db.Find(id) == null || !new[] { "Event", "Plot", "Record", "Puzzle" }.Contains(db.Find(id).Table)) error(r, "source", "无效来源 " + id);
                }
                if (r.Table == "Puzzle") ValidatePuzzle(db, settings, r, error);
            }
            foreach (var group in db.Table("Plot").GroupBy(r => r.Get("group")))
            {
                var ordered = group.OrderBy(r => r.Number("order")).ToArray();
                for (int i = 0; i < ordered.Length; i++) if (ordered[i].Number("order") != i + 1) error(ordered[i], "order", "必须从1连续递增");
            }
            foreach (var e in db.Table("Event").Where(e => e.Get("action") == "ending")) ValidateTimed(db.Plot(e.Get("plot")), error);
            foreach (var group in db.Table("Event").Where(e => e.Get("plan_b") != "").GroupBy(e => e.Get("plan_b"))) if (group.Count() > 1) error(group.First(), "plan_b", "不能共用备用事件");
            foreach (var e in db.Table("Event").Where(e => e.Get("trigger") == "reward")) if (db.Table("Event").Count(o => o.Get("goal") == e.Get("goal")) > 1) error(e, "goal", "奖励目标必须独立");
            foreach (var id in new[] { settings.ArrivalRoom, settings.HomeRoom, settings.ControlRoom }) if (db.Find(id)?.Table != "Room") errors.Add("GameSettings:0 房间不存在 " + id);
            foreach (var id in new[] { settings.OpeningEvent, settings.ArrivalEvent, settings.TapeTutorialEvent }) if (db.Find(id)?.Table != "Event") errors.Add("GameSettings:0 入口事件不存在 " + id);
            if (db.Find(settings.MapEntry)?.Get("type") != "item" || db.Find(settings.TutorialRecord)?.Table != "Record") errors.Add("GameSettings:0 地图或引导录音不存在");
            if (!settings.TitleScene || !settings.ExteriorScene || settings.Map.Floors.Any(f => !f.Background)) errors.Add("GameSettings:0 缺少入口或地图图像");
            foreach (var room in db.Table("Room")) if (!settings.Map.Rooms.Any(b => b.Room == room.Id && b.Background)) error(room, "scene", "缺少 Unity 房间表现绑定");
            foreach (var d in devices) { if (db.Find(d.Room)?.Table != "Room") errors.Add("DeviceConfig:0 无效房间 " + d.Id); foreach (var p in d.Proof) if (!condition(p)) errors.Add("DeviceConfig:0 无效 proof " + p); }
            foreach (var node in nodes) if (db.Find(node.Room)?.Table != "Room" || !settings.Map.Floors.Any(f => f.Floor == node.Floor)) errors.Add("MapConfig:0 无效地图节点 " + node.Id);
            foreach (var ids in new[] { devices.Select(d => d.Id), chars.Select(c => c.Id), nodes.Select(n => n.Id), settings.Hotspots.Hotspots.Select(h => h.Id) }) if (ids.Distinct().Count() != ids.Count()) errors.Add("Config:0 重复编号");
            foreach (var room in db.Table("Room")) if (!int.TryParse(room.Get("floor"), out _)) error(room, "floor", "楼层必须为整数");
            foreach (var group in db.Table("Record").GroupBy(r => r.Get("plot")))
                if (group.Count() > 1 && db.Plot(group.Key).Any(l => l.Get("anchor") == "true")) error(group.First(), "plot", "含锚点文本组不可被多个文件共用；各文件必须有独立行 ID");
            foreach (var p in db.Table("Puzzle").Where(p => p.Get("type") == "anchor"))
            {
                var groups = ContentFormat.Answers(p).Select(id => db.Find(id)?.Get("group")).ToArray();
                if (groups.Distinct().Count() < 2) error(p, "answer", "匹配须来自至少两个不同文件");
            }
            foreach (var hotspot in settings.Hotspots.Hotspots)
                if (db.Find(hotspot.Room)?.Table != "Room" || hotspot.Mode == "puzzle" && db.Find(hotspot.PuzzleId)?.Table != "Puzzle") errors.Add("HotspotBinding:0 无效房间/判定引用 " + hotspot.Id);
            try { ContentFormat.Date(settings.InitialTime); ContentFormat.Minute(settings.WakeTime); } catch (Exception ex) { errors.Add("GameSettings:0 " + ex.Message); }
            if (settings.ControlMinutes < 0 || settings.CalibrationStep <= 0 || settings.CalibrationTolerance < 0 || settings.DialogueHold <= 0 || settings.EndingHold <= 0) errors.Add("GameSettings:0 时间/容差参数无效");
            ValidateReachability(db, settings, errors);
            return errors;
        }

        static void ValidateTimed(ContentRow[] lines, Action<ContentRow, string, string> error)
        {
            float previous = -1;
            foreach (var line in lines)
            {
                if (!float.TryParse(line.Get("at"), NumberStyles.Float, CultureInfo.InvariantCulture, out float at) || float.IsNaN(at) || float.IsInfinity(at) || at <= previous || previous == -1 && at != 0) error(line, "at", "录音/结局必须从0严格递增");
                previous = at;
            }
        }
        static void ValidatePuzzle(ContentDatabase db, GameSettings settings, ContentRow r, Action<ContentRow, string, string> error)
        {
            if (r.Get("fail_text") == "") error(r, "fail_text", "失败反馈必填");
            string type = r.Get("type"); var candidates = r.List("candidates"); var answers = ContentFormat.Answers(r); var slots = r.List("slots");
            if (!new[] { "anchor", "calibrate", "location", "person_path", "timeline", "conclusion" }.Contains(type)) error(r, "type", "未知题型");
            if (db.Find(r.Get("success"))?.Get("trigger") != "reward") error(r, "success", "必须引用奖励事件");
            if (db.Table("Puzzle").Count(p => p.Get("success") == r.Get("success")) > 1) error(r, "success", "奖励事件不能被多个判定共享");
            if (answers.Any(a => !candidates.Contains(a))) error(r, "answer", "答案不在候选集合");
            if (candidates.Distinct().Count() != candidates.Length || slots.Distinct().Count() != slots.Length) error(r, "candidates/slots", "重复候选或标签");
            if (type == "person_path" || type == "timeline")
            {
                if (slots.Length == 0 || r.List("answer").Length != slots.Length || !r.List("answer").Select(a => a.Split('=')[0]).SequenceEqual(slots)) error(r, "answer", "必须按全部槽位配置标签=编号");
                if (type == "timeline" && answers.Distinct().Count() != answers.Length) error(r, "answer", "时间线不能重复事件卡");
            }
            if (type == "location" || type == "calibrate")
            {
                if (!settings.Devices.Devices.Any(d => d.Id == r.Get("target")) || r.Get("answer") != "") error(r, "target/answer", "设备需存在且答案留空");
            }
            if ((type == "person_path" || type == "location") && r.Get("map") != settings.Map.Id) error(r, "map", "地图不存在");
            foreach (var c in candidates)
            {
                bool valid = type == "person_path" ? settings.Map.Nodes.Any(n => n.Id == c) : type == "location" ? db.Find(c)?.Table == "Room" : type == "anchor" || type == "calibrate" ? db.Find(c)?.Get("anchor") == "true" && db.Table("Record").Any(rec => rec.Get("plot") == db.Find(c).Get("group")) : type == "timeline" ? db.Find(c)?.Get("type") == "event" : db.Find(c)?.Get("type") == "conclusion";
                if (!valid) error(r, "candidates", "候选类型错误 " + c);
            }
            if (type == "anchor" && (answers.Length < 2 || answers.Distinct().Count() != answers.Length)) error(r, "answer", "完整锚点集合至少两个不同录音声响");
            if (type == "calibrate" && (r.List("reference").Length == 0 || r.List("reference").Any(id => id == r.Get("target") || !settings.Devices.Devices.Any(d => d.Id == id)))) error(r, "reference", "必须配置其他参照设备");
            if (type == "person_path" && (!settings.Characters.Characters.Any(c => c.Id == r.Get("target")) || !r.List("need").Any(id => db.Find(id)?.Get("type") == "info"))) error(r, "need/target", "人物路径必须指定人物及所需信息");
            if (type == "conclusion" && (candidates.Length < 2 || candidates.Length > 3 || answers.Length != 1 || db.Find(r.Get("target"))?.Get("type") != "timeline")) error(r, "target/candidates", "最终推论需2—3选项和前置时间线");
            if (r.Get("feedback") != "" && (type != "timeline" || db.Plot(r.Get("feedback")).Length == 0 || db.Plot(r.Get("feedback")).Length > slots.Length - 1)) error(r, "feedback", "关系文字超过相邻槽位数或文本组不存在");
        }
        // 单调固定点检测明显依赖死锁；不声称穷举所有时间/消耗路径。
        static void ValidateReachability(ContentDatabase db, GameSettings settings, List<string> errors)
        {
            var known = new HashSet<string>(settings.InitialItems.Select(v => ContentFormat.Grant(v).Key));
            bool changed;
            do
            {
                int count = known.Count;
                foreach (var e in db.Table("Event").Where(e => e.Get("trigger") != "reward" && e.List("need").All(known.Contains)))
                {
                    known.Add(e.Get("goal"));
                    foreach (var grant in e.List("reward")) known.Add(ContentFormat.Grant(grant).Key);
                    foreach (var line in db.Plot(e.Get("plot"))) foreach (var id in line.List("link_entry")) known.Add(id);
                }
                foreach (var entry in db.Table("Entry").Where(e => new[] { "clue", "info" }.Contains(e.Get("type")) && e.List("source").Any(id => db.Find(id)?.Table == "Plot"))) known.Add(entry.Id);
                foreach (var p in db.Table("Puzzle").Where(p => p.List("need").All(known.Contains)))
                {
                    known.Add(p.Id); var reward = db.Find(p.Get("success")); if (reward == null) continue;
                    known.Add(reward.Get("goal")); foreach (var grant in reward.List("reward")) known.Add(ContentFormat.Grant(grant).Key);
                }
                changed = count != known.Count;
            } while (changed);
            foreach (var p in db.Table("Puzzle")) if (!known.Contains(p.Id)) errors.Add("Puzzle:" + p.Line + " [need] 无可达的前置路径（可能循环）");
        }
    }
}

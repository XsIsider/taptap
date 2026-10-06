using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Castle.V2;
using UnityEditor;
using UnityEngine;
namespace Castle.Editor
{
    public static class PrototypeValidation
    {
        [Serializable] public sealed class ContentSnapshot { public List<ContentRow> Rows; }
        static readonly List<string> Results = new List<string>();
        static void Check(bool condition, string label) { if (!condition) throw new Exception("FAIL " + label); Results.Add("PASS " + label); }
        static void Finish(SessionService session)
        {
            session.Events.CompleteLastLine(); Check(session.Events.Continue(), "事件结算 " + session.State.WorldTime);
        }
        static SessionService Reload(SessionService s) => new SessionService(s.Content, s.Settings, new SaveService(null), JsonUtility.FromJson<SessionSave>(JsonUtility.ToJson(s.State)));
        public static void Run()
        {
            Results.Clear();
            var db = AssetDatabase.LoadAssetAtPath<ContentDatabase>(ContentImporter.DatabasePath); var settings = AssetDatabase.LoadAssetAtPath<GameSettings>(ContentImporter.SettingsPath);
            Check(ContentValidator.Validate(db, settings).Count == 0, "六表全量引用校验");
            var csv = CsvContent.Parse("Test", "id,text\r\na,\"comma, quote \"\"ok\"\"\nnewline\"\r\n"); Check(csv.Count == 1 && csv[0].Get("text") == "comma, quote \"ok\"\nnewline", "CSV 引号、逗号、换行");
            var s = new SessionService(db, settings, new SaveService(null)); s.Start(); Finish(s);
            Check(s.State.Active.Id == "E_ARRIVE", "开场自动接待"); s.Events.CompleteLastLine(); Finish(s);
            Check(!s.Has("I_MAP"), "地图只能点击链接领取");
            Check(s.Events.Begin("E_ARRIVE", true, true), "已完成对白回看");
            Check(s.Inventory.Claim("L_ARRIVE_1", "I_MAP"), "链接补领"); string before = s.State.WorldTime; Finish(s);
            Check(s.State.WorldTime == before && s.Inventory.Count("I_MAP") == 1 && s.State.Tapes.Count == 2, "1/2 回看与重复领取不重复奖励计时");
            s.Travel("R_HOME"); Check(s.State.Active.Id == "E_TUTORIAL", "房间引导");
            s.Events.CompleteLastLine(); Check(!s.Events.Continue(), "17 未播放指定文件不能完成引导");
            var tape = s.State.Tapes.First(t => t.RecordId == "REC_TAPE"); s.Recordings.Seek("REC_TAPE", 10, tape.Id); Finish(s);
            Check(s.Has("G_TUTORIAL"), "17 指定磁带播放结束后引导完成");
            s.Travel("R_STUDY"); s.Events.Begin("E_LIVE"); Check(s.State.Personal.Count == 0, "18 现场录音不提前收录");
            string started = s.State.WorldTime; s.Events.CompleteLastLine(); s = Reload(s);
            Check(s.State.Personal.Single().Started == started && !s.Has("G_LIVE") && s.State.Active.LastLineDone, "18/中断恢复 末句已录音、未结算"); Finish(s);
            s.Events.Begin("E_MAINT"); Finish(s);
            s.Travel("R_CONTROL"); before = s.State.WorldTime; s = Reload(s);
            Check(s.State.ControlVisit && s.State.Personal.Single().Imported == before, "6/18 中控室恢复与增量导入");
            Check(s.Recordings.CanAccess("REC_A") && s.Recordings.CanAccess("REC_PERSONAL") && !s.Recordings.CanAccess("REC_TAPE"), "8 中央、个人、磁带分类权限");
            var blank = s.State.Tapes.First(t => t.RecordId == ""); Check(s.Recordings.Burn(blank.Id, "REC_B", false), "7 白带刻录");
            Check(!s.Recordings.Burn(tape.Id, "REC_A", false) && s.State.Tapes.First(t => t.Id == tape.Id).RecordId == "REC_TAPE", "7 取消覆盖不修改");
            s = Reload(s); Check(s.State.Tapes.Count == 2 && s.State.Tapes.First(t => t.Id == blank.Id).RecordId == "REC_B", "7 刻录后恢复、实例隔离");
            foreach (var id in new[] { "REC_A", "REC_B", "REC_C" }) { s.Recordings.Seek(id, 10); s.Recordings.Mark(s.Content.Plot(s.Content.Require(id).Get("plot"))[0].Id); }
            s.Puzzles.Edit("Q_ANCHOR", new[] { "L_A_1", "L_C_1" }); Check(s.Puzzles.Submit("Q_ANCHOR") != null, "9 排除第二次同类声响");
            s.Puzzles.Edit("Q_ANCHOR", new[] { "L_B_1", "L_A_1" }); var anchorError = s.Puzzles.Submit("Q_ANCHOR"); Check(anchorError == null, "9 同次声响无序完整集合: " + anchorError + " marked=" + string.Join(",", s.State.MarkedLines) + " active=" + s.State.Active?.Id);
            s = Reload(s); Check(!s.State.Solved.Contains("Q_ANCHOR") && s.State.PendingPuzzle == "Q_ANCHOR", "13 待领奖中断恢复"); Finish(s); Check(s.Has("V_RETURN") && s.Has("Q_ANCHOR"), "13/14 奖励后才 Solved 和归档");
            s.Puzzles.Edit("Q_LOCATION", new[] { "R_STUDY" }); Check(s.Puzzles.Submit("Q_LOCATION") == null, "位置验证"); Finish(s);
            s.Puzzles.Edit("Q_CALIBRATE", new[] { "L_A_1", "L_B_1" }, -180, "D_A"); Check(s.Puzzles.Submit("Q_CALIBRATE") != null, "11 拒绝错误校时方向");
            s.Puzzles.Edit("Q_CALIBRATE", new[] { "L_A_1", "L_B_1" }, 180, "D_C"); Check(s.Puzzles.Submit("Q_CALIBRATE") != null, "10 无可靠证据参照拒绝");
            s.Puzzles.Edit("Q_CALIBRATE", new[] { "L_A_1", "L_B_1" }, 180, "D_A"); Check(s.Puzzles.Submit("Q_CALIBRATE") == null, "10/11 有依据参照及 +180 秒"); Finish(s);
            Check(s.Knowledge("D_B").Offset == -180 && s.Recordings.TimeAt("REC_B", 0, true) == s.Recordings.TimeAt("REC_A", 0, true), "11 减去偏差统一时间模型");
            s.Travel("R_HALL"); Check(s.Now == ContentFormat.Date(before).AddMinutes(15), "6 中控室离开只计时一次");
            Check(s.State.BackupRoutes.Contains("E_BACK") && s.Events.Choices("HS_DESK").Single().Id == "E_BACK", "4 跨关闭窗口启用备用");
            s.Events.Begin("E_BACK"); Finish(s);
            s.Puzzles.Edit("Q_PATH", new[] { "N_STUDY", "N_HALL", "N_STUDY" }); Check(s.Puzzles.Submit("Q_PATH") != null, "12 错误路径保留"); s = Reload(s); Check(s.Puzzles.Draft("Q_PATH").Values[0] == "N_STUDY", "12 草稿跨读档");
            s.Puzzles.Edit("Q_PATH", new[] { "N_HALL", "N_STUDY", "N_HALL" }); Check(s.Puzzles.Submit("Q_PATH") == null, "12 同点再经过"); Finish(s);
            s.Puzzles.Edit("Q_TIMELINE", new[] { "V_RETURN", "V_STUDY", "V_REGISTER" }); Check(s.Puzzles.Submit("Q_TIMELINE") != null, "15 时间线错误保留");
            s.Puzzles.Edit("Q_TIMELINE", new[] { "V_REGISTER", "V_STUDY", "V_RETURN" }); Check(s.Puzzles.Submit("Q_TIMELINE") == null, "15 三卡时间线"); Finish(s);
            s.Puzzles.Edit("Q_CONCLUSION", new[] { "K_FALSE" }); Check(s.Puzzles.Submit("Q_CONCLUSION") != null, "15 错误推论可重选");
            s.Puzzles.Edit("Q_CONCLUSION", new[] { "K_TRUE" }); Check(s.Puzzles.Submit("Q_CONCLUSION") == null, "15 正确推论进入演出"); s = Reload(s); Check(s.State.EndingStage == "playing", "结局播放中断恢复"); s.Events.SkipEnding(); Check(s.State.EndingStage == "complete", "15 跳过仍只结算一次");
            s.Travel("R_HOME"); s.Rest(); Check(s.Now.Hour == 8 && s.Now.Day == 8, "5 次日起床");
            var room = db.Require("R_STUDY"); Check(EventService.InWindow(room, s.Now) && !EventService.InWindow(room, s.Now.Date.AddHours(22)), "3 开始包含、结束排除");
            Check(!s.Recordings.CanAccess("REC_A") && s.Recordings.CanAccess("REC_B", blank.Id), "8 房间仅持有磁带可播放");
            var json = JsonUtility.ToJson(s.State); Check(json.Contains("N_HALL") && json.Contains("-180"), "存档稳定ID与校正数据可序列化");
            // 次日补做与跨时窗完成：使用全新会话，避免已完成目标遮蔽条件。
            var day = new SessionService(db, settings, new SaveService(null));
            day.State.Started = true; day.State.Goals.AddRange(new[] { "G_INVITE", "G_ARRIVE", "G_TUTORIAL" }); day.Inventory.Grant("I_MAP");
            day.State.Room = "R_STUDY"; day.State.WorldTime = "2026-10-07 21:59:00";
            Check(day.Events.Begin("E_LIVE"), "3 关闭前可开始"); Finish(day);
            Check(day.Has("G_LIVE") && day.State.Room == settings.HomeRoom && day.State.Personal.Count == 1, "3 已开始交互安全结束再离开关闭房间");
            day.Rest(); day.Travel("R_STUDY"); Check(day.Events.Available(db.Require("E_MAINT")), "5 次日未完成事件重新开放");
            var waitBefore = day.Now; day.Travel("R_HOME"); Check(day.Now == waitBefore, "移除固定移动两分钟规则");
            Check(!day.Puzzles.Available(db.Require("Q_PATH")), "12 显式人物信息不足时关闭路径");
            var copy = UnityEngine.Object.Instantiate(settings); copy.KeepRecords = false;
            var history = new SessionService(db, copy, new SaveService(null), JsonUtility.FromJson<SessionSave>(JsonUtility.ToJson(s.State)));
            history.State.Room = settings.ControlRoom; history.State.ControlVisit = true;
            Check(!history.Recordings.CanAccess("REC_A") && history.Recordings.CanAccess("REC_PERSONAL"), "8 关闭历史固定文件仍保留个人文件");
            history.State.Room = settings.HomeRoom; history.State.ControlVisit = false;
            Check(history.Recordings.CanAccess("REC_B", blank.Id), "8 历史中央关闭仍保留磁带"); UnityEngine.Object.DestroyImmediate(copy);
            string savePath = Path.GetFullPath("Logs/castle-v2-test-save.json"); var disk = new SaveService(savePath);
            Check(disk.Write(s.State) && disk.Write(s.State), "原子临时文件与备份写入");
            File.WriteAllText(savePath, "broken"); var recovered = disk.Load(db.Version);
            Check(recovered != null && recovered.Solved.Contains("Q_CONCLUSION"), "损坏主存档回退备份");
            var original = JsonUtility.ToJson(db); string plotPath = "Assets/Castle/Content/CSV/Plot.csv"; string originalPlot = File.ReadAllText(plotPath);
            try
            {
                File.WriteAllText(plotPath, originalPlot.Replace("L_A_1,", "L_B_1,")); bool rejected = false;
                try { ContentImporter.ImportPlot(); } catch (InvalidOperationException) { rejected = true; }
                Check(rejected && JsonUtility.ToJson(db) == original, "Plot单表失败保留有效数据库");
            }
            finally { File.WriteAllText(plotPath, originalPlot); }
            ContentImporter.ImportPlot(); Check(ContentValidator.Validate(db, settings).Count == 0 && db.Rows.Count == JsonUtility.FromJson<ContentSnapshot>(original).Rows.Count, "Plot独立重导入全量引用校验");
            var bad = ScriptableObject.CreateInstance<ContentDatabase>(); bad.Rows = db.Rows.Select(r => new ContentRow { Table = r.Table, Line = r.Line, Headers = r.Headers, Values = r.Values.ToArray() }).ToList();
            var back = bad.Require("E_BACK"); back.Values[Array.IndexOf(back.Headers, "plan_b")] = "E_MISS";
            Check(ContentValidator.Validate(bad, settings).Any(e => e.Contains("备用循环")), "Plan B 环检测"); UnityEngine.Object.DestroyImmediate(bad);
            before = s.State.WorldTime; Check(s.Events.Begin("E_END", true, true), "15 已完成结局可重播"); s.Events.SkipEnding();
            Check(s.State.WorldTime == before && s.State.Solved.Count == 6, "15 结局回放不再次结算");
            Directory.CreateDirectory("Logs"); File.WriteAllLines("Logs/castle-v2-validation.txt", Results); Debug.Log("CASTLE V2 VALIDATION PASSED " + Results.Count);
        }
    }
}

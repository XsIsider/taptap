using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace Castle.V2
{
    public static class PrototypeSmoke
    {
        static string _folder;
        static Camera _camera;
        static RenderTexture _target;
        static void Click(string label)
        {
            var button = UnityEngine.Object.FindObjectsOfType<Button>().FirstOrDefault(b => b.gameObject.activeInHierarchy && b.interactable && b.name.Contains(label));
            if (button == null) throw new Exception("Smoke button not found: " + label);
            button.onClick.Invoke();
        }
        static void Finish(SessionService session) { session.Events.CompleteLastLine(); if (!session.Events.Continue()) throw new Exception("Unable to finish " + session.State.Active?.Id); }
        static void Solve(SessionService session, string id, string[] values, float shift = 0, string reference = null)
        { session.Puzzles.Edit(id, values, shift, reference); var error = session.Puzzles.Submit(id); if (error != null) throw new Exception(id + " " + error); Finish(session); }
        static IEnumerator Capture(string name)
        {
            yield return null; Canvas.ForceUpdateCanvases(); _camera.Render();
            var previous = RenderTexture.active; RenderTexture.active = _target;
            var image = new Texture2D(_target.width, _target.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, _target.width, _target.height), 0, 0); image.Apply();
            File.WriteAllBytes(Path.Combine(_folder, name + ".png"), image.EncodeToPNG()); UnityEngine.Object.Destroy(image); RenderTexture.active = previous;
        }
        public static IEnumerator Run(CastleGame host, PrototypeUi ui)
        {
            Application.runInBackground = true;
            // 隐藏窗口不提交交换链；仅测试进程使用离屏相机检查相同 UI 层级。
            UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline = null; QualitySettings.renderPipeline = null;
            _target = new RenderTexture(Screen.width, Screen.height, 24); _camera = Camera.main; _camera.enabled = false; _camera.targetTexture = _target;
            var canvas = host.GetComponentInChildren<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = _camera; canvas.planeDistance = 1;
            _folder = Path.Combine(Application.dataPath, "..", "Screenshots", Screen.width + "x" + Screen.height); Directory.CreateDirectory(_folder);
            Application.logMessageReceived += (message, trace, type) => { if (type == LogType.Exception || type == LogType.Error) File.AppendAllText(Path.Combine(_folder, "errors.txt"), message + "\n" + trace + "\n"); };
            yield return new WaitForSecondsRealtime(.5f); yield return Capture("01-title"); Click("设置"); yield return Capture("01a-settings"); Click("关闭 / 取消");
            Click("制作名单"); yield return Capture("01b-credits"); Click("关闭 / 取消");
            Click("开始调查"); yield return Capture("01c-outside");
            Click("阅读邀请函"); yield return Capture("01d-invitation");
            Click("收好邀请函"); yield return Capture("01e-collected"); Click("进入古堡");
            var s = host.Session;
            yield return new WaitForSecondsRealtime(2.3f); yield return Capture("02-dialogue");
            Finish(s); ui.Resume(); s.Inventory.Claim("L_ARRIVE_1", "I_MAP"); yield return Capture("03-map-link"); Finish(s); ui.Resume(); yield return Capture("03a-room");
            Click("地图 M"); yield return Capture("03b-map"); Click("2 楼"); yield return Capture("03c-map-floor2"); Click("收起地图");
            Click("调查册 J"); yield return Capture("03d-journal"); Click("返回");
            s.Travel("R_HOME"); ui.Resume(); yield return Capture("04-tutorial"); Click("打开房间放音机"); yield return null; Click("剧情磁带"); Click("播放 / 暂停");
            yield return new WaitForSecondsRealtime(10.5f); yield return Capture("05-playback");
            Finish(s); s.Travel("R_STUDY"); s.Events.Begin("E_LIVE"); Finish(s); s.Events.Begin("E_MAINT"); Finish(s); s.Travel("R_CONTROL"); ui.Resume(); Click("中央档案"); yield return Capture("06-records");
            Click("调查册 J"); Click("人物"); yield return Capture("06a-characters"); Click("访客"); yield return Capture("06b-character-card"); Click("关闭 / 取消"); Click("返回");
            Click("锚点与校时"); yield return Capture("06c-analysis"); Click("返回");
            foreach (var id in new[] { "REC_A", "REC_B", "REC_C" }) { s.Recordings.Seek(id, 10); s.Recordings.Mark(s.Content.Plot(s.Content.Require(id).Get("plot"))[0].Id); }
            ui.ShowPlayback("REC_B"); yield return Capture("07-device-hidden");
            Solve(s, "Q_ANCHOR", new[] { "L_A_1", "L_B_1" }); Solve(s, "Q_LOCATION", new[] { "R_STUDY" });
            ui.ShowPuzzle("Q_CALIBRATE"); yield return Capture("08-calibration"); Solve(s, "Q_CALIBRATE", new[] { "L_A_1", "L_B_1" }, 180, "D_A");
            s.Travel("R_HALL"); s.Events.Begin("E_BACK"); Finish(s);
            ui.ShowPuzzle("Q_PATH"); yield return Capture("09-path"); Solve(s, "Q_PATH", new[] { "N_HALL", "N_STUDY", "N_HALL" });
            ui.ShowPuzzle("Q_TIMELINE"); yield return null;
            Click("大厅登记"); yield return null; Click("S2"); yield return null; Click("书房停留"); yield return null; Click("S3"); yield return null; Click("返回大厅"); yield return null;
            // 用与拖放同一 DropZone 回调验证交换，再交换回正确顺序。
            var first = UnityEngine.Object.FindObjectsOfType<DropZone>().First(z => z.name.Contains("S1"));
            var third = UnityEngine.Object.FindObjectsOfType<DragToken>().First(t => t.name.Contains("S3")); first.Drop(third); yield return null;
            if (s.Puzzles.Draft("Q_TIMELINE").Values[0] != "V_RETURN") throw new Exception("Timeline drop swap failed");
            first = UnityEngine.Object.FindObjectsOfType<DropZone>().First(z => z.name.Contains("S1"));
            third = UnityEngine.Object.FindObjectsOfType<DragToken>().First(t => t.name.Contains("S3")); first.Drop(third); yield return null;
            yield return Capture("10-timeline"); Click("提交判断"); yield return null; Finish(s);
            ui.ShowPuzzle("Q_CONCLUSION"); yield return Capture("11-conclusion");
            s.Puzzles.Edit("Q_CONCLUSION", new[] { "K_TRUE" }); if (s.Puzzles.Submit("Q_CONCLUSION") != null) throw new Exception("Conclusion failed"); ui.Resume(); yield return new WaitForSecondsRealtime(3.2f); yield return Capture("12-ending");
            s.Events.SkipEnding(); ui.Resume();
            File.WriteAllText(Path.Combine(_folder, "result.txt"), "PASS full example + UI timeline swaps + playback + screenshots; " + s.State.Solved.Count + " puzzles solved");
            Debug.Log("CASTLE V2 PLAYER SMOKE PASSED"); Application.Quit(0);
        }
    }
}

using System;
using System.IO;
using System.Linq;
using Castle.Flow;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Castle.Editor
{
    public static class CastleFlowValidation
    {
        private static int _checks;

        [MenuItem("Tools/Castle/Validate Confirmed UI Flow")]
        public static void Validate()
        {
            _checks = 0;
            var content = AssetDatabase.LoadAssetAtPath<CastleFlowContent>(CastleFlowSceneBuilder.ContentPath);
            foreach (var record in content.Recordings.Where(record => string.IsNullOrEmpty(record.DeviceName)))
                record.DeviceName = "录音设备 " + record.DeviceId.Substring(record.DeviceId.Length - 1).ToUpperInvariant();
            EditorUtility.SetDirty(content); AssetDatabase.SaveAssets();
            content.ValidateContent();
            var state = CastleFlowSession.New(content);
            var opening = content.Dialogue(content.OpeningDialogue);
            state.ActiveDialogue = opening.Id;
            state.Reveal(opening, 0);
            Check(!state.Unlocked.Contains(content.ControlRoom), "未显示的对白不提前解锁");
            state.Reveal(opening, 1);
            Check(state.PendingClaims.Contains(content.MapId) && !state.Entries.Contains(content.MapId), "赠送词登记补领而非自动直接领取");
            state.Collect(content.MapId); Check(!state.Collect(content.MapId), "道具去重");
            state.Reveal(opening, 2);
            Check(state.Unlocked.Contains(content.ControlRoom), "句子出现即解锁地点");
            Check(!state.CanEnter(content, content.ControlRoom), "对白中禁止移动");
            state.ActiveDialogue = null;
            Check(!state.CanEnter(content, content.ControlRoom), "未达开放时间禁止移动");
            state.Minutes = 1140;
            Check(state.CanEnter(content, content.ControlRoom), "开放后允许选中进入");
            state.Unlocked.Add("105"); Check(!state.CanEnter(content,"105"), "可见与可导航独立");
            foreach (var puzzle in content.Puzzles)
            {
                var draft = state.Draft(puzzle.Id);
                Check(!state.Align(content,puzzle) && !state.VerifySpace(content,puzzle), "空草稿不可验证");
                foreach (string id in puzzle.RecordingIds) draft.Tracks.Add(new TrackDraft { RecordingId = id });
                Check(state.Match(puzzle), "完整候选可匹配");
                Check(!state.Align(content,puzzle), "无参考不可校准");
                foreach (var reference in puzzle.RecordingIds)
                {
                    state.ChooseReference(draft, reference);
                    float origin = content.Recording(reference).StartSeconds;
                    foreach (var track in draft.Tracks) track.ShiftSeconds = origin - content.Recording(track.RecordingId).StartSeconds;
                    Check(state.Align(content,puzzle), "玩家可任选参考：" + reference);
                }
                Check(!state.VerifySpace(content,puzzle), "只对齐未完成空间不可收录");
                foreach (var record in content.Recordings)
                {
                    var location = state.Devices.FirstOrDefault(item => item.Id == record.DeviceId);
                    if (location == null) { location = new DeviceLocation { Id = record.DeviceId }; state.Devices.Add(location); }
                    location.RoomId = record.RoomId;
                }
                Check(state.VerifySpace(content,puzzle) && state.Entries.Contains(puzzle.ClueId), "锚点与空间正确产生唯一线索");
                int count = state.Entries.Count; state.VerifySpace(content,puzzle); Check(count == state.Entries.Count, "重复验证不重复奖励");
                draft.Tracks[0].ShiftSeconds += 10;
                Check(!state.Align(content,puzzle) && state.Entries.Contains(puzzle.ClueId), "草稿改变不抹除已收录线索");
                state.Devices.Clear();
            }
            state.Timeline = content.EndingOrder.Reverse().ToList();
            Check(!state.SubmitTimeline(content) && state.Timeline.SequenceEqual(content.EndingOrder.Reverse()), "错误排序保留草稿");
            state.Timeline = content.EndingOrder.ToList();
            Check(state.SubmitTimeline(content) && state.EndingSubmitted, "正确提交直接进入揭露状态，无结论选择");
            string path = System.IO.Path.GetFullPath("Logs/flow-test-save.json");
            Check(CastleFlowSave.Write(state,path,out _), "隔离存档写入");
            var restored = CastleFlowSave.Load(path,out var error);
            Check(error == null && restored.EndingSubmitted && restored.Entries.SequenceEqual(state.Entries), "存档往返");
            EditorSceneManager.OpenScene(CastleFlowSceneBuilder.ScenePath);
            var game = UnityEngine.Object.FindObjectOfType<CastleFlowController>();
            Check(game && game.Ui && game.Content, "场景实际绑定新控制器"); game.Ui.ValidateBindings();
            Check(!UnityEngine.Object.FindObjectOfType<CastleGame>(), "旧控制器停用，不争用输入与存档");
            Check(game.Ui.Pages.Length == 7 && game.Ui.Modals.Length == 6, "固定页面与弹窗已序列化");
            ExportBindings(game.Ui);
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/castle-flow-validation.txt", "PASS " + _checks + " checks\n" + DateTime.UtcNow.ToString("O"));
            Debug.Log("CASTLE FLOW VALIDATION PASS: " + _checks);
        }

        private static void Check(bool condition,string message)
        {
            _checks++;
            if (!condition) throw new InvalidOperationException("UI FLOW CHECK FAILED: " + message);
        }

        private static void ExportBindings(CastleFlowUi ui)
        {
            var text = new System.Text.StringBuilder("# UI 对象树与实际绑定清单\n\n由场景验证工具从 CastleEditable 的真实序列化引用导出。总体对象树、玩法及修改约定见根目录《功能与UI实现说明》。\n\n固定页面不在运行时创建；表中路径相对各页面根对象。控件 ID 必须保持稳定，Hierarchy 名称可调整。按钮由 CastleFlowController 绑定，不必在 On Click 中重复挂方法。\n\n");
            foreach (var view in ui.Pages.Concat(ui.Modals).Concat(new[] { ui.Hud, ui.Toast, ui.CardPrefab, ui.TrackPrefab, ui.LinePrefab }))
            {
                text.Append("## ").Append(view.Id).Append("\n\n挂载脚本：CastleUiView。\n\n| 控件 ID | 所需组件 | 对象路径 |\n|---|---|---|\n");
                foreach (var binding in view.Controls)
                {
                    var node = binding.Target.transform; string path = node.name;
                    while (node.parent && node != view.transform && node.parent != view.transform) { node = node.parent; path = node.name + "/" + path; }
                    text.Append('|').Append(binding.Id).Append('|').Append(binding.Target.GetType().Name).Append('|').Append(path).Append("|\n");
                }
                text.Append('\n');
            }
            File.WriteAllText("UI对象树与绑定清单.md", text.ToString(), new System.Text.UTF8Encoding(false));
        }

        public static void InstallAndValidate() { CastleFlowSceneBuilder.Install(); Validate(); }

        // 本轮首次生成后的布局修正；显式运行，不在 Validate 内改写设计师场景。
        public static void RefineAndBuild()
        {
            var scene = EditorSceneManager.OpenScene(CastleFlowSceneBuilder.ScenePath);
            var ui = UnityEngine.Object.FindObjectOfType<CastleFlowUi>();
            foreach (var bar in ui.GetComponentsInChildren<UnityEngine.UI.Scrollbar>(true))
            {
                bar.handleRect.sizeDelta = Vector2.zero;
                bar.handleRect.anchoredPosition = Vector2.zero;
            }
            FitButtonLabels(ui.gameObject);
            var prefab = PrefabUtility.LoadPrefabContents("Assets/Castle/Prefabs/Flow/FlowTrack.prefab");
            FitButtonLabels(prefab);
            PrefabUtility.SaveAsPrefabAsset(prefab,"Assets/Castle/Prefabs/Flow/FlowTrack.prefab");
            PrefabUtility.UnloadPrefabContents(prefab);
            foreach (string id in new[] { "Map", "Space" })
            {
                var view = ui.View(id);
                view.Get<UnityEngine.UI.RawImage>("FloorImage").uvRect = new Rect(210f/1672,181f/941,1280f/1672,600f/941);
                if (view.Controls.Any(binding => binding.Id == "TooltipPanel")) continue;
                var text = view.Get<UnityEngine.UI.Text>("Tooltip");
                var panel = new GameObject("TooltipPanel",typeof(RectTransform),typeof(UnityEngine.UI.Image)).GetComponent<RectTransform>();
                panel.SetParent(view.transform,false); panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0,1);
                panel.anchoredPosition = text.rectTransform.anchoredPosition + new Vector2(0,15); panel.sizeDelta = text.rectTransform.sizeDelta + new Vector2(0,15);
                var image = panel.GetComponent<UnityEngine.UI.Image>(); image.color = new Color32(24,35,29,250); image.raycastTarget = false;
                text.transform.SetParent(panel,false); text.rectTransform.anchoredPosition = new Vector2(14,-12); text.rectTransform.sizeDelta = panel.sizeDelta - new Vector2(28,24); text.gameObject.SetActive(true);
                panel.gameObject.SetActive(false); view.Controls.Add(new CastleUiBinding { Id = "TooltipPanel", Target = panel });
            }
            EditorSceneManager.SaveScene(scene);
            Build();
        }

        private static void FitButtonLabels(GameObject root)
        {
            foreach (var button in root.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                var label = button.GetComponentInChildren<UnityEngine.UI.Text>(true);
                if (label) label.fontSize = Mathf.Min(label.fontSize,Mathf.FloorToInt((label.rectTransform.rect.height)/1.5f));
            }
        }

        [MenuItem("Tools/Castle/Build Confirmed UI Flow")]
        public static void Build()
        {
            Validate(); Directory.CreateDirectory("Builds/Flow");
            var report = BuildPipeline.BuildPlayer(new[] { CastleFlowSceneBuilder.ScenePath }, "Builds/Flow/CastleFlow.exe", BuildTarget.StandaloneWindows64, BuildOptions.Development);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new InvalidOperationException("UI flow build failed.");
        }
    }
}

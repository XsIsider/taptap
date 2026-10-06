using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Castle
{
    public sealed partial class CastleGame
    {
        RenderTexture captureTarget;
        Camera captureCamera;
        // Opt-in development-player test. It never loads or writes the player's real save.
        IEnumerator RunSmokeTest()
        {
            Application.runInBackground = true;
            string folder = Path.GetFullPath("Logs/CastleScreenshots");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "result.txt"), "RUNNING");
            var errors = new System.Collections.Generic.List<string>();
            Application.LogCallback handler = (message, trace, type) => {
                if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) errors.Add(message + "\n" + trace);
            };
            Application.logMessageReceived += handler;
            // Hidden Windows players do not present their swap chain or finish the splash.
            // Render the same uGUI hierarchy into an offscreen camera for layout inspection.
            // This change is confined to the opt-in test process, never normal gameplay.
            UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline = null;
            QualitySettings.renderPipeline = null;
            captureTarget = new RenderTexture(1600, 900, 24);
            captureCamera = Camera.main;
            captureCamera.enabled = false;
            captureCamera.targetTexture = captureTarget;
            var canvas = root.GetComponentInParent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = captureCamera;
            canvas.planeDistance = 1;
            yield return null;
            yield return Capture(folder, "01-title");
            Click("开始游戏"); yield return null;
            Click("展开手中的邀请函"); yield return null;
            Click("收起并保存邀请函"); yield return null;
            Click("确认收纳"); yield return null;
            Click("进入古堡"); yield return null;
            for (int i = 0; i < 5; i++) { Click("继续 →"); yield return null; }
            yield return Capture(folder, "02-dialogue");
            Click("打开地图"); yield return null;
            Click("中控室"); yield return null;
            yield return Capture(folder, "03-map-locked");
            Click("等待至 19:00"); yield return null;
            Click("进入房间"); yield return null;
            yield return Capture(folder, "04-control-room");
            Click("查看录音工作台"); yield return null;
            ChooseFile(0); selectedLine=1; Render(); yield return null;
            Click("设为声音锚点"); yield return null;
            ChooseFile(1); selectedLine=1; Render(); yield return null;
            Click("设为声音锚点"); yield return null;
            yield return Capture(folder, "05-recordings");
            Click("前往锚点对齐"); yield return null;
            SetOffsetLive(180); Click("检查对齐"); yield return null;
            yield return Capture(folder, "06-alignment");
            Click("声音传播 →"); yield return null;
            Click("餐厅"); yield return null;
            Click("关闭"); yield return null;
            Click("关联门状态证据", true); yield return null;
            Click("保存传播草稿"); yield return null;
            yield return Capture(folder, "07-propagation");
            Click("联合验证"); yield return null;
            Click("确认录音身份 →"); yield return null;
            Click("林女士", true); yield return null;
            Click("关联休息室交谈摘录", true); yield return null;
            Click("确认身份"); yield return null;
            Click("重建人物路线 →"); yield return null;
            Click("书房"); yield return null;
            Click("2  21:30", true); yield return null;
            Click("餐厅"); yield return null;
            Click("3  21:40", true); yield return null;
            Click("休息室"); yield return null;
            Click("关联位置证据", true); yield return null;
            Click("核对人物路线"); yield return null;
            yield return Capture(folder, "08-route");
            Click("事件还原 →"); yield return null;
            Click("选择支持证据", true); yield return null;
            yield return Capture(folder, "09-final-evidence");
            Click("提交事件还原"); yield return null;
            yield return Capture(folder, "10-truth");
            Click("继续重建"); yield return null;
            Click("继续重建"); yield return null;
            Click("作出决定"); yield return null;
            Click("公开完整记录", true); yield return null;
            yield return Capture(folder, "11-ending");
            if (!state.finalOK || state.confirmedRoute == null || page != ScreenId.Ending) errors.Add("Full UI walkthrough did not reach ending.");
            Click("返回结局前"); yield return null;
            Click("先与当事人对质", true); yield return null;
            if (state.ending != "当面对质") errors.Add("Second ending did not work.");
            Click("查看调查册"); yield return null;
            yield return Capture(folder, "12-journal");
            CloseOverlay(); Settings(); yield return null;
            yield return Capture(folder, "13-settings");
            Application.logMessageReceived -= handler;
            File.WriteAllText(Path.Combine(folder, "result.txt"), errors.Count == 0 ? "PASS: full UI click-through reached both endings. No runtime errors." : string.Join("\n\n", errors));
            Application.Quit(errors.Count == 0 ? 0 : 1);
        }
        void Click(string label, bool startsWith = false)
        {
            var button = root.GetComponentsInChildren<Button>().FirstOrDefault(b => b.interactable &&
                (startsWith ? b.name.StartsWith(label, StringComparison.Ordinal) : b.name == label));
            if (!button) throw new InvalidOperationException("Smoke test button not found: " + label + " / " + page);
            button.onClick.Invoke();
        }
        IEnumerator Capture(string folder, string name)
        {
            if(toastLabel)toastLabel.transform.parent.gameObject.SetActive(false);
            yield return null;
            Canvas.ForceUpdateCanvases();
            captureCamera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = captureTarget;
            var texture = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), texture.EncodeToPNG());
            Destroy(texture);
        }
    }
}

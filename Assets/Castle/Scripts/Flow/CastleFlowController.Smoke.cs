using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Castle.Flow
{
    public sealed partial class CastleFlowController
    {
        private int _smokeChecks;
        private Camera _captureCamera;
        private RenderTexture _captureTarget;
        private void SmokeCheck(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("FLOW SMOKE FAILED: " + message);
            _smokeChecks++;
        }
        private void Click(string view,string control) => V(view).Get<Button>(control).onClick.Invoke();

        private IEnumerator RunFlowSmoke()
        {
            // 真正运行场景，通过已经接线的按钮/拖放回调执行；不读取或写入玩家存档。
            Application.runInBackground = true;
            // 隐藏的 Windows 窗口没有可截图的 swap chain。仅测试进程使用离屏相机渲染同一套 UI。
            UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline = null;
            QualitySettings.renderPipeline = null;
            _captureTarget = new RenderTexture(1600,900,24);
            _captureCamera = Camera.main; _captureCamera.enabled = false; _captureCamera.targetTexture = _captureTarget;
            var canvas = Ui.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = _captureCamera; canvas.planeDistance = 1;
            Directory.CreateDirectory("Logs/FlowScreens");
            yield return null;
            yield return CaptureFlow("title");
            Click("Title","Start");
            SmokeCheck(_page == "Invitation", "标题到邀请函");
            Click("Invitation","Surface"); SmokeCheck(Session.InvitationExpanded, "展开邀请函");
            yield return CaptureFlow("invitation");
            Click("Invitation","Surface"); SmokeCheck(_modals.Last() == "Acquire" && Session.Entries.Contains(Content.InvitationId), "领取邀请函");
            yield return CaptureFlow("acquire");
            Click("Acquire","Confirm"); SmokeCheck(Session.RoomId == "east" && !string.IsNullOrEmpty(Session.ActiveDialogue), "进入走廊管家对白");
            Click("Explore","Continue");
            var linkedText = _dialogueTexts[1]; linkedText.ForceMeshUpdate();
            var link = linkedText.textInfo.linkInfo[0];
            var character = linkedText.textInfo.characterInfo[link.linkTextfirstCharacterIndex];
            var point = linkedText.transform.TransformPoint((character.bottomLeft + character.topRight) * .5f);
            linkedText.GetComponent<FlowTextLink>().OnPointerClick(new PointerEventData(EventSystem.current)
            {
                position = RectTransformUtility.WorldToScreenPoint(_captureCamera, point),
                pointerPressRaycast = new RaycastResult { module = Ui.GetComponent<GraphicRaycaster>() }
            });
            SmokeCheck(Session.Entries.Contains(Content.MapId), "点击赠送关键词获得地图"); Click("Acquire","Confirm");
            Click("Explore","Continue");
            V("Explore").Get<ScrollRect>("DialogueScroll").verticalNormalizedPosition = .4f;
            Click("Explore","Auto"); ClickKeyword(Content.Dialogue(Content.OpeningDialogue).Lines[2].Keywords[0]);
            SmokeCheck(_selectedRoom == Content.ControlRoom && !_auto && !V("Map").Get<Button>("Enter").interactable, "关键词选中地图、暂停自动、禁止移动");
            Click("Map","Close"); SmokeCheck(Mathf.Abs(Session.Progress(Content.OpeningDialogue).Scroll - .4f) < .01f, "关闭地图保留滚动");
            Click("Explore","Continue"); Click("Explore","Continue"); Click("Explore","Continue"); Click("Explore","Continue");
            SmokeCheck(_modals.Last() == "Acquire" && Session.Entries.Contains(Content.TapeId), "对白结束自动补领磁带"); Click("Acquire","Confirm");
            SmokeCheck(string.IsNullOrEmpty(Session.ActiveDialogue), "补领完毕结束对白");
            OpenJournal(Content.TapeId); UseItem(Content.Entry(Content.TapeId));
            SmokeCheck(Session.TapePlayback, "指定地点使用磁带"); Click("Explore","ExitTape");
            SmokeCheck(_modals.Last() == "Journal" && Session.Entries.Contains(Content.TapeId), "磁带不消耗且退出回调查册");
            UseItem(Content.Entry(Content.TapeId)); Click("Explore","ExitTape"); Click("Journal","Close");
            StartDialogue("dlg_guest"); Click("Explore","Continue");
            SmokeCheck(Content.Dialogue(Session.ActiveDialogue).Lines[Session.Progress(Session.ActiveDialogue).Shown.Last()].Choices.Length == 2, "对白选项");
            StartDialogue("dlg_guard"); RevealNext(Content.Dialogue("dlg_guard"),1); RevealNext(Content.Dialogue("dlg_guard"),2);
            V("Explore").Get<ScrollRect>("DialogueScroll").verticalNormalizedPosition = .6f;
            StartDialogue("dlg_guest");
            SmokeCheck(Mathf.Abs(Session.Progress("dlg_guard").Scroll - .6f) < .01f, "切换对白保存各自滚动");
            SmokeCheck(Session.Progress("dlg_guest").Shown.Last() == 1, "切换人物保留进度");
            RevealNext(Content.Dialogue("dlg_guest"),3); Click("Explore","Continue"); Click("Explore","Continue");
            Click("Explore","ExploreRoom"); OpenMap(Content.ControlRoom); Click("Map","Enter");
            SmokeCheck(Session.RoomId == Content.ControlRoom, "探索推进时间后进入中控室");
            OpenJournal(Content.TapeId); UseItem(Content.Entry(Content.TapeId));
            SmokeCheck(!Session.TapePlayback && _modals.Last() == "Journal", "错误地点拒绝磁带"); Click("Journal","Close");
            Click("Explore","Workbench");
            yield return CaptureFlow("recordings");
            for (int puzzleIndex = 0; puzzleIndex < Content.Puzzles.Length; puzzleIndex++)
            {
                Session.PuzzleIndex = puzzleIndex;
                var draft = Session.Draft(Puzzle.Id);
                foreach (var id in Puzzle.RecordingIds) draft.Marked.Add(id);
                ShowPage("Anchors");
                SmokeCheck(ExecuteEvents.GetEventHandler<IDropHandler>(V("Anchors").Get<ScrollRect>("TracksScroll").viewport.gameObject) == V("Anchors").Get<FlowDrag>("DropZone").gameObject, "滚动区拖放能冒泡到接收器");
                foreach (var id in Puzzle.RecordingIds)
                {
                    var card = V("Anchors").Get<RectTransform>("List").GetComponentsInChildren<FlowDrag>().First(item => item.Id == id);
                    V("Anchors").Get<FlowDrag>("DropZone").Dropped(card);
                }
                SmokeCheck(draft.Tracks.Count == 3, "拖入三轨");
                Session.ChooseReference(draft, "record_b");
                Click("Anchors","Check");
                foreach (var track in draft.Tracks) track.ShiftSeconds = Content.Recording("record_b").StartSeconds - Content.Recording(track.RecordingId).StartSeconds;
                Click("Anchors","Check"); SmokeCheck(draft.Aligned, "任意玩家参考校准");
                Click("Anchors","Next");
                Session.Devices.Clear();
                foreach (var record in Content.Recordings) Session.Devices.Add(new DeviceLocation { Id = record.DeviceId, RoomId = record.RoomId });
                Click("Space","Verify"); Click("Acquire","Confirm");
                SmokeCheck(Session.Entries.Contains(Puzzle.ClueId) && Session.JournalCategory == (int)EntryKind.Clue, "验证收录到记录页"); Click("Journal","Close");
            }
            ShowPage("Timeline");
            for (int i = 0; i < Content.EndingOrder.Length; i++) PlaceClue(Content.EndingOrder[Content.EndingOrder.Length - 1 - i],i);
            Click("Timeline","Submit"); SmokeCheck(!Session.EndingSubmitted, "错误顺序不推进");
            for (int i = 0; i < Content.EndingOrder.Length; i++) PlaceClue(Content.EndingOrder[i],i);
            Click("Timeline","Submit"); SmokeCheck(Session.EndingSubmitted && V("Timeline").Get<RectTransform>("RevealRoot").gameObject.activeSelf, "正确提交直接呈现波形对白");
            _position = 5; UpdateEndingPlayback();
            yield return CaptureFlow("ending");
            OpenJournal(Content.EndingOrder[0]); yield return CaptureFlow("journal");
            OpenJournal(Content.InvitationId); yield return CaptureFlow("items");
            OpenJournal("person_butler"); yield return CaptureFlow("people");
            Click("Journal","Next"); yield return new WaitForSecondsRealtime(.35f);
            SmokeCheck(Session.JournalPage == 1 && Session.JournalSelection == "person_cook", "翻页后默认选中该页第一位人物");
            yield return CaptureFlow("people-page2");
            Click("Journal","Previous"); yield return new WaitForSecondsRealtime(.35f);
            SmokeCheck(Session.JournalPage == 0 && Session.JournalSelection == "person_butler", "上一页恢复首条详情");
            Click("Journal","Close"); ShowPage("Anchors"); yield return CaptureFlow("anchors");
            ShowPage("Space"); yield return CaptureFlow("space");
            ShowPage("Explore"); OpenMap(); yield return CaptureFlow("map");
            PopModal(); StartDialogue("dlg_guard"); yield return CaptureFlow("dialogue");
            ShowPage("Title",false); Click("Title","Start"); Click("Confirm","Cancel"); SmokeCheck(Session.EndingSubmitted,"取消新游戏保留进度");
            Click("Title","Start"); Click("Confirm","Confirm"); SmokeCheck(!Session.EndingSubmitted && Session.Entries.Count == 0,"新游戏干净重置");
            yield return null;
            File.WriteAllText("Logs/castle-flow-smoke.txt", "PASS " + _smokeChecks + " UI flow checks\n" + DateTime.UtcNow.ToString("O"));
            Debug.Log("CASTLE FLOW SMOKE PASS " + _smokeChecks);
            Application.Quit(0);
        }

        private IEnumerator CaptureFlow(string name)
        {
            Ui.Toast.gameObject.SetActive(false);
            yield return null;
            Canvas.ForceUpdateCanvases();
            foreach (var bar in Ui.GetComponentsInChildren<Scrollbar>())
                SmokeCheck(bar.handleRect.rect.height <= ((RectTransform)bar.handleRect.parent).rect.height + 1, "滚动条不超出轨道：" + name);
            _captureCamera.Render();
            var previous = RenderTexture.active; RenderTexture.active = _captureTarget;
            var texture = new Texture2D(1600,900,TextureFormat.RGB24,false);
            texture.ReadPixels(new Rect(0,0,1600,900),0,0); texture.Apply(); RenderTexture.active = previous;
            File.WriteAllBytes(System.IO.Path.GetFullPath("Logs/FlowScreens/" + name + ".png"), texture.EncodeToPNG());
            Destroy(texture);
        }
    }
}

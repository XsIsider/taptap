using System;
using UnityEngine;
using UnityEngine.UI;
namespace Castle
{
    public sealed partial class CastleGame
    {
        void Title()
        {
            var v = Current;
            B(v, "StartButton", () => {
                Action start = () => { state = new CastleState { started = true }; undo.Clear(); file = 0; floor = 1; selectedNode = spatialMarker = 0; playPosition = 291; selectedLine = -1; selectedRoom = null; currentJournalEntry = null; journalTab = "人物"; journalScroll = transcriptScroll = 1; Go(ScreenId.Outside); };
                if (state.started) Message("开始新的调查", "当前进度将被新游戏替换。", start); else start();
            });
            B(v, "ContinueButton", () => Go(state.resume), interactable: state.started); B(v, "SettingsButton", Settings);
            B(v, "CreditsButton", () => Message("古堡 · 声音与时间的谜题", "Unity 交互框架\n场景美术与字体来自提供的 HTML 原型。\n正式制作名单待团队补充。", null));
            B(v, "ExitButton", () => { Save();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });
        }
        void Hud()
        {
            var v = ui.SceneHud; var room = database.Room(state.room);
            T(v, "LocationText", room == null ? "古堡外" : room.floor + "F · " + room.title);
            T(v, "ClockText", "当晚 " + CastleRules.Clock(state.minutes));
            B(v, "MapButton", Navigation, interactable: state.mapUnlocked); B(v, "JournalButton", Journal); B(v, "SettingsButton", Settings);
        }
        void Outside()
        {
            var v = Current; Picture(v, "Background", state.invitation ? "Outside" : "Held");
            Visible(v, "OpenInvitationButton", !state.invitation); Visible(v, "EnterButton", state.invitation); Visible(v, "HintText", state.invitation);
            B(v, "OpenInvitationButton", () => Go(ScreenId.Invitation));
            B(v, "EnterButton", () => { state.room = "101"; state.minutes = 1130; Go(ScreenId.Lounge); });
        }
        void Invitation() { B(Current, "CollectButton", () => { state.invitation = true; Go(ScreenId.Collected); }); }
        void Collected() { B(Current, "ConfirmButton", () => Go(ScreenId.Outside)); }
        void AdvanceDialogue() { CastleRules.AdvanceDialogue(state); Save(); Render(); }
        void Lounge()
        {
            var v = Current; int start = Math.Max(0, state.dialogue - 2);
            for (int i = 0; i < 3; i++)
            {
                var text = v.Get<Text>("Line" + i); text.gameObject.SetActive(start + i <= state.dialogue);
                if (start + i <= state.dialogue) { text.text = database.greeting[start + i]; text.fontSize = state.readingSize; }
            }
            T(v, "ProgressText", state.recorded ? "对话已录音 · 已收入随身档案" : state.controlUnlocked ? "中控室已解锁 · 19:00开放" : state.mapUnlocked ? "古堡地图已收录" : "空格或点击按钮继续阅读");
            B(v, "ContinueButton", state.recorded ? (Action)Navigation : AdvanceDialogue, state.recorded ? "打开地图" : "继续 →");
            Visible(v, "MapButton", state.mapUnlocked); B(v, "MapButton", Navigation);
        }
        void Room()
        {
            var v = Current; bool control = state.room == "110";
            Picture(v, "Background", control ? "ControlRoom" : "EmptyRoom"); Visible(v, "ControlRoot", control); Visible(v, "OtherRoomRoot", !control);
            B(v, "WorkbenchButton", () => { state.workbench = true; state.Discover("103", "104", "108"); Go(ScreenId.Browse); });
            T(v, "RoomNameText", database.Room(state.room)?.title ?? "古堡");
            T(v, "DescriptionText", state.room == "108" ? "林女士：西翼那株白花，是我亲手栽下的。\n\n前一晚，我先去书房，之后经过餐厅，再回到休息室。" : state.room == "104" ? "柜门旁留下碰撞的痕迹。金属搭扣与器物受力方向，可以作为还原事件的物证。" : "房间暂时无人。远处传来器物轻轻碰撞的声音。可以打开地图，继续探索古堡。");
            B(v, "MapButton", Navigation);
        }
        void Navigation()
        {
            if (!IsScene() || !state.mapUnlocked) { Toast("在场景中获得地图后，可以进行房间导航。"); return; }
            selectedRoom = null; DrawNavigation();
        }
        void DrawNavigation()
        {
            var v = OpenOverlay("Navigation");
            T(v, "LocationText", "当前位置：" + (database.Room(state.room)?.title ?? "古堡外") + " / " + CastleRules.Clock(state.minutes));
            BindMap(v, true, false); B(v, "CloseButton", CloseOverlay);
            var selected = database.Room(selectedRoom); string error = selected == null ? null : CastleRules.CanVisit(state, selected);
            T(v, "SelectionText", selected == null ? "点击房间查看开放条件，再选择进入。移动耗时 2 分钟。" : (state.discovered.Contains(selected.id) ? selected.title : "未探索区域") + " · " + (error ?? "现在可以进入"));
            Visible(v, "EnterButton", selected != null && error == null);
            Visible(v, "WaitButton", selected != null && error != null && state.discovered.Contains(selected.id) && state.minutes < selected.opens && (selected.id != "110" || state.controlUnlocked));
            B(v, "EnterButton", () => { var r = database.Room(selectedRoom); Check(CastleRules.Travel(state, r), () => Go(r.id == "101" ? ScreenId.Lounge : ScreenId.Room)); });
            B(v, "WaitButton", () => { state.minutes = database.Room(selectedRoom).opens; Save(); DrawNavigation(); }, "等待至 " + (selected == null ? "" : CastleRules.Clock(selected.opens)));
        }
        void Settings()
        {
            var v = OpenOverlay("Settings"); T(v, "SizeText", state.readingSize.ToString());
            S(v, "SizeSlider", 20, 28, state.readingSize, value => { state.readingSize = (int)value; T(v, "SizeText", state.readingSize.ToString()); Changed(); });
            S(v, "VolumeSlider", 0, 100, state.volume * 100, value => { state.volume = value / 100; Changed(); });
            B(v, "MotionButton", () => { state.reduceMotion = !state.reduceMotion; Save(); Settings(); }, state.reduceMotion ? "动态效果：关闭" : "动态效果：开启");
            B(v, "TitleButton", () => Go(ScreenId.Title)); B(v, "ContinueButton", () => { Save(); CloseOverlay(); Render(); });
        }
        void Journal() { JournalGrid(); }
    }
}

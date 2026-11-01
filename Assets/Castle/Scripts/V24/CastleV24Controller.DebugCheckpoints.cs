using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Castle.V24
{
    public sealed partial class CastleV24Controller
    {
        // 三次独立按下须在 0.8 秒内完成；长按不重复计数。
        private const float CheckpointDebugPressWindow = .8f;
        private int _checkpointDebugPresses;
        private float _checkpointDebugFirstPress;
        private GameObject _checkpointDebugRoot;
        private Text _checkpointDebugStatus;
        private V24TestCheckpointCatalog _checkpointDebugCatalog;

        private bool UpdateCheckpointDebugInput()
        {
            if (_page != "Title" || _modal.Count != 0 || _openingTransition)
            {
                _checkpointDebugPresses = 0;
                CloseCheckpointDebug();
                return false;
            }
            bool visible = _checkpointDebugRoot && _checkpointDebugRoot.activeSelf;
            if (visible && Input.GetKeyDown(KeyCode.Escape)) { CloseCheckpointDebug(); return true; }
            if (Input.GetKeyDown(KeyCode.D))
            {
                if (visible) { CloseCheckpointDebug(); return true; }
                RegisterCheckpointDebugPress(Time.unscaledTime);
                visible = _checkpointDebugRoot && _checkpointDebugRoot.activeSelf;
            }
            return visible;
        }

        private void RegisterCheckpointDebugPress(float now)
        {
            if (_checkpointDebugPresses == 0 || now - _checkpointDebugFirstPress > CheckpointDebugPressWindow)
            {
                _checkpointDebugPresses = 0;
                _checkpointDebugFirstPress = now;
            }
            if (++_checkpointDebugPresses < 3) return;
            _checkpointDebugPresses = 0;
            OpenCheckpointDebug();
        }

        private void OpenCheckpointDebug()
        {
            if (_page != "Title" || _modal.Count != 0) return;
            if (!_checkpointDebugRoot) CreateCheckpointDebug();
            _checkpointDebugRoot.SetActive(true);
            _checkpointDebugRoot.transform.SetAsLastSibling();
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
        }

        private void CloseCheckpointDebug()
        {
            _checkpointDebugPresses = 0;
            if (_checkpointDebugRoot) _checkpointDebugRoot.SetActive(false);
        }

        /// <summary>按主程要求，仅此隐藏调试面板在运行时创建，不写入场景和绑定表。</summary>
        private void CreateCheckpointDebug()
        {
            var root = DebugRect("CheckpointDebug", V("Title").transform, Vector2.zero, Vector2.zero);
            root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;
            _checkpointDebugRoot = root.gameObject;
            root.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .65f);
            var panel = DebugRect("Panel", root, Vector2.zero, new Vector2(1330, 770));
            panel.gameObject.AddComponent<Image>().color = new Color(.055f, .075f, .10f, .94f);
            DebugText("Heading", panel, "开发测试存档", new Vector2(-430, 330), new Vector2(400, 46), 30, TextAnchor.MiddleLeft);
            DebugButton("Close", panel, "返回 ×", new Vector2(555, 330), new Vector2(140, 48), CloseCheckpointDebug);
            DebugText("Instructions", panel, "点击节点将覆盖本机自动档，并先备份原档。关闭面板后，点击主界面的「继续游戏」。",
                new Vector2(0, 275), new Vector2(1250, 54), 21, TextAnchor.MiddleLeft);
            _checkpointDebugStatus = DebugText("Status", panel, "选择一个测试节点；不会立即进入游戏。",
                new Vector2(0, -327), new Vector2(1245, 70), 20, TextAnchor.MiddleLeft);
            var viewport = DebugRect("Viewport", panel, new Vector2(0, -15), new Vector2(1255, 500));
            viewport.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, .025f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = DebugRect("Content", viewport, Vector2.zero, new Vector2(1230, 0));
            content.anchorMin = content.anchorMax = new Vector2(.5f, 1); content.pivot = new Vector2(.5f, 1);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 38;
            var track = DebugRect("Scrollbar", panel, new Vector2(641, -15), new Vector2(12, 500));
            track.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, .08f);
            var handle = DebugRect("Handle", track, Vector2.zero, Vector2.zero);
            handle.anchorMin = Vector2.zero; handle.anchorMax = Vector2.one; handle.offsetMin = handle.offsetMax = Vector2.zero;
            var handleImage = handle.gameObject.AddComponent<Image>(); handleImage.color = new Color(.5f, .65f, .75f, .8f);
            var scrollbar = track.gameObject.AddComponent<Scrollbar>(); scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handleImage; scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = scrollbar;
            try
            {
                _checkpointDebugCatalog = V24TestCheckpoints.LoadCatalog();
                int index = 0;
                foreach (var checkpoint in _checkpointDebugCatalog.Checkpoints)
                {
                    var selected = checkpoint;
                    int row = index / 3, column = index % 3;
                    var button = DebugButton(selected.Id, content, selected.Label + "\n" + selected.Description,
                        new Vector2(-410 + column * 410, -56 - row * 112), new Vector2(396, 100), () => LoadCheckpointDebug(selected));
                    var buttonRect = button.GetComponent<RectTransform>();
                    buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(.5f, 1);
                    button.GetComponentInChildren<Text>().fontSize = 19;
                    index++;
                }
                content.sizeDelta = new Vector2(1230, Mathf.CeilToInt(index / 3f) * 112 + 12);
            }
            catch (Exception ex) { _checkpointDebugStatus.text = "无法载入测试档：" + ex.Message; }
            scroll.verticalNormalizedPosition = 1;
        }

        private void LoadCheckpointDebug(V24TestCheckpoint checkpoint, string destination = null)
        {
            if (_page != "Title" || _modal.Count != 0) return;
            try
            {
                V24TestCheckpoints.LoadCatalog();
                var state = V24TestCheckpoints.Read(_data, checkpoint);
                state.Volume = S.Volume; state.EffectsVolume = S.EffectsVolume; state.AutoSeconds = S.AutoSeconds;
                state.Auto = false;
                string backup = V24TestCheckpoints.Install(state, destination);
                // 磁盘与内存一起切换，继续按钮沿用原回调，但读取的是本次新状态。
                Engine.State = state; _blocked = false; _playing = false;
                _record = 0; _playTape = 0; _position = 0; _heardPosition = 0; _audioLine = 0;
                _presentationRoom = 0; _avatarsExpanded = false; _endingDialogue = 0;
                _dialogueHistoryPaused = false; _renderedDialogueCount = 0;
                _timelineClueOrder = null; _selectedEntry = 0;
                if (Voice) Voice.Stop(); if (Effects) Effects.Stop();
                HideToast(); HideFloatingHint(); ClearDialogueCollectionHint();
                ApplyAudioSettings(); Title();
                if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
                _checkpointDebugStatus.text = "已载入「" + checkpoint.Label + "」。关闭面板，点击「继续游戏」。"
                    + (backup.Length == 0 ? "\n此前没有存档；继续游戏现已可用。" : "\n原档已备份至 TestCheckpointBackups/，不会被后续自动保存覆盖。");
            }
            catch (Exception ex) { _checkpointDebugStatus.text = "载入失败：" + ex.Message; }
        }

        private static RectTransform DebugRect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position; rect.sizeDelta = size;
            return rect;
        }

        private Text DebugText(string name, Transform parent, string value, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment)
        {
            var rect = DebugRect(name, parent, position, size);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = ReadingFont ? ReadingFont : ButtonFont; text.fontSize = fontSize; text.color = Color.white;
            text.alignment = alignment; text.text = value; text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private Button DebugButton(string name, Transform parent, string label, Vector2 position, Vector2 size, Action clicked)
        {
            var rect = DebugRect(name, parent, position, size);
            var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(.16f, .23f, .29f, .96f);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.highlightedColor = new Color(.75f, .9f, 1);
            colors.pressedColor = new Color(.5f, .75f, .9f); button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() => clicked());
            DebugText("Label", rect, label, Vector2.zero, size - new Vector2(22, 8), 22, TextAnchor.MiddleCenter);
            return button;
        }
    }
}

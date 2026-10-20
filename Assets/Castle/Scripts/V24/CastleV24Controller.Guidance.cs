using System.Linq;
using UnityEngine;

namespace Castle.V24
{
    public sealed partial class CastleV24Controller
    {
        public CastleUiView FloatingHint;
        // 与锚点详情一致，从出现到完全淡出的时长，单位为秒。
        private const float FloatingHintFadeSeconds = 5f;
        private float _floatingHintUntil;

        private void ShowFloatingHint(string text)
        {
            Ui.Toast.gameObject.SetActive(false);
            _anchorDetailsUntil = 0f;
            Show(V("Anchors"), "AnchorDetails", false);
            FloatingHint.Get<TMPro.TextMeshProUGUI>("Body").text = text;
            _floatingHintUntil = Time.unscaledTime + FloatingHintFadeSeconds;
            FloatingHint.gameObject.SetActive(true);
            UpdateFloatingHintFade();
        }

        private void HideFloatingHint()
        {
            _floatingHintUntil = 0f;
            FloatingHint.Get<CanvasGroup>("Group").alpha = 0f;
            FloatingHint.gameObject.SetActive(false);
        }

        private void UpdateFloatingHintFade()
        {
            if (_floatingHintUntil <= 0f) return;
            float remaining = Mathf.Clamp01((_floatingHintUntil - Time.unscaledTime) / FloatingHintFadeSeconds);
            FloatingHint.Get<CanvasGroup>("Group").alpha = Mathf.SmoothStep(0f, 1f, remaining);
            if (remaining <= 0f) HideFloatingHint();
        }

        private void UpdatePersonPathReadyHint()
        {
            if (!_data.Formal || Engine.Busy || S.PathReadyNotice || !Engine.Need(_data.PuzzleById(720020).Need)) return;
            S.PathReadyNotice = true;
            Save();
            // 保留原有确认框、查看与取消操作，同时补充非阻塞悬浮提示。
            Confirm("维里诺的人物路径证据已集齐，是否现在查看？", () => Journal(0, 200006, true));
            ShowFloatingHint("打开线索册，试着推理看看吧");
        }

        private void UpdatePersonPathCompletionHint()
        {
            if (!_data.Formal || !S.Started || S.PathCompleteNotice || Engine.Busy || _modal.Count > 0
                || _page == "Title" || _page == "Opening" || _page == "Invitation" || _page == "Cutscene" || _page == "EndCard") return;

            var puzzle = _data.PuzzleById(_data.PersonPathPolicy.Puzzle);
            if (!S.Puzzles.Contains(puzzle.Id)) return;
            var reward = _data.EventById(puzzle.Success);
            if (reward == null || !reward.Reward.All(Engine.Has)) return;
            // 等奖励结算后刷新人物详情，确保奖励句已追加、人物路径入口已隐藏。
            Journal(0, puzzle.Target, true);
            ShowFloatingHint("现在去找维里诺谈话吧");
            S.PathCompleteNotice = true;
            Save();
        }
    }
}

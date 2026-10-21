using System.Collections;
using System.Linq;
using UnityEngine;

namespace Castle.V24
{
    public sealed partial class CastleV24Controller
    {
        public CastleUiView FloatingHint;
        private void HideFloatingHint()
        {
            FloatingHint.Get<CanvasGroup>("Group").alpha = 0f;
            FloatingHint.gameObject.SetActive(false);
        }

        /// <summary>所有悬浮提示共用动画；秒为真实时间，只替换文案，保持淡入、停留和淡出一致。</summary>
        private IEnumerator FadeHint(CanvasGroup hint, System.Action onFinished)
        {
            hint.gameObject.SetActive(true);
            hint.alpha = 0;
            const float fadeInSeconds = 0.25f, holdSeconds = 3.5f, fadeOutSeconds = 1f;
            float elapsed = 0;
            while (elapsed < fadeInSeconds + holdSeconds + fadeOutSeconds)
            {
                hint.alpha = elapsed < fadeInSeconds ? elapsed / fadeInSeconds
                    : elapsed < fadeInSeconds + holdSeconds ? 1f
                    : 1f - (elapsed - fadeInSeconds - holdSeconds) / fadeOutSeconds;
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            hint.alpha = 0;
            hint.gameObject.SetActive(false);
            onFinished();
        }

        private void UpdateTimelineReadyHint()
        {
            if (!_data.Formal || !S.Started || S.TimelineReadyNotice || S.Ending || S.Finished
                || Engine.Busy || _modal.Count > 0 || _toastRoutine != null
                || _page == "Title" || _page == "Opening" || _page == "Invitation" || _page == "Cutscene" || _page == "EndCard") return;

            var comparisons = _data.Puzzle.Where(x => x.Type == "logic_match");
            if (!comparisons.Any() || !comparisons.All(x => S.Puzzles.Contains(x.Id))) return;
            var timeline = _data.Puzzle.FirstOrDefault(x => x.Type == "timeline");
            if (timeline == null || S.Puzzles.Contains(timeline.Id)) return;

            // 等最后一题奖励和当前提示结束后再显示；人物路径不属于文本对照题。
            Toast("请重构时间线");
            S.TimelineReadyNotice = true;
            Save();
        }

        private void UpdatePersonPathReadyHint()
        {
            if (!_data.Formal || Engine.Busy || S.PathReadyNotice || !Engine.Need(_data.PuzzleById(720020).Need)) return;
            S.PathReadyNotice = true;
            Save();
            // 保留原有确认框、查看与取消操作，同时补充非阻塞悬浮提示。
            Confirm("维里诺的人物路径证据已集齐，是否现在查看？", () => Journal(0, 200006, true));
            Toast("打开线索册，试着推理看看吧");
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
            Toast("现在去找维里诺谈话吧");
            S.PathCompleteNotice = true;
            Save();
        }
    }
}

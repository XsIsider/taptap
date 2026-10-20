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

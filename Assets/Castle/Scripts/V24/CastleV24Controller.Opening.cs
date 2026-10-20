using System.Collections;
using System.Linq;
using UnityEngine;

namespace Castle.V24
{
    public sealed partial class CastleV24Controller
    {
        // 黑屏遮罩单程渐变时长，单位为秒；切换时拦截重复点击。
        private const float OpeningFadeSeconds = 0.35f;
        private bool _openingTransition;

        private void BeginOpening()
        {
            if (Engine.OpeningLines.Length == 0) { Page("Invitation"); return; }
            S.Page = "Opening";
            Save();
            StartCoroutine(TransitionOpening("Opening"));
        }

        private void Opening()
        {
            var line = Engine.OpeningLines.FirstOrDefault(x => !S.Shown.Contains(x.Id));
            if (line == null) { Page("Invitation"); return; }
            var view = V("Opening");
            Txt(view, "Body", line.Text);
            Btn(view, "Surface", NextOpening);
        }

        private void NextOpening()
        {
            if (_openingTransition || _page != "Opening" || _modal.Count > 0) return;
            var line = Engine.OpeningLines.FirstOrDefault(x => !S.Shown.Contains(x.Id));
            // 复用已读剧情 ID 保存进度，不把旁白追加回普通对白。
            if (line != null) S.Shown.Add(line.Id);
            string next = Engine.OpeningLines.Any(x => !S.Shown.Contains(x.Id)) ? "Opening" : "Invitation";
            S.Page = next;
            Save();
            StartCoroutine(TransitionOpening(next));
        }

        private IEnumerator TransitionOpening(string target)
        {
            _openingTransition = true;
            var fade = V("Opening").Get<CanvasGroup>("Transition");
            fade.alpha = 0f;
            fade.gameObject.SetActive(true);
            fade.blocksRaycasts = true;
            yield return FadeOpening(fade, 0f, 1f);
            Page(target);
            yield return FadeOpening(fade, 1f, 0f);
            fade.gameObject.SetActive(false);
            _openingTransition = false;
        }

        private IEnumerator FadeOpening(CanvasGroup fade, float from, float to)
        {
            for (float elapsed = 0f; elapsed < OpeningFadeSeconds; elapsed += Time.unscaledDeltaTime)
            {
                fade.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, elapsed / OpeningFadeSeconds));
                yield return null;
            }
            fade.alpha = to;
        }
    }
}

using UnityEngine.EventSystems;

namespace Castle.V24
{
    /// <summary>滑动条按下即暂停自动；正文不挂此组件，避免截获文字链接的点击目标。</summary>
    public sealed class V24DialogueScrollbarInput : V24DialogueScrollInput, IPointerDownHandler
    {
        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) UserScrolled?.Invoke();
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Castle.V24
{
    /// <summary>区分玩家滚动和程序定位；滚动正文或操作滑动条时通知控制器暂停自动对白。</summary>
    public class V24DialogueScrollInput : MonoBehaviour, IScrollHandler, IBeginDragHandler
    {
        public ScrollRect Scroll;
        public bool IsScrollbar;
        public Action UserScrolled;

        public void OnScroll(PointerEventData eventData)
        {
            if (eventData.scrollDelta.sqrMagnitude <= 0) return;
            UserScrolled?.Invoke();
            // 正文上的 ScrollRect 自行处理滚轮；滑动条上的滚轮转交正文，避免在此截断事件。
            if (IsScrollbar && Scroll) Scroll.OnScroll(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) UserScrolled?.Invoke();
        }
    }
}

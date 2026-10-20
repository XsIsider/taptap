using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Castle.V24
{
    public sealed class V24LineHit : MonoBehaviour, IPointerClickHandler
    {
        public Action Clicked;
        public void OnPointerClick(PointerEventData data)
        {
            if (!isActiveAndEnabled || data.button != PointerEventData.InputButton.Left || data.dragging) return;
            var text = GetComponent<TextMeshProUGUI>();
            // 链接交给 FlowTextLink；没有 TMP 的背景也可接收空白处点击。
            if (text != null && TMP_TextUtilities.FindIntersectingLink(text, data.position, data.pressEventCamera) >= 0) return;
            Clicked?.Invoke();
        }
    }
}

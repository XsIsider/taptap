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
            // 正文优先处理链接；无 TMP 的对话框背景也可复用此组件接收空白处点击。
            if (text != null && TMP_TextUtilities.FindIntersectingLink(text, data.position, data.pressEventCamera) >= 0) return;
            Clicked?.Invoke();
        }
    }
}

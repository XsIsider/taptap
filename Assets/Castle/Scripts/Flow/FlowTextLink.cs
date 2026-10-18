using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Castle.Flow
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class FlowTextLink : MonoBehaviour, IPointerClickHandler
    {
        public Action<string> Clicked;
        public void OnPointerClick(PointerEventData eventData)
        {
            var text = GetComponent<TextMeshProUGUI>();
            int index = TMP_TextUtilities.FindIntersectingLink(text, eventData.position, eventData.pressEventCamera);
            if (index >= 0) Clicked?.Invoke(text.textInfo.linkInfo[index].GetLinkID());
        }
    }
}

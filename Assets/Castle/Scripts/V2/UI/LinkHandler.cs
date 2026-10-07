using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Castle.V2
{
    public sealed class LinkHandler : MonoBehaviour, IPointerClickHandler
    {
        public Action<string> Click;
        public void OnPointerClick(PointerEventData eventData)
        {
            var text = GetComponent<TMP_Text>(); int index = TMP_TextUtilities.FindIntersectingLink(text, eventData.position, eventData.pressEventCamera);
            if (index >= 0) Click?.Invoke(text.textInfo.linkInfo[index].GetLinkID());
        }
    }
}

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
            if (TMP_TextUtilities.FindIntersectingLink(GetComponent<TextMeshProUGUI>(), data.position, data.pressEventCamera) < 0) Clicked?.Invoke();
        }
    }
}

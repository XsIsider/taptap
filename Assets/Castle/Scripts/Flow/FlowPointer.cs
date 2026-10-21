using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Castle.Flow
{
    public sealed class FlowPointer : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Action Enter, Exit;
        public void OnPointerEnter(PointerEventData eventData) => Enter?.Invoke();
        public void OnPointerExit(PointerEventData eventData) => Exit?.Invoke();
    }
}

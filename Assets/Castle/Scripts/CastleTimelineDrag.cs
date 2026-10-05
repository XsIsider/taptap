using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Castle
{
    public sealed class CastleTimelineDrag : MonoBehaviour, IDragHandler
    {
        public float width = 738;
        public Action<float> onDrag;
        public void OnDrag(PointerEventData data)
        {
            var rt = (RectTransform)transform;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, data.position, data.pressEventCamera, out var current) &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, data.position-data.delta, data.pressEventCamera, out var previous))
                onDrag?.Invoke((current.x-previous.x)*540/width);
        }
    }
}

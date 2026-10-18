using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Castle.Flow
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class FlowDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        public string Kind, Id;
        public bool CanDrag;
        public Action<FlowDrag> Dropped;
        private Vector3 _origin;
        private CanvasGroup _group;
        private bool _dragging;
        private static int _pathDrags;
        public static bool PathDragging => _pathDrags > 0;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || !CanDrag) return;
            _dragging = true;
            if (Kind == "path") _pathDrags++;
            _origin = transform.localPosition;
            _group = GetComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.alpha = .65f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                GetComponentInParent<Castle.V24.V24MapPan>()?.OnDrag(eventData);
                return;
            }
            if (!_dragging) return;
            transform.localPosition += (Vector3)(eventData.delta / GetComponentInParent<Canvas>().scaleFactor);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_dragging) return;
            transform.localPosition = _origin;
            _group.blocksRaycasts = true;
            _group.alpha = 1;
            if (Kind == "path") _pathDrags = Mathf.Max(0, _pathDrags - 1);
            _dragging = false;
        }

        private void OnDisable()
        {
            if (_dragging && Kind == "path") _pathDrags = Mathf.Max(0, _pathDrags - 1);
            _dragging = false;
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            var source = eventData.pointerDrag ? eventData.pointerDrag.GetComponent<FlowDrag>() : null;
            if (source && source.CanDrag) Dropped?.Invoke(source);
        }
    }
}

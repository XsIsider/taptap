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
        // 可选的页面级拖拽预览父节点；不改变原卡片的布局和放置规则。
        public RectTransform DragRoot { get; set; }
        private Vector3 _origin;
        private CanvasGroup _group;
        private RectTransform _dragVisual;
        private float _sourceAlpha;
        private bool _sourceBlocksRaycasts;
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
            _sourceAlpha = _group.alpha;
            _sourceBlocksRaycasts = _group.blocksRaycasts;
            _group.blocksRaycasts = false;
            if (DragRoot)
            {
                // 预览脱离列表遮罩并置于页面最上层；原卡片留在列表中占位。
                var sourceRect = (RectTransform)transform;
                var visual = Instantiate(gameObject, DragRoot, true);
                visual.name = name + "_DragPreview";
                visual.GetComponent<FlowDrag>().enabled = false;
                _dragVisual = (RectTransform)visual.transform;
                _dragVisual.anchorMin = _dragVisual.anchorMax = new Vector2(.5f, .5f);
                _dragVisual.sizeDelta = sourceRect.rect.size;
                _dragVisual.position = sourceRect.position;
                _dragVisual.SetAsLastSibling();
                var visualGroup = visual.GetComponent<CanvasGroup>();
                visualGroup.blocksRaycasts = false;
                visualGroup.alpha = _sourceAlpha * .65f;
                _group.alpha = 0f;
            }
            else _group.alpha = .65f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                GetComponentInParent<Castle.V24.V24MapPan>()?.OnDrag(eventData);
                return;
            }
            if (!_dragging) return;
            var target = _dragVisual ? _dragVisual : transform;
            target.localPosition += (Vector3)(eventData.delta / GetComponentInParent<Canvas>().scaleFactor);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            EndDrag();
        }

        private void EndDrag()
        {
            if (!_dragging) return;
            _dragging = false;
            if (_dragVisual)
            {
                _dragVisual.gameObject.SetActive(false);
                Destroy(_dragVisual.gameObject);
                _dragVisual = null;
            }
            transform.localPosition = _origin;
            _group.blocksRaycasts = _sourceBlocksRaycasts;
            _group.alpha = _sourceAlpha;
            if (Kind == "path") _pathDrags = Mathf.Max(0, _pathDrags - 1);
        }

        private void OnDisable()
        {
            // 放入后列表可能立即重建，页面切换也可能先于 OnEndDrag。
            EndDrag();
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            var source = eventData.pointerDrag ? eventData.pointerDrag.GetComponent<FlowDrag>() : null;
            if (source && source.CanDrag) Dropped?.Invoke(source);
        }
    }
}

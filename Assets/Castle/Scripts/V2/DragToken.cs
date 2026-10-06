using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Castle.V2
{
    // 拖动期间保留页面和播放组件；松开后才提交一次草稿。
    public sealed class DragToken : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public string Value;
        public int Slot = -1;
        CanvasGroup _group;
        public void OnBeginDrag(PointerEventData eventData)
        {
            _group = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            _group.alpha = .55f; _group.blocksRaycasts = false;
        }
        public void OnDrag(PointerEventData eventData) { }
        public void OnEndDrag(PointerEventData eventData)
        {
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(eventData, hits);
            foreach (var hit in hits)
            {
                var zone = hit.gameObject.GetComponentInParent<DropZone>();
                if (zone != null) { zone.Drop?.Invoke(this); break; }
            }
            if (_group) { _group.alpha = 1; _group.blocksRaycasts = true; }
        }
    }
}

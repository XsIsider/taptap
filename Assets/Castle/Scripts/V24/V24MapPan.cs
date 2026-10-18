using System;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Castle.V24
{
    public sealed class V24MapPan : MonoBehaviour, IScrollHandler, IBeginDragHandler, IDragHandler
    {
        public RectTransform Content;
        public Action<float,Vector2> Changed;
        public float Zoom=1;
        public void OnScroll(PointerEventData e)
        {
            if(Castle.Flow.FlowDrag.PathDragging)return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,e.position,e.pressEventCamera,out var point);
            point-=((RectTransform)transform).rect.center;
            float next=Mathf.Clamp(Zoom+e.scrollDelta.y*.15f,1,3);
            Content.anchoredPosition=point-(point-Content.anchoredPosition)*(next/Zoom);Zoom=next;Apply();
        }
        public void OnBeginDrag(PointerEventData e) { }
        public void OnDrag(PointerEventData e)
        {
            if(e.button!=PointerEventData.InputButton.Right || Castle.Flow.FlowDrag.PathDragging)return;
            Content.anchoredPosition+=e.delta/GetComponentInParent<Canvas>().scaleFactor;Apply();
        }
        public void ZoomAtCenter(float delta)
        {
            if(Castle.Flow.FlowDrag.PathDragging)return;
            float next=Mathf.Clamp(Zoom+delta,1,3);Content.anchoredPosition*=next/Zoom;Zoom=next;Apply();
        }
        public void Apply()
        {
            Content.localScale=Vector3.one*Zoom;
            Vector2 size=((RectTransform)transform).rect.size;
            var p=Content.anchoredPosition;p.x=Mathf.Clamp(p.x,-size.x*(Zoom-1)/2,size.x*(Zoom-1)/2);p.y=Mathf.Clamp(p.y,-size.y*(Zoom-1)/2,size.y*(Zoom-1)/2);Content.anchoredPosition=p;
            // 房间热区与地图同比缩放，不能把热区缩回原尺寸导致点击范围脱离房间。
            Changed?.Invoke(Zoom,p);
        }
        private void OnRectTransformDimensionsChange(){if(Content)Apply();}
    }
}

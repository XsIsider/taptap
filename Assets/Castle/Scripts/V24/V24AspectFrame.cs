using UnityEngine;

namespace Castle.V24
{
    /// <summary>用场景中预置的四块黑边遮住 Stage 外的画面和点击，不改变全屏分辨率。</summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class V24AspectFrame : MonoBehaviour
    {
        [SerializeField] private RectTransform _stage;
        [SerializeField] private RectTransform _left;
        [SerializeField] private RectTransform _right;
        [SerializeField] private RectTransform _top;
        [SerializeField] private RectTransform _bottom;

        private readonly Vector3[] _stageCorners = new Vector3[4];
        private Rect _lastOuter;
        private Rect _lastInner;
        private bool _hasLayout;

        private void OnEnable()
        {
            _hasLayout = false;
            Canvas.preWillRenderCanvases += RefreshLayout;
            RefreshLayout();
        }

        private void OnDisable()
        {
            Canvas.preWillRenderCanvases -= RefreshLayout;
        }

        private void OnRectTransformDimensionsChange()
        {
            _hasLayout = false;
        }

        private void RefreshLayout()
        {
            if (!_stage || !_left || !_right || !_top || !_bottom)
                return;

            // Canvas 的最后一个子对象覆盖全部页面，也覆盖不支持 UI 裁切的自定义图形。
            if (transform.GetSiblingIndex() != transform.parent.childCount - 1)
                transform.SetAsLastSibling();

            var frame = (RectTransform)transform;
            var outer = frame.rect;
            _stage.GetWorldCorners(_stageCorners);
            var lower = frame.InverseTransformPoint(_stageCorners[0]);
            var upper = frame.InverseTransformPoint(_stageCorners[2]);
            var inner = Rect.MinMaxRect(
                Mathf.Clamp(lower.x, outer.xMin, outer.xMax),
                Mathf.Clamp(lower.y, outer.yMin, outer.yMax),
                Mathf.Clamp(upper.x, outer.xMin, outer.xMax),
                Mathf.Clamp(upper.y, outer.yMin, outer.yMax));

            if (_hasLayout && outer == _lastOuter && inner == _lastInner)
                return;

            // 坐标和尺寸均为 Canvas 单位；左右覆盖全高，上下只覆盖中间宽度。
            SetRegion(_left, Rect.MinMaxRect(outer.xMin, outer.yMin, inner.xMin, outer.yMax));
            SetRegion(_right, Rect.MinMaxRect(inner.xMax, outer.yMin, outer.xMax, outer.yMax));
            SetRegion(_top, Rect.MinMaxRect(inner.xMin, inner.yMax, inner.xMax, outer.yMax));
            SetRegion(_bottom, Rect.MinMaxRect(inner.xMin, outer.yMin, inner.xMax, inner.yMin));
            _lastOuter = outer;
            _lastInner = inner;
            _hasLayout = true;
        }

        private static void SetRegion(RectTransform region, Rect bounds)
        {
            region.anchoredPosition = bounds.center;
            region.sizeDelta = bounds.size;
            bool visible = bounds.width > 0 && bounds.height > 0;
            if (region.gameObject.activeSelf != visible)
                region.gameObject.SetActive(visible);
        }
    }
}

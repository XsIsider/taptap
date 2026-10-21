using UnityEngine;
using UnityEngine.UI;

namespace Castle.V24
{
    /// <summary>场景中预置的声纹画布组件。只更新网格，不在运行时创建 UI 对象。</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class V24EndingWave : Graphic
    {
        public bool Playing;
        private void Update() { if (Playing) SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear(); var rect = rectTransform.rect;
            for (int band = 0; band < 15; band++)
            {
                Vector2 previous = Vector2.zero;
                for (int i = 0; i <= 180; i++)
                {
                    float x = i / 180f;
                    float envelope = Mathf.Pow(Mathf.Max(0, Mathf.Sin(x * Mathf.PI)), 1.5f);
                    float phase = Playing ? Time.unscaledTime * 1.8f : 0;
                    float y = Mathf.Sin(x * 22 + band * .06f + phase) * envelope * (rect.height * (.15f + band * .017f));
                    var current = new Vector2(rect.xMin + x * rect.width, rect.center.y + y);
                    if (i > 0)
                    {
                        var normal = new Vector2(-(current - previous).y, (current - previous).x).normalized * .6f;
                        int start = vertices.currentVertCount;
                        var tint = color; tint.a *= .75f;
                        vertices.AddVert(previous - normal, tint, Vector2.zero); vertices.AddVert(previous + normal, tint, Vector2.zero);
                        vertices.AddVert(current + normal, tint, Vector2.zero); vertices.AddVert(current - normal, tint, Vector2.zero);
                        vertices.AddTriangle(start, start + 1, start + 2); vertices.AddTriangle(start, start + 2, start + 3);
                    }
                    previous = current;
                }
            }
        }
    }
}

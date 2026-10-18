using System;
using System.Linq;
using UnityEngine;

namespace Castle.Flow
{
    /// <summary>场景固定页面与动态列表模板的序列化引用；运行时不创建 Canvas 或页面壳。</summary>
    public sealed class CastleFlowUi : MonoBehaviour
    {
        public CastleUiView[] Pages = Array.Empty<CastleUiView>(), Modals = Array.Empty<CastleUiView>();
        public CastleUiView Hud, Toast;
        public GameObject ModalRoot;
        public CastleUiView CardPrefab, TrackPrefab, LinePrefab;
        public CastleUiView View(string id) => Pages.Concat(Modals).Concat(new[] { Hud, Toast }).First(view => view && view.Id == id);
        public void ValidateBindings()
        {
            if (!ModalRoot || !CardPrefab || !TrackPrefab || !LinePrefab) throw new InvalidOperationException("UI 模板引用缺失。");
            foreach (var view in Pages.Concat(Modals).Concat(new[] { Hud, Toast, CardPrefab, TrackPrefab, LinePrefab })) view.ValidateBindings();
        }
    }
}

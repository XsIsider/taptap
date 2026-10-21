using System.Linq;
using UnityEngine;

namespace Castle.V24
{
    public sealed partial class CastleV24Controller
    {
        private Texture2D _scaledCursor;
        public Texture2D WorkbenchCursor;
        private Texture2D _activeCursor;

        private void SetTalkingCursor()
        {
            // 原指针导入上限为 32 像素，按 80% 缩小；原 PNG 与热点不变。
            int width = Mathf.Max(1, Mathf.RoundToInt(TalkingCursor.width * .8f));
            int height = Mathf.Max(1, Mathf.RoundToInt(TalkingCursor.height * .8f));
            _scaledCursor = new Texture2D(width,height,TextureFormat.RGBA32,false);
            for (int y=0;y<height;y++)
                for (int x=0;x<width;x++)
                    _scaledCursor.SetPixel(x,y,TalkingCursor.GetPixelBilinear((x+.5f)/width,(y+.5f)/height));
            _scaledCursor.Apply();
            Cursor.SetCursor(_scaledCursor,Vector2.zero,CursorMode.Auto);
            _activeCursor = _scaledCursor;
        }

        private void UpdatePageCursor()
        {
            bool workbench = _modal.Count > 0 ? _modal.Last() == "Path"
                : _page == "Recordings" || _page == "Anchors" || _page == "Space";
            var texture = workbench && WorkbenchCursor ? WorkbenchCursor : _scaledCursor;
            if (_activeCursor == texture) return;
            Cursor.SetCursor(texture, Vector2.zero, CursorMode.Auto);
            _activeCursor = texture;
        }

        private void OnDestroy()
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            if (_scaledCursor) Destroy(_scaledCursor);
        }
    }
}

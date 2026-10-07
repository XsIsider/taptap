using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Castle.V2
{
    // 可替换的空面板 Prefab；页面创建时构建一次，播放/拖动仅更新现有元素。
    public sealed class UiFactory
    {
        public readonly TMP_FontAsset Font;
        public static readonly Color Ink = new Color32(20, 34, 28, 255), Gold = new Color32(216, 184, 127, 255), Paper = new Color32(235, 225, 203, 255), Panel = new Color32(30, 47, 36, 245);
        public UiFactory(TMP_FontAsset font) { Font = font; }
        public RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            var obj = new GameObject(name, typeof(RectTransform)); var rect = (RectTransform)obj.transform; rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height); return rect;
        }
        public RectTransform Box(Transform parent, string name, float x, float y, float width, float height, Color color)
        { var rect = Rect(parent, name, x, y, width, height); rect.gameObject.AddComponent<Image>().color = color; return rect; }
        public TMP_Text Text(Transform parent, string text, float x, float y, float width, float height, int size = 24, Color? color = null)
        {
            var label = Rect(parent, "Text", x, y, width, height).gameObject.AddComponent<TextMeshProUGUI>(); label.font = Font; label.text = text; label.fontSize = size; label.color = color ?? Paper; label.raycastTarget = false; label.enableWordWrapping = true; label.overflowMode = TextOverflowModes.Ellipsis; return label;
        }
        public Button Button(Transform parent, string title, float x, float y, float width, Action click, bool enabled = true, float height = 50)
        {
            var rect = Box(parent, title, x, y, width, height, Panel); var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = rect.GetComponent<Image>(); button.interactable = enabled;
            foreach (var edge in new[] { Box(rect, "Top border", 0, 0, width, 1, Gold), Box(rect, "Bottom border", 0, height - 1, width, 1, Gold), Box(rect, "Left border", 0, 0, 1, height, Gold), Box(rect, "Right border", width - 1, 0, 1, height, Gold) }) edge.GetComponent<Image>().raycastTarget = false;
            var label = Text(rect, title, 10, 5, width - 20, height - 10, 21, Gold); label.alignment = TextAlignmentOptions.Midline; button.onClick.AddListener(() => click()); return button;
        }
        public RectTransform Header(Transform parent, string title, Action back = null)
        {
            var bar = Box(parent, "Header", 0, 0, 1600, 82, Panel);
            Text(bar, title, 35, 16, 720, 48, 29, Gold);
            if (back != null) Button(bar, "返回", 1410, 14, 140, back, true, 48);
            return bar;
        }
        public Button ArtButton(Transform parent, string title, float x, float y, float width, Action click, bool enabled = true, bool primary = false)
        {
            var rect = Rect(parent, title, x, y, width, 62);
            var image = rect.gameObject.AddComponent<RawImage>();
            image.texture = Resources.Load<Texture2D>("Castle/UiArt/" + (primary ? "MenuPrimary" : "MenuSecondary"));
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.interactable = enabled;
            Text(rect, title, 20, 10, width - 40, 42, 25, primary ? Ink : Gold).alignment = TextAlignmentOptions.Midline;
            var colors = button.colors; colors.disabledColor = new Color(.45f, .45f, .45f); button.colors = colors;
            button.onClick.AddListener(() => click()); return button;
        }
        public RectTransform TabBar(Transform parent, string[] labels, int selected, Action<int> select)
        {
            var bar = Box(parent, "Tabs", 70, 92, 1460, 58, new Color(0.08f, 0.14f, 0.11f, .96f));
            float width = 1460f / labels.Length;
            for (int i = 0; i < labels.Length; i++) { int index = i; Button(bar, (i == selected ? "◆ " : "") + labels[i], i * width, 4, width - 4, () => select(index), true, 50); }
            return bar;
        }
        public RectTransform StatusBadge(Transform parent, string text, float x, float y, Color color)
        { var badge = Box(parent, "Status", x, y, 210, 36, color); Text(badge, text, 8, 4, 194, 28, 17, Ink).alignment = TextAlignmentOptions.Center; return badge; }
        public void Picture(Transform parent, Texture texture, float x, float y, float width, float height)
        { if (!texture) return; var image = Rect(parent, "Image", x, y, width, height).gameObject.AddComponent<RawImage>(); image.texture = texture; image.raycastTarget = false; }
        public RectTransform Scroll(Transform parent, float x, float y, float width, float height, float contentHeight)
        {
            var rect = Box(parent, "Scroll", x, y, width, height, new Color(0, 0, 0, .08f)); var scroll = rect.gameObject.AddComponent<ScrollRect>(); rect.gameObject.AddComponent<RectMask2D>();
            var content = Rect(rect, "Content", 0, 0, width - 12, Mathf.Max(height, contentHeight)); scroll.viewport = rect; scroll.content = content; scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 40; return content;
        }
        public Slider Slider(Transform parent, float x, float y, float width, float min, float max, float value, Action<float> change)
        {
            var rect = Rect(parent, "Slider", x, y, width, 40); Box(rect, "Track", 0, 17, width, 6, Gold);
            var area = Rect(rect, "Handle Area", 10, 0, width - 20, 40); var handle = Box(area, "Handle", 0, 0, 20, 40, Paper); handle.pivot = new Vector2(.5f, .5f); handle.sizeDelta = new Vector2(20, 0);
            var slider = rect.gameObject.AddComponent<Slider>(); slider.handleRect = handle; slider.targetGraphic = handle.GetComponent<Image>(); slider.minValue = min; slider.maxValue = max; slider.SetValueWithoutNotify(value); slider.onValueChanged.AddListener(v => change(v)); return slider;
        }
    }
}

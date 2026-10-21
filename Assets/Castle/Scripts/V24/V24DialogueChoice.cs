using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Castle.V24
{
    /// <summary>对白选项边框与金色纹理；鼠标悬停和键盘选中使用同一高亮。</summary>
    public sealed class V24DialogueChoice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler, IPointerDownHandler, IPointerUpHandler
    {
        public Image Border;
        public GameObject Glow;
        public Sprite FirstBorder, RegularBorder;
        private bool _hovered, _selected, _pressed;

        public void SetFirst(bool first)
        {
            Border.sprite = first ? FirstBorder : RegularBorder;
            GetComponent<LayoutElement>().preferredHeight = first ? 54 : 46;
            Refresh();
        }
        public void OnPointerEnter(PointerEventData eventData) { _hovered = true;Refresh(); }
        public void OnPointerExit(PointerEventData eventData) { _hovered = false;_pressed = false;Refresh(); }
        public void OnSelect(BaseEventData eventData) { _selected = true;Refresh(); }
        public void OnDeselect(BaseEventData eventData) { _selected = false;Refresh(); }
        public void OnPointerDown(PointerEventData eventData) { if (eventData.button == PointerEventData.InputButton.Left) _pressed = true;Refresh(); }
        public void OnPointerUp(PointerEventData eventData) { _pressed = false;Refresh(); }
        private void OnDisable() { _hovered = _selected = _pressed = false; }
        private void Refresh()
        {
            bool active = _hovered || _selected || _pressed;
            Border.color = new Color(1, 1, 1, _pressed ? .85f : active ? .65f : .28f);
            Glow.SetActive(active);
        }
    }
}

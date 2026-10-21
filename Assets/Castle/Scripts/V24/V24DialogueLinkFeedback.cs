using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Castle.V24
{
    /// <summary>人物姓名链接悬停时稍微透明并放大字形；不接管点击和拖动事件。</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class V24DialogueLinkFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField, Range(0f, 1f)] private float _hoverAlpha = 0.85f;
        [SerializeField, Range(1f, 1.2f)] private float _hoverScale = 1.05f;

        private readonly HashSet<string> _linkIds = new HashSet<string>();
        private TextMeshProUGUI _text;
        private PointerEventData _pointer;
        private Camera _eventCamera;
        private Func<bool> _canInteract;
        private int _highlightedLink = -1;

        /// <summary>只登记本条对白的人物姓名链接；允许交互时才显示反馈。</summary>
        public void Configure(IEnumerable<string> linkIds, Func<bool> canInteract)
        {
            SetHighlight(-1);
            _linkIds.Clear();
            foreach (var id in linkIds) _linkIds.Add(id);
            _canInteract = canInteract;
        }

        private void OnEnable()
        {
            _text = GetComponent<TextMeshProUGUI>();
            _text.OnPreRenderText += ApplyFeedback;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _pointer = eventData;
            _eventCamera = eventData.enterEventCamera;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _pointer = null;
            SetHighlight(-1);
        }

        private void LateUpdate()
        {
            if (_pointer == null || _linkIds.Count == 0 || _pointer.dragging || _canInteract == null || !_canInteract())
            {
                SetHighlight(-1);
                return;
            }

            int index = TMP_TextUtilities.FindIntersectingLink(_text, _pointer.position, _eventCamera);
            if (index >= 0 && !_linkIds.Contains(_text.textInfo.linkInfo[index].GetLinkID())) index = -1;
            // 按下时沿用悬停外观，不新增 PointerDown，避免抢走链接或滚动输入。
            SetHighlight(index);
        }

        private void OnDisable()
        {
            _pointer = null;
            if (!_text) return;
            _text.OnPreRenderText -= ApplyFeedback;
            SetHighlight(-1);
        }

        private void SetHighlight(int index)
        {
            if (_highlightedLink == index) return;
            _highlightedLink = index;
            // 每次从原始字形重建，避免反复悬停累积缩放；不改正文、换行与链接命中区。
            if (_text) _text.ForceMeshUpdate(true);
        }

        private void ApplyFeedback(TMP_TextInfo textInfo)
        {
            if (_highlightedLink < 0 || _highlightedLink >= textInfo.linkCount) return;
            var link = textInfo.linkInfo[_highlightedLink];
            if (!_linkIds.Contains(link.GetLinkID())) return;

            int end = Mathf.Min(link.linkTextfirstCharacterIndex + link.linkTextLength, textInfo.characterCount);
            for (int start = link.linkTextfirstCharacterIndex; start < end;)
            {
                int lineEnd = start + 1;
                while (lineEnd < end && textInfo.characterInfo[lineEnd].lineNumber == textInfo.characterInfo[start].lineNumber)
                    lineEnd++;

                // 跨行姓名按每行各自的中心放大，保留原排版与阅读位置。
                var min = new Vector3(float.MaxValue, float.MaxValue, 0f);
                var max = new Vector3(float.MinValue, float.MinValue, 0f);
                for (int i = start; i < lineEnd; i++)
                {
                    var character = textInfo.characterInfo[i];
                    if (!character.isVisible) continue;
                    min = Vector3.Min(min, character.bottomLeft);
                    max = Vector3.Max(max, character.topRight);
                }
                var center = (min + max) * 0.5f;
                for (int i = start; i < lineEnd; i++)
                {
                    var character = textInfo.characterInfo[i];
                    if (!character.isVisible) continue;
                    var mesh = textInfo.meshInfo[character.materialReferenceIndex];
                    for (int vertex = character.vertexIndex; vertex < character.vertexIndex + 4; vertex++)
                    {
                        mesh.vertices[vertex] = center + (mesh.vertices[vertex] - center) * _hoverScale;
                        var color = mesh.colors32[vertex];
                        color.a = (byte)Mathf.RoundToInt(color.a * _hoverAlpha);
                        mesh.colors32[vertex] = color;
                    }
                }
                start = lineEnd;
            }
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;

namespace Castle
{
    public sealed class CastleJournalCardView : MonoBehaviour
    {
        public Button OpenButton;
        public Text CategoryText, TitleText, SummaryText;
        public string EntryId { get; private set; }
        Action _open;
        bool _bound;
        public void Bind(string id, string category, string title, string summary, Action open)
        {
            if (!_bound) { OpenButton.onClick.AddListener(Open); _bound = true; }
            EntryId = id; _open = open;
            CategoryText.text = category; TitleText.text = title; SummaryText.text = summary;
            gameObject.SetActive(true);
        }
        void Open() { _open?.Invoke(); }
        public void Release() { _open = null; EntryId = null; gameObject.SetActive(false); }
        void OnDestroy() { if (_bound && OpenButton) OpenButton.onClick.RemoveListener(Open); }
    }
}

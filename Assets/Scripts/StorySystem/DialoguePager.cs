using TMPro;
using UnityEngine;

namespace StorySystem
{
    public sealed class DialoguePager
    {
        private readonly TMP_Text _text;
        public int Page => _text.pageToDisplay;
        public int PageCount { get { _text.ForceMeshUpdate(); return Mathf.Max(1, _text.textInfo.pageCount); } }

        public DialoguePager(TMP_Text text)
        {
            _text = text;
            _text.overflowMode = TextOverflowModes.Page;
            _text.textWrappingMode = TextWrappingModes.Normal;
        }

        public void SetText(string text)
        {
            _text.text = FuriganaProcessor.Process(text ?? "");
            _text.pageToDisplay = 1;
            _text.ForceMeshUpdate();
        }

        public bool Advance()
        {
            if (_text.pageToDisplay >= PageCount) return true;
            _text.pageToDisplay++;
            return false;
        }

        public void ShowLastPage() { _text.pageToDisplay = PageCount; }
    }
}

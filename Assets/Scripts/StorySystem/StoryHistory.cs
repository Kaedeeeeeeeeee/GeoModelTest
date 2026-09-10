using System;
using System.Collections.Generic;
using UnityEngine;

namespace StorySystem
{
    [Serializable]
    public sealed class StoryHistoryEntry
    {
        public string speaker;
        public string text;
        public string kind;
        public string contentId;
        public string language;
        public string scene;
    }

    public static class StoryHistory
    {
        public const string StorageKey = "StorySystem.History.v1";
        [Serializable]
        private sealed class State
        {
            public string runId;
            public List<StoryHistoryEntry> entries = new List<StoryHistoryEntry>();
        }
        private static State _state;
        private static State Current
        {
            get
            {
                string run = QuizScoreManager.Instance.RunId;
                if (_state == null || _state.runId != run)
                {
                    try { _state = JsonUtility.FromJson<State>(PlayerPrefs.GetString(StorageKey, "")); }
                    catch { _state = null; }
                    if (_state == null || _state.runId != run) _state = new State { runId = run };
                    _state.entries ??= new List<StoryHistoryEntry>();
                }
                return _state;
            }
        }

        public static IReadOnlyList<StoryHistoryEntry> Entries => Current.entries;

        public static void Append(string speaker, string text, string kind = "dialogue", string contentId = "")
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            var entries = Current.entries;
            string language = LocalizationManager.Instance.CurrentLanguage.ToString();
            if (entries.Count > 0)
            {
                var previous = entries[entries.Count - 1];
                if (previous.speaker == speaker && previous.text == text && previous.kind == kind && previous.language == language) return;
            }
            entries.Add(new StoryHistoryEntry
            {
                speaker = speaker ?? "", text = text, kind = kind, contentId = contentId ?? "",
                language = language, scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            });
            // Bound local storage for long-running saves. Only displayed content is recorded.
            if (entries.Count > 600) entries.RemoveRange(0, entries.Count - 600);
            PlayerPrefs.SetString(StorageKey, JsonUtility.ToJson(Current));
            PlayerPrefs.Save();
        }

        public static void Reset()
        {
            _state = null;
            PlayerPrefs.DeleteKey(StorageKey);
            PlayerPrefs.Save();
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace StorySystem
{
    /// <summary>Only acknowledged lines are passed; the current page remains replayable after interruption.</summary>
    public static class StoryCheckpoint
    {
        public const string StorageKey = "StorySystem.Checkpoints.v1";
        [Serializable]
        private sealed class State
        {
            public string runId;
            public List<string> passed = new List<string>();
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
                    _state.passed ??= new List<string>();
                }
                return _state;
            }
        }
        public static bool IsPassed(string id) => !string.IsNullOrEmpty(id) && Current.passed.Contains(id);
        public static void Acknowledge(string id)
        {
            if (string.IsNullOrEmpty(id) || IsPassed(id)) return;
            Current.passed.Add(id);
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

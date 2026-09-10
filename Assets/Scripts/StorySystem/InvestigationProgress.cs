using System;
using UnityEngine;
using QuestSystem;

namespace StorySystem
{
    /// <summary>Versioned, run-scoped investigation milestones; never counts repeated tool clicks.</summary>
    public static class InvestigationProgress
    {
        public const string StorageKey = "StorySystem.Investigation.v1";
        private static State _state;
        public static event Action Changed;

        [Serializable]
        private sealed class State
        {
            public string runId;
            public string coreSampleId;
            public bool fieldSample;
            public bool laboratoryAnalysis;
            public bool completed;
        }

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
                }
                return _state;
            }
        }

        public static bool HasCore => !string.IsNullOrEmpty(Current.coreSampleId);
        public static bool IsComplete => Current.completed;
        public static bool NeedsLabAnalysis => HasCore && !IsComplete;
        public static int ActivityCount => (Current.fieldSample ? 1 : 0) + (HasCore ? 1 : 0) +
            (Current.laboratoryAnalysis ? 1 : 0) + (IsComplete ? 1 : 0);
        public const int ActivityTotal = 4;
        public const int StepTotal = 10;

        public static bool AcceptCoreSample(string sampleId, string sourceToolId, bool isField, bool questActive)
        {
            if (!isField || !questActive || sourceToolId != "1001" || string.IsNullOrWhiteSpace(sampleId) || HasCore)
                return false;
            Current.coreSampleId = sampleId;
            Save();
            return true;
        }

        public static void MarkFieldSample() { Current.fieldSample = true; Save(); }
        public static void MarkLabAnalysis() { Current.laboratoryAnalysis = true; Save(); }
        public static void MarkComplete() { Current.completed = true; Save(); }

        public static void MigrateLegacyCompletion(bool legacyCompleted)
        {
            // Preserve known completion without inventing practice evidence for old saves.
            if (legacyCompleted && !IsComplete) MarkComplete();
        }

        public static int GetStep()
        {
            if (IsComplete) return 10;
            var qm = QuestManager.Instance;
            if (NeedsLabAnalysis) return qm.GetQuestStatus("q.chapter4.return") == QuestStatus.InProgress ? 9 : 8;
            string[] stages = { "q.lab.intro", "q.lab.drkaede", "q.lab.anomaly", "q.field.phase", "q.lab.return",
                "q.chapter4.kaede", "q.chapter4.field", "q.chapter4.sample" };
            for (int i = stages.Length - 1; i >= 0; i--)
                if (qm.GetQuestStatus(stages[i]) != QuestStatus.NotStarted) return i + 1;
            return 1;
        }

        public static void Reset()
        {
            _state = null;
            PlayerPrefs.DeleteKey(StorageKey);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        private static void Save()
        {
            PlayerPrefs.SetString(StorageKey, JsonUtility.ToJson(Current));
            PlayerPrefs.Save();
            Core.GameEventBus.RaiseProgressDirty("investigation_milestone");
            Changed?.Invoke();
        }
    }
}

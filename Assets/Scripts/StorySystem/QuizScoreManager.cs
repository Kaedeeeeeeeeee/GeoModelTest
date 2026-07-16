using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace StorySystem
{
    [Serializable]
    public sealed class QuizAttempt
    {
        public string eventId;
        public string runId;
        public string questionId;
        public string questionVersion;
        public string choiceId;
        public int attemptIndex;
        public bool isCorrect;
        public bool usedHint;
        public long responseTimeMs;
        public string occurredAt;
    }

    public sealed class QuizSummary
    {
        public int ExpectedQuestionCount { get; internal set; }
        public int AnsweredQuestionCount { get; internal set; }
        public int FirstCorrectCount { get; internal set; }
        public int FinalMasteredCount { get; internal set; }
        public int HintUsedQuestionCount { get; internal set; }
        public float AverageAttemptCount { get; internal set; }

        public float CompletionRate => ExpectedQuestionCount == 0
            ? 0f
            : (float)AnsweredQuestionCount / ExpectedQuestionCount;

        public float FirstCorrectRate => ExpectedQuestionCount == 0
            ? 0f
            : (float)FirstCorrectCount / ExpectedQuestionCount;

        public float FinalMasteryRate => ExpectedQuestionCount == 0
            ? 0f
            : (float)FinalMasteredCount / ExpectedQuestionCount;

        public float HintUsageRate => AnsweredQuestionCount == 0
            ? 0f
            : (float)HintUsedQuestionCount / AnsweredQuestionCount;
    }

    /// <summary>
    /// 追加式の形成的クイズ記録。現在の研究用ストーリー経路では11問を固定分母として扱う。
    /// 回答履歴は現在のプレイ run ごとに PlayerPrefs へ保存し、シーン再読込後も維持する。
    /// </summary>
    public sealed class QuizScoreManager
    {
        public const string PersistedStatePrefsKey = "StorySystem.QuizAttemptState.v2";
        public const string DefaultQuestionVersion = "story-formative-v1";

        private static readonly string[] ExpectedQuestionIdsInternal =
        {
            "q.weathering_order",
            "q.rock_mudstone",
            "q.rock_limestone",
            "q.rock_chert",
            "q.fossil_coral_env",
            "q.fossil_facies_term",
            "q.fossil_ammonite_era",
            "q.fossil_index_term",
            "q.tuff_volcano",
            "q.strata_tilt",
            "q.fold_term"
        };

        private static readonly HashSet<string> ExpectedQuestionIdSet =
            new HashSet<string>(ExpectedQuestionIdsInternal, StringComparer.Ordinal);

        private static QuizScoreManager _instance;
        public static QuizScoreManager Instance => _instance ??= new QuizScoreManager();

        private QuizAttemptState _state;

        public event Action<QuizAttempt> AttemptRecorded;

        private QuizScoreManager()
        {
            ReloadFromPersistence();
        }

        public IReadOnlyList<string> ExpectedQuestionIds => ExpectedQuestionIdsInternal;
        public IReadOnlyList<QuizAttempt> Attempts => _state.attempts;
        public string RunId => _state.runId;
        public int Total => ExpectedQuestionIdsInternal.Length;
        public int CorrectCount => BuildSummary().FirstCorrectCount;
        public float Ratio => BuildSummary().FirstCorrectRate;

        public QuizAttempt Record(
            string questionId,
            string questionVersion,
            string choiceId,
            bool isCorrect,
            bool usedHint,
            long responseTimeMs,
            string occurredAt = null)
        {
            if (string.IsNullOrWhiteSpace(questionId))
            {
                throw new ArgumentException("questionId is required", nameof(questionId));
            }

            if (!ExpectedQuestionIdSet.Contains(questionId))
            {
                throw new ArgumentOutOfRangeException(nameof(questionId), questionId, "Unknown research-route questionId");
            }

            if (string.IsNullOrWhiteSpace(choiceId))
            {
                throw new ArgumentException("choiceId is required", nameof(choiceId));
            }

            int attemptIndex = _state.attempts.Count(attempt =>
                string.Equals(attempt.runId, _state.runId, StringComparison.Ordinal) &&
                string.Equals(attempt.questionId, questionId, StringComparison.Ordinal)) + 1;

            var attempt = new QuizAttempt
            {
                eventId = Guid.NewGuid().ToString("D"),
                runId = _state.runId,
                questionId = questionId,
                questionVersion = string.IsNullOrWhiteSpace(questionVersion)
                    ? DefaultQuestionVersion
                    : questionVersion.Trim(),
                choiceId = choiceId.Trim(),
                attemptIndex = attemptIndex,
                isCorrect = isCorrect,
                usedHint = usedHint,
                responseTimeMs = Math.Min(3600000L, Math.Max(0L, responseTimeMs)),
                occurredAt = NormalizeOccurredAt(occurredAt)
            };

            _state.attempts.Add(attempt);
            Persist();
            AttemptRecorded?.Invoke(attempt);
            return attempt;
        }

        public QuizSummary BuildSummary()
        {
            var currentRunAttempts = _state.attempts
                .Where(attempt =>
                    attempt != null &&
                    string.Equals(attempt.runId, _state.runId, StringComparison.Ordinal) &&
                    ExpectedQuestionIdSet.Contains(attempt.questionId))
                .OrderBy(attempt => attempt.attemptIndex)
                .ToList();

            var groups = currentRunAttempts
                .GroupBy(attempt => attempt.questionId, StringComparer.Ordinal)
                .ToList();

            var summary = new QuizSummary
            {
                ExpectedQuestionCount = ExpectedQuestionIdsInternal.Length,
                AnsweredQuestionCount = groups.Count,
                FirstCorrectCount = groups.Count(group => group.First().isCorrect),
                FinalMasteredCount = groups.Count(group => group.Last().isCorrect),
                HintUsedQuestionCount = groups.Count(group => group.Any(attempt => attempt.usedHint)),
                AverageAttemptCount = groups.Count == 0
                    ? 0f
                    : (float)groups.Average(group => group.Count())
            };

            return summary;
        }

        /// <summary>
        /// 「最初から始める」時だけ呼び出す。新しい runId を発行し、現在の報告対象を空にする。
        /// </summary>
        public void StartNewRun()
        {
            _state = CreateEmptyState();
            Persist();
        }

        public void Reset()
        {
            StartNewRun();
        }

        public void ReloadFromPersistence()
        {
            string json = PlayerPrefs.GetString(PersistedStatePrefsKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                _state = CreateEmptyState();
                Persist();
                return;
            }

            try
            {
                _state = JsonConvert.DeserializeObject<QuizAttemptState>(json) ?? CreateEmptyState();
                if (!Guid.TryParse(_state.runId, out _))
                {
                    _state.runId = Guid.NewGuid().ToString("D");
                }

                _state.attempts ??= new List<QuizAttempt>();
                _state.attempts = _state.attempts
                    .Where(IsValidPersistedAttempt)
                    .ToList();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[QuizScoreManager] Failed to load persisted attempts; starting a new run: {ex.Message}");
                _state = CreateEmptyState();
                Persist();
            }
        }

        public string Grade
        {
            get
            {
                QuizSummary summary = BuildSummary();
                if (summary.AnsweredQuestionCount == 0) return "-";
                if (summary.FirstCorrectRate >= 0.8f) return "A";
                if (summary.FirstCorrectRate >= 0.6f) return "B";
                return "C";
            }
        }

        private static QuizAttemptState CreateEmptyState()
        {
            return new QuizAttemptState
            {
                runId = Guid.NewGuid().ToString("D"),
                attempts = new List<QuizAttempt>()
            };
        }

        private static bool IsValidPersistedAttempt(QuizAttempt attempt)
        {
            return attempt != null &&
                   Guid.TryParse(attempt.eventId, out _) &&
                   Guid.TryParse(attempt.runId, out _) &&
                   ExpectedQuestionIdSet.Contains(attempt.questionId) &&
                   !string.IsNullOrWhiteSpace(attempt.choiceId) &&
                   attempt.attemptIndex > 0 &&
                   attempt.responseTimeMs >= 0 &&
                   DateTimeOffset.TryParse(attempt.occurredAt, out _);
        }

        private static string NormalizeOccurredAt(string occurredAt)
        {
            if (!string.IsNullOrWhiteSpace(occurredAt) &&
                DateTimeOffset.TryParse(occurredAt, out DateTimeOffset parsed))
            {
                return parsed.ToUniversalTime().ToString("o");
            }

            return DateTimeOffset.UtcNow.ToString("o");
        }

        private void Persist()
        {
            string json = JsonConvert.SerializeObject(_state);
            PlayerPrefs.SetString(PersistedStatePrefsKey, json);
            PlayerPrefs.Save();
        }

        [Serializable]
        private sealed class QuizAttemptState
        {
            public string runId;
            public List<QuizAttempt> attempts = new List<QuizAttempt>();
        }
    }
}

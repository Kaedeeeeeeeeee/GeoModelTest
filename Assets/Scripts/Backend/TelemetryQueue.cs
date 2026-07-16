using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace Backend
{
    /// <summary>
    /// 研究参加者・セッションに紐づいたイベントと回答を永続化するオフラインキュー。
    /// v1以前の参加者情報を持たないイベントは研究データとして送信しない。
    /// </summary>
    public sealed class TelemetryQueue
    {
        public const string PendingEventsPrefsKey = "Backend.PendingTelemetry";
        public const int MaxEventPropsBytes = 8192;
        private static readonly TimeSpan MaxQueuedAge = TimeSpan.FromDays(30);
        private static readonly TimeSpan MaxFutureSkew = TimeSpan.FromMinutes(5);

        private readonly List<TelemetryEvent> _events = new List<TelemetryEvent>();
        private readonly List<QuizAttemptUpload> _quizAttempts = new List<QuizAttemptUpload>();
        private readonly int _maxPersistedItems;

        public TelemetryQueue(int maxPersistedItems)
        {
            _maxPersistedItems = Mathf.Clamp(maxPersistedItems, 10, 1000);
            LoadPersisted();
        }

        public int Count => _events.Count + _quizAttempts.Count;

        public static TelemetryEvent Create(string eventName, string sceneName, Dictionary<string, object> props = null)
        {
            return new TelemetryEvent
            {
                id = Guid.NewGuid().ToString("D"),
                name = eventName,
                occurredAt = DateTimeOffset.UtcNow.ToString("o"),
                sceneName = sceneName,
                props = props ?? new Dictionary<string, object>()
            };
        }

        public bool Enqueue(TelemetryEvent telemetryEvent)
        {
            if (!HasResearchBinding(telemetryEvent) ||
                string.IsNullOrWhiteSpace(telemetryEvent.name) ||
                !IsUploadableTimestamp(telemetryEvent.occurredAt))
            {
                return false;
            }

            telemetryEvent.props = ClampProps(telemetryEvent.props);
            _events.Add(telemetryEvent);
            TrimToCapacity();
            Persist();
            return true;
        }

        public bool EnqueueQuizAttempt(QuizAttemptUpload quizAttempt)
        {
            if (!HasResearchBinding(quizAttempt) ||
                !Guid.TryParse(quizAttempt.eventId, out _) ||
                !Guid.TryParse(quizAttempt.runId, out _) ||
                string.IsNullOrWhiteSpace(quizAttempt.questionId) ||
                string.IsNullOrWhiteSpace(quizAttempt.choiceId) ||
                quizAttempt.attemptIndex <= 0 ||
                !IsUploadableTimestamp(quizAttempt.occurredAt))
            {
                return false;
            }

            _quizAttempts.Add(quizAttempt);
            TrimToCapacity();
            Persist();
            return true;
        }

        public TelemetryBatch PeekBatch(int maxBatchSize)
        {
            int limit = Mathf.Clamp(maxBatchSize, 1, 100);
            var batch = new TelemetryBatch();
            ResearchBinding binding = FindOldestBinding();
            if (binding == null)
            {
                return batch;
            }

            batch.participantId = binding.participantId;
            batch.studyId = binding.studyId;
            batch.condition = binding.condition;
            batch.sessionId = binding.sessionId;

            batch.events = _events
                .Where(value => Matches(value, binding))
                .Take(limit)
                .ToList();

            int remaining = limit - batch.events.Count;
            if (remaining > 0)
            {
                batch.quizAttempts = _quizAttempts
                    .Where(value => Matches(value, binding))
                    .Take(remaining)
                    .ToList();
            }

            return batch;
        }

        public void RemoveSent(IEnumerable<string> eventIds, IEnumerable<string> quizAttemptIds)
        {
            var sentEvents = new HashSet<string>(eventIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            var sentAttempts = new HashSet<string>(quizAttemptIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            _events.RemoveAll(value => sentEvents.Contains(value.id));
            _quizAttempts.RemoveAll(value => sentAttempts.Contains(value.eventId));
            Persist();
        }

        public void Clear()
        {
            _events.Clear();
            _quizAttempts.Clear();
            PlayerPrefs.DeleteKey(PendingEventsPrefsKey);
            PlayerPrefs.Save();
        }

        public void Persist()
        {
            TrimToCapacity();
            var persisted = new PersistedTelemetryQueue
            {
                events = _events,
                quizAttempts = _quizAttempts
            };
            string json = JsonConvert.SerializeObject(persisted, BackendJson.Settings);
            PlayerPrefs.SetString(PendingEventsPrefsKey, json);
            PlayerPrefs.Save();
        }

        public static int EstimateJsonBytes(object value)
        {
            string json = JsonConvert.SerializeObject(value, BackendJson.Settings);
            return Encoding.UTF8.GetByteCount(json);
        }

        private void LoadPersisted()
        {
            string json = PlayerPrefs.GetString(PendingEventsPrefsKey, "");
            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            try
            {
                var persisted = JsonConvert.DeserializeObject<PersistedTelemetryQueue>(json, BackendJson.Settings);
                _events.Clear();
                _quizAttempts.Clear();
                if (persisted?.events != null)
                {
                    _events.AddRange(persisted.events.Where(value =>
                        HasResearchBinding(value) &&
                        !string.IsNullOrWhiteSpace(value.name) &&
                        IsUploadableTimestamp(value.occurredAt)));
                }

                if (persisted?.quizAttempts != null)
                {
                    _quizAttempts.AddRange(persisted.quizAttempts.Where(value =>
                        HasResearchBinding(value) &&
                        IsUploadableTimestamp(value.occurredAt)));
                }

                TrimToCapacity();
                Persist();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TelemetryQueue] Pending queue load failed, clearing stale data: {ex.Message}");
                Clear();
            }
        }

        private static bool HasResearchBinding(TelemetryEvent value)
        {
            return value != null &&
                   Guid.TryParse(value.participantId, out _) &&
                   Guid.TryParse(value.studyId, out _) &&
                   Guid.TryParse(value.sessionId, out _) &&
                   !string.IsNullOrWhiteSpace(value.condition);
        }

        private static bool HasResearchBinding(QuizAttemptUpload value)
        {
            return value != null &&
                   Guid.TryParse(value.participantId, out _) &&
                   Guid.TryParse(value.studyId, out _) &&
                   Guid.TryParse(value.sessionId, out _) &&
                   !string.IsNullOrWhiteSpace(value.condition);
        }

        private static bool IsUploadableTimestamp(string value)
        {
            if (!DateTimeOffset.TryParse(value, out DateTimeOffset occurredAt))
            {
                return false;
            }

            DateTimeOffset now = DateTimeOffset.UtcNow;
            return occurredAt >= now - MaxQueuedAge && occurredAt <= now + MaxFutureSkew;
        }

        private static bool Matches(TelemetryEvent value, ResearchBinding binding)
        {
            return value != null &&
                   value.participantId == binding.participantId &&
                   value.studyId == binding.studyId &&
                   value.condition == binding.condition &&
                   value.sessionId == binding.sessionId;
        }

        private static bool Matches(QuizAttemptUpload value, ResearchBinding binding)
        {
            return value != null &&
                   value.participantId == binding.participantId &&
                   value.studyId == binding.studyId &&
                   value.condition == binding.condition &&
                   value.sessionId == binding.sessionId;
        }

        private ResearchBinding FindOldestBinding()
        {
            TelemetryEvent firstEvent = _events.FirstOrDefault();
            QuizAttemptUpload firstAttempt = _quizAttempts.FirstOrDefault();
            if (firstEvent == null && firstAttempt == null)
            {
                return null;
            }

            if (firstAttempt == null ||
                (firstEvent != null && CompareOccurredAt(firstEvent.occurredAt, firstAttempt.occurredAt) <= 0))
            {
                return new ResearchBinding(firstEvent.participantId, firstEvent.studyId, firstEvent.condition, firstEvent.sessionId);
            }

            return new ResearchBinding(firstAttempt.participantId, firstAttempt.studyId, firstAttempt.condition, firstAttempt.sessionId);
        }

        private Dictionary<string, object> ClampProps(Dictionary<string, object> props)
        {
            props ??= new Dictionary<string, object>();
            int size = EstimateJsonBytes(props);
            if (size <= MaxEventPropsBytes)
            {
                return props;
            }

            return new Dictionary<string, object>
            {
                ["truncated"] = true,
                ["originalPayloadBytes"] = size
            };
        }

        private void TrimToCapacity()
        {
            while (Count > _maxPersistedItems)
            {
                TelemetryEvent firstEvent = _events.FirstOrDefault();
                QuizAttemptUpload firstAttempt = _quizAttempts.FirstOrDefault();
                if (firstAttempt == null ||
                    (firstEvent != null && CompareOccurredAt(firstEvent.occurredAt, firstAttempt.occurredAt) <= 0))
                {
                    _events.RemoveAt(0);
                }
                else
                {
                    _quizAttempts.RemoveAt(0);
                }
            }
        }

        private static int CompareOccurredAt(string left, string right)
        {
            DateTimeOffset.TryParse(left, out DateTimeOffset leftTime);
            DateTimeOffset.TryParse(right, out DateTimeOffset rightTime);
            return leftTime.CompareTo(rightTime);
        }

        private sealed class PersistedTelemetryQueue
        {
            public List<TelemetryEvent> events = new List<TelemetryEvent>();
            public List<QuizAttemptUpload> quizAttempts = new List<QuizAttemptUpload>();
        }

        private sealed class ResearchBinding
        {
            public readonly string participantId;
            public readonly string studyId;
            public readonly string condition;
            public readonly string sessionId;

            public ResearchBinding(string participantId, string studyId, string condition, string sessionId)
            {
                this.participantId = participantId;
                this.studyId = studyId;
                this.condition = condition;
                this.sessionId = sessionId;
            }
        }
    }
}

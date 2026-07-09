using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace Backend
{
    public sealed class TelemetryQueue
    {
        public const string PendingEventsPrefsKey = "Backend.PendingTelemetry";
        public const int MaxEventPropsBytes = 8192;

        private readonly List<TelemetryEvent> _events = new List<TelemetryEvent>();
        private readonly int _maxPersistedEvents;

        public TelemetryQueue(int maxPersistedEvents)
        {
            _maxPersistedEvents = Mathf.Clamp(maxPersistedEvents, 10, 500);
            LoadPersisted();
        }

        public int Count => _events.Count;

        public static TelemetryEvent Create(string eventName, string sceneName, Dictionary<string, object> props = null)
        {
            return new TelemetryEvent
            {
                id = Guid.NewGuid().ToString("D"),
                name = eventName,
                occurredAt = DateTime.UtcNow.ToString("o"),
                sceneName = sceneName,
                props = props ?? new Dictionary<string, object>()
            };
        }

        public bool Enqueue(TelemetryEvent telemetryEvent)
        {
            if (telemetryEvent == null || string.IsNullOrWhiteSpace(telemetryEvent.name))
            {
                return false;
            }

            telemetryEvent.props = ClampProps(telemetryEvent.props);
            _events.Add(telemetryEvent);
            TrimToCapacity();
            Persist();
            return true;
        }

        public List<TelemetryEvent> PeekBatch(int maxBatchSize)
        {
            int count = Mathf.Clamp(maxBatchSize, 1, 100);
            string sessionId = _events.FirstOrDefault()?.sessionId ?? "";
            if (string.IsNullOrEmpty(sessionId))
            {
                return _events.Take(count).ToList();
            }

            return _events
                .Where(e => string.IsNullOrEmpty(e.sessionId) || e.sessionId == sessionId)
                .Take(count)
                .ToList();
        }

        public void RemoveSent(IEnumerable<string> sentIds)
        {
            if (sentIds == null)
            {
                return;
            }

            var sent = new HashSet<string>(sentIds);
            _events.RemoveAll(e => sent.Contains(e.id));
            Persist();
        }

        public void Clear()
        {
            _events.Clear();
            PlayerPrefs.DeleteKey(PendingEventsPrefsKey);
            PlayerPrefs.Save();
        }

        public void Persist()
        {
            TrimToCapacity();
            var persisted = new PersistedTelemetryQueue { events = _events };
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
                if (persisted?.events == null)
                {
                    return;
                }

                _events.Clear();
                _events.AddRange(persisted.events.Where(e => e != null && !string.IsNullOrWhiteSpace(e.name)));
                TrimToCapacity();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TelemetryQueue] Pending queue load failed, clearing stale data: {ex.Message}");
                Clear();
            }
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
            while (_events.Count > _maxPersistedEvents)
            {
                _events.RemoveAt(0);
            }
        }

        private sealed class PersistedTelemetryQueue
        {
            public List<TelemetryEvent> events = new List<TelemetryEvent>();
        }
    }
}

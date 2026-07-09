using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Core;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace Backend
{
    public sealed class TelemetryClient : MonoBehaviour
    {
        private const string PendingSnapshotPrefsKey = "Backend.PendingProgressSnapshot";

        public static TelemetryClient Instance { get; private set; }

        private BackendSettings _settings;
        private TelemetryQueue _queue;
        private string _installId;
        private string _sessionId;
        private bool _initialized;
        private bool _flushRunning;
        private bool _snapshotDirty;
        private ProgressSnapshot _latestSnapshot;

        public string InstallId => _installId;
        public string SessionId => _sessionId;

        public void Initialize(BackendSettings settings)
        {
            if (_initialized)
            {
                return;
            }

            _settings = settings;
            _installId = BackendSessionStore.GetOrCreateInstallId();
            _sessionId = BackendSessionStore.CreateSessionId();
            _queue = new TelemetryQueue(_settings.MaxPersistedEvents);
            _latestSnapshot = LoadPendingSnapshot();
            _snapshotDirty = _latestSnapshot != null;
            _initialized = true;

            SubscribeEvents();
            MarkProgressDirty("startup");
            Track("session_started", new Dictionary<string, object>
            {
                ["gameVersion"] = Application.version,
                ["platform"] = Application.platform.ToString(),
                ["language"] = Application.systemLanguage.ToString()
            });

            Debug.Log($"[TelemetryClient] Initialized Supabase telemetry. session={ShortId(_sessionId)} install={ShortId(_installId)}");

            StartCoroutine(FlushLoop());
            StartCoroutine(FlushAsync(false));
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                UnsubscribeEvents();
                Instance = null;
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                StartCoroutine(FlushAsync(false));
            }
        }

        private void OnApplicationQuit()
        {
            if (!_initialized)
            {
                return;
            }

            Track("session_ended", new Dictionary<string, object>
            {
                ["reason"] = "application_quit"
            });
            _queue?.Persist();
        }

        public static void MarkProgressDirty(string reason)
        {
            if (Instance == null || !Instance._initialized)
            {
                return;
            }

            Instance.CaptureProgressSnapshot();
            Instance.Track("progress_dirty", new Dictionary<string, object>
            {
                ["reason"] = string.IsNullOrEmpty(reason) ? "unknown" : reason
            });
        }

        public void Track(string eventName, Dictionary<string, object> props = null)
        {
            if (!_initialized || _settings == null || !_settings.EnableBackend)
            {
                return;
            }

            var evt = TelemetryQueue.Create(eventName, SceneManager.GetActiveScene().name, props);
            evt.sessionId = _sessionId;
            _queue.Enqueue(evt);

            if (_queue.Count >= _settings.MaxBatchSize)
            {
                StartCoroutine(FlushAsync(false));
            }
        }

        public void FlushNow()
        {
            if (_initialized)
            {
                StartCoroutine(FlushAsync(false));
            }
        }

        private IEnumerator FlushLoop()
        {
            while (_initialized)
            {
                yield return new WaitForSeconds(_settings.FlushIntervalSeconds);
                yield return FlushAsync(false);
            }
        }

        private IEnumerator FlushAsync(bool sessionEnd)
        {
            if (_flushRunning || !_initialized || !_settings.EnableBackend || !_settings.HasClientConfig)
            {
                yield break;
            }

            List<TelemetryEvent> batch = _queue.PeekBatch(_settings.MaxBatchSize);
            if (batch.Count == 0 && !_snapshotDirty && !sessionEnd)
            {
                yield break;
            }

            _flushRunning = true;

            bool signedIn = false;
            yield return EnsureSignedIn(value => signedIn = value);
            if (!signedIn)
            {
                _flushRunning = false;
                yield break;
            }

            string requestSessionId = batch.Count > 0 && !string.IsNullOrEmpty(batch[0].sessionId)
                ? batch[0].sessionId
                : _sessionId;
            bool attachCurrentSnapshot = _snapshotDirty && requestSessionId == _sessionId;

            var requestPayload = new IngestRequest
            {
                installId = _installId,
                sessionId = requestSessionId,
                gameVersion = Application.version,
                platform = Application.platform.ToString(),
                buildTarget = GetBuildTarget(),
                language = Application.systemLanguage.ToString(),
                currentScene = SceneManager.GetActiveScene().name,
                events = batch,
                progressSnapshot = attachCurrentSnapshot ? (_latestSnapshot ?? ProgressSnapshotBuilder.Build()) : null,
                sessionEnd = sessionEnd && requestSessionId == _sessionId
                    ? new SessionEndPayload { endedAt = DateTime.UtcNow.ToString("o") }
                    : null
            };

            bool posted = false;
            yield return PostIngest(requestPayload, value => posted = value);

            if (posted)
            {
                _queue.RemoveSent(batch.ConvertAll(e => e.id));
                if (requestPayload.progressSnapshot != null)
                {
                    _snapshotDirty = false;
                    ClearPendingSnapshot();
                }
            }

            _flushRunning = false;
        }

        private IEnumerator EnsureSignedIn(Action<bool> done)
        {
            if (BackendSessionStore.TryGetValidAccessToken(out _))
            {
                done(true);
                yield break;
            }

            if (BackendSessionStore.TryGetRefreshToken(out string refreshToken))
            {
                bool refreshed = false;
                yield return RefreshAuth(refreshToken, value => refreshed = value);
                if (refreshed)
                {
                    done(true);
                    yield break;
                }
            }

            yield return SignInAnonymously(done);
        }

        private IEnumerator SignInAnonymously(Action<bool> done)
        {
            var body = new
            {
                data = new Dictionary<string, string>
                {
                    ["install_id"] = _installId,
                    ["game"] = "geo-model-geological-drilling-simulator"
                }
            };

            yield return SendAuthRequest(_settings.AuthSignupUrl, body, done);
        }

        private IEnumerator RefreshAuth(string refreshToken, Action<bool> done)
        {
            var body = new
            {
                refresh_token = refreshToken
            };

            yield return SendAuthRequest(_settings.AuthRefreshUrl, body, done);
        }

        private IEnumerator SendAuthRequest(string url, object body, Action<bool> done)
        {
            string json = JsonConvert.SerializeObject(body, BackendJson.Settings);
            using var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("apikey", _settings.PublishableKey);
            request.SetRequestHeader("Authorization", $"Bearer {_settings.PublishableKey}");

            yield return request.SendWebRequest();

            if (!IsSuccess(request))
            {
                LogWarning($"Auth request failed: HTTP {request.responseCode} {request.error} {TruncateResponse(request.downloadHandler?.text)}");
                done(false);
                yield break;
            }

            try
            {
                var response = JsonConvert.DeserializeObject<BackendAuthResponse>(request.downloadHandler.text, BackendJson.Settings);
                BackendSessionStore.SaveAuthSession(response);
                bool hasToken = !string.IsNullOrEmpty(response?.accessToken);
                if (hasToken)
                {
                    Debug.Log($"[TelemetryClient] Anonymous auth ok. user={ShortId(response.user?.id)}");
                }

                done(hasToken);
            }
            catch (Exception ex)
            {
                LogWarning($"Auth response parse failed: {ex.Message}");
                done(false);
            }
        }

        private IEnumerator PostIngest(IngestRequest payload, Action<bool> done)
        {
            if (!BackendSessionStore.TryGetValidAccessToken(out string accessToken))
            {
                done(false);
                yield break;
            }

            string json = JsonConvert.SerializeObject(payload, BackendJson.Settings);
            using var request = new UnityWebRequest(_settings.FunctionUrl, UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("apikey", _settings.PublishableKey);
            request.SetRequestHeader("Authorization", $"Bearer {accessToken}");

            yield return request.SendWebRequest();

            bool success = IsSuccess(request);
            if (!success)
            {
                LogWarning($"Ingest failed: HTTP {request.responseCode} {request.error} {TruncateResponse(request.downloadHandler?.text)}");
            }
            else
            {
                Debug.Log($"[TelemetryClient] Ingest ok: {payload.events?.Count ?? 0} events");
            }

            done(success);
        }

        private void SubscribeEvents()
        {
            GameEventBus.SceneLoaded += OnSceneLoaded;
            GameEventBus.ToolEquipped += OnToolEquipped;
            GameEventBus.ToolUsed += OnToolUsed;
            GameEventBus.QuestStarted += OnQuestStarted;
            GameEventBus.ObjectiveCompleted += OnObjectiveCompleted;
            GameEventBus.QuestCompleted += OnQuestCompleted;
            GameEventBus.ProgressDirty += OnProgressDirty;
        }

        private void UnsubscribeEvents()
        {
            GameEventBus.SceneLoaded -= OnSceneLoaded;
            GameEventBus.ToolEquipped -= OnToolEquipped;
            GameEventBus.ToolUsed -= OnToolUsed;
            GameEventBus.QuestStarted -= OnQuestStarted;
            GameEventBus.ObjectiveCompleted -= OnObjectiveCompleted;
            GameEventBus.QuestCompleted -= OnQuestCompleted;
            GameEventBus.ProgressDirty -= OnProgressDirty;
        }

        private void OnSceneLoaded(string sceneName)
        {
            Track("scene_loaded", new Dictionary<string, object> { ["sceneName"] = sceneName });
            MarkProgressDirty("scene_loaded");
        }

        private void OnToolEquipped(string toolId, string toolName)
        {
            Track("tool_equipped", new Dictionary<string, object>
            {
                ["toolId"] = toolId,
                ["toolName"] = toolName
            });
        }

        private void OnToolUsed(string toolId, string toolName, string targetName, string targetTag)
        {
            Track("tool_used", new Dictionary<string, object>
            {
                ["toolId"] = toolId,
                ["toolName"] = toolName,
                ["targetName"] = targetName,
                ["targetTag"] = targetTag
            });
        }

        private void OnQuestStarted(string questId)
        {
            Track("quest_started", new Dictionary<string, object> { ["questId"] = questId });
            MarkProgressDirty("quest_started");
        }

        private void OnObjectiveCompleted(string objectiveId)
        {
            Track("objective_completed", new Dictionary<string, object> { ["objectiveId"] = objectiveId });
            MarkProgressDirty("objective_completed");
        }

        private void OnQuestCompleted(string questId)
        {
            Track("quest_completed", new Dictionary<string, object> { ["questId"] = questId });
            MarkProgressDirty("quest_completed");
        }

        private void OnProgressDirty(string reason)
        {
            CaptureProgressSnapshot();
            Track("progress_dirty", new Dictionary<string, object>
            {
                ["reason"] = string.IsNullOrEmpty(reason) ? "unknown" : reason
            });
        }

        private void CaptureProgressSnapshot()
        {
            _snapshotDirty = true;
            _latestSnapshot = ProgressSnapshotBuilder.Build();
            string json = JsonConvert.SerializeObject(_latestSnapshot, BackendJson.Settings);
            PlayerPrefs.SetString(PendingSnapshotPrefsKey, json);
            PlayerPrefs.Save();
        }

        private static ProgressSnapshot LoadPendingSnapshot()
        {
            string json = PlayerPrefs.GetString(PendingSnapshotPrefsKey, "");
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            try
            {
                return JsonConvert.DeserializeObject<ProgressSnapshot>(json, BackendJson.Settings);
            }
            catch
            {
                PlayerPrefs.DeleteKey(PendingSnapshotPrefsKey);
                PlayerPrefs.Save();
                return null;
            }
        }

        private static void ClearPendingSnapshot()
        {
            PlayerPrefs.DeleteKey(PendingSnapshotPrefsKey);
            PlayerPrefs.Save();
        }

        private static bool IsSuccess(UnityWebRequest request)
        {
            return request.result == UnityWebRequest.Result.Success &&
                   request.responseCode >= 200 &&
                   request.responseCode < 300;
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[TelemetryClient] {message}");
        }

        private static string TruncateResponse(string responseBody)
        {
            if (string.IsNullOrEmpty(responseBody))
            {
                return "";
            }

            const int maxLength = 256;
            return responseBody.Length <= maxLength
                ? responseBody
                : responseBody.Substring(0, maxLength) + "...";
        }

        private static string ShortId(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "none";
            }

            return value.Length <= 8 ? value : value.Substring(0, 8);
        }

        private static string GetBuildTarget()
        {
#if UNITY_WEBGL
            return "WebGL";
#elif UNITY_STANDALONE_OSX
            return "StandaloneOSX";
#elif UNITY_STANDALONE_WIN
            return "StandaloneWindows";
#else
            return Application.platform.ToString();
#endif
        }
    }
}

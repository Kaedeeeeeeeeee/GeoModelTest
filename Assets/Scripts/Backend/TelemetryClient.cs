using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core;
using Newtonsoft.Json;
using StorySystem;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace Backend
{
    public sealed class TelemetryClient : MonoBehaviour
    {
        private const string PendingSnapshotPrefsKey = "Backend.PendingProgressSnapshot.v2";

        public static TelemetryClient Instance { get; private set; }

        private BackendSettings _settings;
        private TelemetryQueue _queue;
        private ResearchContext _researchContext;
        private string _installId;
        private string _sessionId;
        private bool _initialized;
        private bool _flushRunning;
        private bool _ending;
        private bool _snapshotDirty;
        private bool _researchAccessRevoked;
        private Action _endCompletionCallbacks;
        private ProgressSnapshot _latestSnapshot;

        public string InstallId => _installId;
        public string SessionId => _sessionId;
        public bool IsResearchActive => _initialized && _researchContext != null;
        public ResearchContext Context => _researchContext;

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

        /// <summary>
        /// 参加コードをサーバーで検証し、成功した場合だけ研究テレメトリを開始する。
        /// 通常プレイからは呼び出さない。
        /// </summary>
        public IEnumerator ActivateForResearch(
            BackendSettings settings,
            string participantCode,
            Action<bool, string> completed)
        {
            if (_initialized)
            {
                completed?.Invoke(true, string.Empty);
                yield break;
            }

            if (settings == null || !settings.EnableBackend || !settings.HasClientConfig)
            {
                completed?.Invoke(false, "研究用サーバーの設定がありません。");
                yield break;
            }

            if (!settings.CanShowResearchEntry)
            {
                completed?.Invoke(false, "研究参加の受付は現在停止しています。");
                yield break;
            }

            string normalizedCode = participantCode?.Trim() ?? string.Empty;
            if (normalizedCode.Length < 8 || normalizedCode.Length > 64)
            {
                completed?.Invoke(false, "参加コードを確認してください。");
                yield break;
            }

            _settings = settings;
            _installId = BackendSessionStore.GetOrCreateInstallId();

            bool signedIn = false;
            yield return EnsureSignedIn(value => signedIn = value);
            if (!signedIn)
            {
                completed?.Invoke(false, "サーバーに接続できませんでした。時間をおいて再試行してください。");
                yield break;
            }

            ResearchParticipationResponse participation = null;
            string participationError = string.Empty;
            yield return ValidateParticipationCode(
                normalizedCode,
                value => participation = value,
                value => participationError = value);

            if (participation == null || !participation.ok ||
                !Guid.TryParse(participation.participantId, out _) ||
                !Guid.TryParse(participation.studyId, out _))
            {
                completed?.Invoke(false, string.IsNullOrWhiteSpace(participationError)
                    ? "参加コードを確認できませんでした。"
                    : participationError);
                yield break;
            }

            var context = new ResearchContext
            {
                participantId = participation.participantId,
                studyId = participation.studyId,
                condition = participation.condition,
                protocolVersion = participation.protocolVersion
            };

            ClearStaleParticipantDataIfNeeded(context.participantId);
            BackendSessionStore.SaveResearchContext(context);
            InitializeAuthorized(settings, context);
            completed?.Invoke(true, string.Empty);
        }

        public void RecordQuizAttempt(QuizAttempt attempt)
        {
            if (!_initialized || attempt == null)
            {
                return;
            }

            var upload = new QuizAttemptUpload
            {
                eventId = attempt.eventId,
                participantId = _researchContext.participantId,
                studyId = _researchContext.studyId,
                condition = _researchContext.condition,
                sessionId = _sessionId,
                runId = attempt.runId,
                questionId = attempt.questionId,
                questionVersion = attempt.questionVersion,
                choiceId = attempt.choiceId,
                attemptIndex = attempt.attemptIndex,
                isCorrect = attempt.isCorrect,
                usedHint = attempt.usedHint,
                responseTimeMs = attempt.responseTimeMs,
                occurredAt = attempt.occurredAt,
                gameVersion = Application.version,
                contentVersion = ResearchContentVersion.ContentVersion,
                storyRoute = ResearchContentVersion.StoryRoute
            };

            if (_queue.EnqueueQuizAttempt(upload))
            {
                Track("quiz_answered", new Dictionary<string, object>
                {
                    ["questionId"] = attempt.questionId,
                    ["questionVersion"] = attempt.questionVersion,
                    ["choiceId"] = attempt.choiceId,
                    ["attemptIndex"] = attempt.attemptIndex,
                    ["isCorrect"] = attempt.isCorrect,
                    ["usedHint"] = attempt.usedHint,
                    ["responseTimeMs"] = attempt.responseTimeMs
                });
            }

            if (_queue.Count >= _settings.MaxBatchSize)
            {
                StartCoroutine(FlushAsync(false));
            }
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
            if (!_initialized || _settings == null || !_settings.EnableBackend || _researchContext == null)
            {
                return;
            }

            props ??= new Dictionary<string, object>();
            props["contentVersion"] = ResearchContentVersion.ContentVersion;
            props["storyRoute"] = ResearchContentVersion.StoryRoute;
            props["copyChecklistVersion"] = ResearchContentVersion.CopyChecklistVersion;

            var evt = TelemetryQueue.Create(eventName, SceneManager.GetActiveScene().name, props);
            evt.participantId = _researchContext.participantId;
            evt.studyId = _researchContext.studyId;
            evt.condition = _researchContext.condition;
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

        public void EndResearchSession(string reason, Action completed = null)
        {
            if (!_initialized)
            {
                ClearResearchIdentity();
                completed?.Invoke();
                return;
            }

            if (_ending)
            {
                _endCompletionCallbacks += completed;
                return;
            }

            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "explicit_exit" : reason.Trim();
            _ending = true;
            _endCompletionCallbacks = completed;
            Track("session_ended", new Dictionary<string, object> { ["reason"] = normalizedReason });
            StartCoroutine(FinishResearchSession(normalizedReason));
        }

        private IEnumerator FinishResearchSession(string reason)
        {
            while (_flushRunning)
            {
                yield return null;
            }

            // Send all full batches first so the final request can reliably close the session.
            while (_initialized && _queue != null && _queue.Count > 0)
            {
                bool posted = false;
                yield return FlushAsync(false, done: value => posted = value);
                if (!posted)
                {
                    break;
                }
            }

            if (_initialized)
            {
                yield return FlushAsync(true, reason);
            }

            _initialized = false;
            UnsubscribeEvents();
            _queue?.Clear();
            ClearPendingSnapshot();
            ClearResearchIdentity();
            Action callbacks = _endCompletionCallbacks;
            _endCompletionCallbacks = null;
            callbacks?.Invoke();
            Destroy(gameObject);
        }

        private void InitializeAuthorized(BackendSettings settings, ResearchContext context)
        {
            _settings = settings;
            _researchContext = context;
            _installId = BackendSessionStore.GetOrCreateInstallId();
            _sessionId = BackendSessionStore.CreateSessionId();
            _queue = new TelemetryQueue(_settings.MaxPersistedEvents);
            _latestSnapshot = LoadPendingSnapshotForCurrentContext();
            _snapshotDirty = _latestSnapshot != null;
            _initialized = true;

            SubscribeEvents();
            MarkProgressDirty("startup");
            Track("session_started", new Dictionary<string, object>
            {
                ["gameVersion"] = Application.version,
                ["platform"] = Application.platform.ToString(),
                ["language"] = Application.systemLanguage.ToString(),
                ["protocolVersion"] = _researchContext.protocolVersion
            });
            Track("research_mode_started");

            Debug.Log($"[TelemetryClient] Research telemetry started. participant={ShortId(context.participantId)} session={ShortId(_sessionId)}");
            StartCoroutine(FlushLoop());
            StartCoroutine(HeartbeatLoop());
            StartCoroutine(FlushAsync(false));
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
            if (pauseStatus && _initialized)
            {
                TrackHeartbeat("application_pause");
                StartCoroutine(FlushAsync(false));
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && _initialized)
            {
                TrackHeartbeat("focus_lost");
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

        private IEnumerator FlushLoop()
        {
            while (_initialized && !_ending)
            {
                yield return new WaitForSecondsRealtime(_settings.FlushIntervalSeconds);
                if (!_initialized || _ending)
                {
                    yield break;
                }
                yield return FlushAsync(false);
            }
        }

        private IEnumerator HeartbeatLoop()
        {
            while (_initialized && !_ending)
            {
                yield return new WaitForSecondsRealtime(_settings.HeartbeatIntervalSeconds);
                if (!_initialized || _ending)
                {
                    yield break;
                }
                TrackHeartbeat("interval");
                yield return FlushAsync(false);
            }
        }

        private void TrackHeartbeat(string reason)
        {
            Track("session_heartbeat", new Dictionary<string, object>
            {
                ["reason"] = reason,
                ["heartbeatAt"] = DateTimeOffset.UtcNow.ToString("o")
            });
        }

        private IEnumerator FlushAsync(bool sessionEnd, string endReason = null, Action<bool> done = null)
        {
            if (_flushRunning || !_initialized || !_settings.EnableBackend || !_settings.HasClientConfig)
            {
                done?.Invoke(false);
                yield break;
            }

            TelemetryBatch batch = _queue.PeekBatch(_settings.MaxBatchSize);
            if (batch.Count == 0 && !_snapshotDirty && !sessionEnd)
            {
                done?.Invoke(true);
                yield break;
            }

            _flushRunning = true;

            bool signedIn = false;
            yield return EnsureSignedIn(value => signedIn = value);
            if (!signedIn)
            {
                _flushRunning = false;
                done?.Invoke(false);
                yield break;
            }

            string requestSessionId = !string.IsNullOrEmpty(batch.sessionId) ? batch.sessionId : _sessionId;
            string requestParticipantId = !string.IsNullOrEmpty(batch.participantId)
                ? batch.participantId
                : _researchContext.participantId;
            string requestStudyId = !string.IsNullOrEmpty(batch.studyId)
                ? batch.studyId
                : _researchContext.studyId;
            string requestCondition = !string.IsNullOrEmpty(batch.condition)
                ? batch.condition
                : _researchContext.condition;
            bool attachCurrentSnapshot = _snapshotDirty &&
                                         requestSessionId == _sessionId &&
                                         requestParticipantId == _researchContext.participantId;

            var requestPayload = new IngestRequest
            {
                installId = _installId,
                participantId = requestParticipantId,
                studyId = requestStudyId,
                condition = requestCondition,
                protocolVersion = _researchContext.protocolVersion,
                sessionId = requestSessionId,
                gameVersion = Application.version,
                platform = Application.platform.ToString(),
                buildTarget = GetBuildTarget(),
                language = Application.systemLanguage.ToString(),
                currentScene = SceneManager.GetActiveScene().name,
                contentVersion = ResearchContentVersion.ContentVersion,
                storyRoute = ResearchContentVersion.StoryRoute,
                events = batch.events,
                quizAttempts = batch.quizAttempts,
                progressSnapshot = attachCurrentSnapshot ? (_latestSnapshot ?? BuildBoundProgressSnapshot()) : null,
                sessionEnd = sessionEnd && requestSessionId == _sessionId
                    ? new SessionEndPayload
                    {
                        endedAt = DateTimeOffset.UtcNow.ToString("o"),
                        reason = string.IsNullOrWhiteSpace(endReason) ? "explicit_exit" : endReason
                    }
                    : null
            };

            bool posted = false;
            yield return PostIngest(requestPayload, value => posted = value);

            if (_researchAccessRevoked)
            {
                _queue.Clear();
                ClearPendingSnapshot();
                _snapshotDirty = false;
                _initialized = false;
                UnsubscribeEvents();
                ClearResearchIdentity();
                _flushRunning = false;
                LogWarning("Research access was rejected; local research queue and credentials were cleared.");
                done?.Invoke(false);
                Destroy(gameObject);
                yield break;
            }

            if (posted)
            {
                _queue.RemoveSent(
                    batch.events.Select(value => value.id),
                    batch.quizAttempts.Select(value => value.eventId));
                if (requestPayload.progressSnapshot != null)
                {
                    _snapshotDirty = false;
                    ClearPendingSnapshot();
                }
            }

            _flushRunning = false;
            done?.Invoke(posted);
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
            yield return SendAuthRequest(_settings.AuthRefreshUrl, new { refresh_token = refreshToken }, done);
        }

        private IEnumerator SendAuthRequest(string url, object body, Action<bool> done)
        {
            string json = JsonConvert.SerializeObject(body, BackendJson.Settings);
            using var request = CreateJsonPost(url, json);
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
                done(!string.IsNullOrEmpty(response?.accessToken));
            }
            catch (Exception ex)
            {
                LogWarning($"Auth response parse failed: {ex.Message}");
                done(false);
            }
        }

        private IEnumerator ValidateParticipationCode(
            string participantCode,
            Action<ResearchParticipationResponse> success,
            Action<string> failure)
        {
            if (!BackendSessionStore.TryGetValidAccessToken(out string accessToken))
            {
                failure("認証情報を確認できませんでした。");
                yield break;
            }

            string json = JsonConvert.SerializeObject(new
            {
                participantCode,
                gameVersion = Application.version,
                contentVersion = ResearchContentVersion.ContentVersion,
                storyRoute = ResearchContentVersion.StoryRoute
            }, BackendJson.Settings);

            using var request = CreateJsonPost(_settings.ParticipationFunctionUrl, json);
            request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
            yield return request.SendWebRequest();

            if (!IsSuccess(request))
            {
                ResearchParticipationResponse errorResponse = null;
                try
                {
                    errorResponse = JsonConvert.DeserializeObject<ResearchParticipationResponse>(request.downloadHandler?.text, BackendJson.Settings);
                }
                catch
                {
                    // Use the generic message below.
                }

                failure(errorResponse?.error ?? "参加コードを確認できませんでした。");
                yield break;
            }

            try
            {
                success(JsonConvert.DeserializeObject<ResearchParticipationResponse>(request.downloadHandler.text, BackendJson.Settings));
            }
            catch (Exception ex)
            {
                LogWarning($"Participation response parse failed: {ex.Message}");
                failure("サーバーの応答を読み取れませんでした。");
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
            using var request = CreateJsonPost(_settings.FunctionUrl, json);
            request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
            yield return request.SendWebRequest();

            bool success = IsSuccess(request);
            if (!success)
            {
                if (request.responseCode == 401 || request.responseCode == 403)
                {
                    _researchAccessRevoked = true;
                }
                LogWarning($"Ingest failed: HTTP {request.responseCode} {request.error} {TruncateResponse(request.downloadHandler?.text)}");
            }
            else if (_settings.VerboseLogging)
            {
                Debug.Log($"[TelemetryClient] Ingest ok: {payload.events?.Count ?? 0} events, {payload.quizAttempts?.Count ?? 0} quiz attempts");
            }

            done(success);
        }

        private UnityWebRequest CreateJsonPost(string url, string json)
        {
            var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = 15;
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("apikey", _settings.PublishableKey);
            return request;
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
            Track("tool_equipped", new Dictionary<string, object> { ["toolId"] = toolId, ["toolName"] = toolName });
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

        private ProgressSnapshot BuildBoundProgressSnapshot()
        {
            ProgressSnapshot snapshot = ProgressSnapshotBuilder.Build();
            snapshot.participantId = _researchContext.participantId;
            snapshot.studyId = _researchContext.studyId;
            snapshot.condition = _researchContext.condition;
            snapshot.sessionId = _sessionId;
            return snapshot;
        }

        private void CaptureProgressSnapshot()
        {
            _snapshotDirty = true;
            _latestSnapshot = BuildBoundProgressSnapshot();
            string json = JsonConvert.SerializeObject(_latestSnapshot, BackendJson.Settings);
            PlayerPrefs.SetString(PendingSnapshotPrefsKey, json);
            PlayerPrefs.Save();
        }

        private ProgressSnapshot LoadPendingSnapshotForCurrentContext()
        {
            string json = PlayerPrefs.GetString(PendingSnapshotPrefsKey, "");
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            try
            {
                ProgressSnapshot snapshot = JsonConvert.DeserializeObject<ProgressSnapshot>(json, BackendJson.Settings);
                if (snapshot != null &&
                    snapshot.participantId == _researchContext.participantId &&
                    snapshot.studyId == _researchContext.studyId)
                {
                    return snapshot;
                }
            }
            catch
            {
                // Clear below.
            }

            ClearPendingSnapshot();
            return null;
        }

        private static void ClearPendingSnapshot()
        {
            PlayerPrefs.DeleteKey(PendingSnapshotPrefsKey);
            PlayerPrefs.Save();
        }

        private static void ClearStaleParticipantDataIfNeeded(string participantId)
        {
            string previousParticipantId = PlayerPrefs.GetString(BackendSessionStore.ResearchParticipantIdKey, string.Empty);
            if (string.IsNullOrEmpty(previousParticipantId) || previousParticipantId == participantId)
            {
                return;
            }

            PlayerPrefs.DeleteKey(TelemetryQueue.PendingEventsPrefsKey);
            ClearPendingSnapshot();
            BackendSessionStore.ClearResearchContext();
        }

        private static void ClearResearchIdentity()
        {
            BackendSessionStore.ClearResearchContext();
            BackendSessionStore.ClearAuthSession();
        }

        private static bool IsSuccess(UnityWebRequest request)
        {
            return request.result == UnityWebRequest.Result.Success &&
                   request.responseCode >= 200 &&
                   request.responseCode < 300;
        }

        private static string TruncateResponse(string responseBody)
        {
            if (string.IsNullOrEmpty(responseBody)) return string.Empty;
            const int maxLength = 256;
            return responseBody.Length <= maxLength ? responseBody : responseBody.Substring(0, maxLength) + "...";
        }

        private static string ShortId(string value)
        {
            if (string.IsNullOrEmpty(value)) return "none";
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

        private static void LogWarning(string message)
        {
            Debug.LogWarning($"[TelemetryClient] {message}");
        }
    }
}

using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using StorySystem;
using UISystem;

namespace Backend
{
    public sealed class SurveyGateway : MonoBehaviour
    {
        private static SurveyGateway _instance;
        public bool IsBusy { get; private set; }
        private const string TicketPrefsKey = "Backend.SurveyTicket.v1";
        [Serializable] private sealed class Ticket { public bool ok; public string ticket, expiresAt, participantId, runId, server; }
#if UNITY_EDITOR
        public static Action<string> ReviewOpenUrl;
#endif
        public static SurveyGateway Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GameObject("SurveyGateway").AddComponent<SurveyGateway>();
                    DontDestroyOnLoad(_instance.gameObject);
                }
                return _instance;
            }
        }

        public static bool IsEligible
        {
            get
            {
                if (!InvestigationProgress.IsComplete) return false;
                var settings = BackendSettingsProvider.Load();
                return settings != null && settings.HasClientConfig &&
                    (TelemetryClient.Instance?.IsResearchActive == true || SurveyCompletionStore.Current(settings.SupabaseUrl, QuizScoreManager.Instance.RunId) != null);
            }
        }

        public void Open(Action<string> status)
        {
            if (!IsBusy) StartCoroutine(OpenRoutine(status));
        }

        private IEnumerator OpenRoutine(Action<string> status)
        {
            if (!IsEligible) { status?.Invoke(GameUI.L("survey.research_only")); yield break; }
            IsBusy = true;
            try
            {
                status?.Invoke(GameUI.L("survey.preparing"));
                var settings = BackendSettingsProvider.Load();
                SceneSystem.GameSession.SaveCheckpoint();
                var client = TelemetryClient.Instance;
                if (client?.IsResearchActive == true)
                {
                    bool posted = false;
                    yield return client.FlushForSurvey(value => posted = value);
                    if (!posted) { status?.Invoke(GameUI.L("survey.retry")); yield break; }
                }
                var completion = SurveyCompletionStore.Current(settings.SupabaseUrl, QuizScoreManager.Instance.RunId);
                if (completion == null) { status?.Invoke(GameUI.L("survey.retry")); yield break; }
                Ticket cached = null;
                try { cached = JsonUtility.FromJson<Ticket>(PlayerPrefs.GetString(TicketPrefsKey, "")); } catch { }
                if (cached == null || cached.runId != completion.runId || cached.participantId != completion.participantId ||
                    cached.server != completion.server || !IsValidTicket(cached.ticket) ||
                    !DateTimeOffset.TryParse(cached.expiresAt, out var expiry) || expiry <= DateTimeOffset.UtcNow.AddMinutes(1))
                {
                    if (!BackendSessionStore.TryGetValidAccessToken(out _))
                    {
                        if (!BackendSessionStore.TryGetRefreshToken(out var refreshToken)) { status?.Invoke(GameUI.L("survey.reenter")); yield break; }
                        using var refresh = Post(settings.AuthRefreshUrl, JsonConvert.SerializeObject(new { refresh_token = refreshToken }), settings);
                        yield return refresh.SendWebRequest();
                        if (refresh.result != UnityWebRequest.Result.Success) { status?.Invoke(GameUI.L("survey.reenter")); yield break; }
                        BackendAuthResponse auth = null;
                        try { auth = JsonConvert.DeserializeObject<BackendAuthResponse>(refresh.downloadHandler.text, BackendJson.Settings); } catch { }
                        if (auth?.user?.id != PlayerPrefs.GetString(BackendSessionStore.UserIdKey, "")) { status?.Invoke(GameUI.L("survey.reenter")); yield break; }
                        BackendSessionStore.SaveAuthSession(auth);
                    }
                    BackendSessionStore.TryGetValidAccessToken(out var accessToken);
                    using var request = Post(settings.SupabaseUrl + "/functions/v1/game-survey", JsonConvert.SerializeObject(new { action = "issue", sessionId = completion.sessionId, runId = completion.runId }), settings);
                    request.SetRequestHeader("Authorization", "Bearer " + accessToken);
                    yield return request.SendWebRequest();
                    if (request.result != UnityWebRequest.Result.Success) { status?.Invoke(GameUI.L("survey.retry")); yield break; }
                    try { cached = JsonUtility.FromJson<Ticket>(request.downloadHandler.text); } catch { cached = null; }
                    if (cached == null || !cached.ok || !IsValidTicket(cached.ticket)) { status?.Invoke(GameUI.L("survey.retry")); yield break; }
                    cached.runId = completion.runId; cached.participantId = completion.participantId; cached.server = completion.server;
                    PlayerPrefs.SetString(TicketPrefsKey, JsonUtility.ToJson(cached));
                    PlayerPrefs.Save();
                }
                bool ended = false;
                ResearchParticipationCoordinator.Instance.EndSession("post_game_survey", () => ended = true);
                while (!ended) yield return null;
                bool saved = false;
                yield return WebGLFileSync.FlushAndWait(value => saved = value);
                if (!saved) { status?.Invoke(GameUI.L("survey.retry")); yield break; }
                var config = Resources.Load<SurveySettings>("SurveySettings");
                if (config == null) { status?.Invoke(GameUI.L("survey.retry")); yield break; }
                string url = config.PageUrl.Split('#')[0] + "#ticket=" + cached.ticket;
                status?.Invoke(GameUI.L("survey.opened"));
#if UNITY_WEBGL && !UNITY_EDITOR
                GeoModelTest_OpenSurvey(url, GameUI.L("survey.answer"));
#elif UNITY_EDITOR
                if (ReviewOpenUrl != null) ReviewOpenUrl(url); else Application.OpenURL(url);
#else
                Application.OpenURL(url);
#endif
            }
            finally { IsBusy = false; }
        }

        public static bool IsValidTicket(string ticket) => ticket != null && Regex.IsMatch(ticket, "\\A[0-9a-f]{64}\\z");
        private static UnityWebRequest Post(string url, string body, BackendSettings settings)
        {
            var request = new UnityWebRequest(url, "POST") { uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body)), downloadHandler = new DownloadHandlerBuffer(), timeout = 20 };
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("apikey", settings.PublishableKey);
            return request;
        }
#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void GeoModelTest_OpenSurvey(string url, string label);
#endif
    }
}

using System;
using System.Collections;
using UnityEngine;

namespace Backend
{
    /// <summary>
    /// 明示的な参加コード入力から研究モードを開始・終了する調整役。
    /// Instance の生成だけでは認証も通信も開始しない。
    /// </summary>
    public sealed class ResearchParticipationCoordinator : MonoBehaviour
    {
        private static ResearchParticipationCoordinator _instance;

        public static ResearchParticipationCoordinator Instance
        {
            get
            {
                if (_instance == null)
                {
                    var host = new GameObject("ResearchParticipationCoordinator");
                    _instance = host.AddComponent<ResearchParticipationCoordinator>();
                    DontDestroyOnLoad(host);
                }

                return _instance;
            }
        }

        public bool IsActivating { get; private set; }
        public bool IsResearchActive => TelemetryClient.Instance != null && TelemetryClient.Instance.IsResearchActive;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Activate(string participantCode, Action<bool, string> completed)
        {
            if (IsActivating)
            {
                completed?.Invoke(false, UISystem.GameUI.L("backend.checking"));
                return;
            }

            StartCoroutine(ActivateRoutine(participantCode, completed));
        }

        public void EndSession(string reason, Action completed = null)
        {
            TelemetryClient client = TelemetryClient.Instance;
            if (client == null || !client.IsResearchActive)
            {
                // Ending an opt-in session must not discard the anonymous identity needed for re-entry.
                completed?.Invoke();
                return;
            }

            client.EndResearchSession(reason, completed);
        }

        private IEnumerator ActivateRoutine(string participantCode, Action<bool, string> completed)
        {
            IsActivating = true;
            BackendSettings settings = BackendSettingsProvider.Load();
            if (settings == null || !settings.CanShowResearchEntry)
            {
                IsActivating = false;
                completed?.Invoke(false, UISystem.GameUI.L("backend.closed"));
                yield break;
            }

            TelemetryClient client = BackendBootstrap.CreateResearchClient();
            if (client.IsResearchActive && !BackendAuthProfiles.IsCurrentCode(settings.SupabaseUrl, participantCode ?? ""))
            {
                bool ended = false;
                client.EndResearchSession("participant_switch", () => ended = true);
                while (!ended) yield return null;
                yield return null; // Let the previous client be destroyed before creating its replacement.
                client = BackendBootstrap.CreateResearchClient();
            }
            bool success = false;
            string message = string.Empty;
            yield return client.ActivateForResearch(settings, participantCode, (value, error) =>
            {
                success = value;
                message = error;
            });

            IsActivating = false;
            if (!success && client != null && !client.IsResearchActive)
            {
                Destroy(client.gameObject);
            }

            completed?.Invoke(success, message);
        }
    }
}

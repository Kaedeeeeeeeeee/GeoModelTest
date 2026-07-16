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
                completed?.Invoke(false, "参加コードを確認しています。少し待ってください。");
                return;
            }

            StartCoroutine(ActivateRoutine(participantCode, completed));
        }

        public void EndSession(string reason, Action completed = null)
        {
            TelemetryClient client = TelemetryClient.Instance;
            if (client == null || !client.IsResearchActive)
            {
                BackendSessionStore.ClearResearchContext();
                BackendSessionStore.ClearAuthSession();
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
                completed?.Invoke(false, "研究参加の受付は現在停止しています。");
                yield break;
            }

            TelemetryClient client = BackendBootstrap.CreateResearchClient();
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

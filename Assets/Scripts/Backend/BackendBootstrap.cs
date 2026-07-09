using UnityEngine;

namespace Backend
{
    public static class BackendBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (TelemetryClient.Instance != null)
            {
                return;
            }

            BackendSettings settings = BackendSettingsProvider.Load();
            if (settings == null || !settings.EnableBackend || !settings.HasClientConfig)
            {
                return;
            }

            var gameObject = new GameObject("BackendBootstrap");
            var client = gameObject.AddComponent<TelemetryClient>();
            client.Initialize(settings);
        }
    }
}

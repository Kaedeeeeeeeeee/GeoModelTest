using UnityEngine;

namespace Backend
{
    [CreateAssetMenu(fileName = "BackendSettings", menuName = "GeoModel/Backend Settings")]
    public class BackendSettings : ScriptableObject
    {
        [Header("Backend")]
        [SerializeField] private bool enableBackend = false;
        [SerializeField] private string supabaseUrl = "";
        [SerializeField] private string publishableKey = "";
        [SerializeField] private string ingestFunctionName = "game-ingest";

        [Header("Queue")]
        [SerializeField] private float flushIntervalSeconds = 30f;
        [SerializeField] private int maxBatchSize = 25;
        [SerializeField] private int maxPersistedEvents = 100;

        [Header("Diagnostics")]
        [SerializeField] private bool verboseLogging = false;

        public bool EnableBackend => enableBackend;
        public string SupabaseUrl => string.IsNullOrWhiteSpace(supabaseUrl) ? "" : supabaseUrl.TrimEnd('/');
        public string PublishableKey => publishableKey ?? "";
        public string IngestFunctionName => string.IsNullOrWhiteSpace(ingestFunctionName) ? "game-ingest" : ingestFunctionName.Trim();
        public float FlushIntervalSeconds => Mathf.Max(5f, flushIntervalSeconds);
        public int MaxBatchSize => Mathf.Clamp(maxBatchSize, 1, 100);
        public int MaxPersistedEvents => Mathf.Clamp(maxPersistedEvents, 10, 500);
        public bool VerboseLogging => verboseLogging;

        public bool HasClientConfig =>
            !string.IsNullOrWhiteSpace(SupabaseUrl) &&
            !string.IsNullOrWhiteSpace(PublishableKey);

        public string FunctionUrl => $"{SupabaseUrl}/functions/v1/{IngestFunctionName}";
        public string AuthSignupUrl => $"{SupabaseUrl}/auth/v1/signup";
        public string AuthRefreshUrl => $"{SupabaseUrl}/auth/v1/token?grant_type=refresh_token";
    }
}

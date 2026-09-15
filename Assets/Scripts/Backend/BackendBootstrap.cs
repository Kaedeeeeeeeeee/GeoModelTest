using UnityEngine;

namespace Backend
{
    /// <summary>
    /// New Game またはアンケート再試行から匿名プレイクライアントを生成する。
    /// サーバーの登録が成功するまで TelemetryClient は初期化されない。
    /// </summary>
    public static class BackendBootstrap
    {
        public static TelemetryClient CreateResearchClient()
        {
            if (TelemetryClient.Instance != null)
            {
                return TelemetryClient.Instance;
            }

            var gameObject = new GameObject("ResearchTelemetryClient");
            return gameObject.AddComponent<TelemetryClient>();
        }
    }
}

using UnityEngine;

namespace Backend
{
    /// <summary>
    /// 通常プレイでは呼び出されない明示的な研究クライアント生成口。
    /// 参加コードの検証が成功するまで TelemetryClient は初期化されない。
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

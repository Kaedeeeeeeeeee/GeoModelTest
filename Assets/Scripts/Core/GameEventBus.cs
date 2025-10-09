using System;

namespace Core
{
    /// <summary>
    /// 全局事件总线：用于系统间解耦。仅包含最小 MVP 事件。
    /// </summary>
    public static class GameEventBus
    {
        public static event Action<string> SceneLoaded; // 参数：场景名

        public static void RaiseSceneLoaded(string sceneName)
        {
            try
            {
                UnityEngine.Debug.Log($"[GameEventBus] SceneLoaded => {sceneName}");
                SceneLoaded?.Invoke(sceneName);
            }
            catch (Exception ex) { UnityEngine.Debug.LogError($"[GameEventBus] SceneLoaded 触发异常: {ex.Message}"); }
        }
    }
}

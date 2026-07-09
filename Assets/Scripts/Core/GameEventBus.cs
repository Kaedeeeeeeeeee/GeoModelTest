using System;

namespace Core
{
    /// <summary>
    /// 全局事件总线：用于系统间解耦。仅包含最小 MVP 事件。
    /// </summary>
    public static class GameEventBus
    {
        public static event Action<string> SceneLoaded; // 参数：场景名
        public static event Action<string, string> ToolEquipped; // toolId, toolName
        public static event Action<string, string, string, string> ToolUsed; // toolId, toolName, targetName, targetTag
        public static event Action<string> QuestStarted; // questId
        public static event Action<string> ObjectiveCompleted; // objectiveId
        public static event Action<string> QuestCompleted; // questId
        public static event Action<string> ProgressDirty; // reason

        public static void RaiseSceneLoaded(string sceneName)
        {
            try
            {
                UnityEngine.Debug.Log($"[GameEventBus] SceneLoaded => {sceneName}");
                SceneLoaded?.Invoke(sceneName);
            }
            catch (Exception ex) { UnityEngine.Debug.LogError($"[GameEventBus] SceneLoaded 触发异常: {ex.Message}"); }
        }

        public static void RaiseToolEquipped(string toolId, string toolName)
        {
            try
            {
                ToolEquipped?.Invoke(toolId, toolName);
            }
            catch (Exception ex) { UnityEngine.Debug.LogError($"[GameEventBus] ToolEquipped 触发异常: {ex.Message}"); }
        }

        public static void RaiseToolUsed(string toolId, string toolName, string targetName, string targetTag)
        {
            try
            {
                ToolUsed?.Invoke(toolId, toolName, targetName, targetTag);
            }
            catch (Exception ex) { UnityEngine.Debug.LogError($"[GameEventBus] ToolUsed 触发异常: {ex.Message}"); }
        }

        public static void RaiseQuestStarted(string questId)
        {
            try
            {
                QuestStarted?.Invoke(questId);
            }
            catch (Exception ex) { UnityEngine.Debug.LogError($"[GameEventBus] QuestStarted 触发异常: {ex.Message}"); }
        }

        public static void RaiseObjectiveCompleted(string objectiveId)
        {
            try
            {
                ObjectiveCompleted?.Invoke(objectiveId);
            }
            catch (Exception ex) { UnityEngine.Debug.LogError($"[GameEventBus] ObjectiveCompleted 触发异常: {ex.Message}"); }
        }

        public static void RaiseQuestCompleted(string questId)
        {
            try
            {
                QuestCompleted?.Invoke(questId);
            }
            catch (Exception ex) { UnityEngine.Debug.LogError($"[GameEventBus] QuestCompleted 触发异常: {ex.Message}"); }
        }

        public static void RaiseProgressDirty(string reason)
        {
            try
            {
                ProgressDirty?.Invoke(reason);
            }
            catch (Exception ex) { UnityEngine.Debug.LogError($"[GameEventBus] ProgressDirty 触发异常: {ex.Message}"); }
        }
    }
}

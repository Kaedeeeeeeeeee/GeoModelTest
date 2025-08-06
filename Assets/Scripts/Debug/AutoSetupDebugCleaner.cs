using UnityEngine;

/// <summary>
/// 自动设置调试清理器 - 自动创建StartupDebugCleaner组件
/// </summary>
[System.Serializable]
public class AutoSetupDebugCleaner
{
    /// <summary>
    /// 在游戏启动时自动创建调试清理器
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void SetupDebugCleaner()
    {
        // 创建一个专门的GameObject用于调试清理
        GameObject debugCleanerObj = new GameObject("AutoDebugCleaner");
        debugCleanerObj.AddComponent<SimpleDebugCleaner>();
        
        // 设置为DontDestroyOnLoad以确保在场景切换时不被销毁
        Object.DontDestroyOnLoad(debugCleanerObj);
        
        Debug.Log("🧹 AutoSetupDebugCleaner: 简单调试清理器已自动创建！");
    }
}
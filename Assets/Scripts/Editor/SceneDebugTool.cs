using UnityEngine;
using UnityEditor;

/// <summary>
/// 场景调试工具 - 帮助诊断场景切换和初始化问题
/// </summary>
public class SceneDebugTool
{
#if UNITY_EDITOR
    [MenuItem("Tools/场景切换器设置/检查当前场景状态")]
    public static void CheckCurrentSceneStatus()
    {
        Debug.Log("=== 当前场景状态检查 ===");

        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        Debug.Log($"当前场景: {currentScene}");

        // 检查SceneInitializer
        SceneInitializer sceneInitializer = Object.FindFirstObjectByType<SceneInitializer>();
        Debug.Log($"SceneInitializer存在: {sceneInitializer != null}");

        // 检查GameSceneManager
        GameSceneManager sceneManager = GameSceneManager.Instance;
        Debug.Log($"GameSceneManager存在: {sceneManager != null}");

        // 检查移动端组件
        MobileInputManager inputManager = MobileInputManager.Instance;
        Debug.Log($"MobileInputManager存在: {inputManager != null}");

        MobileControlsUI controlsUI = Object.FindFirstObjectByType<MobileControlsUI>();
        Debug.Log($"MobileControlsUI存在: {controlsUI != null}");

        Debug.Log("=== 检查完成 ===");
    }

    [MenuItem("Tools/场景切换器设置/手动触发场景初始化")]
    public static void ManualTriggerSceneSetup()
    {
        Debug.Log("=== 手动触发场景初始化 ===");

        SceneInitializer.SetupCurrentScene();

        Debug.Log("✅ 场景初始化已触发");
    }

    [MenuItem("Tools/场景切换器设置/创建SceneInitializer")]
    public static void CreateSceneInitializer()
    {
        // 检查是否已存在
        SceneInitializer existing = Object.FindFirstObjectByType<SceneInitializer>();
        if (existing != null)
        {
            Debug.Log("SceneInitializer已存在，跳过创建");
            return;
        }

        // 创建新的SceneInitializer
        GameObject initializerObj = new GameObject("SceneInitializer");
        SceneInitializer initializer = initializerObj.AddComponent<SceneInitializer>();

        Debug.Log("✅ SceneInitializer创建完成");
    }
#endif
}
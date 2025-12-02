using UnityEngine;
using System.Collections;

/// <summary>
/// 场景清理器 - 清理重复或不完整的自动创建对象
/// </summary>
public class SceneCleanup : MonoBehaviour
{
    [Header("清理设置")]
    public bool autoCleanOnStart = true;
    public float cleanupDelay = 0.1f;
    
    void Start()
    {
        if (ShouldSkipRuntimeCleanup())
        {
            Debug.Log($"{GetTimestamp()} [SceneCleanup] 检测到运行时场景管理器，跳过自动清理");
            return;
        }

        if (autoCleanOnStart)
        {
            StartCoroutine(CleanupScene());
        }
    }
    
    IEnumerator CleanupScene()
    {
        yield return new WaitForSeconds(cleanupDelay);
        
        Debug.Log("=== 开始场景清理 ===");
        
        // 清理重复的Player对象
        CleanupDuplicatePlayers();
        
        // 清理重复的摄像机
        CleanupDuplicateCameras();
        
        // 清理重复的UI系统
        CleanupDuplicateUISystems();
        
        // 清理重复的工具管理器
        CleanupDuplicateToolManagers();
        
        // 清理可能冲突的Canvas
        CleanupConflictingCanvases();
        
        Debug.Log("=== 场景清理完成 ===");
        
        // 自毁
        Destroy(this);
    }
    
    /// <summary>
    /// 清理重复的Player对象
    /// </summary>
    void CleanupDuplicatePlayers()
    {
        FirstPersonController[] players = FindObjectsByType<FirstPersonController>(FindObjectsSortMode.None);
        
        if (players.Length > 1)
        {
            Debug.Log($"{GetTimestamp()} [SceneCleanup] 发现 {players.Length} 个Player对象，开始清理");
            
            // 保留第一个完整的Player，删除其他的
            FirstPersonController keepPlayer = null;
            
            foreach (var player in players)
            {
                if (player != null)
                {
                    // 检查Player是否完整（有摄像机子对象）
                    Camera playerCamera = player.GetComponentInChildren<Camera>();
                    if (playerCamera != null && keepPlayer == null)
                    {
                        keepPlayer = player;
                        Debug.Log($"{GetTimestamp()} [SceneCleanup] 保留Lily: {player.name}，位置 {player.transform.position}");
                    }
                    else if (player != keepPlayer)
                    {
                        Debug.Log($"{GetTimestamp()} [SceneCleanup] 删除重复Lily: {player.name}");
                        DestroyImmediate(player.gameObject);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 清理重复的摄像机
    /// </summary>
    void CleanupDuplicateCameras()
    {
        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        
        if (cameras.Length > 1)
        {
            Debug.Log($"{GetTimestamp()} [SceneCleanup] 发现 {cameras.Length} 个摄像机，开始清理");
            
            Camera mainCamera = null;
            
            foreach (var camera in cameras)
            {
                if (camera != null)
                {
                    // 优先保留MainCamera标签的摄像机
                    if (camera.CompareTag("MainCamera") && mainCamera == null)
                    {
                        mainCamera = camera;
                        Debug.Log($"{GetTimestamp()} [SceneCleanup] 保留主摄像机: {camera.name}");
                    }
                    else if (camera != mainCamera)
                    {
                        // 检查是否是Emergency Camera
                        if (camera.name.Contains("Emergency"))
                        {
                            Debug.Log($"{GetTimestamp()} [SceneCleanup] 删除紧急摄像机: {camera.name}");
                            DestroyImmediate(camera.gameObject);
                        }
                    }
                }
            }
        }

        // 清理重复的AudioListener
        CleanupDuplicateAudioListeners();
    }
    
    /// <summary>
    /// 清理重复的AudioListener
    /// </summary>
    void CleanupDuplicateAudioListeners()
    {
        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        
        if (listeners.Length > 1)
        {
            Debug.Log($"{GetTimestamp()} [SceneCleanup] 发现 {listeners.Length} 个AudioListener，开始清理");
            
            AudioListener keepListener = null;
            
            foreach (var listener in listeners)
            {
                if (listener != null)
                {
                    // 优先保留MainCamera上的AudioListener
                    Camera camera = listener.GetComponent<Camera>();
                    if (camera != null && camera.CompareTag("MainCamera") && keepListener == null)
                    {
                        keepListener = listener;
                        Debug.Log($"{GetTimestamp()} [SceneCleanup] 保留AudioListener: {listener.name}");
                    }
                    else if (listener != keepListener)
                    {
                        Debug.Log($"{GetTimestamp()} [SceneCleanup] 删除重复AudioListener: {listener.name}");
                        DestroyImmediate(listener);
                    }
                }
            }
            
            // 如果没有找到MainCamera上的AudioListener，保留第一个
            if (keepListener == null && listeners.Length > 0)
            {
                keepListener = listeners[0];
                Debug.Log($"{GetTimestamp()} [SceneCleanup] 保留第一个AudioListener: {keepListener.name}");
                
                // 删除其他的
                for (int i = 1; i < listeners.Length; i++)
                {
                    if (listeners[i] != null)
                    {
                        Debug.Log($"{GetTimestamp()} [SceneCleanup] 删除重复AudioListener: {listeners[i].name}");
                        DestroyImmediate(listeners[i]);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 清理重复的UI系统
    /// </summary>
    void CleanupDuplicateUISystems()
    {
        InventoryUISystem[] uiSystems = FindObjectsByType<InventoryUISystem>(FindObjectsSortMode.None);
        
        if (uiSystems.Length > 1)
        {
            Debug.Log($"发现 {uiSystems.Length} 个UI系统，清理重复项");
            
            // 保留第一个，删除其他的
            for (int i = 1; i < uiSystems.Length; i++)
            {
                if (uiSystems[i] != null)
                {
                    Debug.Log($"删除重复UI系统: {uiSystems[i].name}");
                    DestroyImmediate(uiSystems[i].gameObject);
                }
            }
        }
    }
    
    /// <summary>
    /// 清理重复的工具管理器
    /// </summary>
    void CleanupDuplicateToolManagers()
    {
        ToolManager[] toolManagers = FindObjectsByType<ToolManager>(FindObjectsSortMode.None);
        
        if (toolManagers.Length > 1)
        {
            Debug.Log($"发现 {toolManagers.Length} 个工具管理器，清理重复项");
            
            // 保留第一个，删除其他的
            for (int i = 1; i < toolManagers.Length; i++)
            {
                if (toolManagers[i] != null)
                {
                    Debug.Log($"删除重复工具管理器: {toolManagers[i].name}");
                    DestroyImmediate(toolManagers[i].gameObject);
                }
            }
        }
    }
    
    /// <summary>
    /// 手动清理场景
    /// </summary>
    public static void ManualCleanup()
    {
        GameObject cleanupObj = new GameObject("SceneCleanup_Manual");
        SceneCleanup cleanup = cleanupObj.AddComponent<SceneCleanup>();
        cleanup.autoCleanOnStart = true;
        cleanup.cleanupDelay = 0.1f;
        
        Debug.Log("手动启动场景清理");
    }
    
    /// <summary>
    /// 清理可能与TabUI冲突的Canvas
    /// </summary>
    void CleanupConflictingCanvases()
    {
        // 查找所有可能冲突的Canvas
        string[] conflictingCanvasNames = {
            "SamplePromptCanvas",
            "PlacedSamplePromptCanvas"
        };

        // 白名单：演出/设置相关Canvas不清理
        string[] whitelistNames = {
            "SubtitleCanvas",
            "StartMenuCanvas",
            "SettingsCanvas",
            "SceneSelectionUI"
        };

        Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        int cleanedCount = 0;

        foreach (Canvas canvas in allCanvases)
        {
            if (canvas == null) continue;

            string canvasName = canvas.gameObject.name;

            // 跳过白名单
            bool inWhitelist = false;
            foreach (var w in whitelistNames)
            {
                if (canvasName.Contains(w)) { inWhitelist = true; break; }
            }
            if (inWhitelist) continue;

            // 名称匹配则清理并进入下一项，避免继续访问已销毁对象
            bool destroyed = false;
            foreach (string conflictingName in conflictingCanvasNames)
            {
                if (canvasName.Contains(conflictingName))
                {
                    Debug.Log($"清理冲突Canvas: {canvasName}");
                    DestroyImmediate(canvas.gameObject);
                    cleanedCount++;
                    destroyed = true;
                    break;
                }
            }
            if (destroyed) continue;

            // 检查是否有包含"Cycle"的子对象（保护空引用）
            try
            {
                var t = canvas.transform; // 若对象已被销毁将抛异常
                if (t != null && t.Find("Cycle") != null)
                {
                    Debug.Log($"清理包含Cycle的Canvas: {canvasName}");
                    DestroyImmediate(canvas.gameObject);
                    cleanedCount++;
                }
            }
            catch (System.Exception)
            {
                // 忽略已销毁引用
            }
        }

        if (cleanedCount > 0)
        {
            Debug.Log($"清理了 {cleanedCount} 个可能冲突的Canvas");
        }
    }

    string GetTimestamp()
    {
        return $"[{Time.time:F3}s]";
    }

    bool ShouldSkipRuntimeCleanup()
    {
        if (!Application.isPlaying)
        {
            return false;
        }

        var sceneManager = GameSceneManager.Instance;
        return sceneManager != null;
    }
}

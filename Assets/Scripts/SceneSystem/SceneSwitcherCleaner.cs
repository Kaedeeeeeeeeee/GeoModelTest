using UnityEngine;
using System.Collections;

/// <summary>
/// 场景切换器清理器 - 清理临时创建的丑陋模型，替换为用户的美观预制体
/// </summary>
public class SceneSwitcherCleaner : MonoBehaviour
{
    [Header("清理设置")]
    public bool autoCleanOnStart = true;
    public float cleanDelay = 1f;
    
    void Start()
    {
        if (autoCleanOnStart)
        {
            StartCoroutine(CleanupTemporaryModels());
        }
    }
    
    IEnumerator CleanupTemporaryModels()
    {
        yield return new WaitForSeconds(cleanDelay);
        
        Debug.Log("=== 开始清理临时场景切换器模型 ===");
        
        // 查找所有SceneSwitcherTool实例
        SceneSwitcherTool[] tools = FindObjectsByType<SceneSwitcherTool>(FindObjectsSortMode.None);
        
        foreach (var tool in tools)
        {
            if (tool != null)
            {
                CleanupToolModel(tool);
            }
        }
        
        // 查找并删除临时创建的模型
        CleanupTemporaryObjects();
        
        Debug.Log("=== 场景切换器模型清理完成 ===");
        
        // 自毁
        Destroy(this);
    }
    
    /// <summary>
    /// 清理工具模型
    /// </summary>
    void CleanupToolModel(SceneSwitcherTool tool)
    {
        // 检查工具是否有正确的预制体
        if (tool.switcherPrefab == null)
        {
            Debug.Log($"为工具 {tool.name} 加载正确的预制体");
            LoadCorrectPrefab(tool);
        }
        else if (tool.switcherPrefab.name.Contains("Default") || tool.switcherPrefab.name.Contains("Fallback"))
        {
            Debug.Log($"替换工具 {tool.name} 的临时预制体");
            LoadCorrectPrefab(tool);
        }
    }
    
    /// <summary>
    /// 为工具加载正确的预制体
    /// </summary>
    void LoadCorrectPrefab(SceneSwitcherTool tool)
    {
        // 尝试加载用户的SceneSwitcher预制体
#if UNITY_EDITOR
        GameObject userPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Model/SceneSwitcher/SceneSwitcher.prefab");
        if (userPrefab != null)
        {
            tool.switcherPrefab = userPrefab;
            Debug.Log("✅ 成功为工具加载用户的SceneSwitcher预制体");
            return;
        }
#endif
        
        // 尝试从Resources加载
        GameObject resourcePrefab = Resources.Load<GameObject>("Model/SceneSwitcher/SceneSwitcher");
        if (resourcePrefab != null)
        {
            tool.switcherPrefab = resourcePrefab;
            Debug.Log("✅ 成功从Resources为工具加载SceneSwitcher预制体");
            return;
        }
        
        Debug.LogWarning("❌ 无法加载用户的SceneSwitcher预制体");
    }
    
    /// <summary>
    /// 清理临时对象
    /// </summary>
    void CleanupTemporaryObjects()
    {
        // 查找并删除临时创建的对象
        string[] tempObjectNames = {
            "DefaultSceneSwitcher",
            "FallbackSceneSwitcher",
            "DefaultSwitcher"
        };
        
        foreach (string objName in tempObjectNames)
        {
            GameObject[] tempObjects = GameObject.FindGameObjectsWithTag("Untagged");
            foreach (GameObject obj in tempObjects)
            {
                if (obj.name.Contains(objName))
                {
                    Debug.Log($"删除临时对象: {obj.name}");
                    DestroyImmediate(obj);
                }
            }
        }
    }
    
    /// <summary>
    /// 手动清理方法（可通过其他脚本调用）
    /// </summary>
    public static void ManualCleanup()
    {
        GameObject cleanerObj = new GameObject("SceneSwitcherCleaner_Manual");
        SceneSwitcherCleaner cleaner = cleanerObj.AddComponent<SceneSwitcherCleaner>();
        cleaner.autoCleanOnStart = true;
        cleaner.cleanDelay = 0.1f;
        
        Debug.Log("手动启动场景切换器清理");
    }
    
    void Update()
    {
        // 按F7键手动触发清理
        if (Input.GetKeyDown(KeyCode.F7))
        {
            ManualCleanup();
        }
    }
}
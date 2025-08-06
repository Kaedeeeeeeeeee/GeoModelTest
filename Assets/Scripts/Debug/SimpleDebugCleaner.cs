using UnityEngine;
using System.Reflection;

/// <summary>
/// 简单的调试清理器 - 安全地清理已确认存在的系统
/// </summary>
public class SimpleDebugCleaner : MonoBehaviour
{
    [Header("清理选项")]
    [SerializeField] private bool cleanOnStart = true;
    [SerializeField] private bool showCleanupLog = true;
    
    void Start()
    {
        if (cleanOnStart)
        {
            CleanupDebugOutput();
        }
    }
    
    /// <summary>
    /// 清理调试输出
    /// </summary>
    [ContextMenu("清理调试输出")]
    public void CleanupDebugOutput()
    {
        int totalCleaned = 0;
        
        // 清理Encyclopedia系统
        totalCleaned += CleanEncyclopediaSystem();
        
        // 清理Localization系统  
        totalCleaned += CleanLocalizationSystem();
        
        // 清理Warehouse系统
        totalCleaned += CleanWarehouseSystem();
        
        // 清理GameInitializer系统
        totalCleaned += CleanGameInitializer();
        
        // 清理ManualSampleSetup系统
        totalCleaned += CleanSampleSystem();
        
        if (showCleanupLog)
        {
            Debug.Log($"🔇 SimpleDebugCleaner: 成功清理 {totalCleaned} 个组件的调试输出");
        }
    }
    
    /// <summary>
    /// 清理Encyclopedia系统
    /// </summary>
    private int CleanEncyclopediaSystem()
    {
        int count = 0;
        
        // 清理SimpleEncyclopediaManager
        var encyclopediaManagers = FindObjectsByType<Encyclopedia.SimpleEncyclopediaManager>(FindObjectsSortMode.None);
        foreach (var manager in encyclopediaManagers)
        {
            SetBoolField(manager, "showDebugInfo", false);
            count++;
        }
        
        return count;
    }
    
    /// <summary>
    /// 清理Localization系统
    /// </summary>
    private int CleanLocalizationSystem()
    {
        int count = 0;
        
        // LocalizationManager
        if (LocalizationManager.Instance != null)
        {
            SetBoolField(LocalizationManager.Instance, "enableDebugLog", false);
            count++;
        }
        
        // LocalizationInitializer
        var initializers = FindObjectsByType<LocalizationInitializer>(FindObjectsSortMode.None);
        foreach (var initializer in initializers)
        {
            SetBoolField(initializer, "enableDebugLog", false);
            count++;
        }
        
        return count;
    }
    
    /// <summary>
    /// 清理Warehouse系统
    /// </summary>
    private int CleanWarehouseSystem()
    {
        int count = 0;
        
        // WarehouseManager
        var managers = FindObjectsByType<WarehouseManager>(FindObjectsSortMode.None);
        foreach (var manager in managers)
        {
            SetBoolField(manager, "enableDebugLog", false);
            count++;
        }
        
        return count;
    }
    
    /// <summary>
    /// 清理GameInitializer系统
    /// </summary>
    private int CleanGameInitializer()
    {
        int count = 0;
        
        var initializers = FindObjectsByType<GameInitializer>(FindObjectsSortMode.None);
        foreach (var initializer in initializers)
        {
            SetBoolField(initializer, "enableDebugMode", false);
            count++;
        }
        
        return count;
    }
    
    /// <summary>
    /// 清理Sample系统
    /// </summary>
    private int CleanSampleSystem()
    {
        int count = 0;
        
        // ManualSampleSetup
        var setups = FindObjectsByType<ManualSampleSetup>(FindObjectsSortMode.None);
        foreach (var setup in setups)
        {
            SetBoolField(setup, "enableDebugMode", false);
            count++;
        }
        
        return count;
    }
    
    /// <summary>
    /// 安全地设置布尔字段
    /// </summary>
    private void SetBoolField(object obj, string fieldName, bool value)
    {
        if (obj == null) return;
        
        try
        {
            var type = obj.GetType();
            var field = type.GetField(fieldName, 
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (field != null && field.FieldType == typeof(bool))
            {
                field.SetValue(obj, value);
                if (showCleanupLog)
                {
                    Debug.Log($"✅ 已禁用 {type.Name}.{fieldName}");
                }
            }
            else if (showCleanupLog)
            {
                Debug.LogWarning($"⚠️ 字段 {type.Name}.{fieldName} 不存在或类型不匹配");
            }
        }
        catch (System.Exception e)
        {
            if (showCleanupLog)
            {
                Debug.LogError($"❌ 设置字段失败: {e.Message}");
            }
        }
    }
}
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using System;
using Encyclopedia;
using SampleCuttingSystem;

/// <summary>
/// 全局调试控制器 - 一键管理整个项目的Debug.Log输出
/// 可以禁用/启用各个系统的调试输出，提高性能和控制台清洁度
/// </summary>
public class GlobalDebugController : MonoBehaviour
{
    [Header("调试控制设置")]
    [Tooltip("启用此选项可以看到详细的操作日志")]
    public bool showOperationLogs = true;
    
    [Header("系统调试开关")]
    public bool encyclopediaSystemDebug = false;
    public bool localizationSystemDebug = false;
    public bool warehouseSystemDebug = false;
    public bool sampleSystemDebug = false;
    public bool gameInitializerDebug = false;
    public bool drillTowerSystemDebug = false;
    public bool sceneSystemDebug = false;
    
    [Header("快速操作")]
    [Space]
    public bool disableAllSystemsOnStart = true;
    
    // 存储各系统的调试字段信息
    private Dictionary<Type, List<DebugFieldInfo>> systemDebugFields = new Dictionary<Type, List<DebugFieldInfo>>();
    
    private struct DebugFieldInfo
    {
        public string fieldName;
        public FieldInfo fieldInfo;
        public UnityEngine.Object targetObject;
        public bool originalValue;
    }
    
    void Start()
    {
        if (disableAllSystemsOnStart)
        {
            DisableAllSystemDebugLogs();
        }
        
        // 扫描并缓存所有调试字段
        ScanAndCacheDebugFields();
    }
    
    /// <summary>
    /// 扫描并缓存所有系统的调试字段
    /// </summary>
    private void ScanAndCacheDebugFields()
    {
        if (showOperationLogs)
            Debug.Log("🔍 扫描项目中的调试字段...");
        
        // Encyclopedia 系统
        CacheDebugFields<SimpleEncyclopediaManager>("showDebugInfo", encyclopediaSystemDebug);
        CacheDebugFields<Sample3DModelViewer>("showDebugInfo", encyclopediaSystemDebug);
        
        // Localization 系统
        CacheDebugFields<LocalizedText>("enableDebugLog", localizationSystemDebug);
        
        // Warehouse 系统
        CacheDebugFields<WarehouseUI>("enableDebugLogging", warehouseSystemDebug);
        CacheDebugFields<MultiSelectSystem>("enableDebugLogging", warehouseSystemDebug);
        
        // Sample 系统
        CacheDebugFields<SampleIconDebugger>("enableDetailedLogging", sampleSystemDebug);
        CacheDebugFields<SamplePlacer>("enableDebugLogging", sampleSystemDebug);
        
        // DrillTower 系统
        CacheDebugFields<DrillTowerDebugger>("enableDebugLog", drillTowerSystemDebug);
        CacheDebugFields<DrillTowerDebuggerSimple>("enableDebugLogging", drillTowerSystemDebug);
        
        if (showOperationLogs)
            Debug.Log($"✅ 扫描完成，找到 {systemDebugFields.Count} 个系统的调试字段");
    }
    
    /// <summary>
    /// 缓存指定类型的调试字段
    /// </summary>
    private void CacheDebugFields<T>(string fieldName, bool systemEnabled) where T : UnityEngine.Object
    {
        var objects = FindObjectsByType<T>(FindObjectsSortMode.None);
        if (objects.Length == 0) return;
        
        Type type = typeof(T);
        if (!systemDebugFields.ContainsKey(type))
            systemDebugFields[type] = new List<DebugFieldInfo>();
        
        foreach (var obj in objects)
        {
            var fieldInfo = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (fieldInfo != null && fieldInfo.FieldType == typeof(bool))
            {
                var debugField = new DebugFieldInfo
                {
                    fieldName = fieldName,
                    fieldInfo = fieldInfo,
                    targetObject = obj,
                    originalValue = (bool)fieldInfo.GetValue(obj)
                };
                systemDebugFields[type].Add(debugField);
            }
        }
    }
    
    /// <summary>
    /// 一键禁用所有系统的调试日志
    /// </summary>
    [ContextMenu("🔇 禁用所有系统调试日志")]
    public void DisableAllSystemDebugLogs()
    {
        if (showOperationLogs)
            Debug.Log("🔇 开始禁用所有系统调试日志...");
        
        int totalDisabled = 0;
        
        // Encyclopedia 系统
        totalDisabled += SetSystemDebugState<SimpleEncyclopediaManager>("showDebugInfo", false);
        totalDisabled += SetSystemDebugState<Sample3DModelViewer>("showDebugInfo", false);
        totalDisabled += SetSystemDebugState<EncyclopediaDebugHelper>("showDebugInfo", false);
        
        // Localization 系统  
        totalDisabled += SetSystemDebugState<LocalizedText>("enableDebugLog", false);
        
        // Warehouse 系统
        totalDisabled += SetSystemDebugState<WarehouseUI>("enableDebugLogging", false);
        totalDisabled += SetSystemDebugState<MultiSelectSystem>("enableDebugLogging", false);
        totalDisabled += SetSystemDebugState<WarehouseManager>("enableDebugLog", false);
        // WarehouseStorage不是MonoBehaviour，跳过
        
        // Sample 系统
        totalDisabled += SetSystemDebugState<SampleIconDebugger>("enableDetailedLogging", false);
        totalDisabled += SetSystemDebugState<SamplePlacer>("enableDebugLogging", false);
        totalDisabled += SetSystemDebugState<SampleIconGenerator>("enableDebugLogging", false);
        
        // DrillTower 系统
        totalDisabled += SetSystemDebugState<DrillTowerDebugger>("enableDebugLog", false);
        totalDisabled += SetSystemDebugState<DrillTowerDebuggerSimple>("enableDebugLogging", false);
        
        // Scene 系统
        totalDisabled += SetSystemDebugState<GameSceneManager>("enableDebugLogging", false);
        totalDisabled += SetSystemDebugState<SceneSwitcherInitializer>("enableDebugLogging", false);
        
        // GameInitializer
        totalDisabled += SetSystemDebugState<GameInitializer>("enableDebugLogging", false);
        
        if (showOperationLogs)
        {
            Debug.Log($"✅ 调试日志禁用完成！共禁用了 {totalDisabled} 个组件的调试输出");
            Debug.Log("🎉 Console 现在应该安静多了！");
        }
    }
    
    /// <summary>
    /// 一键启用所有系统的调试日志（谨慎使用）
    /// </summary>
    [ContextMenu("🔊 启用所有系统调试日志（谨慎使用）")]
    public void EnableAllSystemDebugLogs()
    {
        Debug.LogWarning("⚠️ 启用所有调试日志会产生大量输出，建议只在调试时使用");
        
        int totalEnabled = 0;
        
        // Encyclopedia 系统
        totalEnabled += SetSystemDebugState<SimpleEncyclopediaManager>("showDebugInfo", true);
        totalEnabled += SetSystemDebugState<Sample3DModelViewer>("showDebugInfo", true);
        
        // Localization 系统
        totalEnabled += SetSystemDebugState<LocalizedText>("enableDebugLog", true);
        
        // Warehouse 系统  
        totalEnabled += SetSystemDebugState<WarehouseUI>("enableDebugLogging", true);
        totalEnabled += SetSystemDebugState<MultiSelectSystem>("enableDebugLogging", true);
        
        // Sample 系统
        totalEnabled += SetSystemDebugState<SampleIconDebugger>("enableDetailedLogging", true);
        totalEnabled += SetSystemDebugState<SamplePlacer>("enableDebugLogging", true);
        
        Debug.Log($"🔊 已启用 {totalEnabled} 个组件的调试输出");
    }
    
    /// <summary>
    /// 设置指定系统的调试状态
    /// </summary>
    private int SetSystemDebugState<T>(string fieldName, bool enabled) where T : MonoBehaviour
    {
        var objects = FindObjectsByType<T>(FindObjectsSortMode.None);
        if (objects.Length == 0) return 0;
        
        int changedCount = 0;
        
        foreach (var obj in objects)
        {
            var fieldInfo = typeof(T).GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (fieldInfo != null && fieldInfo.FieldType == typeof(bool))
            {
                bool currentValue = (bool)fieldInfo.GetValue(obj);
                if (currentValue != enabled)
                {
                    fieldInfo.SetValue(obj, enabled);
                    changedCount++;
                    
                    if (showOperationLogs && enabled)
                        Debug.Log($"  - 启用 {obj.gameObject.name}({typeof(T).Name}).{fieldName}");
                    else if (showOperationLogs)
                        Debug.Log($"  - 禁用 {obj.gameObject.name}({typeof(T).Name}).{fieldName}");
                }
            }
        }
        
        return changedCount;
    }
    
    /// <summary>
    /// 显示所有系统的调试状态统计
    /// </summary>
    [ContextMenu("📊 显示调试状态统计")]
    public void ShowDebugStatusReport()
    {
        Debug.Log("=== 📊 系统调试状态报告 ===");
        Debug.Log("".PadRight(50, '='));
        
        // Encyclopedia 系统
        ReportSystemStatus<SimpleEncyclopediaManager>("Encyclopedia 系统", "showDebugInfo");
        ReportSystemStatus<Sample3DModelViewer>("3D模型查看器", "showDebugInfo");
        
        // Localization 系统
        ReportSystemStatus<LocalizedText>("多语言系统", "enableDebugLog");
        
        // Warehouse 系统
        ReportSystemStatus<WarehouseUI>("仓库UI系统", "enableDebugLogging");
        ReportSystemStatus<MultiSelectSystem>("多选系统", "enableDebugLogging");
        
        // Sample 系统
        ReportSystemStatus<SampleIconDebugger>("样本图标调试器", "enableDetailedLogging");
        ReportSystemStatus<SamplePlacer>("样本放置器", "enableDebugLogging");
        
        // DrillTower 系统
        ReportSystemStatus<DrillTowerDebugger>("钻塔调试器", "enableDebugLog");
        
        Debug.Log("".PadRight(50, '='));
        Debug.Log("📝 提示: 使用上下文菜单快速禁用/启用调试输出");
    }
    
    /// <summary>
    /// 报告指定系统的调试状态
    /// </summary>
    private void ReportSystemStatus<T>(string systemName, string fieldName) where T : UnityEngine.Object
    {
        var objects = FindObjectsByType<T>(FindObjectsSortMode.None);
        if (objects.Length == 0)
        {
            Debug.Log($"🔍 {systemName}: 未找到组件");
            return;
        }
        
        int enabledCount = 0;
        int totalCount = objects.Length;
        
        foreach (var obj in objects)
        {
            var fieldInfo = typeof(T).GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (fieldInfo != null && fieldInfo.FieldType == typeof(bool))
            {
                bool isEnabled = (bool)fieldInfo.GetValue(obj);
                if (isEnabled) enabledCount++;
            }
        }
        
        string status = enabledCount == 0 ? "🔇 全部静音" : 
                       enabledCount == totalCount ? "🔊 全部开启" : 
                       $"🔘 部分开启 ({enabledCount}/{totalCount})";
        
        Debug.Log($"📋 {systemName}: {status}");
    }
    
    /// <summary>
    /// 仅禁用高频输出系统（推荐使用）
    /// </summary>
    [ContextMenu("🎯 仅禁用高频输出系统（推荐）")]
    public void DisableHighFrequencySystems()
    {
        if (showOperationLogs)
            Debug.Log("🎯 禁用高频输出系统...");
        
        int totalDisabled = 0;
        
        // 最高频的系统 - Encyclopedia (139个)
        totalDisabled += SetSystemDebugState<Sample3DModelViewer>("showDebugInfo", false);
        totalDisabled += SetSystemDebugState<SimpleEncyclopediaManager>("showDebugInfo", false);
        
        // 高频系统 - Sample (92个)
        totalDisabled += SetSystemDebugState<SampleIconDebugger>("enableDetailedLogging", false);
        totalDisabled += SetSystemDebugState<SampleIconTester>("enableDetailedLogging", false);
        totalDisabled += SetSystemDebugState<SamplePlacer>("enableDebugLogging", false);
        
        // GameInitializer (52个)
        totalDisabled += SetSystemDebugState<GameInitializer>("enableDebugLogging", false);
        
        if (showOperationLogs)
        {
            Debug.Log($"✅ 高频系统调试禁用完成！共禁用了 {totalDisabled} 个组件");
            Debug.Log("💡 这应该能显著减少Console输出量");
        }
    }
    
    /// <summary>
    /// 一键清理Console
    /// </summary>
    [ContextMenu("🧹 清理Console")]
    public void ClearConsole()
    {
        // 使用反射调用Unity编辑器的清理Console功能
        #if UNITY_EDITOR
        var assembly = System.Reflection.Assembly.GetAssembly(typeof(UnityEditor.SceneView));
        var logEntries = assembly.GetType("UnityEditor.LogEntries");
        var clearMethod = logEntries.GetMethod("Clear");
        clearMethod?.Invoke(new object(), null);
        
        if (showOperationLogs)
            Debug.Log("🧹 Console已清理完成");
        #endif
    }
}
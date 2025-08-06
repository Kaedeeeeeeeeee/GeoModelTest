using UnityEngine;

/// <summary>
/// 手动样本设置工具 - 为场景中的现有样本添加采集功能
/// </summary>
public class ManualSampleSetup : MonoBehaviour
{
    [Header("设置选项")]
    public bool autoSetupOnStart = true;
    public bool includeExistingSamples = true;
    public bool enableDebugMode = false;
    
    [Header("搜索关键词")]
    public string[] sampleKeywords = { "Sample", "样本", "Drill", "钻探", "Geometric" };
    
    void Start()
    {
        if (autoSetupOnStart)
        {
            // 延迟执行，确保所有系统都已初始化
            Invoke(nameof(SetupAllSamples), 2f);
        }
    }
    
    /// <summary>
    /// 设置所有样本
    /// </summary>
    [ContextMenu("设置所有样本")]
    public void SetupAllSamples()
    {
        LogMessage("开始设置场景中的所有样本...");
        
        int setupCount = 0;
        
        // 1. 查找所有几何样本组件
        GeometricSampleInfo[] geometricSamples = FindObjectsByType<GeometricSampleInfo>(FindObjectsSortMode.None);
        foreach (var sample in geometricSamples)
        {
            if (SetupSampleForCollection(sample.gameObject, "geometric"))
            {
                setupCount++;
            }
        }
        
        // 2. 根据名称搜索可能的样本对象
        if (includeExistingSamples)
        {
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var obj in allObjects)
            {
                if (IsSampleObject(obj) && !HasCollectionComponent(obj))
                {
                    if (SetupSampleForCollection(obj, "existing"))
                    {
                        setupCount++;
                    }
                }
            }
        }
        
        LogMessage($"样本设置完成！共设置了 {setupCount} 个样本");
        
        // 3. 手动触发钻探工具集成器的检查
        if (DrillToolSampleIntegrator.Instance != null)
        {
            DrillToolSampleIntegrator.IntegrateAllSamplesInScene();
        }
    }
    
    /// <summary>
    /// 为单个样本设置采集功能
    /// </summary>
    bool SetupSampleForCollection(GameObject sampleObject, string sampleType)
    {
        if (sampleObject == null) return false;
        
        // 检查是否已有采集组件
        if (HasCollectionComponent(sampleObject)) 
        {
            LogMessage($"样本 {sampleObject.name} 已有采集组件，跳过");
            return false;
        }
        
        // 添加 SampleCollector 组件
        SampleCollector collector = sampleObject.AddComponent<SampleCollector>();
        
        // 设置源工具ID
        string sourceToolID = DetermineSourceToolID(sampleObject);
        collector.sourceToolID = sourceToolID;
        
        // 设置交互范围
        collector.interactionRange = 3f;
        
        LogMessage($"已为样本 {sampleObject.name} 添加采集组件 (类型: {sampleType}, 工具ID: {sourceToolID})");
        return true;
    }
    
    /// <summary>
    /// 检查对象是否为样本
    /// </summary>
    bool IsSampleObject(GameObject obj)
    {
        string objName = obj.name.ToLower();
        
        foreach (string keyword in sampleKeywords)
        {
            if (objName.Contains(keyword.ToLower()))
            {
                return true;
            }
        }
        
        // 检查是否有地质样本相关组件
        if (obj.GetComponent<GeometricSampleInfo>() != null)
        {
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 检查是否已有采集组件
    /// </summary>
    bool HasCollectionComponent(GameObject obj)
    {
        return obj.GetComponent<SampleCollector>() != null || 
               obj.GetComponent<PlacedSampleCollector>() != null;
    }
    
    /// <summary>
    /// 确定源工具ID
    /// </summary>
    string DetermineSourceToolID(GameObject sampleObject)
    {
        string objectName = sampleObject.name.ToLower();
        
        // 根据对象名称推断工具类型
        if (objectName.Contains("simple") || objectName.Contains("boring"))
        {
            return "1000"; // SimpleDrillTool
        }
        else if (objectName.Contains("tower") || objectName.Contains("drill"))
        {
            return "1001"; // DrillTowerTool
        }
        else if (objectName.Contains("geometric"))
        {
            // 检查是否有深度标记组件来判断是否来自钻塔
            var depthMarker = sampleObject.GetComponent<DepthSampleMarker>();
            if (depthMarker != null)
            {
                return "1001"; // DrillTowerTool
            }
            else
            {
                return "1000"; // 默认为SimpleDrillTool
            }
        }
        
        return "1000"; // 默认为简易钻探工具
    }
    
    /// <summary>
    /// 查找并设置特定名称的样本
    /// </summary>
    [ContextMenu("设置特定样本")]
    public void SetupSpecificSample()
    {
        // 查找第一个样本对象进行测试
        GeometricSampleInfo sample = FindFirstObjectByType<GeometricSampleInfo>();
        if (sample != null)
        {
            SetupSampleForCollection(sample.gameObject, "manual");
            LogMessage($"已手动设置样本: {sample.name}");
        }
        else
        {
            LogMessage("未找到样本对象进行设置");
        }
    }
    
    /// <summary>
    /// 清理所有采集组件
    /// </summary>
    [ContextMenu("清理所有采集组件")]
    public void CleanupAllCollectionComponents()
    {
        int cleanupCount = 0;
        
        // 清理 SampleCollector 组件
        SampleCollector[] collectors = FindObjectsByType<SampleCollector>(FindObjectsSortMode.None);
        foreach (var collector in collectors)
        {
            if (collector != null)
            {
                DestroyImmediate(collector);
                cleanupCount++;
            }
        }
        
        // 清理 PlacedSampleCollector 组件
        PlacedSampleCollector[] placedCollectors = FindObjectsByType<PlacedSampleCollector>(FindObjectsSortMode.None);
        foreach (var collector in placedCollectors)
        {
            if (collector != null)
            {
                DestroyImmediate(collector);
                cleanupCount++;
            }
        }
        
        LogMessage($"已清理 {cleanupCount} 个采集组件");
    }
    
    /// <summary>
    /// 日志输出
    /// </summary>
    void LogMessage(string message)
    {
        if (enableDebugMode)
        {
            Debug.Log($"[ManualSampleSetup] {message}");
        }
    }
    
    /// <summary>
    /// 获取场景样本统计
    /// </summary>
    [ContextMenu("显示样本统计")]
    public void ShowSampleStats()
    {
        int geometricSamples = FindObjectsByType<GeometricSampleInfo>(FindObjectsSortMode.None).Length;
        int collectorsCount = FindObjectsByType<SampleCollector>(FindObjectsSortMode.None).Length;
        int placedCollectorsCount = FindObjectsByType<PlacedSampleCollector>(FindObjectsSortMode.None).Length;
        
        string stats = "=== 场景样本统计 ===\n";
        stats += $"几何样本数量: {geometricSamples}\n";
        stats += $"采集组件数量: {collectorsCount}\n";
        stats += $"已放置采集组件数量: {placedCollectorsCount}\n";
        stats += $"未设置采集的样本: {geometricSamples - collectorsCount}\n";
        
        Debug.Log(stats);
    }
}
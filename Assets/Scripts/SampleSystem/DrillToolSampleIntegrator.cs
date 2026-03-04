using UnityEngine;
using System.Collections;

/// <summary>
/// 钻探工具样本集成器 - 为钻探工具生成的样本自动添加收集功能
/// </summary>
public class DrillToolSampleIntegrator : MonoBehaviour
{
    [Header("集成设置")]
    public bool enableAutoIntegration = false; // 默认关闭自动扫描
    public float integrationDelay = 0.1f; // 延迟时间，等待样本完全生成
    public bool enableOnDemandIntegration = true; // 启用按需集成（推荐）
    
    [Header("监听设置")]
    public bool monitorGeometricSamples = true;
    public bool monitorSimpleSamples = true;
    
    [Header("调试")]
    public bool enableDebugLog = false; // 默认关闭调试日志
    
    // 单例模式
    public static DrillToolSampleIntegrator Instance { get; private set; }
    
    void Awake()
    {
        // 单例模式设置
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeIntegrator();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // 只在允许自动扫描时才启动监听
        if (enableAutoIntegration)
        {
            SetupSampleMonitoring();
            LogMessage("ℹ️ 注意：自动扫描已启用，将每2秒检查新样本");
        }
        else
        {
            LogMessage("✅ 推荐模式：只在钻探操作后集成样本");
        }
    }
    
    /// <summary>
    /// 初始化集成器
    /// </summary>
    void InitializeIntegrator()
    {
        LogMessage("钻探工具样本集成器已初始化");
    }
    
    /// <summary>
    /// 设置样本监听（仅在自动扫描模式下使用）
    /// </summary>
    void SetupSampleMonitoring()
    {
        if (enableAutoIntegration)
        {
            // 启动定期检查新生成的样本
            InvokeRepeating(nameof(CheckForNewSamples), 1f, 2f);
            LogMessage("⚠️ 自动扫描已启动，可能产生频繁日志输出");
        }
    }
    
    /// <summary>
    /// 检查新生成的样本
    /// </summary>
    void CheckForNewSamples()
    {
        if (!enableAutoIntegration) return;
        
        // 查找所有可能是钻探样本的对象
        if (monitorGeometricSamples)
        {
            CheckGeometricSamples();
        }
        
        if (monitorSimpleSamples)
        {
            CheckSimpleSamples();
        }
    }
    
    /// <summary>
    /// 检查几何样本
    /// </summary>
    void CheckGeometricSamples()
    {
        // 查找所有GeometricSampleInfo组件
        GeometricSampleInfo[] geometricSamples = FindObjectsByType<GeometricSampleInfo>(FindObjectsSortMode.None);
        
        foreach (var sample in geometricSamples)
        {
            if (sample != null && !HasCollectionComponent(sample.gameObject))
            {
                StartCoroutine(IntegrateSampleWithDelay(sample.gameObject, "geometric"));
            }
        }
    }
    
    /// <summary>
    /// 检查简单样本
    /// </summary>
    void CheckSimpleSamples()
    {
        // 查找所有可能的简单钻探样本
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        
        foreach (var obj in allObjects)
        {
            // 排除实验台等设施对象
            string objName = obj.name.ToLower();
            string[] excludeKeywords = { "laboratory", "cutting", "station", "table", "desk", "workstation" };
            bool shouldExclude = false;
            foreach (string exclude in excludeKeywords)
            {
                if (objName.Contains(exclude.ToLower()))
                {
                    shouldExclude = true;
                    break;
                }
            }
            
            // 检查是否是钻探样本
            if (!shouldExclude && 
                obj.name.Contains("Sample") && 
                (obj.name.Contains("Drill") || obj.name.Contains("Boring")) &&
                !HasCollectionComponent(obj))
            {
                StartCoroutine(IntegrateSampleWithDelay(obj, "simple"));
            }
        }
    }
    
    /// <summary>
    /// 检查对象是否已有收集组件
    /// </summary>
    bool HasCollectionComponent(GameObject obj)
    {
        return obj.GetComponent<SampleCollector>() != null || 
               obj.GetComponent<PlacedSampleCollector>() != null;
    }
    
    /// <summary>
    /// 延迟集成样本
    /// </summary>
    IEnumerator IntegrateSampleWithDelay(GameObject sampleObject, string sampleType)
    {
        if (sampleObject == null) yield break;
        
        yield return new WaitForSeconds(integrationDelay);
        
        // 再次检查对象是否仍然存在且未被集成
        if (sampleObject != null && !HasCollectionComponent(sampleObject))
        {
            IntegrateSample(sampleObject, sampleType);
        }
    }
    
    /// <summary>
    /// 为样本添加收集组件（公共接口，供钻探工具调用）
    /// </summary>
    public static void IntegrateSample(GameObject sampleObject, string sampleType = "unknown")
    {
        if (sampleObject == null)
        {
            Debug.LogWarning("尝试集成空的样本对象");
            return;
        }
        
        // 检查是否已有收集组件
        if (Instance != null && Instance.HasCollectionComponent(sampleObject))
        {
            Instance.LogMessage($"样本 {sampleObject.name} 已有收集组件，跳过集成");
            return;
        }
        
        // 添加SampleCollector组件
        SampleCollector collector = sampleObject.GetComponent<SampleCollector>();
        if (collector == null)
        {
            collector = sampleObject.AddComponent<SampleCollector>();
        }
        
        // 设置源工具ID
        string sourceToolID = DetermineSourceToolID(sampleObject, sampleType);
        collector.sourceToolID = sourceToolID;
        
        // 尝试自动生成样本数据
        if (collector.sampleData == null)
        {
            collector.sampleData = SampleItem.CreateFromGeologicalSample(sampleObject, sourceToolID);
        }

        LogLayerInfo("IntegrateSample", collector.sampleData);
        Instance?.LogMessage($"✅ 已为样本 {sampleObject.name} 添加收集组件 (类型: {sampleType}, 工具ID: {sourceToolID})");
    }
    
    /// <summary>
    /// 确定源工具ID
    /// </summary>
    static string DetermineSourceToolID(GameObject sampleObject, string sampleType)
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
        else if (sampleType == "geometric")
        {
            // 地质样本可能来自多种工具，需要进一步判断
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
    /// 钻探后立即集成样本（推荐方式 - 由钻探工具调用）
    /// </summary>
    public static void IntegrateSampleAfterDrilling(GameObject sampleObject, string sourceToolID, string drillType = "unknown")
    {
        if (sampleObject == null || Instance == null) return;
        
        if (!Instance.enableOnDemandIntegration)
        {
            Instance.LogMessage("⚠️ 按需集成已禁用，跳过样本集成");
            return;
        }
        
        Instance.LogMessage($"🔧 钻探完成，开始集成样本: {sampleObject.name} (工具ID: {sourceToolID})");
        
        // 直接集成，不需要延迟（因为样本刚刚生成完毕）
        if (!Instance.HasCollectionComponent(sampleObject))
        {
            // 添加SampleCollector组件
            SampleCollector collector = sampleObject.GetComponent<SampleCollector>();
            if (collector == null)
            {
                collector = sampleObject.AddComponent<SampleCollector>();
            }
            
            // 设置源工具ID
            collector.sourceToolID = sourceToolID;
            
            // 生成样本数据
            if (collector.sampleData == null)
            {
                collector.sampleData = SampleItem.CreateFromGeologicalSample(sampleObject, sourceToolID);
            }

            LogLayerInfo("IntegrateSampleAfterDrilling", collector.sampleData);
            Instance.LogMessage($"✅ 钻探样本集成完成: {sampleObject.name} ({drillType})");
        }
        else
        {
            Instance.LogMessage($"ℹ️ 样本 {sampleObject.name} 已有收集组件，跳过");
        }
    }

    static void LogLayerInfo(string context, SampleItem sampleData)
    {
        if (sampleData == null)
        {
            Debug.Log($"[DrillToolSampleIntegrator] {context} layer info: sampleData=null");
            return;
        }

        int layerCount = sampleData.geologicalLayers?.Count ?? 0;
        string firstLayerName = layerCount > 0 ? sampleData.geologicalLayers[0].layerName : "None";
        Debug.Log($"[DrillToolSampleIntegrator] {context} layer info: count={layerCount}, first={firstLayerName}");
    }
    
    /// <summary>
    /// 手动集成指定样本（保留原有接口）
    /// </summary>
    public static void ManuallyIntegrateSample(GameObject sampleObject, string sourceToolID = null)
    {
        if (sampleObject == null) return;
        
        string toolID = sourceToolID ?? DetermineSourceToolID(sampleObject, "manual");
        IntegrateSample(sampleObject, "manual");
        
        // 如果提供了特定的工具ID，更新它
        if (!string.IsNullOrEmpty(sourceToolID))
        {
            var collector = sampleObject.GetComponent<SampleCollector>();
            if (collector != null)
            {
                collector.sourceToolID = sourceToolID;
                if (collector.sampleData != null)
                {
                    collector.sampleData.sourceToolID = sourceToolID;
                }
            }
        }
    }
    
    /// <summary>
    /// 手动集成场景中的所有样本
    /// </summary>
    public static void IntegrateAllSamplesInScene()
    {
        if (Instance == null)
        {
            Debug.LogWarning("DrillToolSampleIntegrator 实例不存在");
            return;
        }
        
        Instance.LogMessage("开始手动集成场景中的所有样本");
        
        // 集成几何样本
        GeometricSampleInfo[] geometricSamples = FindObjectsByType<GeometricSampleInfo>(FindObjectsSortMode.None);
        foreach (var sample in geometricSamples)
        {
            if (sample != null && !Instance.HasCollectionComponent(sample.gameObject))
            {
                IntegrateSample(sample.gameObject, "geometric");
            }
        }
        
        // 集成简单样本
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var obj in allObjects)
        {
            // 排除实验台等设施对象
            string objName = obj.name.ToLower();
            string[] excludeKeywords = { "laboratory", "cutting", "station", "table", "desk", "workstation" };
            bool shouldExclude = false;
            foreach (string exclude in excludeKeywords)
            {
                if (objName.Contains(exclude.ToLower()))
                {
                    shouldExclude = true;
                    Debug.Log($"🛡️ DrillToolSampleIntegrator 排除对象: {obj.name} (包含关键词: {exclude})");
                    break;
                }
            }
            
            if (!shouldExclude && 
                obj.name.Contains("Sample") && 
                (obj.name.Contains("Drill") || obj.name.Contains("Boring")) &&
                !Instance.HasCollectionComponent(obj))
            {
                IntegrateSample(obj, "simple");
            }
        }
        
        Instance.LogMessage("场景样本集成完成");
    }
    
    /// <summary>
    /// 启用/禁用自动集成
    /// </summary>
    public void SetAutoIntegration(bool enabled)
    {
        enableAutoIntegration = enabled;
        
        if (enabled)
        {
            // 重新启动定期检查
            CancelInvoke(nameof(CheckForNewSamples));
            InvokeRepeating(nameof(CheckForNewSamples), 1f, 2f);
            LogMessage("自动样本集成已启用");
        }
        else
        {
            // 停止定期检查
            CancelInvoke(nameof(CheckForNewSamples));
            LogMessage("自动样本集成已禁用");
        }
    }
    
    /// <summary>
    /// 获取集成统计信息
    /// </summary>
    public string GetIntegrationStats()
    {
        int totalSamples = 0;
        int integratedSamples = 0;
        int unintegratedSamples = 0;
        
        // 统计几何样本
        GeometricSampleInfo[] geometricSamples = FindObjectsByType<GeometricSampleInfo>(FindObjectsSortMode.None);
        foreach (var sample in geometricSamples)
        {
            if (sample != null)
            {
                totalSamples++;
                if (HasCollectionComponent(sample.gameObject))
                {
                    integratedSamples++;
                }
                else
                {
                    unintegratedSamples++;
                }
            }
        }
        
        // 统计简单样本
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var obj in allObjects)
        {
            if (obj.name.Contains("Sample") && 
                (obj.name.Contains("Drill") || obj.name.Contains("Boring")))
            {
                totalSamples++;
                if (HasCollectionComponent(obj))
                {
                    integratedSamples++;
                }
                else
                {
                    unintegratedSamples++;
                }
            }
        }
        
        string stats = "=== 钻探样本集成统计 ===\n";
        stats += $"总样本数: {totalSamples}\n";
        stats += $"已集成: {integratedSamples}\n";
        stats += $"未集成: {unintegratedSamples}\n";
        stats += $"自动集成: {(enableAutoIntegration ? "开启" : "关闭")}\n";
        
        return stats;
    }
    
    /// <summary>
    /// 日志输出
    /// </summary>
    void LogMessage(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[DrillToolSampleIntegrator] {message}");
        }
    }
    
    void OnDestroy()
    {
        CancelInvoke();
    }
    
    /// <summary>
    /// 在Inspector中显示集成状态
    /// </summary>
    [ContextMenu("显示集成统计")]
    void ShowIntegrationStats()
    {
        Debug.Log(GetIntegrationStats());
    }
    
    /// <summary>
    /// 手动集成所有样本
    /// </summary>
    [ContextMenu("集成所有样本")]
    void ManualIntegrateAll()
    {
        IntegrateAllSamplesInScene();
    }
    
    /// <summary>
    /// 立即检查新样本
    /// </summary>
    [ContextMenu("检查新样本")]
    void ManualCheckSamples()
    {
        CheckForNewSamples();
    }
}

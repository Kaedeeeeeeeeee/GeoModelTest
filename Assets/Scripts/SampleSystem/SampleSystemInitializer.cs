using UnityEngine;
using System.Collections;

/// <summary>
/// 样本系统初始化器 - 确保所有样本和背包系统组件正确初始化
/// </summary>
public class SampleSystemInitializer : MonoBehaviour
{
    [Header("系统组件")]
    public bool autoCreateMissingComponents = true;
    public bool validateOnStart = true;
    public bool enableSystemDebugging = false;
    
    [Header("初始化延迟")]
    public float initializationDelay = 0.5f;
    public float validationDelay = 1f;
    
    [Header("组件预制件")]
    public GameObject sampleInventoryPrefab;
    public GameObject inventoryUIPrefab;
    public GameObject samplePlacerPrefab;
    public GameObject sampleTrackerPrefab;
    public GameObject drillIntegratorPrefab;
    public GameObject previewGeneratorPrefab;
    
    // 系统状态
    private bool isInitialized = false;
    private int validationAttempts = 0;
    private const int maxValidationAttempts = 3;
    
    void Start()
    {
        if (validateOnStart)
        {
            StartCoroutine(InitializeSystemWithDelay());
        }
    }
    
    /// <summary>
    /// 延迟初始化系统
    /// </summary>
    IEnumerator InitializeSystemWithDelay()
    {
        yield return new WaitForSeconds(initializationDelay);
        
        LogMessage("开始初始化样本系统...");
        
        bool success = InitializeSampleSystem();
        
        if (success)
        {
            yield return new WaitForSeconds(validationDelay);
            StartCoroutine(ValidateSystemWithRetry());
        }
        else
        {
            LogError("样本系统初始化失败！");
        }
    }
    
    /// <summary>
    /// 初始化样本系统
    /// </summary>
    public bool InitializeSampleSystem()
    {
        if (isInitialized)
        {
            LogMessage("样本系统已经初始化");
            return true;
        }
        
        try
        {
            // 1. 初始化核心背包系统
            if (!EnsureSampleInventory())
            {
                LogError("SampleInventory 初始化失败");
                return false;
            }
            
            // 2. 初始化背包UI
            if (!EnsureInventoryUI())
            {
                LogError("InventoryUI 初始化失败");
                return false;
            }
            
            // 3. 初始化样本放置系统
            if (!EnsureSamplePlacer())
            {
                LogError("SamplePlacer 初始化失败");
                return false;
            }
            
            // 4. 初始化样本跟踪器
            if (!EnsurePlacedSampleTracker())
            {
                LogError("PlacedSampleTracker 初始化失败");
                return false;
            }
            
            // 5. 初始化钻探工具集成器
            if (!EnsureDrillToolIntegrator())
            {
                LogError("DrillToolSampleIntegrator 初始化失败");
                return false;
            }
            
            // 6. 初始化预览生成器（可选）
            EnsurePreviewGenerator();
            
            isInitialized = true;
            LogMessage("样本系统初始化完成！");
            return true;
        }
        catch (System.Exception e)
        {
            LogError($"系统初始化过程中发生异常: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 确保样本背包存在
    /// </summary>
    bool EnsureSampleInventory()
    {
        var inventory = FindFirstObjectByType<SampleInventory>();
        if (inventory != null)
        {
            LogMessage("找到现有的 SampleInventory");
            return true;
        }
        
        if (!autoCreateMissingComponents)
        {
            LogWarning("未找到 SampleInventory 且自动创建已禁用");
            return false;
        }
        
        // 创建背包组件
        GameObject inventoryObj;
        if (sampleInventoryPrefab != null)
        {
            inventoryObj = Instantiate(sampleInventoryPrefab);
        }
        else
        {
            inventoryObj = new GameObject("SampleInventory");
            inventoryObj.AddComponent<SampleInventory>();
        }
        
        LogMessage("已创建 SampleInventory");
        return true;
    }
    
    /// <summary>
    /// 确保背包UI存在
    /// </summary>
    bool EnsureInventoryUI()
    {
        var ui = FindFirstObjectByType<InventoryUI>();
        if (ui != null)
        {
            LogMessage("找到现有的 InventoryUI");
            return true;
        }
        
        if (!autoCreateMissingComponents)
        {
            LogWarning("未找到 InventoryUI 且自动创建已禁用");
            return false;
        }
        
        // 创建UI组件
        GameObject uiObj;
        if (inventoryUIPrefab != null)
        {
            uiObj = Instantiate(inventoryUIPrefab);
        }
        else
        {
            uiObj = new GameObject("InventoryUI");
            uiObj.AddComponent<InventoryUI>();
        }
        
        LogMessage("已创建 InventoryUI");
        return true;
    }
    
    /// <summary>
    /// 确保样本放置器存在
    /// </summary>
    bool EnsureSamplePlacer()
    {
        var placer = FindFirstObjectByType<SamplePlacer>();
        if (placer != null)
        {
            LogMessage("找到现有的 SamplePlacer");
            return true;
        }
        
        if (!autoCreateMissingComponents)
        {
            LogWarning("未找到 SamplePlacer 且自动创建已禁用");
            return false;
        }
        
        // 创建放置器组件
        GameObject placerObj;
        if (samplePlacerPrefab != null)
        {
            placerObj = Instantiate(samplePlacerPrefab);
        }
        else
        {
            placerObj = new GameObject("SamplePlacer");
            placerObj.AddComponent<SamplePlacer>();
        }
        
        LogMessage("已创建 SamplePlacer");
        return true;
    }
    
    /// <summary>
    /// 确保样本跟踪器存在
    /// </summary>
    bool EnsurePlacedSampleTracker()
    {
        var tracker = FindFirstObjectByType<PlacedSampleTracker>();
        if (tracker != null)
        {
            LogMessage("找到现有的 PlacedSampleTracker");
            return true;
        }
        
        if (!autoCreateMissingComponents)
        {
            LogWarning("未找到 PlacedSampleTracker 且自动创建已禁用");
            return false;
        }
        
        // 创建跟踪器组件
        GameObject trackerObj;
        if (sampleTrackerPrefab != null)
        {
            trackerObj = Instantiate(sampleTrackerPrefab);
        }
        else
        {
            trackerObj = new GameObject("PlacedSampleTracker");
            trackerObj.AddComponent<PlacedSampleTracker>();
        }
        
        LogMessage("已创建 PlacedSampleTracker");
        return true;
    }
    
    /// <summary>
    /// 确保钻探工具集成器存在
    /// </summary>
    bool EnsureDrillToolIntegrator()
    {
        var integrator = FindFirstObjectByType<DrillToolSampleIntegrator>();
        if (integrator != null)
        {
            LogMessage("找到现有的 DrillToolSampleIntegrator");
            return true;
        }
        
        if (!autoCreateMissingComponents)
        {
            LogWarning("未找到 DrillToolSampleIntegrator 且自动创建已禁用");
            return false;
        }
        
        // 创建集成器组件
        GameObject integratorObj;
        if (drillIntegratorPrefab != null)
        {
            integratorObj = Instantiate(drillIntegratorPrefab);
        }
        else
        {
            integratorObj = new GameObject("DrillToolSampleIntegrator");
            integratorObj.AddComponent<DrillToolSampleIntegrator>();
        }
        
        LogMessage("已创建 DrillToolSampleIntegrator");
        return true;
    }
    
    /// <summary>
    /// 确保预览生成器存在（可选）
    /// </summary>
    bool EnsurePreviewGenerator()
    {
        var generator = FindFirstObjectByType<SamplePreviewGenerator>();
        if (generator != null)
        {
            LogMessage("找到现有的 SamplePreviewGenerator");
            return true;
        }
        
        if (!autoCreateMissingComponents)
        {
            LogMessage("SamplePreviewGenerator 为可选组件，跳过创建");
            return true; // 预览生成器是可选的
        }
        
        // 创建预览生成器组件
        GameObject generatorObj;
        if (previewGeneratorPrefab != null)
        {
            generatorObj = Instantiate(previewGeneratorPrefab);
        }
        else
        {
            generatorObj = new GameObject("SamplePreviewGenerator");
            generatorObj.AddComponent<SamplePreviewGenerator>();
        }
        
        LogMessage("已创建 SamplePreviewGenerator");
        return true;
    }
    
    /// <summary>
    /// 重试验证系统
    /// </summary>
    IEnumerator ValidateSystemWithRetry()
    {
        while (validationAttempts < maxValidationAttempts)
        {
            validationAttempts++;
            LogMessage($"开始系统验证 (第 {validationAttempts} 次)...");
            
            bool isValid = ValidateSystem();
            if (isValid)
            {
                LogMessage("系统验证通过！");
                yield break;
            }
            
            LogWarning($"系统验证失败，等待重试... ({validationAttempts}/{maxValidationAttempts})");
            yield return new WaitForSeconds(2f);
        }
        
        LogError("系统验证多次失败，请检查配置！");
    }
    
    /// <summary>
    /// 验证系统完整性
    /// </summary>
    public bool ValidateSystem()
    {
        bool isValid = true;
        
        // 检查核心组件
        if (FindFirstObjectByType<SampleInventory>() == null)
        {
            LogError("验证失败: SampleInventory 不存在");
            isValid = false;
        }
        
        if (FindFirstObjectByType<InventoryUI>() == null)
        {
            LogError("验证失败: InventoryUI 不存在");
            isValid = false;
        }
        
        if (FindFirstObjectByType<SamplePlacer>() == null)
        {
            LogError("验证失败: SamplePlacer 不存在");
            isValid = false;
        }
        
        if (FindFirstObjectByType<PlacedSampleTracker>() == null)
        {
            LogError("验证失败: PlacedSampleTracker 不存在");
            isValid = false;
        }
        
        if (FindFirstObjectByType<DrillToolSampleIntegrator>() == null)
        {
            LogError("验证失败: DrillToolSampleIntegrator 不存在");
            isValid = false;
        }
        
        // 检查单例实例
        if (SampleInventory.Instance == null)
        {
            LogError("验证失败: SampleInventory.Instance 为空");
            isValid = false;
        }
        
        if (SamplePlacer.Instance == null)
        {
            LogError("验证失败: SamplePlacer.Instance 为空");
            isValid = false;
        }
        
        if (PlacedSampleTracker.Instance == null)
        {
            LogError("验证失败: PlacedSampleTracker.Instance 为空");
            isValid = false;
        }
        
        return isValid;
    }
    
    /// <summary>
    /// 获取系统状态报告
    /// </summary>
    public string GetSystemStatusReport()
    {
        string report = "=== 样本系统状态报告 ===\n";
        report += $"系统已初始化: {isInitialized}\n";
        report += $"验证尝试次数: {validationAttempts}\n\n";
        
        // 组件状态
        report += "核心组件状态:\n";
        report += $"- SampleInventory: {(FindFirstObjectByType<SampleInventory>() != null ? "✓" : "✗")}\n";
        report += $"- InventoryUI: {(FindFirstObjectByType<InventoryUI>() != null ? "✓" : "✗")}\n";
        report += $"- SamplePlacer: {(FindFirstObjectByType<SamplePlacer>() != null ? "✓" : "✗")}\n";
        report += $"- PlacedSampleTracker: {(FindFirstObjectByType<PlacedSampleTracker>() != null ? "✓" : "✗")}\n";
        report += $"- DrillToolSampleIntegrator: {(FindFirstObjectByType<DrillToolSampleIntegrator>() != null ? "✓" : "✗")}\n";
        report += $"- SamplePreviewGenerator: {(FindFirstObjectByType<SamplePreviewGenerator>() != null ? "✓" : "✗")}\n\n";
        
        // 单例状态
        report += "单例实例状态:\n";
        report += $"- SampleInventory.Instance: {(SampleInventory.Instance != null ? "✓" : "✗")}\n";
        report += $"- SamplePlacer.Instance: {(SamplePlacer.Instance != null ? "✓" : "✗")}\n";
        report += $"- PlacedSampleTracker.Instance: {(PlacedSampleTracker.Instance != null ? "✓" : "✗")}\n";
        report += $"- DrillToolSampleIntegrator.Instance: {(DrillToolSampleIntegrator.Instance != null ? "✓" : "✗")}\n";
        report += $"- SamplePreviewGenerator.Instance: {(SamplePreviewGenerator.Instance != null ? "✓" : "✗")}\n\n";
        
        // 背包状态
        if (SampleInventory.Instance != null)
        {
            var (current, max) = SampleInventory.Instance.GetCapacityInfo();
            report += $"背包状态: {current}/{max}\n";
        }
        
        // 已放置样本状态
        int placedCount = PlacedSampleTracker.GetPlacedSampleCount();
        report += $"已放置样本数量: {placedCount}\n";
        
        return report;
    }
    
    /// <summary>
    /// 重置系统
    /// </summary>
    public void ResetSystem()
    {
        LogMessage("开始重置样本系统...");
        
        isInitialized = false;
        validationAttempts = 0;
        
        // 清理背包
        if (SampleInventory.Instance != null)
        {
            SampleInventory.Instance.ClearInventory();
        }
        
        // 清理已放置的样本
        PlacedSampleTracker.ClearAllPlacedSamples();
        
        // 强制清理孤立样本
        PlacedSampleTracker.ForceCleanup();
        
        LogMessage("系统重置完成");
    }
    
    /// <summary>
    /// 日志输出
    /// </summary>
    void LogMessage(string message)
    {
        if (enableSystemDebugging)
        {
            Debug.Log($"[SampleSystemInitializer] {message}");
        }
    }
    
    /// <summary>
    /// 警告输出
    /// </summary>
    void LogWarning(string message)
    {
        Debug.LogWarning($"[SampleSystemInitializer] {message}");
    }
    
    /// <summary>
    /// 错误输出
    /// </summary>
    void LogError(string message)
    {
        Debug.LogError($"[SampleSystemInitializer] {message}");
    }
    
    /// <summary>
    /// 在Inspector中显示系统状态
    /// </summary>
    [ContextMenu("显示系统状态")]
    void ShowSystemStatus()
    {
        Debug.Log(GetSystemStatusReport());
    }
    
    /// <summary>
    /// 手动初始化系统
    /// </summary>
    [ContextMenu("手动初始化系统")]
    void ManualInitializeSystem()
    {
        InitializeSampleSystem();
    }
    
    /// <summary>
    /// 手动验证系统
    /// </summary>
    [ContextMenu("验证系统")]
    void ManualValidateSystem()
    {
        bool isValid = ValidateSystem();
        Debug.Log($"系统验证结果: {(isValid ? "通过" : "失败")}");
    }
    
    /// <summary>
    /// 手动重置系统
    /// </summary>
    [ContextMenu("重置系统")]
    void ManualResetSystem()
    {
        ResetSystem();
    }
}
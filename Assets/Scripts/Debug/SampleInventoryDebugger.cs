using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// SampleInventory诊断工具 - 检查样本背包系统的状态
/// </summary>
public class SampleInventoryDebugger : MonoBehaviour
{
    [Header("运行时诊断")]
    public bool runDiagnosticsOnStart = true;
    public bool enableDetailedLogging = true;
    
    [Header("自动修复")]
    public bool autoFixMissingComponents = true;
    
    private void Start()
    {
        if (runDiagnosticsOnStart)
        {
            DiagnoseSampleInventorySystem();
        }
    }
    
    /// <summary>
    /// 诊断样本背包系统
    /// </summary>
    [ContextMenu("诊断样本背包系统")]
    public void DiagnoseSampleInventorySystem()
    {
        Log("=== 样本背包系统诊断开始 ===");
        
        // 1. 检查GameInitializer
        CheckGameInitializer();
        
        // 2. 检查SampleInventory单例
        CheckSampleInventory();
        
        // 3. 检查相关组件
        CheckRelatedComponents();
        
        // 4. 检查场景中的样本收集器
        CheckSampleCollectors();
        
        // 5. 自动修复（如果启用）
        if (autoFixMissingComponents)
        {
            AttemptAutoFix();
        }
        
        Log("=== 样本背包系统诊断完成 ===");
    }
    
    /// <summary>
    /// 检查GameInitializer
    /// </summary>
    void CheckGameInitializer()
    {
        Log("\n[1] 检查GameInitializer状态...");
        
        GameInitializer gameInitializer = FindFirstObjectByType<GameInitializer>();
        if (gameInitializer == null)
        {
            LogError("❌ 未找到GameInitializer！这是主要问题所在。");
            LogError("   解决方案：需要在场景中创建一个GameObject并添加GameInitializer组件");
            return;
        }
        
        Log($"✅ 找到GameInitializer: {gameInitializer.gameObject.name}");
        Log($"   - 启用样本系统: {gameInitializer.initializeSampleSystem}");
        Log($"   - GameObject激活: {gameInitializer.gameObject.activeInHierarchy}");
        Log($"   - 组件启用: {gameInitializer.enabled}");
        
        if (!gameInitializer.initializeSampleSystem)
        {
            LogWarning("⚠️ GameInitializer.initializeSampleSystem 设置为false");
            LogWarning("   这会导致样本背包系统不被初始化");
        }
    }
    
    /// <summary>
    /// 检查SampleInventory单例
    /// </summary>
    void CheckSampleInventory()
    {
        Log("\n[2] 检查SampleInventory单例状态...");
        
        // 检查单例实例
        bool hasInstance = SampleInventory.Instance != null;
        Log($"SampleInventory.Instance != null: {hasInstance}");
        
        if (hasInstance)
        {
            var instance = SampleInventory.Instance;
            Log($"✅ SampleInventory实例存在: {instance.gameObject.name}");
            Log($"   - GameObject激活: {instance.gameObject.activeInHierarchy}");
            Log($"   - 组件启用: {instance.enabled}");
            Log($"   - 最大容量: {instance.maxSampleCapacity}");
            
            var (current, max) = instance.GetCapacityInfo();
            Log($"   - 当前容量: {current}/{max}");
        }
        else
        {
            LogError("❌ SampleInventory.Instance 为 null");
            
            // 检查是否有SampleInventory组件但没有设置Instance
            SampleInventory[] inventories = FindObjectsByType<SampleInventory>(FindObjectsSortMode.None);
            if (inventories.Length > 0)
            {
                LogWarning($"⚠️ 找到{inventories.Length}个SampleInventory组件，但Instance未设置");
                for (int i = 0; i < inventories.Length; i++)
                {
                    var inv = inventories[i];
                    Log($"   [{i}] {inv.gameObject.name} - 激活:{inv.gameObject.activeInHierarchy} - 启用:{inv.enabled}");
                }
            }
            else
            {
                LogError("❌ 场景中完全没有SampleInventory组件");
            }
        }
        
        // 使用FindFirstObjectByType检查（这是SampleCollector使用的方法）
        var foundInventory = FindFirstObjectByType<SampleInventory>();
        Log($"FindFirstObjectByType<SampleInventory>(): {(foundInventory != null ? "找到" : "未找到")}");
    }
    
    /// <summary>
    /// 检查相关组件
    /// </summary>
    void CheckRelatedComponents()
    {
        Log("\n[3] 检查相关组件...");
        
        // 检查其他样本系统组件
        var systemInitializer = FindFirstObjectByType<SampleSystemInitializer>();
        Log($"SampleSystemInitializer: {(systemInitializer != null ? "存在" : "缺失")}");
        
        var inventoryUI = FindFirstObjectByType<InventoryUI>();
        Log($"InventoryUI: {(inventoryUI != null ? "存在" : "缺失")}");
        
        var samplePlacer = FindFirstObjectByType<SamplePlacer>();
        Log($"SamplePlacer: {(samplePlacer != null ? "存在" : "缺失")}");
        
        var placedSampleTracker = FindFirstObjectByType<PlacedSampleTracker>();
        Log($"PlacedSampleTracker: {(placedSampleTracker != null ? "存在" : "缺失")}");
    }
    
    /// <summary>
    /// 检查场景中的样本收集器
    /// </summary>
    void CheckSampleCollectors()
    {
        Log("\n[4] 检查样本收集器...");
        
        SampleCollector[] collectors = FindObjectsByType<SampleCollector>(FindObjectsSortMode.None);
        Log($"场景中的SampleCollector数量: {collectors.Length}");
        
        for (int i = 0; i < collectors.Length; i++)
        {
            var collector = collectors[i];
            Log($"   [{i}] {collector.gameObject.name}");
            Log($"       - 激活: {collector.gameObject.activeInHierarchy}");
            Log($"       - 启用: {collector.enabled}");
            Log($"       - 样本数据: {(collector.sampleData != null ? collector.sampleData.displayName : "null")}");
        }
    }
    
    /// <summary>
    /// 尝试自动修复
    /// </summary>
    void AttemptAutoFix()
    {
        Log("\n[5] 尝试自动修复...");
        
        // 如果没有GameInitializer，创建一个
        GameInitializer gameInitializer = FindFirstObjectByType<GameInitializer>();
        if (gameInitializer == null)
        {
            Log("创建GameInitializer...");
            GameObject initializerObj = new GameObject("GameInitializer (Auto-Created)");
            gameInitializer = initializerObj.AddComponent<GameInitializer>();
            gameInitializer.initializeSampleSystem = true;
            Log("✅ 已创建GameInitializer");
        }
        
        // 如果没有SampleInventory，手动创建
        if (SampleInventory.Instance == null)
        {
            Log("创建SampleInventory...");
            GameObject inventoryObj = new GameObject("SampleInventory (Auto-Created)");
            inventoryObj.AddComponent<SampleInventory>();
            Log("✅ 已创建SampleInventory");
        }
        
        Log("自动修复完成！");
    }
    
    /// <summary>
    /// 强制重新初始化样本系统
    /// </summary>
    [ContextMenu("强制重新初始化样本系统")]
    public void ForceReinitializeSampleSystem()
    {
        Log("强制重新初始化样本系统...");
        
        // 找到或创建GameInitializer
        GameInitializer gameInitializer = FindFirstObjectByType<GameInitializer>();
        if (gameInitializer == null)
        {
            GameObject initializerObj = new GameObject("GameInitializer (Force-Created)");
            gameInitializer = initializerObj.AddComponent<GameInitializer>();
        }
        
        // 启用样本系统初始化
        gameInitializer.initializeSampleSystem = true;
        
        // 清理现有的SampleInventory
        if (SampleInventory.Instance != null)
        {
            Log("销毁现有的SampleInventory实例...");
            DestroyImmediate(SampleInventory.Instance.gameObject);
        }
        
        // 手动调用初始化方法
        var initMethod = typeof(GameInitializer).GetMethod("InitializeSampleSystem", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (initMethod != null)
        {
            Log("调用InitializeSampleSystem方法...");
            initMethod.Invoke(gameInitializer, null);
        }
        
        // 验证结果
        DiagnoseSampleInventorySystem();
    }
    
    void Log(string message)
    {
        if (enableDetailedLogging)
        {
            Debug.Log($"[SampleInventoryDebugger] {message}");
        }
    }
    
    void LogWarning(string message)
    {
        Debug.LogWarning($"[SampleInventoryDebugger] {message}");
    }
    
    void LogError(string message)
    {
        Debug.LogError($"[SampleInventoryDebugger] {message}");
    }
}
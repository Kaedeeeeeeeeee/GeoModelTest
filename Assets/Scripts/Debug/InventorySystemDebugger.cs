using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// 背包系统综合诊断工具
/// 检查所有相关的背包系统组件和它们之间的关系
/// </summary>
public class InventorySystemDebugger : MonoBehaviour
{
    [Header("诊断设置")]
    public bool enableDetailedLogging = true;
    public bool autoRunOnStart = false;
    
    [Header("检查目标")]
    public bool checkSampleInventorySystem = true;
    public bool checkPlayerInventorySystem = true;
    public bool checkInventoryUISystem = true;
    public bool checkWarehouseSystem = true;
    public bool checkEventSystems = true;
    
    void Start()
    {
        if (autoRunOnStart)
        {
            Invoke(nameof(RunComprehensiveDiagnosis), 2f); // 延迟2秒等待所有系统初始化
        }
    }
    
    void Update()
    {
        // 按下 F11 键进行全面诊断
        if (Keyboard.current.f11Key.wasPressedThisFrame)
        {
            RunComprehensiveDiagnosis();
        }
        
        // 按下 F12 键进行I键测试
        if (Keyboard.current.f12Key.wasPressedThisFrame)
        {
            TestIKeyBehavior();
        }
    }
    
    /// <summary>
    /// 运行全面诊断
    /// </summary>
    [ContextMenu("运行全面诊断")]
    public void RunComprehensiveDiagnosis()
    {
        Log("=== 背包系统全面诊断开始 ===");
        
        // 1. 检查SampleInventory系统
        if (checkSampleInventorySystem)
        {
            CheckSampleInventorySystem();
        }
        
        // 2. 检查PlayerInventory系统
        if (checkPlayerInventorySystem)
        {
            CheckPlayerInventorySystem();
        }
        
        // 3. 检查InventoryUI系统
        if (checkInventoryUISystem)
        {
            CheckInventoryUISystem();
        }
        
        // 4. 检查仓库系统
        if (checkWarehouseSystem)
        {
            CheckWarehouseSystem();
        }
        
        // 5. 检查事件系统
        if (checkEventSystems)
        {
            CheckEventSystems();
        }
        
        // 6. 分析系统冲突
        AnalyzeSystemConflicts();
        
        Log("=== 背包系统全面诊断完成 ===");
    }
    
    /// <summary>
    /// 检查SampleInventory系统
    /// </summary>
    void CheckSampleInventorySystem()
    {
        Log("\n[1] 检查SampleInventory系统...");
        
        // 检查单例实例
        var instance = SampleInventory.Instance;
        if (instance == null)
        {
            LogError("❌ SampleInventory.Instance 为 null");
        }
        else
        {
            Log($"✅ SampleInventory.Instance 存在: {instance.gameObject.name}");
            
            // 检查配置
            Log($"   - 最大容量: {instance.maxSampleCapacity}");
            Log($"   - 调试模式: {instance.enableDebugLog}");
            
            // 检查样本数据
            var allSamples = instance.GetAllSamples();
            var inventorySamples = instance.GetInventorySamples();
            Log($"   - 总样本数: {allSamples.Count}");
            Log($"   - 背包中样本数: {inventorySamples.Count}");
            
            // 详细列出样本
            if (allSamples.Count > 0)
            {
                Log("   样本详情:");
                for (int i = 0; i < allSamples.Count; i++)
                {
                    var sample = allSamples[i];
                    Log($"     [{i}] {sample.displayName} - 位置: {sample.currentLocation} - ID: {sample.sampleID}");
                }
            }
        }
        
        // 检查场景中是否有多个SampleInventory
        var allInventories = FindObjectsByType<SampleInventory>(FindObjectsSortMode.None);
        Log($"   - 场景中SampleInventory数量: {allInventories.Length}");
        
        if (allInventories.Length > 1)
        {
            LogWarning("⚠️ 检测到多个SampleInventory组件！这可能导致冲突");
            for (int i = 0; i < allInventories.Length; i++)
            {
                Log($"     [{i}] {allInventories[i].gameObject.name}");
            }
        }
    }
    
    /// <summary>
    /// 检查PlayerInventory系统（旧系统）
    /// </summary>
    void CheckPlayerInventorySystem()
    {
        Log("\n[2] 检查PlayerInventory系统（旧系统）...");
        
        var playerInventory = FindFirstObjectByType<PlayerInventory>();
        if (playerInventory == null)
        {
            Log("ℹ️  未找到PlayerInventory组件（这是正常的，如果使用新系统）");
        }
        else
        {
            LogWarning("⚠️ 发现PlayerInventory组件！这可能与新系统冲突");
            Log($"   - 对象: {playerInventory.gameObject.name}");
            Log($"   - 最大样本数: {playerInventory.maxSamples}");
            Log($"   - I键设置: {playerInventory.inventoryKey}");
            Log($"   - 当前样本数: {playerInventory.GetSampleCount()}");
            
            // 获取样本数据
            var samples = playerInventory.GetAllSamples();
            if (samples.Length > 0)
            {
                Log("   旧系统样本:");
                for (int i = 0; i < samples.Length; i++)
                {
                    Log($"     [{i}] ID: {samples[i].sampleID} - 深度: {samples[i].drillingDepth}m");
                }
            }
        }
    }
    
    /// <summary>
    /// 检查InventoryUI系统
    /// </summary>
    void CheckInventoryUISystem()
    {
        Log("\n[3] 检查InventoryUI系统...");
        
        var inventoryUI = FindFirstObjectByType<InventoryUI>();
        if (inventoryUI == null)
        {
            LogError("❌ 未找到InventoryUI组件！");
        }
        else
        {
            Log($"✅ InventoryUI组件存在: {inventoryUI.gameObject.name}");
            Log($"   - I键设置: {inventoryUI.toggleKey}");
            
            // 使用反射检查私有字段
            CheckInventoryUIInternals(inventoryUI);
        }
    }
    
    /// <summary>
    /// 检查InventoryUI内部状态
    /// </summary>
    void CheckInventoryUIInternals(InventoryUI inventoryUI)
    {
        var type = typeof(InventoryUI);
        
        // 检查sampleInventory引用
        var inventoryField = type.GetField("sampleInventory", BindingFlags.NonPublic | BindingFlags.Instance);
        if (inventoryField != null)
        {
            var inventory = inventoryField.GetValue(inventoryUI) as SampleInventory;
            Log($"   - sampleInventory引用: {(inventory != null ? "存在" : "null")}");
            if (inventory != null && inventory != SampleInventory.Instance)
            {
                LogWarning("⚠️ InventoryUI的sampleInventory引用与单例不匹配！");
            }
        }
        
        // 检查UI状态
        var isOpenField = type.GetField("isInventoryOpen", BindingFlags.NonPublic | BindingFlags.Instance);
        if (isOpenField != null)
        {
            bool isOpen = (bool)isOpenField.GetValue(inventoryUI);
            Log($"   - 当前是否打开: {isOpen}");
        }
        
        // 检查UI组件
        var canvasField = type.GetField("inventoryCanvas", BindingFlags.Public | BindingFlags.Instance);
        if (canvasField != null)
        {
            var canvas = canvasField.GetValue(inventoryUI) as Canvas;
            Log($"   - Canvas组件: {(canvas != null ? "存在" : "null")}");
        }
        
        var panelField = type.GetField("inventoryPanel", BindingFlags.Public | BindingFlags.Instance);
        if (panelField != null)
        {
            var panel = panelField.GetValue(inventoryUI) as GameObject;
            Log($"   - InventoryPanel: {(panel != null ? "存在" : "null")}");
            if (panel != null)
            {
                Log($"     - 激活状态: {panel.activeSelf}");
            }
        }
    }
    
    /// <summary>
    /// 检查仓库系统
    /// </summary>
    void CheckWarehouseSystem()
    {
        Log("\n[4] 检查仓库系统...");
        
        var warehousePanel = FindFirstObjectByType<WarehouseInventoryPanel>();
        if (warehousePanel == null)
        {
            Log("ℹ️  未找到WarehouseInventoryPanel组件");
        }
        else
        {
            Log($"✅ WarehouseInventoryPanel存在: {warehousePanel.gameObject.name}");
            
            // 使用反射检查内部状态
            var type = typeof(WarehouseInventoryPanel);
            var inventoryField = type.GetField("inventory", BindingFlags.NonPublic | BindingFlags.Instance);
            if (inventoryField != null)
            {
                var inventory = inventoryField.GetValue(warehousePanel) as SampleInventory;
                Log($"   - inventory引用: {(inventory != null ? "存在" : "null")}");
            }
            
            var itemsField = type.GetField("currentItems", BindingFlags.NonPublic | BindingFlags.Instance);
            if (itemsField != null)
            {
                var items = itemsField.GetValue(warehousePanel) as List<SampleItem>;
                Log($"   - currentItems数量: {items?.Count ?? 0}");
            }
        }
        
        // 检查WarehouseManager
        var warehouseManager = WarehouseManager.Instance;
        if (warehouseManager == null)
        {
            Log("ℹ️  WarehouseManager.Instance 为 null");
        }
        else
        {
            Log($"✅ WarehouseManager.Instance存在");
            if (warehouseManager.Storage != null)
            {
                Log($"   - Storage样本数量: {warehouseManager.Storage.GetAllItems().Count}");
            }
        }
    }
    
    /// <summary>
    /// 检查事件系统
    /// </summary>
    void CheckEventSystems()
    {
        Log("\n[5] 检查事件系统...");
        
        var eventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem == null)
        {
            LogError("❌ 未找到EventSystem！UI交互可能无法正常工作");
        }
        else
        {
            Log($"✅ EventSystem存在: {eventSystem.gameObject.name}");
        }
        
        // 检查输入系统
        var inputModule = FindFirstObjectByType<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        if (inputModule == null)
        {
            LogWarning("⚠️ 未找到InputSystemUIInputModule，可能使用旧的输入系统");
        }
        else
        {
            Log($"✅ InputSystemUIInputModule存在: {inputModule.gameObject.name}");
        }
    }
    
    /// <summary>
    /// 分析系统冲突
    /// </summary>
    void AnalyzeSystemConflicts()
    {
        Log("\n[6] 分析系统冲突...");
        
        var playerInventory = FindFirstObjectByType<PlayerInventory>();
        var inventoryUI = FindFirstObjectByType<InventoryUI>();
        
        if (playerInventory != null && inventoryUI != null)
        {
            LogWarning("⚠️ 检测到I键冲突！");
            Log($"   PlayerInventory使用: {playerInventory.inventoryKey}");
            Log($"   InventoryUI使用: {inventoryUI.toggleKey}");
            
            if (playerInventory.inventoryKey == inventoryUI.toggleKey)
            {
                LogError("❌ 两个系统都监听相同的I键！这会导致冲突");
                Log("建议解决方案：");
                Log("1. 禁用或删除PlayerInventory组件");
                Log("2. 或者修改其中一个系统的快捷键");
            }
        }
        
        // 检查是否存在多个背包系统
        var sampleInventories = FindObjectsByType<SampleInventory>(FindObjectsSortMode.None);
        if (sampleInventories.Length > 1)
        {
            LogError($"❌ 发现{sampleInventories.Length}个SampleInventory实例！");
            Log("这会导致单例模式失效和数据不同步");
        }
    }
    
    /// <summary>
    /// 测试I键行为
    /// </summary>
    [ContextMenu("测试I键行为")]
    public void TestIKeyBehavior()
    {
        Log("\n=== I键行为测试 ===");
        
        // 模拟按下I键
        Log("模拟按下I键...");
        
        var playerInventory = FindFirstObjectByType<PlayerInventory>();
        var inventoryUI = FindFirstObjectByType<InventoryUI>();
        
        if (playerInventory != null)
        {
            Log("PlayerInventory会响应I键");
            // 手动调用
            var toggleMethod = typeof(PlayerInventory).GetMethod("ToggleInventory", BindingFlags.NonPublic | BindingFlags.Instance);
            if (toggleMethod != null)
            {
                toggleMethod.Invoke(playerInventory, null);
                Log("已手动触发PlayerInventory.ToggleInventory()");
            }
        }
        
        if (inventoryUI != null)
        {
            Log("InventoryUI会响应I键");
            // 手动调用
            inventoryUI.ToggleInventory();
            Log("已手动触发InventoryUI.ToggleInventory()");
        }
        
        if (playerInventory == null && inventoryUI == null)
        {
            LogError("❌ 没有找到任何响应I键的组件！");
        }
    }
    
    /// <summary>
    /// 修复系统冲突
    /// </summary>
    [ContextMenu("修复系统冲突")]
    public void FixSystemConflicts()
    {
        Log("开始修复系统冲突...");
        
        var playerInventory = FindFirstObjectByType<PlayerInventory>();
        if (playerInventory != null)
        {
            Log("禁用旧的PlayerInventory组件...");
            playerInventory.enabled = false;
            Log("✅ 已禁用PlayerInventory组件");
        }
        
        // 确保新系统正常工作
        var inventoryUI = FindFirstObjectByType<InventoryUI>();
        if (inventoryUI != null)
        {
            Log("确保InventoryUI组件启用...");
            inventoryUI.enabled = true;
            
            // 强制重新初始化
            var initMethod = typeof(InventoryUI).GetMethod("InitializeComponents", BindingFlags.NonPublic | BindingFlags.Instance);
            if (initMethod != null)
            {
                initMethod.Invoke(inventoryUI, null);
                Log("✅ 已重新初始化InventoryUI");
            }
        }
        
        // 确保SampleInventory正常
        if (SampleInventory.Instance != null)
        {
            Log("✅ SampleInventory单例正常");
        }
        else
        {
            LogError("❌ SampleInventory单例仍然为null，需要手动创建");
        }
        
        Log("修复完成！请重新测试I键功能");
    }
    
    void Log(string message)
    {
        if (enableDetailedLogging)
        {
            Debug.Log($"[InventorySystemDebugger] {message}");
        }
    }
    
    void LogWarning(string message)
    {
        Debug.LogWarning($"[InventorySystemDebugger] {message}");
    }
    
    void LogError(string message)
    {
        Debug.LogError($"[InventorySystemDebugger] {message}");
    }
}
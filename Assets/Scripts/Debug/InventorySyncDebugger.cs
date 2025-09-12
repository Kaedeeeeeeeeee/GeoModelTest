using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 背包同步问题诊断工具
/// </summary>
public class InventorySyncDebugger : MonoBehaviour
{
    [Header("调试设置")]
    public bool enableDetailedLogging = true;
    
    void Update()
    {
        // 按下 F10 键进行诊断
        if (Input.GetKeyDown(KeyCode.F10))
        {
            DiagnoseInventorySync();
        }
    }
    
    /// <summary>
    /// 诊断背包同步问题
    /// </summary>
    [ContextMenu("诊断背包同步问题")]
    public void DiagnoseInventorySync()
    {
        Log("=== 背包同步诊断开始 ===");
        
        // 1. 检查SampleInventory状态
        CheckSampleInventory();
        
        // 2. 检查仓库面板状态  
        CheckWarehousePanel();
        
        // 3. 检查样本状态详情
        CheckSampleDetails();
        
        Log("=== 背包同步诊断完成 ===");
    }
    
    /// <summary>
    /// 检查SampleInventory状态
    /// </summary>
    void CheckSampleInventory()
    {
        Log("\n[1] 检查SampleInventory状态...");
        
        var inventory = SampleInventory.Instance;
        if (inventory == null)
        {
            LogError("❌ SampleInventory.Instance 为 null");
            return;
        }
        
        Log($"✅ SampleInventory实例存在");
        
        // 获取所有样本
        var allSamples = inventory.GetAllSamples();
        var inventorySamples = inventory.GetInventorySamples();
        
        Log($"   - 总样本数量: {allSamples.Count}");
        Log($"   - 背包中样本数量: {inventorySamples.Count}");
        
        // 获取容量信息
        var (current, max) = inventory.GetCapacityInfo();
        Log($"   - 容量信息: {current}/{max}");
        
        // 详细列出每个样本
        Log("\n   所有样本详情:");
        for (int i = 0; i < allSamples.Count; i++)
        {
            var sample = allSamples[i];
            Log($"   [{i}] {sample.displayName}");
            Log($"       - ID: {sample.sampleID}");
            Log($"       - 当前位置: {sample.currentLocation}");
            Log($"       - 是否玩家放置: {sample.isPlayerPlaced}");
            Log($"       - 源工具ID: {sample.sourceToolID}");
        }
    }
    
    /// <summary>
    /// 检查仓库面板状态
    /// </summary>
    void CheckWarehousePanel()
    {
        Log("\n[2] 检查仓库面板状态...");
        
        var warehousePanel = FindFirstObjectByType<WarehouseInventoryPanel>();
        if (warehousePanel == null)
        {
            LogError("❌ 未找到WarehouseInventoryPanel");
            return;
        }
        
        Log($"✅ 找到WarehouseInventoryPanel: {warehousePanel.gameObject.name}");
        
        // 使用反射获取private字段
        var currentItemsField = typeof(WarehouseInventoryPanel).GetField("currentItems", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var inventoryField = typeof(WarehouseInventoryPanel).GetField("inventory", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (currentItemsField != null && inventoryField != null)
        {
            var currentItems = currentItemsField.GetValue(warehousePanel) as List<SampleItem>;
            var inventory = inventoryField.GetValue(warehousePanel) as SampleInventory;
            
            Log($"   - 仓库面板关联的inventory: {(inventory != null ? "存在" : "null")}");
            Log($"   - 仓库面板的currentItems数量: {currentItems?.Count ?? 0}");
            
            if (currentItems != null && currentItems.Count > 0)
            {
                Log("   仓库面板显示的样本:");
                for (int i = 0; i < currentItems.Count; i++)
                {
                    var item = currentItems[i];
                    Log($"   [{i}] {item.displayName} - 位置: {item.currentLocation}");
                }
            }
        }
        else
        {
            LogWarning("⚠️ 无法通过反射获取仓库面板私有字段");
        }
        
        // 手动触发刷新
        Log("   手动触发仓库面板刷新...");
        warehousePanel.RefreshInventoryDisplay();
    }
    
    /// <summary>
    /// 检查样本状态详情
    /// </summary>
    void CheckSampleDetails()
    {
        Log("\n[3] 检查样本状态详情...");
        
        var inventory = SampleInventory.Instance;
        if (inventory == null) return;
        
        var allSamples = inventory.GetAllSamples();
        var inventorySamples = inventory.GetInventorySamples();
        
        Log($"过滤前样本数量: {allSamples.Count}");
        Log($"过滤后样本数量: {inventorySamples.Count}");
        
        if (allSamples.Count != inventorySamples.Count)
        {
            LogWarning("⚠️ 存在样本被过滤了！");
            
            foreach (var sample in allSamples)
            {
                if (sample.currentLocation != SampleLocation.InInventory)
                {
                    LogWarning($"   被过滤的样本: {sample.displayName} - 位置: {sample.currentLocation}");
                }
            }
        }
    }
    
    /// <summary>
    /// 强制修复样本状态
    /// </summary>
    [ContextMenu("强制修复样本状态")]
    public void ForceFixSampleStates()
    {
        Log("开始强制修复样本状态...");
        
        var inventory = SampleInventory.Instance;
        if (inventory == null) 
        {
            LogError("SampleInventory不存在，无法修复");
            return;
        }
        
        var allSamples = inventory.GetAllSamples();
        int fixedCount = 0;
        
        foreach (var sample in allSamples)
        {
            if (sample.currentLocation != SampleLocation.InInventory)
            {
                Log($"修复样本状态: {sample.displayName} ({sample.currentLocation} -> InInventory)");
                sample.currentLocation = SampleLocation.InInventory;
                fixedCount++;
            }
        }
        
        Log($"修复完成，共修复 {fixedCount} 个样本");
        
        // 强制刷新仓库面板
        var warehousePanel = FindFirstObjectByType<WarehouseInventoryPanel>();
        if (warehousePanel != null)
        {
            warehousePanel.RefreshInventoryDisplay();
            Log("已刷新仓库面板显示");
        }
    }
    
    void Log(string message)
    {
        if (enableDetailedLogging)
        {
            Debug.Log($"[InventorySyncDebugger] {message}");
        }
    }
    
    void LogWarning(string message)
    {
        Debug.LogWarning($"[InventorySyncDebugger] {message}");
    }
    
    void LogError(string message)
    {
        Debug.LogError($"[InventorySyncDebugger] {message}");
    }
}
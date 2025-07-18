using UnityEngine;
using System;

/// <summary>
/// 样本时间序列化测试助手
/// </summary>
public class SampleTimeSerializationTest : MonoBehaviour
{
    /// <summary>
    /// 测试时间序列化功能
    /// </summary>
    [ContextMenu("测试时间序列化")]
    public void TestTimeSerialization()
    {
        Debug.Log("=== 样本时间序列化测试 ===");
        
        // 创建一个测试样本
        SampleItem testSample = new SampleItem();
        testSample.sampleID = "TEST001";
        testSample.displayName = "测试样本";
        testSample.description = "用于测试时间序列化";
        
        // 设置一个特定的时间
        DateTime testTime = new DateTime(2024, 12, 25, 14, 30, 45, 123);
        testSample.collectionTime = testTime;
        
        Debug.Log($"原始时间: {testTime:yyyy-MM-dd HH:mm:ss.fff}");
        Debug.Log($"设置后的时间: {testSample.collectionTime:yyyy-MM-dd HH:mm:ss.fff}");
        
        // 序列化测试
        string json = JsonUtility.ToJson(testSample, true);
        Debug.Log($"序列化后的JSON:\n{json}");
        
        // 反序列化测试
        SampleItem deserializedSample = JsonUtility.FromJson<SampleItem>(json);
        Debug.Log($"反序列化后的时间: {deserializedSample.collectionTime:yyyy-MM-dd HH:mm:ss.fff}");
        
        // 比较时间
        if (Math.Abs((testTime - deserializedSample.collectionTime).TotalMilliseconds) < 1000)
        {
            Debug.Log("✅ 时间序列化测试成功！");
        }
        else
        {
            Debug.LogError("❌ 时间序列化测试失败！");
            Debug.LogError($"时间差: {(testTime - deserializedSample.collectionTime).TotalMilliseconds} 毫秒");
        }
    }
    
    /// <summary>
    /// 测试现有仓库数据的时间恢复
    /// </summary>
    [ContextMenu("测试仓库数据时间恢复")]
    public void TestWarehouseDataTimeRecovery()
    {
        Debug.Log("=== 仓库数据时间恢复测试 ===");
        
        WarehouseManager warehouseManager = WarehouseManager.Instance;
        if (warehouseManager == null)
        {
            Debug.LogError("未找到WarehouseManager实例！");
            return;
        }
        
        var warehouseItems = warehouseManager.Storage.GetAllItems();
        if (warehouseItems.Count == 0)
        {
            Debug.LogWarning("仓库中没有物品可供测试");
            return;
        }
        
        Debug.Log($"检查仓库中的 {warehouseItems.Count} 个物品的时间数据：");
        
        int validTimeCount = 0;
        int invalidTimeCount = 0;
        
        foreach (var item in warehouseItems)
        {
            DateTime itemTime = item.collectionTime;
            
            // 检查是否是默认时间（0001-01-01）
            if (itemTime.Year == 1)
            {
                Debug.LogWarning($"物品 {item.displayName} 的时间无效: {itemTime:yyyy-MM-dd HH:mm:ss}");
                invalidTimeCount++;
            }
            else
            {
                Debug.Log($"物品 {item.displayName} 的时间正常: {itemTime:yyyy-MM-dd HH:mm:ss}");
                validTimeCount++;
            }
        }
        
        Debug.Log($"时间检查完成: {validTimeCount} 个有效时间, {invalidTimeCount} 个无效时间");
        
        if (invalidTimeCount > 0)
        {
            Debug.LogWarning($"发现 {invalidTimeCount} 个物品的时间数据丢失，建议重新保存仓库数据");
        }
        else
        {
            Debug.Log("✅ 所有物品的时间数据都正常！");
        }
    }
    
    /// <summary>
    /// 强制重新保存仓库数据以应用时间修复
    /// </summary>
    [ContextMenu("强制重新保存仓库数据")]
    public void ForceResaveWarehouseData()
    {
        Debug.Log("=== 强制重新保存仓库数据 ===");
        
        WarehouseManager warehouseManager = WarehouseManager.Instance;
        if (warehouseManager == null)
        {
            Debug.LogError("未找到WarehouseManager实例！");
            return;
        }
        
        var warehouseItems = warehouseManager.Storage.GetAllItems();
        int fixedCount = 0;
        
        // 为时间无效的物品设置当前时间
        foreach (var item in warehouseItems)
        {
            if (item.collectionTime.Year == 1)
            {
                item.collectionTime = DateTime.Now;
                fixedCount++;
                Debug.Log($"为物品 {item.displayName} 设置了新的采集时间: {item.collectionTime:yyyy-MM-dd HH:mm:ss}");
            }
        }
        
        // 强制保存
        warehouseManager.SaveWarehouseData();
        
        Debug.Log($"✅ 已修复 {fixedCount} 个物品的时间数据并重新保存");
    }
}
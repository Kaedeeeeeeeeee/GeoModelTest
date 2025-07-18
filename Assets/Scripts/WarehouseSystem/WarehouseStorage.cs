using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// 仓库存储系统 - 管理仓库中的物品存储
/// </summary>
[System.Serializable]
public class WarehouseStorage
{
    [Header("仓库设置")]
    public int baseCapacity = 100; // 基础容量
    public int maxCapacityLimit = 500; // 最大容量限制
    public int itemsPerPage = 50; // 每页显示的物品数量
    
    [Header("当前状态")]
    public int currentCapacity; // 当前容量（可通过任务奖励等扩展）
    
    // 存储的物品列表
    private List<SampleItem> storedItems = new List<SampleItem>();
    
    // 事件系统
    public System.Action<SampleItem> OnItemAdded;
    public System.Action<SampleItem> OnItemRemoved;
    public System.Action OnStorageChanged;
    public System.Action<int> OnCapacityChanged; // 容量变化事件
    
    // 构造函数
    public WarehouseStorage()
    {
        currentCapacity = baseCapacity;
        storedItems = new List<SampleItem>();
    }
    
    /// <summary>
    /// 初始化仓库存储
    /// </summary>
    public void Initialize()
    {
        if (currentCapacity <= 0)
        {
            currentCapacity = baseCapacity;
        }
        
        if (storedItems == null)
        {
            storedItems = new List<SampleItem>();
        }
        
        Debug.Log($"仓库存储初始化完成 - 容量: {currentCapacity}/{maxCapacityLimit}");
    }
    
    /// <summary>
    /// 检查是否可以添加物品
    /// </summary>
    public bool CanAddItem(SampleItem item)
    {
        if (item == null)
        {
            Debug.LogWarning("物品为空，无法添加到仓库");
            return false;
        }
        
        // 检查容量
        if (storedItems.Count >= currentCapacity)
        {
            Debug.LogWarning("仓库已满！");
            return false;
        }
        
        // 检查是否已存在相同ID的物品
        if (HasItem(item.sampleID))
        {
            Debug.LogWarning($"仓库中已存在相同ID的样本: {item.sampleID}");
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// 检查是否可以添加多个物品
    /// </summary>
    public bool CanAddItems(List<SampleItem> items)
    {
        if (items == null || items.Count == 0)
        {
            return true;
        }
        
        // 检查总容量
        if (storedItems.Count + items.Count > currentCapacity)
        {
            Debug.LogWarning($"仓库容量不足！需要 {items.Count} 个空位，当前剩余 {currentCapacity - storedItems.Count} 个");
            return false;
        }
        
        // 检查重复ID
        foreach (var item in items)
        {
            if (item != null && HasItem(item.sampleID))
            {
                Debug.LogWarning($"批量添加失败：仓库中已存在样本 {item.sampleID}");
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// 添加单个物品到仓库
    /// </summary>
    public bool AddItem(SampleItem item)
    {
        if (!CanAddItem(item))
        {
            return false;
        }
        
        storedItems.Add(item);
        item.currentLocation = SampleLocation.InWarehouse; // 新增仓库位置状态
        
        OnItemAdded?.Invoke(item);
        OnStorageChanged?.Invoke();
        
        Debug.Log($"物品已添加到仓库: {item.displayName} ({item.sampleID})");
        Debug.Log($"当前仓库: {storedItems.Count}/{currentCapacity}");
        
        return true;
    }
    
    /// <summary>
    /// 批量添加物品到仓库
    /// </summary>
    public bool AddItems(List<SampleItem> items)
    {
        if (!CanAddItems(items))
        {
            return false;
        }
        
        int addedCount = 0;
        foreach (var item in items)
        {
            if (item != null && AddItem(item))
            {
                addedCount++;
            }
        }
        
        Debug.Log($"批量添加完成：成功添加 {addedCount}/{items.Count} 个物品到仓库");
        return addedCount == items.Count;
    }
    
    /// <summary>
    /// 从仓库移除物品
    /// </summary>
    public bool RemoveItem(SampleItem item)
    {
        if (item == null || !storedItems.Contains(item))
        {
            return false;
        }
        
        bool removed = storedItems.Remove(item);
        if (removed)
        {
            OnItemRemoved?.Invoke(item);
            OnStorageChanged?.Invoke();
            
            Debug.Log($"物品已从仓库移除: {item.displayName}");
            Debug.Log($"当前仓库: {storedItems.Count}/{currentCapacity}");
        }
        
        return removed;
    }
    
    /// <summary>
    /// 批量移除物品
    /// </summary>
    public bool RemoveItems(List<SampleItem> items)
    {
        if (items == null || items.Count == 0)
        {
            return true;
        }
        
        int removedCount = 0;
        foreach (var item in items)
        {
            if (item != null && RemoveItem(item))
            {
                removedCount++;
            }
        }
        
        Debug.Log($"批量移除完成：成功移除 {removedCount}/{items.Count} 个物品");
        return removedCount == items.Count;
    }
    
    /// <summary>
    /// 根据ID移除物品
    /// </summary>
    public bool RemoveItemByID(string itemID)
    {
        var item = GetItemByID(itemID);
        return item != null && RemoveItem(item);
    }
    
    /// <summary>
    /// 检查是否包含指定ID的物品
    /// </summary>
    public bool HasItem(string itemID)
    {
        return storedItems.Any(item => item.sampleID == itemID);
    }
    
    /// <summary>
    /// 根据ID获取物品
    /// </summary>
    public SampleItem GetItemByID(string itemID)
    {
        return storedItems.FirstOrDefault(item => item.sampleID == itemID);
    }
    
    /// <summary>
    /// 获取所有存储的物品
    /// </summary>
    public List<SampleItem> GetAllItems()
    {
        return new List<SampleItem>(storedItems);
    }
    
    /// <summary>
    /// 获取指定页的物品
    /// </summary>
    public List<SampleItem> GetItemsForPage(int pageIndex)
    {
        if (pageIndex < 0)
        {
            Debug.LogWarning($"页面索引无效: {pageIndex}");
            return new List<SampleItem>();
        }
        
        int startIndex = pageIndex * itemsPerPage;
        if (startIndex >= storedItems.Count)
        {
            return new List<SampleItem>();
        }
        
        int count = Mathf.Min(itemsPerPage, storedItems.Count - startIndex);
        return storedItems.GetRange(startIndex, count);
    }
    
    /// <summary>
    /// 获取总页数
    /// </summary>
    public int GetTotalPages()
    {
        return Mathf.CeilToInt((float)storedItems.Count / itemsPerPage);
    }
    
    /// <summary>
    /// 获取当前容量信息
    /// </summary>
    public (int current, int max) GetCapacityInfo()
    {
        return (storedItems.Count, currentCapacity);
    }
    
    /// <summary>
    /// 扩展仓库容量
    /// </summary>
    public bool ExpandCapacity(int additionalCapacity)
    {
        if (additionalCapacity <= 0)
        {
            Debug.LogWarning("扩展容量必须大于0");
            return false;
        }
        
        int newCapacity = currentCapacity + additionalCapacity;
        if (newCapacity > maxCapacityLimit)
        {
            Debug.LogWarning($"容量超过限制！当前: {currentCapacity}, 尝试扩展到: {newCapacity}, 最大限制: {maxCapacityLimit}");
            newCapacity = maxCapacityLimit;
        }
        
        int oldCapacity = currentCapacity;
        currentCapacity = newCapacity;
        
        OnCapacityChanged?.Invoke(currentCapacity);
        OnStorageChanged?.Invoke();
        
        Debug.Log($"仓库容量已扩展：{oldCapacity} -> {currentCapacity}");
        return true;
    }
    
    /// <summary>
    /// 设置仓库容量（用于任务奖励等）
    /// </summary>
    public bool SetCapacity(int newCapacity)
    {
        if (newCapacity < storedItems.Count)
        {
            Debug.LogWarning($"无法设置容量为 {newCapacity}，当前已存储 {storedItems.Count} 个物品");
            return false;
        }
        
        if (newCapacity > maxCapacityLimit)
        {
            Debug.LogWarning($"容量超过最大限制 {maxCapacityLimit}，设置为最大值");
            newCapacity = maxCapacityLimit;
        }
        
        int oldCapacity = currentCapacity;
        currentCapacity = newCapacity;
        
        OnCapacityChanged?.Invoke(currentCapacity);
        OnStorageChanged?.Invoke();
        
        Debug.Log($"仓库容量已设置：{oldCapacity} -> {currentCapacity}");
        return true;
    }
    
    /// <summary>
    /// 按采集时间排序
    /// </summary>
    public void SortByCollectionTime(bool ascending = true)
    {
        if (ascending)
        {
            storedItems.Sort((a, b) => a.collectionTime.CompareTo(b.collectionTime));
        }
        else
        {
            storedItems.Sort((a, b) => b.collectionTime.CompareTo(a.collectionTime));
        }
        
        OnStorageChanged?.Invoke();
        Debug.Log($"仓库物品已按采集时间排序 ({(ascending ? "升序" : "降序")})");
    }
    
    /// <summary>
    /// 按物品名称排序
    /// </summary>
    public void SortByName(bool ascending = true)
    {
        if (ascending)
        {
            storedItems.Sort((a, b) => string.Compare(a.displayName, b.displayName));
        }
        else
        {
            storedItems.Sort((a, b) => string.Compare(b.displayName, a.displayName));
        }
        
        OnStorageChanged?.Invoke();
        Debug.Log($"仓库物品已按名称排序 ({(ascending ? "升序" : "降序")})");
    }
    
    /// <summary>
    /// 清空仓库
    /// </summary>
    public void ClearStorage()
    {
        int oldCount = storedItems.Count;
        storedItems.Clear();
        
        OnStorageChanged?.Invoke();
        Debug.Log($"仓库已清空，移除了 {oldCount} 个物品");
    }
    
    /// <summary>
    /// 获取仓库统计信息
    /// </summary>
    public string GetStorageStats()
    {
        var stats = $"=== 仓库统计信息 ===\n";
        stats += $"存储物品: {storedItems.Count}/{currentCapacity}\n";
        stats += $"容量利用率: {(float)storedItems.Count / currentCapacity * 100:F1}%\n";
        stats += $"总页数: {GetTotalPages()}\n";
        stats += $"每页物品数: {itemsPerPage}\n";
        stats += $"最大容量限制: {maxCapacityLimit}\n";
        
        if (storedItems.Count > 0)
        {
            var oldestItem = storedItems.OrderBy(s => s.collectionTime).First();
            var newestItem = storedItems.OrderByDescending(s => s.collectionTime).First();
            
            stats += $"最早物品: {oldestItem.displayName} ({oldestItem.collectionTime:MM-dd HH:mm})\n";
            stats += $"最新物品: {newestItem.displayName} ({newestItem.collectionTime:MM-dd HH:mm})\n";
        }
        
        return stats;
    }
    
    /// <summary>
    /// 验证仓库数据完整性
    /// </summary>
    public bool ValidateStorage()
    {
        bool isValid = true;
        
        if (storedItems == null)
        {
            Debug.LogError("仓库物品列表为null");
            storedItems = new List<SampleItem>();
            isValid = false;
        }
        
        if (currentCapacity <= 0)
        {
            Debug.LogWarning("仓库容量异常，重置为基础容量");
            currentCapacity = baseCapacity;
            isValid = false;
        }
        
        if (currentCapacity > maxCapacityLimit)
        {
            Debug.LogWarning("仓库容量超过限制，调整为最大限制");
            currentCapacity = maxCapacityLimit;
            isValid = false;
        }
        
        // 检查重复ID
        var duplicateIDs = storedItems.GroupBy(item => item.sampleID)
                                     .Where(group => group.Count() > 1)
                                     .Select(group => group.Key);
        
        foreach (var duplicateID in duplicateIDs)
        {
            Debug.LogError($"发现重复的样本ID: {duplicateID}");
            isValid = false;
        }
        
        return isValid;
    }
}
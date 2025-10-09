using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// 样本背包系统 - 管理地质样本的存储和操作
/// </summary>
public class SampleInventory : MonoBehaviour
{
    [Header("背包设置")]
    public int maxSampleCapacity = 20;
    
    [Header("支持的物品类型")]
    public List<ItemType> acceptedTypes = new List<ItemType> 
    {
        ItemType.GeologicalSample  // 目前只支持地质样本
    };
    
    [Header("事件通知")]
    public bool enableDebugLog = true;
    
    // 单例模式
    public static SampleInventory Instance { get; private set; }
    
    // 样本存储
    private List<SampleItem> samples = new List<SampleItem>();
    
    // 事件系统
    public System.Action<SampleItem> OnSampleAdded;
    public System.Action<SampleItem> OnSampleRemoved;
    public System.Action OnInventoryChanged;
    
    // 支持的物品类型白名单
    private static readonly HashSet<ItemType> SUPPORTED_TYPES = new HashSet<ItemType>
    {
        ItemType.GeologicalSample
    };
    
    void Awake()
    {
        // 单例模式设置
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeInventory();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        ValidateSettings();
    }
    
    /// <summary>
    /// 初始化背包系统
    /// </summary>
    void InitializeInventory()
    {
        samples = new List<SampleItem>();
        
        if (enableDebugLog)
        {
            Debug.Log($"样本背包系统已初始化 - 最大容量: {maxSampleCapacity}");
        }
    }
    
    /// <summary>
    /// 验证设置
    /// </summary>
    void ValidateSettings()
    {
        if (maxSampleCapacity <= 0)
        {
            Debug.LogWarning("背包容量必须大于0，已设置为默认值20");
            maxSampleCapacity = 20;
        }
        
        if (acceptedTypes == null || acceptedTypes.Count == 0)
        {
            Debug.LogWarning("未设置接受的物品类型，已设置为默认值");
            acceptedTypes = new List<ItemType> { ItemType.GeologicalSample };
        }
    }
    
    /// <summary>
    /// 检查物品是否可以加入背包
    /// </summary>
    public bool CanAcceptItem(IInventoryItem item)
    {
        if (item == null)
        {
            LogMessage("物品为空，无法添加");
            return false;
        }
        
        // 检查类型是否被支持
        if (!IsItemTypeSupported(item.Type))
        {
            LogMessage($"物品类型 {item.Type} ({(int)item.Type}) 暂未实现背包支持");
            return false;
        }
        
        // 检查类型是否被接受
        if (!acceptedTypes.Contains(item.Type))
        {
            LogMessage($"{item.ItemName} 不能放入样本背包");
            return false;
        }
        
        // 检查容量
        if (samples.Count >= maxSampleCapacity)
        {
            LogMessage("样本背包已满！");
            return false;
        }
        
        // 检查是否已存在相同ID的物品
        if (HasSample(item.ItemID))
        {
            LogMessage($"背包中已存在相同ID的样本: {item.ItemID}");
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// 检查物品类型是否被支持
    /// </summary>
    public static bool IsItemTypeSupported(ItemType type)
    {
        return SUPPORTED_TYPES.Contains(type);
    }
    
    /// <summary>
    /// 尝试添加样本到背包
    /// </summary>
    public bool TryAddSample(SampleItem sample)
    {
        if (sample == null)
        {
            LogMessage("样本为空，无法添加");
            return false;
        }
        
        if (CanAcceptItem(sample))
        {
            samples.Add(sample);
            sample.currentLocation = SampleLocation.InInventory;
            sample.isPlayerPlaced = false;
            
            // 触发事件（添加异常保护）
            try
            {
                OnSampleAdded?.Invoke(sample);
                OnInventoryChanged?.Invoke();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SampleInventory] 触发事件时发生错误: {ex.Message}");
                Debug.LogError($"[SampleInventory] 错误堆栈: {ex.StackTrace}");
            }
            
            LogMessage($"已添加样本到背包: {sample.displayName} ({sample.sampleID})");
            LogMessage($"当前背包: {samples.Count}/{maxSampleCapacity}");
            
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 从背包移除样本
    /// </summary>
    public bool RemoveSample(SampleItem sample)
    {
        if (sample == null) return false;
        
        bool removed = samples.Remove(sample);
        if (removed)
        {
            // 触发事件
            OnSampleRemoved?.Invoke(sample);
            OnInventoryChanged?.Invoke();
            
            LogMessage($"已从背包移除样本: {sample.displayName}");
            LogMessage($"当前背包: {samples.Count}/{maxSampleCapacity}");
        }
        
        return removed;
    }
    
    /// <summary>
    /// 从背包移除样本（按ID）
    /// </summary>
    public bool RemoveSampleByID(string sampleID)
    {
        var sample = GetSampleByID(sampleID);
        return sample != null && RemoveSample(sample);
    }
    
    /// <summary>
    /// 从UI移除样本（但保留数据用于收回）
    /// </summary>
    public bool RemoveSampleFromUI(SampleItem sample)
    {
        // 这个方法用于样本被放置到世界时，从UI中隐藏但保留数据
        if (sample == null) return false;
        
        sample.currentLocation = SampleLocation.InWorld;
        
        // 不从samples列表中移除，只触发UI更新
        OnInventoryChanged?.Invoke();
        
        LogMessage($"样本已从UI移除（放置到世界）: {sample.displayName}");
        return true;
    }
    
    /// <summary>
    /// 将样本重新添加到背包UI（从世界收回）
    /// </summary>
    public bool AddSampleBackToInventory(SampleItem sample)
    {
        if (sample == null) return false;
        
        // 检查样本是否已在列表中
        if (samples.Contains(sample))
        {
            sample.currentLocation = SampleLocation.InInventory;
            sample.isPlayerPlaced = false;
            
            OnInventoryChanged?.Invoke();
            LogMessage($"样本已收回到背包: {sample.displayName}");
            return true;
        }
        else
        {
            // 如果不在列表中，尝试重新添加
            return TryAddSample(sample);
        }
    }
    
    /// <summary>
    /// 检查背包是否可以添加新样本
    /// </summary>
    public bool CanAddSample()
    {
        return samples.Count < maxSampleCapacity;
    }
    
    /// <summary>
    /// 检查是否存在指定ID的样本
    /// </summary>
    public bool HasSample(string sampleID)
    {
        return samples.Any(s => s.sampleID == sampleID);
    }
    
    /// <summary>
    /// 根据ID获取样本
    /// </summary>
    public SampleItem GetSampleByID(string sampleID)
    {
        return samples.FirstOrDefault(s => s.sampleID == sampleID);
    }
    
    /// <summary>
    /// 获取所有背包中的样本（只返回在背包中的）
    /// </summary>
    public List<SampleItem> GetInventorySamples()
    {
        return samples.Where(s => s.currentLocation == SampleLocation.InInventory).ToList();
    }
    
    /// <summary>
    /// 获取所有样本（包括放置在世界中的）
    /// </summary>
    public List<SampleItem> GetAllSamples()
    {
        return new List<SampleItem>(samples);
    }
    
    /// <summary>
    /// 获取当前背包使用情况
    /// </summary>
    public (int current, int max) GetCapacityInfo()
    {
        int currentCount = GetInventorySamples().Count;
        return (currentCount, maxSampleCapacity);
    }
    
    /// <summary>
    /// 清空背包
    /// </summary>
    public void ClearInventory()
    {
        int oldCount = samples.Count;
        samples.Clear();
        
        OnInventoryChanged?.Invoke();
        LogMessage($"背包已清空，移除了 {oldCount} 个样本");
    }
    
    /// <summary>
    /// 按采集时间排序样本
    /// </summary>
    public void SortByCollectionTime(bool ascending = true)
    {
        if (ascending)
        {
            samples.Sort((a, b) => a.collectionTime.CompareTo(b.collectionTime));
        }
        else
        {
            samples.Sort((a, b) => b.collectionTime.CompareTo(a.collectionTime));
        }
        
        OnInventoryChanged?.Invoke();
        LogMessage($"样本已按采集时间排序 ({(ascending ? "升序" : "降序")})");
    }
    
    /// <summary>
    /// 按样本名称排序
    /// </summary>
    public void SortByName(bool ascending = true)
    {
        if (ascending)
        {
            samples.Sort((a, b) => string.Compare(a.displayName, b.displayName));
        }
        else
        {
            samples.Sort((a, b) => string.Compare(b.displayName, a.displayName));
        }
        
        OnInventoryChanged?.Invoke();
        LogMessage($"样本已按名称排序 ({(ascending ? "升序" : "降序")})");
    }
    
    /// <summary>
    /// 获取背包统计信息
    /// </summary>
    public string GetInventoryStats()
    {
        var inventorySamples = GetInventorySamples();
        var worldSamples = samples.Where(s => s.currentLocation == SampleLocation.InWorld).ToList();
        
        string stats = $"=== 样本背包统计 ===\n";
        stats += $"背包中样本: {inventorySamples.Count}/{maxSampleCapacity}\n";
        stats += $"世界中样本: {worldSamples.Count}\n";
        stats += $"总样本数量: {samples.Count}\n";
        
        if (inventorySamples.Count > 0)
        {
            var oldestSample = inventorySamples.OrderBy(s => s.collectionTime).First();
            var newestSample = inventorySamples.OrderByDescending(s => s.collectionTime).First();
            
            stats += $"最早采集: {oldestSample.displayName} ({oldestSample.collectionTime:MM-dd HH:mm})\n";
            stats += $"最新采集: {newestSample.displayName} ({newestSample.collectionTime:MM-dd HH:mm})\n";
        }
        
        return stats;
    }
    
    /// <summary>
    /// 日志输出
    /// </summary>
    void LogMessage(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[SampleInventory] {message}");
        }
    }
    
    /// <summary>
    /// 在Inspector中显示背包状态
    /// </summary>
    [ContextMenu("显示背包状态")]
    void ShowInventoryStatus()
    {
        Debug.Log(GetInventoryStats());
    }
    
    /// <summary>
    /// 测试添加样本
    /// </summary>
    [ContextMenu("测试添加样本")]
    void TestAddSample()
    {
        var testSample = new SampleItem
        {
            sampleID = $"TEST_{DateTime.Now:HHmmss}",
            displayName = $"测试样本_{DateTime.Now:HHmm}",
            description = "测试用地质样本",
            collectionTime = DateTime.Now,
            originalCollectionPosition = Vector3.zero,
            sourceToolID = "1000"
        };
        
        TryAddSample(testSample);
    }
}
using UnityEngine;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// 仓库管理器 - 单例模式管理仓库系统
/// </summary>
public class WarehouseManager : MonoBehaviour
{
    [Header("仓库设置")]
    public bool enableDebugLog = true;
    public bool autoSave = true;
    public float autoSaveInterval = 60f; // 自动保存间隔（秒）
    
    // 单例实例
    public static WarehouseManager Instance { get; private set; }
    
    // 仓库存储系统
    public WarehouseStorage Storage { get; private set; }
    
    // 事件系统
    public System.Action OnWarehouseDataLoaded;
    public System.Action OnWarehouseDataSaved;
    
    // 保存路径
    private string saveFilePath;
    private float lastSaveTime;
    
    void Awake()
    {
        // 单例模式设置
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeWarehouse();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        LoadWarehouseData();
    }
    
    void Update()
    {
        // 自动保存
        if (autoSave && Time.time - lastSaveTime > autoSaveInterval)
        {
            SaveWarehouseData();
        }
    }
    
    /// <summary>
    /// 初始化仓库系统
    /// </summary>
    void InitializeWarehouse()
    {
        Storage = new WarehouseStorage();
        Storage.Initialize();
        
        // 设置保存路径
        saveFilePath = Path.Combine(Application.persistentDataPath, "warehouse_data.json");
        
        // 订阅存储事件
        Storage.OnItemAdded += OnItemAddedToWarehouse;
        Storage.OnItemRemoved += OnItemRemovedFromWarehouse;
        Storage.OnStorageChanged += OnWarehouseStorageChanged;
        Storage.OnCapacityChanged += OnWarehouseCapacityChanged;
        
        lastSaveTime = Time.time;
        
        LogMessage("仓库管理器初始化完成");
    }
    
    /// <summary>
    /// 物品添加到仓库时的回调
    /// </summary>
    void OnItemAddedToWarehouse(SampleItem item)
    {
        LogMessage($"物品已添加到仓库: {item.displayName}");
        
        // 标记需要保存
        if (autoSave)
        {
            lastSaveTime = Time.time - autoSaveInterval + 5f; // 5秒后自动保存
        }
    }
    
    /// <summary>
    /// 物品从仓库移除时的回调
    /// </summary>
    void OnItemRemovedFromWarehouse(SampleItem item)
    {
        LogMessage($"物品已从仓库移除: {item.displayName}");
        
        // 标记需要保存
        if (autoSave)
        {
            lastSaveTime = Time.time - autoSaveInterval + 5f; // 5秒后自动保存
        }
    }
    
    /// <summary>
    /// 仓库存储变化时的回调
    /// </summary>
    void OnWarehouseStorageChanged()
    {
        LogMessage($"仓库存储已变化，当前物品数: {Storage.GetCapacityInfo().current}");
    }
    
    /// <summary>
    /// 仓库容量变化时的回调
    /// </summary>
    void OnWarehouseCapacityChanged(int newCapacity)
    {
        LogMessage($"仓库容量已变化: {newCapacity}");
        
        // 容量变化时立即保存
        SaveWarehouseData();
    }
    
    /// <summary>
    /// 从背包移动物品到仓库
    /// </summary>
    public bool MoveFromInventoryToWarehouse(SampleItem item)
    {
        if (item == null)
        {
            LogMessage("物品为空，无法移动到仓库", true);
            return false;
        }
        
        // 检查物品是否在背包中
        if (item.currentLocation != SampleLocation.InInventory)
        {
            LogMessage($"物品 {item.displayName} 不在背包中，当前位置: {item.currentLocation}", true);
            return false;
        }
        
        // 检查仓库是否可以添加
        if (!Storage.CanAddItem(item))
        {
            LogMessage($"仓库无法添加物品: {item.displayName}", true);
            return false;
        }
        
        // 从背包移除
        var inventory = SampleInventory.Instance;
        if (inventory == null)
        {
            LogMessage("未找到样本背包系统", true);
            return false;
        }
        
        if (!inventory.RemoveSample(item))
        {
            LogMessage($"从背包移除物品失败: {item.displayName}", true);
            return false;
        }
        
        // 添加到仓库
        if (Storage.AddItem(item))
        {
            LogMessage($"物品已从背包移动到仓库: {item.displayName}");
            return true;
        }
        else
        {
            // 如果添加到仓库失败，尝试恢复到背包
            inventory.TryAddSample(item);
            LogMessage($"移动到仓库失败，已恢复到背包: {item.displayName}", true);
            return false;
        }
    }
    
    /// <summary>
    /// 从仓库移动物品到背包
    /// </summary>
    public bool MoveFromWarehouseToInventory(SampleItem item)
    {
        if (item == null)
        {
            LogMessage("物品为空，无法移动到背包", true);
            return false;
        }
        
        // 检查物品是否在仓库中
        if (item.currentLocation != SampleLocation.InWarehouse)
        {
            LogMessage($"物品 {item.displayName} 不在仓库中，当前位置: {item.currentLocation}", true);
            return false;
        }
        
        // 检查背包是否可以添加
        var inventory = SampleInventory.Instance;
        if (inventory == null)
        {
            LogMessage("未找到样本背包系统", true);
            return false;
        }
        
        if (!inventory.CanAddSample())
        {
            LogMessage("背包已满，无法添加物品", true);
            return false;
        }
        
        // 从仓库移除
        if (!Storage.RemoveItem(item))
        {
            LogMessage($"从仓库移除物品失败: {item.displayName}", true);
            return false;
        }
        
        // 添加到背包
        if (inventory.TryAddSample(item))
        {
            LogMessage($"物品已从仓库移动到背包: {item.displayName}");
            return true;
        }
        else
        {
            // 如果添加到背包失败，尝试恢复到仓库
            Storage.AddItem(item);
            LogMessage($"移动到背包失败，已恢复到仓库: {item.displayName}", true);
            return false;
        }
    }
    
    /// <summary>
    /// 批量从背包移动到仓库
    /// </summary>
    public bool BatchMoveFromInventoryToWarehouse(List<SampleItem> items)
    {
        if (items == null || items.Count == 0)
        {
            return true;
        }
        
        // 预检查所有物品
        foreach (var item in items)
        {
            if (item == null || item.currentLocation != SampleLocation.InInventory)
            {
                LogMessage("批量移动失败：包含无效或不在背包中的物品", true);
                return false;
            }
        }
        
        if (!Storage.CanAddItems(items))
        {
            LogMessage("批量移动失败：仓库容量不足", true);
            return false;
        }
        
        var inventory = SampleInventory.Instance;
        if (inventory == null)
        {
            LogMessage("未找到样本背包系统", true);
            return false;
        }
        
        // 执行批量移动
        List<SampleItem> movedItems = new List<SampleItem>();
        
        foreach (var item in items)
        {
            if (inventory.RemoveSample(item))
            {
                if (Storage.AddItem(item))
                {
                    movedItems.Add(item);
                }
                else
                {
                    // 恢复到背包
                    inventory.TryAddSample(item);
                    LogMessage($"物品 {item.displayName} 添加到仓库失败，已恢复", true);
                }
            }
        }
        
        LogMessage($"批量移动完成：{movedItems.Count}/{items.Count} 个物品已移动到仓库");
        return movedItems.Count == items.Count;
    }
    
    /// <summary>
    /// 批量从仓库移动到背包
    /// </summary>
    public bool BatchMoveFromWarehouseToInventory(List<SampleItem> items)
    {
        if (items == null || items.Count == 0)
        {
            return true;
        }
        
        // 预检查所有物品
        foreach (var item in items)
        {
            if (item == null || item.currentLocation != SampleLocation.InWarehouse)
            {
                LogMessage("批量移动失败：包含无效或不在仓库中的物品", true);
                return false;
            }
        }
        
        var inventory = SampleInventory.Instance;
        if (inventory == null)
        {
            LogMessage("未找到样本背包系统", true);
            return false;
        }
        
        // 检查背包容量
        var capacityInfo = inventory.GetCapacityInfo();
        int availableSpace = capacityInfo.max - capacityInfo.current;
        
        if (availableSpace < items.Count)
        {
            LogMessage($"批量移动失败：背包空间不足，需要 {items.Count} 个空位，当前剩余 {availableSpace} 个", true);
            return false;
        }
        
        // 执行批量移动
        List<SampleItem> movedItems = new List<SampleItem>();
        
        foreach (var item in items)
        {
            if (Storage.RemoveItem(item))
            {
                if (inventory.TryAddSample(item))
                {
                    movedItems.Add(item);
                }
                else
                {
                    // 恢复到仓库
                    Storage.AddItem(item);
                    LogMessage($"物品 {item.displayName} 添加到背包失败，已恢复", true);
                }
            }
        }
        
        LogMessage($"批量移动完成：{movedItems.Count}/{items.Count} 个物品已移动到背包");
        return movedItems.Count == items.Count;
    }
    
    /// <summary>
    /// 保存仓库数据
    /// </summary>
    public void SaveWarehouseData()
    {
        try
        {
            var saveData = new WarehouseSaveData
            {
                currentCapacity = Storage.currentCapacity,
                storedItems = Storage.GetAllItems(),
                saveTimestamp = System.DateTime.Now.ToBinary()
            };
            
            // 确保所有物品的时间数据都正确设置
            foreach (var item in saveData.storedItems)
            {
                if (item.collectionTime.Year == 1) // 检查是否是默认时间
                {
                    LogMessage($"警告: 物品 {item.displayName} 的采集时间无效，将使用当前时间");
                    item.collectionTime = System.DateTime.Now;
                }
            }
            
            string jsonData = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(saveFilePath, jsonData);
            
            lastSaveTime = Time.time;
            OnWarehouseDataSaved?.Invoke();
            
            LogMessage($"仓库数据已保存，物品数: {saveData.storedItems.Count}");
        }
        catch (System.Exception e)
        {
            LogMessage($"保存仓库数据失败: {e.Message}", true);
        }
    }
    
    /// <summary>
    /// 加载仓库数据
    /// </summary>
    public void LoadWarehouseData()
    {
        try
        {
            if (File.Exists(saveFilePath))
            {
                string jsonData = File.ReadAllText(saveFilePath);
                var saveData = JsonUtility.FromJson<WarehouseSaveData>(jsonData);
                
                if (saveData != null)
                {
                    Storage.SetCapacity(saveData.currentCapacity);
                    
                    // 清空当前数据并加载保存的物品
                    Storage.ClearStorage();
                    if (saveData.storedItems != null)
                    {
                        foreach (var item in saveData.storedItems)
                        {
                            if (item != null)
                            {
                                item.currentLocation = SampleLocation.InWarehouse;
                                Storage.AddItem(item);
                            }
                        }
                    }
                    
                    OnWarehouseDataLoaded?.Invoke();
                    
                    System.DateTime saveTime = System.DateTime.FromBinary(saveData.saveTimestamp);
                    LogMessage($"仓库数据已加载，物品数: {saveData.storedItems?.Count ?? 0}，保存时间: {saveTime:yyyy-MM-dd HH:mm:ss}");
                }
            }
            else
            {
                LogMessage("未找到仓库数据文件，使用默认设置");
            }
        }
        catch (System.Exception e)
        {
            LogMessage($"加载仓库数据失败: {e.Message}", true);
        }
    }
    
    /// <summary>
    /// 获取仓库统计信息
    /// </summary>
    public string GetWarehouseStats()
    {
        if (Storage == null)
        {
            return "仓库系统未初始化";
        }
        
        return Storage.GetStorageStats();
    }
    
    /// <summary>
    /// 扩展仓库容量（用于任务奖励等）
    /// </summary>
    public bool ExpandWarehouseCapacity(int additionalCapacity, string reason = "")
    {
        if (Storage == null)
        {
            LogMessage("仓库系统未初始化", true);
            return false;
        }
        
        bool success = Storage.ExpandCapacity(additionalCapacity);
        if (success)
        {
            LogMessage($"仓库容量扩展成功：+{additionalCapacity} {(string.IsNullOrEmpty(reason) ? "" : $"({reason})")}");
            SaveWarehouseData(); // 立即保存
        }
        
        return success;
    }
    
    /// <summary>
    /// 验证仓库数据完整性
    /// </summary>
    public bool ValidateWarehouseData()
    {
        if (Storage == null)
        {
            LogMessage("仓库存储系统为null", true);
            return false;
        }
        
        return Storage.ValidateStorage();
    }
    
    /// <summary>
    /// 日志输出
    /// </summary>
    void LogMessage(string message, bool isWarning = false)
    {
        if (enableDebugLog)
        {
            string logMessage = $"[WarehouseManager] {message}";
            
            // 过滤掉频繁的保存信息，只保留重要的调试信息
            if (message.Contains("仓库数据已保存") && !isWarning)
            {
                return; // 不输出保存信息
            }
            
            if (isWarning)
            {
                Debug.LogWarning(logMessage);
            }
            else
            {
                Debug.Log(logMessage);
            }
        }
    }
    
    /// <summary>
    /// 在Inspector中显示仓库状态
    /// </summary>
    [ContextMenu("显示仓库状态")]
    void ShowWarehouseStatus()
    {
        Debug.Log(GetWarehouseStats());
    }
    
    /// <summary>
    /// 手动保存数据
    /// </summary>
    [ContextMenu("手动保存数据")]
    void ManualSave()
    {
        SaveWarehouseData();
    }
    
    /// <summary>
    /// 手动加载数据
    /// </summary>
    [ContextMenu("手动加载数据")]
    void ManualLoad()
    {
        LoadWarehouseData();
    }
    
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && autoSave)
        {
            SaveWarehouseData();
        }
    }
    
    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && autoSave)
        {
            SaveWarehouseData();
        }
    }
    
    void OnDestroy()
    {
        if (autoSave && Instance == this)
        {
            SaveWarehouseData();
        }
    }
}

/// <summary>
/// 仓库保存数据结构
/// </summary>
[System.Serializable]
public class WarehouseSaveData
{
    public int currentCapacity;
    public List<SampleItem> storedItems;
    public long saveTimestamp;
}
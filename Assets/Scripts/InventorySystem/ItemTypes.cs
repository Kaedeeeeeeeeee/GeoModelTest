using UnityEngine;

/// <summary>
/// 物品类型枚举 - 使用5000系列避免与工具ID(1000系列)冲突
/// </summary>
public enum ItemType
{
    // === 地质样本类 (5000-5099) ===
    GeologicalSample = 5000,           // 地质钻探样本 ✅ 当前支持
    SurfaceRockSample = 5001,          // 🔮 表面岩石样本
    SedimentSample = 5002,             // 🔮 沉积物样本
    
    // === 观察记录类 (6000-6099) ===  
    DronePhoto = 6000,                 // 🔮 无人机照片
    TerrainScan = 6001,                // 🔮 地形扫描数据
    GeologicalMap = 6002,              // 🔮 地质图谱
    
    // === 工具零件类 (7000-7099) ===
    DrillBit = 7000,                   // 🔮 钻头
    Battery = 7001,                    // 🔮 电池
    RepairKit = 7002,                  // 🔮 维修包
    
    // === 消耗品类 (8000-8099) ===
    Fuel = 8000,                       // 🔮 燃料
    Lubricant = 8001,                  // 🔮 润滑油
}

/// <summary>
/// 背包物品接口 - 所有可收集物品必须实现此接口
/// </summary>
public interface IInventoryItem
{
    /// <summary>
    /// 物品唯一ID
    /// </summary>
    string ItemID { get; }
    
    /// <summary>
    /// 物品显示名称
    /// </summary>
    string ItemName { get; }
    
    /// <summary>
    /// 物品类型
    /// </summary>
    ItemType Type { get; }
    
    /// <summary>
    /// 物品图标
    /// </summary>
    Sprite Icon { get; }
    
    /// <summary>
    /// 是否可以堆叠
    /// </summary>
    bool CanStack { get; }
    
    /// <summary>
    /// 最大堆叠数量
    /// </summary>
    int MaxStackSize { get; }
    
    /// <summary>
    /// 物品描述
    /// </summary>
    string Description { get; }
}

/// <summary>
/// 样本位置状态
/// </summary>
public enum SampleLocation
{
    InInventory,    // 在背包中
    InWorld,        // 在世界中（放置状态）
    InWarehouse     // 在仓库中
}

/// <summary>
/// ID命名空间说明
/// </summary>
public static class IDNamespace
{
    // === 工具ID系列 (1000-1999) ===
    // 1000-1099: 采集工具
    // 1100-1199: 观察工具  
    // 1200-1299: 记录工具
    
    // === 背包物品ID系列 (5000-8999) ===
    // 5000-5999: 样本类物品
    // 6000-6999: 记录类物品
    // 7000-7999: 零件类物品
    // 8000-8999: 消耗品类物品
    
    // === 其他系统ID预留 ===
    // 2000-2999: 预留给场景对象
    // 3000-3999: 预留给NPC/实体
    // 4000-4999: 预留给任务系统
    // 9000-9999: 预留给特殊系统
}
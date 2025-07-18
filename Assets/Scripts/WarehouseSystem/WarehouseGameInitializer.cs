using UnityEngine;
using System.Collections;

/// <summary>
/// 仓库系统游戏初始化器 - 负责初始化和设置仓库系统
/// </summary>
public class WarehouseGameInitializer : MonoBehaviour
{
    [Header("仓库系统设置")]
    public bool autoInitializeOnStart = true;
    public float initializationDelay = 1f;
    
    [Header("仓库触发器设置")]
    public bool createWarehouseTrigger = true;
    public Vector3 triggerPosition = new Vector3(-1.924f, 1.06f, 0.662f);
    public Vector3 triggerRotation = Vector3.zero;
    public Vector3 triggerScale = Vector3.one;
    
    [Header("UI设置")]
    public bool createWarehouseUI = true;
    public bool enableDebugMode = true;
    
    [Header("组件引用")]
    public WarehouseManager warehouseManager;
    public WarehouseTrigger warehouseTrigger;
    public WarehouseUI warehouseUI;
    
    // 初始化状态
    private bool isInitialized = false;
    private bool isInitializing = false;
    
    void Start()
    {
        // 强制确保triggerPosition使用正确的值
        EnsureCorrectTriggerPosition();
        
        if (autoInitializeOnStart)
        {
            StartCoroutine(InitializeWarehouseSystemDelayed());
        }
        
        // 添加运行时调试器
        if (FindFirstObjectByType<RuntimeDebugger>() == null)
        {
            GameObject debuggerObj = new GameObject("RuntimeDebugger");
            debuggerObj.AddComponent<RuntimeDebugger>();
            DontDestroyOnLoad(debuggerObj);
            Debug.Log("创建了RuntimeDebugger调试器");
        }
    }
    
    /// <summary>
    /// 强制确保triggerPosition使用正确的值
    /// </summary>
    void EnsureCorrectTriggerPosition()
    {
        Vector3 correctPosition = new Vector3(-1.924f, 1.06f, 0.662f);
        
        // 检查当前值是否正确
        if (triggerPosition != correctPosition)
        {
            Debug.LogWarning($"检测到triggerPosition值不正确: {triggerPosition}，强制修正为: {correctPosition}");
            triggerPosition = correctPosition;
        }
        
        // 同时检查是否为旧的默认值
        if (triggerPosition == Vector3.zero || triggerPosition == new Vector3(0, 1, 0))
        {
            Debug.LogWarning($"检测到triggerPosition为旧默认值: {triggerPosition}，强制修正为: {correctPosition}");
            triggerPosition = correctPosition;
        }
        
        Debug.Log($"triggerPosition已确认为正确值: {triggerPosition}");
    }
    
    /// <summary>
    /// 延迟初始化仓库系统
    /// </summary>
    IEnumerator InitializeWarehouseSystemDelayed()
    {
        yield return new WaitForSeconds(initializationDelay);
        InitializeWarehouseSystem();
    }
    
    /// <summary>
    /// 初始化仓库系统
    /// </summary>
    public void InitializeWarehouseSystem()
    {
        if (isInitialized || isInitializing)
        {
            Debug.LogWarning("仓库系统已经初始化或正在初始化");
            return;
        }
        
        isInitializing = true;
        Debug.Log("开始初始化仓库系统...");
        
        try
        {
            // 1. 初始化仓库管理器
            InitializeWarehouseManager();
            
            // 2. 创建仓库触发器
            if (createWarehouseTrigger)
            {
                InitializeWarehouseTrigger();
            }
            
            // 3. 创建仓库UI
            if (createWarehouseUI)
            {
                InitializeWarehouseUI();
            }
            
            // 4. 验证系统完整性
            ValidateSystemIntegrity();
            
            isInitialized = true;
            isInitializing = false;
            
            Debug.Log("仓库系统初始化完成！");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"仓库系统初始化失败: {e.Message}");
            isInitializing = false;
        }
    }
    
    /// <summary>
    /// 初始化仓库管理器
    /// </summary>
    void InitializeWarehouseManager()
    {
        // 查找现有的仓库管理器
        if (warehouseManager == null)
        {
            warehouseManager = FindFirstObjectByType<WarehouseManager>();
        }
        
        // 如果没有找到，创建新的
        if (warehouseManager == null)
        {
            GameObject managerObj = new GameObject("WarehouseManager");
            warehouseManager = managerObj.AddComponent<WarehouseManager>();
            
            // 设置为DontDestroyOnLoad
            DontDestroyOnLoad(managerObj);
            
            Debug.Log("创建了新的WarehouseManager");
        }
        else
        {
            Debug.Log("找到现有的WarehouseManager");
        }
    }
    
    /// <summary>
    /// 初始化仓库触发器
    /// </summary>
    void InitializeWarehouseTrigger()
    {
        // 查找现有的仓库触发器
        if (warehouseTrigger == null)
        {
            warehouseTrigger = FindFirstObjectByType<WarehouseTrigger>();
        }
        
        // 如果没有找到，创建新的
        if (warehouseTrigger == null)
        {
            GameObject triggerObj = new GameObject("WarehouseTrigger");
            warehouseTrigger = triggerObj.AddComponent<WarehouseTrigger>();
            
            Debug.Log("创建了新的WarehouseTrigger");
        }
        else
        {
            Debug.Log("找到现有的WarehouseTrigger");
        }
        
        // 无论是新创建还是现有的，都强制更新位置
        Debug.Log($"准备设置WarehouseTrigger位置: {triggerPosition}");
        warehouseTrigger.ForceUpdatePosition(triggerPosition, triggerRotation, triggerScale);
        
        // 立即验证位置是否正确设置
        if (warehouseTrigger.transform.position != triggerPosition)
        {
            Debug.LogError($"WarehouseTrigger位置设置失败！期望: {triggerPosition}, 实际: {warehouseTrigger.transform.position}");
            // 再次尝试设置
            warehouseTrigger.transform.position = triggerPosition;
            Debug.Log($"重新强制设置WarehouseTrigger位置为: {triggerPosition}");
        }
        else
        {
            Debug.Log($"WarehouseTrigger位置设置成功: {warehouseTrigger.transform.position}");
        }
        
        // 启动协程进行延迟验证
        StartCoroutine(VerifyTriggerPositionDelayed(warehouseTrigger));
    }
    
    /// <summary>
    /// 延迟验证触发器位置
    /// </summary>
    IEnumerator VerifyTriggerPositionDelayed(WarehouseTrigger trigger)
    {
        yield return new WaitForSeconds(1f); // 等待1秒确保所有初始化完成
        
        if (trigger != null)
        {
            Vector3 expectedPosition = new Vector3(-1.924f, 1.06f, 0.662f);
            if (trigger.transform.position != expectedPosition)
            {
                Debug.LogError($"延迟验证失败！WarehouseTrigger位置不正确: {trigger.transform.position}, 期望: {expectedPosition}");
                // 最后一次强制设置
                trigger.transform.position = expectedPosition;
                trigger.ForceUpdatePosition(expectedPosition, triggerRotation, triggerScale);
                Debug.Log($"最终强制设置WarehouseTrigger位置为: {expectedPosition}");
            }
            else
            {
                Debug.Log($"延迟验证成功！WarehouseTrigger位置正确: {trigger.transform.position}");
            }
        }
    }
    
    /// <summary>
    /// 初始化仓库UI
    /// </summary>
    void InitializeWarehouseUI()
    {
        // 查找现有的仓库UI
        if (warehouseUI == null)
        {
            warehouseUI = FindFirstObjectByType<WarehouseUI>();
        }
        
        // 如果没有找到，创建新的
        if (warehouseUI == null)
        {
            GameObject uiObj = new GameObject("WarehouseUI");
            warehouseUI = uiObj.AddComponent<WarehouseUI>();
            
            Debug.Log("创建了新的WarehouseUI");
        }
        else
        {
            Debug.Log("找到现有的WarehouseUI");
        }
    }
    
    /// <summary>
    /// 验证系统完整性
    /// </summary>
    void ValidateSystemIntegrity()
    {
        bool isValid = true;
        
        // 验证仓库管理器
        if (warehouseManager == null)
        {
            Debug.LogError("WarehouseManager 未找到");
            isValid = false;
        }
        else if (warehouseManager.Storage == null)
        {
            Debug.LogError("WarehouseStorage 未初始化");
            isValid = false;
        }
        
        // 验证仓库触发器
        if (createWarehouseTrigger && warehouseTrigger == null)
        {
            Debug.LogError("WarehouseTrigger 未找到");
            isValid = false;
        }
        
        // 验证仓库UI
        if (createWarehouseUI && warehouseUI == null)
        {
            Debug.LogError("WarehouseUI 未找到");
            isValid = false;
        }
        
        // 验证样本背包系统
        var sampleInventory = SampleInventory.Instance;
        if (sampleInventory == null)
        {
            Debug.LogWarning("SampleInventory 未找到，仓库系统可能无法正常与背包交互");
        }
        
        if (isValid)
        {
            Debug.Log("仓库系统完整性验证通过");
        }
        else
        {
            Debug.LogError("仓库系统完整性验证失败");
        }
    }
    
    /// <summary>
    /// 手动设置触发器位置
    /// </summary>
    public void SetTriggerPosition(Vector3 position, Vector3 rotation, Vector3 scale)
    {
        triggerPosition = position;
        triggerRotation = rotation;
        triggerScale = scale;
        
        if (warehouseTrigger != null)
        {
            warehouseTrigger.triggerPosition = position;
            warehouseTrigger.triggerRotation = rotation;
            warehouseTrigger.triggerScale = scale;
        }
        
        Debug.Log($"触发器位置已设置: {position}, {rotation}, {scale}");
    }
    
    /// <summary>
    /// 重新初始化系统
    /// </summary>
    public void ReinitializeSystem()
    {
        if (isInitializing)
        {
            Debug.LogWarning("系统正在初始化中，请稍后重试");
            return;
        }
        
        Debug.Log("重新初始化仓库系统...");
        
        isInitialized = false;
        InitializeWarehouseSystem();
    }
    
    /// <summary>
    /// 获取系统状态
    /// </summary>
    public string GetSystemStatus()
    {
        var status = "=== 仓库系统状态 ===\n";
        status += $"已初始化: {isInitialized}\n";
        status += $"正在初始化: {isInitializing}\n";
        status += $"WarehouseManager: {(warehouseManager != null ? "✓" : "✗")}\n";
        status += $"WarehouseTrigger: {(warehouseTrigger != null ? "✓" : "✗")}\n";
        status += $"WarehouseUI: {(warehouseUI != null ? "✓" : "✗")}\n";
        
        if (warehouseManager != null && warehouseManager.Storage != null)
        {
            var capacityInfo = warehouseManager.Storage.GetCapacityInfo();
            status += $"仓库容量: {capacityInfo.current}/{capacityInfo.max}\n";
        }
        
        var sampleInventory = SampleInventory.Instance;
        if (sampleInventory != null)
        {
            var inventoryInfo = sampleInventory.GetCapacityInfo();
            status += $"背包容量: {inventoryInfo.current}/{inventoryInfo.max}\n";
        }
        
        return status;
    }
    
    /// <summary>
    /// 检查是否已初始化
    /// </summary>
    public bool IsInitialized()
    {
        return isInitialized;
    }
    
    /// <summary>
    /// 检查是否正在初始化
    /// </summary>
    public bool IsInitializing()
    {
        return isInitializing;
    }
    
    /// <summary>
    /// 获取组件引用
    /// </summary>
    public void GetComponentReferences()
    {
        warehouseManager = FindFirstObjectByType<WarehouseManager>();
        warehouseTrigger = FindFirstObjectByType<WarehouseTrigger>();
        warehouseUI = FindFirstObjectByType<WarehouseUI>();
    }
    
    /// <summary>
    /// 在Inspector中手动初始化
    /// </summary>
    [ContextMenu("手动初始化仓库系统")]
    void ManualInitialize()
    {
        InitializeWarehouseSystem();
    }
    
    /// <summary>
    /// 在Inspector中显示系统状态
    /// </summary>
    [ContextMenu("显示系统状态")]
    void ShowSystemStatus()
    {
        Debug.Log(GetSystemStatus());
    }
    
    /// <summary>
    /// 在Inspector中验证系统
    /// </summary>
    [ContextMenu("验证系统完整性")]
    void ManualValidate()
    {
        ValidateSystemIntegrity();
    }
    
    /// <summary>
    /// 重置系统（谨慎使用）
    /// </summary>
    [ContextMenu("重置系统")]
    void ResetSystem()
    {
        Debug.LogWarning("正在重置仓库系统...");
        
        isInitialized = false;
        isInitializing = false;
        
        // 清空引用
        warehouseManager = null;
        warehouseTrigger = null;
        warehouseUI = null;
        
        Debug.Log("系统已重置，请重新初始化");
    }
}
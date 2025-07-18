using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 选择模式枚举
/// </summary>
public enum SelectionMode
{
    None,               // 未选择模式
    Ready,              // 准备多选模式（等待第一个物品选择）
    BackpackSelection,  // 背包选择模式
    WarehouseSelection  // 仓库选择模式
}

/// <summary>
/// 多选系统 - 管理仓库和背包的多选功能
/// </summary>
public class MultiSelectSystem : MonoBehaviour
{
    [Header("多选设置")]
    public bool enableDebugLog = false; // 关闭调试输出
    public int maxSelectCount = 50; // 最大选择数量
    
    [Header("视觉反馈")]
    public Color selectedItemColor = Color.cyan;
    public Color selectedBorderColor = Color.yellow;
    public float borderWidth = 3f;
    
    // 当前选择状态
    private SelectionMode currentMode = SelectionMode.None;
    private List<SampleItem> selectedItems = new List<SampleItem>();
    private Dictionary<SampleItem, GameObject> selectedVisuals = new Dictionary<SampleItem, GameObject>();
    
    // 防重复调用机制
    private bool isProcessingSelection = false;
    private float lastSelectionTime = 0f;
    private const float SELECTION_COOLDOWN = 0.1f; // 100ms冷却时间
    
    // 事件系统
    public System.Action<SelectionMode> OnSelectionModeChanged;
    public System.Action<List<SampleItem>> OnSelectionChanged;
    public System.Action<SampleItem> OnItemSelected;
    public System.Action<SampleItem> OnItemDeselected;
    
    void Start()
    {
        InitializeMultiSelectSystem();
    }
    
    /// <summary>
    /// 初始化多选系统
    /// </summary>
    void InitializeMultiSelectSystem()
    {
        currentMode = SelectionMode.None;
        selectedItems.Clear();
        selectedVisuals.Clear();
        
        LogMessage("多选系统初始化完成");
    }
    
    /// <summary>
    /// 进入多选模式
    /// </summary>
    public void EnterMultiSelectMode()
    {
        if (currentMode != SelectionMode.None)
        {
            LogMessage("已在多选模式中");
            return;
        }
        
        // 清空之前的选择
        ClearAllSelections();
        
        // 设置为准备选择状态
        currentMode = SelectionMode.Ready;
        LogMessage("进入多选模式，等待选择物品");
        
        // 通知UI进入多选模式
        OnSelectionModeChanged?.Invoke(currentMode);
    }
    
    /// <summary>
    /// 退出多选模式
    /// </summary>
    public void ExitMultiSelectMode()
    {
        LogMessage($"退出多选模式，之前模式: {currentMode}");
        
        // 清空所有选择
        ClearAllSelections();
        
        // 重置模式
        currentMode = SelectionMode.None;
        
        OnSelectionModeChanged?.Invoke(currentMode);
        OnSelectionChanged?.Invoke(new List<SampleItem>());
    }
    
    /// <summary>
    /// 选择物品
    /// </summary>
    public bool SelectItem(SampleItem item, SampleLocation itemLocation)
    {
        if (item == null)
        {
            LogMessage("无法选择空物品", true);
            return false;
        }
        
        // 检查是否已选择
        if (selectedItems.Contains(item))
        {
            LogMessage($"物品已被选择: {item.displayName}");
            return true;
        }
        
        // 检查选择数量限制
        if (selectedItems.Count >= maxSelectCount)
        {
            LogMessage($"达到最大选择数量限制: {maxSelectCount}", true);
            return false;
        }
        
        // 确定选择模式
        SelectionMode requiredMode = DetermineSelectionMode(itemLocation);
        
        // 检查模式兼容性
        if (!IsSelectionModeCompatible(requiredMode))
        {
            LogMessage($"选择模式不兼容。当前模式: {currentMode}, 需要模式: {requiredMode}", true);
            return false;
        }
        
        // 设置选择模式（如果是第一个物品或从Ready状态转换）
        if (currentMode == SelectionMode.None || currentMode == SelectionMode.Ready)
        {
            currentMode = requiredMode;
            OnSelectionModeChanged?.Invoke(currentMode);
            LogMessage($"设置选择模式为: {currentMode}");
        }
        
        // 添加到选择列表
        selectedItems.Add(item);
        
        // 创建视觉反馈
        CreateSelectionVisual(item);
        
        // 触发事件
        OnItemSelected?.Invoke(item);
        OnSelectionChanged?.Invoke(new List<SampleItem>(selectedItems));
        
        LogMessage($"物品已选择: {item.displayName} (总计: {selectedItems.Count})");
        return true;
    }
    
    /// <summary>
    /// 取消选择物品
    /// </summary>
    public bool DeselectItem(SampleItem item)
    {
        if (item == null || !selectedItems.Contains(item))
        {
            return false;
        }
        
        // 从选择列表移除
        selectedItems.Remove(item);
        
        // 移除视觉反馈
        RemoveSelectionVisual(item);
        
        // 如果没有选中的物品，但仍保持在多选模式
        // 只有用户主动点击"退出多选"才会退出模式
        if (selectedItems.Count == 0)
        {
            // 检查当前是否是具体的选择模式，如果是则返回到Ready状态
            if (currentMode == SelectionMode.BackpackSelection || currentMode == SelectionMode.WarehouseSelection)
            {
                currentMode = SelectionMode.Ready;
                OnSelectionModeChanged?.Invoke(currentMode);
                LogMessage("选择清空，返回Ready状态");
            }
        }
        
        // 触发事件
        OnItemDeselected?.Invoke(item);
        OnSelectionChanged?.Invoke(new List<SampleItem>(selectedItems));
        
        LogMessage($"取消选择物品: {item.displayName} (剩余: {selectedItems.Count})");
        return true;
    }
    
    /// <summary>
    /// 切换物品选择状态
    /// </summary>
    public bool ToggleItemSelection(SampleItem item, SampleLocation itemLocation)
    {
        // 防重复调用检查
        float currentTime = Time.time;
        if (isProcessingSelection || (currentTime - lastSelectionTime < SELECTION_COOLDOWN))
        {
            LogMessage($"ToggleItemSelection调用被阻止（防重复）: {item?.displayName}, 处理中: {isProcessingSelection}, 时间间隔: {currentTime - lastSelectionTime}", true);
            return false;
        }
        
        isProcessingSelection = true;
        lastSelectionTime = currentTime;
        
        LogMessage($"ToggleItemSelection调用: {item?.displayName}, 位置: {itemLocation}");
        
        bool result = false;
        try
        {
            if (selectedItems.Contains(item))
            {
                LogMessage("物品已选中，取消选择");
                result = DeselectItem(item);
            }
            else
            {
                LogMessage("物品未选中，添加选择");
                result = SelectItem(item, itemLocation);
            }
        }
        finally
        {
            isProcessingSelection = false;
        }
        
        return result;
    }
    
    /// <summary>
    /// 确定选择模式
    /// </summary>
    SelectionMode DetermineSelectionMode(SampleLocation itemLocation)
    {
        switch (itemLocation)
        {
            case SampleLocation.InInventory:
                return SelectionMode.BackpackSelection;
            case SampleLocation.InWarehouse:
                return SelectionMode.WarehouseSelection;
            default:
                LogMessage($"未知的物品位置: {itemLocation}", true);
                return SelectionMode.None;
        }
    }
    
    /// <summary>
    /// 检查选择模式是否兼容
    /// </summary>
    bool IsSelectionModeCompatible(SelectionMode requiredMode)
    {
        if (currentMode == SelectionMode.None || currentMode == SelectionMode.Ready)
        {
            return true; // 第一次选择，任何模式都可以
        }
        
        return currentMode == requiredMode;
    }
    
    /// <summary>
    /// 创建选择视觉反馈
    /// </summary>
    void CreateSelectionVisual(SampleItem item)
    {
        // 通知所有面板更新物品的选中状态
        NotifyItemSelectionChanged(item, true);
        
        selectedVisuals[item] = null; // 占位符
        
        LogMessage($"创建选择视觉反馈: {item.displayName}");
    }
    
    /// <summary>
    /// 移除选择视觉反馈
    /// </summary>
    void RemoveSelectionVisual(SampleItem item)
    {
        // 通知所有面板更新物品的选中状态
        NotifyItemSelectionChanged(item, false);
        
        if (selectedVisuals.ContainsKey(item))
        {
            GameObject visual = selectedVisuals[item];
            if (visual != null)
            {
                Destroy(visual);
            }
            selectedVisuals.Remove(item);
        }
        
        LogMessage($"移除选择视觉反馈: {item.displayName}");
    }
    
    /// <summary>
    /// 清空所有选择
    /// </summary>
    public void ClearAllSelections()
    {
        LogMessage($"清空所有选择，当前选择数量: {selectedItems.Count}");
        
        // 移除所有视觉反馈
        foreach (var item in selectedItems.ToList())
        {
            RemoveSelectionVisual(item);
        }
        
        // 清空列表
        selectedItems.Clear();
        selectedVisuals.Clear();
        
        // 强制清空所有槽位的选中状态（确保完全清空）
        ForceClearAllSlotSelections();
        
        // 额外安全措施：等待一帧后再次清空（确保UI完全更新）
        if (Application.isPlaying)
        {
            StartCoroutine(DelayedForceClear());
        }
        
        OnSelectionChanged?.Invoke(new List<SampleItem>());
    }
    
    /// <summary>
    /// 检查物品是否被选中
    /// </summary>
    public bool IsItemSelected(SampleItem item)
    {
        return selectedItems.Contains(item);
    }
    
    /// <summary>
    /// 获取选中的物品列表
    /// </summary>
    public List<SampleItem> GetSelectedItems()
    {
        return new List<SampleItem>(selectedItems);
    }
    
    /// <summary>
    /// 获取选中物品数量
    /// </summary>
    public int GetSelectedCount()
    {
        return selectedItems.Count;
    }
    
    /// <summary>
    /// 获取当前选择模式
    /// </summary>
    public SelectionMode GetCurrentSelectionMode()
    {
        return currentMode;
    }
    
    /// <summary>
    /// 检查是否在多选模式中
    /// </summary>
    public bool IsInMultiSelectMode()
    {
        return currentMode != SelectionMode.None;
    }
    
    /// <summary>
    /// 检查是否可以选择更多物品
    /// </summary>
    public bool CanSelectMore()
    {
        return selectedItems.Count < maxSelectCount;
    }
    
    /// <summary>
    /// 获取选择统计信息
    /// </summary>
    public string GetSelectionStats()
    {
        var stats = $"=== 多选系统状态 ===\n";
        stats += $"当前模式: {currentMode}\n";
        stats += $"选中物品数: {selectedItems.Count}/{maxSelectCount}\n";
        
        if (selectedItems.Count > 0)
        {
            stats += $"选中物品列表:\n";
            for (int i = 0; i < selectedItems.Count; i++)
            {
                stats += $"  {i + 1}. {selectedItems[i].displayName} ({selectedItems[i].sampleID})\n";
            }
        }
        
        return stats;
    }
    
    /// <summary>
    /// 按物品类型选择
    /// </summary>
    public int SelectItemsByType(string toolID, SampleLocation location)
    {
        if (!IsInMultiSelectMode())
        {
            LogMessage("必须先进入多选模式", true);
            return 0;
        }
        
        SelectionMode requiredMode = DetermineSelectionMode(location);
        if (!IsSelectionModeCompatible(requiredMode))
        {
            LogMessage($"选择模式不兼容: {currentMode} vs {requiredMode}", true);
            return 0;
        }
        
        // 如果是Ready状态，设置为具体的选择模式
        if (currentMode == SelectionMode.Ready)
        {
            currentMode = requiredMode;
            OnSelectionModeChanged?.Invoke(currentMode);
            LogMessage($"从Ready状态切换到选择模式: {currentMode}");
        }
        
        List<SampleItem> itemsToSelect = new List<SampleItem>();
        
        // 根据位置获取物品列表
        if (location == SampleLocation.InInventory)
        {
            var inventory = SampleInventory.Instance;
            if (inventory != null)
            {
                var inventoryItems = inventory.GetInventorySamples();
                if (string.IsNullOrEmpty(toolID))
                {
                    // 选择所有物品
                    itemsToSelect = inventoryItems.Where(item => !selectedItems.Contains(item)).ToList();
                }
                else
                {
                    // 选择指定类型的物品
                    itemsToSelect = inventoryItems.Where(item => item.sourceToolID == toolID && !selectedItems.Contains(item)).ToList();
                }
            }
        }
        else if (location == SampleLocation.InWarehouse)
        {
            var warehouse = WarehouseManager.Instance;
            if (warehouse != null)
            {
                var warehouseItems = warehouse.Storage.GetAllItems();
                if (string.IsNullOrEmpty(toolID))
                {
                    // 选择所有物品
                    itemsToSelect = warehouseItems.Where(item => !selectedItems.Contains(item)).ToList();
                }
                else
                {
                    // 选择指定类型的物品
                    itemsToSelect = warehouseItems.Where(item => item.sourceToolID == toolID && !selectedItems.Contains(item)).ToList();
                }
            }
        }
        
        // 批量选择
        int selectedCount = 0;
        foreach (var item in itemsToSelect)
        {
            if (SelectItem(item, location))
            {
                selectedCount++;
            }
            
            if (!CanSelectMore())
            {
                break;
            }
        }
        
        LogMessage($"按类型选择完成：{selectedCount} 个物品 (工具ID: {toolID})");
        return selectedCount;
    }
    
    /// <summary>
    /// 全选当前模式的所有物品
    /// </summary>
    public int SelectAll(SampleLocation location)
    {
        if (!IsInMultiSelectMode())
        {
            LogMessage("必须先进入多选模式", true);
            return 0;
        }
        
        SelectionMode requiredMode = DetermineSelectionMode(location);
        if (!IsSelectionModeCompatible(requiredMode))
        {
            LogMessage($"选择模式不兼容: {currentMode} vs {requiredMode}", true);
            return 0;
        }
        
        // 如果是Ready状态，设置为具体的选择模式
        if (currentMode == SelectionMode.Ready)
        {
            currentMode = requiredMode;
            OnSelectionModeChanged?.Invoke(currentMode);
            LogMessage($"从Ready状态切换到选择模式: {currentMode}");
        }
        
        return SelectItemsByType("", location); // 空字符串表示所有类型
    }
    
    /// <summary>
    /// 日志输出
    /// </summary>
    void LogMessage(string message, bool isWarning = false)
    {
        if (enableDebugLog)
        {
            string logMessage = $"[MultiSelectSystem] {message}";
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
    /// 在Inspector中显示选择状态
    /// </summary>
    [ContextMenu("显示选择状态")]
    void ShowSelectionStatus()
    {
        Debug.Log(GetSelectionStats());
    }
    
    /// <summary>
    /// 测试选择功能
    /// </summary>
    [ContextMenu("测试多选功能")]
    void TestMultiSelect()
    {
        LogMessage("开始测试多选功能");
        
        EnterMultiSelectMode();
        
        // 这里可以添加测试逻辑
        LogMessage("多选功能测试完成");
    }
    
    /// <summary>
    /// 通知物品选择状态变化 - 让所有相关的槽位更新视觉反馈
    /// </summary>
    void NotifyItemSelectionChanged(SampleItem item, bool isSelected)
    {
        if (item == null) return;
        
        Debug.Log($"[MultiSelectSystem] 通知物品选择状态变化: {item.displayName}, 选中: {isSelected}");
        
        // 查找所有的仓库物品槽位并更新它们的选中状态
        WarehouseItemSlot[] allSlots = FindObjectsByType<WarehouseItemSlot>(FindObjectsSortMode.None);
        Debug.Log($"[MultiSelectSystem] 找到 {allSlots.Length} 个槽位");
        
        int updatedSlots = 0;
        foreach (var slot in allSlots)
        {
            if (slot.HasItem() && slot.GetItem() == item)
            {
                Debug.Log($"[MultiSelectSystem] 找到匹配槽位: {slot.GetItemName()}, 准备设置选中状态: {isSelected}");
                
                // 直接同步更新，避免延迟带来的问题
                if (slot.IsSelected() != isSelected)
                {
                    slot.SetSelected(isSelected);
                    updatedSlots++;
                    Debug.Log($"[MultiSelectSystem] 直接更新槽位选中状态: {slot.GetItemName()} -> {isSelected}");
                }
                else
                {
                    Debug.Log($"[MultiSelectSystem] 槽位状态已经正确，跳过: {slot.GetItemName()} = {isSelected}");
                }
            }
        }
        
        if (updatedSlots == 0)
        {
            Debug.LogWarning($"[MultiSelectSystem] 没有更新任何槽位状态");
        }
        else
        {
            Debug.Log($"[MultiSelectSystem] 成功更新了 {updatedSlots} 个槽位的选中状态");
        }
    }
    
    /// <summary>
    /// 延迟强制清空所有选择（确保UI完全更新）
    /// </summary>
    System.Collections.IEnumerator DelayedForceClear()
    {
        yield return new WaitForEndOfFrame();
        
        // 再次强制清空所有槽位的选中状态
        ForceClearAllSlotSelections();
        
        LogMessage("延迟强制清空完成");
    }
    
    /// <summary>
    /// 延迟更新槽位选中状态
    /// </summary>
    System.Collections.IEnumerator UpdateSlotSelectionDelayed(WarehouseItemSlot slot, bool isSelected)
    {
        yield return new WaitForEndOfFrame();
        
        if (slot != null && slot.gameObject != null)
        {
            Debug.Log($"[MultiSelectSystem] 执行延迟槽位选中状态更新: {slot.GetItemName()}, 选中: {isSelected}");
            
            // 检查当前状态是否已经正确
            if (slot.IsSelected() != isSelected)
            {
                slot.SetSelected(isSelected);
                Debug.Log($"[MultiSelectSystem] 槽位状态已更新: {slot.GetItemName()} -> {isSelected}");
            }
            else
            {
                Debug.Log($"[MultiSelectSystem] 槽位状态已经正确，跳过更新: {slot.GetItemName()} = {isSelected}");
            }
        }
    }
    
    /// <summary>
    /// 强制清空所有槽位的选中状态
    /// </summary>
    void ForceClearAllSlotSelections()
    {
        WarehouseItemSlot[] allSlots = FindObjectsByType<WarehouseItemSlot>(FindObjectsSortMode.None);
        
        foreach (var slot in allSlots)
        {
            if (slot.IsSelected())
            {
                slot.SetSelected(false);
            }
        }
        
        LogMessage($"强制清空了所有槽位的选中状态");
    }
}
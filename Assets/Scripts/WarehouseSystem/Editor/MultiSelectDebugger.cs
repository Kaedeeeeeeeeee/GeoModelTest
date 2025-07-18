using UnityEngine;
using UnityEditor;

/// <summary>
/// 多选系统调试器 - 专门用于调试多选和视觉反馈问题
/// </summary>
public class MultiSelectDebugger : EditorWindow
{
    [MenuItem("Tools/多选系统调试器")]
    public static void ShowWindow()
    {
        GetWindow<MultiSelectDebugger>("多选系统调试器");
    }
    
    void OnGUI()
    {
        GUILayout.Label("多选系统调试器", EditorStyles.boldLabel);
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("1. 检查多选系统状态"))
        {
            CheckMultiSelectSystemStatus();
        }
        
        if (GUILayout.Button("2. 检查物品槽位状态"))
        {
            CheckWarehouseItemSlots();
        }
        
        if (GUILayout.Button("3. 强制进入多选模式"))
        {
            ForceEnterMultiSelectMode();
        }
        
        if (GUILayout.Button("4. 强制选择第一个物品"))
        {
            ForceSelectFirstItem();
        }
        
        if (GUILayout.Button("5. 检查批量传输按钮"))
        {
            CheckBatchTransferButton();
        }
        
        if (GUILayout.Button("6. 模拟完整多选流程"))
        {
            SimulateCompleteMultiSelectFlow();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("清空Console日志"))
        {
            var logEntries = System.Type.GetType("UnityEditor.LogEntries,UnityEditor.dll");
            var clearMethod = logEntries.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            clearMethod.Invoke(null, null);
        }
    }
    
    void CheckMultiSelectSystemStatus()
    {
        MultiSelectSystem multiSelect = FindObjectOfType<MultiSelectSystem>();
        if (multiSelect != null)
        {
            Debug.Log("=== 多选系统状态检查 ===");
            Debug.Log($"当前模式: {multiSelect.GetCurrentSelectionMode()}");
            Debug.Log($"是否在多选模式: {multiSelect.IsInMultiSelectMode()}");
            Debug.Log($"选中物品数量: {multiSelect.GetSelectedCount()}");
            Debug.Log($"调试日志开启: {multiSelect.enableDebugLog}");
            
            var selectedItems = multiSelect.GetSelectedItems();
            if (selectedItems.Count > 0)
            {
                Debug.Log("选中的物品:");
                foreach (var item in selectedItems)
                {
                    Debug.Log($"  - {item.displayName} ({item.sampleID})");
                }
            }
        }
        else
        {
            Debug.LogError("未找到MultiSelectSystem组件!");
        }
    }
    
    void CheckWarehouseItemSlots()
    {
        WarehouseItemSlot[] slots = FindObjectsOfType<WarehouseItemSlot>();
        Debug.Log($"=== 物品槽位状态检查 ===");
        Debug.Log($"找到 {slots.Length} 个物品槽位");
        
        int slotsWithItems = 0;
        int selectedSlots = 0;
        
        foreach (var slot in slots)
        {
            if (slot.HasItem())
            {
                slotsWithItems++;
                Debug.Log($"槽位有物品: {slot.GetItemName()}, 选中状态: {slot.IsSelected()}");
                
                if (slot.IsSelected())
                {
                    selectedSlots++;
                }
            }
        }
        
        Debug.Log($"有物品的槽位: {slotsWithItems}");
        Debug.Log($"选中的槽位: {selectedSlots}");
    }
    
    void ForceEnterMultiSelectMode()
    {
        MultiSelectSystem multiSelect = FindObjectOfType<MultiSelectSystem>();
        if (multiSelect != null)
        {
            Debug.Log("=== 强制进入多选模式 ===");
            multiSelect.EnterMultiSelectMode();
            Debug.Log($"进入后的模式: {multiSelect.GetCurrentSelectionMode()}");
        }
        else
        {
            Debug.LogError("未找到MultiSelectSystem组件!");
        }
    }
    
    void ForceSelectFirstItem()
    {
        MultiSelectSystem multiSelect = FindObjectOfType<MultiSelectSystem>();
        WarehouseItemSlot[] slots = FindObjectsOfType<WarehouseItemSlot>();
        
        if (multiSelect == null)
        {
            Debug.LogError("未找到MultiSelectSystem组件!");
            return;
        }
        
        // 确保在多选模式中
        if (!multiSelect.IsInMultiSelectMode())
        {
            multiSelect.EnterMultiSelectMode();
        }
        
        // 找到第一个有物品的槽位
        WarehouseItemSlot firstSlotWithItem = null;
        foreach (var slot in slots)
        {
            if (slot.HasItem())
            {
                firstSlotWithItem = slot;
                break;
            }
        }
        
        if (firstSlotWithItem != null)
        {
            var item = firstSlotWithItem.GetItem();
            Debug.Log($"=== 强制选择第一个物品 ===");
            Debug.Log($"目标物品: {item.displayName}");
            
            // 直接调用多选系统的选择方法
            bool success = multiSelect.SelectItem(item, SampleLocation.InInventory);
            Debug.Log($"选择结果: {success}");
            Debug.Log($"选择后的模式: {multiSelect.GetCurrentSelectionMode()}");
            Debug.Log($"选中物品数量: {multiSelect.GetSelectedCount()}");
        }
        else
        {
            Debug.LogWarning("没有找到有物品的槽位");
        }
    }
    
    void CheckBatchTransferButton()
    {
        WarehouseUI warehouseUI = FindObjectOfType<WarehouseUI>();
        if (warehouseUI == null)
        {
            Debug.LogError("未找到WarehouseUI组件!");
            return;
        }
        
        Debug.Log("=== 批量传输按钮检查 ===");
        
        // 使用反射获取私有字段
        var buttonField = typeof(WarehouseUI).GetField("batchTransferButton", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var textField = typeof(WarehouseUI).GetField("batchTransferButtonText", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (buttonField != null && textField != null)
        {
            UnityEngine.UI.Button button = (UnityEngine.UI.Button)buttonField.GetValue(warehouseUI);
            UnityEngine.UI.Text text = (UnityEngine.UI.Text)textField.GetValue(warehouseUI);
            
            if (button != null && text != null)
            {
                Debug.Log($"按钮存在: {button != null}");
                Debug.Log($"按钮激活: {button.gameObject.activeInHierarchy}");
                Debug.Log($"按钮可交互: {button.interactable}");
                Debug.Log($"按钮文本: '{text.text}'");
                Debug.Log($"按钮颜色: {button.image.color}");
            }
            else
            {
                Debug.LogError("按钮或文本组件为null");
            }
        }
        else
        {
            Debug.LogError("无法通过反射获取按钮字段");
        }
    }
    
    void SimulateCompleteMultiSelectFlow()
    {
        Debug.Log("=== 模拟完整多选流程 ===");
        
        // 1. 进入多选模式
        ForceEnterMultiSelectMode();
        
        // 等待一帧
        EditorApplication.delayCall += () => {
            // 2. 选择物品
            ForceSelectFirstItem();
            
            // 等待另一帧
            EditorApplication.delayCall += () => {
                // 3. 检查结果
                CheckMultiSelectSystemStatus();
                CheckWarehouseItemSlots();
                CheckBatchTransferButton();
            };
        };
    }
}
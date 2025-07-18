using UnityEngine;
using UnityEditor;

/// <summary>
/// 仓库系统测试工具 - Editor工具
/// </summary>
public class WarehouseTestTool : EditorWindow
{
    [MenuItem("Tools/仓库系统测试")]
    public static void ShowWindow()
    {
        GetWindow<WarehouseTestTool>("仓库系统测试");
    }
    
    void OnGUI()
    {
        GUILayout.Label("仓库系统测试工具", EditorStyles.boldLabel);
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("测试多选系统"))
        {
            TestMultiSelectSystem();
        }
        
        if (GUILayout.Button("验证视觉反馈"))
        {
            TestVisualFeedback();
        }
        
        if (GUILayout.Button("检查批量传输按钮"))
        {
            TestBatchTransferButton();
        }
        
        if (GUILayout.Button("模拟物品选择"))
        {
            SimulateItemSelection();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("显示系统状态"))
        {
            ShowSystemStatus();
        }
    }
    
    void TestMultiSelectSystem()
    {
        MultiSelectSystem multiSelect = FindObjectOfType<MultiSelectSystem>();
        if (multiSelect != null)
        {
            Debug.Log("=== 多选系统测试 ===");
            Debug.Log($"当前模式: {multiSelect.GetCurrentSelectionMode()}");
            Debug.Log($"选中物品数: {multiSelect.GetSelectedCount()}");
            Debug.Log($"是否在多选模式: {multiSelect.IsInMultiSelectMode()}");
            Debug.Log("多选系统测试完成");
        }
        else
        {
            Debug.LogError("未找到MultiSelectSystem组件");
        }
    }
    
    void TestVisualFeedback()
    {
        WarehouseItemSlot[] slots = FindObjectsOfType<WarehouseItemSlot>();
        Debug.Log($"=== 视觉反馈测试 ===");
        Debug.Log($"找到 {slots.Length} 个物品槽位");
        
        int slotsWithItems = 0;
        int selectedSlots = 0;
        
        foreach (var slot in slots)
        {
            if (slot.HasItem())
            {
                slotsWithItems++;
                if (slot.IsSelected())
                {
                    selectedSlots++;
                }
            }
        }
        
        Debug.Log($"有物品的槽位: {slotsWithItems}");
        Debug.Log($"选中的槽位: {selectedSlots}");
        Debug.Log("视觉反馈测试完成");
    }
    
    void TestBatchTransferButton()
    {
        WarehouseUI warehouseUI = FindObjectOfType<WarehouseUI>();
        if (warehouseUI != null)
        {
            Debug.Log("=== 批量传输按钮测试 ===");
            Debug.Log($"仓库是否打开: {warehouseUI.IsWarehouseOpen()}");
            
            // 获取私有字段进行检查
            var batchTransferButtonField = typeof(WarehouseUI).GetField("batchTransferButton", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (batchTransferButtonField != null)
            {
                UnityEngine.UI.Button button = (UnityEngine.UI.Button)batchTransferButtonField.GetValue(warehouseUI);
                if (button != null)
                {
                    Debug.Log($"批量传输按钮存在: {button.gameObject.activeInHierarchy}");
                    Debug.Log($"按钮可交互: {button.interactable}");
                    
                    var textComponent = button.GetComponentInChildren<UnityEngine.UI.Text>();
                    if (textComponent != null)
                    {
                        Debug.Log($"按钮文本: {textComponent.text}");
                    }
                }
                else
                {
                    Debug.LogError("批量传输按钮为null");
                }
            }
            
            Debug.Log("批量传输按钮测试完成");
        }
        else
        {
            Debug.LogError("未找到WarehouseUI组件");
        }
    }
    
    void SimulateItemSelection()
    {
        Debug.Log("=== 模拟物品选择 ===");
        
        WarehouseItemSlot[] slots = FindObjectsOfType<WarehouseItemSlot>();
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
            Debug.Log($"找到有物品的槽位: {firstSlotWithItem.GetItemName()}");
            
            // 模拟选中状态
            firstSlotWithItem.SetSelected(true);
            Debug.Log("已模拟设置槽位为选中状态");
            
            // 等待1秒后取消选中
            EditorApplication.delayCall += () => {
                if (firstSlotWithItem != null)
                {
                    firstSlotWithItem.SetSelected(false);
                    Debug.Log("已取消槽位选中状态");
                }
            };
        }
        else
        {
            Debug.LogWarning("未找到有物品的槽位");
        }
        
        Debug.Log("模拟物品选择完成");
    }
    
    void ShowSystemStatus()
    {
        Debug.Log("=== 仓库系统状态 ===");
        
        // MultiSelectSystem状态
        MultiSelectSystem multiSelect = FindObjectOfType<MultiSelectSystem>();
        if (multiSelect != null)
        {
            Debug.Log(multiSelect.GetSelectionStats());
        }
        
        // WarehouseManager状态
        if (WarehouseManager.Instance != null)
        {
            Debug.Log(WarehouseManager.Instance.GetWarehouseStats());
        }
        
        // WarehouseUI状态
        WarehouseUI warehouseUI = FindObjectOfType<WarehouseUI>();
        if (warehouseUI != null)
        {
            Debug.Log($"仓库UI状态: {(warehouseUI.IsWarehouseOpen() ? "打开" : "关闭")}");
        }
        
        Debug.Log("系统状态显示完成");
    }
}
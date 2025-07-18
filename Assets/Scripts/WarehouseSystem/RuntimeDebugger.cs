using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// 运行时调试器 - 在游戏运行时按键调试多选系统
/// </summary>
public class RuntimeDebugger : MonoBehaviour
{
    [Header("调试快捷键")]
    public KeyCode debugKey = KeyCode.Alpha0;      // 数字0: 完整系统检查
    
    void Update()
    {
        if (Keyboard.current != null)
        {
            // 数字0键: 完整系统检查
            if (Keyboard.current.digit0Key.wasPressedThisFrame)
            {
                Debug.Log("=== 运行时系统检查 (数字0) ===");
                CheckCompleteSystem();
            }
        }
    }
    
    void CheckCompleteSystem()
    {
        // 检查MultiSelectSystem
        MultiSelectSystem multiSelect = FindFirstObjectByType<MultiSelectSystem>();
        Debug.Log($"MultiSelectSystem 存在: {multiSelect != null}");
        if (multiSelect != null)
        {
            Debug.Log($"当前模式: {multiSelect.GetCurrentSelectionMode()}");
            Debug.Log($"选中数量: {multiSelect.GetSelectedCount()}");
        }
        
        // 检查WarehouseUI
        WarehouseUI warehouseUI = FindFirstObjectByType<WarehouseUI>();
        Debug.Log($"WarehouseUI 存在: {warehouseUI != null}");
        if (warehouseUI != null)
        {
            Debug.Log($"仓库是否打开: {warehouseUI.IsWarehouseOpen()}");
        }
        
        // 检查槽位数量
        WarehouseItemSlot[] slots = FindObjectsByType<WarehouseItemSlot>(FindObjectsSortMode.None);
        Debug.Log($"找到槽位数量: {slots.Length}");
        
        // 检查SampleInventory
        SampleInventory inventory = SampleInventory.Instance;
        Debug.Log($"SampleInventory 存在: {inventory != null}");
        if (inventory != null)
        {
            var samples = inventory.GetInventorySamples();
            Debug.Log($"背包中样本数量: {samples.Count}");
            foreach (var sample in samples)
            {
                Debug.Log($"  样本: {sample.displayName} ({sample.sampleID})");
            }
        }
    }
    
    void OnGUI()
    {
        // 在屏幕上显示调试信息
        GUILayout.BeginArea(new Rect(10, 10, 300, 150));
        GUILayout.Box("运行时调试器");
        GUILayout.Label("数字0: 完整系统检查");
        
        // 显示当前状态
        MultiSelectSystem multiSelect = FindFirstObjectByType<MultiSelectSystem>();
        if (multiSelect != null)
        {
            GUILayout.Label($"多选模式: {multiSelect.GetCurrentSelectionMode()}");
            GUILayout.Label($"选中数量: {multiSelect.GetSelectedCount()}");
        }
        
        WarehouseItemSlot[] slots = FindObjectsByType<WarehouseItemSlot>(FindObjectsSortMode.None);
        GUILayout.Label($"槽位数量: {slots.Length}");
        
        GUILayout.EndArea();
    }
}
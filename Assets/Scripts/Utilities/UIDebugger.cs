using UnityEngine;
using UnityEngine.InputSystem;

public class UIDebugger : MonoBehaviour
{
    void Update()
    {
        if (Keyboard.current.f1Key.wasPressedThisFrame)
        {
            DebugUIState();
        }
    }
    
    void DebugUIState()
    {
        Debug.Log("=== UI调试信息 ===");
        
        // 查找所有Canvas
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        Debug.Log($"找到 {canvases.Length} 个Canvas:");
        foreach (var canvas in canvases)
        {
            Debug.Log($"  - Canvas: {canvas.name}");
        }
        
        // 查找所有InventoryUISystem
        InventoryUISystem[] inventorySystems = FindObjectsOfType<InventoryUISystem>();
        Debug.Log($"找到 {inventorySystems.Length} 个InventoryUISystem:");
        foreach (var system in inventorySystems)
        {
            Debug.Log($"  - InventoryUISystem在: {system.gameObject.name}");
            Debug.Log($"    wheelUI: {(system.wheelUI != null ? system.wheelUI.name : "null")}");
            Debug.Log($"    wheelSlots数量: {(system.wheelSlots != null ? system.wheelSlots.Length : 0)}");
        }
        
        // 查找所有包含"Slot"的对象
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int slotCount = 0;
        foreach (var obj in allObjects)
        {
            if (obj.name.Contains("Slot"))
            {
                slotCount++;
                Debug.Log($"  - 找到Slot对象: {obj.name} 在 {(obj.transform.parent ? obj.transform.parent.name : "根")}");
            }
        }
        Debug.Log($"总共找到 {slotCount} 个Slot对象");
    }
}
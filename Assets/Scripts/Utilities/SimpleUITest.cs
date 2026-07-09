using UnityEngine;

public class SimpleUITest : MonoBehaviour
{
    void Update()
    {
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard == null) return;

        // F3键：简单的UI状态检查
        if (keyboard.f3Key.wasPressedThisFrame)
        {
            TestUIState();
        }
    }
    
    void TestUIState()
    {
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        Debug.Log("=== 简单UI测试 ===");
        
        // 查找InventoryUISystem
        InventoryUISystem[] systems = FindObjectsOfType<InventoryUISystem>();
        Debug.Log($"找到 {systems.Length} 个InventoryUISystem");
        
        foreach (var system in systems)
        {
            Debug.Log($"InventoryUISystem在: {system.gameObject.name}");
            if (system.wheelUI != null)
            {
                Debug.Log($"  wheelUI: {system.wheelUI.name}");
                Debug.Log($"  wheelUI是否激活: {system.wheelUI.activeInHierarchy}");
                
                // 强制显示轮盘测试
                if (keyboard != null && keyboard.f4Key.wasPressedThisFrame)
                {
                    system.wheelUI.SetActive(true);
                    Debug.Log("强制显示轮盘UI");
                }
            }
            else
            {
                Debug.Log("  wheelUI: null");
            }
        }
    }
}

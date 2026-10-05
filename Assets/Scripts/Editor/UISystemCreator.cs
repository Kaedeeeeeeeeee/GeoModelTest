using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class UISystemCreator : MonoBehaviour
{
    [MenuItem("Tools/创建UI系统")]
    static void CreateUISystem()
    {
        CreateUISystemInScene();
    }
    
    [MenuItem("Tools/删除所有UI系统")]
    static void DestroyAllUISystems()
    {
        // 删除所有Canvas
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        foreach (var canvas in canvases)
        {
            if (canvas.name.Contains("Collection") || canvas.name.Contains("Inventory"))
            {
                Debug.Log($"删除Canvas: {canvas.name}");
                DestroyImmediate(canvas.gameObject);
            }
        }
        
        // 删除所有InventoryUISystem
        InventoryUISystem[] systems = FindObjectsOfType<InventoryUISystem>();
        foreach (var system in systems)
        {
            Debug.Log($"删除InventoryUISystem: {system.gameObject.name}");
            DestroyImmediate(system.gameObject);
        }
        
        Debug.Log("所有UI系统已删除");
    }
    
    [MenuItem("Tools/修复UI大小")]
    static void FixUISize()
    {
        foreach (var system in FindObjectsByType<InventoryUISystem>(FindObjectsSortMode.None))
            system.RebuildWheelUI();
        Debug.Log("轮盘已按可见道具数重建等分扇区，大小不超过屏幕短边的86%。");
    }

    [MenuItem("Tools/修复EventSystem")]
    static void FixEventSystem()
    {
        // 删除所有旧的EventSystem
        UnityEngine.EventSystems.EventSystem[] eventSystems = FindObjectsOfType<UnityEngine.EventSystems.EventSystem>();
        foreach (var es in eventSystems)
        {
            Debug.Log($"删除旧的EventSystem: {es.name}");
            DestroyImmediate(es.gameObject);
        }
        
        // 创建新的兼容Input System的EventSystem
        GameObject eventSystemObj = new GameObject("EventSystem");
        eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        Debug.Log("创建了新的兼容Input System的EventSystem");
    }
    
    [MenuItem("Tools/调试UI状态")]
    static void DebugUIState()
    {
        Debug.Log("=== 编辑器UI调试信息 ===");
        
        // 检查EventSystem
        UnityEngine.EventSystems.EventSystem[] eventSystems = FindObjectsOfType<UnityEngine.EventSystems.EventSystem>();
        Debug.Log($"找到 {eventSystems.Length} 个EventSystem:");
        foreach (var es in eventSystems)
        {
            Debug.Log($"  - EventSystem: {es.name}");
            var inputModules = es.GetComponents<UnityEngine.EventSystems.BaseInputModule>();
            foreach (var module in inputModules)
            {
                Debug.Log($"    InputModule: {module.GetType().Name}");
            }
        }
        
        // 查找所有Canvas
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        Debug.Log($"找到 {canvases.Length} 个Canvas:");
        foreach (var canvas in canvases)
        {
            Debug.Log($"  - Canvas: {canvas.name}");
            InventoryUISystem inventoryUI = canvas.GetComponent<InventoryUISystem>();
            if (inventoryUI != null)
            {
                Debug.Log($"    包含InventoryUISystem");
                Debug.Log($"    wheelUI: {(inventoryUI.wheelUI != null ? inventoryUI.wheelUI.name : "null")}");
                Debug.Log($"    wheelSlots: {(inventoryUI.wheelSlots != null ? inventoryUI.wheelSlots.Length : 0)}");
            }
        }
        
        // 查找所有InventoryUISystem
        InventoryUISystem[] inventorySystems = FindObjectsOfType<InventoryUISystem>();
        Debug.Log($"独立的InventoryUISystem: {inventorySystems.Length}");
        
        // 查找所有包含"Slot"的对象
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int slotCount = 0;
        foreach (var obj in allObjects)
        {
            if (obj.name.Contains("Slot"))
            {
                slotCount++;
                Debug.Log($"  - Slot对象: {obj.name} (父对象: {(obj.transform.parent ? obj.transform.parent.name : "根")})");
            }
        }
        Debug.Log($"总共找到 {slotCount} 个Slot对象");
    }
    
    static void CreateUISystemInScene()
    {
        Debug.Log("开始在编辑器中创建UI系统...");
        
        // 1. 创建Canvas
        GameObject canvasObj = new GameObject("Collection UI Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        
        // 添加CanvasScaler
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // 2. 确保有EventSystem（修复Input System兼容性）
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            
            // 使用InputSystemUIInputModule替代StandaloneInputModule
            var inputModule = eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            Debug.Log("创建了EventSystem with InputSystemUIInputModule");
        }
        
        // 3. 添加InventoryUISystem并手动创建UI
        InventoryUISystem inventoryUI = canvasObj.AddComponent<InventoryUISystem>();
        
        // 4. 手动创建轮盘UI
        CreateWheelUIManually(inventoryUI);
        
        Debug.Log("UI系统创建完成！");
        Debug.Log("- Canvas: " + canvasObj.name);
        Debug.Log("- InventoryUISystem已添加");
        Debug.Log("- 圆形轮盘UI已创建");
        Debug.Log("运行游戏后按Tab键测试");
        
        // 选中创建的Canvas
        Selection.activeGameObject = canvasObj;
    }
    
    static void CreateWheelUIManually(InventoryUISystem inventoryUI)
    {
        inventoryUI.RebuildWheelUI();
        Debug.Log("等分扇形轮盘已创建，运行时随可见道具数更新。");
    }
}

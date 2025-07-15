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
        InventoryUISystem[] systems = FindObjectsOfType<InventoryUISystem>();
        foreach (var system in systems)
        {
            if (system.wheelUI != null)
            {
                RectTransform wheelRect = system.wheelUI.GetComponent<RectTransform>();
                if (wheelRect != null)
                {
                    // 使用80%屏幕大小
                    float screenSize = Mathf.Min(Screen.width, Screen.height);
                    float wheelSize = screenSize * 0.8f;
                    
                    wheelRect.sizeDelta = new Vector2(wheelSize, wheelSize);
                    wheelRect.anchorMin = new Vector2(0.5f, 0.5f);
                    wheelRect.anchorMax = new Vector2(0.5f, 0.5f);
                    wheelRect.pivot = new Vector2(0.5f, 0.5f);
                    wheelRect.anchoredPosition = Vector2.zero;
                    
                    // 手动重新计算slot位置
                    FixSlotPositions(system, wheelSize);
                    
                    Debug.Log($"已修复 {system.gameObject.name} 的轮盘大小为: {wheelSize}x{wheelSize}");
                }
            }
        }
        Debug.Log("UI大小修复完成 - 使用80%屏幕大小，slot已重新定位");
    }
    
    static void FixSlotPositions(InventoryUISystem system, float wheelSize)
    {
        if (system.wheelSlots == null) return;
        
        // 使用与InventoryUISystem相同的计算逻辑
        float slotSize = wheelSize * 0.08f;
        float slotRadius = (wheelSize * 0.5f) - (slotSize * 2.5f);
        
        for (int i = 0; i < system.wheelSlots.Length; i++)
        {
            if (system.wheelSlots[i] != null)
            {
                // 计算圆形位置：从顶部开始，顺时针排列
                float angle = i * 45f * Mathf.Deg2Rad;
                Vector2 slotPos = new Vector2(Mathf.Sin(angle) * slotRadius, Mathf.Cos(angle) * slotRadius);
                system.wheelSlots[i].anchoredPosition = slotPos;
                system.wheelSlots[i].sizeDelta = new Vector2(slotSize, slotSize);
            }
        }
        
        Debug.Log($"重新定位了 {system.wheelSlots.Length} 个slot，半径: {slotRadius}, 大小: {slotSize}");
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
        // 创建圆形轮盘背景
        GameObject wheelBG = new GameObject("WheelBackground");
        wheelBG.transform.SetParent(inventoryUI.transform);
        
        RectTransform bgRect = wheelBG.AddComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(300, 300);
        bgRect.anchorMin = new Vector2(0.5f, 0.5f);
        bgRect.anchorMax = new Vector2(0.5f, 0.5f);
        bgRect.pivot = new Vector2(0.5f, 0.5f);
        bgRect.anchoredPosition = Vector2.zero;
        
        // 创建圆形背景图像
        Image bgImage = wheelBG.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        
        // 初始化数组
        RectTransform[] wheelSlots = new RectTransform[8];
        Image[] slotImages = new Image[8];
        Text[] slotTexts = new Text[8];
        
        // 创建8个轮盘槽位（圆形排列）
        for (int i = 0; i < 8; i++)
        {
            // 创建槽位容器
            GameObject slot = new GameObject($"Slot_{i}");
            slot.transform.SetParent(wheelBG.transform);
            
            RectTransform slotRect = slot.AddComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(60, 60);
            slotRect.anchorMin = new Vector2(0.5f, 0.5f);
            slotRect.anchorMax = new Vector2(0.5f, 0.5f);
            slotRect.pivot = new Vector2(0.5f, 0.5f);
            
            // 计算圆形位置
            float angle = i * 45f * Mathf.Deg2Rad;
            float radius = 120f; // 圆形半径
            Vector2 slotPos = new Vector2(Mathf.Sin(angle) * radius, Mathf.Cos(angle) * radius);
            slotRect.anchoredPosition = slotPos;
            
            // 添加槽位背景
            Image slotBG = slot.AddComponent<Image>();
            slotBG.color = new Color(0.3f, 0.3f, 0.3f, 0.7f);
            
            // 创建工具图标
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(slot.transform);
            
            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(40, 40);
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            
            Image iconImage = iconObj.AddComponent<Image>();
            iconImage.color = Color.white;
            
            // 创建文本
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(slot.transform);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(80, 15);
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = new Vector2(0, -40);
            
            Text text = textObj.AddComponent<Text>();
            text.text = $"工具{i + 1}";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 10;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            
            // 保存引用
            wheelSlots[i] = slotRect;
            slotImages[i] = iconImage;
            slotTexts[i] = text;
        }
        
        // 设置InventoryUISystem的引用（通过反射）
        var wheelUIField = typeof(InventoryUISystem).GetField("wheelUI");
        var wheelSlotsField = typeof(InventoryUISystem).GetField("wheelSlots");
        var slotImagesField = typeof(InventoryUISystem).GetField("slotImages");
        var slotTextsField = typeof(InventoryUISystem).GetField("slotTexts");
        var wheelBackgroundField = typeof(InventoryUISystem).GetField("wheelBackground");
        var wheelCenterField = typeof(InventoryUISystem).GetField("wheelCenter");
        
        if (wheelUIField != null) wheelUIField.SetValue(inventoryUI, wheelBG);
        if (wheelSlotsField != null) wheelSlotsField.SetValue(inventoryUI, wheelSlots);
        if (slotImagesField != null) slotImagesField.SetValue(inventoryUI, slotImages);
        if (slotTextsField != null) slotTextsField.SetValue(inventoryUI, slotTexts);
        if (wheelBackgroundField != null) wheelBackgroundField.SetValue(inventoryUI, bgImage);
        if (wheelCenterField != null) wheelCenterField.SetValue(inventoryUI, wheelBG.transform);
        
        // 初始隐藏轮盘
        wheelBG.SetActive(false);
        
        Debug.Log("圆形轮盘UI创建完成 - 8个slot已按圆形排列");
    }
}
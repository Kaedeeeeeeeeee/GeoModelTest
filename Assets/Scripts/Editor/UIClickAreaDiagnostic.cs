using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// UI点击区域诊断工具 - 检查实际的屏幕坐标和点击区域
/// </summary>
public class UIClickAreaDiagnostic
{
    [MenuItem("Tools/研究室移动端UI/🎯 检查UI点击区域")]
    public static void DiagnoseUIClickAreas()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ 请在游戏运行时使用此工具");
            return;
        }

        Debug.Log("=== 🎯 UI点击区域诊断 ===");

        // 1. 检查所有WarehouseItemSlot的位置和区域
        CheckWarehouseSlotAreas();

        // 2. 检查Canvas层级和射线检测器
        CheckCanvasHierarchy();

        // 3. 测试多个位置的射线检测
        TestMultipleRaycastPositions();

        // 4. 检查UI元素的激活状态
        CheckUIElementStates();

        Debug.Log("=== 点击区域诊断完成 ===");
    }

    private static void CheckWarehouseSlotAreas()
    {
        Debug.Log("📊 检查仓库槽位的点击区域:");

        WarehouseItemSlot[] slots = Object.FindObjectsOfType<WarehouseItemSlot>();
        Debug.Log($"找到 {slots.Length} 个槽位");

        foreach (var slot in slots)
        {
            if (!slot.gameObject.activeInHierarchy) continue;

            RectTransform rectTransform = slot.GetComponent<RectTransform>();
            if (rectTransform == null) continue;

            // 获取世界坐标
            Vector3 worldPosition = rectTransform.position;

            // 转换到屏幕坐标
            Camera camera = Camera.main;
            if (camera == null)
            {
                // 寻找UI Camera
                Canvas canvas = slot.GetComponentInParent<Canvas>();
                if (canvas != null && canvas.worldCamera != null)
                {
                    camera = canvas.worldCamera;
                }
            }

            Vector2 screenPosition;
            if (camera != null)
            {
                screenPosition = RectTransformUtility.WorldToScreenPoint(camera, worldPosition);
            }
            else
            {
                screenPosition = worldPosition;
            }

            // 获取矩形大小
            Vector2 size = rectTransform.rect.size;
            Vector2 scale = rectTransform.lossyScale;
            Vector2 actualSize = new Vector2(size.x * scale.x, size.y * scale.y);

            Debug.Log($"槽位 {slot.name}:");
            Debug.Log($"  世界位置: {worldPosition}");
            Debug.Log($"  屏幕位置: {screenPosition}");
            Debug.Log($"  矩形大小: {size}");
            Debug.Log($"  实际大小: {actualSize}");
            Debug.Log($"  有物品: {slot.HasItem()}");

            // 检查Button组件
            Button button = slot.GetComponent<Button>();
            if (button == null)
                button = slot.GetComponentInChildren<Button>();

            if (button != null)
            {
                Debug.Log($"  Button可交互: {button.interactable}");
                Debug.Log($"  Button启用: {button.enabled}");
            }
            else
            {
                Debug.LogWarning($"  ⚠️ 槽位没有Button组件");
            }

            // 检查GraphicRaycaster
            Canvas parentCanvas = slot.GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                GraphicRaycaster raycaster = parentCanvas.GetComponent<GraphicRaycaster>();
                Debug.Log($"  Canvas: {parentCanvas.name}");
                Debug.Log($"  GraphicRaycaster: {(raycaster != null && raycaster.enabled ? "✅" : "❌")}");
                Debug.Log($"  Canvas排序: {parentCanvas.sortingOrder}");
            }

            Debug.Log("---");
        }
    }

    private static void CheckCanvasHierarchy()
    {
        Debug.Log("📊 检查Canvas层级结构:");

        Canvas[] canvases = Object.FindObjectsOfType<Canvas>();
        System.Array.Sort(canvases, (a, b) => a.sortingOrder.CompareTo(b.sortingOrder));

        Debug.Log($"找到 {canvases.Length} 个Canvas (按排序层级排列):");

        foreach (var canvas in canvases)
        {
            Debug.Log($"Canvas: {canvas.name}");
            Debug.Log($"  排序层级: {canvas.sortingOrder}");
            Debug.Log($"  渲染模式: {canvas.renderMode}");
            Debug.Log($"  激活状态: {canvas.gameObject.activeInHierarchy}");
            Debug.Log($"  覆盖排序: {canvas.overrideSorting}");

            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
            {
                Debug.Log($"  GraphicRaycaster启用: {raycaster.enabled}");
                Debug.Log($"  阻挡对象: {raycaster.blockingObjects}");
                Debug.Log($"  阻挡蒙版: {raycaster.blockingMask.value}");
            }
            else
            {
                Debug.LogWarning($"  ⚠️ 没有GraphicRaycaster");
            }

            // 检查是否包含仓库UI
            if (canvas.name.Contains("Warehouse") || canvas.name.Contains("Mobile"))
            {
                Debug.Log($"  🏪 这是仓库相关Canvas");

                // 检查子对象中的WarehouseItemSlot
                WarehouseItemSlot[] childSlots = canvas.GetComponentsInChildren<WarehouseItemSlot>();
                Debug.Log($"  包含 {childSlots.Length} 个槽位");
            }

            Debug.Log("---");
        }
    }

    private static void TestMultipleRaycastPositions()
    {
        Debug.Log("🎯 测试多个位置的射线检测:");

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            Debug.LogError("❌ EventSystem不存在");
            return;
        }

        // 测试不同的屏幕位置
        Vector2[] testPositions = {
            Input.mousePosition,  // 当前鼠标位置
            new Vector2(Screen.width * 0.5f, Screen.height * 0.5f),  // 屏幕中心
            new Vector2(Screen.width * 0.2f, Screen.height * 0.5f),  // 左侧
            new Vector2(Screen.width * 0.8f, Screen.height * 0.5f),  // 右侧
            new Vector2(Screen.width * 0.5f, Screen.height * 0.3f),  // 下方
            new Vector2(Screen.width * 0.5f, Screen.height * 0.7f),  // 上方
        };

        string[] positionNames = {
            "当前鼠标位置",
            "屏幕中心",
            "左侧",
            "右侧",
            "下方",
            "上方"
        };

        for (int i = 0; i < testPositions.Length; i++)
        {
            Vector2 testPos = testPositions[i];
            string posName = positionNames[i];

            PointerEventData pointerData = new PointerEventData(eventSystem);
            pointerData.position = testPos;

            var raycastResults = new System.Collections.Generic.List<RaycastResult>();
            eventSystem.RaycastAll(pointerData, raycastResults);

            Debug.Log($"{posName} ({testPos}): 检测到 {raycastResults.Count} 个对象");

            foreach (var result in raycastResults)
            {
                Debug.Log($"  - {result.gameObject.name} (深度: {result.depth}, 距离: {result.distance})");

                // 检查是否是仓库槽位
                if (result.gameObject.GetComponent<WarehouseItemSlot>() != null ||
                    result.gameObject.GetComponentInParent<WarehouseItemSlot>() != null)
                {
                    Debug.Log($"    ✅ 这是仓库槽位！");
                }
            }
        }
    }

    private static void CheckUIElementStates()
    {
        Debug.Log("📊 检查UI元素状态:");

        // 检查WarehouseUI主组件
        WarehouseUI warehouseUI = Object.FindFirstObjectByType<WarehouseUI>();
        if (warehouseUI != null)
        {
            Debug.Log($"WarehouseUI: {warehouseUI.name}");
            Debug.Log($"  激活: {warehouseUI.gameObject.activeInHierarchy}");
            Debug.Log($"  启用: {warehouseUI.enabled}");

            if (warehouseUI.inventoryPanel != null)
            {
                Debug.Log($"  背包面板: {warehouseUI.inventoryPanel.name}");
                Debug.Log($"    激活: {warehouseUI.inventoryPanel.gameObject.activeInHierarchy}");

                WarehouseInventoryPanel inventoryPanel = warehouseUI.inventoryPanel.GetComponent<WarehouseInventoryPanel>();
                if (inventoryPanel != null)
                {
                    Debug.Log($"    WarehouseInventoryPanel启用: {inventoryPanel.enabled}");
                }
            }

            if (warehouseUI.warehouseCanvas != null)
            {
                Debug.Log($"  仓库Canvas: {warehouseUI.warehouseCanvas.name}");
                Debug.Log($"    激活: {warehouseUI.warehouseCanvas.gameObject.activeInHierarchy}");
            }
        }
        else
        {
            Debug.LogError("❌ WarehouseUI不存在");
        }

        // 检查EventSystem
        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem != null)
        {
            Debug.Log($"EventSystem: {eventSystem.name}");
            Debug.Log($"  激活: {eventSystem.gameObject.activeInHierarchy}");
            Debug.Log($"  启用: {eventSystem.enabled}");
            Debug.Log($"  当前选中: {eventSystem.currentSelectedGameObject}");

            var inputModules = eventSystem.GetComponents<BaseInputModule>();
            foreach (var module in inputModules)
            {
                Debug.Log($"  输入模块: {module.GetType().Name} - 启用: {module.enabled}");
            }
        }
        else
        {
            Debug.LogError("❌ EventSystem不存在");
        }
    }

    [MenuItem("Tools/研究室移动端UI/🔍 实时鼠标位置检测")]
    public static void StartMousePositionTracking()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ 请在游戏运行时使用此工具");
            return;
        }

        Debug.Log("=== 🔍 开始实时鼠标位置检测 ===");
        Debug.Log("移动鼠标到要测试的UI元素上，然后点击鼠标左键");

        // 创建一个MonoBehaviour来持续监控
        GameObject tracker = new GameObject("MouseTracker");
        tracker.AddComponent<MousePositionTracker>();
    }
}

/// <summary>
/// 鼠标位置追踪器
/// </summary>
public class MousePositionTracker : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))  // 左键点击
        {
            Vector2 mousePosition = Input.mousePosition;
            Debug.Log($"🖱️ 鼠标点击位置: {mousePosition}");

            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null)
            {
                PointerEventData pointerData = new PointerEventData(eventSystem);
                pointerData.position = mousePosition;

                var raycastResults = new System.Collections.Generic.List<RaycastResult>();
                eventSystem.RaycastAll(pointerData, raycastResults);

                Debug.Log($"📊 射线检测结果 ({raycastResults.Count} 个对象):");
                foreach (var result in raycastResults)
                {
                    Debug.Log($"  - {result.gameObject.name} (Canvas: {result.gameObject.GetComponentInParent<Canvas>()?.name})");

                    // 检查组件类型
                    if (result.gameObject.GetComponent<WarehouseItemSlot>() != null)
                    {
                        Debug.Log($"    ✅ 这是WarehouseItemSlot!");

                        WarehouseItemSlot slot = result.gameObject.GetComponent<WarehouseItemSlot>();
                        Debug.Log($"    有物品: {slot.HasItem()}");

                        Button button = slot.GetComponent<Button>();
                        if (button != null)
                        {
                            Debug.Log($"    Button可交互: {button.interactable}");
                        }
                    }

                    if (result.gameObject.GetComponent<Button>() != null)
                    {
                        Debug.Log($"    ✅ 这是Button组件!");
                    }
                }

                if (raycastResults.Count == 0)
                {
                    Debug.LogWarning("⚠️ 没有检测到任何UI对象！");
                }
            }

            Debug.Log("---");
        }

        // ESC键停止追踪
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("⏹️ 停止鼠标位置追踪");
            Destroy(gameObject);
        }
    }
}
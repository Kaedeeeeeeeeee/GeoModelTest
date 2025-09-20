using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 仓库点击诊断工具 - 专门诊断为什么样本点不了
/// </summary>
public class WarehouseClickDiagnostic
{
    [MenuItem("Tools/研究室移动端UI/🔍 诊断点击问题")]
    public static void DiagnoseClickIssues()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ 请在游戏运行时使用此工具");
            return;
        }

        Debug.Log("=== 🔍 诊断仓库点击问题 ===");

        // 1. 检查EventSystem
        CheckEventSystem();

        // 2. 检查仓库面板
        CheckWarehousePanels();

        // 3. 检查样本槽位
        CheckSampleSlots();

        // 4. 检查Canvas设置
        CheckCanvasSettings();

        // 5. 检查输入系统
        CheckInputSystem();

        Debug.Log("=== 诊断完成 ===");
    }

    private static void CheckEventSystem()
    {
        Debug.Log("📋 检查EventSystem:");

        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            Debug.LogError("❌ EventSystem不存在！这会导致UI无法接收点击事件");
            return;
        }

        Debug.Log($"✅ EventSystem存在: {eventSystem.name}");
        Debug.Log($"  当前选中对象: {eventSystem.currentSelectedGameObject}");
        Debug.Log($"  激活状态: {eventSystem.gameObject.activeInHierarchy}");
        Debug.Log($"  启用状态: {eventSystem.enabled}");

        // 检查InputModule
        var inputModules = eventSystem.GetComponents<BaseInputModule>();
        Debug.Log($"  输入模块数量: {inputModules.Length}");
        foreach (var module in inputModules)
        {
            Debug.Log($"    - {module.GetType().Name}: {module.enabled}");
        }
    }

    private static void CheckWarehousePanels()
    {
        Debug.Log("📋 检查仓库面板:");

        WarehouseUI warehouseUI = Object.FindFirstObjectByType<WarehouseUI>();
        if (warehouseUI == null)
        {
            Debug.LogError("❌ WarehouseUI不存在");
            return;
        }

        Debug.Log($"✅ WarehouseUI存在: {warehouseUI.name}");
        Debug.Log($"  激活状态: {warehouseUI.gameObject.activeInHierarchy}");

        // 检查背包面板
        if (warehouseUI.inventoryPanel != null)
        {
            Debug.Log($"✅ 背包面板存在: {warehouseUI.inventoryPanel.name}");
            Debug.Log($"  激活状态: {warehouseUI.inventoryPanel.gameObject.activeInHierarchy}");

            // 详细检查WarehouseInventoryPanel组件
            WarehouseInventoryPanel inventoryPanel = warehouseUI.inventoryPanel.GetComponent<WarehouseInventoryPanel>();
            if (inventoryPanel != null)
            {
                Debug.Log($"✅ WarehouseInventoryPanel组件存在");
                Debug.Log($"  组件启用状态: {inventoryPanel.enabled}");
            }
            else
            {
                Debug.LogError("❌ WarehouseInventoryPanel组件不存在");
            }
        }
        else
        {
            Debug.LogError("❌ inventoryPanel引用为空");
        }
    }

    private static void CheckSampleSlots()
    {
        Debug.Log("📋 检查样本槽位:");

        WarehouseInventoryPanel inventoryPanel = Object.FindFirstObjectByType<WarehouseInventoryPanel>();
        if (inventoryPanel == null)
        {
            Debug.LogError("❌ WarehouseInventoryPanel不存在");
            return;
        }

        // 查找所有WarehouseItemSlot组件
        WarehouseItemSlot[] slots = Object.FindObjectsOfType<WarehouseItemSlot>();
        Debug.Log($"📊 找到 {slots.Length} 个样本槽位");

        int activeSlots = 0;
        int slotsWithItems = 0;
        int clickableSlots = 0;

        foreach (var slot in slots)
        {
            if (slot.gameObject.activeInHierarchy)
            {
                activeSlots++;

                if (slot.HasItem())
                {
                    slotsWithItems++;

                    // 检查按钮组件
                    Button button = slot.GetComponent<Button>();
                    if (button == null)
                    {
                        button = slot.GetComponentInChildren<Button>();
                    }

                    if (button != null && button.interactable)
                    {
                        clickableSlots++;
                        Debug.Log($"  ✅ 槽位 {slot.name}: 有物品且可点击");

                        // 检查点击事件订阅
                        var clickEvent = slot.GetType().GetField("OnSlotClicked");
                        if (clickEvent != null)
                        {
                            var eventValue = clickEvent.GetValue(slot);
                            if (eventValue != null)
                            {
                                Debug.Log($"    ✅ OnSlotClicked事件已订阅");
                            }
                            else
                            {
                                Debug.LogWarning($"    ⚠️ OnSlotClicked事件未订阅");
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"  ⚠️ 槽位 {slot.name}: 有物品但不可点击");
                        if (button == null)
                            Debug.LogWarning($"    - 没有Button组件");
                        else if (!button.interactable)
                            Debug.LogWarning($"    - Button不可交互");
                    }
                }
            }
        }

        Debug.Log($"📊 槽位统计:");
        Debug.Log($"  总槽位: {slots.Length}");
        Debug.Log($"  激活槽位: {activeSlots}");
        Debug.Log($"  有物品槽位: {slotsWithItems}");
        Debug.Log($"  可点击槽位: {clickableSlots}");
    }

    private static void CheckCanvasSettings()
    {
        Debug.Log("📋 检查Canvas设置:");

        WarehouseUI warehouseUI = Object.FindFirstObjectByType<WarehouseUI>();
        if (warehouseUI?.warehouseCanvas != null)
        {
            Canvas canvas = warehouseUI.warehouseCanvas;
            Debug.Log($"✅ 仓库Canvas存在: {canvas.name}");
            Debug.Log($"  渲染模式: {canvas.renderMode}");
            Debug.Log($"  排序层级: {canvas.sortingOrder}");
            Debug.Log($"  覆盖排序: {canvas.overrideSorting}");

            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
            {
                Debug.Log($"✅ GraphicRaycaster存在且启用: {raycaster.enabled}");
            }
            else
            {
                Debug.LogError("❌ GraphicRaycaster不存在");
            }
        }
        else
        {
            Debug.LogError("❌ 仓库Canvas不存在");
        }
    }

    private static void CheckInputSystem()
    {
        Debug.Log("📋 检查输入系统:");

        // 检查鼠标位置
        Vector3 mousePosition = Input.mousePosition;
        Debug.Log($"鼠标位置: {mousePosition}");

        // 检查是否有物体阻挡射线
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null)
        {
            PointerEventData pointerData = new PointerEventData(eventSystem);
            pointerData.position = Input.mousePosition;

            var raycastResults = new System.Collections.Generic.List<RaycastResult>();
            eventSystem.RaycastAll(pointerData, raycastResults);

            Debug.Log($"射线检测结果 ({raycastResults.Count} 个对象):");
            foreach (var result in raycastResults)
            {
                Debug.Log($"  - {result.gameObject.name} (深度: {result.depth})");
            }
        }

        // 检查Mobile Input系统是否干扰
        MobileInputManager mobileInput = MobileInputManager.Instance;
        if (mobileInput != null)
        {
            Debug.Log($"MobileInputManager存在: {mobileInput.name}");
            Debug.Log($"  桌面测试模式: {mobileInput.desktopTestMode}");
        }
    }

    [MenuItem("Tools/研究室移动端UI/🧪 强制点击测试")]
    public static void ForceClickTest()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ 请在游戏运行时使用此工具");
            return;
        }

        Debug.Log("=== 🧪 强制点击测试 ===");

        // 查找第一个有物品的槽位
        WarehouseItemSlot[] slots = Object.FindObjectsOfType<WarehouseItemSlot>();
        WarehouseItemSlot testSlot = null;

        foreach (var slot in slots)
        {
            if (slot.HasItem() && slot.gameObject.activeInHierarchy)
            {
                testSlot = slot;
                break;
            }
        }

        if (testSlot != null)
        {
            Debug.Log($"找到测试槽位: {testSlot.name}");

            // 尝试强制触发点击事件
            try
            {
                var onClickField = testSlot.GetType().GetField("OnSlotClicked");
                if (onClickField != null)
                {
                    var onClickEvent = onClickField.GetValue(testSlot) as System.Action<WarehouseItemSlot>;
                    if (onClickEvent != null)
                    {
                        Debug.Log("手动触发OnSlotClicked事件");
                        onClickEvent.Invoke(testSlot);
                    }
                    else
                    {
                        Debug.LogWarning("OnSlotClicked事件为空");
                    }
                }
                else
                {
                    Debug.LogError("找不到OnSlotClicked字段");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"强制点击测试失败: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("找不到有物品的槽位进行测试");
        }

        Debug.Log("=== 强制点击测试完成 ===");
    }
}
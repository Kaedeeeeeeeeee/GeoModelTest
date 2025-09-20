using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 完整点击测试工具 - 验证修复后的点击功能
/// </summary>
public class CompleteClickTest
{
    [MenuItem("Tools/研究室移动端UI/🎯 完整点击功能测试")]
    public static void CompleteClickFunctionTest()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ 请在游戏运行时使用此工具");
            return;
        }

        Debug.Log("=== 🎯 完整点击功能测试 ===");

        // 1. 测试输入系统
        TestInputSystem();

        // 2. 测试UI射线检测
        TestUIRaycast();

        // 3. 模拟真实点击
        SimulateRealClick();

        Debug.Log("=== 完整测试完成 ===");
    }

    private static void TestInputSystem()
    {
        Debug.Log("🖱️ 测试输入系统:");

        try
        {
            // 测试旧输入系统
            Vector3 mousePos = Input.mousePosition;
            Debug.Log($"✅ 旧Input系统鼠标位置: {mousePos}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ 旧Input系统错误: {e.Message}");
        }

        try
        {
            // 测试新输入系统（使用反射）
            var mouseType = System.Type.GetType("UnityEngine.InputSystem.Mouse, Unity.InputSystem");
            if (mouseType != null)
            {
                var currentProperty = mouseType.GetProperty("current");
                if (currentProperty != null)
                {
                    var mouse = currentProperty.GetValue(null);
                    if (mouse != null)
                    {
                        var positionProperty = mouse.GetType().GetProperty("position");
                        if (positionProperty != null)
                        {
                            var position = positionProperty.GetValue(mouse);
                            Debug.Log($"✅ 新Input系统鼠标位置: {position}");
                        }
                    }
                }
            }
            else
            {
                Debug.Log("⚠️ 新Input系统不可用");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ 新Input系统错误: {e.Message}");
        }
    }

    private static void TestUIRaycast()
    {
        Debug.Log("🎯 测试UI射线检测:");

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            Debug.LogError("❌ EventSystem不存在");
            return;
        }

        // 使用屏幕中心点进行测试
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);

        PointerEventData pointerData = new PointerEventData(eventSystem);
        pointerData.position = screenCenter;

        var raycastResults = new System.Collections.Generic.List<RaycastResult>();
        eventSystem.RaycastAll(pointerData, raycastResults);

        Debug.Log($"屏幕中心 ({screenCenter}) 射线检测结果:");
        Debug.Log($"检测到 {raycastResults.Count} 个UI对象");

        foreach (var result in raycastResults)
        {
            Debug.Log($"  - {result.gameObject.name} (层级: {result.depth})");

            // 检查是否是仓库相关的UI
            if (result.gameObject.name.Contains("Slot") ||
                result.gameObject.name.Contains("Warehouse") ||
                result.gameObject.name.Contains("Inventory"))
            {
                Debug.Log($"    ✅ 这是仓库相关UI");
            }
        }
    }

    private static void SimulateRealClick()
    {
        Debug.Log("🖱️ 模拟真实点击:");

        // 查找有物品的槽位
        WarehouseItemSlot[] slots = Object.FindObjectsOfType<WarehouseItemSlot>();
        WarehouseItemSlot targetSlot = null;

        foreach (var slot in slots)
        {
            if (slot.HasItem() && slot.gameObject.activeInHierarchy)
            {
                // 检查槽位是否在仓库面板中（而不是背包面板）
                if (slot.name.Contains("Storage"))
                {
                    targetSlot = slot;
                    break;
                }
            }
        }

        if (targetSlot == null)
        {
            Debug.LogWarning("⚠️ 找不到合适的测试槽位");
            return;
        }

        Debug.Log($"🎯 目标槽位: {targetSlot.name}");

        // 获取槽位的屏幕位置
        RectTransform rectTransform = targetSlot.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError("❌ 槽位没有RectTransform");
            return;
        }

        // 转换到屏幕坐标
        Vector3 worldPosition = rectTransform.position;
        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(Camera.main, worldPosition);

        Debug.Log($"槽位屏幕位置: {screenPosition}");

        // 创建模拟的点击事件
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null)
        {
            PointerEventData clickData = new PointerEventData(eventSystem);
            clickData.position = screenPosition;
            clickData.button = PointerEventData.InputButton.Left;

            // 执行射线检测
            var raycastResults = new System.Collections.Generic.List<RaycastResult>();
            eventSystem.RaycastAll(clickData, raycastResults);

            Debug.Log($"模拟点击射线检测结果 ({raycastResults.Count} 个对象):");
            foreach (var result in raycastResults)
            {
                Debug.Log($"  - {result.gameObject.name}");
            }

            // 尝试直接触发点击
            if (raycastResults.Count > 0)
            {
                var topResult = raycastResults[0];
                var clickable = topResult.gameObject.GetComponent<IPointerClickHandler>();

                if (clickable != null)
                {
                    Debug.Log("🎯 执行模拟点击");
                    clickable.OnPointerClick(clickData);
                }
                else
                {
                    Debug.LogWarning("⚠️ 目标对象不支持点击");
                }
            }
        }

        // 最后，直接触发槽位事件作为备用
        Debug.Log("🔄 备用方案：直接触发槽位点击");
        TriggerSlotClick(targetSlot);
    }

    private static void TriggerSlotClick(WarehouseItemSlot slot)
    {
        try
        {
            var onClickField = slot.GetType().GetField("OnSlotClicked");
            if (onClickField != null)
            {
                var onClickEvent = onClickField.GetValue(slot) as System.Action<WarehouseItemSlot>;
                if (onClickEvent != null)
                {
                    Debug.Log("✅ 直接触发OnSlotClicked事件");
                    onClickEvent.Invoke(slot);
                }
                else
                {
                    Debug.LogWarning("⚠️ OnSlotClicked事件为空");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ 直接触发失败: {e.Message}");
        }
    }

    [MenuItem("Tools/研究室移动端UI/🔍 详细UI层次检查")]
    public static void DetailedUIHierarchyCheck()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ 请在游戏运行时使用此工具");
            return;
        }

        Debug.Log("=== 🔍 详细UI层次检查 ===");

        // 查找所有Canvas
        Canvas[] canvases = Object.FindObjectsOfType<Canvas>();
        Debug.Log($"找到 {canvases.Length} 个Canvas:");

        foreach (var canvas in canvases)
        {
            Debug.Log($"Canvas: {canvas.name}");
            Debug.Log($"  排序层级: {canvas.sortingOrder}");
            Debug.Log($"  渲染模式: {canvas.renderMode}");
            Debug.Log($"  激活状态: {canvas.gameObject.activeInHierarchy}");

            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            Debug.Log($"  GraphicRaycaster: {(raycaster != null && raycaster.enabled ? "✅" : "❌")}");

            // 检查是否包含仓库UI
            if (canvas.name.Contains("Warehouse") || canvas.name.Contains("Mobile"))
            {
                Debug.Log($"  🏪 这是仓库/移动端相关Canvas");
            }
        }

        Debug.Log("=== UI层次检查完成 ===");
    }
}
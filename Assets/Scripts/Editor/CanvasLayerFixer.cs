using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

/// <summary>
/// Canvas层级修复工具 - 解决UI层级冲突问题
/// </summary>
public class CanvasLayerFixer
{
    [MenuItem("Tools/研究室移动端UI/🔧 修复Canvas层级冲突")]
    public static void FixCanvasLayerConflict()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ 请在游戏运行时使用此工具");
            return;
        }

        Debug.Log("=== 🔧 修复Canvas层级冲突 ===");

        // 1. 检查当前Canvas层级
        CheckCurrentCanvasLayers();

        // 2. 修复移动端控制UI层级
        FixMobileControlsLayering();

        // 3. 确保仓库UI在合适的层级
        EnsureWarehouseUILayer();

        // 4. 优化LookTouchArea配置
        OptimizeLookTouchArea();

        // 5. 验证修复结果
        VerifyFix();

        Debug.Log("🎉 Canvas层级冲突修复完成！");
    }

    private static void CheckCurrentCanvasLayers()
    {
        Debug.Log("📊 当前Canvas层级状态:");

        Canvas[] canvases = Object.FindObjectsOfType<Canvas>();
        System.Array.Sort(canvases, (a, b) => a.sortingOrder.CompareTo(b.sortingOrder));

        foreach (var canvas in canvases)
        {
            Debug.Log($"Canvas: {canvas.name} - 层级: {canvas.sortingOrder} - 激活: {canvas.gameObject.activeInHierarchy}");

            // 检查是否包含LookTouchArea
            if (canvas.name.Contains("MobileControls"))
            {
                Transform lookTouchArea = canvas.transform.Find("LookTouchArea");
                if (lookTouchArea != null)
                {
                    Debug.Log($"  🎯 发现LookTouchArea: {lookTouchArea.gameObject.activeInHierarchy}");
                }
            }
        }
    }

    private static void FixMobileControlsLayering()
    {
        Debug.Log("🔧 修复移动端控制UI层级:");

        MobileControlsUI mobileControlsUI = Object.FindFirstObjectByType<MobileControlsUI>();
        if (mobileControlsUI != null)
        {
            Canvas mobileCanvas = mobileControlsUI.GetComponent<Canvas>();
            if (mobileCanvas != null)
            {
                Debug.Log($"找到MobileControlsUI Canvas: {mobileCanvas.name}");
                Debug.Log($"  当前层级: {mobileCanvas.sortingOrder}");

                // 将移动端UI设置为较低的层级，让仓库UI在上面
                int oldOrder = mobileCanvas.sortingOrder;
                mobileCanvas.sortingOrder = 100;  // 设置为较低层级
                Debug.Log($"  层级已修改: {oldOrder} → {mobileCanvas.sortingOrder}");
            }

            // 检查并优化LookTouchArea
            Transform lookTouchArea = mobileControlsUI.transform.Find("LookTouchArea");
            if (lookTouchArea != null)
            {
                Debug.Log("发现LookTouchArea，检查其配置:");

                Image lookImage = lookTouchArea.GetComponent<Image>();
                if (lookImage != null)
                {
                    Debug.Log($"  当前raycastTarget: {lookImage.raycastTarget}");

                    // 关闭raycastTarget，这样它就不会阻挡其他UI
                    lookImage.raycastTarget = false;
                    Debug.Log("  ✅ 已关闭LookTouchArea的raycastTarget");
                }

                GraphicRaycaster raycaster = lookTouchArea.GetComponent<GraphicRaycaster>();
                if (raycaster != null)
                {
                    raycaster.enabled = false;
                    Debug.Log("  ✅ 已禁用LookTouchArea的GraphicRaycaster");
                }

                // 检查其他可能阻挡的组件
                Button lookButton = lookTouchArea.GetComponent<Button>();
                if (lookButton != null)
                {
                    Debug.Log($"  LookTouchArea有Button组件，可交互: {lookButton.interactable}");
                }
            }
        }
        else
        {
            Debug.LogWarning("⚠️ 未找到MobileControlsUI");
        }
    }

    private static void EnsureWarehouseUILayer()
    {
        Debug.Log("🔧 确保仓库UI层级:");

        WarehouseUI warehouseUI = Object.FindFirstObjectByType<WarehouseUI>();
        if (warehouseUI != null && warehouseUI.warehouseCanvas != null)
        {
            Canvas warehouseCanvas = warehouseUI.warehouseCanvas;
            Debug.Log($"找到仓库Canvas: {warehouseCanvas.name}");
            Debug.Log($"  当前层级: {warehouseCanvas.sortingOrder}");

            // 确保仓库UI在移动端UI之上
            int oldOrder = warehouseCanvas.sortingOrder;
            warehouseCanvas.sortingOrder = 200;  // 设置为较高层级
            warehouseCanvas.overrideSorting = true;  // 确保覆盖排序生效
            Debug.Log($"  层级已修改: {oldOrder} → {warehouseCanvas.sortingOrder}");

            // 确保GraphicRaycaster启用
            GraphicRaycaster raycaster = warehouseCanvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
            {
                raycaster.enabled = true;
                Debug.Log("  ✅ 确保GraphicRaycaster启用");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ 未找到仓库UI或Canvas");
        }
    }

    private static void OptimizeLookTouchArea()
    {
        Debug.Log("🔧 优化LookTouchArea配置:");

        // 查找所有名为LookTouchArea的对象
        GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
        foreach (var obj in allObjects)
        {
            if (obj.name.Contains("LookTouchArea"))
            {
                Debug.Log($"处理 {obj.name}:");

                // 关闭Image的raycastTarget
                Image image = obj.GetComponent<Image>();
                if (image != null)
                {
                    image.raycastTarget = false;
                    Debug.Log("  ✅ 关闭raycastTarget");
                }

                // 检查RectTransform大小
                RectTransform rectTransform = obj.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    Vector2 size = rectTransform.rect.size;
                    Debug.Log($"  大小: {size}");

                    // 如果LookTouchArea覆盖了整个屏幕，我们需要调整它
                    if (size.x > Screen.width * 0.8f || size.y > Screen.height * 0.8f)
                    {
                        Debug.LogWarning("  ⚠️ LookTouchArea覆盖了大部分屏幕区域");

                        // 可以选择调整大小或者位置
                        // 这里我们先尝试将其移到屏幕右半部分
                        rectTransform.anchorMin = new Vector2(0.5f, 0f);
                        rectTransform.anchorMax = new Vector2(1f, 1f);
                        rectTransform.offsetMin = Vector2.zero;
                        rectTransform.offsetMax = Vector2.zero;
                        Debug.Log("  ✅ 调整LookTouchArea到屏幕右半部分");
                    }
                }

                // 检查其父级Canvas
                Canvas parentCanvas = obj.GetComponentInParent<Canvas>();
                if (parentCanvas != null)
                {
                    Debug.Log($"  父级Canvas: {parentCanvas.name} (层级: {parentCanvas.sortingOrder})");
                }
            }
        }
    }

    private static void VerifyFix()
    {
        Debug.Log("🔍 验证修复结果:");

        // 模拟点击测试
        Vector2 testPosition = new Vector2(1333f, 986f);  // 使用用户报告的点击位置

        UnityEngine.EventSystems.EventSystem eventSystem = UnityEngine.EventSystems.EventSystem.current;
        if (eventSystem != null)
        {
            UnityEngine.EventSystems.PointerEventData pointerData = new UnityEngine.EventSystems.PointerEventData(eventSystem);
            pointerData.position = testPosition;

            var raycastResults = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            eventSystem.RaycastAll(pointerData, raycastResults);

            Debug.Log($"修复后射线检测结果 ({raycastResults.Count} 个对象):");
            for (int i = 0; i < raycastResults.Count; i++)
            {
                var result = raycastResults[i];
                Debug.Log($"  {i+1}. {result.gameObject.name} (Canvas: {result.gameObject.GetComponentInParent<Canvas>()?.name})");

                if (result.gameObject.GetComponent<WarehouseItemSlot>() != null)
                {
                    Debug.Log($"    ✅ WarehouseItemSlot在第{i+1}位 - {(i == 0 ? "优先级最高！" : "仍被其他UI覆盖")}");
                }
            }

            // 检查仓库槽位是否能正确响应
            if (raycastResults.Count > 0)
            {
                var topResult = raycastResults[0];
                if (topResult.gameObject.GetComponent<WarehouseItemSlot>() != null)
                {
                    Debug.Log("🎉 修复成功！仓库槽位现在是点击优先级最高的对象");
                }
                else
                {
                    Debug.LogWarning($"⚠️ 修复可能不完整，顶层对象仍然是: {topResult.gameObject.name}");
                }
            }
        }
    }

    [MenuItem("Tools/研究室移动端UI/📊 显示当前Canvas层级")]
    public static void ShowCanvasLayers()
    {
        Debug.Log("=== 📊 当前Canvas层级状态 ===");

        Canvas[] canvases = Object.FindObjectsOfType<Canvas>();
        System.Array.Sort(canvases, (a, b) => a.sortingOrder.CompareTo(b.sortingOrder));

        foreach (var canvas in canvases)
        {
            Debug.Log($"Canvas: {canvas.name}");
            Debug.Log($"  层级: {canvas.sortingOrder}");
            Debug.Log($"  覆盖排序: {canvas.overrideSorting}");
            Debug.Log($"  激活: {canvas.gameObject.activeInHierarchy}");
            Debug.Log($"  GraphicRaycaster: {(canvas.GetComponent<GraphicRaycaster>()?.enabled ?? false)}");
            Debug.Log("---");
        }
    }
}
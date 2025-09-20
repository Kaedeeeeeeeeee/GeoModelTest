using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

/// <summary>
/// 样本详情显示修复工具
/// </summary>
public class SampleDetailFixer
{
    [MenuItem("Tools/研究室移动端UI/🔍 修复样本详情显示")]
    public static void FixSampleDetailDisplay()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ 此工具需要在游戏运行时使用！请先点击Play按钮启动游戏。");
            EditorUtility.DisplayDialog("提示", "此工具需要在游戏运行时使用！\n请先点击Play按钮启动游戏，然后再运行此工具。", "确定");
            return;
        }

        Debug.Log("=== 🔍 修复样本详情显示 ===");

        // 1. 检查InventoryUI是否存在
        InventoryUI inventoryUI = Object.FindFirstObjectByType<InventoryUI>();
        if (inventoryUI == null)
        {
            Debug.Log("❌ InventoryUI不存在，创建新的InventoryUI系统");
            CreateInventoryUISystem();
        }
        else
        {
            Debug.Log("✅ InventoryUI已存在");

            // 检查InventoryUI的组件完整性
            CheckInventoryUIComponents(inventoryUI);
        }

        // 2. 测试样本详情显示功能
        TestSampleDetailDisplay();

        Debug.Log("🎉 样本详情显示修复完成！");
    }

    private static void CreateInventoryUISystem()
    {
        // 创建InventoryUI游戏对象
        GameObject inventoryUIObj = new GameObject("InventoryUI");
        InventoryUI inventoryUI = inventoryUIObj.AddComponent<InventoryUI>();

        // 创建Canvas
        GameObject canvasObj = new GameObject("InventoryCanvas");
        canvasObj.transform.SetParent(inventoryUIObj.transform);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // 确保在仓库UI之上
        canvasObj.AddComponent<GraphicRaycaster>();

        // 创建详情面板
        GameObject detailPanel = CreateDetailPanel(canvasObj.transform);

        // 设置InventoryUI的引用
        inventoryUI.inventoryCanvas = canvas;
        inventoryUI.detailPanel = detailPanel;

        // 查找详情面板的子组件
        Transform titleTransform = detailPanel.transform.Find("DetailTitle");
        Transform infoTransform = detailPanel.transform.Find("DetailInfo");
        Transform closeButtonTransform = detailPanel.transform.Find("CloseButton");

        if (titleTransform != null)
            inventoryUI.detailTitleText = titleTransform.GetComponent<UnityEngine.UI.Text>();
        if (infoTransform != null)
            inventoryUI.detailInfoText = infoTransform.GetComponent<UnityEngine.UI.Text>();
        if (closeButtonTransform != null)
            inventoryUI.closeDetailButton = closeButtonTransform.GetComponent<UnityEngine.UI.Button>();

        // 初始隐藏详情面板
        detailPanel.SetActive(false);

        Debug.Log("✅ 创建了InventoryUI系统");
    }

    private static GameObject CreateDetailPanel(Transform parent)
    {
        // 创建详情面板
        GameObject detailPanel = new GameObject("DetailPanel");
        detailPanel.transform.SetParent(parent);

        RectTransform detailRect = detailPanel.AddComponent<RectTransform>();
        detailRect.anchorMin = new Vector2(0.5f, 0.5f);
        detailRect.anchorMax = new Vector2(0.5f, 0.5f);
        detailRect.pivot = new Vector2(0.5f, 0.5f);
        detailRect.sizeDelta = new Vector2(400, 300);
        detailRect.anchoredPosition = Vector2.zero;

        // 添加背景
        UnityEngine.UI.Image detailBg = detailPanel.AddComponent<UnityEngine.UI.Image>();
        detailBg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

        // 创建标题文本
        GameObject titleObj = new GameObject("DetailTitle");
        titleObj.transform.SetParent(detailPanel.transform);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.8f);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.offsetMin = new Vector2(10, 0);
        titleRect.offsetMax = new Vector2(-10, -10);

        UnityEngine.UI.Text titleText = titleObj.AddComponent<UnityEngine.UI.Text>();
        titleText.text = "样本详情";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 18;
        titleText.color = Color.white;
        titleText.alignment = TextAnchor.MiddleCenter;

        // 创建信息文本
        GameObject infoObj = new GameObject("DetailInfo");
        infoObj.transform.SetParent(detailPanel.transform);
        RectTransform infoRect = infoObj.AddComponent<RectTransform>();
        infoRect.anchorMin = new Vector2(0, 0.2f);
        infoRect.anchorMax = new Vector2(1, 0.8f);
        infoRect.offsetMin = new Vector2(10, 0);
        infoRect.offsetMax = new Vector2(-10, 0);

        UnityEngine.UI.Text infoText = infoObj.AddComponent<UnityEngine.UI.Text>();
        infoText.text = "样本信息将在这里显示";
        infoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        infoText.fontSize = 14;
        infoText.color = Color.white;
        infoText.alignment = TextAnchor.UpperLeft;

        // 创建关闭按钮
        GameObject closeButtonObj = new GameObject("CloseButton");
        closeButtonObj.transform.SetParent(detailPanel.transform);
        RectTransform closeButtonRect = closeButtonObj.AddComponent<RectTransform>();
        closeButtonRect.anchorMin = new Vector2(0.3f, 0.05f);
        closeButtonRect.anchorMax = new Vector2(0.7f, 0.15f);
        closeButtonRect.offsetMin = Vector2.zero;
        closeButtonRect.offsetMax = Vector2.zero;

        UnityEngine.UI.Image closeButtonBg = closeButtonObj.AddComponent<UnityEngine.UI.Image>();
        closeButtonBg.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);

        UnityEngine.UI.Button closeButton = closeButtonObj.AddComponent<UnityEngine.UI.Button>();
        closeButton.targetGraphic = closeButtonBg;

        GameObject closeTextObj = new GameObject("CloseText");
        closeTextObj.transform.SetParent(closeButtonObj.transform);
        RectTransform closeTextRect = closeTextObj.AddComponent<RectTransform>();
        closeTextRect.anchorMin = Vector2.zero;
        closeTextRect.anchorMax = Vector2.one;
        closeTextRect.offsetMin = Vector2.zero;
        closeTextRect.offsetMax = Vector2.zero;

        UnityEngine.UI.Text closeText = closeTextObj.AddComponent<UnityEngine.UI.Text>();
        closeText.text = "关闭";
        closeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        closeText.fontSize = 14;
        closeText.color = Color.white;
        closeText.alignment = TextAnchor.MiddleCenter;

        // 设置关闭按钮事件
        closeButton.onClick.AddListener(() => {
            detailPanel.SetActive(false);
        });

        return detailPanel;
    }

    private static void CheckInventoryUIComponents(InventoryUI inventoryUI)
    {
        Debug.Log("📊 检查InventoryUI组件完整性:");

        Debug.Log($"  inventoryCanvas: {(inventoryUI.inventoryCanvas != null ? "✅" : "❌")}");
        Debug.Log($"  detailPanel: {(inventoryUI.detailPanel != null ? "✅" : "❌")}");
        Debug.Log($"  detailTitleText: {(inventoryUI.detailTitleText != null ? "✅" : "❌")}");
        Debug.Log($"  detailInfoText: {(inventoryUI.detailInfoText != null ? "✅" : "❌")}");
        Debug.Log($"  closeDetailButton: {(inventoryUI.closeDetailButton != null ? "✅" : "❌")}");

        // 如果缺少组件，尝试修复
        if (inventoryUI.detailPanel == null)
        {
            Debug.LogWarning("❌ 详情面板缺失，尝试修复");
            if (inventoryUI.inventoryCanvas != null)
            {
                GameObject detailPanel = CreateDetailPanel(inventoryUI.inventoryCanvas.transform);
                inventoryUI.detailPanel = detailPanel;
                Debug.Log("✅ 重新创建了详情面板");
            }
        }
    }

    private static void TestSampleDetailDisplay()
    {
        Debug.Log("🧪 测试样本详情显示功能");

        // 获取第一个样本进行测试
        SampleInventory inventory = SampleInventory.Instance;
        if (inventory != null)
        {
            var samples = inventory.GetAllSamples();
            if (samples.Count > 0)
            {
                var testSample = samples[0];
                Debug.Log($"📋 使用测试样本: {testSample.displayName}");

                // 获取InventoryUI并测试显示
                InventoryUI inventoryUI = Object.FindFirstObjectByType<InventoryUI>();
                if (inventoryUI != null)
                {
                    // 使用反射调用ShowSampleDetail方法
                    var method = inventoryUI.GetType().GetMethod("ShowSampleDetail",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (method != null)
                    {
                        try
                        {
                            method.Invoke(inventoryUI, new object[] { testSample });
                            Debug.Log("✅ 成功显示样本详情");
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"❌ 显示样本详情失败: {e.Message}");
                        }
                    }
                    else
                    {
                        Debug.LogError("❌ 找不到ShowSampleDetail方法");
                    }
                }
            }
            else
            {
                Debug.LogWarning("⚠️ 背包中没有样本用于测试");
            }
        }
        else
        {
            Debug.LogError("❌ SampleInventory不存在");
        }
    }

    [MenuItem("Tools/研究室移动端UI/🧪 测试样本点击")]
    public static void TestSampleClick()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ 请在游戏运行时使用此工具");
            return;
        }

        Debug.Log("=== 🧪 测试样本点击功能 ===");

        // 查找WarehouseInventoryPanel
        WarehouseInventoryPanel inventoryPanel = Object.FindFirstObjectByType<WarehouseInventoryPanel>();
        if (inventoryPanel != null)
        {
            Debug.Log("✅ 找到WarehouseInventoryPanel");

            // 强制刷新显示
            inventoryPanel.RefreshInventoryDisplay();
            Debug.Log("✅ 刷新了背包显示");
        }
        else
        {
            Debug.LogError("❌ 找不到WarehouseInventoryPanel");
        }

        Debug.Log("=== 测试完成 ===");
    }
}
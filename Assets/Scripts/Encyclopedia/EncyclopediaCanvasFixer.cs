using UnityEngine;
using UnityEngine.UI;
using Encyclopedia;

/// <summary>
/// 图鉴Canvas层级和点击事件修复器
/// 在游戏启动时自动修复Canvas层级和点击事件问题
/// </summary>
public class EncyclopediaCanvasFixer : MonoBehaviour
{
    [Header("自动修复设置")]
    [SerializeField] private bool autoFixOnStart = true;
    [SerializeField] private int targetSortingOrder = 10001; // 比InventoryCanvas(10000)高
    [SerializeField] private float delaySeconds = 2f; // 延迟修复，确保所有UI已初始化
    [SerializeField] private float iconFixDelaySeconds = 3f; // 图标修复额外延迟

    private void Awake()
    {
        Debug.Log($"[EncyclopediaCanvasFixer] Awake - autoFixOnStart: {autoFixOnStart}");
    }

    private void Start()
    {
        Debug.Log($"[EncyclopediaCanvasFixer] Start - autoFixOnStart: {autoFixOnStart}");
        if (autoFixOnStart)
        {
            Debug.Log($"[EncyclopediaCanvasFixer] 将在 {delaySeconds} 秒后开始修复");
            Invoke(nameof(ApplyFixes), delaySeconds);
        }
    }

    private void OnEnable()
    {
        Debug.Log("[EncyclopediaCanvasFixer] OnEnable 被调用");
    }

    /// <summary>
    /// 应用所有修复
    /// </summary>
    public void ApplyFixes()
    {
        Debug.Log("[EncyclopediaCanvasFixer] ======= 开始应用图鉴修复 =======");
        Debug.Log($"[EncyclopediaCanvasFixer] 当前时间: {Time.time}");
        Debug.Log($"[EncyclopediaCanvasFixer] 游戏对象: {gameObject.name}");
        Debug.Log($"[EncyclopediaCanvasFixer] 激活状态: {gameObject.activeInHierarchy}");

        try
        {
            FixCanvasLayer();
            FixClickEvents();
            FixDetailPanelLayout();

            // 添加详情面板关闭按钮，延迟执行确保面板创建完成
            Invoke(nameof(AddDetailPanelCloseButton), iconFixDelaySeconds - delaySeconds + 0.5f);

            Debug.Log("[EncyclopediaCanvasFixer] ======= 基础修复已应用完成，UI修复将稍后执行 =======");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[EncyclopediaCanvasFixer] 修复过程中发生错误: {e.Message}");
            Debug.LogError($"[EncyclopediaCanvasFixer] 堆栈跟踪: {e.StackTrace}");
        }
    }

    /// <summary>
    /// 修复Canvas层级
    /// </summary>
    private void FixCanvasLayer()
    {
        var encyclopediaUI = FindObjectOfType<EncyclopediaUI>();
        if (encyclopediaUI == null)
        {
            Debug.LogWarning("[EncyclopediaCanvasFixer] 未找到EncyclopediaUI组件");
            return;
        }

        var canvas = encyclopediaUI.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[EncyclopediaCanvasFixer] EncyclopediaUI没有父级Canvas");
            return;
        }

        Debug.Log($"[EncyclopediaCanvasFixer] 修复Canvas层级: {canvas.name}");
        Debug.Log($"  - 原层级: {canvas.sortingOrder}");

        // 设置高优先级层级
        canvas.sortingOrder = targetSortingOrder;
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // 确保GraphicRaycaster存在且启用
        var raycaster = canvas.GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
            Debug.Log("  - 添加了GraphicRaycaster组件");
        }
        raycaster.enabled = true;

        Debug.Log($"  - 新层级: {canvas.sortingOrder}");
    }

    /// <summary>
    /// 修复点击事件
    /// </summary>
    private void FixClickEvents()
    {
        var encyclopediaUI = FindObjectOfType<EncyclopediaUI>();
        if (encyclopediaUI == null)
        {
            Debug.LogWarning("[EncyclopediaCanvasFixer] 未找到EncyclopediaUI组件");
            return;
        }

        // 使用反射获取私有字段
        var entryListContainerField = typeof(EncyclopediaUI).GetField("entryListContainer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var entryListContainer = entryListContainerField?.GetValue(encyclopediaUI) as Transform;

        if (entryListContainer == null)
        {
            Debug.LogWarning("[EncyclopediaCanvasFixer] 未找到entryListContainer");
            return;
        }

        var onEntryItemClickedMethod = typeof(EncyclopediaUI).GetMethod("OnEntryItemClicked",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (onEntryItemClickedMethod == null)
        {
            Debug.LogError("[EncyclopediaCanvasFixer] 未找到OnEntryItemClicked方法");
            return;
        }

        Debug.Log($"[EncyclopediaCanvasFixer] 修复 {entryListContainer.childCount} 个条目的点击事件");

        // 确保EncyclopediaData已初始化
        if (EncyclopediaData.Instance == null)
        {
            var dataComponent = FindObjectOfType<EncyclopediaData>();
            if (dataComponent != null)
            {
                // 强制初始化
                var awakeMethod = typeof(EncyclopediaData).GetMethod("Awake",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                awakeMethod?.Invoke(dataComponent, null);
            }
        }

        if (EncyclopediaData.Instance == null || EncyclopediaData.Instance.AllEntries == null)
        {
            Debug.LogError("[EncyclopediaCanvasFixer] EncyclopediaData未正确初始化");
            return;
        }

        var allEntries = EncyclopediaData.Instance.AllEntries.Values;
        int fixedCount = 0;

        // 重新绑定所有条目的点击事件
        for (int i = 0; i < entryListContainer.childCount; i++)
        {
            var child = entryListContainer.GetChild(i);
            var button = child.GetComponent<Button>();

            if (button != null)
            {
                // 清除现有事件
                button.onClick.RemoveAllListeners();

                // 获取条目名称
                var nameTexts = child.GetComponentsInChildren<Text>();
                string entryName = "";
                foreach (var text in nameTexts)
                {
                    if (!string.IsNullOrEmpty(text.text) && text.text != "???")
                    {
                        entryName = text.text.Trim();
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(entryName))
                {
                    // 查找匹配的Entry
                    EncyclopediaEntry matchedEntry = null;
                    foreach (var entry in allEntries)
                    {
                        if (entry.GetFormattedDisplayName() == entryName)
                        {
                            matchedEntry = entry;
                            break;
                        }
                    }

                    if (matchedEntry != null)
                    {
                        // 绑定点击事件
                        button.onClick.AddListener(() => {
                            Debug.Log($"[EncyclopediaCanvasFixer] 条目被点击: {matchedEntry.GetFormattedDisplayName()}");
                            onEntryItemClickedMethod.Invoke(encyclopediaUI, new object[] { matchedEntry });
                        });

                        fixedCount++;
                    }
                }
            }
        }

        Debug.Log($"[EncyclopediaCanvasFixer] 成功修复 {fixedCount} 个条目的点击事件");
    }


    /// <summary>
    /// 自动添加详情面板关闭按钮
    /// </summary>
    private void AddDetailPanelCloseButton()
    {
        var encyclopediaUI = FindObjectOfType<EncyclopediaUI>();
        if (encyclopediaUI == null)
        {
            Debug.LogWarning("[EncyclopediaCanvasFixer] 未找到EncyclopediaUI组件");
            return;
        }

        // 使用反射获取detailPanel
        var detailPanelField = typeof(EncyclopediaUI).GetField("detailPanel",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var detailPanel = detailPanelField?.GetValue(encyclopediaUI) as GameObject;

        if (detailPanel == null)
        {
            Debug.LogWarning("[EncyclopediaCanvasFixer] 未找到detailPanel");
            return;
        }

        Debug.Log($"[EncyclopediaCanvasFixer] 为详情面板添加关闭按钮: {detailPanel.name}");

        // 检查是否已存在关闭按钮，避免重复创建
        var existingCloseButton = detailPanel.transform.Find("DetailCloseButton");
        if (existingCloseButton != null)
        {
            Debug.Log("[EncyclopediaCanvasFixer] 详情面板关闭按钮已存在，跳过创建");
            return;
        }

        // 移除错误位置的关闭按钮
        var wrongCloseButton = detailPanel.transform.Find("CloseButton");
        if (wrongCloseButton != null)
        {
            UnityEngine.Object.Destroy(wrongCloseButton.gameObject);
            Debug.Log("[EncyclopediaCanvasFixer] 移除错误位置的关闭按钮");
        }

        // 创建详情面板专用关闭按钮
        GameObject detailCloseButtonGO = new GameObject("DetailCloseButton");
        detailCloseButtonGO.transform.SetParent(detailPanel.transform, false);

        // 设置RectTransform（右上角位置）
        RectTransform closeRect = detailCloseButtonGO.AddComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1, 1);
        closeRect.anchorMax = new Vector2(1, 1);
        closeRect.pivot = new Vector2(1, 1);
        closeRect.anchoredPosition = new Vector2(-15, -15);
        closeRect.sizeDelta = new Vector2(30, 30);

        // 添加Image组件（背景）
        Image buttonImage = detailCloseButtonGO.AddComponent<Image>();
        buttonImage.color = new Color(0.9f, 0.3f, 0.3f, 0.9f);

        // 添加Button组件
        Button detailCloseButton = detailCloseButtonGO.AddComponent<Button>();

        // 绑定关闭事件
        detailCloseButton.onClick.AddListener(() => {
            Debug.Log("[EncyclopediaCanvasFixer] 详情面板关闭按钮被点击");
            encyclopediaUI.CloseDetailPanel();
        });

        // 创建X文字
        GameObject closeTextGO = new GameObject("CloseText");
        closeTextGO.transform.SetParent(detailCloseButtonGO.transform, false);

        RectTransform textRect = closeTextGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text closeText = closeTextGO.AddComponent<Text>();
        closeText.text = "×";
        closeText.fontSize = 18;
        closeText.color = Color.white;
        closeText.alignment = TextAnchor.MiddleCenter;
        closeText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        Debug.Log("[EncyclopediaCanvasFixer] 详情面板关闭按钮创建完成");
    }

    /// <summary>
    /// 修复详情页面布局
    /// </summary>
    private void FixDetailPanelLayout()
    {
        Debug.Log("[EncyclopediaCanvasFixer] === 开始修复详情页面布局 ===");

        var encyclopediaUI = FindObjectOfType<EncyclopediaUI>();
        if (encyclopediaUI == null)
        {
            Debug.LogWarning("[EncyclopediaCanvasFixer] 未找到EncyclopediaUI组件");
            return;
        }

        // 使用反射获取detailPanel
        var detailPanelField = typeof(EncyclopediaUI).GetField("detailPanel",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var detailPanel = detailPanelField?.GetValue(encyclopediaUI) as GameObject;

        if (detailPanel == null)
        {
            Debug.LogWarning("[EncyclopediaCanvasFixer] 未找到detailPanel");
            return;
        }

        // 1. 修复详情页面为全屏
        var rectTransform = detailPanel.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            Debug.Log($"[EncyclopediaCanvasFixer] 修复前: anchorMin={rectTransform.anchorMin}, anchorMax={rectTransform.anchorMax}");

            // 设置为全屏
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            Debug.Log("[EncyclopediaCanvasFixer] ✅ 详情页面已设置为全屏");
        }

        // 2. 修改背景为不透明
        var background = detailPanel.GetComponent<Image>();
        if (background != null)
        {
            var oldColor = background.color;
            var newColor = new Color(oldColor.r, oldColor.g, oldColor.b, 1.0f);
            background.color = newColor;

            Debug.Log($"[EncyclopediaCanvasFixer] ✅ 背景透明度: {oldColor.a:F2} → 1.00 (不透明)");
        }

        // 3. 增大字体大小
        FixDetailFontSizes(encyclopediaUI);

        Debug.Log("[EncyclopediaCanvasFixer] === 详情页面布局修复完成 ===");
    }

    /// <summary>
    /// 修复详情页面字体大小
    /// </summary>
    private void FixDetailFontSizes(EncyclopediaUI encyclopediaUI)
    {
        // 使用反射获取文本组件
        var detailTitleField = typeof(EncyclopediaUI).GetField("detailTitle",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var detailDescriptionField = typeof(EncyclopediaUI).GetField("detailDescription",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var detailPropertiesField = typeof(EncyclopediaUI).GetField("detailProperties",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var detailTitle = detailTitleField?.GetValue(encyclopediaUI) as Text;
        var detailDescription = detailDescriptionField?.GetValue(encyclopediaUI) as Text;
        var detailProperties = detailPropertiesField?.GetValue(encyclopediaUI) as Text;

        int updatedCount = 0;

        if (detailTitle != null)
        {
            Debug.Log($"[EncyclopediaCanvasFixer] 标题字体: {detailTitle.fontSize} → 36");
            detailTitle.fontSize = 36;
            updatedCount++;
        }

        if (detailDescription != null)
        {
            Debug.Log($"[EncyclopediaCanvasFixer] 描述字体: {detailDescription.fontSize} → 24");
            detailDescription.fontSize = 24;
            updatedCount++;
        }

        if (detailProperties != null)
        {
            Debug.Log($"[EncyclopediaCanvasFixer] 属性字体: {detailProperties.fontSize} → 22");
            detailProperties.fontSize = 22;
            updatedCount++;
        }

        Debug.Log($"[EncyclopediaCanvasFixer] ✅ 已更新 {updatedCount} 个文本组件的字体大小");
    }

    /// <summary>
    /// 手动触发修复（用于测试）
    /// </summary>
    [ContextMenu("手动应用修复")]
    public void ManualFix()
    {
        ApplyFixes();
    }
}
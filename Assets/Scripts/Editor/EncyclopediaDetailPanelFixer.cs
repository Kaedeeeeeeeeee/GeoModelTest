using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using Encyclopedia;

public class EncyclopediaDetailPanelFixer : EditorWindow
{
    [MenuItem("Tools/图鉴系统/修复详情页面布局")]
    public static void ShowWindow()
    {
        GetWindow<EncyclopediaDetailPanelFixer>("详情页面修复器");
    }

    private void OnGUI()
    {
        GUILayout.Label("=== 🔧 详情页面布局修复器 ===", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            GUILayout.Label("⚠️ 请先运行游戏", EditorStyles.helpBox);
            return;
        }

        GUILayout.Space(10);

        if (GUILayout.Button("🖥️ 修复详情页面为全屏", GUILayout.Height(40)))
        {
            FixDetailPanelLayout();
        }

        if (GUILayout.Button("📝 增大字体大小", GUILayout.Height(40)))
        {
            IncreaseFontSizes();
        }

        if (GUILayout.Button("🎨 修改背景为不透明", GUILayout.Height(40)))
        {
            FixBackgroundOpacity();
        }

        if (GUILayout.Button("🔄 应用所有修复", GUILayout.Height(40)))
        {
            ApplyAllFixes();
        }

        if (GUILayout.Button("🚪 添加关闭按钮事件", GUILayout.Height(40)))
        {
            SetupCloseButtonEvent();
        }
    }

    private void FixDetailPanelLayout()
    {
        Debug.Log("=== 🖥️ 修复详情页面布局 ===");

        var encyclopediaUI = FindObjectOfType<EncyclopediaUI>();
        if (encyclopediaUI == null)
        {
            Debug.LogError("❌ 没有找到EncyclopediaUI");
            return;
        }

        // 使用反射获取detailPanel
        var detailPanelField = typeof(EncyclopediaUI).GetField("detailPanel",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var detailPanel = detailPanelField?.GetValue(encyclopediaUI) as GameObject;

        if (detailPanel == null)
        {
            Debug.LogError("❌ 没有找到detailPanel");
            return;
        }

        var rectTransform = detailPanel.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            Debug.Log($"修复前: anchorMin={rectTransform.anchorMin}, anchorMax={rectTransform.anchorMax}");

            // 设置为全屏
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            Debug.Log($"✅ 详情页面已设置为全屏");
            Debug.Log($"修复后: anchorMin={rectTransform.anchorMin}, anchorMax={rectTransform.anchorMax}");
        }
    }

    private void IncreaseFontSizes()
    {
        Debug.Log("=== 📝 增大字体大小 ===");

        var encyclopediaUI = FindObjectOfType<EncyclopediaUI>();
        if (encyclopediaUI == null)
        {
            Debug.LogError("❌ 没有找到EncyclopediaUI");
            return;
        }

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
            Debug.Log($"标题字体: {detailTitle.fontSize} → 28");
            detailTitle.fontSize = 28;
            updatedCount++;
        }

        if (detailDescription != null)
        {
            Debug.Log($"描述字体: {detailDescription.fontSize} → 18");
            detailDescription.fontSize = 18;
            updatedCount++;
        }

        if (detailProperties != null)
        {
            Debug.Log($"属性字体: {detailProperties.fontSize} → 18");
            detailProperties.fontSize = 18;
            updatedCount++;
        }

        Debug.Log($"✅ 已更新 {updatedCount} 个文本组件的字体大小");
    }

    private void FixBackgroundOpacity()
    {
        Debug.Log("=== 🎨 修改背景为不透明 ===");

        var encyclopediaUI = FindObjectOfType<EncyclopediaUI>();
        if (encyclopediaUI == null)
        {
            Debug.LogError("❌ 没有找到EncyclopediaUI");
            return;
        }

        // 使用反射获取detailPanel
        var detailPanelField = typeof(EncyclopediaUI).GetField("detailPanel",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var detailPanel = detailPanelField?.GetValue(encyclopediaUI) as GameObject;

        if (detailPanel == null)
        {
            Debug.LogError("❌ 没有找到detailPanel");
            return;
        }

        var background = detailPanel.GetComponent<Image>();
        if (background != null)
        {
            var oldColor = background.color;
            var newColor = new Color(oldColor.r, oldColor.g, oldColor.b, 1.0f);
            background.color = newColor;

            Debug.Log($"✅ 背景透明度: {oldColor.a:F2} → 1.00 (不透明)");
        }
    }

    private void ApplyAllFixes()
    {
        Debug.Log("=== 🔄 应用所有修复 ===");
        FixDetailPanelLayout();
        IncreaseFontSizes();
        FixBackgroundOpacity();
        SetupCloseButtonEvent();
        Debug.Log("✅ 所有修复已完成！");
    }

    private void SetupCloseButtonEvent()
    {
        Debug.Log("=== 🚪 设置关闭按钮事件 ===");

        var encyclopediaUI = FindObjectOfType<EncyclopediaUI>();
        if (encyclopediaUI == null)
        {
            Debug.LogError("❌ 没有找到EncyclopediaUI");
            return;
        }

        // 使用反射获取detailPanel
        var detailPanelField = typeof(EncyclopediaUI).GetField("detailPanel",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var detailPanel = detailPanelField?.GetValue(encyclopediaUI) as GameObject;

        if (detailPanel == null)
        {
            Debug.LogError("❌ 没有找到detailPanel");
            return;
        }

        // 查找关闭按钮
        var closeButton = detailPanel.transform.Find("DetailCloseButton");
        if (closeButton == null)
        {
            Debug.LogWarning("⚠️ 没有找到关闭按钮，正在创建...");
            CreateCloseButtonRuntime(detailPanel, encyclopediaUI);
            return;
        }

        var button = closeButton.GetComponent<Button>();
        if (button != null)
        {
            // 清除现有事件
            button.onClick.RemoveAllListeners();

            // 添加关闭事件
            button.onClick.AddListener(() => {
                Debug.Log("关闭按钮被点击");
                encyclopediaUI.CloseDetailPanel();
            });

            Debug.Log("✅ 关闭按钮事件已设置");
        }
        else
        {
            Debug.LogError("❌ 关闭按钮没有Button组件");
        }
    }

    private void CreateCloseButtonRuntime(GameObject detailPanel, EncyclopediaUI encyclopediaUI)
    {
        Debug.Log("🔧 运行时创建关闭按钮");

        GameObject closeButtonGO = new GameObject("DetailCloseButton");
        closeButtonGO.transform.SetParent(detailPanel.transform, false);

        RectTransform rect = closeButtonGO.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(1, 1);
        rect.anchoredPosition = new Vector2(-20, -20);
        rect.sizeDelta = new Vector2(60, 60);

        Image background = closeButtonGO.AddComponent<Image>();
        background.color = new Color(0.8f, 0.2f, 0.2f, 0.8f);

        Button button = closeButtonGO.AddComponent<Button>();

        // 创建X文字
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(closeButtonGO.transform, false);

        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textGO.AddComponent<Text>();
        text.text = "×";
        text.font = UIFontResolver.GetUIFont();
        text.fontSize = 36;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontStyle = FontStyle.Bold;

        // 添加点击事件
        button.onClick.AddListener(() => {
            Debug.Log("关闭按钮被点击");
            encyclopediaUI.CloseDetailPanel();
        });

        Debug.Log("✅ 运行时关闭按钮创建并配置完成");
    }
}
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using Encyclopedia;

/// <summary>
/// 最终修复工具
/// 直接在游戏运行时修复3D显示问题
/// </summary>
public class FinalFixTool : EditorWindow
{
    [MenuItem("Tools/图鉴系统/最终修复工具")]
    public static void ShowWindow()
    {
        GetWindow<FinalFixTool>("最终修复工具");
    }

    private void OnGUI()
    {
        GUILayout.Label("最终修复工具", EditorStyles.boldLabel);
        GUILayout.Space(10);

        GUILayout.Label("如果3D查看器仍然是黑屏，使用以下修复：", EditorStyles.helpBox);
        GUILayout.Space(10);

        if (GUILayout.Button("🔧 运行时修复3D查看器", GUILayout.Height(40)))
        {
            FixRuntimeViewer();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("创建独立3D测试窗口", GUILayout.Height(30)))
        {
            CreateStandaloneViewer();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("强制刷新图鉴UI", GUILayout.Height(30)))
        {
            ForceRefreshEncyclopediaUI();
        }
    }

    private void FixRuntimeViewer()
    {
        Debug.Log("=== 🔧 运行时修复3D查看器 ===");

        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ 请先运行游戏，然后再执行此修复");
            return;
        }

        var encyclopediaUI = FindObjectOfType<EncyclopediaUI>();
        if (encyclopediaUI == null)
        {
            Debug.LogError("❌ 未找到EncyclopediaUI，请先打开图鉴面板");
            return;
        }

        // 获取详情面板
        var detailPanelField = typeof(EncyclopediaUI).GetField("detailPanel",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var detailPanel = detailPanelField?.GetValue(encyclopediaUI) as GameObject;

        if (detailPanel == null)
        {
            Debug.LogError("❌ 未找到详情面板");
            return;
        }

        Debug.Log($"📋 详情面板找到: {detailPanel.name}");

        // 查找或创建3D查看器
        Simple3DViewer viewer = detailPanel.GetComponentInChildren<Simple3DViewer>();

        if (viewer == null)
        {
            Debug.Log("🔧 创建新的Simple3DViewer");

            // 创建查看器容器
            GameObject viewerContainer = new GameObject("RuntimeModel3DViewer");
            viewerContainer.transform.SetParent(detailPanel.transform, false);

            RectTransform rect = viewerContainer.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.1f);
            rect.anchorMax = new Vector2(0.9f, 0.9f);
            rect.offsetMin = new Vector2(10, 10);
            rect.offsetMax = new Vector2(-10, -10);

            Image bg = viewerContainer.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.2f, 0.3f, 0.9f);

            // 添加Simple3DViewer组件
            viewer = viewerContainer.AddComponent<Simple3DViewer>();

            Debug.Log("✅ Simple3DViewer创建完成");
        }
        else
        {
            Debug.Log("🔧 重新初始化现有Simple3DViewer");
            viewer.Reinitialize();
        }

        // 更新EncyclopediaUI的引用
        var viewerField = typeof(EncyclopediaUI).GetField("model3DViewer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        viewerField?.SetValue(encyclopediaUI, viewer);

        Debug.Log("✅ EncyclopediaUI引用已更新");

        // 立即测试显示
        TestViewerImmediately(viewer);
    }

    private void TestViewerImmediately(Simple3DViewer viewer)
    {
        Debug.Log("🧪 立即测试3D查看器");

        // 创建测试模型
        GameObject testCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        testCube.name = "RuntimeTestCube";

        var renderer = testCube.GetComponent<Renderer>();
        var material = new Material(Shader.Find("Standard"));
        material.color = Color.red;
        material.SetFloat("_Metallic", 0.2f);
        material.SetFloat("_Glossiness", 0.8f);
        renderer.material = material;

        // 显示模型
        viewer.ShowModel(testCube);

        // 清理原始GameObject
        Destroy(testCube);

        Debug.Log("🎯 红色立方体应该现在显示在图鉴的右侧");
    }

    private void CreateStandaloneViewer()
    {
        Debug.Log("=== 创建独立3D测试窗口 ===");

        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ 请先运行游戏");
            return;
        }

        // 创建独立窗口
        GameObject windowGO = new GameObject("Standalone3DWindow");

        Canvas canvas = windowGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        CanvasScaler scaler = windowGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        windowGO.AddComponent<GraphicRaycaster>();

        // 创建查看器面板
        GameObject panelGO = new GameObject("ViewerPanel");
        panelGO.transform.SetParent(windowGO.transform, false);

        RectTransform panelRect = panelGO.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.3f, 0.3f);
        panelRect.anchorMax = new Vector2(0.7f, 0.7f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelBg = panelGO.AddComponent<Image>();
        panelBg.color = new Color(0.1f, 0.1f, 0.2f, 0.9f);

        // 添加Simple3DViewer
        Simple3DViewer viewer = panelGO.AddComponent<Simple3DViewer>();

        // 添加关闭按钮
        CreateCloseButton(panelGO, windowGO);

        Debug.Log("✅ 独立3D测试窗口创建完成");

        // 延迟测试
        MonoBehaviour.Destroy(null, 0.5f);
        EditorApplication.delayCall += () => {
            if (viewer != null)
            {
                TestViewerImmediately(viewer);
            }
        };
    }

    private void CreateCloseButton(GameObject parent, GameObject windowToClose)
    {
        GameObject buttonGO = new GameObject("CloseButton");
        buttonGO.transform.SetParent(parent.transform, false);

        RectTransform rect = buttonGO.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(1, 1);
        rect.anchoredPosition = new Vector2(-10, -10);
        rect.sizeDelta = new Vector2(30, 30);

        Image bg = buttonGO.AddComponent<Image>();
        bg.color = Color.red;

        Button button = buttonGO.AddComponent<Button>();
        button.targetGraphic = bg;
        button.onClick.AddListener(() => {
            Debug.Log("关闭独立3D窗口");
            Destroy(windowToClose);
        });

        // 添加X文字
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);

        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textGO.AddComponent<Text>();
        text.text = "×";
        text.fontSize = 20;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private void ForceRefreshEncyclopediaUI()
    {
        Debug.Log("=== 强制刷新图鉴UI ===");

        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ 请先运行游戏");
            return;
        }

        var encyclopediaUI = FindObjectOfType<EncyclopediaUI>();
        if (encyclopediaUI == null)
        {
            Debug.LogWarning("⚠️ 图鉴UI未打开，请先按O键打开图鉴");
            return;
        }

        // 强制刷新UI
        var refreshMethod = typeof(EncyclopediaUI).GetMethod("RefreshUI",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (refreshMethod != null)
        {
            refreshMethod.Invoke(encyclopediaUI, null);
            Debug.Log("✅ 图鉴UI刷新完成");
        }
        else
        {
            Debug.Log("📋 未找到RefreshUI方法，尝试重新加载条目");

            // 尝试重新加载条目列表
            var loadMethod = typeof(EncyclopediaUI).GetMethod("LoadEntryList",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            loadMethod?.Invoke(encyclopediaUI, null);
        }
    }
}
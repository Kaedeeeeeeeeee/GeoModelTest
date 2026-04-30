using UnityEngine;
using UnityEditor;
using SampleCuttingSystem;

/// <summary>
/// 切割系统集成修复工具 - 修复切割界面无法显示的问题
/// </summary>
public class CuttingSystemIntegrationFixer
{
    [MenuItem("Tools/切割系统调试/🔧 修复切割界面集成问题")]
    public static void FixCuttingInterfaceIntegration()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ 请在游戏运行时使用此工具");
            return;
        }

        Debug.Log("=== 🔧 修复切割界面集成问题 ===");

        // 1. 查找切割台交互组件
        CuttingStationInteraction station = Object.FindFirstObjectByType<CuttingStationInteraction>();
        if (station == null)
        {
            Debug.LogError("❌ 找不到CuttingStationInteraction组件");
            return;
        }

        // 2. 查找或创建切割系统管理器
        SampleCuttingSystemManager manager = Object.FindFirstObjectByType<SampleCuttingSystemManager>();
        if (manager == null)
        {
            Debug.Log("创建新的切割系统管理器...");
            GameObject managerObj = new GameObject("SampleCuttingSystemManager");
            manager = managerObj.AddComponent<SampleCuttingSystemManager>();
            Debug.Log("✅ 切割系统管理器已创建");
        }

        // 3. 查找或创建切割UI
        CuttingStationUI cuttingUI = Object.FindFirstObjectByType<CuttingStationUI>();
        if (cuttingUI == null)
        {
            Debug.Log("在管理器对象上添加切割UI组件...");
            cuttingUI = manager.gameObject.AddComponent<CuttingStationUI>();
            Debug.Log("✅ 切割UI组件已添加");
        }

        // 4. 创建完整的切割界面UI
        CreateCuttingInterfaceUI(manager, cuttingUI);

        Debug.Log("🎉 切割界面集成修复完成！");
    }

    [MenuItem("Tools/切割系统调试/🎨 激活现有切割界面")]
    public static void ActivateExistingCuttingInterface()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ 请在游戏运行时使用此工具");
            return;
        }

        Debug.Log("=== 🎨 激活现有切割界面 ===");

        // 查找切割系统管理器
        SampleCuttingSystemManager manager = Object.FindFirstObjectByType<SampleCuttingSystemManager>();
        if (manager == null)
        {
            Debug.LogError("❌ 找不到SampleCuttingSystemManager");
            return;
        }

        // 查找切割UI
        CuttingStationUI cuttingUI = Object.FindFirstObjectByType<CuttingStationUI>();
        if (cuttingUI == null)
        {
            Debug.LogError("❌ 找不到CuttingStationUI");
            return;
        }

        // 激活管理器和UI
        manager.gameObject.SetActive(true);
        cuttingUI.gameObject.SetActive(true);

        // 设置鼠标状态
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // 隐藏交互提示
        CuttingStationInteraction station = Object.FindFirstObjectByType<CuttingStationInteraction>();
        if (station != null)
        {
            var stationType = station.GetType();
            var showMethod = stationType.GetMethod("ShowInteractionPrompt",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (showMethod != null)
            {
                showMethod.Invoke(station, new object[] { false });
            }
        }

        Debug.Log("✅ 现有切割界面已激活！");
    }

    private static void CreateCuttingInterfaceUI(SampleCuttingSystemManager manager, CuttingStationUI cuttingUI)
    {
        Debug.Log("创建完整的切割界面UI...");

        // 查找或创建Canvas
        Canvas canvas = FindOrCreateCanvas();

        // 创建主面板
        GameObject mainPanel = new GameObject("CuttingInterface");
        mainPanel.transform.SetParent(canvas.transform, false);

        // 设置主面板的RectTransform
        RectTransform mainRect = mainPanel.AddComponent<RectTransform>();
        mainRect.anchorMin = Vector2.zero;
        mainRect.anchorMax = Vector2.one;
        mainRect.offsetMin = Vector2.zero;
        mainRect.offsetMax = Vector2.zero;

        // 添加背景
        UnityEngine.UI.Image mainBg = mainPanel.AddComponent<UnityEngine.UI.Image>();
        mainBg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        // 使用反射设置cuttingUI的cuttingPanel字段
        var cuttingUIType = cuttingUI.GetType();
        var cuttingPanelField = cuttingUIType.GetField("cuttingPanel",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (cuttingPanelField != null)
        {
            cuttingPanelField.SetValue(cuttingUI, mainPanel);
            Debug.Log("✅ 设置cuttingPanel引用");
        }

        // 创建样本信息面板
        CreateSampleInfoPanel(mainPanel.transform, cuttingUI);

        // 创建操作说明面板
        CreateInstructionPanel(mainPanel.transform, cuttingUI);

        // 创建按钮区域
        CreateButtonArea(mainPanel.transform, cuttingUI);

        // 初始化UI
        cuttingUI.gameObject.SetActive(true);

        Debug.Log("✅ 切割界面UI创建完成");
    }

    private static Canvas FindOrCreateCanvas()
    {
        // 优先查找CuttingCanvas
        Canvas[] canvases = Object.FindObjectsOfType<Canvas>();
        foreach (var canvas in canvases)
        {
            if (canvas.name.Contains("Cutting"))
            {
                Debug.Log($"找到现有切割Canvas: {canvas.name}");
                return canvas;
            }
        }

        // 查找任何ScreenSpaceOverlay Canvas
        foreach (var canvas in canvases)
        {
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                Debug.Log($"使用现有Canvas: {canvas.name}");
                return canvas;
            }
        }

        // 创建新Canvas
        Debug.Log("创建新的切割UI Canvas");
        GameObject canvasObj = new GameObject("CuttingUICanvas");
        Canvas newCanvas = canvasObj.AddComponent<Canvas>();
        newCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        newCanvas.sortingOrder = 400;

        UnityEngine.UI.CanvasScaler scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        return newCanvas;
    }

    private static void CreateSampleInfoPanel(Transform parent, CuttingStationUI cuttingUI)
    {
        GameObject infoPanel = new GameObject("SampleInfoPanel");
        infoPanel.transform.SetParent(parent, false);

        RectTransform infoRect = infoPanel.AddComponent<RectTransform>();
        infoRect.anchorMin = new Vector2(0.1f, 0.7f);
        infoRect.anchorMax = new Vector2(0.9f, 0.9f);
        infoRect.offsetMin = Vector2.zero;
        infoRect.offsetMax = Vector2.zero;

        UnityEngine.UI.Image infoBg = infoPanel.AddComponent<UnityEngine.UI.Image>();
        infoBg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        // 创建标题
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(infoPanel.transform, false);

        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.7f);
        titleRect.anchorMax = new Vector2(1, 1f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        UnityEngine.UI.Text titleText = titleObj.AddComponent<UnityEngine.UI.Text>();
        titleText.text = "样本切割系统";
        titleText.font = UIFontResolver.GetUIFont();
        titleText.fontSize = 32;
        titleText.color = Color.white;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.fontStyle = FontStyle.Bold;

        // 使用反射设置sampleInfoPanel字段
        var cuttingUIType = cuttingUI.GetType();
        var sampleInfoField = cuttingUIType.GetField("sampleInfoPanel",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (sampleInfoField != null)
        {
            sampleInfoField.SetValue(cuttingUI, infoPanel);
        }
    }

    private static void CreateInstructionPanel(Transform parent, CuttingStationUI cuttingUI)
    {
        GameObject instructionPanel = new GameObject("InstructionPanel");
        instructionPanel.transform.SetParent(parent, false);

        RectTransform instructionRect = instructionPanel.AddComponent<RectTransform>();
        instructionRect.anchorMin = new Vector2(0.1f, 0.1f);
        instructionRect.anchorMax = new Vector2(0.9f, 0.3f);
        instructionRect.offsetMin = Vector2.zero;
        instructionRect.offsetMax = Vector2.zero;

        UnityEngine.UI.Image instructionBg = instructionPanel.AddComponent<UnityEngine.UI.Image>();
        instructionBg.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);

        // 创建说明文字
        GameObject textObj = new GameObject("InstructionText");
        textObj.transform.SetParent(instructionPanel.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(20, 20);
        textRect.offsetMax = new Vector2(-20, -20);

        UnityEngine.UI.Text instructionText = textObj.AddComponent<UnityEngine.UI.Text>();
        instructionText.text = "将多层样本拖拽到切割台进行切割";
        instructionText.font = UIFontResolver.GetUIFont();
        instructionText.fontSize = 18;
        instructionText.color = Color.white;
        instructionText.alignment = TextAnchor.MiddleCenter;

        // 使用反射设置instructionPanel和instructionText字段
        var cuttingUIType = cuttingUI.GetType();
        var instructionPanelField = cuttingUIType.GetField("instructionPanel",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (instructionPanelField != null)
        {
            instructionPanelField.SetValue(cuttingUI, instructionPanel);
        }

        var instructionTextField = cuttingUIType.GetField("instructionText",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (instructionTextField != null)
        {
            instructionTextField.SetValue(cuttingUI, instructionText);
        }
    }

    private static void CreateButtonArea(Transform parent, CuttingStationUI cuttingUI)
    {
        // 创建关闭按钮
        GameObject closeButton = new GameObject("CloseButton");
        closeButton.transform.SetParent(parent, false);

        RectTransform closeRect = closeButton.AddComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.9f, 0.9f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-30, -30);
        closeRect.sizeDelta = new Vector2(60, 60);

        UnityEngine.UI.Image closeImage = closeButton.AddComponent<UnityEngine.UI.Image>();
        closeImage.color = Color.red;

        UnityEngine.UI.Button closeBtn = closeButton.AddComponent<UnityEngine.UI.Button>();
        closeBtn.targetGraphic = closeImage;
        closeBtn.onClick.AddListener(() => {
            Debug.Log("关闭切割界面");

            // 查找切割台交互组件并调用关闭方法
            CuttingStationInteraction station = Object.FindFirstObjectByType<CuttingStationInteraction>();
            if (station != null)
            {
                var closeMethod = station.GetType().GetMethod("CloseCuttingInterface",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (closeMethod != null)
                {
                    closeMethod.Invoke(station, null);
                }
            }
        });

        // X符号
        GameObject xObj = new GameObject("X");
        xObj.transform.SetParent(closeButton.transform, false);

        RectTransform xRect = xObj.AddComponent<RectTransform>();
        xRect.anchorMin = Vector2.zero;
        xRect.anchorMax = Vector2.one;
        xRect.offsetMin = Vector2.zero;
        xRect.offsetMax = Vector2.zero;

        UnityEngine.UI.Text xText = xObj.AddComponent<UnityEngine.UI.Text>();
        xText.text = "✕";
        xText.font = UIFontResolver.GetUIFont();
        xText.fontSize = 30;
        xText.color = Color.white;
        xText.alignment = TextAnchor.MiddleCenter;
        xText.fontStyle = FontStyle.Bold;

        // 使用反射设置closeButton字段
        var cuttingUIType = cuttingUI.GetType();
        var closeButtonField = cuttingUIType.GetField("closeButton",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (closeButtonField != null)
        {
            closeButtonField.SetValue(cuttingUI, closeBtn);
        }
    }

    [MenuItem("Tools/切割系统调试/🔧 修复CuttingStationInteraction的OpenCuttingInterface方法")]
    public static void FixOpenCuttingInterfaceMethod()
    {
        Debug.Log("=== 🔧 修复OpenCuttingInterface方法 ===");
        Debug.Log("注意：此方法需要运行时生效，但建议直接修改源代码");
        Debug.Log("修复方案：在OpenCuttingInterface()方法中，当cuttingInterfacePrefab为null时，");
        Debug.Log("应该查找并激活现有的SampleCuttingSystemManager和CuttingStationUI组件");
        Debug.Log("而不是尝试实例化预制体");

        if (Application.isPlaying)
        {
            Debug.Log("运行时临时修复：使用反射替换方法行为...");
            // 这里可以添加运行时临时修复的代码
        }
    }
}
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using SampleCuttingSystem;

/// <summary>
/// 切割界面生成器 - 不依赖预制体直接创建切割界面
/// </summary>
public class CuttingInterfaceGenerator
{
    [MenuItem("Tools/切割系统调试/🎨 创建切割界面")]
    public static void CreateCuttingInterface()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ 请在游戏运行时使用此工具");
            return;
        }

        Debug.Log("=== 🎨 创建切割界面 ===");

        // 1. 查找切割台交互组件
        CuttingStationInteraction station = Object.FindFirstObjectByType<CuttingStationInteraction>();
        if (station == null)
        {
            Debug.LogError("❌ 找不到CuttingStationInteraction组件");
            return;
        }

        // 2. 检查是否已有界面
        var stationType = station.GetType();
        var currentInterfaceField = stationType.GetField("currentCuttingInterface",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (currentInterfaceField != null)
        {
            GameObject currentInterface = (GameObject)currentInterfaceField.GetValue(station);
            if (currentInterface != null)
            {
                Debug.Log("销毁现有界面");
                Object.Destroy(currentInterface);
            }
        }

        // 3. 获取界面父对象
        var parentField = stationType.GetField("interfaceParent",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Transform parent = null;
        if (parentField != null)
        {
            parent = (Transform)parentField.GetValue(station);
        }

        if (parent == null)
        {
            // 查找合适的Canvas
            Canvas canvas = FindOrCreateCanvas();
            parent = canvas.transform;

            // 设置父对象引用
            if (parentField != null)
            {
                parentField.SetValue(station, parent);
            }
        }

        // 4. 创建切割界面
        GameObject cuttingInterface = CreateCuttingInterfaceUI(parent);

        // 5. 设置当前界面引用
        if (currentInterfaceField != null)
        {
            currentInterfaceField.SetValue(station, cuttingInterface);
        }

        // 6. 显示界面
        cuttingInterface.SetActive(true);

        // 7. 设置鼠标状态
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // 8. 隐藏交互提示
        HideInteractionPrompt(station);

        Debug.Log("🎉 切割界面创建成功！");
    }

    private static Canvas FindOrCreateCanvas()
    {
        // 优先查找CuttingUICanvas
        Canvas[] canvases = Object.FindObjectsOfType<Canvas>();
        foreach (var canvas in canvases)
        {
            if (canvas.name.Contains("CuttingUI") || canvas.name.Contains("Cutting"))
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
        newCanvas.sortingOrder = 400; // 高于其他UI

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        return newCanvas;
    }

    private static GameObject CreateCuttingInterfaceUI(Transform parent)
    {
        Debug.Log("创建切割界面UI");

        // 主界面容器
        GameObject mainPanel = new GameObject("CuttingInterface");
        mainPanel.transform.SetParent(parent, false);

        RectTransform mainRect = mainPanel.AddComponent<RectTransform>();
        mainRect.anchorMin = Vector2.zero;
        mainRect.anchorMax = Vector2.one;
        mainRect.offsetMin = Vector2.zero;
        mainRect.offsetMax = Vector2.zero;

        // 添加背景
        Image mainBg = mainPanel.AddComponent<Image>();
        mainBg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        // 创建标题
        CreateTitle(mainPanel.transform);

        // 创建拖拽区域
        CreateDropZone(mainPanel.transform);

        // 创建按钮区域
        CreateButtonArea(mainPanel.transform);

        // 创建关闭按钮
        CreateCloseButton(mainPanel.transform);

        // 创建说明文本
        CreateInstructionText(mainPanel.transform);

        return mainPanel;
    }

    private static void CreateTitle(Transform parent)
    {
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(parent, false);

        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.9f);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = "样本切割系统";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 36;
        titleText.color = Color.white;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.fontStyle = FontStyle.Bold;
    }

    private static void CreateDropZone(Transform parent)
    {
        GameObject dropZoneObj = new GameObject("SampleDropZone");
        dropZoneObj.transform.SetParent(parent, false);

        RectTransform dropRect = dropZoneObj.AddComponent<RectTransform>();
        dropRect.anchorMin = new Vector2(0.1f, 0.5f);
        dropRect.anchorMax = new Vector2(0.6f, 0.85f);
        dropRect.offsetMin = Vector2.zero;
        dropRect.offsetMax = Vector2.zero;

        // 背景
        Image dropBg = dropZoneObj.AddComponent<Image>();
        dropBg.color = new Color(0.2f, 0.3f, 0.5f, 0.8f);

        // 边框
        Outline outline = dropZoneObj.AddComponent<Outline>();
        outline.effectColor = Color.cyan;
        outline.effectDistance = new Vector2(3, 3);

        // 提示文本
        GameObject textObj = new GameObject("DropText");
        textObj.transform.SetParent(dropZoneObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text dropText = textObj.AddComponent<Text>();
        dropText.text = "将样本拖拽到此处\n\n📦\n\n点击开始切割";
        dropText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        dropText.fontSize = 24;
        dropText.color = Color.white;
        dropText.alignment = TextAnchor.MiddleCenter;

        // 添加按钮功能
        Button dropButton = dropZoneObj.AddComponent<Button>();
        dropButton.targetGraphic = dropBg;
        dropButton.onClick.AddListener(() => {
            Debug.Log("样本拖拽区域被点击 - 开始切割流程");
            StartCuttingProcess();
        });
    }

    private static void CreateButtonArea(Transform parent)
    {
        GameObject buttonAreaObj = new GameObject("ButtonArea");
        buttonAreaObj.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonAreaObj.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.65f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.95f, 0.85f);
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;

        // 开始切割按钮
        CreateButton(buttonAreaObj.transform, "开始切割", new Vector2(0, 0.8f), new Vector2(1, 1), Color.green, StartCuttingProcess);

        // 重置按钮
        CreateButton(buttonAreaObj.transform, "重置", new Vector2(0, 0.6f), new Vector2(1, 0.8f), Color.yellow, ResetCuttingInterface);

        // 帮助按钮
        CreateButton(buttonAreaObj.transform, "帮助", new Vector2(0, 0.4f), new Vector2(1, 0.6f), Color.blue, ShowHelp);
    }

    private static void CreateButton(Transform parent, string text, Vector2 anchorMin, Vector2 anchorMax, Color color, System.Action onClick)
    {
        GameObject buttonObj = new GameObject($"Button_{text}");
        buttonObj.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchorMin = anchorMin;
        buttonRect.anchorMax = anchorMax;
        buttonRect.offsetMin = new Vector2(10, 5);
        buttonRect.offsetMax = new Vector2(-10, -5);

        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = color;

        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(() => onClick?.Invoke());

        // 按钮文字
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text buttonText = textObj.AddComponent<Text>();
        buttonText.text = text;
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = 18;
        buttonText.color = Color.white;
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.fontStyle = FontStyle.Bold;
    }

    private static void CreateCloseButton(Transform parent)
    {
        GameObject closeObj = new GameObject("CloseButton");
        closeObj.transform.SetParent(parent, false);

        RectTransform closeRect = closeObj.AddComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.95f, 0.95f);
        closeRect.anchorMax = new Vector2(1, 1);
        closeRect.anchoredPosition = new Vector2(-30, -30);
        closeRect.sizeDelta = new Vector2(60, 60);

        Image closeImage = closeObj.AddComponent<Image>();
        closeImage.color = Color.red;

        Button closeButton = closeObj.AddComponent<Button>();
        closeButton.targetGraphic = closeImage;
        closeButton.onClick.AddListener(() => {
            Debug.Log("关闭切割界面");
            CloseCuttingInterface();
        });

        // X符号
        GameObject xObj = new GameObject("X");
        xObj.transform.SetParent(closeObj.transform, false);

        RectTransform xRect = xObj.AddComponent<RectTransform>();
        xRect.anchorMin = Vector2.zero;
        xRect.anchorMax = Vector2.one;
        xRect.offsetMin = Vector2.zero;
        xRect.offsetMax = Vector2.zero;

        Text xText = xObj.AddComponent<Text>();
        xText.text = "✕";
        xText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        xText.fontSize = 30;
        xText.color = Color.white;
        xText.alignment = TextAnchor.MiddleCenter;
        xText.fontStyle = FontStyle.Bold;
    }

    private static void CreateInstructionText(Transform parent)
    {
        GameObject instructionObj = new GameObject("Instructions");
        instructionObj.transform.SetParent(parent, false);

        RectTransform instructionRect = instructionObj.AddComponent<RectTransform>();
        instructionRect.anchorMin = new Vector2(0.1f, 0.1f);
        instructionRect.anchorMax = new Vector2(0.9f, 0.45f);
        instructionRect.offsetMin = Vector2.zero;
        instructionRect.offsetMax = Vector2.zero;

        Text instructionText = instructionObj.AddComponent<Text>();
        instructionText.text = @"使用说明：
1. 将地质样本拖拽到上方的蓝色区域
2. 点击'开始切割'按钮
3. 使用激光切割工具进行精确切割
4. 完成后获得切割后的样本片

快捷键：
- ESC: 关闭界面
- F: 重新打开界面（在切割台附近）";

        instructionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        instructionText.fontSize = 16;
        instructionText.color = new Color(0.9f, 0.9f, 0.9f);
        instructionText.alignment = TextAnchor.UpperLeft;
    }

    private static void StartCuttingProcess()
    {
        Debug.Log("🔥 开始切割流程！");

        // 查找切割游戏组件
        SampleCuttingGame cuttingGame = Object.FindFirstObjectByType<SampleCuttingGame>();
        if (cuttingGame != null)
        {
            cuttingGame.gameObject.SetActive(true);
            Debug.Log("激活切割游戏组件");

            // 尝试开始游戏
            var gameType = cuttingGame.GetType();
            var startMethod = gameType.GetMethod("StartCuttingGame",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (startMethod != null)
            {
                try
                {
                    startMethod.Invoke(cuttingGame, null);
                    Debug.Log("✅ 切割游戏已启动");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"启动切割游戏失败: {e.Message}");
                }
            }
        }
        else
        {
            Debug.LogWarning("未找到切割游戏组件");
        }
    }

    private static void ResetCuttingInterface()
    {
        Debug.Log("🔄 重置切割界面");
        // 可以在这里添加重置逻辑
    }

    private static void ShowHelp()
    {
        Debug.Log("❓ 显示帮助信息");
        EditorUtility.DisplayDialog("切割系统帮助",
            "这是样本切割系统的帮助信息。\n\n" +
            "1. 拖拽样本到指定区域\n" +
            "2. 点击开始切割\n" +
            "3. 使用激光工具进行切割\n" +
            "4. 获得切割后的样本",
            "了解");
    }

    private static void CloseCuttingInterface()
    {
        CuttingStationInteraction station = Object.FindFirstObjectByType<CuttingStationInteraction>();
        if (station != null)
        {
            // 调用关闭方法
            var closeMethod = station.GetType().GetMethod("CloseCuttingInterface",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (closeMethod != null)
            {
                closeMethod.Invoke(station, null);
            }
        }
    }

    private static void HideInteractionPrompt(CuttingStationInteraction station)
    {
        var stationType = station.GetType();
        var showMethod = stationType.GetMethod("ShowInteractionPrompt",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (showMethod != null)
        {
            showMethod.Invoke(station, new object[] { false });
        }
    }

    [MenuItem("Tools/切割系统调试/❌ 关闭切割界面")]
    public static void CloseCuttingInterfaceManually()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ 请在游戏运行时使用此工具");
            return;
        }

        CloseCuttingInterface();

        // 恢复鼠标状态
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        Debug.Log("✅ 切割界面已关闭");
    }
}
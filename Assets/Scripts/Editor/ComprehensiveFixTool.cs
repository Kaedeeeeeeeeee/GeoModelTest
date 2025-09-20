using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Encyclopedia;

/// <summary>
/// 综合修复工具
/// 解决图鉴系统的所有问题：黑色方块、3D显示、Input System冲突
/// </summary>
public class ComprehensiveFixTool : EditorWindow
{
    [MenuItem("Tools/图鉴系统/一键修复所有问题")]
    public static void ShowWindow()
    {
        GetWindow<ComprehensiveFixTool>("图鉴系统综合修复工具");
    }

    private void OnGUI()
    {
        GUILayout.Label("图鉴系统综合修复工具", EditorStyles.boldLabel);
        GUILayout.Space(10);

        GUILayout.Label("解决所有图鉴问题：黑色方块、3D显示、Input System冲突", EditorStyles.helpBox);
        GUILayout.Space(10);

        if (GUILayout.Button("🔧 一键修复所有问题", GUILayout.Height(40)))
        {
            FixAllProblems();
        }

        GUILayout.Space(10);

        GUILayout.Label("单独修复选项：", EditorStyles.boldLabel);

        if (GUILayout.Button("清理多余面板和黑色方块", GUILayout.Height(30)))
        {
            CleanupPanelsAndIconImages();
        }

        if (GUILayout.Button("修复Input System冲突", GUILayout.Height(30)))
        {
            FixInputSystemConflict();
        }

        if (GUILayout.Button("修复3D查看器", GUILayout.Height(30)))
        {
            Fix3DViewer();
        }

        if (GUILayout.Button("测试整个系统", GUILayout.Height(30)))
        {
            TestCompleteSystem();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("🧹 清理所有测试对象", GUILayout.Height(25)))
        {
            CleanupAllTestObjects();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("修复重复EventSystem", GUILayout.Height(25)))
        {
            FixDuplicateEventSystems();
        }
    }

    private void FixAllProblems()
    {
        Debug.Log("=== 🔧 开始一键修复所有问题 ===");

        try
        {
            // 1. 清理面板和黑色方块
            CleanupPanelsAndIconImages();

            // 2. 修复Input System冲突
            FixInputSystemConflict();

            // 3. 修复3D查看器
            Fix3DViewer();

            Debug.Log("✅ 一键修复完成！");
            Debug.Log("现在可以尝试打开图鉴系统测试了");

            // 延迟测试
            EditorApplication.delayCall += () => {
                TestCompleteSystem();
            };
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ 修复过程中出现错误: {e.Message}");
        }
    }

    private void CleanupPanelsAndIconImages()
    {
        Debug.Log("=== 清理多余面板和黑色方块 ===");

        int cleanedPanels = 0;
        int cleanedIcons = 0;

        // 找出所有EncyclopediaPanel
        var allPanels = FindObjectsOfType<GameObject>();
        var encyclopediaPanels = new System.Collections.Generic.List<GameObject>();

        foreach (var obj in allPanels)
        {
            if (obj.name == "EncyclopediaPanel")
            {
                encyclopediaPanels.Add(obj);
            }
        }

        Debug.Log($"找到 {encyclopediaPanels.Count} 个EncyclopediaPanel");

        // 保留一个有EncyclopediaUI组件的面板
        GameObject keepPanel = null;
        foreach (var panel in encyclopediaPanels)
        {
            if (panel.GetComponent<EncyclopediaUI>() != null && keepPanel == null)
            {
                keepPanel = panel;
                break;
            }
        }

        // 如果没找到有组件的面板，保留第一个
        if (keepPanel == null && encyclopediaPanels.Count > 0)
        {
            keepPanel = encyclopediaPanels[0];
        }

        // 删除多余面板
        foreach (var panel in encyclopediaPanels)
        {
            if (panel != keepPanel)
            {
                Debug.Log($"删除多余面板: {panel.name}");
                DestroyImmediate(panel);
                cleanedPanels++;
            }
        }

        // 清理IconImage组件
        if (keepPanel != null)
        {
            var iconImages = keepPanel.GetComponentsInChildren<Image>(true);
            foreach (var image in iconImages)
            {
                if (image.gameObject.name == "IconImage")
                {
                    Debug.Log($"删除IconImage: {image.gameObject.name}");
                    DestroyImmediate(image.gameObject);
                    cleanedIcons++;
                }
            }

            // 调整NameText位置
            var nameTexts = keepPanel.GetComponentsInChildren<Text>(true);
            foreach (var text in nameTexts)
            {
                if (text.gameObject.name == "NameText")
                {
                    var rectTransform = text.GetComponent<RectTransform>();
                    if (rectTransform != null && Mathf.Approximately(rectTransform.offsetMin.x, 70f))
                    {
                        rectTransform.offsetMin = new Vector2(15f, rectTransform.offsetMin.y);
                        Debug.Log("调整NameText位置");
                    }
                }
            }
        }

        Debug.Log($"✅ 清理完成: 删除 {cleanedPanels} 个面板, {cleanedIcons} 个IconImage");
    }

    private void FixInputSystemConflict()
    {
        Debug.Log("=== 修复Input System冲突 ===");

        // 删除现有的有问题的EventSystem
        var existingSystems = FindObjectsOfType<EventSystem>();
        foreach (var system in existingSystems)
        {
            var standaloneModule = system.GetComponent<StandaloneInputModule>();
            if (standaloneModule != null)
            {
                Debug.Log($"删除有冲突的EventSystem: {system.gameObject.name}");
                DestroyImmediate(system.gameObject);
            }
        }

        // 创建兼容的EventSystem
        GameObject eventSystemGO = new GameObject("CompatibleEventSystem");
        EventSystem eventSystem = eventSystemGO.AddComponent<EventSystem>();

        // 尝试使用InputSystemUIInputModule
        try
        {
            var inputSystemUIType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemUIType != null)
            {
                eventSystemGO.AddComponent(inputSystemUIType);
                Debug.Log("✅ 使用InputSystemUIInputModule解决Input System冲突");
            }
            else
            {
                // 作为备用，创建一个禁用输入的StandaloneInputModule
                var backupModule = eventSystemGO.AddComponent<StandaloneInputModule>();
                backupModule.horizontalAxis = "";
                backupModule.verticalAxis = "";
                backupModule.submitButton = "";
                backupModule.cancelButton = "";
                Debug.Log("✅ 使用禁用输入的StandaloneInputModule作为备用");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Input System模块设置失败: {e.Message}");
        }

        Debug.Log("✅ Input System冲突修复完成");
    }

    private void Fix3DViewer()
    {
        Debug.Log("=== 修复3D查看器 ===");

        var encyclopediaUI = FindObjectOfType<EncyclopediaUI>();
        if (encyclopediaUI == null)
        {
            Debug.LogWarning("未找到EncyclopediaUI，跳过3D查看器修复");
            return;
        }

        // 获取详情面板
        var detailPanelField = typeof(EncyclopediaUI).GetField("detailPanel",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var detailPanel = detailPanelField?.GetValue(encyclopediaUI) as GameObject;

        if (detailPanel == null)
        {
            Debug.LogWarning("未找到详情面板，跳过3D查看器修复");
            return;
        }

        // 查找或创建Simple3DViewer
        var existingViewer = detailPanel.GetComponentInChildren<Simple3DViewer>();
        if (existingViewer == null)
        {
            // 查找Model3DViewer容器
            Transform viewerContainer = null;
            foreach (Transform child in detailPanel.transform)
            {
                if (child.name.Contains("Model3DViewer") || child.name.Contains("Viewer"))
                {
                    viewerContainer = child;
                    break;
                }
            }

            if (viewerContainer == null)
            {
                // 创建新容器
                GameObject viewerGO = new GameObject("Model3DViewer");
                viewerGO.transform.SetParent(detailPanel.transform, false);

                RectTransform rect = viewerGO.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0);
                rect.anchorMax = new Vector2(1, 1);
                rect.offsetMin = new Vector2(10, 20);
                rect.offsetMax = new Vector2(-20, -100);

                Image background = viewerGO.AddComponent<Image>();
                background.color = new Color(0.02f, 0.05f, 0.08f, 0.9f);

                viewerContainer = viewerGO.transform;
            }

            // 添加Simple3DViewer组件
            existingViewer = viewerContainer.gameObject.AddComponent<Simple3DViewer>();
        }

        // 更新EncyclopediaUI的引用
        var viewerField = typeof(EncyclopediaUI).GetField("model3DViewer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        viewerField?.SetValue(encyclopediaUI, existingViewer);

        Debug.Log("✅ 3D查看器修复完成");
    }

    private void TestCompleteSystem()
    {
        Debug.Log("=== 测试完整系统 ===");

        // 测试简单3D显示（不依赖UI）
        TestSimple3DViewer();
    }

    private void TestSimple3DViewer()
    {
        Debug.Log("🧪 测试Simple3DViewer");

        // 创建独立的测试对象
        GameObject testGO = new GameObject("TestSimple3DViewer");
        Simple3DViewer viewer = testGO.AddComponent<Simple3DViewer>();

        // 延迟测试，等待初始化
        EditorApplication.delayCall += () => {
            try
            {
                // 创建测试模型
                GameObject testCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                testCube.name = "SystemTestCube";

                var renderer = testCube.GetComponent<Renderer>();
                var material = new Material(Shader.Find("Standard"));
                material.color = Color.yellow;
                material.SetFloat("_Metallic", 0.1f);
                material.SetFloat("_Glossiness", 0.6f);
                renderer.material = material;

                // 显示模型
                viewer.ShowModel(testCube);

                // 清理原始GameObject
                DestroyImmediate(testCube);

                Debug.Log("✅ Simple3DViewer测试成功");
                Debug.Log("🎯 现在可以尝试在游戏中打开图鉴测试3D显示");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Simple3DViewer测试失败: {e.Message}");
            }
        };
    }

    private void CleanupAllTestObjects()
    {
        Debug.Log("=== 清理所有测试对象 ===");

        var testNames = new string[] {
            "Test", "MinimalTest", "Compatible", "Direct", "Simple3DViewer",
            "NonUI_", "SystemTest", "BigTest", "EventSystem"
        };

        int cleanedCount = 0;
        var allObjects = FindObjectsOfType<GameObject>();

        foreach (var obj in allObjects)
        {
            foreach (var testName in testNames)
            {
                if (obj.name.Contains(testName))
                {
                    Debug.Log($"删除测试对象: {obj.name}");
                    DestroyImmediate(obj);
                    cleanedCount++;
                    break;
                }
            }
        }

        Debug.Log($"✅ 清理完成，删除了 {cleanedCount} 个测试对象");
    }

    private void FixDuplicateEventSystems()
    {
        Debug.Log("=== 修复重复EventSystem ===");

        var eventSystems = FindObjectsOfType<EventSystem>();
        Debug.Log($"找到 {eventSystems.Length} 个EventSystem");

        if (eventSystems.Length <= 1)
        {
            Debug.Log("EventSystem数量正常，无需修复");
            return;
        }

        // 保留最新创建的CompatibleEventSystem
        EventSystem keepSystem = null;
        foreach (var system in eventSystems)
        {
            if (system.gameObject.name.Contains("Compatible"))
            {
                keepSystem = system;
                break;
            }
        }

        // 如果没找到Compatible的，保留第一个
        if (keepSystem == null)
        {
            keepSystem = eventSystems[0];
        }

        // 删除其他的EventSystem
        int deletedCount = 0;
        foreach (var system in eventSystems)
        {
            if (system != keepSystem)
            {
                Debug.Log($"删除多余EventSystem: {system.gameObject.name}");
                DestroyImmediate(system.gameObject);
                deletedCount++;
            }
        }

        Debug.Log($"✅ EventSystem修复完成，删除了 {deletedCount} 个多余的EventSystem");
    }
}
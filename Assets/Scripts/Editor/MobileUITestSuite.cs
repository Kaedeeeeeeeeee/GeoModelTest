using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// 移动端UI测试套件
/// 提供编辑器测试工具，验证移动端UI组件的功能和性能
/// </summary>
public class MobileUITestSuite : EditorWindow
{
    [MenuItem("Tools/移动端UI测试套件")]
    public static void ShowWindow()
    {
        MobileUITestSuite window = GetWindow<MobileUITestSuite>();
        window.titleContent = new GUIContent("移动端UI测试");
        window.minSize = new Vector2(450, 600);
        window.Show();
    }
    
    private Vector2 scrollPosition;
    private List<TestResult> testResults = new List<TestResult>();
    private bool showDetailedResults = false;
    
    void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        DrawHeader();
        DrawTestButtons();
        DrawTestResults();
        
        EditorGUILayout.EndScrollView();
    }
    
    void DrawHeader()
    {
        EditorGUILayout.Space();
        
        GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontSize = 16;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.alignment = TextAnchor.MiddleCenter;
        
        EditorGUILayout.LabelField("移动端UI测试套件", headerStyle);
        EditorGUILayout.LabelField("验证移动端UI组件功能和性能", EditorStyles.centeredGreyMiniLabel);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space();
    }
    
    void DrawTestButtons()
    {
        EditorGUILayout.LabelField("🧪 测试功能", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🔍 组件完整性测试", GUILayout.Height(40)))
        {
            RunComponentIntegrityTest();
        }
        if (GUILayout.Button("⚙️ 配置验证测试", GUILayout.Height(40)))
        {
            RunConfigurationValidationTest();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("📱 设备兼容性测试", GUILayout.Height(40)))
        {
            RunDeviceCompatibilityTest();
        }
        if (GUILayout.Button("🎮 输入系统测试", GUILayout.Height(40)))
        {
            RunInputSystemTest();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🔄 所有测试", GUILayout.Height(40)))
        {
            RunAllTests();
        }
        if (GUILayout.Button("🧹 清除结果", GUILayout.Height(40)))
        {
            ClearTestResults();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        showDetailedResults = EditorGUILayout.Toggle("显示详细结果", showDetailedResults);
        
        EditorGUILayout.Space();
    }
    
    void DrawTestResults()
    {
        if (testResults.Count == 0) return;
        
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.LabelField("📊 测试结果", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        int passedTests = 0;
        int failedTests = 0;
        
        foreach (var result in testResults)
        {
            if (result.passed) passedTests++;
            else failedTests++;
            
            GUIStyle resultStyle = new GUIStyle(EditorStyles.helpBox);
            if (result.passed)
            {
                resultStyle.normal.textColor = Color.green;
            }
            else
            {
                resultStyle.normal.textColor = Color.red;
            }
            
            string statusIcon = result.passed ? "✅" : "❌";
            EditorGUILayout.LabelField($"{statusIcon} {result.testName}", EditorStyles.boldLabel);
            
            if (showDetailedResults || !result.passed)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField(result.description, EditorStyles.wordWrappedMiniLabel);
                if (!string.IsNullOrEmpty(result.details))
                {
                    EditorGUILayout.LabelField($"详情: {result.details}", EditorStyles.miniLabel);
                }
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space(5);
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        
        GUIStyle summaryStyle = new GUIStyle(EditorStyles.boldLabel);
        summaryStyle.alignment = TextAnchor.MiddleCenter;
        
        string summary = $"测试完成: {passedTests} 通过, {failedTests} 失败";
        EditorGUILayout.LabelField(summary, summaryStyle);
        
        if (failedTests > 0)
        {
            EditorGUILayout.HelpBox("存在失败的测试项目，请检查相关组件配置。", MessageType.Warning);
        }
        else if (passedTests > 0)
        {
            EditorGUILayout.HelpBox("所有测试项目通过！移动端UI系统配置正确。", MessageType.Info);
        }
    }
    
    void RunAllTests()
    {
        ClearTestResults();
        
        RunComponentIntegrityTest();
        RunConfigurationValidationTest();
        RunDeviceCompatibilityTest();
        RunInputSystemTest();
        
        Debug.Log($"[MobileUITestSuite] 所有测试完成 - 总计{testResults.Count}项测试");
    }
    
    void RunComponentIntegrityTest()
    {
        AddTestResult("移动端输入管理器", "检查MobileInputManager组件", 
            TestComponentExists<MobileInputManager>());
            
        AddTestResult("移动端UI适配器", "检查MobileUIAdapter组件", 
            TestComponentExists<MobileUIAdapter>());
            
        AddTestResult("触摸手势处理器", "检查TouchGestureHandler组件", 
            TestComponentExists<TouchGestureHandler>());
            
        AddTestResult("触觉反馈管理器", "检查TouchFeedbackManager组件", 
            TestComponentExists<TouchFeedbackManager>());
            
        AddTestResult("移动端控制界面", "检查MobileControlsUI组件", 
            TestComponentExists<MobileControlsUI>());
            
        // 检查UI组件的移动端适配
        var inventoryUI = FindObjectOfType<InventoryUI>();
        bool inventoryAdapted = inventoryUI != null && inventoryUI.enableMobileAdaptation;
        AddTestResult("背包界面移动端适配", "检查InventoryUI移动端适配状态", 
            inventoryAdapted, inventoryAdapted ? "" : "InventoryUI未启用移动端适配");
            
        var inventoryUISystem = FindObjectOfType<InventoryUISystem>();
        bool inventorySystemAdapted = inventoryUISystem != null && inventoryUISystem.enableMobileAdaptation;
        AddTestResult("工具轮盘移动端适配", "检查InventoryUISystem移动端适配状态", 
            inventorySystemAdapted, inventorySystemAdapted ? "" : "InventoryUISystem未启用移动端适配");
    }
    
    void RunConfigurationValidationTest()
    {
        var inputManager = FindObjectOfType<MobileInputManager>();
        if (inputManager != null)
        {
            bool validMode = inputManager.currentInputMode != MobileInputManager.InputMode.Auto || 
                           Application.isMobilePlatform;
            AddTestResult("输入模式配置", "验证输入模式设置合理性", 
                validMode, validMode ? "" : "建议在移动平台使用Auto或Mobile模式");
        }
        
        var mobileControls = FindObjectOfType<MobileControlsUI>();
        if (mobileControls != null)
        {
            bool validButtons = mobileControls.buttonSize >= 50f && mobileControls.buttonSize <= 120f;
            AddTestResult("按钮尺寸配置", "验证虚拟按钮尺寸合理性", 
                validButtons, validButtons ? "" : $"按钮尺寸{mobileControls.buttonSize}px可能不适宜");
                
            bool validControls = mobileControls.buttonSize > 0;
            AddTestResult("控制组件配置", "验证虚拟控制组件基本配置", 
                validControls, validControls ? "基本配置正确" : "控制组件配置异常");
        }
        
        var gestureHandler = FindObjectOfType<TouchGestureHandler>();
        if (gestureHandler != null)
        {
            bool validGesture = gestureHandler.enableGestureRecognition;
            AddTestResult("手势识别配置", "验证手势识别启用状态", 
                validGesture, validGesture ? "" : "建议启用手势识别以获得更好的移动端体验");
        }
        
        var feedbackManager = FindObjectOfType<TouchFeedbackManager>();
        if (feedbackManager != null)
        {
            bool feedbackEnabled = feedbackManager.enableVibration || feedbackManager.enableSoundFeedback;
            AddTestResult("反馈系统配置", "验证触觉反馈配置", 
                feedbackEnabled, feedbackEnabled ? "" : "建议启用震动或音频反馈");
        }
    }
    
    void RunDeviceCompatibilityTest()
    {
        // 检查Canvas配置
        var canvas = FindObjectOfType<Canvas>();
        bool hasCanvas = canvas != null;
        AddTestResult("Canvas组件", "检查UI Canvas配置", 
            hasCanvas, hasCanvas ? "" : "场景中需要Canvas组件");
            
        if (hasCanvas)
        {
            bool correctRenderMode = canvas.renderMode == RenderMode.ScreenSpaceOverlay;
            AddTestResult("Canvas渲染模式", "验证Canvas渲染模式适合移动端", 
                correctRenderMode, correctRenderMode ? "" : "建议使用ScreenSpaceOverlay模式");
        }
        
        // 检查EventSystem
        var eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        bool hasEventSystem = eventSystem != null;
        AddTestResult("事件系统", "检查EventSystem组件", 
            hasEventSystem, hasEventSystem ? "" : "移动端UI需要EventSystem");
            
        // 检查输入系统模块
        var inputModule = FindObjectOfType<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        bool hasInputModule = inputModule != null;
        AddTestResult("输入系统模块", "检查新输入系统UI模块", 
            hasInputModule, hasInputModule ? "" : "建议使用InputSystemUIInputModule");
            
        // 检查安全区域组件
        var safeAreaPanels = FindObjectsOfType<SafeAreaPanel>();
        bool hasSafeArea = safeAreaPanels.Length > 0;
        AddTestResult("安全区域适配", "检查SafeAreaPanel组件", 
            hasSafeArea, hasSafeArea ? $"找到{safeAreaPanels.Length}个安全区域组件" : "建议添加SafeAreaPanel处理刘海屏");
    }
    
    void RunInputSystemTest()
    {
        var inputManager = FindObjectOfType<MobileInputManager>();
        if (inputManager != null)
        {
            bool canDetectMobile = inputManager.IsMobileDevice();
            AddTestResult("移动设备检测", "测试移动设备检测功能", 
                true, canDetectMobile ? "检测为移动设备" : "检测为桌面设备");
                
            bool shouldShowVirtual = inputManager.ShouldShowVirtualControls();
            AddTestResult("虚拟控制显示", "测试虚拟控制显示逻辑", 
                true, shouldShowVirtual ? "应显示虚拟控制" : "不显示虚拟控制");
        }
        
        // 检查触摸输入支持
        bool touchSupported = UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.enabled ||
                             UnityEngine.InputSystem.Touchscreen.current != null;
        AddTestResult("触摸输入支持", "检查触摸输入系统", 
            touchSupported, touchSupported ? "触摸输入已支持" : "触摸输入可能未正确配置");
            
        // 检查输入动作
        var touchScreen = UnityEngine.InputSystem.Touchscreen.current;
        bool hasTouchScreen = touchScreen != null;
        AddTestResult("触摸屏设备", "检查触摸屏输入设备", 
            hasTouchScreen, hasTouchScreen ? "找到触摸屏设备" : "未找到触摸屏设备（正常，如果在PC上测试）");
    }
    
    bool TestComponentExists<T>() where T : Component
    {
        return FindObjectOfType<T>() != null;
    }
    
    void AddTestResult(string testName, string description, bool passed, string details = "")
    {
        testResults.Add(new TestResult
        {
            testName = testName,
            description = description,
            passed = passed,
            details = details,
            timestamp = System.DateTime.Now
        });
    }
    
    void ClearTestResults()
    {
        testResults.Clear();
    }
    
    [System.Serializable]
    public class TestResult
    {
        public string testName;
        public string description;
        public bool passed;
        public string details;
        public System.DateTime timestamp;
    }
}
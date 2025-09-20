using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 移动端UI组件快速配置工具
/// 帮助在Unity编辑器中快速设置和配置移动端适配UI组件
/// </summary>
public class MobileUISetupTool : EditorWindow
{
    [MenuItem("Tools/移动端UI配置工具")]
    public static void ShowWindow()
    {
        MobileUISetupTool window = GetWindow<MobileUISetupTool>();
        window.titleContent = new GUIContent("移动端UI配置工具");
        window.minSize = new Vector2(500, 700);
        window.Show();
    }
    
    private Vector2 scrollPosition;
    private bool showAdvancedSettings = false;
    
    // 配置选项
    private bool setupInputManager = true;
    private bool setupUIAdapter = true;
    private bool setupGestureHandler = true;
    private bool setupFeedbackManager = true;
    private bool setupMobileControls = true;
    private bool setupInventoryAdaptation = true;
    
    // 高级配置
    private float buttonSize = 80f;
    private int gridColumns = 3;
    private bool enableDebug = false;
    
    void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        DrawHeader();
        DrawMainSettings();
        DrawAdvancedSettings();
        DrawActionButtons();
        DrawExistingComponents();
        
        EditorGUILayout.EndScrollView();
    }
    
    void DrawHeader()
    {
        EditorGUILayout.Space();
        
        GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontSize = 18;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.alignment = TextAnchor.MiddleCenter;
        
        EditorGUILayout.LabelField("移动端UI适配系统配置工具", headerStyle);
        EditorGUILayout.LabelField("快速设置和配置移动端UI组件", EditorStyles.centeredGreyMiniLabel);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space();
    }
    
    void DrawMainSettings()
    {
        EditorGUILayout.LabelField("📱 主要组件设置", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        setupInputManager = EditorGUILayout.ToggleLeft("🎮 输入管理器 (MobileInputManager)", setupInputManager);
        EditorGUILayout.LabelField("    统一触控和传统输入处理", EditorStyles.miniLabel);
        EditorGUILayout.Space(5);
        
        setupUIAdapter = EditorGUILayout.ToggleLeft("📐 UI适配器 (MobileUIAdapter)", setupUIAdapter);
        EditorGUILayout.LabelField("    响应式界面和设备检测", EditorStyles.miniLabel);
        EditorGUILayout.Space(5);
        
        setupGestureHandler = EditorGUILayout.ToggleLeft("👆 手势识别 (TouchGestureHandler)", setupGestureHandler);
        EditorGUILayout.LabelField("    多点触控和地质勘探手势", EditorStyles.miniLabel);
        EditorGUILayout.Space(5);
        
        setupFeedbackManager = EditorGUILayout.ToggleLeft("📳 触觉反馈 (TouchFeedbackManager)", setupFeedbackManager);
        EditorGUILayout.LabelField("    震动和音频反馈系统", EditorStyles.miniLabel);
        EditorGUILayout.Space(5);
        
        setupMobileControls = EditorGUILayout.ToggleLeft("🕹️ 虚拟控制 (MobileControlsUI)", setupMobileControls);
        EditorGUILayout.LabelField("    虚拟摇杆和触控按钮", EditorStyles.miniLabel);
        EditorGUILayout.Space(5);
        
        setupInventoryAdaptation = EditorGUILayout.ToggleLeft("🎒 背包适配 (InventoryUI Mobile)", setupInventoryAdaptation);
        EditorGUILayout.LabelField("    工具轮盘和背包界面移动端优化", EditorStyles.miniLabel);
        
        EditorGUILayout.Space();
    }
    
    void DrawAdvancedSettings()
    {
        EditorGUILayout.LabelField("⚙️ 高级设置", EditorStyles.boldLabel);
        
        showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "显示高级配置选项");
        
        if (showAdvancedSettings)
        {
            EditorGUILayout.Space();
            EditorGUI.indentLevel++;
            
            EditorGUILayout.LabelField("虚拟控制参数", EditorStyles.boldLabel);
            buttonSize = EditorGUILayout.Slider("按钮大小", buttonSize, 50f, 120f);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("界面布局参数", EditorStyles.boldLabel);
            gridColumns = EditorGUILayout.IntSlider("网格列数", gridColumns, 2, 5);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("调试选项", EditorStyles.boldLabel);
            enableDebug = EditorGUILayout.Toggle("启用调试信息", enableDebug);
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
    }
    
    void DrawActionButtons()
    {
        EditorGUILayout.LabelField("🛠️ 操作", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("🚀 一键配置所有组件", GUILayout.Height(40)))
        {
            SetupAllComponents();
        }
        
        if (GUILayout.Button("🔄 重置选中组件", GUILayout.Height(40)))
        {
            ResetSelectedComponents();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("📋 检查现有组件"))
        {
            CheckExistingComponents();
        }
        
        if (GUILayout.Button("🧹 清理移动端组件"))
        {
            CleanupMobileComponents();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
    }
    
    void DrawExistingComponents()
    {
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.LabelField("📊 场景中的现有组件", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // 检查各个组件的存在状态
        DrawComponentStatus("MobileInputManager", FindObjectOfType<MobileInputManager>());
        DrawComponentStatus("MobileUIAdapter", FindObjectOfType<MobileUIAdapter>());
        DrawComponentStatus("TouchGestureHandler", FindObjectOfType<TouchGestureHandler>());
        DrawComponentStatus("TouchFeedbackManager", FindObjectOfType<TouchFeedbackManager>());
        DrawComponentStatus("MobileControlsUI", FindObjectOfType<MobileControlsUI>());
        
        // 检查现有UI组件的移动端适配状态
        var inventoryUI = FindObjectOfType<InventoryUI>();
        var inventoryUISystem = FindObjectOfType<InventoryUISystem>();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("UI组件适配状态:", EditorStyles.boldLabel);
        
        if (inventoryUI != null)
        {
            bool isMobileAdapted = inventoryUI.enableMobileAdaptation;
            string status = isMobileAdapted ? "✅ 已适配" : "❌ 未适配";
            EditorGUILayout.LabelField($"InventoryUI: {status}");
        }
        else
        {
            EditorGUILayout.LabelField("InventoryUI: ❓ 未找到");
        }
        
        if (inventoryUISystem != null)
        {
            bool isMobileAdapted = inventoryUISystem.enableMobileAdaptation;
            string status = isMobileAdapted ? "✅ 已适配" : "❌ 未适配";
            EditorGUILayout.LabelField($"InventoryUISystem: {status}");
        }
        else
        {
            EditorGUILayout.LabelField("InventoryUISystem: ❓ 未找到");
        }
    }
    
    void DrawComponentStatus(string componentName, Component component)
    {
        string status = component != null ? "✅ 已存在" : "❌ 未找到";
        EditorGUILayout.LabelField($"{componentName}: {status}");
        
        if (component != null)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"对象: {component.gameObject.name}", EditorStyles.miniLabel);
            if (GUILayout.Button("选择", GUILayout.Width(50)))
            {
                Selection.activeGameObject = component.gameObject;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--;
        }
    }
    
    void SetupAllComponents()
    {
        if (EditorUtility.DisplayDialog("确认配置", 
            "这将在场景中设置所有选中的移动端UI组件。\n\n确定要继续吗？", 
            "确定", "取消"))
        {
            try
            {
                int setupCount = 0;
                
                if (setupInputManager) setupCount += SetupInputManager() ? 1 : 0;
                if (setupUIAdapter) setupCount += SetupUIAdapter() ? 1 : 0;
                if (setupGestureHandler) setupCount += SetupGestureHandler() ? 1 : 0;
                if (setupFeedbackManager) setupCount += SetupFeedbackManager() ? 1 : 0;
                if (setupMobileControls) setupCount += SetupMobileControls() ? 1 : 0;
                if (setupInventoryAdaptation) setupCount += SetupInventoryAdaptation() ? 1 : 0;
                
                EditorUtility.DisplayDialog("配置完成", 
                    $"成功配置了 {setupCount} 个移动端UI组件！\n\n" +
                    "组件已添加到场景中，可以在Inspector中进一步调整参数。", 
                    "确定");
                    
                Debug.Log($"[MobileUISetupTool] 移动端UI配置完成 - 配置了{setupCount}个组件");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("配置错误", 
                    $"配置过程中出现错误:\n{e.Message}", 
                    "确定");
                Debug.LogError($"[MobileUISetupTool] 配置错误: {e.Message}");
            }
        }
    }
    
    bool SetupInputManager()
    {
        var existing = FindObjectOfType<MobileInputManager>();
        if (existing != null)
        {
            Debug.Log("[MobileUISetupTool] MobileInputManager已存在，跳过创建");
            return false;
        }
        
        GameObject inputManagerObj = new GameObject("MobileInputManager");
        var inputManager = inputManagerObj.AddComponent<MobileInputManager>();
        
        // 应用高级设置
        inputManager.enableDebugLog = enableDebug;
        
        Debug.Log("[MobileUISetupTool] MobileInputManager已创建");
        return true;
    }
    
    bool SetupUIAdapter()
    {
        var existing = FindObjectOfType<MobileUIAdapter>();
        if (existing != null)
        {
            Debug.Log("[MobileUISetupTool] MobileUIAdapter已存在，跳过创建");
            return false;
        }
        
        GameObject uiAdapterObj = new GameObject("MobileUIAdapter");
        var uiAdapter = uiAdapterObj.AddComponent<MobileUIAdapter>();
        
        // 应用高级设置
        uiAdapter.enableDebugInfo = enableDebug;
        
        Debug.Log("[MobileUISetupTool] MobileUIAdapter已创建");
        return true;
    }
    
    bool SetupGestureHandler()
    {
        var existing = FindObjectOfType<TouchGestureHandler>();
        if (existing != null)
        {
            Debug.Log("[MobileUISetupTool] TouchGestureHandler已存在，跳过创建");
            return false;
        }
        
        GameObject gestureObj = new GameObject("TouchGestureHandler");
        var gestureHandler = gestureObj.AddComponent<TouchGestureHandler>();
        
        Debug.Log("[MobileUISetupTool] TouchGestureHandler已创建");
        return true;
    }
    
    bool SetupFeedbackManager()
    {
        var existing = FindObjectOfType<TouchFeedbackManager>();
        if (existing != null)
        {
            Debug.Log("[MobileUISetupTool] TouchFeedbackManager已存在，跳过创建");
            return false;
        }
        
        GameObject feedbackObj = new GameObject("TouchFeedbackManager");
        var feedbackManager = feedbackObj.AddComponent<TouchFeedbackManager>();
        
        // 应用高级设置
        feedbackManager.enableDebugLog = enableDebug;
        
        Debug.Log("[MobileUISetupTool] TouchFeedbackManager已创建");
        return true;
    }
    
    bool SetupMobileControls()
    {
        var existing = FindObjectOfType<MobileControlsUI>();
        if (existing != null)
        {
            Debug.Log("[MobileUISetupTool] MobileControlsUI已存在，跳过创建");
            return false;
        }
        
        // 创建Canvas如果不存在
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("MobileControlsCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            
            // 确保EventSystem存在
            if (FindObjectOfType<EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
        }
        
        GameObject controlsObj = new GameObject("MobileControlsUI");
        controlsObj.transform.SetParent(canvas.transform);
        var mobileControls = controlsObj.AddComponent<MobileControlsUI>();
        
        // 应用高级设置
        mobileControls.buttonSize = buttonSize;
        mobileControls.enableDebugVisualization = enableDebug;
        
        Debug.Log("[MobileUISetupTool] MobileControlsUI已创建");
        return true;
    }
    
    bool SetupInventoryAdaptation()
    {
        bool adapted = false;
        
        // 适配InventoryUI
        var inventoryUI = FindObjectOfType<InventoryUI>();
        if (inventoryUI != null)
        {
            inventoryUI.enableMobileAdaptation = true;
            inventoryUI.mobileGridColumns = gridColumns;
            adapted = true;
            Debug.Log("[MobileUISetupTool] InventoryUI移动端适配已启用");
        }
        
        // 适配InventoryUISystem
        var inventoryUISystem = FindObjectOfType<InventoryUISystem>();
        if (inventoryUISystem != null)
        {
            inventoryUISystem.enableMobileAdaptation = true;
            inventoryUISystem.showMobileToolbar = true;
            adapted = true;
            Debug.Log("[MobileUISetupTool] InventoryUISystem移动端适配已启用");
        }
        
        if (!adapted)
        {
            Debug.LogWarning("[MobileUISetupTool] 未找到InventoryUI或InventoryUISystem组件");
        }
        
        return adapted;
    }
    
    void ResetSelectedComponents()
    {
        if (EditorUtility.DisplayDialog("确认重置", 
            "这将删除场景中所有选中类型的移动端UI组件。\n\n确定要继续吗？", 
            "确定", "取消"))
        {
            int resetCount = 0;
            
            if (setupInputManager) resetCount += RemoveComponent<MobileInputManager>();
            if (setupUIAdapter) resetCount += RemoveComponent<MobileUIAdapter>();
            if (setupGestureHandler) resetCount += RemoveComponent<TouchGestureHandler>();
            if (setupFeedbackManager) resetCount += RemoveComponent<TouchFeedbackManager>();
            if (setupMobileControls) resetCount += RemoveComponent<MobileControlsUI>();
            
            EditorUtility.DisplayDialog("重置完成", 
                $"已删除 {resetCount} 个移动端UI组件。", 
                "确定");
        }
    }
    
    int RemoveComponent<T>() where T : Component
    {
        var components = FindObjectsOfType<T>();
        foreach (var component in components)
        {
            DestroyImmediate(component.gameObject);
        }
        return components.Length;
    }
    
    void CheckExistingComponents()
    {
        Debug.Log("=== 移动端UI组件检查报告 ===");
        
        var inputManager = FindObjectOfType<MobileInputManager>();
        var uiAdapter = FindObjectOfType<MobileUIAdapter>();
        var gestureHandler = FindObjectOfType<TouchGestureHandler>();
        var feedbackManager = FindObjectOfType<TouchFeedbackManager>();
        var mobileControls = FindObjectOfType<MobileControlsUI>();
        var inventoryUI = FindObjectOfType<InventoryUI>();
        var inventoryUISystem = FindObjectOfType<InventoryUISystem>();
        
        Debug.Log($"MobileInputManager: {(inputManager != null ? "✅" : "❌")}");
        Debug.Log($"MobileUIAdapter: {(uiAdapter != null ? "✅" : "❌")}");
        Debug.Log($"TouchGestureHandler: {(gestureHandler != null ? "✅" : "❌")}");
        Debug.Log($"TouchFeedbackManager: {(feedbackManager != null ? "✅" : "❌")}");
        Debug.Log($"MobileControlsUI: {(mobileControls != null ? "✅" : "❌")}");
        Debug.Log($"InventoryUI移动端适配: {(inventoryUI?.enableMobileAdaptation == true ? "✅" : "❌")}");
        Debug.Log($"InventoryUISystem移动端适配: {(inventoryUISystem?.enableMobileAdaptation == true ? "✅" : "❌")}");
        
        EditorUtility.DisplayDialog("检查完成", 
            "组件检查报告已输出到Console窗口。", 
            "确定");
    }
    
    void CleanupMobileComponents()
    {
        if (EditorUtility.DisplayDialog("确认清理", 
            "这将删除场景中所有移动端UI组件。\n\n确定要继续吗？", 
            "确定", "取消"))
        {
            int cleanedCount = 0;
            cleanedCount += RemoveComponent<MobileInputManager>();
            cleanedCount += RemoveComponent<MobileUIAdapter>();
            cleanedCount += RemoveComponent<TouchGestureHandler>();
            cleanedCount += RemoveComponent<TouchFeedbackManager>();
            cleanedCount += RemoveComponent<MobileControlsUI>();
            
            // 重置现有UI组件的移动端适配
            var inventoryUI = FindObjectOfType<InventoryUI>();
            if (inventoryUI != null)
            {
                inventoryUI.enableMobileAdaptation = false;
            }
            
            var inventoryUISystem = FindObjectOfType<InventoryUISystem>();
            if (inventoryUISystem != null)
            {
                inventoryUISystem.enableMobileAdaptation = false;
                inventoryUISystem.showMobileToolbar = false;
            }
            
            EditorUtility.DisplayDialog("清理完成", 
                $"已清理 {cleanedCount} 个移动端UI组件。", 
                "确定");
                
            Debug.Log($"[MobileUISetupTool] 移动端UI组件清理完成 - 清理了{cleanedCount}个组件");
        }
    }
}
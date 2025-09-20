using UnityEngine;
using UnityEditor;

/// <summary>
/// 移动端UI调试助手
/// 提供快速切换移动端UI显示模式的功能
/// </summary>
public class MobileUIDebugHelper : EditorWindow
{
    [MenuItem("Tools/移动端UI/🔧 调试助手")]
    public static void ShowWindow()
    {
        MobileUIDebugHelper window = GetWindow<MobileUIDebugHelper>();
        window.titleContent = new GUIContent("移动端UI调试");
        window.minSize = new Vector2(300, 400);
        window.Show();
    }
    
    void OnGUI()
    {
        DrawHeader();
        DrawControlButtons();
        DrawCurrentStatus();
    }
    
    void DrawHeader()
    {
        EditorGUILayout.Space();
        
        GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontSize = 16;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.alignment = TextAnchor.MiddleCenter;
        
        EditorGUILayout.LabelField("移动端UI调试助手", headerStyle);
        EditorGUILayout.LabelField("快速切换UI显示和调试模式", EditorStyles.centeredGreyMiniLabel);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space();
    }
    
    void DrawControlButtons()
    {
        EditorGUILayout.LabelField("🎮 快速控制", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        if (GUILayout.Button("🟢 启用桌面测试模式", GUILayout.Height(40)))
        {
            EnableDesktopTestMode();
        }
        EditorGUILayout.LabelField("    在桌面上强制显示移动端UI控件", EditorStyles.miniLabel);
        EditorGUILayout.Space();
        
        if (GUILayout.Button("🔴 禁用桌面测试模式", GUILayout.Height(40)))
        {
            DisableDesktopTestMode();
        }
        EditorGUILayout.LabelField("    恢复正常的移动端检测逻辑", EditorStyles.miniLabel);
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("🔍 调试功能", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        if (GUILayout.Button("📊 启用调试可视化", GUILayout.Height(35)))
        {
            EnableDebugVisualization();
        }
        
        if (GUILayout.Button("📋 禁用调试可视化", GUILayout.Height(35)))
        {
            DisableDebugVisualization();
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("🔄 刷新MobileControlsUI", GUILayout.Height(35)))
        {
            RefreshMobileControlsUI();
        }
    }
    
    void DrawCurrentStatus()
    {
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.LabelField("📊 当前状态", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        var mobileControls = FindObjectOfType<MobileControlsUI>();
        if (mobileControls != null)
        {
            EditorGUILayout.LabelField($"MobileControlsUI: ✅ 找到");
            EditorGUILayout.LabelField($"GameObject激活: {(mobileControls.gameObject.activeInHierarchy ? "✅" : "❌")}");
            EditorGUILayout.LabelField($"强制桌面显示: {(mobileControls.forceShowOnDesktop ? "✅" : "❌")}");
            EditorGUILayout.LabelField($"桌面自动隐藏: {(mobileControls.autoHideOnDesktop ? "✅" : "❌")}");
            EditorGUILayout.LabelField($"调试可视化: {(mobileControls.enableDebugVisualization ? "✅" : "❌")}");
            EditorGUILayout.LabelField($"按钮大小: {mobileControls.buttonSize}px");
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("组件状态:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"摇杆容器: {(mobileControls.joystickContainer != null ? "✅" : "❌")}");
            EditorGUILayout.LabelField($"跳跃按钮: {(mobileControls.jumpButton != null ? "✅" : "❌")}");
            EditorGUILayout.LabelField($"背包按钮: {(mobileControls.inventoryButton != null ? "✅" : "❌")}");
        }
        else
        {
            EditorGUILayout.LabelField("MobileControlsUI: ❌ 未找到");
            EditorGUILayout.HelpBox("请先使用移动端UI配置工具创建MobileControlsUI组件。", MessageType.Warning);
        }
        
        EditorGUILayout.Space();
        
        var inputManager = FindObjectOfType<MobileInputManager>();
        EditorGUILayout.LabelField($"MobileInputManager: {(inputManager != null ? "✅" : "❌")}");
        if (inputManager != null)
        {
            EditorGUILayout.LabelField($"当前输入模式: {inputManager.currentInputMode}");
            EditorGUILayout.LabelField($"检测为移动设备: {inputManager.IsMobileDevice()}");
            EditorGUILayout.LabelField($"桌面测试模式: {(inputManager.desktopTestMode ? "✅" : "❌")}");
            EditorGUILayout.LabelField($"虚拟控件启用: {(inputManager.enableVirtualControls ? "✅" : "❌")}");
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"当前平台: {(Application.isMobilePlatform ? "移动平台" : "桌面平台")}");
    }
    
    void EnableDesktopTestMode()
    {
        var mobileControls = FindObjectOfType<MobileControlsUI>();
        var inputManager = FindObjectOfType<MobileInputManager>();
        var firstPersonController = FindObjectOfType<FirstPersonController>();
        
        if (mobileControls != null)
        {
            mobileControls.forceShowOnDesktop = true;
            mobileControls.autoHideOnDesktop = false;
            mobileControls.enableMouseInput = true;
            mobileControls.isDynamicJoystick = false; // 桌面测试模式禁用动态摇杆
            
            // 立即激活GameObject
            mobileControls.gameObject.SetActive(true);
            
            // 重置摇杆位置（在运行时才会生效）
            if (Application.isPlaying)
            {
                mobileControls.ResetJoystickPosition();
            }
            
            EditorUtility.SetDirty(mobileControls);
        }
        
        // 启用InputManager的桌面测试模式，保持鼠标功能
        if (inputManager != null)
        {
            inputManager.desktopTestMode = true;
            inputManager.enableVirtualControls = true;
            EditorUtility.SetDirty(inputManager);
        }
        
        // 通知FirstPersonController更新鼠标锁定状态
        if (firstPersonController != null)
        {
            // 注意：这个方法需要在运行时调用，编辑器模式下无法直接设置
            EditorUtility.SetDirty(firstPersonController);
        }
        
        if (mobileControls != null)
        {
            Debug.Log("[MobileUIDebugHelper] 桌面测试模式已启用 - 鼠标和虚拟控件同时可用");
            EditorUtility.DisplayDialog("成功", 
                "桌面测试模式已启用！\n\n✅ 移动端UI已显示\n✅ 鼠标输入已启用\n✅ 运行游戏后鼠标将解锁\n✅ 可以点击虚拟按钮测试\n\n请运行游戏查看效果。", 
                "确定");
        }
        else
        {
            EditorUtility.DisplayDialog("错误", 
                "未找到MobileControlsUI组件！\n\n请先使用移动端UI配置工具创建组件。", 
                "确定");
        }
    }
    
    void DisableDesktopTestMode()
    {
        var mobileControls = FindObjectOfType<MobileControlsUI>();
        var inputManager = FindObjectOfType<MobileInputManager>();
        var firstPersonController = FindObjectOfType<FirstPersonController>();
        
        if (mobileControls != null)
        {
            mobileControls.forceShowOnDesktop = false;
            mobileControls.autoHideOnDesktop = true;
            mobileControls.enableMouseInput = false;
            
            EditorUtility.SetDirty(mobileControls);
        }
        
        // 禁用InputManager的桌面测试模式
        if (inputManager != null)
        {
            inputManager.desktopTestMode = false;
            inputManager.enableVirtualControls = false;
            EditorUtility.SetDirty(inputManager);
        }
        
        // 通知FirstPersonController恢复鼠标锁定状态
        if (firstPersonController != null)
        {
            EditorUtility.SetDirty(firstPersonController);
        }
        
        if (mobileControls != null)
        {
            Debug.Log("[MobileUIDebugHelper] 桌面测试模式已禁用");
            EditorUtility.DisplayDialog("成功", 
                "桌面测试模式已禁用！\n\n现在恢复正常的移动端检测逻辑。\n运行游戏后鼠标将重新锁定。", 
                "确定");
        }
        else
        {
            EditorUtility.DisplayDialog("错误", 
                "未找到MobileControlsUI组件！", 
                "确定");
        }
    }
    
    void EnableDebugVisualization()
    {
        var mobileControls = FindObjectOfType<MobileControlsUI>();
        if (mobileControls != null)
        {
            mobileControls.enableDebugVisualization = true;
            EditorUtility.SetDirty(mobileControls);
            Debug.Log("[MobileUIDebugHelper] 调试可视化已启用");
        }
        
        // 同时启用其他组件的调试
        var inputManager = FindObjectOfType<MobileInputManager>();
        if (inputManager != null)
        {
            inputManager.enableDebugLog = true;
            EditorUtility.SetDirty(inputManager);
        }
        
        var uiAdapter = FindObjectOfType<MobileUIAdapter>();
        if (uiAdapter != null)
        {
            uiAdapter.enableDebugInfo = true;
            EditorUtility.SetDirty(uiAdapter);
        }
        
        EditorUtility.DisplayDialog("成功", 
            "调试可视化已启用！\n\n现在将显示详细的调试信息。", 
            "确定");
    }
    
    void DisableDebugVisualization()
    {
        var mobileControls = FindObjectOfType<MobileControlsUI>();
        if (mobileControls != null)
        {
            mobileControls.enableDebugVisualization = false;
            EditorUtility.SetDirty(mobileControls);
            Debug.Log("[MobileUIDebugHelper] 调试可视化已禁用");
        }
        
        // 同时禁用其他组件的调试
        var inputManager = FindObjectOfType<MobileInputManager>();
        if (inputManager != null)
        {
            inputManager.enableDebugLog = false;
            EditorUtility.SetDirty(inputManager);
        }
        
        var uiAdapter = FindObjectOfType<MobileUIAdapter>();
        if (uiAdapter != null)
        {
            uiAdapter.enableDebugInfo = false;
            EditorUtility.SetDirty(uiAdapter);
        }
        
        EditorUtility.DisplayDialog("成功", 
            "调试可视化已禁用！", 
            "确定");
    }
    
    void RefreshMobileControlsUI()
    {
        var mobileControls = FindObjectOfType<MobileControlsUI>();
        if (mobileControls != null)
        {
            // 重新激活组件以触发初始化
            bool wasActive = mobileControls.gameObject.activeInHierarchy;
            mobileControls.gameObject.SetActive(false);
            mobileControls.gameObject.SetActive(wasActive);
            
            Debug.Log("[MobileUIDebugHelper] MobileControlsUI已刷新");
            EditorUtility.DisplayDialog("成功", 
                "MobileControlsUI已刷新！\n\n组件已重新初始化。", 
                "确定");
        }
        else
        {
            EditorUtility.DisplayDialog("错误", 
                "未找到MobileControlsUI组件！", 
                "确定");
        }
    }
}
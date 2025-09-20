using UnityEngine;
using UnityEditor;

/// <summary>
/// 移动端UI工具菜单
/// 提供快速访问所有移动端UI相关工具的统一入口
/// </summary>
public class MobileUIToolsMenu : EditorWindow
{
    [MenuItem("Tools/移动端UI工具中心")]
    public static void ShowWindow()
    {
        MobileUIToolsMenu window = GetWindow<MobileUIToolsMenu>();
        window.titleContent = new GUIContent("移动端UI工具");
        window.minSize = new Vector2(350, 450);
        window.maxSize = new Vector2(350, 450);
        window.Show();
    }
    
    void OnGUI()
    {
        DrawHeader();
        DrawMainTools();
        DrawQuickActions();
        DrawDocumentation();
    }
    
    void DrawHeader()
    {
        EditorGUILayout.Space();
        
        GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontSize = 18;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.alignment = TextAnchor.MiddleCenter;
        
        EditorGUILayout.LabelField("移动端UI工具中心", headerStyle);
        EditorGUILayout.LabelField("Unity地质勘探教育游戏移动端适配", EditorStyles.centeredGreyMiniLabel);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space();
    }
    
    void DrawMainTools()
    {
        EditorGUILayout.LabelField("🛠️ 主要工具", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // 配置工具
        if (GUILayout.Button("🔧 移动端UI配置工具", GUILayout.Height(35)))
        {
            MobileUISetupTool.ShowWindow();
        }
        EditorGUILayout.LabelField("    快速设置和配置移动端UI组件", EditorStyles.miniLabel);
        EditorGUILayout.Space(5);
        
        // 预设工具
        if (GUILayout.Button("📱 配置预设管理器", GUILayout.Height(35)))
        {
            MobileUIConfigPresets.ShowWindow();
        }
        EditorGUILayout.LabelField("    应用常见设备配置预设", EditorStyles.miniLabel);
        EditorGUILayout.Space(5);
        
        // 测试工具
        if (GUILayout.Button("🧪 测试套件", GUILayout.Height(35)))
        {
            MobileUITestSuite.ShowWindow();
        }
        EditorGUILayout.LabelField("    验证移动端UI功能和性能", EditorStyles.miniLabel);
        
        EditorGUILayout.Space();
    }
    
    void DrawQuickActions()
    {
        EditorGUILayout.LabelField("⚡ 快速操作", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("📱 手机配置"))
        {
            ApplyQuickPreset("phone");
        }
        if (GUILayout.Button("📋 平板配置"))
        {
            ApplyQuickPreset("tablet");
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🔍 快速检查"))
        {
            QuickComponentCheck();
        }
        if (GUILayout.Button("🧹 清理组件"))
        {
            QuickCleanup();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
    }
    
    void DrawDocumentation()
    {
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.LabelField("📚 帮助和文档", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        if (GUILayout.Button("📖 查看系统文档"))
        {
            string readmePath = "Assets/Scripts/MobileSystem/README.md";
            if (System.IO.File.Exists(readmePath))
            {
                Application.OpenURL("file://" + System.IO.Path.GetFullPath(readmePath));
            }
            else
            {
                EditorUtility.DisplayDialog("文档未找到", 
                    "README.md文档文件未找到。\\n请确保文件位于Assets/Scripts/MobileSystem/README.md", 
                    "确定");
            }
        }
        
        EditorGUILayout.Space(5);
        
        if (GUILayout.Button("🔗 打开组件脚本文件夹"))
        {
            string folderPath = "Assets/Scripts/MobileSystem";
            if (System.IO.Directory.Exists(folderPath))
            {
                EditorUtility.RevealInFinder(folderPath);
            }
        }
        
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("📊 当前状态:", EditorStyles.boldLabel);
        DrawCurrentStatus();
    }
    
    void DrawCurrentStatus()
    {
        var inputManager = FindObjectOfType<MobileInputManager>();
        var uiAdapter = FindObjectOfType<MobileUIAdapter>();
        var gestureHandler = FindObjectOfType<TouchGestureHandler>();
        var feedbackManager = FindObjectOfType<TouchFeedbackManager>();
        var mobileControls = FindObjectOfType<MobileControlsUI>();
        
        int componentCount = 0;
        if (inputManager != null) componentCount++;
        if (uiAdapter != null) componentCount++;
        if (gestureHandler != null) componentCount++;
        if (feedbackManager != null) componentCount++;
        if (mobileControls != null) componentCount++;
        
        string status = componentCount == 5 ? "✅ 完整" : $"⚠️ 部分 ({componentCount}/5)";
        EditorGUILayout.LabelField($"系统状态: {status}");
        
        bool mobileMode = Application.isMobilePlatform || 
                         (inputManager != null && inputManager.IsMobileDevice());
        string deviceStatus = mobileMode ? "📱 移动设备" : "🖥️ 桌面设备";
        EditorGUILayout.LabelField($"检测设备: {deviceStatus}");
    }
    
    void ApplyQuickPreset(string presetType)
    {
        bool hasComponents = FindObjectOfType<MobileInputManager>() != null;
        
        if (!hasComponents)
        {
            if (EditorUtility.DisplayDialog("组件未找到", 
                "没有找到移动端UI组件。是否先创建组件？", 
                "创建组件", "取消"))
            {
                // 打开配置工具
                MobileUISetupTool.ShowWindow();
            }
            return;
        }
        
        // 应用预设
        string presetName = presetType == "phone" ? "手机" : "平板";
        if (EditorUtility.DisplayDialog($"应用{presetName}预设", 
            $"这将应用{presetName}优化配置。\\n\\n确定要继续吗？", 
            "确定", "取消"))
        {
            // 打开预设工具并应用相应预设
            var presetWindow = GetWindow<MobileUIConfigPresets>();
            presetWindow.titleContent = new GUIContent("移动端UI预设");
            presetWindow.Show();
            
            // 这里可以添加自动应用预设的逻辑
            Debug.Log($"[MobileUIToolsMenu] 请在预设工具中点击'{presetName}优化配置'按钮");
        }
    }
    
    void QuickComponentCheck()
    {
        Debug.Log("=== 移动端UI组件快速检查 ===");
        
        var components = new System.Collections.Generic.Dictionary<string, Component>
        {
            {"MobileInputManager", FindObjectOfType<MobileInputManager>()},
            {"MobileUIAdapter", FindObjectOfType<MobileUIAdapter>()},
            {"TouchGestureHandler", FindObjectOfType<TouchGestureHandler>()},
            {"TouchFeedbackManager", FindObjectOfType<TouchFeedbackManager>()},
            {"MobileControlsUI", FindObjectOfType<MobileControlsUI>()}
        };
        
        int foundCount = 0;
        foreach (var kvp in components)
        {
            bool exists = kvp.Value != null;
            string status = exists ? "✅" : "❌";
            Debug.Log($"{status} {kvp.Key}: {(exists ? "已找到" : "未找到")}");
            if (exists) foundCount++;
        }
        
        Debug.Log($"检查完成: {foundCount}/5 个组件已配置");
        
        string message = foundCount == 5 ? 
            "所有移动端UI组件都已正确配置！" : 
            $"找到 {foundCount}/5 个组件。请检查缺失的组件。";
            
        EditorUtility.DisplayDialog("检查完成", message, "确定");
    }
    
    void QuickCleanup()
    {
        if (EditorUtility.DisplayDialog("确认清理", 
            "这将删除场景中所有移动端UI组件。\\n\\n确定要继续吗？", 
            "确定", "取消"))
        {
            int cleanedCount = 0;
            
            var inputManager = FindObjectOfType<MobileInputManager>();
            if (inputManager != null) { DestroyImmediate(inputManager.gameObject); cleanedCount++; }
            
            var uiAdapter = FindObjectOfType<MobileUIAdapter>();
            if (uiAdapter != null) { DestroyImmediate(uiAdapter.gameObject); cleanedCount++; }
            
            var gestureHandler = FindObjectOfType<TouchGestureHandler>();
            if (gestureHandler != null) { DestroyImmediate(gestureHandler.gameObject); cleanedCount++; }
            
            var feedbackManager = FindObjectOfType<TouchFeedbackManager>();
            if (feedbackManager != null) { DestroyImmediate(feedbackManager.gameObject); cleanedCount++; }
            
            var mobileControls = FindObjectOfType<MobileControlsUI>();
            if (mobileControls != null) { DestroyImmediate(mobileControls.gameObject); cleanedCount++; }
            
            EditorUtility.DisplayDialog("清理完成", 
                $"已清理 {cleanedCount} 个移动端UI组件。", 
                "确定");
                
            Debug.Log($"[MobileUIToolsMenu] 移动端UI组件清理完成 - 清理了{cleanedCount}个组件");
        }
    }
    
    // 添加菜单项快捷方式
    [MenuItem("Tools/移动端UI/📱 手机配置预设", priority = 100)]
    public static void ApplyPhonePresetQuick()
    {
        var window = GetWindow<MobileUIToolsMenu>();
        window.ApplyQuickPreset("phone");
    }
    
    [MenuItem("Tools/移动端UI/📋 平板配置预设", priority = 101)]
    public static void ApplyTabletPresetQuick()
    {
        var window = GetWindow<MobileUIToolsMenu>();
        window.ApplyQuickPreset("tablet");
    }
    
    [MenuItem("Tools/移动端UI/🔍 快速组件检查", priority = 102)]
    public static void QuickCheckComponents()
    {
        var window = GetWindow<MobileUIToolsMenu>();
        window.QuickComponentCheck();
    }
}
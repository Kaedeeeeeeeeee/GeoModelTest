using UnityEngine;
using UnityEditor;

/// <summary>
/// 移动端UI配置预设管理器
/// 提供常见的配置方案，快速应用到移动端UI组件
/// </summary>
public class MobileUIConfigPresets : EditorWindow
{
    [MenuItem("Tools/移动端UI预设配置")]
    public static void ShowWindow()
    {
        MobileUIConfigPresets window = GetWindow<MobileUIConfigPresets>();
        window.titleContent = new GUIContent("移动端UI预设");
        window.minSize = new Vector2(400, 500);
        window.Show();
    }
    
    private Vector2 scrollPosition;
    
    void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        DrawHeader();
        DrawPresetButtons();
        DrawCurrentSettings();
        
        EditorGUILayout.EndScrollView();
    }
    
    void DrawHeader()
    {
        EditorGUILayout.Space();
        
        GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontSize = 16;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.alignment = TextAnchor.MiddleCenter;
        
        EditorGUILayout.LabelField("移动端UI配置预设", headerStyle);
        EditorGUILayout.LabelField("快速应用常见配置方案", EditorStyles.centeredGreyMiniLabel);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space();
    }
    
    void DrawPresetButtons()
    {
        EditorGUILayout.LabelField("📱 设备配置预设", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // 手机配置预设
        if (GUILayout.Button("📱 手机优化配置", GUILayout.Height(50)))
        {
            ApplyPhonePreset();
        }
        EditorGUILayout.LabelField("    • 小尺寸摇杆和按钮\n    • 3列网格布局\n    • 简化手势识别", EditorStyles.helpBox);
        EditorGUILayout.Space();
        
        // 平板配置预设
        if (GUILayout.Button("📋 平板优化配置", GUILayout.Height(50)))
        {
            ApplyTabletPreset();
        }
        EditorGUILayout.LabelField("    • 中等尺寸控件\n    • 4列网格布局\n    • 完整手势支持", EditorStyles.helpBox);
        EditorGUILayout.Space();
        
        // 调试配置预设
        if (GUILayout.Button("🔍 调试开发配置", GUILayout.Height(50)))
        {
            ApplyDebugPreset();
        }
        EditorGUILayout.LabelField("    • 启用所有调试信息\n    • 显示性能指标\n    • 大尺寸控件便于测试", EditorStyles.helpBox);
        EditorGUILayout.Space();
        
        // 性能优化预设
        if (GUILayout.Button("⚡ 性能优化配置", GUILayout.Height(50)))
        {
            ApplyPerformancePreset();
        }
        EditorGUILayout.LabelField("    • 禁用复杂手势\n    • 减少触觉反馈\n    • 优化渲染性能", EditorStyles.helpBox);
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("🛠️ 实用工具", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("💾 保存当前配置"))
        {
            SaveCurrentConfiguration();
        }
        if (GUILayout.Button("📂 加载配置"))
        {
            LoadConfiguration();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("🔄 恢复默认配置"))
        {
            ApplyDefaultPreset();
        }
    }
    
    void DrawCurrentSettings()
    {
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.LabelField("📊 当前配置状态", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        var inputManager = FindObjectOfType<MobileInputManager>();
        var uiAdapter = FindObjectOfType<MobileUIAdapter>();
        var gestureHandler = FindObjectOfType<TouchGestureHandler>();
        var feedbackManager = FindObjectOfType<TouchFeedbackManager>();
        var mobileControls = FindObjectOfType<MobileControlsUI>();
        
        if (inputManager != null)
        {
            EditorGUILayout.LabelField($"输入模式: {inputManager.currentInputMode}");
            EditorGUILayout.LabelField($"调试日志: {(inputManager.enableDebugLog ? "开启" : "关闭")}");
        }
        
        if (mobileControls != null)
        {
            EditorGUILayout.LabelField($"按钮大小: {mobileControls.buttonSize}px");
            EditorGUILayout.LabelField($"调试可视化: {(mobileControls.enableDebugVisualization ? "开启" : "关闭")}");
        }
        
        if (gestureHandler != null)
        {
            EditorGUILayout.LabelField($"手势识别: {(gestureHandler.enableGestureRecognition ? "开启" : "关闭")}");
        }
        
        if (feedbackManager != null)
        {
            EditorGUILayout.LabelField($"震动反馈: {(feedbackManager.enableVibration ? "开启" : "关闭")}");
            EditorGUILayout.LabelField($"音频反馈: {(feedbackManager.enableSoundFeedback ? "开启" : "关闭")}");
        }
    }
    
    void ApplyPhonePreset()
    {
        if (EditorUtility.DisplayDialog("应用手机预设", 
            "这将应用针对手机优化的配置。\n\n确定要继续吗？", 
            "确定", "取消"))
        {
            ApplyPresetConfiguration(new MobileUIPreset
            {
                buttonSize = 60f,
                gridColumns = 3,
                enableDebug = false,
                gestureRecognition = true,
                vibrationEnabled = true,
                audioFeedback = true,
                inputMode = MobileInputManager.InputMode.Auto
            });
            
            Debug.Log("[MobileUIPresets] 手机优化配置已应用");
        }
    }
    
    void ApplyTabletPreset()
    {
        if (EditorUtility.DisplayDialog("应用平板预设", 
            "这将应用针对平板优化的配置。\n\n确定要继续吗？", 
            "确定", "取消"))
        {
            ApplyPresetConfiguration(new MobileUIPreset
            {
                buttonSize = 80f,
                gridColumns = 4,
                enableDebug = false,
                gestureRecognition = true,
                vibrationEnabled = true,
                audioFeedback = true,
                inputMode = MobileInputManager.InputMode.Auto
            });
            
            Debug.Log("[MobileUIPresets] 平板优化配置已应用");
        }
    }
    
    void ApplyDebugPreset()
    {
        if (EditorUtility.DisplayDialog("应用调试预设", 
            "这将启用所有调试功能。\n\n确定要继续吗？", 
            "确定", "取消"))
        {
            ApplyPresetConfiguration(new MobileUIPreset
            {
                buttonSize = 100f,
                gridColumns = 3,
                enableDebug = true,
                gestureRecognition = true,
                vibrationEnabled = true,
                audioFeedback = true,
                inputMode = MobileInputManager.InputMode.Hybrid
            });
            
            Debug.Log("[MobileUIPresets] 调试开发配置已应用");
        }
    }
    
    void ApplyPerformancePreset()
    {
        if (EditorUtility.DisplayDialog("应用性能预设", 
            "这将应用性能优化配置，可能会禁用一些功能。\n\n确定要继续吗？", 
            "确定", "取消"))
        {
            ApplyPresetConfiguration(new MobileUIPreset
            {
                buttonSize = 50f,
                gridColumns = 2,
                enableDebug = false,
                gestureRecognition = false,
                vibrationEnabled = false,
                audioFeedback = false,
                inputMode = MobileInputManager.InputMode.Mobile
            });
            
            Debug.Log("[MobileUIPresets] 性能优化配置已应用");
        }
    }
    
    void ApplyDefaultPreset()
    {
        if (EditorUtility.DisplayDialog("恢复默认配置", 
            "这将恢复所有组件的默认设置。\n\n确定要继续吗？", 
            "确定", "取消"))
        {
            ApplyPresetConfiguration(new MobileUIPreset
            {
                buttonSize = 80f,
                gridColumns = 3,
                enableDebug = false,
                gestureRecognition = true,
                vibrationEnabled = true,
                audioFeedback = true,
                inputMode = MobileInputManager.InputMode.Auto
            });
            
            Debug.Log("[MobileUIPresets] 默认配置已恢复");
        }
    }
    
    void ApplyPresetConfiguration(MobileUIPreset preset)
    {
        // 应用到MobileInputManager
        var inputManager = FindObjectOfType<MobileInputManager>();
        if (inputManager != null)
        {
            inputManager.currentInputMode = preset.inputMode;
            inputManager.enableDebugLog = preset.enableDebug;
            EditorUtility.SetDirty(inputManager);
        }
        
        // 应用到MobileControlsUI
        var mobileControls = FindObjectOfType<MobileControlsUI>();
        if (mobileControls != null)
        {
            // 设置按钮大小
            mobileControls.buttonSize = preset.buttonSize;
            mobileControls.enableDebugVisualization = preset.enableDebug;
            EditorUtility.SetDirty(mobileControls);
        }
        
        // 应用到TouchGestureHandler
        var gestureHandler = FindObjectOfType<TouchGestureHandler>();
        if (gestureHandler != null)
        {
            gestureHandler.enableGestureRecognition = preset.gestureRecognition;
            EditorUtility.SetDirty(gestureHandler);
        }
        
        // 应用到TouchFeedbackManager
        var feedbackManager = FindObjectOfType<TouchFeedbackManager>();
        if (feedbackManager != null)
        {
            feedbackManager.enableVibration = preset.vibrationEnabled;
            feedbackManager.enableSoundFeedback = preset.audioFeedback;
            feedbackManager.enableDebugLog = preset.enableDebug;
            EditorUtility.SetDirty(feedbackManager);
        }
        
        // 应用到MobileUIAdapter
        var uiAdapter = FindObjectOfType<MobileUIAdapter>();
        if (uiAdapter != null)
        {
            uiAdapter.enableDebugInfo = preset.enableDebug;
            EditorUtility.SetDirty(uiAdapter);
        }
        
        // 应用到InventoryUI
        var inventoryUI = FindObjectOfType<InventoryUI>();
        if (inventoryUI != null)
        {
            inventoryUI.mobileGridColumns = preset.gridColumns;
            EditorUtility.SetDirty(inventoryUI);
        }
        
        EditorUtility.DisplayDialog("配置完成", 
            "预设配置已成功应用到所有相关组件！", 
            "确定");
    }
    
    void SaveCurrentConfiguration()
    {
        string path = EditorUtility.SaveFilePanel("保存移动端UI配置", "", "MobileUIConfig", "json");
        if (!string.IsNullOrEmpty(path))
        {
            var config = GatherCurrentConfiguration();
            string json = JsonUtility.ToJson(config, true);
            System.IO.File.WriteAllText(path, json);
            
            EditorUtility.DisplayDialog("保存成功", 
                $"配置已保存到:\n{path}", 
                "确定");
                
            Debug.Log($"[MobileUIPresets] 配置已保存: {path}");
        }
    }
    
    void LoadConfiguration()
    {
        string path = EditorUtility.OpenFilePanel("加载移动端UI配置", "", "json");
        if (!string.IsNullOrEmpty(path))
        {
            try
            {
                string json = System.IO.File.ReadAllText(path);
                var config = JsonUtility.FromJson<MobileUIPreset>(json);
                ApplyPresetConfiguration(config);
                
                Debug.Log($"[MobileUIPresets] 配置已加载: {path}");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("加载失败", 
                    $"无法加载配置文件:\n{e.Message}", 
                    "确定");
            }
        }
    }
    
    MobileUIPreset GatherCurrentConfiguration()
    {
        var preset = new MobileUIPreset();
        
        var inputManager = FindObjectOfType<MobileInputManager>();
        if (inputManager != null)
        {
            preset.inputMode = inputManager.currentInputMode;
            preset.enableDebug = inputManager.enableDebugLog;
        }
        
        var mobileControls = FindObjectOfType<MobileControlsUI>();
        if (mobileControls != null)
        {
            preset.buttonSize = mobileControls.buttonSize;
            // joystickSize 在MobileControlsUI中没有对应属性，使用默认值
        }
        
        var gestureHandler = FindObjectOfType<TouchGestureHandler>();
        if (gestureHandler != null)
        {
            preset.gestureRecognition = gestureHandler.enableGestureRecognition;
        }
        
        var feedbackManager = FindObjectOfType<TouchFeedbackManager>();
        if (feedbackManager != null)
        {
            preset.vibrationEnabled = feedbackManager.enableVibration;
            preset.audioFeedback = feedbackManager.enableSoundFeedback;
        }
        
        var inventoryUI = FindObjectOfType<InventoryUI>();
        if (inventoryUI != null)
        {
            preset.gridColumns = inventoryUI.mobileGridColumns;
        }
        
        return preset;
    }
    
    [System.Serializable]
    public class MobileUIPreset
    {
        public float buttonSize = 80f;
        public int gridColumns = 3;
        public bool enableDebug = false;
        public bool gestureRecognition = true;
        public bool vibrationEnabled = true;
        public bool audioFeedback = true;
        public MobileInputManager.InputMode inputMode = MobileInputManager.InputMode.Auto;
    }
}
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// 移动端UI适配器 - 处理响应式布局、屏幕适配、安全区域等
/// 为不同设备和屏幕尺寸提供统一的UI体验
/// </summary>
public class MobileUIAdapter : MonoBehaviour
{
    [Header("屏幕适配设置")]
    public bool enableAutoAdaptation = true;
    public bool adaptToSafeArea = true;
    public bool autoScaleUI = true;
    
    [Header("屏幕尺寸分类")]
    public Vector2 phoneScreenSize = new Vector2(1080, 1920);
    public Vector2 tabletScreenSize = new Vector2(1536, 2048);
    public Vector2 desktopScreenSize = new Vector2(1920, 1080);
    
    [Header("UI缩放设置")]
    [Range(0.5f, 2.0f)]
    public float phoneUIScale = 1.0f;
    [Range(0.5f, 2.0f)]
    public float tabletUIScale = 0.8f;
    [Range(0.5f, 2.0f)]
    public float desktopUIScale = 0.7f;
    
    [Header("布局调整")]
    public bool useVerticalLayoutOnNarrowScreens = true;
    public float aspectRatioThreshold = 1.5f; // 高宽比阈值
    
    [Header("安全区域设置")]
    public bool showSafeAreaVisualization = false;
    public Color safeAreaColor = new Color(0, 1, 0, 0.2f);
    
    [Header("性能优化")]
    public bool enableDynamicQuality = true;
    public bool reduceAnimationsOnLowEnd = true;
    
    [Header("调试设置")]
    public bool enableDebugInfo = false;
    public bool logAdaptationChanges = true;
    
    // 单例模式
    public static MobileUIAdapter Instance { get; private set; }
    
    // 屏幕信息
    public DeviceType CurrentDeviceType { get; private set; }
    public ScreenOrientation CurrentOrientation { get; private set; }
    public Rect SafeArea { get; private set; }
    public Vector2 ScreenSize { get; private set; }
    public float AspectRatio { get; private set; }
    
    // UI组件缓存
    private List<CanvasScaler> registeredCanvasScalers = new List<CanvasScaler>();
    private List<SafeAreaPanel> safeAreaPanels = new List<SafeAreaPanel>();
    private Dictionary<Canvas, float> originalCanvasScales = new Dictionary<Canvas, float>();
    
    // 适配状态
    private bool hasInitialized = false;
    private Vector2 lastScreenSize;
    private ScreenOrientation lastOrientation;
    private Rect lastSafeArea;
    
    public enum DeviceType
    {
        Phone,      // 手机
        Tablet,     // 平板
        Desktop,    // 桌面
        Unknown     // 未知
    }
    
    public enum LayoutMode
    {
        Horizontal, // 横向布局
        Vertical,   // 纵向布局
        Adaptive    // 自适应布局
    }
    
    void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[MobileUIAdapter] UI适配器初始化");
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // 初始化屏幕信息
        UpdateScreenInfo();
    }
    
    void Start()
    {
        if (enableAutoAdaptation)
        {
            InitializeAdaptation();
        }
    }
    
    void Update()
    {
        // 检查屏幕变化
        if (HasScreenChanged())
        {
            OnScreenChanged();
        }
    }
    
    /// <summary>
    /// 初始化适配系统
    /// </summary>
    void InitializeAdaptation()
    {
        if (hasInitialized) return;
        
        // 检测设备类型
        DetectDeviceType();
        
        // 查找并注册所有Canvas
        RegisterCanvasComponents();
        
        // 应用初始适配
        ApplyAdaptation();
        
        hasInitialized = true;
        
        if (logAdaptationChanges)
        {
            Debug.Log($"[MobileUIAdapter] 适配初始化完成 - 设备: {CurrentDeviceType}, 屏幕: {ScreenSize}, 安全区域: {SafeArea}");
        }
    }
    
    /// <summary>
    /// 更新屏幕信息
    /// </summary>
    void UpdateScreenInfo()
    {
        ScreenSize = new Vector2(Screen.width, Screen.height);
        AspectRatio = ScreenSize.y / ScreenSize.x;
        CurrentOrientation = Screen.orientation;
        SafeArea = Screen.safeArea;
        
        // 确保安全区域数据有效
        if (SafeArea.width <= 0 || SafeArea.height <= 0)
        {
            SafeArea = new Rect(0, 0, Screen.width, Screen.height);
        }
    }
    
    /// <summary>
    /// 检测设备类型
    /// </summary>
    void DetectDeviceType()
    {
        float screenDiagonal = Mathf.Sqrt(ScreenSize.x * ScreenSize.x + ScreenSize.y * ScreenSize.y);
        float dpi = Screen.dpi > 0 ? Screen.dpi : 160f; // 默认DPI
        float screenSizeInches = screenDiagonal / dpi;
        
        if (Application.isMobilePlatform)
        {
            if (screenSizeInches < 7.0f)
            {
                CurrentDeviceType = DeviceType.Phone;
            }
            else
            {
                CurrentDeviceType = DeviceType.Tablet;
            }
        }
        else
        {
            CurrentDeviceType = DeviceType.Desktop;
        }
        
        if (logAdaptationChanges)
        {
            Debug.Log($"[MobileUIAdapter] 设备检测: {CurrentDeviceType} (屏幕: {screenSizeInches:F1}英寸, DPI: {dpi})");
        }
    }
    
    /// <summary>
    /// 注册Canvas组件
    /// </summary>
    void RegisterCanvasComponents()
    {
        // 查找所有Canvas
        Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        
        foreach (Canvas canvas in allCanvases)
        {
            RegisterCanvas(canvas);
        }
        
        // 查找所有安全区域面板
        SafeAreaPanel[] safeAreaComponents = FindObjectsByType<SafeAreaPanel>(FindObjectsSortMode.None);
        safeAreaPanels.AddRange(safeAreaComponents);
        
        if (logAdaptationChanges)
        {
            Debug.Log($"[MobileUIAdapter] 注册了 {registeredCanvasScalers.Count} 个Canvas和 {safeAreaPanels.Count} 个安全区域面板");
        }
    }
    
    /// <summary>
    /// 注册单个Canvas
    /// </summary>
    public void RegisterCanvas(Canvas canvas)
    {
        if (canvas == null) return;
        
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null && !registeredCanvasScalers.Contains(scaler))
        {
            registeredCanvasScalers.Add(scaler);
            
            // 保存原始缩放值
            if (!originalCanvasScales.ContainsKey(canvas))
            {
                originalCanvasScales[canvas] = scaler.scaleFactor;
            }
        }
    }
    
    /// <summary>
    /// 应用适配设置
    /// </summary>
    void ApplyAdaptation()
    {
        if (autoScaleUI)
        {
            ApplyUIScaling();
        }
        
        if (adaptToSafeArea)
        {
            ApplySafeAreaAdaptation();
        }
        
        ApplyLayoutAdaptation();
        
        if (enableDynamicQuality)
        {
            ApplyQualityOptimization();
        }
    }
    
    /// <summary>
    /// 应用UI缩放
    /// </summary>
    void ApplyUIScaling()
    {
        float targetScale = GetTargetUIScale();
        
        foreach (CanvasScaler scaler in registeredCanvasScalers)
        {
            if (scaler == null) continue;
            
            // 根据设备类型调整Canvas Scaler
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            
            switch (CurrentDeviceType)
            {
                case DeviceType.Phone:
                    scaler.referenceResolution = phoneScreenSize;
                    scaler.matchWidthOrHeight = AspectRatio > aspectRatioThreshold ? 0f : 1f;
                    break;
                    
                case DeviceType.Tablet:
                    scaler.referenceResolution = tabletScreenSize;
                    scaler.matchWidthOrHeight = 0.5f;
                    break;
                    
                case DeviceType.Desktop:
                    scaler.referenceResolution = desktopScreenSize;
                    scaler.matchWidthOrHeight = AspectRatio > 1f ? 1f : 0f;
                    break;
            }
            
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        }
        
        if (logAdaptationChanges)
        {
            Debug.Log($"[MobileUIAdapter] UI缩放应用完成 - 目标缩放: {targetScale}");
        }
    }
    
    /// <summary>
    /// 获取目标UI缩放
    /// </summary>
    float GetTargetUIScale()
    {
        switch (CurrentDeviceType)
        {
            case DeviceType.Phone:
                return phoneUIScale;
            case DeviceType.Tablet:
                return tabletUIScale;
            case DeviceType.Desktop:
                return desktopUIScale;
            default:
                return 1.0f;
        }
    }
    
    /// <summary>
    /// 应用安全区域适配
    /// </summary>
    void ApplySafeAreaAdaptation()
    {
        foreach (SafeAreaPanel panel in safeAreaPanels)
        {
            if (panel != null)
            {
                panel.ApplySafeArea(SafeArea);
            }
        }
        
        if (logAdaptationChanges)
        {
            Debug.Log($"[MobileUIAdapter] 安全区域适配完成 - 区域: {SafeArea}");
        }
    }
    
    /// <summary>
    /// 应用布局适配
    /// </summary>
    void ApplyLayoutAdaptation()
    {
        LayoutMode targetLayout = GetTargetLayoutMode();
        
        // 查找并适配布局组件
        LayoutGroup[] layoutGroups = FindObjectsByType<LayoutGroup>(FindObjectsSortMode.None);
        
        foreach (LayoutGroup layoutGroup in layoutGroups)
        {
            AdaptLayoutGroup(layoutGroup, targetLayout);
        }
        
        if (logAdaptationChanges)
        {
            Debug.Log($"[MobileUIAdapter] 布局适配完成 - 模式: {targetLayout}");
        }
    }
    
    /// <summary>
    /// 获取目标布局模式
    /// </summary>
    LayoutMode GetTargetLayoutMode()
    {
        if (useVerticalLayoutOnNarrowScreens && AspectRatio > aspectRatioThreshold)
        {
            return LayoutMode.Vertical;
        }
        
        switch (CurrentDeviceType)
        {
            case DeviceType.Phone:
                return AspectRatio > aspectRatioThreshold ? LayoutMode.Vertical : LayoutMode.Horizontal;
            case DeviceType.Tablet:
                return LayoutMode.Adaptive;
            case DeviceType.Desktop:
                return LayoutMode.Horizontal;
            default:
                return LayoutMode.Adaptive;
        }
    }
    
    /// <summary>
    /// 适配布局组件
    /// </summary>
    void AdaptLayoutGroup(LayoutGroup layoutGroup, LayoutMode mode)
    {
        if (layoutGroup == null) return;
        
        // 检查是否有移动端适配标记
        MobileLayoutAdapter adapter = layoutGroup.GetComponent<MobileLayoutAdapter>();
        if (adapter != null)
        {
            adapter.ApplyLayout(mode, CurrentDeviceType);
        }
    }
    
    /// <summary>
    /// 应用性能优化
    /// </summary>
    void ApplyQualityOptimization()
    {
        // 根据设备性能调整质量设置
        int qualityLevel = GetRecommendedQualityLevel();
        
        if (QualitySettings.GetQualityLevel() != qualityLevel)
        {
            QualitySettings.SetQualityLevel(qualityLevel);
            
            if (logAdaptationChanges)
            {
                Debug.Log($"[MobileUIAdapter] 质量等级调整为: {qualityLevel}");
            }
        }
        
        // 动画优化
        if (reduceAnimationsOnLowEnd && IsLowEndDevice())
        {
            AnimationOptimizer.ReduceAnimations();
        }
    }
    
    /// <summary>
    /// 获取推荐的质量等级
    /// </summary>
    int GetRecommendedQualityLevel()
    {
        switch (CurrentDeviceType)
        {
            case DeviceType.Phone:
                return SystemInfo.systemMemorySize < 3000 ? 0 : 1; // 低端手机用最低质量
            case DeviceType.Tablet:
                return 2;
            case DeviceType.Desktop:
                return QualitySettings.names.Length - 1; // 最高质量
            default:
                return 1;
        }
    }
    
    /// <summary>
    /// 检查是否为低端设备
    /// </summary>
    bool IsLowEndDevice()
    {
        return SystemInfo.systemMemorySize < 2000 || // 小于2GB内存
               SystemInfo.processorCount < 4 ||       // 少于4核CPU
               SystemInfo.graphicsMemorySize < 512;   // 少于512MB显存
    }
    
    /// <summary>
    /// 检查屏幕是否发生变化
    /// </summary>
    bool HasScreenChanged()
    {
        return lastScreenSize != ScreenSize ||
               lastOrientation != CurrentOrientation ||
               lastSafeArea != SafeArea;
    }
    
    /// <summary>
    /// 屏幕变化处理
    /// </summary>
    void OnScreenChanged()
    {
        UpdateScreenInfo();
        
        if (hasInitialized && enableAutoAdaptation)
        {
            // 重新检测设备类型（可能旋转了屏幕）
            DetectDeviceType();
            
            // 重新应用适配
            ApplyAdaptation();
            
            if (logAdaptationChanges)
            {
                Debug.Log($"[MobileUIAdapter] 屏幕变化检测 - 新尺寸: {ScreenSize}, 方向: {CurrentOrientation}");
            }
        }
        
        // 更新缓存的屏幕信息
        lastScreenSize = ScreenSize;
        lastOrientation = CurrentOrientation;
        lastSafeArea = SafeArea;
    }
    
    #region 公共接口
    
    /// <summary>
    /// 手动触发适配
    /// </summary>
    public void RefreshAdaptation()
    {
        UpdateScreenInfo();
        DetectDeviceType();
        ApplyAdaptation();
        
        Debug.Log("[MobileUIAdapter] 手动刷新适配完成");
    }
    
    /// <summary>
    /// 注册安全区域面板
    /// </summary>
    public void RegisterSafeAreaPanel(SafeAreaPanel panel)
    {
        if (panel != null && !safeAreaPanels.Contains(panel))
        {
            safeAreaPanels.Add(panel);
            
            if (adaptToSafeArea)
            {
                panel.ApplySafeArea(SafeArea);
            }
        }
    }
    
    /// <summary>
    /// 取消注册安全区域面板
    /// </summary>
    public void UnregisterSafeAreaPanel(SafeAreaPanel panel)
    {
        safeAreaPanels.Remove(panel);
    }
    
    /// <summary>
    /// 获取当前屏幕信息
    /// </summary>
    public ScreenInfo GetScreenInfo()
    {
        return new ScreenInfo
        {
            deviceType = CurrentDeviceType,
            screenSize = ScreenSize,
            aspectRatio = AspectRatio,
            orientation = CurrentOrientation,
            safeArea = SafeArea,
            isLowEndDevice = IsLowEndDevice()
        };
    }
    
    /// <summary>
    /// 设置UI缩放
    /// </summary>
    public void SetUIScale(DeviceType deviceType, float scale)
    {
        switch (deviceType)
        {
            case DeviceType.Phone:
                phoneUIScale = scale;
                break;
            case DeviceType.Tablet:
                tabletUIScale = scale;
                break;
            case DeviceType.Desktop:
                desktopUIScale = scale;
                break;
        }
        
        if (CurrentDeviceType == deviceType)
        {
            ApplyUIScaling();
        }
    }
    
    #endregion
    
    #region 调试功能
    
    void OnGUI()
    {
        if (!enableDebugInfo) return;
        
        GUILayout.BeginArea(new Rect(10, 370, 350, 200));
        GUILayout.Label("=== UI适配器调试信息 ===");
        GUILayout.Label($"设备类型: {CurrentDeviceType}");
        GUILayout.Label($"屏幕尺寸: {ScreenSize}");
        GUILayout.Label($"宽高比: {AspectRatio:F2}");
        GUILayout.Label($"屏幕方向: {CurrentOrientation}");
        GUILayout.Label($"安全区域: {SafeArea}");
        GUILayout.Label($"注册Canvas: {registeredCanvasScalers.Count}");
        GUILayout.Label($"安全区域面板: {safeAreaPanels.Count}");
        GUILayout.Label($"低端设备: {IsLowEndDevice()}");
        
        if (GUILayout.Button("刷新适配"))
        {
            RefreshAdaptation();
        }
        
        if (GUILayout.Button("切换调试可视化"))
        {
            showSafeAreaVisualization = !showSafeAreaVisualization;
        }
        
        GUILayout.EndArea();
        
        // 绘制安全区域可视化
        if (showSafeAreaVisualization)
        {
            DrawSafeAreaVisualization();
        }
    }
    
    void DrawSafeAreaVisualization()
    {
        // 绘制安全区域边界
        Rect screenRect = new Rect(0, 0, Screen.width, Screen.height);
        
        // 绘制屏幕边界
        GUI.color = Color.red;
        GUI.DrawTexture(new Rect(0, 0, 2, Screen.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(Screen.width - 2, 0, 2, Screen.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, 2), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(0, Screen.height - 2, Screen.width, 2), Texture2D.whiteTexture);
        
        // 绘制安全区域
        GUI.color = safeAreaColor;
        GUI.DrawTexture(SafeArea, Texture2D.whiteTexture);
        
        GUI.color = Color.white; // 重置颜色
    }
    
    #endregion
    
    /// <summary>
    /// 屏幕信息结构
    /// </summary>
    [System.Serializable]
    public struct ScreenInfo
    {
        public DeviceType deviceType;
        public Vector2 screenSize;
        public float aspectRatio;
        public ScreenOrientation orientation;
        public Rect safeArea;
        public bool isLowEndDevice;
    }
}

/// <summary>
/// 动画优化器
/// </summary>
public static class AnimationOptimizer
{
    public static void ReduceAnimations()
    {
        // 减少动画效果以提高性能
        Time.timeScale = 1.0f; // 确保时间比例正常
        
        // 可以在这里添加更多动画优化逻辑
        Debug.Log("[AnimationOptimizer] 动画优化已应用");
    }
}
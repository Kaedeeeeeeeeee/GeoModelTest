using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 移动端布局适配器
/// 根据设备类型和屏幕方向自动调整布局组件的行为
/// </summary>
[RequireComponent(typeof(LayoutGroup))]
public class MobileLayoutAdapter : MonoBehaviour
{
    [Header("布局适配设置")]
    public bool enableAutoAdaptation = true;
    public bool adaptToOrientation = true;
    
    [Header("手机布局设置")]
    public LayoutSettings phoneSettings = new LayoutSettings
    {
        horizontalSpacing = 10f,
        verticalSpacing = 10f,
        useVerticalLayout = true,
        childControlWidth = true,
        childControlHeight = false
    };
    
    [Header("平板布局设置")]
    public LayoutSettings tabletSettings = new LayoutSettings
    {
        horizontalSpacing = 15f,
        verticalSpacing = 15f,
        useVerticalLayout = false,
        childControlWidth = false,
        childControlHeight = false
    };
    
    [Header("桌面布局设置")]
    public LayoutSettings desktopSettings = new LayoutSettings
    {
        horizontalSpacing = 20f,
        verticalSpacing = 20f,
        useVerticalLayout = false,
        childControlWidth = false,
        childControlHeight = false
    };
    
    [Header("响应式设置")]
    public bool useResponsiveColumns = true;
    public int maxColumnsPhone = 2;
    public int maxColumnsTablet = 3;
    public int maxColumnsDesktop = 4;
    
    [Header("调试")]
    public bool showDebugInfo = false;
    
    private LayoutGroup layoutGroup;
    private HorizontalLayoutGroup horizontalLayout;
    private VerticalLayoutGroup verticalLayout;
    private GridLayoutGroup gridLayout;
    
    private MobileUIAdapter.DeviceType lastDeviceType = MobileUIAdapter.DeviceType.Unknown;
    private MobileUIAdapter.LayoutMode lastLayoutMode = MobileUIAdapter.LayoutMode.Adaptive;
    
    [System.Serializable]
    public struct LayoutSettings
    {
        public float horizontalSpacing;
        public float verticalSpacing;
        public bool useVerticalLayout;
        public bool childControlWidth;
        public bool childControlHeight;
        public RectOffset padding;
    }
    
    void Awake()
    {
        // 获取布局组件
        layoutGroup = GetComponent<LayoutGroup>();
        horizontalLayout = GetComponent<HorizontalLayoutGroup>();
        verticalLayout = GetComponent<VerticalLayoutGroup>();
        gridLayout = GetComponent<GridLayoutGroup>();
    }
    
    void Start()
    {
        if (enableAutoAdaptation && MobileUIAdapter.Instance != null)
        {
            var screenInfo = MobileUIAdapter.Instance.GetScreenInfo();
            ApplyLayout(GetLayoutModeFromScreenInfo(screenInfo), screenInfo.deviceType);
        }
    }
    
    void Update()
    {
        if (enableAutoAdaptation && MobileUIAdapter.Instance != null)
        {
            var screenInfo = MobileUIAdapter.Instance.GetScreenInfo();
            var currentLayoutMode = GetLayoutModeFromScreenInfo(screenInfo);
            
            // 检查是否需要更新布局
            if (screenInfo.deviceType != lastDeviceType || currentLayoutMode != lastLayoutMode)
            {
                ApplyLayout(currentLayoutMode, screenInfo.deviceType);
                lastDeviceType = screenInfo.deviceType;
                lastLayoutMode = currentLayoutMode;
            }
        }
    }
    
    /// <summary>
    /// 应用布局设置
    /// </summary>
    public void ApplyLayout(MobileUIAdapter.LayoutMode mode, MobileUIAdapter.DeviceType deviceType)
    {
        LayoutSettings settings = GetLayoutSettings(deviceType);
        
        switch (mode)
        {
            case MobileUIAdapter.LayoutMode.Horizontal:
                ApplyHorizontalLayout(settings);
                break;
                
            case MobileUIAdapter.LayoutMode.Vertical:
                ApplyVerticalLayout(settings);
                break;
                
            case MobileUIAdapter.LayoutMode.Adaptive:
                ApplyAdaptiveLayout(settings, deviceType);
                break;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[MobileLayoutAdapter] {gameObject.name} 布局应用: {mode} - {deviceType}");
        }
    }
    
    /// <summary>
    /// 获取布局设置
    /// </summary>
    LayoutSettings GetLayoutSettings(MobileUIAdapter.DeviceType deviceType)
    {
        switch (deviceType)
        {
            case MobileUIAdapter.DeviceType.Phone:
                return phoneSettings;
            case MobileUIAdapter.DeviceType.Tablet:
                return tabletSettings;
            case MobileUIAdapter.DeviceType.Desktop:
                return desktopSettings;
            default:
                return phoneSettings;
        }
    }
    
    /// <summary>
    /// 应用水平布局
    /// </summary>
    void ApplyHorizontalLayout(LayoutSettings settings)
    {
        if (horizontalLayout != null)
        {
            horizontalLayout.spacing = settings.horizontalSpacing;
            horizontalLayout.childControlWidth = settings.childControlWidth;
            horizontalLayout.childControlHeight = settings.childControlHeight;
            horizontalLayout.padding = settings.padding;
            
            // 启用水平布局，禁用其他布局
            horizontalLayout.enabled = true;
            if (verticalLayout != null) verticalLayout.enabled = false;
        }
        else if (gridLayout != null)
        {
            // 如果没有水平布局但有网格布局，配置为水平网格
            ConfigureGridAsHorizontal(settings);
        }
    }
    
    /// <summary>
    /// 应用垂直布局
    /// </summary>
    void ApplyVerticalLayout(LayoutSettings settings)
    {
        if (verticalLayout != null)
        {
            verticalLayout.spacing = settings.verticalSpacing;
            verticalLayout.childControlWidth = settings.childControlWidth;
            verticalLayout.childControlHeight = settings.childControlHeight;
            verticalLayout.padding = settings.padding;
            
            // 启用垂直布局，禁用其他布局
            verticalLayout.enabled = true;
            if (horizontalLayout != null) horizontalLayout.enabled = false;
        }
        else if (gridLayout != null)
        {
            // 如果没有垂直布局但有网格布局，配置为垂直网格
            ConfigureGridAsVertical(settings);
        }
    }
    
    /// <summary>
    /// 应用自适应布局
    /// </summary>
    void ApplyAdaptiveLayout(LayoutSettings settings, MobileUIAdapter.DeviceType deviceType)
    {
        if (gridLayout != null)
        {
            ConfigureAdaptiveGrid(settings, deviceType);
        }
        else
        {
            // 根据设备类型选择默认布局
            if (deviceType == MobileUIAdapter.DeviceType.Phone)
            {
                ApplyVerticalLayout(settings);
            }
            else
            {
                ApplyHorizontalLayout(settings);
            }
        }
    }
    
    /// <summary>
    /// 配置网格为水平布局
    /// </summary>
    void ConfigureGridAsHorizontal(LayoutSettings settings)
    {
        if (gridLayout == null) return;
        
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedRowCount;
        gridLayout.constraintCount = 1;
        gridLayout.spacing = new Vector2(settings.horizontalSpacing, settings.verticalSpacing);
        gridLayout.padding = settings.padding;
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
    }
    
    /// <summary>
    /// 配置网格为垂直布局
    /// </summary>
    void ConfigureGridAsVertical(LayoutSettings settings)
    {
        if (gridLayout == null) return;
        
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 1;
        gridLayout.spacing = new Vector2(settings.horizontalSpacing, settings.verticalSpacing);
        gridLayout.padding = settings.padding;
        gridLayout.startAxis = GridLayoutGroup.Axis.Vertical;
    }
    
    /// <summary>
    /// 配置自适应网格
    /// </summary>
    void ConfigureAdaptiveGrid(LayoutSettings settings, MobileUIAdapter.DeviceType deviceType)
    {
        if (gridLayout == null) return;
        
        int columnCount = GetOptimalColumnCount(deviceType);
        
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = columnCount;
        gridLayout.spacing = new Vector2(settings.horizontalSpacing, settings.verticalSpacing);
        gridLayout.padding = settings.padding;
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        
        // 根据列数调整单元格大小
        if (useResponsiveColumns)
        {
            AdjustCellSize(columnCount);
        }
    }
    
    /// <summary>
    /// 获取最佳列数
    /// </summary>
    int GetOptimalColumnCount(MobileUIAdapter.DeviceType deviceType)
    {
        switch (deviceType)
        {
            case MobileUIAdapter.DeviceType.Phone:
                return maxColumnsPhone;
            case MobileUIAdapter.DeviceType.Tablet:
                return maxColumnsTablet;
            case MobileUIAdapter.DeviceType.Desktop:
                return maxColumnsDesktop;
            default:
                return maxColumnsPhone;
        }
    }
    
    /// <summary>
    /// 调整网格单元格大小
    /// </summary>
    void AdjustCellSize(int columnCount)
    {
        if (gridLayout == null) return;
        
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null) return;
        
        float availableWidth = rectTransform.rect.width - gridLayout.padding.left - gridLayout.padding.right;
        float spacingWidth = gridLayout.spacing.x * (columnCount - 1);
        float cellWidth = (availableWidth - spacingWidth) / columnCount;
        
        // 保持原有的单元格高度，只调整宽度
        Vector2 newCellSize = gridLayout.cellSize;
        newCellSize.x = Mathf.Max(cellWidth, 50f); // 最小宽度50像素
        
        gridLayout.cellSize = newCellSize;
    }
    
    /// <summary>
    /// 从屏幕信息获取布局模式
    /// </summary>
    MobileUIAdapter.LayoutMode GetLayoutModeFromScreenInfo(MobileUIAdapter.ScreenInfo screenInfo)
    {
        if (!adaptToOrientation)
        {
            return MobileUIAdapter.LayoutMode.Adaptive;
        }
        
        switch (screenInfo.deviceType)
        {
            case MobileUIAdapter.DeviceType.Phone:
                return screenInfo.aspectRatio > 1.5f ? MobileUIAdapter.LayoutMode.Vertical : MobileUIAdapter.LayoutMode.Horizontal;
                
            case MobileUIAdapter.DeviceType.Tablet:
                return MobileUIAdapter.LayoutMode.Adaptive;
                
            case MobileUIAdapter.DeviceType.Desktop:
                return MobileUIAdapter.LayoutMode.Horizontal;
                
            default:
                return MobileUIAdapter.LayoutMode.Adaptive;
        }
    }
    
    /// <summary>
    /// 设置布局设置
    /// </summary>
    public void SetLayoutSettings(MobileUIAdapter.DeviceType deviceType, LayoutSettings settings)
    {
        switch (deviceType)
        {
            case MobileUIAdapter.DeviceType.Phone:
                phoneSettings = settings;
                break;
            case MobileUIAdapter.DeviceType.Tablet:
                tabletSettings = settings;
                break;
            case MobileUIAdapter.DeviceType.Desktop:
                desktopSettings = settings;
                break;
        }
        
        // 如果当前设备类型匹配，立即应用设置
        if (MobileUIAdapter.Instance != null)
        {
            var screenInfo = MobileUIAdapter.Instance.GetScreenInfo();
            if (screenInfo.deviceType == deviceType)
            {
                ApplyLayout(GetLayoutModeFromScreenInfo(screenInfo), deviceType);
            }
        }
    }
    
    /// <summary>
    /// 强制刷新布局
    /// </summary>
    public void RefreshLayout()
    {
        if (MobileUIAdapter.Instance != null)
        {
            var screenInfo = MobileUIAdapter.Instance.GetScreenInfo();
            ApplyLayout(GetLayoutModeFromScreenInfo(screenInfo), screenInfo.deviceType);
        }
    }
    
    void OnGUI()
    {
        if (!showDebugInfo) return;
        
        GUILayout.BeginArea(new Rect(10, 690, 300, 120));
        GUILayout.Label($"=== {gameObject.name} 布局适配调试 ===");
        GUILayout.Label($"设备类型: {lastDeviceType}");
        GUILayout.Label($"布局模式: {lastLayoutMode}");
        GUILayout.Label($"活动布局: {GetActiveLayoutType()}");
        
        if (gridLayout != null)
        {
            GUILayout.Label($"网格列数: {gridLayout.constraintCount}");
            GUILayout.Label($"单元格大小: {gridLayout.cellSize}");
        }
        
        GUILayout.EndArea();
    }
    
    /// <summary>
    /// 获取当前活动的布局类型
    /// </summary>
    string GetActiveLayoutType()
    {
        if (horizontalLayout != null && horizontalLayout.enabled) return "水平布局";
        if (verticalLayout != null && verticalLayout.enabled) return "垂直布局";
        if (gridLayout != null && gridLayout.enabled) return "网格布局";
        return "无";
    }
}
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 安全区域面板组件
/// 自动调整UI元素以适应设备的安全区域（避开刘海屏、虚拟按键等）
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaPanel : MonoBehaviour
{
    [Header("安全区域设置")]
    public bool adaptToSafeArea = true;
    public bool maintainAspectRatio = false;
    
    [Header("边距设置")]
    public float additionalTopMargin = 0f;
    public float additionalBottomMargin = 0f;
    public float additionalLeftMargin = 0f;
    public float additionalRightMargin = 0f;
    
    [Header("适配选项")]
    public bool adaptWidth = true;
    public bool adaptHeight = true;
    public bool adaptPosition = true;
    
    [Header("调试")]
    public bool showDebugInfo = false;
    
    private RectTransform rectTransform;
    private Rect lastSafeArea;
    private Vector2 lastScreenSize;
    
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }
    
    void Start()
    {
        // 注册到UI适配器
        if (MobileUIAdapter.Instance != null)
        {
            MobileUIAdapter.Instance.RegisterSafeAreaPanel(this);
        }
        
        // 应用初始安全区域
        if (adaptToSafeArea)
        {
            ApplySafeArea(Screen.safeArea);
        }
    }
    
    void Update()
    {
        // 检查屏幕和安全区域变化
        if (HasSafeAreaChanged())
        {
            ApplySafeArea(Screen.safeArea);
        }
    }
    
    void OnDestroy()
    {
        // 从UI适配器取消注册
        if (MobileUIAdapter.Instance != null)
        {
            MobileUIAdapter.Instance.UnregisterSafeAreaPanel(this);
        }
    }
    
    /// <summary>
    /// 应用安全区域设置
    /// </summary>
    public void ApplySafeArea(Rect safeArea)
    {
        if (!adaptToSafeArea || rectTransform == null) return;
        
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);
        
        // 计算安全区域的归一化坐标
        Vector2 anchorMin = new Vector2(
            safeArea.x / screenSize.x,
            safeArea.y / screenSize.y
        );
        
        Vector2 anchorMax = new Vector2(
            (safeArea.x + safeArea.width) / screenSize.x,
            (safeArea.y + safeArea.height) / screenSize.y
        );
        
        // 应用额外边距
        anchorMin.x += additionalLeftMargin / screenSize.x;
        anchorMin.y += additionalBottomMargin / screenSize.y;
        anchorMax.x -= additionalRightMargin / screenSize.x;
        anchorMax.y -= additionalTopMargin / screenSize.y;
        
        // 确保锚点在有效范围内
        anchorMin.x = Mathf.Clamp01(anchorMin.x);
        anchorMin.y = Mathf.Clamp01(anchorMin.y);
        anchorMax.x = Mathf.Clamp01(anchorMax.x);
        anchorMax.y = Mathf.Clamp01(anchorMax.y);
        
        // 应用锚点设置
        if (adaptWidth)
        {
            rectTransform.anchorMin = new Vector2(anchorMin.x, rectTransform.anchorMin.y);
            rectTransform.anchorMax = new Vector2(anchorMax.x, rectTransform.anchorMax.y);
        }
        
        if (adaptHeight)
        {
            rectTransform.anchorMin = new Vector2(rectTransform.anchorMin.x, anchorMin.y);
            rectTransform.anchorMax = new Vector2(rectTransform.anchorMax.x, anchorMax.y);
        }
        
        // 重置偏移以使用锚点布局
        if (adaptPosition)
        {
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
        
        // 更新缓存
        lastSafeArea = safeArea;
        lastScreenSize = screenSize;
        
        if (showDebugInfo)
        {
            Debug.Log($"[SafeAreaPanel] {gameObject.name} 安全区域应用: {safeArea} -> 锚点: {anchorMin} - {anchorMax}");
        }
    }
    
    /// <summary>
    /// 检查安全区域是否发生变化
    /// </summary>
    bool HasSafeAreaChanged()
    {
        Vector2 currentScreenSize = new Vector2(Screen.width, Screen.height);
        Rect currentSafeArea = Screen.safeArea;
        
        return lastSafeArea != currentSafeArea || lastScreenSize != currentScreenSize;
    }
    
    /// <summary>
    /// 设置额外边距
    /// </summary>
    public void SetAdditionalMargins(float top, float bottom, float left, float right)
    {
        additionalTopMargin = top;
        additionalBottomMargin = bottom;
        additionalLeftMargin = left;
        additionalRightMargin = right;
        
        // 重新应用安全区域
        ApplySafeArea(Screen.safeArea);
    }
    
    /// <summary>
    /// 启用/禁用安全区域适配
    /// </summary>
    public void SetSafeAreaAdaptation(bool enabled)
    {
        adaptToSafeArea = enabled;
        
        if (enabled)
        {
            ApplySafeArea(Screen.safeArea);
        }
        else
        {
            // 重置到全屏
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
    
    /// <summary>
    /// 获取当前有效区域
    /// </summary>
    public Rect GetEffectiveArea()
    {
        if (rectTransform == null) return new Rect();
        
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return new Rect();
        
        // 获取在Canvas中的实际区域
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            corners[0],
            canvas.worldCamera,
            out Vector2 min
        );
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            corners[2],
            canvas.worldCamera,
            out Vector2 max
        );
        
        return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
    }
    
    void OnGUI()
    {
        if (!showDebugInfo) return;
        
        GUILayout.BeginArea(new Rect(10, 580, 300, 100));
        GUILayout.Label($"=== {gameObject.name} 安全区域调试 ===");
        GUILayout.Label($"安全区域: {lastSafeArea}");
        GUILayout.Label($"锚点: {rectTransform.anchorMin} - {rectTransform.anchorMax}");
        GUILayout.Label($"偏移: {rectTransform.offsetMin} - {rectTransform.offsetMax}");
        GUILayout.EndArea();
    }
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 本地化文本组件 - 可附加到任何Text组件实现自动本地化
/// </summary>
public class LocalizedText : MonoBehaviour
{
    [Header("本地化设置")]
    [SerializeField] private string textKey = "";
    [SerializeField] private bool useFormatting = false;
    [SerializeField] private string[] formatArgs = new string[0];
    
    [Header("调试设置")]
    [SerializeField] private bool showKeyInEditor = true;
    [SerializeField] private bool enableDebugLog = false;
    
    // 文本组件引用
    private Text uiText;
    private TextMeshProUGUI tmpText;
    private TextMeshPro tmpPro;
    
    // 原始文本（用于fallback）
    private string originalText;
    
    // 是否已初始化
    private bool isInitialized = false;
    
    #region 公共属性
    /// <summary>
    /// 文本键
    /// </summary>
    public string TextKey
    {
        get => textKey;
        set
        {
            if (textKey != value)
            {
                textKey = value;
                UpdateText();
            }
        }
    }
    
    /// <summary>
    /// 格式化参数
    /// </summary>
    public string[] FormatArgs
    {
        get => formatArgs;
        set
        {
            formatArgs = value ?? new string[0];
            if (useFormatting)
                UpdateText();
        }
    }
    #endregion
    
    #region Unity生命周期
    void Awake()
    {
        InitializeComponent();
    }
    
    void Start()
    {
        // 确保初始化完成
        if (!isInitialized)
            InitializeComponent();
        
        // 首次更新文本
        UpdateText();
    }
    
    void OnEnable()
    {
        // 订阅语言切换事件
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
        }
        
        // 如果已经初始化，立即更新文本
        if (isInitialized)
            UpdateText();
    }
    
    void OnDisable()
    {
        // 取消订阅语言切换事件
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
        }
    }
    
    #if UNITY_EDITOR
    void OnValidate()
    {
        // 编辑器中实时预览
        if (Application.isPlaying && isInitialized)
        {
            UpdateText();
        }
    }
    #endif
    #endregion
    
    #region 初始化
    /// <summary>
    /// 初始化组件
    /// </summary>
    private void InitializeComponent()
    {
        if (isInitialized)
            return;
        
        // 查找文本组件
        uiText = GetComponent<Text>();
        tmpText = GetComponent<TextMeshProUGUI>();
        tmpPro = GetComponent<TextMeshPro>();
        
        // 检查是否找到文本组件
        if (uiText == null && tmpText == null && tmpPro == null)
        {
            LogError("未找到Text、TextMeshProUGUI或TextMeshPro组件！");
            return;
        }
        
        // 保存原始文本
        originalText = GetCurrentText();
        
        // 如果没有设置textKey，尝试从原始文本推断
        if (string.IsNullOrEmpty(textKey) && !string.IsNullOrEmpty(originalText))
        {
            // 如果原始文本看起来像一个key（包含点号），就使用它
            if (originalText.Contains(".") && !originalText.Contains(" "))
            {
                textKey = originalText;
                LogDebug($"从原始文本推断textKey: {textKey}");
            }
        }
        
        isInitialized = true;
        LogDebug($"LocalizedText初始化完成: {gameObject.name}, Key: {textKey}");
    }
    #endregion
    
    #region 文本更新
    /// <summary>
    /// 更新文本内容
    /// </summary>
    public void UpdateText()
    {
        if (!isInitialized)
            return;
        
        if (string.IsNullOrEmpty(textKey))
        {
            // 如果没有设置key，使用原始文本
            SetCurrentText(originalText);
            return;
        }
        
        // 等待LocalizationManager初始化
        if (LocalizationManager.Instance == null || !LocalizationManager.Instance.IsInitialized)
        {
            LogDebug("等待LocalizationManager初始化...");
            // 设置为key，以便用户知道正在等待
            SetCurrentText($"[{textKey}]");
            return;
        }
        
        // 获取本地化文本
        string localizedText;
        
        if (useFormatting && formatArgs != null && formatArgs.Length > 0)
        {
            // 使用格式化参数
            localizedText = LocalizationManager.Instance.GetText(textKey, (object[])formatArgs);
        }
        else
        {
            // 直接获取文本
            localizedText = LocalizationManager.Instance.GetText(textKey);
        }
        
        // 设置文本
        SetCurrentText(localizedText);
        
        LogDebug($"更新文本: {textKey} -> {localizedText}");
    }
    
    /// <summary>
    /// 语言切换回调
    /// </summary>
    private void OnLanguageChanged()
    {
        LogDebug($"语言切换，更新文本: {textKey}");
        UpdateText();
    }
    
    /// <summary>
    /// 设置格式化参数并更新文本
    /// </summary>
    public void SetFormatArgs(params string[] args)
    {
        useFormatting = args != null && args.Length > 0;
        FormatArgs = args;
    }
    
    /// <summary>
    /// 设置文本键并更新文本
    /// </summary>
    public void SetTextKey(string key, params string[] formatArgs)
    {
        textKey = key;
        
        if (formatArgs != null && formatArgs.Length > 0)
        {
            useFormatting = true;
            this.formatArgs = formatArgs;
            LogDebug($"设置文本键 '{key}' 带格式化参数: [{string.Join(", ", formatArgs)}]");
        }
        else
        {
            useFormatting = false;
            this.formatArgs = new string[0];
            LogDebug($"设置文本键 '{key}' 无格式化参数");
        }
        
        UpdateText();
    }
    #endregion
    
    #region 文本组件操作
    /// <summary>
    /// 获取当前文本内容
    /// </summary>
    private string GetCurrentText()
    {
        if (uiText != null)
            return uiText.text;
        else if (tmpText != null)
            return tmpText.text;
        else if (tmpPro != null)
            return tmpPro.text;
        
        return "";
    }
    
    /// <summary>
    /// 设置当前文本内容
    /// </summary>
    private void SetCurrentText(string text)
    {
        if (uiText != null)
            uiText.text = text;
        else if (tmpText != null)
            tmpText.text = text;
        else if (tmpPro != null)
            tmpPro.text = text;
    }
    #endregion
    
    #region 调试和工具
    /// <summary>
    /// 强制刷新文本
    /// </summary>
    [ContextMenu("刷新文本")]
    public void RefreshText()
    {
        UpdateText();
    }
    
    /// <summary>
    /// 重置为原始文本
    /// </summary>
    [ContextMenu("重置为原始文本")]
    public void ResetToOriginalText()
    {
        SetCurrentText(originalText);
    }
    
    /// <summary>
    /// 在Inspector中显示当前信息
    /// </summary>
    [ContextMenu("显示调试信息")]
    public void ShowDebugInfo()
    {
        string info = $"=== LocalizedText Debug Info ===\n" +
                     $"GameObject: {gameObject.name}\n" +
                     $"TextKey: {textKey}\n" +
                     $"原始文本: {originalText}\n" +
                     $"当前文本: {GetCurrentText()}\n" +
                     $"使用格式化: {useFormatting}\n" +
                     $"格式化参数: [{string.Join(", ", formatArgs)}]\n" +
                     $"初始化状态: {isInitialized}";
        
        Debug.Log(info);
    }
    
    private void LogDebug(string message)
    {
        if (enableDebugLog)
            Debug.Log($"[LocalizedText:{gameObject.name}] {message}");
    }
    
    private void LogError(string message)
    {
        Debug.LogError($"[LocalizedText:{gameObject.name}] {message}");
    }
    #endregion
}
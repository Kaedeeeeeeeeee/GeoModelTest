using UnityEngine;

/// <summary>
/// 本地化系统初始化器 - 确保多语言系统在游戏开始时正确初始化
/// </summary>
public class LocalizationInitializer : MonoBehaviour
{
    [Header("初始化设置")]
    [SerializeField] private bool autoInitializeOnStart = true;
    [SerializeField] private bool createSettingsManager = true;
    [SerializeField] private bool enableDebugLog = true;
    
    [Header("语言设置")]
    [SerializeField] private LanguageSettings.Language defaultLanguage = LanguageSettings.DefaultLanguage;
    [SerializeField] private bool loadSavedLanguage = true;
    
    // 初始化状态
    private bool isInitialized = false;
    
    #region Unity生命周期
    void Awake()
    {
        if (autoInitializeOnStart)
        {
            InitializeLocalizationSystem();
        }
    }
    
    void Start()
    {
        // 确保初始化完成
        if (!isInitialized)
        {
            InitializeLocalizationSystem();
        }
        
        // 延迟创建设置管理器（确保LocalizationManager已初始化）
        if (createSettingsManager)
        {
            Invoke(nameof(EnsureSettingsManager), 0.1f);
        }
    }
    #endregion
    
    #region 初始化方法
    /// <summary>
    /// 初始化本地化系统
    /// </summary>
    public void InitializeLocalizationSystem()
    {
        if (isInitialized)
        {
            LogDebug("本地化系统已经初始化");
            return;
        }
        
        LogDebug("开始初始化本地化系统...");
        
        try
        {
            // 1. 确保LocalizationManager存在
            var localizationManager = LocalizationManager.Instance;
            
            if (localizationManager == null)
            {
                LogError("无法创建LocalizationManager");
                return;
            }
            
            LogDebug("LocalizationManager已创建");
            
            // 2. 设置默认语言或加载保存的语言
            LanguageSettings.Language targetLanguage = defaultLanguage;
            
            if (loadSavedLanguage)
            {
                targetLanguage = LanguageSettings.LoadLanguagePreference();
                LogDebug($"加载保存的语言设置: {targetLanguage}");
            }
            
            // 3. 如果当前语言不是目标语言，切换语言
            if (localizationManager.CurrentLanguage != targetLanguage)
            {
                localizationManager.SwitchLanguage(targetLanguage);
                LogDebug($"切换语言到: {targetLanguage}");
            }
            
            // 4. 验证系统状态
            bool isValid = ValidateSystem();
            
            if (isValid)
            {
                isInitialized = true;
                LogDebug("本地化系统初始化完成");
                
                // 触发初始化完成事件
                OnLocalizationInitialized();
            }
            else
            {
                LogError("本地化系统初始化验证失败");
            }
        }
        catch (System.Exception e)
        {
            LogError($"本地化系统初始化异常: {e.Message}");
        }
    }
    
    /// <summary>
    /// 确保设置管理器存在
    /// </summary>
    private void EnsureSettingsManager()
    {
        var settingsManager = SettingsManager.Instance;
        
        if (settingsManager != null)
        {
            LogDebug("SettingsManager已创建");
        }
        else
        {
            LogWarning("无法创建SettingsManager");
        }
    }
    
    /// <summary>
    /// 验证系统完整性
    /// </summary>
    private bool ValidateSystem()
    {
        bool isValid = true;
        
        // 验证LocalizationManager
        var localizationManager = LocalizationManager.Instance;
        if (localizationManager == null)
        {
            LogError("LocalizationManager未找到");
            isValid = false;
        }
        else if (!localizationManager.IsInitialized)
        {
            LogError("LocalizationManager未初始化");
            isValid = false;
        }
        
        // 验证语言文件加载
        if (localizationManager != null)
        {
            string testKey = "ui.settings.title";
            string testText = localizationManager.GetText(testKey);
            
            if (testText.StartsWith("[") && testText.EndsWith("]"))
            {
                LogWarning($"测试键 '{testKey}' 未找到对应文本，可能语言文件加载失败");
                // 不设为失败，因为可能只是测试键不存在
            }
            else
            {
                LogDebug($"语言文件验证成功: {testKey} = {testText}");
            }
        }
        
        return isValid;
    }
    
    /// <summary>
    /// 初始化完成回调
    /// </summary>
    private void OnLocalizationInitialized()
    {
        // 可以在这里添加初始化完成后的逻辑
        LogDebug("本地化系统初始化完成，准备就绪");
        
        // 例如：通知其他系统本地化已准备就绪
        // EventBus.Trigger(\"LocalizationReady\");
    }
    #endregion
    
    #region 公共接口
    /// <summary>
    /// 手动初始化（用于Inspector）
    /// </summary>
    [ContextMenu("手动初始化本地化系统")]
    public void ManualInitialize()
    {
        isInitialized = false;
        InitializeLocalizationSystem();
    }
    
    /// <summary>
    /// 重新初始化系统
    /// </summary>
    [ContextMenu("重新初始化系统")]
    public void ReinitializeSystem()
    {
        LogDebug("重新初始化本地化系统...");
        
        isInitialized = false;
        
        // 清理现有实例（如果需要）
        // 注意：这里不销毁LocalizationManager，因为它是单例
        
        InitializeLocalizationSystem();
    }
    
    /// <summary>
    /// 显示系统状态
    /// </summary>
    [ContextMenu("显示系统状态")]
    public void ShowSystemStatus()
    {
        var status = GetSystemStatus();
        Debug.Log(status);
    }
    
    /// <summary>
    /// 获取系统状态信息
    /// </summary>
    public string GetSystemStatus()
    {
        var status = "=== 本地化系统状态 ===\n";
        status += $"初始化状态: {isInitialized}\n";
        status += $"自动初始化: {autoInitializeOnStart}\n";
        status += $"创建设置管理器: {createSettingsManager}\n";
        status += $"默认语言: {defaultLanguage}\n";
        status += $"加载保存的语言: {loadSavedLanguage}\n";
        
        var localizationManager = LocalizationManager.Instance;
        if (localizationManager != null)
        {
            status += $"\n{localizationManager.GetLanguageInfo()}";
        }
        else
        {
            status += "\nLocalizationManager: 未找到";
        }
        
        var settingsManager = FindFirstObjectByType<SettingsManager>();
        if (settingsManager != null)
        {
            status += $"\nSettingsManager: 已创建";
            status += $"\n设置界面状态: {(settingsManager.IsSettingsOpen ? "打开" : "关闭")}";
        }
        else
        {
            status += "\nSettingsManager: 未找到";
        }
        
        return status;
    }
    
    /// <summary>
    /// 是否已初始化
    /// </summary>
    public bool IsInitialized => isInitialized;
    #endregion
    
    #region 调试工具
    private void LogDebug(string message)
    {
        if (enableDebugLog)
            Debug.Log($"[LocalizationInitializer] {message}");
    }
    
    private void LogWarning(string message)
    {
        if (enableDebugLog)
            Debug.LogWarning($"[LocalizationInitializer] {message}");
    }
    
    private void LogError(string message)
    {
        Debug.LogError($"[LocalizationInitializer] {message}");
    }
    #endregion
}
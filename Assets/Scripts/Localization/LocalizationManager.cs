using UnityEngine;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// 本地化管理器 - 单例模式管理所有语言相关操作
/// </summary>
public class LocalizationManager : MonoBehaviour
{
    #region 单例模式
    private static LocalizationManager _instance;
    public static LocalizationManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // 查找现有实例
                _instance = FindFirstObjectByType<LocalizationManager>();
                
                // 如果没有找到，创建新实例
                if (_instance == null)
                {
                    GameObject go = new GameObject("LocalizationManager");
                    _instance = go.AddComponent<LocalizationManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    #endregion
    
    #region 事件
    /// <summary>
    /// 语言切换事件
    /// </summary>
    public System.Action OnLanguageChanged;
    #endregion
    
    #region 私有字段
    [Header("当前语言设置")]
    [SerializeField] private LanguageSettings.Language currentLanguage = LanguageSettings.DefaultLanguage;
    
    [Header("调试设置")]
    [SerializeField] private bool enableDebugLog = true;
    
    /// <summary>
    /// 当前语言的文本字典
    /// </summary>
    private Dictionary<string, string> currentLanguageDict = new Dictionary<string, string>();
    
    /// <summary>
    /// 是否已初始化
    /// </summary>
    private bool isInitialized = false;
    #endregion
    
    #region 公共属性
    /// <summary>
    /// 当前语言
    /// </summary>
    public LanguageSettings.Language CurrentLanguage 
    { 
        get => currentLanguage; 
        private set => currentLanguage = value; 
    }
    
    /// <summary>
    /// 是否已初始化
    /// </summary>
    public bool IsInitialized => isInitialized;
    #endregion
    
    #region Unity生命周期
    void Awake()
    {
        // 确保单例
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 初始化
            InitializeLocalization();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // 确保初始化完成
        if (!isInitialized)
        {
            InitializeLocalization();
        }
    }
    #endregion
    
    #region 初始化
    /// <summary>
    /// 初始化本地化系统
    /// </summary>
    private void InitializeLocalization()
    {
        if (isInitialized)
            return;
        
        LogDebug("初始化本地化系统...");
        
        // 加载保存的语言设置
        CurrentLanguage = LanguageSettings.LoadLanguagePreference();
        
        // 加载语言文件
        LoadLanguage(CurrentLanguage);
        
        isInitialized = true;
        LogDebug($"本地化系统初始化完成，当前语言: {CurrentLanguage}");
    }
    #endregion
    
    #region 语言加载
    /// <summary>
    /// 加载指定语言
    /// </summary>
    public bool LoadLanguage(LanguageSettings.Language language)
    {
        string languageCode = LanguageSettings.GetLanguageCode(language);
        string fileName = $"{languageCode}.json";

        Debug.Log($"[LocalizationManager] 加载语言文件: {fileName}");

        // 尝试从Resources加载
        string resourcePath = $"Localization/Data/{languageCode}";
        TextAsset languageFile = Resources.Load<TextAsset>(resourcePath);

        if (languageFile != null)
        {
            Debug.Log($"[LocalizationManager] Resources.Load 成功，文本长度: {languageFile.text?.Length ?? 0}");
            return ParseLanguageJson(languageFile.text, language);
        }
        Debug.LogWarning($"[LocalizationManager] Resources.Load 未找到: {resourcePath}");
        
#if !UNITY_WEBGL || UNITY_EDITOR
        // 尝试从StreamingAssets加载（WebGL 平台不支持 File.ReadAllText 读 StreamingAssets，跳过）
        string streamingPath = Path.Combine(Application.streamingAssetsPath, "Localization", fileName);
        if (File.Exists(streamingPath))
        {
            try
            {
                string jsonContent = File.ReadAllText(streamingPath);
                return ParseLanguageJson(jsonContent, language);
            }
            catch (System.Exception e)
            {
                LogError($"读取语言文件失败: {streamingPath}, 错误: {e.Message}");
            }
        }
#endif
        
        // 如果无法加载，创建默认字典
        LogWarning($"无法加载语言文件 {fileName}，使用默认文本");
        CreateDefaultLanguageDict(language);
        return false;
    }
    
    /// <summary>
    /// 解析语言JSON文件
    /// </summary>
    private bool ParseLanguageJson(string jsonContent, LanguageSettings.Language language)
    {
        currentLanguageDict.Clear();

        // 优先用 JsonUtility（性能好）
        try
        {
            var languageData = JsonUtility.FromJson<LocalizationData>(jsonContent);
            if (languageData != null && languageData.texts != null && languageData.texts.Length > 0)
            {
                foreach (var textEntry in languageData.texts)
                {
                    if (!string.IsNullOrEmpty(textEntry.key))
                    {
                        currentLanguageDict[textEntry.key] = textEntry.value ?? "";
                    }
                }
                Debug.Log($"[LocalizationManager] (JsonUtility) 加载 {currentLanguageDict.Count} 条 ({language})");
                return true;
            }
            Debug.LogWarning($"[LocalizationManager] JsonUtility 返回空（IL2CPP 可能裁剪了序列化类型），降级到 Regex 解析");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[LocalizationManager] JsonUtility 抛异常 ({e.GetType().Name}: {e.Message})，降级到 Regex 解析");
        }

        // Fallback: 不依赖反射的 Regex 解析，专门处理 {"texts":[{"key":"...","value":"..."}]} 结构
        // 在 WebGL/IL2CPP 下当 JsonUtility 因序列化元数据被裁剪而失败时使用
        return ParseWithRegex(jsonContent, language);
    }

    private bool ParseWithRegex(string jsonContent, LanguageSettings.Language language)
    {
        if (string.IsNullOrEmpty(jsonContent))
        {
            CreateDefaultLanguageDict(language);
            return false;
        }

        // 匹配 "key":"...","value":"..." 对（顺序固定，匹配项目中的实际格式）
        // (?:[^"\\]|\\.) 匹配非转义字符或转义序列（\", \\, \n 等）
        var pattern = @"""key""\s*:\s*""((?:[^""\\]|\\.)*)""\s*,\s*""value""\s*:\s*""((?:[^""\\]|\\.)*)""";
        var matches = System.Text.RegularExpressions.Regex.Matches(
            jsonContent, pattern,
            System.Text.RegularExpressions.RegexOptions.Singleline);

        foreach (System.Text.RegularExpressions.Match m in matches)
        {
            string key = UnescapeJsonString(m.Groups[1].Value);
            string val = UnescapeJsonString(m.Groups[2].Value);
            if (!string.IsNullOrEmpty(key))
            {
                currentLanguageDict[key] = val;
            }
        }

        if (currentLanguageDict.Count == 0)
        {
            Debug.LogError($"[LocalizationManager] Regex fallback 也未匹配到任何条目，JSON 长度: {jsonContent.Length}");
            CreateDefaultLanguageDict(language);
            return false;
        }

        Debug.Log($"[LocalizationManager] (Regex fallback) 加载 {currentLanguageDict.Count} 条 ({language})");
        return true;
    }

    private static string UnescapeJsonString(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var sb = new System.Text.StringBuilder(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c == '\\' && i + 1 < s.Length)
            {
                char next = s[++i];
                switch (next)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    default: sb.Append(c).Append(next); break;
                }
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
    
    /// <summary>
    /// 创建默认语言字典
    /// </summary>
    private void CreateDefaultLanguageDict(LanguageSettings.Language language)
    {
        currentLanguageDict.Clear();
        
        // 添加一些基本的默认文本
        switch (language)
        {
            case LanguageSettings.Language.ChineseSimplified:
                currentLanguageDict["ui.settings.title"] = "设置";
                currentLanguageDict["ui.settings.language"] = "语言";
                currentLanguageDict["ui.button.close"] = "关闭";
                currentLanguageDict["ui.button.confirm"] = "确定";
                currentLanguageDict["ui.button.cancel"] = "取消";
                break;
            
            case LanguageSettings.Language.English:
                currentLanguageDict["ui.settings.title"] = "Settings";
                currentLanguageDict["ui.settings.language"] = "Language";
                currentLanguageDict["ui.button.close"] = "Close";
                currentLanguageDict["ui.button.confirm"] = "Confirm";
                currentLanguageDict["ui.button.cancel"] = "Cancel";
                break;
            
            case LanguageSettings.Language.Japanese:
                currentLanguageDict["ui.settings.title"] = "設定";
                currentLanguageDict["ui.settings.language"] = "言語";
                currentLanguageDict["ui.button.close"] = "閉じる";
                currentLanguageDict["ui.button.confirm"] = "確定";
                currentLanguageDict["ui.button.cancel"] = "キャンセル";
                break;
        }
        
        LogDebug($"创建了 {currentLanguageDict.Count} 条默认文本");
    }
    #endregion
    
    #region 文本获取
    /// <summary>
    /// 获取本地化文本
    /// </summary>
    public string GetText(string key)
    {
        if (string.IsNullOrEmpty(key))
            return "[EMPTY_KEY]";
        
        if (currentLanguageDict.TryGetValue(key, out string text))
        {
            return text;
        }
        
        // 如果找不到，返回带标记的键名用于调试
        LogWarning($"找不到本地化文本: {key}");
        return $"[{key}]";
    }
    
    /// <summary>
    /// 获取本地化文本（带格式化参数）
    /// </summary>
    public string GetText(string key, params object[] args)
    {
        string text = GetText(key);
        
        if (args != null && args.Length > 0)
        {
            try
            {
                return string.Format(text, args);
            }
            catch (System.Exception e)
            {
                LogError($"格式化文本失败: {key}, 错误: {e.Message}");
                return text;
            }
        }
        
        return text;
    }

    /// <summary>
    /// 获取本地化文本；键不存在或系统尚未初始化时使用明确的本地回退文案。
    /// 与 GetText 不同，本方法不会把 [missing.key] 暴露给玩家。
    /// </summary>
    public string GetTextOrFallback(string key, string fallback, params object[] args)
    {
        string template = fallback ?? string.Empty;
        if (!string.IsNullOrEmpty(key) && currentLanguageDict.TryGetValue(key, out string localized))
        {
            template = localized ?? template;
        }

        return FormatText(template, args);
    }

    /// <summary>
    /// 可在运行时 UI 构造代码中安全使用的静态入口。
    /// </summary>
    public static string Resolve(string key, string fallback, params object[] args)
    {
        LocalizationManager manager = Instance;
        return manager != null && manager.IsInitialized
            ? manager.GetTextOrFallback(key, fallback, args)
            : FormatText(fallback ?? string.Empty, args);
    }

    /// <summary>
    /// 根据当前输入方式选择桌面或触摸端文案，避免在手机上显示键盘按键。
    /// </summary>
    public static string ResolveForCurrentInput(
        string desktopKey,
        string mobileKey,
        string desktopFallback,
        string mobileFallback,
        params object[] args)
    {
        MobileInputManager mobileManager = MobileInputManager.Instance;
        bool useTouchPrompt = mobileManager != null
            ? mobileManager.ShouldShowVirtualControls()
            : MobileInputManager.IsRuntimeMobileDevice();

        return useTouchPrompt
            ? Resolve(mobileKey, mobileFallback, args)
            : Resolve(desktopKey, desktopFallback, args);
    }

    private static string FormatText(string template, object[] args)
    {
        if (args == null || args.Length == 0)
        {
            return template ?? string.Empty;
        }

        try
        {
            return string.Format(template ?? string.Empty, args);
        }
        catch (System.FormatException)
        {
            return template ?? string.Empty;
        }
    }
    
    /// <summary>
    /// 检查是否存在指定键的文本
    /// </summary>
    public bool HasText(string key)
    {
        return !string.IsNullOrEmpty(key) && currentLanguageDict.ContainsKey(key);
    }
    #endregion
    
    #region 语言切换
    /// <summary>
    /// 切换语言
    /// </summary>
    public void SwitchLanguage(LanguageSettings.Language newLanguage)
    {
        if (newLanguage == CurrentLanguage)
        {
            LogDebug($"语言未改变: {newLanguage}");
            return;
        }
        
        LogDebug($"切换语言: {CurrentLanguage} -> {newLanguage}");
        
        // 加载新语言
        bool loadSuccess = LoadLanguage(newLanguage);
        
        if (loadSuccess || newLanguage != CurrentLanguage)
        {
            CurrentLanguage = newLanguage;
            
            // 保存设置
            LanguageSettings.SaveLanguagePreference(newLanguage);
            
            // 触发语言切换事件
            OnLanguageChanged?.Invoke();
            
            LogDebug($"语言切换完成: {newLanguage}");
        }
        else
        {
            LogError($"语言切换失败: {newLanguage}");
        }
    }
    #endregion
    
    #region 调试工具
    /// <summary>
    /// 获取所有已加载的文本键
    /// </summary>
    public string[] GetAllTextKeys()
    {
        var keys = new string[currentLanguageDict.Count];
        currentLanguageDict.Keys.CopyTo(keys, 0);
        return keys;
    }
    
    /// <summary>
    /// 获取语言统计信息
    /// </summary>
    public string GetLanguageInfo()
    {
        return $"当前语言: {CurrentLanguage} ({LanguageSettings.GetLanguageCode(CurrentLanguage)})\n" +
               $"已加载文本: {currentLanguageDict.Count} 条\n" +
               $"初始化状态: {isInitialized}";
    }
    
    private void LogDebug(string message)
    {
        if (enableDebugLog)
            Debug.Log($"[LocalizationManager] {message}");
    }
    
    private void LogWarning(string message)
    {
        if (enableDebugLog)
            Debug.LogWarning($"[LocalizationManager] {message}");
    }
    
    private void LogError(string message)
    {
        Debug.LogError($"[LocalizationManager] {message}");
    }
    #endregion
}

/// <summary>
/// 本地化数据结构
/// </summary>
[System.Serializable]
[UnityEngine.Scripting.Preserve]
public class LocalizationData
{
    [UnityEngine.Scripting.Preserve] public LocalizationTextEntry[] texts;
}

/// <summary>
/// 本地化文本条目
/// </summary>
[System.Serializable]
[UnityEngine.Scripting.Preserve]
public class LocalizationTextEntry
{
    [UnityEngine.Scripting.Preserve] public string key;
    [UnityEngine.Scripting.Preserve] public string value;
}

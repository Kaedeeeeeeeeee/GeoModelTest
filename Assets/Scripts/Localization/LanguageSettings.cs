using UnityEngine;

/// <summary>
/// 语言设置和枚举定义
/// </summary>
public static class LanguageSettings
{
    /// <summary>
    /// 支持的语言
    /// </summary>
    public enum Language
    {
        ChineseSimplified,  // 简体中文
        English,            // 英语
        Japanese           // 日语
    }
    
    /// <summary>
    /// 语言代码映射
    /// </summary>
    public static readonly System.Collections.Generic.Dictionary<Language, string> LanguageCodes = 
        new System.Collections.Generic.Dictionary<Language, string>
        {
            { Language.ChineseSimplified, "zh-CN" },
            { Language.English, "en-US" },
            { Language.Japanese, "ja-JP" }
        };
    
    /// <summary>
    /// 语言显示名称（用于UI按钮）
    /// </summary>
    public static readonly System.Collections.Generic.Dictionary<Language, string> LanguageDisplayNames = 
        new System.Collections.Generic.Dictionary<Language, string>
        {
            { Language.ChineseSimplified, "中文" },
            { Language.English, "English" },
            { Language.Japanese, "日本語" }
        };
    
    /// <summary>
    /// 默认语言
    /// </summary>
    public const Language DefaultLanguage = Language.ChineseSimplified;
    
    /// <summary>
    /// PlayerPrefs键名
    /// </summary>
    public const string LanguagePrefsKey = "GameLanguage";
    
    /// <summary>
    /// 语言文件路径模板
    /// </summary>
    public const string LanguageFilePathTemplate = "Localization/Data/{0}";
    
    /// <summary>
    /// 获取语言代码
    /// </summary>
    public static string GetLanguageCode(Language language)
    {
        return LanguageCodes.TryGetValue(language, out string code) ? code : LanguageCodes[DefaultLanguage];
    }
    
    /// <summary>
    /// 获取语言显示名称
    /// </summary>
    public static string GetLanguageDisplayName(Language language)
    {
        return LanguageDisplayNames.TryGetValue(language, out string name) ? name : LanguageDisplayNames[DefaultLanguage];
    }
    
    /// <summary>
    /// 从代码获取语言枚举
    /// </summary>
    public static Language GetLanguageFromCode(string code)
    {
        foreach (var kvp in LanguageCodes)
        {
            if (kvp.Value == code)
                return kvp.Key;
        }
        return DefaultLanguage;
    }
    
    /// <summary>
    /// 保存语言设置
    /// </summary>
    public static void SaveLanguagePreference(Language language)
    {
        PlayerPrefs.SetString(LanguagePrefsKey, GetLanguageCode(language));
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// 加载语言设置
    /// </summary>
    public static Language LoadLanguagePreference()
    {
        string savedCode = PlayerPrefs.GetString(LanguagePrefsKey, GetLanguageCode(DefaultLanguage));
        return GetLanguageFromCode(savedCode);
    }
}
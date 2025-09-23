#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 多语言系统编辑器工具
/// </summary>
public class LocalizationTools : EditorWindow
{
    [MenuItem("Tools/Localization/多语言工具")]
    public static void ShowWindow()
    {
        GetWindow<LocalizationTools>("多语言工具");
    }
    
    private Vector2 scrollPosition;
    private string newKey = "";
    private string newValue = "";
    private LanguageSettings.Language selectedLanguage = LanguageSettings.Language.Japanese;
    
    void OnGUI()
    {
        GUILayout.Label("多语言系统工具", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        // 系统状态
        DrawSystemStatus();
        
        EditorGUILayout.Space();
        
        // 快速操作
        DrawQuickActions();
        
        EditorGUILayout.Space();
        
        // 文本组件转换
        DrawTextConversion();
        
        EditorGUILayout.Space();
        
        // 语言管理
        DrawLanguageManagement();
        
        EditorGUILayout.EndScrollView();
    }
    
    /// <summary>
    /// 绘制系统状态
    /// </summary>
    private void DrawSystemStatus()
    {
        EditorGUILayout.LabelField("系统状态", EditorStyles.boldLabel);
        
        var localizationManager = FindFirstObjectByType<LocalizationManager>();
        
        if (localizationManager != null)
        {
            EditorGUILayout.LabelField("LocalizationManager", "已创建", EditorStyles.helpBox);
            EditorGUILayout.LabelField("当前语言", localizationManager.CurrentLanguage.ToString());
            EditorGUILayout.LabelField("初始化状态", localizationManager.IsInitialized ? "已初始化" : "未初始化");
            
            if (localizationManager.IsInitialized)
            {
                if (GUILayout.Button("显示系统信息"))
                {
                    Debug.Log(localizationManager.GetLanguageInfo());
                }
            }
        }
        else
        {
            EditorGUILayout.LabelField("LocalizationManager", "未找到", EditorStyles.helpBox);
            
            if (GUILayout.Button("创建LocalizationManager"))
            {
                GameObject go = new GameObject("LocalizationManager");
                go.AddComponent<LocalizationManager>();
                Selection.activeGameObject = go;
            }
        }
        
        var settingsManager = FindFirstObjectByType<SettingsManager>();
        EditorGUILayout.LabelField("SettingsManager", settingsManager != null ? "已创建" : "未找到");
        
        if (settingsManager == null && GUILayout.Button("创建SettingsManager"))
        {
            GameObject go = new GameObject("SettingsManager");
            go.AddComponent<SettingsManager>();
            Selection.activeGameObject = go;
        }
    }
    
    /// <summary>
    /// 绘制快速操作
    /// </summary>
    private void DrawQuickActions()
    {
        EditorGUILayout.LabelField("快速操作", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("切换到中文"))
        {
            SwitchLanguage(LanguageSettings.Language.ChineseSimplified);
        }

        if (GUILayout.Button("切换到日语"))
        {
            SwitchLanguage(LanguageSettings.Language.Japanese);
        }

        if (GUILayout.Button("重置语言设置"))
        {
            if (EditorUtility.DisplayDialog("确认重置",
                $"确定要将语言设置重置为默认值({LanguageSettings.DefaultLanguage})吗？",
                "确定", "取消"))
            {
                LanguageSettings.ResetLanguagePreference();
                SwitchLanguage(LanguageSettings.DefaultLanguage);
            }
        }
        
        if (GUILayout.Button("切换到英文"))
        {
            SwitchLanguage(LanguageSettings.Language.English);
        }
        
        if (GUILayout.Button("切换到日文"))
        {
            SwitchLanguage(LanguageSettings.Language.Japanese);
        }
        
        EditorGUILayout.EndHorizontal();
        
        if (GUILayout.Button("重新加载当前语言"))
        {
            var localizationManager = FindFirstObjectByType<LocalizationManager>();
            if (localizationManager != null)
            {
                localizationManager.LoadLanguage(localizationManager.CurrentLanguage);
            }
        }
        
        if (GUILayout.Button("创建多语言演示"))
        {
            CreateLocalizationDemo();
        }
    }
    
    /// <summary>
    /// 绘制文本组件转换
    /// </summary>
    private void DrawTextConversion()
    {
        EditorGUILayout.LabelField("文本组件转换", EditorStyles.boldLabel);
        
        EditorGUILayout.HelpBox("选择Text组件，然后点击按钮为其添加LocalizedText组件", MessageType.Info);
        
        if (GUILayout.Button("为选中的Text组件添加LocalizedText"))
        {
            AddLocalizedTextToSelected();
        }
        
        if (GUILayout.Button("为场景中所有Text组件添加LocalizedText"))
        {
            AddLocalizedTextToAllTexts();
        }
        
        if (GUILayout.Button("扫描场景中的中文文本"))
        {
            ScanChineseTexts();
        }
    }
    
    /// <summary>
    /// 绘制语言管理
    /// </summary>
    private void DrawLanguageManagement()
    {
        EditorGUILayout.LabelField("语言管理", EditorStyles.boldLabel);
        
        selectedLanguage = (LanguageSettings.Language)EditorGUILayout.EnumPopup("选择语言", selectedLanguage);
        
        EditorGUILayout.LabelField("添加新文本条目:");
        newKey = EditorGUILayout.TextField("键", newKey);
        newValue = EditorGUILayout.TextField("值", newValue);
        
        if (GUILayout.Button("添加文本条目"))
        {
            // TODO: 实现添加文本条目到JSON文件
            Debug.Log($"TODO: 添加 {newKey} = {newValue} 到 {selectedLanguage} 语言文件");
        }
        
        if (GUILayout.Button("打开语言文件目录"))
        {
            string path = Application.dataPath + "/Resources/Localization/Data";
            EditorUtility.RevealInFinder(path);
        }
    }
    
    /// <summary>
    /// 切换语言
    /// </summary>
    private void SwitchLanguage(LanguageSettings.Language language)
    {
        var localizationManager = FindFirstObjectByType<LocalizationManager>();
        if (localizationManager != null)
        {
            localizationManager.SwitchLanguage(language);
            Debug.Log($"切换语言到: {language}");
        }
        else
        {
            Debug.LogWarning("未找到LocalizationManager");
        }
    }
    
    /// <summary>
    /// 为选中的Text组件添加LocalizedText
    /// </summary>
    private void AddLocalizedTextToSelected()
    {
        var selectedObjects = Selection.gameObjects;
        int addedCount = 0;
        
        foreach (var obj in selectedObjects)
        {
            var textComponents = obj.GetComponentsInChildren<Text>();
            var tmpComponents = obj.GetComponentsInChildren<TextMeshProUGUI>();
            var tmpProComponents = obj.GetComponentsInChildren<TextMeshPro>();
            
            foreach (var text in textComponents)
            {
                if (text.GetComponent<LocalizedText>() == null)
                {
                    var localizedText = text.gameObject.AddComponent<LocalizedText>();
                    // 如果文本包含中文，建议一个key
                    if (ContainsChinese(text.text))
                    {
                        string suggestedKey = GenerateKeyFromText(text.text);
                        Debug.Log($"建议为 '{text.text}' 使用键: {suggestedKey}");
                    }
                    addedCount++;
                }
            }
            
            foreach (var tmp in tmpComponents)
            {
                if (tmp.GetComponent<LocalizedText>() == null)
                {
                    tmp.gameObject.AddComponent<LocalizedText>();
                    addedCount++;
                }
            }
            
            foreach (var tmpPro in tmpProComponents)
            {
                if (tmpPro.GetComponent<LocalizedText>() == null)
                {
                    tmpPro.gameObject.AddComponent<LocalizedText>();
                    addedCount++;
                }
            }
        }
        
        Debug.Log($"为 {addedCount} 个文本组件添加了LocalizedText");
    }
    
    /// <summary>
    /// 为场景中所有Text组件添加LocalizedText
    /// </summary>
    private void AddLocalizedTextToAllTexts()
    {
        var allTexts = FindObjectsOfType<Text>();
        var allTMPs = FindObjectsOfType<TextMeshProUGUI>();
        var allTMPPros = FindObjectsOfType<TextMeshPro>();
        
        int addedCount = 0;
        
        foreach (var text in allTexts)
        {
            if (text.GetComponent<LocalizedText>() == null)
            {
                text.gameObject.AddComponent<LocalizedText>();
                addedCount++;
            }
        }
        
        foreach (var tmp in allTMPs)
        {
            if (tmp.GetComponent<LocalizedText>() == null)
            {
                tmp.gameObject.AddComponent<LocalizedText>();
                addedCount++;
            }
        }
        
        foreach (var tmpPro in allTMPPros)
        {
            if (tmpPro.GetComponent<LocalizedText>() == null)
            {
                tmpPro.gameObject.AddComponent<LocalizedText>();
                addedCount++;
            }
        }
        
        Debug.Log($"为场景中 {addedCount} 个文本组件添加了LocalizedText");
    }
    
    /// <summary>
    /// 扫描场景中的中文文本
    /// </summary>
    private void ScanChineseTexts()
    {
        var allTexts = FindObjectsOfType<Text>();
        var chineseTexts = new List<string>();
        
        foreach (var text in allTexts)
        {
            if (ContainsChinese(text.text))
            {
                string info = $"GameObject: {text.gameObject.name}, Text: {text.text}";
                chineseTexts.Add(info);
            }
        }
        
        Debug.Log($"找到 {chineseTexts.Count} 个包含中文的文本组件:");
        foreach (var info in chineseTexts)
        {
            Debug.Log(info);
        }
    }
    
    /// <summary>
    /// 创建多语言演示
    /// </summary>
    private void CreateLocalizationDemo()
    {
        GameObject demoObj = new GameObject("LocalizationDemo");
        demoObj.AddComponent<LocalizationDemo>();
        Selection.activeGameObject = demoObj;
        Debug.Log("创建了多语言演示对象");
    }
    
    /// <summary>
    /// 检查文本是否包含中文
    /// </summary>
    private bool ContainsChinese(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;
        
        foreach (char c in text)
        {
            if (c >= 0x4e00 && c <= 0x9fff)
                return true;
        }
        return false;
    }
    
    /// <summary>
    /// 从文本生成建议的键
    /// </summary>
    private string GenerateKeyFromText(string text)
    {
        // 简单的键生成逻辑
        if (text.Contains("设置"))
            return "ui.settings.title";
        if (text.Contains("关闭"))
            return "ui.button.close";
        if (text.Contains("确定"))
            return "ui.button.confirm";
        if (text.Contains("取消"))
            return "ui.button.cancel";
        if (text.Contains("仓库"))
            return "warehouse.title";
        if (text.Contains("背包"))
            return "inventory.title";
        
        return "custom.text.key";
    }
}
#endif
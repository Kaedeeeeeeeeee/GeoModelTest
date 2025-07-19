using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 多语言系统演示和测试脚本
/// </summary>
public class LocalizationDemo : MonoBehaviour
{
    [Header("演示设置")]
    public bool showDemoUI = true;
    public bool enableKeyboardShortcuts = true;
    
    [Header("演示文本")]
    public Text demoText;
    public Button switchLanguageButton;
    public Text languageStatusText;
    
    [Header("测试键")]
    public string[] testKeys = {
        "ui.settings.title",
        "warehouse.title",
        "tool.drill.name",
        "sample.collection.prompt"
    };
    
    // 当前测试键索引
    private int currentTestKeyIndex = 0;
    
    #region Unity生命周期
    void Start()
    {
        if (showDemoUI)
        {
            CreateDemoUI();
        }
        
        // 订阅语言切换事件
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
        }
        
        // 初始更新
        UpdateDemoDisplay();
    }
    
    void Update()
    {
        if (enableKeyboardShortcuts)
        {
            HandleKeyboardInput();
        }
    }
    
    void OnDestroy()
    {
        // 取消订阅
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
        }
    }
    #endregion
    
    #region 演示UI
    /// <summary>
    /// 创建演示UI
    /// </summary>
    private void CreateDemoUI()
    {
        // 创建演示Canvas
        GameObject canvasObj = new GameObject("LocalizationDemoCanvas");
        canvasObj.transform.SetParent(transform);
        
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300; // 确保在最顶层
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // 创建演示面板
        CreateDemoPanel(canvasObj);
    }
    
    /// <summary>
    /// 创建演示面板
    /// </summary>
    private void CreateDemoPanel(GameObject parent)
    {
        GameObject panelObj = new GameObject("DemoPanel");
        panelObj.transform.SetParent(parent.transform);
        
        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.02f, 0.02f);
        panelRect.anchorMax = new Vector2(0.35f, 0.4f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        
        // 添加背景
        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0, 0, 0, 0.8f);
        
        // 创建标题
        CreateDemoTitle(panelObj);
        
        // 创建演示文本
        CreateDemoText(panelObj);
        
        // 创建语言状态
        CreateLanguageStatus(panelObj);
        
        // 创建切换按钮
        CreateSwitchButton(panelObj);
        
        // 创建说明文本
        CreateInstructions(panelObj);
    }
    
    /// <summary>
    /// 创建演示标题
    /// </summary>
    private void CreateDemoTitle(GameObject parent)
    {
        GameObject titleObj = new GameObject("DemoTitle");
        titleObj.transform.SetParent(parent.transform);
        
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.05f, 0.85f);
        titleRect.anchorMax = new Vector2(0.95f, 0.95f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        
        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = "多语言系统演示";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 18;
        titleText.color = Color.yellow;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.fontStyle = FontStyle.Bold;
    }
    
    /// <summary>
    /// 创建演示文本
    /// </summary>
    private void CreateDemoText(GameObject parent)
    {
        GameObject textObj = new GameObject("DemoText");
        textObj.transform.SetParent(parent.transform);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.05f, 0.5f);
        textRect.anchorMax = new Vector2(0.95f, 0.8f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        demoText = textObj.AddComponent<Text>();
        demoText.text = "演示文本";
        demoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        demoText.fontSize = 14;
        demoText.color = Color.white;
        demoText.alignment = TextAnchor.UpperLeft;
    }
    
    /// <summary>
    /// 创建语言状态
    /// </summary>
    private void CreateLanguageStatus(GameObject parent)
    {
        GameObject statusObj = new GameObject("LanguageStatus");
        statusObj.transform.SetParent(parent.transform);
        
        RectTransform statusRect = statusObj.AddComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.05f, 0.35f);
        statusRect.anchorMax = new Vector2(0.95f, 0.45f);
        statusRect.offsetMin = Vector2.zero;
        statusRect.offsetMax = Vector2.zero;
        
        languageStatusText = statusObj.AddComponent<Text>();
        languageStatusText.text = "当前语言: 中文";
        languageStatusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        languageStatusText.fontSize = 12;
        languageStatusText.color = Color.cyan;
        languageStatusText.alignment = TextAnchor.MiddleCenter;
    }
    
    /// <summary>
    /// 创建切换按钮
    /// </summary>
    private void CreateSwitchButton(GameObject parent)
    {
        GameObject buttonObj = new GameObject("SwitchButton");
        buttonObj.transform.SetParent(parent.transform);
        
        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.1f, 0.2f);
        buttonRect.anchorMax = new Vector2(0.9f, 0.3f);
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;
        
        switchLanguageButton = buttonObj.AddComponent<Button>();
        
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.6f, 0.2f, 0.8f);
        
        // 创建按钮文本
        GameObject textObj = new GameObject("ButtonText");
        textObj.transform.SetParent(buttonObj.transform);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        Text buttonText = textObj.AddComponent<Text>();
        buttonText.text = "切换语言";
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = 12;
        buttonText.color = Color.white;
        buttonText.alignment = TextAnchor.MiddleCenter;
        
        // 设置按钮事件
        switchLanguageButton.onClick.AddListener(SwitchToNextLanguage);
    }
    
    /// <summary>
    /// 创建说明文本
    /// </summary>
    private void CreateInstructions(GameObject parent)
    {
        GameObject instructObj = new GameObject("Instructions");
        instructObj.transform.SetParent(parent.transform);
        
        RectTransform instructRect = instructObj.AddComponent<RectTransform>();
        instructRect.anchorMin = new Vector2(0.05f, 0.05f);
        instructRect.anchorMax = new Vector2(0.95f, 0.15f);
        instructRect.offsetMin = Vector2.zero;
        instructRect.offsetMax = Vector2.zero;
        
        Text instructText = instructObj.AddComponent<Text>();
        instructText.text = "快捷键:\nESC - 设置\nSpace - 切换演示文本";
        instructText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        instructText.fontSize = 10;
        instructText.color = Color.gray;
        instructText.alignment = TextAnchor.UpperLeft;
    }
    #endregion
    
    #region 演示功能
    /// <summary>
    /// 切换到下一个语言
    /// </summary>
    private void SwitchToNextLanguage()
    {
        if (LocalizationManager.Instance == null)
            return;
        
        var currentLang = LocalizationManager.Instance.CurrentLanguage;
        LanguageSettings.Language nextLang;
        
        switch (currentLang)
        {
            case LanguageSettings.Language.ChineseSimplified:
                nextLang = LanguageSettings.Language.English;
                break;
            case LanguageSettings.Language.English:
                nextLang = LanguageSettings.Language.Japanese;
                break;
            case LanguageSettings.Language.Japanese:
                nextLang = LanguageSettings.Language.ChineseSimplified;
                break;
            default:
                nextLang = LanguageSettings.Language.ChineseSimplified;
                break;
        }
        
        LocalizationManager.Instance.SwitchLanguage(nextLang);
    }
    
    /// <summary>
    /// 切换演示文本
    /// </summary>
    private void SwitchDemoText()
    {
        if (testKeys.Length == 0)
            return;
        
        currentTestKeyIndex = (currentTestKeyIndex + 1) % testKeys.Length;
        UpdateDemoDisplay();
    }
    
    /// <summary>
    /// 更新演示显示
    /// </summary>
    private void UpdateDemoDisplay()
    {
        if (LocalizationManager.Instance == null || demoText == null)
            return;
        
        string currentKey = testKeys.Length > 0 ? testKeys[currentTestKeyIndex] : "ui.settings.title";
        string localizedText = LocalizationManager.Instance.GetText(currentKey);
        
        demoText.text = $"键: {currentKey}\n文本: {localizedText}\n\n点击按钮或按Space切换演示文本";
        
        // 更新语言状态
        if (languageStatusText != null)
        {
            var currentLang = LocalizationManager.Instance.CurrentLanguage;
            string langDisplayName = LanguageSettings.GetLanguageDisplayName(currentLang);
            languageStatusText.text = $"当前语言: {langDisplayName}";
        }
    }
    
    /// <summary>
    /// 语言切换回调
    /// </summary>
    private void OnLanguageChanged()
    {
        UpdateDemoDisplay();
        Debug.Log("[LocalizationDemo] 语言已切换，更新演示显示");
    }
    
    /// <summary>
    /// 处理键盘输入
    /// </summary>
    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SwitchDemoText();
        }
        
        if (Input.GetKeyDown(KeyCode.L))
        {
            SwitchToNextLanguage();
        }
    }
    #endregion
    
    #region 公共接口
    /// <summary>
    /// 显示/隐藏演示UI
    /// </summary>
    public void ToggleDemoUI()
    {
        showDemoUI = !showDemoUI;
        
        Canvas demoCanvas = GetComponentInChildren<Canvas>();
        if (demoCanvas != null)
        {
            demoCanvas.gameObject.SetActive(showDemoUI);
        }
    }
    
    /// <summary>
    /// 添加测试键
    /// </summary>
    public void AddTestKey(string key)
    {
        System.Array.Resize(ref testKeys, testKeys.Length + 1);
        testKeys[testKeys.Length - 1] = key;
    }
    #endregion
}
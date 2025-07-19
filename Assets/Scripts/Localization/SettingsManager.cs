using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// 设置管理器 - 管理ESC键触发的设置界面
/// </summary>
public class SettingsManager : MonoBehaviour
{
    #region 单例模式
    private static SettingsManager _instance;
    public static SettingsManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<SettingsManager>();
                
                if (_instance == null)
                {
                    GameObject go = new GameObject("SettingsManager");
                    _instance = go.AddComponent<SettingsManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    #endregion
    
    [Header("设置界面")]
    public GameObject settingsPanel;
    public Canvas settingsCanvas;
    
    [Header("语言设置")]
    public Button chineseButton;
    public Button englishButton;
    public Button japaneseButton;
    public Button closeButton;
    
    [Header("UI文本组件")]
    public Text titleText;
    public Text languageLabel;
    public Text chineseText;
    public Text englishText;
    public Text japaneseText;
    public Text closeText;
    
    [Header("设置")]
    public bool pauseGameWhenOpen = true;
    public bool disablePlayerControlWhenOpen = true;
    
    // 私有成员
    private bool isSettingsOpen = false;
    private FirstPersonController playerController;
    private float originalTimeScale = 1f;
    
    #region Unity生命周期
    void Awake()
    {
        // 确保单例
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        // 查找玩家控制器
        FindPlayerController();
    }
    
    void Start()
    {
        // 如果没有设置界面，创建一个
        if (settingsPanel == null)
        {
            CreateSettingsUI();
        }
        
        // 设置按钮事件
        SetupButtonEvents();
        
        // 初始隐藏设置界面
        CloseSettings();
    }
    
    void Update()
    {
        // 监听ESC键
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ToggleSettings();
        }
    }
    #endregion
    
    #region 设置界面控制
    /// <summary>
    /// 切换设置界面显示/隐藏
    /// </summary>
    public void ToggleSettings()
    {
        if (isSettingsOpen)
        {
            CloseSettings();
        }
        else
        {
            OpenSettings();
        }
    }
    
    /// <summary>
    /// 打开设置界面
    /// </summary>
    public void OpenSettings()
    {
        if (isSettingsOpen)
            return;
        
        Debug.Log("打开设置界面");
        
        // 显示设置面板
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
        
        if (settingsCanvas != null)
            settingsCanvas.gameObject.SetActive(true);
        
        // 暂停游戏
        if (pauseGameWhenOpen)
        {
            originalTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }
        
        // 禁用玩家控制
        if (disablePlayerControlWhenOpen && playerController != null)
        {
            playerController.enabled = false;
        }
        
        isSettingsOpen = true;
        
        // 设置鼠标状态
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    /// <summary>
    /// 关闭设置界面
    /// </summary>
    public void CloseSettings()
    {
        if (!isSettingsOpen && settingsPanel != null && !settingsPanel.activeInHierarchy)
            return;
        
        Debug.Log("关闭设置界面");
        
        // 隐藏设置面板
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
        
        if (settingsCanvas != null)
            settingsCanvas.gameObject.SetActive(false);
        
        // 恢复游戏
        if (pauseGameWhenOpen)
        {
            Time.timeScale = originalTimeScale;
        }
        
        // 恢复玩家控制
        if (disablePlayerControlWhenOpen && playerController != null)
        {
            playerController.enabled = true;
        }
        
        isSettingsOpen = false;
        
        // 恢复鼠标状态
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    #endregion
    
    #region UI创建
    /// <summary>
    /// 创建设置UI界面
    /// </summary>
    private void CreateSettingsUI()
    {
        Debug.Log("创建设置UI界面");
        
        // 创建Canvas
        CreateSettingsCanvas();
        
        // 创建主面板
        CreateMainPanel();
        
        // 创建UI元素
        CreateUIElements();
        
        Debug.Log("设置UI界面创建完成");
    }
    
    /// <summary>
    /// 创建设置Canvas
    /// </summary>
    private void CreateSettingsCanvas()
    {
        GameObject canvasObj = new GameObject("SettingsCanvas");
        canvasObj.transform.SetParent(transform);
        
        settingsCanvas = canvasObj.AddComponent<Canvas>();
        settingsCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        settingsCanvas.sortingOrder = 200; // 确保在最顶层
        
        // 添加CanvasScaler
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        
        // 添加GraphicRaycaster
        canvasObj.AddComponent<GraphicRaycaster>();
    }
    
    /// <summary>
    /// 创建主面板
    /// </summary>
    private void CreateMainPanel()
    {
        GameObject panelObj = new GameObject("SettingsPanel");
        panelObj.transform.SetParent(settingsCanvas.transform);
        
        // 设置RectTransform
        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        
        // 添加半透明背景
        Image backgroundImage = panelObj.AddComponent<Image>();
        backgroundImage.color = new Color(0, 0, 0, 0.7f);
        
        settingsPanel = panelObj;
    }
    
    /// <summary>
    /// 创建UI元素
    /// </summary>
    private void CreateUIElements()
    {
        // 创建内容区域
        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(settingsPanel.transform);
        
        RectTransform contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.3f, 0.3f);
        contentRect.anchorMax = new Vector2(0.7f, 0.7f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;
        
        // 添加内容背景
        Image contentBg = contentObj.AddComponent<Image>();
        contentBg.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);
        
        // 创建标题
        CreateTitle(contentObj);
        
        // 创建语言标签
        CreateLanguageLabel(contentObj);
        
        // 创建语言按钮
        CreateLanguageButtons(contentObj);
        
        // 创建关闭按钮
        CreateCloseButton(contentObj);
    }
    
    /// <summary>
    /// 创建标题
    /// </summary>
    private void CreateTitle(GameObject parent)
    {
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(parent.transform);
        
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.1f, 0.8f);
        titleRect.anchorMax = new Vector2(0.9f, 0.95f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        
        titleText = titleObj.AddComponent<Text>();
        titleText.text = "设置";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 36;
        titleText.color = Color.white;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.fontStyle = FontStyle.Bold;
        
        // 添加本地化组件
        LocalizedText localizedTitle = titleObj.AddComponent<LocalizedText>();
        localizedTitle.TextKey = "ui.settings.title";
    }
    
    /// <summary>
    /// 创建语言标签
    /// </summary>
    private void CreateLanguageLabel(GameObject parent)
    {
        GameObject labelObj = new GameObject("LanguageLabel");
        labelObj.transform.SetParent(parent.transform);
        
        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.1f, 0.6f);
        labelRect.anchorMax = new Vector2(0.9f, 0.7f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        
        languageLabel = labelObj.AddComponent<Text>();
        languageLabel.text = "语言 / Language / 言語";
        languageLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        languageLabel.fontSize = 24;
        languageLabel.color = Color.white;
        languageLabel.alignment = TextAnchor.MiddleCenter;
        
        // 添加本地化组件
        LocalizedText localizedLabel = labelObj.AddComponent<LocalizedText>();
        localizedLabel.TextKey = "ui.settings.language";
    }
    
    /// <summary>
    /// 创建语言按钮
    /// </summary>
    private void CreateLanguageButtons(GameObject parent)
    {
        // 中文按钮
        chineseButton = CreateLanguageButton(parent, "ChineseButton", "中文", 
            new Vector2(0.1f, 0.4f), new Vector2(0.35f, 0.55f));
        chineseText = chineseButton.GetComponentInChildren<Text>();
        
        // 英文按钮
        englishButton = CreateLanguageButton(parent, "EnglishButton", "English", 
            new Vector2(0.375f, 0.4f), new Vector2(0.625f, 0.55f));
        englishText = englishButton.GetComponentInChildren<Text>();
        
        // 日文按钮
        japaneseButton = CreateLanguageButton(parent, "JapaneseButton", "日本語", 
            new Vector2(0.65f, 0.4f), new Vector2(0.9f, 0.55f));
        japaneseText = japaneseButton.GetComponentInChildren<Text>();
    }
    
    /// <summary>
    /// 创建语言按钮
    /// </summary>
    private Button CreateLanguageButton(GameObject parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent.transform);
        
        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchorMin = anchorMin;
        buttonRect.anchorMax = anchorMax;
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;
        
        // 添加按钮组件
        Button button = buttonObj.AddComponent<Button>();
        
        // 添加背景图片
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
        
        // 设置按钮颜色
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
        colors.highlightedColor = new Color(0.4f, 0.4f, 0.4f, 0.9f);
        colors.pressedColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        colors.selectedColor = new Color(0.5f, 0.7f, 0.9f, 1f);
        button.colors = colors;
        
        // 创建按钮文本
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        Text buttonText = textObj.AddComponent<Text>();
        buttonText.text = text;
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = 20;
        buttonText.color = Color.white;
        buttonText.alignment = TextAnchor.MiddleCenter;
        
        return button;
    }
    
    /// <summary>
    /// 创建关闭按钮
    /// </summary>
    private void CreateCloseButton(GameObject parent)
    {
        GameObject buttonObj = new GameObject("CloseButton");
        buttonObj.transform.SetParent(parent.transform);
        
        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.35f, 0.1f);
        buttonRect.anchorMax = new Vector2(0.65f, 0.25f);
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;
        
        closeButton = buttonObj.AddComponent<Button>();
        
        // 添加背景图片
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.6f, 0.2f, 0.2f, 0.8f);
        
        // 设置按钮颜色
        ColorBlock colors = closeButton.colors;
        colors.normalColor = new Color(0.6f, 0.2f, 0.2f, 0.8f);
        colors.highlightedColor = new Color(0.7f, 0.3f, 0.3f, 0.9f);
        colors.pressedColor = new Color(0.5f, 0.1f, 0.1f, 1f);
        closeButton.colors = colors;
        
        // 创建按钮文本
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        closeText = textObj.AddComponent<Text>();
        closeText.text = "关闭";
        closeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        closeText.fontSize = 24;
        closeText.color = Color.white;
        closeText.alignment = TextAnchor.MiddleCenter;
        
        // 添加本地化组件
        LocalizedText localizedClose = textObj.AddComponent<LocalizedText>();
        localizedClose.TextKey = "ui.button.close";
    }
    #endregion
    
    #region 事件设置
    /// <summary>
    /// 设置按钮事件
    /// </summary>
    private void SetupButtonEvents()
    {
        if (chineseButton != null)
        {
            chineseButton.onClick.AddListener(() => SwitchLanguage(LanguageSettings.Language.ChineseSimplified));
        }
        
        if (englishButton != null)
        {
            englishButton.onClick.AddListener(() => SwitchLanguage(LanguageSettings.Language.English));
        }
        
        if (japaneseButton != null)
        {
            japaneseButton.onClick.AddListener(() => SwitchLanguage(LanguageSettings.Language.Japanese));
        }
        
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseSettings);
        }
    }
    
    /// <summary>
    /// 切换语言
    /// </summary>
    private void SwitchLanguage(LanguageSettings.Language language)
    {
        Debug.Log($"切换语言: {language}");
        
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.SwitchLanguage(language);
            UpdateButtonStates(language);
        }
    }
    
    /// <summary>
    /// 更新按钮状态
    /// </summary>
    private void UpdateButtonStates(LanguageSettings.Language currentLanguage)
    {
        // 重置所有按钮颜色
        ResetButtonColor(chineseButton);
        ResetButtonColor(englishButton);
        ResetButtonColor(japaneseButton);
        
        // 高亮当前语言按钮
        Button activeButton = null;
        switch (currentLanguage)
        {
            case LanguageSettings.Language.ChineseSimplified:
                activeButton = chineseButton;
                break;
            case LanguageSettings.Language.English:
                activeButton = englishButton;
                break;
            case LanguageSettings.Language.Japanese:
                activeButton = japaneseButton;
                break;
        }
        
        if (activeButton != null)
        {
            HighlightButton(activeButton);
        }
    }
    
    /// <summary>
    /// 重置按钮颜色
    /// </summary>
    private void ResetButtonColor(Button button)
    {
        if (button != null)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
            button.colors = colors;
        }
    }
    
    /// <summary>
    /// 高亮按钮
    /// </summary>
    private void HighlightButton(Button button)
    {
        if (button != null)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.5f, 0.7f, 0.9f, 1f);
            button.colors = colors;
        }
    }
    #endregion
    
    #region 工具方法
    /// <summary>
    /// 查找玩家控制器
    /// </summary>
    private void FindPlayerController()
    {
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<FirstPersonController>();
            
            if (playerController == null)
            {
                Debug.LogWarning("未找到FirstPersonController，设置界面将无法控制玩家移动");
            }
        }
    }
    
    /// <summary>
    /// 设置界面是否打开
    /// </summary>
    public bool IsSettingsOpen => isSettingsOpen;
    #endregion
}
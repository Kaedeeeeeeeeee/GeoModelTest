using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// 游戏场景管理器 - 处理多场景切换和数据持久化
/// </summary>
public class GameSceneManager : MonoBehaviour
{
    [Header("场景配置")]
    public SceneConfig[] availableScenes = {
        new SceneConfig("MainScene", "野外", "主要的地质勘探场景"),
        new SceneConfig("Laboratory Scene", "研究室", "样本分析和研究场景")
    };
    
    [Header("UI引用")]
    public GameObject sceneSelectionUI;
    public Transform sceneButtonContainer;
    public GameObject sceneButtonPrefab;
    
    private static GameSceneManager instance;
    private string currentSceneName;
    private PlayerPersistentData playerData;
    private bool isSceneLoading = false;
    
    public static GameSceneManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<GameSceneManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("GameSceneManager");
                    instance = go.AddComponent<GameSceneManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSceneManager();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    void InitializeSceneManager()
    {
        currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        playerData = GetComponent<PlayerPersistentData>();
        
        if (playerData == null)
        {
            playerData = gameObject.AddComponent<PlayerPersistentData>();
        }
        
        // 确保场景初始化器存在
        SceneInitializer.GetOrCreate();
        
        Debug.Log($"场景管理器初始化完成，当前场景: {currentSceneName}");
    }
    
    /// <summary>
    /// 显示场景选择UI
    /// </summary>
    public void ShowSceneSelectionUI()
    {
        Debug.Log("[GameSceneManager] ShowSceneSelectionUI被调用");

        if (isSceneLoading)
        {
            Debug.LogWarning("正在加载场景，请稍后");
            return;
        }

        Debug.Log("[GameSceneManager] 开始创建场景选择UI");
        CreateSceneSelectionUI();

        if (sceneSelectionUI != null)
        {
            Debug.Log("[GameSceneManager] 激活场景选择UI");
            sceneSelectionUI.SetActive(true);

            // 暂停游戏时间，显示光标
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // 禁用玩家输入
            SetPlayerInputEnabled(false);

            Debug.Log("[GameSceneManager] 场景选择UI显示完成");
        }
        else
        {
            Debug.LogError("[GameSceneManager] sceneSelectionUI为null！");
        }
    }
    
    /// <summary>
    /// 隐藏场景选择UI
    /// </summary>
    public void HideSceneSelectionUI()
    {
        if (sceneSelectionUI != null)
        {
            sceneSelectionUI.SetActive(false);
        }
        
        // 恢复游戏时间和光标
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // 恢复玩家输入
        SetPlayerInputEnabled(true);
        
        Debug.Log("隐藏场景选择UI");
    }
    
    /// <summary>
    /// 创建场景选择UI
    /// </summary>
    void CreateSceneSelectionUI()
    {
        if (sceneSelectionUI != null)
        {
            Debug.Log("[GameSceneManager] 场景选择UI已存在，跳过创建");
            return;
        }

        Debug.Log("[GameSceneManager] 开始创建新的场景选择UI");
        
        // 创建UI根对象
        GameObject uiRoot = new GameObject("SceneSelectionUI");
        Canvas canvas = uiRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767; // 最高排序优先级
        canvas.overrideSorting = true;

        var canvasScaler = uiRoot.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasScaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);
        canvasScaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.matchWidthOrHeight = 0.5f;

        uiRoot.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        Debug.Log($"[GameSceneManager] UI Canvas创建 - 排序层级: {canvas.sortingOrder}");

        // 确保EventSystem存在
        EnsureEventSystem();

        // 创建背景
        GameObject background = new GameObject("Background");
        background.transform.SetParent(uiRoot.transform);
        
        RectTransform bgRect = background.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        
        UnityEngine.UI.Image bgImage = background.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = new Color(0, 0, 0, 0.9f); // 更深的背景，更明显
        
        // 创建主面板
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(uiRoot.transform);
        
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(800, 600);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        
        UnityEngine.UI.Image panelImage = panel.AddComponent<UnityEngine.UI.Image>();
        panelImage.color = new Color(0.15f, 0.15f, 0.15f, 1.0f); // 完全不透明的面板
        
        // 创建标题
        GameObject title = new GameObject("Title");
        title.transform.SetParent(panel.transform);
        
        RectTransform titleRect = title.AddComponent<RectTransform>();
        titleRect.sizeDelta = new Vector2(700, 100);
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -80);
        
        UnityEngine.UI.Text titleText = title.AddComponent<UnityEngine.UI.Text>();
        titleText.fontSize = 48;
        titleText.color = Color.white;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        
        // 添加本地化组件
        LocalizedText localizedTitle = title.AddComponent<LocalizedText>();
        localizedTitle.TextKey = "scene.selection.title";
        
        // 创建按钮容器
        GameObject buttonContainer = new GameObject("ButtonContainer");
        buttonContainer.transform.SetParent(panel.transform);
        
        RectTransform containerRect = buttonContainer.AddComponent<RectTransform>();
        containerRect.sizeDelta = new Vector2(700, 300);
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = Vector2.zero;
        
        // 创建关闭按钮
        CreateCloseButton(panel.transform);
        
        // 创建场景按钮
        CreateSceneButtons(buttonContainer.transform);
        
        sceneSelectionUI = uiRoot;
        sceneButtonContainer = buttonContainer.transform;
        
        Debug.Log("场景选择UI创建完成");
    }
    
    /// <summary>
    /// 创建场景按钮
    /// </summary>
    void CreateSceneButtons(Transform parent)
    {
        float buttonHeight = 100f;
        float buttonSpacing = 20f;
        float startY = (availableScenes.Length - 1) * (buttonHeight + buttonSpacing) * 0.5f;
        
        for (int i = 0; i < availableScenes.Length; i++)
        {
            SceneConfig scene = availableScenes[i];
            GameObject button = CreateSceneButton(parent, scene, i);
            
            RectTransform buttonRect = button.GetComponent<RectTransform>();
            buttonRect.anchoredPosition = new Vector2(0, startY - i * (buttonHeight + buttonSpacing));
        }
    }
    
    /// <summary>
    /// 创建单个场景按钮
    /// </summary>
    GameObject CreateSceneButton(Transform parent, SceneConfig scene, int index)
    {
        GameObject button = new GameObject($"SceneButton_{scene.sceneName}");
        button.transform.SetParent(parent);
        
        RectTransform buttonRect = button.AddComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(600, 90);
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        
        // 按钮背景
        UnityEngine.UI.Image buttonImage = button.AddComponent<UnityEngine.UI.Image>();
        bool isCurrentScene = scene.sceneName == currentSceneName;
        buttonImage.color = isCurrentScene ? new Color(0.4f, 0.4f, 0.4f, 0.8f) : new Color(0.3f, 0.6f, 0.9f, 0.8f);
        
        // 按钮组件
        UnityEngine.UI.Button buttonComponent = button.AddComponent<UnityEngine.UI.Button>();
        buttonComponent.targetGraphic = buttonImage;
        buttonComponent.interactable = !isCurrentScene;
        
        // 按钮文本
        GameObject buttonText = new GameObject("Text");
        buttonText.transform.SetParent(button.transform);
        
        RectTransform textRect = buttonText.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        UnityEngine.UI.Text text = buttonText.AddComponent<UnityEngine.UI.Text>();
        text.fontSize = 32;
        text.color = isCurrentScene ? Color.gray : Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        
        // 添加本地化组件
        LocalizedText localizedText = buttonText.AddComponent<LocalizedText>();
        if (isCurrentScene)
        {
            localizedText.SetTextKey("scene.button.current", GetLocalizedSceneName(scene.sceneName));
        }
        else
        {
            localizedText.TextKey = GetSceneLocalizationKey(scene.sceneName);
        }
        
        // 添加点击事件
        int sceneIndex = index;
        buttonComponent.onClick.AddListener(() => {
            Debug.Log($"[GameSceneManager] 按钮被点击 - 场景: {availableScenes[sceneIndex].sceneName}");
            SwitchToScene(availableScenes[sceneIndex].sceneName);
        });
        
        return button;
    }
    
    /// <summary>
    /// 创建关闭按钮
    /// </summary>
    void CreateCloseButton(Transform parent)
    {
        GameObject closeButton = new GameObject("CloseButton");
        closeButton.transform.SetParent(parent);
        
        RectTransform closeRect = closeButton.AddComponent<RectTransform>();
        closeRect.sizeDelta = new Vector2(160, 60);
        closeRect.anchorMin = new Vector2(1f, 0f);
        closeRect.anchorMax = new Vector2(1f, 0f);
        closeRect.anchoredPosition = new Vector2(-100, 40);
        
        UnityEngine.UI.Image closeImage = closeButton.AddComponent<UnityEngine.UI.Image>();
        closeImage.color = new Color(0.8f, 0.3f, 0.3f, 0.8f);
        
        UnityEngine.UI.Button closeButtonComponent = closeButton.AddComponent<UnityEngine.UI.Button>();
        closeButtonComponent.targetGraphic = closeImage;
        closeButtonComponent.onClick.AddListener(HideSceneSelectionUI);
        
        GameObject closeText = new GameObject("Text");
        closeText.transform.SetParent(closeButton.transform);
        
        RectTransform closeTextRect = closeText.AddComponent<RectTransform>();
        closeTextRect.anchorMin = Vector2.zero;
        closeTextRect.anchorMax = Vector2.one;
        closeTextRect.offsetMin = Vector2.zero;
        closeTextRect.offsetMax = Vector2.zero;
        
        UnityEngine.UI.Text closeTextComponent = closeText.AddComponent<UnityEngine.UI.Text>();
        closeTextComponent.fontSize = 28;
        closeTextComponent.color = Color.white;
        closeTextComponent.alignment = TextAnchor.MiddleCenter;
        closeTextComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        
        // 添加本地化组件
        LocalizedText localizedClose = closeText.AddComponent<LocalizedText>();
        localizedClose.TextKey = "ui.button.close";
    }
    
    /// <summary>
    /// 切换到指定场景
    /// </summary>
    public void SwitchToScene(string sceneName)
    {
        if (isSceneLoading)
        {
            Debug.LogWarning("正在加载场景，请稍后");
            return;
        }
        
        if (sceneName == currentSceneName)
        {
            Debug.LogWarning("已经在当前场景中");
            HideSceneSelectionUI();
            return;
        }
        
        StartCoroutine(LoadSceneAsync(sceneName));
    }
    
    /// <summary>
    /// 异步加载场景
    /// </summary>
    IEnumerator LoadSceneAsync(string sceneName)
    {
        isSceneLoading = true;
        
        // 隐藏场景选择UI
        HideSceneSelectionUI();
        
        // 保存当前场景数据
        playerData.SaveCurrentSceneData(currentSceneName);
        
        // 显示加载界面
        ShowLoadingUI();
        
        Debug.Log($"开始加载场景: {sceneName}");
        
        // 异步加载场景
        AsyncOperation asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;
        
        // 等待加载完成
        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }
        
        // 小延迟让加载界面显示
        yield return new WaitForSeconds(0.5f);
        
        // 激活场景
        asyncLoad.allowSceneActivation = true;
        
        // 等待场景完全加载
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        
        // 更新当前场景名称
        currentSceneName = sceneName;
        
        // 恢复场景数据
        yield return StartCoroutine(RestoreSceneData(sceneName));
        
        // 隐藏加载界面
        HideLoadingUI();
        
        isSceneLoading = false;
        
        Debug.Log($"场景加载完成: {sceneName}");
    }
    
    /// <summary>
    /// 恢复场景数据
    /// </summary>
    IEnumerator RestoreSceneData(string sceneName)
    {
        yield return new WaitForSeconds(0.1f); // 等待场景初始化
        
        if (playerData != null)
        {
            playerData.RestoreSceneData(sceneName);
        }
        
        Debug.Log($"场景数据恢复完成: {sceneName}");
    }
    
    /// <summary>
    /// 显示加载界面
    /// </summary>
    void ShowLoadingUI()
    {
        // 这里可以创建或显示加载界面
        Debug.Log("显示加载界面");
    }
    
    /// <summary>
    /// 隐藏加载界面
    /// </summary>
    void HideLoadingUI()
    {
        // 这里可以隐藏加载界面
        Debug.Log("隐藏加载界面");
    }
    
    /// <summary>
    /// 设置玩家输入启用状态
    /// </summary>
    void SetPlayerInputEnabled(bool enabled)
    {
        FirstPersonController fpController = FindFirstObjectByType<FirstPersonController>();
        if (fpController != null)
        {
            fpController.enabled = enabled;
        }
        
        // 禁用/启用其他输入组件
        InventoryUISystem inventoryUI = FindFirstObjectByType<InventoryUISystem>();
        if (inventoryUI != null)
        {
            inventoryUI.enabled = enabled;
        }
    }
    
    /// <summary>
    /// 获取当前场景名称
    /// </summary>
    public string GetCurrentSceneName()
    {
        return currentSceneName;
    }
    
    /// <summary>
    /// 获取场景配置
    /// </summary>
    public SceneConfig GetSceneConfig(string sceneName)
    {
        foreach (var scene in availableScenes)
        {
            if (scene.sceneName == sceneName)
                return scene;
        }
        return null;
    }
    
    /// <summary>
    /// 获取场景本地化键
    /// </summary>
    private string GetSceneLocalizationKey(string sceneName)
    {
        return sceneName switch
        {
            "MainScene" => "scene.main.name",
            "Laboratory Scene" => "scene.laboratory.name",
            _ => "scene.unknown.name"
        };
    }
    
    /// <summary>
    /// 获取本地化的场景名称
    /// </summary>
    private string GetLocalizedSceneName(string sceneName)
    {
        string key = GetSceneLocalizationKey(sceneName);
        return LocalizationManager.Instance?.GetText(key) ?? sceneName;
    }

    /// <summary>
    /// 确保EventSystem存在
    /// </summary>
    void EnsureEventSystem()
    {
        var existingEventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (existingEventSystem == null)
        {
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("[GameSceneManager] EventSystem已创建");
        }
        else
        {
            Debug.Log("[GameSceneManager] EventSystem已存在");
        }
    }
}

/// <summary>
/// 场景配置数据
/// </summary>
[System.Serializable]
public class SceneConfig
{
    public string sceneName;        // 场景文件名
    public string displayName;      // 显示名称
    public string description;      // 场景描述
    
    public SceneConfig(string sceneName, string displayName, string description)
    {
        this.sceneName = sceneName;
        this.displayName = displayName;
        this.description = description;
    }
}
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// 背包UI系统 - 管理样本背包的用户界面
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("UI组件")]
    public Canvas inventoryCanvas;
    public GameObject inventoryPanel;
    public GridLayoutGroup samplesGrid;
    public Text capacityText;
    public Text titleText;
    public Button closeButton;
    
    [Header("样本详情面板")]
    public GameObject detailPanel;
    public Text detailTitleText;
    public Text detailInfoText;
    public Button takeOutButton;
    public Button discardButton;
    public Button closeDetailButton;
    private GameObject detailBackgroundOverlay; // 详情面板的背景覆盖层
    
    [Header("样本槽位")]
    public GameObject sampleSlotPrefab;
    public int gridColumns = 5;
    public int gridRows = 4;
    
    [Header("输入设置")]
    public KeyCode toggleKey = KeyCode.I;
    
    [Header("UI设置")]
    public Vector2 panelSize = new Vector2(1200, 1000);
    public Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
    
    // 私有成员
    private bool isInventoryOpen = false;
    private SampleInventory sampleInventory;
    private List<InventorySlot> slotComponents = new List<InventorySlot>();
    private SampleItem selectedSample;
    private FirstPersonController fpController;
    
    void Start()
    {
        InitializeComponents();
        SetupUI();
        SetupEventListeners();
        
        // 确保初始鼠标状态正确
        if (!isInventoryOpen)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
    
    void Update()
    {
        HandleInput();
    }
    
    /// <summary>
    /// 初始化组件引用
    /// </summary>
    void InitializeComponents()
    {
        // 优先使用单例实例
        if (SampleInventory.Instance != null)
        {
            sampleInventory = SampleInventory.Instance;
            Debug.Log("[InventoryUI] 使用SampleInventory单例实例");
        }
        else
        {
            sampleInventory = FindFirstObjectByType<SampleInventory>();
            if (sampleInventory == null)
            {
                Debug.LogError("[InventoryUI] 未找到SampleInventory组件！尝试延迟初始化...");
                // 延迟重试
                Invoke(nameof(RetryInitializeInventory), 1f);
            }
            else
            {
                Debug.Log("[InventoryUI] 通过FindFirstObjectByType找到SampleInventory");
            }
        }
        
        fpController = FindFirstObjectByType<FirstPersonController>();
        
        // 如果UI组件为空，自动创建
        if (inventoryCanvas == null)
        {
            CreateInventoryUI();
        }
    }
    
    /// <summary>
    /// 重试初始化背包引用
    /// </summary>
    void RetryInitializeInventory()
    {
        if (sampleInventory == null)
        {
            if (SampleInventory.Instance != null)
            {
                sampleInventory = SampleInventory.Instance;
                Debug.Log("[InventoryUI] 延迟初始化成功：使用SampleInventory单例");
                
                // 重新设置事件监听
                SetupEventListeners();
            }
            else
            {
                sampleInventory = FindFirstObjectByType<SampleInventory>();
                if (sampleInventory != null)
                {
                    Debug.Log("[InventoryUI] 延迟初始化成功：通过FindFirstObjectByType找到");
                    SetupEventListeners();
                }
                else
                {
                    Debug.LogError("[InventoryUI] 延迟初始化失败，仍未找到SampleInventory");
                }
            }
        }
    }
    
    /// <summary>
    /// 创建背包UI
    /// </summary>
    void CreateInventoryUI()
    {
        // 创建Canvas
        GameObject canvasObj = new GameObject("InventoryCanvas");
        canvasObj.transform.SetParent(transform);
        
        inventoryCanvas = canvasObj.AddComponent<Canvas>();
        inventoryCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        inventoryCanvas.sortingOrder = 200; // 确保在其他UI之上
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // 确保有EventSystem
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
        
        // 创建背景面板
        CreateInventoryPanel();
        
        // 创建详情面板
        CreateDetailPanel();
        
        // 初始时隐藏UI
        inventoryPanel.SetActive(false);
        detailPanel.SetActive(false);
        
        if (detailBackgroundOverlay != null)
        {
            detailBackgroundOverlay.SetActive(false);
        }
    }
    
    /// <summary>
    /// 创建背包面板
    /// </summary>
    void CreateInventoryPanel()
    {
        // 主面板
        GameObject panel = new GameObject("InventoryPanel");
        panel.transform.SetParent(inventoryCanvas.transform);
        
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.sizeDelta = panelSize;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = backgroundColor;
        
        inventoryPanel = panel;
        
        // 标题
        CreateTitleText();
        
        // 容量显示
        CreateCapacityText();
        
        // 关闭按钮
        CreateCloseButton();
        
        // 样本网格
        CreateSamplesGrid();
    }
    
    /// <summary>
    /// 创建标题文本
    /// </summary>
    void CreateTitleText()
    {
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(inventoryPanel.transform);
        
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.sizeDelta = new Vector2(800, 80);
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -40);
        
        titleText = titleObj.AddComponent<Text>();
        titleText.text = "sample.inventory.title"; // 临时文本，会被本地化组件替换
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 40;
        titleText.color = Color.white;
        titleText.alignment = TextAnchor.MiddleCenter;
        
        // 添加本地化组件
        LocalizedText localizedTitle = titleObj.AddComponent<LocalizedText>();
        localizedTitle.TextKey = "sample.inventory.title";
    }
    
    /// <summary>
    /// 创建容量显示文本
    /// </summary>
    void CreateCapacityText()
    {
        GameObject capacityObj = new GameObject("CapacityText");
        capacityObj.transform.SetParent(inventoryPanel.transform);
        
        RectTransform capacityRect = capacityObj.AddComponent<RectTransform>();
        capacityRect.sizeDelta = new Vector2(300, 60);
        capacityRect.anchorMin = new Vector2(0f, 1f);
        capacityRect.anchorMax = new Vector2(0f, 1f);
        capacityRect.pivot = new Vector2(0f, 1f);
        capacityRect.anchoredPosition = new Vector2(40, -120);
        
        capacityText = capacityObj.AddComponent<Text>();
        capacityText.text = "0/20";
        capacityText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        capacityText.fontSize = 32;
        capacityText.color = Color.yellow;
        capacityText.alignment = TextAnchor.MiddleLeft;
    }
    
    /// <summary>
    /// 创建关闭按钮
    /// </summary>
    void CreateCloseButton()
    {
        GameObject buttonObj = new GameObject("CloseButton");
        buttonObj.transform.SetParent(inventoryPanel.transform);
        
        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(160, 60);
        buttonRect.anchorMin = new Vector2(1f, 1f);
        buttonRect.anchorMax = new Vector2(1f, 1f);
        buttonRect.pivot = new Vector2(1f, 1f);
        buttonRect.anchoredPosition = new Vector2(-40, -40);
        
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.8f, 0.2f, 0.2f, 0.8f);
        
        closeButton = buttonObj.AddComponent<Button>();
        closeButton.targetGraphic = buttonImage;
        
        // 按钮文本
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.sizeDelta = Vector2.zero;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.anchoredPosition = Vector2.zero;
        
        Text buttonText = textObj.AddComponent<Text>();
        buttonText.text = "ui.button.close"; // 临时文本，会被本地化组件替换
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = 28;
        buttonText.color = Color.white;
        buttonText.alignment = TextAnchor.MiddleCenter;
        
        // 添加本地化组件
        LocalizedText localizedButtonText = textObj.AddComponent<LocalizedText>();
        localizedButtonText.TextKey = "ui.button.close";
    }
    
    /// <summary>
    /// 创建样本网格
    /// </summary>
    void CreateSamplesGrid()
    {
        GameObject gridObj = new GameObject("SamplesGrid");
        gridObj.transform.SetParent(inventoryPanel.transform);
        
        RectTransform gridRect = gridObj.AddComponent<RectTransform>();
        gridRect.sizeDelta = new Vector2(1000, 700);
        gridRect.anchorMin = new Vector2(0.5f, 0f);
        gridRect.anchorMax = new Vector2(0.5f, 0f);
        gridRect.pivot = new Vector2(0.5f, 0f);
        gridRect.anchoredPosition = new Vector2(0, 40);
        
        samplesGrid = gridObj.AddComponent<GridLayoutGroup>();
        samplesGrid.cellSize = new Vector2(160, 160);
        samplesGrid.spacing = new Vector2(20, 20);
        samplesGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        samplesGrid.constraintCount = gridColumns;
        samplesGrid.padding = new RectOffset(20, 20, 20, 20);
        
        // 创建槽位
        CreateSampleSlots();
    }
    
    /// <summary>
    /// 创建样本槽位
    /// </summary>
    void CreateSampleSlots()
    {
        int totalSlots = gridColumns * gridRows;
        
        for (int i = 0; i < totalSlots; i++)
        {
            GameObject slotObj = CreateSampleSlot(i);
            InventorySlot slotComponent = slotObj.GetComponent<InventorySlot>();
            if (slotComponent != null)
            {
                slotComponents.Add(slotComponent);
                slotComponent.OnSlotClicked += OnSlotClicked;
            }
        }
    }
    
    /// <summary>
    /// 创建单个样本槽位
    /// </summary>
    GameObject CreateSampleSlot(int index)
    {
        GameObject slotObj = new GameObject($"SampleSlot_{index}");
        slotObj.transform.SetParent(samplesGrid.transform);
        
        // 添加槽位组件
        InventorySlot slotComponent = slotObj.AddComponent<InventorySlot>();
        slotComponent.SetupSlot(index);
        
        return slotObj;
    }
    
    /// <summary>
    /// 创建详情面板
    /// </summary>
    void CreateDetailPanel()
    {
        // 创建背景覆盖层（用于点击外部区域关闭面板）
        CreateDetailBackgroundOverlay();
        
        GameObject panel = new GameObject("DetailPanel");
        panel.transform.SetParent(inventoryCanvas.transform);
        
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(800, 600);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = new Vector2(700, 0); // 显示在背包右侧
        
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = backgroundColor;
        
        detailPanel = panel;
        
        // 创建详情面板内容
        CreateDetailPanelContent();
    }
    
    /// <summary>
    /// 创建详情面板背景覆盖层
    /// </summary>
    void CreateDetailBackgroundOverlay()
    {
        // 创建透明背景覆盖层
        GameObject overlay = new GameObject("DetailBackgroundOverlay");
        overlay.transform.SetParent(inventoryCanvas.transform);
        
        RectTransform overlayRect = overlay.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        
        // 添加半透明背景
        Image overlayImage = overlay.AddComponent<Image>();
        overlayImage.color = new Color(0, 0, 0, 0.3f); // 半透明黑色背景
        overlayImage.raycastTarget = true;
        
        // 添加按钮组件用于检测点击
        Button overlayButton = overlay.AddComponent<Button>();
        overlayButton.image = overlayImage;
        overlayButton.onClick.AddListener(CloseDetailPanel);
        
        // 设置层级顺序：背景覆盖层在详情面板之下
        overlay.transform.SetAsFirstSibling();
        
        detailBackgroundOverlay = overlay;
        
        // 初始时隐藏覆盖层
        detailBackgroundOverlay.SetActive(false);
    }
    
    /// <summary>
    /// 创建详情面板内容
    /// </summary>
    void CreateDetailPanelContent()
    {
        // 标题
        GameObject titleObj = new GameObject("DetailTitle");
        titleObj.transform.SetParent(detailPanel.transform);
        
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.sizeDelta = new Vector2(700, 60);
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -40);
        
        detailTitleText = titleObj.AddComponent<Text>();
        detailTitleText.text = "sample.detail.title"; // 临时文本，会被本地化组件替换
        detailTitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        detailTitleText.fontSize = 36;
        detailTitleText.color = Color.white;
        detailTitleText.alignment = TextAnchor.MiddleCenter;
        
        // 添加本地化组件
        LocalizedText localizedDetailTitle = titleObj.AddComponent<LocalizedText>();
        localizedDetailTitle.TextKey = "sample.detail.title";
        
        // 详情信息
        GameObject infoObj = new GameObject("DetailInfo");
        infoObj.transform.SetParent(detailPanel.transform);
        
        RectTransform infoRect = infoObj.AddComponent<RectTransform>();
        infoRect.sizeDelta = new Vector2(700, 360);
        infoRect.anchorMin = new Vector2(0.5f, 1f);
        infoRect.anchorMax = new Vector2(0.5f, 1f);
        infoRect.pivot = new Vector2(0.5f, 1f);
        infoRect.anchoredPosition = new Vector2(0, -120);
        
        detailInfoText = infoObj.AddComponent<Text>();
        detailInfoText.text = "sample.detail.placeholder"; // 临时文本，会被本地化组件替换
        detailInfoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        detailInfoText.fontSize = 24;
        detailInfoText.color = Color.white;
        detailInfoText.alignment = TextAnchor.UpperLeft;
        
        // 添加本地化组件
        LocalizedText localizedDetailInfo = infoObj.AddComponent<LocalizedText>();
        localizedDetailInfo.TextKey = "sample.detail.placeholder";
        
        // 按钮区域
        CreateDetailButtons();
    }
    
    /// <summary>
    /// 创建详情面板按钮
    /// </summary>
    void CreateDetailButtons()
    {
        float buttonWidth = 160f;
        float buttonHeight = 60f;
        float spacing = 20f;
        
        // 拿出按钮
        takeOutButton = CreateDetailButton("sample.button.take_out", new Vector2(-90, -500), buttonWidth, buttonHeight, new Color(0.2f, 0.8f, 0.2f, 0.8f));
        
        // 丢弃按钮
        discardButton = CreateDetailButton("sample.button.discard", new Vector2(90, -500), buttonWidth, buttonHeight, new Color(0.8f, 0.2f, 0.2f, 0.8f));
        
        // 关闭按钮
        closeDetailButton = CreateDetailButton("ui.button.close", new Vector2(0, -500 - buttonHeight - spacing), buttonWidth, buttonHeight, new Color(0.5f, 0.5f, 0.5f, 0.8f));
    }
    
    /// <summary>
    /// 创建详情面板按钮
    /// </summary>
    Button CreateDetailButton(string textKey, Vector2 position, float width, float height, Color color)
    {
        GameObject buttonObj = new GameObject($"{textKey}Button");
        buttonObj.transform.SetParent(detailPanel.transform);
        
        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(width, height);
        buttonRect.anchorMin = new Vector2(0.5f, 1f);
        buttonRect.anchorMax = new Vector2(0.5f, 1f);
        buttonRect.pivot = new Vector2(0.5f, 1f);
        buttonRect.anchoredPosition = position;
        
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = color;
        
        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        
        // 按钮文本
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.sizeDelta = Vector2.zero;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.anchoredPosition = Vector2.zero;
        
        Text buttonText = textObj.AddComponent<Text>();
        buttonText.text = textKey; // 临时文本，会被本地化组件替换
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = 28;
        buttonText.color = Color.white;
        buttonText.alignment = TextAnchor.MiddleCenter;
        
        // 添加本地化组件
        LocalizedText localizedButtonText = textObj.AddComponent<LocalizedText>();
        localizedButtonText.TextKey = textKey;
        
        return button;
    }
    
    /// <summary>
    /// 设置UI
    /// </summary>
    void SetupUI()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }
        
        if (detailPanel != null)
        {
            detailPanel.SetActive(false);
        }
        
        if (detailBackgroundOverlay != null)
        {
            detailBackgroundOverlay.SetActive(false);
        }
        
        UpdateCapacityDisplay();
    }
    
    /// <summary>
    /// 设置事件监听器
    /// </summary>
    void SetupEventListeners()
    {
        // 背包事件
        if (sampleInventory != null)
        {
            sampleInventory.OnInventoryChanged += RefreshInventoryDisplay;
        }
        
        // 按钮事件
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseInventory);
        }
        
        if (takeOutButton != null)
        {
            takeOutButton.onClick.AddListener(OnTakeOutButtonClicked);
        }
        
        if (discardButton != null)
        {
            discardButton.onClick.AddListener(OnDiscardButtonClicked);
        }
        
        if (closeDetailButton != null)
        {
            closeDetailButton.onClick.AddListener(CloseDetailPanel);
        }
    }
    
    /// <summary>
    /// 处理输入
    /// </summary>
    void HandleInput()
    {
        // 检查输入系统是否正常
        if (Keyboard.current == null)
        {
            return; // 输入系统未初始化
        }
        
        if (Keyboard.current.iKey.wasPressedThisFrame)
        {
            Debug.Log("[InventoryUI] 检测到I键按下");
            ToggleInventory();
        }
        
        if (isInventoryOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Debug.Log("[InventoryUI] 检测到ESC键按下，关闭背包");
            CloseInventory();
        }
    }
    
    /// <summary>
    /// 切换背包显示
    /// </summary>
    public void ToggleInventory()
    {
        Debug.Log($"[InventoryUI] ToggleInventory调用，当前状态: {(isInventoryOpen ? "打开" : "关闭")}");
        
        if (isInventoryOpen)
        {
            CloseInventory();
        }
        else
        {
            OpenInventory();
        }
    }
    
    /// <summary>
    /// 打开背包
    /// </summary>
    public void OpenInventory()
    {
        Debug.Log("[InventoryUI] 开始打开背包");
        
        if (sampleInventory == null)
        {
            Debug.LogError("[InventoryUI] 无法打开背包：sampleInventory为null");
            // 尝试重新获取
            if (SampleInventory.Instance != null)
            {
                sampleInventory = SampleInventory.Instance;
                Debug.Log("[InventoryUI] 重新获取到SampleInventory实例");
            }
            else
            {
                Debug.LogError("[InventoryUI] SampleInventory.Instance仍然为null");
                return;
            }
        }
        
        isInventoryOpen = true;
        
        if (inventoryPanel == null)
        {
            Debug.LogError("[InventoryUI] inventoryPanel为null，尝试重新创建UI");
            CreateInventoryUI();
        }
        
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(true);
            Debug.Log("[InventoryUI] 背包面板已激活");
        }
        
        // 禁用鼠标视角控制
        if (fpController != null)
        {
            fpController.enableMouseLook = false;
            Debug.Log("[InventoryUI] 已禁用鼠标视角控制");
        }
        
        // 激活鼠标指针
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Debug.Log("[InventoryUI] 已激活鼠标指针");
        
        RefreshInventoryDisplay();
        Debug.Log("[InventoryUI] 背包打开完成");
    }
    
    /// <summary>
    /// 关闭背包
    /// </summary>
    public void CloseInventory()
    {
        isInventoryOpen = false;
        inventoryPanel.SetActive(false);
        CloseDetailPanel();
        
        // 恢复鼠标视角控制
        if (fpController != null)
        {
            fpController.enableMouseLook = true;
        }
        
        // 恢复鼠标状态
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    /// <summary>
    /// 关闭详情面板
    /// </summary>
    void CloseDetailPanel()
    {
        // 隐藏详情面板
        detailPanel.SetActive(false);
        
        // 隐藏背景覆盖层
        if (detailBackgroundOverlay != null)
        {
            detailBackgroundOverlay.SetActive(false);
        }
        
        selectedSample = null;
    }
    
    /// <summary>
    /// 刷新背包显示
    /// </summary>
    void RefreshInventoryDisplay()
    {
        Debug.Log("[InventoryUI] 开始刷新背包显示");
        
        if (sampleInventory == null)
        {
            Debug.LogError("[InventoryUI] 无法刷新显示：sampleInventory为null");
            return;
        }
        
        var inventorySamples = sampleInventory.GetInventorySamples();
        Debug.Log($"[InventoryUI] 获取到 {inventorySamples.Count} 个背包中的样本");
        
        if (slotComponents == null || slotComponents.Count == 0)
        {
            Debug.LogError("[InventoryUI] slotComponents为空，UI可能未正确初始化");
            return;
        }
        
        Debug.Log($"[InventoryUI] 槽位数量: {slotComponents.Count}");
        
        // 更新槽位
        for (int i = 0; i < slotComponents.Count; i++)
        {
            if (i < inventorySamples.Count)
            {
                slotComponents[i].SetSample(inventorySamples[i]);
                Debug.Log($"[InventoryUI] 槽位[{i}]设置样本: {inventorySamples[i].displayName}");
            }
            else
            {
                slotComponents[i].SetSample(null);
            }
        }
        
        UpdateCapacityDisplay();
        Debug.Log("[InventoryUI] 背包显示刷新完成");
    }
    
    /// <summary>
    /// 更新容量显示
    /// </summary>
    void UpdateCapacityDisplay()
    {
        if (sampleInventory != null && capacityText != null)
        {
            var (current, max) = sampleInventory.GetCapacityInfo();
            capacityText.text = $"{current}/{max}";
        }
    }
    
    /// <summary>
    /// 槽位点击事件
    /// </summary>
    void OnSlotClicked(InventorySlot slot, SampleItem sample)
    {
        if (sample != null)
        {
            selectedSample = sample;
            ShowSampleDetail(sample);
        }
    }
    
    /// <summary>
    /// 显示样本详情
    /// </summary>
    void ShowSampleDetail(SampleItem sample)
    {
        // 设置选中的样本（重要：支持仓库调用）
        selectedSample = sample;
        
        // 显示背景覆盖层
        if (detailBackgroundOverlay != null)
        {
            detailBackgroundOverlay.SetActive(true);
        }
        
        // 显示详情面板
        detailPanel.SetActive(true);
        detailTitleText.text = sample.displayName;
        detailInfoText.text = sample.GetDetailedInfo();
        
        // 确保详情面板在覆盖层之上
        detailPanel.transform.SetAsLastSibling();
    }
    
    /// <summary>
    /// 拿出按钮点击事件
    /// </summary>
    void OnTakeOutButtonClicked()
    {
        if (selectedSample != null)
        {
            try
            {
                var placer = FindFirstObjectByType<SamplePlacer>();
                if (placer != null)
                {
                    string sampleName = selectedSample.displayName ?? "未知样本";
                    Debug.Log($"开始放置样本: {sampleName}");
                    
                    placer.StartPlacingMode(selectedSample);
                    CloseInventory();
                }
                else
                {
                    Debug.LogWarning("未找到SamplePlacer组件，无法拿出样本");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"拿出样本时发生错误: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("无法拿出样本：selectedSample 为 null");
        }
    }
    
    /// <summary>
    /// 丢弃按钮点击事件
    /// </summary>
    void OnDiscardButtonClicked()
    {
        if (selectedSample == null)
        {
            Debug.LogWarning("无法丢弃样本：selectedSample 为 null");
            return;
        }
        
        // 保存样本引用和信息，避免在事件处理中被清空
        SampleItem sampleToRemove = selectedSample;
        
        // 额外的null检查
        if (sampleToRemove == null)
        {
            Debug.LogWarning("无法丢弃样本：sampleToRemove 为 null");
            selectedSample = null;
            CloseDetailPanel();
            return;
        }
        
        string sampleName = sampleToRemove.displayName ?? "未知样本";
        string sampleID = sampleToRemove.sampleID ?? "无ID";
        
        try
        {
            // 先清理选中状态和关闭详情面板
            selectedSample = null;
            CloseDetailPanel();
            
            // 根据样本位置决定从哪里移除
            bool removed = false;
            if (sampleToRemove.currentLocation == SampleLocation.InInventory)
            {
                // 从背包中移除
                if (sampleInventory == null)
                {
                    Debug.LogWarning("无法丢弃样本：sampleInventory 为 null");
                    return;
                }
                removed = sampleInventory.RemoveSample(sampleToRemove);
                Debug.Log($"从背包丢弃样本: {sampleName} (ID: {sampleID}) - 结果: {removed}");
            }
            else if (sampleToRemove.currentLocation == SampleLocation.InWarehouse)
            {
                // 从仓库中移除
                var warehouseManager = WarehouseManager.Instance;
                if (warehouseManager?.Storage == null)
                {
                    Debug.LogWarning("无法丢弃样本：仓库系统为 null");
                    return;
                }
                removed = warehouseManager.Storage.RemoveItem(sampleToRemove);
                Debug.Log($"从仓库丢弃样本: {sampleName} (ID: {sampleID}) - 结果: {removed}");
            }
            else
            {
                Debug.LogWarning($"样本位置未知，无法丢弃: {sampleToRemove.currentLocation}");
                return;
            }
            
            if (removed)
            {
                Debug.Log($"已丢弃样本: {sampleName} (ID: {sampleID})");
            }
            else
            {
                Debug.LogWarning($"样本移除失败: {sampleName} (ID: {sampleID})");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"丢弃样本时发生错误: {e.Message}");
            Debug.LogError($"样本信息: {sampleName} (ID: {sampleID})");
            Debug.LogError($"堆栈跟踪: {e.StackTrace}");
        }
    }
    
    void OnDestroy()
    {
        // 移除事件监听
        if (sampleInventory != null)
        {
            sampleInventory.OnInventoryChanged -= RefreshInventoryDisplay;
        }
    }
}
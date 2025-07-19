using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// 仓库UI主界面控制器 - 管理仓库系统的用户界面
/// </summary>
public class WarehouseUI : MonoBehaviour
{
    [Header("UI引用")]
    public GameObject warehousePanel;
    public Transform leftPanel; // 背包面板
    public Transform rightPanel; // 仓库面板
    public Canvas warehouseCanvas;
    
    [Header("控制按钮")]
    public Button closeButton;
    public Button multiSelectButton;
    public Button batchTransferButton;
    public Button batchDiscardButton;
    public Text multiSelectButtonText;
    public Text batchTransferButtonText;
    public Text batchDiscardButtonText;
    
    [Header("面板组件")]
    public WarehouseInventoryPanel inventoryPanel;
    public WarehouseStoragePanel storagePanel;
    
    [Header("多选系统")]
    public MultiSelectSystem multiSelectSystem;
    
    [Header("确认对话框")]
    public GameObject confirmDialogPanel;
    public Text confirmDialogText;
    public Button confirmYesButton;
    public Button confirmNoButton;
    
    [Header("设置")]
    public Color normalButtonColor = Color.white;
    public Color multiSelectActiveColor = Color.yellow;
    public Color batchTransferActiveColor = Color.green;
    public Color batchDiscardActiveColor = Color.red;
    
    // 私有成员
    private bool isWarehouseOpen = false;
    private FirstPersonController fpController;
    
    // 单例模式
    public static WarehouseUI Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        InitializeWarehouseUI();
        SetupUIComponents();
        SetupEventListeners();
        
        // 订阅仓库数据加载事件
        SubscribeToWarehouseEvents();
        
        // 初始隐藏 - 强制隐藏UI
        ForceHideWarehouseInterface();
    }
    
    void Update()
    {
        HandleInput();
    }
    
    /// <summary>
    /// 初始化仓库UI
    /// </summary>
    void InitializeWarehouseUI()
    {
        fpController = FindFirstObjectByType<FirstPersonController>();
        
        if (warehousePanel == null)
        {
            CreateWarehouseUI();
        }
        
        Debug.Log("仓库UI初始化完成");
    }
    
    /// <summary>
    /// 创建仓库UI界面
    /// </summary>
    void CreateWarehouseUI()
    {
        // 创建主Canvas
        GameObject canvasObj = new GameObject("WarehouseCanvas");
        warehouseCanvas = canvasObj.AddComponent<Canvas>();
        warehouseCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        warehouseCanvas.sortingOrder = 150; // 低于样本详情UI，但高于TabUI
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // 创建主面板
        CreateMainPanel(canvasObj);
        
        // 创建左右分栏
        CreatePanels();
        
        // 创建控制按钮
        CreateControlButtons();
        
        // 创建确认对话框
        CreateConfirmDialog();
        
        Debug.Log("仓库UI界面创建完成");
    }
    
    /// <summary>
    /// 创建主面板
    /// </summary>
    void CreateMainPanel(GameObject canvasObj)
    {
        warehousePanel = new GameObject("WarehousePanel");
        warehousePanel.transform.SetParent(canvasObj.transform);
        
        RectTransform panelRect = warehousePanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        
        // 添加轻微的半透明背景（用于ESC键检测等）
        Image background = warehousePanel.AddComponent<Image>();
        background.color = new Color(0, 0, 0, 0.3f); // 更透明的背景
        
        // 创建内容区域
        GameObject contentArea = new GameObject("ContentArea");
        contentArea.transform.SetParent(warehousePanel.transform);
        
        RectTransform contentRect = contentArea.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.05f, 0.05f);
        contentRect.anchorMax = new Vector2(0.95f, 0.95f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;
        
        // 不添加内容区域背景，让左右面板自己处理背景
        
        // 设置contentArea为父对象
        warehousePanel = contentArea;
    }
    
    /// <summary>
    /// 创建左右面板
    /// </summary>
    void CreatePanels()
    {
        // 创建左面板（背包）
        GameObject leftPanelObj = new GameObject("BackpackPanel");
        leftPanelObj.transform.SetParent(warehousePanel.transform);
        
        RectTransform leftRect = leftPanelObj.AddComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0.02f, 0.15f);
        leftRect.anchorMax = new Vector2(0.35f, 0.95f); // 缩小左面板宽度
        leftRect.offsetMin = Vector2.zero;
        leftRect.offsetMax = Vector2.zero;
        
        leftPanel = leftPanelObj.transform;
        
        // 创建右面板（仓库）
        GameObject rightPanelObj = new GameObject("WarehousePanel");
        rightPanelObj.transform.SetParent(warehousePanel.transform);
        
        RectTransform rightRect = rightPanelObj.AddComponent<RectTransform>();
        rightRect.anchorMin = new Vector2(0.37f, 0.15f); // 扩大右面板宽度
        rightRect.anchorMax = new Vector2(0.98f, 0.95f);
        rightRect.offsetMin = Vector2.zero;
        rightRect.offsetMax = Vector2.zero;
        
        rightPanel = rightPanelObj.transform;
        
        // 添加面板背景 - 使用更明显的区分
        Image leftBg = leftPanelObj.AddComponent<Image>();
        leftBg.color = new Color(0.2f, 0.3f, 0.4f, 0.9f); // 偏蓝色背景（背包）
        
        Image rightBg = rightPanelObj.AddComponent<Image>();
        rightBg.color = new Color(0.3f, 0.4f, 0.2f, 0.9f); // 偏绿色背景（仓库）
        
        // 添加边框
        AddPanelBorder(leftPanelObj, new Color(0.4f, 0.6f, 0.8f, 1f)); // 蓝色边框
        AddPanelBorder(rightPanelObj, new Color(0.6f, 0.8f, 0.4f, 1f)); // 绿色边框
        
        // 添加标题
        CreatePanelTitle(leftPanelObj, "warehouse.inventory.title");
        CreatePanelTitle(rightPanelObj, "warehouse.storage.title");
    }
    
    /// <summary>
    /// 添加面板边框
    /// </summary>
    void AddPanelBorder(GameObject panel, Color borderColor)
    {
        GameObject borderObj = new GameObject("Border");
        borderObj.transform.SetParent(panel.transform);
        
        RectTransform borderRect = borderObj.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = Vector2.zero;
        borderRect.offsetMax = Vector2.zero;
        
        Image borderImage = borderObj.AddComponent<Image>();
        borderImage.color = borderColor;
        borderImage.sprite = null;
        
        // 创建一个边框效果（通过Outline组件）
        Outline outline = borderObj.AddComponent<Outline>();
        outline.effectColor = borderColor;
        outline.effectDistance = new Vector2(2, 2);
        
        // 将边框置于底层
        borderObj.transform.SetAsFirstSibling();
    }
    
    /// <summary>
    /// 创建面板标题
    /// </summary>
    void CreatePanelTitle(GameObject panel, string titleKey)
    {
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panel.transform);
        
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.9f);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        
        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = titleKey; // 临时文本，会被本地化组件替换
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 24;
        titleText.color = Color.white;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.fontStyle = FontStyle.Bold;
        
        // 添加本地化组件
        LocalizedText localizedTitle = titleObj.AddComponent<LocalizedText>();
        localizedTitle.TextKey = titleKey;
    }
    
    /// <summary>
    /// 创建控制按钮
    /// </summary>
    void CreateControlButtons()
    {
        // 创建按钮容器
        GameObject buttonContainer = new GameObject("ButtonContainer");
        buttonContainer.transform.SetParent(warehousePanel.transform);
        
        RectTransform containerRect = buttonContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0, 0.02f);
        containerRect.anchorMax = new Vector2(1, 0.12f);
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;
        
        // 关闭按钮
        closeButton = CreateButton(buttonContainer, "ui.button.close", new Vector2(0.85f, 0.5f), new Vector2(120, 50));
        closeButton.onClick.AddListener(CloseWarehouseInterface);
        
        // 多选按钮
        multiSelectButton = CreateButton(buttonContainer, "warehouse.button.multi_select", new Vector2(0.15f, 0.5f), new Vector2(120, 50));
        multiSelectButtonText = multiSelectButton.GetComponentInChildren<Text>();
        
        // 批量传输按钮（初始隐藏）
        batchTransferButton = CreateButton(buttonContainer, "warehouse.button.batch_transfer", new Vector2(0.35f, 0.5f), new Vector2(150, 50));
        batchTransferButtonText = batchTransferButton.GetComponentInChildren<Text>();
        batchTransferButton.gameObject.SetActive(false);
        
        // 批量丢弃按钮（初始隐藏）
        batchDiscardButton = CreateButton(buttonContainer, "warehouse.button.batch_discard", new Vector2(0.5f, 0.5f), new Vector2(150, 50));
        batchDiscardButtonText = batchDiscardButton.GetComponentInChildren<Text>();
        batchDiscardButton.gameObject.SetActive(false);
    }
    
    /// <summary>
    /// 创建按钮
    /// </summary>
    Button CreateButton(GameObject parent, string textKey, Vector2 anchorPosition, Vector2 size)
    {
        GameObject buttonObj = new GameObject($"Button_{textKey}");
        buttonObj.transform.SetParent(parent.transform);
        
        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchorMin = anchorPosition;
        buttonRect.anchorMax = anchorPosition;
        buttonRect.anchoredPosition = Vector2.zero;
        buttonRect.sizeDelta = size;
        
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = normalButtonColor;
        
        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        
        // 添加文本
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        Text buttonText = textObj.AddComponent<Text>();
        buttonText.text = textKey; // 临时文本，会被本地化组件替换
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = 18;
        buttonText.color = Color.black;
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.fontStyle = FontStyle.Bold;
        
        // 添加本地化组件
        LocalizedText localizedText = textObj.AddComponent<LocalizedText>();
        localizedText.TextKey = textKey;
        
        return button;
    }
    
    /// <summary>
    /// 创建确认对话框
    /// </summary>
    void CreateConfirmDialog()
    {
        // 创建对话框容器
        confirmDialogPanel = new GameObject("ConfirmDialogPanel");
        confirmDialogPanel.transform.SetParent(warehouseCanvas.transform);
        
        RectTransform dialogRect = confirmDialogPanel.AddComponent<RectTransform>();
        dialogRect.anchorMin = Vector2.zero;
        dialogRect.anchorMax = Vector2.one;
        dialogRect.offsetMin = Vector2.zero;
        dialogRect.offsetMax = Vector2.zero;
        
        // 添加半透明背景
        Image dialogBackground = confirmDialogPanel.AddComponent<Image>();
        dialogBackground.color = new Color(0, 0, 0, 0.7f);
        
        // 创建对话框面板
        GameObject dialogContent = new GameObject("DialogContent");
        dialogContent.transform.SetParent(confirmDialogPanel.transform);
        
        RectTransform contentRect = dialogContent.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(500, 300);
        
        // 对话框背景
        Image contentBackground = dialogContent.AddComponent<Image>();
        contentBackground.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);
        
        // 添加边框
        Outline contentOutline = dialogContent.AddComponent<Outline>();
        contentOutline.effectColor = Color.white;
        contentOutline.effectDistance = new Vector2(2, 2);
        
        // 创建对话框标题
        GameObject titleObj = new GameObject("DialogTitle");
        titleObj.transform.SetParent(dialogContent.transform);
        
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.7f);
        titleRect.anchorMax = new Vector2(1, 0.95f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        
        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = "确认丢弃";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 24;
        titleText.color = Color.white;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.fontStyle = FontStyle.Bold;
        
        // 创建对话框内容文本
        GameObject textObj = new GameObject("DialogText");
        textObj.transform.SetParent(dialogContent.transform);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.1f, 0.4f);
        textRect.anchorMax = new Vector2(0.9f, 0.7f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        confirmDialogText = textObj.AddComponent<Text>();
        confirmDialogText.text = "确定要丢弃所有选中的样本吗？\n此操作不可撤销！";
        confirmDialogText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        confirmDialogText.fontSize = 18;
        confirmDialogText.color = Color.white;
        confirmDialogText.alignment = TextAnchor.MiddleCenter;
        
        // 创建确认按钮
        confirmYesButton = CreateDialogButton(dialogContent, "确定", new Vector2(0.3f, 0.15f), new Vector2(120, 50), Color.red);
        confirmYesButton.onClick.AddListener(OnConfirmDiscard);
        
        // 创建取消按钮
        confirmNoButton = CreateDialogButton(dialogContent, "取消", new Vector2(0.7f, 0.15f), new Vector2(120, 50), Color.gray);
        confirmNoButton.onClick.AddListener(OnCancelDiscard);
        
        // 初始隐藏对话框
        confirmDialogPanel.SetActive(false);
    }
    
    /// <summary>
    /// 创建对话框按钮
    /// </summary>
    Button CreateDialogButton(GameObject parent, string text, Vector2 anchorPosition, Vector2 size, Color color)
    {
        GameObject buttonObj = new GameObject($"DialogButton_{text}");
        buttonObj.transform.SetParent(parent.transform);
        
        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchorMin = anchorPosition;
        buttonRect.anchorMax = anchorPosition;
        buttonRect.anchoredPosition = Vector2.zero;
        buttonRect.sizeDelta = size;
        
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = color;
        
        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        
        // 添加文本
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
        buttonText.fontSize = 16;
        buttonText.color = Color.white;
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.fontStyle = FontStyle.Bold;
        
        return button;
    }
    
    /// <summary>
    /// 设置UI组件
    /// </summary>
    void SetupUIComponents()
    {
        // 先初始化多选系统
        if (multiSelectSystem == null)
        {
            GameObject multiSelectObj = new GameObject("MultiSelectSystem");
            multiSelectObj.transform.SetParent(transform);
            multiSelectSystem = multiSelectObj.AddComponent<MultiSelectSystem>();
            Debug.Log("创建MultiSelectSystem组件");
            
            // 强制设置为全局引用，确保面板能找到
            multiSelectSystem.gameObject.name = "WarehouseMultiSelectSystem";
        }
        
        // 初始化面板组件
        if (inventoryPanel == null)
        {
            GameObject invPanelObj = new GameObject("WarehouseInventoryPanel");
            invPanelObj.transform.SetParent(leftPanel);
            
            // 设置面板的RectTransform以填充整个左面板
            RectTransform invRect = invPanelObj.AddComponent<RectTransform>();
            invRect.anchorMin = Vector2.zero;
            invRect.anchorMax = Vector2.one;
            invRect.offsetMin = Vector2.zero;
            invRect.offsetMax = Vector2.zero;
            
            inventoryPanel = invPanelObj.AddComponent<WarehouseInventoryPanel>();
            Debug.Log("创建WarehouseInventoryPanel组件");
            
            // 手动初始化面板（因为动态创建的组件Start方法可能不会立即调用）
            StartCoroutine(InitializePanelAfterFrame(inventoryPanel));
        }
        
        if (storagePanel == null)
        {
            GameObject storagePanelObj = new GameObject("WarehouseStoragePanel");
            storagePanelObj.transform.SetParent(rightPanel);
            
            // 设置面板的RectTransform以填充整个右面板
            RectTransform storageRect = storagePanelObj.AddComponent<RectTransform>();
            storageRect.anchorMin = Vector2.zero;
            storageRect.anchorMax = Vector2.one;
            storageRect.offsetMin = Vector2.zero;
            storageRect.offsetMax = Vector2.zero;
            
            storagePanel = storagePanelObj.AddComponent<WarehouseStoragePanel>();
            Debug.Log("创建WarehouseStoragePanel组件");
            
            // 手动初始化面板（因为动态创建的组件Start方法可能不会立即调用）
            StartCoroutine(InitializePanelAfterFrame(storagePanel));
        }
    }
    
    /// <summary>
    /// 设置事件监听
    /// </summary>
    void SetupEventListeners()
    {
        if (multiSelectButton != null)
        {
            multiSelectButton.onClick.AddListener(ToggleMultiSelectMode);
        }
        
        if (batchTransferButton != null)
        {
            batchTransferButton.onClick.AddListener(ExecuteBatchTransfer);
        }
        
        if (batchDiscardButton != null)
        {
            batchDiscardButton.onClick.AddListener(ExecuteBatchDiscard);
        }
        
        // 订阅多选系统事件
        if (multiSelectSystem != null)
        {
            multiSelectSystem.OnSelectionModeChanged += OnSelectionModeChanged;
            multiSelectSystem.OnSelectionChanged += OnSelectionChanged;
        }
    }
    
    /// <summary>
    /// 订阅仓库数据事件
    /// </summary>
    void SubscribeToWarehouseEvents()
    {
        // 订阅仓库数据加载事件
        if (WarehouseManager.Instance != null)
        {
            WarehouseManager.Instance.OnWarehouseDataLoaded += OnWarehouseDataLoaded;
            Debug.Log("[WarehouseUI] 已订阅仓库数据加载事件");
        }
        else
        {
            // 如果仓库管理器还没初始化，延迟订阅
            StartCoroutine(DelayedSubscribeToWarehouseEvents());
        }
    }
    
    /// <summary>
    /// 延迟订阅仓库事件
    /// </summary>
    System.Collections.IEnumerator DelayedSubscribeToWarehouseEvents()
    {
        // 等待仓库管理器初始化
        while (WarehouseManager.Instance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        WarehouseManager.Instance.OnWarehouseDataLoaded += OnWarehouseDataLoaded;
        Debug.Log("[WarehouseUI] 延迟订阅仓库数据加载事件成功");
    }
    
    /// <summary>
    /// 仓库数据加载完成回调
    /// </summary>
    void OnWarehouseDataLoaded()
    {
        Debug.Log("[WarehouseUI] 仓库数据加载完成，刷新界面");
        
        // 如果仓库界面当前是打开的，则立即刷新
        if (isWarehouseOpen)
        {
            RefreshPanels();
        }
    }
    
    /// <summary>
    /// 打开仓库界面
    /// </summary>
    public void OpenWarehouseInterface()
    {
        if (isWarehouseOpen) return;
        
        isWarehouseOpen = true;
        
        // 显示整个Canvas
        if (warehouseCanvas != null)
        {
            warehouseCanvas.gameObject.SetActive(true);
        }
        
        // 显示UI面板
        if (warehousePanel != null)
        {
            warehousePanel.SetActive(true);
        }
        
        // 禁用玩家控制
        DisablePlayerControls();
        
        // 刷新面板内容 - 使用协程确保在界面完全打开后再刷新
        StartCoroutine(RefreshPanelsAfterOpen());
        
        Debug.Log("仓库界面已打开");
    }
    
    /// <summary>
    /// 关闭仓库界面
    /// </summary>
    public void CloseWarehouseInterface()
    {
        // 强制隐藏整个Canvas（无论当前状态如何）
        if (warehouseCanvas != null)
        {
            warehouseCanvas.gameObject.SetActive(false);
        }
        
        // 强制隐藏UI面板（双重保险）
        if (warehousePanel != null)
        {
            warehousePanel.SetActive(false);
        }
        
        // 如果仓库已经是关闭状态，就不需要执行其他操作
        if (!isWarehouseOpen) return;
        
        isWarehouseOpen = false;
        
        // 退出多选模式
        if (multiSelectSystem != null && multiSelectSystem.IsInMultiSelectMode())
        {
            multiSelectSystem.ExitMultiSelectMode();
        }
        
        // 关闭确认对话框
        HideConfirmDialog();
        
        // 恢复玩家控制
        EnablePlayerControls();
        
        Debug.Log("仓库界面已关闭");
    }
    
    /// <summary>
    /// 强制隐藏仓库界面（用于初始化）
    /// </summary>
    public void ForceHideWarehouseInterface()
    {
        isWarehouseOpen = false;
        
        // 强制隐藏整个Canvas
        if (warehouseCanvas != null)
        {
            warehouseCanvas.gameObject.SetActive(false);
        }
        
        // 也隐藏UI面板（双重保险）
        if (warehousePanel != null)
        {
            warehousePanel.SetActive(false);
        }
        
        // 退出多选模式
        if (multiSelectSystem != null && multiSelectSystem.IsInMultiSelectMode())
        {
            multiSelectSystem.ExitMultiSelectMode();
        }
        
        // 恢复玩家控制
        EnablePlayerControls();
        
        Debug.Log("仓库界面已强制隐藏");
    }
    
    /// <summary>
    /// 刷新面板内容
    /// </summary>
    void RefreshPanels()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.RefreshInventoryDisplay();
        }
        
        if (storagePanel != null)
        {
            storagePanel.RefreshStorageDisplay();
        }
    }
    
    /// <summary>
    /// 切换多选模式
    /// </summary>
    void ToggleMultiSelectMode()
    {
        if (multiSelectSystem == null) return;
        
        if (multiSelectSystem.IsInMultiSelectMode())
        {
            multiSelectSystem.ExitMultiSelectMode();
        }
        else
        {
            multiSelectSystem.EnterMultiSelectMode();
        }
    }
    
    /// <summary>
    /// 执行批量传输
    /// </summary>
    void ExecuteBatchTransfer()
    {
        if (multiSelectSystem == null || !multiSelectSystem.IsInMultiSelectMode())
        {
            return;
        }
        
        var selectedItems = multiSelectSystem.GetSelectedItems();
        if (selectedItems.Count == 0)
        {
            Debug.LogWarning("没有选中的物品");
            return;
        }
        
        bool success = false;
        var selectionMode = multiSelectSystem.GetCurrentSelectionMode();
        
        if (selectionMode == SelectionMode.BackpackSelection)
        {
            // 从背包移动到仓库
            success = WarehouseManager.Instance.BatchMoveFromInventoryToWarehouse(selectedItems);
        }
        else if (selectionMode == SelectionMode.WarehouseSelection)
        {
            // 从仓库移动到背包
            success = WarehouseManager.Instance.BatchMoveFromWarehouseToInventory(selectedItems);
        }
        
        if (success)
        {
            // 1. 先清空选择并退出多选模式（确保多选系统状态正确）
            multiSelectSystem.ExitMultiSelectMode();
            
            // 2. 强制清空所有槽位的选中状态
            ClearAllSlotSelections();
            
            // 3. 刷新界面
            RefreshPanels();
            
            // 4. 再次强制清空槽位选中状态（确保完全清空）
            StartCoroutine(DelayedClearSelections());
            
            Debug.Log($"批量传输成功：{selectedItems.Count} 个物品");
        }
        else
        {
            Debug.LogWarning("批量传输失败");
        }
    }
    
    /// <summary>
    /// 执行批量丢弃
    /// </summary>
    void ExecuteBatchDiscard()
    {
        if (multiSelectSystem == null || !multiSelectSystem.IsInMultiSelectMode())
        {
            return;
        }
        
        var selectedItems = multiSelectSystem.GetSelectedItems();
        if (selectedItems.Count == 0)
        {
            Debug.LogWarning("没有选中的物品可以丢弃");
            return;
        }
        
        // 检查是否都是仓库中的物品
        var warehouseItems = selectedItems.FindAll(item => item.currentLocation == SampleLocation.InWarehouse);
        if (warehouseItems.Count == 0)
        {
            Debug.LogWarning("没有选中仓库中的物品");
            return;
        }
        
        // 更新确认对话框文本
        if (confirmDialogText != null)
        {
            confirmDialogText.text = $"确定要丢弃选中的 {warehouseItems.Count} 个样本吗？\n此操作不可撤销！";
        }
        
        // 显示确认对话框
        if (confirmDialogPanel != null)
        {
            confirmDialogPanel.SetActive(true);
            
            // 确保对话框在最顶层
            confirmDialogPanel.transform.SetAsLastSibling();
        }
        
        Debug.Log($"准备丢弃 {warehouseItems.Count} 个仓库物品");
    }
    
    /// <summary>
    /// 确认丢弃
    /// </summary>
    void OnConfirmDiscard()
    {
        if (multiSelectSystem == null || !multiSelectSystem.IsInMultiSelectMode())
        {
            HideConfirmDialog();
            return;
        }
        
        var selectedItems = multiSelectSystem.GetSelectedItems();
        var warehouseItems = selectedItems.FindAll(item => item.currentLocation == SampleLocation.InWarehouse);
        
        if (warehouseItems.Count == 0)
        {
            Debug.LogWarning("没有可丢弃的仓库物品");
            HideConfirmDialog();
            return;
        }
        
        // 执行批量删除
        var warehouseManager = WarehouseManager.Instance;
        if (warehouseManager?.Storage == null)
        {
            Debug.LogError("仓库管理器不可用");
            HideConfirmDialog();
            return;
        }
        
        int successCount = 0;
        foreach (var item in warehouseItems)
        {
            if (warehouseManager.Storage.RemoveItem(item))
            {
                successCount++;
            }
        }
        
        // 隐藏确认对话框
        HideConfirmDialog();
        
        // 退出多选模式
        multiSelectSystem.ExitMultiSelectMode();
        
        // 刷新界面
        RefreshPanels();
        
        Debug.Log($"批量丢弃完成：成功丢弃 {successCount}/{warehouseItems.Count} 个样本");
        
        if (successCount > 0)
        {
            // 可以在这里添加丢弃成功的提示音效或视觉反馈
            Debug.Log($"✅ 已丢弃 {successCount} 个样本");
        }
    }
    
    /// <summary>
    /// 取消丢弃
    /// </summary>
    void OnCancelDiscard()
    {
        HideConfirmDialog();
        Debug.Log("取消丢弃操作");
    }
    
    /// <summary>
    /// 隐藏确认对话框
    /// </summary>
    void HideConfirmDialog()
    {
        if (confirmDialogPanel != null)
        {
            confirmDialogPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// 选择模式变化回调
    /// </summary>
    void OnSelectionModeChanged(SelectionMode mode)
    {
        UpdateButtonStates(mode);
    }
    
    /// <summary>
    /// 选择变化回调
    /// </summary>
    void OnSelectionChanged(List<SampleItem> selectedItems)
    {
        
        UpdateBatchTransferButton(selectedItems);
        
        // 强制刷新所有面板的选中状态
        ForceRefreshAllSlotSelections();
    }
    
    /// <summary>
    /// 强制刷新所有槽位的选中状态
    /// </summary>
    void ForceRefreshAllSlotSelections()
    {
        if (multiSelectSystem == null) return;
        
        // 查找所有的仓库物品槽位
        WarehouseItemSlot[] allSlots = FindObjectsByType<WarehouseItemSlot>(FindObjectsSortMode.None);
        var selectedItems = multiSelectSystem.GetSelectedItems();
        
        foreach (var slot in allSlots)
        {
            if (slot.HasItem())
            {
                var item = slot.GetItem();
                bool shouldBeSelected = selectedItems.Contains(item);
                
                if (slot.IsSelected() != shouldBeSelected)
                {
                    slot.SetSelected(shouldBeSelected);
                }
            }
        }
    }
    
    /// <summary>
    /// 更新按钮状态
    /// </summary>
    void UpdateButtonStates(SelectionMode mode)
    {
        if (multiSelectButton == null || multiSelectButtonText == null) return;
        
        bool isMultiSelectActive = mode != SelectionMode.None;
        
        // 更新多选按钮
        multiSelectButton.image.color = isMultiSelectActive ? multiSelectActiveColor : normalButtonColor;
        multiSelectButtonText.text = isMultiSelectActive ? "退出多选" : "多选";
        
        // 显示/隐藏批量传输按钮
        if (batchTransferButton != null && batchTransferButtonText != null)
        {
            // 在Ready模式下也显示批量传输按钮，但设置为不可交互状态
            bool showBatchButton = mode == SelectionMode.Ready || mode == SelectionMode.BackpackSelection || mode == SelectionMode.WarehouseSelection;
            
            // 强制设置显示状态
            batchTransferButton.gameObject.SetActive(showBatchButton);
            
            if (showBatchButton)
            {
                if (mode == SelectionMode.Ready)
                {
                    batchTransferButtonText.text = "选择物品";
                    batchTransferButton.interactable = false;
                    batchTransferButton.image.color = normalButtonColor;
                }
                else if (mode == SelectionMode.BackpackSelection || mode == SelectionMode.WarehouseSelection)
                {
                    // 获取当前选中物品数量
                    int selectedCount = multiSelectSystem != null ? multiSelectSystem.GetSelectedCount() : 0;
                    
                    if (mode == SelectionMode.BackpackSelection)
                    {
                        batchTransferButtonText.text = $"放入仓库 ({selectedCount})";
                    }
                    else
                    {
                        batchTransferButtonText.text = $"放入背包 ({selectedCount})";
                    }
                    
                    batchTransferButton.interactable = selectedCount > 0;
                    batchTransferButton.image.color = selectedCount > 0 ? batchTransferActiveColor : normalButtonColor;
                }
                
                // 强制刷新按钮UI
                Canvas.ForceUpdateCanvases();
            }
        }
        
        // 显示/隐藏批量丢弃按钮（在Ready和WarehouseSelection模式下显示）
        if (batchDiscardButton != null && batchDiscardButtonText != null)
        {
            bool showDiscardButton = mode == SelectionMode.Ready || mode == SelectionMode.WarehouseSelection;
            
            // 强制设置显示状态
            batchDiscardButton.gameObject.SetActive(showDiscardButton);
            
            if (showDiscardButton)
            {
                // 获取当前选中物品数量
                int selectedCount = multiSelectSystem != null ? multiSelectSystem.GetSelectedCount() : 0;
                
                if (mode == SelectionMode.Ready)
                {
                    batchDiscardButtonText.text = "全部丢弃 (0)";
                    batchDiscardButton.interactable = false;
                    batchDiscardButton.image.color = normalButtonColor;
                }
                else if (mode == SelectionMode.WarehouseSelection)
                {
                    batchDiscardButtonText.text = $"全部丢弃 ({selectedCount})";
                    batchDiscardButton.interactable = selectedCount > 0;
                    batchDiscardButton.image.color = selectedCount > 0 ? batchDiscardActiveColor : normalButtonColor;
                }
                
                // 强制刷新按钮UI
                Canvas.ForceUpdateCanvases();
            }
        }
    }
    
    /// <summary>
    /// 更新批量传输按钮
    /// </summary>
    void UpdateBatchTransferButton(List<SampleItem> selectedItems)
    {
        if (batchTransferButton == null || batchTransferButtonText == null) return;
        
        var mode = multiSelectSystem.GetCurrentSelectionMode();
        int count = selectedItems.Count;
        
        if (mode == SelectionMode.BackpackSelection)
        {
            batchTransferButtonText.text = $"放入仓库 ({count})";
            batchTransferButton.interactable = count > 0;
            batchTransferButton.image.color = count > 0 ? batchTransferActiveColor : normalButtonColor;
        }
        else if (mode == SelectionMode.WarehouseSelection)
        {
            batchTransferButtonText.text = $"放入背包 ({count})";
            batchTransferButton.interactable = count > 0;
            batchTransferButton.image.color = count > 0 ? batchTransferActiveColor : normalButtonColor;
            
            // 同时更新批量丢弃按钮
            if (batchDiscardButton != null && batchDiscardButtonText != null)
            {
                batchDiscardButtonText.text = $"全部丢弃 ({count})";
                batchDiscardButton.interactable = count > 0;
                batchDiscardButton.image.color = count > 0 ? batchDiscardActiveColor : normalButtonColor;
            }
        }
        else if (mode == SelectionMode.Ready)
        {
            batchTransferButtonText.text = "选择物品";
            batchTransferButton.interactable = false;
            batchTransferButton.image.color = normalButtonColor;
        }
        else
        {
            batchTransferButtonText.text = "选择物品";
            batchTransferButton.interactable = false;
            batchTransferButton.image.color = normalButtonColor;
        }
    }
    
    /// <summary>
    /// 处理输入
    /// </summary>
    void HandleInput()
    {
        if (!isWarehouseOpen) return;
        
        // ESC键处理
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // 如果确认对话框正在显示，先关闭对话框
            if (confirmDialogPanel != null && confirmDialogPanel.activeInHierarchy)
            {
                HideConfirmDialog();
            }
            else
            {
                // 否则关闭仓库界面
                CloseWarehouseInterface();
            }
        }
    }
    
    /// <summary>
    /// 禁用玩家控制
    /// </summary>
    void DisablePlayerControls()
    {
        if (fpController != null)
        {
            fpController.enableMouseLook = false;
        }
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f; // 不暂停游戏
    }
    
    /// <summary>
    /// 启用玩家控制
    /// </summary>
    void EnablePlayerControls()
    {
        if (fpController != null)
        {
            fpController.enableMouseLook = true;
        }
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    /// <summary>
    /// 获取仓库开启状态
    /// </summary>
    public bool IsWarehouseOpen()
    {
        return isWarehouseOpen;
    }
    
    /// <summary>
    /// 强制修复多选视觉反馈问题
    /// </summary>
    [ContextMenu("强制修复多选视觉反馈")]
    public void ForceFixMultiSelectVisuals()
    {
        Debug.Log("[WarehouseUI] 开始强制修复多选视觉反馈...");
        
        // 1. 强制更新按钮状态
        if (multiSelectSystem != null)
        {
            var mode = multiSelectSystem.GetCurrentSelectionMode();
            Debug.Log($"[WarehouseUI] 当前模式: {mode}");
            UpdateButtonStates(mode);
            
            var selectedItems = multiSelectSystem.GetSelectedItems();
            Debug.Log($"[WarehouseUI] 当前选中物品数量: {selectedItems.Count}");
            UpdateBatchTransferButton(selectedItems);
        }
        
        // 2. 强制刷新所有槽位的选中标记
        ForceRefreshAllSlotSelections();
        
        // 3. 强制刷新Canvas
        Canvas.ForceUpdateCanvases();
        
        Debug.Log("[WarehouseUI] 强制修复完成");
    }
    
    /// <summary>
    /// 检查多选系统状态
    /// </summary>
    [ContextMenu("检查多选系统状态")]
    public void CheckMultiSelectSystemStatus()
    {
        Debug.Log("=== 多选系统状态检查 ===");
        
        if (multiSelectSystem != null)
        {
            Debug.Log($"多选系统存在: 是");
            Debug.Log($"当前模式: {multiSelectSystem.GetCurrentSelectionMode()}");
            Debug.Log($"选中物品数量: {multiSelectSystem.GetSelectedCount()}");
            Debug.Log($"是否在多选模式: {multiSelectSystem.IsInMultiSelectMode()}");
        }
        else
        {
            Debug.LogError("多选系统不存在!");
        }
        
        Debug.Log($"多选按钮存在: {multiSelectButton != null}");
        Debug.Log($"批量传输按钮存在: {batchTransferButton != null}");
        Debug.Log($"批量丢弃按钮存在: {batchDiscardButton != null}");
        
        if (multiSelectButton != null && multiSelectButtonText != null)
        {
            Debug.Log($"多选按钮状态: 文本='{multiSelectButtonText.text}', 颜色={multiSelectButton.image.color}");
        }
        
        if (batchTransferButton != null && batchTransferButtonText != null)
        {
            Debug.Log($"批量传输按钮状态: 文本='{batchTransferButtonText.text}', 活跃={batchTransferButton.gameObject.activeInHierarchy}, 可交互={batchTransferButton.interactable}");
        }
        
        if (batchDiscardButton != null && batchDiscardButtonText != null)
        {
            Debug.Log($"批量丢弃按钮状态: 文本='{batchDiscardButtonText.text}', 活跃={batchDiscardButton.gameObject.activeInHierarchy}, 可交互={batchDiscardButton.interactable}");
        }
        else
        {
            Debug.LogError("批量丢弃按钮或文本组件不存在!");
        }
        
        // 检查槽位状态
        WarehouseItemSlot[] allSlots = FindObjectsByType<WarehouseItemSlot>(FindObjectsSortMode.None);
        int slotsWithItems = 0;
        int selectedSlots = 0;
        
        foreach (var slot in allSlots)
        {
            if (slot.HasItem())
            {
                slotsWithItems++;
                if (slot.IsSelected())
                {
                    selectedSlots++;
                }
            }
        }
        
        Debug.Log($"槽位统计: 总数={allSlots.Length}, 有物品={slotsWithItems}, 选中={selectedSlots}");
    }
    
    /// <summary>
    /// 清空所有槽位的选中状态
    /// </summary>
    void ClearAllSlotSelections()
    {
        WarehouseItemSlot[] allSlots = FindObjectsByType<WarehouseItemSlot>(FindObjectsSortMode.None);
        
        int clearedSlots = 0;
        foreach (var slot in allSlots)
        {
            if (slot.IsSelected())
            {
                slot.SetSelected(false);
                clearedSlots++;
            }
        }
        
        if (clearedSlots > 0)
        {
            Debug.Log($"[WarehouseUI] 清空了 {clearedSlots} 个槽位的选中状态");
        }
    }
    
    /// <summary>
    /// 界面打开后刷新面板
    /// </summary>
    System.Collections.IEnumerator RefreshPanelsAfterOpen()
    {
        // 等待一帧确保界面完全激活
        yield return new WaitForEndOfFrame();
        
        // 强制刷新面板
        RefreshPanels();
        
        // 再等一帧后再次刷新，确保数据完全同步
        yield return new WaitForEndOfFrame();
        RefreshPanels();
    }
    
    /// <summary>
    /// 延迟清空选中状态
    /// </summary>
    System.Collections.IEnumerator DelayedClearSelections()
    {
        yield return new WaitForEndOfFrame();
        ClearAllSlotSelections();
        
        // 再次刷新界面确保数据同步
        RefreshPanels();
        
        yield return new WaitForEndOfFrame();
        ClearAllSlotSelections(); // 再次清空确保完全清除
        
        // 最后强制刷新Canvas
        Canvas.ForceUpdateCanvases();
    }
    
    
    void OnDestroy()
    {
        // 取消事件订阅
        if (multiSelectSystem != null)
        {
            multiSelectSystem.OnSelectionModeChanged -= OnSelectionModeChanged;
            multiSelectSystem.OnSelectionChanged -= OnSelectionChanged;
        }
    }
    
    /// <summary>
    /// 设置面板的多选系统引用
    /// </summary>
    void SetPanelMultiSelectSystem(MonoBehaviour panel)
    {
        if (multiSelectSystem == null)
        {
            Debug.LogError("[WarehouseUI] 多选系统未创建，无法设置面板引用");
            return;
        }
        
        if (panel is WarehouseInventoryPanel invPanel)
        {
            // 使用反射设置私有字段
            var field = typeof(WarehouseInventoryPanel).GetField("multiSelectSystem", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(invPanel, multiSelectSystem);
                Debug.Log("[WarehouseUI] 已设置WarehouseInventoryPanel的多选系统引用");
                
                // 订阅事件（避免重复订阅）
                multiSelectSystem.OnSelectionChanged -= invPanel.OnSelectionChanged;
                multiSelectSystem.OnSelectionChanged += invPanel.OnSelectionChanged;
            }
        }
        else if (panel is WarehouseStoragePanel storagePanel)
        {
            // 使用反射设置私有字段
            var field = typeof(WarehouseStoragePanel).GetField("multiSelectSystem", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(storagePanel, multiSelectSystem);
                Debug.Log("[WarehouseUI] 已设置WarehouseStoragePanel的多选系统引用");
                
                // 订阅事件（避免重复订阅）
                multiSelectSystem.OnSelectionChanged -= storagePanel.OnSelectionChanged;
                multiSelectSystem.OnSelectionChanged += storagePanel.OnSelectionChanged;
            }
        }
    }
    
    /// <summary>
    /// 延迟初始化面板组件（确保Start方法被调用）
    /// </summary>
    System.Collections.IEnumerator InitializePanelAfterFrame(MonoBehaviour panel)
    {
        yield return new WaitForEndOfFrame();
        
        if (panel is WarehouseInventoryPanel invPanel)
        {
            // 直接设置多选系统引用
            SetPanelMultiSelectSystem(invPanel);
            
            // 强制调用初始化方法
            invPanel.RefreshInventoryDisplay();
            Debug.Log("强制初始化WarehouseInventoryPanel完成");
        }
        else if (panel is WarehouseStoragePanel storagePanel)
        {
            // 直接设置多选系统引用
            SetPanelMultiSelectSystem(storagePanel);
            
            // 强制调用初始化方法
            storagePanel.RefreshStorageDisplay();
            Debug.Log("强制初始化WarehouseStoragePanel完成");
        }
    }
}
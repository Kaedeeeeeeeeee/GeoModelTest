using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using System.Collections;
using System.Collections.Generic;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

/// <summary>
/// 移动端虚拟控制界面
/// 包含虚拟摇杆、触摸区域、虚拟按钮等移动端专用控件
/// </summary>
public class MobileControlsUI : MonoBehaviour
{
    private enum ControlButtonRole
    {
        Action,
        Interact,
        Utility,
        Drone
    }

    private enum ControlButtonIcon
    {
        Jump,
        Run,
        Interact,
        Secondary,
        Inventory,
        Encyclopedia,
        Tools,
        Menu,
        Ascend,
        Descend
    }

    private static readonly Color LegacyJoystickBackgroundColor = new Color(1f, 1f, 1f, 0.3f);
    private static readonly Color LegacyJoystickHandleColor = new Color(1f, 1f, 1f, 0.6f);
    private static readonly Color LegacyButtonNormalColor = new Color(1f, 1f, 1f, 0.7f);
    private static readonly Color LegacyButtonPressedColor = new Color(0.8f, 0.8f, 0.8f, 0.9f);
    private static readonly Color ModernJoystickBackgroundColor = new Color(0.04f, 0.055f, 0.065f, 0.42f);
    private static readonly Color ModernJoystickHandleColor = new Color(0.78f, 0.94f, 1f, 0.82f);
    private static readonly Color ModernButtonNormalColor = new Color(0.045f, 0.055f, 0.07f, 0.78f);
    private static readonly Color ModernButtonPressedColor = new Color(0.98f, 0.75f, 0.32f, 0.94f);
    private const float ButtonPressedScale = 0.92f;

    [Header("虚拟摇杆设置")]
    public GameObject joystickContainer;
    public RectTransform joystickBackground;
    public RectTransform joystickHandle;
    public float joystickRange = 86f;
    public float joystickHitAreaMultiplier = 1.7f;
    public bool isDynamicJoystick = false; // 动态摇杆位置（桌面测试建议禁用）

    [Header("虚拟按钮")]
    public Button jumpButton;
    public Button runButton;
    public Button interactButton; // E键交互按钮
    public Button secondaryInteractButton; // F键交互按钮
    public Button inventoryButton;
    // public Button warehouseButton; // 仓库按钮已移除，使用其他方式访问仓库
    public Button encyclopediaButton;
    public Button toolWheelButton;
    public Button menuButton;

    [Header("移动端菜单")]
    public GameObject mobileMenuPanel;
    public Button resumeMenuButton;
    public Button settingsMenuButton;
    public Button quitMenuButton;

    [Header("无人机专用按钮")]
    public Button ascendButton; // 上升按钮（无人机模式）
    public Button descendButton; // 下降按钮（无人机模式）
    public GameObject droneControlsContainer; // 无人机控制容器（用于显示/隐藏）

    [Header("触摸区域")]
    public RectTransform lookTouchArea; // 视角控制区域

    [Header("UI布局")]
    public float buttonSize = 120f;
    public float buttonSpacing = 36f;
    public float edgeMargin = 64f;
    public Vector2 joystickPosition = new Vector2(190, 165); // 从左下角的偏移 - 安全可见位置

    [Header("视觉效果")]
    public Color joystickBackgroundColor = ModernJoystickBackgroundColor;
    public Color joystickHandleColor = ModernJoystickHandleColor;
    public Color buttonNormalColor = ModernButtonNormalColor;
    public Color buttonPressedColor = ModernButtonPressedColor;

    [Header("自适应设置")]
    public bool autoHideOnDesktop = true;
    public bool adaptToSafeArea = true;

    [Header("调试")]
    public bool enableDebugVisualization = false;
    public bool forceShowOnDesktop = false; // 强制在桌面显示（用于测试）
    public bool enableMouseInput = true; // 桌面测试模式下允许鼠标输入
    public bool enableRawTouchFallback = true; // WebGL/iPad兜底：直接读取Touchscreen命中虚拟控件

    public static MobileControlsUI ActiveInstance { get; private set; }

    // 私有变量
    private Canvas controlsCanvas;
    private CanvasScaler canvasScaler;
    private MobileInputManager inputManager;

    // 摇杆相关
    private bool isJoystickActive = false;
    private Vector2 joystickInput = Vector2.zero;
    private Vector2 joystickStartPosition;
    private int joystickPointerId = -1;

    // 触摸区域相关
    private bool isLookTouchActive = false;
    private Vector2 lastLookTouchPosition;
    private int lookTouchPointerId = -1;

    // 按钮状态
    private bool isRunPressed = false;
    private const int NoTouchId = int.MinValue;
    private int rawJoystickTouchId = NoTouchId;
    private int rawJumpTouchId = NoTouchId;
    private int rawRunTouchId = NoTouchId;
    private int rawInteractTouchId = NoTouchId;
    private int rawSecondaryInteractTouchId = NoTouchId;
    private int rawInventoryTouchId = NoTouchId;
    private int rawEncyclopediaTouchId = NoTouchId;
    private int rawToolWheelTouchId = NoTouchId;
    private int rawMenuTouchId = NoTouchId;
    private int rawResumeMenuTouchId = NoTouchId;
    private int rawSettingsMenuTouchId = NoTouchId;
    private int rawQuitMenuTouchId = NoTouchId;
    private int rawAscendTouchId = NoTouchId;
    private int rawDescendTouchId = NoTouchId;
    private readonly HashSet<int> activeRawTouchIds = new HashSet<int>();
    private bool isLanguageChangeSubscribed = false;
    private bool hasPublishedJoystickInput = false;
    private bool isMobileMenuOpen = false;
    private float mobileMenuOriginalTimeScale = 1f;
    private ModalCanvasLayerGuard.Scope mobileMenuCanvasScope;
    private FirstPersonController pausedPlayerController;
    private bool pausedPlayerControllerWasEnabled;

    void Awake()
    {
        ActiveInstance = this;

        // 获取或创建Canvas
        SetupCanvas();
        EnsureEventSystemForTouch();
    }

    void OnEnable()
    {
        ActiveInstance = this;
    }

    void Start()
    {
        // 获取输入管理器引用（在Start中确保MobileInputManager已初始化）
        inputManager = MobileInputManager.Instance;
        if (inputManager == null)
        {
            // 尝试在场景中查找
            inputManager = FindFirstObjectByType<MobileInputManager>();
            if (inputManager == null)
            {
                Debug.LogError("[MobileControlsUI] 未找到MobileInputManager！移动端输入无法工作");
            }
            else
            {
                Debug.Log("[MobileControlsUI] 通过FindFirstObjectByType找到MobileInputManager");
            }
        }
        else
        {
            Debug.Log("[MobileControlsUI] 通过Instance找到MobileInputManager");
        }

        // 原有的Start逻辑
        StartOriginalLogic();
        SubscribeLanguageChanges();
        UpdateLocalizedButtonLabels();
    }

    void StartOriginalLogic()
    {
        // 根据设备类型决定是否显示
        bool shouldShow = ShouldShowForCurrentDevice();

        gameObject.SetActive(shouldShow);

        // 设置界面
        if (gameObject.activeInHierarchy)
        {
            SetupVirtualControls();
            SetupSafeArea();
            Debug.Log("[MobileControlsUI] 虚拟控制组件设置完成");
        }
        else
        {
            Debug.LogWarning("[MobileControlsUI] GameObject未激活，跳过虚拟控制设置");
        }

        Debug.Log($"[MobileControlsUI] 虚拟控制界面初始化完成 - 激活状态: {gameObject.activeInHierarchy}");
    }

    bool ShouldShowForCurrentDevice()
    {
        if (forceShowOnDesktop)
        {
            Debug.Log("[MobileControlsUI] 强制显示模式 - 显示虚拟控件");
            return true;
        }

        if (inputManager != null)
        {
            bool shouldShow = inputManager.ShouldShowVirtualControls();
            Debug.Log($"[MobileControlsUI] 平台检测 - 移动设备: {inputManager.IsMobileDevice()}, 应该显示虚拟控件: {shouldShow}");
            return shouldShow;
        }

        bool isMobile = MobileInputManager.IsRuntimeMobileDevice();
        bool shouldShowWithoutManager = isMobile;
        Debug.Log($"[MobileControlsUI] 无输入管理器平台检测 - 移动设备: {isMobile}, 应该显示虚拟控件: {shouldShowWithoutManager}");
        return shouldShowWithoutManager;
    }

    void Update()
    {
        // 处理摇杆输入
        ProcessJoystickInput();
        ProcessRawTouchFallbackInput();

        // 桌面测试模式：处理鼠标输入模拟触摸
        if (enableMouseInput && (forceShowOnDesktop || (inputManager != null && inputManager.desktopTestMode)))
        {
            ProcessMouseInput();

            var keyboard = Keyboard.current;

            // 调试快捷键：R键重置摇杆位置
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
            {
                ResetJoystickPosition();
                Debug.Log("[MobileControlsUI] R键重置摇杆位置");
            }
        }

        // 发送输入数据给输入管理器
        PublishJoystickInput();
    }

    void OnDisable()
    {
        if (isMobileMenuOpen)
        {
            CloseMobileMenu();
        }

        if (ActiveInstance == this)
        {
            ActiveInstance = null;
        }

        ResetControlState();
    }

    void OnDestroy()
    {
        UnsubscribeLanguageChanges();
    }

    /// <summary>
    /// 设置Canvas组件
    /// </summary>
    void SetupCanvas()
    {
        controlsCanvas = GetComponent<Canvas>();
        if (controlsCanvas == null)
        {
            controlsCanvas = gameObject.AddComponent<Canvas>();
        }

        // 强制设置为屏幕覆盖模式
        controlsCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        controlsCanvas.sortingOrder = 100; // 设置为较低层级，让仓库UI等功能性UI在上层

        // 重要：重置transform，确保不受父对象影响
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        // 添加GraphicRaycaster用于UI交互
        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        // 设置CanvasScaler以适应不同分辨率
        canvasScaler = GetComponent<CanvasScaler>();
        if (canvasScaler == null)
        {
            canvasScaler = gameObject.AddComponent<CanvasScaler>();
        }

        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.matchWidthOrHeight = 0.5f;

        Debug.Log($"[MobileControlsUI] Canvas设置完成 - 渲染模式: {controlsCanvas.renderMode}, 排序: {controlsCanvas.sortingOrder}");
    }

    /// <summary>
    /// 设置虚拟控制组件
    /// </summary>
    void SetupVirtualControls()
    {
        // 如果组件为空，自动创建
        if (joystickContainer == null) CreateVirtualJoystick();
        if (jumpButton == null) CreateVirtualButtons();
        if (menuButton == null) CreateMobileMenuButton();
        if (mobileMenuPanel == null) CreateMobilePauseMenu();
        if (lookTouchArea == null) CreateLookTouchArea();
        ConfigureLookTouchAreaRaycasts();
        UpdateLocalizedButtonLabels();

        // 设置事件监听
        SetupButtonEvents();
        SetupTouchEvents();
    }

    /// <summary>
    /// 创建虚拟摇杆
    /// </summary>
    void CreateVirtualJoystick()
    {
        // 创建摇杆容器
        GameObject container = new GameObject("VirtualJoystick");
        container.transform.SetParent(transform, false);

        RectTransform containerRect = container.AddComponent<RectTransform>();
        float joystickDiameter = joystickRange * 2f;
        float hitDiameter = joystickDiameter * Mathf.Max(joystickHitAreaMultiplier, 1f);

        containerRect.sizeDelta = new Vector2(hitDiameter, hitDiameter);
        containerRect.anchorMin = new Vector2(0, 0);
        containerRect.anchorMax = new Vector2(0, 0);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = joystickPosition;

        joystickContainer = container;

        Image hitImage = container.AddComponent<Image>();
        hitImage.color = enableDebugVisualization ? new Color(1f, 0.9f, 0f, 0.08f) : new Color(1f, 1f, 1f, 0f);
        hitImage.raycastTarget = true;

        // 创建摇杆背景
        GameObject background = new GameObject("JoystickBackground");
        background.transform.SetParent(container.transform, false);

        joystickBackground = background.AddComponent<RectTransform>();
        joystickBackground.sizeDelta = new Vector2(joystickDiameter, joystickDiameter);
        joystickBackground.anchorMin = new Vector2(0.5f, 0.5f);
        joystickBackground.anchorMax = new Vector2(0.5f, 0.5f);
        joystickBackground.pivot = new Vector2(0.5f, 0.5f);
        joystickBackground.anchoredPosition = Vector2.zero;

        Image bgImage = background.AddComponent<Image>();
        bgImage.sprite = CreateCircleSprite(128);
        bgImage.color = GetJoystickBackgroundColor(false);
        bgImage.type = Image.Type.Simple;
        bgImage.raycastTarget = false;

        Image bgRingImage = CreateChildImage(
            background.transform,
            "JoystickOuterRing",
            CreateRingSprite(128, 0.82f, 0.98f),
            new Color(0.72f, 0.92f, 1f, 0.42f),
            Vector2.zero,
            Vector2.one);
        bgRingImage.type = Image.Type.Simple;

        // 创建摇杆手柄
        GameObject handle = new GameObject("JoystickHandle");
        handle.transform.SetParent(container.transform, false);

        joystickHandle = handle.AddComponent<RectTransform>();
        joystickHandle.sizeDelta = new Vector2(joystickRange, joystickRange);
        joystickHandle.anchorMin = new Vector2(0.5f, 0.5f);
        joystickHandle.anchorMax = new Vector2(0.5f, 0.5f);
        joystickHandle.pivot = new Vector2(0.5f, 0.5f);
        joystickHandle.anchoredPosition = Vector2.zero;

        Image handleImage = handle.AddComponent<Image>();
        handleImage.sprite = CreateCircleSprite(64);
        handleImage.color = GetJoystickHandleColor(false);
        handleImage.type = Image.Type.Simple;
        handleImage.raycastTarget = false;

        Image handleRingImage = CreateChildImage(
            handle.transform,
            "JoystickHandleRing",
            CreateRingSprite(96, 0.68f, 0.96f),
            new Color(1f, 1f, 1f, 0.55f),
            Vector2.zero,
            Vector2.one);
        handleRingImage.type = Image.Type.Simple;

        Debug.Log("[MobileControlsUI] 虚拟摇杆创建完成");
    }

    /// <summary>
    /// 重置摇杆到初始位置
    /// </summary>
    public void ResetJoystickPosition()
    {
        if (joystickContainer != null)
        {
            RectTransform containerRect = joystickContainer.GetComponent<RectTransform>();
            if (containerRect != null)
            {
                containerRect.anchoredPosition = joystickPosition;
                Debug.Log($"[MobileControlsUI] 摇杆位置已重置到: {joystickPosition}");
            }
        }

        if (joystickHandle != null)
        {
            joystickHandle.anchoredPosition = Vector2.zero;
        }

        SetJoystickVisualActive(false);

        // 重置摇杆状态
        isJoystickActive = false;
        joystickInput = Vector2.zero;
        joystickPointerId = -1;
    }

    /// <summary>
    /// 创建虚拟按钮
    /// </summary>
    void CreateVirtualButtons()
    {
        // 跳跃按钮
        float primaryX = -edgeMargin - buttonSize * 0.55f;
        float bottomY = edgeMargin + buttonSize * 0.55f;
        float staggerSpacing = buttonSpacing * 1.15f;
        float secondaryX = primaryX - buttonSize - staggerSpacing;
        float runX = secondaryX - buttonSize - staggerSpacing;
        float middleY = bottomY + buttonSize * 0.55f + buttonSpacing * 0.5f;
        float upperY = bottomY + buttonSize + staggerSpacing;

        // 右下角动作区：主按钮靠右，辅助按钮错位展开，减少4键挤在一起造成的误触。
        jumpButton = CreateButton("JumpButton", "ジャンプ", new Vector2(primaryX, bottomY),
                                  new Vector2(1, 0), OnJumpButtonDown, OnJumpButtonUp);

        // 奔跑按钮
        runButton = CreateButton("RunButton", "走る", new Vector2(runX, bottomY),
                                 new Vector2(1, 0), OnRunButtonDown, OnRunButtonUp);

        // 主交互按钮 - 右下角
        interactButton = CreateButton("InteractButton", "調べる", new Vector2(primaryX, upperY),
                                      new Vector2(1, 0), OnInteractButtonDown, OnInteractButtonUp);

        // 次操作按钮 - 主交互左侧
        secondaryInteractButton = CreateButton("SecondaryInteractButton", "使う", new Vector2(secondaryX, middleY),
                                               new Vector2(1, 0), OnSecondaryInteractButtonDown, OnSecondaryInteractButtonUp);

        // 顶部低频入口
        inventoryButton = CreateButton("InventoryButton", "バッグ", new Vector2(edgeMargin + buttonSize/2, -edgeMargin - buttonSize/2),
                                       new Vector2(0, 1), OnInventoryButtonClick, null);

        encyclopediaButton = CreateButton("EncyclopediaButton", "図鑑", new Vector2(edgeMargin + buttonSize * 1.5f + buttonSpacing, -edgeMargin - buttonSize/2),
                                          new Vector2(0, 1), OnEncyclopediaButtonClick, null);

        toolWheelButton = CreateButton("ToolWheelButton", "道具", new Vector2(edgeMargin + buttonSize * 2.5f + buttonSpacing * 2, -edgeMargin - buttonSize/2),
                                       new Vector2(0, 1), OnToolWheelButtonClick, null);

        CreateMobileMenuButton();

        // 创建无人机控制容器
        CreateDroneControls();

        Debug.Log("[MobileControlsUI] 虚拟按钮创建完成");
    }

    void CreateMobileMenuButton()
    {
        menuButton = CreateButton("MenuButton", "メニュー", new Vector2(-edgeMargin - buttonSize/2, -edgeMargin - buttonSize/2),
                                  new Vector2(1, 1), OnMenuButtonClick, null);
    }

    void CreateMobilePauseMenu()
    {
        GameObject overlay = new GameObject("MobilePauseMenu");
        overlay.transform.SetParent(transform, false);

        RectTransform overlayRect = overlay.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image overlayImage = overlay.AddComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.72f);
        overlayImage.raycastTarget = true;

        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(overlay.transform, false);

        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.30f, 0.16f);
        panelRect.anchorMax = new Vector2(0.70f, 0.84f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.06f, 0.075f, 0.085f, 1f);
        panelImage.raycastTarget = true;
        panel.AddComponent<RectMask2D>();

        CreateMenuText(panel.transform, "Title", "ui.mobile_menu.title", "メニュー", 34,
            new Vector2(0.08f, 0.80f), new Vector2(0.92f, 0.94f), FontStyle.Bold);

        resumeMenuButton = CreateMenuOptionButton(panel.transform, "ResumeButton", "ui.mobile_menu.resume", "ゲームに戻る",
            new Vector2(0.12f, 0.59f), new Vector2(0.88f, 0.73f), CloseMobileMenu);

        settingsMenuButton = CreateMenuOptionButton(panel.transform, "SettingsButton", "ui.button.settings", "設定",
            new Vector2(0.12f, 0.40f), new Vector2(0.88f, 0.54f), OpenSettingsFromMobileMenu);

        quitMenuButton = CreateMenuOptionButton(panel.transform, "QuitButton", "ui.mobile_menu.quit", "ゲーム終了",
            new Vector2(0.12f, 0.21f), new Vector2(0.88f, 0.35f), QuitGameFromMobileMenu);

        mobileMenuPanel = overlay;
        mobileMenuPanel.transform.SetAsLastSibling();
        mobileMenuPanel.SetActive(false);
    }

    Text CreateMenuText(Transform parent, string name, string localizationKey, string fallbackText, int fontSize,
                        Vector2 anchorMin, Vector2 anchorMax, FontStyle fontStyle = FontStyle.Normal)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);

        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Text text = textObj.AddComponent<Text>();
        text.text = fallbackText;
        text.font = UIFontResolver.GetUIFont();
        text.fontSize = fontSize;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 12;
        text.resizeTextMaxSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontStyle = fontStyle;
        text.raycastTarget = false;

        LocalizedText localizedText = textObj.AddComponent<LocalizedText>();
        localizedText.TextKey = localizationKey;

        return text;
    }

    Button CreateMenuOptionButton(Transform parent, string name, string localizationKey, string fallbackText,
                                  Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);

        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = buttonObj.AddComponent<Image>();
        image.color = new Color(0.12f, 0.24f, 0.27f, 0.92f);
        image.raycastTarget = true;

        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.18f, 0.34f, 0.38f, 0.96f);
        colors.pressedColor = new Color(0.08f, 0.18f, 0.2f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.08f, 0.08f, 0.08f, 0.5f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.06f;
        button.colors = colors;

        CreateMenuText(buttonObj.transform, "Text", localizationKey, fallbackText, 24,
            Vector2.zero, Vector2.one, FontStyle.Bold);

        EventTrigger trigger = buttonObj.AddComponent<EventTrigger>();
        AddTriggerEntry(trigger, EventTriggerType.PointerDown, (data) => SetButtonVisualPressed(button, true));
        AddTriggerEntry(trigger, EventTriggerType.PointerUp, (data) =>
        {
            SetButtonVisualPressed(button, false);
            InvokeClickForPointerEvent(data, onClick);
        });
        AddTriggerEntry(trigger, EventTriggerType.PointerExit, (data) => SetButtonVisualPressed(button, false));
        return button;
    }

    /// <summary>
    /// 创建单个按钮
    /// </summary>
    Button CreateButton(string name, string text, Vector2 position, Vector2 anchor,
                       UnityEngine.Events.UnityAction onDown, UnityEngine.Events.UnityAction onUp)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(transform, false);

        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(buttonSize, buttonSize);
        buttonRect.anchorMin = anchor;
        buttonRect.anchorMax = anchor;
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = position;

        Image buttonImage = buttonObj.AddComponent<Image>();

        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        ConfigureGameButton(button, buttonImage, name);

        // 添加按钮文本
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.sizeDelta = Vector2.zero;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.anchoredPosition = Vector2.zero;

        Text buttonText = textObj.AddComponent<Text>();
        ConfigureButtonText(buttonText, text);

        // 设置按钮事件
        EventTrigger trigger = buttonObj.AddComponent<EventTrigger>();
        if (onDown != null)
        {
            if (onUp != null)
            {
                AddTriggerEntry(trigger, EventTriggerType.PointerDown, (data) =>
                {
                    SetButtonVisualPressed(button, true);
                    onDown.Invoke();
                });
                AddTriggerEntry(trigger, EventTriggerType.PointerUp, (data) =>
                {
                    SetButtonVisualPressed(button, false);
                    onUp.Invoke();
                });
                AddTriggerEntry(trigger, EventTriggerType.PointerExit, (data) =>
                {
                    SetButtonVisualPressed(button, false);
                    onUp.Invoke();
                });
            }
            else
            {
                AddTriggerEntry(trigger, EventTriggerType.PointerDown, (data) => SetButtonVisualPressed(button, true));
                AddTriggerEntry(trigger, EventTriggerType.PointerUp, (data) =>
                {
                    SetButtonVisualPressed(button, false);
                    InvokeClickForPointerEvent(data, onDown);
                });
                AddTriggerEntry(trigger, EventTriggerType.PointerExit, (data) => SetButtonVisualPressed(button, false));
            }
        }
        else
        {
            AddTriggerEntry(trigger, EventTriggerType.PointerDown, (data) => SetButtonVisualPressed(button, true));
            AddTriggerEntry(trigger, EventTriggerType.PointerUp, (data) => SetButtonVisualPressed(button, false));
            AddTriggerEntry(trigger, EventTriggerType.PointerExit, (data) => SetButtonVisualPressed(button, false));
        }

        return button;
    }

    /// <summary>
    /// 创建无人机控制按钮
    /// </summary>
    void CreateDroneControls()
    {
        // 创建无人机控制容器 - 位置在右下角，替代E和F键位置
        GameObject droneContainer = new GameObject("DroneControlsContainer");
        droneContainer.transform.SetParent(transform, false);

        RectTransform containerRect = droneContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(1f, 0f);
        containerRect.anchorMax = new Vector2(1f, 0f);
        containerRect.pivot = new Vector2(1f, 0f);
        containerRect.anchoredPosition = new Vector2(-edgeMargin, edgeMargin); // 恢复到安全可见位置
        containerRect.sizeDelta = new Vector2(buttonSize, buttonSize * 2.5f + buttonSpacing);

        droneControlsContainer = droneContainer;

        // 创建上升按钮（右下角上方位置，对应F键位置）
        ascendButton = CreateButton("AscendButton", "上昇", new Vector2(0, buttonSize * 1.5f + buttonSpacing),
                                   new Vector2(0.5f, 0f), OnAscendButtonDown, OnAscendButtonUp, droneContainer.transform);

        // 创建下降按钮（右下角下方位置，对应E键位置）
        descendButton = CreateButton("DescendButton", "下降", new Vector2(0, buttonSize * 0.5f),
                                    new Vector2(0.5f, 0f), OnDescendButtonDown, OnDescendButtonUp, droneContainer.transform);

        // 默认隐藏无人机控制（只有在无人机模式下才显示）
        SetDroneControlsVisible(false);

        Debug.Log("[MobileControlsUI] 无人机控制按钮创建完成");
    }

    /// <summary>
    /// 创建带有父级的按钮
    /// </summary>
    Button CreateButton(string name, string text, Vector2 position, Vector2 anchor,
                       System.Action onDown, System.Action onUp, Transform parent)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);

        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(buttonSize, buttonSize);

        // 添加Image组件
        Image image = buttonObj.AddComponent<Image>();

        // 添加Button组件
        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = image;
        ConfigureGameButton(button, image, name);

        // 添加文字
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text buttonText = textObj.AddComponent<Text>();
        ConfigureButtonText(buttonText, text);

        // 设置按钮事件
        EventTrigger trigger = buttonObj.AddComponent<EventTrigger>();
        if (onUp != null)
        {
            AddTriggerEntry(trigger, EventTriggerType.PointerDown, (data) =>
            {
                SetButtonVisualPressed(button, true);
                onDown?.Invoke();
            });
            AddTriggerEntry(trigger, EventTriggerType.PointerUp, (data) =>
            {
                SetButtonVisualPressed(button, false);
                onUp.Invoke();
            });
            AddTriggerEntry(trigger, EventTriggerType.PointerExit, (data) =>
            {
                SetButtonVisualPressed(button, false);
                onUp.Invoke();
            });
        }
        else
        {
            AddTriggerEntry(trigger, EventTriggerType.PointerDown, (data) => SetButtonVisualPressed(button, true));
            AddTriggerEntry(trigger, EventTriggerType.PointerUp, (data) =>
            {
                SetButtonVisualPressed(button, false);
                InvokeClickForPointerEvent(data, onDown);
            });
            AddTriggerEntry(trigger, EventTriggerType.PointerExit, (data) => SetButtonVisualPressed(button, false));
        }

        return button;
    }

    bool ShouldRawTouchFallbackHandleClick(BaseEventData eventData)
    {
        PointerEventData pointerData = eventData as PointerEventData;
        return pointerData != null &&
               pointerData.pointerId >= 0 &&
               enableRawTouchFallback &&
               Touchscreen.current != null;
    }

    void InvokeClickForPointerEvent(BaseEventData eventData, UnityEngine.Events.UnityAction onClick)
    {
        if (!ShouldRawTouchFallbackHandleClick(eventData))
        {
            onClick?.Invoke();
        }
    }

    void InvokeClickForPointerEvent(BaseEventData eventData, System.Action onClick)
    {
        if (!ShouldRawTouchFallbackHandleClick(eventData))
        {
            onClick?.Invoke();
        }
    }

    /// <summary>
    /// 创建视角触摸区域
    /// </summary>
    void CreateLookTouchArea()
    {
        GameObject touchArea = new GameObject("LookTouchArea");
        touchArea.transform.SetParent(transform, false);
        touchArea.transform.SetAsFirstSibling();

        lookTouchArea = touchArea.AddComponent<RectTransform>();
        lookTouchArea.anchorMin = new Vector2(0.3f, 0.3f);
        lookTouchArea.anchorMax = new Vector2(1f, 1f);
        lookTouchArea.offsetMin = Vector2.zero;
        lookTouchArea.offsetMax = Vector2.zero;

        // 视角拖拽由MobileInputManager读取原始触摸；这里不能抢UI射线。
        Image touchImage = touchArea.AddComponent<Image>();
        touchImage.color = new Color(0, 0, 0, 0); // 完全透明
        touchImage.raycastTarget = false;

        if (enableDebugVisualization)
        {
            touchImage.color = new Color(0, 1, 0, 0.1f); // 调试时显示绿色半透明
        }

        Debug.Log("[MobileControlsUI] 视角触摸区域创建完成");
    }

    void ConfigureLookTouchAreaRaycasts()
    {
        if (lookTouchArea == null) return;

        Image touchImage = lookTouchArea.GetComponent<Image>();
        if (touchImage != null)
        {
            touchImage.raycastTarget = false;
            touchImage.color = enableDebugVisualization ? new Color(0, 1, 0, 0.1f) : new Color(0, 0, 0, 0);
        }

        GraphicRaycaster raycaster = lookTouchArea.GetComponent<GraphicRaycaster>();
        if (raycaster != null)
        {
            raycaster.enabled = false;
        }
    }

    /// <summary>
    /// 设置按钮事件
    /// </summary>
    void SetupButtonEvents()
    {
        // 按钮事件在CreateButton中已经设置
        Debug.Log("[MobileControlsUI] 按钮事件设置完成");
    }

    /// <summary>
    /// 设置触摸事件
    /// </summary>
    void SetupTouchEvents()
    {
        // 为摇杆添加触摸事件
        if (joystickContainer != null)
        {
            EventTrigger joystickTrigger = joystickContainer.GetComponent<EventTrigger>();
            if (joystickTrigger == null)
                joystickTrigger = joystickContainer.AddComponent<EventTrigger>();
            joystickTrigger.triggers.Clear();

            // 摇杆按下事件
            EventTrigger.Entry joystickDownEntry = new EventTrigger.Entry();
            joystickDownEntry.eventID = EventTriggerType.PointerDown;
            joystickDownEntry.callback.AddListener(OnJoystickPointerDown);
            joystickTrigger.triggers.Add(joystickDownEntry);

            // 摇杆拖拽事件
            EventTrigger.Entry joystickDragEntry = new EventTrigger.Entry();
            joystickDragEntry.eventID = EventTriggerType.Drag;
            joystickDragEntry.callback.AddListener(OnJoystickDrag);
            joystickTrigger.triggers.Add(joystickDragEntry);

            // 摇杆释放事件
            EventTrigger.Entry joystickUpEntry = new EventTrigger.Entry();
            joystickUpEntry.eventID = EventTriggerType.PointerUp;
            joystickUpEntry.callback.AddListener(OnJoystickPointerUp);
            joystickTrigger.triggers.Add(joystickUpEntry);

            EventTrigger.Entry joystickExitEntry = new EventTrigger.Entry();
            joystickExitEntry.eventID = EventTriggerType.PointerExit;
            joystickExitEntry.callback.AddListener(OnJoystickPointerUp);
            joystickTrigger.triggers.Add(joystickExitEntry);
        }

        // 为视角触摸区域添加事件
        if (lookTouchArea != null)
        {
            EventTrigger lookTrigger = lookTouchArea.GetComponent<EventTrigger>();
            if (lookTrigger == null)
                lookTrigger = lookTouchArea.gameObject.AddComponent<EventTrigger>();
            lookTrigger.triggers.Clear();

            // 视角触摸开始
            EventTrigger.Entry lookDownEntry = new EventTrigger.Entry();
            lookDownEntry.eventID = EventTriggerType.PointerDown;
            lookDownEntry.callback.AddListener(OnLookTouchDown);
            lookTrigger.triggers.Add(lookDownEntry);

            // 视角触摸拖拽
            EventTrigger.Entry lookDragEntry = new EventTrigger.Entry();
            lookDragEntry.eventID = EventTriggerType.Drag;
            lookDragEntry.callback.AddListener(OnLookTouchDrag);
            lookTrigger.triggers.Add(lookDragEntry);

            // 视角触摸结束
            EventTrigger.Entry lookUpEntry = new EventTrigger.Entry();
            lookUpEntry.eventID = EventTriggerType.PointerUp;
            lookUpEntry.callback.AddListener(OnLookTouchUp);
            lookTrigger.triggers.Add(lookUpEntry);

            EventTrigger.Entry lookExitEntry = new EventTrigger.Entry();
            lookExitEntry.eventID = EventTriggerType.PointerExit;
            lookExitEntry.callback.AddListener(OnLookTouchUp);
            lookTrigger.triggers.Add(lookExitEntry);
        }

        Debug.Log("[MobileControlsUI] 触摸事件设置完成");
    }

    /// <summary>
    /// 设置安全区域适配
    /// </summary>
    void SetupSafeArea()
    {
        if (!adaptToSafeArea) return;

        // 获取安全区域
        Rect safeArea = Screen.safeArea;
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);

        // 计算安全区域边距
        float leftMargin = safeArea.x;
        float rightMargin = screenSize.x - safeArea.xMax;
        float topMargin = screenSize.y - safeArea.yMax;
        float bottomMargin = safeArea.y;

        ApplySafeAreaOffset(GetRectTransform(joystickContainer), leftMargin, rightMargin, topMargin, bottomMargin);
        ApplySafeAreaOffset(GetButtonRect(jumpButton), leftMargin, rightMargin, topMargin, bottomMargin);
        ApplySafeAreaOffset(GetButtonRect(runButton), leftMargin, rightMargin, topMargin, bottomMargin);
        ApplySafeAreaOffset(GetButtonRect(interactButton), leftMargin, rightMargin, topMargin, bottomMargin);
        ApplySafeAreaOffset(GetButtonRect(secondaryInteractButton), leftMargin, rightMargin, topMargin, bottomMargin);
        ApplySafeAreaOffset(GetButtonRect(inventoryButton), leftMargin, rightMargin, topMargin, bottomMargin);
        ApplySafeAreaOffset(GetButtonRect(encyclopediaButton), leftMargin, rightMargin, topMargin, bottomMargin);
        ApplySafeAreaOffset(GetButtonRect(toolWheelButton), leftMargin, rightMargin, topMargin, bottomMargin);
        ApplySafeAreaOffset(GetButtonRect(menuButton), leftMargin, rightMargin, topMargin, bottomMargin);
        ApplySafeAreaOffset(GetRectTransform(droneControlsContainer), leftMargin, rightMargin, topMargin, bottomMargin);

        Debug.Log($"[MobileControlsUI] 安全区域适配完成 - 边距: L{leftMargin} R{rightMargin} T{topMargin} B{bottomMargin}");
    }

    RectTransform GetRectTransform(GameObject target)
    {
        return target != null ? target.GetComponent<RectTransform>() : null;
    }

    RectTransform GetButtonRect(Button button)
    {
        return button != null ? button.GetComponent<RectTransform>() : null;
    }

    public bool ContainsControlAtScreenPoint(Vector2 screenPosition)
    {
        if (!isActiveAndEnabled) return false;

        return IsScreenPointInRect(GetRectTransform(joystickContainer), screenPosition) ||
               IsScreenPointInButton(jumpButton, screenPosition) ||
               IsScreenPointInButton(runButton, screenPosition) ||
               IsScreenPointInButton(interactButton, screenPosition) ||
               IsScreenPointInButton(secondaryInteractButton, screenPosition) ||
               IsScreenPointInButton(inventoryButton, screenPosition) ||
               IsScreenPointInButton(encyclopediaButton, screenPosition) ||
               IsScreenPointInButton(toolWheelButton, screenPosition) ||
               IsScreenPointInButton(menuButton, screenPosition) ||
               IsScreenPointInRect(GetRectTransform(mobileMenuPanel), screenPosition) ||
               IsScreenPointInButton(ascendButton, screenPosition) ||
               IsScreenPointInButton(descendButton, screenPosition);
    }

    bool IsScreenPointInButton(Button button, Vector2 screenPosition)
    {
        if (!IsButtonTouchable(button)) return false;
        return IsScreenPointInRect(GetButtonRect(button), screenPosition);
    }

    bool IsScreenPointInRect(RectTransform rect, Vector2 screenPosition)
    {
        return rect != null &&
               rect.gameObject.activeInHierarchy &&
               RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, null);
    }

    bool IsButtonTouchable(Button button)
    {
        return button != null &&
               button.gameObject.activeInHierarchy &&
               button.enabled &&
               button.interactable;
    }

    void ApplySafeAreaOffset(RectTransform rect, float left, float right, float top, float bottom)
    {
        if (rect == null) return;

        float scaleFactor = canvasScaler != null ? Mathf.Max(canvasScaler.scaleFactor, 0.01f) : 1f;
        Vector2 position = rect.anchoredPosition;

        if (rect.anchorMin.x <= 0.01f && rect.anchorMax.x <= 0.01f)
        {
            position.x += left / scaleFactor;
        }
        else if (rect.anchorMin.x >= 0.99f && rect.anchorMax.x >= 0.99f)
        {
            position.x -= right / scaleFactor;
        }

        if (rect.anchorMin.y <= 0.01f && rect.anchorMax.y <= 0.01f)
        {
            position.y += bottom / scaleFactor;
        }
        else if (rect.anchorMin.y >= 0.99f && rect.anchorMax.y >= 0.99f)
        {
            position.y -= top / scaleFactor;
        }

        rect.anchoredPosition = position;
    }

    void ConfigureGameButton(Button button, Image buttonImage, string buttonName)
    {
        ControlButtonRole role = GetButtonRole(buttonName);

        buttonImage.sprite = CreateCircleSprite(128);
        buttonImage.color = GetButtonBaseColor(role, false);
        buttonImage.type = Image.Type.Simple;
        buttonImage.raycastTarget = true;

        ColorBlock colors = button.colors;
        colors.normalColor = GetButtonBaseColor(role, false);
        colors.highlightedColor = GetButtonBaseColor(role, true);
        colors.pressedColor = GetButtonPressedColor(role);
        colors.selectedColor = GetButtonBaseColor(role, true);
        colors.disabledColor = new Color(0.05f, 0.055f, 0.06f, 0.34f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.06f;
        button.colors = colors;

        CreateChildImage(
            button.transform,
            "ButtonGlow",
            CreateCircleSprite(128),
            GetButtonGlowColor(role),
            new Vector2(0.13f, 0.18f),
            new Vector2(0.87f, 0.92f));

        CreateChildImage(
            button.transform,
            "ButtonRim",
            CreateRingSprite(128, 0.84f, 0.98f),
            GetButtonRingColor(role),
            Vector2.zero,
            Vector2.one);

        CreateChildImage(
            button.transform,
            "Icon",
            CreateIconSprite(GetButtonIcon(buttonName), 96),
            GetButtonIconColor(role),
            new Vector2(0.27f, 0.36f),
            new Vector2(0.73f, 0.83f));
    }

    Image CreateChildImage(Transform parent, string name, Sprite sprite, Color color, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject imageObj = new GameObject(name);
        imageObj.transform.SetParent(parent, false);

        RectTransform rect = imageObj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = imageObj.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.type = Image.Type.Simple;
        image.raycastTarget = false;
        return image;
    }

    void AddTriggerEntry(EventTrigger trigger, EventTriggerType eventID, UnityEngine.Events.UnityAction<BaseEventData> callback)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = eventID;
        entry.callback.AddListener(callback);
        trigger.triggers.Add(entry);
    }

    ControlButtonRole GetButtonRole(string buttonName)
    {
        switch (buttonName)
        {
            case "InteractButton":
            case "SecondaryInteractButton":
                return ControlButtonRole.Interact;
            case "InventoryButton":
            case "EncyclopediaButton":
            case "ToolWheelButton":
            case "MenuButton":
                return ControlButtonRole.Utility;
            case "AscendButton":
            case "DescendButton":
                return ControlButtonRole.Drone;
            default:
                return ControlButtonRole.Action;
        }
    }

    ControlButtonIcon GetButtonIcon(string buttonName)
    {
        switch (buttonName)
        {
            case "RunButton":
                return ControlButtonIcon.Run;
            case "InteractButton":
                return ControlButtonIcon.Interact;
            case "SecondaryInteractButton":
                return ControlButtonIcon.Secondary;
            case "InventoryButton":
                return ControlButtonIcon.Inventory;
            case "EncyclopediaButton":
                return ControlButtonIcon.Encyclopedia;
            case "ToolWheelButton":
                return ControlButtonIcon.Tools;
            case "MenuButton":
                return ControlButtonIcon.Menu;
            case "AscendButton":
                return ControlButtonIcon.Ascend;
            case "DescendButton":
                return ControlButtonIcon.Descend;
            default:
                return ControlButtonIcon.Jump;
        }
    }

    Color GetButtonBaseColor(ControlButtonRole role, bool highlighted)
    {
        Color baseColor;
        switch (role)
        {
            case ControlButtonRole.Interact:
                baseColor = new Color(0.56f, 0.32f, 0.08f, 0.84f);
                break;
            case ControlButtonRole.Utility:
                baseColor = new Color(0.045f, 0.13f, 0.15f, 0.68f);
                break;
            case ControlButtonRole.Drone:
                baseColor = new Color(0.06f, 0.22f, 0.34f, 0.82f);
                break;
            default:
                baseColor = ResolveLegacyColor(buttonNormalColor, LegacyButtonNormalColor, ModernButtonNormalColor);
                break;
        }

        return highlighted ? Color.Lerp(baseColor, GetButtonPressedColor(role), 0.35f) : baseColor;
    }

    Color GetButtonPressedColor(ControlButtonRole role)
    {
        switch (role)
        {
            case ControlButtonRole.Interact:
                return new Color(1f, 0.64f, 0.18f, 0.96f);
            case ControlButtonRole.Utility:
                return new Color(0.12f, 0.45f, 0.48f, 0.86f);
            case ControlButtonRole.Drone:
                return new Color(0.16f, 0.62f, 0.96f, 0.96f);
            default:
                return ResolveLegacyColor(buttonPressedColor, LegacyButtonPressedColor, ModernButtonPressedColor);
        }
    }

    Color GetButtonRingColor(ControlButtonRole role)
    {
        switch (role)
        {
            case ControlButtonRole.Interact:
                return new Color(1f, 0.72f, 0.32f, 0.78f);
            case ControlButtonRole.Utility:
                return new Color(0.58f, 1f, 0.9f, 0.48f);
            case ControlButtonRole.Drone:
                return new Color(0.48f, 0.86f, 1f, 0.72f);
            default:
                return new Color(0.9f, 0.97f, 1f, 0.56f);
        }
    }

    Color GetButtonGlowColor(ControlButtonRole role)
    {
        switch (role)
        {
            case ControlButtonRole.Interact:
                return new Color(1f, 0.75f, 0.28f, 0.13f);
            case ControlButtonRole.Utility:
                return new Color(0.42f, 1f, 0.9f, 0.09f);
            case ControlButtonRole.Drone:
                return new Color(0.34f, 0.78f, 1f, 0.13f);
            default:
                return new Color(1f, 1f, 1f, 0.08f);
        }
    }

    Color GetButtonIconColor(ControlButtonRole role)
    {
        switch (role)
        {
            case ControlButtonRole.Interact:
                return new Color(1f, 0.92f, 0.72f, 0.96f);
            case ControlButtonRole.Drone:
                return new Color(0.82f, 0.96f, 1f, 0.96f);
            default:
                return new Color(0.96f, 0.99f, 1f, 0.94f);
        }
    }

    Color GetButtonLabelColor(ControlButtonRole role)
    {
        switch (role)
        {
            case ControlButtonRole.Interact:
                return new Color(1f, 0.88f, 0.66f, 0.96f);
            case ControlButtonRole.Drone:
                return new Color(0.78f, 0.95f, 1f, 0.96f);
            default:
                return new Color(1f, 1f, 1f, 0.86f);
        }
    }

    Color GetJoystickBackgroundColor(bool active)
    {
        Color color = ResolveLegacyColor(joystickBackgroundColor, LegacyJoystickBackgroundColor, ModernJoystickBackgroundColor);
        return SetAlpha(color, Mathf.Clamp01(color.a + (active ? 0.16f : 0f)));
    }

    Color GetJoystickHandleColor(bool active)
    {
        Color color = ResolveLegacyColor(joystickHandleColor, LegacyJoystickHandleColor, ModernJoystickHandleColor);
        return SetAlpha(color, Mathf.Clamp01(color.a + (active ? 0.12f : 0f)));
    }

    Color ResolveLegacyColor(Color configuredColor, Color legacyColor, Color modernColor)
    {
        return AreColorsClose(configuredColor, legacyColor) ? modernColor : configuredColor;
    }

    bool AreColorsClose(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < 0.005f &&
               Mathf.Abs(a.g - b.g) < 0.005f &&
               Mathf.Abs(a.b - b.b) < 0.005f &&
               Mathf.Abs(a.a - b.a) < 0.005f;
    }

    Color SetAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    void SetButtonVisualPressed(Button button, bool pressed)
    {
        if (button == null) return;

        ControlButtonRole role = GetButtonRole(button.gameObject.name);
        button.transform.localScale = pressed ? Vector3.one * ButtonPressedScale : Vector3.one;

        Graphic targetGraphic = button.targetGraphic;
        if (targetGraphic != null)
        {
            targetGraphic.color = pressed ? GetButtonPressedColor(role) : GetButtonBaseColor(role, false);
        }
    }

    void ResetButtonVisuals()
    {
        SetButtonVisualPressed(jumpButton, false);
        SetButtonVisualPressed(runButton, false);
        SetButtonVisualPressed(interactButton, false);
        SetButtonVisualPressed(secondaryInteractButton, false);
        SetButtonVisualPressed(inventoryButton, false);
        SetButtonVisualPressed(encyclopediaButton, false);
        SetButtonVisualPressed(toolWheelButton, false);
        SetButtonVisualPressed(menuButton, false);
        SetButtonVisualPressed(ascendButton, false);
        SetButtonVisualPressed(descendButton, false);
    }

    void SetJoystickVisualActive(bool active)
    {
        if (joystickBackground != null)
        {
            Image backgroundImage = joystickBackground.GetComponent<Image>();
            if (backgroundImage != null)
            {
                backgroundImage.color = GetJoystickBackgroundColor(active);
            }
        }

        if (joystickHandle != null)
        {
            Image handleImage = joystickHandle.GetComponent<Image>();
            if (handleImage != null)
            {
                handleImage.color = GetJoystickHandleColor(active);
            }
        }
    }

    void ConfigureButtonText(Text buttonText, string text)
    {
        RectTransform textRect = buttonText.rectTransform;
        textRect.anchorMin = new Vector2(0.08f, 0.07f);
        textRect.anchorMax = new Vector2(0.92f, 0.31f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        string buttonName = buttonText.transform.parent != null ? buttonText.transform.parent.name : string.Empty;
        ControlButtonRole role = GetButtonRole(buttonName);

        buttonText.text = text;
        buttonText.font = UIFontResolver.GetUIFont();
        buttonText.fontSize = Mathf.RoundToInt(buttonSize * (text.Length <= 2 ? 0.16f : 0.13f));
        buttonText.resizeTextForBestFit = true;
        buttonText.resizeTextMinSize = 9;
        buttonText.resizeTextMaxSize = Mathf.RoundToInt(buttonSize * 0.17f);
        buttonText.color = GetButtonLabelColor(role);
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.horizontalOverflow = HorizontalWrapMode.Wrap;
        buttonText.verticalOverflow = VerticalWrapMode.Truncate;
        buttonText.raycastTarget = false;
    }

    void SubscribeLanguageChanges()
    {
        if (isLanguageChangeSubscribed) return;

        LocalizationManager localizationManager = LocalizationManager.Instance;
        if (localizationManager != null)
        {
            localizationManager.OnLanguageChanged += UpdateLocalizedButtonLabels;
            isLanguageChangeSubscribed = true;
        }
    }

    void UnsubscribeLanguageChanges()
    {
        if (!isLanguageChangeSubscribed) return;

        LocalizationManager localizationManager = FindFirstObjectByType<LocalizationManager>();
        if (localizationManager != null)
        {
            localizationManager.OnLanguageChanged -= UpdateLocalizedButtonLabels;
        }

        isLanguageChangeSubscribed = false;
    }

    void UpdateLocalizedButtonLabels()
    {
        LanguageSettings.Language language = LocalizationManager.Instance != null
            ? LocalizationManager.Instance.CurrentLanguage
            : LanguageSettings.DefaultLanguage;

        UpdateButtonLabel(jumpButton, GetControlLabel(language, "jump"));
        UpdateButtonLabel(runButton, GetControlLabel(language, "run"));
        UpdateButtonLabel(interactButton, GetControlLabel(language, "interact"));
        UpdateButtonLabel(secondaryInteractButton, GetControlLabel(language, "secondary"));
        UpdateButtonLabel(inventoryButton, GetControlLabel(language, "inventory"));
        UpdateButtonLabel(encyclopediaButton, GetControlLabel(language, "encyclopedia"));
        UpdateButtonLabel(toolWheelButton, GetControlLabel(language, "tools"));
        UpdateButtonLabel(menuButton, GetControlLabel(language, "menu"));
        UpdateButtonLabel(ascendButton, GetControlLabel(language, "ascend"));
        UpdateButtonLabel(descendButton, GetControlLabel(language, "descend"));
    }

    void UpdateButtonLabel(Button button, string text)
    {
        if (button == null) return;

        Text buttonText = button.GetComponentInChildren<Text>(true);
        if (buttonText != null)
        {
            ConfigureButtonText(buttonText, text);
        }
    }

    string GetControlLabel(LanguageSettings.Language language, string key)
    {
        string fallback;
        switch (language)
        {
            case LanguageSettings.Language.ChineseSimplified:
                fallback = key switch
                {
                    "jump" => "跳跃",
                    "run" => "奔跑",
                    "interact" => "交互",
                    "secondary" => "次操作",
                    "inventory" => "背包",
                    "encyclopedia" => "图鉴",
                    "tools" => "工具",
                    "menu" => "菜单",
                    "ascend" => "上升",
                    "descend" => "下降",
                    _ => key
                };
                break;

            case LanguageSettings.Language.English:
                fallback = key switch
                {
                    "jump" => "Jump",
                    "run" => "Run",
                    "interact" => "Use",
                    "secondary" => "Alt",
                    "inventory" => "Bag",
                    "encyclopedia" => "Guide",
                    "tools" => "Tools",
                    "menu" => "Menu",
                    "ascend" => "Up",
                    "descend" => "Down",
                    _ => key
                };
                break;

            case LanguageSettings.Language.Japanese:
            default:
                fallback = key switch
                {
                    "jump" => "ジャンプ",
                    "run" => "走る",
                    "interact" => "調べる",
                    "secondary" => "使う",
                    "inventory" => "バッグ",
                    "encyclopedia" => "図鑑",
                    "tools" => "道具",
                    "menu" => "メニュー",
                    "ascend" => "上昇",
                    "descend" => "下降",
                    _ => key
                };
                break;
        }

        string localizationKey = $"ui.mobile_controls.{key}";
        LocalizationManager localizationManager = LocalizationManager.Instance;
        return localizationManager != null && localizationManager.IsInitialized
            ? localizationManager.GetTextOrFallback(localizationKey, fallback)
            : fallback;
    }

    void EnsureEventSystemForTouch()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            eventSystem = FindFirstObjectByType<EventSystem>();
        }

        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
        }

        InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
        if (inputModule == null)
        {
            inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
            Debug.Log("[MobileControlsUI] 已为EventSystem添加InputSystemUIInputModule");
        }
        else if (inputModule.actionsAsset == null)
        {
            inputModule.AssignDefaultActions();
            Debug.Log("[MobileControlsUI] 已为InputSystemUIInputModule分配默认UI actions");
        }

        StandaloneInputModule standaloneInputModule = eventSystem.GetComponent<StandaloneInputModule>();
        if (standaloneInputModule != null && standaloneInputModule.enabled)
        {
            standaloneInputModule.enabled = false;
            Debug.Log("[MobileControlsUI] 已禁用旧StandaloneInputModule，使用InputSystemUIInputModule处理触控UI");
        }
    }

    void ResetControlState()
    {
        isJoystickActive = false;
        joystickInput = Vector2.zero;
        joystickPointerId = -1;
        rawJoystickTouchId = NoTouchId;
        isLookTouchActive = false;
        lookTouchPointerId = -1;
        isRunPressed = false;
        rawJumpTouchId = NoTouchId;
        rawRunTouchId = NoTouchId;
        rawInteractTouchId = NoTouchId;
        rawSecondaryInteractTouchId = NoTouchId;
        rawInventoryTouchId = NoTouchId;
        rawEncyclopediaTouchId = NoTouchId;
        rawToolWheelTouchId = NoTouchId;
        rawMenuTouchId = NoTouchId;
        rawResumeMenuTouchId = NoTouchId;
        rawSettingsMenuTouchId = NoTouchId;
        rawQuitMenuTouchId = NoTouchId;
        rawAscendTouchId = NoTouchId;
        rawDescendTouchId = NoTouchId;
        hasPublishedJoystickInput = false;
        ResetButtonVisuals();

        if (joystickHandle != null)
        {
            joystickHandle.anchoredPosition = Vector2.zero;
        }
        SetJoystickVisualActive(false);

        if (inputManager == null)
        {
            inputManager = MobileInputManager.Instance;
        }

        if (inputManager != null)
        {
            inputManager.SetMoveInput(Vector2.zero);
            inputManager.SetLookInput(Vector2.zero);
            inputManager.SetJumpInput(false);
            inputManager.SetRunInput(false);
            inputManager.SetInteractInput(false);
            inputManager.SetSecondaryInteractInput(false);
            inputManager.SetAscendInput(false);
            inputManager.SetDescendInput(false);
        }
    }

    /// <summary>
    /// 创建圆形Sprite
    /// </summary>
    Sprite CreateCircleSprite(int size = 128)
    {
        Color[] colors = new Color[size * size];

        Vector2 center = new Vector2((size - 1) / 2f, (size - 1) / 2f);
        float radius = size / 2f - 2f; // 留一点边距

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 point = new Vector2(x, y);
                float distance = Vector2.Distance(point, center);

                if (distance <= radius)
                {
                    float edgeFade = Mathf.Clamp01((radius - distance) / 2f);
                    float alpha = Mathf.Lerp(0.98f, 0.74f, distance / radius) * edgeFade;
                    colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
                else
                {
                    // 在圆形外，设置为透明
                    colors[y * size + x] = Color.clear;
                }
            }
        }

        return CreateSpriteFromColors(colors, size);
    }

    Sprite CreateRingSprite(int size, float innerRadiusRatio, float outerRadiusRatio)
    {
        Color[] colors = new Color[size * size];
        Vector2 center = new Vector2((size - 1) / 2f, (size - 1) / 2f);
        float radius = size / 2f - 2f;
        float innerRadius = radius * Mathf.Clamp01(innerRadiusRatio);
        float outerRadius = radius * Mathf.Clamp01(outerRadiusRatio);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance >= innerRadius && distance <= outerRadius)
                {
                    float outerFade = Mathf.Clamp01((outerRadius - distance) / 2f);
                    float innerFade = Mathf.Clamp01((distance - innerRadius) / 2f);
                    float alpha = Mathf.Min(outerFade, innerFade);
                    colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
                else
                {
                    colors[y * size + x] = Color.clear;
                }
            }
        }

        return CreateSpriteFromColors(colors, size);
    }

    Sprite CreateIconSprite(ControlButtonIcon icon, int size)
    {
        Color[] colors = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 point = new Vector2((x + 0.5f) / size, (y + 0.5f) / size);
                colors[y * size + x] = IsIconPixel(icon, point) ? Color.white : Color.clear;
            }
        }

        return CreateSpriteFromColors(colors, size);
    }

    Sprite CreateSpriteFromColors(Color[] colors, int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(colors);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    bool IsIconPixel(ControlButtonIcon icon, Vector2 point)
    {
        switch (icon)
        {
            case ControlButtonIcon.Run:
                return IsChevron(point, 0.24f) || IsChevron(point, 0.54f);
            case ControlButtonIcon.Interact:
                return IsTapIcon(point);
            case ControlButtonIcon.Secondary:
                return IsCircle(point, new Vector2(0.29f, 0.5f), 0.075f) ||
                       IsCircle(point, new Vector2(0.5f, 0.5f), 0.075f) ||
                       IsCircle(point, new Vector2(0.71f, 0.5f), 0.075f);
            case ControlButtonIcon.Inventory:
                return IsInventoryIcon(point);
            case ControlButtonIcon.Encyclopedia:
                return IsBookIcon(point);
            case ControlButtonIcon.Tools:
                return IsToolIcon(point);
            case ControlButtonIcon.Menu:
                return IsMenuIcon(point);
            case ControlButtonIcon.Descend:
                return IsArrowIcon(point, false);
            case ControlButtonIcon.Ascend:
            case ControlButtonIcon.Jump:
            default:
                return IsArrowIcon(point, true);
        }
    }

    bool IsArrowIcon(Vector2 point, bool up)
    {
        Vector2 p = up ? point : new Vector2(point.x, 1f - point.y);
        return IsTriangle(p, new Vector2(0.5f, 0.84f), new Vector2(0.24f, 0.55f), new Vector2(0.76f, 0.55f)) ||
               IsRect(p, 0.43f, 0.21f, 0.57f, 0.61f);
    }

    bool IsChevron(Vector2 point, float xOffset)
    {
        return IsLine(point, new Vector2(xOffset, 0.26f), new Vector2(xOffset + 0.24f, 0.5f), 0.055f) ||
               IsLine(point, new Vector2(xOffset + 0.24f, 0.5f), new Vector2(xOffset, 0.74f), 0.055f);
    }

    bool IsTapIcon(Vector2 point)
    {
        return IsRing(point, new Vector2(0.5f, 0.74f), 0.22f, 0.18f) ||
               IsRect(point, 0.43f, 0.28f, 0.57f, 0.72f) ||
               IsCircle(point, new Vector2(0.5f, 0.72f), 0.085f) ||
               IsLine(point, new Vector2(0.35f, 0.26f), new Vector2(0.65f, 0.26f), 0.055f);
    }

    bool IsInventoryIcon(Vector2 point)
    {
        return IsRect(point, 0.25f, 0.25f, 0.75f, 0.65f) ||
               IsLine(point, new Vector2(0.36f, 0.64f), new Vector2(0.36f, 0.77f), 0.04f) ||
               IsLine(point, new Vector2(0.64f, 0.64f), new Vector2(0.64f, 0.77f), 0.04f) ||
               IsLine(point, new Vector2(0.36f, 0.77f), new Vector2(0.64f, 0.77f), 0.04f);
    }

    bool IsBookIcon(Vector2 point)
    {
        return IsRectBorder(point, 0.18f, 0.23f, 0.48f, 0.76f, 0.045f) ||
               IsRectBorder(point, 0.52f, 0.23f, 0.82f, 0.76f, 0.045f) ||
               IsLine(point, new Vector2(0.5f, 0.22f), new Vector2(0.5f, 0.79f), 0.025f) ||
               IsLine(point, new Vector2(0.27f, 0.61f), new Vector2(0.41f, 0.61f), 0.018f) ||
               IsLine(point, new Vector2(0.59f, 0.61f), new Vector2(0.73f, 0.61f), 0.018f);
    }

    bool IsToolIcon(Vector2 point)
    {
        return IsLine(point, new Vector2(0.3f, 0.25f), new Vector2(0.7f, 0.65f), 0.065f) ||
               IsRing(point, new Vector2(0.74f, 0.7f), 0.14f, 0.09f) ||
               IsLine(point, new Vector2(0.67f, 0.79f), new Vector2(0.84f, 0.62f), 0.04f) ||
               IsCircle(point, new Vector2(0.28f, 0.23f), 0.065f);
    }

    bool IsMenuIcon(Vector2 point)
    {
        return IsLine(point, new Vector2(0.25f, 0.68f), new Vector2(0.75f, 0.68f), 0.05f) ||
               IsLine(point, new Vector2(0.25f, 0.50f), new Vector2(0.75f, 0.50f), 0.05f) ||
               IsLine(point, new Vector2(0.25f, 0.32f), new Vector2(0.75f, 0.32f), 0.05f);
    }

    bool IsRect(Vector2 point, float minX, float minY, float maxX, float maxY)
    {
        return point.x >= minX && point.x <= maxX && point.y >= minY && point.y <= maxY;
    }

    bool IsRectBorder(Vector2 point, float minX, float minY, float maxX, float maxY, float width)
    {
        if (!IsRect(point, minX, minY, maxX, maxY)) return false;

        return point.x <= minX + width ||
               point.x >= maxX - width ||
               point.y <= minY + width ||
               point.y >= maxY - width;
    }

    bool IsCircle(Vector2 point, Vector2 center, float radius)
    {
        return Vector2.Distance(point, center) <= radius;
    }

    bool IsRing(Vector2 point, Vector2 center, float outerRadius, float innerRadius)
    {
        float distance = Vector2.Distance(point, center);
        return distance <= outerRadius && distance >= innerRadius;
    }

    bool IsLine(Vector2 point, Vector2 start, Vector2 end, float width)
    {
        return DistanceToSegment(point, start, end) <= width;
    }

    float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 segment = end - start;
        float segmentLength = segment.sqrMagnitude;
        if (segmentLength <= 0.0001f)
        {
            return Vector2.Distance(point, start);
        }

        float t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / segmentLength);
        return Vector2.Distance(point, start + segment * t);
    }

    bool IsTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Sign(point, a, b);
        float d2 = Sign(point, b, c);
        float d3 = Sign(point, c, a);

        bool hasNegative = d1 < 0 || d2 < 0 || d3 < 0;
        bool hasPositive = d1 > 0 || d2 > 0 || d3 > 0;
        return !(hasNegative && hasPositive);
    }

    float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
    }

    #region 摇杆事件处理

    void OnJoystickPointerDown(BaseEventData eventData)
    {
        PointerEventData pointerData = eventData as PointerEventData;
        if (pointerData != null)
        {
            isJoystickActive = true;
            joystickPointerId = pointerData.pointerId;
            joystickStartPosition = pointerData.position;
            SetJoystickVisualActive(true);
            UpdateJoystickFromPointer(pointerData);

            Debug.Log("[MobileControlsUI] 摇杆激活");
        }
    }

    void OnJoystickDrag(BaseEventData eventData)
    {
        if (!isJoystickActive) return;

        PointerEventData pointerData = eventData as PointerEventData;
        if (pointerData != null && pointerData.pointerId == joystickPointerId)
        {
            UpdateJoystickFromPointer(pointerData);
        }
    }

    void UpdateJoystickFromPointer(PointerEventData pointerData)
    {
        if (pointerData == null)
        {
            joystickInput = Vector2.zero;
            return;
        }

        UpdateJoystickFromScreenPoint(pointerData.position, pointerData.pressEventCamera);
    }

    void UpdateJoystickFromScreenPoint(Vector2 screenPosition, Camera eventCamera = null)
    {
        RectTransform referenceRect = joystickBackground != null ? joystickBackground : GetRectTransform(joystickContainer);
        if (referenceRect == null || joystickRange <= 0f)
        {
            joystickInput = Vector2.zero;
            return;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(referenceRect, screenPosition, eventCamera, out Vector2 localPoint))
        {
            return;
        }

        Vector2 clampedPoint = Vector2.ClampMagnitude(localPoint, joystickRange);
        joystickInput = clampedPoint / joystickRange;

        if (joystickHandle != null)
        {
            joystickHandle.anchoredPosition = clampedPoint;
        }
    }

    void OnJoystickPointerUp(BaseEventData eventData)
    {
        PointerEventData pointerData = eventData as PointerEventData;
        if (pointerData != null && pointerData.pointerId == joystickPointerId)
        {
            isJoystickActive = false;
            joystickInput = Vector2.zero;
            joystickPointerId = -1;
            SetJoystickVisualActive(false);

            // 重置手柄位置
            if (joystickHandle != null)
            {
                joystickHandle.anchoredPosition = Vector2.zero;
            }

            Debug.Log("[MobileControlsUI] 摇杆释放");
        }
    }

    void ProcessJoystickInput()
    {
        // 在Update中已处理，这里为备用
    }

    void PublishJoystickInput()
    {
        if (inputManager == null)
        {
            return;
        }

        bool hasActiveJoystickTouch = isJoystickActive || rawJoystickTouchId != NoTouchId || joystickPointerId != -1;
        bool hasJoystickValue = joystickInput.sqrMagnitude > 0.0001f;
        if (!hasActiveJoystickTouch && !hasJoystickValue && !hasPublishedJoystickInput)
        {
            return;
        }

        inputManager.SetMoveInput(joystickInput);
        hasPublishedJoystickInput = hasActiveJoystickTouch || hasJoystickValue;
    }

    /// <summary>
    /// 处理鼠标输入（桌面测试模式）
    /// </summary>
    void ProcessMouseInput()
    {
        // Unity的EventTrigger系统已经自动处理鼠标和触摸输入
        // 这个方法用于额外的鼠标特定逻辑（如果需要）

        if (enableDebugVisualization)
        {
            // 可以在这里添加鼠标位置调试信息
        }
    }

    void ProcessRawTouchFallbackInput()
    {
        if (!enableRawTouchFallback || Touchscreen.current == null)
        {
            return;
        }

        activeRawTouchIds.Clear();
        var touches = Touchscreen.current.touches;
        for (int i = 0; i < touches.Count; i++)
        {
            var touch = touches[i];
            TouchPhase phase = touch.phase.ReadValue();
            if (phase == TouchPhase.None)
            {
                continue;
            }

            int touchId = touch.touchId.ReadValue();
            Vector2 position = touch.position.ReadValue();
            bool isActive = IsActiveTouchPhase(phase);
            if (isActive)
            {
                activeRawTouchIds.Add(touchId);
            }

            if (isMobileMenuOpen)
            {
                ProcessRawClickButtonTouch(resumeMenuButton, ref rawResumeMenuTouchId, touchId, position, phase, CloseMobileMenu);
                ProcessRawClickButtonTouch(settingsMenuButton, ref rawSettingsMenuTouchId, touchId, position, phase, OpenSettingsFromMobileMenu);
                ProcessRawClickButtonTouch(quitMenuButton, ref rawQuitMenuTouchId, touchId, position, phase, QuitGameFromMobileMenu);
                continue;
            }

            ProcessRawJoystickTouch(touchId, position, phase);
            ProcessRawHoldButtonTouch(jumpButton, ref rawJumpTouchId, touchId, position, phase, OnJumpButtonDown, OnJumpButtonUp);
            ProcessRawHoldButtonTouch(runButton, ref rawRunTouchId, touchId, position, phase, OnRunButtonDown, OnRunButtonUp);
            ProcessRawHoldButtonTouch(interactButton, ref rawInteractTouchId, touchId, position, phase, OnInteractButtonDown, OnInteractButtonUp);
            ProcessRawHoldButtonTouch(secondaryInteractButton, ref rawSecondaryInteractTouchId, touchId, position, phase, OnSecondaryInteractButtonDown, OnSecondaryInteractButtonUp);
            ProcessRawHoldButtonTouch(ascendButton, ref rawAscendTouchId, touchId, position, phase, OnAscendButtonDown, OnAscendButtonUp);
            ProcessRawHoldButtonTouch(descendButton, ref rawDescendTouchId, touchId, position, phase, OnDescendButtonDown, OnDescendButtonUp);

            ProcessRawClickButtonTouch(inventoryButton, ref rawInventoryTouchId, touchId, position, phase, OnInventoryButtonClick);
            ProcessRawClickButtonTouch(encyclopediaButton, ref rawEncyclopediaTouchId, touchId, position, phase, OnEncyclopediaButtonClick);
            ProcessRawClickButtonTouch(toolWheelButton, ref rawToolWheelTouchId, touchId, position, phase, OnToolWheelButtonClick);
            ProcessRawClickButtonTouch(menuButton, ref rawMenuTouchId, touchId, position, phase, OnMenuButtonClick);
        }

        ReleaseMissingRawTouches(activeRawTouchIds);
    }

    void ProcessRawJoystickTouch(int touchId, Vector2 position, TouchPhase phase)
    {
        if (rawJoystickTouchId == touchId)
        {
            if (!IsActiveTouchPhase(phase))
            {
                ReleaseRawJoystickTouch();
                return;
            }

            isJoystickActive = true;
            SetJoystickVisualActive(true);
            UpdateJoystickFromScreenPoint(position);
            return;
        }

        if (rawJoystickTouchId == NoTouchId &&
            phase == TouchPhase.Began &&
            IsScreenPointInRect(GetRectTransform(joystickContainer), position))
        {
            rawJoystickTouchId = touchId;
            isJoystickActive = true;
            SetJoystickVisualActive(true);
            UpdateJoystickFromScreenPoint(position);
        }
    }

    void ProcessRawHoldButtonTouch(Button button, ref int trackedTouchId, int touchId, Vector2 position, TouchPhase phase, System.Action onDown, System.Action onUp)
    {
        if (!IsButtonTouchable(button))
        {
            if (trackedTouchId != NoTouchId)
            {
                trackedTouchId = NoTouchId;
                SetButtonVisualPressed(button, false);
                onUp?.Invoke();
            }
            return;
        }

        bool contains = IsScreenPointInButton(button, position);
        if (trackedTouchId == touchId)
        {
            if (!IsActiveTouchPhase(phase) || !contains)
            {
                trackedTouchId = NoTouchId;
                SetButtonVisualPressed(button, false);
                onUp?.Invoke();
            }
            return;
        }

        if (trackedTouchId == NoTouchId && phase == TouchPhase.Began && contains)
        {
            trackedTouchId = touchId;
            SetButtonVisualPressed(button, true);
            onDown?.Invoke();
        }
    }

    void ProcessRawClickButtonTouch(Button button, ref int trackedTouchId, int touchId, Vector2 position, TouchPhase phase, System.Action onClick)
    {
        if (!IsButtonTouchable(button))
        {
            SetButtonVisualPressed(button, false);
            trackedTouchId = NoTouchId;
            return;
        }

        bool contains = IsScreenPointInButton(button, position);
        if (trackedTouchId == touchId)
        {
            if (phase == TouchPhase.Ended)
            {
                trackedTouchId = NoTouchId;
                SetButtonVisualPressed(button, false);
                if (contains)
                {
                    onClick?.Invoke();
                }
            }
            else if (phase == TouchPhase.Canceled || !contains)
            {
                trackedTouchId = NoTouchId;
                SetButtonVisualPressed(button, false);
            }
            return;
        }

        if (trackedTouchId == NoTouchId && phase == TouchPhase.Began && contains)
        {
            trackedTouchId = touchId;
            SetButtonVisualPressed(button, true);
        }
    }

    void ReleaseMissingRawTouches(HashSet<int> activeTouchIds)
    {
        if (rawJoystickTouchId != NoTouchId && !activeTouchIds.Contains(rawJoystickTouchId))
        {
            ReleaseRawJoystickTouch();
        }

        ReleaseMissingRawHoldTouch(ref rawJumpTouchId, activeTouchIds, OnJumpButtonUp);
        ReleaseMissingRawHoldTouch(ref rawRunTouchId, activeTouchIds, OnRunButtonUp);
        ReleaseMissingRawHoldTouch(ref rawInteractTouchId, activeTouchIds, OnInteractButtonUp);
        ReleaseMissingRawHoldTouch(ref rawSecondaryInteractTouchId, activeTouchIds, OnSecondaryInteractButtonUp);
        ReleaseMissingRawHoldTouch(ref rawAscendTouchId, activeTouchIds, OnAscendButtonUp);
        ReleaseMissingRawHoldTouch(ref rawDescendTouchId, activeTouchIds, OnDescendButtonUp);

        ReleaseMissingRawClickTouch(inventoryButton, ref rawInventoryTouchId, activeTouchIds);
        ReleaseMissingRawClickTouch(encyclopediaButton, ref rawEncyclopediaTouchId, activeTouchIds);
        ReleaseMissingRawClickTouch(toolWheelButton, ref rawToolWheelTouchId, activeTouchIds);
        ReleaseMissingRawClickTouch(menuButton, ref rawMenuTouchId, activeTouchIds);
        ReleaseMissingRawClickTouch(resumeMenuButton, ref rawResumeMenuTouchId, activeTouchIds);
        ReleaseMissingRawClickTouch(settingsMenuButton, ref rawSettingsMenuTouchId, activeTouchIds);
        ReleaseMissingRawClickTouch(quitMenuButton, ref rawQuitMenuTouchId, activeTouchIds);
    }

    void ReleaseMissingRawHoldTouch(ref int trackedTouchId, HashSet<int> activeTouchIds, System.Action onUp)
    {
        if (trackedTouchId != NoTouchId && !activeTouchIds.Contains(trackedTouchId))
        {
            trackedTouchId = NoTouchId;
            onUp?.Invoke();
        }
    }

    void ReleaseMissingRawClickTouch(Button button, ref int trackedTouchId, HashSet<int> activeTouchIds)
    {
        if (trackedTouchId != NoTouchId && !activeTouchIds.Contains(trackedTouchId))
        {
            trackedTouchId = NoTouchId;
            SetButtonVisualPressed(button, false);
        }
    }

    void ReleaseRawJoystickTouch()
    {
        rawJoystickTouchId = NoTouchId;
        isJoystickActive = false;
        joystickInput = Vector2.zero;
        SetJoystickVisualActive(false);

        if (joystickHandle != null)
        {
            joystickHandle.anchoredPosition = Vector2.zero;
        }
    }

    bool IsActiveTouchPhase(TouchPhase phase)
    {
        return phase == TouchPhase.Began ||
               phase == TouchPhase.Moved ||
               phase == TouchPhase.Stationary;
    }

    #endregion

    #region 视角触摸事件处理

    void OnLookTouchDown(BaseEventData eventData)
    {
        PointerEventData pointerData = eventData as PointerEventData;
        if (pointerData != null)
        {
            isLookTouchActive = true;
            lookTouchPointerId = pointerData.pointerId;
            lastLookTouchPosition = pointerData.position;
        }
    }

    void OnLookTouchDrag(BaseEventData eventData)
    {
        if (!isLookTouchActive) return;

        PointerEventData pointerData = eventData as PointerEventData;
        if (pointerData != null && pointerData.pointerId == lookTouchPointerId)
        {
            Vector2 delta = pointerData.position - lastLookTouchPosition;
            lastLookTouchPosition = pointerData.position;

            // 发送视角输入给输入管理器
            if (inputManager != null)
            {
                inputManager.SetLookInput(delta);
            }
        }
    }

    void OnLookTouchUp(BaseEventData eventData)
    {
        PointerEventData pointerData = eventData as PointerEventData;
        if (pointerData != null && pointerData.pointerId == lookTouchPointerId)
        {
            isLookTouchActive = false;
            lookTouchPointerId = -1;

            // 停止视角输入
            if (inputManager != null)
            {
                inputManager.SetLookInput(Vector2.zero);
            }
        }
    }

    #endregion

    #region 按钮事件处理

    void OnJumpButtonDown()
    {
        SetButtonVisualPressed(jumpButton, true);
        if (inputManager != null)
        {
            inputManager.SetJumpInput(true);
        }
        Debug.Log("[MobileControlsUI] 跳跃按钮按下");
    }

    void OnJumpButtonUp()
    {
        SetButtonVisualPressed(jumpButton, false);
        if (inputManager != null)
        {
            inputManager.SetJumpInput(false);
        }
        Debug.Log("[MobileControlsUI] 跳跃按钮释放");
    }

    void OnRunButtonDown()
    {
        SetButtonVisualPressed(runButton, true);
        isRunPressed = true;
        if (inputManager != null)
        {
            inputManager.SetRunInput(true);
        }
        Debug.Log("[MobileControlsUI] 奔跑按钮按下");
    }

    void OnRunButtonUp()
    {
        SetButtonVisualPressed(runButton, false);
        isRunPressed = false;
        if (inputManager != null)
        {
            inputManager.SetRunInput(false);
        }
        Debug.Log("[MobileControlsUI] 奔跑按钮释放");
    }

    void OnInteractButtonDown()
    {
        SetButtonVisualPressed(interactButton, true);
        if (inputManager != null)
        {
            inputManager.SetInteractInput(true);
        }
        Debug.Log("[MobileControlsUI] 交互按钮按下");
    }

    void OnInteractButtonUp()
    {
        SetButtonVisualPressed(interactButton, false);
        if (inputManager != null)
        {
            inputManager.SetInteractInput(false);
        }
        Debug.Log("[MobileControlsUI] 交互按钮释放");
    }

    void OnSecondaryInteractButtonDown()
    {
        SetButtonVisualPressed(secondaryInteractButton, true);
        if (inputManager != null)
        {
            inputManager.SetSecondaryInteractInput(true);
        }
        Debug.Log("[MobileControlsUI] F键交互按钮按下");
    }

    void OnSecondaryInteractButtonUp()
    {
        SetButtonVisualPressed(secondaryInteractButton, false);
        if (inputManager != null)
        {
            inputManager.SetSecondaryInteractInput(false);
        }
        Debug.Log("[MobileControlsUI] F键交互按钮释放");
    }

    void OnInventoryButtonClick()
    {
        if (inputManager != null)
        {
            inputManager.TriggerInventoryInput();
        }
        Debug.Log("[MobileControlsUI] 背包按钮点击");
    }

    void OnEncyclopediaButtonClick()
    {
        if (inputManager != null)
        {
            inputManager.TriggerEncyclopediaInput();
        }
        Debug.Log("[MobileControlsUI] 图鉴按钮点击");
    }

    // void OnWarehouseButtonClick() - 仓库按钮已移除
    // {
    //     if (inputManager != null)
    //     {
    //         inputManager.TriggerWarehouseInput();
    //     }
    //     Debug.Log("[MobileControlsUI] 仓库按钮点击");
    // }

    void OnToolWheelButtonClick()
    {
        Debug.Log("[MobileControlsUI] 工具轮盘按钮被点击！");
        if (inputManager != null)
        {
            inputManager.TriggerToolWheelInput();
            Debug.Log("[MobileControlsUI] 工具轮盘输入已触发");
        }
        else
        {
            Debug.LogError("[MobileControlsUI] inputManager为null，无法触发工具轮盘");
        }
    }

    void OnMenuButtonClick()
    {
        if (isMobileMenuOpen)
        {
            CloseMobileMenu();
        }
        else
        {
            OpenMobileMenu();
        }
    }

    void OpenMobileMenu()
    {
        if (isMobileMenuOpen)
        {
            return;
        }

        if (mobileMenuPanel == null)
        {
            CreateMobilePauseMenu();
        }

        ResetControlState();
        mobileMenuOriginalTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        mobileMenuCanvasScope = ModalCanvasLayerGuard.Activate(controlsCanvas);

        pausedPlayerController = FindFirstObjectByType<FirstPersonController>();
        if (pausedPlayerController != null)
        {
            pausedPlayerControllerWasEnabled = pausedPlayerController.enabled;
            pausedPlayerController.enabled = false;
        }

        mobileMenuPanel.transform.SetAsLastSibling();
        mobileMenuPanel.SetActive(true);
        isMobileMenuOpen = true;
        Debug.Log("[MobileControlsUI] 移动端菜单打开");
    }

    void CloseMobileMenu()
    {
        if (!isMobileMenuOpen)
        {
            return;
        }

        if (mobileMenuPanel != null)
        {
            mobileMenuPanel.SetActive(false);
        }

        Time.timeScale = mobileMenuOriginalTimeScale;

        mobileMenuCanvasScope?.Dispose();
        mobileMenuCanvasScope = null;

        if (pausedPlayerController != null && pausedPlayerControllerWasEnabled)
        {
            pausedPlayerController.enabled = true;
        }

        pausedPlayerController = null;
        isMobileMenuOpen = false;
        Debug.Log("[MobileControlsUI] 移动端菜单关闭");
    }

    void OpenSettingsFromMobileMenu()
    {
        CloseMobileMenu();
        SettingsManager.Instance.OpenSettings();
    }

    void QuitGameFromMobileMenu()
    {
        CloseMobileMenu();
        Time.timeScale = 1f;

        void FinishExit()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            UnityEngine.SceneManagement.SceneManager.LoadScene("StartScene");
#else
            Application.Quit();
#endif
        }

        if (Backend.TelemetryClient.Instance != null && Backend.TelemetryClient.Instance.IsResearchActive)
        {
            Backend.ResearchParticipationCoordinator.Instance.EndSession("menu_exit", FinishExit);
        }
        else
        {
            FinishExit();
        }
        Debug.Log("[MobileControlsUI] 退出游戏");
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 显示/隐藏虚拟控制
    /// </summary>
    public void SetVirtualControlsVisible(bool visible)
    {
        if (!visible)
        {
            ResetControlState();
        }

        gameObject.SetActive(visible);
        Debug.Log($"[MobileControlsUI] 虚拟控制: {(visible ? "显示" : "隐藏")}");
    }

    /// <summary>
    /// 动态调整控件大小
    /// </summary>
    public void SetControlSize(float scale)
    {
        buttonSize *= scale;
        joystickRange *= scale;

        // 重新布局控件
        SetupVirtualControls();
    }

    #endregion

    #region 调试

    void OnGUI()
    {
        if (!enableDebugVisualization) return;

        GUILayout.BeginArea(new Rect(10, 220, 300, 150));
        GUILayout.Label("=== 虚拟控制调试 ===");
        GUILayout.Label($"摇杆输入: {joystickInput}");
        GUILayout.Label($"摇杆激活: {isJoystickActive}");
        GUILayout.Label($"视角触摸: {isLookTouchActive}");
        GUILayout.Label($"奔跑状态: {isRunPressed}");

        if (GUILayout.Button("显示/隐藏控制"))
        {
            SetVirtualControlsVisible(!gameObject.activeSelf);
        }

        GUILayout.EndArea();
    }

    #endregion

    #region 无人机控制方法

    /// <summary>
    /// 设置无人机控制按钮的显示/隐藏
    /// </summary>
    public void SetDroneControlsVisible(bool visible)
    {
        if (droneControlsContainer != null)
        {
            droneControlsContainer.SetActive(visible);
            Debug.Log($"[MobileControlsUI] 无人机控制按钮 {(visible ? "显示" : "隐藏")}");
        }

        // 在无人机模式下隐藏右下角的白色按钮（E键和F键）
        if (interactButton != null)
        {
            interactButton.gameObject.SetActive(!visible);
        }
        if (secondaryInteractButton != null)
        {
            secondaryInteractButton.gameObject.SetActive(!visible);
        }

        Debug.Log($"[MobileControlsUI] 右下角白色按钮 {(visible ? "隐藏" : "显示")} (E和F键)");
    }

    /// <summary>
    /// 检查是否在无人机模式
    /// </summary>
    public bool IsInDroneMode()
    {
        return droneControlsContainer != null && droneControlsContainer.activeSelf;
    }

    /// <summary>
    /// 上升按钮按下事件
    /// </summary>
    void OnAscendButtonDown()
    {
        SetButtonVisualPressed(ascendButton, true);
        if (inputManager != null)
        {
            inputManager.SetAscendInput(true);
        }
        Debug.Log("[MobileControlsUI] 上升按钮按下");
    }

    /// <summary>
    /// 上升按钮释放事件
    /// </summary>
    void OnAscendButtonUp()
    {
        SetButtonVisualPressed(ascendButton, false);
        if (inputManager != null)
        {
            inputManager.SetAscendInput(false);
        }
        Debug.Log("[MobileControlsUI] 上升按钮释放");
    }

    /// <summary>
    /// 下降按钮按下事件
    /// </summary>
    void OnDescendButtonDown()
    {
        SetButtonVisualPressed(descendButton, true);
        if (inputManager != null)
        {
            inputManager.SetDescendInput(true);
        }
        Debug.Log("[MobileControlsUI] 下降按钮按下");
    }

    /// <summary>
    /// 下降按钮释放事件
    /// </summary>
    void OnDescendButtonUp()
    {
        SetButtonVisualPressed(descendButton, false);
        if (inputManager != null)
        {
            inputManager.SetDescendInput(false);
        }
        Debug.Log("[MobileControlsUI] 下降按钮释放");
    }

    #endregion
}

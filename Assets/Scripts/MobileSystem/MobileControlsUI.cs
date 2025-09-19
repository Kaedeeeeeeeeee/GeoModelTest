using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// 移动端虚拟控制界面
/// 包含虚拟摇杆、触摸区域、虚拟按钮等移动端专用控件
/// </summary>
public class MobileControlsUI : MonoBehaviour
{
    [Header("虚拟摇杆设置")]
    public GameObject joystickContainer;
    public RectTransform joystickBackground;
    public RectTransform joystickHandle;
    public float joystickRange = 50f;
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

    [Header("无人机专用按钮")]
    public Button ascendButton; // 上升按钮（无人机模式）
    public Button descendButton; // 下降按钮（无人机模式）
    public GameObject droneControlsContainer; // 无人机控制容器（用于显示/隐藏）
    
    [Header("触摸区域")]
    public RectTransform lookTouchArea; // 视角控制区域
    
    [Header("UI布局")]
    public float buttonSize = 80f;
    public float buttonSpacing = 20f;
    public float edgeMargin = 40f;
    public Vector2 joystickPosition = new Vector2(100, 100); // 从左下角的偏移 - 安全可见位置
    
    [Header("视觉效果")]
    public Color joystickBackgroundColor = new Color(1f, 1f, 1f, 0.3f);
    public Color joystickHandleColor = new Color(1f, 1f, 1f, 0.6f);
    public Color buttonNormalColor = new Color(1f, 1f, 1f, 0.7f);
    public Color buttonPressedColor = new Color(0.8f, 0.8f, 0.8f, 0.9f);
    
    [Header("自适应设置")]
    public bool autoHideOnDesktop = true;
    public bool adaptToSafeArea = true;
    
    [Header("调试")]
    public bool enableDebugVisualization = false;
    public bool forceShowOnDesktop = false; // 强制在桌面显示（用于测试）
    public bool enableMouseInput = true; // 桌面测试模式下允许鼠标输入
    
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
    
    void Awake()
    {
        // 获取或创建Canvas
        SetupCanvas();
    }

    void Start()
    {
        // 获取输入管理器引用（在Start中确保MobileInputManager已初始化）
        inputManager = MobileInputManager.Instance;
        if (inputManager == null)
        {
            // 尝试在场景中查找
            inputManager = FindObjectOfType<MobileInputManager>();
            if (inputManager == null)
            {
                Debug.LogError("[MobileControlsUI] 未找到MobileInputManager！移动端输入无法工作");
            }
            else
            {
                Debug.Log("[MobileControlsUI] 通过FindObjectOfType找到MobileInputManager");
            }
        }
        else
        {
            Debug.Log("[MobileControlsUI] 通过Instance找到MobileInputManager");
        }

        // 原有的Start逻辑
        StartOriginalLogic();
    }

    void StartOriginalLogic()
    {
        // 根据设备类型决定是否显示
        bool shouldShow = true;

        if (forceShowOnDesktop)
        {
            shouldShow = true;
            Debug.Log($"[MobileControlsUI] 强制显示模式 - 显示虚拟控件");
        }
        else if (autoHideOnDesktop && !Application.isMobilePlatform)
        {
            shouldShow = inputManager != null && inputManager.ShouldShowVirtualControls();
            Debug.Log($"[MobileControlsUI] 桌面平台检测 - 应该显示虚拟控件: {shouldShow}");
            Debug.Log($"[MobileControlsUI] 输入管理器存在: {inputManager != null}");
            if (inputManager != null)
            {
                Debug.Log($"[MobileControlsUI] ShouldShowVirtualControls: {inputManager.ShouldShowVirtualControls()}");
            }
        }
        else
        {
            Debug.Log($"[MobileControlsUI] 移动平台或未启用桌面隐藏 - 显示虚拟控件");
        }

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
    
    void Update()
    {
        // 处理摇杆输入
        ProcessJoystickInput();

        // 桌面测试模式：处理鼠标输入模拟触摸
        if (enableMouseInput && (forceShowOnDesktop || (inputManager != null && inputManager.desktopTestMode)))
        {
            ProcessMouseInput();

            // 调试快捷键：R键重置摇杆位置
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                ResetJoystickPosition();
                Debug.Log("[MobileControlsUI] R键重置摇杆位置");
            }
        }

        // 发送输入数据给输入管理器
        if (inputManager != null)
        {
            inputManager.SetMoveInput(joystickInput);
        }
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
        if (lookTouchArea == null) CreateLookTouchArea();
        
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
        container.transform.SetParent(transform);
        
        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.sizeDelta = new Vector2(joystickRange * 2, joystickRange * 2);
        containerRect.anchorMin = new Vector2(0, 0);
        containerRect.anchorMax = new Vector2(0, 0);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = joystickPosition;
        
        joystickContainer = container;
        
        // 创建摇杆背景
        GameObject background = new GameObject("JoystickBackground");
        background.transform.SetParent(container.transform);
        
        joystickBackground = background.AddComponent<RectTransform>();
        joystickBackground.sizeDelta = new Vector2(joystickRange * 2, joystickRange * 2);
        joystickBackground.anchorMin = new Vector2(0.5f, 0.5f);
        joystickBackground.anchorMax = new Vector2(0.5f, 0.5f);
        joystickBackground.pivot = new Vector2(0.5f, 0.5f);
        joystickBackground.anchoredPosition = Vector2.zero;
        
        Image bgImage = background.AddComponent<Image>();
        bgImage.sprite = CreateCircleSprite(128);
        bgImage.color = joystickBackgroundColor;
        bgImage.type = Image.Type.Simple;
        
        // 创建摇杆手柄
        GameObject handle = new GameObject("JoystickHandle");
        handle.transform.SetParent(container.transform);
        
        joystickHandle = handle.AddComponent<RectTransform>();
        joystickHandle.sizeDelta = new Vector2(joystickRange, joystickRange);
        joystickHandle.anchorMin = new Vector2(0.5f, 0.5f);
        joystickHandle.anchorMax = new Vector2(0.5f, 0.5f);
        joystickHandle.pivot = new Vector2(0.5f, 0.5f);
        joystickHandle.anchoredPosition = Vector2.zero;
        
        Image handleImage = handle.AddComponent<Image>();
        handleImage.sprite = CreateCircleSprite(64);
        handleImage.color = joystickHandleColor;
        handleImage.type = Image.Type.Simple;
        
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
        jumpButton = CreateButton("JumpButton", "⬆", new Vector2(-edgeMargin - buttonSize/2, edgeMargin + buttonSize/2),
                                  new Vector2(1, 0), OnJumpButtonDown, OnJumpButtonUp);

        // 奔跑按钮
        runButton = CreateButton("RunButton", "🏃", new Vector2(-edgeMargin - buttonSize * 1.5f - buttonSpacing, edgeMargin + buttonSize/2),
                                 new Vector2(1, 0), OnRunButtonDown, OnRunButtonUp);

        // E键交互按钮 - 右下角
        interactButton = CreateButton("InteractButton", "E", new Vector2(-edgeMargin - buttonSize/2, edgeMargin + buttonSize * 1.5f + buttonSpacing),
                                      new Vector2(1, 0), OnInteractButtonDown, OnInteractButtonUp);

        // F键交互按钮 - E键上方
        secondaryInteractButton = CreateButton("SecondaryInteractButton", "F", new Vector2(-edgeMargin - buttonSize/2, edgeMargin + buttonSize * 2.5f + buttonSpacing * 2),
                                               new Vector2(1, 0), OnSecondaryInteractButtonDown, OnSecondaryInteractButtonUp);
        
        // 背包按钮
        inventoryButton = CreateButton("InventoryButton", "🎒", new Vector2(edgeMargin + buttonSize/2, -edgeMargin - buttonSize/2),
                                       new Vector2(0, 1), OnInventoryButtonClick, null);

        // 图鉴按钮 - 在背包按钮旁边
        encyclopediaButton = CreateButton("EncyclopediaButton", "📚", new Vector2(edgeMargin + buttonSize * 1.5f + buttonSpacing, -edgeMargin - buttonSize/2),
                                          new Vector2(0, 1), OnEncyclopediaButtonClick, null);

        // 工具轮盘按钮
        toolWheelButton = CreateButton("ToolWheelButton", "⚙", new Vector2(edgeMargin + buttonSize/2, -edgeMargin - buttonSize * 1.5f - buttonSpacing),
                                       new Vector2(0, 1), OnToolWheelButtonClick, null);

        // 创建无人机控制容器
        CreateDroneControls();

        Debug.Log("[MobileControlsUI] 虚拟按钮创建完成");
    }
    
    /// <summary>
    /// 创建单个按钮
    /// </summary>
    Button CreateButton(string name, string text, Vector2 position, Vector2 anchor, 
                       UnityEngine.Events.UnityAction onDown, UnityEngine.Events.UnityAction onUp)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(transform);
        
        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(buttonSize, buttonSize);
        buttonRect.anchorMin = anchor;
        buttonRect.anchorMax = anchor;
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = position;
        
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.sprite = CreateCircleSprite();
        buttonImage.color = buttonNormalColor;
        buttonImage.type = Image.Type.Simple;
        
        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        
        // 添加按钮文本
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.sizeDelta = Vector2.zero;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.anchoredPosition = Vector2.zero;
        
        Text buttonText = textObj.AddComponent<Text>();
        buttonText.text = text;
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = (int)(buttonSize * 0.4f);
        buttonText.color = Color.white;
        buttonText.alignment = TextAnchor.MiddleCenter;
        
        // 设置按钮事件
        if (onDown != null)
        {
            // 使用EventTrigger处理按下和释放事件
            EventTrigger trigger = buttonObj.AddComponent<EventTrigger>();
            
            EventTrigger.Entry downEntry = new EventTrigger.Entry();
            downEntry.eventID = EventTriggerType.PointerDown;
            downEntry.callback.AddListener((data) => onDown.Invoke());
            trigger.triggers.Add(downEntry);
            
            if (onUp != null)
            {
                EventTrigger.Entry upEntry = new EventTrigger.Entry();
                upEntry.eventID = EventTriggerType.PointerUp;
                upEntry.callback.AddListener((data) => onUp.Invoke());
                trigger.triggers.Add(upEntry);
                
                EventTrigger.Entry exitEntry = new EventTrigger.Entry();
                exitEntry.eventID = EventTriggerType.PointerExit;
                exitEntry.callback.AddListener((data) => onUp.Invoke());
                trigger.triggers.Add(exitEntry);
            }
        }
        else
        {
            button.onClick.AddListener(onDown);
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
        ascendButton = CreateButton("AscendButton", "🔺", new Vector2(0, buttonSize * 1.5f + buttonSpacing),
                                   new Vector2(0.5f, 0f), OnAscendButtonDown, OnAscendButtonUp, droneContainer.transform);

        // 创建下降按钮（右下角下方位置，对应E键位置）
        descendButton = CreateButton("DescendButton", "🔻", new Vector2(0, buttonSize * 0.5f),
                                    new Vector2(0.5f, 0f), OnDescendButtonDown, OnDescendButtonUp, droneContainer.transform);

        // 设置按钮颜色为蓝色系（区别于普通按钮）
        if (ascendButton != null)
        {
            var ascendImage = ascendButton.GetComponent<Image>();
            if (ascendImage != null)
            {
                ascendImage.color = new Color(0.3f, 0.7f, 1f, 0.8f); // 浅蓝色
            }
        }

        if (descendButton != null)
        {
            var descendImage = descendButton.GetComponent<Image>();
            if (descendImage != null)
            {
                descendImage.color = new Color(0.3f, 0.7f, 1f, 0.8f); // 浅蓝色
            }
        }

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
        image.color = buttonNormalColor;

        // 添加Button组件
        Button button = buttonObj.AddComponent<Button>();

        // 添加文字
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text buttonText = textObj.AddComponent<Text>();
        buttonText.text = text;
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = 24;
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.color = Color.white;

        // 设置按钮事件
        if (onUp != null)
        {
            EventTrigger trigger = buttonObj.AddComponent<EventTrigger>();

            EventTrigger.Entry downEntry = new EventTrigger.Entry();
            downEntry.eventID = EventTriggerType.PointerDown;
            downEntry.callback.AddListener((data) => onDown.Invoke());
            trigger.triggers.Add(downEntry);

            EventTrigger.Entry upEntry = new EventTrigger.Entry();
            upEntry.eventID = EventTriggerType.PointerUp;
            upEntry.callback.AddListener((data) => onUp.Invoke());
            trigger.triggers.Add(upEntry);

            EventTrigger.Entry exitEntry = new EventTrigger.Entry();
            exitEntry.eventID = EventTriggerType.PointerExit;
            exitEntry.callback.AddListener((data) => onUp.Invoke());
            trigger.triggers.Add(exitEntry);
        }
        else
        {
            button.onClick.AddListener(() => onDown?.Invoke());
        }

        return button;
    }

    /// <summary>
    /// 创建视角触摸区域
    /// </summary>
    void CreateLookTouchArea()
    {
        GameObject touchArea = new GameObject("LookTouchArea");
        touchArea.transform.SetParent(transform);
        
        lookTouchArea = touchArea.AddComponent<RectTransform>();
        lookTouchArea.anchorMin = new Vector2(0.3f, 0.3f);
        lookTouchArea.anchorMax = new Vector2(1f, 1f);
        lookTouchArea.offsetMin = Vector2.zero;
        lookTouchArea.offsetMax = Vector2.zero;
        
        // 添加透明图像以接收触摸事件
        Image touchImage = touchArea.AddComponent<Image>();
        touchImage.color = new Color(0, 0, 0, 0); // 完全透明
        touchImage.raycastTarget = false; // 关闭射线检测，避免阻挡其他UI
        
        if (enableDebugVisualization)
        {
            touchImage.color = new Color(0, 1, 0, 0.1f); // 调试时显示绿色半透明
        }
        
        Debug.Log("[MobileControlsUI] 视角触摸区域创建完成");
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
        }
        
        // 为视角触摸区域添加事件
        if (lookTouchArea != null)
        {
            EventTrigger lookTrigger = lookTouchArea.GetComponent<EventTrigger>();
            if (lookTrigger == null)
                lookTrigger = lookTouchArea.gameObject.AddComponent<EventTrigger>();
            
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
        
        // 调整控件位置以适应安全区域
        if (joystickContainer != null)
        {
            RectTransform joystickRect = joystickContainer.GetComponent<RectTransform>();
            Vector2 newPos = joystickRect.anchoredPosition;
            newPos.x += leftMargin / canvasScaler.scaleFactor;
            newPos.y += bottomMargin / canvasScaler.scaleFactor;
            joystickRect.anchoredPosition = newPos;
        }
        
        Debug.Log($"[MobileControlsUI] 安全区域适配完成 - 边距: L{leftMargin} R{rightMargin} T{topMargin} B{bottomMargin}");
    }
    
    /// <summary>
    /// 创建圆形Sprite
    /// </summary>
    Sprite CreateCircleSprite(int size = 128)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] colors = new Color[size * size];
        
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 2f; // 留一点边距
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 point = new Vector2(x, y);
                float distance = Vector2.Distance(point, center);
                
                if (distance <= radius)
                {
                    // 在圆形内，设置为白色
                    float alpha = 1f - (distance / radius) * 0.2f; // 边缘稍微透明
                    colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
                else
                {
                    // 在圆形外，设置为透明
                    colors[y * size + x] = Color.clear;
                }
            }
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
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
            
            // 禁用动态摇杆功能，保持固定位置
            // 动态摇杆功能已完全禁用以避免位置计算问题
            
            Debug.Log("[MobileControlsUI] 摇杆激活");
        }
    }
    
    void OnJoystickDrag(BaseEventData eventData)
    {
        if (!isJoystickActive) return;

        PointerEventData pointerData = eventData as PointerEventData;
        if (pointerData != null && pointerData.pointerId == joystickPointerId)
        {
            // 简化计算：直接使用存储的起始位置
            Vector2 direction = pointerData.position - joystickStartPosition;
            float distance = Mathf.Clamp(direction.magnitude, 0, joystickRange);

            joystickInput = direction.normalized * (distance / joystickRange);

            // 更新手柄位置
            if (joystickHandle != null)
            {
                joystickHandle.anchoredPosition = direction.normalized * distance;
            }
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
        if (inputManager != null)
        {
            inputManager.SetJumpInput(true);
        }
        Debug.Log("[MobileControlsUI] 跳跃按钮按下");
    }
    
    void OnJumpButtonUp()
    {
        if (inputManager != null)
        {
            inputManager.SetJumpInput(false);
        }
        Debug.Log("[MobileControlsUI] 跳跃按钮释放");
    }
    
    void OnRunButtonDown()
    {
        isRunPressed = true;
        if (inputManager != null)
        {
            inputManager.SetRunInput(true);
        }
        Debug.Log("[MobileControlsUI] 奔跑按钮按下");
    }
    
    void OnRunButtonUp()
    {
        isRunPressed = false;
        if (inputManager != null)
        {
            inputManager.SetRunInput(false);
        }
        Debug.Log("[MobileControlsUI] 奔跑按钮释放");
    }
    
    void OnInteractButtonDown()
    {
        if (inputManager != null)
        {
            inputManager.SetInteractInput(true);
        }
        Debug.Log("[MobileControlsUI] 交互按钮按下");
    }
    
    void OnInteractButtonUp()
    {
        if (inputManager != null)
        {
            inputManager.SetInteractInput(false);
        }
        Debug.Log("[MobileControlsUI] 交互按钮释放");
    }

    void OnSecondaryInteractButtonDown()
    {
        if (inputManager != null)
        {
            inputManager.SetSecondaryInteractInput(true);
        }
        Debug.Log("[MobileControlsUI] F键交互按钮按下");
    }

    void OnSecondaryInteractButtonUp()
    {
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
    
    #endregion
    
    #region 辅助方法
    
    /// <summary>
    /// 创建圆形精灵
    /// </summary>
    Sprite CreateCircleSprite()
    {
        // 创建简单的圆形纹理
        int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 2;
        
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance <= radius)
                {
                    float alpha = Mathf.SmoothStep(1f, 0f, (distance - radius + 4) / 4f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }
        
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
    
    /// <summary>
    /// 显示/隐藏虚拟控制
    /// </summary>
    public void SetVirtualControlsVisible(bool visible)
    {
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
        if (inputManager != null)
        {
            inputManager.SetDescendInput(false);
        }
        Debug.Log("[MobileControlsUI] 下降按钮释放");
    }

    #endregion
}
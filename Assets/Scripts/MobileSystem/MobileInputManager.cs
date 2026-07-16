using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;
using StorySystem;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

/// <summary>
/// 移动端输入管理器 - 统一处理触摸输入、虚拟摇杆、手势识别
/// 为现有控制器提供标准接口，支持PC/移动端无缝切换
/// </summary>
public class MobileInputManager : MonoBehaviour
{
    [Header("输入模式设置")]
    public InputMode currentInputMode = InputMode.Auto;
    public bool enableTouchInput = true;
    public bool enableVirtualControls = true;
    
    [Header("虚拟摇杆设置")]
    public float joystickDeadZone = 0.1f;
    public float joystickSensitivity = 1.0f;
    
    [Header("触摸控制设置")]
    [Range(0.1f, 3.0f)]
    public float touchSensitivity = 0.55f;
    public float touchDeadZone = 10f; // 像素
    
    [Header("调试设置")]
    public bool enableDebugLog = false;
    
    [Header("桌面测试模式")]
    public bool desktopTestMode = false; // 桌面测试模式 - 允许鼠标点击虚拟控件
    
    // 单例模式
    public static MobileInputManager Instance { get; private set; }
    
    // 输入事件
    public event System.Action<Vector2> OnMoveInput;
    public event System.Action<Vector2> OnLookInput;
    public event System.Action<bool> OnJumpInput;
    public event System.Action<bool> OnRunInput;
    public event System.Action<bool> OnInteractInput;
    public event System.Action<bool> OnSecondaryInteractInput;
    public event System.Action OnInventoryInput;
    public event System.Action OnWarehouseInput;
    public event System.Action OnToolWheelInput;
    public event System.Action OnEncyclopediaInput;

    // 垂直控制事件（无人机专用）
    public event System.Action<bool> OnAscendInput;
    public event System.Action<bool> OnDescendInput;
    public event System.Action<float> OnVerticalInput;
    
    // 输入状态
    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool IsJumping { get; private set; }
    public bool IsRunning { get; private set; }
    public bool IsInteracting { get; private set; }
    public bool IsSecondaryInteracting { get; private set; }

    // 垂直控制输入（无人机专用）
    public bool IsAscending { get; private set; }
    public bool IsDescending { get; private set; }
    public float VerticalInput { get; private set; } // -1下降, 0悬停, 1上升
    
    // 触摸相关
    private Vector2 lastTouchPosition;
    private bool isDragging = false;
    private float touchStartTime;
    private Vector2 touchStartPosition;
    private int activeLookTouchId = int.MinValue;
    private bool activeTouchStartedOverUI = false;
    
    // 设备检测
    private bool isMobileDevice;
    private bool hasTouch;
    private bool hasActiveTouchInput;
    private bool wasStoryInputBlocked;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern int GeoModelTest_IsMobileBrowser();
#endif
    
    public enum InputMode
    {
        Auto,           // 自动检测
        Desktop,        // 强制桌面端模式
        Mobile,         // 强制移动端模式
        Hybrid          // 混合模式（同时支持）
    }
    
    void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 初始化设备检测
            DetectDevice();
            
            // 创建光标管理器
            CreateCursorManager();
            
            Debug.Log($"[MobileInputManager] 初始化完成 - 设备类型: {(isMobileDevice ? "移动设备" : "桌面设备")}");
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // 启用触摸输入
        if (enableTouchInput && hasTouch)
        {
            // Unity新输入系统自动支持触摸
            Debug.Log("[MobileInputManager] 触摸输入已启用");
        }
        
        // 根据设备类型调整输入模式
        AdjustInputMode();
    }
    
    void Update()
    {
        DetectDevice();

        if (StoryDirector.IsStoryPlaybackActive)
        {
            if (!wasStoryInputBlocked)
            {
                ReleaseGameplayInputsForStory();
            }

            wasStoryInputBlocked = true;
            return;
        }

        wasStoryInputBlocked = false;

        // 处理不同输入源
        switch (currentInputMode)
        {
            case InputMode.Auto:
                if (desktopTestMode)
                {
                    // 桌面测试模式允许键鼠和虚拟控件同时工作，方便在电脑上验证移动端UI。
                    ProcessDesktopInput();
                    ProcessMobileInput();
                }
                else if (isMobileDevice)
                {
                    ProcessMobileInput();
                }
                else
                {
                    ProcessDesktopInput();
                    if (HasActiveTouchscreenInput())
                    {
                        ProcessMobileInput();
                    }
                    else
                    {
                        hasActiveTouchInput = false;
                    }
                }
                break;
                
            case InputMode.Desktop:
                ProcessDesktopInput();
                break;
                
            case InputMode.Mobile:
                ProcessMobileInput();
                break;
                
            case InputMode.Hybrid:
                ProcessDesktopInput();
                ProcessMobileInput();
                break;
        }
        
        // 处理通用按键输入
        ProcessCommonInput();
    }
    
    /// <summary>
    /// 检测设备类型
    /// </summary>
    void DetectDevice()
    {
        // 检测是否为移动设备。触摸输入单独记录，不能把 PC/Mac 的触摸能力当成移动端。
        isMobileDevice = IsRuntimeMobileDevice();
        
        // 检测是否支持触摸
        hasTouch = Touchscreen.current != null;
        
        if (enableDebugLog)
        {
            Debug.Log($"[MobileInputManager] 设备检测: 移动设备={isMobileDevice}, 支持触摸={hasTouch}");
            Debug.Log($"[MobileInputManager] 平台: {Application.platform}");
        }
    }
    
    /// <summary>
    /// 根据设备自动调整输入模式
    /// </summary>
    void AdjustInputMode()
    {
        if (currentInputMode == InputMode.Auto)
        {
            if (isMobileDevice)
            {
                // 手机/平板设备
                enableVirtualControls = true;
                Debug.Log("[MobileInputManager] 自动切换到移动端输入模式");
            }
            else
            {
                // 桌面设备
                enableVirtualControls = false;
                Debug.Log("[MobileInputManager] 自动切换到桌面端输入模式");
            }
        }
    }
    
    /// <summary>
    /// 处理桌面端输入 (键盘鼠标)
    /// </summary>
    void ProcessDesktopInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            SetMoveInput(Vector2.zero);
            SetRunInput(false);
            SetJumpInput(false);
            SetInteractInput(false);
            SetSecondaryInteractInput(false);
            SetLookInput(Vector2.zero);
            return;
        }
        
        // 移动输入 (WASD)
        Vector2 move = Vector2.zero;
        if (keyboard.wKey.isPressed) move.y = 1;
        if (keyboard.sKey.isPressed) move.y = -1;
        if (keyboard.aKey.isPressed) move.x = -1;
        if (keyboard.dKey.isPressed) move.x = 1;
        
        SetMoveInput(move);
        
        // 鼠标视角控制
        var mouse = Mouse.current;
        if (mouse != null)
        {
            Vector2 mouseDelta = mouse.delta.ReadValue();
            SetLookInput(mouseDelta);
        }
        else
        {
            SetLookInput(Vector2.zero);
        }
        
        // 跳跃输入
        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            SetJumpInput(true);
        }
        else if (keyboard.spaceKey.wasReleasedThisFrame)
        {
            SetJumpInput(false);
        }
        
        // 奔跑输入
        SetRunInput(keyboard.leftShiftKey.isPressed);
    }
    
    /// <summary>
    /// 处理移动端输入 (触摸)
    /// </summary>
    void ProcessMobileInput()
    {
        if (!enableTouchInput || Touchscreen.current == null)
        {
            hasActiveTouchInput = false;
            ResetRawTouchState();
            return;
        }

        bool processedLookTouch = false;
        bool anyActiveTouch = false;
        var touches = Touchscreen.current.touches;
        for (int i = 0; i < touches.Count; i++)
        {
            var touch = touches[i];
            TouchPhase phase = touch.phase.ReadValue();
            if (phase == TouchPhase.None)
            {
                continue;
            }

            if (phase == TouchPhase.Began ||
                phase == TouchPhase.Moved ||
                phase == TouchPhase.Stationary)
            {
                anyActiveTouch = true;
            }

            int touchId = touch.touchId.ReadValue();
            if (touchId == activeLookTouchId)
            {
                ProcessPrimaryTouch(touch);
                processedLookTouch = true;
                continue;
            }

            if (activeLookTouchId == int.MinValue && phase == TouchPhase.Began)
            {
                Vector2 touchPosition = touch.position.ReadValue();
                if (!IsPointOverUI(touchPosition, touchId))
                {
                    ProcessPrimaryTouch(touch);
                    processedLookTouch = true;
                }
            }
        }

        if (!processedLookTouch)
        {
            if (activeLookTouchId == int.MinValue)
            {
                SetLookInput(Vector2.zero);
            }
            else
            {
                ResetRawTouchState();
            }
        }

        hasActiveTouchInput = anyActiveTouch;
    }

    bool HasActiveTouchscreenInput()
    {
        if (!enableTouchInput || Touchscreen.current == null)
        {
            return false;
        }

        var touches = Touchscreen.current.touches;
        for (int i = 0; i < touches.Count; i++)
        {
            TouchPhase phase = touches[i].phase.ReadValue();
            if (phase == TouchPhase.Began ||
                phase == TouchPhase.Moved ||
                phase == TouchPhase.Stationary)
            {
                return true;
            }
        }

        return false;
    }
    
    /// <summary>
    /// 处理主触摸点 (通常用于视角控制)
    /// </summary>
    void ProcessPrimaryTouch(UnityEngine.InputSystem.Controls.TouchControl touch)
    {
        Vector2 touchPosition = touch.position.ReadValue();
        TouchPhase phase = touch.phase.ReadValue();
        int touchId = touch.touchId.ReadValue();
        
        switch (phase)
        {
            case TouchPhase.Began:
                lastTouchPosition = touchPosition;
                touchStartPosition = touchPosition;
                touchStartTime = Time.time;
                isDragging = false;
                activeLookTouchId = touchId;
                activeTouchStartedOverUI = IsPointOverUI(touchPosition, touchId);

                if (activeTouchStartedOverUI)
                {
                    SetLookInput(Vector2.zero);
                }
                
                if (enableDebugLog)
                    Debug.Log($"[MobileInputManager] 触摸开始: {touchPosition}");
                break;
                
            case TouchPhase.Moved:
                if (touchId != activeLookTouchId || activeTouchStartedOverUI)
                {
                    SetLookInput(Vector2.zero);
                    break;
                }

                if (!isDragging)
                {
                    float distance = Vector2.Distance(touchPosition, touchStartPosition);
                    if (distance > touchDeadZone)
                    {
                        isDragging = true;
                    }
                }
                
                if (isDragging)
                {
                    Vector2 delta = touchPosition - lastTouchPosition;
                    SetLookInput(delta * touchSensitivity);
                }
                
                lastTouchPosition = touchPosition;
                break;

            case TouchPhase.Stationary:
                if (touchId == activeLookTouchId)
                {
                    SetLookInput(Vector2.zero);
                }
                break;
                
            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                if (touchId == activeLookTouchId && !isDragging && !activeTouchStartedOverUI)
                {
                    // 短触摸 - 可能是点击事件
                    float touchDuration = Time.time - touchStartTime;
                    if (touchDuration < 0.3f) // 300ms内算作点击
                    {
                        ProcessTouchTap(touchStartPosition, touchId);
                    }
                }
                
                ResetRawTouchState();
                
                if (enableDebugLog)
                    Debug.Log($"[MobileInputManager] 触摸结束");
                break;

            default:
                SetLookInput(Vector2.zero);
                break;
        }
    }
    
    /// <summary>
    /// 处理次要触摸点 (可用于特殊操作)
    /// </summary>
    void ProcessSecondaryTouch(UnityEngine.InputSystem.Controls.TouchControl touch)
    {
        // 可以用于双指缩放、旋转等操作
        if (enableDebugLog)
        {
            Debug.Log($"[MobileInputManager] 检测到第二个触摸点");
        }
    }
    
    /// <summary>
    /// 处理触摸点击事件
    /// </summary>
    void ProcessTouchTap(Vector2 position, int pointerId)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[MobileInputManager] 检测到点击: {position}");
        }
        
        // 这里可以处理UI点击检测等
        // 如果点击的不是UI元素，可以触发交互事件
        if (!IsPointOverUI(position, pointerId))
        {
            SetInteractInput(true);
            SetInteractInput(false); // 立即释放
        }
    }
    
    /// <summary>
    /// 处理通用输入 (快捷键等)
    /// </summary>
    void ProcessCommonInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        
        // UI keyboard shortcuts (Tab/I/O) are handled by InventoryUISystem.
        // MobileControlsUI still uses Trigger... methods below for touch buttons.
        
        // 交互键 (E键)
        if (keyboard.eKey.wasPressedThisFrame)
        {
            OnInteractInput?.Invoke(true);
        }
        else if (keyboard.eKey.wasReleasedThisFrame)
        {
            OnInteractInput?.Invoke(false);
        }

        // F键交互
        if (keyboard.fKey.wasPressedThisFrame)
        {
            OnSecondaryInteractInput?.Invoke(true);
        }
        else if (keyboard.fKey.wasReleasedThisFrame)
        {
            OnSecondaryInteractInput?.Invoke(false);
        }
    }
    
    /// <summary>
    /// 检查点击位置是否在UI上
    /// </summary>
    bool IsPointOverUI(Vector2 screenPosition, int pointerId = int.MinValue)
    {
        MobileControlsUI mobileControls = MobileControlsUI.ActiveInstance;
        if (mobileControls == null)
        {
            mobileControls = FindFirstObjectByType<MobileControlsUI>();
        }

        if (mobileControls != null && mobileControls.ContainsControlAtScreenPoint(screenPosition))
        {
            return true;
        }

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null) return false;

        if (pointerId != int.MinValue && eventSystem.IsPointerOverGameObject(pointerId))
        {
            return true;
        }

        if (pointerId == int.MinValue && eventSystem.IsPointerOverGameObject())
        {
            return true;
        }

        PointerEventData eventData = new PointerEventData(eventSystem)
        {
            position = screenPosition
        };
        List<RaycastResult> results = new List<RaycastResult>();
        eventSystem.RaycastAll(eventData, results);
        return results.Count > 0;
    }

    void ResetRawTouchState()
    {
        isDragging = false;
        activeLookTouchId = int.MinValue;
        activeTouchStartedOverUI = false;
        SetLookInput(Vector2.zero);
    }

    /// <summary>
    /// 剧情 UI 可能接管原本落在虚拟按钮上的抬起事件，因此进入剧情时主动释放所有状态。
    /// </summary>
    void ReleaseGameplayInputsForStory()
    {
        hasActiveTouchInput = false;
        ResetRawTouchState();
        SetMoveInput(Vector2.zero);

        if (IsJumping) SetJumpInput(false);
        if (IsRunning) SetRunInput(false);
        if (IsInteracting) SetInteractInput(false);
        if (IsSecondaryInteracting) SetSecondaryInteractInput(false);
        if (IsAscending) SetAscendInput(false);
        if (IsDescending) SetDescendInput(false);
    }
    
    #region 公共接口方法
    
    /// <summary>
    /// 设置移动输入
    /// </summary>
    public void SetMoveInput(Vector2 input)
    {
        // 应用死区
        if (input.magnitude < joystickDeadZone)
        {
            input = Vector2.zero;
        }
        else
        {
            // 应用灵敏度
            input *= joystickSensitivity;
            input = Vector2.ClampMagnitude(input, 1f);
        }

        MoveInput = input;
        OnMoveInput?.Invoke(input);
    }
    
    /// <summary>
    /// 设置视角输入
    /// </summary>
    public void SetLookInput(Vector2 input)
    {
        LookInput = input;
        OnLookInput?.Invoke(input);
    }
    
    /// <summary>
    /// 设置跳跃输入
    /// </summary>
    public void SetJumpInput(bool isPressed)
    {
        IsJumping = isPressed;
        OnJumpInput?.Invoke(isPressed);
    }
    
    /// <summary>
    /// 设置奔跑输入
    /// </summary>
    public void SetRunInput(bool isPressed)
    {
        IsRunning = isPressed;
        OnRunInput?.Invoke(isPressed);
    }
    
    /// <summary>
    /// 设置交互输入
    /// </summary>
    public void SetInteractInput(bool isPressed)
    {
        if (isPressed && StoryDirector.IsStoryPlaybackActive)
        {
            return;
        }

        IsInteracting = isPressed;
        OnInteractInput?.Invoke(isPressed);
    }

    /// <summary>
    /// 设置F键交互输入
    /// </summary>
    public void SetSecondaryInteractInput(bool isPressed)
    {
        if (isPressed && StoryDirector.IsStoryPlaybackActive)
        {
            return;
        }

        IsSecondaryInteracting = isPressed;
        OnSecondaryInteractInput?.Invoke(isPressed);
    }
    
    /// <summary>
    /// 强制切换输入模式
    /// </summary>
    public void SwitchInputMode(InputMode mode)
    {
        currentInputMode = mode;
        AdjustInputMode();
        
        Debug.Log($"[MobileInputManager] 输入模式切换为: {mode}");
    }
    
    /// <summary>
    /// 启用/禁用虚拟控制
    /// </summary>
    public void SetVirtualControlsEnabled(bool enabled)
    {
        enableVirtualControls = enabled;
        Debug.Log($"[MobileInputManager] 虚拟控制: {(enabled ? "启用" : "禁用")}");
    }
    
    /// <summary>
    /// 获取当前是否为移动设备
    /// </summary>
    public bool IsMobileDevice()
    {
        return isMobileDevice;
    }
    
    /// <summary>
    /// 获取是否应该显示虚拟控制
    /// </summary>
    public bool ShouldShowVirtualControls()
    {
        if (!enableVirtualControls)
        {
            return false;
        }

        if (desktopTestMode)
        {
            return true;
        }

        return IsMobileDevice() && currentInputMode != InputMode.Desktop;
    }

    /// <summary>
    /// 获取当前是否有触摸或虚拟控件输入正在驱动角色。
    /// </summary>
    public bool HasActiveGameplayInput()
    {
        return hasActiveTouchInput ||
               MoveInput.sqrMagnitude > 0.0001f ||
               LookInput.sqrMagnitude > 0.0001f ||
               IsJumping ||
               IsRunning ||
               IsInteracting ||
               IsSecondaryInteracting ||
               IsAscending ||
               IsDescending ||
               Mathf.Abs(VerticalInput) > 0.0001f;
    }

    /// <summary>
    /// 获取当前运行环境是否是真正的手机/平板。
    /// </summary>
    public static bool IsRuntimeMobileDevice()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            return GeoModelTest_IsMobileBrowser() == 1;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[MobileInputManager] WebGL移动浏览器检测失败，回退到Unity平台检测: {e.Message}");
        }
#endif

        return Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld;
    }
    
    /// <summary>
    /// 创建光标管理器
    /// </summary>
    void CreateCursorManager()
    {
        if (MobileCursorManager.Instance == null)
        {
            GameObject cursorManagerObj = new GameObject("MobileCursorManager");
            cursorManagerObj.AddComponent<MobileCursorManager>();
            Debug.Log("[MobileInputManager] MobileCursorManager 已创建");
        }
    }
    
    /// <summary>
    /// 启用桌面测试模式
    /// </summary>
    public void EnableDesktopTestMode(bool enable)
    {
        desktopTestMode = enable;
        enableVirtualControls = enable;
        
        if (enable)
        {
            Debug.Log("[MobileInputManager] 桌面测试模式已启用 - 鼠标和虚拟控件同时工作");
        }
        else
        {
            Debug.Log("[MobileInputManager] 桌面测试模式已禁用");
        }
    }
    
    #endregion
    
    #region 公共事件触发方法
    
    /// <summary>
    /// 触发背包输入事件
    /// </summary>
    public void TriggerInventoryInput()
    {
        OnInventoryInput?.Invoke();
    }
    
    /// <summary>
    /// 触发仓库输入事件
    /// </summary>
    public void TriggerWarehouseInput()
    {
        OnWarehouseInput?.Invoke();
    }
    
    /// <summary>
    /// 触发工具轮盘输入事件
    /// </summary>
    public void TriggerToolWheelInput()
    {
        if (StoryDirector.IsStoryPlaybackActive)
        {
            return;
        }

        Debug.Log("[MobileInputManager] 触发工具轮盘输入");
        OnToolWheelInput?.Invoke();
    }

    /// <summary>
    /// 触发图鉴输入事件
    /// </summary>
    public void TriggerEncyclopediaInput()
    {
        Debug.Log("[MobileInputManager] 触发图鉴输入");
        OnEncyclopediaInput?.Invoke();
    }
    
    #endregion

    #region 垂直控制方法（无人机专用）

    /// <summary>
    /// 设置上升输入状态
    /// </summary>
    public void SetAscendInput(bool isPressed)
    {
        bool wasAscending = IsAscending;
        IsAscending = isPressed;

        // 更新垂直输入值
        UpdateVerticalInput();

        // 触发事件
        if (wasAscending != isPressed)
        {
            OnAscendInput?.Invoke(isPressed);
            if (enableDebugLog)
            {
                Debug.Log($"[MobileInputManager] 上升输入: {isPressed}");
            }
        }
    }

    /// <summary>
    /// 设置下降输入状态
    /// </summary>
    public void SetDescendInput(bool isPressed)
    {
        bool wasDescending = IsDescending;
        IsDescending = isPressed;

        // 更新垂直输入值
        UpdateVerticalInput();

        // 触发事件
        if (wasDescending != isPressed)
        {
            OnDescendInput?.Invoke(isPressed);
            if (enableDebugLog)
            {
                Debug.Log($"[MobileInputManager] 下降输入: {isPressed}");
            }
        }
    }

    /// <summary>
    /// 更新垂直输入值
    /// </summary>
    private void UpdateVerticalInput()
    {
        float newVerticalInput = 0f;

        if (IsAscending && !IsDescending)
        {
            newVerticalInput = 1f; // 上升
        }
        else if (IsDescending && !IsAscending)
        {
            newVerticalInput = -1f; // 下降
        }
        // 两个都按下或都没按下时为0（悬停）

        if (VerticalInput != newVerticalInput)
        {
            VerticalInput = newVerticalInput;
            OnVerticalInput?.Invoke(VerticalInput);

            if (enableDebugLog)
            {
                Debug.Log($"[MobileInputManager] 垂直输入更新: {VerticalInput}");
            }
        }
    }

    /// <summary>
    /// 获取当前垂直输入状态（用于调试）
    /// </summary>
    public string GetVerticalInputStatus()
    {
        return $"上升: {IsAscending}, 下降: {IsDescending}, 垂直值: {VerticalInput:F1}";
    }

    #endregion

    #region 调试方法
    
    void OnGUI()
    {
        if (!enableDebugLog) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label($"输入模式: {currentInputMode}");
        GUILayout.Label($"设备类型: {(isMobileDevice ? "移动设备" : "桌面设备")}");
        GUILayout.Label($"触摸支持: {hasTouch}");
        GUILayout.Label($"移动输入: {MoveInput}");
        GUILayout.Label($"视角输入: {LookInput}");
        GUILayout.Label($"跳跃: {IsJumping}, 奔跑: {IsRunning}");
        GUILayout.Label($"虚拟控制: {enableVirtualControls}");
        
        if (GUILayout.Button("切换到桌面模式"))
        {
            SwitchInputMode(InputMode.Desktop);
        }
        if (GUILayout.Button("切换到移动模式"))
        {
            SwitchInputMode(InputMode.Mobile);
        }
        if (GUILayout.Button("切换到自动模式"))
        {
            SwitchInputMode(InputMode.Auto);
        }
        
        GUILayout.EndArea();
    }
    
    #endregion
}

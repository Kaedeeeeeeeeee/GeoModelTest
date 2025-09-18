using UnityEngine;
using UnityEngine.InputSystem;
using System;
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
    public float touchSensitivity = 2.0f;
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
    
    // 设备检测
    private bool isMobileDevice;
    private bool hasTouch;
    
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
        // 处理不同输入源
        switch (currentInputMode)
        {
            case InputMode.Auto:
                if (desktopTestMode)
                {
                    // 桌面测试模式：只处理移动端输入，禁用桌面输入避免冲突
                    ProcessMobileInput();
                }
                else if (isMobileDevice || hasTouch)
                {
                    ProcessMobileInput();
                }
                else
                {
                    ProcessDesktopInput();
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
        // 检测是否为移动设备
        isMobileDevice = Application.isMobilePlatform;
        
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
            if (isMobileDevice || hasTouch)
            {
                // 移动设备或支持触摸的设备
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
        if (Keyboard.current == null) return;
        
        // 移动输入 (WASD)
        Vector2 move = Vector2.zero;
        if (Keyboard.current.wKey.isPressed) move.y = 1;
        if (Keyboard.current.sKey.isPressed) move.y = -1;
        if (Keyboard.current.aKey.isPressed) move.x = -1;
        if (Keyboard.current.dKey.isPressed) move.x = 1;
        
        SetMoveInput(move);
        
        // 鼠标视角控制
        if (Mouse.current != null)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            SetLookInput(mouseDelta);
        }
        
        // 跳跃输入
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            SetJumpInput(true);
        }
        else if (Keyboard.current.spaceKey.wasReleasedThisFrame)
        {
            SetJumpInput(false);
        }
        
        // 奔跑输入
        SetRunInput(Keyboard.current.leftShiftKey.isPressed);
    }
    
    /// <summary>
    /// 处理移动端输入 (触摸)
    /// </summary>
    void ProcessMobileInput()
    {
        if (!enableTouchInput || Touchscreen.current == null) return;
        
        // 处理触摸输入
        var touches = Touchscreen.current.touches;
        
        if (touches.Count > 0)
        {
            ProcessPrimaryTouch(touches[0]);
        }
        
        // 处理多点触控
        if (touches.Count > 1)
        {
            ProcessSecondaryTouch(touches[1]);
        }
    }
    
    /// <summary>
    /// 处理主触摸点 (通常用于视角控制)
    /// </summary>
    void ProcessPrimaryTouch(UnityEngine.InputSystem.Controls.TouchControl touch)
    {
        Vector2 touchPosition = touch.position.ReadValue();
        TouchPhase phase = touch.phase.ReadValue();
        
        switch (phase)
        {
            case TouchPhase.Began:
                lastTouchPosition = touchPosition;
                touchStartPosition = touchPosition;
                touchStartTime = Time.time;
                isDragging = false;
                
                if (enableDebugLog)
                    Debug.Log($"[MobileInputManager] 触摸开始: {touchPosition}");
                break;
                
            case TouchPhase.Moved:
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
                
            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                if (!isDragging)
                {
                    // 短触摸 - 可能是点击事件
                    float touchDuration = Time.time - touchStartTime;
                    if (touchDuration < 0.3f) // 300ms内算作点击
                    {
                        ProcessTouchTap(touchStartPosition);
                    }
                }
                
                isDragging = false;
                SetLookInput(Vector2.zero);
                
                if (enableDebugLog)
                    Debug.Log($"[MobileInputManager] 触摸结束");
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
    void ProcessTouchTap(Vector2 position)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[MobileInputManager] 检测到点击: {position}");
        }
        
        // 这里可以处理UI点击检测等
        // 如果点击的不是UI元素，可以触发交互事件
        if (!IsPointOverUI(position))
        {
            OnInteractInput?.Invoke(true);
            OnInteractInput?.Invoke(false); // 立即释放
        }
    }
    
    /// <summary>
    /// 处理通用输入 (快捷键等)
    /// </summary>
    void ProcessCommonInput()
    {
        if (Keyboard.current == null) return;
        
        // 背包快捷键
        if (Keyboard.current.iKey.wasPressedThisFrame)
        {
            OnInventoryInput?.Invoke();
        }
        
        // 仓库快捷键已移除，F键现在专用于交互
        
        // 工具轮盘快捷键
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            OnToolWheelInput?.Invoke();
        }

        // 图鉴快捷键
        if (Keyboard.current.oKey.wasPressedThisFrame)
        {
            OnEncyclopediaInput?.Invoke();
        }
        
        // 交互键 (E键)
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            OnInteractInput?.Invoke(true);
        }
        else if (Keyboard.current.eKey.wasReleasedThisFrame)
        {
            OnInteractInput?.Invoke(false);
        }

        // F键交互
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            OnSecondaryInteractInput?.Invoke(true);
        }
        else if (Keyboard.current.fKey.wasReleasedThisFrame)
        {
            OnSecondaryInteractInput?.Invoke(false);
        }
    }
    
    /// <summary>
    /// 检查点击位置是否在UI上
    /// </summary>
    bool IsPointOverUI(Vector2 screenPosition)
    {
        // 使用Unity的UI系统检测
        return UnityEngine.EventSystems.EventSystem.current != null &&
               UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
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
        IsInteracting = isPressed;
        OnInteractInput?.Invoke(isPressed);
    }

    /// <summary>
    /// 设置F键交互输入
    /// </summary>
    public void SetSecondaryInteractInput(bool isPressed)
    {
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
        return isMobileDevice || hasTouch;
    }
    
    /// <summary>
    /// 获取是否应该显示虚拟控制
    /// </summary>
    public bool ShouldShowVirtualControls()
    {
        return enableVirtualControls && (currentInputMode == InputMode.Mobile || 
               (currentInputMode == InputMode.Auto && IsMobileDevice()) ||
               desktopTestMode);
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

        // 首先检查并初始化单例
        EnsureSingletons();

        Debug.Log("[MobileInputManager] 开始查找EncyclopediaUI组件...");

        // 直接调用EncyclopediaUI（包括非激活的）
        var encyclopediaUI = FindObjectOfType<Encyclopedia.EncyclopediaUI>(true);
        if (encyclopediaUI != null)
        {
            Debug.Log($"[MobileInputManager] 找到EncyclopediaUI在 {encyclopediaUI.gameObject.name}，激活状态: {encyclopediaUI.gameObject.activeInHierarchy}");
            Debug.Log("[MobileInputManager] 调用ToggleEncyclopedia");
            encyclopediaUI.ToggleEncyclopedia();
        }
        else
        {
            Debug.LogWarning("[MobileInputManager] 未找到EncyclopediaUI组件");

            // 尝试查找其他可能的图鉴组件
            var simpleManager = FindObjectOfType<Encyclopedia.SimpleEncyclopediaManager>(true);
            if (simpleManager != null)
            {
                Debug.Log("[MobileInputManager] 找到SimpleEncyclopediaManager，调用ToggleEncyclopedia");
                simpleManager.ToggleEncyclopedia();
            }
            else
            {
                Debug.LogWarning("[MobileInputManager] 也未找到SimpleEncyclopediaManager组件");
            }
        }
    }

    /// <summary>
    /// 确保图鉴系统单例正确初始化
    /// </summary>
    private void EnsureSingletons()
    {
        Debug.Log("[MobileInputManager] 检查单例状态...");

        // 检查并修复EncyclopediaData单例
        if (Encyclopedia.EncyclopediaData.Instance == null)
        {
            var encyclopediaData = FindObjectOfType<Encyclopedia.EncyclopediaData>();
            if (encyclopediaData != null)
            {
                Debug.Log("[MobileInputManager] EncyclopediaData组件存在但Instance为null，尝试修复...");

                // 确保组件启用
                if (!encyclopediaData.gameObject.activeInHierarchy)
                    encyclopediaData.gameObject.SetActive(true);
                if (!encyclopediaData.enabled)
                    encyclopediaData.enabled = true;

                // 手动调用Awake方法初始化单例
                var awakeMethod = typeof(Encyclopedia.EncyclopediaData).GetMethod("Awake",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (awakeMethod != null)
                {
                    try
                    {
                        awakeMethod.Invoke(encyclopediaData, null);
                        Debug.Log("[MobileInputManager] 已手动调用EncyclopediaData.Awake方法");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[MobileInputManager] 调用Awake方法失败: {e.Message}");
                    }
                }
            }
        }
        else
        {
            Debug.Log("[MobileInputManager] EncyclopediaData.Instance 正常");
        }

        // 检查并修复CollectionManager单例
        if (Encyclopedia.CollectionManager.Instance == null)
        {
            var collectionManager = FindObjectOfType<Encyclopedia.CollectionManager>();
            if (collectionManager != null)
            {
                Debug.Log("[MobileInputManager] CollectionManager组件存在但Instance为null，尝试修复...");

                // 确保组件启用
                if (!collectionManager.gameObject.activeInHierarchy)
                    collectionManager.gameObject.SetActive(true);
                if (!collectionManager.enabled)
                    collectionManager.enabled = true;

                // 手动调用Awake方法初始化单例
                var awakeMethod = typeof(Encyclopedia.CollectionManager).GetMethod("Awake",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (awakeMethod != null)
                {
                    try
                    {
                        awakeMethod.Invoke(collectionManager, null);
                        Debug.Log("[MobileInputManager] 已手动调用CollectionManager.Awake方法");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[MobileInputManager] 调用CollectionManager Awake方法失败: {e.Message}");
                    }
                }
            }
        }
        else
        {
            Debug.Log("[MobileInputManager] CollectionManager.Instance 正常");
        }

        // 验证数据可访问性
        if (Encyclopedia.EncyclopediaData.Instance != null)
        {
            try
            {
                var count = Encyclopedia.EncyclopediaData.Instance.AllEntries?.Count ?? 0;
                Debug.Log($"[MobileInputManager] 数据验证: {count} 个条目可访问");

                if (count == 0)
                {
                    Debug.LogWarning("[MobileInputManager] 条目数据为0，可能需要重新加载数据");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MobileInputManager] 数据访问错误: {e.Message}");
            }
        }

        // 验证收集系统
        if (Encyclopedia.CollectionManager.Instance != null)
        {
            try
            {
                var stats = Encyclopedia.CollectionManager.Instance.CurrentStats;
                if (stats != null)
                {
                    Debug.Log($"[MobileInputManager] 收集系统验证: {stats.totalEntries} 个总条目");
                }
                else
                {
                    Debug.LogWarning("[MobileInputManager] 收集统计数据为空");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MobileInputManager] 收集系统访问错误: {e.Message}");
            }
        }
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
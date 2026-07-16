using GuidanceSystem;
using StorySystem;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 3.0f;
    public float runSpeed = 6.0f;
    public float jumpHeight = 1.0f;
    public float gravity = -15.0f;

    [Header("Look Settings")]
    public float mouseSensitivity = 8.0f;
    public float maxLookAngle = 80.0f;
    public bool enableMouseLook = true; // 控制是否启用鼠标视角控制

    [Header("摄像机设置")]
    public Vector3 cameraPosition = new Vector3(0f, 1.0f, 0.065f); // 摄像机相对于玩家的位置（眼高）
    public float nearClipPlane = 0.1f; // 近裁剪面
    public bool hidePlayerFromCamera = true; // 是否隐藏玩家模型

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask = 1;

    [Header("手部位置设置")]
    public Transform rightHandAnchor;  // 右手锚点
    public Transform leftHandAnchor;   // 左手锚点（备用）
    public Vector3 rightHandOffset = new Vector3(0.18f, -1.13f, 0.3f);  // 右手偏移
    public Vector3 rightHandRotation = Vector3.zero;  // 右手旋转
    public Vector3 leftHandOffset = new Vector3(-0.5f, -0.4f, 0.3f);  // 左手偏移
    public Vector3 leftHandRotation = Vector3.zero;   // 左手旋转

    private CharacterController controller;
    private Camera playerCamera;
    private Vector3 velocity;
    private bool isGrounded;
    private float xRotation = 0f;

    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool runInput;
    private bool jumpInput;
    private bool jumpEnabled = true; // 控制是否允许跳跃

    [Header("移动端适配")]
    public bool enableMobileInput = true; // 启用移动端输入支持
    public bool prioritizeMobileInput = false; // 移动端输入优先级高于桌面端

    // 移动端输入管理器引用
    private MobileInputManager mobileInputManager;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();

        if (playerCamera == null)
        {

        }

        // 初始化手部锚点系统
        InitializeHandAnchors();

        // 设置摄像机位置和参数
        SetupCamera();

        // 设置玩家模型可见性
        SetupPlayerVisibility();

        // 初始化移动端输入管理器
        InitializeMobileInput();

        ApplyLookSensitivity(GamePerformanceSettings.LoadLookSensitivity(mouseSensitivity));
        GamePerformanceSettings.LookSensitivityChanged += ApplyLookSensitivity;

        // 桌面测试模式下不锁定鼠标，允许点击虚拟控件
        SetCursorLockState();

        GuidanceManager.Instance?.RegisterPlayer(transform);
    }

    void Update()
    {
        bool blockInput = StoryDirector.SubtitleUI.IsPlayerInputBlocked;
        if (blockInput)
        {
            SuppressPlayerInputs();
        }
        else
        {
            HandleInput();
        }

        GroundCheck();
        HandleMovement();

        if (!blockInput && enableMouseLook)
        {
            HandleLook();
        }
        else
        {
            lookInput = Vector2.zero;
        }

        // 检测桌面测试模式状态变化，动态更新鼠标锁定状态
        CheckDesktopTestModeChange();
    }

    private bool lastDesktopTestMode = false;
    private bool lastMobileInputMode = false;

    /// <summary>
    /// 检测桌面测试模式状态变化
    /// </summary>
    void CheckDesktopTestModeChange()
    {
        bool currentDesktopTestMode = mobileInputManager != null && mobileInputManager.desktopTestMode;
        bool currentMobileInputMode = HasActiveMobileInputSurface();

        if (currentDesktopTestMode != lastDesktopTestMode || currentMobileInputMode != lastMobileInputMode)
        {
            SetCursorLockState();

            if (!Application.isMobilePlatform)
            {
                Debug.Log($"[FirstPersonController] 桌面输入状态变化 - 虚拟控件可用: {currentMobileInputMode}, 桌面测试: {currentDesktopTestMode}");
            }

            lastDesktopTestMode = currentDesktopTestMode;
            lastMobileInputMode = currentMobileInputMode;
        }

        // 注释掉复杂的强制解锁逻辑，改用ESC键手动控制
    }

    void HandleInput()
    {
        if (ShouldUseMobileInputOnly())
        {
            HandleMobileInput();
            return;
        }

        HandleDesktopInput();
        ApplyMobileInputOverlay();
    }

    bool ShouldUseMobileInputOnly()
    {
        if (!enableMobileInput || mobileInputManager == null)
        {
            return false;
        }

        if (IsRuntimeMobileInputDevice())
        {
            return true;
        }

        return prioritizeMobileInput && !HasDesktopInputDevice();
    }

    bool HasActiveMobileInputSurface()
    {
        return enableMobileInput &&
               mobileInputManager != null &&
               (IsRuntimeMobileInputDevice() ||
                mobileInputManager.desktopTestMode ||
                mobileInputManager.ShouldShowVirtualControls());
    }

    bool HasDesktopInputDevice()
    {
        return Keyboard.current != null || Mouse.current != null;
    }

    bool IsRuntimeMobileInputDevice()
    {
        if (mobileInputManager != null)
        {
            return mobileInputManager.IsMobileDevice();
        }

        return Application.isMobilePlatform;
    }

    /// <summary>
    /// 处理移动端输入
    /// </summary>
    void HandleMobileInput()
    {
        if (mobileInputManager == null)
        {
            SuppressPlayerInputs();
            return;
        }

        moveInput = mobileInputManager.MoveInput;
        lookInput = enableMouseLook ? mobileInputManager.LookInput : Vector2.zero;
        runInput = mobileInputManager.IsRunning;

        if (mobileInputManager.IsJumping && jumpEnabled)
        {
            jumpInput = true;
        }
    }

    /// <summary>
    /// 处理桌面端输入
    /// </summary>
    void HandleDesktopInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            moveInput = Vector2.zero;
            runInput = false;
            jumpInput = false;
        }
        else
        {
            Vector2 move = Vector2.zero;
            if (keyboard.wKey.isPressed) move.y = 1;
            if (keyboard.sKey.isPressed) move.y = -1;
            if (keyboard.aKey.isPressed) move.x = -1;
            if (keyboard.dKey.isPressed) move.x = 1;

            moveInput = move;
            runInput = keyboard.leftShiftKey.isPressed;

            if (keyboard.spaceKey.wasPressedThisFrame && jumpEnabled)
            {
                jumpInput = true;
                Debug.Log("空格键按下：跳跃输入已设置");
            }
            else if (keyboard.spaceKey.wasPressedThisFrame && !jumpEnabled)
            {
                Debug.Log("空格键按下：跳跃已禁用，忽略输入");
            }

            if (keyboard.gKey.wasPressedThisFrame)
            {
                DebugHandPositions();
            }

            // ESC键切换鼠标锁定状态（方便调试和退出）
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                ToggleCursorLock();
            }
        }

        // 鼠标视角控制 - 简化逻辑
        var mouse = Mouse.current;
        if (mouse != null && enableMouseLook)
        {
            Vector2 mouseLook = mouse.delta.ReadValue();
            lookInput = mouseLook;
        }
        else
        {
            lookInput = Vector2.zero;
        }
    }

    void ApplyMobileInputOverlay()
    {
        if (!enableMobileInput || mobileInputManager == null ||
            (!HasActiveMobileInputSurface() && !mobileInputManager.HasActiveGameplayInput()))
        {
            return;
        }

        Vector2 mobileMove = mobileInputManager.MoveInput;
        if (mobileMove.sqrMagnitude > 0.01f)
        {
            moveInput = mobileMove;
        }

        Vector2 mobileLook = mobileInputManager.LookInput;
        if (enableMouseLook && mobileLook.sqrMagnitude > 0.01f)
        {
            lookInput = mobileLook;
        }

        if (mobileInputManager.IsRunning)
        {
            runInput = true;
        }

        if (mobileInputManager.IsJumping && jumpEnabled)
        {
            jumpInput = true;
        }
    }

    void SuppressPlayerInputs()
    {
        moveInput = Vector2.zero;
        lookInput = Vector2.zero;
        runInput = false;
        jumpInput = false;
    }

    void GroundCheck()
    {
        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        }
        else
        {
            isGrounded = controller.isGrounded;
        }

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
    }

    private float footstepTimer = 0f;
    private const float WalkStepInterval = 0.45f;
    private const float RunStepInterval = 0.28f;

    void HandleMovement()
    {
        float currentSpeed = runInput ? runSpeed : walkSpeed;

        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;

        controller.Move(move * currentSpeed * Time.deltaTime);

        if (jumpInput && isGrounded && jumpEnabled)  // 三重检查：输入、接地、允许跳跃
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        jumpInput = false;

        // 脚步声：仅在地面、有水平移动时触发
        if (isGrounded && moveInput.sqrMagnitude > 0.01f)
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0f)
            {
                GeoModel.AudioSystem.AudioManager.Instance.PlaySFXRandom(
                    GeoModel.AudioSystem.AudioKeys.SFX.Footsteps, transform.position, 0.6f);
                footstepTimer = runInput ? RunStepInterval : WalkStepInterval;
            }
        }
        else
        {
            footstepTimer = 0f;
        }
    }

    void HandleLook()
    {
        if (playerCamera == null) return;

        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        if (enableMouseLook)
        {
            lookInput = context.ReadValue<Vector2>();
        }
        else
        {
            lookInput = Vector2.zero;
        }
    }

    public void SetMouseLookEnabled(bool enabled)
    {
        enableMouseLook = enabled;
        if (!enabled)
        {
            lookInput = Vector2.zero;
        }
    }

    public void ApplyLookSensitivity(float sensitivity)
    {
        mouseSensitivity = Mathf.Clamp(sensitivity, GamePerformanceSettings.MinLookSensitivity, GamePerformanceSettings.MaxLookSensitivity);
    }

    public void OnRun(InputAction.CallbackContext context)
    {
        runInput = context.ReadValueAsButton();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && jumpEnabled)  // 只有在允许跳跃时才处理跳跃输入
        {
            jumpInput = true;
        }
    }

    /// <summary>
    /// 启用或禁用跳跃功能（用于切割系统等需要禁用跳跃的场景）
    /// </summary>
    /// <param name="enabled">是否允许跳跃</param>
    public void SetJumpEnabled(bool enabled)
    {
        jumpEnabled = enabled;
        if (!enabled)
        {
            jumpInput = false; // 禁用时清除当前的跳跃输入
        }
        Debug.Log($"跳跃功能已{(enabled ? "启用" : "禁用")}");
    }

    /// <summary>
    /// 检查跳跃是否被启用
    /// </summary>
    /// <returns>跳跃是否被启用</returns>
    public bool IsJumpEnabled()
    {
        return jumpEnabled;
    }

    /// <summary>
    /// 设置摄像机位置和参数
    /// </summary>
    void SetupCamera()
    {
        if (playerCamera != null)
        {
            // 设置摄像机相对于玩家的位置
            playerCamera.transform.localPosition = cameraPosition;

            // 设置近裁剪面
            playerCamera.nearClipPlane = nearClipPlane;

            Debug.Log($"摄像机位置已设置为: {cameraPosition}, 近裁剪面: {nearClipPlane}");
        }
        else
        {
            Debug.LogWarning("未找到摄像机，无法设置位置");
        }
    }

    /// <summary>
    /// 设置玩家模型可见性
    /// </summary>
    void SetupPlayerVisibility()
    {
        if (hidePlayerFromCamera)
        {
            // 方法1：将玩家模型设置到"Player"层，然后让摄像机忽略这个层
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer == -1)
            {
                // 如果没有Player层，使用第8层作为玩家层
                playerLayer = 8;
                Debug.Log("使用第8层作为玩家层");
            }

            // 将玩家和所有子对象（除了摄像机）设置到玩家层
            SetLayerRecursively(gameObject, playerLayer, new string[] { "Camera" });

            // 让摄像机忽略玩家层
            if (playerCamera != null)
            {
                playerCamera.cullingMask &= ~(1 << playerLayer);
                Debug.Log($"摄像机已设置为忽略第{playerLayer}层（玩家层）");
            }
        }

        Debug.Log($"玩家模型可见性设置完成，隐藏模式: {hidePlayerFromCamera}");
    }

    /// <summary>
    /// 递归设置游戏对象及其子对象的层级（排除指定组件）
    /// </summary>
    void SetLayerRecursively(GameObject obj, int layer, string[] excludeComponents = null)
    {
        // 检查是否需要排除此对象
        bool shouldExclude = false;
        if (excludeComponents != null)
        {
            foreach (string componentName in excludeComponents)
            {
                if (obj.GetComponent(componentName) != null)
                {
                    shouldExclude = true;
                    break;
                }
            }
        }

        // 如果不需要排除，设置层级
        if (!shouldExclude)
        {
            obj.layer = layer;
        }

        // 递归设置所有子对象
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer, excludeComponents);
        }
    }

    /// <summary>
    /// 初始化手部锚点系统
    /// </summary>
    void InitializeHandAnchors()
    {
        // 创建或设置右手锚点
        if (rightHandAnchor == null)
        {
            GameObject rightHand = new GameObject("RightHandAnchor");
            rightHand.transform.SetParent(playerCamera.transform);
            rightHand.transform.localPosition = rightHandOffset;
            rightHand.transform.localRotation = Quaternion.Euler(rightHandRotation);
            rightHandAnchor = rightHand.transform;

            Debug.Log($"自动创建右手锚点: 位置={rightHandOffset}, 旋转={rightHandRotation}");
        }
        else
        {
            // 如果已经存在，应用配置的偏移
            rightHandAnchor.localPosition = rightHandOffset;
            rightHandAnchor.localRotation = Quaternion.Euler(rightHandRotation);
        }

        // 创建或设置左手锚点
        if (leftHandAnchor == null)
        {
            GameObject leftHand = new GameObject("LeftHandAnchor");
            leftHand.transform.SetParent(playerCamera.transform);
            leftHand.transform.localPosition = leftHandOffset;
            leftHand.transform.localRotation = Quaternion.Euler(leftHandRotation);
            leftHandAnchor = leftHand.transform;

            Debug.Log($"自动创建左手锚点: 位置={leftHandOffset}, 旋转={leftHandRotation}");
        }
        else
        {
            // 如果已经存在，应用配置的偏移
            leftHandAnchor.localPosition = leftHandOffset;
            leftHandAnchor.localRotation = Quaternion.Euler(leftHandRotation);
        }
    }

    /// <summary>
    /// 调试手部位置信息
    /// </summary>
    void DebugHandPositions()
    {
        Debug.Log("=== 手部位置调试信息 ===");

        if (rightHandAnchor != null)
        {
            Debug.Log($"右手锚点:");
            Debug.Log($"  本地位置: {rightHandAnchor.localPosition}");
            Debug.Log($"  本地旋转: {rightHandAnchor.localEulerAngles}");
            Debug.Log($"  世界位置: {rightHandAnchor.position}");
            Debug.Log($"  世界旋转: {rightHandAnchor.eulerAngles}");
        }

        if (leftHandAnchor != null)
        {
            Debug.Log($"左手锚点:");
            Debug.Log($"  本地位置: {leftHandAnchor.localPosition}");
            Debug.Log($"  本地旋转: {leftHandAnchor.localEulerAngles}");
            Debug.Log($"  世界位置: {leftHandAnchor.position}");
            Debug.Log($"  世界旋转: {leftHandAnchor.eulerAngles}");
        }

        Debug.Log("提示：在Inspector中调整rightHandOffset和rightHandRotation数值");
    }

    /// <summary>
    /// 获取指定手部锚点
    /// </summary>
    public Transform GetHandAnchor(bool isRightHand = true)
    {
        return isRightHand ? rightHandAnchor : leftHandAnchor;
    }

    /// <summary>
    /// 更新手部锚点位置（运行时调用）
    /// </summary>
    public void UpdateHandAnchors()
    {
        if (rightHandAnchor != null)
        {
            rightHandAnchor.localPosition = rightHandOffset;
            rightHandAnchor.localRotation = Quaternion.Euler(rightHandRotation);
        }

        if (leftHandAnchor != null)
        {
            leftHandAnchor.localPosition = leftHandOffset;
            leftHandAnchor.localRotation = Quaternion.Euler(leftHandRotation);
        }
    }

    private void OnDestroy()
    {
        GamePerformanceSettings.LookSensitivityChanged -= ApplyLookSensitivity;

        if (GuidanceManager.Current != null)
        {
            GuidanceManager.Current.RegisterPlayer(null);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }

        // 绘制手部锚点位置（仅在Scene视图中可见）
        if (rightHandAnchor != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(rightHandAnchor.position, 0.05f);
            Gizmos.DrawLine(rightHandAnchor.position, rightHandAnchor.position + rightHandAnchor.forward * 0.1f);
        }

        if (leftHandAnchor != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(leftHandAnchor.position, 0.05f);
            Gizmos.DrawLine(leftHandAnchor.position, leftHandAnchor.position + leftHandAnchor.forward * 0.1f);
        }
    }

    /// <summary>
    /// 初始化移动端输入管理器
    /// </summary>
    void InitializeMobileInput()
    {
        if (!enableMobileInput)
        {
            Debug.Log("[FirstPersonController] 移动端输入支持已禁用");
            return;
        }

        // 查找移动端输入管理器
        mobileInputManager = MobileInputManager.Instance;

        if (mobileInputManager == null)
        {
            // 如果没有找到，尝试在场景中查找
            mobileInputManager = FindFirstObjectByType<MobileInputManager>();

            if (mobileInputManager == null)
            {
                Debug.LogWarning("[FirstPersonController] 未找到MobileInputManager，移动端输入功能将不可用");
                Debug.LogWarning("请确保场景中有MobileInputManager组件，或者禁用enableMobileInput");
                return;
            }
        }

        // 根据设备类型和桌面测试模式自动调整设置
        if (IsRuntimeMobileInputDevice())
        {
            prioritizeMobileInput = true;
            Debug.Log("[FirstPersonController] 检测到移动设备，启用移动端输入优先模式");
        }
        else
        {
            bool shouldShowVirtualControls = mobileInputManager != null && mobileInputManager.ShouldShowVirtualControls();
            prioritizeMobileInput = false;

            Debug.Log($"[FirstPersonController] 桌面设备 - 使用键鼠主输入，虚拟控件混合: {shouldShowVirtualControls}");
        }

        Debug.Log($"[FirstPersonController] 移动端输入初始化完成 - 优先级: {prioritizeMobileInput}");
    }

    /// <summary>
    /// 设置移动端输入模式
    /// </summary>
    public void SetMobileInputMode(bool enabled, bool priority = false)
    {
        enableMobileInput = enabled;
        prioritizeMobileInput = priority;

        if (enabled && mobileInputManager == null)
        {
            InitializeMobileInput();
        }

        Debug.Log($"[FirstPersonController] 移动端输入模式更新 - 启用: {enabled}, 优先: {priority}");
    }

    /// <summary>
    /// 设置鼠标光标锁定状态
    /// </summary>
    void SetCursorLockState()
    {
        if (ShouldKeepCursorFreeForMobileControls())
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Debug.Log("[FirstPersonController] 触摸/虚拟控件输入模式，鼠标未锁定。");
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Debug.Log("[FirstPersonController] 鼠标已锁定并隐藏，用于视角控制。按ESC切换锁定状态。");
    }

    bool ShouldKeepCursorFreeForMobileControls()
    {
        return IsRuntimeMobileInputDevice() ||
               (mobileInputManager != null && mobileInputManager.desktopTestMode) ||
               (HasActiveMobileInputSurface() && Keyboard.current == null);
    }

    /// <summary>
    /// 公开方法：更新鼠标锁定状态
    /// </summary>
    public void UpdateCursorLockState()
    {
        SetCursorLockState();
    }

    /// <summary>
    /// 切换鼠标锁定状态（ESC键功能）
    /// </summary>
    public void ToggleCursorLock()
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Debug.Log("[FirstPersonController] 鼠标已解锁 - 按ESC再次锁定");
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Debug.Log("[FirstPersonController] 鼠标已锁定 - 按ESC解锁");
        }
    }

    /// <summary>
    /// 获取当前输入状态信息（调试用）
    /// </summary>
    public string GetInputStatusInfo()
    {
        return $"移动输入: {moveInput}, 视角输入: {lookInput}, 跳跃: {jumpInput}, 奔跑: {runInput}\n" +
               $"移动端支持: {enableMobileInput}, 移动端优先: {prioritizeMobileInput}\n" +
               $"移动端管理器: {(mobileInputManager != null ? "可用" : "不可用")}";
    }
}

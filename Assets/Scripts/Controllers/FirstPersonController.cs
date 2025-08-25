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
    public Vector3 cameraPosition = new Vector3(0f, 0.452f, 0.065f); // 摄像机相对于玩家的位置
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
        
        Cursor.lockState = CursorLockMode.Locked;
    }
    
    void Update()
    {
        HandleInput();
        GroundCheck();
        HandleMovement();
        HandleLook();
    }
    
    void HandleInput()
    {
        if (Keyboard.current != null)
        {
            Vector2 move = Vector2.zero;
            if (Keyboard.current.wKey.isPressed) move.y = 1;
            if (Keyboard.current.sKey.isPressed) move.y = -1;
            if (Keyboard.current.aKey.isPressed) move.x = -1;
            if (Keyboard.current.dKey.isPressed) move.x = 1;
            moveInput = move;
            
            runInput = Keyboard.current.leftShiftKey.isPressed;
            
            if (Keyboard.current.spaceKey.wasPressedThisFrame && jumpEnabled)  // 也需要检查jumpEnabled
            {
                jumpInput = true;
                Debug.Log("空格键按下：跳跃输入已设置");
            }
            else if (Keyboard.current.spaceKey.wasPressedThisFrame && !jumpEnabled)
            {
                Debug.Log("空格键按下：跳跃已禁用，忽略输入");
            }
            
            // 调试快捷键：G键显示手部位置信息
            if (Keyboard.current.gKey.wasPressedThisFrame)
            {
                DebugHandPositions();
            }
        }
        
        if (Mouse.current != null && enableMouseLook)
        {
            lookInput = Mouse.current.delta.ReadValue();
        }
        else
        {
            lookInput = Vector2.zero; // 禁用鼠标视角时，清零输入
        }
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
}
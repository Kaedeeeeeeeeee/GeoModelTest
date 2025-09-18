using UnityEngine;

public class DrillCarController : MonoBehaviour
{
    [Header("Drill Car Behavior")]
    public bool isActive = true;
    public float drillAnimationSpeed = 2f;
    public GameObject drillPart;
    
    [Header("Driving Settings")]
    public float interactionRange = 3f;
    public float driveSpeed = 50f;  // 增加驱动力
    public float turnSpeed = 90f;
    public Transform playerSeatPosition; // 玩家坐车时的位置
    
    [Header("Camera Settings")]
    public Transform cameraPosition; // 第三人称摄像机位置
    public float cameraDistance = 4f; // 摄像机距离车辆的距离
    public float cameraHeight = 2f; // 摄像机高度
    
    private Vector3 originalPosition;
    private bool isDrilling = false;
    private float drillTimer = 0f;
    private AudioSource audioSource;
    
    // 驾驶相关变量
    private bool isBeingDriven = false;
    private FirstPersonController playerController;
    private Camera playerCamera;
    private Vector3 originalCameraPosition;
    private MobileInputManager mobileInputManager; // 移动端输入管理器
    private bool wasFKeyPressedLastFrame = false; // 上一帧F键状态
    private Quaternion originalCameraRotation;
    private Rigidbody rb;
    
    void Start()
    {
        originalPosition = transform.position;
        audioSource = GetComponent<AudioSource>();
        
        // 自动设置钻探部件
        if (drillPart == null)
        {
            Transform drillTransform = transform.Find("Drill");
            if (drillTransform != null)
            {
                drillPart = drillTransform.gameObject;
            }
        }
        
        // 获取Rigidbody组件
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        // 强制设置Rigidbody参数（无论是否已存在）
        rb.mass = 100f;  // 减小质量
        rb.centerOfMass = new Vector3(0, -0.5f, 0);
        rb.linearDamping = 1f;  // 减少线性阻力
        rb.angularDamping = 5f; // 减少角度阻力
        rb.isKinematic = true; // 初始为运动学模式
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // 获取移动端输入管理器
        mobileInputManager = MobileInputManager.Instance;
        if (mobileInputManager == null)
        {
            mobileInputManager = FindObjectOfType<MobileInputManager>();
        }

        // 摄像机位置将在运行时计算
    }

    /// <summary>
    /// 检测F键输入 - 支持键盘和移动端虚拟按钮
    /// </summary>
    bool IsFKeyPressed()
    {
        // 键盘F键检测
        bool keyboardFPressed = UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.fKey.wasPressedThisFrame;

        // 移动端F键检测
        bool mobileFPressed = false;
        if (mobileInputManager != null)
        {
            bool currentFKeyState = mobileInputManager.IsSecondaryInteracting;
            mobileFPressed = currentFKeyState && !wasFKeyPressedLastFrame;
            wasFKeyPressedLastFrame = currentFKeyState;
        }

        return keyboardFPressed || mobileFPressed;
    }
    
    void Update()
    {
        // 检查玩家交互
        if (!isBeingDriven)
        {
            CheckForPlayerInteraction();
        }
        else
        {
            HandleDriving();
            UpdateCameraFollow();
        }
        
        // 钻探动画
        if (isDrilling)
        {
            drillTimer += Time.deltaTime * drillAnimationSpeed;
        }
        
        if (drillPart != null)
        {
            drillPart.transform.Rotate(Vector3.up * 360f * Time.deltaTime * drillAnimationSpeed);
        }
        
        // 不强制设置位置，让物理系统正常工作
        // 钻探车会通过Rigidbody自然落地并保持静止
    }
    
    public void StartDrilling()
    {
        isDrilling = true;
        isActive = true;
        
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Play();
        }
    }
    
    public void StopDrilling()
    {
        isDrilling = false;
        isActive = false;
        
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }
    
    void CheckForPlayerInteraction()
    {
        if (playerController != null) return; // 已经有玩家在驾驶
        
        // 查找附近的玩家
        FirstPersonController nearbyPlayer = FindFirstObjectByType<FirstPersonController>();
        if (nearbyPlayer != null)
        {
            float distance = Vector3.Distance(transform.position, nearbyPlayer.transform.position);
            if (distance <= interactionRange)
            {
                // 显示交互提示（可以考虑后续添加UI提示）
                if (IsFKeyPressed())
                {
                    StartDriving(nearbyPlayer);
                }
            }
        }
    }
    
    void StartDriving(FirstPersonController player)
    {
        if (isBeingDriven) return;
        
        playerController = player;
        playerCamera = Camera.main;
        isBeingDriven = true;
        
        // 保存玩家原始摄像机设置（相对位置）
        if (playerCamera != null && playerController != null)
        {
            originalCameraPosition = playerCamera.transform.localPosition;
            originalCameraRotation = playerCamera.transform.localRotation;
            
            Debug.Log($"保存摄像机状态 - 本地位置: {originalCameraPosition}, 本地旋转: {originalCameraRotation}");
        }
        
        // 禁用玩家的鼠标控制
        if (playerController != null)
        {
            playerController.enableMouseLook = false;
        }
        
        // 隐藏玩家
        if (playerController != null)
        {
            playerController.gameObject.SetActive(false);
        }
        
        // 设置摄像机跟随车辆
        SetupVehicleCamera();
        
        // 启用物理模式
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.mass = 100f;  // 确保质量正确
            rb.linearDamping = 2f;  // 增加阻力，减少滑动
            rb.angularDamping = 10f; // 增加角度阻力，减少旋转
            rb.useGravity = true;  // 确保受重力影响

            // 冻结X和Z轴旋转，防止翻车，但允许Y轴转向
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            // 清除任何现有的速度
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            Debug.Log($"[DrillCar] 启用物理模式 - 质量: {rb.mass}, 线性阻力: {rb.linearDamping}, 约束: {rb.constraints}");
        }
        
        Debug.Log("开始驾驶钻探车");
    }
    
    void SetupVehicleCamera()
    {
        if (playerCamera != null)
        {
            // 计算摄像机的世界位置（钻探车后方上方）
            Vector3 cameraWorldPos = transform.position + 
                                    Vector3.up * cameraHeight +
                                    transform.forward * (-cameraDistance);
            
            // 直接设置摄像机的世界位置
            playerCamera.transform.SetParent(null);
            playerCamera.transform.position = cameraWorldPos;
            
            // 让摄像机看向钻探车
            Vector3 lookTarget = transform.position + Vector3.up * 1f; // 稍微向上看
            playerCamera.transform.LookAt(lookTarget);
        }
    }
    
    void UpdateCameraFollow()
    {
        if (playerCamera == null || !isBeingDriven) return;
        
        // 计算摄像机应该在的位置
        Vector3 cameraWorldPos = transform.position + 
                                Vector3.up * cameraHeight +
                                transform.forward * (-cameraDistance);
        
        // 更新摄像机位置
        playerCamera.transform.position = cameraWorldPos;
        
        // 让摄像机看向钻探车
        Vector3 lookTarget = transform.position + Vector3.up * 1f;
        playerCamera.transform.LookAt(lookTarget);
    }
    
    void HandleDriving()
    {
        // 检查退出驾驶
        if (IsFKeyPressed())
        {
            StopDriving();
            return;
        }
        
        // 获取输入
        Vector2 moveInput = Vector2.zero;

        // 键盘输入
        if (UnityEngine.InputSystem.Keyboard.current.wKey.isPressed) moveInput.y = 1;
        if (UnityEngine.InputSystem.Keyboard.current.sKey.isPressed) moveInput.y = -1;
        if (UnityEngine.InputSystem.Keyboard.current.aKey.isPressed) moveInput.x = -1;
        if (UnityEngine.InputSystem.Keyboard.current.dKey.isPressed) moveInput.x = 1;

        // 移动端输入（优先级高于键盘）
        if (mobileInputManager != null && mobileInputManager.MoveInput.magnitude > 0.1f)
        {
            moveInput = mobileInputManager.MoveInput;
        }
        
        // 应用移动和旋转
        if (rb != null)
        {
            // 前进/后退
            Vector3 moveDirection = transform.forward * moveInput.y * driveSpeed;

            // 调试输出已简化

            // 车辆风格的移动控制
            Vector3 currentVelocity = rb.linearVelocity;

            // 只在水平面移动，保持Y轴速度（重力影响）
            float targetSpeedX = moveDirection.x * 1.0f;  // 调整到1.0f速度
            float targetSpeedZ = moveDirection.z * 1.0f;  // 调整到1.0f速度

            Vector3 targetVelocity = new Vector3(targetSpeedX, currentVelocity.y, targetSpeedZ);
            rb.linearVelocity = targetVelocity;

            // 速度设置完成
            
            // 转向
            if (Mathf.Abs(moveInput.y) > 0.1f) // 只有在移动时才能转向
            {
                float turn = moveInput.x * turnSpeed * Time.deltaTime;
                transform.Rotate(0, turn, 0);
            }
            
            // 限制最大速度
            if (rb.linearVelocity.magnitude > driveSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * driveSpeed;
            }
        }
    }
    
    void StopDriving()
    {
        if (!isBeingDriven) return;
        
        isBeingDriven = false;
        
        // 先显示玩家并设置位置
        if (playerController != null)
        {
            // 将玩家移到车的正后方
            Vector3 exitPosition = transform.position + transform.forward * (-3f); // 车后方3米
            playerController.transform.position = exitPosition;
            
            // 让玩家面向车辆（看向车屁股）
            Vector3 lookDirection = (transform.position - exitPosition).normalized;
            playerController.transform.rotation = Quaternion.LookRotation(lookDirection);
            
            playerController.gameObject.SetActive(true);
            
            // 重要：重新启用玩家的鼠标控制
            playerController.enableMouseLook = true;
            
            Debug.Log($"玩家已下车，位置: {exitPosition}，面向车辆");
        }
        
        // 恢复摄像机到玩家身上
        if (playerCamera != null && playerController != null)
        {
            // 将摄像机重新作为玩家的子对象
            playerCamera.transform.SetParent(playerController.transform);
            
            // 恢复摄像机的本地位置（使用保存的值或默认值）
            playerCamera.transform.localPosition = originalCameraPosition != Vector3.zero ? originalCameraPosition : new Vector3(0, 1.6f, 0);
            
            // 摄像机朝向与玩家一致（看向车辆）
            playerCamera.transform.localRotation = Quaternion.identity;
            
            // 激活摄像机
            playerCamera.enabled = true;
            
            Debug.Log($"摄像机已重新附加到玩家 - 本地位置: {playerCamera.transform.localPosition}，朝向车辆");
        }
        
        // 清理引用
        playerController = null;
        
        // 设置为运动学模式
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        Debug.Log("停止驾驶钻探车");
    }
    
    
    public void ToggleDrilling()
    {
        if (isDrilling)
        {
            StopDrilling();
        }
        else
        {
            StartDrilling();
        }
    }
    
    // Gizmos绘制
    void OnDrawGizmos()
    {
        // 绘制交互范围
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
        
        // 绘制摄像机位置
        if (isBeingDriven)
        {
            Vector3 cameraPos = transform.position + Vector3.up * cameraHeight + transform.forward * (-cameraDistance);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(cameraPos, Vector3.one * 0.2f);
            Gizmos.DrawLine(transform.position, cameraPos);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // 绘制详细信息
        Gizmos.color = Color.yellow;
        
        // 绘制车辆边界
        Gizmos.DrawWireCube(transform.position, new Vector3(2f, 1f, 4f));
        
        // 绘制前进方向
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.forward * 2f);
        
        // 绘制玩家座位位置
        if (playerSeatPosition != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(playerSeatPosition.position, 0.3f);
        }
    }
}
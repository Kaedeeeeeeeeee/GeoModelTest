using UnityEngine;

public class DroneController : MonoBehaviour
{
    [Header("Drone Behavior")]
    public float hoverHeight = 2f;
    public float hoverSpeed = 1f;
    public float rotationSpeed = 30f;
    
    [Header("Control Settings")]
    public float interactionRange = 3f;
    public float flySpeed = 8f;
    public float verticalSpeed = 5f;
    public float turnSpeed = 120f;
    
    [Header("Camera Settings")]
    public float cameraDistance = 4f;
    public float cameraHeight = 2f;
    
    [Header("Ground Detection")]
    public float groundCheckDistance = 1f;
    public LayerMask groundLayers = 1;
    
    private Vector3 originalPosition;
    private float timeOffset;
    
    // 控制相关变量
    private bool isBeingControlled = false;
    private FirstPersonController playerController;
    private Transform playerTransform;
    private Rigidbody droneRigidbody;
    
    // 摄像机相关变量
    private Camera playerCamera;
    private Transform originalCameraParent;
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    
    // 地面检测变量
    private bool isOnGround = false;
    
    void Start()
    {
        originalPosition = transform.position;
        timeOffset = Random.Range(0f, Mathf.PI * 2f);
        droneRigidbody = GetComponent<Rigidbody>();
        
        
    }
    
    void Update()
    {
        // 始终检测地面状态
        CheckGroundStatus();
        
        if (isBeingControlled)
        {
            HandleFlying();
            UpdateCameraFollow();
        }
        else
        {
            CheckForPlayerInteraction();
            PerformHoverBehavior();
            // PerformRotation(); // 移除旋转效果
        }
    }
    
    void PerformHoverBehavior()
    {
        // 不强制设置位置，让物理系统正常工作
        // 无人机会通过Rigidbody自然落地并保持静止
    }
    
    void PerformRotation()
    {
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
    }
    
    public void SetHoverHeight(float height)
    {
        hoverHeight = height;
        originalPosition.y = height;
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, 2f);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * hoverHeight);
        
        // 绘制交互范围
        if (!isBeingControlled)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, interactionRange);
        }
        
        // 绘制地面检测
        Gizmos.color = isOnGround ? Color.red : Color.green;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);
        
        // 绘制地面状态指示器
        if (isOnGround)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position + Vector3.down * 0.5f, Vector3.one * 0.3f);
        }
    }
    
    void CheckForPlayerInteraction()
    {
        if (playerController != null) return; // 已经有玩家在控制
        
        // 查找附近的玩家
        FirstPersonController nearbyPlayer = FindFirstObjectByType<FirstPersonController>();
        if (nearbyPlayer != null)
        {
            float distance = Vector3.Distance(transform.position, nearbyPlayer.transform.position);
            if (distance <= interactionRange)
            {
                // 显示交互提示（临时调试）
                if (Time.frameCount % 60 == 0) // 每秒显示一次
                {
                    Debug.Log($"无人机交互可用 - 距离: {distance:F1}m，按F键驾驶");
                }
                
                if (UnityEngine.InputSystem.Keyboard.current.fKey.wasPressedThisFrame)
                {
                    Debug.Log("检测到F键按下，开始控制无人机");
                    StartControlling(nearbyPlayer);
                }
            }
        }
    }
    
    void StartControlling(FirstPersonController player)
    {
        playerController = player;
        playerTransform = player.transform;
        
        // 获取玩家摄像机
        playerCamera = playerController.GetComponentInChildren<Camera>();
        if (playerCamera != null)
        {
            // 保存摄像机原始状态
            originalCameraParent = playerCamera.transform.parent;
            originalCameraPosition = playerCamera.transform.localPosition;
            originalCameraRotation = playerCamera.transform.localRotation;
            
            // 设置第三人称摄像机
            SetupThirdPersonCamera();
        }
        
        // 禁用玩家控制器并隐藏玩家
        playerController.enabled = false;
        playerTransform.gameObject.SetActive(false);
        
        // 启用无人机物理并设置稳定参数
        if (droneRigidbody != null)
        {
            droneRigidbody.isKinematic = false;
            droneRigidbody.mass = 2f; // 轻质量
            droneRigidbody.linearDamping = 3f; // 高阻力，便于控制
            droneRigidbody.angularDamping = 5f;
            droneRigidbody.useGravity = false; // 无人机不受重力影响
            
            
        }
        
        isBeingControlled = true;
        
    }
    
    void StopControlling()
    {
        if (playerController == null) return;
        
        // 恢复摄像机到第一人称
        if (playerCamera != null)
        {
            RestoreFirstPersonCamera();
        }
        
        // 重新显示玩家并移到无人机下方
        playerTransform.gameObject.SetActive(true);
        
        // 从无人机位置向下寻找地面
        Vector3 exitPosition = transform.position;
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 100f, groundLayers))
        {
            exitPosition = hit.point + Vector3.up * 0.1f; // 地面上方10cm
        }
        else
        {
            // 如果没有找到地面，在无人机下方2米处
            exitPosition = transform.position + Vector3.down * 2f;
        }
        
        // 重新启用玩家控制器
        playerController.enabled = true;
        
        // 使用CharacterController的Warp方法进行瞬移
        CharacterController charController = playerController.GetComponent<CharacterController>();
        if (charController != null)
        {
            charController.enabled = false; // 临时禁用
            playerTransform.position = exitPosition;
            charController.enabled = true; // 重新启用
            
        }
        else
        {
            playerTransform.position = exitPosition;
            
        }
        
        // 设置玩家朝向无人机的方向（向上看无人机）
        Vector3 lookDirection = (transform.position - exitPosition).normalized;
        // 对于无人机，我们希望玩家能看到无人机，所以保持一定的向上角度
        
        if (lookDirection != Vector3.zero)
        {
            // 计算水平朝向（面向无人机的投影）
            Vector3 horizontalDirection = new Vector3(lookDirection.x, 0, lookDirection.z).normalized;
            if (horizontalDirection != Vector3.zero)
            {
                playerTransform.rotation = Quaternion.LookRotation(horizontalDirection);
                
            }
            
            // 设置摄像机向上看无人机
            if (playerCamera != null)
            {
                // 计算向上看的角度
                float lookUpAngle = Mathf.Atan2(lookDirection.y, new Vector3(lookDirection.x, 0, lookDirection.z).magnitude) * Mathf.Rad2Deg;
                lookUpAngle = Mathf.Clamp(lookUpAngle, 0f, 60f); // 限制最大仰角60度
                
                // 重置FirstPersonController的内部旋转变量
                var fpController = playerController.GetComponent<FirstPersonController>();
                if (fpController != null)
                {
                    var xRotationField = typeof(FirstPersonController).GetField("xRotation", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (xRotationField != null)
                    {
                        xRotationField.SetValue(fpController, -lookUpAngle); // 负值表示向上看
                        
                    }
                }
                
                // 设置摄像机的本地旋转
                playerCamera.transform.localRotation = Quaternion.Euler(-lookUpAngle, 0, 0);
            }
        }
        
        
        
        // 设置无人机为悬浮模式
        if (droneRigidbody != null)
        {
            droneRigidbody.useGravity = true; // 恢复重力
            droneRigidbody.linearVelocity = Vector3.zero;
            droneRigidbody.angularVelocity = Vector3.zero;
        }
        
        isBeingControlled = false;
        playerController = null;
        playerTransform = null;
        playerCamera = null;
        
        
    }
    
    void HandleFlying()
    {
        if (playerController == null) return;
        
        // 检查退出控制
        if (UnityEngine.InputSystem.Keyboard.current.fKey.wasPressedThisFrame)
        {
            StopControlling();
            return;
        }
        
        // 获取输入
        Vector3 moveInput = Vector3.zero;
        
        // 水平移动只有在空中时才允许
        if (!isOnGround)
        {
            if (UnityEngine.InputSystem.Keyboard.current.wKey.isPressed) moveInput.z = 1; // 前进
            if (UnityEngine.InputSystem.Keyboard.current.sKey.isPressed) moveInput.z = -1; // 后退
            if (UnityEngine.InputSystem.Keyboard.current.aKey.isPressed) moveInput.x = -1; // 左转
            if (UnityEngine.InputSystem.Keyboard.current.dKey.isPressed) moveInput.x = 1; // 右转
        }
        else
        {
            // 在地面时，如果尝试使用WASD，显示提示
            if (UnityEngine.InputSystem.Keyboard.current.wKey.isPressed || 
                UnityEngine.InputSystem.Keyboard.current.sKey.isPressed ||
                UnityEngine.InputSystem.Keyboard.current.aKey.isPressed ||
                UnityEngine.InputSystem.Keyboard.current.dKey.isPressed)
            {
                // 限制提示频率，避免每帧都输出
                if (Time.frameCount % 60 == 0) // 每秒显示一次
                {
                    
                }
            }
        }
        
        // 垂直移动始终允许
        if (UnityEngine.InputSystem.Keyboard.current.jKey.isPressed) moveInput.y = 1; // 上升
        if (UnityEngine.InputSystem.Keyboard.current.kKey.isPressed) moveInput.y = -1; // 下降
        
        // 无人机移动
        if (droneRigidbody != null)
        {
            // 前进/后退和左右移动
            Vector3 horizontalMove = (transform.forward * moveInput.z + transform.right * moveInput.x) * flySpeed;
            
            // 垂直移动
            Vector3 verticalMove = Vector3.up * moveInput.y * verticalSpeed;
            
            // 组合移动
            Vector3 totalMove = horizontalMove + verticalMove;
            droneRigidbody.linearVelocity = totalMove;
            
            // 转向（基于左右输入）
            float turn = moveInput.x * turnSpeed * Time.deltaTime;
            transform.Rotate(0, turn, 0);
        }
    }
    
    void SetupThirdPersonCamera()
    {
        if (playerCamera == null) return;
        
        // 计算摄像机的世界位置（无人机后方）
        Vector3 cameraWorldPos = transform.position + 
                                transform.up * cameraHeight +
                                transform.forward * (-cameraDistance);
        
        // 直接设置摄像机的世界位置
        playerCamera.transform.SetParent(null);
        playerCamera.transform.position = cameraWorldPos;
        
        // 让摄像机看向无人机
        Vector3 lookTarget = transform.position;
        playerCamera.transform.LookAt(lookTarget);
        
        
    }
    
    void UpdateCameraFollow()
    {
        if (playerCamera == null || !isBeingControlled) return;
        
        // 计算摄像机应该在的位置
        Vector3 cameraWorldPos = transform.position + 
                                transform.up * cameraHeight +
                                transform.forward * (-cameraDistance);
        
        // 更新摄像机位置
        playerCamera.transform.position = cameraWorldPos;
        
        // 让摄像机看向无人机
        Vector3 lookTarget = transform.position;
        playerCamera.transform.LookAt(lookTarget);
    }
    
    void RestoreFirstPersonCamera()
    {
        if (playerCamera == null) return;
        
        // 恢复摄像机到原始父对象和位置
        playerCamera.transform.SetParent(originalCameraParent);
        playerCamera.transform.localPosition = originalCameraPosition;
        playerCamera.transform.localRotation = originalCameraRotation;
        
        
    }
    
    void CheckGroundStatus()
    {
        // 从无人机底部向下发射射线检测地面
        Vector3 rayStart = transform.position;
        bool wasOnGround = isOnGround;
        
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayers))
        {
            isOnGround = true;
            
            // 如果刚刚着陆，显示提示
            if (!wasOnGround && isBeingControlled)
            {
                
            }
        }
        else
        {
            isOnGround = false;
            
            // 如果刚刚起飞，显示提示
            if (wasOnGround && isBeingControlled)
            {
                
            }
        }
        
        // 绘制调试射线
        Debug.DrawRay(rayStart, Vector3.down * groundCheckDistance, isOnGround ? Color.red : Color.green);
    }
}
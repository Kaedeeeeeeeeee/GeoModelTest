using UnityEngine;

public class DrillCarController : MonoBehaviour
{
    [Header("Drill Car Behavior")]
    public bool isActive = true;
    public float drillAnimationSpeed = 2f;
    public GameObject drillPart;
    
    [Header("Driving Settings")]
    public float interactionRange = 3f;
    public float driveSpeed = 5f;
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
            rb.mass = 1000f;
            rb.centerOfMass = new Vector3(0, -0.5f, 0);
            rb.linearDamping = 5f;  // 增加线性阻力，减少滑动
            rb.angularDamping = 15f; // 增加角度阻力，减少摇摆
            rb.isKinematic = true; // 初始为运动学模式
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
        
        // 摄像机位置将在运行时计算
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
                if (UnityEngine.InputSystem.Keyboard.current.fKey.wasPressedThisFrame)
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
        
        // 保存玩家原始摄像机设置
        if (playerCamera != null)
        {
            originalCameraPosition = playerCamera.transform.position;
            originalCameraRotation = playerCamera.transform.rotation;
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
            // 确保约束设置正确（防止翻车）
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
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
        if (UnityEngine.InputSystem.Keyboard.current.fKey.wasPressedThisFrame)
        {
            StopDriving();
            return;
        }
        
        // 获取输入
        Vector2 moveInput = Vector2.zero;
        if (UnityEngine.InputSystem.Keyboard.current.wKey.isPressed) moveInput.y = 1;
        if (UnityEngine.InputSystem.Keyboard.current.sKey.isPressed) moveInput.y = -1;
        if (UnityEngine.InputSystem.Keyboard.current.aKey.isPressed) moveInput.x = -1;
        if (UnityEngine.InputSystem.Keyboard.current.dKey.isPressed) moveInput.x = 1;
        
        // 应用移动和旋转
        if (rb != null)
        {
            // 前进/后退
            Vector3 moveDirection = transform.forward * moveInput.y * driveSpeed;
            rb.AddForce(moveDirection, ForceMode.Acceleration);
            
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
        
        // 恢复摄像机
        if (playerCamera != null)
        {
            playerCamera.transform.SetParent(null);
            playerCamera.transform.position = originalCameraPosition;
            playerCamera.transform.rotation = originalCameraRotation;
        }
        
        // 显示玩家
        if (playerController != null)
        {
            // 将玩家移到车旁
            Vector3 exitPosition = transform.position + transform.right * 2f;
            playerController.transform.position = exitPosition;
            playerController.gameObject.SetActive(true);
            playerController = null;
        }
        
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
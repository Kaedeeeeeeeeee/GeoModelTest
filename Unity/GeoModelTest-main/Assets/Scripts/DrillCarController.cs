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
    public float cameraDistance = 3f; // 摄像机距离车辆的距离
    public float cameraHeight = 1.5f; // 摄像机高度
    
    [Header("Vehicle Drilling System")]
    public BoringTool boringTool; // 钻探工具引用
    public float drillingDistance = 1f; // 车头前方钻探距离
    public LayerMask drillingLayers = 1; // 可钻探的地层
    public float drillingCooldown = 3f; // 钻探冷却时间
    public ParticleSystem vehicleDrillingEffect; // 车载钻探粒子效果
    
    private float lastDrillingTime = 0f; // 上次钻探时间
    
    private Vector3 originalPosition;
    private bool isDrilling = false;
    private float drillTimer = 0f;
    private AudioSource audioSource;
    
    // 驾驶相关变量
    private bool isBeingDriven = false;
    private FirstPersonController playerController;
    private Transform playerTransform;
    private Vector3 playerOriginalPosition;
    private Rigidbody carRigidbody;
    
    // 摄像机相关变量
    private Camera playerCamera;
    private Transform originalCameraParent;
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    
    void Start()
    {
        originalPosition = transform.position;
        audioSource = GetComponent<AudioSource>();
        carRigidbody = GetComponent<Rigidbody>();
        
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        if (drillPart == null)
        {
            Transform drillTransform = transform.Find("Drill");
            if (drillTransform != null)
            {
                drillPart = drillTransform.gameObject;
            }
        }
        
        // 如果没有设置座位位置，创建一个默认位置
        if (playerSeatPosition == null)
        {
            GameObject seat = new GameObject("PlayerSeat");
            seat.transform.SetParent(transform);
            seat.transform.localPosition = new Vector3(0, 1f, 0); // 车辆上方1米
            playerSeatPosition = seat.transform;
        }
        
        // 如果没有设置摄像机位置，创建一个默认位置（车后方）
        if (cameraPosition == null)
        {
            CreateCameraPosition();
        }
        
        Debug.Log($"钻探车控制器已启动: {gameObject.name}");
    }
    
    void Update()
    {
        if (isBeingDriven)
        {
            HandleDriving();
            UpdateCameraFollow(); // 每帧更新摄像机跟随
        }
        else
        {
            CheckForPlayerInteraction();
        }
        
        if (isDrilling)
        {
            PerformDrillAnimation();
        }
    }
    
    void PerformDrillAnimation()
    {
        drillTimer += Time.deltaTime * drillAnimationSpeed;
        
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
        
        Debug.Log($"钻探车开始工作: {gameObject.name}");
    }
    
    public void StopDrilling()
    {
        isDrilling = false;
        isActive = false;
        
        if (audioSource != null)
        {
            audioSource.Stop();
        }
        
        Debug.Log($"钻探车停止工作: {gameObject.name}");
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
        playerController = player;
        playerTransform = player.transform;
        playerOriginalPosition = playerTransform.position;
        
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
        
        // 禁用玩家控制器
        playerController.enabled = false;
        
        // 隐藏玩家（不移动到座位，直接隐藏）
        playerTransform.gameObject.SetActive(false);
        
        isBeingDriven = true;
        
        // 启用车辆物理并设置稳定参数
        if (carRigidbody != null)
        {
            carRigidbody.isKinematic = false;
            
            // 设置更稳定的物理参数
            carRigidbody.mass = 1000f; // 较重的质量
            carRigidbody.centerOfMass = new Vector3(0, -1f, 0); // 很低的重心
            carRigidbody.linearDamping = 3f; // 增加阻力
            carRigidbody.angularDamping = 10f; // 高角阻力，防止翻滚
            
            // 冻结X和Z轴旋转，防止翻倒
            carRigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            
            Debug.Log("车辆物理已启用，重心已降低");
        }
        
        // 如果车辆已经倒了，先扶正它
        CorrectVehicleOrientation();
        
        Debug.Log($"玩家开始驾驶钻探车: {gameObject.name}");
    }
    
    void CorrectVehicleOrientation()
    {
        // 检查车辆是否倒下（Y轴朝上向量与世界Y轴的角度）
        float angle = Vector3.Angle(transform.up, Vector3.up);
        if (angle > 30f) // 如果倾斜超过30度
        {
            Debug.Log($"车辆倾斜{angle:F1}度，正在扶正...");
            
            // 扶正车辆
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.identity, 1f);
            
            // 稍微抬高一点，避免卡在地里
            transform.position += Vector3.up * 0.5f;
            
            // 清除所有速度
            if (carRigidbody != null)
            {
                carRigidbody.linearVelocity = Vector3.zero;
                carRigidbody.angularVelocity = Vector3.zero;
            }
        }
    }
    
    void StopDriving()
    {
        if (playerController == null) return;
        
        // 恢复摄像机到第一人称
        if (playerCamera != null)
        {
            RestoreFirstPersonCamera();
        }
        
        // 重新显示玩家并移到车辆后方
        // Debug.Log($"下车前 - 车辆位置: {transform.position}, 玩家原始位置: {playerOriginalPosition}");
        
        playerTransform.gameObject.SetActive(true);
        
        // 计算车后方位置（车尾后2米）
        Vector3 exitPosition = transform.position + (-transform.forward) * 2f;
        
        // 确保玩家落在地面上，而不是悬空
        if (Physics.Raycast(exitPosition + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 10f))
        {
            exitPosition = hit.point + Vector3.up * 0.1f; // 地面上方10cm
            // Debug.Log($"地面检测成功，调整到地面: {hit.point}");
        }
        else
        {
            // 如果没有检测到地面，使用车辆的Y坐标
            exitPosition.y = transform.position.y;
            // Debug.Log("地面检测失败，使用车辆Y坐标");
        }
        
        // Debug.Log($"计算的下车位置: {exitPosition}");
        
        // 重新启用玩家控制器
        playerController.enabled = true;
        
        // 使用CharacterController的Warp方法进行瞬移（推荐方式）
        CharacterController charController = playerController.GetComponent<CharacterController>();
        if (charController != null)
        {
            charController.enabled = false; // 临时禁用
            playerTransform.position = exitPosition;
            charController.enabled = true; // 重新启用
            Debug.Log($"使用CharacterController瞬移到位置: {exitPosition}");
        }
        else
        {
            // 如果没有CharacterController，直接设置位置
            playerTransform.position = exitPosition;
            Debug.Log($"直接设置玩家位置: {exitPosition}");
        }
        
        // 设置玩家朝向车辆的方向（面向车屁股）
        Vector3 lookDirection = (transform.position - exitPosition).normalized;
        lookDirection.y = 0; // 保持水平朝向，不要上下倾斜
        
        if (lookDirection != Vector3.zero)
        {
            playerTransform.rotation = Quaternion.LookRotation(lookDirection);
            Debug.Log($"设置玩家朝向: {lookDirection}");
            
            // 同时重置摄像机的垂直旋转
            if (playerCamera != null)
            {
                // 重置FirstPersonController的内部旋转变量
                var fpController = playerController.GetComponent<FirstPersonController>();
                if (fpController != null)
                {
                    // 通过反射访问私有字段xRotation并重置
                    var xRotationField = typeof(FirstPersonController).GetField("xRotation", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (xRotationField != null)
                    {
                        xRotationField.SetValue(fpController, 0f);
                        Debug.Log("重置摄像机垂直旋转");
                    }
                }
                
                // 重置摄像机的本地旋转
                playerCamera.transform.localRotation = Quaternion.identity;
            }
        }
        
        Debug.Log("玩家停止驾驶钻探车");
        
        // 最后清理引用
        isBeingDriven = false;
        playerController = null;
        playerTransform = null;
        playerCamera = null;
    }
    
    void HandleDriving()
    {
        if (playerController == null) return;
        
        // 检查退出驾驶
        if (UnityEngine.InputSystem.Keyboard.current.fKey.wasPressedThisFrame)
        {
            StopDriving();
            return;
        }
        
        // 检查车载钻探功能（J键）
        if (UnityEngine.InputSystem.Keyboard.current.jKey.wasPressedThisFrame)
        {
            PerformVehicleDrilling();
        }
        
        // 获取输入
        Vector2 moveInput = Vector2.zero;
        if (UnityEngine.InputSystem.Keyboard.current.wKey.isPressed) moveInput.y = 1;
        if (UnityEngine.InputSystem.Keyboard.current.sKey.isPressed) moveInput.y = -1;
        if (UnityEngine.InputSystem.Keyboard.current.aKey.isPressed) moveInput.x = -1;
        if (UnityEngine.InputSystem.Keyboard.current.dKey.isPressed) moveInput.x = 1;
        
        // 调试信息（注释掉以减少日志输出）
        // if (moveInput != Vector2.zero)
        // {
        //     Debug.Log($"驾驶输入: {moveInput}, Rigidbody状态: isKinematic={carRigidbody?.isKinematic}, 速度={carRigidbody?.linearVelocity}");
        // }
        
        // 车辆移动
        if (carRigidbody != null)
        {
            // 检查Rigidbody状态
            if (carRigidbody.isKinematic)
            {
                Debug.LogWarning("Rigidbody是运动学模式，无法通过物理力移动！");
                carRigidbody.isKinematic = false; // 强制设置为非运动学
            }
            
            // 前进/后退
            Vector3 moveDirection = transform.forward * moveInput.y * driveSpeed;
            carRigidbody.linearVelocity = new Vector3(moveDirection.x, carRigidbody.linearVelocity.y, moveDirection.z);
            
            // 转向（只有在移动时才转向）
            if (Mathf.Abs(moveInput.y) > 0.1f)
            {
                float turn = moveInput.x * turnSpeed * Time.deltaTime;
                transform.Rotate(0, turn, 0);
            }
        }
        else
        {
            Debug.LogError("carRigidbody为空！");
        }
    }
    
    void PerformVehicleDrilling()
    {
        // 检查冷却时间
        if (Time.time - lastDrillingTime < drillingCooldown)
        {
            float remainingCooldown = drillingCooldown - (Time.time - lastDrillingTime);
            Debug.Log($"车载钻探冷却中，剩余时间: {remainingCooldown:F1}秒");
            return;
        }
        
        // 计算车头前方钻探位置
        Vector3 drillingPosition = transform.position + transform.forward * drillingDistance;
        Vector3 rayStart = drillingPosition + Vector3.up * 2f; // 从车头前方上方开始射线
        
        // 详细调试信息（可在需要时启用）
        // Debug.Log($"车载钻探检测 - 车辆位置: {transform.position}");
        // Debug.Log($"车载钻探检测 - 车辆朝向: {transform.forward}");
        // Debug.Log($"车载钻探检测 - 钻探位置: {drillingPosition}");
        // Debug.Log($"车载钻探检测 - 射线起点: {rayStart}");
        // Debug.Log($"车载钻探检测 - 钻探层级: {drillingLayers.value} (二进制: {System.Convert.ToString(drillingLayers.value, 2)})");
        
        // 先尝试不限制LayerMask的射线检测
        bool hasAnyHit = Physics.Raycast(rayStart, Vector3.down, out RaycastHit anyHit, 10f);
        if (hasAnyHit)
        {
            int hitLayer = anyHit.collider.gameObject.layer;
            bool layerIncluded = (drillingLayers.value & (1 << hitLayer)) != 0;
            // Debug.Log($"检测到任意物体 - 名称: {anyHit.collider.name}, 层级: {hitLayer}, 位置: {anyHit.point}");
            // Debug.Log($"Layer {hitLayer} 是否在drillingLayers中: {layerIncluded}");
            
            // 如果检测到的物体不在指定层级中，自动添加该层级
            if (!layerIncluded)
            {
                // Debug.Log($"自动将Layer {hitLayer}添加到可钻探层级中");
                // 临时扩展钻探层级包含检测到的层级
                LayerMask expandedLayers = drillingLayers | (1 << hitLayer);
                // Debug.Log($"扩展后的钻探层级: {expandedLayers.value} (二进制: {System.Convert.ToString(expandedLayers.value, 2)})");
            }
        }
        else
        {
            Debug.Log("没有检测到任何物体，可能射线距离不够或位置太高");
        }
        
        // 智能层级检测：如果原始LayerMask失败但检测到其他物体，尝试扩展LayerMask
        LayerMask smartDrillingLayers = drillingLayers;
        if (hasAnyHit)
        {
            int hitLayer = anyHit.collider.gameObject.layer;
            if ((drillingLayers.value & (1 << hitLayer)) == 0)
            {
                // 临时扩展钻探层级
                smartDrillingLayers = drillingLayers | (1 << hitLayer);
                // Debug.Log($"智能扩展钻探层级，包含Layer {hitLayer}");
            }
        }
        
        // 向下射线检测地面（使用智能LayerMask）
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 10f, smartDrillingLayers))
        {
            Debug.Log($"车载钻探开始 - 位置: {hit.point}");
            
            // 如果有BoringTool引用，使用它的钻探功能
            if (boringTool != null)
            {
                // 直接调用钻探工具的钻探方法
                PerformBoringToolDrilling(hit);
            }
            else
            {
                // 如果没有BoringTool引用，执行简单的钻探效果
                PerformSimpleVehicleDrilling(hit);
            }
            
            // 设置冷却时间
            lastDrillingTime = Time.time;
            
            // 播放钻探动画和音效
            if (drillPart != null)
            {
                StartCoroutine(PlayDrillingAnimation());
            }
        }
        else
        {
            Debug.Log("车载钻探失败 - 智能LayerMask检测也未成功");
            
            // 如果智能LayerMask也失败了，但确实检测到物体，进行最后的降级处理
            if (hasAnyHit)
            {
                Debug.Log($"降级处理：直接在检测到的物体上进行钻探: {anyHit.collider.name}");
                
                // 使用检测到的任意物体进行钻探
                if (boringTool != null)
                {
                    PerformBoringToolDrilling(anyHit);
                }
                else
                {
                    PerformSimpleVehicleDrilling(anyHit);
                }
                
                // 设置冷却时间
                lastDrillingTime = Time.time;
                
                // 播放钻探动画
                if (drillPart != null)
                {
                    StartCoroutine(PlayDrillingAnimation());
                }
            }
            else
            {
                Debug.Log("完全失败：1) 车头前方可能是悬崖或水面；2) 射线检测距离不够；3) 车辆位置太高");
            }
        }
    }
    
    void PerformBoringToolDrilling(RaycastHit hit)
    {
        try
        {
            // 通过反射调用BoringTool的私有方法
            var boringToolType = boringTool.GetType();
            
            // 先尝试调用UseTool方法
            var useTool = boringToolType.GetMethod("UseTool", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (useTool != null)
            {
                useTool.Invoke(boringTool, new object[] { hit });
                Debug.Log("成功通过反射调用BoringTool的UseTool方法");
                return;
            }
            
            // 如果UseTool方法不存在，尝试调用其他钻探相关方法
            var performDrilling = boringToolType.GetMethod("PerformDrilling", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (performDrilling != null)
            {
                // 这可能需要协程，所以我们启动它
                var drillCoroutine = performDrilling.Invoke(boringTool, new object[] { hit });
                if (drillCoroutine != null)
                {
                    StartCoroutine((System.Collections.IEnumerator)drillCoroutine);
                    Debug.Log("成功通过反射调用BoringTool的PerformDrilling方法");
                    return;
                }
            }
            
            // 如果所有反射方法都失败，使用简单钻探
            Debug.LogWarning("无法通过反射调用BoringTool方法，使用简单钻探");
            PerformSimpleVehicleDrilling(hit);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"调用BoringTool钻探功能时出错: {e.Message}");
            PerformSimpleVehicleDrilling(hit);
        }
    }
    
    void PerformSimpleVehicleDrilling(RaycastHit hit)
    {
        Debug.Log($"执行简单车载钻探 - 位置: {hit.point}");
        
        // 创建简单的钻探效果
        if (vehicleDrillingEffect != null)
        {
            vehicleDrillingEffect.transform.position = hit.point;
            vehicleDrillingEffect.Play();
        }
        
        // 这里可以添加其他简单的钻探效果，比如创建洞、粒子效果等
    }
    
    System.Collections.IEnumerator PlayDrillingAnimation()
    {
        isDrilling = true;
        float animationDuration = 2f; // 钻探动画持续时间
        float elapsed = 0f;
        
        while (elapsed < animationDuration)
        {
            if (drillPart != null)
            {
                drillPart.transform.Rotate(Vector3.up * 360f * Time.deltaTime * drillAnimationSpeed);
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        isDrilling = false;
        Debug.Log("车载钻探动画完成");
    }
    
    void SetupThirdPersonCamera()
    {
        if (playerCamera == null) return;
        
        // 考虑车辆的缩放，计算实际的世界位置
        Vector3 vehicleScale = transform.localScale;
        
        // 计算摄像机的世界位置（不受车辆缩放影响）
        Vector3 cameraWorldPos = transform.position + 
                                transform.up * cameraHeight +
                                transform.forward * (-cameraDistance);
        
        // 直接设置摄像机的世界位置，不设置为子对象（避免缩放影响）
        playerCamera.transform.SetParent(null);
        playerCamera.transform.position = cameraWorldPos;
        
        // 让摄像机看向车辆
        Vector3 lookTarget = transform.position + Vector3.up * (cameraHeight * 0.3f);
        playerCamera.transform.LookAt(lookTarget);
        
        Debug.Log($"摄像机设置到世界位置: {cameraWorldPos}，车辆缩放: {vehicleScale}");
        
        // 摄像机跟随将在Update中处理
    }
    
    void UpdateThirdPersonCameraLook()
    {
        // 不需要每帧更新，使用固定的本地旋转即可
        // 摄像机会自动跟随车辆移动和转向
    }
    
    void RestoreFirstPersonCamera()
    {
        if (playerCamera == null) return;
        
        // 恢复摄像机到原始父对象和位置
        playerCamera.transform.SetParent(originalCameraParent);
        playerCamera.transform.localPosition = originalCameraPosition;
        playerCamera.transform.localRotation = originalCameraRotation;
        
        Debug.Log("恢复到第一人称视角");
    }
    
    System.Collections.IEnumerator FollowVehicle()
    {
        Debug.Log("开始摄像机跟随协程");
        
        while (isBeingDriven && playerCamera != null)
        {
            // 每帧更新摄像机位置，跟随车辆
            Vector3 cameraWorldPos = transform.position + 
                                    transform.up * cameraHeight +
                                    transform.forward * (-cameraDistance);
            
            playerCamera.transform.position = cameraWorldPos;
            
            // 让摄像机始终看向车辆
            Vector3 lookTarget = transform.position + Vector3.up * (cameraHeight * 0.3f);
            playerCamera.transform.LookAt(lookTarget);
            
            yield return null; // 等待下一帧
        }
        
        Debug.Log("摄像机跟随协程结束");
    }
    
    void UpdateCameraFollow()
    {
        if (playerCamera == null || !isBeingDriven) return;
        
        // 计算摄像机应该在的位置
        Vector3 cameraWorldPos = transform.position + 
                                transform.up * cameraHeight +
                                transform.forward * (-cameraDistance);
        
        // 更新摄像机位置
        playerCamera.transform.position = cameraWorldPos;
        
        // 让摄像机看向车辆
        Vector3 lookTarget = transform.position + Vector3.up * (cameraHeight * 0.3f);
        playerCamera.transform.LookAt(lookTarget);
    }
    
    void CreateCameraPosition()
    {
        GameObject camPos = new GameObject("CameraPosition");
        camPos.transform.SetParent(transform);
        UpdateCameraPositionValues();
        cameraPosition = camPos.transform;
        Debug.Log($"创建摄像机位置: distance={cameraDistance}, height={cameraHeight}");
    }
    
    void UpdateCameraPositionValues()
    {
        if (cameraPosition != null)
        {
            cameraPosition.localPosition = new Vector3(0, cameraHeight, -cameraDistance);
            Debug.Log($"更新摄像机位置到: {cameraPosition.localPosition}");
        }
    }
    
    // 在Inspector中修改值时调用此方法
    void OnValidate()
    {
        if (Application.isPlaying && cameraPosition != null)
        {
            UpdateCameraPositionValues();
        }
    }
    
    // 公共方法：手动更新摄像机位置（用于调试）
    [ContextMenu("更新摄像机位置")]
    public void RefreshCameraPosition()
    {
        UpdateCameraPositionValues();
        Debug.Log("手动更新摄像机位置完成");
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
    
    void OnDrawGizmosSelected()
    {
        // 绘制交互范围
        Gizmos.color = isBeingDriven ? Color.green : Color.blue;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
        
        // 绘制钻探范围
        if (isDrilling)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.down * 2f);
        }
        
        // 绘制车载钻探位置（车头前方）
        Gizmos.color = Color.red;
        Vector3 vehicleDrillingPos = transform.position + transform.forward * drillingDistance;
        Gizmos.DrawWireSphere(vehicleDrillingPos, 0.3f); // 钻探位置标记
        Gizmos.DrawLine(transform.position, vehicleDrillingPos); // 从车辆到钻探位置的连线
        
        // 绘制射线检测范围
        Gizmos.color = new Color(1f, 0.5f, 0f, 1f); // 橙色
        Vector3 rayStart = vehicleDrillingPos + Vector3.up * 2f;
        Gizmos.DrawLine(rayStart, vehicleDrillingPos + Vector3.down * 8f); // 射线检测范围
        
        // 绘制射线起点和终点标记
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(rayStart, 0.1f); // 射线起点
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(vehicleDrillingPos + Vector3.down * 8f, 0.1f); // 射线终点
        
        // 如果在运行时，尝试实际的射线检测并显示结果
        if (Application.isPlaying)
        {
            bool testHit = Physics.Raycast(rayStart, Vector3.down, out RaycastHit testHitInfo, 10f);
            if (testHit)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(testHitInfo.point, 0.2f); // 实际命中点
                Gizmos.DrawLine(rayStart, testHitInfo.point); // 实际射线路径
            }
            else
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(rayStart, rayStart + Vector3.down * 10f); // 未命中的射线
            }
        }
        
        // 绘制钻探冷却状态
        if (Application.isPlaying && Time.time - lastDrillingTime < drillingCooldown)
        {
            Gizmos.color = Color.gray;
            float cooldownProgress = (Time.time - lastDrillingTime) / drillingCooldown;
            Gizmos.DrawWireCube(vehicleDrillingPos + Vector3.up * 1f, Vector3.one * (1f - cooldownProgress));
        }
        
        // 绘制座位位置
        if (playerSeatPosition != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(playerSeatPosition.position, Vector3.one * 0.5f);
        }
        
        // 绘制摄像机位置
        if (cameraPosition != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(cameraPosition.position, Vector3.one * 0.3f);
            
            // 绘制摄像机朝向车辆的线
            Gizmos.color = Color.red;
            Vector3 lookTarget = transform.position + Vector3.up * 1f;
            Gizmos.DrawLine(cameraPosition.position, lookTarget);
        }
    }
}
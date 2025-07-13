using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 样本放置系统 - 管理从背包拿出样本到世界中的放置
/// </summary>
public class SamplePlacer : MonoBehaviour
{
    [Header("放置设置")]
    public LayerMask groundLayer = 1;
    public float maxPlaceDistance = 10f;
    public float minPlaceDistance = 1f;
    
    [Header("预览设置")]
    public Material previewMaterial;
    public Color validPlaceColor = Color.green;
    public Color invalidPlaceColor = Color.red;
    
    [Header("碰撞检测")]
    public float overlapCheckRadius = 0.5f;
    public LayerMask obstacleLayer = -1;
    
    // 私有成员
    private SampleItem itemToPlace;
    private GameObject previewObject;
    private bool isPlacingMode = false;
    private Camera playerCamera;
    private FirstPersonController fpController;
    private Renderer previewRenderer;
    private bool canPlaceAtCurrentPosition = false;
    
    // 单例模式
    public static SamplePlacer Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        InitializeComponents();
        SetupPreviewMaterial();
    }
    
    void Update()
    {
        if (isPlacingMode)
        {
            UpdatePreviewPosition();
            HandlePlacementInput();
        }
    }
    
    /// <summary>
    /// 初始化组件
    /// </summary>
    void InitializeComponents()
    {
        playerCamera = Camera.main;
        fpController = FindFirstObjectByType<FirstPersonController>();
        
        if (playerCamera == null)
        {
            Debug.LogError("未找到主摄像机！");
        }
    }
    
    /// <summary>
    /// 设置预览材质
    /// </summary>
    void SetupPreviewMaterial()
    {
        if (previewMaterial == null)
        {
            // 创建默认预览材质
            previewMaterial = new Material(Shader.Find("Standard"));
            previewMaterial.SetFloat("_Mode", 3); // Transparent mode
            previewMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            previewMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            previewMaterial.SetInt("_ZWrite", 0);
            previewMaterial.DisableKeyword("_ALPHATEST_ON");
            previewMaterial.EnableKeyword("_ALPHABLEND_ON");
            previewMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            previewMaterial.renderQueue = 3000;
        }
    }
    
    /// <summary>
    /// 开始放置模式
    /// </summary>
    public void StartPlacingMode(SampleItem sample)
    {
        if (sample == null)
        {
            Debug.LogWarning("样本数据为空，无法开始放置模式");
            return;
        }
        
        itemToPlace = sample;
        isPlacingMode = true;
        
        CreatePreviewObject();
        DisablePlayerControls();
        
        Debug.Log($"开始放置模式: {sample.displayName}");
    }
    
    /// <summary>
    /// 结束放置模式
    /// </summary>
    public void EndPlacingMode()
    {
        isPlacingMode = false;
        itemToPlace = null;
        
        DestroyPreviewObject();
        EnablePlayerControls();
        
        Debug.Log("结束放置模式");
    }
    
    /// <summary>
    /// 创建预览对象
    /// </summary>
    void CreatePreviewObject()
    {
        if (previewObject != null)
        {
            DestroyImmediate(previewObject);
        }
        
        // 创建基础预览对象
        previewObject = CreateSamplePreview();
        
        if (previewObject != null)
        {
            // 设置预览材质
            previewRenderer = previewObject.GetComponent<Renderer>();
            if (previewRenderer == null)
            {
                previewRenderer = previewObject.GetComponentInChildren<Renderer>();
            }
            
            if (previewRenderer != null && previewMaterial != null)
            {
                previewRenderer.material = previewMaterial;
            }
            
            // 移除碰撞器（预览对象不需要物理碰撞）
            Collider[] colliders = previewObject.GetComponentsInChildren<Collider>();
            foreach (var collider in colliders)
            {
                collider.enabled = false;
            }
            
            previewObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// 创建样本预览（简化版本）
    /// </summary>
    GameObject CreateSamplePreview()
    {
        // 创建简单的圆柱体预览
        GameObject preview = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        preview.name = $"SamplePreview_{itemToPlace.sampleID}";
        
        // 设置大小
        float radius = itemToPlace.sampleRadius * 2; // 直径
        float height = itemToPlace.totalDepth;
        preview.transform.localScale = new Vector3(radius, height / 2, radius);
        
        return preview;
    }
    
    /// <summary>
    /// 更新预览位置
    /// </summary>
    void UpdatePreviewPosition()
    {
        if (previewObject == null || playerCamera == null) return;
        
        // 从屏幕中心发射射线
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        
        if (Physics.Raycast(ray, out RaycastHit hit, maxPlaceDistance, groundLayer))
        {
            Vector3 placePosition = hit.point;
            
            // 检查距离限制
            float distanceToPlayer = Vector3.Distance(playerCamera.transform.position, placePosition);
            if (distanceToPlayer >= minPlaceDistance && distanceToPlayer <= maxPlaceDistance)
            {
                // 调整位置（预览高度比最终放置低60cm）
                placePosition.y += (itemToPlace.totalDepth * 1.1f) - 0.6f;
                
                previewObject.transform.position = placePosition;
                previewObject.transform.rotation = Quaternion.identity;
                
                // 检查是否可以放置
                canPlaceAtCurrentPosition = CanPlaceAtPosition(placePosition);
                UpdatePreviewColor();
                
                previewObject.SetActive(true);
            }
            else
            {
                previewObject.SetActive(false);
                canPlaceAtCurrentPosition = false;
            }
        }
        else
        {
            previewObject.SetActive(false);
            canPlaceAtCurrentPosition = false;
        }
    }
    
    /// <summary>
    /// 检查是否可以在指定位置放置
    /// </summary>
    bool CanPlaceAtPosition(Vector3 position)
    {
        // 检查是否与其他对象重叠
        Collider[] overlapping = Physics.OverlapSphere(position, overlapCheckRadius, obstacleLayer);
        
        foreach (var collider in overlapping)
        {
            // 排除地面和预览对象本身
            if (collider.gameObject != previewObject && 
                !IsGroundObject(collider.gameObject))
            {
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// 检查是否为地面对象
    /// </summary>
    bool IsGroundObject(GameObject obj)
    {
        // 检查是否在地面层
        return ((1 << obj.layer) & groundLayer) != 0;
    }
    
    /// <summary>
    /// 更新预览颜色
    /// </summary>
    void UpdatePreviewColor()
    {
        if (previewRenderer != null && previewMaterial != null)
        {
            Color targetColor = canPlaceAtCurrentPosition ? validPlaceColor : invalidPlaceColor;
            targetColor.a = 0.5f; // 保持半透明
            previewMaterial.color = targetColor;
        }
    }
    
    /// <summary>
    /// 处理放置输入
    /// </summary>
    void HandlePlacementInput()
    {
        // 左键确认放置
        if (Mouse.current.leftButton.wasPressedThisFrame && previewObject.activeInHierarchy)
        {
            if (canPlaceAtCurrentPosition)
            {
                ConfirmPlacement();
            }
            else
            {
                ShowMessage("无法在此位置放置样本");
            }
        }
        
        // 右键或ESC取消
        if (Mouse.current.rightButton.wasPressedThisFrame || 
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CancelPlacement();
        }
    }
    
    /// <summary>
    /// 确认放置
    /// </summary>
    void ConfirmPlacement()
    {
        if (itemToPlace == null || previewObject == null) return;
        
        // 获取预览位置，但将实际放置高度恢复到原始高度（加回60cm）
        Vector3 placePosition = previewObject.transform.position;
        placePosition.y += 0.6f; // 恢复预览时减去的60cm
        
        // 创建真实样本对象
        GameObject realSample = CreateRealSample(itemToPlace, placePosition);
        
        if (realSample != null)
        {
            // 注册到场景跟踪器
            PlacedSampleTracker.RegisterPlacedSample(realSample);
            
            // 更新样本状态
            itemToPlace.currentLocation = SampleLocation.InWorld;
            itemToPlace.currentWorldPosition = placePosition;
            itemToPlace.isPlayerPlaced = true;
            
            // 从背包UI中移除（但保留数据用于收回）
            if (SampleInventory.Instance != null)
            {
                SampleInventory.Instance.RemoveSampleFromUI(itemToPlace);
            }
            
            ShowMessage($"已放置样本: {itemToPlace.displayName}");
            Debug.Log($"样本已放置: {itemToPlace.displayName} 位置: {placePosition}");
        }
        
        EndPlacingMode();
    }
    
    /// <summary>
    /// 取消放置
    /// </summary>
    void CancelPlacement()
    {
        ShowMessage("取消放置样本");
        EndPlacingMode();
    }
    
    /// <summary>
    /// 创建真实样本对象
    /// </summary>
    GameObject CreateRealSample(SampleItem sampleData, Vector3 position)
    {
        // 创建基础样本对象
        GameObject sampleObj = CreateSampleGameObject(sampleData);
        
        if (sampleObj != null)
        {
            sampleObj.transform.position = position;
            sampleObj.transform.rotation = Quaternion.identity;
            
            // 添加收回组件
            PlacedSampleCollector collector = sampleObj.AddComponent<PlacedSampleCollector>();
            collector.Setup(sampleData);
            
            // 先添加基础物理组件（悬浮组件会自动配置）
            if (sampleObj.GetComponent<Rigidbody>() == null)
            {
                Rigidbody rb = sampleObj.AddComponent<Rigidbody>();
                rb.mass = 0.5f;
                rb.linearDamping = 5f;
                rb.angularDamping = 10f;
                // 悬浮组件会自动配置useGravity和constraints
            }
            
            // 添加悬浮效果组件（会自动配置物理属性）
            AddFloatingBehavior(sampleObj);
            
            return sampleObj;
        }
        
        return null;
    }
    
    /// <summary>
    /// 创建样本游戏对象
    /// </summary>
    GameObject CreateSampleGameObject(SampleItem sampleData)
    {
        // 使用保存的原始模型数据重建样本
        GameObject sampleObj = sampleData.RecreateOriginalModel(Vector3.zero);
        
        if (sampleObj == null)
        {
            // 如果重建失败，使用备用方案
            sampleObj = CreateFallbackSample(sampleData);
        }
        
        return sampleObj;
    }
    
    /// <summary>
    /// 创建备用样本（当模型重建失败时）
    /// </summary>
    GameObject CreateFallbackSample(SampleItem sampleData)
    {
        GameObject sampleObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        sampleObj.name = $"PlacedSample_{sampleData.sampleID}";
        
        // 设置大小
        float radius = sampleData.sampleRadius * 2;
        float height = sampleData.totalDepth;
        sampleObj.transform.localScale = new Vector3(radius, height / 2, radius);
        
        // 设置材质颜色
        SetSampleMaterial(sampleObj, sampleData);
        
        return sampleObj;
    }
    
    /// <summary>
    /// 添加悬浮行为
    /// </summary>
    void AddFloatingBehavior(GameObject sampleObj)
    {
        // 优先使用现有的GeometricSampleFloating组件
        try
        {
            GeometricSampleFloating floating = sampleObj.AddComponent<GeometricSampleFloating>();
            floating.floatingAmplitude = 0.2f;
            floating.floatingSpeed = 1f;
            floating.rotationSpeed = new Vector3(0, 20f, 0);
            floating.enableFloating = true;
            floating.enableRotation = true;
            floating.enableBobbing = true;
            Debug.Log("已添加GeometricSampleFloating悬浮效果");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"无法添加GeometricSampleFloating: {e.Message}，使用简单悬浮组件");
            // 如果GeometricSampleFloating不可用，使用简单的悬浮脚本
            SimpleSampleFloating simpleFloat = sampleObj.AddComponent<SimpleSampleFloating>();
            simpleFloat.floatHeight = 0.2f;
            simpleFloat.floatSpeed = 1f;
            simpleFloat.rotationSpeed = 20f;
        }
    }
    
    /// <summary>
    /// 设置样本材质
    /// </summary>
    void SetSampleMaterial(GameObject sampleObj, SampleItem sampleData)
    {
        Renderer renderer = sampleObj.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material sampleMaterial = new Material(Shader.Find("Standard"));
            
            // 基于地质层设置颜色
            if (sampleData.geologicalLayers != null && sampleData.geologicalLayers.Count > 0)
            {
                sampleMaterial.color = sampleData.geologicalLayers[0].layerColor;
            }
            else
            {
                // 基于工具ID设置默认颜色
                sampleMaterial.color = sampleData.sourceToolID switch
                {
                    "1000" => new Color(0.8f, 0.6f, 0.4f), // 简易钻探 - 棕色
                    "1001" => new Color(0.6f, 0.8f, 0.4f), // 钻塔 - 绿色
                    _ => Color.gray
                };
            }
            
            renderer.material = sampleMaterial;
        }
    }
    
    /// <summary>
    /// 销毁预览对象
    /// </summary>
    void DestroyPreviewObject()
    {
        if (previewObject != null)
        {
            DestroyImmediate(previewObject);
            previewObject = null;
            previewRenderer = null;
        }
    }
    
    /// <summary>
    /// 禁用玩家控制
    /// </summary>
    void DisablePlayerControls()
    {
        if (fpController != null)
        {
            fpController.enableMouseLook = false;
        }
        
        Cursor.lockState = CursorLockMode.None;
    }
    
    /// <summary>
    /// 启用玩家控制
    /// </summary>
    void EnablePlayerControls()
    {
        if (fpController != null)
        {
            fpController.enableMouseLook = true;
        }
        
        Cursor.lockState = CursorLockMode.Locked;
    }
    
    /// <summary>
    /// 显示消息
    /// </summary>
    void ShowMessage(string message)
    {
        Debug.Log($"[SamplePlacer] {message}");
        // TODO: 实现屏幕消息显示系统
    }
    
    /// <summary>
    /// 清理资源
    /// </summary>
    void OnDestroy()
    {
        DestroyPreviewObject();
    }
    
    /// <summary>
    /// 在Scene视图中绘制放置范围
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (playerCamera != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(playerCamera.transform.position, minPlaceDistance);
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(playerCamera.transform.position, maxPlaceDistance);
        }
        
        if (previewObject != null && isPlacingMode)
        {
            Gizmos.color = canPlaceAtCurrentPosition ? Color.green : Color.red;
            Gizmos.DrawWireSphere(previewObject.transform.position, overlapCheckRadius);
        }
    }
}
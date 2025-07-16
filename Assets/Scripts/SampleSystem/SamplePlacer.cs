using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 样本放置系统 - 管理从背包拿出样本到世界中的放置
/// </summary>
public class SamplePlacer : MonoBehaviour
{
    [Header("放置设置")]
    public LayerMask groundLayer = -1; // 包含所有层级，提高兼容性
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
            // 创建更可靠的半透明预览材质
            previewMaterial = new Material(Shader.Find("Legacy Shaders/Transparent/Diffuse"));
            if (previewMaterial.shader == null)
            {
                // 备用方案：使用Unlit着色器
                previewMaterial = new Material(Shader.Find("Unlit/Transparent"));
            }
            
            // 设置明显的颜色和透明度
            previewMaterial.color = new Color(0f, 1f, 0f, 0.5f); // 绿色半透明
            previewMaterial.renderQueue = 3000;
            
            Debug.Log($"创建预览材质: {previewMaterial.shader.name}");
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
        
        Debug.Log($"开始放置模式: {sample.displayName} (工具ID: {sample.sourceToolID})");
        
        CreatePreviewObject();
        
        if (previewObject != null)
        {
            Debug.Log($"预览对象已创建: {previewObject.name}, 初始活跃状态: {previewObject.activeInHierarchy}");
        }
        else
        {
            Debug.LogError("预览对象创建失败！");
        }
        
        DisablePlayerControls();
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
            Debug.Log($"预览对象创建: {previewObject.name}, 位置: {previewObject.transform.position}");
            
            // 设置预览材质
            previewRenderer = previewObject.GetComponent<Renderer>();
            if (previewRenderer == null)
            {
                previewRenderer = previewObject.GetComponentInChildren<Renderer>();
            }
            
            if (previewRenderer != null)
            {
                Debug.Log($"找到渲染器: {previewRenderer.gameObject.name}");
                
                if (previewMaterial != null)
                {
                    previewRenderer.material = previewMaterial;
                    Debug.Log($"应用预览材质: {previewMaterial.shader.name}, 颜色: {previewMaterial.color}");
                }
                else
                {
                    Debug.LogError("预览材质为空！");
                }
                
                // 确保渲染器启用
                previewRenderer.enabled = true;
                Debug.Log($"渲染器状态: enabled={previewRenderer.enabled}, bounds={previewRenderer.bounds}");
            }
            else
            {
                Debug.LogError("未找到预览对象的渲染器！");
            }
            
            // 移除碰撞器（预览对象不需要物理碰撞）
            Collider[] colliders = previewObject.GetComponentsInChildren<Collider>();
            foreach (var collider in colliders)
            {
                collider.enabled = false;
            }
            
            previewObject.SetActive(false);
            Debug.Log("预览对象初始设置为非活跃状态，等待位置更新");
        }
        else
        {
            Debug.LogError("预览对象创建失败！");
        }
    }
    
    /// <summary>
    /// 创建样本预览（简化版本）
    /// </summary>
    GameObject CreateSamplePreview()
    {
        GameObject preview;
        
        // 根据工具来源决定预览形状
        if (itemToPlace.sourceToolID == "1002") // 地质锤工具
        {
            // 创建薄片预览（扁平立方体）
            preview = GameObject.CreatePrimitive(PrimitiveType.Cube);
            preview.name = $"SlabPreview_{itemToPlace.sampleID}";
            
            // 薄片形状：宽度0.8m，厚度0.06m，长度0.6m
            float width = 0.8f;
            float thickness = 0.06f;
            float length = 0.6f;
            preview.transform.localScale = new Vector3(width, thickness, length);
        }
        else
        {
            // 创建圆柱体预览（钻探样本）
            preview = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            preview.name = $"SamplePreview_{itemToPlace.sampleID}";
            
            // 设置大小
            float radius = itemToPlace.sampleRadius * 2; // 直径
            float height = itemToPlace.totalDepth;
            preview.transform.localScale = new Vector3(radius, height / 2, radius);
        }
        
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
        
        // 先尝试使用指定的groundLayer进行射线检测
        bool hitGround = Physics.Raycast(ray, out RaycastHit hit, maxPlaceDistance, groundLayer);
        
        // 如果没有击中指定层，尝试击中任何碰撞器（用于调试）
        if (!hitGround)
        {
            hitGround = Physics.Raycast(ray, out hit, maxPlaceDistance);
            if (hitGround)
            {
                Debug.Log($"射线击中了 {hit.collider.gameObject.name} (层级: {hit.collider.gameObject.layer}), 但不在groundLayer ({groundLayer.value}) 中");
            }
            else
            {
                Debug.Log($"射线未击中任何对象 (最大距离: {maxPlaceDistance}m)");
            }
        }
        
        if (hitGround)
        {
            Vector3 placePosition = hit.point;
            Debug.Log($"样本预览: 击中 {hit.collider.gameObject.name} 在位置 {placePosition}");
            
            // 检查距离限制
            float distanceToPlayer = Vector3.Distance(playerCamera.transform.position, placePosition);
            if (distanceToPlayer >= minPlaceDistance && distanceToPlayer <= maxPlaceDistance)
            {
                // 调整位置计算：确保样本在地面上方
                if (itemToPlace.sourceToolID == "1002") // 地质锤薄片样本
                {
                    placePosition.y += 0.3f; // 薄片样本：在地面上方0.3米
                }
                else
                {
                    placePosition.y += 2.0f; // 钻探样本：在地面上方2.0米
                }
                
                previewObject.transform.position = placePosition;
                previewObject.transform.rotation = Quaternion.identity;
                
                // 检查是否可以放置
                canPlaceAtCurrentPosition = CanPlaceAtPosition(placePosition);
                UpdatePreviewColor();
                
                previewObject.SetActive(true);
                
                // 详细调试预览对象状态
                Debug.Log($"显示样本预览在位置: {placePosition}, 可放置: {canPlaceAtCurrentPosition}");
                Debug.Log($"预览对象状态: active={previewObject.activeInHierarchy}, 世界位置={previewObject.transform.position}, 渲染器enabled={previewRenderer?.enabled}");
                
                // 检查相机视锥体
                if (playerCamera != null)
                {
                    bool inFrustum = GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(playerCamera), previewRenderer.bounds);
                    Debug.Log($"预览对象在相机视锥体内: {inFrustum}, 到相机距离: {Vector3.Distance(playerCamera.transform.position, previewObject.transform.position)}");
                }
            }
            else
            {
                previewObject.SetActive(false);
                canPlaceAtCurrentPosition = false;
                Debug.Log($"距离超出范围: {distanceToPlayer}m (范围: {minPlaceDistance}-{maxPlaceDistance}m)");
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
        
        // 获取预览位置作为最终放置位置
        Vector3 placePosition = previewObject.transform.position;
        
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
        Debug.Log($"开始创建真实样本在位置: {position}");
        
        // 创建基础样本对象，直接传入正确位置
        GameObject sampleObj = CreateSampleGameObject(sampleData, position);
        
        if (sampleObj != null)
        {
            // 强制确保位置正确 - 这是关键修复
            sampleObj.transform.position = position;
            sampleObj.transform.rotation = Quaternion.identity;
            
            // 同时确保所有子对象的位置也正确
            Transform[] allChildren = sampleObj.GetComponentsInChildren<Transform>();
            foreach (Transform child in allChildren)
            {
                if (child != sampleObj.transform) // 不处理根对象自己
                {
                    // 重置子对象到相对于新父对象位置的正确位置
                    Vector3 originalLocalPos = child.localPosition;
                    child.localPosition = originalLocalPos;
                    Debug.Log($"子对象 {child.name} 本地位置重置为: {originalLocalPos}");
                }
            }
            
            Debug.Log($"样本对象最终位置: {sampleObj.transform.position}");
            
            // 添加收回组件
            PlacedSampleCollector collector = sampleObj.AddComponent<PlacedSampleCollector>();
            collector.Setup(sampleData);
            Debug.Log($"PlacedSampleCollector已添加到样本: {sampleData.displayName}");
            
            // 只为地质锤样本添加特殊的悬浮效果，钻探样本保持原有逻辑
            if (sampleData.sourceToolID == "1002") // 只处理地质锤样本
            {
                AddSampleFloatingEffect(sampleObj, sampleData);
            }
            else
            {
                // 钻探样本：添加悬浮效果（恢复原有行为）
                AddSampleFloatingEffect(sampleObj, sampleData);
                Debug.Log("钻探样本悬浮效果已恢复");
            }
            
            // 最终位置确认（延迟执行，确保所有组件都添加完成）
            StartCoroutine(FinalizePosition(sampleObj, position));
            
            return sampleObj;
        }
        
        return null;
    }
    
    /// <summary>
    /// 创建样本游戏对象
    /// </summary>
    GameObject CreateSampleGameObject(SampleItem sampleData, Vector3 position)
    {
        Debug.Log($"CreateSampleGameObject: 请求位置={position}, 工具ID={sampleData.sourceToolID}");
        
        // 使用保存的原始模型数据重建样本，直接传入正确位置
        GameObject sampleObj = sampleData.RecreateOriginalModel(position);
        
        if (sampleObj == null)
        {
            Debug.Log("RecreateOriginalModel失败，使用备用方案CreateFallbackSample");
            // 如果重建失败，使用备用方案
            sampleObj = CreateFallbackSample(sampleData);
            if (sampleObj != null)
            {
                sampleObj.transform.position = position;
                Debug.Log($"备用样本位置设置为: {position}");
            }
        }
        else
        {
            Debug.Log($"RecreateOriginalModel成功创建样本: {sampleObj.name}");
        }
        
        if (sampleObj != null)
        {
            Debug.Log($"最终样本对象: {sampleObj.name}, 世界位置: {sampleObj.transform.position}");
            
            // 检查是否有子对象，可能位置在子对象上
            Transform[] children = sampleObj.GetComponentsInChildren<Transform>();
            Debug.Log($"样本对象包含 {children.Length} 个Transform组件:");
            foreach (Transform child in children)
            {
                Debug.Log($"  - {child.name}: 位置={child.position}, 本地位置={child.localPosition}");
                
                // 只对地质锤样本进行修正
                if (sampleData.sourceToolID == "1002") // 只处理地质锤样本
                {
                    if (child.name.Contains("Slab") || child.name.Contains("Mesh"))
                    {
                        Vector3 originalLocalPos = child.localPosition;
                        Vector3 originalLocalScale = child.localScale;
                        
                        child.localPosition = Vector3.zero; // 强制设置为(0, 0, 0)
                        // 移除不必要的放大，保持原始大小
                        child.localScale = originalLocalScale; // 保持原始缩放
                        
                        Debug.Log($"地质锤子对象位置修正: {child.name}");
                        Debug.Log($"  - 原本地位置: {originalLocalPos}");
                        Debug.Log($"  - 新本地位置: {child.localPosition}");
                        Debug.Log($"  - 保持原始缩放: {child.localScale}");
                        Debug.Log($"  - 修正后世界位置: {child.position}");
                        
                        Renderer childRenderer = child.GetComponent<Renderer>();
                        if (childRenderer != null)
                        {
                            Debug.Log($"  - 渲染器启用: {childRenderer.enabled}");
                            if (childRenderer.material != null)
                            {
                                Debug.Log($"  - 材质: {childRenderer.material.name}, 颜色: {childRenderer.material.color}");
                            }
                        }
                    }
                }
            }
        }
        
        return sampleObj;
    }
    
    /// <summary>
    /// 创建备用样本（当模型重建失败时）
    /// </summary>
    GameObject CreateFallbackSample(SampleItem sampleData)
    {
        GameObject sampleObj;
        
        // 根据工具来源决定样本形状
        if (sampleData.sourceToolID == "1002") // 地质锤工具
        {
            // 创建薄片形状
            sampleObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sampleObj.name = $"PlacedSlabSample_{sampleData.sampleID}";
            
            // 薄片形状：宽度0.8m，厚度0.06m，长度0.6m
            float width = 0.8f;
            float thickness = 0.06f;
            float length = 0.6f;
            sampleObj.transform.localScale = new Vector3(width, thickness, length);
        }
        else
        {
            // 创建圆柱形钻探样本
            sampleObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            sampleObj.name = $"PlacedSample_{sampleData.sampleID}";
            
            // 设置大小
            float radius = sampleData.sampleRadius * 2;
            float height = sampleData.totalDepth;
            sampleObj.transform.localScale = new Vector3(radius, height / 2, radius);
        }
        
        // 设置材质颜色（确保双面渲染）
        SetSampleMaterial(sampleObj, sampleData);
        
        return sampleObj;
    }
    
    /// <summary>
    /// 添加样本悬浮效果（与圆柱体样本一致）
    /// </summary>
    void AddSampleFloatingEffect(GameObject sampleObj, SampleItem sampleData)
    {
        // 记录当前位置，防止悬浮组件改变位置
        Vector3 currentPosition = sampleObj.transform.position;
        Debug.Log($"添加悬浮效果前，样本位置: {currentPosition}");
        
        // 添加基础物理组件
        Rigidbody rb = sampleObj.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = sampleObj.AddComponent<Rigidbody>();
        }
        
        rb.mass = 0.5f;
        rb.linearDamping = 5f;
        rb.angularDamping = 10f;
        rb.useGravity = false; // 禁用重力，由悬浮组件控制
        rb.isKinematic = false; // 允许物理交互
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ; // 只允许Y轴旋转
        
        // 添加悬浮效果组件
        SimpleSampleFloating floating = sampleObj.AddComponent<SimpleSampleFloating>();
        
        // 根据样本类型设置不同的悬浮参数
        if (sampleData.sourceToolID == "1002") // 地质锤薄片样本
        {
            floating.floatHeight = 0.15f; // 悬浮高度
            floating.floatSpeed = 0.8f; // 较慢的悬浮速度
            floating.rotationSpeed = 10f; // 较慢的旋转速度
            floating.enableRotation = true;
            floating.enableFloating = true;
        }
        else // 钻探样本
        {
            floating.floatHeight = 0.2f;
            floating.floatSpeed = 1f;
            floating.rotationSpeed = 20f;
            floating.enableRotation = true;
            floating.enableFloating = true;
        }
        
        // 强制恢复位置（防止悬浮组件初始化时改变位置）
        sampleObj.transform.position = currentPosition;
        
        Debug.Log($"悬浮效果添加完成，最终位置: {sampleObj.transform.position} (工具ID: {sampleData.sourceToolID})");
    }
    
    /// <summary>
    /// 最终确认样本位置（延迟执行）
    /// </summary>
    System.Collections.IEnumerator FinalizePosition(GameObject sampleObj, Vector3 targetPosition)
    {
        // 等待几帧，确保所有组件都初始化完成
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        
        if (sampleObj != null)
        {
            Debug.Log($"最终位置确认: 目标={targetPosition}, 当前={sampleObj.transform.position}");
            sampleObj.transform.position = targetPosition;
            Debug.Log($"位置已强制设置为: {sampleObj.transform.position}");
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
            // 简化着色器选择，使用最兼容的Standard
            Shader shader = Shader.Find("Standard");
            if (shader == null)
            {
                shader = Shader.Find("Legacy Shaders/Diffuse");
                Debug.LogWarning("使用Legacy Diffuse着色器作为备用");
            }
            
            Material sampleMaterial = new Material(shader);
            Debug.Log($"SamplePlacer使用着色器: {shader.name}");
            
            // 基于地质层设置颜色
            if (sampleData.geologicalLayers != null && sampleData.geologicalLayers.Count > 0)
            {
                sampleMaterial.color = sampleData.geologicalLayers[0].layerColor;
                Debug.Log($"使用样本数据中的地质层颜色: {sampleData.geologicalLayers[0].layerColor}");
            }
            else
            {
                // 基于工具ID设置默认颜色
                sampleMaterial.color = sampleData.sourceToolID switch
                {
                    "1000" => new Color(0.8f, 0.6f, 0.4f), // 简易钻探 - 棕色
                    "1001" => new Color(0.6f, 0.8f, 0.4f), // 钻塔 - 绿色
                    "1002" => new Color(0.7f, 0.5f, 0.3f), // 地质锤 - 深棕色
                    _ => Color.gray
                };
                Debug.Log($"使用工具ID ({sampleData.sourceToolID}) 默认颜色: {sampleMaterial.color}");
            }
            
            // 对于薄片样本，启用简化的双面渲染
            if (sampleData.sourceToolID == "1002") // 地质锤薄片样本
            {
                try
                {
                    // 关键：关闭背面剔除
                    if (sampleMaterial.HasProperty("_Cull"))
                    {
                        sampleMaterial.SetInt("_Cull", 0); // 0 = Off（显示双面）
                        Debug.Log("薄片样本启用双面渲染 (_Cull = 0)");
                    }
                    else
                    {
                        Debug.LogWarning("材质不支持_Cull属性，无法设置双面渲染");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"设置双面渲染失败: {e.Message}");
                }
            }
            
            // 设置基本材质属性
            sampleMaterial.SetFloat("_Metallic", 0.1f);
            sampleMaterial.SetFloat("_Glossiness", 0.3f);
            
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
using UnityEngine;
using System.Collections;

public class BoringTool : CollectionTool
{
    [Header("Boring Tool Settings")]
    public float boringRadius = 0.5f;
    public float boringDepth = 2f;
    public LayerMask groundLayers = 1;
    public GameObject samplePrefab;
    
    [Header("Visual Effects")]
    public ParticleSystem drillingEffect;
    public GameObject drillingIndicator;
    
    [Header("Preview Settings")]
    public Material previewMaterial;
    public Color validPreviewColor = Color.green;
    public Color invalidPreviewColor = Color.red;
    
    // 预览相关变量
    private GameObject previewCylinder;
    private bool showPreview = false;
    
    [Header("Geology System")]
    public LayerDetectionSystem detectionSystem;
    public SampleReconstructionSystem reconstructionSystem;
    
    [Header("Geometric Cutting System")]
    public bool useGeometricCutting = true;
    public SimpleGeometricTool simpleGeometricTool;
    
    [Header("Sample Display Settings")]
    [Range(0.5f, 5.0f)]
    public float minSampleHeight = 1.5f;
    [Range(1.0f, 8.0f)]
    public float maxSampleHeight = 3.5f;
    [Range(0.0f, 1.0f)]
    public float playerDirectionOffset = 0.3f;
    
    protected override void Start()
    {
        base.Start();
        toolName = "钻探工具";
        
        if (drillingIndicator != null)
        {
            drillingIndicator.SetActive(false);
        }
        
        // 初始化地质系统
        InitializeGeologySystem();
        
        // 创建预览圆柱体
        CreatePreviewCylinder();
    }
    
    void InitializeGeologySystem()
    {
        // 优先使用几何切割系统
        if (useGeometricCutting)
        {
            if (simpleGeometricTool == null)
            {
                simpleGeometricTool = GetComponent<SimpleGeometricTool>();
                if (simpleGeometricTool == null)
                {
                    simpleGeometricTool = gameObject.AddComponent<SimpleGeometricTool>();
                }
            }
            
            // 同步钻探参数
            simpleGeometricTool.SetDrillingParameters(boringRadius, boringDepth);
            Debug.Log("简化几何切割系统初始化完成");
            return;
        }
        
        // 回退到传统系统
        if (detectionSystem == null)
        {
            detectionSystem = FindFirstObjectByType<LayerDetectionSystem>();
            if (detectionSystem == null)
            {
                GameObject detectionObj = new GameObject("LayerDetectionSystem");
                detectionSystem = detectionObj.AddComponent<LayerDetectionSystem>();
            }
        }
        
        if (reconstructionSystem == null)
        {
            reconstructionSystem = FindFirstObjectByType<SampleReconstructionSystem>();
            if (reconstructionSystem == null)
            {
                GameObject reconstructionObj = new GameObject("SampleReconstructionSystem");
                reconstructionSystem = reconstructionObj.AddComponent<SampleReconstructionSystem>();
            }
        }
        
        Debug.Log("传统地质系统初始化完成");
    }
    
    protected override void Update()
    {
        base.Update(); // 调用父类的Update方法
        
        if (isEquipped)
        {
            UpdatePreview();
        }
    }
    
    void CreatePreviewCylinder()
    {
        // 创建预览圆柱体
        previewCylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        previewCylinder.name = "BoringTool_Preview";
        
        // 设置圆柱体大小
        previewCylinder.transform.localScale = new Vector3(boringRadius * 2, boringDepth / 2, boringRadius * 2);
        
        // 移除碰撞器
        Collider col = previewCylinder.GetComponent<Collider>();
        if (col != null)
        {
            DestroyImmediate(col);
        }
        
        // 设置材质
        Renderer renderer = previewCylinder.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (previewMaterial != null)
            {
                renderer.material = previewMaterial;
            }
            else
            {
                // 创建默认半透明材质
                Material defaultMaterial = new Material(Shader.Find("Standard"));
                defaultMaterial.SetFloat("_Mode", 3); // Transparent mode
                defaultMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                defaultMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                defaultMaterial.SetInt("_ZWrite", 0);
                defaultMaterial.DisableKeyword("_ALPHATEST_ON");
                defaultMaterial.EnableKeyword("_ALPHABLEND_ON");
                defaultMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                defaultMaterial.renderQueue = 3000;
                defaultMaterial.color = new Color(validPreviewColor.r, validPreviewColor.g, validPreviewColor.b, 0.3f);
                renderer.material = defaultMaterial;
            }
        }
        
        // 初始隐藏
        previewCylinder.SetActive(false);
        
        // Debug.Log("钻探预览圆柱体已创建");
    }
    
    void UpdatePreview()
    {
        if (previewCylinder == null || !showPreview) 
        {
            if (previewCylinder != null && previewCylinder.activeInHierarchy)
            {
                previewCylinder.SetActive(false);
            }
            return;
        }
        
        // 使用屏幕中心点进行射线检测
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, useRange, groundLayers))
        {
            // 显示预览
            if (!previewCylinder.activeInHierarchy)
            {
                previewCylinder.SetActive(true);
            }
            
            // 更新预览位置
            Vector3 previewPosition = hit.point + Vector3.down * (boringDepth / 2);
            previewCylinder.transform.position = previewPosition;
            
            // 根据是否可以钻探来改变颜色
            bool canDrill = CanUseOnTarget(hit);
            Renderer renderer = previewCylinder.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                Color previewColor = canDrill ? validPreviewColor : invalidPreviewColor;
                Material material = renderer.material;
                material.color = new Color(previewColor.r, previewColor.g, previewColor.b, 0.3f);
            }
        }
        else
        {
            // 隐藏预览
            if (previewCylinder.activeInHierarchy)
            {
                previewCylinder.SetActive(false);
            }
        }
    }
    
    
    void OnDestroy()
    {
        // 清理预览对象
        if (previewCylinder != null)
        {
            DestroyImmediate(previewCylinder);
        }
    }
    
    protected override bool CanUseOnTarget(RaycastHit hit)
    {
        int hitLayer = 1 << hit.collider.gameObject.layer;
        return (groundLayers.value & hitLayer) != 0;
    }
    
    protected override void UseTool(RaycastHit hit)
    {
        Debug.Log("开始钻探...");
        
        // 根据配置选择钻探方式
        if (useGeometricCutting)
        {
            // 使用真实几何切割系统
            StartCoroutine(PerformRealGeometricDrilling(hit));
        }
        else
        {
            // 使用传统系统
            StartCoroutine(PerformDrilling(hit));
        }
    }
    
    System.Collections.IEnumerator PerformRealGeometricDrilling(RaycastHit hit)
    {
        Vector3 drillingPosition = hit.point;
        Vector3 drillingDirection = Vector3.down;
        
        Debug.Log($"🎯 开始真实几何切割 - 钻探点: {drillingPosition}");
        
        // 验证钻探点的地层情况
        ValidateDrillingLocation(drillingPosition, hit);
        
        ShowDrillingEffect(drillingPosition);
        
        // 初始化几何切割系统
        GeometricSampleReconstructor reconstructor = GetComponent<GeometricSampleReconstructor>();
        if (reconstructor == null)
        {
            reconstructor = gameObject.AddComponent<GeometricSampleReconstructor>();
        }
        
        yield return new WaitForSeconds(0.5f);
        
        // 创建地形洞
        CreateBoringHole(hit);
        
        yield return new WaitForSeconds(1f);
        
        // 计算合适的样本显示位置
        Vector3 sampleDisplayPosition = CalculateOptimalSamplePosition(drillingPosition, hit);
        var geometricSample = reconstructor.ReconstructSample(
            drillingPosition, 
            drillingDirection, 
            boringRadius, 
            boringDepth, 
            sampleDisplayPosition
        );
        
        HideDrillingEffect();
        
        if (geometricSample != null)
        {
            Debug.Log("真实几何样本创建成功！");
        }
        else
        {
            Debug.LogWarning("几何样本创建失败，回退到简单样本");
            CreateSimpleSample(hit, sampleDisplayPosition);
        }
    }
    
    System.Collections.IEnumerator PerformDrilling(RaycastHit hit)
    {
        Vector3 drillingPosition = hit.point;
        Vector3 drillingDirection = -hit.normal;
        
        ShowDrillingEffect(drillingPosition);
        
        yield return new WaitForSeconds(1.5f);
        
        CreateBoringHole(hit);
        CreateGeologicalSample(hit);
        
        HideDrillingEffect();
        
        Debug.Log("钻探完成！");
    }
    
    void ShowDrillingEffect(Vector3 position)
    {
        if (drillingIndicator != null)
        {
            drillingIndicator.transform.position = position;
            drillingIndicator.SetActive(true);
        }
        
        if (drillingEffect != null)
        {
            drillingEffect.transform.position = position;
            drillingEffect.Play();
        }
    }
    
    void HideDrillingEffect()
    {
        if (drillingIndicator != null)
        {
            drillingIndicator.SetActive(false);
        }
        
        if (drillingEffect != null)
        {
            drillingEffect.Stop();
        }
    }
    
    void CreateBoringHole(RaycastHit hit)
    {
        TerrainHoleSystem holeSystem = hit.collider.GetComponent<TerrainHoleSystem>();
        
        if (holeSystem == null)
        {
            holeSystem = hit.collider.gameObject.AddComponent<TerrainHoleSystem>();
        }
        
        holeSystem.CreateCylindricalHole(hit.point, boringRadius, boringDepth, hit.normal);
    }
    
    void CreateGeologicalSample(RaycastHit hit)
    {
        Debug.Log("开始创建真实地质样本...");
        
        Vector3 drillingStart = hit.point;
        Vector3 sampleSpawnPosition = hit.point + Vector3.up * 0.5f;
        
        if (detectionSystem == null || reconstructionSystem == null)
        {
            Debug.LogError("地质系统未初始化！");
            CreateSimpleSample(hit, sampleSpawnPosition);
            return;
        }
        
        // 使用地质检测系统分析钻探位置
        GeologicalSampleData sampleData = detectionSystem.AnalyzeDrillingSample(
            drillingStart, 
            boringDepth, 
            boringRadius
        );
        
        if (sampleData.segments.Length == 0)
        {
            Debug.LogWarning("未检测到地层，创建简单样本");
            CreateSimpleSample(hit, sampleSpawnPosition);
            return;
        }
        
        // 使用重建系统创建真实地质样本
        GameObject reconstructedSample = reconstructionSystem.ReconstructSample(
            sampleData, 
            sampleSpawnPosition
        );
        
        if (reconstructedSample != null)
        {
            Debug.Log($"成功创建地质样本！包含 {sampleData.layerStats.Length} 种地层");
            
            // 添加样本收集信息
            ReconstructedGeologicalSample sampleComponent = reconstructedSample.GetComponent<ReconstructedGeologicalSample>();
            if (sampleComponent != null)
            {
                sampleComponent.canBePickedUp = true;
            }
        }
        else
        {
            Debug.LogError("样本重建失败，创建简单样本");
            CreateSimpleSample(hit, sampleSpawnPosition);
        }
    }
    
    /// <summary>
    /// 计算样本的最佳显示位置
    /// </summary>
    Vector3 CalculateOptimalSamplePosition(Vector3 drillingPosition, RaycastHit hit)
    {
        // 基础高度：地面位置 + 小幅度偏移
        float baseHeight = hit.point.y;
        
        // 根据地形倾斜度调整高度
        float terrainSlope = Mathf.Abs(Vector3.Dot(hit.normal, Vector3.up));
        float slopeAdjustment = (1f - terrainSlope) * 0.5f; // 倾斜地形稍微高一点
        
        // 计算玩家位置，确保样本在视野范围内
        Camera playerCam = Camera.main;
        Vector3 playerPosition = playerCam != null ? playerCam.transform.position : drillingPosition;
        
        // 计算合适的悬浮高度（使用Inspector中的参数）
        float playerDistance = Vector3.Distance(playerPosition, drillingPosition);
        
        // 根据玩家距离调整高度（距离越远，样本稍微高一点便于观察）
        float distanceAdjustment = Mathf.Clamp(playerDistance * 0.1f, 0f, 0.8f);
        
        float finalHeight = baseHeight + minSampleHeight + slopeAdjustment + distanceAdjustment;
        finalHeight = Mathf.Min(finalHeight, baseHeight + maxSampleHeight);
        
        // 确保不会太低（至少在地面上方）
        finalHeight = Mathf.Max(finalHeight, baseHeight + minSampleHeight);
        
        Vector3 samplePosition = new Vector3(drillingPosition.x, finalHeight, drillingPosition.z);
        
        // 稍微向玩家方向偏移，便于观察（使用Inspector参数）
        if (playerCam != null)
        {
            Vector3 toPlayer = (playerPosition - drillingPosition).normalized;
            toPlayer.y = 0; // 只在水平面偏移
            samplePosition += toPlayer * playerDirectionOffset; // 可调整的偏移距离
        }
        
        Debug.Log($"样本显示位置计算 - 钻探点: {drillingPosition.y:F2}m, 样本高度: {finalHeight:F2}m, 悬浮: {finalHeight - baseHeight:F2}m");
        
        return samplePosition;
    }
    
    void CreateSimpleSample(RaycastHit hit, Vector3 position)
    {
        Debug.Log("创建简单地质样本作为备选方案");
        
        GameObject sampleObj = new GameObject("Simple Geological Sample");
        sampleObj.transform.position = position;
        
        // 创建简单圆柱体样本
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.transform.SetParent(sampleObj.transform);
        cylinder.transform.localPosition = Vector3.zero;
        cylinder.transform.localScale = new Vector3(boringRadius * 2, boringDepth / 2, boringRadius * 2);
        
        // 应用地面材质
        MeshRenderer hitRenderer = hit.collider.GetComponent<MeshRenderer>();
        if (hitRenderer != null && hitRenderer.material != null)
        {
            cylinder.GetComponent<MeshRenderer>().material = hitRenderer.material;
        }
        
        // 添加物理组件
        SamplePhysicsManager physicsManager = sampleObj.AddComponent<SamplePhysicsManager>();
        physicsManager.mass = boringRadius * boringDepth * 0.5f;
        
        // 添加基础地质样本组件
        GeologicalSample basicSample = sampleObj.AddComponent<GeologicalSample>();
        basicSample.Initialize(position, Quaternion.identity, hitRenderer?.material, hit.collider.gameObject.name);
        basicSample.sampleRadius = boringRadius;
        basicSample.sampleHeight = boringDepth;
    }
    
    protected override void OnEquip()
    {
        base.OnEquip();
        
        // 启用预览功能
        showPreview = true;
        if (previewCylinder != null)
        {
            previewCylinder.SetActive(false); // 先隐藏，Update中会根据需要显示
        }
        
        if (useGeometricCutting && simpleGeometricTool != null)
        {
            // Debug.Log("简化几何钻探工具已装备 - 瞄准地面进行几何采样（预览模式开启）");
        }
        else
        {
            // Debug.Log("传统钻探工具已装备 - 瞄准地面进行钻探采样（预览模式开启）");
        }
    }
    
    protected override void OnUnequip()
    {
        base.OnUnequip();
        
        // 禁用预览功能
        showPreview = false;
        if (previewCylinder != null)
        {
            previewCylinder.SetActive(false);
        }
        
        // Debug.Log("钻探工具已卸载，预览模式关闭");
    }
    
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        
        if (playerCamera != null)
        {
            RaycastHit hit;
            Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
            
            if (Physics.Raycast(ray, out hit, useRange))
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(hit.point, boringRadius);
                
                Gizmos.color = Color.green;
                Gizmos.DrawLine(hit.point, hit.point - hit.normal * boringDepth);
            }
        }
    }
    
    /// <summary>
    /// 验证钻探位置的地层信息
    /// </summary>
    void ValidateDrillingLocation(Vector3 drillingPosition, RaycastHit hit)
    {
        // Debug.Log($"🔍 验证钻探位置: {drillingPosition}");
        
        // 获取射线击中的对象信息
        GameObject hitObject = hit.collider.gameObject;
        // Debug.Log($"🎯 射线击中对象: {hitObject.name}");
        
        // 检查击中对象的材质
        MeshRenderer hitRenderer = hitObject.GetComponent<MeshRenderer>();
        if (hitRenderer != null && hitRenderer.material != null)
        {
            // Debug.Log($"🎨 击中对象材质: {hitRenderer.material.name}, 颜色: {hitRenderer.material.color}");
        }
        
        // 检查击中对象是否是地层
        GeologyLayer hitLayer = hitObject.GetComponent<GeologyLayer>();
        if (hitLayer != null)
        {
            // Debug.Log($"🗿 击中地层: {hitLayer.layerName}, 地层材质: {hitLayer.layerMaterial?.name}, 地层颜色: {hitLayer.layerColor}");
        }
        else
        {
            // Debug.LogWarning($"⚠️ 击中对象不是地层，正在寻找附近的地层...");
            
            // 搜索钻探点附近的地层
            GeologyLayer[] nearbyLayers = FindObjectsByType<GeologyLayer>(FindObjectsSortMode.None);
            GeologyLayer closestLayer = null;
            float minDistance = float.MaxValue;
            
            foreach (GeologyLayer layer in nearbyLayers)
            {
                float distance = Vector3.Distance(drillingPosition, layer.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestLayer = layer;
                }
            }
            
            if (closestLayer != null)
            {
                // Debug.Log($"🔍 最近的地层: {closestLayer.layerName} (距离: {minDistance:F2}m)");
            }
        }
    }
}
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// 钻探深度记录 - 记录每次钻探结束时的精确位置信息
/// </summary>
[System.Serializable]
public class DrillDepthRecord
{
    public float depth; // 深度（从地表算起）
    public Vector3 worldPosition; // 世界坐标位置
    public Vector3 direction; // 钻探方向
    public List<LayerInfo> layersAtDepth; // 该深度处的地层信息
    
    [System.Serializable]
    public class LayerInfo
    {
        public string layerName;
        public float remainingThickness; // 该地层剩余厚度
        public Vector3 layerContactPoint; // 地层接触点
    }
}

/// <summary>
/// 可放置的钻塔工具 - 支持多层深度采集
/// 可以在同一位置进行多次采集：0-2m, 2-4m, 4-6m, 6-8m, 8-10m
/// 样本会围绕钻塔呈环形排列
/// </summary>
public class DrillTowerTool : PlaceableTool
{
    [Header("钻塔设置")]
    public GameObject drillTowerPrefab; // 钻塔预制件
    public float interactionRange = 3f; // 交互范围
    public int maxDrillDepths = 5; // 最大钻探次数
    public float depthPerDrill = 2f; // 每次钻探深度
    
    [Header("样本排列")]
    public float sampleRingRadius = 2.5f; // 样本环形半径
    public float sampleElevation = 3.0f; // 样本悬浮高度，可在Inspector中调整
    public float sampleSpacing = 0.8f; // 样本间最小间距
    
    [Header("钻探效果")]
    public ParticleSystem drillingEffectPrefab; // 钻探粒子效果
    public AudioClip drillingSound; // 钻探音效
    public Material activeDrillMaterial; // 钻探中的材质
    public Material inactiveDrillMaterial; // 闲置状态材质
    
    private DrillTower placedTower; // 已放置的钻塔引用
    
    protected override void Start()
    {
        base.Start();
        toolName = "钻塔工具";
        
        // 设置预制件
        if (drillTowerPrefab != null)
        {
            prefabToPlace = drillTowerPrefab;
        }
        
        Debug.Log("钻塔工具初始化完成");
    }
    
    protected override void Update()
    {
        base.Update();
        
        // 检查与已放置钻塔的交互
        if (!isPlacementMode && hasPlacedObject && placedTower != null)
        {
            CheckTowerInteraction();
            CheckTowerRecall(); // 检查是否要收回钻塔
        }
    }
    
    /// <summary>
    /// 检查玩家是否可以与钻塔交互
    /// </summary>
    void CheckTowerInteraction()
    {
        if (placedTower == null) return;
        
        float distance = Vector3.Distance(playerCamera.transform.position, placedTower.transform.position);
        
        if (distance <= interactionRange)
        {
            // 显示交互提示 - 使用F键交互
            if (Keyboard.current.fKey.wasPressedThisFrame)
            {
                InteractWithTower();
            }
        }
    }
    
    /// <summary>
    /// 检查是否要收回钻塔
    /// </summary>
    void CheckTowerRecall()
    {
        if (placedTower == null) return;
        
        float distance = Vector3.Distance(playerCamera.transform.position, placedTower.transform.position);
        
        if (distance <= interactionRange)
        {
            // 按G键收回钻塔
            if (Keyboard.current.gKey.wasPressedThisFrame)
            {
                RecallTower();
            }
        }
    }
    
    /// <summary>
    /// 收回钻塔
    /// </summary>
    void RecallTower()
    {
        if (placedTower == null)
        {
            Debug.LogWarning("没有找到要收回的钻塔！");
            return;
        }
        
        // 检查钻塔是否正在钻探
        if (placedTower.isDrilling)
        {
            Debug.Log("钻塔正在钻探中，无法收回！");
            return;
        }
        
        Debug.Log($"收回钻塔，已采集 {placedTower.collectedSamples.Count} 个样本");
        
        // 销毁所有采集的样本
        foreach (GameObject sample in placedTower.collectedSamples)
        {
            if (sample != null)
            {
                Destroy(sample);
            }
        }
        
        // 销毁钻塔对象
        if (placedTower.gameObject != null)
        {
            Destroy(placedTower.gameObject);
        }
        
        // 重置状态，允许重新放置
        placedTower = null;
        hasPlacedObject = false;
        canUse = true;
        
        Debug.Log("✅ 钻塔已收回，可以重新放置");
    }
    
    /// <summary>
    /// 与钻塔交互，进行钻探
    /// </summary>
    void InteractWithTower()
    {
        if (placedTower == null)
        {
            Debug.LogWarning("没有找到钻塔引用！");
            return;
        }
        
        if (placedTower.CanDrill())
        {
            placedTower.StartDrilling();
            Debug.Log($"开始第 {placedTower.CurrentDrillCount + 1} 次钻探");
        }
        else
        {
            Debug.Log("钻塔已达到最大钻探次数或正在钻探中");
        }
    }
    
    protected override void OnObjectPlaced(GameObject placedObject)
    {
        base.OnObjectPlaced(placedObject);
        
        // 确保钻塔可见和物理效果正常
        placedObject.SetActive(true);
        Debug.Log($"钻塔放置成功: {placedObject.name} 在位置 {placedObject.transform.position}");
        
        // 确保物理组件正常工作
        Rigidbody rb = placedObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = true; // 确保重力开启
            rb.isKinematic = false; // 确保不是运动学刚体
            Debug.Log("钻塔物理效果已启用");
        }
        
        // 获取或添加DrillTower组件
        placedTower = placedObject.GetComponent<DrillTower>();
        if (placedTower == null)
        {
            placedTower = placedObject.AddComponent<DrillTower>();
        }
        
        // 初始化钻塔
        placedTower.Initialize(this);
        
        // 立即修复可见性问题
        FixTowerVisibility(placedObject);
        
        Debug.Log($"✅ 钻塔完全初始化完成: {placedObject.transform.position}");
    }
    
    /// <summary>
    /// 立即修复钻塔可见性
    /// </summary>
    void FixTowerVisibility(GameObject towerObj)
    {
        Debug.Log($"🔧 立即修复钻塔可见性: {towerObj.name}");
        
        // 确保所有渲染器都启用且有正确材质
        Renderer[] renderers = towerObj.GetComponentsInChildren<Renderer>();
        
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = true;
            
            // 如果材质有问题，创建新的可见材质
            if (renderer.material == null || renderer.material.color.a < 0.5f)
            {
                Material visibleMaterial = new Material(Shader.Find("Standard"));
                visibleMaterial.color = new Color(0.8f, 0.3f, 0.1f, 1f); // 橙红色
                renderer.material = visibleMaterial;
                Debug.Log($"   修复了渲染器材质: {renderer.gameObject.name}");
            }
        }
        
        // 确保缩放正常
        if (towerObj.transform.localScale.magnitude < 0.1f)
        {
            towerObj.transform.localScale = Vector3.one;
            Debug.Log("   修复了缩放问题");
        }
        
        // 添加临时发光效果，确保可见
        AddTemporaryGlow(towerObj);
    }
    
    /// <summary>
    /// 添加临时发光效果
    /// </summary>
    void AddTemporaryGlow(GameObject towerObj)
    {
        // 创建发光球体
        GameObject glowSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        glowSphere.name = "TowerGlow";
        glowSphere.transform.SetParent(towerObj.transform);
        glowSphere.transform.localPosition = Vector3.up * 2f;
        glowSphere.transform.localScale = Vector3.one * 0.5f;
        
        // 设置发光材质
        Material glowMaterial = new Material(Shader.Find("Standard"));
        glowMaterial.color = Color.yellow;
        glowMaterial.EnableKeyword("_EMISSION");
        glowMaterial.SetColor("_EmissionColor", Color.yellow * 2f);
        
        glowSphere.GetComponent<Renderer>().material = glowMaterial;
        
        // 移除碰撞器
        Collider glowCollider = glowSphere.GetComponent<Collider>();
        if (glowCollider != null)
        {
            DestroyImmediate(glowCollider);
        }
        
        Debug.Log("   ✅ 添加了临时发光标记");
    }
    
    /// <summary>
    /// 获取样本放置位置（环形排列，自动避免重叠）
    /// </summary>
    public Vector3 GetSamplePosition(Vector3 towerPosition, int drillIndex)
    {
        // 简化的确定性环形排列算法
        float angle = (drillIndex * 360f / maxDrillDepths) * Mathf.Deg2Rad;
        
        // 确定性的水平偏移
        Vector3 horizontalOffset = new Vector3(
            Mathf.Sin(angle) * sampleRingRadius,
            0,
            Mathf.Cos(angle) * sampleRingRadius
        );
        
        Vector3 targetPosition = towerPosition + horizontalOffset;
        
        // 简单的地面检测，确保样本悬浮在合适高度
        RaycastHit hit;
        if (Physics.Raycast(targetPosition + Vector3.up * 10f, Vector3.down, out hit))
        {
            targetPosition.y = hit.point.y + sampleElevation;
        }
        else
        {
            targetPosition.y = towerPosition.y + sampleElevation;
        }
        
        Debug.Log($"样本 {drillIndex} 位置: 角度 {angle * Mathf.Rad2Deg:F1}°, 半径 {sampleRingRadius:F1}m, 高度 {targetPosition.y:F1}m");
        
        return targetPosition;
    }
    
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        
        if (placedTower != null)
        {
            // 绘制交互范围
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(placedTower.transform.position, interactionRange);
            
            // 绘制样本环形排列
            Gizmos.color = Color.green;
            for (int i = 0; i < maxDrillDepths; i++)
            {
                Vector3 samplePos = GetSamplePosition(placedTower.transform.position, i);
                Gizmos.DrawWireSphere(samplePos, 0.3f);
            }
        }
    }
}

/// <summary>
/// 钻塔组件 - 负责管理钻探逻辑
/// </summary>
public class DrillTower : MonoBehaviour
{
    [Header("钻塔状态")]
    public int currentDrillCount = 0;
    public bool isDrilling = false;
    public Vector3 drillingPosition;
    
    [Header("连续钻探记录")]
    public List<DrillDepthRecord> depthRecords = new List<DrillDepthRecord>(); // 记录每个深度点的信息
    
    private DrillTowerTool toolReference;
    public List<GameObject> collectedSamples = new List<GameObject>();
    private Renderer towerRenderer;
    private AudioSource audioSource;
    private ParticleSystem drillingEffect;
    
    public int CurrentDrillCount => currentDrillCount;
    
    void Start()
    {
        drillingPosition = transform.position;
        towerRenderer = GetComponent<Renderer>();
        audioSource = GetComponent<AudioSource>();
        
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        UpdateTowerAppearance();
    }
    
    public void Initialize(DrillTowerTool tool)
    {
        toolReference = tool;
        Debug.Log("钻塔初始化完成");
    }
    
    public bool CanDrill()
    {
        return !isDrilling && currentDrillCount < toolReference.maxDrillDepths;
    }
    
    public void StartDrilling()
    {
        if (!CanDrill()) return;
        
        StartCoroutine(DrillingProcess());
    }
    
    IEnumerator DrillingProcess()
    {
        isDrilling = true;
        UpdateTowerAppearance();
        
        // 播放钻探效果
        PlayDrillingEffects();
        
        float currentDepthStart = currentDrillCount * toolReference.depthPerDrill;
        float currentDepthEnd = currentDepthStart + toolReference.depthPerDrill;
        
        Debug.Log($"开始钻探深度 {currentDepthStart:F1}m - {currentDepthEnd:F1}m");
        
        // 钻探动画延迟
        yield return new WaitForSeconds(2.0f);
        
        // 执行实际钻探
        PerformDrilling(currentDepthStart, currentDepthEnd);
        
        // 停止效果
        StopDrillingEffects();
        
        currentDrillCount++;
        isDrilling = false;
        UpdateTowerAppearance();
        
        Debug.Log($"钻探完成！当前已钻探 {currentDrillCount}/{toolReference.maxDrillDepths} 次");
    }
    
    void PerformDrilling(float depthStart, float depthEnd)
    {
        // 计算样本位置（环形排列）
        Vector3 samplePosition = toolReference.GetSamplePosition(drillingPosition, currentDrillCount);
        
        // 使用现有的几何切割系统进行钻探
        GeometricSampleReconstructor reconstructor = FindFirstObjectByType<GeometricSampleReconstructor>();
        if (reconstructor == null)
        {
            reconstructor = gameObject.AddComponent<GeometricSampleReconstructor>();
        }
        
        Vector3 actualDrillingStart;
        Vector3 drillingDirection = Vector3.down;
        float actualDrillingDepth;
        float drillingRadius = 0.1f;
        
        // 🔧 钻塔连续钻探修复：先找到真正的地面位置，然后从地面开始钻探
        // 关键修复：避免射线检测到钻塔自身，使用真正的地面位置作为起点
        
        // 第1步：从钻塔上方检测地面位置
        Vector3 skyPosition = drillingPosition + Vector3.up * 10f;
        RaycastHit groundHit;
        Vector3 realGroundPosition = drillingPosition; // 默认使用钻塔位置
        
        // 检测真正的地面位置（忽略钻塔自身，优先检测表层）
        RaycastHit[] allHits = Physics.RaycastAll(skyPosition, Vector3.down, 15f);
        
        // 🔧 修复：优先寻找表层地层（dem），而不是第一个击中的地层
        GeologyLayer surfaceLayer = null;
        RaycastHit surfaceHit = new RaycastHit();
        bool foundSurface = false;
        
        Debug.Log($"🔍 地面检测：从 {skyPosition} 向下射线检测，共击中 {allHits.Length} 个对象");
        
        foreach (RaycastHit hit in allHits)
        {
            // 跳过钻塔自身的组件
            if (hit.collider.name.Contains("DrillTower") || hit.collider.name.Contains("Tower") || hit.collider.name.Contains("Drill"))
            {
                Debug.Log($"   跳过钻塔组件: {hit.collider.name}");
                continue;
            }
            
            GeologyLayer geoLayer = hit.collider.GetComponent<GeologyLayer>();
            if (geoLayer != null)
            {
                Debug.Log($"   击中地层: {geoLayer.layerName} 距离: {hit.distance:F2}m 位置: {hit.point}");
                
                // 🔧 优先选择表层地层（dem）
                if (geoLayer.layerName == "dem")
                {
                    surfaceLayer = geoLayer;
                    surfaceHit = hit;
                    foundSurface = true;
                    Debug.Log($"   ✅ 找到表层地层: {geoLayer.layerName}");
                    break;
                }
                // 如果还没找到表层，记录第一个找到的地层作为备选
                else if (!foundSurface)
                {
                    surfaceLayer = geoLayer;
                    surfaceHit = hit;
                    Debug.Log($"   📝 记录备选地层: {geoLayer.layerName}");
                }
            }
        }
        
        if (foundSurface || surfaceLayer != null)
        {
            realGroundPosition = surfaceHit.point;
            Debug.Log($"🌍 确定地面位置: {realGroundPosition} (地层: {surfaceLayer.layerName})");
        }
        else
        {
            Debug.LogWarning($"⚠️ 未找到有效地面，使用钻塔位置: {realGroundPosition}");
        }
        
        // 第2步：直接从地面位置开始钻探，确保检测到表层地层
        actualDrillingStart = realGroundPosition; // 直接从地面开始
        actualDrillingDepth = depthEnd; // 标准深度，不需要补偿
        
        Debug.Log($"   射线起点: {actualDrillingStart} (直接从地面开始)");
        Debug.Log($"   检测深度: {actualDrillingDepth:F2}m (标准深度)");
        Debug.Log($"   提取范围: {depthStart:F1}m-{depthEnd:F1}m (通过深度范围参数筛选地层)");
        Debug.Log($"   策略: 地面射线检测+深度范围筛选，确保检测表层地层");
        
        GeometricSampleReconstructor.ReconstructedSample geometricSample;
        
        // 使用6参数版本，传递正确的深度范围
        geometricSample = reconstructor.ReconstructSample(
            actualDrillingStart,
            drillingDirection,
            drillingRadius,
            actualDrillingDepth,
            samplePosition,
            depthStart,
            depthEnd
        );
        
        if (geometricSample != null && geometricSample.sampleContainer != null)
        {
            collectedSamples.Add(geometricSample.sampleContainer);
            Debug.Log($"✅ 成功创建钻探样本 {depthStart:F1}m-{depthEnd:F1}m");
            
            // 统一的样本组成分析（所有层都使用）
            Debug.Log($"🔍 样本分析（第{currentDrillCount + 1}次钻探）:");
            Debug.Log($"   样本ID: {geometricSample.sampleID}");
            Debug.Log($"   地层段数量: {geometricSample.layerSegments?.Length ?? 0}");
            
            if (geometricSample.layerSegments != null)
            {
                for (int i = 0; i < geometricSample.layerSegments.Length; i++)
                {
                    var segment = geometricSample.layerSegments[i];
                    if (segment != null && segment.sourceLayer != null)
                    {
                        Debug.Log($"   地层段 {i}: {segment.sourceLayer.name}");
                        Debug.Log($"     相对深度: {segment.relativeDepth:F3}m");
                        Debug.Log($"     材质: {segment.material?.name ?? "无"}");
                        Debug.Log($"     对象: {segment.segmentObject?.name ?? "无"}");
                    }
                }
            }
            
            // 记录本次钻探结束时的深度信息
            RecordDepthInfo(depthEnd);
            
            // 设置样本标识
            DepthSampleMarker marker = geometricSample.sampleContainer.AddComponent<DepthSampleMarker>();
            marker.depthStart = depthStart;
            marker.depthEnd = depthEnd;
            marker.drillIndex = currentDrillCount;
            marker.parentTower = this;
            
        }
    }
    
    /// <summary>
    /// 记录钻探深度信息
    /// </summary>
    void RecordDepthInfo(float totalDepthFromSurface)
    {
        // 计算从地表算起的绝对位置
        Vector3 depthPosition = drillingPosition + Vector3.down * totalDepthFromSurface;
        
        DrillDepthRecord record = new DrillDepthRecord
        {
            depth = totalDepthFromSurface,
            worldPosition = depthPosition,
            direction = Vector3.down,
            layersAtDepth = new List<DrillDepthRecord.LayerInfo>()
        };
        
        depthRecords.Add(record);
        
        Debug.Log($"📝 记录深度信息: 总深度 {totalDepthFromSurface:F1}m, 世界位置 {depthPosition}");
        Debug.Log($"📝 当前记录总数: {depthRecords.Count}");
    }
    
    // 移除了AddSampleLabel方法，不再显示深度标签
    // void AddSampleLabel(GameObject sample, float depthStart, float depthEnd)
    // {
    //     // 创建样本标签
    //     GameObject labelObj = new GameObject("SampleLabel");
    //     labelObj.transform.SetParent(sample.transform);
    //     labelObj.transform.localPosition = Vector3.up * 0.5f;
    //     
    //     TextMesh textMesh = labelObj.AddComponent<TextMesh>();
    //     textMesh.text = $"{depthStart:F1}m-{depthEnd:F1}m";
    //     textMesh.fontSize = 20;
    //     textMesh.color = Color.white;
    //     textMesh.anchor = TextAnchor.MiddleCenter;
    //     
    //     // 让文字始终面向玩家
    //     RotateTowardsPlayer rotateScript = labelObj.AddComponent<RotateTowardsPlayer>();
    // }
    
    void PlayDrillingEffects()
    {
        // 播放钻探音效
        if (audioSource != null && toolReference.drillingSound != null)
        {
            audioSource.clip = toolReference.drillingSound;
            audioSource.loop = true;
            audioSource.Play();
        }
        
        // 播放粒子效果
        if (toolReference.drillingEffectPrefab != null)
        {
            drillingEffect = Instantiate(toolReference.drillingEffectPrefab, transform.position, Quaternion.identity);
            drillingEffect.transform.SetParent(transform);
        }
    }
    
    void StopDrillingEffects()
    {
        // 停止音效
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        
        // 停止粒子效果
        if (drillingEffect != null)
        {
            drillingEffect.Stop();
            Destroy(drillingEffect.gameObject, 2f);
        }
    }
    
    void UpdateTowerAppearance()
    {
        if (towerRenderer == null || toolReference == null) return;
        
        // 根据状态更新材质
        if (isDrilling && toolReference.activeDrillMaterial != null)
        {
            towerRenderer.material = toolReference.activeDrillMaterial;
        }
        else if (toolReference.inactiveDrillMaterial != null)
        {
            towerRenderer.material = toolReference.inactiveDrillMaterial;
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (toolReference != null)
        {
            // 绘制已钻探的深度层
            Gizmos.color = Color.red;
            for (int i = 0; i < currentDrillCount; i++)
            {
                float depth = i * toolReference.depthPerDrill;
                Vector3 layerPosition = drillingPosition + Vector3.down * depth;
                Gizmos.DrawWireCube(layerPosition, new Vector3(0.4f, 0.1f, 0.4f));
            }
            
            // 绘制下一层钻探位置
            if (currentDrillCount < toolReference.maxDrillDepths)
            {
                Gizmos.color = Color.yellow;
                float nextDepth = currentDrillCount * toolReference.depthPerDrill;
                Vector3 nextPosition = drillingPosition + Vector3.down * nextDepth;
                Gizmos.DrawWireCube(nextPosition, new Vector3(0.4f, 0.1f, 0.4f));
            }
        }
    }
}

/// <summary>
/// 深度样本标记组件 - 用于标识来自钻塔的特定深度样本
/// </summary>
public class DepthSampleMarker : MonoBehaviour
{
    [Header("深度信息")]
    public float depthStart;
    public float depthEnd;
    public int drillIndex;
    
    [Header("钻塔引用")]
    public DrillTower parentTower;
    
    [Header("显示设置")]
    public bool showDepthInfo = false; // 默认不显示深度信息
    public Color depthLabelColor = Color.white;
    
    void Start()
    {
        if (showDepthInfo)
        {
            UpdateDepthDisplay();
        }
    }
    
    void UpdateDepthDisplay()
    {
        // 不显示深度信息，深度数据仅保存在组件中
        // TextMesh textMesh = GetComponentInChildren<TextMesh>();
        // if (textMesh != null)
        // {
        //     textMesh.text = $"第{drillIndex + 1}次\n{depthStart:F1}m-{depthEnd:F1}m";
        //     textMesh.color = depthLabelColor;
        // }
    }
    
    /// <summary>
    /// 获取样本描述信息
    /// </summary>
    public string GetSampleDescription()
    {
        float thickness = depthEnd - depthStart;
        return $"钻塔样本 #{drillIndex + 1}\n深度: {depthStart:F1}m - {depthEnd:F1}m\n厚度: {thickness:F1}m";
    }
    
}


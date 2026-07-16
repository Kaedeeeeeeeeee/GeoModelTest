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
    
    public DrillTower placedTower; // 已放置的钻塔引用
    private MobileInputManager mobileInputManager; // 移动端输入管理器
    private bool wasFKeyPressedLastFrame = false; // 上一帧F键状态
    
    protected override void Start()
    {
        base.Start();
        toolName = "钻塔工具";

        // 设置预制件
        if (drillTowerPrefab != null)
        {
            prefabToPlace = drillTowerPrefab;
        }

        // 获取移动端输入管理器
        mobileInputManager = MobileInputManager.Instance;
        if (mobileInputManager == null)
        {
            mobileInputManager = FindObjectOfType<MobileInputManager>();
        }

        // 场景切换后恢复钻塔引用
        if (hasPlacedObject && placedTower == null)
        {
            RestorePlacedTowerReference();
        }

        // 钻塔工具初始化完成
    }

    /// <summary>
    /// 场景切换后恢复已放置钻塔的引用
    /// </summary>
    void RestorePlacedTowerReference()
    {
        DrillTower[] towers = FindObjectsByType<DrillTower>(FindObjectsSortMode.None);
        if (towers.Length > 0)
        {
            // 找到属于此工具的钻塔
            foreach (var tower in towers)
            {
                if (tower.toolReference == null || tower.toolReference == this)
                {
                    placedTower = tower;
                    tower.Initialize(this);
                    Debug.Log("[DrillTowerTool] 场景切换后恢复钻塔引用");
                    break;
                }
            }
        }
    }

    /// <summary>
    /// 检测F键输入 - 支持键盘和移动端虚拟按钮
    /// </summary>
    bool IsFKeyPressed()
    {
        // 键盘F键检测
        var keyboard = Keyboard.current;
        bool keyboardFPressed = keyboard != null && keyboard.fKey.wasPressedThisFrame;

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
    
    protected override void Update()
    {
        base.Update();

        if (IsToolInputBlocked)
        {
            return;
        }
        
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
            if (IsFKeyPressed())
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
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.gKey.wasPressedThisFrame)
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
            return;
        }
        
        // 检查钻塔是否正在钻探
        if (placedTower.isDrilling)
        {
            return;
        }
        
        
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
        
    }
    
    /// <summary>
    /// 与钻塔交互，进行钻探
    /// </summary>
    void InteractWithTower()
    {
        if (placedTower == null)
        {
            return;
        }
        
        if (placedTower.CanDrill())
        {
            placedTower.StartDrilling();
        }
        else
        {
        }
    }
    
    protected override void OnObjectPlaced(GameObject placedObject)
    {
        base.OnObjectPlaced(placedObject);
        
        // 确保钻塔可见和物理效果正常
        placedObject.SetActive(true);
        
        // 确保物理组件正常工作
        Rigidbody rb = placedObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = true; // 确保重力开启
            rb.isKinematic = false; // 确保不是运动学刚体
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
        
    }
    
    /// <summary>
    /// 立即修复钻塔可见性
    /// </summary>
    void FixTowerVisibility(GameObject towerObj)
    {
        
        // 确保所有渲染器都启用且有正确材质
        Renderer[] renderers = towerObj.GetComponentsInChildren<Renderer>();
        
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = true;
            
            // 如果材质有问题，创建新的可见材质
            if (renderer.material == null || renderer.material.color.a < 0.5f)
            {
                Material visibleMaterial = new Material(Shader.Find("Standard"));
                visibleMaterial.color = new Color(1f, 1f, 1f, 1f); // 默认白灰色
                renderer.material = visibleMaterial;
            }
        }
        
        // 确保缩放正常
        if (towerObj.transform.localScale.magnitude < 0.1f)
        {
            towerObj.transform.localScale = Vector3.one;
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
    
    public DrillTowerTool toolReference;
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
    }

    /// <summary>
    /// 确保工具引用有效，场景切换后重新查找
    /// </summary>
    void EnsureToolReference()
    {
        if (toolReference == null)
        {
            // 场景切换后重新查找工具引用
            DrillTowerTool[] tools = FindObjectsByType<DrillTowerTool>(FindObjectsSortMode.None);
            foreach (var tool in tools)
            {
                if (tool.hasPlacedObject && tool.placedTower == this)
                {
                    toolReference = tool;
                    Debug.Log("[DrillTower] 重新找到工具引用");
                    break;
                }
            }

            if (toolReference == null)
            {
                Debug.LogWarning("[DrillTower] 无法找到对应的DrillTowerTool引用");
            }
        }
    }
    
    public bool CanDrill()
    {
        EnsureToolReference();
        return !isDrilling && toolReference != null && currentDrillCount < toolReference.maxDrillDepths;
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

        // 钻探循环音（挂在钻塔上的 3D 循环）
        var drillLoop = GeoModel.AudioSystem.AudioManager.Instance.StartLoopSFX3D(
            GeoModel.AudioSystem.AudioKeys.SFX.DrillLoop, transform);

        EnsureToolReference();
        if (toolReference == null)
        {
            if (drillLoop != null) GeoModel.AudioSystem.AudioManager.Instance.StopLoop(drillLoop, 0.2f);
            yield break;
        }

        float currentDepthStart = currentDrillCount * toolReference.depthPerDrill;
        float currentDepthEnd = currentDepthStart + toolReference.depthPerDrill;


        // 钻探动画延迟
        yield return new WaitForSeconds(2.0f);

        // 执行实际钻探
        PerformDrilling(currentDepthStart, currentDepthEnd);

        // 停止效果
        StopDrillingEffects();
        if (drillLoop != null) GeoModel.AudioSystem.AudioManager.Instance.StopLoop(drillLoop, 0.3f);
        GeoModel.AudioSystem.AudioManager.Instance.PlaySFX3D(
            GeoModel.AudioSystem.AudioKeys.SFX.DrillComplete, transform.position);

        currentDrillCount++;
        isDrilling = false;
        UpdateTowerAppearance();

    }
    
    void PerformDrilling(float depthStart, float depthEnd)
    {
        EnsureToolReference();
        if (toolReference == null) return;

        // 计算样本位置（环形排列）
        Vector3 samplePosition = toolReference.GetSamplePosition(drillingPosition, currentDrillCount);
        
        // 🚀 使用集成了v1.3检测的几何重建系统进行钻探
        GeometricSampleReconstructor reconstructor = FindFirstObjectByType<GeometricSampleReconstructor>();
        if (reconstructor == null)
        {
            reconstructor = gameObject.AddComponent<GeometricSampleReconstructor>();
        }
        
        Vector3 actualDrillingStart;
        Vector3 drillingDirection = Vector3.down;
        float actualDrillingDepth;
        float drillingRadius = 0.1f;
        
        // 🔧 修复钻探起点计算：确保从真正的地表开始钻探
        // 核心问题：原来的起点计算低于地层表面，导致只检测到深层地层
        
        // 第1步：找到钻塔水平位置上的最高地层表面
        Vector3 horizontalPosition = new Vector3(drillingPosition.x, 0, drillingPosition.z);
        Vector3 skyPosition = horizontalPosition + Vector3.up * 50f; // 从高空开始检测
        
        RaycastHit[] allHits = Physics.RaycastAll(skyPosition, Vector3.down, 100f);
        
        // 🔧 验证：检查钻塔脚下的实际地表位置
        Vector3 towerBase = drillingPosition + Vector3.down * 0.5f; // 钻塔底部位置
        RaycastHit[] nearbyHits = Physics.RaycastAll(towerBase + Vector3.up * 2f, Vector3.down, 5f);
        
        // 🔧 找到最高的地层表面（确保从真正的地表开始）
        GeologyLayer highestLayer = null;
        float highestY = float.MinValue;
        Vector3 surfacePoint = drillingPosition; // 默认值
        
        
        foreach (RaycastHit hit in allHits)
        {
            // 跳过钻塔自身的组件
            if (hit.collider.name.Contains("DrillTower") || 
                hit.collider.name.Contains("Tower") || 
                hit.collider.name.Contains("Drill") ||
                hit.collider.name.Contains("Glow"))
            {
                continue;
            }
            
            GeologyLayer geoLayer = hit.collider.GetComponent<GeologyLayer>();
            if (geoLayer != null)
            {
                // 选择Y坐标最高的地层表面（真正的地表）
                if (hit.point.y > highestY)
                {
                    highestY = hit.point.y;
                    highestLayer = geoLayer;
                    surfacePoint = hit.point;
                }
            }
        }
        
        // 🔧 验证并比较钻塔脚下的地表
        GeologyLayer nearestLayer = null;
        float nearestY = float.MinValue;
        Vector3 nearestSurfacePoint = surfacePoint;
        
        foreach (RaycastHit hit in nearbyHits)
        {
            if (hit.collider.name.Contains("DrillTower") || 
                hit.collider.name.Contains("Tower") || 
                hit.collider.name.Contains("Drill") ||
                hit.collider.name.Contains("Glow"))
            {
                continue;
            }
            
            GeologyLayer geoLayer = hit.collider.GetComponent<GeologyLayer>();
            if (geoLayer != null && hit.point.y > nearestY)
            {
                nearestY = hit.point.y;
                nearestLayer = geoLayer;
                nearestSurfacePoint = hit.point;
            }
        }
        
        // 🔧 选择最合适的地表位置
        if (highestLayer != null && nearestLayer != null)
        {
            float heightDifference = Mathf.Abs(highestY - nearestY);
            if (heightDifference > 0.5f) // 如果高度差超过0.5米，报告差异
            {
                // 使用钻塔脚下的地表位置，更准确
                surfacePoint = nearestSurfacePoint;
            }
            else
            {
                // 高度差异不大，使用原来的最高地表
            }
        }
        else if (highestLayer != null)
        {
        }
        else if (nearestLayer != null)
        {
            surfacePoint = nearestSurfacePoint;
        }
        else
        {
            surfacePoint = drillingPosition;
        }
        
        // 第2步：从真正的地表开始钻探
        actualDrillingStart = surfacePoint; // 🔧 关键修复：使用真正的地表位置
        actualDrillingDepth = depthEnd; // 使用目标深度
        
        
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
            
            // 统一的样本组成分析（所有层都使用）
            
            if (geometricSample.layerSegments != null)
            {
                for (int i = 0; i < geometricSample.layerSegments.Length; i++)
                {
                    var segment = geometricSample.layerSegments[i];
                    if (segment != null && segment.sourceLayer != null)
                    {
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
            marker.collectionPosition = actualDrillingStart; // 设置采集位置
            marker.parentTower = this;
            
            // 立即为生成的样本添加收集组件（按需集成）
            DrillToolSampleIntegrator.IntegrateSampleAfterDrilling(
                geometricSample.sampleContainer, 
                "1001", // DrillTowerTool的ID
                $"钻塔钻探_{currentDrillCount}"
            );
            
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
        EnsureToolReference();
        if (toolReference == null) return;

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
        if (towerRenderer == null) return;

        EnsureToolReference();
        if (toolReference == null) return;

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
    
    [Header("位置信息")]
    public Vector3 collectionPosition; // 采集点位置（用于世界坐标计算）
    
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
        
        // 使用世界坐标深度计算
        if (collectionPosition != Vector3.zero)
        {
            string depthInfo = WorldDepthCalculator.GetLocalizedDepthDescription(
                collectionPosition, depthStart, depthEnd, true);
            
            var localizationManager = LocalizationManager.Instance;
            if (localizationManager != null)
            {
                return localizationManager.GetText("sample.drill_tower.description", 
                    (drillIndex + 1).ToString(), depthInfo, thickness.ToString("F1"));
            }
            else
            {
                var (worldDepthStart, worldDepthEnd) = WorldDepthCalculator.CalculateWorldDepthRange(
                    collectionPosition, depthStart, depthEnd);
                return $"钻塔样本 #{drillIndex + 1}\n深度: {worldDepthStart:F1}m - {worldDepthEnd:F1}m (相对: {depthStart:F1}m - {depthEnd:F1}m)\n厚度: {thickness:F1}m";
            }
        }
        else
        {
            // 如果没有位置信息，使用相对深度
            return $"钻塔样本 #{drillIndex + 1}\n深度: {depthStart:F1}m - {depthEnd:F1}m\n厚度: {thickness:F1}m";
        }
    }
    
}

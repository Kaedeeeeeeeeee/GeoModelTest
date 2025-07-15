using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// 地质锤工具 - 薄片样本采集工具
/// 通过多次敲击采集表面地质薄片样本
/// </summary>
public class HammerTool : CollectionTool
{
    [Header("锤子工具设置")]
    public GameObject hammerPrefab; // 锤子预制体
    public int requiredHits = 3; // 需要的敲击次数
    public float collectionRange = 2f; // 采集范围
    public float hitTimeout = 5f; // 敲击超时时间
    public float hitTolerance = 0.5f; // 敲击位置容错范围
    
    [Header("视觉效果")]
    public GameObject targetMarkerPrefab; // 目标标记预制体
    public Material progressMaterial; // 进度显示材质
    
    [Header("音效")]
    public AudioClip hammerHitSound; // 敲击音效
    public AudioClip collectionCompleteSound; // 采集完成音效
    
    // 锤子对象引用
    private GameObject equippedHammer;
    private Transform playerHand; // 玩家手部位置
    
    // 采集状态
    private HammerCollectionState currentCollection;
    private CollectionTargetMarker targetMarker;
    private GameObject targetMarkerObject;
    
    // 音效组件
    private AudioSource audioSource;
    
    protected override void Start()
    {
        base.Start();
        
        // 配置工具基础属性
        toolID = "1002";
        toolName = "地质锤";
        useRange = collectionRange;
        useCooldown = 0.5f; // 短冷却时间，允许快速敲击
        
        // 初始化音效组件
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // 查找玩家手部位置
        FindPlayerHandPosition();
        
        Debug.Log("地质锤工具初始化完成");
    }
    
    /// <summary>
    /// 查找玩家手部位置
    /// </summary>
    void FindPlayerHandPosition()
    {
        // 直接找到玩家模型（FirstPersonController）作为锤子的父对象
        FirstPersonController player = FindFirstObjectByType<FirstPersonController>();
        if (player != null)
        {
            playerHand = player.transform; // 直接使用玩家模型作为父对象
            Debug.Log("找到玩家模型，锤子将作为玩家的子对象");
        }
        else
        {
            Debug.LogWarning("未找到玩家模型，锤子可能无法正确显示");
        }
    }
    
    protected override void HandleInput()
    {
        if (!canUse) return;
        
        // 处理鼠标左键敲击
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (currentCollection == null)
            {
                StartCollection();
            }
            else
            {
                ContinueCollection();
            }
        }
        
        // 处理取消操作
        if (Mouse.current.rightButton.wasPressedThisFrame || 
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CancelCollection();
        }
        
        // 检查超时
        CheckCollectionTimeout();
    }
    
    /// <summary>
    /// 开始采集过程
    /// </summary>
    void StartCollection()
    {
        // 射线检测确定采集位置
        Ray ray = playerCamera.ScreenPointToRay(new Vector2(Screen.width / 2f, Screen.height / 2f));
        
        if (Physics.Raycast(ray, out RaycastHit hit, useRange))
        {
            // 检查是否可以在此位置采集
            if (!CanUseOnTarget(hit))
            {
                return;
            }
            
            // 创建采集状态
            currentCollection = new HammerCollectionState
            {
                targetPosition = hit.point,
                currentHits = 1, // 第一次敲击
                requiredHits = requiredHits,
                lastHitTime = Time.time
            };
            
            // 显示目标标记
            ShowTargetMarker(hit.point);
            
            // 播放第一次敲击动画和音效
            PlayHammerHit();
            
            Debug.Log($"开始采集 - 位置: {hit.point}, 需要敲击: {requiredHits}次");
        }
        else
        {
            ShowMessage("无有效采集目标");
        }
    }
    
    /// <summary>
    /// 继续采集（后续敲击）
    /// </summary>
    void ContinueCollection()
    {
        if (currentCollection == null) return;
        
        // 检测敲击位置
        Ray ray = playerCamera.ScreenPointToRay(new Vector2(Screen.width / 2f, Screen.height / 2f));
        
        if (Physics.Raycast(ray, out RaycastHit hit, useRange))
        {
            // 检查敲击位置是否在容错范围内
            float distance = Vector3.Distance(hit.point, currentCollection.targetPosition);
            
            if (distance <= hitTolerance)
            {
                // 有效敲击
                currentCollection.currentHits++;
                currentCollection.lastHitTime = Time.time;
                
                // 更新进度显示
                UpdateProgress();
                
                // 播放敲击效果
                PlayHammerHit();
                
                Debug.Log($"敲击进度: {currentCollection.currentHits}/{currentCollection.requiredHits}");
                
                // 检查是否完成采集
                if (currentCollection.currentHits >= currentCollection.requiredHits)
                {
                    CompleteCollection();
                }
            }
            else
            {
                ShowMessage($"敲击位置偏差过大！请在目标标记范围内敲击");
            }
        }
    }
    
    /// <summary>
    /// 完成采集
    /// </summary>
    void CompleteCollection()
    {
        if (currentCollection == null) return;
        
        Vector3 collectionPos = currentCollection.targetPosition;
        
        // 播放完成动画
        if (targetMarker != null)
        {
            targetMarker.CompleteCollection();
        }
        
        // 播放完成音效
        if (collectionCompleteSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(collectionCompleteSound);
        }
        
        // 生成薄片样本
        GenerateSlabSample(collectionPos);
        
        // 延迟清理采集状态，让动画播放完成
        StartCoroutine(DelayedCleanup());
        
        // 设置冷却时间
        lastUseTime = Time.time;
        canUse = false;
        
        Debug.Log("采集完成！生成薄片样本");
        ShowMessage("采集成功！获得地质薄片样本");
    }
    
    /// <summary>
    /// 延迟清理协程
    /// </summary>
    System.Collections.IEnumerator DelayedCleanup()
    {
        yield return new WaitForSeconds(1.2f); // 等待完成动画
        CancelCollection();
    }
    
    /// <summary>
    /// 取消当前采集
    /// </summary>
    void CancelCollection()
    {
        if (currentCollection != null)
        {
            Debug.Log("取消采集");
            currentCollection = null;
        }
        
        // 隐藏目标标记
        HideTargetMarker();
    }
    
    /// <summary>
    /// 检查采集超时
    /// </summary>
    void CheckCollectionTimeout()
    {
        if (currentCollection != null)
        {
            if (Time.time - currentCollection.lastHitTime > hitTimeout)
            {
                ShowMessage("采集超时，请重新开始");
                CancelCollection();
            }
        }
    }
    
    /// <summary>
    /// 播放锤击效果
    /// </summary>
    void PlayHammerHit()
    {
        // 播放音效
        if (hammerHitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hammerHitSound);
        }
        
        // 播放锤击动画
        PlayHammerAnimation();
    }
    
    /// <summary>
    /// 播放锤子动画
    /// </summary>
    void PlayHammerAnimation()
    {
        if (equippedHammer != null)
        {
            StartCoroutine(HammerSwingAnimation());
        }
    }
    
    /// <summary>
    /// 锤子挥舞动画协程
    /// </summary>
    IEnumerator HammerSwingAnimation()
    {
        if (equippedHammer == null) yield break;
        
        Transform hammerTransform = equippedHammer.transform;
        Vector3 startPos = hammerTransform.localPosition;
        Vector3 startRot = hammerTransform.localEulerAngles;
        
        // 动画参数
        float animationDuration = 0.6f;
        float upPhase = 0.2f; // 抬起阶段
        float downPhase = 0.3f; // 下挥阶段
        float returnPhase = 0.1f; // 回位阶段
        
        float elapsed = 0f;
        
        // 第一阶段：抬起
        while (elapsed < upPhase)
        {
            float t = elapsed / upPhase;
            Vector3 upPos = startPos + Vector3.up * 0.2f + Vector3.back * 0.1f;
            Vector3 upRot = startRot + Vector3.right * -30f;
            
            hammerTransform.localPosition = Vector3.Lerp(startPos, upPos, t);
            hammerTransform.localEulerAngles = Vector3.Lerp(startRot, upRot, t);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // 第二阶段：下挥
        Vector3 upPosition = hammerTransform.localPosition;
        Vector3 upRotation = hammerTransform.localEulerAngles;
        elapsed = 0f;
        
        while (elapsed < downPhase)
        {
            float t = elapsed / downPhase;
            Vector3 downPos = startPos + Vector3.down * 0.1f + Vector3.forward * 0.1f;
            Vector3 downRot = startRot + Vector3.right * 20f;
            
            hammerTransform.localPosition = Vector3.Lerp(upPosition, downPos, t);
            hammerTransform.localEulerAngles = Vector3.Lerp(upRotation, downRot, t);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // 第三阶段：回位
        Vector3 downPosition = hammerTransform.localPosition;
        Vector3 downRotation = hammerTransform.localEulerAngles;
        elapsed = 0f;
        
        while (elapsed < returnPhase)
        {
            float t = elapsed / returnPhase;
            
            hammerTransform.localPosition = Vector3.Lerp(downPosition, startPos, t);
            hammerTransform.localEulerAngles = Vector3.Lerp(downRotation, startRot, t);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // 确保回到初始位置
        hammerTransform.localPosition = startPos;
        hammerTransform.localEulerAngles = startRot;
    }
    
    /// <summary>
    /// 显示目标标记
    /// </summary>
    void ShowTargetMarker(Vector3 position)
    {
        // 创建目标标记对象
        if (targetMarkerObject == null)
        {
            targetMarkerObject = new GameObject("HammerTargetMarker");
            targetMarker = targetMarkerObject.AddComponent<CollectionTargetMarker>();
        }
        
        // 开始采集标记
        targetMarker.StartCollection(position, requiredHits);
        
        Debug.Log($"显示采集目标标记在: {position}");
    }
    
    /// <summary>
    /// 隐藏目标标记
    /// </summary>
    void HideTargetMarker()
    {
        if (targetMarker != null)
        {
            targetMarker.CancelCollection();
        }
        
        if (targetMarkerObject != null)
        {
            DestroyImmediate(targetMarkerObject);
            targetMarkerObject = null;
            targetMarker = null;
        }
        
        Debug.Log("隐藏采集目标标记");
    }
    
    /// <summary>
    /// 更新采集进度显示
    /// </summary>
    void UpdateProgress()
    {
        if (currentCollection != null && targetMarker != null)
        {
            targetMarker.UpdateProgress(currentCollection.currentHits);
            Debug.Log($"更新进度显示: {currentCollection.currentHits}/{currentCollection.requiredHits}");
        }
    }
    
    /// <summary>
    /// 生成薄片样本 - 使用GeometricSampleReconstructor确保颜色准确
    /// </summary>
    void GenerateSlabSample(Vector3 position)
    {
        Debug.Log($"开始生成薄片样本，采集位置: {position}");
        
        // 第1步：精确地表检测（学习钻塔工具的方法）
        Vector3 preciseCollectionPosition = GetPreciseSurfacePosition(position);
        Debug.Log($"精确地表位置: {preciseCollectionPosition}");
        
        // 第2步：使用GeometricSampleReconstructor获取真实地质层信息（关键修复）
        GeometricSampleReconstructor reconstructor = FindFirstObjectByType<GeometricSampleReconstructor>();
        if (reconstructor == null)
        {
            Debug.LogError("未找到GeometricSampleReconstructor！使用备用方法");
            GenerateSlabSampleFallback(position);
            return;
        }
        
        // 第3步：创建一个很小的钻探样本来获取地质层信息（厚度0.06m模拟薄片）
        GeometricSampleReconstructor.ReconstructedSample geometricSample = reconstructor.ReconstructSample(
            preciseCollectionPosition,        // 精确的地表位置
            Vector3.down,                     // 向下采集
            0.05f,                           // 很小的半径（5cm）
            0.06f,                           // 薄片厚度（6cm）
            preciseCollectionPosition + Vector3.up * 0.5f,  // 显示位置
            0f,                              // 起始深度
            0.06f                            // 结束深度
        );
        
        if (geometricSample != null && geometricSample.layerSegments != null && geometricSample.layerSegments.Length > 0)
        {
            // 直接使用GeometricSampleReconstructor提供的材质
            var firstLayer = geometricSample.layerSegments[0];
            
            // 清理临时样本
            if (geometricSample.sampleContainer != null)
            {
                DestroyImmediate(geometricSample.sampleContainer);
            }
            
            // 直接传递材质给SlabSampleGenerator
            GenerateSlabSampleWithMaterial(preciseCollectionPosition, firstLayer.material, firstLayer.sourceLayer);
        }
        else
        {
            Debug.LogWarning("GeometricSampleReconstructor未能重建样本，使用备用方法");
            GenerateSlabSampleFallback(position);
        }
    }
    
    /// <summary>
    /// 精确地表位置检测 - 学习钻塔工具的成功方法
    /// </summary>
    Vector3 GetPreciseSurfacePosition(Vector3 position)
    {
        Debug.Log($"开始精确地表检测，目标位置: {position}");
        
        // 学习钻塔工具：从高空检测确保找到真正的地表
        Vector3 horizontalPosition = new Vector3(position.x, 0, position.z);
        Vector3 skyPosition = horizontalPosition + Vector3.up * 50f;
        
        RaycastHit[] allHits = Physics.RaycastAll(skyPosition, Vector3.down, 100f);
        Debug.Log($"高空射线检测到 {allHits.Length} 个对象");
        
        Vector3 surfacePosition = position;
        float highestY = float.MinValue;
        bool foundValidSurface = false;
        
        // 寻找最高的地表位置
        foreach (RaycastHit hit in allHits)
        {
            // 优先查找GeologyLayer组件
            GeologyLayer geoLayer = hit.collider.GetComponent<GeologyLayer>();
            if (geoLayer != null && hit.point.y > highestY)
            {
                highestY = hit.point.y;
                surfacePosition = hit.point;
                foundValidSurface = true;
                Debug.Log($"找到GeologyLayer地表: {hit.point.y}m (地层: {geoLayer.layerName})");
            }
            // 如果没有GeologyLayer，检查一般的地表碰撞器
            else if (!foundValidSurface && hit.point.y > highestY)
            {
                // 检查是否是地面对象（通过层级或标签）
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Ground") || 
                    hit.collider.gameObject.CompareTag("Ground") ||
                    hit.collider.gameObject.name.ToLower().Contains("ground") ||
                    hit.collider.gameObject.name.ToLower().Contains("terrain"))
                {
                    highestY = hit.point.y;
                    surfacePosition = hit.point;
                    Debug.Log($"找到一般地表: {hit.point.y}m (对象: {hit.collider.gameObject.name})");
                }
            }
        }
        
        if (foundValidSurface)
        {
            Debug.Log($"精确地表检测成功: {surfacePosition}");
        }
        else
        {
            Debug.LogWarning($"未找到有效地表，使用原始位置: {position}");
            surfacePosition = position;
        }
        
        return surfacePosition;
    }
    
    
    /// <summary>
    /// 使用原始材质生成薄片样本
    /// </summary>
    void GenerateSlabSampleWithMaterial(Vector3 position, Material originalMaterial, GeologyLayer sourceLayer)
    {
        // 创建或查找薄片生成器
        SlabSampleGenerator generator = FindFirstObjectByType<SlabSampleGenerator>();
        if (generator == null)
        {
            GameObject generatorObj = new GameObject("SlabSampleGenerator");
            generator = generatorObj.AddComponent<SlabSampleGenerator>();
        }
        
        // 使用直接材质方法生成样本
        GameObject slabSample = generator.GenerateSlabSampleWithMaterial(position + Vector3.up * 0.5f, originalMaterial, sourceLayer);
        
        if (slabSample != null)
        {
            // 集成到样本收集系统
            IntegrateSlabSample(slabSample);
            Debug.Log($"生成薄片样本: {slabSample.name}");
        }
        else
        {
            Debug.LogError("薄片样本生成失败！");
        }
    }
    
    
    /// <summary>
    /// 备用薄片样本生成方法
    /// </summary>
    void GenerateSlabSampleFallback(Vector3 position)
    {
        Debug.Log("使用备用方法生成薄片样本");
        
        // 创建默认材质
        Material fallbackMaterial = new Material(Shader.Find("Standard"));
        fallbackMaterial.color = new Color(0.7f, 0.5f, 0.3f); // 棕色
        fallbackMaterial.name = "HammerFallbackMaterial";
        
        // 直接使用材质生成样本
        GenerateSlabSampleWithMaterial(position, fallbackMaterial, null);
    }
    
    
    /// <summary>
    /// 将薄片样本集成到样本收集系统
    /// </summary>
    void IntegrateSlabSample(GameObject slabSample)
    {
        // 添加样本收集器组件
        SampleCollector collector = slabSample.GetComponent<SampleCollector>();
        if (collector == null)
        {
            collector = slabSample.AddComponent<SampleCollector>();
        }
        
        // 设置样本数据
        collector.sourceToolID = toolID; // "1002"
        collector.sampleData = SampleItem.CreateFromGeologicalSample(slabSample, toolID);
        
        // 标记为薄片样本
        if (collector.sampleData != null)
        {
            collector.sampleData.description = "使用地质锤采集的薄片样本";
            collector.sampleData.depthStart = 0f; // 表面采集
            collector.sampleData.depthEnd = 0.06f; // 薄片厚度
            collector.sampleData.totalDepth = 0.06f; // 更新总深度为薄片厚度
        }
        
        // 使用样本集成器进行处理
        DrillToolSampleIntegrator.IntegrateSampleAfterDrilling(slabSample, toolID, "薄片采集");
        
        Debug.Log("薄片样本已集成到收集系统");
    }
    
    protected override bool CanUseOnTarget(RaycastHit hit)
    {
        // 检查距离限制
        float distance = Vector3.Distance(transform.position, hit.point);
        if (distance > collectionRange)
        {
            ShowMessage($"目标距离过远！当前距离: {distance:F1}m，最大范围: {collectionRange}m");
            return false;
        }
        
        // 检查是否在有效地层上
        // TODO: 添加更多地层检测逻辑
        
        return true;
    }
    
    protected override void UseTool(RaycastHit hit)
    {
        // 锤子工具的使用逻辑在HandleInput中处理
        // 这个方法保持为空，符合CollectionTool接口
    }
    
    public override void Equip()
    {
        base.Equip();
        
        // 显示锤子在手中
        ShowHammerInHand();
        
        Debug.Log("装备地质锤");
    }
    
    public override void Unequip()
    {
        base.Unequip();
        
        // 隐藏锤子
        HideHammerInHand();
        
        // 取消当前采集
        CancelCollection();
        
        Debug.Log("卸下地质锤");
    }
    
    /// <summary>
    /// 在手中显示锤子
    /// </summary>
    void ShowHammerInHand()
    {
        if (hammerPrefab != null && playerHand != null)
        {
            // 创建锤子实例
            equippedHammer = Instantiate(hammerPrefab);
            equippedHammer.name = "EquippedHammer";
            
            // 设置锤子位置和父级
            equippedHammer.transform.SetParent(playerHand);
            
            // 调整锤子位置和旋转（使用调试得到的最佳数值）
            equippedHammer.transform.localPosition = new Vector3(0.173f, 0.303f, 0.203f);
            equippedHammer.transform.localRotation = Quaternion.Euler(-35.67f, -4.745f, -79.289f);
            equippedHammer.transform.localScale = Vector3.one * 20f; // 放大20倍
            
            // 移除碰撞器，避免干扰
            Collider[] colliders = equippedHammer.GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                col.enabled = false;
            }
            
            Debug.Log("锤子已装备到手中");
        }
        else
        {
            Debug.LogWarning("无法显示锤子：缺少预制体或手部位置");
        }
    }
    
    /// <summary>
    /// 隐藏手中的锤子
    /// </summary>
    void HideHammerInHand()
    {
        if (equippedHammer != null)
        {
            DestroyImmediate(equippedHammer);
            equippedHammer = null;
            Debug.Log("锤子已从手中移除");
        }
    }
    
    /// <summary>
    /// 显示消息给玩家
    /// </summary>
    void ShowMessage(string message)
    {
        Debug.Log($"[地质锤] {message}");
        // TODO: 集成UI消息系统
    }
    
    void OnDestroy()
    {
        // 清理资源
        if (equippedHammer != null)
        {
            DestroyImmediate(equippedHammer);
        }
    }
}

/// <summary>
/// 锤子采集状态数据
/// </summary>
[System.Serializable]
public class HammerCollectionState
{
    public Vector3 targetPosition;     // 敲击目标位置
    public int currentHits = 0;        // 当前敲击次数
    public int requiredHits = 3;       // 需要的敲击次数
    public float lastHitTime;          // 上次敲击时间
}
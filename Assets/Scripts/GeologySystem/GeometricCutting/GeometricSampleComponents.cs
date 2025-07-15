using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 几何样本信息组件
/// 管理和显示真实几何切割样本的详细信息
/// </summary>
public class GeometricSampleInfo : MonoBehaviour
{
    [Header("信息显示")]
    public float displayDistance = 5f;
    public bool autoShowInfo = true;
    public KeyCode detailKey = KeyCode.E;
    
    private GeometricSampleReconstructor.ReconstructedSample sampleData;
    private Camera playerCamera;
    private bool isInfoVisible = false;
    private bool isNearPlayer = false;
    
    public void Initialize(GeometricSampleReconstructor.ReconstructedSample sample)
    {
        sampleData = sample;
        playerCamera = Camera.main;
        
        
    }
    
    /// <summary>
    /// 获取样本数据（用于样本采集系统）
    /// </summary>
    public GeometricSampleReconstructor.ReconstructedSample GetSampleData()
    {
        return sampleData;
    }
    
    void Update()
    {
        if (playerCamera == null || sampleData.sampleContainer == null) return;
        
        // 检查玩家距离
        float distance = Vector3.Distance(transform.position, playerCamera.transform.position);
        bool shouldShowInfo = distance <= displayDistance;
        
        if (shouldShowInfo != isNearPlayer)
        {
            isNearPlayer = shouldShowInfo;
            if (autoShowInfo)
            {
                UpdateInfoDisplay();
            }
        }
        
        // 处理详细信息键
        try
        {
            if (isNearPlayer && Input.GetKeyDown(detailKey))
            {
                ShowDetailedInfo();
            }
        }
        catch (System.InvalidOperationException)
        {
            // Input系统切换错误，暂时跳过输入检查
        }
    }
    
    void UpdateInfoDisplay()
    {
        if (isNearPlayer && autoShowInfo)
        {
            ShowBasicInfo();
        }
        else
        {
            HideInfo();
        }
    }
    
    void ShowBasicInfo()
    {
        if (isInfoVisible) return;
        
        string basicInfo = GenerateBasicInfo();
        
        
        isInfoVisible = true;
    }
    
    void ShowDetailedInfo()
    {
        string detailedInfo = GenerateDetailedInfo();
        
    }
    
    void HideInfo()
    {
        isInfoVisible = false;
    }
    
    string GenerateBasicInfo()
    {
        if (sampleData == null)
        {
            return "样本数据不可用";
        }
        
        string sampleID = sampleData.sampleID ?? "Unknown";
        string shortID = sampleID.Length >= 8 ? sampleID.Substring(0, 8) : sampleID;
        
        string info = "=== 几何地质样本 " + shortID + " ===" + "\n";
        info += "地层段数: " + (sampleData.layerSegments?.Length ?? 0) + "\n";
        info += "总体积: " + sampleData.totalVolume.ToString("F3") + "m³" + "\n";
        info += "总高度: " + sampleData.totalHeight.ToString("F2") + "m" + "\n";
        info += "按 " + detailKey + " 键查看详细信息";
        
        return info;
    }
    
    string GenerateDetailedInfo()
    {
        if (sampleData == null)
        {
            return "样本数据不可用";
        }
        
        string info = "=== 详细几何样本分析 ===" + "\n";
        info += "样本ID: " + (sampleData.sampleID ?? "Unknown") + "\n";
        
        // 检查originalData是否有效（结构体不能与null比较）
        bool hasOriginalData = !string.IsNullOrEmpty(sampleData.originalData.sampleID);
        if (hasOriginalData)
        {
            var data = sampleData.originalData;
            info += "采集位置: " + data.drillingPosition + "\n";
            info += "钻探方向: " + data.drillingDirection + "\n";
            info += "钻探半径: " + data.drillingRadius.ToString("F2") + "m" + "\n";
            info += "钻探深度: " + data.drillingDepth.ToString("F2") + "m" + "\n";
            info += "采集时间: " + data.collectionTime.ToString("yyyy-MM-dd HH:mm:ss") + "\n";
        }
        
        info += "总体积: " + sampleData.totalVolume.ToString("F4") + "m³" + "\n";
        info += "总质量: " + (sampleData.physics?.totalMass.ToString("F2") ?? "0") + "kg" + "\n";
        info += "质心: " + sampleData.centerOfMass + "\n\n";
        
        info += "=== 地层段分析 ===" + "\n";
        
        if (sampleData.layerSegments != null && sampleData.layerSegments.Length > 0)
        {
            for (int i = 0; i < sampleData.layerSegments.Length; i++)
            {
                var segment = sampleData.layerSegments[i];
                if (segment?.sourceLayer != null && segment.cutResult.isValid)
                {
                    var cutResult = segment.cutResult;
                    
                    info += "段 " + (i + 1) + ": " + segment.sourceLayer.layerName + "\n";
                    info += "  地层类型: " + segment.sourceLayer.layerType + "\n";
                    info += "  体积: " + cutResult.volume.ToString("F4") + "m³" + "\n";
                    info += "  表面积: " + cutResult.surfaceArea.ToString("F3") + "m²" + "\n";
                    info += "  深度范围: " + cutResult.depthStart.ToString("F2") + "m - " + cutResult.depthEnd.ToString("F2") + "m" + "\n";
                    info += "\n";
                }
                else
                {
                    info += "段 " + (i + 1) + ": 数据不可用\n\n";
                }
            }
        }
        else
        {
            info += "无地层段数据\n";
        }
        
        return info;
    }
    
    /// <summary>
    /// 获取样本统计信息
    /// </summary>
    public SampleStatistics GetStatistics()
    {
        return new SampleStatistics
        {
            sampleID = sampleData.sampleID,
            totalVolume = sampleData.originalData.totalVolume,
            totalMass = sampleData.physics.totalMass,
            layerCount = sampleData.layerSegments.Length,
            averageDepth = CalculateAverageDepth(),
            dominantLayerType = GetDominantLayerType(),
            complexityScore = CalculateComplexityScore()
        };
    }
    
    float CalculateAverageDepth()
    {
        if (sampleData.layerSegments.Length == 0) return 0f;
        
        float totalDepth = 0f;
        foreach (var segment in sampleData.layerSegments)
        {
            totalDepth += (segment.cutResult.depthStart + segment.cutResult.depthEnd) * 0.5f;
        }
        
        return totalDepth / sampleData.layerSegments.Length;
    }
    
    LayerType GetDominantLayerType()
    {
        var layerVolumes = new Dictionary<LayerType, float>();
        
        foreach (var segment in sampleData.layerSegments)
        {
            LayerType type = segment.sourceLayer.layerType;
            if (!layerVolumes.ContainsKey(type))
            {
                layerVolumes[type] = 0f;
            }
            layerVolumes[type] += segment.cutResult.volume;
        }
        
        return layerVolumes.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key;
    }
    
    float CalculateComplexityScore()
    {
        float score = 0f;
        
        // 基于地层数量
        score += sampleData.layerSegments.Length * 10f;
        
        // 基于几何复杂度
        foreach (var segment in sampleData.layerSegments)
        {
            if (segment.geometry != null)
            {
                score += segment.geometry.vertexCount * 0.01f;
                score += segment.cutResult.features.surfaceRoughness;
                score += segment.cutResult.features.thicknessVariation * 10f;
            }
        }
        
        return score;
    }
}

/// <summary>
/// 几何样本交互组件
/// 处理用户与真实几何样本的交互
/// </summary>
public class GeometricSampleInteraction : MonoBehaviour
{
    [Header("交互设置")]
    public bool canBePickedUp = true;
    public bool showHoverEffects = true;
    public KeyCode inspectKey = KeyCode.E;
    public KeyCode pickupKey = KeyCode.F;
    
    [Header("视觉反馈")]
    public Material hoverMaterial;
    public Color hoverColor = Color.yellow;
    public float hoverBrightness = 1.3f;
    
    private GeometricSampleReconstructor.ReconstructedSample sampleData;
    private bool isPlayerNear = false;
    private bool isBeingInspected = false;
    private Dictionary<Renderer, Material> originalMaterials = new Dictionary<Renderer, Material>();
    private GeometricSampleFloating floatingComponent;
    
    public void Initialize(GeometricSampleReconstructor.ReconstructedSample sample)
    {
        sampleData = sample;
        floatingComponent = GetComponent<GeometricSampleFloating>();
        
        // 记录原始材质
        StoreOriginalMaterials();
        
        
    }
    
    void StoreOriginalMaterials()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            if (renderer.material != null)
            {
                originalMaterials[renderer] = renderer.material;
            }
        }
    }
    
    void Update()
    {
        if (isPlayerNear)
        {
            HandleInteractionInput();
        }
    }
    
    void HandleInteractionInput()
    {
        // 检查按键
        try
        {
            if (Input.GetKeyDown(inspectKey))
            {
                InspectSample();
            }
            
            if (Input.GetKeyDown(pickupKey) && canBePickedUp)
            {
                PickupSample();
            }
        }
        catch (System.InvalidOperationException)
        {
            // Input系统切换错误，暂时跳过输入检查
        }
    }
    
    void InspectSample()
    {
        isBeingInspected = !isBeingInspected;
        
        if (isBeingInspected)
        {
            StartInspection();
        }
        else
        {
            StopInspection();
        }
    }
    
    void StartInspection()
    {
        
        
        // 停止悬浮动画以便仔细观察
        if (floatingComponent != null)
        {
            floatingComponent.enableFloating = false;
            floatingComponent.enableRotation = false;
        }
        
        // 应用检查模式的视觉效果
        ApplyInspectionEffects();
    }
    
    void StopInspection()
    {
        
        
        // 恢复悬浮动画
        if (floatingComponent != null)
        {
            floatingComponent.enableFloating = true;
            floatingComponent.enableRotation = true;
        }
        
        // 移除检查效果
        RemoveInspectionEffects();
    }
    
    void ApplyInspectionEffects()
    {
        // 为每个地层段应用不同的高亮颜色
        for (int i = 0; i < sampleData.layerSegments.Length; i++)
        {
            var segment = sampleData.layerSegments[i];
            Renderer renderer = segment.segmentObject.GetComponent<Renderer>();
            
            if (renderer != null)
            {
                Material inspectionMat = new Material(renderer.material);
                
                // 基于地层类型调整颜色
                Color enhancedColor = GetEnhancedLayerColor(segment.sourceLayer.layerType, i);
                inspectionMat.color = enhancedColor;
                inspectionMat.SetFloat("_Metallic", 0.2f);
                inspectionMat.SetFloat("_Smoothness", 0.8f);
                
                renderer.material = inspectionMat;
            }
        }
    }
    
    Color GetEnhancedLayerColor(LayerType layerType, int index)
    {
        Color baseColor;
        
        switch (layerType)
        {
            case LayerType.Soil:
                baseColor = new Color(0.4f, 0.2f, 0.1f);
                break;
            case LayerType.Sedimentary:
                baseColor = new Color(0.7f, 0.6f, 0.4f);
                break;
            case LayerType.Igneous:
                baseColor = new Color(0.3f, 0.3f, 0.3f);
                break;
            case LayerType.Metamorphic:
                baseColor = new Color(0.5f, 0.4f, 0.6f);
                break;
            case LayerType.Alluvium:
                baseColor = new Color(0.6f, 0.5f, 0.3f);
                break;
            case LayerType.Bedrock:
                baseColor = new Color(0.2f, 0.2f, 0.2f);
                break;
            default:
                baseColor = Color.gray;
                break;
        }
        
        // 为不同段添加轻微变化
        float variation = (index % 3 - 1) * 0.1f;
        baseColor = Color.Lerp(baseColor, Color.white, variation);
        
        return baseColor * hoverBrightness;
    }
    
    void RemoveInspectionEffects()
    {
        // 恢复原始材质
        foreach (var kvp in originalMaterials)
        {
            if (kvp.Key != null)
            {
                kvp.Key.material = kvp.Value;
            }
        }
    }
    
    void PickupSample()
    {
        if (!canBePickedUp) return;
        
        
        
        // 添加到玩家库存
        PlayerInventory inventory = FindFirstObjectByType<PlayerInventory>();
        if (inventory != null)
        {
            // 这里需要将几何样本数据转换为库存系统可以理解的格式
            // inventory.AddGeometricSample(sampleData);
        }
        
        // 播放收集动画
        if (floatingComponent != null)
        {
            floatingComponent.PlayCollectionAnimation();
        }
        else
        {
            // 如果没有悬浮组件，直接销毁
            StartCoroutine(SimpleCollectionAnimation());
        }
    }
    
    System.Collections.IEnumerator SimpleCollectionAnimation()
    {
        Vector3 startPos = transform.position;
        Vector3 startScale = transform.localScale;
        
        float duration = 1f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // 缩小并上升
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            transform.position = startPos + Vector3.up * t * 2f;
            
            yield return null;
        }
        
        Destroy(gameObject);
    }
    
    // 碰撞器事件
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;
            OnPlayerEnter();
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
            OnPlayerExit();
        }
    }
    
    void OnPlayerEnter()
    {
        if (showHoverEffects)
        {
            ApplyHoverEffects();
        }
        
        // 播放强调动画
        if (floatingComponent != null)
        {
            floatingComponent.PlayHighlightAnimation(1f);
        }
        
        // 显示交互提示
        ShowInteractionHints();
    }
    
    void OnPlayerExit()
    {
        if (showHoverEffects)
        {
            RemoveHoverEffects();
        }
        
        // 如果正在检查，停止检查
        if (isBeingInspected)
        {
            StopInspection();
        }
        
        HideInteractionHints();
    }
    
    void ApplyHoverEffects()
    {
        // 轻微的悬停效果
        foreach (var kvp in originalMaterials)
        {
            if (kvp.Key != null)
            {
                Material hoverMat = new Material(kvp.Value);
                hoverMat.color = kvp.Value.color * 1.2f;
                kvp.Key.material = hoverMat;
            }
        }
    }
    
    void RemoveHoverEffects()
    {
        if (!isBeingInspected)
        {
            // 只有在不是检查模式时才恢复原始材质
            foreach (var kvp in originalMaterials)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.material = kvp.Value;
                }
            }
        }
    }
    
    void ShowInteractionHints()
    {
        string hint = "按 " + inspectKey + " 键检查样本";
        if (canBePickedUp)
        {
            hint += "\n按 " + pickupKey + " 键拾取样本";
        }
        
        
    }
    
    void HideInteractionHints()
    {
        // 隐藏交互提示
    }
    
    /// <summary>
    /// 获取样本是否可以交互
    /// </summary>
    public bool CanInteract()
    {
        return isPlayerNear;
    }
    
    /// <summary>
    /// 强制停止所有交互
    /// </summary>
    public void ForceStopInteraction()
    {
        isBeingInspected = false;
        isPlayerNear = false;
        RemoveInspectionEffects();
        RemoveHoverEffects();
    }
}

/// <summary>
/// 样本统计信息数据结构
/// </summary>
[System.Serializable]
public struct SampleStatistics
{
    public string sampleID;
    public float totalVolume;
    public float totalMass;
    public int layerCount;
    public float averageDepth;
    public LayerType dominantLayerType;
    public float complexityScore;
}
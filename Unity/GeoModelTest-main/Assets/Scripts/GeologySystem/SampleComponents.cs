using UnityEngine;
using System.Collections.Generic;

// 重建后的地质样本主组件
public class ReconstructedGeologicalSample : MonoBehaviour
{
    [Header("样本数据")]
    public GeologicalSampleData sampleData;
    
    [Header("交互设置")]
    public bool canBePickedUp = true;
    public bool showInfoOnHover = true;
    public KeyCode inspectKey = KeyCode.E;
    
    private bool isBeingInspected = false;
    private SampleInspectionUI inspectionUI;
    
    void Start()
    {
        // 确保有正确的标签
        gameObject.tag = "GeologicalSample";
        
        // 查找或创建检查UI
        inspectionUI = FindFirstObjectByType<SampleInspectionUI>();
    }
    
    void Update()
    {
        if (isBeingInspected && Input.GetKeyDown(inspectKey))
        {
            if (inspectionUI != null)
            {
                inspectionUI.ShowSampleDetails(sampleData);
            }
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isBeingInspected = true;
            if (showInfoOnHover)
            {
                ShowQuickInfo();
            }
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isBeingInspected = false;
            HideQuickInfo();
        }
    }
    
    void ShowQuickInfo()
    {
        // 显示快速信息
        string info = $"地质样本\\n深度: {sampleData.drillingDepth:F1}m\\n地层数: {sampleData.layerStats.Length}\\n按 {inspectKey} 键详细查看";
        
        // 这里可以显示UI提示
        
    }
    
    void HideQuickInfo()
    {
        // 隐藏快速信息
    }
    
    public void PickUp()
    {
        if (!canBePickedUp) return;
        
        // 停止悬浮效果
        SampleFloatingDisplay floatingDisplay = GetComponent<SampleFloatingDisplay>();
        if (floatingDisplay != null)
        {
            floatingDisplay.StopFloating();
        }
        
        // 添加到玩家库存
        PlayerInventory inventory = FindFirstObjectByType<PlayerInventory>();
        if (inventory != null)
        {
            inventory.AddSample(sampleData);
            
            // 添加收集动画效果
            StartCoroutine(CollectionAnimation());
        }
    }
    
    System.Collections.IEnumerator CollectionAnimation()
    {
        // 简单的收集动画：缩小并上升
        Vector3 startScale = transform.localScale;
        Vector3 startPos = transform.position;
        
        float animTime = 1.0f;
        float elapsed = 0f;
        
        while (elapsed < animTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animTime;
            
            // 缩小
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            
            // 上升
            transform.position = startPos + Vector3.up * t * 2f;
            
            yield return null;
        }
        
        Destroy(gameObject);
    }
}

// 地层段信息组件
public class LayerSegmentInfo : MonoBehaviour
{
    [Header("段信息")]
    public LayerInfo layerInfo;
    public LayerSampleSegment segment;
    
    [Header("可视化")]
    public bool highlightOnHover = true;
    public Color highlightColor = Color.yellow;
    
    private Renderer segmentRenderer;
    private Color originalColor;
    private bool isHighlighted = false;
    
    void Start()
    {
        segmentRenderer = GetComponent<Renderer>();
        if (segmentRenderer != null)
        {
            originalColor = segmentRenderer.material.color;
        }
    }
    
    void OnMouseEnter()
    {
        if (highlightOnHover && !isHighlighted)
        {
            Highlight();
            ShowLayerInfo();
        }
    }
    
    void OnMouseExit()
    {
        if (isHighlighted)
        {
            RemoveHighlight();
            HideLayerInfo();
        }
    }
    
    void Highlight()
    {
        if (segmentRenderer != null)
        {
            Material highlightMaterial = new Material(segmentRenderer.material);
            highlightMaterial.color = highlightColor;
            segmentRenderer.material = highlightMaterial;
            isHighlighted = true;
        }
    }
    
    void RemoveHighlight()
    {
        if (segmentRenderer != null && layerInfo.material != null)
        {
            Material originalMaterial = new Material(layerInfo.material);
            originalMaterial.color = layerInfo.color;
            segmentRenderer.material = originalMaterial;
            isHighlighted = false;
        }
    }
    
    void ShowLayerInfo()
    {
        string info = $"地层: {layerInfo.layer.layerName}\\n" +
                     $"类型: {layerInfo.layer.layerType}\\n" +
                     $"厚度: {layerInfo.thickness:F2}m\\n" +
                     $"占比: {layerInfo.areaPercentage * 100:F1}%\\n" +
                     $"倾角: {layerInfo.dipAngle:F1}°";
        
        
        
        // 这里可以显示UI信息面板
    }
    
    void HideLayerInfo()
    {
        // 隐藏信息面板
    }
}

// 接触界面信息组件
public class ContactInfo : MonoBehaviour
{
    [Header("接触信息")]
    public ContactInterface contactInterface;
    
    [Header("可视化")]
    public bool showContactDetails = true;
    
    void OnMouseEnter()
    {
        if (showContactDetails)
        {
            ShowContactInfo();
        }
    }
    
    void OnMouseExit()
    {
        HideContactInfo();
    }
    
    void ShowContactInfo()
    {
        string info = $"地层接触\\n" +
                     $"层A: {contactInterface.layerA.layerName}\\n" +
                     $"层B: {contactInterface.layerB.layerName}\\n" +
                     $"接触角: {contactInterface.contactAngle:F1}°\\n" +
                     $"接触类型: {GetContactTypeDescription(contactInterface.contactType)}";
        
        
    }
    
    void HideContactInfo()
    {
        // 隐藏接触信息
    }
    
    string GetContactTypeDescription(ContactType type)
    {
        switch (type)
        {
            case ContactType.Conformable: return "整合接触";
            case ContactType.Unconformable: return "不整合接触";
            case ContactType.Disconformable: return "假整合接触";
            case ContactType.Intrusive: return "侵入接触";
            case ContactType.Fault: return "断层接触";
            case ContactType.Gradational: return "渐变接触";
            default: return "未知接触";
        }
    }
}

// 样本信息显示组件
public class SampleInfoDisplay : MonoBehaviour
{
    [Header("信息显示")]
    public GameObject infoPanel;
    public float displayDistance = 5f;
    
    private GeologicalSampleData sampleData;
    private Camera playerCamera;
    private bool isInfoVisible = false;
    
    public void Initialize(GeologicalSampleData data)
    {
        sampleData = data;
        playerCamera = Camera.main;
        
        if (infoPanel != null)
        {
            infoPanel.SetActive(false);
        }
    }
    
    void Update()
    {
        if (playerCamera == null || sampleData == null) return;
        
        float distance = Vector3.Distance(transform.position, playerCamera.transform.position);
        bool shouldShowInfo = distance <= displayDistance;
        
        if (shouldShowInfo != isInfoVisible)
        {
            isInfoVisible = shouldShowInfo;
            UpdateInfoDisplay();
        }
    }
    
    void UpdateInfoDisplay()
    {
        if (infoPanel != null)
        {
            infoPanel.SetActive(isInfoVisible);
        }
        
        if (isInfoVisible)
        {
            // 更新信息面板内容
            UpdateInfoContent();
        }
    }
    
    void UpdateInfoContent()
    {
        // 这里可以更新UI文本内容
        // 暂时用Debug.Log代替
        if (isInfoVisible)
        {
            string info = GenerateSampleSummary();
            
        }
    }
    
    string GenerateSampleSummary()
    {
        string summary = $"=== 地质样本 {sampleData.sampleID.Substring(0, 8)} ===\\n";
        summary += $"采集位置: {sampleData.drillingPosition}\\n";
        summary += $"钻探深度: {sampleData.drillingDepth:F2}m\\n";
        summary += $"钻探半径: {sampleData.drillingRadius:F2}m\\n";
        summary += $"采集时间: {sampleData.collectionTime:yyyy-MM-dd HH:mm}\\n";
        summary += $"样本段数: {sampleData.segments.Length}\\n\\n";
        
        summary += "=== 地层统计 ===\\n";
        foreach (var stat in sampleData.layerStats)
        {
            summary += $"• {stat.layerName} ({stat.layerType})\\n";
            summary += $"  总厚度: {stat.totalThickness:F2}m\\n";
            summary += $"  平均倾角: {stat.averageDipAngle:F1}°\\n";
            summary += $"  段数: {stat.numberOfSegments}\\n\\n";
        }
        
        return summary;
    }
}

// 网格合并工具
public class MeshCombiner : MonoBehaviour
{
    public Mesh CombineMeshes(MeshFilter[] meshFilters)
    {
        CombineInstance[] combineInstances = new CombineInstance[meshFilters.Length];
        
        for (int i = 0; i < meshFilters.Length; i++)
        {
            combineInstances[i].mesh = meshFilters[i].sharedMesh;
            combineInstances[i].transform = meshFilters[i].transform.localToWorldMatrix;
        }
        
        Mesh combinedMesh = new Mesh();
        combinedMesh.CombineMeshes(combineInstances);
        combinedMesh.RecalculateNormals();
        combinedMesh.RecalculateBounds();
        
        return combinedMesh;
    }
}
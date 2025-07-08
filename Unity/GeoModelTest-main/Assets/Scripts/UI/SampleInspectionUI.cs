using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SampleInspectionUI : MonoBehaviour
{
    [Header("UI组件")]
    public GameObject inspectionPanel;
    public Text sampleIDText;
    public Text basicInfoText;
    public Text layerStatsText;
    public Transform layerDetailContainer;
    public GameObject layerDetailPrefab;
    
    [Header("3D样本显示")]
    public Camera sampleViewCamera;
    public Transform sampleDisplayArea;
    public Light sampleLight;
    
    [Header("控制设置")]
    public KeyCode closeKey = KeyCode.Escape;
    public float cameraRotationSpeed = 50f;
    public float cameraZoomSpeed = 2f;
    
    private GeologicalSampleData currentSample;
    private GameObject displayedSampleObj;
    private bool isInspecting = false;
    private Vector3 lastMousePosition;
    
    void Start()
    {
        InitializeUI();
    }
    
    void InitializeUI()
    {
        if (inspectionPanel != null)
        {
            inspectionPanel.SetActive(false);
        }
        
        if (sampleViewCamera != null)
        {
            sampleViewCamera.gameObject.SetActive(false);
        }
        
        // 确保样本显示区域隐藏
        if (sampleDisplayArea != null)
        {
            sampleDisplayArea.gameObject.SetActive(false);
        }
    }
    
    void Update()
    {
        if (isInspecting)
        {
            HandleInspectionInput();
            
            if (Input.GetKeyDown(closeKey))
            {
                CloseSampleInspection();
            }
        }
    }
    
    public void ShowSampleDetails(GeologicalSampleData sampleData)
    {
        if (sampleData == null)
        {
            Debug.LogWarning("样本数据为空，无法显示详情");
            return;
        }
        
        currentSample = sampleData;
        isInspecting = true;
        
        // 激活UI面板
        if (inspectionPanel != null)
        {
            inspectionPanel.SetActive(true);
        }
        
        // 激活3D显示
        if (sampleViewCamera != null)
        {
            sampleViewCamera.gameObject.SetActive(true);
        }
        
        if (sampleDisplayArea != null)
        {
            sampleDisplayArea.gameObject.SetActive(true);
        }
        
        // 更新UI内容
        UpdateSampleDisplay();
        
        // 创建3D样本显示
        Create3DSampleDisplay();
        
        // 暂停游戏时间或禁用玩家控制
        Time.timeScale = 0.1f;
        
        Debug.Log($"打开样本检查界面: {sampleData.sampleID}");
    }
    
    public void CloseSampleInspection()
    {
        isInspecting = false;
        
        if (inspectionPanel != null)
        {
            inspectionPanel.SetActive(false);
        }
        
        if (sampleViewCamera != null)
        {
            sampleViewCamera.gameObject.SetActive(false);
        }
        
        if (sampleDisplayArea != null)
        {
            sampleDisplayArea.gameObject.SetActive(false);
        }
        
        // 清理3D显示对象
        if (displayedSampleObj != null)
        {
            DestroyImmediate(displayedSampleObj);
            displayedSampleObj = null;
        }
        
        // 恢复游戏时间
        Time.timeScale = 1f;
        
        Debug.Log("关闭样本检查界面");
    }
    
    void UpdateSampleDisplay()
    {
        if (currentSample == null) return;
        
        // 更新基本信息
        if (sampleIDText != null)
        {
            sampleIDText.text = $"样本ID: {currentSample.sampleID.Substring(0, 8)}...";
        }
        
        if (basicInfoText != null)
        {
            string basicInfo = $"采集位置: {currentSample.drillingPosition}\\n" +
                              $"钻探深度: {currentSample.drillingDepth:F2}m\\n" +
                              $"钻探半径: {currentSample.drillingRadius:F2}m\\n" +
                              $"采集时间: {currentSample.collectionTime:yyyy-MM-dd HH:mm}\\n" +
                              $"样本段数: {currentSample.segments.Length}\\n" +
                              $"地层种类: {currentSample.layerStats.Length}";
            
            basicInfoText.text = basicInfo;
        }
        
        // 更新地层统计
        UpdateLayerStatistics();
        
        // 更新详细地层信息
        UpdateLayerDetails();
    }
    
    void UpdateLayerStatistics()
    {
        if (layerStatsText == null || currentSample.layerStats == null) return;
        
        string statsText = "=== 地层统计 ===\\n\\n";
        
        foreach (var stat in currentSample.layerStats)
        {
            statsText += $"• {stat.layerName}\\n";
            statsText += $"  类型: {GetLayerTypeDescription(stat.layerType)}\\n";
            statsText += $"  总厚度: {stat.totalThickness:F2}m\\n";
            statsText += $"  段数: {stat.numberOfSegments}\\n";
            statsText += $"  平均倾角: {stat.averageDipAngle:F1}°\\n\\n";
        }
        
        layerStatsText.text = statsText;
    }
    
    void UpdateLayerDetails()
    {
        if (layerDetailContainer == null || currentSample.segments == null) return;
        
        // 清理现有的详细信息UI
        foreach (Transform child in layerDetailContainer)
        {
            DestroyImmediate(child.gameObject);
        }
        
        // 为每个段创建详细信息
        for (int i = 0; i < currentSample.segments.Length; i++)
        {
            LayerSampleSegment segment = currentSample.segments[i];
            CreateSegmentDetailUI(segment, i);
        }
    }
    
    void CreateSegmentDetailUI(LayerSampleSegment segment, int index)
    {
        GameObject detailObj = new GameObject($"SegmentDetail_{index}");
        detailObj.transform.SetParent(layerDetailContainer, false);
        
        // 添加文本组件
        Text detailText = detailObj.AddComponent<Text>();
        detailText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        detailText.fontSize = 12;
        detailText.color = Color.white;
        
        string detailInfo = $"=== 段 {index + 1} (深度: {segment.depth:F2}m) ===\\n";
        
        foreach (var layerInfo in segment.layersInSection)
        {
            detailInfo += $"地层: {layerInfo.layer.layerName}\\n";
            detailInfo += $"占比: {layerInfo.areaPercentage * 100:F1}%\\n";
            detailInfo += $"厚度: {layerInfo.thickness:F2}m\\n";
            detailInfo += $"倾角: {layerInfo.dipAngle:F1}°\\n";
            detailInfo += "---\\n";
        }
        
        if (segment.interfaces.Length > 0)
        {
            detailInfo += "接触关系:\\n";
            foreach (var contact in segment.interfaces)
            {
                detailInfo += $"{contact.layerA.layerName} - {contact.layerB.layerName}\\n";
                detailInfo += $"接触角: {contact.contactAngle:F1}°\\n";
                detailInfo += $"类型: {GetContactTypeDescription(contact.contactType)}\\n";
            }
        }
        
        detailText.text = detailInfo;
        
        // 设置RectTransform
        RectTransform rectTransform = detailObj.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.offsetMin = new Vector2(0, -150 * (index + 1));
        rectTransform.offsetMax = new Vector2(0, -150 * index);
    }
    
    void Create3DSampleDisplay()
    {
        if (sampleDisplayArea == null || currentSample == null) return;
        
        // 清理现有显示对象
        if (displayedSampleObj != null)
        {
            DestroyImmediate(displayedSampleObj);
        }
        
        // 使用重建系统创建3D样本
        SampleReconstructionSystem reconstructionSystem = FindFirstObjectByType<SampleReconstructionSystem>();
        if (reconstructionSystem != null)
        {
            displayedSampleObj = reconstructionSystem.ReconstructSample(
                currentSample, 
                sampleDisplayArea.position
            );
            
            if (displayedSampleObj != null)
            {
                displayedSampleObj.transform.SetParent(sampleDisplayArea);
                
                // 移除物理组件以避免干扰
                Rigidbody rb = displayedSampleObj.GetComponent<Rigidbody>();
                if (rb != null) DestroyImmediate(rb);
                
                Collider[] colliders = displayedSampleObj.GetComponents<Collider>();
                foreach (var col in colliders)
                {
                    if (!col.isTrigger) DestroyImmediate(col);
                }
                
                // 调整样本大小以适合显示区域
                displayedSampleObj.transform.localScale = Vector3.one * 0.5f;
                
                Debug.Log("3D样本显示创建完成");
            }
        }
    }
    
    void HandleInspectionInput()
    {
        if (sampleViewCamera == null || displayedSampleObj == null) return;
        
        // 鼠标拖拽旋转样本
        if (Input.GetMouseButtonDown(0))
        {
            lastMousePosition = Input.mousePosition;
        }
        
        if (Input.GetMouseButton(0))
        {
            Vector3 deltaMousePosition = Input.mousePosition - lastMousePosition;
            
            float rotationX = deltaMousePosition.y * cameraRotationSpeed * Time.unscaledDeltaTime;
            float rotationY = -deltaMousePosition.x * cameraRotationSpeed * Time.unscaledDeltaTime;
            
            sampleViewCamera.transform.RotateAround(displayedSampleObj.transform.position, Vector3.up, rotationY);
            sampleViewCamera.transform.RotateAround(displayedSampleObj.transform.position, sampleViewCamera.transform.right, rotationX);
            
            lastMousePosition = Input.mousePosition;
        }
        
        // 滚轮缩放
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            Vector3 direction = (sampleViewCamera.transform.position - displayedSampleObj.transform.position).normalized;
            sampleViewCamera.transform.position += direction * scroll * cameraZoomSpeed;
        }
    }
    
    string GetLayerTypeDescription(LayerType layerType)
    {
        switch (layerType)
        {
            case LayerType.Sedimentary: return "沉积岩";
            case LayerType.Igneous: return "火成岩";
            case LayerType.Metamorphic: return "变质岩";
            case LayerType.Soil: return "土壤层";
            case LayerType.Alluvium: return "冲积层";
            case LayerType.Bedrock: return "基岩";
            default: return "未知类型";
        }
    }
    
    string GetContactTypeDescription(ContactType contactType)
    {
        switch (contactType)
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
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 简易钻探工具 - 点击地面进行单次钻探并生成样本
/// </summary>
public class SimpleDrillTool : CollectionTool
{
    [Header("钻探参数")]
    public float drillingDepth = 2.0f;
    public float drillingRadius = 0.1f;
    
    [Header("预览设置")]
    public Material previewMaterial;
    private GameObject previewObject;
    private bool isPreviewMode = false;
    
    protected override void Start()
    {
        base.Start();
        toolID = "1000";
        toolName = "简易钻探";
    }
    
    protected override void Update()
    {
        base.Update();
        if (isPreviewMode && isEquipped)
        {
            UpdatePreview();
        }
    }
    
    public override void Equip()
    {
        base.Equip();
        isPreviewMode = true;
        CreatePreviewObject();
    }
    
    public override void Unequip()
    {
        base.Unequip();
        isPreviewMode = false;
        DestroyPreviewObject();
    }
    
    protected override void UseTool(RaycastHit hit)
    {
        PerformDrilling(hit);
    }
    
    void CreatePreviewObject()
    {
        if (previewObject != null)
        {
            DestroyImmediate(previewObject);
        }
        
        // 创建钻探预览圆柱体
        previewObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        previewObject.name = "SimpleDrill_Preview";
        
        // 设置预览对象属性
        previewObject.transform.localScale = new Vector3(drillingRadius * 2, drillingDepth / 2, drillingRadius * 2);
        
        // 移除碰撞器
        Collider previewCollider = previewObject.GetComponent<Collider>();
        if (previewCollider != null)
        {
            DestroyImmediate(previewCollider);
        }
        
        // 设置半透明材质
        Renderer renderer = previewObject.GetComponent<Renderer>();
        if (renderer != null)
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
                previewMaterial.color = new Color(0, 1, 0, 0.3f); // 绿色半透明
            }
            renderer.material = previewMaterial;
        }
    }
    
    void UpdatePreview()
    {
        if (previewObject == null) return;
        
        Camera playerCamera = Camera.main;
        if (playerCamera == null) return;
        
        // 从摄像机发射射线
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, 100f))
        {
            // 更新预览位置
            Vector3 previewPos = hit.point;
            previewPos.y += drillingDepth / 2; // 让圆柱体底部贴地
            previewObject.transform.position = previewPos;
            
            // 更新材质颜色表示是否可以钻探
            Renderer renderer = previewObject.GetComponent<Renderer>();
            if (renderer != null && previewMaterial != null)
            {
                // 简单检查：如果命中地面则显示绿色，否则红色
                if (hit.collider.gameObject.layer == 0) // 默认layer
                {
                    previewMaterial.color = new Color(0, 1, 0, 0.3f); // 绿色 - 可以钻探
                }
                else
                {
                    previewMaterial.color = new Color(1, 0, 0, 0.3f); // 红色 - 不能钻探
                }
            }
            
            previewObject.SetActive(true);
        }
        else
        {
            previewObject.SetActive(false);
        }
    }
    
    protected override void HandleInput()
    {
        // 调用基类的输入处理（处理左键钻探）
        base.HandleInput();
        
        // 右键或ESC键退出预览模式
        if (Mouse.current.rightButton.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            var toolManager = FindFirstObjectByType<ToolManager>();
            if (toolManager != null)
            {
                toolManager.UnequipCurrentTool();
            }
        }
    }
    
    void PerformDrilling(RaycastHit hit)
    {
        Vector3 drillingPosition = hit.point;
        
        // 检查是否可以在此位置钻探
        if (hit.collider.gameObject.layer == 0) // 默认layer
        {
            // 执行钻探并生成样本
            CreateDrillingSample(drillingPosition);
            
            Debug.Log($"简易钻探完成，位置: {drillingPosition}");
            
            // 钻探完成后自动退出工具
            var toolManager = FindFirstObjectByType<ToolManager>();
            if (toolManager != null)
            {
                toolManager.UnequipCurrentTool();
            }
        }
        else
        {
            Debug.Log("无法在此位置钻探");
        }
    }
    
    void CreateDrillingSample(Vector3 drillingPosition)
    {
        Debug.Log($"开始创建钻探样本，位置: {drillingPosition}");
        
        // 查找或创建GeometricSampleReconstructor来生成样本
        GeometricSampleReconstructor reconstructor = FindFirstObjectByType<GeometricSampleReconstructor>();
        if (reconstructor == null)
        {
            // 如果没有找到，创建一个新的
            GameObject reconstructorObj = new GameObject("GeometricSampleReconstructor");
            reconstructor = reconstructorObj.AddComponent<GeometricSampleReconstructor>();
            Debug.Log("创建了新的GeometricSampleReconstructor");
        }
        
        if (reconstructor != null)
        {
            Debug.Log("找到GeometricSampleReconstructor，开始生成样本");
            
            // 参考钻塔的采集逻辑，使用6参数版本生成真实地质样本
            Vector3 drillingDirection = Vector3.down; // 向下钻探
            Vector3 displayPosition = drillingPosition + Vector3.up * (drillingDepth * 1.1f); // 样本显示在地面以上
            float depthStart = 0f; // 从地表开始
            float depthEnd = drillingDepth; // 到钻探深度结束
            
            Debug.Log($"调用参数: pos={drillingPosition}, dir={drillingDirection}, radius={drillingRadius}, depth={drillingDepth}, display={displayPosition}, start={depthStart}, end={depthEnd}");
            
            GeometricSampleReconstructor.ReconstructedSample geometricSample = reconstructor.ReconstructSample(
                drillingPosition,
                drillingDirection,
                drillingRadius,
                drillingDepth,
                displayPosition,
                depthStart,
                depthEnd
            );
            
            if (geometricSample != null && geometricSample.sampleContainer != null)
            {
                Debug.Log($"成功生成地质样本: {geometricSample.sampleID}");
                
                // 立即为生成的样本添加收集组件（按需集成）
                DrillToolSampleIntegrator.IntegrateSampleAfterDrilling(
                    geometricSample.sampleContainer, 
                    "1000", // SimpleDrillTool的ID
                    "简易钻探"
                );
                
                return;
            }
            else
            {
                Debug.LogWarning("地质样本生成失败，样本为null或容器为null");
            }
        }
        else
        {
            Debug.LogWarning("未找到GeometricSampleReconstructor，无法生成钻探样本");
            
            // 如果没有重建器，创建一个简单的指示物
            GameObject simpleSample = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            simpleSample.name = "SimpleDrill_Sample";
            simpleSample.transform.position = drillingPosition + Vector3.up * 0.5f;
            simpleSample.transform.localScale = new Vector3(drillingRadius * 2, 0.1f, drillingRadius * 2);
            
            // 设置颜色
            Renderer renderer = simpleSample.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material sampleMaterial = new Material(Shader.Find("Standard"));
                sampleMaterial.color = new Color(0.6f, 0.3f, 0.1f); // 棕色 (替代Color.brown)
                renderer.material = sampleMaterial;
            }
        }
    }
    
    void DestroyPreviewObject()
    {
        if (previewObject != null)
        {
            DestroyImmediate(previewObject);
            previewObject = null;
        }
    }
    
    void OnDestroy()
    {
        DestroyPreviewObject();
    }
}
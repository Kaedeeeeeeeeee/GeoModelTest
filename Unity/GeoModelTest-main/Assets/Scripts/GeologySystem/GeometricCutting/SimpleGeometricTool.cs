using UnityEngine;
using System.Collections;

/// <summary>
/// 简化的几何钻探工具
/// 作为几何切割系统的简化实现，用于测试基本功能
/// </summary>
public class SimpleGeometricTool : MonoBehaviour
{
    [Header("钻探参数")]
    public float drillingRadius = 0.5f;
    public float drillingDepth = 2.0f;
    
    [Header("材质设置")]
    public Material sampleMaterial;
    public Material[] layerMaterials;
    
    [Header("调试")]
    public bool enableDebugLogs = true;
    
    private TerrainHoleSystem terrainHoleSystem;
    
    void Start()
    {
        InitializeTool();
    }
    
    void InitializeTool()
    {
        terrainHoleSystem = FindFirstObjectByType<TerrainHoleSystem>();
        
        if (enableDebugLogs)
        {
            Debug.Log("简化几何工具初始化完成");
        }
    }
    
    /// <summary>
    /// 执行几何钻探
    /// </summary>
    public void PerformGeometricDrilling(Vector3 position, Vector3 direction)
    {
        if (enableDebugLogs)
        {
            Debug.Log("开始几何钻探 - 位置: " + position);
        }
        
        StartCoroutine(GeometricDrillingCoroutine(position, direction));
    }
    
    IEnumerator GeometricDrillingCoroutine(Vector3 position, Vector3 direction)
    {
        // 第1步：创建地形洞
        if (terrainHoleSystem != null)
        {
            terrainHoleSystem.CreateCylindricalHole(position, drillingRadius, drillingDepth, direction);
            Debug.Log("创建了地形洞");
        }
        
        yield return new WaitForSeconds(1f);
        
        // 第2步：创建简化的几何样本
        GameObject sample = CreateSimplifiedGeometricSample(position, direction);
        
        if (sample != null)
        {
            // 第3步：添加悬浮效果
            AddFloatingEffect(sample);
            
            Debug.Log("简化几何样本创建完成");
        }
        
        yield return null;
    }
    
    /// <summary>
    /// 创建简化的几何样本
    /// </summary>
    GameObject CreateSimplifiedGeometricSample(Vector3 position, Vector3 direction)
    {
        // 创建样本容器
        GameObject sampleContainer = new GameObject("SimpleGeometricSample");
        sampleContainer.transform.position = position + Vector3.up * 1.5f;
        
        // 模拟多个地层段
        int segmentCount = Random.Range(2, 4);
        float segmentHeight = drillingDepth / segmentCount;
        
        for (int i = 0; i < segmentCount; i++)
        {
            GameObject segment = CreateSampleSegment(i, segmentHeight, sampleContainer.transform);
            if (segment != null)
            {
                // 设置段的位置
                float yOffset = -i * segmentHeight;
                segment.transform.localPosition = new Vector3(0, yOffset, 0);
            }
        }
        
        // 添加物理组件
        AddPhysicsComponents(sampleContainer);
        
        return sampleContainer;
    }
    
    /// <summary>
    /// 创建样本段
    /// </summary>
    GameObject CreateSampleSegment(int index, float height, Transform parent)
    {
        GameObject segment = new GameObject("Segment_" + index);
        segment.transform.SetParent(parent);
        
        // 创建圆柱体几何
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.transform.SetParent(segment.transform);
        cylinder.transform.localPosition = Vector3.zero;
        
        // 设置尺寸
        float radius = drillingRadius * 0.8f;
        cylinder.transform.localScale = new Vector3(radius, height * 0.5f, radius);
        
        // 设置材质
        MeshRenderer renderer = cylinder.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Material segmentMaterial = GetSegmentMaterial(index);
            renderer.material = segmentMaterial;
        }
        
        return segment;
    }
    
    /// <summary>
    /// 获取段材质
    /// </summary>
    Material GetSegmentMaterial(int index)
    {
        if (layerMaterials != null && layerMaterials.Length > 0)
        {
            return layerMaterials[index % layerMaterials.Length];
        }
        
        if (sampleMaterial != null)
        {
            return sampleMaterial;
        }
        
        // 创建默认材质
        Material defaultMat = new Material(Shader.Find("Standard"));
        
        // 根据索引设置不同颜色
        Color[] colors = { new Color(0.6f, 0.3f, 0.1f), Color.gray, Color.yellow, Color.red };
        defaultMat.color = colors[index % colors.Length];
        
        return defaultMat;
    }
    
    /// <summary>
    /// 添加物理组件
    /// </summary>
    void AddPhysicsComponents(GameObject sample)
    {
        // 添加刚体
        Rigidbody rb = sample.AddComponent<Rigidbody>();
        rb.mass = 2f;
        rb.useGravity = false;
        rb.isKinematic = true;
        
        // 添加碰撞器
        CapsuleCollider collider = sample.AddComponent<CapsuleCollider>();
        collider.radius = drillingRadius;
        collider.height = drillingDepth;
        collider.center = Vector3.down * drillingDepth * 0.5f;
        collider.isTrigger = true;
    }
    
    /// <summary>
    /// 添加悬浮效果
    /// </summary>
    void AddFloatingEffect(GameObject sample)
    {
        SimpleFloatingEffect floating = sample.AddComponent<SimpleFloatingEffect>();
        floating.floatingHeight = 0.2f;
        floating.floatingSpeed = 1f;
        floating.rotationSpeed = 15f;
    }
    
    /// <summary>
    /// 设置钻探参数
    /// </summary>
    public void SetDrillingParameters(float radius, float depth)
    {
        drillingRadius = radius;
        drillingDepth = depth;
        
        if (enableDebugLogs)
        {
            Debug.Log("钻探参数已更新 - 半径: " + radius + ", 深度: " + depth);
        }
    }
}

/// <summary>
/// 简单的悬浮效果组件
/// </summary>
public class SimpleFloatingEffect : MonoBehaviour
{
    [Header("悬浮参数")]
    public float floatingHeight = 0.2f;
    public float floatingSpeed = 1f;
    public float rotationSpeed = 15f;
    
    private Vector3 startPosition;
    private float timeOffset;
    
    void Start()
    {
        startPosition = transform.position;
        timeOffset = Random.Range(0f, 2f * Mathf.PI);
    }
    
    void Update()
    {
        // 上下浮动
        float newY = startPosition.y + Mathf.Sin((Time.time + timeOffset) * floatingSpeed) * floatingHeight;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);
        
        // 旋转
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
    }
    
    /// <summary>
    /// 播放收集动画
    /// </summary>
    public void PlayCollectionAnimation()
    {
        StartCoroutine(CollectionCoroutine());
    }
    
    System.Collections.IEnumerator CollectionCoroutine()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 originalPosition = transform.position;
        
        float duration = 1f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // 缩小并上升
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
            transform.position = originalPosition + Vector3.up * t * 2f;
            
            yield return null;
        }
        
        Destroy(gameObject);
    }
}
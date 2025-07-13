using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class SampleReconstructionSystem : MonoBehaviour
{
    [Header("重建参数")]
    public Material defaultSampleMaterial;
    public float meshSmoothing = 0.1f;
    public bool generateColliders = true;
    public bool showLayerBoundaries = true;
    
    [Header("视觉设置")]
    public Color boundaryLineColor = Color.black;
    public float boundaryLineWidth = 0.002f;
    public Material boundaryMaterial;
    
    private MeshCombiner meshCombiner;
    
    void Start()
    {
        meshCombiner = gameObject.AddComponent<MeshCombiner>();
        InitializeBoundaryMaterial();
    }
    
    void InitializeBoundaryMaterial()
    {
        if (boundaryMaterial == null)
        {
            boundaryMaterial = new Material(Shader.Find("Unlit/Color"));
            boundaryMaterial.color = boundaryLineColor;
        }
    }
    
    public GameObject ReconstructSample(GeologicalSampleData sampleData, Vector3 position)
    {
        
        
        // 创建样本容器
        GameObject sampleContainer = new GameObject($"GeologicalSample_{sampleData.sampleID}");
        sampleContainer.transform.position = position;
        
        // 添加样本组件
        ReconstructedGeologicalSample sampleComponent = sampleContainer.AddComponent<ReconstructedGeologicalSample>();
        sampleComponent.sampleData = sampleData;
        
        // 重建每个段
        List<GameObject> segmentObjects = new List<GameObject>();
        
        // 对段进行排序，确保按深度顺序重建
        var sortedSegments = sampleData.segments.OrderBy(s => s.depth).ToArray();
        
        for (int i = 0; i < sortedSegments.Length; i++)
        {
            LayerSampleSegment segment = sortedSegments[i];
            GameObject segmentObj = ReconstructSegment(segment, i, sampleContainer.transform);
            
            if (segmentObj != null)
            {
                segmentObjects.Add(segmentObj);
            }
        }
        
        // 添加地层边界线（暂时禁用以减少视觉混乱）
        if (showLayerBoundaries && sampleData.segments.Length > 3)
        {
            CreateLayerBoundaries(sampleData, sampleContainer.transform);
        }
        
        // 添加基本物理组件（由悬浮系统管理）
        AddBasicPhysicsComponents(sampleContainer, sampleData);
        
        // 添加样本信息组件
        SampleInfoDisplay infoDisplay = sampleContainer.AddComponent<SampleInfoDisplay>();
        infoDisplay.Initialize(sampleData);
        
        // 添加悬浮展示组件
        SampleFloatingDisplay floatingDisplay = sampleContainer.AddComponent<SampleFloatingDisplay>();
        
        // 设置简单的悬浮参数
        floatingDisplay.floatingHeight = 1.0f + Random.Range(0f, 0.3f); // 稍微随机高度
        floatingDisplay.floatingAmplitude = 0.15f; // 轻微浮动
        floatingDisplay.floatingSpeed = 0.8f; // 固定速度
        floatingDisplay.rotationSpeed = new Vector3(0, 15f, 0); // 简单Y轴旋转
        floatingDisplay.enablePulse = false; // 禁用脉冲
        floatingDisplay.preserveOriginalMaterials = true; // 保持原始材质
        
        
        
        return sampleContainer;
    }
    
    GameObject ReconstructSegment(LayerSampleSegment segment, int segmentIndex, Transform parent)
    {
        if (segment.layersInSection.Length == 0) return null;
        
        GameObject segmentObj = new GameObject($"Segment_{segmentIndex}_Depth_{segment.depth:F2}");
        segmentObj.transform.SetParent(parent);
        segmentObj.transform.localPosition = Vector3.zero; // 不使用depth偏移，让CreateSimpleLayerSegment处理
        
        // 如果只有一个地层，直接创建圆柱体
        if (segment.layersInSection.Length == 1)
        {
            LayerInfo layer = segment.layersInSection[0];
            GameObject layerObj = CreateSimpleLayerSegment(layer, segment, segmentObj.transform, segmentIndex);
            
            // 添加地层信息组件
            LayerSegmentInfo info = layerObj.AddComponent<LayerSegmentInfo>();
            info.layerInfo = layer;
            info.segment = segment;
        }
        else
        {
            // 多地层情况，创建复杂几何体
            CreateComplexLayerSegment(segment, segmentObj.transform);
        }
        
        return segmentObj;
    }
    
    GameObject CreateSimpleLayerSegment(LayerInfo layerInfo, LayerSampleSegment segment, Transform parent, int segmentIndex)
    {
        GameObject layerObj = new GameObject($"Layer_{layerInfo.layer.layerName}_Seg{segmentIndex}");
        layerObj.transform.SetParent(parent);
        layerObj.transform.localPosition = Vector3.zero;
        
        // 创建更合理尺寸的圆柱体
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.transform.SetParent(layerObj.transform);
        
        // 统一的尺寸参数 - 确保所有段都使用相同的半径和高度计算方式
        float uniformRadius = 0.3f; // 稍微减小半径，让段之间更明显
        float segmentHeight = Mathf.Max(segment.segmentHeight, 0.15f); // 使用实际段高度
        
        // Unity圆柱体默认直径2，高度2，所以需要缩放到我们想要的尺寸
        cylinder.transform.localScale = new Vector3(
            uniformRadius, // X轴缩放（半径）
            segmentHeight * 0.5f, // Y轴缩放（高度）
            uniformRadius  // Z轴缩放（半径）
        );
        
        // 重要修复：使用段的实际深度进行定位
        // 每个段都应该在其正确的深度位置
        float segmentCenterY = -(segment.depth + segmentHeight * 0.5f);
        cylinder.transform.localPosition = new Vector3(0, segmentCenterY, 0);
        
        // 修复材质渲染问题 - 根据段的索引给不同颜色
        MeshRenderer meshRenderer = cylinder.GetComponent<MeshRenderer>();
        
        // 创建新的材质实例
        Material instanceMaterial;
        if (layerInfo.material != null)
        {
            instanceMaterial = new Material(layerInfo.material);
        }
        else if (defaultSampleMaterial != null)
        {
        	instanceMaterial = new Material(defaultSampleMaterial);
        }
        else
        {
            // 创建基本材质
            instanceMaterial = new Material(Shader.Find("Standard"));
        }
        
        // 设置颜色 - 为不同段设置稍微不同的颜色以便区分
        Color segmentColor = layerInfo.color;
        if (segmentIndex % 2 == 1) // 奇数段稍微调亮
        {
            segmentColor = Color.Lerp(segmentColor, Color.white, 0.2f);
        }
        instanceMaterial.color = segmentColor;
        
        // 确保纹理设置正确
        instanceMaterial.mainTextureScale = Vector2.one;
        instanceMaterial.mainTextureOffset = Vector2.zero;
        
        // 禁用金属度以获得更好的显示效果
        if (instanceMaterial.HasProperty("_Metallic"))
        {
            instanceMaterial.SetFloat("_Metallic", 0f);
        }
        if (instanceMaterial.HasProperty("_Smoothness"))
        {
            instanceMaterial.SetFloat("_Smoothness", 0.3f);
        }
        
        meshRenderer.material = instanceMaterial;
        
        
        
        return layerObj;
    }
    
    void CreateComplexLayerSegment(LayerSampleSegment segment, Transform parent)
    {
        foreach (LayerInfo layerInfo in segment.layersInSection)
        {
            GameObject layerPart = CreateLayerPart(layerInfo, segment, parent);
            
            // 添加层信息
            LayerSegmentInfo info = layerPart.AddComponent<LayerSegmentInfo>();
            info.layerInfo = layerInfo;
            info.segment = segment;
        }
        
        // 创建接触界面
        foreach (ContactInterface contact in segment.interfaces)
        {
            CreateContactInterface(contact, segment, parent);
        }
    }
    
    GameObject CreateLayerPart(LayerInfo layerInfo, LayerSampleSegment segment, Transform parent)
    {
        GameObject layerPart = new GameObject($"LayerPart_{layerInfo.layer.layerName}");
        layerPart.transform.SetParent(parent);
        layerPart.transform.localPosition = Vector3.zero;
        
        // 根据边界形状创建网格
        Mesh layerMesh = CreateLayerPartMesh(layerInfo, segment);
        
        MeshFilter meshFilter = layerPart.AddComponent<MeshFilter>();
        meshFilter.mesh = layerMesh;
        
        MeshRenderer meshRenderer = layerPart.AddComponent<MeshRenderer>();
        meshRenderer.material = layerInfo.material != null ? layerInfo.material : defaultSampleMaterial;
        
        // 应用地层颜色
        if (meshRenderer.material != null)
        {
            Material instanceMaterial = new Material(meshRenderer.material);
            instanceMaterial.color = layerInfo.color;
            meshRenderer.material = instanceMaterial;
        }
        
        return layerPart;
    }
    
    Mesh CreateLayerPartMesh(LayerInfo layerInfo, LayerSampleSegment segment)
    {
        Mesh mesh = new Mesh();
        mesh.name = $"LayerPart_{layerInfo.layer.layerName}";
        
        // 将2D边界形状挤出为3D网格
        Vector3[] vertices = ExtrudeBoundaryShape(layerInfo.boundaryShape, segment.segmentHeight);
        int[] triangles = GenerateTriangles(layerInfo.boundaryShape.Length);
        Vector2[] uvs = GenerateUVs(vertices);
        Vector3[] normals = CalculateNormals(vertices, triangles);
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.normals = normals;
        
        mesh.RecalculateBounds();
        
        return mesh;
    }
    
    Vector3[] ExtrudeBoundaryShape(Vector2[] shape, float height)
    {
        List<Vector3> vertices = new List<Vector3>();
        
        // 顶部顶点
        foreach (Vector2 point in shape)
        {
            vertices.Add(new Vector3(point.x, 0, point.y));
        }
        
        // 底部顶点
        foreach (Vector2 point in shape)
        {
            vertices.Add(new Vector3(point.x, -height, point.y));
        }
        
        return vertices.ToArray();
    }
    
    int[] GenerateTriangles(int shapeVertexCount)
    {
        List<int> triangles = new List<int>();
        
        // 顶面三角形
        for (int i = 1; i < shapeVertexCount - 1; i++)
        {
            triangles.Add(0);
            triangles.Add(i + 1);
            triangles.Add(i);
        }
        
        // 底面三角形
        int offset = shapeVertexCount;
        for (int i = 1; i < shapeVertexCount - 1; i++)
        {
            triangles.Add(offset);
            triangles.Add(offset + i);
            triangles.Add(offset + i + 1);
        }
        
        // 侧面三角形
        for (int i = 0; i < shapeVertexCount; i++)
        {
            int next = (i + 1) % shapeVertexCount;
            
            // 第一个三角形
            triangles.Add(i);
            triangles.Add(next);
            triangles.Add(offset + i);
            
            // 第二个三角形
            triangles.Add(next);
            triangles.Add(offset + next);
            triangles.Add(offset + i);
        }
        
        return triangles.ToArray();
    }
    
    Vector2[] GenerateUVs(Vector3[] vertices)
    {
        Vector2[] uvs = new Vector2[vertices.Length];
        
        // 找到顶点的边界范围，用于正确的UV映射
        Vector3 min = vertices[0];
        Vector3 max = vertices[0];
        for (int i = 1; i < vertices.Length; i++)
        {
            min = Vector3.Min(min, vertices[i]);
            max = Vector3.Max(max, vertices[i]);
        }
        
        Vector3 size = max - min;
        
        for (int i = 0; i < vertices.Length; i++)
        {
            // 改进的UV映射，避免拉伸和条纹
            Vector3 localPos = vertices[i] - min;
            uvs[i] = new Vector2(
                size.x > 0.001f ? localPos.x / size.x : 0.5f,
                size.z > 0.001f ? localPos.z / size.z : 0.5f
            );
        }
        
        return uvs;
    }
    
    Vector3[] CalculateNormals(Vector3[] vertices, int[] triangles)
    {
        Vector3[] normals = new Vector3[vertices.Length];
        
        // 计算每个三角形的法向量
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 v1 = vertices[triangles[i]];
            Vector3 v2 = vertices[triangles[i + 1]];
            Vector3 v3 = vertices[triangles[i + 2]];
            
            Vector3 normal = Vector3.Cross(v2 - v1, v3 - v1).normalized;
            
            normals[triangles[i]] += normal;
            normals[triangles[i + 1]] += normal;
            normals[triangles[i + 2]] += normal;
        }
        
        // 归一化法向量
        for (int i = 0; i < normals.Length; i++)
        {
            normals[i] = normals[i].normalized;
        }
        
        return normals;
    }
    
    Mesh CreateCylinderMesh(Vector3 center, Vector3 sampleCenter, Vector3 direction, float scale)
    {
        // 创建基础圆柱体
        GameObject tempCylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Mesh cylinderMesh = tempCylinder.GetComponent<MeshFilter>().mesh;
        
        // 复制网格
        Mesh mesh = new Mesh();
        mesh.vertices = cylinderMesh.vertices;
        mesh.triangles = cylinderMesh.triangles;
        mesh.uv = cylinderMesh.uv;
        mesh.normals = cylinderMesh.normals;
        
        DestroyImmediate(tempCylinder);
        
        return mesh;
    }
    
    void CreateContactInterface(ContactInterface contact, LayerSampleSegment segment, Transform parent)
    {
        if (contact.contactLine.Length < 2) return;
        
        GameObject contactObj = new GameObject($"Contact_{contact.layerA.layerName}_{contact.layerB.layerName}");
        contactObj.transform.SetParent(parent);
        contactObj.transform.localPosition = Vector3.zero;
        
        // 创建接触线的可视化
        LineRenderer lineRenderer = contactObj.AddComponent<LineRenderer>();
        lineRenderer.material = boundaryMaterial;
        lineRenderer.startWidth = boundaryLineWidth;
        lineRenderer.endWidth = boundaryLineWidth;
        lineRenderer.positionCount = contact.contactLine.Length;
        
        Vector3[] positions = new Vector3[contact.contactLine.Length];
        for (int i = 0; i < contact.contactLine.Length; i++)
        {
            Vector2 point2D = contact.contactLine[i];
            positions[i] = new Vector3(point2D.x, -segment.segmentHeight * 0.5f, point2D.y);
        }
        
        lineRenderer.SetPositions(positions);
        
        // 添加接触信息组件
        ContactInfo contactInfo = contactObj.AddComponent<ContactInfo>();
        contactInfo.contactInterface = contact;
    }
    
    void CreateLayerBoundaries(GeologicalSampleData sampleData, Transform parent)
    {
        GameObject boundariesObj = new GameObject("LayerBoundaries");
        boundariesObj.transform.SetParent(parent);
        boundariesObj.transform.localPosition = Vector3.zero;
        
        for (int i = 0; i < sampleData.segments.Length - 1; i++)
        {
            CreateSegmentBoundary(sampleData.segments[i], sampleData.segments[i + 1], boundariesObj.transform);
        }
    }
    
    void CreateSegmentBoundary(LayerSampleSegment segmentA, LayerSampleSegment segmentB, Transform parent)
    {
        GameObject boundaryObj = new GameObject($"Boundary_{segmentA.depth:F2}_{segmentB.depth:F2}");
        boundaryObj.transform.SetParent(parent);
        
        float boundaryDepth = segmentA.depth + segmentA.segmentHeight;
        boundaryObj.transform.localPosition = Vector3.down * boundaryDepth;
        
        // 创建边界线圆环  
        float defaultRadius = 1.0f; // 默认半径
        CreateBoundaryRing(boundaryObj, defaultRadius);
    }
    
    void CreateBoundaryRing(GameObject parent, float radius)
    {
        LineRenderer lineRenderer = parent.AddComponent<LineRenderer>();
        lineRenderer.material = boundaryMaterial;
        lineRenderer.startWidth = boundaryLineWidth;
        lineRenderer.endWidth = boundaryLineWidth;
        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = true;
        
        int segments = 32;
        lineRenderer.positionCount = segments;
        
        Vector3[] positions = new Vector3[segments];
        for (int i = 0; i < segments; i++)
        {
            float angle = i * 2f * Mathf.PI / segments;
            positions[i] = new Vector3(
                Mathf.Cos(angle) * radius,
                0,
                Mathf.Sin(angle) * radius
            );
        }
        
        lineRenderer.SetPositions(positions);
    }
    
    void AddPhysicsComponents(GameObject sampleContainer, GeologicalSampleData sampleData)
    {
        // 添加整体碰撞器
        CapsuleCollider capsuleCollider = sampleContainer.AddComponent<CapsuleCollider>();
        capsuleCollider.radius = sampleData.drillingRadius;
        capsuleCollider.height = sampleData.drillingDepth;
        capsuleCollider.center = Vector3.down * sampleData.drillingDepth * 0.5f;
        
        // 添加刚体，但设置为运动学以防止掉落
        Rigidbody rb = sampleContainer.AddComponent<Rigidbody>();
        rb.mass = CalculateSampleMass(sampleData);
        rb.isKinematic = true; // 防止重力影响，样本不会掉落
        rb.useGravity = false; // 明确禁用重力
        
        // 延迟几秒后启用物理，让样本稳定
        StartCoroutine(EnablePhysicsDelayed(rb, 3.0f));
        
        // 添加触发器用于拾取
        BoxCollider triggerCollider = sampleContainer.AddComponent<BoxCollider>();
        triggerCollider.isTrigger = true;
        triggerCollider.size = new Vector3(
            sampleData.drillingRadius * 2.2f,
            sampleData.drillingDepth * 1.1f,
            sampleData.drillingRadius * 2.2f
        );
        triggerCollider.center = capsuleCollider.center;
    }
    
    System.Collections.IEnumerator EnablePhysicsDelayed(Rigidbody rb, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearDamping = 2.0f; // 增加阻尼，减缓下落
            rb.angularDamping = 2.0f;
        }
    }
    
    float CalculateSampleMass(GeologicalSampleData sampleData)
    {
        float totalMass = 0f;
        
        foreach (var stat in sampleData.layerStats)
        {
            // 根据地层类型和厚度计算质量
            float layerDensity = GetLayerDensity(stat.layerType);
            float volume = stat.totalThickness * Mathf.PI * sampleData.drillingRadius * sampleData.drillingRadius;
            totalMass += volume * layerDensity;
        }
        
        return Mathf.Max(totalMass, 0.1f); // 最小质量
    }
    
    float GetLayerDensity(LayerType layerType)
    {
        switch (layerType)
        {
            case LayerType.Soil: return 1.5f;
            case LayerType.Sedimentary: return 2.3f;
            case LayerType.Igneous: return 2.7f;
            case LayerType.Metamorphic: return 2.8f;
            case LayerType.Alluvium: return 1.8f;
            case LayerType.Bedrock: return 2.9f;
            default: return 2.5f;
        }
    }
    
    void AddBasicPhysicsComponents(GameObject sampleContainer, GeologicalSampleData sampleData)
    {
        // 添加简单的碰撞器
        CapsuleCollider capsuleCollider = sampleContainer.AddComponent<CapsuleCollider>();
        capsuleCollider.radius = sampleData.drillingRadius;
        capsuleCollider.height = sampleData.drillingDepth;
        capsuleCollider.center = Vector3.down * sampleData.drillingDepth * 0.5f;
        
        // 添加刚体（初始设为运动学，由悬浮系统控制）
        Rigidbody rb = sampleContainer.AddComponent<Rigidbody>();
        rb.mass = CalculateSampleMass(sampleData);
        rb.isKinematic = true; // 悬浮系统会管理这个
        rb.useGravity = false;
        
        // 添加触发器用于交互
        BoxCollider triggerCollider = sampleContainer.AddComponent<BoxCollider>();
        triggerCollider.isTrigger = true;
        triggerCollider.size = new Vector3(
            sampleData.drillingRadius * 2.2f,
            sampleData.drillingDepth * 1.1f,
            sampleData.drillingRadius * 2.2f
        );
        triggerCollider.center = capsuleCollider.center;
    }
}
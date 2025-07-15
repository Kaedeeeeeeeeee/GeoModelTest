using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 薄片样本生成器 - 生成不规则的地质薄片样本
/// </summary>
public class SlabSampleGenerator : MonoBehaviour
{
    [Header("薄片设置")]
    public float baseRadius = 0.4f; // 基础半径
    public float thickness = 0.06f; // 薄片厚度
    public int polygonSides = 6; // 多边形边数
    public float irregularityFactor = 0.3f; // 不规则度因子
    
    [Header("表面效果")]
    public bool addSurfaceNoise = true; // 添加表面噪声
    public float noiseScale = 0.1f; // 噪声缩放
    public float noiseStrength = 0.02f; // 噪声强度
    
    [Header("材质设置")]
    public Material defaultSlabMaterial; // 默认薄片材质
    public Color[] geologicalColors = {
        new Color(0.7f, 0.5f, 0.3f), // 棕色
        new Color(0.6f, 0.6f, 0.5f), // 灰色
        new Color(0.8f, 0.4f, 0.2f), // 红棕色
        new Color(0.5f, 0.7f, 0.4f), // 绿色
        new Color(0.4f, 0.4f, 0.6f)  // 蓝灰色
    };
    
    /// <summary>
    /// 使用原始材质生成薄片样本
    /// </summary>
    public GameObject GenerateSlabSampleWithMaterial(Vector3 position, Material originalMaterial, GeologyLayer sourceLayer, bool enableFloating = true)
    {
        // 创建样本容器
        GameObject slabSample = new GameObject("HammerSlabSample");
        slabSample.transform.position = position;
        
        // 生成薄片网格
        Mesh slabMesh = CreateSlabMesh();
        
        // 添加网格组件
        MeshFilter meshFilter = slabSample.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = slabSample.AddComponent<MeshRenderer>();
        
        meshFilter.mesh = slabMesh;
        
        // 直接使用原始材质（就像钻塔系统一样）
        if (originalMaterial != null)
        {
            meshRenderer.material = originalMaterial;
        }
        else
        {
            // 如果没有原始材质，创建一个简单的
            Material fallbackMaterial = new Material(Shader.Find("Standard"));
            fallbackMaterial.color = sourceLayer?.layerColor ?? new Color(0.7f, 0.5f, 0.3f);
            meshRenderer.material = fallbackMaterial;
        }
        
        // 添加物理组件
        MeshCollider meshCollider = slabSample.AddComponent<MeshCollider>();
        meshCollider.convex = true;
        
        Rigidbody rb = slabSample.AddComponent<Rigidbody>();
        rb.mass = 0.5f;
        
        // 根据参数决定是否添加悬浮效果
        if (enableFloating)
        {
            SimpleSampleFloating floating = slabSample.AddComponent<SimpleSampleFloating>();
            floating.floatHeight = 0.15f;
            floating.floatSpeed = 1f;
            rb.useGravity = false; // 禁用重力，由悬浮组件控制
            rb.isKinematic = false;
            Debug.Log("添加了悬浮效果组件");
        }
        else
        {
            // 从背包放置时，不添加悬浮效果，让SamplePlacer来管理
            rb.useGravity = false; // 先禁用重力，等SamplePlacer添加悬浮效果
            rb.isKinematic = false;
            Debug.Log("样本设置为静态放置，由SamplePlacer管理悬浮效果");
        }
        
        // 添加样本标识组件
        SlabSampleMarker marker = slabSample.AddComponent<SlabSampleMarker>();
        marker.collectionTime = System.DateTime.Now;
        marker.sampleThickness = thickness;
        
        // 为兼容性创建LayerInfo
        if (sourceLayer != null)
        {
            marker.surfaceLayer = new SampleItem.LayerInfo
            {
                layerName = sourceLayer.layerName,
                thickness = thickness,
                depthStart = 0f,
                depthEnd = thickness,
                layerColor = originalMaterial?.color ?? sourceLayer.layerColor,
                materialName = originalMaterial?.name ?? "UnknownMaterial",
                layerDescription = "使用地层原始材质"
            };
        }
        
        return slabSample;
    }
    
    /// <summary>
    /// 生成薄片样本（旧方法，保留兼容性）
    /// </summary>
    public GameObject GenerateSlabSample(Vector3 position, SampleItem.LayerInfo surfaceLayer = null)
    {
        
        // 创建样本容器
        GameObject slabSample = new GameObject("HammerSlabSample");
        slabSample.transform.position = position;
        
        // 生成薄片网格
        Mesh slabMesh = CreateSlabMesh();
        
        // 添加网格组件
        MeshFilter meshFilter = slabSample.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = slabSample.AddComponent<MeshRenderer>();
        
        meshFilter.mesh = slabMesh;
        
        // 应用材质
        Material slabMaterial = CreateSlabMaterial(surfaceLayer);
        meshRenderer.material = slabMaterial;
        
        // 添加物理组件
        MeshCollider meshCollider = slabSample.AddComponent<MeshCollider>();
        meshCollider.convex = true;
        
        Rigidbody rb = slabSample.AddComponent<Rigidbody>();
        rb.mass = 0.5f; // 轻质薄片
        
        // 添加样本悬浮效果
        SimpleSampleFloating floating = slabSample.AddComponent<SimpleSampleFloating>();
        floating.floatHeight = 0.15f; // 悬浮高度
        floating.floatSpeed = 1f; // 悬浮速度
        
        // 添加样本标识组件
        SlabSampleMarker marker = slabSample.AddComponent<SlabSampleMarker>();
        marker.collectionTime = System.DateTime.Now;
        marker.sampleThickness = thickness;
        marker.surfaceLayer = surfaceLayer;
        
        Debug.Log($"生成薄片样本: {slabSample.name} 在位置 {position}");
        
        return slabSample;
    }
    
    /// <summary>
    /// 创建薄片网格
    /// </summary>
    Mesh CreateSlabMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "SlabMesh";
        
        // 生成不规则多边形顶点
        Vector3[] topVertices = GenerateIrregularPolygon();
        Vector3[] bottomVertices = new Vector3[topVertices.Length];
        
        // 创建底面顶点（向下偏移）
        for (int i = 0; i < topVertices.Length; i++)
        {
            bottomVertices[i] = topVertices[i] - Vector3.up * thickness;
        }
        
        // 组合所有顶点
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        List<Vector3> normals = new List<Vector3>();
        
        // 添加顶面
        AddPolygonFace(vertices, triangles, uvs, normals, topVertices, Vector3.up, false);
        
        // 添加底面
        AddPolygonFace(vertices, triangles, uvs, normals, bottomVertices, Vector3.down, true);
        
        // 添加侧面
        AddSideFaces(vertices, triangles, uvs, normals, topVertices, bottomVertices);
        
        // 应用噪声
        if (addSurfaceNoise)
        {
            ApplySurfaceNoise(vertices);
        }
        
        // 设置网格数据
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        
        // 重新计算法线和边界
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        // 由于我们使用双面渲染，确保法线正确计算
        mesh.RecalculateTangents();
        
        // 确保网格可读（重要：这样SampleItem可以保存网格数据）
        mesh.MarkDynamic();
        
        // 启用双面网格生成，解决单向可见问题
        mesh = CreateDoubleSidedMesh(mesh);
        
        return mesh;
    }
    
    /// <summary>
    /// 生成不规则多边形顶点
    /// </summary>
    Vector3[] GenerateIrregularPolygon()
    {
        Vector3[] vertices = new Vector3[polygonSides];
        
        for (int i = 0; i < polygonSides; i++)
        {
            // 基础角度
            float angle = (float)i / polygonSides * Mathf.PI * 2f;
            
            // 添加随机偏移
            float angleOffset = (Random.value - 0.5f) * irregularityFactor;
            angle += angleOffset;
            
            // 基础半径加随机变化
            float radius = baseRadius * (1f + (Random.value - 0.5f) * irregularityFactor);
            
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            
            vertices[i] = new Vector3(x, thickness * 0.5f, z);
        }
        
        return vertices;
    }
    
    /// <summary>
    /// 添加多边形面
    /// </summary>
    void AddPolygonFace(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, 
                       List<Vector3> normals, Vector3[] faceVertices, Vector3 normal, bool reverse)
    {
        int startIndex = vertices.Count;
        
        // 添加顶点
        vertices.AddRange(faceVertices);
        
        // 添加法线
        for (int i = 0; i < faceVertices.Length; i++)
        {
            normals.Add(normal);
        }
        
        // 添加UV坐标
        for (int i = 0; i < faceVertices.Length; i++)
        {
            Vector3 vertex = faceVertices[i];
            Vector2 uv = new Vector2(
                (vertex.x / baseRadius + 1f) * 0.5f,
                (vertex.z / baseRadius + 1f) * 0.5f
            );
            uvs.Add(uv);
        }
        
        // 添加三角形（扇形三角化）- 确保正确的绕序
        for (int i = 1; i < faceVertices.Length - 1; i++)
        {
            if (reverse) // 底面（法线向下）
            {
                triangles.Add(startIndex);
                triangles.Add(startIndex + i + 1);
                triangles.Add(startIndex + i);
            }
            else // 顶面（法线向上）
            {
                triangles.Add(startIndex);
                triangles.Add(startIndex + i);
                triangles.Add(startIndex + i + 1);
            }
        }
    }
    
    /// <summary>
    /// 添加侧面
    /// </summary>
    void AddSideFaces(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, 
                     List<Vector3> normals, Vector3[] topVertices, Vector3[] bottomVertices)
    {
        for (int i = 0; i < topVertices.Length; i++)
        {
            int next = (i + 1) % topVertices.Length;
            
            // 当前侧面的四个顶点
            Vector3 topCurrent = topVertices[i];
            Vector3 topNext = topVertices[next];
            Vector3 bottomCurrent = bottomVertices[i];
            Vector3 bottomNext = bottomVertices[next];
            
            // 计算侧面法线
            Vector3 sideDir = (topNext - topCurrent).normalized;
            Vector3 sideNormal = Vector3.Cross(Vector3.up, sideDir).normalized;
            
            int startIndex = vertices.Count;
            
            // 添加四个顶点
            vertices.Add(topCurrent);
            vertices.Add(topNext);
            vertices.Add(bottomNext);
            vertices.Add(bottomCurrent);
            
            // 添加法线
            for (int j = 0; j < 4; j++)
            {
                normals.Add(sideNormal);
            }
            
            // 添加UV坐标
            uvs.Add(new Vector2(0, 1));
            uvs.Add(new Vector2(1, 1));
            uvs.Add(new Vector2(1, 0));
            uvs.Add(new Vector2(0, 0));
            
            // 添加两个三角形（确保外向法线）
            // 第一个三角形（逆时针绕序，外向法线）
            triangles.Add(startIndex);
            triangles.Add(startIndex + 1);
            triangles.Add(startIndex + 2);
            
            // 第二个三角形（逆时针绕序，外向法线）
            triangles.Add(startIndex);
            triangles.Add(startIndex + 2);
            triangles.Add(startIndex + 3);
        }
    }
    
    /// <summary>
    /// 应用表面噪声
    /// </summary>
    void ApplySurfaceNoise(List<Vector3> vertices)
    {
        for (int i = 0; i < vertices.Count; i++)
        {
            Vector3 vertex = vertices[i];
            
            // 只对Y方向添加噪声
            float noise = Mathf.PerlinNoise(vertex.x * noiseScale, vertex.z * noiseScale);
            vertex.y += (noise - 0.5f) * noiseStrength;
            
            vertices[i] = vertex;
        }
    }
    
    /// <summary>
    /// 创建薄片材质
    /// </summary>
    Material CreateSlabMaterial(SampleItem.LayerInfo surfaceLayer)
    {
        Material material;
        
        if (defaultSlabMaterial != null)
        {
            material = new Material(defaultSlabMaterial);
        }
        else
        {
            // 简化着色器选择，优先使用Standard（最兼容）
            Shader shader = Shader.Find("Standard");
            if (shader == null)
            {
                shader = Shader.Find("Legacy Shaders/Diffuse");
            }
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }
            
            if (shader != null)
            {
                material = new Material(shader);
                Debug.Log($"使用着色器: {shader.name}");
            }
            else
            {
                Debug.LogError("未找到可用的着色器！");
                material = new Material(Shader.Find("Sprites/Default")); // 最基本的备用着色器
            }
        }
        
        // 设置颜色
        Color slabColor;
        if (surfaceLayer != null)
        {
            slabColor = surfaceLayer.layerColor;
        }
        else
        {
            // 随机选择地质颜色
            slabColor = geologicalColors[Random.Range(0, geologicalColors.Length)];
        }
        
        // 设置材质颜色
        material.color = slabColor;
        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", slabColor);
        }
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", slabColor);
        }
        
        // 简化双面渲染设置，避免材质错误
        try
        {
            // 最重要：关闭背面剔除
            if (material.HasProperty("_Cull"))
            {
                material.SetInt("_Cull", 0); // 0 = Off（双面渲染）
                Debug.Log("成功设置双面渲染 (_Cull = 0)");
            }
            
            // 只设置必要的基础属性
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", 0.1f);
            if (material.HasProperty("_Glossiness"))
                material.SetFloat("_Glossiness", 0.3f);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", 0.3f);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"设置材质属性时出错: {e.Message}");
        }
        
        return material;
    }
    
    /// <summary>
    /// 创建双面网格（复制所有三角形并翻转法线）
    /// </summary>
    Mesh CreateDoubleSidedMesh(Mesh originalMesh)
    {
        Vector3[] originalVertices = originalMesh.vertices;
        int[] originalTriangles = originalMesh.triangles;
        Vector3[] originalNormals = originalMesh.normals;
        Vector2[] originalUV = originalMesh.uv;
        
        // 创建双倍大小的数组
        Vector3[] doubleSidedVertices = new Vector3[originalVertices.Length * 2];
        int[] doubleSidedTriangles = new int[originalTriangles.Length * 2];
        Vector3[] doubleSidedNormals = new Vector3[originalNormals.Length * 2];
        Vector2[] doubleSidedUV = new Vector2[originalUV.Length * 2];
        
        // 复制原始面（正面）
        for (int i = 0; i < originalVertices.Length; i++)
        {
            doubleSidedVertices[i] = originalVertices[i];
            doubleSidedNormals[i] = originalNormals[i];
            doubleSidedUV[i] = originalUV[i];
        }
        
        // 复制原始三角形（正面）
        for (int i = 0; i < originalTriangles.Length; i++)
        {
            doubleSidedTriangles[i] = originalTriangles[i];
        }
        
        // 添加背面顶点（位置相同，法线反向）
        for (int i = 0; i < originalVertices.Length; i++)
        {
            doubleSidedVertices[originalVertices.Length + i] = originalVertices[i];
            doubleSidedNormals[originalVertices.Length + i] = -originalNormals[i]; // 反向法线
            doubleSidedUV[originalVertices.Length + i] = originalUV[i];
        }
        
        // 添加背面三角形（反向绕序）
        for (int i = 0; i < originalTriangles.Length; i += 3)
        {
            int baseIndex = originalTriangles.Length + i;
            int vertexOffset = originalVertices.Length;
            
            // 反向三角形绕序来面向相反方向
            doubleSidedTriangles[baseIndex] = originalTriangles[i] + vertexOffset;
            doubleSidedTriangles[baseIndex + 1] = originalTriangles[i + 2] + vertexOffset; // 交换顺序
            doubleSidedTriangles[baseIndex + 2] = originalTriangles[i + 1] + vertexOffset; // 交换顺序
        }
        
        // 创建新的双面网格
        Mesh doubleSidedMesh = new Mesh();
        doubleSidedMesh.name = originalMesh.name + "_DoubleSided";
        doubleSidedMesh.vertices = doubleSidedVertices;
        doubleSidedMesh.triangles = doubleSidedTriangles;
        doubleSidedMesh.normals = doubleSidedNormals;
        doubleSidedMesh.uv = doubleSidedUV;
        doubleSidedMesh.RecalculateBounds();
        doubleSidedMesh.MarkDynamic();
        
        return doubleSidedMesh;
    }
    
    /// <summary>
    /// 获取表面地质层信息
    /// </summary>
    SampleItem.LayerInfo GetSurfaceLayerInfo(Vector3 position)
    {
        // 这里可以集成地质检测系统来获取真实的地层信息
        // 暂时返回null，使用随机颜色
        return null;
    }
}

/// <summary>
/// 薄片样本标记组件 - 标识样本的基础信息
/// </summary>
public class SlabSampleMarker : MonoBehaviour
{
    [Header("样本信息")]
    public System.DateTime collectionTime;
    public float sampleThickness;
    public SampleItem.LayerInfo surfaceLayer;
    public string collectorToolID = "1002"; // 锤子工具ID
    
    /// <summary>
    /// 获取样本描述
    /// </summary>
    public string GetSampleDescription()
    {
        return $"地质薄片样本\n" +
               $"采集时间: {collectionTime:yyyy-MM-dd HH:mm:ss}\n" +
               $"厚度: {sampleThickness * 100:F1}cm\n" +
               $"采集工具: 地质锤";
    }
}
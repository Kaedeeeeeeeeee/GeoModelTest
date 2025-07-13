using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 几何样本重建系统 - 简化版
/// 使用Y坐标差异算法进行地层厚度计算
/// </summary>
public class GeometricSampleReconstructor : MonoBehaviour
{
    [System.Serializable]
    public class ReconstructedSample
    {
        public string sampleID;
        public GameObject sampleContainer;
        public LayerSegment[] layerSegments;
        public SamplePhysics physics;
        public SampleDisplay display;
        public LayerGeometricCutter.GeometricSampleData originalData;
        public float totalHeight;
        public float totalVolume;
        public Vector3 centerOfMass;
    }
    
    [System.Serializable]
    public class LayerSegment
    {
        public GameObject segmentObject;
        public GeologyLayer sourceLayer;
        public Mesh geometry;
        public Material material;
        public LayerGeometricCutter.LayerCutResult cutResult;
        public float relativeDepth;
        public Vector3 localCenterOfMass;
    }
    
    [System.Serializable]
    public class SamplePhysics
    {
        public Rigidbody rigidbody;
        public Collider[] colliders;
        public float totalMass;
        public bool isFloating;
        public Vector3 floatingCenter;
    }
    
    [System.Serializable]
    public class SampleDisplay
    {
        public bool enableFloating;
        public float floatingHeight;
        public float floatingAmplitude;
        public float rotationSpeed;
        public bool showLayerBoundaries;
        public Material boundaryMaterial;
    }
    
    [Header("重建参数")]
    public bool enableDetailedGeometry = true;
    public float geometrySimplification = 0.1f;
    public bool preserveOriginalMaterials = true;
    public bool generateLayerBoundaries = true;
    
    [Header("显示设置")]
    public float defaultFloatingHeight = 0.3f;
    public float floatingAmplitude = 0.15f;
    public float rotationSpeed = 15f;
    public bool autoStartFloating = true;
    
    [Header("物理设置")]
    public bool enablePhysics = true;
    public float densityMultiplier = 1.0f;
    public bool useCompoundColliders = true;
    
    [Header("材质设置")]
    public Material defaultLayerMaterial;
    public Material boundaryLineMaterial;
    public bool enhanceLayerContrast = true;
    public float contrastFactor = 0.3f;
    
    private LayerGeometricCutter geometricCutter;
    private List<ReconstructedSample> activeSamples = new List<ReconstructedSample>();
    
    void Start()
    {
        InitializeReconstructor();
    }
    
    void InitializeReconstructor()
    {
        geometricCutter = FindFirstObjectByType<LayerGeometricCutter>();
        if (geometricCutter == null)
        {
            GameObject cutterObj = new GameObject("LayerGeometricCutter");
            geometricCutter = cutterObj.AddComponent<LayerGeometricCutter>();
        }
        
        if (defaultLayerMaterial == null)
        {
            defaultLayerMaterial = new Material(Shader.Find("Standard"));
            defaultLayerMaterial.color = Color.gray;
        }
        
        if (boundaryLineMaterial == null)
        {
            boundaryLineMaterial = new Material(Shader.Find("Unlit/Color"));
            boundaryLineMaterial.color = Color.black;
        }
    }
    
    /// <summary>
    /// 重建几何样本（简化版本）
    /// </summary>
    public ReconstructedSample ReconstructSample(Vector3 drillingPoint, Vector3 direction, float radius, float depth, Vector3 displayPosition)
    {
        return ReconstructSample(drillingPoint, direction, radius, depth, displayPosition, 0f, depth);
    }
    
    /// <summary>
    /// 重建几何样本（支持指定深度范围）
    /// </summary>
    public ReconstructedSample ReconstructSample(Vector3 drillingPoint, Vector3 direction, float radius, float depth, Vector3 displayPosition, float depthStart, float depthEnd)
    {
        try
        {
            // 使用新的简化检测系统
            DrillingCylinderGenerator cylinderGen = GetComponent<DrillingCylinderGenerator>();
            if (cylinderGen == null)
            {
                cylinderGen = gameObject.AddComponent<DrillingCylinderGenerator>();
            }
            
            // 获取地层相交信息
            List<LayerIntersection> intersections = cylinderGen.GetLayerIntersections(drillingPoint, direction, depth);
            
            if (intersections.Count == 0)
            {
                return null;
            }
            
            // 创建样本容器
            GameObject sampleContainer = CreateSampleContainer(drillingPoint, displayPosition);
            
            // 重建地层段
            LayerSegment[] layerSegments = ReconstructLayerSegments(intersections, sampleContainer.transform, radius, depthStart, depthEnd);
            
            // 设置物理和显示属性
            SamplePhysics physics = SetupSamplePhysics(sampleContainer, layerSegments);
            SampleDisplay display = SetupSampleDisplay(sampleContainer, displayPosition);
            
            // 创建原始数据结构
            LayerGeometricCutter.GeometricSampleData originalData = new LayerGeometricCutter.GeometricSampleData
            {
                sampleID = System.Guid.NewGuid().ToString(),
                drillingPosition = drillingPoint,
                drillingDirection = direction,
                drillingRadius = radius,
                drillingDepth = depth,
                collectionTime = System.DateTime.Now,
                totalVolume = CalculateTotalVolume(layerSegments)
            };
            
            // 创建重建样本对象
            ReconstructedSample sample = new ReconstructedSample
            {
                sampleID = originalData.sampleID,
                sampleContainer = sampleContainer,
                layerSegments = layerSegments,
                physics = physics,
                display = display,
                originalData = originalData,
                totalHeight = CalculateTotalHeight(layerSegments),
                totalVolume = CalculateTotalVolume(layerSegments),
                centerOfMass = CalculateCenterOfMass(layerSegments)
            };
            
            // 添加样本组件
            SetupSampleComponents(sample);
            
            activeSamples.Add(sample);
            
            return sample;
        }
        catch (System.Exception)
        {
            return null;
        }
    }
    
    /// <summary>
    /// 创建样本容器
    /// </summary>
    private GameObject CreateSampleContainer(Vector3 drillingPoint, Vector3 position)
    {
        GameObject container = new GameObject("GeometricSample_" + System.Guid.NewGuid().ToString().Substring(0, 8));
        container.transform.position = position;
        
        try
        {
            container.tag = "GeologicalSample";
        }
        catch
        {
            // 标签不存在时忽略
        }
        
        return container;
    }
    
    /// <summary>
    /// 重建地层段（使用简化算法）
    /// </summary>
    private LayerSegment[] ReconstructLayerSegments(List<LayerIntersection> intersections, Transform parent, float radius, float depthStart, float depthEnd)
    {
        List<LayerSegment> segments = new List<LayerSegment>();
        
        // 过滤深度范围内的地层
        var filteredIntersections = intersections.Where(i => 
            i.IsValid && 
            i.exitDistance >= depthStart && 
            i.entryDistance < depthEnd
        ).ToList();
        
        // 按深度排序
        filteredIntersections.Sort((a, b) => a.entryDistance.CompareTo(b.entryDistance));
        
        float currentBottom = 0f; // 当前样本的底部位置
        const float safeGap = 0.001f; // 减小到1mm的最小间距
        
        for (int i = 0; i < filteredIntersections.Count; i++)
        {
            var intersection = filteredIntersections[i];
            
            // 调整深度范围
            float adjustedStart = Mathf.Max(intersection.entryDistance, depthStart);
            float adjustedEnd = Mathf.Min(intersection.exitDistance, depthEnd);
            float thickness = adjustedEnd - adjustedStart;
            
            if (thickness <= 0.01f) continue;
            
            // 计算段位置 - 紧密排列，避免大间隔
            float segmentTop = currentBottom;
            float segmentCenter = -(segmentTop + thickness * 0.5f);
            
            // 创建地层段
            LayerSegment segment = CreateLayerSegment(intersection, i, parent, segmentCenter, thickness, radius);
            if (segment != null)
            {
                segments.Add(segment);
                
                // 更新底部位置，为下一层做准备
                currentBottom = segmentTop + thickness + safeGap;
            }
        }
        
        return segments.ToArray();
    }
    
    /// <summary>
    /// 创建地层段
    /// </summary>
    private LayerSegment CreateLayerSegment(LayerIntersection intersection, int index, Transform parent, float segmentCenter, float thickness, float radius)
    {
        GameObject segmentObj = new GameObject("LayerSegment_" + index + "_" + intersection.layer.layerName);
        segmentObj.transform.SetParent(parent);
        segmentObj.transform.localPosition = new Vector3(0, segmentCenter, 0);
        
        // 创建网格
        Mesh layerMesh = CreateCylinderMesh(radius, thickness);
        
        // 创建材质
        Material segmentMaterial = CreateLayerMaterial(intersection.layer, index);
        
        // 添加网格组件
        MeshFilter meshFilter = segmentObj.AddComponent<MeshFilter>();
        meshFilter.mesh = layerMesh;
        
        MeshRenderer meshRenderer = segmentObj.AddComponent<MeshRenderer>();
        meshRenderer.material = segmentMaterial;
        
        // 添加碰撞器
        if (useCompoundColliders)
        {
            MeshCollider meshCollider = segmentObj.AddComponent<MeshCollider>();
            meshCollider.convex = true;
            meshCollider.sharedMesh = layerMesh;
        }
        
        // 创建虚拟的CutResult用于兼容性
        LayerGeometricCutter.LayerCutResult cutResult = new LayerGeometricCutter.LayerCutResult
        {
            isValid = true,
            originalLayer = intersection.layer,
            volume = Mathf.PI * radius * radius * thickness,
            centerOfMass = intersection.CenterPoint,
            surfaceArea = 2 * Mathf.PI * radius * thickness,
            depthStart = intersection.entryDistance,
            depthEnd = intersection.exitDistance,
            resultMesh = layerMesh
        };
        
        LayerSegment segment = new LayerSegment
        {
            segmentObject = segmentObj,
            sourceLayer = intersection.layer,
            geometry = layerMesh,
            material = segmentMaterial,
            cutResult = cutResult,
            relativeDepth = intersection.entryDistance,
            localCenterOfMass = intersection.CenterPoint
        };
        
        return segment;
    }
    
    /// <summary>
    /// 创建圆柱体网格
    /// </summary>
    private Mesh CreateCylinderMesh(float radius, float height)
    {
        Mesh mesh = new Mesh();
        mesh.name = "CylinderMesh";
        
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        
        int segments = 12;
        float halfHeight = height * 0.5f;
        
        // 底面中心
        vertices.Add(Vector3.down * halfHeight);
        uvs.Add(new Vector2(0.5f, 0.5f));
        
        // 顶面中心
        vertices.Add(Vector3.up * halfHeight);
        uvs.Add(new Vector2(0.5f, 0.5f));
        
        // 圆周顶点
        for (int i = 0; i < segments; i++)
        {
            float angle = i * 2f * Mathf.PI / segments;
            Vector3 circlePoint = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            
            vertices.Add(circlePoint + Vector3.down * halfHeight); // 底面
            vertices.Add(circlePoint + Vector3.up * halfHeight); // 顶面
            
            uvs.Add(new Vector2((float)i / segments, 0));
            uvs.Add(new Vector2((float)i / segments, 1));
        }
        
        // 创建三角形
        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            
            // 底面
            triangles.Add(0);
            triangles.Add(2 + i * 2);
            triangles.Add(2 + next * 2);
            
            // 顶面
            triangles.Add(1);
            triangles.Add(2 + next * 2 + 1);
            triangles.Add(2 + i * 2 + 1);
            
            // 侧面
            triangles.Add(2 + i * 2);
            triangles.Add(2 + i * 2 + 1);
            triangles.Add(2 + next * 2);
            
            triangles.Add(2 + next * 2);
            triangles.Add(2 + i * 2 + 1);
            triangles.Add(2 + next * 2 + 1);
        }
        
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        return mesh;
    }
    
    /// <summary>
    /// 创建地层材质
    /// </summary>
    private Material CreateLayerMaterial(GeologyLayer layer, int segmentIndex)
    {
        Material material;
        
        // 获取地层的当前材质
        MeshRenderer meshRenderer = layer.GetComponent<MeshRenderer>();
        if (preserveOriginalMaterials && meshRenderer != null && meshRenderer.sharedMaterial != null)
        {
            material = new Material(meshRenderer.sharedMaterial);
            
            if (enhanceLayerContrast)
            {
                Color originalColor = material.color;
                Color.RGBToHSV(originalColor, out float h, out float s, out float v);
                
                float variation = (segmentIndex % 2 == 0) ? contrastFactor * 0.1f : -contrastFactor * 0.1f;
                v = Mathf.Clamp01(v + variation);
                
                Color enhancedColor = Color.HSVToRGB(h, s, v);
                enhancedColor.a = originalColor.a;
                
                material.color = enhancedColor;
            }
        }
        else
        {
            material = new Material(defaultLayerMaterial);
            Color baseColor = layer.layerColor;
            
            if (enhanceLayerContrast)
            {
                float variation = (segmentIndex % 2 == 0) ? contrastFactor : -contrastFactor;
                baseColor = Color.Lerp(baseColor, Color.white, variation);
            }
            
            material.color = baseColor;
            
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", 0.1f);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", 0.4f);
        }
        
        return material;
    }
    
    /// <summary>
    /// 设置样本物理属性
    /// </summary>
    private SamplePhysics SetupSamplePhysics(GameObject container, LayerSegment[] segments)
    {
        SamplePhysics physics = new SamplePhysics();
        
        if (enablePhysics)
        {
            Rigidbody rb = container.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = container.AddComponent<Rigidbody>();
            }
            
            float totalMass = CalculateTotalMass(segments);
            rb.mass = totalMass;
            rb.useGravity = false;
            rb.isKinematic = true;
            
            physics.rigidbody = rb;
            physics.totalMass = totalMass;
            physics.colliders = container.GetComponentsInChildren<Collider>();
        }
        
        return physics;
    }
    
    /// <summary>
    /// 设置样本显示效果
    /// </summary>
    private SampleDisplay SetupSampleDisplay(GameObject container, Vector3 position)
    {
        SampleDisplay display = new SampleDisplay
        {
            enableFloating = autoStartFloating,
            floatingHeight = defaultFloatingHeight,
            floatingAmplitude = floatingAmplitude,
            rotationSpeed = rotationSpeed,
            showLayerBoundaries = generateLayerBoundaries,
            boundaryMaterial = boundaryLineMaterial
        };
        
        if (autoStartFloating)
        {
            container.transform.position = position;
            
            GeometricSampleFloating floating = container.GetComponent<GeometricSampleFloating>();
            if (floating == null)
            {
                floating = container.AddComponent<GeometricSampleFloating>();
            }
            
            floating.floatingAmplitude = floatingAmplitude;
            floating.rotationSpeed = new Vector3(0, rotationSpeed, 0);
        }
        
        return display;
    }
    
    /// <summary>
    /// 设置样本组件
    /// </summary>
    private void SetupSampleComponents(ReconstructedSample sample)
    {
        GameObject container = sample.sampleContainer;
        
        GeometricSampleInfo info = container.GetComponent<GeometricSampleInfo>();
        if (info == null)
        {
            info = container.AddComponent<GeometricSampleInfo>();
        }
        info.Initialize(sample);
        
        GeometricSampleInteraction interaction = container.GetComponent<GeometricSampleInteraction>();
        if (interaction == null)
        {
            interaction = container.AddComponent<GeometricSampleInteraction>();
        }
        interaction.Initialize(sample);
    }
    
    /// <summary>
    /// 计算样本总高度
    /// </summary>
    private float CalculateTotalHeight(LayerSegment[] segments)
    {
        if (segments.Length == 0) return 0f;
        
        float minY = float.MaxValue;
        float maxY = float.MinValue;
        
        foreach (var segment in segments)
        {
            if (segment.geometry != null)
            {
                Bounds bounds = segment.geometry.bounds;
                Vector3 worldPos = segment.segmentObject.transform.position;
                minY = Mathf.Min(minY, worldPos.y + bounds.min.y);
                maxY = Mathf.Max(maxY, worldPos.y + bounds.max.y);
            }
        }
        
        return maxY - minY;
    }
    
    /// <summary>
    /// 计算样本总体积
    /// </summary>
    private float CalculateTotalVolume(LayerSegment[] segments)
    {
        float totalVolume = 0f;
        foreach (var segment in segments)
        {
            totalVolume += segment.cutResult.volume;
        }
        return totalVolume;
    }
    
    /// <summary>
    /// 计算样本质心
    /// </summary>
    private Vector3 CalculateCenterOfMass(LayerSegment[] segments)
    {
        if (segments.Length == 0) return Vector3.zero;
        
        Vector3 totalCenter = Vector3.zero;
        float totalVolume = 0f;
        
        foreach (var segment in segments)
        {
            float volume = segment.cutResult.volume;
            totalCenter += segment.localCenterOfMass * volume;
            totalVolume += volume;
        }
        
        return totalVolume > 0 ? totalCenter / totalVolume : Vector3.zero;
    }
    
    /// <summary>
    /// 计算样本总质量
    /// </summary>
    private float CalculateTotalMass(LayerSegment[] segments)
    {
        float totalMass = 0f;
        
        foreach (var segment in segments)
        {
            float density = GetLayerDensity(segment.sourceLayer.layerType);
            float mass = segment.cutResult.volume * density * densityMultiplier;
            totalMass += mass;
        }
        
        return Mathf.Max(totalMass, 0.1f);
    }
    
    private float GetLayerDensity(LayerType layerType)
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
    
    /// <summary>
    /// 获取所有活跃样本
    /// </summary>
    public ReconstructedSample[] GetActiveSamples()
    {
        activeSamples.RemoveAll(s => s == null || s.sampleContainer == null);
        return activeSamples.ToArray();
    }
    
    /// <summary>
    /// 移除样本
    /// </summary>
    public void RemoveSample(string sampleID)
    {
        for (int i = activeSamples.Count - 1; i >= 0; i--)
        {
            if (activeSamples[i].sampleID == sampleID)
            {
                if (activeSamples[i].sampleContainer != null)
                {
                    DestroyImmediate(activeSamples[i].sampleContainer);
                }
                activeSamples.RemoveAt(i);
                break;
            }
        }
    }
}
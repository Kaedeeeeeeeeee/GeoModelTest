using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 几何样本重建系统
/// 将几何切割的结果重建为完整的3D地质样本
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
    public float defaultFloatingHeight = 0.3f; // 降低默认悬浮高度
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
    
    [Header("调试")]
    public bool showDebugInfo = true;
    public bool visualizeGeometryBounds = false;
    
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
            Debug.LogWarning("未找到LayerGeometricCutter组件，将在需要时创建");
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
        
        Debug.Log("几何样本重建系统初始化完成");
    }
    
    /// <summary>
    /// 重建几何样本（异步版本的同步接口）
    /// </summary>
    public ReconstructedSample ReconstructSample(Vector3 drillingPoint, Vector3 direction, float radius, float depth, Vector3 displayPosition)
    {
        return ReconstructSample(drillingPoint, direction, radius, depth, displayPosition, 0f, depth);
    }
    
    /// <summary>
    /// 重建几何样本（支持指定深度范围，用于钻塔多层钻探）
    /// </summary>
    public ReconstructedSample ReconstructSample(Vector3 drillingPoint, Vector3 direction, float radius, float depth, Vector3 displayPosition, float depthStart, float depthEnd)
    {
        // 由于Unity主线程限制，这里使用同步版本
        return ReconstructSampleSync(drillingPoint, direction, radius, depth, displayPosition, depthStart, depthEnd);
    }
    
    /// <summary>
    /// 同步重建几何样本
    /// </summary>
    public ReconstructedSample ReconstructSampleSync(Vector3 drillingPoint, Vector3 direction, float radius, float depth, Vector3 displayPosition)
    {
        return ReconstructSampleSync(drillingPoint, direction, radius, depth, displayPosition, 0f, depth);
    }
    
    /// <summary>
    /// 同步重建几何样本（支持指定深度范围）
    /// </summary>
    public ReconstructedSample ReconstructSampleSync(Vector3 drillingPoint, Vector3 direction, float radius, float depth, Vector3 displayPosition, float depthStart, float depthEnd)
    {
        // 开始重建几何样本
        
        try
        {
            // 第1步：获取几何切割器
            if (geometricCutter == null)
            {
                geometricCutter = FindFirstObjectByType<LayerGeometricCutter>();
                if (geometricCutter == null)
                {
                    GameObject cutterObj = new GameObject("LayerGeometricCutter");
                    geometricCutter = cutterObj.AddComponent<LayerGeometricCutter>();
                }
            }
            
            // 第2步：执行几何切割（同步版本）
            var geometricData = CreateGeometricSampleSync(drillingPoint, direction, radius, depth, depthStart, depthEnd);
            
            if (geometricData.layerResults == null || geometricData.layerResults.Length == 0)
            {
                Debug.LogWarning("几何切割未产生有效结果");
                return null;
            }
            
            // 第3步：创建样本容器
            GameObject sampleContainer = CreateSampleContainer(geometricData, displayPosition);
            
            // 第4步：重建地层段
            LayerSegment[] layerSegments = ReconstructLayerSegments(geometricData.layerResults, sampleContainer.transform);
            
            // 第5步：设置物理属性
            SamplePhysics physics = SetupSamplePhysics(sampleContainer, layerSegments, geometricData);
            
            // 第6步：设置显示效果
            SampleDisplay display = SetupSampleDisplay(sampleContainer, displayPosition);
            
            // 第7步：创建重建样本对象
            ReconstructedSample sample = new ReconstructedSample
            {
                sampleID = geometricData.sampleID,
                sampleContainer = sampleContainer,
                layerSegments = layerSegments,
                physics = physics,
                display = display,
                originalData = geometricData,
                totalHeight = CalculateTotalHeight(layerSegments),
                totalVolume = geometricData.totalVolume,
                centerOfMass = CalculateCenterOfMass(layerSegments)
            };
            
            // 第8步：添加样本组件
            SetupSampleComponents(sample);
            
            activeSamples.Add(sample);
            
            Debug.Log("几何样本重建完成 - 地层段数: " + layerSegments.Length);
            
            return sample;
        }
        catch (System.Exception e)
        {
            Debug.LogError("重建几何样本时发生错误: " + e.Message + "\n" + e.StackTrace);
            return null;
        }
    }
    
    /// <summary>
    /// 同步创建几何样本数据
    /// </summary>
    private LayerGeometricCutter.GeometricSampleData CreateGeometricSampleSync(Vector3 drillingPoint, Vector3 direction, float radius, float depth)
    {
        // 直接调用同步版本避免卡死
        return CreateGeometricSampleDirect(drillingPoint, direction, radius, depth, 0f, depth);
    }
    
    /// <summary>
    /// 同步创建几何样本数据（支持深度范围）
    /// </summary>
    private LayerGeometricCutter.GeometricSampleData CreateGeometricSampleSync(Vector3 drillingPoint, Vector3 direction, float radius, float depth, float depthStart, float depthEnd)
    {
        // 直接调用同步版本避免卡死
        return CreateGeometricSampleDirect(drillingPoint, direction, radius, depth, depthStart, depthEnd);
    }
    
    /// <summary>
    /// 直接创建几何样本数据（同步版本）
    /// </summary>
    private LayerGeometricCutter.GeometricSampleData CreateGeometricSampleDirect(Vector3 drillingPoint, Vector3 direction, float radius, float depth)
    {
        return CreateGeometricSampleDirect(drillingPoint, direction, radius, depth, 0f, depth);
    }
    
    /// <summary>
    /// 直接创建几何样本数据（支持深度范围）
    /// </summary>
    private LayerGeometricCutter.GeometricSampleData CreateGeometricSampleDirect(Vector3 drillingPoint, Vector3 direction, float radius, float depth, float depthStart, float depthEnd)
    {
        LayerGeometricCutter.GeometricSampleData sampleData = new LayerGeometricCutter.GeometricSampleData
        {
            sampleID = System.Guid.NewGuid().ToString(),
            drillingPosition = drillingPoint,
            drillingDirection = direction.normalized,
            drillingRadius = radius,
            drillingDepth = depth,
            collectionTime = System.DateTime.Now
        };
        
        // 开始同步几何切割
        
        try
        {
            // 获取钻探范围内的地层
            DrillingCylinderGenerator cylinderGen = geometricCutter.GetComponent<DrillingCylinderGenerator>();
            if (cylinderGen == null)
            {
                cylinderGen = geometricCutter.gameObject.AddComponent<DrillingCylinderGenerator>();
            }
            
            GeologyLayer[] layersInRange = cylinderGen.GetLayersInDrillingRange(drillingPoint, direction, depth + radius);
            // 找到地层在钻探范围内
            
            if (layersInRange.Length == 0)
            {
                Debug.LogWarning("钻探范围内没有找到地层");
                return sampleData;
            }
            
            // 创建真实的切割结果，基于实际地层厚度和位置
            List<LayerGeometricCutter.LayerCutResult> cutResults = new List<LayerGeometricCutter.LayerCutResult>();
            
            // 使用全局射线检测获取地层切换序列，传递深度范围
            var layerIntervals = AnalyzeGlobalLayerIntersections(layersInRange, drillingPoint, direction, depthStart, depthEnd);
            
            for (int i = 0; i < layerIntervals.Count; i++)
            {
                var interval = layerIntervals[i];
                GeologyLayer layer = interval.layer;
                
                // 使用全局分析的精确深度信息
                float layerDepthStart = interval.startDepth;
                float layerDepthEnd = interval.endDepth;
                float actualThickness = layerDepthEnd - layerDepthStart;
                
                // 地层厚度和深度计算完成
                
                LayerGeometricCutter.LayerCutResult result = new LayerGeometricCutter.LayerCutResult
                {
                    isValid = true,
                    originalLayer = layer,
                    volume = radius * radius * Mathf.PI * actualThickness,
                    centerOfMass = drillingPoint + direction * (layerDepthStart + actualThickness * 0.5f),
                    surfaceArea = 2 * Mathf.PI * radius * actualThickness,
                    depthStart = layerDepthStart,
                    depthEnd = layerDepthEnd,
                    resultMesh = CreateVerticalLayerMesh(radius, actualThickness),
                    features = new LayerGeometricCutter.GeologicalFeatures
                    {
                        averageDip = layer.dipAngle,
                        dipDirection = layer.strikeDirection,
                        surfaceRoughness = 0.1f,
                        thicknessVariation = 0.05f,
                        foldPoints = new List<Vector3>(),
                        faultLines = new List<Vector3>()
                    }
                };
                
                cutResults.Add(result);
            }
            
            sampleData.layerResults = cutResults.ToArray();
            sampleData.totalVolume = cutResults.Sum(r => r.volume);
            
            Debug.Log("同步几何切割完成 - 有效地层: " + sampleData.layerResults.Length);
            
            return sampleData;
        }
        catch (System.Exception e)
        {
            Debug.LogError("同步几何切割失败: " + e.Message);
            return sampleData;
        }
    }
    
    /// <summary>
    /// 创建简化的地层网格
    /// </summary>
    private Mesh CreateSimplifiedLayerMesh(float radius, float height)
    {
        Mesh mesh = new Mesh();
        mesh.name = "SimplifiedLayerMesh";
        
        // 创建简单圆柱体网格 - 修正：以Y=0为中心，上下对称分布
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        
        int segments = 12;
        float halfHeight = height * 0.5f; // 使用一半高度来实现对称分布
        
        // 底面中心（向下偏移一半高度）
        vertices.Add(Vector3.down * halfHeight);
        uvs.Add(new Vector2(0.5f, 0.5f));
        
        // 顶面中心（向上偏移一半高度）  
        vertices.Add(Vector3.up * halfHeight);
        uvs.Add(new Vector2(0.5f, 0.5f));
        
        // 圆周顶点
        for (int i = 0; i < segments; i++)
        {
            float angle = i * 2f * Mathf.PI / segments;
            Vector3 circlePoint = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            
            vertices.Add(circlePoint + Vector3.down * halfHeight); // 底面（向下偏移）
            vertices.Add(circlePoint + Vector3.up * halfHeight); // 顶面（向上偏移）
            
            uvs.Add(new Vector2((float)i / segments, 0));
            uvs.Add(new Vector2((float)i / segments, 1));
        }
        
        // 简化的三角形
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
    /// 地层深度信息结构
    /// </summary>
    private struct LayerDepthInfo
    {
        public GeologyLayer layer;
        public float depthStart;
        public float depthEnd;
        public float thickness;
        public float realThickness;
    }
    
    /// <summary>
    /// 地层区间信息结构（全局分析结果）
    /// </summary>
    private struct LayerInterval
    {
        public GeologyLayer layer;
        public float startDepth;
        public float endDepth;
        public Vector3 startPoint;
        public Vector3 endPoint;
        public bool isValid;
    }
    
    /// <summary>
    /// 射线击中点信息
    /// </summary>
    private struct RayHit
    {
        public GeologyLayer layer;
        public Vector3 point;
        public float distance;
        public Vector3 normal;
        public bool isEntering; // true=进入地层, false=离开地层
    }
    
    /// <summary>
    /// 按深度排序地层并计算真实厚度
    /// </summary>
    private LayerDepthInfo[] SortLayersByDepth(GeologyLayer[] layers, Vector3 drillingPoint, Vector3 direction)
    {
        List<LayerDepthInfo> layerInfos = new List<LayerDepthInfo>();
        
        foreach (GeologyLayer layer in layers)
        {
            // 计算地层在钻探路径上的深度范围
            var depthRange = CalculateLayerDepthRange(layer, drillingPoint, direction);
            
            // 检查是否为有效的深度范围（修正后：-1f表示无效）
            if (depthRange.x >= 0f && depthRange.y > depthRange.x)
            {
                LayerDepthInfo info = new LayerDepthInfo
                {
                    layer = layer,
                    depthStart = depthRange.x,
                    depthEnd = depthRange.y,
                    thickness = depthRange.y - depthRange.x,
                    realThickness = CalculateRealLayerThickness(layer, drillingPoint, direction, depthRange.y - depthRange.x)
                };
                layerInfos.Add(info);
                // 有效地层已添加
            }
            else
            {
                // 跳过无效地层
            }
        }
        
        // 按深度开始位置排序（从地表到深层）
        layerInfos.Sort((a, b) => a.depthStart.CompareTo(b.depthStart));
        
        // 智能地层分布算法 - 保持真实厚度比例
        layerInfos = DistributeLayersProportionally(layerInfos, drillingPoint, direction);
        
        Debug.Log($"🔄 地层排序完成: 有效地层数 {layerInfos.Count}");
        
        return layerInfos.ToArray();
    }
    
    /// <summary>
    /// 智能地层分布算法 - 基于真实位置的地层分布
    /// </summary>
    private List<LayerDepthInfo> DistributeLayersProportionally(List<LayerDepthInfo> layers, Vector3 drillingPoint, Vector3 direction)
    {
        if (layers.Count == 0) 
        {
            Debug.LogWarning("⚠️ 没有有效地层进行分布");
            return layers;
        }
        
        // 获取钻探深度
        BoringTool boringTool = FindFirstObjectByType<BoringTool>();
        float maxDrillingDepth = boringTool?.boringDepth ?? 2.0f;
        
        // 开始位置特异性地层分布
        
        // 直接使用已计算好的深度范围，不再重新分配
        List<LayerDepthInfo> distributedLayers = new List<LayerDepthInfo>();
        
        foreach (var layer in layers)
        {
            // 检查地层是否在钻探深度范围内
            if (layer.depthStart >= maxDrillingDepth)
            {
                Debug.Log($"⚠️ 地层 {layer.layer.layerName} 起始深度 {layer.depthStart:F3}m 超出钻探深度 {maxDrillingDepth:F2}m，跳过");
                continue;
            }
            
            // 调整地层范围以适应钻探深度
            float adjustedDepthStart = layer.depthStart;
            float adjustedDepthEnd = Mathf.Min(layer.depthEnd, maxDrillingDepth);
            float adjustedThickness = adjustedDepthEnd - adjustedDepthStart;
            
            if (adjustedThickness <= 0.001f)
            {
                Debug.Log($"⚠️ 地层 {layer.layer.layerName} 调整后厚度 {adjustedThickness:F3}m 过薄，跳过");
                continue;
            }
            
            LayerDepthInfo distributedLayer = new LayerDepthInfo
            {
                layer = layer.layer,
                depthStart = adjustedDepthStart,
                depthEnd = adjustedDepthEnd,
                thickness = adjustedThickness,
                realThickness = layer.realThickness
            };
            
            distributedLayers.Add(distributedLayer);
            
            // 保持地层已添加
        }
        
        Debug.Log($"🎯 位置特异性分布完成: 有效地层数 {distributedLayers.Count}");
        
        return distributedLayers;
    }
    
    /// <summary>
    /// 计算地层在钻探路径上的深度范围 - 修复版：基于真实位置的地层分布
    /// </summary>
    private Vector2 CalculateLayerDepthRange(GeologyLayer layer, Vector3 drillingPoint, Vector3 direction)
    {
        // 开始位置特异性深度计算
        
        // 第1步：检查钻探点是否真正在该地层的水平投影范围内
        if (!IsPointInLayerHorizontalBounds(drillingPoint, layer))
        {
            return new Vector2(-1f, -1f);
        }
        
        // 第2步：使用精确的射线检测计算地层交点
        var intersections = CalculateRayLayerIntersections(drillingPoint, direction, layer);
        
        // 特殊处理：如果只有一个交点且钻探点在地表，则计算从地表到地层底部的距离
        if (intersections.Count == 1)
        {
            // 检查钻探点是否在地层表面附近
            Vector3 surfacePoint = intersections[0];
            float distanceToSurface = Vector3.Distance(drillingPoint, surfacePoint);
            
            if (distanceToSurface < 0.5f) // 在地表附近
            {
                // 估算地层厚度作为第二个交点
                Bounds layerBounds = GetLayerBounds(layer);
                float estimatedThickness = layerBounds.size.y;
                
                // 添加第二个交点（地层底部）
                Vector3 bottomPoint = drillingPoint + direction * estimatedThickness;
                intersections.Add(bottomPoint);
                
                // 地表钻探修复
            }
            else
            {
                // 地层射线交点不足且不在地表，跳过
                return new Vector2(-1f, -1f);
            }
        }
        else if (intersections.Count < 1)
        {
            // 地层无射线交点，跳过
            return new Vector2(-1f, -1f);
        }
        
        // 第3步：计算沿钻探方向的深度
        float depthToTop = Vector3.Dot(intersections[0] - drillingPoint, direction);
        float depthToBottom = Vector3.Dot(intersections[1] - drillingPoint, direction);
        
        // 确保深度顺序正确
        if (depthToTop > depthToBottom)
        {
            float temp = depthToTop;
            depthToTop = depthToBottom;
            depthToBottom = temp;
        }
        
        // 第4步：限制在有效钻探范围内（使用传递的深度参数）
        // 注意：这里需要获取传递的深度参数，但当前方法签名没有这些参数
        // 临时使用更大的默认深度以支持钻塔深层钻探
        float maxDrillingDepth = 10.0f; // 支持钻塔的最大深度
        
        depthToTop = Mathf.Max(0f, depthToTop);
        depthToBottom = Mathf.Min(maxDrillingDepth, depthToBottom);
        
        if (depthToBottom <= depthToTop)
        {
            Debug.Log($"❌ 地层 {layer.layerName} 在钻探深度范围外，跳过");
            return new Vector2(-1f, -1f);
        }
        
        float actualThickness = depthToBottom - depthToTop;
        // 地层位置特异性深度计算完成
        
        return new Vector2(depthToTop, depthToBottom);
    }
    
    /// <summary>
    /// 全局射线检测，分析所有地层的交点序列
    /// </summary>
    private List<LayerInterval> AnalyzeGlobalLayerIntersections(GeologyLayer[] layers, Vector3 drillingPoint, Vector3 direction)
    {
        return AnalyzeGlobalLayerIntersections(layers, drillingPoint, direction, 0f, 2.0f);
    }
    
    /// <summary>
    /// 全局射线检测，分析所有地层的交点序列（支持深度范围）
    /// </summary>
    private List<LayerInterval> AnalyzeGlobalLayerIntersections(GeologyLayer[] layers, Vector3 drillingPoint, Vector3 direction, float depthStart, float depthEnd)
    {
        Debug.Log($"🌍 开始全局射线检测: 地层数 {layers.Length}, 深度范围 {depthStart:F1}m-{depthEnd:F1}m");
        
        // 第1步：收集所有击中点
        List<RayHit> allHits = CollectAllRayHits(layers, drillingPoint, direction, depthEnd);
        
        // 第2步：按距离排序
        allHits.Sort((a, b) => a.distance.CompareTo(b.distance));
        
        Debug.Log($"📊 收集到 {allHits.Count} 个击中点");
        
        // 击中点范围分析
        if (allHits.Count == 0)
        {
            Debug.LogWarning($"⚠️ 警告: 没有击中任何地层，深度范围 {depthStart:F1}m - {depthEnd:F1}m");
        }
        
        // 第3步：分析地层切换序列（传递深度偏移用于正确的深度计算）
        List<LayerInterval> intervals = AnalyzeLayerSequence(allHits, drillingPoint, direction, depthStart);
        
        Debug.Log($"📈 生成 {intervals.Count} 个地层区间");
        
        // 第4步：过滤并调整深度范围以匹配钻塔的特定深度范围
        List<LayerInterval> filteredIntervals = new List<LayerInterval>();
        
        // 开始深度范围过滤
        
        foreach (var interval in intervals)
        {
            // 检查地层是否与钻探深度范围有交集
            bool hasIntersection = interval.endDepth > depthStart && interval.startDepth < depthEnd;
            
            if (hasIntersection)
            {
                // 调整地层深度范围以适应钻探范围
                float adjustedStart = Mathf.Max(interval.startDepth, depthStart);
                float adjustedEnd = Mathf.Min(interval.endDepth, depthEnd);
                
                LayerInterval adjustedInterval = new LayerInterval
                {
                    layer = interval.layer,
                    startDepth = adjustedStart,
                    endDepth = adjustedEnd
                };
                
                // 只保留有有效厚度的地层
                if (adjustedInterval.endDepth > adjustedInterval.startDepth)
                {
                    filteredIntervals.Add(adjustedInterval);
                }
            }
        }
        
        Debug.Log($"🎯 过滤后保留 {filteredIntervals.Count} 个地层区间，深度范围 {depthStart:F1}m-{depthEnd:F1}m");
        
        return filteredIntervals;
    }
    
    /// <summary>
    /// 收集所有地层的射线击中点
    /// </summary>
    private List<RayHit> CollectAllRayHits(GeologyLayer[] layers, Vector3 startPoint, Vector3 direction)
    {
        // 获取钻探深度
        BoringTool boringTool = FindFirstObjectByType<BoringTool>();
        float maxDistance = boringTool?.boringDepth ?? 2.0f;
        return CollectAllRayHits(layers, startPoint, direction, maxDistance);
    }
    
    /// <summary>
    /// 收集所有地层的射线击中点（支持指定深度）
    /// </summary>
    private List<RayHit> CollectAllRayHits(GeologyLayer[] layers, Vector3 startPoint, Vector3 direction, float maxDistance)
    {
        List<RayHit> hits = new List<RayHit>();
        
        // 一次性对所有地层进行射线检测
        Ray ray = new Ray(startPoint, direction);
        RaycastHit[] allHits = Physics.RaycastAll(ray, maxDistance + 1f);
        
        foreach (var hit in allHits)
        {
            // 查找击中的地层
            GeologyLayer hitLayer = hit.collider.GetComponent<GeologyLayer>();
            if (hitLayer != null && System.Array.IndexOf(layers, hitLayer) >= 0)
            {
                RayHit rayHit = new RayHit
                {
                    layer = hitLayer,
                    point = hit.point,
                    distance = hit.distance,
                    normal = hit.normal,
                    isEntering = Vector3.Dot(direction, hit.normal) < 0 // 法线与方向相反表示进入
                };
                hits.Add(rayHit);
            }
        }
        
        return hits;
    }
    
    /// <summary>
    /// 分析击中点序列，生成地层区间
    /// </summary>
    private List<LayerInterval> AnalyzeLayerSequence(List<RayHit> hits, Vector3 startPoint, Vector3 direction, float depthOffset = 0f)
    {
        List<LayerInterval> intervals = new List<LayerInterval>();
        
        // 开始地层序列分析
        
        if (hits.Count == 0)
        {
            Debug.LogWarning("⚠️ 无击中点，无法生成地层区间");
            return intervals;
        }
        
        // 从钻探起点开始分析
        float currentDepth = 0f;
        GeologyLayer currentLayer = null;
        
        // 检查起始点处的地层
        GeologyLayer startingLayer = GetLayerAtPoint(startPoint);
        if (startingLayer != null)
        {
            currentLayer = startingLayer;
            // 起始地层已确定
            
            // 🔧 修复：如果从地层内部开始，需要检查是否有对应的离开事件
            // 如果第一个击中点是该地层的离开事件，说明我们从地层内部开始
            if (hits.Count > 0 && hits[0].layer == startingLayer && !hits[0].isEntering)
            {
                // 检测到从地层内部开始钻探
            }
        }
        
        for (int i = 0; i < hits.Count; i++)
        {
            var hit = hits[i];
            
            // 🔧 改进的地层切换逻辑：处理从地层内部开始的情况
            if (hit.isEntering)
            {
                // 进入新地层
                if (currentLayer != null && hit.layer != currentLayer)
                {
                    // 结束当前地层区间（加上深度偏移）
                    LayerInterval interval = new LayerInterval
                    {
                        layer = currentLayer,
                        startDepth = currentDepth + depthOffset,
                        endDepth = hit.distance + depthOffset,
                        startPoint = startPoint + direction * currentDepth,
                        endPoint = hit.point,
                        isValid = true
                    };
                    intervals.Add(interval);
                    
                    // 地层结束
                    currentDepth = hit.distance;
                }
                
                currentLayer = hit.layer;
                // 进入地层
            }
            else if (currentLayer == hit.layer)
            {
                // 离开当前地层（加上深度偏移）
                LayerInterval interval = new LayerInterval
                {
                    layer = currentLayer,
                    startDepth = currentDepth + depthOffset,
                    endDepth = hit.distance + depthOffset,
                    startPoint = startPoint + direction * currentDepth,
                    endPoint = hit.point,
                    isValid = true
                };
                intervals.Add(interval);
                
                // 离开地层
                
                currentLayer = null;
                currentDepth = hit.distance;
            }
        }
        
        // 处理最后一段
        if (currentLayer != null)
        {
            BoringTool boringTool = FindFirstObjectByType<BoringTool>();
            float maxDepth = boringTool?.boringDepth ?? 2.0f;
            
            LayerInterval finalInterval = new LayerInterval
            {
                layer = currentLayer,
                startDepth = currentDepth + depthOffset,
                endDepth = maxDepth + depthOffset,
                startPoint = startPoint + direction * currentDepth,
                endPoint = startPoint + direction * maxDepth,
                isValid = true
            };
            intervals.Add(finalInterval);
            
            // 最后一区间
        }
        
        return intervals;
    }
    
    /// <summary>
    /// 获取指定点处的地层
    /// </summary>
    private GeologyLayer GetLayerAtPoint(Vector3 point)
    {
        // 使用球形检测查找当前点处的地层
        Collider[] colliders = Physics.OverlapSphere(point, 0.1f);
        foreach (var collider in colliders)
        {
            GeologyLayer layer = collider.GetComponent<GeologyLayer>();
            if (layer != null)
            {
                return layer;
            }
        }
        return null;
    }
    
    /// <summary>
    /// 检查钻探点是否在地层的水平投影范围内
    /// </summary>
    private bool IsPointInLayerHorizontalBounds(Vector3 point, GeologyLayer layer)
    {
        Bounds layerBounds = GetLayerBounds(layer);
        
        // 检查XZ平面投影
        bool inX = point.x >= layerBounds.min.x && point.x <= layerBounds.max.x;
        bool inZ = point.z >= layerBounds.min.z && point.z <= layerBounds.max.z;
        
        bool inHorizontalBounds = inX && inZ;
        // 水平边界检查
        
        return inHorizontalBounds;
    }
    
    /// <summary>
    /// 计算射线与地层的精确交点
    /// </summary>
    private List<Vector3> CalculateRayLayerIntersections(Vector3 startPoint, Vector3 direction, GeologyLayer layer)
    {
        List<Vector3> intersections = new List<Vector3>();
        
        // 方法1：检查是否从地层内部开始钻探
        MeshCollider meshCollider = layer.GetComponent<MeshCollider>();
        if (meshCollider != null)
        {
            // 检查起始点是否在地层内部
            Vector3 closestPoint = meshCollider.ClosestPoint(startPoint);
            float distanceToSurface = Vector3.Distance(startPoint, closestPoint);
            
            if (distanceToSurface < 0.1f) // 在地层表面附近
            {
                // 添加起始点作为第一个交点（地表点）
                intersections.Add(startPoint);
                // 地表起始点
            }
            
            // 然后进行正常的射线检测
            Ray ray = new Ray(startPoint, direction);
            RaycastHit[] hits = Physics.RaycastAll(ray, 10f);
            
            foreach (var hit in hits)
            {
                if (hit.collider == meshCollider && hit.distance > 0.01f) // 忽略起始点附近的击中
                {
                    intersections.Add(hit.point);
                    // 射线交点
                }
            }
        }
        
        // 方法2：回退到边界框交点计算
        if (intersections.Count == 0)
        {
            intersections = CalculateBoundsIntersections(startPoint, direction, layer);
        }
        
        // 按距离排序
        intersections.Sort((a, b) => Vector3.Distance(startPoint, a).CompareTo(Vector3.Distance(startPoint, b)));
        
        // 地层射线交点完成
        return intersections;
    }
    
    /// <summary>
    /// 使用边界框计算交点（回退方案）
    /// </summary>
    private List<Vector3> CalculateBoundsIntersections(Vector3 startPoint, Vector3 direction, GeologyLayer layer)
    {
        List<Vector3> intersections = new List<Vector3>();
        Bounds bounds = GetLayerBounds(layer);
        
        Ray ray = new Ray(startPoint, direction);
        if (bounds.IntersectRay(ray, out float distance))
        {
            Vector3 enterPoint = ray.GetPoint(distance);
            
            // 计算退出点（简化计算）
            float thickness = bounds.size.y;
            Vector3 exitPoint = enterPoint + direction * thickness;
            
            intersections.Add(enterPoint);
            intersections.Add(exitPoint);
            
            // 边界框交点
        }
        
        return intersections;
    }
    
    /// <summary>
    /// 计算真实地层厚度（考虑倾斜）
    /// </summary>
    private float CalculateRealLayerThickness(GeologyLayer layer, Vector3 drillingPoint, Vector3 direction, float apparentThickness)
    {
        // 考虑地层倾角对厚度的影响
        float dipRadians = layer.dipAngle * Mathf.Deg2Rad;
        float realThickness = apparentThickness * Mathf.Cos(dipRadians);
        
        // 确保厚度不会太小
        return Mathf.Max(realThickness, 0.05f);
    }
    
    private Bounds GetLayerBounds(GeologyLayer layer)
    {
        MeshRenderer renderer = layer.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            return renderer.bounds;
        }
        
        // 回退方案
        return new Bounds(layer.transform.position, layer.transform.localScale);
    }
    
    /// <summary>
    /// 处理地层形状以适应样本显示
    /// </summary>
    private Mesh ProcessLayerShapeForSample(Mesh originalMesh, float targetThickness, float targetRadius)
    {
        if (originalMesh == null) return CreateVerticalLayerMesh(targetRadius, targetThickness);
        
        // 获取原始网格的边界
        Bounds originalBounds = originalMesh.bounds;
        
        // 计算缩放比例以适应目标尺寸
        float scaleX = (targetRadius * 2f) / Mathf.Max(originalBounds.size.x, 0.001f);
        float scaleY = targetThickness / Mathf.Max(originalBounds.size.y, 0.001f);
        float scaleZ = (targetRadius * 2f) / Mathf.Max(originalBounds.size.z, 0.001f);
        
        Vector3 scale = new Vector3(scaleX, scaleY, scaleZ);
        
        // 创建处理后的网格
        Mesh processedMesh = new Mesh();
        processedMesh.name = originalMesh.name + "_Processed";
        
        // 缩放顶点
        Vector3[] originalVertices = originalMesh.vertices;
        Vector3[] processedVertices = new Vector3[originalVertices.Length];
        
        for (int i = 0; i < originalVertices.Length; i++)
        {
            // 以原始中心为基准进行缩放
            Vector3 localVertex = originalVertices[i] - originalBounds.center;
            localVertex = Vector3.Scale(localVertex, scale);
            processedVertices[i] = localVertex; // 不加回中心点，因为样本段有自己的位置
        }
        
        processedMesh.vertices = processedVertices;
        processedMesh.triangles = originalMesh.triangles;
        processedMesh.uv = originalMesh.uv;
        processedMesh.normals = originalMesh.normals;
        
        // 重新计算属性
        processedMesh.RecalculateBounds();
        processedMesh.RecalculateNormals();
        processedMesh.RecalculateTangents();
        
        // 地层形状处理完成
        
        return processedMesh;
    }
    
    /// <summary>
    /// 创建垂直的地层网格（地层倾斜通过材质纹理体现，而非几何体倾斜）
    /// </summary>
    private Mesh CreateVerticalLayerMesh(float radius, float height)
    {
        // 直接使用已有的简化圆柱体方法，保持垂直
        return CreateSimplifiedLayerMesh(radius, height);
    }
    
    /// <summary>
    /// 创建样本容器
    /// </summary>
    private GameObject CreateSampleContainer(LayerGeometricCutter.GeometricSampleData data, Vector3 position)
    {
        GameObject container = new GameObject("GeometricSample_" + data.sampleID.Substring(0, 8));
        container.transform.position = position;
        
        // 添加标识标签
        if (!HasTag("GeologicalSample"))
        {
            Debug.LogWarning("GeologicalSample标签未定义，跳过标签设置");
        }
        else
        {
            container.tag = "GeologicalSample";
        }
        
        return container;
    }
    
    private bool HasTag(string tag)
    {
        try
        {
            GameObject.FindWithTag(tag);
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// 重建地层段 - 真实钻探版本（按实际钻探深度切割地层）
    /// </summary>
    private LayerSegment[] ReconstructLayerSegments(LayerGeometricCutter.LayerCutResult[] cutResults, Transform parent)
    {
        List<LayerSegment> segments = new List<LayerSegment>();
        
        // 获取钻探深度 - 支持钻塔深层钻探
        float drillingDepth = 10.0f; // 默认支持钻塔的最大深度
        
        // 如果是普通钻探工具，使用其深度限制
        BoringTool boringTool = FindFirstObjectByType<BoringTool>();
        if (boringTool != null && cutResults.Length > 0)
        {
            // 检查是否是钻塔系统的调用（通过深度范围判断）
            bool isDrillTowerCall = false;
            foreach (var result in cutResults)
            {
                if (result.depthEnd > 2.1f) // 如果有地层深度超过2.1米，很可能是钻塔调用
                {
                    isDrillTowerCall = true;
                    break;
                }
            }
            
            if (!isDrillTowerCall)
            {
                drillingDepth = boringTool.boringDepth; // 只有普通钻探才使用2米限制
            }
        }
        
        Debug.Log($"🔍 开始真实钻探重建: 钻探深度 {drillingDepth:F2}m, 地层数 {cutResults.Length}");
        
        // 计算总的地层厚度和深度范围
        float totalLayerThickness = 0f;
        float minDepth = float.MaxValue;
        float maxDepth = float.MinValue;
        
        foreach (var result in cutResults)
        {
            if (result.isValid)
            {
                float layerThickness = result.depthEnd - result.depthStart;
                totalLayerThickness += layerThickness;
                minDepth = Mathf.Min(minDepth, result.depthStart);
                maxDepth = Mathf.Max(maxDepth, result.depthEnd);
                Debug.Log($"📊 地层 {result.originalLayer.layerName}: 深度 {result.depthStart:F3}m-{result.depthEnd:F3}m, 厚度 {layerThickness:F3}m");
            }
        }
        
        float actualDepthRange = maxDepth - minDepth;
        Debug.Log($"📏 深度统计: 总厚度 {totalLayerThickness:F3}m, 深度范围 {minDepth:F3}m-{maxDepth:F3}m (范围 {actualDepthRange:F3}m)");
        
        float currentDepth = 0f; // 当前钻探深度（从地面开始）
        const float safeGap = 0.005f; // 非常小的安全间距，保持紧密拼接（0.5cm）
        
        for (int i = 0; i < cutResults.Length; i++)
        {
            var cutResult = cutResults[i];
            if (!cutResult.isValid) continue;
            
            // 使用智能分布算法计算的厚度（已经过比例调整）
            float originalLayerThickness = cutResult.depthEnd - cutResult.depthStart;
            
            // 对于钻塔系统，确保厚度按实际比例分配（样本总高度通常是2米）
            float targetSampleHeight = 2.0f; // 样本的标准高度
            float layerThickness;
            
            if (totalLayerThickness > 0)
            {
                // 按比例分配厚度，保持相对比例正确
                float thicknessRatio = originalLayerThickness / totalLayerThickness;
                layerThickness = thicknessRatio * targetSampleHeight;
                
                Debug.Log($"📏 地层 {cutResult.originalLayer.layerName}:");
                Debug.Log($"   原始厚度: {originalLayerThickness:F3}m");
                Debug.Log($"   厚度比例: {thicknessRatio:F3} ({thicknessRatio*100:F1}%)"); 
                Debug.Log($"   样本厚度: {layerThickness:F3}m");
            }
            else
            {
                layerThickness = originalLayerThickness;
                Debug.Log($"📏 地层 {cutResult.originalLayer.layerName} 使用原始厚度: {layerThickness:F3}m");
            }
            
            // 对于钻塔系统，不进行深度截断，保持所有地层的比例
            float actualThickness = layerThickness;
            
            Debug.Log($"✅ 地层 {cutResult.originalLayer.layerName} 最终厚度: {actualThickness:F3}m");
            
            // 计算地层段在样本中的位置（Y坐标系，负值向下）
            // 确保每个地层段之间有清晰的分离
            float segmentCenter = -(currentDepth + actualThickness * 0.5f);
            
            Debug.Log($"🔧 计算地层段 {i} 中心位置: currentDepth={currentDepth:F3}m, actualThickness={actualThickness:F3}m, segmentCenter={segmentCenter:F3}m");
            
            LayerSegment segment = CreateLayerSegment(cutResult, i, parent, segmentCenter, actualThickness);
            if (segment != null)
            {
                segments.Add(segment);
                
                float segmentTop = segmentCenter + actualThickness * 0.5f;
                float segmentBottom = segmentCenter - actualThickness * 0.5f;
                
                Debug.Log($"🪨 真实钻探地层段 {i} ({cutResult.originalLayer.layerName}): 深度 {currentDepth:F3}m-{currentDepth + actualThickness:F3}m, 厚度 {actualThickness:F3}m, 样本位置 [{segmentTop:F3}m 到 {segmentBottom:F3}m]");
                
                // 验证没有重叠（改进的重叠检测）- 在更新currentDepth之前检查
                if (segments.Count > 1)
                {
                    var prevSegment = segments[segments.Count - 2];
                    var prevCutResult = prevSegment.cutResult;
                    float prevThickness = prevCutResult.depthEnd - prevCutResult.depthStart;
                    prevThickness = Mathf.Max(prevThickness, 0.01f); // 与前面逻辑保持一致
                    
                    float prevCenter = prevSegment.segmentObject.transform.localPosition.y;
                    float prevBottom = prevCenter - prevThickness * 0.5f;
                    float currentTop = segmentTop;
                    
                    if (currentTop > prevBottom - 0.0001f) // 检测真正的重叠
                    {
                        Debug.LogWarning($"⚠️ 检测到地层段重叠: 前一段 {prevSegment.sourceLayer.layerName} 底部 {prevBottom:F3}m, 当前段 {cutResult.originalLayer.layerName} 顶部 {currentTop:F3}m");
                        // 紧密拼接：将当前段移到前一段正下方，只加极小间距
                        float newSegmentCenter = prevBottom - actualThickness * 0.5f - safeGap;
                        segmentCenter = newSegmentCenter;
                        segment.segmentObject.transform.localPosition = new Vector3(0, segmentCenter, 0);
                        
                        // 重新计算段的顶部和底部
                        segmentTop = segmentCenter + actualThickness * 0.5f;
                        segmentBottom = segmentCenter - actualThickness * 0.5f;
                        
                        Debug.Log($"🔧 修正重叠: 新位置 {segmentCenter:F3}m, 新范围 [{segmentTop:F3}m 到 {segmentBottom:F3}m], 极小间距 {prevBottom - segmentTop:F3}m");
                    }
                }
                
                // 更新当前深度：使用修正后的段底部位置计算下一个段的起始深度
                float nextDepthStart = -segmentBottom + safeGap; // 从当前段底部 + 极小间距开始
                currentDepth = nextDepthStart;
            }
            
            // 如果已经达到钻探深度，停止
            if (currentDepth >= drillingDepth)
            {
                break;
            }
        }
        
        Debug.Log($"✅ 真实钻探样本完成: 钻探深度 {drillingDepth:F2}m, 样本长度 {currentDepth:F3}m, 地层段数 {segments.Count}");
        
        return segments.ToArray();
    }
    
    /// <summary>
    /// 创建地层段 - 简化版本，直接使用传入的位置
    /// </summary>
    private LayerSegment CreateLayerSegment(LayerGeometricCutter.LayerCutResult cutResult, int index, Transform parent, float segmentCenter, float segmentThickness)
    {
        // 创建段对象
        GameObject segmentObj = new GameObject("LayerSegment_" + index + "_" + cutResult.originalLayer.layerName);
        segmentObj.transform.SetParent(parent);
        
        // 直接使用传入的中心位置，不再进行额外计算
        float yOffset = segmentCenter;
        
        // 轻微限制范围，防止样本过长，但不影响紧密拼接
        yOffset = Mathf.Clamp(yOffset, -5f, 1f);
        
        segmentObj.transform.localPosition = new Vector3(0, yOffset, 0);
        
        float segmentTop = yOffset + segmentThickness * 0.5f;
        float segmentBottom = yOffset - segmentThickness * 0.5f;
        Debug.Log($"✓ 地层段 {index} ({cutResult.originalLayer.layerName}): 中心Y {yOffset:F3}m, 厚度 {segmentThickness:F3}m, 范围[顶部 {segmentTop:F3}m 到 底部 {segmentBottom:F3}m]");
        
        // 创建具有正确尺寸的网格，考虑地层实际形状
        BoringTool boringTool = FindFirstObjectByType<BoringTool>();
        float drillingRadius = boringTool?.boringRadius ?? 0.25f;
        
        // 尝试保持地层的真实形状（如果有布尔运算结果）
        Mesh layerMesh;
        if (cutResult.resultMesh != null && preserveOriginalMaterials)
        {
            // 使用真实的几何切割结果，保持地层的原始形状
            layerMesh = ProcessLayerShapeForSample(cutResult.resultMesh, segmentThickness, drillingRadius);
            Debug.Log($"🔧 使用真实地层形状: {cutResult.originalLayer.layerName}, 顶点数: {layerMesh.vertexCount}");
        }
        else
        {
            // 使用简化的圆柱体形状
            layerMesh = CreateVerticalLayerMesh(drillingRadius, segmentThickness);
            Debug.Log($"🔧 使用简化圆柱形状: {cutResult.originalLayer.layerName}");
        }
        
        // 创建材质 - 确保使用原始地层材质
        Material segmentMaterial = CreateLayerMaterial(cutResult.originalLayer, index);
        
        // 验证材质映射是否正确
        ValidateMaterialMapping(cutResult.originalLayer, segmentMaterial);
        
        Debug.Log($"✓ 创建地层段 {index}: {cutResult.originalLayer.layerName}, 材质: {segmentMaterial.name}, 颜色: {segmentMaterial.color}, 网格: {layerMesh.name}");
        
        // 添加网格组件
        MeshFilter meshFilter = segmentObj.AddComponent<MeshFilter>();
        meshFilter.mesh = layerMesh;
        
        MeshRenderer meshRenderer = segmentObj.AddComponent<MeshRenderer>();
        meshRenderer.material = segmentMaterial;
        
        // 调试：输出实际网格边界信息
        Bounds meshBounds = layerMesh.bounds;
        Vector3 worldCenter = segmentObj.transform.position + meshBounds.center;
        Vector3 worldMin = worldCenter - meshBounds.size * 0.5f;
        Vector3 worldMax = worldCenter + meshBounds.size * 0.5f;
        
        Debug.Log($"🔍 地层段 {index} 网格边界: 中心 {worldCenter.y:F3}m, 范围 [{worldMax.y:F3}m 到 {worldMin.y:F3}m], 网格尺寸 {meshBounds.size.y:F3}m");
        
        // 添加碰撞器
        if (useCompoundColliders)
        {
            MeshCollider meshCollider = segmentObj.AddComponent<MeshCollider>();
            meshCollider.convex = true;
            meshCollider.sharedMesh = layerMesh;
        }
        
        LayerSegment segment = new LayerSegment
        {
            segmentObject = segmentObj,
            sourceLayer = cutResult.originalLayer,
            geometry = layerMesh,
            material = segmentMaterial,
            cutResult = cutResult,
            relativeDepth = cutResult.depthStart,
            localCenterOfMass = cutResult.centerOfMass
        };
        
        return segment;
    }
    
    /// <summary>
    /// 处理地层几何体
    /// </summary>
    private Mesh ProcessLayerGeometry(Mesh originalMesh)
    {
        if (originalMesh == null) return null;
        
        Mesh processedMesh = new Mesh();
        processedMesh.name = originalMesh.name + "_Processed";
        
        // 复制基本属性
        processedMesh.vertices = originalMesh.vertices;
        processedMesh.triangles = originalMesh.triangles;
        processedMesh.uv = originalMesh.uv;
        processedMesh.normals = originalMesh.normals;
        
        // 应用几何简化
        if (geometrySimplification > 0.01f && !enableDetailedGeometry)
        {
            processedMesh = SimplifyMesh(processedMesh, geometrySimplification);
        }
        
        // 重新计算属性
        processedMesh.RecalculateBounds();
        processedMesh.RecalculateNormals();
        processedMesh.RecalculateTangents();
        
        return processedMesh;
    }
    
    /// <summary>
    /// 简化网格
    /// </summary>
    private Mesh SimplifyMesh(Mesh mesh, float simplificationFactor)
    {
        // 简化的网格优化：通过移除一些顶点来减少复杂度
        // 目前使用占位符实现
        return mesh;
    }
    
    /// <summary>
    /// 获取地层的当前实际材质（优先从MeshRenderer获取）
    /// </summary>
    private Material GetCurrentLayerMaterial(GeologyLayer layer)
    {
        // 优先从MeshRenderer获取当前使用的共享材质（避免运行时实例化问题）
        MeshRenderer meshRenderer = layer.GetComponent<MeshRenderer>();
        if (meshRenderer != null && meshRenderer.sharedMaterial != null)
        {
            Debug.Log($"🎨 从MeshRenderer获取地层 {layer.layerName} 的共享材质: {meshRenderer.sharedMaterial.name}, 颜色: {meshRenderer.sharedMaterial.color}");
            return meshRenderer.sharedMaterial;
        }
        
        // 回退到GeologyLayer组件中定义的材质
        if (layer.layerMaterial != null)
        {
            Debug.Log($"🎨 从GeologyLayer组件获取地层 {layer.layerName} 的材质: {layer.layerMaterial.name}, 颜色: {layer.layerMaterial.color}");
            return layer.layerMaterial;
        }
        
        Debug.LogWarning($"⚠️ 地层 {layer.layerName} 没有找到材质，将使用默认材质");
        return null;
    }
    
    /// <summary>
    /// 创建地层材质 - 修复：确保使用当前地层的实际材质
    /// </summary>
    private Material CreateLayerMaterial(GeologyLayer layer, int segmentIndex)
    {
        Material material;
        
        // 获取地层的当前实际材质
        Material currentLayerMaterial = GetCurrentLayerMaterial(layer);
        
        if (preserveOriginalMaterials && currentLayerMaterial != null)
        {
            // 创建地层材质的副本，确保获取到最新的材质属性
            material = new Material(currentLayerMaterial);
            // 使用地层的当前材质
            
            // 重要：保持材质的原始属性，对比度增强要非常小心
            if (enhanceLayerContrast)
            {
                Color originalColor = material.color;
                
                // 使用HSV色彩空间进行更精确的亮度调整，避免颜色失真
                Color.RGBToHSV(originalColor, out float h, out float s, out float v);
                
                // 非常小的亮度调整（±10%），避免改变色相
                float variation = (segmentIndex % 2 == 0) ? contrastFactor * 0.1f : -contrastFactor * 0.1f;
                v = Mathf.Clamp01(v + variation);
                
                Color enhancedColor = Color.HSVToRGB(h, s, v);
                enhancedColor.a = originalColor.a; // 保持透明度
                
                material.color = enhancedColor;
                Debug.Log($"🎨 轻微对比度调整: {layer.layerName} 原始HSV({h:F2},{s:F2},{v-variation:F2}) → 调整HSV({h:F2},{s:F2},{v:F2})");
            }
            else
            {
                Debug.Log($"🎨 保持原始材质颜色: {layer.layerName} 颜色 {material.color}");
            }
        }
        else
        {
            // 只有当地层材质为null时，才使用默认材质并应用颜色
            material = new Material(defaultLayerMaterial);
            // 地层使用默认材质
            
            // 设置基础颜色（仅在使用默认材质时）
            Color baseColor = layer.layerColor;
            
            // 增强对比度
            if (enhanceLayerContrast)
            {
                float variation = (segmentIndex % 2 == 0) ? contrastFactor : -contrastFactor;
                baseColor = Color.Lerp(baseColor, Color.white, variation);
            }
            
            material.color = baseColor;
            
            // 设置渲染属性
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", 0.1f);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", 0.4f);
        }
        
        return material;
    }
    
    /// <summary>
    /// 验证材质映射是否正确
    /// </summary>
    private void ValidateMaterialMapping(GeologyLayer layer, Material appliedMaterial)
    {
        if (appliedMaterial == null)
        {
            Debug.LogError($"❌ 材质验证失败: 地层 {layer.layerName} 的应用材质为null");
            return;
        }
        
        // 获取地层的期望材质
        Material expectedMaterial = GetCurrentLayerMaterial(layer);
        
        if (expectedMaterial != null)
        {
            // 比较材质名称和颜色
            bool nameMatch = appliedMaterial.name.Contains(expectedMaterial.name.Replace(" (Instance)", ""));
            bool colorSimilar = Vector4.Distance(expectedMaterial.color, appliedMaterial.color) < 0.1f;
            
            if (!nameMatch)
            {
                Debug.LogWarning($"⚠️ 材质名称不匹配: 地层 {layer.layerName} 期望 {expectedMaterial.name}, 实际 {appliedMaterial.name}");
            }
            
            if (!colorSimilar && !enhanceLayerContrast)
            {
                Debug.LogWarning($"⚠️ 材质颜色差异较大: 地层 {layer.layerName} 期望颜色 {expectedMaterial.color}, 实际颜色 {appliedMaterial.color}");
            }
            
            if (nameMatch && (colorSimilar || enhanceLayerContrast))
            {
                Debug.Log($"✅ 材质验证通过: 地层 {layer.layerName} 材质 {appliedMaterial.name} 颜色 {appliedMaterial.color}");
            }
        }
        else
        {
            Debug.Log($"🔄 使用默认材质: 地层 {layer.layerName} 没有指定材质，使用默认材质 {appliedMaterial.name}");
        }
    }
    
    /// <summary>
    /// 设置样本物理属性
    /// </summary>
    private SamplePhysics SetupSamplePhysics(GameObject container, LayerSegment[] segments, LayerGeometricCutter.GeometricSampleData data)
    {
        SamplePhysics physics = new SamplePhysics();
        
        if (enablePhysics)
        {
            // 添加刚体
            Rigidbody rb = container.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = container.AddComponent<Rigidbody>();
            }
            
            // 计算总质量
            float totalMass = CalculateTotalMass(segments, data);
            rb.mass = totalMass;
            rb.useGravity = false;
            rb.isKinematic = true;
            
            physics.rigidbody = rb;
            physics.totalMass = totalMass;
            
            // 收集所有碰撞器
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
            // 样本位置已经在外部计算好，只需要微调悬浮效果
            container.transform.position = position;
            
            // 添加悬浮组件
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
        
        // 添加几何样本信息组件
        GeometricSampleInfo info = container.GetComponent<GeometricSampleInfo>();
        if (info == null)
        {
            info = container.AddComponent<GeometricSampleInfo>();
        }
        info.Initialize(sample);
        
        // 添加交互组件
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
                minY = Mathf.Min(minY, bounds.min.y);
                maxY = Mathf.Max(maxY, bounds.max.y);
            }
        }
        
        return maxY - minY;
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
    private float CalculateTotalMass(LayerSegment[] segments, LayerGeometricCutter.GeometricSampleData data)
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
    
    void OnDrawGizmos()
    {
        if (!visualizeGeometryBounds) return;
        
        foreach (var sample in activeSamples)
        {
            if (sample?.layerSegments == null) continue;
            
            Gizmos.color = Color.cyan;
            foreach (var segment in sample.layerSegments)
            {
                if (segment.geometry != null)
                {
                    Gizmos.DrawWireCube(segment.geometry.bounds.center, segment.geometry.bounds.size);
                }
            }
        }
    }
}
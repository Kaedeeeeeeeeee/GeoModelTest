using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// 地层几何切割系统
/// 使用真实的几何体切割技术生成地质样本
/// </summary>
public class LayerGeometricCutter : MonoBehaviour
{
    [System.Serializable]
    public struct LayerCutResult
    {
        public bool isValid;
        public Mesh resultMesh;
        public GeologyLayer originalLayer;
        public float volume;
        public Vector3 centerOfMass;
        public float surfaceArea;
        public Bounds bounds;
        public GeologicalFeatures features;
        public float depthStart;
        public float depthEnd;
    }
    
    [System.Serializable]
    public struct GeologicalFeatures
    {
        public float averageDip;
        public Vector3 dipDirection;
        public float surfaceRoughness;
        public Vector3 textureOrientation;
        public float thicknessVariation;
        public List<Vector3> foldPoints;
        public List<Vector3> faultLines;
    }
    
    [System.Serializable]
    public struct GeometricSampleData
    {
        public string sampleID;
        public Vector3 drillingPosition;
        public Vector3 drillingDirection;
        public float drillingRadius;
        public float drillingDepth;
        public LayerCutResult[] layerResults;
        public float totalVolume;
        public System.DateTime collectionTime;
        public Bounds overallBounds;
    }
    
    [Header("切割参数")]
    public float minimumLayerThickness = 0.05f;
    public float geometryTolerance = 0.001f;
    public bool preserveFineDetails = true;
    
    [Header("性能设置")]
    public bool useAsyncProcessing = true;
    public int maxConcurrentOperations = 4;
    public bool enableProgressReporting = true;
    
    [Header("调试")]
    public bool showDebugVisualization = false;
    public bool logDetailedInfo = true;
    public Material debugMaterial;
    
    private DrillingCylinderGenerator cylinderGenerator;
    private MeshBooleanOperations booleanOps;
    private List<GameObject> debugObjects = new List<GameObject>();
    private LayerCutResult[] lastResults;
    
    void Start()
    {
        InitializeComponents();
    }
    
    void InitializeComponents()
    {
        cylinderGenerator = GetComponent<DrillingCylinderGenerator>();
        if (cylinderGenerator == null)
        {
            cylinderGenerator = gameObject.AddComponent<DrillingCylinderGenerator>();
        }
        
        booleanOps = GetComponent<MeshBooleanOperations>();
        if (booleanOps == null)
        {
            booleanOps = gameObject.AddComponent<MeshBooleanOperations>();
        }
        
        
    }
    
    /// <summary>
    /// 创建真实地质样本
    /// </summary>
    public async Task<GeometricSampleData> CreateRealGeologicalSampleAsync(Vector3 drillingPoint, Vector3 direction, float radius, float depth)
    {
        
        
        GeometricSampleData sampleData = new GeometricSampleData
        {
            sampleID = System.Guid.NewGuid().ToString(),
            drillingPosition = drillingPoint,
            drillingDirection = direction.normalized,
            drillingRadius = radius,
            drillingDepth = depth,
            collectionTime = System.DateTime.Now
        };
        
        try
        {
            // 确保组件初始化
            if (cylinderGenerator == null || booleanOps == null)
            {
                InitializeComponents();
            }
            
            // 第1步：创建钻探圆柱体
            Mesh drillingCylinder = cylinderGenerator.CreateDrillingCylinder(drillingPoint, direction, radius, depth);
            if (drillingCylinder == null)
            {
                
                return sampleData;
            }
            
            // 第2步：获取钻探范围内的地层
            GeologyLayer[] layersInRange = cylinderGenerator.GetLayersInDrillingRange(drillingPoint, direction, depth + radius);
            
            
            if (layersInRange.Length == 0)
            {
                
                return sampleData;
            }
            
            // 第3步：对每个地层进行切割
            List<LayerCutResult> cutResults = new List<LayerCutResult>();
            
            if (useAsyncProcessing)
            {
                cutResults = await ProcessLayersAsync(layersInRange, drillingCylinder, drillingPoint, direction);
            }
            else
            {
                cutResults = ProcessLayersSync(layersInRange, drillingCylinder, drillingPoint, direction);
            }
            
            // 第4步：按深度排序和验证结果
            var validResults = cutResults.Where(r => r.isValid && r.volume > 0.001f).ToArray();
            sampleData.layerResults = SortResultsByDepth(validResults, drillingPoint, direction);
            
            // 第5步：计算整体属性
            CalculateOverallProperties(ref sampleData);
            
            lastResults = sampleData.layerResults;
            
            
            
            return sampleData;
        }
        catch (System.Exception e)
        {
            
            return sampleData;
        }
    }
    
    /// <summary>
    /// 同步处理地层
    /// </summary>
    private List<LayerCutResult> ProcessLayersSync(GeologyLayer[] layers, Mesh drillingCylinder, Vector3 drillingPoint, Vector3 direction)
    {
        List<LayerCutResult> results = new List<LayerCutResult>();
        
        for (int i = 0; i < layers.Length; i++)
        {
            if (enableProgressReporting)
            {
                float progress = (float)i / layers.Length;
                
            }
            
            LayerCutResult result = CutLayerWithCylinder(layers[i], drillingCylinder, drillingPoint, direction);
            if (result.isValid)
            {
                results.Add(result);
            }
        }
        
        return results;
    }
    
    /// <summary>
    /// 异步处理地层
    /// </summary>
    private async Task<List<LayerCutResult>> ProcessLayersAsync(GeologyLayer[] layers, Mesh drillingCylinder, Vector3 drillingPoint, Vector3 direction)
    {
        List<LayerCutResult> results = new List<LayerCutResult>();
        
        for (int i = 0; i < layers.Length; i += maxConcurrentOperations)
        {
            int batchEnd = Mathf.Min(i + maxConcurrentOperations, layers.Length);
            var batchLayers = layers.Skip(i).Take(batchEnd - i).ToArray();
            
            var batchTasks = batchLayers.Select(layer => 
                Task.Run(() => CutLayerWithCylinder(layer, drillingCylinder, drillingPoint, direction))
            ).ToArray();
            
            var batchResults = await Task.WhenAll(batchTasks);
            
            foreach (var result in batchResults)
            {
                if (result.isValid)
                {
                    results.Add(result);
                }
            }
            
            if (enableProgressReporting)
            {
                float progress = (float)batchEnd / layers.Length;
                
            }
        }
        
        return results;
    }
    
    /// <summary>
    /// 用圆柱体切割地层
    /// </summary>
    private LayerCutResult CutLayerWithCylinder(GeologyLayer layer, Mesh cylinder, Vector3 drillingPoint, Vector3 direction)
    {
        LayerCutResult result = new LayerCutResult
        {
            originalLayer = layer,
            isValid = false
        };
        
        try
        {
            MeshFilter layerMeshFilter = layer.GetComponent<MeshFilter>();
            if (layerMeshFilter == null || layerMeshFilter.mesh == null)
            {
                if (logDetailedInfo)
                    
                return result;
            }
            
            // 执行布尔交集运算
            var booleanResult = booleanOps.IntersectMeshes(
                layerMeshFilter.mesh, 
                cylinder, 
                layer.transform, 
                transform,
                layer.layerMaterial
            );
            
            if (!booleanResult.isValid || booleanResult.vertices.Length == 0)
            {
                if (logDetailedInfo)
                    
                return result;
            }
            
            // 创建结果网格
            Mesh intersectionMesh = CreateMeshFromBooleanResult(booleanResult);
            
            // 计算深度范围
            var depthRange = CalculateDepthRange(booleanResult.vertices, drillingPoint, direction);
            
            // 分析地质特征
            GeologicalFeatures features = AnalyzeGeologicalFeatures(intersectionMesh, layer, drillingPoint, direction);
            
            result = new LayerCutResult
            {
                isValid = true,
                resultMesh = intersectionMesh,
                originalLayer = layer,
                volume = booleanResult.volume,
                centerOfMass = booleanResult.centerOfMass,
                surfaceArea = CalculateSurfaceArea(intersectionMesh),
                bounds = intersectionMesh.bounds,
                features = features,
                depthStart = depthRange.x,
                depthEnd = depthRange.y
            };
            
            if (logDetailedInfo)
            {
                
            }
            
            return result;
        }
        catch (System.Exception e)
        {
            
            return result;
        }
    }
    
    /// <summary>
    /// 从布尔运算结果创建网格
    /// </summary>
    private Mesh CreateMeshFromBooleanResult(MeshBooleanOperations.CSGResult booleanResult)
    {
        Mesh mesh = new Mesh();
        mesh.name = "CutLayerMesh";
        mesh.vertices = booleanResult.vertices;
        mesh.triangles = booleanResult.triangles;
        mesh.uv = booleanResult.uvs;
        mesh.normals = booleanResult.normals;
        
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        
        return mesh;
    }
    
    /// <summary>
    /// 计算深度范围
    /// </summary>
    private Vector2 CalculateDepthRange(Vector3[] vertices, Vector3 drillingPoint, Vector3 direction)
    {
        float minDepth = float.MaxValue;
        float maxDepth = float.MinValue;
        
        foreach (Vector3 vertex in vertices)
        {
            Vector3 toVertex = vertex - drillingPoint;
            float depth = Vector3.Dot(toVertex, direction);
            
            minDepth = Mathf.Min(minDepth, depth);
            maxDepth = Mathf.Max(maxDepth, depth);
        }
        
        return new Vector2(minDepth, maxDepth);
    }
    
    /// <summary>
    /// 分析地质特征
    /// </summary>
    private GeologicalFeatures AnalyzeGeologicalFeatures(Mesh mesh, GeologyLayer layer, Vector3 drillingPoint, Vector3 direction)
    {
        GeologicalFeatures features = new GeologicalFeatures();
        
        features.averageDip = CalculateAverageDip(mesh, layer);
        features.dipDirection = CalculateDipDirection(mesh, layer);
        features.surfaceRoughness = CalculateSurfaceRoughness(mesh);
        features.textureOrientation = layer.strikeDirection;
        features.thicknessVariation = CalculateThicknessVariation(mesh, direction);
        features.foldPoints = new List<Vector3>();
        features.faultLines = new List<Vector3>();
        
        return features;
    }
    
    private float CalculateAverageDip(Mesh mesh, GeologyLayer layer)
    {
        return layer.dipAngle;
    }
    
    private Vector3 CalculateDipDirection(Mesh mesh, GeologyLayer layer)
    {
        Vector3 strike = layer.strikeDirection.normalized;
        Vector3 dip = Vector3.Cross(Vector3.up, strike);
        return dip.normalized;
    }
    
    private float CalculateSurfaceRoughness(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        
        float roughness = 0f;
        for (int i = 0; i < normals.Length - 1; i++)
        {
            float angle = Vector3.Angle(normals[i], normals[i + 1]);
            roughness += angle;
        }
        
        return normals.Length > 0 ? roughness / normals.Length : 0f;
    }
    
    private float CalculateThicknessVariation(Mesh mesh, Vector3 direction)
    {
        Vector3[] vertices = mesh.vertices;
        
        float minThickness = float.MaxValue;
        float maxThickness = float.MinValue;
        
        foreach (Vector3 vertex in vertices)
        {
            float thickness = Vector3.Dot(vertex, direction);
            minThickness = Mathf.Min(minThickness, thickness);
            maxThickness = Mathf.Max(maxThickness, thickness);
        }
        
        return maxThickness - minThickness;
    }
    
    private float CalculateSurfaceArea(Mesh mesh)
    {
        float totalArea = 0f;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 v0 = vertices[triangles[i]];
            Vector3 v1 = vertices[triangles[i + 1]];
            Vector3 v2 = vertices[triangles[i + 2]];
            
            Vector3 cross = Vector3.Cross(v1 - v0, v2 - v0);
            totalArea += cross.magnitude * 0.5f;
        }
        
        return totalArea;
    }
    
    /// <summary>
    /// 按深度排序结果
    /// </summary>
    private LayerCutResult[] SortResultsByDepth(LayerCutResult[] results, Vector3 drillingPoint, Vector3 direction)
    {
        return results.OrderBy(r => r.depthStart).ToArray();
    }
    
    /// <summary>
    /// 计算整体属性
    /// </summary>
    private void CalculateOverallProperties(ref GeometricSampleData sampleData)
    {
        sampleData.totalVolume = sampleData.layerResults.Sum(r => r.volume);
        
        if (sampleData.layerResults.Length > 0)
        {
            Bounds overallBounds = sampleData.layerResults[0].bounds;
            for (int i = 1; i < sampleData.layerResults.Length; i++)
            {
                overallBounds.Encapsulate(sampleData.layerResults[i].bounds);
            }
            sampleData.overallBounds = overallBounds;
        }
    }
    
    /// <summary>
    /// 创建调试可视化
    /// </summary>
    public void CreateDebugVisualization()
    {
        if (!showDebugVisualization || lastResults == null) return;
        
        ClearDebugVisualization();
        
        for (int i = 0; i < lastResults.Length; i++)
        {
            LayerCutResult result = lastResults[i];
            if (!result.isValid) continue;
            
            GameObject debugObj = new GameObject("DebugCut_" + result.originalLayer.layerName);
            debugObj.transform.SetParent(transform);
            
            MeshFilter meshFilter = debugObj.AddComponent<MeshFilter>();
            meshFilter.mesh = result.resultMesh;
            
            MeshRenderer meshRenderer = debugObj.AddComponent<MeshRenderer>();
            if (debugMaterial != null)
            {
                Material instanceMat = new Material(debugMaterial);
                instanceMat.color = result.originalLayer.layerColor;
                meshRenderer.material = instanceMat;
            }
            else
            {
                meshRenderer.material = result.originalLayer.layerMaterial;
            }
            
            debugObjects.Add(debugObj);
        }
        
        
    }
    
    /// <summary>
    /// 清除调试可视化
    /// </summary>
    public void ClearDebugVisualization()
    {
        foreach (GameObject obj in debugObjects)
        {
            if (obj != null)
            {
                DestroyImmediate(obj);
            }
        }
        debugObjects.Clear();
    }
    
    void OnDestroy()
    {
        ClearDebugVisualization();
    }
}
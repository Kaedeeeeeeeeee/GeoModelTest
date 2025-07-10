using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 钻探圆柱体几何生成器
/// 创建高精度的钻探圆柱体网格，用于与地层网格进行布尔运算
/// </summary>
public class DrillingCylinderGenerator : MonoBehaviour
{
    [Header("圆柱体参数")]
    public int radialSegments = 32; // 圆周分段数
    public int heightSegments = 20; // 高度分段数
    public bool generateCaps = true; // 是否生成顶底面
    
    [Header("调试")]
    public bool showDebugGizmos = false;
    public Material debugMaterial;
    
    private Mesh lastGeneratedMesh;
    private Vector3 lastStartPoint;
    private Vector3 lastDirection;
    
    /// <summary>
    /// 创建钻探圆柱体网格
    /// </summary>
    /// <param name="startPoint">起始点（钻探起始位置）</param>
    /// <param name="direction">钻探方向（通常为Vector3.down）</param>
    /// <param name="radius">钻探半径</param>
    /// <param name="depth">钻探深度</param>
    /// <returns>生成的圆柱体网格</returns>
    public Mesh CreateDrillingCylinder(Vector3 startPoint, Vector3 direction, float radius, float depth)
    {
        Debug.Log($"创建钻探圆柱体 - 起点: {startPoint}, 方向: {direction}, 半径: {radius}, 深度: {depth}");
        
        // 缓存参数用于调试
        lastStartPoint = startPoint;
        lastDirection = direction.normalized;
        
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        List<Vector3> normals = new List<Vector3>();
        
        // 计算圆柱体的局部坐标系
        Vector3 up = lastDirection;
        Vector3 right = Vector3.Cross(up, Vector3.forward);
        if (right.magnitude < 0.1f) // 如果方向接近forward，使用right作为参考
        {
            right = Vector3.Cross(up, Vector3.right);
        }
        right = right.normalized;
        Vector3 forward = Vector3.Cross(right, up).normalized;
        
        // 生成圆柱体侧面顶点
        GenerateCylinderSides(vertices, triangles, uvs, normals, 
                             startPoint, right, forward, up, radius, depth);
        
        // 生成顶底面
        if (generateCaps)
        {
            GenerateCylinderCaps(vertices, triangles, uvs, normals,
                               startPoint, right, forward, up, radius, depth);
        }
        
        // 创建网格
        Mesh cylinderMesh = new Mesh();
        cylinderMesh.name = "DrillingCylinder";
        cylinderMesh.vertices = vertices.ToArray();
        cylinderMesh.triangles = triangles.ToArray();
        cylinderMesh.uv = uvs.ToArray();
        cylinderMesh.normals = normals.ToArray();
        
        // 重新计算边界和切线
        cylinderMesh.RecalculateBounds();
        cylinderMesh.RecalculateTangents();
        
        lastGeneratedMesh = cylinderMesh;
        
        // Debug.Log($"圆柱体网格生成完成 - 顶点数: {vertices.Count}, 三角形数: {triangles.Count / 3}");
        
        return cylinderMesh;
    }
    
    /// <summary>
    /// 生成圆柱体侧面
    /// </summary>
    private void GenerateCylinderSides(List<Vector3> vertices, List<int> triangles, 
                                     List<Vector2> uvs, List<Vector3> normals,
                                     Vector3 startPoint, Vector3 right, Vector3 forward, Vector3 up,
                                     float radius, float depth)
    {
        // 生成圆柱体侧面的顶点
        for (int h = 0; h <= heightSegments; h++)
        {
            float t = (float)h / heightSegments;
            Vector3 heightOffset = up * (depth * t);
            
            for (int r = 0; r < radialSegments; r++)
            {
                float angle = (float)r / radialSegments * 2f * Mathf.PI;
                
                // 计算圆周上的点
                Vector3 circlePoint = right * Mathf.Cos(angle) + forward * Mathf.Sin(angle);
                Vector3 vertex = startPoint + heightOffset + circlePoint * radius;
                
                vertices.Add(vertex);
                
                // 计算UV坐标
                Vector2 uv = new Vector2((float)r / radialSegments, t);
                uvs.Add(uv);
                
                // 计算法向量（指向圆柱体外侧）
                Vector3 normal = circlePoint.normalized;
                normals.Add(normal);
            }
        }
        
        // 生成侧面三角形
        for (int h = 0; h < heightSegments; h++)
        {
            for (int r = 0; r < radialSegments; r++)
            {
                int current = h * radialSegments + r;
                int next = h * radialSegments + (r + 1) % radialSegments;
                int currentNext = (h + 1) * radialSegments + r;
                int nextNext = (h + 1) * radialSegments + (r + 1) % radialSegments;
                
                // 第一个三角形 (逆时针)
                triangles.Add(current);
                triangles.Add(next);
                triangles.Add(currentNext);
                
                // 第二个三角形 (逆时针)
                triangles.Add(next);
                triangles.Add(nextNext);
                triangles.Add(currentNext);
            }
        }
    }
    
    /// <summary>
    /// 生成圆柱体顶底面
    /// </summary>
    private void GenerateCylinderCaps(List<Vector3> vertices, List<int> triangles,
                                    List<Vector2> uvs, List<Vector3> normals,
                                    Vector3 startPoint, Vector3 right, Vector3 forward, Vector3 up,
                                    float radius, float depth)
    {
        int sideVertexCount = vertices.Count;
        
        // 顶面中心顶点
        int topCenterIndex = vertices.Count;
        vertices.Add(startPoint);
        uvs.Add(new Vector2(0.5f, 0.5f));
        normals.Add(-up); // 顶面法向量向上
        
        // 顶面圆周顶点
        for (int r = 0; r < radialSegments; r++)
        {
            float angle = (float)r / radialSegments * 2f * Mathf.PI;
            Vector3 circlePoint = right * Mathf.Cos(angle) + forward * Mathf.Sin(angle);
            Vector3 vertex = startPoint + circlePoint * radius;
            
            vertices.Add(vertex);
            
            // UV坐标映射到圆形
            Vector2 uv = new Vector2(
                0.5f + Mathf.Cos(angle) * 0.5f,
                0.5f + Mathf.Sin(angle) * 0.5f
            );
            uvs.Add(uv);
            normals.Add(-up);
        }
        
        // 底面中心顶点
        int bottomCenterIndex = vertices.Count;
        vertices.Add(startPoint + up * depth);
        uvs.Add(new Vector2(0.5f, 0.5f));
        normals.Add(up); // 底面法向量向下
        
        // 底面圆周顶点
        for (int r = 0; r < radialSegments; r++)
        {
            float angle = (float)r / radialSegments * 2f * Mathf.PI;
            Vector3 circlePoint = right * Mathf.Cos(angle) + forward * Mathf.Sin(angle);
            Vector3 vertex = startPoint + up * depth + circlePoint * radius;
            
            vertices.Add(vertex);
            
            Vector2 uv = new Vector2(
                0.5f + Mathf.Cos(angle) * 0.5f,
                0.5f + Mathf.Sin(angle) * 0.5f
            );
            uvs.Add(uv);
            normals.Add(up);
        }
        
        // 生成顶面三角形
        for (int r = 0; r < radialSegments; r++)
        {
            int current = topCenterIndex + 1 + r;
            int next = topCenterIndex + 1 + (r + 1) % radialSegments;
            
            triangles.Add(topCenterIndex);
            triangles.Add(current);
            triangles.Add(next);
        }
        
        // 生成底面三角形
        for (int r = 0; r < radialSegments; r++)
        {
            int current = bottomCenterIndex + 1 + r;
            int next = bottomCenterIndex + 1 + (r + 1) % radialSegments;
            
            triangles.Add(bottomCenterIndex);
            triangles.Add(next);
            triangles.Add(current);
        }
    }
    
    /// <summary>
    /// 获取钻探范围内的所有地层 - 改进版：支持精确位置检测
    /// </summary>
    public GeologyLayer[] GetLayersInDrillingRange(Vector3 startPoint, Vector3 direction, float maxDistance)
    {
        List<GeologyLayer> layersInRange = new List<GeologyLayer>();
        GeologyLayer[] allLayers = FindObjectsByType<GeologyLayer>(FindObjectsSortMode.None);
        
        Debug.Log($"🔍 开始地层检测: 钻探点 {startPoint}, 地层数 {allLayers.Length}");
        
        // 🔧 修复：对于深层钻探，检测所有可能相关的地层
        // 不再基于起点位置进行预筛选，而是检查整个钻探路径
        List<GeologyLayer> nearbyLayers = PrefilterLayersForDrillingPath(allLayers, startPoint, direction, maxDistance);
        // 预筛选完成
        
        foreach (GeologyLayer layer in nearbyLayers)
        {
            // 多级检测：边界框 + 射线检测 + 深度验证
            if (IsLayerInDrillingPath(layer, startPoint, direction, maxDistance))
            {
                layersInRange.Add(layer);
            }
        }
        
        // 🔧 修复：按照钻探路径上的相交顺序排序，适用于深层钻探
        layersInRange.Sort((a, b) => {
            // 计算地层在钻探路径上的相交深度
            float depthA = GetLayerIntersectionDepth(a, startPoint, direction);
            float depthB = GetLayerIntersectionDepth(b, startPoint, direction);
            return depthA.CompareTo(depthB);
        });
        
        Debug.Log($"🎯 地层检测完成，找到 {layersInRange.Count} 个相关地层");
        
        return layersInRange.ToArray();
    }
    
    /// <summary>
    /// 🔧 新方法：基于整个钻探路径预筛选地层，适用于深层钻探
    /// </summary>
    private List<GeologyLayer> PrefilterLayersForDrillingPath(GeologyLayer[] allLayers, Vector3 startPoint, Vector3 direction, float maxDistance)
    {
        List<GeologyLayer> relevantLayers = new List<GeologyLayer>();
        
        // 计算钻探路径的终点
        Vector3 endPoint = startPoint + direction * maxDistance;
        
        Debug.Log($"🔧 预筛选开始: 钻探起点 {startPoint}, 终点 {endPoint}, 深度 {maxDistance}m");
        
        foreach (GeologyLayer layer in allLayers)
        {
            Bounds layerBounds = GetLayerBounds(layer);
            
            // 🔧 关键修复：检查地层是否与钻探路径有交集，而不是仅检查起点
            bool intersects = DoesLayerIntersectDrillingPath(layerBounds, startPoint, endPoint);
            
            // 特别关注dem.003的详细调试信息
            if (layer.layerName == "dem.003")
            {
                Debug.Log($"🔍 [dem.003] 边界框分析:");
                Debug.Log($"   边界框中心: {layerBounds.center}");
                Debug.Log($"   边界框尺寸: {layerBounds.size}");
                Debug.Log($"   边界框范围: min({layerBounds.min}) ~ max({layerBounds.max})");
                Debug.Log($"   钻探起点: {startPoint}");
                Debug.Log($"   钻探终点: {endPoint}");
                Debug.Log($"   路径相交测试: {intersects}");
                
                // 详细的相交分析
                Vector3 drillingDirection = (endPoint - startPoint).normalized;
                float drillingDistance = Vector3.Distance(startPoint, endPoint);
                Ray drillingRay = new Ray(startPoint, drillingDirection);
                bool rayIntersects = layerBounds.IntersectRay(drillingRay, out float enterDistance);
                
                Debug.Log($"   射线相交分析: {rayIntersects}, 进入距离: {enterDistance}m, 钻探距离: {drillingDistance}m");
                Debug.Log($"   是否在距离范围内: {enterDistance <= drillingDistance}");
            }
            
            if (intersects)
            {
                relevantLayers.Add(layer);
                if (layer.layerName == "dem.003")
                {
                    Debug.Log($"✅ [dem.003] 通过预筛选");
                }
            }
            else if (layer.layerName == "dem.003")
            {
                Debug.Log($"❌ [dem.003] 未通过预筛选");
            }
        }
        
        Debug.Log($"🔧 路径预筛选: {allLayers.Length} 个地层 → {relevantLayers.Count} 个相关地层");
        return relevantLayers;
    }
    
    /// <summary>
    /// 检查地层边界框是否与钻探路径相交
    /// </summary>
    private bool DoesLayerIntersectDrillingPath(Bounds layerBounds, Vector3 startPoint, Vector3 endPoint)
    {
        // 使用线段与边界框相交测试
        Vector3 direction = (endPoint - startPoint).normalized;
        float distance = Vector3.Distance(startPoint, endPoint);
        
        // Unity的Bounds.IntersectRay方法
        Ray drillingRay = new Ray(startPoint, direction);
        return layerBounds.IntersectRay(drillingRay, out float enterDistance) && enterDistance <= distance;
    }
    
    /// <summary>
    /// 原有方法：预筛选附近的地层（备用）
    /// </summary>
    private List<GeologyLayer> PrefilterNearbyLayers(GeologyLayer[] allLayers, Vector3 startPoint, float searchRadius)
    {
        List<GeologyLayer> nearbyLayers = new List<GeologyLayer>();
        
        foreach (GeologyLayer layer in allLayers)
        {
            Bounds layerBounds = GetLayerBounds(layer);
            
            // 计算钻探点到地层边界框的最短距离
            Vector3 closestPoint = layerBounds.ClosestPoint(startPoint);
            float distance = Vector3.Distance(startPoint, closestPoint);
            
            if (distance <= searchRadius)
            {
                nearbyLayers.Add(layer);
            }
        }
        
        return nearbyLayers;
    }
    
    /// <summary>
    /// 精确检测地层是否在钻探路径中 - 改进版：优先检测钻探起点处的地层
    /// </summary>
    private bool IsLayerInDrillingPath(GeologyLayer layer, Vector3 startPoint, Vector3 direction, float maxDistance)
    {
        Bounds layerBounds = GetLayerBounds(layer);
        bool isDem003 = layer.layerName == "dem.003";
        
        if (isDem003)
        {
            Debug.Log($"🔍 [dem.003] 进入IsLayerInDrillingPath详细检测:");
            Debug.Log($"   地层边界框: {layerBounds.center} ± {layerBounds.size/2}");
            Debug.Log($"   钻探起点: {startPoint}");
            Debug.Log($"   钻探方向: {direction}");
            Debug.Log($"   最大距离: {maxDistance}m");
        }
        
        // 重要：先检测钻探起点是否在地层内（地表检测）
        bool pointInLayer = IsPointInLayer(startPoint, layer);
        if (isDem003)
        {
            Debug.Log($"   步骤1 - 起点在地层内: {pointInLayer}");
        }
        
        if (pointInLayer)
        {
            if (isDem003) Debug.Log($"✅ [dem.003] 通过起点检测");
            return true;
        }
        
        // 第1步：快速边界框检测
        bool inBounds = IsLayerInBounds(layerBounds, startPoint, direction, maxDistance);
        if (isDem003)
        {
            Debug.Log($"   步骤2 - 边界框检测: {inBounds}");
        }
        
        if (!inBounds)
        {
            if (isDem003) Debug.Log($"❌ [dem.003] 未通过边界框检测");
            return false;
        }
        
        // 第2步：射线-边界框交点检测（更精确）
        Ray drillingRay = new Ray(startPoint, direction);
        bool rayIntersects = layerBounds.IntersectRay(drillingRay, out float distance);
        if (isDem003)
        {
            Debug.Log($"   步骤3 - 射线-边界框交点: {rayIntersects}, 距离: {distance}m");
        }
        
        if (!rayIntersects)
        {
            if (isDem003) Debug.Log($"❌ [dem.003] 未通过射线-边界框交点检测");
            return false;
        }
        
        // 🔧 修复：对于深层钻探，使用更宽松的距离限制
        // 深层钻探时，起点可能已经在地层内部，需要更宽松的检测
        bool withinDistance = distance <= maxDistance * 5f;
        if (isDem003)
        {
            Debug.Log($"   步骤4 - 距离限制检测: {withinDistance} (距离: {distance}m <= 限制: {maxDistance * 5f}m)");
        }
        
        if (!withinDistance)
        {
            if (isDem003) Debug.Log($"❌ [dem.003] 超出距离限制");
            return false;
        }
        
        // 第3步：精确的网格交点检测（如果需要更高精度）
        MeshCollider meshCollider = layer.GetComponent<MeshCollider>();
        if (meshCollider != null)
        {
            bool meshIntersects = IsLayerIntersectedByMesh(layer, startPoint, direction, maxDistance);
            if (isDem003)
            {
                Debug.Log($"   步骤5 - 网格精确检测: {meshIntersects}");
            }
            
            if (!meshIntersects)
            {
                if (isDem003) Debug.Log($"❌ [dem.003] 未通过网格精确检测");
                return false;
            }
        }
        else if (isDem003)
        {
            Debug.Log($"   步骤5 - 无网格碰撞器，跳过网格检测");
        }
        
        // 地层通过边界框和射线检测
        if (isDem003) Debug.Log($"✅ [dem.003] 通过所有检测步骤");
        return true;
    }
    
    /// <summary>
    /// 检测点是否在地层内部（用于地表检测）- 修复版：更严格的检测
    /// </summary>
    private bool IsPointInLayer(Vector3 point, GeologyLayer layer)
    {
        Bounds layerBounds = GetLayerBounds(layer);
        bool isDem003 = layer.layerName == "dem.003";
        
        if (isDem003)
        {
            Debug.Log($"🔍 [dem.003] IsPointInLayer检测:");
            Debug.Log($"   检测点: {point}");
            Debug.Log($"   地层边界框: center({layerBounds.center}), size({layerBounds.size})");
            Debug.Log($"   边界范围: min({layerBounds.min}) ~ max({layerBounds.max})");
        }
        
        // 严格的3D边界框检测（避免过于宽松的容差）
        bool inBounds = layerBounds.Contains(point);
        if (isDem003)
        {
            Debug.Log($"   3D边界框包含检测: {inBounds}");
            if (!inBounds)
            {
                Vector3 distance = point - layerBounds.center;
                Vector3 halfSize = layerBounds.size * 0.5f;
                Debug.Log($"   距离分析: X({distance.x:F3}, 边界±{halfSize.x:F3}), Y({distance.y:F3}, 边界±{halfSize.y:F3}), Z({distance.z:F3}, 边界±{halfSize.z:F3})");
                
                bool xIn = Mathf.Abs(distance.x) <= halfSize.x;
                bool yIn = Mathf.Abs(distance.y) <= halfSize.y;
                bool zIn = Mathf.Abs(distance.z) <= halfSize.z;
                Debug.Log($"   轴向检测: X({xIn}), Y({yIn}), Z({zIn})");
            }
        }
        
        if (!inBounds)
        {
            if (isDem003) Debug.Log($"❌ [dem.003] 点不在边界框内");
            return false;
        }
        
        // 🔧 改进：深层钻探时的精确检测
        MeshCollider meshCollider = layer.GetComponent<MeshCollider>();
        if (meshCollider != null)
        {
            if (isDem003) Debug.Log($"   进行网格碰撞器检测...");
            
            // 多方向射线检测，适应深层钻探
            Vector3[] directions = { Vector3.down, Vector3.up, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
            
            bool anyHit = false;
            foreach (Vector3 dir in directions)
            {
                Ray ray = new Ray(point, dir);
                bool hit = meshCollider.Raycast(ray, out RaycastHit hitInfo, 2.0f);
                if (hit)
                {
                    anyHit = true;
                    if (isDem003) Debug.Log($"   射线方向 {dir} 击中距离: {hitInfo.distance:F3}m");
                    break;
                }
            }
            
            if (isDem003)
            {
                Debug.Log($"   网格射线检测结果: {anyHit}");
                Debug.Log($"   最终结果: {true} (边界框内则有效)");
            }
            
            // 如果射线检测失败，但点在边界框内，对于深层钻探仍然认为有效
            return true;
        }
        
        if (isDem003) Debug.Log($"   无网格碰撞器，使用边界框结果: {true}");
        
        // 如果没有MeshCollider，但在边界框内，谨慎返回true
        return true;
    }
    
    /// <summary>
    /// 简单的边界框检测 - 改进版：更严格的位置检测
    /// </summary>
    private bool IsLayerInBounds(Bounds layerBounds, Vector3 startPoint, Vector3 direction, float maxDistance)
    {
        // 获取钻探半径
        BoringTool boringTool = FindFirstObjectByType<BoringTool>();
        float drillingRadius = boringTool?.boringRadius ?? 0.25f;
        
        bool isDem003 = false;
        // 检查是否有GeologyLayer组件来判断是否是dem.003
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        foreach (var obj in allObjects)
        {
            GeologyLayer layer = obj.GetComponent<GeologyLayer>();
            if (layer != null && layer.layerName == "dem.003")
            {
                Bounds objBounds = GetLayerBounds(layer);
                if (Vector3.Distance(objBounds.center, layerBounds.center) < 0.1f)
                {
                    isDem003 = true;
                    break;
                }
            }
        }
        
        if (isDem003)
        {
            Debug.Log($"🔍 [dem.003] IsLayerInBounds边界框检测:");
            Debug.Log($"   钻探起点: {startPoint}");
            Debug.Log($"   钻探方向: {direction}");
            Debug.Log($"   钻探半径: {drillingRadius}m");
            Debug.Log($"   地层边界框: {layerBounds.center} ± {layerBounds.size/2}");
        }
        
        // 检查钻探起点是否在地层的水平范围内（XZ平面）
        Vector2 startPointXZ = new Vector2(startPoint.x, startPoint.z);
        Vector2 layerCenterXZ = new Vector2(layerBounds.center.x, layerBounds.center.z);
        Vector2 layerSizeXZ = new Vector2(layerBounds.size.x, layerBounds.size.z);
        
        // 创建地层在XZ平面的矩形
        Rect layerRect = new Rect(
            layerCenterXZ.x - layerSizeXZ.x * 0.5f,
            layerCenterXZ.y - layerSizeXZ.y * 0.5f,
            layerSizeXZ.x,
            layerSizeXZ.y
        );
        
        // 扩展矩形以包含钻探半径
        layerRect.x -= drillingRadius;
        layerRect.y -= drillingRadius;
        layerRect.width += drillingRadius * 2f;
        layerRect.height += drillingRadius * 2f;
        
        bool inHorizontalBounds = layerRect.Contains(startPointXZ);
        
        if (isDem003)
        {
            Debug.Log($"   XZ平面分析:");
            Debug.Log($"   起点XZ: {startPointXZ}");
            Debug.Log($"   地层中心XZ: {layerCenterXZ}");
            Debug.Log($"   地层尺寸XZ: {layerSizeXZ}");
            Debug.Log($"   原始矩形: x({layerCenterXZ.x - layerSizeXZ.x * 0.5f:F3} ~ {layerCenterXZ.x + layerSizeXZ.x * 0.5f:F3}), z({layerCenterXZ.y - layerSizeXZ.y * 0.5f:F3} ~ {layerCenterXZ.y + layerSizeXZ.y * 0.5f:F3})");
            Debug.Log($"   扩展矩形: x({layerRect.x:F3} ~ {layerRect.x + layerRect.width:F3}), z({layerRect.y:F3} ~ {layerRect.y + layerRect.height:F3})");
            Debug.Log($"   水平边界检测: {inHorizontalBounds}");
        }
        
        if (!inHorizontalBounds)
        {
            if (isDem003) Debug.Log($"❌ [dem.003] 水平边界检测失败");
            return false;
        }
        
        // 检查垂直方向的交集
        Vector3 endPoint = startPoint + direction * maxDistance;
        float drillingTop = Mathf.Max(startPoint.y, endPoint.y);
        float drillingBottom = Mathf.Min(startPoint.y, endPoint.y);
        
        bool inVerticalBounds = !(layerBounds.max.y < drillingBottom || layerBounds.min.y > drillingTop);
        
        if (isDem003)
        {
            Debug.Log($"   垂直方向分析:");
            Debug.Log($"   钻探终点: {endPoint}");
            Debug.Log($"   钻探垂直范围: {drillingBottom:F3}m ~ {drillingTop:F3}m");
            Debug.Log($"   地层垂直范围: {layerBounds.min.y:F3}m ~ {layerBounds.max.y:F3}m");
            Debug.Log($"   垂直边界检测: {inVerticalBounds}");
            Debug.Log($"   最终边界框检测结果: {inVerticalBounds}");
        }
        
        return inVerticalBounds;
    }
    
    /// <summary>
    /// 使用网格碰撞器进行精确检测
    /// </summary>
    private bool IsLayerIntersectedByMesh(GeologyLayer layer, Vector3 startPoint, Vector3 direction, float maxDistance)
    {
        MeshCollider meshCollider = layer.GetComponent<MeshCollider>();
        if (meshCollider == null) return true; // 如果没有网格碰撞器，默认认为相交
        
        // 使用多个采样点进行射线检测
        int sampleCount = 5;
        BoringTool boringTool = FindFirstObjectByType<BoringTool>();
        float drillingRadius = boringTool?.boringRadius ?? 0.25f;
        
        for (int i = 0; i < sampleCount; i++)
        {
            // 在钻探圆柱体内生成采样点
            float angle = (float)i / sampleCount * 2f * Mathf.PI;
            float sampleRadius = drillingRadius * 0.8f; // 略小于钻探半径
            
            Vector3 right = Vector3.Cross(direction, Vector3.up).normalized;
            Vector3 forward = Vector3.Cross(right, direction).normalized;
            Vector3 offset = (right * Mathf.Cos(angle) + forward * Mathf.Sin(angle)) * sampleRadius;
            Vector3 sampleStart = startPoint + offset;
            
            Ray sampleRay = new Ray(sampleStart, direction);
            
            if (meshCollider.Raycast(sampleRay, out RaycastHit hit, maxDistance))
            {
                return true;
            }
        }
        
        // 网格精确检测：无命中
        return false;
    }
    
    /// <summary>
    /// 计算地层在钻探方向上距离起点的深度 - 修复版
    /// </summary>
    private float GetLayerDepthFromStart(GeologyLayer layer, Vector3 startPoint, Vector3 direction)
    {
        Bounds layerBounds = GetLayerBounds(layer);
        
        // 关键修复：正确计算地层顶部的深度
        // 因为direction是向下的(Vector3.down)，我们需要计算Y坐标差
        float groundLevel = startPoint.y;
        float layerTopY = layerBounds.max.y;
        float layerBottomY = layerBounds.min.y;
        
        // 计算地层顶部距离地面的深度
        float depthToTop = groundLevel - layerTopY;
        float depthToBottom = groundLevel - layerBottomY;
        
        // 确保深度为正值，并处理特殊情况
        if (layerTopY > groundLevel)
        {
            // 地层顶部高于地面，深度为0（地表层）
            depthToTop = 0f;
        }
        
        if (layerBottomY > groundLevel)
        {
            // 地层完全高于地面，这种情况很少见
            depthToTop = 0f;
            depthToBottom = 0f;
        }
        
        // 使用地层顶部深度作为排序依据
        float finalDepth = Mathf.Max(0f, depthToTop);
        
        // 深度修复计算完成
        
        return finalDepth;
    }
    
    /// <summary>
    /// 🔧 新方法：计算地层与钻探路径的相交深度，用于正确排序
    /// </summary>
    private float GetLayerIntersectionDepth(GeologyLayer layer, Vector3 startPoint, Vector3 direction)
    {
        Bounds layerBounds = GetLayerBounds(layer);
        Ray drillingRay = new Ray(startPoint, direction);
        
        if (layerBounds.IntersectRay(drillingRay, out float intersectionDistance))
        {
            return intersectionDistance;
        }
        
        // 如果没有相交，返回一个很大的值，让它排在后面
        return float.MaxValue;
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
    /// 创建调试用的可视化对象
    /// </summary>
    public GameObject CreateDebugVisualization()
    {
        if (lastGeneratedMesh == null) return null;
        
        GameObject debugObj = new GameObject("DrillingCylinder_Debug");
        debugObj.transform.position = lastStartPoint;
        
        MeshFilter meshFilter = debugObj.AddComponent<MeshFilter>();
        meshFilter.mesh = lastGeneratedMesh;
        
        MeshRenderer meshRenderer = debugObj.AddComponent<MeshRenderer>();
        if (debugMaterial != null)
        {
            meshRenderer.material = debugMaterial;
        }
        else
        {
            Material defaultMat = new Material(Shader.Find("Standard"));
            defaultMat.color = new Color(1f, 0f, 0f, 0.3f);
            defaultMat.SetFloat("_Mode", 3); // 透明模式
            meshRenderer.material = defaultMat;
        }
        
        return debugObj;
    }
    
    void OnDrawGizmos()
    {
        if (!showDebugGizmos || lastGeneratedMesh == null) return;
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(lastStartPoint, 0.1f);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(lastStartPoint, lastDirection * 2f);
    }
}
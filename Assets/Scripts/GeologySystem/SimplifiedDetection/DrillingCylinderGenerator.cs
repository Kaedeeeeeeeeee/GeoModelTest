using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 地层相交数据结构 - 记录地层的入口和出口信息
/// </summary>
[System.Serializable]
public class LayerIntersection
{
    public GeologyLayer layer;          // 地层对象
    public Vector3 entryPoint;          // 入口点
    public Vector3 exitPoint;           // 出口点
    public float entryDistance;         // 入口距离
    public float exitDistance;          // 出口距离
    
    /// <summary>
    /// 地层厚度（出口距离 - 入口距离）
    /// </summary>
    public float Thickness => exitDistance - entryDistance;
    
    /// <summary>
    /// 是否是有效的相交
    /// </summary>
    public bool IsValid => layer != null && exitDistance > entryDistance && Thickness > 0.01f;
    
    /// <summary>
    /// 地层在射线路径上的中心位置
    /// </summary>
    public Vector3 CenterPoint => Vector3.Lerp(entryPoint, exitPoint, 0.5f);
    
    /// <summary>
    /// 地层在射线路径上的中心距离
    /// </summary>
    public float CenterDistance => (entryDistance + exitDistance) * 0.5f;
}

/// <summary>
/// 钻探圆柱体几何生成器 - 简化版
/// 专注于地层检测和厚度计算
/// </summary>
public class DrillingCylinderGenerator : MonoBehaviour
{
    [Header("圆柱体参数")]
    public int radialSegments = 32;     // 圆周分段数
    public int heightSegments = 20;     // 高度分段数
    public bool generateCaps = true;    // 是否生成顶底面
    
    [Header("调试")]
    public bool showDebugGizmos = false;
    public Material debugMaterial;
    
    private Mesh lastGeneratedMesh;
    private Vector3 lastStartPoint;
    private Vector3 lastDirection;
    
    /// <summary>
    /// 创建钻探圆柱体网格
    /// </summary>
    public Mesh CreateDrillingCylinder(Vector3 startPoint, Vector3 direction, float radius, float depth)
    {
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
        if (right.magnitude < 0.1f)
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
        
        lastGeneratedMesh = cylinderMesh;
        return cylinderMesh;
    }
    
    /// <summary>
    /// 获取地层相交信息 - 基于Y坐标差值的简化算法
    /// </summary>
    public List<LayerIntersection> GetLayerIntersections(Vector3 startPoint, Vector3 direction, float maxDistance)
    {
        // 收集钻探位置的所有地层Y坐标
        Vector3 skyPosition = new Vector3(startPoint.x, startPoint.y + 50f, startPoint.z);
        RaycastHit[] allHits = Physics.RaycastAll(skyPosition, Vector3.down, 100f);
        
        // 收集地层数据：<地层, Y坐标>
        Dictionary<GeologyLayer, float> layerYPositions = new Dictionary<GeologyLayer, float>();
        
        foreach (RaycastHit hit in allHits)
        {
            // 跳过非地层对象
            if (hit.collider.name.Contains("DrillTower") || 
                hit.collider.name.Contains("Tower") || 
                hit.collider.name.Contains("Drill") ||
                hit.collider.name.Contains("Platform"))
                continue;
                
            GeologyLayer geoLayer = hit.collider.GetComponent<GeologyLayer>();
            if (geoLayer != null)
            {
                // 只记录最高的Y坐标（地层顶面）
                if (!layerYPositions.ContainsKey(geoLayer) || hit.point.y > layerYPositions[geoLayer])
                {
                    layerYPositions[geoLayer] = hit.point.y;
                }
            }
        }
        
        // 按Y坐标排序（从高到低）
        var sortedLayers = layerYPositions.OrderByDescending(kvp => kvp.Value).ToList();
        
        // 计算每个地层的厚度和在钻探深度内的部分
        List<LayerIntersection> intersections = new List<LayerIntersection>();
        
        for (int i = 0; i < sortedLayers.Count; i++)
        {
            GeologyLayer currentLayer = sortedLayers[i].Key;
            float currentLayerTop = sortedLayers[i].Value;
            float currentLayerBottom;
            
            // 确定地层底部：下一层的顶部，或者钻探范围底部
            if (i < sortedLayers.Count - 1)
            {
                currentLayerBottom = sortedLayers[i + 1].Value;
            }
            else
            {
                // 最后一层，假设延伸到钻探深度之外
                currentLayerBottom = startPoint.y - maxDistance - 10f;
            }
            
            // 计算这个地层在钻探范围内的部分
            float layerEntryDepth = Mathf.Max(0, startPoint.y - currentLayerTop);
            float layerExitDepth = Mathf.Min(maxDistance, startPoint.y - currentLayerBottom);
            
            // 只有在钻探范围内的地层才加入结果
            if (layerEntryDepth < maxDistance && layerExitDepth > 0 && layerExitDepth > layerEntryDepth)
            {
                LayerIntersection intersection = new LayerIntersection();
                intersection.layer = currentLayer;
                intersection.entryDistance = layerEntryDepth;
                intersection.exitDistance = layerExitDepth;
                intersection.entryPoint = startPoint + direction * layerEntryDepth;
                intersection.exitPoint = startPoint + direction * layerExitDepth;
                
                intersections.Add(intersection);
            }
        }
        
        return intersections;
    }
    
    /// <summary>
    /// 获取钻探范围内的地层（向后兼容）
    /// </summary>
    public GeologyLayer[] GetLayersInDrillingRange(Vector3 startPoint, Vector3 direction, float maxDistance)
    {
        var intersections = GetLayerIntersections(startPoint, direction, maxDistance);
        return intersections.Select(i => i.layer).ToArray();
    }
    
    /// <summary>
    /// 生成圆柱体侧面
    /// </summary>
    private void GenerateCylinderSides(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, List<Vector3> normals,
                                     Vector3 startPoint, Vector3 right, Vector3 forward, Vector3 up, float radius, float depth)
    {
        for (int h = 0; h <= heightSegments; h++)
        {
            float heightRatio = (float)h / heightSegments;
            Vector3 heightPosition = startPoint + up * (depth * heightRatio);
            
            for (int r = 0; r < radialSegments; r++)
            {
                float angle = (float)r / radialSegments * 2f * Mathf.PI;
                Vector3 circlePoint = right * Mathf.Cos(angle) + forward * Mathf.Sin(angle);
                
                Vector3 vertex = heightPosition + circlePoint * radius;
                vertices.Add(vertex);
                
                Vector2 uv = new Vector2((float)r / radialSegments, heightRatio);
                uvs.Add(uv);
                
                normals.Add(circlePoint.normalized);
                
                // 生成三角形（除了最后一圈）
                if (h < heightSegments && r < radialSegments)
                {
                    int current = h * radialSegments + r;
                    int next = h * radialSegments + (r + 1) % radialSegments;
                    int currentNext = (h + 1) * radialSegments + r;
                    int nextNext = (h + 1) * radialSegments + (r + 1) % radialSegments;
                    
                    // 第一个三角形
                    triangles.Add(current);
                    triangles.Add(currentNext);
                    triangles.Add(next);
                    
                    // 第二个三角形
                    triangles.Add(next);
                    triangles.Add(currentNext);
                    triangles.Add(nextNext);
                }
            }
        }
    }
    
    /// <summary>
    /// 生成圆柱体顶底面
    /// </summary>
    private void GenerateCylinderCaps(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, List<Vector3> normals,
                                    Vector3 startPoint, Vector3 right, Vector3 forward, Vector3 up, float radius, float depth)
    {
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
    
    void OnDrawGizmosSelected()
    {
        if (showDebugGizmos && lastGeneratedMesh != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireMesh(lastGeneratedMesh, lastStartPoint, Quaternion.LookRotation(lastDirection));
        }
    }
}
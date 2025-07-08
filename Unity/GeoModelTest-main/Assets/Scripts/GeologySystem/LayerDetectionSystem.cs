using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class LayerDetectionSystem : MonoBehaviour
{
    [Header("检测参数")]
    public float sampleResolution = 0.2f; // 采样分辨率，调大一些减少段数
    public int circleSegments = 16; // 圆形截面的分段数，减少计算量
    public LayerMask geologyLayerMask = -1;
    
    [Header("调试")]
    public bool showDebugGizmos = true;
    public Color debugColor = Color.green;
    
    private GeologyLayer[] allLayers;
    private List<LayerSampleSegment> detectedSegments;
    
    void Start()
    {
        InitializeSystem();
    }
    
    void InitializeSystem()
    {
        // 查找所有地层对象
        allLayers = FindObjectsByType<GeologyLayer>(FindObjectsSortMode.None);
        Debug.Log($"地层检测系统初始化完成，找到 {allLayers.Length} 个地层");
        
        detectedSegments = new List<LayerSampleSegment>();
    }
    
    public GeologicalSampleData AnalyzeDrillingSample(Vector3 drillingStart, float drillingDepth, float drillingRadius)
    {
        Debug.Log($"开始分析钻探样本 - 位置: {drillingStart}, 深度: {drillingDepth}, 半径: {drillingRadius}");
        
        // 创建样本数据容器
        GeologicalSampleData sampleData = new GeologicalSampleData(drillingStart, drillingDepth, drillingRadius);
        
        List<LayerSampleSegment> segments = new List<LayerSampleSegment>();
        
        // 分段分析钻探路径 - 使用相对深度
        for (float relativeDepth = 0; relativeDepth < drillingDepth; relativeDepth += sampleResolution)
        {
            Vector3 sectionCenter = drillingStart + Vector3.down * relativeDepth;
            LayerSampleSegment segment = AnalyzeDepthSection(sectionCenter, relativeDepth, drillingRadius);
            
            if (segment.layersInSection.Length > 0)
            {
                segments.Add(segment);
            }
        }
        
        // 优化相邻相同段
        segments = OptimizeSegments(segments);
        
        sampleData.segments = segments.ToArray();
        sampleData.layerStats = CalculateLayerStatistics(segments);
        
        Debug.Log($"样本分析完成，共 {segments.Count} 个段，涉及 {sampleData.layerStats.Length} 种地层");
        
        return sampleData;
    }
    
    LayerSampleSegment AnalyzeDepthSection(Vector3 sectionCenter, float depth, float radius)
    {
        LayerSampleSegment segment = new LayerSampleSegment(depth, sectionCenter, sampleResolution);
        
        // 创建圆形截面用于检测
        Vector2[] sectionCircle = GeometryUtils.CircleToPolygon(Vector2.zero, radius, circleSegments);
        
        List<LayerInfo> layersFound = new List<LayerInfo>();
        List<ContactInterface> interfaces = new List<ContactInterface>();
        
        // 检测每个地层与截面的交集
        foreach (GeologyLayer layer in allLayers)
        {
            LayerInfo layerInfo = AnalyzeLayerIntersection(layer, sectionCenter, sectionCircle, radius, depth);
            if (layerInfo != null && layerInfo.areaPercentage > 0.001f) // 忽略极小的交集
            {
                layersFound.Add(layerInfo);
            }
        }
        
        // 分析地层间的接触关系
        if (layersFound.Count > 1)
        {
            interfaces = AnalyzeLayerContacts(layersFound, sectionCenter);
        }
        
        segment.layersInSection = layersFound.ToArray();
        segment.interfaces = interfaces.ToArray();
        
        return segment;
    }
    
    LayerInfo AnalyzeLayerIntersection(GeologyLayer layer, Vector3 sectionCenter, Vector2[] sectionCircle, float radius, float depth)
    {
        if (layer == null) return null;
        
        MeshCollider meshCollider = layer.GetComponent<MeshCollider>();
        Bounds layerBounds = GetLayerBounds(layer);
        
        // 首先检查截面是否与地层边界相交
        if (!layerBounds.Contains(sectionCenter))
        {
            // 如果截面中心不在边界内，检查是否有部分相交
            Vector3 closestPoint = layerBounds.ClosestPoint(sectionCenter);
            if (Vector3.Distance(sectionCenter, closestPoint) > radius)
            {
                return null; // 完全不相交
            }
        }
        
        // 简化检测：如果截面在地层边界内，认为整个截面都属于该地层
        float areaPercentage = 1.0f;
        
        // 如果有MeshCollider，进行更精确的检测
        if (meshCollider != null)
        {
            int hitCount = 0;
            for (int i = 0; i < circleSegments; i++)
            {
                Vector2 localPoint = sectionCircle[i];
                Vector3 worldPoint = sectionCenter + new Vector3(localPoint.x, 0, localPoint.y);
                
                if (IsPointInLayerBounds(worldPoint, layerBounds))
                {
                    hitCount++;
                }
            }
            
            areaPercentage = (float)hitCount / circleSegments;
        }
        
        // 如果交集太小，忽略
        if (areaPercentage < 0.1f) return null;
        
        // 创建简单的圆形边界
        Vector2[] intersectionPolygon = GeometryUtils.CircleToPolygon(Vector2.zero, radius * areaPercentage, 16);
        
        // 创建LayerInfo
        LayerInfo layerInfo = layer.CreateLayerInfo(intersectionPolygon, areaPercentage, sectionCenter);
        layerInfo.centerPoint = sectionCenter;
        layerInfo.strikeDirection = layer.strikeDirection;
        layerInfo.dipAngle = layer.dipAngle;
        
        // Debug.Log($"地层 {layer.layerName} 在相对深度 {depth:F2}m 处相交，占比: {areaPercentage:F2}"); // 减少日志输出
        
        return layerInfo;
    }
    
    bool IsPointInLayerBounds(Vector3 point, Bounds bounds)
    {
        return bounds.Contains(point);
    }
    
    Bounds GetLayerBounds(GeologyLayer layer)
    {
        MeshRenderer renderer = layer.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            return renderer.bounds;
        }
        
        // 回退到变换边界
        return new Bounds(layer.transform.position, layer.transform.localScale);
    }
    
    bool IsPointInsideLayer(Vector3 point, MeshCollider meshCollider)
    {
        if (meshCollider == null) return false;
        
        try 
        {
            // 对于凸形网格，使用ClosestPoint
            if (meshCollider.convex)
            {
                Vector3 closestPoint = meshCollider.ClosestPoint(point);
                float distance = Vector3.Distance(point, closestPoint);
                return distance < 0.1f;
            }
            else
            {
                // 对于非凸网格，使用射线检测方法
                return IsPointInsideLayerRaycast(point, meshCollider);
            }
        }
        catch (System.Exception)
        {
            // 如果ClosestPoint失败，回退到射线检测
            return IsPointInsideLayerRaycast(point, meshCollider);
        }
    }
    
    bool IsPointInsideLayerRaycast(Vector3 point, MeshCollider meshCollider)
    {
        // 使用多方向射线检测
        Vector3[] directions = {
            Vector3.up, Vector3.down, 
            Vector3.left, Vector3.right,
            Vector3.forward, Vector3.back
        };
        
        int hitCount = 0;
        foreach (Vector3 direction in directions)
        {
            Ray ray = new Ray(point, direction);
            RaycastHit hit;
            
            if (meshCollider.Raycast(ray, out hit, 10f))
            {
                hitCount++;
            }
        }
        
        // 如果大部分方向都击中了地层，认为点在内部
        return hitCount >= 3;
    }
    
    Vector2[] ConvexHull(List<Vector2> points)
    {
        // 简化的凸包算法
        if (points.Count <= 3) return points.ToArray();
        
        // 找到最左下角的点
        Vector2 start = points.OrderBy(p => p.x).ThenBy(p => p.y).First();
        
        List<Vector2> hull = new List<Vector2>();
        Vector2 current = start;
        
        do
        {
            hull.Add(current);
            Vector2 next = points[0];
            
            for (int i = 1; i < points.Count; i++)
            {
                if (next == current || IsLeftTurn(current, next, points[i]))
                {
                    next = points[i];
                }
            }
            
            current = next;
        } while (current != start && hull.Count < points.Count);
        
        return hull.ToArray();
    }
    
    bool IsLeftTurn(Vector2 a, Vector2 b, Vector2 c)
    {
        return ((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x)) > 0;
    }
    
    List<ContactInterface> AnalyzeLayerContacts(List<LayerInfo> layers, Vector3 sectionCenter)
    {
        List<ContactInterface> contacts = new List<ContactInterface>();
        
        for (int i = 0; i < layers.Count; i++)
        {
            for (int j = i + 1; j < layers.Count; j++)
            {
                ContactInterface contact = AnalyzeContact(layers[i], layers[j], sectionCenter);
                if (contact != null)
                {
                    contacts.Add(contact);
                }
            }
        }
        
        return contacts;
    }
    
    ContactInterface AnalyzeContact(LayerInfo layerA, LayerInfo layerB, Vector3 sectionCenter)
    {
        // 分析两个地层的接触关系
        ContactInterface contact = new ContactInterface();
        contact.layerA = layerA.layer;
        contact.layerB = layerB.layer;
        
        // 计算接触线（简化为两个边界的交线）
        contact.contactLine = GeometryUtils.IntersectPolygons(layerA.boundaryShape, layerB.boundaryShape);
        
        if (contact.contactLine.Length == 0) return null;
        
        // 计算接触角度
        Vector3 normalA = layerA.normalDirection;
        Vector3 normalB = layerB.normalDirection;
        contact.contactAngle = Vector3.Angle(normalA, normalB);
        
        // 确定接触类型
        contact.contactType = DetermineContactType(layerA, layerB, contact.contactAngle);
        
        return contact;
    }
    
    ContactType DetermineContactType(LayerInfo layerA, LayerInfo layerB, float angle)
    {
        // 根据角度和地层类型确定接触类型
        if (angle < 10f)
        {
            return ContactType.Conformable; // 整合接触
        }
        else if (angle > 45f)
        {
            return ContactType.Unconformable; // 不整合接触
        }
        else
        {
            return ContactType.Disconformable; // 假整合接触
        }
    }
    
    List<LayerSampleSegment> OptimizeSegments(List<LayerSampleSegment> segments)
    {
        List<LayerSampleSegment> optimized = new List<LayerSampleSegment>();
        
        if (segments.Count == 0) return optimized;
        
        LayerSampleSegment current = segments[0];
        float accumulatedHeight = current.segmentHeight;
        
        for (int i = 1; i < segments.Count; i++)
        {
            LayerSampleSegment next = segments[i];
            
            // 检查是否可以合并（相同的地层组合）
            if (IsSameLayerComposition(current, next))
            {
                accumulatedHeight += next.segmentHeight;
            }
            else
            {
                // 保存当前段并开始新段
                current.segmentHeight = accumulatedHeight;
                optimized.Add(current);
                
                current = next;
                accumulatedHeight = next.segmentHeight;
            }
        }
        
        // 添加最后一段
        current.segmentHeight = accumulatedHeight;
        optimized.Add(current);
        
        return optimized;
    }
    
    bool IsSameLayerComposition(LayerSampleSegment a, LayerSampleSegment b)
    {
        if (a.layersInSection.Length != b.layersInSection.Length) return false;
        
        for (int i = 0; i < a.layersInSection.Length; i++)
        {
            bool found = false;
            for (int j = 0; j < b.layersInSection.Length; j++)
            {
                if (a.layersInSection[i].layer == b.layersInSection[j].layer &&
                    Mathf.Abs(a.layersInSection[i].areaPercentage - b.layersInSection[j].areaPercentage) < 0.1f)
                {
                    found = true;
                    break;
                }
            }
            if (!found) return false;
        }
        
        return true;
    }
    
    LayerStatistics[] CalculateLayerStatistics(List<LayerSampleSegment> segments)
    {
        Dictionary<GeologyLayer, LayerStatistics> stats = new Dictionary<GeologyLayer, LayerStatistics>();
        
        foreach (var segment in segments)
        {
            foreach (var layerInfo in segment.layersInSection)
            {
                if (!stats.ContainsKey(layerInfo.layer))
                {
                    stats[layerInfo.layer] = new LayerStatistics
                    {
                        layerName = layerInfo.layer.layerName,
                        layerType = layerInfo.layer.layerType,
                        totalThickness = 0f,
                        numberOfSegments = 0,
                        averageDipAngle = 0f
                    };
                }
                
                LayerStatistics stat = stats[layerInfo.layer];
                stat.totalThickness += segment.segmentHeight * layerInfo.areaPercentage;
                stat.numberOfSegments++;
                stat.averageDipAngle += layerInfo.dipAngle;
            }
        }
        
        // 计算平均值
        foreach (var stat in stats.Values)
        {
            stat.averageDipAngle /= stat.numberOfSegments;
        }
        
        return stats.Values.ToArray();
    }
    
    void OnDrawGizmos()
    {
        if (!showDebugGizmos || detectedSegments == null) return;
        
        Gizmos.color = debugColor;
        foreach (var segment in detectedSegments)
        {
            Gizmos.DrawWireSphere(segment.sampleCenter, 0.1f);
        }
    }
}
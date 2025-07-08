using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class LayerSampleSegment
{
    public float depth;
    public LayerInfo[] layersInSection;
    public ContactInterface[] interfaces;
    public Vector3 sampleCenter;
    public float segmentHeight;
    
    public LayerSampleSegment(float depth, Vector3 center, float height)
    {
        this.depth = depth;
        this.sampleCenter = center;
        this.segmentHeight = height;
        this.layersInSection = new LayerInfo[0];
        this.interfaces = new ContactInterface[0];
    }
}

[System.Serializable]
public class LayerInfo
{
    public GeologyLayer layer;
    public float areaPercentage;
    public Vector2[] boundaryShape;
    public Vector3 normalDirection;
    public float thickness;
    public Color color;
    public Material material;
    
    // 额外的几何信息
    public Vector3 strikeDirection;
    public float dipAngle;
    public Vector3 centerPoint;
    
    public LayerInfo()
    {
        boundaryShape = new Vector2[0];
    }
}

[System.Serializable]
public class ContactInterface
{
    public GeologyLayer layerA;
    public GeologyLayer layerB;
    public Vector2[] contactLine;
    public float contactAngle;
    public ContactType contactType;
    public Vector3 contactNormal;
    
    public ContactInterface()
    {
        contactLine = new Vector2[0];
    }
}

public enum ContactType
{
    Conformable,        // 整合接触
    Unconformable,      // 不整合接触
    Disconformable,     // 假整合接触
    Intrusive,          // 侵入接触
    Fault,              // 断层接触
    Gradational         // 渐变接触
}

[System.Serializable]
public class GeologicalSampleData
{
    public string sampleID;
    public Vector3 drillingPosition;
    public float drillingDepth;
    public float drillingRadius;
    public LayerSampleSegment[] segments;
    public System.DateTime collectionTime;
    
    [Header("分析结果")]
    public LayerStatistics[] layerStats;
    public string[] identifiedFormations;
    public float totalLayers;
    
    public GeologicalSampleData(Vector3 position, float depth, float radius)
    {
        sampleID = System.Guid.NewGuid().ToString();
        drillingPosition = position;
        drillingDepth = depth;
        drillingRadius = radius;
        collectionTime = System.DateTime.Now;
        segments = new LayerSampleSegment[0];
    }
}

[System.Serializable]
public class LayerStatistics
{
    public string layerName;
    public LayerType layerType;
    public float totalThickness;
    public float percentageOfSample;
    public int numberOfSegments;
    public float averageDipAngle;
    public string dominantStrike;
}

// 几何工具类
public static class GeometryUtils
{
    public static bool PointInPolygon(Vector2 point, Vector2[] polygon)
    {
        int count = polygon.Length;
        bool inside = false;
        
        for (int i = 0, j = count - 1; i < count; j = i++)
        {
            Vector2 pi = polygon[i];
            Vector2 pj = polygon[j];
            
            if (((pi.y > point.y) != (pj.y > point.y)) &&
                (point.x < (pj.x - pi.x) * (point.y - pi.y) / (pj.y - pi.y) + pi.x))
            {
                inside = !inside;
            }
        }
        
        return inside;
    }
    
    public static float PolygonArea(Vector2[] polygon)
    {
        if (polygon.Length < 3) return 0f;
        
        float area = 0f;
        for (int i = 0; i < polygon.Length; i++)
        {
            int j = (i + 1) % polygon.Length;
            area += polygon[i].x * polygon[j].y;
            area -= polygon[j].x * polygon[i].y;
        }
        
        return Mathf.Abs(area) / 2f;
    }
    
    public static Vector2[] CircleToPolygon(Vector2 center, float radius, int segments = 16)
    {
        Vector2[] points = new Vector2[segments];
        float angleStep = 2f * Mathf.PI / segments;
        
        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep;
            points[i] = center + new Vector2(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius
            );
        }
        
        return points;
    }
    
    public static Vector2[] IntersectPolygons(Vector2[] polyA, Vector2[] polyB)
    {
        // 简化的多边形交集算法
        // 在实际应用中可能需要更复杂的算法如Sutherland-Hodgman裁剪
        List<Vector2> intersection = new List<Vector2>();
        
        // 检查polyA的点是否在polyB内
        foreach (Vector2 point in polyA)
        {
            if (PointInPolygon(point, polyB))
            {
                intersection.Add(point);
            }
        }
        
        // 检查polyB的点是否在polyA内
        foreach (Vector2 point in polyB)
        {
            if (PointInPolygon(point, polyA))
            {
                intersection.Add(point);
            }
        }
        
        return intersection.ToArray();
    }
}
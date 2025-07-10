using UnityEngine;
using UnityEditor;

/// <summary>
/// dem.003调试测试工具
/// </summary>
public class Dem003DebugTest
{
    [MenuItem("Tools/Debug dem.003 Detection")]
    public static void DebugDem003Detection()
    {
        Debug.Log("🔍 开始dem.003调试分析");
        
        // 查找所有地层
        GeologyLayer[] allLayers = Object.FindObjectsByType<GeologyLayer>(FindObjectsSortMode.None);
        GeologyLayer dem003 = null;
        GeologyLayer dem004 = null;
        
        foreach (var layer in allLayers)
        {
            if (layer.layerName == "dem.003")
            {
                dem003 = layer;
            }
            else if (layer.layerName == "dem.004")
            {
                dem004 = layer;
            }
        }
        
        if (dem003 == null)
        {
            Debug.LogError("❌ 未找到dem.003地层");
            return;
        }
        
        if (dem004 == null)
        {
            Debug.LogError("❌ 未找到dem.004地层");
            return;
        }
        
        // 获取边界框信息
        Bounds dem003Bounds = GetLayerBounds(dem003);
        Bounds dem004Bounds = GetLayerBounds(dem004);
        
        Debug.Log($"📊 dem.003边界框分析:");
        Debug.Log($"   中心: {dem003Bounds.center}");
        Debug.Log($"   尺寸: {dem003Bounds.size}");
        Debug.Log($"   范围: min({dem003Bounds.min}) ~ max({dem003Bounds.max})");
        
        Debug.Log($"📊 dem.004边界框分析:");
        Debug.Log($"   中心: {dem004Bounds.center}");
        Debug.Log($"   尺寸: {dem004Bounds.size}");
        Debug.Log($"   范围: min({dem004Bounds.min}) ~ max({dem004Bounds.max})");
        
        // 模拟钻探点
        Vector3 testDrillingPoint = new Vector3(0, 10, 0); // 假设的钻探点
        Vector3 drillingDirection = Vector3.down;
        float maxDistance = 10f;
        
        Debug.Log($"🎯 测试钻探参数:");
        Debug.Log($"   钻探点: {testDrillingPoint}");
        Debug.Log($"   方向: {drillingDirection}");
        Debug.Log($"   最大距离: {maxDistance}m");
        
        // 测试边界框相交
        TestBoundsIntersection(dem003Bounds, dem004Bounds, testDrillingPoint, drillingDirection, maxDistance);
        
        // 测试水平和垂直边界
        TestHorizontalVerticalBounds(dem003Bounds, dem004Bounds, testDrillingPoint, drillingDirection, maxDistance);
    }
    
    private static void TestBoundsIntersection(Bounds dem003Bounds, Bounds dem004Bounds, Vector3 startPoint, Vector3 direction, float maxDistance)
    {
        Debug.Log($"🔍 边界框相交测试:");
        
        Ray drillingRay = new Ray(startPoint, direction);
        
        // dem.003测试
        bool dem003Intersects = dem003Bounds.IntersectRay(drillingRay, out float dem003Distance);
        Debug.Log($"   dem.003射线相交: {dem003Intersects}, 距离: {dem003Distance}m");
        
        // dem.004测试
        bool dem004Intersects = dem004Bounds.IntersectRay(drillingRay, out float dem004Distance);
        Debug.Log($"   dem.004射线相交: {dem004Intersects}, 距离: {dem004Distance}m");
        
        // 比较分析
        if (dem003Intersects && dem004Intersects)
        {
            Debug.Log($"   距离比较: dem.003({dem003Distance:F3}m) vs dem.004({dem004Distance:F3}m)");
            Debug.Log($"   更近的地层: {(dem003Distance < dem004Distance ? "dem.003" : "dem.004")}");
        }
        else if (!dem003Intersects && dem004Intersects)
        {
            Debug.LogWarning($"⚠️ dem.003未相交但dem.004相交，这可能是问题所在");
        }
    }
    
    private static void TestHorizontalVerticalBounds(Bounds dem003Bounds, Bounds dem004Bounds, Vector3 startPoint, Vector3 direction, float maxDistance)
    {
        Debug.Log($"🔍 水平/垂直边界测试:");
        
        Vector3 endPoint = startPoint + direction * maxDistance;
        
        // 水平边界测试 (XZ平面)
        bool dem003HorizontalContains = IsPointInHorizontalBounds(startPoint, dem003Bounds);
        bool dem004HorizontalContains = IsPointInHorizontalBounds(startPoint, dem004Bounds);
        
        Debug.Log($"   水平边界包含测试:");
        Debug.Log($"   dem.003包含钻探点XZ: {dem003HorizontalContains}");
        Debug.Log($"   dem.004包含钻探点XZ: {dem004HorizontalContains}");
        
        // 垂直边界测试 (Y轴)
        float drillingTop = Mathf.Max(startPoint.y, endPoint.y);
        float drillingBottom = Mathf.Min(startPoint.y, endPoint.y);
        
        bool dem003VerticalIntersects = !(dem003Bounds.max.y < drillingBottom || dem003Bounds.min.y > drillingTop);
        bool dem004VerticalIntersects = !(dem004Bounds.max.y < drillingBottom || dem004Bounds.min.y > drillingTop);
        
        Debug.Log($"   垂直边界相交测试:");
        Debug.Log($"   钻探Y范围: {drillingBottom:F3}m ~ {drillingTop:F3}m");
        Debug.Log($"   dem.003垂直相交: {dem003VerticalIntersects} (Y范围: {dem003Bounds.min.y:F3}m ~ {dem003Bounds.max.y:F3}m)");
        Debug.Log($"   dem.004垂直相交: {dem004VerticalIntersects} (Y范围: {dem004Bounds.min.y:F3}m ~ {dem004Bounds.max.y:F3}m)");
        
        // 综合分析
        bool dem003ShouldPass = dem003HorizontalContains && dem003VerticalIntersects;
        bool dem004ShouldPass = dem004HorizontalContains && dem004VerticalIntersects;
        
        Debug.Log($"📈 综合分析结果:");
        Debug.Log($"   dem.003应该通过检测: {dem003ShouldPass}");
        Debug.Log($"   dem.004应该通过检测: {dem004ShouldPass}");
        
        if (!dem003ShouldPass && dem004ShouldPass)
        {
            Debug.LogWarning($"⚠️ 发现问题: dem.003不应该被忽略，但检测条件显示它会被排除");
            if (!dem003HorizontalContains)
            {
                Debug.LogError($"❌ 问题根源: dem.003水平边界不包含钻探点");
                AnalyzeHorizontalBoundsProblem(startPoint, dem003Bounds, dem004Bounds);
            }
            if (!dem003VerticalIntersects)
            {
                Debug.LogError($"❌ 问题根源: dem.003垂直边界不相交");
            }
        }
    }
    
    private static bool IsPointInHorizontalBounds(Vector3 point, Bounds bounds)
    {
        return point.x >= bounds.min.x && point.x <= bounds.max.x &&
               point.z >= bounds.min.z && point.z <= bounds.max.z;
    }
    
    private static void AnalyzeHorizontalBoundsProblem(Vector3 drillingPoint, Bounds dem003Bounds, Bounds dem004Bounds)
    {
        Debug.Log($"🔍 水平边界问题深度分析:");
        
        Vector2 pointXZ = new Vector2(drillingPoint.x, drillingPoint.z);
        Vector2 dem003CenterXZ = new Vector2(dem003Bounds.center.x, dem003Bounds.center.z);
        Vector2 dem004CenterXZ = new Vector2(dem004Bounds.center.x, dem004Bounds.center.z);
        
        float dem003DistanceXZ = Vector2.Distance(pointXZ, dem003CenterXZ);
        float dem004DistanceXZ = Vector2.Distance(pointXZ, dem004CenterXZ);
        
        Debug.Log($"   钻探点XZ: {pointXZ}");
        Debug.Log($"   dem.003中心XZ: {dem003CenterXZ}, 距离: {dem003DistanceXZ:F3}m");
        Debug.Log($"   dem.004中心XZ: {dem004CenterXZ}, 距离: {dem004DistanceXZ:F3}m");
        
        // 计算边界范围
        Vector2 dem003MinXZ = new Vector2(dem003Bounds.min.x, dem003Bounds.min.z);
        Vector2 dem003MaxXZ = new Vector2(dem003Bounds.max.x, dem003Bounds.max.z);
        Vector2 dem004MinXZ = new Vector2(dem004Bounds.min.x, dem004Bounds.min.z);
        Vector2 dem004MaxXZ = new Vector2(dem004Bounds.max.x, dem004Bounds.max.z);
        
        Debug.Log($"   dem.003 XZ范围: ({dem003MinXZ.x:F3}, {dem003MinXZ.y:F3}) ~ ({dem003MaxXZ.x:F3}, {dem003MaxXZ.y:F3})");
        Debug.Log($"   dem.004 XZ范围: ({dem004MinXZ.x:F3}, {dem004MinXZ.y:F3}) ~ ({dem004MaxXZ.x:F3}, {dem004MaxXZ.y:F3})");
        
        // 分析差距
        float dem003XMargin = Mathf.Min(pointXZ.x - dem003MinXZ.x, dem003MaxXZ.x - pointXZ.x);
        float dem003ZMargin = Mathf.Min(pointXZ.y - dem003MinXZ.y, dem003MaxXZ.y - pointXZ.y);
        float dem004XMargin = Mathf.Min(pointXZ.x - dem004MinXZ.x, dem004MaxXZ.x - pointXZ.x);
        float dem004ZMargin = Mathf.Min(pointXZ.y - dem004MinXZ.y, dem004MaxXZ.y - pointXZ.y);
        
        Debug.Log($"   dem.003边界距离: X轴{dem003XMargin:F3}m, Z轴{dem003ZMargin:F3}m");
        Debug.Log($"   dem.004边界距离: X轴{dem004XMargin:F3}m, Z轴{dem004ZMargin:F3}m");
        
        if (dem003XMargin < 0 || dem003ZMargin < 0)
        {
            Debug.LogError($"❌ dem.003边界问题: 钻探点在边界外 (X差距: {dem003XMargin:F3}m, Z差距: {dem003ZMargin:F3}m)");
        }
    }
    
    private static Bounds GetLayerBounds(GeologyLayer layer)
    {
        MeshRenderer renderer = layer.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            return renderer.bounds;
        }
        
        return new Bounds(layer.transform.position, layer.transform.localScale);
    }
}
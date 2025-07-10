using UnityEngine;

/// <summary>
/// dem.003运行时调试器
/// 在钻探时提供详细的调试信息
/// </summary>
public class Dem003RuntimeDebugger : MonoBehaviour
{
    [Header("调试设置")]
    public bool enableDebug = true;
    public KeyCode debugKey = KeyCode.F8;
    
    private void Update()
    {
        if (enableDebug && Input.GetKeyDown(debugKey))
        {
            PerformDrillingDebugTest();
        }
    }
    
    public void PerformDrillingDebugTest()
    {
        Debug.Log("🔍 ===========================================");
        Debug.Log("🔍 dem.003运行时调试测试开始");
        Debug.Log("🔍 ===========================================");
        
        // 查找地层
        GeologyLayer[] allLayers = FindObjectsByType<GeologyLayer>(FindObjectsSortMode.None);
        Debug.Log($"🔍 发现地层总数: {allLayers.Length}");
        
        GeologyLayer dem003 = null;
        GeologyLayer dem004 = null;
        
        foreach (var layer in allLayers)
        {
            Debug.Log($"🔍 地层: {layer.layerName}");
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
        
        Debug.Log("✅ 找到dem.003和dem.004地层");
        
        // 获取DrillingCylinderGenerator
        DrillingCylinderGenerator cylinderGen = FindFirstObjectByType<DrillingCylinderGenerator>();
        if (cylinderGen == null)
        {
            Debug.LogError("❌ 未找到DrillingCylinderGenerator");
            return;
        }
        
        // 使用玩家当前位置作为钻探点
        Transform playerTransform = Camera.main?.transform;
        if (playerTransform == null)
        {
            playerTransform = this.transform;
        }
        
        Vector3 drillingPoint = playerTransform.position;
        Vector3 direction = Vector3.down;
        float maxDistance = 10f;
        
        Debug.Log($"🎯 钻探参数:");
        Debug.Log($"   钻探点: {drillingPoint}");
        Debug.Log($"   方向: {direction}");
        Debug.Log($"   最大距离: {maxDistance}m");
        
        // 详细分析dem.003和dem.004的边界框
        AnalyzeLayerBounds(dem003, dem004, drillingPoint);
        
        // 测试地层检测
        Debug.Log("🔍 开始地层检测测试...");
        GeologyLayer[] detectedLayers = cylinderGen.GetLayersInDrillingRange(drillingPoint, direction, maxDistance);
        
        Debug.Log($"📊 检测结果: 发现 {detectedLayers.Length} 个地层");
        
        bool dem003Found = false;
        bool dem004Found = false;
        
        foreach (var layer in detectedLayers)
        {
            Debug.Log($"✅ 检测到地层: {layer.layerName}");
            if (layer.layerName == "dem.003") dem003Found = true;
            if (layer.layerName == "dem.004") dem004Found = true;
        }
        
        Debug.Log($"📈 检测总结:");
        Debug.Log($"   dem.003是否被检测到: {dem003Found}");
        Debug.Log($"   dem.004是否被检测到: {dem004Found}");
        
        if (!dem003Found && dem004Found)
        {
            Debug.LogError("❌ 问题确认: dem.003被忽略而dem.004被检测到");
            PerformDetailedAnalysis(dem003, dem004, drillingPoint, direction, maxDistance);
        }
        else if (dem003Found)
        {
            Debug.Log("✅ dem.003正常被检测到");
        }
        
        Debug.Log("🔍 ===========================================");
        Debug.Log("🔍 dem.003运行时调试测试结束");
        Debug.Log("🔍 ===========================================");
    }
    
    private void AnalyzeLayerBounds(GeologyLayer dem003, GeologyLayer dem004, Vector3 drillingPoint)
    {
        Bounds dem003Bounds = GetLayerBounds(dem003);
        Bounds dem004Bounds = GetLayerBounds(dem004);
        
        Debug.Log($"📊 dem.003详细边界框信息:");
        Debug.Log($"   中心: {dem003Bounds.center}");
        Debug.Log($"   尺寸: {dem003Bounds.size}");
        Debug.Log($"   最小值: {dem003Bounds.min}");
        Debug.Log($"   最大值: {dem003Bounds.max}");
        
        Debug.Log($"📊 dem.004详细边界框信息:");
        Debug.Log($"   中心: {dem004Bounds.center}");
        Debug.Log($"   尺寸: {dem004Bounds.size}");
        Debug.Log($"   最小值: {dem004Bounds.min}");
        Debug.Log($"   最大值: {dem004Bounds.max}");
        
        // 计算钻探点与地层的关系
        Debug.Log($"📏 钻探点与地层关系分析:");
        
        // dem.003分析
        Vector3 dem003Distance = drillingPoint - dem003Bounds.center;
        bool dem003Contains = dem003Bounds.Contains(drillingPoint);
        Debug.Log($"   dem.003 - 距离中心: {dem003Distance}, 包含钻探点: {dem003Contains}");
        
        // dem.004分析
        Vector3 dem004Distance = drillingPoint - dem004Bounds.center;
        bool dem004Contains = dem004Bounds.Contains(drillingPoint);
        Debug.Log($"   dem.004 - 距离中心: {dem004Distance}, 包含钻探点: {dem004Contains}");
        
        // 水平距离分析
        float dem003HorizontalDistance = Vector2.Distance(
            new Vector2(drillingPoint.x, drillingPoint.z),
            new Vector2(dem003Bounds.center.x, dem003Bounds.center.z)
        );
        float dem004HorizontalDistance = Vector2.Distance(
            new Vector2(drillingPoint.x, drillingPoint.z),
            new Vector2(dem004Bounds.center.x, dem004Bounds.center.z)
        );
        
        Debug.Log($"📏 水平距离分析:");
        Debug.Log($"   dem.003水平距离: {dem003HorizontalDistance:F3}m");
        Debug.Log($"   dem.004水平距离: {dem004HorizontalDistance:F3}m");
        Debug.Log($"   更近的地层: {(dem003HorizontalDistance < dem004HorizontalDistance ? "dem.003" : "dem.004")}");
    }
    
    private void PerformDetailedAnalysis(GeologyLayer dem003, GeologyLayer dem004, Vector3 drillingPoint, Vector3 direction, float maxDistance)
    {
        Debug.Log("🔍 开始详细问题分析...");
        
        Bounds dem003Bounds = GetLayerBounds(dem003);
        Bounds dem004Bounds = GetLayerBounds(dem004);
        
        // 模拟预筛选过程
        Vector3 endPoint = drillingPoint + direction * maxDistance;
        
        // 测试DoesLayerIntersectDrillingPath
        bool dem003PathIntersects = TestLayerPathIntersection(dem003Bounds, drillingPoint, endPoint);
        bool dem004PathIntersects = TestLayerPathIntersection(dem004Bounds, drillingPoint, endPoint);
        
        Debug.Log($"🔍 钻探路径相交测试:");
        Debug.Log($"   dem.003路径相交: {dem003PathIntersects}");
        Debug.Log($"   dem.004路径相交: {dem004PathIntersects}");
        
        if (!dem003PathIntersects && dem004PathIntersects)
        {
            Debug.LogError("❌ 问题发现: dem.003在预筛选阶段就被排除了");
            AnalyzePrefilterFailure(dem003Bounds, drillingPoint, endPoint);
        }
        
        // 测试水平和垂直边界
        TestBoundaryConditions(dem003Bounds, dem004Bounds, drillingPoint, direction, maxDistance);
    }
    
    private bool TestLayerPathIntersection(Bounds layerBounds, Vector3 startPoint, Vector3 endPoint)
    {
        Vector3 direction = (endPoint - startPoint).normalized;
        float distance = Vector3.Distance(startPoint, endPoint);
        
        Ray drillingRay = new Ray(startPoint, direction);
        bool intersects = layerBounds.IntersectRay(drillingRay, out float enterDistance);
        
        return intersects && enterDistance <= distance;
    }
    
    private void AnalyzePrefilterFailure(Bounds layerBounds, Vector3 startPoint, Vector3 endPoint)
    {
        Debug.Log("🔍 预筛选失败原因分析:");
        
        Vector3 direction = (endPoint - startPoint).normalized;
        float distance = Vector3.Distance(startPoint, endPoint);
        
        Ray drillingRay = new Ray(startPoint, direction);
        bool rayIntersects = layerBounds.IntersectRay(drillingRay, out float enterDistance);
        
        Debug.Log($"   射线相交: {rayIntersects}");
        Debug.Log($"   进入距离: {enterDistance:F3}m");
        Debug.Log($"   钻探距离: {distance:F3}m");
        Debug.Log($"   距离检查: {enterDistance <= distance}");
        
        if (!rayIntersects)
        {
            Debug.LogError("❌ 根本原因: 射线不与边界框相交");
            AnalyzeRayMiss(layerBounds, startPoint, direction);
        }
        else if (enterDistance > distance)
        {
            Debug.LogError($"❌ 根本原因: 相交距离({enterDistance:F3}m)超出钻探距离({distance:F3}m)");
        }
    }
    
    private void AnalyzeRayMiss(Bounds bounds, Vector3 startPoint, Vector3 direction)
    {
        Debug.Log("🔍 射线未命中分析:");
        Debug.Log($"   射线起点: {startPoint}");
        Debug.Log($"   射线方向: {direction}");
        Debug.Log($"   边界框中心: {bounds.center}");
        Debug.Log($"   边界框尺寸: {bounds.size}");
        
        // 检查射线是否从边界框内部开始
        bool startsInside = bounds.Contains(startPoint);
        Debug.Log($"   射线起点在边界框内: {startsInside}");
        
        if (startsInside)
        {
            Debug.LogWarning("⚠️ 射线从边界框内部开始，但IntersectRay返回false，这可能是Unity的边界情况");
        }
        
        // 计算最近点
        Vector3 closestPoint = bounds.ClosestPoint(startPoint);
        float distanceToBox = Vector3.Distance(startPoint, closestPoint);
        Debug.Log($"   到边界框最近距离: {distanceToBox:F3}m");
        Debug.Log($"   最近点: {closestPoint}");
    }
    
    private void TestBoundaryConditions(Bounds dem003Bounds, Bounds dem004Bounds, Vector3 drillingPoint, Vector3 direction, float maxDistance)
    {
        Debug.Log("🔍 边界条件测试:");
        
        BoringTool boringTool = FindFirstObjectByType<BoringTool>();
        float drillingRadius = boringTool?.boringRadius ?? 0.25f;
        
        // 水平边界测试
        bool dem003Horizontal = TestHorizontalBounds(dem003Bounds, drillingPoint, drillingRadius);
        bool dem004Horizontal = TestHorizontalBounds(dem004Bounds, drillingPoint, drillingRadius);
        
        Debug.Log($"   水平边界测试:");
        Debug.Log($"   dem.003水平通过: {dem003Horizontal}");
        Debug.Log($"   dem.004水平通过: {dem004Horizontal}");
        
        // 垂直边界测试
        Vector3 endPoint = drillingPoint + direction * maxDistance;
        bool dem003Vertical = TestVerticalBounds(dem003Bounds, drillingPoint, endPoint);
        bool dem004Vertical = TestVerticalBounds(dem004Bounds, drillingPoint, endPoint);
        
        Debug.Log($"   垂直边界测试:");
        Debug.Log($"   dem.003垂直通过: {dem003Vertical}");
        Debug.Log($"   dem.004垂直通过: {dem004Vertical}");
        
        // 综合结果
        bool dem003ShouldPass = dem003Horizontal && dem003Vertical;
        bool dem004ShouldPass = dem004Horizontal && dem004Vertical;
        
        Debug.Log($"📈 边界条件综合结果:");
        Debug.Log($"   dem.003应该通过: {dem003ShouldPass}");
        Debug.Log($"   dem.004应该通过: {dem004ShouldPass}");
    }
    
    private bool TestHorizontalBounds(Bounds bounds, Vector3 point, float radius)
    {
        Vector2 pointXZ = new Vector2(point.x, point.z);
        Vector2 centerXZ = new Vector2(bounds.center.x, bounds.center.z);
        Vector2 sizeXZ = new Vector2(bounds.size.x, bounds.size.z);
        
        Rect layerRect = new Rect(
            centerXZ.x - sizeXZ.x * 0.5f - radius,
            centerXZ.y - sizeXZ.y * 0.5f - radius,
            sizeXZ.x + radius * 2f,
            sizeXZ.y + radius * 2f
        );
        
        return layerRect.Contains(pointXZ);
    }
    
    private bool TestVerticalBounds(Bounds bounds, Vector3 startPoint, Vector3 endPoint)
    {
        float drillingTop = Mathf.Max(startPoint.y, endPoint.y);
        float drillingBottom = Mathf.Min(startPoint.y, endPoint.y);
        
        return !(bounds.max.y < drillingBottom || bounds.min.y > drillingTop);
    }
    
    private Bounds GetLayerBounds(GeologyLayer layer)
    {
        MeshRenderer renderer = layer.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            return renderer.bounds;
        }
        
        return new Bounds(layer.transform.position, layer.transform.localScale);
    }
}
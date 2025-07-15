using UnityEngine;

/// <summary>
/// 简化的钻塔调试器 - 避免输入系统冲突
/// 注意：默认已禁用自动调试输出，需要手动启用
/// 使用方法：在Inspector中启用 enableDebugMode 和 enableAutoDetection
/// 或者使用右键菜单的"手动检测地面"功能
/// </summary>
public class DrillTowerDebuggerSimple : MonoBehaviour
{
    [Header("调试设置")]
    public bool enableDebugMode = false; // 默认关闭自动调试
    public bool showRaycastInfo = false; // 默认关闭射线信息
    public bool enableAutoDetection = false; // 新增：控制自动检测
    public float debugRayLength = 50f;
    
    [Header("射线测试")]
    public LayerMask testLayerMask = 1; // Default layer
    
    private Camera playerCamera;
    
    void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            playerCamera = FindFirstObjectByType<Camera>();
        }
        
        if (enableDebugMode)
        {
            Debug.Log("🔍 简化钻塔调试器已启动");
            
            // 只有明确启用自动检测时才运行
            if (enableAutoDetection)
            {
                Debug.Log("⚠️ 自动检测已启用，将每5秒输出一次调试信息");
                // 立即进行一次地面检测
                Invoke(nameof(DebugGroundDetection), 1f);
                
                // 每5秒检测一次
                InvokeRepeating(nameof(DebugGroundDetection), 5f, 5f);
            }
            else
            {
                Debug.Log("ℹ️ 自动检测已禁用，使用手动检测功能");
            }
        }
    }
    
    /// <summary>
    /// 定期检测地面信息
    /// </summary>
    void DebugGroundDetection()
    {
        if (!enableDebugMode || !showRaycastInfo || playerCamera == null) return;
        
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = playerCamera.ScreenPointToRay(screenCenter);
        
        Debug.Log("--- 钻塔地面检测调试 ---");
        Debug.Log($"📍 射线起点: {ray.origin}");
        Debug.Log($"📐 射线方向: {ray.direction}");
        Debug.Log($"📏 检测距离: {debugRayLength}m");
        Debug.Log($"🎯 LayerMask: {testLayerMask.value} (应该检测Layer 0)");
        
        // 检测所有可能的碰撞
        RaycastHit[] allHits = Physics.RaycastAll(ray, debugRayLength);
        Debug.Log($"🎯 射线击中 {allHits.Length} 个对象:");
        
        bool foundGroundObject = false;
        
        for (int i = 0; i < allHits.Length; i++)
        {
            RaycastHit hit = allHits[i];
            GameObject hitObj = hit.collider.gameObject;
            
            Debug.Log($"  [{i}] 🎯 {hitObj.name}:");
            Debug.Log($"      🏷️ Layer: {hitObj.layer} ({LayerMask.LayerToName(hitObj.layer)})");
            Debug.Log($"      📍 位置: {hit.point}");
            Debug.Log($"      📏 距离: {hit.distance:F2}m");
            Debug.Log($"      🔧 碰撞器: {hit.collider.GetType().Name}");
            
            // 检查是否有GeologyLayer组件
            GeologyLayer geoLayer = hitObj.GetComponent<GeologyLayer>();
            if (geoLayer != null)
            {
                Debug.Log($"      🗿 地质地层: {geoLayer.layerName}");
                foundGroundObject = true;
            }
            
            // 检查是否是地面对象
            if (IsLikelyGroundObject(hitObj))
            {
                Debug.Log($"      🌍 疑似地面对象");
                foundGroundObject = true;
            }
        }
        
        // 测试特定LayerMask的检测（钻塔工具使用的设置）
        if (Physics.Raycast(ray, out RaycastHit specificHit, debugRayLength, testLayerMask))
        {
            Debug.Log($"✅ 钻塔LayerMask({testLayerMask.value})检测成功:");
            Debug.Log($"   🎯 击中: {specificHit.collider.name}");
            Debug.Log($"   🏷️ Layer: {specificHit.collider.gameObject.layer}");
            Debug.Log($"   📍 位置: {specificHit.point}");
            Debug.Log($"   📏 距离: {specificHit.distance:F2}m");
        }
        else
        {
            Debug.LogWarning($"❌ 钻塔LayerMask({testLayerMask.value})检测失败!");
            Debug.LogWarning("   这就是为什么钻塔无法放置的原因");
            
            if (foundGroundObject)
            {
                Debug.LogWarning("   💡 建议: 发现了地面对象但Layer不匹配，需要修复Layer设置");
            }
        }
        
        // 检查钻塔工具设置
        DrillTowerTool drillTool = FindFirstObjectByType<DrillTowerTool>();
        if (drillTool != null)
        {
            Debug.Log($"📋 钻塔工具当前设置:");
            Debug.Log($"   🎯 groundLayers: {drillTool.groundLayers.value}");
            Debug.Log($"   📏 useRange: {drillTool.useRange}");
            Debug.Log($"   📍 placementOffset: {drillTool.placementOffset}");
            
            if (drillTool.groundLayers.value != testLayerMask.value)
            {
                Debug.LogWarning($"   ⚠️ 工具LayerMask({drillTool.groundLayers.value}) ≠ 测试LayerMask({testLayerMask.value})");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ 未找到钻塔工具");
        }
        
        Debug.Log("--- 检测结束 ---\n");
    }
    
    bool IsLikelyGroundObject(GameObject obj)
    {
        string name = obj.name.ToLower();
        
        // 检查名称关键词
        if (name.Contains("ground") || name.Contains("terrain") || 
            name.Contains("floor") || name.Contains("plane") ||
            name.Contains("地面") || name.Contains("地层"))
        {
            return true;
        }
        
        // 检查是否是Unity地形
        if (obj.GetComponent<Terrain>() != null)
        {
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 手动触发射线检测（可从Inspector调用）
    /// </summary>
    [ContextMenu("手动检测地面")]
    public void ManualGroundDetection()
    {
        Debug.Log("🔍 手动触发地面检测:");
        DebugGroundDetection();
    }
    
    /// <summary>
    /// 显示场景中所有地面对象信息
    /// </summary>
    [ContextMenu("显示所有地面对象")]
    public void ShowAllGroundObjects()
    {
        Debug.Log("🌍 场景中所有可能的地面对象:");
        
        // 查找所有带碰撞器的对象
        Collider[] allColliders = FindObjectsOfType<Collider>();
        int groundObjectCount = 0;
        
        foreach (Collider col in allColliders)
        {
            GameObject obj = col.gameObject;
            
            // 跳过玩家、UI等对象
            if (ShouldSkipObject(obj)) continue;
            
            // 检查是否是地面相关对象
            if (IsLikelyGroundObject(obj) || obj.GetComponent<GeologyLayer>() != null)
            {
                groundObjectCount++;
                Debug.Log($"🔲 #{groundObjectCount} {obj.name}:");
                Debug.Log($"   🏷️ Layer: {obj.layer} ({LayerMask.LayerToName(obj.layer)})");
                Debug.Log($"   📍 位置: {obj.transform.position}");
                Debug.Log($"   🔧 碰撞器: {col.GetType().Name}");
                
                // 检查是否是地质地层
                GeologyLayer geoLayer = obj.GetComponent<GeologyLayer>();
                if (geoLayer != null)
                {
                    Debug.Log($"   🗿 地质地层: {geoLayer.layerName}");
                }
                
                // 检查是否是地形
                if (obj.GetComponent<Terrain>() != null)
                {
                    Debug.Log($"   🏔️ Unity地形");
                }
            }
        }
        
        Debug.Log($"📊 总共找到 {groundObjectCount} 个地面相关对象");
    }
    
    bool ShouldSkipObject(GameObject obj)
    {
        string name = obj.name.ToLower();
        
        // 跳过这些类型的对象
        if (name.Contains("player") || name.Contains("camera") || 
            name.Contains("ui") || name.Contains("preview") ||
            name.Contains("sample") || name.Contains("tower"))
        {
            return true;
        }
        
        return false;
    }
    
    void OnDrawGizmos()
    {
        if (!enableDebugMode || playerCamera == null) return;
        
        // 绘制射线
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = playerCamera.ScreenPointToRay(screenCenter);
        
        Gizmos.color = Color.red;
        Gizmos.DrawRay(ray.origin, ray.direction * debugRayLength);
        
        // 检测并绘制击中点
        if (Physics.Raycast(ray, out RaycastHit hit, debugRayLength, testLayerMask))
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(hit.point, 0.5f);
            
            // 绘制钻塔预计放置位置
            Vector3 towerPos = hit.point + Vector3.up * 0.1f;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(towerPos, new Vector3(1.5f, 3f, 1.5f));
        }
    }
}
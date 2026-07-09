using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 钻塔工具调试器 - 帮助诊断放置问题
/// </summary>
public class DrillTowerDebugger : MonoBehaviour
{
    [Header("调试设置")]
    public bool enableDebugMode = true;
    public bool showRaycastInfo = true;
    public bool showLayerInfo = true;
    public float debugRayLength = 50f;
    
    [Header("射线测试")]
    public LayerMask testLayerMask = -1;
    
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
            Debug.Log("🔍 钻塔调试器已启动");
            InvokeRepeating(nameof(DebugGroundDetection), 1f, 2f);
        }
    }
    
    void Update()
    {
        var keyboard = Keyboard.current;
        if (enableDebugMode && keyboard != null && keyboard.gKey.wasPressedThisFrame)
        {
            PerformManualRaycastTest();
        }
    }
    
    /// <summary>
    /// 定期检测地面信息
    /// </summary>
    void DebugGroundDetection()
    {
        if (!showRaycastInfo || playerCamera == null) return;
        
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = playerCamera.ScreenPointToRay(screenCenter);
        
        Debug.Log("--- 地面检测调试 ---");
        Debug.Log($"射线起点: {ray.origin}");
        Debug.Log($"射线方向: {ray.direction}");
        Debug.Log($"检测距离: {debugRayLength}m");
        Debug.Log($"测试LayerMask: {testLayerMask.value}");
        
        // 检测所有可能的碰撞
        RaycastHit[] allHits = Physics.RaycastAll(ray, debugRayLength);
        Debug.Log($"射线击中 {allHits.Length} 个对象:");
        
        for (int i = 0; i < allHits.Length; i++)
        {
            RaycastHit hit = allHits[i];
            GameObject hitObj = hit.collider.gameObject;
            
            Debug.Log($"  [{i}] {hitObj.name}:");
            Debug.Log($"      Layer: {hitObj.layer} ({LayerMask.LayerToName(hitObj.layer)})");
            Debug.Log($"      位置: {hit.point}");
            Debug.Log($"      距离: {hit.distance:F2}m");
            Debug.Log($"      碰撞器类型: {hit.collider.GetType().Name}");
            
            // 检查是否有GeologyLayer组件
            GeologyLayer geoLayer = hitObj.GetComponent<GeologyLayer>();
            if (geoLayer != null)
            {
                Debug.Log($"      🗿 地质地层: {geoLayer.layerName}");
            }
        }
        
        // 测试特定LayerMask的检测
        if (Physics.Raycast(ray, out RaycastHit specificHit, debugRayLength, testLayerMask))
        {
            Debug.Log($"✅ LayerMask {testLayerMask.value} 检测成功:");
            Debug.Log($"   击中: {specificHit.collider.name}");
            Debug.Log($"   Layer: {specificHit.collider.gameObject.layer}");
        }
        else
        {
            Debug.LogWarning($"❌ LayerMask {testLayerMask.value} 检测失败");
        }
        
        Debug.Log("--- 检测结束 ---\n");
    }
    
    /// <summary>
    /// 手动射线检测测试（按G键触发）
    /// </summary>
    void PerformManualRaycastTest()
    {
        Debug.Log("🎯 手动射线检测测试 (G键)");
        DebugGroundDetection();
        
        // 检查钻塔工具设置
        DrillTowerTool drillTool = FindFirstObjectByType<DrillTowerTool>();
        if (drillTool != null)
        {
            Debug.Log($"📋 钻塔工具设置:");
            Debug.Log($"   groundLayers: {drillTool.groundLayers.value}");
            Debug.Log($"   useRange: {drillTool.useRange}");
            Debug.Log($"   placementOffset: {drillTool.placementOffset}");
        }
        else
        {
            Debug.LogWarning("⚠️ 未找到钻塔工具");
        }
    }
    
    /// <summary>
    /// 显示场景中所有地面对象的Layer信息
    /// </summary>
    [ContextMenu("显示所有地面对象Layer信息")]
    public void ShowAllGroundObjectLayers()
    {
        Debug.Log("🌍 场景中所有可能的地面对象:");
        
        // 查找所有带碰撞器的对象
        Collider[] allColliders = FindObjectsOfType<Collider>();
        
        foreach (Collider col in allColliders)
        {
            GameObject obj = col.gameObject;
            
            // 跳过玩家、UI等对象
            if (obj.name.Contains("Player") || obj.name.Contains("UI") || 
                obj.name.Contains("Camera") || obj.name.Contains("Preview"))
                continue;
            
            Debug.Log($"🔲 {obj.name}:");
            Debug.Log($"   Layer: {obj.layer} ({LayerMask.LayerToName(obj.layer)})");
            Debug.Log($"   位置: {obj.transform.position}");
            Debug.Log($"   碰撞器: {col.GetType().Name}");
            
            // 检查是否是地质地层
            if (obj.GetComponent<GeologyLayer>() != null)
            {
                Debug.Log($"   🗿 地质地层");
            }
            
            // 检查是否是地形
            if (obj.GetComponent<Terrain>() != null)
            {
                Debug.Log($"   🏔️ Unity地形");
            }
        }
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
        if (Physics.Raycast(ray, out RaycastHit hit, debugRayLength))
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(hit.point, 0.5f);
        }
    }
}

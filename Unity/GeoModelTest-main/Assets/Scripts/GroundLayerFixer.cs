using UnityEngine;

/// <summary>
/// 地面Layer修复器 - 确保地面对象在正确的Layer上
/// </summary>
public class GroundLayerFixer : MonoBehaviour
{
    [Header("修复设置")]
    public bool autoFixOnStart = true;
    public int targetGroundLayer = 0; // Default layer
    
    [Header("搜索关键词")]
    public string[] groundKeywords = { "ground", "terrain", "floor", "plane", "地面", "地层" };
    
    void Start()
    {
        if (autoFixOnStart)
        {
            FixGroundLayers();
        }
    }
    
    [ContextMenu("修复地面Layer设置")]
    public void FixGroundLayers()
    {
        Debug.Log("🔧 开始修复地面Layer设置...");
        
        int fixedCount = 0;
        
        // 查找所有带碰撞器的对象
        Collider[] allColliders = FindObjectsOfType<Collider>();
        
        foreach (Collider col in allColliders)
        {
            GameObject obj = col.gameObject;
            
            // 跳过玩家、UI、工具等对象
            if (ShouldSkipObject(obj)) continue;
            
            // 检查是否是地面相关对象
            if (IsGroundObject(obj))
            {
                if (obj.layer != targetGroundLayer)
                {
                    Debug.Log($"🔄 修复 {obj.name}: Layer {obj.layer} → {targetGroundLayer}");
                    obj.layer = targetGroundLayer;
                    fixedCount++;
                }
                else
                {
                    Debug.Log($"✅ {obj.name} Layer已正确: {obj.layer}");
                }
            }
        }
        
        Debug.Log($"🎯 地面Layer修复完成，共修复 {fixedCount} 个对象");
        
        // 测试射线检测
        TestRaycastAfterFix();
    }
    
    bool ShouldSkipObject(GameObject obj)
    {
        string name = obj.name.ToLower();
        
        // 跳过这些类型的对象
        if (name.Contains("player") || name.Contains("camera") || 
            name.Contains("ui") || name.Contains("preview") ||
            name.Contains("drill") && name.Contains("tower") ||
            name.Contains("sample"))
        {
            return true;
        }
        
        return false;
    }
    
    bool IsGroundObject(GameObject obj)
    {
        string name = obj.name.ToLower();
        
        // 检查名称关键词
        foreach (string keyword in groundKeywords)
        {
            if (name.Contains(keyword.ToLower()))
            {
                return true;
            }
        }
        
        // 检查是否有地质地层组件
        if (obj.GetComponent<GeologyLayer>() != null)
        {
            return true;
        }
        
        // 检查是否是Unity地形
        if (obj.GetComponent<Terrain>() != null)
        {
            return true;
        }
        
        // 检查是否是大型的水平平面（可能是地面）
        MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Bounds bounds = renderer.bounds;
            Vector3 size = bounds.size;
            
            // 如果是扁平的大型对象，可能是地面
            if (size.x > 5f && size.z > 5f && size.y < 2f)
            {
                return true;
            }
        }
        
        return false;
    }
    
    void TestRaycastAfterFix()
    {
        Debug.Log("🎯 测试修复后的射线检测...");
        
        Camera cam = Camera.main;
        if (cam == null) cam = FindFirstObjectByType<Camera>();
        
        if (cam != null)
        {
            Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
            Ray ray = cam.ScreenPointToRay(screenCenter);
            
            LayerMask testMask = 1; // 测试Default layer
            
            if (Physics.Raycast(ray, out RaycastHit hit, 50f, testMask))
            {
                Debug.Log($"✅ 射线检测成功!");
                Debug.Log($"   击中对象: {hit.collider.name}");
                Debug.Log($"   Layer: {hit.collider.gameObject.layer}");
                Debug.Log($"   位置: {hit.point}");
                Debug.Log($"   距离: {hit.distance:F2}m");
            }
            else
            {
                Debug.LogWarning("❌ 射线检测仍然失败");
                
                // 尝试不使用LayerMask
                if (Physics.Raycast(ray, out RaycastHit anyHit, 50f))
                {
                    Debug.LogWarning($"   但击中了其他Layer的对象: {anyHit.collider.name} (Layer: {anyHit.collider.gameObject.layer})");
                }
            }
        }
    }
    
    [ContextMenu("显示场景中所有对象的Layer信息")]
    public void ShowAllObjectLayers()
    {
        Debug.Log("📋 场景中所有对象的Layer信息:");
        
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        
        // 按Layer分组统计
        System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<string>> layerGroups = 
            new System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<string>>();
        
        foreach (GameObject obj in allObjects)
        {
            if (!layerGroups.ContainsKey(obj.layer))
            {
                layerGroups[obj.layer] = new System.Collections.Generic.List<string>();
            }
            layerGroups[obj.layer].Add(obj.name);
        }
        
        foreach (var group in layerGroups)
        {
            string layerName = LayerMask.LayerToName(group.Key);
            Debug.Log($"Layer {group.Key} ({layerName}): {group.Value.Count} 个对象");
            
            if (group.Value.Count < 10) // 只显示少量对象的详细信息
            {
                foreach (string objName in group.Value)
                {
                    Debug.Log($"  - {objName}");
                }
            }
        }
    }
}
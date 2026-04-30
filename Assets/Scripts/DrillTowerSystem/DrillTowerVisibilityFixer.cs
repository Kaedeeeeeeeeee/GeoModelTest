using UnityEngine;

/// <summary>
/// 钻塔可见性修复器 - 检查和修复钻塔可见性问题
/// </summary>
public class DrillTowerVisibilityFixer : MonoBehaviour
{
    [Header("修复设置")]
    public bool autoFixOnStart = true;
    public Material fixMaterial;
    public Color defaultColor = new Color(1f, 1f, 1f, 1f); // 默认白灰色
    
    void Start()
    {
        if (autoFixOnStart)
        {
            Invoke(nameof(CheckAndFixAllTowers), 2f); // 延迟2秒检查
        }
    }
    
    [ContextMenu("检查并修复所有钻塔可见性")]
    public void CheckAndFixAllTowers()
    {
        Debug.Log("🔍 开始检查钻塔可见性...");
        
        // 查找所有钻塔对象
        DrillTower[] towers = FindObjectsOfType<DrillTower>();
        Debug.Log($"找到 {towers.Length} 个钻塔");
        
        if (towers.Length == 0)
        {
            // 尝试通过名称查找钻塔
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                if (obj.name.Contains("DrillTower") || obj.name.Contains("Tower"))
                {
                    Debug.Log($"发现疑似钻塔对象: {obj.name}");
                    FixTowerVisibility(obj);
                }
            }
        }
        else
        {
            foreach (DrillTower tower in towers)
            {
                FixTowerVisibility(tower.gameObject);
            }
        }
        
        Debug.Log("✅ 钻塔可见性检查完成");
    }
    
    /// <summary>
    /// 修复单个钻塔的可见性
    /// </summary>
    void FixTowerVisibility(GameObject towerObj)
    {
        Debug.Log($"🔧 修复钻塔可见性: {towerObj.name}");
        Debug.Log($"   位置: {towerObj.transform.position}");
        Debug.Log($"   激活状态: {towerObj.activeInHierarchy}");
        Debug.Log($"   本地缩放: {towerObj.transform.localScale}");
        
        // 确保对象激活
        if (!towerObj.activeInHierarchy)
        {
            towerObj.SetActive(true);
            Debug.Log("   ✅ 激活了钻塔对象");
        }
        
        // 检查缩放
        if (towerObj.transform.localScale.magnitude < 0.1f)
        {
            towerObj.transform.localScale = Vector3.one;
            Debug.Log("   ✅ 修复了缩放问题");
        }
        
        // 修复所有子对象的渲染器
        Renderer[] renderers = towerObj.GetComponentsInChildren<Renderer>(true);
        Debug.Log($"   找到 {renderers.Length} 个渲染器");
        
        Material materialToUse = GetMaterialToUse();
        
        int fixedCount = 0;
        foreach (Renderer renderer in renderers)
        {
            bool needsFix = false;
            
            // 检查渲染器状态
            if (!renderer.enabled)
            {
                renderer.enabled = true;
                needsFix = true;
                Debug.Log($"     ✅ 启用渲染器: {renderer.gameObject.name}");
            }
            
            // 检查材质
            if (renderer.material == null || IsMaterialInvisible(renderer.material))
            {
                renderer.material = materialToUse;
                needsFix = true;
                Debug.Log($"     ✅ 修复材质: {renderer.gameObject.name}");
            }
            
            // 检查父对象激活状态
            if (!renderer.gameObject.activeInHierarchy)
            {
                renderer.gameObject.SetActive(true);
                needsFix = true;
                Debug.Log($"     ✅ 激活对象: {renderer.gameObject.name}");
            }
            
            // 设置渲染属性
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
            
            if (needsFix) fixedCount++;
        }
        
        Debug.Log($"   🎯 修复了 {fixedCount} 个渲染器");
        
        // 检查是否在相机视野内
        CheckCameraVisibility(towerObj);
        
        // 添加醒目的标记
        AddVisibilityMarker(towerObj);
    }
    
    /// <summary>
    /// 获取要使用的材质
    /// </summary>
    Material GetMaterialToUse()
    {
        if (fixMaterial != null)
        {
            return fixMaterial;
        }
        
        // 创建醒目的默认材质
        Material material = new Material(Shader.Find("Standard"));
        material.color = defaultColor;
        material.SetFloat("_Metallic", 0.1f);
        material.SetFloat("_Glossiness", 0.5f);
        material.name = "DrillTowerFixMaterial";
        
        return material;
    }
    
    /// <summary>
    /// 检查材质是否不可见
    /// </summary>
    bool IsMaterialInvisible(Material material)
    {
        if (material == null) return true;
        
        Color color = material.color;
        
        // 检查透明度
        if (color.a < 0.1f) return true;
        
        // 检查颜色强度
        if (color.r + color.g + color.b < 0.1f) return true;
        
        return false;
    }
    
    /// <summary>
    /// 检查相机可见性
    /// </summary>
    void CheckCameraVisibility(GameObject towerObj)
    {
        Camera cam = Camera.main;
        if (cam == null) cam = FindFirstObjectByType<Camera>();
        
        if (cam != null)
        {
            Vector3 towerPos = towerObj.transform.position;
            Vector3 camPos = cam.transform.position;
            float distance = Vector3.Distance(towerPos, camPos);
            
            Debug.Log($"   📷 相机距离: {distance:F2}m");
            
            // 检查是否在视野内
            Vector3 viewportPoint = cam.WorldToViewportPoint(towerPos);
            bool inView = viewportPoint.x >= 0 && viewportPoint.x <= 1 && 
                         viewportPoint.y >= 0 && viewportPoint.y <= 1 && 
                         viewportPoint.z > 0;
            
            Debug.Log($"   📷 在视野内: {inView}");
            Debug.Log($"   📷 视口坐标: {viewportPoint}");
            
            if (!inView)
            {
                Debug.LogWarning("   ⚠️ 钻塔不在相机视野内，可能需要移动相机或钻塔");
            }
        }
    }
    
    /// <summary>
    /// 添加可见性标记
    /// </summary>
    void AddVisibilityMarker(GameObject towerObj)
    {
        // 检查是否已有标记
        Transform marker = towerObj.transform.Find("VisibilityMarker");
        if (marker != null) return;
        
        // 创建明显的标记球体
        GameObject markerObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        markerObj.name = "VisibilityMarker";
        markerObj.transform.SetParent(towerObj.transform);
        markerObj.transform.localPosition = new Vector3(0, 3f, 0); // 钻塔顶部
        markerObj.transform.localScale = Vector3.one * 0.3f;
        
        // 设置醒目的材质
        Material markerMaterial = new Material(Shader.Find("Standard"));
        markerMaterial.color = Color.yellow;
        markerMaterial.SetFloat("_Mode", 3); // Transparent
        markerMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        markerMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        markerMaterial.SetInt("_ZWrite", 0);
        markerMaterial.DisableKeyword("_ALPHATEST_ON");
        markerMaterial.EnableKeyword("_ALPHABLEND_ON");
        markerMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        markerMaterial.renderQueue = 3000;
        
        Color glowColor = Color.yellow;
        glowColor.a = 0.8f;
        markerMaterial.color = glowColor;
        markerMaterial.EnableKeyword("_EMISSION");
        markerMaterial.SetColor("_EmissionColor", Color.yellow * 0.5f);
        
        markerObj.GetComponent<Renderer>().material = markerMaterial;
        
        // 移除碰撞器
        Collider markerCollider = markerObj.GetComponent<Collider>();
        if (markerCollider != null)
        {
            DestroyImmediate(markerCollider);
        }
        
        Debug.Log("   ✅ 添加了黄色可见性标记球");
    }
    
    [ContextMenu("显示所有钻塔信息")]
    public void ShowAllTowerInfo()
    {
        Debug.Log("📋 所有钻塔对象信息:");
        
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int towerCount = 0;
        
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("Tower") || obj.name.Contains("Drill"))
            {
                towerCount++;
                Debug.Log($"🏗️ #{towerCount} {obj.name}:");
                Debug.Log($"   📍 位置: {obj.transform.position}");
                Debug.Log($"   📏 缩放: {obj.transform.localScale}");
                Debug.Log($"   ✅ 激活: {obj.activeInHierarchy}");
                Debug.Log($"   🔧 组件: {obj.GetComponents<Component>().Length} 个");
                
                Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
                Debug.Log($"   🎨 渲染器: {renderers.Length} 个");
                
                foreach (Renderer r in renderers)
                {
                    Debug.Log($"     - {r.gameObject.name}: 启用={r.enabled}, 材质={r.material?.name}");
                }
            }
        }
        
        Debug.Log($"📊 总共找到 {towerCount} 个钻塔相关对象");
    }
}
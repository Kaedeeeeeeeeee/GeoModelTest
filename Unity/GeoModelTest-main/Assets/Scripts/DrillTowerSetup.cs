using UnityEngine;

/// <summary>
/// 钻塔工具设置脚本 - 在运行时自动创建和配置钻塔工具
/// </summary>
public class DrillTowerSetup : MonoBehaviour
{
    [Header("钻塔工具设置")]
    public bool autoCreateOnStart = true;
    public Sprite drillTowerIcon; // 钻塔工具图标
    
    [Header("钻塔预制件设置")]
    public GameObject existingDrillTowerPrefab; // 可以拖入现有的钻塔预制件
    public Material towerMaterial;
    public Material activeMaterial;
    public Material inactiveMaterial;
    
    void Start()
    {
        if (autoCreateOnStart)
        {
            CreateDrillTowerTool();
        }
    }
    
    /// <summary>
    /// 创建钻塔工具
    /// </summary>
    public void CreateDrillTowerTool()
    {
        Debug.Log("开始创建钻塔工具...");
        
        // 检查是否已经存在钻塔工具
        DrillTowerTool existingTool = FindFirstObjectByType<DrillTowerTool>();
        if (existingTool != null)
        {
            Debug.Log("钻塔工具已存在，跳过创建");
            return;
        }
        
        // 创建钻塔工具对象
        GameObject toolObj = new GameObject("DrillTowerTool");
        toolObj.transform.SetParent(transform);
        
        // 添加钻塔工具组件
        DrillTowerTool drillTool = toolObj.AddComponent<DrillTowerTool>();
        
        // 配置基础工具属性
        drillTool.toolName = "钻塔工具";
        drillTool.toolIcon = drillTowerIcon;
        drillTool.useRange = 50f;
        drillTool.useCooldown = 1f;
        
        // 重要：设置地面Layer，与地质检测系统保持一致
        drillTool.groundLayers = -1; // LayerMask for all layers，与LayerDetectionSystem一致
        drillTool.placementOffset = 0f; // 贴地放置，让物理系统处理高度
        
        // 配置钻塔特定属性
        drillTool.interactionRange = 3f;
        drillTool.maxDrillDepths = 5;
        drillTool.depthPerDrill = 2f;
        drillTool.sampleRingRadius = 2.5f;
        drillTool.sampleElevation = 3.0f; // 默认3米悬浮高度
        drillTool.sampleSpacing = 0.8f;
        
        // 使用现有预制件或创建新的钻塔预制件
        if (existingDrillTowerPrefab != null)
        {
            drillTool.drillTowerPrefab = existingDrillTowerPrefab;
            Debug.Log("使用现有的钻塔预制件");
        }
        else
        {
            drillTool.drillTowerPrefab = CreateDrillTowerPrefab();
            Debug.Log("动态创建钻塔预制件");
        }
        drillTool.prefabToPlace = drillTool.drillTowerPrefab;
        
        // 配置材质
        if (activeMaterial != null) drillTool.activeDrillMaterial = activeMaterial;
        if (inactiveMaterial != null) drillTool.inactiveDrillMaterial = inactiveMaterial;
        
        // 添加到工具管理器
        ToolManager toolManager = FindFirstObjectByType<ToolManager>();
        if (toolManager != null)
        {
            toolManager.AddTool(drillTool);
            Debug.Log("钻塔工具已添加到工具管理器");
        }
        else
        {
            Debug.LogWarning("未找到工具管理器，钻塔工具可能无法在UI中显示");
        }
        
        Debug.Log("✅ 钻塔工具创建完成！");
    }
    
    /// <summary>
    /// 创建简单的钻塔预制件
    /// </summary>
    GameObject CreateDrillTowerPrefab()
    {
        // 创建钻塔主体
        GameObject towerPrefab = new GameObject("DrillTowerPrefab");
        
        // 创建底座（扁平圆柱体）- 底部贴地，考虑到圆柱体的pivot在中心
        GameObject base_platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        base_platform.name = "BasePlatform";
        base_platform.transform.SetParent(towerPrefab.transform);
        base_platform.transform.localPosition = new Vector3(0, 0.1f, 0); // 底座高度0.2f，所以中心在0.1f处
        base_platform.transform.localScale = new Vector3(1.2f, 0.2f, 1.2f);
        
        // 创建塔身（圆柱体）- 从底座向上
        GameObject towerBody = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        towerBody.name = "TowerBody";
        towerBody.transform.SetParent(towerPrefab.transform);
        towerBody.transform.localPosition = new Vector3(0, 1.4f, 0); // 底座顶部(0.2f) + 塔身高度一半(1.2f) = 1.4f
        towerBody.transform.localScale = new Vector3(0.8f, 1.2f, 0.8f);
        
        // 创建钻探臂（立方体）- 在塔身顶部
        GameObject drillArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        drillArm.name = "DrillArm";
        drillArm.transform.SetParent(towerPrefab.transform);
        drillArm.transform.localPosition = new Vector3(0, 2.6f, 0.6f); // 塔身顶部(1.4f + 1.2f = 2.6f)
        drillArm.transform.localScale = new Vector3(0.3f, 0.3f, 1.2f);
        
        // 创建钻头（小圆柱体）- 悬挂在钻探臂下方
        GameObject drillBit = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        drillBit.name = "DrillBit";
        drillBit.transform.SetParent(towerPrefab.transform);
        drillBit.transform.localPosition = new Vector3(0, 1.0f, 0); // 稍微高于底座，调整到合适高度
        drillBit.transform.localScale = new Vector3(0.2f, 0.4f, 0.2f);
        
        // 配置材质 - 重要：确保钻塔可见
        Material finalMaterial;
        if (towerMaterial != null)
        {
            finalMaterial = towerMaterial;
        }
        else
        {
            // 创建明显可见的默认材质
            finalMaterial = new Material(Shader.Find("Standard"));
            finalMaterial.color = new Color(0.8f, 0.3f, 0.1f, 1f); // 橙红色，确保可见
            finalMaterial.SetFloat("_Metallic", 0.2f);
            finalMaterial.SetFloat("_Glossiness", 0.6f);
        }
        
        ApplyMaterialToChildren(towerPrefab, finalMaterial);
        
        // 确保所有渲染器都正常配置
        Renderer[] renderers = towerPrefab.GetComponentsInChildren<Renderer>();
        Debug.Log($"钻塔预制件包含 {renderers.Length} 个渲染器");
        
        foreach (Renderer renderer in renderers)
        {
            renderer.material = finalMaterial;
            renderer.enabled = true;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
            Debug.Log($"配置渲染器: {renderer.gameObject.name}");
        }
        
        // 添加物理组件，让钻塔有重力和碰撞
        Rigidbody towerRigidbody = towerPrefab.AddComponent<Rigidbody>();
        towerRigidbody.mass = 100f; // 重量适中，不会轻易被推动
        towerRigidbody.linearDamping = 5f; // 增加阻力，放置后快速稳定
        towerRigidbody.angularDamping = 10f; // 增加角阻力，防止旋转
        towerRigidbody.centerOfMass = new Vector3(0, 0.5f, 0); // 低重心，更稳定
        
        // 添加主碰撞器（用于物理碰撞）
        CapsuleCollider physicsCollider = towerPrefab.AddComponent<CapsuleCollider>();
        physicsCollider.radius = 0.6f;
        physicsCollider.height = 2.8f; // 稍微增加高度以覆盖整个钻塔
        physicsCollider.center = new Vector3(0, 1.4f, 0); // 从底座底部到钻塔顶部的中心
        physicsCollider.direction = 1; // Y轴方向
        
        // 添加交互碰撞器（用于F键交互检测）
        BoxCollider interactionCollider = towerPrefab.AddComponent<BoxCollider>();
        interactionCollider.isTrigger = true; // 设为触发器
        interactionCollider.size = new Vector3(2f, 3f, 2f); // 稍大，便于交互
        interactionCollider.center = new Vector3(0, 1.4f, 0); // 调整到钻塔中心
        
        // 冻结旋转，防止钻塔倾倒
        towerRigidbody.freezeRotation = true;
        
        // 确保预制件不会立即激活
        towerPrefab.SetActive(false);
        
        Debug.Log("钻塔预制件创建完成");
        return towerPrefab;
    }
    
    /// <summary>
    /// 为对象及其子对象应用材质
    /// </summary>
    void ApplyMaterialToChildren(GameObject obj, Material material)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.material = material;
        }
    }
    
    [ContextMenu("手动创建钻塔工具")]
    public void ManualCreateTool()
    {
        CreateDrillTowerTool();
    }
}
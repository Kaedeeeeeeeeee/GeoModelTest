using UnityEngine;
using System.Collections;

/// <summary>
/// 场景切换器初始化器 - 确保场景切换器工具正确初始化
/// </summary>
public class SceneSwitcherInitializer : MonoBehaviour
{
    [Header("场景切换器配置")]
    public GameObject sceneSwitcherPrefab; // 场景切换器预制体
    public Sprite sceneSwitcherIcon; // 场景切换器图标
    
    [Header("音效配置")]
    public AudioClip switcherActivateSound;
    public AudioClip sceneChangeSound;
    
    void Start()
    {
        StartCoroutine(InitializeSceneSwitcher());
    }
    
    IEnumerator InitializeSceneSwitcher()
    {
        // 等待其他系统初始化
        yield return new WaitForSeconds(0.5f);
        
        // 创建场景切换器工具
        CreateSceneSwitcherTool();
        
        // 确保场景管理器存在
        EnsureSceneManagerExists();
        
        Debug.Log("场景切换器系统初始化完成");
    }
    
    /// <summary>
    /// 创建场景切换器工具
    /// </summary>
    void CreateSceneSwitcherTool()
    {
        // 检查是否已存在场景切换器
        SceneSwitcherTool existingTool = FindFirstObjectByType<SceneSwitcherTool>();
        if (existingTool != null)
        {
            Debug.Log("场景切换器工具已存在，跳过创建");
            return;
        }
        
        // 创建场景切换器工具对象
        GameObject toolObject = new GameObject("SceneSwitcherTool");
        SceneSwitcherTool tool = toolObject.AddComponent<SceneSwitcherTool>();
        
        // 配置工具属性
        tool.switcherPrefab = sceneSwitcherPrefab;
        tool.toolIcon = sceneSwitcherIcon;
        tool.switcherActivateSound = switcherActivateSound;
        tool.sceneChangeSound = sceneChangeSound;
        
        // 添加到工具管理器
        AddToToolManager(tool);
        
        Debug.Log("场景切换器工具创建完成");
    }
    
    /// <summary>
    /// 添加到工具管理器
    /// </summary>
    void AddToToolManager(SceneSwitcherTool tool)
    {
        ToolManager toolManager = FindFirstObjectByType<ToolManager>();
        if (toolManager != null)
        {
            toolManager.AddTool(tool);
            Debug.Log("场景切换器工具已添加到工具管理器");
        }
        else
        {
            Debug.LogWarning("未找到工具管理器，场景切换器工具未能添加");
        }
        
        // 刷新库存UI
        InventoryUISystem inventoryUI = FindFirstObjectByType<InventoryUISystem>();
        if (inventoryUI != null)
        {
            inventoryUI.RefreshTools();
            Debug.Log("库存UI已刷新");
        }
    }
    
    /// <summary>
    /// 确保场景管理器存在
    /// </summary>
    void EnsureSceneManagerExists()
    {
        GameSceneManager sceneManager = GameSceneManager.Instance;
        if (sceneManager != null)
        {
            Debug.Log("场景管理器已就绪");
        }
        else
        {
            Debug.LogError("场景管理器创建失败！");
        }
    }
    
    /// <summary>
    /// 创建默认场景切换器预制体（如果没有指定）
    /// </summary>
    void CreateDefaultSwitcherPrefab()
    {
        if (sceneSwitcherPrefab != null) return;
        
        // 创建简单的场景切换器模型
        GameObject defaultSwitcher = new GameObject("DefaultSceneSwitcher");
        
        // 添加视觉组件
        GameObject visualModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visualModel.name = "SwitcherModel";
        visualModel.transform.SetParent(defaultSwitcher.transform);
        visualModel.transform.localPosition = Vector3.zero;
        visualModel.transform.localScale = new Vector3(0.1f, 0.02f, 0.1f);
        
        // 设置材质
        Renderer renderer = visualModel.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material switcherMaterial = new Material(Shader.Find("Standard"));
            switcherMaterial.color = new Color(0.8f, 0.8f, 0.2f); // 金黄色
            switcherMaterial.SetFloat("_Metallic", 0.8f);
            switcherMaterial.SetFloat("_Glossiness", 0.9f);
            renderer.material = switcherMaterial;
        }
        
        // 移除碰撞器
        Collider collider = visualModel.GetComponent<Collider>();
        if (collider != null)
        {
            DestroyImmediate(collider);
        }
        
        // 添加发光效果
        GameObject glowEffect = new GameObject("GlowEffect");
        glowEffect.transform.SetParent(defaultSwitcher.transform);
        glowEffect.transform.localPosition = Vector3.zero;
        glowEffect.transform.localScale = new Vector3(0.12f, 0.03f, 0.12f);
        
        GameObject glowSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        glowSphere.name = "GlowSphere";
        glowSphere.transform.SetParent(glowEffect.transform);
        glowSphere.transform.localPosition = Vector3.zero;
        glowSphere.transform.localScale = Vector3.one;
        
        // 设置发光材质
        Renderer glowRenderer = glowSphere.GetComponent<Renderer>();
        if (glowRenderer != null)
        {
            Material glowMaterial = new Material(Shader.Find("Standard"));
            glowMaterial.color = new Color(1f, 1f, 0.5f, 0.5f); // 半透明黄色
            glowMaterial.SetFloat("_Mode", 3); // 透明模式
            glowMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            glowMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            glowMaterial.SetInt("_ZWrite", 0);
            glowMaterial.DisableKeyword("_ALPHATEST_ON");
            glowMaterial.EnableKeyword("_ALPHABLEND_ON");
            glowMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            glowMaterial.renderQueue = 3000;
            glowRenderer.material = glowMaterial;
        }
        
        // 移除碰撞器
        Collider glowCollider = glowSphere.GetComponent<Collider>();
        if (glowCollider != null)
        {
            DestroyImmediate(glowCollider);
        }
        
        sceneSwitcherPrefab = defaultSwitcher;
        
        Debug.Log("默认场景切换器预制体创建完成");
    }
    
    void Awake()
    {
        // 如果没有指定预制体，尝试加载用户的预制体
        if (sceneSwitcherPrefab == null)
        {
            LoadUserSceneSwitcherPrefab();
        }
    }
    
    /// <summary>
    /// 加载用户的SceneSwitcher预制体
    /// </summary>
    void LoadUserSceneSwitcherPrefab()
    {
        // 尝试加载用户的SceneSwitcher预制体
#if UNITY_EDITOR
        sceneSwitcherPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Model/SceneSwitcher/SceneSwitcher.prefab");
        if (sceneSwitcherPrefab != null)
        {
            Debug.Log("✅ SceneSwitcherInitializer: 成功加载用户的SceneSwitcher预制体");
            return;
        }
#endif
        
        // 尝试从Resources加载
        sceneSwitcherPrefab = Resources.Load<GameObject>("Model/SceneSwitcher/SceneSwitcher");
        if (sceneSwitcherPrefab != null)
        {
            Debug.Log("✅ SceneSwitcherInitializer: 从Resources加载用户的SceneSwitcher预制体");
            return;
        }
        
        // 如果都加载失败，创建默认的
        Debug.LogWarning("❌ SceneSwitcherInitializer: 无法加载用户预制体，创建默认预制体");
        CreateDefaultSwitcherPrefab();
    }
}
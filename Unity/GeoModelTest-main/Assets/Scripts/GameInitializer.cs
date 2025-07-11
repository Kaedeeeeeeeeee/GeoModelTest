using UnityEngine;

/// <summary>
/// 游戏初始化管理器 - 负责初始化新功能和工具
/// </summary>
public class GameInitializer : MonoBehaviour
{
    [Header("工具初始化")]
    public bool initializeDrillTower = true;
    public bool enableDebugMode = true;
    public Sprite drillTowerIcon;
    public GameObject existingDrillTowerPrefab; // 可以拖入现有的钻塔预制件
    
    [Header("材质设置")]
    public Material towerMaterial;
    public Material activeMaterial;
    public Material inactiveMaterial;
    
    void Start()
    {
        Debug.Log("🚀 游戏初始化开始...");
        
        if (initializeDrillTower)
        {
            InitializeDrillTowerTool();
            InitializeInteractionUI();
        }
        
        if (enableDebugMode)
        {
            InitializeDebugger();
            InitializeGroundLayerFixer();
            InitializeVisibilityFixer();
            InitializeDem003Debugger();
        }
        
        Debug.Log("✅ 游戏初始化完成！");
    }
    
    void InitializeDrillTowerTool()
    {
        // 创建钻塔设置组件
        GameObject setupObj = new GameObject("DrillTowerSetup");
        setupObj.transform.SetParent(transform);
        
        DrillTowerSetup setup = setupObj.AddComponent<DrillTowerSetup>();
        setup.drillTowerIcon = drillTowerIcon;
        setup.existingDrillTowerPrefab = existingDrillTowerPrefab; // 传递预制件引用
        setup.towerMaterial = towerMaterial;
        setup.activeMaterial = activeMaterial;
        setup.inactiveMaterial = inactiveMaterial;
        
        // 立即创建工具
        setup.CreateDrillTowerTool();
        
        Debug.Log("钻塔工具初始化完成");
    }
    
    void InitializeDebugger()
    {
        // 创建简化调试器（避免输入系统冲突）
        GameObject debuggerObj = new GameObject("DrillTowerDebuggerSimple");
        debuggerObj.transform.SetParent(transform);
        
        DrillTowerDebuggerSimple debugger = debuggerObj.AddComponent<DrillTowerDebuggerSimple>();
        debugger.enableDebugMode = true;
        debugger.showRaycastInfo = true;
        debugger.testLayerMask = 1; // 测试钻塔使用的LayerMask
        
        Debug.Log("🔍 简化调试器初始化完成 - 可在Inspector中手动触发检测");
    }
    
    void InitializeGroundLayerFixer()
    {
        // 创建地面Layer修复器
        GameObject fixerObj = new GameObject("GroundLayerFixer");
        fixerObj.transform.SetParent(transform);
        
        GroundLayerFixer fixer = fixerObj.AddComponent<GroundLayerFixer>();
        fixer.autoFixOnStart = true;
        fixer.targetGroundLayer = 0; // Default layer
        
        Debug.Log("🔧 地面Layer修复器初始化完成");
    }
    
    void InitializeVisibilityFixer()
    {
        // 创建可见性修复器
        GameObject visibilityFixerObj = new GameObject("DrillTowerVisibilityFixer");
        visibilityFixerObj.transform.SetParent(transform);
        
        DrillTowerVisibilityFixer visibilityFixer = visibilityFixerObj.AddComponent<DrillTowerVisibilityFixer>();
        visibilityFixer.autoFixOnStart = true;
        visibilityFixer.defaultColor = new Color(0.8f, 0.3f, 0.1f, 1f); // 橙红色
        
        Debug.Log("👁️ 钻塔可见性修复器初始化完成");
    }
    
    void InitializeInteractionUI()
    {
        // 创建交互UI系统
        GameObject interactionUIObj = new GameObject("DrillTowerInteractionUI");
        interactionUIObj.transform.SetParent(transform);
        
        DrillTowerInteractionUI interactionUI = interactionUIObj.AddComponent<DrillTowerInteractionUI>();
        interactionUI.promptDistance = 3f;
        
        Debug.Log("🎮 钻塔交互UI系统初始化完成 - 靠近钻塔按F键交互");
    }
    
    void InitializeDem003Debugger()
    {
        // 创建dem.003专用调试器
        GameObject dem003DebuggerObj = new GameObject("Dem003RuntimeDebugger");
        dem003DebuggerObj.transform.SetParent(transform);
        
        Dem003RuntimeDebugger dem003Debugger = dem003DebuggerObj.AddComponent<Dem003RuntimeDebugger>();
        dem003Debugger.enableDebug = true;
        
        Debug.Log("🔍 dem.003专用调试器初始化完成 - 按P键进行调试分析");
    }
}
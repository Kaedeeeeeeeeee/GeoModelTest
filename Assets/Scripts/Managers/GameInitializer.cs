using UnityEngine;
using UnityEngine.UI;
using SampleCuttingSystem;

/// <summary>
/// 游戏初始化管理器 - 负责初始化新功能和工具
/// </summary>
public class GameInitializer : MonoBehaviour
{
    [Header("工具初始化")]
    public bool initializeDrillTower = true;
    public bool initializeUISystem = true;
    public bool initializeSampleSystem = true;
    public bool enableDebugMode = false;
    
    // 当为 true 时，不在启动时将其他工具注册到 ToolManager.availableTools，改由任务系统逐步解锁
    [SerializeField]
    private bool unlockToolsViaQuests = true;
    public Sprite drillTowerIcon;
    public GameObject existingDrillTowerPrefab; // 可以拖入现有的钻塔预制件
    
    [Header("材质设置")]
    public Material towerMaterial;
    public Material activeMaterial;
    public Material inactiveMaterial;
    
    void Start()
    {
        
        // 首先初始化多语言系统
        InitializeLocalizationSystem();
        
        if (initializeUISystem)
        {
            // 不再创建重复的UI系统，使用现有的InventoryUISystem
            InitializeToolManager();
            InitializeGeologySystem();
        }
        
        // 若采用任务解锁，不在启动时创建/注册钻塔工具，避免抢先出现在轮盘
        if (initializeDrillTower && !unlockToolsViaQuests)
        {
            InitializeDrillTowerTool();
            InitializeInteractionUI();
        }
        
        if (initializeSampleSystem)
        {
            InitializeSampleSystem();
        }
        
        // 初始化样本图标系统
        InitializeSampleIconSystem();
        
        // 初始化图标调试器（开发环境）
        if (enableDebugMode)
        {
            InitializeSampleIconDebugger();
        }
        
        // 初始化仓库系统
        InitializeWarehouseSystem();
        
        // 初始化样本切割系统（只在实验室场景中）
        InitializeCuttingSystem();
        
        // 初始化工作台系统（只在实验室场景中）
        InitializeWorkbenchSystem();
        
        if (enableDebugMode)
        {
            InitializeDebugger();
            InitializeGroundLayerFixer();
            InitializeVisibilityFixer();
        }
        
        
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
        
        
    }
    
    void InitializeGroundLayerFixer()
    {
        // 创建地面Layer修复器
        GameObject fixerObj = new GameObject("GroundLayerFixer");
        fixerObj.transform.SetParent(transform);
        
        GroundLayerFixer fixer = fixerObj.AddComponent<GroundLayerFixer>();
        fixer.autoFixOnStart = true;
        fixer.targetGroundLayer = 0; // Default layer
        
        
    }
    
    void InitializeVisibilityFixer()
    {
        // 创建可见性修复器
        GameObject visibilityFixerObj = new GameObject("DrillTowerVisibilityFixer");
        visibilityFixerObj.transform.SetParent(transform);
        
        DrillTowerVisibilityFixer visibilityFixer = visibilityFixerObj.AddComponent<DrillTowerVisibilityFixer>();
        visibilityFixer.autoFixOnStart = true;
        visibilityFixer.defaultColor = new Color(0.8f, 0.3f, 0.1f, 1f); // 橙红色
        
        
    }
    
    void InitializeInteractionUI()
    {
        // 创建交互UI系统
        GameObject interactionUIObj = new GameObject("DrillTowerInteractionUI");
        interactionUIObj.transform.SetParent(transform);
        
        DrillTowerInteractionUI interactionUI = interactionUIObj.AddComponent<DrillTowerInteractionUI>();
        interactionUI.promptDistance = 3f;
        
        
    }
    
    void EnsureUISystem()
    {
        // 查找或创建Canvas
        Canvas existingCanvas = FindFirstObjectByType<Canvas>();
        GameObject canvasObj;
        
        if (existingCanvas == null)
        {
            canvasObj = new GameObject("Collection UI Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            
            // 添加CanvasScaler保证UI自适应
            UnityEngine.UI.CanvasScaler scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            
            canvasObj.AddComponent<GraphicRaycaster>();
            
            // 添加EventSystem如果不存在（修复Input System兼容性）
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
        }
        else
        {
            canvasObj = existingCanvas.gameObject;
        }
        
        // 确保Canvas有InventoryUISystem组件
        InventoryUISystem inventoryUI = canvasObj.GetComponent<InventoryUISystem>();
        if (inventoryUI == null)
        {
            inventoryUI = canvasObj.AddComponent<InventoryUISystem>();
        }
        
        if (enableDebugMode) Debug.Log("UI系统已确保存在 - 使用InventoryUISystem");
    }
    
    void InitializeToolManager()
    {
        // 确保有Canvas和InventoryUISystem
        EnsureUISystem();
        
        // 查找玩家对象
        GameObject lily = GameObject.Find("Lily");
        if (lily == null)
        {
            Debug.LogWarning("未找到Lily对象，尝试查找主摄像机");
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                lily = mainCam.gameObject;
            }
        }
        
        if (lily != null)
        {
            // 添加工具管理器
            ToolManager toolManager = lily.GetComponent<ToolManager>();
            if (toolManager == null)
            {
                toolManager = lily.AddComponent<ToolManager>();
                if (enableDebugMode) Debug.Log("已为玩家添加工具管理器");
            }
            
            // 添加无人机工具
            DroneTool droneTool = lily.GetComponent<DroneTool>();
            if (droneTool == null)
            {
                droneTool = lily.AddComponent<DroneTool>();
                droneTool.toolID = "1100";
                droneTool.toolName = "无人机";
                if (enableDebugMode) Debug.Log("已添加无人机工具");
            }
            
            // 添加简易钻探工具
            SimpleDrillTool simpleDrillTool = lily.GetComponent<SimpleDrillTool>();
            if (simpleDrillTool == null)
            {
                simpleDrillTool = lily.AddComponent<SimpleDrillTool>();
                simpleDrillTool.toolID = "1000";
                simpleDrillTool.toolName = "简易钻探";
                if (enableDebugMode) Debug.Log("已添加简易钻探工具");
            }
            
            // 添加钻探车工具
            DrillCarTool drillCarTool = lily.GetComponent<DrillCarTool>();
            if (drillCarTool == null)
            {
                drillCarTool = lily.AddComponent<DrillCarTool>();
                drillCarTool.toolID = "1101";
                drillCarTool.toolName = "钻探车";
                
                // 尝试找到DrillCar预制体
                #if UNITY_EDITOR
                string[] searchTerms = {"DrillCar", "Drill Car", "DrillCar t:Prefab", "Drill Car t:Prefab"};
                GameObject carPrefab = null;
                string foundPath = "";
                
                foreach (string searchTerm in searchTerms)
                {
                    string[] guids = UnityEditor.AssetDatabase.FindAssets(searchTerm);
                    foreach (string guid in guids)
                    {
                        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                        if (path.EndsWith(".prefab"))
                        {
                            GameObject testPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                            if (testPrefab != null && (testPrefab.name.Contains("Drill") || testPrefab.name.Contains("Car")))
                            {
                                carPrefab = testPrefab;
                                foundPath = path;
                                break;
                            }
                        }
                    }
                    if (carPrefab != null) break;
                }
                
                if (carPrefab != null)
                {
                    // 设置预制体字段
                    drillCarTool.prefabToPlace = carPrefab;
                    if (enableDebugMode) Debug.Log($"已设置DrillCar预制体: {foundPath}");
                }
                else
                {
                    Debug.LogWarning("未找到DrillCar预制体");
                }
                #endif
                
                Debug.Log("已添加钻探车工具");
            }
            
            // 添加地质锤工具
            HammerTool hammerTool = lily.GetComponent<HammerTool>();
            if (hammerTool == null)
            {
                hammerTool = lily.AddComponent<HammerTool>();
                hammerTool.toolID = "1002";
                hammerTool.toolName = "地质锤";
                
                // 尝试找到Hammer预制体
                #if UNITY_EDITOR
                string[] hammerSearchTerms = {"Hammer", "hammer", "Hammer t:Prefab"};
                GameObject hammerPrefab = null;
                string hammerFoundPath = "";
                
                foreach (string searchTerm in hammerSearchTerms)
                {
                    string[] guids = UnityEditor.AssetDatabase.FindAssets(searchTerm);
                    foreach (string guid in guids)
                    {
                        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                        if (path.EndsWith(".prefab") && path.Contains("Hammer"))
                        {
                            GameObject testPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                            if (testPrefab != null && testPrefab.name.Contains("Hammer"))
                            {
                                hammerPrefab = testPrefab;
                                hammerFoundPath = path;
                                break;
                            }
                        }
                    }
                    if (hammerPrefab != null) break;
                }
                
                if (hammerPrefab != null)
                {
                    // 设置锤子预制体字段
                    hammerTool.hammerPrefab = hammerPrefab;
                    Debug.Log($"已设置Hammer预制体: {hammerFoundPath}");
                }
                else
                {
                    Debug.LogWarning("未找到Hammer预制体，锤子工具可能无法正常显示");
                }
                #endif
                
                Debug.Log("已添加地质锤工具");
            }
            
            // 按需注册工具：若采用任务解锁，则初始为空（仅保留场景切换器由ToolManager内部创建）
            if (unlockToolsViaQuests)
            {
                toolManager.availableTools = new CollectionTool[0];
                if (enableDebugMode) Debug.Log("工具注册延迟：由任务系统按需解锁");
            }
            else
            {
                // 自动发现并注册所有工具（旧行为）
                RefreshToolManager(toolManager);
            }
        }
        else
        {
            Debug.LogError("无法找到玩家对象来添加工具管理器！");
        }
    }
    
    void RefreshToolManager(ToolManager toolManager)
    {
        // 查找所有工具组件
        CollectionTool[] tools = toolManager.GetComponents<CollectionTool>();
        
        if (tools.Length > 0)
        {
            toolManager.availableTools = tools;
            Debug.Log($"已注册 {tools.Length} 个工具到工具管理器");
            
            // 查找InventoryUISystem并更新工具列表
            InventoryUISystem inventoryUI = FindFirstObjectByType<InventoryUISystem>();
            if (inventoryUI != null)
            {
                // 调用RefreshTools如果方法存在
                var refreshMethod = inventoryUI.GetType().GetMethod("RefreshTools");
                if (refreshMethod != null)
                {
                    refreshMethod.Invoke(inventoryUI, null);
                    Debug.Log("已刷新UI工具列表");
                }
            }
        }
        else
        {
            Debug.LogWarning("未找到任何工具组件");
        }
    }
    
    void InitializeGeologySystem()
    {
        // 确保有GeometricSampleReconstructor组件用于样本重建
        GeometricSampleReconstructor reconstructor = FindFirstObjectByType<GeometricSampleReconstructor>();
        if (reconstructor == null)
        {
            GameObject reconstructorObj = new GameObject("GeometricSampleReconstructor");
            reconstructor = reconstructorObj.AddComponent<GeometricSampleReconstructor>();
            Debug.Log("游戏初始化：创建了GeometricSampleReconstructor");
        }
        else
        {
            Debug.Log("游戏初始化：找到现有的GeometricSampleReconstructor");
        }
    }
    
    void InitializeSampleSystem()
    {
        Debug.Log("开始初始化样本采集和背包系统...");
        
        // 0. 先清理重复的Canvas
        CleanupDuplicateCanvases();
        
        // 1. 创建或查找样本系统初始化器
        SampleSystemInitializer systemInitializer = FindFirstObjectByType<SampleSystemInitializer>();
        if (systemInitializer == null)
        {
            GameObject initializerObj = new GameObject("SampleSystemInitializer");
            systemInitializer = initializerObj.AddComponent<SampleSystemInitializer>();
            Debug.Log("创建了 SampleSystemInitializer");
        }
        
        // 2. 创建样本背包
        if (SampleInventory.Instance == null)
        {
            GameObject inventoryObj = new GameObject("SampleInventory");
            inventoryObj.AddComponent<SampleInventory>();
            Debug.Log("创建了 SampleInventory");
        }
        
        // 3. 创建背包UI
        InventoryUI inventoryUI = FindFirstObjectByType<InventoryUI>();
        if (inventoryUI == null)
        {
            GameObject uiObj = new GameObject("InventoryUI");
            uiObj.AddComponent<InventoryUI>();
            Debug.Log("创建了 InventoryUI");
        }
        
        // 4. 创建样本放置器
        if (SamplePlacer.Instance == null)
        {
            GameObject placerObj = new GameObject("SamplePlacer");
            placerObj.AddComponent<SamplePlacer>();
            Debug.Log("创建了 SamplePlacer");
        }
        
        // 5. 创建样本跟踪器
        if (PlacedSampleTracker.Instance == null)
        {
            GameObject trackerObj = new GameObject("PlacedSampleTracker");
            trackerObj.AddComponent<PlacedSampleTracker>();
            Debug.Log("创建了 PlacedSampleTracker");
        }
        
        // 6. 创建钻探工具集成器
        if (DrillToolSampleIntegrator.Instance == null)
        {
            GameObject integratorObj = new GameObject("DrillToolSampleIntegrator");
            integratorObj.AddComponent<DrillToolSampleIntegrator>();
            Debug.Log("创建了 DrillToolSampleIntegrator");
        }
        
        // 7. 创建预览生成器（可选）
        if (SamplePreviewGenerator.Instance == null)
        {
            GameObject generatorObj = new GameObject("SamplePreviewGenerator");
            generatorObj.AddComponent<SamplePreviewGenerator>();
            Debug.Log("创建了 SamplePreviewGenerator");
        }
        
        // 8. 创建手动样本设置工具
        ManualSampleSetup sampleSetup = FindFirstObjectByType<ManualSampleSetup>();
        if (sampleSetup == null)
        {
            GameObject setupObj = new GameObject("ManualSampleSetup");
            sampleSetup = setupObj.AddComponent<ManualSampleSetup>();
            Debug.Log("创建了 ManualSampleSetup");
        }
        
        Debug.Log("样本采集和背包系统初始化完成！");
        Debug.Log("使用说明:");
        Debug.Log("- E键: 采集/收回样本");
        Debug.Log("- I键: 打开/关闭背包");
        Debug.Log("- 在背包中点击样本查看详情，可以拿出到世界中");
    }
    
    /// <summary>
    /// 初始化样本图标系统
    /// </summary>
    void InitializeSampleIconSystem()
    {
        Debug.Log("开始初始化样本图标系统...");
        
        // 创建或查找样本图标初始化器
        SampleIconInitializer iconInitializer = FindFirstObjectByType<SampleIconInitializer>();
        if (iconInitializer == null)
        {
            GameObject initializerObj = new GameObject("SampleIconInitializer");
            iconInitializer = initializerObj.AddComponent<SampleIconInitializer>();
            Debug.Log("创建了新的SampleIconInitializer");
        }
        
        // 验证图标系统
        iconInitializer.ValidateIconSystem();
        
        Debug.Log("样本图标系统初始化完成！");
        Debug.Log("功能说明:");
        Debug.Log("- 钻探样本显示为圆柱形图标");
        Debug.Log("- 地质锤样本显示为薄片形图标");
        Debug.Log("- 图标颜色反映样本的实际地质层颜色");
    }
    
    /// <summary>
    /// 初始化样本图标调试器
    /// </summary>
    void InitializeSampleIconDebugger()
    {
        Debug.Log("开始初始化样本图标调试器...");
        
        // 创建或查找调试器
        SampleIconDebugger iconDebugger = FindFirstObjectByType<SampleIconDebugger>();
        if (iconDebugger == null)
        {
            GameObject debuggerObj = new GameObject("SampleIconDebugger");
            iconDebugger = debuggerObj.AddComponent<SampleIconDebugger>();
            iconDebugger.enableDetailedLogging = true;
            iconDebugger.refreshIconsOnStart = false; // 避免自动刷新，让用户手动触发
            Debug.Log("创建了新的SampleIconDebugger");
        }
        
        Debug.Log("样本图标调试器初始化完成！");
        Debug.Log("调试功能:");
        Debug.Log("- 右键点击SampleIconDebugger组件可访问调试菜单");
        Debug.Log("- '调试并刷新所有样本图标' - 重新生成所有图标并输出详细日志");
        Debug.Log("- '分析样本地质层数据' - 分析地质层颜色数据质量");
        Debug.Log("- '测试颜色亮度判断' - 测试颜色过滤算法");
    }
    
    /// <summary>
    /// 初始化仓库系统
    /// </summary>
    void InitializeWarehouseSystem()
    {
        Debug.Log("开始初始化仓库系统...");
        
        // 创建或查找仓库系统初始化器
        WarehouseGameInitializer warehouseInitializer = FindFirstObjectByType<WarehouseGameInitializer>();
        if (warehouseInitializer == null)
        {
            GameObject initializerObj = new GameObject("WarehouseGameInitializer");
            warehouseInitializer = initializerObj.AddComponent<WarehouseGameInitializer>();
            Debug.Log("创建了新的WarehouseGameInitializer");
        }
        
        // 确保仓库系统初始化
        if (!warehouseInitializer.IsInitialized())
        {
            warehouseInitializer.InitializeWarehouseSystem();
        }
        
        Debug.Log("仓库系统初始化完成！");
    }
    
    /// <summary>
    /// 初始化多语言系统
    /// </summary>
    void InitializeLocalizationSystem()
    {
        Debug.Log("开始初始化多语言系统...");
        
        // 创建或查找本地化初始化器
        LocalizationInitializer localizationInitializer = FindFirstObjectByType<LocalizationInitializer>();
        if (localizationInitializer == null)
        {
            GameObject initializerObj = new GameObject("LocalizationInitializer");
            localizationInitializer = initializerObj.AddComponent<LocalizationInitializer>();
            Debug.Log("创建了新的LocalizationInitializer");
        }
        
        // 确保本地化系统初始化
        if (!localizationInitializer.IsInitialized)
        {
            localizationInitializer.InitializeLocalizationSystem();
        }
        
        Debug.Log("多语言系统初始化完成！");
    }
    
    /// <summary>
    /// 清理重复的Canvas对象
    /// </summary>
    void CleanupDuplicateCanvases()
    {
        CanvasCleanupUtility cleanupUtility = FindFirstObjectByType<CanvasCleanupUtility>();
        if (cleanupUtility == null)
        {
            // 创建临时清理工具
            GameObject tempCleanupObj = new GameObject("TempCanvasCleanup");
            cleanupUtility = tempCleanupObj.AddComponent<CanvasCleanupUtility>();
            cleanupUtility.enableDebugLog = enableDebugMode;
            
            // 执行清理
            cleanupUtility.CleanupDuplicateCanvases();
            
            // 清理完成后销毁临时对象
            Destroy(tempCleanupObj);
        }
        else
        {
            // 使用现有的清理工具
            cleanupUtility.CleanupDuplicateCanvases();
        }
    }
    
    /// <summary>
    /// 初始化样本切割系统（只在实验室场景中）
    /// </summary>
    void InitializeCuttingSystem()
    {
        // 检查是否在实验室场景
        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (!currentSceneName.Contains("Laboratory") && !currentSceneName.Contains("实验室"))
        {
            Debug.Log("当前不在实验室场景，跳过切割系统初始化");
            return;
        }
        
        Debug.Log("开始初始化样本切割系统...");
        
        // 查找现有的切割台
        GameObject cuttingStation = GameObject.FindGameObjectWithTag("CuttingStation");
        
        if (cuttingStation == null)
        {
            Debug.Log("未找到切割台标签，查找按名称");
            cuttingStation = GameObject.Find("SampleCuttingStation");
        }
        
        if (cuttingStation != null)
        {
            Debug.Log("找到现有切割台，检查组件完整性");
            
            // 确保有初始化器组件
            var initializer = cuttingStation.GetComponent<SampleCuttingSystemInitializer>();
            if (initializer == null)
            {
                initializer = cuttingStation.AddComponent<SampleCuttingSystemInitializer>();
                Debug.Log("添加切割系统初始化器");
            }
            
            // 触发初始化
            initializer.InitializeSystem();
        }
        else
        {
            Debug.Log("未找到切割台，将在需要时创建");
            Debug.Log("提示：可以使用 工具→样本切割系统→集成到实验室 来手动创建切割台");
        }
    }
    
    /// <summary>
    /// 初始化工作台系统（只在实验室场景中）
    /// </summary>
    void InitializeWorkbenchSystem()
    {
        // 检查是否在实验室场景
        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (!currentSceneName.Contains("Laboratory") && !currentSceneName.Contains("实验室"))
        {
            return;
        }
        
        Debug.Log("开始初始化工作台系统...");
        
        // 1. 查找目标物体 "shelf (8)"
        GameObject shelf = GameObject.Find("shelf (8)");
        if (shelf == null)
        {
            Debug.LogWarning("未找到 'shelf (8)'，无法自动配置工作台。");
            return;
        }
        
        // 2. 确保有 WorkbenchController 组件
        var controller = shelf.GetComponent<WorkbenchSystem.WorkbenchController>();
        if (controller == null)
        {
            controller = shelf.AddComponent<WorkbenchSystem.WorkbenchController>();
            Debug.Log("已为 'shelf (8)' 添加 WorkbenchController 组件");
        }
        
        // 3. 查找或创建 WorkbenchCamera
        if (controller.workbenchCamera == null)
        {
            GameObject camObj = GameObject.Find("WorkbenchCamera");
            if (camObj == null)
            {
                camObj = new GameObject("WorkbenchCamera");
                Camera cam = camObj.AddComponent<Camera>();
                cam.enabled = true; // 启用组件，具体显示由激活状态控制
                ConfigureDefaultWorkbenchCamera(cam, shelf);
                
                Debug.Log("创建了 'WorkbenchCamera' 并设置了默认俯视位置");
            }
            
            controller.workbenchCamera = camObj.GetComponent<Camera>();
        }
        
        // 4. 设置交互目标
        if (controller.interactionTarget == null)
        {
            controller.interactionTarget = shelf.transform;
        }
        
        Debug.Log("工作台系统初始化完成！");
    }

    void ConfigureDefaultWorkbenchCamera(Camera cam, GameObject shelf)
    {
        if (cam == null || shelf == null) return;

        // 合并渲染器/碰撞体的包围盒，估算桌面大小
        Bounds bounds = new Bounds(shelf.transform.position, Vector3.one);
        bool hasBounds = false;

        var renderers = shelf.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            if (!hasBounds)
            {
                bounds = r.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }
        }

        if (!hasBounds)
        {
            var colliders = shelf.GetComponentsInChildren<Collider>();
            foreach (var c in colliders)
            {
                if (!hasBounds)
                {
                    bounds = c.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(c.bounds);
                }
            }
        }

        if (!hasBounds)
        {
            bounds = new Bounds(shelf.transform.position + Vector3.up * 0.8f, new Vector3(1.5f, 1f, 1.5f));
        }

        Vector3 center = bounds.center;
        float horizontalExtent = Mathf.Max(bounds.extents.x, bounds.extents.z);
        float margin = Mathf.Max(0.25f, horizontalExtent * 0.35f);
        float lookDistance = Mathf.Max(1.5f, horizontalExtent + margin);
        float height = Mathf.Max(bounds.size.y, 0.6f) + lookDistance * 0.75f;

        // 透视相机，参数尽量贴合主角相机，方便自由调整
        float defaultFov = 60f;
        if (Camera.main != null)
        {
            defaultFov = Camera.main.fieldOfView;
        }
        cam.orthographic = false;
        cam.fieldOfView = defaultFov;
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = Mathf.Max(50f, (height + lookDistance) * 4f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.07f, 0.07f, 0.07f, 1f);
        cam.cullingMask = ~0; // 默认渲染所有层

        // 使用用户提供的视角（本地变换），方便自由微调
        cam.transform.SetParent(shelf.transform, false);
        cam.transform.localPosition = new Vector3(0f, 1.19799995f, -0.545000017f);
        cam.transform.localRotation = new Quaternion(0.47813347f, 0f, 0f, 0.878287137f);
        cam.transform.localScale = new Vector3(0.563881993f, 0.769636571f, 0.769241333f);

        // 默认禁用，按需启用
        cam.gameObject.SetActive(false);
    }
}

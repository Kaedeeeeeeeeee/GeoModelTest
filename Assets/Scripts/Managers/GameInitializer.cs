using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 游戏初始化管理器 - 负责初始化新功能和工具
/// </summary>
public class GameInitializer : MonoBehaviour
{
    [Header("工具初始化")]
    public bool initializeDrillTower = true;
    public bool initializeUISystem = true;
    public bool initializeSampleSystem = true;
    public bool enableDebugMode = true;
    public Sprite drillTowerIcon;
    public GameObject existingDrillTowerPrefab; // 可以拖入现有的钻塔预制件
    
    [Header("材质设置")]
    public Material towerMaterial;
    public Material activeMaterial;
    public Material inactiveMaterial;
    
    void Start()
    {
        
        
        if (initializeUISystem)
        {
            // 不再创建重复的UI系统，使用现有的InventoryUISystem
            InitializeToolManager();
            InitializeGeologySystem();
        }
        
        if (initializeDrillTower)
        {
            InitializeDrillTowerTool();
            InitializeInteractionUI();
        }
        
        if (initializeSampleSystem)
        {
            InitializeSampleSystem();
        }
        
        // 初始化仓库系统
        InitializeWarehouseSystem();
        
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
        
        Debug.Log("UI系统已确保存在 - 使用InventoryUISystem");
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
                Debug.Log("已为玩家添加工具管理器");
            }
            
            // 添加无人机工具
            DroneTool droneTool = lily.GetComponent<DroneTool>();
            if (droneTool == null)
            {
                droneTool = lily.AddComponent<DroneTool>();
                droneTool.toolID = "1100";
                droneTool.toolName = "无人机";
                Debug.Log("已添加无人机工具");
            }
            
            // 添加简易钻探工具
            SimpleDrillTool simpleDrillTool = lily.GetComponent<SimpleDrillTool>();
            if (simpleDrillTool == null)
            {
                simpleDrillTool = lily.AddComponent<SimpleDrillTool>();
                simpleDrillTool.toolID = "1000";
                simpleDrillTool.toolName = "简易钻探";
                Debug.Log("已添加简易钻探工具");
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
                    Debug.Log($"已设置DrillCar预制体: {foundPath}");
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
            
            // 自动发现并注册所有工具
            RefreshToolManager(toolManager);
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
    
}
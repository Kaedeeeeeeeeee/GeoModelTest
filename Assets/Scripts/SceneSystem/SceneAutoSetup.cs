using UnityEngine;
using System.Collections;

/// <summary>
/// 场景自动设置器 - 确保每个场景都有必要的游戏系统
/// </summary>
public class SceneAutoSetup : MonoBehaviour
{
    [Header("自动设置配置")]
    public bool autoSetupOnSceneLoad = true;
    public float setupDelay = 0.2f;
    [Tooltip("运行时（有GameSceneManager时）是否仍然执行自动设置。默认关闭以避免与场景系统冲突。")]
    public bool enableRuntimeAutoSetup = false;
    
    [Header("Player设置")]
    public Vector3 defaultPlayerPosition = new Vector3(-29.9230003f, 14.3459997f, -20.9599991f);
    public Quaternion defaultPlayerRotation = new Quaternion(0f, 0.995849609f, 0f, 0.0910143629f);
    
    [Header("Laboratory Scene特殊设置")]
    public Vector3 laboratoryPlayerPosition = new Vector3(0f, 0.167999998f, 4.52699995f);
    public Quaternion laboratoryPlayerRotation = Quaternion.identity;
    
    void Start()
    {
        if (ShouldSkipRuntimeAutoSetup())
        {
            Debug.Log($"{GetTimestamp()} [SceneAutoSetup] 检测到运行时场景管理器，跳过自动设置");
            return;
        }

        if (autoSetupOnSceneLoad)
        {
            StartCoroutine(AutoSetupScene());
        }
    }

    bool ShouldSkipRuntimeAutoSetup()
    {
        if (!Application.isPlaying)
        {
            return false;
        }

        if (enableRuntimeAutoSetup)
        {
            return false;
        }

        var sceneManager = FindFirstObjectByType<GameSceneManager>();
        if (sceneManager == null)
        {
            return false;
        }

        bool playerExists = FindFirstObjectByType<FirstPersonController>() != null;
        return playerExists;
    }
    
    IEnumerator AutoSetupScene()
    {
        yield return new WaitForSeconds(setupDelay);
        
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        Debug.Log($"=== 开始自动设置场景: {sceneName} ===");
        
        // 1. 立即清理重复的AudioListener
        CleanupDuplicateAudioListeners();
        
        // 2. 确保有Player系统
        SetupPlayerSystem(sceneName);
        
        // 3. 确保有UI系统
        SetupUISystem();
        
        // 4. 确保有工具系统
        SetupToolSystem();
        
        // 5. 确保有场景管理器
        SetupSceneManager();
        
        // 6. 最后再次清理AudioListener（防止创建过程中产生重复）
        CleanupDuplicateAudioListeners();
        
        Debug.Log($"=== 场景自动设置完成: {sceneName} ===");
    }

    
    /// <summary>
    /// 设置Player系统
    /// </summary>
    void SetupPlayerSystem(string sceneName)
    {
        FirstPersonController existingPlayer = FindFirstObjectByType<FirstPersonController>();
        
        if (existingPlayer != null)
        {
            Debug.Log($"{GetTimestamp()} [SceneAutoSetup] Player系统已存在 -> {existingPlayer.name}，位置 {existingPlayer.transform.position} (场景: {sceneName})");
            return;
        }
        
        Debug.Log($"{GetTimestamp()} [SceneAutoSetup] 未检测到Player，准备创建（场景: {sceneName}）");
        
        // 尝试从MainScene复制Player设置
        bool playerCreated = TryCreatePlayerFromMainScene(sceneName);
        
        if (!playerCreated)
        {
            // 备用方案：创建基础Player
            CreateBasicPlayerSystem(sceneName);
        }
    }
    
    /// <summary>
    /// 尝试从用户的Lily预制体创建Player
    /// </summary>
    bool TryCreatePlayerFromMainScene(string sceneName)
    {
        if (sceneName == "MainScene")
        {
            return false; // 如果已经在MainScene，不需要复制
        }
        
        // 尝试直接实例化用户的Lily预制体
        GameObject lilyPrefab = null;
        
        // 方法1：从AssetDatabase加载用户指定的Lily预制体（仅Editor模式）
#if UNITY_EDITOR
        lilyPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Model/Player/Lily.prefab");
#endif
        
        // 方法2：从Resources加载备用路径
        if (lilyPrefab == null)
        {
            lilyPrefab = Resources.Load<GameObject>("Model/Player/Lily");
        }
        
        if (lilyPrefab != null)
        {
            // 直接实例化完整的Lily预制体
            GameObject playerInstance = Instantiate(lilyPrefab);
            playerInstance.name = "Lily";
            
            // 设置位置
            Vector3 playerPos = GetPlayerPositionForScene(sceneName);
            Quaternion playerRot = GetPlayerRotationForScene(sceneName);
            playerInstance.transform.position = playerPos;
            playerInstance.transform.rotation = playerRot;
            
            // 确保摄像机标签正确
            Camera playerCamera = playerInstance.GetComponentInChildren<Camera>();
            if (playerCamera != null)
            {
                playerCamera.tag = "MainCamera";
            }
            
            Debug.Log($"{GetTimestamp()} [SceneAutoSetup] 从Lily预制体创建Player，位置 {playerPos}，场景 {sceneName}\n{new System.Diagnostics.StackTrace(true)}");
            
            NotifyPlayerPersistentData(sceneName);
            return true;
        }
        else
        {
            Debug.LogWarning($"{GetTimestamp()} [SceneAutoSetup] 无法加载Lily预制体，改用基础玩家方案（场景: {sceneName}）");
            return false;
        }
    }
    
    
    /// <summary>
    /// 创建基础Player系统（备用方案）
    /// </summary>
    void CreateBasicPlayerSystem(string sceneName)
    {
        Debug.LogWarning($"{GetTimestamp()} [SceneAutoSetup] 使用备用方案创建基础Player（场景: {sceneName}）");
        
        // 创建Player对象
        GameObject playerObj = new GameObject("Lily");
        FirstPersonController fpController = playerObj.AddComponent<FirstPersonController>();
        
        // 设置位置
        Vector3 playerPos = GetPlayerPositionForScene(sceneName);
        Quaternion playerRot = GetPlayerRotationForScene(sceneName);
        playerObj.transform.position = playerPos;
        playerObj.transform.rotation = playerRot;
        
        // 添加Character Controller
        CharacterController characterController = playerObj.AddComponent<CharacterController>();
        characterController.center = new Vector3(0, 1, 0);
        characterController.radius = 0.5f;
        characterController.height = 2f;
        
        // 创建摄像机
        GameObject cameraObj = new GameObject("Main Camera");
        cameraObj.transform.SetParent(playerObj.transform);
        cameraObj.transform.localPosition = new Vector3(0, 1.6f, 0);
        cameraObj.transform.localRotation = Quaternion.identity;
        
        Camera camera = cameraObj.AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.fieldOfView = 60f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 1000f;
        
        // 添加Audio Listener（确保场景中只有一个）
        AudioListener[] existingListeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        if (existingListeners.Length == 0)
        {
            cameraObj.AddComponent<AudioListener>();
            Debug.Log("✅ 添加AudioListener到基础Player摄像机");
        }
        else
        {
            Debug.Log($"⚠️ 场景中已存在 {existingListeners.Length} 个AudioListener，跳过添加");
        }
        
        Debug.Log($"{GetTimestamp()} [SceneAutoSetup] 基础Player系统创建完成，位置 {playerPos} (场景: {sceneName})\n{new System.Diagnostics.StackTrace(true)}");

        NotifyPlayerPersistentData(sceneName);
    }
    
    /// <summary>
    /// 获取场景专用的Player位置
    /// </summary>
    Vector3 GetPlayerPositionForScene(string sceneName)
    {
        switch (sceneName)
        {
            case "Laboratory Scene":
                return laboratoryPlayerPosition;
            case "MainScene":
                return defaultPlayerPosition;
            default:
                return defaultPlayerPosition;
        }
    }
    
    /// <summary>
    /// 获取场景专用的Player旋转
    /// </summary>
    Quaternion GetPlayerRotationForScene(string sceneName)
    {
        switch (sceneName)
        {
            case "Laboratory Scene":
                return laboratoryPlayerRotation;
            case "MainScene":
                return defaultPlayerRotation;
            default:
                return defaultPlayerRotation;
        }
    }
    
    /// <summary>
    /// 设置UI系统
    /// </summary>
    void SetupUISystem()
    {
        InventoryUISystem existingUI = FindFirstObjectByType<InventoryUISystem>();
        
        if (existingUI != null)
        {
            Debug.Log("✅ UI系统已存在");
            return;
        }
        
        Debug.Log("🔧 创建UI系统");
        
        // 清理可能冲突的Canvas（如SamplePromptCanvas）
        CleanupConflictingCanvases();
        
        // 创建UI Canvas
        GameObject canvasObj = new GameObject("InventoryUICanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        
        // 添加Canvas Scaler
        UnityEngine.UI.CanvasScaler scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        
        // 添加Graphic Raycaster
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        // 添加InventoryUISystem组件
        InventoryUISystem inventoryUI = canvasObj.AddComponent<InventoryUISystem>();
        
        // 确保EventSystem存在
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            // 使用新的Input System UI输入模块
            eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
        
        // 设置完整的背包系统
        SetupInventorySystem();
        
        // 初始化GameInitializer来确保所有系统正常工作
        SetupGameInitializer();
        
        Debug.Log("✅ UI系统创建完成");
    }
    
    /// <summary>
    /// 设置背包系统
    /// </summary>
    void SetupInventorySystem()
    {
        // 检查是否已有背包系统
        if (FindFirstObjectByType<SampleInventory>() != null)
        {
            Debug.Log("✅ 背包系统已存在");
            return;
        }
        
        Debug.Log("🔧 创建背包系统");
        
        // 创建SampleInventory
        GameObject sampleInventoryObj = new GameObject("SampleInventory");
        SampleInventory sampleInventory = sampleInventoryObj.AddComponent<SampleInventory>();
        
        // 创建InventoryUI
        GameObject inventoryUIObj = new GameObject("InventoryUI");
        InventoryUI inventoryUI = inventoryUIObj.AddComponent<InventoryUI>();
        
        Debug.Log("✅ 背包系统创建完成");
    }
    
    /// <summary>
    /// 设置GameInitializer来初始化完整系统
    /// </summary>
    void SetupGameInitializer()
    {
        // 检查是否已有GameInitializer
        if (FindFirstObjectByType<GameInitializer>() != null)
        {
            Debug.Log("✅ GameInitializer已存在");
            return;
        }
        
        Debug.Log("🔧 创建GameInitializer");
        
        // 创建GameInitializer
        GameObject initializerObj = new GameObject("GameInitializer");
        GameInitializer initializer = initializerObj.AddComponent<GameInitializer>();
        
        // 配置GameInitializer
        initializer.initializeDrillTower = true;
        initializer.initializeUISystem = true;
        initializer.initializeSampleSystem = true;
        initializer.enableDebugMode = false; // 在自动创建的场景中禁用调试模式
        
        Debug.Log("✅ GameInitializer创建完成");
    }
    
    /// <summary>
    /// 清理可能与TabUI冲突的Canvas
    /// </summary>
    void CleanupConflictingCanvases()
    {
        // 查找所有可能冲突的Canvas
        string[] conflictingCanvasNames = {
            "SamplePromptCanvas",
            "PlacedSamplePromptCanvas",
            "DrillTowerInteractionCanvas"
        };
        
        Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        int cleanedCount = 0;
        
        foreach (Canvas canvas in allCanvases)
        {
            if (canvas != null)
            {
                string canvasName = canvas.gameObject.name;
                
                foreach (string conflictingName in conflictingCanvasNames)
                {
                    if (canvasName.Contains(conflictingName))
                    {
                        Debug.Log($"🧹 清理冲突Canvas: {canvasName}");
                        DestroyImmediate(canvas.gameObject);
                        cleanedCount++;
                        break;
                    }
                }
                
                // 检查是否有包含"Cycle"的子对象
                if (canvas.transform.Find("Cycle") != null)
                {
                    Debug.Log($"🧹 清理包含Cycle的Canvas: {canvasName}");
                    DestroyImmediate(canvas.gameObject);
                    cleanedCount++;
                }
            }
        }
        
        if (cleanedCount > 0)
        {
            Debug.Log($"✅ 清理了 {cleanedCount} 个可能冲突的Canvas");
        }
    }
    
    /// <summary>
    /// 设置工具系统
    /// </summary>
    void SetupToolSystem()
    {
        ToolManager existingToolManager = FindFirstObjectByType<ToolManager>();
        
        if (existingToolManager != null)
        {
            Debug.Log("✅ 工具系统已存在");
            return;
        }
        
        Debug.Log("🔧 创建工具系统");
        
        // 创建ToolManager
        GameObject toolManagerObj = new GameObject("ToolManager");
        ToolManager toolManager = toolManagerObj.AddComponent<ToolManager>();
        
        // 初始化空的工具数组
        toolManager.availableTools = new CollectionTool[0];
        
        Debug.Log("✅ 工具系统创建完成");
    }
    
    /// <summary>
    /// 设置场景管理器
    /// </summary>
    void SetupSceneManager()
    {
        GameSceneManager existingSceneManager = GameSceneManager.Instance;
        
        if (existingSceneManager != null)
        {
            Debug.Log("✅ 场景管理器已存在");
            return;
        }
        
        Debug.Log("🔧 创建场景管理器");
        
        // GameSceneManager是单例，调用Instance会自动创建
        var sceneManager = GameSceneManager.Instance;
        
        Debug.Log("✅ 场景管理器创建完成");
    }
    
    /// <summary>
    /// 手动触发场景设置
    /// </summary>
    public void ManualSetup()
    {
        StartCoroutine(AutoSetupScene());
    }
    
    /// <summary>
    /// 静态方法：为任何场景快速设置
    /// </summary>
    public static void QuickSetupScene()
    {
        GameObject setupObj = new GameObject("SceneAutoSetup_Manual");
        SceneAutoSetup setup = setupObj.AddComponent<SceneAutoSetup>();
        setup.autoSetupOnSceneLoad = true;
        setup.setupDelay = 0.1f;
        
        Debug.Log("手动启动场景自动设置");
    }
    
    /// <summary>
    /// 清理重复的AudioListener
    /// </summary>
    void CleanupDuplicateAudioListeners()
    {
        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        
        if (listeners.Length > 1)
        {
            Debug.Log($"🧹 发现 {listeners.Length} 个AudioListener，清理重复项");
            
            AudioListener keepListener = null;
            
            foreach (var listener in listeners)
            {
                if (listener != null)
                {
                    // 优先保留MainCamera上的AudioListener
                    Camera camera = listener.GetComponent<Camera>();
                    if (camera != null && camera.CompareTag("MainCamera") && keepListener == null)
                    {
                        keepListener = listener;
                        Debug.Log($"✅ 保留MainCamera上的AudioListener: {listener.name}");
                    }
                    else if (listener != keepListener)
                    {
                        Debug.Log($"🗑️ 删除重复AudioListener: {listener.name}");
                        DestroyImmediate(listener);
                    }
                }
            }
            
            // 如果没有找到MainCamera上的AudioListener，保留第一个
            if (keepListener == null && listeners.Length > 0)
            {
                keepListener = listeners[0];
                Debug.Log($"✅ 保留第一个AudioListener: {keepListener.name}");
                
                // 删除其他的
                for (int i = 1; i < listeners.Length; i++)
                {
                    if (listeners[i] != null)
                    {
                        Debug.Log($"🗑️ 删除重复AudioListener: {listeners[i].name}");
                        DestroyImmediate(listeners[i]);
                    }
                }
            }
        }
        else if (listeners.Length == 1)
        {
            Debug.Log($"✅ 场景中有 1 个AudioListener，无需清理");
        }
        else
        {
            Debug.Log($"⚠️ 场景中没有AudioListener");
        }
    }

    string GetTimestamp()
    {
        return $"[{Time.time:F3}s]";
    }

    void NotifyPlayerPersistentData(string sceneName)
    {
        if (sceneName != "Laboratory Scene")
        {
            return;
        }

        var persistent = FindFirstObjectByType<PlayerPersistentData>();
        if (persistent != null)
        {
            persistent.ForceSetPlayerToLaboratorySpawn();
            Debug.Log($"{GetTimestamp()} [SceneAutoSetup] 已通知PlayerPersistentData在实验室强制定位");
        }
    }
}

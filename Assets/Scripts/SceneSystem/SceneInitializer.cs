using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景初始化器 - 监听场景加载事件，自动设置必要系统
/// </summary>
public class SceneInitializer : MonoBehaviour
{
    private static SceneInitializer instance;
    
    [Header("初始化设置")]
    public bool enableAutoSetup = true;
    
    void Awake()
    {
        // 单例模式，确保只有一个初始化器
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (enableAutoSetup)
            {
                // 监听场景加载事件
                SceneManager.sceneLoaded += OnSceneLoaded;
                Debug.Log("场景初始化器已启动，监听场景加载事件");
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
    
    /// <summary>
    /// 场景加载时的回调
    /// </summary>
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"🎬 场景加载检测: {scene.name}, 模式: {mode}");
        
        // 只处理单场景加载（场景切换）
        if (mode == LoadSceneMode.Single)
        {
            SetupSceneIfNeeded(scene.name);
        }
    }
    
    /// <summary>
    /// 根据需要设置场景
    /// </summary>
    void SetupSceneIfNeeded(string sceneName)
    {
        // 检查是否需要自动设置的场景
        if (ShouldAutoSetup(sceneName))
        {
            Debug.Log($"📋 场景 {sceneName} 需要自动设置");

            // 查找现有的SceneAutoSetup
            SceneAutoSetup existingSetup = FindFirstObjectByType<SceneAutoSetup>();

            if (existingSetup == null)
            {
                // 创建自动设置器
                GameObject setupObj = new GameObject("SceneAutoSetup");
                SceneAutoSetup autoSetup = setupObj.AddComponent<SceneAutoSetup>();

                // 配置特定场景的设置
                ConfigureSetupForScene(autoSetup, sceneName);

                Debug.Log($"✅ 为场景 {sceneName} 创建了自动设置器");

                // 延迟运行清理器
                StartCoroutine(DelayedCleanup());
            }
            else
            {
                Debug.Log($"✅ 场景 {sceneName} 已有自动设置器");
            }

            // 为研究室场景初始化移动端UI系统
            if (sceneName == "Laboratory Scene")
            {
                StartCoroutine(InitializeLaboratoryMobileUIHelper());
            }
        }
        else
        {
            Debug.Log($"⏭️ 场景 {sceneName} 不需要自动设置");
        }
    }
    
    /// <summary>
    /// 判断场景是否需要自动设置
    /// </summary>
    bool ShouldAutoSetup(string sceneName)
    {
        // 定义需要自动设置的场景列表
        string[] autoSetupScenes = {
            "Laboratory Scene",
            "MainScene"
        };
        
        foreach (string scene in autoSetupScenes)
        {
            if (sceneName == scene)
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 为特定场景配置设置器
    /// </summary>
    void ConfigureSetupForScene(SceneAutoSetup setup, string sceneName)
    {
        switch (sceneName)
        {
            case "Laboratory Scene":
                // 实验室场景的特殊配置（使用 PlayerPersistentData 的统一位置）
                setup.laboratoryPlayerPosition = PlayerPersistentData.LaboratorySpawnPosition;
                setup.laboratoryPlayerRotation = PlayerPersistentData.LaboratorySpawnRotation;
                setup.setupDelay = 0.3f; // 稍长的延迟，等待场景完全加载
                break;
                
            case "MainScene":
                // 主场景的配置
                setup.defaultPlayerPosition = new Vector3(-29.9230003f, 14.3459997f, -20.9599991f);
                setup.defaultPlayerRotation = new Quaternion(0f, 0.995849609f, 0f, 0.0910143629f);
                setup.setupDelay = 0.2f;
                break;
                
            default:
                // 默认配置
                setup.setupDelay = 0.2f;
                break;
        }
        
        Debug.Log($"🔧 场景 {sceneName} 配置完成");
    }
    
    /// <summary>
    /// 获取或创建场景初始化器实例
    /// </summary>
    public static SceneInitializer GetOrCreate()
    {
        if (instance == null)
        {
            GameObject initializerObj = new GameObject("SceneInitializer");
            instance = initializerObj.AddComponent<SceneInitializer>();
            DontDestroyOnLoad(initializerObj);
            Debug.Log("创建场景初始化器实例");
        }
        
        return instance;
    }
    
    /// <summary>
    /// 延迟清理协程
    /// </summary>
    System.Collections.IEnumerator DelayedCleanup()
    {
        yield return new WaitForSeconds(2f); // 等待自动设置完成
        SceneCleanup.ManualCleanup();
    }
    
    /// <summary>
    /// 手动为当前场景运行设置
    /// </summary>
    public static void SetupCurrentScene()
    {
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        SceneInitializer initializer = GetOrCreate();
        initializer.SetupSceneIfNeeded(currentScene);
    }

    /// <summary>
    /// 初始化研究室移动端UI系统
    /// </summary>
    System.Collections.IEnumerator InitializeLaboratoryMobileUI()
    {
        Debug.Log("🔧 开始初始化研究室移动端UI系统");

        // 等待场景完全加载
        yield return new WaitForSeconds(0.8f);

        bool needsSimplifiedInitialization = false;

        // 使用更安全的方式检查是否已存在LaboratoryMobileUIInitializer
        UnityEngine.Object existingInitializerObj = FindFirstObjectByType(System.Type.GetType("LaboratoryMobileUIInitializer"));
        Component existingInitializer = existingInitializerObj as Component;

        if (existingInitializer == null)
        {
            // 尝试通过反射创建组件
            GameObject initializerObj = new GameObject("LaboratoryMobileUIInitializer");

            // 使用反射添加组件
            System.Type initializerType = System.Type.GetType("LaboratoryMobileUIInitializer");
            if (initializerType != null)
            {
                try
                {
                    Component labUIInitializer = initializerObj.AddComponent(initializerType);

                    // 通过反射设置属性
                    SetComponentProperty(labUIInitializer, "enableMobileUI", true);
                    SetComponentProperty(labUIInitializer, "forceShowOnDesktop", ShouldForceShowMobileUI());
                    SetComponentProperty(labUIInitializer, "enableDebugVisualization", false);

                    Debug.Log("✅ 研究室移动端UI初始化器创建完成（通过反射）");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"❌ 创建LaboratoryMobileUIInitializer失败: {e.Message}");
                    DestroyImmediate(initializerObj);

                    // 不能在catch块中使用yield，设置标志位
                    needsSimplifiedInitialization = true;
                }
            }
            else
            {
                Debug.LogWarning("❌ 无法找到LaboratoryMobileUIInitializer类型，将使用简化初始化");
                DestroyImmediate(initializerObj);
                needsSimplifiedInitialization = true;
            }

            // 在try-catch块外处理简化初始化
            if (needsSimplifiedInitialization)
            {
                yield return StartCoroutine(SimplifiedMobileUIInitialization());
            }
        }
        else
        {
            Debug.Log("✅ 研究室移动端UI初始化器已存在，跳过创建");
        }

        Debug.Log("🎉 研究室移动端UI系统初始化完成");
    }

    /// <summary>
    /// 判断是否应该强制显示移动端UI（桌面测试模式）
    /// </summary>
    bool ShouldForceShowMobileUI()
    {
        // 检查MobileInputManager是否存在且启用了桌面测试模式
        MobileInputManager inputManager = MobileInputManager.Instance;
        if (inputManager != null && inputManager.desktopTestMode)
        {
            return true;
        }

        // 检查是否为移动设备或支持触摸
        bool isMobile = Application.isMobilePlatform;
        bool hasTouch = UnityEngine.InputSystem.Touchscreen.current != null;

        return isMobile || hasTouch;
    }

    /// <summary>
    /// 通过反射设置组件属性
    /// </summary>
    void SetComponentProperty(Component component, string propertyName, object value)
    {
        if (component == null) return;

        try
        {
            System.Type componentType = component.GetType();
            System.Reflection.FieldInfo field = componentType.GetField(propertyName,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                field.SetValue(component, value);
                Debug.Log($"🔧 设置属性 {propertyName} = {value}");
            }
            else
            {
                Debug.LogWarning($"❌ 无法找到属性: {propertyName}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ 设置属性 {propertyName} 失败: {e.Message}");
        }
    }

    /// <summary>
    /// 简化的移动端UI初始化（备用方案）
    /// </summary>
    System.Collections.IEnumerator SimplifiedMobileUIInitialization()
    {
        Debug.Log("🔧 启动简化移动端UI初始化");

        // 确保MobileInputManager存在
        if (MobileInputManager.Instance == null)
        {
            GameObject inputManagerObj = new GameObject("MobileInputManager");
            MobileInputManager inputManager = inputManagerObj.AddComponent<MobileInputManager>();

            // 配置输入管理器
            inputManager.enableTouchInput = true;
            inputManager.enableVirtualControls = true;

            if (ShouldForceShowMobileUI())
            {
                inputManager.EnableDesktopTestMode(true);
            }

            DontDestroyOnLoad(inputManagerObj);
            Debug.Log("✅ MobileInputManager创建完成");
        }

        yield return new WaitForSeconds(0.2f);

        // 确保MobileControlsUI存在
        MobileControlsUI existingControlsUI = FindFirstObjectByType<MobileControlsUI>();
        if (existingControlsUI == null)
        {
            GameObject controlsUIObj = new GameObject("MobileControlsUI");
            MobileControlsUI controlsUI = controlsUIObj.AddComponent<MobileControlsUI>();

            // 配置控制UI
            controlsUI.forceShowOnDesktop = ShouldForceShowMobileUI();
            controlsUI.enableDebugVisualization = false;

            // 研究室场景特定配置：隐藏无人机控制
            yield return new WaitForSeconds(0.5f); // 等待UI初始化
            controlsUI.SetDroneControlsVisible(false);

            Debug.Log("✅ MobileControlsUI创建完成（简化模式）");
        }

        Debug.Log("🎉 简化移动端UI初始化完成");
    }

    /// <summary>
    /// 使用简化管理器初始化研究室移动端UI
    /// </summary>
    System.Collections.IEnumerator InitializeLaboratoryMobileUISimple()
    {
        Debug.Log("🔧 使用简化管理器初始化研究室移动端UI");

        // 等待场景完全加载
        yield return new WaitForSeconds(0.5f);

        // 使用反射检查是否已存在SimpleLaboratoryMobileUIManager
        System.Type managerType = System.Type.GetType("SimpleLaboratoryMobileUIManager");
        UnityEngine.Object existingManagerObj = null;

        if (managerType != null)
        {
            existingManagerObj = FindFirstObjectByType(managerType);
        }

        Component existingManager = existingManagerObj as Component;

        if (existingManager == null && managerType != null)
        {
            // 创建简化的移动端UI管理器
            GameObject managerObj = new GameObject("SimpleLaboratoryMobileUIManager");
            Component uiManager = managerObj.AddComponent(managerType);

            // 通过反射配置管理器
            SetComponentProperty(uiManager, "enableMobileUI", true);
            SetComponentProperty(uiManager, "forceShowOnDesktop", ShouldForceShowMobileUI());
            SetComponentProperty(uiManager, "enableDebugVisualization", false);

            Debug.Log("✅ SimpleLaboratoryMobileUIManager创建完成（通过反射）");
        }
        else if (existingManager != null)
        {
            Debug.Log("✅ SimpleLaboratoryMobileUIManager已存在，跳过创建");
        }
        else
        {
            Debug.LogWarning("❌ 无法找到SimpleLaboratoryMobileUIManager类型，使用简化初始化");
            yield return StartCoroutine(SimplifiedMobileUIInitialization());
        }

        Debug.Log("🎉 简化研究室移动端UI初始化完成");
    }

    /// <summary>
    /// 使用辅助器初始化研究室移动端UI（无类型依赖）
    /// </summary>
    System.Collections.IEnumerator InitializeLaboratoryMobileUIHelper()
    {
        Debug.Log("🔧 使用辅助器初始化研究室移动端UI");

        // 等待场景完全加载
        yield return new WaitForSeconds(0.8f);

        // 尝试通过反射调用辅助器方法
        bool helperCallSuccess = false;

        // 在非yield上下文中处理反射调用
        System.Type helperType = System.Type.GetType("LaboratoryMobileUIHelper");
        if (helperType != null)
        {
            try
            {
                var method = helperType.GetMethod("InitializeLaboratoryMobileUI",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (method != null)
                {
                    method.Invoke(null, null);
                    Debug.Log("✅ 通过反射调用辅助器初始化成功");
                    helperCallSuccess = true;
                }
                else
                {
                    Debug.LogWarning("❌ 无法找到InitializeLaboratoryMobileUI方法");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ 调用辅助器失败: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("❌ 无法找到LaboratoryMobileUIHelper类型");
        }

        // 如果辅助器调用失败，使用简化初始化
        if (!helperCallSuccess)
        {
            Debug.Log("🔧 辅助器调用失败，使用简化初始化作为备用方案");
            yield return StartCoroutine(SimplifiedMobileUIInitialization());
        }

        Debug.Log("🎉 辅助器研究室移动端UI初始化完成");
    }
}

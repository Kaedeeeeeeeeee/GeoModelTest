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
                // 实验室场景的特殊配置
                setup.laboratoryPlayerPosition = new Vector3(1, 0.200000003f, 5);
                setup.laboratoryPlayerRotation = Quaternion.identity;
                setup.setupDelay = 0.3f; // 稍长的延迟，等待场景完全加载
                break;
                
            case "MainScene":
                // 主场景的配置
                setup.defaultPlayerPosition = new Vector3(0, 1, 0);
                setup.defaultPlayerRotation = Quaternion.identity;
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
}
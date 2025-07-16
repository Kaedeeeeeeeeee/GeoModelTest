using UnityEngine;
using System.Collections;

/// <summary>
/// 场景设置测试器 - 验证场景自动设置系统是否正常工作
/// </summary>
public class SceneSetupTester : MonoBehaviour
{
    [Header("测试设置")]
    public bool runTestOnStart = true;
    public float testDelay = 1f;
    
    void Start()
    {
        if (runTestOnStart)
        {
            StartCoroutine(RunSceneSetupTest());
        }
    }
    
    IEnumerator RunSceneSetupTest()
    {
        yield return new WaitForSeconds(testDelay);
        
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        Debug.Log($"=== 场景设置测试开始: {sceneName} ===");
        
        // 测试1：检查Player系统
        TestPlayerSystem();
        
        // 测试2：检查UI系统
        TestUISystem();
        
        // 测试3：检查工具系统
        TestToolSystem();
        
        // 测试4：检查场景管理器
        TestSceneManager();
        
        // 测试5：检查摄像机
        TestCameraSystem();
        
        Debug.Log($"=== 场景设置测试完成: {sceneName} ===");
        Debug.Log("💡 提示：按F8键可以手动运行场景设置");
        Debug.Log("💡 提示：按F9键可以重新测试系统");
        
        // 自毁
        Destroy(this);
    }
    
    void TestPlayerSystem()
    {
        FirstPersonController player = FindFirstObjectByType<FirstPersonController>();
        
        if (player != null)
        {
            Debug.Log("✅ Player系统存在");
            Debug.Log($"   位置: {player.transform.position}");
            Debug.Log($"   旋转: {player.transform.rotation.eulerAngles}");
            
            // 检查Character Controller
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null)
            {
                Debug.Log("✅ CharacterController组件存在");
            }
            else
            {
                Debug.LogWarning("❌ CharacterController组件缺失");
            }
            
            // 检查Lily模型
            if (player.name == "Lily")
            {
                Debug.Log("✅ Lily角色模型存在");
            }
            else
            {
                Debug.LogWarning("❌ Lily角色模型缺失，当前名称: " + player.name);
            }
        }
        else
        {
            Debug.LogError("❌ Player系统不存在");
        }
    }
    
    void TestUISystem()
    {
        InventoryUISystem inventoryUI = FindFirstObjectByType<InventoryUISystem>();
        
        if (inventoryUI != null)
        {
            Debug.Log("✅ UI系统存在");
            
            // 检查Canvas
            Canvas canvas = inventoryUI.GetComponent<Canvas>();
            if (canvas != null)
            {
                Debug.Log($"✅ Canvas存在，渲染模式: {canvas.renderMode}");
            }
            
            // 检查EventSystem
            UnityEngine.EventSystems.EventSystem eventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem != null)
            {
                Debug.Log("✅ EventSystem存在");
            }
            else
            {
                Debug.LogWarning("❌ EventSystem缺失");
            }
        }
        else
        {
            Debug.LogError("❌ UI系统不存在");
        }
    }
    
    void TestToolSystem()
    {
        ToolManager toolManager = FindFirstObjectByType<ToolManager>();
        
        if (toolManager != null)
        {
            Debug.Log("✅ 工具系统存在");
            Debug.Log($"   可用工具数量: {toolManager.availableTools.Length}");
            
            // 检查场景切换器工具
            bool hasSceneSwitcher = false;
            foreach (var tool in toolManager.availableTools)
            {
                if (tool != null && tool is SceneSwitcherTool)
                {
                    hasSceneSwitcher = true;
                    Debug.Log("✅ 场景切换器工具存在");
                    break;
                }
            }
            
            if (!hasSceneSwitcher)
            {
                Debug.LogWarning("⚠️ 场景切换器工具不存在（可能还在初始化中）");
            }
        }
        else
        {
            Debug.LogError("❌ 工具系统不存在");
        }
    }
    
    void TestSceneManager()
    {
        GameSceneManager sceneManager = GameSceneManager.Instance;
        
        if (sceneManager != null)
        {
            Debug.Log("✅ 场景管理器存在");
            Debug.Log($"   当前场景: {sceneManager.GetCurrentSceneName()}");
        }
        else
        {
            Debug.LogError("❌ 场景管理器不存在");
        }
    }
    
    void TestCameraSystem()
    {
        Camera mainCamera = Camera.main;
        
        if (mainCamera != null)
        {
            Debug.Log("✅ 主摄像机存在");
            Debug.Log($"   位置: {mainCamera.transform.position}");
            Debug.Log($"   标签: {mainCamera.tag}");
            
            // 检查Audio Listener
            AudioListener audioListener = mainCamera.GetComponent<AudioListener>();
            if (audioListener != null)
            {
                Debug.Log("✅ AudioListener存在");
            }
            else
            {
                Debug.LogWarning("❌ AudioListener缺失");
            }
        }
        else
        {
            Debug.LogError("❌ 主摄像机不存在");
        }
    }
    
    void Update()
    {
        // F8键：手动运行场景设置
        if (Input.GetKeyDown(KeyCode.F8))
        {
            SceneAutoSetup.QuickSetupScene();
        }
        
        // F9键：重新运行测试
        if (Input.GetKeyDown(KeyCode.F9))
        {
            StartCoroutine(RunSceneSetupTest());
        }
    }
}
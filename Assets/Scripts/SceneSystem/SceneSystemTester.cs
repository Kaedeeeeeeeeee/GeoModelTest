using UnityEngine;
using System.Collections;

/// <summary>
/// 场景系统测试器 - 验证场景切换器是否正确集成到Tab UI中
/// </summary>
public class SceneSystemTester : MonoBehaviour
{
    [Header("测试设置")]
    public bool runTestOnStart = true;
    public float testDelay = 2f;
    
    void Start()
    {
        if (runTestOnStart)
        {
            StartCoroutine(RunSystemTest());
        }
    }
    
    IEnumerator RunSystemTest()
    {
        Debug.Log("=== 场景系统测试开始 ===");
        
        // 等待系统初始化
        yield return new WaitForSeconds(testDelay);
        
        // 测试1：检查ToolManager是否存在
        ToolManager toolManager = FindFirstObjectByType<ToolManager>();
        if (toolManager != null)
        {
            Debug.Log("✅ ToolManager 找到");
            
            // 检查是否有场景切换器工具
            bool hasSceneSwitcher = false;
            foreach (var tool in toolManager.availableTools)
            {
                if (tool != null && tool is SceneSwitcherTool)
                {
                    hasSceneSwitcher = true;
                    Debug.Log($"✅ 场景切换器工具找到: {tool.toolName} (ID: {tool.toolID})");
                    break;
                }
            }
            
            if (!hasSceneSwitcher)
            {
                Debug.LogWarning("❌ ToolManager中没有找到场景切换器工具");
            }
        }
        else
        {
            Debug.LogError("❌ ToolManager 未找到");
        }
        
        // 测试2：检查InventoryUISystem是否存在
        InventoryUISystem inventoryUI = FindFirstObjectByType<InventoryUISystem>();
        if (inventoryUI != null)
        {
            Debug.Log("✅ InventoryUISystem 找到");
            
            // 强制刷新工具列表
            inventoryUI.RefreshTools();
            
            // 检查UI中的工具数量
            Debug.Log($"📊 UI中的工具数量: {inventoryUI.GetAvailableToolsCount()}");
        }
        else
        {
            Debug.LogError("❌ InventoryUISystem 未找到");
        }
        
        // 测试3：检查GameSceneManager是否存在
        GameSceneManager sceneManager = GameSceneManager.Instance;
        if (sceneManager != null)
        {
            Debug.Log("✅ GameSceneManager 实例存在");
        }
        else
        {
            Debug.LogError("❌ GameSceneManager 未能创建实例");
        }
        
        // 测试4：检查场景切换器工具本身
        SceneSwitcherTool sceneSwitcher = FindFirstObjectByType<SceneSwitcherTool>();
        if (sceneSwitcher != null)
        {
            Debug.Log($"✅ SceneSwitcherTool 组件存在: {sceneSwitcher.name}");
            Debug.Log($"   工具ID: {sceneSwitcher.toolID}");
            Debug.Log($"   工具名称: {sceneSwitcher.toolName}");
            
            // 检查预制体
            if (sceneSwitcher.switcherPrefab != null)
            {
                Debug.Log($"   预制体: {sceneSwitcher.switcherPrefab.name}");
                if (sceneSwitcher.switcherPrefab.name.Contains("SceneSwitcher"))
                {
                    Debug.Log("✅ 使用用户的SceneSwitcher预制体");
                }
                else
                {
                    Debug.LogWarning("⚠️ 使用临时预制体，建议运行清理器");
                }
            }
            else
            {
                Debug.LogWarning("❌ 工具没有预制体");
            }
        }
        else
        {
            Debug.LogError("❌ SceneSwitcherTool 组件未找到");
        }
        
        Debug.Log("=== 场景系统测试完成 ===");
        Debug.Log("💡 提示：按Tab键打开工具轮盘，场景切换器应该显示在其中");
        Debug.Log("🔧 提示：按F7键可以手动清理临时模型");
        
        // 自动运行清理器
        SceneSwitcherCleaner.ManualCleanup();
        
        // 自毁
        Destroy(this);
    }
    
    void Update()
    {
        // 按F5键手动运行测试
        if (Input.GetKeyDown(KeyCode.F5))
        {
            StartCoroutine(RunSystemTest());
        }
        
        // 按F6键强制创建场景切换器
        if (Input.GetKeyDown(KeyCode.F6))
        {
            ForceCreateSceneSwitcher();
        }
    }
    
    /// <summary>
    /// 强制创建场景切换器工具
    /// </summary>
    void ForceCreateSceneSwitcher()
    {
        Debug.Log("强制创建场景切换器工具...");
        
        ToolManager toolManager = FindFirstObjectByType<ToolManager>();
        if (toolManager != null)
        {
            // 通过反射调用私有方法
            var method = toolManager.GetType().GetMethod("CreateSceneSwitcherTool", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (method != null)
            {
                method.Invoke(toolManager, null);
                Debug.Log("✅ 强制创建完成");
            }
            else
            {
                Debug.LogError("❌ 未找到CreateSceneSwitcherTool方法");
            }
        }
        else
        {
            Debug.LogError("❌ ToolManager未找到");
        }
    }
}
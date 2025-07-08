using UnityEngine;

public class CollectionSystemDebugger : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            TestCollectionSystem();
        }
    }
    
    void TestCollectionSystem()
    {
        Debug.Log("=== 采集系统测试 ===");
        
        // 检查UI系统
        InventoryUISystem uiSystem = FindFirstObjectByType<InventoryUISystem>();
        if (uiSystem != null)
        {
            Debug.Log("✓ 找到InventoryUISystem");
            if (uiSystem.wheelUI != null)
            {
                Debug.Log("✓ wheelUI已配置");
            }
            else
            {
                Debug.LogError("✗ wheelUI未配置");
            }
        }
        else
        {
            Debug.LogError("✗ 未找到InventoryUISystem");
        }
        
        // 检查工具管理器
        ToolManager toolManager = FindFirstObjectByType<ToolManager>();
        if (toolManager != null)
        {
            Debug.Log($"✓ 找到ToolManager，可用工具数: {toolManager.availableTools?.Length ?? 0}");
            
            if (toolManager.availableTools != null)
            {
                for (int i = 0; i < toolManager.availableTools.Length; i++)
                {
                    var tool = toolManager.availableTools[i];
                    if (tool != null)
                    {
                        Debug.Log($"  工具 {i}: {tool.toolName} (图标: {(tool.toolIcon != null ? "有" : "无")})");
                    }
                }
            }
        }
        else
        {
            Debug.LogError("✗ 未找到ToolManager");
        }
        
        // 检查钻探工具
        BoringTool[] boringTools = FindObjectsByType<BoringTool>(FindObjectsSortMode.None);
        Debug.Log($"找到 {boringTools.Length} 个钻探工具");
        
        foreach (var tool in boringTools)
        {
            Debug.Log($"钻探工具: {tool.name} - {tool.toolName}");
        }
        
        // 检查所有采集工具
        CollectionTool[] allTools = FindObjectsByType<CollectionTool>(FindObjectsSortMode.None);
        Debug.Log($"场景中总共有 {allTools.Length} 个采集工具");
        
        Debug.Log("=== 测试完成 ===");
    }
}
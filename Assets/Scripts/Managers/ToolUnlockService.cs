using UnityEngine;

/// <summary>
/// 运行时按工具ID解锁工具并注册到ToolManager与UI。
/// 前提：对应工具组件已挂在玩家对象上（由GameInitializer添加），但未注册为可用工具。
/// </summary>
public static class ToolUnlockService
{
    /// <summary>
    /// 通过工具ID解锁工具（如 "1000" 简易钻探、"1002" 地质锤、"1100" 无人机、"1101" 钻探车）。
    /// </summary>
    public static bool UnlockToolById(string toolId)
    {
        var toolManager = Object.FindFirstObjectByType<ToolManager>();
        if (toolManager == null)
        {
            Debug.LogError("[ToolUnlockService] 未找到 ToolManager");
            return false;
        }

        // 在同一物体上查找所有 CollectionTool 组件
        var tools = toolManager.GetComponents<CollectionTool>();
        foreach (var tool in tools)
        {
            if (tool != null && tool.toolID == toolId)
            {
                // 若已存在于可用列表则不重复添加
                if (IsToolUnlocked(toolManager, toolId))
                {
                    Debug.Log($"[ToolUnlockService] 工具 {toolId} 已解锁");
                    return true;
                }

                toolManager.AddTool(tool);
                Debug.Log($"[ToolUnlockService] 已解锁工具: {tool.toolID}");
                return true;
            }
        }

        Debug.LogWarning($"[ToolUnlockService] 未在玩家对象上找到工具组件: {toolId}");
        return false;
    }

    /// <summary>
    /// 判断指定工具ID是否已在可用列表中。
    /// </summary>
    public static bool IsToolUnlocked(ToolManager toolManager, string toolId)
    {
        if (toolManager.availableTools == null) return false;
        foreach (var t in toolManager.availableTools)
        {
            if (t != null && t.toolID == toolId) return true;
        }
        return false;
    }
}


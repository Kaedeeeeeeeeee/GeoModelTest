using UnityEngine;

/// <summary>
/// Canvas清理工具 - 清理场景中重复的Canvas对象
/// </summary>
public class CanvasCleanupUtility : MonoBehaviour
{
    [Header("清理选项")]
    public bool cleanupOnStart = true;
    public bool enableDebugLog = true;
    
    void Start()
    {
        if (cleanupOnStart)
        {
            CleanupDuplicateCanvases();
        }
    }
    
    /// <summary>
    /// 清理重复的Canvas对象
    /// </summary>
    [ContextMenu("清理重复Canvas")]
    public void CleanupDuplicateCanvases()
    {
        int cleanedCount = 0;
        
        // 清理样本采集Canvas
        cleanedCount += CleanupCanvasByName("SamplePromptCanvas");
        
        // 清理已放置样本Canvas
        cleanedCount += CleanupCanvasByName("PlacedSamplePromptCanvas");
        
        // 清理钻塔交互Canvas（防止重复）
        cleanedCount += CleanupCanvasByName("DrillTowerInteractionCanvas");
        
        if (enableDebugLog)
        {
            if (cleanedCount > 0)
            {
                Debug.Log($"[CanvasCleanup] 已清理 {cleanedCount} 个重复的Canvas对象");
            }
            else
            {
                Debug.Log("[CanvasCleanup] 没有找到需要清理的重复Canvas");
            }
        }
    }
    
    /// <summary>
    /// 根据名称清理Canvas
    /// </summary>
    private int CleanupCanvasByName(string canvasName)
    {
        Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        int cleanedCount = 0;
        
        foreach (Canvas canvas in allCanvases)
        {
            if (canvas.gameObject.name.Contains(canvasName))
            {
                if (enableDebugLog)
                {
                    Debug.Log($"[CanvasCleanup] 清理重复Canvas: {canvas.gameObject.name}");
                }
                
                DestroyImmediate(canvas.gameObject);
                cleanedCount++;
            }
        }
        
        return cleanedCount;
    }
    
    /// <summary>
    /// 清理特定类型的Canvas
    /// </summary>
    public void CleanupSpecificCanvas(string canvasNamePattern)
    {
        int cleaned = CleanupCanvasByName(canvasNamePattern);
        
        if (enableDebugLog)
        {
            Debug.Log($"[CanvasCleanup] 清理了 {cleaned} 个匹配 '{canvasNamePattern}' 的Canvas");
        }
    }
    
    /// <summary>
    /// 获取场景中Canvas统计信息
    /// </summary>
    [ContextMenu("显示Canvas统计")]
    public void ShowCanvasStatistics()
    {
        Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        
        int sampleCanvases = 0;
        int placedSampleCanvases = 0;
        int drillTowerCanvases = 0;
        int otherCanvases = 0;
        
        foreach (Canvas canvas in allCanvases)
        {
            string name = canvas.gameObject.name;
            
            if (name.Contains("SamplePromptCanvas"))
            {
                sampleCanvases++;
            }
            else if (name.Contains("PlacedSamplePromptCanvas"))
            {
                placedSampleCanvases++;
            }
            else if (name.Contains("DrillTowerInteractionCanvas"))
            {
                drillTowerCanvases++;
            }
            else
            {
                otherCanvases++;
            }
        }
        
        Debug.Log($"[Canvas统计] 总Canvas数: {allCanvases.Length}");
        Debug.Log($"[Canvas统计] 样本采集Canvas: {sampleCanvases}");
        Debug.Log($"[Canvas统计] 已放置样本Canvas: {placedSampleCanvases}");
        Debug.Log($"[Canvas统计] 钻塔交互Canvas: {drillTowerCanvases}");
        Debug.Log($"[Canvas统计] 其他Canvas: {otherCanvases}");
        
        if (sampleCanvases > 1 || placedSampleCanvases > 1 || drillTowerCanvases > 1)
        {
            Debug.LogWarning("[Canvas统计] 检测到重复Canvas，建议运行清理！");
        }
    }
}
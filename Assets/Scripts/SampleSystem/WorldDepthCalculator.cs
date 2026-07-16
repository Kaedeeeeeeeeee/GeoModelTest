using UnityEngine;

/// <summary>
/// 世界坐标深度计算器 - 将相对钻探深度转换为真实世界坐标
/// </summary>
public static class WorldDepthCalculator
{
    /// <summary>
    /// 将相对深度转换为世界坐标深度范围
    /// </summary>
    /// <param name="collectionPosition">采集点世界坐标</param>
    /// <param name="relativeDepthStart">相对起始深度</param>
    /// <param name="relativeDepthEnd">相对结束深度</param>
    /// <returns>世界坐标深度范围</returns>
    public static (float worldDepthStart, float worldDepthEnd) CalculateWorldDepthRange(
        Vector3 collectionPosition, 
        float relativeDepthStart, 
        float relativeDepthEnd)
    {
        // 采集点的Y坐标作为地表高度
        float surfaceElevation = collectionPosition.y;
        
        // 计算世界坐标深度（地表高度 - 相对深度）
        float worldDepthStart = surfaceElevation - relativeDepthStart;
        float worldDepthEnd = surfaceElevation - relativeDepthEnd;
        
        return (worldDepthStart, worldDepthEnd);
    }
    
    /// <summary>
    /// 格式化深度范围显示文本
    /// </summary>
    /// <param name="worldDepthStart">世界坐标起始深度</param>
    /// <param name="worldDepthEnd">世界坐标结束深度</param>
    /// <param name="showRelativeDepth">是否同时显示相对深度</param>
    /// <param name="relativeDepthStart">相对起始深度</param>
    /// <param name="relativeDepthEnd">相对结束深度</param>
    /// <returns>格式化的深度文本</returns>
    public static string FormatDepthRange(
        float worldDepthStart, 
        float worldDepthEnd, 
        bool showRelativeDepth = false,
        float relativeDepthStart = 0f,
        float relativeDepthEnd = 0f)
    {
        string worldDepthText = $"{worldDepthStart:F1}m - {worldDepthEnd:F1}m";
        
        if (showRelativeDepth)
        {
            string relativeDepthText = $"({relativeDepthStart:F1}m - {relativeDepthEnd:F1}m)";
            return $"{worldDepthText} {relativeDepthText}";
        }
        
        return worldDepthText;
    }
    
    /// <summary>
    /// 获取本地化的深度描述
    /// </summary>
    /// <param name="collectionPosition">采集点世界坐标</param>
    /// <param name="relativeDepthStart">相对起始深度</param>
    /// <param name="relativeDepthEnd">相对结束深度</param>
    /// <param name="showRelativeDepth">是否显示相对深度</param>
    /// <returns>本地化的深度描述</returns>
    public static string GetLocalizedDepthDescription(
        Vector3 collectionPosition,
        float relativeDepthStart,
        float relativeDepthEnd,
        bool showRelativeDepth = false)
    {
        // 世界座標は実装内部の値であり、生徒向け画面では意味を持たない。
        // 表示は常に採取地点の地表を0mとした相対深度に統一する。
        return LocalizationManager.Resolve(
            "sample.info.collection_depth",
            "採取した深さ：{0}m～{1}m",
            relativeDepthStart.ToString("F1"),
            relativeDepthEnd.ToString("F1"));
    }
    
    /// <summary>
    /// 验证深度范围的合理性
    /// </summary>
    /// <param name="collectionPosition">采集点坐标</param>
    /// <param name="relativeDepthStart">相对起始深度</param>
    /// <param name="relativeDepthEnd">相对结束深度</param>
    /// <returns>是否为合理的深度范围</returns>
    public static bool ValidateDepthRange(
        Vector3 collectionPosition,
        float relativeDepthStart,
        float relativeDepthEnd)
    {
        // 检查深度范围逻辑性
        if (relativeDepthStart < 0 || relativeDepthEnd < 0)
        {
            Debug.LogWarning($"深度值不能为负数: {relativeDepthStart} - {relativeDepthEnd}");
            return false;
        }
        
        if (relativeDepthStart >= relativeDepthEnd)
        {
            Debug.LogWarning($"起始深度必须小于结束深度: {relativeDepthStart} >= {relativeDepthEnd}");
            return false;
        }
        
        // 检查世界坐标合理性
        var (worldDepthStart, worldDepthEnd) = CalculateWorldDepthRange(
            collectionPosition, relativeDepthStart, relativeDepthEnd);
        
        if (worldDepthEnd < -1000f || worldDepthStart > 10000f)
        {
            Debug.LogWarning($"世界坐标深度超出合理范围: {worldDepthStart} - {worldDepthEnd}");
            return false;
        }
        
        return true;
    }
}

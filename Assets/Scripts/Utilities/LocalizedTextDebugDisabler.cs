using UnityEngine;
using System.Reflection;

/// <summary>
/// 一键禁用所有LocalizedText的调试日志
/// </summary>
public class LocalizedTextDebugDisabler : MonoBehaviour
{
    /// <summary>
    /// 禁用场景中所有LocalizedText的调试日志
    /// </summary>
    [ContextMenu("禁用所有LocalizedText调试日志")]
    public void DisableAllLocalizedTextDebugLogs()
    {
        // 找到场景中所有LocalizedText组件
        var localizedTexts = FindObjectsOfType<LocalizedText>();
        
        int disabledCount = 0;
        
        foreach (var localizedText in localizedTexts)
        {
            // 使用反射访问私有字段enableDebugLog
            FieldInfo enableDebugLogField = typeof(LocalizedText).GetField("enableDebugLog", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (enableDebugLogField != null)
            {
                bool currentValue = (bool)enableDebugLogField.GetValue(localizedText);
                if (currentValue)
                {
                    enableDebugLogField.SetValue(localizedText, false);
                    disabledCount++;
                    Debug.Log($"已禁用 {localizedText.gameObject.name} 的调试日志");
                }
            }
        }
        
        Debug.Log($"=== 调试日志禁用完成 ===");
        Debug.Log($"总共处理了 {localizedTexts.Length} 个LocalizedText组件");
        Debug.Log($"禁用了 {disabledCount} 个组件的调试日志");
        
        if (disabledCount > 0)
        {
            Debug.Log("✅ 调试日志已清理，Console应该安静多了！");
        }
        else
        {
            Debug.Log("ℹ️ 所有组件的调试日志都已经是禁用状态");
        }
    }
    
    /// <summary>
    /// 显示当前所有LocalizedText的调试状态
    /// </summary>
    [ContextMenu("显示LocalizedText调试状态")]
    public void ShowLocalizedTextDebugStatus()
    {
        var localizedTexts = FindObjectsOfType<LocalizedText>();
        
        Debug.Log($"=== LocalizedText调试状态报告 ===");
        Debug.Log($"总共找到 {localizedTexts.Length} 个LocalizedText组件:");
        
        int enabledCount = 0;
        int disabledCount = 0;
        
        foreach (var localizedText in localizedTexts)
        {
            FieldInfo enableDebugLogField = typeof(LocalizedText).GetField("enableDebugLog", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (enableDebugLogField != null)
            {
                bool isEnabled = (bool)enableDebugLogField.GetValue(localizedText);
                string status = isEnabled ? "🔊 启用" : "🔇 禁用";
                Debug.Log($"  - {localizedText.gameObject.name}: {status}");
                
                if (isEnabled) enabledCount++;
                else disabledCount++;
            }
        }
        
        Debug.Log($"启用调试: {enabledCount} 个");
        Debug.Log($"禁用调试: {disabledCount} 个");
    }
}
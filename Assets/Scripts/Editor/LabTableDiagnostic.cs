using UnityEngine;
using UnityEditor;
using System.Text;

/// <summary>
/// 实验台诊断工具 - 帮助查找实验台消失的原因
/// </summary>
public class LabTableDiagnostic : EditorWindow
{
    [MenuItem("Tools/实验台诊断工具")]
    static void ShowWindow()
    {
        GetWindow<LabTableDiagnostic>("实验台诊断");
    }

    void OnGUI()
    {
        GUILayout.Label("实验台诊断工具", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (GUILayout.Button("🔍 检查实验台状态"))
        {
            CheckLabTableStatus();
        }

        EditorGUILayout.Space();
        
        if (GUILayout.Button("🔧 检查所有SampleCollector"))
        {
            CheckAllSampleCollectors();
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("🧹 清理错误的SampleCollector"))
        {
            CleanupWrongSampleCollectors();
        }
    }

    /// <summary>
    /// 检查实验台状态
    /// </summary>
    void CheckLabTableStatus()
    {
        StringBuilder report = new StringBuilder();
        report.AppendLine("=== 实验台状态诊断报告 ===\n");

        // 查找所有可能的实验台对象
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        
        int labTableCount = 0;
        foreach (GameObject obj in allObjects)
        {
            string objName = obj.name.ToLower();
            if (objName.Contains("laboratory") || objName.Contains("cutting") || objName.Contains("station"))
            {
                labTableCount++;
                report.AppendLine($"🔵 发现实验台对象: {obj.name}");
                report.AppendLine($"   位置: {obj.transform.position}");
                report.AppendLine($"   激活状态: {obj.activeInHierarchy}");
                report.AppendLine($"   父对象: {(obj.transform.parent?.name ?? "无")}");
                
                // 检查组件
                var components = obj.GetComponents<Component>();
                report.AppendLine($"   组件数量: {components.Length}");
                
                bool hasSampleCollector = false;
                foreach (var comp in components)
                {
                    if (comp == null) continue;
                    
                    string compType = comp.GetType().Name;
                    report.AppendLine($"     - {compType}");
                    
                    if (compType == "SampleCollector")
                    {
                        hasSampleCollector = true;
                        report.AppendLine($"       ⚠️ 警告：实验台有SampleCollector组件！");
                    }
                }
                
                if (hasSampleCollector)
                {
                    report.AppendLine($"   ❌ 问题发现：实验台被错误添加了SampleCollector组件！");
                }
                else
                {
                    report.AppendLine($"   ✅ 正常：实验台没有SampleCollector组件");
                }
                
                report.AppendLine();
            }
        }
        
        if (labTableCount == 0)
        {
            report.AppendLine("❌ 没有找到任何实验台对象！");
        }
        else
        {
            report.AppendLine($"📊 总共找到 {labTableCount} 个实验台相关对象");
        }

        Debug.Log(report.ToString());
    }

    /// <summary>
    /// 检查所有SampleCollector
    /// </summary>
    void CheckAllSampleCollectors()
    {
        StringBuilder report = new StringBuilder();
        report.AppendLine("=== SampleCollector 诊断报告 ===\n");

        SampleCollector[] collectors = GameObject.FindObjectsOfType<SampleCollector>();
        
        report.AppendLine($"📊 场景中共找到 {collectors.Length} 个SampleCollector组件\n");

        int suspiciousCount = 0;
        foreach (var collector in collectors)
        {
            string objName = collector.gameObject.name;
            report.AppendLine($"🔵 对象: {objName}");
            report.AppendLine($"   位置: {collector.transform.position}");
            
            // 检查是否是可疑的实验台对象
            string lowerName = objName.ToLower();
            bool isSuspicious = lowerName.Contains("laboratory") || 
                               lowerName.Contains("cutting") || 
                               lowerName.Contains("station") ||
                               lowerName.Contains("table") ||
                               lowerName.Contains("desk");
                               
            if (isSuspicious)
            {
                suspiciousCount++;
                report.AppendLine($"   ⚠️ 可疑：这个对象名称像是实验台，但有SampleCollector组件！");
            }
            else
            {
                report.AppendLine($"   ✅ 正常：看起来是合法的样本对象");
            }
            
            report.AppendLine();
        }
        
        if (suspiciousCount > 0)
        {
            report.AppendLine($"❌ 发现 {suspiciousCount} 个可疑的SampleCollector！");
            report.AppendLine("建议使用'清理错误的SampleCollector'功能");
        }
        else
        {
            report.AppendLine("✅ 所有SampleCollector看起来都正常");
        }

        Debug.Log(report.ToString());
    }

    /// <summary>
    /// 清理错误的SampleCollector
    /// </summary>
    void CleanupWrongSampleCollectors()
    {
        if (!EditorUtility.DisplayDialog("确认清理", 
            "这将移除所有看起来像实验台对象上的SampleCollector组件。\n\n确定要继续吗？", 
            "确定", "取消"))
        {
            return;
        }

        int removedCount = 0;
        SampleCollector[] collectors = GameObject.FindObjectsOfType<SampleCollector>();
        
        foreach (var collector in collectors)
        {
            string objName = collector.gameObject.name.ToLower();
            bool shouldRemove = objName.Contains("laboratory") || 
                               objName.Contains("cutting") || 
                               objName.Contains("station") ||
                               objName.Contains("table") ||
                               objName.Contains("desk");
                               
            if (shouldRemove)
            {
                Debug.Log($"🧹 移除错误的SampleCollector: {collector.gameObject.name}");
                DestroyImmediate(collector);
                removedCount++;
            }
        }
        
        if (removedCount > 0)
        {
            Debug.Log($"✅ 清理完成！移除了 {removedCount} 个错误的SampleCollector组件");
            EditorUtility.DisplayDialog("清理完成", 
                $"成功移除了 {removedCount} 个错误的SampleCollector组件", "确定");
        }
        else
        {
            Debug.Log("✅ 没有发现需要清理的错误SampleCollector组件");
            EditorUtility.DisplayDialog("清理完成", 
                "没有发现需要清理的错误SampleCollector组件", "确定");
        }
    }
}
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

/// <summary>
/// Debug.Log分析器 - 分析项目中所有Debug.Log语句的分布
/// 提供详细的统计报告和一键优化功能
/// </summary>
public class DebugLogAnalyzer : EditorWindow
{
    private Vector2 scrollPosition;
    private List<DebugLogInfo> debugLogStats = new List<DebugLogInfo>();
    private bool showDetailedReport = true;
    private bool groupBySystem = true;
    
    private struct DebugLogInfo
    {
        public string filePath;
        public string fileName;
        public string systemName;
        public int logCount;
        public List<string> logLines;
    }
    
    [MenuItem("Tools/调试日志分析器")]
    public static void ShowWindow()
    {
        var window = GetWindow<DebugLogAnalyzer>("Debug日志分析器");
        window.minSize = new Vector2(800, 600);
        window.AnalyzeDebugLogs();
    }
    
    void OnGUI()
    {
        EditorGUILayout.BeginVertical();
        
        // 标题
        GUILayout.Label("Unity项目Debug.Log分析报告", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        // 控制选项
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🔍 重新分析", GUILayout.Width(100)))
        {
            AnalyzeDebugLogs();
        }
        if (GUILayout.Button("🔇 禁用所有Debug", GUILayout.Width(120)))
        {
            DisableAllDebugLogs();
        }
        if (GUILayout.Button("📊 导出报告", GUILayout.Width(100)))
        {
            ExportReport();
        }
        GUILayout.FlexibleSpace();
        showDetailedReport = GUILayout.Toggle(showDetailedReport, "详细报告");
        groupBySystem = GUILayout.Toggle(groupBySystem, "按系统分组");
        EditorGUILayout.EndHorizontal();
        
        GUILayout.Space(10);
        
        // 统计概览
        if (debugLogStats.Count > 0)
        {
            int totalLogs = debugLogStats.Sum(x => x.logCount);
            int totalFiles = debugLogStats.Count;
            
            EditorGUILayout.HelpBox($"总计: {totalLogs} 个Debug.Log语句分布在 {totalFiles} 个文件中", MessageType.Info);
            
            // 前10个最多Debug.Log的文件
            var topFiles = debugLogStats.OrderByDescending(x => x.logCount).Take(10).ToList();
            
            EditorGUILayout.LabelField("🏆 Debug.Log输出量排行榜 (前10名):", EditorStyles.boldLabel);
            
            foreach (var fileInfo in topFiles)
            {
                EditorGUILayout.BeginHorizontal();
                
                // 文件名（可点击跳转）
                if (GUILayout.Button(fileInfo.fileName, EditorStyles.linkLabel, GUILayout.Width(300)))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<MonoScript>(fileInfo.filePath);
                    if (asset != null)
                        AssetDatabase.OpenAsset(asset);
                }
                
                // 系统名
                GUILayout.Label($"[{fileInfo.systemName}]", GUILayout.Width(150));
                
                // Debug数量
                GUILayout.Label($"{fileInfo.logCount} 个", GUILayout.Width(60));
                
                // 问题级别颜色
                Color originalColor = GUI.color;
                if (fileInfo.logCount > 50)
                    GUI.color = Color.red;
                else if (fileInfo.logCount > 20)
                    GUI.color = Color.yellow;
                else if (fileInfo.logCount > 10)
                    GUI.color = new Color(1f, 0.5f, 0f); // 橙色
                
                string level = fileInfo.logCount > 50 ? "🔴 严重" : 
                              fileInfo.logCount > 20 ? "🟡 高" : 
                              fileInfo.logCount > 10 ? "🟠 中" : "🟢 低";
                GUILayout.Label(level, GUILayout.Width(50));
                GUI.color = originalColor;
                
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
        }
        
        GUILayout.Space(10);
        
        // 系统统计
        if (groupBySystem && debugLogStats.Count > 0)
        {
            EditorGUILayout.LabelField("📊 按系统分组统计:", EditorStyles.boldLabel);
            
            var systemGroups = debugLogStats
                .GroupBy(x => x.systemName)
                .Select(g => new { System = g.Key, Count = g.Sum(x => x.logCount), Files = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();
            
            foreach (var group in systemGroups)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"📁 {group.System}", GUILayout.Width(200));
                GUILayout.Label($"{group.Count} 个日志", GUILayout.Width(80));
                GUILayout.Label($"({group.Files} 个文件)", GUILayout.Width(80));
                
                // 系统级别建议
                string suggestion = "";
                if (group.Count > 200)
                    suggestion = "🚨 急需优化";
                else if (group.Count > 100)
                    suggestion = "⚠️ 建议优化";
                else if (group.Count > 50)
                    suggestion = "💡 可以优化";
                else
                    suggestion = "✅ 良好";
                
                GUILayout.Label(suggestion);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
        }
        
        GUILayout.Space(10);
        
        // 详细文件列表
        if (showDetailedReport)
        {
            EditorGUILayout.LabelField("📋 详细文件列表:", EditorStyles.boldLabel);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            var sortedStats = groupBySystem ? 
                debugLogStats.OrderBy(x => x.systemName).ThenByDescending(x => x.logCount) :
                debugLogStats.OrderByDescending(x => x.logCount);
            
            foreach (var stat in sortedStats)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(stat.fileName, EditorStyles.linkLabel))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<MonoScript>(stat.filePath);
                    if (asset != null)
                        AssetDatabase.OpenAsset(asset);
                }
                GUILayout.FlexibleSpace();
                GUILayout.Label($"[{stat.systemName}]", GUILayout.Width(150));
                GUILayout.Label($"{stat.logCount} 个Debug.Log", GUILayout.Width(120));
                EditorGUILayout.EndHorizontal();
                
                if (stat.logCount > 0 && stat.logLines != null && stat.logLines.Count > 0)
                {
                    EditorGUILayout.LabelField("示例日志语句:", EditorStyles.miniBoldLabel);
                    int showCount = Mathf.Min(3, stat.logLines.Count);
                    for (int i = 0; i < showCount; i++)
                    {
                        EditorGUILayout.LabelField($"  • {stat.logLines[i]}", EditorStyles.miniLabel);
                    }
                    if (stat.logLines.Count > 3)
                    {
                        EditorGUILayout.LabelField($"  ... 还有 {stat.logLines.Count - 3} 个", EditorStyles.miniLabel);
                    }
                }
                
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        EditorGUILayout.EndVertical();
    }
    
    /// <summary>
    /// 分析项目中的Debug.Log语句
    /// </summary>
    private void AnalyzeDebugLogs()
    {
        debugLogStats.Clear();
        
        string[] scriptFiles = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);
        Regex debugLogRegex = new Regex(@"Debug\.Log\w*\s*\(", RegexOptions.IgnoreCase);
        
        EditorUtility.DisplayProgressBar("分析Debug.Log", "正在扫描脚本文件...", 0f);
        
        for (int i = 0; i < scriptFiles.Length; i++)
        {
            string filePath = scriptFiles[i];
            string relativePath = "Assets" + filePath.Substring(Application.dataPath.Length);
            
            EditorUtility.DisplayProgressBar("分析Debug.Log", 
                $"正在分析: {Path.GetFileName(filePath)}", 
                (float)i / scriptFiles.Length);
            
            try
            {
                string content = File.ReadAllText(filePath);
                var matches = debugLogRegex.Matches(content);
                
                if (matches.Count > 0)
                {
                    var logInfo = new DebugLogInfo
                    {
                        filePath = relativePath,
                        fileName = Path.GetFileName(filePath),
                        systemName = DetermineSystemName(relativePath),
                        logCount = matches.Count,
                        logLines = ExtractDebugLogLines(content, matches)
                    };
                    
                    debugLogStats.Add(logInfo);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"无法读取文件 {filePath}: {e.Message}");
            }
        }
        
        EditorUtility.ClearProgressBar();
        
        Debug.Log($"✅ Debug.Log分析完成！共发现 {debugLogStats.Sum(x => x.logCount)} 个Debug.Log语句");
    }
    
    /// <summary>
    /// 根据文件路径确定系统名称
    /// </summary>
    private string DetermineSystemName(string filePath)
    {
        if (filePath.Contains("Encyclopedia"))
            return "图鉴系统";
        else if (filePath.Contains("Localization"))
            return "多语言系统";
        else if (filePath.Contains("WarehouseSystem"))
            return "仓库系统";
        else if (filePath.Contains("SampleSystem"))
            return "样本系统";
        else if (filePath.Contains("DrillTowerSystem") || filePath.Contains("DrillTower"))
            return "钻塔系统";
        else if (filePath.Contains("SceneSystem"))
            return "场景系统";
        else if (filePath.Contains("Managers"))
            return "管理器系统";
        else if (filePath.Contains("GeologySystem"))
            return "地质系统";
        else if (filePath.Contains("Tools"))
            return "工具系统";
        else if (filePath.Contains("VehicleSystem"))
            return "载具系统";
        else if (filePath.Contains("Debug") || filePath.Contains("Utilities"))
            return "调试/工具";
        else if (filePath.Contains("Editor"))
            return "编辑器工具";
        else if (filePath.Contains("MineralSystem"))
            return "矿物系统";
        else
            return "核心系统";
    }
    
    /// <summary>
    /// 提取Debug.Log语句的内容
    /// </summary>
    private List<string> ExtractDebugLogLines(string content, MatchCollection matches)
    {
        List<string> logLines = new List<string>();
        string[] lines = content.Split('\n');
        
        foreach (Match match in matches.Cast<Match>().Take(5)) // 只取前5个示例
        {
            int charIndex = match.Index;
            int lineNumber = content.Substring(0, charIndex).Count(c => c == '\n');
            
            if (lineNumber < lines.Length)
            {
                string line = lines[lineNumber].Trim();
                if (line.Length > 100)
                    line = line.Substring(0, 97) + "...";
                logLines.Add(line);
            }
        }
        
        return logLines;
    }
    
    /// <summary>
    /// 禁用所有Debug.Log（通过GlobalDebugController）
    /// </summary>
    private void DisableAllDebugLogs()
    {
        var controller = FindObjectOfType<GlobalDebugController>();
        if (controller != null)
        {
            controller.DisableAllSystemDebugLogs();
            EditorUtility.DisplayDialog("调试控制", "已通过GlobalDebugController禁用所有系统调试输出", "确定");
        }
        else
        {
            EditorUtility.DisplayDialog("调试控制", 
                "未找到GlobalDebugController组件。\n请在场景中添加GlobalDebugController脚本来管理调试输出。", 
                "确定");
        }
    }
    
    /// <summary>
    /// 导出详细报告
    /// </summary>
    private void ExportReport()
    {
        string reportPath = EditorUtility.SaveFilePanel("导出Debug.Log分析报告", "", "DebugLogReport", "txt");
        if (!string.IsNullOrEmpty(reportPath))
        {
            using (StreamWriter writer = new StreamWriter(reportPath))
            {
                writer.WriteLine("Unity项目Debug.Log分析报告");
                writer.WriteLine("生成时间: " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                writer.WriteLine("".PadRight(60, '='));
                writer.WriteLine();
                
                // 概览统计
                int totalLogs = debugLogStats.Sum(x => x.logCount);
                int totalFiles = debugLogStats.Count;
                writer.WriteLine($"统计概览:");
                writer.WriteLine($"  总Debug.Log数量: {totalLogs}");
                writer.WriteLine($"  涉及文件数量: {totalFiles}");
                writer.WriteLine($"  平均每文件: {(double)totalLogs / totalFiles:F1} 个");
                writer.WriteLine();
                
                // 系统分组统计
                writer.WriteLine("按系统分组统计:");
                var systemGroups = debugLogStats
                    .GroupBy(x => x.systemName)
                    .Select(g => new { System = g.Key, Count = g.Sum(x => x.logCount), Files = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToList();
                
                foreach (var group in systemGroups)
                {
                    writer.WriteLine($"  {group.System}: {group.Count} 个日志 ({group.Files} 个文件)");
                }
                writer.WriteLine();
                
                // 详细文件列表
                writer.WriteLine("详细文件列表:");
                foreach (var stat in debugLogStats.OrderByDescending(x => x.logCount))
                {
                    writer.WriteLine($"📁 {stat.fileName} [{stat.systemName}] - {stat.logCount} 个Debug.Log");
                    writer.WriteLine($"   路径: {stat.filePath}");
                    if (stat.logLines != null && stat.logLines.Count > 0)
                    {
                        writer.WriteLine("   示例日志:");
                        foreach (var line in stat.logLines.Take(3))
                        {
                            writer.WriteLine($"     • {line}");
                        }
                    }
                    writer.WriteLine();
                }
            }
            
            EditorUtility.RevealInFinder(reportPath);
            Debug.Log($"✅ 报告已导出到: {reportPath}");
        }
    }
}
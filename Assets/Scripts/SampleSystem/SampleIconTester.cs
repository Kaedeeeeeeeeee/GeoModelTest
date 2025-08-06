using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 样本图标测试器 - 用于测试不同类型样本的图标显示效果
/// </summary>
public class SampleIconTester : MonoBehaviour
{
    [Header("测试设置")]
    public bool createTestSamplesOnStart = false;
    public int testSampleCount = 6;
    
    [Header("测试样本配置")]
    public Color[] testColors = {
        Color.red,
        Color.green,
        Color.blue,
        Color.yellow,
        new Color(0.8f, 0.4f, 0.2f), // 棕色
        new Color(0.5f, 0.3f, 0.7f)  // 紫色
    };
    
    // 测试样本列表
    private List<SampleItem> testSamples = new List<SampleItem>();
    
    void Start()
    {
        if (createTestSamplesOnStart)
        {
            CreateTestSamples();
        }
    }
    
    /// <summary>
    /// 创建测试样本
    /// </summary>
    [ContextMenu("创建测试样本")]
    public void CreateTestSamples()
    {
        Debug.Log("开始创建测试样本...");
        
        // 清理之前的测试样本
        testSamples.Clear();
        
        // 创建不同类型的测试样本
        for (int i = 0; i < testSampleCount && i < testColors.Length; i++)
        {
            // 交替创建钻探样本和薄片样本
            bool isDrillSample = i % 2 == 0;
            string toolID = isDrillSample ? "1000" : "1002"; // 钻探工具 或 地质锤
            string toolName = isDrillSample ? "钻探工具" : "地质锤";
            string shapeType = isDrillSample ? "圆柱形" : "薄片形";
            
            SampleItem testSample = CreateTestSample(i, toolID, toolName, shapeType, testColors[i]);
            testSamples.Add(testSample);
            
            Debug.Log($"创建测试样本 {i + 1}: {testSample.displayName} ({shapeType}, {testColors[i]})");
        }
        
        // 添加测试样本到背包系统
        AddTestSamplesToInventory();
        
        Debug.Log($"测试样本创建完成！共创建 {testSamples.Count} 个样本");
    }
    
    /// <summary>
    /// 创建单个测试样本
    /// </summary>
    SampleItem CreateTestSample(int index, string toolID, string toolName, string shapeType, Color color)
    {
        var testSample = new SampleItem
        {
            sampleID = $"TEST_ICON_{index:000}_{System.DateTime.Now:HHmmss}",
            displayName = $"测试样本_{index + 1}_{shapeType}",
            description = $"用于测试图标显示的{shapeType}样本（{toolName}采集）",
            collectionTime = System.DateTime.Now,
            originalCollectionPosition = new Vector3(index * 2f, 0f, 0f),
            sourceToolID = toolID,
            totalDepth = toolID == "1002" ? 0.1f : 2.0f, // 薄片样本较薄
            sampleRadius = 0.1f,
            layerCount = 1,
            geologicalLayers = new List<SampleItem.LayerInfo>
            {
                new SampleItem.LayerInfo
                {
                    layerName = $"测试地质层_{index + 1}",
                    layerColor = color,
                    thickness = toolID == "1002" ? 0.1f : 2.0f,
                    depthStart = 0f,
                    depthEnd = toolID == "1002" ? 0.1f : 2.0f,
                    materialName = $"TestMaterial_{index}",
                    layerDescription = $"测试颜色: {ColorUtility.ToHtmlStringRGB(color)}"
                }
            },
            currentLocation = SampleLocation.InInventory
        };
        
        // 为样本生成图标
        if (SampleIconGenerator.Instance != null)
        {
            testSample.previewIcon = SampleIconGenerator.Instance.GenerateIconForSample(testSample);
            if (testSample.previewIcon != null)
            {
                Debug.Log($"为测试样本 {testSample.displayName} 生成了动态图标");
            }
        }
        
        return testSample;
    }
    
    /// <summary>
    /// 将测试样本添加到背包系统
    /// </summary>
    void AddTestSamplesToInventory()
    {
        if (SampleInventory.Instance == null)
        {
            Debug.LogWarning("SampleInventory 实例不存在，无法添加测试样本");
            return;
        }
        
        int successCount = 0;
        foreach (var sample in testSamples)
        {
            if (SampleInventory.Instance.TryAddSample(sample))
            {
                successCount++;
            }
        }
        
        Debug.Log($"成功添加 {successCount}/{testSamples.Count} 个测试样本到背包");
        
        if (successCount > 0)
        {
            Debug.Log("💡 提示: 按 I 键打开背包查看图标效果");
        }
    }
    
    /// <summary>
    /// 测试图标生成性能
    /// </summary>
    [ContextMenu("测试图标生成性能")]
    public void TestIconGenerationPerformance()
    {
        if (SampleIconGenerator.Instance == null)
        {
            Debug.LogError("SampleIconGenerator 实例不存在");
            return;
        }
        
        Debug.Log("开始图标生成性能测试...");
        
        float startTime = Time.realtimeSinceStartup;
        int testCount = 100;
        int successCount = 0;
        
        for (int i = 0; i < testCount; i++)
        {
            // 创建临时测试样本
            var tempSample = new SampleItem
            {
                sampleID = $"PERF_TEST_{i}",
                sourceToolID = i % 2 == 0 ? "1000" : "1002",
                geologicalLayers = new List<SampleItem.LayerInfo>
                {
                    new SampleItem.LayerInfo
                    {
                        layerColor = new Color(
                            Random.Range(0.2f, 1f),
                            Random.Range(0.2f, 1f),
                            Random.Range(0.2f, 1f)
                        )
                    }
                }
            };
            
            // 生成图标
            Sprite icon = SampleIconGenerator.Instance.GenerateIconForSample(tempSample);
            if (icon != null)
            {
                successCount++;
                // 立即清理测试图标
                if (icon.texture != null)
                {
                    DestroyImmediate(icon.texture);
                }
                DestroyImmediate(icon);
            }
        }
        
        float endTime = Time.realtimeSinceStartup;
        float totalTime = endTime - startTime;
        float averageTime = totalTime / testCount * 1000f; // 转换为毫秒
        
        Debug.Log($"📊 图标生成性能测试结果:");
        Debug.Log($"   总测试数量: {testCount}");
        Debug.Log($"   成功生成: {successCount}");
        Debug.Log($"   总耗时: {totalTime:F3} 秒");
        Debug.Log($"   平均耗时: {averageTime:F2} 毫秒/个");
        Debug.Log($"   缓存统计: {SampleIconGenerator.Instance.GetCacheStats()}");
    }
    
    /// <summary>
    /// 测试所有样本形状和颜色组合
    /// </summary>
    [ContextMenu("测试所有形状颜色组合")]
    public void TestAllShapeColorCombinations()
    {
        if (SampleIconGenerator.Instance == null)
        {
            Debug.LogError("SampleIconGenerator 实例不存在");
            return;
        }
        
        Debug.Log("测试所有形状和颜色组合...");
        
        string[] toolIDs = { "1000", "1002" }; // 钻探工具, 地质锤
        string[] shapeNames = { "圆柱形", "薄片形" };
        
        int totalCombinations = 0;
        int successfulGenerations = 0;
        
        for (int shapeIndex = 0; shapeIndex < toolIDs.Length; shapeIndex++)
        {
            for (int colorIndex = 0; colorIndex < testColors.Length; colorIndex++)
            {
                var testSample = new SampleItem
                {
                    sampleID = $"COMBO_{shapeIndex}_{colorIndex}",
                    sourceToolID = toolIDs[shapeIndex],
                    geologicalLayers = new List<SampleItem.LayerInfo>
                    {
                        new SampleItem.LayerInfo
                        {
                            layerColor = testColors[colorIndex]
                        }
                    }
                };
                
                Sprite icon = SampleIconGenerator.Instance.GenerateIconForSample(testSample);
                totalCombinations++;
                
                if (icon != null)
                {
                    successfulGenerations++;
                    Debug.Log($"✅ {shapeNames[shapeIndex]} + {ColorUtility.ToHtmlStringRGB(testColors[colorIndex])} = 成功");
                }
                else
                {
                    Debug.LogWarning($"❌ {shapeNames[shapeIndex]} + {ColorUtility.ToHtmlStringRGB(testColors[colorIndex])} = 失败");
                }
            }
        }
        
        Debug.Log($"🎯 组合测试完成: {successfulGenerations}/{totalCombinations} 成功");
        Debug.Log($"📦 {SampleIconGenerator.Instance.GetCacheStats()}");
    }
    
    /// <summary>
    /// 刷新现有样本图标
    /// </summary>
    [ContextMenu("刷新现有样本图标")]
    public void RefreshExistingSampleIcons()
    {
        if (SampleInventory.Instance == null)
        {
            Debug.LogWarning("SampleInventory 实例不存在");
            return;
        }
        
        if (SampleIconGenerator.Instance == null)
        {
            Debug.LogWarning("SampleIconGenerator 实例不存在");
            return;
        }
        
        var allSamples = SampleInventory.Instance.GetAllSamples();
        int refreshedCount = 0;
        
        foreach (var sample in allSamples)
        {
            // 刷新样本图标
            Sprite newIcon = SampleIconGenerator.Instance.RefreshSampleIcon(sample);
            if (newIcon != null)
            {
                sample.previewIcon = newIcon;
                refreshedCount++;
                Debug.Log($"已刷新样本图标: {sample.displayName}");
            }
        }
        
        Debug.Log($"✅ 已刷新 {refreshedCount} 个样本的图标");
        Debug.Log("💡 提示: 重新打开背包 (I键) 查看新的图标效果");
        
        // 触发背包界面刷新
        if (SampleInventory.Instance != null)
        {
            SampleInventory.Instance.OnInventoryChanged?.Invoke();
        }
    }
    
    /// <summary>
    /// 清理测试样本
    /// </summary>
    [ContextMenu("清理测试样本")]
    public void ClearTestSamples()
    {
        if (SampleInventory.Instance == null)
        {
            Debug.LogWarning("SampleInventory 实例不存在");
            return;
        }
        
        int removedCount = 0;
        List<SampleItem> samplesToRemove = new List<SampleItem>();
        
        // 查找所有测试样本
        var allSamples = SampleInventory.Instance.GetAllSamples();
        foreach (var sample in allSamples)
        {
            if (sample.sampleID.StartsWith("TEST_ICON_") || sample.displayName.StartsWith("测试样本_"))
            {
                samplesToRemove.Add(sample);
            }
        }
        
        // 移除测试样本
        foreach (var sample in samplesToRemove)
        {
            if (SampleInventory.Instance.RemoveSample(sample))
            {
                removedCount++;
            }
        }
        
        testSamples.Clear();
        
        Debug.Log($"已清理 {removedCount} 个测试样本");
        
        // 清理图标缓存
        if (SampleIconGenerator.Instance != null)
        {
            SampleIconGenerator.Instance.ClearIconCache();
        }
    }
    
    /// <summary>
    /// 诊断现有样本颜色问题
    /// </summary>
    [ContextMenu("诊断样本颜色问题")]
    public void DiagnoseSampleColorIssues()
    {
        if (SampleInventory.Instance == null)
        {
            Debug.LogError("❌ SampleInventory 实例不存在");
            return;
        }
        
        var allSamples = SampleInventory.Instance.GetAllSamples();
        if (allSamples.Count == 0)
        {
            Debug.LogWarning("⚠️ 背包中没有样本");
            return;
        }
        
        Debug.Log("🔬 开始诊断样本颜色问题...");
        Debug.Log($"📊 总样本数量: {allSamples.Count}");
        Debug.Log("=".PadRight(50, '='));
        
        Dictionary<string, int> toolCounts = new Dictionary<string, int>();
        Dictionary<string, int> colorCounts = new Dictionary<string, int>();
        
        for (int i = 0; i < allSamples.Count; i++)
        {
            var sample = allSamples[i];
            Debug.Log($"\n📦 样本 {i + 1}: {sample.displayName}");
            Debug.Log($"   🔧 工具ID: {sample.sourceToolID}");
            Debug.Log($"   📏 总深度: {sample.totalDepth}m");
            Debug.Log($"   🗂️ 地质层数量: {sample.geologicalLayers?.Count ?? 0}");
            
            // 统计工具类型
            if (toolCounts.ContainsKey(sample.sourceToolID))
                toolCounts[sample.sourceToolID]++;
            else
                toolCounts[sample.sourceToolID] = 1;
            
            // 分析地质层颜色
            if (sample.geologicalLayers != null && sample.geologicalLayers.Count > 0)
            {
                // 找到最上层
                var topLayer = sample.geologicalLayers[0];
                float minDepth = topLayer.depthStart;
                
                for (int j = 0; j < sample.geologicalLayers.Count; j++)
                {
                    var layer = sample.geologicalLayers[j];
                    string colorHtml = ColorUtility.ToHtmlStringRGBA(layer.layerColor);
                    
                    bool isTopLayer = layer.depthStart <= minDepth;
                    if (isTopLayer)
                    {
                        topLayer = layer;
                        minDepth = layer.depthStart;
                    }
                    
                    string layerStatus = isTopLayer ? "⭐ [最上层]" : "   [下层]";
                    Debug.Log($"   🎨 地质层 {j + 1}: {layer.layerName} {layerStatus}");
                    Debug.Log($"      深度: {layer.depthStart:F2}m - {layer.depthEnd:F2}m");
                    Debug.Log($"      颜色: #{colorHtml} (R={layer.layerColor.r:F2}, G={layer.layerColor.g:F2}, B={layer.layerColor.b:F2})");
                    Debug.Log($"      厚度: {layer.thickness:F2}m");
                    
                    // 统计颜色
                    if (colorCounts.ContainsKey(colorHtml))
                        colorCounts[colorHtml]++;
                    else
                        colorCounts[colorHtml] = 1;
                }
                
                // 显示选中的表面层
                string topColorHtml = ColorUtility.ToHtmlStringRGBA(topLayer.layerColor);
                Debug.Log($"   🏆 选中表面层: {topLayer.layerName} (#{topColorHtml})");
            }
            else
            {
                Debug.LogWarning($"   ❌ 没有地质层数据！");
            }
        }
        
        // 显示统计结果
        Debug.Log("\n" + "=".PadRight(50, '='));
        Debug.Log("📈 统计结果:");
        
        Debug.Log("🛠️ 工具类型分布:");
        foreach (var tool in toolCounts)
        {
            string toolName = tool.Key switch
            {
                "1000" => "简易钻探",
                "1001" => "钻塔",
                "1002" => "地质锤",
                _ => "未知工具"
            };
            Debug.Log($"   {toolName} ({tool.Key}): {tool.Value} 个");
        }
        
        Debug.Log("🎨 颜色分布:");
        foreach (var color in colorCounts)
        {
            Debug.Log($"   #{color.Key}: {color.Value} 个地质层");
        }
        
        // 问题分析
        Debug.Log("\n🔍 问题分析:");
        if (colorCounts.Count == 1)
        {
            Debug.LogWarning("⚠️ 所有地质层使用相同颜色 - 这可能是问题所在！");
        }
        if (toolCounts.Count == 1)
        {
            Debug.LogWarning("⚠️ 所有样本使用相同工具 - 颜色差异应该来自地质层");
        }
        
        Debug.Log("=".PadRight(50, '='));
    }
    
    /// <summary>
    /// 显示测试帮助信息
    /// </summary>
    [ContextMenu("显示测试帮助")]
    public void ShowTestHelp()
    {
        Debug.Log("🔧 样本图标测试器使用说明:");
        Debug.Log("1. 创建测试样本 - 生成不同形状和颜色的测试样本");
        Debug.Log("2. 诊断样本颜色问题 - 分析现有样本的颜色数据");
        Debug.Log("3. 刷新现有样本图标 - 重新生成所有样本图标");
        Debug.Log("4. 测试图标生成性能 - 测试大量图标生成的性能");
        Debug.Log("5. 测试所有形状颜色组合 - 验证所有可能的图标组合");
        Debug.Log("6. 清理测试样本 - 移除所有测试样本并清理缓存");
        Debug.Log("7. 按 I 键打开背包查看图标效果");
        Debug.Log("");
        Debug.Log("📝 图标规则:");
        Debug.Log("• 钻探工具(1000) → 圆柱形图标");
        Debug.Log("• 钻塔工具(1001) → 圆柱形图标");
        Debug.Log("• 地质锤(1002) → 薄片形图标");
        Debug.Log("• 图标颜色来自样本的地质层颜色");
        Debug.Log("");
        Debug.Log("🐛 如果所有图标都是同一颜色:");
        Debug.Log("1. 先运行'诊断样本颜色问题'查看详细信息");
        Debug.Log("2. 然后运行'刷新现有样本图标'重新生成");
        Debug.Log("3. 查看Console日志了解颜色选择过程");
    }
}
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 样本图标调试器 - 专门用于诊断图标颜色问题
/// </summary>
public class SampleIconDebugger : MonoBehaviour
{
    [Header("调试设置")]
    public bool enableDetailedLogging = true;
    public bool refreshIconsOnStart = false;
    
    void Start()
    {
        if (refreshIconsOnStart)
        {
            DebugAndRefreshAllSampleIcons();
        }
    }
    
    /// <summary>
    /// 调试并刷新所有样本图标
    /// </summary>
    [ContextMenu("调试并刷新所有样本图标")]
    public void DebugAndRefreshAllSampleIcons()
    {
        Debug.Log("🔧 开始调试所有样本图标...");
        Debug.Log("".PadRight(60, '='));
        
        if (SampleInventory.Instance == null)
        {
            Debug.LogError("❌ SampleInventory 实例不存在");
            return;
        }
        
        if (SampleIconGenerator.Instance == null)
        {
            Debug.LogError("❌ SampleIconGenerator 实例不存在");
            return;
        }
        
        var allSamples = SampleInventory.Instance.GetAllSamples();
        if (allSamples.Count == 0)
        {
            Debug.LogWarning("⚠️ 背包中没有样本");
            return;
        }
        
        Debug.Log($"📦 找到 {allSamples.Count} 个样本，开始逐个分析...");
        
        for (int i = 0; i < allSamples.Count; i++)
        {
            var sample = allSamples[i];
            Debug.Log($"\n🔍 分析样本 {i + 1}/{allSamples.Count}: {sample.displayName}");
            Debug.Log($"   样本ID: {sample.sampleID}");
            Debug.Log($"   工具ID: {sample.sourceToolID}");
            Debug.Log($"   采集时间: {sample.collectionTime:yyyy-MM-dd HH:mm:ss}");
            Debug.Log($"   原图标: {(sample.previewIcon != null ? sample.previewIcon.name : "无")}");
            
            // 清理旧图标缓存
            SampleIconGenerator.Instance.RefreshSampleIcon(sample);
            
            // 重新生成图标 - 这会产生详细的调试输出
            Sprite newIcon = SampleIconGenerator.Instance.GenerateIconForSample(sample);
            
            if (newIcon != null)
            {
                sample.previewIcon = newIcon;
                Debug.Log($"   ✅ 新图标: {newIcon.name}");
            }
            else
            {
                Debug.LogError($"   ❌ 图标生成失败");
            }
            
            Debug.Log("-".PadRight(50, '-'));
        }
        
        Debug.Log("".PadRight(60, '='));
        Debug.Log($"🎉 样本图标调试完成！共处理 {allSamples.Count} 个样本");
        Debug.Log("💡 提示: 重新打开背包 (I键) 查看新的图标效果");
        
        // 触发背包界面刷新
        if (SampleInventory.Instance != null)
        {
            SampleInventory.Instance.OnInventoryChanged?.Invoke();
        }
    }
    
    /// <summary>
    /// 分析单个样本的地质层数据
    /// </summary>
    [ContextMenu("分析样本地质层数据")]
    public void AnalyzeSampleGeologicalData()
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
        
        Debug.Log("🔬 分析样本地质层数据...");
        Debug.Log("".PadRight(60, '='));
        
        Dictionary<string, int> toolStats = new Dictionary<string, int>();
        Dictionary<string, int> layerColorStats = new Dictionary<string, int>();
        int samplesWithValidColors = 0;
        int samplesWithoutLayers = 0;
        
        foreach (var sample in allSamples)
        {
            Debug.Log($"\n📦 样本: {sample.displayName}");
            Debug.Log($"   ID: {sample.sampleID}");
            Debug.Log($"   工具: {sample.sourceToolID}");
            
            // 统计工具类型
            if (toolStats.ContainsKey(sample.sourceToolID))
                toolStats[sample.sourceToolID]++;
            else
                toolStats[sample.sourceToolID] = 1;
            
            // 分析地质层
            if (sample.geologicalLayers != null && sample.geologicalLayers.Count > 0)
            {
                Debug.Log($"   地质层数量: {sample.geologicalLayers.Count}");
                
                bool hasValidColor = false;
                for (int i = 0; i < sample.geologicalLayers.Count; i++)
                {
                    var layer = sample.geologicalLayers[i];
                    string colorHtml = ColorUtility.ToHtmlStringRGBA(layer.layerColor);
                    
                    Debug.Log($"     层 {i + 1}: {layer.layerName}");
                    Debug.Log($"       深度: {layer.depthStart:F2}m - {layer.depthEnd:F2}m");
                    Debug.Log($"       颜色: #{colorHtml} (R={layer.layerColor.r:F2}, G={layer.layerColor.g:F2}, B={layer.layerColor.b:F2}, A={layer.layerColor.a:F2})");
                    
                    // 统计颜色
                    if (layerColorStats.ContainsKey(colorHtml))
                        layerColorStats[colorHtml]++;
                    else
                        layerColorStats[colorHtml] = 1;
                    
                    // 检查是否有有效颜色
                    float brightness = (layer.layerColor.r + layer.layerColor.g + layer.layerColor.b) / 3f;
                    if (brightness < 0.95f && layer.layerColor.a >= 0.05f)
                    {
                        hasValidColor = true;
                    }
                }
                
                if (hasValidColor)
                {
                    samplesWithValidColors++;
                    Debug.Log($"   ✅ 有有效颜色");
                }
                else
                {
                    Debug.Log($"   ⚠️ 所有颜色都过浅或透明");
                }
            }
            else
            {
                Debug.LogWarning($"   ❌ 没有地质层数据");
                samplesWithoutLayers++;
            }
        }
        
        // 显示统计结果
        Debug.Log("\n".PadRight(60, '='));
        Debug.Log("📊 统计结果:");
        
        Debug.Log("\n🛠️ 工具类型分布:");
        foreach (var tool in toolStats)
        {
            string toolName = tool.Key switch
            {
                "1000" => "简易钻探",
                "1001" => "钻塔",
                "1002" => "地质锤",
                _ => "未知工具"
            };
            Debug.Log($"   {toolName} ({tool.Key}): {tool.Value} 个样本");
        }
        
        Debug.Log("\n🎨 颜色分布 (前10种):");
        var sortedColors = new List<KeyValuePair<string, int>>(layerColorStats);
        sortedColors.Sort((x, y) => y.Value.CompareTo(x.Value));
        
        for (int i = 0; i < Mathf.Min(10, sortedColors.Count); i++)
        {
            var color = sortedColors[i];
            Debug.Log($"   #{color.Key}: {color.Value} 个地质层");
        }
        
        Debug.Log($"\n📈 数据质量:");
        Debug.Log($"   总样本数: {allSamples.Count}");
        Debug.Log($"   有地质层数据: {allSamples.Count - samplesWithoutLayers}");
        Debug.Log($"   无地质层数据: {samplesWithoutLayers}");
        Debug.Log($"   有有效颜色: {samplesWithValidColors}");
        Debug.Log($"   颜色可能有问题: {allSamples.Count - samplesWithValidColors}");
        
        if (samplesWithoutLayers > 0)
        {
            Debug.LogWarning($"⚠️ 有 {samplesWithoutLayers} 个样本没有地质层数据，这些样本只能使用工具默认颜色");
        }
        
        if (samplesWithValidColors == 0)
        {
            Debug.LogError($"❌ 所有样本的地质层颜色都过浅或透明，这是图标颜色问题的根本原因！");
        }
    }
    
    /// <summary>
    /// 测试颜色亮度判断算法
    /// </summary>
    [ContextMenu("测试颜色亮度判断")]
    public void TestColorBrightnessCheck()
    {
        Debug.Log("🌈 测试颜色亮度判断算法...");
        
        Color[] testColors = {
            Color.white,           // 纯白色
            Color.black,           // 纯黑色
            Color.red,             // 纯红色
            Color.green,           // 纯绿色
            Color.blue,            // 纯蓝色
            Color.yellow,          // 纯黄色
            Color.gray,            // 灰色
            new Color(0.9f, 0.9f, 0.9f), // 浅灰色
            new Color(0.1f, 0.1f, 0.1f), // 深灰色
            new Color(0.8f, 0.5f, 0.2f), // 棕色
            new Color(0.3f, 0.7f, 0.2f), // 绿色
            new Color(1f, 1f, 1f, 0f),   // 透明白色
            new Color(0.5f, 0.5f, 0.5f, 0.5f), // 半透明灰色
        };
        
        string[] colorNames = {
            "纯白色", "纯黑色", "纯红色", "纯绿色", "纯蓝色", "纯黄色", "标准灰色",
            "浅灰色", "深灰色", "棕色", "绿色", "透明白色", "半透明灰色"
        };
        
        for (int i = 0; i < testColors.Length; i++)
        {
            Color color = testColors[i];
            float brightness = (color.r + color.g + color.b) / 3f;
            bool isTooLight = brightness > 0.95f || color.a < 0.05f || 
                             (color.r > 0.98f && color.g > 0.98f && color.b > 0.98f);
            
            Debug.Log($"   {colorNames[i]}: #{ColorUtility.ToHtmlStringRGBA(color)}");
            Debug.Log($"     亮度: {brightness:F3}, 透明度: {color.a:F3}");
            Debug.Log($"     判断: {(isTooLight ? "❌ 过浅" : "✅ 合适")}");
        }
    }
    
    /// <summary>
    /// 清理所有样本图标缓存
    /// </summary>
    [ContextMenu("清理图标缓存")]
    public void ClearAllIconCache()
    {
        if (SampleIconGenerator.Instance != null)
        {
            SampleIconGenerator.Instance.ClearIconCache();
            Debug.Log("✅ 图标缓存已清理");
        }
        else
        {
            Debug.LogWarning("⚠️ SampleIconGenerator 实例不存在");
        }
    }
    
    /// <summary>
    /// 测试明亮色图标生成
    /// </summary>
    [ContextMenu("测试明亮色图标生成")]
    public void TestBrightColorIcons()
    {
        if (SampleIconGenerator.Instance == null)
        {
            Debug.LogError("❌ SampleIconGenerator 实例不存在");
            return;
        }
        
        if (SampleInventory.Instance == null)
        {
            Debug.LogError("❌ SampleInventory 实例不存在");
            return;
        }
        
        Debug.Log("🌈 开始测试明亮色图标生成...");
        
        // 创建测试样本，强制使用明亮的颜色
        var testSample = new SampleItem
        {
            sampleID = "BRIGHT_COLOR_TEST",
            displayName = "明亮色测试样本",
            sourceToolID = "1000", // 简易钻探 - 圆柱形
            geologicalLayers = new List<SampleItem.LayerInfo>
            {
                new SampleItem.LayerInfo
                {
                    layerName = "明亮测试层",
                    layerColor = Color.red, // 明亮的红色
                    thickness = 1.0f,
                    depthStart = 0f,
                    depthEnd = 1.0f
                }
            }
        };
        
        // 清理缓存确保重新生成
        SampleIconGenerator.Instance.ClearIconCache();
        
        // 生成图标
        Sprite testIcon = SampleIconGenerator.Instance.GenerateIconForSample(testSample);
        
        if (testIcon != null)
        {
            testSample.previewIcon = testIcon;
            Debug.Log($"✅ 明亮色测试图标生成成功: {testIcon.name}");
            
            // 添加到背包
            if (SampleInventory.Instance.TryAddSample(testSample))
            {
                Debug.Log("✅ 测试样本已添加到背包");
                Debug.Log("💡 打开背包 (I键) 查看明亮红色圆柱形图标");
                
                // 触发背包刷新
                SampleInventory.Instance.OnInventoryChanged?.Invoke();
            }
            else
            {
                Debug.LogWarning("⚠️ 无法添加测试样本到背包");
            }
        }
        else
        {
            Debug.LogError("❌ 明亮色测试图标生成失败");
        }
    }
    
    /// <summary>
    /// 刷新所有样本图标（应用新的灰色检测逻辑）
    /// </summary>
    [ContextMenu("刷新样本图标（应用颜色增强）")]
    public void RefreshSampleIconsWithColorEnhancement()
    {
        if (SampleIconGenerator.Instance == null || SampleInventory.Instance == null)
        {
            Debug.LogError("❌ 必要组件不存在");
            return;
        }
        
        Debug.Log("🎨 开始刷新样本图标，应用颜色增强...");
        
        // 清理所有图标缓存
        SampleIconGenerator.Instance.ClearIconCache();
        
        var allSamples = SampleInventory.Instance.GetAllSamples();
        Debug.Log($"📦 找到 {allSamples.Count} 个样本需要刷新");
        
        for (int i = 0; i < allSamples.Count; i++)
        {
            var sample = allSamples[i];
            
            Debug.Log($"\\n🔄 刷新样本 {i + 1}/{allSamples.Count}: {sample.displayName}");
            
            // 强制重新生成图标
            sample.previewIcon = null; // 清理旧图标
            Sprite newIcon = SampleIconGenerator.Instance.GenerateIconForSample(sample);
            
            if (newIcon != null)
            {
                sample.previewIcon = newIcon;
                Debug.Log($"   ✅ 新图标: {newIcon.name}");
            }
            else
            {
                Debug.LogError($"   ❌ 图标生成失败");
            }
        }
        
        // 触发背包界面刷新
        SampleInventory.Instance.OnInventoryChanged?.Invoke();
        
        Debug.Log($"🎉 图标刷新完成！现在灰色样本将显示为鲜艳的工具颜色");
        Debug.Log("💡 简易钻探=橙色, 钻塔=绿色, 地质锤=红褐色");
    }
    
    /// <summary>
    /// 创建单个红色测试样本（用于验证当前问题）
    /// </summary>
    [ContextMenu("创建红色测试样本")]
    public void CreateRedTestSample()
    {
        if (SampleIconGenerator.Instance == null || SampleInventory.Instance == null)
        {
            Debug.LogError("❌ 必要组件不存在");
            return;
        }
        
        Debug.Log("🔴 创建红色测试样本...");
        
        var testSample = new SampleItem
        {
            sampleID = "RED_TEST_SAMPLE",
            displayName = "红色测试样本",
            sourceToolID = "1000", // 简易钻探 - 圆柱形
            geologicalLayers = new List<SampleItem.LayerInfo>
            {
                new SampleItem.LayerInfo
                {
                    layerName = "红色测试层",
                    layerColor = Color.red, // 纯红色
                    thickness = 2.0f,
                    depthStart = 0f,
                    depthEnd = 2f
                }
            }
        };
        
        // 强制清理缓存
        SampleIconGenerator.Instance.ClearIconCache();
        
        // 生成图标
        Sprite testIcon = SampleIconGenerator.Instance.GenerateIconForSample(testSample);
        if (testIcon != null)
        {
            testSample.previewIcon = testIcon;
            Debug.Log($"✅ 红色测试图标生成成功: {testIcon.name}");
            
            // 添加到背包
            if (SampleInventory.Instance.TryAddSample(testSample))
            {
                Debug.Log("✅ 红色测试样本已添加到背包");
                Debug.Log("💡 打开背包 (I键) 查看红色圆柱形图标");
                
                // 触发背包刷新
                SampleInventory.Instance.OnInventoryChanged?.Invoke();
            }
            else
            {
                Debug.LogWarning("⚠️ 无法添加红色测试样本到背包");
            }
        }
        else
        {
            Debug.LogError("❌ 红色测试图标生成失败");
        }
    }
    
    /// <summary>
    /// 创建彩虹测试样本
    /// </summary>
    [ContextMenu("创建彩虹测试样本")]
    public void CreateRainbowTestSamples()
    {
        if (SampleIconGenerator.Instance == null || SampleInventory.Instance == null)
        {
            Debug.LogError("❌ 必要组件不存在");
            return;
        }
        
        Debug.Log("🌈 创建彩虹测试样本...");
        
        Color[] rainbowColors = {
            Color.red,      // 红色
            new Color(1f, 0.5f, 0f),  // 橙色
            Color.yellow,   // 黄色
            Color.green,    // 绿色
            Color.blue,     // 蓝色
            new Color(0.5f, 0f, 1f),  // 紫色
        };
        
        string[] colorNames = { "红色", "橙色", "黄色", "绿色", "蓝色", "紫色" };
        
        for (int i = 0; i < rainbowColors.Length; i++)
        {
            var testSample = new SampleItem
            {
                sampleID = $"RAINBOW_TEST_{i}",
                displayName = $"{colorNames[i]}测试样本",
                sourceToolID = i % 2 == 0 ? "1000" : "1002", // 交替圆柱形和薄片形
                geologicalLayers = new List<SampleItem.LayerInfo>
                {
                    new SampleItem.LayerInfo
                    {
                        layerName = $"{colorNames[i]}测试层",
                        layerColor = rainbowColors[i],
                        thickness = 1.0f
                    }
                }
            };
            
            Sprite testIcon = SampleIconGenerator.Instance.GenerateIconForSample(testSample);
            if (testIcon != null)
            {
                testSample.previewIcon = testIcon;
                SampleInventory.Instance.TryAddSample(testSample);
                Debug.Log($"✅ 创建{colorNames[i]}样本: {testIcon.name}");
            }
        }
        
        Debug.Log("🎉 彩虹测试样本创建完成！打开背包查看效果");
        SampleInventory.Instance.OnInventoryChanged?.Invoke();
    }
    
    /// <summary>
    /// 检查样本图标引用
    /// </summary>
    [ContextMenu("检查样本图标引用")]
    public void CheckSampleIconReferences()
    {
        if (SampleInventory.Instance == null)
        {
            Debug.LogError("❌ SampleInventory 实例不存在");
            return;
        }
        
        var allSamples = SampleInventory.Instance.GetAllSamples();
        Debug.Log($"🔍 检查 {allSamples.Count} 个样本的图标引用...");
        
        for (int i = 0; i < allSamples.Count; i++)
        {
            var sample = allSamples[i];
            Debug.Log($"\n📦 样本 {i + 1}: {sample.displayName}");
            Debug.Log($"   ID: {sample.sampleID}");
            Debug.Log($"   工具: {sample.sourceToolID}");
            
            if (sample.previewIcon != null)
            {
                Debug.Log($"   图标名称: {sample.previewIcon.name}");
                Debug.Log($"   图标纹理: {sample.previewIcon.texture?.name ?? "null"}");
                Debug.Log($"   图标尺寸: {sample.previewIcon.texture?.width}x{sample.previewIcon.texture?.height}");
            }
            else
            {
                Debug.LogWarning($"   ❌ previewIcon 为 null");
            }
            
            // 检查Icon属性
            if (sample.Icon != null)
            {
                Debug.Log($"   Icon属性: {sample.Icon.name}");
                Debug.Log($"   Icon == previewIcon: {sample.Icon == sample.previewIcon}");
            }
            else
            {
                Debug.LogWarning($"   ❌ Icon属性 为 null");
            }
            
            // 检查地质层颜色
            if (sample.geologicalLayers != null && sample.geologicalLayers.Count > 0)
            {
                var topLayer = sample.geologicalLayers[0];
                string colorHtml = ColorUtility.ToHtmlStringRGBA(topLayer.layerColor);
                Debug.Log($"   地质层颜色: #{colorHtml}");
                Debug.Log($"   期望图标: SampleIcon_Cylinder_{ColorUtility.ToHtmlStringRGB(topLayer.layerColor)}");
            }
        }
    }
}
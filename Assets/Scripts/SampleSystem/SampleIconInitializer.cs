using UnityEngine;

/// <summary>
/// 样本图标系统初始化器 - 确保SampleIconGenerator在游戏开始时正确初始化
/// </summary>
public class SampleIconInitializer : MonoBehaviour
{
    [Header("图标生成器设置")]
    public bool createIconGeneratorOnStart = true;
    public bool preGenerateCommonIcons = true;
    
    [Header("图标生成器配置")]
    public int iconSize = 128;
    public Color outlineColor = Color.black;
    public float outlineWidth = 2f;
    
    void Awake()
    {
        InitializeSampleIconSystem();
    }
    
    /// <summary>
    /// 初始化样本图标系统
    /// </summary>
    void InitializeSampleIconSystem()
    {
        // 检查是否已经存在SampleIconGenerator实例
        if (SampleIconGenerator.Instance == null && createIconGeneratorOnStart)
        {
            CreateSampleIconGenerator();
        }
        
        // 预生成常用图标（可选优化）
        if (preGenerateCommonIcons && SampleIconGenerator.Instance != null)
        {
            SampleIconGenerator.Instance.PreGenerateCommonIcons();
        }
        
        Debug.Log("[SampleIconInitializer] 样本图标系统初始化完成");
    }
    
    /// <summary>
    /// 创建SampleIconGenerator实例
    /// </summary>
    void CreateSampleIconGenerator()
    {
        GameObject iconGeneratorObj = new GameObject("SampleIconGenerator");
        DontDestroyOnLoad(iconGeneratorObj);
        
        SampleIconGenerator generator = iconGeneratorObj.AddComponent<SampleIconGenerator>();
        
        // 配置图标生成器
        generator.iconSize = iconSize;
        generator.outlineColor = outlineColor;
        generator.outlineWidth = outlineWidth;
        
        Debug.Log("[SampleIconInitializer] 已创建 SampleIconGenerator 实例");
    }
    
    /// <summary>
    /// 测试图标生成功能
    /// </summary>
    [ContextMenu("测试图标生成")]
    void TestIconGeneration()
    {
        if (SampleIconGenerator.Instance == null)
        {
            Debug.LogWarning("SampleIconGenerator 实例不存在，无法测试");
            return;
        }
        
        // 创建测试样本
        var testSample = new SampleItem
        {
            sampleID = "TEST_ICON_001",
            displayName = "测试样本",
            sourceToolID = "1000", // 钻探工具
            geologicalLayers = new System.Collections.Generic.List<SampleItem.LayerInfo>
            {
                new SampleItem.LayerInfo
                {
                    layerName = "测试层",
                    layerColor = Color.red,
                    thickness = 1.0f
                }
            }
        };
        
        // 生成图标
        Sprite testIcon = SampleIconGenerator.Instance.GenerateIconForSample(testSample);
        
        if (testIcon != null)
        {
            Debug.Log($"成功生成测试图标: {testIcon.name}");
        }
        else
        {
            Debug.LogError("测试图标生成失败");
        }
        
        // 显示缓存统计
        Debug.Log(SampleIconGenerator.Instance.GetCacheStats());
    }
    
    /// <summary>
    /// 验证图标系统状态
    /// </summary>
    [ContextMenu("验证图标系统")]
    public void ValidateIconSystem()
    {
        bool isValid = true;
        
        if (SampleIconGenerator.Instance == null)
        {
            Debug.LogError("❌ SampleIconGenerator 实例不存在");
            isValid = false;
        }
        else
        {
            Debug.Log("✅ SampleIconGenerator 实例存在");
            Debug.Log($"📊 {SampleIconGenerator.Instance.GetCacheStats()}");
        }
        
        // 检查样本背包系统
        var sampleInventory = FindFirstObjectByType<SampleInventory>();
        if (sampleInventory == null)
        {
            Debug.LogWarning("⚠️ SampleInventory 系统未找到");
        }
        else
        {
            Debug.Log("✅ SampleInventory 系统存在");
        }
        
        // 检查背包UI系统
        var inventoryUI = FindFirstObjectByType<InventoryUI>();
        if (inventoryUI == null)
        {
            Debug.LogWarning("⚠️ InventoryUI 系统未找到");
        }
        else
        {
            Debug.Log("✅ InventoryUI 系统存在");
        }
        
        if (isValid)
        {
            Debug.Log("🎉 样本图标系统验证通过！");
        }
        else
        {
            Debug.LogError("💥 样本图标系统验证失败，请检查配置");
        }
    }
    
    /// <summary>
    /// 清理图标缓存
    /// </summary>
    [ContextMenu("清理图标缓存")]
    void ClearIconCache()
    {
        if (SampleIconGenerator.Instance != null)
        {
            SampleIconGenerator.Instance.ClearIconCache();
            Debug.Log("图标缓存已清理");
        }
        else
        {
            Debug.LogWarning("SampleIconGenerator 实例不存在，无法清理缓存");
        }
    }
}

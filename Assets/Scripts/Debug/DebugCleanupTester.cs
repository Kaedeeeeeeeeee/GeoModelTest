using UnityEngine;

/// <summary>
/// 调试清理测试器 - 验证清理系统是否工作
/// </summary>
public class DebugCleanupTester : MonoBehaviour
{
    [Header("测试设置")]
    [SerializeField] private bool testOnStart = false;
    
    void Start()
    {
        if (testOnStart)
        {
            TestCleanupSystem();
        }
    }
    
    /// <summary>
    /// 测试清理系统
    /// </summary>
    [ContextMenu("测试清理系统")]
    public void TestCleanupSystem()
    {
        Debug.Log("=== 🧪 调试清理系统测试开始 ===");
        
        // 测试各个系统的调试状态
        TestEncyclopediaSystem();
        TestLocalizationSystem();
        TestWarehouseSystem();
        TestGameInitializer();
        TestSampleSystem();
        
        Debug.Log("=== ✅ 调试清理系统测试完成 ===");
        Debug.Log("📝 如果看到很多'已禁用'消息，说明清理系统工作正常");
    }
    
    private void TestEncyclopediaSystem()
    {
        var managers = FindObjectsByType<Encyclopedia.SimpleEncyclopediaManager>(FindObjectsSortMode.None);
        Debug.Log($"📚 Encyclopedia系统: 找到 {managers.Length} 个SimpleEncyclopediaManager");
        
        foreach (var manager in managers)
        {
            var field = typeof(Encyclopedia.SimpleEncyclopediaManager).GetField("showDebugInfo", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                bool value = (bool)field.GetValue(manager);
                Debug.Log($"  - showDebugInfo = {value} {(value ? "❌需要清理" : "✅已清理")}");
            }
        }
    }
    
    private void TestLocalizationSystem()
    {
        Debug.Log($"🌐 Localization系统:");
        
        if (LocalizationManager.Instance != null)
        {
            var field = typeof(LocalizationManager).GetField("enableDebugLog", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                bool value = (bool)field.GetValue(LocalizationManager.Instance);
                Debug.Log($"  - LocalizationManager.enableDebugLog = {value} {(value ? "❌需要清理" : "✅已清理")}");
            }
        }
        else
        {
            Debug.Log("  - LocalizationManager 未找到");
        }
    }
    
    private void TestWarehouseSystem()
    {
        var managers = FindObjectsByType<WarehouseManager>(FindObjectsSortMode.None);
        Debug.Log($"📦 Warehouse系统: 找到 {managers.Length} 个WarehouseManager");
        
        foreach (var manager in managers)
        {
            var field = typeof(WarehouseManager).GetField("enableDebugLog", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                bool value = (bool)field.GetValue(manager);
                Debug.Log($"  - enableDebugLog = {value} {(value ? "❌需要清理" : "✅已清理")}");
            }
        }
    }
    
    private void TestGameInitializer()
    {
        var initializers = FindObjectsByType<GameInitializer>(FindObjectsSortMode.None);
        Debug.Log($"⚙️ GameInitializer系统: 找到 {initializers.Length} 个GameInitializer");
        
        foreach (var initializer in initializers)
        {
            var field = typeof(GameInitializer).GetField("enableDebugMode", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                bool value = (bool)field.GetValue(initializer);
                Debug.Log($"  - enableDebugMode = {value} {(value ? "❌需要清理" : "✅已清理")}");
            }
        }
    }
    
    private void TestSampleSystem()
    {
        var setups = FindObjectsByType<ManualSampleSetup>(FindObjectsSortMode.None);
        Debug.Log($"🧪 Sample系统: 找到 {setups.Length} 个ManualSampleSetup");
        
        foreach (var setup in setups)
        {
            var field = typeof(ManualSampleSetup).GetField("enableDebugMode", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                bool value = (bool)field.GetValue(setup);
                Debug.Log($"  - enableDebugMode = {value} {(value ? "❌需要清理" : "✅已清理")}");
            }
        }
    }
    
    /// <summary>
    /// 手动触发清理
    /// </summary>
    [ContextMenu("手动清理调试输出")]
    public void ManualCleanup()
    {
        var cleaner = FindFirstObjectByType<SimpleDebugCleaner>();
        if (cleaner != null)
        {
            cleaner.CleanupDebugOutput();
            Debug.Log("✅ 已使用现有的SimpleDebugCleaner进行清理");
        }
        else
        {
            Debug.LogWarning("⚠️ 未找到SimpleDebugCleaner，请添加该组件到场景中");
        }
    }
}
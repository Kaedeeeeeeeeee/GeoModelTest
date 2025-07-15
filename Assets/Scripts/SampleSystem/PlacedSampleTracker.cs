using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 已放置样本跟踪器 - 管理场景中所有已放置的样本，处理场景切换清理
/// </summary>
public class PlacedSampleTracker : MonoBehaviour
{
    [Header("跟踪设置")]
    public bool enableDebugLog = true;
    public bool autoCleanupOnSceneChange = true;
    
    [Header("清理设置")]
    public bool saveDataOnSceneChange = true;
    public float cleanupDelay = 0.5f;
    
    // 单例模式
    public static PlacedSampleTracker Instance { get; private set; }
    
    // 已放置样本跟踪
    private static Dictionary<string, GameObject> placedSamples = new Dictionary<string, GameObject>();
    private static Dictionary<string, SampleItem> sampleDataCache = new Dictionary<string, SampleItem>();
    
    // 事件系统
    public static System.Action<GameObject> OnSamplePlaced;
    public static System.Action<GameObject> OnSampleRemoved;
    public static System.Action OnAllSamplesCleared;
    
    void Awake()
    {
        // 单例模式设置
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeTracker();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        SetupSceneChangeHandling();
    }
    
    /// <summary>
    /// 初始化跟踪器
    /// </summary>
    void InitializeTracker()
    {
        if (placedSamples == null)
        {
            placedSamples = new Dictionary<string, GameObject>();
        }
        
        if (sampleDataCache == null)
        {
            sampleDataCache = new Dictionary<string, SampleItem>();
        }
        
        LogMessage($"已放置样本跟踪器已初始化 - 自动清理: {autoCleanupOnSceneChange}");
    }
    
    /// <summary>
    /// 设置场景切换处理
    /// </summary>
    void SetupSceneChangeHandling()
    {
        if (autoCleanupOnSceneChange)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }
    }
    
    /// <summary>
    /// 场景加载事件处理
    /// </summary>
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        LogMessage($"场景已加载: {scene.name}, 模式: {mode}");
        
        if (mode == LoadSceneMode.Single)
        {
            // 单场景加载时清理旧样本
            Invoke(nameof(CleanupOrphanedSamples), cleanupDelay);
        }
    }
    
    /// <summary>
    /// 场景卸载事件处理
    /// </summary>
    void OnSceneUnloaded(Scene scene)
    {
        LogMessage($"场景已卸载: {scene.name}");
        
        if (saveDataOnSceneChange)
        {
            SaveSampleDataBeforeCleanup();
        }
        
        CleanupSamplesFromScene(scene);
    }
    
    /// <summary>
    /// 注册已放置的样本
    /// </summary>
    public static void RegisterPlacedSample(GameObject sampleObject)
    {
        if (sampleObject == null)
        {
            Debug.LogWarning("尝试注册空的样本对象");
            return;
        }
        
        string sampleKey = GenerateSampleKey(sampleObject);
        
        if (placedSamples.ContainsKey(sampleKey))
        {
            Instance?.LogMessage($"样本已存在于跟踪器中: {sampleKey}");
            return;
        }
        
        placedSamples[sampleKey] = sampleObject;
        
        // 缓存样本数据
        var collector = sampleObject.GetComponent<PlacedSampleCollector>();
        if (collector != null && collector.originalSampleData != null)
        {
            sampleDataCache[sampleKey] = collector.originalSampleData;
        }
        
        Instance?.LogMessage($"已注册放置样本: {sampleObject.name} (Key: {sampleKey})");
        OnSamplePlaced?.Invoke(sampleObject);
    }
    
    /// <summary>
    /// 注销已放置的样本
    /// </summary>
    public static void UnregisterPlacedSample(GameObject sampleObject)
    {
        if (sampleObject == null) return;
        
        string sampleKey = GenerateSampleKey(sampleObject);
        
        if (placedSamples.Remove(sampleKey))
        {
            sampleDataCache.Remove(sampleKey);
            Instance?.LogMessage($"已注销放置样本: {sampleObject.name} (Key: {sampleKey})");
            OnSampleRemoved?.Invoke(sampleObject);
        }
    }
    
    /// <summary>
    /// 生成样本唯一键
    /// </summary>
    static string GenerateSampleKey(GameObject sampleObject)
    {
        // 使用对象实例ID和名称生成唯一键
        return $"{sampleObject.GetInstanceID()}_{sampleObject.name}";
    }
    
    /// <summary>
    /// 获取所有已放置的样本
    /// </summary>
    public static List<GameObject> GetAllPlacedSamples()
    {
        return placedSamples.Values.Where(obj => obj != null).ToList();
    }
    
    /// <summary>
    /// 获取已放置样本的数量
    /// </summary>
    public static int GetPlacedSampleCount()
    {
        return placedSamples.Count(kvp => kvp.Value != null);
    }
    
    /// <summary>
    /// 根据样本ID查找已放置的样本
    /// </summary>
    public static GameObject FindPlacedSampleByID(string sampleID)
    {
        foreach (var kvp in sampleDataCache)
        {
            if (kvp.Value.sampleID == sampleID)
            {
                if (placedSamples.TryGetValue(kvp.Key, out GameObject sampleObj))
                {
                    return sampleObj;
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// 清理孤立的样本（无效引用）
    /// </summary>
    public void CleanupOrphanedSamples()
    {
        var keysToRemove = new List<string>();
        
        foreach (var kvp in placedSamples)
        {
            if (kvp.Value == null)
            {
                keysToRemove.Add(kvp.Key);
            }
        }
        
        foreach (string key in keysToRemove)
        {
            placedSamples.Remove(key);
            sampleDataCache.Remove(key);
        }
        
        if (keysToRemove.Count > 0)
        {
            LogMessage($"已清理 {keysToRemove.Count} 个孤立样本引用");
        }
    }
    
    /// <summary>
    /// 清理指定场景中的样本
    /// </summary>
    void CleanupSamplesFromScene(Scene scene)
    {
        var keysToRemove = new List<string>();
        
        foreach (var kvp in placedSamples)
        {
            if (kvp.Value != null && kvp.Value.scene == scene)
            {
                keysToRemove.Add(kvp.Key);
            }
        }
        
        foreach (string key in keysToRemove)
        {
            placedSamples.Remove(key);
            sampleDataCache.Remove(key);
        }
        
        if (keysToRemove.Count > 0)
        {
            LogMessage($"已清理场景 {scene.name} 中的 {keysToRemove.Count} 个样本");
        }
    }
    
    /// <summary>
    /// 清理前保存样本数据
    /// </summary>
    void SaveSampleDataBeforeCleanup()
    {
        foreach (var kvp in placedSamples)
        {
            if (kvp.Value != null)
            {
                var collector = kvp.Value.GetComponent<PlacedSampleCollector>();
                if (collector != null && collector.originalSampleData != null)
                {
                    // 确保样本数据中的世界位置是最新的
                    collector.originalSampleData.currentWorldPosition = kvp.Value.transform.position;
                    
                    // 更新缓存
                    sampleDataCache[kvp.Key] = collector.originalSampleData;
                }
            }
        }
        
        LogMessage($"已保存 {sampleDataCache.Count} 个样本的数据");
    }
    
    /// <summary>
    /// 清理所有已放置的样本
    /// </summary>
    public static void ClearAllPlacedSamples()
    {
        var samples = GetAllPlacedSamples();
        
        foreach (var sample in samples)
        {
            if (sample != null)
            {
                Destroy(sample);
            }
        }
        
        placedSamples.Clear();
        sampleDataCache.Clear();
        
        Instance?.LogMessage($"已清理所有已放置的样本 ({samples.Count} 个)");
        OnAllSamplesCleared?.Invoke();
    }
    
    /// <summary>
    /// 强制清理无效样本
    /// </summary>
    public static void ForceCleanup()
    {
        if (Instance != null)
        {
            Instance.CleanupOrphanedSamples();
        }
    }
    
    /// <summary>
    /// 获取跟踪器统计信息
    /// </summary>
    public string GetTrackerStats()
    {
        int validSamples = GetPlacedSampleCount();
        int totalKeys = placedSamples.Count;
        int orphanedKeys = totalKeys - validSamples;
        
        string stats = "=== 已放置样本跟踪器统计 ===\n";
        stats += $"有效样本: {validSamples}\n";
        stats += $"孤立引用: {orphanedKeys}\n";
        stats += $"缓存数据: {sampleDataCache.Count}\n";
        stats += $"当前场景: {SceneManager.GetActiveScene().name}\n";
        stats += $"自动清理: {autoCleanupOnSceneChange}\n";
        
        if (validSamples > 0)
        {
            stats += "\n当前样本:\n";
            foreach (var kvp in placedSamples)
            {
                if (kvp.Value != null)
                {
                    string sampleInfo = kvp.Value.name;
                    if (sampleDataCache.TryGetValue(kvp.Key, out SampleItem data))
                    {
                        sampleInfo += $" ({data.displayName})";
                    }
                    stats += $"- {sampleInfo}\n";
                }
            }
        }
        
        return stats;
    }
    
    /// <summary>
    /// 日志输出
    /// </summary>
    void LogMessage(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[PlacedSampleTracker] {message}");
        }
    }
    
    /// <summary>
    /// 清理资源
    /// </summary>
    void OnDestroy()
    {
        if (autoCleanupOnSceneChange)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }
    }
    
    /// <summary>
    /// 在Inspector中显示跟踪器状态
    /// </summary>
    [ContextMenu("显示跟踪器状态")]
    void ShowTrackerStatus()
    {
        Debug.Log(GetTrackerStats());
    }
    
    /// <summary>
    /// 强制清理孤立样本
    /// </summary>
    [ContextMenu("强制清理孤立样本")]
    void ForceCleanupOrphaned()
    {
        CleanupOrphanedSamples();
    }
    
    /// <summary>
    /// 清理所有样本
    /// </summary>
    [ContextMenu("清理所有样本")]
    void ClearAllSamples()
    {
        ClearAllPlacedSamples();
    }
}
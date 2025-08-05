using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Encyclopedia
{
    /// <summary>
    /// 收集进度数据
    /// 用于保存和加载
    /// </summary>
    [Serializable]
    public class CollectionSaveData
    {
        public string version = "1.0";
        public DateTime lastSaveTime;
        public List<DiscoveredEntry> discoveredEntries = new List<DiscoveredEntry>();
    }

    [Serializable]
    public class DiscoveredEntry
    {
        public string entryId;
        public DateTime firstDiscoveredTime;
        public int discoveryCount;
    }

    /// <summary>
    /// 收集统计信息
    /// </summary>
    [Serializable]
    public class CollectionStats
    {
        public int totalEntries = 0;
        public int discoveredEntries = 0;
        public int totalMinerals = 0;
        public int discoveredMinerals = 0;
        public int totalFossils = 0;
        public int discoveredFossils = 0;
        public float overallProgress = 0f;
        public float mineralProgress = 0f;
        public float fossilProgress = 0f;

        public Dictionary<string, LayerStats> layerStats = new Dictionary<string, LayerStats>();
    }

    [Serializable]
    public class LayerStats
    {
        public string layerName;
        public int totalEntries;
        public int discoveredEntries;
        public float progress;
    }

    /// <summary>
    /// 收集进度管理器
    /// 负责追踪玩家的发现进度并提供统计信息
    /// </summary>
    public class CollectionManager : MonoBehaviour
    {
        [Header("保存设置")]
        [SerializeField] private string saveFileName = "EncyclopediaProgress";
        [SerializeField] private bool autoSave = true;
        [SerializeField] private float autoSaveInterval = 30f;

        [Header("调试信息")]
        [SerializeField] private CollectionStats currentStats = new CollectionStats();

        // 事件
        public static event Action<EncyclopediaEntry> OnEntryDiscovered;
        public static event Action<CollectionStats> OnStatsUpdated;

        // 单例
        public static CollectionManager Instance { get; private set; }

        // 私有变量
        private float lastSaveTime;
        private bool isDirty = false;

        // 公共属性
        public CollectionStats CurrentStats => currentStats;

        private void Awake()
        {
            // 单例初始化
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            // 等待数据加载完成后初始化
            if (EncyclopediaData.Instance != null && EncyclopediaData.Instance.IsDataLoaded)
            {
                Initialize();
            }
            else
            {
                // 延迟初始化
                Invoke(nameof(Initialize), 1f);
            }
        }

        private void Update()
        {
            // 自动保存
            if (autoSave && isDirty && Time.time - lastSaveTime > autoSaveInterval)
            {
                SaveProgress();
            }
        }

        /// <summary>
        /// 初始化收集管理器
        /// </summary>
        private void Initialize()
        {
            if (EncyclopediaData.Instance == null || !EncyclopediaData.Instance.IsDataLoaded)
            {
                Debug.LogError("EncyclopediaData未加载，无法初始化CollectionManager");
                return;
            }

            Debug.Log("初始化收集进度管理器...");

            // 加载保存的进度
            LoadProgress();

            // 更新统计信息
            UpdateStats();

            Debug.Log($"收集进度初始化完成: {currentStats.discoveredEntries}/{currentStats.totalEntries} ({currentStats.overallProgress:P1})");
        }

        /// <summary>
        /// 记录发现条目
        /// </summary>
        public void DiscoverEntry(string entryId)
        {
            var entry = EncyclopediaData.Instance.GetEntryById(entryId);
            if (entry == null)
            {
                Debug.LogWarning($"未找到条目: {entryId}");
                return;
            }

            // 标记为已发现
            bool wasNewDiscovery = !entry.isDiscovered;
            entry.MarkAsDiscovered();

            if (wasNewDiscovery)
            {
                Debug.Log($"新发现: {entry.GetFormattedDisplayName()}");
                OnEntryDiscovered?.Invoke(entry);
            }

            // 更新统计信息
            UpdateStats();

            // 标记需要保存
            isDirty = true;
        }

        /// <summary>
        /// 批量发现条目（用于测试）
        /// </summary>
        public void DiscoverEntries(List<string> entryIds)
        {
            foreach (string id in entryIds)
            {
                DiscoverEntry(id);
            }
        }

        /// <summary>
        /// 更新统计信息
        /// </summary>
        private void UpdateStats()
        {
            if (EncyclopediaData.Instance == null)
                return;

            var allEntries = EncyclopediaData.Instance.AllEntries.Values.ToList();
            var minerals = allEntries.Where(e => e.entryType == EntryType.Mineral).ToList();
            var fossils = allEntries.Where(e => e.entryType == EntryType.Fossil).ToList();

            currentStats = new CollectionStats
            {
                totalEntries = allEntries.Count,
                discoveredEntries = allEntries.Count(e => e.isDiscovered),
                totalMinerals = minerals.Count,
                discoveredMinerals = minerals.Count(e => e.isDiscovered),
                totalFossils = fossils.Count,
                discoveredFossils = fossils.Count(e => e.isDiscovered)
            };

            // 计算进度百分比
            currentStats.overallProgress = currentStats.totalEntries > 0 ? 
                (float)currentStats.discoveredEntries / currentStats.totalEntries : 0f;
            currentStats.mineralProgress = currentStats.totalMinerals > 0 ? 
                (float)currentStats.discoveredMinerals / currentStats.totalMinerals : 0f;
            currentStats.fossilProgress = currentStats.totalFossils > 0 ? 
                (float)currentStats.discoveredFossils / currentStats.totalFossils : 0f;

            // 更新各地层统计
            UpdateLayerStats();

            // 通知统计更新
            OnStatsUpdated?.Invoke(currentStats);
        }

        /// <summary>
        /// 更新地层统计信息
        /// </summary>
        private void UpdateLayerStats()
        {
            currentStats.layerStats.Clear();

            foreach (string layerName in EncyclopediaData.Instance.LayerNames)
            {
                var layerEntries = EncyclopediaData.Instance.GetEntriesByLayer(layerName);
                var layerStat = new LayerStats
                {
                    layerName = layerName,
                    totalEntries = layerEntries.Count,
                    discoveredEntries = layerEntries.Count(e => e.isDiscovered)
                };
                layerStat.progress = layerStat.totalEntries > 0 ? 
                    (float)layerStat.discoveredEntries / layerStat.totalEntries : 0f;

                currentStats.layerStats[layerName] = layerStat;
            }
        }

        /// <summary>
        /// 保存进度
        /// </summary>
        public void SaveProgress()
        {
            try
            {
                var saveData = new CollectionSaveData
                {
                    lastSaveTime = DateTime.Now
                };

                // 收集已发现的条目
                foreach (var entry in EncyclopediaData.Instance.AllEntries.Values)
                {
                    if (entry.isDiscovered)
                    {
                        saveData.discoveredEntries.Add(new DiscoveredEntry
                        {
                            entryId = entry.id,
                            firstDiscoveredTime = entry.firstDiscoveredTime,
                            discoveryCount = entry.discoveryCount
                        });
                    }
                }

                // 序列化并保存
                string json = JsonUtility.ToJson(saveData, true);
                string savePath = Application.persistentDataPath + "/" + saveFileName + ".json";
                System.IO.File.WriteAllText(savePath, json);

                lastSaveTime = Time.time;
                isDirty = false;

                Debug.Log($"收集进度已保存: {saveData.discoveredEntries.Count} 个已发现条目 -> {savePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"保存收集进度失败: {e.Message}");
            }
        }

        /// <summary>
        /// 加载进度
        /// </summary>
        private void LoadProgress()
        {
            try
            {
                string savePath = Application.persistentDataPath + "/" + saveFileName + ".json";
                
                if (!System.IO.File.Exists(savePath))
                {
                    Debug.Log("未找到保存文件，开始新的收集进度");
                    return;
                }

                string json = System.IO.File.ReadAllText(savePath);
                var saveData = JsonUtility.FromJson<CollectionSaveData>(json);

                if (saveData == null)
                {
                    Debug.LogWarning("保存文件解析失败");
                    return;
                }

                // 恢复发现状态
                int restoredCount = 0;
                foreach (var discoveredEntry in saveData.discoveredEntries)
                {
                    var entry = EncyclopediaData.Instance.GetEntryById(discoveredEntry.entryId);
                    if (entry != null)
                    {
                        entry.isDiscovered = true;
                        entry.firstDiscoveredTime = discoveredEntry.firstDiscoveredTime;
                        entry.discoveryCount = discoveredEntry.discoveryCount;
                        restoredCount++;
                    }
                }

                Debug.Log($"收集进度已加载: {restoredCount} 个已发现条目 (保存时间: {saveData.lastSaveTime})");
            }
            catch (Exception e)
            {
                Debug.LogError($"加载收集进度失败: {e.Message}");
            }
        }

        /// <summary>
        /// 重置所有进度（用于测试）
        /// </summary>
        [ContextMenu("重置收集进度")]
        public void ResetProgress()
        {
            if (EncyclopediaData.Instance == null)
                return;

            foreach (var entry in EncyclopediaData.Instance.AllEntries.Values)
            {
                entry.isDiscovered = false;
                entry.discoveryCount = 0;
            }

            UpdateStats();
            isDirty = true;

            Debug.Log("收集进度已重置");
        }

        /// <summary>
        /// 解锁所有条目（用于测试）
        /// </summary>
        [ContextMenu("解锁所有条目")]
        public void UnlockAllEntries()
        {
            if (EncyclopediaData.Instance == null)
                return;

            foreach (var entry in EncyclopediaData.Instance.AllEntries.Values)
            {
                entry.MarkAsDiscovered();
            }

            UpdateStats();
            isDirty = true;

            Debug.Log("所有条目已解锁");
        }

        /// <summary>
        /// 获取指定地层的统计信息
        /// </summary>
        public LayerStats GetLayerStats(string layerName)
        {
            return currentStats.layerStats.ContainsKey(layerName) ? 
                currentStats.layerStats[layerName] : null;
        }

        /// <summary>
        /// 检查条目是否已发现
        /// </summary>
        public bool IsEntryDiscovered(string entryId)
        {
            var entry = EncyclopediaData.Instance.GetEntryById(entryId);
            return entry?.isDiscovered ?? false;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && isDirty)
            {
                SaveProgress();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && isDirty)
            {
                SaveProgress();
            }
        }

        private void OnDestroy()
        {
            if (isDirty)
            {
                SaveProgress();
            }
        }
    }
}
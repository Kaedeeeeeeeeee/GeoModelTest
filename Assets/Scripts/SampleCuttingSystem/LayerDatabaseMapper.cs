using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace SampleCuttingSystem
{
    /// <summary>
    /// 地层数据库映射器
    /// 从SendaiMineralDatabase.json中查询地层对应的矿物信息
    /// </summary>
    public class LayerDatabaseMapper : MonoBehaviour
    {
        [Header("数据库设置")]
        [SerializeField] private string databasePath = "MineralData/Data/SendaiMineralDatabase";
        [SerializeField] private bool loadOnStart = true;
        [SerializeField] private bool enableDebugLogs = false;
        
        // 缓存的数据库
        private MineralDatabase cachedDatabase;
        
        // 地层名称映射表 (处理不同的命名方式)
        private Dictionary<string, string> layerNameMappings = new Dictionary<string, string>();
        
        void Start()
        {
            if (loadOnStart)
            {
                LoadDatabase();
                InitializeLayerNameMappings();
            }
        }
        
        /// <summary>
        /// 加载矿物数据库
        /// </summary>
        public bool LoadDatabase()
        {
            try
            {
                TextAsset databaseJson = Resources.Load<TextAsset>(databasePath);
                if (databaseJson == null)
                {
                    Debug.LogError($"无法加载矿物数据库: {databasePath}");
                    return false;
                }
                
                cachedDatabase = JsonUtility.FromJson<MineralDatabase>(databaseJson.text);
                
                if (cachedDatabase?.stratigraphicLayers == null)
                {
                    Debug.LogError("数据库格式错误：缺少stratigraphicLayers数据");
                    return false;
                }
                
                Debug.Log($"成功加载矿物数据库: {cachedDatabase.stratigraphicLayers.Length} 个地层");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"加载矿物数据库失败: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 初始化地层名称映射表
        /// </summary>
        private void InitializeLayerNameMappings()
        {
            layerNameMappings.Clear();
            
            if (cachedDatabase?.stratigraphicLayers == null) return;
            
            foreach (var layer in cachedDatabase.stratigraphicLayers)
            {
                if (string.IsNullOrEmpty(layer.layerName)) continue;
                
                // 添加不同语言版本的映射
                string baseLayerName = layer.layerName.ToLower().Trim();
                
                layerNameMappings[baseLayerName] = layer.layerId;
                
                if (!string.IsNullOrEmpty(layer.layerNameEN))
                    layerNameMappings[layer.layerNameEN.ToLower().Trim()] = layer.layerId;
                    
                if (!string.IsNullOrEmpty(layer.layerNameJA))
                    layerNameMappings[layer.layerNameJA.ToLower().Trim()] = layer.layerId;
                
                // 添加简化版本映射
                layerNameMappings[baseLayerName.Replace("层", "").Replace("formation", "")] = layer.layerId;
                layerNameMappings[baseLayerName.Replace(" ", "")] = layer.layerId;
            }
            
            if (enableDebugLogs)
            {
                Debug.Log($"初始化地层名称映射: {layerNameMappings.Count} 个映射");
            }
        }
        
        /// <summary>
        /// 根据地层名称获取矿物成分信息
        /// </summary>
        public MineralComposition[] GetMineralsForLayer(string layerName)
        {
            if (cachedDatabase == null && !LoadDatabase())
            {
                Debug.LogError("数据库未加载，无法查询矿物信息");
                return new MineralComposition[0];
            }
            
            if (string.IsNullOrEmpty(layerName))
            {
                Debug.LogWarning("地层名称为空，无法查询");
                return new MineralComposition[0];
            }
            
            // 查找匹配的地层
            var matchedLayer = FindMatchingLayer(layerName);
            if (matchedLayer == null)
            {
                Debug.LogWarning($"未找到匹配的地层: {layerName}");
                return CreateDefaultMinerals(layerName);
            }
            
            // 提取矿物信息
            var minerals = new List<MineralComposition>();
            
            if (matchedLayer.rockTypes != null)
            {
                foreach (var rockType in matchedLayer.rockTypes)
                {
                    if (rockType?.minerals != null)
                    {
                        foreach (var mineral in rockType.minerals)
                        {
                            if (mineral != null)
                            {
                                var composition = ConvertToMineralComposition(mineral);
                                minerals.Add(composition);
                            }
                        }
                    }
                }
            }
            
            if (enableDebugLogs)
            {
                Debug.Log($"地层 '{layerName}' 找到 {minerals.Count} 种矿物");
            }
            
            return minerals.ToArray();
        }
        
        /// <summary>
        /// 查找匹配的地层
        /// </summary>
        private StratigraphicLayer FindMatchingLayer(string layerName)
        {
            if (cachedDatabase?.stratigraphicLayers == null) return null;
            
            string searchName = layerName.ToLower().Trim();
            
            // 1. 直接匹配
            var directMatch = cachedDatabase.stratigraphicLayers.FirstOrDefault(layer =>
                layer.layerName?.ToLower().Trim() == searchName ||
                layer.layerNameEN?.ToLower().Trim() == searchName ||
                layer.layerNameJA?.ToLower().Trim() == searchName);
                
            if (directMatch != null) return directMatch;
            
            // 2. 使用映射表查找
            if (layerNameMappings.TryGetValue(searchName, out string layerId))
            {
                var mappedMatch = cachedDatabase.stratigraphicLayers.FirstOrDefault(layer => 
                    layer.layerId == layerId);
                if (mappedMatch != null) return mappedMatch;
            }
            
            // 3. 模糊匹配
            var fuzzyMatch = cachedDatabase.stratigraphicLayers.FirstOrDefault(layer =>
                layer.layerName?.ToLower().Contains(searchName) == true ||
                searchName.Contains(layer.layerName?.ToLower() ?? ""));
                
            return fuzzyMatch;
        }
        
        /// <summary>
        /// 转换数据库中的矿物数据为组件格式
        /// </summary>
        private MineralComposition ConvertToMineralComposition(DatabaseMineral dbMineral)
        {
            return new MineralComposition
            {
                mineralId = dbMineral.mineralId,
                mineralName = dbMineral.mineralName,
                mineralNameEN = dbMineral.mineralNameEN,
                mineralNameJA = dbMineral.mineralNameJA,
                percentage = dbMineral.percentage,
                imageFile = dbMineral.properties?.imageFile,
                modelFile = dbMineral.properties?.modelFile,
                properties = ConvertMineralProperties(dbMineral.properties)
            };
        }
        
        /// <summary>
        /// 转换矿物属性
        /// </summary>
        private MineralProperties ConvertMineralProperties(DatabaseMineralProperties dbProps)
        {
            if (dbProps == null) return new MineralProperties();
            
            return new MineralProperties
            {
                mohsHardness = dbProps.mohsHardness,
                acidReaction = dbProps.acidReaction,
                uvFluorescence = dbProps.uvFluorescence,
                magnetism = dbProps.magnetism,
                density = dbProps.density,
                polarizedColor = dbProps.polarizedColor,
                appearance = dbProps.appearance
            };
        }
        
        /// <summary>
        /// 创建默认矿物成分（当找不到数据库记录时）
        /// </summary>
        private MineralComposition[] CreateDefaultMinerals(string layerName)
        {
            // 根据地层名称推测可能的矿物组成
            var defaultMinerals = new List<MineralComposition>();
            
            // 简单的规则推测
            if (layerName.Contains("砂") || layerName.Contains("sand"))
            {
                defaultMinerals.Add(new MineralComposition
                {
                    mineralId = "quartz_default",
                    mineralName = "石英",
                    percentage = 0.7f,
                    imageFile = "quartz_001.jpg",
                    modelFile = "quartz_001.glb"
                });
            }
            else if (layerName.Contains("页") || layerName.Contains("泥") || layerName.Contains("clay"))
            {
                defaultMinerals.Add(new MineralComposition
                {
                    mineralId = "clay_default",
                    mineralName = "粘土矿物",
                    percentage = 0.8f,
                    imageFile = "clay_minerals_001.jpg",
                    modelFile = "clay_minerals_001.glb"
                });
            }
            else if (layerName.Contains("石灰") || layerName.Contains("limestone"))
            {
                defaultMinerals.Add(new MineralComposition
                {
                    mineralId = "calcite_default",
                    mineralName = "方解石",
                    percentage = 0.9f
                });
            }
            else
            {
                // 通用默认矿物
                defaultMinerals.Add(new MineralComposition
                {
                    mineralId = "mixed_default",
                    mineralName = "混合矿物",
                    percentage = 1.0f
                });
            }
            
            Debug.LogWarning($"使用默认矿物组成for地层: {layerName}");
            return defaultMinerals.ToArray();
        }
        
        /// <summary>
        /// 获取数据库中所有可用的地层列表
        /// </summary>
        public string[] GetAvailableLayers()
        {
            if (cachedDatabase?.stratigraphicLayers == null)
                return new string[0];
                
            return cachedDatabase.stratigraphicLayers
                .Where(layer => !string.IsNullOrEmpty(layer.layerName))
                .Select(layer => layer.layerName)
                .ToArray();
        }
        
        /// <summary>
        /// 检查指定地层是否在数据库中存在
        /// </summary>
        public bool IsLayerInDatabase(string layerName)
        {
            return FindMatchingLayer(layerName) != null;
        }
        
        /// <summary>
        /// 获取地层的详细信息
        /// </summary>
        public LayerDetailInfo GetLayerDetails(string layerName)
        {
            var layer = FindMatchingLayer(layerName);
            if (layer == null) return null;
            
            return new LayerDetailInfo
            {
                layerId = layer.layerId,
                layerName = layer.layerName,
                layerNameEN = layer.layerNameEN,
                layerNameJA = layer.layerNameJA,
                rockTypeCount = layer.rockTypes?.Length ?? 0,
                totalMineralCount = layer.rockTypes?.Sum(rt => rt.minerals?.Length ?? 0) ?? 0
            };
        }
        
        // 数据结构定义
        [System.Serializable]
        public class MineralDatabase
        {
            public string version;
            public string lastUpdated;
            public string description;
            public StratigraphicLayer[] stratigraphicLayers;
        }
        
        [System.Serializable]
        public class StratigraphicLayer
        {
            public string layerId;
            public string layerName;
            public string layerNameEN;
            public string layerNameJA;
            public RockType[] rockTypes;
        }
        
        [System.Serializable]
        public class RockType
        {
            public string rockId;
            public string rockName;
            public string rockNameEN;
            public string rockNameJA;
            public DatabaseMineral[] minerals;
        }
        
        [System.Serializable]
        public class DatabaseMineral
        {
            public string mineralId;
            public string mineralName;
            public string mineralNameEN;
            public string mineralNameJA;
            public float percentage;
            public DatabaseMineralProperties properties;
        }
        
        [System.Serializable]
        public class DatabaseMineralProperties
        {
            public string mohsHardness;
            public bool acidReaction;
            public string uvFluorescence;
            public string magnetism;
            public string density;
            public string polarizedColor;
            public string appearance;
            public string imageFile;
            public string modelFile;
        }
        
        [System.Serializable]
        public class LayerDetailInfo
        {
            public string layerId;
            public string layerName;
            public string layerNameEN;
            public string layerNameJA;
            public int rockTypeCount;
            public int totalMineralCount;
        }
        
        /// <summary>
        /// Editor调试：显示数据库信息
        /// </summary>
        [ContextMenu("显示数据库信息")]
        private void ShowDatabaseInfo()
        {
            if (cachedDatabase == null && !LoadDatabase()) return;
            
            Debug.Log($"=== 矿物数据库信息 ===");
            Debug.Log($"版本: {cachedDatabase.version}");
            Debug.Log($"更新时间: {cachedDatabase.lastUpdated}");
            Debug.Log($"地层数量: {cachedDatabase.stratigraphicLayers?.Length ?? 0}");
            
            if (cachedDatabase.stratigraphicLayers != null)
            {
                foreach (var layer in cachedDatabase.stratigraphicLayers)
                {
                    int mineralCount = layer.rockTypes?.Sum(rt => rt.minerals?.Length ?? 0) ?? 0;
                    Debug.Log($"- {layer.layerName} ({layer.layerId}): {mineralCount} 种矿物");
                }
            }
        }
        
        /// <summary>
        /// Editor调试：测试地层查询
        /// </summary>
        [ContextMenu("测试地层查询")]
        private void TestLayerQuery()
        {
            string[] testLayers = { "青葉山層", "Aobayama Formation", "青葉山", "砂岩层" };
            
            foreach (string layerName in testLayers)
            {
                var minerals = GetMineralsForLayer(layerName);
                Debug.Log($"测试查询 '{layerName}': 找到 {minerals.Length} 种矿物");
            }
        }
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;

namespace Encyclopedia
{
    /// <summary>
    /// 数据库原始结构类
    /// 用于JSON反序列化
    /// </summary>
    [Serializable]
    public class DatabaseRoot
    {
        public string version;
        public string lastUpdated;
        public string description;
        public StratigraphicLayer[] stratigraphicLayers;
    }

    [Serializable]
    public class StratigraphicLayer
    {
        public string layerId;
        public string layerName;
        public string layerNameEN;
        public string layerNameJA;
        public RockType[] rockTypes;
        public FossilData[] fossils;
    }

    [Serializable]
    public class RockType
    {
        public string rockId;
        public string rockName;
        public string rockNameEN;
        public string rockNameJA;
        public MineralData[] minerals;
    }

    [Serializable]
    public class MineralData
    {
        public string mineralId;
        public string mineralName;
        public string mineralNameEN;
        public string mineralNameJA;
        public float percentage;
        public MineralProperties properties;
    }

    [Serializable]
    public class FossilData
    {
        public string fossilId;
        public string fossilName;
        public string fossilNameEN;
        public string fossilNameJA;
        public string rarity;
        public float discoveryProbability;
        public FossilProperties properties;
    }

    [Serializable]
    public class MineralProperties
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

    [Serializable]
    public class FossilProperties
    {
        public string type;
        public string imageFile;
        public string modelFile;
        public string description;
    }

    /// <summary>
    /// 图鉴数据管理器
    /// 负责加载、解析和管理所有图鉴数据
    /// </summary>
    public class EncyclopediaData : MonoBehaviour
    {
        [Header("数据文件路径")]
        [SerializeField] private string databaseFileName = "SendaiMineralDatabase";
        [SerializeField] private string mineralImagePath = "MineralData/Images/Minerals/";
        [SerializeField] private string fossilImagePath = "MineralData/Images/Fossil/";
        [SerializeField] private string mineralModelPath = "MineralData/Models/Minerals/";
        [SerializeField] private string fossilModelPath = "MineralData/Models/Fossil/";

        [Header("数据状态")]
        [SerializeField] private bool isDataLoaded = false;
        [SerializeField] private int totalMinerals = 0;
        [SerializeField] private int totalFossils = 0;

        // 数据容器
        private DatabaseRoot database;
        private Dictionary<string, EncyclopediaEntry> allEntries = new Dictionary<string, EncyclopediaEntry>();
        private Dictionary<string, List<EncyclopediaEntry>> entriesByLayer = new Dictionary<string, List<EncyclopediaEntry>>();
        private List<string> layerNames = new List<string>();

        // 单例模式
        public static EncyclopediaData Instance { get; private set; }

        // 公共访问属性
        public bool IsDataLoaded => isDataLoaded;
        public int TotalMinerals => totalMinerals;
        public int TotalFossils => totalFossils;
        public List<string> LayerNames => layerNames;
        public Dictionary<string, EncyclopediaEntry> AllEntries => allEntries;

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

            // 加载数据
            LoadEncyclopediaData();
        }

        /// <summary>
        /// 加载图鉴数据
        /// </summary>
        private void LoadEncyclopediaData()
        {
            try
            {
                Debug.Log("[EncyclopediaData] 开始加载图鉴数据...");

                // 加载JSON数据
                TextAsset jsonFile = Resources.Load<TextAsset>($"MineralData/Data/{databaseFileName}");
                if (jsonFile == null)
                {
                    Debug.LogError($"[EncyclopediaData] 无法找到数据库文件: MineralData/Data/{databaseFileName}");
                    return;
                }
                Debug.Log($"[EncyclopediaData] 成功加载JSON文件: {databaseFileName}, 大小: {jsonFile.text.Length} 字符");

                // 解析JSON
                Debug.Log("[EncyclopediaData] 开始解析JSON数据...");
                database = JsonUtility.FromJson<DatabaseRoot>(jsonFile.text);
                if (database == null)
                {
                    Debug.LogError("[EncyclopediaData] JSON解析失败");
                    return;
                }

                Debug.Log($"[EncyclopediaData] 成功加载数据库 v{database.version}, 包含 {database.stratigraphicLayers?.Length ?? 0} 个地层");

                // 处理数据
                ProcessDatabaseData();

                // 加载资源
                LoadResources();

                isDataLoaded = true;
                #if UNITY_EDITOR
                Debug.Log($"图鉴数据加载完成! 矿物: {totalMinerals}, 化石: {totalFossils}");
                #endif
            }
            catch (Exception e)
            {
                Debug.LogError($"[EncyclopediaData] 加载图鉴数据失败: {e.Message}");
                Debug.LogError($"[EncyclopediaData] 堆栈跟踪: {e.StackTrace}");
            }
        }

        /// <summary>
        /// 处理数据库数据，转换为图鉴条目
        /// </summary>
        private void ProcessDatabaseData()
        {
            allEntries.Clear();
            entriesByLayer.Clear();
            layerNames.Clear();
            totalMinerals = 0;
            totalFossils = 0;

            foreach (var layer in database.stratigraphicLayers)
            {
                layerNames.Add(layer.layerName);
                entriesByLayer[layer.layerName] = new List<EncyclopediaEntry>();

                // 处理矿物数据
                if (layer.rockTypes != null)
                {
                    foreach (var rock in layer.rockTypes)
                    {
                        if (rock.minerals != null)
                        {
                            foreach (var mineral in rock.minerals)
                            {
                                var entry = CreateMineralEntry(layer, rock, mineral);
                                allEntries[entry.id] = entry;
                                entriesByLayer[layer.layerName].Add(entry);
                                totalMinerals++;
                            }
                        }
                    }
                }

                // 处理化石数据
                if (layer.fossils != null)
                {
                    foreach (var fossil in layer.fossils)
                    {
                        var entry = CreateFossilEntry(layer, fossil);
                        allEntries[entry.id] = entry;
                        entriesByLayer[layer.layerName].Add(entry);
                        totalFossils++;
                    }
                }
            }

            #if UNITY_EDITOR
            Debug.Log($"数据处理完成: {allEntries.Count} 个条目");
            #endif
        }

        /// <summary>
        /// 创建矿物图鉴条目
        /// </summary>
        private EncyclopediaEntry CreateMineralEntry(StratigraphicLayer layer, RockType rock, MineralData mineral)
        {
            var entry = new EncyclopediaEntry
            {
                id = $"{layer.layerId}_{rock.rockId}_{mineral.mineralId}",
                entryType = EntryType.Mineral,
                displayName = mineral.mineralName,
                nameEN = mineral.mineralNameEN,
                nameJA = mineral.mineralNameJA,
                nameCN = mineral.mineralName,

                layerName = layer.layerName,
                layerId = layer.layerId,
                rockName = rock.rockName,
                rockId = rock.rockId,

                percentage = mineral.percentage,

                description = mineral.properties.appearance,
                appearance = mineral.properties.appearance,

                mohsHardness = mineral.properties.mohsHardness,
                acidReaction = mineral.properties.acidReaction,
                uvFluorescence = mineral.properties.uvFluorescence,
                magnetism = mineral.properties.magnetism,
                density = mineral.properties.density,
                polarizedColor = mineral.properties.polarizedColor,

                imageFile = mineral.properties.imageFile,
                modelFile = mineral.properties.modelFile,

                rarity = Rarity.Common, // 矿物默认为常见
                discoveryProbability = mineral.percentage,

                isDiscovered = true,
                discoveryCount = 0
            };

            return entry;
        }

        /// <summary>
        /// 创建化石图鉴条目
        /// </summary>
        private EncyclopediaEntry CreateFossilEntry(StratigraphicLayer layer, FossilData fossil)
        {
            // 解析稀有度
            Rarity rarity = fossil.rarity.ToLower() switch
            {
                "common" => Rarity.Common,
                "uncommon" => Rarity.Uncommon,
                "rare" => Rarity.Rare,
                _ => Rarity.Common
            };

            var entry = new EncyclopediaEntry
            {
                id = $"{layer.layerId}_{fossil.fossilId}",
                entryType = EntryType.Fossil,
                displayName = fossil.fossilName,
                nameEN = fossil.fossilNameEN,
                nameJA = fossil.fossilNameJA,
                nameCN = fossil.fossilName,

                layerName = layer.layerName,
                layerId = layer.layerId,

                rarity = rarity,
                discoveryProbability = fossil.discoveryProbability,

                description = fossil.properties.description,

                imageFile = fossil.properties.imageFile,
                modelFile = fossil.properties.modelFile,

                isDiscovered = true,
                discoveryCount = 0
            };

            return entry;
        }

        /// <summary>
        /// 加载图片和模型资源
        /// </summary>
        private void LoadResources()
        {
            #if UNITY_EDITOR
            Debug.Log("开始加载资源文件...");
            #endif

            int loadedImages = 0;
            int loadedModels = 0;

            foreach (var entry in allEntries.Values)
            {
                // 加载图片
                if (!string.IsNullOrEmpty(entry.imageFile))
                {
                    string imagePath = entry.entryType == EntryType.Mineral ? 
                        mineralImagePath : fossilImagePath;
                    
                    // 尝试加载图片，Resources.Load不需要扩展名
                    string fileName = Path.GetFileNameWithoutExtension(entry.imageFile);
                    Sprite sprite = Resources.Load<Sprite>(imagePath + fileName);
                    
                    // 如果加载失败，尝试不同的路径格式
                    if (sprite == null)
                    {
                        // 尝试直接使用完整的imageFile名称
                        sprite = Resources.Load<Sprite>(imagePath + entry.imageFile.Replace(".jpg", "").Replace(".jpeg", "").Replace(".png", ""));
                    }
                    
                    if (sprite != null)
                    {
                        entry.icon = sprite;
                        loadedImages++;
                    }
                    else
                    {
                        Debug.LogWarning($"无法加载图片: {imagePath + fileName} (尝试了: {entry.imageFile})");
                    }
                }

                // 加载3D模型
                if (!string.IsNullOrEmpty(entry.modelFile))
                {
                    string modelPath = entry.entryType == EntryType.Mineral ? 
                        mineralModelPath : fossilModelPath;
                    
                    string fileName = Path.GetFileNameWithoutExtension(entry.modelFile);
                    GameObject model = Resources.Load<GameObject>(modelPath + fileName);
                    
                    if (model != null)
                    {
                        entry.model3D = model;
                        loadedModels++;
                    }
                    else
                    {
                        Debug.LogWarning($"无法加载模型: {modelPath + fileName}");
                    }
                }
            }

            // 为缺少3D模型的条目创建默认立方体
            int createdModels = CreateDefaultModelsForEmptyEntries();

            #if UNITY_EDITOR
            Debug.Log($"资源加载完成: 图片 {loadedImages}/{allEntries.Count}, 模型 {loadedModels + createdModels}/{allEntries.Count} (创建默认模型: {createdModels})");
            #endif
        }

        /// <summary>
        /// 为缺少3D模型的条目创建默认立方体
        /// </summary>
        private int CreateDefaultModelsForEmptyEntries()
        {
            int createdCount = 0;

            foreach (var entry in allEntries.Values)
            {
                if (entry.model3D == null)
                {
                    // 创建一个简单的立方体作为默认模型
                    GameObject defaultModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    defaultModel.name = $"DefaultModel_{entry.displayName}";

                    // 根据条目类型设置不同颜色
                    var renderer = defaultModel.GetComponent<Renderer>();
                    if (entry.entryType == EntryType.Mineral)
                    {
                        // 矿物使用随机明亮色
                        renderer.material.color = new Color(
                            UnityEngine.Random.Range(0.3f, 1f),
                            UnityEngine.Random.Range(0.3f, 1f),
                            UnityEngine.Random.Range(0.3f, 1f)
                        );
                    }
                    else // 化石
                    {
                        // 化石使用棕色系
                        renderer.material.color = new Color(
                            UnityEngine.Random.Range(0.4f, 0.8f),
                            UnityEngine.Random.Range(0.3f, 0.6f),
                            UnityEngine.Random.Range(0.2f, 0.4f)
                        );
                    }

                    entry.model3D = defaultModel;
                    createdCount++;

                    #if UNITY_EDITOR
                    Debug.Log($"创建默认模型: {entry.displayName} ({entry.entryType})");
                    #endif
                }
            }

            return createdCount;
        }

        /// <summary>
        /// 获取指定地层的所有条目
        /// </summary>
        public List<EncyclopediaEntry> GetEntriesByLayer(string layerName)
        {
            Debug.Log($"[EncyclopediaData] GetEntriesByLayer被调用，layerName: '{layerName}'");
            Debug.Log($"[EncyclopediaData] 可用地层: {string.Join(", ", entriesByLayer.Keys)}");

            if (entriesByLayer.ContainsKey(layerName))
            {
                var entries = entriesByLayer[layerName];
                Debug.Log($"[EncyclopediaData] 返回 {entries.Count} 个条目给地层 '{layerName}'");
                if (entries.Count > 0)
                {
                    Debug.Log($"[EncyclopediaData] 示例条目: {string.Join(", ", entries.Take(3).Select(e => $"{e.displayName}({e.layerName})"))}");
                }
                return entries;
            }
            else
            {
                Debug.LogWarning($"[EncyclopediaData] 地层 '{layerName}' 不存在于entriesByLayer中");
                return new List<EncyclopediaEntry>();
            }
        }

        /// <summary>
        /// 根据ID获取条目
        /// </summary>
        public EncyclopediaEntry GetEntryById(string id)
        {
            return allEntries.ContainsKey(id) ? allEntries[id] : null;
        }

        /// <summary>
        /// 获取所有矿物条目
        /// </summary>
        public List<EncyclopediaEntry> GetAllMinerals()
        {
            return allEntries.Values.Where(e => e.entryType == EntryType.Mineral).ToList();
        }

        /// <summary>
        /// 获取所有化石条目
        /// </summary>
        public List<EncyclopediaEntry> GetAllFossils()
        {
            return allEntries.Values.Where(e => e.entryType == EntryType.Fossil).ToList();
        }

        /// <summary>
        /// 筛选条目
        /// </summary>
        public List<EncyclopediaEntry> FilterEntries(string layerName = null, EntryType? entryType = null, Rarity? rarity = null)
        {
            var filtered = allEntries.Values.AsEnumerable();

            if (!string.IsNullOrEmpty(layerName))
                filtered = filtered.Where(e => e.layerName == layerName);

            if (entryType.HasValue)
                filtered = filtered.Where(e => e.entryType == entryType.Value);

            if (rarity.HasValue)
                filtered = filtered.Where(e => e.rarity == rarity.Value);

            return filtered.ToList();
        }
    }
}
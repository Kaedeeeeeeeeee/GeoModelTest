using System;
using System.Collections;
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

        // Only resources explicitly requested by a visible consumer are retained.
        private readonly HashSet<EncyclopediaEntry> requestedIcons = new HashSet<EncyclopediaEntry>();
        private readonly HashSet<EncyclopediaEntry> requestedModels = new HashSet<EncyclopediaEntry>();
        private readonly Dictionary<EncyclopediaEntry, GameObject> fallbackModels = new Dictionary<EncyclopediaEntry, GameObject>();
        private readonly Dictionary<EncyclopediaEntry, Material> fallbackMaterials = new Dictionary<EncyclopediaEntry, Material>();
        private Transform fallbackRoot;
        private bool unloadPending;

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

                // Metadata is sufficient for collection, filters and the text-only lists.
                // Images and models are loaded only when a visible consumer requests them.

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
        /// Load only the image requested by a consumer. Text-only lists do not call this.
        /// A missing image is cached until release to avoid repeated resource lookups.
        /// </summary>
        public Sprite LoadEntryIcon(EncyclopediaEntry entry)
        {
            if (entry == null || entry.icon != null || !requestedIcons.Add(entry))
            {
                return entry?.icon;
            }

            if (!string.IsNullOrEmpty(entry.imageFile))
            {
                string root = entry.entryType == EntryType.Mineral ? mineralImagePath : fossilImagePath;
                entry.icon = Resources.Load<Sprite>(root + Path.GetFileNameWithoutExtension(entry.imageFile));
            }
            return entry.icon;
        }

        /// <summary>
        /// Load one requested detail model; its consumer releases the previous entry when switching.
        /// Model materials load their own texture dependencies; the unused standalone photo does not.
        /// </summary>
        public GameObject LoadEntryModel(EncyclopediaEntry entry)
        {
            if (entry == null || entry.model3D != null || !requestedModels.Add(entry))
            {
                return entry?.model3D;
            }

            if (!string.IsNullOrEmpty(entry.modelFile))
            {
                string root = entry.entryType == EntryType.Mineral ? mineralModelPath : fossilModelPath;
                entry.model3D = Resources.Load<GameObject>(root + Path.GetFileNameWithoutExtension(entry.modelFile));
            }

            if (entry.model3D == null)
            {
                entry.model3D = CreateDefaultModel(entry);
            }
            return entry.model3D;
        }

        private GameObject CreateDefaultModel(EncyclopediaEntry entry)
        {
            if (fallbackRoot == null)
            {
                var root = new GameObject("EncyclopediaFallbackModels");
                root.transform.SetParent(transform, false);
                root.SetActive(false);
                fallbackRoot = root.transform;
            }

            // Keep the template inactive in the scene, but activeSelf true so its preview clone is visible.
            var model = GameObject.CreatePrimitive(PrimitiveType.Cube);
            model.name = $"DefaultModel_{entry.displayName}";
            model.transform.SetParent(fallbackRoot, false);
            var collider = model.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            var material = model.GetComponent<Renderer>().material;
            material.color = entry.entryType == EntryType.Mineral
                ? new Color(0.55f, 0.75f, 0.85f)
                : new Color(0.6f, 0.45f, 0.3f);
            fallbackModels[entry] = model;
            fallbackMaterials[entry] = material;
            return model;
        }

        /// <summary>
        /// Call after clearing the consumer's image/preview. Shared assets are not forcibly unloaded:
        /// Unity retains anything still referenced by another live preview or gameplay object.
        /// </summary>
        public void ReleaseEntryResources(EncyclopediaEntry entry)
        {
            if (entry == null) return;
            bool hadResources = entry.icon != null || entry.model3D != null;
            entry.icon = null;
            entry.model3D = null;
            requestedIcons.Remove(entry);
            requestedModels.Remove(entry);
            DestroyFallback(entry);

            if (hadResources && !unloadPending && isActiveAndEnabled)
            {
                unloadPending = true;
                StartCoroutine(UnloadReleasedResources());
            }
        }

        private IEnumerator UnloadReleasedResources()
        {
            // Allow deferred preview/template destruction to complete before scanning references.
            yield return null;
            yield return Resources.UnloadUnusedAssets();
            unloadPending = false;
        }

        private void DestroyFallback(EncyclopediaEntry entry)
        {
            if (fallbackModels.TryGetValue(entry, out var model))
            {
                if (model != null) Destroy(model);
                fallbackModels.Remove(entry);
            }
            if (fallbackMaterials.TryGetValue(entry, out var material))
            {
                if (material != null) Destroy(material);
                fallbackMaterials.Remove(entry);
            }
        }

        private void OnDestroy()
        {
            foreach (var material in fallbackMaterials.Values)
            {
                if (material != null) Destroy(material);
            }
            fallbackMaterials.Clear();
            fallbackModels.Clear();
            if (Instance == this) Instance = null;
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
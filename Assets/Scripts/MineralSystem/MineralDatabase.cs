using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

namespace MineralSystem
{
    [System.Serializable]
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

    [System.Serializable]
    public class MineralData
    {
        public string mineralId;
        public string mineralName;
        public string mineralNameEN;
        public string mineralNameJA;
        public float percentage;
        public MineralProperties properties;
    }

    [System.Serializable]
    public class RockType
    {
        public string rockId;
        public string rockName;
        public string rockNameEN;
        public string rockNameJA;
        public List<MineralData> minerals;
    }

    [System.Serializable]
    public class StratigraphicLayer
    {
        public string layerId;
        public string layerName;
        public string layerNameEN;
        public string layerNameJA;
        public List<RockType> rockTypes;
    }

    [System.Serializable]
    public class SendaiMineralDatabase
    {
        public string version;
        public string lastUpdated;
        public string description;
        public List<StratigraphicLayer> stratigraphicLayers;
    }

    public class MineralDatabase : MonoBehaviour
    {
        [Header("Database Settings")]
        public string databaseFileName = "SendaiMineralDatabase.json";
        
        [Header("Resource Paths")]
        public string mineralImagesPath = "Images/Minerals/";
        public string mineralModelsPath = "Models/Minerals/";
        
        private SendaiMineralDatabase mineralDatabase;
        private Dictionary<string, MineralData> mineralLookup;
        private Dictionary<string, StratigraphicLayer> layerLookup;
        private Dictionary<string, RockType> rockLookup;
        
        public static MineralDatabase Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadDatabase();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void LoadDatabase()
        {
            string dataPath = Path.Combine(Application.streamingAssetsPath, "MineralData", databaseFileName);
            
            if (!File.Exists(dataPath))
            {
                dataPath = Path.Combine(Application.dataPath, "MineralData", "Data", databaseFileName);
            }
            
            if (File.Exists(dataPath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(dataPath);
                    mineralDatabase = JsonConvert.DeserializeObject<SendaiMineralDatabase>(jsonContent);
                    BuildLookupTables();
                    Debug.Log($"矿物数据库加载成功: {mineralDatabase.stratigraphicLayers.Count} 个地层");
                }
                catch (Exception e)
                {
                    Debug.LogError($"加载矿物数据库失败: {e.Message}");
                }
            }
            else
            {
                Debug.LogError($"未找到矿物数据库文件: {dataPath}");
            }
        }
        
        private void BuildLookupTables()
        {
            mineralLookup = new Dictionary<string, MineralData>();
            layerLookup = new Dictionary<string, StratigraphicLayer>();
            rockLookup = new Dictionary<string, RockType>();
            
            foreach (var layer in mineralDatabase.stratigraphicLayers)
            {
                layerLookup[layer.layerId] = layer;
                
                foreach (var rock in layer.rockTypes)
                {
                    rockLookup[rock.rockId] = rock;
                    
                    foreach (var mineral in rock.minerals)
                    {
                        mineralLookup[mineral.mineralId] = mineral;
                    }
                }
            }
        }
        
        public SendaiMineralDatabase GetDatabase()
        {
            return mineralDatabase;
        }
        
        public MineralData GetMineral(string mineralId)
        {
            mineralLookup.TryGetValue(mineralId, out MineralData mineral);
            return mineral;
        }
        
        public StratigraphicLayer GetLayer(string layerId)
        {
            layerLookup.TryGetValue(layerId, out StratigraphicLayer layer);
            return layer;
        }
        
        public RockType GetRock(string rockId)
        {
            rockLookup.TryGetValue(rockId, out RockType rock);
            return rock;
        }
        
        public List<MineralData> GetMineralsInLayer(string layerId)
        {
            var minerals = new List<MineralData>();
            var layer = GetLayer(layerId);
            
            if (layer != null)
            {
                foreach (var rock in layer.rockTypes)
                {
                    minerals.AddRange(rock.minerals);
                }
            }
            
            return minerals;
        }
        
        public List<MineralData> GetMineralsInRock(string rockId)
        {
            var rock = GetRock(rockId);
            return rock?.minerals ?? new List<MineralData>();
        }
        
        public List<MineralData> SearchMineralsByName(string searchTerm, bool useEnglish = false)
        {
            var results = new List<MineralData>();
            
            foreach (var mineral in mineralLookup.Values)
            {
                string nameToSearch = useEnglish ? mineral.mineralNameEN : mineral.mineralName;
                if (nameToSearch.Contains(searchTerm))
                {
                    results.Add(mineral);
                }
            }
            
            return results;
        }
        
        public Sprite GetMineralImage(string mineralId)
        {
            var mineral = GetMineral(mineralId);
            if (mineral != null && !string.IsNullOrEmpty(mineral.properties.imageFile))
            {
                string imagePath = mineralImagesPath + mineral.properties.imageFile;
                return Resources.Load<Sprite>(imagePath);
            }
            return null;
        }
        
        public GameObject GetMineralModel(string mineralId)
        {
            var mineral = GetMineral(mineralId);
            if (mineral != null && !string.IsNullOrEmpty(mineral.properties.modelFile))
            {
                // GLB 文件直接使用完整路径（包含扩展名）
                string modelPath = mineralModelsPath + mineral.properties.modelFile;
                return Resources.Load<GameObject>(modelPath.Replace(".glb", ""));
            }
            return null;
        }
        
        public List<MineralData> GetAllMinerals()
        {
            return new List<MineralData>(mineralLookup.Values);
        }
        
        public List<StratigraphicLayer> GetAllLayers()
        {
            return mineralDatabase?.stratigraphicLayers ?? new List<StratigraphicLayer>();
        }
        
        public int GetTotalMineralCount()
        {
            return mineralLookup.Count;
        }
        
        public int GetTotalLayerCount()
        {
            return layerLookup.Count;
        }
        
        public int GetTotalRockCount()
        {
            return rockLookup.Count;
        }
        
        public string GetLocalizedMineralName(string mineralId, string language = "zh")
        {
            var mineral = GetMineral(mineralId);
            if (mineral == null) return "";
            
            return language.ToLower() switch
            {
                "en" => mineral.mineralNameEN,
                "ja" => mineral.mineralNameJA,
                _ => mineral.mineralName
            };
        }
        
        public string GetLocalizedLayerName(string layerId, string language = "zh")
        {
            var layer = GetLayer(layerId);
            if (layer == null) return "";
            
            return language.ToLower() switch
            {
                "en" => layer.layerNameEN,
                "ja" => layer.layerNameJA,
                _ => layer.layerName
            };
        }
        
        public List<MineralData> GetMineralsByHardness(float minHardness, float maxHardness)
        {
            var results = new List<MineralData>();
            
            foreach (var mineral in mineralLookup.Values)
            {
                if (TryParseHardnessRange(mineral.properties.mohsHardness, out float min, out float max))
                {
                    if ((min >= minHardness && min <= maxHardness) || 
                        (max >= minHardness && max <= maxHardness))
                    {
                        results.Add(mineral);
                    }
                }
            }
            
            return results;
        }
        
        private bool TryParseHardnessRange(string hardnessStr, out float min, out float max)
        {
            min = max = 0f;
            
            if (string.IsNullOrEmpty(hardnessStr)) return false;
            
            if (hardnessStr.Contains("–") || hardnessStr.Contains("-"))
            {
                var parts = hardnessStr.Split(new char[] { '–', '-' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    return float.TryParse(parts[0].Trim(), out min) && 
                           float.TryParse(parts[1].Trim(), out max);
                }
            }
            else
            {
                if (float.TryParse(hardnessStr.Trim(), out min))
                {
                    max = min;
                    return true;
                }
            }
            
            return false;
        }
        
        public List<MineralData> GetMagneticMinerals()
        {
            var results = new List<MineralData>();
            
            foreach (var mineral in mineralLookup.Values)
            {
                if (mineral.properties.magnetism.Contains("磁性") && 
                    !mineral.properties.magnetism.Contains("无") &&
                    !mineral.properties.magnetism.Contains("抗磁"))
                {
                    results.Add(mineral);
                }
            }
            
            return results;
        }
        
        public List<MineralData> GetAcidReactiveMinerals()
        {
            var results = new List<MineralData>();
            
            foreach (var mineral in mineralLookup.Values)
            {
                if (mineral.properties.acidReaction)
                {
                    results.Add(mineral);
                }
            }
            
            return results;
        }
    }
}
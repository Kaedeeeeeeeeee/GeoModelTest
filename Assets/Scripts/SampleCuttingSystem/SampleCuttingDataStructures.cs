using UnityEngine;
using System.Collections.Generic;

namespace SampleCuttingSystem
{
    /// <summary>
    /// 样本切割系统的数据结构定义
    /// </summary>
    
    /// <summary>
    /// 切割后的单层样本数据
    /// </summary>
    [System.Serializable]
    public class SingleLayerSample
    {
        [Header("基本信息")]
        public string sampleID;              // 样本唯一ID
        public string layerName;             // 地层名称
        public string originalSampleID;      // 原始样本ID（追踪来源）
        
        [Header("3D对象")]
        public GameObject sampleObject;      // 3D显示对象
        public Mesh sampleMesh;             // 几何体网格
        public Material sampleMaterial;     // 样本材质
        
        [Header("地层信息")]
        public Color layerColor;            // 地层颜色
        public float originalThickness;     // 在原始样本中的厚度
        public float originalStartDepth;    // 在原始样本中的起始深度
        public float originalEndDepth;      // 在原始样本中的结束深度
        
        [Header("矿物成分")]
        public MineralComposition[] minerals; // 矿物组成列表
        public bool hasMineralData;         // 是否包含矿物数据
        
        [Header("切割信息")]
        public bool isCutFromMultiLayer;    // 是否来自多层样本切割
        public System.DateTime cuttingTime; // 切割时间
        public Vector3 cuttingPosition;     // 切割位置（用于生成样本时定位）
        
        [Header("物理属性")]
        public float estimatedMass;         // 估算质量
        public float estimatedVolume;       // 估算体积
        public Vector3 centerOfMass;        // 质心位置
        
        /// <summary>
        /// 生成唯一的样本ID
        /// </summary>
        public static string GenerateSampleID(string layerName, string originalID)
        {
            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string cleanLayerName = layerName.Replace(" ", "_").Replace("层", "");
            return $"Cut_{cleanLayerName}_{originalID}_{timestamp}";
        }
        
        /// <summary>
        /// 验证样本数据完整性
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(sampleID) &&
                   !string.IsNullOrEmpty(layerName) &&
                   sampleObject != null &&
                   minerals != null;
        }
        
        /// <summary>
        /// 获取主要矿物（含量最高的矿物）
        /// </summary>
        public MineralComposition GetPrimaryMineral()
        {
            if (minerals == null || minerals.Length == 0)
                return null;
                
            MineralComposition primary = minerals[0];
            foreach (var mineral in minerals)
            {
                if (mineral.percentage > primary.percentage)
                    primary = mineral;
            }
            return primary;
        }
        
        /// <summary>
        /// 获取样本描述
        /// </summary>
        public string GetDescription()
        {
            if (minerals == null || minerals.Length == 0)
                return $"{layerName}样本";
                
            var primary = GetPrimaryMineral();
            return $"{layerName}样本 (主要矿物: {primary.mineralName} {primary.percentage:P0})";
        }
    }
    
    /// <summary>
    /// 矿物成分数据
    /// </summary>
    [System.Serializable]
    public class MineralComposition
    {
        [Header("基本信息")]
        public string mineralId;            // 矿物ID
        public string mineralName;          // 中文名称
        public string mineralNameEN;        // 英文名称
        public string mineralNameJA;        // 日文名称
        
        [Header("含量信息")]
        [Range(0f, 1f)]
        public float percentage;            // 含量百分比 (0-1)
        
        [Header("资源文件")]
        public string imageFile;            // 矿物图片文件名
        public string modelFile;            // 3D模型文件名
        public Texture2D cachedImage;       // 缓存的图片
        public GameObject cachedModel;      // 缓存的3D模型
        
        [Header("物理和化学属性")]
        public MineralProperties properties; // 详细属性
        
        /// <summary>
        /// 获取显示名称（根据当前语言）
        /// </summary>
        public string GetDisplayName()
        {
            // 这里可以根据本地化系统获取当前语言
            // 暂时返回中文名称
            return !string.IsNullOrEmpty(mineralName) ? mineralName : mineralId;
        }
        
        /// <summary>
        /// 加载矿物图片
        /// </summary>
        public Texture2D LoadMineralImage()
        {
            if (cachedImage != null) return cachedImage;
            
            if (string.IsNullOrEmpty(imageFile)) return null;
            
            // 尝试从Resources加载
            string imagePath = $"MineralData/Images/Minerals/{imageFile}";
            cachedImage = Resources.Load<Texture2D>(imagePath);
            
            if (cachedImage == null)
            {
                Debug.LogWarning($"无法加载矿物图片: {imagePath}");
            }
            
            return cachedImage;
        }
        
        /// <summary>
        /// 加载矿物3D模型
        /// </summary>
        public GameObject LoadMineralModel()
        {
            if (cachedModel != null) return cachedModel;
            
            if (string.IsNullOrEmpty(modelFile)) return null;
            
            // 尝试从Resources加载
            string modelPath = $"MineralData/Models/Minerals/{modelFile}";
            cachedModel = Resources.Load<GameObject>(modelPath);
            
            if (cachedModel == null)
            {
                Debug.LogWarning($"无法加载矿物模型: {modelPath}");
            }
            
            return cachedModel;
        }
    }
    
    /// <summary>
    /// 矿物物理和化学属性
    /// </summary>
    [System.Serializable]
    public class MineralProperties
    {
        [Header("物理属性")]
        public string mohsHardness;         // 莫氏硬度
        public string density;              // 密度
        public string magnetism;            // 磁性
        public string appearance;           // 外观描述
        
        [Header("化学属性")]
        public bool acidReaction;           // 酸反应
        public string uvFluorescence;       // 紫外荧光
        public string polarizedColor;       // 偏光颜色
        
        [Header("其他属性")]
        public string crystallSystem;       // 晶系
        public string cleavage;            // 解理
        public string fracture;            // 断口
        public string luster;              // 光泽
        
        /// <summary>
        /// 获取属性摘要
        /// </summary>
        public string GetSummary()
        {
            var summary = new List<string>();
            
            if (!string.IsNullOrEmpty(mohsHardness))
                summary.Add($"硬度: {mohsHardness}");
                
            if (!string.IsNullOrEmpty(density))
                summary.Add($"密度: {density}");
                
            if (!string.IsNullOrEmpty(magnetism) && magnetism != "无")
                summary.Add($"磁性: {magnetism}");
                
            return string.Join(", ", summary);
        }
    }
    
    /// <summary>
    /// 切割操作记录
    /// </summary>
    [System.Serializable]
    public class CuttingRecord
    {
        public string originalSampleID;     // 原始样本ID
        public System.DateTime cuttingTime; // 切割时间
        public int totalLayers;            // 原始样本总层数
        public int successfulCuts;         // 成功切割次数
        public int failedCuts;             // 失败切割次数
        public string[] resultingSampleIDs; // 生成的样本ID列表
        public Vector3 cuttingPosition;    // 切割位置
        
        /// <summary>
        /// 切割是否完全成功
        /// </summary>
        public bool IsCompletelySuccessful()
        {
            return failedCuts == 0 && successfulCuts == (totalLayers - 1);
        }
        
        /// <summary>
        /// 获取成功率
        /// </summary>
        public float GetSuccessRate()
        {
            int totalAttempts = successfulCuts + failedCuts;
            return totalAttempts > 0 ? (float)successfulCuts / totalAttempts : 0f;
        }
    }
    
    /// <summary>
    /// 切割台状态数据
    /// </summary>
    [System.Serializable]
    public class CuttingStationState
    {
        public bool isOccupied;             // 是否被占用
        public string currentSampleID;      // 当前样本ID
        public CuttingPhase currentPhase;   // 当前阶段
        public float operationProgress;     // 操作进度 (0-1)
        public System.DateTime sessionStartTime; // 会话开始时间
        
        public enum CuttingPhase
        {
            Idle,           // 空闲
            Loading,        // 加载样本
            Analyzing,      // 分析样本
            Cutting,        // 切割中
            Success,        // 成功
            Failed,         // 失败
            Completing      // 完成中
        }
        
        /// <summary>
        /// 重置状态
        /// </summary>
        public void Reset()
        {
            isOccupied = false;
            currentSampleID = string.Empty;
            currentPhase = CuttingPhase.Idle;
            operationProgress = 0f;
        }
    }
    
    /// <summary>
    /// 样本生成配置
    /// </summary>
    [System.Serializable]
    public class SampleGenerationConfig
    {
        [Header("3D对象设置")]
        public float sampleScale = 1f;      // 样本缩放比例
        public float heightOffset = 0.1f;   // 生成高度偏移
        public bool enablePhysics = true;   // 是否启用物理
        public bool autoCollect = true;     // 是否自动收集到背包
        
        [Header("视觉效果")]
        public bool showGenerationEffect = true; // 显示生成特效
        public GameObject generationEffectPrefab; // 生成特效预制体
        public float effectDuration = 2f;   // 特效持续时间
        
        [Header("音效设置")]
        public AudioClip generationSound;   // 生成音效
        public float soundVolume = 0.5f;    // 音效音量
        
        /// <summary>
        /// 获取默认配置
        /// </summary>
        public static SampleGenerationConfig GetDefault()
        {
            return new SampleGenerationConfig
            {
                sampleScale = 1f,
                heightOffset = 0.1f,
                enablePhysics = true,
                autoCollect = true,
                showGenerationEffect = true,
                effectDuration = 2f,
                soundVolume = 0.5f
            };
        }
    }
}
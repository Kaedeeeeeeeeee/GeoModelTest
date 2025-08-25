using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace SampleCuttingSystem
{
    /// <summary>
    /// 样本地层分析器
    /// 分析ReconstructedSample中的地层结构，计算切割边界和成功区域
    /// </summary>
    public class SampleLayerAnalyzer : MonoBehaviour
    {
        [Header("切割设置")]
        [Tooltip("2m样本的物理长度对应的UI高度")]
        [SerializeField] private float sampleUIHeight = 400f; // UI中样本图的高度
        
        [Header("成功区域设置")]
        [Tooltip("不同地层数量对应的成功区域大小(厘米)")]
        [SerializeField] private SuccessZoneSetting[] successZoneSettings = new SuccessZoneSetting[]
        {
            new SuccessZoneSetting { layerCount = 2, zoneSize = 40f }, // 40cm
            new SuccessZoneSetting { layerCount = 3, zoneSize = 20f }, // 20cm  
            new SuccessZoneSetting { layerCount = 4, zoneSize = 10f }, // 10cm
            new SuccessZoneSetting { layerCount = 5, zoneSize = 10f }  // 10cm (5层以上都用10cm)
        };
        
        [System.Serializable]
        public class SuccessZoneSetting
        {
            public int layerCount;
            public float zoneSize; // 成功区域大小，单位：厘米
        }
        
        /// <summary>
        /// 分析样本的地层边界
        /// </summary>
        public SampleCuttingGame.LayerBoundary[] AnalyzeLayerBoundaries(GeometricSampleReconstructor.ReconstructedSample sample)
        {
            if (sample?.layerSegments == null || sample.layerSegments.Length <= 1)
            {
                Debug.LogWarning("样本不需要切割：只有单层或无效样本");
                return new SampleCuttingGame.LayerBoundary[0];
            }
            
            // 按相对深度排序地层
            var sortedLayers = sample.layerSegments
                .Where(segment => segment?.sourceLayer != null)
                .OrderBy(segment => segment.relativeDepth)
                .ToArray();
                
            if (sortedLayers.Length <= 1)
            {
                Debug.LogWarning("样本不需要切割：有效地层不足");
                return new SampleCuttingGame.LayerBoundary[0];
            }
            
            Debug.Log($"样本分析: 找到 {sortedLayers.Length} 个地层");
            
            // 计算地层厚度
            var layerThicknesses = CalculateLayerThicknesses(sortedLayers, sample.totalHeight);
            
            // 生成边界数据
            var boundaries = new List<SampleCuttingGame.LayerBoundary>();
            float currentDepth = 0f;
            
            // 计算成功区域大小
            float successZoneSize = GetSuccessZoneSize(sortedLayers.Length);
            
            for (int i = 0; i < sortedLayers.Length - 1; i++) // 最后一层不需要切割
            {
                currentDepth += layerThicknesses[i];
                
                // 将深度转换为UI位置 (0-1标准化)
                float normalizedPosition = currentDepth / sample.totalHeight;
                
                var boundary = new SampleCuttingGame.LayerBoundary
                {
                    position = normalizedPosition,
                    successZoneSize = successZoneSize / 200f, // 转换为标准化值 (200cm = 2m)
                    layerName = sortedLayers[i].sourceLayer.layerName ?? $"地层 {i + 1}",
                    layerColor = GetLayerColor(sortedLayers[i])
                };
                
                boundaries.Add(boundary);
                
                Debug.Log($"边界 {i + 1}: 位置={normalizedPosition:F3}, 地层={boundary.layerName}, 成功区域={successZoneSize}cm");
            }
            
            return boundaries.ToArray();
        }
        
        /// <summary>
        /// 计算各地层的厚度
        /// </summary>
        private float[] CalculateLayerThicknesses(GeometricSampleReconstructor.LayerSegment[] sortedLayers, float totalHeight)
        {
            var thicknesses = new float[sortedLayers.Length];
            
            for (int i = 0; i < sortedLayers.Length; i++)
            {
                if (i < sortedLayers.Length - 1)
                {
                    // 中间层：下一层的起始深度 - 当前层的起始深度
                    thicknesses[i] = sortedLayers[i + 1].relativeDepth - sortedLayers[i].relativeDepth;
                }
                else
                {
                    // 最后一层：总高度 - 当前层的起始深度
                    thicknesses[i] = totalHeight - sortedLayers[i].relativeDepth;
                }
                
                // 确保厚度为正值
                thicknesses[i] = Mathf.Max(thicknesses[i], 0.01f);
                
                Debug.Log($"地层 {i} ({sortedLayers[i].sourceLayer.layerName}): 厚度={thicknesses[i]:F3}m");
            }
            
            return thicknesses;
        }
        
        /// <summary>
        /// 根据地层数量获取成功区域大小
        /// </summary>
        private float GetSuccessZoneSize(int layerCount)
        {
            // 查找匹配的设置
            var setting = successZoneSettings.FirstOrDefault(s => s.layerCount == layerCount);
            
            if (setting != null)
            {
                return setting.zoneSize;
            }
            
            // 如果没有找到精确匹配，使用最接近的设置
            if (layerCount >= 5)
            {
                return 10f; // 5层以上都用10cm
            }
            else if (layerCount == 1)
            {
                return 50f; // 单层（虽然不应该切割，但以防万一）
            }
            else
            {
                return 20f; // 默认值
            }
        }
        
        /// <summary>
        /// 获取地层颜色
        /// </summary>
        private Color GetLayerColor(GeometricSampleReconstructor.LayerSegment segment)
        {
            if (segment?.material != null)
            {
                return segment.material.color;
            }
            
            // 默认颜色基于地层名称生成
            if (segment?.sourceLayer?.layerName != null)
            {
                return GenerateColorFromString(segment.sourceLayer.layerName);
            }
            
            return Color.gray;
        }
        
        /// <summary>
        /// 根据字符串生成颜色
        /// </summary>
        private Color GenerateColorFromString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return Color.gray;
                
            // 使用字符串哈希值生成稳定的颜色
            int hash = input.GetHashCode();
            
            // 提取RGB分量
            float r = ((hash & 0xFF0000) >> 16) / 255f;
            float g = ((hash & 0x00FF00) >> 8) / 255f;
            float b = (hash & 0x0000FF) / 255f;
            
            // 确保颜色足够明亮和饱和
            r = Mathf.Clamp(r * 0.8f + 0.2f, 0.2f, 1f);
            g = Mathf.Clamp(g * 0.8f + 0.2f, 0.2f, 1f);
            b = Mathf.Clamp(b * 0.8f + 0.2f, 0.2f, 1f);
            
            return new Color(r, g, b, 1f);
        }
        
        /// <summary>
        /// 验证样本是否可以切割
        /// </summary>
        public bool CanSampleBeCut(GeometricSampleReconstructor.ReconstructedSample sample)
        {
            if (sample?.layerSegments == null)
                return false;
                
            // 统计有效地层数量
            int validLayerCount = sample.layerSegments.Count(segment => 
                segment?.sourceLayer != null && 
                !string.IsNullOrEmpty(segment.sourceLayer.layerName));
                
            return validLayerCount > 1;
        }
        
        /// <summary>
        /// 获取样本的详细信息用于UI显示
        /// </summary>
        public SampleInfo GetSampleInfo(GeometricSampleReconstructor.ReconstructedSample sample)
        {
            if (sample?.layerSegments == null)
                return null;
                
            var sortedLayers = sample.layerSegments
                .Where(segment => segment?.sourceLayer != null)
                .OrderBy(segment => segment.relativeDepth)
                .ToArray();
                
            var info = new SampleInfo
            {
                totalHeight = sample.totalHeight,
                layerCount = sortedLayers.Length,
                needsCutting = sortedLayers.Length > 1,
                estimatedCuts = Mathf.Max(0, sortedLayers.Length - 1),
                layers = new LayerInfo[sortedLayers.Length]
            };
            
            var thicknesses = CalculateLayerThicknesses(sortedLayers, sample.totalHeight);
            
            for (int i = 0; i < sortedLayers.Length; i++)
            {
                info.layers[i] = new LayerInfo
                {
                    name = sortedLayers[i].sourceLayer.layerName ?? $"地层 {i + 1}",
                    thickness = thicknesses[i],
                    startDepth = sortedLayers[i].relativeDepth,
                    endDepth = sortedLayers[i].relativeDepth + thicknesses[i],
                    color = GetLayerColor(sortedLayers[i])
                };
            }
            
            return info;
        }
        
        [System.Serializable]
        public class SampleInfo
        {
            public float totalHeight;
            public int layerCount;
            public bool needsCutting;
            public int estimatedCuts;
            public LayerInfo[] layers;
        }
        
        [System.Serializable]
        public class LayerInfo
        {
            public string name;
            public float thickness;
            public float startDepth;
            public float endDepth;
            public Color color;
        }
        
        /// <summary>
        /// Editor调试方法：显示样本分析结果
        /// </summary>
        [ContextMenu("测试样本分析")]
        private void TestSampleAnalysis()
        {
            // 查找场景中的样本进行测试
            var reconstructor = FindFirstObjectByType<GeometricSampleReconstructor>();
            if (reconstructor != null)
            {
                Debug.Log("找到GeometricSampleReconstructor，开始测试样本分析...");
            }
            else
            {
                Debug.LogWarning("未找到GeometricSampleReconstructor进行测试");
            }
        }
    }
}
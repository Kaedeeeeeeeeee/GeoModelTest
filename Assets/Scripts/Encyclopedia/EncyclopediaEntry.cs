using System;
using UnityEngine;

namespace Encyclopedia
{
    /// <summary>
    /// 图鉴条目类型
    /// </summary>
    public enum EntryType
    {
        Mineral,
        Fossil
    }

    /// <summary>
    /// 稀有度枚举
    /// </summary>
    public enum Rarity
    {
        Common,
        Uncommon,
        Rare
    }

    /// <summary>
    /// 图鉴单个条目数据
    /// </summary>
    [Serializable]
    public class EncyclopediaEntry
    {
        [Header("基本信息")]
        public string id;
        public EntryType entryType;
        public string displayName;
        public string nameEN;
        public string nameJA;
        public string nameCN;

        [Header("分类信息")]
        public string layerName;
        public string layerId;
        public string rockName;  // 仅矿物使用
        public string rockId;    // 仅矿物使用

        [Header("稀有度信息")]
        public Rarity rarity;
        public float discoveryProbability;
        public float percentage;  // 仅矿物使用

        [Header("描述信息")]
        public string description;
        public string appearance;

        [Header("物理属性")]
        public string mohsHardness;
        public bool acidReaction;
        public string uvFluorescence;
        public string magnetism;
        public string density;
        public string polarizedColor;

        [Header("资源文件")]
        public string imageFile;
        public string modelFile;
        public Sprite icon;
        public GameObject model3D;

        [Header("收集状态")]
        public bool isDiscovered;
        public DateTime firstDiscoveredTime;
        public int discoveryCount;

        /// <summary>
        /// 获取格式化的显示名称
        /// 矿物: 地层名-岩石名-矿物名
        /// 化石: 地层名-化石名
        /// </summary>
        public string GetFormattedDisplayName()
        {
            if (entryType == EntryType.Mineral)
            {
                return $"{layerName}-{rockName}-{displayName}";
            }
            else
            {
                return $"{layerName}-{displayName}";
            }
        }

        /// <summary>
        /// 获取稀有度显示文本
        /// </summary>
        public string GetRarityText()
        {
            return rarity switch
            {
                Rarity.Common => "常见",
                Rarity.Uncommon => "少见", 
                Rarity.Rare => "稀有",
                _ => "未知"
            };
        }

        /// <summary>
        /// 获取稀有度颜色
        /// </summary>
        public Color GetRarityColor()
        {
            return rarity switch
            {
                Rarity.Common => Color.white,
                Rarity.Uncommon => Color.green,
                Rarity.Rare => Color.yellow,
                _ => Color.gray
            };
        }

        /// <summary>
        /// 获取条目类型显示文本
        /// </summary>
        public string GetEntryTypeText()
        {
            return entryType switch
            {
                EntryType.Mineral => "矿物",
                EntryType.Fossil => "化石",
                _ => "未知"
            };
        }

        /// <summary>
        /// 标记为已发现
        /// </summary>
        public void MarkAsDiscovered()
        {
            if (!isDiscovered)
            {
                isDiscovered = true;
                firstDiscoveredTime = DateTime.Now;
                discoveryCount = 1;
            }
            else
            {
                discoveryCount++;
            }
        }

        /// <summary>
        /// 检查是否应该显示详细信息
        /// </summary>
        public bool ShouldShowDetails()
        {
            return isDiscovered;
        }

        /// <summary>
        /// 获取显示用的名称（未发现时显示???）
        /// </summary>
        public string GetDisplayNameForUI()
        {
            if (!isDiscovered)
            {
                return "???";
            }
            return GetFormattedDisplayName();
        }

        /// <summary>
        /// 获取显示用的描述（未发现时显示???）
        /// </summary>
        public string GetDescriptionForUI()
        {
            if (!isDiscovered)
            {
                return "未发现的" + GetEntryTypeText();
            }
            return description;
        }
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.InputSystem;

namespace Encyclopedia
{
    /// <summary>
    /// 图鉴主UI控制器
    /// 负责整个图鉴界面的显示和交互逻辑
    /// </summary>
    public class EncyclopediaUI : MonoBehaviour
    {
        [Header("主要面板")]
        [SerializeField] private GameObject encyclopediaPanel;
        [SerializeField] private Button closeButton;

        [Header("左侧导航")]
        [SerializeField] private Transform layerTabContainer;
        [SerializeField] private Button layerTabPrefab;
        [SerializeField] private Text statisticsText;

        [Header("右侧内容区")]
        [SerializeField] private Transform entryListContainer;
        [SerializeField] private GameObject entryItemPrefab;
        [SerializeField] private ScrollRect entryScrollRect;

        [Header("筛选控件")]
        [SerializeField] private Dropdown entryTypeFilter;
        [SerializeField] private Dropdown rarityFilter;
        [SerializeField] private InputField searchInput;
        [SerializeField] private Button clearFiltersButton;

        [Header("详细信息面板")]
        [SerializeField] private GameObject detailPanel;
        [SerializeField] private Text detailTitle;
        [SerializeField] private Image detailIcon;
        [SerializeField] private Text detailDescription;
        [SerializeField] private Text detailProperties;
        [SerializeField] private Model3DViewer model3DViewer;

        [Header("设置")]
        [SerializeField] private Key toggleKey = Key.O;
        [SerializeField] private bool startClosed = true;

        // 私有变量
        private bool isOpen = false;
        private string currentLayerName = "";
        private List<Button> layerTabs = new List<Button>();
        private List<GameObject> entryItems = new List<GameObject>();
        private EncyclopediaEntry selectedEntry = null;

        // 筛选状态
        private EntryType? currentEntryTypeFilter = null;
        private Rarity? currentRarityFilter = null;
        private string currentSearchQuery = "";

        private void Start()
        {
            // 确保开始时图鉴是关闭的
            if (encyclopediaPanel != null)
            {
                encyclopediaPanel.SetActive(false);
                isOpen = false;
            }
            
            InitializeUI();
            
            // 强制关闭图鉴
            CloseEncyclopedia();
        }

        private void Update()
        {
            // 检测按键输入 - 使用新Input System
            if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
            {
                ToggleEncyclopedia();
            }
        }

        /// <summary>
        /// 初始化UI
        /// </summary>
        private void InitializeUI()
        {
            // 设置关闭按钮
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseEncyclopedia);
            }

            // 设置筛选控件
            SetupFilterControls();

            // 等待数据加载完成
            if (EncyclopediaData.Instance != null && EncyclopediaData.Instance.IsDataLoaded)
            {
                SetupLayerTabs();
                UpdateStatistics();
            }
            else
            {
                Invoke(nameof(DelayedInitialize), 1f);
            }

            // 订阅事件
            CollectionManager.OnStatsUpdated += OnStatsUpdated;
        }

        /// <summary>
        /// 延迟初始化
        /// </summary>
        private void DelayedInitialize()
        {
            if (EncyclopediaData.Instance != null && EncyclopediaData.Instance.IsDataLoaded)
            {
                SetupLayerTabs();
                UpdateStatistics();
            }
            else
            {
                Debug.LogError("EncyclopediaData仍未加载完成");
            }
        }

        /// <summary>
        /// 设置筛选控件
        /// </summary>
        private void SetupFilterControls()
        {
            // 条目类型筛选
            if (entryTypeFilter != null)
            {
                entryTypeFilter.ClearOptions();
                entryTypeFilter.AddOptions(new List<string> { "全部", "矿物", "化石" });
                entryTypeFilter.onValueChanged.AddListener(OnEntryTypeFilterChanged);
            }

            // 稀有度筛选
            if (rarityFilter != null)
            {
                rarityFilter.ClearOptions();
                rarityFilter.AddOptions(new List<string> { "全部", "常见", "少见", "稀有" });
                rarityFilter.onValueChanged.AddListener(OnRarityFilterChanged);
            }

            // 搜索输入
            if (searchInput != null)
            {
                searchInput.onValueChanged.AddListener(OnSearchInputChanged);
            }

            // 清除筛选按钮
            if (clearFiltersButton != null)
            {
                clearFiltersButton.onClick.AddListener(ClearAllFilters);
            }
        }

        /// <summary>
        /// 设置地层标签页
        /// </summary>
        private void SetupLayerTabs()
        {
            if (layerTabContainer == null || layerTabPrefab == null)
                return;

            // 清除现有标签页
            foreach (var tab in layerTabs)
            {
                if (tab != null)
                {
                    DestroyImmediate(tab.gameObject);
                }
            }
            layerTabs.Clear();

            // 创建地层标签页
            foreach (string layerName in EncyclopediaData.Instance.LayerNames)
            {
                var tabButton = Instantiate(layerTabPrefab, layerTabContainer);
                tabButton.GetComponentInChildren<Text>().text = layerName;
                
                string layer = layerName; // 捕获局部变量
                tabButton.onClick.AddListener(() => OnLayerTabClicked(layer));
                
                layerTabs.Add(tabButton);
            }

            // 默认选择第一个地层
            if (layerTabs.Count > 0)
            {
                OnLayerTabClicked(EncyclopediaData.Instance.LayerNames[0]);
            }
        }

        /// <summary>
        /// 地层标签页点击事件
        /// </summary>
        private void OnLayerTabClicked(string layerName)
        {
            currentLayerName = layerName;
            
            // 更新标签页样式
            UpdateLayerTabStyles();
            
            // 刷新条目列表
            RefreshEntryList();
        }

        /// <summary>
        /// 更新地层标签页样式
        /// </summary>
        private void UpdateLayerTabStyles()
        {
            for (int i = 0; i < layerTabs.Count; i++)
            {
                if (i < EncyclopediaData.Instance.LayerNames.Count)
                {
                    bool isSelected = EncyclopediaData.Instance.LayerNames[i] == currentLayerName;
                    var colors = layerTabs[i].colors;
                    colors.normalColor = isSelected ? Color.cyan : Color.white;
                    layerTabs[i].colors = colors;
                }
            }
        }

        /// <summary>
        /// 刷新条目列表
        /// </summary>
        private void RefreshEntryList()
        {
            if (entryListContainer == null || entryItemPrefab == null)
                return;

            // 清除现有条目
            foreach (var item in entryItems)
            {
                if (item != null)
                {
                    DestroyImmediate(item);
                }
            }
            entryItems.Clear();

            // 获取筛选后的条目
            var entries = GetFilteredEntries();

            // 创建条目UI
            foreach (var entry in entries)
            {
                CreateEntryItem(entry);
            }

            // 重置滚动位置
            if (entryScrollRect != null)
            {
                entryScrollRect.verticalNormalizedPosition = 1f;
            }
        }

        /// <summary>
        /// 获取筛选后的条目
        /// </summary>
        private List<EncyclopediaEntry> GetFilteredEntries()
        {
            var entries = EncyclopediaData.Instance.GetEntriesByLayer(currentLayerName);

            // 应用筛选条件
            if (currentEntryTypeFilter.HasValue)
            {
                entries = entries.Where(e => e.entryType == currentEntryTypeFilter.Value).ToList();
            }

            if (currentRarityFilter.HasValue)
            {
                entries = entries.Where(e => e.rarity == currentRarityFilter.Value).ToList();
            }

            if (!string.IsNullOrEmpty(currentSearchQuery))
            {
                entries = entries.Where(e => 
                    e.GetFormattedDisplayName().ToLower().Contains(currentSearchQuery.ToLower()) ||
                    e.nameEN.ToLower().Contains(currentSearchQuery.ToLower())
                ).ToList();
            }

            // 按类型和稀有度排序
            return entries.OrderBy(e => e.entryType)
                         .ThenBy(e => e.rarity)
                         .ThenBy(e => e.displayName)
                         .ToList();
        }

        /// <summary>
        /// 创建条目UI项
        /// </summary>
        private void CreateEntryItem(EncyclopediaEntry entry)
        {
            var itemGO = Instantiate(entryItemPrefab, entryListContainer);
            entryItems.Add(itemGO);

            // 设置条目信息
            var nameText = itemGO.transform.Find("NameText")?.GetComponent<Text>();
            var iconImage = itemGO.transform.Find("IconImage")?.GetComponent<Image>();
            var rarityText = itemGO.transform.Find("RarityText")?.GetComponent<Text>();
            var statusImage = itemGO.transform.Find("StatusImage")?.GetComponent<Image>();
            var button = itemGO.GetComponent<Button>();

            if (nameText != null)
            {
                nameText.text = entry.GetDisplayNameForUI();
                nameText.color = entry.isDiscovered ? Color.white : Color.gray;
            }

            if (iconImage != null)
            {
                if (entry.isDiscovered && entry.icon != null)
                {
                    iconImage.sprite = entry.icon;
                    iconImage.color = Color.white;
                }
                else
                {
                    iconImage.sprite = null;
                    iconImage.color = Color.black;
                }
            }

            if (rarityText != null)
            {
                rarityText.text = entry.GetRarityText();
                rarityText.color = entry.GetRarityColor();
            }

            if (statusImage != null)
            {
                statusImage.color = entry.isDiscovered ? Color.green : Color.red;
            }

            if (button != null)
            {
                button.onClick.AddListener(() => OnEntryItemClicked(entry));
            }
        }

        /// <summary>
        /// 条目项点击事件
        /// </summary>
        private void OnEntryItemClicked(EncyclopediaEntry entry)
        {
            selectedEntry = entry;
            ShowEntryDetails(entry);
        }

        /// <summary>
        /// 显示条目详细信息
        /// </summary>
        private void ShowEntryDetails(EncyclopediaEntry entry)
        {
            if (detailPanel == null)
                return;

            detailPanel.SetActive(true);

            // 设置标题
            if (detailTitle != null)
            {
                detailTitle.text = entry.GetDisplayNameForUI();
            }

            // 设置图标
            if (detailIcon != null)
            {
                if (entry.isDiscovered && entry.icon != null)
                {
                    detailIcon.sprite = entry.icon;
                    detailIcon.color = Color.white;
                }
                else
                {
                    detailIcon.sprite = null;
                    detailIcon.color = Color.gray;
                }
            }

            // 设置描述
            if (detailDescription != null)
            {
                detailDescription.text = entry.GetDescriptionForUI();
            }

            // 设置属性信息
            if (detailProperties != null)
            {
                detailProperties.text = GeneratePropertiesText(entry);
            }

            // 显示3D模型
            if (model3DViewer != null && entry.isDiscovered && entry.model3D != null)
            {
                model3DViewer.ShowModel(entry.model3D);
            }
            else if (model3DViewer != null)
            {
                model3DViewer.ClearModel();
            }
        }

        /// <summary>
        /// 生成属性文本
        /// </summary>
        private string GeneratePropertiesText(EncyclopediaEntry entry)
        {
            if (!entry.isDiscovered)
            {
                return "发现后显示详细属性";
            }

            var properties = new List<string>();
            
            properties.Add($"类型: {entry.GetEntryTypeText()}");
            properties.Add($"地层: {entry.layerName}");
            
            if (entry.entryType == EntryType.Mineral)
            {
                properties.Add($"岩石: {entry.rockName}");
                properties.Add($"含量: {entry.percentage:P1}");
                
                if (!string.IsNullOrEmpty(entry.mohsHardness))
                    properties.Add($"莫氏硬度: {entry.mohsHardness}");
                if (!string.IsNullOrEmpty(entry.density))
                    properties.Add($"密度: {entry.density}");
                if (!string.IsNullOrEmpty(entry.magnetism))
                    properties.Add($"磁性: {entry.magnetism}");
            }
            else
            {
                properties.Add($"稀有度: {entry.GetRarityText()}");
                properties.Add($"发现概率: {entry.discoveryProbability:P1}");
            }

            if (entry.discoveryCount > 0)
            {
                properties.Add($"发现次数: {entry.discoveryCount}");
                properties.Add($"首次发现: {entry.firstDiscoveredTime:yyyy-MM-dd}");
            }

            return string.Join("\n", properties);
        }

        /// <summary>
        /// 筛选事件处理
        /// </summary>
        private void OnEntryTypeFilterChanged(int value)
        {
            currentEntryTypeFilter = value switch
            {
                1 => EntryType.Mineral,
                2 => EntryType.Fossil,
                _ => null
            };
            RefreshEntryList();
        }

        private void OnRarityFilterChanged(int value)
        {
            currentRarityFilter = value switch
            {
                1 => Rarity.Common,
                2 => Rarity.Uncommon,
                3 => Rarity.Rare,
                _ => null
            };
            RefreshEntryList();
        }

        private void OnSearchInputChanged(string query)
        {
            currentSearchQuery = query;
            RefreshEntryList();
        }

        private void ClearAllFilters()
        {
            currentEntryTypeFilter = null;
            currentRarityFilter = null;
            currentSearchQuery = "";

            if (entryTypeFilter != null) entryTypeFilter.value = 0;
            if (rarityFilter != null) rarityFilter.value = 0;
            if (searchInput != null) searchInput.text = "";

            RefreshEntryList();
        }

        /// <summary>
        /// 更新统计信息
        /// </summary>
        private void UpdateStatistics()
        {
            if (statisticsText == null || CollectionManager.Instance == null)
                return;

            var stats = CollectionManager.Instance.CurrentStats;
            
            statisticsText.text = $"收集进度: {stats.discoveredEntries}/{stats.totalEntries} ({stats.overallProgress:P1})\n" +
                                 $"矿物: {stats.discoveredMinerals}/{stats.totalMinerals} ({stats.mineralProgress:P1})\n" +
                                 $"化石: {stats.discoveredFossils}/{stats.totalFossils} ({stats.fossilProgress:P1})";
        }

        /// <summary>
        /// 统计更新事件
        /// </summary>
        private void OnStatsUpdated(CollectionStats stats)
        {
            UpdateStatistics();
            RefreshEntryList();
        }

        /// <summary>
        /// 切换图鉴显示状态
        /// </summary>
        public void ToggleEncyclopedia()
        {
            if (isOpen)
            {
                CloseEncyclopedia();
            }
            else
            {
                OpenEncyclopedia();
            }
        }

        /// <summary>
        /// 打开图鉴
        /// </summary>
        public void OpenEncyclopedia()
        {
            if (encyclopediaPanel != null)
            {
                encyclopediaPanel.SetActive(true);
                isOpen = true;
                
                // 刷新数据
                RefreshEntryList();
                UpdateStatistics();
                
                Debug.Log("图鉴已打开");
            }
        }

        /// <summary>
        /// 关闭图鉴
        /// </summary>
        public void CloseEncyclopedia()
        {
            if (encyclopediaPanel != null)
            {
                encyclopediaPanel.SetActive(false);
                isOpen = false;
                
                Debug.Log("图鉴已关闭");
            }
        }

        /// <summary>
        /// 获取图鉴开启状态
        /// </summary>
        public bool IsOpen()
        {
            return isOpen;
        }

        private void OnDestroy()
        {
            // 取消事件订阅
            CollectionManager.OnStatsUpdated -= OnStatsUpdated;
        }

#if UNITY_EDITOR
        [ContextMenu("测试发现所有条目")]
        private void TestDiscoverAllEntries()
        {
            if (CollectionManager.Instance != null)
            {
                CollectionManager.Instance.UnlockAllEntries();
            }
        }

        [ContextMenu("测试重置收集进度")]
        private void TestResetProgress()
        {
            if (CollectionManager.Instance != null)
            {
                CollectionManager.Instance.ResetProgress();
            }
        }
#endif
    }
}
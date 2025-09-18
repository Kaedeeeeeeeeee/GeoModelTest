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
        [SerializeField] private Simple3DViewer model3DViewer;

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

        // 翻页功能
        private List<EncyclopediaEntry> currentFilteredEntries = new List<EncyclopediaEntry>();
        private int currentEntryIndex = -1;

        private void Start()
        {
            // 修复Canvas层级问题
            FixCanvasLayer();

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
                // 增大关闭按钮字体以适配移动端
                var closeButtonText = closeButton.GetComponentInChildren<Text>();
                if (closeButtonText != null) closeButtonText.fontSize = 24;
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
                // 增大Dropdown字体
                var entryTypeText = entryTypeFilter.GetComponentInChildren<Text>();
                if (entryTypeText != null) entryTypeText.fontSize = 20;
            }

            // 稀有度筛选
            if (rarityFilter != null)
            {
                rarityFilter.ClearOptions();
                rarityFilter.AddOptions(new List<string> { "全部", "常见", "少见", "稀有" });
                rarityFilter.onValueChanged.AddListener(OnRarityFilterChanged);
                // 增大Dropdown字体
                var rarityText = rarityFilter.GetComponentInChildren<Text>();
                if (rarityText != null) rarityText.fontSize = 20;
            }

            // 搜索输入
            if (searchInput != null)
            {
                searchInput.onValueChanged.AddListener(OnSearchInputChanged);
                // 增大InputField字体
                var inputText = searchInput.GetComponentInChildren<Text>();
                if (inputText != null) inputText.fontSize = 20;
            }

            // 清除筛选按钮
            if (clearFiltersButton != null)
            {
                clearFiltersButton.onClick.AddListener(ClearAllFilters);
                // 增大Button字体
                var buttonText = clearFiltersButton.GetComponentInChildren<Text>();
                if (buttonText != null) buttonText.fontSize = 20;
            }
        }

        /// <summary>
        /// 设置地层标签页
        /// </summary>
        private void SetupLayerTabs()
        {
            if (layerTabContainer == null || layerTabPrefab == null)
                return;

            Debug.Log($"[EncyclopediaUI] SetupLayerTabs - 当前layerTabs数量: {layerTabs.Count}");
            Debug.Log($"[EncyclopediaUI] SetupLayerTabs - layerTabContainer子物体数量: {layerTabContainer.childCount}");

            // 检查是否已经有现有的标签页（运行时防重复创建）
            if (layerTabs.Count > 0 && Application.isPlaying)
            {
                Debug.Log("[EncyclopediaUI] 运行时检测到已存在地层标签，跳过创建以防止重复");
                // 仍然需要设置默认选择
                if (string.IsNullOrEmpty(currentLayerName) && EncyclopediaData.Instance.LayerNames.Count > 0)
                {
                    OnLayerTabClicked(EncyclopediaData.Instance.LayerNames[0]);
                }
                return;
            }

            // 如果layerTabs为空，但container中有子物体，尝试收集现有按钮
            if (layerTabs.Count == 0 && layerTabContainer.childCount > 0)
            {
                Debug.Log("[EncyclopediaUI] 尝试收集现有的地层标签按钮");
                for (int i = 0; i < layerTabContainer.childCount; i++)
                {
                    var child = layerTabContainer.GetChild(i);
                    var button = child.GetComponent<Button>();
                    var text = child.GetComponentInChildren<Text>();

                    if (button != null && text != null && text.text != "地层名称")
                    {
                        layerTabs.Add(button);
                        Debug.Log($"[EncyclopediaUI] 收集到现有地层标签: {text.text}");

                        // 重新绑定点击事件
                        string layerName = text.text;
                        button.onClick.RemoveAllListeners();
                        button.onClick.AddListener(() => OnLayerTabClicked(layerName));
                    }
                }

                if (layerTabs.Count > 0)
                {
                    Debug.Log($"[EncyclopediaUI] 成功收集到 {layerTabs.Count} 个现有地层标签，无需重新创建");
                    // 设置默认选择
                    if (EncyclopediaData.Instance.LayerNames.Count > 0)
                    {
                        OnLayerTabClicked(EncyclopediaData.Instance.LayerNames[0]);
                    }
                    return;
                }
            }

            // 清除现有标签页（仅在编辑器模式或确实需要重新创建时）
            Debug.Log("[EncyclopediaUI] 清除现有标签页并重新创建");
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
                var buttonText = tabButton.GetComponentInChildren<Text>();
                buttonText.text = layerName;
                buttonText.fontSize = 28; // 进一步增大地层按钮字体以适配移动端

                // 增加按钮高度以适配更大的字体
                var buttonRect = tabButton.GetComponent<RectTransform>();
                if (buttonRect != null)
                {
                    var sizeDelta = buttonRect.sizeDelta;
                    buttonRect.sizeDelta = new Vector2(sizeDelta.x, 60f); // 增加按钮高度到60像素
                    Debug.Log($"[EncyclopediaUI] 地层按钮 {layerName} 高度已调整为60px");
                }

                string layer = layerName; // 捕获局部变量
                tabButton.onClick.AddListener(() => OnLayerTabClicked(layer));

                layerTabs.Add(tabButton);
                Debug.Log($"[EncyclopediaUI] 创建地层标签: {layerName}");
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
            Debug.Log($"[EncyclopediaUI] OnLayerTabClicked被调用，layerName: '{layerName}'");
            Debug.Log($"[EncyclopediaUI] 旧currentLayerName: '{currentLayerName}'");

            currentLayerName = layerName;

            Debug.Log($"[EncyclopediaUI] 新currentLayerName: '{currentLayerName}'");

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
            if (EncyclopediaData.Instance == null)
            {
                Debug.LogWarning("[EncyclopediaUI] EncyclopediaData.Instance为null，跳过样式更新");
                return;
            }

            if (layerTabs == null)
            {
                Debug.LogWarning("[EncyclopediaUI] layerTabs为null，跳过样式更新");
                return;
            }

            for (int i = 0; i < layerTabs.Count; i++)
            {
                if (layerTabs[i] == null)
                {
                    Debug.LogWarning($"[EncyclopediaUI] layerTabs[{i}]为null，跳过");
                    continue;
                }

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
            Debug.Log("[EncyclopediaUI] RefreshEntryList开始...");

            if (entryListContainer == null)
            {
                Debug.LogError("[EncyclopediaUI] entryListContainer为null!");
                return;
            }

            if (entryItemPrefab == null)
            {
                Debug.LogError("[EncyclopediaUI] entryItemPrefab为null!");
                return;
            }

            Debug.Log($"[EncyclopediaUI] 容器检查通过，清除现有条目 {entryItems.Count} 个");
            Debug.Log($"[EncyclopediaUI] 容器子物体数量: {entryListContainer.childCount}");

            // 清除现有条目 - 使用更彻底的清理方式
            foreach (var item in entryItems)
            {
                if (item != null)
                {
                    DestroyImmediate(item);
                }
            }
            entryItems.Clear();

            // 额外清理：确保容器中没有遗留的子物体
            for (int i = entryListContainer.childCount - 1; i >= 0; i--)
            {
                var child = entryListContainer.GetChild(i);
                Debug.Log($"[EncyclopediaUI] 发现容器中的遗留子物体: {child.name}");
                DestroyImmediate(child.gameObject);
            }

            Debug.Log($"[EncyclopediaUI] 清理后容器子物体数量: {entryListContainer.childCount}");

            // 获取筛选后的条目
            var entries = GetFilteredEntries();

            Debug.Log($"[EncyclopediaUI] 准备创建 {entries.Count} 个条目UI");

            // 创建条目UI
            foreach (var entry in entries)
            {
                CreateEntryItem(entry);
            }

            Debug.Log($"[EncyclopediaUI] 完成创建 {entryItems.Count} 个条目UI");

            // 重置滚动位置
            if (entryScrollRect != null)
            {
                entryScrollRect.verticalNormalizedPosition = 1f;
            }

            Debug.Log("[EncyclopediaUI] RefreshEntryList完成");
        }

        /// <summary>
        /// 获取筛选后的条目
        /// </summary>
        private List<EncyclopediaEntry> GetFilteredEntries()
        {
            List<EncyclopediaEntry> entries;

            Debug.Log($"[EncyclopediaUI] GetFilteredEntries被调用，currentLayerName: '{currentLayerName}'");

            // 如果没有选择地层或地层名为空，显示所有条目
            if (string.IsNullOrEmpty(currentLayerName))
            {
                Debug.Log($"[EncyclopediaUI] currentLayerName为空，显示所有条目");
                entries = EncyclopediaData.Instance.AllEntries.Values.ToList();
            }
            else
            {
                Debug.Log($"[EncyclopediaUI] 显示地层 '{currentLayerName}' 的条目");
                entries = EncyclopediaData.Instance.GetEntriesByLayer(currentLayerName);

                // 调试：检查返回的条目的地层信息
                Debug.Log($"[EncyclopediaUI] GetEntriesByLayer返回了 {entries.Count} 个条目");
                if (entries.Count > 0)
                {
                    Debug.Log($"[EncyclopediaUI] 前3个条目的地层: {string.Join(", ", entries.Take(3).Select(e => $"{e.displayName}({e.layerName})"))}");
                }
            }

            Debug.Log($"[EncyclopediaUI] 基础条目数: {entries.Count}");

            // 应用筛选条件
            if (currentEntryTypeFilter.HasValue)
            {
                entries = entries.Where(e => e.entryType == currentEntryTypeFilter.Value).ToList();
                Debug.Log($"[EncyclopediaUI] 类型筛选后: {entries.Count}");
            }

            if (currentRarityFilter.HasValue)
            {
                entries = entries.Where(e => e.rarity == currentRarityFilter.Value).ToList();
                Debug.Log($"[EncyclopediaUI] 稀有度筛选后: {entries.Count}");
            }

            if (!string.IsNullOrEmpty(currentSearchQuery))
            {
                entries = entries.Where(e =>
                    e.GetFormattedDisplayName().ToLower().Contains(currentSearchQuery.ToLower()) ||
                    e.nameEN.ToLower().Contains(currentSearchQuery.ToLower())
                ).ToList();
                Debug.Log($"[EncyclopediaUI] 搜索筛选后: {entries.Count}");
            }

            // 按类型和稀有度排序
            var sortedEntries = entries.OrderBy(e => e.entryType)
                         .ThenBy(e => e.rarity)
                         .ThenBy(e => e.displayName)
                         .ToList();

            Debug.Log($"[EncyclopediaUI] 最终条目数: {sortedEntries.Count}");
            return sortedEntries;
        }

        /// <summary>
        /// 创建条目UI项
        /// </summary>
        private void CreateEntryItem(EncyclopediaEntry entry)
        {
            Debug.Log($"[EncyclopediaUI] 开始创建条目: {entry.GetFormattedDisplayName()}");

            if (entryItemPrefab == null)
            {
                Debug.LogError("[EncyclopediaUI] entryItemPrefab为null!");
                return;
            }

            if (entryListContainer == null)
            {
                Debug.LogError("[EncyclopediaUI] entryListContainer为null!");
                return;
            }

            var itemGO = Instantiate(entryItemPrefab, entryListContainer);
            if (itemGO == null)
            {
                Debug.LogError("[EncyclopediaUI] Instantiate失败!");
                return;
            }

            entryItems.Add(itemGO);
            Debug.Log($"[EncyclopediaUI] 成功实例化GameObject: {itemGO.name}，父级: {itemGO.transform.parent?.name}");

            // 设置条目信息
            var nameText = itemGO.transform.Find("NameText")?.GetComponent<Text>();
            var rarityText = itemGO.transform.Find("RarityText")?.GetComponent<Text>();
            var statusImage = itemGO.transform.Find("StatusImage")?.GetComponent<Image>();
            var iconImage = itemGO.transform.Find("IconImage")?.GetComponent<Image>();
            var button = itemGO.GetComponent<Button>();

            Debug.Log($"[EncyclopediaUI] 组件查找结果: nameText={nameText != null}, button={button != null}, iconImage={iconImage != null}");

            if (nameText != null)
            {
                nameText.text = entry.GetDisplayNameForUI();
                nameText.color = entry.isDiscovered ? Color.white : Color.gray;
                nameText.fontSize = 24; // 增大字体以适配移动端
                Debug.Log($"[EncyclopediaUI] 设置名称文本: {nameText.text}");
                Debug.Log($"[EncyclopediaUI] 条目发现状态: {entry.isDiscovered} ({entry.GetFormattedDisplayName()})");
                Debug.Log($"[EncyclopediaUI] 条目地层验证: {entry.layerName} (应该是: {currentLayerName})");

                // 验证条目地层是否正确
                if (entry.layerName != currentLayerName)
                {
                    Debug.LogError($"[EncyclopediaUI] ❌ 条目地层不匹配! 期望: {currentLayerName}, 实际: {entry.layerName}");
                }
            }
            else
            {
                Debug.LogWarning("[EncyclopediaUI] 未找到NameText组件");
            }

            if (rarityText != null)
            {
                rarityText.text = entry.GetRarityText();
                rarityText.color = entry.GetRarityColor();
                rarityText.fontSize = 18; // 增大字体以适配移动端
            }

            if (statusImage != null)
            {
                statusImage.color = entry.isDiscovered ? Color.green : Color.red;
            }

            // 隐藏白色图标方块
            if (iconImage != null)
            {
                iconImage.gameObject.SetActive(false);
                Debug.Log($"[EncyclopediaUI] 隐藏条目图标: {entry.GetFormattedDisplayName()}");
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
            Debug.Log($"[EncyclopediaUI] 条目被点击: {entry?.GetFormattedDisplayName() ?? "null"}");
            selectedEntry = entry;
            ShowEntryDetails(entry);
        }

        /// <summary>
        /// 关闭详情面板
        /// </summary>
        public void CloseDetailPanel()
        {
            if (detailPanel != null)
            {
                detailPanel.SetActive(false);
                Debug.Log("[EncyclopediaUI] 详情面板已关闭");
            }
        }

        /// <summary>
        /// 显示条目详细信息
        /// </summary>
        private void ShowEntryDetails(EncyclopediaEntry entry)
        {
            Debug.Log($"[EncyclopediaUI] ShowEntryDetails被调用: {entry?.GetFormattedDisplayName() ?? "null"}");

            if (detailPanel == null)
            {
                Debug.LogError("[EncyclopediaUI] detailPanel为null!");
                return;
            }

            // 更新当前筛选条目列表和索引
            currentFilteredEntries = GetFilteredEntries();
            currentEntryIndex = currentFilteredEntries.FindIndex(e => e.id == entry.id);
            Debug.Log($"[EncyclopediaUI] 当前条目索引: {currentEntryIndex}/{currentFilteredEntries.Count}");

            detailPanel.SetActive(true);
            Debug.Log($"[EncyclopediaUI] 详情面板已激活: {detailPanel.name}");

            // 确保详情页面为全屏布局
            var rectTransform = detailPanel.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0f, 0f);
                rectTransform.anchorMax = new Vector2(1f, 1f);
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
                Debug.Log("[EncyclopediaUI] 详情页面布局已修复为全屏");
            }

            // 修复背景为不透明
            var background = detailPanel.GetComponent<UnityEngine.UI.Image>();
            if (background != null)
            {
                var oldColor = background.color;
                background.color = new Color(oldColor.r, oldColor.g, oldColor.b, 1.0f);
                Debug.Log("[EncyclopediaUI] 背景已设置为不透明");
            }

            // 修复字体大小
            if (detailTitle != null)
            {
                detailTitle.fontSize = 36;
            }
            if (detailDescription != null)
            {
                detailDescription.fontSize = 24;
            }
            if (detailProperties != null)
            {
                detailProperties.fontSize = 22;
            }
            Debug.Log("[EncyclopediaUI] 字体大小已调整");

            // 添加关闭按钮（如果不存在）
            AddCloseButtonIfNeeded();

            // 设置标题
            if (detailTitle != null)
            {
                detailTitle.text = entry.GetDisplayNameForUI();
            }

            // 隐藏图标（我们没有为矿物和化石准备图标）
            if (detailIcon != null)
            {
                detailIcon.gameObject.SetActive(false);
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

            // 添加翻页按钮（如果不存在）
            AddNavigationButtonsIfNeeded();
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
            statisticsText.fontSize = 20; // 增大字体以适配移动端
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

                Debug.Log("[EncyclopediaUI] 图鉴开始打开...");

                // 验证数据状态
                if (EncyclopediaData.Instance == null)
                {
                    Debug.LogError("[EncyclopediaUI] EncyclopediaData.Instance为null!");
                    return;
                }

                if (!EncyclopediaData.Instance.IsDataLoaded)
                {
                    Debug.LogError("[EncyclopediaUI] 数据未加载!");
                    return;
                }

                Debug.Log($"[EncyclopediaUI] 数据验证通过: {EncyclopediaData.Instance.AllEntries?.Count ?? 0} 个条目");
                Debug.Log($"[EncyclopediaUI] 当前地层: '{currentLayerName}'");

                // 刷新数据
                RefreshEntryList();
                UpdateStatistics();

                Debug.Log("[EncyclopediaUI] 图鉴已打开");
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

        /// <summary>
        /// 修复Canvas层级问题，确保图鉴UI在所有其他UI之上
        /// </summary>
        private void FixCanvasLayer()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                // 设置为比InventoryCanvas(10000)更高的层级
                canvas.sortingOrder = 10001;
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                // 确保GraphicRaycaster存在且启用
                var raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster == null)
                {
                    raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
                }
                raycaster.enabled = true;

                Debug.Log($"[EncyclopediaUI] 修复Canvas层级: {canvas.name}, sortingOrder: {canvas.sortingOrder}");
            }
            else
            {
                Debug.LogWarning("[EncyclopediaUI] 未找到父级Canvas");
            }
        }

        /// <summary>
        /// 为详情面板添加关闭按钮（如果还没有的话）
        /// </summary>
        private void AddCloseButtonIfNeeded()
        {
            if (detailPanel == null)
            {
                Debug.LogWarning("[EncyclopediaUI] 详情面板为空，无法添加关闭按钮");
                return;
            }

            // 检查是否已经存在关闭按钮
            var existingCloseButton = detailPanel.transform.Find("DetailCloseButton");
            if (existingCloseButton != null)
            {
                Debug.Log("[EncyclopediaUI] 详情面板关闭按钮已存在");
                return;
            }

            Debug.Log("[EncyclopediaUI] 为详情面板创建关闭按钮");

            // 创建关闭按钮GameObject
            GameObject closeButtonGO = new GameObject("DetailCloseButton");
            closeButtonGO.transform.SetParent(detailPanel.transform, false);

            // 设置RectTransform（右上角位置）
            RectTransform closeRect = closeButtonGO.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 1);
            closeRect.anchorMax = new Vector2(1, 1);
            closeRect.pivot = new Vector2(1, 1);
            closeRect.anchoredPosition = new Vector2(-15, -15);
            closeRect.sizeDelta = new Vector2(30, 30);

            // 添加Image组件（背景）
            Image buttonImage = closeButtonGO.AddComponent<Image>();
            buttonImage.color = new Color(0.9f, 0.3f, 0.3f, 0.9f);

            // 添加Button组件
            Button detailCloseBtn = closeButtonGO.AddComponent<Button>();

            // 绑定关闭事件
            detailCloseBtn.onClick.AddListener(() => {
                Debug.Log("[EncyclopediaUI] 详情面板关闭按钮被点击");
                CloseDetailPanel();
            });

            // 创建X文字
            GameObject closeTextGO = new GameObject("CloseText");
            closeTextGO.transform.SetParent(closeButtonGO.transform, false);

            RectTransform textRect = closeTextGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text closeText = closeTextGO.AddComponent<Text>();
            closeText.text = "×";
            closeText.fontSize = 18;
            closeText.color = Color.white;
            closeText.alignment = TextAnchor.MiddleCenter;
            closeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            Debug.Log("[EncyclopediaUI] 详情面板关闭按钮创建完成");
        }

        /// <summary>
        /// 为详情面板添加翻页按钮（如果还没有的话）
        /// </summary>
        private void AddNavigationButtonsIfNeeded()
        {
            if (detailPanel == null)
            {
                Debug.LogWarning("[EncyclopediaUI] 详情面板为空，无法添加翻页按钮");
                return;
            }

            // 清理现有按钮
            var existingPrevButton = detailPanel.transform.Find("PrevButton");
            var existingNextButton = detailPanel.transform.Find("NextButton");

            if (existingPrevButton != null)
            {
                DestroyImmediate(existingPrevButton.gameObject);
            }
            if (existingNextButton != null)
            {
                DestroyImmediate(existingNextButton.gameObject);
            }

            Debug.Log("[EncyclopediaUI] 为详情面板创建翻页按钮");

            // 创建左侧上一页按钮（三角形向左）- 底部中央偏左
            CreateTriangleNavigationButton(detailPanel.transform, "PrevButton", true, new Vector2(-100, 80), GoToPreviousEntry);

            // 创建右侧下一页按钮（三角形向右）- 底部中央偏右
            CreateTriangleNavigationButton(detailPanel.transform, "NextButton", false, new Vector2(100, 80), GoToNextEntry);

            // 更新按钮状态
            UpdateNavigationButtons();

            Debug.Log("[EncyclopediaUI] 翻页按钮创建完成");
        }

        /// <summary>
        /// 创建三角形翻页按钮
        /// </summary>
        private void CreateTriangleNavigationButton(Transform parent, string name, bool isLeftArrow, Vector2 position, System.Action onClick)
        {
            GameObject buttonGO = new GameObject(name);
            buttonGO.transform.SetParent(parent, false);

            // 设置RectTransform（都锚定到底部中央）
            RectTransform buttonRect = buttonGO.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0f);
            buttonRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0f);
            buttonRect.anchoredPosition = position;
            buttonRect.sizeDelta = new Vector2(180, 240); // 放大三倍：60*3=180, 80*3=240

            // 添加Button组件（透明背景）
            Button button = buttonGO.AddComponent<Button>();
            button.onClick.AddListener(() => onClick());

            // 创建三角形图像
            GameObject triangleGO = new GameObject("Triangle");
            triangleGO.transform.SetParent(buttonGO.transform, false);

            RectTransform triangleRect = triangleGO.AddComponent<RectTransform>();
            triangleRect.anchorMin = Vector2.zero;
            triangleRect.anchorMax = Vector2.one;
            triangleRect.offsetMin = Vector2.zero;
            triangleRect.offsetMax = Vector2.zero;

            // 使用Text组件显示三角形箭头
            Text triangleText = triangleGO.AddComponent<Text>();
            triangleText.text = isLeftArrow ? "◀" : "▶";
            triangleText.fontSize = 120; // 放大三倍：40*3=120
            triangleText.color = new Color(0.9f, 0.9f, 0.9f, 0.9f); // 白色半透明
            triangleText.alignment = TextAnchor.MiddleCenter;
            triangleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            Debug.Log($"[EncyclopediaUI] 创建三角形按钮: {name}, 位置: {position}, 左箭头: {isLeftArrow}");
        }

        /// <summary>
        /// 更新翻页按钮状态
        /// </summary>
        private void UpdateNavigationButtons()
        {
            if (detailPanel == null) return;

            var prevButton = detailPanel.transform.Find("PrevButton")?.GetComponent<Button>();
            var nextButton = detailPanel.transform.Find("NextButton")?.GetComponent<Button>();

            if (prevButton != null)
            {
                prevButton.interactable = currentEntryIndex > 0;
                var prevText = prevButton.GetComponentInChildren<Text>();
                if (prevText != null)
                {
                    prevText.color = prevButton.interactable ?
                        new Color(0.9f, 0.9f, 0.9f, 0.9f) :
                        new Color(0.4f, 0.4f, 0.4f, 0.5f);
                }
            }

            if (nextButton != null)
            {
                nextButton.interactable = currentEntryIndex < currentFilteredEntries.Count - 1;
                var nextText = nextButton.GetComponentInChildren<Text>();
                if (nextText != null)
                {
                    nextText.color = nextButton.interactable ?
                        new Color(0.9f, 0.9f, 0.9f, 0.9f) :
                        new Color(0.4f, 0.4f, 0.4f, 0.5f);
                }
            }

            Debug.Log($"[EncyclopediaUI] 翻页按钮状态更新: 上一页={prevButton?.interactable}, 下一页={nextButton?.interactable}");
        }

        /// <summary>
        /// 跳转到上一个条目
        /// </summary>
        private void GoToPreviousEntry()
        {
            if (currentEntryIndex > 0 && currentFilteredEntries.Count > 0)
            {
                currentEntryIndex--;
                var prevEntry = currentFilteredEntries[currentEntryIndex];
                selectedEntry = prevEntry;
                ShowEntryDetails(prevEntry);
                Debug.Log($"[EncyclopediaUI] 翻页到上一个条目: {prevEntry.GetFormattedDisplayName()}");
            }
        }

        /// <summary>
        /// 跳转到下一个条目
        /// </summary>
        private void GoToNextEntry()
        {
            if (currentEntryIndex < currentFilteredEntries.Count - 1 && currentFilteredEntries.Count > 0)
            {
                currentEntryIndex++;
                var nextEntry = currentFilteredEntries[currentEntryIndex];
                selectedEntry = nextEntry;
                ShowEntryDetails(nextEntry);
                Debug.Log($"[EncyclopediaUI] 翻页到下一个条目: {nextEntry.GetFormattedDisplayName()}");
            }
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
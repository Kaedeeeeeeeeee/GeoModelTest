using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.InputSystem;
using SampleCuttingSystem;

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
        [SerializeField] private Sample3DModelViewer model3DViewer;

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

            // 监听语言变化事件
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
            }

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
            // 调试：检查所有关键UI组件引用
            Debug.Log($"[EncyclopediaUI] UI组件引用检查:");
            Debug.Log($"[EncyclopediaUI] - encyclopediaPanel: {(encyclopediaPanel != null ? encyclopediaPanel.name : "null")}");
            Debug.Log($"[EncyclopediaUI] - detailPanel: {(detailPanel != null ? detailPanel.name : "null")}");
            Debug.Log($"[EncyclopediaUI] - layerTabContainer: {(layerTabContainer != null ? layerTabContainer.name : "null")}");
            Debug.Log($"[EncyclopediaUI] - entryListContainer: {(entryListContainer != null ? entryListContainer.name : "null")}");

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

                // 立即更新地层标签的语言显示
                UpdateLayerTabsLanguage();
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

                // 立即更新地层标签的语言显示
                UpdateLayerTabsLanguage();
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
                // 保存当前选中的值
                int currentTypeValue = entryTypeFilter.value;

                // 清除现有的监听器和选项
                entryTypeFilter.onValueChanged.RemoveAllListeners();
                entryTypeFilter.ClearOptions();

                var typeOptions = new List<string> {
                    LocalizationManager.Instance.GetText("encyclopedia.ui.filter.all"),
                    LocalizationManager.Instance.GetText("encyclopedia.ui.filter.mineral"),
                    LocalizationManager.Instance.GetText("encyclopedia.ui.filter.fossil")
                };
                entryTypeFilter.AddOptions(typeOptions);

                // 恢复选中的值
                entryTypeFilter.value = currentTypeValue;

                // 重新添加监听器
                entryTypeFilter.onValueChanged.AddListener(OnEntryTypeFilterChanged);
                // 增大Dropdown字体
                var entryTypeText = entryTypeFilter.GetComponentInChildren<Text>();
                if (entryTypeText != null) entryTypeText.fontSize = 20;
            }

            // 稀有度筛选
            if (rarityFilter != null)
            {
                // 保存当前选中的值
                int currentRarityValue = rarityFilter.value;

                // 清除现有的监听器和选项
                rarityFilter.onValueChanged.RemoveAllListeners();
                rarityFilter.ClearOptions();

                var rarityOptions = new List<string> {
                    LocalizationManager.Instance.GetText("encyclopedia.ui.filter.all"),
                    LocalizationManager.Instance.GetText("encyclopedia.ui.rarity.common"),
                    LocalizationManager.Instance.GetText("encyclopedia.ui.rarity.uncommon"),
                    LocalizationManager.Instance.GetText("encyclopedia.ui.rarity.rare")
                };
                rarityFilter.AddOptions(rarityOptions);

                // 恢复选中的值
                rarityFilter.value = currentRarityValue;

                // 重新添加监听器
                rarityFilter.onValueChanged.AddListener(OnRarityFilterChanged);
                // 增大Dropdown字体
                var rarityText = rarityFilter.GetComponentInChildren<Text>();
                if (rarityText != null) rarityText.fontSize = 20;
            }

            // 搜索输入
            if (searchInput != null)
            {
                // 清除现有监听器
                searchInput.onValueChanged.RemoveAllListeners();

                // 更新placeholder文本
                var placeholder = searchInput.placeholder.GetComponent<Text>();
                if (placeholder != null)
                {
                    placeholder.text = LocalizationManager.Instance.GetText("encyclopedia.ui.search_placeholder");
                }

                // 重新添加监听器
                searchInput.onValueChanged.AddListener(OnSearchInputChanged);
                // 增大InputField字体
                var inputText = searchInput.GetComponentInChildren<Text>();
                if (inputText != null) inputText.fontSize = 20;
            }

            // 清除筛选按钮
            if (clearFiltersButton != null)
            {
                // 清除现有监听器
                clearFiltersButton.onClick.RemoveAllListeners();

                // 更新按钮文本
                var buttonText = clearFiltersButton.GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    buttonText.text = LocalizationManager.Instance.GetText("encyclopedia.ui.clear_filters");
                    buttonText.fontSize = 20;
                }

                // 重新添加监听器
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

                    // 检查是否为标题（"地层名称"）
                    if (button == null && text != null && text.text == "地层名称")
                    {
                        // 更新标题为本地化文本
                        var localizedText = text.GetComponent<LocalizedText>();
                        if (localizedText == null)
                        {
                            localizedText = text.gameObject.AddComponent<LocalizedText>();
                            localizedText.TextKey = "encyclopedia.ui.layer_section_title";
                            Debug.Log("[EncyclopediaUI] 为现有标题添加本地化组件");
                        }
                        continue;
                    }

                    if (button != null && text != null && text.text != "地层名称")
                    {
                        layerTabs.Add(button);
                        Debug.Log($"[EncyclopediaUI] 收集到现有地层标签: {text.text}");

                        // 当前显示的是中文名称，需要映射到EncyclopediaData中的对应名称
                        string displayedName = text.text;
                        string dataLayerName = FindDataLayerName(displayedName, i);

                        // 更新为本地化显示文本
                        string localizedLayerName = GetLocalizedLayerName(dataLayerName);
                        text.text = localizedLayerName;

                        Debug.Log($"[EncyclopediaUI] 更新地层标签: 显示[{displayedName}] -> 数据[{dataLayerName}] -> 本地化[{localizedLayerName}]");

                        // 重新绑定点击事件，使用EncyclopediaData中的名称作为标识符
                        button.onClick.RemoveAllListeners();
                        button.onClick.AddListener(() => OnLayerTabClicked(dataLayerName));
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
                // 使用本地化的地层名称
                string localizedLayerName = GetLocalizedLayerName(layerName);
                buttonText.text = localizedLayerName;
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

            // 检查Canvas和GraphicRaycaster设置
            CheckCanvasRaycastSettings();

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

            // 按日文假名顺序排序：先按中间词（岩石类型），再按最后词（矿物名称）
            var sortedEntries = entries.OrderBy(e => e.entryType)
                         .ThenBy(e => GetRockTypeForSorting(e))
                         .ThenBy(e => GetMineralNameForSorting(e))
                         .ToList();

            Debug.Log($"[EncyclopediaUI] 最终条目数: {sortedEntries.Count}");

            // 调试输出前5个条目的排序信息
            for (int i = 0; i < Math.Min(5, sortedEntries.Count); i++)
            {
                var entry = sortedEntries[i];
                Debug.Log($"[EncyclopediaUI] 排序 {i+1}: {entry.displayName} | 岩石排序键: {GetRockTypeForSorting(entry)} | 矿物排序键: {GetMineralNameForSorting(entry)}");
            }
            return sortedEntries;
        }

        /// <summary>
        /// 创建条目UI项
        /// </summary>
        private void CreateEntryItem(EncyclopediaEntry entry)
        {
            if (entryItemPrefab == null || entryListContainer == null)
            {
                Debug.LogError("[EncyclopediaUI] 必要组件缺失，无法创建条目");
                return;
            }

            // 实例化条目预制体
            var itemGO = Instantiate(entryItemPrefab, entryListContainer);
            entryItems.Add(itemGO);

            // 确保有Button组件
            var button = itemGO.GetComponent<Button>();
            if (button == null)
            {
                button = itemGO.AddComponent<Button>();
                Debug.Log($"[EncyclopediaUI] 为条目添加Button组件: {entry.GetFormattedDisplayName()}");
            }

            // 确保有可交互的背景
            var image = itemGO.GetComponent<Image>();
            if (image == null)
            {
                image = itemGO.AddComponent<Image>();
                image.color = new Color(0.15f, 0.2f, 0.3f, 0.8f);
            }

            // 设置条目信息
            SetupEntryText(itemGO, entry);
            SetupEntryVisuals(itemGO, entry);

            // 绑定点击事件（最重要的部分）
            SetupEntryClickEvent(button, entry);

            Debug.Log($"[EncyclopediaUI] 成功创建条目: {entry.GetFormattedDisplayName()}");
        }

        /// <summary>
        /// 设置条目文本信息
        /// </summary>
        private void SetupEntryText(GameObject itemGO, EncyclopediaEntry entry)
        {
            var nameText = itemGO.transform.Find("NameText")?.GetComponent<Text>();
            if (nameText != null)
            {
                string localizedName = GetLocalizedEntryName(entry);
                nameText.text = localizedName;
                nameText.color = entry.isDiscovered ? Color.white : Color.gray;
                nameText.fontSize = 24;
            }

            var rarityText = itemGO.transform.Find("RarityText")?.GetComponent<Text>();
            if (rarityText != null)
            {
                rarityText.text = entry.GetRarityText();
                rarityText.color = entry.GetRarityColor();
                rarityText.fontSize = 18;
            }
        }

        /// <summary>
        /// 设置条目视觉效果
        /// </summary>
        private void SetupEntryVisuals(GameObject itemGO, EncyclopediaEntry entry)
        {
            var statusImage = itemGO.transform.Find("StatusImage")?.GetComponent<Image>();
            if (statusImage != null)
            {
                statusImage.color = entry.isDiscovered ? Color.green : Color.red;
            }

            // 隐藏图标（如果存在）
            var iconImage = itemGO.transform.Find("IconImage")?.GetComponent<Image>();
            if (iconImage != null)
            {
                iconImage.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 设置条目点击事件（核心功能）
        /// </summary>
        private void SetupEntryClickEvent(Button button, EncyclopediaEntry entry)
        {
            if (button == null) return;

            // 清除所有现有监听器
            button.onClick.RemoveAllListeners();

            // 确保按钮可交互
            button.interactable = true;

            // 添加点击事件
            button.onClick.AddListener(() => {
                Debug.Log($"[EncyclopediaUI] 条目被点击: {entry.GetFormattedDisplayName()}");
                OpenEntryDetails(entry);
            });

            Debug.Log($"[EncyclopediaUI] ✓ 已为条目绑定点击事件: {entry.GetFormattedDisplayName()}");
        }

        /// <summary>
        /// 打开条目详情（新的核心方法）
        /// </summary>
        private void OpenEntryDetails(EncyclopediaEntry entry)
        {
            if (entry == null)
            {
                Debug.LogError("[EncyclopediaUI] 条目为null，无法显示详情");
                return;
            }

            // 确保详情面板存在
            if (!EnsureDetailPanelExists())
            {
                Debug.LogError("[EncyclopediaUI] 无法创建或找到详情面板");
                return;
            }

            selectedEntry = entry;
            currentFilteredEntries = GetFilteredEntries();
            currentEntryIndex = currentFilteredEntries.FindIndex(e => e.id == entry.id);

            Debug.Log($"[EncyclopediaUI] 打开详情: {entry.GetFormattedDisplayName()}");

            // 在显示前确保面板是全屏的（避免闪烁）
            EnsureDetailPanelFullscreen();

            // 显示详情面板
            detailPanel.SetActive(true);

            // 设置详情内容
            SetDetailContent(entry);

            // 添加翻页按钮
            AddNavigationButtonsIfNeeded();

            Debug.Log("[EncyclopediaUI] 详情面板已打开");
        }

        /// <summary>
        /// 确保详情面板存在并正确配置
        /// </summary>
        private bool EnsureDetailPanelExists()
        {
            if (detailPanel != null)
            {
                // 确保现有面板是全屏的
                EnsureDetailPanelFullscreen();
                return true;
            }

            // 尝试在场景中查找DetailPanel
            var foundPanel = GameObject.Find("DetailPanel");
            if (foundPanel != null)
            {
                detailPanel = foundPanel;
                Debug.Log("[EncyclopediaUI] 在场景中找到DetailPanel，立即设置为全屏");

                // 立即设置为全屏，避免闪烁
                EnsureDetailPanelFullscreen();

                // 确保有3D查看器
                Ensure3DViewerExists();

                return true;
            }

            // 如果没找到，创建一个简单的详情面板
            return CreateSimpleDetailPanel();
        }

        /// <summary>
        /// 确保详情面板为全屏显示
        /// </summary>
        private void EnsureDetailPanelFullscreen()
        {
            if (detailPanel == null) return;

            var rectTransform = detailPanel.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                Debug.Log($"[EncyclopediaUI] 修复详情面板尺寸: {rectTransform.anchorMin}-{rectTransform.anchorMax} → 全屏");

                // 立即设置为全屏
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
            }

            // 修复背景为不透明
            var background = detailPanel.GetComponent<Image>();
            if (background != null)
            {
                var oldColor = background.color;
                background.color = new Color(oldColor.r, oldColor.g, oldColor.b, 1.0f);
            }
        }

        /// <summary>
        /// 创建简单的详情面板
        /// </summary>
        private bool CreateSimpleDetailPanel()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[EncyclopediaUI] 找不到Canvas，无法创建详情面板");
                return false;
            }

            // 创建详情面板
            detailPanel = new GameObject("DetailPanel");
            detailPanel.transform.SetParent(canvas.transform, false);

            var rect = detailPanel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var background = detailPanel.AddComponent<Image>();
            background.color = new Color(0.05f, 0.1f, 0.15f, 1.0f);

            // 创建标题
            CreateDetailTitle();

            // 创建描述
            CreateDetailDescription();

            // 创建属性
            CreateDetailProperties();

            // 创建3D模型查看器
            Create3DModelViewer();

            // 创建关闭按钮
            CreateDetailCloseButton();

            detailPanel.SetActive(false);

            Debug.Log("[EncyclopediaUI] 创建了简单的详情面板");
            return true;
        }

        /// <summary>
        /// 创建详情面板标题
        /// </summary>
        private void CreateDetailTitle()
        {
            var titleGO = new GameObject("DetailTitle");
            titleGO.transform.SetParent(detailPanel.transform, false);

            var rect = titleGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0.9f);
            rect.anchorMax = new Vector2(1, 1);
            rect.offsetMin = new Vector2(20, 0);
            rect.offsetMax = new Vector2(-20, -10);

            detailTitle = titleGO.AddComponent<Text>();
            detailTitle.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            detailTitle.fontSize = 32;  // 更大的标题字体大小
            detailTitle.color = Color.white;
            detailTitle.alignment = TextAnchor.MiddleLeft;
            detailTitle.fontStyle = FontStyle.Bold;
        }

        /// <summary>
        /// 创建详情面板描述
        /// </summary>
        private void CreateDetailDescription()
        {
            var descGO = new GameObject("DetailDescription");
            descGO.transform.SetParent(detailPanel.transform, false);

            var rect = descGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0.6f);
            rect.anchorMax = new Vector2(1, 0.85f);
            rect.offsetMin = new Vector2(20, 0);
            rect.offsetMax = new Vector2(-20, 0);

            detailDescription = descGO.AddComponent<Text>();
            detailDescription.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            detailDescription.fontSize = 48;  // 从24增加到48，放大一倍
            detailDescription.color = Color.white;
            detailDescription.alignment = TextAnchor.UpperLeft;
        }

        /// <summary>
        /// 创建详情面板属性
        /// </summary>
        private void CreateDetailProperties()
        {
            var propsGO = new GameObject("DetailProperties");
            propsGO.transform.SetParent(detailPanel.transform, false);

            var rect = propsGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0.2f);
            rect.anchorMax = new Vector2(1, 0.55f);
            rect.offsetMin = new Vector2(20, 0);
            rect.offsetMax = new Vector2(-20, 0);

            detailProperties = propsGO.AddComponent<Text>();
            detailProperties.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            detailProperties.fontSize = 44;  // 从22增加到44，放大一倍
            detailProperties.color = Color.white;
            detailProperties.alignment = TextAnchor.UpperLeft;
        }

        /// <summary>
        /// 创建详情面板关闭按钮
        /// </summary>
        private void CreateDetailCloseButton()
        {
            var closeGO = new GameObject("DetailCloseButton");
            closeGO.transform.SetParent(detailPanel.transform, false);

            var rect = closeGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-75, -75);  // 更远离边角
            rect.sizeDelta = new Vector2(120, 120);  // 更大的按钮

            var background = closeGO.AddComponent<Image>();
            background.color = new Color(0.8f, 0.2f, 0.2f, 0.8f);

            var button = closeGO.AddComponent<Button>();
            button.onClick.AddListener(() => {
                Debug.Log("[EncyclopediaUI] 关闭按钮被点击");
                CloseDetailPanel();
            });

            // 添加X文字
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(closeGO.transform, false);

            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textGO.AddComponent<Text>();
            text.text = "×";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 72;  // 更大的字体
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontStyle = FontStyle.Bold;
        }

        /// <summary>
        /// 确保3D查看器存在
        /// </summary>
        private void Ensure3DViewerExists()
        {
            Debug.Log($"[EncyclopediaUI] Ensure3DViewerExists开始 - detailPanel: {detailPanel != null}");

            if (detailPanel == null)
            {
                Debug.LogError("[EncyclopediaUI] detailPanel为空，无法创建3D查看器");
                return;
            }

            // 检查是否已经有model3DViewer引用
            if (model3DViewer == null)
            {
                Debug.Log("[EncyclopediaUI] model3DViewer为空，尝试查找现有组件");
                // 尝试在详情面板中查找现有的Sample3DModelViewer
                model3DViewer = detailPanel.GetComponentInChildren<Sample3DModelViewer>();
                Debug.Log($"[EncyclopediaUI] 查找结果: {model3DViewer != null}");
            }

            // 如果还是没有，创建一个新的
            if (model3DViewer == null)
            {
                Debug.Log("[EncyclopediaUI] 详情面板没有3D查看器，创建新的Sample3DModelViewer");
                Create3DModelViewer();
                Debug.Log($"[EncyclopediaUI] 创建完成后 model3DViewer: {model3DViewer != null}");
            }
            else
            {
                Debug.Log("[EncyclopediaUI] 找到现有的Sample3DModelViewer");
            }
        }

        /// <summary>
        /// 创建3D模型查看器
        /// </summary>
        private void Create3DModelViewer()
        {
            Debug.Log("[EncyclopediaUI] 创建Sample3DModelViewer");

            // 创建3D查看器容器
            var viewerGO = new GameObject("Encyclopedia3DModelViewer");
            viewerGO.transform.SetParent(detailPanel.transform, false);

            var rect = viewerGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.55f, 0.15f);  // 增大查看器区域
            rect.anchorMax = new Vector2(0.95f, 0.9f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // 添加背景
            var background = viewerGO.AddComponent<Image>();
            background.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);  // 深色背景

            // 创建RawImage用于显示3D模型
            var rawImageGO = new GameObject("ModelDisplayImage");
            rawImageGO.transform.SetParent(viewerGO.transform, false);

            var rawImageRect = rawImageGO.AddComponent<RectTransform>();
            rawImageRect.anchorMin = Vector2.zero;
            rawImageRect.anchorMax = Vector2.one;
            rawImageRect.offsetMin = Vector2.zero;
            rawImageRect.offsetMax = Vector2.zero;

            var rawImage = rawImageGO.AddComponent<RawImage>();

            // 添加经过验证的Sample3DModelViewer组件
            model3DViewer = viewerGO.AddComponent<Sample3DModelViewer>();

            // 直接设置RawImage引用（Sample3DModelViewer使用public字段）
            model3DViewer.rawImage = rawImage;

            // 调整相机距离，让模型显示得更远一些
            float oldDistance = model3DViewer.cameraDistance;
            model3DViewer.cameraDistance = 3.0f;  // 最终确定的相机距离
            Debug.Log($"[3D模型距离] 相机距离调整: {oldDistance} -> {model3DViewer.cameraDistance}");

            // 设置鼠标交互事件监听器
            var eventTrigger = rawImageGO.AddComponent<UnityEngine.EventSystems.EventTrigger>();

            // 鼠标进入事件
            var entryEvent = new UnityEngine.EventSystems.EventTrigger.Entry();
            entryEvent.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
            entryEvent.callback.AddListener((data) => { model3DViewer.SetMouseOverArea(true); });
            eventTrigger.triggers.Add(entryEvent);

            // 鼠标离开事件
            var exitEvent = new UnityEngine.EventSystems.EventTrigger.Entry();
            exitEvent.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
            exitEvent.callback.AddListener((data) => { model3DViewer.SetMouseOverArea(false); });
            eventTrigger.triggers.Add(exitEvent);

            // 创建关闭按钮
            CreateDetailPanelCloseButton(viewerGO);

            Debug.Log("[EncyclopediaUI] Sample3DModelViewer创建完成，包含交互支持和关闭按钮");
        }

        /// <summary>
        /// 创建详情面板关闭按钮
        /// </summary>
        private void CreateDetailPanelCloseButton(GameObject parent)
        {
            GameObject closeButtonGO = new GameObject("DetailCloseButton");
            // 将按钮设置为detailPanel的子对象，而不是viewer的子对象
            closeButtonGO.transform.SetParent(detailPanel.transform, false);

            RectTransform buttonRect = closeButtonGO.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(1, 1);
            buttonRect.anchorMax = new Vector2(1, 1);
            buttonRect.pivot = new Vector2(1, 1);
            buttonRect.anchoredPosition = new Vector2(-20, -20);  // 更靠近右上角
            buttonRect.sizeDelta = new Vector2(50, 50);  // 稍微大一点，更容易点击

            // 按钮背景
            Image buttonBg = closeButtonGO.AddComponent<Image>();
            buttonBg.color = new Color(0.8f, 0.2f, 0.2f, 0.9f);  // 红色背景

            // 按钮组件
            Button button = closeButtonGO.AddComponent<Button>();
            button.targetGraphic = buttonBg;
            button.onClick.AddListener(CloseDetailPanel);

            // 添加悬停效果
            var colors = button.colors;
            colors.highlightedColor = new Color(1f, 0.3f, 0.3f, 1f);
            colors.pressedColor = new Color(0.6f, 0.1f, 0.1f, 1f);
            button.colors = colors;

            // 创建X符号文字
            GameObject textGO = new GameObject("CloseText");
            textGO.transform.SetParent(closeButtonGO.transform, false);

            RectTransform textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text text = textGO.AddComponent<Text>();
            text.text = "×";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 24;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontStyle = FontStyle.Bold;

            // 确保关闭按钮显示在最顶层
            closeButtonGO.transform.SetAsLastSibling();

            Debug.Log("[EncyclopediaUI] 详情面板关闭按钮创建完成，位置更靠近右上角");
        }

        /// <summary>
        /// 设置详情内容
        /// </summary>
        private void SetDetailContent(EncyclopediaEntry entry)
        {
            if (detailTitle != null)
            {
                detailTitle.text = GetLocalizedEntryName(entry);
                detailTitle.fontSize = 32;  // 更大的标题字体大小
            }

            if (detailDescription != null)
            {
                // 尝试获取本地化描述
                string localizedDescription = GetLocalizedDescription(entry);
                Debug.Log($"[EncyclopediaUI] 矿物 {entry.displayName} 本地化描述: '{localizedDescription}'");
                detailDescription.text = !string.IsNullOrEmpty(localizedDescription) ?
                    localizedDescription : LocalizationManager.Instance.GetText("encyclopedia.detail.no_description");
                detailDescription.fontSize = 22;  // 更大的描述字体大小
            }

            if (detailProperties != null)
            {
                detailProperties.text = GeneratePropertiesText(entry);
                detailProperties.fontSize = 20;  // 更大的属性字体大小
            }

            // 确保3D查看器存在
            Ensure3DViewerExists();

            // 显示3D模型
            Debug.Log($"[EncyclopediaUI] 3D模型检查 - model3DViewer: {model3DViewer != null}, isDiscovered: {entry.isDiscovered}, model3D: {entry.model3D != null}");
            if (model3DViewer != null && entry.isDiscovered && entry.model3D != null)
            {
                Debug.Log($"[EncyclopediaUI] 显示3D模型: {entry.model3D.name}");
                Debug.Log($"[3D模型距离] 当前相机距离: {model3DViewer.cameraDistance}");
                model3DViewer.ShowSampleModel(entry.model3D);
                Debug.Log($"[3D模型距离] 模型显示后相机距离: {model3DViewer.cameraDistance}");

                // 修复首次显示白屏问题：延迟强制渲染确保内容正确显示
                StartCoroutine(ForceRenderAfterDelay(model3DViewer));
            }
            else if (model3DViewer != null)
            {
                Debug.Log("[EncyclopediaUI] 清除3D模型");
                model3DViewer.ClearCurrentModel();
            }

            // 隐藏图标（我们没有为矿物和化石准备图标）
            if (detailIcon != null)
            {
                detailIcon.gameObject.SetActive(false);
            }

            // 更新翻页按钮状态
            UpdateNavigationButtons();

            Debug.Log($"[EncyclopediaUI] 详情内容已设置: {entry.GetFormattedDisplayName()}");
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
            Debug.Log($"[EncyclopediaUI] 所有详情面板组件状态检查:");
            Debug.Log($"[EncyclopediaUI] - detailPanel: {(detailPanel != null ? detailPanel.name : "null")}");
            Debug.Log($"[EncyclopediaUI] - detailTitle: {(detailTitle != null ? detailTitle.name : "null")}");
            Debug.Log($"[EncyclopediaUI] - detailDescription: {(detailDescription != null ? detailDescription.name : "null")}");
            Debug.Log($"[EncyclopediaUI] - detailProperties: {(detailProperties != null ? detailProperties.name : "null")}");

            if (detailPanel == null)
            {
                Debug.LogError("[EncyclopediaUI] detailPanel为null! 这表明Inspector中的引用丢失了!");
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

            // 设置合适的字体大小
            if (detailTitle != null)
            {
                detailTitle.fontSize = 32;  // 更大的标题字体
            }
            if (detailDescription != null)
            {
                detailDescription.fontSize = 22;  // 更大的描述字体
            }
            if (detailProperties != null)
            {
                detailProperties.fontSize = 20;  // 更大的属性字体
            }
            Debug.Log("[EncyclopediaUI] 字体大小已调整");

            // 添加关闭按钮（如果不存在）
            AddCloseButtonIfNeeded();

            // 设置标题
            if (detailTitle != null)
            {
                detailTitle.text = GetLocalizedEntryName(entry);
            }

            // 隐藏图标（我们没有为矿物和化石准备图标）
            if (detailIcon != null)
            {
                detailIcon.gameObject.SetActive(false);
            }

            // 设置描述
            if (detailDescription != null)
            {
                // 使用本地化描述，保持与SetDetailContent方法一致
                string localizedDescription = GetLocalizedDescription(entry);
                Debug.Log($"[EncyclopediaUI] ShowEntryDetails矿物 {entry.displayName} 本地化描述: '{localizedDescription}'");
                detailDescription.text = !string.IsNullOrEmpty(localizedDescription) ?
                    localizedDescription : LocalizationManager.Instance.GetText("encyclopedia.detail.no_description");
            }

            // 设置属性信息
            if (detailProperties != null)
            {
                detailProperties.text = GeneratePropertiesText(entry);
            }

            // 显示3D模型
            Debug.Log($"[EncyclopediaUI] 3D模型检查 - model3DViewer: {model3DViewer != null}, isDiscovered: {entry.isDiscovered}, model3D: {entry.model3D != null}");
            if (model3DViewer != null && entry.isDiscovered && entry.model3D != null)
            {
                Debug.Log($"[EncyclopediaUI] 显示3D模型: {entry.model3D.name}");
                Debug.Log($"[3D模型距离] 当前相机距离: {model3DViewer.cameraDistance}");
                model3DViewer.ShowSampleModel(entry.model3D);
                Debug.Log($"[3D模型距离] 模型显示后相机距离: {model3DViewer.cameraDistance}");

                // 修复首次显示白屏问题：延迟强制渲染确保内容正确显示
                StartCoroutine(ForceRenderAfterDelay(model3DViewer));
            }
            else if (model3DViewer != null)
            {
                Debug.Log("[EncyclopediaUI] 清除3D模型");
                model3DViewer.ClearCurrentModel();
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
                return LocalizationManager.Instance.GetText("encyclopedia.detail.undiscovered");
            }

            var properties = new List<string>();

            properties.Add($"{LocalizationManager.Instance.GetText("encyclopedia.detail.type")}: {entry.GetEntryTypeText()}");
            properties.Add($"{LocalizationManager.Instance.GetText("encyclopedia.detail.layer")}: {GetLocalizedLayerName(entry.layerName)}");

            if (entry.entryType == EntryType.Mineral)
            {
                properties.Add($"{LocalizationManager.Instance.GetText("encyclopedia.detail.rock")}: {GetLocalizedRockName(entry.rockName)}");
                properties.Add($"{LocalizationManager.Instance.GetText("encyclopedia.detail.content")}: {entry.percentage:P1}");

                if (!string.IsNullOrEmpty(entry.mohsHardness))
                    properties.Add($"{LocalizationManager.Instance.GetText("encyclopedia.detail.mohs_hardness")}: {entry.mohsHardness}");
                if (!string.IsNullOrEmpty(entry.density))
                    properties.Add($"{LocalizationManager.Instance.GetText("encyclopedia.detail.density")}: {entry.density}");
                if (!string.IsNullOrEmpty(entry.magnetism))
                    properties.Add($"{LocalizationManager.Instance.GetText("encyclopedia.detail.magnetism")}: {GetLocalizedPropertyValue(entry.magnetism)}");
            }
            else
            {
                properties.Add($"{LocalizationManager.Instance.GetText("encyclopedia.detail.rarity")}: {entry.GetRarityText()}");
                properties.Add($"{LocalizationManager.Instance.GetText("encyclopedia.detail.discovery_probability")}: {entry.discoveryProbability:P1}");
            }

            if (entry.discoveryCount > 0)
            {
                properties.Add($"{LocalizationManager.Instance.GetText("encyclopedia.detail.discovery_count")}: {entry.discoveryCount}");
                properties.Add($"{LocalizationManager.Instance.GetText("encyclopedia.detail.first_discovered")}: {entry.firstDiscoveredTime:yyyy-MM-dd}");
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

            // 使用本地化文本
            string progressText = LocalizationManager.Instance.GetText("encyclopedia.ui.collection_progress",
                stats.discoveredEntries.ToString(), stats.totalEntries.ToString(), stats.overallProgress.ToString("P1"));
            string mineralsText = LocalizationManager.Instance.GetText("encyclopedia.ui.minerals_progress",
                stats.discoveredMinerals.ToString(), stats.totalMinerals.ToString(), stats.mineralProgress.ToString("P1"));
            string fossilsText = LocalizationManager.Instance.GetText("encyclopedia.ui.fossils_progress",
                stats.discoveredFossils.ToString(), stats.totalFossils.ToString(), stats.fossilProgress.ToString("P1"));

            statisticsText.text = $"{progressText}\n{mineralsText}\n{fossilsText}";
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
        /// 获取本地化的地层名称
        /// </summary>
        private string GetLocalizedLayerName(string layerName)
        {
            if (string.IsNullOrEmpty(layerName) || LocalizationManager.Instance == null)
                return layerName;

            // 日文地层名称到英文键值的映射
            var layerMappings = new Dictionary<string, string>
            {
                {"青葉山層", "encyclopedia.layer.aoba_mountain"},
                {"大年寺層", "encyclopedia.layer.dainenji"},
                {"向山層", "encyclopedia.layer.mukoyama"},
                {"広瀬川凝灰岩部層", "encyclopedia.layer.hirose_river_tuff"},
                {"竜ノ口層", "encyclopedia.layer.ryunokuchi"},
                {"亀岡層", "encyclopedia.layer.kameoka"}
            };

            // 尝试直接映射
            if (layerMappings.TryGetValue(layerName, out string key))
            {
                string localizedName = LocalizationManager.Instance.GetText(key);
                return localizedName == key ? layerName : localizedName;
            }

            // 如果没有映射，返回原始名称
            return layerName;
        }

        /// <summary>
        /// 获取本地化的岩石名称
        /// </summary>
        private string GetLocalizedRockName(string rockName)
        {
            if (string.IsNullOrEmpty(rockName) || LocalizationManager.Instance == null)
                return rockName;

            // 中文岩石名称到英文键值的映射
            var rockMappings = new Dictionary<string, string>
            {
                {"砾岩", "rock.conglomerate"},
                {"火山灰", "rock.volcanic_ash"},
                {"粉砂岩/砂岩", "rock.siltstone_sandstone"},
                {"砂岩/粉砂岩", "rock.sandstone_siltstone"},
                {"英安岩质熔结凝灰岩", "rock.dacitic_welded_tuff"},
                {"粉砂岩/细粒砂岩", "rock.siltstone_fine_sandstone"},
                {"凝灰岩", "rock.tuff"},
                {"凝灰质砂岩", "rock.tuffaceous_sandstone"},
                {"粉砂岩", "rock.siltstone"}
            };

            // 首先尝试直接映射
            if (rockMappings.TryGetValue(rockName, out string key))
            {
                string localizedName = LocalizationManager.Instance.GetText(key);
                return localizedName == key ? rockName : localizedName;
            }

            // 如果没有直接映射，尝试构建键值
            string generatedKey = $"rock.{rockName.ToLower().Replace(" ", "_").Replace("/", "_")}";
            string generatedName = LocalizationManager.Instance.GetText(generatedKey);

            // 如果找不到本地化，返回原始名称
            return generatedName == generatedKey ? rockName : generatedName;
        }

        /// <summary>
        /// 获取本地化的属性值
        /// </summary>
        private string GetLocalizedPropertyValue(string propertyValue)
        {
            if (string.IsNullOrEmpty(propertyValue) || LocalizationManager.Instance == null)
                return propertyValue;

            // 映射常见的属性值到本地化键值
            var propertyMappings = new Dictionary<string, string>
            {
                {"无", "encyclopedia.property.none"},
                {"无磁性", "encyclopedia.property.non_magnetic"},
                {"弱磁性", "encyclopedia.property.weak_magnetic"},
                {"强磁性", "encyclopedia.property.strong_magnetic"},
                {"抗磁性", "encyclopedia.property.diamagnetic"},
                {"顺磁性", "encyclopedia.property.weak_magnetic_paramagnetic"},
                {"无（抗磁性）", "encyclopedia.property.diamagnetic"},
                {"弱磁性（顺磁性）", "encyclopedia.property.weak_magnetic_paramagnetic"},
                {"弱磁性（顺磁性）；部分晶种含铁磁性", "encyclopedia.property.weak_magnetic_paramagnetic"}
            };

            // 尝试直接匹配
            if (propertyMappings.TryGetValue(propertyValue, out string key))
            {
                string localizedValue = LocalizationManager.Instance.GetText(key);
                return localizedValue == key ? propertyValue : localizedValue;
            }

            // 处理复杂的磁性描述（包含分号的）
            if (propertyValue.Contains("弱磁性") && propertyValue.Contains("顺磁性"))
            {
                string weakMagnetic = LocalizationManager.Instance.GetText("encyclopedia.property.weak_magnetic_paramagnetic");
                return weakMagnetic == "encyclopedia.property.weak_magnetic_paramagnetic" ? propertyValue : weakMagnetic;
            }

            // 如果没有直接匹配，返回原始值
            return propertyValue;
        }

        /// <summary>
        /// 获取本地化的描述文本
        /// </summary>
        private string GetLocalizedDescription(EncyclopediaEntry entry)
        {
            if (LocalizationManager.Instance == null)
            {
                Debug.LogWarning("[EncyclopediaUI] LocalizationManager实例为空");
                return entry.description; // 备用：使用原始描述
            }

            // 构建本地化键值
            string localizationKey = GetDescriptionLocalizationKey(entry);
            Debug.Log($"[EncyclopediaUI] 获取本地化描述 - 矿物: {entry.displayName}, 键值: {localizationKey}, 当前语言: {LocalizationManager.Instance.CurrentLanguage}");

            // 尝试获取本地化文本
            string localizedText = LocalizationManager.Instance.GetText(localizationKey);
            Debug.Log($"[EncyclopediaUI] 本地化文本获取结果: '{localizedText}'");

            // 如果本地化文本就是键值本身或者是带方括号的键值（表示没找到），则尝试其他变体
            if (localizedText == localizationKey || localizedText == $"[{localizationKey}]")
            {
                // 尝试带后缀的变体（如 magnetite_simple, magnetite_detailed）
                string[] suffixes = { "_simple", "_detailed", "_short", "_long" };

                foreach (string suffix in suffixes)
                {
                    string variantKey = localizationKey + suffix;
                    string variantText = LocalizationManager.Instance.GetText(variantKey);

                    if (variantText != variantKey && variantText != $"[{variantKey}]")
                    {
                        return variantText; // 找到了变体，返回
                    }
                }

                // 如果所有变体都没找到，返回原始描述
                return entry.description;
            }

            return localizedText;
        }

        /// <summary>
        /// 获取描述的本地化键值
        /// </summary>
        private string GetDescriptionLocalizationKey(EncyclopediaEntry entry)
        {
            // 总是使用英文名称作为键值基础，因为本地化文件中的键值都是基于英文的
            string baseName = !string.IsNullOrEmpty(entry.nameEN) ? entry.nameEN : entry.displayName;

            // 特殊名称映射
            var specialMappings = new Dictionary<string, string>
            {
                {"Illite (Alteration Product)", "illite_alteration_product"},
                {"Clay Minerals", "clay_minerals"},
                {"Heavy Minerals", "heavy_minerals"},
                {"Volcanic Glass", "volcanic_glass"},
                {"Volcanic Ash", "volcanic_ash"},
                {"carbonaceous_matter", "carbonaceous_matter"}
            };

            // 检查特殊映射
            string entryName;
            if (specialMappings.TryGetValue(baseName, out string specialName))
            {
                entryName = specialName;
            }
            else
            {
                // 转换为键值格式
                entryName = baseName.ToLower().Replace(" ", "_").Replace("(", "").Replace(")", "");
            }

            if (entry.entryType == EntryType.Mineral)
            {
                return $"mineral.description.{entryName}";
            }
            else if (entry.entryType == EntryType.Fossil)
            {
                return $"fossil.description.{entryName}";
            }

            return $"description.{entryName}";
        }

        /// <summary>
        /// 根据当前语言设置获取本地化的条目名称（完整格式）
        /// </summary>
        private string GetLocalizedEntryName(EncyclopediaEntry entry)
        {
            if (LocalizationManager.Instance == null)
            {
                Debug.LogWarning("[EncyclopediaUI] LocalizationManager实例为空，使用默认名称");
                // 如果本地化管理器不可用，返回完整格式化名称
                return entry.GetFormattedDisplayName();
            }

            var currentLanguage = LocalizationManager.Instance.CurrentLanguage;
            Debug.Log($"[EncyclopediaUI] 获取本地化条目名称 - 矿物: {entry.displayName}, 当前语言: {currentLanguage}");

            // 获取本地化的地层名称
            string localizedLayerName = GetLocalizedLayerName(entry.layerName);

            // 获取本地化的岩石名称
            string localizedRockName = entry.entryType == EntryType.Mineral ? GetLocalizedRockName(entry.rockName) : "";

            // 获取本地化的矿物/化石名称
            string localizedMineral = "";
            switch (currentLanguage)
            {
                case LanguageSettings.Language.English:
                    localizedMineral = !string.IsNullOrEmpty(entry.nameEN) ? entry.nameEN : entry.displayName;
                    break;

                case LanguageSettings.Language.Japanese:
                    localizedMineral = !string.IsNullOrEmpty(entry.nameJA) ? entry.nameJA : entry.displayName;
                    break;

                case LanguageSettings.Language.ChineseSimplified:
                default:
                    localizedMineral = !string.IsNullOrEmpty(entry.nameCN) ? entry.nameCN : entry.displayName;
                    break;
            }

            // 构建完整的本地化格式化名称
            if (entry.entryType == EntryType.Mineral)
            {
                return $"{localizedLayerName}-{localizedRockName}-{localizedMineral}";
            }
            else
            {
                return $"{localizedLayerName}-{localizedMineral}";
            }
        }


        /// <summary>
        /// 根据显示名称和索引，找到EncyclopediaData中对应的地层名称
        /// </summary>
        private string FindDataLayerName(string displayedName, int containerIndex)
        {
            // 计算实际的地层索引（需要考虑标题占用的位置）
            int layerIndex = containerIndex;

            // 如果容器中第一个元素是标题（没有Button组件），则索引需要减1
            if (layerTabContainer != null && layerTabContainer.childCount > 0)
            {
                var firstChild = layerTabContainer.GetChild(0);
                if (firstChild.GetComponent<Button>() == null && firstChild.GetComponentInChildren<Text>() != null)
                {
                    // 第一个是标题，所以地层索引需要减1
                    layerIndex = containerIndex - 1;
                    Debug.Log($"[EncyclopediaUI] 检测到标题，调整索引: {containerIndex} -> {layerIndex}");
                }
            }

            // 首先尝试直接用调整后的索引获取（最可靠的方法）
            if (EncyclopediaData.Instance?.LayerNames != null &&
                layerIndex >= 0 && layerIndex < EncyclopediaData.Instance.LayerNames.Count)
            {
                Debug.Log($"[EncyclopediaUI] 通过索引 {layerIndex} 找到地层名称: {EncyclopediaData.Instance.LayerNames[layerIndex]}");
                return EncyclopediaData.Instance.LayerNames[layerIndex];
            }

            // 备用方法：尝试根据显示名称匹配
            if (EncyclopediaData.Instance?.LayerNames != null)
            {
                foreach (string dataLayerName in EncyclopediaData.Instance.LayerNames)
                {
                    // 直接名称匹配
                    if (dataLayerName == displayedName)
                    {
                        Debug.Log($"[EncyclopediaUI] 直接匹配找到地层名称: {dataLayerName}");
                        return dataLayerName;
                    }

                    // 检查是否为已知的地层名称（通过关键字匹配）
                    if (IsKnownLayerName(displayedName, dataLayerName))
                    {
                        Debug.Log($"[EncyclopediaUI] 关键字匹配找到地层名称: {displayedName} -> {dataLayerName}");
                        return dataLayerName;
                    }
                }
            }

            // 最后的备用：返回显示名称本身
            Debug.LogWarning($"[EncyclopediaUI] 无法找到显示名称 '{displayedName}' 对应的数据层名称，使用显示名称");
            return displayedName;
        }

        /// <summary>
        /// 检查是否为已知的地层名称
        /// </summary>
        private bool IsKnownLayerName(string displayedName, string dataLayerName)
        {
            // 检查显示名称是否包含已知地层的关键词
            var pairs = new[]
            {
                (new[] {"青葉", "青葉山", "Aoba"}, dataLayerName),
                (new[] {"大年寺", "Dainenji"}, dataLayerName),
                (new[] {"向山", "Mukoyama"}, dataLayerName),
                (new[] {"広瀬川", "Hirose"}, dataLayerName),
                (new[] {"竜ノ口", "Ryunokuchi"}, dataLayerName),
                (new[] {"亀岡", "Kameoka"}, dataLayerName)
            };

            foreach (var (keywords, layer) in pairs)
            {
                if (layer == dataLayerName)
                {
                    foreach (string keyword in keywords)
                    {
                        if (displayedName.Contains(keyword))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 将地层名称映射到本地化键
        /// </summary>
        private string GetLayerLocalizationKey(string layerName)
        {
            // 移除可能的额外字符和标准化名称
            string cleanName = layerName.Trim();

            // 根据地层名称返回对应的本地化键
            if (cleanName.Contains("青葉") || cleanName.Contains("青葉山") || cleanName.Contains("Aoba"))
                return "encyclopedia.layer.aoba_mountain";
            else if (cleanName.Contains("大年寺") || cleanName.Contains("Dainenji"))
                return "encyclopedia.layer.dainenji";
            else if (cleanName.Contains("向山") || cleanName.Contains("Mukoyama"))
                return "encyclopedia.layer.mukoyama";
            else if (cleanName.Contains("広瀬川") || cleanName.Contains("Hirose"))
                return "encyclopedia.layer.hirose_river_tuff";
            else if (cleanName.Contains("竜ノ口") || cleanName.Contains("Ryunokuchi"))
                return "encyclopedia.layer.ryunokuchi";
            else if (cleanName.Contains("亀岡") || cleanName.Contains("Kameoka"))
                return "encyclopedia.layer.kameoka";
            else
            {
                // 对于未知地层，返回通用键或原始名称
                Debug.LogWarning($"[EncyclopediaUI] 未知地层名称: {layerName}，使用原始名称");
                return "encyclopedia.layer.unknown";
            }
        }

        /// <summary>
        /// 语言变化时的回调函数
        /// </summary>
        private void OnLanguageChanged()
        {
            // 如果图鉴当前是打开的，刷新条目列表以更新显示的语言
            if (isOpen)
            {
                // 更新筛选器选项
                SetupFilterControls();

                // 更新地层标签显示
                UpdateLayerTabsLanguage();

                // 更新统计信息显示
                UpdateStatistics();

                // 如果详情面板开启，更新当前条目的详情显示
                if (detailPanel != null && detailPanel.activeInHierarchy && selectedEntry != null)
                {
                    ShowEntryDetails(selectedEntry);
                }

                // 刷新条目列表
                RefreshEntryList();
            }
        }

        /// <summary>
        /// 更新地层标签的语言显示
        /// </summary>
        private void UpdateLayerTabsLanguage()
        {
            if (layerTabs == null || layerTabs.Count == 0)
                return;

            // 确保我们有对应的地层名称
            if (EncyclopediaData.Instance?.LayerNames == null ||
                EncyclopediaData.Instance.LayerNames.Count != layerTabs.Count)
                return;

            for (int i = 0; i < layerTabs.Count && i < EncyclopediaData.Instance.LayerNames.Count; i++)
            {
                if (layerTabs[i] != null)
                {
                    var buttonText = layerTabs[i].GetComponentInChildren<Text>();
                    if (buttonText != null)
                    {
                        string originalLayerName = EncyclopediaData.Instance.LayerNames[i];
                        string localizedLayerName = GetLocalizedLayerName(originalLayerName);
                        buttonText.text = localizedLayerName;
                        Debug.Log($"[EncyclopediaUI] 更新地层标签语言: {originalLayerName} -> {localizedLayerName}");
                    }
                }
            }
        }


        /// <summary>
        /// 检查Canvas和Raycaster设置
        /// </summary>
        private void CheckCanvasRaycastSettings()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                var raycaster = canvas.GetComponent<GraphicRaycaster>();
                Debug.Log($"[EncyclopediaUI] Canvas检查: {canvas.name}");
                Debug.Log($"[EncyclopediaUI] - RenderMode: {canvas.renderMode}");
                Debug.Log($"[EncyclopediaUI] - SortingOrder: {canvas.sortingOrder}");
                Debug.Log($"[EncyclopediaUI] - GraphicRaycaster: {(raycaster != null ? "存在" : "缺失")}");
                if (raycaster != null)
                {
                    Debug.Log($"[EncyclopediaUI] - Raycaster enabled: {raycaster.enabled}");
                    Debug.Log($"[EncyclopediaUI] - IgnoreReversedGraphics: {raycaster.ignoreReversedGraphics}");
                    Debug.Log($"[EncyclopediaUI] - BlockingObjects: {raycaster.blockingObjects}");
                }
                else
                {
                    Debug.LogError("[EncyclopediaUI] ❌ GraphicRaycaster缺失! 这会导致UI点击失效!");
                }
            }
            else
            {
                Debug.LogError("[EncyclopediaUI] ❌ 未找到父级Canvas!");
            }

            // 检查EventSystem
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem != null)
            {
                Debug.Log($"[EncyclopediaUI] EventSystem: {eventSystem.name} (enabled: {eventSystem.enabled})");
            }
            else
            {
                Debug.LogError("[EncyclopediaUI] ❌ EventSystem缺失! UI点击事件无法工作!");
            }
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
            CreateTriangleNavigationButton(detailPanel.transform, "PrevButton", true, new Vector2(-120, 120), GoToPreviousEntry);

            // 创建右侧下一页按钮（三角形向右）- 底部中央偏右
            CreateTriangleNavigationButton(detailPanel.transform, "NextButton", false, new Vector2(120, 120), GoToNextEntry);

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
            buttonRect.sizeDelta = new Vector2(100, 100); // 更适中的按钮大小

            // 添加Button组件和半透明背景
            Image buttonBg = buttonGO.AddComponent<Image>();
            buttonBg.color = new Color(0.2f, 0.3f, 0.4f, 0.7f); // 深蓝色半透明背景

            Button button = buttonGO.AddComponent<Button>();
            button.targetGraphic = buttonBg;
            button.onClick.AddListener(() => onClick());

            // 添加悬停效果
            var colors = button.colors;
            colors.highlightedColor = new Color(0.3f, 0.4f, 0.6f, 0.9f);
            colors.pressedColor = new Color(0.1f, 0.2f, 0.3f, 1.0f);
            colors.disabledColor = new Color(0.1f, 0.1f, 0.1f, 0.3f);
            button.colors = colors;

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
            triangleText.fontSize = 48; // 适中的三角形大小
            triangleText.color = Color.white; // 纯白色，更清晰
            triangleText.alignment = TextAnchor.MiddleCenter;
            triangleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            triangleText.fontStyle = FontStyle.Bold; // 加粗让箭头更明显

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
                var prevBg = prevButton.GetComponent<Image>();
                if (prevText != null)
                {
                    prevText.color = prevButton.interactable ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.6f);
                }
                if (prevBg != null)
                {
                    prevBg.color = prevButton.interactable ?
                        new Color(0.2f, 0.3f, 0.4f, 0.7f) :
                        new Color(0.1f, 0.1f, 0.1f, 0.3f);
                }
            }

            if (nextButton != null)
            {
                nextButton.interactable = currentEntryIndex < currentFilteredEntries.Count - 1;
                var nextText = nextButton.GetComponentInChildren<Text>();
                var nextBg = nextButton.GetComponent<Image>();
                if (nextText != null)
                {
                    nextText.color = nextButton.interactable ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.6f);
                }
                if (nextBg != null)
                {
                    nextBg.color = nextButton.interactable ?
                        new Color(0.2f, 0.3f, 0.4f, 0.7f) :
                        new Color(0.1f, 0.1f, 0.1f, 0.3f);
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

        /// <summary>
        /// 延迟强制渲染协程 - 修复首次显示白屏问题
        /// </summary>
        private System.Collections.IEnumerator ForceRenderAfterDelay(Sample3DModelViewer viewer)
        {
            // 等待几帧让3D模型完全加载
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            if (viewer != null)
            {
                // 调用Sample3DModelViewer的强制渲染方法
                viewer.ForceRender();
                Debug.Log("[EncyclopediaUI] 执行延迟强制渲染以修复白屏问题");

                // 再等一帧确保渲染完成
                yield return new WaitForEndOfFrame();

                // 如果还是有问题，再强制切换到RenderTexture显示
                viewer.ForceRenderTextureDisplay();
                Debug.Log("[EncyclopediaUI] 强制切换到RenderTexture显示");
            }
        }

        /// <summary>
        /// 获取岩石类型用于排序（使用日文假名顺序）
        /// </summary>
        private string GetRockTypeForSorting(EncyclopediaEntry entry)
        {
            // 获取本地化的岩石名称
            string rockName = GetLocalizedRockName(entry.rockName);

            // 如果rockName为null或空，提供默认值
            if (string.IsNullOrEmpty(rockName))
            {
                rockName = "未知岩石"; // 提供默认值
            }

            // 岩石类型的假名排序顺序映射
            var rockSortOrder = new Dictionary<string, string>
            {
                // 按假名顺序排列
                {"火山灰", "01かざんばい"},
                {"火山ガラス", "02かざんがらす"},
                {"礫岩", "03れきがん"},
                {"砂岩", "04さがん"},
                {"泥岩", "05でいがん"},
                {"凝灰岩", "06ぎょうかいがん"},
                {"石灰岩", "07せっかいがん"},
                {"花崗岩", "08かこうがん"},
                {"安山岩", "09あんざんがん"},
                {"玄武岩", "10げんぶがん"},
                {"未知岩石", "99みちがんせき"} // 默认值的排序键
            };

            // 如果找到映射，返回排序键，否则返回原始名称
            return rockSortOrder.TryGetValue(rockName, out string sortKey) ? sortKey : ("99" + rockName);
        }

        /// <summary>
        /// 获取矿物名称用于排序（使用日文假名顺序）
        /// </summary>
        private string GetMineralNameForSorting(EncyclopediaEntry entry)
        {
            // 获取本地化的矿物名称
            string mineralName = "";
            var currentLanguage = LocalizationManager.Instance?.CurrentLanguage ?? LanguageSettings.Language.Japanese;

            switch (currentLanguage)
            {
                case LanguageSettings.Language.English:
                    mineralName = !string.IsNullOrEmpty(entry.nameEN) ? entry.nameEN : entry.displayName;
                    break;
                case LanguageSettings.Language.Japanese:
                    mineralName = !string.IsNullOrEmpty(entry.nameJA) ? entry.nameJA : entry.displayName;
                    break;
                case LanguageSettings.Language.ChineseSimplified:
                default:
                    mineralName = !string.IsNullOrEmpty(entry.nameCN) ? entry.nameCN : entry.displayName;
                    break;
            }

            // 如果mineralName为null或空，提供默认值
            if (string.IsNullOrEmpty(mineralName))
            {
                mineralName = "未知矿物"; // 提供默认值
            }

            // 矿物名称的假名排序顺序映射
            var mineralSortOrder = new Dictionary<string, string>
            {
                // 按假名顺序排列（假名读音）
                {"石英", "01せきえい"},
                {"斜長石", "02しゃちょうせき"},
                {"角閃石", "03かくせんせき"},
                {"輝石", "04きせき"},
                {"橄榄石", "05かんらんせき"},
                {"磁鉄鉱", "06じてっこう"},
                {"黄鉄鉱", "07おうてっこう"},
                {"方解石", "08ほうかいせき"},
                {"長石", "09ちょうせき"},
                {"雲母", "10うんも"},
                {"ザクロ石", "11ざくろいし"},
                {"紫蘇輝石", "12しそきせき"},
                {"火山ガラス", "13かざんがらす"},
                {"重鉱物", "14じゅうこうぶつ"},
                {"炭質物", "15たんしつぶつ"},
                {"未知矿物", "99みちこうぶつ"} // 默认值的排序键
            };

            // 如果找到映射，返回排序键，否则返回原始名称
            return mineralSortOrder.TryGetValue(mineralName, out string sortKey) ? sortKey : ("99" + mineralName);
        }

        private void OnDestroy()
        {
            // 取消事件订阅
            CollectionManager.OnStatsUpdated -= OnStatsUpdated;

            // 取消语言变化事件订阅
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
            }
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
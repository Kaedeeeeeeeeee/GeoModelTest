using UnityEngine;
using UnityEngine.UI;

namespace Encyclopedia
{
    /// <summary>
    /// 图鉴UI设置助手
    /// 用于在编辑器中快速创建图鉴UI结构
    /// </summary>
    public class EncyclopediaUISetup : MonoBehaviour
    {
        [Header("创建设置")]
        [SerializeField] private bool createOnStart = false;
        
        [ContextMenu("创建图鉴UI")]
        public void CreateEncyclopediaUI()
        {
            Debug.Log("开始创建图鉴UI结构...");
            
            // 创建主Canvas
            GameObject canvas = CreateMainCanvas();
            
            // 创建图鉴面板
            GameObject encyclopediaPanel = CreateEncyclopediaPanel(canvas);
            
            // 创建左侧面板
            GameObject leftPanel = CreateLeftPanel(encyclopediaPanel);
            
            // 创建右侧面板
            GameObject rightPanel = CreateRightPanel(encyclopediaPanel);
            
            // 创建详细信息面板
            GameObject detailPanel = CreateDetailPanel(encyclopediaPanel);
            
            // 设置EncyclopediaUI组件
            SetupEncyclopediaUIComponent(encyclopediaPanel, leftPanel, rightPanel, detailPanel);
            
            Debug.Log("图鉴UI结构创建完成！");
        }

        private GameObject CreateMainCanvas()
        {
            // 查找现有Canvas或创建新的
            Canvas existingCanvas = FindObjectOfType<Canvas>();
            if (existingCanvas != null)
            {
                Debug.Log("使用现有Canvas");
                return existingCanvas.gameObject;
            }

            GameObject canvasGO = new GameObject("EncyclopediaCanvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // 确保在其他UI之上
            
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            
            canvasGO.AddComponent<GraphicRaycaster>();
            
            Debug.Log("创建新Canvas");
            return canvasGO;
        }

        private GameObject CreateEncyclopediaPanel(GameObject parent)
        {
            GameObject panel = new GameObject("EncyclopediaPanel");
            panel.transform.SetParent(parent.transform, false);
            
            // RectTransform设置为全屏
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            // 背景图片
            Image background = panel.AddComponent<Image>();
            background.color = new Color(0.1f, 0.1f, 0.15f, 0.95f); // 深蓝色半透明背景
            
            // 确保面板默认是隐藏的
            panel.SetActive(false);
            
            // 创建关闭按钮
            CreateCloseButton(panel);
            
            return panel;
        }

        private void CreateCloseButton(GameObject parent)
        {
            GameObject buttonGO = new GameObject("CloseButton");
            buttonGO.transform.SetParent(parent.transform, false);
            
            RectTransform rect = buttonGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-20, -20);
            rect.sizeDelta = new Vector2(60, 60);
            
            Image image = buttonGO.AddComponent<Image>();
            image.color = new Color(0.8f, 0.2f, 0.2f, 0.8f);
            
            Button button = buttonGO.AddComponent<Button>();
            
            // 添加X文字
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);
            
            RectTransform textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            Text text = textGO.AddComponent<Text>();
            text.text = "×";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 36;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
        }

        private GameObject CreateLeftPanel(GameObject parent)
        {
            GameObject leftPanel = new GameObject("LeftPanel");
            leftPanel.transform.SetParent(parent.transform, false);
            
            RectTransform rect = leftPanel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(0.25f, 1);
            rect.offsetMin = new Vector2(20, 20);
            rect.offsetMax = new Vector2(-10, -80);
            
            // 背景
            Image background = leftPanel.AddComponent<Image>();
            background.color = new Color(0.05f, 0.05f, 0.1f, 0.9f);
            
            // 创建地层标签容器
            CreateLayerTabContainer(leftPanel);
            
            // 创建统计信息面板
            CreateStatisticsPanel(leftPanel);
            
            return leftPanel;
        }

        private void CreateLayerTabContainer(GameObject parent)
        {
            GameObject container = new GameObject("LayerTabContainer");
            container.transform.SetParent(parent.transform, false);
            
            RectTransform rect = container.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0.3f);
            rect.anchorMax = new Vector2(1, 1);
            rect.offsetMin = new Vector2(10, 10);
            rect.offsetMax = new Vector2(-10, -10);
            
            // 添加垂直布局组
            VerticalLayoutGroup layout = container.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 5;
            layout.padding = new RectOffset(5, 5, 5, 5);
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            
            ContentSizeFitter fitter = container.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            // 创建标签页预制体
            CreateLayerTabPrefab(container);
        }

        private void CreateLayerTabPrefab(GameObject parent)
        {
            GameObject tabPrefab = new GameObject("LayerTabPrefab");
            tabPrefab.transform.SetParent(parent.transform, false);
            
            RectTransform rect = tabPrefab.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 50);
            
            // 按钮背景
            Image background = tabPrefab.AddComponent<Image>();
            background.color = new Color(0.2f, 0.3f, 0.5f, 0.8f);
            
            Button button = tabPrefab.AddComponent<Button>();
            
            // 文字
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(tabPrefab.transform, false);
            
            RectTransform textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 0);
            textRect.offsetMax = new Vector2(-10, 0);
            
            Text text = textGO.AddComponent<Text>();
            text.text = "地层名称";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            
            // 禁用预制体（这只是模板）
            tabPrefab.SetActive(false);
        }

        private void CreateStatisticsPanel(GameObject parent)
        {
            GameObject statsPanel = new GameObject("StatisticsPanel");
            statsPanel.transform.SetParent(parent.transform, false);
            
            RectTransform rect = statsPanel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 0.25f);
            rect.offsetMin = new Vector2(10, 10);
            rect.offsetMax = new Vector2(-10, -10);
            
            // 背景
            Image background = statsPanel.AddComponent<Image>();
            background.color = new Color(0.1f, 0.15f, 0.2f, 0.9f);
            
            // 统计文字
            GameObject textGO = new GameObject("StatisticsText");
            textGO.transform.SetParent(statsPanel.transform, false);
            
            RectTransform textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 10);
            textRect.offsetMax = new Vector2(-10, -10);
            
            Text text = textGO.AddComponent<Text>();
            text.text = "收集统计:\n矿物: 0/0\n化石: 0/0";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 14;
            text.color = new Color(0.7f, 0.9f, 1f);
            text.alignment = TextAnchor.UpperLeft;
        }

        private GameObject CreateRightPanel(GameObject parent)
        {
            GameObject rightPanel = new GameObject("RightPanel");
            rightPanel.transform.SetParent(parent.transform, false);
            
            RectTransform rect = rightPanel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.25f, 0);
            rect.anchorMax = new Vector2(1, 1);
            rect.offsetMin = new Vector2(10, 20);
            rect.offsetMax = new Vector2(-20, -80);
            
            // 背景
            Image background = rightPanel.AddComponent<Image>();
            background.color = new Color(0.05f, 0.05f, 0.1f, 0.9f);
            
            // 创建筛选面板
            CreateFilterPanel(rightPanel);
            
            // 创建条目列表
            CreateEntryList(rightPanel);
            
            return rightPanel;
        }

        private void CreateFilterPanel(GameObject parent)
        {
            GameObject filterPanel = new GameObject("FilterPanel");
            filterPanel.transform.SetParent(parent.transform, false);
            
            RectTransform rect = filterPanel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0.9f);
            rect.anchorMax = new Vector2(1, 1);
            rect.offsetMin = new Vector2(10, 0);
            rect.offsetMax = new Vector2(-10, -10);
            
            // 水平布局
            HorizontalLayoutGroup layout = filterPanel.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.padding = new RectOffset(10, 10, 5, 5);
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = true;
            
            // 创建筛选控件
            CreateDropdown(filterPanel, "EntryTypeFilter", "类型筛选");
            CreateDropdown(filterPanel, "RarityFilter", "稀有度筛选");
            CreateSearchInput(filterPanel);
            CreateClearButton(filterPanel);
        }

        private void CreateDropdown(GameObject parent, string name, string label)
        {
            GameObject dropdownGO = new GameObject(name);
            dropdownGO.transform.SetParent(parent.transform, false);
            
            RectTransform rect = dropdownGO.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(150, 30);
            
            Image background = dropdownGO.AddComponent<Image>();
            background.color = new Color(0.2f, 0.2f, 0.3f, 0.9f);
            
            Dropdown dropdown = dropdownGO.AddComponent<Dropdown>();
            dropdown.options.Add(new Dropdown.OptionData("全部"));
            
            // Label (Caption Text)
            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(dropdownGO.transform, false);
            
            RectTransform labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10, 2);
            labelRect.offsetMax = new Vector2(-25, -2);
            
            Text labelText = labelGO.AddComponent<Text>();
            labelText.text = "全部";
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 12;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleLeft;
            
            dropdown.captionText = labelText;
            
            // Arrow
            GameObject arrowGO = new GameObject("Arrow");
            arrowGO.transform.SetParent(dropdownGO.transform, false);
            
            RectTransform arrowRect = arrowGO.AddComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1, 0.5f);
            arrowRect.anchorMax = new Vector2(1, 0.5f);
            arrowRect.pivot = new Vector2(1, 0.5f);
            arrowRect.anchoredPosition = new Vector2(-10, 0);
            arrowRect.sizeDelta = new Vector2(10, 10);
            
            Image arrowImage = arrowGO.AddComponent<Image>();
            arrowImage.color = Color.white;
            
            // 创建简单的Template（这里简化处理）
            GameObject templateGO = new GameObject("Template");
            templateGO.transform.SetParent(dropdownGO.transform, false);
            templateGO.SetActive(false);
            
            RectTransform templateRect = templateGO.AddComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0, 0);
            templateRect.anchorMax = new Vector2(1, 0);
            templateRect.pivot = new Vector2(0.5f, 1);
            templateRect.anchoredPosition = new Vector2(0, 0);
            templateRect.sizeDelta = new Vector2(0, 100);
            
            Image templateBg = templateGO.AddComponent<Image>();
            templateBg.color = new Color(0.2f, 0.2f, 0.3f, 0.95f);
            
            dropdown.template = templateRect;
        }

        private void CreateSearchInput(GameObject parent)
        {
            GameObject inputGO = new GameObject("SearchInput");
            inputGO.transform.SetParent(parent.transform, false);
            
            RectTransform rect = inputGO.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 30);
            
            Image background = inputGO.AddComponent<Image>();
            background.color = new Color(0.2f, 0.2f, 0.3f, 0.9f);
            
            InputField inputField = inputGO.AddComponent<InputField>();
            
            // Placeholder
            GameObject placeholderGO = new GameObject("Placeholder");
            placeholderGO.transform.SetParent(inputGO.transform, false);
            
            RectTransform placeholderRect = placeholderGO.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(10, 0);
            placeholderRect.offsetMax = new Vector2(-10, 0);
            
            Text placeholder = placeholderGO.AddComponent<Text>();
            placeholder.text = "搜索...";
            placeholder.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            placeholder.fontSize = 12;
            placeholder.color = new Color(0.7f, 0.7f, 0.7f, 0.5f);
            placeholder.alignment = TextAnchor.MiddleLeft;
            
            // Text
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(inputGO.transform, false);
            
            RectTransform textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 0);
            textRect.offsetMax = new Vector2(-10, 0);
            
            Text text = textGO.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 12;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            
            inputField.placeholder = placeholder;
            inputField.textComponent = text;
        }

        private void CreateClearButton(GameObject parent)
        {
            GameObject buttonGO = new GameObject("ClearFiltersButton");
            buttonGO.transform.SetParent(parent.transform, false);
            
            RectTransform rect = buttonGO.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(80, 30);
            
            Image background = buttonGO.AddComponent<Image>();
            background.color = new Color(0.5f, 0.3f, 0.2f, 0.9f);
            
            Button button = buttonGO.AddComponent<Button>();
            
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);
            
            RectTransform textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            Text text = textGO.AddComponent<Text>();
            text.text = "清除";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 12;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
        }

        private void CreateEntryList(GameObject parent)
        {
            GameObject listContainer = new GameObject("EntryListContainer");
            listContainer.transform.SetParent(parent.transform, false);
            
            RectTransform rect = listContainer.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 0.85f);
            rect.offsetMin = new Vector2(10, 10);
            rect.offsetMax = new Vector2(-10, 0);
            
            // 滚动视图
            ScrollRect scrollRect = listContainer.AddComponent<ScrollRect>();
            
            // Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(listContainer.transform, false);
            
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            
            Image viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = new Color(0.1f, 0.1f, 0.15f, 0.5f);
            
            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            
            // Content
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            
            VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 2;
            contentLayout.padding = new RectOffset(5, 5, 5, 5);
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            
            ContentSizeFitter contentFitter = content.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            scrollRect.vertical = true;
            scrollRect.horizontal = false;
            
            // 创建条目预制体
            CreateEntryItemPrefab(content);
        }

        private void CreateEntryItemPrefab(GameObject parent)
        {
            GameObject itemPrefab = new GameObject("EntryItemPrefab");
            itemPrefab.transform.SetParent(parent.transform, false);
            
            RectTransform rect = itemPrefab.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 60);
            
            Image background = itemPrefab.AddComponent<Image>();
            background.color = new Color(0.15f, 0.2f, 0.3f, 0.8f);
            
            Button button = itemPrefab.AddComponent<Button>();
            
            // 图标
            GameObject iconGO = new GameObject("IconImage");
            iconGO.transform.SetParent(itemPrefab.transform, false);
            
            RectTransform iconRect = iconGO.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0, 0.5f);
            iconRect.anchorMax = new Vector2(0, 0.5f);
            iconRect.pivot = new Vector2(0, 0.5f);
            iconRect.anchoredPosition = new Vector2(10, 0);
            iconRect.sizeDelta = new Vector2(50, 50);
            
            Image iconImage = iconGO.AddComponent<Image>();
            iconImage.color = Color.gray;
            
            // 名称文字
            GameObject nameGO = new GameObject("NameText");
            nameGO.transform.SetParent(itemPrefab.transform, false);
            
            RectTransform nameRect = nameGO.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0.5f);
            nameRect.anchorMax = new Vector2(1, 1);
            nameRect.offsetMin = new Vector2(70, 0);
            nameRect.offsetMax = new Vector2(-100, -5);
            
            Text nameText = nameGO.AddComponent<Text>();
            nameText.text = "条目名称";
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize = 14;
            nameText.color = Color.white;
            nameText.alignment = TextAnchor.MiddleLeft;
            
            // 稀有度文字
            GameObject rarityGO = new GameObject("RarityText");
            rarityGO.transform.SetParent(itemPrefab.transform, false);
            
            RectTransform rarityRect = rarityGO.AddComponent<RectTransform>();
            rarityRect.anchorMin = new Vector2(0, 0);
            rarityRect.anchorMax = new Vector2(1, 0.5f);
            rarityRect.offsetMin = new Vector2(70, 5);
            rarityRect.offsetMax = new Vector2(-100, 0);
            
            Text rarityText = rarityGO.AddComponent<Text>();
            rarityText.text = "稀有度";
            rarityText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            rarityText.fontSize = 12;
            rarityText.color = Color.yellow;
            rarityText.alignment = TextAnchor.MiddleLeft;
            
            // 状态指示器
            GameObject statusGO = new GameObject("StatusImage");
            statusGO.transform.SetParent(itemPrefab.transform, false);
            
            RectTransform statusRect = statusGO.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(1, 0.5f);
            statusRect.anchorMax = new Vector2(1, 0.5f);
            statusRect.pivot = new Vector2(1, 0.5f);
            statusRect.anchoredPosition = new Vector2(-20, 0);
            statusRect.sizeDelta = new Vector2(20, 20);
            
            Image statusImage = statusGO.AddComponent<Image>();
            statusImage.color = Color.red;
            
            // 禁用预制体
            itemPrefab.SetActive(false);
        }

        private GameObject CreateDetailPanel(GameObject parent)
        {
            GameObject detailPanel = new GameObject("DetailPanel");
            detailPanel.transform.SetParent(parent.transform, false);
            
            RectTransform rect = detailPanel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.3f, 0.1f);
            rect.anchorMax = new Vector2(0.9f, 0.9f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            Image background = detailPanel.AddComponent<Image>();
            background.color = new Color(0.05f, 0.1f, 0.15f, 0.95f);
            
            // 创建详细信息内容
            CreateDetailContent(detailPanel);
            
            // 创建3D查看器
            Create3DViewer(detailPanel);
            
            // 默认隐藏
            detailPanel.SetActive(false);
            
            return detailPanel;
        }

        private void CreateDetailContent(GameObject parent)
        {
            // 标题
            GameObject titleGO = new GameObject("DetailTitle");
            titleGO.transform.SetParent(parent.transform, false);
            
            RectTransform titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.9f);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.offsetMin = new Vector2(20, 0);
            titleRect.offsetMax = new Vector2(-20, -10);
            
            Text titleText = titleGO.AddComponent<Text>();
            titleText.text = "详细信息";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 18;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.fontStyle = FontStyle.Bold;
            
            // 图标
            GameObject iconGO = new GameObject("DetailIcon");
            iconGO.transform.SetParent(parent.transform, false);
            
            RectTransform iconRect = iconGO.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0, 0.7f);
            iconRect.anchorMax = new Vector2(0, 0.9f);
            iconRect.offsetMin = new Vector2(20, 0);
            iconRect.offsetMax = new Vector2(120, 0);
            
            Image iconImage = iconGO.AddComponent<Image>();
            iconImage.color = Color.gray;
            
            // 描述
            GameObject descGO = new GameObject("DetailDescription");
            descGO.transform.SetParent(parent.transform, false);
            
            RectTransform descRect = descGO.AddComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0, 0.4f);
            descRect.anchorMax = new Vector2(0.5f, 0.7f);
            descRect.offsetMin = new Vector2(20, 0);
            descRect.offsetMax = new Vector2(-10, 0);
            
            Text descText = descGO.AddComponent<Text>();
            descText.text = "描述信息";
            descText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            descText.fontSize = 12;
            descText.color = Color.white;
            descText.alignment = TextAnchor.UpperLeft;
            
            // 属性
            GameObject propsGO = new GameObject("DetailProperties");
            propsGO.transform.SetParent(parent.transform, false);
            
            RectTransform propsRect = propsGO.AddComponent<RectTransform>();
            propsRect.anchorMin = new Vector2(0, 0);
            propsRect.anchorMax = new Vector2(0.5f, 0.4f);
            propsRect.offsetMin = new Vector2(20, 20);
            propsRect.offsetMax = new Vector2(-10, 0);
            
            Text propsText = propsGO.AddComponent<Text>();
            propsText.text = "属性信息";
            propsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            propsText.fontSize = 12;
            propsText.color = new Color(0.8f, 0.9f, 1f);
            propsText.alignment = TextAnchor.UpperLeft;
        }

        private void Create3DViewer(GameObject parent)
        {
            GameObject viewerGO = new GameObject("Model3DViewer");
            viewerGO.transform.SetParent(parent.transform, false);
            
            RectTransform rect = viewerGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0);
            rect.anchorMax = new Vector2(1, 1);
            rect.offsetMin = new Vector2(10, 20);
            rect.offsetMax = new Vector2(-20, -100);
            
            Image background = viewerGO.AddComponent<Image>();
            background.color = new Color(0.02f, 0.05f, 0.08f, 0.9f);
            
            // 添加Model3DViewer脚本
            viewerGO.AddComponent<Model3DViewer>();
            
            // 创建相机
            GameObject cameraGO = new GameObject("ViewerCamera");
            cameraGO.transform.SetParent(viewerGO.transform, false);
            cameraGO.transform.localPosition = new Vector3(0, 0, -2);
            
            Camera camera = cameraGO.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.02f, 0.05f, 0.08f, 1f);
            camera.cullingMask = LayerMask.GetMask("UI"); // 只渲染UI层
            camera.orthographic = false;
            camera.fieldOfView = 60f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 10f;
            
            // 创建控制按钮
            CreateViewerControls(viewerGO);
        }

        private void CreateViewerControls(GameObject parent)
        {
            GameObject controlsGO = new GameObject("ViewerControls");
            controlsGO.transform.SetParent(parent.transform, false);
            
            RectTransform rect = controlsGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0, 40);
            
            HorizontalLayoutGroup layout = controlsGO.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.padding = new RectOffset(10, 10, 5, 5);
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = true;
            
            // 重置按钮
            GameObject resetButton = new GameObject("ResetButton");
            resetButton.transform.SetParent(controlsGO.transform, false);
            
            RectTransform resetRect = resetButton.AddComponent<RectTransform>();
            resetRect.sizeDelta = new Vector2(80, 30);
            
            Image resetImage = resetButton.AddComponent<Image>();
            resetImage.color = new Color(0.3f, 0.5f, 0.7f, 0.8f);
            
            Button resetBtn = resetButton.AddComponent<Button>();
            
            GameObject resetTextGO = new GameObject("Text");
            resetTextGO.transform.SetParent(resetButton.transform, false);
            
            RectTransform resetTextRect = resetTextGO.AddComponent<RectTransform>();
            resetTextRect.anchorMin = Vector2.zero;
            resetTextRect.anchorMax = Vector2.one;
            resetTextRect.offsetMin = Vector2.zero;
            resetTextRect.offsetMax = Vector2.zero;
            
            Text resetText = resetTextGO.AddComponent<Text>();
            resetText.text = "重置";
            resetText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            resetText.fontSize = 12;
            resetText.color = Color.white;
            resetText.alignment = TextAnchor.MiddleCenter;
        }

        private void SetupEncyclopediaUIComponent(GameObject encyclopediaPanel, GameObject leftPanel, GameObject rightPanel, GameObject detailPanel)
        {
            EncyclopediaUI uiController = encyclopediaPanel.GetComponent<EncyclopediaUI>();
            if (uiController == null)
            {
                uiController = encyclopediaPanel.AddComponent<EncyclopediaUI>();
            }
            
            // 使用反射来设置SerializeField字段
            SetupUIReferences(uiController, encyclopediaPanel, leftPanel, rightPanel, detailPanel);
            
            Debug.Log("EncyclopediaUI组件引用设置完成！");
        }
        
        private void SetupUIReferences(EncyclopediaUI uiController, GameObject encyclopediaPanel, GameObject leftPanel, GameObject rightPanel, GameObject detailPanel)
        {
            var uiType = typeof(EncyclopediaUI);
            
            try
            {
                // 主要面板
                SetFieldValue(uiController, "encyclopediaPanel", encyclopediaPanel);
                SetFieldValue(uiController, "closeButton", FindDeepChild(encyclopediaPanel.transform, "CloseButton")?.GetComponent<Button>());
                
                // 左侧导航
                SetFieldValue(uiController, "layerTabContainer", FindDeepChild(leftPanel.transform, "LayerTabContainer"));
                SetFieldValue(uiController, "layerTabPrefab", FindDeepChild(leftPanel.transform, "LayerTabPrefab")?.GetComponent<Button>());
                SetFieldValue(uiController, "statisticsText", FindDeepChild(leftPanel.transform, "StatisticsText")?.GetComponent<Text>());
                
                // 右侧内容区
                SetFieldValue(uiController, "entryListContainer", FindDeepChild(rightPanel.transform, "Content"));
                SetFieldValue(uiController, "entryItemPrefab", FindDeepChild(rightPanel.transform, "EntryItemPrefab"));
                SetFieldValue(uiController, "entryScrollRect", FindDeepChild(rightPanel.transform, "EntryListContainer")?.GetComponent<ScrollRect>());
                
                // 筛选控件
                SetFieldValue(uiController, "entryTypeFilter", FindDeepChild(rightPanel.transform, "EntryTypeFilter")?.GetComponent<Dropdown>());
                SetFieldValue(uiController, "rarityFilter", FindDeepChild(rightPanel.transform, "RarityFilter")?.GetComponent<Dropdown>());
                SetFieldValue(uiController, "searchInput", FindDeepChild(rightPanel.transform, "SearchInput")?.GetComponent<InputField>());
                SetFieldValue(uiController, "clearFiltersButton", FindDeepChild(rightPanel.transform, "ClearFiltersButton")?.GetComponent<Button>());
                
                // 详细信息面板
                SetFieldValue(uiController, "detailPanel", detailPanel);
                SetFieldValue(uiController, "detailTitle", FindDeepChild(detailPanel.transform, "DetailTitle")?.GetComponent<Text>());
                SetFieldValue(uiController, "detailIcon", FindDeepChild(detailPanel.transform, "DetailIcon")?.GetComponent<Image>());
                SetFieldValue(uiController, "detailDescription", FindDeepChild(detailPanel.transform, "DetailDescription")?.GetComponent<Text>());
                SetFieldValue(uiController, "detailProperties", FindDeepChild(detailPanel.transform, "DetailProperties")?.GetComponent<Text>());
                SetFieldValue(uiController, "model3DViewer", FindDeepChild(detailPanel.transform, "Model3DViewer")?.GetComponent<Model3DViewer>());
                
                Debug.Log("UI引用自动连接完成");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"设置UI引用时出错: {e.Message}");
            }
        }
        
        private void SetFieldValue(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null && value != null)
            {
                field.SetValue(target, value);
                Debug.Log($"设置 {fieldName}: {value}");
            }
            else if (field == null)
            {
                Debug.LogWarning($"未找到字段: {fieldName}");
            }
            else
            {
                Debug.LogWarning($"未找到组件: {fieldName}");
            }
        }
        
        private Transform FindDeepChild(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == name)
                    return child;
                
                Transform result = FindDeepChild(child, name);
                if (result != null)
                    return result;
            }
            return null;
        }

        private void Start()
        {
            if (createOnStart)
            {
                CreateEncyclopediaUI();
            }
        }
    }
}
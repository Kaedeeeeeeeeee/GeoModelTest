using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Encyclopedia
{
    /// <summary>
    /// 简化的图鉴管理器
    /// 用于快速测试和调试核心功能
    /// </summary>
    public class SimpleEncyclopediaManager : MonoBehaviour
    {
        [Header("UI组件")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private Text headerText;
        [SerializeField] private Button closeButton;
        
        [Header("左侧导航")]
        [SerializeField] private GameObject leftPanel;
        [SerializeField] private Transform layerButtonContainer;
        [SerializeField] private Button layerButtonPrefab;
        
        [Header("右侧内容")]
        [SerializeField] private GameObject rightPanel;
        [SerializeField] private Text systemStatusText;
        [SerializeField] private Transform entryListContainer;
        [SerializeField] private GameObject entryItemPrefab;
        [SerializeField] private ScrollRect entryScrollRect;
        
        [Header("详情面板")]
        [SerializeField] private GameObject detailPanel;
        [SerializeField] private Text detailTitleText;
        [SerializeField] private Text detailDescriptionText;
        [SerializeField] private RawImage detailImage;
        [SerializeField] private Button detailCloseButton;
        [SerializeField] private Model3DViewer model3DViewer;
        
        [Header("设置")]
        [SerializeField] private Key toggleKey = Key.O;
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool autoCreateDataSystems = true;
        
        private bool isOpen = false;
        private Canvas canvas;
        private string currentLayerName = "";
        private List<Button> layerButtons = new List<Button>();
        private List<GameObject> entryItems = new List<GameObject>();
        private EncyclopediaEntry currentDetailEntry;
        
        // 鼠标和摄像机控制
        private CursorLockMode originalCursorLockMode;
        private bool originalCursorVisible;
        private FirstPersonController firstPersonController;
        
        // 地层名称键（用于本地化）
        private readonly string[] layerNameKeys = new string[]
        {
            "encyclopedia.layer.aoba_mountain", "encyclopedia.layer.dainenji", "encyclopedia.layer.mukoyama", 
            "encyclopedia.layer.hirose_river_tuff", "encyclopedia.layer.ryunokuchi", "encyclopedia.layer.kameoka"
        };
        
        // 地层名称（用于数据查询）
        private readonly string[] layerNames = new string[]
        {
            "青葉山層", "大年寺層", "向山層", 
            "広瀬川凝灰岩部層", "竜ノ口層", "亀岡層"
        };
        
        private void Start()
        {
            CreateSimpleUI();
            
            // 查找第一人称控制器
            firstPersonController = FindObjectOfType<FirstPersonController>();
            
            // 保存原始鼠标状态
            originalCursorLockMode = Cursor.lockState;
            originalCursorVisible = Cursor.visible;
            
            // 确保图鉴面板是关闭的
            if (mainPanel != null)
            {
                mainPanel.SetActive(false);
                isOpen = false;
            }
            
            // 自动创建数据系统（如果需要）
            if (autoCreateDataSystems)
            {
                StartCoroutine(AutoInitializeDataSystems());
            }
            
            // 订阅语言切换事件
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged += RefreshLocalization;
            }
            
            if (showDebugInfo)
            {
                Debug.Log("图鉴管理器已启动，按O键开关");
                if (firstPersonController != null)
                    Debug.Log("找到FirstPersonController，图鉴打开时将禁用鼠标控制");
                else
                    Debug.LogWarning("未找到FirstPersonController，无法禁用鼠标控制");
            }
        }
        
        private void OnDestroy()
        {
            // 取消订阅
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= RefreshLocalization;
            }
        }
        
        /// <summary>
        /// 刷新所有本地化文本
        /// </summary>
        private void RefreshLocalization()
        {
            // 强制等待LocalizationManager初始化
            if (LocalizationManager.Instance != null && !LocalizationManager.Instance.IsInitialized)
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning("LocalizationManager未初始化，尝试延迟刷新");
                }
                StartCoroutine(DelayedRefreshLocalization());
                return;
            }
            
            // 刷新主标题
            if (headerText != null)
            {
                string systemTitle = GetLocalizedText("encyclopedia.system.title");
                headerText.text = systemTitle;
                if (showDebugInfo)
                {
                    Debug.Log($"主标题本地化: 'encyclopedia.system.title' -> '{systemTitle}'");
                }
            }
            
            // 刷新地层按钮
            for (int i = 0; i < layerButtons.Count && i < layerNameKeys.Length; i++)
            {
                var text = layerButtons[i].GetComponentInChildren<Text>();
                if (text != null)
                {
                    string layerText = GetLocalizedText(layerNameKeys[i]);
                    text.text = layerText;
                    if (showDebugInfo && i < 3)
                    {
                        Debug.Log($"地层按钮 {i} 本地化: '{layerNameKeys[i]}' -> '{layerText}'");
                    }
                }
            }
            
            // 刷新详细面板（如果有打开的条目）
            if (currentDetailEntry != null)
            {
                ShowEntryDetail(currentDetailEntry);
            }
            
            // 刷新其他UI
            RefreshInfo();
        }
        
        /// <summary>
        /// 延迟刷新本地化文本
        /// </summary>
        private IEnumerator DelayedRefreshLocalization()
        {
            // 等待最多5秒直到LocalizationManager初始化
            float timeout = 5f;
            while (timeout > 0f && (LocalizationManager.Instance == null || !LocalizationManager.Instance.IsInitialized))
            {
                yield return new WaitForSeconds(0.1f);
                timeout -= 0.1f;
            }
            
            if (LocalizationManager.Instance != null && LocalizationManager.Instance.IsInitialized)
            {
                RefreshLocalization();
            }
            else
            {
                Debug.LogError("LocalizationManager初始化超时，本地化可能无法正常工作");
            }
        }
        
        private void Update()
        {
            // 处理按键输入
            if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
            {
                ToggleEncyclopedia();
            }
        }
        
        /// <summary>
        /// 创建完整的图鉴UI
        /// </summary>
        private void CreateSimpleUI()
        {
            CreateCanvas();
            CreateMainPanel();
            CreateHeaderAndCloseButton();
            CreateLeftPanel();
            CreateRightPanel();
            CreateDetailPanel();
            CreateLayerButtons();
        }
        
        private void CreateCanvas()
        {
            canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                var canvasGO = new GameObject("EncyclopediaCanvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                
                var scaler = canvasGO.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
                
                canvasGO.AddComponent<GraphicRaycaster>();
                
                if (showDebugInfo)
                    Debug.Log("创建了图鉴Canvas");
            }
        }
        
        private void CreateMainPanel()
        {
            mainPanel = new GameObject("EncyclopediaMainPanel");
            mainPanel.transform.SetParent(canvas.transform, false);
            
            var rectTransform = mainPanel.AddComponent<RectTransform>();
            // 全屏显示
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            
            var background = mainPanel.AddComponent<Image>();
            background.color = new Color(0.08f, 0.12f, 0.18f, 0.95f); // 深蓝科技感背景
            
            // 立即设置为隐藏状态
            mainPanel.SetActive(false);
        }
        
        private void CreateHeaderAndCloseButton()
        {
            // 创建标题
            var headerGO = new GameObject("Header");
            headerGO.transform.SetParent(mainPanel.transform, false);
            
            var headerRect = headerGO.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 0.9f);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.offsetMin = new Vector2(20, 0);
            headerRect.offsetMax = new Vector2(-80, -10);
            
            headerText = headerGO.AddComponent<Text>();
            headerText.text = GetLocalizedText("encyclopedia.system.title");
            headerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            headerText.fontSize = 24;
            headerText.color = new Color(0.8f, 0.9f, 1f);
            headerText.alignment = TextAnchor.MiddleLeft;
            headerText.fontStyle = FontStyle.Bold;
            
            // 创建关闭按钮
            var buttonGO = new GameObject("CloseButton");
            buttonGO.transform.SetParent(mainPanel.transform, false);
            
            var buttonRect = buttonGO.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(1, 1);
            buttonRect.anchorMax = new Vector2(1, 1);
            buttonRect.pivot = new Vector2(1, 1);
            buttonRect.anchoredPosition = new Vector2(-15, -15);
            buttonRect.sizeDelta = new Vector2(50, 40);
            
            var buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = new Color(0.8f, 0.3f, 0.3f, 0.8f);
            
            closeButton = buttonGO.AddComponent<Button>();
            closeButton.onClick.AddListener(CloseEncyclopedia);
            
            // 按钮文字
            var buttonTextGO = new GameObject("Text");
            buttonTextGO.transform.SetParent(buttonGO.transform, false);
            
            var buttonTextRect = buttonTextGO.AddComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.offsetMin = Vector2.zero;
            buttonTextRect.offsetMax = Vector2.zero;
            
            var buttonText = buttonTextGO.AddComponent<Text>();
            buttonText.text = "×";
            buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            buttonText.fontSize = 24; // 从20增加到24，主关闭按钮更明显
            buttonText.color = Color.white;
            buttonText.alignment = TextAnchor.MiddleCenter;
        }
        
        private void CreateLeftPanel()
        {
            leftPanel = new GameObject("LeftPanel");
            leftPanel.transform.SetParent(mainPanel.transform, false);
            
            var leftRect = leftPanel.AddComponent<RectTransform>();
            leftRect.anchorMin = new Vector2(0, 0);
            leftRect.anchorMax = new Vector2(0.25f, 0.9f);
            leftRect.offsetMin = new Vector2(20, 20);
            leftRect.offsetMax = new Vector2(-10, 0);
            
            var leftBg = leftPanel.AddComponent<Image>();
            leftBg.color = new Color(0.05f, 0.08f, 0.12f, 0.9f);
            
            // 创建地层按钮区域（上半部分）
            CreateLayerButtonArea();
            
            // 创建系统状态区域（下半部分）
            CreateSystemStatusInLeftPanel();
        }
        
        private void CreateLayerButtonArea()
        {
            // 地层按钮区域 - 占左侧面板上部分（压缩到35%）
            var buttonAreaGO = new GameObject("LayerButtonArea");
            buttonAreaGO.transform.SetParent(leftPanel.transform, false);
            
            var buttonAreaRect = buttonAreaGO.AddComponent<RectTransform>();
            buttonAreaRect.anchorMin = new Vector2(0, 0.65f); // 从0.5f改为0.65f，压缩按钮区域
            buttonAreaRect.anchorMax = new Vector2(1, 1);
            buttonAreaRect.offsetMin = new Vector2(10, 5); // 减少内边距
            buttonAreaRect.offsetMax = new Vector2(-10, -5);
            
            var buttonAreaBg = buttonAreaGO.AddComponent<Image>();
            buttonAreaBg.color = new Color(0.03f, 0.05f, 0.08f, 0.8f);
            
            // 地层按钮容器
            var buttonContainerGO = new GameObject("LayerButtonContainer");
            buttonContainerGO.transform.SetParent(buttonAreaGO.transform, false);
            
            var containerRect = buttonContainerGO.AddComponent<RectTransform>();
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.offsetMin = new Vector2(10, 10);
            containerRect.offsetMax = new Vector2(-10, -10);
            
            var layoutGroup = buttonContainerGO.AddComponent<VerticalLayoutGroup>();
            layoutGroup.spacing = 6; // 压缩间距从10回到6像素
            layoutGroup.padding = new RectOffset(6, 6, 6, 6); // 减少内边距从8到6像素
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = true;
            
            layerButtonContainer = buttonContainerGO.transform;
        }
        
        private void CreateSystemStatusInLeftPanel()
        {
            // 系统状态区域 - 占左侧面板下部分（扩展到65%）
            var statusAreaGO = new GameObject("SystemStatusArea");
            statusAreaGO.transform.SetParent(leftPanel.transform, false);
            
            var statusAreaRect = statusAreaGO.AddComponent<RectTransform>();
            statusAreaRect.anchorMin = new Vector2(0, 0);
            statusAreaRect.anchorMax = new Vector2(1, 0.65f); // 从0.5f增加到0.65f，给系统状态更多空间
            statusAreaRect.offsetMin = new Vector2(10, 10);
            statusAreaRect.offsetMax = new Vector2(-10, -5); // 减少上边距
            
            var statusAreaBg = statusAreaGO.AddComponent<Image>();
            statusAreaBg.color = new Color(0.03f, 0.05f, 0.08f, 0.8f);
            
            // 系统状态文本
            var statusGO = new GameObject("SystemStatus");
            statusGO.transform.SetParent(statusAreaGO.transform, false);
            
            var statusRect = statusGO.AddComponent<RectTransform>();
            statusRect.anchorMin = Vector2.zero;
            statusRect.anchorMax = Vector2.one;
            statusRect.offsetMin = new Vector2(10, 10);
            statusRect.offsetMax = new Vector2(-10, -10);
            
            systemStatusText = statusGO.AddComponent<Text>();
            systemStatusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            systemStatusText.fontSize = 14; // 从10增加到14，更易阅读
            systemStatusText.color = new Color(0.8f, 0.9f, 1f);
            systemStatusText.alignment = TextAnchor.UpperLeft;
            
            UpdateSystemStatus();
        }
        
        private void CreateRightPanel()
        {
            rightPanel = new GameObject("RightPanel");
            rightPanel.transform.SetParent(mainPanel.transform, false);
            
            var rightRect = rightPanel.AddComponent<RectTransform>();
            rightRect.anchorMin = new Vector2(0.25f, 0);
            rightRect.anchorMax = new Vector2(1, 0.9f);
            rightRect.offsetMin = new Vector2(10, 20);
            rightRect.offsetMax = new Vector2(-20, 0);
            
            var rightBg = rightPanel.AddComponent<Image>();
            rightBg.color = new Color(0.05f, 0.08f, 0.12f, 0.9f);
            
            // 整个右侧面板都用作条目列表区域
            CreateFullEntryListArea();
        }
        
        private void CreateFullEntryListArea()
        {
            // 条目列表区域 - 占满整个右侧面板
            var listAreaGO = new GameObject("EntryListArea");
            listAreaGO.transform.SetParent(rightPanel.transform, false);
            
            var listAreaRect = listAreaGO.AddComponent<RectTransform>();
            listAreaRect.anchorMin = Vector2.zero;
            listAreaRect.anchorMax = Vector2.one;
            listAreaRect.offsetMin = new Vector2(10, 10);
            listAreaRect.offsetMax = new Vector2(-10, -10);
            
            var listAreaBg = listAreaGO.AddComponent<Image>();
            listAreaBg.color = new Color(0.03f, 0.05f, 0.08f, 0.8f);
            
            // 标题
            var titleGO = new GameObject("EntryListTitle");
            titleGO.transform.SetParent(listAreaGO.transform, false);
            
            var titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.95f);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.offsetMin = new Vector2(10, 0);
            titleRect.offsetMax = new Vector2(-10, -5);
            
            var titleText = titleGO.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleText.fontSize = 18; // 从16增加到18，标题更突出
            titleText.color = new Color(0.9f, 0.95f, 1f);
            // 添加LocalizedText组件
            var localizedText = titleGO.AddComponent<LocalizedText>();
            localizedText.TextKey = "encyclopedia.entry_list.title";
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.fontStyle = FontStyle.Bold;
            
            // 滚动视图 - 占据几乎整个区域
            var scrollViewGO = new GameObject("EntryScrollView");
            scrollViewGO.transform.SetParent(listAreaGO.transform, false);
            
            var scrollViewRect = scrollViewGO.AddComponent<RectTransform>();
            scrollViewRect.anchorMin = new Vector2(0, 0);
            scrollViewRect.anchorMax = new Vector2(1, 0.95f);
            scrollViewRect.offsetMin = new Vector2(5, 5);
            scrollViewRect.offsetMax = new Vector2(-5, -5);
            
            entryScrollRect = scrollViewGO.AddComponent<ScrollRect>();
            entryScrollRect.horizontal = false;
            entryScrollRect.vertical = true;
            
            // 创建滚动视图的内部组件
            CreateScrollViewComponents(scrollViewGO);
        }
        
        private void CreateScrollViewComponents(GameObject scrollViewGO)
        {
            // Viewport
            var viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(scrollViewGO.transform, false);
            
            var viewportRect = viewportGO.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            
            // 暂时禁用Mask，避免遮罩问题
            // var viewportMask = viewportGO.AddComponent<Mask>();
            // viewportMask.showMaskGraphic = false;
            
            var viewportImage = viewportGO.AddComponent<Image>();
            viewportImage.color = Color.clear; // 保持透明
            
            entryScrollRect.viewport = viewportRect;
            
            // Content
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewportGO.transform, false);
            
            var contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            // 让Content根据内容自动调整大小，避免固定高度导致的间距问题
            contentRect.sizeDelta = new Vector2(0, 0); // 初始高度为0，由ContentSizeFitter控制
            
            var layoutGroup = contentGO.AddComponent<VerticalLayoutGroup>();
            layoutGroup.spacing = 8; // 统一设置固定间距8像素
            layoutGroup.padding = new RectOffset(10, 10, 10, 10);
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true; // 强制控制子元素高度
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false; // 禁止强制扩展高度，保持每个条目的固定尺寸
            
            // 使用ContentSizeFitter根据内容自动调整Content高度
            var contentFitter = contentGO.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            entryScrollRect.content = contentRect;
            entryListContainer = contentGO.transform;
            
            if (showDebugInfo)
            {
                Debug.Log($"条目列表容器创建完成: {contentGO.name}");
                Debug.Log($"容器初始RectTransform: {contentRect.rect}");
            }
        }

        private void CreateDetailPanel()
        {
            detailPanel = new GameObject("DetailPanel");
            detailPanel.transform.SetParent(mainPanel.transform, false);
            
            var detailRect = detailPanel.AddComponent<RectTransform>();
            detailRect.anchorMin = new Vector2(0.25f, 0);
            detailRect.anchorMax = new Vector2(1, 0.9f);
            detailRect.offsetMin = new Vector2(20, 30);
            detailRect.offsetMax = new Vector2(-30, -10);
            
            var detailBg = detailPanel.AddComponent<Image>();
            detailBg.color = new Color(0.05f, 0.08f, 0.12f, 0.95f);
            
            // 创建详情面板标题栏
            CreateDetailHeader();
            
            // 创建详情内容区域
            CreateDetailContent();
            
            // 默认隐藏详情面板
            detailPanel.SetActive(false);
        }

        private void CreateDetailHeader()
        {
            // 标题栏
            var headerGO = new GameObject("DetailHeader");
            headerGO.transform.SetParent(detailPanel.transform, false);
            
            var headerRect = headerGO.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 0.9f);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.offsetMin = new Vector2(10, 0);
            headerRect.offsetMax = new Vector2(-50, -5);
            
            var headerBg = headerGO.AddComponent<Image>();
            headerBg.color = new Color(0.1f, 0.15f, 0.2f, 0.8f);
            
            // 标题文本（作为子对象）
            var titleTextGO = new GameObject("TitleText");
            titleTextGO.transform.SetParent(headerGO.transform, false);
            
            var titleTextRect = titleTextGO.AddComponent<RectTransform>();
            titleTextRect.anchorMin = Vector2.zero;
            titleTextRect.anchorMax = Vector2.one;
            titleTextRect.offsetMin = new Vector2(15, 0);
            titleTextRect.offsetMax = new Vector2(-15, 0);
            
            detailTitleText = titleTextGO.AddComponent<Text>();
            detailTitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            detailTitleText.fontSize = 20; // 从16增加到20，标题更突出
            detailTitleText.color = new Color(0.9f, 0.95f, 1f);
            detailTitleText.alignment = TextAnchor.MiddleLeft;
            detailTitleText.fontStyle = FontStyle.Bold;
            // 添加本地化组件到详情标题
            var detailTitleLocalizedText = titleTextGO.AddComponent<LocalizedText>();
            detailTitleLocalizedText.TextKey = "encyclopedia.detail.title";
            detailTitleText.text = GetLocalizedText("encyclopedia.detail.title");
            
            // 关闭按钮
            var closeButtonGO = new GameObject("DetailCloseButton");
            closeButtonGO.transform.SetParent(detailPanel.transform, false);
            
            var closeButtonRect = closeButtonGO.AddComponent<RectTransform>();
            closeButtonRect.anchorMin = new Vector2(1, 1);
            closeButtonRect.anchorMax = new Vector2(1, 1);
            closeButtonRect.pivot = new Vector2(1, 1);
            closeButtonRect.anchoredPosition = new Vector2(-5, -5);
            closeButtonRect.sizeDelta = new Vector2(40, 35);
            
            var closeButtonImage = closeButtonGO.AddComponent<Image>();
            closeButtonImage.color = new Color(0.6f, 0.2f, 0.2f, 0.8f);
            
            detailCloseButton = closeButtonGO.AddComponent<Button>();
            detailCloseButton.onClick.AddListener(CloseDetailPanel);
            
            // 关闭按钮文字
            var closeTextGO = new GameObject("Text");
            closeTextGO.transform.SetParent(closeButtonGO.transform, false);
            
            var closeTextRect = closeTextGO.AddComponent<RectTransform>();
            closeTextRect.anchorMin = Vector2.zero;
            closeTextRect.anchorMax = Vector2.one;
            closeTextRect.offsetMin = Vector2.zero;
            closeTextRect.offsetMax = Vector2.zero;
            
            var closeText = closeTextGO.AddComponent<Text>();
            closeText.text = "×";
            closeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            closeText.fontSize = 20; // 从16增加到20，关闭按钮更明显
            closeText.color = Color.white;
            closeText.alignment = TextAnchor.MiddleCenter;
        }

        private void CreateDetailContent()
        {
            // 内容区域
            var contentAreaGO = new GameObject("DetailContentArea");
            contentAreaGO.transform.SetParent(detailPanel.transform, false);
            
            var contentAreaRect = contentAreaGO.AddComponent<RectTransform>();
            contentAreaRect.anchorMin = new Vector2(0, 0);
            contentAreaRect.anchorMax = new Vector2(1, 0.9f);
            contentAreaRect.offsetMin = new Vector2(10, 10);
            contentAreaRect.offsetMax = new Vector2(-10, -5);
            
            // 图片区域（左侧）
            var imageAreaGO = new GameObject("ImageArea");
            imageAreaGO.transform.SetParent(contentAreaGO.transform, false);
            
            var imageAreaRect = imageAreaGO.AddComponent<RectTransform>();
            imageAreaRect.anchorMin = new Vector2(0, 0.5f);
            imageAreaRect.anchorMax = new Vector2(0.4f, 1);
            imageAreaRect.offsetMin = new Vector2(5, 5);
            imageAreaRect.offsetMax = new Vector2(-5, -5);
            
            var imageAreaBg = imageAreaGO.AddComponent<Image>();
            imageAreaBg.color = new Color(0.03f, 0.05f, 0.08f, 0.6f);
            
            // 3D模型显示区域
            var modelGO = new GameObject("Detail3DModel");
            modelGO.transform.SetParent(imageAreaGO.transform, false);
            
            var modelRect = modelGO.AddComponent<RectTransform>();
            modelRect.anchorMin = Vector2.zero;
            modelRect.anchorMax = Vector2.one;
            modelRect.offsetMin = new Vector2(10, 10);
            modelRect.offsetMax = new Vector2(-10, -10);
            
            // 集成Model3DViewer组件
            if (model3DViewer == null)
            {
                model3DViewer = modelGO.AddComponent<Model3DViewer>();
                
                if (showDebugInfo)
                {
                    Debug.Log("✅ 自动创建Model3DViewer组件");
                    Debug.Log($"  - Model3DViewer GameObject: {model3DViewer.gameObject.name}");
                    Debug.Log($"  - 父对象: {model3DViewer.transform.parent?.name}");
                    Debug.Log($"  - RectTransform: {model3DViewer.GetComponent<RectTransform>() != null}");
                }
            }
            
            // 配置Model3DViewer的RectTransform（如果它还没有正确设置）
            var viewerRect = model3DViewer.GetComponent<RectTransform>();
            if (viewerRect == null)
            {
                viewerRect = model3DViewer.gameObject.AddComponent<RectTransform>();
            }
            
            // 确保Model3DViewer占满整个模型显示区域
            viewerRect.anchorMin = Vector2.zero;
            viewerRect.anchorMax = Vector2.one;
            viewerRect.offsetMin = Vector2.zero;
            viewerRect.offsetMax = Vector2.zero;
            
            // 设置为3D模型显示区域的子对象
            model3DViewer.transform.SetParent(modelGO.transform, false);
            
            // 不再使用静态图片显示
            detailImage = null;
            
            // 描述区域（右侧和下方）
            var descAreaGO = new GameObject("DescriptionArea");
            descAreaGO.transform.SetParent(contentAreaGO.transform, false);
            
            var descAreaRect = descAreaGO.AddComponent<RectTransform>();
            descAreaRect.anchorMin = new Vector2(0.4f, 0);
            descAreaRect.anchorMax = new Vector2(1, 1);
            descAreaRect.offsetMin = new Vector2(5, 5);
            descAreaRect.offsetMax = new Vector2(-5, -5);
            
            var descAreaBg = descAreaGO.AddComponent<Image>();
            descAreaBg.color = new Color(0.03f, 0.05f, 0.08f, 0.6f);
            
            // 简化：直接在描述区域中显示文本，不使用滚动视图
            var descTextGO = new GameObject("DescriptionText");
            descTextGO.transform.SetParent(descAreaGO.transform, false);
            
            var descTextRect = descTextGO.AddComponent<RectTransform>();
            descTextRect.anchorMin = Vector2.zero;
            descTextRect.anchorMax = Vector2.one;
            descTextRect.offsetMin = new Vector2(15, 15);
            descTextRect.offsetMax = new Vector2(-15, -15);
            
            // 描述文本
            detailDescriptionText = descTextGO.AddComponent<Text>();
            detailDescriptionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            detailDescriptionText.fontSize = 16; // 从13增加到16，更易阅读
            detailDescriptionText.color = Color.white; // 使用纯白色，确保可见
            detailDescriptionText.alignment = TextAnchor.UpperLeft;
            detailDescriptionText.text = "选择一个条目查看详细信息...";
            
            if (showDebugInfo)
            {
                Debug.Log($"🎨 创建描述文本: 颜色={detailDescriptionText.color}, 字体={detailDescriptionText.font?.name}");
                Debug.Log($"  - 描述文本GameObject: {descTextGO.name}, 激活状态: {descTextGO.activeInHierarchy}");
                Debug.Log($"  - 父对象: {descAreaGO.name}, 激活状态: {descAreaGO.activeInHierarchy}");
            }
        }
        
        private void CreateLayerButtons()
        {
            layerButtons.Clear();
            
            for (int i = 0; i < layerNames.Length; i++)
            {
                string layerName = layerNames[i];
                var buttonGO = new GameObject($"LayerButton_{layerName}");
                buttonGO.transform.SetParent(layerButtonContainer, false);
                
                var buttonRect = buttonGO.AddComponent<RectTransform>();
                buttonRect.sizeDelta = new Vector2(0, 36); // 从42压缩到36，节省空间
                
                var buttonImage = buttonGO.AddComponent<Image>();
                buttonImage.color = new Color(0.2f, 0.3f, 0.5f, 0.8f);
                
                var button = buttonGO.AddComponent<Button>();
                
                // 按钮文字
                var textGO = new GameObject("Text");
                textGO.transform.SetParent(buttonGO.transform, false);
                
                var textRect = textGO.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(10, 0);
                textRect.offsetMax = new Vector2(-10, 0);
                
                var text = textGO.AddComponent<Text>();
                
                // 直接设置本地化文本，带调试输出
                string localizedLayerText = GetLocalizedText(layerNameKeys[i]);
                text.text = localizedLayerText;
                
                // 调试输出
                if (showDebugInfo && i < 3)
                {
                    Debug.Log($"地层按钮 {i}: 键='{layerNameKeys[i]}' -> 文本='{localizedLayerText}'");
                    Debug.Log($"LocalizationManager存在: {LocalizationManager.Instance != null}");
                    if (LocalizationManager.Instance != null)
                    {
                        Debug.Log($"LocalizationManager初始化: {LocalizationManager.Instance.IsInitialized}");
                    }
                }
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                text.fontSize = 14; // 从16调整到14，适应压缩的按钮高度
                text.color = Color.white;
                text.alignment = TextAnchor.MiddleLeft;
                
                // 添加点击事件
                string layerNameCopy = layerName; // 避免闭包问题
                button.onClick.AddListener(() => OnLayerButtonClicked(layerNameCopy));
                
                layerButtons.Add(button);
            }
            
            // 默认设置第一个地层为当前层，但不触发UI更新
            if (layerButtons.Count > 0)
            {
                currentLayerName = layerNames[0];
                // 设置第一个按钮为选中状态，但不触发点击事件
                var firstButtonImage = layerButtons[0].GetComponent<Image>();
                firstButtonImage.color = new Color(0.3f, 0.5f, 0.8f, 1f);
            }
        }
        
        /// <summary>
        /// 获取本地化文本的辅助方法
        /// </summary>
        private string GetLocalizedText(string key)
        {
            if (LocalizationManager.Instance != null && LocalizationManager.Instance.IsInitialized)
            {
                string result = LocalizationManager.Instance.GetText(key);
                if (showDebugInfo && key.Contains("layer"))
                {
                    Debug.Log($"GetLocalizedText: '{key}' -> '{result}'");
                }
                return result;
            }
            
            if (showDebugInfo)
            {
                Debug.LogWarning($"LocalizationManager不可用或未初始化，键: {key}");
            }
            return $"[{key}]"; // 如果本地化管理器不存在，显示键值
        }
        
        /// <summary>
        /// 获取本地化的条目类型
        /// </summary>
        private string GetLocalizedEntryType(EntryType entryType)
        {
            string key = entryType == EntryType.Mineral ? "encyclopedia.type.mineral" : "encyclopedia.type.fossil";
            return GetLocalizedText(key);
        }
        
        /// <summary>
        /// 获取本地化的稀有度
        /// </summary>
        private string GetLocalizedRarity(Rarity rarity)
        {
            string key = rarity switch
            {
                Rarity.Common => "encyclopedia.rarity.common",
                Rarity.Uncommon => "encyclopedia.rarity.uncommon", 
                Rarity.Rare => "encyclopedia.rarity.rare",
                _ => "encyclopedia.rarity.unknown"
            };
            return GetLocalizedText(key);
        }
        
        /// <summary>
        /// 获取本地化的岩石名称
        /// </summary>
        private string GetLocalizedRockName(string rockName)
        {
            if (string.IsNullOrEmpty(rockName))
                return "";
                
            // 岩石名称到本地化键的映射
            var rockKeyMapping = new System.Collections.Generic.Dictionary<string, string>
            {
                { "砾岩", "rock.conglomerate" },
                { "火山灰", "rock.volcanic_ash" },
                { "粉砂岩/砂岩", "rock.siltstone_sandstone" },
                { "砂岩/粉砂岩", "rock.sandstone_siltstone" },
                { "英安岩质熔结凝灰岩", "rock.dacitic_welded_tuff" },
                { "粉砂岩/细粒砂岩", "rock.siltstone_fine_sandstone" },
                { "凝灰岩", "rock.tuff" },
                { "凝灰质砂岩", "rock.tuffaceous_sandstone" },
                { "粉砂岩", "rock.siltstone" }
            };
            
            if (rockKeyMapping.TryGetValue(rockName, out string key))
            {
                return GetLocalizedText(key);
            }
            
            // 如果没有找到映射，返回原始名称
            return rockName;
        }
        
        /// <summary>
        /// 获取本地化的地层名称
        /// </summary>
        private string GetLocalizedLayerName(string layerName)
        {
            if (string.IsNullOrEmpty(layerName))
                return "";
                
            // 地层名称到本地化键的映射
            var layerKeyMapping = new System.Collections.Generic.Dictionary<string, string>
            {
                { "青葉山層", "encyclopedia.layer.aoba_mountain" },
                { "大年寺層", "encyclopedia.layer.dainenji" },
                { "向山層", "encyclopedia.layer.mukoyama" },
                { "広瀬川凝灰岩部層", "encyclopedia.layer.hirose_river_tuff" },
                { "竜ノ口層", "encyclopedia.layer.ryunokuchi" },
                { "亀岡層", "encyclopedia.layer.kameoka" }
            };
            
            if (layerKeyMapping.TryGetValue(layerName, out string key))
            {
                return GetLocalizedText(key);
            }
            
            // 如果没有找到映射，返回原始名称
            return layerName;
        }
        
        /// <summary>
        /// 获取本地化的描述文本
        /// </summary>
        private string GetLocalizedDescription(string originalDescription)
        {
            if (string.IsNullOrEmpty(originalDescription))
                return "";
                
            // 尝试通过内容生成本地化键
            string descriptionKey = GenerateDescriptionKey(originalDescription);
            if (!string.IsNullOrEmpty(descriptionKey))
            {
                string localizedText = GetLocalizedText(descriptionKey);
                if (!localizedText.StartsWith("[MISSING KEY]") && !localizedText.StartsWith("["))
                {
                    if (showDebugInfo)
                        Debug.Log($"✅ 描述本地化成功: '{originalDescription.Substring(0, Mathf.Min(20, originalDescription.Length))}...' -> '{descriptionKey}' -> '{localizedText.Substring(0, Mathf.Min(20, localizedText.Length))}...'");
                    return localizedText;
                }
            }
            
            // 如果没有找到本地化版本，返回原始文本
            if (showDebugInfo)
                Debug.LogWarning($"❌ 描述未找到本地化: '{originalDescription.Substring(0, Mathf.Min(30, originalDescription.Length))}...'");
            return originalDescription;
        }
        
        /// <summary>
        /// 根据描述内容生成本地化键
        /// </summary>
        private string GenerateDescriptionKey(string description)
        {
            if (string.IsNullOrEmpty(description))
                return null;
            
            // 根据描述内容的关键词生成键
            string cleanDesc = description.Trim();
            
            // 检查是否是化石描述格式："在{地层}中发现的{化石名}"
            if (cleanDesc.StartsWith("在") && cleanDesc.Contains("中发现的"))
            {
                return GenerateFossilDescriptionKey(cleanDesc);
            }
            
            // 常见矿物描述的关键词匹配
            var descriptionMapping = new Dictionary<string, string>
            {
                // 斜长石相关
                { "通常呈白色或灰色，有时带淡蓝或淡绿；玻璃光泽。", "mineral.description.plagioclase" },
                
                // 辉石相关  
                { "呈深绿色、褐色至黑色等深色，柱状晶形，光泽玻璃至暗淡。", "mineral.description.pyroxene" },
                
                // 角闪石相关
                { "颜色多为黑色、深绿色或深褐色，玻璃光泽。", "mineral.description.amphibole" },
                
                // 磁铁矿相关
                { "颜色通常黑色或灰色带棕色调，具金属光泽。", "mineral.description.magnetite_simple" },
                { "颜色通常黑色或灰色带棕色调，具金属光泽；断口不平，条痕黑色。", "mineral.description.magnetite_detailed" },
                
                // 橄榄石相关
                { "颜色多为橄榄绿至黄褐色，玻璃光泽，条痕白色，铁含量高的样品表面会氧化呈红色。", "mineral.description.olivine" },
                
                // 石英相关
                { "呈无色、粉色、橙色、白色、绿色、黄色、蓝色、紫色或深褐色等多种颜色，断口贝壳状，玻璃光泽，条痕白色。", "mineral.description.quartz" },
                
                // 长石相关
                { "颜色可为粉红、白、灰、褐或蓝色，玻璃光泽，条痕白色。", "mineral.description.feldspar" },
                
                // 黑云母相关
                { "颜色黑色至褐色或黄色，具玻璃至珍珠光泽，条痕白色，晶形常呈假六方片状。", "mineral.description.biotite" },
                
                // 锆石相关
                { "颜色深褐、黑、灰、浅褐、褐红、橙、粉红等多种,条痕无色,光泽油脂至金刚光泽。断口贝壳状至不平,晶体常为短柱状或水磨圆粒。", "mineral.description.zircon_short" },
                { "颜色深褐、黑、灰、浅褐、褐红、橙、粉红等多种，条痕无色，光泽油脂至金刚光泽，断口贝壳状至不平，晶体常为短柱状或水磨圆粒。", "mineral.description.zircon_long" },
                
                // 火山玻璃相关
                { "通常黑色，也有绿色或褐色，断口呈典型贝壳状，质地光滑玻璃状，玻璃光泽。", "mineral.description.volcanic_glass" },
                
                // 紫苏辉石相关
                { "颜色灰色、褐色或绿色，断口不平，光泽玻璃至珍珠，条痕灰白或绿灰，表面有铜红色金属光泽。", "mineral.description.hypersthene" },
                
                // 石榴石相关
                { "颜色几乎涵盖所有色谱，常见为红色；晶体为菱形十二面体或立方体，断口贝壳状至不平，光泽玻璃或树脂光泽，条痕白色。", "mineral.description.garnet" },
                
                // 粘土矿物相关
                { "以含水铝硅酸盐为主的细粒矿物集合体，质地柔软，湿润时具有可塑性；颜色多为白色、灰色或浅褐色，光泽土状，常呈土状或粉末状集合体。", "mineral.description.clay_minerals" },
                
                // 重矿物相关
                { "重矿物指密度较大的矿物（如锆石、钛磁铁矿、石榴石等）的集合体，常出现在砂中，颜色通常较深，粒度细小，具光泽。", "mineral.description.heavy_minerals" }
            };
            
            // 直接匹配
            if (descriptionMapping.TryGetValue(cleanDesc, out string key))
            {
                return key;
            }
            
            // 模糊匹配（去除标点符号和空格）
            string cleanInput = cleanDesc.Replace("，", "").Replace("。", "").Replace("；", "").Replace(" ", "");
            foreach (var kvp in descriptionMapping)
            {
                string cleanKey = kvp.Key.Replace("，", "").Replace("。", "").Replace("；", "").Replace(" ", "");
                if (cleanInput == cleanKey)
                {
                    return kvp.Value;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// 为化石描述生成本地化键
        /// </summary>
        private string GenerateFossilDescriptionKey(string description)
        {
            // 化石描述的通用模式："在{地层}中发现的{化石名}"
            // 提取化石名称
            int startIndex = description.LastIndexOf("的") + 1;
            if (startIndex > 0 && startIndex < description.Length)
            {
                string fossilName = description.Substring(startIndex);
                
                // 化石名称到本地化键的映射
                var fossilMapping = new Dictionary<string, string>
                {
                    { "植物遺骸", "fossil.description.plant_remains" },
                    { "浮遊性珪藻", "fossil.description.planktonic_diatoms" },
                    { "有孔虫", "fossil.description.foraminifera" },
                    { "貝類", "fossil.description.shellfish" },
                    { "淡水貝類", "fossil.description.freshwater_shellfish" },
                    { "葉化石", "fossil.description.plant_leaf_fossils" },
                    { "花粉化石", "fossil.description.pollen_fossils" },
                    { "魚類化石", "fossil.description.fish_fossils" },
                    { "珪化木", "fossil.description.silicified_wood" },
                    { "センダイヌノメハマグリ", "fossil.description.sendai_clam" },
                    { "タカハシホタテ", "fossil.description.takahashi_scallop" },
                    { "クジラ類化石", "fossil.description.cetacean_fossils" },
                    { "古サンゴ", "fossil.description.ancient_coral" },
                    { "古タコ", "fossil.description.ancient_octopus" },
                    { "古ヒトデ", "fossil.description.ancient_starfish" },
                    { "アンモナイト", "fossil.description.ammonite" },
                    { "三葉虫", "fossil.description.trilobite" }
                };
                
                if (fossilMapping.TryGetValue(fossilName, out string key))
                {
                    return key;
                }
            }
            
            // 如果无法识别，返回通用化石描述键
            return "fossil.description.generic";
        }
        
        /// <summary>
        /// 获取本地化的属性值
        /// </summary>
        private string GetLocalizedPropertyValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
                
            // 清理输入值（去除前后空格）
            string cleanValue = value.Trim();
            
            // 调试输出：显示输入值的详细信息
            if (showDebugInfo)
            {
                Debug.Log($"🔍 属性值本地化输入: '{cleanValue}' (长度: {cleanValue.Length})");
                // 显示每个字符的ASCII码
                string asciiInfo = "";
                foreach (char c in cleanValue)
                {
                    asciiInfo += $"'{c}'({(int)c}) ";
                }
                Debug.Log($"   字符详情: {asciiInfo}");
            }
                
            // 属性值的本地化映射 - 使用忽略大小写的字典
            var propertyMapping = new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
            {
                // 基本属性 - "无"和"None"变体
                { "无", "encyclopedia.property.none" },
                { "None", "encyclopedia.property.none" },
                { "なし", "encyclopedia.property.none" },
                
                // 磁性相关 - 包含所有可能的变体
                { "无磁性", "encyclopedia.property.non_magnetic" },
                { "无（抗磁性）", "encyclopedia.property.diamagnetic" },
                { "なし（反磁性）", "encyclopedia.property.diamagnetic" },
                { "弱磁性", "encyclopedia.property.weak_magnetic" },
                { "弱磁性（顺磁性）", "encyclopedia.property.weak_magnetic_paramagnetic" },
                { "强磁性", "encyclopedia.property.strong_magnetic" },
                { "None (Diamagnetic)", "encyclopedia.property.diamagnetic" },
                { "Non-magnetic", "encyclopedia.property.non_magnetic" },
                { "Nonmagnetic", "encyclopedia.property.non_magnetic" },
                { "Weak magnetic", "encyclopedia.property.weak_magnetic" },
                { "Strong magnetic", "encyclopedia.property.strong_magnetic" },
                { "Weak magnetic (paramagnetic)", "encyclopedia.property.weak_magnetic_paramagnetic" },
                
                // 颜色相关
                { "无色", "encyclopedia.property.colorless" },
                { "无色/无绿色", "encyclopedia.property.colorless_green" },
                { "无色/无薄色", "encyclopedia.property.colorless_fade" },
                { "无色/无褪色", "encyclopedia.property.colorless_fade" },
                { "无色／无绿色", "encyclopedia.property.colorless_green" }, // 全角斜杠
                { "强变色", "encyclopedia.property.strong_pleochroism" },
                { "强变色：绿色到深绿色或棕色", "encyclopedia.property.strong_pleochroism_green_brown" },
                { "无变色", "encyclopedia.property.no_pleochroism" },
                { "弱变色", "encyclopedia.property.weak_pleochroism" },
                
                // 复杂偏光颜色相关
                { "灰色至粉红/绿色变色", "encyclopedia.property.gray_pink_green_pleochroism" },
                { "粉红到绿色变色", "encyclopedia.property.pink_green_pleochroism" },
                { "X=浅绿/浅褐黄，Y=浅褐/浅黄绿/紫色，Z=浅绿/灰绿/紫色", "encyclopedia.property.complex_pleochroism_xyz" },
                { "强变色/不透明", "encyclopedia.property.strong_pleochroism_opaque" },
                { "强变色（棕色/绿色）", "encyclopedia.property.strong_pleochroism_brown_green" },
                { "无（镁富石）；Fe富含样品α=γ淡黄到β橙黄", "encyclopedia.property.mg_rich_fe_rich_pleochroism" },
                
                // 其他属性相关
                { "不透明", "encyclopedia.property.opaque" },
                { "多样", "encyclopedia.property.variable" },
                { "多样/取决于组成", "encyclopedia.property.variable_composition_dependent" },
                { "无/浅色", "encyclopedia.property.none_light_color" },
                { "无/玻璃质", "encyclopedia.property.none_glassy" },
                { "无色/无变色", "encyclopedia.property.colorless_no_change" },
                { "无（玻璃质）", "encyclopedia.property.none_glassy_quality" },
                
                { "Colorless", "encyclopedia.property.colorless" },
                { "Colorless/Green", "encyclopedia.property.colorless_green" },
                { "Strong pleochroism", "encyclopedia.property.strong_pleochroism" },
                { "No pleochroism", "encyclopedia.property.no_pleochroism" },
                { "Weak pleochroism", "encyclopedia.property.weak_pleochroism" },
                
                // 紫外荧光相关
                { "大多数不发光", "encyclopedia.property.mostly_non_fluorescent" },
                { "大多数不发光（部分石英可发光）", "encyclopedia.property.mostly_non_fluorescent_quartz" },
                { "Most do not fluoresce", "encyclopedia.property.mostly_non_fluorescent" },
                { "Most do not fluoresce (some quartz may fluoresce)", "encyclopedia.property.mostly_non_fluorescent_quartz" },
                
                // 反应性 - 包含所有可能的变体和拼写方式
                { "无反应", "encyclopedia.property.non_reactive" },
                { "反应なし", "encyclopedia.property.non_reactive" },
                { "Non-reactive", "encyclopedia.property.non_reactive" },
                { "Nonreactive", "encyclopedia.property.non_reactive" },
                { "Non reactive", "encyclopedia.property.non_reactive" },
                
                // 强度
                { "弱", "encyclopedia.property.weak" },
                { "强", "encyclopedia.property.strong" },
                { "Weak", "encyclopedia.property.weak" },
                { "Strong", "encyclopedia.property.strong" }
            };
            
            // 尝试映射
            if (propertyMapping.TryGetValue(cleanValue, out string key))
            {
                string localizedValue = GetLocalizedText(key);
                if (showDebugInfo)
                {
                    Debug.Log($"✅ 属性本地化成功: '{cleanValue}' -> '{key}' -> '{localizedValue}'");
                }
                return localizedValue;
            }
            
            // 如果没有找到映射，尝试模糊匹配
            foreach (var kvp in propertyMapping)
            {
                if (string.Equals(kvp.Key.Replace("-", "").Replace(" ", ""), 
                                  cleanValue.Replace("-", "").Replace(" ", ""), 
                                  System.StringComparison.OrdinalIgnoreCase))
                {
                    string localizedValue = GetLocalizedText(kvp.Value);
                    if (showDebugInfo)
                    {
                        Debug.Log($"✅ 属性模糊匹配成功: '{cleanValue}' ≈ '{kvp.Key}' -> '{kvp.Value}' -> '{localizedValue}'");
                    }
                    return localizedValue;
                }
            }
            
            // 如果仍然没有找到映射，返回原始值并输出详细调试信息
            if (showDebugInfo)
            {
                Debug.LogWarning($"❌ 未找到属性值映射: '{cleanValue}' (原始: '{value}', 清理后长度: {cleanValue.Length})");
                Debug.LogWarning($"   可用映射键示例: None, Non-reactive, None (Diamagnetic), Colorless");
                
                // 列出所有映射键供参考
                var allKeys = string.Join(", ", propertyMapping.Keys.Take(10));
                Debug.LogWarning($"   前10个映射键: {allKeys}");
            }
            return cleanValue;
        }
        
        /// <summary>
        /// 获取本地化的条目显示名称
        /// </summary>
        private string GetLocalizedEntryDisplayName(EncyclopediaEntry entry)
        {
            // 获取本地化的地层名称
            string localizedLayerName = GetLocalizedLayerName(entry.layerName);
            
            // 获取本地化的条目名称
            string localizedEntryName = entry.displayName;
            if (LocalizationManager.Instance != null && LocalizationManager.Instance.IsInitialized)
            {
                var currentLang = LocalizationManager.Instance.CurrentLanguage;
                
                switch (currentLang)
                {
                    case LanguageSettings.Language.English:
                        localizedEntryName = !string.IsNullOrEmpty(entry.nameEN) ? entry.nameEN : entry.displayName;
                        break;
                    case LanguageSettings.Language.Japanese:
                        localizedEntryName = !string.IsNullOrEmpty(entry.nameJA) ? entry.nameJA : entry.displayName;
                        break;
                    case LanguageSettings.Language.ChineseSimplified:
                        localizedEntryName = !string.IsNullOrEmpty(entry.nameCN) ? entry.nameCN : entry.displayName;
                        break;
                }
            }
            
            // 格式化完整名称
            if (entry.entryType == EntryType.Mineral)
            {
                string localizedRockName = GetLocalizedRockName(entry.rockName);
                return $"{localizedLayerName}-{localizedRockName}-{localizedEntryName}";
            }
            else
            {
                return $"{localizedLayerName}-{localizedEntryName}";
            }
        }
        
        /// <summary>
        /// 更新系统状态
        /// </summary>
        private void UpdateSystemStatus()
        {
            if (systemStatusText == null) return;
            
            // 清除现有内容，重新创建美观的状态面板
            ClearSystemStatusContent();
            CreateBeautifulSystemStatus();
        }
        
        /// <summary>
        /// 清除系统状态内容
        /// </summary>
        private void ClearSystemStatusContent()
        {
            // 清除systemStatusText所在容器的所有子对象（除了文本本身）
            Transform statusContainer = systemStatusText.transform.parent;
            for (int i = statusContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = statusContainer.GetChild(i);
                if (child.gameObject != systemStatusText.gameObject)
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }
        
        /// <summary>
        /// 创建美观的系统状态面板
        /// </summary>
        private void CreateBeautifulSystemStatus()
        {
            Transform statusContainer = systemStatusText.transform.parent;
            
            // 隐藏原来的文本，我们将创建新的UI元素
            systemStatusText.gameObject.SetActive(false);
            
            // 创建主标题
            CreateStatusTitle(statusContainer, "encyclopedia.system_status.title", 0.95f);
            
            // 创建数据系统状态
            float yPos = 0.85f; // 从0.75f向上移动
            if (EncyclopediaData.Instance != null && EncyclopediaData.Instance.IsDataLoaded)
            {
                CreateStatusItem(statusContainer, "encyclopedia.data_system.label", "encyclopedia.status.loaded", Color.green, yPos);
                yPos -= 0.06f; // 减少间距从0.08f到0.06f
                CreateDataStats(statusContainer, yPos);
                yPos -= 0.15f; // 减少间距从0.2f到0.15f
            }
            else
            {
                CreateStatusItem(statusContainer, "encyclopedia.data_system.label", "encyclopedia.status.not_initialized", Color.red, yPos);
                yPos -= 0.08f;
            }
            
            // 创建收集系统状态和进度条
            if (CollectionManager.Instance != null)
            {
                var stats = CollectionManager.Instance.CurrentStats;
                if (stats != null)
                {
                    CreateStatusItem(statusContainer, "encyclopedia.collection_system.label", "encyclopedia.status.running", Color.green, yPos);
                    yPos -= 0.06f; // 减少间距
                    CreateProgressBars(statusContainer, stats, yPos);
                    yPos -= 0.20f; // 减少间距从0.25f到0.20f
                }
                else
                {
                    CreateStatusItem(statusContainer, "encyclopedia.collection_system.label", "encyclopedia.status.data_error", Color.yellow, yPos);
                    yPos -= 0.06f;
                }
            }
            else
            {
                CreateStatusItem(statusContainer, "encyclopedia.collection_system.label", "encyclopedia.status.not_initialized", Color.red, yPos);
                yPos -= 0.06f;
            }
            
            // 创建当前地层信息
            if (!string.IsNullOrEmpty(currentLayerName))
            {
                CreateCurrentLayerInfo(statusContainer, yPos);
            }
        }
        
        /// <summary>
        /// 创建状态标题
        /// </summary>
        private void CreateStatusTitle(Transform parent, string titleKey, float yPos, params object[] formatArgs)
        {
            var titleGO = new GameObject("StatusTitle");
            titleGO.transform.SetParent(parent, false);
            
            var titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, yPos - 0.05f);
            titleRect.anchorMax = new Vector2(1, yPos);
            titleRect.offsetMin = new Vector2(10, 0);
            titleRect.offsetMax = new Vector2(-10, 0);
            
            var titleText = titleGO.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 16;
            titleText.color = new Color(0.8f, 0.9f, 1f);
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.fontStyle = FontStyle.Bold;
            
            // 获取本地化文本（支持格式化）
            string localizedText = "";
            if (LocalizationManager.Instance != null)
            {
                if (formatArgs != null && formatArgs.Length > 0)
                {
                    localizedText = LocalizationManager.Instance.GetText(titleKey, formatArgs);
                }
                else
                {
                    localizedText = LocalizationManager.Instance.GetText(titleKey);
                }
            }
            else
            {
                localizedText = $"[{titleKey}]";
            }
            
            titleText.text = localizedText;
            
            // 添加本地化组件（可选，用于运行时语言切换）
            var localizedTextComponent = titleGO.AddComponent<LocalizedText>();
            localizedTextComponent.TextKey = titleKey;
        }
        
        /// <summary>
        /// 创建状态项
        /// </summary>
        private void CreateStatusItem(Transform parent, string labelKey, string statusKey, Color statusColor, float yPos)
        {
            var itemGO = new GameObject($"StatusItem_{labelKey}");
            itemGO.transform.SetParent(parent, false);
            
            var itemRect = itemGO.AddComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0, yPos - 0.06f);
            itemRect.anchorMax = new Vector2(1, yPos);
            itemRect.offsetMin = new Vector2(15, 0);
            itemRect.offsetMax = new Vector2(-15, 0);
            
            // 标签
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(itemGO.transform, false);
            
            var labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(0.6f, 1);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            
            var labelText = labelGO.AddComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 12;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleLeft;
            
            // 添加本地化组件到标签
            var labelLocalizedText = labelGO.AddComponent<LocalizedText>();
            labelLocalizedText.TextKey = labelKey;
            labelText.text = GetLocalizedText(labelKey);
            
            // 状态
            var statusGO = new GameObject("Status");
            statusGO.transform.SetParent(itemGO.transform, false);
            
            var statusRect = statusGO.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.6f, 0);
            statusRect.anchorMax = new Vector2(1, 1);
            statusRect.offsetMin = Vector2.zero;
            statusRect.offsetMax = Vector2.zero;
            
            var statusText = statusGO.AddComponent<Text>();
            statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            statusText.fontSize = 12;
            statusText.color = statusColor;
            statusText.alignment = TextAnchor.MiddleRight;
            statusText.fontStyle = FontStyle.Bold;
            
            // 添加本地化组件到状态
            var statusLocalizedText = statusGO.AddComponent<LocalizedText>();
            statusLocalizedText.TextKey = statusKey;
            statusText.text = GetLocalizedText(statusKey);
        }
        
        /// <summary>
        /// 创建数据统计信息
        /// </summary>
        private void CreateDataStats(Transform parent, float yPos)
        {
            var data = EncyclopediaData.Instance;
            if (data == null) return;
            
            string[] stats = {
                $"矿物: {data.TotalMinerals}",
                $"化石: {data.TotalFossils}",
                $"地层: {data.LayerNames.Count}"
            };
            
            for (int i = 0; i < stats.Length; i++)
            {
                var statGO = new GameObject($"DataStat_{i}");
                statGO.transform.SetParent(parent, false);
                
                var statRect = statGO.AddComponent<RectTransform>();
                statRect.anchorMin = new Vector2(0.2f, yPos - 0.05f - i * 0.05f);
                statRect.anchorMax = new Vector2(1, yPos - i * 0.05f);
                statRect.offsetMin = new Vector2(0, 0);
                statRect.offsetMax = new Vector2(-15, 0);
                
                var statText = statGO.AddComponent<Text>();
                statText.text = stats[i];
                statText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                statText.fontSize = 11;
                statText.color = new Color(0.8f, 0.8f, 0.8f);
                statText.alignment = TextAnchor.MiddleLeft;
            }
        }
        
        /// <summary>
        /// 创建进度条
        /// </summary>
        private void CreateProgressBars(Transform parent, CollectionStats stats, float yPos)
        {
            // 总进度条
            CreateProgressBar(parent, "encyclopedia.progress.overall", stats.overallProgress, Color.cyan, yPos);
            
            // 矿物进度条
            CreateProgressBar(parent, "encyclopedia.progress.minerals", stats.mineralProgress, Color.yellow, yPos - 0.08f);
            
            // 化石进度条
            CreateProgressBar(parent, "encyclopedia.progress.fossils", stats.fossilProgress, Color.green, yPos - 0.16f);
        }
        
        /// <summary>
        /// 创建单个进度条
        /// </summary>
        private void CreateProgressBar(Transform parent, string labelKey, float progress, Color color, float yPos)
        {
            var progressGO = new GameObject($"Progress_{labelKey}");
            progressGO.transform.SetParent(parent, false);
            
            var progressRect = progressGO.AddComponent<RectTransform>();
            progressRect.anchorMin = new Vector2(0, yPos - 0.05f);
            progressRect.anchorMax = new Vector2(1, yPos);
            progressRect.offsetMin = new Vector2(15, 5);
            progressRect.offsetMax = new Vector2(-15, -5);
            
            // 背景
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(progressGO.transform, false);
            
            var bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            
            var bgImage = bgGO.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            // 进度填充
            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(progressGO.transform, false);
            
            var fillRect = fillGO.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(progress, 1);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            
            var fillImage = fillGO.AddComponent<Image>();
            fillImage.color = color;
            
            // 标签文本
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(progressGO.transform, false);
            
            var labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(0.4f, 1);
            labelRect.offsetMin = new Vector2(5, 0);
            labelRect.offsetMax = new Vector2(0, 0);
            
            var labelText = labelGO.AddComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 10;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleLeft;
            
            // 添加本地化组件到标签
            var labelLocalizedText = labelGO.AddComponent<LocalizedText>();
            labelLocalizedText.TextKey = labelKey;
            labelText.text = GetLocalizedText(labelKey);
            
            // 百分比文本
            var percentGO = new GameObject("Percent");
            percentGO.transform.SetParent(progressGO.transform, false);
            
            var percentRect = percentGO.AddComponent<RectTransform>();
            percentRect.anchorMin = new Vector2(0.6f, 0);
            percentRect.anchorMax = new Vector2(1, 1);
            percentRect.offsetMin = Vector2.zero;
            percentRect.offsetMax = new Vector2(-5, 0);
            
            var percentText = percentGO.AddComponent<Text>();
            percentText.text = $"{progress:P1}";
            percentText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            percentText.fontSize = 10;
            percentText.color = color;
            percentText.alignment = TextAnchor.MiddleRight;
            percentText.fontStyle = FontStyle.Bold;
        }
        
        /// <summary>
        /// 创建地层统计项（特殊处理数字显示）
        /// </summary>
        private void CreateLayerStatsItem(Transform parent, string labelKey, int count, Color textColor, float yPos)
        {
            var itemGO = new GameObject($"LayerStats_{labelKey}");
            itemGO.transform.SetParent(parent, false);
            
            var itemRect = itemGO.AddComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0, yPos - 0.06f);
            itemRect.anchorMax = new Vector2(1, yPos);
            itemRect.offsetMin = new Vector2(15, 0);
            itemRect.offsetMax = new Vector2(-15, 0);
            
            // 标签
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(itemGO.transform, false);
            
            var labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(0.6f, 1);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            
            var labelText = labelGO.AddComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 12;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleLeft;
            
            // 添加本地化组件到标签
            var labelLocalizedText = labelGO.AddComponent<LocalizedText>();
            labelLocalizedText.TextKey = labelKey;
            labelText.text = GetLocalizedText(labelKey);
            
            // 数字显示
            var countGO = new GameObject("Count");
            countGO.transform.SetParent(itemGO.transform, false);
            
            var countRect = countGO.AddComponent<RectTransform>();
            countRect.anchorMin = new Vector2(0.6f, 0);
            countRect.anchorMax = new Vector2(1, 1);
            countRect.offsetMin = Vector2.zero;
            countRect.offsetMax = Vector2.zero;
            
            var countText = countGO.AddComponent<Text>();
            countText.text = count.ToString();
            countText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            countText.fontSize = 12;
            countText.color = textColor;
            countText.alignment = TextAnchor.MiddleRight;
            countText.fontStyle = FontStyle.Bold;
        }
        
        /// <summary>
        /// 创建当前地层信息
        /// </summary>
        private void CreateCurrentLayerInfo(Transform parent, float yPos)
        {
            // 创建地层标题
            CreateStatusTitle(parent, "encyclopedia.current_layer.title", yPos, currentLayerName);
            yPos -= 0.06f; // 减少间距从0.08f到0.06f
            
            if (EncyclopediaData.Instance != null && EncyclopediaData.Instance.IsDataLoaded)
            {
                var entries = EncyclopediaData.Instance.GetEntriesByLayer(currentLayerName);
                var minerals = entries.Where(e => e.entryType == EntryType.Mineral).ToList();
                var fossils = entries.Where(e => e.entryType == EntryType.Fossil).ToList();
                
                // 地层统计 - 使用更紧凑的间距，但这些需要显示数字，所以我们需要特殊处理
                CreateLayerStatsItem(parent, "encyclopedia.current_layer.total_entries", entries.Count, Color.white, yPos);
                yPos -= 0.05f; // 减少间距从0.06f到0.05f
                CreateLayerStatsItem(parent, "encyclopedia.current_layer.minerals", minerals.Count, Color.yellow, yPos);  
                yPos -= 0.05f; // 减少间距从0.06f到0.05f
                CreateLayerStatsItem(parent, "encyclopedia.current_layer.fossils", fossils.Count, Color.green, yPos);
                
                // 地层进度
                if (CollectionManager.Instance != null)
                {
                    var layerStats = CollectionManager.Instance.GetLayerStats(currentLayerName);
                    if (layerStats != null)
                    {
                        yPos -= 0.06f; // 减少间距从0.08f到0.06f
                        CreateProgressBar(parent, "encyclopedia.progress.discovery", layerStats.progress, new Color(0.8f, 0.6f, 1f), yPos);
                    }
                }
            }
        }
        
        /// <summary>
        /// 地层按钮点击事件
        /// </summary>
        private void OnLayerButtonClicked(string layerName)
        {
            currentLayerName = layerName;
            
            // 更新按钮样式
            for (int i = 0; i < layerButtons.Count; i++)
            {
                bool isSelected = layerNames[i] == currentLayerName;
                var buttonImage = layerButtons[i].GetComponent<Image>();
                buttonImage.color = isSelected ? 
                    new Color(0.3f, 0.5f, 0.8f, 1f) : 
                    new Color(0.2f, 0.3f, 0.5f, 0.8f);
            }
            
            // 更新右侧内容
            UpdateSystemStatus();
            UpdateEntryList();
            
            if (showDebugInfo)
                Debug.Log($"选择了地层: {layerName}");
        }
        
        /// <summary>
        /// 更新条目列表
        /// </summary>
        private void UpdateEntryList()
        {
            if (showDebugInfo)
                Debug.Log($"开始更新条目列表，当前地层: {currentLayerName}");

            if (entryListContainer == null)
            {
                if (showDebugInfo)
                    Debug.LogError("entryListContainer为空！");
                return;
            }

            // 清除现有条目
            ClearEntryList();

            // 检查数据是否加载
            if (EncyclopediaData.Instance == null)
            {
                if (showDebugInfo)
                    Debug.LogWarning("EncyclopediaData.Instance为空");
                CreateNoDataMessage();
                return;
            }

            if (!EncyclopediaData.Instance.IsDataLoaded)
            {
                if (showDebugInfo)
                    Debug.LogWarning("数据未加载完成");
                CreateNoDataMessage();
                return;
            }

            // 获取当前地层的条目
            if (string.IsNullOrEmpty(currentLayerName))
            {
                if (showDebugInfo)
                    Debug.LogWarning("当前地层名称为空");
                CreateNoLayerMessage();
                return;
            }

            var entries = EncyclopediaData.Instance.GetEntriesByLayer(currentLayerName);
            
            if (showDebugInfo)
                Debug.Log($"获取到 {entries?.Count ?? 0} 个条目");
            
            if (entries == null || entries.Count == 0)
            {
                CreateEmptyLayerMessage();
                return;
            }

            // 创建条目列表项
            foreach (var entry in entries)
            {
                CreateEntryItem(entry);
            }

            // 移除调试测试元素
            // CreateTestVisibilityElement(); // 已不需要
            
            // 多重刷新确保布局正确
            StartCoroutine(RefreshLayoutCoroutine());

            if (showDebugInfo)
            {
                Debug.Log($"✅ 成功创建了 {entries.Count} 个条目UI");
                Debug.Log($"条目列表容器子对象数量: {entryListContainer.childCount}");
                Debug.Log($"条目列表容器是否激活: {entryListContainer.gameObject.activeInHierarchy}");
                Debug.Log($"条目列表容器位置: {entryListContainer.position}");
                Debug.Log($"条目列表容器RectTransform: {((RectTransform)entryListContainer).rect}");
                
                // 检查前几个条目的状态
                for (int i = 0; i < Mathf.Min(3, entryListContainer.childCount); i++)
                {
                    var child = entryListContainer.GetChild(i);
                    Debug.Log($"条目 {i}: 名称={child.name}, 激活={child.gameObject.activeInHierarchy}, 位置={child.position}");
                }
            }
        }

        /// <summary>
        /// 创建测试可见性元素
        /// </summary>
        private void CreateTestVisibilityElement()
        {
            var testGO = new GameObject("TEST_VISIBILITY");
            testGO.transform.SetParent(entryListContainer, false);
            
            var testRect = testGO.AddComponent<RectTransform>();
            testRect.anchorMin = new Vector2(0, 1);
            testRect.anchorMax = new Vector2(1, 1);
            testRect.pivot = new Vector2(0.5f, 1);
            testRect.sizeDelta = new Vector2(0, 50);
            
            var testBg = testGO.AddComponent<Image>();
            testBg.color = Color.magenta; // 使用最显眼的紫红色
            
            var testTextGO = new GameObject("TestText");
            testTextGO.transform.SetParent(testGO.transform, false);
            
            var testTextRect = testTextGO.AddComponent<RectTransform>();
            testTextRect.anchorMin = Vector2.zero;
            testTextRect.anchorMax = Vector2.one;
            testTextRect.offsetMin = Vector2.zero;
            testTextRect.offsetMax = Vector2.zero;
            
            var testText = testTextGO.AddComponent<Text>();
            testText.text = "🔴 测试可见性元素 - 如果你看到这个说明UI正常";
            testText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            testText.fontSize = 16;
            testText.color = Color.white;
            testText.alignment = TextAnchor.MiddleCenter;
            testText.fontStyle = FontStyle.Bold;
            
            Debug.Log($"🧪 创建了测试可见性元素，如果Content区域正常工作应该能看到紫红色背景的测试文字");
        }

        /// <summary>
        /// 刷新布局协程 - 多帧刷新确保布局正确
        /// </summary>
        private System.Collections.IEnumerator RefreshLayoutCoroutine()
        {
            // 第一次刷新
            yield return null;
            Canvas.ForceUpdateCanvases();
            
            var layoutGroup = entryListContainer.GetComponent<VerticalLayoutGroup>();
            var contentSizeFitter = entryListContainer.GetComponent<ContentSizeFitter>();
            
            if (layoutGroup != null)
            {
                layoutGroup.SetLayoutVertical();
                layoutGroup.CalculateLayoutInputVertical();
            }
            
            if (contentSizeFitter != null)
            {
                contentSizeFitter.SetLayoutVertical();
            }
            
            // 第二次刷新
            yield return null;
            Canvas.ForceUpdateCanvases();
            
            if (layoutGroup != null)
            {
                layoutGroup.SetLayoutVertical();
            }
            
            // 最终调试输出
            if (showDebugInfo)
            {
                yield return null;
                Debug.Log("=== 🔍 UI层级和可见性诊断 ===");
                
                // 检查整个UI层级
                Debug.Log($"MainPanel 激活: {mainPanel.activeInHierarchy}, 位置: {mainPanel.transform.position}");
                Debug.Log($"RightPanel 激活: {rightPanel.activeInHierarchy}, 位置: {rightPanel.transform.position}");
                Debug.Log($"EntryListContainer 激活: {entryListContainer.gameObject.activeInHierarchy}, 位置: {entryListContainer.position}");
                
                var entryContainerRect = entryListContainer as RectTransform;
                Debug.Log($"EntryListContainer RectTransform: rect={entryContainerRect.rect}, anchoredPosition={entryContainerRect.anchoredPosition}");
                
                // 检查ScrollRect设置
                Debug.Log($"ScrollRect enabled: {entryScrollRect.enabled}, viewport: {entryScrollRect.viewport != null}");
                if (entryScrollRect.viewport != null)
                {
                    Debug.Log($"Viewport rect: {entryScrollRect.viewport.rect}");
                }
                
                // 检查每个条目的详细状态
                for (int i = 0; i < Mathf.Min(3, entryListContainer.childCount); i++)
                {
                    var child = entryListContainer.GetChild(i);
                    var childRect = child as RectTransform;
                    var childImage = child.GetComponent<Image>();
                    var childText = child.GetComponentInChildren<Text>();
                    
                    Debug.Log($"📋 条目 {i}: '{child.name}'");
                    Debug.Log($"  - 激活状态: {child.gameObject.activeInHierarchy}");
                    Debug.Log($"  - 世界位置: {child.position}");
                    Debug.Log($"  - RectTransform: {childRect.rect}, anchoredPos: {childRect.anchoredPosition}");
                    Debug.Log($"  - Image组件: {childImage != null}, 颜色: {childImage?.color}");
                    Debug.Log($"  - Text组件: {childText != null}, 内容: '{childText?.text}', 颜色: {childText?.color}");
                    Debug.Log($"  - Canvas渲染顺序: {child.GetComponentInParent<Canvas>()?.sortingOrder}");
                }
                
                // 检查Content的ContentSizeFitter状态
                var contentFitter = entryListContainer.GetComponent<ContentSizeFitter>();
                if (contentFitter != null)
                {
                    Debug.Log($"ContentSizeFitter: vertical={contentFitter.verticalFit}");
                }
                
                // 检查VerticalLayoutGroup状态
                var entryLayoutGroup = entryListContainer.GetComponent<VerticalLayoutGroup>();
                if (entryLayoutGroup != null)
                {
                    Debug.Log($"VerticalLayoutGroup: enabled={entryLayoutGroup.enabled}, spacing={entryLayoutGroup.spacing}");
                }
            }
        }

        /// <summary>
        /// 清除条目列表
        /// </summary>
        private void ClearEntryList()
        {
            foreach (var item in entryItems)
            {
                if (item != null)
                    DestroyImmediate(item);
            }
            entryItems.Clear();
        }

        /// <summary>
        /// 创建条目项
        /// </summary>
        private void CreateEntryItem(EncyclopediaEntry entry)
        {
            var itemGO = new GameObject($"EntryItem_{entry.id}");
            itemGO.transform.SetParent(entryListContainer, false);

            var itemRect = itemGO.AddComponent<RectTransform>();
            // 设置正确的锚点和大小，让VerticalLayoutGroup正确处理
            itemRect.anchorMin = new Vector2(0, 1);
            itemRect.anchorMax = new Vector2(1, 1);
            itemRect.pivot = new Vector2(0.5f, 1);
            itemRect.sizeDelta = new Vector2(0, 45); // 从40增加到45，适应16号字体
            
            // 添加LayoutElement组件以确保布局组件正确处理大小
            var layoutElement = itemGO.AddComponent<UnityEngine.UI.LayoutElement>();
            layoutElement.minHeight = 45;
            layoutElement.preferredHeight = 45;

            // 背景
            var itemBg = itemGO.AddComponent<Image>();
            
            // 检查是否已发现
            bool isDiscovered = CollectionManager.Instance != null && 
                               CollectionManager.Instance.IsEntryDiscovered(entry.id);
            
            // 测试模式：让所有条目都显示为已发现状态
            if (showDebugInfo)
            {
                isDiscovered = true;
            }
            
            // 使用正常的颜色方案
            itemBg.color = isDiscovered ? 
                new Color(0.2f, 0.3f, 0.4f, 0.8f) :     // 已发现：深蓝色
                new Color(0.3f, 0.2f, 0.15f, 0.8f);     // 未发现：深棕色

            // 按钮组件
            var button = itemGO.AddComponent<Button>();
            button.targetGraphic = itemBg;
            button.onClick.AddListener(() => OnEntryItemClicked(entry));

            // 文本
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(itemGO.transform, false);

            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 0);
            textRect.offsetMax = new Vector2(-30, 0);

            var text = textGO.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 16; // 从14增加到16，更易阅读
            text.alignment = TextAnchor.MiddleLeft;
            text.fontStyle = FontStyle.Bold; // 加粗字体

            // 设置显示文本和颜色
            if (isDiscovered)
            {
                string statusText = GetLocalizedText("encyclopedia.detail.discovered");
                text.text = $"{GetLocalizedEntryDisplayName(entry)} ({statusText})";
                text.color = new Color(0.9f, 0.95f, 1f); // 已发现：亮白色
            }
            else
            {
                string statusText = GetLocalizedText("encyclopedia.detail.not_discovered"); 
                text.text = $"??? ({statusText})";
                text.color = new Color(0.7f, 0.6f, 0.5f); // 未发现：暗灰棕色
            }
            
            // 调试输出
            if (showDebugInfo && entryItems.Count <= 3)
            {
                Debug.Log($"条目本地化调试: {GetLocalizedEntryDisplayName(entry)}");
                Debug.Log($"发现状态键获取结果: '{GetLocalizedText("encyclopedia.detail.discovered")}'");
                Debug.Log($"LocalizationManager是否存在: {LocalizationManager.Instance != null}");
                if (LocalizationManager.Instance != null)
                {
                    Debug.Log($"LocalizationManager是否初始化: {LocalizationManager.Instance.IsInitialized}");
                    Debug.Log($"当前语言: {LocalizationManager.Instance.CurrentLanguage}");
                }
            }

            // 类型图标
            var iconGO = new GameObject("TypeIcon");
            iconGO.transform.SetParent(itemGO.transform, false);

            var iconRect = iconGO.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(1, 0.5f);
            iconRect.anchorMax = new Vector2(1, 0.5f);
            iconRect.pivot = new Vector2(1, 0.5f);
            iconRect.anchoredPosition = new Vector2(-5, 0);
            iconRect.sizeDelta = new Vector2(20, 20);

            var iconText = iconGO.AddComponent<Text>();
            iconText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            iconText.fontSize = 14; // 从10增加到14，图标文字更清楚
            iconText.alignment = TextAnchor.MiddleCenter;
            iconText.text = entry.entryType == EntryType.Mineral ? GetLocalizedText("encyclopedia.type.mineral")[0].ToString() : GetLocalizedText("encyclopedia.type.fossil")[0].ToString();
            iconText.color = entry.entryType == EntryType.Mineral ? 
                new Color(0.8f, 0.6f, 0.2f) : 
                new Color(0.6f, 0.8f, 0.4f);

            entryItems.Add(itemGO);
            
            if (showDebugInfo && entryItems.Count <= 3)
            {
                Debug.Log($"🔍 创建条目 {entryItems.Count}: {entry.GetFormattedDisplayName()}");
                Debug.Log($"  - 发现状态: {isDiscovered}");
                Debug.Log($"  - 背景颜色: {itemBg.color}");
                Debug.Log($"  - 文本内容: '{text.text}', 颜色: {text.color}");
                Debug.Log($"  - 条目大小: {itemRect.sizeDelta}, 锚点位置: {itemRect.anchoredPosition}");
                Debug.Log($"  - GameObject激活: {itemGO.activeInHierarchy}");
                Debug.Log($"  - 世界位置: {itemGO.transform.position}");
            }
        }

        /// <summary>
        /// 条目项点击事件
        /// </summary>
        private void OnEntryItemClicked(EncyclopediaEntry entry)
        {
            if (showDebugInfo)
                Debug.Log($"点击了条目: {entry.GetFormattedDisplayName()}");
            
            // 检查是否已发现
            bool isDiscovered = CollectionManager.Instance != null && 
                               CollectionManager.Instance.IsEntryDiscovered(entry.id);
            
            // 测试模式：让所有条目都可以查看详情
            if (showDebugInfo)
            {
                isDiscovered = true;
            }
            
            if (!isDiscovered)
            {
                if (showDebugInfo)
                    Debug.Log("条目尚未发现，无法查看详情");
                return;
            }

            // 显示详情面板
            ShowEntryDetail(entry);
        }

        /// <summary>
        /// 显示条目详情
        /// </summary>
        private void ShowEntryDetail(EncyclopediaEntry entry)
        {
            if (detailPanel == null) return;

            currentDetailEntry = entry;

            // 设置标题
            if (detailTitleText != null)
            {
                detailTitleText.text = entry.GetFormattedDisplayName();
            }

            // 设置描述信息
            if (detailDescriptionText != null)
            {
                string description = BuildEntryDescription(entry);
                detailDescriptionText.text = description;
                
                if (showDebugInfo)
                {
                    Debug.Log($"🔍 设置详情描述: 长度={description.Length}");
                    Debug.Log($"  - 文本颜色: {detailDescriptionText.color}");
                    Debug.Log($"  - 字体: {detailDescriptionText.font?.name}");
                    Debug.Log($"  - 字体大小: {detailDescriptionText.fontSize}");
                    Debug.Log($"  - 对齐方式: {detailDescriptionText.alignment}");
                    Debug.Log($"  - GameObject激活: {detailDescriptionText.gameObject.activeInHierarchy}");
                    Debug.Log($"  - 前100字符: {description.Substring(0, Mathf.Min(100, description.Length))}");
                }
            }

            // 先显示详情面板
            detailPanel.SetActive(true);

            // 然后加载并设置图片
            LoadEntryImage(entry);

            if (showDebugInfo)
                Debug.Log($"显示详情: {entry.GetFormattedDisplayName()}");
        }

        /// <summary>
        /// 构建条目描述信息 - 美观版本
        /// </summary>
        private string BuildEntryDescription(EncyclopediaEntry entry)
        {
            string description = "";
            
            // 基本信息区块
            description += $"{GetLocalizedText("encyclopedia.detail.basic_info")}\n\n";
            // 获取矿物名称（条目显示名称的最后一部分）
            string[] nameParts = GetLocalizedEntryDisplayName(entry).Split('-');
            string mineralName = nameParts.Length > 0 ? nameParts[nameParts.Length - 1] : entry.displayName;
            description += $"{GetLocalizedText("encyclopedia.detail.name")}: {mineralName}\n";
            description += $"{GetLocalizedText("encyclopedia.detail.type")}: {GetLocalizedEntryType(entry.entryType)}\n";
            description += $"{GetLocalizedText("encyclopedia.detail.layer")}: {GetLocalizedLayerName(entry.layerName)}\n";
            
            if (entry.entryType == EntryType.Mineral)
            {
                description += $"{GetLocalizedText("encyclopedia.detail.rock_type")}: {GetLocalizedRockName(entry.rockName)}\n";
                if (entry.percentage > 0)
                {
                    // 处理百分比显示
                    float displayPercentage = entry.percentage;
                    if (displayPercentage < 1.0f && displayPercentage > 0)
                    {
                        displayPercentage *= 100f; // 0.3 -> 30
                    }
                    description += $"{GetLocalizedText("encyclopedia.detail.percentage")}: {displayPercentage:F1}%\n";
                }
            }
            else
            {
                description += $"{GetLocalizedText("encyclopedia.detail.discovery_probability")}: {entry.discoveryProbability:F2}\n";
            }
            
            description += $"{GetLocalizedText("encyclopedia.detail.rarity")}: {GetLocalizedRarity(entry.rarity)}\n";
            
            // 发现状态
            bool isDiscovered = CollectionManager.Instance?.IsEntryDiscovered(entry.id) == true;
            if (showDebugInfo) isDiscovered = true; // 测试模式
            
            string statusKey = isDiscovered ? "encyclopedia.detail.discovered" : "encyclopedia.detail.not_discovered";  
            description += $"{GetLocalizedText("encyclopedia.detail.discovery_status")}: {GetLocalizedText(statusKey)}\n";
            
            // 详细描述区块 - 避免重复显示
            bool hasDescription = !string.IsNullOrEmpty(entry.description);
            bool hasAppearance = !string.IsNullOrEmpty(entry.appearance);
            
            // 检查description和appearance是否重复
            bool isContentSame = hasDescription && hasAppearance && 
                                entry.description.Trim() == entry.appearance.Trim();
            
            if (hasDescription || hasAppearance)
            {
                description += $"\n{GetLocalizedText("encyclopedia.detail.description")}\n\n";
                
                if (isContentSame)
                {
                    // 如果内容相同，只显示一次
                    string localizedDesc = GetLocalizedDescription(entry.description);
                    description += $"   {localizedDesc}\n\n";
                }
                else
                {
                    // 如果内容不同，分别显示
                    if (hasDescription)
                    {
                        string localizedDesc = GetLocalizedDescription(entry.description);
                        description += $"   {localizedDesc}\n\n";
                    }
                    if (hasAppearance && !isContentSame)
                    {
                        string localizedAppearance = GetLocalizedDescription(entry.appearance);
                        description += $"   {GetLocalizedText("encyclopedia.detail.appearance")}: {localizedAppearance}\n\n";
                    }
                }
            }
            
            // 物理属性区块（仅矿物）
            if (entry.entryType == EntryType.Mineral)
            {
                bool hasPhysicalProps = !string.IsNullOrEmpty(entry.mohsHardness) ||
                                      !string.IsNullOrEmpty(entry.density) ||
                                      !string.IsNullOrEmpty(entry.uvFluorescence) ||
                                      !string.IsNullOrEmpty(entry.magnetism) ||
                                      !string.IsNullOrEmpty(entry.polarizedColor);
                
                if (hasPhysicalProps)
                {
                    description += $"\n{GetLocalizedText("encyclopedia.detail.physical_properties")}\n\n";
                    
                    if (!string.IsNullOrEmpty(entry.mohsHardness))
                        description += $"{GetLocalizedText("encyclopedia.detail.mohs_hardness")}: {entry.mohsHardness}\n";
                    if (!string.IsNullOrEmpty(entry.density))
                        description += $"{GetLocalizedText("encyclopedia.detail.density")}: {entry.density}\n";
                    if (!string.IsNullOrEmpty(entry.uvFluorescence))
                    {
                        string localizedUV = GetLocalizedPropertyValue(entry.uvFluorescence);
                        description += $"{GetLocalizedText("encyclopedia.detail.uv_fluorescence")}: {localizedUV}\n";
                        if (showDebugInfo)
                            Debug.Log($"UV荧光属性: 原始='{entry.uvFluorescence}' -> 本地化='{localizedUV}'");
                    }
                    if (!string.IsNullOrEmpty(entry.magnetism))
                    {
                        string localizedMagnetism = GetLocalizedPropertyValue(entry.magnetism);
                        description += $"{GetLocalizedText("encyclopedia.detail.magnetism")}: {localizedMagnetism}\n";
                        if (showDebugInfo)
                            Debug.Log($"磁性属性: 原始='{entry.magnetism}' -> 本地化='{localizedMagnetism}'");
                    }
                    if (!string.IsNullOrEmpty(entry.polarizedColor))
                    {
                        string localizedColor = GetLocalizedPropertyValue(entry.polarizedColor);
                        description += $"{GetLocalizedText("encyclopedia.detail.polarized_color")}: {localizedColor}\n";
                        if (showDebugInfo)
                            Debug.Log($"偏光颜色属性: 原始='{entry.polarizedColor}' -> 本地化='{localizedColor}'");
                    }
                    
                    string acidReactionKey = entry.acidReaction ? "encyclopedia.detail.acid_reaction_yes" : "encyclopedia.detail.acid_reaction_no";
                    string localizedAcidReaction = GetLocalizedText(acidReactionKey);
                    description += $"{GetLocalizedText("encyclopedia.detail.acid_reaction")}: {localizedAcidReaction}\n";
                    if (showDebugInfo)
                        Debug.Log($"酸性反应: 布尔='{entry.acidReaction}' -> 键='{acidReactionKey}' -> 本地化='{localizedAcidReaction}'");
                }
            }
            
            // 收集信息区块
            if (entry.isDiscovered && entry.discoveryCount > 0)
            {
                description += $"📊 {GetLocalizedText("encyclopedia.detail.collection_info")}\n\n";
                description += $"   {GetLocalizedText("encyclopedia.detail.first_discovered")}: {entry.firstDiscoveredTime:yyyy年MM月dd日 HH:mm}\n";
                description += $"   {GetLocalizedText("encyclopedia.detail.discovery_count")}: {entry.discoveryCount}{GetLocalizedText("encyclopedia.detail.times")}\n";
            }
            
            return description;
        }

        /// <summary>
        /// 获取稀有度名称
        /// </summary>
        private string GetRarityName(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Common: return "常见";
                case Rarity.Uncommon: return "不常见";
                case Rarity.Rare: return "稀有";
                default: return "未知";
            }
        }

        /// <summary>
        /// 加载条目图片
        /// </summary>
        private void LoadEntryImage(EncyclopediaEntry entry)
        {
            if (showDebugInfo)
            {
                Debug.Log($"🎯 准备显示3D模型: {entry.id}");
                Debug.Log($"  - 模型文件: {entry.modelFile}");
                Debug.Log($"  - 3D模型对象: {(entry.model3D != null ? entry.model3D.name : "null")}");
            }
            
            // 集成3D模型查看器
            if (model3DViewer != null)
            {
                if (entry.model3D != null)
                {
                    // 显示加载提示
                    ShowModelLoadingState(true);
                    
                    // 显示3D模型
                    model3DViewer.ShowModel(entry.model3D);
                    
                    // 隐藏加载提示，显示模型
                    ShowModelLoadingState(false);
                    
                    if (showDebugInfo)
                    {
                        Debug.Log($"✅ 成功加载3D模型: {entry.model3D.name}");
                    }
                }
                else
                {
                    if (showDebugInfo)
                    {
                        Debug.Log($"⚠️ 条目无3D模型，尝试测试模式加载");
                    }
                    
                    // 测试模式：如果条目没有3D模型，尝试加载第一个可用的矿物模型
                    model3DViewer.TestLoadFirstMineralModel();
                    
                    // 如果测试也失败了，显示"无模型可用"提示
                    if (!model3DViewer.HasModel())
                    {
                        ShowNoModelAvailableMessage(entry);
                        
                        if (showDebugInfo)
                        {
                            Debug.Log($"⚠️ 测试模式也未找到可用模型: {entry.id} ({entry.modelFile})");
                        }
                    }
                }
            }
            else
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning("❌ Model3DViewer组件未分配！");
                }
            }
            // 现在不需要detailImage了，因为我们改用3D模型显示
        }

        /// <summary>
        /// 关闭详情面板
        /// </summary>
        private void CloseDetailPanel()
        {
            // 清理3D模型
            if (model3DViewer != null)
            {
                model3DViewer.ClearModel();
                
                // 隐藏"无模型可用"提示
                Transform noModelMessage = model3DViewer.transform.Find("NoModelMessage");
                if (noModelMessage != null)
                {
                    noModelMessage.gameObject.SetActive(false);
                }
                
                if (showDebugInfo)
                {
                    Debug.Log("🧹 清理3D模型和提示信息");
                }
            }
            
            if (detailPanel != null)
            {
                detailPanel.SetActive(false);
                currentDetailEntry = null;
                
                if (showDebugInfo)
                    Debug.Log("关闭详情面板");
            }
        }

        /// <summary>
        /// 创建无数据消息
        /// </summary>
        private void CreateNoDataMessage()
        {
            CreateMessageItem("⚠️ 数据系统未加载", "请等待数据初始化完成", Color.yellow);
        }

        /// <summary>
        /// 创建无地层消息
        /// </summary>
        private void CreateNoLayerMessage()
        {
            CreateMessageItem("ℹ️ 未选择地层", "请从左侧选择一个地层", new Color(0.7f, 0.8f, 1f));
        }

        /// <summary>
        /// 创建空地层消息
        /// </summary>
        private void CreateEmptyLayerMessage()
        {
            CreateMessageItem("📭 地层为空", $"{currentLayerName} 暂无条目数据", new Color(0.8f, 0.8f, 0.8f));
        }

        /// <summary>
        /// 创建消息项
        /// </summary>
        private void CreateMessageItem(string title, string message, Color color)
        {
            var itemGO = new GameObject("MessageItem");
            itemGO.transform.SetParent(entryListContainer, false);

            var itemRect = itemGO.AddComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(0, 50);

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(itemGO.transform, false);

            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 5);
            textRect.offsetMax = new Vector2(-10, -5);

            var text = textGO.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 11;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = $"{title}\n{message}";
            text.color = color;

            entryItems.Add(itemGO);
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
            if (mainPanel != null)
            {
                mainPanel.SetActive(true);
                isOpen = true;
                
                // 确保有选中的地层
                if (string.IsNullOrEmpty(currentLayerName) && layerNames.Length > 0)
                {
                    currentLayerName = layerNames[0];
                }
                
                UpdateSystemStatus(); // 刷新信息
                UpdateEntryList(); // 刷新条目列表
                
                // 强制刷新本地化文本
                RefreshLocalization();
                
                // 启用鼠标光标，禁用摄像机控制
                EnableMouseCursor();
                
                if (showDebugInfo)
                {
                    Debug.Log("图鉴已打开");
                    Debug.Log($"FirstPersonController找到: {firstPersonController != null}");
                    Debug.Log($"鼠标状态: Cursor.lockState={Cursor.lockState}, Cursor.visible={Cursor.visible}");
                }
            }
        }
        
        /// <summary>
        /// 关闭图鉴
        /// </summary>
        public void CloseEncyclopedia()
        {
            if (mainPanel != null)
            {
                mainPanel.SetActive(false);
                isOpen = false;
                
                // 恢复原始鼠标状态，启用摄像机控制
                DisableMouseCursor();
                
                if (showDebugInfo)
                    Debug.Log("图鉴已关闭");
            }
        }
        
        /// <summary>
        /// 检查图鉴是否打开
        /// </summary>
        public bool IsOpen()
        {
            return isOpen;
        }
        
        /// <summary>
        /// 手动刷新信息
        /// </summary>
        [ContextMenu("刷新信息")]
        public void RefreshInfo()
        {
            UpdateSystemStatus();
        }
        
        /// <summary>
        /// 添加数据系统初始化器
        /// </summary>
        [ContextMenu("添加数据系统")]
        public void AddDataSystems()
        {
            Debug.Log("=== 开始初始化数据系统 ===");
            
            // 如果没有数据系统，创建它们
            if (EncyclopediaData.Instance == null)
            {
                var dataGO = new GameObject("EncyclopediaData");
                var dataComponent = dataGO.AddComponent<EncyclopediaData>();
                Debug.Log("✅ 创建了EncyclopediaData组件");
                
                // 立即尝试初始化
                dataComponent.SendMessage("Awake", SendMessageOptions.DontRequireReceiver);
                dataComponent.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
            }
            else
            {
                Debug.Log("ℹ️ EncyclopediaData已存在");
            }
            
            if (CollectionManager.Instance == null)
            {
                var collectionGO = new GameObject("CollectionManager");
                var collectionComponent = collectionGO.AddComponent<CollectionManager>();
                Debug.Log("✅ 创建了CollectionManager组件");
                
                // 立即尝试初始化
                collectionComponent.SendMessage("Awake", SendMessageOptions.DontRequireReceiver);
                collectionComponent.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
            }
            else
            {
                Debug.Log("ℹ️ CollectionManager已存在");
            }
            
            // 延迟刷新信息，确保数据加载完成
            StartCoroutine(RefreshAfterDelay());
        }
        
        /// <summary>
        /// 自动初始化数据系统
        /// </summary>
        private System.Collections.IEnumerator AutoInitializeDataSystems()
        {
            // 等待一帧，确保所有组件初始化完成
            yield return null;
            
            if (showDebugInfo)
                Debug.Log("🔧 自动检查数据系统...");
            
            // 检查并创建数据系统
            if (EncyclopediaData.Instance == null || CollectionManager.Instance == null)
            {
                if (showDebugInfo)
                    Debug.Log("⚠️ 检测到缺失的数据系统，自动创建中...");
                
                AddDataSystems();
            }
            else
            {
                if (showDebugInfo)
                    Debug.Log("✅ 数据系统已存在");
            }
        }
        
        /// <summary>
        /// 延迟刷新信息
        /// </summary>
        private System.Collections.IEnumerator RefreshAfterDelay()
        {
            yield return new WaitForSeconds(1f);
            RefreshInfo();
            if (showDebugInfo)
                Debug.Log("📊 信息已刷新");
            
            // 再次检查数据状态
            yield return new WaitForSeconds(2f);
            RefreshInfo();
            if (showDebugInfo)
                Debug.Log("📊 二次信息刷新完成");
        }
        
        /// <summary>
        /// 启用鼠标光标，禁用摄像机控制
        /// </summary>
        private void EnableMouseCursor()
        {
            // 显示鼠标光标
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            // 禁用第一人称控制器的鼠标控制
            if (firstPersonController != null)
            {
                firstPersonController.enableMouseLook = false;
                if (showDebugInfo)
                    Debug.Log("已禁用摄像机鼠标控制");
            }
        }
        
        /// <summary>
        /// 恢复原始鼠标状态，启用摄像机控制
        /// </summary>
        private void DisableMouseCursor()
        {
            // 恢复原始鼠标状态
            Cursor.lockState = originalCursorLockMode;
            Cursor.visible = originalCursorVisible;
            
            // 启用第一人称控制器的鼠标控制
            if (firstPersonController != null)
            {
                firstPersonController.enableMouseLook = true;
                if (showDebugInfo)
                    Debug.Log("已启用摄像机鼠标控制");
            }
        }
        
        /// <summary>
        /// 显示模型加载状态
        /// </summary>
        private void ShowModelLoadingState(bool isLoading)
        {
            // 这里可以在未来添加加载动画或提示
            // 现在暂时只输出调试信息
            if (showDebugInfo && isLoading)
            {
                Debug.Log("🔄 正在加载3D模型...");
            }
        }
        
        /// <summary>
        /// 显示无模型可用的友好提示
        /// </summary>
        private void ShowNoModelAvailableMessage(EncyclopediaEntry entry)
        {
            // 在Model3DViewer区域显示友好的提示信息
            // 这里可以创建一个临时的Text组件来显示提示
            if (model3DViewer != null)
            {
                // 创建提示文本（如果还不存在）
                Transform noModelMessage = model3DViewer.transform.Find("NoModelMessage");
                if (noModelMessage == null)
                {
                    var messageGO = new GameObject("NoModelMessage");
                    messageGO.transform.SetParent(model3DViewer.transform, false);
                    
                    var messageRect = messageGO.AddComponent<RectTransform>();
                    messageRect.anchorMin = Vector2.zero;
                    messageRect.anchorMax = Vector2.one;
                    messageRect.offsetMin = Vector2.zero;
                    messageRect.offsetMax = Vector2.zero;
                    
                    var messageText = messageGO.AddComponent<Text>();
                    messageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    messageText.fontSize = 14;
                    messageText.color = new Color(0.8f, 0.8f, 0.8f, 0.8f);
                    messageText.alignment = TextAnchor.MiddleCenter;
                    
                    noModelMessage = messageGO.transform;
                }
                
                var textComponent = noModelMessage.GetComponent<Text>();
                if (textComponent != null)
                {
                    string entryTypeName = GetLocalizedEntryType(entry.entryType);
                    textComponent.text = $"暂无{entryTypeName}3D模型\n\n{entry.GetFormattedDisplayName()}\n\n请查看右侧详细描述";
                }
                
                noModelMessage.gameObject.SetActive(true);
            }
        }
    }
}
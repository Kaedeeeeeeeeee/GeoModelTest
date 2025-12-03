using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using SampleCuttingSystem;
using System.Linq;
using QuestSystem;

namespace WorkbenchSystem
{
    /// <summary>
    /// 显微镜交互控制器：点击桌面上的显微镜以打开样本观察界面
    /// </summary>
    public class MicroscopeController : MonoBehaviour
    {
        [Header("UI")]
        public GameObject uiRoot;
        public RawImage previewImage;
        public Text detailText;
        public RectTransform sampleListContainer;
        public GameObject sampleButtonPrefab;
        public Color buttonNormalColor = new Color(0.2f, 0.2f, 0.2f, 0.85f);
        public Color buttonSelectedColor = new Color(0.3f, 0.55f, 0.9f, 0.9f);
        public RectTransform dropArea;
        public Text compositionText;
        public Text inventoryCountText;

        [Header("Camera/Preview")]
        public Camera microscopeCamera;
        public RenderTexture renderTexture;
        public int previewSize = 512;
        public float rotateSpeed = 80f;
        public float zoomSpeed = 1.5f;
        public float minZoom = 0.4f;
        public float maxZoom = 1.8f;

        [Header("Target")]
        [Tooltip("可选：显微镜机身的碰撞体，如果为空将自动添加 BoxCollider")]
        public Collider targetCollider;
        [Header("Highlight")]
        public Color highlightColor = new Color(0.2f, 0.6f, 1f, 1f);
        public float highlightEmission = 1.5f;
        [Header("Composition/Preview")]
        public LayerDatabaseMapper layerDatabaseMapper;
        public int maxPreviewInstances = 24;
        public float previewScatterRadius = 0.35f;

        private readonly List<Button> sampleButtons = new List<Button>();
        private SampleItem currentSample;
        private Transform previewRoot;
        private GameObject previewSample;
        private Light previewLight;
        private bool isActive;
        private int previewLayer;
        private Renderer[] highlightRenderers;
        private bool isHighlighted;
        private readonly Dictionary<Renderer, Color[]> originalEmissionColors = new Dictionary<Renderer, Color[]>();
        private readonly Dictionary<Renderer, bool[]> originalEmissionEnabled = new Dictionary<Renderer, bool[]>();
        private SampleItem draggingSample;
        public SampleItem DraggingSample => draggingSample;
        private readonly List<GameObject> spawnedMineralVisuals = new List<GameObject>();
        private static readonly Dictionary<string, string> MaterialToLayerMap = new Dictionary<string, string>
        {
            {"dem2", "青葉山層"},
            {"dem1", "大年寺層"},
            {"dem3", "広瀬川凝灰岩部層"},
            {"dem6", "亀岡層"},
            {"dem7", "竜ノ口層"},
            {"dem",  "向山層"}
        };

        void Start()
        {
            EnsureCollider();
            EnsureCameraAndPreview();
            EnsureUI();
            EnsureLayerDatabase();
            BuildSampleList();
            HideUI();
            CacheRenderers();
        }

        void Update()
        {
            if (!isActive) return;

            HandlePreviewInput();

            // 退出快捷键
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Close();
            }
        }

        public void HandleClick()
        {
            if (!isActive)
            {
                Open();
            }
            else
            {
                Close();
            }
        }

        void Open()
        {
            isActive = true;
            ShowUI();
            EnableCamera(true);
            QuestSystem.QuestUI.SetForceHidden(true);
            if (currentSample == null)
            {
                AutoSelectFirstSample();
            }
        }

        public void Close()
        {
            isActive = false;
            EnableCamera(false);
            HideUI();
            SetHighlight(false);
            QuestSystem.QuestUI.SetForceHidden(false);
        }

        void EnsureCollider()
        {
            if (targetCollider == null)
            {
                targetCollider = GetComponent<Collider>();
            }

            if (targetCollider == null)
            {
                var renderer = GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    var box = gameObject.AddComponent<BoxCollider>();

                    // 将世界空间的包围盒换算到本地坐标，避免出现超大碰撞体
                    Bounds bounds = renderer.bounds;
                    Vector3 localCenter = transform.InverseTransformPoint(bounds.center);

                    Vector3 size = bounds.size;
                    Vector3 lossy = transform.lossyScale;
                    // 防止除以0
                    if (Mathf.Abs(lossy.x) < 1e-4f) lossy.x = 1f;
                    if (Mathf.Abs(lossy.y) < 1e-4f) lossy.y = 1f;
                    if (Mathf.Abs(lossy.z) < 1e-4f) lossy.z = 1f;
                    Vector3 localSize = new Vector3(size.x / lossy.x, size.y / lossy.y, size.z / lossy.z);

                    box.center = localCenter;
                    box.size = localSize;
                    targetCollider = box;
                }
                else
                {
                    targetCollider = gameObject.AddComponent<BoxCollider>();
                }
            }
        }

        void EnsureCameraAndPreview()
        {
            previewLayer = LayerMask.NameToLayer("UI");
            if (previewLayer < 0) previewLayer = 0;

            if (microscopeCamera == null)
            {
                var camObj = new GameObject("MicroscopeCamera");
                camObj.transform.SetParent(transform, false);
                microscopeCamera = camObj.AddComponent<Camera>();
                microscopeCamera.transform.localPosition = new Vector3(0f, 0.1f, 1.5f);
                microscopeCamera.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                microscopeCamera.nearClipPlane = 0.01f;
                microscopeCamera.farClipPlane = 6f;
                microscopeCamera.fieldOfView = 40f;
            }

            microscopeCamera.clearFlags = CameraClearFlags.SolidColor;
            microscopeCamera.backgroundColor = new Color(0.05f, 0.05f, 0.05f, 1f);
            microscopeCamera.cullingMask = 1 << previewLayer;

            if (renderTexture == null)
            {
                renderTexture = new RenderTexture(previewSize, previewSize, 16, RenderTextureFormat.ARGB32);
                renderTexture.name = "MicroscopeRT";
            }
            microscopeCamera.targetTexture = renderTexture;

            EnsurePreviewRig();
            EnableCamera(false);
        }

        void EnsurePreviewRig()
        {
            if (previewRoot == null)
            {
                var rootObj = new GameObject("MicroscopePreviewRoot");
                rootObj.transform.SetParent(microscopeCamera.transform, false);
                rootObj.transform.localPosition = new Vector3(0f, 0f, 0.6f);
                previewRoot = rootObj.transform;
            }

            previewRoot.gameObject.layer = previewLayer;

            if (previewSample == null)
            {
                previewSample = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                previewSample.name = "PreviewSample";
                previewSample.transform.SetParent(previewRoot, false);
                previewSample.transform.localScale = Vector3.one * 0.35f;
                previewSample.layer = previewLayer;
                var col = previewSample.GetComponent<Collider>();
                if (col != null) Destroy(col);
            }

            if (previewLight == null)
            {
                var lightObj = new GameObject("MicroscopeLight");
                previewLight = lightObj.AddComponent<Light>();
                previewLight.type = LightType.Spot;
                previewLight.spotAngle = 60f;
                previewLight.intensity = 2.1f;
                previewLight.range = 3f;
                lightObj.transform.SetParent(previewRoot, false);
                lightObj.transform.localPosition = new Vector3(0.15f, 0.35f, 0.6f);
                lightObj.transform.localRotation = Quaternion.Euler(20f, -160f, 0f);
                lightObj.gameObject.layer = previewLayer;
            }

            // 防止层级污染
            previewRoot.gameObject.hideFlags = HideFlags.DontSave;
            previewSample.hideFlags = HideFlags.DontSave;
        }

        void EnsureUI()
        {
            if (uiRoot != null)
            {
                if (previewImage != null && renderTexture != null)
                {
                    previewImage.texture = renderTexture;
                }
                return;
            }

            // Canvas
            GameObject canvasObj = new GameObject("MicroscopeCanvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 220;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();

            // 全屏遮罩
            GameObject maskObj = new GameObject("Mask");
            maskObj.transform.SetParent(canvasObj.transform, false);
            var maskRect = maskObj.AddComponent<RectTransform>();
            maskRect.anchorMin = Vector2.zero;
            maskRect.anchorMax = Vector2.one;
            maskRect.offsetMin = Vector2.zero;
            maskRect.offsetMax = Vector2.zero;
            var maskImg = maskObj.AddComponent<Image>();
            maskImg.color = new Color(0.05f, 0.05f, 0.05f, 0.95f);

            // 主面板
            GameObject panelObj = new GameObject("MicroscopePanel");
            panelObj.transform.SetParent(maskObj.transform, false);
            var panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.offsetMin = new Vector2(32f, 32f);
            panelRect.offsetMax = new Vector2(-32f, -32f);
            var bg = panelObj.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.98f);

            // 左侧背包区域
            GameObject listObj = new GameObject("SampleList");
            listObj.transform.SetParent(panelObj.transform, false);
            var listRect = listObj.AddComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0f, 0f);
            listRect.anchorMax = new Vector2(0.32f, 1f);
            listRect.offsetMin = new Vector2(16f, 16f);
            listRect.offsetMax = new Vector2(-12f, -16f);
            var listBg = listObj.AddComponent<Image>();
            listBg.color = new Color(0.12f, 0.12f, 0.12f, 0.9f);
            // 计数/标题
            GameObject countObj = new GameObject("InventoryCount");
            countObj.transform.SetParent(listObj.transform, false);
            var countRect = countObj.AddComponent<RectTransform>();
            countRect.anchorMin = new Vector2(0f, 1f);
            countRect.anchorMax = new Vector2(1f, 1f);
            countRect.pivot = new Vector2(0f, 1f);
            countRect.sizeDelta = new Vector2(0f, 32f);
            inventoryCountText = countObj.AddComponent<Text>();
            inventoryCountText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            inventoryCountText.fontSize = 18;
            inventoryCountText.color = Color.white;
            inventoryCountText.alignment = TextAnchor.MiddleLeft;
            inventoryCountText.text = "样本 0/0";

            // ScrollRect + Grid
            GameObject viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(listObj.transform, false);
            var viewportRect = viewportObj.AddComponent<RectTransform>();
            viewportRect.anchorMin = new Vector2(0f, 0f);
            viewportRect.anchorMax = new Vector2(1f, 1f);
            viewportRect.offsetMin = new Vector2(6f, 6f);
            viewportRect.offsetMax = new Vector2(-6f, -40f);
            var viewportImg = viewportObj.AddComponent<Image>();
            viewportImg.color = new Color(0f, 0f, 0f, 0.2f);
            var mask = viewportObj.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewportObj.transform, false);
            var contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            var grid = contentObj.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(140f, 120f);
            grid.spacing = new Vector2(8f, 8f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            var contentFitter = contentObj.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = listObj.AddComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 24f;

            sampleListContainer = contentRect;

            // 右侧预览区域
            GameObject viewObj = new GameObject("PreviewArea");
            viewObj.transform.SetParent(panelObj.transform, false);
            var viewRect = viewObj.AddComponent<RectTransform>();
            viewRect.anchorMin = new Vector2(0.35f, 0.08f);
            viewRect.anchorMax = new Vector2(0.98f, 0.92f);
            viewRect.offsetMin = new Vector2(0f, 0f);
            viewRect.offsetMax = new Vector2(0f, 0f);
            var viewBg = viewObj.AddComponent<Image>();
            viewBg.color = new Color(0.14f, 0.14f, 0.14f, 0.92f);
            dropArea = viewRect;
            var dropHandler = viewObj.AddComponent<MicroscopeDropTarget>();
            dropHandler.Init(this);

            GameObject rawObj = new GameObject("PreviewImage");
            rawObj.transform.SetParent(viewObj.transform, false);
            var rawRect = rawObj.AddComponent<RectTransform>();
            rawRect.anchorMin = new Vector2(0f, 0.25f);
            rawRect.anchorMax = new Vector2(1f, 1f);
            rawRect.offsetMin = new Vector2(16f, 16f);
            rawRect.offsetMax = new Vector2(-16f, -16f);
            previewImage = rawObj.AddComponent<RawImage>();
            previewImage.texture = renderTexture;
            previewImage.color = Color.white;

            GameObject compositionObj = new GameObject("CompositionText");
            compositionObj.transform.SetParent(viewObj.transform, false);
            var compRect = compositionObj.AddComponent<RectTransform>();
            compRect.anchorMin = new Vector2(0f, 0f);
            compRect.anchorMax = new Vector2(1f, 0.26f);
            compRect.offsetMin = new Vector2(16f, 12f);
            compRect.offsetMax = new Vector2(-16f, -12f);
            compositionText = compositionObj.AddComponent<Text>();
            compositionText.text = "拖拽样本到此区域以观察组成";
            compositionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            compositionText.fontSize = 20;
            compositionText.color = Color.white;
            compositionText.alignment = TextAnchor.UpperLeft;

            GameObject detailObj = new GameObject("DetailText");
            detailObj.transform.SetParent(maskObj.transform, false);
            var detailRect = detailObj.AddComponent<RectTransform>();
            detailRect.anchorMin = new Vector2(0f, 0f);
            detailRect.anchorMax = new Vector2(0.4f, 0.08f);
            detailRect.offsetMin = new Vector2(20f, 12f);
            detailRect.offsetMax = new Vector2(-20f, -12f);
            detailText = detailObj.AddComponent<Text>();
            detailText.text = "选择或拖拽样本以查看详情";
            detailText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            detailText.fontSize = 18;
            detailText.color = Color.white;
            detailText.alignment = TextAnchor.MiddleLeft;

            // 关闭按钮
            GameObject closeObj = new GameObject("CloseButton");
            closeObj.transform.SetParent(maskObj.transform, false);
            var closeRect = closeObj.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.sizeDelta = new Vector2(36f, 36f);
            closeRect.anchoredPosition = new Vector2(-16f, -16f);
            var closeImg = closeObj.AddComponent<Image>();
            closeImg.color = new Color(0.9f, 0.25f, 0.25f, 0.9f);
            var closeBtn = closeObj.AddComponent<Button>();
            closeBtn.onClick.AddListener(Close);

            GameObject closeTxtObj = new GameObject("X");
            closeTxtObj.transform.SetParent(closeObj.transform, false);
            var closeTxt = closeTxtObj.AddComponent<Text>();
            closeTxt.text = "X";
            closeTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            closeTxt.fontSize = 18;
            closeTxt.color = Color.white;
            closeTxt.alignment = TextAnchor.MiddleCenter;
            var closeTxtRect = closeTxt.GetComponent<RectTransform>();
            closeTxtRect.anchorMin = Vector2.zero;
            closeTxtRect.anchorMax = Vector2.one;
            closeTxtRect.offsetMin = Vector2.zero;
            closeTxtRect.offsetMax = Vector2.zero;

            uiRoot = maskObj;
        }

        void CacheRenderers()
        {
            highlightRenderers = GetComponentsInChildren<Renderer>(true);
        }

        void EnsureLayerDatabase()
        {
            if (layerDatabaseMapper != null) return;
            layerDatabaseMapper = FindObjectOfType<LayerDatabaseMapper>();
            if (layerDatabaseMapper == null)
            {
                GameObject mapperObj = new GameObject("LayerDatabaseMapper (Microscope)");
                layerDatabaseMapper = mapperObj.AddComponent<LayerDatabaseMapper>();
                // 默认不在 Start 自动加载，手动加载一次
                layerDatabaseMapper.loadOnStart = false;
                layerDatabaseMapper.LoadDatabase();
                DontDestroyOnLoad(mapperObj);
            }
            else if (!layerDatabaseMapper.isActiveAndEnabled)
            {
                layerDatabaseMapper.enabled = true;
            }
        }

        void BuildSampleList()
        {
            if (sampleListContainer == null) return;
            foreach (var btn in sampleButtons)
            {
                if (btn != null) Destroy(btn.gameObject);
            }
            sampleButtons.Clear();

            var samples = SampleInventory.Instance != null
                ? SampleInventory.Instance.GetInventorySamples()
                : new List<SampleItem>();

            if (inventoryCountText != null)
            {
                int max = SampleInventory.Instance != null ? SampleInventory.Instance.maxSampleCapacity : 0;
                inventoryCountText.text = $"样本 {samples.Count}/{max}";
            }

            if (samples.Count == 0)
            {
                AddTextPlaceholder("背包中没有可供观察的样本");
                return;
            }

            foreach (var sample in samples)
            {
                var buttonObj = CreateSampleButton(sample);
                var btn = buttonObj.GetComponent<Button>();
                btn.onClick.AddListener(() => SelectSample(sample, btn));

                // 拖拽支持
                var trigger = buttonObj.AddComponent<EventTrigger>();
                var entryBegin = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
                entryBegin.callback.AddListener(_ => draggingSample = sample);
                var entryEnd = new EventTrigger.Entry { eventID = EventTriggerType.EndDrag };
                entryEnd.callback.AddListener(_ => draggingSample = null);
                trigger.triggers.Add(entryBegin);
                trigger.triggers.Add(entryEnd);

                sampleButtons.Add(btn);
            }
        }

        GameObject CreateSampleButton(SampleItem sample)
        {
            string label = string.IsNullOrEmpty(sample.displayName) ? sample.sampleID : sample.displayName;

            if (sampleButtonPrefab != null)
            {
                var instance = Instantiate(sampleButtonPrefab, sampleListContainer);
                var txt = instance.GetComponentInChildren<Text>();
                if (txt != null) txt.text = label;
                return instance;
            }

            GameObject obj = new GameObject($"SampleButton_{label}");
            obj.transform.SetParent(sampleListContainer, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(240f, 48f);
            var img = obj.AddComponent<Image>();
            img.color = buttonNormalColor;
            var btn = obj.AddComponent<Button>();

            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(obj.transform, false);
            var iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.15f);
            iconRect.anchorMax = new Vector2(0.35f, 0.85f);
            iconRect.offsetMin = new Vector2(8f, 0f);
            iconRect.offsetMax = new Vector2(-8f, 0f);
            var iconImg = iconObj.AddComponent<Image>();
            iconImg.color = Color.white;
            // 填充图标
            iconImg.sprite = GetSampleIcon(sample);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(obj.transform, false);
            var text = textObj.AddComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 18;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            var tRect = text.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0.4f, 0f);
            tRect.anchorMax = new Vector2(1f, 1f);
            tRect.offsetMin = new Vector2(8f, 4f);
            tRect.offsetMax = new Vector2(-8f, -4f);

            // 保存图标引用在 Button 的 Image (targetGraphic)，用于点击状态
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.ColorTint;
            var colors = btn.colors;
            colors.normalColor = buttonNormalColor;
            colors.highlightedColor = buttonSelectedColor * 1.05f;
            colors.pressedColor = buttonSelectedColor;
            colors.selectedColor = buttonSelectedColor;
            colors.colorMultiplier = 1f;
            btn.colors = colors;

            return obj;
        }

        void AddTextPlaceholder(string message)
        {
            GameObject obj = new GameObject("EmptyMessage");
            obj.transform.SetParent(sampleListContainer, false);
            var text = obj.AddComponent<Text>();
            text.text = message;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            var rect = text.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(240f, 60f);
        }

        Sprite GetSampleIcon(SampleItem sample)
        {
            if (sample != null && sample.previewIcon != null) return sample.previewIcon;
            // 尝试从 Resources 获取通用图标
            var fallback = Resources.Load<Sprite>("SampleIcons/DefaultSampleIcon");
            return fallback;
        }

        void SelectSample(SampleItem sample, Button clickedBtn)
        {
            currentSample = sample;
            UpdateDetail(sample);
            TintButtons(clickedBtn);
            UpdatePreviewAppearance(sample);
            UpdateCompositionPlaceholder(sample);
        }

        void AutoSelectFirstSample()
        {
            if (sampleButtons.Count > 0 && SampleInventory.Instance != null)
            {
                var samples = SampleInventory.Instance.GetInventorySamples();
                if (samples.Count > 0)
                {
                    SelectSample(samples[0], sampleButtons[0]);
                }
            }
        }

        void TintButtons(Button active)
        {
            foreach (var btn in sampleButtons)
            {
                if (btn == null) continue;
                var img = btn.GetComponent<Image>();
                if (img == null) continue;
                img.color = btn == active ? buttonSelectedColor : buttonNormalColor;
            }
        }

        void UpdateDetail(SampleItem sample)
        {
            if (detailText == null || sample == null) return;

            string info =
                $"名称: {sample.displayName}\n" +
                $"ID: {sample.sampleID}\n" +
                $"采集时间: {sample.collectionTime:yyyy-MM-dd HH:mm}\n" +
                $"来源工具: {sample.sourceToolID}\n" +
                $"深度范围: {sample.depthStart:F2} - {sample.depthEnd:F2} m\n" +
                $"描述: {sample.description}";

            detailText.text = info;
        }

        public void HandleDrop(SampleItem sample)
        {
            if (sample == null)
            {
                compositionText.text = "拖拽样本到此区域以观察组成";
                return;
            }

            SelectSample(sample, null);
        }

        void UpdatePreviewAppearance(SampleItem sample)
        {
            if (sample == null) return;
            BuildCompositionPreview(sample);
        }

        void UpdateCompositionPlaceholder(SampleItem sample)
        {
            if (compositionText == null || sample == null) return;

            var entries = ResolveComposition(sample);
            if (entries.Count == 0)
            {
                string layerInfo = Localize("无矿物数据，地层:", "No mineral data. Layers:") + "\n";
                if (sample.geologicalLayers != null && sample.geologicalLayers.Count > 0)
                {
                    foreach (var layer in sample.geologicalLayers)
                    {
                        layerInfo += $"- {layer.layerName} (厚度 {layer.thickness:F2}m)\n";
                    }
                }
                compositionText.text = layerInfo;
                return;
            }

            entries = entries.OrderByDescending(e => e.Percentage).ToList();
            string compositionInfo = Localize("矿物组成：", "Mineral composition:") + "\n";
            foreach (var entry in entries)
            {
                string name = entry.Name;
                if (string.IsNullOrEmpty(name))
                {
                    name = Localize("混合矿物", "Mixed minerals");
                }
                compositionInfo += $"- {name} ({entry.Percentage:P0})\n";
            }

            compositionText.text = compositionInfo;
        }

        void HandlePreviewInput()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            // 旋转
            if (mouse.leftButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                previewRoot.Rotate(Vector3.up, -delta.x * rotateSpeed * Time.deltaTime, Space.World);
                previewRoot.Rotate(Vector3.right, delta.y * rotateSpeed * Time.deltaTime, Space.World);
            }

            // 缩放
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                float scale = Mathf.Clamp(previewRoot.localScale.x + scroll * 0.01f * zoomSpeed, minZoom, maxZoom);
                previewRoot.localScale = Vector3.one * scale;
            }
        }

        void ShowUI()
        {
            if (uiRoot != null) uiRoot.SetActive(true);
        }

        void HideUI()
        {
            if (uiRoot != null) uiRoot.SetActive(false);
        }

        void EnableCamera(bool enable)
        {
            if (microscopeCamera == null) return;
            microscopeCamera.enabled = enable;
            if (microscopeCamera.gameObject.activeSelf != enable)
            {
                microscopeCamera.gameObject.SetActive(enable);
            }
        }

        public void SetHighlight(bool enable)
        {
            if (highlightRenderers == null || highlightRenderers.Length == 0) CacheRenderers();
            if (isHighlighted == enable) return;
            isHighlighted = enable;

            foreach (var r in highlightRenderers)
            {
                if (r == null) continue;
                var mats = r.materials;
                if (!originalEmissionColors.ContainsKey(r))
                {
                    var colorCache = new Color[mats.Length];
                    var enabledCache = new bool[mats.Length];
                    for (int i = 0; i < mats.Length; i++)
                    {
                        if (mats[i] != null)
                        {
                            colorCache[i] = mats[i].GetColor("_EmissionColor");
                            enabledCache[i] = mats[i].IsKeywordEnabled("_EMISSION");
                        }
                    }
                    originalEmissionColors[r] = colorCache;
                    originalEmissionEnabled[r] = enabledCache;
                }

                if (enable)
                {
                    for (int i = 0; i < mats.Length; i++)
                    {
                        if (mats[i] == null) continue;
                        mats[i].EnableKeyword("_EMISSION");
                        mats[i].SetColor("_EmissionColor", highlightColor * highlightEmission);
                    }
                }
                else
                {
                    // 还原原始发光
                    if (originalEmissionColors.TryGetValue(r, out var cache) && originalEmissionEnabled.TryGetValue(r, out var enabled))
                    {
                        for (int i = 0; i < mats.Length && i < cache.Length; i++)
                        {
                            if (mats[i] == null) continue;
                            mats[i].SetColor("_EmissionColor", cache[i]);
                            if (!enabled[i])
                            {
                                mats[i].DisableKeyword("_EMISSION");
                            }
                        }
                    }
                }
            }
        }

        Color SampleColorFromID(string id)
        {
            int hash = id != null ? id.GetHashCode() : 0;
            float r = ((hash >> 16) & 0xFF) / 255f;
            float g = ((hash >> 8) & 0xFF) / 255f;
            float b = (hash & 0xFF) / 255f;
            return new Color(0.25f + r * 0.5f, 0.25f + g * 0.5f, 0.25f + b * 0.5f, 1f);
        }

        // --- 组成与预览生成 ---
        class MineralEntry
        {
            public string Id;
            public string Name;
            public float Percentage;
            public string ModelFile;
            public Color Color;
            public MineralComposition Composition;
        }

        string Localize(string zh, string en)
        {
            switch (Application.systemLanguage)
            {
                case SystemLanguage.Chinese:
                case SystemLanguage.ChineseSimplified:
                case SystemLanguage.ChineseTraditional:
                    return zh;
                case SystemLanguage.Japanese:
                    // 简单日文占位
                    return en;
                default:
                    return en;
            }
        }

        List<MineralEntry> ResolveComposition(SampleItem sample)
        {
            var result = new List<MineralEntry>();
            if (sample == null) return result;

            // 按地层厚度加权的矿物占比
            if (sample.geologicalLayers == null || sample.geologicalLayers.Count == 0)
            {
                TryInferLayerFromMaterial(sample);
                if (sample.geologicalLayers == null || sample.geologicalLayers.Count == 0)
                {
                    Debug.LogWarning($"[Microscope] 样本 {sample.sampleID} 缺少地质层数据，无法解析成分");
                    return result;
                }
            }

            EnsureLayerDatabase();

            float totalThickness = Mathf.Max(0.0001f, sample.geologicalLayers.Sum(l => l.thickness));
            var weightMap = new Dictionary<string, MineralEntry>();

            foreach (var layer in sample.geologicalLayers)
            {
                float layerWeight = layer.thickness / totalThickness;

                MineralComposition[] minerals = layerDatabaseMapper != null
                    ? layerDatabaseMapper.GetMineralsForLayer(layer.layerName)
                    : new MineralComposition[0];

                if (minerals == null || minerals.Length == 0)
                {
                    // 用层名占位
                    string id = layer.layerName;
                    if (!weightMap.TryGetValue(id, out var entry))
                    {
                        entry = new MineralEntry
                        {
                            Id = id,
                            Name = layer.layerName,
                            Percentage = 0f,
                            ModelFile = string.Empty,
                            Color = SampleColorFromID(id)
                        };
                        weightMap[id] = entry;
                    }
                    entry.Percentage += layerWeight;
                    continue;
                }

                foreach (var mineral in minerals)
                {
                    if (mineral == null) continue;
                    float weight = layerWeight * Mathf.Clamp01(mineral.percentage);
                    string key = !string.IsNullOrEmpty(mineral.mineralId) ? mineral.mineralId : mineral.mineralName;
                    if (string.IsNullOrEmpty(key)) key = mineral.GetDisplayName();

                    if (!weightMap.TryGetValue(key, out var entry))
                    {
                        entry = new MineralEntry
                        {
                            Id = key,
                            Name = mineral.GetDisplayName(),
                            Percentage = 0f,
                            ModelFile = mineral.modelFile,
                            Color = SampleColorFromID(key),
                            Composition = mineral
                        };
                        weightMap[key] = entry;
                    }
                    entry.Percentage += weight;
                }
            }

            // 如果没找到任何矿物数据，用地层作为占位（避免空预览）
            if (weightMap.Count == 0)
            {
                foreach (var layer in sample.geologicalLayers)
                {
                    float layerWeight = layer.thickness / totalThickness;
                    string key = layer.layerName;
                    weightMap[key] = new MineralEntry
                    {
                        Id = key,
                        Name = layer.layerName,
                        Percentage = Mathf.Clamp01(layerWeight),
                        ModelFile = string.Empty,
                        Color = SampleColorFromID(key),
                        Composition = null
                    };
                }
            }

            result = weightMap.Values.ToList();

            // 归一化百分比
            float total = result.Sum(e => e.Percentage);
            if (total > 0.0001f)
            {
                foreach (var e in result)
                {
                    e.Percentage = Mathf.Clamp01(e.Percentage / total);
                }
            }

            return result;
        }

        void ClearPreviewInstances()
        {
            foreach (var go in spawnedMineralVisuals)
            {
                if (go != null) Destroy(go);
            }
            spawnedMineralVisuals.Clear();
        }

        void BuildCompositionPreview(SampleItem sample)
        {
            if (previewRoot == null) return;
            ClearPreviewInstances();

            var entries = ResolveComposition(sample);
            if (entries.Count == 0)
            {
                // fallback: 单个球体
                if (previewSample != null)
                {
                    previewSample.SetActive(true);
                    var r = previewSample.GetComponent<MeshRenderer>();
                    if (r != null)
                    {
                        if (r.material == null) r.material = new Material(Shader.Find("Standard"));
                        r.material.color = SampleColorFromID(sample.sampleID);
                    }
                }
                return;
            }

            if (previewSample != null) previewSample.SetActive(false);

            // 稳定随机种子确保同一样本结果一致
            int seed = sample.sampleID != null ? sample.sampleID.GetHashCode() : 0;
            var rng = new System.Random(seed);

            int remainingSlots = maxPreviewInstances;
            foreach (var entry in entries.OrderByDescending(e => e.Percentage))
            {
                int count = Mathf.Max(1, Mathf.RoundToInt(entry.Percentage * maxPreviewInstances));
                count = Mathf.Min(count, remainingSlots);
                remainingSlots -= count;
                for (int i = 0; i < count; i++)
                {
                    SpawnMineralVisual(entry, rng);
                }
                if (remainingSlots <= 0) break;
            }
        }

        void SpawnMineralVisual(MineralEntry entry, System.Random rng)
        {
            GameObject go = null;

            // 尝试加载模型
            go = LoadMineralModel(entry);

            if (go == null)
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                var col = go.GetComponent<Collider>();
                if (col != null) Destroy(col);
            }

            go.layer = previewLayer;
            go.transform.SetParent(previewRoot, false);

            // 随机散布
            Vector3 pos = new Vector3(
                (float)(rng.NextDouble() * 2 - 1),
                (float)(rng.NextDouble() * 2 - 1),
                (float)(rng.NextDouble() * 2 - 1)
            );
            pos = pos.normalized * (float)rng.NextDouble() * previewScatterRadius;
            go.transform.localPosition = pos;

            float baseScale = Mathf.Lerp(0.12f, 0.25f, entry.Percentage);
            go.transform.localScale = Vector3.one * baseScale;

            var renderer = go.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                foreach (var mat in renderer.materials)
                {
                    if (mat == null) continue;
                    mat.color = entry.Color;
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", entry.Color * 0.4f);
                }
            }

            spawnedMineralVisuals.Add(go);
        }

        GameObject LoadMineralModel(MineralEntry entry)
        {
            if (entry == null) return null;

            // 尝试使用缓存模型
            if (entry.Composition != null && entry.Composition.cachedModel != null)
            {
                return Instantiate(entry.Composition.cachedModel, previewRoot);
            }

            string file = entry.ModelFile ?? string.Empty;
            if (!string.IsNullOrEmpty(file))
            {
                file = System.IO.Path.GetFileNameWithoutExtension(file);
            }

            string[] roots =
            {
                "MineralData/Models/Minerals",
                "MineralData/Models/Minerals1",
                "MineralData/Models/Rocks",
                "MineralData/Models/Fossil",
                "MineralData/Models/Fossil1",
                "MineralData/Models/Layers"
            };

            foreach (var root in roots)
            {
                if (string.IsNullOrEmpty(file)) continue;
                var model = Resources.Load<GameObject>($"{root}/{file}");
                if (model != null)
                {
                    return Instantiate(model, previewRoot);
                }
            }

            // 如果 Composition 有自带的加载逻辑，尝试一次
            if (entry.Composition != null)
            {
                var model = entry.Composition.LoadMineralModel();
                if (model != null)
                {
                    return Instantiate(model, previewRoot);
                }
            }

            Debug.LogWarning($"[Microscope] 未找到矿物模型: id={entry.Id}, name={entry.Name}, modelFile={entry.ModelFile}");
            return null;
        }

        void TryInferLayerFromMaterial(SampleItem sample)
        {
            if (sample == null) return;

            List<string> inferredLayers = new List<string>();
            List<string> candidates = new List<string>();

            // materialData
            if (sample.materialData != null)
            {
                foreach (var m in sample.materialData)
                {
                    if (m.materialName != null && m.materialName.Length > 0)
                    {
                        candidates.Add(m.materialName);
                    }
                }
            }

            // display/description 作为弱匹配
            if (!string.IsNullOrEmpty(sample.displayName)) candidates.Add(sample.displayName);
            if (!string.IsNullOrEmpty(sample.description)) candidates.Add(sample.description);

            foreach (var c in candidates)
            {
                string lowered = c.ToLowerInvariant().Replace(" ", "").Replace("_", "").Replace("-", "");
                foreach (var kv in MaterialToLayerMap)
                {
                    if (lowered.Contains(kv.Key))
                    {
                        if (!inferredLayers.Contains(kv.Value))
                        {
                            inferredLayers.Add(kv.Value);
                        }
                    }
                }
            }

            if (inferredLayers.Count > 0)
            {
                float thickness = sample.totalDepth > 0.01f ? sample.totalDepth : 1f;
                float perLayerThickness = thickness / inferredLayers.Count;
                var layers = new List<SampleItem.LayerInfo>();
                foreach (var inferred in inferredLayers)
                {
                    var layerInfo = new SampleItem.LayerInfo
                    {
                        layerName = inferred,
                        thickness = perLayerThickness,
                        depthStart = 0f,
                        depthEnd = perLayerThickness,
                        layerColor = SampleColorFromID(inferred),
                        materialName = inferred,
                        layerDescription = $"推断层: {inferred}"
                    };
                    layers.Add(layerInfo);
                    Debug.Log($"[Microscope] 通过材质/名称推断地层: {inferred} (样本 {sample.sampleID})");
                }

                sample.geologicalLayers = layers;
                sample.layerCount = layers.Count;
            }
            else
            {
                // 若仍然没有，生成一个占位层，避免空预览
                float thickness = sample.totalDepth > 0.01f ? sample.totalDepth : 1f;
                string fallbackName = "未识别地层";
                var layerInfo = new SampleItem.LayerInfo
                {
                    layerName = fallbackName,
                    thickness = thickness,
                    depthStart = 0f,
                    depthEnd = thickness,
                    layerColor = SampleColorFromID(sample.sampleID),
                    materialName = fallbackName,
                    layerDescription = "未能从材质/名称推断地层"
                };
                sample.geologicalLayers = new List<SampleItem.LayerInfo> { layerInfo };
                sample.layerCount = 1;
                Debug.LogWarning($"[Microscope] 未匹配到地层，使用占位层 (样本 {sample.sampleID})");
            }
        }
    }
}

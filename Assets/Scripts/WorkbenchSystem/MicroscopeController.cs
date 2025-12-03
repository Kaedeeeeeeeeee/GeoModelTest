using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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

        void Start()
        {
            EnsureCollider();
            EnsureCameraAndPreview();
            EnsureUI();
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

            // 背景面板
            GameObject panelObj = new GameObject("MicroscopePanel");
            panelObj.transform.SetParent(canvasObj.transform, false);
            var panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(860f, 520f);
            panelRect.anchoredPosition = Vector2.zero;
            var bg = panelObj.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.08f, 0.92f);

            // 样本列表区域
            GameObject listObj = new GameObject("SampleList");
            listObj.transform.SetParent(panelObj.transform, false);
            var listRect = listObj.AddComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0f, 0f);
            listRect.anchorMax = new Vector2(0.33f, 1f);
            listRect.offsetMin = new Vector2(15f, 20f);
            listRect.offsetMax = new Vector2(0f, -20f);
            var layout = listObj.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(6, 6, 6, 6);
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperLeft;
            var fitter = listObj.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 预览区域
            GameObject viewObj = new GameObject("PreviewArea");
            viewObj.transform.SetParent(panelObj.transform, false);
            var viewRect = viewObj.AddComponent<RectTransform>();
            viewRect.anchorMin = new Vector2(0.35f, 0.2f);
            viewRect.anchorMax = new Vector2(0.97f, 0.95f);
            viewRect.offsetMin = new Vector2(0f, 0f);
            viewRect.offsetMax = new Vector2(0f, 0f);
            var viewBg = viewObj.AddComponent<Image>();
            viewBg.color = new Color(0.12f, 0.12f, 0.12f, 0.85f);

            GameObject rawObj = new GameObject("PreviewImage");
            rawObj.transform.SetParent(viewObj.transform, false);
            var rawRect = rawObj.AddComponent<RectTransform>();
            rawRect.anchorMin = new Vector2(0f, 0.2f);
            rawRect.anchorMax = new Vector2(1f, 1f);
            rawRect.offsetMin = new Vector2(12f, 12f);
            rawRect.offsetMax = new Vector2(-12f, -12f);
            previewImage = rawObj.AddComponent<RawImage>();
            previewImage.texture = renderTexture;
            previewImage.color = Color.white;

            GameObject detailObj = new GameObject("DetailText");
            detailObj.transform.SetParent(viewObj.transform, false);
            var detailRect = detailObj.AddComponent<RectTransform>();
            detailRect.anchorMin = new Vector2(0f, 0f);
            detailRect.anchorMax = new Vector2(1f, 0.22f);
            detailRect.offsetMin = new Vector2(12f, 12f);
            detailRect.offsetMax = new Vector2(-12f, -12f);
            detailText = detailObj.AddComponent<Text>();
            detailText.text = "选择一个样本以查看详情";
            detailText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            detailText.fontSize = 20;
            detailText.color = Color.white;
            detailText.alignment = TextAnchor.UpperLeft;

            // 关闭按钮
            GameObject closeObj = new GameObject("CloseButton");
            closeObj.transform.SetParent(panelObj.transform, false);
            var closeRect = closeObj.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.sizeDelta = new Vector2(32f, 32f);
            closeRect.anchoredPosition = new Vector2(-10f, -10f);
            var closeImg = closeObj.AddComponent<Image>();
            closeImg.color = new Color(0.9f, 0.25f, 0.25f, 0.9f);
            var closeBtn = closeObj.AddComponent<Button>();
            closeBtn.onClick.AddListener(Close);

            // 关闭按钮文字
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

            sampleListContainer = listRect;
            uiRoot = panelObj;
        }

        void CacheRenderers()
        {
            highlightRenderers = GetComponentsInChildren<Renderer>(true);
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

            if (samples.Count == 0)
            {
                AddTextPlaceholder("背包中没有可供观察的样本");
                return;
            }

            foreach (var sample in samples)
            {
                var buttonObj = CreateSampleButton(sample.displayName);
                var btn = buttonObj.GetComponent<Button>();
                btn.onClick.AddListener(() => SelectSample(sample, btn));
                sampleButtons.Add(btn);
            }
        }

        GameObject CreateSampleButton(string label)
        {
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

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(obj.transform, false);
            var text = textObj.AddComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 18;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            var tRect = text.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = new Vector2(12f, 4f);
            tRect.offsetMax = new Vector2(-12f, -4f);

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
            rect.sizeDelta = new Vector2(240f, 40f);
        }

        void SelectSample(SampleItem sample, Button clickedBtn)
        {
            currentSample = sample;
            UpdateDetail(sample);
            TintButtons(clickedBtn);
            UpdatePreviewAppearance(sample);
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

        void UpdatePreviewAppearance(SampleItem sample)
        {
            if (previewSample == null || sample == null) return;

            // 根据样本ID生成稳定的颜色，模拟“显微镜下的样本”
            Color color = SampleColorFromID(sample.sampleID);
            var renderer = previewSample.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                if (renderer.material == null)
                {
                    renderer.material = new Material(Shader.Find("Standard"));
                }
                renderer.material.color = color;
                renderer.material.SetFloat("_Glossiness", 0.05f);
            }
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
                float scale = Mathf.Clamp(previewSample.transform.localScale.x + scroll * 0.01f * zoomSpeed, minZoom, maxZoom);
                previewSample.transform.localScale = Vector3.one * scale;
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
    }
}

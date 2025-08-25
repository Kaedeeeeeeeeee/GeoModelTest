using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace SampleCuttingSystem
{
    /// <summary>
    /// 切割台UI管理器
    /// 负责2D剖面图显示和用户界面管理
    /// </summary>
    public class CuttingStationUI : MonoBehaviour
    {
        [Header("UI面板引用")]
        [SerializeField] private GameObject cuttingPanel;           // 切割主面板
        [SerializeField] private GameObject sampleInfoPanel;        // 样本信息面板
        [SerializeField] private GameObject instructionPanel;       // 操作说明面板
        
        [Header("样本显示区域")]
        [SerializeField] private RectTransform sampleDisplayArea;   // 样本显示区域
        [SerializeField] private RawImage sample3DPreview;          // 3D样本预览
        [SerializeField] private RectTransform layerDiagramArea;    // 地层图区域
        [SerializeField] private ScrollRect layerScrollView;        // 地层滚动视图
        
        [Header("切割控制区域")]
        [SerializeField] private RectTransform cuttingArea;         // 切割区域
        [SerializeField] private Image cuttingLine;                 // 移动切割线
        [SerializeField] private Image successZone;                 // 成功区域高亮
        [SerializeField] private RectTransform progressContainer;   // 进度容器
        
        [Header("信息显示")]
        [SerializeField] private Text sampleNameText;               // 样本名称
        [SerializeField] private Text layerCountText;               // 地层数量
        [SerializeField] private Text instructionText;             // 操作说明
        [SerializeField] private Text progressText;                // 进度文字
        [SerializeField] private Image spaceKeyIcon;               // 空格键图标
        
        [Header("按钮控制")]
        [SerializeField] private Button startCuttingButton;        // 开始切割按钮
        [SerializeField] private Button cancelButton;              // 取消按钮
        [SerializeField] private Button closeButton;               // 关闭按钮
        
        [Header("地层显示设置")]
        [SerializeField] private GameObject layerItemPrefab;       // 地层条目预制体
        [SerializeField] private float layerItemHeight = 60f;      // 地层条目高度
        [SerializeField] private float minLayerHeight = 20f;       // 最小地层高度
        [SerializeField] private Color boundaryLineColor = Color.red; // 边界线颜色
        
        [Header("3D预览设置")]
        [SerializeField] private Camera previewCamera;             // 预览相机
        [SerializeField] private RenderTexture previewRenderTexture; // 预览渲染纹理
        [SerializeField] private int previewTextureSize = 512;     // 预览纹理大小
        
        [Header("视觉效果")]
        [SerializeField] private float lineAnimationSpeed = 2f;    // 切割线动画速度
        [SerializeField] private Color successColor = Color.green; // 成功颜色
        [SerializeField] private Color failureColor = Color.red;   // 失败颜色
        [SerializeField] private Color defaultLineColor = Color.white; // 默认线条颜色
        
        // 内部状态
        private GeometricSampleReconstructor.ReconstructedSample currentSample;
        private SampleLayerAnalyzer.SampleInfo currentSampleInfo;
        private List<GameObject> layerItems = new List<GameObject>();
        private SampleCuttingGame cuttingGame;
        private bool isUIInitialized = false;
        
        // 3D预览控制
        private GameObject previewSampleObject;
        private float previewRotationSpeed = 30f;
        
        void Awake()
        {
            InitializeComponents();
        }
        
        void Start()
        {
            InitializeUI();
        }
        
        void Update()
        {
            UpdatePreviewRotation();
        }
        
        /// <summary>
        /// 初始化组件引用
        /// </summary>
        private void InitializeComponents()
        {
            // 获取切割游戏控制器
            cuttingGame = GetComponent<SampleCuttingGame>();
            if (cuttingGame == null)
                cuttingGame = gameObject.AddComponent<SampleCuttingGame>();
                
            // 设置按钮事件
            if (startCuttingButton != null)
                startCuttingButton.onClick.AddListener(OnStartCuttingClicked);
                
            if (cancelButton != null)
                cancelButton.onClick.AddListener(OnCancelClicked);
                
            if (closeButton != null)
                closeButton.onClick.AddListener(OnCloseClicked);
        }
        
        /// <summary>
        /// 初始化UI状态
        /// </summary>
        private void InitializeUI()
        {
            if (isUIInitialized) return;
            
            // 设置默认状态
            SetUIState(UIState.WaitingForSample);
            
            // 初始化预览相机
            InitializePreviewCamera();
            
            // 初始化切割线
            InitializeCuttingLine();
            
            isUIInitialized = true;
        }
        
        /// <summary>
        /// 初始化预览相机
        /// </summary>
        private void InitializePreviewCamera()
        {
            if (previewCamera == null)
            {
                // 创建预览相机
                GameObject cameraObj = new GameObject("SamplePreviewCamera");
                cameraObj.transform.SetParent(transform);
                previewCamera = cameraObj.AddComponent<Camera>();
                
                // 设置相机参数
                previewCamera.orthographic = true;
                previewCamera.orthographicSize = 1.5f;
                previewCamera.clearFlags = CameraClearFlags.SolidColor;
                previewCamera.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
                previewCamera.cullingMask = LayerMask.GetMask("SamplePreview");
                previewCamera.enabled = false; // 手动控制渲染
            }
            
            // 创建渲染纹理
            if (previewRenderTexture == null)
            {
                previewRenderTexture = new RenderTexture(previewTextureSize, previewTextureSize, 16);
                previewRenderTexture.Create();
            }
            
            previewCamera.targetTexture = previewRenderTexture;
            
            // 设置到UI
            if (sample3DPreview != null)
            {
                sample3DPreview.texture = previewRenderTexture;
            }
        }
        
        /// <summary>
        /// 初始化切割线
        /// </summary>
        private void InitializeCuttingLine()
        {
            if (cuttingLine != null)
            {
                cuttingLine.color = defaultLineColor;
                cuttingLine.gameObject.SetActive(false);
            }
            
            if (successZone != null)
            {
                successZone.gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// 设置UI状态
        /// </summary>
        public void SetUIState(UIState state)
        {
            switch (state)
            {
                case UIState.WaitingForSample:
                    ShowWaitingForSampleUI();
                    break;
                case UIState.SampleLoaded:
                    ShowSampleLoadedUI();
                    break;
                case UIState.Cutting:
                    ShowCuttingUI();
                    break;
                case UIState.Success:
                    ShowSuccessUI();
                    break;
                case UIState.Failed:
                    ShowFailedUI();
                    break;
                case UIState.Completed:
                    ShowCompletedUI();
                    break;
            }
        }
        
        /// <summary>
        /// 显示等待样本的UI
        /// </summary>
        private void ShowWaitingForSampleUI()
        {
            if (instructionText != null)
                instructionText.text = "将多层样本拖拽到切割台";
                
            if (startCuttingButton != null)
                startCuttingButton.gameObject.SetActive(false);
                
            if (sampleInfoPanel != null)
                sampleInfoPanel.SetActive(false);
                
            ClearLayerDiagram();
            ClearPreview();
        }
        
        /// <summary>
        /// 显示样本已加载的UI
        /// </summary>
        private void ShowSampleLoadedUI()
        {
            if (instructionText != null)
                instructionText.text = "样本分析完成，准备开始切割";
                
            if (startCuttingButton != null)
                startCuttingButton.gameObject.SetActive(true);
                
            if (sampleInfoPanel != null)
                sampleInfoPanel.SetActive(true);
        }
        
        /// <summary>
        /// 显示切割中的UI
        /// </summary>
        private void ShowCuttingUI()
        {
            if (instructionText != null)
                instructionText.text = "按空格键进行切割";
                
            if (startCuttingButton != null)
                startCuttingButton.gameObject.SetActive(false);
                
            if (spaceKeyIcon != null)
                spaceKeyIcon.gameObject.SetActive(true);
                
            if (cuttingLine != null)
                cuttingLine.gameObject.SetActive(true);
        }
        
        /// <summary>
        /// 显示成功的UI
        /// </summary>
        private void ShowSuccessUI()
        {
            if (instructionText != null)
                instructionText.text = "切割成功！";
                
            if (spaceKeyIcon != null)
                spaceKeyIcon.gameObject.SetActive(false);
        }
        
        /// <summary>
        /// 显示失败的UI
        /// </summary>
        private void ShowFailedUI()
        {
            if (instructionText != null)
                instructionText.text = "切割失败，样本损坏";
                
            if (spaceKeyIcon != null)
                spaceKeyIcon.gameObject.SetActive(false);
                
            if (cuttingLine != null)
                cuttingLine.color = failureColor;
        }
        
        /// <summary>
        /// 显示完成的UI
        /// </summary>
        private void ShowCompletedUI()
        {
            if (instructionText != null)
                instructionText.text = "所有切割完成！收集样本";
                
            if (cuttingLine != null)
                cuttingLine.gameObject.SetActive(false);
                
            if (successZone != null)
                successZone.gameObject.SetActive(false);
        }
        
        /// <summary>
        /// 加载样本到UI
        /// </summary>
        public void LoadSample(GeometricSampleReconstructor.ReconstructedSample sample)
        {
            currentSample = sample;
            
            // 获取样本分析信息
            var analyzer = GetComponent<SampleLayerAnalyzer>();
            if (analyzer != null)
            {
                currentSampleInfo = analyzer.GetSampleInfo(sample);
            }
            
            // 更新样本信息显示
            UpdateSampleInfoDisplay();
            
            // 生成地层图
            GenerateLayerDiagram();
            
            // 创建3D预览
            CreateSamplePreview();
            
            // 设置UI状态
            SetUIState(UIState.SampleLoaded);
        }
        
        /// <summary>
        /// 更新样本信息显示
        /// </summary>
        private void UpdateSampleInfoDisplay()
        {
            if (currentSampleInfo == null) return;
            
            if (sampleNameText != null)
            {
                string sampleName = currentSample?.sampleID ?? "未知样本";
                sampleNameText.text = $"样本: {sampleName}";
            }
            
            if (layerCountText != null)
            {
                layerCountText.text = $"地层数量: {currentSampleInfo.layerCount}";
            }
            
            if (progressText != null)
            {
                progressText.text = $"需要切割: {currentSampleInfo.estimatedCuts} 次";
            }
        }
        
        /// <summary>
        /// 生成地层图
        /// </summary>
        private void GenerateLayerDiagram()
        {
            ClearLayerDiagram();
            
            if (currentSampleInfo?.layers == null || layerDiagramArea == null)
                return;
                
            float totalHeight = layerDiagramArea.rect.height;
            float currentY = 0f;
            
            for (int i = 0; i < currentSampleInfo.layers.Length; i++)
            {
                var layerInfo = currentSampleInfo.layers[i];
                
                // 计算层高度（按比例）
                float layerHeight = (layerInfo.thickness / currentSampleInfo.totalHeight) * totalHeight;
                layerHeight = Mathf.Max(layerHeight, minLayerHeight);
                
                // 创建地层条目
                GameObject layerItem = CreateLayerItem(layerInfo, layerHeight, currentY);
                layerItems.Add(layerItem);
                
                currentY += layerHeight;
                
                // 添加边界线（除了最后一层）
                if (i < currentSampleInfo.layers.Length - 1)
                {
                    CreateBoundaryLine(currentY);
                }
            }
        }
        
        /// <summary>
        /// 创建地层条目
        /// </summary>
        private GameObject CreateLayerItem(SampleLayerAnalyzer.LayerInfo layerInfo, float height, float yPosition)
        {
            GameObject item;
            
            if (layerItemPrefab != null)
            {
                item = Instantiate(layerItemPrefab, layerDiagramArea);
            }
            else
            {
                // 创建默认地层条目
                item = new GameObject($"Layer_{layerInfo.name}");
                item.transform.SetParent(layerDiagramArea);
                
                // 添加Image组件显示地层颜色
                Image layerImage = item.AddComponent<Image>();
                layerImage.color = layerInfo.color;
                
                // 添加Text组件显示地层名称
                GameObject textObj = new GameObject("LayerName");
                textObj.transform.SetParent(item.transform);
                Text layerText = textObj.AddComponent<Text>();
                layerText.text = layerInfo.name;
                layerText.color = Color.white;
                layerText.fontSize = 14;
                layerText.alignment = TextAnchor.MiddleCenter;
                
                // 设置Text的RectTransform
                RectTransform textRect = textObj.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
            }
            
            // 设置位置和大小
            RectTransform itemRect = item.GetComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0, 1);
            itemRect.anchorMax = new Vector2(1, 1);
            itemRect.anchoredPosition = new Vector2(0, -yPosition);
            itemRect.sizeDelta = new Vector2(0, height);
            
            return item;
        }
        
        /// <summary>
        /// 创建边界线
        /// </summary>
        private void CreateBoundaryLine(float yPosition)
        {
            GameObject line = new GameObject("BoundaryLine");
            line.transform.SetParent(layerDiagramArea);
            
            Image lineImage = line.AddComponent<Image>();
            lineImage.color = boundaryLineColor;
            
            RectTransform lineRect = line.GetComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0, 1);
            lineRect.anchorMax = new Vector2(1, 1);
            lineRect.anchoredPosition = new Vector2(0, -yPosition);
            lineRect.sizeDelta = new Vector2(0, 2f); // 2像素厚的线
            
            layerItems.Add(line);
        }
        
        /// <summary>
        /// 清空地层图
        /// </summary>
        private void ClearLayerDiagram()
        {
            foreach (var item in layerItems)
            {
                if (item != null)
                    DestroyImmediate(item);
            }
            layerItems.Clear();
        }
        
        /// <summary>
        /// 创建样本3D预览
        /// </summary>
        private void CreateSamplePreview()
        {
            ClearPreview();
            
            if (currentSample?.sampleContainer == null || previewCamera == null)
                return;
                
            // 复制样本对象到预览层
            previewSampleObject = Instantiate(currentSample.sampleContainer);
            previewSampleObject.name = "PreviewSample";
            
            // 设置到预览层
            SetLayerRecursively(previewSampleObject, LayerMask.NameToLayer("SamplePreview"));
            
            // 调整位置和缩放
            previewSampleObject.transform.position = Vector3.zero;
            previewSampleObject.transform.localScale = Vector3.one * 0.8f;
            
            // 渲染预览
            previewCamera.Render();
        }
        
        /// <summary>
        /// 设置对象及其子对象的层
        /// </summary>
        private void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }
        
        /// <summary>
        /// 清空预览
        /// </summary>
        private void ClearPreview()
        {
            if (previewSampleObject != null)
            {
                DestroyImmediate(previewSampleObject);
                previewSampleObject = null;
            }
        }
        
        /// <summary>
        /// 更新预览旋转
        /// </summary>
        private void UpdatePreviewRotation()
        {
            if (previewSampleObject != null && previewCamera != null)
            {
                previewSampleObject.transform.Rotate(0, previewRotationSpeed * Time.deltaTime, 0);
                
                // 定期重新渲染
                if (Time.frameCount % 5 == 0) // 每5帧渲染一次
                {
                    previewCamera.Render();
                }
            }
        }
        
        /// <summary>
        /// 按钮事件：开始切割
        /// </summary>
        private void OnStartCuttingClicked()
        {
            if (currentSample != null && cuttingGame != null)
            {
                cuttingGame.StartCutting(currentSample);
                SetUIState(UIState.Cutting);
            }
        }
        
        /// <summary>
        /// 按钮事件：取消操作
        /// </summary>
        private void OnCancelClicked()
        {
            ResetUI();
        }
        
        /// <summary>
        /// 按钮事件：关闭界面
        /// </summary>
        private void OnCloseClicked()
        {
            if (cuttingPanel != null)
                cuttingPanel.SetActive(false);
        }
        
        /// <summary>
        /// 重置UI状态
        /// </summary>
        public void ResetUI()
        {
            currentSample = null;
            currentSampleInfo = null;
            
            ClearLayerDiagram();
            ClearPreview();
            
            SetUIState(UIState.WaitingForSample);
        }
        
        /// <summary>
        /// UI状态枚举
        /// </summary>
        public enum UIState
        {
            WaitingForSample,   // 等待样本
            SampleLoaded,       // 样本已加载
            Cutting,           // 切割中
            Success,           // 成功
            Failed,            // 失败
            Completed          // 完成
        }
        
        /// <summary>
        /// 清理资源
        /// </summary>
        void OnDestroy()
        {
            ClearPreview();
            
            if (previewRenderTexture != null)
            {
                previewRenderTexture.Release();
                DestroyImmediate(previewRenderTexture);
            }
        }
        
        /// <summary>
        /// Editor测试方法
        /// </summary>
        [ContextMenu("测试UI初始化")]
        private void TestUIInitialization()
        {
            InitializeUI();
            Debug.Log("UI初始化测试完成");
        }
    }
}
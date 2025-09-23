using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

namespace SampleCuttingSystem
{
    /// <summary>
    /// 样本切割小游戏核心控制器
    /// 实现经典的移动横条时机按键游戏机制
    /// </summary>
    public class SampleCuttingGame : MonoBehaviour
    {
        [Header("游戏设置")]
        [SerializeField] private float cuttingLineSpeed = 800f; // 切割线移动速度 (像素/秒) - 超高速！
        [SerializeField] private AudioClip laserHumSound; // 激光切割嗡嗡声
        [SerializeField] private AudioClip successSound; // 成功音效
        [SerializeField] private AudioClip failureSound; // 失败音效
        
        [Header("UI组件引用")]
        [SerializeField] private RectTransform cuttingLine; // 移动的切割线
        [SerializeField] private RectTransform cuttingArea; // 切割区域容器
        [SerializeField] private RectTransform sampleDiagram; // 样本柱状图容器
        [SerializeField] private Image successZone; // 成功区域高亮
        [SerializeField] private Text instructionText; // 操作提示文字
        [SerializeField] private Image spaceKeyIcon; // 空格键图标
        
        // 游戏状态
        public enum CuttingState
        {
            WaitingForSample,    // 等待样本放入
            Preparing,           // 准备阶段 
            Cutting,            // 切割进行中
            Success,            // 切割成功
            Failed,             // 切割失败
            Completed           // 全部切割完成
        }
        
        [Header("游戏状态")]
        [SerializeField] private CuttingState currentState = CuttingState.WaitingForSample;
        
        // 当前样本数据
        private GeometricSampleReconstructor.ReconstructedSample currentSample;
        private SampleLayerAnalyzer layerAnalyzer;
        private LayerDatabaseMapper databaseMapper;
        
        // 切割数据
        private LayerBoundary[] layerBoundaries;
        private int currentCuttingIndex = 0; // 当前切割的边界索引
        private float currentSuccessZoneStart;
        private float currentSuccessZoneEnd;
        private bool cuttingLineMovingDown = true; // 红线移动方向：true=向下，false=向上
        
        // 角色控制器引用（用于禁用跳跃）
        private FirstPersonController playerController;
        
        // 切割系统管理器引用（用于触发事件）
        private SampleCuttingSystemManager systemManager;
        
        // 音频控制
        private AudioSource audioSource;
        private Coroutine laserSoundCoroutine;
        
        // 工作台位置存储（用于样本生成定位）
        private Vector3? currentWorkstationPosition;
        
        [System.Serializable]
        public class LayerBoundary
        {
            public float position;        // 边界位置 (在UI坐标系中)
            public float successZoneSize; // 成功区域大小
            public string layerName;      // 地层名称
            public Color layerColor;      // 地层颜色
        }
        
        void Awake()
        {
            // 获取或创建必要组件
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
                
            layerAnalyzer = GetComponent<SampleLayerAnalyzer>();
            if (layerAnalyzer == null)
                layerAnalyzer = gameObject.AddComponent<SampleLayerAnalyzer>();
                
            databaseMapper = GetComponent<LayerDatabaseMapper>();
            if (databaseMapper == null)
                databaseMapper = gameObject.AddComponent<LayerDatabaseMapper>();
        }
        
        void Start()
        {
            // 找到玩家控制器
            playerController = FindFirstObjectByType<FirstPersonController>();
            if (playerController == null)
            {
                Debug.LogWarning("未找到FirstPersonController，无法控制跳跃功能");
            }
            
            // 找到切割系统管理器
            systemManager = FindFirstObjectByType<SampleCuttingSystemManager>();
            if (systemManager == null)
            {
                Debug.LogWarning("未找到SampleCuttingSystemManager，无法触发切割完成事件");
            }
            else
            {
                Debug.Log("[SampleCuttingGame] 成功找到SampleCuttingSystemManager");
            }
            
            // 只设置初始状态，不创建UI
            SetState(CuttingState.WaitingForSample);
        }
        
        /// <summary>
        /// 初始化UI组件（运行时创建）
        /// </summary>
        private void InitializeUIComponents()
        {
            // 检查是否已经有父容器（嵌入模式）
            if (transform.parent != null)
            {
                // 嵌入模式：直接在当前GameObject创建UI
                CreateEmbeddedCuttingArea();
            }
            else
            {
                // 独立模式：创建独立Canvas
                Canvas canvas = FindOrCreateCanvas();
                if (canvas == null)
                {
                    Debug.LogError("无法创建Canvas用于切割游戏UI");
                    return;
                }
                CreateCuttingArea(canvas);
            }
            
            // 创建切割线
            CreateCuttingLine();
            
            // 创建成功区域
            CreateSuccessZone();
            
            // 创建指令文本
            CreateInstructionText();
            
            // 创建空格键图标
            CreateSpaceKeyIcon();
            
            // 创建关闭按钮
            CreateCloseButton();
            
            Debug.Log("切割游戏UI组件初始化完成");
        }
        
        /// <summary>
        /// 查找或创建Canvas
        /// </summary>
        private Canvas FindOrCreateCanvas()
        {
            // 先查找是否已经有切割游戏专用的Canvas
            Canvas[] canvases = FindObjectsOfType<Canvas>();
            foreach (Canvas canvas in canvases)
            {
                if (canvas.name == "CuttingGameCanvas")
                {
                    return canvas;
                }
            }
            
            // 创建新的切割游戏专用Canvas
            GameObject canvasObj = new GameObject("CuttingGameCanvas");
            Canvas canvas_new = canvasObj.AddComponent<Canvas>();
            canvas_new.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas_new.sortingOrder = 1000; // 高优先级，在仓库UI之上
            
            // 添加CanvasScaler
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            // 添加GraphicRaycaster
            canvasObj.AddComponent<GraphicRaycaster>();
            
            DontDestroyOnLoad(canvasObj);
            return canvas_new;
        }
        
        /// <summary>
        /// 创建切割区域容器
        /// </summary>
        private void CreateCuttingArea(Canvas canvas)
        {
            if (cuttingArea != null) return;
            
            GameObject areaObj = new GameObject("CuttingArea");
            areaObj.transform.SetParent(canvas.transform, false);
            
            cuttingArea = areaObj.AddComponent<RectTransform>();
            
            // 设置为全屏居中区域
            cuttingArea.anchorMin = new Vector2(0.1f, 0.1f);
            cuttingArea.anchorMax = new Vector2(0.9f, 0.9f);
            cuttingArea.offsetMin = Vector2.zero;
            cuttingArea.offsetMax = Vector2.zero;
            
            // 添加Unity UI常见的黑色半透明背景
            Image bgImage = areaObj.AddComponent<Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.8f); // Unity UI标准黑色半透明
            
            // 创建边框装饰
            CreateUIBorder(areaObj);
            
            Debug.Log($"创建切割区域: 尺寸={cuttingArea.rect.size}");
        }
        
        /// <summary>
        /// 创建UI边框装饰
        /// </summary>
        private void CreateUIBorder(GameObject parent)
        {
            // 顶部边框
            GameObject topBorder = new GameObject("TopBorder");
            topBorder.transform.SetParent(parent.transform, false);
            RectTransform topRect = topBorder.AddComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0f, 1f);
            topRect.anchorMax = new Vector2(1f, 1f);
            topRect.sizeDelta = new Vector2(0, 3f);
            topRect.anchoredPosition = new Vector2(0, -1.5f);
            Image topImage = topBorder.AddComponent<Image>();
            topImage.color = new Color(0.2f, 0.8f, 1f, 0.8f); // 科技蓝色
            
            // 左侧边框
            GameObject leftBorder = new GameObject("LeftBorder");
            leftBorder.transform.SetParent(parent.transform, false);
            RectTransform leftRect = leftBorder.AddComponent<RectTransform>();
            leftRect.anchorMin = new Vector2(0f, 0f);
            leftRect.anchorMax = new Vector2(0f, 1f);
            leftRect.sizeDelta = new Vector2(3f, 0);
            leftRect.anchoredPosition = new Vector2(1.5f, 0);
            Image leftImage = leftBorder.AddComponent<Image>();
            leftImage.color = new Color(0.2f, 0.8f, 1f, 0.8f);
        }
        
        /// <summary>
        /// 创建嵌入式切割区域（在现有UI容器中）
        /// </summary>
        private void CreateEmbeddedCuttingArea()
        {
            // 直接使用当前GameObject作为切割区域容器
            cuttingArea = GetComponent<RectTransform>();
            if (cuttingArea == null)
            {
                cuttingArea = gameObject.AddComponent<RectTransform>();
            }
            
            // 清空背景（父容器已经有背景了）
            Image existingBg = GetComponent<Image>();
            if (existingBg == null)
            {
                // 添加透明背景以确保射线检测正常
                Image bgImage = gameObject.AddComponent<Image>();
                bgImage.color = new Color(0f, 0f, 0f, 0f); // 完全透明
            }
            
            Debug.Log("创建嵌入式切割区域");
        }
        
        /// <summary>
        /// 创建样本图表
        /// </summary>
        private void CreateSampleDiagram()
        {
            if (sampleDiagram != null || cuttingArea == null) return;
            
            GameObject diagramObj = new GameObject("SampleDiagram");
            diagramObj.transform.SetParent(cuttingArea.transform, false);
            
            sampleDiagram = diagramObj.AddComponent<RectTransform>();
            
            // 嵌入模式下的布局调整
            bool isEmbedded = transform.parent != null && cuttingArea == transform;
            
            if (isEmbedded)
            {
                // 嵌入模式：使用更紧凑的布局
                sampleDiagram.anchorMin = new Vector2(0.05f, 0.15f);
                sampleDiagram.anchorMax = new Vector2(0.4f, 0.85f);
            }
            else
            {
                // 独立模式：使用原始布局
                sampleDiagram.anchorMin = new Vector2(0.08f, 0.15f);
                sampleDiagram.anchorMax = new Vector2(0.48f, 0.85f);
            }
            sampleDiagram.offsetMin = Vector2.zero;
            sampleDiagram.offsetMax = Vector2.zero;
            
            // 添加半透明背景
            Image bgImage = diagramObj.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.7f); // 较浅的黑色半透明背景
            
            // 添加标题
            CreateSampleDiagramTitle(diagramObj);
            
            Debug.Log("创建样本图表容器");
        }
        
        /// <summary>
        /// 创建样本图表标题
        /// </summary>
        private void CreateSampleDiagramTitle(GameObject parent)
        {
            GameObject titleObj = new GameObject("DiagramTitle");
            titleObj.transform.SetParent(parent.transform, false);
            
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.9f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            
            Text titleText = titleObj.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 20;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = new Color(0.8f, 0.9f, 1f, 1f);
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.text = "样本剖面图";
        }
        
        /// <summary>
        /// 创建切割线
        /// </summary>
        private void CreateCuttingLine()
        {
            if (cuttingLine != null || cuttingArea == null) return;
            
            GameObject lineObj = new GameObject("CuttingLine");
            lineObj.transform.SetParent(cuttingArea.transform, false);
            
            cuttingLine = lineObj.AddComponent<RectTransform>();
            
            // 根据模式调整切割线位置
            bool isEmbedded = transform.parent != null && cuttingArea == transform;
            
            if (isEmbedded)
            {
                // 嵌入模式：横跨整个区域（无需预留样本图空间）
                cuttingLine.anchorMin = new Vector2(0.05f, 1f);
                cuttingLine.anchorMax = new Vector2(0.95f, 1f);
            }
            else
            {
                // 独立模式：使用原始布局
                cuttingLine.anchorMin = new Vector2(0.52f, 1f);
                cuttingLine.anchorMax = new Vector2(0.92f, 1f);
            }  
            cuttingLine.sizeDelta = new Vector2(0, 4f); // 稍微粗一点的线
            cuttingLine.anchoredPosition = new Vector2(0, 0);
            
            // 添加发光效果的切割线
            Image lineImage = lineObj.AddComponent<Image>();
            lineImage.color = new Color(1f, 0.2f, 0.2f, 0.9f); // 明亮的红色
            
            // 创建切割线发光效果
            CreateCuttingLineGlow(lineObj);
            
            Debug.Log("创建切割线");
        }
        
        /// <summary>
        /// 创建切割线发光效果
        /// </summary>
        private void CreateCuttingLineGlow(GameObject parent)
        {
            GameObject glowObj = new GameObject("LineGlow");
            glowObj.transform.SetParent(parent.transform, false);
            
            RectTransform glowRect = glowObj.AddComponent<RectTransform>();
            glowRect.anchorMin = new Vector2(0f, 0f);
            glowRect.anchorMax = new Vector2(1f, 1f);
            glowRect.sizeDelta = new Vector2(0, 8f); // 比主线稍微宽
            glowRect.anchoredPosition = Vector2.zero;
            
            Image glowImage = glowObj.AddComponent<Image>();
            glowImage.color = new Color(1f, 0.4f, 0.4f, 0.3f); // 半透明的红色发光
            
            // 将发光效果放在背景
            glowObj.transform.SetAsFirstSibling();
        }
        
        /// <summary>
        /// 创建成功区域
        /// </summary>
        private void CreateSuccessZone()
        {
            if (successZone != null || cuttingArea == null) return;
            
            GameObject zoneObj = new GameObject("SuccessZone");
            zoneObj.transform.SetParent(cuttingArea.transform, false);
            
            RectTransform zoneRect = zoneObj.AddComponent<RectTransform>();
            
            // 使用与切割线相同的顶部锚点系统
            zoneRect.anchorMin = new Vector2(0f, 1f); // 左上角锚点
            zoneRect.anchorMax = new Vector2(1f, 1f); // 右上角锚点
            zoneRect.pivot = new Vector2(0.5f, 0.5f); // 中心作为轴点
            
            // 设置初始尺寸和位置 (会在UpdateSuccessZone中更新)
            zoneRect.sizeDelta = new Vector2(0, 50f); // 宽度填满，高度50像素
            zoneRect.anchoredPosition = Vector2.zero; // 初始位置
            
            // 添加现代化的成功区域背景
            successZone = zoneObj.AddComponent<Image>();
            successZone.color = new Color(0.2f, 0.8f, 0.3f, 0.4f); // 更柔和的绿色
            
            // 创建成功区域边框指示器
            CreateSuccessZoneBorders(zoneObj);
            
            // 初始状态隐藏
            zoneObj.SetActive(false);
            
            Debug.Log("创建成功区域");
        }
        
        /// <summary>
        /// 创建成功区域边框指示器
        /// </summary>
        private void CreateSuccessZoneBorders(GameObject parent)
        {
            // 上边框
            GameObject topBorder = new GameObject("SuccessTopBorder");
            topBorder.transform.SetParent(parent.transform, false);
            RectTransform topRect = topBorder.AddComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0f, 1f);
            topRect.anchorMax = new Vector2(1f, 1f);
            topRect.sizeDelta = new Vector2(0, 2f);
            topRect.anchoredPosition = new Vector2(0, 1f);
            Image topImage = topBorder.AddComponent<Image>();
            topImage.color = new Color(0.2f, 1f, 0.4f, 0.8f); // 亮绿色边框
            
            // 下边框
            GameObject bottomBorder = new GameObject("SuccessBottomBorder");
            bottomBorder.transform.SetParent(parent.transform, false);
            RectTransform bottomRect = bottomBorder.AddComponent<RectTransform>();
            bottomRect.anchorMin = new Vector2(0f, 0f);
            bottomRect.anchorMax = new Vector2(1f, 0f);
            bottomRect.sizeDelta = new Vector2(0, 2f);
            bottomRect.anchoredPosition = new Vector2(0, -1f);
            Image bottomImage = bottomBorder.AddComponent<Image>();
            bottomImage.color = new Color(0.2f, 1f, 0.4f, 0.8f);
        }
        
        /// <summary>
        /// 创建指令文本
        /// </summary>
        private void CreateInstructionText()
        {
            if (instructionText != null || cuttingArea == null) return;
            
            // 创建指令面板容器
            GameObject panelObj = new GameObject("InstructionPanel");
            panelObj.transform.SetParent(cuttingArea.transform, false);
            
            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            // 移动到右半边UI的左上角，并缩小尺寸
            panelRect.anchorMin = new Vector2(0.51f, 0.88f);
            panelRect.anchorMax = new Vector2(0.75f, 0.98f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            
            // 添加面板背景
            Image panelBg = panelObj.AddComponent<Image>();
            panelBg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f); // 统一的黑色半透明背景
            
            // 创建文本对象
            GameObject textObj = new GameObject("InstructionText");
            textObj.transform.SetParent(panelObj.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 5f);
            textRect.offsetMax = new Vector2(-10f, -5f);
            
            // 添加文本组件
            instructionText = textObj.AddComponent<Text>();
            instructionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            instructionText.fontSize = 16;  // 从22减小到16，让文字更小
            instructionText.fontStyle = FontStyle.Bold;
            instructionText.color = new Color(0.9f, 0.9f, 1f, 1f);
            instructionText.alignment = TextAnchor.MiddleCenter;
            instructionText.text = "初始化切割系统...";
            
            Debug.Log("创建指令文本");
        }
        
        /// <summary>
        /// 创建空格键图标
        /// </summary>
        private void CreateSpaceKeyIcon()
        {
            if (spaceKeyIcon != null || cuttingArea == null) return;
            
            // 创建空格键提示面板
            GameObject panelObj = new GameObject("SpaceKeyPanel");
            panelObj.transform.SetParent(cuttingArea.transform, false);
            
            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.55f, 0.88f);
            panelRect.anchorMax = new Vector2(0.88f, 0.98f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            
            // 添加面板背景
            Image panelBg = panelObj.AddComponent<Image>();
            panelBg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f); // 统一的黑色半透明背景
            
            // 创建空格键图标
            GameObject iconObj = new GameObject("SpaceKeyIcon");
            iconObj.transform.SetParent(panelObj.transform, false);
            
            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.05f, 0.2f);
            iconRect.anchorMax = new Vector2(0.35f, 0.8f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            
            spaceKeyIcon = iconObj.AddComponent<Image>();
            spaceKeyIcon.color = new Color(0.8f, 0.9f, 1f, 1f);
            
            // 创建现代化的空格键图标
            CreateSpaceKeyTexture();
            
            // 添加空格键文本
            GameObject textObj = new GameObject("SpaceKeyText");
            textObj.transform.SetParent(panelObj.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.4f, 0f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            Text keyText = textObj.AddComponent<Text>();
            keyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            keyText.fontSize = 16;
            keyText.fontStyle = FontStyle.Bold;
            keyText.color = new Color(0.8f, 0.9f, 1f, 1f);
            keyText.alignment = TextAnchor.MiddleLeft;

            // 添加本地化组件
            var localizedKeyText = textObj.AddComponent<LocalizedText>();
            localizedKeyText.TextKey = "cutting_system.cutting_line.instruction";
            
            // 初始状态隐藏
            panelObj.SetActive(false);
            
            Debug.Log("创建空格键图标");
        }
        
        /// <summary>
        /// 创建空格键纹理
        /// </summary>
        private void CreateSpaceKeyTexture()
        {
            Texture2D keyTexture = new Texture2D(80, 30);
            Color[] pixels = new Color[80 * 30];
            
            // 创建带边框的按键效果
            for (int y = 0; y < 30; y++)
            {
                for (int x = 0; x < 80; x++)
                {
                    if (x == 0 || x == 79 || y == 0 || y == 29)
                    {
                        pixels[y * 80 + x] = new Color(0.6f, 0.7f, 0.8f, 1f); // 边框
                    }
                    else if (x < 3 || x > 76 || y < 3 || y > 26)
                    {
                        pixels[y * 80 + x] = new Color(0.7f, 0.8f, 0.9f, 1f); // 外边缘
                    }
                    else
                    {
                        pixels[y * 80 + x] = new Color(0.8f, 0.9f, 1f, 1f); // 内部
                    }
                }
            }
            
            keyTexture.SetPixels(pixels);
            keyTexture.Apply();
            
            spaceKeyIcon.sprite = Sprite.Create(keyTexture, new Rect(0, 0, 80, 30), new Vector2(0.5f, 0.5f));
        }
        
        /// <summary>
        /// 创建关闭按钮
        /// </summary>
        private void CreateCloseButton()
        {
            if (cuttingArea == null) return;
            
            GameObject closeBtn = new GameObject("CloseButton");
            closeBtn.transform.SetParent(cuttingArea.transform, false);
            
            RectTransform btnRect = closeBtn.AddComponent<RectTransform>();
            
            // 设置在右上角，高度减半
            btnRect.anchorMin = new Vector2(0.85f, 0.9f);   // 调整Y位置
            btnRect.anchorMax = new Vector2(0.98f, 0.975f); // 高度减半：从0.98改为0.975 (一半高度)
            btnRect.offsetMin = Vector2.zero;
            btnRect.offsetMax = Vector2.zero;
            
            // 添加按钮背景
            Image btnBg = closeBtn.AddComponent<Image>();
            btnBg.color = new Color(0.8f, 0.2f, 0.2f, 0.9f); // 红色背景
            
            // 添加按钮组件
            Button button = closeBtn.AddComponent<Button>();
            button.onClick.AddListener(CloseInterface);
            
            // 添加按钮文字
            GameObject btnText = new GameObject("CloseButtonText");
            btnText.transform.SetParent(closeBtn.transform, false);
            
            RectTransform textRect = btnText.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            Text text = btnText.AddComponent<Text>();
            text.text = LocalizationManager.Instance?.GetText("cutting_system.button.close") ?? "关闭";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 14; // 稍小的字体适应更小的按钮
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontStyle = FontStyle.Bold;
            
            Debug.Log("创建关闭按钮");
        }
        
        /// <summary>
        /// 关闭切割界面
        /// </summary>
        private void CloseInterface()
        {
            // 重置切割台状态并隐藏UI
            ResetCuttingStation();
            Debug.Log("用户点击关闭按钮");
        }
        
        /// <summary>
        /// 通知投放区域切割结果
        /// </summary>
        private void NotifyDropZone(bool success)
        {
            Debug.Log($"=== [SampleCuttingGame] NotifyDropZone 开始执行，success = {success} ===");
            // 尝试在父级查找SampleDropZone
            SampleDropZone dropZone = GetComponentInParent<SampleDropZone>();
            if (dropZone != null)
            {
                Debug.Log($"=== [SampleCuttingGame] 找到SampleDropZone，即将调用OnCuttingComplete ===");
                dropZone.OnCuttingComplete(success);
                Debug.Log($"通知投放区域切割结果: {success}");
            }
            else
            {
                Debug.LogWarning("未找到SampleDropZone组件，无法通知切割结果");
            }
        }
        
        void Update()
        {
            HandleInput();
            UpdateCuttingLine();
            UpdateUI();
        }
        
        /// <summary>
        /// 初始化UI组件
        /// </summary>
        private void InitializeUI()
        {
            // 确保切割线初始位置在顶部
            if (cuttingLine != null)
            {
                // 切割线现在使用顶部锚点，位置应该为0
                cuttingLine.anchoredPosition = new Vector2(0, 0);
            }
            
            // 设置空格键图标闪烁效果
            if (spaceKeyIcon != null)
            {
                StartCoroutine(BlinkSpaceKeyIcon());
            }
        }
        
        /// <summary>
        /// 处理玩家输入
        /// </summary>
        private void HandleInput()
        {
            if (currentState != CuttingState.Cutting) return;

            bool shouldCut = false;

            // 键盘空格键检测
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                shouldCut = true;
            }

            // 移动端触摸检测
            if (Touchscreen.current != null)
            {
                for (int i = 0; i < Touchscreen.current.touches.Count; i++)
                {
                    var touch = Touchscreen.current.touches[i];
                    if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
                    {
                        shouldCut = true;
                        break;
                    }
                }
            }

            // 鼠标点击检测（桌面端）
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                shouldCut = true;
            }

            if (shouldCut)
            {
                PerformCut();
            }
        }
        
        /// <summary>
        /// 更新切割线位置 - 往复移动
        /// </summary>
        private void UpdateCuttingLine()
        {
            if (currentState != CuttingState.Cutting || cuttingLine == null || cuttingArea == null)
                return;
                
            // 获取切割区域高度
            float areaHeight = cuttingArea.rect.height;
            Vector3 currentPos = cuttingLine.anchoredPosition;
            
            // 根据移动方向更新位置
            if (cuttingLineMovingDown)
            {
                // 向下移动（Y值变为负数）
                currentPos.y -= cuttingLineSpeed * Time.deltaTime;
                
                // 检查是否到达底部
                if (currentPos.y <= -areaHeight)
                {
                    currentPos.y = -areaHeight; // 限制在底部
                    cuttingLineMovingDown = false; // 反转方向
                    Debug.Log("红线到达底部，开始向上移动");
                }
            }
            else
            {
                // 向上移动（Y值变为正数）
                currentPos.y += cuttingLineSpeed * Time.deltaTime;
                
                // 检查是否到达顶部
                if (currentPos.y >= 0)
                {
                    currentPos.y = 0; // 限制在顶部
                    cuttingLineMovingDown = true; // 反转方向
                    Debug.Log("红线到达顶部，开始向下移动");
                }
            }
            
            cuttingLine.anchoredPosition = currentPos;
        }
        
        /// <summary>
        /// 更新UI显示
        /// </summary>
        private void UpdateUI()
        {
            if (instructionText != null)
            {
                switch (currentState)
                {
                    case CuttingState.WaitingForSample:
                        instructionText.text = LocalizationManager.Instance?.GetText("cutting_system.instruction.drag_sample") ?? "将多层样本拖拽到切割台";
                        break;
                    case CuttingState.Preparing:
                        instructionText.text = LocalizationManager.Instance?.GetText("cutting_system.analyzing_sample") ?? "分析样本中...";
                        break;
                    case CuttingState.Cutting:
                        instructionText.text = LocalizationManager.Instance?.GetText("cutting_system.cutting_progress", currentCuttingIndex + 1, layerBoundaries.Length) ?? $"切割进度: {currentCuttingIndex + 1}/{layerBoundaries.Length}";
                        break;
                    case CuttingState.Success:
                        instructionText.text = LocalizationManager.Instance?.GetText("cutting_system.cutting_complete") ?? "切割成功！";
                        break;
                    case CuttingState.Failed:
                        instructionText.text = LocalizationManager.Instance?.GetText("cutting_system.cutting_failed") ?? "切割失败，样本损坏";
                        break;
                    case CuttingState.Completed:
                        instructionText.text = LocalizationManager.Instance?.GetText("cutting_system.all_cuts_complete") ?? "所有切割完成！";
                        break;
                }
            }
            
            // 显示/隐藏空格键提示
            GameObject spaceKeyPanel = GameObject.Find("SpaceKeyPanel");
            if (spaceKeyPanel != null)
            {
                spaceKeyPanel.SetActive(currentState == CuttingState.Cutting);
            }
        }
        
        /// <summary>
        /// 执行切割操作
        /// </summary>
        private void PerformCut()
        {
            if (currentCuttingIndex >= layerBoundaries.Length)
                return;
                
            StopLaserSound();
            
            // 获取当前切割线位置
            float currentLinePos = GetNormalizedCuttingLinePosition();
            
            // 调试信息：显示详细的判定数值
            if (currentCuttingIndex < layerBoundaries.Length)
            {
                LayerBoundary boundary = layerBoundaries[currentCuttingIndex];
                float zoneHalfSize = boundary.successZoneSize / 2f;
                float zoneStart = boundary.position - zoneHalfSize;
                float zoneEnd = boundary.position + zoneHalfSize;
                
                Debug.Log($"=== 切割判定调试 ===");
                Debug.Log($"当前切割线位置: {currentLinePos:F4}");
                Debug.Log($"目标边界位置: {boundary.position:F4}");
                Debug.Log($"成功区域大小: {boundary.successZoneSize:F4}");
                Debug.Log($"成功区域范围: {zoneStart:F4} - {zoneEnd:F4}");
                Debug.Log($"判定结果: {(currentLinePos >= zoneStart && currentLinePos <= zoneEnd ? "成功" : "失败")}");
            }
            
            // 检查是否在成功区域内
            bool isSuccessful = IsPositionInSuccessZone(currentLinePos);
            
            if (isSuccessful)
            {
                HandleSuccessfulCut();
            }
            else
            {
                HandleFailedCut();
            }
        }
        
        /// <summary>
        /// 获取切割线的标准化位置 (0-1之间)
        /// </summary>
        private float GetNormalizedCuttingLinePosition()
        {
            if (cuttingLine == null || cuttingArea == null)
                return 0f;
                
            float areaHeight = cuttingArea.rect.height;
            float lineY = cuttingLine.anchoredPosition.y;
            
            // 将Y坐标转换为0-1的标准化值 (顶部=0, 底部=1)
            // 由于使用顶部锚点，Y=0为顶部，Y=-areaHeight为底部
            return Mathf.Clamp01(-lineY / areaHeight);
        }
        
        /// <summary>
        /// 检查位置是否在成功区域内
        /// </summary>
        private bool IsPositionInSuccessZone(float normalizedPosition)
        {
            if (currentCuttingIndex >= layerBoundaries.Length)
                return false;
                
            LayerBoundary boundary = layerBoundaries[currentCuttingIndex];
            float zoneHalfSize = boundary.successZoneSize / 2f;
            
            return normalizedPosition >= (boundary.position - zoneHalfSize) && 
                   normalizedPosition <= (boundary.position + zoneHalfSize);
        }
        
        /// <summary>
        /// 处理成功的切割
        /// </summary>
        private void HandleSuccessfulCut()
        {
            SetState(CuttingState.Success);
            
            // 播放成功音效
            if (successSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(successSound);
            }
            
            // 显示成功反馈
            StartCoroutine(ShowSuccessFlash());
            
            // 继续下一次切割或完成
            currentCuttingIndex++;
            if (currentCuttingIndex >= layerBoundaries.Length)
            {
                // 所有切割完成
                StartCoroutine(CompleteCutting());
            }
            else
            {
                // 准备下一次切割
                StartCoroutine(PrepareNextCut());
            }
        }
        
        /// <summary>
        /// 处理失败的切割
        /// </summary>
        private void HandleFailedCut()
        {
            SetState(CuttingState.Failed);
            
            // 播放失败音效
            if (failureSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(failureSound);
            }
            
            // 显示失败反馈
            StartCoroutine(ShowFailureFlash());
            
            // 样本报废
            StartCoroutine(HandleSampleDestruction());
        }
        
        /// <summary>
        /// 设置游戏状态
        /// </summary>
        private void SetState(CuttingState newState)
        {
            currentState = newState;
            
            // 根据状态播放或停止激光声音
            if (newState == CuttingState.Cutting)
            {
                StartLaserSound();
            }
            else
            {
                StopLaserSound();
            }
        }
        
        /// <summary>
        /// 开始激光切割声音
        /// </summary>
        private void StartLaserSound()
        {
            if (laserHumSound != null && audioSource != null)
            {
                StopLaserSound(); // 确保之前的声音停止
                laserSoundCoroutine = StartCoroutine(PlayLaserSoundLoop());
            }
        }
        
        /// <summary>
        /// 停止激光切割声音
        /// </summary>
        private void StopLaserSound()
        {
            if (laserSoundCoroutine != null)
            {
                StopCoroutine(laserSoundCoroutine);
                laserSoundCoroutine = null;
            }
            
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }
        
        /// <summary>
        /// 循环播放激光声音
        /// </summary>
        private IEnumerator PlayLaserSoundLoop()
        {
            while (currentState == CuttingState.Cutting)
            {
                if (audioSource != null && laserHumSound != null)
                {
                    audioSource.clip = laserHumSound;
                    audioSource.loop = true;
                    audioSource.Play();
                }
                yield return null;
            }
        }
        
        /// <summary>
        /// 显示成功闪光效果
        /// </summary>
        private IEnumerator ShowSuccessFlash()
        {
            if (successZone != null)
            {
                Color originalColor = successZone.color;
                successZone.color = Color.green;
                
                for (int i = 0; i < 3; i++)
                {
                    successZone.gameObject.SetActive(true);
                    yield return new WaitForSeconds(0.1f);
                    successZone.gameObject.SetActive(false);
                    yield return new WaitForSeconds(0.1f);
                }
                
                successZone.color = originalColor;
            }
        }
        
        /// <summary>
        /// 显示失败闪光效果
        /// </summary>
        private IEnumerator ShowFailureFlash()
        {
            if (successZone != null)
            {
                Color originalColor = successZone.color;
                successZone.color = Color.red;
                
                for (int i = 0; i < 5; i++)
                {
                    successZone.gameObject.SetActive(true);
                    yield return new WaitForSeconds(0.15f);
                    successZone.gameObject.SetActive(false);
                    yield return new WaitForSeconds(0.15f);
                }
                
                successZone.color = originalColor;
            }
        }
        
        /// <summary>
        /// 空格键图标闪烁效果
        /// </summary>
        private IEnumerator BlinkSpaceKeyIcon()
        {
            while (true)
            {
                GameObject spaceKeyPanel = GameObject.Find("SpaceKeyPanel");
                if (spaceKeyPanel != null && currentState == CuttingState.Cutting)
                {
                    // 整个面板的闪烁效果
                    Image panelBg = spaceKeyPanel.GetComponent<Image>();
                    if (panelBg != null)
                    {
                        panelBg.color = new Color(0.2f, 0.2f, 0.2f, 0.9f); // 亮一点的黑色
                        yield return new WaitForSeconds(0.5f);
                        panelBg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f); // 正常的黑色半透明
                        yield return new WaitForSeconds(0.5f);
                    }
                    else
                    {
                        yield return new WaitForSeconds(0.1f);
                    }
                }
                else
                {
                    yield return new WaitForSeconds(0.1f);
                }
            }
        }
        
        /// <summary>
        /// 准备下一次切割
        /// </summary>
        private IEnumerator PrepareNextCut()
        {
            yield return new WaitForSeconds(1f);
            
            // 更新成功区域位置
            UpdateSuccessZone();
            
            // 重置切割线位置
            ResetCuttingLine();
            
            // 开始下一次切割
            SetState(CuttingState.Cutting);
        }
        
        /// <summary>
        /// 完成所有切割
        /// </summary>
        private IEnumerator CompleteCutting()
        {
            yield return new WaitForSeconds(1.5f);
            SetState(CuttingState.Completed);
            
            // 生成切割后的样本
            GenerateCutSamples();
            
            // 通知投放区域切割成功
            NotifyDropZone(true);
            
            // 通知切割系统管理器切割成功
            if (systemManager != null && currentSample != null)
            {
                Debug.Log("[SampleCuttingGame] 通知SampleCuttingSystemManager切割成功");
                systemManager.HandleCuttingSuccess(currentSample);
            }
            
            // 2秒后重置（因为投放区域会处理显示）
            yield return new WaitForSeconds(2f);
            ResetCuttingStation();
        }
        
        /// <summary>
        /// 处理样本销毁
        /// </summary>
        private IEnumerator HandleSampleDestruction()
        {
            yield return new WaitForSeconds(2f);
            
            // 销毁原始样本
            if (currentSample?.sampleContainer != null)
            {
                Destroy(currentSample.sampleContainer);
            }
            
            // 通知投放区域切割失败
            NotifyDropZone(false);
            
            // 通知切割系统管理器切割失败
            if (systemManager != null && currentSample != null)
            {
                Debug.Log("[SampleCuttingGame] 通知SampleCuttingSystemManager切割失败");
                systemManager.HandleCuttingFailure(currentSample);
            }
            
            // 重置切割台
            ResetCuttingStation();
        }
        
        /// <summary>
        /// 更新成功区域显示
        /// </summary>
        private void UpdateSuccessZone()
        {
            if (currentCuttingIndex >= layerBoundaries.Length || successZone == null)
                return;
                
            LayerBoundary boundary = layerBoundaries[currentCuttingIndex];
            
            // 设置成功区域的位置和大小
            RectTransform successRect = successZone.rectTransform;
            
            // 使用与切割线相同的坐标系统 (顶部=0, 底部=1)
            float areaHeight = cuttingArea.rect.height;
            float yPosition = -boundary.position * areaHeight; // 标准化位置转换为UI Y坐标
            float zoneHeight = boundary.successZoneSize * areaHeight;
            
            Debug.Log($"=== 成功区域更新调试 ===");
            Debug.Log($"边界位置: {boundary.position:F4}");
            Debug.Log($"切割区域高度: {areaHeight}");
            Debug.Log($"成功区域Y位置: {yPosition}");
            Debug.Log($"成功区域高度: {zoneHeight}");
            
            successRect.anchoredPosition = new Vector2(0, yPosition);
            successRect.sizeDelta = new Vector2(successRect.sizeDelta.x, zoneHeight);
            
            // 设置颜色 (半透明绿色)
            successZone.color = new Color(0f, 1f, 0f, 0.3f);
            successZone.gameObject.SetActive(true);
        }
        
        /// <summary>
        /// 重置切割线位置和方向
        /// </summary>
        private void ResetCuttingLine()
        {
            if (cuttingLine != null)
            {
                // 重置到顶部位置
                cuttingLine.anchoredPosition = new Vector2(0, 0);
                // 重置移动方向为向下
                cuttingLineMovingDown = true;
                Debug.Log("切割线已重置：位置=顶部，方向=向下");
            }
        }
        
        /// <summary>
        /// 生成切割后的样本
        /// </summary>
        private void GenerateCutSamples()
        {
            if (currentSample == null) return;
            
            Debug.Log($"开始生成 {currentSample.layerSegments.Length} 个切割后的样本");
            
            // 查找实验台位置（用于放置样本）
            Vector3 workstationCenter = FindWorkstationCenter();
            
            // 为每个地质层段生成一个独立的样本
            for (int i = 0; i < currentSample.layerSegments.Length; i++)
            {
                var layerSegment = currentSample.layerSegments[i];
                
                // 计算样本放置位置（围绕实验台中心呈圆形分布）
                Vector3 samplePosition = CalculateSamplePosition(workstationCenter, i, currentSample.layerSegments.Length);
                
                // 创建单层样本对象
                GameObject cutSample = CreateCutSampleObject(layerSegment, samplePosition, i);
                
                if (cutSample != null)
                {
                    Debug.Log($"切割样本 {i+1} 已生成在位置: {samplePosition}");
                    
                    // 添加收集组件（使其可以被玩家收集）
                    AddSampleCollectionComponent(cutSample, layerSegment, i);
                }
                else
                {
                    Debug.LogError($"切割样本 {i+1} 创建失败");
                }
            }
            
            Debug.Log($"所有 {currentSample.layerSegments.Length} 个切割样本已生成完成");
        }
        
        /// <summary>
        /// 创建智能样本材质（与3D ModelViewer保持一致）
        /// </summary>
        private Material CreateIntelligentSampleMaterial(GeometricSampleReconstructor.LayerSegment layerSegment, int segmentIndex)
        {
            Debug.Log($"[智能材质] 开始创建材质，段索引: {segmentIndex}");
            
            // 🔑 关键修复：直接使用layerSegment.material（这就是真实的地质材质）
            if (layerSegment.material != null)
            {
                Material originalMaterial = layerSegment.material;
                Debug.Log($"[智能材质] LayerSegment材质信息:");
                Debug.Log($"  - 名称: {originalMaterial.name}");
                Debug.Log($"  - 着色器: {originalMaterial.shader.name}");
                Debug.Log($"  - 颜色: {originalMaterial.color}");
                Debug.Log($"  - 主纹理: {originalMaterial.mainTexture?.name ?? "无"}");
                
                // 直接返回原始材质（不复制，保持所有属性和纹理）
                Debug.Log($"[智能材质] ✅ 直接使用LayerSegment的真实材质");
                return originalMaterial;
            }
            
            // 备用方案：如果LayerSegment.material为null，创建基于索引的材质
            Debug.Log($"[智能材质] ⚠️ LayerSegment.material为null，使用备用方案");
            Debug.Log($"[智能材质] layerSegment.sourceLayer: {layerSegment.sourceLayer?.layerName ?? "null"}");
            
            Material material = new Material(Shader.Find("Standard"));
            
            // 策略2：使用源层的颜色
            if (layerSegment.sourceLayer != null)
            {
                material.color = layerSegment.sourceLayer.layerColor;
                material.name = $"CutSampleMaterial_{segmentIndex}_{layerSegment.sourceLayer.layerName}";
                Debug.Log($"[智能材质] ✅ 使用sourceLayer.layerColor，层名: {layerSegment.sourceLayer.layerName}，颜色: {material.color}");
            }
            // 策略3：默认颜色
            else
            {
                // 生成基于索引的色彩方案
                Color[] segmentColors = {
                    new Color(0.8f, 0.3f, 0.2f, 1f), // 红色系
                    new Color(0.2f, 0.7f, 0.3f, 1f), // 绿色系
                    new Color(0.3f, 0.4f, 0.8f, 1f), // 蓝色系
                    new Color(0.7f, 0.6f, 0.2f, 1f), // 黄色系
                    new Color(0.6f, 0.2f, 0.7f, 1f), // 紫色系
                    new Color(0.4f, 0.7f, 0.7f, 1f), // 青色系
                    new Color(0.8f, 0.5f, 0.3f, 1f), // 橙色系
                    new Color(0.5f, 0.5f, 0.5f, 1f)  // 灰色系
                };
                
                int colorIndex = segmentIndex % segmentColors.Length;
                material.color = segmentColors[colorIndex];
                material.name = $"CutSampleMaterial_{segmentIndex}_Fallback";
                Debug.Log($"[智能材质] ⚠️ 使用默认颜色方案，索引: {segmentIndex}，颜色: {material.color}");
            }
            
            // 设置默认材质属性
            if (layerSegment.material == null)
            {
                material.SetFloat("_Metallic", 0.0f);
                material.SetFloat("_Glossiness", 0.3f);
            }
            
            Debug.Log($"[智能材质] 材质创建完成: {material.name}，最终颜色: {material.color}");
            return material;
        }
        
        /// <summary>
        /// 分析切割样本材质信息
        /// </summary>
        private void AnalyzeCutSampleMaterial(Material material, int segmentIndex)
        {
            try
            {
                Debug.Log($"📤 [切割输出] ===== 切割样本材质分析开始 =====");
                Debug.Log($"📤 [切割输出] 样本索引: {segmentIndex}");
                Debug.Log($"📤 [切割输出] 材质名称: {material.name}");
                Debug.Log($"📤 [切割输出] 着色器: {material.shader.name}");
                Debug.Log($"📤 [切割输出] 颜色: {material.color}");
                Debug.Log($"📤 [切割输出] 主纹理: {material.mainTexture?.name ?? "无"}");
                
                // 检查所有纹理属性
                var textureNames = material.GetTexturePropertyNames();
                if (textureNames.Length > 0)
                {
                    Debug.Log($"📤 [切割输出] 所有纹理属性:");
                    foreach (string texName in textureNames)
                    {
                        var texture = material.GetTexture(texName);
                        Debug.Log($"📤 [切割输出]   {texName}: {texture?.name ?? "null"}");
                    }
                }
                else
                {
                    Debug.Log($"📤 [切割输出] 无纹理属性");
                }
                
                // 检查重要的材质参数
                if (material.HasProperty("_Metallic"))
                {
                    Debug.Log($"📤 [切割输出] 金属度: {material.GetFloat("_Metallic")}");
                }
                if (material.HasProperty("_Glossiness"))
                {
                    Debug.Log($"📤 [切割输出] 光泽度: {material.GetFloat("_Glossiness")}");
                }
                if (material.HasProperty("_BumpMap"))
                {
                    var bumpTexture = material.GetTexture("_BumpMap");
                    Debug.Log($"📤 [切割输出] 法线贴图: {bumpTexture?.name ?? "无"}");
                }
                
                Debug.Log($"📤 [切割输出] ===== 切割样本材质分析结束 =====");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"📤 [切割输出] 材质分析失败: {e.Message}");
            }
        }
        
        
        /// <summary>
        /// 根据层名称获取颜色（与3D ModelViewer保持一致）
        /// </summary>
        private Color GetColorByLayerName(string layerName, int index)
        {
            switch (layerName.ToLower())
            {
                case "砂岩": case "sandstone":
                    return new Color(0.9f, 0.8f, 0.6f, 1f);
                case "页岩": case "shale":
                    return new Color(0.4f, 0.4f, 0.4f, 1f);
                case "石灰岩": case "limestone":
                    return new Color(0.8f, 0.8f, 0.7f, 1f);
                case "花岗岩": case "granite":
                    return new Color(0.6f, 0.5f, 0.5f, 1f);
                default:
                    return GetLayerColor(index);
            }
        }
        
        /// <summary>
        /// 获取层级颜色（与3D ModelViewer保持一致）
        /// </summary>
        private Color GetLayerColor(int index)
        {
            Color[] colors = {
                new Color(0.8f, 0.6f, 0.4f, 1f), // 浅褐色
                new Color(0.6f, 0.8f, 0.4f, 1f), // 浅绿色
                new Color(0.4f, 0.6f, 0.8f, 1f), // 浅蓝色
                new Color(0.8f, 0.4f, 0.6f, 1f), // 浅红色
                new Color(0.8f, 0.8f, 0.4f, 1f), // 浅黄色
                new Color(0.6f, 0.4f, 0.8f, 1f)  // 浅紫色
            };
            return colors[index % colors.Length];
        }
        
        /// <summary>
        /// 查找实验台中心位置
        /// </summary>
        private Vector3 FindWorkstationCenter()
        {
            // 优先使用存储的工作台位置
            if (currentWorkstationPosition.HasValue)
            {
                Vector3 stationPos = currentWorkstationPosition.Value;
                Debug.Log($"使用存储的工作台位置: {stationPos}");
                return new Vector3(stationPos.x, stationPos.y + 0.5f, stationPos.z);
            }
            
            // 查找切割台对象（备用方案）
            GameObject cuttingStation = GameObject.Find("LaboratoryCuttingStation");
            if (cuttingStation != null)
            {
                // 返回切割台上方位置作为样本放置区域
                Vector3 stationPos = cuttingStation.transform.position;
                Debug.Log($"使用默认切割台位置: {stationPos}");
                return new Vector3(stationPos.x, stationPos.y + 0.5f, stationPos.z);
            }
            
            // 如果没找到切割台，查找包含"table"或相关词汇的对象
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            foreach (var obj in allObjects)
            {
                string objName = obj.name.ToLower();
                if (objName.Contains("table") || objName.Contains("workstation") || 
                    objName.Contains("bench") || objName.Contains("desk"))
                {
                    Vector3 tablePos = obj.transform.position;
                    return new Vector3(tablePos.x, tablePos.y + 0.8f, tablePos.z);
                }
            }
            
            // 默认位置（如果找不到实验台）
            Debug.LogWarning("未找到实验台，使用默认位置");
            return new Vector3(0f, 1.5f, 0f);
        }
        
        /// <summary>
        /// 计算样本放置位置（圆形分布）
        /// </summary>
        private Vector3 CalculateSamplePosition(Vector3 center, int index, int totalCount)
        {
            // 基础半径
            float baseRadius = 0.8f;
            
            // 如果样本数量很多，增加半径避免重叠
            if (totalCount > 8)
            {
                baseRadius = 1.2f;
            }
            else if (totalCount > 4)
            {
                baseRadius = 1.0f;
            }
            
            // 计算角度（均匀分布在圆周上）
            float angle = (index * 2f * Mathf.PI) / totalCount;
            
            // 添加一些随机偏移避免完全规律的排列
            float randomOffset = UnityEngine.Random.Range(-0.1f, 0.1f);
            angle += randomOffset;
            
            // 计算位置
            float x = center.x + Mathf.Cos(angle) * baseRadius;
            float z = center.z + Mathf.Sin(angle) * baseRadius;
            float y = center.y + 0.2f; // 样本悬浮在实验台上方
            
            return new Vector3(x, y, z);
        }
        
        /// <summary>
        /// 创建切割样本对象
        /// </summary>
        private GameObject CreateCutSampleObject(GeometricSampleReconstructor.LayerSegment layerSegment, Vector3 position, int segmentIndex)
        {
            // 创建样本容器
            GameObject sampleObj = new GameObject($"CutSample_{segmentIndex:D2}_{layerSegment.sourceLayer.layerName}");
            sampleObj.transform.position = position;
            
            // 创建几何形状（圆柱体，表示切割后的样本段）
            GameObject meshObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            meshObj.name = "SampleMesh";
            meshObj.transform.SetParent(sampleObj.transform);
            meshObj.transform.localPosition = Vector3.zero;
            
            // 设置尺寸（基于原始样本和深度比例）
            float originalRadius = 0.1f; // 钻探样本半径
            float segmentHeight = CalculateSegmentThickness(layerSegment);
            
            // 限制最小和最大高度
            segmentHeight = Mathf.Clamp(segmentHeight, 0.05f, 1.0f);
            
            meshObj.transform.localScale = new Vector3(
                originalRadius * 2f, // X轴：直径
                segmentHeight / 2f,   // Y轴：圆柱体的高度是scale的两倍
                originalRadius * 2f   // Z轴：直径
            );
            
            // 设置材质和颜色（使用与3D ModelViewer相同的智能材质系统）
            Renderer renderer = meshObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Debug.Log($"🔥 [CRITICAL] 准备调用CreateIntelligentSampleMaterial，段索引: {segmentIndex}");
                Debug.Log($"🔥 [CRITICAL] LayerSegment.material状态: {(layerSegment.material != null ? layerSegment.material.name : "NULL")}");
                
                // 🔥 强制直接使用LayerSegment.material
                if (layerSegment.material != null)
                {
                    Debug.Log($"🔥 [DIRECT] 直接使用LayerSegment.material: {layerSegment.material.name}");
                    Debug.Log($"🔥 [DIRECT] 原材质着色器: {layerSegment.material.shader.name}");
                    Debug.Log($"🔥 [DIRECT] 原材质颜色: {layerSegment.material.color}");
                    Debug.Log($"🔥 [DIRECT] 原材质纹理: {layerSegment.material.mainTexture?.name ?? "无"}");
                    
                    renderer.material = layerSegment.material;
                    Debug.Log($"🔥 [DIRECT] 直接应用材质完成！");
                }
                else
                {
                    Material sampleMaterial = CreateIntelligentSampleMaterial(layerSegment, segmentIndex);
                    renderer.material = sampleMaterial;
                }
                
                Material finalMaterial = renderer.material;
                Debug.Log($"🔥 [CRITICAL] 最终材质应用完成: {finalMaterial.name}, 着色器: {finalMaterial.shader.name}");
                Debug.Log($"切割样本 {segmentIndex} 材质设置完成，最终颜色: {finalMaterial.color}");
                
                // 📤 输出切割样本的完整材质信息
                AnalyzeCutSampleMaterial(finalMaterial, segmentIndex);
            }
            
            // 添加物理组件
            Rigidbody rb = sampleObj.AddComponent<Rigidbody>();
            rb.mass = 0.3f;
            rb.linearDamping = 3f;
            rb.angularDamping = 5f;
            
            // 添加简单的掉落效果
            SampleDropController dropController = sampleObj.AddComponent<SampleDropController>();
            if (dropController != null)
            {
                dropController.dropHeight = 1.0f;
                dropController.maxBounces = 1;
                dropController.bounceReduction = 0.7f; // 正确的属性名
            }
            
            Debug.Log($"创建切割样本: {sampleObj.name}, 位置: {position}, 厚度: {segmentHeight:F3}m");
            
            return sampleObj;
        }
        
        /// <summary>
        /// 为切割样本添加收集组件
        /// </summary>
        private void AddSampleCollectionComponent(GameObject sampleObj, GeometricSampleReconstructor.LayerSegment layerSegment, int segmentIndex)
        {
            // 创建对应的SampleItem数据
            SampleItem cutSampleData = CreateCutSampleData(layerSegment, segmentIndex);
            
            // 添加样本收集器
            SampleCollector collector = sampleObj.AddComponent<SampleCollector>();
            if (collector != null)
            {
                // 使用Setup方法正确设置样本数据
                collector.Setup(cutSampleData);
                collector.interactionRange = 2.0f;
                
                // 确保添加碰撞器用于检测交互
                if (sampleObj.GetComponent<Collider>() == null)
                {
                    SphereCollider collider = sampleObj.AddComponent<SphereCollider>();
                    collider.isTrigger = true;
                    collider.radius = 2.0f; // 与交互范围匹配
                    Debug.Log($"为切割样本 {segmentIndex + 1} 添加了碰撞器");
                }
                
                Debug.Log($"切割样本 {segmentIndex + 1} 收集组件已添加，sampleID: {cutSampleData.sampleID}");
            }
        }
        
        /// <summary>
        /// 创建切割样本的数据结构
        /// </summary>
        private SampleItem CreateCutSampleData(GeometricSampleReconstructor.LayerSegment layerSegment, int segmentIndex)
        {
            SampleItem cutSample = new SampleItem();
            
            // 基础信息
            cutSample.sampleID = System.Guid.NewGuid().ToString();
            cutSample.displayName = $"切割样本 {segmentIndex + 1:D2} - {layerSegment.sourceLayer.layerName}";
            cutSample.sourceToolID = "9999"; // 特殊工具ID标识切割样本
            
            // 位置和尺寸信息
            cutSample.originalCollectionPosition = Vector3.zero; // 会在收集时更新
            cutSample.sampleRadius = 0.1f;
            float segmentThickness = CalculateSegmentThickness(layerSegment);
            cutSample.totalDepth = segmentThickness;
            
            // 地质层信息（使用SampleItem.LayerInfo类）
            cutSample.geologicalLayers = new List<SampleItem.LayerInfo>
            {
                new SampleItem.LayerInfo
                {
                    layerName = layerSegment.sourceLayer.layerName,
                    layerColor = layerSegment.sourceLayer.layerColor,
                    depthStart = 0f, // 切割样本从0开始
                    depthEnd = segmentThickness,
                    thickness = segmentThickness,
                    materialName = layerSegment.sourceLayer.layerMaterial != null ? layerSegment.sourceLayer.layerMaterial.name : "Unknown",
                    layerDescription = $"切割样本段 - 厚度 {segmentThickness:F2}m"
                }
            };
            
            // 样本状态
            cutSample.currentLocation = SampleLocation.InWorld;
            cutSample.collectionTime = System.DateTime.Now; // 使用DateTime属性
            
            Debug.Log($"切割样本数据创建完成: {cutSample.displayName}");
            return cutSample;
        }
        
        /// <summary>
        /// 计算层段厚度
        /// </summary>
        private float CalculateSegmentThickness(GeometricSampleReconstructor.LayerSegment layerSegment)
        {
            // 尝试从几何体获取厚度
            if (layerSegment.geometry != null && layerSegment.geometry.bounds.size.y > 0)
            {
                return layerSegment.geometry.bounds.size.y;
            }
            
            // 尝试从段对象获取厚度
            if (layerSegment.segmentObject != null)
            {
                Renderer renderer = layerSegment.segmentObject.GetComponent<Renderer>();
                if (renderer != null && renderer.bounds.size.y > 0)
                {
                    return renderer.bounds.size.y;
                }
                
                // 尝试从Transform的scale获取
                Vector3 scale = layerSegment.segmentObject.transform.localScale;
                if (scale.y > 0)
                {
                    return scale.y;
                }
            }
            
            // 尝试从源地质层获取厚度
            if (layerSegment.sourceLayer != null && layerSegment.sourceLayer.averageThickness > 0)
            {
                return layerSegment.sourceLayer.averageThickness;
            }
            
            // 默认厚度
            Debug.LogWarning($"无法计算层段厚度，使用默认值 0.2m");
            return 0.2f;
        }
        
        /// <summary>
        /// 重置切割台状态
        /// </summary>
        private void ResetCuttingStation()
        {
            currentSample = null;
            layerBoundaries = null;
            currentCuttingIndex = 0;
            cuttingLineMovingDown = true; // 重置切割线移动方向
            
            if (successZone != null)
                successZone.gameObject.SetActive(false);
                
            SetState(CuttingState.WaitingForSample);
            
            // 重新启用玩家跳跃功能
            if (playerController != null)
            {
                playerController.SetJumpEnabled(true);
                Debug.Log("切割系统已重新启用角色跳跃功能");
            }
            
            // 重新启用Tab功能（工具轮盘）
            ReenableInventoryUI();
            
            // 隐藏切割界面
            HideCuttingUI();
        }
        
        /// <summary>
        /// 重新启用Tab功能（工具轮盘）
        /// </summary>
        private void ReenableInventoryUI()
        {
            try
            {
                // 查找InventoryUISystem
                InventoryUISystem inventoryUI = FindFirstObjectByType<InventoryUISystem>();
                if (inventoryUI != null)
                {
                    // 重新启用组件
                    inventoryUI.enabled = true;
                    Debug.Log("✅ 切割系统已重新启用Tab工具轮盘功能");
                }
                else
                {
                    Debug.LogWarning("❌ 未找到InventoryUISystem，无法重新启用Tab功能");
                }
                
                // 查找FirstPersonController并重新启用输入
                if (playerController != null)
                {
                    // 如果FirstPersonController有禁用输入的方法，在这里重新启用
                    var fpsController = playerController.GetComponent<FirstPersonController>();
                    if (fpsController != null)
                    {
                        fpsController.enabled = true;
                        Debug.Log("✅ 切割系统已重新启用玩家输入控制");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"重新启用Tab功能时出错: {e.Message}");
            }
        }
        
        /// <summary>
        /// 隐藏切割界面
        /// </summary>
        private void HideCuttingUI()
        {
            if (cuttingArea != null)
            {
                cuttingArea.gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// 显示切割界面
        /// </summary>
        private void ShowCuttingUI()
        {
            if (cuttingArea != null)
            {
                cuttingArea.gameObject.SetActive(true);
            }
        }
        
        /// <summary>
        /// 公共方法：停止切割游戏
        /// 由SampleDropZone调用，用于停止切割而不关闭实验台
        /// </summary>
        public void StopCutting()
        {
            Debug.Log("🛑 外部请求停止切割游戏");
            
            // 停止激光音效
            StopLaserSound();
            
            // 隐藏切割UI
            HideCuttingUI();
            
            // 重置切割台状态
            ResetCuttingStation();
        }
        
        /// <summary>
        /// 公共接口：开始切割指定样本
        /// </summary>
        public void StartCutting(GeometricSampleReconstructor.ReconstructedSample sample, Vector3? workstationPosition = null)
        {
            if (currentState != CuttingState.WaitingForSample)
            {
                Debug.LogWarning("切割台当前忙碌，请等待当前操作完成");
                return;
            }
            
            // 首次使用时初始化UI组件
            if (cuttingArea == null)
            {
                InitializeUIComponents();
                InitializeUI();
            }
            
            // 只在独立模式下显示切割界面
            bool isEmbedded = transform.parent != null;
            if (!isEmbedded)
            {
                ShowCuttingUI();
            }
            
            currentSample = sample;
            
            // 保存工作台位置用于样本生成
            if (workstationPosition.HasValue)
            {
                currentWorkstationPosition = workstationPosition.Value;
                Debug.Log($"✅ 保存工作台位置: {currentWorkstationPosition.Value}");
            }
            else
            {
                Debug.LogError("❌ StartCutting调用时未传递工作台位置！");
            }
            
            // 禁用玩家跳跃功能，避免空格键冲突
            if (playerController != null)
            {
                playerController.SetJumpEnabled(false);
                Debug.Log("切割系统已禁用角色跳跃功能");
            }
            else
            {
                Debug.LogWarning("无法禁用跳跃：未找到FirstPersonController");
            }
            
            SetState(CuttingState.Preparing);
            
            StartCoroutine(AnalyzeSampleAndStartCutting());
        }
        
        /// <summary>
        /// 分析样本并开始切割流程
        /// </summary>
        private IEnumerator AnalyzeSampleAndStartCutting()
        {
            yield return new WaitForSeconds(1f); // 模拟分析时间
            
            if (layerAnalyzer != null)
            {
                layerBoundaries = layerAnalyzer.AnalyzeLayerBoundaries(currentSample);
            }
            
            if (layerBoundaries != null && layerBoundaries.Length > 0)
            {
                currentCuttingIndex = 0;
                UpdateSuccessZone();
                ResetCuttingLine();
                SetState(CuttingState.Cutting);
            }
            else
            {
                Debug.LogError("无法分析样本层结构");
                ResetCuttingStation();
            }
        }
    }
}
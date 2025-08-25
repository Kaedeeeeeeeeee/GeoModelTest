using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace SampleCuttingSystem
{
    /// <summary>
    /// 3D样本模型查看器控制器
    /// 处理UI层面的鼠标交互事件，并传递给Sample3DModelViewer
    /// </summary>
    public class Sample3DModelViewerController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI控制")]
        public Button resetButton;
        public Button rotationToggleButton;
        
        private Sample3DModelViewer modelViewer;
        private Text rotationButtonText;
        
        void Awake()
        {
            // 查找关联的Sample3DModelViewer组件
            modelViewer = GetComponent<Sample3DModelViewer>();
            if (modelViewer == null)
            {
                modelViewer = GetComponentInParent<Sample3DModelViewer>();
            }
            if (modelViewer == null)
            {
                modelViewer = GetComponentInChildren<Sample3DModelViewer>();
            }
            
            // 设置按钮事件
            SetupButtons();
        }
        
        void Start()
        {
            CreateControlButtons();
            
            // 延迟显示操作说明
            Invoke(nameof(ShowInstructions), 1f);
        }
        
        /// <summary>
        /// 设置按钮事件
        /// </summary>
        void SetupButtons()
        {
            if (resetButton != null)
            {
                resetButton.onClick.AddListener(OnResetView);
            }
            
            if (rotationToggleButton != null)
            {
                rotationToggleButton.onClick.AddListener(OnToggleRotation);
                rotationButtonText = rotationToggleButton.GetComponentInChildren<Text>();
                UpdateRotationButtonText();
            }
        }
        
        /// <summary>
        /// 创建控制按钮（如果不存在）
        /// </summary>
        void CreateControlButtons()
        {
            // 如果没有重置按钮，创建一个简单的控制区域
            if (resetButton == null && rotationToggleButton == null)
            {
                CreateSimpleControlArea();
            }
        }
        
        /// <summary>
        /// 创建简单的控制区域
        /// </summary>
        void CreateSimpleControlArea()
        {
            // 在RawImage的右下角创建控制按钮
            RawImage rawImage = GetComponent<RawImage>();
            if (rawImage == null) return;
            
            // 创建控制面板
            GameObject controlPanel = new GameObject("ControlPanel");
            controlPanel.transform.SetParent(transform);
            
            RectTransform panelRect = controlPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.7f, 0f);
            panelRect.anchorMax = new Vector2(1f, 0.3f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            
            // 添加背景
            Image panelBg = controlPanel.AddComponent<Image>();
            panelBg.color = new Color(0, 0, 0, 0.5f);
            
            // 创建重置按钮
            CreateResetButton(controlPanel);
            
            // 创建旋转切换按钮
            CreateRotationToggleButton(controlPanel);
            
            Debug.Log("3D模型控制区域已创建");
        }
        
        /// <summary>
        /// 创建重置按钮
        /// </summary>
        void CreateResetButton(GameObject parent)
        {
            GameObject buttonObj = new GameObject("ResetButton");
            buttonObj.transform.SetParent(parent.transform);
            
            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.1f, 0.6f);
            buttonRect.anchorMax = new Vector2(0.9f, 0.9f);
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;
            
            // 添加按钮组件
            resetButton = buttonObj.AddComponent<Button>();
            
            // 添加按钮背景
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
            
            // 添加按钮文字
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            Text text = textObj.AddComponent<Text>();
            text.text = "重置";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 12;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            
            // 设置按钮事件
            resetButton.onClick.AddListener(OnResetView);
            
            Debug.Log("重置按钮已创建");
        }
        
        /// <summary>
        /// 创建旋转切换按钮
        /// </summary>
        void CreateRotationToggleButton(GameObject parent)
        {
            GameObject buttonObj = new GameObject("RotationToggleButton");
            buttonObj.transform.SetParent(parent.transform);
            
            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.1f, 0.1f);
            buttonRect.anchorMax = new Vector2(0.9f, 0.4f);
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;
            
            // 添加按钮组件
            rotationToggleButton = buttonObj.AddComponent<Button>();
            
            // 添加按钮背景
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
            
            // 添加按钮文字
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            rotationButtonText = textObj.AddComponent<Text>();
            rotationButtonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            rotationButtonText.fontSize = 10;
            rotationButtonText.color = Color.white;
            rotationButtonText.alignment = TextAnchor.MiddleCenter;
            
            // 设置按钮事件
            rotationToggleButton.onClick.AddListener(OnToggleRotation);
            UpdateRotationButtonText();
            
            Debug.Log("旋转切换按钮已创建");
        }
        
        /// <summary>
        /// 重置视角按钮事件
        /// </summary>
        void OnResetView()
        {
            if (modelViewer != null)
            {
                modelViewer.ResetView();
                UpdateRotationButtonText();
                Debug.Log("触发重置视角");
            }
        }
        
        /// <summary>
        /// 切换自动旋转按钮事件
        /// </summary>
        void OnToggleRotation()
        {
            if (modelViewer != null)
            {
                modelViewer.ToggleAutoRotation();
                UpdateRotationButtonText();
                Debug.Log("触发旋转切换");
            }
        }
        
        /// <summary>
        /// 更新旋转按钮文字
        /// </summary>
        void UpdateRotationButtonText()
        {
            if (rotationButtonText != null && modelViewer != null)
            {
                rotationButtonText.text = modelViewer.IsAutoRotating ? "自动\n旋转" : "手动\n模式";
                
                // 更新按钮颜色
                if (rotationToggleButton != null)
                {
                    Image buttonImage = rotationToggleButton.GetComponent<Image>();
                    if (buttonImage != null)
                    {
                        buttonImage.color = modelViewer.IsAutoRotating ? 
                            new Color(0.2f, 0.6f, 0.2f, 0.8f) : // 绿色表示自动旋转
                            new Color(0.6f, 0.2f, 0.2f, 0.8f);   // 红色表示手动模式
                    }
                }
            }
        }
        
        /// <summary>
        /// 鼠标进入事件
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (modelViewer != null)
            {
                modelViewer.SetMouseOverArea(true);
            }
        }
        
        /// <summary>
        /// 鼠标离开事件
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (modelViewer != null)
            {
                modelViewer.SetMouseOverArea(false);
            }
        }
        
        /// <summary>
        /// 显示操作说明
        /// </summary>
        public void ShowInstructions()
        {
            Debug.Log("=== 3D模型交互说明 ===");
            Debug.Log("• 鼠标拖拽: 手动旋转模型");
            Debug.Log("• 滚轮: 缩放模型");
            Debug.Log("• 重置按钮: 恢复默认视角");
            Debug.Log("• 自动旋转: 开启/关闭自动旋转");
            Debug.Log("• 手动操作后3秒自动恢复自动旋转");
        }
        
        void OnDestroy()
        {
            // 清理按钮事件
            if (resetButton != null)
            {
                resetButton.onClick.RemoveAllListeners();
            }
            
            if (rotationToggleButton != null)
            {
                rotationToggleButton.onClick.RemoveAllListeners();
            }
        }
    }
}
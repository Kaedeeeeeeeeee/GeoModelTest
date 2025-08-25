using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

namespace SampleCuttingSystem
{
    /// <summary>
    /// 仓库系统集成组件
    /// 处理从仓库拖拽多层样本到切割台的交互
    /// </summary>
    public class WarehouseIntegration : MonoBehaviour, IDropHandler, IDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("集成组件")]
        [SerializeField] private SampleCuttingSystemManager cuttingSystemManager;
        [SerializeField] private WarehouseManager warehouseManager;            // 仓库管理器引用
        
        [Header("拖拽区域设置")]
        [SerializeField] private RectTransform dropZone;                       // 拖拽接收区域
        [SerializeField] private Image dropZoneBackground;                     // 拖拽区域背景
        [SerializeField] private Text instructionText;                         // 说明文字
        [SerializeField] private GameObject dragPreview;                       // 拖拽预览对象
        
        [Header("视觉反馈")]
        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.1f);    // 正常状态颜色
        [SerializeField] private Color highlightColor = new Color(0f, 1f, 0f, 0.3f); // 高亮颜色
        [SerializeField] private Color invalidColor = new Color(1f, 0f, 0f, 0.3f);   // 无效颜色
        [SerializeField] private float colorTransitionSpeed = 5f;                    // 颜色过渡速度
        
        [Header("样本筛选")]
        [SerializeField] private bool onlyAcceptMultiLayerSamples = true;      // 只接受多层样本
        [SerializeField] private int minimumLayerCount = 2;                    // 最小地层数量
        [SerializeField] private string[] acceptedSampleTypes = { "GeometricSample" }; // 接受的样本类型
        
        // 内部状态
        private bool isDragOver = false;
        private bool isValidDrag = false;
        private Color targetColor;
        private GameObject currentDraggedItem = null;
        private GeometricSampleReconstructor.ReconstructedSample draggedSample = null;
        
        // 组件引用
        private SampleLayerAnalyzer layerAnalyzer;
        
        void Awake()
        {
            InitializeComponents();
        }
        
        void Start()
        {
            SetupInitialState();
        }
        
        void Update()
        {
            UpdateVisualFeedback();
        }
        
        /// <summary>
        /// 初始化组件引用
        /// </summary>
        private void InitializeComponents()
        {
            // 获取切割系统管理器
            if (cuttingSystemManager == null)
                cuttingSystemManager = GetComponent<SampleCuttingSystemManager>();
                
            // 获取仓库管理器
            if (warehouseManager == null)
                warehouseManager = FindFirstObjectByType<WarehouseManager>();
                
            // 获取地层分析器
            layerAnalyzer = GetComponent<SampleLayerAnalyzer>();
            if (layerAnalyzer == null)
                layerAnalyzer = gameObject.AddComponent<SampleLayerAnalyzer>();
                
            // 设置拖拽区域
            if (dropZone == null)
                dropZone = GetComponent<RectTransform>();
        }
        
        /// <summary>
        /// 设置初始状态
        /// </summary>
        private void SetupInitialState()
        {
            targetColor = normalColor;
            
            if (dropZoneBackground != null)
                dropZoneBackground.color = normalColor;
                
            UpdateInstructionText("将多层样本从仓库拖拽到此处");
        }
        
        /// <summary>
        /// 更新视觉反馈
        /// </summary>
        private void UpdateVisualFeedback()
        {
            if (dropZoneBackground != null)
            {
                dropZoneBackground.color = Color.Lerp(
                    dropZoneBackground.color, 
                    targetColor, 
                    colorTransitionSpeed * Time.deltaTime
                );
            }
        }
        
        /// <summary>
        /// 处理拖拽放置
        /// </summary>
        public void OnDrop(PointerEventData eventData)
        {
            Debug.Log("检测到拖拽放置事件");
            
            // 获取拖拽的对象
            GameObject draggedObject = eventData.pointerDrag;
            if (draggedObject == null)
            {
                Debug.LogWarning("拖拽对象为空");
                HandleDropResult(false, "拖拽对象无效");
                return;
            }
            
            // 验证拖拽对象
            var sampleData = ValidateDraggedObject(draggedObject);
            if (sampleData == null)
            {
                HandleDropResult(false, "无效的样本对象");
                return;
            }
            
            // 尝试开始切割
            bool success = StartCuttingProcess(sampleData);
            HandleDropResult(success, success ? "样本加载成功" : "无法开始切割");
        }
        
        /// <summary>
        /// 验证拖拽对象
        /// </summary>
        private GeometricSampleReconstructor.ReconstructedSample ValidateDraggedObject(GameObject draggedObject)
        {
            // 检查对象是否来自仓库系统
            var warehouseItem = draggedObject.GetComponent<WarehouseItemSlot>();
            if (warehouseItem == null)
            {
                Debug.LogWarning("拖拽对象不是仓库物品");
                return null;
            }
            
            // 获取样本数据
            var sampleItem = draggedObject.GetComponent<SampleItem>();
            if (sampleItem == null)
            {
                Debug.LogWarning("拖拽对象没有样本数据");
                return null;
            }
            
            // 检查是否有重建样本数据
            var geometricSample = draggedObject.GetComponent<GeometricSampleInfo>();
            if (geometricSample == null)
            {
                Debug.LogWarning("拖拽对象没有几何样本组件");
                return null;
            }
            
            var reconstructedSample = geometricSample.GetSampleData();
            if (reconstructedSample == null)
            {
                Debug.LogWarning("无法获取重建样本数据");
                return null;
            }
            
            // 验证样本是否可以切割
            if (onlyAcceptMultiLayerSamples)
            {
                if (!layerAnalyzer.CanSampleBeCut(reconstructedSample))
                {
                    Debug.LogWarning("样本不需要切割（单层或无效）");
                    return null;
                }
                
                var sampleInfo = layerAnalyzer.GetSampleInfo(reconstructedSample);
                if (sampleInfo.layerCount < minimumLayerCount)
                {
                    Debug.LogWarning($"样本地层数量不足: {sampleInfo.layerCount} < {minimumLayerCount}");
                    return null;
                }
            }
            
            Debug.Log($"验证通过: 样本ID={reconstructedSample.sampleID}, 地层数={reconstructedSample.layerSegments?.Length}");
            return reconstructedSample;
        }
        
        /// <summary>
        /// 开始切割流程
        /// </summary>
        private bool StartCuttingProcess(GeometricSampleReconstructor.ReconstructedSample sampleData)
        {
            if (cuttingSystemManager == null)
            {
                Debug.LogError("切割系统管理器未找到");
                return false;
            }
            
            // 检查系统状态
            var systemState = cuttingSystemManager.GetSystemState();
            if (systemState.isOccupied)
            {
                Debug.LogWarning("切割台当前被占用");
                return false;
            }
            
            // 从仓库中移除样本（移动到切割台）
            bool removed = RemoveSampleFromWarehouse(sampleData);
            if (!removed)
            {
                Debug.LogWarning("无法从仓库移除样本");
                return false;
            }
            
            // 开始切割
            return cuttingSystemManager.StartCuttingSample(sampleData);
        }
        
        /// <summary>
        /// 从仓库移除样本
        /// </summary>
        private bool RemoveSampleFromWarehouse(GeometricSampleReconstructor.ReconstructedSample sampleData)
        {
            if (warehouseManager == null)
            {
                Debug.LogWarning("仓库管理器未找到，跳过移除操作");
                return true; // 假设成功，继续流程
            }
            
            // TODO: 这里需要根据实际的仓库系统API来实现
            // 示例代码：
            // return warehouseManager.RemoveItem(sampleData.sampleID);
            
            Debug.Log($"从仓库移除样本: {sampleData.sampleID}");
            return true;
        }
        
        /// <summary>
        /// 处理放置结果
        /// </summary>
        private void HandleDropResult(bool success, string message)
        {
            if (success)
            {
                targetColor = highlightColor;
                UpdateInstructionText("样本加载成功，准备切割");
                
                // 延迟恢复正常状态
                Invoke(nameof(ResetToNormalState), 2f);
            }
            else
            {
                targetColor = invalidColor;
                UpdateInstructionText(message);
                
                // 延迟恢复正常状态
                Invoke(nameof(ResetToNormalState), 3f);
            }
            
            // 清理拖拽状态
            isDragOver = false;
            isValidDrag = false;
            currentDraggedItem = null;
            draggedSample = null;
        }
        
        /// <summary>
        /// 恢复正常状态
        /// </summary>
        private void ResetToNormalState()
        {
            targetColor = normalColor;
            UpdateInstructionText("将多层样本从仓库拖拽到此处");
        }
        
        /// <summary>
        /// 更新说明文字
        /// </summary>
        private void UpdateInstructionText(string text)
        {
            if (instructionText != null)
                instructionText.text = text;
        }
        
        /// <summary>
        /// 处理拖拽进入
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (eventData.pointerDrag != null)
            {
                isDragOver = true;
                
                // 预验证拖拽对象
                var sampleData = ValidateDraggedObject(eventData.pointerDrag);
                isValidDrag = sampleData != null;
                
                if (isValidDrag)
                {
                    targetColor = highlightColor;
                    UpdateInstructionText("松开鼠标放置样本");
                    
                    // 显示样本信息预览
                    ShowSamplePreview(sampleData);
                }
                else
                {
                    targetColor = invalidColor;
                    UpdateInstructionText("无效样本：需要多层地质样本");
                }
            }
        }
        
        /// <summary>
        /// 处理拖拽离开
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (isDragOver)
            {
                isDragOver = false;
                isValidDrag = false;
                targetColor = normalColor;
                UpdateInstructionText("将多层样本从仓库拖拽到此处");
                
                // 隐藏预览
                HideSamplePreview();
            }
        }
        
        /// <summary>
        /// 处理拖拽中
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            // 这个方法主要用于接收拖拽事件，具体逻辑在OnPointerEnter/Exit中处理
        }
        
        /// <summary>
        /// 显示样本预览
        /// </summary>
        private void ShowSamplePreview(GeometricSampleReconstructor.ReconstructedSample sampleData)
        {
            if (layerAnalyzer != null)
            {
                var sampleInfo = layerAnalyzer.GetSampleInfo(sampleData);
                if (sampleInfo != null)
                {
                    string previewText = $"样本预览:\n地层数: {sampleInfo.layerCount}\n需要切割: {sampleInfo.estimatedCuts} 次";
                    UpdateInstructionText(previewText);
                }
            }
        }
        
        /// <summary>
        /// 隐藏样本预览
        /// </summary>
        private void HideSamplePreview()
        {
            // 预览隐藏逻辑
        }
        
        /// <summary>
        /// 检查系统可用性
        /// </summary>
        public bool IsSystemAvailable()
        {
            if (cuttingSystemManager == null)
                return false;
                
            var systemState = cuttingSystemManager.GetSystemState();
            return !systemState.isOccupied;
        }
        
        /// <summary>
        /// 获取支持的样本类型
        /// </summary>
        public string[] GetSupportedSampleTypes()
        {
            return acceptedSampleTypes.ToArray();
        }
        
        /// <summary>
        /// 设置样本筛选条件
        /// </summary>
        public void SetSampleFilter(bool onlyMultiLayer, int minLayerCount)
        {
            onlyAcceptMultiLayerSamples = onlyMultiLayer;
            minimumLayerCount = minLayerCount;
        }
        
        /// <summary>
        /// 获取拖拽区域统计信息
        /// </summary>
        public DropZoneStatistics GetStatistics()
        {
            return new DropZoneStatistics
            {
                isActive = gameObject.activeInHierarchy,
                isSystemAvailable = IsSystemAvailable(),
                minimumLayerRequirement = minimumLayerCount,
                onlyAcceptsMultiLayer = onlyAcceptMultiLayerSamples,
                supportedTypes = GetSupportedSampleTypes()
            };
        }
        
        [System.Serializable]
        public class DropZoneStatistics
        {
            public bool isActive;
            public bool isSystemAvailable;
            public int minimumLayerRequirement;
            public bool onlyAcceptsMultiLayer;
            public string[] supportedTypes;
        }
        
        /// <summary>
        /// Editor测试方法
        /// </summary>
        [ContextMenu("测试拖拽区域")]
        private void TestDropZone()
        {
            Debug.Log("=== 拖拽区域测试 ===");
            Debug.Log($"系统可用: {IsSystemAvailable()}");
            Debug.Log($"只接受多层样本: {onlyAcceptMultiLayerSamples}");
            Debug.Log($"最小地层数: {minimumLayerCount}");
            Debug.Log($"支持的样本类型: {string.Join(", ", acceptedSampleTypes)}");
            
            var stats = GetStatistics();
            Debug.Log($"区域状态: 激活={stats.isActive}, 系统可用={stats.isSystemAvailable}");
        }
        
        [ContextMenu("重置拖拽状态")]
        private void ResetDragState()
        {
            isDragOver = false;
            isValidDrag = false;
            currentDraggedItem = null;
            draggedSample = null;
            ResetToNormalState();
            Debug.Log("拖拽状态已重置");
        }
    }
}
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SampleCuttingSystem
{
    /// <summary>
    /// 样本拖拽处理器
    /// 处理从仓库到切割区域的拖拽操作
    /// </summary>
    public class SampleDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("拖拽设置")]
        [SerializeField] private bool enableDragging = true;
        [SerializeField] private float dragAlpha = 0.6f;
        
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Vector2 originalPosition;
        private Transform originalParent;
        private Canvas canvas;
        
        // 样本数据
        private SampleData sampleData;
        private GameObject dragPreview;
        
        void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            
            canvas = GetComponentInParent<Canvas>();
        }
        
        /// <summary>
        /// 设置样本数据
        /// </summary>
        public void SetSampleData(SampleData data)
        {
            sampleData = data;
        }
        
        /// <summary>
        /// 获取样本数据
        /// </summary>
        public SampleData GetSampleData()
        {
            return sampleData;
        }
        
        /// <summary>
        /// 检查拖拽是否启用
        /// </summary>
        public bool IsDraggingEnabled()
        {
            return enableDragging;
        }
        
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!enableDragging)
                return;
                
            Debug.Log($"开始拖拽样本: {sampleData?.name ?? "未知样本"}");
            
            // 记录原始状态
            originalPosition = rectTransform.anchoredPosition;
            originalParent = transform.parent;
            
            // 设置拖拽状态
            canvasGroup.alpha = dragAlpha;
            canvasGroup.blocksRaycasts = false;
            
            // 移动到Canvas顶层以避免被遮挡
            transform.SetParent(canvas.transform, true);
            transform.SetAsLastSibling();
            
            // 创建拖拽预览
            CreateDragPreview();
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            if (!enableDragging)
                return;
                
            // 更新位置跟随鼠标
            rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
            
            // 更新预览位置
            if (dragPreview != null)
            {
                dragPreview.transform.position = transform.position;
            }
        }
        
        public void OnEndDrag(PointerEventData eventData)
        {
            if (!enableDragging)
                return;
                
            Debug.Log("结束拖拽样本");
            
            // 检查是否拖拽到有效的投放区域
            bool dropSuccessful = CheckDropTarget(eventData);
            
            if (!dropSuccessful)
            {
                // 拖拽失败，恢复原位
                RestoreOriginalState();
            }
            
            // 清理拖拽状态
            CleanupDragState();
        }
        
        /// <summary>
        /// 创建拖拽预览
        /// </summary>
        private void CreateDragPreview()
        {
            if (dragPreview != null)
            {
                Destroy(dragPreview);
            }
            
            // 创建预览对象
            dragPreview = new GameObject("DragPreview");
            dragPreview.transform.SetParent(canvas.transform, false);
            
            // 复制当前对象的外观
            var previewRect = dragPreview.AddComponent<RectTransform>();
            previewRect.sizeDelta = rectTransform.sizeDelta;
            previewRect.position = rectTransform.position;
            
            var previewImage = dragPreview.AddComponent<Image>();
            var originalImage = GetComponent<Image>();
            if (originalImage != null)
            {
                previewImage.sprite = originalImage.sprite;
                previewImage.color = new Color(originalImage.color.r, originalImage.color.g, originalImage.color.b, 0.5f);
            }
            
            // 添加样本信息文本
            if (sampleData != null)
            {
                CreatePreviewText(dragPreview);
            }
        }
        
        /// <summary>
        /// 创建预览文本
        /// </summary>
        private void CreatePreviewText(GameObject parent)
        {
            GameObject textObj = new GameObject("PreviewText");
            textObj.transform.SetParent(parent.transform, false);
            
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            var text = textObj.AddComponent<Text>();
            text.text = sampleData.name;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 12;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
        }
        
        /// <summary>
        /// 检查投放目标
        /// </summary>
        private bool CheckDropTarget(PointerEventData eventData)
        {
            // 使用射线检测查找投放目标
            var raycastResults = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, raycastResults);
            
            foreach (var result in raycastResults)
            {
                // 查找切割区域
                var dropZone = result.gameObject.GetComponent<SampleDropZone>();
                if (dropZone != null)
                {
                    Debug.Log($"找到投放区域: {result.gameObject.name}");
                    return dropZone.AcceptSample(this);
                }
            }
            
            Debug.Log("未找到有效的投放区域");
            return false;
        }
        
        /// <summary>
        /// 恢复原始状态
        /// </summary>
        public void RestoreOriginalState()
        {
            Debug.Log("恢复样本到原始位置");
            
            // 恢复父对象和位置
            transform.SetParent(originalParent, true);
            rectTransform.anchoredPosition = originalPosition;
            
            // 恢复透明度和射线检测
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }
        
        /// <summary>
        /// 清理拖拽状态
        /// </summary>
        private void CleanupDragState()
        {
            // 销毁预览对象
            if (dragPreview != null)
            {
                Destroy(dragPreview);
                dragPreview = null;
            }
            
            // 恢复透明度
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }
        
        /// <summary>
        /// 启用/禁用拖拽
        /// </summary>
        public void SetDraggingEnabled(bool enabled)
        {
            enableDragging = enabled;
        }
        
        /// <summary>
        /// 检查是否为多层样本（可切割）
        /// </summary>
        public bool IsMultiLayerSample()
        {
            if (sampleData == null)
                return false;

            // 使用层数来判断是否为多层样本
            return sampleData.layerCount > 1;
        }
    }
}

/// <summary>
/// 样本数据结构（简化版）
/// </summary>
[System.Serializable]
public class SampleData
{
    public string name;
    public string description;
    public int layerCount;
    public UnityEngine.GameObject samplePrefab;

    public SampleData(string sampleName, string desc = "", int layers = 1)
    {
        name = sampleName;
        description = desc;
        layerCount = layers;
    }
}
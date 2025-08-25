using UnityEngine;
using UnityEngine.UI;

namespace SampleCuttingSystem
{
    /// <summary>
    /// 仓库样本增强器
    /// 为仓库中的样本添加拖拽功能
    /// </summary>
    public class WarehouseSampleEnhancer : MonoBehaviour
    {
        /// <summary>
        /// 为仓库中的样本添加拖拽功能
        /// </summary>
        public static void EnhanceWarehouseSamples(Transform warehousePanel)
        {
            if (warehousePanel == null)
            {
                Debug.LogError("仓库面板为空，无法增强样本");
                return;
            }
            
            Debug.Log("开始为仓库样本添加拖拽功能");
            
            // 查找所有样本项目
            var sampleItems = FindSampleItems(warehousePanel);
            
            int enhancedCount = 0;
            foreach (var item in sampleItems)
            {
                if (EnhanceSampleItem(item))
                {
                    enhancedCount++;
                }
            }
            
            Debug.Log($"成功为 {enhancedCount} 个样本添加了拖拽功能");
        }
        
        /// <summary>
        /// 查找样本项目
        /// </summary>
        private static GameObject[] FindSampleItems(Transform warehousePanel)
        {
            var items = new System.Collections.Generic.List<GameObject>();
            
            // 递归查找所有可能的样本项目
            FindSampleItemsRecursive(warehousePanel, items);
            
            return items.ToArray();
        }
        
        /// <summary>
        /// 递归查找样本项目
        /// </summary>
        private static void FindSampleItemsRecursive(Transform parent, System.Collections.Generic.List<GameObject> items)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                
                // 检查是否为样本项目
                if (IsSampleItem(child.gameObject))
                {
                    items.Add(child.gameObject);
                }
                
                // 递归查找子对象
                FindSampleItemsRecursive(child, items);
            }
        }
        
        /// <summary>
        /// 判断是否为样本项目
        /// </summary>
        private static bool IsSampleItem(GameObject obj)
        {
            // 检查对象名称
            if (obj.name.Contains("样本") || obj.name.Contains("Sample") || 
                obj.name.Contains("钻孔") || obj.name.Contains("Drill"))
            {
                return true;
            }
            
            // 检查是否有Image组件且有Sprite（样本图标）
            var image = obj.GetComponent<Image>();
            if (image != null && image.sprite != null)
            {
                return true;
            }
            
            // 检查是否有Button组件（可点击的样本）
            var button = obj.GetComponent<Button>();
            if (button != null)
            {
                // 进一步检查是否为样本按钮
                var text = obj.GetComponentInChildren<Text>();
                if (text != null && (text.text.Contains("样本") || text.text.Contains("钻孔")))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 增强单个样本项目
        /// </summary>
        private static bool EnhanceSampleItem(GameObject item)
        {
            if (item == null)
                return false;
                
            // 检查是否已经有拖拽组件
            var existingDragHandler = item.GetComponent<SampleDragHandler>();
            if (existingDragHandler != null)
            {
                Debug.Log($"样本 {item.name} 已有拖拽功能，跳过");
                return false;
            }
            
            try
            {
                // 添加拖拽处理器
                var dragHandler = item.AddComponent<SampleDragHandler>();
                
                // 设置样本数据
                var sampleData = ExtractSampleData(item);
                dragHandler.SetSampleData(sampleData);
                
                Debug.Log($"为样本 {item.name} 添加拖拽功能: {sampleData.name}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"为样本 {item.name} 添加拖拽功能失败: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 提取样本数据
        /// </summary>
        private static SampleData ExtractSampleData(GameObject item)
        {
            // 尝试从文本组件获取名称
            string sampleName = "未知样本";
            string description = "";
            int layerCount = 1;
            
            var text = item.GetComponentInChildren<Text>();
            if (text != null && !string.IsNullOrEmpty(text.text))
            {
                sampleName = text.text;
            }
            else
            {
                sampleName = item.name;
            }
            
            // 尝试判断层级数量
            if (sampleName.Contains("多层") || sampleName.Contains("钻孔"))
            {
                layerCount = EstimateLayerCount(sampleName);
                description = "可进行切割的多层地质样本";
            }
            else if (sampleName.Contains("单层"))
            {
                layerCount = 1;
                description = "单层样本，无需切割";
            }
            else
            {
                // 默认假设为多层样本
                layerCount = 2;
                description = "地质样本，可尝试切割";
            }
            
            return new SampleData(sampleName, description, layerCount);
        }
        
        /// <summary>
        /// 估算层级数量
        /// </summary>
        private static int EstimateLayerCount(string sampleName)
        {
            // 从名称中提取数字
            var numbers = System.Text.RegularExpressions.Regex.Matches(sampleName, @"\d+");
            
            foreach (System.Text.RegularExpressions.Match match in numbers)
            {
                if (int.TryParse(match.Value, out int number))
                {
                    if (number >= 2 && number <= 10) // 合理的层级范围
                    {
                        return number;
                    }
                }
            }
            
            // 默认返回2层
            return 2;
        }
        
        /// <summary>
        /// 移除样本的拖拽功能（用于清理）
        /// </summary>
        public static void RemoveDragFunctionality(Transform warehousePanel)
        {
            if (warehousePanel == null)
                return;
                
            var dragHandlers = warehousePanel.GetComponentsInChildren<SampleDragHandler>();
            
            int removedCount = 0;
            foreach (var handler in dragHandlers)
            {
                if (handler != null)
                {
                    DestroyImmediate(handler);
                    removedCount++;
                }
            }
            
            Debug.Log($"移除了 {removedCount} 个样本的拖拽功能");
        }
    }
}
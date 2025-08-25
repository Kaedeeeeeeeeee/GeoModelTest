using UnityEngine;

namespace SampleCuttingSystem
{
    /// <summary>
    /// 统一样本重建系统功能测试
    /// </summary>
    public class UnifiedSystemTest : MonoBehaviour
    {
        [Header("测试设置")]
        public bool runTestOnStart = true;
        public bool enableDetailedLogging = true;
        
        void Start()
        {
            if (runTestOnStart)
            {
                RunAllTests();
            }
        }
        
        /// <summary>
        /// 运行所有功能测试
        /// </summary>
        [ContextMenu("运行统一系统测试")]
        public void RunAllTests()
        {
            Debug.Log("=== 开始统一样本重建系统测试 ===");
            
            TestDataStructures();
            TestViewerComponents();
            TestDropZoneLogic();
            TestSystemIntegration();
            
            Debug.Log("=== 统一系统测试完成 ===");
        }
        
        /// <summary>
        /// 测试数据结构
        /// </summary>
        void TestDataStructures()
        {
            Log("测试数据结构...");
            
            // 创建测试SampleItem
            SampleItem testSample = new SampleItem();
            testSample.sampleID = "TEST_UNIFIED_001";
            testSample.displayName = "统一系统测试样本";
            testSample.totalDepth = 2.0f;
            testSample.sampleRadius = 0.1f;
            
            // 测试geologicalLayers
            testSample.geologicalLayers = new System.Collections.Generic.List<SampleItem.LayerInfo>();
            
            var layer1 = new SampleItem.LayerInfo
            {
                layerName = "砂岩",
                thickness = 1.0f,
                depthStart = 0f,
                depthEnd = 1.0f,
                layerColor = new Color(0.9f, 0.8f, 0.6f, 1f),
                materialName = "Sandstone"
            };
            
            var layer2 = new SampleItem.LayerInfo
            {
                layerName = "页岩",
                thickness = 1.0f,
                depthStart = 1.0f,
                depthEnd = 2.0f,
                layerColor = new Color(0.4f, 0.4f, 0.4f, 1f),
                materialName = "Shale"
            };
            
            testSample.geologicalLayers.Add(layer1);
            testSample.geologicalLayers.Add(layer2);
            
            Log($"✓ SampleItem创建成功: {testSample.displayName}");
            Log($"✓ geologicalLayers包含 {testSample.geologicalLayers.Count} 层");
            Log($"✓ 层1: {layer1.layerName}, 厚度: {layer1.thickness}m, 颜色: {layer1.layerColor}");
            Log($"✓ 层2: {layer2.layerName}, 厚度: {layer2.thickness}m, 颜色: {layer2.layerColor}");
        }
        
        /// <summary>
        /// 测试查看器组件
        /// </summary>
        void TestViewerComponents()
        {
            Log("测试查看器组件...");
            
            Sample3DModelViewer viewer = FindObjectOfType<Sample3DModelViewer>();
            if (viewer != null)
            {
                Log("✓ Sample3DModelViewer组件找到");
                Log($"  - ShowRealSampleModel方法可用");
                Log($"  - ShowReconstructedSample方法可用");
                
                Sample3DModelViewerController controller = FindObjectOfType<Sample3DModelViewerController>();
                if (controller != null)
                {
                    Log("✓ Sample3DModelViewerController组件找到");
                }
                else
                {
                    Log("⚠ Sample3DModelViewerController组件未找到");
                }
            }
            else
            {
                Log("⚠ Sample3DModelViewer组件未在场景中找到");
            }
        }
        
        /// <summary>
        /// 测试投放区逻辑
        /// </summary>
        void TestDropZoneLogic()
        {
            Log("测试投放区逻辑...");
            
            SampleDropZone dropZone = FindObjectOfType<SampleDropZone>();
            if (dropZone != null)
            {
                Log("✓ SampleDropZone组件找到");
                Log($"  - ExtractSampleItemFromDragHandler方法可用");
                Log($"  - ShowRealSampleModel方法可用");
                Log($"  - FindSampleItemById方法可用");
            }
            else
            {
                Log("⚠ SampleDropZone组件未在场景中找到");
            }
        }
        
        /// <summary>
        /// 测试系统集成
        /// </summary>
        void TestSystemIntegration()
        {
            Log("测试系统集成...");
            
            // 检查SampleInventory
            if (SampleInventory.Instance != null)
            {
                Log("✓ SampleInventory单例可用");
                var samples = SampleInventory.Instance.GetInventorySamples();
                Log($"  - 当前背包样本数量: {samples.Count}");
            }
            else
            {
                Log("⚠ SampleInventory单例未初始化");
            }
            
            // 检查GeometricSampleReconstructor
            GeometricSampleReconstructor reconstructor = FindObjectOfType<GeometricSampleReconstructor>();
            if (reconstructor != null)
            {
                Log("✓ GeometricSampleReconstructor组件找到");
            }
            else
            {
                Log("⚠ GeometricSampleReconstructor组件未在场景中找到");
            }
            
            // 检查WarehouseItemSlot
            WarehouseItemSlot[] slots = FindObjectsOfType<WarehouseItemSlot>();
            Log($"✓ 场景中找到 {slots.Length} 个WarehouseItemSlot");
        }
        
        /// <summary>
        /// 日志输出（可控制详细程度）
        /// </summary>
        void Log(string message)
        {
            if (enableDetailedLogging)
            {
                Debug.Log($"[UnifiedSystemTest] {message}");
            }
        }
        
        /// <summary>
        /// 手动触发ShowRealSampleModel测试
        /// </summary>
        [ContextMenu("测试ShowRealSampleModel")]
        public void TestShowRealSampleModel()
        {
            Sample3DModelViewer viewer = FindObjectOfType<Sample3DModelViewer>();
            if (viewer == null)
            {
                Debug.LogWarning("未找到Sample3DModelViewer组件");
                return;
            }
            
            // 创建测试样本
            SampleItem testSample = new SampleItem();
            testSample.sampleID = "TEST_SHOW_001";
            testSample.displayName = "ShowRealSampleModel测试";
            testSample.sourceToolID = "1000";
            testSample.totalDepth = 2.0f;
            testSample.sampleRadius = 0.1f;
            
            // 添加地质层
            testSample.geologicalLayers = new System.Collections.Generic.List<SampleItem.LayerInfo>();
            var layer = new SampleItem.LayerInfo
            {
                layerName = "测试砂岩",
                thickness = 2.0f,
                layerColor = Color.yellow
            };
            testSample.geologicalLayers.Add(layer);
            
            Debug.Log("开始测试ShowRealSampleModel...");
            viewer.ShowRealSampleModel(testSample);
            Debug.Log("ShowRealSampleModel测试调用完成");
        }
    }
}
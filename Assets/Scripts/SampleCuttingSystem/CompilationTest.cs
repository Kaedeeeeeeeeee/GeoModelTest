using UnityEngine;

namespace SampleCuttingSystem
{
    /// <summary>
    /// 编译测试脚本
    /// 验证所有组件和引用是否正确
    /// </summary>
    public class CompilationTest : MonoBehaviour
    {
        [ContextMenu("测试组件引用")]
        public void TestComponentReferences()
        {
            Debug.Log("开始测试组件引用...");
            
            // 测试所有主要组件
            TestComponent<SampleCuttingSystemManager>("系统管理器");
            TestComponent<SampleCuttingGame>("切割游戏");
            TestComponent<SampleLayerAnalyzer>("地层分析器");
            TestComponent<LayerDatabaseMapper>("数据库映射器");
            TestComponent<CuttingStationUI>("UI管理器");
            TestComponent<SingleLayerSampleGenerator>("样本生成器");
            TestComponent<WarehouseIntegration>("仓库集成");
            TestComponent<SampleCuttingSystemInitializer>("系统初始化器");
            
            Debug.Log("组件引用测试完成！");
        }
        
        private void TestComponent<T>(string componentName) where T : Component
        {
            try
            {
                var component = gameObject.GetComponent<T>();
                if (component == null)
                {
                    gameObject.AddComponent<T>();
                    Debug.Log($"✓ {componentName} 组件可以正常添加");
                }
                else
                {
                    Debug.Log($"✓ {componentName} 组件已存在");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ {componentName} 组件测试失败: {e.Message}");
            }
        }
        
        [ContextMenu("测试数据结构")]
        public void TestDataStructures()
        {
            Debug.Log("开始测试数据结构...");
            
            try
            {
                // 测试SingleLayerSample
                var singleSample = new SingleLayerSample();
                singleSample.sampleID = "test_sample";
                singleSample.layerName = "测试地层";
                Debug.Log($"✓ SingleLayerSample 创建成功: {singleSample.sampleID}");
                
                // 测试MineralComposition
                var mineral = new MineralComposition();
                mineral.mineralName = "测试矿物";
                mineral.percentage = 0.5f;
                Debug.Log($"✓ MineralComposition 创建成功: {mineral.mineralName}");
                
                // 测试其他数据结构
                var cuttingRecord = new CuttingRecord();
                var stationState = new CuttingStationState();
                var generationConfig = SampleGenerationConfig.GetDefault();
                
                Debug.Log("✓ 所有数据结构测试通过");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ 数据结构测试失败: {e.Message}");
            }
        }
        
        [ContextMenu("测试外部依赖")]
        public void TestExternalDependencies()
        {
            Debug.Log("开始测试外部依赖...");
            
            try
            {
                // 测试GeometricSampleReconstructor依赖
                var reconstructor = FindFirstObjectByType<GeometricSampleReconstructor>();
                Debug.Log($"GeometricSampleReconstructor: {(reconstructor != null ? "找到" : "未找到")}");
                
                // 测试GeometricSampleInfo依赖
                var sampleInfo = FindFirstObjectByType<GeometricSampleInfo>();
                Debug.Log($"GeometricSampleInfo: {(sampleInfo != null ? "找到" : "未找到")}");
                
                // 测试SampleCollector依赖
                var collector = FindFirstObjectByType<SampleCollector>();
                Debug.Log($"SampleCollector: {(collector != null ? "找到" : "未找到")}");
                
                // 测试WarehouseManager依赖
                var warehouse = FindFirstObjectByType<WarehouseManager>();
                Debug.Log($"WarehouseManager: {(warehouse != null ? "找到" : "未找到")}");
                
                Debug.Log("外部依赖测试完成");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"外部依赖测试失败: {e.Message}");
            }
        }
        
        [ContextMenu("清理测试组件")]
        public void CleanupTestComponents()
        {
            var components = GetComponents<Component>();
            int removedCount = 0;
            
            foreach (var component in components)
            {
                if (component != this && component != transform)
                {
                    DestroyImmediate(component);
                    removedCount++;
                }
            }
            
            Debug.Log($"清理了 {removedCount} 个测试组件");
        }
    }
}
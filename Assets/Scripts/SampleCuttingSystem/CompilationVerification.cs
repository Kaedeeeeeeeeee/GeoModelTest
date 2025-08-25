using UnityEngine;

namespace SampleCuttingSystem
{
    /// <summary>
    /// 编译验证脚本
    /// 验证所有核心组件是否可以正常编译和实例化
    /// </summary>
    public class CompilationVerification : MonoBehaviour
    {
        [ContextMenu("验证编译状态")]
        public void VerifyCompilation()
        {
            Debug.Log("=== 开始编译验证 ===");
            
            try
            {
                // 测试核心数据结构
                var sampleData = new SampleData("测试样本", "编译验证", 2);
                Debug.Log("✅ SampleData 编译成功");
                
                // 测试组件创建
                var testObj = new GameObject("CompilationTest");
                
                var dragHandler = testObj.AddComponent<SampleDragHandler>();
                dragHandler.SetSampleData(sampleData);
                Debug.Log("✅ SampleDragHandler 编译成功");
                
                var dropZone = testObj.AddComponent<SampleDropZone>();
                Debug.Log("✅ SampleDropZone 编译成功");
                
                var cuttingGame = testObj.AddComponent<SampleCuttingGame>();
                Debug.Log("✅ SampleCuttingGame 编译成功");
                
                var canvasProtector = testObj.AddComponent<CanvasProtector>();
                Debug.Log("✅ CanvasProtector 编译成功");
                
                // 清理测试对象
                DestroyImmediate(testObj);
                
                Debug.Log("🎉 所有核心组件编译验证成功！");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ 编译验证失败: {e.Message}");
            }
        }
        
        void Start()
        {
            // 延迟执行验证，确保所有脚本加载完成
            Invoke(nameof(VerifyCompilation), 1f);
        }
    }
}
using UnityEngine;
using UnityEngine.InputSystem;

namespace SampleCuttingSystem
{
    /// <summary>
    /// 系统完整性测试脚本
    /// 验证修复后的系统是否正常工作
    /// </summary>
    public class SystemIntegrityTest : MonoBehaviour
    {
        [Header("测试组件")]
        public Sample3DModelViewer modelViewer;
        public Sample3DModelViewerController viewerController;
        
        void Start()
        {
            // 运行所有完整性测试
            RunIntegrityTests();
        }
        
        /// <summary>
        /// 运行完整性测试
        /// </summary>
        public void RunIntegrityTests()
        {
            Debug.Log("=== 开始系统完整性测试 ===");
            
            // 1. 测试Input System兼容性
            TestInputSystemCompatibility();
            
            // 2. 测试3D模型查看器
            Test3DModelViewer();
            
            // 3. 测试模型控制器
            TestModelController();
            
            Debug.Log("=== 系统完整性测试完成 ===");
        }
        
        /// <summary>
        /// 测试Input System兼容性
        /// </summary>
        void TestInputSystemCompatibility()
        {
            Debug.Log("[测试] Input System兼容性检查");
            
            try
            {
                var mouse = Mouse.current;
                if (mouse != null)
                {
                    Debug.Log("✓ 新Input System鼠标检测正常");
                    
                    // 测试鼠标位置读取
                    Vector2 mousePos = mouse.position.ReadValue();
                    Debug.Log($"✓ 鼠标位置读取正常: {mousePos}");
                    
                    // 测试滚轮读取
                    Vector2 scrollDelta = mouse.scroll.ReadValue();
                    Debug.Log($"✓ 滚轮读取正常: {scrollDelta}");
                }
                else
                {
                    Debug.LogWarning("⚠ 未检测到鼠标设备");
                }
                
                var keyboard = Keyboard.current;
                if (keyboard != null)
                {
                    Debug.Log("✓ 新Input System键盘检测正常");
                }
                else
                {
                    Debug.LogWarning("⚠ 未检测到键盘设备");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Input System兼容性测试失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 测试3D模型查看器
        /// </summary>
        void Test3DModelViewer()
        {
            Debug.Log("[测试] 3D模型查看器功能");
            
            if (modelViewer == null)
            {
                // 尝试查找组件
                modelViewer = FindObjectOfType<Sample3DModelViewer>();
            }
            
            if (modelViewer != null)
            {
                Debug.Log("✓ Sample3DModelViewer组件已找到");
                
                // 测试基础方法
                try
                {
                    modelViewer.ResetView();
                    Debug.Log("✓ ResetView方法调用正常");
                    
                    modelViewer.ToggleAutoRotation();
                    Debug.Log("✓ ToggleAutoRotation方法调用正常");
                    
                    modelViewer.SetMouseOverArea(true);
                    modelViewer.SetMouseOverArea(false);
                    Debug.Log("✓ SetMouseOverArea方法调用正常");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"✗ 3D模型查看器方法测试失败: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning("⚠ 未找到Sample3DModelViewer组件");
            }
        }
        
        /// <summary>
        /// 测试模型控制器
        /// </summary>
        void TestModelController()
        {
            Debug.Log("[测试] 3D模型控制器功能");
            
            if (viewerController == null)
            {
                // 尝试查找组件
                viewerController = FindObjectOfType<Sample3DModelViewerController>();
            }
            
            if (viewerController != null)
            {
                Debug.Log("✓ Sample3DModelViewerController组件已找到");
                
                // 测试操作说明显示
                try
                {
                    viewerController.ShowInstructions();
                    Debug.Log("✓ ShowInstructions方法调用正常");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"✗ 模型控制器方法测试失败: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning("⚠ 未找到Sample3DModelViewerController组件");
            }
        }
        
        /// <summary>
        /// 手动触发测试（用于编辑器调试）
        /// </summary>
        [ContextMenu("运行完整性测试")]
        public void ManualRunTests()
        {
            RunIntegrityTests();
        }
    }
}
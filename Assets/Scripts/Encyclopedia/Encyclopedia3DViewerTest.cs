using UnityEngine;
using UnityEngine.UI;
using SampleCuttingSystem;

namespace Encyclopedia
{
    /// <summary>
    /// 图鉴3D查看器集成测试
    /// 验证Sample3DModelViewer是否正确集成到图鉴系统
    /// </summary>
    public class Encyclopedia3DViewerTest : MonoBehaviour
    {
        [Header("测试设置")]
        [SerializeField] private bool autoTest = true;
        [SerializeField] private float testDelay = 3f;

        private void Start()
        {
            if (autoTest)
            {
                Invoke(nameof(RunIntegrationTest), testDelay);
            }
        }

        [ContextMenu("运行集成测试")]
        public void RunIntegrationTest()
        {
            Debug.Log("=== 图鉴3D查看器集成测试开始 ===");
            TestViewerIntegration();
        }

        private void TestViewerIntegration()
        {
            // 查找图鉴UI
            EncyclopediaUI encyclopediaUI = FindObjectOfType<EncyclopediaUI>();
            if (encyclopediaUI == null)
            {
                Debug.LogError("❌ 未找到EncyclopediaUI，请先打开图鉴");
                return;
            }

            Debug.Log("✓ 找到EncyclopediaUI");

            // 检查Sample3DModelViewer是否正确集成
            Sample3DModelViewer viewer = FindObjectOfType<Sample3DModelViewer>();
            if (viewer != null)
            {
                Debug.Log("✓ 找到Sample3DModelViewer");

                // 检查viewer的基本设置
                if (viewer.rawImage != null)
                {
                    Debug.Log("✓ RawImage正确设置");
                }
                else
                {
                    Debug.LogWarning("⚠️ RawImage未设置");
                }

                // 创建测试模型进行显示测试
                CreateTestModel(viewer);
            }
            else
            {
                Debug.LogWarning("⚠️ 未找到Sample3DModelViewer，可能需要先打开图鉴详情页面");
            }

            Debug.Log("=== 图鉴3D查看器集成测试完成 ===");
        }

        private void CreateTestModel(Sample3DModelViewer viewer)
        {
            Debug.Log("🎯 创建测试模型");

            // 创建简单的测试立方体
            GameObject testCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            testCube.name = "Encyclopedia3DTest";

            // 设置材质
            var renderer = testCube.GetComponent<Renderer>();
            var material = new Material(Shader.Find("Standard"));
            material.color = Color.red;
            material.SetFloat("_Metallic", 0.2f);
            material.SetFloat("_Glossiness", 0.8f);
            renderer.material = material;

            // 移除碰撞器
            DestroyImmediate(testCube.GetComponent<Collider>());

            // 显示测试模型
            viewer.ShowSampleModel(testCube);

            Debug.Log("✅ 测试模型已发送到Sample3DModelViewer");

            // 延迟清理
            Destroy(testCube, 10f);
        }

        /// <summary>
        /// 测试图鉴系统的完整工作流程
        /// </summary>
        [ContextMenu("测试完整工作流程")]
        public void TestCompleteWorkflow()
        {
            Debug.Log("=== 图鉴系统完整工作流程测试 ===");

            // 1. 查找EncyclopediaUI
            EncyclopediaUI encyclopediaUI = FindObjectOfType<EncyclopediaUI>();
            if (encyclopediaUI == null)
            {
                Debug.LogError("❌ 未找到EncyclopediaUI");
                return;
            }

            // 2. 检查图鉴管理器
            SimpleEncyclopediaManager manager = FindObjectOfType<SimpleEncyclopediaManager>();
            if (manager == null)
            {
                Debug.LogError("❌ 未找到SimpleEncyclopediaManager");
                return;
            }

            Debug.Log("✓ 图鉴系统组件完整");

            // 3. 检查Sample3DModelViewer是否正确集成
            Sample3DModelViewer[] viewers = FindObjectsOfType<Sample3DModelViewer>();
            if (viewers.Length > 0)
            {
                Debug.Log($"✓ 找到 {viewers.Length} 个Sample3DModelViewer组件");

                foreach (var viewer in viewers)
                {
                    if (viewer.rawImage != null)
                    {
                        Debug.Log($"✓ 查看器 {viewer.name} 的RawImage正确配置");
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ 查看器 {viewer.name} 的RawImage未配置");
                    }
                }
            }
            else
            {
                Debug.LogWarning("⚠️ 未找到Sample3DModelViewer组件");
            }

            Debug.Log("=== 完整工作流程测试完成 ===");
        }
    }
}
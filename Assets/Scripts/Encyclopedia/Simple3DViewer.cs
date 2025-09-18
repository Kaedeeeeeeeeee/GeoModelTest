using UnityEngine;
using UnityEngine.UI;

namespace Encyclopedia
{
    /// <summary>
    /// 简化版3D模型查看器
    /// 使用基础Camera渲染到RenderTexture的简单实现
    /// </summary>
    public class Simple3DViewer : MonoBehaviour
    {
        [Header("显示设置")]
        [SerializeField] private RawImage displayImage;
        [SerializeField] private int textureSize = 512;

        [Header("相机设置")]
        [SerializeField] private float cameraDistance = 2f;
        [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);

        [Header("旋转设置")]
        [SerializeField] private bool enableAutoRotation = true;
        [SerializeField] private float rotationSpeed = 30f; // 度/秒
        [SerializeField] private Vector3 rotationAxis = Vector3.up; // 默认绕Y轴旋转

        // 私有组件
        private Camera viewerCamera;
        private RenderTexture renderTexture;
        private GameObject modelContainer;
        private GameObject currentModel;
        private Light modelLight;

        private void Awake()
        {
            Initialize();
        }

        private void Start()
        {
            // 确保在Start中也进行初始化检查
            if (viewerCamera == null || renderTexture == null)
            {
                Initialize();
            }
        }

        private void Update()
        {
            // 自动旋转功能
            if (enableAutoRotation && currentModel != null)
            {
                float rotationAmount = rotationSpeed * Time.deltaTime;
                currentModel.transform.Rotate(rotationAxis, rotationAmount, Space.Self);
            }
        }

        private void Initialize()
        {
            Debug.Log("[Simple3DViewer] 开始初始化简化版3D查看器");
            Debug.Log($"[Simple3DViewer] GameObject: {gameObject.name}, 激活状态: {gameObject.activeSelf}");

            try
            {
                // 创建RenderTexture
                CreateRenderTexture();

                // 创建相机
                CreateCamera();

                // 创建模型容器
                CreateModelContainer();

                // 创建光源
                CreateLight();

                // 设置显示图像
                SetupDisplayImage();

                Debug.Log("[Simple3DViewer] 初始化完成");
                Debug.Log($"[Simple3DViewer] 最终状态:");
                Debug.Log($"  - viewerCamera: {viewerCamera != null}");
                Debug.Log($"  - renderTexture: {renderTexture != null}");
                Debug.Log($"  - modelContainer: {modelContainer != null}");
                Debug.Log($"  - displayImage: {displayImage != null}");
                Debug.Log($"  - modelLight: {modelLight != null}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Simple3DViewer] 初始化失败: {e.Message}");
                Debug.LogError($"[Simple3DViewer] 堆栈跟踪: {e.StackTrace}");
            }
        }

        private void CreateRenderTexture()
        {
            renderTexture = new RenderTexture(textureSize, textureSize, 16);
            renderTexture.name = "ModelViewerRenderTexture";
            renderTexture.Create();

            Debug.Log($"[Simple3DViewer] 创建RenderTexture: {textureSize}x{textureSize}");
        }

        private void CreateCamera()
        {
            GameObject cameraGO = new GameObject("ViewerCamera");
            cameraGO.transform.SetParent(transform);
            cameraGO.transform.localPosition = new Vector3(0, 0, -cameraDistance);
            cameraGO.transform.LookAt(transform);

            viewerCamera = cameraGO.AddComponent<Camera>();
            viewerCamera.targetTexture = renderTexture;
            viewerCamera.clearFlags = CameraClearFlags.SolidColor;
            viewerCamera.backgroundColor = backgroundColor;
            viewerCamera.fieldOfView = 60f;
            viewerCamera.nearClipPlane = 0.1f;
            viewerCamera.farClipPlane = 10f;
            viewerCamera.cullingMask = -1; // 渲染所有层级

            Debug.Log($"[Simple3DViewer] 创建相机: 位置={cameraGO.transform.localPosition}");
        }

        private void CreateModelContainer()
        {
            GameObject containerGO = new GameObject("ModelContainer");
            containerGO.transform.SetParent(transform);
            containerGO.transform.localPosition = Vector3.zero;
            containerGO.transform.localRotation = Quaternion.identity;

            modelContainer = containerGO;

            Debug.Log("[Simple3DViewer] 创建模型容器");
        }

        private void CreateLight()
        {
            GameObject lightGO = new GameObject("ModelLight");
            lightGO.transform.SetParent(viewerCamera.transform);
            lightGO.transform.localPosition = new Vector3(1, 1, 0);
            lightGO.transform.LookAt(modelContainer.transform);

            modelLight = lightGO.AddComponent<Light>();
            modelLight.type = LightType.Directional;
            modelLight.intensity = 1f;
            modelLight.color = Color.white;

            Debug.Log("[Simple3DViewer] 创建光源");
        }

        private void SetupDisplayImage()
        {
            if (displayImage == null)
            {
                // 查找子对象中的RawImage
                displayImage = GetComponentInChildren<RawImage>();
            }

            if (displayImage == null)
            {
                // 创建新的RawImage
                GameObject imageGO = new GameObject("ModelDisplay");
                imageGO.transform.SetParent(transform, false);

                RectTransform rect = imageGO.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                displayImage = imageGO.AddComponent<RawImage>();

                Debug.Log("[Simple3DViewer] 创建新的RawImage");
            }

            displayImage.texture = renderTexture;
            displayImage.gameObject.SetActive(true);

            Debug.Log($"[Simple3DViewer] 设置显示图像: texture={renderTexture != null}, active={displayImage.gameObject.activeSelf}");
        }

        /// <summary>
        /// 显示3D模型
        /// </summary>
        public void ShowModel(GameObject modelPrefab)
        {
            Debug.Log($"[Simple3DViewer] 开始显示模型: {modelPrefab?.name}");

            // 确保组件已初始化
            if (viewerCamera == null || renderTexture == null || modelContainer == null)
            {
                Debug.Log("[Simple3DViewer] 组件未初始化，重新初始化...");
                Initialize();
            }

            // 再次检查关键组件
            if (modelContainer == null)
            {
                Debug.LogError("[Simple3DViewer] modelContainer仍然为空，无法显示模型");
                return;
            }

            // 清除当前模型
            ClearModel();

            if (modelPrefab == null)
            {
                Debug.LogWarning("[Simple3DViewer] 模型预制体为空");
                return;
            }

            try
            {
                Debug.Log($"[Simple3DViewer] 组件状态检查:");
                Debug.Log($"  - modelContainer: {modelContainer != null}");
                Debug.Log($"  - viewerCamera: {viewerCamera != null}");
                Debug.Log($"  - renderTexture: {renderTexture != null}");
                Debug.Log($"  - displayImage: {displayImage != null}");

                // 实例化模型
                currentModel = Instantiate(modelPrefab, modelContainer.transform);
                currentModel.transform.localPosition = Vector3.zero;
                currentModel.transform.localRotation = Quaternion.identity;
                currentModel.transform.localScale = Vector3.one;

                Debug.Log($"[Simple3DViewer] 模型旋转设置: 启用={enableAutoRotation}, 速度={rotationSpeed}度/秒");

                Debug.Log($"[Simple3DViewer] 模型实例化成功: {currentModel.name}");
                Debug.Log($"  - 模型位置: {currentModel.transform.position}");
                Debug.Log($"  - 模型父级: {currentModel.transform.parent?.name}");

                // 调整模型到合适大小
                AdjustModelSize();

                // 添加基础材质（如果需要）
                ApplyBasicMaterial();

                // 检查相机和模型的相对位置
                if (viewerCamera != null && currentModel != null)
                {
                    Vector3 cameraPos = viewerCamera.transform.position;
                    Vector3 modelPos = currentModel.transform.position;
                    float distance = Vector3.Distance(cameraPos, modelPos);

                    Debug.Log($"[Simple3DViewer] 空间关系:");
                    Debug.Log($"  - 相机位置: {cameraPos}");
                    Debug.Log($"  - 模型位置: {modelPos}");
                    Debug.Log($"  - 距离: {distance:F3}");
                    Debug.Log($"  - 相机看向: {viewerCamera.transform.forward}");
                }

                // 渲染一帧
                if (viewerCamera != null)
                {
                    viewerCamera.Render();
                    Debug.Log("[Simple3DViewer] 强制渲染完成");
                }

                Debug.Log("[Simple3DViewer] 模型显示成功");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Simple3DViewer] 显示模型失败: {e.Message}");
                Debug.LogError($"[Simple3DViewer] 堆栈跟踪: {e.StackTrace}");
            }
        }

        /// <summary>
        /// 清除当前模型
        /// </summary>
        public void ClearModel()
        {
            if (currentModel != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(currentModel);
                }
                else
                {
                    DestroyImmediate(currentModel);
                }
                currentModel = null;

                Debug.Log("[Simple3DViewer] 已清除当前模型");
            }
        }

        /// <summary>
        /// 调整模型大小
        /// </summary>
        private void AdjustModelSize()
        {
            if (currentModel == null) return;

            Bounds bounds = CalculateBounds(currentModel);
            float maxSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);

            Debug.Log($"[Simple3DViewer] 模型原始大小: {bounds.size}, 最大尺寸: {maxSize}");

            // 如果模型太小，放大它
            if (maxSize < 0.1f)
            {
                float scaleFactor = 1f / maxSize;
                currentModel.transform.localScale = Vector3.one * scaleFactor;
                Debug.Log($"[Simple3DViewer] 模型放大 {scaleFactor:F2} 倍");
            }
            // 如果模型太大，缩小它
            else if (maxSize > 2f)
            {
                float scaleFactor = 1.5f / maxSize;
                currentModel.transform.localScale = Vector3.one * scaleFactor;
                Debug.Log($"[Simple3DViewer] 模型缩小 {scaleFactor:F2} 倍");
            }

            // 居中模型
            bounds = CalculateBounds(currentModel);
            Vector3 offset = bounds.center - currentModel.transform.position;
            currentModel.transform.position = currentModel.transform.position - offset;

            Debug.Log($"[Simple3DViewer] 模型居中偏移: {offset}");
        }

        /// <summary>
        /// 计算模型包围盒
        /// </summary>
        private Bounds CalculateBounds(GameObject obj)
        {
            Bounds bounds = new Bounds(obj.transform.position, Vector3.zero);
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }

        /// <summary>
        /// 应用基础材质
        /// </summary>
        private void ApplyBasicMaterial()
        {
            if (currentModel == null) return;

            Renderer[] renderers = currentModel.GetComponentsInChildren<Renderer>();

            foreach (var renderer in renderers)
            {
                if (renderer.materials.Length > 0)
                {
                    Material[] newMaterials = new Material[renderer.materials.Length];

                    for (int i = 0; i < renderer.materials.Length; i++)
                    {
                        var originalMaterial = renderer.materials[i];

                        if (originalMaterial != null)
                        {
                            // 使用原始材质
                            newMaterials[i] = originalMaterial;
                        }
                        else
                        {
                            // 创建基础材质
                            var basicMaterial = new Material(Shader.Find("Standard"));
                            basicMaterial.color = Color.white;
                            basicMaterial.SetFloat("_Metallic", 0.2f);
                            basicMaterial.SetFloat("_Glossiness", 0.5f);
                            newMaterials[i] = basicMaterial;

                            Debug.Log("[Simple3DViewer] 创建基础材质");
                        }
                    }

                    renderer.materials = newMaterials;
                }
            }
        }

        /// <summary>
        /// 测试显示立方体
        /// </summary>
        [ContextMenu("测试立方体")]
        public void TestCube()
        {
            GameObject testCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            testCube.name = "TestCube";

            // 设置红色材质
            var renderer = testCube.GetComponent<Renderer>();
            var material = new Material(Shader.Find("Standard"));
            material.color = Color.red;
            renderer.material = material;

            ShowModel(testCube);

            // 清理原始对象
            if (Application.isPlaying)
            {
                Destroy(testCube);
            }
            else
            {
                DestroyImmediate(testCube);
            }

            Debug.Log("[Simple3DViewer] 测试立方体已显示");
        }

        private void OnDestroy()
        {
            ClearModel();

            if (renderTexture != null)
            {
                renderTexture.Release();
                DestroyImmediate(renderTexture);
            }
        }

        /// <summary>
        /// 重新初始化（公开方法）
        /// </summary>
        public void Reinitialize()
        {
            Debug.Log("[Simple3DViewer] 执行重新初始化");

            // 清理现有资源
            if (renderTexture != null)
            {
                renderTexture.Release();
                DestroyImmediate(renderTexture);
                renderTexture = null;
            }

            // 重新初始化
            Initialize();
        }

        /// <summary>
        /// 设置自动旋转开关
        /// </summary>
        public void SetAutoRotation(bool enabled)
        {
            enableAutoRotation = enabled;
            Debug.Log($"[Simple3DViewer] 自动旋转设置为: {enabled}");
        }

        /// <summary>
        /// 设置旋转速度
        /// </summary>
        public void SetRotationSpeed(float speed)
        {
            rotationSpeed = speed;
            Debug.Log($"[Simple3DViewer] 旋转速度设置为: {speed}度/秒");
        }

        /// <summary>
        /// 设置旋转轴
        /// </summary>
        public void SetRotationAxis(Vector3 axis)
        {
            rotationAxis = axis.normalized;
            Debug.Log($"[Simple3DViewer] 旋转轴设置为: {rotationAxis}");
        }

        /// <summary>
        /// 重置模型旋转
        /// </summary>
        public void ResetModelRotation()
        {
            if (currentModel != null)
            {
                currentModel.transform.localRotation = Quaternion.identity;
                Debug.Log("[Simple3DViewer] 模型旋转已重置");
            }
        }

#if UNITY_EDITOR
        [ContextMenu("重新初始化")]
        public void ReinitializeFromMenu()
        {
            Reinitialize();
        }
#endif
    }
}
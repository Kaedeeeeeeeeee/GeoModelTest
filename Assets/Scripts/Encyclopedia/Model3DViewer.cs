using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace Encyclopedia
{
    /// <summary>
    /// 3D模型查看器
    /// 支持鼠标旋转、缩放和重置
    /// </summary>
    public class Model3DViewer : MonoBehaviour, IDragHandler, IScrollHandler
    {
        [Header("控制设置")]
        [SerializeField] private float rotationSpeed = 2f;
        [SerializeField] private float zoomSpeed = 0.5f;
        [SerializeField] private float minZoom = 0.5f;
        [SerializeField] private float maxZoom = 3f;
        [SerializeField] private bool invertY = false;

        [Header("相机设置")]
        [SerializeField] private Camera viewerCamera;
        [SerializeField] private Transform cameraRig;
        [SerializeField] private Vector3 defaultCameraPosition = new Vector3(0, 0, -2);
        [SerializeField] private Vector3 defaultCameraRotation = Vector3.zero;
        [SerializeField] private RenderTexture renderTexture;
        [SerializeField] private RawImage displayImage;

        [Header("模型容器")]
        [SerializeField] private Transform modelContainer;
        [SerializeField] private Light modelLight;

        [Header("UI控件")]
        [SerializeField] private Button resetButton;
        [SerializeField] private Slider zoomSlider;
        [SerializeField] private Text zoomText;

        // 私有变量
        private GameObject currentModel;
        private Vector3 lastMousePosition;
        private float currentZoom = 1f;
        private Vector3 currentRotation = Vector3.zero;
        private bool isInitialized = false;

        // 默认设置
        private Vector3 originalCameraPosition;
        private Vector3 originalCameraRotation;

        private void Awake()
        {
            InitializeViewer();
            isInitialized = true;
        }

        private void Start()
        {
            SetupUI();
        }

        /// <summary>
        /// 初始化查看器
        /// </summary>
        private void InitializeViewer()
        {
            Debug.Log("🔧 开始初始化Model3DViewer");

            // 如果没有指定相机，尝试找到子对象中的相机
            if (viewerCamera == null)
                viewerCamera = GetComponentInChildren<Camera>();

            // 如果仍然没有相机，创建一个新的相机
            if (viewerCamera == null)
            {
                Debug.Log("📷 创建新的相机组件");
                var cameraGO = new GameObject("ViewerCamera");
                cameraGO.transform.SetParent(transform);
                cameraGO.transform.localPosition = defaultCameraPosition;
                cameraGO.transform.localRotation = Quaternion.Euler(defaultCameraRotation);
                
                viewerCamera = cameraGO.AddComponent<Camera>();
                viewerCamera.clearFlags = CameraClearFlags.SolidColor;
                viewerCamera.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
                viewerCamera.fieldOfView = 60f;
                viewerCamera.nearClipPlane = 0.1f;
                viewerCamera.farClipPlane = 100f;
            }

            // 创建RenderTexture
            if (renderTexture == null)
            {
                Debug.Log("🖼️ 创建RenderTexture");
                renderTexture = new RenderTexture(512, 512, 16);
                renderTexture.Create();
                viewerCamera.targetTexture = renderTexture;
            }

            // 创建RawImage来显示RenderTexture
            if (displayImage == null)
            {
                Debug.Log("🖼️ 创建RawImage显示组件");
                var imageGO = new GameObject("ModelDisplay");
                imageGO.transform.SetParent(transform, false);
                
                var imageRect = imageGO.AddComponent<RectTransform>();
                imageRect.anchorMin = Vector2.zero;
                imageRect.anchorMax = Vector2.one;
                imageRect.offsetMin = Vector2.zero;
                imageRect.offsetMax = Vector2.zero;
                
                displayImage = imageGO.AddComponent<RawImage>();
                displayImage.texture = renderTexture;
                
                // 确保GameObject是激活的
                imageGO.SetActive(true);
                
                Debug.Log($"🖼️ RawImage创建完成: active={imageGO.activeSelf}, activeInHierarchy={imageGO.activeInHierarchy}");
                
                // 追踪整个父对象链的激活状态
                Transform current = imageGO.transform;
                int level = 0;
                while (current != null && level < 10) // 限制层级防止死循环
                {
                    Debug.Log($"  - Level {level}: {current.name} - activeSelf={current.gameObject.activeSelf}, activeInHierarchy={current.gameObject.activeInHierarchy}");
                    current = current.parent;
                    level++;
                }
            }

            // 如果没有相机控制器，创建一个
            if (cameraRig == null)
            {
                var rigGO = new GameObject("CameraRig");
                rigGO.transform.SetParent(transform);
                rigGO.transform.localPosition = Vector3.zero;
                rigGO.transform.localRotation = Quaternion.identity;
                cameraRig = rigGO.transform;
                Debug.Log($"📷 创建相机控制器: {rigGO.name}");

                if (viewerCamera != null)
                {
                    viewerCamera.transform.SetParent(cameraRig);
                }
            }
            
            // 确保相机控制器在正确位置
            if (cameraRig.localPosition != Vector3.zero)
            {
                Debug.Log($"📷 重置相机控制器位置: {cameraRig.localPosition} -> (0,0,0)");
                cameraRig.localPosition = Vector3.zero;
                cameraRig.localRotation = Quaternion.identity;
            }

            // 如果没有模型容器，创建一个
            if (modelContainer == null)
            {
                var containerGO = new GameObject("ModelContainer");
                containerGO.transform.SetParent(transform);
                containerGO.transform.localPosition = Vector3.zero;
                containerGO.transform.localRotation = Quaternion.identity;
                modelContainer = containerGO.transform;
                Debug.Log($"📦 创建模型容器: {containerGO.name}");
            }
            
            // 确保模型容器在正确位置
            if (modelContainer.localPosition != Vector3.zero)
            {
                Debug.Log($"📦 重置模型容器位置: {modelContainer.localPosition} -> (0,0,0)");
                modelContainer.localPosition = Vector3.zero;
                modelContainer.localRotation = Quaternion.identity;
            }

            // 如果没有灯光，创建一个
            if (modelLight == null)
            {
                var lightGO = new GameObject("ModelLight");
                lightGO.transform.SetParent(cameraRig);
                lightGO.transform.localPosition = new Vector3(1, 1, -1);
                
                var light = lightGO.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1f;
                light.color = Color.white;
                modelLight = light;
            }

            // 保存原始设置
            originalCameraPosition = defaultCameraPosition;
            originalCameraRotation = defaultCameraRotation;

            // 设置初始位置
            ResetView();

            Debug.Log($"✅ Model3DViewer初始化完成:");
            Debug.Log($"  - ViewerCamera存在: {viewerCamera != null}");
            Debug.Log($"  - ModelContainer存在: {modelContainer != null}");
            Debug.Log($"  - RenderTexture存在: {renderTexture != null}");
            Debug.Log($"  - DisplayImage存在: {displayImage != null}");
            Debug.Log($"  - ModelLight存在: {modelLight != null}");
        }

        /// <summary>
        /// 设置UI控件
        /// </summary>
        private void SetupUI()
        {
            // 重置按钮
            if (resetButton != null)
            {
                resetButton.onClick.AddListener(ResetView);
            }

            // 缩放滑块
            if (zoomSlider != null)
            {
                zoomSlider.minValue = minZoom;
                zoomSlider.maxValue = maxZoom;
                zoomSlider.value = currentZoom;
                zoomSlider.onValueChanged.AddListener(OnZoomSliderChanged);
            }

            UpdateZoomText();
        }

        /// <summary>
        /// 显示3D模型
        /// </summary>
        public void ShowModel(GameObject modelPrefab)
        {
            // 确保查看器已初始化
            if (!isInitialized)
            {
                InitializeViewer();
                isInitialized = true;
            }

            // 清除当前模型
            ClearModel();

            if (modelPrefab == null)
            {
                Debug.LogWarning("尝试显示空的模型预制体");
                return;
            }

            try
            {
                Debug.Log($"🎯 开始加载3D模型: {modelPrefab.name}");
                Debug.Log($"  - ModelContainer存在: {modelContainer != null}");
                Debug.Log($"  - ViewerCamera存在: {viewerCamera != null}");
                Debug.Log($"  - RenderTexture存在: {renderTexture != null}");
                Debug.Log($"  - DisplayImage存在: {displayImage != null}");
                
                if (viewerCamera != null)
                {
                    Debug.Log($"  - Camera.enabled: {viewerCamera.enabled}");
                    Debug.Log($"  - Camera.targetTexture: {viewerCamera.targetTexture != null}");
                    Debug.Log($"  - Camera.cullingMask: {viewerCamera.cullingMask}");
                    Debug.Log($"  - Camera.position: {viewerCamera.transform.position}");
                }
                
                if (renderTexture != null)
                {
                    Debug.Log($"  - RenderTexture.IsCreated: {renderTexture.IsCreated()}");
                    Debug.Log($"  - RenderTexture.width×height: {renderTexture.width}×{renderTexture.height}");
                }
                
                if (displayImage != null)
                {
                    Debug.Log($"  - RawImage.enabled: {displayImage.enabled}");
                    Debug.Log($"  - RawImage.gameObject.activeSelf: {displayImage.gameObject.activeSelf}");
                    Debug.Log($"  - RawImage.gameObject.activeInHierarchy: {displayImage.gameObject.activeInHierarchy}");
                    Debug.Log($"  - RawImage.texture: {displayImage.texture != null}");
                    
                    // 如果RawImage没有激活，强制激活它
                    if (!displayImage.gameObject.activeSelf)
                    {
                        Debug.Log($"🚨 RawImage未激活，强制激活");
                        displayImage.gameObject.SetActive(true);
                    }
                    
                    // 如果RawImage组件被禁用，启用它
                    if (!displayImage.enabled)
                    {
                        Debug.Log($"🚨 RawImage组件被禁用，强制启用");
                        displayImage.enabled = true;
                    }
                    
                    // 如果activeInHierarchy仍然为false，输出警告
                    if (!displayImage.gameObject.activeInHierarchy && displayImage.gameObject.activeSelf)
                    {
                        Debug.LogWarning($"⚠️ RawImage仍然在层级中不活跃，这可能是时序问题");
                    }
                }

                // 实例化新模型
                currentModel = Instantiate(modelPrefab, modelContainer);
                currentModel.transform.localPosition = Vector3.zero;
                currentModel.transform.localRotation = Quaternion.identity;
                currentModel.transform.localScale = Vector3.one;

                Debug.Log($"📦 模型实例化信息:");
                Debug.Log($"  - 模型名称: {currentModel.name}");
                Debug.Log($"  - 模型位置: {currentModel.transform.position}");
                Debug.Log($"  - 模型激活状态: {currentModel.activeInHierarchy}");
                Debug.Log($"  - 模型层级: {currentModel.layer}");
                Debug.Log($"  - 模型缩放: {currentModel.transform.localScale}");
                Debug.Log($"  - 父容器位置: {modelContainer.position}");
                
                // 检查模型是否有Renderer组件和材质
                Renderer[] renderers = currentModel.GetComponentsInChildren<Renderer>();
                Debug.Log($"  - Renderer组件数量: {renderers.Length}");
                for (int i = 0; i < renderers.Length; i++)
                {
                    var renderer = renderers[i];
                    Debug.Log($"    - Renderer[{i}]: {renderer.name}, enabled={renderer.enabled}, bounds={renderer.bounds}");
                    Debug.Log($"      - 材质数量: {renderer.materials.Length}");
                    
                    for (int j = 0; j < renderer.materials.Length; j++)
                    {
                        var material = renderer.materials[j];
                        if (material != null)
                        {
                            Debug.Log($"        - Material[{j}]: {material.name}");
                            Debug.Log($"          - Shader: {material.shader.name}");
                            Debug.Log($"          - 主颜色: {material.color}");
                            Debug.Log($"          - 主纹理: {(material.mainTexture != null ? material.mainTexture.name : "null")}");
                            
                            // 检查是否是默认材质
                            if (material.name.Contains("Default"))
                            {
                                Debug.LogWarning($"          ⚠️ 使用默认材质，可能需要设置颜色");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"        - Material[{j}]: null");
                        }
                    }
                }
                
                if (viewerCamera != null)
                {
                    Debug.Log($"📷 相机信息:");
                    Debug.Log($"  - 相机位置: {viewerCamera.transform.position}");
                    Debug.Log($"  - 相机朝向: {viewerCamera.transform.forward}");
                    Debug.Log($"  - 视野角度: {viewerCamera.fieldOfView}");
                    Debug.Log($"  - 近裁剪面: {viewerCamera.nearClipPlane}");
                    Debug.Log($"  - 远裁剪面: {viewerCamera.farClipPlane}");
                }

                // 确保模型在正确的层级
                SetLayerRecursively(currentModel, gameObject.layer);

                // 居中模型
                CenterModel();
                
                // 应用材质颜色（如果需要）
                ApplyMineralColor();

                // 重置视图
                ResetView();

                // 强制渲染一帧
                if (viewerCamera != null)
                {
                    viewerCamera.Render();
                    Debug.Log($"🎬 强制渲染相机完成");
                }

                // 检查相机和模型的空间关系
                DebugSpatialRelationship();

                // 检查RenderTexture内容
                StartCoroutine(CheckRenderTextureContent());

                Debug.Log($"✅ 模型加载成功: {modelPrefab.name}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ 加载模型失败: {e.Message}");
            }
        }

        /// <summary>
        /// 清除当前模型
        /// </summary>
        public void ClearModel()
        {
            if (currentModel != null)
            {
                DestroyImmediate(currentModel);
                currentModel = null;
            }
        }

        /// <summary>
        /// 居中模型
        /// </summary>
        private void CenterModel()
        {
            if (currentModel == null)
                return;

            Debug.Log("🎯 开始居中模型");

            // 获取模型的包围盒
            Bounds bounds = CalculateBounds(currentModel);
            Debug.Log($"  - 原始包围盒: center={bounds.center}, size={bounds.size}");
            
            // 将模型移动到容器中心（本地坐标原点）
            Vector3 offset = bounds.center - currentModel.transform.position;
            currentModel.transform.position = currentModel.transform.position - offset;
            
            Debug.Log($"  - 模型偏移: {offset}");
            Debug.Log($"  - 调整后模型位置: {currentModel.transform.position}");

            // 重新计算包围盒验证居中效果
            Bounds newBounds = CalculateBounds(currentModel);
            Debug.Log($"  - 新包围盒: center={newBounds.center}, size={newBounds.size}");

            // 根据模型大小调整相机距离
            float maxSize = Mathf.Max(newBounds.size.x, newBounds.size.y, newBounds.size.z);
            
            // 检查模型是否过小
            if (maxSize < 0.1f)
            {
                Debug.LogWarning($"⚠️ 模型过小 ({maxSize:F6})，将放大模型");
                // 放大模型到合理尺寸
                float scaleFactor = 1f / maxSize; // 放大到1单位大小
                currentModel.transform.localScale = Vector3.one * scaleFactor;
                
                // 重新计算包围盒
                newBounds = CalculateBounds(currentModel);
                maxSize = Mathf.Max(newBounds.size.x, newBounds.size.y, newBounds.size.z);
                Debug.Log($"  - 模型放大 {scaleFactor:F2} 倍后尺寸: {maxSize:F3}");
            }
            
            float distance = Mathf.Max(maxSize * 1.5f, 1.2f); // 调整距离让模型显示更大
            
            originalCameraPosition = new Vector3(0, 0, -distance);
            Debug.Log($"  - 计算相机距离: {distance}，基于模型最大尺寸: {maxSize}");
            Debug.Log($"  - 设置相机位置: {originalCameraPosition}");

            Debug.Log("✅ 模型居中完成");
        }

        /// <summary>
        /// 应用矿物颜色到模型材质
        /// </summary>
        private void ApplyMineralColor()
        {
            if (currentModel == null) return;

            Debug.Log("🎨 开始应用材质颜色");

            Renderer[] renderers = currentModel.GetComponentsInChildren<Renderer>();
            
            // 矿物典型颜色映射
            var mineralColors = new Dictionary<string, Color>
            {
                {"plagioclase", new Color(0.9f, 0.9f, 0.95f, 1f)}, // 淡灰白色
                {"pyroxene", new Color(0.2f, 0.4f, 0.2f, 1f)},     // 深绿色
                {"amphibole", new Color(0.1f, 0.2f, 0.1f, 1f)},    // 深绿黑色
                {"magnetite", new Color(0.15f, 0.15f, 0.15f, 1f)}, // 黑色
                {"olivine", new Color(0.4f, 0.6f, 0.2f, 1f)},      // 橄榄绿
                {"quartz", new Color(0.95f, 0.95f, 0.95f, 1f)},    // 透明白色
                {"feldspar", new Color(0.8f, 0.7f, 0.6f, 1f)},     // 肉色
                {"biotite", new Color(0.1f, 0.1f, 0.1f, 1f)},      // 黑色
            };

            // 从模型名称推断矿物类型
            string modelName = currentModel.name.ToLower();
            Color targetColor = Color.white; // 默认白色
            
            foreach (var kvp in mineralColors)
            {
                if (modelName.Contains(kvp.Key))
                {
                    targetColor = kvp.Value;
                    Debug.Log($"  - 识别矿物类型: {kvp.Key} -> 颜色: {targetColor}");
                    break;
                }
            }

            // 应用颜色到所有材质
            foreach (var renderer in renderers)
            {
                if (renderer.materials.Length > 0)
                {
                    // 创建新材质实例以避免修改原始资源
                    Material[] newMaterials = new Material[renderer.materials.Length];
                    
                    for (int i = 0; i < renderer.materials.Length; i++)
                    {
                        var originalMaterial = renderer.materials[i];
                        
                        if (originalMaterial != null)
                        {
                            // 创建材质副本
                            var newMaterial = new Material(originalMaterial);
                            
                            // 设置颜色
                            newMaterial.color = targetColor;
                            
                            // 如果使用Standard shader，确保设置金属度和平滑度
                            if (newMaterial.shader.name.Contains("Standard"))
                            {
                                newMaterial.SetFloat("_Metallic", 0.1f);
                                newMaterial.SetFloat("_Glossiness", 0.3f);
                            }
                            
                            newMaterials[i] = newMaterial;
                            Debug.Log($"    - 应用颜色到材质: {originalMaterial.name} -> {targetColor}");
                        }
                        else
                        {
                            // 创建基础材质
                            var basicMaterial = new Material(Shader.Find("Standard"));
                            basicMaterial.color = targetColor;
                            basicMaterial.SetFloat("_Metallic", 0.1f);
                            basicMaterial.SetFloat("_Glossiness", 0.3f);
                            newMaterials[i] = basicMaterial;
                            Debug.Log($"    - 创建新材质并应用颜色: {targetColor}");
                        }
                    }
                    
                    renderer.materials = newMaterials;
                }
            }

            Debug.Log("✅ 材质颜色应用完成");
        }

        /// <summary>
        /// 计算游戏对象的包围盒
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
        /// 递归设置层级
        /// </summary>
        private void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        /// <summary>
        /// 重置视图
        /// </summary>
        public void ResetView()
        {
            currentZoom = 1f;
            currentRotation = Vector3.zero;

            if (cameraRig != null)
            {
                cameraRig.localRotation = Quaternion.Euler(originalCameraRotation);
            }

            if (viewerCamera != null)
            {
                viewerCamera.transform.localPosition = originalCameraPosition * currentZoom;
            }

            if (zoomSlider != null)
            {
                zoomSlider.value = currentZoom;
            }

            UpdateZoomText();
        }

        /// <summary>
        /// 处理拖拽旋转
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            if (cameraRig == null)
                return;

            Vector2 delta = eventData.delta;
            
            // 计算旋转
            float rotationX = -delta.y * rotationSpeed;
            float rotationY = delta.x * rotationSpeed;

            if (invertY)
                rotationX = -rotationX;

            // 应用旋转
            currentRotation.x += rotationX;
            currentRotation.y += rotationY;

            // 限制X轴旋转角度
            currentRotation.x = Mathf.Clamp(currentRotation.x, -90f, 90f);

            cameraRig.localRotation = Quaternion.Euler(currentRotation);
        }

        /// <summary>
        /// 处理滚轮缩放
        /// </summary>
        public void OnScroll(PointerEventData eventData)
        {
            float scroll = eventData.scrollDelta.y;
            SetZoom(currentZoom - scroll * zoomSpeed * 0.1f);
        }

        /// <summary>
        /// 设置缩放级别
        /// </summary>
        public void SetZoom(float zoom)
        {
            currentZoom = Mathf.Clamp(zoom, minZoom, maxZoom);

            if (viewerCamera != null)
            {
                viewerCamera.transform.localPosition = originalCameraPosition * currentZoom;
            }

            if (zoomSlider != null && Mathf.Abs(zoomSlider.value - currentZoom) > 0.01f)
            {
                zoomSlider.value = currentZoom;
            }

            UpdateZoomText();
        }

        /// <summary>
        /// 缩放滑块值改变事件
        /// </summary>
        private void OnZoomSliderChanged(float value)
        {
            SetZoom(value);
        }

        /// <summary>
        /// 更新缩放文本
        /// </summary>
        private void UpdateZoomText()
        {
            if (zoomText != null)
            {
                zoomText.text = $"{currentZoom:F1}x";
            }
        }

        /// <summary>
        /// 获取当前是否有模型显示
        /// </summary>
        public bool HasModel()
        {
            return currentModel != null;
        }

        /// <summary>
        /// 获取当前模型
        /// </summary>
        public GameObject GetCurrentModel()
        {
            return currentModel;
        }

        private void OnDestroy()
        {
            ClearModel();
        }

        /// <summary>
        /// 调试相机和模型的空间关系
        /// </summary>
        private void DebugSpatialRelationship()
        {
            if (currentModel == null || viewerCamera == null)
            {
                Debug.LogWarning("⚠️ 无法调试空间关系：模型或相机为空");
                return;
            }

            Debug.Log("🔍 === 空间关系调试信息 ===");

            // 模型信息
            Bounds modelBounds = CalculateBounds(currentModel);
            Debug.Log($"📦 模型空间信息:");
            Debug.Log($"  - 模型世界位置: {currentModel.transform.position}");
            Debug.Log($"  - 模型本地位置: {currentModel.transform.localPosition}");
            Debug.Log($"  - 模型包围盒中心: {modelBounds.center}");
            Debug.Log($"  - 模型包围盒大小: {modelBounds.size}");
            Debug.Log($"  - 模型包围盒范围: min={modelBounds.min}, max={modelBounds.max}");

            // 相机信息
            Debug.Log($"📷 相机空间信息:");
            Debug.Log($"  - 相机世界位置: {viewerCamera.transform.position}");
            Debug.Log($"  - 相机本地位置: {viewerCamera.transform.localPosition}");
            Debug.Log($"  - 相机朝向: {viewerCamera.transform.forward}");
            Debug.Log($"  - 相机向上方向: {viewerCamera.transform.up}");
            Debug.Log($"  - 相机右方向: {viewerCamera.transform.right}");

            // 距离计算
            float distanceToModel = Vector3.Distance(viewerCamera.transform.position, modelBounds.center);
            Debug.Log($"📏 距离信息:");
            Debug.Log($"  - 相机到模型中心距离: {distanceToModel:F3}");
            Debug.Log($"  - 相机近裁剪面: {viewerCamera.nearClipPlane}");
            Debug.Log($"  - 相机远裁剪面: {viewerCamera.farClipPlane}");
            Debug.Log($"  - 模型是否在裁剪范围内: {distanceToModel >= viewerCamera.nearClipPlane && distanceToModel <= viewerCamera.farClipPlane}");

            // 视野角度和模型大小关系
            float maxModelSize = Mathf.Max(modelBounds.size.x, modelBounds.size.y, modelBounds.size.z);
            float fovRadians = viewerCamera.fieldOfView * Mathf.Deg2Rad;
            float visibleSize = 2f * distanceToModel * Mathf.Tan(fovRadians / 2f);
            Debug.Log($"🎯 视野信息:");
            Debug.Log($"  - 视野角度: {viewerCamera.fieldOfView}°");
            Debug.Log($"  - 在当前距离可见大小: {visibleSize:F3}");
            Debug.Log($"  - 模型最大尺寸: {maxModelSize:F3}");
            Debug.Log($"  - 模型是否适合视野: {maxModelSize <= visibleSize}");

            // 检查模型是否在相机前方
            Vector3 toModel = (modelBounds.center - viewerCamera.transform.position).normalized;
            float dot = Vector3.Dot(viewerCamera.transform.forward, toModel);
            Debug.Log($"🎪 方向信息:");
            Debug.Log($"  - 模型方向向量: {toModel}");
            Debug.Log($"  - 相机前方点积: {dot:F3}");
            Debug.Log($"  - 模型是否在相机前方: {dot > 0}");

            // 层级检查
            Debug.Log($"🏷️ 层级信息:");
            Debug.Log($"  - 相机层级: {viewerCamera.gameObject.layer}");
            Debug.Log($"  - 相机剔除遮罩: {viewerCamera.cullingMask}");
            Debug.Log($"  - 模型层级: {currentModel.layer}");
            Debug.Log($"  - 模型是否在相机可见层级: {(viewerCamera.cullingMask & (1 << currentModel.layer)) != 0}");

            // 建议修复
            Debug.Log("🔧 修复建议:");
            if (distanceToModel < viewerCamera.nearClipPlane)
            {
                Debug.LogWarning($"  ⚠️ 模型太近，需要调整相机距离或近裁剪面");
            }
            if (distanceToModel > viewerCamera.farClipPlane)
            {
                Debug.LogWarning($"  ⚠️ 模型太远，需要调整相机距离或远裁剪面");
            }
            if (maxModelSize > visibleSize)
            {
                Debug.LogWarning($"  ⚠️ 模型太大，需要增加相机距离或调整视野角度");
            }
            if (dot <= 0)
            {
                Debug.LogWarning($"  ⚠️ 模型不在相机前方，需要调整相机或模型位置");
            }
            if ((viewerCamera.cullingMask & (1 << currentModel.layer)) == 0)
            {
                Debug.LogWarning($"  ⚠️ 模型层级不在相机可见范围内");
            }

            Debug.Log("🔍 === 空间关系调试完成 ===");
        }

        /// <summary>
        /// 测试加载第一个可用的矿物模型
        /// </summary>
        public void TestLoadFirstMineralModel()
        {
            Debug.Log("🧪 开始测试加载第一个矿物模型");

            // 尝试加载几个已知存在的矿物模型（GLB格式）
            string[] testMinerals = { "quartz_001", "plagioclase_001", "pyroxene_001", "amphibole_001" };
            
            foreach (string mineralName in testMinerals)
            {
                string modelPath = "MineralData/Models/Minerals/" + mineralName;
                Debug.Log($"尝试加载GLB模型: {modelPath}");
                
                GameObject modelPrefab = Resources.Load<GameObject>(modelPath);
                if (modelPrefab != null)
                {
                    Debug.Log($"✅ 成功找到GLB模型: {mineralName}");
                    ShowModel(modelPrefab);
                    return;
                }
                else
                {
                    Debug.LogWarning($"⚠️ 未找到GLB模型: {modelPath}");
                }
            }

            Debug.LogError("❌ 未找到任何测试GLB矿物模型");
        }

        /// <summary>
        /// 检查RenderTexture内容
        /// </summary>
        private System.Collections.IEnumerator CheckRenderTextureContent()
        {
            Debug.Log("🔍 开始检查RenderTexture内容");
            
            yield return new WaitForEndOfFrame();
            
            try
            {
                if (renderTexture == null)
                {
                    Debug.LogError("❌ RenderTexture为null");
                    yield break;
                }
                
                if (!renderTexture.IsCreated())
                {
                    Debug.LogError("❌ RenderTexture未创建");
                    yield break;
                }
                
                Debug.Log($"📊 RenderTexture状态: {renderTexture.width}x{renderTexture.height}, IsCreated={renderTexture.IsCreated()}");
                
                // 创建临时Texture2D来读取RenderTexture内容
                RenderTexture previousActive = RenderTexture.active;
                RenderTexture.active = renderTexture;
                
                Texture2D tempTexture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
                tempTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                tempTexture.Apply();
                
                RenderTexture.active = previousActive;
                
                Debug.Log("📸 成功读取RenderTexture像素数据");
                
                // 检查中心像素颜色
                Color centerPixel = tempTexture.GetPixel(renderTexture.width / 2, renderTexture.height / 2);
                Color backgroundColor = viewerCamera != null ? viewerCamera.backgroundColor : Color.black;
                
                Debug.Log($"🎨 颜色分析:");
                Debug.Log($"  - 中心像素颜色: R={centerPixel.r:F3}, G={centerPixel.g:F3}, B={centerPixel.b:F3}");
                Debug.Log($"  - 背景颜色: R={backgroundColor.r:F3}, G={backgroundColor.g:F3}, B={backgroundColor.b:F3}");
                
                float colorDistance = Vector3.Distance(
                    new Vector3(centerPixel.r, centerPixel.g, centerPixel.b), 
                    new Vector3(backgroundColor.r, backgroundColor.g, backgroundColor.b)
                );
                Debug.Log($"  - 颜色距离: {colorDistance:F3}");
                Debug.Log($"  - 是否为背景色: {colorDistance < 0.1f}");
                
                // 计算非背景像素数量
                int nonBackgroundPixels = 0;
                Color[] pixels = tempTexture.GetPixels();
                Debug.Log($"📊 像素分析: 总像素数={pixels.Length}");
                
                for (int i = 0; i < pixels.Length; i++)
                {
                    float pixelDistance = Vector3.Distance(
                        new Vector3(pixels[i].r, pixels[i].g, pixels[i].b), 
                        new Vector3(backgroundColor.r, backgroundColor.g, backgroundColor.b)
                    );
                    if (pixelDistance > 0.1f)
                    {
                        nonBackgroundPixels++;
                    }
                }
                
                float percentage = (float)nonBackgroundPixels / pixels.Length * 100;
                Debug.Log($"🔍 最终结果: 非背景像素数量={nonBackgroundPixels}/{pixels.Length} ({percentage:F1}%)");
                
                if (nonBackgroundPixels == 0)
                {
                    Debug.LogWarning("⚠️ 检测到RenderTexture只有背景色，模型可能没有正确渲染");
                    Debug.LogWarning($"  - 检查相机是否正确设置");
                    Debug.LogWarning($"  - 检查模型是否在相机视野内");
                    Debug.LogWarning($"  - 检查模型层级设置");
                }
                else
                {
                    Debug.Log($"✅ 检测到模型内容，RenderTexture渲染正常");
                }
                
                DestroyImmediate(tempTexture);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ RenderTexture检查异常: {e.Message}");
                Debug.LogError($"Stack trace: {e.StackTrace}");
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 编辑器中的参数验证
            minZoom = Mathf.Max(0.1f, minZoom);
            maxZoom = Mathf.Max(minZoom + 0.1f, maxZoom);
            rotationSpeed = Mathf.Max(0f, rotationSpeed);
            zoomSpeed = Mathf.Max(0f, zoomSpeed);
        }
#endif
    }
}
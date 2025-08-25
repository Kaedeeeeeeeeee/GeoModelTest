using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace SampleCuttingSystem
{
    /// <summary>
    /// 3D样本模型查看器
    /// 在UI中显示样本的3D模型
    /// </summary>
    public class Sample3DModelViewer : MonoBehaviour
    {
        [Header("渲染设置")]
        public RawImage rawImage;
        public int textureWidth = 512;
        public int textureHeight = 512;
        
        [Header("相机设置")]
        public float cameraDistance = 2f;
        public Vector3 cameraRotation = new Vector3(0f, 0f, 0f);  // 正视角度：摄像机正对样本
        public float autoRotationSpeed = 30f;
        public float manualRotationSensitivity = 2f;
        public float zoomSensitivity = 1f;
        public float minCameraDistance = 0.5f;
        public float maxCameraDistance = 5f;
        
        [Header("光照设置")]
        public Color lightColor = Color.white;
        public float lightIntensity = 2.0f;  // 增加光照强度确保模型可见
        
        // 私有变量
        private RenderTexture renderTexture;
        private Camera renderCamera;
        private GameObject currentSample;
        private GameObject lightObject;
        private Transform cameraTransform;
        [SerializeField] private bool isAutoRotating = true;
        
        // 公开属性用于外部访问
        public bool IsAutoRotating => isAutoRotating;
        
        // 交互控制变量
        private bool isMouseDown = false;
        private Vector2 lastMousePosition;
        private bool isMouseOverArea = false;
        
        void Awake()
        {
            SetupRenderSystem();
            SetupMouseInteraction();
        }
        
        /// <summary>
        /// 设置渲染系统
        /// </summary>
        void SetupRenderSystem()
        {
            // 创建RenderTexture
            renderTexture = new RenderTexture(textureWidth, textureHeight, 16);
            renderTexture.format = RenderTextureFormat.ARGB32;
            renderTexture.Create();
            
            Debug.Log($"RenderTexture创建成功: {textureWidth}x{textureHeight}, IsCreated: {renderTexture.IsCreated()}");
            
            // 设置RawImage
            if (rawImage != null)
            {
                rawImage.texture = renderTexture;
                // 确保RawImage可见
                rawImage.color = Color.white;
                // 确保RawImage在最顶层
                rawImage.transform.SetAsLastSibling();
                
                Debug.Log($"RawImage纹理设置成功，RawImage active: {rawImage.gameObject.activeInHierarchy}");
                Debug.Log($"RawImage Rect: {((RectTransform)rawImage.transform).rect}");
                Debug.Log($"RawImage层级索引: {rawImage.transform.GetSiblingIndex()}");
            }
            else
            {
                Debug.LogError("RawImage组件为空！");
            }
            
            // 创建渲染相机
            GameObject cameraObj = new GameObject("ModelRenderCamera");
            cameraObj.transform.SetParent(transform);
            renderCamera = cameraObj.AddComponent<Camera>();
            cameraTransform = cameraObj.transform;
            
            // 配置相机
            renderCamera.targetTexture = renderTexture;
            renderCamera.backgroundColor = new Color(0.3f, 0.3f, 0.4f, 1f); // 浅灰蓝色背景，不透明，便于调试
            renderCamera.clearFlags = CameraClearFlags.SolidColor;
            // 设置相机渲染层 - 使用严格的层级隔离
            int previewLayer = LayerMask.NameToLayer("ModelPreview");
            if (previewLayer != -1)
            {
                renderCamera.cullingMask = LayerMask.GetMask("ModelPreview");
                Debug.Log($"使用ModelPreview层: {previewLayer}");
            }
            else
            {
                // 创建隔离的渲染空间：将摄像机移动到远离场景的位置
                // 只渲染我们创建的样本模型，不渲染场景中的任何物体
                renderCamera.cullingMask = LayerMask.GetMask("Default");
                Debug.Log("ModelPreview层不存在，使用Default层但采用位置隔离策略");
            }
            renderCamera.orthographic = true;  // 改为正交模式，适合地质样本科学观察
            renderCamera.orthographicSize = 1.5f;  // 初始正交尺寸，后续会根据样本大小调整
            renderCamera.nearClipPlane = 0.1f;
            renderCamera.farClipPlane = 10f;
            
            Debug.Log($"相机配置完成: 位置={renderCamera.transform.position}, 旋转={renderCamera.transform.rotation}");
            
            // 设置相机位置
            UpdateCameraPosition();
            
            // 创建光源
            SetupLighting();
            
            Debug.Log("3D模型渲染系统初始化完成");
        }
        
        /// <summary>
        /// 设置光照
        /// </summary>
        void SetupLighting()
        {
            // 主光源 - 移动到隔离空间
            GameObject mainLightObj = new GameObject("ModelMainLight");
            mainLightObj.transform.SetParent(transform);
            mainLightObj.transform.position = new Vector3(1000f, 1005f, 1000f); // 在隔离空间中的光源位置
            Light mainLight = mainLightObj.AddComponent<Light>();
            mainLight.type = LightType.Point; // 改为点光源，在隔离空间中更容易控制
            mainLight.color = lightColor;
            mainLight.intensity = lightIntensity;
            mainLight.range = 50f; // 设置点光源照射范围
            mainLight.shadows = LightShadows.None;
            // 设置光照层 (如果ModelPreview层不存在，照亮所有层)
            int previewLayer = LayerMask.NameToLayer("ModelPreview");
            if (previewLayer != -1)
            {
                mainLight.cullingMask = LayerMask.GetMask("ModelPreview");
            }
            else
            {
                mainLight.cullingMask = LayerMask.GetMask("Default"); // 只照亮Default层
            }
            // 点光源不需要旋转
            
            // 补光 - 也移动到隔离空间
            GameObject fillLightObj = new GameObject("ModelFillLight");
            fillLightObj.transform.SetParent(transform);
            fillLightObj.transform.position = new Vector3(995f, 1000f, 1005f); // 补光位置
            Light fillLight = fillLightObj.AddComponent<Light>();
            fillLight.type = LightType.Point; // 也改为点光源
            fillLight.color = new Color(0.8f, 0.9f, 1f, 1f); // 稍微偏蓝的补光
            fillLight.intensity = 0.8f; // 增加补光强度
            fillLight.range = 30f; // 补光范围
            fillLight.shadows = LightShadows.None;
            // 设置补光层 (如果ModelPreview层不存在，照亮所有层)
            if (previewLayer != -1)
            {
                fillLight.cullingMask = LayerMask.GetMask("ModelPreview");
            }
            else
            {
                fillLight.cullingMask = LayerMask.GetMask("Default"); // 只照亮Default层
            }
            // 点光源不需要旋转设置
            
            lightObject = mainLightObj;
        }
        
        /// <summary>
        /// 显示样本模型
        /// </summary>
        public void ShowSampleModel(GameObject samplePrefab)
        {
            // 清除当前模型
            ClearCurrentModel();
            
            if (samplePrefab == null)
            {
                Debug.LogWarning("样本预制体为空");
                return;
            }
            
            // 实例化样本模型
            currentSample = Instantiate(samplePrefab);
            currentSample.transform.SetParent(transform);
            currentSample.name = "PreviewSample";
            
            Debug.Log($"样本模型实例化成功: {currentSample.name}");
            
            // 设置到预览层 (如果ModelPreview层不存在，使用Default层)
            int previewLayer = LayerMask.NameToLayer("ModelPreview");
            if (previewLayer == -1)
            {
                previewLayer = 0; // 使用Default层
                Debug.LogWarning("ModelPreview层不存在，使用Default层");
            }
            SetLayerRecursively(currentSample, previewLayer);
            Debug.Log($"模型层级设置为: {previewLayer}");
            
            // 定位模型
            PositionModel();
            
            // 隐藏默认提示
            HideDefaultPrompt();
            
            Debug.Log($"显示样本模型完成: {samplePrefab.name}");
        }
        
        /// <summary>
        /// 显示真实样本模型（使用与野外场景相同的重建系统）
        /// </summary>
        public void ShowRealSampleModel(SampleItem sampleData)
        {
            // 清除当前模型
            ClearCurrentModel();
            
            if (sampleData == null)
            {
                Debug.LogWarning("样本数据为空");
                return;
            }
            
            Debug.Log($"开始显示真实样本模型: {sampleData.displayName}, 工具ID: {sampleData.sourceToolID}");
            
            // 使用与野外场景相同的重建逻辑
            currentSample = CreateRealSampleModel(sampleData);
            
            if (currentSample != null)
            {
                currentSample.transform.SetParent(transform);
                currentSample.name = $"RealSamplePreview_{sampleData.sampleID}";
                
                Debug.Log($"真实样本模型创建成功: {currentSample.name}");
                
                // 设置到预览层
                int previewLayer = LayerMask.NameToLayer("ModelPreview");
                if (previewLayer == -1)
                {
                    previewLayer = 0; // 使用Default层
                    Debug.LogWarning("ModelPreview层不存在，使用Default层");
                }
                SetLayerRecursively(currentSample, previewLayer);
                Debug.Log($"真实模型层级设置为: {previewLayer}");
                
                // 定位模型
                PositionModel();
                
                // 隐藏默认提示
                HideDefaultPrompt();
                
                Debug.Log($"显示真实样本完成: {sampleData.displayName}");
            }
            else
            {
                Debug.LogError("真实样本模型创建失败！回退到重建样本模式");
                // 如果真实模型创建失败，回退到旧的重建逻辑
                ShowReconstructedSampleFallback(sampleData);
            }
        }
        
        /// <summary>
        /// 显示重建的样本模型（保留原方法作为备用）
        /// </summary>
        public void ShowReconstructedSample(GeometricSampleReconstructor.ReconstructedSample sample)
        {
            // 清除当前模型
            ClearCurrentModel();
            
            if (sample == null)
            {
                Debug.LogWarning("重建样本为空");
                return;
            }
            
            // 根据重建样本创建显示模型
            currentSample = CreateModelFromReconstructedSample(sample);
            
            if (currentSample != null)
            {
                currentSample.transform.SetParent(transform);
                currentSample.name = "ReconstructedPreviewSample";
                
                Debug.Log($"重建样本模型创建成功: {currentSample.name}，层级数: {sample.layerSegments?.Length ?? 0}");
                
                // 设置到预览层 (如果ModelPreview层不存在，使用Default层)
                int previewLayer = LayerMask.NameToLayer("ModelPreview");
                if (previewLayer == -1)
                {
                    previewLayer = 0; // 使用Default层
                    Debug.LogWarning("ModelPreview层不存在，使用Default层");
                }
                SetLayerRecursively(currentSample, previewLayer);
                Debug.Log($"重建模型层级设置为: {previewLayer}");
                
                // 定位模型
                PositionModel();
                
                // 隐藏默认提示
                HideDefaultPrompt();
                
                Debug.Log($"显示重建样本完成: {sample.sampleID}");
            }
            else
            {
                Debug.LogError("重建样本模型创建失败！");
            }
        }
        
        /// <summary>
        /// 创建真实样本模型（使用与野外场景相同的逻辑）
        /// </summary>
        GameObject CreateRealSampleModel(SampleItem sampleData)
        {
            Debug.Log($"开始创建真实样本模型: {sampleData.displayName}");
            
            // 方法1: 尝试使用原始模型重建（与SamplePlacer相同的逻辑）
            GameObject realModel = sampleData.RecreateOriginalModel(Vector3.zero);
            
            if (realModel != null)
            {
                Debug.Log($"✓ RecreateOriginalModel成功: {realModel.name}");
                
                // 移除不需要的组件（UI显示不需要物理效果）
                RemovePhysicsComponents(realModel);
                
                // 确保渲染器启用
                EnsureRenderersEnabled(realModel);
                
                return realModel;
            }
            
            // 方法2: 尝试使用GeometricSampleReconstructor
            Debug.Log("RecreateOriginalModel失败，尝试使用GeometricSampleReconstructor");
            
            GeometricSampleReconstructor reconstructor = FindObjectOfType<GeometricSampleReconstructor>();
            if (reconstructor != null)
            {
                Debug.Log("找到GeometricSampleReconstructor，尝试重建样本");
                try
                {
                    // 这里需要调用reconstructor的重建方法
                    // 注意：可能需要根据实际的GeometricSampleReconstructor API调整
                    realModel = TryGeometricReconstruction(sampleData, reconstructor);
                    
                    if (realModel != null)
                    {
                        Debug.Log($"✓ GeometricSampleReconstructor成功: {realModel.name}");
                        RemovePhysicsComponents(realModel);
                        EnsureRenderersEnabled(realModel);
                        return realModel;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"GeometricSampleReconstructor重建失败: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning("未找到GeometricSampleReconstructor组件");
            }
            
            // 方法3: 创建改进的备用模型（基于真实数据而非简单圆柱）
            Debug.Log("使用改进的备用模型创建方法");
            return CreateImprovedFallbackModel(sampleData);
        }
        
        /// <summary>
        /// 尝试使用GeometricSampleReconstructor进行重建
        /// </summary>
        GameObject TryGeometricReconstruction(SampleItem sampleData, GeometricSampleReconstructor reconstructor)
        {
            // 这是一个简化的实现，可能需要根据实际GeometricSampleReconstructor的API调整
            try
            {
                // 创建一个临时容器来进行重建
                GameObject tempContainer = new GameObject($"TempReconstruction_{sampleData.sampleID}");
                
                // 如果有geologicalLayers数据，尝试重建
                if (sampleData.geologicalLayers != null && sampleData.geologicalLayers.Count > 0)
                {
                    Debug.Log($"使用地质层数据重建，层数: {sampleData.geologicalLayers.Count}");
                    
                    // 创建基于真实地质数据的模型
                    GameObject layeredModel = CreateModelFromGeologicalLayers(sampleData);
                    
                    if (layeredModel != null)
                    {
                        layeredModel.transform.SetParent(tempContainer.transform);
                        return tempContainer;
                    }
                }
                
                // 清理失败的尝试
                DestroyImmediate(tempContainer);
                return null;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"GeometricSampleReconstructor重建异常: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 基于地质层数据创建模型
        /// </summary>
        GameObject CreateModelFromGeologicalLayers(SampleItem sampleData)
        {
            GameObject layeredModel = new GameObject($"GeologicalModel_{sampleData.sampleID}");
            
            float totalHeight = sampleData.totalDepth;
            float currentY = -totalHeight * 0.5f; // 从底部开始
            
            Debug.Log($"创建地质分层模型: 总高度={totalHeight}, 层数={sampleData.geologicalLayers.Count}");
            
            for (int i = 0; i < sampleData.geologicalLayers.Count; i++)
            {
                var geoLayer = sampleData.geologicalLayers[i];
                
                // 计算层高度（基于真实厚度）
                float layerHeight = geoLayer.thickness;
                if (layerHeight <= 0)
                {
                    // 如果没有厚度数据，平均分配
                    layerHeight = totalHeight / sampleData.geologicalLayers.Count;
                }
                
                // 创建层级对象
                GameObject layerObject = CreateLayerObjectFromLayerInfo(geoLayer, layerHeight, currentY, sampleData.sampleRadius);
                layerObject.transform.SetParent(layeredModel.transform);
                
                currentY += layerHeight;
                
                Debug.Log($"地质层 {i}: {geoLayer.layerName}, 高度={layerHeight:F3}, 颜色={geoLayer.layerColor}");
            }
            
            return layeredModel;
        }
        
        /// <summary>
        /// 创建单个地质层对象
        /// </summary>
        GameObject CreateLayerObject(GeologyLayer geoLayer, float height, float yPosition, float radius)
        {
            GameObject layerObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            layerObj.name = $"Layer_{geoLayer.layerName}";
            
            // 移除碰撞器
            DestroyImmediate(layerObj.GetComponent<Collider>());
            
            // 设置位置和大小
            layerObj.transform.localPosition = new Vector3(0, yPosition + height * 0.5f, 0);
            layerObj.transform.localScale = new Vector3(radius * 2, height * 0.5f, radius * 2);
            
            // 设置材质
            Renderer renderer = layerObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material layerMaterial = new Material(Shader.Find("Standard"));
                layerMaterial.color = geoLayer.layerColor;
                layerMaterial.name = $"GeoLayer_{geoLayer.layerName}";
                
                // 设置材质属性以获得更好的视觉效果
                layerMaterial.SetFloat("_Metallic", 0.0f);
                layerMaterial.SetFloat("_Glossiness", 0.3f);
                
                renderer.material = layerMaterial;
            }
            
            return layerObj;
        }
        
        /// <summary>
        /// 从LayerInfo创建单个地质层对象
        /// </summary>
        GameObject CreateLayerObjectFromLayerInfo(SampleItem.LayerInfo layerInfo, float height, float yPosition, float radius)
        {
            GameObject layerObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            layerObj.name = $"Layer_{layerInfo.layerName}";
            
            // 移除碰撞器
            DestroyImmediate(layerObj.GetComponent<Collider>());
            
            // 设置位置和大小
            layerObj.transform.localPosition = new Vector3(0, yPosition + height * 0.5f, 0);
            layerObj.transform.localScale = new Vector3(radius * 2, height * 0.5f, radius * 2);
            
            // 设置材质
            Renderer renderer = layerObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material layerMaterial = new Material(Shader.Find("Standard"));
                layerMaterial.color = layerInfo.layerColor;
                layerMaterial.name = $"GeoLayer_{layerInfo.layerName}";
                
                // 设置材质属性以获得更好的视觉效果
                layerMaterial.SetFloat("_Metallic", 0.0f);
                layerMaterial.SetFloat("_Glossiness", 0.3f);
                
                renderer.material = layerMaterial;
            }
            
            return layerObj;
        }
        
        /// <summary>
        /// 创建改进的备用模型
        /// </summary>
        GameObject CreateImprovedFallbackModel(SampleItem sampleData)
        {
            Debug.Log("创建改进的备用模型");
            
            // 如果有geologicalLayers数据，使用它们
            if (sampleData.geologicalLayers != null && sampleData.geologicalLayers.Count > 0)
            {
                return CreateModelFromGeologicalLayers(sampleData);
            }
            
            // 如果有LayerInfo数据，使用它们
            if (sampleData.geologicalLayers != null && sampleData.geologicalLayers.Count > 0)
            {
                return CreateModelFromGeologicalLayers(sampleData);
            }
            
            // 最后备用：创建单一颜色模型
            return CreateSingleColorModel(sampleData);
        }
        
        /// <summary>
        /// 基于SampleItem.geologicalLayers创建模型
        /// </summary>
        GameObject CreateModelFromSampleLayers(SampleItem sampleData)
        {
            GameObject layeredModel = new GameObject($"SampleLayerModel_{sampleData.sampleID}");
            
            float totalHeight = sampleData.totalDepth;
            float currentY = -totalHeight * 0.5f;
            
            Debug.Log($"使用SampleItem.geologicalLayers创建模型: 层数={sampleData.geologicalLayers.Count}");
            
            for (int i = 0; i < sampleData.geologicalLayers.Count; i++)
            {
                var layer = sampleData.geologicalLayers[i];
                
                // 计算层高度
                float layerHeight = layer.thickness > 0 ? layer.thickness : totalHeight / sampleData.geologicalLayers.Count;
                
                // 创建层级圆柱
                GameObject layerObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                layerObj.name = $"SampleLayer_{i}_{layer.layerName}";
                layerObj.transform.SetParent(layeredModel.transform);
                
                // 移除碰撞器
                DestroyImmediate(layerObj.GetComponent<Collider>());
                
                // 设置位置和大小
                layerObj.transform.localPosition = new Vector3(0, currentY + layerHeight * 0.5f, 0);
                layerObj.transform.localScale = new Vector3(sampleData.sampleRadius * 2, layerHeight * 0.5f, sampleData.sampleRadius * 2);
                
                // 设置材质
                Renderer renderer = layerObj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material layerMaterial = CreateLayerMaterialFromLayerInfo(layer, i);
                    renderer.material = layerMaterial;
                }
                
                currentY += layerHeight;
            }
            
            return layeredModel;
        }
        
        /// <summary>
        /// 创建单色模型（最后备用）
        /// </summary>
        GameObject CreateSingleColorModel(SampleItem sampleData)
        {
            Debug.Log("创建单色备用模型");
            
            GameObject singleModel;
            
            // 根据工具类型选择形状
            if (sampleData.sourceToolID == "1002") // 地质锤
            {
                singleModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                singleModel.transform.localScale = new Vector3(0.8f, 0.06f, 0.6f); // 薄片形状
            }
            else
            {
                singleModel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                singleModel.transform.localScale = new Vector3(
                    sampleData.sampleRadius * 2,
                    sampleData.totalDepth * 0.5f,
                    sampleData.sampleRadius * 2
                );
            }
            
            singleModel.name = $"SingleColorModel_{sampleData.sampleID}";
            
            // 移除碰撞器
            DestroyImmediate(singleModel.GetComponent<Collider>());
            
            // 设置材质
            Renderer renderer = singleModel.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = new Material(Shader.Find("Standard"));
                material.color = GetToolDefaultColor(sampleData.sourceToolID);
                material.SetFloat("_Metallic", 0.0f);
                material.SetFloat("_Glossiness", 0.3f);
                renderer.material = material;
            }
            
            return singleModel;
        }
        
        /// <summary>
        /// 移除物理组件（UI显示不需要）
        /// </summary>
        void RemovePhysicsComponents(GameObject obj)
        {
            // 移除Rigidbody组件
            Rigidbody[] rigidbodies = obj.GetComponentsInChildren<Rigidbody>();
            foreach (var rb in rigidbodies)
            {
                DestroyImmediate(rb);
            }
            
            // 禁用Collider组件
            Collider[] colliders = obj.GetComponentsInChildren<Collider>();
            foreach (var collider in colliders)
            {
                collider.enabled = false;
            }
            
            // 移除浮动效果组件
            SimpleSampleFloating[] floatingComponents = obj.GetComponentsInChildren<SimpleSampleFloating>();
            foreach (var floating in floatingComponents)
            {
                DestroyImmediate(floating);
            }
            
            Debug.Log($"移除物理组件: {rigidbodies.Length}个Rigidbody, {colliders.Length}个Collider, {floatingComponents.Length}个FloatingComponent");
        }
        
        /// <summary>
        /// 确保渲染器启用
        /// </summary>
        void EnsureRenderersEnabled(GameObject obj)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                renderer.enabled = true;
                Debug.Log($"确保渲染器启用: {renderer.gameObject.name}");
            }
        }
        
        /// <summary>
        /// 基于层名称创建材质
        /// </summary>
        Material CreateLayerMaterialFromName(string layerName, int index)
        {
            Material material = new Material(Shader.Find("Standard"));
            
            // 基于层名称智能分配颜色
            switch (layerName.ToLower())
            {
                case "砂岩": case "sandstone":
                    material.color = new Color(0.9f, 0.8f, 0.6f, 1f);
                    break;
                case "页岩": case "shale":
                    material.color = new Color(0.4f, 0.4f, 0.4f, 1f);
                    break;
                case "石灰岩": case "limestone":
                    material.color = new Color(0.8f, 0.8f, 0.7f, 1f);
                    break;
                case "花岗岩": case "granite":
                    material.color = new Color(0.6f, 0.5f, 0.5f, 1f);
                    break;
                default:
                    material.color = GetLayerColor(index);
                    break;
            }
            
            material.name = $"LayerMaterial_{layerName}_{index}";
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Glossiness", 0.3f);
            
            return material;
        }
        
        /// <summary>
        /// 基于LayerInfo创建材质
        /// </summary>
        Material CreateLayerMaterialFromLayerInfo(SampleItem.LayerInfo layerInfo, int index)
        {
            Material material = new Material(Shader.Find("Standard"));
            
            // 优先使用layerInfo中的颜色
            if (layerInfo.layerColor != Color.clear)
            {
                material.color = layerInfo.layerColor;
            }
            else
            {
                // 基于层名称智能分配颜色
                material.color = GetColorByLayerName(layerInfo.layerName, index);
            }
            
            material.name = $"LayerMaterial_{layerInfo.layerName}_{index}";
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Glossiness", 0.3f);
            
            return material;
        }
        
        /// <summary>
        /// 根据层名称获取颜色
        /// </summary>
        Color GetColorByLayerName(string layerName, int index)
        {
            switch (layerName.ToLower())
            {
                case "砂岩": case "sandstone":
                    return new Color(0.9f, 0.8f, 0.6f, 1f);
                case "页岩": case "shale":
                    return new Color(0.4f, 0.4f, 0.4f, 1f);
                case "石灰岩": case "limestone":
                    return new Color(0.8f, 0.8f, 0.7f, 1f);
                case "花岗岩": case "granite":
                    return new Color(0.6f, 0.5f, 0.5f, 1f);
                default:
                    return GetLayerColor(index);
            }
        }
        
        /// <summary>
        /// 获取工具默认颜色
        /// </summary>
        Color GetToolDefaultColor(string toolID)
        {
            return toolID switch
            {
                "1000" => new Color(0.8f, 0.6f, 0.4f, 1f), // 简易钻探 - 棕色
                "1001" => new Color(0.6f, 0.8f, 0.4f, 1f), // 钻塔 - 绿色
                "1002" => new Color(0.7f, 0.5f, 0.3f, 1f), // 地质锤 - 深棕色
                _ => new Color(0.6f, 0.6f, 0.6f, 1f)        // 默认 - 灰色
            };
        }
        
        /// <summary>
        /// 备用的重建样本显示（给ShowRealSampleModel调用）
        /// </summary>
        void ShowReconstructedSampleFallback(SampleItem sampleData)
        {
            Debug.Log("使用重建样本备用方案");
            
            // 从SampleDropZone的逻辑转换为ReconstructedSample格式
            var reconstructedSample = ConvertSampleItemToReconstructedSample(sampleData);
            if (reconstructedSample != null)
            {
                ShowReconstructedSample(reconstructedSample);
            }
            else
            {
                Debug.LogError("重建样本备用方案也失败了！");
            }
        }
        
        /// <summary>
        /// 将SampleItem转换为ReconstructedSample格式
        /// </summary>
        GeometricSampleReconstructor.ReconstructedSample ConvertSampleItemToReconstructedSample(SampleItem sampleData)
        {
            var reconstructedSample = new GeometricSampleReconstructor.ReconstructedSample();
            reconstructedSample.sampleID = sampleData.sampleID;
            reconstructedSample.totalHeight = sampleData.totalDepth;
            
            // 转换层级数据
            if (sampleData.geologicalLayers != null && sampleData.geologicalLayers.Count > 0)
            {
                List<GeometricSampleReconstructor.LayerSegment> segments = new List<GeometricSampleReconstructor.LayerSegment>();
                
                for (int i = 0; i < sampleData.geologicalLayers.Count; i++)
                {
                    var layer = sampleData.geologicalLayers[i];
                    var segment = new GeometricSampleReconstructor.LayerSegment();
                    
                    // 创建虚拟的GeologyLayer
                    segment.sourceLayer = new GeologyLayer();
                    segment.sourceLayer.layerName = layer.layerName;
                    segment.sourceLayer.averageThickness = layer.thickness;
                    segment.sourceLayer.layerColor = layer.layerColor != Color.clear ? layer.layerColor : GetLayerColorByName(layer.layerName, i);
                    
                    segments.Add(segment);
                }
                
                reconstructedSample.layerSegments = segments.ToArray();
            }
            
            return reconstructedSample;
        }
        
        /// <summary>
        /// 根据层名称获取颜色
        /// </summary>
        Color GetLayerColorByName(string layerName, int index)
        {
            switch (layerName.ToLower())
            {
                case "砂岩": case "sandstone":
                    return new Color(0.9f, 0.8f, 0.6f, 1f);
                case "页岩": case "shale":
                    return new Color(0.4f, 0.4f, 0.4f, 1f);
                case "石灰岩": case "limestone":
                    return new Color(0.8f, 0.8f, 0.7f, 1f);
                case "花岗岩": case "granite":
                    return new Color(0.6f, 0.5f, 0.5f, 1f);
                default:
                    return GetLayerColor(index);
            }
        }
        
        /// <summary>
        /// 从重建样本创建显示模型
        /// </summary>
        GameObject CreateModelFromReconstructedSample(GeometricSampleReconstructor.ReconstructedSample sample)
        {
            GameObject modelObj = new GameObject("ReconstructedSampleModel");
            
            Debug.Log($"开始创建重建样本模型: ID={sample.sampleID}, 高度={sample.totalHeight}, 层数={sample.layerSegments?.Length ?? 0}");
            
            // 创建圆柱体形状 (模拟钻孔样本)
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.transform.SetParent(modelObj.transform);
            cylinder.transform.localPosition = Vector3.zero;
            cylinder.transform.localScale = new Vector3(0.2f, sample.totalHeight * 0.5f, 0.2f); // 2m高度对应1.0的scale
            
            Debug.Log($"创建基础圆柱体: 位置={cylinder.transform.position}, 缩放={cylinder.transform.localScale}");
            
            // 移除碰撞器
            DestroyImmediate(cylinder.GetComponent<Collider>());
            
            // 如果有层级数据，创建分层显示
            if (sample.layerSegments != null && sample.layerSegments.Length > 0)
            {
                Debug.Log($"创建分层模型，层数: {sample.layerSegments.Length}");
                CreateLayeredModel(modelObj, sample);
                // 隐藏基础圆柱体，使用分层显示
                cylinder.SetActive(false);
            }
            else
            {
                // 使用默认材质
                Renderer renderer = cylinder.GetComponent<Renderer>();
                Material defaultMat = new Material(Shader.Find("Standard"));
                defaultMat.color = new Color(0.6f, 0.4f, 0.3f, 1f); // 默认褐色
                renderer.material = defaultMat;
                
                Debug.Log("使用默认单层材质显示");
            }
            
            // 验证模型是否有渲染器
            Renderer[] renderers = modelObj.GetComponentsInChildren<Renderer>();
            Debug.Log($"模型渲染器数量: {renderers.Length}");
            foreach (Renderer r in renderers)
            {
                Debug.Log($"渲染器: {r.name}, 材质: {r.material?.name}, 激活: {r.gameObject.activeInHierarchy}");
            }
            
            return modelObj;
        }
        
        /// <summary>
        /// 创建分层模型
        /// </summary>
        void CreateLayeredModel(GameObject parent, GeometricSampleReconstructor.ReconstructedSample sample)
        {
            float totalHeight = sample.totalHeight;
            Debug.Log($"创建分层模型：总高度={totalHeight}, 层数={sample.layerSegments.Length}");
            
            // 计算每层的实际高度（基于真实厚度数据）
            var layerHeights = CalculateLayerHeights(sample.layerSegments, totalHeight);
            
            float currentY = -totalHeight * 0.5f; // 从底部开始
            
            for (int i = 0; i < sample.layerSegments.Length; i++)
            {
                var segment = sample.layerSegments[i];
                float layerHeight = layerHeights[i];
                
                // 创建层级圆柱
                GameObject segmentCylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                segmentCylinder.transform.SetParent(parent.transform);
                segmentCylinder.name = $"Layer_{i}_{GetLayerDisplayName(segment)}";
                
                // 移除碰撞器
                DestroyImmediate(segmentCylinder.GetComponent<Collider>());
                
                // 定位和缩放 - 使用真实的层高度
                float yPos = currentY + layerHeight * 0.5f;
                segmentCylinder.transform.localPosition = new Vector3(0, yPos, 0);
                segmentCylinder.transform.localScale = new Vector3(0.2f, layerHeight * 0.5f, 0.2f);
                
                // 设置真实材质
                Renderer renderer = segmentCylinder.GetComponent<Renderer>();
                if (segment.material != null)
                {
                    renderer.material = segment.material;
                    Debug.Log($"层级 {i} 使用真实材质: {segment.material.name}, 颜色: {segment.material.color}");
                }
                else
                {
                    // 根据层级索引和源层信息生成材质
                    Material layerMat = CreateIntelligentLayerMaterial(segment, i);
                    renderer.material = layerMat;
                    Debug.Log($"层级 {i} 创建智能材质: {layerMat.name}, 颜色: {layerMat.color}");
                }
                
                currentY += layerHeight;
                Debug.Log($"层级 {i}: 高度={layerHeight:F3}m, Y位置={yPos:F3}, 材质={renderer.material.name}");
            }
        }
        
        /// <summary>
        /// 计算各层的实际高度
        /// </summary>
        float[] CalculateLayerHeights(GeometricSampleReconstructor.LayerSegment[] segments, float totalHeight)
        {
            float[] heights = new float[segments.Length];
            
            // 尝试从源层获取真实厚度
            float totalRealThickness = 0f;
            bool hasRealThickness = false;
            
            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i].sourceLayer != null && segments[i].sourceLayer.averageThickness > 0)
                {
                    heights[i] = segments[i].sourceLayer.averageThickness;
                    totalRealThickness += heights[i];
                    hasRealThickness = true;
                }
            }
            
            if (hasRealThickness && totalRealThickness > 0)
            {
                // 按比例缩放到总高度
                float scale = totalHeight / totalRealThickness;
                for (int i = 0; i < heights.Length; i++)
                {
                    if (heights[i] > 0)
                    {
                        heights[i] *= scale;
                    }
                    else
                    {
                        heights[i] = totalHeight / segments.Length; // 默认平均分配
                    }
                }
                Debug.Log($"使用真实厚度数据，缩放比例: {scale:F3}");
            }
            else
            {
                // 平均分配
                float averageHeight = totalHeight / segments.Length;
                for (int i = 0; i < heights.Length; i++)
                {
                    heights[i] = averageHeight;
                }
                Debug.Log("使用平均分配高度");
            }
            
            return heights;
        }
        
        /// <summary>
        /// 获取层级显示名称
        /// </summary>
        string GetLayerDisplayName(GeometricSampleReconstructor.LayerSegment segment)
        {
            if (segment.sourceLayer != null && !string.IsNullOrEmpty(segment.sourceLayer.layerName))
            {
                return segment.sourceLayer.layerName;
            }
            return "Unknown";
        }
        
        /// <summary>
        /// 创建智能层级材质
        /// </summary>
        Material CreateIntelligentLayerMaterial(GeometricSampleReconstructor.LayerSegment segment, int index)
        {
            var material = new Material(Shader.Find("Standard"));
            
            // 优先使用源层的颜色
            if (segment.sourceLayer != null)
            {
                material.color = segment.sourceLayer.layerColor;
                material.name = $"LayerMaterial_{segment.sourceLayer.layerName}";
            }
            else
            {
                // 使用默认颜色方案
                material.color = GetLayerColor(index);
                material.name = $"LayerMaterial_{index}";
            }
            
            // 设置材质属性以获得更好的视觉效果
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Glossiness", 0.3f);
            
            return material;
        }
        
        /// <summary>
        /// 获取层级颜色
        /// </summary>
        Color GetLayerColor(int index)
        {
            Color[] colors = {
                new Color(0.8f, 0.6f, 0.4f, 1f), // 浅褐色
                new Color(0.6f, 0.8f, 0.4f, 1f), // 浅绿色
                new Color(0.4f, 0.6f, 0.8f, 1f), // 浅蓝色
                new Color(0.8f, 0.4f, 0.6f, 1f), // 浅红色
                new Color(0.8f, 0.8f, 0.4f, 1f), // 浅黄色
                new Color(0.6f, 0.4f, 0.8f, 1f)  // 浅紫色
            };
            return colors[index % colors.Length];
        }
        
        /// <summary>
        /// 定位模型
        /// </summary>
        void PositionModel()
        {
            if (currentSample == null) return;
            
            // 计算模型边界
            Bounds bounds = CalculateModelBounds(currentSample);
            Debug.Log($"模型边界: center={bounds.center}, size={bounds.size}");
            
            // 将模型移动到固定的隔离渲染空间中心，抬高更多让它显示在画面中心
            Vector3 isolatedPosition = new Vector3(1000f, 1002f, 1000f);
            currentSample.transform.position = isolatedPosition;
            Debug.Log($"模型位置设置为: {currentSample.transform.position}");
            
            // 调整正交摄像机的显示范围以适应模型大小
            float maxSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            cameraDistance = maxSize * 1.4f;  // 保持合理的摄像机距离用于光照
            
            // 设置正交摄像机的显示大小（orthographicSize是半高）
            renderCamera.orthographicSize = maxSize * 0.51f;  // 让样本占据视野的大部分
            Debug.Log($"相机距离: {cameraDistance}, 正交显示大小: {renderCamera.orthographicSize}");
            UpdateCameraPosition();
            
            // 强制渲染一帧
            ForceRender();
            
            // 强制切换到RenderTexture显示
            ForceRenderTextureDisplay();
        }
        
        /// <summary>
        /// 计算模型边界
        /// </summary>
        Bounds CalculateModelBounds(GameObject obj)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return new Bounds(Vector3.zero, Vector3.one);
                
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }
            return bounds;
        }
        
        /// <summary>
        /// 更新相机位置
        /// </summary>
        void UpdateCameraPosition()
        {
            if (cameraTransform == null) return;
            
            // 固定使用隔离空间中心，抬高与模型位置对应
            Vector3 modelCenter = new Vector3(1000f, 1002f, 1000f);
            if (currentSample != null)
            {
                Debug.Log($"[简化修复] 样本存在，使用固定隔离空间中心: {modelCenter}");
            }
            else
            {
                Debug.Log($"[简化修复] 无样本，使用默认隔离空间中心: {modelCenter}");
            }
            
            // 计算相机位置（相对于模型中心）
            Vector3 cameraPos = modelCenter + Quaternion.Euler(cameraRotation) * Vector3.forward * cameraDistance;
            cameraTransform.position = cameraPos;
            cameraTransform.LookAt(modelCenter);
            
            Debug.Log($"相机位置更新: 位置={cameraTransform.position}, 距离={cameraDistance}, 注视点={modelCenter}");
        }
        
        /// <summary>
        /// 递归设置层级
        /// </summary>
        void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                SetLayerRecursively(obj.transform.GetChild(i).gameObject, layer);
            }
        }
        
        /// <summary>
        /// 清除当前模型
        /// </summary>
        public void ClearCurrentModel()
        {
            if (currentSample != null)
            {
                DestroyImmediate(currentSample);
                currentSample = null;
                
                // 显示默认提示
                ShowDefaultPrompt();
            }
        }
        
        /// <summary>
        /// 隐藏默认提示
        /// </summary>
        void HideDefaultPrompt()
        {
            // 首先在当前组件中查找
            Transform prompt = transform.Find("DefaultPrompt");
            if (prompt == null)
            {
                // 然后在父级查找
                prompt = transform.parent?.Find("DefaultPrompt");
            }
            if (prompt == null)
            {
                // 递归查找所有子对象
                prompt = GetComponentInChildren<Transform>()?.Find("DefaultPrompt");
            }
            
            if (prompt != null)
            {
                prompt.gameObject.SetActive(false);
                Debug.Log($"默认提示已隐藏: {prompt.name}");
            }
            else
            {
                Debug.LogWarning("未找到DefaultPrompt，搜索所有子对象...");
                // 备用方案：搜索所有包含提示文字的Text组件
                Text[] texts = GetComponentsInChildren<Text>();
                foreach (Text text in texts)
                {
                    if (text.text.Contains("拖入样本") || text.text.Contains("3D模型"))
                    {
                        text.gameObject.SetActive(false);
                        Debug.Log($"找到并隐藏提示文字: {text.text}");
                    }
                }
            }
        }
        
        /// <summary>
        /// 显示默认提示
        /// </summary>
        void ShowDefaultPrompt()
        {
            Transform prompt = transform.parent.Find("DefaultPrompt");
            if (prompt != null)
            {
                prompt.gameObject.SetActive(true);
            }
        }
        
        void Update()
        {
            // 处理鼠标交互
            HandleMouseInteraction();
            
            // 自动旋转模型（当没有手动交互时）
            if (isAutoRotating && currentSample != null && !isMouseDown)
            {
                currentSample.transform.Rotate(0, autoRotationSpeed * Time.deltaTime, 0);
            }
        }
        
        /// <summary>
        /// 测试RawImage显示 - 设置纯色纹理
        /// </summary>
        public void TestRawImageDisplay()
        {
            if (rawImage != null)
            {
                // 创建一个红色纹理
                Texture2D testTexture = new Texture2D(512, 512);
                Color[] pixels = new Color[512 * 512];
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = Color.red;
                }
                testTexture.SetPixels(pixels);
                testTexture.Apply();
                
                rawImage.texture = testTexture;
                rawImage.color = Color.white;
                
                Debug.Log("测试纹理已设置到RawImage - 应该显示红色");
            }
            else
            {
                Debug.LogError("RawImage为空，无法测试");
            }
        }
        
        /// <summary>
        /// 强制切换到RenderTexture显示
        /// </summary>
        public void ForceRenderTextureDisplay()
        {
            if (rawImage != null && renderTexture != null)
            {
                // 强制清除之前的纹理
                rawImage.texture = null;
                
                // 等待一帧然后设置RenderTexture
                StartCoroutine(SetRenderTextureDelayed());
            }
        }
        
        private System.Collections.IEnumerator SetRenderTextureDelayed()
        {
            yield return null; // 等待一帧
            
            if (rawImage != null && renderTexture != null)
            {
                rawImage.texture = renderTexture;
                Debug.Log("延迟设置RenderTexture完成");
            }
        }
        
        /// <summary>
        /// 测试渲染系统 - 创建一个简单的测试立方体
        /// </summary>
        [System.Obsolete("仅用于调试")]
        public void CreateTestCube()
        {
            Debug.Log("创建测试立方体");
            
            // 清除现有模型
            ClearCurrentModel();
            
            // 创建测试立方体
            currentSample = GameObject.CreatePrimitive(PrimitiveType.Cube);
            currentSample.transform.SetParent(transform);
            currentSample.name = "TestCube";
            currentSample.transform.position = Vector3.zero;
            currentSample.transform.localScale = Vector3.one * 0.5f;
            
            // 设置明亮的材质
            Renderer renderer = currentSample.GetComponent<Renderer>();
            Material testMat = new Material(Shader.Find("Standard"));
            testMat.color = Color.red;
            testMat.SetFloat("_Metallic", 0.0f);
            testMat.SetFloat("_Glossiness", 0.5f);
            renderer.material = testMat;
            
            // 移除碰撞器
            DestroyImmediate(currentSample.GetComponent<Collider>());
            
            // 设置层级
            int previewLayer = LayerMask.NameToLayer("ModelPreview");
            if (previewLayer == -1) previewLayer = 0;
            SetLayerRecursively(currentSample, previewLayer);
            
            // 定位相机
            cameraDistance = 2f;
            UpdateCameraPosition();
            
            // 隐藏提示
            HideDefaultPrompt();
            
            // 强制渲染
            ForceRender();
            
            // 强制切换到RenderTexture显示
            ForceRenderTextureDisplay();
            
            Debug.Log("测试立方体创建完成");
        }
        
        /// <summary>
        /// 强制渲染一帧
        /// </summary>
        void ForceRender()
        {
            if (renderCamera != null && renderTexture != null)
            {
                // 确保相机激活并立即渲染
                renderCamera.enabled = true;
                renderCamera.Render();
                
                // 关键修复：强制更新RawImage的纹理引用
                if (rawImage != null)
                {
                    rawImage.texture = renderTexture;
                    Debug.Log("强制更新RawImage纹理引用到RenderTexture");
                }
                
                Debug.Log($"强制渲染完成 - 相机位置: {renderCamera.transform.position}, 目标物体数量: {FindObjectsOfType<Renderer>().Length}");
                
                // 验证RenderTexture是否有内容
                RenderTexture.active = renderTexture;
                Texture2D debugTexture = new Texture2D(renderTexture.width, renderTexture.height);
                debugTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                debugTexture.Apply();
                RenderTexture.active = null;
                
                // 检查纹理是否全黑（可能意味着没有渲染内容）
                Color[] pixels = debugTexture.GetPixels();
                bool hasContent = false;
                foreach (Color pixel in pixels)
                {
                    if (pixel.r > 0.3f || pixel.g > 0.3f || pixel.b > 0.3f)
                    {
                        hasContent = true;
                        break;
                    }
                }
                Debug.Log($"RenderTexture内容检查: {(hasContent ? "有内容" : "全黑/无内容")}");
                
                DestroyImmediate(debugTexture);
            }
            else
            {
                Debug.LogWarning($"无法强制渲染 - 相机: {renderCamera != null}, RenderTexture: {renderTexture != null}");
            }
        }
        
        /// <summary>
        /// 设置鼠标交互
        /// </summary>
        void SetupMouseInteraction()
        {
            // 为RawImage添加事件触发器组件以处理鼠标事件
            if (rawImage != null)
            {
                // 确保RawImage可以接收鼠标事件
                rawImage.raycastTarget = true;
                
                Debug.Log("鼠标交互系统已设置");
            }
        }
        
        /// <summary>
        /// 处理鼠标交互
        /// </summary>
        void HandleMouseInteraction()
        {
            if (!isMouseOverArea) return;
            
            // 使用新Input System处理鼠标输入
            var mouse = Mouse.current;
            if (mouse == null) return;
            
            // 检测鼠标按下
            if (mouse.leftButton.wasPressedThisFrame)
            {
                isMouseDown = true;
                lastMousePosition = mouse.position.ReadValue();
                isAutoRotating = false; // 暂停自动旋转
                Debug.Log("开始手动旋转 - 拖拽模型进行旋转");
            }
            
            // 检测鼠标释放
            if (mouse.leftButton.wasReleasedThisFrame)
            {
                isMouseDown = false;
                // 延迟3秒后恢复自动旋转
                CancelInvoke(nameof(ResumeAutoRotation));
                Invoke(nameof(ResumeAutoRotation), 3f);
                Debug.Log("结束手动旋转，3秒后恢复自动旋转");
            }
            
            // 处理拖拽旋转
            if (isMouseDown && currentSample != null)
            {
                Vector2 currentMousePosition = mouse.position.ReadValue();
                Vector2 mouseDelta = currentMousePosition - lastMousePosition;
                lastMousePosition = currentMousePosition;
                
                // 水平拖拽控制Y轴旋转
                float yRotation = mouseDelta.x * manualRotationSensitivity;
                // 垂直拖拽控制X轴旋转
                float xRotation = -mouseDelta.y * manualRotationSensitivity;
                
                currentSample.transform.Rotate(xRotation, yRotation, 0, Space.World);
                
                // 强制渲染更新
                if (Time.frameCount % 2 == 0) // 每两帧渲染一次以提高性能
                {
                    ForceRender();
                }
            }
            
            // 处理滚轮缩放
            Vector2 scrollDelta = mouse.scroll.ReadValue();
            float scroll = scrollDelta.y / 120f; // 转换为标准滚轮值
            if (Mathf.Abs(scroll) > 0.01f)
            {
                HandleZoom(scroll);
            }
        }
        
        /// <summary>
        /// 处理缩放
        /// </summary>
        void HandleZoom(float scrollDelta)
        {
            float zoomAmount = scrollDelta * zoomSensitivity;
            cameraDistance = Mathf.Clamp(cameraDistance - zoomAmount, minCameraDistance, maxCameraDistance);
            
            UpdateCameraPosition();
            ForceRender();
            
            Debug.Log($"缩放到距离: {cameraDistance:F2}");
        }
        
        /// <summary>
        /// 恢复自动旋转
        /// </summary>
        void ResumeAutoRotation()
        {
            if (!isMouseDown) // 只有在没有手动操作时才恢复
            {
                isAutoRotating = true;
                Debug.Log("恢复自动旋转");
            }
        }
        
        /// <summary>
        /// 重置模型视角
        /// </summary>
        public void ResetView()
        {
            if (currentSample != null)
            {
                currentSample.transform.rotation = Quaternion.identity;
            }
            
            cameraDistance = 2f;
            cameraRotation = new Vector3(0f, 0f, 0f);  // 重置为正视角度
            
            // 重置正交摄像机的显示大小
            if (renderCamera != null)
            {
                renderCamera.orthographicSize = 1.5f;  // 重置为默认正交大小
                Debug.Log($"重置正交显示大小为: {renderCamera.orthographicSize}");
            }
            
            UpdateCameraPosition();
            ForceRender();
            
            isAutoRotating = true;
            Debug.Log("视角已重置");
        }
        
        /// <summary>
        /// 开始/停止自动旋转
        /// </summary>
        public void ToggleAutoRotation()
        {
            isAutoRotating = !isAutoRotating;
            Debug.Log($"自动旋转: {(isAutoRotating ? "开启" : "关闭")}");
        }
        
        /// <summary>
        /// 设置鼠标悬停状态
        /// </summary>
        public void SetMouseOverArea(bool isOver)
        {
            isMouseOverArea = isOver;
            
            // 提供视觉反馈
            if (rawImage != null)
            {
                if (isOver)
                {
                    // 鼠标悬停时增加边框亮度
                    rawImage.color = new Color(1f, 1f, 1f, 1f);
                }
                else
                {
                    // 鼠标离开时恢复正常
                    rawImage.color = new Color(0.9f, 0.9f, 0.9f, 1f);
                    
                    // 停止手动操作
                    if (isMouseDown)
                    {
                        isMouseDown = false;
                        CancelInvoke(nameof(ResumeAutoRotation));
                        Invoke(nameof(ResumeAutoRotation), 1f);
                    }
                }
            }
            
            Debug.Log($"鼠标悬停状态: {(isOver ? "进入交互区域" : "离开交互区域")}");
        }
        
        void OnDestroy()
        {
            // 清理资源
            if (renderTexture != null)
            {
                renderTexture.Release();
                DestroyImmediate(renderTexture);
            }
        }
    }
}
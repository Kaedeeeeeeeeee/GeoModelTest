using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System;
using System.Linq;

namespace SampleCuttingSystem
{
    /// <summary>
    /// 样本投放区域
    /// 处理样本拖拽到切割区域的逻辑
    /// </summary>
    public class SampleDropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("投放区域设置")]
        [SerializeField] private Color normalColor = new Color(0f, 0f, 0f, 0.8f);
        [SerializeField] private Color highlightColor = new Color(0.2f, 1f, 0.4f, 0.9f);
        [SerializeField] private Color errorColor = new Color(1f, 0.2f, 0.2f, 0.8f);
        
        private Image backgroundImage;
        private Text instructionText;
        private bool isHighlighted = false;
        private SampleCuttingGame cuttingGame;
        private SampleDragHandler originalSampleHandler; // 保存原始样本的引用
        private List<GameObject> createdSampleObjects = new List<GameObject>(); // 保存切割过程中创建的所有样本对象
        
        void Awake()
        {
            backgroundImage = GetComponent<Image>();
            instructionText = GetComponentInChildren<Text>();
            
            if (backgroundImage == null)
            {
                backgroundImage = gameObject.AddComponent<Image>();
            }
            
            // 设置初始状态
            SetNormalState();
        }
        
        void Start()
        {
            // 在Start中查找或创建切割游戏组件
            EnsureCuttingGameComponent();
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            // 检查是否正在拖拽样本
            var dragHandler = GetDraggedSample(eventData);
            if (dragHandler != null)
            {
                Debug.Log("样本进入投放区域");
                SetHighlightState(dragHandler.IsMultiLayerSample());
            }
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            if (isHighlighted)
            {
                Debug.Log("样本离开投放区域");
                SetNormalState();
            }
        }
        
        public void OnDrop(PointerEventData eventData)
        {
            var dragHandler = GetDraggedSample(eventData);
            if (dragHandler != null)
            {
                Debug.Log($"样本投放到切割区域: {dragHandler.GetSampleData()?.name}");
                AcceptSample(dragHandler);
            }
        }
        
        /// <summary>
        /// 接受样本投放
        /// </summary>
        public bool AcceptSample(SampleDragHandler dragHandler)
        {
            if (dragHandler == null)
            {
                Debug.LogError("拖拽处理器为空");
                return false;
            }
            
            var sampleData = dragHandler.GetSampleData();
            if (sampleData == null)
            {
                Debug.LogError("样本数据为空");
                ShowError("无效的样本数据");
                return false;
            }
            
            // 检查是否为多层样本
            if (!dragHandler.IsMultiLayerSample())
            {
                Debug.LogWarning($"样本 {sampleData.name} 不是多层样本，显示分析结果");
                ShowSingleLayerAnalysis(sampleData, dragHandler);
                return true; // 接受单层样本，但进行分析而不是切割
            }

            // 验证通过，开始切割流程
            Debug.Log($"开始切割样本: {sampleData.name}");
            StartCuttingProcess(sampleData, dragHandler);
            
            return true;
        }

        /// <summary>
        /// 显示单层样本分析结果
        /// </summary>
        private void ShowSingleLayerAnalysis(SampleData sampleData, SampleDragHandler dragHandler)
        {
            Debug.Log($"分析单层样本: {sampleData.name}");

            // 显示分析信息
            string analysisInfo = $"样本名称: {sampleData.name}\n" +
                                $"层数: {sampleData.layerCount} (单层)\n" +
                                $"说明: {sampleData.description}\n\n" +
                                "单层样本无需切割，可直接进行地质分析。";

            ShowAnalysisInfo(analysisInfo);

            // 将样本返回到原位
            StartCoroutine(ReturnSampleAfterDelay(dragHandler, 2.0f));
        }

        /// <summary>
        /// 延迟后返回样本到原位
        /// </summary>
        private System.Collections.IEnumerator ReturnSampleAfterDelay(SampleDragHandler dragHandler, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (dragHandler != null)
            {
                dragHandler.RestoreOriginalState();
                Debug.Log("单层样本已返回原位置");
            }
        }

        /// <summary>
        /// 开始切割流程
        /// </summary>
        private void StartCuttingProcess(SampleData sampleData, SampleDragHandler dragHandler)
        {
            // 保存原始样本引用，用于切割完成后销毁
            originalSampleHandler = dragHandler;
            
            // 隐藏原始样本UI
            dragHandler.gameObject.SetActive(false);
            
            // 为兼容现有切割系统，创建ReconstructedSample
            var reconstructedSample = ConvertToReconstructedSample(sampleData, dragHandler);
            
            // 显示3D样本模型
            if (reconstructedSample != null)
            {
                Show3DSampleModel(reconstructedSample);
            }
            else
            {
                Debug.LogWarning("无法创建ReconstructedSample，3D模型显示可能异常");
            }
            
            // 切换到嵌入式切割游戏模式
            SwitchToEmbeddedCuttingMode(sampleData, reconstructedSample);
        }
        /// <summary>
        /// 显示3D样本模型（保留作为备用）
        /// </summary>
        private void Show3DSampleModel(GeometricSampleReconstructor.ReconstructedSample reconstructedSample)
        {
            // 查找3D模型显示器
            Sample3DModelViewer modelViewer = GetComponentInParent<Sample3DModelViewer>();
            
            if (modelViewer == null)
            {
                // 在父级中搜索
                modelViewer = transform.GetComponentInParent<Transform>()?.GetComponentInChildren<Sample3DModelViewer>();
            }
            
            if (modelViewer != null)
            {
                // 直接显示真实的重建样本
                Debug.Log("直接显示真实的重建样本，跳过测试序列");
                modelViewer.ShowReconstructedSample(reconstructedSample);
                Debug.Log("3D样本模型显示完成");
            }
            else
            {
                Debug.LogWarning("未找到3D模型显示器组件");
            }
        }
        
        /// <summary>
        /// 测试序列 - 依次测试不同的显示方式
        /// </summary>
        private System.Collections.IEnumerator ShowTestSequence(Sample3DModelViewer modelViewer, GeometricSampleReconstructor.ReconstructedSample reconstructedSample)
        {
            // 1秒后测试3D立方体
            yield return new WaitForSeconds(1f);
            Debug.Log("现在测试3D立方体渲染");
            modelViewer.CreateTestCube();
            
            // 再等2秒测试实际样本
            yield return new WaitForSeconds(2f);
            Debug.Log("现在显示实际的重建样本");
            modelViewer.ShowReconstructedSample(reconstructedSample);
            Debug.Log("3D样本模型已显示");
        }
        
        /// <summary>
        /// 延迟显示样本（给渲染系统时间初始化）
        /// </summary>
        private System.Collections.IEnumerator ShowSampleAfterDelay(Sample3DModelViewer modelViewer, GeometricSampleReconstructor.ReconstructedSample reconstructedSample)
        {
            // 等待几帧让测试立方体渲染完成
            yield return new WaitForSeconds(2f);
            
            // 显示实际的重建样本
            Debug.Log("现在显示实际的重建样本");
            modelViewer.ShowReconstructedSample(reconstructedSample);
            Debug.Log("3D样本模型已显示");
        }
        
        /// <summary>
        /// 清除3D样本模型
        /// </summary>
        private void Clear3DSampleModel()
        {
            // 查找3D模型显示器
            Sample3DModelViewer modelViewer = GetComponentInParent<Sample3DModelViewer>();
            
            if (modelViewer == null)
            {
                // 在父级中搜索
                modelViewer = transform.GetComponentInParent<Transform>()?.GetComponentInChildren<Sample3DModelViewer>();
            }
            
            if (modelViewer != null)
            {
                // 清除模型显示
                modelViewer.ClearCurrentModel();
                Debug.Log("3D样本模型已清除");
            }
        }
        
        /// <summary>
        /// 切换到嵌入式切割游戏模式
        /// </summary>
        private void SwitchToEmbeddedCuttingMode(SampleData sampleData, GeometricSampleReconstructor.ReconstructedSample reconstructedSample)
        {
            Debug.Log("切换到嵌入式切割游戏模式");
            
            // 保留黑色半透明背景，只隐藏中间的提示文字区域
            if (instructionText != null)
            {
                instructionText.gameObject.SetActive(false);
                Debug.Log("投放区域提示文字已隐藏");
            }
            
            // 查找并隐藏中间的灰色提示区域（深度搜索）
            Transform dropHint = transform.Find("DropHint");
            if (dropHint == null)
            {
                // 递归查找子对象
                dropHint = transform.GetComponentInChildren<Transform>().Find("DropHint");
            }
            
            if (dropHint != null)
            {
                dropHint.gameObject.SetActive(false);
                Debug.Log($"DropHint 灰色区域已隐藏: {dropHint.name}");
            }
            else
            {
                Debug.LogWarning("未找到 DropHint 对象，尝试隐藏所有子对象");
                // 作为备用方案，隐藏所有子对象（除了3D模型显示区域）
                foreach (Transform child in transform)
                {
                    if (child != null && child.gameObject != gameObject)
                    {
                        // 跳过3D模型显示区域
                        if (child.gameObject.name == "ModelViewArea")
                        {
                            Debug.Log($"保留3D模型显示区域: {child.gameObject.name}");
                            continue;
                        }
                        
                        // 检查是否包含文字或图像组件
                        Text textComp = child.GetComponent<Text>();
                        Image imageComp = child.GetComponent<Image>();
                        
                        if (textComp != null || imageComp != null)
                        {
                            child.gameObject.SetActive(false);
                            Debug.Log($"隐藏子对象: {child.gameObject.name} (包含: {(textComp ? "Text " : "")}{(imageComp ? "Image" : "")})");
                        }
                    }
                }
            }
            
            // 确保切割游戏组件存在
            EnsureCuttingGameComponent();
            
            // 启动嵌入式切割游戏
            if (cuttingGame != null)
            {
                Debug.Log($"启动嵌入式切割游戏，样本: {sampleData.name}");
                // 传递当前工作台位置给切割游戏
                Vector3 workstationPos = transform.position;
                Debug.Log($"SampleDropZone位置: {workstationPos}，尝试寻找真实实验台位置");
                
                // 尝试找到更准确的实验台位置
                Vector3 actualWorkstation = FindActualWorkstationPosition();
                if (actualWorkstation != Vector3.zero)
                {
                    workstationPos = actualWorkstation;
                    Debug.Log($"找到实际实验台位置: {workstationPos}");
                }
                else
                {
                    Debug.Log($"未找到实际实验台，使用SampleDropZone位置: {workstationPos}");
                }
                
                cuttingGame.StartCutting(reconstructedSample, workstationPos);
            }
            else
            {
                Debug.LogError("无法创建切割游戏组件");
                ShowError(LocalizationManager.Instance?.GetText("cutting_system.initialization_failed") ?? "切割系统初始化失败");
            }
        }
        
        /// <summary>
        /// 将SampleData转换为ReconstructedSample
        /// </summary>
        private GeometricSampleReconstructor.ReconstructedSample ConvertToReconstructedSample(SampleData sampleData, SampleDragHandler dragHandler)
        {
            try
            {
                Debug.Log($"开始转换样本数据: {sampleData?.name}");
                
                // 尝试从拖拽对象获取真实样本信息
                var realSample = ExtractRealSampleFromDragHandler(dragHandler);
                if (realSample != null)
                {
                    Debug.Log($"成功提取真实样本: {realSample.sampleID}");
                    return realSample;
                }
                
                // 如果无法提取真实样本，创建模拟样本
                Debug.Log("无法提取真实样本，创建模拟样本");
                var mockSample = CreateMockReconstructedSample(sampleData);
                if (mockSample != null)
                {
                    Debug.Log($"成功创建模拟样本: {mockSample.sampleID}");
                    return mockSample;
                }
                
                Debug.LogError("创建样本失败");
                return null;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"转换样本数据失败: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 从拖拽处理器中提取真实样本信息
        /// </summary>
        private GeometricSampleReconstructor.ReconstructedSample ExtractRealSampleFromDragHandler(SampleDragHandler dragHandler)
        {
            try
            {
                Debug.Log($"尝试从拖拽对象提取真实样本: {dragHandler.gameObject.name}");
                
                // 方法1: 检查是否有GeometricSampleInfo组件
                var sampleInfo = dragHandler.gameObject.GetComponent<GeometricSampleInfo>();
                if (sampleInfo != null)
                {
                    var reconstructedSample = sampleInfo.GetSampleData();
                    if (reconstructedSample != null)
                    {
                        Debug.Log($"从GeometricSampleInfo提取到样本: {reconstructedSample.sampleID}");
                        return reconstructedSample;
                    }
                }
                
                // 方法2: 检查父对象
                var parentSampleInfo = dragHandler.gameObject.GetComponentInParent<GeometricSampleInfo>();
                if (parentSampleInfo != null)
                {
                    var reconstructedSample = parentSampleInfo.GetSampleData();
                    if (reconstructedSample != null)
                    {
                        Debug.Log($"从父级GeometricSampleInfo提取到样本: {reconstructedSample.sampleID}");
                        return reconstructedSample;
                    }
                }
                
                // 方法3: 从WarehouseItemSlot获取SampleItem并重建
                var warehouseSlot = dragHandler.gameObject.GetComponent<WarehouseItemSlot>();
                if (warehouseSlot != null && warehouseSlot.HasItem())
                {
                    var sampleItem = warehouseSlot.GetItem();
                    if (sampleItem != null && sampleItem.layerCount > 1)
                    {
                        Debug.Log($"从WarehouseItemSlot获取SampleItem，准备重建: {sampleItem.displayName}, 层数: {sampleItem.layerCount}");
                        return ReconstructFromSampleItem(sampleItem);
                    }
                }
                
                Debug.LogWarning("无法从拖拽对象提取真实样本信息");
                return null;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"提取真实样本失败: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 从SampleItem重建ReconstructedSample（保持真实材质）
        /// </summary>
        private GeometricSampleReconstructor.ReconstructedSample ReconstructFromSampleItem(SampleItem sampleItem)
        {
            try
            {
                Debug.Log($"开始从SampleItem重建真实样本: {sampleItem.displayName}");
                Debug.Log($"样本信息 - 材质数据数量: {sampleItem.materialData?.Length ?? 0}, 地质层数量: {sampleItem.geologicalLayers?.Count ?? 0}, 总深度: {sampleItem.totalDepth}");
                
                // 使用SampleItem的RecreateOriginalModel方法获取真实材质
                Debug.Log("调用sampleItem.RecreateOriginalModel...");
                GameObject originalModel = sampleItem.RecreateOriginalModel(Vector3.zero);
                if (originalModel == null)
                {
                    Debug.LogError("RecreateOriginalModel返回null！这不应该发生");
                    return null;
                }
                else
                {
                    Debug.Log($"RecreateOriginalModel成功，返回对象: {originalModel.name}, 子对象数量: {originalModel.transform.childCount}");
                    
                    // 标记为临时切割对象，防止被样本收集系统识别
                    MarkAsTemporaryCuttingObject(originalModel);
                    
                    // 记录创建的模型对象，用于后续清理
                    createdSampleObjects.Add(originalModel);
                    Debug.Log($"已记录重建的样本模型用于清理: {originalModel.name}");
                }
                
                // 创建ReconstructedSample
                var reconstructedSample = new GeometricSampleReconstructor.ReconstructedSample();
                reconstructedSample.sampleID = sampleItem.sampleID;
                reconstructedSample.totalHeight = sampleItem.totalDepth;
                reconstructedSample.centerOfMass = new Vector3(0, sampleItem.totalDepth * 0.5f, 0);
                reconstructedSample.totalVolume = Mathf.PI * 0.1f * 0.1f * sampleItem.totalDepth; // 假设10cm半径
                
                // 从原始模型提取真实材质
                Renderer[] renderers = originalModel.GetComponentsInChildren<Renderer>();
                var layerSegments = new List<GeometricSampleReconstructor.LayerSegment>();
                
                // 使用真实的地质层信息
                if (sampleItem.geologicalLayers != null && sampleItem.geologicalLayers.Count > 0)
                {
                    float currentDepth = 0f;
                    
                    for (int i = 0; i < sampleItem.geologicalLayers.Count; i++)
                    {
                        var layerInfo = sampleItem.geologicalLayers[i];
                        var segment = new GeometricSampleReconstructor.LayerSegment();
                        
                        segment.relativeDepth = currentDepth;
                        segment.localCenterOfMass = new Vector3(0, currentDepth + layerInfo.thickness * 0.5f, 0);
                        
                        // 使用真实渲染器的材质
                        if (i < renderers.Length && renderers[i] != null && renderers[i].material != null)
                        {
                            segment.material = CreateDeepCopyMaterial(renderers[i].material);
                            segment.material.name = $"RealLayer_{i}_{layerInfo.layerName}";
                            Debug.Log($"使用真实材质: {segment.material.name}, 颜色: {segment.material.color}, MainTexture: {segment.material.mainTexture}");
                        }
                        else
                        {
                            Debug.LogWarning($"层级 {i} 没有对应的渲染器，使用默认材质");
                            segment.material = CreateDefaultMaterial(i, layerInfo.layerName);
                        }
                        
                        // 创建对应的GeologyLayer
                        segment.sourceLayer = CreateGeologyLayerFromLayerInfo(layerInfo, i);
                        
                        layerSegments.Add(segment);
                        currentDepth += layerInfo.thickness;
                        
                        Debug.Log($"重建层级 {i}: {layerInfo.layerName}, 深度: {currentDepth:F3}m, 厚度: {layerInfo.thickness:F3}m");
                    }
                }
                else
                {
                    Debug.LogWarning("SampleItem没有地质层信息，使用渲染器材质");
                    // 使用渲染器数量作为层数
                    for (int i = 0; i < renderers.Length; i++)
                    {
                        var segment = new GeometricSampleReconstructor.LayerSegment();
                        segment.relativeDepth = i * (sampleItem.totalDepth / renderers.Length);
                        segment.localCenterOfMass = new Vector3(0, segment.relativeDepth + (sampleItem.totalDepth / renderers.Length) * 0.5f, 0);
                        
                        if (renderers[i] != null && renderers[i].material != null)
                        {
                            segment.material = new Material(renderers[i].material);
                            segment.material.name = $"RealLayer_{i}";
                        }
                        else
                        {
                            segment.material = CreateDefaultMaterial(i, $"Layer_{i + 1}");
                        }
                        
                        segment.sourceLayer = CreateDefaultGeologyLayer(i, sampleItem.sampleID);
                        layerSegments.Add(segment);
                    }
                }
                
                reconstructedSample.layerSegments = layerSegments.ToArray();
                
                // 清理临时对象
                DestroyImmediate(originalModel);
                
                Debug.Log($"成功从SampleItem重建真实样本: {reconstructedSample.sampleID}, 层数: {layerSegments.Count}");
                return reconstructedSample;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"从SampleItem重建失败: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 从LayerInfo创建GeologyLayer
        /// </summary>
        private GeologyLayer CreateGeologyLayerFromLayerInfo(SampleItem.LayerInfo layerInfo, int index)
        {
            var layerObj = new GameObject($"RealLayer_{index}_{layerInfo.layerName}");
            var geologyLayer = layerObj.AddComponent<GeologyLayer>();
            
            geologyLayer.layerName = layerInfo.layerName;
            geologyLayer.description = $"真实地层: {layerInfo.layerName}, 厚度: {layerInfo.thickness:F2}m";
            geologyLayer.averageThickness = layerInfo.thickness;
            
            // 根据层级名称设置合适的颜色
            geologyLayer.layerColor = GetColorFromLayerName(layerInfo.layerName);
            
            DontDestroyOnLoad(layerObj);
            return geologyLayer;
        }
        
        /// <summary>
        /// 深度复制材质，包括所有纹理和属性
        /// </summary>
        private Material CreateDeepCopyMaterial(Material sourceMaterial)
        {
            if (sourceMaterial == null)
                return null;

            // 创建新材质实例
            Material newMaterial = new Material(sourceMaterial.shader);
            
            // 复制所有纹理属性
            if (sourceMaterial.mainTexture != null)
                newMaterial.mainTexture = sourceMaterial.mainTexture;
            
            if (sourceMaterial.HasProperty("_BumpMap") && sourceMaterial.GetTexture("_BumpMap") != null)
                newMaterial.SetTexture("_BumpMap", sourceMaterial.GetTexture("_BumpMap"));
            
            if (sourceMaterial.HasProperty("_MetallicGlossMap") && sourceMaterial.GetTexture("_MetallicGlossMap") != null)
                newMaterial.SetTexture("_MetallicGlossMap", sourceMaterial.GetTexture("_MetallicGlossMap"));
            
            if (sourceMaterial.HasProperty("_OcclusionMap") && sourceMaterial.GetTexture("_OcclusionMap") != null)
                newMaterial.SetTexture("_OcclusionMap", sourceMaterial.GetTexture("_OcclusionMap"));
            
            if (sourceMaterial.HasProperty("_EmissionMap") && sourceMaterial.GetTexture("_EmissionMap") != null)
                newMaterial.SetTexture("_EmissionMap", sourceMaterial.GetTexture("_EmissionMap"));
            
            // 复制颜色和数值属性
            newMaterial.color = sourceMaterial.color;
            
            if (sourceMaterial.HasProperty("_Metallic"))
                newMaterial.SetFloat("_Metallic", sourceMaterial.GetFloat("_Metallic"));
            
            if (sourceMaterial.HasProperty("_Glossiness"))
                newMaterial.SetFloat("_Glossiness", sourceMaterial.GetFloat("_Glossiness"));
            
            if (sourceMaterial.HasProperty("_BumpScale"))
                newMaterial.SetFloat("_BumpScale", sourceMaterial.GetFloat("_BumpScale"));
            
            if (sourceMaterial.HasProperty("_OcclusionStrength"))
                newMaterial.SetFloat("_OcclusionStrength", sourceMaterial.GetFloat("_OcclusionStrength"));
            
            if (sourceMaterial.HasProperty("_EmissionColor"))
                newMaterial.SetColor("_EmissionColor", sourceMaterial.GetColor("_EmissionColor"));

            Debug.Log($"深度复制材质: {sourceMaterial.name} -> {newMaterial.name}, Shader: {newMaterial.shader.name}, MainTexture: {newMaterial.mainTexture}");
            
            return newMaterial;
        }

        /// <summary>
        /// 根据层级名称获取颜色
        /// </summary>
        private Color GetColorFromLayerName(string layerName)
        {
            string name = layerName.ToLower();
            if (name.Contains("砂岩") || name.Contains("sandstone"))
                return new Color(0.9f, 0.8f, 0.6f, 1f);
            else if (name.Contains("页岩") || name.Contains("shale"))
                return new Color(0.4f, 0.4f, 0.4f, 1f);
            else if (name.Contains("石灰岩") || name.Contains("limestone"))
                return new Color(0.8f, 0.8f, 0.9f, 1f);
            else if (name.Contains("花岗岩") || name.Contains("granite"))
                return new Color(0.7f, 0.6f, 0.6f, 1f);
            else
                return Color.gray;
        }
        
        /// <summary>
        /// 创建默认材质
        /// </summary>
        private Material CreateDefaultMaterial(int index, string layerName)
        {
            var material = new Material(Shader.Find("Standard"));
            material.color = GetColorFromLayerName(layerName);
            material.name = $"DefaultMaterial_{layerName}_{index}";
            return material;
        }
        
        /// <summary>
        /// 创建默认GeologyLayer
        /// </summary>
        private GeologyLayer CreateDefaultGeologyLayer(int index, string sampleID)
        {
            var layerObj = new GameObject($"DefaultLayer_{index}_{sampleID}");
            var geologyLayer = layerObj.AddComponent<GeologyLayer>();
            
            geologyLayer.layerName = $"Layer_{index + 1}";
            geologyLayer.description = $"默认地层 {index + 1}";
            geologyLayer.averageThickness = 1f;
            geologyLayer.layerColor = GetLayerColor(index);
            
            DontDestroyOnLoad(layerObj);
            return geologyLayer;
        }
        
        /// <summary>
        /// 创建模拟的ReconstructedSample
        /// </summary>
        private GeometricSampleReconstructor.ReconstructedSample CreateMockReconstructedSample(SampleData sampleData)
        {
            // 创建一个基本的ReconstructedSample
            var mockSample = new GeometricSampleReconstructor.ReconstructedSample();
            
            // 设置基本属性
            mockSample.sampleID = sampleData.name;
            mockSample.totalHeight = 2f; // 默认2米高度
            mockSample.totalVolume = 0.1f * 0.1f * 2f; // 默认体积
            mockSample.centerOfMass = Vector3.zero;
            
            // 创建模拟的层级数据
            var layers = new System.Collections.Generic.List<GeometricSampleReconstructor.LayerSegment>();
            
            for (int i = 0; i < sampleData.layerCount; i++)
            {
                var layer = new GeometricSampleReconstructor.LayerSegment();
                layer.material = CreateMockMaterial(i);
                layer.relativeDepth = i * (2f / sampleData.layerCount);
                layer.localCenterOfMass = new Vector3(0, layer.relativeDepth + (1f / sampleData.layerCount), 0);
                
                // 创建模拟的GeologyLayer
                layer.sourceLayer = CreateMockGeologyLayer(i, sampleData.name, sampleData.layerCount);
                
                layers.Add(layer);
            }
            
            mockSample.layerSegments = layers.ToArray();
            
            Debug.Log($"创建模拟样本: {mockSample.sampleID}, 层级数: {layers.Count}");
            return mockSample;
        }
        
        /// <summary>
        /// 创建模拟材质
        /// </summary>
        private Material CreateMockMaterial(int layerIndex)
        {
            var material = new Material(Shader.Find("Standard"));
            
            // 根据层级索引设置不同颜色
            Color[] colors = {
                Color.red,
                Color.green,
                Color.blue,
                Color.yellow,
                Color.cyan,
                Color.magenta
            };
            
            material.color = colors[layerIndex % colors.Length];
            return material;
        }
        
        /// <summary>
        /// 创建模拟的GeologyLayer
        /// </summary>
        private GeologyLayer CreateMockGeologyLayer(int layerIndex, string sampleName, int totalLayerCount)
        {
            // 创建一个简单的GeologyLayer对象
            var layerObj = new GameObject($"MockLayer_{layerIndex}_{sampleName}");
            var geologyLayer = layerObj.AddComponent<GeologyLayer>();
            
            // 设置层级基本信息
            geologyLayer.layerName = $"Layer_{layerIndex + 1}";
            geologyLayer.layerColor = GetLayerColor(layerIndex);
            geologyLayer.averageThickness = 2f / totalLayerCount; // 根据总高度计算平均厚度
            geologyLayer.description = $"模拟地层 {layerIndex + 1}，来自样本 {sampleName}";
            
            // 防止被场景清理系统删除
            DontDestroyOnLoad(layerObj);
            
            Debug.Log($"创建模拟GeologyLayer: {geologyLayer.layerName}");
            
            return geologyLayer;
        }
        
        /// <summary>
        /// 获取层级颜色
        /// </summary>
        private Color GetLayerColor(int layerIndex)
        {
            Color[] colors = {
                Color.red,
                Color.green,  
                Color.blue,
                Color.yellow,
                Color.cyan,
                Color.magenta
            };
            
            return colors[layerIndex % colors.Length];
        }
        
        /// <summary>
        /// 更新UI显示切割状态
        /// </summary>
        private void UpdateUIForCutting(SampleData sampleData)
        {
            if (instructionText != null)
            {
                // 使用本地化文本
                var localizedText = instructionText.GetComponent<LocalizedText>();
                if (localizedText == null)
                {
                    localizedText = instructionText.gameObject.AddComponent<LocalizedText>();
                }
                localizedText.SetTextKey("cutting_system.instruction.cutting_in_progress", sampleData.name);
                instructionText.color = Color.yellow;
                instructionText.fontSize = 20;
            }
            
            // 改变背景颜色表示正在切割
            if (backgroundImage != null)
            {
                backgroundImage.color = new Color(0.8f, 0.8f, 0.2f, 0.9f); // 黄色背景
            }
        }
        
        /// <summary>
        /// 获取正在拖拽的样本
        /// </summary>
        private SampleDragHandler GetDraggedSample(PointerEventData eventData)
        {
            if (eventData.pointerDrag == null)
                return null;
                
            return eventData.pointerDrag.GetComponent<SampleDragHandler>();
        }
        
        /// <summary>
        /// 设置正常状态
        /// </summary>
        private void SetNormalState()
        {
            isHighlighted = false;
            if (backgroundImage != null)
            {
                backgroundImage.color = normalColor;
            }
            
            if (instructionText != null)
            {
                // 使用本地化文本
                var localizedText = instructionText.GetComponent<LocalizedText>();
                if (localizedText == null)
                {
                    localizedText = instructionText.gameObject.AddComponent<LocalizedText>();
                }
                localizedText.TextKey = "cutting_system.instruction.drag_sample";
                instructionText.color = Color.white;
                instructionText.fontSize = 18;
            }
        }
        
        /// <summary>
        /// 设置高亮状态
        /// </summary>
        private void SetHighlightState(bool canAccept)
        {
            isHighlighted = true;
            
            if (backgroundImage != null)
            {
                backgroundImage.color = canAccept ? highlightColor : errorColor;
            }
            
            if (instructionText != null)
            {
                if (canAccept)
                {
                    instructionText.text = "✅ 松开鼠标开始切割";
                    instructionText.color = Color.green;
                }
                else
                {
                    instructionText.text = "❌ 只能切割多层样本";
                    instructionText.color = Color.red;
                }
                instructionText.fontSize = 24;
            }
        }
        
        /// <summary>
        /// 显示错误提示
        /// </summary>
        private void ShowError(string message)
        {
            if (instructionText != null)
            {
                instructionText.text = $"❌ {message}";
                instructionText.color = Color.red;
            }
            
            if (backgroundImage != null)
            {
                backgroundImage.color = errorColor;
            }
            
            // 2秒后恢复正常状态
            Invoke(nameof(SetNormalState), 2f);
        }

        /// <summary>
        /// 显示分析信息
        /// </summary>
        private void ShowAnalysisInfo(string message)
        {
            if (instructionText != null)
            {
                instructionText.text = $"🔍 {message}";
                instructionText.color = Color.cyan;
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = new Color(0.2f, 0.8f, 1f, 0.3f); // 蓝色透明背景
            }

            Debug.Log($"[SampleDropZone] 显示分析信息: {message}");
        }

        /// <summary>
        /// 重置投放区域到初始状态
        /// </summary>
        public void ResetDropZone()
        {
            CancelInvoke(); // 取消任何待执行的恢复操作
            
            // 重新显示中间的灰色提示区域（深度搜索）
            Transform dropHint = transform.Find("DropHint");
            if (dropHint == null)
            {
                // 递归查找子对象
                dropHint = transform.GetComponentInChildren<Transform>().Find("DropHint");
            }
            
            if (dropHint != null)
            {
                dropHint.gameObject.SetActive(true);
                Debug.Log($"重置时 DropHint 灰色区域已重新显示: {dropHint.name}");
            }
            else
            {
                Debug.LogWarning("重置时未找到 DropHint 对象，尝试重新显示所有子对象");
                // 作为备用方案，重新显示所有子对象（除了3D模型显示区域）
                foreach (Transform child in transform)
                {
                    if (child != null && child.gameObject != gameObject)
                    {
                        // 跳过3D模型显示区域（它应该一直保持显示）
                        if (child.gameObject.name == "ModelViewArea")
                        {
                            continue;
                        }
                        
                        // 检查是否包含文字或图像组件
                        Text textComp = child.GetComponent<Text>();
                        Image imageComp = child.GetComponent<Image>();
                        
                        if (textComp != null || imageComp != null)
                        {
                            child.gameObject.SetActive(true);
                            Debug.Log($"重置时重新显示子对象: {child.gameObject.name} (包含: {(textComp ? "Text " : "")}{(imageComp ? "Image" : "")})");
                        }
                    }
                }
            }
            
            // 停止切割游戏，但不关闭整个GameObject（避免关闭实验台）
            if (cuttingGame != null)
            {
                // 只停止切割游戏逻辑，不关闭整个对象
                cuttingGame.StopCutting();
                Debug.Log("停止切割游戏逻辑，但保持实验台激活");
            }
            
            // 显示原始提示文本
            if (instructionText != null)
            {
                instructionText.gameObject.SetActive(true);
            }
            
            SetNormalState();
        }
        
        /// <summary>
        /// 切割完成回调
        /// </summary>
        public void OnCuttingComplete(bool success)
        {
            Debug.Log($"=== [SampleDropZone] OnCuttingComplete 被调用！success = {success} ===");
            Debug.Log($"切割完成回调: {success}");
            
            // 如果切割成功，关闭切割台UI
            if (success)
            {
                Debug.Log("[SampleDropZone] 切割成功，寻找切割台交互系统并关闭UI");
                var cuttingStationInteraction = FindFirstObjectByType<CuttingStationInteraction>();
                if (cuttingStationInteraction != null)
                {
                    Debug.Log("[SampleDropZone] 找到CuttingStationInteraction，调用关闭UI");
                    cuttingStationInteraction.CloseCuttingInterface();
                }
                else
                {
                    Debug.LogWarning("[SampleDropZone] 未找到CuttingStationInteraction，无法自动关闭UI");
                }
            }
            
            // 如果切割成功，销毁原始样本和所有创建的样本对象，避免重复收集
            if (success)
            {
                if (originalSampleHandler != null)
                {
                    Debug.Log($"切割成功，正在销毁原始样本: {originalSampleHandler.name}");
                    Destroy(originalSampleHandler.gameObject);
                    originalSampleHandler = null;
                }
                
                // 销毁所有在切割过程中创建的样本对象
                foreach (var sampleObj in createdSampleObjects)
                {
                    if (sampleObj != null)
                    {
                        Debug.Log($"切割成功，正在销毁重建的样本对象: {sampleObj.name}");
                        Destroy(sampleObj);
                    }
                }
                createdSampleObjects.Clear();
                
                // 额外的安全清理：搜索并销毁所有可能的原始样本对象
                CleanupOriginalSampleObjects();
            }
            else if (!success)
            {
                // 切割失败，恢复原始样本显示
                if (originalSampleHandler != null)
                {
                    Debug.Log($"切割失败，恢复原始样本显示: {originalSampleHandler.name}");
                    originalSampleHandler.gameObject.SetActive(true);
                    originalSampleHandler = null;
                }
                
                // 切割失败时也清理创建的对象
                foreach (var sampleObj in createdSampleObjects)
                {
                    if (sampleObj != null)
                    {
                        Debug.Log($"切割失败，清理重建的样本对象: {sampleObj.name}");
                        Destroy(sampleObj);
                    }
                }
                createdSampleObjects.Clear();
            }
            
            // 清除3D模型显示
            Clear3DSampleModel();
            
            // 自动关闭仓库UI
            CloseWarehouseUI();
            
            // 重新显示中间的灰色提示区域（深度搜索）
            Transform dropHint = transform.Find("DropHint");
            if (dropHint == null)
            {
                // 递归查找子对象
                dropHint = transform.GetComponentInChildren<Transform>().Find("DropHint");
            }
            
            if (dropHint != null)
            {
                dropHint.gameObject.SetActive(true);
                Debug.Log($"DropHint 灰色区域已重新显示: {dropHint.name}");
            }
            else
            {
                Debug.LogWarning("未找到 DropHint 对象，尝试重新显示所有子对象");
                // 作为备用方案，重新显示所有子对象（除了3D模型显示区域）
                foreach (Transform child in transform)
                {
                    if (child != null && child.gameObject != gameObject)
                    {
                        // 跳过3D模型显示区域（它应该一直保持显示）
                        if (child.gameObject.name == "ModelViewArea")
                        {
                            continue;
                        }
                        
                        // 检查是否包含文字或图像组件
                        Text textComp = child.GetComponent<Text>();
                        Image imageComp = child.GetComponent<Image>();
                        
                        if (textComp != null || imageComp != null)
                        {
                            child.gameObject.SetActive(true);
                            Debug.Log($"重新显示子对象: {child.gameObject.name} (包含: {(textComp ? "Text " : "")}{(imageComp ? "Image" : "")})");
                        }
                    }
                }
            }
            
            // 重置切割状态，确保可以重新使用
            ResetDropZoneState();
            
            Debug.Log("✅ 切割系统状态已完全重置，可以重新使用F键进入切割");
            
            // 停止切割游戏UI，但不关闭实验台
            if (cuttingGame != null)
            {
                cuttingGame.StopCutting();
                Debug.Log("重置时停止切割游戏，保持实验台激活");
            }
            
            // 显示结果提示
            if (instructionText != null)
            {
                instructionText.gameObject.SetActive(true);
                
                if (success)
                {
                    instructionText.text = LocalizationManager.Instance?.GetText("cutting_system.success_message") ?? "✅ 切割成功！\n\n单层样本已添加到背包\n可以继续切割其他样本";
                    instructionText.color = Color.green;
                }
                else
                {
                    instructionText.text = LocalizationManager.Instance?.GetText("cutting_system.failure_message") ?? "❌ 切割失败\n\n样本已被销毁\n请重新选择样本进行切割";
                    instructionText.color = Color.red;
                }
            }
            
            if (backgroundImage != null)
            {
                backgroundImage.color = success ? 
                    new Color(0.2f, 0.8f, 0.2f, 0.9f) : // 成功绿色
                    errorColor; // 失败红色
            }
            
            // 3秒后恢复正常状态
            Invoke(nameof(ResetDropZone), 3f);
        }
        
        /// <summary>
        /// 标记GameObject为临时切割对象，防止被样本收集系统识别
        /// </summary>
        private void MarkAsTemporaryCuttingObject(GameObject obj)
        {
            if (obj == null) return;
            
            // 移除SampleCollector组件，防止被收集
            var collector = obj.GetComponent<SampleCollector>();
            if (collector != null)
            {
                Debug.Log($"移除SampleCollector组件: {obj.name}");
                DestroyImmediate(collector);
            }
            
            // 移除所有与样本相关的MonoBehaviour组件
            var allComponents = obj.GetComponents<MonoBehaviour>();
            foreach (var component in allComponents)
            {
                if (component != null)
                {
                    // 检查是否是样本相关的组件
                    string componentName = component.GetType().Name;
                    if (componentName.Contains("Sample") && componentName != "SampleDropController")
                    {
                        Debug.Log($"移除样本相关组件: {componentName} 从 {obj.name}");
                        DestroyImmediate(component);
                    }
                }
            }
            
            // 添加临时标记标签
            obj.tag = "TemporaryCuttingObject";
            
            // 递归处理所有子对象
            foreach (Transform child in obj.transform)
            {
                MarkAsTemporaryCuttingObject(child.gameObject);
            }
            
            Debug.Log($"已标记为临时切割对象: {obj.name}");
        }
        
        /// <summary>
        /// 清理所有可能的原始样本对象
        /// </summary>
        private void CleanupOriginalSampleObjects()
        {
            // 搜索场景中所有SampleCollector组件（这些是可收集的样本对象）
            var allSampleCollectors = FindObjectsOfType<SampleCollector>();
            
            foreach (var collector in allSampleCollectors)
            {
                if (collector != null && collector.gameObject != null)
                {
                    // 获取样本数据（SampleItem是数据类，通过SampleCollector访问）
                    var sampleData = collector.sampleData;
                    if (sampleData != null)
                    {
                        // 检查是否是原始多层样本（layerCount > 1的样本）
                        if (sampleData.layerCount > 1)
                        {
                            Debug.Log($"清理原始多层样本: {sampleData.displayName} (层数: {sampleData.layerCount})");
                            Destroy(collector.gameObject);
                        }
                        // 检查是否是名称匹配的样本
                        else if (sampleData.displayName.Contains("地质样本_"))
                        {
                            Debug.Log($"清理地质样本对象: {sampleData.displayName}");
                            Destroy(collector.gameObject);
                        }
                    }
                }
            }
            
            // 清理所有临时切割对象 - 使用安全的查找方式
            try
            {
                var temporaryObjects = GameObject.FindGameObjectsWithTag("TemporaryCuttingObject");
                foreach (var tempObj in temporaryObjects)
                {
                    if (tempObj != null)
                    {
                        Debug.Log($"清理临时切割对象: {tempObj.name}");
                        Destroy(tempObj);
                    }
                }
            }
            catch (UnityException)
            {
                Debug.Log("TemporaryCuttingObject标签未定义，跳过临时对象清理");
            }
        }
        
        /// <summary>
        /// 重置投放区域状态
        /// </summary>
        private void ResetDropZoneState()
        {
            // 清理原始样本引用
            originalSampleHandler = null;
            
            // 清理创建的样本对象列表
            createdSampleObjects.Clear();
            
            // 重置背景色为正常状态
            if (backgroundImage != null)
            {
                backgroundImage.color = normalColor;
            }
            
            // 重新显示提示文字
            if (instructionText != null)
            {
                instructionText.gameObject.SetActive(true);
                instructionText.text = GetLocalizedDropZoneText();
                instructionText.color = Color.white; // 重置文字颜色
            }
            
            // 确保切割游戏组件重置
            SampleCuttingGame cuttingGame = GetComponentInChildren<SampleCuttingGame>();
            if (cuttingGame != null)
            {
                // 只重置切割逻辑，不关闭重开实验台（避免闪烁）
                cuttingGame.StopCutting();
                Debug.Log("切割游戏组件已重置，实验台保持激活");
            }
            
            // 重置高亮状态
            isHighlighted = false;
            
            Debug.Log("投放区域状态已重置为初始状态");
        }
        
        /// <summary>
        /// 获取本地化投放区域文字
        /// </summary>
        private string GetLocalizedDropZoneText()
        {
            var localizationManager = LocalizationManager.Instance;
            if (localizationManager != null)
            {
                return localizationManager.GetText("ui.cutting.dropzone.instruction");
            }
            return "将多层样本拖拽到此处进行切割"; // 默认文本
        }
        
        /// <summary>
        /// 关闭仓库UI
        /// </summary>
        private void CloseWarehouseUI()
        {
            WarehouseUI warehouseUI = FindFirstObjectByType<WarehouseUI>();
            if (warehouseUI != null)
            {
                warehouseUI.CloseWarehouseInterface();
                Debug.Log("切割完成，已自动关闭仓库UI");
            }
            else
            {
                Debug.LogWarning("未找到WarehouseUI组件，无法自动关闭仓库界面");
            }
        }
        
        /// <summary>
        /// 查找实际实验台位置
        /// </summary>
        private Vector3 FindActualWorkstationPosition()
        {
            // 方法1：查找切割台对象
            GameObject cuttingStation = GameObject.Find("LaboratoryCuttingStation");
            if (cuttingStation != null)
            {
                Debug.Log($"找到LaboratoryCuttingStation: {cuttingStation.transform.position}");
                return cuttingStation.transform.position;
            }
            
            // 方法2：查找实验台相关对象
            string[] workstationNames = {
                "CuttingStation", "WorkStation", "Table", "Bench", 
                "LabTable", "CuttingBench", "SampleTable"
            };
            
            foreach (string name in workstationNames)
            {
                GameObject station = GameObject.Find(name);
                if (station != null)
                {
                    Debug.Log($"找到工作台: {name} 位置: {station.transform.position}");
                    return station.transform.position;
                }
            }
            
            // 方法3：通过组件查找
            WarehouseTrigger warehouseTrigger = FindFirstObjectByType<WarehouseTrigger>();
            if (warehouseTrigger != null)
            {
                // 仓库附近可能就有实验台
                Debug.Log($"通过WarehouseTrigger推断实验台位置: {warehouseTrigger.transform.position}");
                return warehouseTrigger.transform.position;
            }
            
            // 方法4：查找任何包含"lab"或"cutting"的对象
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var obj in allObjects)
            {
                string objName = obj.name.ToLower();
                if (objName.Contains("lab") && (objName.Contains("table") || objName.Contains("bench") || objName.Contains("cutting")))
                {
                    Debug.Log($"找到实验台对象: {obj.name} 位置: {obj.transform.position}");
                    return obj.transform.position;
                }
            }
            
            Debug.LogWarning("未找到任何实验台对象");
            return Vector3.zero;
        }
        
        /// <summary>
        /// 确保切割游戏组件存在
        /// </summary>
        private void EnsureCuttingGameComponent()
        {
            if (cuttingGame != null)
                return;
                
            // 首先尝试从父级查找
            cuttingGame = GetComponentInParent<SampleCuttingGame>();
            
            if (cuttingGame == null)
            {
                // 在场景中查找
                cuttingGame = FindObjectOfType<SampleCuttingGame>();
            }
            
            if (cuttingGame == null)
            {
                // 创建新的切割游戏组件
                Debug.Log("创建新的切割游戏组件");
                
                // 寻找合适的父对象
                Transform parentTransform = transform.parent;
                if (parentTransform == null)
                {
                    // 如果没有父对象，创建一个
                    GameObject gameParent = new GameObject("CuttingGameContainer");
                    transform.SetParent(gameParent.transform);
                    parentTransform = gameParent.transform;
                }
                
                // 在父对象上添加切割游戏组件
                cuttingGame = parentTransform.gameObject.AddComponent<SampleCuttingGame>();
                Debug.Log("切割游戏组件创建成功");
            }
            else
            {
                Debug.Log("找到现有的切割游戏组件");
            }
        }
    }
}
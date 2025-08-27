using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 地质样本数据类 - 实现IInventoryItem接口
/// </summary>
[System.Serializable]
public class SampleItem : IInventoryItem
{
    [Header("基础信息")]
    public string sampleID;
    public string displayName;
    public string description;
    
    [Header("采集信息")]
    // 用于序列化的字符串格式时间
    [SerializeField] private string collectionTimeString;
    
    // 用于代码中访问的DateTime属性
    public DateTime collectionTime
    {
        get
        {
            if (string.IsNullOrEmpty(collectionTimeString))
            {
                return DateTime.Now;
            }
            
            if (DateTime.TryParseExact(collectionTimeString, "yyyy-MM-ddTHH:mm:ss.fffZ", 
                System.Globalization.CultureInfo.InvariantCulture, 
                System.Globalization.DateTimeStyles.RoundtripKind, out DateTime parsedTime))
            {
                return parsedTime;
            }
            else if (DateTime.TryParse(collectionTimeString, out DateTime fallbackTime))
            {
                return fallbackTime;
            }
            else
            {
                return DateTime.Now;
            }
        }
        set
        {
            collectionTimeString = value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            // 添加调试输出（仅在开发环境中）
            #if UNITY_EDITOR
            Debug.Log($"[SampleItem] 设置采集时间: {value:yyyy-MM-dd HH:mm:ss.fff} -> {collectionTimeString}");
            #endif
        }
    }
    
    public Vector3 originalCollectionPosition;
    public string collectorName = "Player";
    public string sourceToolID; // 来源工具ID (1000=简易钻探, 1001=钻塔)
    
    [Header("当前状态")]
    public SampleLocation currentLocation = SampleLocation.InInventory;
    public Vector3 currentWorldPosition;
    public bool isPlayerPlaced = false;
    
    [Header("地质数据")]
    public float totalDepth = 2.0f;
    public float sampleRadius = 0.1f;
    public int layerCount = 0;
    public List<LayerInfo> geologicalLayers = new List<LayerInfo>();
    
    [Header("采集深度信息")]
    public float depthStart = 0f; // 采集起始深度
    public float depthEnd = 2f;   // 采集结束深度
    public int drillIndex = 0;    // 钻探序号 (用于钻塔)
    
    [Header("视觉数据")]
    public Sprite previewIcon;
    public Texture2D previewImage;
    public string meshDataPath; // 3D模型数据路径
    
    [Header("原始模型数据")]
    [System.NonSerialized] // 防止序列化导致Unity对象标记问题
    public GameObject originalPrefab; // 原始样本预制件引用
    public SampleMaterialData[] materialData; // 存储原始材质数据
    public Vector3 originalScale; // 原始缩放
    public Quaternion originalRotation; // 原始旋转
    
    // IInventoryItem 接口实现
    public string ItemID => sampleID;
    public string ItemName => displayName;
    public ItemType Type => ItemType.GeologicalSample;
    public Sprite Icon => previewIcon;
    public bool CanStack => false; // 地质样本不能堆叠
    public int MaxStackSize => 1;
    public string Description => description;
    
    
    /// <summary>
    /// 地质层信息
    /// </summary>
    [System.Serializable]
    public class LayerInfo
    {
        public string layerName;
        public float thickness;
        public float depthStart;
        public float depthEnd;
        public Color layerColor;
        public string materialName;
        public string layerDescription;
    }
    
    /// <summary>
    /// 从地质样本GameObject创建SampleItem
    /// </summary>
    public static SampleItem CreateFromGeologicalSample(GameObject geologicalSample, string sourceToolID = "")
    {
        var item = new SampleItem();
        item.sampleID = GenerateUniqueSampleID();
        // 使用本地化的样本名称
        string samplePrefix = LocalizationManager.Instance?.GetText("sample.item.prefix") ?? "地质样本_";
        item.displayName = $"{samplePrefix}{item.sampleID[^8..]}";
        
        // 使用本地化的描述
        string toolName = GetLocalizedToolName(sourceToolID);
        string descriptionTemplate = LocalizationManager.Instance?.GetText("sample.description.template") ?? "使用{0}采集的地质样本";
        item.description = string.Format(descriptionTemplate, toolName);
        item.collectionTime = DateTime.Now;
        item.originalCollectionPosition = geologicalSample.transform.position;
        item.sourceToolID = sourceToolID;
        
        // 提取地质数据
        item.ExtractGeologicalData(geologicalSample);
        
        // 提取深度信息
        item.ExtractDepthInfo(geologicalSample);
        
        // 保存原始模型数据
        item.SaveOriginalModelData(geologicalSample);
        
        // 延迟生成预览图标，确保地质层数据已提取完成
        // 将在 SampleCollector.SetupSampleData() 中强制重新生成图标
        item.previewIcon = null; // 明确设置为null，强制后续重新生成
        
        return item;
    }
    
    /// <summary>
    /// 生成唯一样本ID
    /// </summary>
    private static string GenerateUniqueSampleID()
    {
        string guid = System.Guid.NewGuid().ToString("N")[..8].ToUpper();
        string timestamp = DateTime.Now.ToString("yyMMddHHmm");
        return $"GEO{timestamp}{guid}";
        // 例如: "GEO2507131430A1B2C3D4"
    }
    
    /// <summary>
    /// 根据工具ID获取工具名称
    /// </summary>
    private static string GetToolName(string toolID)
    {
        return toolID switch
        {
            "1000" => "简易钻探工具",
            "1001" => "钻塔工具",
            "1002" => "地质锤",
            _ => "未知工具"
        };
    }
    
    /// <summary>
    /// 获取本地化工具名称
    /// </summary>
    private static string GetLocalizedToolName(string toolID)
    {
        var localizationManager = LocalizationManager.Instance;
        if (localizationManager != null)
        {
            string key = toolID switch
            {
                "1000" => "tool.drill.simple.name",
                "1001" => "tool.drill_tower.name",
                "1002" => "tool.hammer.name",
                _ => "tool.unknown.name"
            };
            
            string localizedName = localizationManager.GetText(key);
            // 如果本地化文本存在，返回本地化版本
            if (!string.IsNullOrEmpty(localizedName) && !localizedName.StartsWith("[") && !localizedName.EndsWith("]"))
            {
                return localizedName;
            }
        }
        
        // 如果没有本地化系统或本地化失败，返回默认名称
        return GetToolName(toolID);
    }
    
    /// <summary>
    /// 从地质样本对象提取地质数据
    /// </summary>
    private void ExtractGeologicalData(GameObject geologicalSample)
    {
        // 尝试从 GeometricSampleInfo 组件获取重建样本数据
        var sampleInfo = geologicalSample.GetComponent<GeometricSampleInfo>();
        if (sampleInfo != null)
        {
            var reconstructedSample = sampleInfo.GetSampleData();
            if (reconstructedSample != null)
            {
                ExtractFromReconstructedSample(reconstructedSample);
                return;
            }
        }
        
        // 如果没有找到地质数据，使用默认值
        SetDefaultGeologicalData(geologicalSample);
    }
    
    /// <summary>
    /// 从重建样本提取数据
    /// </summary>
    private void ExtractFromReconstructedSample(GeometricSampleReconstructor.ReconstructedSample reconstructed)
    {
        if (reconstructed.layerSegments != null)
        {
            layerCount = reconstructed.layerSegments.Length;
            
            foreach (var segment in reconstructed.layerSegments)
            {
                if (segment?.sourceLayer != null)
                {
                    var layerInfo = new LayerInfo
                    {
                        layerName = segment.sourceLayer.layerName ?? "未知层",
                        thickness = CalculateLayerThickness(segment),
                        depthStart = segment.relativeDepth,
                        depthEnd = segment.relativeDepth + CalculateLayerThickness(segment),
                        layerColor = GetLayerColor(segment),
                        materialName = GetMaterialName(segment),
                        layerDescription = $"深度 {segment.relativeDepth:F2}m 的地质层"
                    };
                    geologicalLayers.Add(layerInfo);
                }
            }
        }
        
        totalDepth = reconstructed.totalHeight;
    }
    
    /// <summary>
    /// 设置默认地质数据
    /// </summary>
    private void SetDefaultGeologicalData(GameObject geologicalSample)
    {
        // 尝试从样本GameObject获取材质颜色
        Color extractedColor = ExtractColorFromSample(geologicalSample);
        
        layerCount = 1;
        var defaultLayer = new LayerInfo
        {
            layerName = "未命名地层",
            thickness = totalDepth,
            depthStart = 0f,
            depthEnd = totalDepth,
            layerColor = extractedColor,
            materialName = "Default",
            layerDescription = "从样本外观提取的默认地质层数据"
        };
        geologicalLayers.Add(defaultLayer);
        
        Debug.Log($"🔧 默认地质数据，颜色: #{ColorUtility.ToHtmlStringRGBA(extractedColor)}");
    }
    
    /// <summary>
    /// 从样本GameObject提取颜色
    /// </summary>
    private Color ExtractColorFromSample(GameObject geologicalSample)
    {
        if (geologicalSample != null)
        {
            // 尝试从MeshRenderer获取材质颜色
            MeshRenderer renderer = geologicalSample.GetComponent<MeshRenderer>();
            if (renderer != null && renderer.material != null)
            {
                Color materialColor = renderer.material.color;
                // 如果材质颜色不是白色，使用它
                if (materialColor != Color.white && materialColor.a > 0.1f)
                {
                    Debug.Log($"🎨 提取MeshRenderer颜色: #{ColorUtility.ToHtmlStringRGBA(materialColor)}");
                    return materialColor;
                }
            }
            
            // 尝试从子对象的MeshRenderer获取颜色
            MeshRenderer[] childRenderers = geologicalSample.GetComponentsInChildren<MeshRenderer>();
            foreach (var childRenderer in childRenderers)
            {
                if (childRenderer.material != null)
                {
                    Color childColor = childRenderer.material.color;
                    if (childColor != Color.white && childColor.a > 0.1f)
                    {
                        Debug.Log($"🎨 子对象颜色: #{ColorUtility.ToHtmlStringRGBA(childColor)}");
                        return childColor;
                    }
                }
            }
        }
        
        // 如果无法提取有效颜色，使用合理的地质层默认颜色
        Color defaultColor = new Color(0.6f, 0.4f, 0.2f); // 土褐色
        Debug.LogWarning($"⚠️ 无法提取颜色，默认: #{ColorUtility.ToHtmlStringRGBA(defaultColor)}");
        return defaultColor;
    }
    
    /// <summary>
    /// 提取样本深度信息
    /// </summary>
    private void ExtractDepthInfo(GameObject geologicalSample)
    {
        // 尝试从 DepthSampleMarker 组件获取钻塔深度信息
        var depthMarker = geologicalSample.GetComponent<DepthSampleMarker>();
        if (depthMarker != null)
        {
            depthStart = depthMarker.depthStart;
            depthEnd = depthMarker.depthEnd;
            drillIndex = depthMarker.drillIndex;
            Debug.Log($"提取到钻塔深度信息: {depthStart}m - {depthEnd}m (序号: {drillIndex})");
            return;
        }
        
        // 如果没有找到钻塔标记，检查是否是普通钻探工具 (sourceToolID = "1000")
        if (sourceToolID == "1000")
        {
            // 普通钻探工具始终是 0-2m
            depthStart = 0f;
            depthEnd = 2f;
            drillIndex = 0;
            #if UNITY_EDITOR && DEBUG_SAMPLE_DEPTH
            Debug.Log("设置普通钻探工具深度信息: 0m - 2m");
            #endif
            return;
        }
        
        // 如果是钻塔工具但没有深度标记，尝试从工具名称推断
        if (sourceToolID == "1001")
        {
            // 根据样本名称或其他信息推断深度，如果无法推断则使用默认
            depthStart = 0f;
            depthEnd = 2f;
            drillIndex = 0;
            #if UNITY_EDITOR && DEBUG_SAMPLE_DEPTH
            Debug.Log("钻塔工具无深度标记，使用默认深度信息: 0m - 2m");
            #endif
            return;
        }
        
        // 其他工具使用默认深度
        depthStart = 0f;
        depthEnd = totalDepth;
        drillIndex = 0;
        Debug.Log($"未知工具类型，使用默认深度信息: 0m - {totalDepth}m");
    }
    
    /// <summary>
    /// 计算图层厚度
    /// </summary>
    private float CalculateLayerThickness(GeometricSampleReconstructor.LayerSegment segment)
    {
        if (segment.geometry != null && segment.geometry.bounds.size.y > 0)
        {
            return segment.geometry.bounds.size.y;
        }
        return 0.1f; // 默认厚度
    }
    
    /// <summary>
    /// 获取图层颜色
    /// </summary>
    private Color GetLayerColor(GeometricSampleReconstructor.LayerSegment segment)
    {
        // 优先使用源地质层的颜色，这是真实的地质层颜色
        if (segment.sourceLayer != null)
        {
            Color layerColor = segment.sourceLayer.layerColor;
            
            // 如果地质层颜色不是默认的白色，使用它
            if (layerColor != Color.white && layerColor.a > 0.1f)
            {
                Debug.Log($"🎨 地质层颜色: {segment.sourceLayer.layerName} - #{ColorUtility.ToHtmlStringRGBA(layerColor)}");
                return layerColor;
            }
        }
        
        // 备选方案：检查材质颜色（但要避免白色材质）
        if (segment.material != null)
        {
            Color materialColor = segment.material.color;
            if (materialColor != Color.white && materialColor.a > 0.1f)
            {
                Debug.Log($"🎨 材质颜色: #{ColorUtility.ToHtmlStringRGBA(materialColor)}");
                return materialColor;
            }
        }
        
        // 最后备选：使用合理的默认颜色而不是纯白色
        Color defaultColor = new Color(0.6f, 0.4f, 0.2f); // 土褐色
        Debug.LogWarning($"⚠️ 使用默认颜色: #{ColorUtility.ToHtmlStringRGBA(defaultColor)}");
        return defaultColor;
    }
    
    /// <summary>
    /// 获取材质名称
    /// </summary>
    private string GetMaterialName(GeometricSampleReconstructor.LayerSegment segment)
    {
        if (segment.material != null)
        {
            return segment.material.name;
        }
        return "Unknown Material";
    }
    
    /// <summary>
    /// 生成预览图标
    /// </summary>
    private void GeneratePreviewIcon(GameObject geologicalSample)
    {
        // 使用新的动态图标生成系统
        if (SampleIconGenerator.Instance != null)
        {
            previewIcon = SampleIconGenerator.Instance.GenerateIconForSample(this);
            Debug.Log($"为样本 {displayName} 生成了动态图标");
        }
        else
        {
            Debug.LogWarning("SampleIconGenerator 实例未找到，使用默认图标");
            previewIcon = null;
        }
    }
    
    /// <summary>
    /// 获取样本详细信息
    /// </summary>
    public string GetDetailedInfo()
    {
        var localizationManager = LocalizationManager.Instance;
        
        string info = "";
        if (localizationManager != null)
        {
            // 使用本地化文本
            info += localizationManager.GetText("sample.info.id", sampleID) + "\n";
            info += localizationManager.GetText("sample.info.name", displayName) + "\n";
            info += localizationManager.GetText("sample.info.collection_time", collectionTime.ToString("yyyy-MM-dd HH:mm:ss")) + "\n";
            info += localizationManager.GetText("sample.info.collection_position", 
                originalCollectionPosition.x.ToString("F2"), 
                originalCollectionPosition.y.ToString("F2"), 
                originalCollectionPosition.z.ToString("F2")) + "\n";
            info += localizationManager.GetText("sample.info.collection_tool", GetLocalizedToolName(sourceToolID)) + "\n";
            // 使用世界坐标深度计算系统
            string depthInfo = WorldDepthCalculator.GetLocalizedDepthDescription(
                originalCollectionPosition, depthStart, depthEnd, true);
            info += depthInfo + "\n";
            info += localizationManager.GetText("sample.info.layer_count", layerCount.ToString()) + "\n";
            
            string statusText = currentLocation == SampleLocation.InInventory ? 
                localizationManager.GetText("sample.status.inventory") : 
                localizationManager.GetText("sample.status.world");
            info += localizationManager.GetText("sample.info.current_status", statusText) + "\n";
            
            if (geologicalLayers.Count > 0)
            {
                info += "\n" + localizationManager.GetText("sample.info.layer_details") + "\n";
                for (int i = 0; i < geologicalLayers.Count; i++)
                {
                    var layer = geologicalLayers[i];
                    info += localizationManager.GetText("sample.info.layer_item", 
                        (i + 1).ToString(), 
                        layer.layerName, 
                        layer.thickness.ToString("F2")) + "\n";
                }
            }
        }
        else
        {
            // 如果没有本地化系统，使用默认中文
            info = $"样本ID: {sampleID}\n";
            info += $"名称: {displayName}\n";
            info += $"采集时间: {collectionTime:yyyy-MM-dd HH:mm:ss}\n";
            info += $"采集位置: ({originalCollectionPosition.x:F2}, {originalCollectionPosition.y:F2}, {originalCollectionPosition.z:F2})\n";
            info += $"采集工具: {GetToolName(sourceToolID)}\n";
            // 使用世界坐标深度计算系统（默认版本）
            var (worldDepthStart, worldDepthEnd) = WorldDepthCalculator.CalculateWorldDepthRange(
                originalCollectionPosition, depthStart, depthEnd);
            info += $"采集深度: {worldDepthStart:F1}m - {worldDepthEnd:F1}m (相对: {depthStart:F1}m - {depthEnd:F1}m)\n";
            info += $"地质层数: {layerCount}\n";
            info += $"当前状态: {(currentLocation == SampleLocation.InInventory ? "背包中" : "世界中")}\n";
            
            if (geologicalLayers.Count > 0)
            {
                info += "\n地质层详情:\n";
                for (int i = 0; i < geologicalLayers.Count; i++)
                {
                    var layer = geologicalLayers[i];
                    info += $"  {i + 1}. {layer.layerName} - 厚度: {layer.thickness:F2}m\n";
                }
            }
        }
        
        return info;
    }
    
    /// <summary>
    /// 保存原始几何模型数据 - 简化版本，只保存材质数据
    /// </summary>
    public void SaveOriginalModelData(GameObject originalSample)
    {
        if (originalSample == null) return;
        
        // 保存基础变换
        originalScale = originalSample.transform.localScale;
        originalRotation = originalSample.transform.rotation;
        
        // 获取所有MeshRenderer
        MeshRenderer[] renderers = originalSample.GetComponentsInChildren<MeshRenderer>();
        
        if (renderers.Length > 0)
        {
            List<SampleMaterialData> materialList = new List<SampleMaterialData>();
            
            // 只保存材质数据
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMaterial != null)
                {
                    var materialInfo = new SampleMaterialData
                    {
                        materialName = renderer.sharedMaterial.name,
                        shaderName = renderer.sharedMaterial.shader.name,
                        mainTexture = renderer.sharedMaterial.mainTexture as Texture2D,
                        bumpMap = renderer.sharedMaterial.HasProperty("_BumpMap") ? renderer.sharedMaterial.GetTexture("_BumpMap") as Texture2D : null,
                        metallicGlossMap = renderer.sharedMaterial.HasProperty("_MetallicGlossMap") ? renderer.sharedMaterial.GetTexture("_MetallicGlossMap") as Texture2D : null,
                        occlusionMap = renderer.sharedMaterial.HasProperty("_OcclusionMap") ? renderer.sharedMaterial.GetTexture("_OcclusionMap") as Texture2D : null,
                        emissionMap = renderer.sharedMaterial.HasProperty("_EmissionMap") ? renderer.sharedMaterial.GetTexture("_EmissionMap") as Texture2D : null,
                        color = renderer.sharedMaterial.color,
                        metallic = renderer.sharedMaterial.HasProperty("_Metallic") ? renderer.sharedMaterial.GetFloat("_Metallic") : 0f,
                        smoothness = renderer.sharedMaterial.HasProperty("_Smoothness") ? renderer.sharedMaterial.GetFloat("_Smoothness") : 0f,
                        bumpScale = renderer.sharedMaterial.HasProperty("_BumpScale") ? renderer.sharedMaterial.GetFloat("_BumpScale") : 1f,
                        occlusionStrength = renderer.sharedMaterial.HasProperty("_OcclusionStrength") ? renderer.sharedMaterial.GetFloat("_OcclusionStrength") : 1f,
                        emission = renderer.sharedMaterial.HasProperty("_EmissionColor") ? renderer.sharedMaterial.GetColor("_EmissionColor") : Color.black,
                        mainTextureScale = renderer.sharedMaterial.mainTextureScale,
                        mainTextureOffset = renderer.sharedMaterial.mainTextureOffset
                    };
                    materialList.Add(materialInfo);
                    Debug.Log($"保存材质数据: {materialInfo.materialName}, 主纹理: {materialInfo.mainTexture?.name ?? "无"}");
                }
            }
            
            materialData = materialList.ToArray();
            Debug.Log($"已保存 {materialData.Length} 个材质的数据");
        }
    }
    
    /// <summary>
    /// 重建原始几何模型
    /// </summary>
    public GameObject RecreateOriginalModel(Vector3 position)
    {
        // 创建根对象
        GameObject sampleRoot = new GameObject($"ReconstructedSample_{sampleID}");
        sampleRoot.transform.position = position;
        sampleRoot.transform.rotation = originalRotation;
        sampleRoot.transform.localScale = originalScale;
        
        // 统一使用材质数据重建样本
        if (materialData != null && materialData.Length > 0)
        {
            // 为每个材质层创建一个几何体
            for (int i = 0; i < materialData.Length; i++)
            {
                var materialInfo = materialData[i];
                
                // 创建圆柱体几何体（标准地质样本形状）
                GameObject layerObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                layerObj.name = $"Layer_{i}_{materialInfo.materialName}";
                layerObj.transform.SetParent(sampleRoot.transform);
                
                // 设置层级位置和大小
                float layerHeight = totalDepth / materialData.Length;
                float yOffset = (i - materialData.Length / 2f + 0.5f) * layerHeight;
                layerObj.transform.localPosition = new Vector3(0, yOffset, 0);
                layerObj.transform.localScale = new Vector3(sampleRadius * 2, layerHeight * 0.5f, sampleRadius * 2);
                
                // 应用完整的材质数据
                Renderer renderer = layerObj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material recreatedMaterial = CreateMaterialFromData(materialInfo);
                    renderer.material = recreatedMaterial;
                }
            }
        }
        else
        {
            // 如果没有材质数据，使用地质层颜色创建默认样本
            GameObject defaultObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            defaultObj.name = "DefaultSample";
            defaultObj.transform.SetParent(sampleRoot.transform);
            defaultObj.transform.localPosition = Vector3.zero;
            defaultObj.transform.localScale = new Vector3(sampleRadius * 2, totalDepth * 0.5f, sampleRadius * 2);
            
            Renderer renderer = defaultObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material defaultMaterial = new Material(Shader.Find("Standard"));
                defaultMaterial.color = geologicalLayers.Count > 0 ? geologicalLayers[0].layerColor : new Color(0.7f, 0.5f, 0.3f);
                defaultMaterial.name = "DefaultSampleMaterial";
                renderer.material = defaultMaterial;
            }
        }
        
        Debug.Log($"重建样本模型 {displayName}，包含 {materialData?.Length ?? 1} 个材质层");
        return sampleRoot;
    }

    /// <summary>
    /// 从SampleMaterialData创建完整的材质对象
    /// </summary>
    private Material CreateMaterialFromData(SampleMaterialData materialInfo)
    {
        // 创建材质对象
        Material material = new Material(Shader.Find(materialInfo.shaderName));
        material.name = materialInfo.materialName;
        
        // 设置基本颜色
        material.color = materialInfo.color;
        
        // 设置主纹理和UV参数
        if (materialInfo.mainTexture != null)
        {
            material.mainTexture = materialInfo.mainTexture;
            material.mainTextureScale = materialInfo.mainTextureScale;
            material.mainTextureOffset = materialInfo.mainTextureOffset;
        }
        
        // 设置法线贴图
        if (material.HasProperty("_BumpMap") && materialInfo.bumpMap != null)
        {
            material.SetTexture("_BumpMap", materialInfo.bumpMap);
            if (material.HasProperty("_BumpScale"))
                material.SetFloat("_BumpScale", materialInfo.bumpScale);
        }
        
        // 设置金属度/光滑度贴图
        if (material.HasProperty("_MetallicGlossMap") && materialInfo.metallicGlossMap != null)
        {
            material.SetTexture("_MetallicGlossMap", materialInfo.metallicGlossMap);
        }
        
        // 设置遮挡贴图
        if (material.HasProperty("_OcclusionMap") && materialInfo.occlusionMap != null)
        {
            material.SetTexture("_OcclusionMap", materialInfo.occlusionMap);
            if (material.HasProperty("_OcclusionStrength"))
                material.SetFloat("_OcclusionStrength", materialInfo.occlusionStrength);
        }
        
        // 设置自发光贴图
        if (material.HasProperty("_EmissionMap") && materialInfo.emissionMap != null)
        {
            material.SetTexture("_EmissionMap", materialInfo.emissionMap);
        }
        
        // 设置数值属性
        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", materialInfo.metallic);
        
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", materialInfo.smoothness);
        
        if (material.HasProperty("_EmissionColor"))
            material.SetColor("_EmissionColor", materialInfo.emission);
            
        Debug.Log($"从数据重建材质: {materialInfo.materialName}, 着色器: {materialInfo.shaderName}, 主纹理: {materialInfo.mainTexture?.name ?? "无"}");
        
        return material;
    }
}


/// <summary>
/// 样本材质数据存储结构 - 增强版本，支持完整材质数据
/// </summary>
[System.Serializable]
public struct SampleMaterialData
{
    public string materialName;
    public string shaderName;
    public Texture2D mainTexture;
    public Texture2D bumpMap;           // 法线贴图
    public Texture2D metallicGlossMap;  // 金属度/光滑度贴图
    public Texture2D occlusionMap;      // 遮挡贴图
    public Texture2D emissionMap;       // 自发光贴图
    public Color color;
    public float metallic;
    public float smoothness;
    public float bumpScale;             // 法线强度
    public float occlusionStrength;     // 遮挡强度
    public Color emission;
    
    // 纹理平铺和偏移信息
    public Vector2 mainTextureScale;
    public Vector2 mainTextureOffset;
}
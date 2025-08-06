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
    public SampleMeshData[] meshData; // 存储原始网格数据
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
    /// 保存原始几何模型数据
    /// </summary>
    public void SaveOriginalModelData(GameObject originalSample)
    {
        if (originalSample == null) return;
        
        // 保存基础变换
        originalScale = originalSample.transform.localScale;
        originalRotation = originalSample.transform.rotation;
        
        // 获取所有MeshRenderer和MeshFilter
        MeshRenderer[] renderers = originalSample.GetComponentsInChildren<MeshRenderer>();
        MeshFilter[] filters = originalSample.GetComponentsInChildren<MeshFilter>();
        
        if (renderers.Length > 0)
        {
            // 保存网格数据
            List<SampleMeshData> meshList = new List<SampleMeshData>();
            List<SampleMaterialData> materialList = new List<SampleMaterialData>();
            
            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                var filter = i < filters.Length ? filters[i] : null;
                
                if (filter != null && filter.sharedMesh != null)
                {
                    // 尝试保存网格数据，如果不可读取则创建可读副本
                    SampleMeshData meshInfo = new SampleMeshData();
                    meshInfo.meshName = filter.sharedMesh.name;
                    meshInfo.localPosition = renderer.transform.localPosition;
                    meshInfo.localRotation = renderer.transform.localRotation;
                    meshInfo.localScale = renderer.transform.localScale;
                    meshInfo.bounds = filter.sharedMesh.bounds;
                    
                    try
                    {
                        // 尝试直接访问网格数据
                        if (filter.sharedMesh.isReadable)
                        {
                            meshInfo.vertices = filter.sharedMesh.vertices;
                            meshInfo.triangles = filter.sharedMesh.triangles;
                            meshInfo.normals = filter.sharedMesh.normals;
                            meshInfo.uv = filter.sharedMesh.uv;
                            Debug.Log($"成功保存可读网格数据: {filter.sharedMesh.name}");
                        }
                        else
                        {
                            // 网格不可读，创建可读副本
                            Mesh readableMesh = CreateReadableMesh(filter.sharedMesh);
                            if (readableMesh != null)
                            {
                                meshInfo.vertices = readableMesh.vertices;
                                meshInfo.triangles = readableMesh.triangles;
                                meshInfo.normals = readableMesh.normals;
                                meshInfo.uv = readableMesh.uv;
                                UnityEngine.Object.DestroyImmediate(readableMesh); // 清理临时网格
                                Debug.Log($"成功创建并保存可读网格副本: {filter.sharedMesh.name}");
                            }
                            else
                            {
                                // 无法创建可读副本，保存基本信息
                                Debug.LogWarning($"无法读取网格数据: {filter.sharedMesh.name}，仅保存基本信息");
                                meshInfo.vertices = new Vector3[0];
                                meshInfo.triangles = new int[0];
                                meshInfo.normals = new Vector3[0];
                                meshInfo.uv = new Vector2[0];
                            }
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"保存网格数据时发生错误: {e.Message}，使用空数据");
                        meshInfo.vertices = new Vector3[0];
                        meshInfo.triangles = new int[0];
                        meshInfo.normals = new Vector3[0];
                        meshInfo.uv = new Vector2[0];
                    }
                    
                    meshList.Add(meshInfo);
                    
                    // 保存材质数据
                    if (renderer.sharedMaterial != null)
                    {
                        var materialInfo = new SampleMaterialData
                        {
                            materialName = renderer.sharedMaterial.name,
                            shaderName = renderer.sharedMaterial.shader.name,
                            mainTexture = renderer.sharedMaterial.mainTexture as Texture2D,
                            color = renderer.sharedMaterial.color,
                            metallic = renderer.sharedMaterial.HasProperty("_Metallic") ? renderer.sharedMaterial.GetFloat("_Metallic") : 0f,
                            smoothness = renderer.sharedMaterial.HasProperty("_Smoothness") ? renderer.sharedMaterial.GetFloat("_Smoothness") : 0f,
                            emission = renderer.sharedMaterial.HasProperty("_EmissionColor") ? renderer.sharedMaterial.GetColor("_EmissionColor") : Color.black
                        };
                        materialList.Add(materialInfo);
                    }
                }
            }
            
            meshData = meshList.ToArray();
            materialData = materialList.ToArray();
            
            Debug.Log($"已保存 {meshData.Length} 个网格和 {materialData.Length} 个材质的数据");
        }
    }
    
    /// <summary>
    /// 创建可读的网格副本
    /// </summary>
    private Mesh CreateReadableMesh(Mesh originalMesh)
    {
        try
        {
            // 方法1: 使用Instantiate创建副本
            Mesh readableMesh = UnityEngine.Object.Instantiate(originalMesh);
            readableMesh.name = originalMesh.name + "_Readable";
            
            // 检查副本是否可读
            if (readableMesh.isReadable)
            {
                return readableMesh;
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(readableMesh);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"方法1创建可读网格失败: {e.Message}");
        }
        
        // 方法2: 使用GPU读回技术（创建简化几何体）
        try
        {
            return CreateReadableMeshFromGPU(originalMesh);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"方法2创建可读网格失败: {e.Message}");
        }
        
        return null;
    }
    
    /// <summary>
    /// 使用GPU读回创建可读网格
    /// </summary>
    private Mesh CreateReadableMeshFromGPU(Mesh originalMesh)
    {
        // 创建新的可读网格
        Mesh readableMesh = new Mesh();
        readableMesh.name = originalMesh.name + "_GPUReadable";
        
        // 备用方案：基于bounds信息创建简化几何体
        Bounds bounds = originalMesh.bounds;
        
        // 创建简化的立方体网格来近似原始形状
        Vector3[] vertices = new Vector3[]
        {
            // 立方体的8个顶点，基于原始网格的bounds
            new Vector3(bounds.min.x, bounds.min.y, bounds.min.z),
            new Vector3(bounds.max.x, bounds.min.y, bounds.min.z),
            new Vector3(bounds.max.x, bounds.max.y, bounds.min.z),
            new Vector3(bounds.min.x, bounds.max.y, bounds.min.z),
            new Vector3(bounds.min.x, bounds.min.y, bounds.max.z),
            new Vector3(bounds.max.x, bounds.min.y, bounds.max.z),
            new Vector3(bounds.max.x, bounds.max.y, bounds.max.z),
            new Vector3(bounds.min.x, bounds.max.y, bounds.max.z)
        };
        
        int[] triangles = new int[]
        {
            // 立方体的12个三角形（36个索引）
            0, 2, 1, 0, 3, 2, // 前面
            2, 3, 6, 3, 7, 6, // 顶面
            1, 2, 5, 2, 6, 5, // 右面
            0, 7, 3, 0, 4, 7, // 左面
            5, 6, 4, 6, 7, 4, // 后面
            0, 1, 4, 1, 5, 4  // 底面
        };
        
        Vector3[] normals = new Vector3[vertices.Length];
        Vector2[] uv = new Vector2[vertices.Length];
        
        // 计算法线
        for (int i = 0; i < vertices.Length; i++)
        {
            normals[i] = vertices[i].normalized;
            uv[i] = new Vector2((vertices[i].x - bounds.min.x) / bounds.size.x, 
                               (vertices[i].z - bounds.min.z) / bounds.size.z);
        }
        
        readableMesh.vertices = vertices;
        readableMesh.triangles = triangles;
        readableMesh.normals = normals;
        readableMesh.uv = uv;
        readableMesh.bounds = bounds;
        
        Debug.Log($"创建了基于bounds的简化网格: {readableMesh.name}");
        return readableMesh;
    }
    
    /// <summary>
    /// 重建原始几何模型
    /// </summary>
    public GameObject RecreateOriginalModel(Vector3 position)
    {
        if (meshData == null || meshData.Length == 0)
        {
            Debug.LogWarning($"样本 {displayName} 没有保存的网格数据，创建备用圆柱体模型");
            return CreateFallbackModel(position);
        }
        
        // 检查是否有有效的网格数据
        bool hasValidMeshData = false;
        foreach (var mesh in meshData)
        {
            if (mesh.vertices != null && mesh.vertices.Length > 0)
            {
                hasValidMeshData = true;
                break;
            }
        }
        
        if (!hasValidMeshData)
        {
            Debug.LogWarning($"样本 {displayName} 的网格数据为空（可能由于网格不可读），创建基于材质的备用模型");
            return CreateMaterialBasedFallbackModel(position);
        }
        
        // 创建根对象
        GameObject sampleRoot = new GameObject($"ReconstructedSample_{sampleID}");
        sampleRoot.transform.position = position;
        sampleRoot.transform.rotation = originalRotation;
        sampleRoot.transform.localScale = originalScale;
        
        // 重建每个网格组件
        for (int i = 0; i < meshData.Length; i++)
        {
            var meshInfo = meshData[i];
            
            // 创建子对象
            GameObject meshObj = new GameObject($"Mesh_{i}_{meshInfo.meshName}");
            meshObj.transform.SetParent(sampleRoot.transform);
            meshObj.transform.localPosition = meshInfo.localPosition;
            meshObj.transform.localRotation = meshInfo.localRotation;
            meshObj.transform.localScale = meshInfo.localScale;
            
            // 重建网格
            Mesh recreatedMesh = new Mesh();
            recreatedMesh.name = meshInfo.meshName;
            recreatedMesh.vertices = meshInfo.vertices;
            recreatedMesh.triangles = meshInfo.triangles;
            recreatedMesh.normals = meshInfo.normals;
            recreatedMesh.uv = meshInfo.uv;
            recreatedMesh.bounds = meshInfo.bounds;
            
            // 添加MeshFilter和MeshRenderer
            MeshFilter meshFilter = meshObj.AddComponent<MeshFilter>();
            meshFilter.mesh = recreatedMesh;
            
            MeshRenderer meshRenderer = meshObj.AddComponent<MeshRenderer>();
            
            // 重建材质
            if (i < materialData.Length)
            {
                var materialInfo = materialData[i];
                Material recreatedMaterial = new Material(Shader.Find(materialInfo.shaderName));
                
                recreatedMaterial.name = materialInfo.materialName;
                recreatedMaterial.color = materialInfo.color;
                
                if (materialInfo.mainTexture != null)
                    recreatedMaterial.mainTexture = materialInfo.mainTexture;
                
                if (recreatedMaterial.HasProperty("_Metallic"))
                    recreatedMaterial.SetFloat("_Metallic", materialInfo.metallic);
                
                if (recreatedMaterial.HasProperty("_Smoothness"))
                    recreatedMaterial.SetFloat("_Smoothness", materialInfo.smoothness);
                
                if (recreatedMaterial.HasProperty("_EmissionColor"))
                    recreatedMaterial.SetColor("_EmissionColor", materialInfo.emission);
                
                meshRenderer.material = recreatedMaterial;
            }
            else
            {
                // 使用默认材质
                meshRenderer.material = new Material(Shader.Find("Standard"));
                meshRenderer.material.color = geologicalLayers.Count > 0 ? geologicalLayers[0].layerColor : Color.gray;
            }
        }
        
        Debug.Log($"成功重建样本模型 {displayName}，包含 {meshData.Length} 个网格组件");
        return sampleRoot;
    }
    
    /// <summary>
    /// 创建基于材质的备用模型（当网格数据不可读时）
    /// </summary>
    GameObject CreateMaterialBasedFallbackModel(Vector3 position)
    {
        GameObject sampleRoot = new GameObject($"MaterialBasedSample_{sampleID}");
        sampleRoot.transform.position = position;
        sampleRoot.transform.rotation = originalRotation;
        sampleRoot.transform.localScale = originalScale;
        
        // 为每个保存的材质创建一个简单的几何体
        for (int i = 0; i < materialData.Length; i++)
        {
            var materialInfo = materialData[i];
            
            // 创建简单的几何体（立方体或圆柱体）
            GameObject primitiveObj;
            if (i == 0 || totalDepth > sampleRadius * 4)
            {
                // 主要形状使用圆柱体
                primitiveObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                primitiveObj.transform.localScale = new Vector3(sampleRadius * 2, totalDepth / 2, sampleRadius * 2);
            }
            else
            {
                // 其他层使用立方体表示
                primitiveObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                float layerHeight = totalDepth / materialData.Length;
                primitiveObj.transform.localScale = new Vector3(sampleRadius * 1.8f, layerHeight, sampleRadius * 1.8f);
                primitiveObj.transform.localPosition = new Vector3(0, (i - materialData.Length / 2f) * layerHeight, 0);
            }
            
            primitiveObj.name = $"Layer_{i}_{materialInfo.materialName}";
            primitiveObj.transform.SetParent(sampleRoot.transform);
            
            // 应用保存的材质
            Renderer renderer = primitiveObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material recreatedMaterial = new Material(Shader.Find(materialInfo.shaderName));
                recreatedMaterial.name = materialInfo.materialName;
                recreatedMaterial.color = materialInfo.color;
                
                if (materialInfo.mainTexture != null)
                    recreatedMaterial.mainTexture = materialInfo.mainTexture;
                
                if (recreatedMaterial.HasProperty("_Metallic"))
                    recreatedMaterial.SetFloat("_Metallic", materialInfo.metallic);
                
                if (recreatedMaterial.HasProperty("_Smoothness"))
                    recreatedMaterial.SetFloat("_Smoothness", materialInfo.smoothness);
                
                if (recreatedMaterial.HasProperty("_EmissionColor"))
                    recreatedMaterial.SetColor("_EmissionColor", materialInfo.emission);
                
                renderer.material = recreatedMaterial;
            }
        }
        
        Debug.Log($"创建了基于材质的备用模型 {displayName}，包含 {materialData.Length} 个层");
        return sampleRoot;
    }
    
    /// <summary>
    /// 创建备用模型（当没有保存数据时）
    /// </summary>
    GameObject CreateFallbackModel(Vector3 position)
    {
        GameObject fallback;
        
        // 根据源工具ID决定备用模型类型
        if (sourceToolID == "1002") // 地质锤工具
        {
            // 创建薄片模型（使用SlabSampleGenerator）
            fallback = CreateSlabFallbackModel(position);
        }
        else
        {
            // 创建圆柱模型（钻探样本）
            fallback = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fallback.name = $"FallbackSample_{sampleID}";
            fallback.transform.position = position;
            fallback.transform.localScale = new Vector3(sampleRadius * 2, totalDepth / 2, sampleRadius * 2);
            
            // 设置材质
            Renderer renderer = fallback.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = new Material(Shader.Find("Standard"));
                material.color = geologicalLayers.Count > 0 ? geologicalLayers[0].layerColor : Color.gray;
                renderer.material = material;
            }
        }
        
        return fallback;
    }
    
    /// <summary>
    /// 创建薄片备用模型（使用SlabSampleGenerator重建真实薄片）
    /// </summary>
    GameObject CreateSlabFallbackModel(Vector3 position)
    {
        // 查找或创建 SlabSampleGenerator
        SlabSampleGenerator generator = UnityEngine.Object.FindFirstObjectByType<SlabSampleGenerator>();
        if (generator == null)
        {
            GameObject generatorObj = new GameObject("SlabSampleGenerator");
            generator = generatorObj.AddComponent<SlabSampleGenerator>();
        }
        
        // 从保存的数据中获取地质层信息
        Material originalMaterial = null;
        GeologyLayer sourceLayer = null;
        
        // 尝试从材质数据中重建原始材质
        if (materialData != null && materialData.Length > 0)
        {
            var firstMaterial = materialData[0];
            originalMaterial = new Material(Shader.Find(firstMaterial.shaderName));
            originalMaterial.color = firstMaterial.color;
            originalMaterial.name = firstMaterial.materialName;
            
            // 设置其他材质属性
            if (originalMaterial.HasProperty("_Metallic"))
                originalMaterial.SetFloat("_Metallic", firstMaterial.metallic);
            if (originalMaterial.HasProperty("_Smoothness"))
                originalMaterial.SetFloat("_Smoothness", firstMaterial.smoothness);
            if (originalMaterial.HasProperty("_EmissionColor"))
                originalMaterial.SetColor("_EmissionColor", firstMaterial.emission);
        }
        
        // 如果没有材质数据，使用地质层颜色创建默认材质
        if (originalMaterial == null)
        {
            originalMaterial = new Material(Shader.Find("Standard"));
            originalMaterial.color = geologicalLayers.Count > 0 ? geologicalLayers[0].layerColor : new Color(0.7f, 0.5f, 0.3f);
            originalMaterial.name = "SlabFallbackMaterial";
        }
        
        // 使用SlabSampleGenerator重建薄片模型，禁用悬浮效果（静态放置）
        GameObject slabSample = generator.GenerateSlabSampleWithMaterial(position, originalMaterial, sourceLayer, false);
        
        if (slabSample != null)
        {
            slabSample.name = $"FallbackSlabSample_{sampleID}";
            Debug.Log($"使用SlabSampleGenerator重建薄片备用模型: {slabSample.name}");
        }
        else
        {
            Debug.LogWarning("使用SlabSampleGenerator创建备用模型失败，使用简单立方体");
            
            // 最后的备用方案：创建简单的薄片形状
            slabSample = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slabSample.name = $"SimpleFallbackSlabSample_{sampleID}";
            slabSample.transform.position = position;
            slabSample.transform.localScale = new Vector3(0.8f, 0.06f, 0.6f); // 薄片形状
            
            Renderer renderer = slabSample.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = originalMaterial;
            }
        }
        
        return slabSample;
    }
}

/// <summary>
/// 样本网格数据存储结构
/// </summary>
[System.Serializable]
public struct SampleMeshData
{
    public string meshName;
    public Vector3[] vertices;
    public int[] triangles;
    public Vector3[] normals;
    public Vector2[] uv;
    public Vector3 localPosition;
    public Quaternion localRotation;
    public Vector3 localScale;
    public Bounds bounds;
}

/// <summary>
/// 样本材质数据存储结构
/// </summary>
[System.Serializable]
public struct SampleMaterialData
{
    public string materialName;
    public string shaderName;
    public Texture2D mainTexture;
    public Color color;
    public float metallic;
    public float smoothness;
    public Color emission;
}
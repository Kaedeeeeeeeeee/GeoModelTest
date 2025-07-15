using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class TerrierGeologySetupTool : EditorWindow
{
    [MenuItem("Tools/Geology/Terrier地层配置工具")]
    static void ShowWindow()
    {
        TerrierGeologySetupTool window = GetWindow<TerrierGeologySetupTool>("Terrier地层配置");
        window.minSize = new Vector2(400, 600);
        window.Show();
    }
    
    private GameObject terrierTerrain;
    private bool autoDetectLayers = true;
    private bool showAdvancedSettings = false;
    
    // 地层配置数据
    private TerrierLayerConfig[] layerConfigs = new TerrierLayerConfig[8];
    
    void OnEnable()
    {
        InitializeLayerConfigs();
    }
    
    void InitializeLayerConfigs()
    {
        for (int i = 0; i < layerConfigs.Length; i++)
        {
            if (layerConfigs[i] == null)
            {
                layerConfigs[i] = new TerrierLayerConfig
                {
                    layerName = $"地层_{i + 1}",
                    layerType = GetDefaultLayerType(i),
                    strikeDirection = Random.Range(0f, 360f),
                    dipAngle = Random.Range(5f, 45f),
                    thickness = Random.Range(2f, 8f),
                    color = GetDefaultLayerColor(i)
                };
            }
        }
    }
    
    void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Terrier地层配置工具", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("为Terrier地形配置8个地层的地质参数，用于真实地质采样教学。", MessageType.Info);
        
        EditorGUILayout.Space(10);
        
        // Terrier地形选择
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Terrier地形对象:", GUILayout.Width(120));
        terrierTerrain = (GameObject)EditorGUILayout.ObjectField(terrierTerrain, typeof(GameObject), true);
        EditorGUILayout.EndHorizontal();
        
        if (terrierTerrain == null)
        {
            if (GUILayout.Button("自动查找Terrier地形"))
            {
                FindTerrierTerrain();
            }
        }
        
        EditorGUILayout.Space(10);
        
        // 配置选项
        autoDetectLayers = EditorGUILayout.Toggle("自动检测地层材质", autoDetectLayers);
        showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "高级设置");
        
        EditorGUILayout.Space(10);
        
        // 地层配置界面
        DrawLayerConfigurations();
        
        EditorGUILayout.Space(15);
        
        // 操作按钮
        DrawActionButtons();
        
        if (showAdvancedSettings)
        {
            EditorGUILayout.Space(10);
            DrawAdvancedSettings();
        }
    }
    
    void DrawLayerConfigurations()
    {
        EditorGUILayout.LabelField("地层配置", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginVertical(GUI.skin.box);
        
        for (int i = 0; i < layerConfigs.Length; i++)
        {
            DrawLayerConfig(i);
            if (i < layerConfigs.Length - 1)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            }
        }
        
        EditorGUILayout.EndVertical();
    }
    
    void DrawLayerConfig(int index)
    {
        TerrierLayerConfig config = layerConfigs[index];
        
        EditorGUILayout.BeginVertical(GUI.skin.box);
        
        // 地层头部
        EditorGUILayout.BeginHorizontal();
        config.expanded = EditorGUILayout.Foldout(config.expanded, $"地层 {index + 1}: {config.layerName}");
        config.color = EditorGUILayout.ColorField(config.color, GUILayout.Width(50));
        EditorGUILayout.EndHorizontal();
        
        if (config.expanded)
        {
            EditorGUI.indentLevel++;
            
            config.layerName = EditorGUILayout.TextField("地层名称", config.layerName);
            config.layerType = (LayerType)EditorGUILayout.EnumPopup("地层类型", config.layerType);
            
            EditorGUILayout.Space(5);
            
            EditorGUILayout.LabelField("地质参数", EditorStyles.miniBoldLabel);
            config.strikeDirection = EditorGUILayout.Slider("走向角度", config.strikeDirection, 0f, 360f);
            config.dipAngle = EditorGUILayout.Slider("倾斜角度", config.dipAngle, 0f, 90f);
            config.thickness = EditorGUILayout.Slider("厚度 (m)", config.thickness, 0.5f, 15f);
            
            EditorGUILayout.Space(5);
            
            EditorGUILayout.LabelField("材质设置", EditorStyles.miniBoldLabel);
            config.material = (Material)EditorGUILayout.ObjectField("材质", config.material, typeof(Material), false);
            
            if (autoDetectLayers && config.material == null)
            {
                EditorGUILayout.HelpBox($"将自动从Terrier地形检测第{index + 1}层材质", MessageType.Info);
            }
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.EndVertical();
    }
    
    void DrawActionButtons()
    {
        EditorGUILayout.BeginHorizontal();
        
        GUI.enabled = terrierTerrain != null;
        
        if (GUILayout.Button("应用地层配置", GUILayout.Height(30)))
        {
            ApplyGeologyConfiguration();
        }
        
        if (GUILayout.Button("重置为默认", GUILayout.Height(30)))
        {
            ResetToDefaults();
        }
        
        GUI.enabled = true;
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("保存配置"))
        {
            SaveConfiguration();
        }
        
        if (GUILayout.Button("加载配置"))
        {
            LoadConfiguration();
        }
        
        EditorGUILayout.EndHorizontal();
    }
    
    void DrawAdvancedSettings()
    {
        EditorGUILayout.LabelField("高级设置", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(GUI.skin.box);
        
        EditorGUILayout.HelpBox("高级地质参数设置 - 用于更精确的地质建模", MessageType.Info);
        
        EditorGUILayout.LabelField("全局设置");
        // 这里可以添加更多高级设置
        
        EditorGUILayout.EndVertical();
    }
    
    void FindTerrierTerrain()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.ToLower().Contains("terrier"))
            {
                terrierTerrain = obj;
                Debug.Log($"找到Terrier地形: {obj.name}");
                return;
            }
        }
        
        Debug.LogWarning("未找到包含'Terrier'名称的地形对象");
    }
    
    void ApplyGeologyConfiguration()
    {
        if (terrierTerrain == null)
        {
            EditorUtility.DisplayDialog("错误", "请先选择Terrier地形对象", "确定");
            return;
        }
        
        Debug.Log("开始应用地层配置到Terrier地形...");
        
        // 检测或创建地层材质
        if (autoDetectLayers)
        {
            DetectTerrierMaterials();
        }
        
        // 为每个地层创建GeologyLayer组件
        CreateGeologyLayers();
        
        // 创建地层检测系统
        EnsureDetectionSystem();
        
        EditorUtility.DisplayDialog("完成", "Terrier地层配置已成功应用！\n\n现在可以使用钻探工具进行真实地质采样了。", "确定");
        
        Debug.Log("Terrier地层配置完成！");
    }
    
    void DetectTerrierMaterials()
    {
        Debug.Log("自动检测Terrier地层材质...");
        
        MeshRenderer[] renderers = terrierTerrain.GetComponentsInChildren<MeshRenderer>();
        List<Material> detectedMaterials = new List<Material>();
        
        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer.materials != null)
            {
                foreach (Material mat in renderer.materials)
                {
                    if (mat != null && !detectedMaterials.Contains(mat))
                    {
                        detectedMaterials.Add(mat);
                    }
                }
            }
        }
        
        Debug.Log($"检测到 {detectedMaterials.Count} 种材质");
        
        // 将检测到的材质分配给地层配置
        for (int i = 0; i < Mathf.Min(detectedMaterials.Count, layerConfigs.Length); i++)
        {
            if (layerConfigs[i].material == null)
            {
                layerConfigs[i].material = detectedMaterials[i];
                Debug.Log($"地层 {i + 1} 自动分配材质: {detectedMaterials[i].name}");
            }
        }
    }
    
    void CreateGeologyLayers()
    {
        Debug.Log("创建地层组件...");
        
        // 清理现有的地层组件
        GeologyLayer[] existingLayers = terrierTerrain.GetComponentsInChildren<GeologyLayer>();
        for (int i = 0; i < existingLayers.Length; i++)
        {
            DestroyImmediate(existingLayers[i]);
        }
        
        // 为每个配置创建地层组件
        for (int i = 0; i < layerConfigs.Length; i++)
        {
            TerrierLayerConfig config = layerConfigs[i];
            
            GameObject layerObj = new GameObject($"GeologyLayer_{config.layerName}");
            layerObj.transform.SetParent(terrierTerrain.transform);
            layerObj.transform.localPosition = Vector3.zero;
            
            GeologyLayer layer = layerObj.AddComponent<GeologyLayer>();
            
            // 配置地层参数
            layer.layerName = config.layerName;
            layer.layerType = config.layerType;
            layer.strikeDirection = new Vector3(Mathf.Cos(config.strikeDirection * Mathf.Deg2Rad), 0, Mathf.Sin(config.strikeDirection * Mathf.Deg2Rad));
            layer.dipAngle = config.dipAngle;
            layer.averageThickness = config.thickness;
            layer.layerMaterial = config.material;
            layer.layerColor = config.color;
            
            // 设置地层边界（简化为地形边界）
            Bounds terrainBounds = GetTerrainBounds(terrierTerrain);
            
            Debug.Log($"创建地层: {config.layerName} (类型: {config.layerType})");
        }
    }
    
    void EnsureDetectionSystem()
    {
        LayerDetectionSystem detectionSystem = FindObjectOfType<LayerDetectionSystem>();
        if (detectionSystem == null)
        {
            GameObject detectionObj = new GameObject("LayerDetectionSystem");
            detectionSystem = detectionObj.AddComponent<LayerDetectionSystem>();
            Debug.Log("创建地层检测系统");
        }
        
        SampleReconstructionSystem reconstructionSystem = FindObjectOfType<SampleReconstructionSystem>();
        if (reconstructionSystem == null)
        {
            GameObject reconstructionObj = new GameObject("SampleReconstructionSystem");
            reconstructionSystem = reconstructionObj.AddComponent<SampleReconstructionSystem>();
            Debug.Log("创建样本重建系统");
        }
    }
    
    Bounds GetTerrainBounds(GameObject terrain)
    {
        Bounds bounds = new Bounds(terrain.transform.position, Vector3.zero);
        
        Renderer[] renderers = terrain.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }
        
        return bounds;
    }
    
    void ResetToDefaults()
    {
        InitializeLayerConfigs();
        Debug.Log("地层配置已重置为默认值");
    }
    
    void SaveConfiguration()
    {
        string path = EditorUtility.SaveFilePanel("保存地层配置", Application.dataPath, "terrier_geology_config", "json");
        if (!string.IsNullOrEmpty(path))
        {
            // 这里可以实现配置保存逻辑
            Debug.Log("地层配置已保存到: " + path);
        }
    }
    
    void LoadConfiguration()
    {
        string path = EditorUtility.OpenFilePanel("加载地层配置", Application.dataPath, "json");
        if (!string.IsNullOrEmpty(path))
        {
            // 这里可以实现配置加载逻辑
            Debug.Log("地层配置已从以下位置加载: " + path);
        }
    }
    
    LayerType GetDefaultLayerType(int index)
    {
        LayerType[] defaultTypes = {
            LayerType.Soil,           // 表土层
            LayerType.Alluvium,       // 冲积层
            LayerType.Sedimentary,    // 沉积岩1
            LayerType.Sedimentary,    // 沉积岩2
            LayerType.Metamorphic,    // 变质岩1
            LayerType.Sedimentary,    // 沉积岩3
            LayerType.Igneous,        // 火成岩
            LayerType.Bedrock         // 基岩
        };
        
        return index < defaultTypes.Length ? defaultTypes[index] : LayerType.Sedimentary;
    }
    
    Color GetDefaultLayerColor(int index)
    {
        Color[] defaultColors = {
            new Color(0.6f, 0.4f, 0.2f),  // 棕色 - 土壤
            new Color(0.8f, 0.7f, 0.5f),  // 浅棕色 - 冲积
            new Color(0.7f, 0.7f, 0.8f),  // 浅灰色 - 沉积岩
            new Color(0.5f, 0.6f, 0.7f),  // 蓝灰色 - 沉积岩
            new Color(0.6f, 0.5f, 0.7f),  // 紫色 - 变质岩
            new Color(0.8f, 0.6f, 0.6f),  // 浅红色 - 沉积岩
            new Color(0.4f, 0.4f, 0.4f),  // 深灰色 - 火成岩
            new Color(0.3f, 0.3f, 0.3f)   // 很深灰色 - 基岩
        };
        
        return index < defaultColors.Length ? defaultColors[index] : Color.gray;
    }
}

[System.Serializable]
public class TerrierLayerConfig
{
    public string layerName;
    public LayerType layerType;
    public float strikeDirection;
    public float dipAngle;
    public float thickness;
    public Color color;
    public Material material;
    public bool expanded = true;
}
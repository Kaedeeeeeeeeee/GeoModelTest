using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class TerrierAutoDetectionTool : EditorWindow
{
    [MenuItem("Tools/Geology/Terrier自动检测工具")]
    static void ShowWindow()
    {
        TerrierAutoDetectionTool window = GetWindow<TerrierAutoDetectionTool>("Terrier自动检测");
        window.minSize = new Vector2(400, 300);
        window.Show();
    }
    
    private GameObject terrierTerrain;
    private List<TerrierLayerData> detectedLayers = new List<TerrierLayerData>();
    
    void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Terrier自动地层检测工具", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("自动检测Terrier地形的真实几何形状、材质和地层分布，无需手动配置参数。", MessageType.Info);
        
        EditorGUILayout.Space(10);
        
        // Terrier地形选择
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Terrier地形:", GUILayout.Width(100));
        terrierTerrain = (GameObject)EditorGUILayout.ObjectField(terrierTerrain, typeof(GameObject), true);
        EditorGUILayout.EndHorizontal();
        
        if (terrierTerrain == null)
        {
            if (GUILayout.Button("自动查找Terrier地形"))
            {
                FindTerrierTerrain();
            }
        }
        else
        {
            EditorGUILayout.Space(10);
            
            if (GUILayout.Button("分析Terrier地层结构", GUILayout.Height(30)))
            {
                AnalyzeTerrierLayers();
            }
            
            if (detectedLayers.Count > 0)
            {
                EditorGUILayout.Space(10);
                DrawDetectedLayers();
                
                EditorGUILayout.Space(10);
                
                if (GUILayout.Button("应用真实地层数据", GUILayout.Height(30)))
                {
                    ApplyRealLayerData();
                }
            }
        }
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
    
    void AnalyzeTerrierLayers()
    {
        if (terrierTerrain == null)
        {
            EditorUtility.DisplayDialog("错误", "请先选择Terrier地形对象", "确定");
            return;
        }
        
        try 
        {
            Debug.Log("开始分析Terrier地层结构...");
            detectedLayers.Clear();
            
            // 获取所有子层对象
            Transform[] childLayers = GetChildLayers(terrierTerrain.transform);
            
            Debug.Log($"检测到 {childLayers.Length} 个地层子对象");
            
            foreach (Transform layerTransform in childLayers)
            {
                TerrierLayerData layerData = AnalyzeLayer(layerTransform);
                if (layerData != null)
                {
                    detectedLayers.Add(layerData);
                }
            }
            
            // 按位置排序（从上到下）
            detectedLayers = detectedLayers.OrderByDescending(l => l.centerPosition.y).ToList();
            
            Debug.Log($"成功分析了 {detectedLayers.Count} 个地层");
            
            // 强制重绘GUI
            Repaint();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"分析地层时出错: {e.Message}");
            EditorUtility.DisplayDialog("错误", $"分析地层时出错: {e.Message}", "确定");
        }
    }
    
    Transform[] GetChildLayers(Transform parent)
    {
        List<Transform> layers = new List<Transform>();
        
        foreach (Transform child in parent)
        {
            // 检查是否有网格渲染器（表示是一个地层）
            if (child.GetComponent<MeshRenderer>() != null)
            {
                layers.Add(child);
            }
        }
        
        return layers.ToArray();
    }
    
    TerrierLayerData AnalyzeLayer(Transform layerTransform)
    {
        MeshRenderer meshRenderer = layerTransform.GetComponent<MeshRenderer>();
        MeshFilter meshFilter = layerTransform.GetComponent<MeshFilter>();
        
        if (meshRenderer == null || meshFilter == null)
        {
            return null;
        }
        
        TerrierLayerData layerData = new TerrierLayerData();
        
        // 基本信息
        layerData.layerName = layerTransform.name;
        layerData.gameObject = layerTransform.gameObject;
        layerData.transform = layerTransform;
        
        // 材质信息
        layerData.material = meshRenderer.sharedMaterial;
        if (layerData.material != null)
        {
            layerData.color = layerData.material.color;
        }
        
        // 几何信息
        Bounds bounds = meshRenderer.bounds;
        layerData.bounds = bounds;
        layerData.centerPosition = bounds.center;
        layerData.size = bounds.size;
        
        // 计算真实厚度（Y方向尺寸）
        layerData.thickness = bounds.size.y;
        
        // 分析倾斜角度（通过网格顶点分析）
        layerData.dipAngle = CalculateLayerDip(meshFilter.sharedMesh, layerTransform);
        
        // 分析走向（通过边界分析）
        layerData.strikeDirection = CalculateLayerStrike(meshFilter.sharedMesh, layerTransform);
        
        // 自动推断地层类型
        layerData.layerType = InferLayerType(layerData.layerName, layerData.material);
        
        Debug.Log($"分析地层: {layerData.layerName} - 厚度: {layerData.thickness:F2}m, 倾角: {layerData.dipAngle:F1}°");
        
        return layerData;
    }
    
    float CalculateLayerDip(Mesh mesh, Transform layerTransform)
    {
        if (mesh == null || mesh.vertices.Length == 0)
            return 0f;
        
        Vector3[] vertices = mesh.vertices;
        List<Vector3> worldVertices = new List<Vector3>();
        
        // 转换到世界坐标
        foreach (Vector3 vertex in vertices)
        {
            worldVertices.Add(layerTransform.TransformPoint(vertex));
        }
        
        // 找到最高点和最低点
        float minY = worldVertices.Min(v => v.y);
        float maxY = worldVertices.Max(v => v.y);
        
        // 计算整体倾斜
        Vector3 highestPoint = worldVertices.First(v => Mathf.Approximately(v.y, maxY));
        Vector3 lowestPoint = worldVertices.First(v => Mathf.Approximately(v.y, minY));
        
        Vector3 direction = (highestPoint - lowestPoint).normalized;
        float dipAngle = Mathf.Acos(Mathf.Abs(direction.y)) * Mathf.Rad2Deg;
        
        return dipAngle;
    }
    
    Vector3 CalculateLayerStrike(Mesh mesh, Transform layerTransform)
    {
        if (mesh == null || mesh.vertices.Length == 0)
            return Vector3.forward;
        
        // 计算主要延伸方向
        Bounds localBounds = mesh.bounds;
        Vector3 strike = Vector3.forward;
        
        if (localBounds.size.x > localBounds.size.z)
        {
            strike = layerTransform.TransformDirection(Vector3.right);
        }
        else
        {
            strike = layerTransform.TransformDirection(Vector3.forward);
        }
        
        return strike.normalized;
    }
    
    LayerType InferLayerType(string layerName, Material material)
    {
        string name = layerName.ToLower();
        
        if (name.Contains("soil") || name.Contains("土"))
            return LayerType.Soil;
        else if (name.Contains("alluvium") || name.Contains("冲积"))
            return LayerType.Alluvium;
        else if (name.Contains("bedrock") || name.Contains("基岩"))
            return LayerType.Bedrock;
        else if (name.Contains("igneous") || name.Contains("火成"))
            return LayerType.Igneous;
        else if (name.Contains("metamorphic") || name.Contains("变质"))
            return LayerType.Metamorphic;
        else
            return LayerType.Sedimentary; // 默认为沉积岩
    }
    
    void DrawDetectedLayers()
    {
        EditorGUILayout.LabelField("检测到的地层:", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(GUI.skin.box);
        
        for (int i = 0; i < detectedLayers.Count; i++)
        {
            TerrierLayerData layer = detectedLayers[i];
            
            EditorGUILayout.BeginHorizontal();
            
            // 颜色预览
            EditorGUILayout.ColorField(layer.color, GUILayout.Width(30));
            
            // 地层信息
            EditorGUILayout.LabelField($"{i + 1}. {layer.layerName}", GUILayout.Width(100));
            EditorGUILayout.LabelField($"厚度: {layer.thickness:F2}m", GUILayout.Width(80));
            EditorGUILayout.LabelField($"倾角: {layer.dipAngle:F1}°", GUILayout.Width(70));
            EditorGUILayout.LabelField($"类型: {layer.layerType}", GUILayout.Width(80));
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndVertical();
    }
    
    void ApplyRealLayerData()
    {
        try
        {
            Debug.Log("应用真实地层数据到Terrier地形...");
            
            // 开始记录撤销操作
            Undo.RecordObjects(terrierTerrain.GetComponentsInChildren<Transform>(), "Apply Real Layer Data");
            
            // 清理现有的地层组件
            GeologyLayer[] existingLayers = terrierTerrain.GetComponentsInChildren<GeologyLayer>();
            for (int i = 0; i < existingLayers.Length; i++)
            {
                Undo.DestroyObjectImmediate(existingLayers[i]);
            }
            
            // 为每个检测到的地层创建组件
            foreach (TerrierLayerData layerData in detectedLayers)
            {
                if (layerData.gameObject != null)
                {
                    GeologyLayer geologyLayer = Undo.AddComponent<GeologyLayer>(layerData.gameObject);
                    
                    // 使用真实检测到的数据
                    geologyLayer.layerName = layerData.layerName;
                    geologyLayer.layerType = layerData.layerType;
                    geologyLayer.layerMaterial = layerData.material;
                    geologyLayer.layerColor = layerData.color;
                    geologyLayer.averageThickness = layerData.thickness;
                    geologyLayer.dipAngle = layerData.dipAngle;
                    geologyLayer.strikeDirection = layerData.strikeDirection;
                    
                    // 标记对象为脏（需要保存）
                    EditorUtility.SetDirty(layerData.gameObject);
                    
                    Debug.Log($"应用地层数据: {layerData.layerName}");
                }
            }
            
            // 确保检测和重建系统存在
            EnsureGeologySystemsExist();
            
            // 保存场景
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            
            EditorUtility.DisplayDialog("完成", $"成功应用了 {detectedLayers.Count} 个真实地层的数据！\n\n现在可以进行真实地质采样了。", "确定");
            
            Debug.Log("真实地层数据应用完成！");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"应用地层数据时出错: {e.Message}");
            EditorUtility.DisplayDialog("错误", $"应用地层数据时出错: {e.Message}", "确定");
        }
    }
    
    void EnsureGeologySystemsExist()
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
}

[System.Serializable]
public class TerrierLayerData
{
    public string layerName;
    public GameObject gameObject;
    public Transform transform;
    public Material material;
    public Color color;
    public Bounds bounds;
    public Vector3 centerPosition;
    public Vector3 size;
    public float thickness;
    public float dipAngle;
    public Vector3 strikeDirection;
    public LayerType layerType;
}
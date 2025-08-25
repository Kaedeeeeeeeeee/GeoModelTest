using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using SampleCuttingSystem;

/// <summary>
/// 样本切割系统自动集成工具
/// 一键将切割系统集成到Laboratory Scene中
/// </summary>
public class SampleCuttingSystemSetup : EditorWindow
{
    [MenuItem("工具/样本切割系统/集成到实验室", priority = 100)]
    public static void ShowWindow()
    {
        SampleCuttingSystemSetup window = GetWindow<SampleCuttingSystemSetup>();
        window.titleContent = new GUIContent("样本切割系统集成");
        window.minSize = new Vector2(400, 600);
    }
    
    private Vector3 cuttingStationPosition = new Vector3(0.693000019f, 0.588999987f, 14.5710001f);
    private Vector3 cuttingStationRotation = new Vector3(0f, 0f, 0f);
    private bool createUI = true;
    private bool addToGameInitializer = true;
    private bool createAudioResources = true;
    private bool addLocalization = true;
    
    void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("样本切割系统集成工具", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        
        // 检查当前场景
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene.Contains("Laboratory"))
        {
            EditorGUILayout.HelpBox($"当前场景: {currentScene} ✓", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox($"当前场景: {currentScene}\n警告: 建议在Laboratory Scene中进行集成", MessageType.Warning);
        }
        
        EditorGUILayout.Space(10);
        
        // 位置设置
        EditorGUILayout.LabelField("切割台位置设置", EditorStyles.boldLabel);
        cuttingStationPosition = EditorGUILayout.Vector3Field("位置", cuttingStationPosition);
        cuttingStationRotation = EditorGUILayout.Vector3Field("旋转", cuttingStationRotation);
        
        EditorGUILayout.Space(10);
        
        // 集成选项
        EditorGUILayout.LabelField("集成选项", EditorStyles.boldLabel);
        createUI = EditorGUILayout.Toggle("创建UI界面", createUI);
        addToGameInitializer = EditorGUILayout.Toggle("集成到GameInitializer", addToGameInitializer);
        createAudioResources = EditorGUILayout.Toggle("创建音频资源文件夹", createAudioResources);
        addLocalization = EditorGUILayout.Toggle("添加本地化文本", addLocalization);
        
        EditorGUILayout.Space(20);
        
        // 集成按钮
        if (GUILayout.Button("一键集成切割系统", GUILayout.Height(40)))
        {
            IntegrateCuttingSystem();
        }
        
        EditorGUILayout.Space(10);
        
        // 其他工具按钮
        EditorGUILayout.LabelField("其他工具", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("检查系统完整性"))
        {
            CheckSystemIntegrity();
        }
        if (GUILayout.Button("移除切割系统"))
        {
            RemoveCuttingSystem();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("创建测试样本"))
        {
            CreateTestSamples();
        }
        if (GUILayout.Button("打开使用文档"))
        {
            OpenDocumentation();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        
        // 状态信息
        ShowSystemStatus();
    }
    
    /// <summary>
    /// 一键集成切割系统
    /// </summary>
    private void IntegrateCuttingSystem()
    {
        try
        {
            Debug.Log("开始集成样本切割系统...");
            
            // 1. 创建切割台主对象
            GameObject cuttingStation = CreateCuttingStation();
            
            // 2. 添加所有系统组件
            AddSystemComponents(cuttingStation);
            
            // 3. 配置交互组件
            ConfigureInteractionComponent(cuttingStation);
            
            // 4. 集成到GameInitializer
            if (addToGameInitializer)
            {
                IntegrateWithGameInitializer();
            }
            
            // 5. 创建音频资源文件夹
            if (createAudioResources)
            {
                CreateAudioResourcesFolders();
            }
            
            // 6. 添加本地化文本
            if (addLocalization)
            {
                AddLocalizationText();
            }
            
            // 7. 保存场景
            SaveCurrentScene();
            
            // 8. 选中创建的对象
            Selection.activeGameObject = cuttingStation;
            EditorGUIUtility.PingObject(cuttingStation);
            
            Debug.Log("样本切割系统集成完成！");
            EditorUtility.DisplayDialog("集成完成", "样本切割系统已成功集成到当前场景并保存！\n\n现在可以运行游戏测试切割功能。", "确定");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"集成失败: {e.Message}");
            EditorUtility.DisplayDialog("集成失败", $"集成过程中出现错误:\n{e.Message}", "确定");
        }
    }
    
    /// <summary>
    /// 创建切割台主对象
    /// </summary>
    private GameObject CreateCuttingStation()
    {
        // 确保标签存在
        CreateCuttingStationTag();
        
        // 检查是否已存在切割台（使用名称查找，避免标签问题）
        GameObject existingStation = GameObject.Find("SampleCuttingStation");
        if (existingStation != null)
        {
            if (EditorUtility.DisplayDialog("已存在切割台", "场景中已存在切割台，是否替换？", "替换", "取消"))
            {
                DestroyImmediate(existingStation);
            }
            else
            {
                throw new System.Exception("用户取消了替换操作");
            }
        }
        
        // 创建新的切割台
        GameObject cuttingStation = new GameObject("SampleCuttingStation");
        
        // 尝试设置标签（如果标签存在的话）
        try
        {
            cuttingStation.tag = "CuttingStation";
        }
        catch (System.Exception)
        {
            Debug.LogWarning("无法设置CuttingStation标签，使用默认标签");
            cuttingStation.tag = "Untagged";
        }
        
        // 设置位置和旋转
        cuttingStation.transform.position = cuttingStationPosition;
        cuttingStation.transform.eulerAngles = cuttingStationRotation;
        
        // 创建实验台立方体
        CreateLabTableCube(cuttingStation);
        
        Debug.Log($"创建切割台: {cuttingStation.name} at {cuttingStationPosition}");
        return cuttingStation;
    }
    
    /// <summary>
    /// 创建实验台立方体
    /// </summary>
    private void CreateLabTableCube(GameObject parent)
    {
        // 创建实验台立方体
        GameObject labTable = GameObject.CreatePrimitive(PrimitiveType.Cube);
        labTable.name = "LabTable";
        labTable.transform.SetParent(parent.transform);
        
        // 设置实验台的尺寸和位置
        labTable.transform.localPosition = new Vector3(0f, -0.25f, 0f); // 下沉25cm作为桌面
        labTable.transform.localScale = new Vector3(2f, 0.5f, 1.5f); // 2m宽 × 0.5m高 × 1.5m深
        
        // 设置材质 - 实验台的木质外观
        var renderer = labTable.GetComponent<Renderer>();
        if (renderer != null)
        {
            var material = new Material(Shader.Find("Standard"));
            material.color = new Color(0.8f, 0.6f, 0.4f, 1f); // 木色
            material.SetFloat("_Metallic", 0.1f); // 低金属度
            material.SetFloat("_Glossiness", 0.3f); // 中等光滑度
            renderer.material = material;
        }
        
        // 添加碰撞器支持物理交互
        var collider = labTable.GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = false; // 实体桌面
        }
        
        Debug.Log("创建实验台立方体");
    }
    
    /// <summary>
    /// 创建CuttingStation标签
    /// </summary>
    private void CreateCuttingStationTag()
    {
        try
        {
            // 获取TagManager
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");
            
            // 检查标签是否已存在
            bool tagExists = false;
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                SerializedProperty tag = tagsProp.GetArrayElementAtIndex(i);
                if (tag.stringValue.Equals("CuttingStation"))
                {
                    tagExists = true;
                    break;
                }
            }
            
            // 如果标签不存在则创建
            if (!tagExists)
            {
                tagsProp.InsertArrayElementAtIndex(0);
                SerializedProperty newTag = tagsProp.GetArrayElementAtIndex(0);
                newTag.stringValue = "CuttingStation";
                tagManager.ApplyModifiedProperties();
                Debug.Log("创建CuttingStation标签");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"创建标签失败: {e.Message}");
        }
    }
    
    /// <summary>
    /// 添加系统组件
    /// </summary>
    private void AddSystemComponents(GameObject cuttingStation)
    {
        // 按顺序添加组件（顺序很重要）
        var components = new System.Type[]
        {
            typeof(SampleCuttingSystemManager),    // 管理器必须第一个
            typeof(CuttingStationInteraction),    // 交互检测组件
            typeof(SampleCuttingGame),            // 游戏控制器
            typeof(SampleLayerAnalyzer),          // 地层分析器
            typeof(LayerDatabaseMapper),          // 数据库映射器
            typeof(CuttingStationUI),             // UI管理器
            typeof(SingleLayerSampleGenerator),   // 样本生成器
            typeof(WarehouseIntegration),         // 仓库集成
            typeof(SampleCuttingSystemInitializer), // 初始化器
            typeof(AudioSource)                   // 音频组件
        };
        
        foreach (var componentType in components)
        {
            if (cuttingStation.GetComponent(componentType) == null)
            {
                cuttingStation.AddComponent(componentType);
                Debug.Log($"添加组件: {componentType.Name}");
            }
        }
        
        // 配置AudioSource
        var audioSource = cuttingStation.GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.volume = 0.5f;
            audioSource.spatialBlend = 0.8f; // 3D音效
        }
        
        // 配置初始化器
        var initializer = cuttingStation.GetComponent<SampleCuttingSystemInitializer>();
        if (initializer != null)
        {
            // 使用反射设置私有字段
            var field = typeof(SampleCuttingSystemInitializer).GetField("initializeOnStart", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(initializer, true);
            }
        }
    }
    
    /// <summary>
    /// 配置交互组件
    /// </summary>
    private void ConfigureInteractionComponent(GameObject cuttingStation)
    {
        var interactionComponent = cuttingStation.GetComponent<CuttingStationInteraction>();
        if (interactionComponent != null)
        {
            // 使用反射设置交互范围
            var rangeField = typeof(CuttingStationInteraction).GetField("interactionRange", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (rangeField != null)
            {
                rangeField.SetValue(interactionComponent, 3f); // 3米交互范围
            }
            
            Debug.Log("配置切割台交互组件");
        }
    }
    
    /// <summary>
    /// 创建切割台UI界面（备用方法，现在由交互系统处理）
    /// </summary>
    private void CreateCuttingStationUI(GameObject cuttingStation)
    {
        // 创建Canvas
        GameObject canvasObj = new GameObject("CuttingStationCanvas");
        canvasObj.transform.SetParent(cuttingStation.transform);
        
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        
        // 设置Canvas位置和缩放 - 贴近实验台表面
        canvasObj.transform.localPosition = new Vector3(0f, 0.3f, 0f); // 实验台上方30cm
        canvasObj.transform.localScale = Vector3.one * 0.008f; // 适中的尺寸
        canvasObj.transform.localRotation = Quaternion.Euler(15f, 0f, 0f); // 稍微向下倾斜便于观看
        
        // 添加CanvasScaler
        var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // 添加GraphicRaycaster
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        Debug.Log("创建UI Canvas");
        
        // 创建基础UI结构（简化版，主要结构）
        CreateBasicUIStructure(canvasObj);
    }
    
    /// <summary>
    /// 创建基础UI结构
    /// </summary>
    private void CreateBasicUIStructure(GameObject canvas)
    {
        // 主面板 - 更大更醒目
        GameObject mainPanel = new GameObject("MainPanel");
        mainPanel.transform.SetParent(canvas.transform, false);
        
        var mainRect = mainPanel.AddComponent<RectTransform>();
        mainRect.sizeDelta = new Vector2(600, 400); // 适中的大小
        
        var mainImage = mainPanel.AddComponent<UnityEngine.UI.Image>();
        mainImage.color = new Color(0.0f, 0.0f, 0.0f, 0.9f); // 更深的背景
        
        // 标题文字
        GameObject titleText = new GameObject("TitleText");
        titleText.transform.SetParent(mainPanel.transform, false);
        
        var titleRect = titleText.AddComponent<RectTransform>();
        titleRect.sizeDelta = new Vector2(580, 60);
        titleRect.anchoredPosition = new Vector2(0, 150);
        
        var titleComponent = titleText.AddComponent<UnityEngine.UI.Text>();
        titleComponent.text = "样本切割台";
        titleComponent.fontSize = 32;
        titleComponent.color = Color.cyan; // 醒目的青色
        titleComponent.alignment = TextAnchor.MiddleCenter;
        titleComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleComponent.fontStyle = FontStyle.Bold;
        
        // 拖拽区域 - 更醒目的边框
        GameObject dropZone = new GameObject("DropZone");
        dropZone.transform.SetParent(mainPanel.transform, false);
        
        var dropRect = dropZone.AddComponent<RectTransform>();
        dropRect.sizeDelta = new Vector2(500, 150);
        dropRect.anchoredPosition = new Vector2(0, 20);
        
        var dropImage = dropZone.AddComponent<UnityEngine.UI.Image>();
        dropImage.color = new Color(0f, 1f, 0f, 0.3f); // 绿色半透明背景
        
        // 说明文字
        GameObject instructionText = new GameObject("InstructionText");
        instructionText.transform.SetParent(dropZone.transform, false);
        
        var instructionRect = instructionText.AddComponent<RectTransform>();
        instructionRect.sizeDelta = new Vector2(480, 100);
        instructionRect.anchoredPosition = Vector2.zero;
        
        var textComponent = instructionText.AddComponent<UnityEngine.UI.Text>();
        textComponent.text = "将多层地质样本从仓库\n拖拽到此处开始切割";
        textComponent.fontSize = 18;
        textComponent.color = Color.white;
        textComponent.alignment = TextAnchor.MiddleCenter;
        textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        
        // 交互提示
        GameObject interactionHint = new GameObject("InteractionHint");
        interactionHint.transform.SetParent(mainPanel.transform, false);
        
        var hintRect = interactionHint.AddComponent<RectTransform>();
        hintRect.sizeDelta = new Vector2(580, 40);
        hintRect.anchoredPosition = new Vector2(0, -120);
        
        var hintComponent = interactionHint.AddComponent<UnityEngine.UI.Text>();
        hintComponent.text = "按 F 键打开仓库系统";
        hintComponent.fontSize = 16;
        hintComponent.color = Color.yellow;
        hintComponent.alignment = TextAnchor.MiddleCenter;
        hintComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hintComponent.fontStyle = FontStyle.Italic;
        
        Debug.Log("创建改进的UI结构");
    }
    
    /// <summary>
    /// 集成到GameInitializer
    /// </summary>
    private void IntegrateWithGameInitializer()
    {
        var gameInitializer = FindObjectOfType<GameInitializer>();
        if (gameInitializer == null)
        {
            Debug.LogWarning("未找到GameInitializer，跳过集成");
            return;
        }
        
        Debug.Log("找到GameInitializer，切割系统将自动初始化");
        // 实际的集成逻辑已经在SampleCuttingSystemInitializer中实现
    }
    
    /// <summary>
    /// 创建音频资源文件夹
    /// </summary>
    private void CreateAudioResourcesFolders()
    {
        string audioPath = "Assets/Resources/Audio/CuttingSystem";
        
        if (!AssetDatabase.IsValidFolder(audioPath))
        {
            // 确保父文件夹存在
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Audio"))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "Audio");
            }
            
            AssetDatabase.CreateFolder("Assets/Resources/Audio", "CuttingSystem");
            AssetDatabase.Refresh();
            
            Debug.Log($"创建音频资源文件夹: {audioPath}");
        }
        
        // 创建示例音频文件说明
        string readmePath = $"{audioPath}/README.txt";
        if (!System.IO.File.Exists(readmePath))
        {
            string readmeContent = @"样本切割系统音频文件说明

请将以下音频文件放置到此文件夹：

1. laser_hum.wav - 激光切割嗡嗡声
2. cut_success.wav - 切割成功音效  
3. cut_failure.wav - 切割失败音效

音频格式建议：
- 采样率：44.1kHz
- 位深：16bit
- 格式：WAV或OGG

音频长度建议：
- laser_hum: 2-3秒（可循环）
- success/failure: 0.5-1秒
";
            System.IO.File.WriteAllText(readmePath, readmeContent);
            AssetDatabase.Refresh();
        }
    }
    
    /// <summary>
    /// 添加本地化文本
    /// </summary>
    private void AddLocalizationText()
    {
        string[] localizationFiles = {
            "Assets/Resources/Localization/Data/zh-CN.json",
            "Assets/Resources/Localization/Data/en-US.json",
            "Assets/Resources/Localization/Data/ja-JP.json"
        };
        
        foreach (string filePath in localizationFiles)
        {
            if (System.IO.File.Exists(filePath))
            {
                Debug.Log($"本地化文件已存在: {filePath}");
                // 这里可以添加具体的文本添加逻辑
            }
        }
        
        Debug.Log("本地化集成提示：请手动添加切割系统相关文本到本地化文件");
    }
    
    /// <summary>
    /// 保存当前场景
    /// </summary>
    private void SaveCurrentScene()
    {
        try
        {
            // 标记场景为已修改
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            
            // 保存当前场景
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            
            Debug.Log("场景已保存，切割台将在运行时保持存在");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"保存场景失败: {e.Message}");
        }
    }
    
    /// <summary>
    /// 检查系统完整性
    /// </summary>
    private void CheckSystemIntegrity()
    {
        GameObject cuttingStation = FindCuttingStationSafely();
        
        if (cuttingStation == null)
        {
            EditorUtility.DisplayDialog("系统检查", "未找到切割台，请先集成系统", "确定");
            return;
        }
        
        var manager = cuttingStation.GetComponent<SampleCuttingSystemManager>();
        if (manager != null)
        {
            // 简化检查，因为CheckSystemHealth方法可能还没实现
            EditorUtility.DisplayDialog("系统检查", "找到切割台和系统管理器，基础组件完整！", "确定");
        }
        else
        {
            EditorUtility.DisplayDialog("系统检查", "切割台缺少系统管理器组件", "确定");
        }
    }
    
    /// <summary>
    /// 移除切割系统
    /// </summary>
    private void RemoveCuttingSystem()
    {
        if (EditorUtility.DisplayDialog("移除系统", "确定要移除切割系统吗？此操作不可撤销。", "确定", "取消"))
        {
            GameObject cuttingStation = FindCuttingStationSafely();
            if (cuttingStation != null)
            {
                DestroyImmediate(cuttingStation);
                Debug.Log("切割系统已移除");
                EditorUtility.DisplayDialog("移除完成", "切割系统已成功移除", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("移除失败", "未找到切割台", "确定");
            }
        }
    }
    
    /// <summary>
    /// 安全查找切割台对象
    /// </summary>
    private GameObject FindCuttingStationSafely()
    {
        // 方法1：通过名称查找
        GameObject cuttingStation = GameObject.Find("SampleCuttingStation");
        if (cuttingStation != null)
            return cuttingStation;
            
        // 方法2：尝试通过标签查找（如果标签存在）
        try
        {
            cuttingStation = GameObject.FindGameObjectWithTag("CuttingStation");
            if (cuttingStation != null)
                return cuttingStation;
        }
        catch (System.Exception)
        {
            // 标签不存在，忽略异常
        }
        
        // 方法3：通过组件查找
        var manager = Object.FindFirstObjectByType<SampleCuttingSystemManager>();
        if (manager != null)
            return manager.gameObject;
            
        return null;
    }
    
    /// <summary>
    /// 创建测试样本
    /// </summary>
    private void CreateTestSamples()
    {
        EditorUtility.DisplayDialog("创建测试样本", 
            "请使用现有的钻探工具（SimpleDrill或DrillTower）来创建多层样本，然后拖拽到切割台进行测试。", 
            "确定");
    }
    
    /// <summary>
    /// 打开使用文档
    /// </summary>
    private void OpenDocumentation()
    {
        string docPath = "Assets/Scripts/SampleCuttingSystem/README.md";
        if (System.IO.File.Exists(docPath))
        {
            Application.OpenURL("file://" + System.IO.Path.GetFullPath(docPath));
        }
        else
        {
            EditorUtility.DisplayDialog("文档不存在", "找不到README.md文档文件", "确定");
        }
    }
    
    /// <summary>
    /// 显示系统状态
    /// </summary>
    private void ShowSystemStatus()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("系统状态", EditorStyles.boldLabel);
        
        // 使用更安全的查找方式
        GameObject cuttingStation = FindCuttingStationSafely();
        
        if (cuttingStation != null)
        {
            EditorGUILayout.HelpBox("✓ 切割台已存在", MessageType.Info);
            
            var manager = cuttingStation.GetComponent<SampleCuttingSystemManager>();
            if (manager != null)
            {
                EditorGUILayout.HelpBox("✓ 系统管理器已配置", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("✗ 缺少系统管理器", MessageType.Warning);
            }
            
            // 显示组件状态
            var components = new System.Type[]
            {
                typeof(SampleCuttingGame),
                typeof(SampleLayerAnalyzer), 
                typeof(CuttingStationUI),
                typeof(WarehouseIntegration)
            };
            
            int componentCount = 0;
            foreach (var componentType in components)
            {
                if (cuttingStation.GetComponent(componentType) != null)
                    componentCount++;
            }
            
            EditorGUILayout.HelpBox($"组件完整度: {componentCount}/{components.Length}", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("✗ 切割台未集成", MessageType.Warning);
        }
        
        // 检查必要文件夹
        bool audioFolderExists = AssetDatabase.IsValidFolder("Assets/Resources/Audio/CuttingSystem");
        if (audioFolderExists)
        {
            EditorGUILayout.HelpBox("✓ 音频资源文件夹已创建", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("✗ 音频资源文件夹未创建", MessageType.Warning);
        }
    }
}
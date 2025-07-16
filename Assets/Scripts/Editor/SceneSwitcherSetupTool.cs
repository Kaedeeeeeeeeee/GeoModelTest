using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// 场景切换器设置工具 - 编辑器工具，用于快速设置场景切换器
/// </summary>
public class SceneSwitcherSetupTool : EditorWindow
{
    [MenuItem("Tools/场景切换器设置")]
    static void ShowWindow()
    {
        SceneSwitcherSetupTool window = GetWindow<SceneSwitcherSetupTool>();
        window.titleContent = new GUIContent("场景切换器设置");
        window.Show();
    }
    
    private GameObject sceneSwitcherPrefab;
    private Sprite sceneSwitcherIcon;
    private AudioClip activateSound;
    private AudioClip sceneChangeSound;
    
    void OnGUI()
    {
        GUILayout.Label("场景切换器设置", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        
        // 自动检测用户的预制体
        if (sceneSwitcherPrefab == null)
        {
            sceneSwitcherPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Model/SceneSwitcher/SceneSwitcher.prefab");
            if (sceneSwitcherPrefab != null)
            {
                EditorGUILayout.HelpBox("✅ 自动检测到用户的SceneSwitcher预制体", MessageType.Info);
            }
        }
        
        // 预制体设置
        sceneSwitcherPrefab = (GameObject)EditorGUILayout.ObjectField("场景切换器预制体", sceneSwitcherPrefab, typeof(GameObject), false);
        sceneSwitcherIcon = (Sprite)EditorGUILayout.ObjectField("场景切换器图标", sceneSwitcherIcon, typeof(Sprite), false);
        
        EditorGUILayout.Space();
        
        // 音效设置
        activateSound = (AudioClip)EditorGUILayout.ObjectField("激活音效", activateSound, typeof(AudioClip), false);
        sceneChangeSound = (AudioClip)EditorGUILayout.ObjectField("场景切换音效", sceneChangeSound, typeof(AudioClip), false);
        
        EditorGUILayout.Space();
        
        // 操作按钮
        if (GUILayout.Button("创建场景切换器预制体"))
        {
            CreateSceneSwitcherPrefab();
        }
        
        if (GUILayout.Button("设置场景切换器初始化器"))
        {
            SetupSceneSwitcherInitializer();
        }
        
        if (GUILayout.Button("添加场景管理器到场景"))
        {
            AddSceneManagerToScene();
        }
        
        EditorGUILayout.Space();
        
        // 信息显示
        EditorGUILayout.HelpBox("使用说明：\n" +
            "1. 创建场景切换器预制体\n" +
            "2. 设置场景切换器初始化器\n" +
            "3. 添加场景管理器到场景\n" +
            "4. 在Build Settings中添加所有场景", MessageType.Info);
    }
    
    /// <summary>
    /// 创建场景切换器预制体
    /// </summary>
    void CreateSceneSwitcherPrefab()
    {
        // 创建场景切换器模型
        GameObject switcherModel = new GameObject("SceneSwitcher");
        
        // 主体部分
        GameObject mainBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mainBody.name = "MainBody";
        mainBody.transform.SetParent(switcherModel.transform);
        mainBody.transform.localPosition = Vector3.zero;
        mainBody.transform.localScale = new Vector3(0.1f, 0.02f, 0.1f);
        
        // 设置材质
        Renderer mainRenderer = mainBody.GetComponent<Renderer>();
        if (mainRenderer != null)
        {
            Material mainMaterial = new Material(Shader.Find("Standard"));
            mainMaterial.color = new Color(0.8f, 0.8f, 0.2f);
            mainMaterial.SetFloat("_Metallic", 0.8f);
            mainMaterial.SetFloat("_Glossiness", 0.9f);
            mainRenderer.material = mainMaterial;
        }
        
        // 移除碰撞器
        DestroyImmediate(mainBody.GetComponent<Collider>());
        
        // 发光效果
        GameObject glowEffect = new GameObject("GlowEffect");
        glowEffect.transform.SetParent(switcherModel.transform);
        glowEffect.transform.localPosition = new Vector3(0, 0.01f, 0);
        
        GameObject glowSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        glowSphere.name = "GlowSphere";
        glowSphere.transform.SetParent(glowEffect.transform);
        glowSphere.transform.localPosition = Vector3.zero;
        glowSphere.transform.localScale = new Vector3(0.12f, 0.03f, 0.12f);
        
        // 设置发光材质
        Renderer glowRenderer = glowSphere.GetComponent<Renderer>();
        if (glowRenderer != null)
        {
            Material glowMaterial = new Material(Shader.Find("Standard"));
            glowMaterial.color = new Color(1f, 1f, 0.5f, 0.5f);
            glowMaterial.SetFloat("_Mode", 3);
            glowMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            glowMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            glowMaterial.SetInt("_ZWrite", 0);
            glowMaterial.DisableKeyword("_ALPHATEST_ON");
            glowMaterial.EnableKeyword("_ALPHABLEND_ON");
            glowMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            glowMaterial.renderQueue = 3000;
            glowRenderer.material = glowMaterial;
        }
        
        // 移除碰撞器
        DestroyImmediate(glowSphere.GetComponent<Collider>());
        
        // 保存为预制体
        string prefabPath = "Assets/Models/SceneSwitcher.prefab";
        
        // 确保目录存在
        string directory = Path.GetDirectoryName(prefabPath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        PrefabUtility.SaveAsPrefabAsset(switcherModel, prefabPath);
        
        // 删除场景中的临时对象
        DestroyImmediate(switcherModel);
        
        // 加载预制体
        sceneSwitcherPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        
        Debug.Log($"场景切换器预制体已创建: {prefabPath}");
    }
    
    /// <summary>
    /// 设置场景切换器初始化器
    /// </summary>
    void SetupSceneSwitcherInitializer()
    {
        // 查找或创建初始化器
        SceneSwitcherInitializer initializer = FindObjectOfType<SceneSwitcherInitializer>();
        
        if (initializer == null)
        {
            GameObject initializerObj = new GameObject("SceneSwitcherInitializer");
            initializer = initializerObj.AddComponent<SceneSwitcherInitializer>();
            Debug.Log("场景切换器初始化器已创建");
        }
        
        // 设置配置
        initializer.sceneSwitcherPrefab = sceneSwitcherPrefab;
        initializer.sceneSwitcherIcon = sceneSwitcherIcon;
        initializer.switcherActivateSound = activateSound;
        initializer.sceneChangeSound = sceneChangeSound;
        
        // 标记为脏对象
        EditorUtility.SetDirty(initializer);
        
        Debug.Log("场景切换器初始化器配置完成");
    }
    
    /// <summary>
    /// 添加场景管理器到场景
    /// </summary>
    void AddSceneManagerToScene()
    {
        // 查找现有场景管理器
        GameSceneManager existingManager = FindObjectOfType<GameSceneManager>();
        
        if (existingManager != null)
        {
            Debug.Log("场景管理器已存在");
            return;
        }
        
        // 创建场景管理器
        GameObject managerObj = new GameObject("GameSceneManager");
        GameSceneManager sceneManager = managerObj.AddComponent<GameSceneManager>();
        
        // 添加数据持久化组件
        managerObj.AddComponent<PlayerPersistentData>();
        
        // 设置为DontDestroyOnLoad
        DontDestroyOnLoad(managerObj);
        
        Debug.Log("场景管理器已添加到场景");
    }
}
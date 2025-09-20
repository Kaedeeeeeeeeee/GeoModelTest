using UnityEngine;
using UnityEditor;
using Encyclopedia;
using System.Linq;

/// <summary>
/// 图鉴系统设置工具
/// 用于在场景中自动创建和配置SimpleEncyclopediaManager
/// </summary>
public class EncyclopediaSetupTool : EditorWindow
{
    [MenuItem("Tools/图鉴系统/自动设置图鉴管理器")]
    public static void SetupEncyclopediaManager()
    {
        // 首先检查是否已有EncyclopediaInitializer
        EncyclopediaInitializer existingInitializer = FindObjectOfType<EncyclopediaInitializer>();
        if (existingInitializer != null)
        {
            Debug.Log("[EncyclopediaSetupTool] 场景中已存在EncyclopediaInitializer，无需重复创建");
            Selection.activeGameObject = existingInitializer.gameObject;
            return;
        }

        // 检查场景中是否已经存在SimpleEncyclopediaManager
        SimpleEncyclopediaManager existingManager = FindObjectOfType<SimpleEncyclopediaManager>();
        if (existingManager != null)
        {
            Debug.Log("[EncyclopediaSetupTool] 场景中已存在SimpleEncyclopediaManager，无需重复创建");
            Selection.activeGameObject = existingManager.gameObject;
            return;
        }

        // 创建EncyclopediaInitializer，它会自动创建完整的图鉴系统
        GameObject initializerGO = new GameObject("EncyclopediaInitializer");
        EncyclopediaInitializer initializer = initializerGO.AddComponent<EncyclopediaInitializer>();

        Debug.Log("[EncyclopediaSetupTool] EncyclopediaInitializer已创建，将自动设置完整图鉴系统");

        // 选中新创建的对象
        Selection.activeGameObject = initializerGO;

        // 标记场景已修改
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }

    [MenuItem("Tools/图鉴系统/检查图鉴系统状态")]
    public static void CheckEncyclopediaSystemStatus()
    {
        Debug.Log("=== 图鉴系统状态检查 ===");

        // 检查EncyclopediaInitializer
        EncyclopediaInitializer initializer = FindObjectOfType<EncyclopediaInitializer>();
        if (initializer != null)
        {
            Debug.Log("✓ EncyclopediaInitializer存在");
            Debug.Log($"  GameObject名称: {initializer.gameObject.name}");
            Debug.Log($"  是否激活: {initializer.gameObject.activeInHierarchy}");
        }
        else
        {
            Debug.LogWarning("✗ EncyclopediaInitializer不存在");
        }

        // 检查EncyclopediaUI
        Encyclopedia.EncyclopediaUI encyclopediaUI = FindObjectOfType<Encyclopedia.EncyclopediaUI>();
        if (encyclopediaUI != null)
        {
            Debug.Log("✓ EncyclopediaUI存在");
            Debug.Log($"  GameObject名称: {encyclopediaUI.gameObject.name}");
            Debug.Log($"  是否激活: {encyclopediaUI.gameObject.activeInHierarchy}");
        }
        else
        {
            Debug.LogWarning("✗ EncyclopediaUI不存在");
        }

        // 检查SimpleEncyclopediaManager
        SimpleEncyclopediaManager manager = FindObjectOfType<SimpleEncyclopediaManager>();
        if (manager != null)
        {
            Debug.Log("✓ SimpleEncyclopediaManager存在");
            Debug.Log($"  GameObject名称: {manager.gameObject.name}");
            Debug.Log($"  是否激活: {manager.gameObject.activeInHierarchy}");
        }
        else
        {
            Debug.LogWarning("✗ SimpleEncyclopediaManager不存在");
        }

        // 检查MobileInputManager
        MobileInputManager inputManager = FindObjectOfType<MobileInputManager>();
        if (inputManager != null)
        {
            Debug.Log("✓ MobileInputManager存在");
        }
        else
        {
            Debug.LogWarning("✗ MobileInputManager不存在");
        }

        // 检查MobileControlsUI
        MobileControlsUI controlsUI = FindObjectOfType<MobileControlsUI>();
        if (controlsUI != null)
        {
            Debug.Log("✓ MobileControlsUI存在");
            if (controlsUI.encyclopediaButton != null)
            {
                Debug.Log("✓ 图鉴按钮已配置");
            }
            else
            {
                Debug.LogWarning("✗ 图鉴按钮未配置");
            }
        }
        else
        {
            Debug.LogWarning("✗ MobileControlsUI不存在");
        }

        // 检查数据系统
        if (Encyclopedia.EncyclopediaData.Instance != null)
        {
            try
            {
                var allEntries = Encyclopedia.EncyclopediaData.Instance.AllEntries;
                if (allEntries != null)
                {
                    Debug.Log($"✓ EncyclopediaData: 已加载 {allEntries.Count} 个条目");
                }
                else
                {
                    Debug.LogWarning("⚠ EncyclopediaData: 实例存在但条目数据为空");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"⚠ EncyclopediaData访问错误: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("✗ EncyclopediaData: 未初始化");
        }

        // 检查收集系统
        if (Encyclopedia.CollectionManager.Instance != null)
        {
            try
            {
                var stats = Encyclopedia.CollectionManager.Instance.CurrentStats;
                if (stats != null)
                {
                    Debug.Log($"✓ CollectionManager: {stats.discoveredEntries}/{stats.totalEntries} 已发现");
                }
                else
                {
                    Debug.LogWarning("⚠ CollectionManager: 实例存在但统计数据为空");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"⚠ CollectionManager访问错误: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("✗ CollectionManager: 未初始化");
        }

        Debug.Log("=== 检查完成 ===");
    }

    [MenuItem("Tools/图鉴系统/设置图鉴管理器UI")]
    public static void ShowEncyclopediaSetupWindow()
    {
        GetWindow<EncyclopediaSetupTool>("图鉴系统设置");
    }

    private void OnGUI()
    {
        GUILayout.Label("图鉴系统设置工具", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        if (GUILayout.Button("创建图鉴管理器"))
        {
            SetupEncyclopediaManager();
        }

        if (GUILayout.Button("检查系统状态"))
        {
            CheckEncyclopediaSystemStatus();
        }

        if (GUILayout.Button("强制初始化图鉴系统"))
        {
            ForceInitializeEncyclopediaSystem();
        }

        if (GUILayout.Button("重置并重新创建图鉴UI"))
        {
            ResetAndRecreateEncyclopediaUI();
        }

        if (GUILayout.Button("添加测试数据"))
        {
            AddTestEncyclopediaData();
        }

        if (GUILayout.Button("强制重新加载数据"))
        {
            ForceReloadEncyclopediaData();
        }

        if (GUILayout.Button("强制初始化单例"))
        {
            ForceInitializeSingletons();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("注意：创建SimpleEncyclopediaManager后，还需要在Inspector中手动配置UI组件。", MessageType.Info);
    }

    [MenuItem("Tools/图鉴系统/强制初始化图鉴系统")]
    public static void ForceInitializeEncyclopediaSystem()
    {
        Debug.Log("[EncyclopediaSetupTool] 开始强制初始化图鉴系统...");

        EncyclopediaInitializer initializer = FindObjectOfType<EncyclopediaInitializer>();
        if (initializer != null)
        {
            Debug.Log("[EncyclopediaSetupTool] 找到EncyclopediaInitializer，调用RefreshSystem");
            initializer.RefreshSystem();
        }
        else
        {
            Debug.LogWarning("[EncyclopediaSetupTool] 未找到EncyclopediaInitializer，先创建它");
            SetupEncyclopediaManager();
        }
    }

    [MenuItem("Tools/图鉴系统/重置并重新创建图鉴UI")]
    public static void ResetAndRecreateEncyclopediaUI()
    {
        Debug.Log("[EncyclopediaSetupTool] 开始重置图鉴UI...");

        // 查找并删除现有的图鉴相关GameObject
        var encyclopediaCanvases = FindObjectsOfType<Canvas>()
            .Where(c => c.name.Contains("Encyclopedia") || c.name.Contains("图鉴"))
            .ToArray();

        foreach (var canvas in encyclopediaCanvases)
        {
            Debug.Log($"[EncyclopediaSetupTool] 删除Canvas: {canvas.name}");
            DestroyImmediate(canvas.gameObject);
        }

        var encyclopediaUISetups = FindObjectsOfType<EncyclopediaUISetup>();
        foreach (var setup in encyclopediaUISetups)
        {
            Debug.Log($"[EncyclopediaSetupTool] 删除EncyclopediaUISetup: {setup.name}");
            DestroyImmediate(setup.gameObject);
        }

        // 强制重新初始化
        ForceInitializeEncyclopediaSystem();
    }

    [MenuItem("Tools/图鉴系统/添加测试数据")]
    public static void AddTestEncyclopediaData()
    {
        Debug.Log("[EncyclopediaSetupTool] 开始添加测试数据...");

        Debug.Log($"[EncyclopediaSetupTool] Instance检查: EncyclopediaData={Encyclopedia.EncyclopediaData.Instance != null}, CollectionManager={Encyclopedia.CollectionManager.Instance != null}");

        if (Encyclopedia.EncyclopediaData.Instance != null && Encyclopedia.CollectionManager.Instance != null)
        {
            try
            {
                Debug.Log("[EncyclopediaSetupTool] 两个Instance都存在，开始处理数据...");

                // 模拟发现一些条目
                var allEntries = Encyclopedia.EncyclopediaData.Instance.AllEntries;
                Debug.Log($"[EncyclopediaSetupTool] AllEntries: {allEntries?.Count ?? 0} 个条目");

                if (allEntries != null && allEntries.Count > 0)
                {
                    Debug.Log($"[EncyclopediaSetupTool] 发现 {allEntries.Count} 个条目，开始标记为已发现");

                    int discoveredCount = 0;
                    foreach (var entry in allEntries.Values)
                    {
                        if (discoveredCount < 3) // 只发现前3个
                        {
                            Debug.Log($"[EncyclopediaSetupTool] 正在发现第 {discoveredCount + 1} 个条目: {entry.id}");
                            Encyclopedia.CollectionManager.Instance.DiscoverEntry(entry.id);
                            Debug.Log($"[EncyclopediaSetupTool] 已发现: {entry.GetFormattedDisplayName()}");
                            discoveredCount++;
                        }
                    }
                    Debug.Log($"[EncyclopediaSetupTool] 总共标记了 {discoveredCount} 个条目为已发现");
                }
                else
                {
                    Debug.LogWarning("[EncyclopediaSetupTool] 没有找到任何条目数据，可能需要检查数据加载");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EncyclopediaSetupTool] 添加测试数据时出错: {e.Message}");
                Debug.LogError($"[EncyclopediaSetupTool] 堆栈跟踪: {e.StackTrace}");
            }
        }
        else
        {
            Debug.LogWarning("[EncyclopediaSetupTool] 数据系统未初始化，请先运行强制初始化");

            // 检查场景中是否存在EncyclopediaData组件
            var encyclopediaData = FindObjectOfType<EncyclopediaData>();
            if (encyclopediaData != null)
            {
                Debug.Log($"[EncyclopediaSetupTool] 找到EncyclopediaData组件在: {encyclopediaData.gameObject.name}");
                Debug.Log($"[EncyclopediaSetupTool] IsDataLoaded: {encyclopediaData.IsDataLoaded}");
            }
            else
            {
                Debug.LogWarning("[EncyclopediaSetupTool] 场景中没有找到EncyclopediaData组件");
            }

            var collectionManager = FindObjectOfType<CollectionManager>();
            if (collectionManager != null)
            {
                Debug.Log($"[EncyclopediaSetupTool] 找到CollectionManager组件在: {collectionManager.gameObject.name}");
            }
            else
            {
                Debug.LogWarning("[EncyclopediaSetupTool] 场景中没有找到CollectionManager组件");
            }
        }
    }

    [MenuItem("Tools/图鉴系统/强制重新加载数据")]
    public static void ForceReloadEncyclopediaData()
    {
        Debug.Log("[EncyclopediaSetupTool] 开始强制重新加载数据...");

        var encyclopediaData = FindObjectOfType<EncyclopediaData>();
        if (encyclopediaData != null)
        {
            Debug.Log($"[EncyclopediaSetupTool] 重新加载前状态: IsDataLoaded={encyclopediaData.IsDataLoaded}, AllEntries={encyclopediaData.AllEntries?.Count ?? 0}");

            // 使用反射调用私有的LoadEncyclopediaData方法
            var loadMethod = typeof(EncyclopediaData).GetMethod("LoadEncyclopediaData",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (loadMethod != null)
            {
                Debug.Log("[EncyclopediaSetupTool] 调用LoadEncyclopediaData方法...");
                try
                {
                    loadMethod.Invoke(encyclopediaData, null);
                    Debug.Log($"[EncyclopediaSetupTool] LoadEncyclopediaData调用完成");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[EncyclopediaSetupTool] LoadEncyclopediaData调用失败: {e.Message}");
                    Debug.LogError($"[EncyclopediaSetupTool] 内部异常: {e.InnerException?.Message}");
                }
            }
            else
            {
                Debug.LogError("[EncyclopediaSetupTool] 无法找到LoadEncyclopediaData方法");
            }

            // 检查数据处理方法
            var processMethod = typeof(EncyclopediaData).GetMethod("ProcessDatabaseData",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (processMethod != null)
            {
                Debug.Log("[EncyclopediaSetupTool] 尝试调用ProcessDatabaseData方法...");
                try
                {
                    processMethod.Invoke(encyclopediaData, null);
                    Debug.Log("[EncyclopediaSetupTool] ProcessDatabaseData调用完成");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[EncyclopediaSetupTool] ProcessDatabaseData调用失败: {e.Message}");
                }
            }

            Debug.Log($"[EncyclopediaSetupTool] 重新加载完成状态: IsDataLoaded={encyclopediaData.IsDataLoaded}, AllEntries={encyclopediaData.AllEntries?.Count ?? 0}");

            // 详细检查
            if (encyclopediaData.AllEntries != null && encyclopediaData.AllEntries.Count > 0)
            {
                Debug.Log($"[EncyclopediaSetupTool] 成功！条目详情: 总数={encyclopediaData.AllEntries.Count}");
                var firstEntry = encyclopediaData.AllEntries.Values.FirstOrDefault();
                if (firstEntry != null)
                {
                    Debug.Log($"[EncyclopediaSetupTool] 第一个条目: {firstEntry.GetFormattedDisplayName()} (ID: {firstEntry.id})");
                }
            }
            else
            {
                Debug.LogError("[EncyclopediaSetupTool] 数据重新加载失败，AllEntries仍然为空");

                // 检查数据文件是否存在
                var jsonFile = UnityEngine.Resources.Load<UnityEngine.TextAsset>("MineralData/Data/SendaiMineralDatabase");
                if (jsonFile != null)
                {
                    Debug.Log($"[EncyclopediaSetupTool] 数据文件存在，大小: {jsonFile.text.Length} 字符");
                    Debug.Log($"[EncyclopediaSetupTool] 数据内容预览: {jsonFile.text.Substring(0, Mathf.Min(200, jsonFile.text.Length))}...");
                }
                else
                {
                    Debug.LogError("[EncyclopediaSetupTool] 数据文件不存在: Resources/MineralData/Data/SendaiMineralDatabase");
                }
            }
        }
        else
        {
            Debug.LogWarning("[EncyclopediaSetupTool] 未找到EncyclopediaData组件");
        }
    }

    [MenuItem("Tools/图鉴系统/强制初始化单例")]
    public static void ForceInitializeSingletons()
    {
        Debug.Log("[EncyclopediaSetupTool] 开始强制初始化单例...");

        // 强制初始化EncyclopediaData单例
        var encyclopediaData = FindObjectOfType<EncyclopediaData>();
        if (encyclopediaData != null)
        {
            Debug.Log($"[EncyclopediaSetupTool] 找到EncyclopediaData组件: {encyclopediaData.gameObject.name}");
            Debug.Log($"[EncyclopediaSetupTool] GameObject激活状态: {encyclopediaData.gameObject.activeInHierarchy}");
            Debug.Log($"[EncyclopediaSetupTool] Component启用状态: {encyclopediaData.enabled}");

            // 确保GameObject和组件都是启用的
            if (!encyclopediaData.gameObject.activeInHierarchy)
            {
                encyclopediaData.gameObject.SetActive(true);
                Debug.Log("[EncyclopediaSetupTool] 已激活EncyclopediaData GameObject");
            }

            if (!encyclopediaData.enabled)
            {
                encyclopediaData.enabled = true;
                Debug.Log("[EncyclopediaSetupTool] 已启用EncyclopediaData组件");
            }

            // 检查Instance是否已设置
            if (Encyclopedia.EncyclopediaData.Instance == null)
            {
                Debug.Log("[EncyclopediaSetupTool] Instance为null，尝试手动调用Awake方法...");

                // 使用反射调用Awake方法
                var awakeMethod = typeof(EncyclopediaData).GetMethod("Awake",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (awakeMethod != null)
                {
                    try
                    {
                        awakeMethod.Invoke(encyclopediaData, null);
                        Debug.Log("[EncyclopediaSetupTool] 已调用EncyclopediaData.Awake方法");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[EncyclopediaSetupTool] 调用Awake方法失败: {e.Message}");
                    }
                }
                else
                {
                    Debug.LogWarning("[EncyclopediaSetupTool] 未找到Awake方法");
                }
            }
            else
            {
                Debug.Log("[EncyclopediaSetupTool] EncyclopediaData.Instance已存在");
            }

            Debug.Log($"[EncyclopediaSetupTool] 最终验证Instance: {Encyclopedia.EncyclopediaData.Instance != null}");

            // 详细验证
            if (Encyclopedia.EncyclopediaData.Instance != null)
            {
                Debug.Log($"[EncyclopediaSetupTool] Instance验证成功: {Encyclopedia.EncyclopediaData.Instance.gameObject.name}");
                Debug.Log($"[EncyclopediaSetupTool] IsDataLoaded: {Encyclopedia.EncyclopediaData.Instance.IsDataLoaded}");

                // 如果数据没有加载，尝试手动加载
                if (!Encyclopedia.EncyclopediaData.Instance.IsDataLoaded)
                {
                    Debug.Log("[EncyclopediaSetupTool] 数据未加载，尝试手动触发数据加载...");
                    ForceReloadEncyclopediaData();
                }
            }
            else
            {
                Debug.LogError("[EncyclopediaSetupTool] Instance验证失败，仍然为null");
            }
        }
        else
        {
            Debug.LogWarning("[EncyclopediaSetupTool] 未找到EncyclopediaData组件");
        }

        // 强制初始化CollectionManager单例
        var collectionManager = FindObjectOfType<CollectionManager>();
        if (collectionManager != null)
        {
            Debug.Log($"[EncyclopediaSetupTool] 找到CollectionManager组件: {collectionManager.gameObject.name}");
            Debug.Log($"[EncyclopediaSetupTool] GameObject激活状态: {collectionManager.gameObject.activeInHierarchy}");
            Debug.Log($"[EncyclopediaSetupTool] Component启用状态: {collectionManager.enabled}");

            // 确保GameObject和组件都是启用的
            if (!collectionManager.gameObject.activeInHierarchy)
            {
                collectionManager.gameObject.SetActive(true);
                Debug.Log("[EncyclopediaSetupTool] 已激活CollectionManager GameObject");
            }

            if (!collectionManager.enabled)
            {
                collectionManager.enabled = true;
                Debug.Log("[EncyclopediaSetupTool] 已启用CollectionManager组件");
            }

            // 检查Instance是否已设置
            if (Encyclopedia.CollectionManager.Instance == null)
            {
                Debug.Log("[EncyclopediaSetupTool] CollectionManager Instance为null，尝试手动调用Awake方法...");

                // 使用反射调用Awake方法
                var awakeMethod = typeof(CollectionManager).GetMethod("Awake",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (awakeMethod != null)
                {
                    try
                    {
                        awakeMethod.Invoke(collectionManager, null);
                        Debug.Log("[EncyclopediaSetupTool] 已调用CollectionManager.Awake方法");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[EncyclopediaSetupTool] 调用CollectionManager Awake方法失败: {e.Message}");
                    }
                }
                else
                {
                    Debug.LogWarning("[EncyclopediaSetupTool] 未找到CollectionManager Awake方法");
                }
            }
            else
            {
                Debug.Log("[EncyclopediaSetupTool] CollectionManager.Instance已存在");
            }

            Debug.Log($"[EncyclopediaSetupTool] 最终验证CollectionManager Instance: {Encyclopedia.CollectionManager.Instance != null}");

            // 详细验证
            if (Encyclopedia.CollectionManager.Instance != null)
            {
                Debug.Log($"[EncyclopediaSetupTool] CollectionManager Instance验证成功: {Encyclopedia.CollectionManager.Instance.gameObject.name}");

                // 尝试初始化CollectionManager（如果数据系统可用）
                if (Encyclopedia.EncyclopediaData.Instance != null && Encyclopedia.EncyclopediaData.Instance.IsDataLoaded)
                {
                    Debug.Log("[EncyclopediaSetupTool] 数据系统可用，尝试初始化CollectionManager...");

                    // 使用反射调用私有的Initialize方法
                    var initMethod = typeof(CollectionManager).GetMethod("Initialize",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (initMethod != null)
                    {
                        try
                        {
                            initMethod.Invoke(Encyclopedia.CollectionManager.Instance, null);
                            Debug.Log("[EncyclopediaSetupTool] CollectionManager初始化完成");
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogWarning($"[EncyclopediaSetupTool] CollectionManager初始化失败: {e.Message}");
                        }
                    }
                }
            }
            else
            {
                Debug.LogError("[EncyclopediaSetupTool] CollectionManager Instance验证失败，仍然为null");
            }
        }
        else
        {
            Debug.LogWarning("[EncyclopediaSetupTool] 未找到CollectionManager组件");
        }

        // 验证数据访问
        if (Encyclopedia.EncyclopediaData.Instance != null)
        {
            try
            {
                var allEntries = Encyclopedia.EncyclopediaData.Instance.AllEntries;
                Debug.Log($"[EncyclopediaSetupTool] 数据访问测试: {allEntries?.Count ?? 0} 个条目");

                // 如果数据为空，尝试重新加载
                if (allEntries == null || allEntries.Count == 0)
                {
                    Debug.LogWarning("[EncyclopediaSetupTool] AllEntries为空，尝试重新加载数据...");

                    // 检查原始数据库
                    Debug.Log($"[EncyclopediaSetupTool] 矿物总数: {Encyclopedia.EncyclopediaData.Instance.TotalMinerals}");
                    Debug.Log($"[EncyclopediaSetupTool] 化石总数: {Encyclopedia.EncyclopediaData.Instance.TotalFossils}");
                    Debug.Log($"[EncyclopediaSetupTool] 地层数量: {Encyclopedia.EncyclopediaData.Instance.LayerNames?.Count ?? 0}");

                    // 强制重新加载数据
                    ForceReloadEncyclopediaData();

                    // 再次检查
                    var reloadedEntries = Encyclopedia.EncyclopediaData.Instance.AllEntries;
                    Debug.Log($"[EncyclopediaSetupTool] 重新加载后条目数: {reloadedEntries?.Count ?? 0}");

                    // 数据重新加载后，需要重新初始化CollectionManager的统计
                    if (Encyclopedia.CollectionManager.Instance != null && reloadedEntries?.Count > 0)
                    {
                        Debug.Log("[EncyclopediaSetupTool] 数据重新加载成功，重新初始化CollectionManager统计...");

                        var initMethod = typeof(CollectionManager).GetMethod("Initialize",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                        if (initMethod != null)
                        {
                            try
                            {
                                initMethod.Invoke(Encyclopedia.CollectionManager.Instance, null);
                                Debug.Log("[EncyclopediaSetupTool] CollectionManager统计重新初始化完成");
                            }
                            catch (System.Exception e)
                            {
                                Debug.LogWarning($"[EncyclopediaSetupTool] CollectionManager重新初始化失败: {e.Message}");
                            }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EncyclopediaSetupTool] 数据访问错误: {e.Message}");
            }
        }

        if (Encyclopedia.CollectionManager.Instance != null)
        {
            try
            {
                var stats = Encyclopedia.CollectionManager.Instance.CurrentStats;
                Debug.Log($"[EncyclopediaSetupTool] 收集统计访问测试: {stats?.totalEntries ?? 0} 个总条目");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EncyclopediaSetupTool] 收集统计访问错误: {e.Message}");
            }
        }

        Debug.Log("[EncyclopediaSetupTool] 单例初始化完成");
    }
}
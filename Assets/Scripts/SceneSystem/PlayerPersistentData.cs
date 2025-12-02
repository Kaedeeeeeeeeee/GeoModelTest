using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine.SceneManagement;

/// <summary>
/// 玩家持久化数据管理器 - 在场景切换时保存和恢复玩家状态
/// </summary>
public class PlayerPersistentData : MonoBehaviour
{
    [Header("数据持久化")]
    public bool enableDataPersistence = true;
    [Header("全局解锁的工具 (跨场景)")]
    [SerializeField] private string[] unlockedToolIdsSerialized = new string[0];
    private HashSet<string> unlockedToolIds = new HashSet<string>();
    private const string UnlockedToolsPrefsKey = "PlayerPersistentData.UnlockedToolIds";
    private const string InventoryPrefsKey = "PlayerPersistentData.Inventory";
    private static readonly Vector3 LaboratorySpawnPosition = new Vector3(0f, 0.167999998f, 4.52699995f);
    private static readonly Quaternion LaboratorySpawnRotation = Quaternion.identity;

    [System.Serializable]
    private class InventorySaveData
    {
        public List<SavedSampleDTO> items = new List<SavedSampleDTO>();
    }

    [System.Serializable]
    private class SavedSampleDTO
    {
        public string sampleID;
        public string displayName;
        public string description;
        public string collectionTime; // ISO8601 string
        public float ox, oy, oz;      // originalCollectionPosition
        public string sourceToolID;
        public float totalDepth;
        public float sampleRadius;
        public float depthStart;
        public float depthEnd;
        public int layerCount;
        public string meshDataPath;   // 可选：3D模型路径
    }
    
    // 玩家数据
    private Vector3 playerPosition;
    private Quaternion playerRotation;
    private string currentEquippedTool;
    
    // 场景特定数据
    private Dictionary<string, SceneData> sceneDataMap = new Dictionary<string, SceneData>();

    // 全局样本数据（跨场景保存）
    private List<SampleItem> globalSampleData = new List<SampleItem>();
    
    void Awake()
    {
        // 尝试从PlayerPrefs加载已解锁工具（可选持久化）
        string saved = PlayerPrefs.GetString(UnlockedToolsPrefsKey, string.Empty);
        if (!string.IsNullOrEmpty(saved))
        {
            var parts = saved.Split(',');
            foreach (var p in parts)
            {
                if (!string.IsNullOrEmpty(p)) unlockedToolIds.Add(p);
            }
        }
        // 同步到序列化字段以便调试查看
        unlockedToolIdsSerialized = new List<string>(unlockedToolIds).ToArray();
    }

    void Start()
    {
        // 首次进入播放模式时也应用一次全局工具（无场景切换时）
        EnsureUnlockedToolsApplied();
        // 订阅背包变更并尝试加载背包
        HookInventoryPersistence();
        LoadInventory();
    }

    /// <summary>
    /// 保存当前场景数据
    /// </summary>
    public void SaveCurrentSceneData(string sceneName)
    {
        if (!enableDataPersistence) return;
        
        Debug.Log($"保存场景数据: {sceneName}");
        
        SceneData sceneData = new SceneData();
        
        // 保存玩家位置和旋转
        SavePlayerTransform(sceneData);
        
        // 保存当前装备的工具
        SaveEquippedTool(sceneData);

        // 保存样本数据到全局存储
        SaveSampleData();
        
        // 保存到字典
        sceneDataMap[sceneName] = sceneData;
        
        Debug.Log($"场景数据保存完成: {sceneName}");
    }
    
    /// <summary>
    /// 恢复场景数据
    /// </summary>
    public void RestoreSceneData(string sceneName)
    {
        if (!enableDataPersistence) return;
        
        Debug.Log($"恢复场景数据: {sceneName}");
        
        if (sceneDataMap.TryGetValue(sceneName, out SceneData sceneData))
        {
            if (sceneName == "Laboratory Scene")
            {
                sceneData.playerPosition = LaboratorySpawnPosition;
                sceneData.playerRotation = LaboratorySpawnRotation;
            }
            // 恢复玩家位置和旋转
            RestorePlayerTransform(sceneData);
            
            // 恢复装备的工具
            RestoreEquippedTool(sceneData);

            // 恢复样本数据
            RestoreSampleData();

            // 确保全局已解锁工具在当前场景可用
            EnsureUnlockedToolsApplied();
            
            Debug.Log($"场景数据恢复完成: {sceneName}");
        }
        else
        {
            Debug.Log($"场景 {sceneName} 没有保存的数据，使用默认状态");
            SetDefaultSceneState(sceneName);
            // 没有场景存档也要恢复全局工具
            EnsureUnlockedToolsApplied();
        }

        if (sceneName == "Laboratory Scene")
        {
            StartCoroutine(SetPlayerToFixedPosition(LaboratorySpawnPosition, LaboratorySpawnRotation));
        }
    }

    /// <summary>
    /// 标记某个工具已被全局解锁
    /// </summary>
    public void MarkToolUnlocked(string toolId)
    {
        if (string.IsNullOrEmpty(toolId)) return;
        if (unlockedToolIds.Add(toolId))
        {
            // 保存到PlayerPrefs（轻量持久化）
            PlayerPrefs.SetString(UnlockedToolsPrefsKey, string.Join(",", unlockedToolIds));
            PlayerPrefs.Save();
            // 更新调试视图
            unlockedToolIdsSerialized = new List<string>(unlockedToolIds).ToArray();
        }
    }

    /// <summary>
    /// 将全局已解锁的工具应用到当前场景（ToolManager）
    /// </summary>
    public void ApplyUnlockedToolsToScene()
    {
        var tm = FindFirstObjectByType<ToolManager>();
        if (tm == null) return;

        bool changed = false;
        foreach (var id in unlockedToolIds)
        {
            if (!ToolUnlockService.IsToolUnlocked(tm, id))
            {
                ToolUnlockService.UnlockToolById(id);
                changed = true;
            }
        }

        if (changed)
        {
            var ui = FindFirstObjectByType<InventoryUISystem>();
            if (ui != null) ui.RefreshTools();
        }
    }

    // 延迟确保ToolManager初始化后再应用
    void EnsureUnlockedToolsApplied()
    {
        StartCoroutine(EnsureUnlockedToolsApplied_Co());
    }

    System.Collections.IEnumerator EnsureUnlockedToolsApplied_Co()
    {
        // 等待工具系统初始化
        yield return new WaitForSeconds(0.2f);
        ApplyUnlockedToolsToScene();
    }

    // ===== 背包持久化（PlayerPrefs，最多20项） =====
    void HookInventoryPersistence()
    {
        var inv = SampleInventory.Instance;
        if (inv != null)
        {
            inv.OnInventoryChanged += OnInventoryChangedForSave;
        }
        else
        {
            StartCoroutine(WaitForInventoryThen(() =>
            {
                var i = SampleInventory.Instance;
                if (i != null) i.OnInventoryChanged += OnInventoryChangedForSave;
            }));
        }
    }

    void OnDestroy()
    {
        var inv = SampleInventory.Instance;
        if (inv != null)
        {
            inv.OnInventoryChanged -= OnInventoryChangedForSave;
        }
    }

    void OnApplicationQuit()
    {
        SaveInventory();
    }

    void OnInventoryChangedForSave()
    {
        SaveInventory();
    }

    private bool isSavingInventory;

    public void SaveInventory()
    {
        var inv = SampleInventory.Instance;
        if (inv == null) return;

        // 避免在加载过程中因 OnInventoryChanged 触发保存
        if (isSavingInventory) return;
        isSavingInventory = true;

        var data = new InventorySaveData();
        var list = inv.GetInventorySamples();
        int limit = Mathf.Min(list.Count, inv.maxSampleCapacity);
        for (int i = 0; i < limit; i++)
        {
            var s = list[i];
            if (s == null) continue;
            // 仅保存必要字段，禁止序列化 UnityEngine 对象，避免PlayerPrefs膨胀
            var dto = new SavedSampleDTO
            {
                sampleID = s.sampleID,
                displayName = s.displayName,
                description = s.description,
                collectionTime = s.collectionTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                ox = s.originalCollectionPosition.x,
                oy = s.originalCollectionPosition.y,
                oz = s.originalCollectionPosition.z,
                sourceToolID = s.sourceToolID,
                totalDepth = s.totalDepth,
                sampleRadius = s.sampleRadius,
                depthStart = s.depthStart,
                depthEnd = s.depthEnd,
                layerCount = s.layerCount,
                meshDataPath = s.meshDataPath
            };
            data.items.Add(dto);
        }

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(InventoryPrefsKey, json);
        PlayerPrefs.Save();
        Debug.Log($"[PlayerPersistentData] 已保存背包: {data.items.Count} 个样本");
        isSavingInventory = false;
    }

    public void LoadInventory()
    {
        var inv = SampleInventory.Instance;
        if (inv == null)
        {
            StartCoroutine(WaitForInventoryThen(LoadInventory));
            return;
        }

        string json = PlayerPrefs.GetString(InventoryPrefsKey, string.Empty);
        if (string.IsNullOrEmpty(json)) return;

        try
        {
            var data = JsonUtility.FromJson<InventorySaveData>(json);
            if (data?.items == null) return;

            // 清空并恢复
            inv.ClearInventory();
            foreach (var dto in data.items)
            {
                if (dto == null) continue;
                var s = new SampleItem
                {
                    sampleID = dto.sampleID,
                    displayName = dto.displayName,
                    description = dto.description,
                    originalCollectionPosition = new Vector3(dto.ox, dto.oy, dto.oz),
                    sourceToolID = dto.sourceToolID,
                    totalDepth = dto.totalDepth,
                    sampleRadius = dto.sampleRadius,
                    depthStart = dto.depthStart,
                    depthEnd = dto.depthEnd,
                    layerCount = dto.layerCount,
                    meshDataPath = dto.meshDataPath,
                    currentLocation = SampleLocation.InInventory,
                    isPlayerPlaced = false
                };
                // 还原采集时间
                if (!string.IsNullOrEmpty(dto.collectionTime))
                {
                    s.collectionTime = System.DateTime.Parse(dto.collectionTime, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind);
                }
                inv.TryAddSample(s);
            }
            Debug.Log($"[PlayerPersistentData] 已加载背包: {data.items.Count} 个样本");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerPersistentData] 读取背包失败: {e.Message}");
        }
    }

    // 通用等待SampleInventory出现的协程
    System.Collections.IEnumerator WaitForInventoryThen(System.Action action)
    {
        int tries = 0;
        const int maxTries = 40; // 最多20秒
        while (SampleInventory.Instance == null && tries < maxTries)
        {
            tries++;
            yield return new WaitForSeconds(0.5f);
        }
        action?.Invoke();
    }
    
    /// <summary>
    /// 保存玩家位置和旋转
    /// </summary>
    void SavePlayerTransform(SceneData sceneData)
    {
        FirstPersonController player = FindFirstObjectByType<FirstPersonController>();
        if (player != null)
        {
            var activeSceneName = SceneManager.GetActiveScene().name;
            if (activeSceneName == "Laboratory Scene")
            {
                sceneData.playerPosition = LaboratorySpawnPosition;
                sceneData.playerRotation = LaboratorySpawnRotation;
            }
            else
            {
                sceneData.playerPosition = player.transform.position;
                sceneData.playerRotation = player.transform.rotation;
            }
            Debug.Log($"保存玩家位置: {sceneData.playerPosition}");
        }
    }
    
    /// <summary>
    /// 恢复玩家位置和旋转
    /// </summary>
    void RestorePlayerTransform(SceneData sceneData)
    {
        StartCoroutine(RestorePlayerTransformCoroutine(sceneData));
    }
    
    IEnumerator RestorePlayerTransformCoroutine(SceneData sceneData)
    {
        // 等待玩家对象初始化
        yield return new WaitForSeconds(0.1f);
        
        FirstPersonController player = FindFirstObjectByType<FirstPersonController>();
        if (player != null)
        {
            player.transform.position = sceneData.playerPosition;
            player.transform.rotation = sceneData.playerRotation;
            Debug.Log($"{GetTimestamp()} [PlayerPersistentData] 恢复玩家位置 -> {player.name} 目标 {sceneData.playerPosition} (场景: {SceneManager.GetActiveScene().name})");
            
            // 确保玩家对象和摄像机都激活
            player.gameObject.SetActive(true);
            
            // 查找并激活摄像机
            Camera playerCamera = player.GetComponentInChildren<Camera>();
            if (playerCamera != null)
            {
                playerCamera.gameObject.SetActive(true);
                Debug.Log($"{GetTimestamp()} [PlayerPersistentData] 玩家摄像机已激活 -> {playerCamera.name}");
            }
            else
            {
                Debug.LogWarning($"{GetTimestamp()} [PlayerPersistentData] 未找到玩家摄像机");
            }
        }
        else
        {
            Debug.LogWarning($"{GetTimestamp()} [PlayerPersistentData] 未找到玩家对象，尝试设置主摄像机");
            EnsureCameraExists(sceneData.playerPosition, sceneData.playerRotation);
        }
    }
    
    /// <summary>
    /// 保存当前装备的工具
    /// </summary>
    void SaveEquippedTool(SceneData sceneData)
    {
        ToolManager toolManager = FindFirstObjectByType<ToolManager>();
        if (toolManager != null)
        {
            CollectionTool currentTool = toolManager.GetCurrentTool();
            if (currentTool != null)
            {
                sceneData.equippedToolID = currentTool.toolID;
                Debug.Log($"保存装备工具: {sceneData.equippedToolID}");
            }
        }
    }
    
    /// <summary>
    /// 恢复装备的工具
    /// </summary>
    void RestoreEquippedTool(SceneData sceneData)
    {
        if (string.IsNullOrEmpty(sceneData.equippedToolID)) return;
        
        StartCoroutine(RestoreEquippedToolCoroutine(sceneData));
    }
    
    IEnumerator RestoreEquippedToolCoroutine(SceneData sceneData)
    {
        // 等待工具管理器初始化
        yield return new WaitForSeconds(0.2f);
        
        ToolManager toolManager = FindFirstObjectByType<ToolManager>();
        if (toolManager != null && toolManager.availableTools != null)
        {
            foreach (var tool in toolManager.availableTools)
            {
                if (tool != null && tool.toolID == sceneData.equippedToolID)
                {
                    toolManager.EquipTool(tool);
                    Debug.Log($"恢复装备工具: {sceneData.equippedToolID}");
                    break;
                }
            }
        }
    }
    
    
    /// <summary>
    /// 设置默认场景状态
    /// </summary>
    void SetDefaultSceneState(string sceneName)
    {
        // 设置默认玩家位置
        Vector3 defaultPosition = Vector3.zero;
        Quaternion defaultRotation = Quaternion.identity;
        
        // 根据场景设置不同的默认位置
        switch (sceneName)
        {
            case "MainScene":
                defaultPosition = new Vector3(-29.9230003f, 14.3459997f, -20.9599991f);
                defaultRotation = new Quaternion(0f, 0.995849609f, 0f, 0.0910143629f);
                break;
            case "Laboratory Scene":
                defaultPosition = LaboratorySpawnPosition;
                defaultRotation = LaboratorySpawnRotation;
                break;
        }
        
        StartCoroutine(SetDefaultPlayerPosition(defaultPosition, defaultRotation));
    }

    IEnumerator SetPlayerToFixedPosition(Vector3 position, Quaternion rotation)
    {
        const int maxAttempts = 120;
        int attempts = 0;
        FirstPersonController player = null;

        while (attempts < maxAttempts)
        {
            player = FindFirstObjectByType<FirstPersonController>();
            if (player != null)
            {
                break;
            }
            attempts++;
            yield return null;
        }

        if (player != null)
        {
            player.transform.position = position;
            player.transform.rotation = rotation;
            Debug.Log($"{GetTimestamp()} [PlayerPersistentData] SetPlayerToFixedPosition -> {player.name} 位置 {position} (尝试 {attempts})");
        }
        else
        {
            Debug.LogWarning($"{GetTimestamp()} [PlayerPersistentData] 未找到玩家对象，无法设置固定位置 (目标 {position})");
        }
    }
    
    IEnumerator SetDefaultPlayerPosition(Vector3 position, Quaternion rotation)
    {
        yield return new WaitForSeconds(0.1f);
        
        FirstPersonController player = FindFirstObjectByType<FirstPersonController>();
        if (player != null)
        {
            player.transform.position = position;
            player.transform.rotation = rotation;
            Debug.Log($"{GetTimestamp()} [PlayerPersistentData] 设置默认玩家位置 -> {player.name} 位置 {position}");
            
            // 确保玩家对象和摄像机都激活
            player.gameObject.SetActive(true);
            
            // 查找并激活摄像机
            Camera playerCamera = player.GetComponentInChildren<Camera>();
            if (playerCamera != null)
            {
                playerCamera.gameObject.SetActive(true);
                Debug.Log($"{GetTimestamp()} [PlayerPersistentData] 玩家摄像机已激活 -> {playerCamera.name}");
            }
            else
            {
                Debug.LogWarning($"{GetTimestamp()} [PlayerPersistentData] 未找到玩家摄像机");
            }
        }
        else
        {
            Debug.LogWarning($"{GetTimestamp()} [PlayerPersistentData] 未找到玩家对象，尝试查找主摄像机");
            EnsureCameraExists(position, rotation);
        }
    }

    public void ForceSetPlayerToLaboratorySpawn()
    {
        StartCoroutine(SetPlayerToFixedPosition(LaboratorySpawnPosition, LaboratorySpawnRotation));
        StartCoroutine(MonitorPlayerPosition("LabForceSet"));
    }
    
    /// <summary>
    /// 确保场景中有可用的摄像机
    /// </summary>
    void EnsureCameraExists(Vector3 position, Quaternion rotation)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            // 查找任何摄像机
            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            if (cameras.Length > 0)
            {
                mainCamera = cameras[0];
                mainCamera.tag = "MainCamera";
                Debug.Log($"找到摄像机并设为主摄像机: {mainCamera.name}");
            }
        }
        
        if (mainCamera != null)
        {
            mainCamera.gameObject.SetActive(true);
            mainCamera.transform.position = position + Vector3.up * 1.6f; // 摄像机高度
            mainCamera.transform.rotation = rotation;
            Debug.Log($"摄像机位置已设置: {mainCamera.transform.position}");
        }
        else
        {
            Debug.LogError("场景中没有找到任何摄像机！");
            CreateEmergencyCamera(position, rotation);
        }
    }
    
    /// <summary>
    /// 创建紧急摄像机
    /// </summary>
    void CreateEmergencyCamera(Vector3 position, Quaternion rotation)
    {
        GameObject cameraObj = new GameObject("Emergency Camera");
        Camera camera = cameraObj.AddComponent<Camera>();
        camera.tag = "MainCamera";
        
        cameraObj.transform.position = position + Vector3.up * 1.6f;
        cameraObj.transform.rotation = rotation;
        
        // 添加音频监听器（确保场景中只有一个）
        AudioListener[] existingListeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        if (existingListeners.Length == 0)
        {
            cameraObj.AddComponent<AudioListener>();
            Debug.Log("✅ 添加AudioListener到紧急摄像机");
        }
        else
        {
            Debug.Log($"⚠️ 场景中已存在 {existingListeners.Length} 个AudioListener，跳过添加");
        }
        
        Debug.Log("创建紧急摄像机完成");
    }
    
    /// <summary>
    /// 清除场景数据
    /// </summary>
    public void ClearSceneData(string sceneName)
    {
        if (sceneDataMap.ContainsKey(sceneName))
        {
            sceneDataMap.Remove(sceneName);
            Debug.Log($"清除场景数据: {sceneName}");
        }
    }
    
    /// <summary>
    /// 清除所有场景数据
    /// </summary>
    public void ClearAllSceneData()
    {
        sceneDataMap.Clear();
        Debug.Log("清除所有场景数据");
    }

    /// <summary>
    /// 保存样本数据
    /// </summary>
    void SaveSampleData()
    {
        SampleInventory inventory = SampleInventory.Instance;
        if (inventory != null)
        {
            var samples = inventory.GetAllSamples();
            globalSampleData.Clear();
            globalSampleData.AddRange(samples);
            Debug.Log($"保存了 {globalSampleData.Count} 个样本数据");
        }
        else
        {
            Debug.LogWarning("SampleInventory不存在，无法保存样本数据");
        }
    }

    /// <summary>
    /// 恢复样本数据
    /// </summary>
    void RestoreSampleData()
    {
        if (globalSampleData.Count == 0)
        {
            Debug.Log("没有样本数据需要恢复");
            return;
        }

        StartCoroutine(RestoreSampleDataDelayed());
    }

    /// <summary>
    /// 延迟恢复样本数据（等待SampleInventory初始化）
    /// </summary>
    IEnumerator RestoreSampleDataDelayed()
    {
        // 等待SampleInventory初始化
        yield return new WaitForSeconds(1f);

        SampleInventory inventory = SampleInventory.Instance;
        if (inventory == null)
        {
            Debug.LogWarning("SampleInventory仍然不存在，尝试创建");
            // 尝试查找或创建SampleInventory
            GameObject inventoryObj = GameObject.Find("SampleInventory");
            if (inventoryObj == null)
            {
                inventoryObj = new GameObject("SampleInventory");
                inventory = inventoryObj.AddComponent<SampleInventory>();

                // 只在播放模式下使用DontDestroyOnLoad
                if (Application.isPlaying)
                {
                    DontDestroyOnLoad(inventoryObj);
                }
            }
            else
            {
                inventory = inventoryObj.GetComponent<SampleInventory>();
            }
        }

        if (inventory != null)
        {
            // 清空当前背包
            inventory.ClearInventory();

            // 恢复保存的样本
            foreach (var sample in globalSampleData)
            {
                inventory.TryAddSample(sample);
            }

            Debug.Log($"恢复了 {globalSampleData.Count} 个样本到背包");

            // 刷新UI显示
            WarehouseUI warehouseUI = FindFirstObjectByType<WarehouseUI>();
            if (warehouseUI != null && warehouseUI.inventoryPanel != null)
            {
                warehouseUI.inventoryPanel.RefreshInventoryDisplay();
                Debug.Log("刷新了仓库UI显示");
            }
        }
        else
        {
            Debug.LogError("无法创建SampleInventory，样本数据恢复失败");
        }
    }

    /// <summary>
    /// 获取已收集的样本数据
    /// </summary>
    public List<SampleItem> GetCollectedSamples()
    {
        return globalSampleData.ToList();
    }

    /// <summary>
    /// 手动添加样本数据（用于测试）
    /// </summary>
    public void AddSampleData(SampleItem sample)
    {
        globalSampleData.Add(sample);
        Debug.Log($"手动添加样本数据: {sample.displayName}");
    }

    string GetTimestamp()
    {
        return $"[{Time.time:F3}s]";
    }

    IEnumerator MonitorPlayerPosition(string label)
    {
        const int samples = 6;
        const float interval = 0.5f;
        for (int i = 0; i < samples; i++)
        {
            yield return new WaitForSeconds(interval);
            FirstPersonController player = FindFirstObjectByType<FirstPersonController>();
            if (player != null)
            {
                Vector3 pos = player.transform.position;
                bool grounded = player.GetComponent<CharacterController>()?.isGrounded ?? false;
                Debug.Log($"{GetTimestamp()} [PlayerPersistentData] 监控 {label} t={i * interval:F1}s 位置 {pos} grounded={grounded}");

                if (label == "LabForceSet" && (pos - LaboratorySpawnPosition).sqrMagnitude > 0.5f * 0.5f)
                {
                    Debug.LogWarning($"{GetTimestamp()} [PlayerPersistentData] 检测到实验室定位被改动 -> 当前位置 {pos}\n{new System.Diagnostics.StackTrace(true)}");
                    yield break;
                }
            }
        }
    }
}

/// <summary>
/// 场景数据结构 - 简化版，只保存玩家状态
/// </summary>
[System.Serializable]
public class SceneData
{
    public Vector3 playerPosition;
    public Quaternion playerRotation;
    public string equippedToolID;
    
    public SceneData()
    {
        playerPosition = Vector3.zero;
        playerRotation = Quaternion.identity;
        equippedToolID = "";
    }
}

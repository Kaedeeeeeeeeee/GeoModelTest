using UnityEngine;

/// <summary>
/// 运行时按工具ID解锁工具并注册到ToolManager与UI。
/// 前提：对应工具组件已挂在玩家对象上（由GameInitializer添加），但未注册为可用工具。
/// </summary>
public static class ToolUnlockService
{
    /// <summary>
    /// 通过工具ID解锁工具（如 "1000" 简易钻探、"1002" 地质锤、"1100" 无人机、"1101" 钻探车）。
    /// </summary>
    public static bool UnlockToolById(string toolId)
    {
        var toolManager = Object.FindFirstObjectByType<ToolManager>();
        if (toolManager == null)
        {
            Debug.LogError("[ToolUnlockService] 未找到 ToolManager");
            return false;
        }

        // 在同一物体上查找所有 CollectionTool 组件
        var tools = toolManager.GetComponents<CollectionTool>();
        foreach (var tool in tools)
        {
            if (tool != null && ToolMatchesId(tool, toolId))
            {
                ConfigureKnownTool(tool, toolId);

                // 若已存在于可用列表则不重复添加
                if (IsToolUnlocked(toolManager, toolId))
                {
                    Debug.Log($"[ToolUnlockService] 工具 {toolId} 已解锁");
                    return true;
                }

                toolManager.AddTool(tool);
                Debug.Log($"[ToolUnlockService] 已解锁工具: {tool.toolID}");
                // 记录到全局玩家数据
                var g = Object.FindFirstObjectByType<PlayerPersistentData>();
                g?.MarkToolUnlocked(toolId);
                return true;
            }
        }

        // 若未在玩家对象上找到对应组件，进行兜底创建（关键工具）
        if (toolId == "1002") // 地质锤
        {
            var created = ToolUnlockService_Internal.EnsureHammerTool(toolManager);
            if (created != null)
            {
                toolManager.AddTool(created);
                Debug.Log("[ToolUnlockService] 兜底：自动添加并解锁地质锤(1002)");
                var g = Object.FindFirstObjectByType<PlayerPersistentData>();
                g?.MarkToolUnlocked(toolId);
                return true;
            }
        }
        else if (toolId == "1000") // 简易钻探
        {
            var created = ToolUnlockService_Internal.EnsureSimpleDrillTool(toolManager);
            if (created != null)
            {
                toolManager.AddTool(created);
                Debug.Log("[ToolUnlockService] 兜底：自动添加并解锁简易钻探工具(1000)");
                var g = Object.FindFirstObjectByType<PlayerPersistentData>();
                g?.MarkToolUnlocked(toolId);
                return true;
            }
        }
        else if (toolId == "1001") // 钻塔工具
        {
            var created = ToolUnlockService_Internal.EnsureDrillTowerTool(toolManager);
            if (created != null)
            {
                toolManager.AddTool(created);
                Debug.Log("[ToolUnlockService] 兜底：自动添加并解锁钻塔工具(1001)");
                var g = Object.FindFirstObjectByType<PlayerPersistentData>();
                g?.MarkToolUnlocked(toolId);
                return true;
            }
        }
        else if (toolId == "1100") // 无人机
        {
            var created = ToolUnlockService_Internal.EnsureDroneTool(toolManager);
            if (created != null)
            {
                toolManager.AddTool(created);
                Debug.Log("[ToolUnlockService] 兜底：自动添加并解锁无人机(1100)");
                var g = Object.FindFirstObjectByType<PlayerPersistentData>();
                g?.MarkToolUnlocked(toolId);
                return true;
            }
        }
        else if (toolId == "999") // 场景切换器
        {
            var created = ToolUnlockService_Internal.EnsureSceneSwitcherTool(toolManager);
            if (created != null)
            {
                toolManager.AddTool(created);
                Debug.Log("[ToolUnlockService] 兜底：自动添加并解锁场景切换器(999)");
                var g = Object.FindFirstObjectByType<PlayerPersistentData>();
                g?.MarkToolUnlocked(toolId);
                return true;
            }
        }

        Debug.LogWarning($"[ToolUnlockService] 未在玩家对象上找到工具组件，且无兜底可用: {toolId}");
        return false;
    }

    /// <summary>
    /// 判断指定工具ID是否已在可用列表中。
    /// </summary>
    public static bool IsToolUnlocked(ToolManager toolManager, string toolId)
    {
        if (toolManager.availableTools == null) return false;
        foreach (var t in toolManager.availableTools)
        {
            if (t != null && ToolMatchesId(t, toolId)) return true;
        }
        return false;
    }

    private static bool ToolMatchesId(CollectionTool tool, string toolId)
    {
        if (tool == null || string.IsNullOrEmpty(toolId)) return false;
        if (tool.toolID == toolId) return true;

        return toolId switch
        {
            "1000" => tool is SimpleDrillTool,
            "1001" => tool is DrillTowerTool,
            "1002" => tool is HammerTool,
            "999" => tool is SceneSwitcherTool,
            "1100" => tool is DroneTool,
            "1101" => tool is DrillCarTool,
            _ => false
        };
    }

    private static void ConfigureKnownTool(CollectionTool tool, string toolId)
    {
        if (tool == null) return;

        tool.toolID = toolId;
        if (tool is HammerTool)
        {
            tool.toolName = "地质锤";
        }
        else if (tool is SimpleDrillTool)
        {
            tool.toolName = "简易钻探";
        }
        else if (tool is DrillTowerTool)
        {
            tool.toolName = "钻塔工具";
        }
        else if (tool is SceneSwitcherTool &&
                 (string.IsNullOrEmpty(tool.toolName) || tool.toolName == "Collection Tool"))
        {
            tool.toolName = "场景切换器";
        }
        else if (tool is DroneTool)
        {
            tool.toolName = "无人机";
        }
        else if (tool is DrillCarTool)
        {
            tool.toolName = "钻探车";
        }
    }
}

// ===== Internal helpers =====
public static class ToolUnlockService_Internal
{
    /// <summary>
    /// 若玩家对象未挂地质锤，则自动添加一个最小可用的 HammerTool 组件。
    /// </summary>
    public static HammerTool EnsureHammerTool(ToolManager toolManager)
    {
        if (toolManager == null) return null;

        // 若已存在组件则返回
        var existing = toolManager.GetComponent<HammerTool>();
        if (existing != null)
        {
            existing.toolID = "1002";
            existing.toolName = "地质锤";
            return existing;
        }

        // 添加组件到与 ToolManager 相同的对象
        var hammer = toolManager.gameObject.AddComponent<HammerTool>();
        hammer.toolID = "1002";
        hammer.toolName = "地质锤";
        // 其余字段使用 HammerTool 内部默认逻辑；无预制体时仍可工作

        return hammer;
    }

    public static SceneSwitcherTool EnsureSceneSwitcherTool(ToolManager toolManager)
    {
        if (toolManager == null) return null;

        var switcher = toolManager.GetComponent<SceneSwitcherTool>();
        if (switcher == null)
        {
            switcher = toolManager.gameObject.AddComponent<SceneSwitcherTool>();
        }

        switcher.toolID = "999";
        if (string.IsNullOrEmpty(switcher.toolName) || switcher.toolName == "Collection Tool")
        {
            switcher.toolName = "场景切换器";
        }

        return switcher;
    }

    public static SimpleDrillTool EnsureSimpleDrillTool(ToolManager toolManager)
    {
        if (toolManager == null) return null;

        var simple = toolManager.GetComponent<SimpleDrillTool>();
        if (simple != null) return simple;

        simple = toolManager.gameObject.AddComponent<SimpleDrillTool>();
        simple.toolID = "1000";
        simple.toolName = "简易钻探";
        return simple;
    }

    public static DrillTowerTool EnsureDrillTowerTool(ToolManager toolManager)
    {
        if (toolManager == null) return null;

        var drillTower = toolManager.GetComponent<DrillTowerTool>();
        if (drillTower == null)
        {
            drillTower = toolManager.gameObject.AddComponent<DrillTowerTool>();
            drillTower.toolID = "1001";
            drillTower.toolName = "钻塔工具";
            drillTower.interactionRange = Mathf.Approximately(drillTower.interactionRange, 0f) ? 3f : drillTower.interactionRange;
            drillTower.maxDrillDepths = drillTower.maxDrillDepths <= 0 ? 5 : drillTower.maxDrillDepths;
            drillTower.depthPerDrill = Mathf.Approximately(drillTower.depthPerDrill, 0f) ? 2f : drillTower.depthPerDrill;
            drillTower.sampleRingRadius = Mathf.Approximately(drillTower.sampleRingRadius, 0f) ? 2.5f : drillTower.sampleRingRadius;
            drillTower.sampleElevation = Mathf.Approximately(drillTower.sampleElevation, 0f) ? 3f : drillTower.sampleElevation;
            drillTower.sampleSpacing = Mathf.Approximately(drillTower.sampleSpacing, 0f) ? 0.8f : drillTower.sampleSpacing;
        }

        // 若没有预制体，优先从 Resources 加载已配好材质的真 prefab，加载失败再走运行时兜底
        if (drillTower.drillTowerPrefab == null)
        {
            var loadedPrefab = Resources.Load<GameObject>("Prefabs/DrillTower/DrillTowerPrefab");
            drillTower.drillTowerPrefab = loadedPrefab != null ? loadedPrefab : CreateFallbackDrillTowerPrefab();
        }

        if (drillTower.prefabToPlace == null)
        {
            drillTower.prefabToPlace = drillTower.drillTowerPrefab;
        }

        // 任务解锁路径不会跑 GameInitializer.InitializeInteractionUI，这里兜底确保
        // 场景中存在 DrillTowerInteractionUI（负责显示"按 F 钻探/按 G 收回"提示）。
        EnsureDrillTowerInteractionUI();

        return drillTower;
    }

    private static void EnsureDrillTowerInteractionUI()
    {
        if (Object.FindFirstObjectByType<DrillTowerInteractionUI>() != null) return;

        var uiObj = new GameObject("DrillTowerInteractionUI");
        var ui = uiObj.AddComponent<DrillTowerInteractionUI>();
        ui.promptDistance = 3f;
    }

    private static GameObject CreateFallbackDrillTowerPrefab()
    {
        var towerManager = Object.FindFirstObjectByType<ToolManager>();
        Transform parent = towerManager != null ? towerManager.transform : null;

        var towerPrefab = new GameObject("FallbackDrillTower");
        if (parent != null)
        {
            towerPrefab.transform.SetParent(parent, false);
        }

        var basePlatform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        basePlatform.name = "BasePlatform";
        basePlatform.transform.SetParent(towerPrefab.transform, false);
        basePlatform.transform.localPosition = new Vector3(0f, 0.1f, 0f);
        basePlatform.transform.localScale = new Vector3(1.2f, 0.2f, 1.2f);

        var towerBody = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        towerBody.name = "TowerBody";
        towerBody.transform.SetParent(towerPrefab.transform, false);
        towerBody.transform.localPosition = new Vector3(0f, 1.4f, 0f);
        towerBody.transform.localScale = new Vector3(0.8f, 1.2f, 0.8f);

        var drillArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        drillArm.name = "DrillArm";
        drillArm.transform.SetParent(towerPrefab.transform, false);
        drillArm.transform.localPosition = new Vector3(0f, 2.6f, 0.6f);
        drillArm.transform.localScale = new Vector3(0.3f, 0.3f, 1.2f);

        var drillBit = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        drillBit.name = "DrillBit";
        drillBit.transform.SetParent(towerPrefab.transform, false);
        drillBit.transform.localPosition = new Vector3(0f, 1.0f, 0f);
        drillBit.transform.localScale = new Vector3(0.2f, 0.4f, 0.2f);

        var standardMaterial = new Material(Shader.Find("Standard"))
        {
            color = new Color(1f, 1f, 1f, 1f)
        };
        standardMaterial.SetFloat("_Metallic", 0.2f);
        standardMaterial.SetFloat("_Glossiness", 0.6f);

        foreach (var renderer in towerPrefab.GetComponentsInChildren<Renderer>())
        {
            renderer.sharedMaterial = standardMaterial;
        }

        var rigidbody = towerPrefab.AddComponent<Rigidbody>();
        rigidbody.mass = 100f;
        rigidbody.linearDamping = 5f;
        rigidbody.angularDamping = 10f;
        rigidbody.centerOfMass = new Vector3(0f, 0.5f, 0f);
        rigidbody.freezeRotation = true;

        var capsule = towerPrefab.AddComponent<CapsuleCollider>();
        capsule.radius = 0.6f;
        capsule.height = 2.8f;
        capsule.center = new Vector3(0f, 1.4f, 0f);
        capsule.direction = 1;

        var interactionCollider = towerPrefab.AddComponent<BoxCollider>();
        interactionCollider.isTrigger = true;
        interactionCollider.size = new Vector3(2f, 3f, 2f);
        interactionCollider.center = new Vector3(0f, 1.4f, 0f);

        towerPrefab.SetActive(false);
        return towerPrefab;
    }

    public static DroneTool EnsureDroneTool(ToolManager toolManager)
    {
        if (toolManager == null) return null;

        var droneTool = toolManager.GetComponent<DroneTool>();
        if (droneTool == null)
        {
            droneTool = toolManager.gameObject.AddComponent<DroneTool>();
            droneTool.toolID = "1100";
            droneTool.toolName = "无人机";
            droneTool.useRange = Mathf.Approximately(droneTool.useRange, 0f) ? 100f : droneTool.useRange;
            droneTool.useCooldown = Mathf.Approximately(droneTool.useCooldown, 0f) ? 2f : droneTool.useCooldown;
            droneTool.placementOffset = Mathf.Approximately(droneTool.placementOffset, 0f) ? 1f : droneTool.placementOffset;
        }

        if (droneTool.prefabToPlace == null)
        {
            droneTool.prefabToPlace = CreateFallbackDronePrefab(toolManager.transform);
        }

        return droneTool;
    }

    private static GameObject CreateFallbackDronePrefab(Transform parent)
    {
        var prefab = new GameObject("FallbackDrone");
        if (parent != null) prefab.transform.SetParent(parent, false);

        var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = "DroneBody";
        body.transform.SetParent(prefab.transform, false);
        body.transform.localScale = new Vector3(0.6f, 0.1f, 0.6f);
        body.transform.localPosition = new Vector3(0f, 0.1f, 0f);

        var rotorHolder = new GameObject("Rotors");
        rotorHolder.transform.SetParent(prefab.transform, false);
        rotorHolder.transform.localPosition = new Vector3(0f, 0.25f, 0f);

        float rotorRadius = 0.45f;
        for (int i = 0; i < 4; i++)
        {
            var rotor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rotor.name = $"Rotor_{i}";
            rotor.transform.SetParent(rotorHolder.transform, false);
            rotor.transform.localScale = new Vector3(0.1f, 0.01f, 0.3f);
            rotor.transform.localPosition = new Vector3(
                Mathf.Cos(i * Mathf.PI / 2f) * rotorRadius,
                0f,
                Mathf.Sin(i * Mathf.PI / 2f) * rotorRadius);
        }

        foreach (var renderer in prefab.GetComponentsInChildren<Renderer>())
        {
            renderer.sharedMaterial = new Material(Shader.Find("Standard"))
            {
                color = new Color(0.2f, 0.6f, 0.9f, 1f)
            };
        }

        if (prefab.GetComponent<Rigidbody>() == null)
        {
            var rb = prefab.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.linearDamping = 5f;
            rb.angularDamping = 5f;
        }

        if (prefab.GetComponent<Collider>() == null)
        {
            var sphere = prefab.AddComponent<SphereCollider>();
            sphere.radius = 0.6f;
        }

        if (prefab.GetComponent<DroneController>() == null)
        {
            prefab.AddComponent<DroneController>();
        }

        var recaller = prefab.GetComponent<PlacedToolRecaller>();
        if (recaller == null)
        {
            recaller = prefab.AddComponent<PlacedToolRecaller>();
            recaller.toolName = "无人机";
            recaller.interactionRange = 5f;
            recaller.recallKey = KeyCode.G;
        }

        prefab.SetActive(false);
        return prefab;
    }
}

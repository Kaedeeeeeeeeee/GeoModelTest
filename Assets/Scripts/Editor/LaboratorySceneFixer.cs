using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;

public class LaboratorySceneFixer
{
    [MenuItem("Tools/研究室移动端UI/修复研究室场景")]
    public static void FixLaboratoryScene()
    {
        Debug.Log("=== 修复研究室场景 ===");

        // 1. 检查EventSystem
        FixEventSystem();

        // 2. 检查仓库系统
        FixWarehouseSystem();

        // 3. 检查数据持久化
        FixDataPersistence();

        // 4. 强制初始化移动端UI
        ForceInitializeMobileUI();

        Debug.Log("🎉 研究室场景修复完成");
        Debug.Log("💡 提示：如果仍有问题，请尝试使用其他菜单项进行单独测试");
    }

    private static void FixEventSystem()
    {
        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystem = eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
            Debug.Log("✅ 创建了EventSystem");
        }
        else
        {
            Debug.Log("✅ EventSystem已存在");
        }
    }

    private static void FixWarehouseSystem()
    {
        // 检查仓库初始化器
        WarehouseGameInitializer initializer = Object.FindFirstObjectByType<WarehouseGameInitializer>();
        if (initializer == null)
        {
            GameObject initObj = new GameObject("WarehouseGameInitializer");
            initializer = initObj.AddComponent<WarehouseGameInitializer>();
            Debug.Log("✅ 创建了WarehouseGameInitializer");
        }

        // 强制重新初始化仓库系统
        initializer.ReinitializeSystem();
        Debug.Log("✅ 重新初始化了仓库系统");
    }

    private static void FixDataPersistence()
    {
        // 检查PlayerPersistentData
        PlayerPersistentData persistentData = Object.FindFirstObjectByType<PlayerPersistentData>();
        if (persistentData != null)
        {
            Debug.Log($"✅ PlayerPersistentData存在，样本数量: {persistentData.GetCollectedSamples().Count}");

            // 如果没有样本数据，创建一些测试数据
            var samples = persistentData.GetCollectedSamples();
            if (samples.Count == 0)
            {
                Debug.Log("创建测试样本数据...");
                CreateTestSampleData(persistentData);
            }

            // 强制恢复样本数据到背包
            ForceRestoreSampleData(persistentData);
        }
        else
        {
            Debug.LogWarning("❌ PlayerPersistentData不存在，创建新的数据管理器");
            CreatePlayerPersistentData();
        }
    }

    private static void CreatePlayerPersistentData()
    {
        GameObject persistentObj = new GameObject("PlayerPersistentData");
        PlayerPersistentData persistentData = persistentObj.AddComponent<PlayerPersistentData>();

        // 在编辑器模式下不能使用DontDestroyOnLoad
        if (Application.isPlaying)
        {
            Object.DontDestroyOnLoad(persistentObj);
        }

        // 创建测试样本数据
        CreateTestSampleData(persistentData);
        Debug.Log("✅ 创建了PlayerPersistentData和测试样本");
    }

    private static void CreateTestSampleData(PlayerPersistentData persistentData)
    {
        // 创建一些测试样本
        for (int i = 0; i < 3; i++)
        {
            SampleItem testSample = new SampleItem();
            testSample.sampleID = System.Guid.NewGuid().ToString();
            testSample.displayName = $"测试样本 {i + 1}";
            testSample.description = $"这是第 {i + 1} 个测试样本";
            testSample.collectionTime = System.DateTime.Now;

            persistentData.AddSampleData(testSample);
        }
        Debug.Log("✅ 创建了3个测试样本");
    }

    private static void ForceRestoreSampleData(PlayerPersistentData persistentData)
    {
        var samples = persistentData.GetCollectedSamples();
        if (samples.Count > 0)
        {
            // 确保SampleInventory存在
            SampleInventory inventory = SampleInventory.Instance;
            if (inventory == null)
            {
                GameObject inventoryObj = new GameObject("SampleInventory");
                inventory = inventoryObj.AddComponent<SampleInventory>();

                // 在编辑器模式下不能使用DontDestroyOnLoad
                if (Application.isPlaying)
                {
                    Object.DontDestroyOnLoad(inventoryObj);
                }
                Debug.Log("✅ 创建了SampleInventory");
            }

            // 清空并恢复样本
            inventory.ClearInventory();
            foreach (var sample in samples)
            {
                inventory.TryAddSample(sample);
            }
            Debug.Log($"✅ 恢复了 {samples.Count} 个样本到背包");

            // 强制刷新UI
            WarehouseUI warehouseUI = Object.FindFirstObjectByType<WarehouseUI>();
            if (warehouseUI != null && warehouseUI.inventoryPanel != null)
            {
                warehouseUI.inventoryPanel.RefreshInventoryDisplay();
                Debug.Log("✅ 刷新了仓库UI显示");
            }
        }
    }

    private static void ForceInitializeMobileUI()
    {
        // 确保MobileInputManager存在
        MobileInputManager inputManager = MobileInputManager.Instance;
        if (inputManager == null)
        {
            GameObject inputManagerObj = new GameObject("MobileInputManager");
            inputManager = inputManagerObj.AddComponent<MobileInputManager>();
            Debug.Log("✅ 创建MobileInputManager");
        }

        // 启用桌面测试模式
        inputManager.EnableDesktopTestMode(true);
        Debug.Log("✅ 启用桌面测试模式");

        // 确保MobileControlsUI存在
        MobileControlsUI controlsUI = Object.FindFirstObjectByType<MobileControlsUI>();
        if (controlsUI == null)
        {
            GameObject controlsUIObj = new GameObject("MobileControlsUI");
            controlsUI = controlsUIObj.AddComponent<MobileControlsUI>();
            Debug.Log("✅ 创建MobileControlsUI");
        }

        // 配置为强制显示
        controlsUI.forceShowOnDesktop = true;
        controlsUI.gameObject.SetActive(true);

        Debug.Log("✅ 强制初始化了移动端UI");
    }

    [MenuItem("Tools/研究室移动端UI/测试仓库点击")]
    public static void TestWarehouseClicks()
    {
        Debug.Log("=== 测试仓库点击功能 ===");

        WarehouseUI warehouseUI = Object.FindFirstObjectByType<WarehouseUI>();
        if (warehouseUI == null)
        {
            Debug.LogError("❌ 找不到WarehouseUI");
            return;
        }

        // 检查按钮是否正确设置
        if (warehouseUI.closeButton != null)
        {
            Debug.Log("✅ 关闭按钮存在");
        }
        else
        {
            Debug.LogError("❌ 关闭按钮缺失");
        }

        if (warehouseUI.multiSelectButton != null)
        {
            Debug.Log("✅ 多选按钮存在");
        }
        else
        {
            Debug.LogError("❌ 多选按钮缺失");
        }

        // 检查面板组件
        if (warehouseUI.inventoryPanel != null)
        {
            Debug.Log("✅ 背包面板存在");
        }
        else
        {
            Debug.LogError("❌ 背包面板缺失");
        }

        if (warehouseUI.storagePanel != null)
        {
            Debug.Log("✅ 仓库面板存在");
        }
        else
        {
            Debug.LogError("❌ 仓库面板缺失");
        }

        Debug.Log("=== 测试完成 ===");
    }

    [MenuItem("Tools/研究室移动端UI/强制显示样本")]
    public static void ForceShowSamples()
    {
        Debug.Log("=== 强制显示样本 ===");

        // 检查样本背包
        SampleInventory inventory = SampleInventory.Instance;
        if (inventory == null)
        {
            Debug.LogError("❌ SampleInventory不存在");
            return;
        }

        var samples = inventory.GetAllSamples();
        Debug.Log($"背包中有 {samples.Count} 个样本");

        // 刷新仓库UI
        WarehouseUI warehouseUI = Object.FindFirstObjectByType<WarehouseUI>();
        if (warehouseUI != null && warehouseUI.inventoryPanel != null)
        {
            // 强制刷新背包面板
            warehouseUI.inventoryPanel.RefreshInventoryDisplay();
            Debug.Log("✅ 已刷新背包显示");
        }

        Debug.Log("=== 完成 ===");
    }
}
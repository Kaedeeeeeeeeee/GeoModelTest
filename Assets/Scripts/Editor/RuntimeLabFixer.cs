using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;

/// <summary>
/// 运行时研究室修复工具 - 只在游戏运行时工作
/// </summary>
public class RuntimeLabFixer
{
    [MenuItem("Tools/研究室移动端UI/🎮 运行时修复工具")]
    public static void RuntimeFixLaboratory()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ 此工具需要在游戏运行时使用！请先点击Play按钮启动游戏。");
            EditorUtility.DisplayDialog("提示", "此工具需要在游戏运行时使用！\n请先点击Play按钮启动游戏，然后再运行此工具。", "确定");
            return;
        }

        Debug.Log("=== 🎮 运行时修复研究室场景 ===");

        // 1. 检查并修复EventSystem
        FixEventSystemRuntime();

        // 2. 检查并修复移动端UI
        FixMobileUIRuntime();

        // 3. 检查并修复样本数据
        FixSampleDataRuntime();

        // 4. 检查并修复仓库系统
        FixWarehouseSystemRuntime();

        Debug.Log("🎉 运行时修复完成！");
    }

    private static void FixEventSystemRuntime()
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

    private static void FixMobileUIRuntime()
    {
        // 确保MobileInputManager存在
        MobileInputManager inputManager = MobileInputManager.Instance;
        if (inputManager == null)
        {
            GameObject inputManagerObj = new GameObject("MobileInputManager");
            inputManager = inputManagerObj.AddComponent<MobileInputManager>();
            Object.DontDestroyOnLoad(inputManagerObj);
            Debug.Log("✅ 创建了MobileInputManager");
        }

        // 启用桌面测试模式
        inputManager.EnableDesktopTestMode(true);
        Debug.Log("✅ 启用了桌面测试模式");

        // 确保MobileControlsUI存在
        MobileControlsUI controlsUI = Object.FindFirstObjectByType<MobileControlsUI>();
        if (controlsUI == null)
        {
            GameObject controlsUIObj = new GameObject("MobileControlsUI");
            controlsUI = controlsUIObj.AddComponent<MobileControlsUI>();
            Debug.Log("✅ 创建了MobileControlsUI");
        }

        // 配置为强制显示
        controlsUI.forceShowOnDesktop = true;
        controlsUI.gameObject.SetActive(true);
        Debug.Log("✅ 移动端UI已配置为强制显示");
    }

    private static void FixSampleDataRuntime()
    {
        // 确保SampleInventory存在
        SampleInventory inventory = SampleInventory.Instance;
        if (inventory == null)
        {
            GameObject inventoryObj = new GameObject("SampleInventory");
            inventory = inventoryObj.AddComponent<SampleInventory>();
            Object.DontDestroyOnLoad(inventoryObj);
            Debug.Log("✅ 创建了SampleInventory");
        }

        // 检查是否有样本数据
        var samples = inventory.GetAllSamples();
        Debug.Log($"背包中有 {samples.Count} 个样本");

        // 如果没有样本，创建一些测试样本
        if (samples.Count == 0)
        {
            CreateRuntimeTestSamples(inventory);
        }
    }

    private static void CreateRuntimeTestSamples(SampleInventory inventory)
    {
        for (int i = 0; i < 3; i++)
        {
            SampleItem testSample = new SampleItem();
            testSample.sampleID = System.Guid.NewGuid().ToString();
            testSample.displayName = $"测试样本 {i + 1}";
            testSample.description = $"这是第 {i + 1} 个测试样本";
            testSample.collectionTime = System.DateTime.Now;

            inventory.TryAddSample(testSample);
        }
        Debug.Log("✅ 创建了3个测试样本");
    }

    private static void FixWarehouseSystemRuntime()
    {
        // 检查WarehouseUI
        WarehouseUI warehouseUI = Object.FindFirstObjectByType<WarehouseUI>();
        if (warehouseUI == null)
        {
            Debug.LogWarning("❌ WarehouseUI不存在，可能需要重新初始化仓库系统");
            return;
        }

        // 刷新仓库显示
        if (warehouseUI.inventoryPanel != null)
        {
            warehouseUI.inventoryPanel.RefreshInventoryDisplay();
            Debug.Log("✅ 刷新了背包面板显示");
        }

        if (warehouseUI.storagePanel != null)
        {
            // 假设storagePanel也有类似的刷新方法
            Debug.Log("✅ 检查了仓库面板");
        }

        Debug.Log("✅ 仓库系统检查完成");
    }

    [MenuItem("Tools/研究室移动端UI/📊 检查运行时状态")]
    public static void CheckRuntimeStatus()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ 请在游戏运行时使用此工具");
            return;
        }

        Debug.Log("=== 📊 运行时状态检查 ===");

        // 检查场景
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        Debug.Log($"当前场景: {sceneName}");

        // 检查EventSystem
        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
        Debug.Log($"EventSystem: {(eventSystem != null ? "✅" : "❌")}");

        // 检查移动端UI组件
        MobileInputManager inputManager = MobileInputManager.Instance;
        Debug.Log($"MobileInputManager: {(inputManager != null ? "✅" : "❌")}");
        if (inputManager != null)
        {
            Debug.Log($"  桌面测试模式: {inputManager.desktopTestMode}");
        }

        MobileControlsUI controlsUI = Object.FindFirstObjectByType<MobileControlsUI>();
        Debug.Log($"MobileControlsUI: {(controlsUI != null ? "✅" : "❌")}");
        if (controlsUI != null)
        {
            Debug.Log($"  激活状态: {controlsUI.gameObject.activeInHierarchy}");
            Debug.Log($"  强制桌面显示: {controlsUI.forceShowOnDesktop}");
        }

        // 检查样本系统
        SampleInventory inventory = SampleInventory.Instance;
        Debug.Log($"SampleInventory: {(inventory != null ? "✅" : "❌")}");
        if (inventory != null)
        {
            var samples = inventory.GetAllSamples();
            Debug.Log($"  背包样本数量: {samples.Count}");
        }

        // 检查仓库系统
        WarehouseUI warehouseUI = Object.FindFirstObjectByType<WarehouseUI>();
        Debug.Log($"WarehouseUI: {(warehouseUI != null ? "✅" : "❌")}");

        Debug.Log("=== 状态检查完成 ===");
    }
}
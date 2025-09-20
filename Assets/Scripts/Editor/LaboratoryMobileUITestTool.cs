using UnityEngine;
using UnityEditor;

/// <summary>
/// 研究室移动端UI测试工具 - 编辑器工具菜单
/// </summary>
public class LaboratoryMobileUITestTool
{
#if UNITY_EDITOR
    [MenuItem("Tools/研究室移动端UI/测试系统初始化")]
    public static void TestLaboratoryMobileUIInitialization()
    {
        Debug.Log("=== 研究室移动端UI系统测试开始 ===");

        // 检查当前场景
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        Debug.Log($"当前场景: {currentScene}");

        // 检查SceneInitializer
        SceneInitializer sceneInitializer = Object.FindFirstObjectByType<SceneInitializer>();
        Debug.Log($"SceneInitializer存在: {sceneInitializer != null}");

        // 检查MobileInputManager
        MobileInputManager inputManager = MobileInputManager.Instance;
        Debug.Log($"MobileInputManager存在: {inputManager != null}");

        // 检查MobileControlsUI
        MobileControlsUI controlsUI = Object.FindFirstObjectByType<MobileControlsUI>();
        Debug.Log($"MobileControlsUI存在: {controlsUI != null}");

        Debug.Log("=== 测试完成 ===");
    }

    [MenuItem("Tools/研究室移动端UI/强制初始化")]
    public static void ForceInitialization()
    {
        Debug.Log("=== 强制初始化研究室移动端UI ===");

        // 使用反射调用静态初始化方法
        System.Type helperType = System.Type.GetType("LaboratoryMobileUIHelper");
        if (helperType != null)
        {
            try
            {
                var method = helperType.GetMethod("InitializeLaboratoryMobileUI",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (method != null)
                {
                    method.Invoke(null, null);
                    Debug.Log("✅ 通过反射调用辅助器初始化成功");
                }
                else
                {
                    Debug.LogWarning("❌ 无法找到InitializeLaboratoryMobileUI方法");
                    FallbackInitialization();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ 调用辅助器失败: {e.Message}");
                FallbackInitialization();
            }
        }
        else
        {
            Debug.LogWarning("❌ 无法找到LaboratoryMobileUIHelper类型，使用备用初始化");
            FallbackInitialization();
        }

        Debug.Log("🎉 强制初始化完成");
    }

    [MenuItem("Tools/研究室移动端UI/清理所有组件")]
    public static void CleanupAllComponents()
    {
        Debug.Log("=== 清理所有移动端UI组件 ===");

        int totalCleaned = 0;

        // 清理MobileControlsUI
        MobileControlsUI[] controlsUIs = Object.FindObjectsOfType<MobileControlsUI>();
        foreach (var controlsUI in controlsUIs)
        {
            Object.DestroyImmediate(controlsUI.gameObject);
            totalCleaned++;
        }
        Debug.Log($"✅ 清理了 {controlsUIs.Length} 个MobileControlsUI组件");

        // 清理其他测试组件
        CleanupComponentsByType("SimpleLaboratoryMobileUIManager", ref totalCleaned);
        CleanupComponentsByType("LaboratoryMobileUIInitializer", ref totalCleaned);

        Debug.Log($"🎉 总共清理了 {totalCleaned} 个组件");
    }

    private static void CleanupComponentsByType(string typeName, ref int totalCount)
    {
        System.Type componentType = System.Type.GetType(typeName);
        if (componentType != null)
        {
            UnityEngine.Object[] objects = Object.FindObjectsOfType(componentType);
            foreach (var obj in objects)
            {
                Component comp = obj as Component;
                if (comp != null)
                {
                    Object.DestroyImmediate(comp.gameObject);
                    totalCount++;
                }
            }
            Debug.Log($"✅ 清理了 {objects.Length} 个{typeName}组件");
        }
    }

    private static void FallbackInitialization()
    {
        Debug.Log("🔧 启动备用初始化方案");

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

        Debug.Log("✅ 备用初始化完成");
    }
#endif
}
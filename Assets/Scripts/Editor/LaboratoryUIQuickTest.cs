using UnityEngine;
using UnityEditor;

/// <summary>
/// 研究室UI快速测试工具 - 简化版本，确保菜单可见
/// </summary>
public class LaboratoryUIQuickTest
{
#if UNITY_EDITOR
    [MenuItem("Tools/研究室移动端UI/✅ 快速启用桌面测试")]
    public static void QuickEnableDesktopMode()
    {
        Debug.Log("=== 快速启用桌面测试模式 ===");

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

        // 检查或创建MobileControlsUI
        MobileControlsUI controlsUI = Object.FindFirstObjectByType<MobileControlsUI>();
        if (controlsUI == null)
        {
            GameObject controlsUIObj = new GameObject("MobileControlsUI");
            controlsUI = controlsUIObj.AddComponent<MobileControlsUI>();
            Debug.Log("✅ 创建MobileControlsUI");
        }

        // 强制显示控制UI
        controlsUI.forceShowOnDesktop = true;
        controlsUI.gameObject.SetActive(true);

        // 等待UI初始化后配置研究室特定设置
        EditorApplication.delayCall += () => {
            if (controlsUI != null)
            {
                controlsUI.SetDroneControlsVisible(false);
                Debug.Log("✅ 已隐藏无人机控制（研究室配置）");
            }
        };

        Debug.Log("🎉 桌面测试模式启用完成！移动端UI应该已显示");
    }

    [MenuItem("Tools/研究室移动端UI/❌ 禁用桌面测试")]
    public static void QuickDisableDesktopMode()
    {
        Debug.Log("=== 禁用桌面测试模式 ===");

        MobileInputManager inputManager = MobileInputManager.Instance;
        if (inputManager != null)
        {
            inputManager.EnableDesktopTestMode(false);
            Debug.Log("✅ 禁用桌面测试模式");
        }

        MobileControlsUI controlsUI = Object.FindFirstObjectByType<MobileControlsUI>();
        if (controlsUI != null)
        {
            controlsUI.forceShowOnDesktop = false;
            Debug.Log("✅ 隐藏移动端UI");
        }

        Debug.Log("✅ 桌面测试模式已禁用");
    }

    [MenuItem("Tools/研究室移动端UI/📊 检查系统状态")]
    public static void QuickCheckStatus()
    {
        Debug.Log("=== 系统状态检查 ===");

        Debug.Log($"当前场景: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        Debug.Log($"平台: {Application.platform}");
        Debug.Log($"是否移动平台: {Application.isMobilePlatform}");
        Debug.Log($"触摸支持: {(UnityEngine.InputSystem.Touchscreen.current != null)}");

        MobileInputManager inputManager = MobileInputManager.Instance;
        Debug.Log($"MobileInputManager存在: {inputManager != null}");
        if (inputManager != null)
        {
            Debug.Log($"  - 桌面测试模式: {inputManager.desktopTestMode}");
            Debug.Log($"  - 应显示虚拟控制: {inputManager.ShouldShowVirtualControls()}");
        }

        MobileControlsUI controlsUI = Object.FindFirstObjectByType<MobileControlsUI>();
        Debug.Log($"MobileControlsUI存在: {controlsUI != null}");
        if (controlsUI != null)
        {
            Debug.Log($"  - 激活状态: {controlsUI.gameObject.activeInHierarchy}");
            Debug.Log($"  - 强制桌面显示: {controlsUI.forceShowOnDesktop}");
        }

        Debug.Log("=== 状态检查完成 ===");
    }

    [MenuItem("Tools/研究室移动端UI/🧹 清理所有组件")]
    public static void QuickCleanup()
    {
        Debug.Log("=== 清理移动端UI组件 ===");

        // 清理MobileControlsUI
        MobileControlsUI[] controlsUIs = Object.FindObjectsOfType<MobileControlsUI>();
        foreach (var controlsUI in controlsUIs)
        {
            Object.DestroyImmediate(controlsUI.gameObject);
        }
        Debug.Log($"✅ 清理了 {controlsUIs.Length} 个MobileControlsUI组件");

        // 清理其他测试组件
        int managerCount = 0;
        System.Type managerType = System.Type.GetType("SimpleLaboratoryMobileUIManager");
        if (managerType != null)
        {
            UnityEngine.Object[] managers = Object.FindObjectsOfType(managerType);
            foreach (var manager in managers)
            {
                Component managerComponent = manager as Component;
                if (managerComponent != null)
                {
                    Object.DestroyImmediate(managerComponent.gameObject);
                    managerCount++;
                }
            }
        }
        Debug.Log($"✅ 清理了 {managerCount} 个SimpleLaboratoryMobileUIManager组件");

        Debug.Log("🎉 清理完成");
    }

    [MenuItem("Tools/研究室移动端UI/🚀 一键初始化研究室UI")]
    public static void QuickInitializeLaboratory()
    {
        Debug.Log("=== 一键初始化研究室移动端UI ===");

        // 先清理
        QuickCleanup();

        // 等待一帧后初始化
        EditorApplication.delayCall += () => {
            // 启用桌面测试
            QuickEnableDesktopMode();

            Debug.Log("🎉 研究室移动端UI初始化完成！");
            Debug.Log("💡 提示：你应该能看到虚拟摇杆和按钮了");
        };
    }
#endif
}
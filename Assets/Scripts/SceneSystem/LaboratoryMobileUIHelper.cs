using UnityEngine;
using System.Collections;

/// <summary>
/// 研究室移动端UI辅助器 - 使用最简单的方法，避免所有编译时依赖
/// </summary>
public static class LaboratoryMobileUIHelper
{
    /// <summary>
    /// 为研究室场景初始化移动端UI（静态方法，无类型依赖）
    /// </summary>
    public static void InitializeLaboratoryMobileUI()
    {
        Debug.Log("[LaboratoryMobileUIHelper] 开始初始化研究室移动端UI");

        // 检查是否需要移动端UI
        if (!ShouldCreateMobileUI())
        {
            Debug.Log("[LaboratoryMobileUIHelper] 当前环境不需要移动端UI");
            return;
        }

        // 确保MobileInputManager存在
        EnsureMobileInputManager();

        // 确保MobileControlsUI存在
        EnsureMobileControlsUI();

        Debug.Log("[LaboratoryMobileUIHelper] 研究室移动端UI初始化完成");
    }

    /// <summary>
    /// 判断是否需要创建移动端UI
    /// </summary>
    static bool ShouldCreateMobileUI()
    {
        // 检查MobileInputManager是否存在且启用了桌面测试模式
        MobileInputManager inputManager = MobileInputManager.Instance;
        if (inputManager != null && inputManager.desktopTestMode)
        {
            Debug.Log("[LaboratoryMobileUIHelper] 桌面测试模式启用");
            return true;
        }

        // 检查是否为移动设备或支持触摸
        bool isMobile = Application.isMobilePlatform;
        bool hasTouch = UnityEngine.InputSystem.Touchscreen.current != null;

        Debug.Log($"[LaboratoryMobileUIHelper] 设备检测: 移动设备={isMobile}, 触摸支持={hasTouch}");
        return isMobile || hasTouch;
    }

    /// <summary>
    /// 确保移动端输入管理器存在
    /// </summary>
    static void EnsureMobileInputManager()
    {
        MobileInputManager inputManager = MobileInputManager.Instance;

        if (inputManager == null)
        {
            inputManager = Object.FindFirstObjectByType<MobileInputManager>();
        }

        if (inputManager == null)
        {
            Debug.Log("[LaboratoryMobileUIHelper] 创建MobileInputManager");
            GameObject inputManagerObj = new GameObject("MobileInputManager");
            inputManager = inputManagerObj.AddComponent<MobileInputManager>();

            // 配置参数
            inputManager.enableTouchInput = true;
            inputManager.enableVirtualControls = true;
            inputManager.enableDebugLog = false;

            Object.DontDestroyOnLoad(inputManagerObj);
        }
        else
        {
            Debug.Log("[LaboratoryMobileUIHelper] MobileInputManager已存在");
        }
    }

    /// <summary>
    /// 确保移动端控制UI存在
    /// </summary>
    static void EnsureMobileControlsUI()
    {
        MobileControlsUI controlsUI = Object.FindFirstObjectByType<MobileControlsUI>();

        if (controlsUI == null)
        {
            Debug.Log("[LaboratoryMobileUIHelper] 创建MobileControlsUI");
            GameObject controlsUIObj = new GameObject("MobileControlsUI");
            controlsUI = controlsUIObj.AddComponent<MobileControlsUI>();

            // 配置参数
            bool shouldForceShow = ShouldForceShowMobileUI();
            controlsUI.forceShowOnDesktop = shouldForceShow;
            controlsUI.enableDebugVisualization = false;
            controlsUI.autoHideOnDesktop = !shouldForceShow;
        }
        else
        {
            Debug.Log("[LaboratoryMobileUIHelper] MobileControlsUI已存在");
        }

        // 等待UI初始化，然后配置研究室特定设置
        ConfigureLaboratorySpecificUI(controlsUI);
    }

    /// <summary>
    /// 判断是否应该强制显示移动端UI
    /// </summary>
    static bool ShouldForceShowMobileUI()
    {
        // 检查MobileInputManager是否存在且启用了桌面测试模式
        MobileInputManager inputManager = MobileInputManager.Instance;
        if (inputManager != null && inputManager.desktopTestMode)
        {
            return true;
        }

        // 检查是否为移动设备或支持触摸
        bool isMobile = Application.isMobilePlatform;
        bool hasTouch = UnityEngine.InputSystem.Touchscreen.current != null;

        return isMobile || hasTouch;
    }

    /// <summary>
    /// 配置研究室特定的UI设置
    /// </summary>
    static void ConfigureLaboratorySpecificUI(MobileControlsUI controlsUI)
    {
        if (controlsUI == null) return;

        // 使用协程延迟配置，确保UI完全初始化
        MonoBehaviour runner = Object.FindFirstObjectByType<MonoBehaviour>();
        if (runner != null)
        {
            runner.StartCoroutine(DelayedLaboratoryConfiguration(controlsUI));
        }
    }

    /// <summary>
    /// 延迟配置研究室UI设置
    /// </summary>
    static IEnumerator DelayedLaboratoryConfiguration(MobileControlsUI controlsUI)
    {
        // 等待UI完全初始化
        yield return new WaitForSeconds(1.0f);

        if (controlsUI != null)
        {
            // 研究室特定配置：隐藏无人机控制
            controlsUI.SetDroneControlsVisible(false);
            Debug.Log("[LaboratoryMobileUIHelper] 已隐藏无人机控制（研究室场景）");
        }
    }

    /// <summary>
    /// 启用桌面测试模式（公共接口）
    /// </summary>
    public static void EnableDesktopTestMode()
    {
        Debug.Log("[LaboratoryMobileUIHelper] 启用桌面测试模式");

        // 确保输入管理器存在并启用测试模式
        MobileInputManager inputManager = MobileInputManager.Instance;
        if (inputManager == null)
        {
            EnsureMobileInputManager();
            inputManager = MobileInputManager.Instance;
        }

        if (inputManager != null)
        {
            inputManager.EnableDesktopTestMode(true);
        }

        // 确保控制UI存在并强制显示
        MobileControlsUI controlsUI = Object.FindFirstObjectByType<MobileControlsUI>();
        if (controlsUI == null)
        {
            EnsureMobileControlsUI();
            controlsUI = Object.FindFirstObjectByType<MobileControlsUI>();
        }

        if (controlsUI != null)
        {
            controlsUI.forceShowOnDesktop = true;
            controlsUI.gameObject.SetActive(true);
        }

        Debug.Log("[LaboratoryMobileUIHelper] 桌面测试模式已启用");
    }

    /// <summary>
    /// 禁用桌面测试模式
    /// </summary>
    public static void DisableDesktopTestMode()
    {
        Debug.Log("[LaboratoryMobileUIHelper] 禁用桌面测试模式");

        MobileInputManager inputManager = MobileInputManager.Instance;
        if (inputManager != null)
        {
            inputManager.EnableDesktopTestMode(false);
        }

        MobileControlsUI controlsUI = Object.FindFirstObjectByType<MobileControlsUI>();
        if (controlsUI != null)
        {
            controlsUI.forceShowOnDesktop = false;
        }

        Debug.Log("[LaboratoryMobileUIHelper] 桌面测试模式已禁用");
    }

    /// <summary>
    /// 检查系统状态
    /// </summary>
    public static void CheckSystemStatus()
    {
        Debug.Log("=== 研究室移动端UI系统状态 ===");
        Debug.Log($"当前场景: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");

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
}
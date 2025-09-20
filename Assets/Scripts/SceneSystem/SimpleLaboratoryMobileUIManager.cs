using UnityEngine;
using System.Collections;

/// <summary>
/// 简化的研究室移动端UI管理器
/// 避免编译时类型依赖问题，使用更直接的方法
/// </summary>
public class SimpleLaboratoryMobileUIManager : MonoBehaviour
{
    [Header("移动端UI配置")]
    public bool enableMobileUI = true;
    public bool forceShowOnDesktop = false;
    public bool enableDebugVisualization = false;

    void Start()
    {
        if (enableMobileUI)
        {
            StartCoroutine(InitializeMobileUI());
        }
    }

    /// <summary>
    /// 初始化移动端UI系统
    /// </summary>
    IEnumerator InitializeMobileUI()
    {
        Debug.Log("[SimpleLaboratoryMobileUIManager] 开始初始化研究室移动端UI");

        // 等待场景加载完成
        yield return new WaitForSeconds(0.5f);

        // 检查是否需要移动端UI
        if (!ShouldCreateMobileUI())
        {
            Debug.Log("[SimpleLaboratoryMobileUIManager] 当前环境不需要移动端UI");
            yield break;
        }

        // 初始化输入管理器
        yield return StartCoroutine(EnsureMobileInputManager());

        // 初始化控制UI
        yield return StartCoroutine(EnsureMobileControlsUI());

        Debug.Log("[SimpleLaboratoryMobileUIManager] 研究室移动端UI初始化完成");
    }

    /// <summary>
    /// 判断是否需要创建移动端UI
    /// </summary>
    bool ShouldCreateMobileUI()
    {
        if (forceShowOnDesktop)
        {
            Debug.Log("[SimpleLaboratoryMobileUIManager] 强制显示模式启用");
            return true;
        }

        bool isMobile = Application.isMobilePlatform;
        bool hasTouch = UnityEngine.InputSystem.Touchscreen.current != null;

        Debug.Log($"[SimpleLaboratoryMobileUIManager] 设备检测: 移动设备={isMobile}, 触摸支持={hasTouch}");
        return isMobile || hasTouch;
    }

    /// <summary>
    /// 确保移动端输入管理器存在
    /// </summary>
    IEnumerator EnsureMobileInputManager()
    {
        MobileInputManager inputManager = MobileInputManager.Instance;

        if (inputManager == null)
        {
            inputManager = FindFirstObjectByType<MobileInputManager>();
        }

        if (inputManager == null)
        {
            Debug.Log("[SimpleLaboratoryMobileUIManager] 创建MobileInputManager");
            GameObject inputManagerObj = new GameObject("MobileInputManager");
            inputManager = inputManagerObj.AddComponent<MobileInputManager>();

            // 配置参数
            inputManager.enableTouchInput = true;
            inputManager.enableVirtualControls = true;
            inputManager.enableDebugLog = enableDebugVisualization;

            if (forceShowOnDesktop)
            {
                inputManager.EnableDesktopTestMode(true);
            }

            DontDestroyOnLoad(inputManagerObj);
        }
        else
        {
            Debug.Log("[SimpleLaboratoryMobileUIManager] MobileInputManager已存在");

            // 更新配置
            if (forceShowOnDesktop)
            {
                inputManager.EnableDesktopTestMode(true);
            }
        }

        yield return null;
    }

    /// <summary>
    /// 确保移动端控制UI存在
    /// </summary>
    IEnumerator EnsureMobileControlsUI()
    {
        MobileControlsUI controlsUI = FindFirstObjectByType<MobileControlsUI>();

        if (controlsUI == null)
        {
            Debug.Log("[SimpleLaboratoryMobileUIManager] 创建MobileControlsUI");
            GameObject controlsUIObj = new GameObject("MobileControlsUI");
            controlsUI = controlsUIObj.AddComponent<MobileControlsUI>();

            // 配置参数
            controlsUI.forceShowOnDesktop = forceShowOnDesktop;
            controlsUI.enableDebugVisualization = enableDebugVisualization;
            controlsUI.autoHideOnDesktop = !forceShowOnDesktop;
        }
        else
        {
            Debug.Log("[SimpleLaboratoryMobileUIManager] MobileControlsUI已存在");

            // 更新配置
            controlsUI.forceShowOnDesktop = forceShowOnDesktop;
            controlsUI.enableDebugVisualization = enableDebugVisualization;
        }

        // 等待UI初始化完成
        yield return new WaitForSeconds(0.8f);

        // 研究室特定配置：隐藏无人机控制
        if (controlsUI != null)
        {
            controlsUI.SetDroneControlsVisible(false);
            Debug.Log("[SimpleLaboratoryMobileUIManager] 已隐藏无人机控制（研究室场景）");
        }

        yield return null;
    }

    /// <summary>
    /// 手动启用桌面测试模式
    /// </summary>
    [ContextMenu("启用桌面测试模式")]
    public void EnableDesktopTestMode()
    {
        forceShowOnDesktop = true;
        enableDebugVisualization = true;

        if (MobileInputManager.Instance != null)
        {
            MobileInputManager.Instance.EnableDesktopTestMode(true);
        }

        MobileControlsUI controlsUI = FindFirstObjectByType<MobileControlsUI>();
        if (controlsUI != null)
        {
            controlsUI.forceShowOnDesktop = true;
            controlsUI.gameObject.SetActive(true);
        }

        Debug.Log("[SimpleLaboratoryMobileUIManager] 桌面测试模式已启用");
    }

    /// <summary>
    /// 禁用桌面测试模式
    /// </summary>
    [ContextMenu("禁用桌面测试模式")]
    public void DisableDesktopTestMode()
    {
        forceShowOnDesktop = false;
        enableDebugVisualization = false;

        if (MobileInputManager.Instance != null)
        {
            MobileInputManager.Instance.EnableDesktopTestMode(false);
        }

        MobileControlsUI controlsUI = FindFirstObjectByType<MobileControlsUI>();
        if (controlsUI != null)
        {
            controlsUI.forceShowOnDesktop = false;
        }

        Debug.Log("[SimpleLaboratoryMobileUIManager] 桌面测试模式已禁用");
    }

    /// <summary>
    /// 获取移动端UI系统状态
    /// </summary>
    [ContextMenu("检查系统状态")]
    public void CheckSystemStatus()
    {
        Debug.Log("=== 移动端UI系统状态 ===");
        Debug.Log($"当前场景: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        Debug.Log($"启用移动端UI: {enableMobileUI}");
        Debug.Log($"强制桌面显示: {forceShowOnDesktop}");

        MobileInputManager inputManager = MobileInputManager.Instance;
        Debug.Log($"MobileInputManager存在: {inputManager != null}");
        if (inputManager != null)
        {
            Debug.Log($"  - 桌面测试模式: {inputManager.desktopTestMode}");
            Debug.Log($"  - 应显示虚拟控制: {inputManager.ShouldShowVirtualControls()}");
        }

        MobileControlsUI controlsUI = FindFirstObjectByType<MobileControlsUI>();
        Debug.Log($"MobileControlsUI存在: {controlsUI != null}");
        if (controlsUI != null)
        {
            Debug.Log($"  - 激活状态: {controlsUI.gameObject.activeInHierarchy}");
            Debug.Log($"  - 强制桌面显示: {controlsUI.forceShowOnDesktop}");
        }

        Debug.Log("=== 状态检查完成 ===");
    }
}
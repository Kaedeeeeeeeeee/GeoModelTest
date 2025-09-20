using UnityEngine;
using System.Collections;

/// <summary>
/// 研究室场景移动端UI初始化器
/// 确保研究室场景拥有完整的移动端UI系统
/// </summary>
public class LaboratoryMobileUIInitializer : MonoBehaviour
{
    [Header("移动端UI配置")]
    public bool enableMobileUI = true;
    public bool forceShowOnDesktop = false; // 桌面测试模式
    public bool enableDebugVisualization = false;

    [Header("UI组件预制体")]
    public GameObject mobileUICanvasPrefab; // 可选：使用预制体

    // 移动端UI系统引用
    private MobileInputManager mobileInputManager;
    private MobileControlsUI mobileControlsUI;
    private GameObject mobileUICanvas;

    void Start()
    {
        StartCoroutine(InitializeMobileUISystem());
    }

    /// <summary>
    /// 初始化移动端UI系统
    /// </summary>
    IEnumerator InitializeMobileUISystem()
    {
        Debug.Log("[LaboratoryMobileUIInitializer] 开始初始化研究室移动端UI系统");

        // 等待场景完全加载
        yield return new WaitForSeconds(0.5f);

        // 检查是否需要移动端UI
        if (!ShouldCreateMobileUI())
        {
            Debug.Log("[LaboratoryMobileUIInitializer] 当前环境不需要移动端UI，跳过初始化");
            yield break;
        }

        // 检查现有的移动端输入管理器
        EnsureMobileInputManager();

        // 创建移动端控制UI
        CreateMobileControlsUI();

        // 等待组件初始化完成
        yield return new WaitForSeconds(0.2f);

        // 验证系统初始化
        ValidateInitialization();

        Debug.Log("[LaboratoryMobileUIInitializer] 研究室移动端UI系统初始化完成");
    }

    /// <summary>
    /// 判断是否应该创建移动端UI
    /// </summary>
    bool ShouldCreateMobileUI()
    {
        if (!enableMobileUI)
        {
            Debug.Log("[LaboratoryMobileUIInitializer] 移动端UI已禁用");
            return false;
        }

        // 强制显示模式（桌面测试）
        if (forceShowOnDesktop)
        {
            Debug.Log("[LaboratoryMobileUIInitializer] 强制显示模式启用");
            return true;
        }

        // 检查是否为移动设备或支持触摸
        bool isMobile = Application.isMobilePlatform;
        bool hasTouch = UnityEngine.InputSystem.Touchscreen.current != null;

        Debug.Log($"[LaboratoryMobileUIInitializer] 设备检测: 移动设备={isMobile}, 触摸支持={hasTouch}");

        return isMobile || hasTouch;
    }

    /// <summary>
    /// 确保移动端输入管理器存在
    /// </summary>
    void EnsureMobileInputManager()
    {
        // 检查是否已存在全局实例
        mobileInputManager = MobileInputManager.Instance;

        if (mobileInputManager != null)
        {
            Debug.Log("[LaboratoryMobileUIInitializer] 找到现有的MobileInputManager实例");

            // 启用桌面测试模式（如果需要）
            if (forceShowOnDesktop)
            {
                mobileInputManager.EnableDesktopTestMode(true);
            }

            return;
        }

        // 在场景中查找
        mobileInputManager = FindFirstObjectByType<MobileInputManager>();

        if (mobileInputManager != null)
        {
            Debug.Log("[LaboratoryMobileUIInitializer] 在场景中找到MobileInputManager");
            return;
        }

        // 创建新的移动端输入管理器
        CreateMobileInputManager();
    }

    /// <summary>
    /// 创建移动端输入管理器
    /// </summary>
    void CreateMobileInputManager()
    {
        Debug.Log("[LaboratoryMobileUIInitializer] 创建新的MobileInputManager");

        GameObject inputManagerObj = new GameObject("MobileInputManager");
        mobileInputManager = inputManagerObj.AddComponent<MobileInputManager>();

        // 配置输入管理器
        mobileInputManager.enableTouchInput = true;
        mobileInputManager.enableVirtualControls = true;
        mobileInputManager.enableDebugLog = enableDebugVisualization;

        // 启用桌面测试模式（如果需要）
        if (forceShowOnDesktop)
        {
            mobileInputManager.desktopTestMode = true;
            mobileInputManager.EnableDesktopTestMode(true);
        }

        Debug.Log("[LaboratoryMobileUIInitializer] MobileInputManager创建完成");
    }

    /// <summary>
    /// 创建移动端控制UI
    /// </summary>
    void CreateMobileControlsUI()
    {
        // 检查是否已存在移动端控制UI
        mobileControlsUI = FindFirstObjectByType<MobileControlsUI>();

        if (mobileControlsUI != null)
        {
            Debug.Log("[LaboratoryMobileUIInitializer] 找到现有的MobileControlsUI，跳过创建");
            ConfigureExistingMobileUI();
            return;
        }

        // 使用预制体（如果有）
        if (mobileUICanvasPrefab != null)
        {
            Debug.Log("[LaboratoryMobileUIInitializer] 使用预制体创建移动端UI");
            mobileUICanvas = Instantiate(mobileUICanvasPrefab);
            mobileControlsUI = mobileUICanvas.GetComponent<MobileControlsUI>();
        }
        else
        {
            // 动态创建移动端UI
            CreateMobileUIFromScratch();
        }

        // 配置移动端UI
        ConfigureMobileUI();
    }

    /// <summary>
    /// 从零创建移动端UI
    /// </summary>
    void CreateMobileUIFromScratch()
    {
        Debug.Log("[LaboratoryMobileUIInitializer] 动态创建移动端UI");

        // 创建移动端UI Canvas
        mobileUICanvas = new GameObject("MobileControlsCanvas");
        mobileControlsUI = mobileUICanvas.AddComponent<MobileControlsUI>();

        // 确保Canvas独立于场景层级
        mobileUICanvas.transform.SetParent(null);
        DontDestroyOnLoad(mobileUICanvas);

        Debug.Log("[LaboratoryMobileUIInitializer] 移动端UI Canvas创建完成");
    }

    /// <summary>
    /// 配置现有的移动端UI
    /// </summary>
    void ConfigureExistingMobileUI()
    {
        if (mobileControlsUI == null) return;

        // 确保UI在研究室场景中正确显示
        mobileControlsUI.gameObject.SetActive(true);

        // 更新显示设置
        if (forceShowOnDesktop)
        {
            mobileControlsUI.forceShowOnDesktop = true;
        }

        mobileControlsUI.enableDebugVisualization = enableDebugVisualization;

        Debug.Log("[LaboratoryMobileUIInitializer] 现有移动端UI配置完成");
    }

    /// <summary>
    /// 配置移动端UI
    /// </summary>
    void ConfigureMobileUI()
    {
        if (mobileControlsUI == null) return;

        // 配置移动端控制UI参数
        mobileControlsUI.forceShowOnDesktop = forceShowOnDesktop;
        mobileControlsUI.enableDebugVisualization = enableDebugVisualization;
        mobileControlsUI.autoHideOnDesktop = !forceShowOnDesktop;

        // 研究室场景特定配置
        ConfigureLaboratorySpecificUI();

        Debug.Log("[LaboratoryMobileUIInitializer] 移动端UI配置完成");
    }

    /// <summary>
    /// 研究室场景特定UI配置
    /// </summary>
    void ConfigureLaboratorySpecificUI()
    {
        // 在研究室场景中，可能需要隐藏某些户外专用的控制按钮
        // 比如无人机控制按钮在研究室内通常不需要

        if (mobileControlsUI != null)
        {
            // 隐藏无人机控制（研究室内通常不需要）
            mobileControlsUI.SetDroneControlsVisible(false);

            // 其他研究室特定配置...
            Debug.Log("[LaboratoryMobileUIInitializer] 研究室场景UI特定配置完成");
        }
    }

    /// <summary>
    /// 验证初始化结果
    /// </summary>
    void ValidateInitialization()
    {
        bool inputManagerValid = mobileInputManager != null;
        bool controlsUIValid = mobileControlsUI != null;

        Debug.Log($"[LaboratoryMobileUIInitializer] 系统验证:");
        Debug.Log($"  - MobileInputManager: {(inputManagerValid ? "✓" : "✗")}");
        Debug.Log($"  - MobileControlsUI: {(controlsUIValid ? "✓" : "✗")}");

        if (inputManagerValid && controlsUIValid)
        {
            Debug.Log("[LaboratoryMobileUIInitializer] ✅ 研究室移动端UI系统初始化成功");

            // 测试连接
            TestUIConnection();
        }
        else
        {
            Debug.LogError("[LaboratoryMobileUIInitializer] ❌ 研究室移动端UI系统初始化失败");
        }
    }

    /// <summary>
    /// 测试UI连接
    /// </summary>
    void TestUIConnection()
    {
        if (mobileInputManager != null && mobileControlsUI != null)
        {
            // 测试输入管理器是否正确连接到控制UI
            bool connectionValid = mobileInputManager.ShouldShowVirtualControls();
            Debug.Log($"[LaboratoryMobileUIInitializer] UI连接测试: {(connectionValid ? "正常" : "异常")}");
        }
    }

    /// <summary>
    /// 获取移动端输入管理器引用
    /// </summary>
    public MobileInputManager GetMobileInputManager()
    {
        return mobileInputManager;
    }

    /// <summary>
    /// 获取移动端控制UI引用
    /// </summary>
    public MobileControlsUI GetMobileControlsUI()
    {
        return mobileControlsUI;
    }

    /// <summary>
    /// 切换移动端UI显示状态
    /// </summary>
    public void ToggleMobileUI(bool show)
    {
        if (mobileControlsUI != null)
        {
            mobileControlsUI.SetVirtualControlsVisible(show);
            Debug.Log($"[LaboratoryMobileUIInitializer] 移动端UI {(show ? "显示" : "隐藏")}");
        }
    }

    /// <summary>
    /// 清理资源
    /// </summary>
    void OnDestroy()
    {
        Debug.Log("[LaboratoryMobileUIInitializer] 清理研究室移动端UI初始化器");
    }
}
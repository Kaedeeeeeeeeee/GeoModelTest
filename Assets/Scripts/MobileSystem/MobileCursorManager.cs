using UnityEngine;

/// <summary>
/// 移动端光标管理器
/// 确保桌面测试模式下鼠标状态不会被其他系统意外覆盖
/// </summary>
public class MobileCursorManager : MonoBehaviour
{
    public static MobileCursorManager Instance { get; private set; }
    
    private MobileInputManager inputManager;
    private bool lastDesktopTestMode = false;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[MobileCursorManager] 初始化完成");
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        inputManager = MobileInputManager.Instance;
        if (inputManager == null)
        {
            Debug.LogWarning("[MobileCursorManager] 未找到MobileInputManager");
        }
    }
    
    void Update()
    {
        if (inputManager == null) return;
        
        bool currentDesktopTestMode = inputManager.desktopTestMode;
        
        // 临时禁用强制恢复鼠标状态的逻辑，让FirstPersonController控制鼠标
        // if (currentDesktopTestMode)
        // {
        //     if (Cursor.lockState != CursorLockMode.None)
        //     {
        //         Cursor.lockState = CursorLockMode.None;
        //         Cursor.visible = true;
        //         Debug.Log("[MobileCursorManager] 强制恢复桌面测试模式鼠标状态");
        //     }
        // }
        
        // 检测模式切换
        if (currentDesktopTestMode != lastDesktopTestMode)
        {
            if (currentDesktopTestMode)
            {
                EnableDesktopTestCursor();
            }
            else
            {
                DisableDesktopTestCursor();
            }
            lastDesktopTestMode = currentDesktopTestMode;
        }
    }
    
    /// <summary>
    /// 启用桌面测试模式光标
    /// </summary>
    void EnableDesktopTestCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Debug.Log("[MobileCursorManager] 桌面测试模式已启用 - 鼠标解锁");
    }
    
    /// <summary>
    /// 禁用桌面测试模式光标
    /// </summary>
    void DisableDesktopTestCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Debug.Log("[MobileCursorManager] 桌面测试模式已禁用 - 鼠标锁定");
    }
    
    /// <summary>
    /// 公开方法：强制设置桌面测试模式鼠标状态
    /// </summary>
    public static void ForceDesktopTestCursor()
    {
        if (Instance != null && Instance.inputManager != null && Instance.inputManager.desktopTestMode)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Debug.Log("[MobileCursorManager] 强制设置桌面测试模式鼠标状态");
        }
    }
    
    /// <summary>
    /// 检查当前是否为桌面测试模式
    /// </summary>
    public static bool IsDesktopTestMode()
    {
        if (Instance != null && Instance.inputManager != null)
        {
            return Instance.inputManager.desktopTestMode;
        }
        return false;
    }
}
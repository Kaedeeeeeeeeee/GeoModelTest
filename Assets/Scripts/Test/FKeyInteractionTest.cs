using UnityEngine;

/// <summary>
/// F键交互系统测试脚本
/// 验证移动端和桌面端F键交互是否正常工作
/// </summary>
public class FKeyInteractionTest : MonoBehaviour
{
    [Header("测试设置")]
    public bool enableDebugOutput = true;
    public float testInterval = 1f; // 测试间隔

    private MobileInputManager inputManager;
    private float lastTestTime = 0f;

    void Start()
    {
        // 获取输入管理器
        inputManager = MobileInputManager.Instance;
        if (inputManager == null)
        {
            inputManager = FindObjectOfType<MobileInputManager>();
        }

        if (inputManager != null)
        {
            // 订阅F键事件
            inputManager.OnSecondaryInteractInput += OnFKeyInput;
            Debug.Log("[FKeyInteractionTest] 已订阅F键交互事件");
        }
        else
        {
            Debug.LogError("[FKeyInteractionTest] 未找到MobileInputManager!");
        }
    }

    void OnDestroy()
    {
        // 取消订阅避免内存泄漏
        if (inputManager != null)
        {
            inputManager.OnSecondaryInteractInput -= OnFKeyInput;
        }
    }

    private void OnFKeyInput(bool isPressed)
    {
        if (enableDebugOutput)
        {
            string device = Application.isMobilePlatform ? "移动端" : "桌面端";
            Debug.Log($"[FKeyInteractionTest] {device} F键交互: {(isPressed ? "按下" : "释放")}");
        }
    }

    void Update()
    {
        // 每秒显示一次当前状态
        if (enableDebugOutput && Time.time - lastTestTime >= testInterval)
        {
            if (inputManager != null)
            {
                Debug.Log($"[FKeyInteractionTest] 当前F键状态: {inputManager.IsSecondaryInteracting}");
            }
            lastTestTime = Time.time;
        }
    }

    // 在Editor中显示状态信息
    void OnGUI()
    {
        if (!enableDebugOutput) return;

        GUILayout.BeginArea(new Rect(10, 300, 400, 150));
        GUILayout.Label("=== F键交互测试 ===");

        if (inputManager != null)
        {
            GUILayout.Label($"输入管理器状态: 正常");
            GUILayout.Label($"F键状态: {(inputManager.IsSecondaryInteracting ? "按下" : "释放")}");
            GUILayout.Label($"设备类型: {(inputManager.IsMobileDevice() ? "移动设备" : "桌面设备")}");
            GUILayout.Label($"虚拟控件显示: {(inputManager.ShouldShowVirtualControls() ? "是" : "否")}");
        }
        else
        {
            GUILayout.Label("输入管理器状态: 未找到");
        }

        GUILayout.EndArea();
    }
}
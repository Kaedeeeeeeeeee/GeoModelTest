using UnityEngine;

/// <summary>
/// 测试E/F按钮交互功能的脚本
/// 在Console中显示按键状态以验证输入系统工作正常
/// </summary>
public class InteractionButtonTest : MonoBehaviour
{
    private MobileInputManager inputManager;

    void Start()
    {
        // 获取输入管理器
        inputManager = MobileInputManager.Instance;
        if (inputManager != null)
        {
            // 订阅E键事件
            inputManager.OnInteractInput += OnEKeyInteract;
            // 订阅F键事件
            inputManager.OnSecondaryInteractInput += OnFKeyInteract;

            Debug.Log("[InteractionButtonTest] 已订阅E/F键交互事件");
        }
        else
        {
            Debug.LogError("[InteractionButtonTest] 未找到MobileInputManager!");
        }
    }

    void OnDestroy()
    {
        // 取消订阅避免内存泄漏
        if (inputManager != null)
        {
            inputManager.OnInteractInput -= OnEKeyInteract;
            inputManager.OnSecondaryInteractInput -= OnFKeyInteract;
        }
    }

    private void OnEKeyInteract(bool isPressed)
    {
        Debug.Log($"[InteractionButtonTest] E键交互: {(isPressed ? "按下" : "释放")}");
    }

    private void OnFKeyInteract(bool isPressed)
    {
        Debug.Log($"[InteractionButtonTest] F键交互: {(isPressed ? "按下" : "释放")}");
    }

    void Update()
    {
        // 显示实时状态
        if (inputManager != null && Time.frameCount % 60 == 0) // 每秒更新一次
        {
            Debug.Log($"[InteractionButtonTest] 当前状态 - E键: {inputManager.IsInteracting}, F键: {inputManager.IsSecondaryInteracting}");
        }
    }
}
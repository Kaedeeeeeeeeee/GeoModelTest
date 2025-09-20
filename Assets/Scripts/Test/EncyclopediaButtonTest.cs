using UnityEngine;
using Encyclopedia;

/// <summary>
/// 图鉴按钮功能测试脚本
/// 验证移动端图鉴按钮是否正常工作
/// </summary>
public class EncyclopediaButtonTest : MonoBehaviour
{
    [Header("调试设置")]
    public bool enableDebugOutput = true;
    public float testInterval = 2f;

    private MobileInputManager inputManager;
    private SimpleEncyclopediaManager encyclopediaManager;
    private float lastTestTime;

    void Start()
    {
        // 获取输入管理器
        inputManager = MobileInputManager.Instance;
        if (inputManager == null)
        {
            inputManager = FindObjectOfType<MobileInputManager>();
        }

        // 获取图鉴管理器
        encyclopediaManager = FindObjectOfType<SimpleEncyclopediaManager>();

        if (inputManager != null)
        {
            // 订阅图鉴输入事件
            inputManager.OnEncyclopediaInput += OnEncyclopediaInput;
            Debug.Log("[EncyclopediaButtonTest] 已订阅图鉴输入事件");
        }
        else
        {
            Debug.LogError("[EncyclopediaButtonTest] 未找到MobileInputManager!");
        }

        if (encyclopediaManager == null)
        {
            Debug.LogWarning("[EncyclopediaButtonTest] 未找到SimpleEncyclopediaManager!");
        }
        else
        {
            Debug.Log("[EncyclopediaButtonTest] 找到SimpleEncyclopediaManager");
        }
    }

    void OnDestroy()
    {
        // 取消订阅避免内存泄漏
        if (inputManager != null)
        {
            inputManager.OnEncyclopediaInput -= OnEncyclopediaInput;
        }
    }

    private void OnEncyclopediaInput()
    {
        if (enableDebugOutput)
        {
            string device = Application.isMobilePlatform ? "移动端" : "桌面端";
            Debug.Log($"[EncyclopediaButtonTest] {device} 图鉴按钮被点击!");

            // 检查图鉴管理器状态
            if (encyclopediaManager != null)
            {
                // 注意：SimpleEncyclopediaManager的isOpen字段是private，无法直接访问
                Debug.Log("[EncyclopediaButtonTest] 图鉴管理器存在，事件应该已传递");
            }
            else
            {
                Debug.LogWarning("[EncyclopediaButtonTest] 图鉴管理器不存在，尝试重新查找");
                encyclopediaManager = FindObjectOfType<SimpleEncyclopediaManager>();
            }
        }
    }

    void Update()
    {
        // 定期检查状态
        if (enableDebugOutput && Time.time - lastTestTime >= testInterval)
        {
            CheckStatus();
            lastTestTime = Time.time;
        }
    }

    void CheckStatus()
    {
        // 检查移动端控制UI是否存在图鉴按钮
        MobileControlsUI mobileControls = FindObjectOfType<MobileControlsUI>();
        if (mobileControls != null)
        {
            // 通过反射或公共方法检查图鉴按钮
            if (mobileControls.encyclopediaButton != null)
            {
                Debug.Log($"[EncyclopediaButtonTest] 图鉴按钮状态: 激活={mobileControls.encyclopediaButton.gameObject.activeInHierarchy}, 可交互={mobileControls.encyclopediaButton.interactable}");
            }
            else
            {
                Debug.LogWarning("[EncyclopediaButtonTest] 移动端图鉴按钮不存在");
            }
        }
        else
        {
            Debug.LogWarning("[EncyclopediaButtonTest] 未找到MobileControlsUI");
        }

        // 检查图鉴管理器的状态
        if (encyclopediaManager != null)
        {
            Debug.Log("[EncyclopediaButtonTest] 图鉴管理器状态: 存在=true");

            // 检查移动端输入管理器是否正确订阅
            if (inputManager != null)
            {
                Debug.Log("[EncyclopediaButtonTest] MobileInputManager和SimpleEncyclopediaManager都存在，事件应该已连接");
            }
            else
            {
                Debug.LogWarning("[EncyclopediaButtonTest] MobileInputManager不存在，事件无法连接");
            }
        }
        else
        {
            Debug.LogWarning("[EncyclopediaButtonTest] 图鉴管理器不存在");
        }
    }

    void OnGUI()
    {
        if (!enableDebugOutput) return;

        GUILayout.BeginArea(new Rect(420, 300, 400, 200));
        GUILayout.Label("=== 图鉴按钮测试 ===");

        if (inputManager != null)
        {
            GUILayout.Label("输入管理器: 正常");

            MobileControlsUI mobileControls = FindObjectOfType<MobileControlsUI>();
            if (mobileControls != null)
            {
                bool hasButton = mobileControls.encyclopediaButton != null;
                GUILayout.Label($"图鉴按钮: {(hasButton ? "存在" : "缺失")}");

                if (hasButton)
                {
                    bool isActive = mobileControls.encyclopediaButton.gameObject.activeInHierarchy;
                    bool isInteractable = mobileControls.encyclopediaButton.interactable;
                    GUILayout.Label($"按钮状态: {(isActive ? "激活" : "未激活")}, {(isInteractable ? "可交互" : "不可交互")}");
                }
            }
            else
            {
                GUILayout.Label("移动端UI: 未找到");
            }
        }
        else
        {
            GUILayout.Label("输入管理器: 未找到");
        }

        if (encyclopediaManager != null)
        {
            GUILayout.Label("图鉴管理器: 正常");
        }
        else
        {
            GUILayout.Label("图鉴管理器: 未找到");
        }

        // 手动测试按钮
        if (GUILayout.Button("手动触发图鉴"))
        {
            Debug.Log("[EncyclopediaButtonTest] GUI按钮手动触发图鉴");
            if (inputManager != null)
            {
                inputManager.TriggerEncyclopediaInput();
            }
        }

        GUILayout.EndArea();
    }
}
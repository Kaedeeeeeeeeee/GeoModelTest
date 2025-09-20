using UnityEngine;

/// <summary>
/// E键样本收集系统测试脚本
/// 验证移动端和桌面端E键样本收集是否正常工作
/// </summary>
public class EKeyCollectionTest : MonoBehaviour
{
    [Header("测试设置")]
    public bool enableDebugOutput = true;
    public float testInterval = 2f; // 测试间隔

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
            // 订阅E键事件
            inputManager.OnInteractInput += OnEKeyInput;
            Debug.Log("[EKeyCollectionTest] 已订阅E键交互事件");
        }
        else
        {
            Debug.LogError("[EKeyCollectionTest] 未找到MobileInputManager!");
        }
    }

    void OnDestroy()
    {
        // 取消订阅避免内存泄漏
        if (inputManager != null)
        {
            inputManager.OnInteractInput -= OnEKeyInput;
        }
    }

    private void OnEKeyInput(bool isPressed)
    {
        if (enableDebugOutput && isPressed) // 只在按下时显示
        {
            string device = Application.isMobilePlatform ? "移动端" : "桌面端";
            Debug.Log($"[EKeyCollectionTest] {device} E键交互: 按下");

            // 检查附近的样本收集器
            CheckNearbySampleCollectors();
        }
    }

    void CheckNearbySampleCollectors()
    {
        // 查找场景中的样本收集器
        SampleCollector[] collectors = FindObjectsOfType<SampleCollector>();
        PlacedSampleCollector[] placedCollectors = FindObjectsOfType<PlacedSampleCollector>();

        Debug.Log($"[EKeyCollectionTest] 发现 {collectors.Length} 个SampleCollector, {placedCollectors.Length} 个PlacedSampleCollector");

        // 检查是否有玩家在收集器范围内
        GameObject player = FindPlayer();
        if (player != null)
        {
            Vector3 playerPos = player.transform.position;

            foreach (var collector in collectors)
            {
                float distance = Vector3.Distance(playerPos, collector.transform.position);
                if (distance <= collector.interactionRange)
                {
                    Debug.Log($"[EKeyCollectionTest] 玩家在SampleCollector范围内 (距离: {distance:F2}m)");
                }
            }

            foreach (var collector in placedCollectors)
            {
                float distance = Vector3.Distance(playerPos, collector.transform.position);
                if (distance <= collector.interactionRange)
                {
                    Debug.Log($"[EKeyCollectionTest] 玩家在PlacedSampleCollector范围内 (距离: {distance:F2}m)");
                }
            }
        }
    }

    GameObject FindPlayer()
    {
        // 寻找玩家对象
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            FirstPersonController fpc = FindObjectOfType<FirstPersonController>();
            if (fpc != null)
            {
                player = fpc.gameObject;
            }
        }
        return player;
    }

    void Update()
    {
        // 每隔一段时间显示一次状态
        if (enableDebugOutput && Time.time - lastTestTime >= testInterval)
        {
            if (inputManager != null)
            {
                Debug.Log($"[EKeyCollectionTest] E键状态: {(inputManager.IsInteracting ? "按下" : "释放")}");
            }
            lastTestTime = Time.time;
        }
    }

    // 在Editor中显示状态信息
    void OnGUI()
    {
        if (!enableDebugOutput) return;

        GUILayout.BeginArea(new Rect(10, 450, 400, 200));
        GUILayout.Label("=== E键收集测试 ===");

        if (inputManager != null)
        {
            GUILayout.Label($"输入管理器状态: 正常");
            GUILayout.Label($"E键状态: {(inputManager.IsInteracting ? "按下" : "释放")}");
            GUILayout.Label($"设备类型: {(inputManager.IsMobileDevice() ? "移动设备" : "桌面设备")}");

            // 显示样本收集器数量
            SampleCollector[] collectors = FindObjectsOfType<SampleCollector>();
            PlacedSampleCollector[] placedCollectors = FindObjectsOfType<PlacedSampleCollector>();
            GUILayout.Label($"场景中的收集器: {collectors.Length + placedCollectors.Length} 个");
        }
        else
        {
            GUILayout.Label("输入管理器状态: 未找到");
        }

        GUILayout.EndArea();
    }
}
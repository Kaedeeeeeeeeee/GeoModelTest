using UnityEngine;
using UnityEditor;
using SampleCuttingSystem;

/// <summary>
/// 切割台输入修复工具 - 专门修复F键输入问题
/// </summary>
public class CuttingStationInputFixer
{
    [MenuItem("Tools/切割系统调试/🔧 修复F键输入问题")]
    public static void FixFKeyInput()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ 请在游戏运行时使用此工具");
            return;
        }

        Debug.Log("=== 🔧 修复F键输入问题 ===");

        // 1. 缩小交互范围回到3米
        RestoreInteractionRange();

        // 2. 检查F键输入处理
        CheckFKeyInputHandling();

        // 3. 修复输入系统冲突
        FixInputSystemConflicts();

        // 4. 强制触发交互逻辑
        ForceTriggerInteraction();

        Debug.Log("🎉 F键输入修复完成！");
    }

    private static void RestoreInteractionRange()
    {
        Debug.Log("🔧 恢复交互范围到3米:");

        CuttingStationInteraction station = Object.FindFirstObjectByType<CuttingStationInteraction>();
        if (station != null)
        {
            var stationType = station.GetType();
            var rangeField = stationType.GetField("interactionRange",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (rangeField != null)
            {
                float currentRange = (float)rangeField.GetValue(station);
                rangeField.SetValue(station, 3f);
                Debug.Log($"✅ 交互范围已恢复: {currentRange}m → 3m");
            }
        }
    }

    private static void CheckFKeyInputHandling()
    {
        Debug.Log("🔧 检查F键输入处理:");

        CuttingStationInteraction station = Object.FindFirstObjectByType<CuttingStationInteraction>();
        if (station != null)
        {
            // 检查当前F键状态
            bool fKeyPressed = Input.GetKey(KeyCode.F);
            bool fKeyDown = Input.GetKeyDown(KeyCode.F);
            Debug.Log($"F键当前状态 - 按下: {fKeyPressed}, 触发: {fKeyDown}");

            // 检查移动端输入
            MobileInputManager mobileInput = MobileInputManager.Instance;
            if (mobileInput != null)
            {
                try
                {
                    var method = mobileInput.GetType().GetMethod("GetSecondaryInteractInput");
                    if (method != null)
                    {
                        bool mobileF = (bool)method.Invoke(mobileInput, null);
                        Debug.Log($"移动端F键状态: {mobileF}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"无法检查移动端F键: {e.Message}");
                }
            }

            // 检查内部F键状态记录
            var stationType = station.GetType();
            var fKeyField = stationType.GetField("wasFKeyPressedLastFrame",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (fKeyField != null)
            {
                bool lastFrameF = (bool)fKeyField.GetValue(station);
                Debug.Log($"上一帧F键状态: {lastFrameF}");
            }
        }
    }

    private static void FixInputSystemConflicts()
    {
        Debug.Log("🔧 修复输入系统冲突:");

        // 检查是否有多个EventSystem
        UnityEngine.EventSystems.EventSystem[] eventSystems = Object.FindObjectsOfType<UnityEngine.EventSystems.EventSystem>();
        Debug.Log($"找到 {eventSystems.Length} 个EventSystem");

        if (eventSystems.Length > 1)
        {
            Debug.LogWarning("⚠️ 检测到多个EventSystem，可能导致输入冲突");
            for (int i = 1; i < eventSystems.Length; i++)
            {
                eventSystems[i].enabled = false;
                Debug.Log($"禁用多余的EventSystem: {eventSystems[i].name}");
            }
        }

        // 检查FirstPersonController是否启用鼠标锁定
        FirstPersonController fpsController = Object.FindFirstObjectByType<FirstPersonController>();
        if (fpsController != null)
        {
            var fpsType = fpsController.GetType();
            var enableMouseField = fpsType.GetField("enableMouseLook",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (enableMouseField != null)
            {
                bool mouseEnabled = (bool)enableMouseField.GetValue(fpsController);
                Debug.Log($"FirstPersonController鼠标控制: {mouseEnabled}");
            }
        }
    }

    private static void ForceTriggerInteraction()
    {
        Debug.Log("🔧 强制触发交互逻辑:");

        CuttingStationInteraction station = Object.FindFirstObjectByType<CuttingStationInteraction>();
        if (station != null)
        {
            // 尝试直接调用交互方法
            var stationType = station.GetType();

            // 查找可能的交互方法
            var methods = stationType.GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            foreach (var method in methods)
            {
                if (method.Name.Contains("Interact") || method.Name.Contains("Open") || method.Name.Contains("Start"))
                {
                    Debug.Log($"找到可能的交互方法: {method.Name}");
                }
            }

            // 尝试调用OpenCuttingInterface方法
            var openMethod = stationType.GetMethod("OpenCuttingInterface",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (openMethod != null)
            {
                try
                {
                    openMethod.Invoke(station, null);
                    Debug.Log("✅ 成功调用OpenCuttingInterface");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"❌ 调用OpenCuttingInterface失败: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ 找不到OpenCuttingInterface方法");
            }
        }
    }

    [MenuItem("Tools/切割系统调试/🎯 强制打开切割界面")]
    public static void ForceOpenCuttingInterface()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ 请在游戏运行时使用此工具");
            return;
        }

        Debug.Log("=== 🎯 强制打开切割界面 ===");

        CuttingStationInteraction station = Object.FindFirstObjectByType<CuttingStationInteraction>();
        if (station != null)
        {
            var stationType = station.GetType();

            // 方法1: 尝试调用OpenCuttingInterface
            var openMethod = stationType.GetMethod("OpenCuttingInterface",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (openMethod != null)
            {
                try
                {
                    openMethod.Invoke(station, null);
                    Debug.Log("✅ 方法1成功：调用OpenCuttingInterface");
                    return;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"❌ 方法1失败: {e.Message}");
                }
            }

            // 方法2: 直接激活切割界面预制体
            var interfaceField = stationType.GetField("cuttingInterfacePrefab",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (interfaceField != null)
            {
                GameObject prefab = (GameObject)interfaceField.GetValue(station);
                if (prefab != null)
                {
                    GameObject instance = Object.Instantiate(prefab);
                    Debug.Log("✅ 方法2成功：直接实例化切割界面预制体");
                    return;
                }
            }

            // 方法3: 查找并激活现有的切割UI
            SampleCuttingSystemManager manager = Object.FindFirstObjectByType<SampleCuttingSystemManager>();
            if (manager != null)
            {
                Debug.Log("找到切割系统管理器，尝试激活");
                manager.gameObject.SetActive(true);

                // 查找切割UI组件
                CuttingStationUI cuttingUI = Object.FindFirstObjectByType<CuttingStationUI>();
                if (cuttingUI != null)
                {
                    cuttingUI.gameObject.SetActive(true);
                    Debug.Log("✅ 方法3成功：激活切割UI组件");
                    return;
                }
            }

            Debug.LogError("❌ 所有方法都失败了，无法打开切割界面");
        }
        else
        {
            Debug.LogError("❌ 找不到CuttingStationInteraction组件");
        }
    }

    [MenuItem("Tools/切割系统调试/📋 检查切割界面组件")]
    public static void CheckCuttingInterfaceComponents()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ 请在游戏运行时使用此工具");
            return;
        }

        Debug.Log("=== 📋 检查切割界面组件 ===");

        // 检查切割台交互组件
        CuttingStationInteraction station = Object.FindFirstObjectByType<CuttingStationInteraction>();
        if (station != null)
        {
            Debug.Log($"✅ CuttingStationInteraction: {station.name}");

            var stationType = station.GetType();
            var prefabField = stationType.GetField("cuttingInterfacePrefab",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (prefabField != null)
            {
                GameObject prefab = (GameObject)prefabField.GetValue(station);
                Debug.Log($"  切割界面预制体: {(prefab != null ? prefab.name : "null")}");
            }

            var parentField = stationType.GetField("interfaceParent",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (parentField != null)
            {
                Transform parent = (Transform)parentField.GetValue(station);
                Debug.Log($"  界面父对象: {(parent != null ? parent.name : "null")}");
            }
        }

        // 检查切割系统管理器
        SampleCuttingSystemManager manager = Object.FindFirstObjectByType<SampleCuttingSystemManager>();
        if (manager != null)
        {
            Debug.Log($"✅ SampleCuttingSystemManager: {manager.name}");
            Debug.Log($"  激活状态: {manager.gameObject.activeInHierarchy}");
            Debug.Log($"  启用状态: {manager.enabled}");
        }
        else
        {
            Debug.LogWarning("⚠️ 未找到SampleCuttingSystemManager");
        }

        // 检查切割游戏组件
        SampleCuttingGame game = Object.FindFirstObjectByType<SampleCuttingGame>();
        if (game != null)
        {
            Debug.Log($"✅ SampleCuttingGame: {game.name}");
            Debug.Log($"  激活状态: {game.gameObject.activeInHierarchy}");
            Debug.Log($"  启用状态: {game.enabled}");
        }
        else
        {
            Debug.LogWarning("⚠️ 未找到SampleCuttingGame");
        }

        // 检查切割UI
        CuttingStationUI ui = Object.FindFirstObjectByType<CuttingStationUI>();
        if (ui != null)
        {
            Debug.Log($"✅ CuttingStationUI: {ui.name}");
            Debug.Log($"  激活状态: {ui.gameObject.activeInHierarchy}");
            Debug.Log($"  启用状态: {ui.enabled}");
        }
        else
        {
            Debug.LogWarning("⚠️ 未找到CuttingStationUI");
        }

        Debug.Log("=== 检查完成 ===");
    }

    [MenuItem("Tools/切割系统调试/🔍 实时F键监控")]
    public static void StartFKeyMonitoring()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ 请在游戏运行时使用此工具");
            return;
        }

        Debug.Log("=== 🔍 开始实时F键监控 ===");
        Debug.Log("按F键测试，按ESC键停止监控");

        // 创建监控组件
        GameObject monitor = new GameObject("FKeyMonitor");
        monitor.AddComponent<FKeyMonitor>();
    }
}

/// <summary>
/// F键监控组件
/// </summary>
public class FKeyMonitor : MonoBehaviour
{
    private bool lastFKeyState = false;

    void Update()
    {
        bool currentFKey = Input.GetKey(KeyCode.F);
        bool fKeyDown = Input.GetKeyDown(KeyCode.F);
        bool fKeyUp = Input.GetKeyUp(KeyCode.F);

        if (fKeyDown)
        {
            Debug.Log("🔍 F键按下！");
            CheckInteractionState();
        }

        if (fKeyUp)
        {
            Debug.Log("🔍 F键释放！");
        }

        // 检查移动端输入
        MobileInputManager mobileInput = MobileInputManager.Instance;
        if (mobileInput != null)
        {
            try
            {
                var method = mobileInput.GetType().GetMethod("GetSecondaryInteractInput");
                if (method != null)
                {
                    bool mobileF = (bool)method.Invoke(mobileInput, null);
                    if (mobileF && !lastFKeyState)
                    {
                        Debug.Log("🔍 移动端F键触发！");
                        CheckInteractionState();
                    }
                    lastFKeyState = mobileF;
                }
            }
            catch (System.Exception) { }
        }

        // ESC键停止监控
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("⏹️ 停止F键监控");
            Destroy(gameObject);
        }
    }

    private void CheckInteractionState()
    {
        CuttingStationInteraction station = FindFirstObjectByType<CuttingStationInteraction>();
        if (station != null)
        {
            var stationType = station.GetType();

            // 检查玩家是否在范围内
            var inRangeField = stationType.GetField("playerInRange",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (inRangeField != null)
            {
                bool inRange = (bool)inRangeField.GetValue(station);
                Debug.Log($"  玩家在范围内: {inRange}");
            }

            // 检查附近玩家
            var playerField = stationType.GetField("nearbyPlayer",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (playerField != null)
            {
                GameObject player = (GameObject)playerField.GetValue(station);
                Debug.Log($"  附近玩家: {(player != null ? player.name : "null")}");
            }
        }
    }
}
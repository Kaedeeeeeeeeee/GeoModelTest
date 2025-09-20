using UnityEngine;
using UnityEditor;
using SampleCuttingSystem;

/// <summary>
/// 切割台调试工具 - 专门诊断切割台交互问题
/// </summary>
public class CuttingStationDebugTool
{
    [MenuItem("Tools/切割系统调试/🔍 诊断切割台交互问题")]
    public static void DiagnoseCuttingStationInteraction()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ 请在游戏运行时使用此工具");
            return;
        }

        Debug.Log("=== 🔍 切割台交互问题诊断 ===");

        // 1. 检查切割台组件
        CheckCuttingStationComponents();

        // 2. 检查玩家位置和距离
        CheckPlayerDistance();

        // 3. 检查UI提示组件
        CheckInteractionPrompt();

        // 4. 检查输入系统
        CheckInputSystem();

        // 5. 检查场景初始化
        CheckSceneInitialization();

        Debug.Log("=== 诊断完成 ===");
    }

    private static void CheckCuttingStationComponents()
    {
        Debug.Log("📊 检查切割台组件:");

        CuttingStationInteraction[] stations = Object.FindObjectsOfType<CuttingStationInteraction>();
        Debug.Log($"找到 {stations.Length} 个切割台组件");

        if (stations.Length == 0)
        {
            Debug.LogError("❌ 未找到CuttingStationInteraction组件！");

            // 搜索可能的切割台对象
            GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
            foreach (var obj in allObjects)
            {
                if (obj.name.ToLower().Contains("cutting") || obj.name.ToLower().Contains("station"))
                {
                    Debug.Log($"  可能的切割台对象: {obj.name}");
                    Debug.Log($"    位置: {obj.transform.position}");
                    Debug.Log($"    激活状态: {obj.activeInHierarchy}");

                    // 检查组件
                    var components = obj.GetComponents<Component>();
                    Debug.Log($"    组件数量: {components.Length}");
                    foreach (var comp in components)
                    {
                        Debug.Log($"      - {comp.GetType().Name}");
                    }
                }
            }
        }
        else
        {
            for (int i = 0; i < stations.Length; i++)
            {
                var station = stations[i];
                Debug.Log($"切割台 {i+1}: {station.name}");
                Debug.Log($"  位置: {station.transform.position}");
                Debug.Log($"  激活状态: {station.gameObject.activeInHierarchy}");
                Debug.Log($"  启用状态: {station.enabled}");

                // 使用反射检查内部状态
                CheckStationInternalState(station);
            }
        }
    }

    private static void CheckStationInternalState(CuttingStationInteraction station)
    {
        var stationType = station.GetType();

        // 检查交互范围
        var rangeField = stationType.GetField("interactionRange",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (rangeField != null)
        {
            float range = (float)rangeField.GetValue(station);
            Debug.Log($"    交互范围: {range}m");
        }

        // 检查玩家层级
        var layerField = stationType.GetField("playerLayer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (layerField != null)
        {
            LayerMask layer = (LayerMask)layerField.GetValue(station);
            Debug.Log($"    玩家层级: {layer.value}");
        }

        // 检查交互按键
        var keyField = stationType.GetField("interactionKey",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (keyField != null)
        {
            KeyCode key = (KeyCode)keyField.GetValue(station);
            Debug.Log($"    交互按键: {key}");
        }

        // 检查提示UI
        var promptField = stationType.GetField("interactionPrompt",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (promptField != null)
        {
            GameObject prompt = (GameObject)promptField.GetValue(station);
            Debug.Log($"    交互提示UI: {(prompt != null ? prompt.name : "null")}");
            if (prompt != null)
            {
                Debug.Log($"      提示UI激活: {prompt.activeInHierarchy}");
            }
        }

        // 检查Canvas
        var canvasField = stationType.GetField("promptCanvas",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (canvasField != null)
        {
            Canvas canvas = (Canvas)canvasField.GetValue(station);
            Debug.Log($"    提示Canvas: {(canvas != null ? canvas.name : "null")}");
            if (canvas != null)
            {
                Debug.Log($"      Canvas激活: {canvas.gameObject.activeInHierarchy}");
                Debug.Log($"      Canvas层级: {canvas.sortingOrder}");
            }
        }

        // 检查玩家在范围内状态
        var inRangeField = stationType.GetField("playerInRange",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (inRangeField != null)
        {
            bool inRange = (bool)inRangeField.GetValue(station);
            Debug.Log($"    玩家在范围内: {inRange}");
        }

        // 检查附近玩家
        var playerField = stationType.GetField("nearbyPlayer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (playerField != null)
        {
            GameObject player = (GameObject)playerField.GetValue(station);
            Debug.Log($"    附近玩家: {(player != null ? player.name : "null")}");
        }
    }

    private static void CheckPlayerDistance()
    {
        Debug.Log("📊 检查玩家距离:");

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            // 尝试其他方式查找玩家
            FirstPersonController fpsController = Object.FindFirstObjectByType<FirstPersonController>();
            if (fpsController != null)
            {
                player = fpsController.gameObject;
                Debug.Log($"通过FirstPersonController找到玩家: {player.name}");
            }
        }

        if (player == null)
        {
            Debug.LogError("❌ 找不到玩家对象！");
            return;
        }

        Debug.Log($"玩家位置: {player.transform.position}");
        Debug.Log($"玩家Tag: {player.tag}");
        Debug.Log($"玩家Layer: {LayerMask.LayerToName(player.layer)} ({player.layer})");

        // 计算到每个切割台的距离
        CuttingStationInteraction[] stations = Object.FindObjectsOfType<CuttingStationInteraction>();
        foreach (var station in stations)
        {
            float distance = Vector3.Distance(player.transform.position, station.transform.position);
            Debug.Log($"到切割台 {station.name} 的距离: {distance:F2}m");

            // 检查是否在检测范围内
            var stationType = station.GetType();
            var rangeField = stationType.GetField("interactionRange",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (rangeField != null)
            {
                float range = (float)rangeField.GetValue(station);
                bool inRange = distance <= range;
                Debug.Log($"  是否在范围内 ({range}m): {(inRange ? "✅ 是" : "❌ 否")}");
            }
        }
    }

    private static void CheckInteractionPrompt()
    {
        Debug.Log("📊 检查交互提示UI:");

        // 检查Canvas
        Canvas[] canvases = Object.FindObjectsOfType<Canvas>();
        foreach (var canvas in canvases)
        {
            if (canvas.name.ToLower().Contains("cutting") || canvas.name.ToLower().Contains("prompt"))
            {
                Debug.Log($"切割相关Canvas: {canvas.name}");
                Debug.Log($"  激活状态: {canvas.gameObject.activeInHierarchy}");
                Debug.Log($"  排序层级: {canvas.sortingOrder}");
                Debug.Log($"  渲染模式: {canvas.renderMode}");

                // 检查子对象
                CheckCanvasChildren(canvas.transform, 0);
            }
        }

        // 检查所有包含"prompt"或"interaction"的对象
        GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
        foreach (var obj in allObjects)
        {
            if (obj.name.ToLower().Contains("prompt") || obj.name.ToLower().Contains("interaction"))
            {
                Debug.Log($"交互提示相关对象: {obj.name}");
                Debug.Log($"  激活状态: {obj.activeInHierarchy}");
                Debug.Log($"  位置: {obj.transform.position}");

                var text = obj.GetComponent<UnityEngine.UI.Text>();
                if (text != null)
                {
                    Debug.Log($"  文本内容: '{text.text}'");
                    Debug.Log($"  文本颜色: {text.color}");
                }
            }
        }
    }

    private static void CheckCanvasChildren(Transform parent, int depth)
    {
        if (depth > 3) return; // 限制递归深度

        string indent = new string(' ', depth * 2);
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            Debug.Log($"{indent}- {child.name} (激活: {child.gameObject.activeInHierarchy})");

            var text = child.GetComponent<UnityEngine.UI.Text>();
            if (text != null)
            {
                Debug.Log($"{indent}  文本: '{text.text}'");
            }

            CheckCanvasChildren(child, depth + 1);
        }
    }

    private static void CheckInputSystem()
    {
        Debug.Log("📊 检查输入系统:");

        // 检查F键输入
        if (Input.GetKey(KeyCode.F))
        {
            Debug.Log("✅ F键当前被按下");
        }
        else
        {
            Debug.Log("F键当前未被按下");
        }

        // 检查移动端输入管理器
        MobileInputManager mobileInput = MobileInputManager.Instance;
        if (mobileInput != null)
        {
            Debug.Log($"MobileInputManager存在: {mobileInput.name}");
            Debug.Log($"  桌面测试模式: {mobileInput.desktopTestMode}");

            // 检查F键输入状态
            try
            {
                var method = mobileInput.GetType().GetMethod("GetSecondaryInteractInput",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (method != null)
                {
                    bool fKeyPressed = (bool)method.Invoke(mobileInput, null);
                    Debug.Log($"  F键输入状态: {fKeyPressed}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"无法检查F键输入状态: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ MobileInputManager不存在");
        }
    }

    private static void CheckSceneInitialization()
    {
        Debug.Log("📊 检查场景初始化:");

        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        Debug.Log($"当前场景: {sceneName}");

        // 检查切割系统初始化器
        LabCuttingSystemInitializer[] initializers = Object.FindObjectsOfType<LabCuttingSystemInitializer>();
        Debug.Log($"找到 {initializers.Length} 个切割系统初始化器");

        foreach (var initializer in initializers)
        {
            Debug.Log($"初始化器: {initializer.name}");
            Debug.Log($"  激活状态: {initializer.gameObject.activeInHierarchy}");
            Debug.Log($"  启用状态: {initializer.enabled}");
        }

        // 检查切割系统管理器
        SampleCuttingSystemManager[] managers = Object.FindObjectsOfType<SampleCuttingSystemManager>();
        Debug.Log($"找到 {managers.Length} 个切割系统管理器");

        foreach (var manager in managers)
        {
            Debug.Log($"管理器: {manager.name}");
            Debug.Log($"  激活状态: {manager.gameObject.activeInHierarchy}");
            Debug.Log($"  启用状态: {manager.enabled}");
        }
    }

    [MenuItem("Tools/切割系统调试/🔧 强制创建交互提示")]
    public static void ForceCreateInteractionPrompt()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ 请在游戏运行时使用此工具");
            return;
        }

        Debug.Log("=== 🔧 强制创建交互提示 ===");

        CuttingStationInteraction station = Object.FindFirstObjectByType<CuttingStationInteraction>();
        if (station != null)
        {
            // 使用反射调用SetupInteractionPrompt方法
            var method = station.GetType().GetMethod("SetupInteractionPrompt",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (method != null)
            {
                try
                {
                    method.Invoke(station, null);
                    Debug.Log("✅ 成功调用SetupInteractionPrompt");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"❌ 调用SetupInteractionPrompt失败: {e.Message}");
                }
            }
            else
            {
                Debug.LogError("❌ 找不到SetupInteractionPrompt方法");
            }
        }
        else
        {
            Debug.LogError("❌ 找不到CuttingStationInteraction组件");
        }
    }

    [MenuItem("Tools/切割系统调试/🎯 手动触发交互检测")]
    public static void ManualTriggerInteraction()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ 请在游戏运行时使用此工具");
            return;
        }

        CuttingStationInteraction station = Object.FindFirstObjectByType<CuttingStationInteraction>();
        if (station != null)
        {
            // 强制设置玩家在范围内
            var stationType = station.GetType();
            var inRangeField = stationType.GetField("playerInRange",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (inRangeField != null)
            {
                inRangeField.SetValue(station, true);
                Debug.Log("✅ 强制设置玩家在范围内");
            }

            // 尝试显示提示
            var promptField = stationType.GetField("interactionPrompt",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (promptField != null)
            {
                GameObject prompt = (GameObject)promptField.GetValue(station);
                if (prompt != null)
                {
                    prompt.SetActive(true);
                    Debug.Log("✅ 强制显示交互提示");
                }
            }
        }
    }
}
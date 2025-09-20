using UnityEngine;
using UnityEditor;
using SampleCuttingSystem;

/// <summary>
/// 切割台修复工具 - 修复切割台交互问题
/// </summary>
public class CuttingStationFixer
{
    [MenuItem("Tools/切割系统调试/🔧 修复切割台交互问题")]
    public static void FixCuttingStationInteraction()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ 请在游戏运行时使用此工具");
            return;
        }

        Debug.Log("=== 🔧 修复切割台交互问题 ===");

        // 1. 修复距离问题
        FixDistanceIssue();

        // 2. 修复玩家标签和层级
        FixPlayerTagAndLayer();

        // 3. 修复交互范围
        FixInteractionRange();

        // 4. 强制激活交互提示
        ForceActivateInteractionPrompt();

        // 5. 修复Canvas层级
        FixCanvasLayers();

        Debug.Log("🎉 切割台交互问题修复完成！");
    }

    private static void FixDistanceIssue()
    {
        Debug.Log("🔧 修复距离问题:");

        GameObject player = FindPlayer();
        CuttingStationInteraction station = Object.FindFirstObjectByType<CuttingStationInteraction>();

        if (player != null && station != null)
        {
            Vector3 playerPos = player.transform.position;
            Vector3 stationPos = station.transform.position;
            float distance = Vector3.Distance(playerPos, stationPos);

            Debug.Log($"当前距离: {distance:F2}m");

            if (distance > 3f)
            {
                // 将玩家传送到切割台附近
                Vector3 newPosition = stationPos + Vector3.forward * -2f; // 距离切割台2米
                newPosition.y = playerPos.y; // 保持玩家高度

                player.transform.position = newPosition;

                float newDistance = Vector3.Distance(player.transform.position, stationPos);
                Debug.Log($"✅ 玩家已传送到切割台附近，新距离: {newDistance:F2}m");
            }
        }
    }

    private static void FixPlayerTagAndLayer()
    {
        Debug.Log("🔧 修复玩家标签和层级:");

        GameObject player = FindPlayer();
        if (player != null)
        {
            // 检查并修复Tag
            if (player.tag != "Player")
            {
                // 尝试设置为Player标签
                try
                {
                    player.tag = "Player";
                    Debug.Log("✅ 玩家标签设置为'Player'");
                }
                catch (System.Exception)
                {
                    Debug.LogWarning("⚠️ Player标签不存在，保持当前标签");
                }
            }

            // 检查并修复Layer
            Debug.Log($"当前玩家Layer: {LayerMask.LayerToName(player.layer)} ({player.layer})");

            // 检查是否有Player层级
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer != -1)
            {
                player.layer = playerLayer;
                Debug.Log($"✅ 玩家层级设置为Player层 ({playerLayer})");
            }
            else
            {
                Debug.Log("Player层级不存在，保持当前层级");
            }
        }
    }

    private static void FixInteractionRange()
    {
        Debug.Log("🔧 修复交互范围:");

        CuttingStationInteraction station = Object.FindFirstObjectByType<CuttingStationInteraction>();
        if (station != null)
        {
            var stationType = station.GetType();
            var rangeField = stationType.GetField("interactionRange",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (rangeField != null)
            {
                float currentRange = (float)rangeField.GetValue(station);
                Debug.Log($"当前交互范围: {currentRange}m");

                // 增加交互范围到10米，确保能够触发
                float newRange = 10f;
                rangeField.SetValue(station, newRange);
                Debug.Log($"✅ 交互范围扩大到: {newRange}m");
            }
        }
    }

    private static void ForceActivateInteractionPrompt()
    {
        Debug.Log("🔧 强制激活交互提示:");

        CuttingStationInteraction station = Object.FindFirstObjectByType<CuttingStationInteraction>();
        if (station != null)
        {
            var stationType = station.GetType();

            // 强制设置玩家在范围内
            var inRangeField = stationType.GetField("playerInRange",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (inRangeField != null)
            {
                inRangeField.SetValue(station, true);
                Debug.Log("✅ 强制设置玩家在范围内");
            }

            // 设置附近玩家
            var playerField = stationType.GetField("nearbyPlayer",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (playerField != null)
            {
                GameObject player = FindPlayer();
                playerField.SetValue(station, player);
                Debug.Log("✅ 设置附近玩家引用");
            }

            // 强制激活交互提示
            var promptField = stationType.GetField("interactionPrompt",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (promptField != null)
            {
                GameObject prompt = (GameObject)promptField.GetValue(station);
                if (prompt != null)
                {
                    prompt.SetActive(true);
                    Debug.Log("✅ 强制激活交互提示UI");

                    // 检查文本内容
                    var text = prompt.GetComponentInChildren<UnityEngine.UI.Text>();
                    if (text != null)
                    {
                        text.text = "[F] 开始切割样本";
                        text.color = Color.white;
                        Debug.Log("✅ 设置交互提示文本");
                    }
                }
            }

            // 强制激活Canvas
            var canvasField = stationType.GetField("promptCanvas",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (canvasField != null)
            {
                Canvas canvas = (Canvas)canvasField.GetValue(station);
                if (canvas != null)
                {
                    canvas.gameObject.SetActive(true);
                    canvas.sortingOrder = 300; // 确保在最上层
                    Debug.Log("✅ 强制激活提示Canvas并设置高层级");
                }
            }
        }
    }

    private static void FixCanvasLayers()
    {
        Debug.Log("🔧 修复Canvas层级:");

        // 查找切割相关的Canvas并调整层级
        Canvas[] canvases = Object.FindObjectsOfType<Canvas>();
        foreach (var canvas in canvases)
        {
            if (canvas.name.ToLower().Contains("cutting") ||
                canvas.name.ToLower().Contains("prompt") ||
                canvas.name.Contains("InteractionPrompt"))
            {
                int oldOrder = canvas.sortingOrder;
                canvas.sortingOrder = 300; // 设置为高层级
                canvas.overrideSorting = true;

                Debug.Log($"Canvas {canvas.name}: {oldOrder} → {canvas.sortingOrder}");

                // 确保Canvas和子对象都激活
                canvas.gameObject.SetActive(true);

                // 激活所有子对象
                SetActiveRecursively(canvas.transform, true);
            }
        }
    }

    private static void SetActiveRecursively(Transform parent, bool active)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            child.gameObject.SetActive(active);
            SetActiveRecursively(child, active);
        }
    }

    private static GameObject FindPlayer()
    {
        // 优先通过Tag查找
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            // 通过FirstPersonController查找
            FirstPersonController fpsController = Object.FindFirstObjectByType<FirstPersonController>();
            if (fpsController != null)
            {
                player = fpsController.gameObject;
            }
        }

        if (player == null)
        {
            // 通过名称查找（Lily是玩家角色名）
            player = GameObject.Find("Lily");
        }

        return player;
    }

    [MenuItem("Tools/切割系统调试/📏 调整切割台位置到玩家附近")]
    public static void MoveCuttingStationToPlayer()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ 请在游戏运行时使用此工具");
            return;
        }

        GameObject player = FindPlayer();
        CuttingStationInteraction station = Object.FindFirstObjectByType<CuttingStationInteraction>();

        if (player != null && station != null)
        {
            // 将切割台移动到玩家前方3米处
            Vector3 playerForward = player.transform.forward;
            Vector3 newPosition = player.transform.position + playerForward * 3f;
            newPosition.y = station.transform.position.y; // 保持切割台高度

            station.transform.position = newPosition;

            float distance = Vector3.Distance(player.transform.position, newPosition);
            Debug.Log($"✅ 切割台已移动到玩家前方，距离: {distance:F2}m");
        }
    }

    [MenuItem("Tools/切割系统调试/🎯 一键完整修复")]
    public static void CompleteFixAll()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ 请在游戏运行时使用此工具");
            return;
        }

        Debug.Log("=== 🎯 一键完整修复切割台 ===");

        // 1. 修复所有问题
        FixCuttingStationInteraction();

        // 2. 等待一帧后再次检查
        EditorApplication.delayCall += () =>
        {
            // 强制刷新交互检测
            CuttingStationInteraction station = Object.FindFirstObjectByType<CuttingStationInteraction>();
            if (station != null)
            {
                // 调用CheckPlayerInteraction方法
                var method = station.GetType().GetMethod("CheckPlayerInteraction",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (method != null)
                {
                    method.Invoke(station, null);
                    Debug.Log("✅ 强制刷新交互检测");
                }
            }
        };

        Debug.Log("🎉 一键修复完成！现在应该能看到交互提示了！");
    }
}
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 放置工具收回系统 - 允许玩家按G键收回放置的工具
/// </summary>
public class PlacedToolRecaller : MonoBehaviour
{
    [Header("收回设置")]
    public float interactionRange = 5f; // 收回距离
    public string toolName = "工具"; // 工具名称
    public KeyCode recallKey = KeyCode.G; // 收回按键
    
    [Header("视觉反馈")]
    public bool showInteractionPrompt = true;
    public float promptInterval = 2f; // 提示间隔时间
    
    private bool playerInRange = false;
    private FirstPersonController nearbyPlayer = null;
    private float lastPromptTime = 0f;
    
    void Update()
    {
        CheckPlayerProximity();
        HandleRecallInput();
    }
    
    /// <summary>
    /// 检查玩家是否在附近
    /// </summary>
    void CheckPlayerProximity()
    {
        // 查找附近的玩家
        FirstPersonController player = FindFirstObjectByType<FirstPersonController>();
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            bool wasInRange = playerInRange;
            playerInRange = distance <= interactionRange;
            nearbyPlayer = playerInRange ? player : null;
            
            // 显示交互提示
            if (playerInRange && showInteractionPrompt)
            {
                if (Time.time - lastPromptTime > promptInterval)
                {
                    Debug.Log($"按 {recallKey} 键收回 {toolName} (距离: {distance:F1}m)");
                    lastPromptTime = Time.time;
                }
            }
        }
        else
        {
            playerInRange = false;
            nearbyPlayer = null;
        }
    }
    
    /// <summary>
    /// 处理收回输入
    /// </summary>
    void HandleRecallInput()
    {
        if (!playerInRange || nearbyPlayer == null) return;
        
        // 检查按键输入
        bool keyPressed = false;
        if (recallKey == KeyCode.G)
        {
            keyPressed = Keyboard.current.gKey.wasPressedThisFrame;
        }
        else
        {
            keyPressed = Input.GetKeyDown(recallKey);
        }
        
        if (keyPressed)
        {
            RecallTool();
        }
    }
    
    /// <summary>
    /// 收回工具
    /// </summary>
    void RecallTool()
    {
        Debug.Log($"收回工具: {toolName}");
        
        // 停止工具的操作（如果正在操作中）
        StopToolOperation();
        
        // 将工具收回到库存
        ReturnToInventory();
        
        // 销毁工具对象
        Destroy(gameObject);
    }
    
    /// <summary>
    /// 停止工具操作
    /// </summary>
    void StopToolOperation()
    {
        // 检查是否是无人机控制器
        DroneController droneController = GetComponent<DroneController>();
        if (droneController != null)
        {
            // 如果玩家正在控制无人机，先退出控制
            var isControlledField = typeof(DroneController).GetField("isBeingControlled", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (isControlledField != null && (bool)isControlledField.GetValue(droneController))
            {
                // 调用停止控制方法
                var stopMethod = typeof(DroneController).GetMethod("StopControlling", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                stopMethod?.Invoke(droneController, null);
            }
        }
        
        // 检查是否是钻探车控制器
        DrillCarController carController = GetComponent<DrillCarController>();
        if (carController != null)
        {
            // 如果玩家正在驾驶车辆，先退出驾驶
            var isBeingDrivenField = typeof(DrillCarController).GetField("isBeingDriven", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (isBeingDrivenField != null && (bool)isBeingDrivenField.GetValue(carController))
            {
                // 调用停止驾驶方法
                var stopMethod = typeof(DrillCarController).GetMethod("StopDriving", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                stopMethod?.Invoke(carController, null);
            }
        }
    }
    
    /// <summary>
    /// 将工具归还到库存
    /// </summary>
    void ReturnToInventory()
    {
        // 找到工具管理器
        ToolManager toolManager = FindFirstObjectByType<ToolManager>();
        if (toolManager != null)
        {
            // 重置对应工具的放置状态
            if (GetComponent<DroneController>() != null)
            {
                // 查找无人机工具并重置其状态
                DroneTool droneTool = FindFirstObjectByType<DroneTool>();
                if (droneTool != null)
                {
                    ResetPlaceableToolState(droneTool);
                    
                    // 如果当前装备的是无人机工具，取消装备
                    if (toolManager.GetCurrentTool() == droneTool)
                    {
                        toolManager.UnequipCurrentTool();
                        Debug.Log("无人机已收回并取消装备");
                    }
                    else
                    {
                        Debug.Log("无人机已收回到库存，可以重新放置");
                    }
                }
            }
            else if (GetComponent<DrillCarController>() != null)
            {
                // 查找钻探车工具并重置其状态
                DrillCarTool carTool = FindFirstObjectByType<DrillCarTool>();
                if (carTool != null)
                {
                    ResetPlaceableToolState(carTool);
                    
                    // 如果当前装备的是钻探车工具，取消装备
                    if (toolManager.GetCurrentTool() == carTool)
                    {
                        toolManager.UnequipCurrentTool();
                        Debug.Log("钻探车已收回并取消装备");
                    }
                    else
                    {
                        Debug.Log("钻探车已收回到库存，可以重新放置");
                    }
                }
            }
            else if (GetComponent<DrillTower>() != null)
            {
                // 查找钻塔工具并重置其状态
                DrillTowerTool towerTool = FindFirstObjectByType<DrillTowerTool>();
                if (towerTool != null)
                {
                    ResetPlaceableToolState(towerTool);
                    
                    // 如果当前装备的是钻塔工具，取消装备
                    if (toolManager.GetCurrentTool() == towerTool)
                    {
                        toolManager.UnequipCurrentTool();
                        Debug.Log("钻塔已收回并取消装备");
                    }
                    else
                    {
                        Debug.Log("钻塔已收回到库存，可以重新放置");
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 重置PlaceableTool的状态，使其可以再次放置
    /// </summary>
    void ResetPlaceableToolState(PlaceableTool placeableTool)
    {
        if (placeableTool == null) return;
        
        // 使用反射重置hasPlacedObject状态
        var hasPlacedObjectField = typeof(PlaceableTool).GetField("hasPlacedObject", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (hasPlacedObjectField != null)
        {
            hasPlacedObjectField.SetValue(placeableTool, false);
        }
        
        // 重置canUse状态，允许立即使用
        var canUseField = typeof(CollectionTool).GetField("canUse", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (canUseField != null)
        {
            canUseField.SetValue(placeableTool, true);
        }
    }
    
    /// <summary>
    /// 在Scene视图中显示交互范围
    /// </summary>
    void OnDrawGizmosSelected()
    {
        // 绘制交互范围
        Gizmos.color = playerInRange ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
        
        // 绘制收回提示
        if (playerInRange)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, Vector3.one * 0.5f);
        }
    }
}

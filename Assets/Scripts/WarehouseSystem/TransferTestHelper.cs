using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 仓库传输测试助手 - 用于测试物品传输后选中标记是否正确清除
/// </summary>
public class TransferTestHelper : MonoBehaviour
{
    /// <summary>
    /// 测试从仓库到背包的传输
    /// </summary>
    [ContextMenu("测试仓库到背包传输")]
    public void TestWarehouseToInventoryTransfer()
    {
        StartCoroutine(TestTransferProcess());
    }
    
    /// <summary>
    /// 测试传输流程
    /// </summary>
    IEnumerator TestTransferProcess()
    {
        Debug.Log("=== 开始传输测试 ===");
        
        // 1. 获取系统引用
        var multiSelectSystem = FindFirstObjectByType<MultiSelectSystem>();
        var warehouseManager = WarehouseManager.Instance;
        
        if (multiSelectSystem == null)
        {
            Debug.LogError("未找到多选系统!");
            yield break;
        }
        
        if (warehouseManager == null)
        {
            Debug.LogError("未找到仓库管理器!");
            yield break;
        }
        
        // 2. 获取仓库中的第一个物品
        var warehouseItems = warehouseManager.Storage.GetAllItems();
        if (warehouseItems.Count == 0)
        {
            Debug.LogError("仓库中没有物品可供测试!");
            yield break;
        }
        
        var testItem = warehouseItems[0];
        Debug.Log($"测试物品: {testItem.displayName}, 位置: {testItem.currentLocation}");
        
        // 3. 进入多选模式并选择物品
        multiSelectSystem.EnterMultiSelectMode();
        yield return new WaitForSeconds(0.1f);
        
        bool selected = multiSelectSystem.SelectItem(testItem, SampleLocation.InWarehouse);
        Debug.Log($"选择物品结果: {selected}");
        
        if (!selected)
        {
            Debug.LogError("选择物品失败!");
            yield break;
        }
        
        // 4. 检查选中状态
        Debug.Log($"物品是否被选中: {multiSelectSystem.IsItemSelected(testItem)}");
        Debug.Log($"选中物品数量: {multiSelectSystem.GetSelectedCount()}");
        
        // 5. 执行传输
        var selectedItems = multiSelectSystem.GetSelectedItems();
        Debug.Log($"准备传输 {selectedItems.Count} 个物品");
        
        bool transferSuccess = warehouseManager.BatchMoveFromWarehouseToInventory(selectedItems);
        Debug.Log($"传输结果: {transferSuccess}");
        
        // 6. 检查传输后的状态
        yield return new WaitForSeconds(0.5f);
        
        Debug.Log($"传输后物品位置: {testItem.currentLocation}");
        Debug.Log($"传输后是否仍被选中: {multiSelectSystem.IsItemSelected(testItem)}");
        Debug.Log($"传输后选中物品数量: {multiSelectSystem.GetSelectedCount()}");
        
        // 7. 检查所有槽位的选中状态
        var allSlots = FindObjectsByType<WarehouseItemSlot>(FindObjectsSortMode.None);
        int selectedSlotsCount = 0;
        foreach (var slot in allSlots)
        {
            if (slot.IsSelected())
            {
                selectedSlotsCount++;
                Debug.Log($"发现选中的槽位: {slot.GetItemName()} (物品位置: {slot.GetItem()?.currentLocation})");
            }
        }
        
        Debug.Log($"所有槽位中选中的数量: {selectedSlotsCount}");
        
        if (selectedSlotsCount == 0)
        {
            Debug.Log("✅ 测试通过: 传输后没有残留的选中标记");
        }
        else
        {
            Debug.LogError($"❌ 测试失败: 传输后仍有 {selectedSlotsCount} 个槽位显示为选中状态");
        }
        
        Debug.Log("=== 传输测试完成 ===");
    }
}
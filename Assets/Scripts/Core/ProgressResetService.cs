using UnityEngine;
using System.IO;

/// <summary>
/// 提供运行时的一键清档（可从任意UI调用）。
/// 包含：任务进度、已解锁工具、背包、仓库（文件+内存）。
/// </summary>
public static class ProgressResetService
{
    // 与各系统保持一致的键名
    private const string UnlockedToolsPrefsKey = "PlayerPersistentData.UnlockedToolIds";
    private const string InventoryPrefsKey = "PlayerPersistentData.Inventory";
    private const string CompletedQuestsKey = "QuestSystem.CompletedQuests";
    private const string CompletedObjectivesKey = "QuestSystem.CompletedObjectives";
    private const string ClassRoomHiddenKey = "MainScene.ClassRoom.Hidden";

    public static void ResetAll()
    {
        // 1) 清 PlayerPrefs 存档
        PlayerPrefs.DeleteKey(InventoryPrefsKey);
        PlayerPrefs.DeleteKey(UnlockedToolsPrefsKey);
        PlayerPrefs.DeleteKey(CompletedQuestsKey);
        PlayerPrefs.DeleteKey(CompletedObjectivesKey);
        PlayerPrefs.DeleteKey(ClassRoomHiddenKey);
        PlayerPrefs.Save();

        // 2) 清仓库文件
        var path = Path.Combine(Application.persistentDataPath, "warehouse_data.json");
        if (File.Exists(path)) File.Delete(path);

        // 3) 清运行时内存状态
        var inv = Object.FindFirstObjectByType<SampleInventory>();
        if (inv != null) inv.ClearInventory();

        var wm = Object.FindFirstObjectByType<WarehouseManager>();
        if (wm != null && wm.Storage != null)
        {
            wm.Storage.ClearStorage();
            wm.SaveWarehouseData();
        }

        // 4) 可选：刷新UI
        var ui = Object.FindFirstObjectByType<InventoryUISystem>();
        if (ui != null) ui.RefreshTools();

        var classRoom = GameObject.Find("ClassRoom");
        if (classRoom != null)
        {
            classRoom.SetActive(true);
            Debug.Log("[ProgressResetService] 已恢复ClassRoom显示");
        }

        Debug.Log("[ProgressResetService] 完成一键清档（PlayerPrefs + Warehouse + Runtime）");
    }
}

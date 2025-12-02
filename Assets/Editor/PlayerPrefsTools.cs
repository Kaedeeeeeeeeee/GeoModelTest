using UnityEditor;
using UnityEngine;

public static class PlayerPrefsTools
{
    private const string InventoryPrefsKey = "PlayerPersistentData.Inventory";
    private const string UnlockedToolsPrefsKey = "PlayerPersistentData.UnlockedToolIds";
    private const string CompletedQuestsKey = "QuestSystem.CompletedQuests";
    private const string CompletedObjectivesKey = "QuestSystem.CompletedObjectives";

    [MenuItem("Tools/Debug/Clear Inventory (PlayerPrefs)")]
    public static void ClearInventory()
    {
        PlayerPrefs.DeleteKey(InventoryPrefsKey);
        PlayerPrefs.Save();
        Debug.Log("Cleared inventory PlayerPrefs.");
    }

    [MenuItem("Tools/Debug/Clear Unlock Tools (PlayerPrefs)")]
    public static void ClearUnlockTools()
    {
        PlayerPrefs.DeleteKey(UnlockedToolsPrefsKey);
        PlayerPrefs.Save();
        Debug.Log("Cleared unlocked tools PlayerPrefs.");
    }

    [MenuItem("Tools/Debug/Clear Quests Progress (PlayerPrefs)")]
    public static void ClearQuests()
    {
        PlayerPrefs.DeleteKey(CompletedQuestsKey);
        PlayerPrefs.DeleteKey(CompletedObjectivesKey);
        PlayerPrefs.Save();
        Debug.Log("Cleared quest progress PlayerPrefs.");
    }

    [MenuItem("Tools/Debug/Clear Warehouse (File + Runtime)")]
    public static void ClearWarehouse()
    {
        // 删除持久化文件
        var path = System.IO.Path.Combine(Application.persistentDataPath, "warehouse_data.json");
        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
            Debug.Log($"Deleted warehouse file: {path}");
        }

        // 尝试清空运行时仓库
        var wm = UnityEngine.Object.FindFirstObjectByType<WarehouseManager>();
        if (wm != null && wm.Storage != null)
        {
            wm.Storage.ClearStorage();
            wm.SaveWarehouseData();
        }
        Debug.Log("Cleared warehouse storage (file + runtime).");
    }

    [MenuItem("Tools/Debug/Clear ALL Saves (PlayerPrefs + Warehouse)")]
    public static void ClearAllSaves()
    {
        // PlayerPrefs: inventory + unlock tools + quests
        PlayerPrefs.DeleteKey(InventoryPrefsKey);
        PlayerPrefs.DeleteKey(UnlockedToolsPrefsKey);
        PlayerPrefs.DeleteKey(CompletedQuestsKey);
        PlayerPrefs.DeleteKey(CompletedObjectivesKey);
        PlayerPrefs.Save();

        // Warehouse file + runtime
        ClearWarehouse();

        // 尝试清空运行时背包
        var inv = UnityEngine.Object.FindFirstObjectByType<SampleInventory>();
        if (inv != null) inv.ClearInventory();

        Debug.Log("Cleared ALL saves: PlayerPrefs + Warehouse + runtime caches.");
    }
}

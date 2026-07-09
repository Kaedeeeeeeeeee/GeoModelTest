using System;
using System.Collections.Generic;
using System.Linq;
using Encyclopedia;
using Newtonsoft.Json.Linq;
using QuestSystem;
using StorySystem;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Backend
{
    public static class ProgressSnapshotBuilder
    {
        private const string UnlockedToolsKey = "PlayerPersistentData.UnlockedToolIds";
        private const string InventoryKey = "PlayerPersistentData.Inventory";

        public static ProgressSnapshot Build(string currentSceneOverride = null)
        {
            var snapshot = new ProgressSnapshot
            {
                currentScene = string.IsNullOrEmpty(currentSceneOverride)
                    ? SceneManager.GetActiveScene().name
                    : currentSceneOverride,
                completedQuests = QuestPersistence.LoadCompletedQuests().OrderBy(v => v).ToList(),
                completedObjectives = QuestPersistence.LoadCompletedObjectives().OrderBy(v => v).ToList(),
                storyFlags = ProgressPersistence.LoadFlags().OrderBy(v => v).ToList(),
                unlockedToolIds = ReadPrefsList(UnlockedToolsKey, ','),
                updatedAt = DateTime.UtcNow.ToString("o")
            };

            var inventoryCounts = GetInventoryCounts();
            snapshot.inventoryCount = inventoryCounts.current;

            var warehouseCounts = GetWarehouseCounts();
            snapshot.warehouseCount = warehouseCounts.current;

            var encyclopediaCounts = GetEncyclopediaCounts();
            snapshot.encyclopediaDiscovered = encyclopediaCounts.discovered;
            snapshot.encyclopediaTotal = encyclopediaCounts.total;

            snapshot.payload["gameVersion"] = Application.version;
            snapshot.payload["platform"] = Application.platform.ToString();
            snapshot.payload["buildTarget"] = GetBuildTarget();
            snapshot.payload["language"] = Application.systemLanguage.ToString();
            snapshot.payload["inventoryMax"] = inventoryCounts.max;
            snapshot.payload["warehouseMax"] = warehouseCounts.max;
            snapshot.payload["encyclopedia"] = encyclopediaCounts.payload;

            return snapshot;
        }

        public static List<string> ReadPrefsList(string key, char separator)
        {
            string raw = PlayerPrefs.GetString(key, "");
            if (string.IsNullOrEmpty(raw))
            {
                return new List<string>();
            }

            return raw
                .Split(separator)
                .Select(v => v.Trim())
                .Where(v => !string.IsNullOrEmpty(v))
                .Distinct()
                .OrderBy(v => v)
                .ToList();
        }

        private static (int current, int max) GetInventoryCounts()
        {
            var inventory = SampleInventory.Instance;
            if (inventory != null)
            {
                var capacity = inventory.GetCapacityInfo();
                return (capacity.current, capacity.max);
            }

            return (CountJsonArrayItemsInPrefs(InventoryKey, "items"), 0);
        }

        private static (int current, int max) GetWarehouseCounts()
        {
            var warehouse = WarehouseManager.Instance;
            if (warehouse?.Storage != null)
            {
                var capacity = warehouse.Storage.GetCapacityInfo();
                return (capacity.current, capacity.max);
            }

            return (0, 0);
        }

        private static (int discovered, int total, Dictionary<string, object> payload) GetEncyclopediaCounts()
        {
            var payload = new Dictionary<string, object>();
            var collectionManager = CollectionManager.Instance;
            var stats = collectionManager != null ? collectionManager.CurrentStats : null;
            if (stats == null)
            {
                return (0, 0, payload);
            }

            payload["totalMinerals"] = stats.totalMinerals;
            payload["discoveredMinerals"] = stats.discoveredMinerals;
            payload["totalFossils"] = stats.totalFossils;
            payload["discoveredFossils"] = stats.discoveredFossils;
            payload["overallProgress"] = stats.overallProgress;

            return (stats.discoveredEntries, stats.totalEntries, payload);
        }

        private static int CountJsonArrayItemsInPrefs(string prefsKey, string arrayProperty)
        {
            string json = PlayerPrefs.GetString(prefsKey, "");
            if (string.IsNullOrEmpty(json))
            {
                return 0;
            }

            try
            {
                var parsed = JObject.Parse(json);
                return parsed[arrayProperty] is JArray items ? items.Count : 0;
            }
            catch
            {
                return 0;
            }
        }

        private static string GetBuildTarget()
        {
#if UNITY_WEBGL
            return "WebGL";
#elif UNITY_STANDALONE_OSX
            return "StandaloneOSX";
#elif UNITY_STANDALONE_WIN
            return "StandaloneWindows";
#else
            return Application.platform.ToString();
#endif
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace StorySystem.EditorTools
{
    public static class StoryTools
    {
        private const string UnlockedToolsPrefsKey = "PlayerPersistentData.UnlockedToolIds";
        private const string EncyclopediaDatabaseAssetPath = "Assets/Resources/MineralData/Data/SendaiMineralDatabase.json";
        private const string EncyclopediaProgressFileName = "EncyclopediaProgress.json";

        private static readonly string[] AllToolIds =
        {
            "999",
            "1000",
            "1001",
            "1002",
            "1100",
            "1101"
        };

        private static readonly string[] AllQuestIds =
        {
            "q.lab.intro",
            "q.lab.drkaede",
            "q.lab.anomaly",
            "q.field.phase",
            "q.lab.return",
            "q.chapter4.kaede",
            "q.chapter4.field",
            "q.chapter4.sample",
            "q.chapter4.return",
            "q.chapter5.kaede",
            "q.chapter5.field",
            "q.chapter5.return",
            "q.chapter6.kaede"
        };

        private static readonly string[] AllObjectiveIds =
        {
            "q.lab.intro.intro_done",
            "q.lab.drkaede.talk",
            "q.lab.anomaly.talk",
            "q.field.phase.enter_field",
            "q.field.phase.collect_samples",
            "q.lab.return.enter_lab",
            "q.chapter4.kaede.talk",
            "q.chapter4.field.enter_field",
            "q.chapter4.sample.collect",
            "q.chapter4.return.enter_lab",
            "q.chapter5.kaede.talk",
            "q.chapter5.field.enter_field",
            "q.chapter5.return.enter_lab",
            "q.chapter6.kaede.talk"
        };

        private static readonly string[] AllStoryFlags =
        {
            "story.main.rescue",
            "story.lab.intro",
            "story.field.phase_intro",
            "story.lab.return",
            "story.chapter4.sample_intro",
            "story.chapter4.return",
            "story.chapter5.field",
            "story.chapter5.return"
        };

        [MenuItem("Tools/Story/清除剧情标记 (StoryFlags)")]
        private static void ClearStoryFlags()
        {
            PlayerPrefs.DeleteKey("StoryFlags");
            PlayerPrefs.Save();
            Debug.Log("[StoryTools] 已清除 PlayerPrefs 中的 StoryFlags");
        }

        [MenuItem("Tools/Story/查看当前剧情标记")]
        private static void PrintStoryFlags()
        {
            var flags = ProgressPersistence.LoadFlags();
            Debug.Log($"[StoryTools] 当前剧情标记: {(flags.Count == 0 ? "<空>" : string.Join(", ", flags))}");
        }

        [MenuItem("Tools/Story/清除全部 PlayerPrefs (慎用)")]
        private static void ClearAllPlayerPrefs()
        {
            if (EditorUtility.DisplayDialog("清除全部 PlayerPrefs", "确认删除所有 PlayerPrefs？此操作不可撤销。", "确定", "取消"))
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                Debug.Log("[StoryTools] 已清除全部 PlayerPrefs");
            }
        }

        [MenuItem("Tools/Story/一键清档（重置为初次体验）")]
        private static void ResetToFirstPlay()
        {
            if (!EditorUtility.DisplayDialog("一键清档", "将清除剧情标记、工具解锁、背包、仓库、图鉴进度，并删除相关持久化文件。继续吗？", "确定", "取消"))
                return;

            // 统一调度：调用通用清档服务（清 PlayerPrefs + 仓库文件 + 运行时内存）
            ProgressResetService.ResetAll();

            // 额外：清剧情标记与图鉴（通用清档未覆盖的项目）
            PlayerPrefs.DeleteKey("StoryFlags");
            PlayerPrefs.Save();
            Debug.Log("[StoryTools] ✅ 已清除 StoryFlags");

            var encyclopediaSavePath = Path.Combine(Application.persistentDataPath, EncyclopediaProgressFileName);
            var collectionMgr = UnityEngine.Object.FindFirstObjectByType<Encyclopedia.CollectionManager>();
            if (collectionMgr != null)
            {
                collectionMgr.ResetProgress();
                collectionMgr.SaveProgress();
            }

            try
            {
                if (File.Exists(encyclopediaSavePath))
                {
                    File.Delete(encyclopediaSavePath);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[StoryTools] 删除图鉴存档失败: {e.Message}");
            }

            Debug.Log($"[StoryTools] ✅ 已重置图鉴进度并删除保存: {encyclopediaSavePath}");

            // 清放置样本
            try { PlacedSampleTracker.ClearAllPlacedSamples(); } catch {}

            EditorUtility.DisplayDialog("完成", "游戏已重置为初次体验状态。", "好的");
        }

        [MenuItem("Tools/Story/一键全解锁（工具与图鉴）")]
        private static void UnlockAllForDebug()
        {
            PersistUnlockedTools();
            PersistAllStoryProgress();
            int unlockedEntries = UnlockAllEncyclopediaEntries();

            if (Application.isPlaying)
            {
                ApplyRuntimeToolUnlocks();
                ApplyRuntimeCollectionUnlocks();
                Debug.LogWarning("[StoryTools] 当前处于 Play 模式：剧情/任务状态已写入持久化，但已创建的 QuestManager / StoryDirector 不会自动重载；如需立即看到最终剧情状态，请重新进入 Play 模式。");
            }

            EditorUtility.DisplayDialog("完成", $"已写入全解锁状态（剧情 / 工具 / 图鉴）。图鉴条目数：{unlockedEntries}", "好的");
        }

        private static void PersistUnlockedTools()
        {
            var unlockedTools = LoadCsvSet(UnlockedToolsPrefsKey);
            unlockedTools.UnionWith(AllToolIds);

            PlayerPrefs.SetString(UnlockedToolsPrefsKey, string.Join(",", unlockedTools));
            PlayerPrefs.Save();

            Debug.Log($"[StoryTools] ✅ 已持久化全部工具解锁: {string.Join(", ", unlockedTools)}");
        }

        private static void PersistAllStoryProgress()
        {
            var completedQuests = QuestSystem.QuestPersistence.LoadCompletedQuests();
            completedQuests.UnionWith(AllQuestIds);

            var completedObjectives = QuestSystem.QuestPersistence.LoadCompletedObjectives();
            completedObjectives.UnionWith(AllObjectiveIds);
            QuestSystem.QuestPersistence.SaveCompleted(completedQuests, completedObjectives);

            var storyFlags = ProgressPersistence.LoadFlags();
            storyFlags.UnionWith(AllStoryFlags);
            ProgressPersistence.SaveFlags(storyFlags);

            Debug.Log($"[StoryTools] ✅ 已持久化剧情进度: 任务 {completedQuests.Count} 个，目标 {completedObjectives.Count} 个，剧情标记 {storyFlags.Count} 个");
        }

        private static int UnlockAllEncyclopediaEntries()
        {
            if (Application.isPlaying)
            {
                var collectionMgr = UnityEngine.Object.FindFirstObjectByType<Encyclopedia.CollectionManager>();
                if (collectionMgr != null)
                {
                    collectionMgr.UnlockAllEntries();
                    collectionMgr.SaveProgress();
                    return collectionMgr.CurrentStats?.discoveredEntries ?? 0;
                }

                Debug.Log("[StoryTools] 当前场景未初始化 CollectionManager，改为直接写入图鉴存档");
            }

            var databaseAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(EncyclopediaDatabaseAssetPath);
            if (databaseAsset == null)
            {
                Debug.LogError($"[StoryTools] 无法找到图鉴数据库资源: {EncyclopediaDatabaseAssetPath}");
                return 0;
            }

            var database = JsonUtility.FromJson<Encyclopedia.DatabaseRoot>(databaseAsset.text);
            if (database?.stratigraphicLayers == null)
            {
                Debug.LogError("[StoryTools] 图鉴数据库解析失败，无法写入全解锁图鉴存档");
                return 0;
            }

            var saveData = new Encyclopedia.CollectionSaveData
            {
                lastSaveTime = DateTime.Now
            };

            foreach (var layer in database.stratigraphicLayers)
            {
                if (layer == null)
                {
                    continue;
                }

                if (layer.rockTypes != null)
                {
                    foreach (var rock in layer.rockTypes)
                    {
                        if (rock?.minerals == null)
                        {
                            continue;
                        }

                        foreach (var mineral in rock.minerals)
                        {
                            if (mineral == null)
                            {
                                continue;
                            }

                            saveData.discoveredEntries.Add(new Encyclopedia.DiscoveredEntry
                            {
                                entryId = $"{layer.layerId}_{rock.rockId}_{mineral.mineralId}",
                                firstDiscoveredTime = DateTime.Now,
                                discoveryCount = 1
                            });
                        }
                    }
                }

                if (layer.fossils == null)
                {
                    continue;
                }

                foreach (var fossil in layer.fossils)
                {
                    if (fossil == null)
                    {
                        continue;
                    }

                    saveData.discoveredEntries.Add(new Encyclopedia.DiscoveredEntry
                    {
                        entryId = $"{layer.layerId}_{fossil.fossilId}",
                        firstDiscoveredTime = DateTime.Now,
                        discoveryCount = 1
                    });
                }
            }

            var savePath = Path.Combine(Application.persistentDataPath, EncyclopediaProgressFileName);
            File.WriteAllText(savePath, JsonUtility.ToJson(saveData, true));
            Debug.Log($"[StoryTools] ✅ 已写入图鉴全解锁存档，共 {saveData.discoveredEntries.Count} 个条目 -> {savePath}");
            return saveData.discoveredEntries.Count;
        }

        private static void ApplyRuntimeToolUnlocks()
        {
            var toolManager = UnityEngine.Object.FindFirstObjectByType<ToolManager>();
            if (toolManager == null)
            {
                Debug.Log("[StoryTools] 当前场景未找到 ToolManager，工具解锁已写入持久化，将在下次初始化时生效");
                return;
            }

            if (toolManager.GetComponent<SceneSwitcherTool>() == null)
            {
                toolManager.gameObject.AddComponent<SceneSwitcherTool>();
            }

            foreach (var toolId in AllToolIds)
            {
                if (toolId == "1101")
                {
                    UnlockDrillCarTool(toolManager);
                    continue;
                }

                ToolUnlockService.UnlockToolById(toolId);
            }

            var tools = toolManager.GetComponents<CollectionTool>();
            foreach (var tool in tools)
            {
                if (tool != null)
                {
                    toolManager.AddTool(tool);
                }
            }

            var ui = UnityEngine.Object.FindFirstObjectByType<InventoryUISystem>();
            if (ui != null)
            {
                ui.RefreshTools();
            }

            Debug.Log($"[StoryTools] ✅ 已同步当前场景工具解锁，共 {tools.Length} 个组件");
        }

        private static void ApplyRuntimeCollectionUnlocks()
        {
            var collectionMgr = UnityEngine.Object.FindFirstObjectByType<Encyclopedia.CollectionManager>();
            if (collectionMgr == null)
            {
                return;
            }

            collectionMgr.UnlockAllEntries();
            collectionMgr.SaveProgress();
            Debug.Log("[StoryTools] ✅ 已同步当前场景图鉴全解锁");
        }

        private static void UnlockDrillCarTool(ToolManager toolManager)
        {
            if (toolManager == null)
            {
                return;
            }

            var drillCarTool = toolManager.GetComponent<DrillCarTool>();
            if (drillCarTool == null)
            {
                drillCarTool = toolManager.gameObject.AddComponent<DrillCarTool>();
                drillCarTool.toolID = "1101";
                drillCarTool.toolName = "钻探车";
            }

            toolManager.AddTool(drillCarTool);
        }

        private static HashSet<string> LoadCsvSet(string key)
        {
            var set = new HashSet<string>();
            var raw = PlayerPrefs.GetString(key, string.Empty);
            if (string.IsNullOrEmpty(raw))
            {
                return set;
            }

            var parts = raw.Split(',');
            foreach (var part in parts)
            {
                if (!string.IsNullOrWhiteSpace(part))
                {
                    set.Add(part.Trim());
                }
            }

            return set;
        }
    }
}

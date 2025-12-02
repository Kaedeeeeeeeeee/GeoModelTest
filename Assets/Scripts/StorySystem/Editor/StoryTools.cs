using UnityEditor;
using UnityEngine;

namespace StorySystem.EditorTools
{
    public static class StoryTools
    {
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

            var collectionMgr = Object.FindFirstObjectByType<Encyclopedia.CollectionManager>();
            if (collectionMgr != null)
            {
                collectionMgr.ResetProgress();
                collectionMgr.SaveProgress();
                var path = System.IO.Path.Combine(Application.persistentDataPath, "EncyclopediaProgress.json");
                try { if (System.IO.File.Exists(path)) System.IO.File.Delete(path); } catch {}
                Debug.Log($"[StoryTools] ✅ 已重置图鉴进度并删除保存: {path}");
            }

            // 清放置样本
            try { PlacedSampleTracker.ClearAllPlacedSamples(); } catch {}

            EditorUtility.DisplayDialog("完成", "游戏已重置为初次体验状态。", "好的");
        }

        [MenuItem("Tools/Story/一键全解锁（工具与图鉴）")]
        private static void UnlockAllForDebug()
        {
            // 1) 工具：将玩家身上的所有 CollectionTool 注册到 ToolManager
            var toolManager = Object.FindFirstObjectByType<ToolManager>();
            if (toolManager != null)
            {
                // 确保场景切换器组件存在
                var sceneSwitcher = toolManager.GetComponent<SceneSwitcherTool>();
                if (sceneSwitcher == null)
                {
                    sceneSwitcher = toolManager.gameObject.AddComponent<SceneSwitcherTool>();
                }

                var tools = toolManager.GetComponents<CollectionTool>();
                foreach (var t in tools)
                {
                    if (t != null) toolManager.AddTool(t);
                }

                var ui = Object.FindFirstObjectByType<InventoryUISystem>();
                if (ui != null) ui.RefreshTools();
                Debug.Log($"[StoryTools] ✅ 已解锁所有工具，共 {tools.Length} 个");
            }
            else
            {
                Debug.LogWarning("[StoryTools] 未找到 ToolManager");
            }

            // 2) 图鉴：解锁全部
            var collectionMgr = Object.FindFirstObjectByType<Encyclopedia.CollectionManager>();
            if (collectionMgr != null)
            {
                collectionMgr.UnlockAllEntries();
                collectionMgr.SaveProgress();
                Debug.Log("[StoryTools] ✅ 已解锁所有图鉴条目");
            }
            else
            {
                Debug.LogWarning("[StoryTools] 未找到 CollectionManager");
            }

            // 3) 任务：直接标记首个任务完成并发放奖励
            var qm = QuestSystem.QuestManager.Instance;
            qm.StartQuest("q.lab.intro");
            qm.CompleteObjective("q.lab.intro.intro_done");

            EditorUtility.DisplayDialog("完成", "已解锁全部工具与图鉴内容。", "好的");
        }
    }
}

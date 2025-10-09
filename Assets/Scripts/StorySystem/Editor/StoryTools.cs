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
    }
}


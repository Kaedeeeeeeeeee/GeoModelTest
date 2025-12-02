using System.Collections.Generic;
using UnityEngine;

namespace QuestSystem
{
    /// <summary>
    /// 极简任务持久化：使用 PlayerPrefs 存储已完成任务与目标。
    /// </summary>
    public static class QuestPersistence
    {
        private const string CompletedQuestsKey = "QuestSystem.CompletedQuests";
        private const string CompletedObjectivesKey = "QuestSystem.CompletedObjectives";
        private const char Sep = '|';

        public static HashSet<string> LoadCompletedQuests()
        {
            return ParseSet(PlayerPrefs.GetString(CompletedQuestsKey, ""));
        }

        public static HashSet<string> LoadCompletedObjectives()
        {
            return ParseSet(PlayerPrefs.GetString(CompletedObjectivesKey, ""));
        }

        public static void SaveCompleted(HashSet<string> quests, HashSet<string> objectives)
        {
            if (quests != null) PlayerPrefs.SetString(CompletedQuestsKey, string.Join(Sep, quests));
            if (objectives != null) PlayerPrefs.SetString(CompletedObjectivesKey, string.Join(Sep, objectives));
            PlayerPrefs.Save();
        }

        public static void ClearAll()
        {
            PlayerPrefs.DeleteKey(CompletedQuestsKey);
            PlayerPrefs.DeleteKey(CompletedObjectivesKey);
            PlayerPrefs.Save();
        }

        private static HashSet<string> ParseSet(string raw)
        {
            var set = new HashSet<string>();
            if (!string.IsNullOrEmpty(raw))
            {
                var parts = raw.Split(Sep);
                foreach (var p in parts)
                    if (!string.IsNullOrEmpty(p)) set.Add(p);
            }
            return set;
        }
    }
}


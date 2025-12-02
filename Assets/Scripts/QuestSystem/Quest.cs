using System.Collections.Generic;

namespace QuestSystem
{
    public enum QuestStatus
    {
        NotStarted,
        InProgress,
        Completed,
        RewardClaimed
    }

    public class QuestObjective
    {
        public string id;               // 唯一ID: 例如 q.lab.intro.intro_done
        public string titleKey;         // 本地化键
        public bool completed;
    }

    public class Quest
    {
        public string id;               // 任务ID: q.lab.intro
        public string titleKey;         // 本地化键
        public string descriptionKey;   // 本地化键
        public List<QuestObjective> objectives = new List<QuestObjective>();
        public QuestStatus status = QuestStatus.NotStarted;

        public bool IsAllObjectivesCompleted()
        {
            if (objectives == null || objectives.Count == 0) return false;
            foreach (var o in objectives)
                if (!o.completed) return false;
            return true;
        }
    }
}


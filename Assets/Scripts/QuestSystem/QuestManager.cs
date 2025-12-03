using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Core;
using GuidanceSystem;
using StorySystem;
using UnityEngine.SceneManagement;

namespace QuestSystem
{
    /// <summary>
    /// 任务管理器（MVP）：注册内置任务、监听关键事件并发放奖励。
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        private static QuestManager _instance;
        public static QuestManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("QuestManager");
                    _instance = go.AddComponent<QuestManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private readonly Dictionary<string, Quest> _quests = new Dictionary<string, Quest>();
        private HashSet<string> _completedQuests;
        private HashSet<string> _completedObjectives;
        [SerializeField] private bool debugLog = true;
        [SerializeField] private bool autoStartIntroQuestIfNone = true;
        [SerializeField] private string[] uiAllowedScenes = new[] { "MainScene", "Laboratory Scene" };

        // 内部等待绑定控制
        private bool _waitingForSampleBind = false;
        private Coroutine _sampleBindRoutine;
        private bool _chapter4SampleIntroPlayed = false;
        private bool _chapter4SampleCutscenePending = false;
        private bool _chapter4SampleCutscenePlayed = false;
        private bool _chapter4FieldIntroPending = false;
        private bool _fieldPhaseSampleCutscenePending = false;
        private int _fieldPhaseTargetIndex = 0;
        private GuidanceTarget _chapter4SampleGuidanceTarget;

        private static readonly string[] FieldPhaseTargetSequence =
        {
            "chapter3.field.sample_site_a",
            "chapter3.field.sample_site_b",
            "chapter3.field.sample_site_c"
        };

        private static readonly FieldInfo GuidanceTargetIdField = typeof(GuidanceTarget)
            .GetField("targetId", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo GuidanceTargetOffsetField = typeof(GuidanceTarget)
            .GetField("verticalOffset", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo GuidanceTargetRadiusField = typeof(GuidanceTarget)
            .GetField("detectionRadius", BindingFlags.Instance | BindingFlags.NonPublic);

        private const string Chapter4FieldStoryPath = "Story/quest4.2";
        private const string Chapter4SampleCompletionStoryPath = "Story/quest4.3";
        private const string Chapter4ReturnStoryPath = "Story/quest4.4";
        private const string Chapter4SampleGuidanceTargetId = "chapter4.sample.site";
        private static readonly Vector3 Chapter4SampleTargetPosition = new Vector3(-10f, 14f, -28f);
        private const string FieldPhaseSampleStoryPath = "Story/quest3.3";

        // 事件（供UI订阅）
        public System.Action<Quest> OnQuestStarted;
        public System.Action<QuestObjective> OnObjectiveCompleted;
        public System.Action<Quest> OnQuestCompleted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            _ = Instance; // 确保实例存在
        }

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            _completedQuests = QuestPersistence.LoadCompletedQuests();
            _completedObjectives = QuestPersistence.LoadCompletedObjectives();
            var storyFlags = ProgressPersistence.LoadFlags();
            _chapter4SampleIntroPlayed = storyFlags.Contains("story.chapter4.sample_intro");

            RegisterBuiltInQuests();

            // 若当前没有任何进行中或已完成的任务，并且是允许显示任务的场景，自动启动引导任务
            if (autoStartIntroQuestIfNone && IsAllowedScene(SceneManager.GetActiveScene().name))
            {
                bool hasActiveOrCompleted = false;
                foreach (var kv in _quests)
                {
                    if (kv.Value.status == QuestStatus.InProgress || kv.Value.status == QuestStatus.Completed)
                    {
                        hasActiveOrCompleted = true;
                        break;
                    }
                }

                if (!hasActiveOrCompleted)
                {
                    if (debugLog) Debug.Log("[QuestManager] 无活动任务，自动启动 q.lab.intro（允许场景）");
                    StartQuest("q.lab.intro");
                }
            }

            // 监听关键事件（样本采集、场景切换）
            GameEventBus.SceneLoaded += OnSceneLoaded;
            SceneManager.sceneLoaded += OnUnitySceneLoaded; // 使用Unity原生事件兜底，避免错过自定义事件
            TryBindSampleEvents();

            // 确保任务UI存在
            if (FindFirstObjectByType<QuestUI>() == null)
            {
                var uiGO = new GameObject("QuestUI");
                uiGO.AddComponent<QuestUI>();
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                GameEventBus.SceneLoaded -= OnSceneLoaded;
                SceneManager.sceneLoaded -= OnUnitySceneLoaded;
                UnbindSampleEvents();
            }
        }

        private void RegisterBuiltInQuests()
        {
            // Q1：实验室开场 → 发放工具（锤子+场景切换器）
            var q1 = new Quest
            {
                id = "q.lab.intro",
                titleKey = "quest.lab.intro.title",
                descriptionKey = "quest.lab.intro.desc",
                status = QuestStatus.NotStarted,
                objectives = new List<QuestObjective>
                {
                    new QuestObjective { id = "q.lab.intro.intro_done", titleKey = "quest.lab.intro.obj1" }
                }
            };
            _quests[q1.id] = q1;

            // 根据持久化修正状态
            RestoreQuestRuntimeState(q1);

            // Q2：与Dr. Kaede对话
            var q2 = new Quest
            {
                id = "q.lab.drkaede",
                titleKey = "quest.lab.drkaede.title",
                descriptionKey = "quest.lab.drkaede.desc",
                status = QuestStatus.NotStarted,
                objectives = new List<QuestObjective>
                {
                    new QuestObjective
                    {
                        id = "q.lab.drkaede.talk",
                        titleKey = "quest.lab.drkaede.obj1"
                    }
                }
            };
            _quests[q2.id] = q2;

            RestoreQuestRuntimeState(q2);

            // Q3：异常样本
            var q3 = new Quest
            {
                id = "q.lab.anomaly",
                titleKey = "quest.lab.anomaly.title",
                descriptionKey = "quest.lab.anomaly.desc",
                status = QuestStatus.NotStarted,
                objectives = new List<QuestObjective>
                {
                    new QuestObjective
                    {
                        id = "q.lab.anomaly.talk",
                        titleKey = "quest.lab.anomaly.obj1"
                    }
                }
            };
            _quests[q3.id] = q3;

            RestoreQuestRuntimeState(q3);

            if (debugLog) Debug.Log("[QuestManager] 内置任务已注册：q.lab.intro");
            if (debugLog) Debug.Log("[QuestManager] 内置任务已注册：q.lab.drkaede");
            if (debugLog) Debug.Log("[QuestManager] 内置任务已注册：q.lab.anomaly");

            if (q1.status == QuestStatus.Completed && q2.status == QuestStatus.NotStarted)
            {
                if (debugLog) Debug.Log("[QuestManager] 检测到q.lab.intro已完成，自动启动q.lab.drkaede");
                StartQuest(q2.id);
            }

            if (q2.status == QuestStatus.Completed && q3.status == QuestStatus.NotStarted)
            {
                if (debugLog) Debug.Log("[QuestManager] 检测到q.lab.drkaede已完成，自动启动q.lab.anomaly");
                StartQuest(q3.id);
            }

            // Q4：使用场景切换器前往野外
            var q4 = new Quest
            {
                id = "q.field.phase",
                titleKey = "quest.field.phase.title",
                descriptionKey = "quest.field.phase.desc",
                status = QuestStatus.NotStarted,
                objectives = new List<QuestObjective>
                {
                    new QuestObjective
                    {
                        id = "q.field.phase.enter_field",
                        titleKey = "quest.field.phase.obj1"
                    },
                    new QuestObjective
                    {
                        id = "q.field.phase.collect_samples",
                        titleKey = "quest.field.phase.obj2"
                    }
                }
            };
            _quests[q4.id] = q4;

            RestoreQuestRuntimeState(q4);
            if (debugLog) Debug.Log("[QuestManager] 内置任务已注册：q.field.phase");

            if (q3.status == QuestStatus.Completed && q4.status == QuestStatus.NotStarted)
            {
                if (debugLog) Debug.Log("[QuestManager] 检测到q.lab.anomaly已完成，自动启动q.field.phase");
                StartQuest(q4.id);
            }

            var q5 = new Quest
            {
                id = "q.lab.return",
                titleKey = "quest.lab.return.title",
                descriptionKey = "quest.lab.return.desc",
                status = QuestStatus.NotStarted,
                objectives = new List<QuestObjective>
                {
                    new QuestObjective
                    {
                        id = "q.lab.return.enter_lab",
                        titleKey = "quest.lab.return.obj1"
                    }
                }
            };
            _quests[q5.id] = q5;
            RestoreQuestRuntimeState(q5);
            if (debugLog) Debug.Log("[QuestManager] 内置任务已注册：q.lab.return");

            if (q4.status == QuestStatus.Completed && q5.status == QuestStatus.NotStarted)
            {
                if (debugLog) Debug.Log("[QuestManager] 检测到q.field.phase已完成，自动启动q.lab.return");
                StartQuest(q5.id);
            }

            var q6 = new Quest
            {
                id = "q.chapter4.kaede",
                titleKey = "quest.chapter4.kaede.title",
                descriptionKey = "quest.chapter4.kaede.desc",
                status = QuestStatus.NotStarted,
                objectives = new List<QuestObjective>
                {
                    new QuestObjective
                    {
                        id = "q.chapter4.kaede.talk",
                        titleKey = "quest.chapter4.kaede.obj1"
                    }
                }
            };
            _quests[q6.id] = q6;
            RestoreQuestRuntimeState(q6);
            if (debugLog) Debug.Log("[QuestManager] 内置任务已注册：q.chapter4.kaede");

            var qField4 = new Quest
            {
                id = "q.chapter4.field",
                titleKey = "quest.chapter4.field.title",
                descriptionKey = "quest.chapter4.field.desc",
                status = QuestStatus.NotStarted,
                objectives = new List<QuestObjective>
                {
                    new QuestObjective
                    {
                        id = "q.chapter4.field.enter_field",
                        titleKey = "quest.chapter4.field.obj1"
                    }
                }
            };
            _quests[qField4.id] = qField4;
            RestoreQuestRuntimeState(qField4);
            if (debugLog) Debug.Log("[QuestManager] 内置任务已注册：q.chapter4.field");

            var q7 = new Quest
            {
                id = "q.chapter4.sample",
                titleKey = "quest.chapter4.sample.title",
                descriptionKey = "quest.chapter4.sample.desc",
                status = QuestStatus.NotStarted,
                objectives = new List<QuestObjective>
                {
                    new QuestObjective
                    {
                        id = "q.chapter4.sample.collect",
                        titleKey = "quest.chapter4.sample.obj1"
                    }
                }
            };
            _quests[q7.id] = q7;
            RestoreQuestRuntimeState(q7);
            if (debugLog) Debug.Log("[QuestManager] 内置任务已注册：q.chapter4.sample");

            if (q7.IsAllObjectivesCompleted() || q7.status == QuestStatus.Completed)
            {
                _chapter4SampleCutscenePlayed = true;
                // _chapter4SampleIntroPlayed = true; // Deprecated
            }

            var q8 = new Quest
            {
                id = "q.chapter4.return",
                titleKey = "quest.chapter4.return.title",
                descriptionKey = "quest.chapter4.return.desc",
                status = QuestStatus.NotStarted,
                objectives = new List<QuestObjective>
                {
                    new QuestObjective
                    {
                        id = "q.chapter4.return.enter_lab",
                        titleKey = "quest.chapter4.return.obj1"
                    }
                }
            };
            _quests[q8.id] = q8;
            RestoreQuestRuntimeState(q8);
            if (debugLog) Debug.Log("[QuestManager] 内置任务已注册：q.chapter4.return");

            if (q5.status == QuestStatus.Completed && q6.status == QuestStatus.NotStarted)
            {
                if (debugLog) Debug.Log("[QuestManager] 检测到q.lab.return已完成，自动启动q.chapter4.kaede");
                StartQuest(q6.id);
            }

            if (q6.status == QuestStatus.Completed && qField4.status == QuestStatus.NotStarted)
            {
                if (debugLog) Debug.Log("[QuestManager] 检测到q.chapter4.kaede已完成，自动启动q.chapter4.field");
                StartQuest(qField4.id);
            }

            if (qField4.status == QuestStatus.Completed && q7.status == QuestStatus.NotStarted)
            {
                if (debugLog) Debug.Log("[QuestManager] 检测到q.chapter4.field已完成，自动启动q.chapter4.sample");
                _chapter4SampleCutscenePlayed = false;
                _chapter4SampleCutscenePending = false;
                StartQuest(q7.id);
            }

            if (q7.status == QuestStatus.Completed && q8.status == QuestStatus.NotStarted)
            {
                if (debugLog) Debug.Log("[QuestManager] 检测到q.chapter4.sample已完成，自动启动q.chapter4.return");
                StartQuest(q8.id);
            }

            var q9 = new Quest
            {
                id = "q.chapter5.kaede",
                titleKey = "quest.chapter5.kaede.title",
                descriptionKey = "quest.chapter5.kaede.desc",
                status = QuestStatus.NotStarted,
                objectives = new List<QuestObjective>
                {
                    new QuestObjective
                    {
                        id = "q.chapter5.kaede.talk",
                        titleKey = "quest.chapter5.kaede.obj1"
                    }
                }
            };
            _quests[q9.id] = q9;
            RestoreQuestRuntimeState(q9);
            if (debugLog) Debug.Log("[QuestManager] 内置任务已注册：q.chapter5.kaede");

            var q10 = new Quest
            {
                id = "q.chapter5.field",
                titleKey = "quest.chapter5.field.title",
                descriptionKey = "quest.chapter5.field.desc",
                status = QuestStatus.NotStarted,
                objectives = new List<QuestObjective>
                {
                    new QuestObjective
                    {
                        id = "q.chapter5.field.enter_field",
                        titleKey = "quest.chapter5.field.obj1"
                    }
                }
            };
            _quests[q10.id] = q10;
            RestoreQuestRuntimeState(q10);
            if (debugLog) Debug.Log("[QuestManager] 内置任务已注册：q.chapter5.field");

            var q11 = new Quest
            {
                id = "q.chapter5.return",
                titleKey = "quest.chapter5.return.title",
                descriptionKey = "quest.chapter5.return.desc",
                status = QuestStatus.NotStarted,
                objectives = new List<QuestObjective>
                {
                    new QuestObjective
                    {
                        id = "q.chapter5.return.enter_lab",
                        titleKey = "quest.chapter5.return.obj1"
                    }
                }
            };
            _quests[q11.id] = q11;
            RestoreQuestRuntimeState(q11);
            if (debugLog) Debug.Log("[QuestManager] 内置任务已注册：q.chapter5.return");

            var q12 = new Quest
            {
                id = "q.chapter6.kaede",
                titleKey = "quest.chapter6.kaede.title",
                descriptionKey = "quest.chapter6.kaede.desc",
                status = QuestStatus.NotStarted,
                objectives = new List<QuestObjective>
                {
                    new QuestObjective
                    {
                        id = "q.chapter6.kaede.talk",
                        titleKey = "quest.chapter6.kaede.obj1"
                    }
                }
            };
            _quests[q12.id] = q12;
            RestoreQuestRuntimeState(q12);
            if (debugLog) Debug.Log("[QuestManager] 内置任务已注册：q.chapter6.kaede");

            if (q8.status == QuestStatus.Completed && q9.status == QuestStatus.NotStarted)
            {
                if (debugLog) Debug.Log("[QuestManager] 检测到q.chapter4.return已完成，自动启动q.chapter5.kaede");
                StartQuest(q9.id);
            }

            if (q9.status == QuestStatus.Completed && q10.status == QuestStatus.NotStarted)
            {
                if (debugLog) Debug.Log("[QuestManager] 检测到q.chapter5.kaede已完成，自动启动q.chapter5.field");
                StartQuest(q10.id);
            }

            if (q10.status == QuestStatus.Completed && q11.status == QuestStatus.NotStarted)
            {
                if (debugLog) Debug.Log("[QuestManager] 检测到q.chapter5.field已完成，自动启动q.chapter5.return");
                StartQuest(q11.id);
            }

            if (q11.status == QuestStatus.Completed && q12.status == QuestStatus.NotStarted)
            {
                if (debugLog) Debug.Log("[QuestManager] 检测到q.chapter5.return已完成，自动启动q.chapter6.kaede");
                StartQuest(q12.id);
            }
        }

        private void RestoreQuestRuntimeState(Quest q)
        {
            if (_completedQuests.Contains(q.id))
            {
                q.status = QuestStatus.Completed; // 奖励可能已发放或未发放，在MVP中视为完成
            }

            foreach (var obj in q.objectives)
            {
                obj.completed = _completedObjectives.Contains(obj.id);
            }
        }

        private void TryBindSampleEvents()
        {
            var inv = SampleInventory.Instance;
            if (inv != null)
            {
                inv.OnSampleAdded += OnSampleAdded;
                if (debugLog) Debug.Log("[QuestManager] 已绑定 SampleInventory.OnSampleAdded 事件");
                return;
            }

            if (_waitingForSampleBind) return;
            _waitingForSampleBind = true;
            if (debugLog) Debug.Log("[QuestManager] 等待样本背包初始化…");
            _sampleBindRoutine = StartCoroutine(WaitAndBindSampleEvents());
        }

        private System.Collections.IEnumerator WaitAndBindSampleEvents()
        {
            int tries = 0;
            const int maxTries = 30; // 最多等待约15秒（见延迟）
            while (SampleInventory.Instance == null && tries < maxTries)
            {
                tries++;
                yield return new WaitForSeconds(0.5f);
            }

            _waitingForSampleBind = false;
            var inv = SampleInventory.Instance;
            if (inv != null)
            {
                inv.OnSampleAdded += OnSampleAdded;
                if (debugLog) Debug.Log("[QuestManager] 已绑定 SampleInventory.OnSampleAdded 事件 (延迟)");
            }
            else if (debugLog)
            {
                Debug.LogWarning("[QuestManager] 超时，未找到 SampleInventory 实例，跳过事件绑定");
            }
        }

        private void UnbindSampleEvents()
        {
            var inv = SampleInventory.Instance;
            if (inv != null) inv.OnSampleAdded -= OnSampleAdded;
        }

        // 公开API --------------------------------------------------------------
        public void StartQuest(string questId)
        {
            if (!_quests.TryGetValue(questId, out var q)) return;
            if (q.status == QuestStatus.NotStarted)
            {
                q.status = QuestStatus.InProgress;
                if (debugLog) Debug.Log($"[QuestManager] 任务开始: {questId}");
                OnQuestStarted?.Invoke(q);

                if (questId == "q.field.phase")
                {
                    _fieldPhaseTargetIndex = 0;
                    _fieldPhaseSampleCutscenePending = false;
                    GuidanceManager.Instance?.ClearTarget();
                }
            }
        }

        public void CompleteObjective(string objectiveId)
        {
            // 找到拥有该objective的任务
            foreach (var kv in _quests)
            {
                var q = kv.Value;
                var obj = q.objectives.Find(o => o.id == objectiveId);
                if (obj == null) continue;

                if (!obj.completed)
                {
                    obj.completed = true;
                    _completedObjectives.Add(objectiveId);
                    QuestPersistence.SaveCompleted(_completedQuests, _completedObjectives);
                    if (debugLog) Debug.Log($"[QuestManager] 目标完成: {objectiveId}");
                    OnObjectiveCompleted?.Invoke(obj);

                    if (objectiveId == "q.field.phase.enter_field")
                    {
                        _fieldPhaseTargetIndex = 0;
                        ActivateCurrentFieldTarget();
                    }
                    else if (objectiveId == "q.field.phase.collect_samples")
                    {
                        GuidanceManager.Instance?.ClearTarget();
                    }
                }

                if (q.IsAllObjectivesCompleted() && q.status != QuestStatus.Completed)
                {
                    q.status = QuestStatus.Completed;
                    _completedQuests.Add(q.id);
                    QuestPersistence.SaveCompleted(_completedQuests, _completedObjectives);
                    if (debugLog) Debug.Log($"[QuestManager] 任务完成: {q.id}");
                    OnQuestCompleted?.Invoke(q);
                    GrantRewards(q.id);
                }
                return;
            }

            if (debugLog) Debug.LogWarning($"[QuestManager] 未找到目标: {objectiveId}");
        }

        // 事件监听 ------------------------------------------------------------
        private void OnSceneLoaded(string sceneName)
        {
            // 可用于后续任务的“返回实验室”判断
            if (debugLog) Debug.Log($"[QuestManager] OnSceneLoaded: {sceneName}");

            // 切换到允许场景且当前无任务时兜底启动
            if (autoStartIntroQuestIfNone && IsAllowedScene(sceneName))
            {
                bool hasActiveOrCompleted = false;
                foreach (var kv in _quests)
                {
                    if (kv.Value.status == QuestStatus.InProgress || kv.Value.status == QuestStatus.Completed)
                    {
                        hasActiveOrCompleted = true;
                        break;
                    }
                }
                if (!hasActiveOrCompleted)
                {
                    if (debugLog) Debug.Log("[QuestManager] 切到允许场景且无任务，自动启动 q.lab.intro");
                    StartQuest("q.lab.intro");
                }
            }

            if (sceneName == "MainScene")
            {
                // Quest 4.2 Logic: Enter Field -> Play Story -> Complete Quest -> Start 4.3
                if (GetQuestStatus("q.chapter4.field") == QuestStatus.InProgress &&
                    !IsObjectiveCompleted("q.chapter4.field.enter_field") &&
                    !_chapter4FieldIntroPending)
                {
                    _chapter4FieldIntroPending = true;
                    // Play quest4.2 story, then unlock 4.3 sampling
                    System.Action afterFieldIntro = () =>
                    {
                        if (!IsObjectiveCompleted("q.chapter4.field.enter_field"))
                        {
                            CompleteObjective("q.chapter4.field.enter_field");
                        }

                        if (GetQuestStatus("q.chapter4.sample") == QuestStatus.NotStarted)
                        {
                            StartQuest("q.chapter4.sample");
                        }

                        StorySystem.StoryDirector.Instance?.MarkChapter4FieldIntroPlayed();
                        MarkChapter4SampleIntroPlayed();
                        _chapter4FieldIntroPending = false;

                        // Fix: Use coroutine to delay refresh, avoiding race conditions and assertion errors
                        StartCoroutine(DelayedRefreshAfterCutscene());
                    };

                    var director = StorySystem.StoryDirector.Instance;
                    if (director != null)
                    {
                        director.PlaySequence(Chapter4FieldStoryPath, afterFieldIntro);
                    }
                    else
                    {
                        afterFieldIntro.Invoke();
                    }
                }
                
                // Quest 4.3 Guidance Logic
                if (GetQuestStatus("q.chapter4.sample") == QuestStatus.InProgress &&
                    !IsObjectiveCompleted("q.chapter4.sample.collect"))
                {
                    ActivateChapter4SampleGuidance();
                    // Ensure tools are refreshed here too, just in case state is lost
                    StartCoroutine(DelayedRefreshAfterCutscene());
                }
            }
            else if (sceneName == "Laboratory Scene")
            {
                // Fix: Inject Quest 4.2 (q.chapter4.field) before Quest 4.3 (q.chapter4.sample)
                // This ensures that completing 4.1 triggers 4.2 instead of 4.3
                var interactions = Object.FindObjectsByType<QuestNpcInteraction>(FindObjectsSortMode.None);
                foreach (var interaction in interactions)
                {
                    // We assume the interaction with 4.3 is Dr. Kaede
                    // We try to inject 4.2 before 4.3
                    interaction.InjectStageBefore(
                        "q.chapter4.sample", 
                        "q.chapter4.field", 
                        null, // No objective completion needed here, handled by OnSceneLoaded
                        null, // No story here, handled by OnSceneLoaded
                        "q.chapter4.kaede" // Prerequisite for 4.2 is 4.1
                    );
                }

                // Quest 4.4 Logic: Return to Lab -> Play Story -> Complete Quest
                if (GetQuestStatus("q.chapter4.return") == QuestStatus.InProgress &&
                    !IsObjectiveCompleted("q.chapter4.return.enter_lab"))
                {
                    var director = StorySystem.StoryDirector.Instance;
                    if (director != null)
                    {
                        director.PlaySequence(Chapter4ReturnStoryPath, () =>
                        {
                            CompleteObjective("q.chapter4.return.enter_lab");
                        });
                    }
                    else
                    {
                        CompleteObjective("q.chapter4.return.enter_lab");
                    }
                }
            }
        }

        // Unity原生场景事件 → 转发到统一处理，保证从StartScene进入也能触发兜底逻辑
        private void OnUnitySceneLoaded(Scene scene, LoadSceneMode mode)
        {
            OnSceneLoaded(scene.name);
        }

        private void OnSampleAdded(SampleItem sample)
        {
            // 预留：用于后续采样类任务
            if (sample == null) return;
            if (debugLog)
                Debug.Log($"[QuestManager] OnSampleAdded: {sample.displayName}, sourceTool={sample.sourceToolID}");

            HandleFieldPhaseSamplingProgress(sample);

            var sampleQuestStatus = GetQuestStatus("q.chapter4.sample");
            if (sampleQuestStatus == QuestStatus.InProgress && !_chapter4SampleCutscenePending && !_chapter4SampleCutscenePlayed)
            {
                _chapter4SampleCutscenePending = true;
                var director = StorySystem.StoryDirector.Instance;
                System.Action finalize = () =>
                {
                    _chapter4SampleCutscenePending = false;
                    _chapter4SampleCutscenePlayed = true;
                    GuidanceManager.Instance?.ClearTarget();
                    CompleteObjective("q.chapter4.sample.collect");
                    
                    // Auto start next quest 4.4
                    StartQuest("q.chapter4.return");
                };

                if (director != null)
                {
                    director.PlaySequence(Chapter4SampleCompletionStoryPath, finalize);
                }
                else
                {
                    finalize.Invoke();
                }
            }
        }

        public void MarkChapter4SampleIntroPlayed()
        {
            _chapter4SampleIntroPlayed = true;
            ActivateChapter4SampleGuidance();
        }

        private void ActivateChapter4SampleGuidance()
        {
            if (GetQuestStatus("q.chapter4.sample") != QuestStatus.InProgress ||
                IsObjectiveCompleted("q.chapter4.sample.collect"))
            {
                return;
            }

            if (SceneManager.GetActiveScene().name != "MainScene")
            {
                return;
            }

            EnsureChapter4SampleGuidanceTarget();
            GuidanceManager.Instance?.ActivateTarget(Chapter4SampleGuidanceTargetId);
        }

        private void EnsureChapter4SampleGuidanceTarget()
        {
            if (_chapter4SampleGuidanceTarget != null) return;

            var go = new GameObject("Chapter4SampleTarget");
            DontDestroyOnLoad(go);
            go.transform.position = Chapter4SampleTargetPosition;
            _chapter4SampleGuidanceTarget = go.AddComponent<GuidanceTarget>();

            ConfigureGuidanceTargetFields(_chapter4SampleGuidanceTarget, Chapter4SampleGuidanceTargetId, 0.15f, 5f);
            GuidanceManager.Instance?.RegisterTarget(_chapter4SampleGuidanceTarget);
        }

        private static void ConfigureGuidanceTargetFields(GuidanceTarget target, string targetId, float verticalOffset, float detectionRadius)
        {
            if (target == null) return;
            GuidanceTargetIdField?.SetValue(target, targetId);
            GuidanceTargetOffsetField?.SetValue(target, verticalOffset);
            GuidanceTargetRadiusField?.SetValue(target, detectionRadius);
        }

        private void ActivateCurrentFieldTarget()
        {
            if (_fieldPhaseTargetIndex < 0 || _fieldPhaseTargetIndex >= FieldPhaseTargetSequence.Length)
            {
                return;
            }

            var targetId = FieldPhaseTargetSequence[_fieldPhaseTargetIndex];
            GuidanceManager.Instance?.ActivateTarget(targetId);
        }

        private System.Collections.IEnumerator DelayedRefreshAfterCutscene()
        {
            Debug.Log("[QuestManager] DelayedRefreshAfterCutscene: Coroutine started. Waiting for end of frame...");
            // Wait for end of frame to ensure all destruction and event processing is done
            yield return new WaitForEndOfFrame();
            yield return null; // Wait one more frame for safety

            if (debugLog) Debug.Log("[QuestManager] Executing delayed refresh after cutscene...");

            var invUI = Object.FindFirstObjectByType<InventoryUISystem>();
            if (invUI != null) 
            {
                invUI.RefreshTools();
                Debug.Log("[QuestManager] InventoryUISystem refreshed.");
            }

            var toolManager = Object.FindFirstObjectByType<ToolManager>();
            if (toolManager != null)
            {
                // Ensure current tool is re-equipped if any
                var current = toolManager.GetCurrentTool();
                if (current != null)
                {
                    Debug.Log($"[QuestManager] Re-equipping tool: {current.toolName}");
                    // Unequip first to ensure clean state
                    toolManager.UnequipCurrentTool();
                    // Re-equip
                    toolManager.EquipTool(current);
                }
            }
            Debug.Log("[QuestManager] DelayedRefreshAfterCutscene: Completed.");
        }

        private void HandleFieldPhaseSamplingProgress(SampleItem sample)
        {
            if (GetQuestStatus("q.field.phase") != QuestStatus.InProgress)
            {
                return;
            }

            if (!IsObjectiveCompleted("q.field.phase.enter_field") ||
                IsObjectiveCompleted("q.field.phase.collect_samples"))
            {
                return;
            }

            if (_fieldPhaseTargetIndex >= FieldPhaseTargetSequence.Length)
            {
                return;
            }

            if (!IsSampleFromCurrentFieldTarget(sample))
            {
                if (debugLog)
                {
                    Debug.Log("[QuestManager] 样本不在当前指引目标范围内，忽略");
                }
                return;
            }

            _fieldPhaseTargetIndex++;
            if (debugLog)
            {
                Debug.Log($"[QuestManager] FieldPhase 样本完成 {_fieldPhaseTargetIndex}/{FieldPhaseTargetSequence.Length}");
            }

            if (_fieldPhaseTargetIndex < FieldPhaseTargetSequence.Length)
            {
                ActivateCurrentFieldTarget();
            }
            else
            {
                GuidanceManager.Instance?.ClearTarget();
                BeginFieldPhaseCompletionSequence();
            }
        }

        private bool IsSampleFromCurrentFieldTarget(SampleItem sample)
        {
            if (_fieldPhaseTargetIndex >= FieldPhaseTargetSequence.Length)
            {
                return false;
            }

            var targetId = FieldPhaseTargetSequence[_fieldPhaseTargetIndex];
            if (GuidanceManager.Instance == null ||
                !GuidanceManager.Instance.TryGetTarget(targetId, out var target) ||
                target == null)
            {
                return false;
            }

            float radius = Mathf.Max(0.5f, target.DetectionRadius);
            float distance = Vector3.Distance(sample.originalCollectionPosition, target.WorldPosition);
            return distance <= radius;
        }

        private void BeginFieldPhaseCompletionSequence()
        {
            if (_fieldPhaseSampleCutscenePending)
            {
                return;
            }

            _fieldPhaseSampleCutscenePending = true;

            System.Action finalize = () =>
            {
                _fieldPhaseSampleCutscenePending = false;
                PersistentEarthquakeController.Instance?.StartEarthquake();
                CompleteObjective("q.field.phase.collect_samples");
            };

            var director = StorySystem.StoryDirector.Instance;
            if (director != null)
            {
                director.PlaySequence(FieldPhaseSampleStoryPath, finalize);
            }
            else
            {
                finalize.Invoke();
            }
        }

        // 奖励发放 ------------------------------------------------------------
        private void GrantRewards(string questId)
        {
            if (questId == "q.lab.intro")
            {
                // 发放：地质锤 + 场景切换器
                ToolUnlockService.UnlockToolById("1002");
                ToolUnlockService.UnlockToolById("999");

                // 刷新UI
                var ui = Object.FindFirstObjectByType<InventoryUISystem>();
                if (ui != null) ui.RefreshTools();

                if (debugLog) Debug.Log("[QuestManager] 奖励已发放：地质锤(1002) + 场景切换器(999)");

                // 推进到下一任务：与Dr. Kaede对话
                StartQuest("q.lab.drkaede");
            }
            else if (questId == "q.lab.drkaede")
            {
                if (debugLog) Debug.Log("[QuestManager] Kaede对话完成，启动异常样本任务");
                StartQuest("q.lab.anomaly");
            }
            else if (questId == "q.lab.anomaly")
            {
                if (debugLog) Debug.Log("[QuestManager] 异常样本任务完成，引导玩家使用场景切换器");
                StartQuest("q.field.phase");
            }
            else if (questId == "q.field.phase")
            {
                if (debugLog) Debug.Log("[QuestManager] 野外剧情完成，引导玩家返回研究室");
                GuidanceManager.Instance?.ClearTarget();
                PersistentEarthquakeController.Instance?.StopEarthquake();
                StartQuest("q.lab.return");
            }
            else if (questId == "q.lab.return")
            {
                if (debugLog) Debug.Log("[QuestManager] 玩家已返回研究室，启动与Dr.Kaede的后续会谈");
                // 确保场景切换器依旧可用并发放章节4工具
                ToolUnlockService.UnlockToolById("999");   // 场景切换器兜底
                ToolUnlockService.UnlockToolById("1000"); // 简易钻探工具
                ToolUnlockService.UnlockToolById("1001"); // 钻塔工具

                var ui = Object.FindFirstObjectByType<InventoryUISystem>();
                if (ui != null) ui.RefreshTools();

                StartQuest("q.chapter4.kaede");
            }
            else if (questId == "q.chapter4.kaede")
            {
                if (debugLog) Debug.Log("[QuestManager] Kaede阶段剧情完成，启动章节4野外任务");
                _chapter4SampleCutscenePlayed = false;
                _chapter4SampleCutscenePending = false;
                _chapter4SampleIntroPlayed = false;
                if (_chapter4SampleGuidanceTarget != null)
                {
                    Destroy(_chapter4SampleGuidanceTarget.gameObject);
                    _chapter4SampleGuidanceTarget = null;
                }
                StartQuest("q.chapter4.field");
            }
            else if (questId == "q.chapter4.sample")
            {
                if (debugLog) Debug.Log("[QuestManager] 章节4样本目标完成，引导返回研究室");
                GuidanceManager.Instance?.ClearTarget();
                if (_chapter4SampleGuidanceTarget != null)
                {
                    Destroy(_chapter4SampleGuidanceTarget.gameObject);
                    _chapter4SampleGuidanceTarget = null;
                }
                StartQuest("q.chapter4.return");
            }
            else if (questId == "q.chapter4.return")
            {
                if (debugLog) Debug.Log("[QuestManager] 章节4返回完成，开始章节5会谈");
                StartQuest("q.chapter5.kaede");
            }
            else if (questId == "q.chapter5.kaede")
            {
                if (debugLog) Debug.Log("[QuestManager] 章节5会谈完成，解锁无人机并前往野外");
                ToolUnlockService.UnlockToolById("1100"); // 无人机
                var ui = Object.FindFirstObjectByType<InventoryUISystem>();
                if (ui != null) ui.RefreshTools();
                StartQuest("q.chapter5.field");
            }
            else if (questId == "q.chapter5.field")
            {
                if (debugLog) Debug.Log("[QuestManager] 章节5外勤完成，引导返回G-Lab");
                StartQuest("q.chapter5.return");
            }
            else if (questId == "q.chapter5.return")
            {
                if (debugLog) Debug.Log("[QuestManager] 章节5返回完成，准备最终会谈");
                StartQuest("q.chapter6.kaede");
            }
        }

        // 奖励逻辑保持在完成当下发放；跨场景的持久保持由 PlayerPersistentData 负责。

        // 工具：判断场景是否允许显示/启动任务UI
        private bool IsAllowedScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return false;
            if (uiAllowedScenes == null || uiAllowedScenes.Length == 0) return true;
            foreach (var s in uiAllowedScenes)
            {
                if (!string.IsNullOrEmpty(s) && s == sceneName) return true;
            }
            return false;
        }

        // 新的查询辅助 --------------------------------------------------------
        public QuestStatus GetQuestStatus(string questId)
        {
            if (string.IsNullOrEmpty(questId)) return QuestStatus.NotStarted;
            if (_quests.TryGetValue(questId, out var quest))
            {
                return quest.status;
            }
            return QuestStatus.NotStarted;
        }

        public bool IsObjectiveCompleted(string objectiveId)
        {
            if (string.IsNullOrEmpty(objectiveId)) return false;
            return _completedObjectives.Contains(objectiveId);
        }

        public Quest GetQuest(string questId)
        {
            if (string.IsNullOrEmpty(questId)) return null;
            _quests.TryGetValue(questId, out var quest);
            return quest;
        }

        public IEnumerable<Quest> GetAllQuests()
        {
            return _quests.Values;
        }
    }
}

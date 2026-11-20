using System.Collections.Generic;
using Core;
using GuidanceSystem;
using QuestSystem;
using StorySystem;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UISystem
{
    /// <summary>
    /// When nothing has moved the investigation forward for a while, Dr.Kaede gives one short hint
    /// about the next step. Story, menus and the coach pause the timer; any tool use, quest step or
    /// scene change starts it again. At most <see cref="MaxHints"/> hints per step, so it never nags.
    /// </summary>
    public sealed class IdleHintWatcher : MonoBehaviour
    {
        public const float FirstHintDelay = 20f;
        public const float RepeatDelay = 40f;
        public const int MaxHints = 3;
        private const string FieldScene = "MainScene";
        private const string LabScene = "Laboratory Scene";

        private static IdleHintWatcher _instance;
        private float _idle;
        private int _hintsThisStep;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null) return;
            var go = new GameObject("IdleHintWatcher");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<IdleHintWatcher>();
        }

        /// <summary>Seconds of no progress before hint number <paramref name="shown"/> (0-based).</summary>
        public static float DelayBeforeHint(int shown) => shown <= 0 ? FirstHintDelay : RepeatDelay;

        /// <summary>Which hint fits the current state. Pure, so EditMode tests can check the choice.</summary>
        public static string HintKey(string destination, string scene, bool talkPending, bool hammerQuest, bool towerQuest, bool guidanceLine)
        {
            if (talkPending) return "ui.idle.talk";
            if (destination != null) return "ui.idle.shift";
            if (scene == FieldScene && hammerQuest) return "ui.idle.hammer";
            if (scene == FieldScene && towerQuest) return "ui.idle.tower";
            return guidanceLine ? "ui.idle.follow" : "ui.idle.task";
        }

        private void OnEnable()
        {
            GameEventBus.SceneLoaded += OnSceneLoaded;
            GameEventBus.ToolEquipped += OnTool;
            GameEventBus.ToolUsed += OnToolUsed;
            GameEventBus.QuestStarted += OnProgress;
            GameEventBus.ObjectiveCompleted += OnProgress;
            GameEventBus.QuestCompleted += OnProgress;
            InvestigationProgress.Changed += OnStep;
        }

        private void OnDisable()
        {
            GameEventBus.SceneLoaded -= OnSceneLoaded;
            GameEventBus.ToolEquipped -= OnTool;
            GameEventBus.ToolUsed -= OnToolUsed;
            GameEventBus.QuestStarted -= OnProgress;
            GameEventBus.ObjectiveCompleted -= OnProgress;
            GameEventBus.QuestCompleted -= OnProgress;
            InvestigationProgress.Changed -= OnStep;
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void OnSceneLoaded(string _) => ResetStep();
        private void OnTool(string a, string b) => _idle = 0f;
        private void OnToolUsed(string a, string b, string c, string d) => _idle = 0f;
        private void OnProgress(string _) => ResetStep();
        private void OnStep() => ResetStep();

        private void ResetStep()
        {
            _idle = 0f;
            _hintsThisStep = 0;
        }

        private static bool Paused()
        {
            string scene = SceneManager.GetActiveScene().name;
            if (scene != FieldScene && scene != LabScene) return true;
            return GameInputState.IsModalOpen || StoryDirector.IsStoryPlaybackActive || FirstControlGuide.IsOpen ||
                PhaseShifterCoach.IsCoaching || InventoryUISystem.IsAnyWheelOpen || QuestManager.IsPickupFeedbackPending ||
                QuestManager.Instance == null || Time.timeScale <= 0f;
        }

        private void Update()
        {
            if (Paused())
            {
                // Reading story or a menu is not being stuck.
                _idle = 0f;
                return;
            }
            if (_hintsThisStep >= MaxHints) return;
            _idle += Time.unscaledDeltaTime;
            if (_idle < DelayBeforeHint(_hintsThisStep)) return;
            ShowHint();
        }

        private void ShowHint()
        {
            var quests = QuestManager.Instance;
            string scene = SceneManager.GetActiveScene().name;
            var guidance = GuidanceManager.Current;
            string target = guidance != null ? guidance.ActiveTargetId : null;
            bool talkPending = !string.IsNullOrEmpty(target) && target.StartsWith("quest.npc.");
            bool hammer = quests.GetQuestStatus("q.field.phase") == QuestStatus.InProgress &&
                !quests.IsObjectiveCompleted("q.field.phase.collect_samples");
            bool tower = quests.GetQuestStatus("q.chapter4.sample") == QuestStatus.InProgress &&
                !quests.IsObjectiveCompleted("q.chapter4.sample.collect");
            string destination = CollectionGuidanceHUD.RecommendedDestination();
            string key = HintKey(destination, scene, talkPending, hammer, tower, !string.IsNullOrEmpty(target));
            string text = GameUI.L(key);
            if (key == "ui.idle.shift")
                text = string.Format(text, GameUI.L(destination == FieldScene ? "scene.main.name" : "scene.laboratory.name"));
            GameToast.ShowHint(text);
            _hintsThisStep++;
            Backend.TelemetryClient.Instance?.Track("idle_hint_shown", new Dictionary<string, object>
            {
                ["hint"] = key,
                ["count"] = _hintsThisStep,
                ["scene"] = scene,
                ["idleSec"] = Mathf.RoundToInt(_idle),
                ["activities"] = InvestigationProgress.ActivityCount
            });
            _idle = 0f;
        }
    }
}

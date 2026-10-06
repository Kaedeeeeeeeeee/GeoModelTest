using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Core;
using StorySystem;
using UISystem;

namespace QuestSystem
{
    /// <summary>
    /// 简易NPC交互触发器：与Dr. Kaede对话，推动任务与剧情。
    /// </summary>
    public class QuestNpcInteraction : MonoBehaviour
    {
        [Serializable]
        private class QuestInteractionStage
        {
            public string questId;
            public string objectiveId;
            public string storyResourcePath;
            public string promptLocalizationKey;
            public bool autoStartWhenAvailable = true;
            public bool disablePlayerControl = true;
            public string prerequisiteQuestId;
            public QuestStatus prerequisiteStatus = QuestStatus.Completed;
        }

        [Header("阶段配置（按顺序执行）")]
        [SerializeField] private QuestInteractionStage[] stages = Array.Empty<QuestInteractionStage>();

        [Header("高亮设置")]
        [SerializeField] private Color highlightColor = new Color(0.55f, 0.85f, 1f, 1f);
        [SerializeField] private bool tintChildrenRenderers = true;

        private bool playerInRange;
        private const float KaedeTalkingDistance = 1.5f;
        private bool _usesKaedeRange;
        private readonly Collider[] _kaedeRangeHits = new Collider[32];
        private bool isInteracting;
        private bool previousInteractState;
        private GameObject promptCanvasGO;
        private Renderer[] renderers;
        private Material[] cachedMaterials;
        private Color[] originalColors;
        private MobileInputManager mobileInput;
        private Text promptText;
        private Text promptControlInstruction;
        private RectTransform promptPanel;
        private InteractionPrompt sharedPrompt;
        private MobileControlHint promptControlHint;
        private QuestInteractionStage currentStage;
        private QuestStatus currentStageStatus;
        private int currentStageIndex = -1;
        private bool hasPendingStage = false;
        private GameObject markerCanvasGO;
        private RectTransform marker;
        private Camera viewCamera;
        private float markerHeight = 2.4f;
        public bool HasNewConversation => currentStage != null && currentStageStatus == QuestStatus.InProgress;
        public bool IsRepeatingReminder { get; private set; }


        private void Awake()
        {
            EnsureCollider();
        }

        private void Start()
        {
            mobileInput = MobileInputManager.Instance;
            if (mobileInput == null)
            {
                mobileInput = FindFirstObjectByType<MobileInputManager>();
            }

            CacheRenderers();
            CreatePromptUI();
            CreateQuestMarker();
            RefreshCurrentStage();
            UpdateAvailability();
        }

        private void OnEnable()
        {
            UpdateAvailability();
        }

        private void OnDestroy()
        {
            if (promptCanvasGO != null) Destroy(promptCanvasGO);
            if (markerCanvasGO != null) Destroy(markerCanvasGO);
        }

        private void Update()
        {
            UpdateKaedeRange();
            if (isInteracting || StoryDirector.IsStoryPlaybackActive || GameInputState.GameplayBlocked || InventoryUISystem.IsAnyWheelOpen)
            {
                HidePrompt();
                if (markerCanvasGO != null) markerCanvasGO.SetActive(false);
                // Do not turn a held touch from another UI into a new conversation.
                previousInteractState = mobileInput != null && mobileInput.IsInteracting;
                return;
            }

            RefreshCurrentStage();
            UpdateQuestMarker();
            if (!playerInRange) { HidePrompt(); return; }
            UpdatePromptLocalization();
            ShowPrompt();
            if (IsInteractTriggered() && promptPanel != null && promptPanel.gameObject.activeInHierarchy) BeginInteraction();
        }

        private void BeginInteraction()
        {
            if (isInteracting || StoryDirector.IsStoryPlaybackActive || GameInputState.GameplayBlocked) return;
            RefreshCurrentStage();
            isInteracting = true;
            HidePrompt();
            if (markerCanvasGO != null) markerCanvasGO.SetActive(false);

            var director = StoryDirector.Instance;
            if (HasNewConversation)
            {
                IsRepeatingReminder = false;
                // Capture this stage: availability may change when the sequence completes.
                var stage = currentStage;
                director.PlaySequence(stage.storyResourcePath, () =>
                {
                    if (!string.IsNullOrEmpty(stage.objectiveId)) QuestManager.Instance.CompleteObjective(stage.objectiveId);
                    OnInteractionSequenceFinished();
                }, stage.disablePlayerControl);
            }
            else
            {
                IsRepeatingReminder = true;
                director.PlayReminder(LocalizationManager.Resolve("story.speaker.drkaede", "Dr.Kaede"),
                    GameUI.L(GetReminderKey()), OnInteractionSequenceFinished);
            }
        }

        public string GetReminderKey()
        {
            var quests = QuestManager.Instance;
            if (InvestigationProgress.IsComplete) return "quest.npc.reminder.complete";
            if (quests.GetQuestStatus("q.chapter4.sample") == QuestStatus.InProgress ||
                quests.GetQuestStatus("q.chapter4.field") == QuestStatus.InProgress) return "quest.npc.reminder.core";
            if (quests.GetQuestStatus("q.field.phase") == QuestStatus.InProgress) return "quest.npc.reminder.field";
            return "quest.npc.reminder.wait";
        }

        private void OnInteractionSequenceFinished()
        {
            isInteracting = false;
            IsRepeatingReminder = false;
            UpdateAvailability();
        }

        private void CreateQuestMarker()
        {
            var canvas = GameUI.Canvas("QuestNpcMarker", 155);
            markerCanvasGO = canvas.gameObject;
            var badge = GameUI.Box(canvas.transform, "NewConversation", new Color(0.06f, 0.09f, 0.10f, 0.85f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            badge.raycastTarget = false;
            marker = badge.rectTransform;
            marker.sizeDelta = new Vector2(60, 78);
            var text = GameUI.Label(marker, "Exclamation", "!", 48, Vector2.zero, Vector2.one, TextAnchor.MiddleCenter);
            text.color = new Color(1f, 0.8f, 0.2f);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 28;
            text.resizeTextMaxSize = 48;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            if (renderers != null && renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                markerHeight = Mathf.Max(1f, bounds.max.y - transform.position.y) + 0.35f;
            }
            markerCanvasGO.SetActive(false);
        }

        private void UpdateQuestMarker()
        {
            if (markerCanvasGO == null) return;
            if (viewCamera == null) viewCamera = Camera.main;
            if (!HasNewConversation || viewCamera == null) { markerCanvasGO.SetActive(false); return; }
            Vector3 screen = viewCamera.WorldToScreenPoint(transform.position + Vector3.up * markerHeight);
            bool visible = screen.z > 0 && screen.x >= 0 && screen.x <= Screen.width && screen.y >= 0 && screen.y <= Screen.height;
            markerCanvasGO.SetActive(visible);
            if (visible && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)markerCanvasGO.transform, screen, null, out Vector2 local)) marker.anchoredPosition = local;
        }

        private bool IsInteractTriggered()
        {
            var keyboard = Keyboard.current;
            bool keyboardPressed = keyboard != null && keyboard.eKey.wasPressedThisFrame;

            bool mobilePressed = false;
            if (mobileInput != null)
            {
                bool current = mobileInput.IsInteracting;
                mobilePressed = current && !previousInteractState;
                previousInteractState = current;
            }
            else
            {
                previousInteractState = false;
            }

            return keyboardPressed || mobilePressed;
        }

        private void EnsureCollider()
        {
            if (Array.Exists(stages, stage => stage != null && stage.questId == "q.lab.drkaede"))
            {
                // The same scene object serves every Kaede stage. Account for its 1.2 Y
                // scale without changing the model, transform or quest marker position.
                var oldBox = GetComponent<BoxCollider>();
                if (oldBox != null) oldBox.enabled = false;
                CapsuleCollider body = null;
                foreach (var capsule in GetComponents<CapsuleCollider>())
                {
                    if (capsule.isTrigger) capsule.enabled = false;
                    else body = capsule;
                }
                if (body == null) body = gameObject.AddComponent<CapsuleCollider>();
                float verticalScale = Mathf.Max(0.001f, Mathf.Abs(transform.lossyScale.y));
                float horizontalScale = Mathf.Max(0.001f, Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z));
                body.direction = 1;
                body.isTrigger = false;
                body.radius = 0.3f / horizontalScale;
                body.height = 1.8f / verticalScale;
                body.center = new Vector3(0f, 0.9f / verticalScale, 0f);
                _usesKaedeRange = true;
                return;
            }

            Collider col = GetComponent<Collider>();
            if (col == null)
            {
                var box = gameObject.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(1.2f, 1.8f, 1.2f);
            }
            else
            {
                col.isTrigger = true;
            }
        }

        private void UpdateKaedeRange()
        {
            if (!_usesKaedeRange) return;
            // Discover the controller in a wider volume, then use its center
            // rather than its contact/query shape to enforce the talking distance.
            // This world-space volume has no collider, so ordinary interaction rays
            // cannot hit it. Keep the full-height range independent of model scaling.
            Bounds bounds = new Bounds(transform.position + Vector3.up * 1.5f, new Vector3(2f, 3f, 2f));
            float radius = Mathf.Max(bounds.extents.x, bounds.extents.z);
            Vector3 halfAxis = Vector3.up * Mathf.Max(0f, bounds.extents.y - radius);
            int count = Physics.OverlapCapsuleNonAlloc(bounds.center - halfAxis, bounds.center + halfAxis,
                Mathf.Max(radius, KaedeTalkingDistance), _kaedeRangeHits, ~0, QueryTriggerInteraction.Collide);
            playerInRange = false;
            for (int i = 0; i < count; i++)
            {
                // Held tool colliders must not extend the player's talking distance.
                if (!(_kaedeRangeHits[i] is CharacterController player) || !IsPlayerCollider(player)) continue;
                Vector3 offset = player.transform.position - transform.position;
                offset.y = 0f;
                if (offset.sqrMagnitude > KaedeTalkingDistance * KaedeTalkingDistance) continue;
                if (player.bounds.max.y < bounds.min.y || player.bounds.min.y > bounds.max.y) continue;
                playerInRange = true;
                break;
            }
        }

        private void CacheRenderers()
        {
            if (!tintChildrenRenderers) return;

            renderers = GetComponentsInChildren<Renderer>();
            cachedMaterials = new Material[renderers.Length];
            originalColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                cachedMaterials[i] = renderers[i].material;
                originalColors[i] = cachedMaterials[i].color;
            }
        }

        private void SetHighlight(bool active)
        {
            if (!tintChildrenRenderers || renderers == null) return;

            for (int i = 0; i < renderers.Length; i++)
            {
                if (cachedMaterials[i] == null) continue;
                cachedMaterials[i].color = active ? highlightColor : originalColors[i];
            }
        }

        private void CreatePromptUI()
        {
            if (promptCanvasGO != null) return;

            sharedPrompt = InteractionPrompt.Create("QuestNpcPromptCanvas", "PromptPanel", 160, MobileControlHint.Control.Interact);
            promptCanvasGO = sharedPrompt.gameObject;
            promptPanel = sharedPrompt.Panel;
            promptText = sharedPrompt.Action;
            promptControlHint = sharedPrompt.TouchControl;
            promptControlInstruction = sharedPrompt.TouchInstruction;
            UpdatePromptLocalization();
            promptCanvasGO.SetActive(false);
        }

        private void ShowPrompt()
        {
            if (promptCanvasGO != null && !promptCanvasGO.activeSelf)
            {
                promptCanvasGO.SetActive(true);
            }
            sharedPrompt?.SetVisible(true, 2);
            SetHighlight(true);
        }

        private void HidePrompt()
        {
            if (promptCanvasGO != null && promptCanvasGO.activeSelf)
            {
                promptCanvasGO.SetActive(false);
            }
            sharedPrompt?.SetVisible(false);
            SetHighlight(false);
        }

        private void UpdateAvailability()
        {
            RefreshCurrentStage();
            if (playerInRange && !isInteracting && !StoryDirector.IsStoryPlaybackActive && !GameInputState.GameplayBlocked)
            {
                UpdatePromptLocalization();
                ShowPrompt();
            }
            else HidePrompt();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_usesKaedeRange) return;
            if (!IsPlayerCollider(other)) return;

            playerInRange = true;
            UpdateAvailability();
        }

        private void OnTriggerExit(Collider other)
        {
            if (_usesKaedeRange) return;
            if (!IsPlayerCollider(other)) return;

            playerInRange = false;
            HidePrompt();
        }

        private bool IsPlayerCollider(Collider other)
        {
            if (other == null) return false;

            var controller = other.GetComponentInParent<FirstPersonController>();
            if (controller != null) return true;

            return other.CompareTag("Player");
        }

        private bool RefreshCurrentStage()
        {
            var questManager = QuestManager.Instance;
            hasPendingStage = false;
            if (questManager == null || stages == null || stages.Length == 0 || InvestigationProgress.IsComplete)
            {
                currentStage = null;
                currentStageStatus = QuestStatus.NotStarted;
                currentStageIndex = -1;
                return false;
            }

            for (int i = 0; i < stages.Length; i++)
            {
                var stage = stages[i];
                if (stage == null || string.IsNullOrEmpty(stage.questId)) continue;

                if (!string.IsNullOrEmpty(stage.prerequisiteQuestId))
                {
                    var prereqStatus = questManager.GetQuestStatus(stage.prerequisiteQuestId);
                    if (prereqStatus != stage.prerequisiteStatus)
                    {
                        hasPendingStage = true;
                        continue;
                    }
                }

                var status = questManager.GetQuestStatus(stage.questId);
                if (status == QuestStatus.Completed)
                {
                    continue;
                }

                if (status == QuestStatus.NotStarted && stage.autoStartWhenAvailable)
                {
                    questManager.StartQuest(stage.questId);
                    status = questManager.GetQuestStatus(stage.questId);
                }

                currentStageIndex = i;
                currentStage = stage;
                currentStageStatus = status;
                UpdatePromptLocalization();
                return true;
            }

            currentStage = null;
            currentStageStatus = QuestStatus.Completed;
            currentStageIndex = -1;
            return false;
        }

        public void InjectStageBefore(string targetQuestId, string newQuestId, string newObjectiveId, string newStoryPath, string newPrereqId)
        {
            if (stages == null) return;

            int targetIndex = -1;
            for (int i = 0; i < stages.Length; i++)
            {
                if (stages[i].questId == targetQuestId)
                {
                    targetIndex = i;
                    break;
                }
            }

            if (targetIndex != -1)
            {
                // Create new stage
                var newStage = new QuestInteractionStage
                {
                    questId = newQuestId,
                    objectiveId = newObjectiveId,
                    storyResourcePath = newStoryPath,
                    promptLocalizationKey = stages[targetIndex].promptLocalizationKey, // Reuse prompt or use default
                    autoStartWhenAvailable = true,
                    disablePlayerControl = true,
                    prerequisiteQuestId = newPrereqId,
                    prerequisiteStatus = QuestStatus.Completed
                };

                // Update target stage prerequisite
                stages[targetIndex].prerequisiteQuestId = newQuestId;

                // Insert new stage
                var newStages = new QuestInteractionStage[stages.Length + 1];
                for (int i = 0; i < targetIndex; i++) newStages[i] = stages[i];
                newStages[targetIndex] = newStage;
                for (int i = targetIndex; i < stages.Length; i++) newStages[i + 1] = stages[i];
                
                stages = newStages;
                
                // Refresh to apply changes immediately if needed
                RefreshCurrentStage();
            }
        }

        private void UpdatePromptLocalization()
        {
            if (promptText == null) return;
            bool followup = HasNewConversation && currentStageIndex > 0 &&
                QuestManager.Instance.GetQuestStatus(stages[currentStageIndex - 1].questId) == QuestStatus.Completed;
            string actionKey = HasNewConversation ? (followup ? "quest.npc.action.continue" : "quest.npc.action.talk") : "quest.npc.action.reminder";
            string mobileKey = HasNewConversation ? (followup ? "quest.npc.prompt.continue.mobile" : "quest.npc.prompt.mobile") : "quest.npc.prompt.reminder.mobile";
            bool touch = MobileControlHint.UsesTouchControls;
            sharedPrompt.SetContent(GameUI.L(touch ? mobileKey : actionKey), "E", touch);
        }
    }
}

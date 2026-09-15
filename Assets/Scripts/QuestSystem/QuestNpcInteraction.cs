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

        [Header("提示UI")]
        [SerializeField] private Color promptBackgroundColor = new Color(0f, 0f, 0f, 0.65f);
        [SerializeField] private int promptFontSize = 22;

        [Header("高亮设置")]
        [SerializeField] private Color highlightColor = new Color(0.55f, 0.85f, 1f, 1f);
        [SerializeField] private bool tintChildrenRenderers = true;

        private bool playerInRange;
        private bool isInteracting;
        private bool previousInteractState;
        private GameObject promptCanvasGO;
        private Renderer[] renderers;
        private Material[] cachedMaterials;
        private Color[] originalColors;
        private MobileInputManager mobileInput;
        private Text promptText;
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
            if (IsInteractTriggered()) BeginInteraction();
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

            promptCanvasGO = new GameObject("QuestNpcPromptCanvas");

            var canvas = promptCanvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 160;

            var scaler = promptCanvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            promptCanvasGO.AddComponent<GraphicRaycaster>();

            var panel = new GameObject("PromptPanel");
            panel.transform.SetParent(promptCanvasGO.transform, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.22f);
            rect.anchorMax = new Vector2(0.5f, 0.22f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(420f, 64f);

            var bg = panel.AddComponent<Image>();
            bg.color = promptBackgroundColor;

            var textObj = new GameObject("PromptText");
            textObj.transform.SetParent(panel.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16f, 12f);
            textRect.offsetMax = new Vector2(-16f, -12f);

            promptText = textObj.AddComponent<Text>();
            promptText.font = UIFontResolver.GetUIFont();
            promptText.fontSize = promptFontSize;
            promptText.color = Color.white;
            promptText.alignment = TextAnchor.MiddleCenter;

            promptCanvasGO.SetActive(false);
        }

        private void ShowPrompt()
        {
            if (promptCanvasGO != null && !promptCanvasGO.activeSelf)
            {
                promptCanvasGO.SetActive(true);
            }
            SetHighlight(true);
        }

        private void HidePrompt()
        {
            if (promptCanvasGO != null && promptCanvasGO.activeSelf)
            {
                promptCanvasGO.SetActive(false);
            }
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
            if (!IsPlayerCollider(other)) return;

            playerInRange = true;
            UpdateAvailability();
        }

        private void OnTriggerExit(Collider other)
        {
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
            string targetKey = currentStage != null ? currentStage.promptLocalizationKey : string.Empty;
            bool followup = HasNewConversation && currentStageIndex > 0 &&
                QuestManager.Instance.GetQuestStatus(stages[currentStageIndex - 1].questId) == QuestStatus.Completed;
            targetKey = HasNewConversation ? (followup ? "quest.npc.prompt.continue" : targetKey) : "quest.npc.prompt.reminder";
            promptText.text = LocalizationManager.ResolveForCurrentInput(
                targetKey,
                HasNewConversation ? (followup ? "quest.npc.prompt.continue.mobile" : "quest.npc.prompt.mobile") : "quest.npc.prompt.reminder.mobile",
                "［E］カエデ研究員に話を聞く",
                "カエデ研究員に話を聞く");
        }
    }
}

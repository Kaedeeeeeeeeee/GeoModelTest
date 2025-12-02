using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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
        private LocalizedText promptLocalized;
        private QuestInteractionStage currentStage;
        private QuestStatus currentStageStatus;
        private int currentStageIndex = -1;
        private bool hasPendingStage = false;

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
            RefreshCurrentStage();
            UpdateAvailability();
        }

        private void OnEnable()
        {
            UpdateAvailability();
        }

        private void OnDestroy()
        {
            if (promptCanvasGO != null)
            {
                Destroy(promptCanvasGO);
            }
        }

        private void Update()
        {
            if (!playerInRange || isInteracting)
            {
                return;
            }

            if (!RefreshCurrentStage())
            {
                HidePrompt();
                return;
            }

            var questManager = QuestManager.Instance;
            if (questManager == null)
            {
                return;
            }

            if (currentStageStatus != QuestStatus.InProgress)
            {
                HidePrompt();
                return;
            }

            UpdatePromptLocalization();
            ShowPrompt();

            if (IsInteractTriggered())
            {
                BeginInteraction();
            }
        }

        private void BeginInteraction()
        {
            isInteracting = true;
            HidePrompt();
            SetHighlight(false);

            if (!RefreshCurrentStage())
            {
                isInteracting = false;
                return;
            }

            var questManager = QuestManager.Instance;
            if (questManager != null && currentStageStatus == QuestStatus.NotStarted)
            {
                questManager.StartQuest(currentStage.questId);
                RefreshCurrentStage();
            }

            var director = StorySystem.StoryDirector.Instance;
            if (director != null)
            {
                director.PlaySequence(currentStage.storyResourcePath, OnInteractionSequenceFinished, currentStage.disablePlayerControl);
            }
            else
            {
                OnInteractionSequenceFinished();
            }
        }

        private void OnInteractionSequenceFinished()
        {
            var questManager = QuestManager.Instance;
            if (questManager != null)
            {
                if (!string.IsNullOrEmpty(currentStage?.objectiveId))
                {
                    questManager.CompleteObjective(currentStage.objectiveId);
                }
            }

            isInteracting = false;
            UpdateAvailability();
        }

        private bool IsInteractTriggered()
        {
            bool keyboardPressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;

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

            var text = textObj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = promptFontSize;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;

            promptLocalized = textObj.AddComponent<LocalizedText>();

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
            var questManager = QuestManager.Instance;
            if (questManager == null)
            {
                HidePrompt();
                return;
            }

            if (!RefreshCurrentStage())
            {
                HidePrompt();
                if (!hasPendingStage)
                {
                    enabled = false;
                }
                return;
            }

            if (playerInRange && currentStageStatus == QuestStatus.InProgress)
            {
                UpdatePromptLocalization();
                ShowPrompt();
            }
            else
            {
                HidePrompt();
            }
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
            if (questManager == null || stages == null || stages.Length == 0)
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

        private void UpdatePromptLocalization()
        {
            if (promptLocalized == null) return;
            string targetKey = currentStage != null ? currentStage.promptLocalizationKey : string.Empty;
            if (string.IsNullOrEmpty(targetKey))
            {
                if (!string.IsNullOrEmpty(promptLocalized.TextKey))
                {
                    promptLocalized.TextKey = string.Empty;
                }
                return;
            }

            if (promptLocalized.TextKey != targetKey)
            {
                promptLocalized.TextKey = targetKey;
            }
        }
    }
}

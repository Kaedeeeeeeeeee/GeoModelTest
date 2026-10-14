using Core;
using GuidanceSystem;
using QuestSystem;
using StorySystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UISystem
{
    /// <summary>
    /// Shows only the next action for the active field collection objective, or for the
    /// phase-shifter trip when that objective can only progress in the other scene.
    /// </summary>
    public sealed class CollectionGuidanceHUD : MonoBehaviour
    {
        private const string FieldScene = "MainScene";
        private const string LabScene = "Laboratory Scene";
        private const string PhaseShifterId = "999";
        private ToolManager _tools;
        private Canvas _canvas;
        private RectTransform _card;
        private Image _icon;
        private Text _title;
        private Text _instruction;
        private Button _openToolsButton;
        private MobileControlHint _toolsHint;
        public static string RecommendedToolId { get; private set; }
        public static bool IsVisible { get; private set; }
        public static RectTransform VisibleCard { get; private set; }
        /// <summary>True while the cue asks for the primary action with the phase shifter in hand.</summary>
        public static bool HighlightPrimaryAction { get; private set; }

        public static void Attach(ToolManager tools)
        {
            var hud = tools.GetComponent<CollectionGuidanceHUD>();
            if (hud == null) hud = tools.gameObject.AddComponent<CollectionGuidanceHUD>();
            hud._tools = tools;
        }

        private void Start()
        {
            _canvas = GameUI.Canvas("CollectionGuidanceCanvas", 999);
            _canvas.transform.SetParent(transform, false);
            var background = GameUI.Box(_canvas.transform, "CollectionHint", GameUI.Surface, Vector2.up, Vector2.up);
            background.raycastTarget = false;
            _card = background.rectTransform;
            _card.pivot = Vector2.up;
            _icon = GameUI.Box(_card, "ToolIcon", Color.white, new Vector2(0.03f, 0.61f), new Vector2(0.15f, 0.95f));
            _icon.preserveAspect = true;
            _icon.raycastTarget = false;
            _title = GameUI.Label(_card, "Title", "", 26, new Vector2(0.18f, 0.58f), new Vector2(0.97f, 0.97f));
            _title.color = GameUI.Accent;
            _title.verticalOverflow = VerticalWrapMode.Overflow;
            _instruction = GameUI.Label(_card, "NextAction", "", 24, new Vector2(0.04f, 0.07f), new Vector2(0.96f, 0.55f));
            _instruction.resizeTextForBestFit = true;
            _instruction.resizeTextMinSize = 20;
            _instruction.resizeTextMaxSize = 24;
            // The cue uses the same artwork and label as the real touch control.
            // Its own button opens the existing wheel; it never equips a tool implicitly.
            _openToolsButton = GameUI.Button(_card, "OpenTools", "", new Vector2(0.76f, 0.07f),
                new Vector2(0.98f, 0.91f), OpenTools);
            _openToolsButton.image.color = Color.clear;
            _toolsHint = MobileControlHint.Create(_openToolsButton.transform, "ControlHint", MobileControlHint.Control.Tools);
            _toolsHint.RectTransform.anchorMin = Vector2.zero;
            _toolsHint.RectTransform.anchorMax = Vector2.one;
            _toolsHint.RectTransform.offsetMin = _toolsHint.RectTransform.offsetMax = Vector2.zero;
            var aspect = _toolsHint.gameObject.AddComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            aspect.aspectRatio = 1f;
            _openToolsButton.gameObject.SetActive(false);
            _card.gameObject.SetActive(false);
        }

        public static string HammerTitle(string toolName, int site, int total) =>
            toolName + "　" + string.Format(GameUI.L("ui.collection.site_progress"), site, total);

        // Pure state resolution also used by the EditMode regression tests.
        public static string HammerStage(bool equipped, int hits, bool pendingSample)
        {
            if (pendingSample) return "pickup";
            if (!equipped) return "equip";
            return hits > 0 ? "hit_progress" : "hit";
        }

        public static string TowerStage(bool equipped, bool placing, bool validPlacement, bool hasTower,
            bool drilling, bool pendingSample, bool canDrill, bool nearby)
        {
            if (drilling) return "drilling";
            if (pendingSample) return "pickup_core";
            if (hasTower && !canDrill) return "finished";
            if (!equipped) return "equip";
            if (!hasTower) return placing ? (validPlacement ? "place" : "place_invalid") : "preview";
            return nearby ? "drill" : "approach";
        }

        // A field objective only progresses outdoors and a return objective only in the laboratory.
        // Work that is still pending in the current scene keeps the player there.
        public static string TravelDestination(string scene, bool fieldObjective, bool labObjective)
        {
            if (scene == LabScene) return fieldObjective && !labObjective ? FieldScene : null;
            if (scene == FieldScene) return labObjective && !fieldObjective ? LabScene : null;
            return null;
        }

        /// <summary>The scene the active quest sends the player to, or null when they are already there.</summary>
        public static string RecommendedDestination()
        {
            var quests = QuestManager.Instance;
            bool field = Pending(quests, "q.field.phase", "q.field.phase.collect_samples") ||
                Pending(quests, "q.chapter4.field", "q.chapter4.field.enter_field") ||
                Pending(quests, "q.chapter4.sample", "q.chapter4.sample.collect") ||
                Pending(quests, "q.chapter5.field", "q.chapter5.field.enter_field");
            bool lab = Pending(quests, "q.lab.return", "q.lab.return.enter_lab") ||
                Pending(quests, "q.chapter4.return", "q.chapter4.return.enter_lab") ||
                Pending(quests, "q.chapter5.return", "q.chapter5.return.enter_lab");
            return TravelDestination(SceneManager.GetActiveScene().name, field, lab);
        }

        private static bool Pending(QuestManager quests, string questId, string objectiveId) =>
            quests.GetQuestStatus(questId) == QuestStatus.InProgress && !quests.IsObjectiveCompleted(objectiveId);

        private void LateUpdate()
        {
            RecommendedToolId = null;
            IsVisible = false;
            HighlightPrimaryAction = false;
            RefreshGuidance();
            if (_card != null && _card.gameObject.activeSelf != IsVisible)
                _card.gameObject.SetActive(IsVisible);
            if (IsVisible) VisibleCard = _card;
            else if (VisibleCard == _card) VisibleCard = null;
        }

        private void RefreshGuidance()
        {
            if (_card == null || _tools == null || _tools.availableTools == null) return;
            if (GameInputState.IsModalOpen || StoryDirector.IsStoryPlaybackActive || QuestManager.IsPickupFeedbackPending) return;

            bool touch = FirstControlGuide.UsesTouch();
            string destination = RecommendedDestination();
            if (destination != null)
            {
                var shifter = FindTool(PhaseShifterId);
                if (shifter == null) return;
                // Selecting the shifter does not use it, so the cue stays until the destination list opens.
                bool holding = _tools.GetCurrentTool() == shifter;
                if (holding) HighlightPrimaryAction = true;
                else RecommendedToolId = PhaseShifterId;
                string shifterName = CurrentToolHUD.ToolName(shifter);
                Present(shifter, touch, ShowsToolsButton(touch, !holding), shifterName, string.Format(
                    GameUI.L("ui.collection." + (holding ? "shift_use" : "equip") + (touch ? ".touch" : ".desktop")),
                    shifterName, GameUI.L(destination == FieldScene ? "scene.main.name" : "scene.laboratory.name")));
                return;
            }
            if (SceneManager.GetActiveScene().name != FieldScene) return;

            var quests = QuestManager.Instance;
            bool hammerQuest = quests.GetQuestStatus("q.field.phase") == QuestStatus.InProgress &&
                !quests.IsObjectiveCompleted("q.field.phase.collect_samples");
            bool towerQuest = quests.GetQuestStatus("q.chapter4.sample") == QuestStatus.InProgress &&
                !quests.IsObjectiveCompleted("q.chapter4.sample.collect");
            if (!hammerQuest && !towerQuest) return;

            string id = towerQuest ? "1001" : "1002";
            CollectionTool tool = FindTool(id);
            if (tool == null) return;
            bool equipped = _tools.GetCurrentTool() == tool;
            string stage;
            int hits = 0;
            int required = 3;
            bool operationStarted;
            if (tool is HammerTool hammer)
            {
                hits = hammer.CurrentHitCount;
                required = hammer.requiredHits;
                stage = HammerStage(equipped, hits, hammer.PendingSample != null);
                operationStarted = hits > 0 || hammer.PendingSample != null;
            }
            else if (tool is DrillTowerTool drill)
            {
                var tower = drill.placedTower;
                bool pending = tower != null && tower.collectedSamples.Exists(sample => sample != null && sample.activeInHierarchy);
                stage = TowerStage(equipped, drill.IsPlacing, drill.CanConfirmPlacement, tower != null,
                    tower != null && tower.isDrilling, pending, tower == null || tower.CanDrill(), drill.IsPlayerNearTower);
                operationStarted = tower != null || drill.IsPlacing;
            }
            else return;

            if (!operationStarted && GuidanceManager.Current != null && !GuidanceManager.Current.IsPlayerNearActiveTarget)
                stage = "travel";
            if (stage == "equip") RecommendedToolId = id;
            bool showToolsButton = ShowsToolsButton(touch, stage == "equip");
            string toolName = CurrentToolHUD.ToolName(tool);
            string title = hammerQuest ? HammerTitle(toolName, quests.FieldCollectionSite, quests.FieldCollectionSiteTotal) : toolName;
            if (hammerQuest && showToolsButton)
                title = toolName + "\n" + string.Format(GameUI.L("ui.collection.site_progress"),
                    quests.FieldCollectionSite, quests.FieldCollectionSiteTotal);
            Present(tool, touch, showToolsButton, title, string.Format(
                GameUI.L("ui.collection." + stage + (touch ? ".touch" : ".desktop")), toolName, hits, required));
        }

        private CollectionTool FindTool(string id)
        {
            foreach (var candidate in _tools.availableTools)
            {
                if (candidate != null && candidate.toolID == id) return candidate;
            }
            return null;
        }

        private bool ShowsToolsButton(bool touch, bool equipStage) =>
            touch && equipStage && !InventoryUISystem.IsAnyWheelOpen &&
            MobileInputManager.Instance != null && _toolsHint.Refresh();

        private void Present(CollectionTool tool, bool touch, bool showToolsButton, string title, string instruction)
        {
            _openToolsButton.gameObject.SetActive(showToolsButton);
            _instruction.rectTransform.anchorMax = new Vector2(showToolsButton ? 0.73f : 0.96f, 0.55f);
            _title.rectTransform.anchorMax = new Vector2(showToolsButton ? 0.73f : 0.97f, 0.97f);
            _title.text = title;
            _instruction.text = instruction;
            _icon.sprite = ToolIconResolver.GetIcon(tool);
            float scale = Mathf.Max(0.01f, _canvas.scaleFactor);
            Rect safe = Screen.safeArea;
            _card.sizeDelta = new Vector2(Mathf.Min(540f, safe.width / scale * 0.42f), 182f);
            // Touch controls occupy the row below the quest card.
            float top = touch ? 370f : 172f;
            _card.anchoredPosition = new Vector2(safe.xMin / scale + 20f, -(Screen.height - safe.yMax) / scale - top);
            // Keep the recommendation for the wheel's highlighted slot, without a second
            // collection card competing with the tool selection UI.
            IsVisible = !InventoryUISystem.IsAnyWheelOpen;
        }

        private void OpenTools()
        {
            if (!FirstControlGuide.UsesTouch() || GameInputState.GameplayBlocked ||
                StoryDirector.IsStoryPlaybackActive || InventoryUISystem.IsAnyWheelOpen) return;

            // Recheck the objective and proximity at click time, including an objective
            // completed or a player moved since the previous frame's visible cue.
            RecommendedToolId = null;
            IsVisible = false;
            HighlightPrimaryAction = false;
            RefreshGuidance();
            if (IsVisible && !string.IsNullOrEmpty(RecommendedToolId))
                MobileInputManager.Instance?.TriggerToolWheelInput();
        }

        private void OnDisable()
        {
            RecommendedToolId = null;
            IsVisible = false;
            HighlightPrimaryAction = false;
            if (VisibleCard == _card) VisibleCard = null;
            if (_card != null) _card.gameObject.SetActive(false);
        }
    }
}

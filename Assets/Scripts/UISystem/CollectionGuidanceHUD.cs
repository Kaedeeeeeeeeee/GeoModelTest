using Core;
using GuidanceSystem;
using QuestSystem;
using StorySystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UISystem
{
    /// <summary>Shows only the next action for the active field collection objective.</summary>
    public sealed class CollectionGuidanceHUD : MonoBehaviour
    {
        private ToolManager _tools;
        private Canvas _canvas;
        private RectTransform _card;
        private Image _icon;
        private Text _title;
        private Text _instruction;
        public static string RecommendedToolId { get; private set; }
        public static bool IsVisible { get; private set; }

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
            _title = GameUI.Label(_card, "Title", "", 22, new Vector2(0.18f, 0.65f), new Vector2(0.97f, 0.95f));
            _title.color = GameUI.Accent;
            _instruction = GameUI.Label(_card, "NextAction", "", 21, new Vector2(0.04f, 0.07f), new Vector2(0.96f, 0.65f));
            _instruction.resizeTextForBestFit = true;
            _instruction.resizeTextMinSize = 17;
            _instruction.resizeTextMaxSize = 21;
            _card.gameObject.SetActive(false);
        }

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

        private void LateUpdate()
        {
            RecommendedToolId = null;
            IsVisible = false;
            RefreshGuidance();
            if (_card != null && _card.gameObject.activeSelf != IsVisible)
                _card.gameObject.SetActive(IsVisible);
        }

        private void RefreshGuidance()
        {
            if (_card == null || _tools == null) return;
            if (GameInputState.IsModalOpen || StoryDirector.IsStoryPlaybackActive ||
                SceneManager.GetActiveScene().name != "MainScene") return;

            var quests = QuestManager.Instance;
            bool hammerQuest = quests.GetQuestStatus("q.field.phase") == QuestStatus.InProgress &&
                !quests.IsObjectiveCompleted("q.field.phase.collect_samples");
            bool towerQuest = quests.GetQuestStatus("q.chapter4.sample") == QuestStatus.InProgress &&
                !quests.IsObjectiveCompleted("q.chapter4.sample.collect");
            if (!hammerQuest && !towerQuest) return;

            if (_tools.availableTools == null) return;
            string id = towerQuest ? "1001" : "1002";
            CollectionTool tool = null;
            foreach (var candidate in _tools.availableTools)
            {
                if (candidate != null && candidate.toolID == id) { tool = candidate; break; }
            }
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
            bool touch = FirstControlGuide.UsesTouch();
            string toolName = CurrentToolHUD.ToolName(tool);
            _title.text = toolName;
            _instruction.text = string.Format(GameUI.L("ui.collection." + stage + (touch ? ".touch" : ".desktop")),
                toolName, hits, required);
            _icon.sprite = ToolIconResolver.GetIcon(tool);
            float scale = Mathf.Max(0.01f, _canvas.scaleFactor);
            Rect safe = Screen.safeArea;
            _card.sizeDelta = new Vector2(Mathf.Min(540f, safe.width / scale * 0.42f), 142f);
            // Touch controls occupy the row below the quest card.
            float top = touch ? 370f : 172f;
            _card.anchoredPosition = new Vector2(safe.xMin / scale + 20f, -(Screen.height - safe.yMax) / scale - top);
            IsVisible = true;
        }

        private void OnDisable()
        {
            RecommendedToolId = null;
            IsVisible = false;
            if (_card != null) _card.gameObject.SetActive(false);
        }
    }
}

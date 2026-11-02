using Core;
using QuestSystem;
using SceneSystem;
using StorySystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UISystem
{
    /// <summary>
    /// Walks the player through the real controls on the first phase-shifter trip. The later
    /// trips use the same steps, so nothing is done for the player: it only points at each one.
    /// </summary>
    public sealed class PhaseShifterCoach : MonoBehaviour
    {
        public enum Step { None, OpenTools, PickShifter, UseShifter, ChooseDestination }

        // A player who stays on a dimmed step gets the screen back and keeps the ordinary card.
        public const float ReleaseSeconds = 20f;
        private const float ConfirmationSeconds = 6f;
        private const string FieldScene = "MainScene";
        private static readonly Color DimColor = new Color(0.01f, 0.03f, 0.05f, 0.82f);
        // Touch targets stay fully lit, with room for the breathing outline around them.
        private const float HolePadding = 14f;
        private static bool _confirmationPending;

        private Canvas _canvas;
        private readonly Image[] _dim = new Image[4];
        private Card _card;
        private Card _listCard;
        private Card _confirmation;
        private Step _lastStep;
        private float _stepSince;
        private float _confirmationUntil;
        private bool _released;

        public Step Current { get; private set; }
        public bool IsDimming { get; private set; }
        public bool IsBlockingOutsideTarget { get; private set; }

        private struct Card
        {
            public RectTransform Root;
            public Text Title;
            public Text Body;
        }

        // Pure state resolution also used by the EditMode regression tests.
        public static Step ResolveStep(bool listOpen, bool wheelOpen, bool holdingShifter)
        {
            if (listOpen) return Step.ChooseDestination;
            if (wheelOpen) return Step.PickShifter;
            return holdingShifter ? Step.UseShifter : Step.OpenTools;
        }

        public static bool DimsScreen(Step step) => step == Step.OpenTools || step == Step.UseShifter;

        public static int StepNumber(Step step)
        {
            switch (step)
            {
                case Step.OpenTools:
                case Step.PickShifter: return 1;
                case Step.UseShifter: return 2;
                case Step.ChooseDestination: return 3;
                default: return 0;
            }
        }

        public static string InstructionKey(Step step, bool touch)
        {
            string device = touch ? ".touch" : ".desktop";
            switch (step)
            {
                case Step.OpenTools: return "ui.shifter_coach.open" + device;
                case Step.PickShifter: return "ui.shifter_coach.pick";
                case Step.UseShifter: return "ui.collection.shift_use" + device;
                case Step.ChooseDestination: return "ui.shifter_coach.choose";
                default: return null;
            }
        }

        private void Start()
        {
            // Above the tool wheel (31000), which this coach has to label from beside it.
            _canvas = GameUI.Canvas("PhaseShifterCoachCanvas", 32000);
            _canvas.transform.SetParent(transform, false);
            string[] names = { "DimLeft", "DimRight", "DimBelow", "DimAbove" };
            for (int i = 0; i < _dim.Length; i++)
            {
                _dim[i] = GameUI.Box(_canvas.transform, names[i], DimColor, Vector2.zero, Vector2.one);
                _dim[i].raycastTarget = false;
                _dim[i].gameObject.SetActive(false);
            }
            _card = BuildCard(_canvas.transform, "CoachCard");
            _confirmation = BuildCard(_canvas.transform, "CoachConfirmation");
            SetAnchors(_confirmation.Root, new Vector2(0.3f, 0.7f), new Vector2(0.7f, 0.86f));
            _confirmation.Title.gameObject.SetActive(false);
            _confirmation.Body.rectTransform.anchorMax = new Vector2(0.96f, 0.93f);
        }

        private static Card BuildCard(Transform parent, string name)
        {
            var box = GameUI.Box(parent, name, GameUI.Surface, Vector2.zero, Vector2.one);
            box.raycastTarget = false;
            var title = GameUI.Label(box.transform, "Title", "", 28, new Vector2(0.05f, 0.76f), new Vector2(0.95f, 0.96f));
            title.color = GameUI.Accent;
            title.raycastTarget = false;
            var body = GameUI.Label(box.transform, "Instruction", "", 30, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.74f));
            body.alignment = TextAnchor.UpperLeft;
            body.raycastTarget = false;
            box.gameObject.SetActive(false);
            return new Card { Root = box.rectTransform, Title = title, Body = body };
        }

        private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        /// <summary>Returns true while this coach replaces the ordinary card.</summary>
        public bool Refresh(CollectionTool shifter, bool holdingShifter, bool firstTripPending, bool touch, string destinationName)
        {
            if (_canvas == null) return false;
            var manager = GameSceneManager.Instance;
            bool listOpen = manager != null && manager.IsSceneSelectionOpen;
            bool blocked = StoryDirector.IsStoryPlaybackActive || QuestManager.IsPickupFeedbackPending ||
                FirstControlGuide.IsOpen || GameSceneManager.IsLoadingScene || (GameInputState.IsModalOpen && !listOpen);
            RefreshConfirmation(blocked);

            if (!firstTripPending) _released = false;
            Current = firstTripPending && !_released && !blocked && shifter != null
                ? ResolveStep(listOpen, InventoryUISystem.IsAnyWheelOpen, holdingShifter)
                : Step.None;
            if (Current != _lastStep)
            {
                _lastStep = Current;
                _stepSince = Time.unscaledTime;
            }
            if (DimsScreen(Current) && Time.unscaledTime - _stepSince >= ReleaseSeconds)
            {
                _released = true;
                Current = Step.None;
            }
            if (Current != Step.None) _confirmationPending = true;

            RefreshDim(touch);
            RefreshCards(shifter, touch, destinationName, manager);
            return Current != Step.None;
        }

        private void RefreshConfirmation(bool blocked)
        {
            // The first-field guide is modal, so the confirmation waits until the player can read it.
            if (_confirmationPending && !blocked && SceneManager.GetActiveScene().name == FieldScene &&
                PlayerPrefs.GetInt(FirstControlGuide.FieldCompletedKey, 0) == 1)
            {
                _confirmationPending = false;
                _confirmationUntil = Time.unscaledTime + ConfirmationSeconds;
                _confirmation.Body.text = GameUI.L("ui.shifter_coach.done");
            }
            _confirmation.Root.gameObject.SetActive(!blocked && Time.unscaledTime < _confirmationUntil);
        }

        private void RefreshDim(bool touch)
        {
            IsDimming = DimsScreen(Current);
            Button target = null;
            if (IsDimming && touch && MobileControlsUI.ActiveInstance != null)
                target = Current == Step.OpenTools ? MobileControlsUI.ActiveInstance.toolWheelButton : MobileControlsUI.ActiveInstance.interactButton;
            bool hasTarget = target != null && target.gameObject.activeInHierarchy;
            // Without a real control to leave open, the dim must never swallow input.
            IsBlockingOutsideTarget = IsDimming && hasTarget;

            Vector2 holeMin = Vector2.zero, holeMax = Vector2.zero;
            if (hasTarget)
            {
                var corners = new Vector3[4];
                target.GetComponent<RectTransform>().GetWorldCorners(corners);
                var size = new Vector2(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));
                float padding = HolePadding * _canvas.scaleFactor;
                holeMin = new Vector2((corners[0].x - padding) / size.x, (corners[0].y - padding) / size.y);
                holeMax = new Vector2((corners[2].x + padding) / size.x, (corners[2].y + padding) / size.y);
            }
            // Four panels around the hole: pointer input reaches the real button through the gap.
            SetDim(0, Vector2.zero, new Vector2(holeMin.x, 1f));
            SetDim(1, new Vector2(holeMax.x, 0f), Vector2.one);
            SetDim(2, new Vector2(holeMin.x, 0f), new Vector2(holeMax.x, holeMin.y));
            SetDim(3, new Vector2(holeMin.x, holeMax.y), new Vector2(holeMax.x, 1f));
            for (int i = 0; i < _dim.Length; i++)
            {
                // With no hole the first panel has zero width, so the second covers the screen.
                _dim[i].gameObject.SetActive(IsDimming);
                _dim[i].raycastTarget = IsBlockingOutsideTarget;
            }
        }

        private void SetDim(int index, Vector2 min, Vector2 max) => SetAnchors(_dim[index].rectTransform, min, max);

        private void RefreshCards(CollectionTool shifter, bool touch, string destinationName, GameSceneManager manager)
        {
            bool inList = Current == Step.ChooseDestination;
            _card.Root.gameObject.SetActive(Current != Step.None && !inList);
            if (_listCard.Root != null) _listCard.Root.gameObject.SetActive(inList);
            if (Current == Step.None) return;

            if (inList && _listCard.Root == null)
            {
                // The destination list is a top modal, so its label has to live on the list's canvas.
                _listCard = BuildCard(manager.sceneSelectionUI.transform, "CoachCard");
                SetAnchors(_listCard.Root, new Vector2(0.012f, 0.36f), new Vector2(0.285f, 0.72f));
                // That canvas scales from 1920 wide, against 1600 for this one.
                _listCard.Title.fontSize = 34;
                _listCard.Body.fontSize = 36;
                _listCard.Root.gameObject.SetActive(true);
            }
            var card = inList ? _listCard : _card;
            if (!inList)
            {
                // The wheel fills the middle of the screen, so its step reads from beside it.
                if (Current == Step.PickShifter) SetAnchors(card.Root, new Vector2(0.012f, 0.36f), new Vector2(0.285f, 0.72f));
                else SetAnchors(card.Root, new Vector2(0.28f, 0.4f), new Vector2(0.72f, 0.72f));
            }
            card.Title.text = string.Format(GameUI.L("ui.shifter_coach.title"), StepNumber(Current));
            string instruction = string.Format(GameUI.L(InstructionKey(Current, touch)),
                CurrentToolHUD.ToolName(shifter), destinationName);
            card.Body.text = Current == Step.OpenTools ? GameUI.L("ui.shifter_coach.why") + "\n\n" + instruction : instruction;
        }

        private void OnDisable()
        {
            Current = Step.None;
            IsDimming = false;
            IsBlockingOutsideTarget = false;
            foreach (var panel in _dim)
                if (panel != null) panel.gameObject.SetActive(false);
            if (_card.Root != null) _card.Root.gameObject.SetActive(false);
            if (_listCard.Root != null) _listCard.Root.gameObject.SetActive(false);
            if (_confirmation.Root != null) _confirmation.Root.gameObject.SetActive(false);
        }
    }
}

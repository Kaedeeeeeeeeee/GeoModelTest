using System;
using Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UISystem
{
    /// <summary>A fresh, explicit acknowledgement on entering the title screen. No consent is persisted.</summary>
    public sealed class ResearchConsentDialog : MonoBehaviour
    {
        private static ResearchConsentDialog _current;
        private GameInputState.Scope _input;
        private ModalCanvasLayerGuard.Scope _layer;
        private Action _accepted;
        private GameObject _previousSelection;
        private RectTransform _card;
        private readonly Toggle[] _agreements = new Toggle[3];
        private Button _continue;
        private Button _cancel;
        private bool _closed;

        public static bool IsOpen => _current != null;

        public static void Show(Action accepted)
        {
            if (IsOpen) return;
            var canvas = GameUI.Canvas("ResearchConsentDialog", 32767);
            var dialog = canvas.gameObject.AddComponent<ResearchConsentDialog>();
            _current = dialog;
            dialog._accepted = accepted;
            dialog._previousSelection = EventSystem.current.currentSelectedGameObject;
            dialog._input = GameInputState.Acquire(CloseCurrent);
            dialog._layer = ModalCanvasLayerGuard.Activate(canvas);
            dialog.Build(canvas.transform);
            dialog._agreements[0].Select();
        }

        private void Build(Transform root)
        {
            GameUI.Box(root, "Dim", new Color(0.01f, 0.03f, 0.05f, 0.82f), Vector2.zero, Vector2.one);
            _card = GameUI.Box(root, "ConsentCard", GameUI.Surface, Vector2.one * 0.5f, Vector2.one * 0.5f).rectTransform;
            ResizeCard();
            GameUI.Box(_card, "Accent", GameUI.Accent, new Vector2(0, 1), Vector2.one).rectTransform.offsetMin = new Vector2(0, -5);
            Label("Eyebrow", "eyebrow", 19, 32, 28, 30).color = GameUI.Accent;
            Label("Title", "title", 30, 32, 62, 80);

            BuildScrollBody();

            for (int i = 0; i < _agreements.Length; i++) BuildAgreement(i);

            var hint = GameUI.Label(_card, "Hint", GameUI.L("ui.consent.hint"), 18, Vector2.zero, Vector2.right);
            hint.rectTransform.offsetMin = new Vector2(32, 82);
            hint.rectTransform.offsetMax = new Vector2(-32, 110);
            hint.color = GameUI.Muted;
            _cancel = GameUI.Button(_card, "Cancel", GameUI.L("ui.consent.cancel"), Vector2.zero, new Vector2(0.38f, 0), CloseCurrent);
            _cancel.GetComponent<RectTransform>().offsetMin = new Vector2(32, 24);
            _cancel.GetComponent<RectTransform>().offsetMax = new Vector2(-8, 78);
            _continue = GameUI.Button(_card, "Continue", GameUI.L("ui.consent.continue"), new Vector2(0.38f, 0), Vector2.right, Accept, true);
            _continue.GetComponent<RectTransform>().offsetMin = new Vector2(8, 24);
            _continue.GetComponent<RectTransform>().offsetMax = new Vector2(-32, 78);
            UpdateAgreement(false);
        }

        private void BuildAgreement(int index)
        {
            float bottom = 116 + (2 - index) * 76;
            var row = GameUI.Box(_card, "Agreement" + (index + 1), GameUI.Panel, Vector2.zero, Vector2.right);
            row.rectTransform.offsetMin = new Vector2(32, bottom);
            row.rectTransform.offsetMax = new Vector2(-32, bottom + 68);
            var agreement = row.gameObject.AddComponent<Toggle>();
            agreement.toggleTransition = Toggle.ToggleTransition.None;
            _agreements[index] = agreement;
            agreement.targetGraphic = row;
            var colors = agreement.colors;
            colors.highlightedColor = new Color(0.7f, 1f, 0.9f);
            colors.selectedColor = colors.highlightedColor;
            agreement.colors = colors;
            var box = GameUI.Box(row.transform, "Checkbox", GameUI.Muted, new Vector2(0, 0.5f), new Vector2(0, 0.5f));
            box.rectTransform.sizeDelta = new Vector2(30, 30);
            box.rectTransform.anchoredPosition = new Vector2(30, 0);
            GameUI.Box(box.transform, "Inset", GameUI.Surface, Vector2.one * 0.08f, Vector2.one * 0.92f);
            var check = GameUI.Rect(box.transform, "Check", Vector2.zero, Vector2.one).gameObject.AddComponent<ConsentCheckmark>();
            check.color = GameUI.Accent;
            check.raycastTarget = false;
            agreement.graphic = check;
            agreement.isOn = false;
            var agreementText = GameUI.Label(row.transform, "Label", GameUI.L("ui.consent.agreement." + (index + 1)), 23,
                Vector2.zero, Vector2.one);
            agreementText.rectTransform.offsetMin = new Vector2(60, 6);
            agreementText.rectTransform.offsetMax = new Vector2(-16, -6);
            agreementText.resizeTextForBestFit = true;
            agreementText.resizeTextMinSize = 18;
            agreementText.resizeTextMaxSize = 23;
            agreement.onValueChanged.AddListener(UpdateAgreement);
        }

        private Text Label(string name, string key, int size, float inset, float top, float height)
        {
            var label = GameUI.Label(_card, name, GameUI.L("ui.consent." + key), size, Vector2.up, Vector2.one);
            label.rectTransform.offsetMin = new Vector2(inset, -top - height);
            label.rectTransform.offsetMax = new Vector2(-inset, -top);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 18;
            label.resizeTextMaxSize = size;
            return label;
        }

        private void BuildScrollBody()
        {
            var area = GameUI.Box(_card, "Information", GameUI.Panel, Vector2.zero, Vector2.one);
            area.rectTransform.offsetMin = new Vector2(32, 352);
            area.rectTransform.offsetMax = new Vector2(-32, -154);
            var scroll = area.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 32;
            var viewport = GameUI.Rect(area.transform, "Viewport", Vector2.zero, Vector2.one);
            viewport.offsetMin = new Vector2(22, 12);
            viewport.offsetMax = new Vector2(-28, -12);
            viewport.gameObject.AddComponent<RectMask2D>();
            var body = GameUI.Label(viewport, "Body", GameUI.L("ui.consent.body"), 24, Vector2.up, Vector2.one, TextAnchor.UpperLeft);
            body.rectTransform.pivot = Vector2.up;
            body.supportRichText = false;
            body.verticalOverflow = VerticalWrapMode.Overflow;
            body.lineSpacing = 1.2f;
            body.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = body.rectTransform;
            var track = GameUI.Box(area.transform, "Scrollbar", GameUI.Surface, Vector2.right, Vector2.one);
            track.rectTransform.offsetMin = new Vector2(-14, 12);
            track.rectTransform.offsetMax = new Vector2(-6, -12);
            var scrollbar = track.gameObject.AddComponent<Scrollbar>();
            var handle = GameUI.Box(track.transform, "Handle", GameUI.Muted, Vector2.zero, Vector2.one);
            scrollbar.handleRect = handle.rectTransform;
            scrollbar.targetGraphic = handle;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.navigation = new Navigation { mode = Navigation.Mode.None };
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            scroll.verticalNormalizedPosition = 1;
        }

        private void OnRectTransformDimensionsChange() => ResizeCard();

        private void ResizeCard()
        {
            if (_card == null) return;
            var size = ((RectTransform)transform).rect.size;
            _card.sizeDelta = new Vector2(Mathf.Min(1040, size.x - 48), Mathf.Min(820, size.y - 32));
        }

        private bool AllAgreed => Array.TrueForAll(_agreements, agreement => agreement != null && agreement.isOn);

        private void UpdateAgreement(bool _)
        {
            bool agreed = AllAgreed;
            _continue.interactable = agreed;
            // Keep keyboard/controller navigation inside this dialog, including when Continue is disabled.
            Link(_agreements[0], agreed ? (Selectable)_continue : _cancel, _agreements[1]);
            Link(_agreements[1], _agreements[0], _agreements[2]);
            Link(_agreements[2], _agreements[1], _cancel);
            Link(_cancel, _agreements[2], agreed ? (Selectable)_continue : _agreements[0]);
            Link(_continue, _cancel, _agreements[0]);
        }

        private static void Link(Selectable control, Selectable previous, Selectable next)
        {
            control.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = previous, selectOnLeft = previous,
                selectOnDown = next, selectOnRight = next
            };
        }

        private void Accept()
        {
            // Guard the action too: direct event invocation must never bypass the checkbox.
            if (_closed || !AllAgreed) return;
            var accepted = _accepted;
            CloseCurrent();
            accepted?.Invoke();
        }

        public static void CloseCurrent()
        {
            if (_current == null) return;
            var dialog = _current;
            dialog.gameObject.SetActive(false);
            Destroy(dialog.gameObject);
        }

        private void OnDisable()
        {
            _closed = true;
            _accepted = null;
            if (_current == this) _current = null;
            _layer?.Dispose();
            _layer = null;
            _input?.Dispose();
            _input = null;
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(_previousSelection != null && _previousSelection.activeInHierarchy ? _previousSelection : null);
        }
    }
}

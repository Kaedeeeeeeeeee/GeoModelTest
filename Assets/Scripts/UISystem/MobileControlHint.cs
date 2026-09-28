using UnityEngine;
using UnityEngine.UI;

namespace UISystem
{
    /// <summary>
    /// A non-interactive copy of a real touch control, sharing its sprites and localized label.
    /// Keeping the input components on the original button prevents a hint from consuming touches.
    /// </summary>
    public sealed class MobileControlHint : MonoBehaviour
    {
        public enum Control
        {
            Interact,
            Secondary,
            Tools,
            Inventory
        }

        private Control _control;
        private MobileControlsUI _controls;
        private Button _source;
        private GameObject _visual;
        private Text _sourceLabel;
        private Text _label;
        private Text _fallbackLabel;

        public RectTransform RectTransform => (RectTransform)transform;
        public bool HasVisual => _visual != null;

        public static bool UsesTouchControls => MobileInputManager.Instance != null
            ? MobileInputManager.Instance.ShouldShowVirtualControls()
            : MobileInputManager.IsRuntimeMobileDevice();

        public static MobileControlHint Create(Transform parent, string name, Control control)
        {
            var host = new GameObject(name, typeof(RectTransform));
            host.transform.SetParent(parent, false);
            var hint = host.AddComponent<MobileControlHint>();
            hint._control = control;
            hint.RectTransform.sizeDelta = new Vector2(80f, 80f);
            hint._fallbackLabel = GameUI.Label(host.transform, "PendingControlLabel", "", 22,
                Vector2.zero, Vector2.one, TextAnchor.MiddleCenter);
            hint._fallbackLabel.color = GameUI.Accent;
            hint._fallbackLabel.resizeTextForBestFit = true;
            hint._fallbackLabel.resizeTextMinSize = 12;
            hint._fallbackLabel.resizeTextMaxSize = 22;
            hint.Refresh();
            return hint;
        }

        public bool Refresh()
        {
            if (MobileControlsUI.ActiveInstance != null) _controls = MobileControlsUI.ActiveInstance;
            Button source = GetSource();
            if (source != _source || (_visual == null && source != null))
            {
                Rebuild(source);
            }

            if (_sourceLabel != null && _label != null)
            {
                _label.text = _sourceLabel.text;
                _label.font = _sourceLabel.font;
                _label.color = _sourceLabel.color;
            }
            if (_fallbackLabel != null)
            {
                _fallbackLabel.gameObject.SetActive(!HasVisual);
                if (!HasVisual)
                {
                    string key = _control == Control.Secondary ? "secondary" :
                        _control == Control.Tools ? "tools" : _control == Control.Inventory ? "inventory" : "interact";
                    string fallback = _control == Control.Secondary ? "使う" :
                        _control == Control.Tools ? "道具" : _control == Control.Inventory ? "バッグ" : "調べる";
                    _fallbackLabel.text = LocalizationManager.Resolve("ui.mobile_controls." + key, fallback);
                }
            }
            return HasVisual;
        }

        private void LateUpdate()
        {
            Refresh();
        }

        private Button GetSource()
        {
            if (_controls == null) return null;
            switch (_control)
            {
                case Control.Secondary: return _controls.secondaryInteractButton;
                case Control.Tools: return _controls.toolWheelButton;
                case Control.Inventory: return _controls.inventoryButton;
                default: return _controls.interactButton;
            }
        }

        private void Rebuild(Button source)
        {
            if (_visual != null)
            {
                _visual.SetActive(false);
                Destroy(_visual);
            }
            _visual = null;
            _source = source;
            _sourceLabel = null;
            _label = null;
            if (source == null || !(source.targetGraphic is Image background)) return;

            _visual = new GameObject("Visual", typeof(RectTransform));
            _visual.transform.SetParent(transform, false);
            Stretch((RectTransform)_visual.transform);
            // Unity multiplies the image base color by the Selectable's normal tint.
            CopyImage(background, _visual, background.color * source.colors.normalColor * source.colors.colorMultiplier);

            // Copy only the skin. Do not clone the Button, EventTrigger or optional attention effects.
            foreach (Image image in source.GetComponentsInChildren<Image>(true))
            {
                if (image.transform.parent != source.transform ||
                    (image.name != "ButtonGlow" && image.name != "ButtonRim" && image.name != "Icon")) continue;
                var layer = new GameObject(image.name, typeof(RectTransform));
                layer.transform.SetParent(_visual.transform, false);
                CopyLayout(image.rectTransform, (RectTransform)layer.transform);
                CopyImage(image, layer, image.color);
            }

            _sourceLabel = source.GetComponentInChildren<Text>(true);
            if (_sourceLabel == null) return;
            var labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(_visual.transform, false);
            CopyLayout(_sourceLabel.rectTransform, (RectTransform)labelObject.transform);
            _label = labelObject.AddComponent<Text>();
            _label.alignment = _sourceLabel.alignment;
            _label.fontStyle = _sourceLabel.fontStyle;
            _label.resizeTextForBestFit = true;
            _label.resizeTextMinSize = 10;
            _label.resizeTextMaxSize = _sourceLabel.fontSize;
            _label.horizontalOverflow = HorizontalWrapMode.Wrap;
            _label.verticalOverflow = VerticalWrapMode.Truncate;
            _label.raycastTarget = false;
        }

        private static void CopyImage(Image source, GameObject target, Color color)
        {
            var image = target.AddComponent<Image>();
            image.sprite = source.sprite;
            image.type = source.type;
            image.color = color;
            image.preserveAspect = source.preserveAspect;
            image.raycastTarget = false;
        }

        private static void CopyLayout(RectTransform source, RectTransform target)
        {
            target.anchorMin = source.anchorMin;
            target.anchorMax = source.anchorMax;
            target.pivot = source.pivot;
            target.offsetMin = source.offsetMin;
            target.offsetMax = source.offsetMax;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}

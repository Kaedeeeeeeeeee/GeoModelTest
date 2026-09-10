using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace UISystem
{
    public static class GameUI
    {
        public static readonly Color Surface = new Color(0.055f, 0.12f, 0.16f, 1f);
        public static readonly Color Panel = new Color(0.09f, 0.18f, 0.22f, 1f);
        public static readonly Color Accent = new Color(0.38f, 0.83f, 0.70f);
        public static readonly Color Ink = new Color(0.94f, 0.95f, 0.91f);
        public static readonly Color Muted = new Color(0.65f, 0.76f, 0.77f);

        public static string L(string key) => LocalizationManager.Instance.GetText(key);

        public static Canvas Canvas(string name, int order)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            if (EventSystem.current == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            }
            return canvas;
        }

        public static RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image Box(Transform parent, string name, Color color, Vector2 min, Vector2 max)
        {
            var image = Rect(parent, name, min, max).gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        public static Text Label(Transform parent, string name, string text, int size, Vector2 min, Vector2 max,
            TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var label = Rect(parent, name, min, max).gameObject.AddComponent<Text>();
            label.font = UIFontResolver.GetUIFont();
            label.fontSize = size;
            label.text = text;
            label.alignment = alignment;
            label.color = Ink;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false;
            return label;
        }

        public static Button Button(Transform parent, string name, string label, Vector2 min, Vector2 max,
            Action clicked, bool primary = false)
        {
            var bg = Box(parent, name, primary ? Accent : Panel, min, max);
            var button = bg.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            var colors = button.colors;
            colors.highlightedColor = new Color(0.8f, 1f, 0.93f);
            colors.pressedColor = new Color(0.65f, 0.86f, 0.8f);
            colors.disabledColor = new Color(0.4f, 0.45f, 0.45f, 0.6f);
            button.colors = colors;
            var text = Label(bg.transform, "Label", label, 26, new Vector2(0.04f, 0.08f),
                new Vector2(0.96f, 0.92f), TextAnchor.MiddleCenter);
            text.color = primary ? Surface : Ink;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 18;
            text.resizeTextMaxSize = 26;
            button.onClick.AddListener(() => clicked?.Invoke());
            return button;
        }

        public static void Confirm(string title, string message, string confirm, Action accepted)
        {
            var canvas = Canvas("GameConfirmation", 32767);
            var guard = ModalCanvasLayerGuard.Activate(canvas);
            Core.GameInputState.Scope input = null;
            void Close()
            {
                guard.Dispose();
                input?.Dispose();
                UnityEngine.Object.Destroy(canvas.gameObject);
            }
            input = Core.GameInputState.Acquire(Close);
            Box(canvas.transform, "Dim", new Color(0.01f, 0.03f, 0.05f, 0.88f), Vector2.zero, Vector2.one);
            var panel = Box(canvas.transform, "Card", Surface, new Vector2(0.22f, 0.25f), new Vector2(0.78f, 0.75f));
            Box(panel.transform, "Accent", Accent, new Vector2(0, 0.98f), Vector2.one);
            Label(panel.transform, "Title", title, 38, new Vector2(0.07f, 0.72f), new Vector2(0.93f, 0.92f));
            Label(panel.transform, "Message", message, 25, new Vector2(0.07f, 0.29f), new Vector2(0.93f, 0.70f));
            Button(panel.transform, "Cancel", L("ui.common.cancel"), new Vector2(0.07f, 0.08f), new Vector2(0.47f, 0.23f), Close);
            Button(panel.transform, "Confirm", confirm, new Vector2(0.53f, 0.08f), new Vector2(0.93f, 0.23f), () =>
            {
                Close();
                accepted?.Invoke();
            }, true);
        }
    }
}

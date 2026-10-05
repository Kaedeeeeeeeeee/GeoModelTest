using System;
using Core;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace UISystem
{
    /// <summary>
    /// Safari on iPhone/iPad cannot enter fullscreen, but players can hide its toolbars from the page menu.
    /// Shown once per page load on the title screen, in the same card style as the control guide,
    /// with a real Safari screenshot for the player's Safari generation.
    /// </summary>
    public sealed class SafariToolbarGuide : MonoBehaviour
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern int GeoModelTest_SafariToolbarDevice();
#endif
        /// <summary>
        /// From Safari 27 (iPhone and iPad) 「ツールバーを非表示」 sits near the end of a scrolling page menu;
        /// through Safari 26 it is under 「…」 (さらに表示). Checked in iOS 18.5, 26.5 and 27.0 simulators.
        /// </summary>
        public const int ListMenuSafariVersion = 27;

        private static SafariToolbarGuide _current;
        private static bool _shownThisLoad;
        private GameInputState.Scope _input;
        private ModalCanvasLayerGuard.Scope _layer;

        /// <summary>Tests and screenshots replace the browser check (device * 1000 + Safari version; 0 = none).</summary>
        internal static Func<int> DeviceOverride;

        public static bool IsOpen => _current != null;

        /// <summary>1 = iPhone, 2 = iPad, times 1000, plus the Safari major version; 0 when the hint does not apply.</summary>
        public static int SafariDevice
        {
            get
            {
                if (DeviceOverride != null) return DeviceOverride();
#if UNITY_WEBGL && !UNITY_EDITOR
                try { return GeoModelTest_SafariToolbarDevice(); }
                catch (Exception) { return 0; }
#else
                return 0;
#endif
            }
        }

        /// <summary>Shows the hint the first time the title appears in this page load on iPhone/iPad Safari.</summary>
        public static bool TryShowOnce()
        {
            if (_shownThisLoad || IsOpen || GameInputState.IsModalOpen) return false;
            int device = SafariDevice;
            if (device <= 0) return false;
            _shownThisLoad = true;
            Show(device);
            return true;
        }

        /// <summary>"new" = scrolling list menu, "classic" = the 「…」 page menu.</summary>
        public static string Style(int device) => device % 1000 >= ListMenuSafariVersion ? "new" : "classic";

        internal static void ResetForTests()
        {
            CloseCurrent();
            _shownThisLoad = false;
            DeviceOverride = null;
        }

        public static void Show(int device)
        {
            if (IsOpen) return;
            bool iPad = device / 1000 == 2;
            string style = Style(device);
            var canvas = GameUI.Canvas("SafariToolbarGuide", 32767);
            _current = canvas.gameObject.AddComponent<SafariToolbarGuide>();
            _current._input = GameInputState.Acquire(CloseCurrent);
            _current._layer = ModalCanvasLayerGuard.Activate(canvas);
            GameUI.Box(canvas.transform, "Dim", new Color(0.01f, 0.03f, 0.05f, 0.88f), Vector2.zero, Vector2.one);
            var card = GameUI.Box(canvas.transform, "GuideCard", GameUI.Surface,
                new Vector2(0.055f, 0.04f), new Vector2(0.945f, 0.96f)).transform;
            GameUI.Box(card, "Accent", GameUI.Accent, new Vector2(0, 0.988f), Vector2.one);
            GameUI.Label(card, "Device", GameUI.L(iPad ? "ui.safari.device.ipad" : "ui.safari.device.iphone"), 22,
                new Vector2(0.04f, 0.89f), new Vector2(0.96f, 0.965f)).color = GameUI.Accent;
            GameUI.Label(card, "Title", GameUI.L("ui.safari.title"), 38, new Vector2(0.04f, 0.79f), new Vector2(0.96f, 0.9f));

            // A real Safari screenshot of the same generation, with the two taps marked.
            var shot = GameUI.Box(card, "Screenshot", Color.white, new Vector2(0.03f, 0.14f), new Vector2(0.61f, 0.78f));
            shot.preserveAspect = true;
            shot.raycastTarget = false;
            var texture = Resources.Load<Texture2D>($"UI/SafariToolbar/{(iPad ? "ipad" : "iphone")}-{style}");
            if (texture != null)
                shot.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            else shot.enabled = false;

            var steps = GameUI.Rect(card, "Steps", new Vector2(0.63f, 0.15f), new Vector2(0.97f, 0.77f));
            FitLabel(steps, "Lead", GameUI.L("ui.safari.lead"), 26, new Vector2(0, 0.83f), Vector2.one).color = GameUI.Muted;
            string[] keys = { "ui.safari.step.1", $"ui.safari.{style}.step.2", "ui.safari.step.3" };
            for (int i = 0; i < keys.Length; i++)
            {
                float top = 0.8f - i * 0.225f;
                var row = GameUI.Box(steps, "Step" + (i + 1), GameUI.Panel, new Vector2(0, top - 0.205f), new Vector2(1, top));
                var number = GameUI.Label(row.transform, "Number", (i + 1).ToString(), 34,
                    new Vector2(0.02f, 0), new Vector2(0.14f, 1), TextAnchor.MiddleCenter);
                number.color = GameUI.Accent;
                number.fontStyle = FontStyle.Bold;
                FitLabel(row.transform, "Text", GameUI.L(keys[i]), 26, new Vector2(0.15f, 0.06f), new Vector2(0.97f, 0.94f));
            }
            FitLabel(steps, "Restore", GameUI.L("ui.safari.restore"), 22, Vector2.zero, new Vector2(1, 0.12f)).color = GameUI.Muted;

            GameUI.Button(card, "Close", GameUI.L("ui.safari.ok"),
                new Vector2(0.68f, 0.03f), new Vector2(0.96f, 0.115f), CloseCurrent, true);
        }

        private static Text FitLabel(Transform parent, string name, string text, int size, Vector2 min, Vector2 max)
        {
            var label = GameUI.Label(parent, name, text, size, min, max);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 18;
            label.resizeTextMaxSize = size;
            return label;
        }

        public static void CloseCurrent()
        {
            if (_current == null) return;
            var current = _current;
            _current = null;
            current.ReleaseInput();
            current.gameObject.SetActive(false);
            Destroy(current.gameObject);
        }

        private void ReleaseInput()
        {
            _layer?.Dispose();
            _layer = null;
            _input?.Dispose();
            _input = null;
        }

        private void OnDestroy()
        {
            if (_current == this) _current = null;
            ReleaseInput();
        }
    }
}

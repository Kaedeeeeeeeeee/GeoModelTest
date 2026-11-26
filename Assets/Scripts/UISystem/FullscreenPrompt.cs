using System;
using Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace UISystem
{
    /// <summary>
    /// Offers one tap into browser fullscreen on the title screen (computers, Chromebooks, Android).
    /// Browsers only allow fullscreen inside a user gesture, so the primary button arms the request on press
    /// and the browser's own release event performs it. Apple touch devices get <see cref="SafariToolbarGuide"/>
    /// instead (no page fullscreen on iPhone; iPadOS leaves it on a downward swipe). Shown once per page load.
    /// </summary>
    public sealed class FullscreenPrompt : MonoBehaviour
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern int GeoModelTest_FullscreenAvailability();
        [DllImport("__Internal")] private static extern void GeoModelTest_ArmFullscreen();
        [DllImport("__Internal")] private static extern void GeoModelTest_RequestFullscreen();
#endif
        public const int NotAvailable = 0;
        public const int Pointer = 1;
        public const int Touch = 2;

        private static FullscreenPrompt _current;
        private static bool _shownThisLoad;
        private GameInputState.Scope _input;
        private ModalCanvasLayerGuard.Scope _layer;

        /// <summary>Tests replace the browser check (0 = not offered, 1 = mouse/keyboard, 2 = touch).</summary>
        internal static Func<int> AvailabilityOverride;
        /// <summary>Tests count fullscreen requests instead of calling the browser.</summary>
        internal static Action<string> RequestOverride;

        public static bool IsOpen => _current != null;

        public static int Availability
        {
            get
            {
                if (AvailabilityOverride != null) return AvailabilityOverride();
#if UNITY_WEBGL && !UNITY_EDITOR
                try { return GeoModelTest_FullscreenAvailability(); }
                catch (Exception) { return NotAvailable; }
#else
                return NotAvailable;
#endif
            }
        }

        /// <summary>Shows the card the first time the title appears in this page load, where fullscreen works.</summary>
        public static bool TryShowOnce(Action closed = null)
        {
            if (_shownThisLoad || IsOpen || GameInputState.IsModalOpen) return false;
            int availability = Availability;
            if (availability <= NotAvailable) return false;
            _shownThisLoad = true;
            Show(availability, closed);
            return true;
        }

        internal static void ResetForTests()
        {
            if (_current != null) _current._closed = null;
            CloseCurrent();
            _shownThisLoad = false;
            AvailabilityOverride = null;
            RequestOverride = null;
        }

        private Action _closed;

        public static void Show(int availability, Action closed = null)
        {
            if (IsOpen) return;
            var canvas = GameUI.Canvas("FullscreenPrompt", 32767);
            _current = canvas.gameObject.AddComponent<FullscreenPrompt>();
            _current._closed = closed;
            _current._input = GameInputState.Acquire(CloseCurrent);
            _current._layer = ModalCanvasLayerGuard.Activate(canvas);
            GameUI.Box(canvas.transform, "Dim", new Color(0.01f, 0.03f, 0.05f, 0.88f), Vector2.zero, Vector2.one);
            var card = GameUI.Box(canvas.transform, "Card", GameUI.Surface,
                new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f)).transform;
            GameUI.Box(card, "Accent", GameUI.Accent, new Vector2(0, 0.985f), Vector2.one);
            GameUI.Label(card, "Title", GameUI.L("ui.fullscreen.title"), 40,
                new Vector2(0.07f, 0.74f), new Vector2(0.93f, 0.92f));
            FitLabel(card, "Lead", GameUI.L("ui.fullscreen.lead"), 27, new Vector2(0.07f, 0.47f), new Vector2(0.93f, 0.72f));
            string restore = availability == Touch ? "ui.fullscreen.restore.touch" : "ui.fullscreen.restore.keyboard";
            FitLabel(card, "Restore", GameUI.L(restore), 22, new Vector2(0.07f, 0.29f), new Vector2(0.93f, 0.46f)).color = GameUI.Muted;

            GameUI.Button(card, "Skip", GameUI.L("ui.fullscreen.skip"),
                new Vector2(0.07f, 0.07f), new Vector2(0.45f, 0.22f), CloseCurrent);
            var go = GameUI.Button(card, "Fullscreen", GameUI.L("ui.fullscreen.go"),
                new Vector2(0.5f, 0.07f), new Vector2(0.93f, 0.22f), () =>
                {
                    Request("click");
                    CloseCurrent();
                }, true);
            go.gameObject.AddComponent<ArmOnPress>();
            go.Select();
        }

        private static void Request(string phase)
        {
            if (RequestOverride != null) { RequestOverride(phase); return; }
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                if (phase == "arm") GeoModelTest_ArmFullscreen();
                else GeoModelTest_RequestFullscreen();
            }
            catch (Exception e) { Debug.LogWarning("[FullscreenPrompt] " + e.Message); }
#endif
        }

        /// <summary>Arms the browser-side request on press so the release itself is the user gesture.</summary>
        private sealed class ArmOnPress : MonoBehaviour, IPointerDownHandler
        {
            public void OnPointerDown(PointerEventData eventData) => Request("arm");
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
            var closed = current._closed;
            current.ReleaseInput();
            current.gameObject.SetActive(false);
            Destroy(current.gameObject);
            closed?.Invoke();
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

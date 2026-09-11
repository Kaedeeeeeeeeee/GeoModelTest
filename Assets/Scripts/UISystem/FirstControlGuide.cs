using Core;
using SceneSystem;
using StorySystem;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UISystem
{
    /// <summary>A short, device-specific introduction before the first free movement in a run.</summary>
    public sealed class FirstControlGuide : MonoBehaviour
    {
        public const string CompletedKey = "FirstControlGuide.Completed.v2";
        private static FirstControlGuide _current;
        private GameInputState.Scope _input;
        private ModalCanvasLayerGuard.Scope _layer;

        public static bool IsOpen => _current != null;

        public static bool TryShowForFirstControl()
        {
            if (IsOpen) return true;
            if (PlayerPrefs.GetInt(CompletedKey, 0) == 1 ||
                !StoryDirector.HasFinishedLabIntroduction || StoryDirector.IsStoryPlaybackActive ||
                GameInputState.GameplayBlocked || GameSceneManager.IsLoadingScene ||
                !GameSession.IsGameplayScene(SceneManager.GetActiveScene().name)) return false;

            bool touch = MobileInputManager.IsRuntimeMobileDevice() ||
                (MobileInputManager.Instance != null && MobileInputManager.Instance.desktopTestMode);
            Show(touch);
            return true;
        }

        public static void Show(bool touch)
        {
            if (IsOpen) return;
            var canvas = GameUI.Canvas("FirstControlGuide", 32767);
            _current = canvas.gameObject.AddComponent<FirstControlGuide>();
            Debug.Log("[FirstControlGuide] Shown: " + (touch ? "touch" : "desktop"));
            _current._input = GameInputState.Acquire(_current.Complete);
            _current._layer = ModalCanvasLayerGuard.Activate(canvas);
            GameUI.Box(canvas.transform, "Dim", new Color(0.01f, 0.03f, 0.05f, 0.84f), Vector2.zero, Vector2.one);
            var card = GameUI.Box(canvas.transform, "GuideCard", GameUI.Surface,
                new Vector2(0.055f, 0.04f), new Vector2(0.945f, 0.96f));
            GameUI.Box(card.transform, "Accent", GameUI.Accent, new Vector2(0, 0.988f), Vector2.one);
            var eyebrow = GameUI.Label(card.transform, "Device", GameUI.L(touch ? "ui.guide.touch" : "ui.guide.desktop"),
                22, new Vector2(0.04f, 0.89f), new Vector2(0.96f, 0.965f));
            eyebrow.color = GameUI.Accent;
            GameUI.Label(card.transform, "Title", GameUI.L("ui.guide.title"), 40,
                new Vector2(0.04f, 0.785f), new Vector2(0.96f, 0.90f));
            if (touch)
            {
                ControlGuideDiagram.Create(card.transform, "TouchMap", ControlGuideDiagram.Diagram.TouchMap,
                    new Vector2(0.025f, 0.235f), new Vector2(0.595f, 0.755f));
                var caption = GameUI.Label(card.transform, "MapCaption", GameUI.L("ui.guide.touch.map"), 23,
                    new Vector2(0.04f, 0.18f), new Vector2(0.58f, 0.24f), TextAnchor.MiddleCenter);
                caption.color = GameUI.Muted;
            }
            string[] actions = { "move", "look", "interact", "tools" };
            for (int i = 0; i < actions.Length; i++)
            {
                float x = touch ? 0.615f : (i % 2 == 0 ? 0.04f : 0.515f);
                float y = touch ? 0.625f - i * 0.148f : (i < 2 ? 0.48f : 0.185f);
                var cell = GameUI.Box(card.transform, actions[i], GameUI.Panel,
                    new Vector2(x, y), new Vector2(touch ? 0.96f : x + 0.445f, y + (touch ? 0.135f : 0.27f)));
                Color color = i == 2 ? ControlGuideDiagram.InteractionColor :
                    i == 3 ? ControlGuideDiagram.ToolColor : GameUI.Accent;
                if (touch)
                {
                    var number = GameUI.Label(cell.transform, "Number", (i + 1).ToString(), 30,
                        new Vector2(0.025f, 0.18f), new Vector2(0.13f, 0.85f), TextAnchor.MiddleCenter);
                    number.color = i == 1 ? GameUI.Ink : color;
                }
                else
                {
                    ControlGuideDiagram.Create(cell.transform, "Diagram", (ControlGuideDiagram.Diagram)i,
                        new Vector2(0.025f, 0.10f), new Vector2(0.43f, 0.90f));
                }
                float textX = touch ? 0.16f : 0.47f;
                if (!touch)
                {
                    var label = GameUI.Label(cell.transform, "Action", GameUI.L("ui.guide." + actions[i]), 26,
                        new Vector2(textX, 0.66f), new Vector2(0.97f, 0.94f));
                    label.color = color;
                }
                var detail = GameUI.Label(cell.transform, "Instruction",
                    GameUI.L("ui.guide." + (touch ? "touch." : "desktop.") + actions[i]), touch ? 28 : 26,
                    new Vector2(textX, touch ? 0.06f : 0.18f), new Vector2(0.97f, touch ? 0.94f : 0.63f));
                detail.resizeTextForBestFit = true;
                detail.resizeTextMinSize = touch ? 24 : 22;
                detail.resizeTextMaxSize = touch ? 28 : 26;
            }
            GameUI.Label(card.transform, "Hint", GameUI.L("ui.guide.hint"), 22,
                new Vector2(0.04f, 0.12f), new Vector2(0.96f, 0.17f), TextAnchor.MiddleCenter);
            GameUI.Button(card.transform, "Begin", GameUI.L("ui.guide.begin"),
                new Vector2(0.30f, 0.025f), new Vector2(0.70f, 0.11f), _current.Complete, true);
        }

        public void Complete()
        {
            Debug.Log("[FirstControlGuide] Completed");
            PlayerPrefs.SetInt(CompletedKey, 1);
            PlayerPrefs.Save();
            WebGLFileSync.Flush();
            CloseCurrent();
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

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
        public const string CompletedKey = "FirstControlGuide.Completed.v1";
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
                new Vector2(0.12f, 0.08f), new Vector2(0.88f, 0.92f));
            GameUI.Box(card.transform, "Accent", GameUI.Accent, new Vector2(0, 0.988f), Vector2.one);
            var eyebrow = GameUI.Label(card.transform, "Device", GameUI.L(touch ? "ui.guide.touch" : "ui.guide.desktop"),
                22, new Vector2(0.05f, 0.87f), new Vector2(0.95f, 0.96f));
            eyebrow.color = GameUI.Accent;
            GameUI.Label(card.transform, "Title", GameUI.L("ui.guide.title"), 40,
                new Vector2(0.05f, 0.75f), new Vector2(0.95f, 0.88f));
            string[] actions = { "move", "look", "interact", "tools" };
            for (int i = 0; i < actions.Length; i++)
            {
                float x = i % 2 == 0 ? 0.05f : 0.515f;
                float y = i < 2 ? 0.49f : 0.24f;
                var cell = GameUI.Box(card.transform, actions[i], GameUI.Panel,
                    new Vector2(x, y), new Vector2(x + 0.435f, y + 0.22f));
                var label = GameUI.Label(cell.transform, "Action", GameUI.L("ui.guide." + actions[i]), 25,
                    new Vector2(0.06f, 0.60f), new Vector2(0.94f, 0.92f));
                label.color = GameUI.Accent;
                var detail = GameUI.Label(cell.transform, "Instruction",
                    GameUI.L("ui.guide." + (touch ? "touch." : "desktop.") + actions[i]), 28,
                    new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.59f));
                detail.resizeTextForBestFit = true;
                detail.resizeTextMinSize = 22;
                detail.resizeTextMaxSize = 28;
            }
            GameUI.Label(card.transform, "Hint", GameUI.L("ui.guide.hint"), 22,
                new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.23f));
            GameUI.Button(card.transform, "Begin", GameUI.L("ui.guide.begin"),
                new Vector2(0.27f, 0.035f), new Vector2(0.73f, 0.135f), _current.Complete, true);
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

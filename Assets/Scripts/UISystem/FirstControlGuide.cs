using Core;
using SceneSystem;
using StorySystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UISystem
{
    /// <summary>Device-specific, repeatable help, with a separate first-field introduction.</summary>
    public sealed class FirstControlGuide : MonoBehaviour
    {
        public const string CompletedKey = "FirstControlGuide.Completed.v2";
        public const string FieldCompletedKey = "FirstControlGuide.FieldCompleted.v1";
        public const int PageCount = 4;
        private static FirstControlGuide _current;
        private GameInputState.Scope _input;
        private ModalCanvasLayerGuard.Scope _layer;
        private Transform _card;
        private GameObject _content;
        private Text _title;
        private Text _pageLabel;
        private Button _previous;
        private Button _next;
        private bool _touch;
        private bool _fieldOnly;
        public int Page { get; private set; }
        public static bool IsOpen => _current != null;

        public static bool TryShowForFirstControl()
        {
            if (IsOpen) return true;
            if (PlayerPrefs.GetInt(CompletedKey, 0) == 1 || !CanShowAutomatically()) return false;
            Show(UsesTouch());
            return true;
        }

        public static bool TryShowForFirstField()
        {
            if (PlayerPrefs.GetInt(FieldCompletedKey, 0) == 1 || IsOpen ||
                SceneManager.GetActiveScene().name != "MainScene" || !CanShowAutomatically()) return false;
            ShowInternal(UsesTouch(), true);
            return true;
        }

        private static bool CanShowAutomatically() => StoryDirector.HasFinishedLabIntroduction &&
            !StoryDirector.IsStoryPlaybackActive && !GameInputState.GameplayBlocked &&
            !GameSceneManager.IsLoadingScene && !InventoryUISystem.IsAnyWheelOpen &&
            GameSession.IsGameplayScene(SceneManager.GetActiveScene().name);

        public static bool UsesTouch() => MobileInputManager.IsRuntimeMobileDevice() ||
            (MobileInputManager.Instance != null && MobileInputManager.Instance.desktopTestMode);

        public static void Show(bool touch) => ShowInternal(touch, false);

        private static void ShowInternal(bool touch, bool fieldOnly)
        {
            if (IsOpen) return;
            var canvas = GameUI.Canvas("FirstControlGuide", 32767);
            _current = canvas.gameObject.AddComponent<FirstControlGuide>();
            _current._touch = touch;
            _current._fieldOnly = fieldOnly;
            _current.Page = fieldOnly ? PageCount - 1 : 0;
            _current._input = GameInputState.Acquire(_current.CloseFromEscape);
            _current._layer = ModalCanvasLayerGuard.Activate(canvas);
            GameUI.Box(canvas.transform, "Dim", new Color(0.01f, 0.03f, 0.05f, 0.88f), Vector2.zero, Vector2.one);
            _current._card = GameUI.Box(canvas.transform, "GuideCard", GameUI.Surface,
                new Vector2(0.055f, 0.04f), new Vector2(0.945f, 0.96f)).transform;
            GameUI.Box(_current._card, "Accent", GameUI.Accent, new Vector2(0, 0.988f), Vector2.one);
            var device = GameUI.Label(_current._card, "Device", GameUI.L(touch ? "ui.guide.touch" : "ui.guide.desktop"),
                22, new Vector2(0.04f, 0.89f), new Vector2(0.80f, 0.965f));
            device.color = GameUI.Accent;
            _current._title = GameUI.Label(_current._card, "Title", "", 38,
                new Vector2(0.04f, 0.79f), new Vector2(0.96f, 0.9f));
            _current._pageLabel = GameUI.Label(_current._card, "Page", "", 24,
                new Vector2(0.82f, 0.89f), new Vector2(0.96f, 0.965f), TextAnchor.MiddleRight);
            _current._previous = GameUI.Button(_current._card, "Previous", GameUI.L("ui.guide.previous"),
                new Vector2(0.04f, 0.03f), new Vector2(0.28f, 0.115f), () => _current.ChangePage(-1));
            _current._next = GameUI.Button(_current._card, "Begin", "",
                new Vector2(0.68f, 0.03f), new Vector2(0.96f, 0.115f), () => _current.Advance(), true);
            GameUI.Button(_current._card, "Close", GameUI.L("ui.button.close"),
                new Vector2(0.38f, 0.03f), new Vector2(0.62f, 0.115f), () => _current.CloseFromEscape());
            _current.RenderPage();
        }

        public void ChangePage(int delta)
        {
            Page = Mathf.Clamp(Page + delta, 0, PageCount - 1);
            RenderPage();
        }

        public void Advance()
        {
            if (Page < PageCount - 1) ChangePage(1);
            else Complete();
        }

        private void RenderPage()
        {
            if (_content != null) { _content.SetActive(false); Destroy(_content); }
            _content = GameUI.Rect(_card, "Content", new Vector2(0.04f, 0.16f), new Vector2(0.96f, 0.77f)).gameObject;
            _title.text = GameUI.L("ui.guide.page." + Page);
            _pageLabel.text = _fieldOnly ? "" : $"{Page + 1} / {PageCount}";
            _previous.gameObject.SetActive(!_fieldOnly);
            _previous.interactable = Page > 0;
            _next.GetComponentInChildren<Text>().text = GameUI.L(Page == PageCount - 1 ? "ui.guide.done" : "ui.guide.next");
            if (Page == 0)
            {
                string[] actions = { "move", "look", "jump", "run" };
                for (int i = 0; i < actions.Length; i++)
                    ActionCard(actions[i], "ui.guide." + actions[i], "ui.guide." + (_touch ? "touch." : "desktop.") + actions[i], i);
            }
            else if (Page == 1)
            {
                InfoCard("NewConversation", "!", new Color(1f, 0.8f, 0.2f), "ui.guide.npc.new.title", "ui.guide.npc.new", true);
                InfoCard("Talk", "E", new Color(0.55f, 0.85f, 1f), "ui.guide.npc.talk.title",
                    "ui.guide.npc." + (_touch ? "touch" : "desktop"), false);
            }
            else if (Page == 2)
            {
                string[] actions = { "tools", "use", "put_away", "menus" };
                for (int i = 0; i < actions.Length; i++)
                    ActionCard(actions[i], "ui.guide." + actions[i], "ui.guide." + (_touch ? "touch." : "desktop.") + actions[i], i);
            }
            else
            {
                InfoCard("Guidance", "→", new Color(0.3f, 0.8f, 1f), "ui.guide.field.line.title", "ui.guide.field.line", true);
                InfoCard("Outcrop", "岩", GameUI.Accent, "ui.guide.field.outcrop.title",
                    "ui.guide.field.outcrop." + (_touch ? "touch" : "desktop"), false);
            }
        }

        private void ActionCard(string name, string title, string body, int index)
        {
            float x = index % 2 == 0 ? 0 : 0.52f;
            float y = index < 2 ? 0.53f : 0;
            var cell = GameUI.Box(_content.transform, name, GameUI.Panel, new Vector2(x, y), new Vector2(x + 0.48f, y + 0.47f));
            var heading = GameUI.Label(cell.transform, "Action", GameUI.L(title), 30,
                new Vector2(0.05f, 0.69f), new Vector2(0.95f, 0.94f));
            heading.color = GameUI.Accent;
            FitLabel(cell.transform, "Instruction", GameUI.L(body), new Vector2(0.05f, 0.09f), new Vector2(0.95f, 0.66f));
        }

        private void InfoCard(string name, string symbol, Color color, string title, string body, bool upper)
        {
            float y = upper ? 0.53f : 0;
            var cell = GameUI.Box(_content.transform, name, GameUI.Panel, new Vector2(0, y), new Vector2(1, y + 0.47f));
            var icon = GameUI.Label(cell.transform, "Symbol", _touch && name == "Talk" ? "…" : symbol, 76,
                new Vector2(0.02f, 0.1f), new Vector2(0.14f, 0.9f), TextAnchor.MiddleCenter);
            icon.color = color;
            var heading = GameUI.Label(cell.transform, "Heading", GameUI.L(title), 30,
                new Vector2(0.17f, 0.69f), new Vector2(0.96f, 0.95f));
            heading.color = color;
            FitLabel(cell.transform, "Instruction", GameUI.L(body), new Vector2(0.17f, 0.07f), new Vector2(0.96f, 0.66f));
        }

        private static void FitLabel(Transform parent, string name, string text, Vector2 min, Vector2 max)
        {
            var label = GameUI.Label(parent, name, text, 28, min, max);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 22;
            label.resizeTextMaxSize = 28;
        }

        private void CloseFromEscape()
        {
            // Dismissing is an acknowledgement; help remains available at any time.
            Complete();
        }

        public void Complete()
        {
            PlayerPrefs.SetInt(_fieldOnly ? FieldCompletedKey : CompletedKey, 1);
            PlayerPrefs.Save();
            WebGLFileSync.Flush();
            CloseCurrent();
        }

        public static void DismissCurrent() => _current?.Complete();

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

using UnityEngine;
using UISystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SceneSystem
{
    /// <summary>
    /// 在 StartScene 创建 New Game、设置和退出菜单。
    /// </summary>
    public class StartMenuBootstrap : MonoBehaviour
    {
        [SerializeField]
        private string _startSceneName = "StartScene";

        [SerializeField]
        private Canvas _canvas;

        private Button _newGame;
        private Button _reviewConsent;
        private bool _researchConsentAccepted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureOnStartScene()
        {
            // 防止重复创建
            if (FindFirstObjectByType<StartMenuBootstrap>() != null)
            {
                return;
            }

            var host = new GameObject("StartMenuBootstrap");
            host.AddComponent<StartMenuBootstrap>();
            DontDestroyOnLoad(host);
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += SceneLoaded;
            LocalizationManager.Instance.OnLanguageChanged += RefreshLanguage;
            SceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            LocalizationManager.Instance.OnLanguageChanged -= RefreshLanguage;
        }

        private void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ResearchConsentDialog.CloseCurrent();
            _researchConsentAccepted = false;
            if (_canvas != null) Destroy(_canvas.gameObject);
            _canvas = null;
            if (scene.name == _startSceneName) BuildUI();
        }

        private void RefreshLanguage()
        {
            if (SceneManager.GetActiveScene().name != _startSceneName) return;
            ResearchConsentDialog.CloseCurrent();
            if (_canvas != null) Destroy(_canvas.gameObject);
            BuildUI();
        }

        private void BuildUI()
        {
            _canvas = GameUI.Canvas("StartMenuCanvas", 100);
            _canvas.transform.SetParent(transform, false);
            var background = GameUI.Box(_canvas.transform, "Background", Color.white, Vector2.zero, Vector2.one);
            var texture = Resources.Load<Texture2D>("UI/TitleLandscape");
            if (texture != null)
            {
                background.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                background.color = new Color(0.75f, 0.85f, 0.84f);
            }
            else background.color = GameUI.Surface;
            var menu = GameUI.Box(_canvas.transform, "Menu", new Color(0.03f, 0.09f, 0.12f, 0.94f), Vector2.zero, new Vector2(0.45f, 1f));
            GameUI.Label(menu.transform, "Eyebrow", "G-LAB  /  FIELD RESEARCH", 20, new Vector2(0.14f, 0.89f), new Vector2(0.92f, 0.94f)).color = GameUI.Accent;
            GameUI.Label(menu.transform, "Title", "ジオクエスト", 70, new Vector2(0.13f, 0.73f), new Vector2(0.95f, 0.87f));
            GameUI.Label(menu.transform, "Subtitle", GameUI.L("ui.start.subtitle"), 24, new Vector2(0.14f, 0.63f), new Vector2(0.88f, 0.74f)).color = GameUI.Muted;
            _newGame = GameUI.Button(menu.transform, "NewGame", GameUI.L("ui.start.new_game.title"), new Vector2(0.14f, 0.51f), new Vector2(0.87f, 0.59f), OnNewGameClicked, true);
            _newGame.interactable = _researchConsentAccepted;
            GameUI.Button(menu.transform, "Settings", GameUI.L("ui.start.settings"), new Vector2(0.14f, 0.41f), new Vector2(0.49f, 0.49f), OnOpenSettings);
            GameUI.Button(menu.transform, "Quit", GameUI.L("ui.start.quit"), new Vector2(0.52f, 0.41f), new Vector2(0.87f, 0.49f), OnQuitGame);
            _reviewConsent = GameUI.Button(menu.transform, "ReviewConsent", GameUI.L("ui.consent.review"),
                new Vector2(0.14f, 0.29f), new Vector2(0.87f, 0.37f), ShowResearchConsent);
            _reviewConsent.gameObject.SetActive(!_researchConsentAccepted);
            GameUI.Label(menu.transform, "Footer", GameUI.L("ui.start.footer"), 18, new Vector2(0.14f, 0.06f), new Vector2(0.90f, 0.15f)).color = GameUI.Muted;
            GameUI.Label(_canvas.transform, "FieldCaption", GameUI.L("ui.start.field_caption"), 26, new Vector2(0.53f, 0.07f), new Vector2(0.95f, 0.17f));
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _ = SettingsManager.Instance;
            if (!_researchConsentAccepted) ShowResearchConsent();
        }

        /// <summary>
        /// 检查 PlayerPrefs 里是否存在任何已保存的进度。
        /// 任何一个核心存档键存在就算"有进度"。
        /// </summary>
        private static bool HasSavedProgress() => GameSession.HasProgress;

        private void Update()
        {
            // 在 StartScene 保持鼠标可见与解锁，避免 SettingsManager 关闭时把鼠标锁回去
            var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (active.name == _startSceneName)
            {
                if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }
        }

        private void OnOpenSettings() => SettingsManager.Instance.OpenSettings();

        public void OnQuitGame() => GameSession.Instance.QuitToWebsite();

        /// <summary>
        /// 每次进入主菜单时确认研究参与；同意后仅解锁 New Game，不自动开始游戏。
        /// </summary>
        private void ShowResearchConsent()
        {
            if (Core.GameInputState.IsModalOpen) return;
            ResearchConsentDialog.Show(OnResearchConsentAccepted);
        }

        private void OnResearchConsentAccepted()
        {
            _researchConsentAccepted = true;
            _newGame.interactable = true;
            _reviewConsent.gameObject.SetActive(false);
            _newGame.Select();
        }

        private void OnNewGameClicked()
        {
            if (!_researchConsentAccepted || Core.GameInputState.IsModalOpen) return;
            if (!HasSavedProgress())
            {
                GameSession.Instance.StartNewGame();
                return;
            }

            GameUI.Confirm(GameUI.L("ui.start.new_game.title"),
                GameUI.L("ui.start.new_game.message") + "\n\n" + GameUI.L("ui.start.new_game.warning"),
                GameUI.L("ui.start.new_game.confirm"), () =>
                {
                    GameSession.Instance.StartNewGame();
                });
        }

    }
}

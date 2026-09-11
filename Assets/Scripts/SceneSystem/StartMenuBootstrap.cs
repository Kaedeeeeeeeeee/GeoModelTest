using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Backend;
using UISystem;
using UnityEngine.SceneManagement;

namespace SceneSystem
{
    /// <summary>
    /// 在 StartScene 运行时自动创建简单的开始菜单（Game Start/Settings/Quit）。
    /// 目前仅实现 Game Start：切换到 MainScene。
    /// </summary>
    public class StartMenuBootstrap : MonoBehaviour
    {
        [SerializeField]
        private string _mainSceneName = "MainScene";

        [SerializeField]
        private string _startSceneName = "StartScene";

        [SerializeField]
        private Canvas _canvas;

        [SerializeField]
        private Font _font; // 可在 Inspector 替换；默认使用内置字体

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureOnStartScene()
        {
            var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

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
            if (_canvas != null) Destroy(_canvas.gameObject);
            _canvas = null;
            if (scene.name == _startSceneName) BuildUI();
        }

        private void RefreshLanguage()
        {
            if (SceneManager.GetActiveScene().name != _startSceneName) return;
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
            var next = GameUI.Button(menu.transform, "Continue", GameUI.L("ui.start.continue"), new Vector2(0.14f, 0.51f), new Vector2(0.87f, 0.59f), OnStartGame, true);
            next.interactable = HasSavedProgress();
            GameUI.Button(menu.transform, "NewGame", GameUI.L("ui.start.new_game.title"), new Vector2(0.14f, 0.41f), new Vector2(0.87f, 0.49f), () =>
            {
                if (HasSavedProgress()) OnNewGameClicked();
                else { ProgressResetService.ResetAll(); OnStartGame(); }
            });
            GameUI.Button(menu.transform, "Settings", GameUI.L("ui.start.settings"), new Vector2(0.14f, 0.31f), new Vector2(0.49f, 0.39f), OnOpenSettings);
            GameUI.Button(menu.transform, "Quit", GameUI.L("ui.start.quit"), new Vector2(0.52f, 0.31f), new Vector2(0.87f, 0.39f), OnQuitGame);
            var settings = BackendSettingsProvider.Load();
            if (settings != null && settings.CanShowResearchEntry)
            {
                GameUI.Button(menu.transform, "ResearchEntry", GameUI.L("ui.start.research_test"), new Vector2(0.14f, 0.19f), new Vector2(0.87f, 0.27f), ShowResearchCodeDialog);
            }
            GameUI.Label(menu.transform, "Footer", GameUI.L("ui.start.footer"), 18, new Vector2(0.14f, 0.06f), new Vector2(0.90f, 0.15f)).color = GameUI.Muted;
            GameUI.Label(_canvas.transform, "FieldCaption", GameUI.L("ui.start.field_caption"), 26, new Vector2(0.53f, 0.07f), new Vector2(0.95f, 0.17f));
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _ = SettingsManager.Instance;
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

        private Button CreateButton(Transform parent, string label)
        {
            var go = new GameObject($"Button_{label}");
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(420f, 72f);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);

            var img = go.AddComponent<Image>();
            img.color = GameUI.Panel;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var txtGo = CreateText(go.transform, "Text", label, 30);
            var tRt = txtGo.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.offsetMin = Vector2.zero;
            tRt.offsetMax = Vector2.zero;

            return btn;
        }

        private GameObject CreateText(Transform parent, string name, string content, int fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            var text = go.AddComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.font = _font != null ? _font : UIFontResolver.GetUIFont();
            return go;
        }

        public void OnStartGame() => GameSession.Instance.ContinueGame();

        public void OnQuitGame() => GameSession.Instance.QuitToWebsite();

        /// <summary>
        /// 玩家点击 New Game 按钮。弹确认框，确认后清掉所有 PlayerPrefs 进度并进 MainScene。
        /// </summary>
        private void OnNewGameClicked()
        {
            GameUI.Confirm(GameUI.L("ui.start.new_game.title"),
                GameUI.L("ui.start.new_game.message") + "\n\n" + GameUI.L("ui.start.new_game.warning"),
                GameUI.L("ui.start.new_game.confirm"), () =>
                {
                    ProgressResetService.ResetAll();
                    OnStartGame();
                });
        }

        private void ShowResearchCodeDialog()
        {
            var overlay = new GameObject("ResearchCodeDialog");
            overlay.transform.SetParent(_canvas.transform, false);
            var overlayRect = overlay.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            var overlayImage = overlay.AddComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0.78f);
            overlayImage.raycastTarget = true;

            var panel = new GameObject("Panel");
            panel.transform.SetParent(overlay.transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(760f, 470f);
            var panelImage = panel.AddComponent<Image>();
            panelImage.color = GameUI.Surface;

            var title = CreateText(panel.transform, "Title",
                LocalizationManager.Resolve("ui.start.research_code.title", "研究参加（けんきゅうさんか）コード"), 36);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -58f);
            titleRect.sizeDelta = new Vector2(-64f, 60f);

            var explanation = CreateText(panel.transform, "Explanation",
                LocalizationManager.Resolve("ui.start.research_code.help",
                    "研究者（けんきゅうしゃ）から受（う）け取（と）ったコードを入力（にゅうりょく）してください。通常（つうじょう）のゲームでは入力（にゅうりょく）しません。"), 22);
            var explanationRect = explanation.GetComponent<RectTransform>();
            explanationRect.anchorMin = new Vector2(0.5f, 0.5f);
            explanationRect.anchorMax = new Vector2(0.5f, 0.5f);
            explanationRect.anchoredPosition = new Vector2(0f, 90f);
            explanationRect.sizeDelta = new Vector2(660f, 90f);
            explanation.GetComponent<Text>().horizontalOverflow = HorizontalWrapMode.Wrap;

            var inputObject = new GameObject("ParticipantCodeInput");
            inputObject.transform.SetParent(panel.transform, false);
            var inputRect = inputObject.AddComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0.5f, 0.5f);
            inputRect.anchorMax = new Vector2(0.5f, 0.5f);
            inputRect.anchoredPosition = new Vector2(0f, 5f);
            inputRect.sizeDelta = new Vector2(580f, 68f);
            var inputImage = inputObject.AddComponent<Image>();
            inputImage.color = new Color(0.94f, 0.95f, 0.97f, 1f);
            var input = inputObject.AddComponent<InputField>();
            input.targetGraphic = inputImage;
            input.lineType = InputField.LineType.SingleLine;
            input.characterLimit = 64;

            var inputTextObject = CreateText(inputObject.transform, "Text", string.Empty, 28);
            var inputTextRect = inputTextObject.GetComponent<RectTransform>();
            inputTextRect.anchorMin = Vector2.zero;
            inputTextRect.anchorMax = Vector2.one;
            inputTextRect.offsetMin = new Vector2(20f, 8f);
            inputTextRect.offsetMax = new Vector2(-20f, -8f);
            var inputText = inputTextObject.GetComponent<Text>();
            inputText.color = new Color(0.08f, 0.10f, 0.14f, 1f);
            inputText.alignment = TextAnchor.MiddleLeft;
            input.textComponent = inputText;

            var placeholderObject = CreateText(inputObject.transform, "Placeholder",
                LocalizationManager.Resolve("ui.start.research_code.placeholder", "参加コード"), 26);
            var placeholderRect = placeholderObject.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(20f, 8f);
            placeholderRect.offsetMax = new Vector2(-20f, -8f);
            var placeholder = placeholderObject.GetComponent<Text>();
            placeholder.color = new Color(0.35f, 0.38f, 0.43f, 0.75f);
            placeholder.alignment = TextAnchor.MiddleLeft;
            input.placeholder = placeholder;

            var statusObject = CreateText(panel.transform, "Status", string.Empty, 20);
            var statusRect = statusObject.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.5f, 0f);
            statusRect.anchorMax = new Vector2(0.5f, 0f);
            statusRect.anchoredPosition = new Vector2(0f, 132f);
            statusRect.sizeDelta = new Vector2(650f, 48f);
            var statusText = statusObject.GetComponent<Text>();
            statusText.color = new Color(1f, 0.78f, 0.35f, 1f);

            var submitButton = CreateButton(panel.transform,
                LocalizationManager.Resolve("ui.start.research_code.confirm", "コードを確認（かくにん）"));
            var submitRect = submitButton.GetComponent<RectTransform>();
            submitRect.anchorMin = new Vector2(0.5f, 0f);
            submitRect.anchorMax = new Vector2(0.5f, 0f);
            submitRect.anchoredPosition = new Vector2(-155f, 42f);
            submitRect.sizeDelta = new Vector2(280f, 66f);

            var cancelButton = CreateButton(panel.transform,
                LocalizationManager.Resolve("ui.start.research_code.cancel", "戻（もど）る"));
            var cancelRect = cancelButton.GetComponent<RectTransform>();
            cancelRect.anchorMin = new Vector2(0.5f, 0f);
            cancelRect.anchorMax = new Vector2(0.5f, 0f);
            cancelRect.anchoredPosition = new Vector2(155f, 42f);
            cancelRect.sizeDelta = new Vector2(280f, 66f);

            cancelButton.onClick.AddListener(() => Destroy(overlay));
            submitButton.onClick.AddListener(() =>
            {
                submitButton.interactable = false;
                cancelButton.interactable = false;
                statusText.text = LocalizationManager.Resolve("ui.start.research_code.checking", "コードを確認（かくにん）しています…");
                ResearchParticipationCoordinator.Instance.Activate(input.text, (success, error) =>
                {
                    if (success)
                    {
                        Destroy(overlay);
                        OnStartGame();
                        return;
                    }

                    statusText.text = string.IsNullOrWhiteSpace(error)
                        ? LocalizationManager.Resolve("ui.start.research_code.failed", "コードを確認（かくにん）できませんでした。")
                        : error;
                    submitButton.interactable = true;
                    cancelButton.interactable = true;
                });
            });

            input.ActivateInputField();
        }

        /// <summary>
        /// 在 StartMenu Canvas 里动态构造一个简单的确认对话框（半透明遮罩 + 中央面板 + 两按钮）。
        /// </summary>
        private void ShowConfirmDialog(string title, string message, string confirmLabel, string cancelLabel, System.Action onConfirm)
        {
            // 半透明全屏遮罩
            var overlay = new GameObject("ConfirmDialog");
            overlay.transform.SetParent(_canvas.transform, false);
            var oRt = overlay.AddComponent<RectTransform>();
            oRt.anchorMin = Vector2.zero;
            oRt.anchorMax = Vector2.one;
            oRt.offsetMin = Vector2.zero;
            oRt.offsetMax = Vector2.zero;
            var oImg = overlay.AddComponent<Image>();
            oImg.color = new Color(0f, 0f, 0f, 0.75f);
            oImg.raycastTarget = true; // 拦截背景点击

            // 中央面板
            var panel = new GameObject("Panel");
            panel.transform.SetParent(overlay.transform, false);
            var pRt = panel.AddComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0.5f, 0.5f);
            pRt.anchorMax = new Vector2(0.5f, 0.5f);
            pRt.sizeDelta = new Vector2(720f, 380f);
            pRt.anchoredPosition = Vector2.zero;
            var pImg = panel.AddComponent<Image>();
            pImg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            // 标题
            var titleGo = CreateText(panel.transform, "Title", title, 38);
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -36f);
            titleRt.sizeDelta = new Vector2(-40f, 60f);

            // 消息正文
            var msgGo = CreateText(panel.transform, "Message", message, 22);
            var msgRt = msgGo.GetComponent<RectTransform>();
            msgRt.anchorMin = new Vector2(0f, 0.5f);
            msgRt.anchorMax = new Vector2(1f, 0.5f);
            msgRt.pivot = new Vector2(0.5f, 0.5f);
            msgRt.anchoredPosition = new Vector2(0f, 10f);
            msgRt.sizeDelta = new Vector2(-60f, 130f);
            var msgText = msgGo.GetComponent<Text>();
            msgText.horizontalOverflow = HorizontalWrapMode.Wrap;
            msgText.verticalOverflow = VerticalWrapMode.Overflow;

            // 确认按钮（红色，靠左）
            var confirmBtn = CreateButton(panel.transform, confirmLabel);
            confirmBtn.GetComponent<Image>().color = new Color(0.78f, 0.25f, 0.25f, 1f);
            var cBtnRt = confirmBtn.GetComponent<RectTransform>();
            cBtnRt.anchorMin = new Vector2(0.5f, 0f);
            cBtnRt.anchorMax = new Vector2(0.5f, 0f);
            cBtnRt.pivot = new Vector2(0.5f, 0f);
            cBtnRt.anchoredPosition = new Vector2(-140f, 36f);
            cBtnRt.sizeDelta = new Vector2(260f, 64f);
            confirmBtn.onClick.AddListener(() =>
            {
                Destroy(overlay);
                onConfirm?.Invoke();
            });

            // 取消按钮（灰色，靠右）
            var cancelBtn = CreateButton(panel.transform, cancelLabel);
            cancelBtn.GetComponent<Image>().color = new Color(0.4f, 0.4f, 0.4f, 1f);
            var caBtnRt = cancelBtn.GetComponent<RectTransform>();
            caBtnRt.anchorMin = new Vector2(0.5f, 0f);
            caBtnRt.anchorMax = new Vector2(0.5f, 0f);
            caBtnRt.pivot = new Vector2(0.5f, 0f);
            caBtnRt.anchoredPosition = new Vector2(140f, 36f);
            caBtnRt.sizeDelta = new Vector2(260f, 64f);
            cancelBtn.onClick.AddListener(() => Destroy(overlay));
        }

        private void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
        }
    }
}

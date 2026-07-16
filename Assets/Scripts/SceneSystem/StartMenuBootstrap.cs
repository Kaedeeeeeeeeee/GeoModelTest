using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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
            if (active.name == null || active.name != "StartScene")
            {
                return;
            }

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
            // 仅在 StartScene 构建 UI
            var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (active.name != _startSceneName)
            {
                return;
            }

            BuildUI();

            // 预创建 SettingsManager，确保其 Start 在用户点击前已执行，从而生成设置UI
            var _ = SettingsManager.Instance;
        }

        private void BuildUI()
        {
            EnsureEventSystem();

            // Canvas 根
            var root = new GameObject("StartMenuCanvas");
            root.layer = LayerMask.NameToLayer("UI");
            _canvas = root.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            root.AddComponent<GraphicRaycaster>();

            // 背景
            var bg = new GameObject("Background");
            bg.transform.SetParent(root.transform, false);
            var bgRt = bg.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.6f);

            // 标题
            var title = CreateText(root.transform, "Title", "Geo Model", 48);
            var titleRt = title.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.5f, 1f);
            titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -120f);
            titleRt.sizeDelta = new Vector2(800f, 100f);

            // 按钮容器
            var container = new GameObject("Buttons");
            container.transform.SetParent(root.transform, false);
            var cRt = container.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.5f, 0.5f);
            cRt.anchorMax = new Vector2(0.5f, 0.5f);
            cRt.anchoredPosition = new Vector2(0f, -40f);
            cRt.sizeDelta = new Vector2(520f, 360f);

            float y = 0f;
            float spacing = 90f;

            // 检测玩家是否已有存档：决定是单 "Game Start" 还是 "Continue + New Game"
            bool hasProgress = HasSavedProgress();

            if (hasProgress)
            {
                // Continue（接着上次玩，不清档）
                var continueBtn = CreateButton(container.transform, "Continue");
                continueBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, y);
                continueBtn.onClick.AddListener(OnStartGame);
                y -= spacing;

                // New Game（弹确认对话框 → 清档 → 进 MainScene）
                var newGameBtn = CreateButton(container.transform, "New Game");
                newGameBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, y);
                newGameBtn.onClick.AddListener(OnNewGameClicked);
                y -= spacing;
            }
            else
            {
                // 首次玩或已清档：只显示一个 "Game Start"
                var startBtn = CreateButton(container.transform, "Game Start");
                startBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, y);
                startBtn.onClick.AddListener(OnStartGame);
                y -= spacing;
            }

            // Settings（复用 ESC 设置界面的语言切换UI）
            var settingBtn = CreateButton(container.transform, "Settings");
            var setRt = settingBtn.GetComponent<RectTransform>();
            setRt.anchoredPosition = new Vector2(0f, y);
            settingBtn.onClick.AddListener(OnOpenSettings);
            y -= spacing;

            // Quit Game（桌面平台有效）
            var quitBtn = CreateButton(container.transform, "Quit Game");
            var qRt = quitBtn.GetComponent<RectTransform>();
            qRt.anchoredPosition = new Vector2(0f, y);
            quitBtn.onClick.AddListener(OnQuitGame);
        }

        /// <summary>
        /// 检查 PlayerPrefs 里是否存在任何已保存的进度。
        /// 任何一个核心存档键存在就算"有进度"。
        /// </summary>
        private static bool HasSavedProgress()
        {
            return PlayerPrefs.HasKey("StoryFlags")
                || PlayerPrefs.HasKey("PlayerPersistentData.UnlockedToolIds")
                || PlayerPrefs.HasKey("PlayerPersistentData.Inventory")
                || PlayerPrefs.HasKey("QuestSystem.CompletedObjectives")
                || PlayerPrefs.HasKey("QuestSystem.CompletedQuests");
        }

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

        private void OnOpenSettings()
        {
            var sm = SettingsManager.Instance;
            // 在启动菜单里打开不需要暂停游戏、也不需要禁用玩家（StartScene 通常没有玩家）
            sm.pauseGameWhenOpen = false;
            sm.disablePlayerControlWhenOpen = false;
            // 打开设置界面（复用 ESC 的语言切换UI）
            sm.OpenSettings();

            // 额外确保关闭按钮关闭后仍保留鼠标状态（添加一个跟随的监听）
            if (sm.closeButton != null)
            {
                sm.closeButton.onClick.AddListener(() =>
                {
                    // 关闭后回到启动菜单，应保持鼠标可见
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                });
            }
        }

        private Button CreateButton(Transform parent, string label)
        {
            var go = new GameObject($"Button_{label}");
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(420f, 72f);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.5f, 0.9f, 0.9f);

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

        public void OnStartGame()
        {
            Debug.Log($"[StartMenu] Start -> {_mainSceneName}");
            // 优先使用项目的场景管理器逻辑（含数据恢复、加载UI等）
            // 使用项目的场景管理器（若不存在会在其 Instance 中创建）
            var gsm = GameSceneManager.Instance;
            if (gsm != null)
            {
                gsm.SwitchToScene(_mainSceneName);
            }
            else
            {
                // 兜底：直接加载场景
                UnityEngine.SceneManagement.SceneManager.LoadScene(_mainSceneName);
            }
        }

        public void OnQuitGame()
        {
            Debug.Log("[StartMenu] Quit");
#if UNITY_EDITOR
            // 在编辑器下停止播放
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>
        /// 玩家点击 New Game 按钮。弹确认框，确认后清掉所有 PlayerPrefs 进度并进 MainScene。
        /// </summary>
        private void OnNewGameClicked()
        {
            Debug.Log("[StartMenu] New Game clicked, showing confirm dialog");
            ShowConfirmDialog(
                title: LocalizationManager.Resolve(
                    "ui.start.new_game.title",
                    "最初から始めますか？"),
                message: LocalizationManager.Resolve(
                    "ui.start.new_game.message",
                    "現在の進行状況を消して、最初から始めます。\nストーリー、調査ミッション、持っている試料の記録が消えます。"),
                confirmLabel: LocalizationManager.Resolve(
                    "ui.start.new_game.confirm",
                    "最初から始める"),
                cancelLabel: LocalizationManager.Resolve(
                    "ui.start.new_game.cancel",
                    "戻る"),
                onConfirm: () =>
                {
                    Debug.Log("[StartMenu] Resetting all progress and starting new game");
                    ProgressResetService.ResetAll();
                    OnStartGame();
                }
            );
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

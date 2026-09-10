using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UISystem;
using StorySystem;

namespace QuestSystem
{
    /// <summary>Responsive task card with a persisted ten-step investigation progression.</summary>
    public class QuestUI : MonoBehaviour
    {
        private static bool forceHidden;
        private Canvas _canvas;
        private RectTransform _card;
        private Text _progress;
        private Text _objective;
        private Image _fill;
        private QuestManager _quests;
        private Vector2 _lastScreen;
        private Rect _lastSafeArea;

        private void Start()
        {
            DontDestroyOnLoad(gameObject);
            _canvas = GameUI.Canvas("QuestUICanvas", 2000);
            _canvas.transform.SetParent(transform, false);
            _card = GameUI.Box(_canvas.transform, "QuestPanel", GameUI.Surface, Vector2.up, Vector2.up).rectTransform;
            _card.pivot = new Vector2(0, 1);
            _progress = GameUI.Label(_card, "Progress", "", 32, new Vector2(0.05f, 0.57f), new Vector2(0.95f, 0.94f));
            _progress.color = GameUI.Accent;
            _objective = GameUI.Label(_card, "Objective", "", 26, new Vector2(0.05f, 0.12f), new Vector2(0.95f, 0.58f));
            var track = GameUI.Box(_card, "Track", GameUI.Panel, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.085f));
            _fill = GameUI.Box(track.transform, "Fill", GameUI.Accent, Vector2.zero, Vector2.one);
            _quests = QuestManager.Instance;
            _quests.OnQuestStarted += QuestChanged;
            _quests.OnQuestCompleted += QuestChanged;
            _quests.OnObjectiveCompleted += ObjectiveChanged;
            InvestigationProgress.Changed += RefreshNow;
            LocalizationManager.Instance.OnLanguageChanged += RefreshNow;
            SceneManager.sceneLoaded += SceneChanged;
            RefreshNow();
        }

        private void LateUpdate()
        {
            if (_canvas == null) return;
            _canvas.gameObject.SetActive(!forceHidden && SceneSystem.GameSession.IsGameplayScene(SceneManager.GetActiveScene().name));
            if (_lastScreen != new Vector2(Screen.width, Screen.height) || _lastSafeArea != Screen.safeArea)
                ApplyLayout();
        }

        private void ApplyLayout()
        {
            Canvas.ForceUpdateCanvases();
            float scale = Mathf.Max(0.01f, _canvas.scaleFactor);
            var safe = Screen.safeArea;
            float width = Mathf.Min(540f, safe.width / scale * 0.42f);
            _card.sizeDelta = new Vector2(width, 142f);
            _card.anchoredPosition = new Vector2(safe.xMin / scale + 20, -(Screen.height - safe.yMax) / scale - 20);
            _lastScreen = new Vector2(Screen.width, Screen.height);
            _lastSafeArea = safe;
        }

        private void QuestChanged(Quest quest) => RefreshNow();
        private void ObjectiveChanged(QuestObjective objective) => RefreshNow();
        private void SceneChanged(Scene scene, LoadSceneMode mode) => RefreshNow();

        public void RefreshNow()
        {
            if (_progress == null) return;
            int step = InvestigationProgress.GetStep();
            _progress.text = $"{GameUI.L("quest.ui.progress")}  {step}/{InvestigationProgress.StepTotal}";
            _objective.text = GameUI.L("quest.step." + step);
            _fill.rectTransform.anchorMax = new Vector2((float)step / InvestigationProgress.StepTotal, 1f);
            ApplyLayout();
        }

        public static void SetForceHidden(bool hidden)
        {
            forceHidden = hidden;
            RefreshAll();
        }

        public static void RefreshAll()
        {
            foreach (var ui in FindObjectsByType<QuestUI>(FindObjectsSortMode.None)) ui.RefreshNow();
        }

        private void OnDestroy()
        {
            if (_quests != null)
            {
                _quests.OnQuestStarted -= QuestChanged;
                _quests.OnQuestCompleted -= QuestChanged;
                _quests.OnObjectiveCompleted -= ObjectiveChanged;
            }
            InvestigationProgress.Changed -= RefreshNow;
            LocalizationManager.Instance.OnLanguageChanged -= RefreshNow;
            SceneManager.sceneLoaded -= SceneChanged;
        }
    }
}

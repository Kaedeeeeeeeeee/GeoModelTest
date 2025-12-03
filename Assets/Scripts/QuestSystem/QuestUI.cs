using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace QuestSystem
{
    /// <summary>
    /// 任务UI小面板（MVP）：左上角显示当前任务标题与第一个未完成目标。
    /// </summary>
    public class QuestUI : MonoBehaviour
    {
        [Header("样式")]
        [SerializeField] private Color panelColor = new Color(0f, 0f, 0f, 0.45f);
        [SerializeField] private Color titleColor = Color.white;
        [SerializeField] private Color objectiveColor = new Color(0.9f, 0.9f, 0.9f);
        [SerializeField] private int titleFontSize = 18;
        [SerializeField] private int objectiveFontSize = 14;
        [SerializeField] private float panelScaleMultiplier = 1.5f;
        [SerializeField] private float fontScaleMultiplier = 1.5f;

        [Header("显示场景")]
        [SerializeField] private string[] allowedScenes = new[] { "MainScene", "Laboratory Scene" };

        private GameObject root;
        private GameObject canvasGO;
        private Text titleText;
        private Text objectiveText;
        private LocalizedText titleLoc;
        private LocalizedText objectiveLoc;
        private static bool forceHidden;

        private void Start()
        {
            // 跨场景保留自身与画布
            DontDestroyOnLoad(gameObject);
            CreatePanel();
            UpdateVisibilityByScene();

            // 监听场景切换以控制显示
            SceneManager.sceneLoaded += OnSceneLoaded;
            BindQuestEvents();
            RefreshFromCurrentState();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            var qm = QuestManager.Instance;
            if (qm != null)
            {
                qm.OnQuestStarted -= OnQuestChanged;
                qm.OnObjectiveCompleted -= _ => RefreshFromCurrentState();
                qm.OnQuestCompleted -= OnQuestChanged;
            }
        }

        private void CreatePanel()
        {
            // 始终创建独立的最高层Canvas，避免被其它UI覆盖
            canvasGO = new GameObject("QuestUICanvas");
            var c = canvasGO.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            // 使用较高的sortingOrder，保证位于大多数UI之上但低于字幕
            c.sortingOrder = 32000;
            var scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            DontDestroyOnLoad(canvasGO);

            root = new GameObject("QuestUIPanel");
            root.transform.SetParent(canvasGO.transform, false);

            var rect = root.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(16, -16);
            rect.sizeDelta = new Vector2(420 * panelScaleMultiplier, 88 * panelScaleMultiplier);

            var bg = root.AddComponent<Image>();
            bg.color = panelColor;

            // 标题
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(root.transform, false);
            var titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0, 1);
            titleRect.anchoredPosition = new Vector2(10 * panelScaleMultiplier, -8 * panelScaleMultiplier);
            titleRect.sizeDelta = new Vector2(-20 * panelScaleMultiplier, 26 * panelScaleMultiplier);
            titleText = titleGO.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = Mathf.RoundToInt(titleFontSize * fontScaleMultiplier);
            titleText.color = titleColor;
            titleText.alignment = TextAnchor.UpperLeft;
            titleText.fontStyle = FontStyle.Bold;
            titleLoc = titleGO.AddComponent<LocalizedText>();

            // 目标
            var objGO = new GameObject("Objective");
            objGO.transform.SetParent(root.transform, false);
            var objRect = objGO.AddComponent<RectTransform>();
            objRect.anchorMin = new Vector2(0, 0);
            objRect.anchorMax = new Vector2(1, 1);
            objRect.pivot = new Vector2(0, 1);
            objRect.anchoredPosition = new Vector2(10 * panelScaleMultiplier, -36 * panelScaleMultiplier);
            objRect.sizeDelta = new Vector2(-20 * panelScaleMultiplier, -12 * panelScaleMultiplier);
            objectiveText = objGO.AddComponent<Text>();
            objectiveText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            objectiveText.fontSize = Mathf.RoundToInt(objectiveFontSize * fontScaleMultiplier);
            objectiveText.color = objectiveColor;
            objectiveText.alignment = TextAnchor.UpperLeft;
            objectiveText.fontStyle = FontStyle.Bold;
            objectiveLoc = objGO.AddComponent<LocalizedText>();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            UpdateVisibilityByScene();
        }

        private void UpdateVisibilityByScene()
        {
            if (canvasGO == null) return;
            string current = SceneManager.GetActiveScene().name;
            bool allowed = IsAllowedScene(current) && !forceHidden;
            canvasGO.SetActive(allowed);
        }

        private bool IsAllowedScene(string sceneName)
        {
            if (allowedScenes == null || allowedScenes.Length == 0) return true;
            foreach (var s in allowedScenes)
            {
                if (!string.IsNullOrEmpty(s) && s == sceneName) return true;
            }
            return false;
        }

        private void BindQuestEvents()
        {
            var qm = QuestManager.Instance;
            qm.OnQuestStarted += OnQuestChanged;
            qm.OnObjectiveCompleted += _ => RefreshFromCurrentState();
            qm.OnQuestCompleted += OnQuestChanged;
        }

        private void OnQuestChanged(Quest q)
        {
            RefreshFromCurrentState();
        }

        private void RefreshFromCurrentState()
        {
            var qm = QuestManager.Instance;
            if (qm == null) return;

            if (forceHidden)
            {
                if (root != null) root.SetActive(false);
                if (canvasGO != null) canvasGO.SetActive(false);
                return;
            }

            Quest current = FindQuestToDisplay(qm);

            if (current == null)
            {
                root.SetActive(false);
                return;
            }

            root.SetActive(true);
            // 设置标题本地化键
            titleLoc.SetTextKey(current.titleKey);

            // 找到第一个未完成目标；若都完成，显示“已完成”
            QuestObjective first = null;
            if (current.objectives != null)
            {
                foreach (var o in current.objectives)
                {
                    if (!o.completed) { first = o; break; }
                }
            }

            if (first != null)
            {
                objectiveLoc.SetTextKey(first.titleKey);
            }
            else
            {
                objectiveLoc.SetTextKey("quest.ui.completed");
            }
        }

        /// <summary>
        /// 外部强制隐藏或显示（用于全屏界面遮挡时关闭左上角提示）
        /// </summary>
        public static void SetForceHidden(bool hidden)
        {
            forceHidden = hidden;
            // 尝试立即更新现有实例
            var ui = FindObjectOfType<QuestUI>();
            if (ui != null)
            {
                ui.UpdateVisibilityByScene();
                ui.RefreshFromCurrentState();
            }
        }

        private Quest FindQuestToDisplay(QuestManager qm)
        {
            Quest selected = null;

            IEnumerable<Quest> quests = qm.GetAllQuests();
            if (quests == null) return null;

            foreach (var quest in quests)
            {
                if (quest != null && quest.status == QuestStatus.InProgress)
                {
                    selected = quest;
                    break;
                }
            }

            if (selected != null) return selected;

            foreach (var quest in quests)
            {
                if (quest != null && quest.status == QuestStatus.Completed)
                {
                    selected = quest;
                    break;
                }
            }

            return selected;
        }
    }
}

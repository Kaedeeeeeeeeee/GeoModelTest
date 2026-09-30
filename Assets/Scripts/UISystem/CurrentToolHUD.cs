using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Core;

namespace UISystem
{
    public sealed class CurrentToolHUD : MonoBehaviour
    {
        private ToolManager _tools;
        private GameObject _panel;
        private RectTransform _panelRect;
        private Text _name;
        private Text _caption;
        private Text _switchHint;
        private Image _icon;
        private GameObject _tabKey;

        public static void Attach(ToolManager tools)
        {
            var hud = tools.GetComponent<CurrentToolHUD>();
            if (hud == null) hud = tools.gameObject.AddComponent<CurrentToolHUD>();
            hud._tools = tools;
        }

        private void Start()
        {
            var canvas = GameUI.Canvas("CurrentToolCanvas", 1000);
            canvas.transform.SetParent(transform, false);
            var panel = GameUI.Box(canvas.transform, "CurrentTool", GameUI.Surface, new Vector2(0.68f, 0.03f), new Vector2(0.98f, 0.15f));
            _panel = panel.gameObject;
            _panelRect = panel.rectTransform;
            _icon = GameUI.Box(panel.transform, "Icon", Color.white, new Vector2(0.03f, 0.1f), new Vector2(0.21f, 0.9f));
            _icon.preserveAspect = true;
            _icon.raycastTarget = false;
            _caption = GameUI.Label(panel.transform, "Caption", GameUI.L("ui.tool.current"), 19, new Vector2(0.25f, 0.60f), new Vector2(0.96f, 0.94f));
            _caption.color = GameUI.Accent;
            _name = GameUI.Label(panel.transform, "Name", "", 27, new Vector2(0.25f, 0.07f), new Vector2(0.96f, 0.62f));
            _name.resizeTextForBestFit = true;
            _name.resizeTextMinSize = 16;
            _name.resizeTextMaxSize = 27;

            // Keyboard players switch tools with Tab; show the key right where the tool is listed.
            _tabKey = GameUI.Rect(panel.transform, "TabKey", new Vector2(0.78f, 0.10f), new Vector2(0.97f, 0.90f)).gameObject;
            var key = GameUI.Box(_tabKey.transform, "Key", GameUI.Panel, new Vector2(0.06f, 0.44f), new Vector2(0.94f, 0.98f));
            key.raycastTarget = false;
            var keyOutline = key.gameObject.AddComponent<Outline>();
            keyOutline.effectColor = GameUI.Accent;
            keyOutline.effectDistance = new Vector2(2f, -2f);
            var keyLabel = GameUI.Label(key.transform, "Label", "TAB", 22, Vector2.zero, Vector2.one, TextAnchor.MiddleCenter);
            keyLabel.fontStyle = FontStyle.Bold;
            keyLabel.color = GameUI.Accent;
            _switchHint = GameUI.Label(_tabKey.transform, "Hint", "", 16, Vector2.zero, new Vector2(1f, 0.40f), TextAnchor.MiddleCenter);
            _switchHint.color = GameUI.Muted;
            GameEventBus.ToolEquipped += Refresh;
            LocalizationManager.Instance.OnLanguageChanged += RefreshLanguage;
            RefreshLanguage();
        }

        public static string ToolName(CollectionTool tool)
        {
            if (tool == null) return GameUI.L("tool.empty_hand.name");
            string key = tool.toolID switch
            {
                "1002" => "tool.hammer.name", "1000" => "tool.drill.simple.name", "1001" => "tool.drill_tower.name",
                "999" => "tool.scene_switcher.name", _ => "tool." + tool.toolID + ".name"
            };
            return LocalizationManager.Resolve(key, tool.toolName);
        }

        private void Refresh(string id, string label) => RefreshLanguage();

        private void RefreshLanguage()
        {
            if (_name == null || _tools == null) return;
            var tool = _tools.GetCurrentTool();
            _caption.text = GameUI.L("ui.tool.current");
            _switchHint.text = GameUI.L("ui.tool.switch_hint");
            _name.text = ToolName(tool);
            _icon.sprite = ToolIconResolver.GetIcon(tool);
            _icon.enabled = tool != null;
        }

        private void LateUpdate()
        {
            if (_panelRect != null)
            {
                bool touch = FirstControlGuide.UsesTouch();
                _panelRect.anchorMin = touch ? new Vector2(0.70f, 0.61f) : new Vector2(0.68f, 0.03f);
                _panelRect.anchorMax = touch ? new Vector2(0.98f, 0.73f) : new Vector2(0.98f, 0.15f);
                // Touch players use the on-screen tools button instead of Tab.
                if (_tabKey.activeSelf == touch) _tabKey.SetActive(!touch);
                float textRight = touch ? 0.96f : 0.77f;
                _caption.rectTransform.anchorMax = new Vector2(textRight, 0.94f);
                _name.rectTransform.anchorMax = new Vector2(textRight, 0.62f);
            }
            if (_panel != null) _panel.SetActive(!GameInputState.IsModalOpen && !StorySystem.StoryDirector.IsStoryPlaybackActive &&
                SceneSystem.GameSession.IsGameplayScene(SceneManager.GetActiveScene().name));
        }

        private void OnDestroy()
        {
            GameEventBus.ToolEquipped -= Refresh;
            LocalizationManager.Instance.OnLanguageChanged -= RefreshLanguage;
        }
    }
}

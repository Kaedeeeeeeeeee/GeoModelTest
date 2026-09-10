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
        private Text _name;
        private Text _caption;
        private Image _icon;

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
            var panel = GameUI.Box(canvas.transform, "CurrentTool", GameUI.Surface, new Vector2(0.72f, 0.03f), new Vector2(0.98f, 0.15f));
            _panel = panel.gameObject;
            _icon = GameUI.Box(panel.transform, "Icon", Color.white, new Vector2(0.035f, 0.1f), new Vector2(0.24f, 0.9f));
            _icon.preserveAspect = true;
            _icon.raycastTarget = false;
            _caption = GameUI.Label(panel.transform, "Caption", GameUI.L("ui.tool.current"), 19, new Vector2(0.29f, 0.60f), new Vector2(0.96f, 0.94f));
            _caption.color = GameUI.Accent;
            if (MobileInputManager.IsRuntimeMobileDevice())
            {
                panel.rectTransform.anchorMin = new Vector2(0.70f, 0.76f);
                panel.rectTransform.anchorMax = new Vector2(0.98f, 0.88f);
            }
            _name = GameUI.Label(panel.transform, "Name", "", 27, new Vector2(0.29f, 0.07f), new Vector2(0.96f, 0.62f));
            GameEventBus.ToolEquipped += Refresh;
            LocalizationManager.Instance.OnLanguageChanged += RefreshLanguage;
            RefreshLanguage();
        }

        public static string ToolName(CollectionTool tool)
        {
            if (tool == null) return GameUI.L("ui.tool.none");
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
            _name.text = ToolName(tool);
            _icon.sprite = ToolIconResolver.GetIcon(tool);
            _icon.enabled = tool != null;
        }

        private void LateUpdate()
        {
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

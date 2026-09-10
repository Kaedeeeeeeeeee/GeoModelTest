using UnityEngine;
using UnityEngine.UI;
using Core;
using UISystem;

namespace StorySystem
{
    public sealed class StoryHistoryUI : MonoBehaviour
    {
        private static StoryHistoryUI _current;
        private GameInputState.Scope _input;
        private ModalCanvasLayerGuard.Scope _layer;
        private RectTransform _content;
        private ScrollRect _scroll;

        public static void Show()
        {
            if (_current != null) return;
            var canvas = GameUI.Canvas("StoryHistoryCanvas", 32767);
            _current = canvas.gameObject.AddComponent<StoryHistoryUI>();
            _current.Build(canvas);
        }

        public static void CloseCurrent()
        {
            if (_current != null) Destroy(_current.gameObject);
        }

        private void Build(Canvas canvas)
        {
            _input = GameInputState.Acquire(CloseCurrent);
            _layer = ModalCanvasLayerGuard.Activate(canvas);
            GameUI.Box(transform, "Dim", new Color(0.01f, 0.04f, 0.06f, 0.94f), Vector2.zero, Vector2.one);
            var panel = GameUI.Box(transform, "HistoryPanel", GameUI.Surface, new Vector2(0.1f, 0.06f), new Vector2(0.9f, 0.94f));
            GameUI.Label(panel.transform, "Title", GameUI.L("ui.history.title"), 38, new Vector2(0.05f, 0.86f), new Vector2(0.75f, 0.98f));
            GameUI.Button(panel.transform, "Close", GameUI.L("ui.history.close"), new Vector2(0.76f, 0.89f), new Vector2(0.95f, 0.96f), CloseCurrent);
            string[] scenes = { "", "MainScene", "Laboratory Scene" };
            string[] labels = { "all", "field", "lab" };
            for (int i = 0; i < scenes.Length; i++)
            {
                string scene = scenes[i];
                GameUI.Button(panel.transform, "Filter" + labels[i], GameUI.L("ui.history." + labels[i]),
                    new Vector2(0.05f + i * 0.3f, 0.79f), new Vector2(0.33f + i * 0.3f, 0.85f), () => Populate(scene));
            }
            var viewport = GameUI.Rect(panel.transform, "Viewport", new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.77f));
            viewport.gameObject.AddComponent<Image>().color = GameUI.Surface;
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 35;
            scroll.viewport = viewport;
            var content = GameUI.Rect(viewport, "Content", new Vector2(0, 1), Vector2.one);
            content.pivot = new Vector2(0.5f, 1);
            scroll.content = content;
            _content = content;
            _scroll = scroll;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 16;
            layout.padding = new RectOffset(12, 12, 10, 20);
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Populate("");
        }

        private void Populate(string scene)
        {
            foreach (Transform child in _content)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
            var entries = StoryHistory.Entries;
            int count = 0;
            foreach (var entry in entries)
            {
                if (!string.IsNullOrEmpty(scene) && entry.scene != scene) continue;
                count++;
                string heading = entry.kind == "dialogue" ? GameUI.L("story.speaker.narration") : GameUI.L("ui.history." + entry.kind);
                // Choices are the player's response, even though older saves carry the question author's name.
                if (!string.IsNullOrWhiteSpace(entry.speaker) && entry.kind != "choice")
                {
                    heading = entry.kind == "dialogue" ? entry.speaker : entry.speaker + " · " + heading;
                }
                // Preserve original inline readings in the log. This supports older saves and mixed-language history.
                AddEntry(_content, heading, entry.text);
            }
            if (count == 0) AddEntry(_content, GameUI.L("ui.history.empty"), "");
            Canvas.ForceUpdateCanvases();
            _scroll.verticalNormalizedPosition = 0f;
        }

        private static void AddEntry(Transform content, string heading, string body)
        {
            var row = GameUI.Box(content, "Entry", GameUI.Panel, Vector2.zero, Vector2.one);
            var layout = row.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 16, 20);
            layout.spacing = 8;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            var speaker = GameUI.Label(row.transform, "Speaker", heading, 24, Vector2.zero, Vector2.one);
            speaker.color = GameUI.Accent;
            speaker.gameObject.AddComponent<LayoutElement>().preferredHeight = 34;
            var text = GameUI.Label(row.transform, "Body", body, 27, Vector2.zero, Vector2.one);
            text.verticalOverflow = VerticalWrapMode.Overflow;
            row.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private void OnDestroy()
        {
            _layer?.Dispose();
            _input?.Dispose();
            if (_current == this) _current = null;
        }
    }
}

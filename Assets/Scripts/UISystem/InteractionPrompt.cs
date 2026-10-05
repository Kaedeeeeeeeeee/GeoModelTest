using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UISystem
{
    /// <summary>采样、钻塔和对话共用的交互提示；同一时间只显示优先级最高的一个。</summary>
    public sealed class InteractionPrompt : MonoBehaviour
    {
        public const int ActionFontSize = 30;
        public const float PanelHeight = 160f;
        public static readonly Color Warning = new Color(1f, 0.79f, 0.38f);
        private static readonly List<InteractionPrompt> Prompts = new List<InteractionPrompt>();
        private GameObject _key;
        private Text _keyLabel;
        private bool _requested;
        private bool _touch;
        private readonly Vector3[] _corners = new Vector3[4];
        private int _priority;
        private float _distance;
        public Canvas Canvas { get; private set; }
        public RectTransform Panel { get; private set; }
        public Text Action { get; private set; }
        public MobileControlHint TouchControl { get; private set; }
        public Text TouchInstruction { get; private set; }

        public static InteractionPrompt Create(string canvasName, string panelName, int order,
            MobileControlHint.Control touchControl = MobileControlHint.Control.Interact)
        {
            var canvas = GameUI.Canvas(canvasName, order);
            var prompt = canvas.gameObject.AddComponent<InteractionPrompt>();
            prompt.Canvas = canvas;
            var background = GameUI.Box(canvas.transform, panelName, GameUI.Surface, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            background.raycastTarget = false;
            prompt.Panel = background.rectTransform;
            prompt.Panel.pivot = new Vector2(0.5f, 0f);
            prompt.Panel.anchoredPosition = new Vector2(0f, 180f);
            var outline = background.gameObject.AddComponent<Outline>();
            outline.effectColor = GameUI.Accent;
            outline.effectDistance = new Vector2(1f, -1f);
            var key = GameUI.Box(prompt.Panel, "KeyCap", GameUI.Panel, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
            key.rectTransform.sizeDelta = new Vector2(64f, 64f);
            key.rectTransform.anchoredPosition = new Vector2(56f, 0f);
            key.raycastTarget = false;
            var keyOutline = key.gameObject.AddComponent<Outline>();
            keyOutline.effectColor = GameUI.Accent;
            keyOutline.effectDistance = new Vector2(2f, -2f);
            prompt._key = key.gameObject;
            prompt._keyLabel = GameUI.Label(key.transform, "Label", "", 30, Vector2.zero, Vector2.one, TextAnchor.MiddleCenter);
            prompt._keyLabel.color = GameUI.Accent;
            prompt._keyLabel.fontStyle = FontStyle.Bold;
            prompt.Action = GameUI.Label(prompt.Panel, "PromptText", "", ActionFontSize, Vector2.zero, Vector2.one);
            prompt.TouchControl = MobileControlHint.Create(prompt.Panel, "TouchControl", touchControl);
            prompt.TouchControl.RectTransform.anchorMin = prompt.TouchControl.RectTransform.anchorMax = new Vector2(0f, 0.5f);
            prompt.TouchControl.RectTransform.anchoredPosition = new Vector2(70f, 0f);
            prompt.TouchControl.RectTransform.sizeDelta = new Vector2(100f, 100f);
            prompt.TouchInstruction = GameUI.Label(prompt.Panel, "TouchInstruction", "", ActionFontSize,
                new Vector2(0f, 0.1f), new Vector2(1f, 0.46f));
            prompt.TouchInstruction.color = Warning;
            prompt.SetContent("", "E", false);
            prompt.Panel.gameObject.SetActive(false);
            Prompts.Add(prompt);
            return prompt;
        }

        public void SetContent(string text, string key, bool touch, bool warning = false, bool actionable = true)
        {
            _touch = touch;
            int lineBreak = text.IndexOf('\n');
            Action.text = warning && lineBreak >= 0
                ? "<color=#FFCA61>" + text.Substring(0, lineBreak) + "</color>" + text.Substring(lineBreak)
                : text;
            Action.color = GameUI.Ink;
            _keyLabel.text = key;
            _key.SetActive(!touch && actionable);
            bool touchHint = touch && actionable && TouchControl != null;
            // 最大深度保留警示与动作两行，省略重复的点击说明。
            bool touchInstruction = touchHint && !warning;
            if (TouchControl != null)
            {
                TouchControl.gameObject.SetActive(touchHint);
                TouchInstruction.gameObject.SetActive(touchInstruction);
                if (touchHint)
                {
                    TouchControl.Refresh();
                    TouchInstruction.text = GameUI.L("quest.npc.prompt.control.mobile");
                }
            }
            float left = touchHint ? 140f : !touch && actionable ? 110f : 24f;
            float width = Mathf.Clamp(Action.preferredWidth + left + 28f, touchHint ? 640f : 420f, 1040f);
            // Leave room for the longest touch instruction as well as the action.
            if (touchInstruction) width = Mathf.Clamp(Mathf.Max(width, TouchInstruction.preferredWidth + left + 28f), 640f, 1040f);
            Panel.sizeDelta = new Vector2(width, PanelHeight);
            Action.rectTransform.anchorMin = new Vector2(0f, touchInstruction ? 0.5f : 0f);
            Action.rectTransform.anchorMax = new Vector2(1f, touchInstruction ? 0.94f : 1f);
            Action.rectTransform.offsetMin = new Vector2(left, touchInstruction ? 0f : 18f);
            Action.rectTransform.offsetMax = new Vector2(-24f, touchInstruction ? 0f : -18f);
            if (touchHint)
            {
                TouchInstruction.rectTransform.offsetMin = new Vector2(left, 0f);
                TouchInstruction.rectTransform.offsetMax = new Vector2(-24f, 0f);
            }
        }

        public void SetVisible(bool visible, int priority = 0, float distance = 0f)
        {
            _requested = visible;
            _priority = priority;
            _distance = distance;
            RefreshVisibility();
        }

        private void LateUpdate()
        {
            RefreshVisibility();
            if (!Panel.gameObject.activeSelf) return;
            float bottom = 180f;
            var card = CollectionGuidanceHUD.VisibleCard;
            if (_touch && card != null && card.gameObject.activeInHierarchy)
            {
                // 窄横屏中采集卡较低；提示框向下避让，仍保持下方居中。
                card.GetWorldCorners(_corners);
                var camera = Canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : Canvas.worldCamera;
                Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, _corners[0]);
                Vector2 max = RectTransformUtility.WorldToScreenPoint(camera, _corners[2]);
                float scale = Canvas.scaleFactor;
                float halfWidth = Panel.sizeDelta.x * scale * 0.5f;
                if (min.x < Screen.width * 0.5f + halfWidth && max.x > Screen.width * 0.5f - halfWidth &&
                    min.y < (bottom + PanelHeight) * scale && max.y > bottom * scale)
                    bottom = Mathf.Max(24f, min.y / scale - PanelHeight - 12f);
            }
            Panel.anchoredPosition = new Vector2(0f, bottom);
        }

        private static void RefreshVisibility()
        {
            InteractionPrompt winner = null;
            foreach (var prompt in Prompts)
            {
                if (prompt == null || !prompt._requested || !prompt.gameObject.activeInHierarchy) continue;
                if (winner == null || prompt._priority > winner._priority ||
                    (prompt._priority == winner._priority && prompt._distance < winner._distance)) winner = prompt;
            }
            foreach (var prompt in Prompts)
                if (prompt != null && prompt.Panel != null) prompt.Panel.gameObject.SetActive(prompt == winner);
        }

        private void OnDestroy()
        {
            Prompts.Remove(this);
            RefreshVisibility();
        }
    }
}

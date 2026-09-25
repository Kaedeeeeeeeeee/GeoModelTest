using UnityEngine;
using UnityEngine.UI;

namespace UISystem
{
    /// <summary>A short, non-blocking cue pointing at a newly displayed objective.</summary>
    public sealed class QuestAttentionEffect : MonoBehaviour
    {
        private const float Duration = 3.2f;
        private RectTransform _card;
        private Image _background;
        private RectTransform _ring;
        private CanvasGroup _ringGroup;
        private RectTransform _badge;
        private CanvasGroup _badgeGroup;
        private Text _label;
        private bool _pending;
        private float _delay;
        private float _elapsed;
        private bool _completed;

        public bool IsAnimating { get; private set; }
        public int NotificationCount { get; private set; }

        public void Initialize(RectTransform card)
        {
            _card = card;
            _background = card.GetComponent<Image>();
            _ring = GameUI.Rect(card, "TaskAttentionRing", Vector2.zero, Vector2.one);
            _ringGroup = _ring.gameObject.AddComponent<CanvasGroup>();
            _ringGroup.blocksRaycasts = false;
            _ringGroup.interactable = false;
            AddEdge("Top", new Vector2(0, 1), Vector2.one, new Vector2(0, -3), Vector2.zero);
            AddEdge("Bottom", Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 3));
            AddEdge("Left", Vector2.zero, new Vector2(0, 1), Vector2.zero, new Vector2(3, 0));
            AddEdge("Right", new Vector2(1, 0), Vector2.one, new Vector2(-3, 0), Vector2.zero);

            var badge = GameUI.Box(card, "NewTaskCue", GameUI.Accent, Vector2.one, Vector2.one);
            badge.raycastTarget = false;
            _badge = badge.rectTransform;
            _badge.pivot = Vector2.up;
            _badge.sizeDelta = new Vector2(196, 44);
            _badgeGroup = badge.gameObject.AddComponent<CanvasGroup>();
            _badgeGroup.blocksRaycasts = false;
            _badgeGroup.interactable = false;
            _label = GameUI.Label(badge.transform, "Label", "", 20,
                new Vector2(0.18f, 0), new Vector2(0.96f, 1), TextAnchor.MiddleCenter);
            _label.color = GameUI.Surface;
            _label.resizeTextForBestFit = true;
            _label.resizeTextMinSize = 15;
            _label.resizeTextMaxSize = 20;
            for (int i = 0; i < 2; i++)
            {
                var stroke = GameUI.Box(badge.transform, "Arrow" + i, GameUI.Surface,
                    new Vector2(0, 0.5f), new Vector2(0, 0.5f));
                stroke.raycastTarget = false;
                stroke.rectTransform.sizeDelta = new Vector2(12, 3);
                stroke.rectTransform.anchoredPosition = new Vector2(21, i == 0 ? 4 : -4);
                stroke.rectTransform.localRotation = Quaternion.Euler(0, 0, i == 0 ? 45 : -45);
            }
            Hide();
        }

        private void AddEdge(string name, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            var edge = GameUI.Box(_ring, name, GameUI.Accent, min, max);
            edge.raycastTarget = false;
            edge.rectTransform.offsetMin = offsetMin;
            edge.rectTransform.offsetMax = offsetMax;
        }

        public void NotifyTaskChanged(bool completed)
        {
            _completed = completed;
            _pending = true;
            _delay = 0.25f; // Merge quest-completed / quest-started events from the same transition.
            IsAnimating = false;
            Hide();
        }

        public void Tick(float deltaTime, bool canShow)
        {
            if (_card == null) return;
            if (!canShow)
            {
                if (IsAnimating)
                {
                    _pending = true;
                    _delay = 0.25f;
                    IsAnimating = false;
                }
                Hide();
                return;
            }
            if (_pending)
            {
                _delay -= deltaTime;
                if (_delay > 0) return;
                _pending = false;
                IsAnimating = true;
                _elapsed = 0;
                NotificationCount++;
                _ring.gameObject.SetActive(true);
                _badge.gameObject.SetActive(true);
            }
            if (!IsAnimating) return;
            _elapsed += deltaTime;
            if (_elapsed >= Duration)
            {
                IsAnimating = false;
                Hide();
                return;
            }

            float enter = Mathf.SmoothStep(0, 1, _elapsed / 0.24f);
            float fade = 1 - Mathf.SmoothStep(0, 1, (_elapsed - 2.35f) / 0.85f);
            float strength = enter * fade;
            float pulse = 0.5f + 0.5f * Mathf.Cos(_elapsed * Mathf.PI * 2 / 1.15f);
            _card.localScale = Vector3.one * (1 + 0.025f * strength * pulse);
            _background.color = Color.Lerp(GameUI.Surface, new Color(0.10f, 0.28f, 0.28f), strength * (0.4f + 0.6f * pulse));
            float spread = Mathf.Repeat(_elapsed / 1.15f, 1);
            float inset = 3 + 12 * spread;
            _ring.offsetMin = Vector2.one * -inset;
            _ring.offsetMax = Vector2.one * inset;
            _ringGroup.alpha = strength * (1 - spread) * 0.85f;
            _badgeGroup.alpha = strength;
            _badge.anchoredPosition = new Vector2(14 + 26 * (1 - enter) + 5 * (1 - pulse), -84);
            _label.text = GameUI.L(_completed ? "quest.ui.completed_cue" : "quest.ui.new_objective");
        }

        private void Hide()
        {
            if (_card != null) _card.localScale = Vector3.one;
            if (_background != null) _background.color = GameUI.Surface;
            if (_ring != null) _ring.gameObject.SetActive(false);
            if (_badge != null) _badge.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            if (IsAnimating) _pending = true;
            IsAnimating = false;
            Hide();
        }
    }
}

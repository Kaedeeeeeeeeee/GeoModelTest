using UnityEngine;
using UnityEngine.UI;

namespace UISystem
{
    /// <summary>
    /// Short confirmation shown above gameplay and dialogue. It never receives pointer input,
    /// so it cannot block the dialogue, tools or virtual controls underneath.
    /// </summary>
    public sealed class GameToast : MonoBehaviour
    {
        private const float Duration = 2.8f;
        private const float FadeOut = 0.4f;
        private static GameToast _instance;
        private GameObject _toast;
        private CanvasGroup _group;
        private Text _message;
        private float _shownAt;

        public static void Show(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            if (_instance == null) _instance = Create();
            _instance._message.text = message;
            _instance._shownAt = Time.unscaledTime;
            _instance._group.alpha = 1f;
            _instance._toast.SetActive(true);
        }

        private static GameToast Create()
        {
            // Above the dialogue canvas (32766) so the confirmation stays readable when a story line opens.
            var canvas = GameUI.Canvas("GameToastCanvas", 32767);
            DontDestroyOnLoad(canvas.gameObject);
            Destroy(canvas.GetComponent<GraphicRaycaster>());
            var toast = canvas.gameObject.AddComponent<GameToast>();

            var box = GameUI.Box(canvas.transform, "Toast", GameUI.Surface, new Vector2(0.31f, 0.70f), new Vector2(0.69f, 0.79f));
            box.raycastTarget = false;
            var outline = box.gameObject.AddComponent<Outline>();
            outline.effectColor = GameUI.Accent;
            outline.effectDistance = new Vector2(2f, -2f);
            var check = GameUI.Label(box.transform, "Check", "✓", 40, new Vector2(0.02f, 0f), new Vector2(0.16f, 1f), TextAnchor.MiddleCenter);
            check.color = GameUI.Accent;
            toast._message = GameUI.Label(box.transform, "Message", "", 30, new Vector2(0.15f, 0.05f), new Vector2(0.97f, 0.95f), TextAnchor.MiddleCenter);
            toast._message.resizeTextForBestFit = true;
            toast._message.resizeTextMinSize = 18;
            toast._message.resizeTextMaxSize = 30;
            toast._group = box.gameObject.AddComponent<CanvasGroup>();
            toast._group.blocksRaycasts = false;
            toast._group.interactable = false;
            toast._toast = box.gameObject;
            toast._toast.SetActive(false);
            return toast;
        }

        private void Update()
        {
            if (_toast == null || !_toast.activeSelf) return;
            float age = Time.unscaledTime - _shownAt;
            _group.alpha = Mathf.Clamp01((Duration - age) / FadeOut);
            if (age >= Duration) _toast.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}

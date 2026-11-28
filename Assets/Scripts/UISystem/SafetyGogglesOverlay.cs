using UnityEngine;
using UnityEngine.UI;

namespace UISystem
{
    /// <summary>
    /// A goggle-frame vignette shown for a moment when the rock hammer comes out: geologists put on
    /// safety glasses before striking rock. Purely visual; it never receives input.
    /// </summary>
    public sealed class SafetyGogglesOverlay : MonoBehaviour
    {
        public const float FadeIn = 0.15f;
        public const float Hold = 0.95f;
        public const float FadeOut = 0.45f;

        private static SafetyGogglesOverlay _instance;
        private static Texture2D _mask;
        private CanvasGroup _group;
        private float _shownAt;

        public static bool IsShowing => _instance != null;

        public static void Flash()
        {
            if (!Application.isPlaying) return;
            if (_instance == null) _instance = Create();
            _instance._shownAt = Time.unscaledTime;
        }

        private static SafetyGogglesOverlay Create()
        {
            // Below the interaction prompts, toast and dialogue; above the 3D view.
            var canvas = GameUI.Canvas("SafetyGogglesCanvas", 40);
            Destroy(canvas.GetComponent<GraphicRaycaster>());
            var overlay = canvas.gameObject.AddComponent<SafetyGogglesOverlay>();
            overlay._group = canvas.gameObject.AddComponent<CanvasGroup>();
            overlay._group.blocksRaycasts = false;
            overlay._group.interactable = false;
            overlay._group.alpha = 0f;

            var frame = GameUI.Rect(canvas.transform, "Frame", Vector2.zero, Vector2.one).gameObject.AddComponent<RawImage>();
            frame.texture = MaskTexture();
            frame.raycastTarget = false;

            // 画面中央の少し上：左上の課題カード・採集カードや上部の「新しい目標」ボタンと重ならない位置
            var caption = GameUI.Label(canvas.transform, "Caption", GameUI.L("hammer.goggles.on"), 30,
                new Vector2(0.37f, 0.62f), new Vector2(0.63f, 0.70f), TextAnchor.MiddleCenter);
            caption.resizeTextForBestFit = true;
            caption.resizeTextMinSize = 14;
            caption.resizeTextMaxSize = 30;
            var shadow = caption.gameObject.AddComponent<Outline>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
            return overlay;
        }

        /// <summary>Opacity of the frame <paramref name="age"/> seconds after it was shown.</summary>
        public static float AlphaAt(float age)
        {
            if (age < 0f) return 0f;
            if (age < FadeIn) return age / FadeIn;
            if (age < FadeIn + Hold) return 1f;
            return Mathf.Clamp01(1f - (age - FadeIn - Hold) / FadeOut);
        }

        private void Update()
        {
            float age = Time.unscaledTime - _shownAt;
            _group.alpha = AlphaAt(age);
            if (age >= FadeIn + Hold + FadeOut) Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        /// <summary>Dark frame with one wide clear lens and a nose notch, generated once.</summary>
        private static Texture2D MaskTexture()
        {
            if (_mask != null) return _mask;
            const int width = 256;
            const int height = 144;
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float u = (x + 0.5f) / width * 2f - 1f;
                    float v = (y + 0.5f) / height * 2f - 1f;
                    // Superellipse lens; the notch lifts the lower rim in the middle like a nose bridge.
                    float notch = 0.22f * Mathf.Exp(-(u * u) / 0.03f);
                    float lower = v < 0f ? 0.90f - notch : 0.90f;
                    float d = Mathf.Pow(Mathf.Abs(u) / 0.955f, 4f) + Mathf.Pow(Mathf.Abs(v) / lower, 4f);
                    float rim = Mathf.SmoothStep(0.78f, 1.02f, d);
                    float alpha = Mathf.Lerp(0.03f, 0.9f, rim);
                    // A thin lighter band just inside the rim reads as the edge of the lens.
                    float edge = Mathf.Exp(-Mathf.Pow((d - 0.80f) / 0.05f, 2f)) * 0.5f;
                    byte shade = (byte)Mathf.Lerp(18f, 120f, edge);
                    pixels[y * width + x] = new Color32(shade, (byte)(shade + 6), (byte)(shade + 10), (byte)(alpha * 255f));
                }
            }

            _mask = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "SafetyGogglesMask",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            _mask.SetPixels32(pixels);
            _mask.Apply(false, true);
            return _mask;
        }
    }
}

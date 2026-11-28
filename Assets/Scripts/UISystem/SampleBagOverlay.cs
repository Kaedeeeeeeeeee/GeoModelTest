using UnityEngine;
using UnityEngine.UI;

namespace UISystem
{
    /// <summary>
    /// Shows the picked-up sample going into a labelled sample bag, the way field samples are
    /// recorded (locality, number, date). It appears beside the success toast and takes no input,
    /// so it adds no step to the collection flow.
    /// </summary>
    public sealed class SampleBagOverlay : MonoBehaviour
    {
        public const float SlideIn = 0.2f;
        public const float Hold = 1.3f;
        public const float FadeOut = 0.35f;

        private static SampleBagOverlay _instance;
        private CanvasGroup _group;
        private RectTransform _card;
        private Image _swatch;
        private Text _title;
        private Text _detail;
        private float _shownAt;

        public static string LastTitle { get; private set; }
        public static string LastDetail { get; private set; }

        /// <summary>First label line for a sample: locality and running number, or the core number.</summary>
        public static string TitleFor(string toolId, int site, int number)
        {
            if (toolId == "1001") return string.Format(GameUI.L("sample.bag.label.core"), number);
            if (site > 0) return string.Format(GameUI.L("sample.bag.label.site"), SiteLetter(site), number);
            return string.Format(GameUI.L("sample.bag.label.free"), number);
        }

        /// <summary>Second label line: depth range for a core, collection date for a hand sample.</summary>
        public static string DetailFor(string toolId, float depthStart, float depthEnd, System.DateTime date)
        {
            if (toolId == "1001") return string.Format(GameUI.L("sample.bag.label.depth"), depthStart, depthEnd);
            return date.ToString("yyyy/MM/dd");
        }

        public static string SiteLetter(int site)
        {
            return ((char)('A' + Mathf.Clamp(site, 1, 26) - 1)).ToString();
        }

        public static void Show(SampleItem sample, int site, int number)
        {
            if (sample == null || !Application.isPlaying) return;
            LastTitle = TitleFor(sample.sourceToolID, site, number);
            LastDetail = DetailFor(sample.sourceToolID, sample.depthStart, sample.depthEnd, System.DateTime.Now);
            if (_instance == null) _instance = Create();
            _instance._title.text = LastTitle;
            _instance._detail.text = LastDetail;
            _instance._swatch.sprite = sample.previewIcon;
            _instance._swatch.color = sample.previewIcon != null ? Color.white : SwatchColor(sample);
            _instance._shownAt = Time.unscaledTime;
            _instance._card.gameObject.SetActive(true);
        }

        private static Color SwatchColor(SampleItem sample)
        {
            if (sample.geologicalLayers != null && sample.geologicalLayers.Count > 0)
            {
                Color colour = sample.geologicalLayers[0].layerColor;
                colour.a = 1f;
                return colour;
            }
            return new Color(0.62f, 0.55f, 0.47f);
        }

        private static SampleBagOverlay Create()
        {
            // Just under the dialogue (32766) and toast (32767) canvases.
            var canvas = GameUI.Canvas("SampleBagCanvas", 32760);
            DontDestroyOnLoad(canvas.gameObject);
            Destroy(canvas.GetComponent<GraphicRaycaster>());
            var overlay = canvas.gameObject.AddComponent<SampleBagOverlay>();

            // The clear zip bag.
            var bag = GameUI.Box(canvas.transform, "SampleBag", new Color(0.92f, 0.97f, 1f, 0.30f),
                new Vector2(0.39f, 0.49f), new Vector2(0.61f, 0.675f));
            bag.raycastTarget = false;
            var outline = bag.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.75f);
            outline.effectDistance = new Vector2(2f, -2f);
            overlay._card = bag.rectTransform;
            overlay._group = bag.gameObject.AddComponent<CanvasGroup>();
            overlay._group.blocksRaycasts = false;
            overlay._group.interactable = false;
            GameUI.Box(bag.transform, "Zip", new Color(0.30f, 0.62f, 0.90f, 0.95f),
                new Vector2(0.03f, 0.86f), new Vector2(0.97f, 0.91f)).raycastTarget = false;

            overlay._swatch = GameUI.Box(bag.transform, "Sample", Color.white,
                new Vector2(0.07f, 0.12f), new Vector2(0.37f, 0.78f));
            overlay._swatch.preserveAspect = true;
            overlay._swatch.raycastTarget = false;

            // The handwritten label.
            var label = GameUI.Box(bag.transform, "Label", new Color(0.97f, 0.96f, 0.90f, 1f),
                new Vector2(0.42f, 0.16f), new Vector2(0.95f, 0.74f));
            label.raycastTarget = false;
            var ink = new Color(0.13f, 0.16f, 0.22f);
            overlay._title = GameUI.Label(label.transform, "Title", "", 28, new Vector2(0.06f, 0.48f),
                new Vector2(0.94f, 0.94f), TextAnchor.MiddleCenter);
            overlay._detail = GameUI.Label(label.transform, "Detail", "", 22, new Vector2(0.06f, 0.08f),
                new Vector2(0.94f, 0.48f), TextAnchor.MiddleCenter);
            foreach (Text text in new[] { overlay._title, overlay._detail })
            {
                text.color = ink;
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 12;
                text.resizeTextMaxSize = text.fontSize;
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
            }
            bag.gameObject.SetActive(false);
            return overlay;
        }

        private void Update()
        {
            if (_card == null || !_card.gameObject.activeSelf) return;
            float age = Time.unscaledTime - _shownAt;
            float appear = Mathf.Clamp01(age / SlideIn);
            float fade = Mathf.Clamp01((SlideIn + Hold + FadeOut - age) / FadeOut);
            _group.alpha = Mathf.Min(appear, fade);
            // Drops in from slightly above, like being slipped into the bag.
            _card.anchoredPosition = new Vector2(0f, (1f - Mathf.SmoothStep(0f, 1f, appear)) * 36f);
            if (age >= SlideIn + Hold + FadeOut) _card.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}

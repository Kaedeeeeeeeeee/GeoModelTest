using UnityEngine;
using UnityEngine.UI;

namespace UISystem
{
    /// <summary>
    /// Depth read-out shown while the boring rig is drilling: the current depth in metres and a column
    /// that fills, top to bottom, with the colour of each layer the bit passes through.
    /// </summary>
    public sealed class DrillDepthGauge : MonoBehaviour
    {
        private const float LingerSeconds = 0.9f;

        private static DrillDepthGauge _instance;
        private RectTransform _column;
        private RectTransform _bit;
        private Text _depth;
        private Text _top;
        private Text _bottom;
        private Image[] _segments = new Image[0];
        private float[] _fractions = new float[0];
        private float _depthStart;
        private float _depthEnd;
        private float _hideAt = -1f;

        public static bool IsVisible => _instance != null && _instance.gameObject.activeSelf;
        public static float Progress { get; private set; }

        public static string DepthText(float depthStart, float depthEnd, float progress)
        {
            return string.Format(GameUI.L("drill_tower.gauge.depth"), Mathf.Lerp(depthStart, depthEnd, Mathf.Clamp01(progress)));
        }

        /// <summary>
        /// Share of the column, 0..1 from the top, that a segment currently fills.
        /// </summary>
        public static void VisibleSpan(float[] fractions, int index, float progress, out float from, out float to)
        {
            from = 0f;
            for (int i = 0; i < index; i++) from += fractions[i];
            to = Mathf.Min(from + fractions[index], Mathf.Clamp01(progress));
            if (to < from) to = from;
        }

        public static void Begin(float depthStart, float depthEnd, Color[] colours, float[] fractions)
        {
            if (!Application.isPlaying) return;
            if (_instance == null) _instance = Create();
            DrillDepthGauge gauge = _instance;
            gauge._depthStart = depthStart;
            gauge._depthEnd = depthEnd;
            gauge._hideAt = -1f;
            gauge._top.text = string.Format("{0:0.#} m", depthStart);
            gauge._bottom.text = string.Format("{0:0.#} m", depthEnd);

            foreach (Image old in gauge._segments)
            {
                if (old != null) Destroy(old.gameObject);
            }
            int count = colours != null && fractions != null ? Mathf.Min(colours.Length, fractions.Length) : 0;
            gauge._segments = new Image[count];
            gauge._fractions = new float[count];
            for (int i = 0; i < count; i++)
            {
                Color colour = colours[i];
                colour.a = 1f;
                gauge._fractions[i] = fractions[i];
                gauge._segments[i] = GameUI.Box(gauge._column, "Layer" + i, colour, Vector2.zero, Vector2.zero);
                gauge._segments[i].raycastTarget = false;
            }
            gauge._bit.SetAsLastSibling();
            gauge.gameObject.SetActive(true);
            SetProgress(0f);
        }

        public static void SetProgress(float progress)
        {
            Progress = Mathf.Clamp01(progress);
            if (_instance == null) return;
            DrillDepthGauge gauge = _instance;
            gauge._depth.text = DepthText(gauge._depthStart, gauge._depthEnd, Progress);
            for (int i = 0; i < gauge._segments.Length; i++)
            {
                VisibleSpan(gauge._fractions, i, Progress, out float from, out float to);
                RectTransform rect = gauge._segments[i].rectTransform;
                rect.anchorMin = new Vector2(0f, 1f - to);
                rect.anchorMax = new Vector2(1f, 1f - from);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
            }
            gauge._bit.anchorMin = new Vector2(-0.25f, 1f - Progress - 0.006f);
            gauge._bit.anchorMax = new Vector2(1.25f, 1f - Progress + 0.006f);
            gauge._bit.offsetMin = gauge._bit.offsetMax = Vector2.zero;
        }

        /// <summary>Leaves the finished column on screen for a moment, then hides it.</summary>
        public static void Finish()
        {
            if (_instance == null) return;
            SetProgress(1f);
            _instance._hideAt = Time.unscaledTime + LingerSeconds;
        }

        public static void Hide()
        {
            if (_instance != null) _instance.gameObject.SetActive(false);
        }

        private static DrillDepthGauge Create()
        {
            var canvas = GameUI.Canvas("DrillDepthGaugeCanvas", 30);
            Destroy(canvas.GetComponent<GraphicRaycaster>());
            var gauge = canvas.gameObject.AddComponent<DrillDepthGauge>();

            var panel = GameUI.Box(canvas.transform, "Panel", new Color(GameUI.Surface.r, GameUI.Surface.g, GameUI.Surface.b, 0.86f),
                new Vector2(0.025f, 0.36f), new Vector2(0.125f, 0.80f));
            panel.raycastTarget = false;
            gauge._depth = GameUI.Label(panel.transform, "Depth", "", 26, new Vector2(0.04f, 0.87f), new Vector2(0.96f, 0.99f),
                TextAnchor.MiddleCenter);
            gauge._depth.resizeTextForBestFit = true;
            gauge._depth.resizeTextMinSize = 14;
            gauge._depth.resizeTextMaxSize = 26;
            gauge._top = GameUI.Label(panel.transform, "Top", "", 18, new Vector2(0.52f, 0.80f), new Vector2(0.98f, 0.86f));
            gauge._bottom = GameUI.Label(panel.transform, "Bottom", "", 18, new Vector2(0.52f, 0.03f), new Vector2(0.98f, 0.09f));
            gauge._top.color = gauge._bottom.color = GameUI.Muted;

            var hole = GameUI.Box(panel.transform, "Hole", new Color(0.02f, 0.05f, 0.07f, 1f),
                new Vector2(0.16f, 0.05f), new Vector2(0.46f, 0.85f));
            hole.raycastTarget = false;
            gauge._column = hole.rectTransform;
            var bit = GameUI.Box(hole.transform, "Bit", GameUI.Accent, Vector2.zero, Vector2.zero);
            bit.raycastTarget = false;
            gauge._bit = bit.rectTransform;
            return gauge;
        }

        private void Update()
        {
            if (_hideAt > 0f && Time.unscaledTime >= _hideAt)
            {
                _hideAt = -1f;
                gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}

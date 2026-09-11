using UnityEngine;
using UnityEngine.UI;

namespace UISystem
{
    /// <summary>Resolution-independent control illustrations; never receives gameplay input.</summary>
    public sealed class ControlGuideDiagram : MaskableGraphic
    {
        public enum Diagram { MoveKeys, Mouse, InteractKeys, ToolKey, TouchMap }
        public Diagram Kind { get; private set; }
        private VertexHelper _mesh;
        private Rect _rect;
        private static readonly Color Outline = new Color(0.53f, 0.69f, 0.72f);
        private static readonly Color KeyFill = new Color(0.17f, 0.30f, 0.34f);
        public static readonly Color InteractionColor = new Color(1f, 0.76f, 0.40f);
        public static readonly Color ToolColor = new Color(0.70f, 0.65f, 1f);

        public static ControlGuideDiagram Create(Transform parent, string name, Diagram kind, Vector2 min, Vector2 max)
        {
            var diagram = GameUI.Rect(parent, name, min, max).gameObject.AddComponent<ControlGuideDiagram>();
            diagram.Kind = kind;
            diagram.raycastTarget = false;
            diagram.AddLabels();
            diagram.SetVerticesDirty();
            return diagram;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            _mesh = vh;
            _rect = rectTransform.rect;
            if (_rect.width <= 0 || _rect.height <= 0) return;
            switch (Kind)
            {
                case Diagram.MoveKeys:
                    Key(0.37f, 0.56f, 0.26f, 0.35f, GameUI.Accent);
                    Key(0.07f, 0.13f, 0.26f, 0.35f, GameUI.Accent);
                    Key(0.37f, 0.13f, 0.26f, 0.35f, GameUI.Accent);
                    Key(0.67f, 0.13f, 0.26f, 0.35f, GameUI.Accent);
                    break;
                case Diagram.Mouse:
                    Rounded(0.34f, 0.16f, 0.32f, 0.69f, 0.16f, Outline);
                    Rounded(0.355f, 0.18f, 0.29f, 0.65f, 0.145f, KeyFill);
                    Line(P(0.35f, 0.59f), P(0.65f, 0.59f), 2f, Outline);
                    Line(P(0.5f, 0.6f), P(0.5f, 0.81f), 2f, Outline);
                    Rounded(0.48f, 0.62f, 0.04f, 0.13f, 0.02f, GameUI.Accent);
                    Arrow(P(0.24f, 0.50f), P(0.07f, 0.50f), GameUI.Accent);
                    Arrow(P(0.76f, 0.50f), P(0.93f, 0.50f), GameUI.Accent);
                    Arrow(P(0.78f, 0.63f), P(0.78f, 0.86f), GameUI.Accent);
                    Arrow(P(0.78f, 0.38f), P(0.78f, 0.15f), GameUI.Accent);
                    break;
                case Diagram.InteractKeys:
                    Key(0.10f, 0.29f, 0.35f, 0.47f, InteractionColor);
                    Key(0.55f, 0.29f, 0.35f, 0.47f, InteractionColor);
                    break;
                case Diagram.ToolKey:
                    Key(0.10f, 0.33f, 0.65f, 0.44f, ToolColor);
                    // A cursor selecting a tool after opening the wheel.
                    Triangle(P(0.76f, 0.46f), P(0.80f, 0.12f), P(0.92f, 0.26f), GameUI.Ink);
                    Line(P(0.83f, 0.25f), P(0.92f, 0.12f), 5f, GameUI.Ink);
                    break;
                case Diagram.TouchMap: DrawPhone(); break;
            }
        }

        private void AddLabels()
        {
            switch (Kind)
            {
                case Diagram.MoveKeys:
                    KeyLabel("W", 0.37f, 0.56f, 0.26f, 0.35f);
                    KeyLabel("A", 0.07f, 0.13f, 0.26f, 0.35f);
                    KeyLabel("S", 0.37f, 0.13f, 0.26f, 0.35f);
                    KeyLabel("D", 0.67f, 0.13f, 0.26f, 0.35f);
                    break;
                case Diagram.InteractKeys:
                    KeyLabel("E", 0.10f, 0.29f, 0.35f, 0.47f);
                    KeyLabel("F", 0.55f, 0.29f, 0.35f, 0.47f);
                    break;
                case Diagram.ToolKey: KeyLabel("Tab", 0.10f, 0.33f, 0.65f, 0.44f); break;
                case Diagram.TouchMap:
                    Marker("1", 0.26f, 0.25f, GameUI.Accent);
                    Marker("2", 0.72f, 0.75f, GameUI.Ink);
                    Marker("3", 0.94f, 0.43f, InteractionColor);
                    Marker("4", 0.33f, 0.72f, ToolColor);
                    PhoneLabel("ToolsLabel", "ui.mobile_controls.tools", 0.19f, 0.60f, 0.17f, 0.12f);
                    PhoneLabel("InteractLabel", "ui.mobile_controls.interact", 0.80f, 0.285f, 0.17f, 0.12f);
                    PhoneLabel("UseLabel", "ui.mobile_controls.secondary", 0.65f, 0.17f, 0.17f, 0.12f);
                    break;
            }
        }

        private void KeyLabel(string key, float x, float y, float width, float height)
        {
            GameUI.Label(transform, "Key" + key, key, 38, new Vector2(x, y + 0.025f),
                new Vector2(x + width, y + height), TextAnchor.MiddleCenter);
        }

        private void Marker(string number, float x, float y, Color ink)
        {
            var label = GameUI.Label(transform, "Marker" + number, number, 31,
                new Vector2(x - 0.032f, y - 0.055f), new Vector2(x + 0.032f, y + 0.055f), TextAnchor.MiddleCenter);
            label.color = ink;
        }

        private void PhoneLabel(string name, string key, float x, float y, float width, float height)
        {
            var label = GameUI.Label(transform, name, GameUI.L(key), 22,
                new Vector2(x, y), new Vector2(x + width, y + height), TextAnchor.MiddleCenter);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 18;
            label.resizeTextMaxSize = 22;
        }

        private void DrawPhone()
        {
            Rounded(0.02f, 0.065f, 0.96f, 0.86f, 0.055f, Outline);
            Rounded(0.026f, 0.075f, 0.948f, 0.84f, 0.05f, new Color(0.02f, 0.06f, 0.085f));
            Rounded(0.061f, 0.12f, 0.878f, 0.76f, 0.026f, GameUI.Panel);
            Rounded(0.039f, 0.40f, 0.008f, 0.18f, 0.004f, Outline);
            // Faint scene lines distinguish the game view from the surrounding phone.
            Line(P(0.10f, 0.43f), P(0.9f, 0.43f), 1.3f, KeyFill);
            Line(P(0.43f, 0.43f), P(0.29f, 0.14f), 1.3f, KeyFill);
            Line(P(0.57f, 0.43f), P(0.71f, 0.14f), 1.3f, KeyFill);
            Circle(P(0.50f, 0.55f), 0.036f, Outline);
            Rounded(0.48f, 0.41f, 0.04f, 0.085f, 0.015f, Outline);

            // Left joystick, with a displaced thumb and a direction arrow.
            Circle(P(0.155f, 0.25f), 0.101f, GameUI.Accent);
            Circle(P(0.155f, 0.25f), 0.091f, KeyFill);
            Circle(P(0.155f, 0.28f), 0.043f, GameUI.Ink);
            Arrow(P(0.155f, 0.34f), P(0.155f, 0.44f), GameUI.Accent);
            Circle(P(0.26f, 0.25f), 0.047f, GameUI.Surface);

            // The right-side gesture is a finger contact, not a mouse.
            Rounded(0.58f, 0.46f, 0.29f, 0.26f, 0.025f, new Color(0.16f, 0.29f, 0.33f));
            Arrow(P(0.69f, 0.59f), P(0.60f, 0.59f), GameUI.Ink);
            Arrow(P(0.77f, 0.59f), P(0.86f, 0.59f), GameUI.Ink);
            Circle(P(0.73f, 0.59f), 0.035f, GameUI.Accent);
            Circle(P(0.73f, 0.59f), 0.025f, GameUI.Panel);
            Line(P(0.73f, 0.48f), P(0.73f, 0.58f), 10f, GameUI.Ink);
            Circle(P(0.73f, 0.58f), 0.014f, GameUI.Ink);
            Circle(P(0.72f, 0.75f), 0.047f, GameUI.Surface);

            // These labels and regions match the shipped mobile controls.
            Rounded(0.19f, 0.60f, 0.17f, 0.12f, 0.04f, ToolColor * new Color(0.45f, 0.45f, 0.45f, 1));
            Circle(P(0.33f, 0.72f), 0.047f, GameUI.Surface);
            Rounded(0.80f, 0.285f, 0.17f, 0.12f, 0.04f, new Color(0.41f, 0.31f, 0.18f));
            Rounded(0.65f, 0.17f, 0.17f, 0.12f, 0.04f, new Color(0.41f, 0.31f, 0.18f));
            Circle(P(0.94f, 0.43f), 0.047f, GameUI.Surface);
        }

        private Vector2 P(float x, float y) => new Vector2(_rect.xMin + x * _rect.width, _rect.yMin + y * _rect.height);
        private float Scale => Mathf.Min(_rect.width, _rect.height);

        private void Key(float x, float y, float width, float height, Color outline)
        {
            Rounded(x, y - 0.035f, width, height, 0.035f, new Color(0.025f, 0.07f, 0.09f));
            Rounded(x, y, width, height, 0.035f, outline);
            Rounded(x + 0.012f, y + 0.018f, width - 0.024f, height - 0.036f, 0.027f, KeyFill);
        }

        private void Rounded(float x, float y, float width, float height, float radius, Color ink)
        {
            var a = P(x, y);
            var b = P(x + width, y + height);
            float r = Mathf.Min(radius * Scale, Mathf.Min(b.x - a.x, b.y - a.y) * 0.5f);
            Vector2 center = (a + b) * 0.5f;
            Vector2 previous = Vector2.zero;
            Vector2 first = Vector2.zero;
            for (int corner = 0; corner < 4; corner++)
            {
                Vector2 c = new Vector2(corner == 0 || corner == 3 ? b.x - r : a.x + r,
                    corner < 2 ? b.y - r : a.y + r);
                for (int step = 0; step <= 6; step++)
                {
                    float angle = (corner * 90f + step * 15f) * Mathf.Deg2Rad;
                    Vector2 point = c + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
                    if (corner == 0 && step == 0) first = point;
                    else Triangle(center, previous, point, ink);
                    previous = point;
                }
            }
            Triangle(center, previous, first, ink);
        }

        private void Circle(Vector2 center, float radius, Color ink)
        {
            float r = radius * Scale;
            for (int i = 0; i < 40; i++)
            {
                float a = i * Mathf.PI / 20f;
                float b = (i + 1) * Mathf.PI / 20f;
                Triangle(center, center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r,
                    center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * r, ink);
            }
        }

        private void Arrow(Vector2 from, Vector2 to, Color ink)
        {
            Line(from, to, 3f, ink);
            Vector2 direction = (to - from).normalized;
            Vector2 side = new Vector2(-direction.y, direction.x);
            float length = Scale * 0.045f;
            Line(to, to - direction * length + side * length * 0.65f, 3f, ink);
            Line(to, to - direction * length - side * length * 0.65f, 3f, ink);
        }

        private void Line(Vector2 from, Vector2 to, float thickness, Color ink)
        {
            Vector2 direction = (to - from).normalized;
            Vector2 side = new Vector2(-direction.y, direction.x) * thickness * 0.5f;
            Triangle(from - side, from + side, to + side, ink);
            Triangle(from - side, to + side, to - side, ink);
        }

        private void Triangle(Vector2 a, Vector2 b, Vector2 c, Color ink)
        {
            int start = _mesh.currentVertCount;
            _mesh.AddVert(a, ink, Vector2.zero);
            _mesh.AddVert(b, ink, Vector2.zero);
            _mesh.AddVert(c, ink, Vector2.zero);
            _mesh.AddTriangle(start, start + 1, start + 2);
        }
    }
}

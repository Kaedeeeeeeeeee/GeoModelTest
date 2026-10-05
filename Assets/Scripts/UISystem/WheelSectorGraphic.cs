using UnityEngine;
using UnityEngine.UI;

namespace UISystem
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class WheelSectorGraphic : MaskableGraphic
    {
        private float startAngle;
        private float span = 360f;
        private float innerRadius;
        private float outerRadius;
        private float feather = 1f;

        protected WheelSectorGraphic()
        {
            useLegacyMeshGeneration = false;
        }

        public void Configure(float start, float angle, float inner, float outer, float edge)
        {
            startAngle = start;
            span = angle;
            innerRadius = inner;
            outerRadius = outer;
            feather = Mathf.Min(edge, (outer - inner) * 0.5f);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (outerRadius <= innerRadius) return;
            int segments = Mathf.Max(2, Mathf.CeilToInt(span / 2f));
            float[] radii = { innerRadius, innerRadius + feather, outerRadius - feather, outerRadius };
            for (int step = 0; step <= segments; step++)
            {
                Vector2 direction = ToolWheelLayout.Direction(startAngle + span * step / segments);
                for (int ring = 0; ring < radii.Length; ring++)
                {
                    var tint = color;
                    if (ring == 3 || (ring == 0 && innerRadius > 0f)) tint.a = 0f;
                    mesh.AddVert(direction * radii[ring], tint, Vector2.zero);
                }
                if (step == 0) continue;
                int current = step * 4;
                for (int ring = 0; ring < 3; ring++)
                {
                    mesh.AddTriangle(current + ring - 4, current + ring + 1, current + ring);
                    mesh.AddTriangle(current + ring - 4, current + ring - 3, current + ring + 1);
                }
            }
        }
    }
}

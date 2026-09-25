using UnityEngine;
using UnityEngine.UI;

namespace UISystem
{
    /// <summary>A font-independent magnifier that stays crisp at any canvas scale.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MagnifierIcon : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = GetPixelAdjustedRect();
            float size = Mathf.Min(rect.width, rect.height);
            Vector2 center = rect.center + new Vector2(-0.1f, 0.1f) * size;
            float radius = size * 0.28f;
            float width = size * 0.075f;
            for (int i = 0; i < 24; i++)
            {
                float a = i * Mathf.PI * 2f / 24f;
                float b = (i + 1) * Mathf.PI * 2f / 24f;
                AddLine(mesh, center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius,
                    center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius, width);
            }
            AddLine(mesh, center + new Vector2(0.20f, -0.20f) * size,
                center + new Vector2(0.47f, -0.47f) * size, width);
        }

        private void AddLine(VertexHelper mesh, Vector2 start, Vector2 end, float width)
        {
            Vector2 direction = (end - start).normalized;
            Vector2 normal = new Vector2(-direction.y, direction.x) * width * 0.5f;
            int index = mesh.currentVertCount;
            mesh.AddVert(start - normal, color, Vector2.zero);
            mesh.AddVert(start + normal, color, Vector2.zero);
            mesh.AddVert(end + normal, color, Vector2.zero);
            mesh.AddVert(end - normal, color, Vector2.zero);
            mesh.AddTriangle(index, index + 1, index + 2);
            mesh.AddTriangle(index, index + 2, index + 3);
        }
    }
}

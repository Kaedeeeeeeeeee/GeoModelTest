using UnityEngine;
using UnityEngine.UI;

namespace UISystem
{
    /// <summary>A checkmark that remains visible independently of the active language's font.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ConsentCheckmark : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var rect = GetPixelAdjustedRect();
            AddStroke(mesh, rect, new Vector2(0.16f, 0.48f), new Vector2(0.40f, 0.25f));
            AddStroke(mesh, rect, new Vector2(0.40f, 0.25f), new Vector2(0.85f, 0.78f));
        }

        private void AddStroke(VertexHelper mesh, Rect rect, Vector2 start, Vector2 end)
        {
            start = rect.min + Vector2.Scale(start, rect.size);
            end = rect.min + Vector2.Scale(end, rect.size);
            Vector2 direction = (end - start).normalized;
            Vector2 offset = new Vector2(-direction.y, direction.x) * Mathf.Min(rect.width, rect.height) * 0.06f;
            var points = new[] { start - offset, start + offset, end + offset, end - offset };
            var vertices = new UIVertex[4];
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = UIVertex.simpleVert;
                vertices[i].position = points[i];
                vertices[i].color = color;
            }
            mesh.AddUIVertexQuad(vertices);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Short-lived rock chips and drill cuttings. They are plain meshes that reuse the struck layer's
/// own material, so the colour always matches the rock and no particle shader is needed in the build.
/// </summary>
public class DebrisBurst : MonoBehaviour
{
    private struct Chip
    {
        public Transform Transform;
        public Vector3 Velocity;
        public Vector3 Spin;
        public Vector3 Scale;
        public float Life;
        public float MaxLife;
    }

    public const int MaxChips = 96;

    private static DebrisBurst runner;
    private static Mesh chipMesh;
    private readonly List<Chip> chips = new List<Chip>();

    public static int ActiveChipCount => runner != null ? runner.chips.Count : 0;

    public static void Emit(Vector3 position, Vector3 normal, Material material, int count, float speed, float size,
        float life = 0.8f)
    {
        if (!Application.isPlaying || count <= 0) return;
        if (material == null) material = FieldToolModels.GetMaterial("dark");
        if (material == null) return;
        if (normal.sqrMagnitude < 0.0001f) normal = Vector3.up;
        normal.Normalize();

        if (runner == null)
        {
            runner = new GameObject("FieldToolDebris").AddComponent<DebrisBurst>();
        }

        Mesh mesh = ChipMesh();
        for (int i = 0; i < count && runner.chips.Count < MaxChips; i++)
        {
            var chipObject = new GameObject("RockChip");
            chipObject.transform.SetParent(runner.transform, false);
            chipObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = chipObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            float chipSize = size * Random.Range(0.5f, 1.3f);
            var scale = new Vector3(chipSize, chipSize * Random.Range(0.4f, 0.8f), chipSize * Random.Range(0.6f, 1f));
            Vector3 direction = (normal + Random.onUnitSphere * 0.8f).normalized;
            chipObject.transform.position = position + normal * 0.02f + Random.insideUnitSphere * size;
            chipObject.transform.rotation = Random.rotation;
            chipObject.transform.localScale = scale;

            float chipLife = life * Random.Range(0.7f, 1.1f);
            runner.chips.Add(new Chip
            {
                Transform = chipObject.transform,
                Velocity = direction * (speed * Random.Range(0.5f, 1.2f)) + Vector3.up * (speed * 0.3f),
                Spin = Random.insideUnitSphere * 720f,
                Scale = scale,
                Life = chipLife,
                MaxLife = chipLife
            });
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        for (int i = chips.Count - 1; i >= 0; i--)
        {
            Chip chip = chips[i];
            chip.Life -= dt;
            if (chip.Transform == null || chip.Life <= 0f)
            {
                if (chip.Transform != null) Destroy(chip.Transform.gameObject);
                chips.RemoveAt(i);
                continue;
            }

            chip.Velocity += Physics.gravity * dt;
            chip.Transform.position += chip.Velocity * dt;
            chip.Transform.Rotate(chip.Spin * dt, Space.Self);
            // Shrink away over the last third instead of popping out.
            chip.Transform.localScale = chip.Scale * Mathf.Clamp01(chip.Life / (chip.MaxLife * 0.33f));
            chips[i] = chip;
        }
    }

    void OnDestroy()
    {
        if (runner == this) runner = null;
    }

    /// <summary>A small flat-shaded shard, about one unit across.</summary>
    public static Mesh ChipMesh()
    {
        if (chipMesh != null) return chipMesh;

        Vector3[] corners =
        {
            new Vector3(0.5f, -0.2f, 0.3f), new Vector3(-0.5f, -0.2f, 0.35f),
            new Vector3(0.05f, -0.2f, -0.5f), new Vector3(0f, 0.4f, 0.05f)
        };
        int[][] faces = { new[] { 0, 1, 2 }, new[] { 0, 1, 3 }, new[] { 1, 2, 3 }, new[] { 2, 0, 3 } };
        Vector3 centre = (corners[0] + corners[1] + corners[2] + corners[3]) / 4f;

        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        foreach (int[] face in faces)
        {
            Vector3 a = corners[face[0]], b = corners[face[1]], c = corners[face[2]];
            // Unity's front face has cross(b - a, c - a) pointing outward.
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), a - centre) < 0f)
            {
                Vector3 swap = b;
                b = c;
                c = swap;
            }
            int start = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
        }

        chipMesh = new Mesh { name = "RockChip" };
        chipMesh.SetVertices(vertices);
        chipMesh.SetTriangles(triangles, 0);
        chipMesh.RecalculateNormals();
        chipMesh.RecalculateBounds();
        return chipMesh;
    }
}

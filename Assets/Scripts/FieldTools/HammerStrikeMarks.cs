using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// What a rock face looks like while it is being hammered: hairline cracks after the first blow,
/// longer cracks after the second, and a brighter fresh face where the chunk came off.
/// </summary>
public class HammerStrikeMarks : MonoBehaviour
{
    public const int MaxScars = 6;
    public const float SurfaceOffset = 0.006f;

    private static readonly List<HammerStrikeMarks> Scars = new List<HammerStrikeMarks>();
    private static readonly Color CrackColor = new Color(0.10f, 0.085f, 0.075f, 0.92f);

    private readonly List<GameObject> cracks = new List<GameObject>();
    private Material lineMaterial;
    private Material freshMaterial;
    private Mesh freshMesh;
    private int seed;

    public int Stage { get; private set; }
    public int CrackCount => cracks.Count;
    public bool HasFreshFace => freshMesh != null;

    public static HammerStrikeMarks Create(Vector3 point, Vector3 normal)
    {
        if (normal.sqrMagnitude < 0.0001f) normal = Vector3.up;
        normal.Normalize();
        var marksObject = new GameObject("HammerStrikeMarks");
        marksObject.transform.position = point + normal * SurfaceOffset;
        // Local +Z is the surface normal, so everything is drawn in the local XY plane.
        Vector3 up = Mathf.Abs(normal.y) > 0.9f ? Vector3.forward : Vector3.up;
        marksObject.transform.rotation = Quaternion.LookRotation(normal, up);
        var marks = marksObject.AddComponent<HammerStrikeMarks>();
        marks.seed = Random.Range(1, int.MaxValue);
        return marks;
    }

    /// <summary>1 = hairline cracks, 2 = long cracks, 3 = the chunk is gone and a fresh face shows.</summary>
    public void ShowStage(int stage, Material rockMaterial)
    {
        Stage = Mathf.Clamp(stage, 1, 3);
        ClearCracks();

        var random = new System.Random(seed);
        int count = Stage == 1 ? 3 : 5;
        float length = Stage == 1 ? 0.07f : Stage == 2 ? 0.13f : 0.17f;
        float width = Stage == 1 ? 0.005f : 0.008f;
        float inner = Stage == 3 ? 0.085f : 0.008f;
        float baseAngle = (float)random.NextDouble() * 360f;
        for (int i = 0; i < count; i++)
        {
            float angle = baseAngle + i * (360f / count) + ((float)random.NextDouble() - 0.5f) * 40f;
            float reach = length * (0.7f + (float)random.NextDouble() * 0.6f);
            cracks.Add(CreateCrack(angle, inner, Mathf.Max(reach, inner + 0.03f), width, random));
        }

        if (Stage == 3)
        {
            CreateFreshFace(rockMaterial, random);
            RegisterScar();
        }
    }

    /// <summary>Removes marks left by a collection that was abandoned before the rock broke.</summary>
    public void Dismiss()
    {
        if (this == null) return;
        if (Application.isPlaying) Destroy(gameObject);
        else DestroyImmediate(gameObject);
    }

    private void RegisterScar()
    {
        Scars.RemoveAll(scar => scar == null);
        if (!Scars.Contains(this)) Scars.Add(this);
        while (Scars.Count > MaxScars)
        {
            HammerStrikeMarks oldest = Scars[0];
            Scars.RemoveAt(0);
            if (oldest != null) oldest.Dismiss();
        }
    }

    private GameObject CreateCrack(float angleDegrees, float from, float to, float width, System.Random random)
    {
        var crack = new GameObject("Crack");
        crack.transform.SetParent(transform, false);
        var line = crack.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.alignment = LineAlignment.TransformZ;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.numCapVertices = 0;
        line.widthMultiplier = width;
        line.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.6f, 0.7f), new Keyframe(1f, 0.1f));
        // The colour lives on the material: unlit fallbacks ignore LineRenderer vertex colours.
        if (lineMaterial == null) lineMaterial = FieldToolModels.CreateLineMaterial(CrackColor);
        if (lineMaterial != null) line.sharedMaterial = lineMaterial;

        const int points = 5;
        float radians = angleDegrees * Mathf.Deg2Rad;
        var direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        var side = new Vector2(-direction.y, direction.x);
        line.positionCount = points;
        for (int i = 0; i < points; i++)
        {
            float along = Mathf.Lerp(from, to, i / (float)(points - 1));
            float wobble = i == 0 ? 0f : ((float)random.NextDouble() - 0.5f) * (to - from) * 0.28f;
            Vector2 p = direction * along + side * wobble;
            line.SetPosition(i, new Vector3(p.x, p.y, 0f));
        }
        return crack;
    }

    private void CreateFreshFace(Material rockMaterial, System.Random random)
    {
        if (freshMesh != null) return;

        const int sides = 7;
        var vertices = new Vector3[sides + 1];
        var normals = new Vector3[sides + 1];
        vertices[0] = new Vector3(0f, 0f, 0.002f);
        normals[0] = Vector3.forward;
        for (int i = 0; i < sides; i++)
        {
            float angle = (i / (float)sides) * Mathf.PI * 2f;
            float radius = 0.085f * (0.8f + (float)random.NextDouble() * 0.4f);
            vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0.002f);
            normals[i + 1] = Vector3.forward;
        }

        // Both windings, so the patch shows whichever way the importer oriented the face.
        var triangles = new int[sides * 6];
        for (int i = 0; i < sides; i++)
        {
            int a = i + 1;
            int b = (i + 1) % sides + 1;
            triangles[i * 6] = 0;
            triangles[i * 6 + 1] = a;
            triangles[i * 6 + 2] = b;
            triangles[i * 6 + 3] = 0;
            triangles[i * 6 + 4] = b;
            triangles[i * 6 + 5] = a;
        }

        freshMesh = new Mesh { name = "FreshRockFace" };
        freshMesh.vertices = vertices;
        freshMesh.normals = normals;
        freshMesh.triangles = triangles;
        freshMesh.RecalculateBounds();

        var face = new GameObject("FreshFace");
        face.transform.SetParent(transform, false);
        face.AddComponent<MeshFilter>().sharedMesh = freshMesh;
        MeshRenderer renderer = face.AddComponent<MeshRenderer>();
        renderer.shadowCastingMode = ShadowCastingMode.Off;

        // Unweathered rock is paler than the crust around it.
        if (rockMaterial != null)
        {
            freshMaterial = new Material(rockMaterial) { name = "FreshRockFace" };
        }
        else
        {
            Shader shader = FieldToolModels.FindLitShader();
            if (shader != null) freshMaterial = new Material(shader) { name = "FreshRockFace" };
        }
        if (freshMaterial != null)
        {
            Color fresh = Color.Lerp(FieldToolModels.ReadColor(rockMaterial, new Color(0.6f, 0.55f, 0.5f)), Color.white, 0.38f);
            fresh.a = 1f;
            if (freshMaterial.HasProperty("_BaseColor")) freshMaterial.SetColor("_BaseColor", fresh);
            if (freshMaterial.HasProperty("_Color")) freshMaterial.SetColor("_Color", fresh);
            renderer.sharedMaterial = freshMaterial;
        }
    }

    private void ClearCracks()
    {
        foreach (GameObject crack in cracks)
        {
            if (crack == null) continue;
            if (Application.isPlaying) Destroy(crack);
            else DestroyImmediate(crack);
        }
        cracks.Clear();
    }

    void OnDestroy()
    {
        Scars.Remove(this);
        DestroyOwned(lineMaterial);
        DestroyOwned(freshMaterial);
        DestroyOwned(freshMesh);
    }

    private static void DestroyOwned(Object target)
    {
        if (target == null) return;
        if (Application.isPlaying) Destroy(target);
        else DestroyImmediate(target);
    }
}

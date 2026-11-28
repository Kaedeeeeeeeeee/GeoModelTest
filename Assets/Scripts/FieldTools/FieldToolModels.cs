using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Loads the script-generated field-tool models (tools/art-pipeline/field-tools/build_models.py)
/// and gives each sub-mesh its runtime material, chosen by the OBJ material name.
/// </summary>
public static class FieldToolModels
{
    public const string ResourceFolder = "Models/FieldTools/";

    private readonly struct Look
    {
        public readonly Color Color;
        public readonly float Metallic;
        public readonly float Smoothness;

        public Look(float r, float g, float b, float metallic, float smoothness)
        {
            Color = new Color(r, g, b, 1f);
            Metallic = metallic;
            Smoothness = smoothness;
        }
    }

    // Keep in sync with MATERIALS in build_models.py.
    private static readonly Dictionary<string, Look> Looks = new Dictionary<string, Look>
    {
        { "steel", new Look(0.66f, 0.68f, 0.71f, 0.75f, 0.55f) },
        { "dark", new Look(0.20f, 0.21f, 0.23f, 0.45f, 0.35f) },
        { "paint", new Look(0.92f, 0.47f, 0.12f, 0.10f, 0.45f) },
        { "band", new Look(0.96f, 0.77f, 0.20f, 0.05f, 0.40f) },
        { "grip", new Look(0.12f, 0.30f, 0.56f, 0.00f, 0.25f) },
        { "rubber", new Look(0.09f, 0.09f, 0.10f, 0.00f, 0.20f) },
        { "red", new Look(0.74f, 0.16f, 0.13f, 0.05f, 0.45f) },
        { "blue", new Look(0.20f, 0.45f, 0.70f, 0.05f, 0.40f) },
        { "wood", new Look(0.74f, 0.57f, 0.36f, 0.00f, 0.15f) },
        { "wood_dark", new Look(0.55f, 0.40f, 0.24f, 0.00f, 0.15f) },
        { "label", new Look(0.93f, 0.93f, 0.88f, 0.00f, 0.20f) }
    };

    private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();

    /// <summary>Instantiates a model under <paramref name="parent"/> at its origin, or returns null when it is missing.</summary>
    public static GameObject Instantiate(string modelName, Transform parent = null)
    {
        GameObject prefab = Resources.Load<GameObject>(ResourceFolder + modelName);
        if (prefab == null)
        {
            Debug.LogWarning($"[FieldToolModels] Model not found: {ResourceFolder}{modelName}");
            return null;
        }

        GameObject instance = Object.Instantiate(prefab, parent, false);
        instance.name = modelName;
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;
        ApplyMaterials(instance);
        return instance;
    }

    public static void ApplyMaterials(GameObject instance)
    {
        if (instance == null) return;
        foreach (MeshRenderer renderer in instance.GetComponentsInChildren<MeshRenderer>(true))
        {
            Material[] imported = renderer.sharedMaterials;
            var replaced = new Material[imported.Length];
            for (int i = 0; i < imported.Length; i++)
            {
                Material material = GetMaterial(imported[i] != null ? imported[i].name : null, imported[i]);
                replaced[i] = material != null ? material : imported[i];
            }
            renderer.sharedMaterials = replaced;
        }
    }

    /// <summary>Material key ("steel", "wood_dark", ...) contained in an imported material name, or null.</summary>
    public static string ResolveKey(string materialName)
    {
        if (string.IsNullOrEmpty(materialName)) return null;
        string lower = materialName.ToLowerInvariant();
        string best = null;
        foreach (string key in Looks.Keys)
        {
            if (lower.Contains(key) && (best == null || key.Length > best.Length)) best = key;
        }
        return best;
    }

    /// <summary>Shared runtime material for a key. The imported material is cloned when available so the
    /// shader variant is one the build is guaranteed to contain.</summary>
    public static Material GetMaterial(string materialName, Material template = null)
    {
        string key = ResolveKey(materialName);
        if (key == null) return null;
        if (Materials.TryGetValue(key, out Material cached) && cached != null) return cached;

        Material material;
        if (template != null)
        {
            material = new Material(template);
        }
        else
        {
            Shader shader = FindLitShader();
            if (shader == null) return null;
            material = new Material(shader);
        }

        Look look = Looks[key];
        material.name = "FieldTool_" + key;
        ApplyLook(material, look.Color, look.Metallic, look.Smoothness);
        Materials[key] = material;
        return material;
    }

    public static void ApplyLook(Material material, Color color, float metallic, float smoothness)
    {
        if (material == null) return;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
    }

    public static Shader FindLitShader()
    {
        string[] names = { "Universal Render Pipeline/Lit", "Universal Render Pipeline/Simple Lit", "Standard", "Sprites/Default" };
        foreach (string name in names)
        {
            Shader shader = Shader.Find(name);
            if (shader != null) return shader;
        }
        return null;
    }

    /// <summary>Unlit shader for thin lines (wire, hose, cracks) that works under URP in a WebGL build.</summary>
    public static Shader FindLineShader()
    {
        string[] names = { "Sprites/Default", "Universal Render Pipeline/Unlit", "Unlit/Color" };
        foreach (string name in names)
        {
            Shader shader = Shader.Find(name);
            if (shader != null) return shader;
        }
        return null;
    }

    public static Material CreateLineMaterial(Color color)
    {
        Shader shader = FindLineShader();
        if (shader == null) return null;
        var material = new Material(shader) { name = "FieldTool_Line" };
        material.color = color;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        return material;
    }

    /// <summary>Main colour of a rock material, whichever render pipeline property it uses.</summary>
    public static Color ReadColor(Material material, Color fallback)
    {
        if (material == null) return fallback;
        Color tint;
        if (material.HasProperty("_BaseColor")) tint = material.GetColor("_BaseColor");
        else if (material.HasProperty("_Color")) tint = material.GetColor("_Color");
        else return fallback;

        // Layer materials are mostly a white tint over a rock texture; the tint alone would turn
        // every layer white on the depth gauge, so fold in the texture's average colour.
        Texture texture = material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") :
            material.HasProperty("_BaseMap") ? material.GetTexture("_BaseMap") : null;
        if (texture != null && TryAverageColour(texture, out Color average))
        {
            tint = new Color(tint.r * average.r, tint.g * average.g, tint.b * average.b, tint.a);
        }
        return tint;
    }

    private static readonly Dictionary<Texture, Color> averageCache = new Dictionary<Texture, Color>();

    private static bool TryAverageColour(Texture texture, out Color average)
    {
        if (averageCache.TryGetValue(texture, out average)) return true;
        average = Color.white;
        if (!SystemInfo.supportsRenderTextures && !(texture is Texture2D)) return false;
        const int size = 16;
        RenderTexture target = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        RenderTexture previous = RenderTexture.active;
        var pixels = new Texture2D(size, size, TextureFormat.RGBA32, false);
        try
        {
            Graphics.Blit(texture, target);
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, size, size), 0, 0, false);
            Color sum = Color.clear;
            foreach (Color pixel in pixels.GetPixels()) sum += pixel;
            average = sum / (size * size);
            average.a = 1f;
        }
        catch (System.Exception)
        {
            return false;
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            if (Application.isPlaying) Object.Destroy(pixels); else Object.DestroyImmediate(pixels);
        }
        averageCache[texture] = average;
        return true;
    }
}

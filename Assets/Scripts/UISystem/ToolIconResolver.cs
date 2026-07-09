using System.Collections.Generic;
using UnityEngine;

public static class ToolIconResolver
{
    private const int GeneratedIconSize = 128;

    private static readonly Dictionary<string, Sprite> generatedIconCache = new Dictionary<string, Sprite>();
    private static readonly Dictionary<int, Sprite> textureSpriteCache = new Dictionary<int, Sprite>();

    private enum GeneratedIconShape
    {
        SceneSwitcher,
        SimpleDrill,
        DrillTower,
        Hammer,
        Drone,
        DrillCar,
        Generic
    }

    public static Sprite GetIcon(CollectionTool tool)
    {
        if (tool == null)
        {
            return null;
        }

        if (tool.toolIcon != null)
        {
            return tool.toolIcon;
        }

        Sprite resolvedIcon = TryLoadResourceIcon(tool) ??
                              GetGeneratedToolIcon(tool) ??
                              TryCreateSpriteFromToolTexture(tool);

        if (resolvedIcon != null)
        {
            tool.toolIcon = resolvedIcon;
        }

        return resolvedIcon;
    }

    private static Sprite TryLoadResourceIcon(CollectionTool tool)
    {
        foreach (string path in GetResourceIconPaths(tool))
        {
            Sprite sprite = Resources.Load<Sprite>(path);
            if (sprite != null)
            {
                return sprite;
            }

            Texture2D texture = Resources.Load<Texture2D>(path);
            if (texture != null)
            {
                return CreateSpriteFromTexture(texture, path);
            }
        }

        return null;
    }

    private static IEnumerable<string> GetResourceIconPaths(CollectionTool tool)
    {
        if (!string.IsNullOrEmpty(tool.toolID))
        {
            yield return $"UI/ToolIcons/tool_{tool.toolID}";
        }

        if (tool is SceneSwitcherTool)
        {
            yield return "UI/ToolIcons/scene_switcher";
        }
        else if (tool is SimpleDrillTool)
        {
            yield return "UI/ToolIcons/simple_drill";
        }
        else if (tool is DrillTowerTool)
        {
            yield return "UI/ToolIcons/drill_tower";
        }
        else if (tool is HammerTool)
        {
            yield return "UI/ToolIcons/hammer";
        }
        else if (tool is DroneTool)
        {
            yield return "UI/ToolIcons/drone";
        }
        else if (tool is DrillCarTool)
        {
            yield return "UI/ToolIcons/drill_car";
        }
    }

    private static Sprite TryCreateSpriteFromToolTexture(CollectionTool tool)
    {
        Texture2D texture = TryGetTextureFromToolReferences(tool);

#if UNITY_EDITOR
        if (texture == null)
        {
            texture = TryLoadEditorTexture(tool);
        }
#endif

        return texture != null ? CreateSpriteFromTexture(texture, $"{tool.toolID}_texture_icon") : null;
    }

    private static Texture2D TryGetTextureFromToolReferences(CollectionTool tool)
    {
        Texture2D texture = TryGetTextureFromGameObject(tool.toolModel);
        if (texture != null)
        {
            return texture;
        }

        if (tool is SceneSwitcherTool sceneSwitcher)
        {
            texture = TryGetTextureFromGameObject(sceneSwitcher.switcherPrefab);
            if (texture != null) return texture;
        }
        else if (tool is HammerTool hammer)
        {
            texture = TryGetTextureFromGameObject(hammer.hammerPrefab);
            if (texture != null) return texture;
        }
        else if (tool is DrillTowerTool drillTower)
        {
            texture = TryGetTextureFromGameObject(drillTower.drillTowerPrefab);
            if (texture != null) return texture;

            texture = TryGetTextureFromGameObject(drillTower.prefabToPlace);
            if (texture != null) return texture;
        }
        else if (tool is PlaceableTool placeableTool)
        {
            texture = TryGetTextureFromGameObject(placeableTool.prefabToPlace);
            if (texture != null) return texture;
        }

        return null;
    }

    private static Texture2D TryGetTextureFromGameObject(GameObject root)
    {
        if (root == null)
        {
            return null;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            Texture2D texture = TryGetTextureFromMaterials(renderer.sharedMaterials);
            if (texture != null)
            {
                return texture;
            }
        }

        return null;
    }

    private static Texture2D TryGetTextureFromMaterials(Material[] materials)
    {
        if (materials == null)
        {
            return null;
        }

        foreach (Material material in materials)
        {
            Texture2D texture = TryGetTextureFromMaterial(material);
            if (texture != null)
            {
                return texture;
            }
        }

        return null;
    }

    private static Texture2D TryGetTextureFromMaterial(Material material)
    {
        if (material == null)
        {
            return null;
        }

        Texture2D texture = material.mainTexture as Texture2D;
        if (texture != null)
        {
            return texture;
        }

        if (material.HasProperty("_BaseMap"))
        {
            texture = material.GetTexture("_BaseMap") as Texture2D;
            if (texture != null) return texture;
        }

        if (material.HasProperty("_MainTex"))
        {
            texture = material.GetTexture("_MainTex") as Texture2D;
            if (texture != null) return texture;
        }

        if (material.HasProperty("_BaseColorMap"))
        {
            texture = material.GetTexture("_BaseColorMap") as Texture2D;
            if (texture != null) return texture;
        }

        return null;
    }

#if UNITY_EDITOR
    private static Texture2D TryLoadEditorTexture(CollectionTool tool)
    {
        foreach (string path in GetEditorTexturePaths(tool))
        {
            Texture2D texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture != null)
            {
                return texture;
            }
        }

        return null;
    }

    private static IEnumerable<string> GetEditorTexturePaths(CollectionTool tool)
    {
        if (tool is SceneSwitcherTool || tool.toolID == "999")
        {
            yield return "Assets/Model/SceneSwitcher/SceneSwitcher.png";
        }
        else if (tool is HammerTool || tool.toolID == "1002")
        {
            yield return "Assets/Model/Hammer/Geometric_Hammer_0714042033_texture.png";
        }
        else if (tool is SimpleDrillTool || tool is DrillCarTool || tool.toolID == "1000" || tool.toolID == "1101")
        {
            yield return "Assets/Model/Drill/Retro_Drill_Bot_0628054134_texture.png";
        }
        else if (tool is DroneTool || tool.toolID == "1100")
        {
            yield return "Assets/Model/Drone/Polygonal_Drone_Desig_0701040626_texture.png";
        }
    }
#endif

    private static Sprite CreateSpriteFromTexture(Texture2D texture, string name)
    {
        int textureId = texture.GetInstanceID();
        if (textureSpriteCache.TryGetValue(textureId, out Sprite cachedSprite) && cachedSprite != null)
        {
            return cachedSprite;
        }

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);

        sprite.name = string.IsNullOrEmpty(name) ? $"{texture.name}_Icon" : name;
        textureSpriteCache[textureId] = sprite;
        return sprite;
    }

    private static Sprite GetGeneratedToolIcon(CollectionTool tool)
    {
        string cacheKey = string.IsNullOrEmpty(tool.toolID) ? tool.GetType().Name : tool.toolID;
        if (generatedIconCache.TryGetValue(cacheKey, out Sprite cachedSprite) && cachedSprite != null)
        {
            return cachedSprite;
        }

        GeneratedIconShape shape = GetGeneratedIconShape(tool);
        Color accent = GetAccentColor(shape);
        Sprite sprite = CreateGeneratedIcon(cacheKey, shape, accent);
        generatedIconCache[cacheKey] = sprite;
        return sprite;
    }

    private static GeneratedIconShape GetGeneratedIconShape(CollectionTool tool)
    {
        if (tool is SceneSwitcherTool || tool.toolID == "999") return GeneratedIconShape.SceneSwitcher;
        if (tool is SimpleDrillTool || tool.toolID == "1000") return GeneratedIconShape.SimpleDrill;
        if (tool is DrillTowerTool || tool.toolID == "1001") return GeneratedIconShape.DrillTower;
        if (tool is HammerTool || tool.toolID == "1002") return GeneratedIconShape.Hammer;
        if (tool is DroneTool || tool.toolID == "1100") return GeneratedIconShape.Drone;
        if (tool is DrillCarTool || tool.toolID == "1101") return GeneratedIconShape.DrillCar;

        return GeneratedIconShape.Generic;
    }

    private static Color GetAccentColor(GeneratedIconShape shape)
    {
        switch (shape)
        {
            case GeneratedIconShape.SceneSwitcher:
                return new Color(1f, 0.86f, 0.15f, 1f);
            case GeneratedIconShape.SimpleDrill:
                return new Color(0.2f, 0.78f, 0.95f, 1f);
            case GeneratedIconShape.DrillTower:
                return new Color(0.82f, 0.88f, 0.96f, 1f);
            case GeneratedIconShape.Hammer:
                return new Color(0.95f, 0.56f, 0.22f, 1f);
            case GeneratedIconShape.Drone:
                return new Color(0.35f, 0.72f, 1f, 1f);
            case GeneratedIconShape.DrillCar:
                return new Color(1f, 0.72f, 0.2f, 1f);
            default:
                return new Color(0.9f, 0.9f, 0.9f, 1f);
        }
    }

    private static Sprite CreateGeneratedIcon(string cacheKey, GeneratedIconShape shape, Color accent)
    {
        Color[] pixels = new Color[GeneratedIconSize * GeneratedIconSize];
        Color backdrop = new Color(0.03f, 0.04f, 0.05f, 0.35f);
        Color shadow = new Color(0f, 0f, 0f, 0.34f);
        Color light = Color.white;

        DrawFilledCircle(pixels, GeneratedIconSize, 64f, 64f, 54f, backdrop);

        switch (shape)
        {
            case GeneratedIconShape.SceneSwitcher:
                DrawRect(pixels, GeneratedIconSize, 43, 30, 42, 68, shadow);
                DrawRect(pixels, GeneratedIconSize, 40, 27, 42, 68, accent);
                DrawRect(pixels, GeneratedIconSize, 50, 39, 22, 50, new Color(0.05f, 0.06f, 0.06f, 0.65f));
                DrawThickLine(pixels, GeneratedIconSize, new Vector2(68f, 42f), new Vector2(84f, 54f), light, 5f);
                DrawThickLine(pixels, GeneratedIconSize, new Vector2(84f, 54f), new Vector2(68f, 66f), light, 5f);
                DrawFilledCircle(pixels, GeneratedIconSize, 84f, 54f, 8f, new Color(1f, 1f, 1f, 0.25f));
                break;

            case GeneratedIconShape.SimpleDrill:
                DrawThickLine(pixels, GeneratedIconSize, new Vector2(64f, 22f), new Vector2(64f, 88f), shadow, 13f);
                DrawThickLine(pixels, GeneratedIconSize, new Vector2(64f, 20f), new Vector2(64f, 86f), accent, 9f);
                DrawThickLine(pixels, GeneratedIconSize, new Vector2(50f, 36f), new Vector2(78f, 48f), light, 5f);
                DrawThickLine(pixels, GeneratedIconSize, new Vector2(50f, 54f), new Vector2(78f, 66f), light, 5f);
                DrawThickLine(pixels, GeneratedIconSize, new Vector2(64f, 85f), new Vector2(54f, 106f), accent, 8f);
                DrawThickLine(pixels, GeneratedIconSize, new Vector2(64f, 85f), new Vector2(74f, 106f), accent, 8f);
                break;

            case GeneratedIconShape.DrillTower:
                DrawThickLine(pixels, GeneratedIconSize, new Vector2(31f, 99f), new Vector2(64f, 22f), shadow, 8f);
                DrawThickLine(pixels, GeneratedIconSize, new Vector2(97f, 99f), new Vector2(64f, 22f), shadow, 8f);
                DrawThickLine(pixels, GeneratedIconSize, new Vector2(33f, 96f), new Vector2(64f, 22f), accent, 5f);
                DrawThickLine(pixels, GeneratedIconSize, new Vector2(95f, 96f), new Vector2(64f, 22f), accent, 5f);
                DrawThickLine(pixels, GeneratedIconSize, new Vector2(45f, 66f), new Vector2(83f, 66f), light, 4f);
                DrawThickLine(pixels, GeneratedIconSize, new Vector2(39f, 82f), new Vector2(89f, 82f), light, 4f);
                DrawThickLine(pixels, GeneratedIconSize, new Vector2(64f, 34f), new Vector2(64f, 108f), accent, 5f);
                DrawRect(pixels, GeneratedIconSize, 39, 99, 50, 8, accent);
                break;

            case GeneratedIconShape.Hammer:
                DrawThickLine(pixels, GeneratedIconSize, new Vector2(78f, 40f), new Vector2(43f, 99f), shadow, 13f);
                DrawThickLine(pixels, GeneratedIconSize, new Vector2(76f, 38f), new Vector2(41f, 97f), new Color(0.58f, 0.35f, 0.18f, 1f), 9f);
                DrawThickLine(pixels, GeneratedIconSize, new Vector2(43f, 36f), new Vector2(86f, 52f), shadow, 21f);
                DrawThickLine(pixels, GeneratedIconSize, new Vector2(41f, 34f), new Vector2(84f, 50f), accent, 16f);
                DrawThickLine(pixels, GeneratedIconSize, new Vector2(41f, 34f), new Vector2(84f, 50f), light, 5f);
                break;

            case GeneratedIconShape.Drone:
                DrawThickLine(pixels, GeneratedIconSize, new Vector2(64f, 64f), new Vector2(34f, 34f), shadow, 6f);
                DrawThickLine(pixels, GeneratedIconSize, new Vector2(64f, 64f), new Vector2(94f, 34f), shadow, 6f);
                DrawThickLine(pixels, GeneratedIconSize, new Vector2(64f, 64f), new Vector2(34f, 94f), shadow, 6f);
                DrawThickLine(pixels, GeneratedIconSize, new Vector2(64f, 64f), new Vector2(94f, 94f), shadow, 6f);
                DrawThickLine(pixels, GeneratedIconSize, new Vector2(64f, 64f), new Vector2(34f, 34f), accent, 4f);
                DrawThickLine(pixels, GeneratedIconSize, new Vector2(64f, 64f), new Vector2(94f, 34f), accent, 4f);
                DrawThickLine(pixels, GeneratedIconSize, new Vector2(64f, 64f), new Vector2(34f, 94f), accent, 4f);
                DrawThickLine(pixels, GeneratedIconSize, new Vector2(64f, 64f), new Vector2(94f, 94f), accent, 4f);
                DrawFilledCircle(pixels, GeneratedIconSize, 34f, 34f, 11f, light);
                DrawFilledCircle(pixels, GeneratedIconSize, 94f, 34f, 11f, light);
                DrawFilledCircle(pixels, GeneratedIconSize, 34f, 94f, 11f, light);
                DrawFilledCircle(pixels, GeneratedIconSize, 94f, 94f, 11f, light);
                DrawFilledCircle(pixels, GeneratedIconSize, 64f, 64f, 17f, accent);
                break;

            case GeneratedIconShape.DrillCar:
                DrawRect(pixels, GeneratedIconSize, 30, 58, 68, 27, shadow);
                DrawRect(pixels, GeneratedIconSize, 28, 55, 68, 27, accent);
                DrawRect(pixels, GeneratedIconSize, 41, 39, 32, 18, new Color(0.35f, 0.75f, 0.95f, 1f));
                DrawThickLine(pixels, GeneratedIconSize, new Vector2(86f, 50f), new Vector2(105f, 25f), light, 6f);
                DrawThickLine(pixels, GeneratedIconSize, new Vector2(105f, 25f), new Vector2(112f, 40f), light, 5f);
                DrawFilledCircle(pixels, GeneratedIconSize, 43f, 89f, 11f, light);
                DrawFilledCircle(pixels, GeneratedIconSize, 82f, 89f, 11f, light);
                break;

            default:
                DrawFilledCircle(pixels, GeneratedIconSize, 64f, 64f, 35f, accent);
                DrawFilledCircle(pixels, GeneratedIconSize, 64f, 64f, 20f, light);
                break;
        }

        Texture2D texture = new Texture2D(GeneratedIconSize, GeneratedIconSize, TextureFormat.RGBA32, false)
        {
            name = $"{cacheKey}_GeneratedToolIcon",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        texture.SetPixels(pixels);
        texture.Apply(false, true);

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, GeneratedIconSize, GeneratedIconSize),
            new Vector2(0.5f, 0.5f),
            100f);
        sprite.name = texture.name;
        return sprite;
    }

    private static void DrawRect(Color[] pixels, int size, int x, int y, int width, int height, Color color)
    {
        for (int py = y; py < y + height; py++)
        {
            for (int px = x; px < x + width; px++)
            {
                BlendPixel(pixels, size, px, py, color);
            }
        }
    }

    private static void DrawFilledCircle(Color[] pixels, int size, float centerX, float centerY, float radius, Color color)
    {
        float radiusSquared = radius * radius;
        int minX = Mathf.FloorToInt(centerX - radius);
        int maxX = Mathf.CeilToInt(centerX + radius);
        int minY = Mathf.FloorToInt(centerY - radius);
        int maxY = Mathf.CeilToInt(centerY + radius);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float dx = x - centerX;
                float dy = y - centerY;
                if (dx * dx + dy * dy <= radiusSquared)
                {
                    BlendPixel(pixels, size, x, y, color);
                }
            }
        }
    }

    private static void DrawThickLine(Color[] pixels, int size, Vector2 start, Vector2 end, Color color, float thickness)
    {
        float radius = thickness * 0.5f;
        int minX = Mathf.FloorToInt(Mathf.Min(start.x, end.x) - radius);
        int maxX = Mathf.CeilToInt(Mathf.Max(start.x, end.x) + radius);
        int minY = Mathf.FloorToInt(Mathf.Min(start.y, end.y) - radius);
        int maxY = Mathf.CeilToInt(Mathf.Max(start.y, end.y) + radius);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float distance = DistancePointToSegment(new Vector2(x, y), start, end);
                if (distance <= radius)
                {
                    BlendPixel(pixels, size, x, y, color);
                }
            }
        }
    }

    private static float DistancePointToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 segment = end - start;
        float segmentLengthSquared = segment.sqrMagnitude;
        if (segmentLengthSquared <= Mathf.Epsilon)
        {
            return Vector2.Distance(point, start);
        }

        float t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / segmentLengthSquared);
        Vector2 projection = start + t * segment;
        return Vector2.Distance(point, projection);
    }

    private static void BlendPixel(Color[] pixels, int size, int x, int y, Color source)
    {
        if (x < 0 || x >= size || y < 0 || y >= size || source.a <= 0f)
        {
            return;
        }

        int index = y * size + x;
        Color destination = pixels[index];
        float alpha = source.a + destination.a * (1f - source.a);

        if (alpha <= 0f)
        {
            pixels[index] = Color.clear;
            return;
        }

        pixels[index] = new Color(
            (source.r * source.a + destination.r * destination.a * (1f - source.a)) / alpha,
            (source.g * source.a + destination.g * destination.a * (1f - source.a)) / alpha,
            (source.b * source.a + destination.b * destination.a * (1f - source.a)) / alpha,
            alpha);
    }
}

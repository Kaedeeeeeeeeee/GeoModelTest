using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 样本图标生成器 - 根据样本类型和颜色动态生成图标
/// </summary>
public class SampleIconGenerator : MonoBehaviour
{
    [Header("图标设置")]
    public int iconSize = 128;
    public Color outlineColor = Color.black;
    public float outlineWidth = 2f;
    
    [Header("形状设置")]
    public Color cylinderOutlineColor = Color.black;
    public Color slabOutlineColor = Color.black;
    
    // 单例模式
    public static SampleIconGenerator Instance { get; private set; }
    
    // 图标缓存
    private Dictionary<string, Sprite> iconCache = new Dictionary<string, Sprite>();
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// 为样本生成图标
    /// </summary>
    public Sprite GenerateIconForSample(SampleItem sample)
    {
        if (sample == null) return null;
        
        // 生成缓存键
        string cacheKey = GenerateCacheKey(sample);
        
        // 检查缓存
        if (iconCache.TryGetValue(cacheKey, out Sprite cachedIcon))
        {
            Debug.Log($"📦 使用缓存图标: {cacheKey}");
            return cachedIcon;
        }
        else
        {
            Debug.Log($"🆕 缓存中没有图标，需要生成新图标: {cacheKey}");
        }
        
        // 确定样本形状类型
        SampleShapeType shapeType = GetSampleShapeType(sample);
        
        // 获取样本主要颜色
        Color sampleColor = GetSampleMainColor(sample);
        
        // 生成图标
        Debug.Log($"🖼️ 生成图标: {shapeType}, 颜色 #{ColorUtility.ToHtmlStringRGBA(sampleColor)}");
        
        Sprite icon = GenerateIcon(shapeType, sampleColor);
        
        if (icon != null)
        {
            Debug.Log($"✅ 图标生成成功: {icon.name}");
        }
        else
        {
            Debug.LogError($"❌ 图标生成失败！");
        }
        
        // 缓存图标
        iconCache[cacheKey] = icon;
        
        return icon;
    }
    
    /// <summary>
    /// 样本形状类型
    /// </summary>
    public enum SampleShapeType
    {
        Cylinder,  // 圆柱形（钻探样本）
        Slab       // 岩块形（地质锤样本）
    }
    
    /// <summary>
    /// 确定样本形状类型
    /// </summary>
    SampleShapeType GetSampleShapeType(SampleItem sample)
    {
        // 根据源工具ID确定形状
        return sample.sourceToolID switch
        {
            "1002" => SampleShapeType.Slab,      // 地质锤 - 岩块形
            "1000" => SampleShapeType.Cylinder,  // 简易钻探 - 圆柱形
            "1001" => SampleShapeType.Cylinder,  // 钻塔 - 圆柱形
            _ => SampleShapeType.Cylinder         // 默认圆柱形
        };
    }
    
    /// <summary>
    /// 获取样本主要颜色
    /// </summary>
    Color GetSampleMainColor(SampleItem sample)
    {
        Color resultColor = Color.gray;
        
        // 简化调试输出 - 只显示关键信息
        Debug.Log($"🔍 样本颜色分析: {sample.displayName} (工具:{sample.sourceToolID}, 层数:{sample.geologicalLayers?.Count ?? 0})");
        
        // 优先使用地质层颜色 - 使用最上层（表面）地质层
        if (sample.geologicalLayers != null && sample.geologicalLayers.Count > 0)
        {
            // 简化地质层输出 - 只显示数量和顶层
            if (sample.geologicalLayers.Count > 1)
            {
                Debug.Log($"   多地质层 ({sample.geologicalLayers.Count}层)");
            }
            
            // 找到最上层的地质层（深度最小的层）
            var topLayer = GetTopMostLayer(sample.geologicalLayers);
            Color layerColor = topLayer.layerColor;
            
            Debug.Log($"   🎯 最上层: {topLayer.layerName}, 颜色: #{ColorUtility.ToHtmlStringRGBA(layerColor)}");
            
            // 检查是否为白色、透明或灰色，如果是则使用默认颜色
            if (IsColorTooLight(layerColor) || IsColorTooGray(layerColor))
            {
                if (IsColorTooLight(layerColor))
                {
                    Debug.LogWarning($"❌ 最上层地质层颜色过浅或透明，使用工具默认颜色");
                }
                else
                {
                    Debug.LogWarning($"❌ 最上层地质层颜色过于灰暗，使用工具默认颜色以增强视觉效果");
                }
                resultColor = GetDefaultColorByTool(sample.sourceToolID);
            }
            else
            {
                Debug.Log($"✅ 使用最上层地质层颜色（表面颜色）");
                resultColor = layerColor;
            }
        }
        else
        {
            // 没有地质层信息，使用工具默认颜色
            Debug.LogWarning($"❌ 没有地质层信息，使用工具默认颜色");
            resultColor = GetDefaultColorByTool(sample.sourceToolID);
        }
        
        Debug.Log($"🎨 最终颜色: R={resultColor.r:F3}, G={resultColor.g:F3}, B={resultColor.b:F3}, A={resultColor.a:F3}");
        Debug.Log($"   HTML颜色: #{ColorUtility.ToHtmlStringRGBA(resultColor)}");
        Debug.Log($"   颜色亮度: {((resultColor.r + resultColor.g + resultColor.b) / 3f):F3}");
        
        return resultColor;
    }
    
    /// <summary>
    /// 获取最上层的地质层（深度最小的层）
    /// </summary>
    SampleItem.LayerInfo GetTopMostLayer(System.Collections.Generic.List<SampleItem.LayerInfo> layers)
    {
        if (layers == null || layers.Count == 0)
            return null;
        
        // 找到深度起始点最小的地质层（最接近表面的层）
        SampleItem.LayerInfo topLayer = layers[0];
        float minDepth = topLayer.depthStart;
        
        for (int i = 1; i < layers.Count; i++)
        {
            if (layers[i].depthStart < minDepth)
            {
                minDepth = layers[i].depthStart;
                topLayer = layers[i];
            }
        }
        
        Debug.Log($"   🔍 在 {layers.Count} 个地质层中找到最上层:");
        Debug.Log($"      名称: {topLayer.layerName}");
        Debug.Log($"      深度: {topLayer.depthStart:F2}m - {topLayer.depthEnd:F2}m");
        Debug.Log($"      厚度: {topLayer.thickness:F2}m");
        
        return topLayer;
    }
    
    /// <summary>
    /// 检查颜色是否过浅（接近白色）
    /// </summary>
    bool IsColorTooLight(Color color)
    {
        // 计算颜色亮度，如果太亮或者是白色/透明则返回true
        float brightness = (color.r + color.g + color.b) / 3f;
        bool isTooLight = brightness > 0.95f || color.a < 0.05f || 
                         (color.r > 0.98f && color.g > 0.98f && color.b > 0.98f);
        
        // 简化亮度检查输出
        if (isTooLight)
        {
            Debug.Log($"   ❌ 颜色过浅 (亮度:{brightness:F2}, α:{color.a:F2})");
        }
        
        return isTooLight;
    }
    
    /// <summary>
    /// 检查颜色是否过于灰暗（缺乏饱和度）
    /// </summary>
    bool IsColorTooGray(Color color)
    {
        // 计算饱和度：最大值与最小值的差异
        float max = Mathf.Max(color.r, color.g, color.b);
        float min = Mathf.Min(color.r, color.g, color.b);
        float saturation = max > 0 ? (max - min) / max : 0;
        
        // 计算整体亮度
        float brightness = (color.r + color.g + color.b) / 3f;
        
        // 如果饱和度很低（接近灰色）且亮度在中等范围，则认为是"无聊的灰色"
        bool isTooGray = saturation < 0.2f && brightness > 0.3f && brightness < 0.8f;
        
        if (isTooGray)
        {
            Debug.Log($"   ❌ 颜色饱和度过低 (饱和度:{saturation:F2}, 亮度:{brightness:F2})");
        }
        
        return isTooGray;
    }
    
    /// <summary>
    /// 根据工具ID获取默认颜色（增强版，更鲜艳）
    /// </summary>
    Color GetDefaultColorByTool(string toolID)
    {
        Color defaultColor = toolID switch
        {
            "1000" => new Color(1.0f, 0.6f, 0.2f), // 简易钻探 - 鲜艳橙色
            "1001" => new Color(0.2f, 0.8f, 0.3f), // 钻塔 - 鲜绿色
            "1002" => new Color(0.8f, 0.3f, 0.2f), // 地质锤 - 鲜红褐色
            _ => new Color(0.7f, 0.5f, 0.3f)      // 默认 - 温暖棕色
        };
        
        string toolName = toolID switch
        {
            "1000" => "简易钻探",
            "1001" => "钻塔",
            "1002" => "地质锤",
            _ => "未知工具"
        };
        
        Debug.Log($"🛠️ 使用默认颜色: {toolName} #{ColorUtility.ToHtmlStringRGBA(defaultColor)}");
        
        return defaultColor;
    }
    
    /// <summary>
    /// 生成图标
    /// </summary>
    Sprite GenerateIcon(SampleShapeType shapeType, Color color)
    {
        // 注释掉详细的GenerateIcon调试
        
        Texture2D texture = new Texture2D(iconSize, iconSize, TextureFormat.RGBA32, false);
        
        // 清空背景
        Color[] pixels = new Color[iconSize * iconSize];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }
        texture.SetPixels(pixels);
        
        // 根据形状类型绘制
        switch (shapeType)
        {
            case SampleShapeType.Cylinder:
                DrawCylinderShape(texture, color);
                break;
            case SampleShapeType.Slab:
                DrawSlabShape(texture, color);
                break;
        }
        
        texture.Apply();
        
        // 简化纹理验证
        Color[] finalPixels = texture.GetPixels();
        int nonTransparentPixels = 0;
        for (int i = 0; i < finalPixels.Length; i++)
        {
            if (finalPixels[i].a > 0.1f) nonTransparentPixels++;
        }
        Debug.Log($"   纹理像素: {nonTransparentPixels}/{finalPixels.Length}");
        
        // 创建Sprite
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, iconSize, iconSize), new Vector2(0.5f, 0.5f));
        sprite.name = $"SampleIcon_{shapeType}_{ColorUtility.ToHtmlStringRGB(color)}";
        
        return sprite;
    }
    
    /// <summary>
    /// 绘制圆柱形图标
    /// </summary>
    void DrawCylinderShape(Texture2D texture, Color color)
    {
        int centerX = iconSize / 2;
        int centerY = iconSize / 2;
        
        // 圆柱体参数 - 更像真实圆柱体
        int cylinderWidth = (int)(iconSize * 0.5f);  // 减小宽度，更接近真实圆柱
        int cylinderHeight = (int)(iconSize * 0.75f); // 稍微减小高度
        int radius = cylinderWidth / 2;
        int ellipseRadiusY = (int)(radius * 0.25f); // 减小椭圆压扁程度，避免花瓶效果
        
        // 绘制圆柱体侧面 - 直边而不是曲线
        for (int y = 0; y < iconSize; y++)
        {
            for (int x = 0; x < iconSize; x++)
            {
                int relativeX = x - centerX;
                int relativeY = y - centerY;
                
                // 圆柱体侧面区域 - 简单的矩形区域
                bool inCylinderHeight = Mathf.Abs(relativeY) <= cylinderHeight / 2;
                bool inCylinderWidth = Mathf.Abs(relativeX) <= radius;
                
                if (inCylinderHeight && inCylinderWidth)
                {
                    // 直边圆柱体，不要花瓶形状
                    if (Mathf.Abs(relativeX) <= radius - outlineWidth && 
                        Mathf.Abs(relativeY) <= cylinderHeight / 2 - outlineWidth)
                    {
                        // 添加简单的明暗效果
                        float lightness = 1.0f - (Mathf.Abs(relativeX) / (float)radius) * 0.3f; // 中间亮，边缘暗
                        Color shadedColor = new Color(color.r * lightness, color.g * lightness, color.b * lightness, color.a);
                        texture.SetPixel(x, y, shadedColor);
                    }
                    else
                    {
                        // 轮廓
                        texture.SetPixel(x, y, outlineColor);
                    }
                }
            }
        }
        
        // 绘制圆柱体顶部椭圆 - 稍微明亮一些显示顶面
        int topY = centerY - cylinderHeight / 2;
        Color topColor = new Color(
            Mathf.Min(color.r * 1.2f, 1f), 
            Mathf.Min(color.g * 1.2f, 1f), 
            Mathf.Min(color.b * 1.2f, 1f), 
            color.a
        );
        DrawEllipse(texture, centerX, topY, radius, ellipseRadiusY, topColor, outlineColor);
        
        // 绘制圆柱体底部椭圆（部分可见，稍微暗一些）
        int bottomY = centerY + cylinderHeight / 2;
        Color bottomColor = new Color(color.r * 0.8f, color.g * 0.8f, color.b * 0.8f, color.a);
        DrawEllipseArc(texture, centerX, bottomY, radius, ellipseRadiusY, bottomColor, outlineColor, false);
    }
    
    /// <summary>
    /// 绘制地质锤岩块图标：拳头大小的不规则岩块，上面的破裂面亮、侧面暗（旧版是一条红色薄片）
    /// </summary>
    void DrawSlabShape(Texture2D texture, Color color)
    {
        // 外轮廓（逆时针，y 向上）与上破裂面，均以图标尺寸的比例给出
        Vector2[] outline =
        {
            new Vector2(0.16f, 0.44f), new Vector2(0.27f, 0.25f), new Vector2(0.55f, 0.17f), new Vector2(0.80f, 0.27f),
            new Vector2(0.88f, 0.49f), new Vector2(0.77f, 0.70f), new Vector2(0.50f, 0.78f), new Vector2(0.26f, 0.68f)
        };
        Vector2[] topFace =
        {
            new Vector2(0.26f, 0.55f), new Vector2(0.45f, 0.46f), new Vector2(0.70f, 0.49f),
            new Vector2(0.77f, 0.66f), new Vector2(0.50f, 0.75f), new Vector2(0.29f, 0.66f)
        };
        Vector2[] rightFace =
        {
            new Vector2(0.45f, 0.46f), new Vector2(0.55f, 0.17f), new Vector2(0.80f, 0.27f),
            new Vector2(0.88f, 0.49f), new Vector2(0.77f, 0.66f), new Vector2(0.70f, 0.49f)
        };
        Color top = Color.Lerp(color, Color.white, 0.22f);
        Color left = color * 0.85f;
        Color right = color * 0.62f;
        top.a = left.a = right.a = 1f;
        float edge = outlineWidth / iconSize;

        for (int y = 0; y < iconSize; y++)
        {
            for (int x = 0; x < iconSize; x++)
            {
                Vector2 p = new Vector2((x + 0.5f) / iconSize, (y + 0.5f) / iconSize);
                if (!InPolygon(p, outline)) continue;
                if (DistanceToEdge(p, outline) <= edge)
                {
                    texture.SetPixel(x, y, outlineColor);
                    continue;
                }
                Color shade = InPolygon(p, topFace) ? top : InPolygon(p, rightFace) ? right : left;
                // 细微的颗粒感
                float grain = 1f + (Mathf.PerlinNoise(x * 0.35f, y * 0.35f) - 0.5f) * 0.12f;
                texture.SetPixel(x, y, new Color(shade.r * grain, shade.g * grain, shade.b * grain, 1f));
            }
        }
    }

    static bool InPolygon(Vector2 p, Vector2[] polygon)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            if ((polygon[i].y > p.y) != (polygon[j].y > p.y) &&
                p.x < (polygon[j].x - polygon[i].x) * (p.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x)
            {
                inside = !inside;
            }
        }
        return inside;
    }

    static float DistanceToEdge(Vector2 p, Vector2[] polygon)
    {
        float best = float.MaxValue;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            Vector2 a = polygon[j], b = polygon[i];
            float t = Mathf.Clamp01(Vector2.Dot(p - a, b - a) / (b - a).sqrMagnitude);
            best = Mathf.Min(best, Vector2.Distance(p, a + (b - a) * t));
        }
        return best;
    }
    
    /// <summary>
    /// 绘制椭圆
    /// </summary>
    void DrawEllipse(Texture2D texture, int centerX, int centerY, int radiusX, int radiusY, Color fillColor, Color outlineColor)
    {
        for (int y = centerY - radiusY; y <= centerY + radiusY; y++)
        {
            for (int x = centerX - radiusX; x <= centerX + radiusX; x++)
            {
                if (x < 0 || x >= iconSize || y < 0 || y >= iconSize) continue;
                
                float dx = (float)(x - centerX) / radiusX;
                float dy = (float)(y - centerY) / radiusY;
                float distance = dx * dx + dy * dy;
                
                if (distance <= 1.0f)
                {
                    if (distance <= (1.0f - outlineWidth / radiusX))
                    {
                        texture.SetPixel(x, y, fillColor);
                    }
                    else
                    {
                        texture.SetPixel(x, y, outlineColor);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 绘制椭圆弧（用于圆柱底部）
    /// </summary>
    void DrawEllipseArc(Texture2D texture, int centerX, int centerY, int radiusX, int radiusY, Color fillColor, Color outlineColor, bool topHalf)
    {
        for (int y = centerY - radiusY; y <= centerY + radiusY; y++)
        {
            for (int x = centerX - radiusX; x <= centerX + radiusX; x++)
            {
                if (x < 0 || x >= iconSize || y < 0 || y >= iconSize) continue;
                
                // 只绘制下半部分（底部椭圆的可见部分）
                if (topHalf && y > centerY) continue;
                if (!topHalf && y < centerY) continue;
                
                float dx = (float)(x - centerX) / radiusX;
                float dy = (float)(y - centerY) / radiusY;
                float distance = dx * dx + dy * dy;
                
                if (distance <= 1.0f)
                {
                    if (distance <= (1.0f - outlineWidth / radiusX))
                    {
                        texture.SetPixel(x, y, fillColor);
                    }
                    else
                    {
                        texture.SetPixel(x, y, outlineColor);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 生成缓存键
    /// </summary>
    string GenerateCacheKey(SampleItem sample)
    {
        SampleShapeType shapeType = GetSampleShapeType(sample);
        Color color = GetSampleMainColor(sample);
        string cacheKey = $"{shapeType}_{ColorUtility.ToHtmlStringRGB(color)}";
        
        Debug.Log($"🔑 缓存键: {cacheKey} ({sample.displayName})");
        
        return cacheKey;
    }
    
    /// <summary>
    /// 清理图标缓存
    /// </summary>
    public void ClearIconCache()
    {
        foreach (var icon in iconCache.Values)
        {
            if (icon != null && icon.texture != null)
            {
                DestroyImmediate(icon.texture);
                DestroyImmediate(icon);
            }
        }
        iconCache.Clear();
        Debug.Log("[SampleIconGenerator] 图标缓存已清理");
    }
    
    /// <summary>
    /// 强制刷新样本图标（清理缓存后重新生成）
    /// </summary>
    public Sprite RefreshSampleIcon(SampleItem sample)
    {
        if (sample == null) return null;
        
        // 清理这个样本的缓存
        string cacheKey = GenerateCacheKey(sample);
        if (iconCache.ContainsKey(cacheKey))
        {
            var oldIcon = iconCache[cacheKey];
            if (oldIcon != null && oldIcon.texture != null)
            {
                DestroyImmediate(oldIcon.texture);
                DestroyImmediate(oldIcon);
            }
            iconCache.Remove(cacheKey);
        }
        
        // 重新生成图标
        return GenerateIconForSample(sample);
    }
    
    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    public string GetCacheStats()
    {
        return $"图标缓存: {iconCache.Count} 个图标";
    }
    
    /// <summary>
    /// 预生成常用图标（可选优化）
    /// </summary>
    public void PreGenerateCommonIcons()
    {
        Color[] commonColors = {
            new Color(0.8f, 0.6f, 0.4f), // 棕色
            new Color(0.6f, 0.8f, 0.4f), // 绿色
            new Color(0.8f, 0.4f, 0.6f), // 粉色
            Color.gray,
            Color.red,
            Color.blue,
            Color.yellow
        };
        
        foreach (Color color in commonColors)
        {
            GenerateIcon(SampleShapeType.Cylinder, color);
            GenerateIcon(SampleShapeType.Slab, color);
        }
        
        Debug.Log($"[SampleIconGenerator] 预生成了 {commonColors.Length * 2} 个常用图标");
    }
    
    void OnDestroy()
    {
        ClearIconCache();
    }
    
    /// <summary>
    /// 在Inspector中显示缓存统计
    /// </summary>
    [ContextMenu("显示缓存统计")]
    void ShowCacheStats()
    {
        Debug.Log(GetCacheStats());
    }
    
    /// <summary>
    /// 测试生成图标
    /// </summary>
    [ContextMenu("测试生成图标")]
    void TestGenerateIcons()
    {
        Sprite cylinderIcon = GenerateIcon(SampleShapeType.Cylinder, Color.red);
        Sprite slabIcon = GenerateIcon(SampleShapeType.Slab, Color.blue);
        
        Debug.Log($"测试生成图标完成: 圆柱形={cylinderIcon != null}, 薄片形={slabIcon != null}");
    }
}
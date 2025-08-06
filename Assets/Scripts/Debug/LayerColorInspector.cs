using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 地质层颜色检查器 - 查看场景中地质层的实际颜色
/// </summary>
public class LayerColorInspector : MonoBehaviour
{
    [Header("检查设置")]
    public LayerMask groundLayers = -1;
    public float checkRadius = 50f;
    
    [ContextMenu("检查场景中的地质层颜色")]
    public void InspectGeologyLayerColors()
    {
        Debug.Log("🔍 开始检查场景中的地质层颜色...");
        Debug.Log("".PadRight(60, '='));
        
        // 查找所有地质层对象
        GeologyLayer[] allLayers = FindObjectsOfType<GeologyLayer>();
        
        if (allLayers.Length == 0)
        {
            Debug.LogWarning("❌ 场景中未找到任何 GeologyLayer 对象");
            return;
        }
        
        Debug.Log($"📊 发现 {allLayers.Length} 个地质层:");
        
        Dictionary<string, int> colorStats = new Dictionary<string, int>();
        
        for (int i = 0; i < allLayers.Length; i++)
        {
            var layer = allLayers[i];
            if (layer == null) continue;
            
            string colorHtml = ColorUtility.ToHtmlStringRGBA(layer.layerColor);
            float brightness = (layer.layerColor.r + layer.layerColor.g + layer.layerColor.b) / 3f;
            
            Debug.Log($"\\n🏔️ 地质层 {i + 1}: {layer.name}");
            Debug.Log($"   位置: ({layer.transform.position.x:F1}, {layer.transform.position.y:F1}, {layer.transform.position.z:F1})");
            Debug.Log($"   层名称: {layer.layerName ?? "未命名"}");
            Debug.Log($"   颜色: #{colorHtml}");
            Debug.Log($"   RGB: ({layer.layerColor.r:F2}, {layer.layerColor.g:F2}, {layer.layerColor.b:F2})");
            Debug.Log($"   亮度: {brightness:F3}");
            Debug.Log($"   透明度: {layer.layerColor.a:F2}");
            Debug.Log($"   倾角: {layer.dipAngle}°");
            Debug.Log($"   走向: {layer.strikeDirection}");
            
            // 统计颜色分布
            if (colorStats.ContainsKey(colorHtml))
                colorStats[colorHtml]++;
            else
                colorStats[colorHtml] = 1;
            
            // 检查颜色类型
            if (brightness > 0.9f)
                Debug.Log($"   ⚠️ 此层颜色较浅，可能影响图标显示");
            else if (brightness < 0.1f)
                Debug.Log($"   ⚠️ 此层颜色较深，可能影响图标显示");
            else
                Debug.Log($"   ✅ 颜色适合图标显示");
        }
        
        Debug.Log("\\n".PadRight(60, '='));
        Debug.Log("📈 颜色统计:");
        
        foreach (var colorStat in colorStats)
        {
            Debug.Log($"   #{colorStat.Key}: {colorStat.Value} 个地质层");
        }
        
        Debug.Log($"\\n💡 提示: 尝试在不同位置采集样本，可能会获得不同颜色的地质层");
    }
    
    [ContextMenu("检查当前位置的地质层")]
    public void InspectCurrentLocationLayers()
    {
        Debug.Log("🎯 检查当前位置的地质层...");
        
        Vector3 checkPos = transform.position;
        Debug.Log($"检查位置: ({checkPos.x:F2}, {checkPos.y:F2}, {checkPos.z:F2})");
        
        // 向下发射射线
        RaycastHit hit;
        if (Physics.Raycast(checkPos, Vector3.down, out hit, 100f, groundLayers))
        {
            Debug.Log($"撞击到: {hit.collider.name} 在 ({hit.point.x:F2}, {hit.point.y:F2}, {hit.point.z:F2})");
            
            // 检查撞击对象的材质
            Renderer renderer = hit.collider.GetComponent<Renderer>();
            if (renderer != null && renderer.sharedMaterial != null)
            {
                Material mat = renderer.sharedMaterial;
                Color matColor = mat.color;
                
                Debug.Log($"材质: {mat.name}");
                Debug.Log($"材质颜色: #{ColorUtility.ToHtmlStringRGBA(matColor)}");
                Debug.Log($"材质RGB: ({matColor.r:F2}, {matColor.g:F2}, {matColor.b:F2}, {matColor.a:F2})");
                
                if (mat.HasProperty("_Color"))
                {
                    Color propertyColor = mat.GetColor("_Color");
                    Debug.Log($"_Color属性: #{ColorUtility.ToHtmlStringRGBA(propertyColor)}");
                }
                
                if (mat.HasProperty("_BaseColor"))
                {
                    Color baseColor = mat.GetColor("_BaseColor");
                    Debug.Log($"_BaseColor属性: #{ColorUtility.ToHtmlStringRGBA(baseColor)}");
                }
            }
            
            // 检查是否有 GeologyLayer 组件
            GeologyLayer geoLayer = hit.collider.GetComponent<GeologyLayer>();
            if (geoLayer != null)
            {
                Debug.Log($"✅ 发现地质层组件:");
                Debug.Log($"   层名称: {geoLayer.layerName ?? "未命名"}");
                Debug.Log($"   层颜色: #{ColorUtility.ToHtmlStringRGBA(geoLayer.layerColor)}");
                Debug.Log($"   倾角: {geoLayer.dipAngle}°, 走向: {geoLayer.strikeDirection}");
            }
            else
            {
                Debug.LogWarning($"❌ 撞击对象没有 GeologyLayer 组件");
            }
        }
        else
        {
            Debug.LogWarning($"❌ 在当前位置向下未找到地面");
        }
    }
    
    [ContextMenu("寻找彩色地质层")]
    public void FindColorfulLayers()
    {
        Debug.Log("🌈 寻找彩色地质层...");
        
        GeologyLayer[] allLayers = FindObjectsOfType<GeologyLayer>();
        List<GeologyLayer> colorfulLayers = new List<GeologyLayer>();
        
        foreach (var layer in allLayers)
        {
            if (layer == null) continue;
            
            Color c = layer.layerColor;
            float brightness = (c.r + c.g + c.b) / 3f;
            
            // 查找非灰色、非白色、非黑色的图层
            bool isColorful = false;
            
            // 检查是否为红色系
            if (c.r > 0.6f && (c.g < 0.4f || c.b < 0.4f))
                isColorful = true;
            // 检查是否为绿色系
            else if (c.g > 0.6f && (c.r < 0.4f || c.b < 0.4f))
                isColorful = true;
            // 检查是否为蓝色系
            else if (c.b > 0.6f && (c.r < 0.4f || c.g < 0.4f))
                isColorful = true;
            // 检查是否为黄色系
            else if (c.r > 0.6f && c.g > 0.6f && c.b < 0.4f)
                isColorful = true;
            // 检查是否为紫色系
            else if (c.r > 0.6f && c.b > 0.6f && c.g < 0.4f)
                isColorful = true;
            // 检查是否为青色系
            else if (c.g > 0.6f && c.b > 0.6f && c.r < 0.4f)
                isColorful = true;
            
            if (isColorful)
            {
                colorfulLayers.Add(layer);
            }
        }
        
        Debug.Log($"🎨 发现 {colorfulLayers.Count} 个彩色地质层:");
        
        for (int i = 0; i < colorfulLayers.Count; i++)
        {
            var layer = colorfulLayers[i];
            string colorHtml = ColorUtility.ToHtmlStringRGBA(layer.layerColor);
            
            Debug.Log($"\\n🌈 彩色层 {i + 1}: {layer.name}");
            Debug.Log($"   位置: ({layer.transform.position.x:F1}, {layer.transform.position.y:F1}, {layer.transform.position.z:F1})");
            Debug.Log($"   层名称: {layer.layerName ?? "未命名"}");
            Debug.Log($"   颜色: #{colorHtml}");
            Debug.Log($"   RGB: ({layer.layerColor.r:F2}, {layer.layerColor.g:F2}, {layer.layerColor.b:F2})");
            
            // 计算距离
            float distance = Vector3.Distance(transform.position, layer.transform.position);
            Debug.Log($"   距离当前位置: {distance:F1}m");
            
            if (distance < 100f)
            {
                Debug.Log($"   ✅ 在附近范围内，可以尝试在此采集样本");
            }
        }
        
        if (colorfulLayers.Count == 0)
        {
            Debug.LogWarning("❌ 未找到彩色地质层，所有地质层都是灰色系");
            Debug.Log("💡 建议检查地质数据或在不同位置尝试");
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, checkRadius);
        
        // 绘制向下的射线
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * 10f);
    }
}
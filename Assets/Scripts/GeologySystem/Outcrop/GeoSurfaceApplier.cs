using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Phase 2:把地面地层的材质换成 <c>GeoModel/GeoSurfaceLit</c>（平地草、陡坡露地层），
/// 并把 <see cref="OutcropConfig"/> 登记为当前生效配置(Phase 1/3 的门控也依赖它)。
///
/// 设计为「安全可逆」：
///  - 记录原材质，可一键 <see cref="Revert"/> 还原;
///  - 默认 <see cref="applyOnStart"/>=false，方便在编辑器里边看边开(右键组件 ▸ Apply GeoSurface);
///  - 只替换 MeshRenderer 的主材质槽，不改 <c>GeologyLayer.layerMaterial</c> 字段，
///    因此理论上不影响采样样品外观。
///
/// ⚠️ 首次启用务必在编辑器内确认:钻出的样品仍显示地层岩色、而不是草色。
/// </summary>
public class GeoSurfaceApplier : MonoBehaviour
{
    [Tooltip("阈值与外观的单一真相;留空则门控用 OutcropSurface 内置默认值")]
    public OutcropConfig config;

    [Tooltip("勾选则 Start 时立即应用着色器(不勾也会登记 config 让门控生效)")]
    public bool applyOnStart = false;

    [Tooltip("留空则自动查找场景中所有 GeologyLayer 的 MeshRenderer")]
    public List<MeshRenderer> targetRenderers = new List<MeshRenderer>();

    const string ShaderName = "GeoModel/GeoSurfaceLit";

    readonly Dictionary<MeshRenderer, Material[]> _originals = new Dictionary<MeshRenderer, Material[]>();
    bool _applied;

    void Start()
    {
        if (config != null) config.Activate();   // 让门控/着色器用上配置阈值
        if (applyOnStart) Apply();
    }

    [ContextMenu("Apply GeoSurface")]
    public void Apply()
    {
        if (_applied) return;

        Shader shader = Shader.Find(ShaderName);
        if (shader == null)
        {
            Debug.LogError($"[GeoSurfaceApplier] 找不到着色器 {ShaderName} —— 请确认 GeoSurfaceLit.shader 已导入且无编译错误。");
            return;
        }

        foreach (var r in CollectRenderers())
        {
            if (r == null) continue;
            if (!_originals.ContainsKey(r))
                _originals[r] = r.sharedMaterials;

            Material src = r.sharedMaterial;
            var mat = new Material(shader);

            // 地层底色/贴图:优先取当前材质,回退到 GeologyLayer.layerColor
            Color strataColor = TryGetColor(src, out var c)
                ? c
                : (r.TryGetComponent<GeologyLayer>(out var gl) ? gl.layerColor : Color.gray);
            mat.SetColor("_StrataColor", strataColor);

            Texture strataTex = TryGetTexture(src);
            if (strataTex != null) mat.SetTexture("_StrataTex", strataTex);

            if (config != null)
            {
                mat.SetColor("_GrassColor", config.grassColor);
                if (config.grassAlbedo != null) mat.SetTexture("_GrassTex", config.grassAlbedo);
                mat.SetFloat("_GrassTiling", config.grassTiling);
                mat.SetFloat("_CoveredAngle", config.coveredAngle);
                mat.SetFloat("_OutcropAngle", config.outcropAngle);
            }

            // 多材质槽:只替换主槽,其余保留
            var mats = r.sharedMaterials;
            if (mats.Length > 0)
            {
                mats[0] = mat;
                r.sharedMaterials = mats;
            }
        }
        _applied = true;
        Debug.Log($"[GeoSurfaceApplier] 已对 {_originals.Count} 个地层渲染器应用 GeoSurfaceLit。");
    }

    [ContextMenu("Revert")]
    public void Revert()
    {
        foreach (var kv in _originals)
            if (kv.Key != null) kv.Key.sharedMaterials = kv.Value;
        _originals.Clear();
        _applied = false;
    }

    IEnumerable<MeshRenderer> CollectRenderers()
    {
        if (targetRenderers != null && targetRenderers.Count > 0)
            return targetRenderers;

        var list = new List<MeshRenderer>();
        foreach (var gl in Object.FindObjectsByType<GeologyLayer>(FindObjectsSortMode.None))
            if (gl.TryGetComponent<MeshRenderer>(out var mr)) list.Add(mr);
        return list;
    }

    static bool TryGetColor(Material m, out Color c)
    {
        c = Color.gray;
        if (m == null) return false;
        if (m.HasProperty("_BaseColor")) { c = m.GetColor("_BaseColor"); return true; }
        if (m.HasProperty("_Color"))     { c = m.GetColor("_Color");     return true; }
        return false;
    }

    static Texture TryGetTexture(Material m)
    {
        if (m == null) return null;
        if (m.HasProperty("_BaseMap")) return m.GetTexture("_BaseMap");
        if (m.HasProperty("_MainTex")) return m.GetTexture("_MainTex");
        return null;
    }
}

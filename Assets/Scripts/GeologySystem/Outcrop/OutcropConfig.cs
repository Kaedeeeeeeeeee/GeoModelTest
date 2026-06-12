using UnityEngine;

/// <summary>
/// 露头 / 植被系统的集中配置（单一真相）。
/// 渲染层（GeoSurfaceLit 着色器、植被散布）与门控层（采样工具校验）共用这里的阈值，
/// 保证「看起来是草的地方，一定采不了样」—— 视觉与机制永远一致。
/// 运行时通过 Activate() 把自身登记为 <see cref="OutcropSurface"/> 的当前配置。
/// </summary>
[CreateAssetMenu(fileName = "OutcropConfig", menuName = "GeoModel/Outcrop Config", order = 0)]
public class OutcropConfig : ScriptableObject
{
    [Header("坡度门槛（度，相对水平面）")]
    [Tooltip("倾角 ≤ 此值：完全被植被覆盖（满草）")]
    [Range(0f, 89f)] public float coveredAngle = 22f;

    [Tooltip("倾角 ≥ 此值：完全裸露（露头，显示地层）。\n[coveredAngle, outcropAngle] 之间为过渡带，柔化边缘。")]
    [Range(1f, 90f)] public float outcropAngle = 32f;

    [Header("门控宽容度")]
    [Tooltip("采样判定的坡度容差（度）。露头判定阈值 = outcropAngle - graceDegrees，让露头边缘更易采到，避免挫败感。")]
    [Range(0f, 15f)] public float graceDegrees = 3f;

    [Header("植被外观（GeoSurfaceLit 着色器）")]
    public Color grassColor = new Color(0.36f, 0.49f, 0.24f, 1f);
    [Tooltip("可选；留空则用纯色草地")]
    public Texture2D grassAlbedo;
    [Min(0.01f)] public float grassTiling = 4f;

    [Header("植被点缀（VegetationScatter，Phase 4 用）")]
    [Tooltip("每平方米候选点密度")]
    [Min(0f)] public float accentDensity = 0.5f;
    [Tooltip("WebGL 实例硬上限")]
    [Min(0)] public int maxInstances = 4000;
    [Tooltip("超出此距离的点缀不绘制（距离裁剪）")]
    [Min(1f)] public float accentViewDistance = 60f;

    void OnEnable()
    {
        // 运行时一旦被加载/引用即生效。
        if (Application.isPlaying)
            Activate();
    }

    /// <summary>把本配置登记为 OutcropSurface 当前生效配置。</summary>
    public void Activate()
    {
        OutcropSurface.Config = this;
    }

    void OnValidate()
    {
        // 保证 coveredAngle < outcropAngle，避免过渡带反向。
        if (outcropAngle <= coveredAngle)
            outcropAngle = Mathf.Min(90f, coveredAngle + 1f);
    }
}

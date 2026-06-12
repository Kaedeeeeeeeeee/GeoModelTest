using UnityEngine;

/// <summary>
/// 露头判定的「单一真相」：仅凭地面法线的倾角，决定一块地是露头（裸岩、可采样）
/// 还是被植被覆盖。无状态、无烘焙、纯函数 —— 渲染层与门控层都调用这里，天生一致。
/// 未指定 <see cref="Config"/> 时使用内置默认值，保证任何场景都能安全工作。
/// </summary>
public static class OutcropSurface
{
    /// <summary>当前生效配置；为 null 时用默认值。由 <see cref="OutcropConfig.Activate"/> 赋值。</summary>
    public static OutcropConfig Config;

    // —— 无配置时的默认值 ——
    const float DefaultCoveredAngle = 22f;
    const float DefaultOutcropAngle = 32f;
    const float DefaultGrace = 3f;

    static float CoveredAngle => Config != null ? Config.coveredAngle : DefaultCoveredAngle;
    static float OutcropAngle => Config != null ? Config.outcropAngle : DefaultOutcropAngle;
    static float Grace       => Config != null ? Config.graceDegrees : DefaultGrace;

    /// <summary>地面法线与竖直方向的夹角（度）。0 = 水平地面，90 = 竖直崖面。</summary>
    public static float SlopeAngle(Vector3 surfaceNormal)
    {
        if (surfaceNormal.sqrMagnitude < 1e-8f) return 0f;
        return Vector3.Angle(surfaceNormal, Vector3.up);
    }

    /// <summary>裸露度：0 = 完全被植被覆盖（平地），1 = 完全露头（陡坡）。供着色器/散布密度使用。</summary>
    public static float GetExposure01(Vector3 surfaceNormal)
    {
        return Mathf.Clamp01(Mathf.InverseLerp(CoveredAngle, OutcropAngle, SlopeAngle(surfaceNormal)));
    }

    /// <summary>覆盖度 = 1 - 裸露度。植被越密的地方越接近 1。</summary>
    public static float GetCoverage01(Vector3 surfaceNormal)
    {
        return 1f - GetExposure01(surfaceNormal);
    }

    /// <summary>这块地是否算露头（可采样）。宽容版：倾角达到 outcropAngle - grace 即通过。</summary>
    public static bool IsOutcrop(Vector3 surfaceNormal)
    {
        return SlopeAngle(surfaceNormal) >= (OutcropAngle - Grace);
    }

    /// <summary>便捷重载：直接用射线命中结果判定。</summary>
    public static bool IsOutcrop(RaycastHit hit)
    {
        return IsOutcrop(hit.normal);
    }
}

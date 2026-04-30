using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// CLI-friendly WebGL 构建工具：
///  - ConfigureWebGLSettings: 调整 Player Settings 到推荐值
///  - BuildWebGL: 执行 WebGL 构建到 Build/WebGL/
/// 通过 `Unity -batchmode -nographics -quit -executeMethod WebGLBuildSetup.<Method>` 调用。
/// </summary>
public static class WebGLBuildSetup
{
    private const string BuildOutputPath = "Build/WebGL";

    [MenuItem("Tools/WebGL/Configure Player Settings")]
    public static void ConfigureWebGLSettings()
    {
        Debug.Log("[WebGLBuildSetup] 开始配置 Player Settings...");

        var webgl = NamedBuildTarget.WebGL;

        // Managed Stripping Level: Minimal（避免裁剪误删反射目标）
        PlayerSettings.SetManagedStrippingLevel(webgl, ManagedStrippingLevel.Minimal);
        Debug.Log("[WebGLBuildSetup] ManagedStrippingLevel = Minimal");

        // 初始内存 64MB（默认 32 偏小）
        PlayerSettings.WebGL.initialMemorySize = 64;
        Debug.Log("[WebGLBuildSetup] WebGL.initialMemorySize = 64");

        // 解压 fallback 开（部分浏览器无原生 Brotli 支持）
        PlayerSettings.WebGL.decompressionFallback = true;
        Debug.Log("[WebGLBuildSetup] WebGL.decompressionFallback = true");

        // 异常支持保持现状（已为 1 = Explicitly Thrown Exceptions Only）
        Debug.Log($"[WebGLBuildSetup] WebGL.exceptionSupport (unchanged) = {PlayerSettings.WebGL.exceptionSupport}");

        // 数据缓存保持开
        PlayerSettings.WebGL.dataCaching = true;
        Debug.Log("[WebGLBuildSetup] WebGL.dataCaching = true");

        AssetDatabase.SaveAssets();
        Debug.Log("[WebGLBuildSetup] 配置完成，已保存");
    }

    [MenuItem("Tools/WebGL/Build")]
    public static void BuildWebGL()
    {
        Debug.Log("[WebGLBuildSetup] 准备 WebGL 构建...");

        // 收集启用的 scenes
        var enabledScenes = System.Array.FindAll(EditorBuildSettings.scenes, s => s.enabled);
        if (enabledScenes.Length == 0)
        {
            Debug.LogError("[WebGLBuildSetup] EditorBuildSettings 中没有启用的场景！请到 File > Build Settings 添加。");
            EditorApplication.Exit(1);
            return;
        }

        string[] scenePaths = new string[enabledScenes.Length];
        for (int i = 0; i < enabledScenes.Length; i++)
        {
            scenePaths[i] = enabledScenes[i].path;
            Debug.Log($"[WebGLBuildSetup] Scene[{i}]: {scenePaths[i]}");
        }

        var options = new BuildPlayerOptions
        {
            scenes = scenePaths,
            locationPathName = BuildOutputPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        Debug.Log($"[WebGLBuildSetup] 构建结果: {summary.result}");
        Debug.Log($"[WebGLBuildSetup] 输出: {summary.outputPath}");
        Debug.Log($"[WebGLBuildSetup] 大小: {summary.totalSize / 1024 / 1024} MB");
        Debug.Log($"[WebGLBuildSetup] 耗时: {summary.totalTime}");
        Debug.Log($"[WebGLBuildSetup] 错误数: {summary.totalErrors}");

        if (summary.result != BuildResult.Succeeded)
        {
            Debug.LogError("[WebGLBuildSetup] 构建失败");
            EditorApplication.Exit(1);
        }
        else
        {
            Debug.Log("[WebGLBuildSetup] 构建成功 ✓");
        }
    }
}

using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System;
using System.Globalization;

/// <summary>
/// CLI-friendly WebGL 构建工具：
///  - ConfigureWebGLSettings: 调整 Player Settings 到推荐值
///  - BuildWebGL: 执行 WebGL 构建到 Build/WebGL/
/// 通过 `Unity -batchmode -nographics -quit -executeMethod WebGLBuildSetup.<Method>` 调用。
/// </summary>
public static class WebGLBuildSetup
{
    private const string BuildOutputPath = "Build/WebGL";
    private const string BuildVersionEnvironmentVariable = "GEOMODEL_WEBGL_VERSION";

    [MenuItem("Tools/WebGL/Configure Player Settings")]
    public static void ConfigureWebGLSettings()
    {
        Debug.Log("[WebGLBuildSetup] 开始配置 Player Settings...");

        var webgl = NamedBuildTarget.WebGL;

        string buildVersion = Environment.GetEnvironmentVariable(BuildVersionEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(buildVersion))
        {
            buildVersion = DateTime.UtcNow.ToString("yyyy.MM.dd-HHmm'Z'", CultureInfo.InvariantCulture);
        }

        PlayerSettings.bundleVersion = buildVersion;
        Debug.Log($"[WebGLBuildSetup] ProductVersion = {PlayerSettings.bundleVersion}");

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

        // 自定义 letterbox 模板（保持 16:9 内部分辨率，外层 CSS 缩放 + 黑边）
        PlayerSettings.WebGL.template = "PROJECT:FixedAspect";
        Debug.Log("[WebGLBuildSetup] WebGL.template = PROJECT:FixedAspect");

        AssetDatabase.SaveAssets();
        Debug.Log("[WebGLBuildSetup] 配置完成，已保存");
    }

    [MenuItem("Tools/WebGL/Reimport Mineral GLBs (Apply Texture Compression)")]
    public static void ReimportMineralGLBs()
    {
        string[] guids = AssetDatabase.FindAssets("", new[] { "Assets/Resources/MineralData/Models" });
        int count = 0;
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(".glb"))
            {
                Debug.Log($"[WebGLBuildSetup] Reimporting: {path}");
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                count++;
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[WebGLBuildSetup] Reimported {count} .glb files with WebGL texture compression");
    }

    [MenuItem("Tools/WebGL/Build")]
    public static void BuildWebGL()
    {
        Debug.Log("[WebGLBuildSetup] 准备 WebGL 构建...");

        ConfigureWebGLSettings();

        // 强制把 StartScene 放在 scenes[0]，保证游戏从开始菜单进入而不是直接跳 MainScene
        EnsureStartSceneIsFirst();

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

    /// <summary>
    /// 把 StartScene 强制放在 EditorBuildSettings.scenes[0]，保证 build 出的游戏从开始菜单进入。
    /// 如果 StartScene 不在列表里就追加；如果已在但不是第一个，就移到第一位。
    /// </summary>
    private static void EnsureStartSceneIsFirst()
    {
        const string startScenePath = "Assets/Scenes/StartScene.unity";

        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        int idx = scenes.FindIndex(s => s.path == startScenePath);

        if (idx == 0 && scenes[0].enabled)
        {
            Debug.Log($"[WebGLBuildSetup] StartScene 已在 scenes[0] 且启用");
            return;
        }

        if (idx >= 0)
        {
            // 已存在，但不在第一位 → 移到第一位
            var item = scenes[idx];
            item.enabled = true;
            scenes.RemoveAt(idx);
            scenes.Insert(0, item);
            Debug.Log($"[WebGLBuildSetup] StartScene 从 scenes[{idx}] 移到 scenes[0]");
        }
        else
        {
            // 不在列表里 → 插入第一位
            scenes.Insert(0, new EditorBuildSettingsScene(startScenePath, true));
            Debug.Log($"[WebGLBuildSetup] StartScene 插入到 scenes[0]");
        }

        EditorBuildSettings.scenes = scenes.ToArray();
        for (int i = 0; i < scenes.Count; i++)
        {
            Debug.Log($"[WebGLBuildSetup] 最终 Scene[{i}]: {scenes[i].path} (enabled={scenes[i].enabled})");
        }
    }
}

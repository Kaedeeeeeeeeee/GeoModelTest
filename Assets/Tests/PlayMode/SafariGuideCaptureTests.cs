using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>
/// Opt-in screenshots of the real title screen after the web consent page, with the Safari toolbar hint
/// for each Safari generation. Set GEOMODEL_SAFARI_GUIDE_CAPTURE to an output folder.
/// </summary>
public class SafariGuideCaptureTests
{
    private const BindingFlags Static = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
    private static Type T(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private string _output;
    private int _device;

    [UnityTest]
    public IEnumerator TitleWithSafariHint_ShouldBeCapturedForEachSafariGeneration()
    {
        _output = Environment.GetEnvironmentVariable("GEOMODEL_SAFARI_GUIDE_CAPTURE");
        if (string.IsNullOrEmpty(_output)) Assert.Ignore("Set GEOMODEL_SAFARI_GUIDE_CAPTURE to capture the Safari hint.");
        Directory.CreateDirectory(_output);
        var guide = T("UISystem.SafariToolbarGuide");
        var record = T("Backend.ResearchConsentRecord");
        var stateType = T("Backend.ResearchConsentRecord+PageState");
        Func<int> complete = () => 2;
        var lambda = Expression.Lambda(typeof(Func<>).MakeGenericType(stateType),
            Expression.Convert(Expression.Invoke(Expression.Constant(complete)), stateType));
        var localization = T("LocalizationManager").GetProperty("Instance", Static).GetValue(null);
        localization.GetType().GetMethod("SwitchLanguage").Invoke(localization,
            new[] { Enum.Parse(T("LanguageSettings+Language"), "Japanese") });
        try
        {
            foreach (var (device, name) in new[] { (1027, "iphone-safari27"), (1026, "iphone-safari26"),
                         (2027, "ipad-safari27"), (2026, "ipad-safari26") })
            {
                guide.GetMethod("ResetForTests", Static).Invoke(null, null);
                record.GetField("PageStateOverride", Static).SetValue(null, lambda.Compile());
                _device = device;
                guide.GetField("DeviceOverride", Static).SetValue(null, (Func<int>)(() => _device));
                yield return SceneManager.LoadSceneAsync("StartScene");
                yield return new WaitForSecondsRealtime(1.5f);
                Assert.IsNotNull(GameObject.Find("SafariToolbarGuide"), name);
                yield return Capture("title-hint-" + name);
            }
            GameObject.Find("SafariToolbarGuide/GuideCard/Close").GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Capture("title-after-hint");
        }
        finally
        {
            guide.GetMethod("ResetForTests", Static).Invoke(null, null);
            record.GetMethod("ResetForTests", Static).Invoke(null, null);
        }
    }

    private IEnumerator Capture(string name)
    {
        string path = Path.Combine(_output, name + ".png");
        if (!Application.isBatchMode)
        {
            ScreenCapture.CaptureScreenshot(path);
            float deadline = Time.realtimeSinceStartup + 10f;
            while (!File.Exists(path) && Time.realtimeSinceStartup < deadline) yield return null;
            yield break;
        }
        yield return null;
        const int width = 1600, height = 900;
        var camera = new GameObject("CaptureCamera").AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
            .Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay && c.gameObject.activeInHierarchy)
            .OrderBy(c => c.sortingOrder).ToArray();
        var target = new RenderTexture(width, height, 24);
        Texture2D image = null;
        try
        {
            camera.targetTexture = target;
            for (int i = 0; i < canvases.Length; i++)
            {
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                canvases[i].worldCamera = camera;
                canvases[i].planeDistance = camera.nearClipPlane + 0.1f + 0.001f * (canvases.Length - i);
            }
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
        }
        finally
        {
            foreach (var canvas in canvases)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.worldCamera = null;
            }
            RenderTexture.active = null;
            camera.targetTexture = null;
            target.Release();
            UnityEngine.Object.Destroy(target);
            UnityEngine.Object.Destroy(camera.gameObject);
            if (image != null) UnityEngine.Object.Destroy(image);
        }
    }
}

using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>
/// 知識スライド 9 組を一歩ずつ表示して 1280×720 で撮る。
/// GEOMODEL_SLIDES_CAPTURE に保存先フォルダを渡したときだけ動く（ふだんのテストでは Ignore）。
/// ファイル名は &lt;スライドID&gt;_&lt;段&gt;.png（-1 は土台の図だけ）。
/// </summary>
public class KnowledgeSlidesCaptureTests
{
    private static readonly (string sequence, string id)[] Slides =
    {
        ("Story/quest2.1", "slide.rock"),
        ("Story/quest3.4", "slide.grain"),
        ("Story/quest3.4", "slide.lime"),
        ("Story/beat3", "slide.coral"),
        ("Story/beat3", "slide.facies"),
        ("Story/beat3", "slide.era"),
        ("Story/beat3", "slide.tuff"),
        ("Story/beat4", "slide.keybed"),
        ("Story/beat4", "slide.fold"),
    };

    private const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    private static Type T(string name) => Type.GetType(name + ", Assembly-CSharp", true);

    [UnityTest]
    public IEnumerator EverySlideStep_ShouldBeCaptured()
    {
        string directory = Environment.GetEnvironmentVariable("GEOMODEL_SLIDES_CAPTURE");
        if (string.IsNullOrEmpty(directory)) Assert.Ignore("Set GEOMODEL_SLIDES_CAPTURE to capture the teaching slides.");
        Directory.CreateDirectory(directory);

        var loader = T("StorySystem.StorySequenceLoader");
        var sessionType = T("StorySystem.KnowledgeSlideSession");
        var subtitle = T("StorySystem.StoryDirector+SubtitleUI");
        var tmpText = Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro", true);
        var style = Delegate.CreateDelegate(typeof(Action<>).MakeGenericType(tmpText), subtitle.GetMethod("ApplyTmpFont", Any));

        foreach (var (sequence, id) in Slides)
        {
            object story = loader.GetMethod("LoadFromResources", Any).Invoke(null, new object[] { sequence, true });
            Assert.IsNotNull(story, sequence);
            var lines = (IList)loader.GetMethod("ToSubtitleLines", Any).Invoke(null, new[] { story });
            object card = null;
            foreach (var line in lines)
            {
                var c = line.GetType().GetField("Card", Any)?.GetValue(line);
                if (c != null && (string)(c.GetType().GetProperty("Id", Any)?.GetValue(c) ?? c.GetType().GetField("Id", Any)?.GetValue(c)) == id) { card = c; break; }
            }
            Assert.IsNotNull(card, id);

            var (canvasObject, camera) = MakeCanvas();
            object session = sessionType.GetMethod("Build", Any).Invoke(null, new[] { canvasObject.transform, card, style, null, null });
            Assert.IsNotNull(session, id);
            int count = (int)sessionType.GetProperty("StepCount").GetValue(session);
            for (int step = -1; step < count; step++)
            {
                sessionType.GetMethod("ShowStep").Invoke(session, new object[] { step });
                yield return new WaitForSecondsRealtime(1.6f); // 出現・移動・点滅が終わるまで
                Capture(camera, Path.Combine(directory, $"{id}_{step}.png"));
            }
            sessionType.GetMethod("Destroy").Invoke(session, null);
            UnityEngine.Object.Destroy(canvasObject);
            UnityEngine.Object.Destroy(camera.gameObject);
            yield return null;
        }
    }

    private static (GameObject, Camera) MakeCanvas()
    {
        var cameraObject = new GameObject("SlidesCaptureCamera", typeof(Camera));
        var camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.06f, 0.12f, 0.16f);
        camera.orthographic = true;
        var canvasObject = new GameObject("SlidesCaptureCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1f;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        return (canvasObject, camera);
    }

    private static void Capture(Camera camera, string path)
    {
        const int w = 1280, h = 720;
        var target = new RenderTexture(w, h, 24);
        camera.targetTexture = target;
        Canvas.ForceUpdateCanvases();
        camera.Render();
        var previous = RenderTexture.active;
        RenderTexture.active = target;
        var shot = new Texture2D(w, h, TextureFormat.RGB24, false);
        shot.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        shot.Apply();
        File.WriteAllBytes(path, shot.EncodeToPNG());
        RenderTexture.active = previous;
        camera.targetTexture = null;
        target.Release();
        UnityEngine.Object.Destroy(target);
        UnityEngine.Object.Destroy(shot);
    }
}

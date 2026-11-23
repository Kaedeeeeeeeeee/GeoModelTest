using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>
/// 手機の横画面（2868×1320）で、MainScene の上に本物の会話欄・立ち絵・知識スライドを出して撮る。
/// GEOMODEL_SLIDES_PHONE_CAPTURE に保存先フォルダを渡したときだけ動く。
/// 撮るもの：slide.lime の最初の 3 段、slide.rock の最後の段、スライドのない普通の会話。
/// </summary>
public class SlidesPhoneLayoutCaptureTests
{
    private const int Width = 2868, Height = 1320;
    private const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    private static Type T(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private string _output;

    [UnityTest]
    public IEnumerator PhoneLayout_ShouldBeCapturedWithDialogueAndSlides()
    {
        _output = Environment.GetEnvironmentVariable("GEOMODEL_SLIDES_PHONE_CAPTURE");
        if (string.IsNullOrEmpty(_output)) Assert.Ignore("Set GEOMODEL_SLIDES_PHONE_CAPTURE to capture the phone layout.");
        Directory.CreateDirectory(_output);

        yield return SceneManager.LoadSceneAsync("MainScene");
        yield return new WaitForSecondsRealtime(4f);

        // 普通の会話（スライドなし）
        yield return Play("Story/quest2.1", line => Card(line) == null, 0);
        yield return new WaitForSecondsRealtime(2.5f);
        yield return Capture("dialogue-plain");
        Cancel();

        // slide.lime：最初の 3 段
        yield return Play("Story/quest3.4", line => Id(Card(line)) == "slide.lime", 0);
        yield return new WaitForSecondsRealtime(3.5f);
        yield return Capture("lime-step0");
        for (int i = 1; i <= 2; i++)
        {
            Tap();
            yield return new WaitForSecondsRealtime(3.5f);
            yield return Capture("lime-step" + i);
        }
        Cancel();

        // slide.rock：最後の段まで進める
        yield return Play("Story/quest2.1", line => Id(Card(line)) == "slide.rock", 0);
        yield return new WaitForSecondsRealtime(2f);
        var session = Active();
        int count = (int)session.GetType().GetProperty("StepCount").GetValue(session);
        for (int i = 1; i < count; i++)
        {
            Tap();
            yield return new WaitForSecondsRealtime(2.5f);
        }
        yield return new WaitForSecondsRealtime(2f);
        yield return Capture("rock-last-step");
        Cancel();
    }

    private static object Card(object line) => line?.GetType().GetField("Card", Any)?.GetValue(line);
    private static string Id(object card) => card == null ? null
        : (string)(card.GetType().GetProperty("Id", Any)?.GetValue(card) ?? card.GetType().GetField("Id", Any)?.GetValue(card));

    private IEnumerator Play(string sequence, Func<object, bool> start, int extra)
    {
        object story = T("StorySystem.StorySequenceLoader").GetMethod("LoadFromResources", Any).Invoke(null, new object[] { sequence, true });
        var all = ((IList)T("StorySystem.StorySequenceExtensions").GetMethod("ToSubtitleLines", Any).Invoke(null, new[] { story })).Cast<object>().ToList();
        int index = all.FindIndex(l => start(l));
        Assert.GreaterOrEqual(index, 0, sequence);
        var lineType = all[0].GetType();
        var list = (IList)Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(lineType));
        foreach (var l in all.Skip(index)) list.Add(l);
        var subtitle = T("StorySystem.StoryDirector+SubtitleUI");
        var show = subtitle.GetMethod("ShowSequence", Any);
        var args = show.GetParameters().Select(p => p.Position == 0 ? (object)list : null).ToArray();
        var routine = (IEnumerator)show.Invoke(null, args);
        var runner = T("StorySystem.StoryDirectorRunner").GetProperty("Instance", Any).GetValue(null);
        ((MonoBehaviour)runner).StartCoroutine(routine);
        yield return null;
    }

    private static object Active() => T("StorySystem.KnowledgeSlideSession").GetProperty("Active", Any).GetValue(null);

    private static void Tap()
    {
        var session = Active();
        Assert.IsNotNull(session, "a slide session is showing");
        var root = (GameObject)session.GetType().GetProperty("Root").GetValue(session);
        var stage = root.GetComponentsInChildren<Button>(true).First(b => b.name == "Stage");
        stage.onClick.Invoke();
    }

    private static void Cancel()
    {
        T("StorySystem.StoryDirector+SubtitleUI").GetMethod("CancelAll", Any).Invoke(null, null);
        T("Core.GameInputState").GetMethod("ReleaseAll", Any)?.Invoke(null, null);
        Time.timeScale = 1f;
    }

    // 画面いっぱいの Overlay キャンバスをカメラ描画に切り替えて、2868×1320 の RenderTexture に描く
    private IEnumerator Capture(string name)
    {
        yield return null;
        var cameraObject = new GameObject("PhoneCaptureCamera", typeof(Camera));
        var camera = cameraObject.GetComponent<Camera>();
        var main = Camera.main;
        if (main != null) camera.CopyFrom(main);
        var target = new RenderTexture(Width, Height, 24);
        camera.targetTexture = target;
        var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
            .Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay && c.gameObject.activeInHierarchy)
            .OrderBy(c => c.sortingOrder).ToArray();
        for (int i = 0; i < canvases.Length; i++)
        {
            canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
            canvases[i].worldCamera = camera;
            canvases[i].planeDistance = camera.nearClipPlane + 0.1f + 0.001f * (canvases.Length - i);
        }
        yield return null; // キャンバスを新しい大きさで組み直す
        Canvas.ForceUpdateCanvases();
        camera.Render();
        var previous = RenderTexture.active;
        RenderTexture.active = target;
        var shot = new Texture2D(Width, Height, TextureFormat.RGB24, false);
        shot.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
        shot.Apply();
        File.WriteAllBytes(Path.Combine(_output, name + ".png"), shot.EncodeToPNG());
        RenderTexture.active = previous;
        foreach (var c in canvases)
        {
            if (c == null) continue;
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.worldCamera = null;
        }
        camera.targetTexture = null;
        target.Release();
        UnityEngine.Object.Destroy(target);
        UnityEngine.Object.Destroy(shot);
        UnityEngine.Object.Destroy(cameraObject);
    }
}
